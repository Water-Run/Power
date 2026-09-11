// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/core/pwr_graph.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_CHANNEL_INPUT = @import("../abi.zig").PWR_CHANNEL_INPUT;
const PWR_CHANNEL_OUTPUT = @import("../abi.zig").PWR_CHANNEL_OUTPUT;
const PWR_COMPONENT_DC_MOTOR = @import("../abi.zig").PWR_COMPONENT_DC_MOTOR;
const PWR_COMPONENT_SHAFT = @import("../abi.zig").PWR_COMPONENT_SHAFT;
const PWR_COMPONENT_THERMAL_LINK = @import("../abi.zig").PWR_COMPONENT_THERMAL_LINK;
const PWR_COMPONENT_TORQUE_SOURCE = @import("../abi.zig").PWR_COMPONENT_TORQUE_SOURCE;
const PWR_FIELD_ANGLE = @import("../abi.zig").PWR_FIELD_ANGLE;
const PWR_FIELD_CURRENT = @import("../abi.zig").PWR_FIELD_CURRENT;
const PWR_FIELD_ENERGY_RESIDUAL = @import("../abi.zig").PWR_FIELD_ENERGY_RESIDUAL;
const PWR_FIELD_HEAT_REJECTED = @import("../abi.zig").PWR_FIELD_HEAT_REJECTED;
const PWR_FIELD_SOURCE_WORK = @import("../abi.zig").PWR_FIELD_SOURCE_WORK;
const PWR_FIELD_SPEED = @import("../abi.zig").PWR_FIELD_SPEED;
const PWR_FIELD_STORED_ENERGY_CHANGE = @import("../abi.zig").PWR_FIELD_STORED_ENERGY_CHANGE;
const PWR_FIELD_TEMPERATURE = @import("../abi.zig").PWR_FIELD_TEMPERATURE;
const PWR_FIELD_TORQUE = @import("../abi.zig").PWR_FIELD_TORQUE;
const PWR_FIELD_TWIST = @import("../abi.zig").PWR_FIELD_TWIST;
const PWR_MODEL_DIAG_CAPACITY = @import("../abi.zig").PWR_MODEL_DIAG_CAPACITY;
const PWR_MODEL_DIAG_CHANNEL = @import("../abi.zig").PWR_MODEL_DIAG_CHANNEL;
const PWR_MODEL_DIAG_CONNECTION = @import("../abi.zig").PWR_MODEL_DIAG_CONNECTION;
const PWR_MODEL_DIAG_ID = @import("../abi.zig").PWR_MODEL_DIAG_ID;
const PWR_MODEL_DIAG_RANGE = @import("../abi.zig").PWR_MODEL_DIAG_RANGE;
const PWR_MODEL_DIAG_SCHEMA = @import("../abi.zig").PWR_MODEL_DIAG_SCHEMA;
const PWR_MODEL_DIAG_SOLVER = @import("../abi.zig").PWR_MODEL_DIAG_SOLVER;
const PWR_MODEL_DIAG_UNIT = @import("../abi.zig").PWR_MODEL_DIAG_UNIT;
const PWR_NODE_ROTATIONAL = @import("../abi.zig").PWR_NODE_ROTATIONAL;
const PWR_NODE_THERMAL = @import("../abi.zig").PWR_NODE_THERMAL;
const PWR_STATUS_CAPACITY_EXCEEDED = @import("../abi.zig").PWR_STATUS_CAPACITY_EXCEEDED;
const PWR_STATUS_INVALID_ARGUMENT = @import("../abi.zig").PWR_STATUS_INVALID_ARGUMENT;
const PWR_STATUS_INVALID_MODEL = @import("../abi.zig").PWR_STATUS_INVALID_MODEL;
const PWR_STATUS_INVALID_TIME_STEP = @import("../abi.zig").PWR_STATUS_INVALID_TIME_STEP;
const PWR_STATUS_NUMERIC_ERROR = @import("../abi.zig").PWR_STATUS_NUMERIC_ERROR;
const PWR_STATUS_OK = @import("../abi.zig").PWR_STATUS_OK;
const PWR_STATUS_STRUCT_TOO_SMALL = @import("../abi.zig").PWR_STATUS_STRUCT_TOO_SMALL;
const PWR_STATUS_UNKNOWN_CHANNEL = @import("../abi.zig").PWR_STATUS_UNKNOWN_CHANNEL;
const PWR_STATUS_UNSUPPORTED = @import("../abi.zig").PWR_STATUS_UNSUPPORTED;
const PWR_STATUS_UNSUPPORTED_ABI = @import("../abi.zig").PWR_STATUS_UNSUPPORTED_ABI;
const PWR_UNIT_A = @import("../abi.zig").PWR_UNIT_A;
const PWR_UNIT_DEG = @import("../abi.zig").PWR_UNIT_DEG;
const PWR_UNIT_H = @import("../abi.zig").PWR_UNIT_H;
const PWR_UNIT_J = @import("../abi.zig").PWR_UNIT_J;
const PWR_UNIT_J_K = @import("../abi.zig").PWR_UNIT_J_K;
const PWR_UNIT_K = @import("../abi.zig").PWR_UNIT_K;
const PWR_UNIT_KG_M2 = @import("../abi.zig").PWR_UNIT_KG_M2;
const PWR_UNIT_NM = @import("../abi.zig").PWR_UNIT_NM;
const PWR_UNIT_NM_A = @import("../abi.zig").PWR_UNIT_NM_A;
const PWR_UNIT_NM_RAD = @import("../abi.zig").PWR_UNIT_NM_RAD;
const PWR_UNIT_NM_S_RAD = @import("../abi.zig").PWR_UNIT_NM_S_RAD;
const PWR_UNIT_NONE = @import("../abi.zig").PWR_UNIT_NONE;
const PWR_UNIT_OHM = @import("../abi.zig").PWR_UNIT_OHM;
const PWR_UNIT_RAD = @import("../abi.zig").PWR_UNIT_RAD;
const PWR_UNIT_RAD_S = @import("../abi.zig").PWR_UNIT_RAD_S;
const PWR_UNIT_RPM = @import("../abi.zig").PWR_UNIT_RPM;
const PWR_UNIT_V = @import("../abi.zig").PWR_UNIT_V;
const PWR_UNIT_W_K = @import("../abi.zig").PWR_UNIT_W_K;
const fabs = @import("../support.zig").fabs;
const fmax = @import("../support.zig").fmax;
const memcpy = @import("../support.zig").memcpy;
const memset = @import("../support.zig").memset;
const power_isfinite = @import("../support.zig").power_isfinite;
const pwr_channel_id = @import("../abi.zig").pwr_channel_id;
const pwr_channel_info = @import("../abi.zig").pwr_channel_info;
const pwr_component_desc = @import("../abi.zig").pwr_component_desc;
const pwr_graph = @import("../abi.zig").pwr_graph;
const pwr_graph_component = @import("../abi.zig").pwr_graph_component;
const pwr_graph_matrix = @import("../abi.zig").pwr_graph_matrix;
const pwr_graph_node = @import("../abi.zig").pwr_graph_node;
const pwr_graph_state = @import("../abi.zig").pwr_graph_state;
const pwr_input_frame = @import("../abi.zig").pwr_input_frame;
const pwr_model_desc = @import("../abi.zig").pwr_model_desc;
const pwr_model_diagnostic = @import("../abi.zig").pwr_model_diagnostic;
const pwr_node_desc = @import("../abi.zig").pwr_node_desc;
const pwr_quantity = @import("../abi.zig").pwr_quantity;
const pwr_scalar_value = @import("../abi.zig").pwr_scalar_value;
const pwr_snapshot = @import("../abi.zig").pwr_snapshot;
const pwr_status = @import("../abi.zig").pwr_status;
const snprintf = @import("../support.zig").snprintf;

