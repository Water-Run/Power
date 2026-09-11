// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/tests/test_sdk.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_BUILTIN_MODEL_CONTROLLED_SHAFT = @import("../src/abi.zig").PWR_BUILTIN_MODEL_CONTROLLED_SHAFT;
const PWR_STATUS_CAPACITY_EXCEEDED = @import("../src/abi.zig").PWR_STATUS_CAPACITY_EXCEEDED;
const PWR_STATUS_INVALID_ARGUMENT = @import("../src/abi.zig").PWR_STATUS_INVALID_ARGUMENT;
const PWR_STATUS_INVALID_HANDLE = @import("../src/abi.zig").PWR_STATUS_INVALID_HANDLE;
const PWR_STATUS_INVALID_TIME_STEP = @import("../src/abi.zig").PWR_STATUS_INVALID_TIME_STEP;
const PWR_STATUS_IN_USE = @import("../src/abi.zig").PWR_STATUS_IN_USE;
const PWR_STATUS_OK = @import("../src/abi.zig").PWR_STATUS_OK;
const PWR_STATUS_STRUCT_TOO_SMALL = @import("../src/abi.zig").PWR_STATUS_STRUCT_TOO_SMALL;
const PWR_STATUS_UNKNOWN_CHANNEL = @import("../src/abi.zig").PWR_STATUS_UNKNOWN_CHANNEL;
const PWR_STATUS_UNSUPPORTED = @import("../src/abi.zig").PWR_STATUS_UNSUPPORTED;
const PWR_STATUS_UNSUPPORTED_ABI = @import("../src/abi.zig").PWR_STATUS_UNSUPPORTED_ABI;
const fprintf = @import("../src/support.zig").fprintf;
const puts = @import("../src/support.zig").puts;
const pwr_api = @import("../src/abi.zig").pwr_api;
const pwr_capabilities = @import("../src/abi.zig").pwr_capabilities;
const pwr_channel_id = @import("../src/abi.zig").pwr_channel_id;
const pwr_context = @import("../src/abi.zig").pwr_context;
const pwr_context_create_fn = @import("../src/abi.zig").pwr_context_create_fn;
const pwr_context_desc = @import("../src/abi.zig").pwr_context_desc;
const pwr_context_destroy_fn = @import("../src/abi.zig").pwr_context_destroy_fn;
const pwr_get_api = @import("../src/sdk/pwr_api.zig").pwr_get_api;
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
const pwr_status_string_fn = @import("../src/abi.zig").pwr_status_string_fn;
const pwr_version_info = @import("../src/abi.zig").pwr_version_info;
const strcmp = @import("../src/support.zig").strcmp;
const struct_pwr_api = @import("../src/abi.zig").struct_pwr_api;

pub var failures: c_int = @import("std").mem.zeroes(c_int);

