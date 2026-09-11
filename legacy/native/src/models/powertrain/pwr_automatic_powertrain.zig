// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/models/powertrain/pwr_automatic_powertrain.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_AUTOMATIC_GEAR_FIRST = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_FIRST;
const PWR_AUTOMATIC_GEAR_FOURTH = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_FOURTH;
const PWR_AUTOMATIC_GEAR_NEUTRAL = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_NEUTRAL;
const PWR_AUTOMATIC_GEAR_REVERSE = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_REVERSE;
const PWR_AUTOMATIC_GEAR_SECOND = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_SECOND;
const PWR_AUTOMATIC_GEAR_THIRD = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_THIRD;
const PWR_AUTOMATIC_OK = @import("../../abi.zig").PWR_AUTOMATIC_OK;
const PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT;
const PWR_AUTOMATIC_POWERTRAIN_CONTROL_SUPERVISED = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_CONTROL_SUPERVISED;
const PWR_AUTOMATIC_POWERTRAIN_DIAG_DIFFERENTIAL_SPEED_PROJECTED = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_DIAG_DIFFERENTIAL_SPEED_PROJECTED;
const PWR_AUTOMATIC_POWERTRAIN_DIAG_ECU_BROWNOUT = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_DIAG_ECU_BROWNOUT;
const PWR_AUTOMATIC_POWERTRAIN_DIAG_ENGINE_CONVERTER_ENERGY_MISMATCH = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_DIAG_ENGINE_CONVERTER_ENERGY_MISMATCH;
const PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH;
const PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH;
const PWR_AUTOMATIC_POWERTRAIN_DIAG_FINAL_DRIVE_ENERGY_MISMATCH = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_DIAG_FINAL_DRIVE_ENERGY_MISMATCH;
const PWR_AUTOMATIC_POWERTRAIN_DIAG_LOCKUP_ACTIVE = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_DIAG_LOCKUP_ACTIVE;
const PWR_AUTOMATIC_POWERTRAIN_DIAG_NONE = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_DIAG_NONE;
const PWR_AUTOMATIC_POWERTRAIN_DIAG_PUMP_SPEED_PROJECTED = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_DIAG_PUMP_SPEED_PROJECTED;
const PWR_AUTOMATIC_POWERTRAIN_DIAG_SHIFT_ACTIVE = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_DIAG_SHIFT_ACTIVE;
const PWR_AUTOMATIC_POWERTRAIN_DIAG_TCU_BROWNOUT = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_DIAG_TCU_BROWNOUT;
const PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT;
const PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG;
const PWR_AUTOMATIC_POWERTRAIN_INVALID_INPUT = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_INVALID_INPUT;
const PWR_AUTOMATIC_POWERTRAIN_NUMERIC_ERROR = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_NUMERIC_ERROR;
const PWR_AUTOMATIC_POWERTRAIN_OK = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_OK;
const PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR;
const PWR_AUTOMATIC_POWERTRAIN_TCU_BROWNOUT = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_TCU_BROWNOUT;
const PWR_AUTOMATIC_POWERTRAIN_TCU_DIRECT = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_TCU_DIRECT;
const PWR_AUTOMATIC_POWERTRAIN_TCU_FIRST = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_TCU_FIRST;
const PWR_AUTOMATIC_POWERTRAIN_TCU_NEUTRAL = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_TCU_NEUTRAL;
const PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND;
const PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND_LOCKED = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND_LOCKED;
const PWR_AUTOMATIC_POWERTRAIN_TCU_SHIFT_1_TO_2 = @import("../../abi.zig").PWR_AUTOMATIC_POWERTRAIN_TCU_SHIFT_1_TO_2;
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
const pwr_automatic = @import("../../abi.zig").pwr_automatic;
const pwr_automatic_config = @import("../../abi.zig").pwr_automatic_config;
const pwr_automatic_config_default = @import("../automatic/pwr_automatic.zig").pwr_automatic_config_default;
const pwr_automatic_coupled_input = @import("../../abi.zig").pwr_automatic_coupled_input;
const pwr_automatic_coupling_output = @import("../../abi.zig").pwr_automatic_coupling_output;
const pwr_automatic_gear = @import("../../abi.zig").pwr_automatic_gear;
const pwr_automatic_init = @import("../automatic/pwr_automatic.zig").pwr_automatic_init;
const pwr_automatic_powertrain_commands = @import("../../abi.zig").pwr_automatic_powertrain_commands;
const pwr_automatic_powertrain_config = @import("../../abi.zig").pwr_automatic_powertrain_config;
const pwr_automatic_powertrain_control_mode = @import("../../abi.zig").pwr_automatic_powertrain_control_mode;
const pwr_automatic_powertrain_input = @import("../../abi.zig").pwr_automatic_powertrain_input;
const pwr_automatic_powertrain_output = @import("../../abi.zig").pwr_automatic_powertrain_output;
const pwr_automatic_powertrain_result = @import("../../abi.zig").pwr_automatic_powertrain_result;
const pwr_automatic_powertrain_state = @import("../../abi.zig").pwr_automatic_powertrain_state;
const pwr_automatic_powertrain_tcu_phase = @import("../../abi.zig").pwr_automatic_powertrain_tcu_phase;
const pwr_automatic_powertrain_tick_s = @import("../../abi.zig").pwr_automatic_powertrain_tick_s;
const pwr_automatic_snapshot = @import("../../abi.zig").pwr_automatic_snapshot;
const pwr_automatic_snapshot_read = @import("../automatic/pwr_automatic.zig").pwr_automatic_snapshot_read;
const pwr_automatic_state_hash = @import("../automatic/pwr_automatic.zig").pwr_automatic_state_hash;
const pwr_automatic_step_coupled = @import("../automatic/pwr_automatic.zig").pwr_automatic_step_coupled;
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

pub fn pwr_automatic_powertrain_config_default(arg_config: [*c]pwr_automatic_powertrain_config) callconv(.c) void {
    var config = arg_config;
    _ = &config;
    if (config == null) {
        return;
    }
    config.* = pwr_automatic_powertrain_config{
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
        .automatic = @import("std").mem.zeroes(pwr_automatic_config),
        .exhaust = @import("std").mem.zeroes(pwr_exhaust_parameters),
        .initial_ambient_pressure_pa = 0,
        .initial_ambient_temperature_k = 0,
        .initial_exhaust_wall_temperature_k = 0,
        .electrical_nominal_voltage_v = 0,
        .electrical_internal_resistance_ohm = 0,
        .base_accessory_current_a = 0,
        .tcu_current_a = 0,
        .solenoid_current_at_max_pressure_a = 0,
        .starter_current_per_nm_a = 0,
        .ecu_brownout_voltage_v = 0,
        .tcu_brownout_voltage_v = 0,
        .starter_torque_nm = 0,
        .starter_release_speed_rad_s = 0,
        .maximum_cranking_time_s = 0,
        .cranking_throttle_command = 0,
        .idle_target_speed_rad_s = 0,
        .idle_throttle_command = 0,
        .idle_speed_throttle_gain_s_per_rad = 0,
        .line_pressure_throttle_gain_pa = 0,
        .first_to_second_halfshaft_speed_rad_s = 0,
        .minimum_first_gear_time_s = 0,
        .lockup_enable_halfshaft_speed_rad_s = 0,
        .shift_engine_throttle_scale = 0,
        .final_drive_ratio = 0,
        .final_drive_efficiency = 0,
        .left_halfshaft_inertia_kg_m2 = 0,
        .right_halfshaft_inertia_kg_m2 = 0,
        .rolling_resistance_torque_nm_per_side = 0,
        .viscous_road_load_nm_s_per_rad_per_side = 0,
        .quadratic_road_load_nm_s2_per_rad2_per_side = 0,
        .road_load_regularization_speed_rad_s = 0,
        .maximum_exhaust_mass_mismatch_kg_per_s = 0,
        .maximum_exhaust_enthalpy_mismatch_w = 0,
        .maximum_mechanical_coupling_mismatch_w = 0,
        .maximum_speed_projection_rad_s = 0,
    };
    pwr_si_engine_config_default(&config.*.engine);
    pwr_automatic_config_default(&config.*.automatic);
    pwr_apt_exhaust_parameters_default(&config.*.exhaust);
    config.*.initial_ambient_pressure_pa = 101325.0;
    config.*.initial_ambient_temperature_k = 298.15;
    config.*.initial_exhaust_wall_temperature_k = 298.15;
    config.*.electrical_nominal_voltage_v = 13.6;
    config.*.electrical_internal_resistance_ohm = 0.025;
    config.*.base_accessory_current_a = 4.0;
    config.*.tcu_current_a = 2.0;
    config.*.solenoid_current_at_max_pressure_a = 7.0;
    config.*.starter_current_per_nm_a = 0.85;
    config.*.ecu_brownout_voltage_v = 8.5;
    config.*.tcu_brownout_voltage_v = 9.0;
    config.*.starter_torque_nm = 62.0;
    config.*.starter_release_speed_rad_s = 82.0;
    config.*.maximum_cranking_time_s = 1.8;
    config.*.cranking_throttle_command = 0.2;
    config.*.idle_target_speed_rad_s = 92.0;
    config.*.idle_throttle_command = 0.1;
    config.*.idle_speed_throttle_gain_s_per_rad = 0.002;
    config.*.line_pressure_throttle_gain_pa = 250000.0;
    config.*.first_to_second_halfshaft_speed_rad_s = 12.0;
    config.*.minimum_first_gear_time_s = 0.4;
    config.*.lockup_enable_halfshaft_speed_rad_s = 18.0;
    config.*.shift_engine_throttle_scale = 0.84;
    config.*.final_drive_ratio = 3.4;
    config.*.final_drive_efficiency = 0.96;
    config.*.left_halfshaft_inertia_kg_m2 = 1.6;
    config.*.right_halfshaft_inertia_kg_m2 = 1.6;
    config.*.rolling_resistance_torque_nm_per_side = 4.0;
    config.*.viscous_road_load_nm_s_per_rad_per_side = 0.04;
    config.*.quadratic_road_load_nm_s2_per_rad2_per_side = 0.002;
    config.*.road_load_regularization_speed_rad_s = 0.5;
    config.*.maximum_exhaust_mass_mismatch_kg_per_s = 0.025;
    config.*.maximum_exhaust_enthalpy_mismatch_w = 25000.0;
    config.*.maximum_mechanical_coupling_mismatch_w = 25000.0;
    config.*.maximum_speed_projection_rad_s = 0.5;
}