pub fn pwr_graph_compile(arg_desc: [*c]const pwr_model_desc, arg_g: [*c]pwr_graph, arg_d: [*c]pwr_model_diagnostic) callconv(.c) pwr_status {
    var desc = arg_desc;
    _ = &desc;
    var g = arg_g;
    _ = &g;
    var d = arg_d;
    _ = &d;
    if (d != null) {
        if (d.*.abi_version != @as(c_uint, 1)) {
            return PWR_STATUS_UNSUPPORTED_ABI;
        }
        if (@as(u64, @bitCast(@as(u64, d.*.struct_size))) < @sizeOf(pwr_model_diagnostic)) {
            return PWR_STATUS_STRUCT_TOO_SMALL;
        }
        d.* = pwr_model_diagnostic{
            .abi_version = @as(c_uint, 1),
            .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_diagnostic))))),
            .code = @as(u32, @bitCast(@as(c_int, 0))),
            .object_id = @as(u32, @bitCast(@as(c_int, 0))),
            .field = [1]u8{
                0,
            } ++ [1]u8{0} ** 39,
            .message = [1]u8{
                0,
            } ++ [1]u8{0} ** 159,
        };
    }
    if ((desc == null) or (g == null)) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (desc.*.abi_version != @as(c_uint, 1)) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if (@as(u64, @bitCast(@as(u64, desc.*.struct_size))) < @sizeOf(pwr_model_desc)) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    if ((desc.*.schema_version != @as(c_uint, 1)) or (desc.*.flags != @as(c_uint, 0))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_SCHEMA)), @as(u32, @bitCast(@as(c_int, 0))), "schema_version/flags", "Unsupported model schema or flags.", PWR_STATUS_UNSUPPORTED);
    }
    if ((desc.*.node_count > @as(c_uint, 32)) or (desc.*.component_count > @as(c_uint, 64))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_CAPACITY)), @as(u32, @bitCast(@as(c_int, 0))), "counts", "Model exceeds compiler node/component capacity.", PWR_STATUS_CAPACITY_EXCEEDED);
    }
    if (((desc.*.node_count == @as(c_uint, 0)) or (desc.*.nodes == null)) or ((desc.*.component_count != @as(c_uint, 0)) and (desc.*.components == null))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_SCHEMA)), @as(u32, @bitCast(@as(c_int, 0))), "nodes/components", "At least one storage node and valid descriptor arrays are required.", PWR_STATUS_INVALID_ARGUMENT);
    }
    if ((desc.*.step_ns == @as(u64, @bitCast(@as(u64, @as(c_uint, 0))))) or (desc.*.step_ns > @as(u64, 1000000000))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_RANGE)), @as(u32, @bitCast(@as(c_int, 0))), "step_ns", "Fixed step must be between one nanosecond and one second.", PWR_STATUS_INVALID_TIME_STEP);
    }
    _ = memset(@as(?*anyopaque, @ptrCast(g)), @as(c_int, 0), @sizeOf(pwr_graph));
    g.*.step_ns = desc.*.step_ns;
    g.*.dt = @as(f64, @floatFromInt(desc.*.step_ns)) * 0.000000001;
    g.*.node_count = desc.*.node_count;
    g.*.component_count = desc.*.component_count;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.node_count) : (i +%= 1) {
            const status: pwr_status = pwr_graph_node_compile(&desc.*.nodes[i], &g.*.nodes[i], d);
            _ = &status;
            if (status != PWR_STATUS_OK) {
                return status;
            }
            {
                var j: u32 = i;
                _ = &j;
                while ((j > @as(c_uint, 0)) and (g.*.nodes[j].id < g.*.nodes[j -% @as(c_uint, 1)].id)) : (j -%= 1) {
                    const tmp: pwr_graph_node = g.*.nodes[j];
                    _ = &tmp;
                    g.*.nodes[j] = g.*.nodes[j -% @as(c_uint, 1)];
                    g.*.nodes[j -% @as(c_uint, 1)] = tmp;
                }
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.node_count) : (i +%= 1) {
            var n: [*c]pwr_graph_node = &g.*.nodes[i];
            _ = &n;
            if ((i > @as(c_uint, 0)) and (n.*.id == g.*.nodes[i -% @as(c_uint, 1)].id)) {
                return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_ID)), n.*.id, "id", "Duplicate node ID.", PWR_STATUS_INVALID_MODEL);
            }
            if (n.*.domain == @as(u32, @bitCast(PWR_NODE_ROTATIONAL))) {
                n.*.state_index = g.*.dynamic_count;
                g.*.dynamic_count +%= @as(u32, @bitCast(@as(c_uint, 2)));
            } else {
                n.*.state_index = blk: {
                    const ref = &g.*.thermal_count;
                    const tmp = ref.*;
                    ref.* +%= 1;
                    break :blk tmp;
                };
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            const status: pwr_status = pwr_graph_component_compile(g, &desc.*.components[i], &g.*.components[i], d);
            _ = &status;
            if (status != PWR_STATUS_OK) {
                return status;
            }
            {
                var j: u32 = i;
                _ = &j;
                while ((j > @as(c_uint, 0)) and (g.*.components[j].id < g.*.components[j -% @as(c_uint, 1)].id)) : (j -%= 1) {
                    const tmp: pwr_graph_component = g.*.components[j];
                    _ = &tmp;
                    g.*.components[j] = g.*.components[j -% @as(c_uint, 1)];
                    g.*.components[j -% @as(c_uint, 1)] = tmp;
                }
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            var c: [*c]pwr_graph_component = &g.*.components[i];
            _ = &c;
            if ((i > @as(c_uint, 0)) and (c.*.id == g.*.components[i -% @as(c_uint, 1)].id)) {
                return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_ID)), c.*.id, "id", "Duplicate component ID.", PWR_STATUS_INVALID_MODEL);
            }
            {
                var j: u32 = 0;
                _ = &j;
                while (j < i) : (j +%= 1) {
                    if ((c.*.input_channel != @as(u64, @bitCast(@as(u64, @as(c_uint, 0))))) and (c.*.input_channel == g.*.components[j].input_channel)) {
                        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_CHANNEL)), c.*.id, "input_channel", "Duplicate input channel.", PWR_STATUS_INVALID_MODEL);
                    }
                }
            }
            if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR))) {
                c.*.state_index = blk: {
                    const ref = &g.*.dynamic_count;
                    const tmp = ref.*;
                    ref.* +%= 1;
                    break :blk tmp;
                };
            }
        }
    }
    if ((g.*.dynamic_count +% g.*.thermal_count) > @as(c_uint, 64)) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_CAPACITY)), @as(u32, @bitCast(@as(c_int, 0))), "states", "Model exceeds compiler state capacity.", PWR_STATUS_CAPACITY_EXCEEDED);
    }
    pwr_graph_assemble(g);
    var initial: pwr_graph_state = undefined;
    _ = &initial;
    if ((!pwr_graph_factor(&g.*.dynamics) or !pwr_graph_factor(&g.*.thermal)) or (pwr_graph_init(g, &initial) != PWR_STATUS_OK)) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_SOLVER)), @as(u32, @bitCast(@as(c_int, 0))), "solver", "Nonfinite initial state or singular/ill-conditioned discrete system.", PWR_STATUS_NUMERIC_ERROR);
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.dynamic_count) : (i +%= 1) {
            if (!(power_isfinite(g.*.constant_force[i]) != 0)) {
                return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_SOLVER)), @as(u32, @bitCast(@as(c_int, 0))), "constant_force", "Parameter scaling overflows the compiled forcing vector.", PWR_STATUS_NUMERIC_ERROR);
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.thermal_count) : (i +%= 1) {
            if (!(power_isfinite(g.*.ambient_force[i]) != 0)) {
                return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_SOLVER)), @as(u32, @bitCast(@as(c_int, 0))), "ambient_force", "Parameter scaling overflows the thermal boundary vector.", PWR_STATUS_NUMERIC_ERROR);
            }
        }
    }
    g.*.fingerprint = pwr_graph_fingerprint(g);
    return PWR_STATUS_OK;
}

pub fn pwr_graph_init(arg_g: [*c]const pwr_graph, arg_s: [*c]pwr_graph_state) callconv(.c) pwr_status {
    var g = arg_g;
    _ = &g;
    var s = arg_s;
    _ = &s;
    if ((g == null) or (s == null)) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    s.* = pwr_graph_state{
        .time_ns = @as(u64, @bitCast(@as(i64, @as(c_int, 0)))),
        .x = @import("std").mem.zeroes([64]f64),
        .temperature = @import("std").mem.zeroes([32]f64),
        .inputs = @import("std").mem.zeroes([64]f64),
        .initial_energy = 0,
        .source_work = 0,
        .source_work_compensation = 0,
        .heat_rejected = 0,
        .heat_compensation = 0,
    };
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.node_count) : (i +%= 1) {
            var n: [*c]const pwr_graph_node = &g.*.nodes[i];
            _ = &n;
            if (n.*.domain == @as(u32, @bitCast(PWR_NODE_ROTATIONAL))) {
                s.*.x[n.*.state_index] = n.*.position;
                s.*.x[n.*.state_index +% @as(c_uint, 1)] = n.*.initial;
            } else {
                s.*.temperature[n.*.state_index] = n.*.initial;
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            var c: [*c]const pwr_graph_component = &g.*.components[i];
            _ = &c;
            s.*.inputs[i] = c.*.initial_input;
            if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR))) {
                s.*.x[c.*.state_index] = c.*.p[@as(c_uint, @intCast(@as(c_int, 3)))];
            }
        }
    }
    s.*.initial_energy = pwr_graph_energy(g, s);
    return if ((power_isfinite(s.*.initial_energy) != 0) and (@as(c_int, @intFromBool(pwr_graph_observables_finite(g, s))) != 0)) PWR_STATUS_OK else PWR_STATUS_NUMERIC_ERROR;
}

