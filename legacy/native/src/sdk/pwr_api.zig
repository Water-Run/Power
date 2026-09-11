// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/sdk/pwr_api.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_STATUS_INVALID_ARGUMENT = @import("../abi.zig").PWR_STATUS_INVALID_ARGUMENT;
const PWR_STATUS_OK = @import("../abi.zig").PWR_STATUS_OK;
const PWR_STATUS_STRUCT_TOO_SMALL = @import("../abi.zig").PWR_STATUS_STRUCT_TOO_SMALL;
const PWR_STATUS_UNSUPPORTED_ABI = @import("../abi.zig").PWR_STATUS_UNSUPPORTED_ABI;
const memcpy = @import("../support.zig").memcpy;
const pwr_api = @import("../abi.zig").pwr_api;
const pwr_capabilities = @import("../abi.zig").pwr_capabilities;
const pwr_context_create_impl = @import("pwr_context.zig").pwr_context_create_impl;
const pwr_context_destroy_impl = @import("pwr_context.zig").pwr_context_destroy_impl;
const pwr_instance_create_from_model_impl = @import("pwr_model.zig").pwr_instance_create_from_model_impl;
const pwr_instance_create_impl = @import("pwr_instance.zig").pwr_instance_create_impl;
const pwr_instance_destroy_impl = @import("pwr_instance.zig").pwr_instance_destroy_impl;
const pwr_instance_read_snapshot_impl = @import("pwr_instance.zig").pwr_instance_read_snapshot_impl;
const pwr_instance_step_impl = @import("pwr_instance.zig").pwr_instance_step_impl;
const pwr_instance_submit_inputs_impl = @import("pwr_instance.zig").pwr_instance_submit_inputs_impl;
const pwr_model_compile_impl = @import("pwr_model.zig").pwr_model_compile_impl;
const pwr_model_destroy_impl = @import("pwr_model.zig").pwr_model_destroy_impl;
const pwr_model_get_channels_impl = @import("pwr_model.zig").pwr_model_get_channels_impl;
const pwr_model_get_info_impl = @import("pwr_model.zig").pwr_model_get_info_impl;
const pwr_status = @import("../abi.zig").pwr_status;
const pwr_status_string_fn = @import("../abi.zig").pwr_status_string_fn;
const pwr_version_info = @import("../abi.zig").pwr_version_info;
const struct_pwr_api = @import("../abi.zig").struct_pwr_api;

pub fn pwr_get_api(arg_requested_abi_version: u32, arg_api_struct_size: u32, arg_api: [*c]pwr_api) callconv(.c) pwr_status {
    var requested_abi_version = arg_requested_abi_version;
    _ = &requested_abi_version;
    var api_struct_size = arg_api_struct_size;
    _ = &api_struct_size;
    var api = arg_api;
    _ = &api;
    const runtime_api: pwr_api = pwr_api{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_api))))),
        .runtime_get_version = &pwr_runtime_get_version_impl,
        .runtime_get_capabilities = &pwr_runtime_get_capabilities_impl,
        .context_create = &pwr_context_create_impl,
        .context_destroy = &pwr_context_destroy_impl,
        .status_string = &pwr_status_string_impl,
        .instance_create = &pwr_instance_create_impl,
        .instance_destroy = &pwr_instance_destroy_impl,
        .instance_submit_inputs = &pwr_instance_submit_inputs_impl,
        .instance_step = &pwr_instance_step_impl,
        .instance_read_snapshot = &pwr_instance_read_snapshot_impl,
        .model_compile = &pwr_model_compile_impl,
        .model_destroy = &pwr_model_destroy_impl,
        .model_get_info = &pwr_model_get_info_impl,
        .model_get_channels = &pwr_model_get_channels_impl,
        .instance_create_from_model = &pwr_instance_create_from_model_impl,
    };
    _ = &runtime_api;
    const minimum_v1_size: usize = @offsetOf(struct_pwr_api, "status_string") +% @sizeOf(pwr_status_string_fn);
    _ = &minimum_v1_size;
    var copy_size: usize = undefined;
    _ = &copy_size;
    if (api == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (requested_abi_version != @as(c_uint, 1)) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if (@as(usize, @bitCast(@as(u64, api_struct_size))) < minimum_v1_size) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    copy_size = @as(usize, @bitCast(@as(u64, api_struct_size)));
    if (copy_size > @sizeOf(pwr_api)) {
        copy_size = @sizeOf(pwr_api);
    }
    _ = memcpy(@as(?*anyopaque, @ptrCast(api)), @as(?*const anyopaque, @ptrCast(&runtime_api)), copy_size);
    return PWR_STATUS_OK;
}