pub fn pwr_automatic_powertrain_validate_config(arg_config: [*c]const pwr_automatic_powertrain_config) callconv(.c) pwr_automatic_powertrain_result {
    var config = arg_config;
    _ = &config;
    var automatic_probe: pwr_automatic = undefined;
    _ = &automatic_probe;
    if (config == null) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT));
    }
    if (((((config.*.engine.base_tick_ns != @as(u64, 100000)) or (config.*.automatic.step_ns != @as(u64, 100000))) or (pwr_si_engine_validate_config(&config.*.engine) != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) or (pwr_automatic_init(&automatic_probe, &config.*.automatic) != @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))) or (pwr_exhaust_validate_parameters(&config.*.exhaust) != @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG));
    }
    if (((((((((((((((((((((((((((((((((((((((((((!pwr_apt_positive_finite(config.*.initial_ambient_pressure_pa) or !pwr_apt_positive_finite(config.*.initial_ambient_temperature_k)) or !pwr_apt_positive_finite(config.*.initial_exhaust_wall_temperature_k)) or (config.*.initial_ambient_temperature_k < config.*.exhaust.minimum_temperature_k)) or (config.*.initial_exhaust_wall_temperature_k < config.*.exhaust.minimum_temperature_k)) or !pwr_apt_positive_finite(config.*.electrical_nominal_voltage_v)) or !pwr_apt_nonnegative_finite(config.*.electrical_internal_resistance_ohm)) or !pwr_apt_nonnegative_finite(config.*.base_accessory_current_a)) or !pwr_apt_nonnegative_finite(config.*.tcu_current_a)) or !pwr_apt_nonnegative_finite(config.*.solenoid_current_at_max_pressure_a)) or !pwr_apt_nonnegative_finite(config.*.starter_current_per_nm_a)) or !pwr_apt_positive_finite(config.*.ecu_brownout_voltage_v)) or !pwr_apt_positive_finite(config.*.tcu_brownout_voltage_v)) or (config.*.ecu_brownout_voltage_v >= config.*.electrical_nominal_voltage_v)) or (config.*.tcu_brownout_voltage_v >= config.*.electrical_nominal_voltage_v)) or !pwr_apt_nonnegative_finite(config.*.starter_torque_nm)) or !pwr_apt_positive_finite(config.*.starter_release_speed_rad_s)) or !pwr_apt_positive_finite(config.*.maximum_cranking_time_s)) or !pwr_apt_nonnegative_finite(config.*.cranking_throttle_command)) or (config.*.cranking_throttle_command > 1.0)) or !pwr_apt_positive_finite(config.*.idle_target_speed_rad_s)) or !pwr_apt_nonnegative_finite(config.*.idle_throttle_command)) or (config.*.idle_throttle_command > 1.0)) or !pwr_apt_nonnegative_finite(config.*.idle_speed_throttle_gain_s_per_rad)) or !pwr_apt_nonnegative_finite(config.*.line_pressure_throttle_gain_pa)) or !pwr_apt_positive_finite(config.*.first_to_second_halfshaft_speed_rad_s)) or !pwr_apt_nonnegative_finite(config.*.minimum_first_gear_time_s)) or !pwr_apt_positive_finite(config.*.lockup_enable_halfshaft_speed_rad_s)) or (config.*.lockup_enable_halfshaft_speed_rad_s <= config.*.first_to_second_halfshaft_speed_rad_s)) or !pwr_apt_positive_finite(config.*.shift_engine_throttle_scale)) or (config.*.shift_engine_throttle_scale > 1.0)) or !pwr_apt_positive_finite(config.*.final_drive_ratio)) or !pwr_apt_positive_finite(config.*.final_drive_efficiency)) or (config.*.final_drive_efficiency > 1.0)) or !pwr_apt_positive_finite(config.*.left_halfshaft_inertia_kg_m2)) or !pwr_apt_positive_finite(config.*.right_halfshaft_inertia_kg_m2)) or !pwr_apt_nonnegative_finite(config.*.rolling_resistance_torque_nm_per_side)) or !pwr_apt_nonnegative_finite(config.*.viscous_road_load_nm_s_per_rad_per_side)) or !pwr_apt_nonnegative_finite(config.*.quadratic_road_load_nm_s2_per_rad2_per_side)) or !pwr_apt_positive_finite(config.*.road_load_regularization_speed_rad_s)) or !pwr_apt_nonnegative_finite(config.*.maximum_exhaust_mass_mismatch_kg_per_s)) or !pwr_apt_nonnegative_finite(config.*.maximum_exhaust_enthalpy_mismatch_w)) or !pwr_apt_nonnegative_finite(config.*.maximum_mechanical_coupling_mismatch_w)) or !pwr_apt_nonnegative_finite(config.*.maximum_speed_projection_rad_s)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG));
    }
    return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_OK));
}

