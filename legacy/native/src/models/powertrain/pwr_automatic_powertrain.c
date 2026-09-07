// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#include "pwr_automatic_powertrain.h"

#include <math.h>
#include <stddef.h>
#include <string.h>

static const double pwr_automatic_powertrain_tick_s = 1.0e-4;

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

typedef struct pwr_automatic_powertrain_commands {
    double engine_throttle;
    double starter_torque_nm;
    double line_pressure_pa;
    double lockup;
    pwr_automatic_gear gear;
} pwr_automatic_powertrain_commands;

static bool pwr_apt_positive_finite(double value)
{
    return isfinite(value) && value > 0.0;
}

static bool pwr_apt_nonnegative_finite(double value)
{
    return isfinite(value) && value >= 0.0;
}

static double pwr_apt_min(double left, double right)
{
    return left < right ? left : right;
}

static double pwr_apt_max(double left, double right)
{
    return left > right ? left : right;
}

static double pwr_apt_clamp(double value, double minimum, double maximum)
{
    return pwr_apt_min(pwr_apt_max(value, minimum), maximum);
}

static pwr_exhaust_reservoir pwr_apt_ambient_reservoir(
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

static void pwr_apt_exhaust_parameters_default(
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
    parameters->maximum_substep_s = pwr_automatic_powertrain_tick_s;
    parameters->maximum_outflow_fraction_per_substep = 0.10;
    parameters->maximum_substeps = UINT32_C(8);
}

void pwr_automatic_powertrain_config_default(
    pwr_automatic_powertrain_config *config)
{
    if (config == NULL) {
        return;
    }
    *config = (pwr_automatic_powertrain_config){0};
    pwr_si_engine_config_default(&config->engine);
    pwr_automatic_config_default(&config->automatic);
    pwr_apt_exhaust_parameters_default(&config->exhaust);

    /* Every value below, including the automatic ratios, is synthetic. */
    config->initial_ambient_pressure_pa = 101325.0;
    config->initial_ambient_temperature_k = 298.15;
    config->initial_exhaust_wall_temperature_k = 298.15;

    config->electrical_nominal_voltage_v = 13.6;
    config->electrical_internal_resistance_ohm = 0.025;
    config->base_accessory_current_a = 4.0;
    config->tcu_current_a = 2.0;
    config->solenoid_current_at_max_pressure_a = 7.0;
    config->starter_current_per_nm_a = 0.85;
    config->ecu_brownout_voltage_v = 8.5;
    config->tcu_brownout_voltage_v = 9.0;

    config->starter_torque_nm = 62.0;
    config->starter_release_speed_rad_s = 82.0;
    config->maximum_cranking_time_s = 1.8;
    config->cranking_throttle_command = 0.20;
    config->idle_target_speed_rad_s = 92.0;
    config->idle_throttle_command = 0.10;
    config->idle_speed_throttle_gain_s_per_rad = 0.002;

    config->line_pressure_throttle_gain_pa = 250000.0;
    config->first_to_second_halfshaft_speed_rad_s = 12.0;
    config->minimum_first_gear_time_s = 0.40;
    config->lockup_enable_halfshaft_speed_rad_s = 18.0;
    config->shift_engine_throttle_scale = 0.84;

    config->final_drive_ratio = 3.40;
    config->final_drive_efficiency = 0.96;
    config->left_halfshaft_inertia_kg_m2 = 1.6;
    config->right_halfshaft_inertia_kg_m2 = 1.6;
    config->rolling_resistance_torque_nm_per_side = 4.0;
    config->viscous_road_load_nm_s_per_rad_per_side = 0.04;
    config->quadratic_road_load_nm_s2_per_rad2_per_side = 0.002;
    config->road_load_regularization_speed_rad_s = 0.5;

    config->maximum_exhaust_mass_mismatch_kg_per_s = 0.025;
    config->maximum_exhaust_enthalpy_mismatch_w = 25000.0;
    config->maximum_mechanical_coupling_mismatch_w = 25000.0;
    config->maximum_speed_projection_rad_s = 0.5;
}

pwr_automatic_powertrain_result pwr_automatic_powertrain_validate_config(
    const pwr_automatic_powertrain_config *config)
{
    pwr_automatic automatic_probe;

    if (config == NULL) {
        return PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT;
    }
    if (config->engine.base_tick_ns != PWR_AUTOMATIC_POWERTRAIN_BASE_TICK_NS
        || config->automatic.step_ns != PWR_AUTOMATIC_POWERTRAIN_BASE_TICK_NS
        || pwr_si_engine_validate_config(&config->engine)
            != PWR_SI_ENGINE_OK
        || pwr_automatic_init(&automatic_probe, &config->automatic)
            != PWR_AUTOMATIC_OK
        || pwr_exhaust_validate_parameters(&config->exhaust)
            != PWR_EXHAUST_OK) {
        return PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG;
    }
    if (!pwr_apt_positive_finite(config->initial_ambient_pressure_pa)
        || !pwr_apt_positive_finite(config->initial_ambient_temperature_k)
        || !pwr_apt_positive_finite(
            config->initial_exhaust_wall_temperature_k)
        || config->initial_ambient_temperature_k
            < config->exhaust.minimum_temperature_k
        || config->initial_exhaust_wall_temperature_k
            < config->exhaust.minimum_temperature_k
        || !pwr_apt_positive_finite(config->electrical_nominal_voltage_v)
        || !pwr_apt_nonnegative_finite(
            config->electrical_internal_resistance_ohm)
        || !pwr_apt_nonnegative_finite(config->base_accessory_current_a)
        || !pwr_apt_nonnegative_finite(config->tcu_current_a)
        || !pwr_apt_nonnegative_finite(
            config->solenoid_current_at_max_pressure_a)
        || !pwr_apt_nonnegative_finite(config->starter_current_per_nm_a)
        || !pwr_apt_positive_finite(config->ecu_brownout_voltage_v)
        || !pwr_apt_positive_finite(config->tcu_brownout_voltage_v)
        || config->ecu_brownout_voltage_v
            >= config->electrical_nominal_voltage_v
        || config->tcu_brownout_voltage_v
            >= config->electrical_nominal_voltage_v
        || !pwr_apt_nonnegative_finite(config->starter_torque_nm)
        || !pwr_apt_positive_finite(config->starter_release_speed_rad_s)
        || !pwr_apt_positive_finite(config->maximum_cranking_time_s)
        || !pwr_apt_nonnegative_finite(config->cranking_throttle_command)
        || config->cranking_throttle_command > 1.0
        || !pwr_apt_positive_finite(config->idle_target_speed_rad_s)
        || !pwr_apt_nonnegative_finite(config->idle_throttle_command)
        || config->idle_throttle_command > 1.0
        || !pwr_apt_nonnegative_finite(
            config->idle_speed_throttle_gain_s_per_rad)
        || !pwr_apt_nonnegative_finite(
            config->line_pressure_throttle_gain_pa)
        || !pwr_apt_positive_finite(
            config->first_to_second_halfshaft_speed_rad_s)
        || !pwr_apt_nonnegative_finite(config->minimum_first_gear_time_s)
        || !pwr_apt_positive_finite(
            config->lockup_enable_halfshaft_speed_rad_s)
        || config->lockup_enable_halfshaft_speed_rad_s
            <= config->first_to_second_halfshaft_speed_rad_s
        || !pwr_apt_positive_finite(config->shift_engine_throttle_scale)
        || config->shift_engine_throttle_scale > 1.0
        || !pwr_apt_positive_finite(config->final_drive_ratio)
        || !pwr_apt_positive_finite(config->final_drive_efficiency)
        || config->final_drive_efficiency > 1.0
        || !pwr_apt_positive_finite(config->left_halfshaft_inertia_kg_m2)
        || !pwr_apt_positive_finite(config->right_halfshaft_inertia_kg_m2)
        || !pwr_apt_nonnegative_finite(
            config->rolling_resistance_torque_nm_per_side)
        || !pwr_apt_nonnegative_finite(
            config->viscous_road_load_nm_s_per_rad_per_side)
        || !pwr_apt_nonnegative_finite(
            config->quadratic_road_load_nm_s2_per_rad2_per_side)
        || !pwr_apt_positive_finite(
            config->road_load_regularization_speed_rad_s)
        || !pwr_apt_nonnegative_finite(
            config->maximum_exhaust_mass_mismatch_kg_per_s)
        || !pwr_apt_nonnegative_finite(
            config->maximum_exhaust_enthalpy_mismatch_w)
        || !pwr_apt_nonnegative_finite(
            config->maximum_mechanical_coupling_mismatch_w)
        || !pwr_apt_nonnegative_finite(
            config->maximum_speed_projection_rad_s)) {
        return PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG;
    }
    return PWR_AUTOMATIC_POWERTRAIN_OK;
}

static bool pwr_apt_gear_valid(pwr_automatic_gear gear)
{
    return gear == PWR_AUTOMATIC_GEAR_REVERSE
        || gear == PWR_AUTOMATIC_GEAR_NEUTRAL
        || gear == PWR_AUTOMATIC_GEAR_FIRST
        || gear == PWR_AUTOMATIC_GEAR_SECOND
        || gear == PWR_AUTOMATIC_GEAR_THIRD
        || gear == PWR_AUTOMATIC_GEAR_FOURTH;
}

static bool pwr_apt_input_valid(
    const pwr_automatic_powertrain_config *config,
    const pwr_automatic_powertrain_input *input)
{
    const uint32_t known_engine_faults
        = PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT
        | PWR_SI_ENGINE_FAULT_THROTTLE_STUCK
        | PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED
        | PWR_SI_ENGINE_FAULT_LOW_VOLTAGE;
    const double maximum_axle_load = config->automatic.maximum_external_torque_nm
        * config->final_drive_ratio * config->final_drive_efficiency;

    if (input == NULL
        || (input->control_mode
                != PWR_AUTOMATIC_POWERTRAIN_CONTROL_SUPERVISED
            && input->control_mode
                != PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT)
        || !isfinite(input->driver_throttle)
        || input->driver_throttle < 0.0 || input->driver_throttle > 1.0
        || !pwr_apt_positive_finite(input->ambient_pressure_pa)
        || !pwr_apt_positive_finite(input->ambient_temperature_k)
        || !pwr_apt_nonnegative_finite(input->electrical_supply_voltage_v)
        || !pwr_apt_nonnegative_finite(
            input->left_additional_road_load_torque_nm)
        || !pwr_apt_nonnegative_finite(
            input->right_additional_road_load_torque_nm)
        || input->left_additional_road_load_torque_nm
                + input->right_additional_road_load_torque_nm
            > maximum_axle_load
        || !pwr_apt_nonnegative_finite(input->tailpipe_area_scale)
        || (input->engine_fault_mask & ~known_engine_faults) != 0U) {
        return false;
    }
    if (input->control_mode == PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT
        && (!pwr_apt_nonnegative_finite(input->direct_starter_torque_nm)
            || !pwr_apt_nonnegative_finite(
                input->direct_line_pressure_command_pa)
            || input->direct_line_pressure_command_pa
                > config->automatic.maximum_line_pressure_pa
            || !isfinite(input->direct_lockup_command)
            || input->direct_lockup_command < 0.0
            || input->direct_lockup_command > 1.0
            || !pwr_apt_gear_valid(input->direct_requested_gear))) {
        return false;
    }
    return true;
}

pwr_automatic_powertrain_result pwr_automatic_powertrain_init(
    pwr_automatic_powertrain_state *state,
    const pwr_automatic_powertrain_config *config)
{
    pwr_automatic_powertrain_state candidate = {0};
    pwr_exhaust_reservoir ambient;

    if (state == NULL) {
        return PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT;
    }
    if (pwr_automatic_powertrain_validate_config(config)
        != PWR_AUTOMATIC_POWERTRAIN_OK) {
        return config == NULL ? PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT
                              : PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG;
    }

    candidate.config = *config;
    if (pwr_si_engine_init(&candidate.engine, &config->engine)
            != PWR_SI_ENGINE_OK
        || pwr_automatic_init(&candidate.automatic, &config->automatic)
            != PWR_AUTOMATIC_OK) {
        return PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR;
    }
    ambient = pwr_apt_ambient_reservoir(
        config->initial_ambient_pressure_pa,
        config->initial_ambient_temperature_k);
    if (pwr_exhaust_initialize_uniform(
            &config->exhaust,
            &ambient,
            config->initial_exhaust_wall_temperature_k,
            &candidate.exhaust)
        != PWR_EXHAUST_OK) {
        return PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR;
    }
    if (pwr_si_engine_observe(&candidate.engine, &candidate.engine_output)
            != PWR_SI_ENGINE_OK
        || pwr_automatic_snapshot_read(
               &candidate.automatic, &candidate.automatic_output)
            != PWR_AUTOMATIC_OK
        || pwr_exhaust_observe(
               &config->exhaust,
               &candidate.exhaust,
               &candidate.exhaust_output)
            != PWR_EXHAUST_OK) {
        return PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR;
    }
    candidate.last_control_mode
        = PWR_AUTOMATIC_POWERTRAIN_CONTROL_SUPERVISED;
    candidate.tcu_phase = PWR_AUTOMATIC_POWERTRAIN_TCU_NEUTRAL;
    candidate.supervised_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
    candidate.low_voltage_v = config->electrical_nominal_voltage_v;
    *state = candidate;
    return PWR_AUTOMATIC_POWERTRAIN_OK;
}

static double pwr_apt_average_halfshaft_speed(
    const pwr_automatic_powertrain_state *state)
{
    return 0.5
        * (state->left_halfshaft_speed_rad_s
           + state->right_halfshaft_speed_rad_s);
}

static void pwr_apt_preliminary_commands(
    pwr_automatic_powertrain_state *state,
    const pwr_automatic_powertrain_input *input,
    pwr_automatic_powertrain_commands *commands)
{
    const pwr_automatic_powertrain_config *const config = &state->config;
    const double engine_speed = state->engine_output.crank_speed_rad_s;
    const double halfshaft_speed = fabs(pwr_apt_average_halfshaft_speed(state));
    double idle_addition;

    memset(commands, 0, sizeof(*commands));
    commands->engine_throttle = input->driver_throttle;
    commands->gear = PWR_AUTOMATIC_GEAR_NEUTRAL;

    if (!input->ignition_on) {
        state->engine_started = false;
        state->cranking_ticks = UINT64_C(0);
    }
    else if (engine_speed >= config->starter_release_speed_rad_s) {
        state->engine_started = true;
    }

    if (input->control_mode == PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT) {
        commands->starter_torque_nm = input->direct_starter_torque_nm;
        commands->line_pressure_pa = input->direct_line_pressure_command_pa;
        commands->lockup = input->direct_lockup_command;
        commands->gear = input->direct_requested_gear;
        state->tcu_phase = PWR_AUTOMATIC_POWERTRAIN_TCU_DIRECT;
        return;
    }

    if (state->last_control_mode
        == PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT) {
        state->supervised_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
        state->first_gear_ticks = UINT64_C(0);
    }
    if (input->ignition_on && !state->engine_started
        && (double)state->cranking_ticks * pwr_automatic_powertrain_tick_s
            < config->maximum_cranking_time_s) {
        commands->starter_torque_nm = config->starter_torque_nm;
        commands->engine_throttle = pwr_apt_max(
            commands->engine_throttle,
            config->cranking_throttle_command);
        ++state->cranking_ticks;
    }

    idle_addition = config->idle_throttle_command
        + config->idle_speed_throttle_gain_s_per_rad
            * (config->idle_target_speed_rad_s - engine_speed);
    if (input->ignition_on && state->engine_started && engine_speed > 0.0) {
        commands->engine_throttle = pwr_apt_max(
            commands->engine_throttle,
            pwr_apt_clamp(idle_addition, 0.0, 1.0));
    }

    if (!input->ignition_on || !input->selector_drive
        || !state->engine_started) {
        state->supervised_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
        state->first_gear_ticks = UINT64_C(0);
    }
    else if (state->supervised_gear == PWR_AUTOMATIC_GEAR_NEUTRAL) {
        state->supervised_gear = PWR_AUTOMATIC_GEAR_FIRST;
        state->first_gear_ticks = UINT64_C(0);
    }
    else if (state->supervised_gear == PWR_AUTOMATIC_GEAR_FIRST) {
        ++state->first_gear_ticks;
        if (state->automatic_output.active_gear == PWR_AUTOMATIC_GEAR_FIRST
            && !state->automatic_output.shift_active
            && halfshaft_speed
                >= config->first_to_second_halfshaft_speed_rad_s
            && (double)state->first_gear_ticks
                    * pwr_automatic_powertrain_tick_s
                >= config->minimum_first_gear_time_s) {
            state->supervised_gear = PWR_AUTOMATIC_GEAR_SECOND;
        }
    }

    commands->gear = state->supervised_gear;
    if (commands->gear != PWR_AUTOMATIC_GEAR_NEUTRAL) {
        commands->line_pressure_pa = pwr_apt_clamp(
            config->automatic.nominal_line_pressure_pa
                + config->line_pressure_throttle_gain_pa
                    * input->driver_throttle,
            0.0,
            config->automatic.maximum_line_pressure_pa);
    }
    if (commands->gear == PWR_AUTOMATIC_GEAR_SECOND
        && state->automatic_output.active_gear
            == PWR_AUTOMATIC_GEAR_SECOND
        && !state->automatic_output.shift_active
        && halfshaft_speed >= config->lockup_enable_halfshaft_speed_rad_s) {
        commands->lockup = 1.0;
    }
    if (state->automatic_output.shift_active
        || commands->gear != state->automatic_output.target_gear) {
        commands->engine_throttle *= config->shift_engine_throttle_scale;
    }
    commands->engine_throttle = pwr_apt_clamp(
        commands->engine_throttle, 0.0, 1.0);
}

static void pwr_apt_electrical_and_brownout(
    pwr_automatic_powertrain_state *state,
    const pwr_automatic_powertrain_input *input,
    pwr_automatic_powertrain_commands *commands,
    uint32_t *step_flags)
{
    const pwr_automatic_powertrain_config *const config = &state->config;
    const double pressure_fraction = pwr_apt_clamp(
        commands->line_pressure_pa
            / config->automatic.maximum_line_pressure_pa,
        0.0,
        1.0);
    const double current_a = config->base_accessory_current_a
        + config->tcu_current_a
        + config->solenoid_current_at_max_pressure_a * pressure_fraction
        + config->starter_current_per_nm_a * commands->starter_torque_nm;
    const double open_circuit = pwr_apt_min(
        input->electrical_supply_voltage_v,
        config->electrical_nominal_voltage_v);

    state->low_voltage_v = pwr_apt_max(
        0.0,
        open_circuit
            - config->electrical_internal_resistance_ohm * current_a);
    state->ecu_powered = input->ignition_on
        && state->low_voltage_v >= config->ecu_brownout_voltage_v;
    state->tcu_powered = input->ignition_on
        && state->low_voltage_v >= config->tcu_brownout_voltage_v;

    if (!state->ecu_powered && input->ignition_on) {
        *step_flags |= PWR_AUTOMATIC_POWERTRAIN_DIAG_ECU_BROWNOUT;
    }
    if (!state->tcu_powered && input->ignition_on) {
        commands->gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
        commands->line_pressure_pa = 0.0;
        commands->lockup = 0.0;
        state->tcu_phase = PWR_AUTOMATIC_POWERTRAIN_TCU_BROWNOUT;
        *step_flags |= PWR_AUTOMATIC_POWERTRAIN_DIAG_TCU_BROWNOUT;
    }
    commands->starter_torque_nm *= pwr_apt_clamp(
        state->low_voltage_v / config->electrical_nominal_voltage_v,
        0.0,
        1.0);
}

static pwr_exhaust_reservoir pwr_apt_engine_exhaust_reservoir(
    const pwr_si_engine_output *engine,
    double minimum_pressure_pa,
    double minimum_temperature_k)
{
    pwr_exhaust_reservoir reservoir = {
        .pressure_pa = pwr_apt_max(
            engine->exhaust_source_pressure_pa, minimum_pressure_pa),
        .temperature_k = pwr_apt_max(
            engine->exhaust_temperature_k, minimum_temperature_k),
    };

    for (size_t species = 0U; species < PWR_EXHAUST_SPECIES_COUNT;
         ++species) {
        reservoir.species_mass_fraction[species]
            = engine->exhaust_species_mass_fraction[species];
    }
    return reservoir;
}

static double pwr_apt_source_cp(
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

static double pwr_apt_road_load(
    const pwr_automatic_powertrain_config *config,
    double speed_rad_s,
    double additional_torque_nm)
{
    const double sign = tanh(
        speed_rad_s / config->road_load_regularization_speed_rad_s);
    return config->rolling_resistance_torque_nm_per_side * sign
        + config->viscous_road_load_nm_s_per_rad_per_side * speed_rad_s
        + config->quadratic_road_load_nm_s2_per_rad2_per_side
            * speed_rad_s * fabs(speed_rad_s)
        + additional_torque_nm * sign;
}

static void pwr_apt_update_tcu_phase(
    pwr_automatic_powertrain_state *state,
    const pwr_automatic_powertrain_input *input,
    uint32_t *step_flags)
{
    if (!state->tcu_powered && input->ignition_on) {
        state->tcu_phase = PWR_AUTOMATIC_POWERTRAIN_TCU_BROWNOUT;
    }
    else if (input->control_mode
             == PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT) {
        state->tcu_phase = PWR_AUTOMATIC_POWERTRAIN_TCU_DIRECT;
    }
    else if (state->automatic_output.shift_active
             && state->automatic_output.target_gear
                 == PWR_AUTOMATIC_GEAR_SECOND) {
        state->tcu_phase = PWR_AUTOMATIC_POWERTRAIN_TCU_SHIFT_1_TO_2;
        *step_flags |= PWR_AUTOMATIC_POWERTRAIN_DIAG_SHIFT_ACTIVE;
    }
    else if (state->supervised_gear == PWR_AUTOMATIC_GEAR_SECOND
             && state->automatic_output.lockup_engagement > 0.50) {
        state->tcu_phase = PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND_LOCKED;
        *step_flags |= PWR_AUTOMATIC_POWERTRAIN_DIAG_LOCKUP_ACTIVE;
    }
    else if (state->supervised_gear == PWR_AUTOMATIC_GEAR_SECOND) {
        state->tcu_phase = PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND;
    }
    else if (state->supervised_gear == PWR_AUTOMATIC_GEAR_FIRST) {
        state->tcu_phase = PWR_AUTOMATIC_POWERTRAIN_TCU_FIRST;
    }
    else {
        state->tcu_phase = PWR_AUTOMATIC_POWERTRAIN_TCU_NEUTRAL;
    }
}

static bool pwr_apt_state_finite(
    const pwr_automatic_powertrain_state *state)
{
    return isfinite(state->low_voltage_v)
        && isfinite(state->left_halfshaft_speed_rad_s)
        && isfinite(state->right_halfshaft_speed_rad_s)
        && isfinite(state->left_halfshaft_angle_rad)
        && isfinite(state->right_halfshaft_angle_rad)
        && isfinite(state->cumulative_halfshaft_drive_work_j)
        && isfinite(state->cumulative_road_load_work_j)
        && isfinite(state->cumulative_differential_projection_adjustment_j)
        && isfinite(state->cumulative_exhaust_mass_mismatch_kg)
        && isfinite(state->cumulative_exhaust_enthalpy_mismatch_j)
        && isfinite(state->cumulative_engine_converter_energy_mismatch_j)
        && isfinite(state->cumulative_final_drive_energy_mismatch_j);
}

pwr_automatic_powertrain_result pwr_automatic_powertrain_step(
    pwr_automatic_powertrain_state *state,
    const pwr_automatic_powertrain_input *input,
    pwr_automatic_powertrain_output *output)
{
    pwr_automatic_powertrain_state candidate;
    pwr_automatic_powertrain_commands commands;
    pwr_exhaust_inputs exhaust_input;
    pwr_si_engine_input engine_input;
    pwr_automatic_coupled_input automatic_input;
    pwr_si_engine_output next_engine_output;
    pwr_automatic_snapshot next_automatic_output;
    pwr_automatic_coupling_output next_coupling;
    pwr_exhaust_diagnostics next_exhaust_step;
    const double dt = pwr_automatic_powertrain_tick_s;
    double engine_load_work_before;
    double automatic_boundary_energy_before;
    double old_automatic_output_speed;
    double old_left_speed;
    double old_right_speed;
    double predicted_left_speed;
    double predicted_right_speed;
    double predicted_carrier_speed;
    double carrier_speed;
    double carrier_correction;
    double average_left_speed;
    double average_right_speed;
    double old_halfshaft_energy;
    double new_halfshaft_energy;
    double halfshaft_drive_work;
    double road_work;
    double source_mass_flow;
    double network_mass_flow;
    double source_enthalpy_flow;
    double network_enthalpy_flow;
    double old_automatic_speed_average;
    double final_drive_input_work;
    uint32_t engine_faults;
    uint32_t step_flags = PWR_AUTOMATIC_POWERTRAIN_DIAG_NONE;

    if (state == NULL || output == NULL) {
        return PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT;
    }
    if (pwr_automatic_powertrain_validate_config(&state->config)
        != PWR_AUTOMATIC_POWERTRAIN_OK) {
        return PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG;
    }
    if (!pwr_apt_input_valid(&state->config, input)
        || input->ambient_temperature_k
            < state->config.exhaust.minimum_temperature_k) {
        return PWR_AUTOMATIC_POWERTRAIN_INVALID_INPUT;
    }

    candidate = *state;
    pwr_apt_preliminary_commands(&candidate, input, &commands);
    pwr_apt_electrical_and_brownout(
        &candidate, input, &commands, &step_flags);
    candidate.last_control_mode = input->control_mode;

    /* 1. Exhaust consumes the previously committed engine source. */
    exhaust_input = (pwr_exhaust_inputs){
        .upstream = pwr_apt_engine_exhaust_reservoir(
            &candidate.engine_output,
            input->ambient_pressure_pa,
            candidate.config.exhaust.minimum_temperature_k),
        .downstream = pwr_apt_ambient_reservoir(
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
        return PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR;
    }
    candidate.exhaust_step = next_exhaust_step;
    candidate.exhaust_output = next_exhaust_step.final_observation;

    /* 2. Engine sees new backpressure and previous converter/pump load. */
    engine_faults = input->engine_fault_mask;
    if (!candidate.ecu_powered && input->ignition_on) {
        engine_faults |= PWR_SI_ENGINE_FAULT_LOW_VOLTAGE;
    }
    engine_load_work_before = candidate.engine_output.mechanical_load_work_j;
    engine_input = (pwr_si_engine_input){
        .driver_throttle = commands.engine_throttle,
        .load_torque_nm = candidate.converter_coupling.pump_load_torque_nm,
        .starter_torque_nm = commands.starter_torque_nm,
        .exhaust_backpressure_pa = pwr_apt_max(
            candidate.exhaust_output.pressure_pa[
                PWR_EXHAUST_VOLUME_MANIFOLD],
            1.0),
        .ambient_pressure_pa = input->ambient_pressure_pa,
        .ambient_temperature_k = input->ambient_temperature_k,
        .fault_mask = engine_faults,
        .ignition_on = input->ignition_on,
    };
    if (pwr_si_engine_step(
            &candidate.engine, &engine_input, &next_engine_output)
        != PWR_SI_ENGINE_OK) {
        return PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR;
    }
    candidate.engine_output = next_engine_output;

    /* Road load is sampled before the automatic/final-drive explicit solve. */
    candidate.last_left_road_load_torque_nm = pwr_apt_road_load(
        &candidate.config,
        candidate.left_halfshaft_speed_rad_s,
        input->left_additional_road_load_torque_nm);
    candidate.last_right_road_load_torque_nm = pwr_apt_road_load(
        &candidate.config,
        candidate.right_halfshaft_speed_rad_s,
        input->right_additional_road_load_torque_nm);

    /* 3. Impose the new crank speed at the converter pump boundary. */
    automatic_boundary_energy_before
        = candidate.automatic_output.pump_boundary_energy_j;
    old_automatic_output_speed
        = candidate.automatic_output.output_speed_rad_s;
    candidate.last_pump_speed_projection_rad_s
        = candidate.engine_output.crank_speed_rad_s
        - candidate.automatic_output.pump_speed_rad_s;
    automatic_input = (pwr_automatic_coupled_input){
        .pump_speed_rad_s = candidate.engine_output.crank_speed_rad_s,
        .output_external_torque_nm
            = -(candidate.last_left_road_load_torque_nm
                + candidate.last_right_road_load_torque_nm)
                / (candidate.config.final_drive_ratio
                   * candidate.config.final_drive_efficiency),
        .line_pressure_command_pa = commands.line_pressure_pa,
        .lockup_command = commands.lockup,
        .requested_gear = commands.gear,
    };
    if (pwr_automatic_step_coupled(
            &candidate.automatic, &automatic_input, &next_coupling)
            != PWR_AUTOMATIC_OK
        || pwr_automatic_snapshot_read(
               &candidate.automatic, &next_automatic_output)
            != PWR_AUTOMATIC_OK) {
        return PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR;
    }
    candidate.converter_coupling = next_coupling;
    candidate.automatic_output = next_automatic_output;

    /* 4. Final drive and ideal open differential, followed by projection. */
    old_left_speed = candidate.left_halfshaft_speed_rad_s;
    old_right_speed = candidate.right_halfshaft_speed_rad_s;
    candidate.last_left_drive_torque_nm
        = 0.5 * candidate.automatic_output.gear_output_torque_nm
        * candidate.config.final_drive_ratio
        * candidate.config.final_drive_efficiency;
    candidate.last_right_drive_torque_nm
        = candidate.last_left_drive_torque_nm;
    predicted_left_speed = old_left_speed
        + (candidate.last_left_drive_torque_nm
           - candidate.last_left_road_load_torque_nm)
            / candidate.config.left_halfshaft_inertia_kg_m2
            * dt;
    predicted_right_speed = old_right_speed
        + (candidate.last_right_drive_torque_nm
           - candidate.last_right_road_load_torque_nm)
            / candidate.config.right_halfshaft_inertia_kg_m2
            * dt;
    predicted_carrier_speed
        = 0.5 * (predicted_left_speed + predicted_right_speed);
    carrier_speed = candidate.automatic_output.output_speed_rad_s
        / candidate.config.final_drive_ratio;
    carrier_correction = carrier_speed - predicted_carrier_speed;
    candidate.left_halfshaft_speed_rad_s
        = predicted_left_speed + carrier_correction;
    candidate.right_halfshaft_speed_rad_s
        = predicted_right_speed + carrier_correction;
    candidate.last_unconstrained_carrier_speed_mismatch_rad_s
        = carrier_correction;
    candidate.last_carrier_constraint_residual_rad_s
        = 0.5
            * (candidate.left_halfshaft_speed_rad_s
               + candidate.right_halfshaft_speed_rad_s)
        - carrier_speed;

    average_left_speed = 0.5
        * (old_left_speed + candidate.left_halfshaft_speed_rad_s);
    average_right_speed = 0.5
        * (old_right_speed + candidate.right_halfshaft_speed_rad_s);
    candidate.left_halfshaft_angle_rad += average_left_speed * dt;
    candidate.right_halfshaft_angle_rad += average_right_speed * dt;
    halfshaft_drive_work
        = candidate.last_left_drive_torque_nm * average_left_speed * dt
        + candidate.last_right_drive_torque_nm * average_right_speed * dt;
    road_work = candidate.last_left_road_load_torque_nm
            * average_left_speed * dt
        + candidate.last_right_road_load_torque_nm
            * average_right_speed * dt;
    old_halfshaft_energy = 0.5
        * (candidate.config.left_halfshaft_inertia_kg_m2
               * old_left_speed * old_left_speed
           + candidate.config.right_halfshaft_inertia_kg_m2
               * old_right_speed * old_right_speed);
    new_halfshaft_energy = 0.5
        * (candidate.config.left_halfshaft_inertia_kg_m2
               * candidate.left_halfshaft_speed_rad_s
               * candidate.left_halfshaft_speed_rad_s
           + candidate.config.right_halfshaft_inertia_kg_m2
               * candidate.right_halfshaft_speed_rad_s
               * candidate.right_halfshaft_speed_rad_s);
    candidate.last_differential_projection_adjustment_j
        = new_halfshaft_energy - old_halfshaft_energy
        - halfshaft_drive_work + road_work;
    candidate.cumulative_halfshaft_drive_work_j += halfshaft_drive_work;
    candidate.cumulative_road_load_work_j += road_work;
    candidate.cumulative_differential_projection_adjustment_j
        += candidate.last_differential_projection_adjustment_j;

    /* 5. Audit the explicit mass, energy, and speed interfaces. */
    source_mass_flow = state->engine_output.exhaust_mass_flow_kg_per_s;
    network_mass_flow
        = next_exhaust_step.average_path_mass_flow_kg_per_s[
            PWR_EXHAUST_PATH_INLET];
    candidate.last_exhaust_mass_mismatch_kg
        = (source_mass_flow - network_mass_flow) * dt;
    candidate.cumulative_exhaust_mass_mismatch_kg
        += candidate.last_exhaust_mass_mismatch_kg;
    source_enthalpy_flow = source_mass_flow
        * pwr_apt_source_cp(&candidate.config.exhaust, &state->engine_output)
        * exhaust_input.upstream.temperature_k;
    network_enthalpy_flow = network_mass_flow
        * pwr_apt_source_cp(&candidate.config.exhaust, &state->engine_output)
        * exhaust_input.upstream.temperature_k;
    candidate.last_exhaust_enthalpy_mismatch_j
        = (source_enthalpy_flow - network_enthalpy_flow) * dt;
    candidate.cumulative_exhaust_enthalpy_mismatch_j
        += candidate.last_exhaust_enthalpy_mismatch_j;
    candidate.last_engine_converter_energy_mismatch_j
        = (candidate.engine_output.mechanical_load_work_j
           - engine_load_work_before)
        - (candidate.automatic_output.pump_boundary_energy_j
           - automatic_boundary_energy_before);
    candidate.cumulative_engine_converter_energy_mismatch_j
        += candidate.last_engine_converter_energy_mismatch_j;

    old_automatic_speed_average = 0.5
        * (old_automatic_output_speed
           + candidate.automatic_output.output_speed_rad_s);
    final_drive_input_work
        = candidate.automatic_output.gear_output_torque_nm
        * old_automatic_speed_average
        * candidate.config.final_drive_efficiency * dt;
    candidate.last_final_drive_energy_mismatch_j
        = final_drive_input_work - halfshaft_drive_work;
    candidate.cumulative_final_drive_energy_mismatch_j
        += candidate.last_final_drive_energy_mismatch_j;

    if (fabs(candidate.last_exhaust_mass_mismatch_kg) / dt
        > candidate.config.maximum_exhaust_mass_mismatch_kg_per_s) {
        step_flags |= PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH;
    }
    if (fabs(candidate.last_exhaust_enthalpy_mismatch_j) / dt
        > candidate.config.maximum_exhaust_enthalpy_mismatch_w) {
        step_flags |= PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH;
    }
    if (fabs(candidate.last_engine_converter_energy_mismatch_j) / dt
        > candidate.config.maximum_mechanical_coupling_mismatch_w) {
        step_flags
            |= PWR_AUTOMATIC_POWERTRAIN_DIAG_ENGINE_CONVERTER_ENERGY_MISMATCH;
    }
    if (fabs(candidate.last_pump_speed_projection_rad_s)
        > candidate.config.maximum_speed_projection_rad_s) {
        step_flags |= PWR_AUTOMATIC_POWERTRAIN_DIAG_PUMP_SPEED_PROJECTED;
    }
    if (fabs(carrier_correction)
        > candidate.config.maximum_speed_projection_rad_s) {
        step_flags
            |= PWR_AUTOMATIC_POWERTRAIN_DIAG_DIFFERENTIAL_SPEED_PROJECTED;
    }
    if (fabs(candidate.last_final_drive_energy_mismatch_j) / dt
        > candidate.config.maximum_mechanical_coupling_mismatch_w) {
        step_flags
            |= PWR_AUTOMATIC_POWERTRAIN_DIAG_FINAL_DRIVE_ENERGY_MISMATCH;
    }

    pwr_apt_update_tcu_phase(&candidate, input, &step_flags);
    candidate.last_commanded_engine_throttle = commands.engine_throttle;
    candidate.last_commanded_starter_torque_nm = commands.starter_torque_nm;
    candidate.last_commanded_line_pressure_pa = commands.line_pressure_pa;
    candidate.last_commanded_lockup = commands.lockup;
    candidate.last_commanded_gear = commands.gear;
    candidate.last_step_diagnostic_flags = step_flags;
    candidate.diagnostic_flags |= step_flags;
    ++candidate.tick;

    if (!pwr_apt_state_finite(&candidate)) {
        return PWR_AUTOMATIC_POWERTRAIN_NUMERIC_ERROR;
    }
    if (pwr_automatic_powertrain_observe(&candidate, output)
        != PWR_AUTOMATIC_POWERTRAIN_OK) {
        return PWR_AUTOMATIC_POWERTRAIN_NUMERIC_ERROR;
    }
    *state = candidate;
    return PWR_AUTOMATIC_POWERTRAIN_OK;
}

pwr_automatic_powertrain_result pwr_automatic_powertrain_observe(
    const pwr_automatic_powertrain_state *state,
    pwr_automatic_powertrain_output *output)
{
    double kinetic;
    double kinetic_change;

    if (state == NULL || output == NULL) {
        return PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT;
    }
    if (pwr_automatic_powertrain_validate_config(&state->config)
        != PWR_AUTOMATIC_POWERTRAIN_OK) {
        return PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG;
    }

    kinetic = 0.5
        * (state->config.left_halfshaft_inertia_kg_m2
               * state->left_halfshaft_speed_rad_s
               * state->left_halfshaft_speed_rad_s
           + state->config.right_halfshaft_inertia_kg_m2
               * state->right_halfshaft_speed_rad_s
               * state->right_halfshaft_speed_rad_s);
    kinetic_change = kinetic - state->initial_halfshaft_kinetic_energy_j;
    *output = (pwr_automatic_powertrain_output){
        .time_s = (double)state->tick * pwr_automatic_powertrain_tick_s,
        .tick = state->tick,
        .control_mode = state->last_control_mode,
        .tcu_phase = state->tcu_phase,
        .low_voltage_v = state->low_voltage_v,
        .ecu_powered = state->ecu_powered,
        .tcu_powered = state->tcu_powered,
        .commanded_engine_throttle = state->last_commanded_engine_throttle,
        .commanded_starter_torque_nm
            = state->last_commanded_starter_torque_nm,
        .commanded_line_pressure_pa
            = state->last_commanded_line_pressure_pa,
        .commanded_lockup = state->last_commanded_lockup,
        .commanded_gear = state->last_commanded_gear,
        .engine = state->engine_output,
        .automatic = state->automatic_output,
        .converter_coupling = state->converter_coupling,
        .exhaust = state->exhaust_output,
        .exhaust_step = state->exhaust_step,
        .differential_carrier_speed_rad_s
            = state->automatic_output.output_speed_rad_s
            / state->config.final_drive_ratio,
        .left_halfshaft_speed_rad_s = state->left_halfshaft_speed_rad_s,
        .right_halfshaft_speed_rad_s = state->right_halfshaft_speed_rad_s,
        .left_halfshaft_angle_rad = state->left_halfshaft_angle_rad,
        .right_halfshaft_angle_rad = state->right_halfshaft_angle_rad,
        .left_drive_torque_nm = state->last_left_drive_torque_nm,
        .right_drive_torque_nm = state->last_right_drive_torque_nm,
        .left_road_load_torque_nm = state->last_left_road_load_torque_nm,
        .right_road_load_torque_nm = state->last_right_road_load_torque_nm,
        .differential_speed_rad_s
            = state->left_halfshaft_speed_rad_s
            - state->right_halfshaft_speed_rad_s,
        .halfshaft_kinetic_energy_j = kinetic,
        .cumulative_halfshaft_drive_work_j
            = state->cumulative_halfshaft_drive_work_j,
        .cumulative_road_load_work_j = state->cumulative_road_load_work_j,
        .cumulative_differential_projection_adjustment_j
            = state->cumulative_differential_projection_adjustment_j,
        .halfshaft_energy_residual_j
            = state->cumulative_halfshaft_drive_work_j
            - state->cumulative_road_load_work_j
            + state->cumulative_differential_projection_adjustment_j
            - kinetic_change,
        .exhaust_mass_mismatch_kg = state->last_exhaust_mass_mismatch_kg,
        .cumulative_exhaust_mass_mismatch_kg
            = state->cumulative_exhaust_mass_mismatch_kg,
        .exhaust_enthalpy_mismatch_j
            = state->last_exhaust_enthalpy_mismatch_j,
        .cumulative_exhaust_enthalpy_mismatch_j
            = state->cumulative_exhaust_enthalpy_mismatch_j,
        .engine_converter_energy_mismatch_j
            = state->last_engine_converter_energy_mismatch_j,
        .cumulative_engine_converter_energy_mismatch_j
            = state->cumulative_engine_converter_energy_mismatch_j,
        .pump_speed_projection_rad_s
            = state->last_pump_speed_projection_rad_s,
        .pump_projection_adjustment_j
            = state->converter_coupling.projection_adjustment_j,
        .unconstrained_carrier_speed_mismatch_rad_s
            = state->last_unconstrained_carrier_speed_mismatch_rad_s,
        .carrier_constraint_residual_rad_s
            = state->last_carrier_constraint_residual_rad_s,
        .differential_projection_adjustment_j
            = state->last_differential_projection_adjustment_j,
        .final_drive_energy_mismatch_j
            = state->last_final_drive_energy_mismatch_j,
        .cumulative_final_drive_energy_mismatch_j
            = state->cumulative_final_drive_energy_mismatch_j,
        .last_step_diagnostic_flags = state->last_step_diagnostic_flags,
        .diagnostic_flags = state->diagnostic_flags,
    };
    output->state_hash = pwr_automatic_powertrain_state_hash(state);
    return PWR_AUTOMATIC_POWERTRAIN_OK;
}

static uint64_t pwr_apt_hash_u64(uint64_t hash, uint64_t value)
{
    for (unsigned int byte = 0U; byte < 8U; ++byte) {
        hash ^= (value >> (byte * 8U)) & UINT64_C(0xff);
        hash *= UINT64_C(1099511628211);
    }
    return hash;
}

static uint64_t pwr_apt_hash_double(uint64_t hash, double value)
{
    uint64_t bits = UINT64_C(0);
    static_assert(sizeof(bits) == sizeof(value), "double must be 64-bit");
    memcpy(&bits, &value, sizeof(bits));
    return pwr_apt_hash_u64(hash, bits);
}

uint64_t pwr_automatic_powertrain_state_hash(
    const pwr_automatic_powertrain_state *state)
{
    uint64_t hash = UINT64_C(14695981039346656037);

    if (state == NULL) {
        return UINT64_C(0);
    }
    hash = pwr_apt_hash_u64(hash, pwr_si_engine_state_hash(&state->engine));
    hash = pwr_apt_hash_u64(hash, pwr_automatic_state_hash(&state->automatic));
    for (size_t volume = 0U; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        hash = pwr_apt_hash_double(
            hash, state->exhaust.volume[volume].total_mass_kg);
        for (size_t species = 0U; species < PWR_EXHAUST_SPECIES_COUNT;
             ++species) {
            hash = pwr_apt_hash_double(
                hash,
                state->exhaust.volume[volume].species_mass_kg[species]);
        }
        hash = pwr_apt_hash_double(
            hash, state->exhaust.volume[volume].internal_energy_j);
        hash = pwr_apt_hash_double(
            hash, state->exhaust.volume[volume].wall_temperature_k);
    }
    hash = pwr_apt_hash_double(
        hash, state->exhaust.catalyst_conversion_fraction);
    for (size_t species = 0U; species < PWR_EXHAUST_SPECIES_COUNT;
         ++species) {
        hash = pwr_apt_hash_double(
            hash, state->exhaust.cumulative_tailpipe_species_kg[species]);
    }
    hash = pwr_apt_hash_double(hash, state->exhaust.time_s);
    hash = pwr_apt_hash_u64(hash, state->tick);
    hash = pwr_apt_hash_u64(hash, state->cranking_ticks);
    hash = pwr_apt_hash_u64(hash, state->first_gear_ticks);
    hash = pwr_apt_hash_u64(hash, (uint64_t)state->last_control_mode);
    hash = pwr_apt_hash_u64(hash, (uint64_t)state->tcu_phase);
    hash = pwr_apt_hash_u64(hash, (uint64_t)(int64_t)state->supervised_gear);
    hash = pwr_apt_hash_u64(hash, state->engine_started ? 1U : 0U);
    hash = pwr_apt_hash_u64(hash, state->ecu_powered ? 1U : 0U);
    hash = pwr_apt_hash_u64(hash, state->tcu_powered ? 1U : 0U);
    hash = pwr_apt_hash_double(hash, state->low_voltage_v);
    hash = pwr_apt_hash_double(hash, state->left_halfshaft_speed_rad_s);
    hash = pwr_apt_hash_double(hash, state->right_halfshaft_speed_rad_s);
    hash = pwr_apt_hash_double(hash, state->left_halfshaft_angle_rad);
    hash = pwr_apt_hash_double(hash, state->right_halfshaft_angle_rad);
    hash = pwr_apt_hash_double(
        hash, state->cumulative_halfshaft_drive_work_j);
    hash = pwr_apt_hash_double(hash, state->cumulative_road_load_work_j);
    hash = pwr_apt_hash_double(
        hash, state->cumulative_differential_projection_adjustment_j);
    hash = pwr_apt_hash_double(
        hash, state->cumulative_exhaust_mass_mismatch_kg);
    hash = pwr_apt_hash_double(
        hash, state->cumulative_exhaust_enthalpy_mismatch_j);
    hash = pwr_apt_hash_double(
        hash, state->cumulative_engine_converter_energy_mismatch_j);
    hash = pwr_apt_hash_double(
        hash, state->cumulative_final_drive_energy_mismatch_j);
    hash = pwr_apt_hash_u64(hash, state->last_step_diagnostic_flags);
    hash = pwr_apt_hash_u64(hash, state->diagnostic_flags);
    return hash;
}

const char *pwr_automatic_powertrain_result_string(
    pwr_automatic_powertrain_result result)
{
    switch (result) {
        case PWR_AUTOMATIC_POWERTRAIN_OK:
            return "ok";
        case PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT:
            return "invalid argument";
        case PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG:
            return "invalid config";
        case PWR_AUTOMATIC_POWERTRAIN_INVALID_INPUT:
            return "invalid input";
        case PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR:
            return "submodel error";
        case PWR_AUTOMATIC_POWERTRAIN_NUMERIC_ERROR:
            return "numeric error";
        default:
            return "unknown result";
    }
}
