// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/models/powertrain/pwr_dct_powertrain.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_DCT_CLUTCH_COUNT = @import("../../abi.zig").PWR_DCT_CLUTCH_COUNT;
const PWR_DCT_CLUTCH_K1 = @import("../../abi.zig").PWR_DCT_CLUTCH_K1;
const PWR_DCT_CLUTCH_K2 = @import("../../abi.zig").PWR_DCT_CLUTCH_K2;
const PWR_DCT_OK = @import("../../abi.zig").PWR_DCT_OK;
const PWR_DCT_POWERTRAIN_CONTROL_DIRECT = @import("../../abi.zig").PWR_DCT_POWERTRAIN_CONTROL_DIRECT;
const PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED = @import("../../abi.zig").PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED;
const PWR_DCT_POWERTRAIN_DIAG_DCT_OUTPUT_ENERGY_MISMATCH = @import("../../abi.zig").PWR_DCT_POWERTRAIN_DIAG_DCT_OUTPUT_ENERGY_MISMATCH;
const PWR_DCT_POWERTRAIN_DIAG_ENGINE_DCT_ENERGY_MISMATCH = @import("../../abi.zig").PWR_DCT_POWERTRAIN_DIAG_ENGINE_DCT_ENERGY_MISMATCH;
const PWR_DCT_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH = @import("../../abi.zig").PWR_DCT_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH;
const PWR_DCT_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH = @import("../../abi.zig").PWR_DCT_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH;
const PWR_DCT_POWERTRAIN_DIAG_HALFSHAFT_SPEED_CLAMP = @import("../../abi.zig").PWR_DCT_POWERTRAIN_DIAG_HALFSHAFT_SPEED_CLAMP;
const PWR_DCT_POWERTRAIN_DIAG_NONE = @import("../../abi.zig").PWR_DCT_POWERTRAIN_DIAG_NONE;
const PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE = @import("../../abi.zig").PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE;
const PWR_DCT_POWERTRAIN_DIAG_TCU_SHIFT_ACTIVE = @import("../../abi.zig").PWR_DCT_POWERTRAIN_DIAG_TCU_SHIFT_ACTIVE;
const PWR_DCT_POWERTRAIN_INVALID_ARGUMENT = @import("../../abi.zig").PWR_DCT_POWERTRAIN_INVALID_ARGUMENT;
const PWR_DCT_POWERTRAIN_INVALID_CONFIG = @import("../../abi.zig").PWR_DCT_POWERTRAIN_INVALID_CONFIG;
const PWR_DCT_POWERTRAIN_INVALID_INPUT = @import("../../abi.zig").PWR_DCT_POWERTRAIN_INVALID_INPUT;
const PWR_DCT_POWERTRAIN_NUMERIC_ERROR = @import("../../abi.zig").PWR_DCT_POWERTRAIN_NUMERIC_ERROR;
const PWR_DCT_POWERTRAIN_OK = @import("../../abi.zig").PWR_DCT_POWERTRAIN_OK;
const PWR_DCT_POWERTRAIN_SUBMODEL_ERROR = @import("../../abi.zig").PWR_DCT_POWERTRAIN_SUBMODEL_ERROR;
const PWR_DCT_POWERTRAIN_TCU_DIRECT = @import("../../abi.zig").PWR_DCT_POWERTRAIN_TCU_DIRECT;
const PWR_DCT_POWERTRAIN_TCU_GEAR_1 = @import("../../abi.zig").PWR_DCT_POWERTRAIN_TCU_GEAR_1;
const PWR_DCT_POWERTRAIN_TCU_GEAR_2 = @import("../../abi.zig").PWR_DCT_POWERTRAIN_TCU_GEAR_2;
const PWR_DCT_POWERTRAIN_TCU_LAUNCH_1 = @import("../../abi.zig").PWR_DCT_POWERTRAIN_TCU_LAUNCH_1;
const PWR_DCT_POWERTRAIN_TCU_NEUTRAL = @import("../../abi.zig").PWR_DCT_POWERTRAIN_TCU_NEUTRAL;
const PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2 = @import("../../abi.zig").PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2;
const PWR_DCT_POWERTRAIN_TCU_RELEASE_TO_NEUTRAL = @import("../../abi.zig").PWR_DCT_POWERTRAIN_TCU_RELEASE_TO_NEUTRAL;
const PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2 = @import("../../abi.zig").PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2;
const PWR_EXHAUST_OK = @import("../../abi.zig").PWR_EXHAUST_OK;
const PWR_EXHAUST_PATH_CATALYST_TO_MUFFLER = @import("../../abi.zig").PWR_EXHAUST_PATH_CATALYST_TO_MUFFLER;
const PWR_EXHAUST_PATH_COUNT = @import("../../abi.zig").PWR_EXHAUST_PATH_COUNT;
const PWR_EXHAUST_PATH_INLET = @import("../../abi.zig").PWR_EXHAUST_PATH_INLET;
const PWR_EXHAUST_PATH_MANIFOLD_TO_CATALYST = @import("../../abi.zig").PWR_EXHAUST_PATH_MANIFOLD_TO_CATALYST;
const PWR_EXHAUST_PATH_TAILPIPE = @import("../../abi.zig").PWR_EXHAUST_PATH_TAILPIPE;
const PWR_EXHAUST_SPECIES_COUNT = @import("../../abi.zig").PWR_EXHAUST_SPECIES_COUNT;
const PWR_EXHAUST_SPECIES_INERT = @import("../../abi.zig").PWR_EXHAUST_SPECIES_INERT;
const PWR_EXHAUST_SPECIES_OXYGEN = @import("../../abi.zig").PWR_EXHAUST_SPECIES_OXYGEN;
const PWR_EXHAUST_SPECIES_PRODUCTS = @import("../../abi.zig").PWR_EXHAUST_SPECIES_PRODUCTS;
const PWR_EXHAUST_SPECIES_REDUCTANT = @import("../../abi.zig").PWR_EXHAUST_SPECIES_REDUCTANT;
const PWR_EXHAUST_VOLUME_CATALYST = @import("../../abi.zig").PWR_EXHAUST_VOLUME_CATALYST;
const PWR_EXHAUST_VOLUME_COUNT = @import("../../abi.zig").PWR_EXHAUST_VOLUME_COUNT;
const PWR_EXHAUST_VOLUME_MANIFOLD = @import("../../abi.zig").PWR_EXHAUST_VOLUME_MANIFOLD;
const PWR_EXHAUST_VOLUME_MUFFLER = @import("../../abi.zig").PWR_EXHAUST_VOLUME_MUFFLER;
const PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT = @import("../../abi.zig").PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT;
const PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED = @import("../../abi.zig").PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED;
const PWR_SI_ENGINE_FAULT_LOW_VOLTAGE = @import("../../abi.zig").PWR_SI_ENGINE_FAULT_LOW_VOLTAGE;
const PWR_SI_ENGINE_FAULT_THROTTLE_STUCK = @import("../../abi.zig").PWR_SI_ENGINE_FAULT_THROTTLE_STUCK;
const PWR_SI_ENGINE_OK = @import("../../abi.zig").PWR_SI_ENGINE_OK;
const fabs = @import("../../support.zig").fabs;
const memcpy = @import("../../support.zig").memcpy;
const memset = @import("../../support.zig").memset;
const power_isfinite = @import("../../support.zig").power_isfinite;
const pwr_dct = @import("../../abi.zig").pwr_dct;
const pwr_dct_config = @import("../../abi.zig").pwr_dct_config;
const pwr_dct_config_default = @import("../drivetrain/pwr_dct.zig").pwr_dct_config_default;
const pwr_dct_gear_mask = @import("../../abi.zig").pwr_dct_gear_mask;
const pwr_dct_init = @import("../drivetrain/pwr_dct.zig").pwr_dct_init;
const pwr_dct_inputs = @import("../../abi.zig").pwr_dct_inputs;
const pwr_dct_powertrain_commands = @import("../../abi.zig").pwr_dct_powertrain_commands;
const pwr_dct_powertrain_config = @import("../../abi.zig").pwr_dct_powertrain_config;
const pwr_dct_powertrain_control_mode = @import("../../abi.zig").pwr_dct_powertrain_control_mode;
const pwr_dct_powertrain_input = @import("../../abi.zig").pwr_dct_powertrain_input;
const pwr_dct_powertrain_output = @import("../../abi.zig").pwr_dct_powertrain_output;
const pwr_dct_powertrain_result = @import("../../abi.zig").pwr_dct_powertrain_result;
const pwr_dct_powertrain_state = @import("../../abi.zig").pwr_dct_powertrain_state;
const pwr_dct_powertrain_tcu_phase = @import("../../abi.zig").pwr_dct_powertrain_tcu_phase;
const pwr_dct_powertrain_tick_s = @import("../../abi.zig").pwr_dct_powertrain_tick_s;
const pwr_dct_snapshot = @import("../../abi.zig").pwr_dct_snapshot;
const pwr_dct_snapshot_read = @import("../drivetrain/pwr_dct.zig").pwr_dct_snapshot_read;
const pwr_dct_state_hash = @import("../drivetrain/pwr_dct.zig").pwr_dct_state_hash;
const pwr_dct_step = @import("../drivetrain/pwr_dct.zig").pwr_dct_step;
const pwr_dct_validate_config = @import("../drivetrain/pwr_dct.zig").pwr_dct_validate_config;
const pwr_exhaust_diagnostics = @import("../../abi.zig").pwr_exhaust_diagnostics;
const pwr_exhaust_initialize_uniform = @import("../exhaust/pwr_exhaust.zig").pwr_exhaust_initialize_uniform;
const pwr_exhaust_inputs = @import("../../abi.zig").pwr_exhaust_inputs;
const pwr_exhaust_observation = @import("../../abi.zig").pwr_exhaust_observation;
const pwr_exhaust_observe = @import("../exhaust/pwr_exhaust.zig").pwr_exhaust_observe;
const pwr_exhaust_parameters = @import("../../abi.zig").pwr_exhaust_parameters;
const pwr_exhaust_reservoir = @import("../../abi.zig").pwr_exhaust_reservoir;
const pwr_exhaust_state = @import("../../abi.zig").pwr_exhaust_state;
const pwr_exhaust_step = @import("../exhaust/pwr_exhaust.zig").pwr_exhaust_step;
const pwr_exhaust_validate_parameters = @import("../exhaust/pwr_exhaust.zig").pwr_exhaust_validate_parameters;
const pwr_si_engine_config = @import("../../abi.zig").pwr_si_engine_config;
const pwr_si_engine_config_default = @import("../engine/pwr_si_engine.zig").pwr_si_engine_config_default;
const pwr_si_engine_init = @import("../engine/pwr_si_engine.zig").pwr_si_engine_init;
const pwr_si_engine_input = @import("../../abi.zig").pwr_si_engine_input;
const pwr_si_engine_observe = @import("../engine/pwr_si_engine.zig").pwr_si_engine_observe;
const pwr_si_engine_output = @import("../../abi.zig").pwr_si_engine_output;
const pwr_si_engine_state = @import("../../abi.zig").pwr_si_engine_state;
const pwr_si_engine_state_hash = @import("../engine/pwr_si_engine.zig").pwr_si_engine_state_hash;
const pwr_si_engine_step = @import("../engine/pwr_si_engine.zig").pwr_si_engine_step;
const pwr_si_engine_validate_config = @import("../engine/pwr_si_engine.zig").pwr_si_engine_validate_config;
const tanh = @import("../../support.zig").tanh;

