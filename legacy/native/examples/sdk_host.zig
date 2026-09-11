// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/examples/c_host.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_BUILTIN_MODEL_CONTROLLED_SHAFT = @import("../src/abi.zig").PWR_BUILTIN_MODEL_CONTROLLED_SHAFT;
const PWR_STATUS_OK = @import("../src/abi.zig").PWR_STATUS_OK;
const fprintf = @import("../src/support.zig").fprintf;
const printf = @import("../src/support.zig").printf;
const pwr_api = @import("../src/abi.zig").pwr_api;
const pwr_capabilities = @import("../src/abi.zig").pwr_capabilities;
const pwr_channel_id = @import("../src/abi.zig").pwr_channel_id;
const pwr_context = @import("../src/abi.zig").pwr_context;
const pwr_context_create_fn = @import("../src/abi.zig").pwr_context_create_fn;
const pwr_context_desc = @import("../src/abi.zig").pwr_context_desc;
const pwr_context_destroy_fn = @import("../src/abi.zig").pwr_context_destroy_fn;
extern fn pwr_get_api(version: u32, size: u32, api: [*c]pwr_api) i32;
const pwr_input_frame = @import("../src/abi.zig").pwr_input_frame;
const pwr_instance = @import("../src/abi.zig").pwr_instance;
const pwr_instance_create_fn = @import("../src/abi.zig").pwr_instance_create_fn;
const pwr_instance_create_from_model_fn = @import("../src/abi.zig").pwr_instance_create_from_model_fn;
const pwr_instance_desc = @import("../src/abi.zig").pwr_instance_desc;
const pwr_instance_destroy_fn = @import("../src/abi.zig").pwr_instance_destroy_fn;
const pwr_instance_read_snapshot_fn = @import("../src/abi.zig").pwr_instance_read_snapshot_fn;
const pwr_instance_step_fn = @import("../src/abi.zig").pwr_instance_step_fn;
const pwr_instance_submit_inputs_fn = @import("../src/abi.zig").pwr_instance_submit_inputs_fn;
const pwr_model_compile_fn = @import("../src/abi.zig").pwr_model_compile_fn;
const pwr_model_destroy_fn = @import("../src/abi.zig").pwr_model_destroy_fn;
const pwr_model_get_channels_fn = @import("../src/abi.zig").pwr_model_get_channels_fn;
const pwr_model_get_info_fn = @import("../src/abi.zig").pwr_model_get_info_fn;
const pwr_runtime_get_capabilities_fn = @import("../src/abi.zig").pwr_runtime_get_capabilities_fn;
const pwr_runtime_get_version_fn = @import("../src/abi.zig").pwr_runtime_get_version_fn;
const pwr_scalar_value = @import("../src/abi.zig").pwr_scalar_value;
const pwr_snapshot = @import("../src/abi.zig").pwr_snapshot;
const pwr_status = @import("../src/abi.zig").pwr_status;
const pwr_status_string_fn = @import("../src/abi.zig").pwr_status_string_fn;
const pwr_version_info = @import("../src/abi.zig").pwr_version_info;

