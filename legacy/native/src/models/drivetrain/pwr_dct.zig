// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/models/drivetrain/pwr_dct.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_DCT_CLUTCH_COUNT = @import("../../abi.zig").PWR_DCT_CLUTCH_COUNT;
const PWR_DCT_CLUTCH_INVALID = @import("../../abi.zig").PWR_DCT_CLUTCH_INVALID;
const PWR_DCT_CLUTCH_K1 = @import("../../abi.zig").PWR_DCT_CLUTCH_K1;
const PWR_DCT_CLUTCH_K2 = @import("../../abi.zig").PWR_DCT_CLUTCH_K2;
const PWR_DCT_DIAG_CLAMPED_SHAFT_IN_NEUTRAL = @import("../../abi.zig").PWR_DCT_DIAG_CLAMPED_SHAFT_IN_NEUTRAL;
const PWR_DCT_DIAG_DOG_SHIFT_WHILE_CLAMPED = @import("../../abi.zig").PWR_DCT_DIAG_DOG_SHIFT_WHILE_CLAMPED;
const PWR_DCT_DIAG_ENERGY_RESIDUAL = @import("../../abi.zig").PWR_DCT_DIAG_ENERGY_RESIDUAL;
const PWR_DCT_DIAG_INPUT_REJECTED = @import("../../abi.zig").PWR_DCT_DIAG_INPUT_REJECTED;
const PWR_DCT_DIAG_INVALID_GEAR_BIT = @import("../../abi.zig").PWR_DCT_DIAG_INVALID_GEAR_BIT;
const PWR_DCT_DIAG_K1_OVERHEAT = @import("../../abi.zig").PWR_DCT_DIAG_K1_OVERHEAT;
const PWR_DCT_DIAG_K1_SLIPPING = @import("../../abi.zig").PWR_DCT_DIAG_K1_SLIPPING;
const PWR_DCT_DIAG_K1_THERMAL_FADE = @import("../../abi.zig").PWR_DCT_DIAG_K1_THERMAL_FADE;
const PWR_DCT_DIAG_K2_OVERHEAT = @import("../../abi.zig").PWR_DCT_DIAG_K2_OVERHEAT;
const PWR_DCT_DIAG_K2_SLIPPING = @import("../../abi.zig").PWR_DCT_DIAG_K2_SLIPPING;
const PWR_DCT_DIAG_K2_THERMAL_FADE = @import("../../abi.zig").PWR_DCT_DIAG_K2_THERMAL_FADE;
const PWR_DCT_DIAG_MULTIPLE_GEARS_ON_SHAFT = @import("../../abi.zig").PWR_DCT_DIAG_MULTIPLE_GEARS_ON_SHAFT;
const PWR_DCT_DIAG_NONE = @import("../../abi.zig").PWR_DCT_DIAG_NONE;
const PWR_DCT_DIAG_NUMERIC_FAILURE = @import("../../abi.zig").PWR_DCT_DIAG_NUMERIC_FAILURE;
const PWR_DCT_GEAR_7 = @import("../../abi.zig").PWR_DCT_GEAR_7;
const PWR_DCT_GEAR_COUNT = @import("../../abi.zig").PWR_DCT_GEAR_COUNT;
const PWR_DCT_GEAR_NEUTRAL = @import("../../abi.zig").PWR_DCT_GEAR_NEUTRAL;
const PWR_DCT_GEAR_REVERSE = @import("../../abi.zig").PWR_DCT_GEAR_REVERSE;
const PWR_DCT_INVALID_ARGUMENT = @import("../../abi.zig").PWR_DCT_INVALID_ARGUMENT;
const PWR_DCT_INVALID_CONFIG = @import("../../abi.zig").PWR_DCT_INVALID_CONFIG;
const PWR_DCT_INVALID_GEAR_COMMAND = @import("../../abi.zig").PWR_DCT_INVALID_GEAR_COMMAND;
const PWR_DCT_INVALID_TIMESTEP = @import("../../abi.zig").PWR_DCT_INVALID_TIMESTEP;
const PWR_DCT_NUMERIC_ERROR = @import("../../abi.zig").PWR_DCT_NUMERIC_ERROR;
const PWR_DCT_OK = @import("../../abi.zig").PWR_DCT_OK;
const exp = @import("../../support.zig").exp;
const fabs = @import("../../support.zig").fabs;
const fmax = @import("../../support.zig").fmax;
const fmin = @import("../../support.zig").fmin;
const memcpy = @import("../../support.zig").memcpy;
const memset = @import("../../support.zig").memset;
const power_isfinite = @import("../../support.zig").power_isfinite;
const pwr_dct = @import("../../abi.zig").pwr_dct;
const pwr_dct_clutch = @import("../../abi.zig").pwr_dct_clutch;
const pwr_dct_clutch_observation = @import("../../abi.zig").pwr_dct_clutch_observation;
const pwr_dct_config = @import("../../abi.zig").pwr_dct_config;
const pwr_dct_gear = @import("../../abi.zig").pwr_dct_gear;
const pwr_dct_gear_mask = @import("../../abi.zig").pwr_dct_gear_mask;
const pwr_dct_inputs = @import("../../abi.zig").pwr_dct_inputs;
const pwr_dct_result = @import("../../abi.zig").pwr_dct_result;
const pwr_dct_snapshot = @import("../../abi.zig").pwr_dct_snapshot;
const tanh = @import("../../support.zig").tanh;

