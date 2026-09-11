// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/tests/test_dct_powertrain.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_DCT_CLUTCH_K1 = @import("../src/abi.zig").PWR_DCT_CLUTCH_K1;
const PWR_DCT_CLUTCH_K2 = @import("../src/abi.zig").PWR_DCT_CLUTCH_K2;
const PWR_DCT_GEAR_1 = @import("../src/abi.zig").PWR_DCT_GEAR_1;
const PWR_DCT_GEAR_2 = @import("../src/abi.zig").PWR_DCT_GEAR_2;
const PWR_DCT_GEAR_3 = @import("../src/abi.zig").PWR_DCT_GEAR_3;
const PWR_DCT_GEAR_4 = @import("../src/abi.zig").PWR_DCT_GEAR_4;
const PWR_DCT_GEAR_5 = @import("../src/abi.zig").PWR_DCT_GEAR_5;
const PWR_DCT_GEAR_6 = @import("../src/abi.zig").PWR_DCT_GEAR_6;
const PWR_DCT_GEAR_7 = @import("../src/abi.zig").PWR_DCT_GEAR_7;
const PWR_DCT_GEAR_REVERSE = @import("../src/abi.zig").PWR_DCT_GEAR_REVERSE;
const PWR_DCT_POWERTRAIN_CONTROL_DIRECT = @import("../src/abi.zig").PWR_DCT_POWERTRAIN_CONTROL_DIRECT;
const PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED = @import("../src/abi.zig").PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED;
const PWR_DCT_POWERTRAIN_INVALID_CONFIG = @import("../src/abi.zig").PWR_DCT_POWERTRAIN_INVALID_CONFIG;
const PWR_DCT_POWERTRAIN_INVALID_INPUT = @import("../src/abi.zig").PWR_DCT_POWERTRAIN_INVALID_INPUT;
const PWR_DCT_POWERTRAIN_OK = @import("../src/abi.zig").PWR_DCT_POWERTRAIN_OK;
const PWR_DCT_POWERTRAIN_SUBMODEL_ERROR = @import("../src/abi.zig").PWR_DCT_POWERTRAIN_SUBMODEL_ERROR;
const PWR_DCT_POWERTRAIN_TCU_GEAR_1 = @import("../src/abi.zig").PWR_DCT_POWERTRAIN_TCU_GEAR_1;
const PWR_DCT_POWERTRAIN_TCU_GEAR_2 = @import("../src/abi.zig").PWR_DCT_POWERTRAIN_TCU_GEAR_2;
const PWR_DCT_POWERTRAIN_TCU_LAUNCH_1 = @import("../src/abi.zig").PWR_DCT_POWERTRAIN_TCU_LAUNCH_1;
const PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2 = @import("../src/abi.zig").PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2;
const PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2 = @import("../src/abi.zig").PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2;
const PWR_EXHAUST_VOLUME_MANIFOLD = @import("../src/abi.zig").PWR_EXHAUST_VOLUME_MANIFOLD;
const PWR_SI_ENGINE_FAULT_NONE = @import("../src/abi.zig").PWR_SI_ENGINE_FAULT_NONE;
const __builtin_inff = @import("../src/support.zig").__builtin_inff;
const fabs = @import("../src/support.zig").fabs;
const fmin = @import("../src/support.zig").fmin;
const fprintf = @import("../src/support.zig").fprintf;
const memcmp = @import("../src/support.zig").memcmp;
const power_isfinite = @import("../src/support.zig").power_isfinite;
const pwr_dct_gear_mask = @import("../src/abi.zig").pwr_dct_gear_mask;
const pwr_dct_powertrain_config = @import("../src/abi.zig").pwr_dct_powertrain_config;
const pwr_dct_powertrain_config_default = @import("../src/models/powertrain/pwr_dct_powertrain.zig").pwr_dct_powertrain_config_default;
const pwr_dct_powertrain_control_mode = @import("../src/abi.zig").pwr_dct_powertrain_control_mode;
const pwr_dct_powertrain_init = @import("../src/models/powertrain/pwr_dct_powertrain.zig").pwr_dct_powertrain_init;
const pwr_dct_powertrain_input = @import("../src/abi.zig").pwr_dct_powertrain_input;
const pwr_dct_powertrain_output = @import("../src/abi.zig").pwr_dct_powertrain_output;
const pwr_dct_powertrain_state = @import("../src/abi.zig").pwr_dct_powertrain_state;
const pwr_dct_powertrain_step = @import("../src/models/powertrain/pwr_dct_powertrain.zig").pwr_dct_powertrain_step;
const pwr_dct_powertrain_tcu_phase = @import("../src/abi.zig").pwr_dct_powertrain_tcu_phase;
const pwr_dct_powertrain_validate_config = @import("../src/models/powertrain/pwr_dct_powertrain.zig").pwr_dct_powertrain_validate_config;
const pwr_dct_snapshot = @import("../src/abi.zig").pwr_dct_snapshot;
const pwr_exhaust_diagnostics = @import("../src/abi.zig").pwr_exhaust_diagnostics;
const pwr_exhaust_observation = @import("../src/abi.zig").pwr_exhaust_observation;
const pwr_si_engine_output = @import("../src/abi.zig").pwr_si_engine_output;
const sin = @import("../src/support.zig").sin;

