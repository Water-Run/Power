// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/tests/test_dct.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_DCT_CLUTCH_INVALID = @import("../src/abi.zig").PWR_DCT_CLUTCH_INVALID;
const PWR_DCT_CLUTCH_K1 = @import("../src/abi.zig").PWR_DCT_CLUTCH_K1;
const PWR_DCT_CLUTCH_K2 = @import("../src/abi.zig").PWR_DCT_CLUTCH_K2;
const PWR_DCT_DIAG_DOG_SHIFT_WHILE_CLAMPED = @import("../src/abi.zig").PWR_DCT_DIAG_DOG_SHIFT_WHILE_CLAMPED;
const PWR_DCT_DIAG_ENERGY_RESIDUAL = @import("../src/abi.zig").PWR_DCT_DIAG_ENERGY_RESIDUAL;
const PWR_DCT_DIAG_INPUT_REJECTED = @import("../src/abi.zig").PWR_DCT_DIAG_INPUT_REJECTED;
const PWR_DCT_DIAG_INVALID_GEAR_BIT = @import("../src/abi.zig").PWR_DCT_DIAG_INVALID_GEAR_BIT;
const PWR_DCT_DIAG_K1_OVERHEAT = @import("../src/abi.zig").PWR_DCT_DIAG_K1_OVERHEAT;
const PWR_DCT_DIAG_K1_THERMAL_FADE = @import("../src/abi.zig").PWR_DCT_DIAG_K1_THERMAL_FADE;
const PWR_DCT_DIAG_MULTIPLE_GEARS_ON_SHAFT = @import("../src/abi.zig").PWR_DCT_DIAG_MULTIPLE_GEARS_ON_SHAFT;
const PWR_DCT_GEAR_1 = @import("../src/abi.zig").PWR_DCT_GEAR_1;
const PWR_DCT_GEAR_2 = @import("../src/abi.zig").PWR_DCT_GEAR_2;
const PWR_DCT_GEAR_3 = @import("../src/abi.zig").PWR_DCT_GEAR_3;
const PWR_DCT_GEAR_4 = @import("../src/abi.zig").PWR_DCT_GEAR_4;
const PWR_DCT_GEAR_5 = @import("../src/abi.zig").PWR_DCT_GEAR_5;
const PWR_DCT_GEAR_6 = @import("../src/abi.zig").PWR_DCT_GEAR_6;
const PWR_DCT_GEAR_7 = @import("../src/abi.zig").PWR_DCT_GEAR_7;
const PWR_DCT_GEAR_NEUTRAL = @import("../src/abi.zig").PWR_DCT_GEAR_NEUTRAL;
const PWR_DCT_GEAR_REVERSE = @import("../src/abi.zig").PWR_DCT_GEAR_REVERSE;
const PWR_DCT_INVALID_CONFIG = @import("../src/abi.zig").PWR_DCT_INVALID_CONFIG;
const PWR_DCT_INVALID_GEAR_COMMAND = @import("../src/abi.zig").PWR_DCT_INVALID_GEAR_COMMAND;
const PWR_DCT_OK = @import("../src/abi.zig").PWR_DCT_OK;
const cos = @import("../src/support.zig").cos;
const fabs = @import("../src/support.zig").fabs;
const fmax = @import("../src/support.zig").fmax;
const fmin = @import("../src/support.zig").fmin;
const power_assert = @import("../src/support.zig").power_assert;
const pwr_dct = @import("../src/abi.zig").pwr_dct;
const pwr_dct_clutch_for_gear = @import("../src/models/drivetrain/pwr_dct.zig").pwr_dct_clutch_for_gear;
const pwr_dct_clutch_observation = @import("../src/abi.zig").pwr_dct_clutch_observation;
const pwr_dct_config = @import("../src/abi.zig").pwr_dct_config;
const pwr_dct_config_default = @import("../src/models/drivetrain/pwr_dct.zig").pwr_dct_config_default;
const pwr_dct_gear_mask = @import("../src/abi.zig").pwr_dct_gear_mask;
const pwr_dct_init = @import("../src/models/drivetrain/pwr_dct.zig").pwr_dct_init;
const pwr_dct_inputs = @import("../src/abi.zig").pwr_dct_inputs;
const pwr_dct_snapshot = @import("../src/abi.zig").pwr_dct_snapshot;
const pwr_dct_snapshot_read = @import("../src/models/drivetrain/pwr_dct.zig").pwr_dct_snapshot_read;
const pwr_dct_state_hash = @import("../src/models/drivetrain/pwr_dct.zig").pwr_dct_state_hash;
const pwr_dct_step = @import("../src/models/drivetrain/pwr_dct.zig").pwr_dct_step;
const pwr_dct_validate_config = @import("../src/models/drivetrain/pwr_dct.zig").pwr_dct_validate_config;
const sin = @import("../src/support.zig").sin;