pub fn pwr_runtime_get_version_impl(arg_info: [*c]pwr_version_info) callconv(.c) pwr_status {
    var info = arg_info;
    _ = &info;
    var status: pwr_status = undefined;
    _ = &status;
    if (info == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    status = pwr_validate_struct(info.*.abi_version, info.*.struct_size, @sizeOf(pwr_version_info));
    if (status != PWR_STATUS_OK) {
        return status;
    }
    info.*.abi_version = 1;
    info.*.struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_version_info)))));
    info.*.major = 0;
    info.*.minor = 2;
    info.*.patch = 0;
    info.*.reserved = 0;
    info.*.product_name = "Power!";
    info.*.version_string = "0.2.0-zig";
    return PWR_STATUS_OK;
}

pub fn pwr_runtime_get_capabilities_impl(arg_capabilities: [*c]pwr_capabilities) callconv(.c) pwr_status {
    var capabilities = arg_capabilities;
    _ = &capabilities;
    var status: pwr_status = undefined;
    _ = &status;
    if (capabilities == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    status = pwr_validate_struct(capabilities.*.abi_version, capabilities.*.struct_size, @sizeOf(pwr_capabilities));
    if (status != PWR_STATUS_OK) {
        return status;
    }
    capabilities.*.abi_version = 1;
    capabilities.*.struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_capabilities)))));
    capabilities.*.feature_bits = (((((((@as(u64, 1) << @intCast(0)) | (@as(u64, 1) << @intCast(1))) | (@as(u64, 1) << @intCast(2))) | (@as(u64, 1) << @intCast(3))) | (@as(u64, 1) << @intCast(4))) | (@as(u64, 1) << @intCast(5))) | (@as(u64, 1) << @intCast(6))) | (@as(u64, 1) << @intCast(7));
    capabilities.*.max_contexts = 256;
    capabilities.*.reserved = 0;
    return PWR_STATUS_OK;
}

pub fn pwr_status_string_impl(arg_status: pwr_status) callconv(.c) [*c]const u8 {
    var status = arg_status;
    _ = &status;
    while (true) {
        switch (status) {
            @as(c_int, 0) => return "success",
            @as(c_int, -1) => return "invalid argument",
            @as(c_int, -2) => return "unsupported ABI version",
            @as(c_int, -3) => return "structure is too small",
            @as(c_int, -4) => return "invalid or stale handle",
            @as(c_int, -5) => return "capacity exceeded",
            @as(c_int, -6) => return "operation or option is unsupported",
            @as(c_int, -7) => return "internal error",
            @as(c_int, -8) => return "object still has live dependants",
            @as(c_int, -9) => return "object is busy on another thread",
            @as(c_int, -10) => return "time step is invalid for this model",
            @as(c_int, -11) => return "input or output channel is unknown",
            @as(c_int, -12) => return "model definition is invalid",
            @as(c_int, -13) => return "numerical solve failed",
            @as(c_int, -14) => return "out of memory",
            else => return "unknown status",
        }
        break;
    }
    return null;
}

pub fn pwr_validate_struct(arg_abi_version: u32, arg_struct_size: u32, arg_required_size: usize) callconv(.c) pwr_status {
    var abi_version = arg_abi_version;
    _ = &abi_version;
    var struct_size = arg_struct_size;
    _ = &struct_size;
    var required_size = arg_required_size;
    _ = &required_size;
    if (abi_version != @as(c_uint, 1)) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if (@as(usize, @bitCast(@as(u64, struct_size))) < required_size) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    return PWR_STATUS_OK;
}
