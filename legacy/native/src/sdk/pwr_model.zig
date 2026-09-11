// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/sdk/pwr_model.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_MODEL_FIDELITY_LINEAR_LUMPED = @import("../abi.zig").PWR_MODEL_FIDELITY_LINEAR_LUMPED;
const PWR_MODEL_VALIDATION_UNVERIFIED = @import("../abi.zig").PWR_MODEL_VALIDATION_UNVERIFIED;
const PWR_STATUS_CAPACITY_EXCEEDED = @import("../abi.zig").PWR_STATUS_CAPACITY_EXCEEDED;
const PWR_STATUS_INVALID_ARGUMENT = @import("../abi.zig").PWR_STATUS_INVALID_ARGUMENT;
const PWR_STATUS_INVALID_HANDLE = @import("../abi.zig").PWR_STATUS_INVALID_HANDLE;
const PWR_STATUS_IN_USE = @import("../abi.zig").PWR_STATUS_IN_USE;
const PWR_STATUS_OK = @import("../abi.zig").PWR_STATUS_OK;
const PWR_STATUS_OUT_OF_MEMORY = @import("../abi.zig").PWR_STATUS_OUT_OF_MEMORY;
const PWR_STATUS_STRUCT_TOO_SMALL = @import("../abi.zig").PWR_STATUS_STRUCT_TOO_SMALL;
const PWR_STATUS_UNSUPPORTED_ABI = @import("../abi.zig").PWR_STATUS_UNSUPPORTED_ABI;
const atomic_flag_clear_explicit = @import("../support.zig").atomic_flag_clear_explicit;
const atomic_flag_test_and_set_explicit = @import("../support.zig").atomic_flag_test_and_set_explicit;
const free = @import("../support.zig").free;
const malloc = @import("../support.zig").malloc;
const memcpy = @import("../support.zig").memcpy;
const pwr_channel_info = @import("../abi.zig").pwr_channel_info;
const pwr_context = @import("../abi.zig").pwr_context;
const pwr_context_release_impl = @import("pwr_context.zig").pwr_context_release_impl;
const pwr_context_retain_impl = @import("pwr_context.zig").pwr_context_retain_impl;
const pwr_graph = @import("../abi.zig").pwr_graph;
const pwr_graph_compile = @import("../core/pwr_graph.zig").pwr_graph_compile;
const pwr_graph_init = @import("../core/pwr_graph.zig").pwr_graph_init;
const pwr_graph_runtime = @import("../abi.zig").pwr_graph_runtime;
const pwr_graph_snapshot = @import("../core/pwr_graph.zig").pwr_graph_snapshot;
const pwr_graph_step = @import("../core/pwr_graph.zig").pwr_graph_step;
const pwr_graph_submit = @import("../core/pwr_graph.zig").pwr_graph_submit;
const pwr_input_frame = @import("../abi.zig").pwr_input_frame;
const pwr_instance = @import("../abi.zig").pwr_instance;
const pwr_instance_backend = @import("../abi.zig").pwr_instance_backend;
const pwr_instance_register = @import("pwr_instance.zig").pwr_instance_register;
const pwr_model = @import("../abi.zig").pwr_model;
const pwr_model_desc = @import("../abi.zig").pwr_model_desc;
const pwr_model_diagnostic = @import("../abi.zig").pwr_model_diagnostic;
const pwr_model_info = @import("../abi.zig").pwr_model_info;
const pwr_model_slot = @import("../abi.zig").pwr_model_slot;
const pwr_snapshot = @import("../abi.zig").pwr_snapshot;
const pwr_status = @import("../abi.zig").pwr_status;