pub fn test_config() callconv(.c) pwr_dct_config {
    var config: pwr_dct_config = undefined;
    _ = &config;
    pwr_dct_config_default(&config);
    config.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_REVERSE)))] = -3.2;
    config.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_1)))] = 3.4;
    config.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_2)))] = 2.3;
    config.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_3)))] = 1.7;
    config.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_4)))] = 1.3;
    config.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_5)))] = 1.0;
    config.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_6)))] = 0.78;
    config.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_7)))] = 0.62;
    config.final_drive_ratio = 1.0;
    config.clamp_engagement_rate_per_s[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 100.0;
    config.clamp_engagement_rate_per_s[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] = 100.0;
    config.clamp_release_rate_per_s[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 100.0;
    config.clamp_release_rate_per_s[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] = 100.0;
    return config;
}

pub fn test_inputs(arg_gear_mask: pwr_dct_gear_mask) callconv(.c) pwr_dct_inputs {
    var gear_mask = arg_gear_mask;
    _ = &gear_mask;
    return pwr_dct_inputs{
        .engine_speed_rad_s = 220.0,
        .output_speed_rad_s = 30.0,
        .engaged_gear_mask = gear_mask,
        .clutch_clamp_command = [2]f64{
            0.0,
            0.0,
        },
        .ambient_temperature_k = 298.15,
    };
}

pub fn test_odd_even_topology_and_preselection() callconv(.c) void {
    power_assert(@intFromBool(!!(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_NEUTRAL) == PWR_DCT_CLUTCH_INVALID)), "pwr_dct_clutch_for_gear(PWR_DCT_GEAR_NEUTRAL) == PWR_DCT_CLUTCH_INVALID", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 43))));
    power_assert(@intFromBool(!!(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_1) == PWR_DCT_CLUTCH_K1)), "pwr_dct_clutch_for_gear(PWR_DCT_GEAR_1) == PWR_DCT_CLUTCH_K1", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 44))));
    power_assert(@intFromBool(!!(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_3) == PWR_DCT_CLUTCH_K1)), "pwr_dct_clutch_for_gear(PWR_DCT_GEAR_3) == PWR_DCT_CLUTCH_K1", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 45))));
    power_assert(@intFromBool(!!(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_5) == PWR_DCT_CLUTCH_K1)), "pwr_dct_clutch_for_gear(PWR_DCT_GEAR_5) == PWR_DCT_CLUTCH_K1", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 46))));
    power_assert(@intFromBool(!!(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_7) == PWR_DCT_CLUTCH_K1)), "pwr_dct_clutch_for_gear(PWR_DCT_GEAR_7) == PWR_DCT_CLUTCH_K1", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 47))));
    power_assert(@intFromBool(!!(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_REVERSE) == PWR_DCT_CLUTCH_K2)), "pwr_dct_clutch_for_gear(PWR_DCT_GEAR_REVERSE) == PWR_DCT_CLUTCH_K2", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 48))));
    power_assert(@intFromBool(!!(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_2) == PWR_DCT_CLUTCH_K2)), "pwr_dct_clutch_for_gear(PWR_DCT_GEAR_2) == PWR_DCT_CLUTCH_K2", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 49))));
    power_assert(@intFromBool(!!(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_4) == PWR_DCT_CLUTCH_K2)), "pwr_dct_clutch_for_gear(PWR_DCT_GEAR_4) == PWR_DCT_CLUTCH_K2", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 50))));
    power_assert(@intFromBool(!!(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_6) == PWR_DCT_CLUTCH_K2)), "pwr_dct_clutch_for_gear(PWR_DCT_GEAR_6) == PWR_DCT_CLUTCH_K2", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 51))));
    const config: pwr_dct_config = test_config();
    _ = &config;
    var dct: pwr_dct = undefined;
    _ = &dct;
    power_assert(@intFromBool(!!(pwr_dct_init(&dct, &config) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_init(&dct, &config) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 55))));
    var inputs: pwr_dct_inputs = test_inputs(@as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2))))));
    _ = &inputs;
    inputs.clutch_clamp_command[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 1.0;
    power_assert(@intFromBool(!!(pwr_dct_step(&dct, &inputs, 0.01) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 59))));
    var snapshot: pwr_dct_snapshot = undefined;
    _ = &snapshot;
    power_assert(@intFromBool(!!(pwr_dct_snapshot_read(&dct, &snapshot) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 62))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))].selected_gear == PWR_DCT_GEAR_1)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].selected_gear == PWR_DCT_GEAR_1", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 63))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))].selected_gear == PWR_DCT_GEAR_NEUTRAL)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].selected_gear == PWR_DCT_GEAR_NEUTRAL", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 65))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))].transmitted_torque_nm > 0.0)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].transmitted_torque_nm > 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 66))));
    power_assert(@intFromBool(!!(snapshot.output_torque_nm > 0.0)), "snapshot.output_torque_nm > 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 67))));
    inputs.engaged_gear_mask = @as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2) | @as(c_int, 4)))));
    power_assert(@intFromBool(!!(pwr_dct_step(&dct, &inputs, 0.01) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 71))));
    power_assert(@intFromBool(!!(pwr_dct_snapshot_read(&dct, &snapshot) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 72))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))].selected_gear == PWR_DCT_GEAR_2)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].selected_gear == PWR_DCT_GEAR_2", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 73))));
    power_assert(@intFromBool(!!snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))].preselected), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].preselected", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 74))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))].transmitted_torque_nm == 0.0)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].transmitted_torque_nm == 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 75))));
    var reverse_dct: pwr_dct = undefined;
    _ = &reverse_dct;
    power_assert(@intFromBool(!!(pwr_dct_init(&reverse_dct, &config) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_init(&reverse_dct, &config) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 79))));
    inputs = test_inputs(@as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 1))))));
    inputs.output_speed_rad_s = -20.0;
    inputs.clutch_clamp_command[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] = 1.0;
    power_assert(@intFromBool(!!(pwr_dct_step(&reverse_dct, &inputs, 0.01) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_step(&reverse_dct, &inputs, 0.01) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 83))));
    power_assert(@intFromBool(!!(pwr_dct_snapshot_read(&reverse_dct, &snapshot) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_snapshot_read(&reverse_dct, &snapshot) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 84))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))].input_shaft_speed_rad_s > 0.0)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].input_shaft_speed_rad_s > 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 85))));
    power_assert(@intFromBool(!!(snapshot.output_torque_nm < 0.0)), "snapshot.output_torque_nm < 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 86))));
    power_assert(@intFromBool(!!(snapshot.output_power_w > 0.0)), "snapshot.output_power_w > 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 87))));
}

pub fn test_overlap_shift_torque_continuity() callconv(.c) void {
    var config: pwr_dct_config = test_config();
    _ = &config;
    config.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_1)))] = 2.0;
    config.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_2)))] = 1.9;
    config.clamp_engagement_rate_per_s[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 20.0;
    config.clamp_engagement_rate_per_s[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] = 20.0;
    config.clamp_release_rate_per_s[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 20.0;
    config.clamp_release_rate_per_s[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] = 20.0;
    var dct: pwr_dct = undefined;
    _ = &dct;
    power_assert(@intFromBool(!!(pwr_dct_init(&dct, &config) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_init(&dct, &config) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 100))));
    var inputs: pwr_dct_inputs = test_inputs(@as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2) | @as(c_int, 4))))));
    _ = &inputs;
    inputs.engine_speed_rad_s = 250.0;
    inputs.output_speed_rad_s = 30.0;
    inputs.clutch_clamp_command[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 1.0;
    {
        var step: u32 = 0;
        _ = &step;
        while (step < @as(c_uint, 10)) : (step +%= 1) {
            power_assert(@intFromBool(!!(pwr_dct_step(&dct, &inputs, 0.01) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 108))));
        }
    }
    var snapshot: pwr_dct_snapshot = undefined;
    _ = &snapshot;
    power_assert(@intFromBool(!!(pwr_dct_snapshot_read(&dct, &snapshot) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 111))));
    const initial_output_torque_nm: f64 = snapshot.output_torque_nm;
    _ = &initial_output_torque_nm;
    power_assert(@intFromBool(!!(initial_output_torque_nm > 0.0)), "initial_output_torque_nm > 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 113))));
    var minimum_output_torque_nm: f64 = initial_output_torque_nm;
    _ = &minimum_output_torque_nm;
    var previous_output_torque_nm: f64 = initial_output_torque_nm;
    _ = &previous_output_torque_nm;
    var maximum_adjacent_change_nm: f64 = 0.0;
    _ = &maximum_adjacent_change_nm;
    {
        var step: u32 = 1;
        _ = &step;
        while (step <= @as(c_uint, 20)) : (step +%= 1) {
            const progress: f64 = @as(f64, @floatFromInt(step)) / 20.0;
            _ = &progress;
            inputs.clutch_clamp_command[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 1.0 - progress;
            inputs.clutch_clamp_command[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] = progress;
            power_assert(@intFromBool(!!(pwr_dct_step(&dct, &inputs, 0.01) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 122))));
            power_assert(@intFromBool(!!(pwr_dct_snapshot_read(&dct, &snapshot) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 123))));
            minimum_output_torque_nm = fmin(minimum_output_torque_nm, snapshot.output_torque_nm);
            maximum_adjacent_change_nm = fmax(maximum_adjacent_change_nm, fabs(snapshot.output_torque_nm - previous_output_torque_nm));
            previous_output_torque_nm = snapshot.output_torque_nm;
        }
    }
    power_assert(@intFromBool(!!(minimum_output_torque_nm > (0.9 * initial_output_torque_nm))), "minimum_output_torque_nm > 0.90 * initial_output_torque_nm", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 131))));
    power_assert(@intFromBool(!!(maximum_adjacent_change_nm < (0.02 * initial_output_torque_nm))), "maximum_adjacent_change_nm < 0.02 * initial_output_torque_nm", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 132))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))].applied_clamp < 0.000000000001)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].applied_clamp < 1.0e-12", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 133))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))].applied_clamp > 0.999)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].applied_clamp > 0.999", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 134))));
}