pub fn pwr_dct_powertrain_config_default(arg_config: [*c]pwr_dct_powertrain_config) callconv(.c) void {
    var config = arg_config;
    _ = &config;
    if (config == null) {
        return;
    }
    config.* = pwr_dct_powertrain_config{
        .engine = pwr_si_engine_config{
            .base_tick_ns = @as(u64, @bitCast(@as(i64, @as(c_int, 0)))),
            .sensor_period_ticks = @import("std").mem.zeroes(u32),
            .ecu_period_ticks = @import("std").mem.zeroes(u32),
            .maximum_sensor_age_ticks = @import("std").mem.zeroes(u32),
            .cylinder_count = @import("std").mem.zeroes(u32),
            .displacement_m3 = 0,
            .compression_ratio = 0,
            .rotational_inertia_kg_m2 = 0,
            .intake_manifold_volume_m3 = 0,
            .throttle_effective_area_m2 = 0,
            .throttle_discharge_coefficient = 0,
            .throttle_time_constant_s = 0,
            .volumetric_efficiency = 0,
            .air_gas_constant_j_per_kg_k = 0,
            .ambient_pressure_pa = 0,
            .ambient_temperature_k = 0,
            .stoichiometric_air_fuel_ratio = 0,
            .oxygen_mass_fraction = 0,
            .oxygen_per_fuel_kg_per_kg = 0,
            .fuel_lower_heating_value_j_per_kg = 0,
            .indicated_efficiency = 0,
            .exhaust_heat_fraction = 0,
            .injector_time_constant_s = 0,
            .friction_coulomb_nm = 0,
            .friction_viscous_nm_s_per_rad = 0,
            .pumping_scale = 0,
            .combustion_min_speed_rad_s = 0,
            .maximum_speed_rad_s = 0,
            .combustion_torque_ripple_fraction = 0,
            .coolant_initial_temperature_k = 0,
            .coolant_heat_capacity_j_per_k = 0,
            .coolant_ambient_conductance_w_per_k = 0,
            .exhaust_temperature_floor_k = 0,
            .exhaust_effective_heat_capacity_j_per_kg_k = 0,
            .exhaust_port_resistance_pa_per_kg_s_squared = 0,
            .low_voltage_nominal_v = 0,
            .low_voltage_internal_resistance_ohm = 0,
            .ecu_current_a = 0,
            .injector_current_a = 0,
            .starter_current_per_nm_a = 0,
            .ecu_brownout_threshold_v = 0,
        },
        .dct = @import("std").mem.zeroes(pwr_dct_config),
        .exhaust = @import("std").mem.zeroes(pwr_exhaust_parameters),
        .initial_ambient_pressure_pa = 0,
        .initial_ambient_temperature_k = 0,
        .initial_exhaust_wall_temperature_k = 0,
        .halfshaft_inertia_kg_m2 = 0,
        .rolling_resistance_torque_nm = 0,
        .viscous_road_load_nm_s_per_rad = 0,
        .quadratic_road_load_nm_s2_per_rad2 = 0,
        .road_load_regularization_speed_rad_s = 0,
        .starter_torque_nm = 0,
        .starter_release_speed_rad_s = 0,
        .maximum_cranking_time_s = 0,
        .cranking_throttle_command = 0,
        .idle_target_speed_rad_s = 0,
        .idle_throttle_command = 0,
        .idle_speed_throttle_gain_s_per_rad = 0,
        .launch_target_engine_speed_rad_s = 0,
        .launch_clamp_feedforward = 0,
        .launch_clamp_speed_gain_s_per_rad = 0,
        .launch_complete_halfshaft_speed_rad_s = 0,
        .first_to_second_shift_halfshaft_speed_rad_s = 0,
        .minimum_first_gear_time_s = 0,
        .first_to_second_shift_duration_s = 0,
        .shift_engine_throttle_scale = 0,
        .maximum_exhaust_mass_mismatch_kg_per_s = 0,
        .maximum_exhaust_enthalpy_mismatch_w = 0,
        .maximum_mechanical_coupling_mismatch_w = 0,
    };
    pwr_si_engine_config_default(&config.*.engine);
    pwr_dct_config_default(&config.*.dct);
    pwr_pt_exhaust_parameters_default(&config.*.exhaust);
    config.*.initial_ambient_pressure_pa = 101325.0;
    config.*.initial_ambient_temperature_k = 298.15;
    config.*.initial_exhaust_wall_temperature_k = 298.15;
    config.*.halfshaft_inertia_kg_m2 = 4.0;
    config.*.rolling_resistance_torque_nm = 8.0;
    config.*.viscous_road_load_nm_s_per_rad = 0.08;
    config.*.quadratic_road_load_nm_s2_per_rad2 = 0.004;
    config.*.road_load_regularization_speed_rad_s = 0.5;
    config.*.starter_torque_nm = 55.0;
    config.*.starter_release_speed_rad_s = 82.0;
    config.*.maximum_cranking_time_s = 1.5;
    config.*.cranking_throttle_command = 0.2;
    config.*.idle_target_speed_rad_s = 92.0;
    config.*.idle_throttle_command = 0.1;
    config.*.idle_speed_throttle_gain_s_per_rad = 0.002;
    config.*.launch_target_engine_speed_rad_s = 135.0;
    config.*.launch_clamp_feedforward = 0.16;
    config.*.launch_clamp_speed_gain_s_per_rad = 0.003;
    config.*.launch_complete_halfshaft_speed_rad_s = 7.0;
    config.*.first_to_second_shift_halfshaft_speed_rad_s = 35.0;
    config.*.minimum_first_gear_time_s = 0.3;
    config.*.first_to_second_shift_duration_s = 0.25;
    config.*.shift_engine_throttle_scale = 0.82;
    config.*.maximum_exhaust_mass_mismatch_kg_per_s = 0.025;
    config.*.maximum_exhaust_enthalpy_mismatch_w = 25000.0;
    config.*.maximum_mechanical_coupling_mismatch_w = 25000.0;
}

