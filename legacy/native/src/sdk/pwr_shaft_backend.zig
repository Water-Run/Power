// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/sdk/pwr_shaft_backend.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_SHAFT_FAULT_ACTUATOR_STUCK = @import("../abi.zig").PWR_SHAFT_FAULT_ACTUATOR_STUCK;
const PWR_SHAFT_FAULT_LV_BROWNOUT = @import("../abi.zig").PWR_SHAFT_FAULT_LV_BROWNOUT;
const PWR_SHAFT_FAULT_SIGNAL_DROP = @import("../abi.zig").PWR_SHAFT_FAULT_SIGNAL_DROP;
const PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS = @import("../abi.zig").PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS;
const PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT = @import("../abi.zig").PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT;
const PWR_SHAFT_OK = @import("../abi.zig").PWR_SHAFT_OK;
const PWR_SHAFT_SNAPSHOT_VALUE_COUNT = @import("../abi.zig").PWR_SHAFT_SNAPSHOT_VALUE_COUNT;
const PWR_STATUS_CAPACITY_EXCEEDED = @import("../abi.zig").PWR_STATUS_CAPACITY_EXCEEDED;
const PWR_STATUS_INTERNAL_ERROR = @import("../abi.zig").PWR_STATUS_INTERNAL_ERROR;
const PWR_STATUS_INVALID_ARGUMENT = @import("../abi.zig").PWR_STATUS_INVALID_ARGUMENT;
const PWR_STATUS_INVALID_TIME_STEP = @import("../abi.zig").PWR_STATUS_INVALID_TIME_STEP;
const PWR_STATUS_NUMERIC_ERROR = @import("../abi.zig").PWR_STATUS_NUMERIC_ERROR;
const PWR_STATUS_OK = @import("../abi.zig").PWR_STATUS_OK;
const PWR_STATUS_OUT_OF_MEMORY = @import("../abi.zig").PWR_STATUS_OUT_OF_MEMORY;
const PWR_STATUS_UNKNOWN_CHANNEL = @import("../abi.zig").PWR_STATUS_UNKNOWN_CHANNEL;
const PWR_STATUS_UNSUPPORTED = @import("../abi.zig").PWR_STATUS_UNSUPPORTED;
const calloc = @import("../support.zig").calloc;
const free = @import("../support.zig").free;
const power_isfinite = @import("../support.zig").power_isfinite;
const pwr_channel_id = @import("../abi.zig").pwr_channel_id;
const pwr_input_frame = @import("../abi.zig").pwr_input_frame;
const pwr_instance_backend = @import("../abi.zig").pwr_instance_backend;
const pwr_scalar_value = @import("../abi.zig").pwr_scalar_value;
const pwr_shaft_config = @import("../abi.zig").pwr_shaft_config;
const pwr_shaft_config_default = @import("../models/shaft/pwr_shaft_lab.zig").pwr_shaft_config_default;
const pwr_shaft_input = @import("../abi.zig").pwr_shaft_input;
const pwr_shaft_lab_init = @import("../models/shaft/pwr_shaft_lab.zig").pwr_shaft_lab_init;
const pwr_shaft_lab_snapshot = @import("../models/shaft/pwr_shaft_lab.zig").pwr_shaft_lab_snapshot;
const pwr_shaft_lab_step = @import("../models/shaft/pwr_shaft_lab.zig").pwr_shaft_lab_step;
const pwr_shaft_runtime = @import("../abi.zig").pwr_shaft_runtime;
const pwr_shaft_snapshot = @import("../abi.zig").pwr_shaft_snapshot;
const pwr_snapshot = @import("../abi.zig").pwr_snapshot;
const pwr_status = @import("../abi.zig").pwr_status;

pub fn pwr_shaft_backend_create(arg_state: [*c]?*anyopaque) callconv(.c) pwr_status {
    var state = arg_state;
    _ = &state;
    var runtime: [*c]pwr_shaft_runtime = @as([*c]pwr_shaft_runtime, @ptrCast(@alignCast(calloc(@as(u64, @bitCast(@as(i64, @as(c_int, 1)))), @sizeOf(pwr_shaft_runtime)))));
    _ = &runtime;
    if (runtime == null) {
        return PWR_STATUS_OUT_OF_MEMORY;
    }
    var config: pwr_shaft_config = undefined;
    _ = &config;
    pwr_shaft_config_default(&config);
    if (pwr_shaft_lab_init(&runtime.*.shaft, &config) != @as(c_uint, @bitCast(PWR_SHAFT_OK))) {
        free(@as(?*anyopaque, @ptrCast(runtime)));
        return PWR_STATUS_INTERNAL_ERROR;
    }
    state.* = @as(?*anyopaque, @ptrCast(runtime));
    return PWR_STATUS_OK;
}

pub fn pwr_scalar_is_boolean(arg_value: f64) callconv(.c) bool {
    var value = arg_value;
    _ = &value;
    return (value == 0.0) or (value == 1.0);
}

