// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#include "pwr_dct_powertrain.h"

#include <math.h>
#include <stddef.h>
#include <string.h>

static const double pwr_dct_powertrain_tick_s = 1.0e-4;

static_assert((int)PWR_SI_ENGINE_SPECIES_COUNT
                  == (int)PWR_EXHAUST_SPECIES_COUNT,
              "engine/exhaust species vectors must have the same width");
static_assert((int)PWR_SI_ENGINE_SPECIES_INERT
                  == (int)PWR_EXHAUST_SPECIES_INERT,
              "engine/exhaust inert species indices must match");
static_assert((int)PWR_SI_ENGINE_SPECIES_OXYGEN
                  == (int)PWR_EXHAUST_SPECIES_OXYGEN,
              "engine/exhaust oxygen species indices must match");
static_assert(
    (int)PWR_SI_ENGINE_SPECIES_REDUCTANT
        == (int)PWR_EXHAUST_SPECIES_REDUCTANT,
    "engine/exhaust reductant species indices must match");
static_assert((int)PWR_SI_ENGINE_SPECIES_PRODUCTS
                  == (int)PWR_EXHAUST_SPECIES_PRODUCTS,
              "engine/exhaust products species indices must match");

typedef struct pwr_dct_powertrain_commands {
    double engine_throttle;
    double starter_torque_nm;
    pwr_dct_gear_mask gear_mask;
    double clutch_clamp[PWR_DCT_CLUTCH_COUNT];
} pwr_dct_powertrain_commands;

static bool pwr_pt_positive_finite(double value)
{
    return isfinite(value) && value > 0.0;
}

static bool pwr_pt_nonnegative_finite(double value)
{
    return isfinite(value) && value >= 0.0;
}

static double pwr_pt_min(double left, double right)
{
    return left < right ? left : right;
}

static double pwr_pt_max(double left, double right)
{
    return left > right ? left : right;
}

static double pwr_pt_clamp(double value, double minimum, double maximum)
{
    return pwr_pt_min(pwr_pt_max(value, minimum), maximum);
}

static pwr_exhaust_reservoir pwr_pt_ambient_reservoir(
    double pressure_pa,
    double temperature_k)
{
    pwr_exhaust_reservoir reservoir = {
        .pressure_pa = pressure_pa,
        .temperature_k = temperature_k,
    };

    reservoir.species_mass_fraction[PWR_EXHAUST_SPECIES_INERT] = 0.77;
    reservoir.species_mass_fraction[PWR_EXHAUST_SPECIES_OXYGEN] = 0.23;
    return reservoir;
}

static void pwr_pt_exhaust_parameters_default(
    pwr_exhaust_parameters *parameters)
{
    *parameters = (pwr_exhaust_parameters){0};
    parameters->volume_m3[PWR_EXHAUST_VOLUME_MANIFOLD] = 1.5e-3;
    parameters->volume_m3[PWR_EXHAUST_VOLUME_CATALYST] = 2.5e-3;
    parameters->volume_m3[PWR_EXHAUST_VOLUME_MUFFLER] = 4.0e-3;

    parameters->wall_heat_capacity_j_per_k[PWR_EXHAUST_VOLUME_MANIFOLD]
        = 300.0;
    parameters->wall_heat_capacity_j_per_k[PWR_EXHAUST_VOLUME_CATALYST]
        = 180.0;
    parameters->wall_heat_capacity_j_per_k[PWR_EXHAUST_VOLUME_MUFFLER]
        = 500.0;
    parameters->gas_wall_conductance_w_per_k[PWR_EXHAUST_VOLUME_MANIFOLD]
        = 18.0;
    parameters->gas_wall_conductance_w_per_k[PWR_EXHAUST_VOLUME_CATALYST]
        = 75.0;
    parameters->gas_wall_conductance_w_per_k[PWR_EXHAUST_VOLUME_MUFFLER]
        = 15.0;
    parameters->wall_ambient_conductance_w_per_k[
        PWR_EXHAUST_VOLUME_MANIFOLD] = 1.0;
    parameters->wall_ambient_conductance_w_per_k[
        PWR_EXHAUST_VOLUME_CATALYST] = 1.5;
    parameters->wall_ambient_conductance_w_per_k[
        PWR_EXHAUST_VOLUME_MUFFLER] = 2.0;

    parameters->orifice_area_m2[PWR_EXHAUST_PATH_INLET] = 6.0e-5;
    parameters->orifice_area_m2[PWR_EXHAUST_PATH_MANIFOLD_TO_CATALYST]
        = 5.0e-5;
    parameters->orifice_area_m2[PWR_EXHAUST_PATH_CATALYST_TO_MUFFLER]
        = 6.0e-5;
    parameters->orifice_area_m2[PWR_EXHAUST_PATH_TAILPIPE] = 8.0e-5;
    for (size_t path = 0U; path < PWR_EXHAUST_PATH_COUNT; ++path) {
        parameters->discharge_coefficient[path] = 0.72;
    }

    parameters->species_gas_constant_j_per_kg_k[
        PWR_EXHAUST_SPECIES_INERT] = 287.0;
    parameters->species_gas_constant_j_per_kg_k[
        PWR_EXHAUST_SPECIES_OXYGEN] = 260.0;
    parameters->species_gas_constant_j_per_kg_k[
        PWR_EXHAUST_SPECIES_REDUCTANT] = 245.0;
    parameters->species_gas_constant_j_per_kg_k[
        PWR_EXHAUST_SPECIES_PRODUCTS] = 190.0;
    parameters->species_cv_j_per_kg_k[PWR_EXHAUST_SPECIES_INERT] = 720.0;
    parameters->species_cv_j_per_kg_k[PWR_EXHAUST_SPECIES_OXYGEN] = 660.0;
    parameters->species_cv_j_per_kg_k[PWR_EXHAUST_SPECIES_REDUCTANT] = 1500.0;
    parameters->species_cv_j_per_kg_k[PWR_EXHAUST_SPECIES_PRODUCTS] = 900.0;

    parameters->catalyst_light_off_temperature_k = 500.0;
    parameters->catalyst_full_activity_temperature_k = 680.0;
    parameters->catalyst_activity_time_constant_s = 0.35;
    parameters->catalyst_reaction_time_s = 0.040;
    parameters->catalyst_oxygen_per_reductant_kg_per_kg = 3.0;
    parameters->catalyst_reaction_heat_j_per_kg_reductant = 20.0e6;
    parameters->catalyst_heat_to_wall_fraction = 0.75;
    parameters->minimum_temperature_k = 150.0;
    parameters->minimum_volume_mass_kg = 1.0e-9;
    parameters->maximum_substep_s = pwr_dct_powertrain_tick_s;
    parameters->maximum_outflow_fraction_per_substep = 0.10;
    parameters->maximum_substeps = UINT32_C(8);
}