pub fn pwr_dct_powertrain_validate_config(arg_config: [*c]const pwr_dct_powertrain_config) callconv(.c) pwr_dct_powertrain_result {
    var config = arg_config;
    _ = &config;
    if (config == null) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_ARGUMENT));
    }
    if (((((config.*.engine.base_tick_ns != @as(u64, 100000)) or (pwr_si_engine_validate_config(&config.*.engine) != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) or (pwr_dct_validate_config(&config.*.dct) != @as(c_uint, @bitCast(PWR_DCT_OK)))) or (pwr_exhaust_validate_parameters(&config.*.exhaust) != @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) or (config.*.dct.maximum_timestep_s < pwr_dct_powertrain_tick_s)) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_CONFIG));
    }
    if (((((((((((((((((((((((((((((((!pwr_pt_positive_finite(config.*.initial_ambient_pressure_pa) or !pwr_pt_positive_finite(config.*.initial_ambient_temperature_k)) or !pwr_pt_positive_finite(config.*.initial_exhaust_wall_temperature_k)) or (config.*.initial_ambient_temperature_k < config.*.exhaust.minimum_temperature_k)) or (config.*.initial_exhaust_wall_temperature_k < config.*.exhaust.minimum_temperature_k)) or !pwr_pt_positive_finite(config.*.halfshaft_inertia_kg_m2)) or !pwr_pt_nonnegative_finite(config.*.rolling_resistance_torque_nm)) or !pwr_pt_nonnegative_finite(config.*.viscous_road_load_nm_s_per_rad)) or !pwr_pt_nonnegative_finite(config.*.quadratic_road_load_nm_s2_per_rad2)) or !pwr_pt_positive_finite(config.*.road_load_regularization_speed_rad_s)) or !pwr_pt_nonnegative_finite(config.*.starter_torque_nm)) or !pwr_pt_positive_finite(config.*.starter_release_speed_rad_s)) or !pwr_pt_positive_finite(config.*.maximum_cranking_time_s)) or !pwr_pt_nonnegative_finite(config.*.cranking_throttle_command)) or (config.*.cranking_throttle_command > 1.0)) or !pwr_pt_positive_finite(config.*.idle_target_speed_rad_s)) or !pwr_pt_nonnegative_finite(config.*.idle_throttle_command)) or (config.*.idle_throttle_command > 1.0)) or !pwr_pt_nonnegative_finite(config.*.idle_speed_throttle_gain_s_per_rad)) or !pwr_pt_positive_finite(config.*.launch_target_engine_speed_rad_s)) or !pwr_pt_nonnegative_finite(config.*.launch_clamp_feedforward)) or !pwr_pt_nonnegative_finite(config.*.launch_clamp_speed_gain_s_per_rad)) or !pwr_pt_positive_finite(config.*.launch_complete_halfshaft_speed_rad_s)) or !pwr_pt_positive_finite(config.*.first_to_second_shift_halfshaft_speed_rad_s)) or (config.*.first_to_second_shift_halfshaft_speed_rad_s <= config.*.launch_complete_halfshaft_speed_rad_s)) or !pwr_pt_nonnegative_finite(config.*.minimum_first_gear_time_s)) or !pwr_pt_positive_finite(config.*.first_to_second_shift_duration_s)) or !pwr_pt_positive_finite(config.*.shift_engine_throttle_scale)) or (config.*.shift_engine_throttle_scale > 1.0)) or !pwr_pt_nonnegative_finite(config.*.maximum_exhaust_mass_mismatch_kg_per_s)) or !pwr_pt_nonnegative_finite(config.*.maximum_exhaust_enthalpy_mismatch_w)) or !pwr_pt_nonnegative_finite(config.*.maximum_mechanical_coupling_mismatch_w)) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_CONFIG));
    }
    return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK));
}

pub fn pwr_dct_powertrain_init(arg_state: [*c]pwr_dct_powertrain_state, arg_config: [*c]const pwr_dct_powertrain_config) callconv(.c) pwr_dct_powertrain_result {
    var state = arg_state;
    _ = &state;
    var config = arg_config;
    _ = &config;
    var candidate: pwr_dct_powertrain_state = pwr_dct_powertrain_state{
        .config = pwr_dct_powertrain_config{
            .engine = pwr_si_engine_config{
                .base_tick_ns = @as(u64, @bitCast(@as(i64, @as(c_int, 0)))),
                .sensor_period_ticks = @import("std").mem.zeroes(u32),
                .ecu_period_ticks = @import("std").mem.zeroes(u32),
                .maximum_sensor_age_ticks = @import("std").mem.zeroes(u32),
                .cylinder_count = @import("std").mem.zeroes(u32),
                .displacement_m3 = 0,
                .compression_ratio = 0,
                .rotational_inertia_kg_m2 = 0,
                .intake_manifold_volume_m3 = 0,
                .throttle_effective_area_m2 = 0,
                .throttle_discharge_coefficient = 0,
                .throttle_time_constant_s = 0,
                .volumetric_efficiency = 0,
                .air_gas_constant_j_per_kg_k = 0,
                .ambient_pressure_pa = 0,
                .ambient_temperature_k = 0,
                .stoichiometric_air_fuel_ratio = 0,
                .oxygen_mass_fraction = 0,
                .oxygen_per_fuel_kg_per_kg = 0,
                .fuel_lower_heating_value_j_per_kg = 0,
                .indicated_efficiency = 0,
                .exhaust_heat_fraction = 0,
                .injector_time_constant_s = 0,
                .friction_coulomb_nm = 0,
                .friction_viscous_nm_s_per_rad = 0,
                .pumping_scale = 0,
                .combustion_min_speed_rad_s = 0,
                .maximum_speed_rad_s = 0,
                .combustion_torque_ripple_fraction = 0,
                .coolant_initial_temperature_k = 0,
                .coolant_heat_capacity_j_per_k = 0,
                .coolant_ambient_conductance_w_per_k = 0,
                .exhaust_temperature_floor_k = 0,
                .exhaust_effective_heat_capacity_j_per_kg_k = 0,
                .exhaust_port_resistance_pa_per_kg_s_squared = 0,
                .low_voltage_nominal_v = 0,
                .low_voltage_internal_resistance_ohm = 0,
                .ecu_current_a = 0,
                .injector_current_a = 0,
                .starter_current_per_nm_a = 0,
                .ecu_brownout_threshold_v = 0,
            },
            .dct = @import("std").mem.zeroes(pwr_dct_config),
            .exhaust = @import("std").mem.zeroes(pwr_exhaust_parameters),
            .initial_ambient_pressure_pa = 0,
            .initial_ambient_temperature_k = 0,
            .initial_exhaust_wall_temperature_k = 0,
            .halfshaft_inertia_kg_m2 = 0,
            .rolling_resistance_torque_nm = 0,
            .viscous_road_load_nm_s_per_rad = 0,
            .quadratic_road_load_nm_s2_per_rad2 = 0,
            .road_load_regularization_speed_rad_s = 0,
            .starter_torque_nm = 0,
            .starter_release_speed_rad_s = 0,
            .maximum_cranking_time_s = 0,
            .cranking_throttle_command = 0,
            .idle_target_speed_rad_s = 0,
            .idle_throttle_command = 0,
            .idle_speed_throttle_gain_s_per_rad = 0,
            .launch_target_engine_speed_rad_s = 0,
            .launch_clamp_feedforward = 0,
            .launch_clamp_speed_gain_s_per_rad = 0,
            .launch_complete_halfshaft_speed_rad_s = 0,
            .first_to_second_shift_halfshaft_speed_rad_s = 0,
            .minimum_first_gear_time_s = 0,
            .first_to_second_shift_duration_s = 0,
            .shift_engine_throttle_scale = 0,
            .maximum_exhaust_mass_mismatch_kg_per_s = 0,
            .maximum_exhaust_enthalpy_mismatch_w = 0,
            .maximum_mechanical_coupling_mismatch_w = 0,
        },
        .engine = @import("std").mem.zeroes(pwr_si_engine_state),
        .dct = @import("std").mem.zeroes(pwr_dct),
        .exhaust = @import("std").mem.zeroes(pwr_exhaust_state),
        .engine_output = @import("std").mem.zeroes(pwr_si_engine_output),
        .dct_output = @import("std").mem.zeroes(pwr_dct_snapshot),
        .exhaust_output = @import("std").mem.zeroes(pwr_exhaust_observation),
        .exhaust_step = @import("std").mem.zeroes(pwr_exhaust_diagnostics),
        .tick = @import("std").mem.zeroes(u64),
        .tcu_phase_tick = @import("std").mem.zeroes(u64),
        .cranking_ticks = @import("std").mem.zeroes(u64),
        .last_control_mode = @import("std").mem.zeroes(pwr_dct_powertrain_control_mode),
        .tcu_phase = @import("std").mem.zeroes(pwr_dct_powertrain_tcu_phase),
        .engine_started = false,
        .halfshaft_angle_rad = 0,
        .halfshaft_speed_rad_s = 0,
        .initial_halfshaft_kinetic_energy_j = 0,
        .cumulative_halfshaft_drive_work_j = 0,
        .cumulative_road_load_work_j = 0,
        .cumulative_halfshaft_numerical_adjustment_j = 0,
        .last_commanded_engine_throttle = 0,
        .last_commanded_starter_torque_nm = 0,
        .last_commanded_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
        .last_commanded_clutch_clamp = @import("std").mem.zeroes([2]f64),
        .last_road_load_torque_nm = 0,
        .last_exhaust_mass_mismatch_kg = 0,
        .cumulative_exhaust_mass_mismatch_kg = 0,
        .last_exhaust_enthalpy_mismatch_j = 0,
        .cumulative_exhaust_enthalpy_mismatch_j = 0,
        .last_engine_dct_energy_mismatch_j = 0,
        .cumulative_engine_dct_energy_mismatch_j = 0,
        .last_dct_output_energy_mismatch_j = 0,
        .cumulative_dct_output_energy_mismatch_j = 0,
        .last_step_diagnostic_flags = @import("std").mem.zeroes(u32),
        .diagnostic_flags = @import("std").mem.zeroes(u32),
    };
    _ = &candidate;
    var ambient: pwr_exhaust_reservoir = undefined;
    _ = &ambient;
    if (state == null) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_ARGUMENT));
    }
    if (pwr_dct_powertrain_validate_config(config) != @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK))) {
        return @as(c_uint, @bitCast(if (config == null) PWR_DCT_POWERTRAIN_INVALID_ARGUMENT else PWR_DCT_POWERTRAIN_INVALID_CONFIG));
    }
    candidate.config = config.*;
    if ((pwr_si_engine_init(&candidate.engine, &config.*.engine) != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK))) or (pwr_dct_init(&candidate.dct, &config.*.dct) != @as(c_uint, @bitCast(PWR_DCT_OK)))) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_SUBMODEL_ERROR));
    }
    ambient = pwr_pt_ambient_reservoir(config.*.initial_ambient_pressure_pa, config.*.initial_ambient_temperature_k);
    if (pwr_exhaust_initialize_uniform(&config.*.exhaust, &ambient, config.*.initial_exhaust_wall_temperature_k, &candidate.exhaust) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_SUBMODEL_ERROR));
    }
    if (((pwr_si_engine_observe(&candidate.engine, &candidate.engine_output) != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK))) or (pwr_dct_snapshot_read(&candidate.dct, &candidate.dct_output) != @as(c_uint, @bitCast(PWR_DCT_OK)))) or (pwr_exhaust_observe(&config.*.exhaust, &candidate.exhaust, &candidate.exhaust_output) != @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_SUBMODEL_ERROR));
    }
    candidate.last_control_mode = @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED));
    candidate.tcu_phase = @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_NEUTRAL));
    state.* = candidate;
    return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK));
}

