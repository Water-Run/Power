// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/tests/test_model_sdk.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_CHANNEL_INPUT = @import("../src/abi.zig").PWR_CHANNEL_INPUT;
const PWR_COMPONENT_TORQUE_SOURCE = @import("../src/abi.zig").PWR_COMPONENT_TORQUE_SOURCE;
const PWR_FIELD_SPEED = @import("../src/abi.zig").PWR_FIELD_SPEED;
const PWR_MODEL_DIAG_RANGE = @import("../src/abi.zig").PWR_MODEL_DIAG_RANGE;
const PWR_MODEL_FIDELITY_LINEAR_LUMPED = @import("../src/abi.zig").PWR_MODEL_FIDELITY_LINEAR_LUMPED;
const PWR_MODEL_VALIDATION_UNVERIFIED = @import("../src/abi.zig").PWR_MODEL_VALIDATION_UNVERIFIED;
const PWR_NODE_ROTATIONAL = @import("../src/abi.zig").PWR_NODE_ROTATIONAL;
const PWR_STATUS_CAPACITY_EXCEEDED = @import("../src/abi.zig").PWR_STATUS_CAPACITY_EXCEEDED;
const PWR_STATUS_INVALID_ARGUMENT = @import("../src/abi.zig").PWR_STATUS_INVALID_ARGUMENT;
const PWR_STATUS_INVALID_HANDLE = @import("../src/abi.zig").PWR_STATUS_INVALID_HANDLE;
const PWR_STATUS_INVALID_MODEL = @import("../src/abi.zig").PWR_STATUS_INVALID_MODEL;
const PWR_STATUS_IN_USE = @import("../src/abi.zig").PWR_STATUS_IN_USE;
const PWR_STATUS_OK = @import("../src/abi.zig").PWR_STATUS_OK;
const PWR_STATUS_STRUCT_TOO_SMALL = @import("../src/abi.zig").PWR_STATUS_STRUCT_TOO_SMALL;
const PWR_STATUS_UNSUPPORTED_ABI = @import("../src/abi.zig").PWR_STATUS_UNSUPPORTED_ABI;
const PWR_UNIT_KG_M2 = @import("../src/abi.zig").PWR_UNIT_KG_M2;
const PWR_UNIT_NM = @import("../src/abi.zig").PWR_UNIT_NM;
const PWR_UNIT_RAD = @import("../src/abi.zig").PWR_UNIT_RAD;
const PWR_UNIT_RAD_S = @import("../src/abi.zig").PWR_UNIT_RAD_S;
const fabs = @import("../src/support.zig").fabs;
const fprintf = @import("../src/support.zig").fprintf;
const puts = @import("../src/support.zig").puts;
const pwr_api = @import("../src/abi.zig").pwr_api;
const pwr_capabilities = @import("../src/abi.zig").pwr_capabilities;
const pwr_channel_id = @import("../src/abi.zig").pwr_channel_id;
const pwr_channel_info = @import("../src/abi.zig").pwr_channel_info;
const pwr_component_desc = @import("../src/abi.zig").pwr_component_desc;
const pwr_component_parameters = @import("../src/abi.zig").pwr_component_parameters;
const pwr_context = @import("../src/abi.zig").pwr_context;
const pwr_context_create_fn = @import("../src/abi.zig").pwr_context_create_fn;
const pwr_context_desc = @import("../src/abi.zig").pwr_context_desc;
const pwr_context_destroy_fn = @import("../src/abi.zig").pwr_context_destroy_fn;
const pwr_get_api = @import("../src/sdk/pwr_api.zig").pwr_get_api;
const pwr_input_frame = @import("../src/abi.zig").pwr_input_frame;
const pwr_instance = @import("../src/abi.zig").pwr_instance;
const pwr_instance_create_fn = @import("../src/abi.zig").pwr_instance_create_fn;
const pwr_instance_create_from_model_fn = @import("../src/abi.zig").pwr_instance_create_from_model_fn;
const pwr_instance_destroy_fn = @import("../src/abi.zig").pwr_instance_destroy_fn;
const pwr_instance_read_snapshot_fn = @import("../src/abi.zig").pwr_instance_read_snapshot_fn;
const pwr_instance_step_fn = @import("../src/abi.zig").pwr_instance_step_fn;
const pwr_instance_submit_inputs_fn = @import("../src/abi.zig").pwr_instance_submit_inputs_fn;
const pwr_model = @import("../src/abi.zig").pwr_model;
const pwr_model_compile_fn = @import("../src/abi.zig").pwr_model_compile_fn;
const pwr_model_desc = @import("../src/abi.zig").pwr_model_desc;
const pwr_model_destroy_fn = @import("../src/abi.zig").pwr_model_destroy_fn;
const pwr_model_diagnostic = @import("../src/abi.zig").pwr_model_diagnostic;
const pwr_model_get_channels_fn = @import("../src/abi.zig").pwr_model_get_channels_fn;
const pwr_model_get_info_fn = @import("../src/abi.zig").pwr_model_get_info_fn;
const pwr_model_info = @import("../src/abi.zig").pwr_model_info;
const pwr_node_desc = @import("../src/abi.zig").pwr_node_desc;
const pwr_quantity = @import("../src/abi.zig").pwr_quantity;
const pwr_runtime_get_capabilities_fn = @import("../src/abi.zig").pwr_runtime_get_capabilities_fn;
const pwr_runtime_get_version_fn = @import("../src/abi.zig").pwr_runtime_get_version_fn;
const pwr_scalar_value = @import("../src/abi.zig").pwr_scalar_value;
const pwr_snapshot = @import("../src/abi.zig").pwr_snapshot;
const pwr_status = @import("../src/abi.zig").pwr_status;
const pwr_status_string_fn = @import("../src/abi.zig").pwr_status_string_fn;
const strcmp = @import("../src/support.zig").strcmp;

