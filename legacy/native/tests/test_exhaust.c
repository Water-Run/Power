// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#include "pwr_exhaust.h"

#include <math.h>
#include <stddef.h>
#include <stdio.h>
#include <string.h>

#define CHECK(condition)                                                       \
    do {                                                                       \
        if (!(condition)) {                                                    \
            fprintf(stderr,                                                    \
                    "%s:%d: check failed: %s\n",                              \
                    __FILE__,                                                  \
                    __LINE__,                                                  \
                    #condition);                                               \
            return 0;                                                          \
        }                                                                      \
    } while (0)

static pwr_exhaust_parameters test_parameters(void)
{
    pwr_exhaust_parameters parameters = {0};

    parameters.volume_m3[PWR_EXHAUST_VOLUME_MANIFOLD] = 1.5e-3;
    parameters.volume_m3[PWR_EXHAUST_VOLUME_CATALYST] = 2.5e-3;
    parameters.volume_m3[PWR_EXHAUST_VOLUME_MUFFLER] = 4.0e-3;

    parameters.wall_heat_capacity_j_per_k[PWR_EXHAUST_VOLUME_MANIFOLD] = 300.0;
    parameters.wall_heat_capacity_j_per_k[PWR_EXHAUST_VOLUME_CATALYST] = 180.0;
    parameters.wall_heat_capacity_j_per_k[PWR_EXHAUST_VOLUME_MUFFLER] = 500.0;
    parameters.gas_wall_conductance_w_per_k[PWR_EXHAUST_VOLUME_MANIFOLD] = 18.0;
    parameters.gas_wall_conductance_w_per_k[PWR_EXHAUST_VOLUME_CATALYST] = 75.0;
    parameters.gas_wall_conductance_w_per_k[PWR_EXHAUST_VOLUME_MUFFLER] = 15.0;
    parameters.wall_ambient_conductance_w_per_k[PWR_EXHAUST_VOLUME_MANIFOLD]
        = 1.0;
    parameters.wall_ambient_conductance_w_per_k[PWR_EXHAUST_VOLUME_CATALYST]
        = 1.5;
    parameters.wall_ambient_conductance_w_per_k[PWR_EXHAUST_VOLUME_MUFFLER]
        = 2.0;

    parameters.orifice_area_m2[PWR_EXHAUST_PATH_INLET] = 6.0e-5;
    parameters.orifice_area_m2[PWR_EXHAUST_PATH_MANIFOLD_TO_CATALYST] = 5.0e-5;
    parameters.orifice_area_m2[PWR_EXHAUST_PATH_CATALYST_TO_MUFFLER] = 6.0e-5;
    parameters.orifice_area_m2[PWR_EXHAUST_PATH_TAILPIPE] = 8.0e-5;
    for (size_t path = 0; path < PWR_EXHAUST_PATH_COUNT; ++path) {
        parameters.discharge_coefficient[path] = 0.72;
    }

    parameters.species_gas_constant_j_per_kg_k[PWR_EXHAUST_SPECIES_INERT]
        = 287.0;
    parameters.species_gas_constant_j_per_kg_k[PWR_EXHAUST_SPECIES_OXYGEN]
        = 260.0;
    parameters.species_gas_constant_j_per_kg_k[PWR_EXHAUST_SPECIES_REDUCTANT]
        = 245.0;
    parameters.species_gas_constant_j_per_kg_k[PWR_EXHAUST_SPECIES_PRODUCTS]
        = 190.0;
    parameters.species_cv_j_per_kg_k[PWR_EXHAUST_SPECIES_INERT] = 720.0;
    parameters.species_cv_j_per_kg_k[PWR_EXHAUST_SPECIES_OXYGEN] = 660.0;
    parameters.species_cv_j_per_kg_k[PWR_EXHAUST_SPECIES_REDUCTANT] = 1500.0;
    parameters.species_cv_j_per_kg_k[PWR_EXHAUST_SPECIES_PRODUCTS] = 900.0;

    parameters.catalyst_light_off_temperature_k = 500.0;
    parameters.catalyst_full_activity_temperature_k = 680.0;
    parameters.catalyst_activity_time_constant_s = 0.35;
    parameters.catalyst_reaction_time_s = 0.040;
    parameters.catalyst_oxygen_per_reductant_kg_per_kg = 3.0;
    parameters.catalyst_reaction_heat_j_per_kg_reductant = 20.0e6;
    parameters.catalyst_heat_to_wall_fraction = 0.75;

    parameters.minimum_temperature_k = 150.0;
    parameters.minimum_volume_mass_kg = 1.0e-9;
    parameters.maximum_substep_s = 5.0e-4;
    parameters.maximum_outflow_fraction_per_substep = 0.10;
    parameters.maximum_substeps = 1000u;
    return parameters;
}

static pwr_exhaust_reservoir air_reservoir(double pressure_pa, double temperature_k)
{
    pwr_exhaust_reservoir reservoir = {0};

    reservoir.pressure_pa = pressure_pa;
    reservoir.temperature_k = temperature_k;
    reservoir.species_mass_fraction[PWR_EXHAUST_SPECIES_INERT] = 0.77;
    reservoir.species_mass_fraction[PWR_EXHAUST_SPECIES_OXYGEN] = 0.23;
    return reservoir;
}

static pwr_exhaust_reservoir hot_exhaust_reservoir(
    double pressure_pa,
    double temperature_k)
{
    pwr_exhaust_reservoir reservoir = {0};

    reservoir.pressure_pa = pressure_pa;
    reservoir.temperature_k = temperature_k;
    reservoir.species_mass_fraction[PWR_EXHAUST_SPECIES_INERT] = 0.76;
    reservoir.species_mass_fraction[PWR_EXHAUST_SPECIES_OXYGEN] = 0.14;
    reservoir.species_mass_fraction[PWR_EXHAUST_SPECIES_REDUCTANT] = 0.01;
    reservoir.species_mass_fraction[PWR_EXHAUST_SPECIES_PRODUCTS] = 0.09;
    return reservoir;
}

static double total_mass(const pwr_exhaust_state *state)
{
    double result = 0.0;

    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        result += state->volume[volume].total_mass_kg;
    }
    return result;
}

