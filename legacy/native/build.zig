// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

const std = @import("std");
comptime {
    if (!std.mem.eql(u8, @import("builtin").zig_version_string, "0.15.2"))
        @compileError("Power! native builds require Zig 0.15.2; see tools/InstallZig.py.");
}
pub fn build(b: *std.Build) void {
    const target = b.standardTargetOptions(.{});
    var host_query = target.query;
    // Without libc, Zig requires an explicit ELF interpreter for shared-library
    // hosts. Respect a user override and otherwise use the target's loader.
    if (target.result.os.tag == .linux and host_query.dynamic_linker.get() == null) {
        host_query.dynamic_linker = if (target.result.dynamic_linker.get() != null)
            target.result.dynamic_linker
        else
            target.result.standardDynamicLinkerPath();
    }
    const host_target = b.resolveTargetQuery(host_query);
    const optimize = b.standardOptimizeOption(.{});
    const library = b.addLibrary(.{
        .name = "power",
        .linkage = .dynamic,
        .root_module = b.createModule(.{ .root_source_file = b.path("src/root.zig"), .target = target, .optimize = optimize }),
    });
    // There are no C compilation units requiring the C UBSan runtime. Zig's
    // own safety checks remain enabled in Debug and ReleaseSafe.
    library.bundle_ubsan_rt = false;
    b.installArtifact(library);
    for ([_][]const u8{ "LICENSE", "COPYING.NOTICE", "UNITY-LINKING-EXCEPTION.md", "THIRD_PARTY_NOTICES.md" }) |name|
        b.installFile(b.fmt("../../{s}", .{name}), b.fmt("share/licenses/power/{s}", .{name}));
    for ([_][]const u8{ "Zig-MIT.txt", "Go-BSD-3-Clause.txt", "musl-COPYRIGHT.txt" }) |name|
        b.installFile(b.fmt("../../licenses/{s}", .{name}), b.fmt("share/licenses/power/licenses/{s}", .{name}));
    const tests = b.addTest(.{
        .root_module = b.createModule(.{ .root_source_file = b.path("tests.zig"), .target = target, .optimize = optimize }),
    });
    const run = b.addRunArtifact(tests);
    b.step("test", "Run native physics and SDK regression scenarios").dependOn(&run.step);
    for ([_][]const u8{ "host", "model_host" }) |name| {
        const executable = b.addExecutable(.{
            .name = b.fmt("power_{s}", .{name}),
            .linkage = .dynamic,
            .root_module = b.createModule(.{ .root_source_file = b.path(b.fmt("{s}.zig", .{name})), .target = host_target, .optimize = optimize }),
        });
        executable.root_module.linkLibrary(library);
        if (target.result.os.tag == .linux) executable.root_module.addRPathSpecial("$ORIGIN/../lib");
        if (target.result.os.tag == .macos) executable.root_module.addRPathSpecial("@executable_path/../lib");
        b.installArtifact(executable);
    }
}