pub fn pwr_graph_submit(arg_g: [*c]const pwr_graph, arg_s: [*c]pwr_graph_state, arg_frame: [*c]const pwr_input_frame) callconv(.c) pwr_status {
    var g = arg_g;
    _ = &g;
    var s = arg_s;
    _ = &s;
    var frame = arg_frame;
    _ = &frame;
    if (((g == null) or (s == null)) or (frame == null)) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (frame.*.abi_version != @as(c_uint, 1)) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if (@as(u64, @bitCast(@as(u64, frame.*.struct_size))) < @sizeOf(pwr_input_frame)) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    if ((frame.*.flags != @as(c_uint, 0)) or ((frame.*.value_count != @as(c_uint, 0)) and (frame.*.values == null))) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (frame.*.value_count > g.*.component_count) {
        return PWR_STATUS_CAPACITY_EXCEEDED;
    }
    var candidate: [64]f64 = undefined;
    _ = &candidate;
    var seen: [64]bool = [1]bool{
        @as(c_int, 0) != 0,
    } ++ [1]bool{false} ** 63;
    _ = &seen;
    _ = memcpy(@as(?*anyopaque, @ptrCast(@as([*c]f64, @ptrCast(@alignCast(&candidate[@as(usize, @intCast(0))]))))), @as(?*const anyopaque, @ptrCast(@as([*c]f64, @ptrCast(@alignCast(&s.*.inputs[@as(usize, @intCast(0))]))))), @sizeOf([64]f64));
    {
        var i: u32 = 0;
        _ = &i;
        while (i < frame.*.value_count) : (i +%= 1) {
            var v: [*c]const pwr_scalar_value = &frame.*.values[i];
            _ = &v;
            if (!(power_isfinite(v.*.value) != 0)) {
                return PWR_STATUS_INVALID_ARGUMENT;
            }
            if ((v.*.flags != @as(c_uint, 0)) or (v.*.reserved != @as(c_uint, 0))) {
                return PWR_STATUS_UNSUPPORTED;
            }
            var index: u32 = @as(c_uint, 4294967295);
            _ = &index;
            {
                var j: u32 = 0;
                _ = &j;
                while (j < g.*.component_count) : (j +%= 1) {
                    if ((v.*.channel != @as(pwr_channel_id, @bitCast(@as(u64, @as(c_uint, 0))))) and (g.*.components[j].input_channel == v.*.channel)) {
                        index = j;
                        break;
                    }
                }
            }
            if (index == @as(c_uint, 4294967295)) {
                return PWR_STATUS_UNKNOWN_CHANNEL;
            }
            if (seen[index]) {
                return PWR_STATUS_INVALID_ARGUMENT;
            }
            seen[index] = @as(c_int, 1) != 0;
            candidate[index] = if (v.*.value == 0.0) 0.0 else v.*.value;
        }
    }
    _ = memcpy(@as(?*anyopaque, @ptrCast(@as([*c]f64, @ptrCast(@alignCast(&s.*.inputs[@as(usize, @intCast(0))]))))), @as(?*const anyopaque, @ptrCast(@as([*c]f64, @ptrCast(@alignCast(&candidate[@as(usize, @intCast(0))]))))), @sizeOf([64]f64));
    return PWR_STATUS_OK;
}

pub fn pwr_graph_step(arg_g: [*c]const pwr_graph, arg_s: [*c]pwr_graph_state, arg_delta_ns: u64) callconv(.c) pwr_status {
    var g = arg_g;
    _ = &g;
    var s = arg_s;
    _ = &s;
    var delta_ns = arg_delta_ns;
    _ = &delta_ns;
    if ((g == null) or (s == null)) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if ((((delta_ns == @as(u64, @bitCast(@as(u64, @as(c_uint, 0))))) or ((delta_ns % g.*.step_ns) != @as(u64, @bitCast(@as(u64, @as(c_uint, 0)))))) or ((delta_ns / g.*.step_ns) > @as(u64, 1000000))) or ((@as(u64, 18446744073709551615) -% s.*.time_ns) < delta_ns)) {
        return PWR_STATUS_INVALID_TIME_STEP;
    }
    var candidate: pwr_graph_state = s.*;
    _ = &candidate;
    const count: u64 = delta_ns / g.*.step_ns;
    _ = &count;
    {
        var i: u64 = 0;
        _ = &i;
        while (i < count) : (i +%= 1) {
            const status: pwr_status = pwr_graph_tick(g, &candidate);
            _ = &status;
            if (status != PWR_STATUS_OK) {
                return status;
            }
        }
    }
    s.* = candidate;
    return PWR_STATUS_OK;
}

pub fn pwr_graph_snapshot(arg_g: [*c]const pwr_graph, arg_s: [*c]const pwr_graph_state, arg_snapshot: [*c]pwr_snapshot) callconv(.c) pwr_status {
    var g = arg_g;
    _ = &g;
    var s = arg_s;
    _ = &s;
    var snapshot = arg_snapshot;
    _ = &snapshot;
    if (((g == null) or (s == null)) or (snapshot == null)) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (snapshot.*.abi_version != @as(c_uint, 1)) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if (@as(u64, @bitCast(@as(u64, snapshot.*.struct_size))) < @sizeOf(pwr_snapshot)) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    snapshot.*.simulation_time_ns = s.*.time_ns;
    snapshot.*.state_hash = pwr_graph_state_hash(g, s);
    snapshot.*.diagnostic_bits = 0;
    snapshot.*.value_count = g.*.output_count;
    if ((snapshot.*.values == null) or (snapshot.*.value_capacity < g.*.output_count)) {
        return PWR_STATUS_CAPACITY_EXCEEDED;
    }
    var index: u32 = 0;
    _ = &index;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.node_count) : (i +%= 1) {
            var n: [*c]const pwr_graph_node = &g.*.nodes[i];
            _ = &n;
            if (n.*.domain == @as(u32, @bitCast(PWR_NODE_ROTATIONAL))) {
                pwr_graph_write(snapshot, &index, n.*.id, @as(u32, @bitCast(PWR_FIELD_ANGLE)), s.*.x[n.*.state_index]);
                pwr_graph_write(snapshot, &index, n.*.id, @as(u32, @bitCast(PWR_FIELD_SPEED)), s.*.x[n.*.state_index +% @as(c_uint, 1)]);
            } else {
                pwr_graph_write(snapshot, &index, n.*.id, @as(u32, @bitCast(PWR_FIELD_TEMPERATURE)), s.*.temperature[n.*.state_index]);
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            var c: [*c]const pwr_graph_component = &g.*.components[i];
            _ = &c;
            if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_SHAFT))) {
                const twist: f64 = pwr_graph_relative(g, c, @as([*c]const f64, @ptrCast(@alignCast(&s.*.x[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 0))));
                _ = &twist;
                const torque: f64 = (-c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))] * twist) - (c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))] * pwr_graph_relative(g, c, @as([*c]const f64, @ptrCast(@alignCast(&s.*.x[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 1)))));
                _ = &torque;
                pwr_graph_write(snapshot, &index, c.*.id, @as(u32, @bitCast(PWR_FIELD_TWIST)), twist);
                pwr_graph_write(snapshot, &index, c.*.id, @as(u32, @bitCast(PWR_FIELD_TORQUE)), torque);
            } else if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR))) {
                const current: f64 = s.*.x[c.*.state_index];
                _ = &current;
                pwr_graph_write(snapshot, &index, c.*.id, @as(u32, @bitCast(PWR_FIELD_CURRENT)), current);
                pwr_graph_write(snapshot, &index, c.*.id, @as(u32, @bitCast(PWR_FIELD_TORQUE)), c.*.p[@as(c_uint, @intCast(@as(c_int, 2)))] * current);
            }
        }
    }
    const stored: f64 = pwr_graph_energy(g, s) - s.*.initial_energy;
    _ = &stored;
    pwr_graph_write(snapshot, &index, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_SOURCE_WORK)), s.*.source_work);
    pwr_graph_write(snapshot, &index, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_HEAT_REJECTED)), s.*.heat_rejected);
    pwr_graph_write(snapshot, &index, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_STORED_ENERGY_CHANGE)), stored);
    pwr_graph_write(snapshot, &index, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_ENERGY_RESIDUAL)), (s.*.source_work - s.*.heat_rejected) - stored);
    return PWR_STATUS_OK;
}