void pwr_dct_powertrain_config_default(pwr_dct_powertrain_config *config)
{
    if (config == NULL) {
        return;
    }
    *config = (pwr_dct_powertrain_config){0};
    pwr_si_engine_config_default(&config->engine);
    pwr_dct_config_default(&config->dct);
    pwr_pt_exhaust_parameters_default(&config->exhaust);

    config->initial_ambient_pressure_pa = 101325.0;
    config->initial_ambient_temperature_k = 298.15;
    config->initial_exhaust_wall_temperature_k = 298.15;
    config->halfshaft_inertia_kg_m2 = 4.0;
    config->rolling_resistance_torque_nm = 8.0;
    config->viscous_road_load_nm_s_per_rad = 0.08;
    config->quadratic_road_load_nm_s2_per_rad2 = 0.004;
    config->road_load_regularization_speed_rad_s = 0.5;

    config->starter_torque_nm = 55.0;
    config->starter_release_speed_rad_s = 82.0;
    config->maximum_cranking_time_s = 1.5;
    config->cranking_throttle_command = 0.20;
    config->idle_target_speed_rad_s = 92.0;
    config->idle_throttle_command = 0.10;
    config->idle_speed_throttle_gain_s_per_rad = 0.002;

    config->launch_target_engine_speed_rad_s = 135.0;
    config->launch_clamp_feedforward = 0.16;
    config->launch_clamp_speed_gain_s_per_rad = 0.003;
    config->launch_complete_halfshaft_speed_rad_s = 7.0;
    config->first_to_second_shift_halfshaft_speed_rad_s = 35.0;
    config->minimum_first_gear_time_s = 0.30;
    config->first_to_second_shift_duration_s = 0.25;
    config->shift_engine_throttle_scale = 0.82;

    config->maximum_exhaust_mass_mismatch_kg_per_s = 0.025;
    config->maximum_exhaust_enthalpy_mismatch_w = 25000.0;
    config->maximum_mechanical_coupling_mismatch_w = 25000.0;
}

pwr_dct_powertrain_result pwr_dct_powertrain_validate_config(
    const pwr_dct_powertrain_config *config)
{
    if (config == NULL) {
        return PWR_DCT_POWERTRAIN_INVALID_ARGUMENT;
    }
    if (config->engine.base_tick_ns != PWR_DCT_POWERTRAIN_BASE_TICK_NS
        || pwr_si_engine_validate_config(&config->engine)
            != PWR_SI_ENGINE_OK
        || pwr_dct_validate_config(&config->dct) != PWR_DCT_OK
        || pwr_exhaust_validate_parameters(&config->exhaust)
            != PWR_EXHAUST_OK
        || config->dct.maximum_timestep_s < pwr_dct_powertrain_tick_s) {
        return PWR_DCT_POWERTRAIN_INVALID_CONFIG;
    }
    if (!pwr_pt_positive_finite(config->initial_ambient_pressure_pa)
        || !pwr_pt_positive_finite(config->initial_ambient_temperature_k)
        || !pwr_pt_positive_finite(
            config->initial_exhaust_wall_temperature_k)
        || config->initial_ambient_temperature_k
            < config->exhaust.minimum_temperature_k
        || config->initial_exhaust_wall_temperature_k
            < config->exhaust.minimum_temperature_k
        || !pwr_pt_positive_finite(config->halfshaft_inertia_kg_m2)
        || !pwr_pt_nonnegative_finite(config->rolling_resistance_torque_nm)
        || !pwr_pt_nonnegative_finite(
            config->viscous_road_load_nm_s_per_rad)
        || !pwr_pt_nonnegative_finite(
            config->quadratic_road_load_nm_s2_per_rad2)
        || !pwr_pt_positive_finite(
            config->road_load_regularization_speed_rad_s)
        || !pwr_pt_nonnegative_finite(config->starter_torque_nm)
        || !pwr_pt_positive_finite(config->starter_release_speed_rad_s)
        || !pwr_pt_positive_finite(config->maximum_cranking_time_s)
        || !pwr_pt_nonnegative_finite(config->cranking_throttle_command)
        || config->cranking_throttle_command > 1.0
        || !pwr_pt_positive_finite(config->idle_target_speed_rad_s)
        || !pwr_pt_nonnegative_finite(config->idle_throttle_command)
        || config->idle_throttle_command > 1.0
        || !pwr_pt_nonnegative_finite(
            config->idle_speed_throttle_gain_s_per_rad)
        || !pwr_pt_positive_finite(
            config->launch_target_engine_speed_rad_s)
        || !pwr_pt_nonnegative_finite(config->launch_clamp_feedforward)
        || !pwr_pt_nonnegative_finite(
            config->launch_clamp_speed_gain_s_per_rad)
        || !pwr_pt_positive_finite(
            config->launch_complete_halfshaft_speed_rad_s)
        || !pwr_pt_positive_finite(
            config->first_to_second_shift_halfshaft_speed_rad_s)
        || config->first_to_second_shift_halfshaft_speed_rad_s
            <= config->launch_complete_halfshaft_speed_rad_s
        || !pwr_pt_nonnegative_finite(config->minimum_first_gear_time_s)
        || !pwr_pt_positive_finite(
            config->first_to_second_shift_duration_s)
        || !pwr_pt_positive_finite(config->shift_engine_throttle_scale)
        || config->shift_engine_throttle_scale > 1.0
        || !pwr_pt_nonnegative_finite(
            config->maximum_exhaust_mass_mismatch_kg_per_s)
        || !pwr_pt_nonnegative_finite(
            config->maximum_exhaust_enthalpy_mismatch_w)
        || !pwr_pt_nonnegative_finite(
            config->maximum_mechanical_coupling_mismatch_w)) {
        return PWR_DCT_POWERTRAIN_INVALID_CONFIG;
    }
    return PWR_DCT_POWERTRAIN_OK;
}

