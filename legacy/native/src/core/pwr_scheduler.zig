// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/core/pwr_scheduler.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_SCHEDULER_CALLBACK_FAILED = @import("../abi.zig").PWR_SCHEDULER_CALLBACK_FAILED;
const PWR_SCHEDULER_CAPACITY_EXCEEDED = @import("../abi.zig").PWR_SCHEDULER_CAPACITY_EXCEEDED;
const PWR_SCHEDULER_DUPLICATE_ID = @import("../abi.zig").PWR_SCHEDULER_DUPLICATE_ID;
const PWR_SCHEDULER_FAULTED = @import("../abi.zig").PWR_SCHEDULER_FAULTED;
const PWR_SCHEDULER_INVALID_ARGUMENT = @import("../abi.zig").PWR_SCHEDULER_INVALID_ARGUMENT;
const PWR_SCHEDULER_OK = @import("../abi.zig").PWR_SCHEDULER_OK;
const memset = @import("../support.zig").memset;
const pwr_scheduled_task = @import("../abi.zig").pwr_scheduled_task;
const pwr_scheduler = @import("../abi.zig").pwr_scheduler;
const pwr_scheduler_result = @import("../abi.zig").pwr_scheduler_result;
const pwr_task_callback = @import("../abi.zig").pwr_task_callback;
const pwr_task_desc = @import("../abi.zig").pwr_task_desc;

pub fn pwr_scheduler_init(arg_scheduler: [*c]pwr_scheduler, arg_base_tick_ns: u64) callconv(.c) pwr_scheduler_result {
    var scheduler = arg_scheduler;
    _ = &scheduler;
    var base_tick_ns = arg_base_tick_ns;
    _ = &base_tick_ns;
    if ((scheduler == null) or (base_tick_ns == @as(u64, @bitCast(@as(u64, @as(c_uint, 0)))))) {
        return @as(c_uint, @bitCast(PWR_SCHEDULER_INVALID_ARGUMENT));
    }
    _ = memset(@as(?*anyopaque, @ptrCast(scheduler)), @as(c_int, 0), @sizeOf(pwr_scheduler));
    scheduler.*.base_tick_ns = base_tick_ns;
    return @as(c_uint, @bitCast(PWR_SCHEDULER_OK));
}

pub fn pwr_scheduler_add_task(arg_scheduler: [*c]pwr_scheduler, arg_desc: [*c]const pwr_task_desc) callconv(.c) pwr_scheduler_result {
    var scheduler = arg_scheduler;
    _ = &scheduler;
    var desc = arg_desc;
    _ = &desc;
    if ((((((scheduler == null) or (desc == null)) or (desc.*.stable_id == @as(c_uint, 0))) or (desc.*.period_ticks == @as(c_uint, 0))) or (desc.*.phase_ticks >= desc.*.period_ticks)) or (desc.*.callback == null)) {
        return @as(c_uint, @bitCast(PWR_SCHEDULER_INVALID_ARGUMENT));
    }
    if (scheduler.*.faulted) {
        return @as(c_uint, @bitCast(PWR_SCHEDULER_FAULTED));
    }
    if (scheduler.*.task_count >= @as(usize, @bitCast(@as(u64, @as(c_uint, 64))))) {
        return @as(c_uint, @bitCast(PWR_SCHEDULER_CAPACITY_EXCEEDED));
    }
    {
        var i: usize = 0;
        _ = &i;
        while (i < scheduler.*.task_count) : (i +%= 1) {
            if (scheduler.*.tasks[i].desc.stable_id == desc.*.stable_id) {
                return @as(c_uint, @bitCast(PWR_SCHEDULER_DUPLICATE_ID));
            }
        }
    }
    var insert_at: usize = scheduler.*.task_count;
    _ = &insert_at;
    while ((insert_at > @as(usize, @bitCast(@as(u64, @as(c_uint, 0))))) and (@as(c_int, @intFromBool(pwr_task_precedes(desc, &scheduler.*.tasks[insert_at -% @as(usize, @bitCast(@as(u64, @as(c_uint, 1))))].desc))) != 0)) {
        scheduler.*.tasks[insert_at] = scheduler.*.tasks[insert_at -% @as(usize, @bitCast(@as(u64, @as(c_uint, 1))))];
        insert_at -%= 1;
    }
    scheduler.*.tasks[insert_at] = pwr_scheduled_task{
        .desc = desc.*,
        .run_count = @as(u64, @bitCast(@as(u64, @as(c_uint, 0)))),
        .last_run_tick = @as(u64, 18446744073709551615),
    };
    scheduler.*.task_count +%= 1;
    return @as(c_uint, @bitCast(PWR_SCHEDULER_OK));
}