pub fn pwr_graph_state_hash(arg_g: [*c]const pwr_graph, arg_s: [*c]const pwr_graph_state) callconv(.c) u64 {
    var g = arg_g;
    _ = &g;
    var s = arg_s;
    _ = &s;
    var hash: u64 = pwr_graph_hash_word(g.*.fingerprint, s.*.time_ns);
    _ = &hash;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.dynamic_count) : (i +%= 1) {
            hash = pwr_graph_hash_double(hash, s.*.x[i]);
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.thermal_count) : (i +%= 1) {
            hash = pwr_graph_hash_double(hash, s.*.temperature[i]);
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            hash = pwr_graph_hash_double(hash, s.*.inputs[i]);
        }
    }
    hash = pwr_graph_hash_double(hash, s.*.initial_energy);
    hash = pwr_graph_hash_double(hash, s.*.source_work);
    hash = pwr_graph_hash_double(hash, s.*.source_work_compensation);
    hash = pwr_graph_hash_double(hash, s.*.heat_rejected);
    return pwr_graph_hash_double(hash, s.*.heat_compensation);
}

pub fn pwr_graph_error(arg_d: [*c]pwr_model_diagnostic, arg_code: u32, arg_id: u32, arg_field: [*c]const u8, arg_message: [*c]const u8, arg_status: pwr_status) callconv(.c) pwr_status {
    var d = arg_d;
    _ = &d;
    var code = arg_code;
    _ = &code;
    var id = arg_id;
    _ = &id;
    var field = arg_field;
    _ = &field;
    var message = arg_message;
    _ = &message;
    var status = arg_status;
    _ = &status;
    if (d != null) {
        d.*.code = code;
        d.*.object_id = id;
        _ = snprintf(@as([*c]u8, @ptrCast(@alignCast(&d.*.field[@as(usize, @intCast(0))]))), @sizeOf([40]u8), "%s", field);
        _ = snprintf(@as([*c]u8, @ptrCast(@alignCast(&d.*.message[@as(usize, @intCast(0))]))), @sizeOf([160]u8), "%s", message);
    }
    return status;
}

pub fn pwr_graph_quantity(arg_q: pwr_quantity, arg_unit: u32, arg_out: [*c]f64, arg_d: [*c]pwr_model_diagnostic, arg_id: u32, arg_field: [*c]const u8) callconv(.c) pwr_status {
    var q = arg_q;
    _ = &q;
    var unit = arg_unit;
    _ = &unit;
    var out = arg_out;
    _ = &out;
    var d = arg_d;
    _ = &d;
    var id = arg_id;
    _ = &id;
    var field = arg_field;
    _ = &field;
    var scale: f64 = 1.0;
    _ = &scale;
    if (q.reserved != @as(c_uint, 0)) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_SCHEMA)), id, field, "Reserved quantity field must be zero.", PWR_STATUS_UNSUPPORTED);
    }
    if (q.unit != unit) {
        if ((unit == @as(u32, @bitCast(PWR_UNIT_RAD_S))) and (q.unit == @as(u32, @bitCast(PWR_UNIT_RPM)))) {
            scale = 0.10471975511965978;
        } else if ((unit == @as(u32, @bitCast(PWR_UNIT_RAD))) and (q.unit == @as(u32, @bitCast(PWR_UNIT_DEG)))) {
            scale = 0.017453292519943295;
        } else {
            return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_UNIT)), id, field, "Quantity has an incompatible unit.", PWR_STATUS_INVALID_MODEL);
        }
    }
    const value: f64 = q.value * scale;
    _ = &value;
    if (!(power_isfinite(value) != 0) or ((unit == @as(u32, @bitCast(PWR_UNIT_NONE))) and (value != 0.0))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_RANGE)), id, field, "Quantity must be finite; unused quantities must be zero/NONE.", PWR_STATUS_INVALID_MODEL);
    }
    out.* = if (value == 0.0) 0.0 else value;
    return PWR_STATUS_OK;
}

pub fn pwr_graph_node_compile(arg_src: [*c]const pwr_node_desc, arg_node: [*c]pwr_graph_node, arg_d: [*c]pwr_model_diagnostic) callconv(.c) pwr_status {
    var src = arg_src;
    _ = &src;
    var node = arg_node;
    _ = &node;
    var d = arg_d;
    _ = &d;
    var status: pwr_status = undefined;
    _ = &status;
    node.*.id = src.*.id;
    node.*.domain = src.*.domain;
    if (node.*.id == @as(c_uint, 0)) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_ID)), @as(u32, @bitCast(@as(c_int, 0))), "id", "Object zero is reserved for boundaries and the ledger.", PWR_STATUS_INVALID_MODEL);
    }
    if ((node.*.domain != @as(u32, @bitCast(PWR_NODE_ROTATIONAL))) and (node.*.domain != @as(u32, @bitCast(PWR_NODE_THERMAL)))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_SCHEMA)), node.*.id, "domain", "This compiler supports rotational and thermal storage nodes.", PWR_STATUS_UNSUPPORTED);
    }
    const rotor: bool = node.*.domain == @as(u32, @bitCast(PWR_NODE_ROTATIONAL));
    _ = &rotor;
    status = pwr_graph_quantity(src.*.storage, @as(u32, @bitCast(if (@as(c_int, @intFromBool(rotor)) != 0) PWR_UNIT_KG_M2 else PWR_UNIT_J_K)), &node.*.storage, d, node.*.id, "storage");
    if (status != PWR_STATUS_OK) {
        return status;
    }
    status = pwr_graph_quantity(src.*.initial, @as(u32, @bitCast(if (@as(c_int, @intFromBool(rotor)) != 0) PWR_UNIT_RAD_S else PWR_UNIT_K)), &node.*.initial, d, node.*.id, "initial");
    if (status != PWR_STATUS_OK) {
        return status;
    }
    status = pwr_graph_quantity(src.*.position, @as(u32, @bitCast(if (@as(c_int, @intFromBool(rotor)) != 0) PWR_UNIT_RAD else PWR_UNIT_NONE)), &node.*.position, d, node.*.id, "position");
    if (status != PWR_STATUS_OK) {
        return status;
    }
    if (!(node.*.storage > 0.0) or (!rotor and !(node.*.initial > 0.0))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_RANGE)), node.*.id, if (node.*.storage <= 0.0) "storage" else "initial", "Storage and absolute temperature must be positive.", PWR_STATUS_INVALID_MODEL);
    }
    return PWR_STATUS_OK;
}

pub fn pwr_graph_find_node(arg_g: [*c]const pwr_graph, arg_id: u32) callconv(.c) u32 {
    var g = arg_g;
    _ = &g;
    var id = arg_id;
    _ = &id;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.node_count) : (i +%= 1) {
            if (g.*.nodes[i].id == id) {
                return i;
            }
        }
    }
    return @as(c_uint, 4294967295);
}

pub fn pwr_graph_has_domain(arg_g: [*c]const pwr_graph, arg_index: u32, arg_domain: u32) callconv(.c) bool {
    var g = arg_g;
    _ = &g;
    var index = arg_index;
    _ = &index;
    var domain = arg_domain;
    _ = &domain;
    return (index != @as(c_uint, 4294967295)) and (g.*.nodes[index].domain == domain);
}