pub fn pwr_dct_config_default(arg_config: [*c]pwr_dct_config) callconv(.c) void {
    var config = arg_config;
    _ = &config;
    if (config == null) {
        return;
    }
    config.* = pwr_dct_config{
        .gear_ratio = [8]f64{
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
            0.0,
        },
        .mesh_efficiency = [8]f64{
            0.97,
            0.97,
            0.97,
            0.97,
            0.97,
            0.97,
            0.97,
            0.97,
        },
        .final_drive_ratio = 1.0,
        .clutch_nominal_capacity_nm = [2]f64{
            250.0,
            250.0,
        },
        .slip_regularization_speed_rad_s = 0.25,
        .clamp_engagement_rate_per_s = [2]f64{
            8.0,
            8.0,
        },
        .clamp_release_rate_per_s = [2]f64{
            10.0,
            10.0,
        },
        .maximum_dog_shift_clamp = 0.02,
        .initial_clutch_temperature_k = [2]f64{
            298.15,
            298.15,
        },
        .clutch_heat_capacity_j_per_k = [2]f64{
            12000.0,
            12000.0,
        },
        .clutch_ambient_conductance_w_per_k = [2]f64{
            18.0,
            18.0,
        },
        .fade_start_temperature_k = [2]f64{
            523.15,
            523.15,
        },
        .fade_full_temperature_k = [2]f64{
            673.15,
            673.15,
        },
        .minimum_fade_capacity_fraction = [2]f64{
            0.35,
            0.35,
        },
        .overheat_temperature_k = [2]f64{
            623.15,
            623.15,
        },
        .slip_diagnostic_speed_rad_s = 2.0,
        .maximum_timestep_s = 0.02,
    };
}

pub fn pwr_dct_validate_config(arg_config: [*c]const pwr_dct_config) callconv(.c) pwr_dct_result {
    var config = arg_config;
    _ = &config;
    if (config == null) {
        return @as(c_uint, @bitCast(PWR_DCT_INVALID_ARGUMENT));
    }
    if ((((((!pwr_dct_positive_finite(config.*.final_drive_ratio) or !pwr_dct_positive_finite(config.*.slip_regularization_speed_rad_s)) or !(power_isfinite(config.*.maximum_dog_shift_clamp) != 0)) or (config.*.maximum_dog_shift_clamp < 0.0)) or (config.*.maximum_dog_shift_clamp > 1.0)) or !pwr_dct_positive_finite(config.*.slip_diagnostic_speed_rad_s)) or !pwr_dct_positive_finite(config.*.maximum_timestep_s)) {
        return @as(c_uint, @bitCast(PWR_DCT_INVALID_CONFIG));
    }
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_COUNT)))) : (index +%= 1) {
            const ratio: f64 = config.*.gear_ratio[index];
            _ = &ratio;
            const efficiency: f64 = config.*.mesh_efficiency[index];
            _ = &efficiency;
            if ((!(power_isfinite(ratio) != 0) or !pwr_dct_positive_finite(efficiency)) or (efficiency > 1.0)) {
                return @as(c_uint, @bitCast(PWR_DCT_INVALID_CONFIG));
            }
            if (((index == @as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_REVERSE)))) and (ratio >= 0.0)) or ((index != @as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_REVERSE)))) and (ratio <= 0.0))) {
                return @as(c_uint, @bitCast(PWR_DCT_INVALID_CONFIG));
            }
        }
    }
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (index +%= 1) {
            if (!pwr_dct_clutch_parameters_valid(config, index)) {
                return @as(c_uint, @bitCast(PWR_DCT_INVALID_CONFIG));
            }
        }
    }
    return @as(c_uint, @bitCast(PWR_DCT_OK));
}

pub fn pwr_dct_clutch_for_gear(arg_gear: pwr_dct_gear) callconv(.c) pwr_dct_clutch {
    var gear = arg_gear;
    _ = &gear;
    while (true) {
        switch (gear) {
            @as(c_int, 1), @as(c_int, 3), @as(c_int, 5), @as(c_int, 7) => return PWR_DCT_CLUTCH_K1,
            @as(c_int, 0), @as(c_int, 2), @as(c_int, 4), @as(c_int, 6) => return PWR_DCT_CLUTCH_K2,
            else => return PWR_DCT_CLUTCH_INVALID,
        }
        break;
    }
    return @import("std").mem.zeroes(pwr_dct_clutch);
}

pub fn pwr_dct_gear_bit(arg_gear: pwr_dct_gear) callconv(.c) pwr_dct_gear_mask {
    var gear = arg_gear;
    _ = &gear;
    if ((gear < PWR_DCT_GEAR_REVERSE) or (gear > PWR_DCT_GEAR_7)) {
        return 0;
    }
    return @as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 1) << @intCast(@as(c_uint, @bitCast(gear)))))));
}

pub fn pwr_dct_init(arg_dct: [*c]pwr_dct, arg_config: [*c]const pwr_dct_config) callconv(.c) pwr_dct_result {
    var dct = arg_dct;
    _ = &dct;
    var config = arg_config;
    _ = &config;
    if ((dct == null) or (config == null)) {
        return @as(c_uint, @bitCast(PWR_DCT_INVALID_ARGUMENT));
    }
    const validation: pwr_dct_result = pwr_dct_validate_config(config);
    _ = &validation;
    if (validation != @as(c_uint, @bitCast(PWR_DCT_OK))) {
        return validation;
    }
    _ = memset(@as(?*anyopaque, @ptrCast(dct)), @as(c_int, 0), @sizeOf(pwr_dct));
    dct.*.config = config.*;
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (index +%= 1) {
            dct.*.selected_gear[index] = PWR_DCT_GEAR_NEUTRAL;
            dct.*.clutch_temperature_k[index] = config.*.initial_clutch_temperature_k[index];
            dct.*.last_thermal_capacity_factor[index] = 1.0;
        }
    }
    return @as(c_uint, @bitCast(PWR_DCT_OK));
}

