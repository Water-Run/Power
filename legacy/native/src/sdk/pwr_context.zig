// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/sdk/pwr_context.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_STATUS_CAPACITY_EXCEEDED = @import("../abi.zig").PWR_STATUS_CAPACITY_EXCEEDED;
const PWR_STATUS_INVALID_ARGUMENT = @import("../abi.zig").PWR_STATUS_INVALID_ARGUMENT;
const PWR_STATUS_INVALID_HANDLE = @import("../abi.zig").PWR_STATUS_INVALID_HANDLE;
const PWR_STATUS_IN_USE = @import("../abi.zig").PWR_STATUS_IN_USE;
const PWR_STATUS_OK = @import("../abi.zig").PWR_STATUS_OK;
const PWR_STATUS_STRUCT_TOO_SMALL = @import("../abi.zig").PWR_STATUS_STRUCT_TOO_SMALL;
const PWR_STATUS_UNSUPPORTED = @import("../abi.zig").PWR_STATUS_UNSUPPORTED;
const PWR_STATUS_UNSUPPORTED_ABI = @import("../abi.zig").PWR_STATUS_UNSUPPORTED_ABI;
const atomic_flag_clear_explicit = @import("../support.zig").atomic_flag_clear_explicit;
const atomic_flag_test_and_set_explicit = @import("../support.zig").atomic_flag_test_and_set_explicit;
const pwr_context = @import("../abi.zig").pwr_context;
const pwr_context_desc = @import("../abi.zig").pwr_context_desc;
const pwr_context_slot = @import("../abi.zig").pwr_context_slot;
const pwr_status = @import("../abi.zig").pwr_status;