pub fn run() c_int {
    var api: pwr_api = pwr_api{
        .abi_version = @as(u32, @bitCast(@as(c_int, 0))),
        .struct_size = @import("std").mem.zeroes(u32),
        .runtime_get_version = @import("std").mem.zeroes(pwr_runtime_get_version_fn),
        .runtime_get_capabilities = @import("std").mem.zeroes(pwr_runtime_get_capabilities_fn),
        .context_create = @import("std").mem.zeroes(pwr_context_create_fn),
        .context_destroy = @import("std").mem.zeroes(pwr_context_destroy_fn),
        .status_string = @import("std").mem.zeroes(pwr_status_string_fn),
        .instance_create = @import("std").mem.zeroes(pwr_instance_create_fn),
        .instance_destroy = @import("std").mem.zeroes(pwr_instance_destroy_fn),
        .instance_submit_inputs = @import("std").mem.zeroes(pwr_instance_submit_inputs_fn),
        .instance_step = @import("std").mem.zeroes(pwr_instance_step_fn),
        .instance_read_snapshot = @import("std").mem.zeroes(pwr_instance_read_snapshot_fn),
        .model_compile = @import("std").mem.zeroes(pwr_model_compile_fn),
        .model_destroy = @import("std").mem.zeroes(pwr_model_destroy_fn),
        .model_get_info = @import("std").mem.zeroes(pwr_model_get_info_fn),
        .model_get_channels = @import("std").mem.zeroes(pwr_model_get_channels_fn),
        .instance_create_from_model = @import("std").mem.zeroes(pwr_instance_create_from_model_fn),
    };
    _ = &api;
    var version: pwr_version_info = pwr_version_info{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_version_info))))),
        .major = @as(c_uint, 0),
        .minor = @as(c_uint, 0),
        .patch = @as(c_uint, 0),
        .reserved = @as(c_uint, 0),
        .product_name = @as([*c]const u8, @ptrFromInt(@as(c_int, 0))),
        .version_string = @as([*c]const u8, @ptrFromInt(@as(c_int, 0))),
    };
    _ = &version;
    var capabilities: pwr_capabilities = pwr_capabilities{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_capabilities))))),
        .feature_bits = @as(u64, 0),
        .max_contexts = @as(c_uint, 0),
        .reserved = @as(c_uint, 0),
    };
    _ = &capabilities;
    var desc: pwr_context_desc = pwr_context_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_context_desc))))),
        .flags = @as(c_uint, 0),
        .reserved = @as(c_uint, 0),
    };
    _ = &desc;
    var instance_desc: pwr_instance_desc = pwr_instance_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_instance_desc))))),
        .model = @as(u32, @bitCast(PWR_BUILTIN_MODEL_CONTROLLED_SHAFT)),
        .flags = @as(c_uint, 0),
        .seed = @as(u64, 0),
    };
    _ = &instance_desc;
    var context: pwr_context = 0;
    _ = &context;
    var instance: pwr_instance = 0;
    _ = &instance;
    var input_values: [2]pwr_scalar_value = [2]pwr_scalar_value{
        pwr_scalar_value{
            .channel = @as(u64, 1),
            .value = 160.0,
            .flags = @as(c_uint, 0),
            .reserved = @as(c_uint, 0),
        },
        pwr_scalar_value{
            .channel = @as(u64, 2),
            .value = 20.0,
            .flags = @as(c_uint, 0),
            .reserved = @as(c_uint, 0),
        },
    };
    _ = &input_values;
    var input: pwr_input_frame = pwr_input_frame{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_input_frame))))),
        .values = @as([*c]const pwr_scalar_value, @ptrFromInt(@as(c_int, 0))),
        .value_count = @as(c_uint, 0),
        .flags = @as(c_uint, 0),
    };
    _ = &input;
    var output_values: [32]pwr_scalar_value = [1]pwr_scalar_value{
        pwr_scalar_value{
            .channel = @as(pwr_channel_id, @bitCast(@as(i64, @as(c_int, 0)))),
            .value = 0,
            .flags = @import("std").mem.zeroes(u32),
            .reserved = @import("std").mem.zeroes(u32),
        },
    } ++ [1]pwr_scalar_value{@import("std").mem.zeroes(pwr_scalar_value)} ** 31;
    _ = &output_values;
    var snapshot: pwr_snapshot = pwr_snapshot{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_snapshot))))),
        .simulation_time_ns = @as(u64, 0),
        .state_hash = @as(u64, 0),
        .diagnostic_bits = @as(u64, 0),
        .values = @as([*c]pwr_scalar_value, @ptrFromInt(@as(c_int, 0))),
        .value_capacity = @as(c_uint, 0),
        .value_count = @as(c_uint, 0),
    };
    _ = &snapshot;
    var status: pwr_status = undefined;
    _ = &status;
    status = pwr_get_api(@as(c_uint, 1), @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_api))))), &api);
    if (status != PWR_STATUS_OK) {
        _ = fprintf("pwr_get_api failed ({d})\n", .{status});
        return 1;
    }
    status = api.runtime_get_version.?(&version);
    if (status != PWR_STATUS_OK) {
        _ = fprintf("version query failed: {s}\n", .{@import("std").mem.span(api.status_string.?(status))});
        return 1;
    }
    status = api.runtime_get_capabilities.?(&capabilities);
    if (status != PWR_STATUS_OK) {
        _ = fprintf("capability query failed: {s}\n", .{@import("std").mem.span(api.status_string.?(status))});
        return 1;
    }
    status = api.context_create.?(&desc, &context);
    if (status != PWR_STATUS_OK) {
        _ = fprintf("context creation failed: {s}\n", .{@import("std").mem.span(api.status_string.?(status))});
        return 1;
    }
    _ = printf("{s} {s}, ABI {d}, context 0x{x}\n", .{ @import("std").mem.span(version.product_name), @import("std").mem.span(version.version_string), version.abi_version, context });
    _ = printf("capabilities 0x{x}, max contexts {d}\n", .{ capabilities.feature_bits, capabilities.max_contexts });
    status = api.instance_create.?(context, &instance_desc, &instance);
    if (status != PWR_STATUS_OK) {
        _ = fprintf("instance creation failed: {s}\n", .{@import("std").mem.span(api.status_string.?(status))});
        _ = api.context_destroy.?(context);
        return 1;
    }
    input.values = @as([*c]pwr_scalar_value, @ptrCast(@alignCast(&input_values[@as(usize, @intCast(0))])));
    input.value_count = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf([2]pwr_scalar_value) / @sizeOf(pwr_scalar_value)))));
    status = api.instance_submit_inputs.?(instance, &input);
    if (status == PWR_STATUS_OK) {
        status = api.instance_step.?(instance, @as(u64, 2000000000));
    }
    snapshot.values = @as([*c]pwr_scalar_value, @ptrCast(@alignCast(&output_values[@as(usize, @intCast(0))])));
    snapshot.value_capacity = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf([32]pwr_scalar_value) / @sizeOf(pwr_scalar_value)))));
    if (status == PWR_STATUS_OK) {
        status = api.instance_read_snapshot.?(instance, &snapshot);
    }
    if (status != PWR_STATUS_OK) {
        _ = fprintf("simulation failed: {s}\n", .{@import("std").mem.span(api.status_string.?(status))});
        _ = api.instance_destroy.?(instance);
        _ = api.context_destroy.?(context);
        return 1;
    }
    var load_speed: f64 = 0.0;
    _ = &load_speed;
    var energy_residual: f64 = 0.0;
    _ = &energy_residual;
    {
        var index: u32 = 0;
        _ = &index;
        while (index < snapshot.value_count) : (index +%= 1) {
            if (snapshot.values[index].channel == @as(u64, 1002)) {
                load_speed = snapshot.values[index].value;
            } else if (snapshot.values[index].channel == @as(u64, 1015)) {
                energy_residual = snapshot.values[index].value;
            }
        }
    }
    _ = printf("stepped {d} s, load speed {d} rad/s, residual {d} J, hash 0x{x}\n", .{ @as(f64, @floatFromInt(snapshot.simulation_time_ns)) * 0.000000001, load_speed, energy_residual, snapshot.state_hash });
    status = api.instance_destroy.?(instance);
    if (status != PWR_STATUS_OK) {
        _ = fprintf("instance destruction failed: {s}\n", .{@import("std").mem.span(api.status_string.?(status))});
        _ = api.context_destroy.?(context);
        return 1;
    }
    status = api.context_destroy.?(context);
    if (status != PWR_STATUS_OK) {
        _ = fprintf("context destruction failed: {s}\n", .{@import("std").mem.span(api.status_string.?(status))});
        return 1;
    }
    return 0;
}

pub fn main() void {
    @import("std").process.exit(@intCast(run()));
}