static bool pwr_pt_input_valid(const pwr_dct_powertrain_input *input)
{
    const uint32_t known_engine_faults
        = PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT
        | PWR_SI_ENGINE_FAULT_THROTTLE_STUCK
        | PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED
        | PWR_SI_ENGINE_FAULT_LOW_VOLTAGE;

    if (input == NULL
        || (input->control_mode != PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED
            && input->control_mode != PWR_DCT_POWERTRAIN_CONTROL_DIRECT)
        || !isfinite(input->driver_throttle)
        || input->driver_throttle < 0.0 || input->driver_throttle > 1.0
        || !pwr_pt_positive_finite(input->ambient_pressure_pa)
        || !pwr_pt_positive_finite(input->ambient_temperature_k)
        || !pwr_pt_nonnegative_finite(input->additional_road_load_torque_nm)
        || !pwr_pt_nonnegative_finite(input->tailpipe_area_scale)
        || (input->engine_fault_mask & ~known_engine_faults) != 0U) {
        return false;
    }
    if (input->control_mode == PWR_DCT_POWERTRAIN_CONTROL_DIRECT) {
        if (!pwr_pt_nonnegative_finite(input->direct_starter_torque_nm)) {
            return false;
        }
        for (size_t clutch = 0U; clutch < PWR_DCT_CLUTCH_COUNT; ++clutch) {
            if (!isfinite(input->direct_clutch_clamp[clutch])
                || input->direct_clutch_clamp[clutch] < 0.0
                || input->direct_clutch_clamp[clutch] > 1.0) {
                return false;
            }
        }
    }
    return true;
}

pwr_dct_powertrain_result pwr_dct_powertrain_init(
    pwr_dct_powertrain_state *state,
    const pwr_dct_powertrain_config *config)
{
    pwr_dct_powertrain_state candidate = {0};
    pwr_exhaust_reservoir ambient;

    if (state == NULL) {
        return PWR_DCT_POWERTRAIN_INVALID_ARGUMENT;
    }
    if (pwr_dct_powertrain_validate_config(config)
        != PWR_DCT_POWERTRAIN_OK) {
        return config == NULL ? PWR_DCT_POWERTRAIN_INVALID_ARGUMENT
                              : PWR_DCT_POWERTRAIN_INVALID_CONFIG;
    }

    candidate.config = *config;
    if (pwr_si_engine_init(&candidate.engine, &config->engine)
            != PWR_SI_ENGINE_OK
        || pwr_dct_init(&candidate.dct, &config->dct) != PWR_DCT_OK) {
        return PWR_DCT_POWERTRAIN_SUBMODEL_ERROR;
    }
    ambient = pwr_pt_ambient_reservoir(
        config->initial_ambient_pressure_pa,
        config->initial_ambient_temperature_k);
    if (pwr_exhaust_initialize_uniform(
            &config->exhaust,
            &ambient,
            config->initial_exhaust_wall_temperature_k,
            &candidate.exhaust)
        != PWR_EXHAUST_OK) {
        return PWR_DCT_POWERTRAIN_SUBMODEL_ERROR;
    }
    if (pwr_si_engine_observe(&candidate.engine, &candidate.engine_output)
            != PWR_SI_ENGINE_OK
        || pwr_dct_snapshot_read(&candidate.dct, &candidate.dct_output)
            != PWR_DCT_OK
        || pwr_exhaust_observe(
               &config->exhaust,
               &candidate.exhaust,
               &candidate.exhaust_output)
            != PWR_EXHAUST_OK) {
        return PWR_DCT_POWERTRAIN_SUBMODEL_ERROR;
    }
    candidate.last_control_mode = PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED;
    candidate.tcu_phase = PWR_DCT_POWERTRAIN_TCU_NEUTRAL;
    *state = candidate;
    return PWR_DCT_POWERTRAIN_OK;
}

static void pwr_pt_transition(
    pwr_dct_powertrain_state *state,
    pwr_dct_powertrain_tcu_phase phase)
{
    if (state->tcu_phase != phase) {
        state->tcu_phase = phase;
        state->tcu_phase_tick = UINT64_C(0);
    }
}

static double pwr_pt_phase_time_s(const pwr_dct_powertrain_state *state)
{
    return (double)state->tcu_phase_tick * pwr_dct_powertrain_tick_s;
}

static bool pwr_pt_clutches_open(const pwr_dct_powertrain_state *state)
{
    return state->dct.applied_clamp[(size_t)PWR_DCT_CLUTCH_K1]
            <= state->config.dct.maximum_dog_shift_clamp
        && state->dct.applied_clamp[(size_t)PWR_DCT_CLUTCH_K2]
            <= state->config.dct.maximum_dog_shift_clamp;
}

