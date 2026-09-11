// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/tests/test_shaft.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_SHAFT_DIAG_CONTROLLER_BROWNOUT = @import("../src/abi.zig").PWR_SHAFT_DIAG_CONTROLLER_BROWNOUT;
const PWR_SHAFT_DIAG_SENSOR_INVALID = @import("../src/abi.zig").PWR_SHAFT_DIAG_SENSOR_INVALID;
const PWR_SHAFT_FAULT_LV_BROWNOUT = @import("../src/abi.zig").PWR_SHAFT_FAULT_LV_BROWNOUT;
const PWR_SHAFT_FAULT_NONE = @import("../src/abi.zig").PWR_SHAFT_FAULT_NONE;
const PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS = @import("../src/abi.zig").PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS;
const PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT = @import("../src/abi.zig").PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT;
const PWR_SHAFT_INVALID_ARGUMENT = @import("../src/abi.zig").PWR_SHAFT_INVALID_ARGUMENT;
const PWR_SHAFT_INVALID_CONFIG = @import("../src/abi.zig").PWR_SHAFT_INVALID_CONFIG;
const PWR_SHAFT_OK = @import("../src/abi.zig").PWR_SHAFT_OK;
const fabs = @import("../src/support.zig").fabs;
const fmax = @import("../src/support.zig").fmax;
const power_assert = @import("../src/support.zig").power_assert;
const pwr_shaft_config = @import("../src/abi.zig").pwr_shaft_config;
const pwr_shaft_config_default = @import("../src/models/shaft/pwr_shaft_lab.zig").pwr_shaft_config_default;
const pwr_shaft_input = @import("../src/abi.zig").pwr_shaft_input;
const pwr_shaft_lab = @import("../src/abi.zig").pwr_shaft_lab;
const pwr_shaft_lab_init = @import("../src/models/shaft/pwr_shaft_lab.zig").pwr_shaft_lab_init;
const pwr_shaft_lab_snapshot = @import("../src/models/shaft/pwr_shaft_lab.zig").pwr_shaft_lab_snapshot;
const pwr_shaft_lab_state_hash = @import("../src/models/shaft/pwr_shaft_lab.zig").pwr_shaft_lab_state_hash;
const pwr_shaft_lab_step = @import("../src/models/shaft/pwr_shaft_lab.zig").pwr_shaft_lab_step;
const pwr_shaft_snapshot = @import("../src/abi.zig").pwr_shaft_snapshot;

pub fn run_nominal(arg_lab: [*c]pwr_shaft_lab, arg_steps: u64, arg_target_speed: f64, arg_load_torque: f64) callconv(.c) pwr_shaft_snapshot {
    var lab = arg_lab;
    _ = &lab;
    var steps = arg_steps;
    _ = &steps;
    var target_speed = arg_target_speed;
    _ = &target_speed;
    var load_torque = arg_load_torque;
    _ = &load_torque;
    const input: pwr_shaft_input = pwr_shaft_input{
        .target_speed_rad_s = target_speed,
        .load_torque_nm = load_torque,
        .fault_mask = @as(u32, @bitCast(PWR_SHAFT_FAULT_NONE)),
        .speed_sensor_bias_rad_s = 0,
        .actuator_stuck_duty = 0,
    };
    _ = &input;
    {
        var i: u64 = 0;
        _ = &i;
        while (i < steps) : (i +%= 1) {
            power_assert(@intFromBool(!!(pwr_shaft_lab_step(lab, &input) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_step(lab, &input) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 20))));
        }
    }
    var snapshot: pwr_shaft_snapshot = undefined;
    _ = &snapshot;
    power_assert(@intFromBool(!!(pwr_shaft_lab_snapshot(lab, &snapshot) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_snapshot(lab, &snapshot) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 23))));
    return snapshot;
}