pub var failures: c_int = @import("std").mem.zeroes(c_int);

pub fn compile_rotor(arg_api: [*c]const pwr_api, arg_context: pwr_context, arg_model: [*c]pwr_model) callconv(.c) pwr_status {
    var api = arg_api;
    _ = &api;
    var context = arg_context;
    _ = &context;
    var model = arg_model;
    _ = &model;
    const node: pwr_node_desc = pwr_node_desc{
        .id = @as(u32, @bitCast(@as(c_int, 1))),
        .domain = @as(u32, @bitCast(PWR_NODE_ROTATIONAL)),
        .storage = pwr_quantity{
            .value = @as(f64, @floatFromInt(@as(c_int, 2))),
            .unit = @as(u32, @bitCast(PWR_UNIT_KG_M2)),
            .reserved = @as(c_uint, 0),
        },
        .initial = pwr_quantity{
            .value = @as(f64, @floatFromInt(@as(c_int, 0))),
            .unit = @as(u32, @bitCast(PWR_UNIT_RAD_S)),
            .reserved = @as(c_uint, 0),
        },
        .position = pwr_quantity{
            .value = @as(f64, @floatFromInt(@as(c_int, 0))),
            .unit = @as(u32, @bitCast(PWR_UNIT_RAD)),
            .reserved = @as(c_uint, 0),
        },
    };
    _ = &node;
    const component: pwr_component_desc = pwr_component_desc{
        .id = @as(u32, @bitCast(@as(c_int, 10))),
        .kind = @as(u32, @bitCast(PWR_COMPONENT_TORQUE_SOURCE)),
        .node_a = @as(u32, @bitCast(@as(c_int, 1))),
        .node_b = @import("std").mem.zeroes(u32),
        .heat_node = @import("std").mem.zeroes(u32),
        .reserved = @import("std").mem.zeroes(u32),
        .input_channel = @as(u64, @bitCast(@as(i64, @as(c_int, 20)))),
        .initial_input = pwr_quantity{
            .value = @as(f64, @floatFromInt(@as(c_int, 4))),
            .unit = @as(u32, @bitCast(PWR_UNIT_NM)),
            .reserved = @as(c_uint, 0),
        },
        .parameters = @import("std").mem.zeroes(pwr_component_parameters),
    };
    _ = &component;
    var desc: pwr_model_desc = pwr_model_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_desc))))),
        .schema_version = @as(c_uint, 1),
        .flags = @as(u32, @bitCast(@as(c_int, 0))),
        .step_ns = @as(u64, 100000),
        .nodes = @as([*c]const pwr_node_desc, @ptrFromInt(@as(c_int, 0))),
        .node_count = @as(u32, @bitCast(@as(c_int, 0))),
        .component_count = @as(u32, @bitCast(@as(c_int, 0))),
        .components = @as([*c]const pwr_component_desc, @ptrFromInt(@as(c_int, 0))),
    };
    _ = &desc;
    desc.nodes = &node;
    desc.node_count = 1;
    desc.components = &component;
    desc.component_count = 1;
    desc.step_ns = 1000000;
    return api.*.model_compile.?(context, &desc, model, null);
}

