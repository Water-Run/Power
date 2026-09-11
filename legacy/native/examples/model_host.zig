// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from examples/model_host.c at c342d4c; cleanup uses Zig defer.
const std = @import("std");
const abi = @import("../src/abi.zig");
extern fn pwr_get_api(version: u32, size: u32, api: *abi.pwr_api) abi.pwr_status;

fn check(status: abi.pwr_status) !void {
    if (status != abi.PWR_STATUS_OK) {
        std.debug.print("Native API status: {d}\n", .{status});
        return error.NativeOperationFailed;
    }
}

pub fn main() !void {
    var api: abi.pwr_api = .{};
    try check(pwr_get_api(abi.PWR_ABI_VERSION, @sizeOf(abi.pwr_api), &api));
    var context: abi.pwr_context = 0;
    const context_desc: abi.pwr_context_desc = .{ .abi_version = abi.PWR_ABI_VERSION, .struct_size = @sizeOf(abi.pwr_context_desc) };
    try check(api.context_create.?(&context_desc, &context));
    defer check(api.context_destroy.?(context)) catch @panic("Context cleanup failed.");

    const rotor: abi.pwr_node_desc = .{
        .id = 1,
        .domain = abi.PWR_NODE_ROTATIONAL,
        .storage = .{ .value = 2, .unit = abi.PWR_UNIT_KG_M2 },
        .initial = .{ .value = 0, .unit = abi.PWR_UNIT_RAD_S },
        .position = .{ .value = 0, .unit = abi.PWR_UNIT_RAD },
    };
    const torque: abi.pwr_component_desc = .{
        .id = 10,
        .kind = abi.PWR_COMPONENT_TORQUE_SOURCE,
        .node_a = 1,
        .input_channel = 20,
        .initial_input = .{ .value = 4, .unit = abi.PWR_UNIT_NM },
    };
    const desc: abi.pwr_model_desc = .{
        .abi_version = abi.PWR_ABI_VERSION,
        .struct_size = @sizeOf(abi.pwr_model_desc),
        .schema_version = abi.PWR_MODEL_SCHEMA_VERSION,
        .step_ns = 100000,
        .nodes = &rotor,
        .node_count = 1,
        .components = &torque,
        .component_count = 1,
    };
    var diagnostic: abi.pwr_model_diagnostic = .{ .abi_version = abi.PWR_ABI_VERSION, .struct_size = @sizeOf(abi.pwr_model_diagnostic) };
    var model: abi.pwr_model = 0;
    const status = api.model_compile.?(context, &desc, &model, &diagnostic);
    if (status != abi.PWR_STATUS_OK) std.debug.print("Object {d}, {s}: {s}\n", .{
        diagnostic.object_id, std.mem.sliceTo(&diagnostic.field, 0), std.mem.sliceTo(&diagnostic.message, 0),
    });
    try check(status);
    defer check(api.model_destroy.?(model)) catch @panic("Model cleanup failed.");

    var info: abi.pwr_model_info = .{ .abi_version = abi.PWR_ABI_VERSION, .struct_size = @sizeOf(abi.pwr_model_info) };
    try check(api.model_get_info.?(model, &info));
    var instance: abi.pwr_instance = 0;
    try check(api.instance_create_from_model.?(model, &instance));
    defer check(api.instance_destroy.?(instance)) catch @panic("Instance cleanup failed.");
    try check(api.instance_step.?(instance, 1000000000));
    var values: [6]abi.pwr_scalar_value = undefined;
    var snapshot: abi.pwr_snapshot = .{
        .abi_version = abi.PWR_ABI_VERSION,
        .struct_size = @sizeOf(abi.pwr_snapshot),
        .values = &values,
        .value_capacity = values.len,
    };
    try check(api.instance_read_snapshot.?(instance, &snapshot));
    _ = @import("../src/support.zig").printf(
        "{{\"model_fingerprint\":\"{x:0>16}\",\"time_ns\":{d},\"speed_rad_s\":{d},\"energy_residual_j\":{d},\"state_hash\":\"{x:0>16}\"}}\n",
        .{ info.fingerprint, snapshot.simulation_time_ns, values[1].value, values[5].value, snapshot.state_hash },
    );
}