pub fn test_api_negotiation() callconv(.c) void {
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
    const struct_extended_api_buffer = extern struct {
        api: pwr_api = @import("std").mem.zeroes(pwr_api),
        canary: u64 = @import("std").mem.zeroes(u64),
    };
    _ = &struct_extended_api_buffer;
    var extended: struct_extended_api_buffer = struct_extended_api_buffer{
        .api = pwr_api{
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
        },
        .canary = @as(u64, 6510698340921550245),
    };
    _ = &extended;
    while (true) {
        if (!(pwr_get_api(@as(c_uint, 1), @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_api))))), null) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 31), "pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(api), NULL) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_get_api(@as(c_uint, 1) +% @as(c_uint, 1), @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_api))))), &api) == PWR_STATUS_UNSUPPORTED_ABI)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 34), "pwr_get_api(PWR_ABI_VERSION + UINT32_C(1), (uint32_t)sizeof(api), &api) == PWR_STATUS_UNSUPPORTED_ABI" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_get_api(@as(c_uint, 1), @as(u32, @bitCast(@as(c_uint, @truncate(@offsetOf(struct_pwr_api, "status_string"))))), &api) == PWR_STATUS_STRUCT_TOO_SMALL)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 37), "pwr_get_api(PWR_ABI_VERSION, (uint32_t)offsetof(pwr_api, status_string), &api) == PWR_STATUS_STRUCT_TOO_SMALL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_get_api(@as(c_uint, 1), @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_api))))), &api) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 39), "pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(api), &api) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.abi_version == @as(c_uint, 1))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 40), "api.abi_version == PWR_ABI_VERSION" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.struct_size == @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_api))))))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 41), "api.struct_size == (uint32_t)sizeof(api)" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.runtime_get_version != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 42), "api.runtime_get_version != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.runtime_get_capabilities != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 43), "api.runtime_get_capabilities != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.context_create != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 44), "api.context_create != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.context_destroy != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 45), "api.context_destroy != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.status_string != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 46), "api.status_string != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.instance_create != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 47), "api.instance_create != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.instance_destroy != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 48), "api.instance_destroy != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.instance_submit_inputs != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 49), "api.instance_submit_inputs != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.instance_step != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 50), "api.instance_step != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.instance_read_snapshot != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 51), "api.instance_read_snapshot != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_get_api(@as(c_uint, 1), @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(struct_extended_api_buffer))))), &extended.api) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 54), "pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(extended), &extended.api) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(extended.canary == @as(u64, 6510698340921550245))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 55), "extended.canary == UINT64_C(0x5a5aa5a55a5aa5a5)" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_runtime_queries(arg_api: [*c]const pwr_api) callconv(.c) void {
    var api = arg_api;
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
    while (true) {
        if (!(api.*.runtime_get_version.?(null) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 63), "api->runtime_get_version(NULL) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    version.abi_version = @as(c_uint, 1) +% @as(c_uint, 1);
    while (true) {
        if (!(api.*.runtime_get_version.?(&version) == PWR_STATUS_UNSUPPORTED_ABI)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 65), "api->runtime_get_version(&version) == PWR_STATUS_UNSUPPORTED_ABI" });
            failures += 1;
        }
        if (!false) break;
    }
    version.abi_version = 1;
    version.struct_size = 1;
    while (true) {
        if (!(api.*.runtime_get_version.?(&version) == PWR_STATUS_STRUCT_TOO_SMALL)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 68), "api->runtime_get_version(&version) == PWR_STATUS_STRUCT_TOO_SMALL" });
            failures += 1;
        }
        if (!false) break;
    }
    version.struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_version_info)))));
    while (true) {
        if (!(api.*.runtime_get_version.?(&version) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 70), "api->runtime_get_version(&version) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(version.major == @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 71), "version.major == PWR_VERSION_MAJOR" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(version.minor == @as(c_uint, 2))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 72), "version.minor == PWR_VERSION_MINOR" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(version.patch == @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 73), "version.patch == PWR_VERSION_PATCH" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(version.product_name != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 74), "version.product_name != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(strcmp(version.product_name, "Power!") == @as(c_int, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 75), "strcmp(version.product_name, \"Power!\") == 0" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(version.version_string != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 76), "version.version_string != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.runtime_get_capabilities.?(null) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 78), "api->runtime_get_capabilities(NULL) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    capabilities.abi_version = @as(c_uint, 1) +% @as(c_uint, 1);
    while (true) {
        if (!(api.*.runtime_get_capabilities.?(&capabilities) == PWR_STATUS_UNSUPPORTED_ABI)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 81), "api->runtime_get_capabilities(&capabilities) == PWR_STATUS_UNSUPPORTED_ABI" });
            failures += 1;
        }
        if (!false) break;
    }
    capabilities.abi_version = 1;
    capabilities.struct_size = 1;
    while (true) {
        if (!(api.*.runtime_get_capabilities.?(&capabilities) == PWR_STATUS_STRUCT_TOO_SMALL)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 85), "api->runtime_get_capabilities(&capabilities) == PWR_STATUS_STRUCT_TOO_SMALL" });
            failures += 1;
        }
        if (!false) break;
    }
    capabilities.struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_capabilities)))));
    while (true) {
        if (!(api.*.runtime_get_capabilities.?(&capabilities) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 87), "api->runtime_get_capabilities(&capabilities) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((capabilities.feature_bits & (@as(u64, 1) << @intCast(0))) != @as(u64, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 89), "(capabilities.feature_bits & PWR_CAP_CONTEXT_LIFECYCLE) != UINT64_C(0)" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((capabilities.feature_bits & (@as(u64, 1) << @intCast(1))) != @as(u64, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 91), "(capabilities.feature_bits & PWR_CAP_GENERATION_HANDLES) != UINT64_C(0)" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((capabilities.feature_bits & (@as(u64, 1) << @intCast(3))) != @as(u64, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 93), "(capabilities.feature_bits & PWR_CAP_HEADLESS_FIXED_STEP) != UINT64_C(0)" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((capabilities.feature_bits & (@as(u64, 1) << @intCast(4))) != @as(u64, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 95), "(capabilities.feature_bits & PWR_CAP_BATCH_SCALAR_IO) != UINT64_C(0)" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(capabilities.max_contexts > @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 96), "capabilities.max_contexts > UINT32_C(0)" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(strcmp(api.*.status_string.?(PWR_STATUS_INVALID_HANDLE), "invalid or stale handle") == @as(c_int, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 99), "strcmp(api->status_string(PWR_STATUS_INVALID_HANDLE), \"invalid or stale handle\") == 0" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(strcmp(api.*.status_string.?(@as(c_int, 123456)), "unknown status") == @as(c_int, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 100), "strcmp(api->status_string(INT32_C(123456)), \"unknown status\") == 0" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn find_value(arg_snapshot: [*c]const pwr_snapshot, arg_channel: pwr_channel_id) callconv(.c) [*c]const pwr_scalar_value {
    var snapshot = arg_snapshot;
    _ = &snapshot;
    var channel = arg_channel;
    _ = &channel;
    var index: u32 = undefined;
    _ = &index;
    {
        index = 0;
        while (index < snapshot.*.value_count) : (index +%= 1) {
            if (snapshot.*.values[index].channel == channel) {
                return &snapshot.*.values[index];
            }
        }
    }
    return null;
}

pub fn test_instance_lifecycle_and_step(arg_api: [*c]const pwr_api) callconv(.c) void {
    var api = arg_api;
    _ = &api;
    var context_desc: pwr_context_desc = pwr_context_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_context_desc))))),
        .flags = @as(c_uint, 0),
        .reserved = @as(c_uint, 0),
    };
    _ = &context_desc;
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
    var first: pwr_instance = 0;
    _ = &first;
    var second: pwr_instance = 0;
    _ = &second;
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
    var first_values: [32]pwr_scalar_value = [1]pwr_scalar_value{
        pwr_scalar_value{
            .channel = @as(pwr_channel_id, @bitCast(@as(i64, @as(c_int, 0)))),
            .value = 0,
            .flags = @import("std").mem.zeroes(u32),
            .reserved = @import("std").mem.zeroes(u32),
        },
    } ++ [1]pwr_scalar_value{@import("std").mem.zeroes(pwr_scalar_value)} ** 31;
    _ = &first_values;
    var second_values: [32]pwr_scalar_value = [1]pwr_scalar_value{
        pwr_scalar_value{
            .channel = @as(pwr_channel_id, @bitCast(@as(i64, @as(c_int, 0)))),
            .value = 0,
            .flags = @import("std").mem.zeroes(u32),
            .reserved = @import("std").mem.zeroes(u32),
        },
    } ++ [1]pwr_scalar_value{@import("std").mem.zeroes(pwr_scalar_value)} ** 31;
    _ = &second_values;
    var first_snapshot: pwr_snapshot = pwr_snapshot{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_snapshot))))),
        .simulation_time_ns = @as(u64, 0),
        .state_hash = @as(u64, 0),
        .diagnostic_bits = @as(u64, 0),
        .values = @as([*c]pwr_scalar_value, @ptrFromInt(@as(c_int, 0))),
        .value_capacity = @as(c_uint, 0),
        .value_count = @as(c_uint, 0),
    };
    _ = &first_snapshot;
    var second_snapshot: pwr_snapshot = pwr_snapshot{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_snapshot))))),
        .simulation_time_ns = @as(u64, 0),
        .state_hash = @as(u64, 0),
        .diagnostic_bits = @as(u64, 0),
        .values = @as([*c]pwr_scalar_value, @ptrFromInt(@as(c_int, 0))),
        .value_capacity = @as(c_uint, 0),
        .value_count = @as(c_uint, 0),
    };
    _ = &second_snapshot;
    var speed: [*c]const pwr_scalar_value = undefined;
    _ = &speed;
    while (true) {
        if (!(api.*.context_create.?(&context_desc, &context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 134), "api->context_create(&context_desc, &context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_create.?(context, null, &first) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 136), "api->instance_create(context, NULL, &first) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    instance_desc.model = 999;
    while (true) {
        if (!(api.*.instance_create.?(context, &instance_desc, &first) == PWR_STATUS_UNSUPPORTED)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 139), "api->instance_create(context, &instance_desc, &first) == PWR_STATUS_UNSUPPORTED" });
            failures += 1;
        }
        if (!false) break;
    }
    instance_desc.model = @as(u32, @bitCast(PWR_BUILTIN_MODEL_CONTROLLED_SHAFT));
    while (true) {
        if (!(api.*.instance_create.?(context, &instance_desc, &first) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 142), "api->instance_create(context, &instance_desc, &first) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_create.?(context, &instance_desc, &second) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 144), "api->instance_create(context, &instance_desc, &second) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(context) == PWR_STATUS_IN_USE)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 145), "api->context_destroy(context) == PWR_STATUS_IN_USE" });
            failures += 1;
        }
        if (!false) break;
    }
    input.values = @as([*c]pwr_scalar_value, @ptrCast(@alignCast(&input_values[@as(usize, @intCast(0))])));
    input.value_count = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf([2]pwr_scalar_value) / @sizeOf(pwr_scalar_value)))));
    while (true) {
        if (!(api.*.instance_submit_inputs.?(first, &input) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 149), "api->instance_submit_inputs(first, &input) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_submit_inputs.?(second, &input) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 150), "api->instance_submit_inputs(second, &input) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_step.?(first, @as(u64, 1)) == PWR_STATUS_INVALID_TIME_STEP)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 152), "api->instance_step(first, UINT64_C(1)) == PWR_STATUS_INVALID_TIME_STEP" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_step.?(first, @as(u64, 3000000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 153), "api->instance_step(first, UINT64_C(3000000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_step.?(second, @as(u64, 3000000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 154), "api->instance_step(second, UINT64_C(3000000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_read_snapshot.?(first, &first_snapshot) == PWR_STATUS_CAPACITY_EXCEEDED)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 157), "api->instance_read_snapshot(first, &first_snapshot) == PWR_STATUS_CAPACITY_EXCEEDED" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(first_snapshot.value_count > @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 158), "first_snapshot.value_count > UINT32_C(0)" });
            failures += 1;
        }
        if (!false) break;
    }
    first_snapshot.values = @as([*c]pwr_scalar_value, @ptrCast(@alignCast(&first_values[@as(usize, @intCast(0))])));
    first_snapshot.value_capacity = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf([32]pwr_scalar_value) / @sizeOf(pwr_scalar_value)))));
    second_snapshot.values = @as([*c]pwr_scalar_value, @ptrCast(@alignCast(&second_values[@as(usize, @intCast(0))])));
    second_snapshot.value_capacity = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf([32]pwr_scalar_value) / @sizeOf(pwr_scalar_value)))));
    while (true) {
        if (!(api.*.instance_read_snapshot.?(first, &first_snapshot) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 165), "api->instance_read_snapshot(first, &first_snapshot) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_read_snapshot.?(second, &second_snapshot) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 166), "api->instance_read_snapshot(second, &second_snapshot) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(first_snapshot.simulation_time_ns == @as(u64, 3000000000))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 167), "first_snapshot.simulation_time_ns == UINT64_C(3000000000)" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(first_snapshot.state_hash == second_snapshot.state_hash)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 168), "first_snapshot.state_hash == second_snapshot.state_hash" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(first_snapshot.value_count == second_snapshot.value_count)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 169), "first_snapshot.value_count == second_snapshot.value_count" });
            failures += 1;
        }
        if (!false) break;
    }
    speed = find_value(&first_snapshot, @as(u64, 1002));
    while (true) {
        if (!(speed != null)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 171), "speed != NULL" });
            failures += 1;
        }
        if (!false) break;
    }
    if (speed != null) {
        while (true) {
            if (!(speed.*.value > 120.0)) {
                _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 173), "speed->value > 120.0" });
                failures += 1;
            }
            if (!false) break;
        }
        while (true) {
            if (!(speed.*.value < 200.0)) {
                _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 174), "speed->value < 200.0" });
                failures += 1;
            }
            if (!false) break;
        }
    }
    input_values[@as(c_uint, @intCast(@as(c_int, 0)))].channel = 18446744073709551615;
    while (true) {
        if (!(api.*.instance_submit_inputs.?(first, &input) == PWR_STATUS_UNKNOWN_CHANNEL)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 179), "api->instance_submit_inputs(first, &input) == PWR_STATUS_UNKNOWN_CHANNEL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_destroy.?(first) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 181), "api->instance_destroy(first) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_destroy.?(first) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 182), "api->instance_destroy(first) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_read_snapshot.?(first, &first_snapshot) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 184), "api->instance_read_snapshot(first, &first_snapshot) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.instance_destroy.?(second) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 185), "api->instance_destroy(second) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(context) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 186), "api->context_destroy(context) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_context_lifecycle(arg_api: [*c]const pwr_api) callconv(.c) void {
    var api = arg_api;
    _ = &api;
    var desc: pwr_context_desc = pwr_context_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_context_desc))))),
        .flags = @as(c_uint, 0),
        .reserved = @as(c_uint, 0),
    };
    _ = &desc;
    var first: pwr_context = 0;
    _ = &first;
    var second: pwr_context = 0;
    _ = &second;
    while (true) {
        if (!(api.*.context_create.?(null, &first) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 195), "api->context_create(NULL, &first) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(first == @as(u64, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 196), "first == PWR_CONTEXT_INVALID" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_create.?(&desc, null) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 197), "api->context_create(&desc, NULL) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    desc.abi_version = @as(c_uint, 1) +% @as(c_uint, 1);
    while (true) {
        if (!(api.*.context_create.?(&desc, &first) == PWR_STATUS_UNSUPPORTED_ABI)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 200), "api->context_create(&desc, &first) == PWR_STATUS_UNSUPPORTED_ABI" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(first == @as(u64, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 201), "first == PWR_CONTEXT_INVALID" });
            failures += 1;
        }
        if (!false) break;
    }
    desc.abi_version = 1;
    desc.struct_size = 1;
    while (true) {
        if (!(api.*.context_create.?(&desc, &first) == PWR_STATUS_STRUCT_TOO_SMALL)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 204), "api->context_create(&desc, &first) == PWR_STATUS_STRUCT_TOO_SMALL" });
            failures += 1;
        }
        if (!false) break;
    }
    desc.struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_context_desc)))));
    desc.flags = 1;
    while (true) {
        if (!(api.*.context_create.?(&desc, &first) == PWR_STATUS_UNSUPPORTED)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 207), "api->context_create(&desc, &first) == PWR_STATUS_UNSUPPORTED" });
            failures += 1;
        }
        if (!false) break;
    }
    desc.flags = 0;
    while (true) {
        if (!(api.*.context_destroy.?(@as(u64, 0)) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 211), "api->context_destroy(PWR_CONTEXT_INVALID) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_create.?(&desc, &first) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 212), "api->context_create(&desc, &first) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(first != @as(u64, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 213), "first != PWR_CONTEXT_INVALID" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(first) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 214), "api->context_destroy(first) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(first) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 215), "api->context_destroy(first) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_create.?(&desc, &second) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 217), "api->context_create(&desc, &second) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(second != @as(u64, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 218), "second != PWR_CONTEXT_INVALID" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(second != first)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 219), "second != first" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(first) == PWR_STATUS_INVALID_HANDLE)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 220), "api->context_destroy(first) == PWR_STATUS_INVALID_HANDLE" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(api.*.context_destroy.?(second) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 221), "api->context_destroy(second) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_context_capacity(arg_api: [*c]const pwr_api) callconv(.c) void {
    var api = arg_api;
    _ = &api;
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
    var contexts: [256]pwr_context = [1]pwr_context{
        0,
    } ++ [1]pwr_context{@import("std").mem.zeroes(pwr_context)} ** 255;
    _ = &contexts;
    var overflow: pwr_context = 0;
    _ = &overflow;
    var index: u32 = undefined;
    _ = &index;
    while (true) {
        if (!(api.*.runtime_get_capabilities.?(&capabilities) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 232), "api->runtime_get_capabilities(&capabilities) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(capabilities.max_contexts == @as(c_uint, 256))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 233), "capabilities.max_contexts == UINT32_C(256)" });
            failures += 1;
        }
        if (!false) break;
    }
    {
        index = 0;
        while (index < capabilities.max_contexts) : (index +%= 1) {
            while (true) {
                if (!(api.*.context_create.?(&desc, &contexts[index]) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 236), "api->context_create(&desc, &contexts[index]) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
        }
    }
    while (true) {
        if (!(api.*.context_create.?(&desc, &overflow) == PWR_STATUS_CAPACITY_EXCEEDED)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 239), "api->context_create(&desc, &overflow) == PWR_STATUS_CAPACITY_EXCEEDED" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(overflow == @as(u64, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 240), "overflow == PWR_CONTEXT_INVALID" });
            failures += 1;
        }
        if (!false) break;
    }
    {
        index = 0;
        while (index < capabilities.max_contexts) : (index +%= 1) {
            while (true) {
                if (!(api.*.context_destroy.?(contexts[index]) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 243), "api->context_destroy(contexts[index]) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
        }
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
    test_api_negotiation();
    while (true) {
        if (!(pwr_get_api(@as(c_uint, 1), @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_api))))), &api) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_sdk.c", @as(c_int, 253), "pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(api), &api) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    test_runtime_queries(&api);
    test_context_lifecycle(&api);
    test_context_capacity(&api);
    test_instance_lifecycle_and_step(&api);
    if (failures != @as(c_int, 0)) {
        _ = fprintf("{d} SDK test(s) failed\n", .{failures});
        return 1;
    }
    _ = puts("Power! SDK tests passed");
    return 0;
}

pub fn main() void {
    @import("std").process.exit(@intCast(run()));
}
