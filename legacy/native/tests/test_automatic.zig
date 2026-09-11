// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/tests/test_automatic.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_AUTOMATIC_DIAG_COUPLING_SPEED_PROJECTED = @import("../src/abi.zig").PWR_AUTOMATIC_DIAG_COUPLING_SPEED_PROJECTED;
const PWR_AUTOMATIC_DIAG_LOW_LINE_PRESSURE = @import("../src/abi.zig").PWR_AUTOMATIC_DIAG_LOW_LINE_PRESSURE;
const PWR_AUTOMATIC_DIAG_SHIFT_TIMEOUT = @import("../src/abi.zig").PWR_AUTOMATIC_DIAG_SHIFT_TIMEOUT;
const PWR_AUTOMATIC_GEAR_FIRST = @import("../src/abi.zig").PWR_AUTOMATIC_GEAR_FIRST;
const PWR_AUTOMATIC_GEAR_FOURTH = @import("../src/abi.zig").PWR_AUTOMATIC_GEAR_FOURTH;
const PWR_AUTOMATIC_GEAR_NEUTRAL = @import("../src/abi.zig").PWR_AUTOMATIC_GEAR_NEUTRAL;
const PWR_AUTOMATIC_GEAR_SECOND = @import("../src/abi.zig").PWR_AUTOMATIC_GEAR_SECOND;
const PWR_AUTOMATIC_INVALID_ARGUMENT = @import("../src/abi.zig").PWR_AUTOMATIC_INVALID_ARGUMENT;
const PWR_AUTOMATIC_INVALID_CONFIG = @import("../src/abi.zig").PWR_AUTOMATIC_INVALID_CONFIG;
const PWR_AUTOMATIC_INVALID_INPUT = @import("../src/abi.zig").PWR_AUTOMATIC_INVALID_INPUT;
const PWR_AUTOMATIC_OK = @import("../src/abi.zig").PWR_AUTOMATIC_OK;
const __builtin_nanf = @import("../src/support.zig").__builtin_nanf;
const fabs = @import("../src/support.zig").fabs;
const fmax = @import("../src/support.zig").fmax;
const memcmp = @import("../src/support.zig").memcmp;
const memset = @import("../src/support.zig").memset;
const power_assert = @import("../src/support.zig").power_assert;
const power_isfinite = @import("../src/support.zig").power_isfinite;
const pwr_automatic = @import("../src/abi.zig").pwr_automatic;
const pwr_automatic_config = @import("../src/abi.zig").pwr_automatic_config;
const pwr_automatic_config_default = @import("../src/models/automatic/pwr_automatic.zig").pwr_automatic_config_default;
const pwr_automatic_converter_evaluate = @import("../src/models/automatic/pwr_automatic.zig").pwr_automatic_converter_evaluate;
const pwr_automatic_converter_point = @import("../src/abi.zig").pwr_automatic_converter_point;
const pwr_automatic_coupled_input = @import("../src/abi.zig").pwr_automatic_coupled_input;
const pwr_automatic_coupling_output = @import("../src/abi.zig").pwr_automatic_coupling_output;
const pwr_automatic_gear = @import("../src/abi.zig").pwr_automatic_gear;
const pwr_automatic_gear_ratio = @import("../src/models/automatic/pwr_automatic.zig").pwr_automatic_gear_ratio;
const pwr_automatic_init = @import("../src/models/automatic/pwr_automatic.zig").pwr_automatic_init;
const pwr_automatic_input = @import("../src/abi.zig").pwr_automatic_input;
const pwr_automatic_snapshot = @import("../src/abi.zig").pwr_automatic_snapshot;
const pwr_automatic_snapshot_read = @import("../src/models/automatic/pwr_automatic.zig").pwr_automatic_snapshot_read;
const pwr_automatic_state_hash = @import("../src/models/automatic/pwr_automatic.zig").pwr_automatic_state_hash;
const pwr_automatic_step = @import("../src/models/automatic/pwr_automatic.zig").pwr_automatic_step;
const pwr_automatic_step_coupled = @import("../src/models/automatic/pwr_automatic.zig").pwr_automatic_step_coupled;

pub fn drive_input(arg_gear: pwr_automatic_gear, arg_engine_torque_nm: f64, arg_lockup_command: f64, arg_config: [*c]const pwr_automatic_config) callconv(.c) pwr_automatic_input {
    var gear = arg_gear;
    _ = &gear;
    var engine_torque_nm = arg_engine_torque_nm;
    _ = &engine_torque_nm;
    var lockup_command = arg_lockup_command;
    _ = &lockup_command;
    var config = arg_config;
    _ = &config;
    return pwr_automatic_input{
        .engine_torque_nm = engine_torque_nm,
        .output_external_torque_nm = 0.0,
        .line_pressure_command_pa = config.*.nominal_line_pressure_pa,
        .lockup_command = lockup_command,
        .requested_gear = gear,
    };
}