static void pwr_pt_supervisor_commands(
    pwr_dct_powertrain_state *state,
    const pwr_dct_powertrain_input *input,
    pwr_dct_powertrain_commands *commands,
    uint32_t *step_flags)
{
    const pwr_dct_powertrain_config *const config = &state->config;
    const double engine_speed = state->engine_output.crank_speed_rad_s;
    double idle_addition;

    memset(commands, 0, sizeof(*commands));
    commands->engine_throttle = input->driver_throttle;

    if (!input->ignition_on) {
        state->engine_started = false;
        state->cranking_ticks = UINT64_C(0);
    }
    else if (engine_speed >= config->starter_release_speed_rad_s) {
        state->engine_started = true;
    }

    if (input->ignition_on && !state->engine_started
        && (double)state->cranking_ticks * pwr_dct_powertrain_tick_s
            < config->maximum_cranking_time_s) {
        commands->starter_torque_nm = config->starter_torque_nm;
        commands->engine_throttle = pwr_pt_max(
            commands->engine_throttle,
            config->cranking_throttle_command);
        ++state->cranking_ticks;
    }

    idle_addition = config->idle_throttle_command
        + config->idle_speed_throttle_gain_s_per_rad
            * (config->idle_target_speed_rad_s - engine_speed);
    if (input->ignition_on && state->engine_started && engine_speed > 0.0) {
        commands->engine_throttle = pwr_pt_max(
            commands->engine_throttle,
            pwr_pt_clamp(idle_addition, 0.0, 1.0));
    }

    if (!input->ignition_on || !input->selector_drive) {
        if (pwr_pt_clutches_open(state)) {
            pwr_pt_transition(state, PWR_DCT_POWERTRAIN_TCU_NEUTRAL);
        }
        else {
            pwr_pt_transition(
                state, PWR_DCT_POWERTRAIN_TCU_RELEASE_TO_NEUTRAL);
        }
    }
    else {
        switch (state->tcu_phase) {
            case PWR_DCT_POWERTRAIN_TCU_NEUTRAL:
                pwr_pt_transition(
                    state, PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2);
                break;
            case PWR_DCT_POWERTRAIN_TCU_RELEASE_TO_NEUTRAL:
                if (pwr_pt_clutches_open(state)) {
                    pwr_pt_transition(
                        state, PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2);
                }
                break;
            case PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2:
                if (state->engine_started) {
                    pwr_pt_transition(
                        state, PWR_DCT_POWERTRAIN_TCU_LAUNCH_1);
                }
                break;
            case PWR_DCT_POWERTRAIN_TCU_LAUNCH_1:
                if (state->halfshaft_speed_rad_s
                    >= config->launch_complete_halfshaft_speed_rad_s) {
                    pwr_pt_transition(
                        state, PWR_DCT_POWERTRAIN_TCU_GEAR_1);
                }
                break;
            case PWR_DCT_POWERTRAIN_TCU_GEAR_1:
                if (state->halfshaft_speed_rad_s
                        >= config->first_to_second_shift_halfshaft_speed_rad_s
                    && pwr_pt_phase_time_s(state)
                        >= config->minimum_first_gear_time_s) {
                    pwr_pt_transition(
                        state, PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2);
                }
                break;
            case PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2:
                if (pwr_pt_phase_time_s(state)
                    >= config->first_to_second_shift_duration_s) {
                    pwr_pt_transition(
                        state, PWR_DCT_POWERTRAIN_TCU_GEAR_2);
                }
                break;
            case PWR_DCT_POWERTRAIN_TCU_GEAR_2:
            case PWR_DCT_POWERTRAIN_TCU_DIRECT:
            default:
                break;
        }
    }

    switch (state->tcu_phase) {
        case PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2:
            commands->gear_mask
                = PWR_DCT_GEAR_MASK_1 | PWR_DCT_GEAR_MASK_2;
            *step_flags |= PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE;
            break;
        case PWR_DCT_POWERTRAIN_TCU_LAUNCH_1: {
            const double positive_speed_error = pwr_pt_max(
                engine_speed - config->launch_target_engine_speed_rad_s,
                0.0);
            commands->gear_mask
                = PWR_DCT_GEAR_MASK_1 | PWR_DCT_GEAR_MASK_2;
            commands->clutch_clamp[(size_t)PWR_DCT_CLUTCH_K1]
                = pwr_pt_clamp(
                    config->launch_clamp_feedforward
                            * input->driver_throttle
                        + config->launch_clamp_speed_gain_s_per_rad
                            * positive_speed_error,
                    0.0,
                    1.0);
            *step_flags |= PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE;
            break;
        }
        case PWR_DCT_POWERTRAIN_TCU_GEAR_1:
            commands->gear_mask
                = PWR_DCT_GEAR_MASK_1 | PWR_DCT_GEAR_MASK_2;
            commands->clutch_clamp[(size_t)PWR_DCT_CLUTCH_K1] = 1.0;
            *step_flags |= PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE;
            break;
        case PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2: {
            const double progress = pwr_pt_clamp(
                pwr_pt_phase_time_s(state)
                    / config->first_to_second_shift_duration_s,
                0.0,
                1.0);
            commands->gear_mask
                = PWR_DCT_GEAR_MASK_1 | PWR_DCT_GEAR_MASK_2;
            commands->clutch_clamp[(size_t)PWR_DCT_CLUTCH_K1]
                = 1.0 - progress;
            commands->clutch_clamp[(size_t)PWR_DCT_CLUTCH_K2] = progress;
            commands->engine_throttle *= config->shift_engine_throttle_scale;
            *step_flags |= PWR_DCT_POWERTRAIN_DIAG_TCU_SHIFT_ACTIVE;
            break;
        }
        case PWR_DCT_POWERTRAIN_TCU_GEAR_2:
            commands->gear_mask
                = PWR_DCT_GEAR_MASK_1 | PWR_DCT_GEAR_MASK_2;
            commands->clutch_clamp[(size_t)PWR_DCT_CLUTCH_K2] = 1.0;
            *step_flags |= PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE;
            break;
        case PWR_DCT_POWERTRAIN_TCU_RELEASE_TO_NEUTRAL:
            /* Retain dog selections until both clutches are safe to move. */
            commands->gear_mask = state->dct.engaged_gear_mask;
            break;
        case PWR_DCT_POWERTRAIN_TCU_NEUTRAL:
        case PWR_DCT_POWERTRAIN_TCU_DIRECT:
        default:
            break;
    }
    commands->engine_throttle = pwr_pt_clamp(
        commands->engine_throttle, 0.0, 1.0);
}

