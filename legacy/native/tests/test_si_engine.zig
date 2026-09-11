// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/tests/test_si_engine.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_INVALID = @import("../src/abi.zig").PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_INVALID;
const PWR_SI_ENGINE_DIAG_ECU_BROWNOUT = @import("../src/abi.zig").PWR_SI_ENGINE_DIAG_ECU_BROWNOUT;
const PWR_SI_ENGINE_DIAG_FUEL_CUT = @import("../src/abi.zig").PWR_SI_ENGINE_DIAG_FUEL_CUT;
const PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT = @import("../src/abi.zig").PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT;
const PWR_SI_ENGINE_FAULT_LOW_VOLTAGE = @import("../src/abi.zig").PWR_SI_ENGINE_FAULT_LOW_VOLTAGE;
const PWR_SI_ENGINE_FAULT_NONE = @import("../src/abi.zig").PWR_SI_ENGINE_FAULT_NONE;
const PWR_SI_ENGINE_INVALID_ARGUMENT = @import("../src/abi.zig").PWR_SI_ENGINE_INVALID_ARGUMENT;
const PWR_SI_ENGINE_INVALID_CONFIG = @import("../src/abi.zig").PWR_SI_ENGINE_INVALID_CONFIG;
const PWR_SI_ENGINE_INVALID_INPUT = @import("../src/abi.zig").PWR_SI_ENGINE_INVALID_INPUT;
const PWR_SI_ENGINE_OK = @import("../src/abi.zig").PWR_SI_ENGINE_OK;
const PWR_SI_ENGINE_SPECIES_COUNT = @import("../src/abi.zig").PWR_SI_ENGINE_SPECIES_COUNT;
const fabs = @import("../src/support.zig").fabs;
const fprintf = @import("../src/support.zig").fprintf;
const memcmp = @import("../src/support.zig").memcmp;
const pwr_si_engine_config = @import("../src/abi.zig").pwr_si_engine_config;
const pwr_si_engine_config_default = @import("../src/models/engine/pwr_si_engine.zig").pwr_si_engine_config_default;
const pwr_si_engine_init = @import("../src/models/engine/pwr_si_engine.zig").pwr_si_engine_init;
const pwr_si_engine_input = @import("../src/abi.zig").pwr_si_engine_input;
const pwr_si_engine_output = @import("../src/abi.zig").pwr_si_engine_output;
const pwr_si_engine_state = @import("../src/abi.zig").pwr_si_engine_state;
const pwr_si_engine_step = @import("../src/models/engine/pwr_si_engine.zig").pwr_si_engine_step;

// A 32 Nm test load stays below the speed limiter after five seconds.
// The historical 22 Nm case is retained in the explicit limiter test below.
pub fn nominal_input() callconv(.c) pwr_si_engine_input {
    return pwr_si_engine_input{
        .driver_throttle = 0.42,
        .load_torque_nm = 32.0,
        .starter_torque_nm = 0.0,
        .exhaust_backpressure_pa = 103000.0,
        .ambient_pressure_pa = 101325.0,
        .ambient_temperature_k = 298.15,
        .fault_mask = @as(u32, @bitCast(PWR_SI_ENGINE_FAULT_NONE)),
        .ignition_on = @as(c_int, 1) != 0,
    };
}

pub fn run_started_engine(arg_state: [*c]pwr_si_engine_state, arg_output: [*c]pwr_si_engine_output, arg_backpressure_pa: f64) callconv(.c) c_int {
    var state = arg_state;
    _ = &state;
    var output = arg_output;
    _ = &output;
    var backpressure_pa = arg_backpressure_pa;
    _ = &backpressure_pa;
    var input: pwr_si_engine_input = nominal_input();
    _ = &input;
    input.exhaust_backpressure_pa = backpressure_pa;
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, 50000)) : (step +%= 1) {
            input.starter_torque_nm = if (step < @as(u64, 8000)) 55.0 else 0.0;
            while (true) {
                if (!(pwr_si_engine_step(state, &input, output) == @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 46), "pwr_si_engine_step(state, &input, output) == PWR_SI_ENGINE_OK" });
                    return 0;
                }
                if (!false) break;
            }
        }
    }
    return 1;
}