pub fn pwr_dct_powertrain_step(arg_state: [*c]pwr_dct_powertrain_state, arg_input: [*c]const pwr_dct_powertrain_input, arg_output: [*c]pwr_dct_powertrain_output) callconv(.c) pwr_dct_powertrain_result {
    var state = arg_state;
    _ = &state;
    var input = arg_input;
    _ = &input;
    var output = arg_output;
    _ = &output;
    var candidate: pwr_dct_powertrain_state = undefined;
    _ = &candidate;
    var commands: pwr_dct_powertrain_commands = undefined;
    _ = &commands;
    var exhaust_input: pwr_exhaust_inputs = undefined;
    _ = &exhaust_input;
    var engine_input: pwr_si_engine_input = undefined;
    _ = &engine_input;
    var dct_input: pwr_dct_inputs = undefined;
    _ = &dct_input;
    var next_engine_output: pwr_si_engine_output = undefined;
    _ = &next_engine_output;
    var next_dct_output: pwr_dct_snapshot = undefined;
    _ = &next_dct_output;
    var next_exhaust_step: pwr_exhaust_diagnostics = undefined;
    _ = &next_exhaust_step;
    const dt: f64 = pwr_dct_powertrain_tick_s;
    _ = &dt;
    var engine_load_work_before: f64 = undefined;
    _ = &engine_load_work_before;
    var dct_input_energy_before: f64 = undefined;
    _ = &dct_input_energy_before;
    var dct_output_energy_before: f64 = undefined;
    _ = &dct_output_energy_before;
    var old_halfshaft_speed: f64 = undefined;
    _ = &old_halfshaft_speed;
    var next_halfshaft_speed: f64 = undefined;
    _ = &next_halfshaft_speed;
    var average_halfshaft_speed: f64 = undefined;
    _ = &average_halfshaft_speed;
    var drive_work: f64 = undefined;
    _ = &drive_work;
    var road_work: f64 = undefined;
    _ = &road_work;
    var kinetic_change: f64 = undefined;
    _ = &kinetic_change;
    var halfshaft_adjustment: f64 = undefined;
    _ = &halfshaft_adjustment;
    var source_mass_flow: f64 = undefined;
    _ = &source_mass_flow;
    var exhaust_inlet_mass_flow: f64 = undefined;
    _ = &exhaust_inlet_mass_flow;
    var source_enthalpy_flow: f64 = undefined;
    _ = &source_enthalpy_flow;
    var accepted_enthalpy_flow: f64 = undefined;
    _ = &accepted_enthalpy_flow;
    var step_flags: u32 = @as(u32, @bitCast(PWR_DCT_POWERTRAIN_DIAG_NONE));
    _ = &step_flags;
    if ((state == null) or (output == null)) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_ARGUMENT));
    }
    if (pwr_dct_powertrain_validate_config(&state.*.config) != @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK))) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_CONFIG));
    }
    if (!pwr_pt_input_valid(input) or (input.*.ambient_temperature_k < state.*.config.exhaust.minimum_temperature_k)) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_INPUT));
    }
    candidate = state.*;
    pwr_pt_commands(&candidate, input, &commands, &step_flags);
    exhaust_input = pwr_exhaust_inputs{
        .upstream = pwr_pt_engine_exhaust_reservoir(&candidate.engine_output, input.*.ambient_pressure_pa, state.*.config.exhaust.minimum_temperature_k),
        .downstream = pwr_pt_ambient_reservoir(input.*.ambient_pressure_pa, input.*.ambient_temperature_k),
        .inlet_area_scale = 1.0,
        .tailpipe_area_scale = input.*.tailpipe_area_scale,
        .ambient_temperature_k = input.*.ambient_temperature_k,
    };
    if (pwr_exhaust_step(&candidate.config.exhaust, &candidate.exhaust, &exhaust_input, dt, &next_exhaust_step) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_SUBMODEL_ERROR));
    }
    candidate.exhaust_step = next_exhaust_step;
    candidate.exhaust_output = next_exhaust_step.final_observation;
    engine_load_work_before = candidate.engine_output.mechanical_load_work_j;
    engine_input = pwr_si_engine_input{
        .driver_throttle = commands.engine_throttle,
        .load_torque_nm = candidate.dct_output.input_transmitted_torque_nm,
        .starter_torque_nm = commands.starter_torque_nm,
        .exhaust_backpressure_pa = pwr_pt_max(candidate.exhaust_output.pressure_pa[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))], 1.0),
        .ambient_pressure_pa = input.*.ambient_pressure_pa,
        .ambient_temperature_k = input.*.ambient_temperature_k,
        .fault_mask = input.*.engine_fault_mask,
        .ignition_on = input.*.ignition_on,
    };
    if (pwr_si_engine_step(&candidate.engine, &engine_input, &next_engine_output) != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK))) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_SUBMODEL_ERROR));
    }
    candidate.engine_output = next_engine_output;
    dct_input_energy_before = candidate.dct_output.cumulative_input_energy_j;
    dct_output_energy_before = candidate.dct_output.cumulative_output_energy_j;
    dct_input = pwr_dct_inputs{
        .engine_speed_rad_s = candidate.engine_output.crank_speed_rad_s,
        .output_speed_rad_s = candidate.halfshaft_speed_rad_s,
        .engaged_gear_mask = commands.gear_mask,
        .clutch_clamp_command = [2]f64{
            commands.clutch_clamp[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))],
            commands.clutch_clamp[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))],
        },
        .ambient_temperature_k = input.*.ambient_temperature_k,
    };
    if ((pwr_dct_step(&candidate.dct, &dct_input, dt) != @as(c_uint, @bitCast(PWR_DCT_OK))) or (pwr_dct_snapshot_read(&candidate.dct, &next_dct_output) != @as(c_uint, @bitCast(PWR_DCT_OK)))) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_SUBMODEL_ERROR));
    }
    candidate.dct_output = next_dct_output;
    old_halfshaft_speed = candidate.halfshaft_speed_rad_s;
    candidate.last_road_load_torque_nm = pwr_pt_road_load(&candidate.config, input, old_halfshaft_speed);
    next_halfshaft_speed = old_halfshaft_speed + (((candidate.dct_output.output_torque_nm - candidate.last_road_load_torque_nm) / candidate.config.halfshaft_inertia_kg_m2) * dt);
    if (next_halfshaft_speed < 0.0) {
        next_halfshaft_speed = 0.0;
        step_flags |= @as(u32, @bitCast(PWR_DCT_POWERTRAIN_DIAG_HALFSHAFT_SPEED_CLAMP));
    }
    average_halfshaft_speed = 0.5 * (old_halfshaft_speed + next_halfshaft_speed);
    drive_work = (candidate.dct_output.output_torque_nm * average_halfshaft_speed) * dt;
    road_work = (candidate.last_road_load_torque_nm * average_halfshaft_speed) * dt;
    kinetic_change = (0.5 * candidate.config.halfshaft_inertia_kg_m2) * ((next_halfshaft_speed * next_halfshaft_speed) - (old_halfshaft_speed * old_halfshaft_speed));
    halfshaft_adjustment = (drive_work - road_work) - kinetic_change;
    candidate.halfshaft_speed_rad_s = next_halfshaft_speed;
    candidate.halfshaft_angle_rad += average_halfshaft_speed * dt;
    candidate.cumulative_halfshaft_drive_work_j += drive_work;
    candidate.cumulative_road_load_work_j += road_work;
    candidate.cumulative_halfshaft_numerical_adjustment_j += halfshaft_adjustment;
    source_mass_flow = state.*.engine_output.exhaust_mass_flow_kg_per_s;
    exhaust_inlet_mass_flow = next_exhaust_step.average_path_mass_flow_kg_per_s[@as(c_uint, @intCast(PWR_EXHAUST_PATH_INLET))];
    candidate.last_exhaust_mass_mismatch_kg = (source_mass_flow - exhaust_inlet_mass_flow) * dt;
    candidate.cumulative_exhaust_mass_mismatch_kg += candidate.last_exhaust_mass_mismatch_kg;
    source_enthalpy_flow = (source_mass_flow * pwr_pt_source_cp(&candidate.config.exhaust, &state.*.engine_output)) * exhaust_input.upstream.temperature_k;
    accepted_enthalpy_flow = (exhaust_inlet_mass_flow * pwr_pt_source_cp(&candidate.config.exhaust, &state.*.engine_output)) * exhaust_input.upstream.temperature_k;
    candidate.last_exhaust_enthalpy_mismatch_j = (source_enthalpy_flow - accepted_enthalpy_flow) * dt;
    candidate.cumulative_exhaust_enthalpy_mismatch_j += candidate.last_exhaust_enthalpy_mismatch_j;
    candidate.last_engine_dct_energy_mismatch_j = (candidate.engine_output.mechanical_load_work_j - engine_load_work_before) - (candidate.dct_output.cumulative_input_energy_j - dct_input_energy_before);
    candidate.cumulative_engine_dct_energy_mismatch_j += candidate.last_engine_dct_energy_mismatch_j;
    candidate.last_dct_output_energy_mismatch_j = (candidate.dct_output.cumulative_output_energy_j - dct_output_energy_before) - drive_work;
    candidate.cumulative_dct_output_energy_mismatch_j += candidate.last_dct_output_energy_mismatch_j;
    if ((fabs(candidate.last_exhaust_mass_mismatch_kg) / dt) > candidate.config.maximum_exhaust_mass_mismatch_kg_per_s) {
        step_flags |= @as(u32, @bitCast(PWR_DCT_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH));
    }
    if ((fabs(candidate.last_exhaust_enthalpy_mismatch_j) / dt) > candidate.config.maximum_exhaust_enthalpy_mismatch_w) {
        step_flags |= @as(u32, @bitCast(PWR_DCT_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH));
    }
    if ((fabs(candidate.last_engine_dct_energy_mismatch_j) / dt) > candidate.config.maximum_mechanical_coupling_mismatch_w) {
        step_flags |= @as(u32, @bitCast(PWR_DCT_POWERTRAIN_DIAG_ENGINE_DCT_ENERGY_MISMATCH));
    }
    if ((fabs(candidate.last_dct_output_energy_mismatch_j) / dt) > candidate.config.maximum_mechanical_coupling_mismatch_w) {
        step_flags |= @as(u32, @bitCast(PWR_DCT_POWERTRAIN_DIAG_DCT_OUTPUT_ENERGY_MISMATCH));
    }
    candidate.last_commanded_engine_throttle = commands.engine_throttle;
    candidate.last_commanded_starter_torque_nm = commands.starter_torque_nm;
    candidate.last_commanded_gear_mask = commands.gear_mask;
    {
        var clutch: usize = 0;
        _ = &clutch;
        while (clutch < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (clutch +%= 1) {
            candidate.last_commanded_clutch_clamp[clutch] = commands.clutch_clamp[clutch];
        }
    }
    candidate.last_step_diagnostic_flags = step_flags;
    candidate.diagnostic_flags |= step_flags;
    candidate.tick +%= 1;
    candidate.tcu_phase_tick +%= 1;
    if (!pwr_pt_state_finite(&candidate)) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_NUMERIC_ERROR));
    }
    if (pwr_dct_powertrain_observe(&candidate, output) != @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK))) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_NUMERIC_ERROR));
    }
    state.* = candidate;
    return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK));
}

