// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

const std = @import("std");
comptime {
    _ = @import("tests/test_automatic_powertrain.zig");
}
comptime {
    _ = @import("tests/test_native_contracts.zig");
}
test "automatic" {
    try std.testing.expectEqual(@as(c_int, 0), @import("tests/test_automatic.zig").run());
}
test "dct" {
    try std.testing.expectEqual(@as(c_int, 0), @import("tests/test_dct.zig").run());
}
test "dct_powertrain" {
    try std.testing.expectEqual(@as(c_int, 0), @import("tests/test_dct_powertrain.zig").run());
}
test "exhaust" {
    try std.testing.expectEqual(@as(c_int, 0), @import("tests/test_exhaust.zig").run());
}
test "graph" {
    try std.testing.expectEqual(@as(c_int, 0), @import("tests/test_graph.zig").run());
}
test "model_sdk" {
    try std.testing.expectEqual(@as(c_int, 0), @import("tests/test_model_sdk.zig").run());
}
test "scheduler" {
    try std.testing.expectEqual(@as(c_int, 0), @import("tests/test_scheduler.zig").run());
}
test "sdk" {
    try std.testing.expectEqual(@as(c_int, 0), @import("tests/test_sdk.zig").run());
}
test "shaft" {
    try std.testing.expectEqual(@as(c_int, 0), @import("tests/test_shaft.zig").run());
}
test "si_engine" {
    try std.testing.expectEqual(@as(c_int, 0), @import("tests/test_si_engine.zig").run());
}