pub fn test_invalid_configuration_and_transaction() callconv(.c) c_int {
    var config: pwr_si_engine_config = undefined;
    _ = &config;
    var state: pwr_si_engine_state = undefined;
    _ = &state;
    var before: pwr_si_engine_state = undefined;
    _ = &before;
    var output: pwr_si_engine_output = undefined;
    _ = &output;
    var input: pwr_si_engine_input = nominal_input();
    _ = &input;
    pwr_si_engine_config_default(&config);
    config.displacement_m3 = 0.0;
    while (true) {
        if (!(pwr_si_engine_init(&state, &config) == @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_CONFIG)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 62), "pwr_si_engine_init(&state, &config) == PWR_SI_ENGINE_INVALID_CONFIG" });
            return 0;
        }
        if (!false) break;
    }
    pwr_si_engine_config_default(&config);
    while (true) {
        if (!(pwr_si_engine_init(&state, &config) == @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 64), "pwr_si_engine_init(&state, &config) == PWR_SI_ENGINE_OK" });
            return 0;
        }
        if (!false) break;
    }
    before = state;
    input.driver_throttle = 1.1;
    while (true) {
        if (!(pwr_si_engine_step(&state, &input, &output) == @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_INPUT)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 68), "pwr_si_engine_step(&state, &input, &output) == PWR_SI_ENGINE_INVALID_INPUT" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(memcmp(@as(?*const anyopaque, @ptrCast(&before)), @as(?*const anyopaque, @ptrCast(&state)), @sizeOf(pwr_si_engine_state)) == @as(c_int, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 69), "memcmp(&before, &state, sizeof(state)) == 0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_si_engine_init(null, &config) == @as(c_uint, @bitCast(PWR_SI_ENGINE_INVALID_ARGUMENT)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 71), "pwr_si_engine_init(NULL, &config) == PWR_SI_ENGINE_INVALID_ARGUMENT" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

pub fn test_start_combustion_and_ledgers() callconv(.c) c_int {
    var config: pwr_si_engine_config = undefined;
    _ = &config;
    var state: pwr_si_engine_state = undefined;
    _ = &state;
    var output: pwr_si_engine_output = pwr_si_engine_output{
        .time_s = @as(f64, @floatFromInt(@as(c_int, 0))),
        .crank_angle_rad = 0,
        .crank_speed_rad_s = 0,
        .sensed_crank_speed_rad_s = 0,
        .throttle_position = 0,
        .manifold_pressure_pa = 0,
        .low_voltage_v = 0,
        .combustion_torque_nm = 0,
        .brake_torque_nm = 0,
        .pumping_torque_nm = 0,
        .friction_torque_nm = 0,
        .intake_air_flow_kg_per_s = 0,
        .cylinder_air_flow_kg_per_s = 0,
        .injected_fuel_flow_kg_per_s = 0,
        .burned_fuel_flow_kg_per_s = 0,
        .lambda = 0,
        .exhaust_mass_flow_kg_per_s = 0,
        .exhaust_temperature_k = 0,
        .exhaust_source_pressure_pa = 0,
        .exhaust_species_mass_fraction = @import("std").mem.zeroes([4]f64),
        .coolant_temperature_k = 0,
        .fuel_energy_j = 0,
        .mechanical_load_work_j = 0,
        .exhaust_heat_j = 0,
        .ambient_heat_j = 0,
        .stored_energy_change_j = 0,
        .unburned_fuel_energy_j = 0,
        .numerical_adjustment_j = 0,
        .energy_residual_j = 0,
        .air_mass_residual_kg = 0,
        .crank_sensor_age_ticks = @import("std").mem.zeroes(u64),
        .diagnostic_flags = @import("std").mem.zeroes(u32),
        .crank_signal_valid = false,
        .ecu_active = false,
        .combustion_active = false,
        .state_hash = @import("std").mem.zeroes(u64),
    };
    _ = &output;
    var species_sum: f64 = 0.0;
    _ = &species_sum;
    pwr_si_engine_config_default(&config);
    while (true) {
        if (!(pwr_si_engine_init(&state, &config) == @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 83), "pwr_si_engine_init(&state, &config) == PWR_SI_ENGINE_OK" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(run_started_engine(&state, &output, 103000.0) != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 84), "run_started_engine(&state, &output, 103000.0)" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.crank_speed_rad_s > 80.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 86), "output.crank_speed_rad_s > 80.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!output.combustion_active) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 87), "output.combustion_active" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!output.ecu_active) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 88), "output.ecu_active" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!output.crank_signal_valid) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 89), "output.crank_signal_valid" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.injected_fuel_flow_kg_per_s > 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 90), "output.injected_fuel_flow_kg_per_s > 0.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.exhaust_mass_flow_kg_per_s > 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 91), "output.exhaust_mass_flow_kg_per_s > 0.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.exhaust_temperature_k >= config.exhaust_temperature_floor_k)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 92), "output.exhaust_temperature_k >= config.exhaust_temperature_floor_k" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.coolant_temperature_k > config.coolant_initial_temperature_k)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 93), "output.coolant_temperature_k > config.coolant_initial_temperature_k" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.fuel_energy_j > 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 94), "output.fuel_energy_j > 0.0" });
            return 0;
        }
        if (!false) break;
    }
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_SI_ENGINE_SPECIES_COUNT)))) : (species +%= 1) {
            while (true) {
                if (!(output.exhaust_species_mass_fraction[species] >= 0.0)) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 97), "output.exhaust_species_mass_fraction[species] >= 0.0" });
                    return 0;
                }
                if (!false) break;
            }
            species_sum += output.exhaust_species_mass_fraction[species];
        }
    }
    while (true) {
        if (!(fabs(species_sum - 1.0) < 0.000000000001)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 100), "fabs(species_sum - 1.0) < 1.0e-12" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(output.air_mass_residual_kg) < 0.000000000001)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 101), "fabs(output.air_mass_residual_kg) < 1.0e-12" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!((fabs(output.energy_residual_j) / ((output.fuel_energy_j + output.stored_energy_change_j) + 1.0)) < 0.0000000001)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 104), "fabs(output.energy_residual_j) / (output.fuel_energy_j + output.stored_energy_change_j + 1.0) < 1.0e-10" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