static double total_species_mass(
    const pwr_exhaust_state *state,
    size_t species)
{
    double result = 0.0;

    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        result += state->volume[volume].species_mass_kg[species];
    }
    return result;
}

static int test_parameter_and_step_rejection_is_transactional(void)
{
    pwr_exhaust_parameters parameters = test_parameters();
    const pwr_exhaust_reservoir ambient = air_reservoir(101325.0, 300.0);
    pwr_exhaust_state state;
    pwr_exhaust_state before;
    pwr_exhaust_inputs inputs = {
        .upstream = ambient,
        .downstream = ambient,
        .inlet_area_scale = 1.0,
        .tailpipe_area_scale = 1.0,
        .ambient_temperature_k = 300.0,
    };
    pwr_exhaust_diagnostics diagnostics;

    CHECK(pwr_exhaust_validate_parameters(&parameters) == PWR_EXHAUST_OK);
    parameters.volume_m3[PWR_EXHAUST_VOLUME_CATALYST] = 0.0;
    CHECK(pwr_exhaust_validate_parameters(&parameters)
          == PWR_EXHAUST_INVALID_PARAMETER);
    parameters = test_parameters();
    CHECK(pwr_exhaust_initialize_uniform(
              &parameters, &ambient, 300.0, &state)
          == PWR_EXHAUST_OK);
    before = state;
    CHECK(pwr_exhaust_step(&parameters, &state, &inputs, -0.01, &diagnostics)
          == PWR_EXHAUST_INVALID_TIMESTEP);
    CHECK((diagnostics.flags & PWR_EXHAUST_DIAG_INPUT_REJECTED) != 0u);
    CHECK(memcmp(&before, &state, sizeof(state)) == 0);
    return 1;
}

