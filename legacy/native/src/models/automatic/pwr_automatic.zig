// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/models/automatic/pwr_automatic.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_AUTOMATIC_DIAG_COUPLING_SPEED_PROJECTED = @import("../../abi.zig").PWR_AUTOMATIC_DIAG_COUPLING_SPEED_PROJECTED;
const PWR_AUTOMATIC_DIAG_EXCESSIVE_LOCKUP_SLIP = @import("../../abi.zig").PWR_AUTOMATIC_DIAG_EXCESSIVE_LOCKUP_SLIP;
const PWR_AUTOMATIC_DIAG_EXCESSIVE_RATIO_ERROR = @import("../../abi.zig").PWR_AUTOMATIC_DIAG_EXCESSIVE_RATIO_ERROR;
const PWR_AUTOMATIC_DIAG_LOCKUP_OVERTEMPERATURE = @import("../../abi.zig").PWR_AUTOMATIC_DIAG_LOCKUP_OVERTEMPERATURE;
const PWR_AUTOMATIC_DIAG_LOW_LINE_PRESSURE = @import("../../abi.zig").PWR_AUTOMATIC_DIAG_LOW_LINE_PRESSURE;
const PWR_AUTOMATIC_DIAG_NUMERIC_LIMIT = @import("../../abi.zig").PWR_AUTOMATIC_DIAG_NUMERIC_LIMIT;
const PWR_AUTOMATIC_DIAG_OIL_OVERTEMPERATURE = @import("../../abi.zig").PWR_AUTOMATIC_DIAG_OIL_OVERTEMPERATURE;
const PWR_AUTOMATIC_DIAG_OVERSPEED = @import("../../abi.zig").PWR_AUTOMATIC_DIAG_OVERSPEED;
const PWR_AUTOMATIC_DIAG_SHIFT_TIMEOUT = @import("../../abi.zig").PWR_AUTOMATIC_DIAG_SHIFT_TIMEOUT;
const PWR_AUTOMATIC_GEAR_FIRST = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_FIRST;
const PWR_AUTOMATIC_GEAR_FOURTH = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_FOURTH;
const PWR_AUTOMATIC_GEAR_NEUTRAL = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_NEUTRAL;
const PWR_AUTOMATIC_GEAR_REVERSE = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_REVERSE;
const PWR_AUTOMATIC_GEAR_SECOND = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_SECOND;
const PWR_AUTOMATIC_GEAR_THIRD = @import("../../abi.zig").PWR_AUTOMATIC_GEAR_THIRD;
const PWR_AUTOMATIC_INVALID_ARGUMENT = @import("../../abi.zig").PWR_AUTOMATIC_INVALID_ARGUMENT;
const PWR_AUTOMATIC_INVALID_CONFIG = @import("../../abi.zig").PWR_AUTOMATIC_INVALID_CONFIG;
const PWR_AUTOMATIC_INVALID_INPUT = @import("../../abi.zig").PWR_AUTOMATIC_INVALID_INPUT;
const PWR_AUTOMATIC_NUMERIC_ERROR = @import("../../abi.zig").PWR_AUTOMATIC_NUMERIC_ERROR;
const PWR_AUTOMATIC_OK = @import("../../abi.zig").PWR_AUTOMATIC_OK;
const exp = @import("../../support.zig").exp;
const fabs = @import("../../support.zig").fabs;
const fmax = @import("../../support.zig").fmax;
const fmin = @import("../../support.zig").fmin;
const memcpy = @import("../../support.zig").memcpy;
const memset = @import("../../support.zig").memset;
const power_isfinite = @import("../../support.zig").power_isfinite;
const pwr_automatic = @import("../../abi.zig").pwr_automatic;
const pwr_automatic_config = @import("../../abi.zig").pwr_automatic_config;
const pwr_automatic_converter_point = @import("../../abi.zig").pwr_automatic_converter_point;
const pwr_automatic_coupled_input = @import("../../abi.zig").pwr_automatic_coupled_input;
const pwr_automatic_coupling_output = @import("../../abi.zig").pwr_automatic_coupling_output;
const pwr_automatic_gear = @import("../../abi.zig").pwr_automatic_gear;
const pwr_automatic_input = @import("../../abi.zig").pwr_automatic_input;
const pwr_automatic_result = @import("../../abi.zig").pwr_automatic_result;
const pwr_automatic_snapshot = @import("../../abi.zig").pwr_automatic_snapshot;
const tanh = @import("../../support.zig").tanh;

pub fn pwr_automatic_config_default(arg_config: [*c]pwr_automatic_config) callconv(.c) void {
    var config = arg_config;
    _ = &config;
    if (config == null) {
        return;
    }
    config.* = pwr_automatic_config{
        .step_ns = @as(u64, @bitCast(@as(u64, @as(c_uint, 100000)))),
        .forward_ratios = [4]f64{
            2.85,
            1.55,
            1.0,
            0.72,
        },
        .reverse_ratio = -2.4,
        .pump_inertia_kg_m2 = 0.12,
        .turbine_inertia_kg_m2 = 0.08,
        .output_inertia_kg_m2 = 0.8,
        .pump_viscous_drag_nm_s_rad = 0.018,
        .turbine_viscous_drag_nm_s_rad = 0.014,
        .output_viscous_drag_nm_s_rad = 0.18,
        .converter_k_rad_s_sqrt_nm = 20.0,
        .converter_stall_torque_ratio = 2.0,
        .converter_coupling_speed_ratio = 0.9,
        .converter_slip_smoothing_rad_s = 12.0,
        .nominal_line_pressure_pa = 1100000.0,
        .maximum_line_pressure_pa = 1600000.0,
        .minimum_drive_pressure_pa = 550000.0,
        .pressure_time_constant_s = 0.08,
        .maximum_gear_clutch_torque_nm = 280.0,
        .gear_clutch_slip_smoothing_rad_s = 4.0,
        .nominal_shift_duration_s = 0.45,
        .maximum_shift_duration_s = 1.5,
        .shift_torque_hole_fraction = 0.25,
        .maximum_lockup_torque_nm = 300.0,
        .lockup_apply_time_constant_s = 0.25,
        .lockup_release_time_constant_s = 0.08,
        .lockup_slip_smoothing_rad_s = 2.0,
        .ambient_temperature_k = 298.15,
        .initial_oil_temperature_k = 303.15,
        .initial_lockup_temperature_k = 303.15,
        .oil_thermal_capacity_j_k = 30000.0,
        .oil_cooling_conductance_w_k = 60.0,
        .lockup_thermal_capacity_j_k = 1500.0,
        .lockup_to_oil_conductance_w_k = 40.0,
        .lockup_heat_fraction = 0.7,
        .oil_overtemperature_k = 408.15,
        .lockup_overtemperature_k = 473.15,
        .hydraulic_base_loss_w = 80.0,
        .hydraulic_pressure_speed_loss_m3 = 0.00000006,
        .rotating_loss_nm_s_rad = 0.006,
        .ratio_error_threshold_rad_s = 25.0,
        .ratio_error_delay_s = 0.5,
        .lockup_slip_threshold_rad_s = 15.0,
        .lockup_slip_delay_s = 0.4,
        .maximum_input_torque_nm = 500.0,
        .maximum_external_torque_nm = 2000.0,
        .maximum_shaft_speed_rad_s = 1500.0,
    };
}