static void pwr_pt_commands(
    pwr_dct_powertrain_state *state,
    const pwr_dct_powertrain_input *input,
    pwr_dct_powertrain_commands *commands,
    uint32_t *step_flags)
{
    if (input->control_mode == PWR_DCT_POWERTRAIN_CONTROL_DIRECT) {
        memset(commands, 0, sizeof(*commands));
        pwr_pt_transition(state, PWR_DCT_POWERTRAIN_TCU_DIRECT);
        commands->engine_throttle = input->driver_throttle;
        commands->starter_torque_nm = input->direct_starter_torque_nm;
        commands->gear_mask = input->direct_gear_mask;
        for (size_t clutch = 0U; clutch < PWR_DCT_CLUTCH_COUNT; ++clutch) {
            commands->clutch_clamp[clutch]
                = input->direct_clutch_clamp[clutch];
        }
    }
    else {
        if (state->last_control_mode == PWR_DCT_POWERTRAIN_CONTROL_DIRECT) {
            pwr_pt_transition(
                state, PWR_DCT_POWERTRAIN_TCU_RELEASE_TO_NEUTRAL);
        }
        pwr_pt_supervisor_commands(state, input, commands, step_flags);
    }
    state->last_control_mode = input->control_mode;
}

static pwr_exhaust_reservoir pwr_pt_engine_exhaust_reservoir(
    const pwr_si_engine_output *engine,
    double minimum_pressure_pa,
    double minimum_temperature_k)
{
    pwr_exhaust_reservoir reservoir = {
        .pressure_pa = pwr_pt_max(
            engine->exhaust_source_pressure_pa, minimum_pressure_pa),
        .temperature_k = pwr_pt_max(
            engine->exhaust_temperature_k, minimum_temperature_k),
    };

    for (size_t species = 0U; species < PWR_EXHAUST_SPECIES_COUNT;
         ++species) {
        reservoir.species_mass_fraction[species]
            = engine->exhaust_species_mass_fraction[species];
    }
    return reservoir;
}

static double pwr_pt_source_cp(
    const pwr_exhaust_parameters *parameters,
    const pwr_si_engine_output *engine)
{
    double cp = 0.0;
    for (size_t species = 0U; species < PWR_EXHAUST_SPECIES_COUNT;
         ++species) {
        cp += engine->exhaust_species_mass_fraction[species]
            * (parameters->species_cv_j_per_kg_k[species]
               + parameters->species_gas_constant_j_per_kg_k[species]);
    }
    return cp;
}

static double pwr_pt_road_load(
    const pwr_dct_powertrain_config *config,
    const pwr_dct_powertrain_input *input,
    double speed_rad_s)
{
    const double sign = tanh(
        speed_rad_s / config->road_load_regularization_speed_rad_s);
    return config->rolling_resistance_torque_nm * sign
        + config->viscous_road_load_nm_s_per_rad * speed_rad_s
        + config->quadratic_road_load_nm_s2_per_rad2
            * speed_rad_s * fabs(speed_rad_s)
        + input->additional_road_load_torque_nm * sign;
}

static bool pwr_pt_state_finite(const pwr_dct_powertrain_state *state)
{
    return pwr_pt_nonnegative_finite(state->halfshaft_speed_rad_s)
        && isfinite(state->halfshaft_angle_rad)
        && isfinite(state->cumulative_halfshaft_drive_work_j)
        && isfinite(state->cumulative_road_load_work_j)
        && isfinite(state->cumulative_halfshaft_numerical_adjustment_j)
        && isfinite(state->cumulative_exhaust_mass_mismatch_kg)
        && isfinite(state->cumulative_exhaust_enthalpy_mismatch_j)
        && isfinite(state->cumulative_engine_dct_energy_mismatch_j)
        && isfinite(state->cumulative_dct_output_energy_mismatch_j);
}