pub fn pwr_dct_powertrain_observe(arg_state: [*c]const pwr_dct_powertrain_state, arg_output: [*c]pwr_dct_powertrain_output) callconv(.c) pwr_dct_powertrain_result {
    var state = arg_state;
    _ = &state;
    var output = arg_output;
    _ = &output;
    var kinetic: f64 = undefined;
    _ = &kinetic;
    var kinetic_change: f64 = undefined;
    _ = &kinetic_change;
    if ((state == null) or (output == null)) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_ARGUMENT));
    }
    if (pwr_dct_powertrain_validate_config(&state.*.config) != @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK))) {
        return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_CONFIG));
    }
    kinetic = ((0.5 * state.*.config.halfshaft_inertia_kg_m2) * state.*.halfshaft_speed_rad_s) * state.*.halfshaft_speed_rad_s;
    kinetic_change = kinetic - state.*.initial_halfshaft_kinetic_energy_j;
    output.* = pwr_dct_powertrain_output{
        .time_s = @as(f64, @floatFromInt(state.*.tick)) * pwr_dct_powertrain_tick_s,
        .tick = state.*.tick,
        .control_mode = state.*.last_control_mode,
        .tcu_phase = state.*.tcu_phase,
        .shift_progress = pwr_pt_shift_progress(state),
        .commanded_engine_throttle = state.*.last_commanded_engine_throttle,
        .commanded_starter_torque_nm = state.*.last_commanded_starter_torque_nm,
        .commanded_gear_mask = state.*.last_commanded_gear_mask,
        .commanded_clutch_clamp = @import("std").mem.zeroes([2]f64),
        .engine = state.*.engine_output,
        .dct = state.*.dct_output,
        .exhaust = state.*.exhaust_output,
        .exhaust_step = state.*.exhaust_step,
        .halfshaft_angle_rad = state.*.halfshaft_angle_rad,
        .halfshaft_speed_rad_s = state.*.halfshaft_speed_rad_s,
        .halfshaft_drive_torque_nm = state.*.dct_output.output_torque_nm,
        .road_load_torque_nm = state.*.last_road_load_torque_nm,
        .halfshaft_kinetic_energy_j = kinetic,
        .cumulative_halfshaft_drive_work_j = state.*.cumulative_halfshaft_drive_work_j,
        .cumulative_road_load_work_j = state.*.cumulative_road_load_work_j,
        .cumulative_halfshaft_numerical_adjustment_j = state.*.cumulative_halfshaft_numerical_adjustment_j,
        .halfshaft_energy_residual_j = ((state.*.cumulative_halfshaft_drive_work_j - state.*.cumulative_road_load_work_j) - kinetic_change) - state.*.cumulative_halfshaft_numerical_adjustment_j,
        .exhaust_mass_mismatch_kg = state.*.last_exhaust_mass_mismatch_kg,
        .cumulative_exhaust_mass_mismatch_kg = state.*.cumulative_exhaust_mass_mismatch_kg,
        .exhaust_enthalpy_mismatch_j = state.*.last_exhaust_enthalpy_mismatch_j,
        .cumulative_exhaust_enthalpy_mismatch_j = state.*.cumulative_exhaust_enthalpy_mismatch_j,
        .engine_dct_energy_mismatch_j = state.*.last_engine_dct_energy_mismatch_j,
        .cumulative_engine_dct_energy_mismatch_j = state.*.cumulative_engine_dct_energy_mismatch_j,
        .dct_output_energy_mismatch_j = state.*.last_dct_output_energy_mismatch_j,
        .cumulative_dct_output_energy_mismatch_j = state.*.cumulative_dct_output_energy_mismatch_j,
        .last_step_diagnostic_flags = state.*.last_step_diagnostic_flags,
        .diagnostic_flags = state.*.diagnostic_flags,
        .state_hash = @import("std").mem.zeroes(u64),
    };
    {
        var clutch: usize = 0;
        _ = &clutch;
        while (clutch < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (clutch +%= 1) {
            output.*.commanded_clutch_clamp[clutch] = state.*.last_commanded_clutch_clamp[clutch];
        }
    }
    output.*.state_hash = pwr_dct_powertrain_state_hash(state);
    return @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK));
}

