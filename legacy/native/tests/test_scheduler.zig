// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/tests/test_scheduler.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_SCHEDULER_CALLBACK_FAILED = @import("../src/abi.zig").PWR_SCHEDULER_CALLBACK_FAILED;
const PWR_SCHEDULER_DUPLICATE_ID = @import("../src/abi.zig").PWR_SCHEDULER_DUPLICATE_ID;
const PWR_SCHEDULER_FAULTED = @import("../src/abi.zig").PWR_SCHEDULER_FAULTED;
const PWR_SCHEDULER_INVALID_ARGUMENT = @import("../src/abi.zig").PWR_SCHEDULER_INVALID_ARGUMENT;
const PWR_SCHEDULER_OK = @import("../src/abi.zig").PWR_SCHEDULER_OK;
const power_assert = @import("../src/support.zig").power_assert;
const pwr_scheduler = @import("../src/abi.zig").pwr_scheduler;
const pwr_scheduler_add_task = @import("../src/core/pwr_scheduler.zig").pwr_scheduler_add_task;
const pwr_scheduler_init = @import("../src/core/pwr_scheduler.zig").pwr_scheduler_init;
const pwr_scheduler_reset = @import("../src/core/pwr_scheduler.zig").pwr_scheduler_reset;
const pwr_scheduler_run_tick = @import("../src/core/pwr_scheduler.zig").pwr_scheduler_run_tick;
const pwr_scheduler_time_seconds = @import("../src/core/pwr_scheduler.zig").pwr_scheduler_time_seconds;
const pwr_task_desc = @import("../src/abi.zig").pwr_task_desc;

pub fn record_task(arg_user: ?*anyopaque, arg_tick: u64) callconv(.c) bool {
    var user = arg_user;
    _ = &user;
    var tick = arg_tick;
    _ = &tick;
    var task_trace: [*c]trace = @as([*c]trace, @ptrCast(@alignCast(user)));
    _ = &task_trace;
    power_assert(@intFromBool(!!(task_trace != null)), "task_trace != NULL", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 20))));
    power_assert(@intFromBool(!!(task_trace.*.count < (@sizeOf([32]u32) / @sizeOf(u32)))), "task_trace->count < (sizeof(task_trace->values) / sizeof(task_trace->values[0]))", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 21))));
    task_trace.*.values[
        blk: {
            const ref = &task_trace.*.count;
            const tmp = ref.*;
            ref.* +%= 1;
            break :blk tmp;
        }
    ] = task_trace.*.marker +% @as(u32, @bitCast(@as(c_uint, @truncate(tick))));
    return !task_trace.*.should_fail;
}