pub fn coupled_input(arg_gear: pwr_automatic_gear, arg_pump_speed_rad_s: f64, arg_config: [*c]const pwr_automatic_config) callconv(.c) pwr_automatic_coupled_input {
    var gear = arg_gear;
    _ = &gear;
    var pump_speed_rad_s = arg_pump_speed_rad_s;
    _ = &pump_speed_rad_s;
    var config = arg_config;
    _ = &config;
    return pwr_automatic_coupled_input{
        .pump_speed_rad_s = pump_speed_rad_s,
        .output_external_torque_nm = 0.0,
        .line_pressure_command_pa = config.*.nominal_line_pressure_pa,
        .lockup_command = 0.0,
        .requested_gear = gear,
    };
}

pub fn run_steps(arg_automatic: [*c]pwr_automatic, arg_input: [*c]const pwr_automatic_input, arg_step_count: u64) callconv(.c) void {
    var automatic = arg_automatic;
    _ = &automatic;
    var input = arg_input;
    _ = &input;
    var step_count = arg_step_count;
    _ = &step_count;
    {
        var step: u64 = 0;
        _ = &step;
        while (step < step_count) : (step +%= 1) {
            power_assert(@intFromBool(!!(pwr_automatic_step(automatic, input) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step(automatic, input) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 40))));
        }
    }
}

pub fn test_invalid_configuration_and_transactional_failure() callconv(.c) void {
    var config: pwr_automatic_config = undefined;
    _ = &config;
    pwr_automatic_config_default(&config);
    var untouched: pwr_automatic = undefined;
    _ = &untouched;
    _ = memset(@as(?*anyopaque, @ptrCast(&untouched)), @as(c_int, 165), @sizeOf(pwr_automatic));
    var original: pwr_automatic = untouched;
    _ = &original;
    config.forward_ratios[@as(c_uint, @intCast(@as(c_int, 1)))] = config.forward_ratios[@as(c_uint, @intCast(@as(c_int, 0)))];
    power_assert(@intFromBool(!!(pwr_automatic_init(&untouched, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_CONFIG)))), "pwr_automatic_init(&untouched, &config) == PWR_AUTOMATIC_INVALID_CONFIG", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 52))));
    power_assert(@intFromBool(!!(memcmp(@as(?*const anyopaque, @ptrCast(&untouched)), @as(?*const anyopaque, @ptrCast(&original)), @sizeOf(pwr_automatic)) == @as(c_int, 0))), "memcmp(&untouched, &original, sizeof(untouched)) == 0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 53))));
    power_assert(@intFromBool(!!(pwr_automatic_init(null, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_ARGUMENT)))), "pwr_automatic_init(NULL, &config) == PWR_AUTOMATIC_INVALID_ARGUMENT", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 54))));
    pwr_automatic_config_default(&config);
    var automatic: pwr_automatic = undefined;
    _ = &automatic;
    power_assert(@intFromBool(!!(pwr_automatic_init(&automatic, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_init(&automatic, &config) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 58))));
    const valid: pwr_automatic_input = drive_input(PWR_AUTOMATIC_GEAR_FIRST, 80.0, 0.0, &config);
    _ = &valid;
    run_steps(&automatic, &valid, @as(u64, @bitCast(@as(u64, @as(c_uint, 100)))));
    const before_hash: u64 = pwr_automatic_state_hash(&automatic);
    _ = &before_hash;
    var invalid: pwr_automatic_input = valid;
    _ = &invalid;
    invalid.line_pressure_command_pa = config.maximum_line_pressure_pa + 1.0;
    power_assert(@intFromBool(!!(pwr_automatic_step(&automatic, &invalid) == @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_INPUT)))), "pwr_automatic_step(&automatic, &invalid) == PWR_AUTOMATIC_INVALID_INPUT", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 66))));
    power_assert(@intFromBool(!!(pwr_automatic_state_hash(&automatic) == before_hash)), "pwr_automatic_state_hash(&automatic) == before_hash", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 67))));
    invalid = valid;
    invalid.requested_gear = @as(c_int, 5);
    power_assert(@intFromBool(!!(pwr_automatic_step(&automatic, &invalid) == @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_INPUT)))), "pwr_automatic_step(&automatic, &invalid) == PWR_AUTOMATIC_INVALID_INPUT", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 70))));
    power_assert(@intFromBool(!!(pwr_automatic_state_hash(&automatic) == before_hash)), "pwr_automatic_state_hash(&automatic) == before_hash", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 71))));
    var ratio: f64 = 123.0;
    _ = &ratio;
    power_assert(@intFromBool(!!(pwr_automatic_gear_ratio(&config, @as(c_int, 5), &ratio) == @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_INPUT)))), "pwr_automatic_gear_ratio(&config, (pwr_automatic_gear)5, &ratio) == PWR_AUTOMATIC_INVALID_INPUT", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 75))));
    power_assert(@intFromBool(!!(ratio == 123.0)), "ratio == 123.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 76))));
}