static int test_closed_network_conserves_total_and_species_mass(void)
{
    pwr_exhaust_parameters parameters = test_parameters();
    const pwr_exhaust_reservoir initial = air_reservoir(120000.0, 550.0);
    pwr_exhaust_state state;
    pwr_exhaust_inputs inputs = {
        .upstream = initial,
        .downstream = initial,
        .inlet_area_scale = 0.0,
        .tailpipe_area_scale = 0.0,
        .ambient_temperature_k = 550.0,
    };
    double initial_total;
    double initial_species[PWR_EXHAUST_SPECIES_COUNT];

    parameters.wall_ambient_conductance_w_per_k[0] = 0.0;
    parameters.wall_ambient_conductance_w_per_k[1] = 0.0;
    parameters.wall_ambient_conductance_w_per_k[2] = 0.0;
    CHECK(pwr_exhaust_initialize_uniform(
              &parameters, &initial, 550.0, &state)
          == PWR_EXHAUST_OK);

    /* Seed internal pressure gradients without changing any composition. */
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        state.volume[PWR_EXHAUST_VOLUME_MANIFOLD].species_mass_kg[species]
            *= 1.35;
        state.volume[PWR_EXHAUST_VOLUME_CATALYST].species_mass_kg[species]
            *= 0.75;
    }
    state.volume[PWR_EXHAUST_VOLUME_MANIFOLD].total_mass_kg *= 1.35;
    state.volume[PWR_EXHAUST_VOLUME_MANIFOLD].internal_energy_j *= 1.35;
    state.volume[PWR_EXHAUST_VOLUME_CATALYST].total_mass_kg *= 0.75;
    state.volume[PWR_EXHAUST_VOLUME_CATALYST].internal_energy_j *= 0.75;

    initial_total = total_mass(&state);
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        initial_species[species] = total_species_mass(&state, species);
    }

    for (size_t iteration = 0; iteration < 1000u; ++iteration) {
        pwr_exhaust_diagnostics diagnostics;
        CHECK(pwr_exhaust_step(
                  &parameters, &state, &inputs, 0.002, &diagnostics)
              == PWR_EXHAUST_OK);
        CHECK(fabs(diagnostics.mass_residual_kg) < 2.0e-14);
        CHECK(fabs(diagnostics.energy_residual_j) < 2.0e-7);
        CHECK(diagnostics.positivity_correction_events == 0u);
        CHECK(fabs(diagnostics.state_species_closure_residual_kg) < 2.0e-14);
    }

    CHECK(fabs(total_mass(&state) - initial_total) < 2.0e-12);
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        CHECK(fabs(total_species_mass(&state, species) - initial_species[species])
              < 2.0e-12);
    }
    return 1;
}

static int test_orifice_reports_reverse_and_choked_flow(void)
{
    const pwr_exhaust_parameters parameters = test_parameters();
    const pwr_exhaust_reservoir initial = air_reservoir(101325.0, 300.0);
    pwr_exhaust_state state;
    pwr_exhaust_inputs inputs = {
        .upstream = air_reservoir(30000.0, 300.0),
        .downstream = initial,
        .inlet_area_scale = 1.0,
        .tailpipe_area_scale = 0.0,
        .ambient_temperature_k = 300.0,
    };
    pwr_exhaust_diagnostics diagnostics;

    CHECK(pwr_exhaust_initialize_uniform(
              &parameters, &initial, 300.0, &state)
          == PWR_EXHAUST_OK);
    CHECK(pwr_exhaust_step(
              &parameters, &state, &inputs, 0.0005, &diagnostics)
          == PWR_EXHAUST_OK);
    CHECK(diagnostics.average_path_mass_flow_kg_per_s[PWR_EXHAUST_PATH_INLET]
          < 0.0);
    CHECK((diagnostics.flags & PWR_EXHAUST_DIAG_REVERSE_FLOW) != 0u);
    CHECK((diagnostics.flags & PWR_EXHAUST_DIAG_CHOKED_FLOW) != 0u);
    CHECK((diagnostics.choked_path_mask
           & (1u << PWR_EXHAUST_PATH_INLET))
          != 0u);
    CHECK(diagnostics.boundary_mass_net_kg < 0.0);
    return 1;
}

static int run_backpressure_case(
    double tailpipe_area_scale,
    pwr_exhaust_observation *observation,
    double *final_inlet_flow_kg_per_s,
    double *final_tailpipe_flow_kg_per_s)
{
    const pwr_exhaust_parameters parameters = test_parameters();
    const pwr_exhaust_reservoir ambient = air_reservoir(101325.0, 300.0);
    pwr_exhaust_state state;
    pwr_exhaust_inputs inputs = {
        .upstream = hot_exhaust_reservoir(180000.0, 850.0),
        .downstream = ambient,
        .inlet_area_scale = 1.0,
        .tailpipe_area_scale = tailpipe_area_scale,
        .ambient_temperature_k = 300.0,
    };
    pwr_exhaust_diagnostics diagnostics = {0};

    CHECK(pwr_exhaust_initialize_uniform(
              &parameters, &ambient, 300.0, &state)
          == PWR_EXHAUST_OK);
    for (size_t iteration = 0; iteration < 1200u; ++iteration) {
        CHECK(pwr_exhaust_step(
                  &parameters, &state, &inputs, 0.005, &diagnostics)
              == PWR_EXHAUST_OK);
    }
    *observation = diagnostics.final_observation;
    *final_inlet_flow_kg_per_s =
        diagnostics.average_path_mass_flow_kg_per_s[PWR_EXHAUST_PATH_INLET];
    *final_tailpipe_flow_kg_per_s =
        diagnostics.average_path_mass_flow_kg_per_s[PWR_EXHAUST_PATH_TAILPIPE];
    return 1;
}