pub fn test_config() callconv(.c) pwr_dct_powertrain_config {
    var config: pwr_dct_powertrain_config = undefined;
    _ = &config;
    pwr_dct_powertrain_config_default(&config);
    config.dct.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_REVERSE)))] = -3.1;
    config.dct.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_1)))] = 3.2;
    config.dct.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_2)))] = 2.05;
    config.dct.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_3)))] = 1.55;
    config.dct.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_4)))] = 1.2;
    config.dct.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_5)))] = 0.96;
    config.dct.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_6)))] = 0.76;
    config.dct.gear_ratio[@as(usize, @bitCast(@as(i64, PWR_DCT_GEAR_7)))] = 0.61;
    config.dct.final_drive_ratio = 1.0;
    config.dct.clutch_nominal_capacity_nm[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K1)))] = 180.0;
    config.dct.clutch_nominal_capacity_nm[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))] = 180.0;
    config.launch_complete_halfshaft_speed_rad_s = 4.0;
    config.first_to_second_shift_halfshaft_speed_rad_s = 13.0;
    config.minimum_first_gear_time_s = 0.12;
    config.first_to_second_shift_duration_s = 0.16;
    return config;
}

pub fn supervised_input(arg_tailpipe_scale: f64) callconv(.c) pwr_dct_powertrain_input {
    var tailpipe_scale = arg_tailpipe_scale;
    _ = &tailpipe_scale;
    return pwr_dct_powertrain_input{
        .control_mode = @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED)),
        .driver_throttle = 0.52,
        .ambient_pressure_pa = 101325.0,
        .ambient_temperature_k = 298.15,
        .additional_road_load_torque_nm = 2.0,
        .tailpipe_area_scale = tailpipe_scale,
        .engine_fault_mask = @as(u32, @bitCast(PWR_SI_ENGINE_FAULT_NONE)),
        .ignition_on = @as(c_int, 1) != 0,
        .selector_drive = @as(c_int, 1) != 0,
        .direct_starter_torque_nm = 0,
        .direct_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
        .direct_clutch_clamp = @import("std").mem.zeroes([2]f64),
    };
}