pwr_dct_powertrain_result pwr_dct_powertrain_step(
    pwr_dct_powertrain_state *state,
    const pwr_dct_powertrain_input *input,
    pwr_dct_powertrain_output *output)
{
    pwr_dct_powertrain_state candidate;
    pwr_dct_powertrain_commands commands;
    pwr_exhaust_inputs exhaust_input;
    pwr_si_engine_input engine_input;
    pwr_dct_inputs dct_input;
    pwr_si_engine_output next_engine_output;
    pwr_dct_snapshot next_dct_output;
    pwr_exhaust_diagnostics next_exhaust_step;
    const double dt = pwr_dct_powertrain_tick_s;
    double engine_load_work_before;
    double dct_input_energy_before;
    double dct_output_energy_before;
    double old_halfshaft_speed;
    double next_halfshaft_speed;
    double average_halfshaft_speed;
    double drive_work;
    double road_work;
    double kinetic_change;
    double halfshaft_adjustment;
    double source_mass_flow;
    double exhaust_inlet_mass_flow;
    double source_enthalpy_flow;
    double accepted_enthalpy_flow;
    uint32_t step_flags = PWR_DCT_POWERTRAIN_DIAG_NONE;

    if (state == NULL || output == NULL) {
        return PWR_DCT_POWERTRAIN_INVALID_ARGUMENT;
    }
    if (pwr_dct_powertrain_validate_config(&state->config)
        != PWR_DCT_POWERTRAIN_OK) {
        return PWR_DCT_POWERTRAIN_INVALID_CONFIG;
    }
    if (!pwr_pt_input_valid(input)
        || input->ambient_temperature_k
            < state->config.exhaust.minimum_temperature_k) {
        return PWR_DCT_POWERTRAIN_INVALID_INPUT;
    }

    candidate = *state;
    pwr_pt_commands(&candidate, input, &commands, &step_flags);

    /* 1. Exhaust consumes the previously committed engine source state. */
    exhaust_input = (pwr_exhaust_inputs){
        .upstream = pwr_pt_engine_exhaust_reservoir(
            &candidate.engine_output,
            input->ambient_pressure_pa,
            state->config.exhaust.minimum_temperature_k),
        .downstream = pwr_pt_ambient_reservoir(
            input->ambient_pressure_pa, input->ambient_temperature_k),
        .inlet_area_scale = 1.0,
        .tailpipe_area_scale = input->tailpipe_area_scale,
        .ambient_temperature_k = input->ambient_temperature_k,
    };
    if (pwr_exhaust_step(
            &candidate.config.exhaust,
            &candidate.exhaust,
            &exhaust_input,
            dt,
            &next_exhaust_step)
        != PWR_EXHAUST_OK) {
        return PWR_DCT_POWERTRAIN_SUBMODEL_ERROR;
    }
    candidate.exhaust_step = next_exhaust_step;
    candidate.exhaust_output = next_exhaust_step.final_observation;

    /* 2. Engine sees new network backpressure and the previous DCT effort. */
    engine_load_work_before = candidate.engine_output.mechanical_load_work_j;
    engine_input = (pwr_si_engine_input){
        .driver_throttle = commands.engine_throttle,
        .load_torque_nm = candidate.dct_output.input_transmitted_torque_nm,
        .starter_torque_nm = commands.starter_torque_nm,
        .exhaust_backpressure_pa = pwr_pt_max(
            candidate.exhaust_output.pressure_pa[
                PWR_EXHAUST_VOLUME_MANIFOLD],
            1.0),
        .ambient_pressure_pa = input->ambient_pressure_pa,
        .ambient_temperature_k = input->ambient_temperature_k,
        .fault_mask = input->engine_fault_mask,
        .ignition_on = input->ignition_on,
    };
    if (pwr_si_engine_step(
            &candidate.engine, &engine_input, &next_engine_output)
        != PWR_SI_ENGINE_OK) {
        return PWR_DCT_POWERTRAIN_SUBMODEL_ERROR;
    }
    candidate.engine_output = next_engine_output;

    /* 3. DCT sees the new crank speed but the committed halfshaft speed. */
    dct_input_energy_before = candidate.dct_output.cumulative_input_energy_j;
    dct_output_energy_before = candidate.dct_output.cumulative_output_energy_j;
    dct_input = (pwr_dct_inputs){
        .engine_speed_rad_s = candidate.engine_output.crank_speed_rad_s,
        .output_speed_rad_s = candidate.halfshaft_speed_rad_s,
        .engaged_gear_mask = commands.gear_mask,
        .clutch_clamp_command = {
            commands.clutch_clamp[(size_t)PWR_DCT_CLUTCH_K1],
            commands.clutch_clamp[(size_t)PWR_DCT_CLUTCH_K2],
        },
        .ambient_temperature_k = input->ambient_temperature_k,
    };
    if (pwr_dct_step(&candidate.dct, &dct_input, dt) != PWR_DCT_OK
        || pwr_dct_snapshot_read(&candidate.dct, &next_dct_output)
            != PWR_DCT_OK) {
        return PWR_DCT_POWERTRAIN_SUBMODEL_ERROR;
    }
    candidate.dct_output = next_dct_output;

    /* 4. Integrate terminal inertia from new DCT torque and old speed. */
    old_halfshaft_speed = candidate.halfshaft_speed_rad_s;
    candidate.last_road_load_torque_nm = pwr_pt_road_load(
        &candidate.config, input, old_halfshaft_speed);
    next_halfshaft_speed = old_halfshaft_speed
        + (candidate.dct_output.output_torque_nm
           - candidate.last_road_load_torque_nm)
            / candidate.config.halfshaft_inertia_kg_m2
            * dt;
    if (next_halfshaft_speed < 0.0) {
        next_halfshaft_speed = 0.0;
        step_flags |= PWR_DCT_POWERTRAIN_DIAG_HALFSHAFT_SPEED_CLAMP;
    }
    average_halfshaft_speed
        = 0.5 * (old_halfshaft_speed + next_halfshaft_speed);
    drive_work = candidate.dct_output.output_torque_nm
        * average_halfshaft_speed * dt;
    road_work = candidate.last_road_load_torque_nm
        * average_halfshaft_speed * dt;
    kinetic_change = 0.5 * candidate.config.halfshaft_inertia_kg_m2
        * (next_halfshaft_speed * next_halfshaft_speed
           - old_halfshaft_speed * old_halfshaft_speed);
    halfshaft_adjustment = drive_work - road_work - kinetic_change;
    candidate.halfshaft_speed_rad_s = next_halfshaft_speed;
    candidate.halfshaft_angle_rad += average_halfshaft_speed * dt;
    candidate.cumulative_halfshaft_drive_work_j += drive_work;
    candidate.cumulative_road_load_work_j += road_work;
    candidate.cumulative_halfshaft_numerical_adjustment_j
        += halfshaft_adjustment;

    /* 5. Audit the explicit partition interfaces; these are not corrections. */
    source_mass_flow = state->engine_output.exhaust_mass_flow_kg_per_s;
    exhaust_inlet_mass_flow
        = next_exhaust_step.average_path_mass_flow_kg_per_s[
            PWR_EXHAUST_PATH_INLET];
    candidate.last_exhaust_mass_mismatch_kg
        = (source_mass_flow - exhaust_inlet_mass_flow) * dt;
    candidate.cumulative_exhaust_mass_mismatch_kg
        += candidate.last_exhaust_mass_mismatch_kg;
    source_enthalpy_flow = source_mass_flow
        * pwr_pt_source_cp(&candidate.config.exhaust, &state->engine_output)
        * exhaust_input.upstream.temperature_k;
    accepted_enthalpy_flow = exhaust_inlet_mass_flow
        * pwr_pt_source_cp(&candidate.config.exhaust, &state->engine_output)
        * exhaust_input.upstream.temperature_k;
    candidate.last_exhaust_enthalpy_mismatch_j
        = (source_enthalpy_flow - accepted_enthalpy_flow) * dt;
    candidate.cumulative_exhaust_enthalpy_mismatch_j
        += candidate.last_exhaust_enthalpy_mismatch_j;

    candidate.last_engine_dct_energy_mismatch_j
        = (candidate.engine_output.mechanical_load_work_j
           - engine_load_work_before)
        - (candidate.dct_output.cumulative_input_energy_j
           - dct_input_energy_before);
    candidate.cumulative_engine_dct_energy_mismatch_j
        += candidate.last_engine_dct_energy_mismatch_j;
    candidate.last_dct_output_energy_mismatch_j
        = (candidate.dct_output.cumulative_output_energy_j
           - dct_output_energy_before)
        - drive_work;
    candidate.cumulative_dct_output_energy_mismatch_j
        += candidate.last_dct_output_energy_mismatch_j;

    if (fabs(candidate.last_exhaust_mass_mismatch_kg) / dt
        > candidate.config.maximum_exhaust_mass_mismatch_kg_per_s) {
        step_flags |= PWR_DCT_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH;
    }
    if (fabs(candidate.last_exhaust_enthalpy_mismatch_j) / dt
        > candidate.config.maximum_exhaust_enthalpy_mismatch_w) {
        step_flags |= PWR_DCT_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH;
    }
    if (fabs(candidate.last_engine_dct_energy_mismatch_j) / dt
        > candidate.config.maximum_mechanical_coupling_mismatch_w) {
        step_flags |= PWR_DCT_POWERTRAIN_DIAG_ENGINE_DCT_ENERGY_MISMATCH;
    }
    if (fabs(candidate.last_dct_output_energy_mismatch_j) / dt
        > candidate.config.maximum_mechanical_coupling_mismatch_w) {
        step_flags |= PWR_DCT_POWERTRAIN_DIAG_DCT_OUTPUT_ENERGY_MISMATCH;
    }

    candidate.last_commanded_engine_throttle = commands.engine_throttle;
    candidate.last_commanded_starter_torque_nm = commands.starter_torque_nm;
    candidate.last_commanded_gear_mask = commands.gear_mask;
    for (size_t clutch = 0U; clutch < PWR_DCT_CLUTCH_COUNT; ++clutch) {
        candidate.last_commanded_clutch_clamp[clutch]
            = commands.clutch_clamp[clutch];
    }
    candidate.last_step_diagnostic_flags = step_flags;
    candidate.diagnostic_flags |= step_flags;
    ++candidate.tick;
    ++candidate.tcu_phase_tick;

    if (!pwr_pt_state_finite(&candidate)) {
        return PWR_DCT_POWERTRAIN_NUMERIC_ERROR;
    }
    if (pwr_dct_powertrain_observe(&candidate, output)
        != PWR_DCT_POWERTRAIN_OK) {
        return PWR_DCT_POWERTRAIN_NUMERIC_ERROR;
    }
    *state = candidate;
    return PWR_DCT_POWERTRAIN_OK;
}