static int test_tailpipe_blockage_raises_upstream_backpressure(void)
{
    pwr_exhaust_observation open_observation;
    pwr_exhaust_observation blocked_observation;
    double open_inlet_flow;
    double open_tailpipe_flow;
    double blocked_inlet_flow;
    double blocked_tailpipe_flow;

    CHECK(run_backpressure_case(
        1.0, &open_observation, &open_inlet_flow, &open_tailpipe_flow));
    CHECK(run_backpressure_case(
        0.04,
        &blocked_observation,
        &blocked_inlet_flow,
        &blocked_tailpipe_flow));

    CHECK(blocked_observation.pressure_pa[PWR_EXHAUST_VOLUME_MANIFOLD]
          > open_observation.pressure_pa[PWR_EXHAUST_VOLUME_MANIFOLD] + 5000.0);
    CHECK(blocked_observation.pressure_pa[PWR_EXHAUST_VOLUME_CATALYST]
          > open_observation.pressure_pa[PWR_EXHAUST_VOLUME_CATALYST] + 5000.0);
    CHECK(blocked_inlet_flow < open_inlet_flow);
    CHECK(blocked_tailpipe_flow < 0.30 * open_tailpipe_flow);
    return 1;
}

static int test_catalyst_warms_and_reaches_light_off(void)
{
    pwr_exhaust_parameters parameters = test_parameters();
    const pwr_exhaust_reservoir ambient = air_reservoir(101325.0, 300.0);
    pwr_exhaust_state state;
    pwr_exhaust_inputs inputs = {
        .upstream = hot_exhaust_reservoir(180000.0, 1000.0),
        .downstream = ambient,
        .inlet_area_scale = 1.0,
        .tailpipe_area_scale = 1.0,
        .ambient_temperature_k = 300.0,
    };
    pwr_exhaust_diagnostics diagnostics = {0};
    double reacted_first_second = 0.0;
    double reacted_total = 0.0;
    double light_off_time_s = -1.0;

    /* A deliberately low-inertia laboratory brick, not an EA211 calibration. */
    parameters.wall_heat_capacity_j_per_k[PWR_EXHAUST_VOLUME_CATALYST] = 60.0;
    parameters.gas_wall_conductance_w_per_k[PWR_EXHAUST_VOLUME_MANIFOLD] = 5.0;
    parameters.gas_wall_conductance_w_per_k[PWR_EXHAUST_VOLUME_CATALYST] = 120.0;
    parameters.wall_ambient_conductance_w_per_k[PWR_EXHAUST_VOLUME_CATALYST]
        = 0.3;

    CHECK(pwr_exhaust_initialize_uniform(
              &parameters, &ambient, 300.0, &state)
          == PWR_EXHAUST_OK);
    CHECK(state.catalyst_conversion_fraction == 0.0);

    for (size_t iteration = 0; iteration < 3000u; ++iteration) {
        CHECK(pwr_exhaust_step(
                  &parameters, &state, &inputs, 0.005, &diagnostics)
              == PWR_EXHAUST_OK);
        reacted_total += diagnostics.reacted_reductant_kg;
        if (state.time_s <= 1.0) {
            reacted_first_second += diagnostics.reacted_reductant_kg;
        }
        if (light_off_time_s < 0.0
            && state.catalyst_conversion_fraction >= 0.5) {
            light_off_time_s = state.time_s;
        }
    }

    CHECK(diagnostics.final_observation
              .wall_temperature_k[PWR_EXHAUST_VOLUME_CATALYST]
          > parameters.catalyst_light_off_temperature_k);
    CHECK(state.catalyst_conversion_fraction > 0.70);
    CHECK(light_off_time_s > 1.0);
    CHECK(light_off_time_s < state.time_s);
    CHECK(reacted_total > 5.0 * reacted_first_second);
    CHECK(reacted_total > 0.0);
    CHECK((diagnostics.flags & PWR_EXHAUST_DIAG_CATALYST_LIGHT_OFF) != 0u);
    CHECK(state.cumulative_tailpipe_species_kg[PWR_EXHAUST_SPECIES_REDUCTANT]
          > 0.0);
    return 1;
}

int main(void)
{
    CHECK(test_parameter_and_step_rejection_is_transactional());
    CHECK(test_closed_network_conserves_total_and_species_mass());
    CHECK(test_orifice_reports_reverse_and_choked_flow());
    CHECK(test_tailpipe_blockage_raises_upstream_backpressure());
    CHECK(test_catalyst_warms_and_reaches_light_off());
    puts("exhaust model tests passed");
    return 0;
}