pub fn pwr_automatic_gear_ratio(arg_config: [*c]const pwr_automatic_config, arg_gear: pwr_automatic_gear, arg_ratio: [*c]f64) callconv(.c) pwr_automatic_result {
    var config = arg_config;
    _ = &config;
    var gear = arg_gear;
    _ = &gear;
    var ratio = arg_ratio;
    _ = &ratio;
    if ((config == null) or (ratio == null)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_ARGUMENT));
    }
    if (!pwr_automatic_config_valid(config)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_CONFIG));
    }
    while (true) {
        switch (gear) {
            @as(c_int, -1) => {
                ratio.* = config.*.reverse_ratio;
                return @as(c_uint, @bitCast(PWR_AUTOMATIC_OK));
            },
            @as(c_int, 0) => {
                ratio.* = 0.0;
                return @as(c_uint, @bitCast(PWR_AUTOMATIC_OK));
            },
            @as(c_int, 1), @as(c_int, 2), @as(c_int, 3), @as(c_int, 4) => {
                {
                    const index: usize = @as(usize, @bitCast(@as(i64, gear - PWR_AUTOMATIC_GEAR_FIRST)));
                    _ = &index;
                    ratio.* = config.*.forward_ratios[index];
                    return @as(c_uint, @bitCast(PWR_AUTOMATIC_OK));
                }
            },
            else => return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_INPUT)),
        }
        break;
    }
    return @import("std").mem.zeroes(pwr_automatic_result);
}

pub fn pwr_automatic_converter_evaluate(arg_config: [*c]const pwr_automatic_config, arg_pump_speed_rad_s: f64, arg_turbine_speed_rad_s: f64, arg_point: [*c]pwr_automatic_converter_point) callconv(.c) pwr_automatic_result {
    var config = arg_config;
    _ = &config;
    var pump_speed_rad_s = arg_pump_speed_rad_s;
    _ = &pump_speed_rad_s;
    var turbine_speed_rad_s = arg_turbine_speed_rad_s;
    _ = &turbine_speed_rad_s;
    var point = arg_point;
    _ = &point;
    if ((config == null) or (point == null)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_ARGUMENT));
    }
    if (!pwr_automatic_config_valid(config)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_CONFIG));
    }
    if (((!(power_isfinite(pump_speed_rad_s) != 0) or !(power_isfinite(turbine_speed_rad_s) != 0)) or (fabs(pump_speed_rad_s) > config.*.maximum_shaft_speed_rad_s)) or (fabs(turbine_speed_rad_s) > config.*.maximum_shaft_speed_rad_s)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_INPUT));
    }
    const pump_magnitude: f64 = fabs(pump_speed_rad_s);
    _ = &pump_magnitude;
    const slip: f64 = pump_speed_rad_s - turbine_speed_rad_s;
    _ = &slip;
    const capacity_torque: f64 = (pump_magnitude / config.*.converter_k_rad_s_sqrt_nm) * (pump_magnitude / config.*.converter_k_rad_s_sqrt_nm);
    _ = &capacity_torque;
    const pump_torque: f64 = capacity_torque * tanh(slip / config.*.converter_slip_smoothing_rad_s);
    _ = &pump_torque;
    var speed_ratio: f64 = 0.0;
    _ = &speed_ratio;
    if (pump_magnitude > 0.000000000001) {
        speed_ratio = turbine_speed_rad_s / pump_speed_rad_s;
    }
    const multiplication_progress: f64 = pwr_automatic_clamp(speed_ratio / config.*.converter_coupling_speed_ratio, 0.0, 1.0);
    _ = &multiplication_progress;
    const torque_ratio: f64 = if (pump_torque >= 0.0) 1.0 + (((config.*.converter_stall_torque_ratio - 1.0) * (1.0 - multiplication_progress)) * (1.0 - multiplication_progress)) else 1.0;
    _ = &torque_ratio;
    const turbine_torque: f64 = pump_torque * torque_ratio;
    _ = &turbine_torque;
    const raw_slip_power: f64 = (pump_torque * pump_speed_rad_s) - (turbine_torque * turbine_speed_rad_s);
    _ = &raw_slip_power;
    point.* = pwr_automatic_converter_point{
        .speed_ratio = speed_ratio,
        .torque_ratio = torque_ratio,
        .pump_torque_nm = pump_torque,
        .turbine_torque_nm = turbine_torque,
        .slip_power_w = fmax(raw_slip_power, 0.0),
    };
    return @as(c_uint, @bitCast(PWR_AUTOMATIC_OK));
}

pub fn pwr_automatic_init(arg_automatic: [*c]pwr_automatic, arg_config: [*c]const pwr_automatic_config) callconv(.c) pwr_automatic_result {
    var automatic = arg_automatic;
    _ = &automatic;
    var config = arg_config;
    _ = &config;
    if ((automatic == null) or (config == null)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_ARGUMENT));
    }
    if (!pwr_automatic_config_valid(config)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_CONFIG));
    }
    var candidate: pwr_automatic = undefined;
    _ = &candidate;
    _ = memset(@as(?*anyopaque, @ptrCast(&candidate)), @as(c_int, 0), @sizeOf(pwr_automatic));
    candidate.config = config.*;
    candidate.active_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
    candidate.shift_from_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
    candidate.target_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
    candidate.oil_temperature_k = config.*.initial_oil_temperature_k;
    candidate.lockup_temperature_k = config.*.initial_lockup_temperature_k;
    automatic.* = candidate;
    return @as(c_uint, @bitCast(PWR_AUTOMATIC_OK));
}