pub fn pwr_automatic_powertrain_init(arg_state: [*c]pwr_automatic_powertrain_state, arg_config: [*c]const pwr_automatic_powertrain_config) callconv(.c) pwr_automatic_powertrain_result {
    var state = arg_state;
    _ = &state;
    var config = arg_config;
    _ = &config;
    var candidate: pwr_automatic_powertrain_state = pwr_automatic_powertrain_state{
        .config = pwr_automatic_powertrain_config{
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
            .automatic = @import("std").mem.zeroes(pwr_automatic_config),
            .exhaust = @import("std").mem.zeroes(pwr_exhaust_parameters),
            .initial_ambient_pressure_pa = 0,
            .initial_ambient_temperature_k = 0,
            .initial_exhaust_wall_temperature_k = 0,
            .electrical_nominal_voltage_v = 0,
            .electrical_internal_resistance_ohm = 0,
            .base_accessory_current_a = 0,
            .tcu_current_a = 0,
            .solenoid_current_at_max_pressure_a = 0,
            .starter_current_per_nm_a = 0,
            .ecu_brownout_voltage_v = 0,
            .tcu_brownout_voltage_v = 0,
            .starter_torque_nm = 0,
            .starter_release_speed_rad_s = 0,
            .maximum_cranking_time_s = 0,
            .cranking_throttle_command = 0,
            .idle_target_speed_rad_s = 0,
            .idle_throttle_command = 0,
            .idle_speed_throttle_gain_s_per_rad = 0,
            .line_pressure_throttle_gain_pa = 0,
            .first_to_second_halfshaft_speed_rad_s = 0,
            .minimum_first_gear_time_s = 0,
            .lockup_enable_halfshaft_speed_rad_s = 0,
            .shift_engine_throttle_scale = 0,
            .final_drive_ratio = 0,
            .final_drive_efficiency = 0,
            .left_halfshaft_inertia_kg_m2 = 0,
            .right_halfshaft_inertia_kg_m2 = 0,
            .rolling_resistance_torque_nm_per_side = 0,
            .viscous_road_load_nm_s_per_rad_per_side = 0,
            .quadratic_road_load_nm_s2_per_rad2_per_side = 0,
            .road_load_regularization_speed_rad_s = 0,
            .maximum_exhaust_mass_mismatch_kg_per_s = 0,
            .maximum_exhaust_enthalpy_mismatch_w = 0,
            .maximum_mechanical_coupling_mismatch_w = 0,
            .maximum_speed_projection_rad_s = 0,
        },
        .engine = @import("std").mem.zeroes(pwr_si_engine_state),
        .automatic = @import("std").mem.zeroes(pwr_automatic),
        .exhaust = @import("std").mem.zeroes(pwr_exhaust_state),
        .engine_output = @import("std").mem.zeroes(pwr_si_engine_output),
        .automatic_output = @import("std").mem.zeroes(pwr_automatic_snapshot),
        .converter_coupling = @import("std").mem.zeroes(pwr_automatic_coupling_output),
        .exhaust_output = @import("std").mem.zeroes(pwr_exhaust_observation),
        .exhaust_step = @import("std").mem.zeroes(pwr_exhaust_diagnostics),
        .tick = @import("std").mem.zeroes(u64),
        .cranking_ticks = @import("std").mem.zeroes(u64),
        .first_gear_ticks = @import("std").mem.zeroes(u64),
        .last_control_mode = @import("std").mem.zeroes(pwr_automatic_powertrain_control_mode),
        .tcu_phase = @import("std").mem.zeroes(pwr_automatic_powertrain_tcu_phase),
        .supervised_gear = @import("std").mem.zeroes(pwr_automatic_gear),
        .engine_started = false,
        .ecu_powered = false,
        .tcu_powered = false,
        .low_voltage_v = 0,
        .left_halfshaft_speed_rad_s = 0,
        .right_halfshaft_speed_rad_s = 0,
        .left_halfshaft_angle_rad = 0,
        .right_halfshaft_angle_rad = 0,
        .initial_halfshaft_kinetic_energy_j = 0,
        .cumulative_halfshaft_drive_work_j = 0,
        .cumulative_road_load_work_j = 0,
        .cumulative_differential_projection_adjustment_j = 0,
        .last_commanded_engine_throttle = 0,
        .last_commanded_starter_torque_nm = 0,
        .last_commanded_line_pressure_pa = 0,
        .last_commanded_lockup = 0,
        .last_commanded_gear = @import("std").mem.zeroes(pwr_automatic_gear),
        .last_left_drive_torque_nm = 0,
        .last_right_drive_torque_nm = 0,
        .last_left_road_load_torque_nm = 0,
        .last_right_road_load_torque_nm = 0,
        .last_exhaust_mass_mismatch_kg = 0,
        .cumulative_exhaust_mass_mismatch_kg = 0,
        .last_exhaust_enthalpy_mismatch_j = 0,
        .cumulative_exhaust_enthalpy_mismatch_j = 0,
        .last_engine_converter_energy_mismatch_j = 0,
        .cumulative_engine_converter_energy_mismatch_j = 0,
        .last_pump_speed_projection_rad_s = 0,
        .last_unconstrained_carrier_speed_mismatch_rad_s = 0,
        .last_carrier_constraint_residual_rad_s = 0,
        .last_differential_projection_adjustment_j = 0,
        .last_final_drive_energy_mismatch_j = 0,
        .cumulative_final_drive_energy_mismatch_j = 0,
        .last_step_diagnostic_flags = @import("std").mem.zeroes(u32),
        .diagnostic_flags = @import("std").mem.zeroes(u32),
    };
    _ = &candidate;
    var ambient: pwr_exhaust_reservoir = undefined;
    _ = &ambient;
    if (state == null) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT));
    }
    if (pwr_automatic_powertrain_validate_config(config) != @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_OK))) {
        return @as(c_uint, @bitCast(if (config == null) PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT else PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG));
    }
    candidate.config = config.*;
    if ((pwr_si_engine_init(&candidate.engine, &config.*.engine) != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK))) or (pwr_automatic_init(&candidate.automatic, &config.*.automatic) != @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR));
    }
    ambient = pwr_apt_ambient_reservoir(config.*.initial_ambient_pressure_pa, config.*.initial_ambient_temperature_k);
    if (pwr_exhaust_initialize_uniform(&config.*.exhaust, &ambient, config.*.initial_exhaust_wall_temperature_k, &candidate.exhaust) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR));
    }
    if (((pwr_si_engine_observe(&candidate.engine, &candidate.engine_output) != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK))) or (pwr_automatic_snapshot_read(&candidate.automatic, &candidate.automatic_output) != @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))) or (pwr_exhaust_observe(&config.*.exhaust, &candidate.exhaust, &candidate.exhaust_output) != @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR));
    }
    candidate.last_control_mode = @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_CONTROL_SUPERVISED));
    candidate.tcu_phase = @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_TCU_NEUTRAL));
    candidate.supervised_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
    candidate.low_voltage_v = config.*.electrical_nominal_voltage_v;
    state.* = candidate;
    return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_OK));
}