pub fn test_converter_stall_and_coupling_trends() callconv(.c) void {
    var config: pwr_automatic_config = undefined;
    _ = &config;
    pwr_automatic_config_default(&config);
    var stall: pwr_automatic_converter_point = undefined;
    _ = &stall;
    var mid: pwr_automatic_converter_point = undefined;
    _ = &mid;
    var coupled: pwr_automatic_converter_point = undefined;
    _ = &coupled;
    var equal_speed: pwr_automatic_converter_point = undefined;
    _ = &equal_speed;
    power_assert(@intFromBool(!!(pwr_automatic_converter_evaluate(&config, 220.0, 0.0, &stall) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_converter_evaluate(&config, 220.0, 0.0, &stall) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 88))));
    power_assert(@intFromBool(!!(pwr_automatic_converter_evaluate(&config, 220.0, 110.0, &mid) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_converter_evaluate(&config, 220.0, 110.0, &mid) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 90))));
    power_assert(@intFromBool(!!(pwr_automatic_converter_evaluate(&config, 220.0, 198.0, &coupled) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_converter_evaluate(&config, 220.0, 198.0, &coupled) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 92))));
    power_assert(@intFromBool(!!(pwr_automatic_converter_evaluate(&config, 220.0, 220.0, &equal_speed) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_converter_evaluate(&config, 220.0, 220.0, &equal_speed) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 94))));
    power_assert(@intFromBool(!!(fabs(stall.speed_ratio) < 0.000000000001)), "fabs(stall.speed_ratio) < 1.0e-12", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 96))));
    power_assert(@intFromBool(!!(fabs(stall.torque_ratio - config.converter_stall_torque_ratio) < 0.000000000001)), "fabs(stall.torque_ratio - config.converter_stall_torque_ratio) < 1.0e-12", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 97))));
    power_assert(@intFromBool(!!(stall.turbine_torque_nm > stall.pump_torque_nm)), "stall.turbine_torque_nm > stall.pump_torque_nm", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 98))));
    power_assert(@intFromBool(!!(stall.slip_power_w > 0.0)), "stall.slip_power_w > 0.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 99))));
    power_assert(@intFromBool(!!(mid.torque_ratio < stall.torque_ratio)), "mid.torque_ratio < stall.torque_ratio", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 100))));
    power_assert(@intFromBool(!!(mid.torque_ratio > coupled.torque_ratio)), "mid.torque_ratio > coupled.torque_ratio", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 101))));
    power_assert(@intFromBool(!!(coupled.torque_ratio >= 1.0)), "coupled.torque_ratio >= 1.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 102))));
    power_assert(@intFromBool(!!(coupled.pump_torque_nm < stall.pump_torque_nm)), "coupled.pump_torque_nm < stall.pump_torque_nm", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 103))));
    power_assert(@intFromBool(!!(fabs(equal_speed.pump_torque_nm) < 0.000000000001)), "fabs(equal_speed.pump_torque_nm) < 1.0e-12", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 104))));
    power_assert(@intFromBool(!!(fabs(equal_speed.turbine_torque_nm) < 0.000000000001)), "fabs(equal_speed.turbine_torque_nm) < 1.0e-12", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 105))));
    power_assert(@intFromBool(!!(fabs(equal_speed.slip_power_w) < 0.000000000001)), "fabs(equal_speed.slip_power_w) < 1.0e-12", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 106))));
    var sentinel: pwr_automatic_converter_point = pwr_automatic_converter_point{
        .speed_ratio = 7.0,
        .torque_ratio = 8.0,
        .pump_torque_nm = 9.0,
        .turbine_torque_nm = 10.0,
        .slip_power_w = 11.0,
    };
    _ = &sentinel;
    const before: pwr_automatic_converter_point = sentinel;
    _ = &before;
    power_assert(@intFromBool(!!(pwr_automatic_converter_evaluate(&config, @as(f64, @floatCast(__builtin_nanf(""))), 0.0, &sentinel) == @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_INPUT)))), "pwr_automatic_converter_evaluate(&config, NAN, 0.0, &sentinel) == PWR_AUTOMATIC_INVALID_INPUT", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 117))));
    power_assert(@intFromBool(!!(memcmp(@as(?*const anyopaque, @ptrCast(&sentinel)), @as(?*const anyopaque, @ptrCast(&before)), @sizeOf(pwr_automatic_converter_point)) == @as(c_int, 0))), "memcmp(&sentinel, &before, sizeof(sentinel)) == 0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 118))));
}