pub fn pwr_automatic_step(arg_automatic: [*c]pwr_automatic, arg_input: [*c]const pwr_automatic_input) callconv(.c) pwr_automatic_result {
    var automatic = arg_automatic;
    _ = &automatic;
    var input = arg_input;
    _ = &input;
    return pwr_automatic_step_internal(automatic, input, @as(c_int, 0) != 0, 0.0, null);
}

pub fn pwr_automatic_step_coupled(arg_automatic: [*c]pwr_automatic, arg_input: [*c]const pwr_automatic_coupled_input, arg_coupling_output: [*c]pwr_automatic_coupling_output) callconv(.c) pwr_automatic_result {
    var automatic = arg_automatic;
    _ = &automatic;
    var input = arg_input;
    _ = &input;
    var coupling_output = arg_coupling_output;
    _ = &coupling_output;
    if (((automatic == null) or (input == null)) or (coupling_output == null)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_ARGUMENT));
    }
    const internal_input: pwr_automatic_input = pwr_automatic_input{
        .engine_torque_nm = 0.0,
        .output_external_torque_nm = input.*.output_external_torque_nm,
        .line_pressure_command_pa = input.*.line_pressure_command_pa,
        .lockup_command = input.*.lockup_command,
        .requested_gear = input.*.requested_gear,
    };
    _ = &internal_input;
    var candidate_output: pwr_automatic_coupling_output = undefined;
    _ = &candidate_output;
    const result: pwr_automatic_result = pwr_automatic_step_internal(automatic, &internal_input, @as(c_int, 1) != 0, input.*.pump_speed_rad_s, &candidate_output);
    _ = &result;
    if (result == @as(c_uint, @bitCast(PWR_AUTOMATIC_OK))) {
        coupling_output.* = candidate_output;
    }
    return result;
}

pub fn pwr_automatic_snapshot_read(arg_automatic: [*c]const pwr_automatic, arg_snapshot: [*c]pwr_automatic_snapshot) callconv(.c) pwr_automatic_result {
    var automatic = arg_automatic;
    _ = &automatic;
    var snapshot = arg_snapshot;
    _ = &snapshot;
    if ((automatic == null) or (snapshot == null)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_ARGUMENT));
    }
    if (!pwr_automatic_config_valid(&automatic.*.config)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_CONFIG));
    }
    if (!pwr_automatic_state_finite(automatic)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_NUMERIC_ERROR));
    }
    snapshot.* = pwr_automatic_snapshot{
        .time_s = (@as(f64, @floatFromInt(automatic.*.tick)) * @as(f64, @floatFromInt(automatic.*.config.step_ns))) * 0.000000001,
        .pump_speed_rad_s = automatic.*.pump_speed_rad_s,
        .turbine_speed_rad_s = automatic.*.turbine_speed_rad_s,
        .output_speed_rad_s = automatic.*.output_speed_rad_s,
        .active_gear = automatic.*.active_gear,
        .target_gear = automatic.*.target_gear,
        .shift_active = automatic.*.shift_active,
        .shift_progress = automatic.*.shift_progress,
        .effective_ratio = automatic.*.effective_ratio,
        .gear_torque_factor = automatic.*.gear_torque_factor,
        .line_pressure_pa = automatic.*.line_pressure_pa,
        .lockup_engagement = automatic.*.lockup_engagement,
        .converter_speed_ratio = automatic.*.converter.speed_ratio,
        .converter_torque_ratio = automatic.*.converter.torque_ratio,
        .converter_pump_torque_nm = automatic.*.converter.pump_torque_nm,
        .converter_turbine_torque_nm = automatic.*.converter.turbine_torque_nm,
        .lockup_torque_nm = automatic.*.lockup_torque_nm,
        .gear_input_torque_nm = automatic.*.gear_input_torque_nm,
        .gear_output_torque_nm = automatic.*.gear_output_torque_nm,
        .converter_slip_rad_s = automatic.*.pump_speed_rad_s - automatic.*.turbine_speed_rad_s,
        .lockup_slip_rad_s = automatic.*.pump_speed_rad_s - automatic.*.turbine_speed_rad_s,
        .gear_clutch_slip_rad_s = automatic.*.gear_clutch_slip_rad_s,
        .converter_loss_w = automatic.*.converter_loss_w,
        .lockup_loss_w = automatic.*.lockup_loss_w,
        .gear_clutch_loss_w = automatic.*.gear_clutch_loss_w,
        .hydraulic_loss_w = automatic.*.hydraulic_loss_w,
        .rotating_loss_w = automatic.*.rotating_loss_w,
        .oil_temperature_k = automatic.*.oil_temperature_k,
        .lockup_temperature_k = automatic.*.lockup_temperature_k,
        .accumulated_loss_j = automatic.*.accumulated_loss_j,
        .pump_speed_imposed = automatic.*.pump_speed_imposed,
        .engine_reaction_torque_nm = automatic.*.engine_reaction_torque_nm,
        .pump_boundary_power_w = automatic.*.pump_boundary_power_w,
        .pump_boundary_energy_j = automatic.*.pump_boundary_energy_j,
        .coupling_adjustment_j = automatic.*.coupling_adjustment_j,
        .diagnostic_flags = automatic.*.diagnostic_flags,
        .state_hash = pwr_automatic_state_hash(automatic),
    };
    return @as(c_uint, @bitCast(PWR_AUTOMATIC_OK));
}

