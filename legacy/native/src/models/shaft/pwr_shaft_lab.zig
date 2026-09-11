// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/models/shaft/pwr_shaft_lab.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_SCHEDULER_OK = @import("../../abi.zig").PWR_SCHEDULER_OK;
const PWR_SHAFT_DIAG_CONTROLLER_BROWNOUT = @import("../../abi.zig").PWR_SHAFT_DIAG_CONTROLLER_BROWNOUT;
const PWR_SHAFT_DIAG_CURRENT_LIMITED = @import("../../abi.zig").PWR_SHAFT_DIAG_CURRENT_LIMITED;
const PWR_SHAFT_DIAG_DUTY_SATURATED = @import("../../abi.zig").PWR_SHAFT_DIAG_DUTY_SATURATED;
const PWR_SHAFT_DIAG_NUMERIC_FAILURE = @import("../../abi.zig").PWR_SHAFT_DIAG_NUMERIC_FAILURE;
const PWR_SHAFT_DIAG_SENSOR_INVALID = @import("../../abi.zig").PWR_SHAFT_DIAG_SENSOR_INVALID;
const PWR_SHAFT_DIAG_SENSOR_STALE = @import("../../abi.zig").PWR_SHAFT_DIAG_SENSOR_STALE;
const PWR_SHAFT_DIAG_SIGNAL_QUEUE_OVERFLOW = @import("../../abi.zig").PWR_SHAFT_DIAG_SIGNAL_QUEUE_OVERFLOW;
const PWR_SHAFT_FAULT_ACTUATOR_STUCK = @import("../../abi.zig").PWR_SHAFT_FAULT_ACTUATOR_STUCK;
const PWR_SHAFT_FAULT_LV_BROWNOUT = @import("../../abi.zig").PWR_SHAFT_FAULT_LV_BROWNOUT;
const PWR_SHAFT_FAULT_SIGNAL_DROP = @import("../../abi.zig").PWR_SHAFT_FAULT_SIGNAL_DROP;
const PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS = @import("../../abi.zig").PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS;
const PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT = @import("../../abi.zig").PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT;
const PWR_SHAFT_INVALID_ARGUMENT = @import("../../abi.zig").PWR_SHAFT_INVALID_ARGUMENT;
const PWR_SHAFT_INVALID_CONFIG = @import("../../abi.zig").PWR_SHAFT_INVALID_CONFIG;
const PWR_SHAFT_NUMERIC_ERROR = @import("../../abi.zig").PWR_SHAFT_NUMERIC_ERROR;
const PWR_SHAFT_OK = @import("../../abi.zig").PWR_SHAFT_OK;
const PWR_SHAFT_SCHEDULER_ERROR = @import("../../abi.zig").PWR_SHAFT_SCHEDULER_ERROR;
const PWR_SHAFT_TASK_CONTROLLER = @import("../../abi.zig").PWR_SHAFT_TASK_CONTROLLER;
const PWR_SHAFT_TASK_SENSOR = @import("../../abi.zig").PWR_SHAFT_TASK_SENSOR;
const PWR_SHAFT_TASK_SIGNAL_DELIVERY = @import("../../abi.zig").PWR_SHAFT_TASK_SIGNAL_DELIVERY;
const fabs = @import("../../support.zig").fabs;
const fmax = @import("../../support.zig").fmax;
const fmin = @import("../../support.zig").fmin;
const memcpy = @import("../../support.zig").memcpy;
const memset = @import("../../support.zig").memset;
const power_isfinite = @import("../../support.zig").power_isfinite;
const pwr_scheduler_add_task = @import("../../core/pwr_scheduler.zig").pwr_scheduler_add_task;
const pwr_scheduler_init = @import("../../core/pwr_scheduler.zig").pwr_scheduler_init;
const pwr_scheduler_run_tick = @import("../../core/pwr_scheduler.zig").pwr_scheduler_run_tick;
const pwr_scheduler_time_seconds = @import("../../core/pwr_scheduler.zig").pwr_scheduler_time_seconds;
const pwr_shaft_config = @import("../../abi.zig").pwr_shaft_config;
const pwr_shaft_input = @import("../../abi.zig").pwr_shaft_input;
const pwr_shaft_lab = @import("../../abi.zig").pwr_shaft_lab;
const pwr_shaft_result = @import("../../abi.zig").pwr_shaft_result;
const pwr_shaft_signal_slot = @import("../../abi.zig").pwr_shaft_signal_slot;
const pwr_shaft_snapshot = @import("../../abi.zig").pwr_shaft_snapshot;
const pwr_task_desc = @import("../../abi.zig").pwr_task_desc;
const tanh = @import("../../support.zig").tanh;