pub fn pwr_set_fault(arg_fault_mask: [*c]u32, arg_bit: u32, arg_enabled: bool) callconv(.c) void {
    var fault_mask = arg_fault_mask;
    _ = &fault_mask;
    var bit = arg_bit;
    _ = &bit;
    var enabled = arg_enabled;
    _ = &enabled;
    if (enabled) {
        fault_mask.* |= bit;
    } else {
        fault_mask.* &= ~bit;
    }
}

pub fn pwr_apply_input_value(arg_input: [*c]pwr_shaft_input, arg_value: [*c]const pwr_scalar_value) callconv(.c) pwr_status {
    var input = arg_input;
    _ = &input;
    var value = arg_value;
    _ = &value;
    if (!(power_isfinite(value.*.value) != 0)) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if ((value.*.flags != @as(c_uint, 0)) or (value.*.reserved != @as(c_uint, 0))) {
        return PWR_STATUS_UNSUPPORTED;
    }
    while (true) {
        switch (value.*.channel) {
            @as(u64, 1) => {
                if (value.*.value < 0.0) {
                    return PWR_STATUS_INVALID_ARGUMENT;
                }
                input.*.target_speed_rad_s = value.*.value;
                return PWR_STATUS_OK;
            },
            @as(u64, 2) => {
                if (value.*.value < 0.0) {
                    return PWR_STATUS_INVALID_ARGUMENT;
                }
                input.*.load_torque_nm = value.*.value;
                return PWR_STATUS_OK;
            },
            @as(u64, 3) => {
                input.*.speed_sensor_bias_rad_s = value.*.value;
                pwr_set_fault(&input.*.fault_mask, @as(u32, @bitCast(PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS)), value.*.value != 0.0);
                return PWR_STATUS_OK;
            },
            @as(u64, 4) => {
                if (!pwr_scalar_is_boolean(value.*.value)) {
                    return PWR_STATUS_INVALID_ARGUMENT;
                }
                pwr_set_fault(&input.*.fault_mask, @as(u32, @bitCast(PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT)), value.*.value != 0.0);
                return PWR_STATUS_OK;
            },
            @as(u64, 5) => {
                if (!pwr_scalar_is_boolean(value.*.value)) {
                    return PWR_STATUS_INVALID_ARGUMENT;
                }
                pwr_set_fault(&input.*.fault_mask, @as(u32, @bitCast(PWR_SHAFT_FAULT_SIGNAL_DROP)), value.*.value != 0.0);
                return PWR_STATUS_OK;
            },
            @as(u64, 6) => {
                if (!pwr_scalar_is_boolean(value.*.value)) {
                    return PWR_STATUS_INVALID_ARGUMENT;
                }
                pwr_set_fault(&input.*.fault_mask, @as(u32, @bitCast(PWR_SHAFT_FAULT_ACTUATOR_STUCK)), value.*.value != 0.0);
                return PWR_STATUS_OK;
            },
            @as(u64, 7) => {
                if ((value.*.value < -1.0) or (value.*.value > 1.0)) {
                    return PWR_STATUS_INVALID_ARGUMENT;
                }
                input.*.actuator_stuck_duty = value.*.value;
                return PWR_STATUS_OK;
            },
            @as(u64, 8) => {
                if (!pwr_scalar_is_boolean(value.*.value)) {
                    return PWR_STATUS_INVALID_ARGUMENT;
                }
                pwr_set_fault(&input.*.fault_mask, @as(u32, @bitCast(PWR_SHAFT_FAULT_LV_BROWNOUT)), value.*.value != 0.0);
                return PWR_STATUS_OK;
            },
            else => return PWR_STATUS_UNKNOWN_CHANNEL,
        }
        break;
    }
    return @import("std").mem.zeroes(pwr_status);
}

pub fn pwr_shaft_submit(arg_opaque: ?*anyopaque, arg_frame: [*c]const pwr_input_frame) callconv(.c) pwr_status {
    var @"opaque" = arg_opaque;
    _ = &@"opaque";
    var frame = arg_frame;
    _ = &frame;
    var runtime: [*c]pwr_shaft_runtime = @as([*c]pwr_shaft_runtime, @ptrCast(@alignCast(@"opaque")));
    _ = &runtime;
    var candidate: pwr_shaft_input = runtime.*.input;
    _ = &candidate;
    {
        var index: u32 = 0;
        _ = &index;
        while (index < frame.*.value_count) : (index +%= 1) {
            const status: pwr_status = pwr_apply_input_value(&candidate, &frame.*.values[index]);
            _ = &status;
            if (status != PWR_STATUS_OK) {
                return status;
            }
        }
    }
    runtime.*.input = candidate;
    return PWR_STATUS_OK;
}