static double pwr_pt_shift_progress(
    const pwr_dct_powertrain_state *state)
{
    if (state->tcu_phase == PWR_DCT_POWERTRAIN_TCU_GEAR_2) {
        return 1.0;
    }
    if (state->tcu_phase != PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2) {
        return 0.0;
    }
    return pwr_pt_clamp(
        pwr_pt_phase_time_s(state)
            / state->config.first_to_second_shift_duration_s,
        0.0,
        1.0);
}

pwr_dct_powertrain_result pwr_dct_powertrain_observe(
    const pwr_dct_powertrain_state *state,
    pwr_dct_powertrain_output *output)
{
    double kinetic;
    double kinetic_change;

    if (state == NULL || output == NULL) {
        return PWR_DCT_POWERTRAIN_INVALID_ARGUMENT;
    }
    if (pwr_dct_powertrain_validate_config(&state->config)
        != PWR_DCT_POWERTRAIN_OK) {
        return PWR_DCT_POWERTRAIN_INVALID_CONFIG;
    }

    kinetic = 0.5 * state->config.halfshaft_inertia_kg_m2
        * state->halfshaft_speed_rad_s * state->halfshaft_speed_rad_s;
    kinetic_change = kinetic - state->initial_halfshaft_kinetic_energy_j;
    *output = (pwr_dct_powertrain_output){
        .time_s = (double)state->tick * pwr_dct_powertrain_tick_s,
        .tick = state->tick,
        .control_mode = state->last_control_mode,
        .tcu_phase = state->tcu_phase,
        .shift_progress = pwr_pt_shift_progress(state),
        .commanded_engine_throttle = state->last_commanded_engine_throttle,
        .commanded_starter_torque_nm
            = state->last_commanded_starter_torque_nm,
        .commanded_gear_mask = state->last_commanded_gear_mask,
        .engine = state->engine_output,
        .dct = state->dct_output,
        .exhaust = state->exhaust_output,
        .exhaust_step = state->exhaust_step,
        .halfshaft_angle_rad = state->halfshaft_angle_rad,
        .halfshaft_speed_rad_s = state->halfshaft_speed_rad_s,
        .halfshaft_drive_torque_nm = state->dct_output.output_torque_nm,
        .road_load_torque_nm = state->last_road_load_torque_nm,
        .halfshaft_kinetic_energy_j = kinetic,
        .cumulative_halfshaft_drive_work_j
            = state->cumulative_halfshaft_drive_work_j,
        .cumulative_road_load_work_j = state->cumulative_road_load_work_j,
        .cumulative_halfshaft_numerical_adjustment_j
            = state->cumulative_halfshaft_numerical_adjustment_j,
        .halfshaft_energy_residual_j
            = state->cumulative_halfshaft_drive_work_j
            - state->cumulative_road_load_work_j - kinetic_change
            - state->cumulative_halfshaft_numerical_adjustment_j,
        .exhaust_mass_mismatch_kg = state->last_exhaust_mass_mismatch_kg,
        .cumulative_exhaust_mass_mismatch_kg
            = state->cumulative_exhaust_mass_mismatch_kg,
        .exhaust_enthalpy_mismatch_j
            = state->last_exhaust_enthalpy_mismatch_j,
        .cumulative_exhaust_enthalpy_mismatch_j
            = state->cumulative_exhaust_enthalpy_mismatch_j,
        .engine_dct_energy_mismatch_j
            = state->last_engine_dct_energy_mismatch_j,
        .cumulative_engine_dct_energy_mismatch_j
            = state->cumulative_engine_dct_energy_mismatch_j,
        .dct_output_energy_mismatch_j
            = state->last_dct_output_energy_mismatch_j,
        .cumulative_dct_output_energy_mismatch_j
            = state->cumulative_dct_output_energy_mismatch_j,
        .last_step_diagnostic_flags = state->last_step_diagnostic_flags,
        .diagnostic_flags = state->diagnostic_flags,
    };
    for (size_t clutch = 0U; clutch < PWR_DCT_CLUTCH_COUNT; ++clutch) {
        output->commanded_clutch_clamp[clutch]
            = state->last_commanded_clutch_clamp[clutch];
    }
    output->state_hash = pwr_dct_powertrain_state_hash(state);
    return PWR_DCT_POWERTRAIN_OK;
}