pub fn pwr_dct_powertrain_state_hash(arg_state: [*c]const pwr_dct_powertrain_state) callconv(.c) u64 {
    var state = arg_state;
    _ = &state;
    var hash: u64 = 14695981039346656037;
    _ = &hash;
    if (state == null) {
        return 0;
    }
    hash = pwr_pt_hash_u64(hash, pwr_si_engine_state_hash(&state.*.engine));
    hash = pwr_pt_hash_u64(hash, pwr_dct_state_hash(&state.*.dct));
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            hash = pwr_pt_hash_double(hash, state.*.exhaust.volume[volume].total_mass_kg);
            {
                var species: usize = 0;
                _ = &species;
                while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
                    hash = pwr_pt_hash_double(hash, state.*.exhaust.volume[volume].species_mass_kg[species]);
                }
            }
            hash = pwr_pt_hash_double(hash, state.*.exhaust.volume[volume].internal_energy_j);
            hash = pwr_pt_hash_double(hash, state.*.exhaust.volume[volume].wall_temperature_k);
        }
    }
    hash = pwr_pt_hash_double(hash, state.*.exhaust.catalyst_conversion_fraction);
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            hash = pwr_pt_hash_double(hash, state.*.exhaust.cumulative_tailpipe_species_kg[species]);
        }
    }
    hash = pwr_pt_hash_double(hash, state.*.exhaust.time_s);
    hash = pwr_pt_hash_u64(hash, state.*.tick);
    hash = pwr_pt_hash_u64(hash, state.*.tcu_phase_tick);
    hash = pwr_pt_hash_u64(hash, state.*.cranking_ticks);
    hash = pwr_pt_hash_u64(hash, @as(u64, @bitCast(@as(u64, state.*.last_control_mode))));
    hash = pwr_pt_hash_u64(hash, @as(u64, @bitCast(@as(u64, state.*.tcu_phase))));
    hash = pwr_pt_hash_u64(hash, @as(u64, @bitCast(@as(u64, if (@as(c_int, @intFromBool(state.*.engine_started)) != 0) @as(c_uint, 1) else @as(c_uint, 0)))));
    hash = pwr_pt_hash_double(hash, state.*.halfshaft_angle_rad);
    hash = pwr_pt_hash_double(hash, state.*.halfshaft_speed_rad_s);
    hash = pwr_pt_hash_double(hash, state.*.cumulative_halfshaft_drive_work_j);
    hash = pwr_pt_hash_double(hash, state.*.cumulative_road_load_work_j);
    hash = pwr_pt_hash_double(hash, state.*.cumulative_halfshaft_numerical_adjustment_j);
    hash = pwr_pt_hash_double(hash, state.*.cumulative_exhaust_mass_mismatch_kg);
    hash = pwr_pt_hash_double(hash, state.*.cumulative_exhaust_enthalpy_mismatch_j);
    hash = pwr_pt_hash_double(hash, state.*.cumulative_engine_dct_energy_mismatch_j);
    hash = pwr_pt_hash_double(hash, state.*.cumulative_dct_output_energy_mismatch_j);
    hash = pwr_pt_hash_u64(hash, @as(u64, @bitCast(@as(u64, state.*.last_step_diagnostic_flags))));
    hash = pwr_pt_hash_u64(hash, @as(u64, @bitCast(@as(u64, state.*.diagnostic_flags))));
    return hash;
}

pub fn pwr_dct_powertrain_result_string(arg_result: pwr_dct_powertrain_result) callconv(.c) [*c]const u8 {
    var result = arg_result;
    _ = &result;
    while (true) {
        switch (result) {
            @as(c_uint, @bitCast(@as(c_int, 0))) => return "ok",
            @as(c_uint, @bitCast(@as(c_int, 1))) => return "invalid argument",
            @as(c_uint, @bitCast(@as(c_int, 2))) => return "invalid config",
            @as(c_uint, @bitCast(@as(c_int, 3))) => return "invalid input",
            @as(c_uint, @bitCast(@as(c_int, 4))) => return "submodel error",
            @as(c_uint, @bitCast(@as(c_int, 5))) => return "numeric error",
            else => return "unknown result",
        }
        break;
    }
    return null;
}

pub fn pwr_pt_positive_finite(arg_value: f64) callconv(.c) bool {
    var value = arg_value;
    _ = &value;
    return (power_isfinite(value) != 0) and (value > 0.0);
}

pub fn pwr_pt_nonnegative_finite(arg_value: f64) callconv(.c) bool {
    var value = arg_value;
    _ = &value;
    return (power_isfinite(value) != 0) and (value >= 0.0);
}

pub fn pwr_pt_min(arg_left: f64, arg_right: f64) callconv(.c) f64 {
    var left = arg_left;
    _ = &left;
    var right = arg_right;
    _ = &right;
    return if (left < right) left else right;
}

pub fn pwr_pt_max(arg_left: f64, arg_right: f64) callconv(.c) f64 {
    var left = arg_left;
    _ = &left;
    var right = arg_right;
    _ = &right;
    return if (left > right) left else right;
}

pub fn pwr_pt_clamp(arg_value: f64, arg_minimum: f64, arg_maximum: f64) callconv(.c) f64 {
    var value = arg_value;
    _ = &value;
    var minimum = arg_minimum;
    _ = &minimum;
    var maximum = arg_maximum;
    _ = &maximum;
    return pwr_pt_min(pwr_pt_max(value, minimum), maximum);
}

pub fn pwr_pt_ambient_reservoir(arg_pressure_pa: f64, arg_temperature_k: f64) callconv(.c) pwr_exhaust_reservoir {
    var pressure_pa = arg_pressure_pa;
    _ = &pressure_pa;
    var temperature_k = arg_temperature_k;
    _ = &temperature_k;
    var reservoir: pwr_exhaust_reservoir = pwr_exhaust_reservoir{
        .pressure_pa = pressure_pa,
        .temperature_k = temperature_k,
        .species_mass_fraction = @import("std").mem.zeroes([4]f64),
    };
    _ = &reservoir;
    reservoir.species_mass_fraction[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_INERT))] = 0.77;
    reservoir.species_mass_fraction[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_OXYGEN))] = 0.23;
    return reservoir;
}

pub fn pwr_pt_exhaust_parameters_default(arg_parameters: [*c]pwr_exhaust_parameters) callconv(.c) void {
    var parameters = arg_parameters;
    _ = &parameters;
    parameters.* = pwr_exhaust_parameters{
        .volume_m3 = [1]f64{
            0,
        } ++ [1]f64{0} ** 2,
        .wall_heat_capacity_j_per_k = @import("std").mem.zeroes([3]f64),
        .gas_wall_conductance_w_per_k = @import("std").mem.zeroes([3]f64),
        .wall_ambient_conductance_w_per_k = @import("std").mem.zeroes([3]f64),
        .orifice_area_m2 = @import("std").mem.zeroes([4]f64),
        .discharge_coefficient = @import("std").mem.zeroes([4]f64),
        .species_gas_constant_j_per_kg_k = @import("std").mem.zeroes([4]f64),
        .species_cv_j_per_kg_k = @import("std").mem.zeroes([4]f64),
        .catalyst_light_off_temperature_k = 0,
        .catalyst_full_activity_temperature_k = 0,
        .catalyst_activity_time_constant_s = 0,
        .catalyst_reaction_time_s = 0,
        .catalyst_oxygen_per_reductant_kg_per_kg = 0,
        .catalyst_reaction_heat_j_per_kg_reductant = 0,
        .catalyst_heat_to_wall_fraction = 0,
        .minimum_temperature_k = 0,
        .minimum_volume_mass_kg = 0,
        .maximum_substep_s = 0,
        .maximum_outflow_fraction_per_substep = 0,
        .maximum_substeps = @import("std").mem.zeroes(u32),
    };
    parameters.*.volume_m3[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] = 0.0015;
    parameters.*.volume_m3[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 0.0025;
    parameters.*.volume_m3[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MUFFLER))] = 0.004;
    parameters.*.wall_heat_capacity_j_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] = 300.0;
    parameters.*.wall_heat_capacity_j_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 180.0;
    parameters.*.wall_heat_capacity_j_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MUFFLER))] = 500.0;
    parameters.*.gas_wall_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] = 18.0;
    parameters.*.gas_wall_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 75.0;
    parameters.*.gas_wall_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MUFFLER))] = 15.0;
    parameters.*.wall_ambient_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] = 1.0;
    parameters.*.wall_ambient_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 1.5;
    parameters.*.wall_ambient_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MUFFLER))] = 2.0;
    parameters.*.orifice_area_m2[@as(c_uint, @intCast(PWR_EXHAUST_PATH_INLET))] = 0.00006;
    parameters.*.orifice_area_m2[@as(c_uint, @intCast(PWR_EXHAUST_PATH_MANIFOLD_TO_CATALYST))] = 0.00005;
    parameters.*.orifice_area_m2[@as(c_uint, @intCast(PWR_EXHAUST_PATH_CATALYST_TO_MUFFLER))] = 0.00006;
    parameters.*.orifice_area_m2[@as(c_uint, @intCast(PWR_EXHAUST_PATH_TAILPIPE))] = 0.00008;
    {
        var path: usize = 0;
        _ = &path;
        while (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_COUNT)))) : (path +%= 1) {
            parameters.*.discharge_coefficient[path] = 0.72;
        }
    }
    parameters.*.species_gas_constant_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_INERT))] = 287.0;
    parameters.*.species_gas_constant_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_OXYGEN))] = 260.0;
    parameters.*.species_gas_constant_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_REDUCTANT))] = 245.0;
    parameters.*.species_gas_constant_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_PRODUCTS))] = 190.0;
    parameters.*.species_cv_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_INERT))] = 720.0;
    parameters.*.species_cv_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_OXYGEN))] = 660.0;
    parameters.*.species_cv_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_REDUCTANT))] = 1500.0;
    parameters.*.species_cv_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_PRODUCTS))] = 900.0;
    parameters.*.catalyst_light_off_temperature_k = 500.0;
    parameters.*.catalyst_full_activity_temperature_k = 680.0;
    parameters.*.catalyst_activity_time_constant_s = 0.35;
    parameters.*.catalyst_reaction_time_s = 0.04;
    parameters.*.catalyst_oxygen_per_reductant_kg_per_kg = 3.0;
    parameters.*.catalyst_reaction_heat_j_per_kg_reductant = 20000000.0;
    parameters.*.catalyst_heat_to_wall_fraction = 0.75;
    parameters.*.minimum_temperature_k = 150.0;
    parameters.*.minimum_volume_mass_kg = 0.000000001;
    parameters.*.maximum_substep_s = pwr_dct_powertrain_tick_s;
    parameters.*.maximum_outflow_fraction_per_substep = 0.1;
    parameters.*.maximum_substeps = 8;
}