pub fn snapshot_hash(arg_api: [*c]const pwr_api, arg_instance: pwr_instance, arg_speed: [*c]f64) callconv(.c) u64 {
    var api = arg_api;
    _ = &api;
    var instance = arg_instance;
    _ = &instance;
    var speed = arg_speed;
    _ = &speed;
    var values: [6]pwr_scalar_value = undefined;
    _ = &values;
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
    snapshot.values = @as([*c]pwr_scalar_value, @ptrCast(@alignCast(&values[@as(usize, @intCast(0))])));
    snapshot.value_capacity = 6;
    while (true) {
        if (!(api.*.instance_read_snapshot.?(instance, &snapshot) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 38), "api->instance_read_snapshot(instance, &snapshot) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(snapshot.value_count == @as(c_uint, 6))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 39), "snapshot.value_count == 6U" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(values[@as(c_uint, @intCast(@as(c_int, 1)))].channel == ((@as(u64, 9223372036854775808) | (@as(u64, @bitCast(@as(i64, @as(c_int, 1)))) << @intCast(8))) | @as(u64, @bitCast(@as(i64, PWR_FIELD_SPEED)))))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 40), "values[1].channel == PWR_MODEL_CHANNEL(1, PWR_FIELD_SPEED)" });
            failures += 1;
        }
        if (!false) break;
    }
    speed.* = values[@as(c_uint, @intCast(@as(c_int, 1)))].value;
    return snapshot.state_hash;
}