pub fn pwr_shaft_config_default(arg_config: [*c]pwr_shaft_config) callconv(.c) void {
    var config = arg_config;
    _ = &config;
    if (config == null) {
        return;
    }
    config.* = pwr_shaft_config{
        .base_tick_ns = @as(u64, @bitCast(@as(u64, @as(c_uint, 100000)))),
        .sensor_period_ticks = @as(c_uint, 10),
        .control_period_ticks = @as(c_uint, 10),
        .signal_delay_ticks = @as(c_uint, 2),
        .max_sensor_age_ticks = @as(c_uint, 20),
        .controller_recovery_ticks = @as(c_uint, 20),
        .high_voltage_v = 300.0,
        .low_voltage_nominal_v = 13.5,
        .low_voltage_internal_resistance_ohm = 0.1,
        .controller_current_a = 2.0,
        .brownout_threshold_v = 9.0,
        .max_target_speed_rad_s = 600.0,
        .motor_resistance_ohm = 0.15,
        .motor_inductance_h = 0.002,
        .motor_torque_constant_nm_a = 0.3,
        .motor_back_emf_v_s_rad = 0.3,
        .motor_current_limit_a = 400.0,
        .motor_inertia_kg_m2 = 0.04,
        .motor_viscous_friction_nm_s_rad = 0.01,
        .shaft_stiffness_nm_rad = 1200.0,
        .shaft_damping_nm_s_rad = 8.0,
        .load_inertia_kg_m2 = 0.12,
        .load_viscous_friction_nm_s_rad = 0.02,
        .controller_kp = 0.003,
        .controller_ki_per_s = 0.08,
        .duty_rate_limit_per_s = 5.0,
        .ambient_temperature_k = 298.15,
        .thermal_capacity_j_k = 5000.0,
        .thermal_conductance_w_k = 40.0,
        .inverter_loss_fraction = 0.03,
    };
}

pub fn pwr_shaft_lab_init(arg_lab: [*c]pwr_shaft_lab, arg_config: [*c]const pwr_shaft_config) callconv(.c) pwr_shaft_result {
    var lab = arg_lab;
    _ = &lab;
    var config = arg_config;
    _ = &config;
    if ((lab == null) or (config == null)) {
        return @as(c_uint, @bitCast(PWR_SHAFT_INVALID_ARGUMENT));
    }
    if (!pwr_shaft_config_valid(config)) {
        return @as(c_uint, @bitCast(PWR_SHAFT_INVALID_CONFIG));
    }
    _ = memset(@as(?*anyopaque, @ptrCast(lab)), @as(c_int, 0), @sizeOf(pwr_shaft_lab));
    lab.*.config = config.*;
    lab.*.motor_temperature_k = config.*.ambient_temperature_k;
    lab.*.low_voltage_v = config.*.low_voltage_nominal_v;
    lab.*.last_sensor_delivery_tick = 0;
    if (pwr_scheduler_init(&lab.*.scheduler, config.*.base_tick_ns) != @as(c_uint, @bitCast(PWR_SCHEDULER_OK))) {
        return @as(c_uint, @bitCast(PWR_SHAFT_SCHEDULER_ERROR));
    }
    const sensor: pwr_task_desc = pwr_task_desc{
        .stable_id = @as(u32, @bitCast(PWR_SHAFT_TASK_SENSOR)),
        .period_ticks = config.*.sensor_period_ticks,
        .phase_ticks = @as(c_uint, 0),
        .priority = @as(u16, @bitCast(@as(c_ushort, @truncate(@as(c_uint, 10))))),
        .callback = &pwr_shaft_sensor_task,
        .user = @as(?*anyopaque, @ptrCast(lab)),
    };
    _ = &sensor;
    const delivery: pwr_task_desc = pwr_task_desc{
        .stable_id = @as(u32, @bitCast(PWR_SHAFT_TASK_SIGNAL_DELIVERY)),
        .period_ticks = @as(c_uint, 1),
        .phase_ticks = @as(c_uint, 0),
        .priority = @as(u16, @bitCast(@as(c_ushort, @truncate(@as(c_uint, 20))))),
        .callback = &pwr_shaft_signal_delivery_task,
        .user = @as(?*anyopaque, @ptrCast(lab)),
    };
    _ = &delivery;
    const controller: pwr_task_desc = pwr_task_desc{
        .stable_id = @as(u32, @bitCast(PWR_SHAFT_TASK_CONTROLLER)),
        .period_ticks = config.*.control_period_ticks,
        .phase_ticks = @as(c_uint, 0),
        .priority = @as(u16, @bitCast(@as(c_ushort, @truncate(@as(c_uint, 30))))),
        .callback = &pwr_shaft_controller_task,
        .user = @as(?*anyopaque, @ptrCast(lab)),
    };
    _ = &controller;
    if (((pwr_scheduler_add_task(&lab.*.scheduler, &sensor) != @as(c_uint, @bitCast(PWR_SCHEDULER_OK))) or (pwr_scheduler_add_task(&lab.*.scheduler, &delivery) != @as(c_uint, @bitCast(PWR_SCHEDULER_OK)))) or (pwr_scheduler_add_task(&lab.*.scheduler, &controller) != @as(c_uint, @bitCast(PWR_SCHEDULER_OK)))) {
        return @as(c_uint, @bitCast(PWR_SHAFT_SCHEDULER_ERROR));
    }
    lab.*.initial_stored_energy_j = pwr_shaft_stored_energy(lab);
    return @as(c_uint, @bitCast(PWR_SHAFT_OK));
}