pub fn pwr_dct_step(arg_dct: [*c]pwr_dct, arg_inputs: [*c]const pwr_dct_inputs, arg_dt_s: f64) callconv(.c) pwr_dct_result {
    var dct = arg_dct;
    _ = &dct;
    var inputs = arg_inputs;
    _ = &inputs;
    var dt_s = arg_dt_s;
    _ = &dt_s;
    if ((dct == null) or (inputs == null)) {
        return @as(c_uint, @bitCast(PWR_DCT_INVALID_ARGUMENT));
    }
    if (!pwr_dct_inputs_valid(inputs)) {
        pwr_dct_reject_command(dct, @as(u32, @bitCast(PWR_DCT_DIAG_NONE)));
        return @as(c_uint, @bitCast(PWR_DCT_INVALID_ARGUMENT));
    }
    if (!pwr_dct_positive_finite(dt_s) or (dt_s > dct.*.config.maximum_timestep_s)) {
        return @as(c_uint, @bitCast(PWR_DCT_INVALID_TIMESTEP));
    }
    var step_flags: u32 = pwr_dct_validate_gear_command(inputs.*.engaged_gear_mask);
    _ = &step_flags;
    if (step_flags != @as(u32, @bitCast(PWR_DCT_DIAG_NONE))) {
        pwr_dct_reject_command(dct, step_flags);
        return @as(c_uint, @bitCast(PWR_DCT_INVALID_GEAR_COMMAND));
    }
    var desired_gear: [2]pwr_dct_gear = [2]pwr_dct_gear{
        pwr_dct_selected_gear(inputs.*.engaged_gear_mask, PWR_DCT_CLUTCH_K1),
        pwr_dct_selected_gear(inputs.*.engaged_gear_mask, PWR_DCT_CLUTCH_K2),
    };
    _ = &desired_gear;
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (index +%= 1) {
            if ((desired_gear[index] != dct.*.selected_gear[index]) and (dct.*.applied_clamp[index] > dct.*.config.maximum_dog_shift_clamp)) {
                pwr_dct_reject_command(dct, @as(u32, @bitCast(PWR_DCT_DIAG_DOG_SHIFT_WHILE_CLAMPED)));
                return @as(c_uint, @bitCast(PWR_DCT_INVALID_GEAR_COMMAND));
            }
        }
    }
    var next_clamp: [2]f64 = undefined;
    _ = &next_clamp;
    var next_temperature: [2]f64 = undefined;
    _ = &next_temperature;
    var shaft_speed: [2]f64 = [2]f64{
        0.0,
        0.0,
    };
    _ = &shaft_speed;
    var slip_speed: [2]f64 = [2]f64{
        0.0,
        0.0,
    };
    _ = &slip_speed;
    var transmitted_torque: [2]f64 = [2]f64{
        0.0,
        0.0,
    };
    _ = &transmitted_torque;
    var torque_capacity: [2]f64 = [2]f64{
        0.0,
        0.0,
    };
    _ = &torque_capacity;
    var friction_power: [2]f64 = [2]f64{
        0.0,
        0.0,
    };
    _ = &friction_power;
    var capacity_factor: [2]f64 = undefined;
    _ = &capacity_factor;
    var heat_rejected_step_j: [2]f64 = [2]f64{
        0.0,
        0.0,
    };
    _ = &heat_rejected_step_j;
    var output_torque_nm: f64 = 0.0;
    _ = &output_torque_nm;
    var gear_loss_power_w: f64 = 0.0;
    _ = &gear_loss_power_w;
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (index +%= 1) {
            next_clamp[index] = pwr_dct_approach(dct.*.applied_clamp[index], inputs.*.clutch_clamp_command[index], dct.*.config.clamp_engagement_rate_per_s[index], dct.*.config.clamp_release_rate_per_s[index], dt_s);
            capacity_factor[index] = pwr_dct_capacity_factor(&dct.*.config, index, dct.*.clutch_temperature_k[index]);
            if (desired_gear[index] == PWR_DCT_GEAR_NEUTRAL) {
                if (next_clamp[index] > dct.*.config.maximum_dog_shift_clamp) {
                    step_flags |= @as(u32, @bitCast(PWR_DCT_DIAG_CLAMPED_SHAFT_IN_NEUTRAL));
                }
            } else {
                const gear_index: usize = @as(usize, @bitCast(@as(i64, desired_gear[index])));
                _ = &gear_index;
                const total_ratio: f64 = dct.*.config.gear_ratio[gear_index] * dct.*.config.final_drive_ratio;
                _ = &total_ratio;
                const efficiency: f64 = dct.*.config.mesh_efficiency[gear_index];
                _ = &efficiency;
                shaft_speed[index] = inputs.*.output_speed_rad_s * total_ratio;
                slip_speed[index] = inputs.*.engine_speed_rad_s - shaft_speed[index];
                torque_capacity[index] = (dct.*.config.clutch_nominal_capacity_nm[index] * next_clamp[index]) * capacity_factor[index];
                transmitted_torque[index] = torque_capacity[index] * tanh(slip_speed[index] / dct.*.config.slip_regularization_speed_rad_s);
                friction_power[index] = transmitted_torque[index] * slip_speed[index];
                if ((friction_power[index] < 0.0) and (friction_power[index] > -0.000000000001)) {
                    friction_power[index] = 0.0;
                }
                const shaft_power_w: f64 = transmitted_torque[index] * shaft_speed[index];
                _ = &shaft_power_w;
                var path_output_torque_nm: f64 = undefined;
                _ = &path_output_torque_nm;
                if (shaft_power_w >= 0.0) {
                    path_output_torque_nm = (transmitted_torque[index] * total_ratio) * efficiency;
                } else {
                    path_output_torque_nm = (transmitted_torque[index] * total_ratio) / efficiency;
                }
                const path_output_power_w: f64 = path_output_torque_nm * inputs.*.output_speed_rad_s;
                _ = &path_output_power_w;
                var path_gear_loss_w: f64 = shaft_power_w - path_output_power_w;
                _ = &path_gear_loss_w;
                if ((path_gear_loss_w < 0.0) and (path_gear_loss_w > -0.000000001)) {
                    path_gear_loss_w = 0.0;
                }
                output_torque_nm += path_output_torque_nm;
                gear_loss_power_w += path_gear_loss_w;
                if ((fabs(slip_speed[index]) > dct.*.config.slip_diagnostic_speed_rad_s) and (next_clamp[index] > dct.*.config.maximum_dog_shift_clamp)) {
                    step_flags |= @as(u32, @bitCast(if (index == @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))) PWR_DCT_DIAG_K1_SLIPPING else PWR_DCT_DIAG_K2_SLIPPING));
                }
            }
            pwr_dct_update_thermal(&dct.*.config, index, inputs.*.ambient_temperature_k, friction_power[index], dt_s, dct.*.clutch_temperature_k[index], &next_temperature[index], &heat_rejected_step_j[index]);
            const next_capacity_factor: f64 = pwr_dct_capacity_factor(&dct.*.config, index, next_temperature[index]);
            _ = &next_capacity_factor;
            if (next_capacity_factor < 1.0) {
                step_flags |= @as(u32, @bitCast(if (index == @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))) PWR_DCT_DIAG_K1_THERMAL_FADE else PWR_DCT_DIAG_K2_THERMAL_FADE));
            }
            if (next_temperature[index] >= dct.*.config.overheat_temperature_k[index]) {
                step_flags |= @as(u32, @bitCast(if (index == @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))) PWR_DCT_DIAG_K1_OVERHEAT else PWR_DCT_DIAG_K2_OVERHEAT));
            }
        }
    }
    const input_transmitted_torque_nm: f64 = transmitted_torque[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] + transmitted_torque[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))];
    _ = &input_transmitted_torque_nm;
    const input_power_w: f64 = input_transmitted_torque_nm * inputs.*.engine_speed_rad_s;
    _ = &input_power_w;
    const output_power_w: f64 = output_torque_nm * inputs.*.output_speed_rad_s;
    _ = &output_power_w;
    var instantaneous_efficiency: f64 = 0.0;
    _ = &instantaneous_efficiency;
    if ((input_power_w > 0.000000000001) and (output_power_w >= 0.0)) {
        instantaneous_efficiency = output_power_w / input_power_w;
    } else if ((output_power_w < -0.000000000001) and (input_power_w <= 0.0)) {
        instantaneous_efficiency = input_power_w / output_power_w;
    }
    instantaneous_efficiency = pwr_dct_clamp(instantaneous_efficiency, 0.0, 1.0);
    const input_step_j: f64 = input_power_w * dt_s;
    _ = &input_step_j;
    const output_step_j: f64 = output_power_w * dt_s;
    _ = &output_step_j;
    const gear_loss_step_j: f64 = gear_loss_power_w * dt_s;
    _ = &gear_loss_step_j;
    const heat_rejected_total_step_j: f64 = heat_rejected_step_j[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] + heat_rejected_step_j[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))];
    _ = &heat_rejected_total_step_j;
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (index +%= 1) {
            if (((((((!(power_isfinite(next_clamp[index]) != 0) or !pwr_dct_positive_finite(next_temperature[index])) or !(power_isfinite(shaft_speed[index]) != 0)) or !(power_isfinite(slip_speed[index]) != 0)) or !(power_isfinite(transmitted_torque[index]) != 0)) or !(power_isfinite(torque_capacity[index]) != 0)) or !(power_isfinite(friction_power[index]) != 0)) or (friction_power[index] < 0.0)) {
                dct.*.last_step_diagnostic_flags = @as(u32, @bitCast(PWR_DCT_DIAG_NUMERIC_FAILURE));
                dct.*.diagnostic_flags |= @as(u32, @bitCast(PWR_DCT_DIAG_NUMERIC_FAILURE));
                return @as(c_uint, @bitCast(PWR_DCT_NUMERIC_ERROR));
            }
        }
    }
    if (((((!(power_isfinite(output_torque_nm) != 0) or !(power_isfinite(gear_loss_power_w) != 0)) or (gear_loss_power_w < 0.0)) or !(power_isfinite(input_power_w) != 0)) or !(power_isfinite(output_power_w) != 0)) or !(power_isfinite(instantaneous_efficiency) != 0)) {
        dct.*.last_step_diagnostic_flags = @as(u32, @bitCast(PWR_DCT_DIAG_NUMERIC_FAILURE));
        dct.*.diagnostic_flags |= @as(u32, @bitCast(PWR_DCT_DIAG_NUMERIC_FAILURE));
        return @as(c_uint, @bitCast(PWR_DCT_NUMERIC_ERROR));
    }
    dct.*.engaged_gear_mask = inputs.*.engaged_gear_mask;
    dct.*.last_engine_speed_rad_s = inputs.*.engine_speed_rad_s;
    dct.*.last_output_speed_rad_s = inputs.*.output_speed_rad_s;
    dct.*.last_input_transmitted_torque_nm = input_transmitted_torque_nm;
    dct.*.last_output_torque_nm = output_torque_nm;
    dct.*.last_input_power_w = input_power_w;
    dct.*.last_output_power_w = output_power_w;
    dct.*.last_gear_mesh_loss_power_w = gear_loss_power_w;
    dct.*.last_instantaneous_efficiency = instantaneous_efficiency;
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (index +%= 1) {
            dct.*.selected_gear[index] = desired_gear[index];
            dct.*.applied_clamp[index] = next_clamp[index];
            dct.*.clutch_temperature_k[index] = next_temperature[index];
            dct.*.cumulative_friction_work_j[index] += friction_power[index] * dt_s;
            dct.*.last_clamp_command[index] = inputs.*.clutch_clamp_command[index];
            dct.*.last_input_shaft_speed_rad_s[index] = shaft_speed[index];
            dct.*.last_slip_speed_rad_s[index] = slip_speed[index];
            dct.*.last_transmitted_torque_nm[index] = transmitted_torque[index];
            dct.*.last_torque_capacity_nm[index] = torque_capacity[index];
            dct.*.last_friction_power_w[index] = friction_power[index];
            dct.*.last_thermal_capacity_factor[index] = pwr_dct_capacity_factor(&dct.*.config, index, next_temperature[index]);
        }
    }
    dct.*.cumulative_input_energy_j += input_step_j;
    dct.*.cumulative_output_energy_j += output_step_j;
    dct.*.cumulative_gear_mesh_loss_j += gear_loss_step_j;
    dct.*.cumulative_heat_rejected_j += heat_rejected_total_step_j;
    dct.*.time_s += dt_s;
    const residual_j: f64 = pwr_dct_energy_residual(dct);
    _ = &residual_j;
    const energy_scale_j: f64 = ((((fabs(dct.*.cumulative_input_energy_j) + fabs(dct.*.cumulative_output_energy_j)) + dct.*.cumulative_gear_mesh_loss_j) + fabs(dct.*.cumulative_heat_rejected_j)) + fabs(pwr_dct_stored_thermal_change(dct))) + 1.0;
    _ = &energy_scale_j;
    if (fabs(residual_j) > (0.000000001 * energy_scale_j)) {
        step_flags |= @as(u32, @bitCast(PWR_DCT_DIAG_ENERGY_RESIDUAL));
    }
    dct.*.last_step_diagnostic_flags = step_flags;
    dct.*.diagnostic_flags |= step_flags;
    return @as(c_uint, @bitCast(PWR_DCT_OK));
}

