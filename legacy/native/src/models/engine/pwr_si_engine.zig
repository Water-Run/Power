// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/models/engine/pwr_si_engine.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_INVALID = @import("../../abi.zig").PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_INVALID;
const PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_STALE = @import("../../abi.zig").PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_STALE;
const PWR_SI_ENGINE_DIAG_ECU_BROWNOUT = @import("../../abi.zig").PWR_SI_ENGINE_DIAG_ECU_BROWNOUT;
const PWR_SI_ENGINE_DIAG_FUEL_CUT = @import("../../abi.zig").PWR_SI_ENGINE_DIAG_FUEL_CUT;
const PWR_SI_ENGINE_DIAG_MANIFOLD_MASS_LIMIT = @import("../../abi.zig").PWR_SI_ENGINE_DIAG_MANIFOLD_MASS_LIMIT;
const PWR_SI_ENGINE_DIAG_NUMERIC_FAILURE = @import("../../abi.zig").PWR_SI_ENGINE_DIAG_NUMERIC_FAILURE;
const PWR_SI_ENGINE_DIAG_SPEED_LIMIT = @import("../../abi.zig").PWR_SI_ENGINE_DIAG_SPEED_LIMIT;
const PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT = @import("../../abi.zig").PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT;
const PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED = @import("../../abi.zig").PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED;
const PWR_SI_ENGINE_FAULT_LOW_VOLTAGE = @import("../../abi.zig").PWR_SI_ENGINE_FAULT_LOW_VOLTAGE;
const PWR_SI_ENGINE_FAULT_THROTTLE_STUCK = @import("../../abi.zig").PWR_SI_ENGINE_FAULT_THROTTLE_STUCK;
const PWR_SI_ENGINE_INVALID_ARGUMENT = @import("../../abi.zig").PWR_SI_ENGINE_INVALID_ARGUMENT;
const PWR_SI_ENGINE_INVALID_CONFIG = @import("../../abi.zig").PWR_SI_ENGINE_INVALID_CONFIG;
const PWR_SI_ENGINE_INVALID_INPUT = @import("../../abi.zig").PWR_SI_ENGINE_INVALID_INPUT;
const PWR_SI_ENGINE_NUMERIC_ERROR = @import("../../abi.zig").PWR_SI_ENGINE_NUMERIC_ERROR;
const PWR_SI_ENGINE_OK = @import("../../abi.zig").PWR_SI_ENGINE_OK;
const PWR_SI_ENGINE_SPECIES_COUNT = @import("../../abi.zig").PWR_SI_ENGINE_SPECIES_COUNT;
const PWR_SI_ENGINE_SPECIES_INERT = @import("../../abi.zig").PWR_SI_ENGINE_SPECIES_INERT;
const PWR_SI_ENGINE_SPECIES_OXYGEN = @import("../../abi.zig").PWR_SI_ENGINE_SPECIES_OXYGEN;
const PWR_SI_ENGINE_SPECIES_PRODUCTS = @import("../../abi.zig").PWR_SI_ENGINE_SPECIES_PRODUCTS;
const PWR_SI_ENGINE_SPECIES_REDUCTANT = @import("../../abi.zig").PWR_SI_ENGINE_SPECIES_REDUCTANT;
const cos = @import("../../support.zig").cos;
const exp = @import("../../support.zig").exp;
const fabs = @import("../../support.zig").fabs;
const fmod = @import("../../support.zig").fmod;
const memcpy = @import("../../support.zig").memcpy;
const pow = @import("../../support.zig").pow;
const power_isfinite = @import("../../support.zig").power_isfinite;
const pwr_si_engine_config = @import("../../abi.zig").pwr_si_engine_config;
const pwr_si_engine_input = @import("../../abi.zig").pwr_si_engine_input;
const pwr_si_engine_output = @import("../../abi.zig").pwr_si_engine_output;
const pwr_si_engine_result = @import("../../abi.zig").pwr_si_engine_result;
const pwr_si_engine_state = @import("../../abi.zig").pwr_si_engine_state;
const pwr_si_pi = @import("../../abi.zig").pwr_si_pi;
const sqrt = @import("../../support.zig").sqrt;
const tanh = @import("../../support.zig").tanh;

pub fn pwr_si_engine_config_default(arg_config: [*c]pwr_si_engine_config) callconv(.c) void {
    var config = arg_config;
    _ = &config;
    if (config == null) {
        return;
    }
    config.* = pwr_si_engine_config{
        .base_tick_ns = @as(u64, 100000),
        .sensor_period_ticks = @as(c_uint, 10),
        .ecu_period_ticks = @as(c_uint, 10),
        .maximum_sensor_age_ticks = @as(c_uint, 25),
        .cylinder_count = @as(c_uint, 4),
        .displacement_m3 = 0.0015,
        .compression_ratio = 10.0,
        .rotational_inertia_kg_m2 = 0.16,
        .intake_manifold_volume_m3 = 0.002,
        .throttle_effective_area_m2 = 0.0007,
        .throttle_discharge_coefficient = 0.72,
        .throttle_time_constant_s = 0.055,
        .volumetric_efficiency = 0.88,
        .air_gas_constant_j_per_kg_k = 287.05,
        .ambient_pressure_pa = 101325.0,
        .ambient_temperature_k = 298.15,
        .stoichiometric_air_fuel_ratio = 14.7,
        .oxygen_mass_fraction = 0.23,
        .oxygen_per_fuel_kg_per_kg = 3.38,
        .fuel_lower_heating_value_j_per_kg = 42500000.0,
        .indicated_efficiency = 0.32,
        .exhaust_heat_fraction = 0.56,
        .injector_time_constant_s = 0.008,
        .friction_coulomb_nm = 8.0,
        .friction_viscous_nm_s_per_rad = 0.035,
        .pumping_scale = 1.0,
        .combustion_min_speed_rad_s = 35.0,
        .maximum_speed_rad_s = 680.0,
        .combustion_torque_ripple_fraction = 0.12,
        .coolant_initial_temperature_k = 298.15,
        .coolant_heat_capacity_j_per_k = 90000.0,
        .coolant_ambient_conductance_w_per_k = 45.0,
        .exhaust_temperature_floor_k = 430.0,
        .exhaust_effective_heat_capacity_j_per_kg_k = 1100.0,
        .exhaust_port_resistance_pa_per_kg_s_squared = 8000000.0,
        .low_voltage_nominal_v = 13.6,
        .low_voltage_internal_resistance_ohm = 0.018,
        .ecu_current_a = 3.0,
        .injector_current_a = 4.0,
        .starter_current_per_nm_a = 0.85,
        .ecu_brownout_threshold_v = 8.5,
    };
}