pub fn pwr_automatic_powertrain_step(arg_state: [*c]pwr_automatic_powertrain_state, arg_input: [*c]const pwr_automatic_powertrain_input, arg_output: [*c]pwr_automatic_powertrain_output) callconv(.c) pwr_automatic_powertrain_result {
    var state = arg_state;
    _ = &state;
    var input = arg_input;
    _ = &input;
    var output = arg_output;
    _ = &output;
    var candidate: pwr_automatic_powertrain_state = undefined;
    _ = &candidate;
    var commands: pwr_automatic_powertrain_commands = undefined;
    _ = &commands;
    var exhaust_input: pwr_exhaust_inputs = undefined;
    _ = &exhaust_input;
    var engine_input: pwr_si_engine_input = undefined;
    _ = &engine_input;
    var automatic_input: pwr_automatic_coupled_input = undefined;
    _ = &automatic_input;
    var next_engine_output: pwr_si_engine_output = undefined;
    _ = &next_engine_output;
    var next_automatic_output: pwr_automatic_snapshot = undefined;
    _ = &next_automatic_output;
    var next_coupling: pwr_automatic_coupling_output = undefined;
    _ = &next_coupling;
    var next_exhaust_step: pwr_exhaust_diagnostics = undefined;
    _ = &next_exhaust_step;
    const dt: f64 = pwr_automatic_powertrain_tick_s;
    _ = &dt;
    var engine_load_work_before: f64 = undefined;
    _ = &engine_load_work_before;
    var automatic_boundary_energy_before: f64 = undefined;
    _ = &automatic_boundary_energy_before;
    var old_automatic_output_speed: f64 = undefined;
    _ = &old_automatic_output_speed;
    var old_left_speed: f64 = undefined;
    _ = &old_left_speed;
    var old_right_speed: f64 = undefined;
    _ = &old_right_speed;
    var predicted_left_speed: f64 = undefined;
    _ = &predicted_left_speed;
    var predicted_right_speed: f64 = undefined;
    _ = &predicted_right_speed;
    var predicted_carrier_speed: f64 = undefined;
    _ = &predicted_carrier_speed;
    var carrier_speed: f64 = undefined;
    _ = &carrier_speed;
    var carrier_correction: f64 = undefined;
    _ = &carrier_correction;
    var average_left_speed: f64 = undefined;
    _ = &average_left_speed;
    var average_right_speed: f64 = undefined;
    _ = &average_right_speed;
    var old_halfshaft_energy: f64 = undefined;
    _ = &old_halfshaft_energy;
    var new_halfshaft_energy: f64 = undefined;
    _ = &new_halfshaft_energy;
    var halfshaft_drive_work: f64 = undefined;
    _ = &halfshaft_drive_work;
    var road_work: f64 = undefined;
    _ = &road_work;
    var source_mass_flow: f64 = undefined;
    _ = &source_mass_flow;
    var network_mass_flow: f64 = undefined;
    _ = &network_mass_flow;
    var source_enthalpy_flow: f64 = undefined;
    _ = &source_enthalpy_flow;
    var network_enthalpy_flow: f64 = undefined;
    _ = &network_enthalpy_flow;
    var old_automatic_speed_average: f64 = undefined;
    _ = &old_automatic_speed_average;
    var final_drive_input_work: f64 = undefined;
    _ = &final_drive_input_work;
    var engine_faults: u32 = undefined;
    _ = &engine_faults;
    var step_flags: u32 = @as(u32, @bitCast(PWR_AUTOMATIC_POWERTRAIN_DIAG_NONE));
    _ = &step_flags;
    if ((state == null) or (output == null)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT));
    }
    if (pwr_automatic_powertrain_validate_config(&state.*.config) != @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_OK))) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG));
    }
    if (!pwr_apt_input_valid(&state.*.config, input) or (input.*.ambient_temperature_k < state.*.config.exhaust.minimum_temperature_k)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_INVALID_INPUT));
    }
    candidate = state.*;
    pwr_apt_preliminary_commands(&candidate, input, &commands);
    pwr_apt_electrical_and_brownout(&candidate, input, &commands, &step_flags);
    candidate.last_control_mode = input.*.control_mode;
    exhaust_input = pwr_exhaust_inputs{
        .upstream = pwr_apt_engine_exhaust_reservoir(&candidate.engine_output, input.*.ambient_pressure_pa, candidate.config.exhaust.minimum_temperature_k),
        .downstream = pwr_apt_ambient_reservoir(input.*.ambient_pressure_pa, input.*.ambient_temperature_k),
        .inlet_area_scale = 1.0,
        .tailpipe_area_scale = input.*.tailpipe_area_scale,
        .ambient_temperature_k = input.*.ambient_temperature_k,
    };
    if (pwr_exhaust_step(&candidate.config.exhaust, &candidate.exhaust, &exhaust_input, dt, &next_exhaust_step) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR));
    }
    candidate.exhaust_step = next_exhaust_step;
    candidate.exhaust_output = next_exhaust_step.final_observation;
    engine_faults = input.*.engine_fault_mask;
    if (!candidate.ecu_powered and (@as(c_int, @intFromBool(input.*.ignition_on)) != 0)) {
        engine_faults |= @as(u32, @bitCast(PWR_SI_ENGINE_FAULT_LOW_VOLTAGE));
    }
    engine_load_work_before = candidate.engine_output.mechanical_load_work_j;
    engine_input = pwr_si_engine_input{
        .driver_throttle = commands.engine_throttle,
        .load_torque_nm = candidate.converter_coupling.pump_load_torque_nm,
        .starter_torque_nm = commands.starter_torque_nm,
        .exhaust_backpressure_pa = pwr_apt_max(candidate.exhaust_output.pressure_pa[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))], 1.0),
        .ambient_pressure_pa = input.*.ambient_pressure_pa,
        .ambient_temperature_k = input.*.ambient_temperature_k,
        .fault_mask = engine_faults,
        .ignition_on = input.*.ignition_on,
    };
    if (pwr_si_engine_step(&candidate.engine, &engine_input, &next_engine_output) != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK))) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR));
    }
    candidate.engine_output = next_engine_output;
    candidate.last_left_road_load_torque_nm = pwr_apt_road_load(&candidate.config, candidate.left_halfshaft_speed_rad_s, input.*.left_additional_road_load_torque_nm);
    candidate.last_right_road_load_torque_nm = pwr_apt_road_load(&candidate.config, candidate.right_halfshaft_speed_rad_s, input.*.right_additional_road_load_torque_nm);
    automatic_boundary_energy_before = candidate.automatic_output.pump_boundary_energy_j;
    old_automatic_output_speed = candidate.automatic_output.output_speed_rad_s;
    candidate.last_pump_speed_projection_rad_s = candidate.engine_output.crank_speed_rad_s - candidate.automatic_output.pump_speed_rad_s;
    automatic_input = pwr_automatic_coupled_input{
        .pump_speed_rad_s = candidate.engine_output.crank_speed_rad_s,
        .output_external_torque_nm = -(candidate.last_left_road_load_torque_nm + candidate.last_right_road_load_torque_nm) / (candidate.config.final_drive_ratio * candidate.config.final_drive_efficiency),
        .line_pressure_command_pa = commands.line_pressure_pa,
        .lockup_command = commands.lockup,
        .requested_gear = commands.gear,
    };
    if ((pwr_automatic_step_coupled(&candidate.automatic, &automatic_input, &next_coupling) != @as(c_uint, @bitCast(PWR_AUTOMATIC_OK))) or (pwr_automatic_snapshot_read(&candidate.automatic, &next_automatic_output) != @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR));
    }
    candidate.converter_coupling = next_coupling;
    candidate.automatic_output = next_automatic_output;
    old_left_speed = candidate.left_halfshaft_speed_rad_s;
    old_right_speed = candidate.right_halfshaft_speed_rad_s;
    candidate.last_left_drive_torque_nm = ((0.5 * candidate.automatic_output.gear_output_torque_nm) * candidate.config.final_drive_ratio) * candidate.config.final_drive_efficiency;
    candidate.last_right_drive_torque_nm = candidate.last_left_drive_torque_nm;
    predicted_left_speed = old_left_speed + (((candidate.last_left_drive_torque_nm - candidate.last_left_road_load_torque_nm) / candidate.config.left_halfshaft_inertia_kg_m2) * dt);
    predicted_right_speed = old_right_speed + (((candidate.last_right_drive_torque_nm - candidate.last_right_road_load_torque_nm) / candidate.config.right_halfshaft_inertia_kg_m2) * dt);
    predicted_carrier_speed = 0.5 * (predicted_left_speed + predicted_right_speed);
    carrier_speed = candidate.automatic_output.output_speed_rad_s / candidate.config.final_drive_ratio;
    carrier_correction = carrier_speed - predicted_carrier_speed;
    candidate.left_halfshaft_speed_rad_s = predicted_left_speed + carrier_correction;
    candidate.right_halfshaft_speed_rad_s = predicted_right_speed + carrier_correction;
    candidate.last_unconstrained_carrier_speed_mismatch_rad_s = carrier_correction;
    candidate.last_carrier_constraint_residual_rad_s = (0.5 * (candidate.left_halfshaft_speed_rad_s + candidate.right_halfshaft_speed_rad_s)) - carrier_speed;
    average_left_speed = 0.5 * (old_left_speed + candidate.left_halfshaft_speed_rad_s);
    average_right_speed = 0.5 * (old_right_speed + candidate.right_halfshaft_speed_rad_s);
    candidate.left_halfshaft_angle_rad += average_left_speed * dt;
    candidate.right_halfshaft_angle_rad += average_right_speed * dt;
    halfshaft_drive_work = ((candidate.last_left_drive_torque_nm * average_left_speed) * dt) + ((candidate.last_right_drive_torque_nm * average_right_speed) * dt);
    road_work = ((candidate.last_left_road_load_torque_nm * average_left_speed) * dt) + ((candidate.last_right_road_load_torque_nm * average_right_speed) * dt);
    old_halfshaft_energy = 0.5 * (((candidate.config.left_halfshaft_inertia_kg_m2 * old_left_speed) * old_left_speed) + ((candidate.config.right_halfshaft_inertia_kg_m2 * old_right_speed) * old_right_speed));
    new_halfshaft_energy = 0.5 * (((candidate.config.left_halfshaft_inertia_kg_m2 * candidate.left_halfshaft_speed_rad_s) * candidate.left_halfshaft_speed_rad_s) + ((candidate.config.right_halfshaft_inertia_kg_m2 * candidate.right_halfshaft_speed_rad_s) * candidate.right_halfshaft_speed_rad_s));
    candidate.last_differential_projection_adjustment_j = ((new_halfshaft_energy - old_halfshaft_energy) - halfshaft_drive_work) + road_work;
    candidate.cumulative_halfshaft_drive_work_j += halfshaft_drive_work;
    candidate.cumulative_road_load_work_j += road_work;
    candidate.cumulative_differential_projection_adjustment_j += candidate.last_differential_projection_adjustment_j;
    source_mass_flow = state.*.engine_output.exhaust_mass_flow_kg_per_s;
    network_mass_flow = next_exhaust_step.average_path_mass_flow_kg_per_s[@as(c_uint, @intCast(PWR_EXHAUST_PATH_INLET))];
    candidate.last_exhaust_mass_mismatch_kg = (source_mass_flow - network_mass_flow) * dt;
    candidate.cumulative_exhaust_mass_mismatch_kg += candidate.last_exhaust_mass_mismatch_kg;
    source_enthalpy_flow = (source_mass_flow * pwr_apt_source_cp(&candidate.config.exhaust, &state.*.engine_output)) * exhaust_input.upstream.temperature_k;
    network_enthalpy_flow = (network_mass_flow * pwr_apt_source_cp(&candidate.config.exhaust, &state.*.engine_output)) * exhaust_input.upstream.temperature_k;
    candidate.last_exhaust_enthalpy_mismatch_j = (source_enthalpy_flow - network_enthalpy_flow) * dt;
    candidate.cumulative_exhaust_enthalpy_mismatch_j += candidate.last_exhaust_enthalpy_mismatch_j;
    candidate.last_engine_converter_energy_mismatch_j = (candidate.engine_output.mechanical_load_work_j - engine_load_work_before) - (candidate.automatic_output.pump_boundary_energy_j - automatic_boundary_energy_before);
    candidate.cumulative_engine_converter_energy_mismatch_j += candidate.last_engine_converter_energy_mismatch_j;
    old_automatic_speed_average = 0.5 * (old_automatic_output_speed + candidate.automatic_output.output_speed_rad_s);
    final_drive_input_work = ((candidate.automatic_output.gear_output_torque_nm * old_automatic_speed_average) * candidate.config.final_drive_efficiency) * dt;
    candidate.last_final_drive_energy_mismatch_j = final_drive_input_work - halfshaft_drive_work;
    candidate.cumulative_final_drive_energy_mismatch_j += candidate.last_final_drive_energy_mismatch_j;
    if ((fabs(candidate.last_exhaust_mass_mismatch_kg) / dt) > candidate.config.maximum_exhaust_mass_mismatch_kg_per_s) {
        step_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH));
    }
    if ((fabs(candidate.last_exhaust_enthalpy_mismatch_j) / dt) > candidate.config.maximum_exhaust_enthalpy_mismatch_w) {
        step_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH));
    }
    if ((fabs(candidate.last_engine_converter_energy_mismatch_j) / dt) > candidate.config.maximum_mechanical_coupling_mismatch_w) {
        step_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_POWERTRAIN_DIAG_ENGINE_CONVERTER_ENERGY_MISMATCH));
    }
    if (fabs(candidate.last_pump_speed_projection_rad_s) > candidate.config.maximum_speed_projection_rad_s) {
        step_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_POWERTRAIN_DIAG_PUMP_SPEED_PROJECTED));
    }
    if (fabs(carrier_correction) > candidate.config.maximum_speed_projection_rad_s) {
        step_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_POWERTRAIN_DIAG_DIFFERENTIAL_SPEED_PROJECTED));
    }
    if ((fabs(candidate.last_final_drive_energy_mismatch_j) / dt) > candidate.config.maximum_mechanical_coupling_mismatch_w) {
        step_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_POWERTRAIN_DIAG_FINAL_DRIVE_ENERGY_MISMATCH));
    }
    pwr_apt_update_tcu_phase(&candidate, input, &step_flags);
    candidate.last_commanded_engine_throttle = commands.engine_throttle;
    candidate.last_commanded_starter_torque_nm = commands.starter_torque_nm;
    candidate.last_commanded_line_pressure_pa = commands.line_pressure_pa;
    candidate.last_commanded_lockup = commands.lockup;
    candidate.last_commanded_gear = commands.gear;
    candidate.last_step_diagnostic_flags = step_flags;
    candidate.diagnostic_flags |= step_flags;
    candidate.tick +%= 1;
    if (!pwr_apt_state_finite(&candidate)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_NUMERIC_ERROR));
    }
    if (pwr_automatic_powertrain_observe(&candidate, output) != @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_OK))) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_NUMERIC_ERROR));
    }
    state.* = candidate;
    return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_OK));
}