pub fn pwr_dct_snapshot_read(arg_dct: [*c]const pwr_dct, arg_snapshot: [*c]pwr_dct_snapshot) callconv(.c) pwr_dct_result {
    var dct = arg_dct;
    _ = &dct;
    var snapshot = arg_snapshot;
    _ = &snapshot;
    if ((dct == null) or (snapshot == null)) {
        return @as(c_uint, @bitCast(PWR_DCT_INVALID_ARGUMENT));
    }
    snapshot.* = pwr_dct_snapshot{
        .time_s = dct.*.time_s,
        .engaged_gear_mask = dct.*.engaged_gear_mask,
        .clutch = @import("std").mem.zeroes([2]pwr_dct_clutch_observation),
        .input_transmitted_torque_nm = dct.*.last_input_transmitted_torque_nm,
        .output_torque_nm = dct.*.last_output_torque_nm,
        .input_power_w = dct.*.last_input_power_w,
        .output_power_w = dct.*.last_output_power_w,
        .clutch_friction_power_w = 0,
        .gear_mesh_loss_power_w = dct.*.last_gear_mesh_loss_power_w,
        .instantaneous_efficiency = dct.*.last_instantaneous_efficiency,
        .cumulative_input_energy_j = dct.*.cumulative_input_energy_j,
        .cumulative_output_energy_j = dct.*.cumulative_output_energy_j,
        .cumulative_gear_mesh_loss_j = dct.*.cumulative_gear_mesh_loss_j,
        .cumulative_heat_rejected_j = dct.*.cumulative_heat_rejected_j,
        .stored_thermal_energy_change_j = pwr_dct_stored_thermal_change(dct),
        .energy_residual_j = pwr_dct_energy_residual(dct),
        .last_step_diagnostic_flags = dct.*.last_step_diagnostic_flags,
        .diagnostic_flags = dct.*.diagnostic_flags,
        .rejected_command_count = dct.*.rejected_command_count,
        .state_hash = pwr_dct_state_hash(dct),
    };
    var friction_power_w: f64 = 0.0;
    _ = &friction_power_w;
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (index +%= 1) {
            snapshot.*.clutch[index] = pwr_dct_clutch_observation{
                .selected_gear = dct.*.selected_gear[index],
                .preselected = (dct.*.selected_gear[index] != PWR_DCT_GEAR_NEUTRAL) and (dct.*.applied_clamp[index] <= dct.*.config.maximum_dog_shift_clamp),
                .clamp_command = dct.*.last_clamp_command[index],
                .applied_clamp = dct.*.applied_clamp[index],
                .input_shaft_speed_rad_s = dct.*.last_input_shaft_speed_rad_s[index],
                .slip_speed_rad_s = dct.*.last_slip_speed_rad_s[index],
                .transmitted_torque_nm = dct.*.last_transmitted_torque_nm[index],
                .torque_capacity_nm = dct.*.last_torque_capacity_nm[index],
                .friction_power_w = dct.*.last_friction_power_w[index],
                .cumulative_friction_work_j = dct.*.cumulative_friction_work_j[index],
                .temperature_k = dct.*.clutch_temperature_k[index],
                .thermal_capacity_factor = dct.*.last_thermal_capacity_factor[index],
            };
            friction_power_w += dct.*.last_friction_power_w[index];
        }
    }
    snapshot.*.clutch_friction_power_w = friction_power_w;
    return @as(c_uint, @bitCast(PWR_DCT_OK));
}