pub fn pwr_shaft_lab_step(arg_lab: [*c]pwr_shaft_lab, arg_input: [*c]const pwr_shaft_input) callconv(.c) pwr_shaft_result {
    var lab = arg_lab;
    _ = &lab;
    var input = arg_input;
    _ = &input;
    if ((lab == null) or (input == null)) {
        return @as(c_uint, @bitCast(PWR_SHAFT_INVALID_ARGUMENT));
    }
    if ((((!(power_isfinite(input.*.target_speed_rad_s) != 0) or !(power_isfinite(input.*.load_torque_nm) != 0)) or (input.*.load_torque_nm < 0.0)) or !(power_isfinite(input.*.speed_sensor_bias_rad_s) != 0)) or !(power_isfinite(input.*.actuator_stuck_duty) != 0)) {
        return @as(c_uint, @bitCast(PWR_SHAFT_INVALID_ARGUMENT));
    }
    lab.*.current_input = input.*;
    if (pwr_scheduler_run_tick(&lab.*.scheduler) != @as(c_uint, @bitCast(PWR_SCHEDULER_OK))) {
        return @as(c_uint, @bitCast(PWR_SHAFT_SCHEDULER_ERROR));
    }
    var config: [*c]const pwr_shaft_config = &lab.*.config;
    _ = &config;
    const dt: f64 = @as(f64, @floatFromInt(config.*.base_tick_ns)) * 0.000000001;
    _ = &dt;
    lab.*.applied_duty = lab.*.commanded_duty;
    if ((input.*.fault_mask & @as(u32, @bitCast(PWR_SHAFT_FAULT_ACTUATOR_STUCK))) != @as(c_uint, 0)) {
        lab.*.applied_duty = pwr_clamp(input.*.actuator_stuck_duty, -1.0, 1.0);
    }
    const applied_voltage: f64 = lab.*.applied_duty * config.*.high_voltage_v;
    _ = &applied_voltage;
    const old_current: f64 = lab.*.motor_current_a;
    _ = &old_current;
    const current_derivative: f64 = ((applied_voltage - (config.*.motor_back_emf_v_s_rad * lab.*.motor_speed_rad_s)) - (config.*.motor_resistance_ohm * lab.*.motor_current_a)) / config.*.motor_inductance_h;
    _ = &current_derivative;
    var next_current: f64 = lab.*.motor_current_a + (current_derivative * dt);
    _ = &next_current;
    if (fabs(next_current) > config.*.motor_current_limit_a) {
        const unclamped_magnetic: f64 = ((0.5 * config.*.motor_inductance_h) * next_current) * next_current;
        _ = &unclamped_magnetic;
        next_current = pwr_clamp(next_current, -config.*.motor_current_limit_a, config.*.motor_current_limit_a);
        const clamped_magnetic: f64 = ((0.5 * config.*.motor_inductance_h) * next_current) * next_current;
        _ = &clamped_magnetic;
        lab.*.limiter_adjustment_j += unclamped_magnetic - clamped_magnetic;
        lab.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SHAFT_DIAG_CURRENT_LIMITED));
    }
    lab.*.motor_current_a = next_current;
    const average_current: f64 = 0.5 * (old_current + next_current);
    _ = &average_current;
    const motor_torque: f64 = config.*.motor_torque_constant_nm_a * average_current;
    _ = &motor_torque;
    const shaft_torque: f64 = (config.*.shaft_stiffness_nm_rad * lab.*.shaft_twist_rad) + (config.*.shaft_damping_nm_s_rad * (lab.*.motor_speed_rad_s - lab.*.load_speed_rad_s));
    _ = &shaft_torque;
    const motor_alpha: f64 = ((motor_torque - shaft_torque) - (config.*.motor_viscous_friction_nm_s_rad * lab.*.motor_speed_rad_s)) / config.*.motor_inertia_kg_m2;
    _ = &motor_alpha;
    const resisting_load_torque: f64 = input.*.load_torque_nm * tanh(lab.*.load_speed_rad_s);
    _ = &resisting_load_torque;
    const load_alpha: f64 = ((shaft_torque - resisting_load_torque) - (config.*.load_viscous_friction_nm_s_rad * lab.*.load_speed_rad_s)) / config.*.load_inertia_kg_m2;
    _ = &load_alpha;
    lab.*.motor_speed_rad_s += motor_alpha * dt;
    lab.*.load_speed_rad_s += load_alpha * dt;
    lab.*.shaft_twist_rad += (lab.*.motor_speed_rad_s - lab.*.load_speed_rad_s) * dt;
    const terminal_power_w: f64 = applied_voltage * average_current;
    _ = &terminal_power_w;
    const inverter_loss_w: f64 = config.*.inverter_loss_fraction * fabs(terminal_power_w);
    _ = &inverter_loss_w;
    const copper_loss_w: f64 = (config.*.motor_resistance_ohm * average_current) * average_current;
    _ = &copper_loss_w;
    const friction_loss_w: f64 = (((config.*.motor_viscous_friction_nm_s_rad * lab.*.motor_speed_rad_s) * lab.*.motor_speed_rad_s) + ((config.*.load_viscous_friction_nm_s_rad * lab.*.load_speed_rad_s) * lab.*.load_speed_rad_s)) + ((config.*.shaft_damping_nm_s_rad * (lab.*.motor_speed_rad_s - lab.*.load_speed_rad_s)) * (lab.*.motor_speed_rad_s - lab.*.load_speed_rad_s));
    _ = &friction_loss_w;
    const heat_rejection_w: f64 = config.*.thermal_conductance_w_k * (lab.*.motor_temperature_k - config.*.ambient_temperature_k);
    _ = &heat_rejection_w;
    const thermal_derivative: f64 = (((inverter_loss_w + copper_loss_w) + friction_loss_w) - heat_rejection_w) / config.*.thermal_capacity_j_k;
    _ = &thermal_derivative;
    lab.*.motor_temperature_k += thermal_derivative * dt;
    lab.*.electrical_input_j += (terminal_power_w + inverter_loss_w) * dt;
    lab.*.mechanical_output_j += (resisting_load_torque * lab.*.load_speed_rad_s) * dt;
    lab.*.heat_rejected_j += heat_rejection_w * dt;
    lab.*.control_energy_input_j += (lab.*.low_voltage_v * config.*.controller_current_a) * dt;
    if (((((!(power_isfinite(lab.*.motor_speed_rad_s) != 0) or !(power_isfinite(lab.*.load_speed_rad_s) != 0)) or !(power_isfinite(lab.*.shaft_twist_rad) != 0)) or !(power_isfinite(lab.*.motor_current_a) != 0)) or !(power_isfinite(lab.*.motor_temperature_k) != 0)) or (lab.*.motor_temperature_k <= 0.0)) {
        lab.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SHAFT_DIAG_NUMERIC_FAILURE));
        return @as(c_uint, @bitCast(PWR_SHAFT_NUMERIC_ERROR));
    }
    return @as(c_uint, @bitCast(PWR_SHAFT_OK));
}