pub fn run() c_int {
    var scheduler: pwr_scheduler = undefined;
    _ = &scheduler;
    power_assert(@intFromBool(!!(pwr_scheduler_init(&scheduler, @as(u64, @bitCast(@as(u64, @as(c_uint, 100000))))) == @as(c_uint, @bitCast(PWR_SCHEDULER_OK)))), "pwr_scheduler_init(&scheduler, 100000U) == PWR_SCHEDULER_OK", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 28))));
    power_assert(@intFromBool(!!(pwr_scheduler_init(null, @as(u64, @bitCast(@as(u64, @as(c_uint, 100000))))) == @as(c_uint, @bitCast(PWR_SCHEDULER_INVALID_ARGUMENT)))), "pwr_scheduler_init(NULL, 100000U) == PWR_SCHEDULER_INVALID_ARGUMENT", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 29))));
    var late: trace = trace{
        .values = @import("std").mem.zeroes([32]u32),
        .count = @import("std").mem.zeroes(usize),
        .should_fail = false,
        .marker = @as(c_uint, 1000),
    };
    _ = &late;
    var first: trace = trace{
        .values = @import("std").mem.zeroes([32]u32),
        .count = @import("std").mem.zeroes(usize),
        .should_fail = false,
        .marker = @as(c_uint, 100),
    };
    _ = &first;
    var tied: trace = trace{
        .values = @import("std").mem.zeroes([32]u32),
        .count = @import("std").mem.zeroes(usize),
        .should_fail = false,
        .marker = @as(c_uint, 200),
    };
    _ = &tied;
    const late_desc: pwr_task_desc = pwr_task_desc{
        .stable_id = @as(c_uint, 30),
        .period_ticks = @as(c_uint, 2),
        .phase_ticks = @as(c_uint, 0),
        .priority = @as(u16, @bitCast(@as(c_ushort, @truncate(@as(c_uint, 20))))),
        .callback = &record_task,
        .user = @as(?*anyopaque, @ptrCast(&late)),
    };
    _ = &late_desc;
    const tied_desc: pwr_task_desc = pwr_task_desc{
        .stable_id = @as(c_uint, 20),
        .period_ticks = @as(c_uint, 1),
        .phase_ticks = @as(c_uint, 0),
        .priority = @as(u16, @bitCast(@as(c_ushort, @truncate(@as(c_uint, 10))))),
        .callback = &record_task,
        .user = @as(?*anyopaque, @ptrCast(&tied)),
    };
    _ = &tied_desc;
    const first_desc: pwr_task_desc = pwr_task_desc{
        .stable_id = @as(c_uint, 10),
        .period_ticks = @as(c_uint, 1),
        .phase_ticks = @as(c_uint, 0),
        .priority = @as(u16, @bitCast(@as(c_ushort, @truncate(@as(c_uint, 10))))),
        .callback = &record_task,
        .user = @as(?*anyopaque, @ptrCast(&first)),
    };
    _ = &first_desc;
    power_assert(@intFromBool(!!(pwr_scheduler_add_task(&scheduler, &late_desc) == @as(c_uint, @bitCast(PWR_SCHEDULER_OK)))), "pwr_scheduler_add_task(&scheduler, &late_desc) == PWR_SCHEDULER_OK", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 60))));
    power_assert(@intFromBool(!!(pwr_scheduler_add_task(&scheduler, &tied_desc) == @as(c_uint, @bitCast(PWR_SCHEDULER_OK)))), "pwr_scheduler_add_task(&scheduler, &tied_desc) == PWR_SCHEDULER_OK", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 61))));
    power_assert(@intFromBool(!!(pwr_scheduler_add_task(&scheduler, &first_desc) == @as(c_uint, @bitCast(PWR_SCHEDULER_OK)))), "pwr_scheduler_add_task(&scheduler, &first_desc) == PWR_SCHEDULER_OK", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 62))));
    power_assert(@intFromBool(!!(pwr_scheduler_add_task(&scheduler, &first_desc) == @as(c_uint, @bitCast(PWR_SCHEDULER_DUPLICATE_ID)))), "pwr_scheduler_add_task(&scheduler, &first_desc) == PWR_SCHEDULER_DUPLICATE_ID", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 63))));
    power_assert(@intFromBool(!!(scheduler.tasks[@as(c_uint, @intCast(@as(c_int, 0)))].desc.stable_id == @as(c_uint, 10))), "scheduler.tasks[0].desc.stable_id == 10U", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 65))));
    power_assert(@intFromBool(!!(scheduler.tasks[@as(c_uint, @intCast(@as(c_int, 1)))].desc.stable_id == @as(c_uint, 20))), "scheduler.tasks[1].desc.stable_id == 20U", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 66))));
    power_assert(@intFromBool(!!(scheduler.tasks[@as(c_uint, @intCast(@as(c_int, 2)))].desc.stable_id == @as(c_uint, 30))), "scheduler.tasks[2].desc.stable_id == 30U", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 67))));
    power_assert(@intFromBool(!!(pwr_scheduler_run_tick(&scheduler) == @as(c_uint, @bitCast(PWR_SCHEDULER_OK)))), "pwr_scheduler_run_tick(&scheduler) == PWR_SCHEDULER_OK", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 69))));
    power_assert(@intFromBool(!!(first.values[@as(c_uint, @intCast(@as(c_int, 0)))] == @as(c_uint, 100))), "first.values[0] == 100U", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 70))));
    power_assert(@intFromBool(!!(tied.values[@as(c_uint, @intCast(@as(c_int, 0)))] == @as(c_uint, 200))), "tied.values[0] == 200U", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 71))));
    power_assert(@intFromBool(!!(late.values[@as(c_uint, @intCast(@as(c_int, 0)))] == @as(c_uint, 1000))), "late.values[0] == 1000U", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 72))));
    power_assert(@intFromBool(!!(pwr_scheduler_run_tick(&scheduler) == @as(c_uint, @bitCast(PWR_SCHEDULER_OK)))), "pwr_scheduler_run_tick(&scheduler) == PWR_SCHEDULER_OK", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 74))));
    power_assert(@intFromBool(!!(first.count == @as(usize, @bitCast(@as(u64, @as(c_uint, 2)))))), "first.count == 2U", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 75))));
    power_assert(@intFromBool(!!(tied.count == @as(usize, @bitCast(@as(u64, @as(c_uint, 2)))))), "tied.count == 2U", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 76))));
    power_assert(@intFromBool(!!(late.count == @as(usize, @bitCast(@as(u64, @as(c_uint, 1)))))), "late.count == 1U", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 77))));
    power_assert(@intFromBool(!!(pwr_scheduler_time_seconds(&scheduler) == 0.0002)), "pwr_scheduler_time_seconds(&scheduler) == 0.0002", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 78))));
    tied.should_fail = @as(c_int, 1) != 0;
    power_assert(@intFromBool(!!(pwr_scheduler_run_tick(&scheduler) == @as(c_uint, @bitCast(PWR_SCHEDULER_CALLBACK_FAILED)))), "pwr_scheduler_run_tick(&scheduler) == PWR_SCHEDULER_CALLBACK_FAILED", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 81))));
    power_assert(@intFromBool(!!(scheduler.failed_task_id == @as(c_uint, 20))), "scheduler.failed_task_id == 20U", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 82))));
    power_assert(@intFromBool(!!(pwr_scheduler_run_tick(&scheduler) == @as(c_uint, @bitCast(PWR_SCHEDULER_FAULTED)))), "pwr_scheduler_run_tick(&scheduler) == PWR_SCHEDULER_FAULTED", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 83))));
    pwr_scheduler_reset(&scheduler);
    tied.should_fail = @as(c_int, 0) != 0;
    power_assert(@intFromBool(!!(scheduler.current_tick == @as(u64, @bitCast(@as(u64, @as(c_uint, 0)))))), "scheduler.current_tick == 0U", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 87))));
    power_assert(@intFromBool(!!(pwr_scheduler_run_tick(&scheduler) == @as(c_uint, @bitCast(PWR_SCHEDULER_OK)))), "pwr_scheduler_run_tick(&scheduler) == PWR_SCHEDULER_OK", "legacy/native/tests/test_scheduler.c", @as(c_uint, @bitCast(@as(c_int, 88))));
    return 0;
}

pub const trace = struct_trace;

pub const struct_trace = extern struct {
    values: [32]u32 = @import("std").mem.zeroes([32]u32),
    count: usize = @import("std").mem.zeroes(usize),
    should_fail: bool = @import("std").mem.zeroes(bool),
    marker: u32 = @import("std").mem.zeroes(u32),
};

pub fn main() void {
    @import("std").process.exit(@intCast(run()));
}