pub fn test_nominal_and_deterministic() callconv(.c) void {
    var config: pwr_shaft_config = undefined;
    _ = &config;
    pwr_shaft_config_default(&config);
    var first: pwr_shaft_lab = undefined;
    _ = &first;
    var second: pwr_shaft_lab = undefined;
    _ = &second;
    power_assert(@intFromBool(!!(pwr_shaft_lab_init(&first, &config) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_init(&first, &config) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 33))));
    power_assert(@intFromBool(!!(pwr_shaft_lab_init(&second, &config) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_init(&second, &config) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 34))));
    const input: pwr_shaft_input = pwr_shaft_input{
        .target_speed_rad_s = 160.0,
        .load_torque_nm = 20.0,
        .fault_mask = @import("std").mem.zeroes(u32),
        .speed_sensor_bias_rad_s = 0,
        .actuator_stuck_duty = 0,
    };
    _ = &input;
    {
        var i: u64 = 0;
        _ = &i;
        while (i < @as(u64, @bitCast(@as(u64, @as(c_uint, 30000))))) : (i +%= 1) {
            power_assert(@intFromBool(!!(pwr_shaft_lab_step(&first, &input) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_step(&first, &input) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 41))));
            power_assert(@intFromBool(!!(pwr_shaft_lab_step(&second, &input) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_step(&second, &input) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 42))));
            power_assert(@intFromBool(!!(pwr_shaft_lab_state_hash(&first) == pwr_shaft_lab_state_hash(&second))), "pwr_shaft_lab_state_hash(&first) == pwr_shaft_lab_state_hash(&second)", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 43))));
        }
    }
    var snapshot: pwr_shaft_snapshot = undefined;
    _ = &snapshot;
    power_assert(@intFromBool(!!(pwr_shaft_lab_snapshot(&first, &snapshot) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_snapshot(&first, &snapshot) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 47))));
    power_assert(@intFromBool(!!snapshot.controller_active), "snapshot.controller_active", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 48))));
    power_assert(@intFromBool(!!snapshot.sensor_valid), "snapshot.sensor_valid", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 49))));
    power_assert(@intFromBool(!!(snapshot.load_speed_rad_s > 120.0)), "snapshot.load_speed_rad_s > 120.0", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 50))));
    power_assert(@intFromBool(!!(snapshot.load_speed_rad_s < 200.0)), "snapshot.load_speed_rad_s < 200.0", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 51))));
    power_assert(@intFromBool(!!(snapshot.mechanical_output_j > 0.0)), "snapshot.mechanical_output_j > 0.0", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 52))));
    power_assert(@intFromBool(!!(snapshot.motor_temperature_k >= config.ambient_temperature_k)), "snapshot.motor_temperature_k >= config.ambient_temperature_k", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 53))));
    const transfer_scale: f64 = fabs(snapshot.electrical_input_j) + 1.0;
    _ = &transfer_scale;
    power_assert(@intFromBool(!!((fabs(snapshot.energy_residual_j) / transfer_scale) < 0.03)), "fabs(snapshot.energy_residual_j) / transfer_scale < 0.03", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 56))));
}