pub fn pwr_shaft_lab_snapshot(arg_lab: [*c]const pwr_shaft_lab, arg_snapshot: [*c]pwr_shaft_snapshot) callconv(.c) pwr_shaft_result {
    var lab = arg_lab;
    _ = &lab;
    var snapshot = arg_snapshot;
    _ = &snapshot;
    if ((lab == null) or (snapshot == null)) {
        return @as(c_uint, @bitCast(PWR_SHAFT_INVALID_ARGUMENT));
    }
    const stored_change: f64 = pwr_shaft_stored_energy(lab) - lab.*.initial_stored_energy_j;
    _ = &stored_change;
    const residual: f64 = (((lab.*.electrical_input_j - lab.*.mechanical_output_j) - lab.*.heat_rejected_j) - lab.*.limiter_adjustment_j) - stored_change;
    _ = &residual;
    const tick: u64 = lab.*.scheduler.current_tick;
    _ = &tick;
    const age: u64 = if (@as(c_int, @intFromBool(lab.*.sensor_valid)) != 0) tick -% lab.*.last_sensor_delivery_tick else @as(u64, 18446744073709551615);
    _ = &age;
    snapshot.* = pwr_shaft_snapshot{
        .time_s = pwr_scheduler_time_seconds(&lab.*.scheduler),
        .motor_speed_rad_s = lab.*.motor_speed_rad_s,
        .load_speed_rad_s = lab.*.load_speed_rad_s,
        .shaft_twist_rad = lab.*.shaft_twist_rad,
        .motor_current_a = lab.*.motor_current_a,
        .commanded_duty = lab.*.commanded_duty,
        .applied_duty = lab.*.applied_duty,
        .sensed_speed_rad_s = lab.*.sensed_speed_rad_s,
        .low_voltage_v = lab.*.low_voltage_v,
        .motor_temperature_k = lab.*.motor_temperature_k,
        .electrical_input_j = lab.*.electrical_input_j,
        .mechanical_output_j = lab.*.mechanical_output_j,
        .heat_rejected_j = lab.*.heat_rejected_j,
        .stored_energy_change_j = stored_change,
        .numerical_adjustment_j = lab.*.limiter_adjustment_j,
        .energy_residual_j = residual,
        .control_energy_input_j = lab.*.control_energy_input_j,
        .sensor_age_ticks = age,
        .diagnostic_flags = lab.*.diagnostic_flags,
        .sensor_valid = lab.*.sensor_valid,
        .controller_active = lab.*.controller_active,
        .state_hash = pwr_shaft_lab_state_hash(lab),
    };
    return @as(c_uint, @bitCast(PWR_SHAFT_OK));
}