pub fn pwr_graph_component_compile(arg_g: [*c]const pwr_graph, arg_src: [*c]const pwr_component_desc, arg_c: [*c]pwr_graph_component, arg_d: [*c]pwr_model_diagnostic) callconv(.c) pwr_status {
    var g = arg_g;
    _ = &g;
    var src = arg_src;
    _ = &src;
    var c = arg_c;
    _ = &c;
    var d = arg_d;
    _ = &d;
    var status: pwr_status = undefined;
    _ = &status;
    c.*.id = src.*.id;
    c.*.kind = src.*.kind;
    c.*.a = pwr_graph_find_node(g, src.*.node_a);
    c.*.b = pwr_graph_find_node(g, src.*.node_b);
    c.*.heat = pwr_graph_find_node(g, src.*.heat_node);
    c.*.state_index = @as(c_uint, 4294967295);
    c.*.input_channel = src.*.input_channel;
    if ((c.*.id == @as(c_uint, 0)) or (pwr_graph_find_node(g, c.*.id) != @as(c_uint, 4294967295))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_ID)), c.*.id, "id", "Component ID must be nonzero and distinct from node IDs.", PWR_STATUS_INVALID_MODEL);
    }
    if (((src.*.reserved != @as(c_uint, 0)) or (c.*.kind < @as(u32, @bitCast(PWR_COMPONENT_SHAFT)))) or (c.*.kind > @as(u32, @bitCast(PWR_COMPONENT_THERMAL_LINK)))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_SCHEMA)), c.*.id, "kind", "Unknown component kind or nonzero reserved field.", PWR_STATUS_UNSUPPORTED);
    }
    const thermal: bool = c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_THERMAL_LINK));
    _ = &thermal;
    const pair: bool = (@as(c_int, @intFromBool(thermal)) != 0) or (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_SHAFT)));
    _ = &pair;
    const domain: u32 = @as(u32, @bitCast(if (@as(c_int, @intFromBool(thermal)) != 0) PWR_NODE_THERMAL else PWR_NODE_ROTATIONAL));
    _ = &domain;
    if (!pwr_graph_has_domain(g, c.*.a, domain)) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_CONNECTION)), c.*.id, "node_a", "Required node is missing or belongs to a different domain.", PWR_STATUS_INVALID_MODEL);
    }
    if (((src.*.node_b != @as(c_uint, 0)) and (!pair or !pwr_graph_has_domain(g, c.*.b, domain))) or ((src.*.node_b != @as(c_uint, 0)) and (c.*.b == c.*.a))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_CONNECTION)), c.*.id, "node_b", "Connection must reference a distinct node of the required domain.", PWR_STATUS_INVALID_MODEL);
    }
    if ((src.*.heat_node != @as(c_uint, 0)) and (((c.*.kind != @as(u32, @bitCast(PWR_COMPONENT_SHAFT))) and (c.*.kind != @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR)))) or !pwr_graph_has_domain(g, c.*.heat, @as(u32, @bitCast(PWR_NODE_THERMAL))))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_CONNECTION)), c.*.id, "heat_node", "Only shaft and motor losses can target a thermal storage node.", PWR_STATUS_INVALID_MODEL);
    }
    const input: bool = (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR))) or (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_TORQUE_SOURCE)));
    _ = &input;
    if (((@as(c_int, @intFromBool(input)) != 0) and ((c.*.input_channel == @as(u64, @bitCast(@as(u64, @as(c_uint, 0))))) or ((c.*.input_channel >> @intCast(63)) != @as(u64, @bitCast(@as(u64, @as(c_uint, 0))))))) or (!input and (c.*.input_channel != @as(u64, @bitCast(@as(u64, @as(c_uint, 0))))))) {
        return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_CHANNEL)), c.*.id, "input_channel", "Sources require a nonzero channel with its high bit clear; other components require zero.", PWR_STATUS_INVALID_MODEL);
    }
    status = pwr_graph_quantity(src.*.initial_input, @as(u32, @bitCast(if (@as(c_int, @intFromBool(input)) != 0) if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR))) PWR_UNIT_V else PWR_UNIT_NM else PWR_UNIT_NONE)), &c.*.initial_input, d, c.*.id, "initial_input");
    if (status != PWR_STATUS_OK) {
        return status;
    }
    if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_SHAFT))) {
        while (true) {
            status = pwr_graph_quantity(src.*.parameters.shaft.stiffness, @as(u32, @bitCast(PWR_UNIT_NM_RAD)), &c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))], d, c.*.id, "shaft.stiffness");
            if (status != PWR_STATUS_OK) {
                return status;
            }
            if (!false) break;
        }
        while (true) {
            status = pwr_graph_quantity(src.*.parameters.shaft.damping, @as(u32, @bitCast(PWR_UNIT_NM_S_RAD)), &c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))], d, c.*.id, "shaft.damping");
            if (status != PWR_STATUS_OK) {
                return status;
            }
            if (!false) break;
        }
        while (true) {
            status = pwr_graph_quantity(src.*.parameters.shaft.rest_angle, @as(u32, @bitCast(PWR_UNIT_RAD)), &c.*.p[@as(c_uint, @intCast(@as(c_int, 2)))], d, c.*.id, "shaft.rest_angle");
            if (status != PWR_STATUS_OK) {
                return status;
            }
            if (!false) break;
        }
        c.*.p[@as(c_uint, @intCast(@as(c_int, 3)))] = src.*.parameters.shaft.ratio;
        if (((((c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))] < 0.0) or (c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))] < 0.0)) or !(power_isfinite(c.*.p[@as(c_uint, @intCast(@as(c_int, 3)))]) != 0)) or (c.*.p[@as(c_uint, @intCast(@as(c_int, 3)))] == 0.0)) or ((src.*.node_b == @as(c_uint, 0)) and (c.*.p[@as(c_uint, @intCast(@as(c_int, 3)))] != 1.0))) {
            return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_RANGE)), c.*.id, "shaft", "Stiffness/damping must be nonnegative; ratio must be finite and nonzero (one at ground).", PWR_STATUS_INVALID_MODEL);
        }
    } else if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR))) {
        while (true) {
            status = pwr_graph_quantity(src.*.parameters.motor.resistance, @as(u32, @bitCast(PWR_UNIT_OHM)), &c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))], d, c.*.id, "motor.resistance");
            if (status != PWR_STATUS_OK) {
                return status;
            }
            if (!false) break;
        }
        while (true) {
            status = pwr_graph_quantity(src.*.parameters.motor.inductance, @as(u32, @bitCast(PWR_UNIT_H)), &c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))], d, c.*.id, "motor.inductance");
            if (status != PWR_STATUS_OK) {
                return status;
            }
            if (!false) break;
        }
        while (true) {
            status = pwr_graph_quantity(src.*.parameters.motor.coupling, @as(u32, @bitCast(PWR_UNIT_NM_A)), &c.*.p[@as(c_uint, @intCast(@as(c_int, 2)))], d, c.*.id, "motor.coupling");
            if (status != PWR_STATUS_OK) {
                return status;
            }
            if (!false) break;
        }
        while (true) {
            status = pwr_graph_quantity(src.*.parameters.motor.initial_current, @as(u32, @bitCast(PWR_UNIT_A)), &c.*.p[@as(c_uint, @intCast(@as(c_int, 3)))], d, c.*.id, "motor.initial_current");
            if (status != PWR_STATUS_OK) {
                return status;
            }
            if (!false) break;
        }
        if ((c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))] < 0.0) or !(c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))] > 0.0)) {
            return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_RANGE)), c.*.id, "motor", "Resistance must be nonnegative and inductance positive.", PWR_STATUS_INVALID_MODEL);
        }
    } else if (thermal) {
        while (true) {
            status = pwr_graph_quantity(src.*.parameters.thermal.conductance, @as(u32, @bitCast(PWR_UNIT_W_K)), &c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))], d, c.*.id, "thermal.conductance");
            if (status != PWR_STATUS_OK) {
                return status;
            }
            if (!false) break;
        }
        while (true) {
            status = pwr_graph_quantity(src.*.parameters.thermal.ambient_temperature, @as(u32, @bitCast(if (src.*.node_b == @as(c_uint, 0)) PWR_UNIT_K else PWR_UNIT_NONE)), &c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))], d, c.*.id, "thermal.ambient_temperature");
            if (status != PWR_STATUS_OK) {
                return status;
            }
            if (!false) break;
        }
        if ((c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))] < 0.0) or ((src.*.node_b == @as(c_uint, 0)) and !(c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))] > 0.0))) {
            return pwr_graph_error(d, @as(u32, @bitCast(PWR_MODEL_DIAG_RANGE)), c.*.id, "thermal", "Conductance must be nonnegative and ambient temperature positive.", PWR_STATUS_INVALID_MODEL);
        }
    }
    return PWR_STATUS_OK;
}