pub fn pwr_context_create_impl(arg_desc: [*c]const pwr_context_desc, arg_context: [*c]pwr_context) callconv(.c) pwr_status {
    var desc = arg_desc;
    _ = &desc;
    var context = arg_context;
    _ = &context;
    var slot_index: u32 = undefined;
    _ = &slot_index;
    if (context == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    context.* = 0;
    if (desc == null) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (desc.*.abi_version != @as(c_uint, 1)) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if (@as(usize, @bitCast(@as(u64, desc.*.struct_size))) < @sizeOf(pwr_context_desc)) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    if ((desc.*.flags != @as(c_uint, 0)) or (desc.*.reserved != @as(c_uint, 0))) {
        return PWR_STATUS_UNSUPPORTED;
    }
    pwr_lock_registry();
    {
        slot_index = 0;
        while (slot_index < @as(c_uint, 256)) : (slot_index +%= 1) {
            const slot: [*c]pwr_context_slot = &pwr_context_slots[slot_index];
            _ = &slot;
            if (slot.*.active == @as(c_uint, 0)) {
                if (slot.*.generation == @as(c_uint, 0)) {
                    slot.*.generation = 1;
                }
                slot.*.flags = desc.*.flags;
                slot.*.reference_count = 0;
                slot.*.active = 1;
                context.* = pwr_make_handle(slot_index, slot.*.generation);
                pwr_unlock_registry();
                return PWR_STATUS_OK;
            }
        }
    }
    pwr_unlock_registry();
    return PWR_STATUS_CAPACITY_EXCEEDED;
}

pub fn pwr_context_destroy_impl(arg_context: pwr_context) callconv(.c) pwr_status {
    var context = arg_context;
    _ = &context;
    const generation: u32 = pwr_handle_generation(context);
    _ = &generation;
    var slot_index: u32 = 0;
    _ = &slot_index;
    var slot: [*c]pwr_context_slot = undefined;
    _ = &slot;
    if ((generation == @as(c_uint, 0)) or (pwr_handle_slot(context, &slot_index) != PWR_STATUS_OK)) {
        return PWR_STATUS_INVALID_HANDLE;
    }
    pwr_lock_registry();
    slot = &pwr_context_slots[slot_index];
    if ((slot.*.active == @as(c_uint, 0)) or (slot.*.generation != generation)) {
        pwr_unlock_registry();
        return PWR_STATUS_INVALID_HANDLE;
    }
    if (slot.*.reference_count != @as(c_uint, 0)) {
        pwr_unlock_registry();
        return PWR_STATUS_IN_USE;
    }
    slot.*.active = 0;
    slot.*.flags = 0;
    slot.*.generation +%= 1;
    if (slot.*.generation == @as(c_uint, 0)) {
        slot.*.generation = 1;
    }
    pwr_unlock_registry();
    return PWR_STATUS_OK;
}

pub fn pwr_context_retain_impl(arg_context: pwr_context) callconv(.c) pwr_status {
    var context = arg_context;
    _ = &context;
    const generation: u32 = pwr_handle_generation(context);
    _ = &generation;
    var slot_index: u32 = 0;
    _ = &slot_index;
    var slot: [*c]pwr_context_slot = undefined;
    _ = &slot;
    if ((generation == @as(c_uint, 0)) or (pwr_handle_slot(context, &slot_index) != PWR_STATUS_OK)) {
        return PWR_STATUS_INVALID_HANDLE;
    }
    pwr_lock_registry();
    slot = &pwr_context_slots[slot_index];
    if ((slot.*.active == @as(c_uint, 0)) or (slot.*.generation != generation)) {
        pwr_unlock_registry();
        return PWR_STATUS_INVALID_HANDLE;
    }
    if (slot.*.reference_count == @as(c_uint, 4294967295)) {
        pwr_unlock_registry();
        return PWR_STATUS_CAPACITY_EXCEEDED;
    }
    slot.*.reference_count +%= 1;
    pwr_unlock_registry();
    return PWR_STATUS_OK;
}

pub fn pwr_context_release_impl(arg_context: pwr_context) callconv(.c) void {
    var context = arg_context;
    _ = &context;
    const generation: u32 = pwr_handle_generation(context);
    _ = &generation;
    var slot_index: u32 = 0;
    _ = &slot_index;
    if ((generation == @as(c_uint, 0)) or (pwr_handle_slot(context, &slot_index) != PWR_STATUS_OK)) {
        return;
    }
    pwr_lock_registry();
    const slot: [*c]pwr_context_slot = &pwr_context_slots[slot_index];
    _ = &slot;
    if (((slot.*.active != @as(c_uint, 0)) and (slot.*.generation == generation)) and (slot.*.reference_count != @as(c_uint, 0))) {
        slot.*.reference_count -%= 1;
    }
    pwr_unlock_registry();
}

pub var pwr_registry_lock: atomic_flag = @as(c_int, 0) != 0;

pub var pwr_context_slots: [256]pwr_context_slot = @import("std").mem.zeroes([256]pwr_context_slot);

pub fn pwr_lock_registry() callconv(.c) void {
    while (atomic_flag_test_and_set_explicit(&pwr_registry_lock, @as(c_int, 1))) {}
}

pub fn pwr_unlock_registry() callconv(.c) void {
    atomic_flag_clear_explicit(&pwr_registry_lock, @as(c_int, 2));
}

pub fn pwr_make_handle(arg_slot_index: u32, arg_generation: u32) callconv(.c) pwr_context {
    var slot_index = arg_slot_index;
    _ = &slot_index;
    var generation = arg_generation;
    _ = &generation;
    return ((@as(u64, @bitCast(@as(u64, generation))) << @intCast(32)) | @as(u64, 1124073472)) | (@as(u64, @bitCast(@as(u64, slot_index))) +% @as(u64, 1));
}

pub fn pwr_handle_generation(arg_context: pwr_context) callconv(.c) u32 {
    var context = arg_context;
    _ = &context;
    return @as(u32, @bitCast(@as(c_uint, @truncate(context >> @intCast(32)))));
}

pub fn pwr_handle_slot(arg_context: pwr_context, arg_slot_index: [*c]u32) callconv(.c) pwr_status {
    var context = arg_context;
    _ = &context;
    var slot_index = arg_slot_index;
    _ = &slot_index;
    const encoded_slot: u64 = context & @as(u64, 16777215);
    _ = &encoded_slot;
    if ((((context & @as(u64, 4278190080)) != @as(u64, 1124073472)) or (encoded_slot == @as(u64, 0))) or (encoded_slot > @as(u64, @bitCast(@as(u64, @as(c_uint, 256)))))) {
        return PWR_STATUS_INVALID_HANDLE;
    }
    slot_index.* = @as(u32, @bitCast(@as(c_uint, @truncate(encoded_slot -% @as(u64, 1)))));
    return PWR_STATUS_OK;
}

pub const atomic_flag = bool;