pub fn pwr_shaft_lab_state_hash(arg_lab: [*c]const pwr_shaft_lab) callconv(.c) u64 {
    var lab = arg_lab;
    _ = &lab;
    if (lab == null) {
        return 0;
    }
    var hash: u64 = 14695981039346656037;
    _ = &hash;
    hash = pwr_hash_u64(hash, lab.*.scheduler.current_tick);
    hash = pwr_hash_double(hash, lab.*.motor_speed_rad_s);
    hash = pwr_hash_double(hash, lab.*.load_speed_rad_s);
    hash = pwr_hash_double(hash, lab.*.shaft_twist_rad);
    hash = pwr_hash_double(hash, lab.*.motor_current_a);
    hash = pwr_hash_double(hash, lab.*.motor_temperature_k);
    hash = pwr_hash_double(hash, lab.*.sensed_speed_rad_s);
    hash = pwr_hash_double(hash, lab.*.controller_integral);
    hash = pwr_hash_double(hash, lab.*.commanded_duty);
    hash = pwr_hash_double(hash, lab.*.applied_duty);
    hash = pwr_hash_u64(hash, @as(u64, @bitCast(@as(u64, lab.*.diagnostic_flags))));
    hash = pwr_hash_u64(hash, @as(u64, @bitCast(@as(u64, if (@as(c_int, @intFromBool(lab.*.sensor_valid)) != 0) @as(c_uint, 1) else @as(c_uint, 0)))));
    hash = pwr_hash_u64(hash, @as(u64, @bitCast(@as(u64, if (@as(c_int, @intFromBool(lab.*.controller_active)) != 0) @as(c_uint, 1) else @as(c_uint, 0)))));
    return hash;
}

pub fn pwr_clamp(arg_value: f64, arg_minimum: f64, arg_maximum: f64) callconv(.c) f64 {
    var value = arg_value;
    _ = &value;
    var minimum = arg_minimum;
    _ = &minimum;
    var maximum = arg_maximum;
    _ = &maximum;
    return fmin(fmax(value, minimum), maximum);
}