pub fn test_deterministic_fixed_step() callconv(.c) void {
    var config: pwr_automatic_config = undefined;
    _ = &config;
    pwr_automatic_config_default(&config);
    var first: pwr_automatic = undefined;
    _ = &first;
    var second: pwr_automatic = undefined;
    _ = &second;
    power_assert(@intFromBool(!!(pwr_automatic_init(&first, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_init(&first, &config) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 126))));
    power_assert(@intFromBool(!!(pwr_automatic_init(&second, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_init(&second, &config) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 127))));
    var input: pwr_automatic_input = drive_input(PWR_AUTOMATIC_GEAR_FIRST, 95.0, 0.0, &config);
    _ = &input;
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, @bitCast(@as(u64, @as(c_uint, 15000))))) : (step +%= 1) {
            power_assert(@intFromBool(!!(pwr_automatic_step(&first, &input) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step(&first, &input) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 132))));
            power_assert(@intFromBool(!!(pwr_automatic_step(&second, &input) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step(&second, &input) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 133))));
            power_assert(@intFromBool(!!(pwr_automatic_state_hash(&first) == pwr_automatic_state_hash(&second))), "pwr_automatic_state_hash(&first) == pwr_automatic_state_hash(&second)", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 134))));
        }
    }
    input.requested_gear = PWR_AUTOMATIC_GEAR_SECOND;
    input.lockup_command = 0.6;
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, @bitCast(@as(u64, @as(c_uint, 10000))))) : (step +%= 1) {
            power_assert(@intFromBool(!!(pwr_automatic_step(&first, &input) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step(&first, &input) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 140))));
            power_assert(@intFromBool(!!(pwr_automatic_step(&second, &input) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step(&second, &input) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 141))));
            power_assert(@intFromBool(!!(pwr_automatic_state_hash(&first) == pwr_automatic_state_hash(&second))), "pwr_automatic_state_hash(&first) == pwr_automatic_state_hash(&second)", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 142))));
        }
    }
    var snapshot: pwr_automatic_snapshot = undefined;
    _ = &snapshot;
    power_assert(@intFromBool(!!(pwr_automatic_snapshot_read(&first, &snapshot) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_snapshot_read(&first, &snapshot) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 146))));
    power_assert(@intFromBool(!!(fabs(snapshot.time_s - 2.5) < 0.000000000001)), "fabs(snapshot.time_s - 2.5) < 1.0e-12", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 147))));
    power_assert(@intFromBool(!!(snapshot.active_gear == PWR_AUTOMATIC_GEAR_SECOND)), "snapshot.active_gear == PWR_AUTOMATIC_GEAR_SECOND", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 148))));
    power_assert(@intFromBool(!!!snapshot.shift_active), "!snapshot.shift_active", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 149))));
    power_assert(@intFromBool(!!(snapshot.pump_speed_rad_s > 0.0)), "snapshot.pump_speed_rad_s > 0.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 150))));
    power_assert(@intFromBool(!!(snapshot.output_speed_rad_s > 0.0)), "snapshot.output_speed_rad_s > 0.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 151))));
    power_assert(@intFromBool(!!(snapshot.accumulated_loss_j > 0.0)), "snapshot.accumulated_loss_j > 0.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 152))));
}

pub fn test_lockup_reduces_converter_slip() callconv(.c) void {
    var config: pwr_automatic_config = undefined;
    _ = &config;
    pwr_automatic_config_default(&config);
    config.output_viscous_drag_nm_s_rad = 0.8;
    var baseline: pwr_automatic = undefined;
    _ = &baseline;
    power_assert(@intFromBool(!!(pwr_automatic_init(&baseline, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_init(&baseline, &config) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 161))));
    var input: pwr_automatic_input = drive_input(PWR_AUTOMATIC_GEAR_FOURTH, 90.0, 0.0, &config);
    _ = &input;
    run_steps(&baseline, &input, @as(u64, @bitCast(@as(u64, @as(c_uint, 18000)))));
    var open_converter: pwr_automatic = baseline;
    _ = &open_converter;
    var locked_converter: pwr_automatic = baseline;
    _ = &locked_converter;
    var open_input: pwr_automatic_input = input;
    _ = &open_input;
    var locked_input: pwr_automatic_input = input;
    _ = &locked_input;
    locked_input.lockup_command = 1.0;
    run_steps(&open_converter, &open_input, @as(u64, @bitCast(@as(u64, @as(c_uint, 18000)))));
    run_steps(&locked_converter, &locked_input, @as(u64, @bitCast(@as(u64, @as(c_uint, 18000)))));
    var open_snapshot: pwr_automatic_snapshot = undefined;
    _ = &open_snapshot;
    var locked_snapshot: pwr_automatic_snapshot = undefined;
    _ = &locked_snapshot;
    power_assert(@intFromBool(!!(pwr_automatic_snapshot_read(&open_converter, &open_snapshot) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_snapshot_read(&open_converter, &open_snapshot) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 176))));
    power_assert(@intFromBool(!!(pwr_automatic_snapshot_read(&locked_converter, &locked_snapshot) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_snapshot_read(&locked_converter, &locked_snapshot) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 178))));
    power_assert(@intFromBool(!!(locked_snapshot.lockup_engagement > 0.95)), "locked_snapshot.lockup_engagement > 0.95", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 179))));
    power_assert(@intFromBool(!!((fabs(locked_snapshot.lockup_slip_rad_s) + 10.0) < fabs(open_snapshot.lockup_slip_rad_s))), "fabs(locked_snapshot.lockup_slip_rad_s) + 10.0 < fabs(open_snapshot.lockup_slip_rad_s)", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 181))));
    power_assert(@intFromBool(!!(fabs(locked_snapshot.lockup_slip_rad_s) < 5.0)), "fabs(locked_snapshot.lockup_slip_rad_s) < 5.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 182))));
}