pub fn pwr_dct_state_hash(arg_dct: [*c]const pwr_dct) callconv(.c) u64 {
    var dct = arg_dct;
    _ = &dct;
    if (dct == null) {
        return 0;
    }
    var hash: u64 = 14695981039346656037;
    _ = &hash;
    hash = pwr_dct_hash_u64(hash, @as(u64, @bitCast(@as(u64, dct.*.engaged_gear_mask))));
    hash = pwr_dct_hash_double(hash, dct.*.time_s);
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (index +%= 1) {
            hash = pwr_dct_hash_u64(hash, @as(u64, @bitCast(@as(i64, @bitCast(@as(i64, dct.*.selected_gear[index]))))));
            hash = pwr_dct_hash_double(hash, dct.*.applied_clamp[index]);
            hash = pwr_dct_hash_double(hash, dct.*.clutch_temperature_k[index]);
            hash = pwr_dct_hash_double(hash, dct.*.cumulative_friction_work_j[index]);
            hash = pwr_dct_hash_double(hash, dct.*.last_clamp_command[index]);
            hash = pwr_dct_hash_double(hash, dct.*.last_input_shaft_speed_rad_s[index]);
            hash = pwr_dct_hash_double(hash, dct.*.last_slip_speed_rad_s[index]);
            hash = pwr_dct_hash_double(hash, dct.*.last_transmitted_torque_nm[index]);
            hash = pwr_dct_hash_double(hash, dct.*.last_torque_capacity_nm[index]);
            hash = pwr_dct_hash_double(hash, dct.*.last_friction_power_w[index]);
            hash = pwr_dct_hash_double(hash, dct.*.last_thermal_capacity_factor[index]);
        }
    }
    hash = pwr_dct_hash_double(hash, dct.*.last_engine_speed_rad_s);
    hash = pwr_dct_hash_double(hash, dct.*.last_output_speed_rad_s);
    hash = pwr_dct_hash_double(hash, dct.*.last_input_transmitted_torque_nm);
    hash = pwr_dct_hash_double(hash, dct.*.last_output_torque_nm);
    hash = pwr_dct_hash_double(hash, dct.*.last_input_power_w);
    hash = pwr_dct_hash_double(hash, dct.*.last_output_power_w);
    hash = pwr_dct_hash_double(hash, dct.*.last_gear_mesh_loss_power_w);
    hash = pwr_dct_hash_double(hash, dct.*.last_instantaneous_efficiency);
    hash = pwr_dct_hash_double(hash, dct.*.cumulative_input_energy_j);
    hash = pwr_dct_hash_double(hash, dct.*.cumulative_output_energy_j);
    hash = pwr_dct_hash_double(hash, dct.*.cumulative_gear_mesh_loss_j);
    hash = pwr_dct_hash_double(hash, dct.*.cumulative_heat_rejected_j);
    hash = pwr_dct_hash_u64(hash, @as(u64, @bitCast(@as(u64, dct.*.last_step_diagnostic_flags))));
    hash = pwr_dct_hash_u64(hash, @as(u64, @bitCast(@as(u64, dct.*.diagnostic_flags))));
    hash = pwr_dct_hash_u64(hash, dct.*.rejected_command_count);
    return hash;
}