pub fn pwr_automatic_state_hash(arg_automatic: [*c]const pwr_automatic) callconv(.c) u64 {
    var automatic = arg_automatic;
    _ = &automatic;
    if (automatic == null) {
        return 0;
    }
    var hash: u64 = 1469598103934665603;
    _ = &hash;
    hash = pwr_automatic_hash_bytes(hash, @as(?*const anyopaque, @ptrCast(&automatic.*.tick)), @sizeOf(u64));
    hash = pwr_automatic_hash_double(hash, automatic.*.pump_speed_rad_s);
    hash = pwr_automatic_hash_double(hash, automatic.*.turbine_speed_rad_s);
    hash = pwr_automatic_hash_double(hash, automatic.*.output_speed_rad_s);
    hash = pwr_automatic_hash_bytes(hash, @as(?*const anyopaque, @ptrCast(&automatic.*.active_gear)), @sizeOf(pwr_automatic_gear));
    hash = pwr_automatic_hash_bytes(hash, @as(?*const anyopaque, @ptrCast(&automatic.*.target_gear)), @sizeOf(pwr_automatic_gear));
    hash = pwr_automatic_hash_bytes(hash, @as(?*const anyopaque, @ptrCast(&automatic.*.shift_active)), @sizeOf(bool));
    hash = pwr_automatic_hash_double(hash, automatic.*.shift_from_ratio);
    hash = pwr_automatic_hash_double(hash, automatic.*.shift_to_ratio);
    hash = pwr_automatic_hash_double(hash, automatic.*.shift_progress);
    hash = pwr_automatic_hash_double(hash, automatic.*.shift_elapsed_s);
    hash = pwr_automatic_hash_double(hash, automatic.*.settled_elapsed_s);
    hash = pwr_automatic_hash_double(hash, automatic.*.effective_ratio);
    hash = pwr_automatic_hash_double(hash, automatic.*.gear_torque_factor);
    hash = pwr_automatic_hash_double(hash, automatic.*.line_pressure_pa);
    hash = pwr_automatic_hash_double(hash, automatic.*.lockup_engagement);
    hash = pwr_automatic_hash_double(hash, automatic.*.lockup_command_elapsed_s);
    hash = pwr_automatic_hash_double(hash, automatic.*.oil_temperature_k);
    hash = pwr_automatic_hash_double(hash, automatic.*.lockup_temperature_k);
    hash = pwr_automatic_hash_double(hash, automatic.*.converter.pump_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic.*.converter.turbine_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic.*.lockup_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic.*.gear_input_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic.*.gear_output_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic.*.gear_clutch_slip_rad_s);
    hash = pwr_automatic_hash_double(hash, automatic.*.accumulated_loss_j);
    hash = pwr_automatic_hash_bytes(hash, @as(?*const anyopaque, @ptrCast(&automatic.*.pump_speed_imposed)), @sizeOf(bool));
    hash = pwr_automatic_hash_double(hash, automatic.*.engine_reaction_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic.*.pump_boundary_power_w);
    hash = pwr_automatic_hash_double(hash, automatic.*.pump_boundary_energy_j);
    hash = pwr_automatic_hash_double(hash, automatic.*.coupling_adjustment_j);
    hash = pwr_automatic_hash_bytes(hash, @as(?*const anyopaque, @ptrCast(&automatic.*.diagnostic_flags)), @sizeOf(u32));
    return hash;
}

pub fn pwr_automatic_clamp(arg_value: f64, arg_minimum: f64, arg_maximum: f64) callconv(.c) f64 {
    var value = arg_value;
    _ = &value;
    var minimum = arg_minimum;
    _ = &minimum;
    var maximum = arg_maximum;
    _ = &maximum;
    return fmin(fmax(value, minimum), maximum);
}

pub fn pwr_automatic_positive_finite(arg_value: f64) callconv(.c) bool {
    var value = arg_value;
    _ = &value;
    return (power_isfinite(value) != 0) and (value > 0.0);
}

pub fn pwr_automatic_nonnegative_finite(arg_value: f64) callconv(.c) bool {
    var value = arg_value;
    _ = &value;
    return (power_isfinite(value) != 0) and (value >= 0.0);
}

pub fn pwr_automatic_gear_valid(arg_gear: pwr_automatic_gear) callconv(.c) bool {
    var gear = arg_gear;
    _ = &gear;
    return (((((gear == PWR_AUTOMATIC_GEAR_REVERSE) or (gear == PWR_AUTOMATIC_GEAR_NEUTRAL)) or (gear == PWR_AUTOMATIC_GEAR_FIRST)) or (gear == PWR_AUTOMATIC_GEAR_SECOND)) or (gear == PWR_AUTOMATIC_GEAR_THIRD)) or (gear == PWR_AUTOMATIC_GEAR_FOURTH);
}

pub fn pwr_automatic_config_valid(arg_config: [*c]const pwr_automatic_config) callconv(.c) bool {
    var config = arg_config;
    _ = &config;
    if (((config == null) or (config.*.step_ns == @as(u64, @bitCast(@as(u64, @as(c_uint, 0)))))) or (config.*.step_ns > @as(u64, @bitCast(@as(u64, @as(c_uint, 10000000)))))) {
        return @as(c_int, 0) != 0;
    }
    {
        var index: usize = 0;
        _ = &index;
        while (index < @as(usize, @bitCast(@as(u64, @as(c_uint, 4))))) : (index +%= 1) {
            if (!pwr_automatic_positive_finite(config.*.forward_ratios[index])) {
                return @as(c_int, 0) != 0;
            }
            if ((index > @as(usize, @bitCast(@as(u64, @as(c_uint, 0))))) and (config.*.forward_ratios[index -% @as(usize, @bitCast(@as(u64, @as(c_uint, 1))))] <= config.*.forward_ratios[index])) {
                return @as(c_int, 0) != 0;
            }
        }
    }
    if (!(power_isfinite(config.*.reverse_ratio) != 0) or (config.*.reverse_ratio >= 0.0)) {
        return @as(c_int, 0) != 0;
    }
    return (((((((((((((((((((((((((((((((((((((((((((((((((((@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.pump_inertia_kg_m2))) != 0) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.turbine_inertia_kg_m2))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.output_inertia_kg_m2))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.pump_viscous_drag_nm_s_rad))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.turbine_viscous_drag_nm_s_rad))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.output_viscous_drag_nm_s_rad))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.converter_k_rad_s_sqrt_nm))) != 0)) and (power_isfinite(config.*.converter_stall_torque_ratio) != 0)) and (config.*.converter_stall_torque_ratio >= 1.0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.converter_coupling_speed_ratio))) != 0)) and (config.*.converter_coupling_speed_ratio <= 1.0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.converter_slip_smoothing_rad_s))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.nominal_line_pressure_pa))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.maximum_line_pressure_pa))) != 0)) and (config.*.maximum_line_pressure_pa >= config.*.nominal_line_pressure_pa)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.minimum_drive_pressure_pa))) != 0)) and (config.*.minimum_drive_pressure_pa <= config.*.nominal_line_pressure_pa)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.pressure_time_constant_s))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.maximum_gear_clutch_torque_nm))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.gear_clutch_slip_smoothing_rad_s))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.nominal_shift_duration_s))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.maximum_shift_duration_s))) != 0)) and (config.*.maximum_shift_duration_s >= config.*.nominal_shift_duration_s)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.shift_torque_hole_fraction))) != 0)) and (config.*.shift_torque_hole_fraction < 1.0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.maximum_lockup_torque_nm))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.lockup_apply_time_constant_s))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.lockup_release_time_constant_s))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.lockup_slip_smoothing_rad_s))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.ambient_temperature_k))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.initial_oil_temperature_k))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.initial_lockup_temperature_k))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.oil_thermal_capacity_j_k))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.oil_cooling_conductance_w_k))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.lockup_thermal_capacity_j_k))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.lockup_to_oil_conductance_w_k))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.lockup_heat_fraction))) != 0)) and (config.*.lockup_heat_fraction <= 1.0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.oil_overtemperature_k))) != 0)) and (config.*.oil_overtemperature_k > config.*.ambient_temperature_k)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.lockup_overtemperature_k))) != 0)) and (config.*.lockup_overtemperature_k > config.*.ambient_temperature_k)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.hydraulic_base_loss_w))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.hydraulic_pressure_speed_loss_m3))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.rotating_loss_nm_s_rad))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.ratio_error_threshold_rad_s))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.ratio_error_delay_s))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.lockup_slip_threshold_rad_s))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_nonnegative_finite(config.*.lockup_slip_delay_s))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.maximum_input_torque_nm))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.maximum_external_torque_nm))) != 0)) and (@as(c_int, @intFromBool(pwr_automatic_positive_finite(config.*.maximum_shaft_speed_rad_s))) != 0);
}