pub fn test_determinism() callconv(.c) c_int {
    var config: pwr_si_engine_config = undefined;
    _ = &config;
    var first: pwr_si_engine_state = undefined;
    _ = &first;
    var second: pwr_si_engine_state = undefined;
    _ = &second;
    var first_output: pwr_si_engine_output = pwr_si_engine_output{
        .time_s = @as(f64, @floatFromInt(@as(c_int, 0))),
        .crank_angle_rad = 0,
        .crank_speed_rad_s = 0,
        .sensed_crank_speed_rad_s = 0,
        .throttle_position = 0,
        .manifold_pressure_pa = 0,
        .low_voltage_v = 0,
        .combustion_torque_nm = 0,
        .brake_torque_nm = 0,
        .pumping_torque_nm = 0,
        .friction_torque_nm = 0,
        .intake_air_flow_kg_per_s = 0,
        .cylinder_air_flow_kg_per_s = 0,
        .injected_fuel_flow_kg_per_s = 0,
        .burned_fuel_flow_kg_per_s = 0,
        .lambda = 0,
        .exhaust_mass_flow_kg_per_s = 0,
        .exhaust_temperature_k = 0,
        .exhaust_source_pressure_pa = 0,
        .exhaust_species_mass_fraction = @import("std").mem.zeroes([4]f64),
        .coolant_temperature_k = 0,
        .fuel_energy_j = 0,
        .mechanical_load_work_j = 0,
        .exhaust_heat_j = 0,
        .ambient_heat_j = 0,
        .stored_energy_change_j = 0,
        .unburned_fuel_energy_j = 0,
        .numerical_adjustment_j = 0,
        .energy_residual_j = 0,
        .air_mass_residual_kg = 0,
        .crank_sensor_age_ticks = @import("std").mem.zeroes(u64),
        .diagnostic_flags = @import("std").mem.zeroes(u32),
        .crank_signal_valid = false,
        .ecu_active = false,
        .combustion_active = false,
        .state_hash = @import("std").mem.zeroes(u64),
    };
    _ = &first_output;
    var second_output: pwr_si_engine_output = pwr_si_engine_output{
        .time_s = @as(f64, @floatFromInt(@as(c_int, 0))),
        .crank_angle_rad = 0,
        .crank_speed_rad_s = 0,
        .sensed_crank_speed_rad_s = 0,
        .throttle_position = 0,
        .manifold_pressure_pa = 0,
        .low_voltage_v = 0,
        .combustion_torque_nm = 0,
        .brake_torque_nm = 0,
        .pumping_torque_nm = 0,
        .friction_torque_nm = 0,
        .intake_air_flow_kg_per_s = 0,
        .cylinder_air_flow_kg_per_s = 0,
        .injected_fuel_flow_kg_per_s = 0,
        .burned_fuel_flow_kg_per_s = 0,
        .lambda = 0,
        .exhaust_mass_flow_kg_per_s = 0,
        .exhaust_temperature_k = 0,
        .exhaust_source_pressure_pa = 0,
        .exhaust_species_mass_fraction = @import("std").mem.zeroes([4]f64),
        .coolant_temperature_k = 0,
        .fuel_energy_j = 0,
        .mechanical_load_work_j = 0,
        .exhaust_heat_j = 0,
        .ambient_heat_j = 0,
        .stored_energy_change_j = 0,
        .unburned_fuel_energy_j = 0,
        .numerical_adjustment_j = 0,
        .energy_residual_j = 0,
        .air_mass_residual_kg = 0,
        .crank_sensor_age_ticks = @import("std").mem.zeroes(u64),
        .diagnostic_flags = @import("std").mem.zeroes(u32),
        .crank_signal_valid = false,
        .ecu_active = false,
        .combustion_active = false,
        .state_hash = @import("std").mem.zeroes(u64),
    };
    _ = &second_output;
    var input: pwr_si_engine_input = nominal_input();
    _ = &input;
    pwr_si_engine_config_default(&config);
    while (true) {
        if (!(pwr_si_engine_init(&first, &config) == @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 118), "pwr_si_engine_init(&first, &config) == PWR_SI_ENGINE_OK" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_si_engine_init(&second, &config) == @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 119), "pwr_si_engine_init(&second, &config) == PWR_SI_ENGINE_OK" });
            return 0;
        }
        if (!false) break;
    }
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, 25000)) : (step +%= 1) {
            input.starter_torque_nm = if (step < @as(u64, 8000)) 55.0 else 0.0;
            while (true) {
                if (!(pwr_si_engine_step(&first, &input, &first_output) == @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 123), "pwr_si_engine_step(&first, &input, &first_output) == PWR_SI_ENGINE_OK" });
                    return 0;
                }
                if (!false) break;
            }
            while (true) {
                if (!(pwr_si_engine_step(&second, &input, &second_output) == @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 125), "pwr_si_engine_step(&second, &input, &second_output) == PWR_SI_ENGINE_OK" });
                    return 0;
                }
                if (!false) break;
            }
            while (true) {
                if (!(first_output.state_hash == second_output.state_hash)) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 126), "first_output.state_hash == second_output.state_hash" });
                    return 0;
                }
                if (!false) break;
            }
        }
    }
    while (true) {
        if (!(memcmp(@as(?*const anyopaque, @ptrCast(&first)), @as(?*const anyopaque, @ptrCast(&second)), @sizeOf(pwr_si_engine_state)) == @as(c_int, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 128), "memcmp(&first, &second, sizeof(first)) == 0" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

// Compare pressure changes from the same running engine state. Starting a
// heavily loaded engine directly against the restriction can stall it and
// confound the pumping comparison with a different intake manifold pressure.
pub fn test_backpressure_reduces_speed() callconv(.c) c_int {
    const std = @import("std");
    var config: pwr_si_engine_config = undefined;
    pwr_si_engine_config_default(&config);
    var open_state: pwr_si_engine_state = undefined;
    if (pwr_si_engine_init(&open_state, &config) != PWR_SI_ENGINE_OK) return 0;
    var open_output: pwr_si_engine_output = undefined;
    if (run_started_engine(&open_state, &open_output, 103000) == 0) return 0;
    var restricted_state = open_state;
    var restricted_output: pwr_si_engine_output = undefined;
    const open_input = nominal_input();
    var restricted_input = open_input;
    restricted_input.exhaust_backpressure_pa = 185000;
    for (0..10000) |tick| {
        if (pwr_si_engine_step(&open_state, &open_input, &open_output) != PWR_SI_ENGINE_OK) return 0;
        if (pwr_si_engine_step(&restricted_state, &restricted_input, &restricted_output) != PWR_SI_ENGINE_OK) return 0;
        if (tick == 0) {
            const expected = config.pumping_scale * (185000 - 103000) * config.displacement_m3 / (4 * std.math.pi);
            std.testing.expectApproxEqAbs(expected, restricted_output.pumping_torque_nm - open_output.pumping_torque_nm, 1e-10) catch return 0;
        }
    }
    if (!(restricted_output.crank_speed_rad_s + 20 < open_output.crank_speed_rad_s)) return 0;
    return 1;
}

pub fn test_electronic_faults_cut_fuel() callconv(.c) c_int {
    var config: pwr_si_engine_config = undefined;
    _ = &config;
    var state: pwr_si_engine_state = undefined;
    _ = &state;
    var output: pwr_si_engine_output = pwr_si_engine_output{
        .time_s = @as(f64, @floatFromInt(@as(c_int, 0))),
        .crank_angle_rad = 0,
        .crank_speed_rad_s = 0,
        .sensed_crank_speed_rad_s = 0,
        .throttle_position = 0,
        .manifold_pressure_pa = 0,
        .low_voltage_v = 0,
        .combustion_torque_nm = 0,
        .brake_torque_nm = 0,
        .pumping_torque_nm = 0,
        .friction_torque_nm = 0,
        .intake_air_flow_kg_per_s = 0,
        .cylinder_air_flow_kg_per_s = 0,
        .injected_fuel_flow_kg_per_s = 0,
        .burned_fuel_flow_kg_per_s = 0,
        .lambda = 0,
        .exhaust_mass_flow_kg_per_s = 0,
        .exhaust_temperature_k = 0,
        .exhaust_source_pressure_pa = 0,
        .exhaust_species_mass_fraction = @import("std").mem.zeroes([4]f64),
        .coolant_temperature_k = 0,
        .fuel_energy_j = 0,
        .mechanical_load_work_j = 0,
        .exhaust_heat_j = 0,
        .ambient_heat_j = 0,
        .stored_energy_change_j = 0,
        .unburned_fuel_energy_j = 0,
        .numerical_adjustment_j = 0,
        .energy_residual_j = 0,
        .air_mass_residual_kg = 0,
        .crank_sensor_age_ticks = @import("std").mem.zeroes(u64),
        .diagnostic_flags = @import("std").mem.zeroes(u32),
        .crank_signal_valid = false,
        .ecu_active = false,
        .combustion_active = false,
        .state_hash = @import("std").mem.zeroes(u64),
    };
    _ = &output;
    var input: pwr_si_engine_input = nominal_input();
    _ = &input;
    pwr_si_engine_config_default(&config);
    while (true) {
        if (!(pwr_si_engine_init(&state, &config) == @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 161), "pwr_si_engine_init(&state, &config) == PWR_SI_ENGINE_OK" });
            return 0;
        }
        if (!false) break;
    }
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, 15000)) : (step +%= 1) {
            input.starter_torque_nm = if (step < @as(u64, 8000)) 55.0 else 0.0;
            while (true) {
                if (!(pwr_si_engine_step(&state, &input, &output) == @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 165), "pwr_si_engine_step(&state, &input, &output) == PWR_SI_ENGINE_OK" });
                    return 0;
                }
                if (!false) break;
            }
        }
    }
    while (true) {
        if (!output.combustion_active) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 167), "output.combustion_active" });
            return 0;
        }
        if (!false) break;
    }
    input.starter_torque_nm = 0.0;
    input.fault_mask = @as(u32, @bitCast(PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT));
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, 200)) : (step +%= 1) {
            while (true) {
                if (!(pwr_si_engine_step(&state, &input, &output) == @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 173), "pwr_si_engine_step(&state, &input, &output) == PWR_SI_ENGINE_OK" });
                    return 0;
                }
                if (!false) break;
            }
        }
    }
    while (true) {
        if (!!output.combustion_active) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 175), "!output.combustion_active" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!!output.crank_signal_valid) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 176), "!output.crank_signal_valid" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!((output.diagnostic_flags & @as(u32, @bitCast(PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_INVALID))) != @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 179), "(output.diagnostic_flags & PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_INVALID) != 0U" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!((output.diagnostic_flags & @as(u32, @bitCast(PWR_SI_ENGINE_DIAG_FUEL_CUT))) != @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 180), "(output.diagnostic_flags & PWR_SI_ENGINE_DIAG_FUEL_CUT) != 0U" });
            return 0;
        }
        if (!false) break;
    }
    input.fault_mask = @as(u32, @bitCast(PWR_SI_ENGINE_FAULT_LOW_VOLTAGE));
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, 20)) : (step +%= 1) {
            while (true) {
                if (!(pwr_si_engine_step(&state, &input, &output) == @as(c_uint, @bitCast(PWR_SI_ENGINE_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 185), "pwr_si_engine_step(&state, &input, &output) == PWR_SI_ENGINE_OK" });
                    return 0;
                }
                if (!false) break;
            }
        }
    }
    while (true) {
        if (!!output.ecu_active) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 187), "!output.ecu_active" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.low_voltage_v == 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 188), "output.low_voltage_v == 0.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!((output.diagnostic_flags & @as(u32, @bitCast(PWR_SI_ENGINE_DIAG_ECU_BROWNOUT))) != @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 190), "(output.diagnostic_flags & PWR_SI_ENGINE_DIAG_ECU_BROWNOUT) != 0U" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