pub fn test_faults_close_the_loop() callconv(.c) void {
    var config: pwr_shaft_config = undefined;
    _ = &config;
    pwr_shaft_config_default(&config);
    var nominal: pwr_shaft_lab = undefined;
    _ = &nominal;
    var biased: pwr_shaft_lab = undefined;
    _ = &biased;
    power_assert(@intFromBool(!!(pwr_shaft_lab_init(&nominal, &config) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_init(&nominal, &config) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 65))));
    power_assert(@intFromBool(!!(pwr_shaft_lab_init(&biased, &config) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_init(&biased, &config) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 66))));
    const nominal_input: pwr_shaft_input = pwr_shaft_input{
        .target_speed_rad_s = 160.0,
        .load_torque_nm = 20.0,
        .fault_mask = @import("std").mem.zeroes(u32),
        .speed_sensor_bias_rad_s = 0,
        .actuator_stuck_duty = 0,
    };
    _ = &nominal_input;
    var biased_input: pwr_shaft_input = nominal_input;
    _ = &biased_input;
    biased_input.fault_mask = @as(u32, @bitCast(PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS));
    biased_input.speed_sensor_bias_rad_s = 45.0;
    {
        var i: u64 = 0;
        _ = &i;
        while (i < @as(u64, @bitCast(@as(u64, @as(c_uint, 25000))))) : (i +%= 1) {
            power_assert(@intFromBool(!!(pwr_shaft_lab_step(&nominal, &nominal_input) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_step(&nominal, &nominal_input) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 77))));
            power_assert(@intFromBool(!!(pwr_shaft_lab_step(&biased, &biased_input) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_step(&biased, &biased_input) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 78))));
        }
    }
    var nominal_snapshot: pwr_shaft_snapshot = undefined;
    _ = &nominal_snapshot;
    var biased_snapshot: pwr_shaft_snapshot = undefined;
    _ = &biased_snapshot;
    power_assert(@intFromBool(!!(pwr_shaft_lab_snapshot(&nominal, &nominal_snapshot) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_snapshot(&nominal, &nominal_snapshot) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 83))));
    power_assert(@intFromBool(!!(pwr_shaft_lab_snapshot(&biased, &biased_snapshot) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_snapshot(&biased, &biased_snapshot) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 84))));
    power_assert(@intFromBool(!!((biased_snapshot.load_speed_rad_s + 25.0) < nominal_snapshot.load_speed_rad_s)), "biased_snapshot.load_speed_rad_s + 25.0 < nominal_snapshot.load_speed_rad_s", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 85))));
    var dropout_input: pwr_shaft_input = nominal_input;
    _ = &dropout_input;
    dropout_input.fault_mask = @as(u32, @bitCast(PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT));
    {
        var i: u64 = 0;
        _ = &i;
        while (i < @as(u64, @bitCast(@as(u64, @as(c_uint, 100))))) : (i +%= 1) {
            power_assert(@intFromBool(!!(pwr_shaft_lab_step(&biased, &dropout_input) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_step(&biased, &dropout_input) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 90))));
        }
    }
    power_assert(@intFromBool(!!(pwr_shaft_lab_snapshot(&biased, &biased_snapshot) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_snapshot(&biased, &biased_snapshot) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 92))));
    power_assert(@intFromBool(!!!biased_snapshot.sensor_valid), "!biased_snapshot.sensor_valid", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 93))));
    power_assert(@intFromBool(!!(fabs(biased_snapshot.commanded_duty) < 0.000000000001)), "fabs(biased_snapshot.commanded_duty) < 1.0e-12", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 94))));
    power_assert(@intFromBool(!!((biased_snapshot.diagnostic_flags & @as(u32, @bitCast(PWR_SHAFT_DIAG_SENSOR_INVALID))) != @as(c_uint, 0))), "(biased_snapshot.diagnostic_flags & PWR_SHAFT_DIAG_SENSOR_INVALID) != 0U", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 95))));
    var brownout_input: pwr_shaft_input = nominal_input;
    _ = &brownout_input;
    brownout_input.fault_mask = @as(u32, @bitCast(PWR_SHAFT_FAULT_LV_BROWNOUT));
    {
        var i: u64 = 0;
        _ = &i;
        while (i < @as(u64, @bitCast(@as(u64, config.control_period_ticks +% @as(c_uint, 1))))) : (i +%= 1) {
            power_assert(@intFromBool(!!(pwr_shaft_lab_step(&nominal, &brownout_input) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_step(&nominal, &brownout_input) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 100))));
        }
    }
    power_assert(@intFromBool(!!(pwr_shaft_lab_snapshot(&nominal, &nominal_snapshot) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_snapshot(&nominal, &nominal_snapshot) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 102))));
    power_assert(@intFromBool(!!!nominal_snapshot.controller_active), "!nominal_snapshot.controller_active", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 103))));
    power_assert(@intFromBool(!!(fabs(nominal_snapshot.commanded_duty) < 0.000000000001)), "fabs(nominal_snapshot.commanded_duty) < 1.0e-12", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 104))));
    power_assert(@intFromBool(!!((nominal_snapshot.diagnostic_flags & @as(u32, @bitCast(PWR_SHAFT_DIAG_CONTROLLER_BROWNOUT))) != @as(c_uint, 0))), "(nominal_snapshot.diagnostic_flags & PWR_SHAFT_DIAG_CONTROLLER_BROWNOUT) != 0U", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 105))));
}