pub fn test_default_requires_ratios_and_rejection_is_transactional() callconv(.c) c_int {
    var config: pwr_dct_powertrain_config = undefined;
    _ = &config;
    var state: pwr_dct_powertrain_state = undefined;
    _ = &state;
    var before: pwr_dct_powertrain_state = undefined;
    _ = &before;
    var output: pwr_dct_powertrain_output = undefined;
    _ = &output;
    var input: pwr_dct_powertrain_input = supervised_input(1.0);
    _ = &input;
    pwr_dct_powertrain_config_default(&config);
    while (true) {
        if (!(pwr_dct_powertrain_validate_config(&config) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_CONFIG)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 73), "pwr_dct_powertrain_validate_config(&config) == PWR_DCT_POWERTRAIN_INVALID_CONFIG" });
            return 0;
        }
        if (!false) break;
    }
    config = test_config();
    config.engine.base_tick_ns = 1000000;
    while (true) {
        if (!(pwr_dct_powertrain_validate_config(&config) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_CONFIG)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 78), "pwr_dct_powertrain_validate_config(&config) == PWR_DCT_POWERTRAIN_INVALID_CONFIG" });
            return 0;
        }
        if (!false) break;
    }
    config = test_config();
    while (true) {
        if (!(pwr_dct_powertrain_init(&state, &config) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 81), "pwr_dct_powertrain_init(&state, &config) == PWR_DCT_POWERTRAIN_OK" });
            return 0;
        }
        if (!false) break;
    }
    before = state;
    input.driver_throttle = 1.01;
    while (true) {
        if (!(pwr_dct_powertrain_step(&state, &input, &output) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_INVALID_INPUT)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 85), "pwr_dct_powertrain_step(&state, &input, &output) == PWR_DCT_POWERTRAIN_INVALID_INPUT" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(memcmp(@as(?*const anyopaque, @ptrCast(&state)), @as(?*const anyopaque, @ptrCast(&before)), @sizeOf(pwr_dct_powertrain_state)) == @as(c_int, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 86), "memcmp(&state, &before, sizeof(state)) == 0" });
            return 0;
        }
        if (!false) break;
    }
    input = supervised_input(1.0);
    input.control_mode = @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_CONTROL_DIRECT));
    input.direct_starter_torque_nm = 0.0;
    input.direct_gear_mask = @as(pwr_dct_gear_mask, @bitCast(@as(c_short, @truncate(@as(c_int, 2) | @as(c_int, 8)))));
    before = state;
    while (true) {
        if (!(pwr_dct_powertrain_step(&state, &input, &output) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_SUBMODEL_ERROR)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 95), "pwr_dct_powertrain_step(&state, &input, &output) == PWR_DCT_POWERTRAIN_SUBMODEL_ERROR" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(memcmp(@as(?*const anyopaque, @ptrCast(&state)), @as(?*const anyopaque, @ptrCast(&before)), @sizeOf(pwr_dct_powertrain_state)) == @as(c_int, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 96), "memcmp(&state, &before, sizeof(state)) == 0" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

pub fn test_cold_start_launch_and_first_to_second_shift() callconv(.c) c_int {
    const config: pwr_dct_powertrain_config = test_config();
    _ = &config;
    var state: pwr_dct_powertrain_state = undefined;
    _ = &state;
    var output: pwr_dct_powertrain_output = pwr_dct_powertrain_output{
        .time_s = @as(f64, @floatFromInt(@as(c_int, 0))),
        .tick = @import("std").mem.zeroes(u64),
        .control_mode = @import("std").mem.zeroes(pwr_dct_powertrain_control_mode),
        .tcu_phase = @import("std").mem.zeroes(pwr_dct_powertrain_tcu_phase),
        .shift_progress = 0,
        .commanded_engine_throttle = 0,
        .commanded_starter_torque_nm = 0,
        .commanded_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
        .commanded_clutch_clamp = @import("std").mem.zeroes([2]f64),
        .engine = @import("std").mem.zeroes(pwr_si_engine_output),
        .dct = @import("std").mem.zeroes(pwr_dct_snapshot),
        .exhaust = @import("std").mem.zeroes(pwr_exhaust_observation),
        .exhaust_step = @import("std").mem.zeroes(pwr_exhaust_diagnostics),
        .halfshaft_angle_rad = 0,
        .halfshaft_speed_rad_s = 0,
        .halfshaft_drive_torque_nm = 0,
        .road_load_torque_nm = 0,
        .halfshaft_kinetic_energy_j = 0,
        .cumulative_halfshaft_drive_work_j = 0,
        .cumulative_road_load_work_j = 0,
        .cumulative_halfshaft_numerical_adjustment_j = 0,
        .halfshaft_energy_residual_j = 0,
        .exhaust_mass_mismatch_kg = 0,
        .cumulative_exhaust_mass_mismatch_kg = 0,
        .exhaust_enthalpy_mismatch_j = 0,
        .cumulative_exhaust_enthalpy_mismatch_j = 0,
        .engine_dct_energy_mismatch_j = 0,
        .cumulative_engine_dct_energy_mismatch_j = 0,
        .dct_output_energy_mismatch_j = 0,
        .cumulative_dct_output_energy_mismatch_j = 0,
        .last_step_diagnostic_flags = @import("std").mem.zeroes(u32),
        .diagnostic_flags = @import("std").mem.zeroes(u32),
        .state_hash = @import("std").mem.zeroes(u64),
    };
    _ = &output;
    var input: pwr_dct_powertrain_input = supervised_input(1.0);
    _ = &input;
    var saw_starter: bool = @as(c_int, 0) != 0;
    _ = &saw_starter;
    var saw_combustion: bool = @as(c_int, 0) != 0;
    _ = &saw_combustion;
    var saw_preselection: bool = @as(c_int, 0) != 0;
    _ = &saw_preselection;
    var saw_launch: bool = @as(c_int, 0) != 0;
    _ = &saw_launch;
    var saw_first: bool = @as(c_int, 0) != 0;
    _ = &saw_first;
    var saw_shift: bool = @as(c_int, 0) != 0;
    _ = &saw_shift;
    var saw_second: bool = @as(c_int, 0) != 0;
    _ = &saw_second;
    var minimum_shift_drive_torque: f64 = @as(f64, @floatCast(__builtin_inff()));
    _ = &minimum_shift_drive_torque;
    while (true) {
        if (!(pwr_dct_powertrain_init(&state, &config) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 116), "pwr_dct_powertrain_init(&state, &config) == PWR_DCT_POWERTRAIN_OK" });
            return 0;
        }
        if (!false) break;
    }
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, 80000)) : (step +%= 1) {
            while (true) {
                if (!(pwr_dct_powertrain_step(&state, &input, &output) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 119), "pwr_dct_powertrain_step(&state, &input, &output) == PWR_DCT_POWERTRAIN_OK" });
                    return 0;
                }
                if (!false) break;
            }
            saw_starter = (@as(c_int, @intFromBool(saw_starter)) != 0) or (output.commanded_starter_torque_nm > 0.0);
            saw_combustion = (@as(c_int, @intFromBool(saw_combustion)) != 0) or (@as(c_int, @intFromBool(output.engine.combustion_active)) != 0);
            saw_preselection = (@as(c_int, @intFromBool(saw_preselection)) != 0) or (output.tcu_phase == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2)));
            saw_launch = (@as(c_int, @intFromBool(saw_launch)) != 0) or (output.tcu_phase == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_LAUNCH_1)));
            saw_first = (@as(c_int, @intFromBool(saw_first)) != 0) or (output.tcu_phase == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_GEAR_1)));
            if (output.tcu_phase == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2))) {
                saw_shift = @as(c_int, 1) != 0;
                minimum_shift_drive_torque = fmin(minimum_shift_drive_torque, output.halfshaft_drive_torque_nm);
            }
            saw_second = (@as(c_int, @intFromBool(saw_second)) != 0) or (output.tcu_phase == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_GEAR_2)));
        }
    }
    while (true) {
        if (!saw_starter) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 137), "saw_starter" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!saw_combustion) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 138), "saw_combustion" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!saw_preselection) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 139), "saw_preselection" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!saw_launch) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 140), "saw_launch" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!saw_first) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 141), "saw_first" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!saw_shift) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 142), "saw_shift" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!saw_second) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 143), "saw_second" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.tcu_phase == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_TCU_GEAR_2)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 144), "output.tcu_phase == PWR_DCT_POWERTRAIN_TCU_GEAR_2" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.engine.crank_speed_rad_s > 50.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 145), "output.engine.crank_speed_rad_s > 50.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.halfshaft_speed_rad_s > 13.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 146), "output.halfshaft_speed_rad_s > 13.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.halfshaft_angle_rad > 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 147), "output.halfshaft_angle_rad > 0.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.cumulative_halfshaft_drive_work_j > 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 148), "output.cumulative_halfshaft_drive_work_j > 0.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.dct.clutch[@as(usize, @bitCast(@as(i64, PWR_DCT_CLUTCH_K2)))].applied_clamp > 0.9)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 150), "output.dct.clutch[(size_t)PWR_DCT_CLUTCH_K2].applied_clamp > 0.90" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(@as(c_int, @bitCast(@as(c_uint, output.commanded_gear_mask))) == (@as(c_int, 2) | @as(c_int, 4)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 152), "output.commanded_gear_mask == (PWR_DCT_GEAR_MASK_1 | PWR_DCT_GEAR_MASK_2)" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(minimum_shift_drive_torque > -5.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 153), "minimum_shift_drive_torque > -5.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(output.halfshaft_energy_residual_j) < 0.00000001)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 154), "fabs(output.halfshaft_energy_residual_j) < 1.0e-8" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(output.exhaust.pressure_pa[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] > 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 155), "output.exhaust.pressure_pa[PWR_EXHAUST_VOLUME_MANIFOLD] > 0.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(power_isfinite(output.cumulative_engine_dct_energy_mismatch_j) != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 156), "isfinite(output.cumulative_engine_dct_energy_mismatch_j)" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(power_isfinite(output.cumulative_exhaust_mass_mismatch_kg) != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 157), "isfinite(output.cumulative_exhaust_mass_mismatch_kg)" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

pub fn run_restriction_case(arg_tailpipe_scale: f64, arg_final_output: [*c]pwr_dct_powertrain_output, arg_average_brake_power_w: [*c]f64) callconv(.c) c_int {
    var tailpipe_scale = arg_tailpipe_scale;
    _ = &tailpipe_scale;
    var final_output = arg_final_output;
    _ = &final_output;
    var average_brake_power_w = arg_average_brake_power_w;
    _ = &average_brake_power_w;
    const config: pwr_dct_powertrain_config = test_config();
    _ = &config;
    var state: pwr_dct_powertrain_state = undefined;
    _ = &state;
    var input: pwr_dct_powertrain_input = supervised_input(tailpipe_scale);
    _ = &input;
    var output: pwr_dct_powertrain_output = pwr_dct_powertrain_output{
        .time_s = @as(f64, @floatFromInt(@as(c_int, 0))),
        .tick = @import("std").mem.zeroes(u64),
        .control_mode = @import("std").mem.zeroes(pwr_dct_powertrain_control_mode),
        .tcu_phase = @import("std").mem.zeroes(pwr_dct_powertrain_tcu_phase),
        .shift_progress = 0,
        .commanded_engine_throttle = 0,
        .commanded_starter_torque_nm = 0,
        .commanded_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
        .commanded_clutch_clamp = @import("std").mem.zeroes([2]f64),
        .engine = @import("std").mem.zeroes(pwr_si_engine_output),
        .dct = @import("std").mem.zeroes(pwr_dct_snapshot),
        .exhaust = @import("std").mem.zeroes(pwr_exhaust_observation),
        .exhaust_step = @import("std").mem.zeroes(pwr_exhaust_diagnostics),
        .halfshaft_angle_rad = 0,
        .halfshaft_speed_rad_s = 0,
        .halfshaft_drive_torque_nm = 0,
        .road_load_torque_nm = 0,
        .halfshaft_kinetic_energy_j = 0,
        .cumulative_halfshaft_drive_work_j = 0,
        .cumulative_road_load_work_j = 0,
        .cumulative_halfshaft_numerical_adjustment_j = 0,
        .halfshaft_energy_residual_j = 0,
        .exhaust_mass_mismatch_kg = 0,
        .cumulative_exhaust_mass_mismatch_kg = 0,
        .exhaust_enthalpy_mismatch_j = 0,
        .cumulative_exhaust_enthalpy_mismatch_j = 0,
        .engine_dct_energy_mismatch_j = 0,
        .cumulative_engine_dct_energy_mismatch_j = 0,
        .dct_output_energy_mismatch_j = 0,
        .cumulative_dct_output_energy_mismatch_j = 0,
        .last_step_diagnostic_flags = @import("std").mem.zeroes(u32),
        .diagnostic_flags = @import("std").mem.zeroes(u32),
        .state_hash = @import("std").mem.zeroes(u64),
    };
    _ = &output;
    var brake_energy_j: f64 = 0.0;
    _ = &brake_energy_j;
    input.additional_road_load_torque_nm = 8.0;
    while (true) {
        if (!(pwr_dct_powertrain_init(&state, &config) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 174), "pwr_dct_powertrain_init(&state, &config) == PWR_DCT_POWERTRAIN_OK" });
            return 0;
        }
        if (!false) break;
    }
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, 70000)) : (step +%= 1) {
            while (true) {
                if (!(pwr_dct_powertrain_step(&state, &input, &output) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 177), "pwr_dct_powertrain_step(&state, &input, &output) == PWR_DCT_POWERTRAIN_OK" });
                    return 0;
                }
                if (!false) break;
            }
            if (step >= @as(u64, 60000)) {
                brake_energy_j += (output.engine.brake_torque_nm * output.engine.crank_speed_rad_s) * 0.0001;
            }
        }
    }
    average_brake_power_w.* = brake_energy_j;
    final_output.* = output;
    return 1;
}