pub fn pwr_automatic_powertrain_observe(arg_state: [*c]const pwr_automatic_powertrain_state, arg_output: [*c]pwr_automatic_powertrain_output) callconv(.c) pwr_automatic_powertrain_result {
    var state = arg_state;
    _ = &state;
    var output = arg_output;
    _ = &output;
    var kinetic: f64 = undefined;
    _ = &kinetic;
    var kinetic_change: f64 = undefined;
    _ = &kinetic_change;
    if ((state == null) or (output == null)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT));
    }
    if (pwr_automatic_powertrain_validate_config(&state.*.config) != @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_OK))) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG));
    }
    kinetic = 0.5 * (((state.*.config.left_halfshaft_inertia_kg_m2 * state.*.left_halfshaft_speed_rad_s) * state.*.left_halfshaft_speed_rad_s) + ((state.*.config.right_halfshaft_inertia_kg_m2 * state.*.right_halfshaft_speed_rad_s) * state.*.right_halfshaft_speed_rad_s));
    kinetic_change = kinetic - state.*.initial_halfshaft_kinetic_energy_j;
    output.* = pwr_automatic_powertrain_output{
        .time_s = @as(f64, @floatFromInt(state.*.tick)) * pwr_automatic_powertrain_tick_s,
        .tick = state.*.tick,
        .control_mode = state.*.last_control_mode,
        .tcu_phase = state.*.tcu_phase,
        .low_voltage_v = state.*.low_voltage_v,
        .ecu_powered = state.*.ecu_powered,
        .tcu_powered = state.*.tcu_powered,
        .commanded_engine_throttle = state.*.last_commanded_engine_throttle,
        .commanded_starter_torque_nm = state.*.last_commanded_starter_torque_nm,
        .commanded_line_pressure_pa = state.*.last_commanded_line_pressure_pa,
        .commanded_lockup = state.*.last_commanded_lockup,
        .commanded_gear = state.*.last_commanded_gear,
        .engine = state.*.engine_output,
        .automatic = state.*.automatic_output,
        .converter_coupling = state.*.converter_coupling,
        .exhaust = state.*.exhaust_output,
        .exhaust_step = state.*.exhaust_step,
        .differential_carrier_speed_rad_s = state.*.automatic_output.output_speed_rad_s / state.*.config.final_drive_ratio,
        .left_halfshaft_speed_rad_s = state.*.left_halfshaft_speed_rad_s,
        .right_halfshaft_speed_rad_s = state.*.right_halfshaft_speed_rad_s,
        .left_halfshaft_angle_rad = state.*.left_halfshaft_angle_rad,
        .right_halfshaft_angle_rad = state.*.right_halfshaft_angle_rad,
        .left_drive_torque_nm = state.*.last_left_drive_torque_nm,
        .right_drive_torque_nm = state.*.last_right_drive_torque_nm,
        .left_road_load_torque_nm = state.*.last_left_road_load_torque_nm,
        .right_road_load_torque_nm = state.*.last_right_road_load_torque_nm,
        .differential_speed_rad_s = state.*.left_halfshaft_speed_rad_s - state.*.right_halfshaft_speed_rad_s,
        .halfshaft_kinetic_energy_j = kinetic,
        .cumulative_halfshaft_drive_work_j = state.*.cumulative_halfshaft_drive_work_j,
        .cumulative_road_load_work_j = state.*.cumulative_road_load_work_j,
        .cumulative_differential_projection_adjustment_j = state.*.cumulative_differential_projection_adjustment_j,
        .halfshaft_energy_residual_j = ((state.*.cumulative_halfshaft_drive_work_j - state.*.cumulative_road_load_work_j) + state.*.cumulative_differential_projection_adjustment_j) - kinetic_change,
        .exhaust_mass_mismatch_kg = state.*.last_exhaust_mass_mismatch_kg,
        .cumulative_exhaust_mass_mismatch_kg = state.*.cumulative_exhaust_mass_mismatch_kg,
        .exhaust_enthalpy_mismatch_j = state.*.last_exhaust_enthalpy_mismatch_j,
        .cumulative_exhaust_enthalpy_mismatch_j = state.*.cumulative_exhaust_enthalpy_mismatch_j,
        .engine_converter_energy_mismatch_j = state.*.last_engine_converter_energy_mismatch_j,
        .cumulative_engine_converter_energy_mismatch_j = state.*.cumulative_engine_converter_energy_mismatch_j,
        .pump_speed_projection_rad_s = state.*.last_pump_speed_projection_rad_s,
        .pump_projection_adjustment_j = state.*.converter_coupling.projection_adjustment_j,
        .unconstrained_carrier_speed_mismatch_rad_s = state.*.last_unconstrained_carrier_speed_mismatch_rad_s,
        .carrier_constraint_residual_rad_s = state.*.last_carrier_constraint_residual_rad_s,
        .differential_projection_adjustment_j = state.*.last_differential_projection_adjustment_j,
        .final_drive_energy_mismatch_j = state.*.last_final_drive_energy_mismatch_j,
        .cumulative_final_drive_energy_mismatch_j = state.*.cumulative_final_drive_energy_mismatch_j,
        .last_step_diagnostic_flags = state.*.last_step_diagnostic_flags,
        .diagnostic_flags = state.*.diagnostic_flags,
        .state_hash = @import("std").mem.zeroes(u64),
    };
    output.*.state_hash = pwr_automatic_powertrain_state_hash(state);
    return @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_OK));
}