pub fn test_slip_heating_fade_and_energy_balance() callconv(.c) void {
    var config: pwr_dct_config = test_config();
    _ = &config;
    config.clutch_heat_capacity_j_per_k[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 200.0;
    config.clutch_ambient_conductance_w_per_k[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 0.0;
    config.initial_clutch_temperature_k[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 295.0;
    config.fade_start_temperature_k[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 300.0;
    config.fade_full_temperature_k[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 340.0;
    config.overheat_temperature_k[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 325.0;
    config.minimum_fade_capacity_fraction[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 0.25;
    var dct: pwr_dct = undefined;
    _ = &dct;
    power_assert(@intFromBool(!!(pwr_dct_init(&dct, &config) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_init(&dct, &config) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 148))));
    var inputs: pwr_dct_inputs = test_inputs(@as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2))))));
    _ = &inputs;
    inputs.engine_speed_rad_s = 300.0;
    inputs.output_speed_rad_s = 0.0;
    inputs.clutch_clamp_command[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 1.0;
    inputs.ambient_temperature_k = 295.0;
    {
        var step: u32 = 0;
        _ = &step;
        while (step < @as(c_uint, 20)) : (step +%= 1) {
            power_assert(@intFromBool(!!(pwr_dct_step(&dct, &inputs, 0.01) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 155))));
        }
    }
    var snapshot: pwr_dct_snapshot = undefined;
    _ = &snapshot;
    power_assert(@intFromBool(!!(pwr_dct_snapshot_read(&dct, &snapshot) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 159))));
    var k1: [*c]const pwr_dct_clutch_observation = &snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))];
    _ = &k1;
    power_assert(@intFromBool(!!(k1.*.cumulative_friction_work_j > 0.0)), "k1->cumulative_friction_work_j > 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 162))));
    power_assert(@intFromBool(!!(k1.*.temperature_k > config.fade_start_temperature_k[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))])), "k1->temperature_k > config.fade_start_temperature_k[(size_t)PWR_DCT_CLUTCH_K1]", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 163))));
    power_assert(@intFromBool(!!(k1.*.thermal_capacity_factor < 1.0)), "k1->thermal_capacity_factor < 1.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 164))));
    power_assert(@intFromBool(!!(k1.*.thermal_capacity_factor >= config.minimum_fade_capacity_fraction[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))])), "k1->thermal_capacity_factor >= config.minimum_fade_capacity_fraction[(size_t)PWR_DCT_CLUTCH_K1]", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 166))));
    power_assert(@intFromBool(!!((snapshot.diagnostic_flags & @as(u32, @bitCast(PWR_DCT_DIAG_K1_THERMAL_FADE))) != @as(c_uint, 0))), "(snapshot.diagnostic_flags & PWR_DCT_DIAG_K1_THERMAL_FADE) != 0U", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 167))));
    power_assert(@intFromBool(!!((snapshot.diagnostic_flags & @as(u32, @bitCast(PWR_DCT_DIAG_K1_OVERHEAT))) != @as(c_uint, 0))), "(snapshot.diagnostic_flags & PWR_DCT_DIAG_K1_OVERHEAT) != 0U", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 168))));
    const energy_scale: f64 = fabs(snapshot.cumulative_input_energy_j) + 1.0;
    _ = &energy_scale;
    power_assert(@intFromBool(!!((fabs(snapshot.energy_residual_j) / energy_scale) < 0.0000000001)), "fabs(snapshot.energy_residual_j) / energy_scale < 1.0e-10", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 171))));
    power_assert(@intFromBool(!!((snapshot.diagnostic_flags & @as(u32, @bitCast(PWR_DCT_DIAG_ENERGY_RESIDUAL))) == @as(c_uint, 0))), "(snapshot.diagnostic_flags & PWR_DCT_DIAG_ENERGY_RESIDUAL) == 0U", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 172))));
}