pub fn pwr_dct_result_string(arg_result: pwr_dct_result) callconv(.c) [*c]const u8 {
    var result = arg_result;
    _ = &result;
    while (true) {
        switch (result) {
            @as(c_uint, @bitCast(@as(c_int, 0))) => return "ok",
            @as(c_uint, @bitCast(@as(c_int, 1))) => return "invalid argument",
            @as(c_uint, @bitCast(@as(c_int, 2))) => return "invalid configuration",
            @as(c_uint, @bitCast(@as(c_int, 3))) => return "invalid gear command",
            @as(c_uint, @bitCast(@as(c_int, 4))) => return "invalid timestep",
            @as(c_uint, @bitCast(@as(c_int, 5))) => return "numeric error",
            else => return "unknown result",
        }
        break;
    }
    return null;
}

pub fn pwr_dct_positive_finite(arg_value: f64) callconv(.c) bool {
    var value = arg_value;
    _ = &value;
    return (power_isfinite(value) != 0) and (value > 0.0);
}

pub fn pwr_dct_clamp(arg_value: f64, arg_minimum: f64, arg_maximum: f64) callconv(.c) f64 {
    var value = arg_value;
    _ = &value;
    var minimum = arg_minimum;
    _ = &minimum;
    var maximum = arg_maximum;
    _ = &maximum;
    return fmin(fmax(value, minimum), maximum);
}