pub fn pwr_automatic_input_valid(arg_config: [*c]const pwr_automatic_config, arg_input: [*c]const pwr_automatic_input) callconv(.c) bool {
    var config = arg_config;
    _ = &config;
    var input = arg_input;
    _ = &input;
    return (((((((((((input != null) and (@as(c_int, @intFromBool(pwr_automatic_gear_valid(input.*.requested_gear))) != 0)) and (power_isfinite(input.*.engine_torque_nm) != 0)) and (fabs(input.*.engine_torque_nm) <= config.*.maximum_input_torque_nm)) and (power_isfinite(input.*.output_external_torque_nm) != 0)) and (fabs(input.*.output_external_torque_nm) <= config.*.maximum_external_torque_nm)) and (power_isfinite(input.*.line_pressure_command_pa) != 0)) and (input.*.line_pressure_command_pa >= 0.0)) and (input.*.line_pressure_command_pa <= config.*.maximum_line_pressure_pa)) and (power_isfinite(input.*.lockup_command) != 0)) and (input.*.lockup_command >= 0.0)) and (input.*.lockup_command <= 1.0);
}

pub fn pwr_automatic_smoothstep(arg_progress: f64) callconv(.c) f64 {
    var progress = arg_progress;
    _ = &progress;
    const clamped: f64 = pwr_automatic_clamp(progress, 0.0, 1.0);
    _ = &clamped;
    return (clamped * clamped) * (3.0 - (2.0 * clamped));
}

pub fn pwr_automatic_first_order_alpha(arg_dt: f64, arg_time_constant: f64) callconv(.c) f64 {
    var dt = arg_dt;
    _ = &dt;
    var time_constant = arg_time_constant;
    _ = &time_constant;
    return 1.0 - exp(-dt / time_constant);
}

pub fn pwr_automatic_state_finite(arg_automatic: [*c]const pwr_automatic) callconv(.c) bool {
    var automatic = arg_automatic;
    _ = &automatic;
    return (((((((((((((((((((((((((((((((((power_isfinite(automatic.*.pump_speed_rad_s) != 0) and (power_isfinite(automatic.*.turbine_speed_rad_s) != 0)) and (power_isfinite(automatic.*.output_speed_rad_s) != 0)) and (power_isfinite(automatic.*.shift_from_ratio) != 0)) and (power_isfinite(automatic.*.shift_to_ratio) != 0)) and (power_isfinite(automatic.*.shift_progress) != 0)) and (power_isfinite(automatic.*.shift_elapsed_s) != 0)) and (power_isfinite(automatic.*.settled_elapsed_s) != 0)) and (power_isfinite(automatic.*.effective_ratio) != 0)) and (power_isfinite(automatic.*.gear_torque_factor) != 0)) and (power_isfinite(automatic.*.line_pressure_pa) != 0)) and (power_isfinite(automatic.*.lockup_engagement) != 0)) and (power_isfinite(automatic.*.lockup_command_elapsed_s) != 0)) and (power_isfinite(automatic.*.oil_temperature_k) != 0)) and (power_isfinite(automatic.*.lockup_temperature_k) != 0)) and (power_isfinite(automatic.*.converter.speed_ratio) != 0)) and (power_isfinite(automatic.*.converter.torque_ratio) != 0)) and (power_isfinite(automatic.*.converter.pump_torque_nm) != 0)) and (power_isfinite(automatic.*.converter.turbine_torque_nm) != 0)) and (power_isfinite(automatic.*.converter.slip_power_w) != 0)) and (power_isfinite(automatic.*.lockup_torque_nm) != 0)) and (power_isfinite(automatic.*.gear_input_torque_nm) != 0)) and (power_isfinite(automatic.*.gear_output_torque_nm) != 0)) and (power_isfinite(automatic.*.gear_clutch_slip_rad_s) != 0)) and (power_isfinite(automatic.*.converter_loss_w) != 0)) and (power_isfinite(automatic.*.lockup_loss_w) != 0)) and (power_isfinite(automatic.*.gear_clutch_loss_w) != 0)) and (power_isfinite(automatic.*.hydraulic_loss_w) != 0)) and (power_isfinite(automatic.*.rotating_loss_w) != 0)) and (power_isfinite(automatic.*.accumulated_loss_j) != 0)) and (power_isfinite(automatic.*.engine_reaction_torque_nm) != 0)) and (power_isfinite(automatic.*.pump_boundary_power_w) != 0)) and (power_isfinite(automatic.*.pump_boundary_energy_j) != 0)) and (power_isfinite(automatic.*.coupling_adjustment_j) != 0);
}