pub fn test_invalid_configuration_and_shift_commands() callconv(.c) void {
    var config: pwr_dct_config = undefined;
    _ = &config;
    pwr_dct_config_default(&config);
    power_assert(@intFromBool(!!(pwr_dct_validate_config(&config) == @as(c_uint, @bitCast(PWR_DCT_INVALID_CONFIG)))), "pwr_dct_validate_config(&config) == PWR_DCT_INVALID_CONFIG", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 178))));
    config = test_config();
    var invalid: pwr_dct_config = config;
    _ = &invalid;
    invalid.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_REVERSE)))] = 3.0;
    power_assert(@intFromBool(!!(pwr_dct_validate_config(&invalid) == @as(c_uint, @bitCast(PWR_DCT_INVALID_CONFIG)))), "pwr_dct_validate_config(&invalid) == PWR_DCT_INVALID_CONFIG", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 183))));
    invalid = config;
    invalid.mesh_efficiency[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_4)))] = 1.01;
    power_assert(@intFromBool(!!(pwr_dct_validate_config(&invalid) == @as(c_uint, @bitCast(PWR_DCT_INVALID_CONFIG)))), "pwr_dct_validate_config(&invalid) == PWR_DCT_INVALID_CONFIG", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 186))));
    invalid = config;
    invalid.clutch_heat_capacity_j_per_k[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] = 0.0;
    power_assert(@intFromBool(!!(pwr_dct_validate_config(&invalid) == @as(c_uint, @bitCast(PWR_DCT_INVALID_CONFIG)))), "pwr_dct_validate_config(&invalid) == PWR_DCT_INVALID_CONFIG", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 189))));
    var dct: pwr_dct = undefined;
    _ = &dct;
    power_assert(@intFromBool(!!(pwr_dct_init(&dct, &config) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_init(&dct, &config) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 192))));
    var inputs: pwr_dct_inputs = test_inputs(@as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2) | @as(c_int, 8))))));
    _ = &inputs;
    const initial_k1_temperature_k: f64 = dct.clutch_temperature_k[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))];
    _ = &initial_k1_temperature_k;
    power_assert(@intFromBool(!!(pwr_dct_step(&dct, &inputs, 0.01) == @as(c_uint, @bitCast(PWR_DCT_INVALID_GEAR_COMMAND)))), "pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_INVALID_GEAR_COMMAND", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 196))));
    var snapshot: pwr_dct_snapshot = undefined;
    _ = &snapshot;
    power_assert(@intFromBool(!!(pwr_dct_snapshot_read(&dct, &snapshot) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 198))));
    power_assert(@intFromBool(!!(snapshot.time_s == 0.0)), "snapshot.time_s == 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 199))));
    power_assert(@intFromBool(!!(@as(c_int, @bitCast(@as(c_uint, snapshot.engaged_gear_mask))) == @as(c_int, 0))), "snapshot.engaged_gear_mask == UINT16_C(0)", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 200))));
    power_assert(@intFromBool(!!(snapshot.cumulative_input_energy_j == 0.0)), "snapshot.cumulative_input_energy_j == 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 201))));
    power_assert(@intFromBool(!!(snapshot.cumulative_output_energy_j == 0.0)), "snapshot.cumulative_output_energy_j == 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 202))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))].temperature_k == initial_k1_temperature_k)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].temperature_k == initial_k1_temperature_k", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 204))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))].applied_clamp == 0.0)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].applied_clamp == 0.0", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 205))));
    power_assert(@intFromBool(!!(snapshot.rejected_command_count == @as(u64, 1))), "snapshot.rejected_command_count == UINT64_C(1)", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 206))));
    power_assert(@intFromBool(!!((snapshot.last_step_diagnostic_flags & @as(u32, @bitCast(PWR_DCT_DIAG_MULTIPLE_GEARS_ON_SHAFT))) != @as(c_uint, 0))), "(snapshot.last_step_diagnostic_flags & PWR_DCT_DIAG_MULTIPLE_GEARS_ON_SHAFT) != 0U", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 208))));
    power_assert(@intFromBool(!!((snapshot.last_step_diagnostic_flags & @as(u32, @bitCast(PWR_DCT_DIAG_INPUT_REJECTED))) != @as(c_uint, 0))), "(snapshot.last_step_diagnostic_flags & PWR_DCT_DIAG_INPUT_REJECTED) != 0U", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 209))));
    inputs = test_inputs(@as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 1) << @intCast(8))))));
    power_assert(@intFromBool(!!(pwr_dct_step(&dct, &inputs, 0.01) == @as(c_uint, @bitCast(PWR_DCT_INVALID_GEAR_COMMAND)))), "pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_INVALID_GEAR_COMMAND", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 212))));
    power_assert(@intFromBool(!!(pwr_dct_snapshot_read(&dct, &snapshot) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 213))));
    power_assert(@intFromBool(!!((snapshot.last_step_diagnostic_flags & @as(u32, @bitCast(PWR_DCT_DIAG_INVALID_GEAR_BIT))) != @as(c_uint, 0))), "(snapshot.last_step_diagnostic_flags & PWR_DCT_DIAG_INVALID_GEAR_BIT) != 0U", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 214))));
    inputs = test_inputs(@as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2))))));
    inputs.clutch_clamp_command[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 1.0;
    power_assert(@intFromBool(!!(pwr_dct_step(&dct, &inputs, 0.01) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 218))));
    const time_before_rejected_shift_s: f64 = dct.time_s;
    _ = &time_before_rejected_shift_s;
    const energy_before_rejected_shift_j: f64 = dct.cumulative_input_energy_j;
    _ = &energy_before_rejected_shift_j;
    const clamp_before_rejected_shift: f64 = dct.applied_clamp[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))];
    _ = &clamp_before_rejected_shift;
    const temperature_before_rejected_shift_k: f64 = dct.clutch_temperature_k[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))];
    _ = &temperature_before_rejected_shift_k;
    inputs.engaged_gear_mask = 8;
    inputs.clutch_clamp_command[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 0.0;
    power_assert(@intFromBool(!!(pwr_dct_step(&dct, &inputs, 0.01) == @as(c_uint, @bitCast(PWR_DCT_INVALID_GEAR_COMMAND)))), "pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_INVALID_GEAR_COMMAND", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 227))));
    power_assert(@intFromBool(!!(pwr_dct_snapshot_read(&dct, &snapshot) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 228))));
    power_assert(@intFromBool(!!(snapshot.time_s == time_before_rejected_shift_s)), "snapshot.time_s == time_before_rejected_shift_s", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 229))));
    power_assert(@intFromBool(!!(@as(c_int, @bitCast(@as(c_uint, snapshot.engaged_gear_mask))) == @as(c_int, 2))), "snapshot.engaged_gear_mask == PWR_DCT_GEAR_MASK_1", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 230))));
    power_assert(@intFromBool(!!(snapshot.cumulative_input_energy_j == energy_before_rejected_shift_j)), "snapshot.cumulative_input_energy_j == energy_before_rejected_shift_j", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 231))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))].applied_clamp == clamp_before_rejected_shift)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].applied_clamp == clamp_before_rejected_shift", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 233))));
    power_assert(@intFromBool(!!(snapshot.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))].temperature_k == temperature_before_rejected_shift_k)), "snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].temperature_k == temperature_before_rejected_shift_k", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 235))));
    power_assert(@intFromBool(!!((snapshot.last_step_diagnostic_flags & @as(u32, @bitCast(PWR_DCT_DIAG_DOG_SHIFT_WHILE_CLAMPED))) != @as(c_uint, 0))), "(snapshot.last_step_diagnostic_flags & PWR_DCT_DIAG_DOG_SHIFT_WHILE_CLAMPED) != 0U", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 237))));
}