pub fn test_public_workflow(arg_api: [*c]const pwr_api) callconv(.c) void {
    var api = arg_api;
    _ = &api;
    const desc: pwr_context_desc = pwr_context_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_context_desc))))),
        .flags = @as(c_uint, 0),
        .reserved = @as(c_uint, 0),
    };
    _ = &desc;
    var context: pwr_context = undefined;
    _ = &context;
    while (true) {
        if (!(api.*.context_create.?(&desc, &context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 49), "api->context_create(&desc, &context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    var model: pwr_model = undefined;
    _ = &model;
    while (true) {
        if (!(compile_rotor(api, context, &model) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 51), "compile_rotor(api, context, &model) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((model != @as(u64, 0)) and (model != context))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 52), "model != PWR_MODEL_INVALID && model != context" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(context) == PWR_STATUS_IN_USE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 53), "api->context_destroy(context) == PWR_STATUS_IN_USE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(model) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 54), "api->context_destroy(model) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_destroy.?(context) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 55), "api->model_destroy(context) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    var info: pwr_model_info = pwr_model_info{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_info))))),
        .fingerprint = @as(u64, @bitCast(@as(i64, @as(c_int, 0)))),
        .step_ns = @as(u64, @bitCast(@as(i64, @as(c_int, 0)))),
        .node_count = @as(u32, @bitCast(@as(c_int, 0))),
        .component_count = @as(u32, @bitCast(@as(c_int, 0))),
        .state_count = @as(u32, @bitCast(@as(c_int, 0))),
        .channel_count = @as(u32, @bitCast(@as(c_int, 0))),
        .fidelity = @as(u32, @bitCast(@as(c_int, 0))),
        .validation = @as(u32, @bitCast(@as(c_int, 0))),
    };
    _ = &info;
    while (true) {
        if (!(api.*.model_get_info.?(model, &info) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 57), "api->model_get_info(model, &info) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(((info.node_count == @as(c_uint, 1)) and (info.component_count == @as(c_uint, 1))) and (info.state_count == @as(c_uint, 2)))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 58), "info.node_count == 1U && info.component_count == 1U && info.state_count == 2U" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(info.fidelity == @as(u32, @bitCast(PWR_MODEL_FIDELITY_LINEAR_LUMPED)))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 59), "info.fidelity == PWR_MODEL_FIDELITY_LINEAR_LUMPED" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((info.validation == @as(u32, @bitCast(PWR_MODEL_VALIDATION_UNVERIFIED))) and (info.fingerprint != @as(u64, @bitCast(@as(u64, @as(c_uint, 0))))))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 60), "info.validation == PWR_MODEL_VALIDATION_UNVERIFIED && info.fingerprint != 0U" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((info.step_ns == @as(u64, 1000000)) and (info.channel_count == @as(c_uint, 7)))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 61), "info.step_ns == UINT64_C(1000000) && info.channel_count == 7U" });
            failures += 1;
        }
        if (!false) break;
    }
    var count: u32 = 0;
    _ = &count;
    while (true) {
        if (!(api.*.model_get_channels.?(model, null, @as(u32, @bitCast(@as(c_int, 0))), &count) == PWR_STATUS_CAPACITY_EXCEEDED)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 63), "api->model_get_channels(model, NULL, 0, &count) == PWR_STATUS_CAPACITY_EXCEEDED" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(count == @as(c_uint, 7))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 64), "count == 7U" });
            failures += 1;
        }
        if (!false) break;
    }
    var channels: [8]pwr_channel_info = [1]pwr_channel_info{
        pwr_channel_info{
            .channel = @as(u64, @bitCast(@as(i64, @as(c_int, 0)))),
            .object_id = @import("std").mem.zeroes(u32),
            .direction = @import("std").mem.zeroes(u32),
            .unit = @import("std").mem.zeroes(u32),
            .reserved = @import("std").mem.zeroes(u32),
            .quantity = @import("std").mem.zeroes([32]u8),
        },
    } ++ [1]pwr_channel_info{@import("std").mem.zeroes(pwr_channel_info)} ** 7;
    _ = &channels;
    channels[@as(c_uint, @intCast(@as(c_int, 0)))].channel = @as(u64, @bitCast(@as(i64, @as(c_int, 999))));
    channels[@as(c_uint, @intCast(@as(c_int, 7)))].channel = @as(u64, @bitCast(@as(i64, @as(c_int, 888))));
    while (true) {
        if (!(api.*.model_get_channels.?(model, @as([*c]pwr_channel_info, @ptrCast(@alignCast(&channels[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 1))), &count) == PWR_STATUS_CAPACITY_EXCEEDED)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 68), "api->model_get_channels(model, channels, 1, &count) == PWR_STATUS_CAPACITY_EXCEEDED" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(channels[@as(c_uint, @intCast(@as(c_int, 0)))].channel == @as(u64, @bitCast(@as(u64, @as(c_uint, 999)))))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 69), "channels[0].channel == 999U" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_get_channels.?(model, @as([*c]pwr_channel_info, @ptrCast(@alignCast(&channels[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 8))), &count) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 70), "api->model_get_channels(model, channels, 8, &count) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(channels[@as(c_uint, @intCast(@as(c_int, 7)))].channel == @as(u64, @bitCast(@as(u64, @as(c_uint, 888)))))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 71), "channels[7].channel == 888U" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((channels[@as(c_uint, @intCast(@as(c_int, 2)))].channel == @as(u64, @bitCast(@as(u64, @as(c_uint, 20))))) and (channels[@as(c_uint, @intCast(@as(c_int, 2)))].direction == @as(u32, @bitCast(PWR_CHANNEL_INPUT))))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 72), "channels[2].channel == 20U && channels[2].direction == PWR_CHANNEL_INPUT" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((channels[@as(c_uint, @intCast(@as(c_int, 2)))].unit == @as(u32, @bitCast(PWR_UNIT_NM))) and (strcmp(@as([*c]u8, @ptrCast(@alignCast(&channels[@as(c_uint, @intCast(@as(c_int, 2)))].quantity[@as(usize, @intCast(0))]))), "torque") == @as(c_int, 0)))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 73), "channels[2].unit == PWR_UNIT_NM && strcmp(channels[2].quantity, \"torque\") == 0" });
            failures += 1;
        }
        if (!false) break;
    }
    var a: pwr_instance = undefined;
    _ = &a;
    var b: pwr_instance = undefined;
    _ = &b;
    while (true) {
        if (!(api.*.instance_create_from_model.?(model, &a) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 76), "api->instance_create_from_model(model, &a) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_create_from_model.?(model, &b) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 77), "api->instance_create_from_model(model, &b) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((a != model) and (a != context))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 78), "a != model && a != context" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_destroy.?(model) == PWR_STATUS_IN_USE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 79), "api->model_destroy(model) == PWR_STATUS_IN_USE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(a) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 80), "api->context_destroy(a) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_destroy.?(model) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 81), "api->instance_destroy(model) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_step.?(context, @as(u64, @bitCast(@as(i64, @as(c_int, 1000000))))) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 82), "api->instance_step(context, 1000000) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_step.?(a, @as(u64, 1000000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 83), "api->instance_step(a, UINT64_C(1000000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < @as(c_uint, 10)) : (i +%= 1) {
            while (true) {
                if (!(api.*.instance_step.?(b, @as(u64, 100000000)) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 84), "api->instance_step(b, UINT64_C(100000000)) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
        }
    }
    var speed: f64 = undefined;
    _ = &speed;
    const hash_a: u64 = snapshot_hash(api, a, &speed);
    _ = &hash_a;
    while (true) {
        if (!(fabs(speed - 2.0) < 0.0000000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 87), "fabs(speed - 2.0) < 1.0e-10" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(hash_a == snapshot_hash(api, b, &speed))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 88), "hash_a == snapshot_hash(api, b, &speed)" });
            failures += 1;
        }
        if (!false) break;
    }
    const torque: pwr_scalar_value = pwr_scalar_value{
        .channel = @as(pwr_channel_id, @bitCast(@as(i64, @as(c_int, 20)))),
        .value = @as(f64, @floatFromInt(-@as(c_int, 4))),
        .flags = @as(u32, @bitCast(@as(c_int, 0))),
        .reserved = @as(u32, @bitCast(@as(c_int, 0))),
    };
    _ = &torque;
    var frame: pwr_input_frame = pwr_input_frame{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_input_frame))))),
        .values = @as([*c]const pwr_scalar_value, @ptrFromInt(@as(c_int, 0))),
        .value_count = @as(c_uint, 0),
        .flags = @as(c_uint, 0),
    };
    _ = &frame;
    frame.values = &torque;
    frame.value_count = 1;
    while (true) {
        if (!(api.*.instance_submit_inputs.?(a, &frame) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 92), "api->instance_submit_inputs(a, &frame) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(snapshot_hash(api, a, &speed) != hash_a)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 93), "snapshot_hash(api, a, &speed) != hash_a" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(snapshot_hash(api, b, &speed) == hash_a)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 94), "snapshot_hash(api, b, &speed) == hash_a" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_step.?(a, @as(u64, 1000000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 95), "api->instance_step(a, UINT64_C(1000000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    _ = snapshot_hash(api, a, &speed);
    while (true) {
        if (!(fabs(speed) < 0.0000000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 97), "fabs(speed) < 1.0e-10" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_destroy.?(a) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 98), "api->instance_destroy(a) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_destroy.?(model) == PWR_STATUS_IN_USE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 99), "api->model_destroy(model) == PWR_STATUS_IN_USE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_destroy.?(b) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 100), "api->instance_destroy(b) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_destroy.?(model) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 101), "api->model_destroy(model) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_destroy.?(model) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 102), "api->model_destroy(model) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_get_info.?(model, &info) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 103), "api->model_get_info(model, &info) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    var stale: pwr_instance = @as(u64, 18446744073709551615);
    _ = &stale;
    while (true) {
        if (!(api.*.instance_create_from_model.?(model, &stale) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 105), "api->instance_create_from_model(model, &stale) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(stale == @as(u64, 0))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 106), "stale == PWR_INSTANCE_INVALID" });
            failures += 1;
        }
        if (!false) break;
    }
    var next: pwr_model = undefined;
    _ = &next;
    while (true) {
        if (!((compile_rotor(api, context, &next) == PWR_STATUS_OK) and (next != model))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 108), "compile_rotor(api, context, &next) == PWR_STATUS_OK && next != model" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_get_info.?(next, &info) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 109), "api->model_get_info(next, &info) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_destroy.?(model) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 110), "api->model_destroy(model) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_destroy.?(next) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 111), "api->model_destroy(next) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 112), "api->context_destroy(context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_failure_contracts(arg_api: [*c]const pwr_api) callconv(.c) void {
    var api = arg_api;
    _ = &api;
    const cd: pwr_context_desc = pwr_context_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_context_desc))))),
        .flags = @as(c_uint, 0),
        .reserved = @as(c_uint, 0),
    };
    _ = &cd;
    var context: pwr_context = undefined;
    _ = &context;
    while (true) {
        if (!(api.*.context_create.?(&cd, &context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 119), "api->context_create(&cd, &context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    var model: pwr_model = @as(u64, 18446744073709551615);
    _ = &model;
    var diagnostic: pwr_model_diagnostic = pwr_model_diagnostic{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_diagnostic))))),
        .code = @as(u32, @bitCast(@as(c_int, 0))),
        .object_id = @as(u32, @bitCast(@as(c_int, 0))),
        .field = [1]u8{
            0,
        } ++ [1]u8{0} ** 39,
        .message = [1]u8{
            0,
        } ++ [1]u8{0} ** 159,
    };
    _ = &diagnostic;
    while (true) {
        if (!(api.*.model_compile.?(context, null, &model, &diagnostic) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 122), "api->model_compile(context, NULL, &model, &diagnostic) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(model == @as(u64, 0))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 123), "model == PWR_MODEL_INVALID" });
            failures += 1;
        }
        if (!false) break;
    }
    const node: pwr_node_desc = pwr_node_desc{
        .id = @as(u32, @bitCast(@as(c_int, 1))),
        .domain = @as(u32, @bitCast(PWR_NODE_ROTATIONAL)),
        .storage = pwr_quantity{
            .value = @as(f64, @floatFromInt(-@as(c_int, 1))),
            .unit = @as(u32, @bitCast(PWR_UNIT_KG_M2)),
            .reserved = @as(c_uint, 0),
        },
        .initial = pwr_quantity{
            .value = @as(f64, @floatFromInt(@as(c_int, 0))),
            .unit = @as(u32, @bitCast(PWR_UNIT_RAD_S)),
            .reserved = @as(c_uint, 0),
        },
        .position = pwr_quantity{
            .value = @as(f64, @floatFromInt(@as(c_int, 0))),
            .unit = @as(u32, @bitCast(PWR_UNIT_RAD)),
            .reserved = @as(c_uint, 0),
        },
    };
    _ = &node;
    var desc: pwr_model_desc = pwr_model_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_desc))))),
        .schema_version = @as(c_uint, 1),
        .flags = @as(u32, @bitCast(@as(c_int, 0))),
        .step_ns = @as(u64, 100000),
        .nodes = @as([*c]const pwr_node_desc, @ptrFromInt(@as(c_int, 0))),
        .node_count = @as(u32, @bitCast(@as(c_int, 0))),
        .component_count = @as(u32, @bitCast(@as(c_int, 0))),
        .components = @as([*c]const pwr_component_desc, @ptrFromInt(@as(c_int, 0))),
    };
    _ = &desc;
    desc.nodes = &node;
    desc.node_count = 1;
    const struct_unnamed_14 = extern struct {
        d: pwr_model_diagnostic = @import("std").mem.zeroes(pwr_model_diagnostic),
        canary: u64 = @import("std").mem.zeroes(u64),
    };
    _ = &struct_unnamed_14;
    var extended: struct_unnamed_14 = struct_unnamed_14{
        .d = pwr_model_diagnostic{
            .abi_version = @as(c_uint, 1),
            .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_diagnostic))))),
            .code = @as(u32, @bitCast(@as(c_int, 0))),
            .object_id = @as(u32, @bitCast(@as(c_int, 0))),
            .field = [1]u8{
                0,
            } ++ [1]u8{0} ** 39,
            .message = [1]u8{
                0,
            } ++ [1]u8{0} ** 159,
        },
        .canary = @as(u64, @bitCast(@as(i64, @as(c_int, 123)))),
    };
    _ = &extended;
    while (true) {
        if (!(api.*.model_compile.?(context, &desc, &model, &extended.d) == PWR_STATUS_INVALID_MODEL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 129), "api->model_compile(context, &desc, &model, &extended.d) == PWR_STATUS_INVALID_MODEL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((extended.d.code == @as(u32, @bitCast(PWR_MODEL_DIAG_RANGE))) and (extended.d.object_id == @as(c_uint, 1)))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 130), "extended.d.code == PWR_MODEL_DIAG_RANGE && extended.d.object_id == 1U" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((strcmp(@as([*c]u8, @ptrCast(@alignCast(&extended.d.field[@as(usize, @intCast(0))]))), "storage") == @as(c_int, 0)) and (extended.canary == @as(u64, @bitCast(@as(u64, @as(c_uint, 123))))))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 131), "strcmp(extended.d.field, \"storage\") == 0 && extended.canary == 123U" });
            failures += 1;
        }
        if (!false) break;
    }
    extended.d.struct_size = 8;
    while (true) {
        if (!(api.*.model_compile.?(context, &desc, &model, &extended.d) == PWR_STATUS_STRUCT_TOO_SMALL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 133), "api->model_compile(context, &desc, &model, &extended.d) == PWR_STATUS_STRUCT_TOO_SMALL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 134), "api->context_destroy(context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_create.?(&cd, &context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 136), "api->context_create(&cd, &context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(compile_rotor(api, context, &model) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 137), "compile_rotor(api, context, &model) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    var info: pwr_model_info = pwr_model_info{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_info))))),
        .fingerprint = @as(u64, @bitCast(@as(i64, @as(c_int, 0)))),
        .step_ns = @as(u64, @bitCast(@as(i64, @as(c_int, 0)))),
        .node_count = @as(u32, @bitCast(@as(c_int, 0))),
        .component_count = @as(u32, @bitCast(@as(c_int, 0))),
        .state_count = @as(u32, @bitCast(@as(c_int, 0))),
        .channel_count = @as(u32, @bitCast(@as(c_int, 0))),
        .fidelity = @as(u32, @bitCast(@as(c_int, 0))),
        .validation = @as(u32, @bitCast(@as(c_int, 0))),
    };
    _ = &info;
    info.struct_size = 8;
    while (true) {
        if (!(api.*.model_get_info.?(model, &info) == PWR_STATUS_STRUCT_TOO_SMALL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 140), "api->model_get_info(model, &info) == PWR_STATUS_STRUCT_TOO_SMALL" });
            failures += 1;
        }
        if (!false) break;
    }
    info.struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_info)))));
    info.abi_version = 99;
    while (true) {
        if (!(api.*.model_get_info.?(model, &info) == PWR_STATUS_UNSUPPORTED_ABI)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 142), "api->model_get_info(model, &info) == PWR_STATUS_UNSUPPORTED_ABI" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_get_info.?(model, null) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 143), "api->model_get_info(model, NULL) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    var count: u32 = undefined;
    _ = &count;
    while (true) {
        if (!(api.*.model_get_channels.?(model, null, @as(u32, @bitCast(@as(c_int, 1))), &count) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 145), "api->model_get_channels(model, NULL, 1, &count) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_get_channels.?(model, null, @as(u32, @bitCast(@as(c_int, 0))), null) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 146), "api->model_get_channels(model, NULL, 0, NULL) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_create_from_model.?(model, null) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 147), "api->instance_create_from_model(model, NULL) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.model_destroy.?(model) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 148), "api->model_destroy(model) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 149), "api->context_destroy(context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_registry_capacity(arg_api: [*c]const pwr_api) callconv(.c) void {
    var api = arg_api;
    _ = &api;
    const desc: pwr_context_desc = pwr_context_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_context_desc))))),
        .flags = @as(c_uint, 0),
        .reserved = @as(c_uint, 0),
    };
    _ = &desc;
    var context: pwr_context = undefined;
    _ = &context;
    while (true) {
        if (!(api.*.context_create.?(&desc, &context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 156), "api->context_create(&desc, &context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    var models: [64]pwr_model = undefined;
    _ = &models;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < @as(c_uint, 64)) : (i +%= 1) {
            while (true) {
                if (!(compile_rotor(api, context, &models[i]) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 158), "compile_rotor(api, context, &models[i]) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
        }
    }
    var overflow: pwr_model = @as(u64, 18446744073709551615);
    _ = &overflow;
    while (true) {
        if (!(compile_rotor(api, context, &overflow) == PWR_STATUS_CAPACITY_EXCEEDED)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 160), "compile_rotor(api, context, &overflow) == PWR_STATUS_CAPACITY_EXCEEDED" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(overflow == @as(u64, 0))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 161), "overflow == PWR_MODEL_INVALID" });
            failures += 1;
        }
        if (!false) break;
    }
    var instances: [256]pwr_instance = undefined;
    _ = &instances;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < @as(c_uint, 256)) : (i +%= 1) {
            while (true) {
                if (!(api.*.instance_create_from_model.?(models[@as(c_uint, @intCast(@as(c_int, 0)))], &instances[i]) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 164), "api->instance_create_from_model(models[0], &instances[i]) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
        }
    }
    var extra: pwr_instance = @as(u64, 18446744073709551615);
    _ = &extra;
    while (true) {
        if (!(api.*.instance_create_from_model.?(models[@as(c_uint, @intCast(@as(c_int, 0)))], &extra) == PWR_STATUS_CAPACITY_EXCEEDED)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 167), "api->instance_create_from_model(models[0], &extra) == PWR_STATUS_CAPACITY_EXCEEDED" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(extra == @as(u64, 0))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 168), "extra == PWR_INSTANCE_INVALID" });
            failures += 1;
        }
        if (!false) break;
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < @as(c_uint, 256)) : (i +%= 1) {
            while (true) {
                if (!(api.*.instance_destroy.?(instances[i]) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 169), "api->instance_destroy(instances[i]) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < @as(c_uint, 64)) : (i +%= 1) {
            while (true) {
                if (!(api.*.model_destroy.?(models[i]) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 170), "api->model_destroy(models[i]) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
        }
    }
    while (true) {
        if (!(api.*.context_destroy.?(context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 171), "api->context_destroy(context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_old_api_prefix() callconv(.c) void {
    const struct_old_api = extern struct {
        abi_version: u32 = @import("std").mem.zeroes(u32),
        struct_size: u32 = @import("std").mem.zeroes(u32),
        runtime_get_version: pwr_runtime_get_version_fn = @import("std").mem.zeroes(pwr_runtime_get_version_fn),
        runtime_get_capabilities: pwr_runtime_get_capabilities_fn = @import("std").mem.zeroes(pwr_runtime_get_capabilities_fn),
        context_create: pwr_context_create_fn = @import("std").mem.zeroes(pwr_context_create_fn),
        context_destroy: pwr_context_destroy_fn = @import("std").mem.zeroes(pwr_context_destroy_fn),
        status_string: pwr_status_string_fn = @import("std").mem.zeroes(pwr_status_string_fn),
        instance_create: pwr_instance_create_fn = @import("std").mem.zeroes(pwr_instance_create_fn),
        instance_destroy: pwr_instance_destroy_fn = @import("std").mem.zeroes(pwr_instance_destroy_fn),
        instance_submit_inputs: pwr_instance_submit_inputs_fn = @import("std").mem.zeroes(pwr_instance_submit_inputs_fn),
        instance_step: pwr_instance_step_fn = @import("std").mem.zeroes(pwr_instance_step_fn),
        instance_read_snapshot: pwr_instance_read_snapshot_fn = @import("std").mem.zeroes(pwr_instance_read_snapshot_fn),
    };
    _ = &struct_old_api;
    const old_api = struct_old_api;
    _ = &old_api;
    const struct_unnamed_15 = extern struct {
        api: old_api = @import("std").mem.zeroes(old_api),
        canary: u64 = @import("std").mem.zeroes(u64),
    };
    _ = &struct_unnamed_15;
    var caller: struct_unnamed_15 = struct_unnamed_15{
        .api = old_api{
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
        },
        .canary = @as(u64, 1311768467463790320),
    };
    _ = &caller;
    while (true) {
        if (!(pwr_get_api(@as(c_uint, 1), @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(old_api))))), @as([*c]pwr_api, @ptrCast(@alignCast(@as(?*anyopaque, @ptrCast(&caller.api)))))) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 193), "pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(caller.api), (pwr_api *)(void *)&caller.api) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(caller.canary == @as(u64, 1311768467463790320))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 194), "caller.canary == UINT64_C(0x123456789abcdef0)" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((caller.api.instance_step != null) and (caller.api.instance_read_snapshot != null))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 195), "caller.api.instance_step != NULL && caller.api.instance_read_snapshot != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    var desc: pwr_context_desc = pwr_context_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_context_desc))))),
        .flags = @as(c_uint, 0),
        .reserved = @as(c_uint, 0),
    };
    _ = &desc;
    var context: pwr_context = undefined;
    _ = &context;
    while (true) {
        if (!(caller.api.context_create.?(&desc, &context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 198), "caller.api.context_create(&desc, &context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(caller.api.context_destroy.?(context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 199), "caller.api.context_destroy(context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
}

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
    while (true) {
        if (!(pwr_get_api(@as(c_uint, 1), @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_api))))), &api) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 205), "pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(api), &api) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    var capabilities: pwr_capabilities = pwr_capabilities{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_capabilities))))),
        .feature_bits = @as(u64, 0),
        .max_contexts = @as(c_uint, 0),
        .reserved = @as(c_uint, 0),
    };
    _ = &capabilities;
    while (true) {
        if (!(api.runtime_get_capabilities.?(&capabilities) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 207), "api.runtime_get_capabilities(&capabilities) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((capabilities.feature_bits & (@as(u64, 1) << @intCast(6))) != @as(u64, @bitCast(@as(u64, @as(c_uint, 0)))))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 208), "(capabilities.feature_bits & PWR_CAP_COMPILED_MODELS) != 0U" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((capabilities.feature_bits & (@as(u64, 1) << @intCast(7))) != @as(u64, @bitCast(@as(u64, @as(c_uint, 0)))))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_model_sdk.c", @as(c_int, 209), "(capabilities.feature_bits & PWR_CAP_MODEL_CHANNEL_DISCOVERY) != 0U" });
            failures += 1;
        }
        if (!false) break;
    }
    test_old_api_prefix();
    test_public_workflow(&api);
    test_failure_contracts(&api);
    test_registry_capacity(&api);
    if (failures != @as(c_int, 0)) {
        return 1;
    }
    _ = puts("Power! model SDK tests passed");
    return 0;
}

pub fn main() void {
    @import("std").process.exit(@intCast(run()));
}