pub fn pwr_automatic_begin_shift(arg_automatic: [*c]pwr_automatic, arg_requested_gear: pwr_automatic_gear, arg_requested_ratio: f64) callconv(.c) void {
    var automatic = arg_automatic;
    _ = &automatic;
    var requested_gear = arg_requested_gear;
    _ = &requested_gear;
    var requested_ratio = arg_requested_ratio;
    _ = &requested_ratio;
    automatic.*.shift_from_gear = automatic.*.active_gear;
    automatic.*.target_gear = requested_gear;
    automatic.*.shift_from_ratio = automatic.*.effective_ratio;
    automatic.*.shift_to_ratio = requested_ratio;
    automatic.*.shift_progress = 0.0;
    automatic.*.shift_elapsed_s = 0.0;
    automatic.*.settled_elapsed_s = 0.0;
    automatic.*.shift_active = @as(c_int, 1) != 0;
}

pub fn pwr_automatic_update_shift(arg_automatic: [*c]pwr_automatic, arg_dt: f64) callconv(.c) void {
    var automatic = arg_automatic;
    _ = &automatic;
    var dt = arg_dt;
    _ = &dt;
    var config: [*c]const pwr_automatic_config = &automatic.*.config;
    _ = &config;
    if (!automatic.*.shift_active) {
        automatic.*.effective_ratio = automatic.*.shift_to_ratio;
        automatic.*.gear_torque_factor = if (automatic.*.active_gear == PWR_AUTOMATIC_GEAR_NEUTRAL) 0.0 else 1.0;
        automatic.*.settled_elapsed_s += dt;
        return;
    }
    automatic.*.shift_elapsed_s += dt;
    const pressure_authority: f64 = pwr_automatic_clamp(automatic.*.line_pressure_pa / config.*.nominal_line_pressure_pa, 0.0, 1.25);
    _ = &pressure_authority;
    automatic.*.shift_progress = pwr_automatic_clamp(automatic.*.shift_progress + ((dt * pressure_authority) / config.*.nominal_shift_duration_s), 0.0, 1.0);
    const blend: f64 = pwr_automatic_smoothstep(automatic.*.shift_progress);
    _ = &blend;
    automatic.*.effective_ratio = automatic.*.shift_from_ratio + ((automatic.*.shift_to_ratio - automatic.*.shift_from_ratio) * blend);
    if (fabs(automatic.*.shift_from_ratio) < 0.000000000001) {
        automatic.*.gear_torque_factor = blend;
    } else if (fabs(automatic.*.shift_to_ratio) < 0.000000000001) {
        automatic.*.gear_torque_factor = 1.0 - blend;
    } else {
        automatic.*.gear_torque_factor = 1.0 - (((config.*.shift_torque_hole_fraction * 4.0) * blend) * (1.0 - blend));
    }
    if (automatic.*.shift_progress >= 1.0) {
        automatic.*.active_gear = automatic.*.target_gear;
        automatic.*.shift_from_gear = automatic.*.target_gear;
        automatic.*.shift_from_ratio = automatic.*.shift_to_ratio;
        automatic.*.effective_ratio = automatic.*.shift_to_ratio;
        automatic.*.gear_torque_factor = if (automatic.*.active_gear == PWR_AUTOMATIC_GEAR_NEUTRAL) 0.0 else 1.0;
        automatic.*.shift_active = @as(c_int, 0) != 0;
        automatic.*.settled_elapsed_s = 0.0;
    }
}

pub fn pwr_automatic_limit_speed(arg_speed: [*c]f64, arg_maximum: f64, arg_diagnostics: [*c]u32) callconv(.c) void {
    var speed = arg_speed;
    _ = &speed;
    var maximum = arg_maximum;
    _ = &maximum;
    var diagnostics = arg_diagnostics;
    _ = &diagnostics;
    if (fabs(speed.*) > maximum) {
        speed.* = pwr_automatic_clamp(speed.*, -maximum, maximum);
        diagnostics.* |= @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_OVERSPEED | PWR_AUTOMATIC_DIAG_NUMERIC_LIMIT));
    }
}