pub fn test_deterministic_replay() callconv(.c) void {
    const config: pwr_dct_config = test_config();
    _ = &config;
    var first: pwr_dct = undefined;
    _ = &first;
    var second: pwr_dct = undefined;
    _ = &second;
    power_assert(@intFromBool(!!(pwr_dct_init(&first, &config) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_init(&first, &config) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 244))));
    power_assert(@intFromBool(!!(pwr_dct_init(&second, &config) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_init(&second, &config) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 245))));
    var inputs: pwr_dct_inputs = test_inputs(@as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 8) | @as(c_int, 16))))));
    _ = &inputs;
    {
        var step: u32 = 0;
        _ = &step;
        while (step < @as(c_uint, 1000)) : (step +%= 1) {
            const phase: f64 = @as(f64, @floatFromInt(step % @as(c_uint, 200))) / 199.0;
            _ = &phase;
            inputs.engine_speed_rad_s = 180.0 + (40.0 * sin(@as(f64, @floatFromInt(step)) * 0.013));
            inputs.output_speed_rad_s = 40.0 + (3.0 * cos(@as(f64, @floatFromInt(step)) * 0.007));
            inputs.clutch_clamp_command[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 1.0 - phase;
            inputs.clutch_clamp_command[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] = phase;
            power_assert(@intFromBool(!!(pwr_dct_step(&first, &inputs, 0.005) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_step(&first, &inputs, 0.005) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 255))));
            power_assert(@intFromBool(!!(pwr_dct_step(&second, &inputs, 0.005) == @as(c_uint, @bitCast(PWR_DCT_OK)))), "pwr_dct_step(&second, &inputs, 0.005) == PWR_DCT_OK", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 256))));
            power_assert(@intFromBool(!!(pwr_dct_state_hash(&first) == pwr_dct_state_hash(&second))), "pwr_dct_state_hash(&first) == pwr_dct_state_hash(&second)", "legacy/native/tests/test_dct.c", @as(c_uint, @bitCast(@as(c_int, 257))));
        }
    }
}

pub fn run() c_int {
    test_odd_even_topology_and_preselection();
    test_overlap_shift_torque_continuity();
    test_slip_heating_fade_and_energy_balance();
    test_invalid_configuration_and_shift_commands();
    test_deterministic_replay();
    return 0;
}

pub fn main() void {
    @import("std").process.exit(@intCast(run()));
}