pub fn pwr_automatic_powertrain_state_hash(arg_state: [*c]const pwr_automatic_powertrain_state) callconv(.c) u64 {
    var state = arg_state;
    _ = &state;
    var hash: u64 = 14695981039346656037;
    _ = &hash;
    if (state == null) {
        return 0;
    }
    hash = pwr_apt_hash_u64(hash, pwr_si_engine_state_hash(&state.*.engine));
    hash = pwr_apt_hash_u64(hash, pwr_automatic_state_hash(&state.*.automatic));
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            hash = pwr_apt_hash_double(hash, state.*.exhaust.volume[volume].total_mass_kg);
            {
                var species: usize = 0;
                _ = &species;
                while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
                    hash = pwr_apt_hash_double(hash, state.*.exhaust.volume[volume].species_mass_kg[species]);
                }
            }
            hash = pwr_apt_hash_double(hash, state.*.exhaust.volume[volume].internal_energy_j);
            hash = pwr_apt_hash_double(hash, state.*.exhaust.volume[volume].wall_temperature_k);
        }
    }
    hash = pwr_apt_hash_double(hash, state.*.exhaust.catalyst_conversion_fraction);
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            hash = pwr_apt_hash_double(hash, state.*.exhaust.cumulative_tailpipe_species_kg[species]);
        }
    }
    hash = pwr_apt_hash_double(hash, state.*.exhaust.time_s);
    hash = pwr_apt_hash_u64(hash, state.*.tick);
    hash = pwr_apt_hash_u64(hash, state.*.cranking_ticks);
    hash = pwr_apt_hash_u64(hash, state.*.first_gear_ticks);
    hash = pwr_apt_hash_u64(hash, @as(u64, @bitCast(@as(u64, state.*.last_control_mode))));
    hash = pwr_apt_hash_u64(hash, @as(u64, @bitCast(@as(u64, state.*.tcu_phase))));
    hash = pwr_apt_hash_u64(hash, @as(u64, @bitCast(@as(i64, @bitCast(@as(i64, state.*.supervised_gear))))));
    hash = pwr_apt_hash_u64(hash, @as(u64, @bitCast(@as(u64, if (@as(c_int, @intFromBool(state.*.engine_started)) != 0) @as(c_uint, 1) else @as(c_uint, 0)))));
    hash = pwr_apt_hash_u64(hash, @as(u64, @bitCast(@as(u64, if (@as(c_int, @intFromBool(state.*.ecu_powered)) != 0) @as(c_uint, 1) else @as(c_uint, 0)))));
    hash = pwr_apt_hash_u64(hash, @as(u64, @bitCast(@as(u64, if (@as(c_int, @intFromBool(state.*.tcu_powered)) != 0) @as(c_uint, 1) else @as(c_uint, 0)))));
    hash = pwr_apt_hash_double(hash, state.*.low_voltage_v);
    hash = pwr_apt_hash_double(hash, state.*.left_halfshaft_speed_rad_s);
    hash = pwr_apt_hash_double(hash, state.*.right_halfshaft_speed_rad_s);
    hash = pwr_apt_hash_double(hash, state.*.left_halfshaft_angle_rad);
    hash = pwr_apt_hash_double(hash, state.*.right_halfshaft_angle_rad);
    hash = pwr_apt_hash_double(hash, state.*.cumulative_halfshaft_drive_work_j);
    hash = pwr_apt_hash_double(hash, state.*.cumulative_road_load_work_j);
    hash = pwr_apt_hash_double(hash, state.*.cumulative_differential_projection_adjustment_j);
    hash = pwr_apt_hash_double(hash, state.*.cumulative_exhaust_mass_mismatch_kg);
    hash = pwr_apt_hash_double(hash, state.*.cumulative_exhaust_enthalpy_mismatch_j);
    hash = pwr_apt_hash_double(hash, state.*.cumulative_engine_converter_energy_mismatch_j);
    hash = pwr_apt_hash_double(hash, state.*.cumulative_final_drive_energy_mismatch_j);
    hash = pwr_apt_hash_u64(hash, @as(u64, @bitCast(@as(u64, state.*.last_step_diagnostic_flags))));
    hash = pwr_apt_hash_u64(hash, @as(u64, @bitCast(@as(u64, state.*.diagnostic_flags))));
    return hash;
}