pub fn pwr_scheduler_run_tick(arg_scheduler: [*c]pwr_scheduler) callconv(.c) pwr_scheduler_result {
    var scheduler = arg_scheduler;
    _ = &scheduler;
    if (scheduler == null) {
        return @as(c_uint, @bitCast(PWR_SCHEDULER_INVALID_ARGUMENT));
    }
    if (scheduler.*.faulted) {
        return @as(c_uint, @bitCast(PWR_SCHEDULER_FAULTED));
    }
    {
        var i: usize = 0;
        _ = &i;
        while (i < scheduler.*.task_count) : (i +%= 1) {
            var task: [*c]pwr_scheduled_task = &scheduler.*.tasks[i];
            _ = &task;
            const tick: u64 = scheduler.*.current_tick;
            _ = &tick;
            const phase: u64 = @as(u64, @bitCast(@as(u64, task.*.desc.phase_ticks)));
            _ = &phase;
            const period: u64 = @as(u64, @bitCast(@as(u64, task.*.desc.period_ticks)));
            _ = &period;
            const due: bool = (tick >= phase) and (((tick -% phase) % period) == @as(u64, @bitCast(@as(u64, @as(c_uint, 0)))));
            _ = &due;
            if (!due) {
                continue;
            }
            if (!task.*.desc.callback.?(task.*.desc.user, tick)) {
                scheduler.*.faulted = @as(c_int, 1) != 0;
                scheduler.*.failed_task_id = task.*.desc.stable_id;
                return @as(c_uint, @bitCast(PWR_SCHEDULER_CALLBACK_FAILED));
            }
            task.*.run_count +%= 1;
            task.*.last_run_tick = tick;
        }
    }
    scheduler.*.current_tick +%= 1;
    return @as(c_uint, @bitCast(PWR_SCHEDULER_OK));
}

pub fn pwr_scheduler_reset(arg_scheduler: [*c]pwr_scheduler) callconv(.c) void {
    var scheduler = arg_scheduler;
    _ = &scheduler;
    if (scheduler == null) {
        return;
    }
    scheduler.*.current_tick = 0;
    scheduler.*.faulted = @as(c_int, 0) != 0;
    scheduler.*.failed_task_id = 0;
    {
        var i: usize = 0;
        _ = &i;
        while (i < scheduler.*.task_count) : (i +%= 1) {
            scheduler.*.tasks[i].run_count = 0;
            scheduler.*.tasks[i].last_run_tick = @as(u64, 18446744073709551615);
        }
    }
}

pub fn pwr_scheduler_time_seconds(arg_scheduler: [*c]const pwr_scheduler) callconv(.c) f64 {
    var scheduler = arg_scheduler;
    _ = &scheduler;
    if (scheduler == null) {
        return 0.0;
    }
    return (@as(f64, @floatFromInt(scheduler.*.current_tick)) * @as(f64, @floatFromInt(scheduler.*.base_tick_ns))) * 0.000000001;
}

pub fn pwr_task_precedes(arg_lhs: [*c]const pwr_task_desc, arg_rhs: [*c]const pwr_task_desc) callconv(.c) bool {
    var lhs = arg_lhs;
    _ = &lhs;
    var rhs = arg_rhs;
    _ = &rhs;
    if (@as(c_int, @bitCast(@as(c_uint, lhs.*.priority))) != @as(c_int, @bitCast(@as(c_uint, rhs.*.priority)))) {
        return @as(c_int, @bitCast(@as(c_uint, lhs.*.priority))) < @as(c_int, @bitCast(@as(c_uint, rhs.*.priority)));
    }
    return lhs.*.stable_id < rhs.*.stable_id;
}
