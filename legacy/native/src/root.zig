// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

pub const abi = @import("abi.zig");
// A shared library has no Zig executable startup/TLS state. Safety traps must
// not enter the executable's stack-trace machinery or require a C runtime.
pub const panic = @import("std").debug.FullPanic(panicTrap);
fn panicTrap(message: []const u8, _: ?usize) noreturn {
    @import("std").fs.File.stderr().writeAll(message) catch {};
    @trap();
}
pub export fn pwr_get_api(version: u32, size: u32, api: [*c]abi.pwr_api) abi.pwr_status {
    return @import("sdk/pwr_api.zig").pwr_get_api(version, size, api);
}