pub fn pwr_pt_input_valid(arg_input: [*c]const pwr_dct_powertrain_input) callconv(.c) bool {
    var input = arg_input;
    _ = &input;
    const known_engine_faults: u32 = @as(u32, @bitCast(((PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT | PWR_SI_ENGINE_FAULT_THROTTLE_STUCK) | PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED) | PWR_SI_ENGINE_FAULT_LOW_VOLTAGE));
    _ = &known_engine_faults;
    if ((((((((((input == null) or ((input.*.control_mode != @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED))) and (input.*.control_mode != @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_CONTROL_DIRECT))))) or !(power_isfinite(input.*.driver_throttle) != 0)) or (input.*.driver_throttle < 0.0)) or (input.*.driver_throttle > 1.0)) or !pwr_pt_positive_finite(input.*.ambient_pressure_pa)) or !pwr_pt_positive_finite(input.*.ambient_temperature_k)) or !pwr_pt_nonnegative_finite(input.*.additional_road_load_torque_nm)) or !pwr_pt_nonnegative_finite(input.*.tailpipe_area_scale)) or ((input.*.engine_fault_mask & ~known_engine_faults) != @as(c_uint, 0))) {
        return @as(c_int, 0) != 0;
    }
    if (input.*.control_mode == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_CONTROL_DIRECT))) {
        if (!pwr_pt_nonnegative_finite(input.*.direct_starter_torque_nm)) {
            return @as(c_int, 0) != 0;
        }
        {
            var clutch: usize = 0;
            _ = &clutch;
            while (clutch < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (clutch +%= 1) {
                if ((!(power_isfinite(input.*.direct_clutch_clamp[clutch]) != 0) or (input.*.direct_clutch_clamp[clutch] < 0.0)) or (input.*.direct_clutch_clamp[clutch] > 1.0)) {
                    return @as(c_int, 0) != 0;
                }
            }
        }
    }
    return @as(c_int, 1) != 0;
}

pub fn pwr_pt_transition(arg_state: [*c]pwr_dct_powertrain_state, arg_phase: pwr_dct_powertrain_tcu_phase) callconv(.c) void {
    var state = arg_state;
    _ = &state;
    var phase = arg_phase;
    _ = &phase;
    if (state.*.tcu_phase != phase) {
        state.*.tcu_phase = phase;
        state.*.tcu_phase_tick = 0;
    }
}

pub fn pwr_pt_phase_time_s(arg_state: [*c]const pwr_dct_powertrain_state) callconv(.c) f64 {
    var state = arg_state;
    _ = &state;
    return @as(f64, @floatFromInt(state.*.tcu_phase_tick)) * pwr_dct_powertrain_tick_s;
}

pub fn pwr_pt_clutches_open(arg_state: [*c]const pwr_dct_powertrain_state) callconv(.c) bool {
    var state = arg_state;
    _ = &state;
    return (state.*.dct.applied_clamp[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] <= state.*.config.dct.maximum_dog_shift_clamp) and (state.*.dct.applied_clamp[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] <= state.*.config.dct.maximum_dog_shift_clamp);
}

pub fn pwr_pt_supervisor_commands(arg_state: [*c]pwr_dct_powertrain_state, arg_input: [*c]const pwr_dct_powertrain_input, arg_commands: [*c]pwr_dct_powertrain_commands, arg_step_flags: [*c]u32) callconv(.c) void {
    var state = arg_state;
    _ = &state;
    var input = arg_input;
    _ = &input;
    var commands = arg_commands;
    _ = &commands;
    var step_flags = arg_step_flags;
    _ = &step_flags;
    const config: [*c]const pwr_dct_powertrain_config = &state.*.config;
    _ = &config;
    const engine_speed: f64 = state.*.engine_output.crank_speed_rad_s;
    _ = &engine_speed;
    var idle_addition: f64 = undefined;
    _ = &idle_addition;
    _ = memset(@as(?*anyopaque, @ptrCast(commands)), @as(c_int, 0), @sizeOf(pwr_dct_powertrain_commands));
    commands.*.engine_throttle = input.*.driver_throttle;
    if (!input.*.ignition_on) {
        state.*.engine_started = @as(c_int, 0) != 0;
        state.*.cranking_ticks = 0;
    } else if (engine_speed >= config.*.starter_release_speed_rad_s) {
        state.*.engine_started = @as(c_int, 1) != 0;
    }
    if (((@as(c_int, @intFromBool(input.*.ignition_on)) != 0) and !state.*.engine_started) and ((@as(f64, @floatFromInt(state.*.cranking_ticks)) * pwr_dct_powertrain_tick_s) < config.*.maximum_cranking_time_s)) {
        commands.*.starter_torque_nm = config.*.starter_torque_nm;
        commands.*.engine_throttle = pwr_pt_max(commands.*.engine_throttle, config.*.cranking_throttle_command);
        state.*.cranking_ticks +%= 1;
    }
    idle_addition = config.*.idle_throttle_command + (config.*.idle_speed_throttle_gain_s_per_rad * (config.*.idle_target_speed_rad_s - engine_speed));
    if (((@as(c_int, @intFromBool(input.*.ignition_on)) != 0) and (@as(c_int, @intFromBool(state.*.engine_started)) != 0)) and (engine_speed > 0.0)) {
        commands.*.engine_throttle = pwr_pt_max(commands.*.engine_throttle, pwr_pt_clamp(idle_addition, 0.0, 1.0));
    }
    if (!input.*.ignition_on or !input.*.selector_drive) {
        if (pwr_pt_clutches_open(state)) {
            pwr_pt_transition(state, @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_NEUTRAL)));
        } else {
            pwr_pt_transition(state, @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_RELEASE_TO_NEUTRAL)));
        }
    } else {
        while (true) {
            switch (state.*.tcu_phase) {
                @as(c_uint, @bitCast(@as(c_int, 0))) => {
                    pwr_pt_transition(state, @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2)));
                    break;
                },
                @as(c_uint, @bitCast(@as(c_int, 6))) => {
                    if (pwr_pt_clutches_open(state)) {
                        pwr_pt_transition(state, @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2)));
                    }
                    break;
                },
                @as(c_uint, @bitCast(@as(c_int, 1))) => {
                    if (state.*.engine_started) {
                        pwr_pt_transition(state, @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_LAUNCH_1)));
                    }
                    break;
                },
                @as(c_uint, @bitCast(@as(c_int, 2))) => {
                    if (state.*.halfshaft_speed_rad_s >= config.*.launch_complete_halfshaft_speed_rad_s) {
                        pwr_pt_transition(state, @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_GEAR_1)));
                    }
                    break;
                },
                @as(c_uint, @bitCast(@as(c_int, 3))) => {
                    if ((state.*.halfshaft_speed_rad_s >= config.*.first_to_second_shift_halfshaft_speed_rad_s) and (pwr_pt_phase_time_s(state) >= config.*.minimum_first_gear_time_s)) {
                        pwr_pt_transition(state, @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2)));
                    }
                    break;
                },
                @as(c_uint, @bitCast(@as(c_int, 4))) => {
                    if (pwr_pt_phase_time_s(state) >= config.*.first_to_second_shift_duration_s) {
                        pwr_pt_transition(state, @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_GEAR_2)));
                    }
                    break;
                },
                else => break,
            }
            break;
        }
    }
    while (true) {
        switch (state.*.tcu_phase) {
            @as(c_uint, @bitCast(@as(c_int, 1))) => {
                commands.*.gear_mask = @as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2) | @as(c_int, 4)))));
                step_flags.* |= @as(u32, @bitCast(PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE));
                break;
            },
            @as(c_uint, @bitCast(@as(c_int, 2))) => {
                {
                    const positive_speed_error: f64 = pwr_pt_max(engine_speed - config.*.launch_target_engine_speed_rad_s, 0.0);
                    _ = &positive_speed_error;
                    commands.*.gear_mask = @as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2) | @as(c_int, 4)))));
                    commands.*.clutch_clamp[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = pwr_pt_clamp((config.*.launch_clamp_feedforward * input.*.driver_throttle) + (config.*.launch_clamp_speed_gain_s_per_rad * positive_speed_error), 0.0, 1.0);
                    step_flags.* |= @as(u32, @bitCast(PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE));
                    break;
                }
            },
            @as(c_uint, @bitCast(@as(c_int, 3))) => {
                commands.*.gear_mask = @as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2) | @as(c_int, 4)))));
                commands.*.clutch_clamp[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 1.0;
                step_flags.* |= @as(u32, @bitCast(PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE));
                break;
            },
            @as(c_uint, @bitCast(@as(c_int, 4))) => {
                {
                    const progress: f64 = pwr_pt_clamp(pwr_pt_phase_time_s(state) / config.*.first_to_second_shift_duration_s, 0.0, 1.0);
                    _ = &progress;
                    commands.*.gear_mask = @as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2) | @as(c_int, 4)))));
                    commands.*.clutch_clamp[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 1.0 - progress;
                    commands.*.clutch_clamp[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] = progress;
                    commands.*.engine_throttle *= config.*.shift_engine_throttle_scale;
                    step_flags.* |= @as(u32, @bitCast(PWR_DCT_POWERTRAIN_DIAG_TCU_SHIFT_ACTIVE));
                    break;
                }
            },
            @as(c_uint, @bitCast(@as(c_int, 5))) => {
                commands.*.gear_mask = @as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2) | @as(c_int, 4)))));
                commands.*.clutch_clamp[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] = 1.0;
                step_flags.* |= @as(u32, @bitCast(PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE));
                break;
            },
            @as(c_uint, @bitCast(@as(c_int, 6))) => {
                commands.*.gear_mask = state.*.dct.engaged_gear_mask;
                break;
            },
            else => break,
        }
        break;
    }
    commands.*.engine_throttle = pwr_pt_clamp(commands.*.engine_throttle, 0.0, 1.0);
}