pub fn pwr_dct_approach(arg_current: f64, arg_target: f64, arg_rise_rate: f64, arg_fall_rate: f64, arg_dt_s: f64) callconv(.c) f64 {
    var current = arg_current;
    _ = &current;
    var target = arg_target;
    _ = &target;
    var rise_rate = arg_rise_rate;
    _ = &rise_rate;
    var fall_rate = arg_fall_rate;
    _ = &fall_rate;
    var dt_s = arg_dt_s;
    _ = &dt_s;
    const maximum_delta: f64 = (if (target >= current) rise_rate else fall_rate) * dt_s;
    _ = &maximum_delta;
    return current + pwr_dct_clamp(target - current, -maximum_delta, maximum_delta);
}

pub fn pwr_dct_capacity_factor(arg_config: [*c]const pwr_dct_config, arg_clutch_index: usize, arg_temperature_k: f64) callconv(.c) f64 {
    var config = arg_config;
    _ = &config;
    var clutch_index = arg_clutch_index;
    _ = &clutch_index;
    var temperature_k = arg_temperature_k;
    _ = &temperature_k;
    const fade_start: f64 = config.*.fade_start_temperature_k[clutch_index];
    _ = &fade_start;
    const fade_full: f64 = config.*.fade_full_temperature_k[clutch_index];
    _ = &fade_full;
    const minimum: f64 = config.*.minimum_fade_capacity_fraction[clutch_index];
    _ = &minimum;
    if (temperature_k <= fade_start) {
        return 1.0;
    }
    if (temperature_k >= fade_full) {
        return minimum;
    }
    const progress: f64 = (temperature_k - fade_start) / (fade_full - fade_start);
    _ = &progress;
    return 1.0 - (progress * (1.0 - minimum));
}

pub fn pwr_dct_clutch_parameters_valid(arg_config: [*c]const pwr_dct_config, arg_index: usize) callconv(.c) bool {
    var config = arg_config;
    _ = &config;
    var index = arg_index;
    _ = &index;
    return (((((((((((((@as(c_int, @intFromBool(pwr_dct_positive_finite(config.*.clutch_nominal_capacity_nm[index]))) != 0) and (@as(c_int, @intFromBool(pwr_dct_positive_finite(config.*.clamp_engagement_rate_per_s[index]))) != 0)) and (@as(c_int, @intFromBool(pwr_dct_positive_finite(config.*.clamp_release_rate_per_s[index]))) != 0)) and (@as(c_int, @intFromBool(pwr_dct_positive_finite(config.*.initial_clutch_temperature_k[index]))) != 0)) and (@as(c_int, @intFromBool(pwr_dct_positive_finite(config.*.clutch_heat_capacity_j_per_k[index]))) != 0)) and (power_isfinite(config.*.clutch_ambient_conductance_w_per_k[index]) != 0)) and (config.*.clutch_ambient_conductance_w_per_k[index] >= 0.0)) and (@as(c_int, @intFromBool(pwr_dct_positive_finite(config.*.fade_start_temperature_k[index]))) != 0)) and (@as(c_int, @intFromBool(pwr_dct_positive_finite(config.*.fade_full_temperature_k[index]))) != 0)) and (config.*.fade_full_temperature_k[index] > config.*.fade_start_temperature_k[index])) and (@as(c_int, @intFromBool(pwr_dct_positive_finite(config.*.minimum_fade_capacity_fraction[index]))) != 0)) and (config.*.minimum_fade_capacity_fraction[index] <= 1.0)) and (@as(c_int, @intFromBool(pwr_dct_positive_finite(config.*.overheat_temperature_k[index]))) != 0)) and (config.*.overheat_temperature_k[index] >= config.*.fade_start_temperature_k[index]);
}

pub fn pwr_dct_count_bits(arg_mask: pwr_dct_gear_mask) callconv(.c) c_uint {
    var mask = arg_mask;
    _ = &mask;
    var count: c_uint = 0;
    _ = &count;
    while (@as(c_int, @bitCast(@as(c_uint, mask))) != @as(c_int, 0)) {
        count +%= @as(c_uint, @bitCast(@as(c_int, @bitCast(@as(c_uint, mask))) & @as(c_int, 1)));
        mask = @as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, @bitCast(@as(c_uint, mask))) >> @intCast(1)))));
    }
    return count;
}

pub fn pwr_dct_selected_gear(arg_mask: pwr_dct_gear_mask, arg_clutch: pwr_dct_clutch) callconv(.c) pwr_dct_gear {
    var mask = arg_mask;
    _ = &mask;
    var clutch = arg_clutch;
    _ = &clutch;
    {
        var gear_value: c_int = PWR_DCT_GEAR_REVERSE;
        _ = &gear_value;
        while (gear_value <= PWR_DCT_GEAR_7) : (gear_value += 1) {
            const gear: pwr_dct_gear = gear_value;
            _ = &gear;
            if (((@as(c_int, @bitCast(@as(c_uint, mask))) & @as(c_int, @bitCast(@as(c_uint, pwr_dct_gear_bit(gear))))) != @as(c_int, 0)) and (pwr_dct_clutch_for_gear(gear) == clutch)) {
                return gear;
            }
        }
    }
    return PWR_DCT_GEAR_NEUTRAL;
}

pub fn pwr_dct_validate_gear_command(arg_mask: pwr_dct_gear_mask) callconv(.c) u32 {
    var mask = arg_mask;
    _ = &mask;
    var flags: u32 = @as(u32, @bitCast(PWR_DCT_DIAG_NONE));
    _ = &flags;
    if ((@as(c_int, @bitCast(@as(c_uint, mask))) & @as(c_int, @bitCast(@as(c_uint, @as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(~@as(c_int, 255))))))))) != @as(c_int, 0)) {
        flags |= @as(u32, @bitCast(PWR_DCT_DIAG_INVALID_GEAR_BIT));
    }
    if ((pwr_dct_count_bits(@as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, @bitCast(@as(c_uint, mask))) & @as(c_int, 170)))))) > @as(c_uint, 1)) or (pwr_dct_count_bits(@as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, @bitCast(@as(c_uint, mask))) & @as(c_int, 85)))))) > @as(c_uint, 1))) {
        flags |= @as(u32, @bitCast(PWR_DCT_DIAG_MULTIPLE_GEARS_ON_SHAFT));
    }
    return flags;
}