pub fn pwr_si_engine_validate_config(arg_config: [*c]const pwr_si_engine_config) callconv(.c) pwr_si_engine_result {
    var config = arg_config;
    _ = &config;
    if (config == null) {
        return @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_ARGUMENT));
    }
    if ((((((config.*.base_tick_ns == @as(u64, 0)) or (config.*.sensor_period_ticks == @as(c_uint, 0))) or (config.*.ecu_period_ticks == @as(c_uint, 0))) or (config.*.maximum_sensor_age_ticks < config.*.sensor_period_ticks)) or (config.*.cylinder_count == @as(c_uint, 0))) or (config.*.cylinder_count > @as(c_uint, 16))) {
        return @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_CONFIG));
    }
    if (((((((((((((((((((((((((((((((((((((((((((((!pwr_si_positive_finite(config.*.displacement_m3) or !pwr_si_positive_finite(config.*.compression_ratio)) or (config.*.compression_ratio <= 1.0)) or !pwr_si_positive_finite(config.*.rotational_inertia_kg_m2)) or !pwr_si_positive_finite(config.*.intake_manifold_volume_m3)) or !pwr_si_positive_finite(config.*.throttle_effective_area_m2)) or !pwr_si_positive_finite(config.*.throttle_discharge_coefficient)) or (config.*.throttle_discharge_coefficient > 1.0)) or !pwr_si_positive_finite(config.*.throttle_time_constant_s)) or !pwr_si_positive_finite(config.*.volumetric_efficiency)) or (config.*.volumetric_efficiency > 2.0)) or !pwr_si_positive_finite(config.*.air_gas_constant_j_per_kg_k)) or !pwr_si_positive_finite(config.*.ambient_pressure_pa)) or !pwr_si_positive_finite(config.*.ambient_temperature_k)) or !pwr_si_positive_finite(config.*.stoichiometric_air_fuel_ratio)) or !pwr_si_positive_finite(config.*.oxygen_mass_fraction)) or (config.*.oxygen_mass_fraction >= 1.0)) or !pwr_si_positive_finite(config.*.oxygen_per_fuel_kg_per_kg)) or (config.*.oxygen_per_fuel_kg_per_kg > (config.*.oxygen_mass_fraction * config.*.stoichiometric_air_fuel_ratio))) or !pwr_si_positive_finite(config.*.fuel_lower_heating_value_j_per_kg)) or !pwr_si_positive_finite(config.*.indicated_efficiency)) or (config.*.indicated_efficiency >= 1.0)) or !pwr_si_nonnegative_finite(config.*.exhaust_heat_fraction)) or (config.*.exhaust_heat_fraction > 1.0)) or !pwr_si_positive_finite(config.*.injector_time_constant_s)) or !pwr_si_nonnegative_finite(config.*.friction_coulomb_nm)) or !pwr_si_nonnegative_finite(config.*.friction_viscous_nm_s_per_rad)) or !pwr_si_nonnegative_finite(config.*.pumping_scale)) or !pwr_si_positive_finite(config.*.combustion_min_speed_rad_s)) or !pwr_si_positive_finite(config.*.maximum_speed_rad_s)) or (config.*.maximum_speed_rad_s <= config.*.combustion_min_speed_rad_s)) or !pwr_si_nonnegative_finite(config.*.combustion_torque_ripple_fraction)) or (config.*.combustion_torque_ripple_fraction >= 1.0)) or !pwr_si_positive_finite(config.*.coolant_initial_temperature_k)) or !pwr_si_positive_finite(config.*.coolant_heat_capacity_j_per_k)) or !pwr_si_nonnegative_finite(config.*.coolant_ambient_conductance_w_per_k)) or !pwr_si_positive_finite(config.*.exhaust_temperature_floor_k)) or !pwr_si_positive_finite(config.*.exhaust_effective_heat_capacity_j_per_kg_k)) or !pwr_si_nonnegative_finite(config.*.exhaust_port_resistance_pa_per_kg_s_squared)) or !pwr_si_positive_finite(config.*.low_voltage_nominal_v)) or !pwr_si_nonnegative_finite(config.*.low_voltage_internal_resistance_ohm)) or !pwr_si_nonnegative_finite(config.*.ecu_current_a)) or !pwr_si_nonnegative_finite(config.*.injector_current_a)) or !pwr_si_nonnegative_finite(config.*.starter_current_per_nm_a)) or !pwr_si_positive_finite(config.*.ecu_brownout_threshold_v)) or (config.*.ecu_brownout_threshold_v >= config.*.low_voltage_nominal_v)) {
        return @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_CONFIG));
    }
    return @as(c_uint, @bitCast(PWR_SI_ENGINE_OK));
}