pub fn pwr_model_acquire(arg_model: pwr_model, arg_graph: [*c][*c]const pwr_graph, arg_context: [*c]pwr_context) callconv(.c) pwr_status {
    var model = arg_model;
    _ = &model;
    var graph = arg_graph;
    _ = &graph;
    var context = arg_context;
    _ = &context;
    pwr_lock_models();
    var slot: [*c]pwr_model_slot = pwr_model_lookup(model);
    _ = &slot;
    if (slot == null) {
        pwr_unlock_models();
        return PWR_STATUS_INVALID_HANDLE;
    }
    if (slot.*.references == @as(c_uint, 4294967295)) {
        pwr_unlock_models();
        return PWR_STATUS_CAPACITY_EXCEEDED;
    }
    slot.*.references +%= 1;
    graph.* = slot.*.graph;
    context.* = slot.*.context;
    pwr_unlock_models();
    return PWR_STATUS_OK;
}

pub fn pwr_model_release(arg_model: pwr_model) callconv(.c) void {
    var model = arg_model;
    _ = &model;
    pwr_lock_models();
    var slot: [*c]pwr_model_slot = pwr_model_lookup(model);
    _ = &slot;
    if ((slot != null) and (slot.*.references != @as(c_uint, 0))) {
        slot.*.references -%= 1;
    }
    pwr_unlock_models();
}

pub fn pwr_model_compile_impl(arg_context: pwr_context, arg_desc: [*c]const pwr_model_desc, arg_model: [*c]pwr_model, arg_diagnostic: [*c]pwr_model_diagnostic) callconv(.c) pwr_status {
    var context = arg_context;
    _ = &context;
    var desc = arg_desc;
    _ = &desc;
    var model = arg_model;
    _ = &model;
    var diagnostic = arg_diagnostic;
    _ = &diagnostic;
    if (model == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    model.* = 0;
    var status: pwr_status = pwr_context_retain_impl(context);
    _ = &status;
    if (status != PWR_STATUS_OK) {
        return status;
    }
    var graph: [*c]pwr_graph = @as([*c]pwr_graph, @ptrCast(@alignCast(malloc(@sizeOf(pwr_graph)))));
    _ = &graph;
    if (graph == null) {
        pwr_context_release_impl(context);
        return PWR_STATUS_OUT_OF_MEMORY;
    }
    status = pwr_graph_compile(desc, graph, diagnostic);
    if (status != PWR_STATUS_OK) {
        free(@as(?*anyopaque, @ptrCast(graph)));
        pwr_context_release_impl(context);
        return status;
    }
    pwr_lock_models();
    {
        var i: u32 = 0;
        _ = &i;
        while (i < @as(c_uint, 64)) : (i +%= 1) {
            var slot: [*c]pwr_model_slot = &pwr_model_slots[i];
            _ = &slot;
            if (slot.*.graph != null) {
                continue;
            }
            if (slot.*.generation == @as(c_uint, 0)) {
                slot.*.generation = 1;
            }
            slot.*.context = context;
            slot.*.references = 0;
            slot.*.graph = graph;
            model.* = ((@as(u64, @bitCast(@as(u64, slot.*.generation))) << @intCast(32)) | @as(u64, @bitCast(@as(u64, @as(c_uint, 1291845632))))) | (@as(u64, @bitCast(@as(u64, i))) +% @as(u64, @bitCast(@as(u64, @as(c_uint, 1)))));
            pwr_unlock_models();
            return PWR_STATUS_OK;
        }
    }
    pwr_unlock_models();
    free(@as(?*anyopaque, @ptrCast(graph)));
    pwr_context_release_impl(context);
    return PWR_STATUS_CAPACITY_EXCEEDED;
}

pub fn pwr_model_destroy_impl(arg_model: pwr_model) callconv(.c) pwr_status {
    var model = arg_model;
    _ = &model;
    pwr_lock_models();
    var slot: [*c]pwr_model_slot = pwr_model_lookup(model);
    _ = &slot;
    if (slot == null) {
        pwr_unlock_models();
        return PWR_STATUS_INVALID_HANDLE;
    }
    if (slot.*.references != @as(c_uint, 0)) {
        pwr_unlock_models();
        return PWR_STATUS_IN_USE;
    }
    var graph: [*c]pwr_graph = slot.*.graph;
    _ = &graph;
    const context: pwr_context = slot.*.context;
    _ = &context;
    slot.*.graph = null;
    slot.*.context = 0;
    slot.*.generation +%= 1;
    if (slot.*.generation == @as(c_uint, 0)) {
        slot.*.generation = 1;
    }
    pwr_unlock_models();
    free(@as(?*anyopaque, @ptrCast(graph)));
    pwr_context_release_impl(context);
    return PWR_STATUS_OK;
}

pub fn pwr_model_get_info_impl(arg_model: pwr_model, arg_info: [*c]pwr_model_info) callconv(.c) pwr_status {
    var model = arg_model;
    _ = &model;
    var info = arg_info;
    _ = &info;
    if (info == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (info.*.abi_version != @as(c_uint, 1)) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if (@as(u64, @bitCast(@as(u64, info.*.struct_size))) < @sizeOf(pwr_model_info)) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    var graph: [*c]const pwr_graph = null;
    _ = &graph;
    var context: pwr_context = undefined;
    _ = &context;
    const status: pwr_status = pwr_model_acquire(model, &graph, &context);
    _ = &status;
    if (status != PWR_STATUS_OK) {
        return status;
    }
    info.* = pwr_model_info{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_info))))),
        .fingerprint = graph.*.fingerprint,
        .step_ns = graph.*.step_ns,
        .node_count = graph.*.node_count,
        .component_count = graph.*.component_count,
        .state_count = graph.*.dynamic_count +% graph.*.thermal_count,
        .channel_count = graph.*.channel_count,
        .fidelity = @as(u32, @bitCast(PWR_MODEL_FIDELITY_LINEAR_LUMPED)),
        .validation = @as(u32, @bitCast(PWR_MODEL_VALIDATION_UNVERIFIED)),
    };
    pwr_model_release(model);
    return PWR_STATUS_OK;
}

