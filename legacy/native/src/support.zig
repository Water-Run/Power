// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Zig implementations of the primitives used by the port. No libc is linked.
const std = @import("std");

pub fn fabs(x: f64) f64 {
    return @abs(x);
}
pub fn fmax(x: f64, y: f64) f64 {
    return @max(x, y);
}
pub fn fmin(x: f64, y: f64) f64 {
    return @min(x, y);
}
pub fn ceil(x: f64) f64 {
    return @ceil(x);
}
pub fn cos(x: f64) f64 {
    return @cos(x);
}
pub fn sin(x: f64) f64 {
    return @sin(x);
}
pub fn exp(x: f64) f64 {
    return @exp(x);
}
pub fn sqrt(x: f64) f64 {
    return @sqrt(x);
}
pub fn pow(x: f64, y: f64) f64 {
    return std.math.pow(f64, x, y);
}
pub fn tanh(x: f64) f64 {
    return std.math.tanh(x);
}
pub fn acos(x: f64) f64 {
    return std.math.acos(x);
}
pub fn fmod(x: f64, y: f64) f64 {
    return @rem(x, y);
}
pub fn power_isfinite(x: f64) c_int {
    return @intFromBool(std.math.isFinite(x));
}
pub fn __builtin_nanf(_: anytype) f32 {
    return std.math.nan(f32);
}
pub fn __builtin_inff() f32 {
    return std.math.inf(f32);
}

pub fn memset(dest: ?*anyopaque, value: c_int, count: usize) ?*anyopaque {
    if (count != 0) @memset(@as([*]u8, @ptrCast(dest.?))[0..count], @truncate(@as(c_uint, @bitCast(value))));
    return dest;
}
pub fn memcpy(dest: ?*anyopaque, source: ?*const anyopaque, count: usize) ?*anyopaque {
    if (count != 0) @memcpy(@as([*]u8, @ptrCast(dest.?))[0..count], @as([*]const u8, @ptrCast(source.?))[0..count]);
    return dest;
}
pub fn memcmp(a: ?*const anyopaque, b: ?*const anyopaque, count: usize) c_int {
    if (count == 0) return 0;
    return switch (std.mem.order(u8, @as([*]const u8, @ptrCast(a.?))[0..count], @as([*]const u8, @ptrCast(b.?))[0..count])) {
        .lt => -1,
        .eq => 0,
        .gt => 1,
    };
}
pub fn strcmp(a: [*c]const u8, b: [*c]const u8) c_int {
    return switch (std.mem.order(u8, std.mem.span(a), std.mem.span(b))) {
        .lt => -1,
        .eq => 0,
        .gt => 1,
    };
}

// Store the allocation length before the aligned payload so generic backend
// destructors can release memory without changing the binary function table.
const header_size = 16;
pub fn malloc(count: usize) ?*anyopaque {
    const total = std.math.add(usize, count, header_size) catch return null;
    // Linux mmap/munmap accept byte lengths and perform page rounding. Using
    // them here avoids querying executable auxiliary-vector state in a DSO.
    const bytes = if (@import("builtin").os.tag == .linux)
        std.posix.mmap(null, total, std.posix.PROT.READ | std.posix.PROT.WRITE, .{ .TYPE = .PRIVATE, .ANONYMOUS = true }, -1, 0) catch return null
    else
        std.heap.page_allocator.alignedAlloc(u8, .@"16", total) catch return null;
    @as(*usize, @ptrCast(bytes.ptr)).* = total;
    return bytes.ptr + header_size;
}
pub fn calloc(count: usize, size: usize) ?*anyopaque {
    const total = std.math.mul(usize, count, size) catch return null;
    const ptr = malloc(total) orelse return null;
    _ = memset(ptr, 0, total);
    return ptr;
}
pub fn free(ptr: ?*anyopaque) callconv(.c) void {
    if (ptr) |value| {
        const base: [*]align(16) u8 = @alignCast(@as([*]u8, @ptrCast(value)) - header_size);
        const size = @as(*const usize, @ptrCast(base)).*;
        if (@import("builtin").os.tag == .linux) std.posix.munmap(@alignCast(base[0..size])) else std.heap.page_allocator.free(base[0..size]);
    }
}

pub fn atomic_flag_test_and_set_explicit(flag: [*c]bool, _: c_int) bool {
    return @atomicRmw(bool, &flag[0], .Xchg, true, .acquire);
}
pub fn atomic_flag_clear_explicit(flag: [*c]bool, _: c_int) void {
    @atomicStore(bool, &flag[0], false, .release);
}
pub fn atomic_compare_exchange_strong_explicit(flag: [*c]bool, expected: [*c]bool, value: bool, _: c_int, _: c_int) bool {
    if (@cmpxchgStrong(bool, &flag[0], expected.*, value, .acquire, .monotonic)) |actual| {
        expected.* = actual;
        return false;
    }
    return true;
}
pub fn atomic_store_explicit(flag: [*c]bool, value: bool, order: c_int) void {
    if (order == 2) @atomicStore(bool, &flag[0], value, .release) else @atomicStore(bool, &flag[0], value, .monotonic);
}
pub fn atomic_load_explicit(flag: [*c]const bool, _: c_int) bool {
    return @atomicLoad(bool, &flag[0], .acquire);
}

pub fn snprintf(dest: [*c]u8, size: usize, comptime format: []const u8, text: [*c]const u8) c_int {
    if (comptime !std.mem.eql(u8, format, "%s")) @compileError("Only bounded text copies are used by native diagnostics.");
    const source = std.mem.span(text);
    if (size != 0) {
        const count = @min(source.len, size - 1);
        @memcpy(dest[0..count], source[0..count]);
        dest[count] = 0;
    }
    return @intCast(source.len);
}

pub fn printf(comptime format: []const u8, args: anytype) c_int {
    if (@import("builtin").is_test) {
        std.debug.print(format, args);
        return 0;
    }
    var buffer: [8192]u8 = undefined;
    const bytes = std.fmt.bufPrint(&buffer, format, args) catch @panic("Native host output exceeds buffer.");
    std.fs.File.stdout().writeAll(bytes) catch @panic("Cannot write native host output.");
    return @intCast(bytes.len);
}
pub fn fprintf(comptime format: []const u8, args: anytype) c_int {
    std.debug.print(format, args);
    return 0;
}
pub fn puts(text: [*c]const u8) c_int {
    return printf("{s}\n", .{std.mem.span(text)});
}
pub fn power_assert(condition: c_int, expression: [*c]const u8, file: [*c]const u8, line: c_uint) void {
    if (condition == 0) {
        std.debug.print("{s}:{d}: assertion failed: {s}\n", .{ std.mem.span(file), line, std.mem.span(expression) });
        @panic("Native regression assertion failed.");
    }
}