pub fn test_step_convergence() callconv(.c) void {
    var coarse_config: pwr_shaft_config = undefined;
    _ = &coarse_config;
    pwr_shaft_config_default(&coarse_config);
    var fine_config: pwr_shaft_config = coarse_config;
    _ = &fine_config;
    fine_config.base_tick_ns /= @as(u64, @bitCast(@as(u64, @as(c_uint, 2))));
    fine_config.sensor_period_ticks *%= @as(u32, @bitCast(@as(c_uint, 2)));
    fine_config.control_period_ticks *%= @as(u32, @bitCast(@as(c_uint, 2)));
    fine_config.signal_delay_ticks *%= @as(u32, @bitCast(@as(c_uint, 2)));
    fine_config.max_sensor_age_ticks *%= @as(u32, @bitCast(@as(c_uint, 2)));
    fine_config.controller_recovery_ticks *%= @as(u32, @bitCast(@as(c_uint, 2)));
    var coarse: pwr_shaft_lab = undefined;
    _ = &coarse;
    var fine: pwr_shaft_lab = undefined;
    _ = &fine;
    power_assert(@intFromBool(!!(pwr_shaft_lab_init(&coarse, &coarse_config) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_init(&coarse, &coarse_config) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 122))));
    power_assert(@intFromBool(!!(pwr_shaft_lab_init(&fine, &fine_config) == @as(c_uint, @bitCast(PWR_SHAFT_OK)))), "pwr_shaft_lab_init(&fine, &fine_config) == PWR_SHAFT_OK", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 123))));
    const coarse_snapshot: pwr_shaft_snapshot = run_nominal(&coarse, @as(u64, @bitCast(@as(u64, @as(c_uint, 20000)))), 140.0, 15.0);
    _ = &coarse_snapshot;
    const fine_snapshot: pwr_shaft_snapshot = run_nominal(&fine, @as(u64, @bitCast(@as(u64, @as(c_uint, 40000)))), 140.0, 15.0);
    _ = &fine_snapshot;
    const relative_speed_difference: f64 = fabs(coarse_snapshot.load_speed_rad_s - fine_snapshot.load_speed_rad_s) / fmax(fabs(fine_snapshot.load_speed_rad_s), 1.0);
    _ = &relative_speed_difference;
    power_assert(@intFromBool(!!(relative_speed_difference < 0.02)), "relative_speed_difference < 0.02", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 130))));
}

pub fn test_invalid_configuration() callconv(.c) void {
    var config: pwr_shaft_config = undefined;
    _ = &config;
    pwr_shaft_config_default(&config);
    config.motor_inertia_kg_m2 = 0.0;
    var lab: pwr_shaft_lab = undefined;
    _ = &lab;
    power_assert(@intFromBool(!!(pwr_shaft_lab_init(&lab, &config) == @as(c_uint, @bitCast(PWR_SHAFT_INVALID_CONFIG)))), "pwr_shaft_lab_init(&lab, &config) == PWR_SHAFT_INVALID_CONFIG", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 138))));
    power_assert(@intFromBool(!!(pwr_shaft_lab_init(null, &config) == @as(c_uint, @bitCast(PWR_SHAFT_INVALID_ARGUMENT)))), "pwr_shaft_lab_init(NULL, &config) == PWR_SHAFT_INVALID_ARGUMENT", "legacy/native/tests/test_shaft.c", @as(c_uint, @bitCast(@as(c_int, 139))));
}

pub fn run() c_int {
    test_invalid_configuration();
    test_nominal_and_deterministic();
    test_faults_close_the_loop();
    test_step_convergence();
    return 0;
}

pub fn main() void {
    @import("std").process.exit(@intCast(run()));
}
