// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

const std = @import("std");
const abi = @import("../src/abi.zig");
const apt = @import("../src/models/powertrain/pwr_automatic_powertrain.zig");

fn input() abi.pwr_automatic_powertrain_input {
    return .{
        .control_mode = abi.PWR_AUTOMATIC_POWERTRAIN_CONTROL_SUPERVISED,
        .driver_throttle = 0.52,
        .ambient_pressure_pa = 101325,
        .ambient_temperature_k = 298.15,
        .electrical_supply_voltage_v = 13.6,
        .tailpipe_area_scale = 1,
        .ignition_on = true,
        .selector_drive = true,
    };
}

test "automatic powertrain replay, halfshaft energy and brownout" {
    // All defaults, including gear ratios, are synthetic research parameters.
    var config: abi.pwr_automatic_powertrain_config = undefined;
    apt.pwr_automatic_powertrain_config_default(&config);
    var first: abi.pwr_automatic_powertrain_state = undefined;
    try std.testing.expectEqual(@as(c_uint, @intCast(abi.PWR_AUTOMATIC_POWERTRAIN_OK)), apt.pwr_automatic_powertrain_init(&first, &config));
    var replay = first;
    var output: abi.pwr_automatic_powertrain_output = undefined;
    var replay_output: abi.pwr_automatic_powertrain_output = undefined;
    var commands = input();
    var saw_starter = false;
    var saw_motion = false;
    for (0..30000) |tick| {
        if (tick == 20000) commands.electrical_supply_voltage_v = 6;
        try std.testing.expectEqual(@as(c_uint, @intCast(abi.PWR_AUTOMATIC_POWERTRAIN_OK)), apt.pwr_automatic_powertrain_step(&first, &commands, &output));
        try std.testing.expectEqual(@as(c_uint, @intCast(abi.PWR_AUTOMATIC_POWERTRAIN_OK)), apt.pwr_automatic_powertrain_step(&replay, &commands, &replay_output));
        try std.testing.expectEqual(output.state_hash, replay_output.state_hash);
        try std.testing.expectEqual(@as(u64, tick + 1), output.tick);
        try std.testing.expect(std.math.isFinite(output.engine.crank_speed_rad_s));
        try std.testing.expectApproxEqAbs(@as(f64, 0), output.halfshaft_energy_residual_j, 1e-8 * @max(@as(f64, 1), @abs(output.cumulative_halfshaft_drive_work_j)));
        try std.testing.expectApproxEqAbs(@as(f64, 0), output.carrier_constraint_residual_rad_s, 1e-10);
        saw_starter = saw_starter or output.commanded_starter_torque_nm > 0;
        saw_motion = saw_motion or output.left_halfshaft_speed_rad_s > 0.01;
    }
    try std.testing.expect(saw_starter and saw_motion);
    try std.testing.expect(!output.ecu_powered and !output.tcu_powered);
    try std.testing.expect(output.diagnostic_flags & abi.PWR_AUTOMATIC_POWERTRAIN_DIAG_TCU_BROWNOUT != 0);
}

test "automatic powertrain rejects invalid parameters and rolls back input failures" {
    var config: abi.pwr_automatic_powertrain_config = undefined;
    apt.pwr_automatic_powertrain_config_default(&config);
    config.final_drive_ratio = 0;
    try std.testing.expectEqual(@as(c_uint, @intCast(abi.PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG)), apt.pwr_automatic_powertrain_validate_config(&config));
    apt.pwr_automatic_powertrain_config_default(&config);
    var state: abi.pwr_automatic_powertrain_state = undefined;
    try std.testing.expectEqual(@as(c_uint, @intCast(abi.PWR_AUTOMATIC_POWERTRAIN_OK)), apt.pwr_automatic_powertrain_init(&state, &config));
    var output: abi.pwr_automatic_powertrain_output = undefined;
    var commands = input();
    try std.testing.expectEqual(@as(c_uint, @intCast(abi.PWR_AUTOMATIC_POWERTRAIN_OK)), apt.pwr_automatic_powertrain_step(&state, &commands, &output));
    const before = state;
    const before_output = output;
    commands.driver_throttle = std.math.nan(f64);
    try std.testing.expectEqual(@as(c_uint, @intCast(abi.PWR_AUTOMATIC_POWERTRAIN_INVALID_INPUT)), apt.pwr_automatic_powertrain_step(&state, &commands, &output));
    try std.testing.expectEqualDeep(before, state);
    try std.testing.expectEqualDeep(before_output, output);
}