pub fn pwr_model_get_channels_impl(arg_model: pwr_model, arg_channels: [*c]pwr_channel_info, arg_capacity: u32, arg_count: [*c]u32) callconv(.c) pwr_status {
    var model = arg_model;
    _ = &model;
    var channels = arg_channels;
    _ = &channels;
    var capacity = arg_capacity;
    _ = &capacity;
    var count = arg_count;
    _ = &count;
    if (count == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    count.* = 0;
    if ((channels == null) and (capacity != @as(c_uint, 0))) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    var graph: [*c]const pwr_graph = null;
    _ = &graph;
    var context: pwr_context = undefined;
    _ = &context;
    var status: pwr_status = pwr_model_acquire(model, &graph, &context);
    _ = &status;
    if (status != PWR_STATUS_OK) {
        return status;
    }
    count.* = graph.*.channel_count;
    if (capacity < graph.*.channel_count) {
        status = PWR_STATUS_CAPACITY_EXCEEDED;
    } else {
        _ = memcpy(@as(?*anyopaque, @ptrCast(channels)), @as(?*const anyopaque, @ptrCast(@as([*c]const pwr_channel_info, @ptrCast(@alignCast(&graph.*.channels[@as(usize, @intCast(0))]))))), @as(u64, @bitCast(@as(u64, graph.*.channel_count))) *% @sizeOf(pwr_channel_info));
    }
    pwr_model_release(model);
    return status;
}

pub fn pwr_instance_create_from_model_impl(arg_model: pwr_model, arg_instance: [*c]pwr_instance) callconv(.c) pwr_status {
    var model = arg_model;
    _ = &model;
    var instance = arg_instance;
    _ = &instance;
    if (instance == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    instance.* = 0;
    var graph: [*c]const pwr_graph = null;
    _ = &graph;
    var context: pwr_context = undefined;
    _ = &context;
    var status: pwr_status = pwr_model_acquire(model, &graph, &context);
    _ = &status;
    if (status != PWR_STATUS_OK) {
        return status;
    }
    var runtime: [*c]pwr_graph_runtime = @as([*c]pwr_graph_runtime, @ptrCast(@alignCast(malloc(@sizeOf(pwr_graph_runtime)))));
    _ = &runtime;
    if (runtime == null) {
        pwr_model_release(model);
        return PWR_STATUS_OUT_OF_MEMORY;
    }
    runtime.*.graph = graph;
    status = pwr_graph_init(graph, &runtime.*.state);
    if (status == PWR_STATUS_OK) {
        status = pwr_instance_register(context, model, &pwr_model_backend, @as(?*anyopaque, @ptrCast(runtime)), instance);
    }
    if (status != PWR_STATUS_OK) {
        free(@as(?*anyopaque, @ptrCast(runtime)));
        pwr_model_release(model);
    }
    return status;
}

pub var pwr_model_lock: atomic_flag = @as(c_int, 0) != 0;

pub var pwr_model_slots: [64]pwr_model_slot = @import("std").mem.zeroes([64]pwr_model_slot);

pub fn pwr_lock_models() callconv(.c) void {
    while (atomic_flag_test_and_set_explicit(&pwr_model_lock, @as(c_int, 1))) {}
}

pub fn pwr_unlock_models() callconv(.c) void {
    atomic_flag_clear_explicit(&pwr_model_lock, @as(c_int, 2));
}

pub fn pwr_model_lookup(arg_model: pwr_model) callconv(.c) [*c]pwr_model_slot {
    var model = arg_model;
    _ = &model;
    const encoded: u32 = @as(u32, @bitCast(@as(c_uint, @truncate(model))));
    _ = &encoded;
    const generation: u32 = @as(u32, @bitCast(@as(c_uint, @truncate(model >> @intCast(32)))));
    _ = &generation;
    const index: u32 = encoded & @as(c_uint, 16777215);
    _ = &index;
    if (((((encoded & @as(c_uint, 4278190080)) != @as(c_uint, 1291845632)) or (generation == @as(c_uint, 0))) or (index == @as(c_uint, 0))) or (index > @as(c_uint, 64))) {
        return null;
    }
    var slot: [*c]pwr_model_slot = &pwr_model_slots[index -% @as(c_uint, 1)];
    _ = &slot;
    return if ((slot.*.graph != null) and (slot.*.generation == generation)) slot else null;
}

pub fn pwr_model_submit(arg_opaque: ?*anyopaque, arg_frame: [*c]const pwr_input_frame) callconv(.c) pwr_status {
    var @"opaque" = arg_opaque;
    _ = &@"opaque";
    var frame = arg_frame;
    _ = &frame;
    var r: [*c]pwr_graph_runtime = @as([*c]pwr_graph_runtime, @ptrCast(@alignCast(@"opaque")));
    _ = &r;
    return pwr_graph_submit(r.*.graph, &r.*.state, frame);
}

pub fn pwr_model_step(arg_opaque: ?*anyopaque, arg_delta_ns: u64) callconv(.c) pwr_status {
    var @"opaque" = arg_opaque;
    _ = &@"opaque";
    var delta_ns = arg_delta_ns;
    _ = &delta_ns;
    var r: [*c]pwr_graph_runtime = @as([*c]pwr_graph_runtime, @ptrCast(@alignCast(@"opaque")));
    _ = &r;
    return pwr_graph_step(r.*.graph, &r.*.state, delta_ns);
}

pub fn pwr_model_snapshot(arg_opaque: ?*const anyopaque, arg_snapshot: [*c]pwr_snapshot) callconv(.c) pwr_status {
    var @"opaque" = arg_opaque;
    _ = &@"opaque";
    var snapshot = arg_snapshot;
    _ = &snapshot;
    var r: [*c]const pwr_graph_runtime = @as([*c]const pwr_graph_runtime, @ptrCast(@alignCast(@"opaque")));
    _ = &r;
    return pwr_graph_snapshot(r.*.graph, &r.*.state, snapshot);
}

pub const pwr_model_backend: pwr_instance_backend = pwr_instance_backend{
    .submit = &pwr_model_submit,
    .step = &pwr_model_step,
    .snapshot = &pwr_model_snapshot,
    .destroy = &free,
};

pub const atomic_flag = bool;