pub fn pwr_si_engine_init(arg_state: [*c]pwr_si_engine_state, arg_config: [*c]const pwr_si_engine_config) callconv(.c) pwr_si_engine_result {
    var state = arg_state;
    _ = &state;
    var config = arg_config;
    _ = &config;
    var candidate: pwr_si_engine_state = pwr_si_engine_state{
        .config = pwr_si_engine_config{
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
        .tick = @import("std").mem.zeroes(u64),
        .last_crank_sample_tick = @import("std").mem.zeroes(u64),
        .crank_angle_rad = 0,
        .crank_speed_rad_s = 0,
        .sensed_crank_speed_rad_s = 0,
        .throttle_position = 0,
        .latched_throttle_command = 0,
        .intake_air_mass_kg = 0,
        .injector_fuel_flow_kg_per_s = 0,
        .coolant_temperature_k = 0,
        .low_voltage_v = 0,
        .last_combustion_torque_nm = 0,
        .last_brake_torque_nm = 0,
        .last_pumping_torque_nm = 0,
        .last_friction_torque_nm = 0,
        .last_intake_air_flow_kg_per_s = 0,
        .last_cylinder_air_flow_kg_per_s = 0,
        .last_burned_fuel_flow_kg_per_s = 0,
        .last_lambda = 0,
        .last_exhaust_temperature_k = 0,
        .last_exhaust_source_pressure_pa = 0,
        .last_exhaust_species_mass_fraction = @import("std").mem.zeroes([4]f64),
        .initial_intake_air_mass_kg = 0,
        .cumulative_intake_air_kg = 0,
        .cumulative_cylinder_air_kg = 0,
        .cumulative_exhaust_mass_kg = 0,
        .cumulative_injected_fuel_kg = 0,
        .cumulative_burned_fuel_kg = 0,
        .cumulative_fuel_energy_j = 0,
        .cumulative_unburned_fuel_energy_j = 0,
        .cumulative_starter_work_j = 0,
        .cumulative_load_work_j = 0,
        .cumulative_exhaust_heat_j = 0,
        .cumulative_ambient_heat_j = 0,
        .cumulative_numerical_adjustment_j = 0,
        .initial_stored_energy_j = 0,
        .diagnostic_flags = @import("std").mem.zeroes(u32),
        .crank_signal_valid = false,
        .ecu_active = false,
        .combustion_active = false,
    };
    _ = &candidate;
    const validation: pwr_si_engine_result = pwr_si_engine_validate_config(config);
    _ = &validation;
    if (state == null) {
        return @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_ARGUMENT));
    }
    if (validation != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK))) {
        return validation;
    }
    candidate.config = config.*;
    candidate.intake_air_mass_kg = pwr_si_initial_air_mass(config);
    candidate.initial_intake_air_mass_kg = candidate.intake_air_mass_kg;
    candidate.coolant_temperature_k = config.*.coolant_initial_temperature_k;
    candidate.low_voltage_v = config.*.low_voltage_nominal_v;
    candidate.last_lambda = 10.0;
    candidate.last_exhaust_temperature_k = config.*.exhaust_temperature_floor_k;
    candidate.last_exhaust_source_pressure_pa = config.*.ambient_pressure_pa;
    candidate.last_exhaust_species_mass_fraction[@as(c_uint, @intCast(PWR_SI_ENGINE_SPECIES_INERT))] = 1.0 - config.*.oxygen_mass_fraction;
    candidate.last_exhaust_species_mass_fraction[@as(c_uint, @intCast(PWR_SI_ENGINE_SPECIES_OXYGEN))] = config.*.oxygen_mass_fraction;
    candidate.initial_stored_energy_j = pwr_si_stored_energy(&candidate);
    state.* = candidate;
    return @as(c_uint, @bitCast(PWR_SI_ENGINE_OK));
}

pub fn pwr_si_engine_step(arg_state: [*c]pwr_si_engine_state, arg_input: [*c]const pwr_si_engine_input, arg_output: [*c]pwr_si_engine_output) callconv(.c) pwr_si_engine_result {
    var state = arg_state;
    _ = &state;
    var input = arg_input;
    _ = &input;
    var output = arg_output;
    _ = &output;
    var candidate: pwr_si_engine_state = undefined;
    _ = &candidate;
    var result: pwr_si_engine_result = undefined;
    _ = &result;
    if ((state == null) or (output == null)) {
        return @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_ARGUMENT));
    }
    if (pwr_si_engine_validate_config(&state.*.config) != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK))) {
        return @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_CONFIG));
    }
    if (!pwr_si_input_valid(input)) {
        return @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_INPUT));
    }
    candidate = state.*;
    result = pwr_si_engine_advance(&candidate, input);
    if (result != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK))) {
        return result;
    }
    result = pwr_si_engine_observe(&candidate, output);
    if (result != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK))) {
        return result;
    }
    state.* = candidate;
    return @as(c_uint, @bitCast(PWR_SI_ENGINE_OK));
}