pub fn pwr_is_positive_finite(arg_value: f64) callconv(.c) bool {
    var value = arg_value;
    _ = &value;
    return (power_isfinite(value) != 0) and (value > 0.0);
}

pub fn pwr_shaft_config_valid(arg_config: [*c]const pwr_shaft_config) callconv(.c) bool {
    var config = arg_config;
    _ = &config;
    if (((((((config == null) or (config.*.base_tick_ns == @as(u64, @bitCast(@as(u64, @as(c_uint, 0)))))) or (config.*.sensor_period_ticks == @as(c_uint, 0))) or (config.*.control_period_ticks == @as(c_uint, 0))) or (config.*.signal_delay_ticks >= @as(c_uint, 64))) or (config.*.max_sensor_age_ticks < config.*.signal_delay_ticks)) or (config.*.controller_recovery_ticks == @as(c_uint, 0))) {
        return @as(c_int, 0) != 0;
    }
    return (((((((((((((((((((((((((((((((((@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.high_voltage_v))) != 0) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.low_voltage_nominal_v))) != 0)) and (power_isfinite(config.*.low_voltage_internal_resistance_ohm) != 0)) and (config.*.low_voltage_internal_resistance_ohm >= 0.0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.controller_current_a))) != 0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.brownout_threshold_v))) != 0)) and (config.*.brownout_threshold_v < config.*.low_voltage_nominal_v)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.max_target_speed_rad_s))) != 0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.motor_resistance_ohm))) != 0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.motor_inductance_h))) != 0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.motor_torque_constant_nm_a))) != 0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.motor_back_emf_v_s_rad))) != 0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.motor_current_limit_a))) != 0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.motor_inertia_kg_m2))) != 0)) and (power_isfinite(config.*.motor_viscous_friction_nm_s_rad) != 0)) and (config.*.motor_viscous_friction_nm_s_rad >= 0.0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.shaft_stiffness_nm_rad))) != 0)) and (power_isfinite(config.*.shaft_damping_nm_s_rad) != 0)) and (config.*.shaft_damping_nm_s_rad >= 0.0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.load_inertia_kg_m2))) != 0)) and (power_isfinite(config.*.load_viscous_friction_nm_s_rad) != 0)) and (config.*.load_viscous_friction_nm_s_rad >= 0.0)) and (power_isfinite(config.*.controller_kp) != 0)) and (config.*.controller_kp >= 0.0)) and (power_isfinite(config.*.controller_ki_per_s) != 0)) and (config.*.controller_ki_per_s >= 0.0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.duty_rate_limit_per_s))) != 0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.ambient_temperature_k))) != 0)) and (@as(c_int, @intFromBool(pwr_is_positive_finite(config.*.thermal_capacity_j_k))) != 0)) and (power_isfinite(config.*.thermal_conductance_w_k) != 0)) and (config.*.thermal_conductance_w_k >= 0.0)) and (power_isfinite(config.*.inverter_loss_fraction) != 0)) and (config.*.inverter_loss_fraction >= 0.0)) and (config.*.inverter_loss_fraction < 1.0);
}

pub fn pwr_shaft_stored_energy(arg_lab: [*c]const pwr_shaft_lab) callconv(.c) f64 {
    var lab = arg_lab;
    _ = &lab;
    var config: [*c]const pwr_shaft_config = &lab.*.config;
    _ = &config;
    const magnetic: f64 = ((0.5 * config.*.motor_inductance_h) * lab.*.motor_current_a) * lab.*.motor_current_a;
    _ = &magnetic;
    const motor_kinetic: f64 = ((0.5 * config.*.motor_inertia_kg_m2) * lab.*.motor_speed_rad_s) * lab.*.motor_speed_rad_s;
    _ = &motor_kinetic;
    const load_kinetic: f64 = ((0.5 * config.*.load_inertia_kg_m2) * lab.*.load_speed_rad_s) * lab.*.load_speed_rad_s;
    _ = &load_kinetic;
    const shaft_potential: f64 = ((0.5 * config.*.shaft_stiffness_nm_rad) * lab.*.shaft_twist_rad) * lab.*.shaft_twist_rad;
    _ = &shaft_potential;
    const thermal: f64 = config.*.thermal_capacity_j_k * (lab.*.motor_temperature_k - config.*.ambient_temperature_k);
    _ = &thermal;
    return (((magnetic + motor_kinetic) + load_kinetic) + shaft_potential) + thermal;
}