pub fn pwr_automatic_powertrain_result_string(arg_result: pwr_automatic_powertrain_result) callconv(.c) [*c]const u8 {
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

pub fn pwr_apt_positive_finite(arg_value: f64) callconv(.c) bool {
    var value = arg_value;
    _ = &value;
    return (power_isfinite(value) != 0) and (value > 0.0);
}

pub fn pwr_apt_nonnegative_finite(arg_value: f64) callconv(.c) bool {
    var value = arg_value;
    _ = &value;
    return (power_isfinite(value) != 0) and (value >= 0.0);
}

pub fn pwr_apt_min(arg_left: f64, arg_right: f64) callconv(.c) f64 {
    var left = arg_left;
    _ = &left;
    var right = arg_right;
    _ = &right;
    return if (left < right) left else right;
}

pub fn pwr_apt_max(arg_left: f64, arg_right: f64) callconv(.c) f64 {
    var left = arg_left;
    _ = &left;
    var right = arg_right;
    _ = &right;
    return if (left > right) left else right;
}

pub fn pwr_apt_clamp(arg_value: f64, arg_minimum: f64, arg_maximum: f64) callconv(.c) f64 {
    var value = arg_value;
    _ = &value;
    var minimum = arg_minimum;
    _ = &minimum;
    var maximum = arg_maximum;
    _ = &maximum;
    return pwr_apt_min(pwr_apt_max(value, minimum), maximum);
}

pub fn pwr_apt_ambient_reservoir(arg_pressure_pa: f64, arg_temperature_k: f64) callconv(.c) pwr_exhaust_reservoir {
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

pub fn pwr_apt_exhaust_parameters_default(arg_parameters: [*c]pwr_exhaust_parameters) callconv(.c) void {
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
    parameters.*.maximum_substep_s = pwr_automatic_powertrain_tick_s;
    parameters.*.maximum_outflow_fraction_per_substep = 0.1;
    parameters.*.maximum_substeps = 8;
}

pub fn pwr_apt_gear_valid(arg_gear: pwr_automatic_gear) callconv(.c) bool {
    var gear = arg_gear;
    _ = &gear;
    return (((((gear == PWR_AUTOMATIC_GEAR_REVERSE) or (gear == PWR_AUTOMATIC_GEAR_NEUTRAL)) or (gear == PWR_AUTOMATIC_GEAR_FIRST)) or (gear == PWR_AUTOMATIC_GEAR_SECOND)) or (gear == PWR_AUTOMATIC_GEAR_THIRD)) or (gear == PWR_AUTOMATIC_GEAR_FOURTH);
}

pub fn pwr_apt_input_valid(arg_config: [*c]const pwr_automatic_powertrain_config, arg_input: [*c]const pwr_automatic_powertrain_input) callconv(.c) bool {
    var config = arg_config;
    _ = &config;
    var input = arg_input;
    _ = &input;
    const known_engine_faults: u32 = @as(u32, @bitCast(((PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT | PWR_SI_ENGINE_FAULT_THROTTLE_STUCK) | PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED) | PWR_SI_ENGINE_FAULT_LOW_VOLTAGE));
    _ = &known_engine_faults;
    const maximum_axle_load: f64 = (config.*.automatic.maximum_external_torque_nm * config.*.final_drive_ratio) * config.*.final_drive_efficiency;
    _ = &maximum_axle_load;
    if (((((((((((((input == null) or ((input.*.control_mode != @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_CONTROL_SUPERVISED))) and (input.*.control_mode != @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT))))) or !(power_isfinite(input.*.driver_throttle) != 0)) or (input.*.driver_throttle < 0.0)) or (input.*.driver_throttle > 1.0)) or !pwr_apt_positive_finite(input.*.ambient_pressure_pa)) or !pwr_apt_positive_finite(input.*.ambient_temperature_k)) or !pwr_apt_nonnegative_finite(input.*.electrical_supply_voltage_v)) or !pwr_apt_nonnegative_finite(input.*.left_additional_road_load_torque_nm)) or !pwr_apt_nonnegative_finite(input.*.right_additional_road_load_torque_nm)) or ((input.*.left_additional_road_load_torque_nm + input.*.right_additional_road_load_torque_nm) > maximum_axle_load)) or !pwr_apt_nonnegative_finite(input.*.tailpipe_area_scale)) or ((input.*.engine_fault_mask & ~known_engine_faults) != @as(c_uint, 0))) {
        return @as(c_int, 0) != 0;
    }
    if ((input.*.control_mode == @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT))) and ((((((!pwr_apt_nonnegative_finite(input.*.direct_starter_torque_nm) or !pwr_apt_nonnegative_finite(input.*.direct_line_pressure_command_pa)) or (input.*.direct_line_pressure_command_pa > config.*.automatic.maximum_line_pressure_pa)) or !(power_isfinite(input.*.direct_lockup_command) != 0)) or (input.*.direct_lockup_command < 0.0)) or (input.*.direct_lockup_command > 1.0)) or !pwr_apt_gear_valid(input.*.direct_requested_gear))) {
        return @as(c_int, 0) != 0;
    }
    return @as(c_int, 1) != 0;
}

pub fn pwr_apt_average_halfshaft_speed(arg_state: [*c]const pwr_automatic_powertrain_state) callconv(.c) f64 {
    var state = arg_state;
    _ = &state;
    return 0.5 * (state.*.left_halfshaft_speed_rad_s + state.*.right_halfshaft_speed_rad_s);
}

pub fn pwr_apt_preliminary_commands(arg_state: [*c]pwr_automatic_powertrain_state, arg_input: [*c]const pwr_automatic_powertrain_input, arg_commands: [*c]pwr_automatic_powertrain_commands) callconv(.c) void {
    var state = arg_state;
    _ = &state;
    var input = arg_input;
    _ = &input;
    var commands = arg_commands;
    _ = &commands;
    const config: [*c]const pwr_automatic_powertrain_config = &state.*.config;
    _ = &config;
    const engine_speed: f64 = state.*.engine_output.crank_speed_rad_s;
    _ = &engine_speed;
    const halfshaft_speed: f64 = fabs(pwr_apt_average_halfshaft_speed(state));
    _ = &halfshaft_speed;
    var idle_addition: f64 = undefined;
    _ = &idle_addition;
    _ = memset(@as(?*anyopaque, @ptrCast(commands)), @as(c_int, 0), @sizeOf(pwr_automatic_powertrain_commands));
    commands.*.engine_throttle = input.*.driver_throttle;
    commands.*.gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
    if (!input.*.ignition_on) {
        state.*.engine_started = @as(c_int, 0) != 0;
        state.*.cranking_ticks = 0;
    } else if (engine_speed >= config.*.starter_release_speed_rad_s) {
        state.*.engine_started = @as(c_int, 1) != 0;
    }
    if (input.*.control_mode == @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT))) {
        commands.*.starter_torque_nm = input.*.direct_starter_torque_nm;
        commands.*.line_pressure_pa = input.*.direct_line_pressure_command_pa;
        commands.*.lockup = input.*.direct_lockup_command;
        commands.*.gear = input.*.direct_requested_gear;
        state.*.tcu_phase = @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_TCU_DIRECT));
        return;
    }
    if (state.*.last_control_mode == @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT))) {
        state.*.supervised_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
        state.*.first_gear_ticks = 0;
    }
    if (((@as(c_int, @intFromBool(input.*.ignition_on)) != 0) and !state.*.engine_started) and ((@as(f64, @floatFromInt(state.*.cranking_ticks)) * pwr_automatic_powertrain_tick_s) < config.*.maximum_cranking_time_s)) {
        commands.*.starter_torque_nm = config.*.starter_torque_nm;
        commands.*.engine_throttle = pwr_apt_max(commands.*.engine_throttle, config.*.cranking_throttle_command);
        state.*.cranking_ticks +%= 1;
    }
    idle_addition = config.*.idle_throttle_command + (config.*.idle_speed_throttle_gain_s_per_rad * (config.*.idle_target_speed_rad_s - engine_speed));
    if (((@as(c_int, @intFromBool(input.*.ignition_on)) != 0) and (@as(c_int, @intFromBool(state.*.engine_started)) != 0)) and (engine_speed > 0.0)) {
        commands.*.engine_throttle = pwr_apt_max(commands.*.engine_throttle, pwr_apt_clamp(idle_addition, 0.0, 1.0));
    }
    if ((!input.*.ignition_on or !input.*.selector_drive) or !state.*.engine_started) {
        state.*.supervised_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
        state.*.first_gear_ticks = 0;
    } else if (state.*.supervised_gear == PWR_AUTOMATIC_GEAR_NEUTRAL) {
        state.*.supervised_gear = PWR_AUTOMATIC_GEAR_FIRST;
        state.*.first_gear_ticks = 0;
    } else if (state.*.supervised_gear == PWR_AUTOMATIC_GEAR_FIRST) {
        state.*.first_gear_ticks +%= 1;
        if ((((state.*.automatic_output.active_gear == PWR_AUTOMATIC_GEAR_FIRST) and !state.*.automatic_output.shift_active) and (halfshaft_speed >= config.*.first_to_second_halfshaft_speed_rad_s)) and ((@as(f64, @floatFromInt(state.*.first_gear_ticks)) * pwr_automatic_powertrain_tick_s) >= config.*.minimum_first_gear_time_s)) {
            state.*.supervised_gear = PWR_AUTOMATIC_GEAR_SECOND;
        }
    }
    commands.*.gear = state.*.supervised_gear;
    if (commands.*.gear != PWR_AUTOMATIC_GEAR_NEUTRAL) {
        commands.*.line_pressure_pa = pwr_apt_clamp(config.*.automatic.nominal_line_pressure_pa + (config.*.line_pressure_throttle_gain_pa * input.*.driver_throttle), 0.0, config.*.automatic.maximum_line_pressure_pa);
    }
    if ((((commands.*.gear == PWR_AUTOMATIC_GEAR_SECOND) and (state.*.automatic_output.active_gear == PWR_AUTOMATIC_GEAR_SECOND)) and !state.*.automatic_output.shift_active) and (halfshaft_speed >= config.*.lockup_enable_halfshaft_speed_rad_s)) {
        commands.*.lockup = 1.0;
    }
    if ((@as(c_int, @intFromBool(state.*.automatic_output.shift_active)) != 0) or (commands.*.gear != state.*.automatic_output.target_gear)) {
        commands.*.engine_throttle *= config.*.shift_engine_throttle_scale;
    }
    commands.*.engine_throttle = pwr_apt_clamp(commands.*.engine_throttle, 0.0, 1.0);
}