pub fn pwr_pt_commands(arg_state: [*c]pwr_dct_powertrain_state, arg_input: [*c]const pwr_dct_powertrain_input, arg_commands: [*c]pwr_dct_powertrain_commands, arg_step_flags: [*c]u32) callconv(.c) void {
    var state = arg_state;
    _ = &state;
    var input = arg_input;
    _ = &input;
    var commands = arg_commands;
    _ = &commands;
    var step_flags = arg_step_flags;
    _ = &step_flags;
    if (input.*.control_mode == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_CONTROL_DIRECT))) {
        _ = memset(@as(?*anyopaque, @ptrCast(commands)), @as(c_int, 0), @sizeOf(pwr_dct_powertrain_commands));
        pwr_pt_transition(state, @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_DIRECT)));
        commands.*.engine_throttle = input.*.driver_throttle;
        commands.*.starter_torque_nm = input.*.direct_starter_torque_nm;
        commands.*.gear_mask = input.*.direct_gear_mask;
        {
            var clutch: usize = 0;
            _ = &clutch;
            while (clutch < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (clutch +%= 1) {
                commands.*.clutch_clamp[clutch] = input.*.direct_clutch_clamp[clutch];
            }
        }
    } else {
        if (state.*.last_control_mode == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_CONTROL_DIRECT))) {
            pwr_pt_transition(state, @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_RELEASE_TO_NEUTRAL)));
        }
        pwr_pt_supervisor_commands(state, input, commands, step_flags);
    }
    state.*.last_control_mode = input.*.control_mode;
}

pub fn pwr_pt_engine_exhaust_reservoir(arg_engine: [*c]const pwr_si_engine_output, arg_minimum_pressure_pa: f64, arg_minimum_temperature_k: f64) callconv(.c) pwr_exhaust_reservoir {
    var engine = arg_engine;
    _ = &engine;
    var minimum_pressure_pa = arg_minimum_pressure_pa;
    _ = &minimum_pressure_pa;
    var minimum_temperature_k = arg_minimum_temperature_k;
    _ = &minimum_temperature_k;
    var reservoir: pwr_exhaust_reservoir = pwr_exhaust_reservoir{
        .pressure_pa = pwr_pt_max(engine.*.exhaust_source_pressure_pa, minimum_pressure_pa),
        .temperature_k = pwr_pt_max(engine.*.exhaust_temperature_k, minimum_temperature_k),
        .species_mass_fraction = @import("std").mem.zeroes([4]f64),
    };
    _ = &reservoir;
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            reservoir.species_mass_fraction[species] = engine.*.exhaust_species_mass_fraction[species];
        }
    }
    return reservoir;
}

pub fn pwr_pt_source_cp(arg_parameters: [*c]const pwr_exhaust_parameters, arg_engine: [*c]const pwr_si_engine_output) callconv(.c) f64 {
    var parameters = arg_parameters;
    _ = &parameters;
    var engine = arg_engine;
    _ = &engine;
    var cp: f64 = 0.0;
    _ = &cp;
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            cp += engine.*.exhaust_species_mass_fraction[species] * (parameters.*.species_cv_j_per_kg_k[species] + parameters.*.species_gas_constant_j_per_kg_k[species]);
        }
    }
    return cp;
}

pub fn pwr_pt_road_load(arg_config: [*c]const pwr_dct_powertrain_config, arg_input: [*c]const pwr_dct_powertrain_input, arg_speed_rad_s: f64) callconv(.c) f64 {
    var config = arg_config;
    _ = &config;
    var input = arg_input;
    _ = &input;
    var speed_rad_s = arg_speed_rad_s;
    _ = &speed_rad_s;
    const sign: f64 = tanh(speed_rad_s / config.*.road_load_regularization_speed_rad_s);
    _ = &sign;
    return (((config.*.rolling_resistance_torque_nm * sign) + (config.*.viscous_road_load_nm_s_per_rad * speed_rad_s)) + ((config.*.quadratic_road_load_nm_s2_per_rad2 * speed_rad_s) * fabs(speed_rad_s))) + (input.*.additional_road_load_torque_nm * sign);
}

pub fn pwr_pt_state_finite(arg_state: [*c]const pwr_dct_powertrain_state) callconv(.c) bool {
    var state = arg_state;
    _ = &state;
    return ((((((((@as(c_int, @intFromBool(pwr_pt_nonnegative_finite(state.*.halfshaft_speed_rad_s))) != 0) and (power_isfinite(state.*.halfshaft_angle_rad) != 0)) and (power_isfinite(state.*.cumulative_halfshaft_drive_work_j) != 0)) and (power_isfinite(state.*.cumulative_road_load_work_j) != 0)) and (power_isfinite(state.*.cumulative_halfshaft_numerical_adjustment_j) != 0)) and (power_isfinite(state.*.cumulative_exhaust_mass_mismatch_kg) != 0)) and (power_isfinite(state.*.cumulative_exhaust_enthalpy_mismatch_j) != 0)) and (power_isfinite(state.*.cumulative_engine_dct_energy_mismatch_j) != 0)) and (power_isfinite(state.*.cumulative_dct_output_energy_mismatch_j) != 0);
}

pub fn pwr_pt_shift_progress(arg_state: [*c]const pwr_dct_powertrain_state) callconv(.c) f64 {
    var state = arg_state;
    _ = &state;
    if (state.*.tcu_phase == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_GEAR_2))) {
        return 1.0;
    }
    if (state.*.tcu_phase != @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2))) {
        return 0.0;
    }
    return pwr_pt_clamp(pwr_pt_phase_time_s(state) / state.*.config.first_to_second_shift_duration_s, 0.0, 1.0);
}

pub fn pwr_pt_hash_u64(arg_hash: u64, arg_value: u64) callconv(.c) u64 {
    var hash = arg_hash;
    _ = &hash;
    var value = arg_value;
    _ = &value;
    {
        var byte: c_uint = 0;
        _ = &byte;
        while (byte < @as(c_uint, 8)) : (byte +%= 1) {
            hash ^= @as(u64, @bitCast((value >> @intCast(byte *% @as(c_uint, 8))) & @as(u64, 255)));
            hash *%= @as(u64, @bitCast(@as(u64, 1099511628211)));
        }
    }
    return hash;
}

pub fn pwr_pt_hash_double(arg_hash: u64, arg_value: f64) callconv(.c) u64 {
    var hash = arg_hash;
    _ = &hash;
    var value = arg_value;
    _ = &value;
    var bits: u64 = 0;
    _ = &bits;
    _ = memcpy(@as(?*anyopaque, @ptrCast(&bits)), @as(?*const anyopaque, @ptrCast(&value)), @sizeOf(u64));
    return pwr_pt_hash_u64(hash, bits);
}