pub fn test_one_to_two_shift_is_continuous() callconv(.c) void {
    var config: pwr_automatic_config = undefined;
    _ = &config;
    pwr_automatic_config_default(&config);
    config.output_viscous_drag_nm_s_rad = 0.6;
    var automatic: pwr_automatic = undefined;
    _ = &automatic;
    power_assert(@intFromBool(!!(pwr_automatic_init(&automatic, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_init(&automatic, &config) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 191))));
    var input: pwr_automatic_input = drive_input(PWR_AUTOMATIC_GEAR_FIRST, 85.0, 0.0, &config);
    _ = &input;
    run_steps(&automatic, &input, @as(u64, @bitCast(@as(u64, @as(c_uint, 16000)))));
    var before: pwr_automatic_snapshot = undefined;
    _ = &before;
    power_assert(@intFromBool(!!(pwr_automatic_snapshot_read(&automatic, &before) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_snapshot_read(&automatic, &before) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 197))));
    power_assert(@intFromBool(!!(before.active_gear == PWR_AUTOMATIC_GEAR_FIRST)), "before.active_gear == PWR_AUTOMATIC_GEAR_FIRST", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 198))));
    power_assert(@intFromBool(!!!before.shift_active), "!before.shift_active", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 199))));
    input.requested_gear = PWR_AUTOMATIC_GEAR_SECOND;
    power_assert(@intFromBool(!!(pwr_automatic_step(&automatic, &input) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step(&automatic, &input) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 202))));
    var first_shift_step: pwr_automatic_snapshot = undefined;
    _ = &first_shift_step;
    power_assert(@intFromBool(!!(pwr_automatic_snapshot_read(&automatic, &first_shift_step) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_snapshot_read(&automatic, &first_shift_step) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 204))));
    power_assert(@intFromBool(!!first_shift_step.shift_active), "first_shift_step.shift_active", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 205))));
    power_assert(@intFromBool(!!(first_shift_step.effective_ratio <= before.effective_ratio)), "first_shift_step.effective_ratio <= before.effective_ratio", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 206))));
    power_assert(@intFromBool(!!(fabs(first_shift_step.gear_output_torque_nm - before.gear_output_torque_nm) < 1.0)), "fabs(first_shift_step.gear_output_torque_nm - before.gear_output_torque_nm) < 1.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 207))));
    var previous_ratio: f64 = first_shift_step.effective_ratio;
    _ = &previous_ratio;
    var previous_torque: f64 = first_shift_step.gear_output_torque_nm;
    _ = &previous_torque;
    var maximum_torque_step: f64 = 0.0;
    _ = &maximum_torque_step;
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, @bitCast(@as(u64, @as(c_uint, 8000))))) : (step +%= 1) {
            power_assert(@intFromBool(!!(pwr_automatic_step(&automatic, &input) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step(&automatic, &input) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 213))));
            var current: pwr_automatic_snapshot = undefined;
            _ = &current;
            power_assert(@intFromBool(!!(pwr_automatic_snapshot_read(&automatic, &current) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_snapshot_read(&automatic, &current) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 215))));
            power_assert(@intFromBool(!!(current.effective_ratio <= (previous_ratio + 0.000000000001))), "current.effective_ratio <= previous_ratio + 1.0e-12", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 216))));
            maximum_torque_step = fmax(maximum_torque_step, fabs(current.gear_output_torque_nm - previous_torque));
            previous_ratio = current.effective_ratio;
            previous_torque = current.gear_output_torque_nm;
        }
    }
    var after: pwr_automatic_snapshot = undefined;
    _ = &after;
    power_assert(@intFromBool(!!(pwr_automatic_snapshot_read(&automatic, &after) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_snapshot_read(&automatic, &after) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 224))));
    power_assert(@intFromBool(!!(after.active_gear == PWR_AUTOMATIC_GEAR_SECOND)), "after.active_gear == PWR_AUTOMATIC_GEAR_SECOND", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 225))));
    power_assert(@intFromBool(!!!after.shift_active), "!after.shift_active", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 226))));
    power_assert(@intFromBool(!!(fabs(after.effective_ratio - config.forward_ratios[@as(c_uint, @intCast(@as(c_int, 1)))]) < 0.000000000001)), "fabs(after.effective_ratio - config.forward_ratios[1]) < 1.0e-12", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 227))));
    power_assert(@intFromBool(!!(maximum_torque_step < 5.0)), "maximum_torque_step < 5.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 228))));
}