pub fn pwr_si_engine_observe(arg_state: [*c]const pwr_si_engine_state, arg_output: [*c]pwr_si_engine_output) callconv(.c) pwr_si_engine_result {
    var state = arg_state;
    _ = &state;
    var output = arg_output;
    _ = &output;
    var config: [*c]const pwr_si_engine_config = undefined;
    _ = &config;
    var manifold_pressure: f64 = undefined;
    _ = &manifold_pressure;
    var stored_change: f64 = undefined;
    _ = &stored_change;
    var air_residual: f64 = undefined;
    _ = &air_residual;
    var energy_residual: f64 = undefined;
    _ = &energy_residual;
    var sensor_age: u64 = undefined;
    _ = &sensor_age;
    if ((state == null) or (output == null)) {
        return @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_ARGUMENT));
    }
    config = &state.*.config;
    if (pwr_si_engine_validate_config(config) != @as(c_uint, @bitCast(PWR_SI_ENGINE_OK))) {
        return @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_CONFIG));
    }
    manifold_pressure = ((state.*.intake_air_mass_kg * config.*.air_gas_constant_j_per_kg_k) * config.*.ambient_temperature_k) / config.*.intake_manifold_volume_m3;
    stored_change = pwr_si_stored_energy(state) - state.*.initial_stored_energy_j;
    air_residual = ((state.*.initial_intake_air_mass_kg + state.*.cumulative_intake_air_kg) - state.*.cumulative_cylinder_air_kg) - state.*.intake_air_mass_kg;
    energy_residual = ((((((state.*.cumulative_fuel_energy_j + state.*.cumulative_starter_work_j) - state.*.cumulative_load_work_j) - state.*.cumulative_exhaust_heat_j) - state.*.cumulative_ambient_heat_j) - state.*.cumulative_unburned_fuel_energy_j) - stored_change) - state.*.cumulative_numerical_adjustment_j;
    sensor_age = if (@as(c_int, @intFromBool(state.*.crank_signal_valid)) != 0) state.*.tick -% state.*.last_crank_sample_tick else @as(u64, 18446744073709551615);
    output.* = pwr_si_engine_output{
        .time_s = (@as(f64, @floatFromInt(state.*.tick)) * @as(f64, @floatFromInt(config.*.base_tick_ns))) * 0.000000001,
        .crank_angle_rad = state.*.crank_angle_rad,
        .crank_speed_rad_s = state.*.crank_speed_rad_s,
        .sensed_crank_speed_rad_s = state.*.sensed_crank_speed_rad_s,
        .throttle_position = state.*.throttle_position,
        .manifold_pressure_pa = manifold_pressure,
        .low_voltage_v = state.*.low_voltage_v,
        .combustion_torque_nm = state.*.last_combustion_torque_nm,
        .brake_torque_nm = state.*.last_brake_torque_nm,
        .pumping_torque_nm = state.*.last_pumping_torque_nm,
        .friction_torque_nm = state.*.last_friction_torque_nm,
        .intake_air_flow_kg_per_s = state.*.last_intake_air_flow_kg_per_s,
        .cylinder_air_flow_kg_per_s = state.*.last_cylinder_air_flow_kg_per_s,
        .injected_fuel_flow_kg_per_s = state.*.injector_fuel_flow_kg_per_s,
        .burned_fuel_flow_kg_per_s = state.*.last_burned_fuel_flow_kg_per_s,
        .lambda = state.*.last_lambda,
        .exhaust_mass_flow_kg_per_s = state.*.last_cylinder_air_flow_kg_per_s + state.*.injector_fuel_flow_kg_per_s,
        .exhaust_temperature_k = state.*.last_exhaust_temperature_k,
        .exhaust_source_pressure_pa = state.*.last_exhaust_source_pressure_pa,
        .exhaust_species_mass_fraction = @import("std").mem.zeroes([4]f64),
        .coolant_temperature_k = state.*.coolant_temperature_k,
        .fuel_energy_j = state.*.cumulative_fuel_energy_j,
        .mechanical_load_work_j = state.*.cumulative_load_work_j,
        .exhaust_heat_j = state.*.cumulative_exhaust_heat_j,
        .ambient_heat_j = state.*.cumulative_ambient_heat_j,
        .stored_energy_change_j = stored_change,
        .unburned_fuel_energy_j = state.*.cumulative_unburned_fuel_energy_j,
        .numerical_adjustment_j = state.*.cumulative_numerical_adjustment_j,
        .energy_residual_j = energy_residual,
        .air_mass_residual_kg = air_residual,
        .crank_sensor_age_ticks = sensor_age,
        .diagnostic_flags = state.*.diagnostic_flags,
        .crank_signal_valid = state.*.crank_signal_valid,
        .ecu_active = state.*.ecu_active,
        .combustion_active = state.*.combustion_active,
        .state_hash = pwr_si_engine_state_hash(state),
    };
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_SI_ENGINE_SPECIES_COUNT)))) : (species +%= 1) {
            output.*.exhaust_species_mass_fraction[species] = state.*.last_exhaust_species_mass_fraction[species];
        }
    }
    return @as(c_uint, @bitCast(PWR_SI_ENGINE_OK));
}

pub fn pwr_si_engine_state_hash(arg_state: [*c]const pwr_si_engine_state) callconv(.c) u64 {
    var state = arg_state;
    _ = &state;
    var hash: u64 = 14695981039346656037;
    _ = &hash;
    if (state == null) {
        return 0;
    }
    hash = pwr_si_hash_u64(hash, state.*.tick);
    hash = pwr_si_hash_double(hash, state.*.crank_angle_rad);
    hash = pwr_si_hash_double(hash, state.*.crank_speed_rad_s);
    hash = pwr_si_hash_double(hash, state.*.sensed_crank_speed_rad_s);
    hash = pwr_si_hash_double(hash, state.*.throttle_position);
    hash = pwr_si_hash_double(hash, state.*.latched_throttle_command);
    hash = pwr_si_hash_double(hash, state.*.intake_air_mass_kg);
    hash = pwr_si_hash_double(hash, state.*.injector_fuel_flow_kg_per_s);
    hash = pwr_si_hash_double(hash, state.*.coolant_temperature_k);
    hash = pwr_si_hash_double(hash, state.*.low_voltage_v);
    hash = pwr_si_hash_double(hash, state.*.cumulative_intake_air_kg);
    hash = pwr_si_hash_double(hash, state.*.cumulative_cylinder_air_kg);
    hash = pwr_si_hash_double(hash, state.*.cumulative_exhaust_mass_kg);
    hash = pwr_si_hash_double(hash, state.*.cumulative_injected_fuel_kg);
    hash = pwr_si_hash_double(hash, state.*.cumulative_burned_fuel_kg);
    hash = pwr_si_hash_double(hash, state.*.cumulative_fuel_energy_j);
    hash = pwr_si_hash_double(hash, state.*.cumulative_starter_work_j);
    hash = pwr_si_hash_double(hash, state.*.cumulative_load_work_j);
    hash = pwr_si_hash_double(hash, state.*.cumulative_exhaust_heat_j);
    hash = pwr_si_hash_double(hash, state.*.cumulative_ambient_heat_j);
    hash = pwr_si_hash_double(hash, state.*.cumulative_numerical_adjustment_j);
    hash = pwr_si_hash_u64(hash, @as(u64, @bitCast(@as(u64, state.*.diagnostic_flags))));
    hash = pwr_si_hash_u64(hash, @as(u64, @bitCast(@as(u64, if (@as(c_int, @intFromBool(state.*.crank_signal_valid)) != 0) @as(c_uint, 1) else @as(c_uint, 0)))));
    hash = pwr_si_hash_u64(hash, @as(u64, @bitCast(@as(u64, if (@as(c_int, @intFromBool(state.*.ecu_active)) != 0) @as(c_uint, 1) else @as(c_uint, 0)))));
    hash = pwr_si_hash_u64(hash, @as(u64, @bitCast(@as(u64, if (@as(c_int, @intFromBool(state.*.combustion_active)) != 0) @as(c_uint, 1) else @as(c_uint, 0)))));
    return hash;
}