pub fn pwr_graph_factor(arg_m: [*c]pwr_graph_matrix) callconv(.c) bool {
    var m = arg_m;
    _ = &m;
    var scale: [64]f64 = [1]f64{
        0,
    } ++ [1]f64{0} ** 63;
    _ = &scale;
    {
        var row: u32 = 0;
        _ = &row;
        while (row < m.*.size) : (row +%= 1) {
            {
                var col: u32 = 0;
                _ = &col;
                while (col < m.*.size) : (col +%= 1) {
                    const v: f64 = fabs(m.*.lu[row][col]);
                    _ = &v;
                    if (!(power_isfinite(v) != 0)) {
                        return @as(c_int, 0) != 0;
                    }
                    scale[row] = fmax(scale[row], v);
                }
            }
            if (scale[row] == 0.0) {
                return @as(c_int, 0) != 0;
            }
        }
    }
    {
        var k: u32 = 0;
        _ = &k;
        while (k < m.*.size) : (k +%= 1) {
            var pivot: u32 = k;
            _ = &pivot;
            {
                var row: u32 = k +% @as(c_uint, 1);
                _ = &row;
                while (row < m.*.size) : (row +%= 1) {
                    if ((fabs(m.*.lu[row][k]) / scale[row]) > (fabs(m.*.lu[pivot][k]) / scale[pivot])) {
                        pivot = row;
                    }
                }
            }
            if ((fabs(m.*.lu[pivot][k]) / scale[pivot]) < (64.0 * 0.0000000000000002220446049250313)) {
                return @as(c_int, 0) != 0;
            }
            m.*.pivots[k] = pivot;
            if (pivot != k) {
                const tmp_scale: f64 = scale[k];
                _ = &tmp_scale;
                scale[k] = scale[pivot];
                scale[pivot] = tmp_scale;
                {
                    var col: u32 = 0;
                    _ = &col;
                    while (col < m.*.size) : (col +%= 1) {
                        const tmp: f64 = m.*.lu[k][col];
                        _ = &tmp;
                        m.*.lu[k][col] = m.*.lu[pivot][col];
                        m.*.lu[pivot][col] = tmp;
                    }
                }
            }
            {
                var row: u32 = k +% @as(c_uint, 1);
                _ = &row;
                while (row < m.*.size) : (row +%= 1) {
                    m.*.lu[row][k] /= m.*.lu[k][k];
                    if (!(power_isfinite(m.*.lu[row][k]) != 0)) {
                        return @as(c_int, 0) != 0;
                    }
                    {
                        var col: u32 = k +% @as(c_uint, 1);
                        _ = &col;
                        while (col < m.*.size) : (col +%= 1) {
                            m.*.lu[row][col] -= m.*.lu[row][k] * m.*.lu[k][col];
                            if (!(power_isfinite(m.*.lu[row][col]) != 0)) {
                                return @as(c_int, 0) != 0;
                            }
                        }
                    }
                }
            }
        }
    }
    return @as(c_int, 1) != 0;
}

pub fn pwr_graph_solve(arg_m: [*c]const pwr_graph_matrix, arg_rhs: [*c]f64) callconv(.c) bool {
    var m = arg_m;
    _ = &m;
    var rhs = arg_rhs;
    _ = &rhs;
    {
        var k: u32 = 0;
        _ = &k;
        while (k < m.*.size) : (k +%= 1) {
            const tmp: f64 = rhs[k];
            _ = &tmp;
            rhs[k] = rhs[m.*.pivots[k]];
            rhs[m.*.pivots[k]] = tmp;
        }
    }
    {
        var row: u32 = 0;
        _ = &row;
        while (row < m.*.size) : (row +%= 1) {
            {
                var col: u32 = 0;
                _ = &col;
                while (col < row) : (col +%= 1) {
                    rhs[row] -= m.*.lu[row][col] * rhs[col];
                }
            }
        }
    }
    {
        var end: u32 = m.*.size;
        _ = &end;
        while (end > @as(c_uint, 0)) : (end -%= 1) {
            const row: u32 = end -% @as(c_uint, 1);
            _ = &row;
            {
                var col: u32 = row +% @as(c_uint, 1);
                _ = &col;
                while (col < m.*.size) : (col +%= 1) {
                    rhs[row] -= m.*.lu[row][col] * rhs[col];
                }
            }
            rhs[row] /= m.*.lu[row][row];
            if (!(power_isfinite(rhs[row]) != 0)) {
                return @as(c_int, 0) != 0;
            }
        }
    }
    return @as(c_int, 1) != 0;
}

pub fn pwr_graph_hash_word(arg_hash: u64, arg_word: u64) callconv(.c) u64 {
    var hash = arg_hash;
    _ = &hash;
    var word = arg_word;
    _ = &word;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < @as(c_uint, 8)) : (i +%= 1) {
            hash = (hash ^ (word & @as(u64, 255))) *% @as(u64, 1099511628211);
            word >>= @intCast(@as(c_int, 8));
        }
    }
    return hash;
}

pub fn pwr_graph_hash_double(arg_hash: u64, arg_value: f64) callconv(.c) u64 {
    var hash = arg_hash;
    _ = &hash;
    var value = arg_value;
    _ = &value;
    var bits: u64 = 0;
    _ = &bits;
    const normalized: f64 = if (value == 0.0) 0.0 else value;
    _ = &normalized;
    _ = memcpy(@as(?*anyopaque, @ptrCast(&bits)), @as(?*const anyopaque, @ptrCast(&normalized)), @sizeOf(u64));
    return pwr_graph_hash_word(hash, bits);
}

pub fn pwr_graph_fingerprint(arg_g: [*c]const pwr_graph) callconv(.c) u64 {
    var g = arg_g;
    _ = &g;
    var hash: u64 = 14695981039346656037;
    _ = &hash;
    hash = pwr_graph_hash_word(hash, @as(u64, @bitCast(@as(u64, @as(c_uint, 1)))));
    hash = pwr_graph_hash_word(hash, @as(u64, 1));
    hash = pwr_graph_hash_word(hash, g.*.step_ns);
    hash = pwr_graph_hash_word(hash, @as(u64, @bitCast(@as(u64, g.*.node_count))));
    hash = pwr_graph_hash_word(hash, @as(u64, @bitCast(@as(u64, g.*.component_count))));
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.node_count) : (i +%= 1) {
            var n: [*c]const pwr_graph_node = &g.*.nodes[i];
            _ = &n;
            hash = pwr_graph_hash_word(hash, @as(u64, @bitCast(@as(u64, n.*.id))));
            hash = pwr_graph_hash_word(hash, @as(u64, @bitCast(@as(u64, n.*.domain))));
            hash = pwr_graph_hash_double(hash, n.*.storage);
            hash = pwr_graph_hash_double(hash, n.*.initial);
            hash = pwr_graph_hash_double(hash, n.*.position);
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            var c: [*c]const pwr_graph_component = &g.*.components[i];
            _ = &c;
            hash = pwr_graph_hash_word(hash, @as(u64, @bitCast(@as(u64, c.*.id))));
            hash = pwr_graph_hash_word(hash, @as(u64, @bitCast(@as(u64, c.*.kind))));
            hash = pwr_graph_hash_word(hash, @as(u64, @bitCast(@as(u64, c.*.a))));
            hash = pwr_graph_hash_word(hash, @as(u64, @bitCast(@as(u64, c.*.b))));
            hash = pwr_graph_hash_word(hash, @as(u64, @bitCast(@as(u64, c.*.heat))));
            hash = pwr_graph_hash_word(hash, c.*.input_channel);
            hash = pwr_graph_hash_double(hash, c.*.initial_input);
            {
                var p: u32 = 0;
                _ = &p;
                while (p < @as(c_uint, 4)) : (p +%= 1) {
                    hash = pwr_graph_hash_double(hash, c.*.p[p]);
                }
            }
        }
    }
    return hash;
}

pub fn pwr_graph_channel(arg_g: [*c]pwr_graph, arg_channel: u64, arg_object: u32, arg_direction: u32, arg_unit: u32, arg_quantity: [*c]const u8) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    var channel = arg_channel;
    _ = &channel;
    var object = arg_object;
    _ = &object;
    var direction = arg_direction;
    _ = &direction;
    var unit = arg_unit;
    _ = &unit;
    var quantity = arg_quantity;
    _ = &quantity;
    var info: [*c]pwr_channel_info = &g.*.channels[
        blk: {
            const ref = &g.*.channel_count;
            const tmp = ref.*;
            ref.* +%= 1;
            break :blk tmp;
        }
    ];
    _ = &info;
    info.*.channel = channel;
    info.*.object_id = object;
    info.*.direction = direction;
    info.*.unit = unit;
    _ = snprintf(@as([*c]u8, @ptrCast(@alignCast(&info.*.quantity[@as(usize, @intCast(0))]))), @sizeOf([32]u8), "%s", quantity);
    if (direction == @as(u32, @bitCast(PWR_CHANNEL_OUTPUT))) {
        g.*.output_count +%= 1;
    }
}