pub fn test_oil_heating_and_pressure_diagnostics() callconv(.c) void {
    var config: pwr_automatic_config = undefined;
    _ = &config;
    pwr_automatic_config_default(&config);
    config.initial_oil_temperature_k = config.ambient_temperature_k;
    config.initial_lockup_temperature_k = config.ambient_temperature_k;
    config.oil_cooling_conductance_w_k = 0.0;
    var hot: pwr_automatic = undefined;
    _ = &hot;
    power_assert(@intFromBool(!!(pwr_automatic_init(&hot, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_init(&hot, &config) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 239))));
    const drive: pwr_automatic_input = drive_input(PWR_AUTOMATIC_GEAR_FIRST, 110.0, 0.0, &config);
    _ = &drive;
    run_steps(&hot, &drive, @as(u64, @bitCast(@as(u64, @as(c_uint, 30000)))));
    var hot_snapshot: pwr_automatic_snapshot = undefined;
    _ = &hot_snapshot;
    power_assert(@intFromBool(!!(pwr_automatic_snapshot_read(&hot, &hot_snapshot) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_snapshot_read(&hot, &hot_snapshot) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 244))));
    power_assert(@intFromBool(!!(hot_snapshot.oil_temperature_k > (config.ambient_temperature_k + 0.02))), "hot_snapshot.oil_temperature_k > config.ambient_temperature_k + 0.02", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 245))));
    power_assert(@intFromBool(!!(hot_snapshot.accumulated_loss_j > 100.0)), "hot_snapshot.accumulated_loss_j > 100.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 246))));
    var low_pressure: pwr_automatic = undefined;
    _ = &low_pressure;
    power_assert(@intFromBool(!!(pwr_automatic_init(&low_pressure, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_init(&low_pressure, &config) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 249))));
    var starved: pwr_automatic_input = drive;
    _ = &starved;
    starved.line_pressure_command_pa = 0.0;
    run_steps(&low_pressure, &starved, @as(u64, @bitCast(@as(u64, @as(c_uint, 16000)))));
    var diagnostic_snapshot: pwr_automatic_snapshot = undefined;
    _ = &diagnostic_snapshot;
    power_assert(@intFromBool(!!(pwr_automatic_snapshot_read(&low_pressure, &diagnostic_snapshot) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_snapshot_read(&low_pressure, &diagnostic_snapshot) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 255))));
    power_assert(@intFromBool(!!((diagnostic_snapshot.diagnostic_flags & @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_LOW_LINE_PRESSURE))) != @as(c_uint, 0))), "(diagnostic_snapshot.diagnostic_flags & PWR_AUTOMATIC_DIAG_LOW_LINE_PRESSURE) != 0U", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 256))));
    power_assert(@intFromBool(!!((diagnostic_snapshot.diagnostic_flags & @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_SHIFT_TIMEOUT))) != @as(c_uint, 0))), "(diagnostic_snapshot.diagnostic_flags & PWR_AUTOMATIC_DIAG_SHIFT_TIMEOUT) != 0U", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 257))));
}

pub fn test_coupled_boundary_reaction_trends() callconv(.c) void {
    var config: pwr_automatic_config = undefined;
    _ = &config;
    pwr_automatic_config_default(&config);
    var stall: pwr_automatic = undefined;
    _ = &stall;
    var near_coupling: pwr_automatic = undefined;
    _ = &near_coupling;
    power_assert(@intFromBool(!!(pwr_automatic_init(&stall, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_init(&stall, &config) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 265))));
    power_assert(@intFromBool(!!(pwr_automatic_init(&near_coupling, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_init(&near_coupling, &config) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 266))));
    near_coupling.turbine_speed_rad_s = 219.0;
    var input: pwr_automatic_coupled_input = coupled_input(PWR_AUTOMATIC_GEAR_NEUTRAL, 220.0, &config);
    _ = &input;
    input.line_pressure_command_pa = 0.0;
    var stall_output: pwr_automatic_coupling_output = undefined;
    _ = &stall_output;
    var coupled_output: pwr_automatic_coupling_output = undefined;
    _ = &coupled_output;
    power_assert(@intFromBool(!!(pwr_automatic_step_coupled(&stall, &input, &stall_output) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step_coupled(&stall, &input, &stall_output) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 274))));
    power_assert(@intFromBool(!!(pwr_automatic_step_coupled(&near_coupling, &input, &coupled_output) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step_coupled(&near_coupling, &input, &coupled_output) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 276))));
    power_assert(@intFromBool(!!(stall.pump_speed_rad_s == input.pump_speed_rad_s)), "stall.pump_speed_rad_s == input.pump_speed_rad_s", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 278))));
    power_assert(@intFromBool(!!(near_coupling.pump_speed_rad_s == input.pump_speed_rad_s)), "near_coupling.pump_speed_rad_s == input.pump_speed_rad_s", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 279))));
    power_assert(@intFromBool(!!(stall_output.pump_load_torque_nm > (coupled_output.pump_load_torque_nm + 80.0))), "stall_output.pump_load_torque_nm > coupled_output.pump_load_torque_nm + 80.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 280))));
    power_assert(@intFromBool(!!(stall_output.engine_reaction_torque_nm < coupled_output.engine_reaction_torque_nm)), "stall_output.engine_reaction_torque_nm < coupled_output.engine_reaction_torque_nm", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 281))));
    power_assert(@intFromBool(!!(fabs(stall_output.engine_reaction_torque_nm + stall_output.pump_load_torque_nm) < 0.000000000001)), "fabs(stall_output.engine_reaction_torque_nm + stall_output.pump_load_torque_nm) < 1.0e-12", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 283))));
    power_assert(@intFromBool(!!(fabs(stall_output.boundary_power_w - (stall_output.pump_load_torque_nm * input.pump_speed_rad_s)) < 0.000000001)), "fabs(stall_output.boundary_power_w - stall_output.pump_load_torque_nm * input.pump_speed_rad_s) < 1.0e-9", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 286))));
    const expected_projection: f64 = ((0.5 * config.pump_inertia_kg_m2) * input.pump_speed_rad_s) * input.pump_speed_rad_s;
    _ = &expected_projection;
    power_assert(@intFromBool(!!(fabs(stall_output.projection_adjustment_j - expected_projection) < 0.000000000001)), "fabs(stall_output.projection_adjustment_j - expected_projection) < 1.0e-12", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 290))));
    power_assert(@intFromBool(!!(stall_output.boundary_energy_j > 0.0)), "stall_output.boundary_energy_j > 0.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 291))));
    power_assert(@intFromBool(!!((stall.diagnostic_flags & @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_COUPLING_SPEED_PROJECTED))) != @as(c_uint, 0))), "(stall.diagnostic_flags & PWR_AUTOMATIC_DIAG_COUPLING_SPEED_PROJECTED) != 0U", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 292))));
    var repeated_output: pwr_automatic_coupling_output = undefined;
    _ = &repeated_output;
    power_assert(@intFromBool(!!(pwr_automatic_step_coupled(&stall, &input, &repeated_output) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step_coupled(&stall, &input, &repeated_output) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 295))));
    power_assert(@intFromBool(!!(stall.pump_speed_rad_s == input.pump_speed_rad_s)), "stall.pump_speed_rad_s == input.pump_speed_rad_s", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 296))));
    power_assert(@intFromBool(!!(repeated_output.projection_adjustment_j == 0.0)), "repeated_output.projection_adjustment_j == 0.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 297))));
    power_assert(@intFromBool(!!(repeated_output.boundary_energy_j > stall_output.boundary_energy_j)), "repeated_output.boundary_energy_j > stall_output.boundary_energy_j", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 298))));
}