pub fn pwr_shaft_sensor_task(arg_user: ?*anyopaque, arg_tick: u64) callconv(.c) bool {
    var user = arg_user;
    _ = &user;
    var tick = arg_tick;
    _ = &tick;
    var lab: [*c]pwr_shaft_lab = @as([*c]pwr_shaft_lab, @ptrCast(@alignCast(user)));
    _ = &lab;
    var input: [*c]const pwr_shaft_input = &lab.*.current_input;
    _ = &input;
    if ((input.*.fault_mask & @as(u32, @bitCast(PWR_SHAFT_FAULT_SIGNAL_DROP))) != @as(c_uint, 0)) {
        return @as(c_int, 1) != 0;
    }
    const delivery_tick: u64 = tick +% @as(u64, @bitCast(@as(u64, lab.*.config.signal_delay_ticks)));
    _ = &delivery_tick;
    const slot_index: usize = @as(usize, @bitCast(delivery_tick % @as(u64, @bitCast(@as(u64, @as(c_uint, 64))))));
    _ = &slot_index;
    var slot: [*c]pwr_shaft_signal_slot = &lab.*.signal_queue[slot_index];
    _ = &slot;
    if (slot.*.occupied) {
        lab.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SHAFT_DIAG_SIGNAL_QUEUE_OVERFLOW));
        return @as(c_int, 0) != 0;
    }
    var sensed: f64 = lab.*.load_speed_rad_s;
    _ = &sensed;
    if ((input.*.fault_mask & @as(u32, @bitCast(PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS))) != @as(c_uint, 0)) {
        sensed += input.*.speed_sensor_bias_rad_s;
    }
    slot.* = pwr_shaft_signal_slot{
        .delivery_tick = delivery_tick,
        .value = sensed,
        .valid = (input.*.fault_mask & @as(u32, @bitCast(PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT))) == @as(c_uint, 0),
        .occupied = @as(c_int, 1) != 0,
    };
    return @as(c_int, 1) != 0;
}

pub fn pwr_shaft_signal_delivery_task(arg_user: ?*anyopaque, arg_tick: u64) callconv(.c) bool {
    var user = arg_user;
    _ = &user;
    var tick = arg_tick;
    _ = &tick;
    var lab: [*c]pwr_shaft_lab = @as([*c]pwr_shaft_lab, @ptrCast(@alignCast(user)));
    _ = &lab;
    const slot_index: usize = @as(usize, @bitCast(tick % @as(u64, @bitCast(@as(u64, @as(c_uint, 64))))));
    _ = &slot_index;
    var slot: [*c]pwr_shaft_signal_slot = &lab.*.signal_queue[slot_index];
    _ = &slot;
    if (!slot.*.occupied or (slot.*.delivery_tick != tick)) {
        return @as(c_int, 1) != 0;
    }
    lab.*.sensed_speed_rad_s = slot.*.value;
    lab.*.sensor_valid = slot.*.valid;
    lab.*.last_sensor_delivery_tick = tick;
    slot.*.occupied = @as(c_int, 0) != 0;
    return @as(c_int, 1) != 0;
}