pub fn pwr_graph_output_channel(arg_g: [*c]pwr_graph, arg_object: u32, arg_field: u32, arg_unit: u32, arg_quantity: [*c]const u8) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    var object = arg_object;
    _ = &object;
    var field = arg_field;
    _ = &field;
    var unit = arg_unit;
    _ = &unit;
    var quantity = arg_quantity;
    _ = &quantity;
    pwr_graph_channel(g, (@as(u64, 9223372036854775808) | (@as(u64, @bitCast(@as(u64, object))) << @intCast(8))) | @as(u64, @bitCast(@as(u64, field))), object, @as(u32, @bitCast(PWR_CHANNEL_OUTPUT)), unit, quantity);
}

pub fn pwr_graph_assemble(arg_g: [*c]pwr_graph) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    var a: [*c][64]f64 = @as([*c][64]f64, @ptrCast(@alignCast(&g.*.dynamics.lu[@as(usize, @intCast(0))])));
    _ = &a;
    var t: [*c][64]f64 = @as([*c][64]f64, @ptrCast(@alignCast(&g.*.thermal.lu[@as(usize, @intCast(0))])));
    _ = &t;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.node_count) : (i +%= 1) {
            var n: [*c]const pwr_graph_node = &g.*.nodes[i];
            _ = &n;
            const s: u32 = n.*.state_index;
            _ = &s;
            if (n.*.domain == @as(u32, @bitCast(PWR_NODE_ROTATIONAL))) {
                a[s][s +% @as(c_uint, 1)] = 1.0;
                pwr_graph_output_channel(g, n.*.id, @as(u32, @bitCast(PWR_FIELD_ANGLE)), @as(u32, @bitCast(PWR_UNIT_RAD)), "angle");
                pwr_graph_output_channel(g, n.*.id, @as(u32, @bitCast(PWR_FIELD_SPEED)), @as(u32, @bitCast(PWR_UNIT_RAD_S)), "speed");
            } else {
                t[s][s] = n.*.storage;
                pwr_graph_output_channel(g, n.*.id, @as(u32, @bitCast(PWR_FIELD_TEMPERATURE)), @as(u32, @bitCast(PWR_UNIT_K)), "temperature");
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            var c: [*c]const pwr_graph_component = &g.*.components[i];
            _ = &c;
            var na: [*c]const pwr_graph_node = &g.*.nodes[c.*.a];
            _ = &na;
            const sa: u32 = na.*.state_index;
            _ = &sa;
            const sb: u32 = if (c.*.b == @as(c_uint, 4294967295)) @as(c_uint, 4294967295) else g.*.nodes[c.*.b].state_index;
            _ = &sb;
            if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_SHAFT))) {
                const indices: [2]u32 = [2]u32{
                    sa,
                    sb,
                };
                _ = &indices;
                const direction: [2]f64 = [2]f64{
                    1.0,
                    -c.*.p[@as(c_uint, @intCast(@as(c_int, 3)))],
                };
                _ = &direction;
                const count: u32 = if (sb == @as(c_uint, 4294967295)) @as(c_uint, 1) else @as(c_uint, 2);
                _ = &count;
                {
                    var row: u32 = 0;
                    _ = &row;
                    while (row < count) : (row +%= 1) {
                        const inertia: f64 = g.*.nodes[if (row == @as(c_uint, 0)) c.*.a else c.*.b].storage;
                        _ = &inertia;
                        const velocity: u32 = indices[row] +% @as(c_uint, 1);
                        _ = &velocity;
                        g.*.constant_force[velocity] += ((direction[row] * c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))]) * c.*.p[@as(c_uint, @intCast(@as(c_int, 2)))]) / inertia;
                        {
                            var col: u32 = 0;
                            _ = &col;
                            while (col < count) : (col +%= 1) {
                                const factor: f64 = (direction[row] * direction[col]) / inertia;
                                _ = &factor;
                                a[velocity][indices[col]] -= factor * c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))];
                                a[velocity][indices[col] +% @as(c_uint, 1)] -= factor * c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))];
                            }
                        }
                    }
                }
                pwr_graph_output_channel(g, c.*.id, @as(u32, @bitCast(PWR_FIELD_TWIST)), @as(u32, @bitCast(PWR_UNIT_RAD)), "twist");
                pwr_graph_output_channel(g, c.*.id, @as(u32, @bitCast(PWR_FIELD_TORQUE)), @as(u32, @bitCast(PWR_UNIT_NM)), "reaction_torque_at_a");
            } else if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR))) {
                const current: u32 = c.*.state_index;
                _ = &current;
                a[sa +% @as(c_uint, 1)][current] += c.*.p[@as(c_uint, @intCast(@as(c_int, 2)))] / na.*.storage;
                a[current][sa +% @as(c_uint, 1)] -= c.*.p[@as(c_uint, @intCast(@as(c_int, 2)))] / c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))];
                a[current][current] -= c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))] / c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))];
                pwr_graph_channel(g, c.*.input_channel, c.*.id, @as(u32, @bitCast(PWR_CHANNEL_INPUT)), @as(u32, @bitCast(PWR_UNIT_V)), "voltage");
                pwr_graph_output_channel(g, c.*.id, @as(u32, @bitCast(PWR_FIELD_CURRENT)), @as(u32, @bitCast(PWR_UNIT_A)), "current");
                pwr_graph_output_channel(g, c.*.id, @as(u32, @bitCast(PWR_FIELD_TORQUE)), @as(u32, @bitCast(PWR_UNIT_NM)), "motor_torque");
            } else if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_TORQUE_SOURCE))) {
                pwr_graph_channel(g, c.*.input_channel, c.*.id, @as(u32, @bitCast(PWR_CHANNEL_INPUT)), @as(u32, @bitCast(PWR_UNIT_NM)), "torque");
            } else {
                const conductance_dt: f64 = g.*.dt * c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))];
                _ = &conductance_dt;
                t[sa][sa] += conductance_dt;
                if (sb == @as(c_uint, 4294967295)) {
                    g.*.ambient_force[sa] += conductance_dt * c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))];
                } else {
                    t[sa][sb] -= conductance_dt;
                    t[sb][sa] -= conductance_dt;
                    t[sb][sb] += conductance_dt;
                }
            }
        }
    }
    {
        var row: u32 = 0;
        _ = &row;
        while (row < g.*.dynamic_count) : (row +%= 1) {
            {
                var col: u32 = 0;
                _ = &col;
                while (col < g.*.dynamic_count) : (col +%= 1) {
                    a[row][col] = (if (row == col) @as(f64, 1.0) else @as(f64, 0.0)) - ((0.5 * g.*.dt) * a[row][col]);
                }
            }
        }
    }
    g.*.dynamics.size = g.*.dynamic_count;
    g.*.thermal.size = g.*.thermal_count;
    pwr_graph_output_channel(g, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_SOURCE_WORK)), @as(u32, @bitCast(PWR_UNIT_J)), "source_work");
    pwr_graph_output_channel(g, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_HEAT_REJECTED)), @as(u32, @bitCast(PWR_UNIT_J)), "heat_rejected");
    pwr_graph_output_channel(g, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_STORED_ENERGY_CHANGE)), @as(u32, @bitCast(PWR_UNIT_J)), "stored_energy_change");
    pwr_graph_output_channel(g, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_ENERGY_RESIDUAL)), @as(u32, @bitCast(PWR_UNIT_J)), "energy_residual");
}

pub fn pwr_graph_relative(arg_g: [*c]const pwr_graph, arg_c: [*c]const pwr_graph_component, arg_x: [*c]const f64, arg_offset: u32) callconv(.c) f64 {
    var g = arg_g;
    _ = &g;
    var c = arg_c;
    _ = &c;
    var x = arg_x;
    _ = &x;
    var offset = arg_offset;
    _ = &offset;
    const a: f64 = x[g.*.nodes[c.*.a].state_index +% offset];
    _ = &a;
    const b: f64 = if (c.*.b == @as(c_uint, 4294967295)) 0.0 else x[g.*.nodes[c.*.b].state_index +% offset];
    _ = &b;
    return (a - (c.*.p[@as(c_uint, @intCast(@as(c_int, 3)))] * b)) - (if (offset == @as(c_uint, 0)) c.*.p[@as(c_uint, @intCast(@as(c_int, 2)))] else 0.0);
}