pub fn test_coupled_boundary_deterministic_and_transactional() callconv(.c) void {
    var config: pwr_automatic_config = undefined;
    _ = &config;
    pwr_automatic_config_default(&config);
    var first: pwr_automatic = undefined;
    _ = &first;
    var second: pwr_automatic = undefined;
    _ = &second;
    power_assert(@intFromBool(!!(pwr_automatic_init(&first, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_init(&first, &config) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 306))));
    power_assert(@intFromBool(!!(pwr_automatic_init(&second, &config) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_init(&second, &config) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 307))));
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, @bitCast(@as(u64, @as(c_uint, 12000))))) : (step +%= 1) {
            const commanded_speed: f64 = 160.0 + (@as(f64, @floatFromInt(step % @as(u64, @bitCast(@as(u64, @as(c_uint, 1000)))))) * 0.02);
            _ = &commanded_speed;
            var input: pwr_automatic_coupled_input = coupled_input(PWR_AUTOMATIC_GEAR_FIRST, commanded_speed, &config);
            _ = &input;
            if (step >= @as(u64, @bitCast(@as(u64, @as(c_uint, 7000))))) {
                input.requested_gear = PWR_AUTOMATIC_GEAR_SECOND;
                input.lockup_command = 0.7;
            }
            var first_output: pwr_automatic_coupling_output = undefined;
            _ = &first_output;
            var second_output: pwr_automatic_coupling_output = undefined;
            _ = &second_output;
            power_assert(@intFromBool(!!(pwr_automatic_step_coupled(&first, &input, &first_output) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step_coupled(&first, &input, &first_output) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 320))));
            power_assert(@intFromBool(!!(pwr_automatic_step_coupled(&second, &input, &second_output) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_step_coupled(&second, &input, &second_output) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 322))));
            power_assert(@intFromBool(!!(first.pump_speed_rad_s == commanded_speed)), "first.pump_speed_rad_s == commanded_speed", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 323))));
            power_assert(@intFromBool(!!(second.pump_speed_rad_s == commanded_speed)), "second.pump_speed_rad_s == commanded_speed", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 324))));
            power_assert(@intFromBool(!!(first_output.engine_reaction_torque_nm == second_output.engine_reaction_torque_nm)), "first_output.engine_reaction_torque_nm == second_output.engine_reaction_torque_nm", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 326))));
            power_assert(@intFromBool(!!(first_output.boundary_energy_j == second_output.boundary_energy_j)), "first_output.boundary_energy_j == second_output.boundary_energy_j", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 327))));
            power_assert(@intFromBool(!!(pwr_automatic_state_hash(&first) == pwr_automatic_state_hash(&second))), "pwr_automatic_state_hash(&first) == pwr_automatic_state_hash(&second)", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 328))));
        }
    }
    var snapshot: pwr_automatic_snapshot = undefined;
    _ = &snapshot;
    power_assert(@intFromBool(!!(pwr_automatic_snapshot_read(&first, &snapshot) == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK)))), "pwr_automatic_snapshot_read(&first, &snapshot) == PWR_AUTOMATIC_OK", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 332))));
    power_assert(@intFromBool(!!snapshot.pump_speed_imposed), "snapshot.pump_speed_imposed", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 333))));
    power_assert(@intFromBool(!!(snapshot.active_gear == PWR_AUTOMATIC_GEAR_SECOND)), "snapshot.active_gear == PWR_AUTOMATIC_GEAR_SECOND", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 334))));
    power_assert(@intFromBool(!!(snapshot.turbine_speed_rad_s > 0.0)), "snapshot.turbine_speed_rad_s > 0.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 335))));
    power_assert(@intFromBool(!!(snapshot.output_speed_rad_s > 0.0)), "snapshot.output_speed_rad_s > 0.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 336))));
    power_assert(@intFromBool(!!(snapshot.line_pressure_pa > config.minimum_drive_pressure_pa)), "snapshot.line_pressure_pa > config.minimum_drive_pressure_pa", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 337))));
    power_assert(@intFromBool(!!(snapshot.lockup_engagement > 0.0)), "snapshot.lockup_engagement > 0.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 338))));
    power_assert(@intFromBool(!!(snapshot.pump_boundary_energy_j > 0.0)), "snapshot.pump_boundary_energy_j > 0.0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 339))));
    power_assert(@intFromBool(!!(power_isfinite(snapshot.coupling_adjustment_j) != 0)), "isfinite(snapshot.coupling_adjustment_j)", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 340))));
    const before_hash: u64 = pwr_automatic_state_hash(&first);
    _ = &before_hash;
    var invalid: pwr_automatic_coupled_input = coupled_input(PWR_AUTOMATIC_GEAR_SECOND, config.maximum_shaft_speed_rad_s + 1.0, &config);
    _ = &invalid;
    var untouched: pwr_automatic_coupling_output = pwr_automatic_coupling_output{
        .engine_reaction_torque_nm = 1.0,
        .pump_load_torque_nm = 2.0,
        .boundary_power_w = 3.0,
        .boundary_energy_j = 4.0,
        .projection_adjustment_j = 5.0,
        .accumulated_projection_adjustment_j = 6.0,
    };
    _ = &untouched;
    const original: pwr_automatic_coupling_output = untouched;
    _ = &original;
    power_assert(@intFromBool(!!(pwr_automatic_step_coupled(&first, &invalid, &untouched) == @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_INPUT)))), "pwr_automatic_step_coupled(&first, &invalid, &untouched) == PWR_AUTOMATIC_INVALID_INPUT", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 356))));
    power_assert(@intFromBool(!!(pwr_automatic_state_hash(&first) == before_hash)), "pwr_automatic_state_hash(&first) == before_hash", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 357))));
    power_assert(@intFromBool(!!(memcmp(@as(?*const anyopaque, @ptrCast(&untouched)), @as(?*const anyopaque, @ptrCast(&original)), @sizeOf(pwr_automatic_coupling_output)) == @as(c_int, 0))), "memcmp(&untouched, &original, sizeof(untouched)) == 0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 358))));
    invalid.pump_speed_rad_s = 180.0;
    invalid.lockup_command = @as(f64, @floatCast(__builtin_nanf("")));
    power_assert(@intFromBool(!!(pwr_automatic_step_coupled(&first, &invalid, &untouched) == @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_INPUT)))), "pwr_automatic_step_coupled(&first, &invalid, &untouched) == PWR_AUTOMATIC_INVALID_INPUT", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 363))));
    power_assert(@intFromBool(!!(pwr_automatic_state_hash(&first) == before_hash)), "pwr_automatic_state_hash(&first) == before_hash", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 364))));
    power_assert(@intFromBool(!!(memcmp(@as(?*const anyopaque, @ptrCast(&untouched)), @as(?*const anyopaque, @ptrCast(&original)), @sizeOf(pwr_automatic_coupling_output)) == @as(c_int, 0))), "memcmp(&untouched, &original, sizeof(untouched)) == 0", "legacy/native/tests/test_automatic.c", @as(c_uint, @bitCast(@as(c_int, 365))));
}

pub fn run() c_int {
    test_invalid_configuration_and_transactional_failure();
    test_converter_stall_and_coupling_trends();
    test_deterministic_fixed_step();
    test_lockup_reduces_converter_slip();
    test_one_to_two_shift_is_continuous();
    test_oil_heating_and_pressure_diagnostics();
    test_coupled_boundary_reaction_trends();
    test_coupled_boundary_deterministic_and_transactional();
    return 0;
}

pub const NAN = __builtin_nanf("");

pub fn main() void {
    @import("std").process.exit(@intCast(run()));
}