pub fn run() c_int {
    while (true) {
        if (!(test_invalid_configuration_and_transaction() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 196), "test_invalid_configuration_and_transaction()" });
            return 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(test_start_combustion_and_ledgers() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 197), "test_start_combustion_and_ledgers()" });
            return 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(test_determinism() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 198), "test_determinism()" });
            return 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(test_backpressure_reduces_speed() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 199), "test_backpressure_reduces_speed()" });
            return 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(test_electronic_faults_cut_fuel() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_si_engine.c", @as(c_int, 200), "test_electronic_faults_cut_fuel()" });
            return 1;
        }
        if (!false) break;
    }
    return 0;
}

pub fn main() void {
    @import("std").process.exit(@intCast(run()));
}

test "engine speed limiter cuts fuel after a successful start" {
    const std = @import("std");
    const abi = @import("../src/abi.zig");
    var config: pwr_si_engine_config = undefined;
    pwr_si_engine_config_default(&config);
    var state: pwr_si_engine_state = undefined;
    try std.testing.expect(pwr_si_engine_init(&state, &config) == PWR_SI_ENGINE_OK);
    var output: pwr_si_engine_output = undefined;
    var commands = nominal_input();
    commands.load_torque_nm = 22;
    var saw_combustion = false;
    var saw_limiter_cut = false;
    for (0..50000) |tick| {
        commands.starter_torque_nm = if (tick < 8000) 55 else 0;
        try std.testing.expect(pwr_si_engine_step(&state, &commands, &output) == PWR_SI_ENGINE_OK);
        saw_combustion = saw_combustion or output.combustion_active;
        saw_limiter_cut = saw_limiter_cut or (saw_combustion and !output.combustion_active and
            output.ecu_active and output.crank_signal_valid and
            state.sensed_crank_speed_rad_s >= config.maximum_speed_rad_s);
    }
    try std.testing.expect(saw_combustion and saw_limiter_cut);
    try std.testing.expect(output.diagnostic_flags & abi.PWR_SI_ENGINE_DIAG_SPEED_LIMIT != 0);
    try std.testing.expect(output.fuel_energy_j > 0);
}