pub fn pwr_graph_energy(arg_g: [*c]const pwr_graph, arg_s: [*c]const pwr_graph_state) callconv(.c) f64 {
    var g = arg_g;
    _ = &g;
    var s = arg_s;
    _ = &s;
    var energy: f64 = 0.0;
    _ = &energy;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.node_count) : (i +%= 1) {
            var n: [*c]const pwr_graph_node = &g.*.nodes[i];
            _ = &n;
            if (n.*.domain == @as(u32, @bitCast(PWR_NODE_ROTATIONAL))) {
                const speed: f64 = s.*.x[n.*.state_index +% @as(c_uint, 1)];
                _ = &speed;
                energy += ((0.5 * n.*.storage) * speed) * speed;
            } else {
                energy += n.*.storage * (s.*.temperature[n.*.state_index] - n.*.initial);
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            var c: [*c]const pwr_graph_component = &g.*.components[i];
            _ = &c;
            if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_SHAFT))) {
                const twist: f64 = pwr_graph_relative(g, c, @as([*c]const f64, @ptrCast(@alignCast(&s.*.x[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 0))));
                _ = &twist;
                energy += ((0.5 * c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))]) * twist) * twist;
            } else if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR))) {
                const current: f64 = s.*.x[c.*.state_index];
                _ = &current;
                energy += ((0.5 * c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))]) * current) * current;
            }
        }
    }
    return energy;
}

pub fn pwr_graph_observables_finite(arg_g: [*c]const pwr_graph, arg_s: [*c]const pwr_graph_state) callconv(.c) bool {
    var g = arg_g;
    _ = &g;
    var s = arg_s;
    _ = &s;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            var c: [*c]const pwr_graph_component = &g.*.components[i];
            _ = &c;
            if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_SHAFT))) {
                const twist: f64 = pwr_graph_relative(g, c, @as([*c]const f64, @ptrCast(@alignCast(&s.*.x[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 0))));
                _ = &twist;
                const torque: f64 = (-c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))] * twist) - (c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))] * pwr_graph_relative(g, c, @as([*c]const f64, @ptrCast(@alignCast(&s.*.x[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 1)))));
                _ = &torque;
                if (!(power_isfinite(twist) != 0) or !(power_isfinite(torque) != 0)) {
                    return @as(c_int, 0) != 0;
                }
            } else if ((c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR))) and !(power_isfinite(c.*.p[@as(c_uint, @intCast(@as(c_int, 2)))] * s.*.x[c.*.state_index]) != 0)) {
                return @as(c_int, 0) != 0;
            }
        }
    }
    return @as(c_int, 1) != 0;
}

pub fn pwr_graph_accumulate(arg_value: f64, arg_sum: [*c]f64, arg_correction: [*c]f64) callconv(.c) void {
    var value = arg_value;
    _ = &value;
    var sum = arg_sum;
    _ = &sum;
    var correction = arg_correction;
    _ = &correction;
    const adjusted: f64 = value - correction.*;
    _ = &adjusted;
    const next: f64 = sum.* + adjusted;
    _ = &next;
    correction.* = (next - sum.*) - adjusted;
    sum.* = next;
}

pub fn pwr_graph_tick(arg_g: [*c]const pwr_graph, arg_s: [*c]pwr_graph_state) callconv(.c) pwr_status {
    var g = arg_g;
    _ = &g;
    var s = arg_s;
    _ = &s;
    var mid: [64]f64 = [1]f64{
        0,
    } ++ [1]f64{0} ** 63;
    _ = &mid;
    var temperature: [64]f64 = [1]f64{
        0,
    } ++ [1]f64{0} ** 63;
    _ = &temperature;
    var work: f64 = 0.0;
    _ = &work;
    var heat_out: f64 = 0.0;
    _ = &heat_out;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.dynamic_count) : (i +%= 1) {
            mid[i] = s.*.x[i] + ((0.5 * g.*.dt) * g.*.constant_force[i]);
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            var c: [*c]const pwr_graph_component = &g.*.components[i];
            _ = &c;
            if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_TORQUE_SOURCE))) {
                var n: [*c]const pwr_graph_node = &g.*.nodes[c.*.a];
                _ = &n;
                mid[n.*.state_index +% @as(c_uint, 1)] += ((0.5 * g.*.dt) * s.*.inputs[i]) / n.*.storage;
            } else if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR))) {
                mid[c.*.state_index] += ((0.5 * g.*.dt) * s.*.inputs[i]) / c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))];
            }
        }
    }
    if (!pwr_graph_solve(&g.*.dynamics, @as([*c]f64, @ptrCast(@alignCast(&mid[@as(usize, @intCast(0))]))))) {
        return PWR_STATUS_NUMERIC_ERROR;
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.node_count) : (i +%= 1) {
            var n: [*c]const pwr_graph_node = &g.*.nodes[i];
            _ = &n;
            if (n.*.domain == @as(u32, @bitCast(PWR_NODE_THERMAL))) {
                temperature[n.*.state_index] = (n.*.storage * s.*.temperature[n.*.state_index]) + g.*.ambient_force[n.*.state_index];
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            var c: [*c]const pwr_graph_component = &g.*.components[i];
            _ = &c;
            var loss: f64 = 0.0;
            _ = &loss;
            if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_SHAFT))) {
                const slip: f64 = pwr_graph_relative(g, c, @as([*c]f64, @ptrCast(@alignCast(&mid[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 1))));
                _ = &slip;
                loss = ((g.*.dt * c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))]) * slip) * slip;
            } else if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR))) {
                const current: f64 = mid[c.*.state_index];
                _ = &current;
                loss = ((g.*.dt * c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))]) * current) * current;
                work += (g.*.dt * s.*.inputs[i]) * current;
            } else if (c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_TORQUE_SOURCE))) {
                work += (g.*.dt * s.*.inputs[i]) * mid[g.*.nodes[c.*.a].state_index +% @as(c_uint, 1)];
            }
            if (c.*.heat == @as(c_uint, 4294967295)) {
                heat_out += loss;
            } else {
                temperature[g.*.nodes[c.*.heat].state_index] += loss;
            }
        }
    }
    if (!pwr_graph_solve(&g.*.thermal, @as([*c]f64, @ptrCast(@alignCast(&temperature[@as(usize, @intCast(0))]))))) {
        return PWR_STATUS_NUMERIC_ERROR;
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.component_count) : (i +%= 1) {
            var c: [*c]const pwr_graph_component = &g.*.components[i];
            _ = &c;
            if ((c.*.kind == @as(u32, @bitCast(PWR_COMPONENT_THERMAL_LINK))) and (c.*.b == @as(c_uint, 4294967295))) {
                heat_out += (g.*.dt * c.*.p[@as(c_uint, @intCast(@as(c_int, 0)))]) * (temperature[g.*.nodes[c.*.a].state_index] - c.*.p[@as(c_uint, @intCast(@as(c_int, 1)))]);
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.dynamic_count) : (i +%= 1) {
            s.*.x[i] = (2.0 * mid[i]) - s.*.x[i];
            if (!(power_isfinite(s.*.x[i]) != 0)) {
                return PWR_STATUS_NUMERIC_ERROR;
            }
        }
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < g.*.thermal_count) : (i +%= 1) {
            if (!(temperature[i] > 0.0)) {
                return PWR_STATUS_NUMERIC_ERROR;
            }
            s.*.temperature[i] = temperature[i];
        }
    }
    pwr_graph_accumulate(work, &s.*.source_work, &s.*.source_work_compensation);
    pwr_graph_accumulate(heat_out, &s.*.heat_rejected, &s.*.heat_compensation);
    const residual: f64 = (s.*.source_work - s.*.heat_rejected) - (pwr_graph_energy(g, s) - s.*.initial_energy);
    _ = &residual;
    if (((!(power_isfinite(residual) != 0) or !(power_isfinite(s.*.source_work_compensation) != 0)) or !(power_isfinite(s.*.heat_compensation) != 0)) or !pwr_graph_observables_finite(g, s)) {
        return PWR_STATUS_NUMERIC_ERROR;
    }
    s.*.time_ns +%= g.*.step_ns;
    return PWR_STATUS_OK;
}

pub fn pwr_graph_write(arg_snapshot: [*c]pwr_snapshot, arg_index: [*c]u32, arg_object: u32, arg_field: u32, arg_value: f64) callconv(.c) void {
    var snapshot = arg_snapshot;
    _ = &snapshot;
    var index = arg_index;
    _ = &index;
    var object = arg_object;
    _ = &object;
    var field = arg_field;
    _ = &field;
    var value = arg_value;
    _ = &value;
    snapshot.*.values[
        blk: {
            const ref = &index.*;
            const tmp = ref.*;
            ref.* +%= 1;
            break :blk tmp;
        }
    ] = pwr_scalar_value{
        .channel = (@as(u64, 9223372036854775808) | (@as(u64, @bitCast(@as(u64, object))) << @intCast(8))) | @as(u64, @bitCast(@as(u64, field))),
        .value = value,
        .flags = @as(u32, @bitCast(@as(c_int, 0))),
        .reserved = @as(u32, @bitCast(@as(c_int, 0))),
    };
}
