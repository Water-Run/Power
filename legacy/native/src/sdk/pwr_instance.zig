// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/sdk/pwr_instance.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_BUILTIN_MODEL_CONTROLLED_SHAFT = @import("../abi.zig").PWR_BUILTIN_MODEL_CONTROLLED_SHAFT;
const PWR_STATUS_BUSY = @import("../abi.zig").PWR_STATUS_BUSY;
const PWR_STATUS_CAPACITY_EXCEEDED = @import("../abi.zig").PWR_STATUS_CAPACITY_EXCEEDED;
const PWR_STATUS_INVALID_ARGUMENT = @import("../abi.zig").PWR_STATUS_INVALID_ARGUMENT;
const PWR_STATUS_INVALID_HANDLE = @import("../abi.zig").PWR_STATUS_INVALID_HANDLE;
const PWR_STATUS_OK = @import("../abi.zig").PWR_STATUS_OK;
const PWR_STATUS_STRUCT_TOO_SMALL = @import("../abi.zig").PWR_STATUS_STRUCT_TOO_SMALL;
const PWR_STATUS_UNSUPPORTED = @import("../abi.zig").PWR_STATUS_UNSUPPORTED;
const PWR_STATUS_UNSUPPORTED_ABI = @import("../abi.zig").PWR_STATUS_UNSUPPORTED_ABI;
const atomic_compare_exchange_strong_explicit = @import("../support.zig").atomic_compare_exchange_strong_explicit;
const atomic_flag_clear_explicit = @import("../support.zig").atomic_flag_clear_explicit;
const atomic_flag_test_and_set_explicit = @import("../support.zig").atomic_flag_test_and_set_explicit;
const atomic_load_explicit = @import("../support.zig").atomic_load_explicit;
const atomic_store_explicit = @import("../support.zig").atomic_store_explicit;
const pwr_context = @import("../abi.zig").pwr_context;
const pwr_context_release_impl = @import("pwr_context.zig").pwr_context_release_impl;
const pwr_context_retain_impl = @import("pwr_context.zig").pwr_context_retain_impl;
const pwr_input_frame = @import("../abi.zig").pwr_input_frame;
const pwr_instance = @import("../abi.zig").pwr_instance;
const pwr_instance_backend = @import("../abi.zig").pwr_instance_backend;
const pwr_instance_desc = @import("../abi.zig").pwr_instance_desc;
const pwr_instance_slot = @import("../abi.zig").pwr_instance_slot;
const pwr_model = @import("../abi.zig").pwr_model;
const pwr_model_release = @import("pwr_model.zig").pwr_model_release;
const pwr_scalar_value = @import("../abi.zig").pwr_scalar_value;
const pwr_shaft_backend = @import("pwr_shaft_backend.zig").pwr_shaft_backend;
const pwr_shaft_backend_create = @import("pwr_shaft_backend.zig").pwr_shaft_backend_create;
const pwr_snapshot = @import("../abi.zig").pwr_snapshot;
const pwr_status = @import("../abi.zig").pwr_status;

pub fn pwr_instance_register(arg_context: pwr_context, arg_model: pwr_model, arg_backend: [*c]const pwr_instance_backend, arg_state: ?*anyopaque, arg_instance: [*c]pwr_instance) callconv(.c) pwr_status {
    var context = arg_context;
    _ = &context;
    var model = arg_model;
    _ = &model;
    var backend = arg_backend;
    _ = &backend;
    var state = arg_state;
    _ = &state;
    var instance = arg_instance;
    _ = &instance;
    const status: pwr_status = pwr_context_retain_impl(context);
    _ = &status;
    if (status != PWR_STATUS_OK) {
        return status;
    }
    pwr_lock_instance_registry();
    {
        var index: u32 = 0;
        _ = &index;
        while (index < @as(c_uint, 256)) : (index +%= 1) {
            var slot: [*c]pwr_instance_slot = &pwr_instance_slots[index];
            _ = &slot;
            if (slot.*.active != @as(c_uint, 0)) {
                continue;
            }
            if (slot.*.generation == @as(c_uint, 0)) {
                slot.*.generation = 1;
            }
            slot.*.context = context;
            slot.*.model = model;
            slot.*.backend = backend;
            slot.*.state = state;
            atomic_store_explicit(&slot.*.busy, @as(c_int, 0) != 0, @as(c_int, 3));
            slot.*.active = 1;
            instance.* = pwr_make_instance_handle(index, slot.*.generation);
            pwr_unlock_instance_registry();
            return PWR_STATUS_OK;
        }
    }
    pwr_unlock_instance_registry();
    pwr_context_release_impl(context);
    return PWR_STATUS_CAPACITY_EXCEEDED;
}