static uint64_t pwr_pt_hash_u64(uint64_t hash, uint64_t value)
{
    for (unsigned int byte = 0U; byte < 8U; ++byte) {
        hash ^= (value >> (byte * 8U)) & UINT64_C(0xff);
        hash *= UINT64_C(1099511628211);
    }
    return hash;
}

static uint64_t pwr_pt_hash_double(uint64_t hash, double value)
{
    uint64_t bits = UINT64_C(0);
    static_assert(sizeof(bits) == sizeof(value), "double must be 64-bit");
    memcpy(&bits, &value, sizeof(bits));
    return pwr_pt_hash_u64(hash, bits);
}

uint64_t pwr_dct_powertrain_state_hash(
    const pwr_dct_powertrain_state *state)
{
    uint64_t hash = UINT64_C(14695981039346656037);

    if (state == NULL) {
        return UINT64_C(0);
    }
    hash = pwr_pt_hash_u64(hash, pwr_si_engine_state_hash(&state->engine));
    hash = pwr_pt_hash_u64(hash, pwr_dct_state_hash(&state->dct));
    for (size_t volume = 0U; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        hash = pwr_pt_hash_double(
            hash, state->exhaust.volume[volume].total_mass_kg);
        for (size_t species = 0U; species < PWR_EXHAUST_SPECIES_COUNT;
             ++species) {
            hash = pwr_pt_hash_double(
                hash,
                state->exhaust.volume[volume].species_mass_kg[species]);
        }
        hash = pwr_pt_hash_double(
            hash, state->exhaust.volume[volume].internal_energy_j);
        hash = pwr_pt_hash_double(
            hash, state->exhaust.volume[volume].wall_temperature_k);
    }
    hash = pwr_pt_hash_double(
        hash, state->exhaust.catalyst_conversion_fraction);
    for (size_t species = 0U; species < PWR_EXHAUST_SPECIES_COUNT;
         ++species) {
        hash = pwr_pt_hash_double(
            hash, state->exhaust.cumulative_tailpipe_species_kg[species]);
    }
    hash = pwr_pt_hash_double(hash, state->exhaust.time_s);
    hash = pwr_pt_hash_u64(hash, state->tick);
    hash = pwr_pt_hash_u64(hash, state->tcu_phase_tick);
    hash = pwr_pt_hash_u64(hash, state->cranking_ticks);
    hash = pwr_pt_hash_u64(hash, (uint64_t)state->last_control_mode);
    hash = pwr_pt_hash_u64(hash, (uint64_t)state->tcu_phase);
    hash = pwr_pt_hash_u64(hash, state->engine_started ? 1U : 0U);
    hash = pwr_pt_hash_double(hash, state->halfshaft_angle_rad);
    hash = pwr_pt_hash_double(hash, state->halfshaft_speed_rad_s);
    hash = pwr_pt_hash_double(
        hash, state->cumulative_halfshaft_drive_work_j);
    hash = pwr_pt_hash_double(hash, state->cumulative_road_load_work_j);
    hash = pwr_pt_hash_double(
        hash, state->cumulative_halfshaft_numerical_adjustment_j);
    hash = pwr_pt_hash_double(
        hash, state->cumulative_exhaust_mass_mismatch_kg);
    hash = pwr_pt_hash_double(
        hash, state->cumulative_exhaust_enthalpy_mismatch_j);
    hash = pwr_pt_hash_double(
        hash, state->cumulative_engine_dct_energy_mismatch_j);
    hash = pwr_pt_hash_double(
        hash, state->cumulative_dct_output_energy_mismatch_j);
    hash = pwr_pt_hash_u64(hash, state->last_step_diagnostic_flags);
    hash = pwr_pt_hash_u64(hash, state->diagnostic_flags);
    return hash;
}

const char *pwr_dct_powertrain_result_string(
    pwr_dct_powertrain_result result)
{
    switch (result) {
        case PWR_DCT_POWERTRAIN_OK:
            return "ok";
        case PWR_DCT_POWERTRAIN_INVALID_ARGUMENT:
            return "invalid argument";
        case PWR_DCT_POWERTRAIN_INVALID_CONFIG:
            return "invalid config";
        case PWR_DCT_POWERTRAIN_INVALID_INPUT:
            return "invalid input";
        case PWR_DCT_POWERTRAIN_SUBMODEL_ERROR:
            return "submodel error";
        case PWR_DCT_POWERTRAIN_NUMERIC_ERROR:
            return "numeric error";
        default:
            return "unknown result";
    }
}