pub fn pwr_automatic_step_internal(arg_automatic: [*c]pwr_automatic, arg_input: [*c]const pwr_automatic_input, arg_impose_pump_speed: bool, arg_imposed_pump_speed_rad_s: f64, arg_coupling_output: [*c]pwr_automatic_coupling_output) callconv(.c) pwr_automatic_result {
    var automatic = arg_automatic;
    _ = &automatic;
    var input = arg_input;
    _ = &input;
    var impose_pump_speed = arg_impose_pump_speed;
    _ = &impose_pump_speed;
    var imposed_pump_speed_rad_s = arg_imposed_pump_speed_rad_s;
    _ = &imposed_pump_speed_rad_s;
    var coupling_output = arg_coupling_output;
    _ = &coupling_output;
    if ((automatic == null) or (input == null)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_ARGUMENT));
    }
    if (!pwr_automatic_config_valid(&automatic.*.config)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_CONFIG));
    }
    if (!pwr_automatic_input_valid(&automatic.*.config, input)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_INPUT));
    }
    if ((@as(c_int, @intFromBool(impose_pump_speed)) != 0) and ((!(power_isfinite(imposed_pump_speed_rad_s) != 0) or (imposed_pump_speed_rad_s < 0.0)) or (imposed_pump_speed_rad_s > automatic.*.config.maximum_shaft_speed_rad_s))) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_INPUT));
    }
    if ((automatic.*.tick == @as(u64, 18446744073709551615)) or !pwr_automatic_state_finite(automatic)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_NUMERIC_ERROR));
    }
    var next: pwr_automatic = automatic.*;
    _ = &next;
    var config: [*c]const pwr_automatic_config = &next.config;
    _ = &config;
    const dt: f64 = @as(f64, @floatFromInt(config.*.step_ns)) * 0.000000001;
    _ = &dt;
    var projection_adjustment_j: f64 = 0.0;
    _ = &projection_adjustment_j;
    next.pump_speed_imposed = impose_pump_speed;
    next.pump_boundary_power_w = 0.0;
    if (impose_pump_speed) {
        projection_adjustment_j = (0.5 * config.*.pump_inertia_kg_m2) * ((imposed_pump_speed_rad_s * imposed_pump_speed_rad_s) - (next.pump_speed_rad_s * next.pump_speed_rad_s));
        if (imposed_pump_speed_rad_s != next.pump_speed_rad_s) {
            next.diagnostic_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_COUPLING_SPEED_PROJECTED));
        }
        next.coupling_adjustment_j += projection_adjustment_j;
        next.pump_speed_rad_s = imposed_pump_speed_rad_s;
    }
    var requested_ratio: f64 = 0.0;
    _ = &requested_ratio;
    const ratio_result: pwr_automatic_result = pwr_automatic_gear_ratio(config, input.*.requested_gear, &requested_ratio);
    _ = &ratio_result;
    if (ratio_result != @as(c_uint, @bitCast(PWR_AUTOMATIC_OK))) {
        return ratio_result;
    }
    if (input.*.requested_gear != next.target_gear) {
        pwr_automatic_begin_shift(&next, input.*.requested_gear, requested_ratio);
    }
    const pressure_alpha: f64 = pwr_automatic_first_order_alpha(dt, config.*.pressure_time_constant_s);
    _ = &pressure_alpha;
    next.line_pressure_pa += (input.*.line_pressure_command_pa - next.line_pressure_pa) * pressure_alpha;
    pwr_automatic_update_shift(&next, dt);
    const permitted_lockup_command: f64 = if ((@as(c_int, @intFromBool(next.shift_active)) != 0) or (next.target_gear == PWR_AUTOMATIC_GEAR_NEUTRAL)) 0.0 else input.*.lockup_command;
    _ = &permitted_lockup_command;
    const lockup_time_constant: f64 = if (permitted_lockup_command >= next.lockup_engagement) config.*.lockup_apply_time_constant_s else config.*.lockup_release_time_constant_s;
    _ = &lockup_time_constant;
    const lockup_alpha: f64 = pwr_automatic_first_order_alpha(dt, lockup_time_constant);
    _ = &lockup_alpha;
    next.lockup_engagement += (permitted_lockup_command - next.lockup_engagement) * lockup_alpha;
    next.lockup_engagement = pwr_automatic_clamp(next.lockup_engagement, 0.0, 1.0);
    if ((input.*.lockup_command >= 0.8) and !next.shift_active) {
        next.lockup_command_elapsed_s += dt;
    } else {
        next.lockup_command_elapsed_s = 0.0;
    }
    var converter: pwr_automatic_converter_point = undefined;
    _ = &converter;
    const converter_result: pwr_automatic_result = pwr_automatic_converter_evaluate(config, next.pump_speed_rad_s, next.turbine_speed_rad_s, &converter);
    _ = &converter_result;
    if (converter_result != @as(c_uint, @bitCast(PWR_AUTOMATIC_OK))) {
        return if (converter_result == @as(c_uint, @bitCast(PWR_AUTOMATIC_INVALID_INPUT))) @as(c_uint, @bitCast(PWR_AUTOMATIC_NUMERIC_ERROR)) else converter_result;
    }
    next.converter = converter;
    const pressure_fraction: f64 = pwr_automatic_clamp(next.line_pressure_pa / config.*.nominal_line_pressure_pa, 0.0, config.*.maximum_line_pressure_pa / config.*.nominal_line_pressure_pa);
    _ = &pressure_fraction;
    const lockup_slip: f64 = next.pump_speed_rad_s - next.turbine_speed_rad_s;
    _ = &lockup_slip;
    const lockup_capacity: f64 = (config.*.maximum_lockup_torque_nm * pressure_fraction) * next.lockup_engagement;
    _ = &lockup_capacity;
    next.lockup_torque_nm = lockup_capacity * tanh(lockup_slip / config.*.lockup_slip_smoothing_rad_s);
    next.gear_clutch_slip_rad_s = next.turbine_speed_rad_s - (next.effective_ratio * next.output_speed_rad_s);
    const gear_capacity: f64 = (config.*.maximum_gear_clutch_torque_nm * pressure_fraction) * next.gear_torque_factor;
    _ = &gear_capacity;
    next.gear_input_torque_nm = gear_capacity * tanh(next.gear_clutch_slip_rad_s / config.*.gear_clutch_slip_smoothing_rad_s);
    next.gear_output_torque_nm = next.gear_input_torque_nm * next.effective_ratio;
    next.converter_loss_w = converter.slip_power_w;
    next.lockup_loss_w = fmax(next.lockup_torque_nm * lockup_slip, 0.0);
    next.gear_clutch_loss_w = fmax(next.gear_input_torque_nm * next.gear_clutch_slip_rad_s, 0.0);
    const pump_sign: f64 = tanh(next.pump_speed_rad_s / 5.0);
    _ = &pump_sign;
    const hydraulic_drag_torque: f64 = pump_sign * ((config.*.hydraulic_base_loss_w / (fabs(next.pump_speed_rad_s) + 20.0)) + (next.line_pressure_pa * config.*.hydraulic_pressure_speed_loss_m3));
    _ = &hydraulic_drag_torque;
    next.hydraulic_loss_w = fabs(hydraulic_drag_torque * next.pump_speed_rad_s);
    const pump_drag_coefficient: f64 = config.*.pump_viscous_drag_nm_s_rad + config.*.rotating_loss_nm_s_rad;
    _ = &pump_drag_coefficient;
    const turbine_drag_coefficient: f64 = config.*.turbine_viscous_drag_nm_s_rad + config.*.rotating_loss_nm_s_rad;
    _ = &turbine_drag_coefficient;
    const output_drag_coefficient: f64 = config.*.output_viscous_drag_nm_s_rad + config.*.rotating_loss_nm_s_rad;
    _ = &output_drag_coefficient;
    next.rotating_loss_w = (((pump_drag_coefficient * next.pump_speed_rad_s) * next.pump_speed_rad_s) + ((turbine_drag_coefficient * next.turbine_speed_rad_s) * next.turbine_speed_rad_s)) + ((output_drag_coefficient * next.output_speed_rad_s) * next.output_speed_rad_s);
    const pump_load_torque_nm: f64 = ((converter.pump_torque_nm + next.lockup_torque_nm) + hydraulic_drag_torque) + (pump_drag_coefficient * next.pump_speed_rad_s);
    _ = &pump_load_torque_nm;
    next.engine_reaction_torque_nm = -pump_load_torque_nm;
    if (impose_pump_speed) {
        next.pump_boundary_power_w = pump_load_torque_nm * next.pump_speed_rad_s;
        next.pump_boundary_energy_j += next.pump_boundary_power_w * dt;
    }
    const pump_acceleration: f64 = (input.*.engine_torque_nm - pump_load_torque_nm) / config.*.pump_inertia_kg_m2;
    _ = &pump_acceleration;
    const turbine_acceleration: f64 = (((converter.turbine_torque_nm + next.lockup_torque_nm) - next.gear_input_torque_nm) - (turbine_drag_coefficient * next.turbine_speed_rad_s)) / config.*.turbine_inertia_kg_m2;
    _ = &turbine_acceleration;
    const output_acceleration: f64 = ((next.gear_output_torque_nm + input.*.output_external_torque_nm) - (output_drag_coefficient * next.output_speed_rad_s)) / config.*.output_inertia_kg_m2;
    _ = &output_acceleration;
    if (!impose_pump_speed) {
        next.pump_speed_rad_s += pump_acceleration * dt;
    }
    next.turbine_speed_rad_s += turbine_acceleration * dt;
    next.output_speed_rad_s += output_acceleration * dt;
    if (!impose_pump_speed) {
        if (next.pump_speed_rad_s < 0.0) {
            next.pump_speed_rad_s = 0.0;
            next.diagnostic_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_NUMERIC_LIMIT));
        }
        pwr_automatic_limit_speed(&next.pump_speed_rad_s, config.*.maximum_shaft_speed_rad_s, &next.diagnostic_flags);
    }
    pwr_automatic_limit_speed(&next.turbine_speed_rad_s, config.*.maximum_shaft_speed_rad_s, &next.diagnostic_flags);
    pwr_automatic_limit_speed(&next.output_speed_rad_s, config.*.maximum_shaft_speed_rad_s, &next.diagnostic_flags);
    const lockup_to_oil_w: f64 = config.*.lockup_to_oil_conductance_w_k * (next.lockup_temperature_k - next.oil_temperature_k);
    _ = &lockup_to_oil_w;
    const oil_cooling_w: f64 = config.*.oil_cooling_conductance_w_k * (next.oil_temperature_k - config.*.ambient_temperature_k);
    _ = &oil_cooling_w;
    const lockup_heat_w: f64 = config.*.lockup_heat_fraction * next.lockup_loss_w;
    _ = &lockup_heat_w;
    const oil_heat_w: f64 = ((((next.converter_loss_w + next.gear_clutch_loss_w) + next.hydraulic_loss_w) + next.rotating_loss_w) + ((1.0 - config.*.lockup_heat_fraction) * next.lockup_loss_w)) + lockup_to_oil_w;
    _ = &oil_heat_w;
    next.lockup_temperature_k += ((lockup_heat_w - lockup_to_oil_w) * dt) / config.*.lockup_thermal_capacity_j_k;
    next.oil_temperature_k += ((oil_heat_w - oil_cooling_w) * dt) / config.*.oil_thermal_capacity_j_k;
    next.accumulated_loss_j += ((((next.converter_loss_w + next.lockup_loss_w) + next.gear_clutch_loss_w) + next.hydraulic_loss_w) + next.rotating_loss_w) * dt;
    if ((input.*.requested_gear != PWR_AUTOMATIC_GEAR_NEUTRAL) and ((input.*.line_pressure_command_pa < config.*.minimum_drive_pressure_pa) or ((!next.shift_active and (next.settled_elapsed_s >= config.*.ratio_error_delay_s)) and (next.line_pressure_pa < config.*.minimum_drive_pressure_pa)))) {
        next.diagnostic_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_LOW_LINE_PRESSURE));
    }
    if ((@as(c_int, @intFromBool(next.shift_active)) != 0) and (next.shift_elapsed_s > config.*.maximum_shift_duration_s)) {
        next.diagnostic_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_SHIFT_TIMEOUT));
    }
    if (((!next.shift_active and (next.active_gear != PWR_AUTOMATIC_GEAR_NEUTRAL)) and (next.settled_elapsed_s >= config.*.ratio_error_delay_s)) and (fabs(next.gear_clutch_slip_rad_s) > config.*.ratio_error_threshold_rad_s)) {
        next.diagnostic_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_EXCESSIVE_RATIO_ERROR));
    }
    if (((next.lockup_command_elapsed_s >= config.*.lockup_slip_delay_s) and (next.lockup_engagement >= 0.8)) and (fabs(lockup_slip) > config.*.lockup_slip_threshold_rad_s)) {
        next.diagnostic_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_EXCESSIVE_LOCKUP_SLIP));
    }
    if (next.oil_temperature_k >= config.*.oil_overtemperature_k) {
        next.diagnostic_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_OIL_OVERTEMPERATURE));
    }
    if (next.lockup_temperature_k >= config.*.lockup_overtemperature_k) {
        next.diagnostic_flags |= @as(u32, @bitCast(PWR_AUTOMATIC_DIAG_LOCKUP_OVERTEMPERATURE));
    }
    next.tick +%= 1;
    if (!pwr_automatic_state_finite(&next)) {
        return @as(c_uint, @bitCast(PWR_AUTOMATIC_NUMERIC_ERROR));
    }
    automatic.* = next;
    if (coupling_output != null) {
        coupling_output.* = pwr_automatic_coupling_output{
            .engine_reaction_torque_nm = next.engine_reaction_torque_nm,
            .pump_load_torque_nm = -next.engine_reaction_torque_nm,
            .boundary_power_w = next.pump_boundary_power_w,
            .boundary_energy_j = next.pump_boundary_energy_j,
            .projection_adjustment_j = projection_adjustment_j,
            .accumulated_projection_adjustment_j = next.coupling_adjustment_j,
        };
    }
    return @as(c_uint, @bitCast(PWR_AUTOMATIC_OK));
}