pub fn pwr_instance_create_impl(arg_context: pwr_context, arg_desc: [*c]const pwr_instance_desc, arg_instance: [*c]pwr_instance) callconv(.c) pwr_status {
    var context = arg_context;
    _ = &context;
    var desc = arg_desc;
    _ = &desc;
    var instance = arg_instance;
    _ = &instance;
    if (instance == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    instance.* = 0;
    var status: pwr_status = pwr_validate_instance_desc(desc);
    _ = &status;
    if (status != PWR_STATUS_OK) {
        return status;
    }
    var state: ?*anyopaque = null;
    _ = &state;
    status = pwr_shaft_backend_create(&state);
    if (status != PWR_STATUS_OK) {
        return status;
    }
    status = pwr_instance_register(context, @as(u64, 0), &pwr_shaft_backend, state, instance);
    if (status != PWR_STATUS_OK) {
        pwr_shaft_backend.destroy.?(state);
    }
    return status;
}

pub fn pwr_instance_destroy_impl(arg_instance: pwr_instance) callconv(.c) pwr_status {
    var instance = arg_instance;
    _ = &instance;
    var slot_index: u32 = 0;
    _ = &slot_index;
    var generation: u32 = 0;
    _ = &generation;
    var context: pwr_context = undefined;
    _ = &context;
    if (pwr_decode_instance_handle(instance, &slot_index, &generation) != PWR_STATUS_OK) {
        return PWR_STATUS_INVALID_HANDLE;
    }
    pwr_lock_instance_registry();
    const slot: [*c]pwr_instance_slot = &pwr_instance_slots[slot_index];
    _ = &slot;
    if ((slot.*.active == @as(c_uint, 0)) or (slot.*.generation != generation)) {
        pwr_unlock_instance_registry();
        return PWR_STATUS_INVALID_HANDLE;
    }
    if (atomic_load_explicit(&slot.*.busy, @as(c_int, 1))) {
        pwr_unlock_instance_registry();
        return PWR_STATUS_BUSY;
    }
    context = slot.*.context;
    const model: pwr_model = slot.*.model;
    _ = &model;
    var backend: [*c]const pwr_instance_backend = slot.*.backend;
    _ = &backend;
    var state: ?*anyopaque = slot.*.state;
    _ = &state;
    slot.*.model = 0;
    slot.*.backend = null;
    slot.*.state = null;
    slot.*.active = 0;
    slot.*.context = 0;
    slot.*.generation +%= 1;
    if (slot.*.generation == @as(c_uint, 0)) {
        slot.*.generation = 1;
    }
    pwr_unlock_instance_registry();
    backend.*.destroy.?(state);
    if (model != @as(u64, 0)) {
        pwr_model_release(model);
    }
    pwr_context_release_impl(context);
    return PWR_STATUS_OK;
}

pub fn pwr_instance_submit_inputs_impl(arg_instance: pwr_instance, arg_frame: [*c]const pwr_input_frame) callconv(.c) pwr_status {
    var instance = arg_instance;
    _ = &instance;
    var frame = arg_frame;
    _ = &frame;
    if (frame == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (frame.*.abi_version != @as(c_uint, 1)) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if (@as(u64, @bitCast(@as(u64, frame.*.struct_size))) < @sizeOf(pwr_input_frame)) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    if ((frame.*.flags != @as(c_uint, 0)) or ((frame.*.value_count != @as(c_uint, 0)) and (frame.*.values == null))) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    var slot: [*c]pwr_instance_slot = null;
    _ = &slot;
    var status: pwr_status = pwr_instance_begin(instance, &slot);
    _ = &status;
    if (status != PWR_STATUS_OK) {
        return status;
    }
    status = slot.*.backend.*.submit.?(slot.*.state, frame);
    pwr_instance_end(slot);
    return status;
}

pub fn pwr_instance_step_impl(arg_instance: pwr_instance, arg_delta_ns: u64) callconv(.c) pwr_status {
    var instance = arg_instance;
    _ = &instance;
    var delta_ns = arg_delta_ns;
    _ = &delta_ns;
    var slot: [*c]pwr_instance_slot = null;
    _ = &slot;
    var status: pwr_status = pwr_instance_begin(instance, &slot);
    _ = &status;
    if (status != PWR_STATUS_OK) {
        return status;
    }
    status = slot.*.backend.*.step.?(slot.*.state, delta_ns);
    pwr_instance_end(slot);
    return status;
}

pub fn pwr_instance_read_snapshot_impl(arg_instance: pwr_instance, arg_snapshot: [*c]pwr_snapshot) callconv(.c) pwr_status {
    var instance = arg_instance;
    _ = &instance;
    var snapshot = arg_snapshot;
    _ = &snapshot;
    if (snapshot == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (snapshot.*.abi_version != @as(c_uint, 1)) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if (@as(u64, @bitCast(@as(u64, snapshot.*.struct_size))) < @sizeOf(pwr_snapshot)) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    var slot: [*c]pwr_instance_slot = null;
    _ = &slot;
    var status: pwr_status = pwr_instance_begin(instance, &slot);
    _ = &status;
    if (status != PWR_STATUS_OK) {
        return status;
    }
    status = slot.*.backend.*.snapshot.?(slot.*.state, snapshot);
    pwr_instance_end(slot);
    return status;
}

pub var pwr_instance_registry_lock: atomic_flag = @as(c_int, 0) != 0;

pub var pwr_instance_slots: [256]pwr_instance_slot = @import("std").mem.zeroes([256]pwr_instance_slot);

pub fn pwr_lock_instance_registry() callconv(.c) void {
    while (atomic_flag_test_and_set_explicit(&pwr_instance_registry_lock, @as(c_int, 1))) {}
}

pub fn pwr_unlock_instance_registry() callconv(.c) void {
    atomic_flag_clear_explicit(&pwr_instance_registry_lock, @as(c_int, 2));
}

pub fn pwr_make_instance_handle(arg_slot_index: u32, arg_generation: u32) callconv(.c) pwr_instance {
    var slot_index = arg_slot_index;
    _ = &slot_index;
    var generation = arg_generation;
    _ = &generation;
    return ((@as(u64, @bitCast(@as(u64, generation))) << @intCast(32)) | @as(u64, 1224736768)) | (@as(u64, @bitCast(@as(u64, slot_index))) +% @as(u64, 1));
}

pub fn pwr_decode_instance_handle(arg_instance: pwr_instance, arg_slot_index: [*c]u32, arg_generation: [*c]u32) callconv(.c) pwr_status {
    var instance = arg_instance;
    _ = &instance;
    var slot_index = arg_slot_index;
    _ = &slot_index;
    var generation = arg_generation;
    _ = &generation;
    const encoded_slot: u64 = instance & @as(u64, 16777215);
    _ = &encoded_slot;
    const decoded_generation: u32 = @as(u32, @bitCast(@as(c_uint, @truncate(instance >> @intCast(32)))));
    _ = &decoded_generation;
    if ((((((slot_index == null) or (generation == null)) or ((instance & @as(u64, 4278190080)) != @as(u64, 1224736768))) or (decoded_generation == @as(c_uint, 0))) or (encoded_slot == @as(u64, 0))) or (encoded_slot > @as(u64, @bitCast(@as(u64, @as(c_uint, 256)))))) {
        return PWR_STATUS_INVALID_HANDLE;
    }
    slot_index.* = @as(u32, @bitCast(@as(c_uint, @truncate(encoded_slot -% @as(u64, 1)))));
    generation.* = decoded_generation;
    return PWR_STATUS_OK;
}

pub fn pwr_instance_begin(arg_instance: pwr_instance, arg_slot_out: [*c][*c]pwr_instance_slot) callconv(.c) pwr_status {
    var instance = arg_instance;
    _ = &instance;
    var slot_out = arg_slot_out;
    _ = &slot_out;
    var slot_index: u32 = 0;
    _ = &slot_index;
    var generation: u32 = 0;
    _ = &generation;
    var slot: [*c]pwr_instance_slot = undefined;
    _ = &slot;
    var expected: bool = @as(c_int, 0) != 0;
    _ = &expected;
    if ((slot_out == null) or (pwr_decode_instance_handle(instance, &slot_index, &generation) != PWR_STATUS_OK)) {
        return PWR_STATUS_INVALID_HANDLE;
    }
    pwr_lock_instance_registry();
    slot = &pwr_instance_slots[slot_index];
    if ((slot.*.active == @as(c_uint, 0)) or (slot.*.generation != generation)) {
        pwr_unlock_instance_registry();
        return PWR_STATUS_INVALID_HANDLE;
    }
    if (!atomic_compare_exchange_strong_explicit(&slot.*.busy, &expected, @as(c_int, 1) != 0, @as(c_int, 1), @as(c_int, 3))) {
        pwr_unlock_instance_registry();
        return PWR_STATUS_BUSY;
    }
    pwr_unlock_instance_registry();
    slot_out.* = slot;
    return PWR_STATUS_OK;
}

pub fn pwr_instance_end(arg_slot: [*c]pwr_instance_slot) callconv(.c) void {
    var slot = arg_slot;
    _ = &slot;
    atomic_store_explicit(&slot.*.busy, @as(c_int, 0) != 0, @as(c_int, 2));
}

pub fn pwr_validate_instance_desc(arg_desc: [*c]const pwr_instance_desc) callconv(.c) pwr_status {
    var desc = arg_desc;
    _ = &desc;
    if (desc == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (desc.*.abi_version != @as(c_uint, 1)) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if (@as(usize, @bitCast(@as(u64, desc.*.struct_size))) < @sizeOf(pwr_instance_desc)) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    if ((desc.*.flags != @as(c_uint, 0)) or (desc.*.seed != @as(u64, 0))) {
        return PWR_STATUS_UNSUPPORTED;
    }
    if (desc.*.model != @as(u32, @bitCast(PWR_BUILTIN_MODEL_CONTROLLED_SHAFT))) {
        return PWR_STATUS_UNSUPPORTED;
    }
    return PWR_STATUS_OK;
}

pub const atomic_flag = bool;