pub fn pwr_apt_electrical_and_brownout(arg_state: [*c]pwr_automatic_powertrain_state, arg_input: [*c]const pwr_automatic_powertrain_input, arg_commands: [*c]pwr_automatic_powertrain_commands, arg_step_flags: [*c]u32) callconv(.c) void {
    var state = arg_state;
    _ = &state;
    var input = arg_input;
    _ = &input;
    var commands = arg_commands;
    _ = &commands;
    var step_flags = arg_step_flags;
    _ = &step_flags;
    const config: [*c]const pwr_automatic_powertrain_config = &state.*.config;
    _ = &config;
    const pressure_fraction: f64 = pwr_apt_clamp(commands.*.line_pressure_pa / config.*.automatic.maximum_line_pressure_pa, 0.0, 1.0);
    _ = &pressure_fraction;
    const current_a: f64 = ((config.*.base_accessory_current_a + config.*.tcu_current_a) + (config.*.solenoid_current_at_max_pressure_a * pressure_fraction)) + (config.*.starter_current_per_nm_a * commands.*.starter_torque_nm);
    _ = &current_a;
    const open_circuit: f64 = pwr_apt_min(input.*.electrical_supply_voltage_v, config.*.electrical_nominal_voltage_v);
    _ = &open_circuit;
    state.*.low_voltage_v = pwr_apt_max(0.0, open_circuit - (config.*.electrical_internal_resistance_ohm * current_a));
    state.*.ecu_powered = (@as(c_int, @intFromBool(input.*.ignition_on)) != 0) and (state.*.low_voltage_v >= config.*.ecu_brownout_voltage_v);
    state.*.tcu_powered = (@as(c_int, @intFromBool(input.*.ignition_on)) != 0) and (state.*.low_voltage_v >= config.*.tcu_brownout_voltage_v);
    if (!state.*.ecu_powered and (@as(c_int, @intFromBool(input.*.ignition_on)) != 0)) {
        step_flags.* |= @as(u32, @bitCast(PWR_AUTOMATIC_POWERTRAIN_DIAG_ECU_BROWNOUT));
    }
    if (!state.*.tcu_powered and (@as(c_int, @intFromBool(input.*.ignition_on)) != 0)) {
        commands.*.gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
        commands.*.line_pressure_pa = 0.0;
        commands.*.lockup = 0.0;
        state.*.tcu_phase = @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_TCU_BROWNOUT));
        step_flags.* |= @as(u32, @bitCast(PWR_AUTOMATIC_POWERTRAIN_DIAG_TCU_BROWNOUT));
    }
    commands.*.starter_torque_nm *= pwr_apt_clamp(state.*.low_voltage_v / config.*.electrical_nominal_voltage_v, 0.0, 1.0);
}

pub fn pwr_apt_engine_exhaust_reservoir(arg_engine: [*c]const pwr_si_engine_output, arg_minimum_pressure_pa: f64, arg_minimum_temperature_k: f64) callconv(.c) pwr_exhaust_reservoir {
    var engine = arg_engine;
    _ = &engine;
    var minimum_pressure_pa = arg_minimum_pressure_pa;
    _ = &minimum_pressure_pa;
    var minimum_temperature_k = arg_minimum_temperature_k;
    _ = &minimum_temperature_k;
    var reservoir: pwr_exhaust_reservoir = pwr_exhaust_reservoir{
        .pressure_pa = pwr_apt_max(engine.*.exhaust_source_pressure_pa, minimum_pressure_pa),
        .temperature_k = pwr_apt_max(engine.*.exhaust_temperature_k, minimum_temperature_k),
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

pub fn pwr_apt_source_cp(arg_parameters: [*c]const pwr_exhaust_parameters, arg_engine: [*c]const pwr_si_engine_output) callconv(.c) f64 {
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

pub fn pwr_apt_road_load(arg_config: [*c]const pwr_automatic_powertrain_config, arg_speed_rad_s: f64, arg_additional_torque_nm: f64) callconv(.c) f64 {
    var config = arg_config;
    _ = &config;
    var speed_rad_s = arg_speed_rad_s;
    _ = &speed_rad_s;
    var additional_torque_nm = arg_additional_torque_nm;
    _ = &additional_torque_nm;
    const sign: f64 = tanh(speed_rad_s / config.*.road_load_regularization_speed_rad_s);
    _ = &sign;
    return (((config.*.rolling_resistance_torque_nm_per_side * sign) + (config.*.viscous_road_load_nm_s_per_rad_per_side * speed_rad_s)) + ((config.*.quadratic_road_load_nm_s2_per_rad2_per_side * speed_rad_s) * fabs(speed_rad_s))) + (additional_torque_nm * sign);
}

pub fn pwr_apt_update_tcu_phase(arg_state: [*c]pwr_automatic_powertrain_state, arg_input: [*c]const pwr_automatic_powertrain_input, arg_step_flags: [*c]u32) callconv(.c) void {
    var state = arg_state;
    _ = &state;
    var input = arg_input;
    _ = &input;
    var step_flags = arg_step_flags;
    _ = &step_flags;
    if (!state.*.tcu_powered and (@as(c_int, @intFromBool(input.*.ignition_on)) != 0)) {
        state.*.tcu_phase = @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_TCU_BROWNOUT));
    } else if (input.*.control_mode == @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT))) {
        state.*.tcu_phase = @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_TCU_DIRECT));
    } else if ((@as(c_int, @intFromBool(state.*.automatic_output.shift_active)) != 0) and (state.*.automatic_output.target_gear == PWR_AUTOMATIC_GEAR_SECOND)) {
        state.*.tcu_phase = @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_TCU_SHIFT_1_TO_2));
        step_flags.* |= @as(u32, @bitCast(PWR_AUTOMATIC_POWERTRAIN_DIAG_SHIFT_ACTIVE));
    } else if ((state.*.supervised_gear == PWR_AUTOMATIC_GEAR_SECOND) and (state.*.automatic_output.lockup_engagement > 0.5)) {
        state.*.tcu_phase = @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND_LOCKED));
        step_flags.* |= @as(u32, @bitCast(PWR_AUTOMATIC_POWERTRAIN_DIAG_LOCKUP_ACTIVE));
    } else if (state.*.supervised_gear == PWR_AUTOMATIC_GEAR_SECOND) {
        state.*.tcu_phase = @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND));
    } else if (state.*.supervised_gear == PWR_AUTOMATIC_GEAR_FIRST) {
        state.*.tcu_phase = @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_TCU_FIRST));
    } else {
        state.*.tcu_phase = @as(c_uint, @bitCast(PWR_AUTOMATIC_POWERTRAIN_TCU_NEUTRAL));
    }
}

pub fn pwr_apt_state_finite(arg_state: [*c]const pwr_automatic_powertrain_state) callconv(.c) bool {
    var state = arg_state;
    _ = &state;
    return (((((((((((power_isfinite(state.*.low_voltage_v) != 0) and (power_isfinite(state.*.left_halfshaft_speed_rad_s) != 0)) and (power_isfinite(state.*.right_halfshaft_speed_rad_s) != 0)) and (power_isfinite(state.*.left_halfshaft_angle_rad) != 0)) and (power_isfinite(state.*.right_halfshaft_angle_rad) != 0)) and (power_isfinite(state.*.cumulative_halfshaft_drive_work_j) != 0)) and (power_isfinite(state.*.cumulative_road_load_work_j) != 0)) and (power_isfinite(state.*.cumulative_differential_projection_adjustment_j) != 0)) and (power_isfinite(state.*.cumulative_exhaust_mass_mismatch_kg) != 0)) and (power_isfinite(state.*.cumulative_exhaust_enthalpy_mismatch_j) != 0)) and (power_isfinite(state.*.cumulative_engine_converter_energy_mismatch_j) != 0)) and (power_isfinite(state.*.cumulative_final_drive_energy_mismatch_j) != 0);
}

pub fn pwr_apt_hash_u64(arg_hash: u64, arg_value: u64) callconv(.c) u64 {
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

pub fn pwr_apt_hash_double(arg_hash: u64, arg_value: f64) callconv(.c) u64 {
    var hash = arg_hash;
    _ = &hash;
    var value = arg_value;
    _ = &value;
    var bits: u64 = 0;
    _ = &bits;
    _ = memcpy(@as(?*anyopaque, @ptrCast(&bits)), @as(?*const anyopaque, @ptrCast(&value)), @sizeOf(u64));
    return pwr_apt_hash_u64(hash, bits);
}