pub fn pwr_shaft_step(arg_opaque: ?*anyopaque, arg_delta_ns: u64) callconv(.c) pwr_status {
    var @"opaque" = arg_opaque;
    _ = &@"opaque";
    var delta_ns = arg_delta_ns;
    _ = &delta_ns;
    var runtime: [*c]pwr_shaft_runtime = @as([*c]pwr_shaft_runtime, @ptrCast(@alignCast(@"opaque")));
    _ = &runtime;
    const tick_ns: u64 = runtime.*.shaft.config.base_tick_ns;
    _ = &tick_ns;
    const ticks: u64 = delta_ns / tick_ns;
    _ = &ticks;
    if (((((delta_ns == @as(u64, @bitCast(@as(u64, @as(c_uint, 0))))) or ((delta_ns % tick_ns) != @as(u64, @bitCast(@as(u64, @as(c_uint, 0)))))) or (ticks > @as(u64, 1000000))) or (runtime.*.shaft.scheduler.current_tick > (@as(u64, 18446744073709551615) / tick_ns))) or ((@as(u64, 18446744073709551615) -% (runtime.*.shaft.scheduler.current_tick *% tick_ns)) < delta_ns)) {
        return PWR_STATUS_INVALID_TIME_STEP;
    }
    {
        var i: u64 = 0;
        _ = &i;
        while (i < ticks) : (i +%= 1) {
            if (pwr_shaft_lab_step(&runtime.*.shaft, &runtime.*.input) != @as(c_uint, @bitCast(PWR_SHAFT_OK))) {
                return PWR_STATUS_NUMERIC_ERROR;
            }
        }
    }
    return PWR_STATUS_OK;
}

pub fn pwr_write_snapshot_value(arg_value: [*c]pwr_scalar_value, arg_channel: pwr_channel_id, arg_scalar: f64) callconv(.c) void {
    var value = arg_value;
    _ = &value;
    var channel = arg_channel;
    _ = &channel;
    var scalar = arg_scalar;
    _ = &scalar;
    value.* = pwr_scalar_value{
        .channel = channel,
        .value = scalar,
        .flags = @as(c_uint, 0),
        .reserved = @as(c_uint, 0),
    };
}

pub fn pwr_shaft_read_snapshot(arg_opaque: ?*const anyopaque, arg_snapshot: [*c]pwr_snapshot) callconv(.c) pwr_status {
    var @"opaque" = arg_opaque;
    _ = &@"opaque";
    var snapshot = arg_snapshot;
    _ = &snapshot;
    var runtime: [*c]const pwr_shaft_runtime = @as([*c]const pwr_shaft_runtime, @ptrCast(@alignCast(@"opaque")));
    _ = &runtime;
    var shaft: pwr_shaft_snapshot = undefined;
    _ = &shaft;
    if (pwr_shaft_lab_snapshot(&runtime.*.shaft, &shaft) != @as(c_uint, @bitCast(PWR_SHAFT_OK))) {
        return PWR_STATUS_INTERNAL_ERROR;
    }
    snapshot.*.simulation_time_ns = runtime.*.shaft.scheduler.current_tick *% runtime.*.shaft.config.base_tick_ns;
    snapshot.*.state_hash = shaft.state_hash;
    snapshot.*.diagnostic_bits = @as(u64, @bitCast(@as(u64, shaft.diagnostic_flags)));
    snapshot.*.value_count = @as(u32, @bitCast(PWR_SHAFT_SNAPSHOT_VALUE_COUNT));
    if ((snapshot.*.values == null) or (snapshot.*.value_capacity < @as(u32, @bitCast(PWR_SHAFT_SNAPSHOT_VALUE_COUNT)))) {
        return PWR_STATUS_CAPACITY_EXCEEDED;
    }
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 0)))], @as(u64, 1001), shaft.motor_speed_rad_s);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 1)))], @as(u64, 1002), shaft.load_speed_rad_s);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 2)))], @as(u64, 1003), shaft.shaft_twist_rad);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 3)))], @as(u64, 1004), shaft.motor_current_a);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 4)))], @as(u64, 1005), shaft.commanded_duty);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 5)))], @as(u64, 1006), shaft.applied_duty);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 6)))], @as(u64, 1007), shaft.sensed_speed_rad_s);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 7)))], @as(u64, 1008), shaft.low_voltage_v);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 8)))], @as(u64, 1009), shaft.motor_temperature_k);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 9)))], @as(u64, 1010), shaft.electrical_input_j);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 10)))], @as(u64, 1011), shaft.mechanical_output_j);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 11)))], @as(u64, 1012), shaft.heat_rejected_j);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 12)))], @as(u64, 1013), shaft.stored_energy_change_j);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 13)))], @as(u64, 1014), shaft.numerical_adjustment_j);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 14)))], @as(u64, 1015), shaft.energy_residual_j);
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 15)))], @as(u64, 1016), if (shaft.sensor_age_ticks == @as(u64, 18446744073709551615)) -1.0 else @as(f64, @floatFromInt(shaft.sensor_age_ticks)));
    pwr_write_snapshot_value(&snapshot.*.values[@as(c_uint, @intCast(@as(c_int, 16)))], @as(u64, 1017), if (@as(c_int, @intFromBool(shaft.controller_active)) != 0) 1.0 else 0.0);
    return PWR_STATUS_OK;
}

pub const pwr_shaft_backend: pwr_instance_backend = .{ .submit = pwr_shaft_submit, .step = pwr_shaft_step, .snapshot = pwr_shaft_read_snapshot, .destroy = free };