pub fn pwr_si_positive_finite(arg_value: f64) callconv(.c) bool {
    var value = arg_value;
    _ = &value;
    return (power_isfinite(value) != 0) and (value > 0.0);
}

pub fn pwr_si_nonnegative_finite(arg_value: f64) callconv(.c) bool {
    var value = arg_value;
    _ = &value;
    return (power_isfinite(value) != 0) and (value >= 0.0);
}

pub fn pwr_si_min(arg_left: f64, arg_right: f64) callconv(.c) f64 {
    var left = arg_left;
    _ = &left;
    var right = arg_right;
    _ = &right;
    return if (left < right) left else right;
}

pub fn pwr_si_max(arg_left: f64, arg_right: f64) callconv(.c) f64 {
    var left = arg_left;
    _ = &left;
    var right = arg_right;
    _ = &right;
    return if (left > right) left else right;
}

pub fn pwr_si_clamp(arg_value: f64, arg_minimum: f64, arg_maximum: f64) callconv(.c) f64 {
    var value = arg_value;
    _ = &value;
    var minimum = arg_minimum;
    _ = &minimum;
    var maximum = arg_maximum;
    _ = &maximum;
    return pwr_si_min(pwr_si_max(value, minimum), maximum);
}

pub fn pwr_si_initial_air_mass(arg_config: [*c]const pwr_si_engine_config) callconv(.c) f64 {
    var config = arg_config;
    _ = &config;
    return (config.*.ambient_pressure_pa * config.*.intake_manifold_volume_m3) / (config.*.air_gas_constant_j_per_kg_k * config.*.ambient_temperature_k);
}

pub fn pwr_si_stored_energy(arg_state: [*c]const pwr_si_engine_state) callconv(.c) f64 {
    var state = arg_state;
    _ = &state;
    const config: [*c]const pwr_si_engine_config = &state.*.config;
    _ = &config;
    const kinetic: f64 = ((0.5 * config.*.rotational_inertia_kg_m2) * state.*.crank_speed_rad_s) * state.*.crank_speed_rad_s;
    _ = &kinetic;
    const thermal: f64 = config.*.coolant_heat_capacity_j_per_k * (state.*.coolant_temperature_k - config.*.coolant_initial_temperature_k);
    _ = &thermal;
    return kinetic + thermal;
}

pub fn pwr_si_input_valid(arg_input: [*c]const pwr_si_engine_input) callconv(.c) bool {
    var input = arg_input;
    _ = &input;
    const known_faults: u32 = @as(u32, @bitCast(((PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT | PWR_SI_ENGINE_FAULT_THROTTLE_STUCK) | PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED) | PWR_SI_ENGINE_FAULT_LOW_VOLTAGE));
    _ = &known_faults;
    return (((((((((input != null) and (power_isfinite(input.*.driver_throttle) != 0)) and (input.*.driver_throttle >= 0.0)) and (input.*.driver_throttle <= 1.0)) and (power_isfinite(input.*.load_torque_nm) != 0)) and (@as(c_int, @intFromBool(pwr_si_nonnegative_finite(input.*.starter_torque_nm))) != 0)) and (@as(c_int, @intFromBool(pwr_si_positive_finite(input.*.exhaust_backpressure_pa))) != 0)) and (@as(c_int, @intFromBool(pwr_si_positive_finite(input.*.ambient_pressure_pa))) != 0)) and (@as(c_int, @intFromBool(pwr_si_positive_finite(input.*.ambient_temperature_k))) != 0)) and ((input.*.fault_mask & ~known_faults) == @as(c_uint, 0));
}

pub fn pwr_si_orifice_flow(arg_config: [*c]const pwr_si_engine_config, arg_throttle_position: f64, arg_upstream_pressure_pa: f64, arg_upstream_temperature_k: f64, arg_downstream_pressure_pa: f64) callconv(.c) f64 {
    var config = arg_config;
    _ = &config;
    var throttle_position = arg_throttle_position;
    _ = &throttle_position;
    var upstream_pressure_pa = arg_upstream_pressure_pa;
    _ = &upstream_pressure_pa;
    var upstream_temperature_k = arg_upstream_temperature_k;
    _ = &upstream_temperature_k;
    var downstream_pressure_pa = arg_downstream_pressure_pa;
    _ = &downstream_pressure_pa;
    const gamma: f64 = 1.4;
    _ = &gamma;
    const pressure_scale: f64 = pwr_si_max(upstream_pressure_pa, downstream_pressure_pa);
    _ = &pressure_scale;
    const pressure_difference: f64 = upstream_pressure_pa - downstream_pressure_pa;
    _ = &pressure_difference;
    var direction: f64 = 1.0;
    _ = &direction;
    var high_pressure: f64 = upstream_pressure_pa;
    _ = &high_pressure;
    var low_pressure: f64 = downstream_pressure_pa;
    _ = &low_pressure;
    var pressure_ratio: f64 = undefined;
    _ = &pressure_ratio;
    var flow_factor: f64 = undefined;
    _ = &flow_factor;
    var critical_ratio: f64 = undefined;
    _ = &critical_ratio;
    if (fabs(pressure_difference) <= (0.000000000001 * pressure_scale)) {
        return 0.0;
    }
    if (pressure_difference < 0.0) {
        direction = -1.0;
        high_pressure = downstream_pressure_pa;
        low_pressure = upstream_pressure_pa;
    }
    pressure_ratio = pwr_si_clamp(low_pressure / high_pressure, 0.0, 1.0);
    critical_ratio = pow(2.0 / (gamma + 1.0), gamma / (gamma - 1.0));
    if (pressure_ratio <= critical_ratio) {
        flow_factor = sqrt(gamma) * pow(2.0 / (gamma + 1.0), (gamma + 1.0) / (2.0 * (gamma - 1.0)));
    } else {
        const first: f64 = pow(pressure_ratio, 2.0 / gamma);
        _ = &first;
        const second: f64 = pow(pressure_ratio, (gamma + 1.0) / gamma);
        _ = &second;
        flow_factor = sqrt(pwr_si_max(0.0, ((2.0 * gamma) / (gamma - 1.0)) * (first - second)));
    }
    return (((((direction * config.*.throttle_discharge_coefficient) * config.*.throttle_effective_area_m2) * throttle_position) * high_pressure) / sqrt(config.*.air_gas_constant_j_per_kg_k * upstream_temperature_k)) * flow_factor;
}