pub fn test_tailpipe_restriction_feeds_back_to_engine() callconv(.c) c_int {
    var open_output: pwr_dct_powertrain_output = undefined;
    _ = &open_output;
    var restricted_output: pwr_dct_powertrain_output = undefined;
    _ = &restricted_output;
    var open_average_brake_power_w: f64 = undefined;
    _ = &open_average_brake_power_w;
    var restricted_average_brake_power_w: f64 = undefined;
    _ = &restricted_average_brake_power_w;
    while (true) {
        if (!(run_restriction_case(1.0, &open_output, &open_average_brake_power_w) != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 196), "run_restriction_case( 1.0, &open_output, &open_average_brake_power_w)" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(run_restriction_case(0.025, &restricted_output, &restricted_average_brake_power_w) != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 198), "run_restriction_case( 0.025, &restricted_output, &restricted_average_brake_power_w)" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(restricted_output.exhaust.pressure_pa[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] > (open_output.exhaust.pressure_pa[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] + 1000.0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 202), "restricted_output.exhaust.pressure_pa[ PWR_EXHAUST_VOLUME_MANIFOLD] > open_output.exhaust.pressure_pa[PWR_EXHAUST_VOLUME_MANIFOLD] + 1000.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(restricted_output.engine.pumping_torque_nm > (open_output.engine.pumping_torque_nm + 0.5))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 204), "restricted_output.engine.pumping_torque_nm > open_output.engine.pumping_torque_nm + 0.5" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(restricted_average_brake_power_w < open_average_brake_power_w)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 206), "restricted_average_brake_power_w < open_average_brake_power_w" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

pub fn test_deterministic_replay() callconv(.c) c_int {
    const config: pwr_dct_powertrain_config = test_config();
    _ = &config;
    var first: pwr_dct_powertrain_state = undefined;
    _ = &first;
    var second: pwr_dct_powertrain_state = undefined;
    _ = &second;
    var first_output: pwr_dct_powertrain_output = pwr_dct_powertrain_output{
        .time_s = @as(f64, @floatFromInt(@as(c_int, 0))),
        .tick = @import("std").mem.zeroes(u64),
        .control_mode = @import("std").mem.zeroes(pwr_dct_powertrain_control_mode),
        .tcu_phase = @import("std").mem.zeroes(pwr_dct_powertrain_tcu_phase),
        .shift_progress = 0,
        .commanded_engine_throttle = 0,
        .commanded_starter_torque_nm = 0,
        .commanded_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
        .commanded_clutch_clamp = @import("std").mem.zeroes([2]f64),
        .engine = @import("std").mem.zeroes(pwr_si_engine_output),
        .dct = @import("std").mem.zeroes(pwr_dct_snapshot),
        .exhaust = @import("std").mem.zeroes(pwr_exhaust_observation),
        .exhaust_step = @import("std").mem.zeroes(pwr_exhaust_diagnostics),
        .halfshaft_angle_rad = 0,
        .halfshaft_speed_rad_s = 0,
        .halfshaft_drive_torque_nm = 0,
        .road_load_torque_nm = 0,
        .halfshaft_kinetic_energy_j = 0,
        .cumulative_halfshaft_drive_work_j = 0,
        .cumulative_road_load_work_j = 0,
        .cumulative_halfshaft_numerical_adjustment_j = 0,
        .halfshaft_energy_residual_j = 0,
        .exhaust_mass_mismatch_kg = 0,
        .cumulative_exhaust_mass_mismatch_kg = 0,
        .exhaust_enthalpy_mismatch_j = 0,
        .cumulative_exhaust_enthalpy_mismatch_j = 0,
        .engine_dct_energy_mismatch_j = 0,
        .cumulative_engine_dct_energy_mismatch_j = 0,
        .dct_output_energy_mismatch_j = 0,
        .cumulative_dct_output_energy_mismatch_j = 0,
        .last_step_diagnostic_flags = @import("std").mem.zeroes(u32),
        .diagnostic_flags = @import("std").mem.zeroes(u32),
        .state_hash = @import("std").mem.zeroes(u64),
    };
    _ = &first_output;
    var second_output: pwr_dct_powertrain_output = pwr_dct_powertrain_output{
        .time_s = @as(f64, @floatFromInt(@as(c_int, 0))),
        .tick = @import("std").mem.zeroes(u64),
        .control_mode = @import("std").mem.zeroes(pwr_dct_powertrain_control_mode),
        .tcu_phase = @import("std").mem.zeroes(pwr_dct_powertrain_tcu_phase),
        .shift_progress = 0,
        .commanded_engine_throttle = 0,
        .commanded_starter_torque_nm = 0,
        .commanded_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
        .commanded_clutch_clamp = @import("std").mem.zeroes([2]f64),
        .engine = @import("std").mem.zeroes(pwr_si_engine_output),
        .dct = @import("std").mem.zeroes(pwr_dct_snapshot),
        .exhaust = @import("std").mem.zeroes(pwr_exhaust_observation),
        .exhaust_step = @import("std").mem.zeroes(pwr_exhaust_diagnostics),
        .halfshaft_angle_rad = 0,
        .halfshaft_speed_rad_s = 0,
        .halfshaft_drive_torque_nm = 0,
        .road_load_torque_nm = 0,
        .halfshaft_kinetic_energy_j = 0,
        .cumulative_halfshaft_drive_work_j = 0,
        .cumulative_road_load_work_j = 0,
        .cumulative_halfshaft_numerical_adjustment_j = 0,
        .halfshaft_energy_residual_j = 0,
        .exhaust_mass_mismatch_kg = 0,
        .cumulative_exhaust_mass_mismatch_kg = 0,
        .exhaust_enthalpy_mismatch_j = 0,
        .cumulative_exhaust_enthalpy_mismatch_j = 0,
        .engine_dct_energy_mismatch_j = 0,
        .cumulative_engine_dct_energy_mismatch_j = 0,
        .dct_output_energy_mismatch_j = 0,
        .cumulative_dct_output_energy_mismatch_j = 0,
        .last_step_diagnostic_flags = @import("std").mem.zeroes(u32),
        .diagnostic_flags = @import("std").mem.zeroes(u32),
        .state_hash = @import("std").mem.zeroes(u64),
    };
    _ = &second_output;
    var input: pwr_dct_powertrain_input = supervised_input(1.0);
    _ = &input;
    while (true) {
        if (!(pwr_dct_powertrain_init(&first, &config) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 220), "pwr_dct_powertrain_init(&first, &config) == PWR_DCT_POWERTRAIN_OK" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_dct_powertrain_init(&second, &config) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 222), "pwr_dct_powertrain_init(&second, &config) == PWR_DCT_POWERTRAIN_OK" });
            return 0;
        }
        if (!false) break;
    }
    {
        var step: u64 = 0;
        _ = &step;
        while (step < @as(u64, 30000)) : (step +%= 1) {
            input.driver_throttle = 0.45 + (0.08 * sin(@as(f64, @floatFromInt(step)) * 0.0007));
            while (true) {
                if (!(pwr_dct_powertrain_step(&first, &input, &first_output) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 227), "pwr_dct_powertrain_step(&first, &input, &first_output) == PWR_DCT_POWERTRAIN_OK" });
                    return 0;
                }
                if (!false) break;
            }
            while (true) {
                if (!(pwr_dct_powertrain_step(&second, &input, &second_output) == @as(c_uint, @bitCast(PWR_DCT_POWERTRAIN_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 229), "pwr_dct_powertrain_step(&second, &input, &second_output) == PWR_DCT_POWERTRAIN_OK" });
                    return 0;
                }
                if (!false) break;
            }
            while (true) {
                if (!(first_output.state_hash == second_output.state_hash)) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 230), "first_output.state_hash == second_output.state_hash" });
                    return 0;
                }
                if (!false) break;
            }
        }
    }
    while (true) {
        if (!(memcmp(@as(?*const anyopaque, @ptrCast(&first)), @as(?*const anyopaque, @ptrCast(&second)), @sizeOf(pwr_dct_powertrain_state)) == @as(c_int, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 232), "memcmp(&first, &second, sizeof(first)) == 0" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

pub fn run() c_int {
    while (true) {
        if (!(test_default_requires_ratios_and_rejection_is_transactional() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 238), "test_default_requires_ratios_and_rejection_is_transactional()" });
            return 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(test_cold_start_launch_and_first_to_second_shift() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 239), "test_cold_start_launch_and_first_to_second_shift()" });
            return 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(test_tailpipe_restriction_feeds_back_to_engine() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 240), "test_tailpipe_restriction_feeds_back_to_engine()" });
            return 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(test_deterministic_replay() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_dct_powertrain.c", @as(c_int, 241), "test_deterministic_replay()" });
            return 1;
        }
        if (!false) break;
    }
    return 0;
}

pub fn main() void {
    @import("std").process.exit(@intCast(run()));
}
