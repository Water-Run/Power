// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

const std = @import("std");
const abi = @import("../src/abi.zig");
const api_module = @import("../src/sdk/pwr_api.zig");
const support = @import("../src/support.zig");

fn exerciseRegistry(api: *const abi.pwr_api, failed: *std.atomic.Value(bool)) void {
    const desc: abi.pwr_context_desc = .{ .abi_version = 1, .struct_size = @sizeOf(abi.pwr_context_desc) };
    const instance_desc: abi.pwr_instance_desc = .{
        .abi_version = 1,
        .struct_size = @sizeOf(abi.pwr_instance_desc),
        .model = abi.PWR_BUILTIN_MODEL_CONTROLLED_SHAFT,
    };
    for (0..64) |_| {
        var context: abi.pwr_context = 0;
        if (api.context_create.?(&desc, &context) != abi.PWR_STATUS_OK) {
            failed.store(true, .monotonic);
            return;
        }
        defer if (api.context_destroy.?(context) != abi.PWR_STATUS_OK) {
            failed.store(true, .monotonic);
        };
        var instance: abi.pwr_instance = 0;
        if (api.instance_create.?(context, &instance_desc, &instance) != abi.PWR_STATUS_OK) {
            failed.store(true, .monotonic);
            return;
        }
        if (api.context_destroy.?(context) != abi.PWR_STATUS_IN_USE or
            api.instance_step.?(instance, 100000) != abi.PWR_STATUS_OK or
            api.instance_destroy.?(instance) != abi.PWR_STATUS_OK or
            api.instance_step.?(instance, 100000) != abi.PWR_STATUS_INVALID_HANDLE)
        {
            failed.store(true, .monotonic);
            return;
        }
    }
}

test "concurrent native registries preserve lifetime and stale-handle checks" {
    var api: abi.pwr_api = .{};
    try std.testing.expectEqual(abi.PWR_STATUS_OK, api_module.pwr_get_api(1, @sizeOf(abi.pwr_api), &api));
    var failed = std.atomic.Value(bool).init(false);
    var threads: [8]std.Thread = undefined;
    var started: usize = 0;
    defer for (threads[0..started]) |thread| thread.join();
    for (&threads) |*thread| {
        thread.* = try std.Thread.spawn(.{}, exerciseRegistry, .{ &api, &failed });
        started += 1;
    }
    for (threads) |thread| thread.join();
    started = 0;
    try std.testing.expect(!failed.load(.monotonic));
}

test "native allocator rejects size overflow and zeroes aligned payloads" {
    try std.testing.expect(support.malloc(std.math.maxInt(usize)) == null);
    try std.testing.expect(support.calloc(std.math.maxInt(usize), 2) == null);
    const ptr = support.calloc(32, @sizeOf(f64)) orelse return error.OutOfMemory;
    defer support.free(ptr);
    try std.testing.expectEqual(@as(usize, 0), @intFromPtr(ptr) % 16);
    for (@as([*]u8, @ptrCast(ptr))[0..256]) |byte| try std.testing.expectEqual(@as(u8, 0), byte);
    support.free(null);
}

test "bounded native diagnostic copies and IEEE classification" {
    var buffer: [4]u8 = undefined;
    try std.testing.expectEqual(@as(c_int, 6), support.snprintf(&buffer, buffer.len, "%s", "abcdef"));
    try std.testing.expectEqualStrings("abc\x00", &buffer);
    try std.testing.expectEqual(@as(c_int, 1), support.power_isfinite(std.math.floatMax(f64)));
    try std.testing.expectEqual(@as(c_int, 0), support.power_isfinite(std.math.inf(f64)));
    try std.testing.expectEqual(@as(c_int, 0), support.power_isfinite(std.math.nan(f64)));
}