pub fn pwr_si_hash_u64(arg_hash: u64, arg_value: u64) callconv(.c) u64 {
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

pub fn pwr_si_hash_double(arg_hash: u64, arg_value: f64) callconv(.c) u64 {
    var hash = arg_hash;
    _ = &hash;
    var value = arg_value;
    _ = &value;
    var bits: u64 = 0;
    _ = &bits;
    _ = memcpy(@as(?*anyopaque, @ptrCast(&bits)), @as(?*const anyopaque, @ptrCast(&value)), @sizeOf(u64));
    return pwr_si_hash_u64(hash, bits);
}

pub fn pwr_si_engine_advance(arg_state: [*c]pwr_si_engine_state, arg_input: [*c]const pwr_si_engine_input) callconv(.c) pwr_si_engine_result {
    var state = arg_state;
    _ = &state;
    var input = arg_input;
    _ = &input;
    const config: [*c]const pwr_si_engine_config = &state.*.config;
    _ = &config;
    const dt: f64 = @as(f64, @floatFromInt(config.*.base_tick_ns)) * 0.000000001;
    _ = &dt;
    var accessory_current: f64 = undefined;
    _ = &accessory_current;
    var manifold_pressure: f64 = undefined;
    _ = &manifold_pressure;
    var intake_flow: f64 = undefined;
    _ = &intake_flow;
    var cylinder_flow: f64 = undefined;
    _ = &cylinder_flow;
    var minimum_manifold_mass: f64 = undefined;
    _ = &minimum_manifold_mass;
    var fuel_command: f64 = 0.0;
    _ = &fuel_command;
    var burned_fuel_flow: f64 = 0.0;
    _ = &burned_fuel_flow;
    var combustion_torque: f64 = 0.0;
    _ = &combustion_torque;
    var pumping_torque: f64 = undefined;
    _ = &pumping_torque;
    var friction_torque: f64 = undefined;
    _ = &friction_torque;
    var average_speed: f64 = undefined;
    _ = &average_speed;
    var next_speed: f64 = undefined;
    _ = &next_speed;
    var ripple: f64 = undefined;
    _ = &ripple;
    var burned_energy: f64 = undefined;
    _ = &burned_energy;
    var combustion_work: f64 = undefined;
    _ = &combustion_work;
    var starter_work: f64 = undefined;
    _ = &starter_work;
    var pumping_work: f64 = undefined;
    _ = &pumping_work;
    var friction_work: f64 = undefined;
    _ = &friction_work;
    var load_work: f64 = undefined;
    _ = &load_work;
    var net_mechanical_work: f64 = undefined;
    _ = &net_mechanical_work;
    var kinetic_change: f64 = undefined;
    _ = &kinetic_change;
    var residual_combustion_heat: f64 = undefined;
    _ = &residual_combustion_heat;
    var exhaust_heat: f64 = undefined;
    _ = &exhaust_heat;
    var coolant_heat: f64 = undefined;
    _ = &coolant_heat;
    var ambient_heat: f64 = undefined;
    _ = &ambient_heat;
    var exhaust_mass_flow: f64 = undefined;
    _ = &exhaust_mass_flow;
    var unburned_fuel_flow: f64 = undefined;
    _ = &unburned_fuel_flow;
    var species_mass_flow: [4]f64 = [1]f64{
        0.0,
    } ++ [1]f64{0} ** 3;
    _ = &species_mass_flow;
    accessory_current = config.*.ecu_current_a + (config.*.starter_current_per_nm_a * input.*.starter_torque_nm);
    if (state.*.injector_fuel_flow_kg_per_s > 0.0) {
        accessory_current += config.*.injector_current_a;
    }
    state.*.low_voltage_v = config.*.low_voltage_nominal_v - (config.*.low_voltage_internal_resistance_ohm * accessory_current);
    if ((input.*.fault_mask & @as(u32, @bitCast(PWR_SI_ENGINE_FAULT_LOW_VOLTAGE))) != @as(c_uint, 0)) {
        state.*.low_voltage_v = 0.0;
    }
    if ((state.*.tick % @as(u64, @bitCast(@as(u64, config.*.sensor_period_ticks)))) == @as(u64, 0)) {
        if ((input.*.fault_mask & @as(u32, @bitCast(PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT))) != @as(c_uint, 0)) {
            state.*.crank_signal_valid = @as(c_int, 0) != 0;
            state.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_INVALID));
        } else {
            state.*.sensed_crank_speed_rad_s = state.*.crank_speed_rad_s;
            state.*.last_crank_sample_tick = state.*.tick;
            state.*.crank_signal_valid = @as(c_int, 1) != 0;
        }
    }
    if ((state.*.tick % @as(u64, @bitCast(@as(u64, config.*.ecu_period_ticks)))) == @as(u64, 0)) {
        const sensor_age: u64 = if (@as(c_int, @intFromBool(state.*.crank_signal_valid)) != 0) state.*.tick -% state.*.last_crank_sample_tick else @as(u64, 18446744073709551615);
        _ = &sensor_age;
        state.*.ecu_active = state.*.low_voltage_v >= config.*.ecu_brownout_threshold_v;
        if (!state.*.ecu_active) {
            state.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SI_ENGINE_DIAG_ECU_BROWNOUT));
            state.*.latched_throttle_command = 0.0;
        } else {
            state.*.latched_throttle_command = input.*.driver_throttle;
        }
        if (sensor_age > @as(u64, @bitCast(@as(u64, config.*.maximum_sensor_age_ticks)))) {
            state.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_STALE));
        }
    }
    if ((input.*.fault_mask & @as(u32, @bitCast(PWR_SI_ENGINE_FAULT_THROTTLE_STUCK))) == @as(c_uint, 0)) {
        const response: f64 = 1.0 - exp(-dt / config.*.throttle_time_constant_s);
        _ = &response;
        state.*.throttle_position += response * (state.*.latched_throttle_command - state.*.throttle_position);
        state.*.throttle_position = pwr_si_clamp(state.*.throttle_position, 0.0, 1.0);
    }
    manifold_pressure = ((state.*.intake_air_mass_kg * config.*.air_gas_constant_j_per_kg_k) * input.*.ambient_temperature_k) / config.*.intake_manifold_volume_m3;
    intake_flow = pwr_si_orifice_flow(config, state.*.throttle_position, input.*.ambient_pressure_pa, input.*.ambient_temperature_k, manifold_pressure);
    cylinder_flow = ((((manifold_pressure / (config.*.air_gas_constant_j_per_kg_k * input.*.ambient_temperature_k)) * config.*.displacement_m3) * config.*.volumetric_efficiency) * state.*.crank_speed_rad_s) / (4.0 * pwr_si_pi);
    cylinder_flow = pwr_si_max(0.0, cylinder_flow);
    minimum_manifold_mass = ((0.03 * input.*.ambient_pressure_pa) * config.*.intake_manifold_volume_m3) / (config.*.air_gas_constant_j_per_kg_k * input.*.ambient_temperature_k);
    if (((intake_flow - cylinder_flow) * dt) < (minimum_manifold_mass - state.*.intake_air_mass_kg)) {
        cylinder_flow = pwr_si_max(0.0, intake_flow + ((state.*.intake_air_mass_kg - minimum_manifold_mass) / dt));
        state.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SI_ENGINE_DIAG_MANIFOLD_MASS_LIMIT));
    }
    state.*.intake_air_mass_kg += (intake_flow - cylinder_flow) * dt;
    state.*.cumulative_intake_air_kg += intake_flow * dt;
    state.*.cumulative_cylinder_air_kg += cylinder_flow * dt;
    {
        const sensor_age: u64 = if (@as(c_int, @intFromBool(state.*.crank_signal_valid)) != 0) state.*.tick -% state.*.last_crank_sample_tick else @as(u64, 18446744073709551615);
        _ = &sensor_age;
        const speed_valid: bool = (@as(c_int, @intFromBool(state.*.crank_signal_valid)) != 0) and (sensor_age <= @as(u64, @bitCast(@as(u64, config.*.maximum_sensor_age_ticks))));
        _ = &speed_valid;
        const below_limit: bool = state.*.sensed_crank_speed_rad_s < config.*.maximum_speed_rad_s;
        _ = &below_limit;
        state.*.combustion_active = (((((@as(c_int, @intFromBool(input.*.ignition_on)) != 0) and (@as(c_int, @intFromBool(state.*.ecu_active)) != 0)) and (@as(c_int, @intFromBool(speed_valid)) != 0)) and (@as(c_int, @intFromBool(below_limit)) != 0)) and (state.*.crank_speed_rad_s >= config.*.combustion_min_speed_rad_s)) and ((input.*.fault_mask & @as(u32, @bitCast(PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED))) == @as(c_uint, 0));
        if (state.*.sensed_crank_speed_rad_s >= config.*.maximum_speed_rad_s) {
            state.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SI_ENGINE_DIAG_SPEED_LIMIT));
        }
        if (state.*.combustion_active) {
            fuel_command = cylinder_flow / config.*.stoichiometric_air_fuel_ratio;
        } else {
            state.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SI_ENGINE_DIAG_FUEL_CUT));
        }
    }
    {
        const injector_response: f64 = 1.0 - exp(-dt / config.*.injector_time_constant_s);
        _ = &injector_response;
        state.*.injector_fuel_flow_kg_per_s += injector_response * (fuel_command - state.*.injector_fuel_flow_kg_per_s);
        state.*.injector_fuel_flow_kg_per_s = pwr_si_max(0.0, state.*.injector_fuel_flow_kg_per_s);
    }
    if (state.*.combustion_active) {
        const oxygen_available: f64 = cylinder_flow * config.*.oxygen_mass_fraction;
        _ = &oxygen_available;
        burned_fuel_flow = pwr_si_min(state.*.injector_fuel_flow_kg_per_s, oxygen_available / config.*.oxygen_per_fuel_kg_per_kg);
    }
    if (state.*.injector_fuel_flow_kg_per_s > 0.000000000001) {
        state.*.last_lambda = cylinder_flow / (state.*.injector_fuel_flow_kg_per_s * config.*.stoichiometric_air_fuel_ratio);
    } else {
        state.*.last_lambda = 10.0;
    }
    ripple = 1.0 + (config.*.combustion_torque_ripple_fraction * cos((0.5 * @as(f64, @floatFromInt(config.*.cylinder_count))) * state.*.crank_angle_rad));
    if (burned_fuel_flow > 0.0) {
        const indicated_power: f64 = (burned_fuel_flow * config.*.fuel_lower_heating_value_j_per_kg) * config.*.indicated_efficiency;
        _ = &indicated_power;
        combustion_torque = (indicated_power / pwr_si_max(state.*.crank_speed_rad_s, config.*.combustion_min_speed_rad_s)) * ripple;
    }
    pumping_torque = ((config.*.pumping_scale * pwr_si_max(input.*.exhaust_backpressure_pa - manifold_pressure, 0.0)) * config.*.displacement_m3) / (4.0 * pwr_si_pi);
    friction_torque = (config.*.friction_coulomb_nm * tanh(state.*.crank_speed_rad_s / 5.0)) + (config.*.friction_viscous_nm_s_per_rad * state.*.crank_speed_rad_s);
    next_speed = state.*.crank_speed_rad_s + ((((((combustion_torque + input.*.starter_torque_nm) - pumping_torque) - friction_torque) - input.*.load_torque_nm) / config.*.rotational_inertia_kg_m2) * dt);
    if (next_speed < 0.0) {
        next_speed = 0.0;
        state.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SI_ENGINE_DIAG_SPEED_LIMIT));
    }
    average_speed = 0.5 * (state.*.crank_speed_rad_s + next_speed);
    burned_energy = (burned_fuel_flow * config.*.fuel_lower_heating_value_j_per_kg) * dt;
    combustion_work = (combustion_torque * average_speed) * dt;
    starter_work = (input.*.starter_torque_nm * average_speed) * dt;
    pumping_work = (pumping_torque * average_speed) * dt;
    friction_work = (friction_torque * average_speed) * dt;
    load_work = (input.*.load_torque_nm * average_speed) * dt;
    net_mechanical_work = (((combustion_work + starter_work) - pumping_work) - friction_work) - load_work;
    kinetic_change = (0.5 * config.*.rotational_inertia_kg_m2) * ((next_speed * next_speed) - (state.*.crank_speed_rad_s * state.*.crank_speed_rad_s));
    state.*.cumulative_numerical_adjustment_j += net_mechanical_work - kinetic_change;
    state.*.crank_speed_rad_s = next_speed;
    state.*.crank_angle_rad = fmod(state.*.crank_angle_rad + (average_speed * dt), 4.0 * pwr_si_pi);
    residual_combustion_heat = pwr_si_max(0.0, burned_energy - combustion_work);
    exhaust_heat = (config.*.exhaust_heat_fraction * residual_combustion_heat) + pumping_work;
    coolant_heat = ((1.0 - config.*.exhaust_heat_fraction) * residual_combustion_heat) + friction_work;
    ambient_heat = (config.*.coolant_ambient_conductance_w_per_k * (state.*.coolant_temperature_k - input.*.ambient_temperature_k)) * dt;
    state.*.coolant_temperature_k += (coolant_heat - ambient_heat) / config.*.coolant_heat_capacity_j_per_k;
    unburned_fuel_flow = state.*.injector_fuel_flow_kg_per_s - burned_fuel_flow;
    exhaust_mass_flow = cylinder_flow + state.*.injector_fuel_flow_kg_per_s;
    species_mass_flow[@as(c_uint, @intCast(PWR_SI_ENGINE_SPECIES_INERT))] = cylinder_flow * (1.0 - config.*.oxygen_mass_fraction);
    species_mass_flow[@as(c_uint, @intCast(PWR_SI_ENGINE_SPECIES_OXYGEN))] = pwr_si_max(0.0, (cylinder_flow * config.*.oxygen_mass_fraction) - (burned_fuel_flow * config.*.oxygen_per_fuel_kg_per_kg));
    species_mass_flow[@as(c_uint, @intCast(PWR_SI_ENGINE_SPECIES_REDUCTANT))] = unburned_fuel_flow;
    species_mass_flow[@as(c_uint, @intCast(PWR_SI_ENGINE_SPECIES_PRODUCTS))] = burned_fuel_flow * (1.0 + config.*.oxygen_per_fuel_kg_per_kg);
    if (exhaust_mass_flow > 0.000000000001) {
        {
            var species: usize = 0;
            _ = &species;
            while (species < @as(usize, @bitCast(@as(i64, PWR_SI_ENGINE_SPECIES_COUNT)))) : (species +%= 1) {
                state.*.last_exhaust_species_mass_fraction[species] = species_mass_flow[species] / exhaust_mass_flow;
            }
        }
    }
    state.*.last_exhaust_temperature_k = config.*.exhaust_temperature_floor_k;
    if (exhaust_mass_flow > 0.000000000001) {
        state.*.last_exhaust_temperature_k = pwr_si_max(config.*.exhaust_temperature_floor_k, input.*.ambient_temperature_k + (exhaust_heat / ((dt * exhaust_mass_flow) * config.*.exhaust_effective_heat_capacity_j_per_kg_k)));
    }
    state.*.last_exhaust_source_pressure_pa = input.*.exhaust_backpressure_pa + ((config.*.exhaust_port_resistance_pa_per_kg_s_squared * exhaust_mass_flow) * exhaust_mass_flow);
    state.*.cumulative_exhaust_mass_kg += exhaust_mass_flow * dt;
    state.*.cumulative_injected_fuel_kg += state.*.injector_fuel_flow_kg_per_s * dt;
    state.*.cumulative_burned_fuel_kg += burned_fuel_flow * dt;
    state.*.cumulative_fuel_energy_j += (state.*.injector_fuel_flow_kg_per_s * config.*.fuel_lower_heating_value_j_per_kg) * dt;
    state.*.cumulative_unburned_fuel_energy_j += (unburned_fuel_flow * config.*.fuel_lower_heating_value_j_per_kg) * dt;
    state.*.cumulative_starter_work_j += starter_work;
    state.*.cumulative_load_work_j += load_work;
    state.*.cumulative_exhaust_heat_j += exhaust_heat;
    state.*.cumulative_ambient_heat_j += ambient_heat;
    state.*.last_combustion_torque_nm = combustion_torque;
    state.*.last_pumping_torque_nm = pumping_torque;
    state.*.last_friction_torque_nm = friction_torque;
    state.*.last_brake_torque_nm = (combustion_torque - pumping_torque) - friction_torque;
    state.*.last_intake_air_flow_kg_per_s = intake_flow;
    state.*.last_cylinder_air_flow_kg_per_s = cylinder_flow;
    state.*.last_burned_fuel_flow_kg_per_s = burned_fuel_flow;
    state.*.tick +%= 1;
    if ((((((!(power_isfinite(state.*.crank_angle_rad) != 0) or !pwr_si_nonnegative_finite(state.*.crank_speed_rad_s)) or !pwr_si_positive_finite(state.*.intake_air_mass_kg)) or !pwr_si_nonnegative_finite(state.*.injector_fuel_flow_kg_per_s)) or !pwr_si_positive_finite(state.*.coolant_temperature_k)) or !(power_isfinite(state.*.last_exhaust_temperature_k) != 0)) or !(power_isfinite(state.*.last_exhaust_source_pressure_pa) != 0)) {
        state.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SI_ENGINE_DIAG_NUMERIC_FAILURE));
        return @as(c_uint, @bitCast(PWR_SI_ENGINE_NUMERIC_ERROR));
    }
    return @as(c_uint, @bitCast(PWR_SI_ENGINE_OK));
}