pub fn pwr_shaft_controller_task(arg_user: ?*anyopaque, arg_tick: u64) callconv(.c) bool {
    var user = arg_user;
    _ = &user;
    var tick = arg_tick;
    _ = &tick;
    var lab: [*c]pwr_shaft_lab = @as([*c]pwr_shaft_lab, @ptrCast(@alignCast(user)));
    _ = &lab;
    var config: [*c]const pwr_shaft_config = &lab.*.config;
    _ = &config;
    var input: [*c]const pwr_shaft_input = &lab.*.current_input;
    _ = &input;
    const control_dt: f64 = (@as(f64, @floatFromInt(config.*.control_period_ticks)) * @as(f64, @floatFromInt(config.*.base_tick_ns))) * 0.000000001;
    _ = &control_dt;
    lab.*.low_voltage_v = config.*.low_voltage_nominal_v - (config.*.low_voltage_internal_resistance_ohm * config.*.controller_current_a);
    if ((input.*.fault_mask & @as(u32, @bitCast(PWR_SHAFT_FAULT_LV_BROWNOUT))) != @as(c_uint, 0)) {
        lab.*.low_voltage_v = 0.0;
    }
    if (lab.*.low_voltage_v < config.*.brownout_threshold_v) {
        lab.*.controller_active = @as(c_int, 0) != 0;
        lab.*.controller_recovery_progress_ticks = 0;
        lab.*.controller_integral = 0.0;
        lab.*.commanded_duty = 0.0;
        lab.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SHAFT_DIAG_CONTROLLER_BROWNOUT));
        return @as(c_int, 1) != 0;
    }
    if (!lab.*.controller_active) {
        lab.*.controller_recovery_progress_ticks +%= @as(u64, @bitCast(@as(u64, config.*.control_period_ticks)));
        if (lab.*.controller_recovery_progress_ticks < @as(u64, @bitCast(@as(u64, config.*.controller_recovery_ticks)))) {
            lab.*.commanded_duty = 0.0;
            return @as(c_int, 1) != 0;
        }
        lab.*.controller_active = @as(c_int, 1) != 0;
        lab.*.controller_integral = 0.0;
    }
    const sensor_age: u64 = if (@as(c_int, @intFromBool(lab.*.sensor_valid)) != 0) tick -% lab.*.last_sensor_delivery_tick else @as(u64, 18446744073709551615);
    _ = &sensor_age;
    if (!lab.*.sensor_valid) {
        lab.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SHAFT_DIAG_SENSOR_INVALID));
    }
    if (sensor_age > @as(u64, @bitCast(@as(u64, config.*.max_sensor_age_ticks)))) {
        lab.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SHAFT_DIAG_SENSOR_STALE));
    }
    if (!lab.*.sensor_valid or (sensor_age > @as(u64, @bitCast(@as(u64, config.*.max_sensor_age_ticks))))) {
        lab.*.controller_integral = 0.0;
        lab.*.commanded_duty = 0.0;
        return @as(c_int, 1) != 0;
    }
    const target: f64 = pwr_clamp(input.*.target_speed_rad_s, 0.0, config.*.max_target_speed_rad_s);
    _ = &target;
    const @"error": f64 = target - lab.*.sensed_speed_rad_s;
    _ = &@"error";
    const candidate_integral: f64 = lab.*.controller_integral + (@"error" * control_dt);
    _ = &candidate_integral;
    const raw_duty: f64 = (config.*.controller_kp * @"error") + (config.*.controller_ki_per_s * candidate_integral);
    _ = &raw_duty;
    const saturated_duty: f64 = pwr_clamp(raw_duty, -1.0, 1.0);
    _ = &saturated_duty;
    if (saturated_duty != raw_duty) {
        lab.*.diagnostic_flags |= @as(u32, @bitCast(PWR_SHAFT_DIAG_DUTY_SATURATED));
    }
    const drives_further_into_saturation: bool = ((raw_duty > 1.0) and (@"error" > 0.0)) or ((raw_duty < -1.0) and (@"error" < 0.0));
    _ = &drives_further_into_saturation;
    if (!drives_further_into_saturation) {
        lab.*.controller_integral = candidate_integral;
    }
    const maximum_delta: f64 = config.*.duty_rate_limit_per_s * control_dt;
    _ = &maximum_delta;
    const delta: f64 = pwr_clamp(saturated_duty - lab.*.commanded_duty, -maximum_delta, maximum_delta);
    _ = &delta;
    lab.*.commanded_duty = pwr_clamp(lab.*.commanded_duty + delta, -1.0, 1.0);
    return @as(c_int, 1) != 0;
}

pub fn pwr_hash_u64(arg_hash: u64, arg_value: u64) callconv(.c) u64 {
    var hash = arg_hash;
    _ = &hash;
    var value = arg_value;
    _ = &value;
    {
        var byte: c_uint = 0;
        _ = &byte;
        while (byte < @as(c_uint, 8)) : (byte +%= 1) {
            hash ^= @as(u64, @bitCast((value >> @intCast(byte *% @as(c_uint, 8))) & @as(u64, 255)));
            hash *%= @as(u64, @bitCast(@as(u64, 1099511628211)));
        }
    }
    return hash;
}

pub fn pwr_hash_double(arg_hash: u64, arg_value: f64) callconv(.c) u64 {
    var hash = arg_hash;
    _ = &hash;
    var value = arg_value;
    _ = &value;
    var bits: u64 = 0;
    _ = &bits;
    _ = memcpy(@as(?*anyopaque, @ptrCast(&bits)), @as(?*const anyopaque, @ptrCast(&value)), @sizeOf(u64));
    return pwr_hash_u64(hash, bits);
}