pub fn pwr_dct_reject_command(arg_dct: [*c]pwr_dct, arg_reason_flags: u32) callconv(.c) void {
    var dct = arg_dct;
    _ = &dct;
    var reason_flags = arg_reason_flags;
    _ = &reason_flags;
    const flags: u32 = reason_flags | @as(u32, @bitCast(PWR_DCT_DIAG_INPUT_REJECTED));
    _ = &flags;
    dct.*.last_step_diagnostic_flags = flags;
    dct.*.diagnostic_flags |= flags;
    dct.*.rejected_command_count +%= @as(u64, @bitCast(@as(u64, 1)));
}

pub fn pwr_dct_inputs_valid(arg_inputs: [*c]const pwr_dct_inputs) callconv(.c) bool {
    var inputs = arg_inputs;
    _ = &inputs;
    if ((((inputs == null) or !(power_isfinite(inputs.*.engine_speed_rad_s) != 0)) or !(power_isfinite(inputs.*.output_speed_rad_s) != 0)) or !pwr_dct_positive_finite(inputs.*.ambient_temperature_k)) {
        return @as(c_int, 0) != 0;
    }
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (index +%= 1) {
            const command: f64 = inputs.*.clutch_clamp_command[index];
            _ = &command;
            if ((!(power_isfinite(command) != 0) or (command < 0.0)) or (command > 1.0)) {
                return @as(c_int, 0) != 0;
            }
        }
    }
    return @as(c_int, 1) != 0;
}

pub fn pwr_dct_update_thermal(arg_config: [*c]const pwr_dct_config, arg_index: usize, arg_ambient_temperature_k: f64, arg_friction_power_w: f64, arg_dt_s: f64, arg_old_temperature_k: f64, arg_new_temperature_k: [*c]f64, arg_heat_rejected_j: [*c]f64) callconv(.c) void {
    var config = arg_config;
    _ = &config;
    var index = arg_index;
    _ = &index;
    var ambient_temperature_k = arg_ambient_temperature_k;
    _ = &ambient_temperature_k;
    var friction_power_w = arg_friction_power_w;
    _ = &friction_power_w;
    var dt_s = arg_dt_s;
    _ = &dt_s;
    var old_temperature_k = arg_old_temperature_k;
    _ = &old_temperature_k;
    var new_temperature_k = arg_new_temperature_k;
    _ = &new_temperature_k;
    var heat_rejected_j = arg_heat_rejected_j;
    _ = &heat_rejected_j;
    const capacity: f64 = config.*.clutch_heat_capacity_j_per_k[index];
    _ = &capacity;
    const conductance: f64 = config.*.clutch_ambient_conductance_w_per_k[index];
    _ = &conductance;
    if (conductance > 0.0) {
        const equilibrium_temperature: f64 = ambient_temperature_k + (friction_power_w / conductance);
        _ = &equilibrium_temperature;
        const decay: f64 = exp((-conductance * dt_s) / capacity);
        _ = &decay;
        new_temperature_k.* = equilibrium_temperature + ((old_temperature_k - equilibrium_temperature) * decay);
    } else {
        new_temperature_k.* = old_temperature_k + ((friction_power_w * dt_s) / capacity);
    }
    const stored_change_j: f64 = capacity * (new_temperature_k.* - old_temperature_k);
    _ = &stored_change_j;
    heat_rejected_j.* = (friction_power_w * dt_s) - stored_change_j;
}

pub fn pwr_dct_stored_thermal_change(arg_dct: [*c]const pwr_dct) callconv(.c) f64 {
    var dct = arg_dct;
    _ = &dct;
    var stored_change_j: f64 = 0.0;
    _ = &stored_change_j;
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_COUNT)))) : (index +%= 1) {
            stored_change_j += dct.*.config.clutch_heat_capacity_j_per_k[index] * (dct.*.clutch_temperature_k[index] - dct.*.config.initial_clutch_temperature_k[index]);
        }
    }
    return stored_change_j;
}

pub fn pwr_dct_energy_residual(arg_dct: [*c]const pwr_dct) callconv(.c) f64 {
    var dct = arg_dct;
    _ = &dct;
    return (((dct.*.cumulative_input_energy_j - dct.*.cumulative_output_energy_j) - dct.*.cumulative_gear_mesh_loss_j) - dct.*.cumulative_heat_rejected_j) - pwr_dct_stored_thermal_change(dct);
}

pub fn pwr_dct_hash_u64(arg_hash: u64, arg_value: u64) callconv(.c) u64 {
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

pub fn pwr_dct_hash_double(arg_hash: u64, arg_value: f64) callconv(.c) u64 {
    var hash = arg_hash;
    _ = &hash;
    var value = arg_value;
    _ = &value;
    var bits: u64 = 0;
    _ = &bits;
    _ = memcpy(@as(?*anyopaque, @ptrCast(&bits)), @as(?*const anyopaque, @ptrCast(&value)), @sizeOf(u64));
    return pwr_dct_hash_u64(hash, bits);
}