pub fn pwr_automatic_hash_bytes(arg_hash: u64, arg_data: ?*const anyopaque, arg_size: usize) callconv(.c) u64 {
    var hash = arg_hash;
    _ = &hash;
    var data = arg_data;
    _ = &data;
    var size = arg_size;
    _ = &size;
    var bytes: [*c]const u8 = @as([*c]const u8, @ptrCast(@alignCast(data)));
    _ = &bytes;
    {
        var index: usize = 0;
        _ = &index;
        while (index < size) : (index +%= 1) {
            hash ^= @as(u64, @bitCast(@as(u64, bytes[index])));
            hash *%= @as(u64, @bitCast(@as(u64, 1099511628211)));
        }
    }
    return hash;
}

pub fn pwr_automatic_hash_double(arg_hash: u64, arg_value: f64) callconv(.c) u64 {
    var hash = arg_hash;
    _ = &hash;
    var value = arg_value;
    _ = &value;
    var bits: u64 = 0;
    _ = &bits;
    _ = memcpy(@as(?*anyopaque, @ptrCast(&bits)), @as(?*const anyopaque, @ptrCast(&value)), @sizeOf(u64));
    return pwr_automatic_hash_bytes(hash, @as(?*const anyopaque, @ptrCast(&bits)), @sizeOf(u64));
}
