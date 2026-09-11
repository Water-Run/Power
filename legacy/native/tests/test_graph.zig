// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/tests/test_graph.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_COMPONENT_DC_MOTOR = @import("../src/abi.zig").PWR_COMPONENT_DC_MOTOR;
const PWR_COMPONENT_SHAFT = @import("../src/abi.zig").PWR_COMPONENT_SHAFT;
const PWR_COMPONENT_THERMAL_LINK = @import("../src/abi.zig").PWR_COMPONENT_THERMAL_LINK;
const PWR_COMPONENT_TORQUE_SOURCE = @import("../src/abi.zig").PWR_COMPONENT_TORQUE_SOURCE;
const PWR_FIELD_ANGLE = @import("../src/abi.zig").PWR_FIELD_ANGLE;
const PWR_FIELD_CURRENT = @import("../src/abi.zig").PWR_FIELD_CURRENT;
const PWR_FIELD_ENERGY_RESIDUAL = @import("../src/abi.zig").PWR_FIELD_ENERGY_RESIDUAL;
const PWR_FIELD_HEAT_REJECTED = @import("../src/abi.zig").PWR_FIELD_HEAT_REJECTED;
const PWR_FIELD_SOURCE_WORK = @import("../src/abi.zig").PWR_FIELD_SOURCE_WORK;
const PWR_FIELD_SPEED = @import("../src/abi.zig").PWR_FIELD_SPEED;
const PWR_FIELD_TEMPERATURE = @import("../src/abi.zig").PWR_FIELD_TEMPERATURE;
const PWR_MODEL_DIAG_CAPACITY = @import("../src/abi.zig").PWR_MODEL_DIAG_CAPACITY;
const PWR_MODEL_DIAG_CHANNEL = @import("../src/abi.zig").PWR_MODEL_DIAG_CHANNEL;
const PWR_MODEL_DIAG_CONNECTION = @import("../src/abi.zig").PWR_MODEL_DIAG_CONNECTION;
const PWR_MODEL_DIAG_ID = @import("../src/abi.zig").PWR_MODEL_DIAG_ID;
const PWR_MODEL_DIAG_NONE = @import("../src/abi.zig").PWR_MODEL_DIAG_NONE;
const PWR_MODEL_DIAG_UNIT = @import("../src/abi.zig").PWR_MODEL_DIAG_UNIT;
const PWR_NODE_ROTATIONAL = @import("../src/abi.zig").PWR_NODE_ROTATIONAL;
const PWR_NODE_THERMAL = @import("../src/abi.zig").PWR_NODE_THERMAL;
const PWR_STATUS_CAPACITY_EXCEEDED = @import("../src/abi.zig").PWR_STATUS_CAPACITY_EXCEEDED;
const PWR_STATUS_INVALID_ARGUMENT = @import("../src/abi.zig").PWR_STATUS_INVALID_ARGUMENT;
const PWR_STATUS_INVALID_MODEL = @import("../src/abi.zig").PWR_STATUS_INVALID_MODEL;
const PWR_STATUS_INVALID_TIME_STEP = @import("../src/abi.zig").PWR_STATUS_INVALID_TIME_STEP;
const PWR_STATUS_NUMERIC_ERROR = @import("../src/abi.zig").PWR_STATUS_NUMERIC_ERROR;
const PWR_STATUS_OK = @import("../src/abi.zig").PWR_STATUS_OK;
const PWR_STATUS_STRUCT_TOO_SMALL = @import("../src/abi.zig").PWR_STATUS_STRUCT_TOO_SMALL;
const PWR_STATUS_UNKNOWN_CHANNEL = @import("../src/abi.zig").PWR_STATUS_UNKNOWN_CHANNEL;
const PWR_STATUS_UNSUPPORTED = @import("../src/abi.zig").PWR_STATUS_UNSUPPORTED;
const PWR_STATUS_UNSUPPORTED_ABI = @import("../src/abi.zig").PWR_STATUS_UNSUPPORTED_ABI;
const PWR_UNIT_A = @import("../src/abi.zig").PWR_UNIT_A;
const PWR_UNIT_DEG = @import("../src/abi.zig").PWR_UNIT_DEG;
const PWR_UNIT_H = @import("../src/abi.zig").PWR_UNIT_H;
const PWR_UNIT_J_K = @import("../src/abi.zig").PWR_UNIT_J_K;
const PWR_UNIT_K = @import("../src/abi.zig").PWR_UNIT_K;
const PWR_UNIT_KG_M2 = @import("../src/abi.zig").PWR_UNIT_KG_M2;
const PWR_UNIT_NM = @import("../src/abi.zig").PWR_UNIT_NM;
const PWR_UNIT_NM_A = @import("../src/abi.zig").PWR_UNIT_NM_A;
const PWR_UNIT_NM_RAD = @import("../src/abi.zig").PWR_UNIT_NM_RAD;
const PWR_UNIT_NM_S_RAD = @import("../src/abi.zig").PWR_UNIT_NM_S_RAD;
const PWR_UNIT_NONE = @import("../src/abi.zig").PWR_UNIT_NONE;
const PWR_UNIT_OHM = @import("../src/abi.zig").PWR_UNIT_OHM;
const PWR_UNIT_RAD = @import("../src/abi.zig").PWR_UNIT_RAD;
const PWR_UNIT_RAD_S = @import("../src/abi.zig").PWR_UNIT_RAD_S;
const PWR_UNIT_RPM = @import("../src/abi.zig").PWR_UNIT_RPM;
const PWR_UNIT_V = @import("../src/abi.zig").PWR_UNIT_V;
const PWR_UNIT_W_K = @import("../src/abi.zig").PWR_UNIT_W_K;
const __builtin_inff = @import("../src/support.zig").__builtin_inff;
const __builtin_nanf = @import("../src/support.zig").__builtin_nanf;
const acos = @import("../src/support.zig").acos;
const cos = @import("../src/support.zig").cos;
const exp = @import("../src/support.zig").exp;
const fabs = @import("../src/support.zig").fabs;
const fprintf = @import("../src/support.zig").fprintf;
const free = @import("../src/support.zig").free;
const malloc = @import("../src/support.zig").malloc;
const puts = @import("../src/support.zig").puts;
const pwr_channel_id = @import("../src/abi.zig").pwr_channel_id;
const pwr_component_desc = @import("../src/abi.zig").pwr_component_desc;
const pwr_component_parameters = @import("../src/abi.zig").pwr_component_parameters;
const pwr_graph = @import("../src/abi.zig").pwr_graph;
const pwr_graph_compile = @import("../src/core/pwr_graph.zig").pwr_graph_compile;
const pwr_graph_init = @import("../src/core/pwr_graph.zig").pwr_graph_init;
const pwr_graph_snapshot = @import("../src/core/pwr_graph.zig").pwr_graph_snapshot;
const pwr_graph_state = @import("../src/abi.zig").pwr_graph_state;
const pwr_graph_state_hash = @import("../src/core/pwr_graph.zig").pwr_graph_state_hash;
const pwr_graph_step = @import("../src/core/pwr_graph.zig").pwr_graph_step;
const pwr_graph_submit = @import("../src/core/pwr_graph.zig").pwr_graph_submit;
const pwr_input_frame = @import("../src/abi.zig").pwr_input_frame;
const pwr_model_desc = @import("../src/abi.zig").pwr_model_desc;
const pwr_model_diagnostic = @import("../src/abi.zig").pwr_model_diagnostic;
const pwr_node_desc = @import("../src/abi.zig").pwr_node_desc;
const pwr_quantity = @import("../src/abi.zig").pwr_quantity;
const pwr_scalar_value = @import("../src/abi.zig").pwr_scalar_value;
const pwr_snapshot = @import("../src/abi.zig").pwr_snapshot;
const pwr_status = @import("../src/abi.zig").pwr_status;
const strcmp = @import("../src/support.zig").strcmp;

pub var failures: c_int = @import("std").mem.zeroes(c_int);

pub fn rotor(arg_id: u32, arg_inertia: f64, arg_angle: f64, arg_speed: f64) callconv(.c) pwr_node_desc {
    var id = arg_id;
    _ = &id;
    var inertia = arg_inertia;
    _ = &inertia;
    var angle = arg_angle;
    _ = &angle;
    var speed = arg_speed;
    _ = &speed;
    const n: pwr_node_desc = pwr_node_desc{
        .id = id,
        .domain = @as(u32, @bitCast(PWR_NODE_ROTATIONAL)),
        .storage = pwr_quantity{
            .value = inertia,
            .unit = @as(u32, @bitCast(PWR_UNIT_KG_M2)),
            .reserved = @as(c_uint, 0),
        },
        .initial = pwr_quantity{
            .value = speed,
            .unit = @as(u32, @bitCast(PWR_UNIT_RAD_S)),
            .reserved = @as(c_uint, 0),
        },
        .position = pwr_quantity{
            .value = angle,
            .unit = @as(u32, @bitCast(PWR_UNIT_RAD)),
            .reserved = @as(c_uint, 0),
        },
    };
    _ = &n;
    return n;
}

pub fn thermal(arg_id: u32, arg_capacity: f64, arg_temperature: f64) callconv(.c) pwr_node_desc {
    var id = arg_id;
    _ = &id;
    var capacity = arg_capacity;
    _ = &capacity;
    var temperature = arg_temperature;
    _ = &temperature;
    const n: pwr_node_desc = pwr_node_desc{
        .id = id,
        .domain = @as(u32, @bitCast(PWR_NODE_THERMAL)),
        .storage = pwr_quantity{
            .value = capacity,
            .unit = @as(u32, @bitCast(PWR_UNIT_J_K)),
            .reserved = @as(c_uint, 0),
        },
        .initial = pwr_quantity{
            .value = temperature,
            .unit = @as(u32, @bitCast(PWR_UNIT_K)),
            .reserved = @as(c_uint, 0),
        },
        .position = pwr_quantity{
            .value = @as(f64, @floatFromInt(@as(c_int, 0))),
            .unit = @as(u32, @bitCast(PWR_UNIT_NONE)),
            .reserved = @as(c_uint, 0),
        },
    };
    _ = &n;
    return n;
}

pub fn shaft(arg_id: u32, arg_a: u32, arg_b: u32, arg_stiffness: f64, arg_damping: f64, arg_ratio: f64) callconv(.c) pwr_component_desc {
    var id = arg_id;
    _ = &id;
    var a = arg_a;
    _ = &a;
    var b = arg_b;
    _ = &b;
    var stiffness = arg_stiffness;
    _ = &stiffness;
    var damping = arg_damping;
    _ = &damping;
    var ratio = arg_ratio;
    _ = &ratio;
    var c: pwr_component_desc = pwr_component_desc{
        .id = @as(u32, @bitCast(@as(c_int, 0))),
        .kind = @import("std").mem.zeroes(u32),
        .node_a = @import("std").mem.zeroes(u32),
        .node_b = @import("std").mem.zeroes(u32),
        .heat_node = @import("std").mem.zeroes(u32),
        .reserved = @import("std").mem.zeroes(u32),
        .input_channel = @import("std").mem.zeroes(u64),
        .initial_input = @import("std").mem.zeroes(pwr_quantity),
        .parameters = @import("std").mem.zeroes(pwr_component_parameters),
    };
    _ = &c;
    c.id = id;
    c.kind = @as(u32, @bitCast(PWR_COMPONENT_SHAFT));
    c.node_a = a;
    c.node_b = b;
    c.parameters.shaft.stiffness = pwr_quantity{
        .value = stiffness,
        .unit = @as(u32, @bitCast(PWR_UNIT_NM_RAD)),
        .reserved = @as(c_uint, 0),
    };
    c.parameters.shaft.damping = pwr_quantity{
        .value = damping,
        .unit = @as(u32, @bitCast(PWR_UNIT_NM_S_RAD)),
        .reserved = @as(c_uint, 0),
    };
    c.parameters.shaft.rest_angle = pwr_quantity{
        .value = @as(f64, @floatFromInt(@as(c_int, 0))),
        .unit = @as(u32, @bitCast(PWR_UNIT_RAD)),
        .reserved = @as(c_uint, 0),
    };
    c.parameters.shaft.ratio = ratio;
    return c;
}

pub fn source(arg_id: u32, arg_a: u32, arg_torque: f64) callconv(.c) pwr_component_desc {
    var id = arg_id;
    _ = &id;
    var a = arg_a;
    _ = &a;
    var torque = arg_torque;
    _ = &torque;
    var c: pwr_component_desc = pwr_component_desc{
        .id = @as(u32, @bitCast(@as(c_int, 0))),
        .kind = @import("std").mem.zeroes(u32),
        .node_a = @import("std").mem.zeroes(u32),
        .node_b = @import("std").mem.zeroes(u32),
        .heat_node = @import("std").mem.zeroes(u32),
        .reserved = @import("std").mem.zeroes(u32),
        .input_channel = @import("std").mem.zeroes(u64),
        .initial_input = @import("std").mem.zeroes(pwr_quantity),
        .parameters = @import("std").mem.zeroes(pwr_component_parameters),
    };
    _ = &c;
    c.id = id;
    c.kind = @as(u32, @bitCast(PWR_COMPONENT_TORQUE_SOURCE));
    c.node_a = a;
    c.input_channel = @as(u64, @bitCast(@as(u64, id)));
    c.initial_input = pwr_quantity{
        .value = torque,
        .unit = @as(u32, @bitCast(PWR_UNIT_NM)),
        .reserved = @as(c_uint, 0),
    };
    return c;
}

pub fn motor(arg_id: u32, arg_a: u32, arg_heat: u32, arg_resistance: f64, arg_inductance: f64, arg_k: f64, arg_voltage: f64) callconv(.c) pwr_component_desc {
    var id = arg_id;
    _ = &id;
    var a = arg_a;
    _ = &a;
    var heat = arg_heat;
    _ = &heat;
    var resistance = arg_resistance;
    _ = &resistance;
    var inductance = arg_inductance;
    _ = &inductance;
    var k = arg_k;
    _ = &k;
    var voltage = arg_voltage;
    _ = &voltage;
    var c: pwr_component_desc = pwr_component_desc{
        .id = @as(u32, @bitCast(@as(c_int, 0))),
        .kind = @import("std").mem.zeroes(u32),
        .node_a = @import("std").mem.zeroes(u32),
        .node_b = @import("std").mem.zeroes(u32),
        .heat_node = @import("std").mem.zeroes(u32),
        .reserved = @import("std").mem.zeroes(u32),
        .input_channel = @import("std").mem.zeroes(u64),
        .initial_input = @import("std").mem.zeroes(pwr_quantity),
        .parameters = @import("std").mem.zeroes(pwr_component_parameters),
    };
    _ = &c;
    c.id = id;
    c.kind = @as(u32, @bitCast(PWR_COMPONENT_DC_MOTOR));
    c.node_a = a;
    c.heat_node = heat;
    c.input_channel = @as(u64, @bitCast(@as(u64, id)));
    c.initial_input = pwr_quantity{
        .value = voltage,
        .unit = @as(u32, @bitCast(PWR_UNIT_V)),
        .reserved = @as(c_uint, 0),
    };
    c.parameters.motor.resistance = pwr_quantity{
        .value = resistance,
        .unit = @as(u32, @bitCast(PWR_UNIT_OHM)),
        .reserved = @as(c_uint, 0),
    };
    c.parameters.motor.inductance = pwr_quantity{
        .value = inductance,
        .unit = @as(u32, @bitCast(PWR_UNIT_H)),
        .reserved = @as(c_uint, 0),
    };
    c.parameters.motor.coupling = pwr_quantity{
        .value = k,
        .unit = @as(u32, @bitCast(PWR_UNIT_NM_A)),
        .reserved = @as(c_uint, 0),
    };
    c.parameters.motor.initial_current = pwr_quantity{
        .value = @as(f64, @floatFromInt(@as(c_int, 0))),
        .unit = @as(u32, @bitCast(PWR_UNIT_A)),
        .reserved = @as(c_uint, 0),
    };
    return c;
}

pub fn link(arg_id: u32, arg_a: u32, arg_b: u32, arg_g: f64, arg_ambient: f64) callconv(.c) pwr_component_desc {
    var id = arg_id;
    _ = &id;
    var a = arg_a;
    _ = &a;
    var b = arg_b;
    _ = &b;
    var g = arg_g;
    _ = &g;
    var ambient = arg_ambient;
    _ = &ambient;
    var c: pwr_component_desc = pwr_component_desc{
        .id = @as(u32, @bitCast(@as(c_int, 0))),
        .kind = @import("std").mem.zeroes(u32),
        .node_a = @import("std").mem.zeroes(u32),
        .node_b = @import("std").mem.zeroes(u32),
        .heat_node = @import("std").mem.zeroes(u32),
        .reserved = @import("std").mem.zeroes(u32),
        .input_channel = @import("std").mem.zeroes(u64),
        .initial_input = @import("std").mem.zeroes(pwr_quantity),
        .parameters = @import("std").mem.zeroes(pwr_component_parameters),
    };
    _ = &c;
    c.id = id;
    c.kind = @as(u32, @bitCast(PWR_COMPONENT_THERMAL_LINK));
    c.node_a = a;
    c.node_b = b;
    c.parameters.thermal.conductance = pwr_quantity{
        .value = g,
        .unit = @as(u32, @bitCast(PWR_UNIT_W_K)),
        .reserved = @as(c_uint, 0),
    };
    c.parameters.thermal.ambient_temperature = pwr_quantity{
        .value = ambient,
        .unit = @as(u32, @bitCast(if (b == @as(c_uint, 0)) PWR_UNIT_K else PWR_UNIT_NONE)),
        .reserved = @as(c_uint, 0),
    };
    return c;
}

pub fn compile(arg_g: [*c]pwr_graph, arg_nodes: [*c]const pwr_node_desc, arg_nn: u32, arg_components: [*c]const pwr_component_desc, arg_nc: u32, arg_step_ns: u64, arg_diagnostic: [*c]pwr_model_diagnostic) callconv(.c) pwr_status {
    var g = arg_g;
    _ = &g;
    var nodes = arg_nodes;
    _ = &nodes;
    var nn = arg_nn;
    _ = &nn;
    var components = arg_components;
    _ = &components;
    var nc = arg_nc;
    _ = &nc;
    var step_ns = arg_step_ns;
    _ = &step_ns;
    var diagnostic = arg_diagnostic;
    _ = &diagnostic;
    var desc: pwr_model_desc = pwr_model_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_desc))))),
        .schema_version = @as(c_uint, 1),
        .flags = @as(u32, @bitCast(@as(c_int, 0))),
        .step_ns = @as(u64, 100000),
        .nodes = @as([*c]const pwr_node_desc, @ptrFromInt(@as(c_int, 0))),
        .node_count = @as(u32, @bitCast(@as(c_int, 0))),
        .component_count = @as(u32, @bitCast(@as(c_int, 0))),
        .components = @as([*c]const pwr_component_desc, @ptrFromInt(@as(c_int, 0))),
    };
    _ = &desc;
    desc.nodes = nodes;
    desc.node_count = nn;
    desc.components = components;
    desc.component_count = nc;
    desc.step_ns = step_ns;
    return pwr_graph_compile(&desc, g, diagnostic);
}

pub fn value(arg_g: [*c]const pwr_graph, arg_s: [*c]const pwr_graph_state, arg_object: u32, arg_field: u32) callconv(.c) f64 {
    var g = arg_g;
    _ = &g;
    var s = arg_s;
    _ = &s;
    var object = arg_object;
    _ = &object;
    var field = arg_field;
    _ = &field;
    var values: [260]pwr_scalar_value = undefined;
    _ = &values;
    var snapshot: pwr_snapshot = pwr_snapshot{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_snapshot))))),
        .simulation_time_ns = @as(u64, 0),
        .state_hash = @as(u64, 0),
        .diagnostic_bits = @as(u64, 0),
        .values = @as([*c]pwr_scalar_value, @ptrFromInt(@as(c_int, 0))),
        .value_capacity = @as(c_uint, 0),
        .value_count = @as(c_uint, 0),
    };
    _ = &snapshot;
    snapshot.values = @as([*c]pwr_scalar_value, @ptrCast(@alignCast(&values[@as(usize, @intCast(0))])));
    snapshot.value_capacity = ((@as(c_uint, 2) *% @as(c_uint, 32)) +% (@as(c_uint, 3) *% @as(c_uint, 64))) +% @as(c_uint, 4);
    while (true) {
        if (!(pwr_graph_snapshot(g, s, &snapshot) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 96), "pwr_graph_snapshot(g, s, &snapshot) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < snapshot.value_count) : (i +%= 1) {
            if (values[i].channel == ((@as(u64, 9223372036854775808) | (@as(u64, @bitCast(@as(u64, object))) << @intCast(8))) | @as(u64, @bitCast(@as(u64, field))))) {
                return values[i].value;
            }
        }
    }
    while (true) {
        if (!(@as(c_int, 0) != 0)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 100), "false" });
            failures += 1;
        }
        if (!false) break;
    }
    return @as(f64, @floatCast(__builtin_nanf("")));
}

pub fn test_constant_torque_and_replay(arg_g: [*c]pwr_graph) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    const n: pwr_node_desc = rotor(@as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 2))), @as(f64, @floatFromInt(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 0))));
    _ = &n;
    const c: pwr_component_desc = source(@as(u32, @bitCast(@as(c_int, 10))), @as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 4))));
    _ = &c;
    while (true) {
        if (!(compile(g, &n, @as(u32, @bitCast(@as(c_int, 1))), &c, @as(u32, @bitCast(@as(c_int, 1))), @as(u64, 1000000), null) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 108), "compile(g, &n, 1, &c, 1, UINT64_C(1000000), NULL) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    var a: pwr_graph_state = undefined;
    _ = &a;
    var b: pwr_graph_state = undefined;
    _ = &b;
    while (true) {
        if (!(pwr_graph_init(g, &a) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 110), "pwr_graph_init(g, &a) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    b = a;
    while (true) {
        if (!(pwr_graph_step(g, &a, @as(u64, 2000000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 112), "pwr_graph_step(g, &a, UINT64_C(2000000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    {
        var i: u32 = 0;
        _ = &i;
        while (i < @as(c_uint, 2000)) : (i +%= 1) {
            while (true) {
                if (!(pwr_graph_step(g, &b, @as(u64, 1000000)) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 114), "pwr_graph_step(g, &b, UINT64_C(1000000)) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
        }
    }
    while (true) {
        if (!(pwr_graph_state_hash(g, &a) == pwr_graph_state_hash(g, &b))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 116), "pwr_graph_state_hash(g, &a) == pwr_graph_state_hash(g, &b)" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &a, @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(PWR_FIELD_SPEED))) - 4.0) < 0.00000000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 117), "fabs(value(g, &a, 1, PWR_FIELD_SPEED) - 4.0) < 1.0e-11" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &a, @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(PWR_FIELD_ANGLE))) - 4.0) < 0.00000000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 118), "fabs(value(g, &a, 1, PWR_FIELD_ANGLE) - 4.0) < 1.0e-11" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &a, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_SOURCE_WORK))) - 16.0) < 0.0000000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 119), "fabs(value(g, &a, 0, PWR_FIELD_SOURCE_WORK) - 16.0) < 1.0e-10" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &a, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_ENERGY_RESIDUAL)))) < 0.0000000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 120), "fabs(value(g, &a, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-10" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_oscillator_convergence_and_conservation(arg_g: [*c]pwr_graph) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    const n: pwr_node_desc = rotor(@as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 2))), 0.5, @as(f64, @floatFromInt(@as(c_int, 0))));
    _ = &n;
    const c: pwr_component_desc = shaft(@as(u32, @bitCast(@as(c_int, 10))), @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 8))), @as(f64, @floatFromInt(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 1))));
    _ = &c;
    var errors: [3]f64 = undefined;
    _ = &errors;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < @as(c_uint, 3)) : (i +%= 1) {
            const step_ns: u64 = @as(u64, 40000000) >> @intCast(i);
            _ = &step_ns;
            while (true) {
                if (!(compile(g, &n, @as(u32, @bitCast(@as(c_int, 1))), &c, @as(u32, @bitCast(@as(c_int, 1))), step_ns, null) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 130), "compile(g, &n, 1, &c, 1, step_ns, NULL) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
            var s: pwr_graph_state = undefined;
            _ = &s;
            while (true) {
                if (!(pwr_graph_init(g, &s) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 132), "pwr_graph_init(g, &s) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
            while (true) {
                if (!(pwr_graph_step(g, &s, @as(u64, 1000000000)) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 133), "pwr_graph_step(g, &s, UINT64_C(1000000000)) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
            errors[i] = fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(PWR_FIELD_ANGLE))) - (0.5 * cos(2.0)));
            while (true) {
                if (!(fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_ENERGY_RESIDUAL)))) < 0.000000000001)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 135), "fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-12" });
                    failures += 1;
                }
                if (!false) break;
            }
        }
    }
    while (true) {
        if (!(((errors[@as(c_uint, @intCast(@as(c_int, 0)))] / errors[@as(c_uint, @intCast(@as(c_int, 1)))]) > 3.9) and ((errors[@as(c_uint, @intCast(@as(c_int, 0)))] / errors[@as(c_uint, @intCast(@as(c_int, 1)))]) < 4.1))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 137), "errors[0] / errors[1] > 3.9 && errors[0] / errors[1] < 4.1" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(((errors[@as(c_uint, @intCast(@as(c_int, 1)))] / errors[@as(c_uint, @intCast(@as(c_int, 2)))]) > 3.9) and ((errors[@as(c_uint, @intCast(@as(c_int, 1)))] / errors[@as(c_uint, @intCast(@as(c_int, 2)))]) < 4.1))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 138), "errors[1] / errors[2] > 3.9 && errors[1] / errors[2] < 4.1" });
            failures += 1;
        }
        if (!false) break;
    }
    var s: pwr_graph_state = undefined;
    _ = &s;
    while (true) {
        if (!(pwr_graph_init(g, &s) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 140), "pwr_graph_init(g, &s) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &s, @as(u64, 1000000000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 141), "pwr_graph_step(g, &s, UINT64_C(1000000000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_ENERGY_RESIDUAL)))) < 0.000000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 142), "fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-9" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_signed_transmission_and_heat(arg_g: [*c]pwr_graph) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    const nodes: [3]pwr_node_desc = [3]pwr_node_desc{
        rotor(@as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 1))), 0.2, @as(f64, @floatFromInt(@as(c_int, 2)))),
        rotor(@as(u32, @bitCast(@as(c_int, 2))), @as(f64, @floatFromInt(@as(c_int, 3))), -0.1, @as(f64, @floatFromInt(-@as(c_int, 1)))),
        thermal(@as(u32, @bitCast(@as(c_int, 3))), @as(f64, @floatFromInt(@as(c_int, 10))), @as(f64, @floatFromInt(@as(c_int, 300)))),
    };
    _ = &nodes;
    {
        var trial: u32 = 0;
        _ = &trial;
        while (trial < @as(c_uint, 2)) : (trial +%= 1) {
            const ratio: f64 = if (trial == @as(c_uint, 0)) 2.0 else -2.0;
            _ = &ratio;
            var c: pwr_component_desc = shaft(@as(u32, @bitCast(@as(c_int, 10))), @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(@as(c_int, 2))), @as(f64, @floatFromInt(@as(c_int, 40))), 0.8, ratio);
            _ = &c;
            c.heat_node = 3;
            c.parameters.shaft.rest_angle.value = 0.07;
            while (true) {
                if (!(compile(g, @as([*c]const pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 3))), &c, @as(u32, @bitCast(@as(c_int, 1))), @as(u64, 1000000), null) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 153), "compile(g, nodes, 3, &c, 1, UINT64_C(1000000), NULL) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
            var s: pwr_graph_state = undefined;
            _ = &s;
            while (true) {
                if (!(pwr_graph_init(g, &s) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 155), "pwr_graph_init(g, &s) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
            while (true) {
                if (!(pwr_graph_step(g, &s, @as(u64, 10000000000)) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 156), "pwr_graph_step(g, &s, UINT64_C(10000000000)) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
            const momentum: f64 = (ratio * value(g, &s, @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(PWR_FIELD_SPEED)))) + (@as(f64, @floatFromInt(@as(c_int, 3))) * value(g, &s, @as(u32, @bitCast(@as(c_int, 2))), @as(u32, @bitCast(PWR_FIELD_SPEED))));
            _ = &momentum;
            while (true) {
                if (!(fabs(momentum - ((ratio * 2.0) - 3.0)) < 0.0000000001)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 158), "fabs(momentum - (ratio * 2.0 - 3.0)) < 1.0e-10" });
                    failures += 1;
                }
                if (!false) break;
            }
            while (true) {
                if (!(value(g, &s, @as(u32, @bitCast(@as(c_int, 3))), @as(u32, @bitCast(PWR_FIELD_TEMPERATURE))) > 300.0)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 159), "value(g, &s, 3, PWR_FIELD_TEMPERATURE) > 300.0" });
                    failures += 1;
                }
                if (!false) break;
            }
            while (true) {
                if (!(value(g, &s, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_HEAT_REJECTED))) == 0.0)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 160), "value(g, &s, 0, PWR_FIELD_HEAT_REJECTED) == 0.0" });
                    failures += 1;
                }
                if (!false) break;
            }
            while (true) {
                if (!(fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_ENERGY_RESIDUAL)))) < 0.0000001)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 161), "fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-7" });
                    failures += 1;
                }
                if (!false) break;
            }
        }
    }
}

pub fn test_electrical_analytic(arg_g: [*c]pwr_graph) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    const n: pwr_node_desc = rotor(@as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 0))));
    _ = &n;
    const c: pwr_component_desc = motor(@as(u32, @bitCast(@as(c_int, 10))), @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 2))), 0.5, @as(f64, @floatFromInt(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 12))));
    _ = &c;
    while (true) {
        if (!(compile(g, &n, @as(u32, @bitCast(@as(c_int, 1))), &c, @as(u32, @bitCast(@as(c_int, 1))), @as(u64, 1000000), null) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 169), "compile(g, &n, 1, &c, 1, UINT64_C(1000000), NULL) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    var s: pwr_graph_state = undefined;
    _ = &s;
    while (true) {
        if (!(pwr_graph_init(g, &s) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 171), "pwr_graph_init(g, &s) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &s, @as(u64, 500000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 172), "pwr_graph_step(g, &s, UINT64_C(500000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 10))), @as(u32, @bitCast(PWR_FIELD_CURRENT))) - (6.0 * (1.0 - exp(-2.0)))) < 0.000003)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 173), "fabs(value(g, &s, 10, PWR_FIELD_CURRENT) - 6.0 * (1.0 - exp(-2.0))) < 3.0e-6" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(value(g, &s, @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(PWR_FIELD_SPEED))) == 0.0)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 174), "value(g, &s, 1, PWR_FIELD_SPEED) == 0.0" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_ENERGY_RESIDUAL)))) < 0.0000000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 175), "fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-10" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_thermal_network(arg_g: [*c]pwr_graph) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    const nodes: [2]pwr_node_desc = [2]pwr_node_desc{
        thermal(@as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 10))), @as(f64, @floatFromInt(@as(c_int, 400)))),
        thermal(@as(u32, @bitCast(@as(c_int, 2))), @as(f64, @floatFromInt(@as(c_int, 30))), @as(f64, @floatFromInt(@as(c_int, 300)))),
    };
    _ = &nodes;
    const c: pwr_component_desc = link(@as(u32, @bitCast(@as(c_int, 10))), @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(@as(c_int, 2))), @as(f64, @floatFromInt(@as(c_int, 100))), @as(f64, @floatFromInt(@as(c_int, 0))));
    _ = &c;
    while (true) {
        if (!(compile(g, @as([*c]const pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), &c, @as(u32, @bitCast(@as(c_int, 1))), @as(u64, 1000000000), null) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 182), "compile(g, nodes, 2, &c, 1, UINT64_C(1000000000), NULL) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    var s: pwr_graph_state = undefined;
    _ = &s;
    while (true) {
        if (!(pwr_graph_init(g, &s) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 184), "pwr_graph_init(g, &s) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &s, @as(u64, 20000000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 185), "pwr_graph_step(g, &s, UINT64_C(20000000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(PWR_FIELD_TEMPERATURE))) - @as(f64, @floatFromInt(@as(c_int, 325)))) < 0.000000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 186), "fabs(value(g, &s, 1, PWR_FIELD_TEMPERATURE) - 325) < 1.0e-9" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 2))), @as(u32, @bitCast(PWR_FIELD_TEMPERATURE))) - @as(f64, @floatFromInt(@as(c_int, 325)))) < 0.000000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 187), "fabs(value(g, &s, 2, PWR_FIELD_TEMPERATURE) - 325) < 1.0e-9" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_ENERGY_RESIDUAL)))) < 0.00000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 188), "fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-8" });
            failures += 1;
        }
        if (!false) break;
    }
    const ambient: pwr_component_desc = link(@as(u32, @bitCast(@as(c_int, 10))), @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 10))), @as(f64, @floatFromInt(@as(c_int, 300))));
    _ = &ambient;
    var errors: [2]f64 = undefined;
    _ = &errors;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < @as(c_uint, 2)) : (i +%= 1) {
            while (true) {
                if (!(compile(g, @as([*c]const pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 1))), &ambient, @as(u32, @bitCast(@as(c_int, 1))), @as(u64, 10000000) >> @intCast(i), null) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 193), "compile(g, nodes, 1, &ambient, 1, UINT64_C(10000000) >> i, NULL) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
            while (true) {
                if (!(pwr_graph_init(g, &s) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 194), "pwr_graph_init(g, &s) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
            while (true) {
                if (!(pwr_graph_step(g, &s, @as(u64, 1000000000)) == PWR_STATUS_OK)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 195), "pwr_graph_step(g, &s, UINT64_C(1000000000)) == PWR_STATUS_OK" });
                    failures += 1;
                }
                if (!false) break;
            }
            errors[i] = fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(PWR_FIELD_TEMPERATURE))) - (@as(f64, @floatFromInt(@as(c_int, 300))) + (@as(f64, @floatFromInt(@as(c_int, 100))) * exp(-1.0))));
            while (true) {
                if (!(fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_ENERGY_RESIDUAL)))) < 0.00000001)) {
                    _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 197), "fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-8" });
                    failures += 1;
                }
                if (!false) break;
            }
        }
    }
    while (true) {
        if (!(((errors[@as(c_uint, @intCast(@as(c_int, 0)))] / errors[@as(c_uint, @intCast(@as(c_int, 1)))]) > 1.98) and ((errors[@as(c_uint, @intCast(@as(c_int, 0)))] / errors[@as(c_uint, @intCast(@as(c_int, 1)))]) < 2.02))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 199), "errors[0] / errors[1] > 1.98 && errors[0] / errors[1] < 2.02" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_coupled_motor_and_regeneration(arg_g: [*c]pwr_graph) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    const nodes: [4]pwr_node_desc = [4]pwr_node_desc{
        rotor(@as(u32, @bitCast(@as(c_int, 1))), 0.2, @as(f64, @floatFromInt(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 0)))),
        rotor(@as(u32, @bitCast(@as(c_int, 2))), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 0)))),
        thermal(@as(u32, @bitCast(@as(c_int, 3))), @as(f64, @floatFromInt(@as(c_int, 100))), @as(f64, @floatFromInt(@as(c_int, 300)))),
        thermal(@as(u32, @bitCast(@as(c_int, 4))), @as(f64, @floatFromInt(@as(c_int, 200))), @as(f64, @floatFromInt(@as(c_int, 300)))),
    };
    _ = &nodes;
    var cs: [5]pwr_component_desc = [5]pwr_component_desc{
        motor(@as(u32, @bitCast(@as(c_int, 10))), @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(@as(c_int, 3))), 0.5, 0.02, 0.8, @as(f64, @floatFromInt(@as(c_int, 24)))),
        shaft(@as(u32, @bitCast(@as(c_int, 11))), @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(@as(c_int, 2))), @as(f64, @floatFromInt(@as(c_int, 100))), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 3)))),
        shaft(@as(u32, @bitCast(@as(c_int, 12))), @as(u32, @bitCast(@as(c_int, 2))), @as(u32, @bitCast(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 0))), 0.1, @as(f64, @floatFromInt(@as(c_int, 1)))),
        link(@as(u32, @bitCast(@as(c_int, 13))), @as(u32, @bitCast(@as(c_int, 3))), @as(u32, @bitCast(@as(c_int, 4))), @as(f64, @floatFromInt(@as(c_int, 5))), @as(f64, @floatFromInt(@as(c_int, 0)))),
        link(@as(u32, @bitCast(@as(c_int, 14))), @as(u32, @bitCast(@as(c_int, 4))), @as(u32, @bitCast(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 2))), @as(f64, @floatFromInt(@as(c_int, 300)))),
    };
    _ = &cs;
    cs[@as(c_uint, @intCast(@as(c_int, 1)))].heat_node = 4;
    while (true) {
        if (!(compile(g, @as([*c]const pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 4))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 5))), @as(u64, 1000000), null) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 210), "compile(g, nodes, 4, cs, 5, UINT64_C(1000000), NULL) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    var s: pwr_graph_state = undefined;
    _ = &s;
    while (true) {
        if (!(pwr_graph_init(g, &s) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 212), "pwr_graph_init(g, &s) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &s, @as(u64, 20000000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 213), "pwr_graph_step(g, &s, UINT64_C(20000000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(value(g, &s, @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(PWR_FIELD_SPEED))) > 20.0)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 214), "value(g, &s, 1, PWR_FIELD_SPEED) > 20.0" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(value(g, &s, @as(u32, @bitCast(@as(c_int, 2))), @as(u32, @bitCast(PWR_FIELD_SPEED))) > 7.0)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 215), "value(g, &s, 2, PWR_FIELD_SPEED) > 7.0" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(value(g, &s, @as(u32, @bitCast(@as(c_int, 3))), @as(u32, @bitCast(PWR_FIELD_TEMPERATURE))) > 300.0)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 216), "value(g, &s, 3, PWR_FIELD_TEMPERATURE) > 300.0" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(value(g, &s, @as(u32, @bitCast(@as(c_int, 4))), @as(u32, @bitCast(PWR_FIELD_TEMPERATURE))) > 300.0)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 217), "value(g, &s, 4, PWR_FIELD_TEMPERATURE) > 300.0" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_ENERGY_RESIDUAL)))) < 0.000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 218), "fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-6" });
            failures += 1;
        }
        if (!false) break;
    }
    const work_before: f64 = s.source_work;
    _ = &work_before;
    var voltage: pwr_scalar_value = pwr_scalar_value{
        .channel = @as(pwr_channel_id, @bitCast(@as(i64, @as(c_int, 10)))),
        .value = @as(f64, @floatFromInt(@as(c_int, 4))),
        .flags = @as(u32, @bitCast(@as(c_int, 0))),
        .reserved = @as(u32, @bitCast(@as(c_int, 0))),
    };
    _ = &voltage;
    var frame: pwr_input_frame = pwr_input_frame{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_input_frame))))),
        .values = @as([*c]const pwr_scalar_value, @ptrFromInt(@as(c_int, 0))),
        .value_count = @as(c_uint, 0),
        .flags = @as(c_uint, 0),
    };
    _ = &frame;
    frame.values = &voltage;
    frame.value_count = 1;
    while (true) {
        if (!(pwr_graph_submit(g, &s, &frame) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 223), "pwr_graph_submit(g, &s, &frame) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &s, @as(u64, 500000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 224), "pwr_graph_step(g, &s, UINT64_C(500000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(value(g, &s, @as(u32, @bitCast(@as(c_int, 10))), @as(u32, @bitCast(PWR_FIELD_CURRENT))) < 0.0)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 225), "value(g, &s, 10, PWR_FIELD_CURRENT) < 0.0" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(s.source_work < work_before)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 226), "s.source_work < work_before" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &s, @as(u32, @bitCast(@as(c_int, 0))), @as(u32, @bitCast(PWR_FIELD_ENERGY_RESIDUAL)))) < 0.000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 227), "fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-6" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_canonicalization(arg_g: [*c]pwr_graph) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    var nodes: [3]pwr_node_desc = [3]pwr_node_desc{
        rotor(@as(u32, @bitCast(@as(c_int, 5))), @as(f64, @floatFromInt(@as(c_int, 2))), @as(f64, @floatFromInt(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 0)))),
        rotor(@as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 0)))),
        thermal(@as(u32, @bitCast(@as(c_int, 3))), @as(f64, @floatFromInt(@as(c_int, 10))), @as(f64, @floatFromInt(@as(c_int, 300)))),
    };
    _ = &nodes;
    var cs: [2]pwr_component_desc = [2]pwr_component_desc{
        shaft(@as(u32, @bitCast(@as(c_int, 20))), @as(u32, @bitCast(@as(c_int, 5))), @as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 40))), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(-@as(c_int, 2)))),
        source(@as(u32, @bitCast(@as(c_int, 10))), @as(u32, @bitCast(@as(c_int, 5))), @as(f64, @floatFromInt(@as(c_int, 3)))),
    };
    _ = &cs;
    cs[@as(c_uint, @intCast(@as(c_int, 0)))].heat_node = 3;
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 3))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as(u64, 1000000), null) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 235), "compile(g, nodes, 3, cs, 2, UINT64_C(1000000), NULL) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    const fingerprint: u64 = g.*.fingerprint;
    _ = &fingerprint;
    var a: pwr_graph_state = undefined;
    _ = &a;
    var b: pwr_graph_state = undefined;
    _ = &b;
    while (true) {
        if (!(pwr_graph_init(g, &a) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 238), "pwr_graph_init(g, &a) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &a, @as(u64, 1000000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 239), "pwr_graph_step(g, &a, UINT64_C(1000000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    const hash: u64 = pwr_graph_state_hash(g, &a);
    _ = &hash;
    const tmpn: pwr_node_desc = nodes[@as(c_uint, @intCast(@as(c_int, 0)))];
    _ = &tmpn;
    nodes[@as(c_uint, @intCast(@as(c_int, 0)))] = nodes[@as(c_uint, @intCast(@as(c_int, 2)))];
    nodes[@as(c_uint, @intCast(@as(c_int, 2)))] = tmpn;
    const tmpc: pwr_component_desc = cs[@as(c_uint, @intCast(@as(c_int, 0)))];
    _ = &tmpc;
    cs[@as(c_uint, @intCast(@as(c_int, 0)))] = cs[@as(c_uint, @intCast(@as(c_int, 1)))];
    cs[@as(c_uint, @intCast(@as(c_int, 1)))] = tmpc;
    nodes[@as(c_uint, @intCast(@as(c_int, 1)))].position = pwr_quantity{
        .value = -0.0,
        .unit = @as(u32, @bitCast(PWR_UNIT_DEG)),
        .reserved = @as(c_uint, 0),
    };
    nodes[@as(c_uint, @intCast(@as(c_int, 1)))].initial.unit = @as(u32, @bitCast(PWR_UNIT_RPM));
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 3))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as(u64, 1000000), null) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 245), "compile(g, nodes, 3, cs, 2, UINT64_C(1000000), NULL) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(g.*.fingerprint == fingerprint)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 246), "g->fingerprint == fingerprint" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_init(g, &b) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 247), "pwr_graph_init(g, &b) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &b, @as(u64, 1000000000)) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 248), "pwr_graph_step(g, &b, UINT64_C(1000000000)) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_state_hash(g, &b) == hash)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 249), "pwr_graph_state_hash(g, &b) == hash" });
            failures += 1;
        }
        if (!false) break;
    }
    cs[@as(c_uint, @intCast(@as(c_int, 0)))].initial_input.value = 4;
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 3))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as(u64, 1000000), null) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 251), "compile(g, nodes, 3, cs, 2, UINT64_C(1000000), NULL) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(g.*.fingerprint != fingerprint)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 252), "g->fingerprint != fingerprint" });
            failures += 1;
        }
        if (!false) break;
    }
    var rpm: pwr_node_desc = rotor(@as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 60))));
    _ = &rpm;
    rpm.initial.unit = @as(u32, @bitCast(PWR_UNIT_RPM));
    while (true) {
        if (!(compile(g, &rpm, @as(u32, @bitCast(@as(c_int, 1))), null, @as(u32, @bitCast(@as(c_int, 0))), @as(u64, 1000000), null) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 256), "compile(g, &rpm, 1, NULL, 0, UINT64_C(1000000), NULL) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_init(g, &b) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 257), "pwr_graph_init(g, &b) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(fabs(value(g, &b, @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(PWR_FIELD_SPEED))) - (2.0 * acos(-1.0))) < 0.00000000000001)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 258), "fabs(value(g, &b, 1, PWR_FIELD_SPEED) - 2.0 * acos(-1.0)) < 1.0e-14" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_rejection_and_atomicity(arg_g: [*c]pwr_graph) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    var nodes: [2]pwr_node_desc = [2]pwr_node_desc{
        rotor(@as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 2))), @as(f64, @floatFromInt(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 0)))),
        thermal(@as(u32, @bitCast(@as(c_int, 2))), @as(f64, @floatFromInt(@as(c_int, 10))), @as(f64, @floatFromInt(@as(c_int, 300)))),
    };
    _ = &nodes;
    var cs: [2]pwr_component_desc = [2]pwr_component_desc{
        source(@as(u32, @bitCast(@as(c_int, 10))), @as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 4)))),
        shaft(@as(u32, @bitCast(@as(c_int, 11))), @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 1)))),
    };
    _ = &cs;
    var d: pwr_model_diagnostic = pwr_model_diagnostic{
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
    _ = &d;
    nodes[@as(c_uint, @intCast(@as(c_int, 0)))].storage.unit = @as(u32, @bitCast(PWR_UNIT_NM));
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as(u64, @bitCast(@as(i64, @as(c_int, 1000000)))), &d) == PWR_STATUS_INVALID_MODEL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 267), "compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(((d.code == @as(u32, @bitCast(PWR_MODEL_DIAG_UNIT))) and (d.object_id == @as(c_uint, 1))) and (strcmp(@as([*c]u8, @ptrCast(@alignCast(&d.field[@as(usize, @intCast(0))]))), "storage") == @as(c_int, 0)))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 268), "d.code == PWR_MODEL_DIAG_UNIT && d.object_id == 1U && strcmp(d.field, \"storage\") == 0" });
            failures += 1;
        }
        if (!false) break;
    }
    nodes[@as(c_uint, @intCast(@as(c_int, 0)))].storage.unit = @as(u32, @bitCast(PWR_UNIT_KG_M2));
    nodes[@as(c_uint, @intCast(@as(c_int, 0)))].storage.value = @as(f64, @floatCast(__builtin_nanf("")));
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as(u64, @bitCast(@as(i64, @as(c_int, 1000000)))), &d) == PWR_STATUS_INVALID_MODEL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 271), "compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL" });
            failures += 1;
        }
        if (!false) break;
    }
    nodes[@as(c_uint, @intCast(@as(c_int, 0)))].storage.value = 0;
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as(u64, @bitCast(@as(i64, @as(c_int, 1000000)))), &d) == PWR_STATUS_INVALID_MODEL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 273), "compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL" });
            failures += 1;
        }
        if (!false) break;
    }
    nodes[@as(c_uint, @intCast(@as(c_int, 0)))].storage.value = 2;
    cs[@as(c_uint, @intCast(@as(c_int, 1)))].node_b = 2;
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as(u64, @bitCast(@as(i64, @as(c_int, 1000000)))), &d) == PWR_STATUS_INVALID_MODEL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 276), "compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((d.code == @as(u32, @bitCast(PWR_MODEL_DIAG_CONNECTION))) and (d.object_id == @as(c_uint, 11)))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 277), "d.code == PWR_MODEL_DIAG_CONNECTION && d.object_id == 11U" });
            failures += 1;
        }
        if (!false) break;
    }
    cs[@as(c_uint, @intCast(@as(c_int, 1)))].node_b = @as(u32, @bitCast(@as(c_int, 999)));
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as(u64, @bitCast(@as(i64, @as(c_int, 1000000)))), &d) == PWR_STATUS_INVALID_MODEL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 279), "compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL" });
            failures += 1;
        }
        if (!false) break;
    }
    cs[@as(c_uint, @intCast(@as(c_int, 1)))].node_b = 0;
    cs[@as(c_uint, @intCast(@as(c_int, 1)))].id = 10;
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as(u64, @bitCast(@as(i64, @as(c_int, 1000000)))), &d) == PWR_STATUS_INVALID_MODEL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 282), "compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(d.code == @as(u32, @bitCast(PWR_MODEL_DIAG_ID)))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 283), "d.code == PWR_MODEL_DIAG_ID" });
            failures += 1;
        }
        if (!false) break;
    }
    cs[@as(c_uint, @intCast(@as(c_int, 1)))] = source(@as(u32, @bitCast(@as(c_int, 11))), @as(u32, @bitCast(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 0))));
    cs[@as(c_uint, @intCast(@as(c_int, 1)))].input_channel = 10;
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as(u64, @bitCast(@as(i64, @as(c_int, 1000000)))), &d) == PWR_STATUS_INVALID_MODEL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 285), "compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(d.code == @as(u32, @bitCast(PWR_MODEL_DIAG_CHANNEL)))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 286), "d.code == PWR_MODEL_DIAG_CHANNEL" });
            failures += 1;
        }
        if (!false) break;
    }
    cs[@as(c_uint, @intCast(@as(c_int, 1)))].input_channel = 11;
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as([*c]pwr_component_desc, @ptrCast(@alignCast(&cs[@as(usize, @intCast(0))]))), @as(u32, @bitCast(@as(c_int, 2))), @as(u64, @bitCast(@as(i64, @as(c_int, 1000000)))), &d) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 288), "compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!((d.code == @as(u32, @bitCast(PWR_MODEL_DIAG_NONE))) and (@as(c_int, @bitCast(@as(c_uint, d.field[@as(c_uint, @intCast(@as(c_int, 0)))]))) == @as(c_int, '\x00')))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 289), "d.code == PWR_MODEL_DIAG_NONE && d.field[0] == '\\0'" });
            failures += 1;
        }
        if (!false) break;
    }
    var s: pwr_graph_state = undefined;
    _ = &s;
    while (true) {
        if (!(pwr_graph_init(g, &s) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 291), "pwr_graph_init(g, &s) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    const initial_hash: u64 = pwr_graph_state_hash(g, &s);
    _ = &initial_hash;
    var values: [2]pwr_scalar_value = [2]pwr_scalar_value{
        pwr_scalar_value{
            .channel = @as(pwr_channel_id, @bitCast(@as(i64, @as(c_int, 10)))),
            .value = @as(f64, @floatFromInt(@as(c_int, 100))),
            .flags = @as(u32, @bitCast(@as(c_int, 0))),
            .reserved = @as(u32, @bitCast(@as(c_int, 0))),
        },
        pwr_scalar_value{
            .channel = @as(pwr_channel_id, @bitCast(@as(i64, @as(c_int, 99)))),
            .value = @as(f64, @floatFromInt(@as(c_int, 0))),
            .flags = @as(u32, @bitCast(@as(c_int, 0))),
            .reserved = @as(u32, @bitCast(@as(c_int, 0))),
        },
    };
    _ = &values;
    var frame: pwr_input_frame = pwr_input_frame{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_input_frame))))),
        .values = @as([*c]const pwr_scalar_value, @ptrFromInt(@as(c_int, 0))),
        .value_count = @as(c_uint, 0),
        .flags = @as(c_uint, 0),
    };
    _ = &frame;
    frame.values = @as([*c]pwr_scalar_value, @ptrCast(@alignCast(&values[@as(usize, @intCast(0))])));
    frame.value_count = 2;
    while (true) {
        if (!(pwr_graph_submit(g, &s, &frame) == PWR_STATUS_UNKNOWN_CHANNEL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 296), "pwr_graph_submit(g, &s, &frame) == PWR_STATUS_UNKNOWN_CHANNEL" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_state_hash(g, &s) == initial_hash)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 297), "pwr_graph_state_hash(g, &s) == initial_hash" });
            failures += 1;
        }
        if (!false) break;
    }
    values[@as(c_uint, @intCast(@as(c_int, 1)))].channel = 10;
    while (true) {
        if (!(pwr_graph_submit(g, &s, &frame) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 299), "pwr_graph_submit(g, &s, &frame) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_state_hash(g, &s) == initial_hash)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 300), "pwr_graph_state_hash(g, &s) == initial_hash" });
            failures += 1;
        }
        if (!false) break;
    }
    values[@as(c_uint, @intCast(@as(c_int, 1)))].channel = 11;
    values[@as(c_uint, @intCast(@as(c_int, 1)))].value = @as(f64, @floatCast(__builtin_inff()));
    while (true) {
        if (!(pwr_graph_submit(g, &s, &frame) == PWR_STATUS_INVALID_ARGUMENT)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 302), "pwr_graph_submit(g, &s, &frame) == PWR_STATUS_INVALID_ARGUMENT" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_state_hash(g, &s) == initial_hash)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 303), "pwr_graph_state_hash(g, &s) == initial_hash" });
            failures += 1;
        }
        if (!false) break;
    }
    frame.value_count = 1;
    values[@as(c_uint, @intCast(@as(c_int, 0)))].value = 179769313486231570000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000.0;
    while (true) {
        if (!(pwr_graph_submit(g, &s, &frame) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 306), "pwr_graph_submit(g, &s, &frame) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    const submitted_hash: u64 = pwr_graph_state_hash(g, &s);
    _ = &submitted_hash;
    while (true) {
        if (!(submitted_hash != initial_hash)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 308), "submitted_hash != initial_hash" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &s, @as(u64, 2000000)) == PWR_STATUS_NUMERIC_ERROR)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 309), "pwr_graph_step(g, &s, UINT64_C(2000000)) == PWR_STATUS_NUMERIC_ERROR" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_state_hash(g, &s) == submitted_hash)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 310), "pwr_graph_state_hash(g, &s) == submitted_hash" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(s.time_ns == @as(u64, @bitCast(@as(u64, @as(c_uint, 0)))))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 311), "s.time_ns == 0U" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &s, @as(u64, @bitCast(@as(i64, @as(c_int, 0))))) == PWR_STATUS_INVALID_TIME_STEP)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 312), "pwr_graph_step(g, &s, 0) == PWR_STATUS_INVALID_TIME_STEP" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &s, @as(u64, @bitCast(@as(i64, @as(c_int, 3))))) == PWR_STATUS_INVALID_TIME_STEP)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 313), "pwr_graph_step(g, &s, 3) == PWR_STATUS_INVALID_TIME_STEP" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &s, @as(u64, 1000001000000)) == PWR_STATUS_INVALID_TIME_STEP)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 314), "pwr_graph_step(g, &s, UINT64_C(1000001000000)) == PWR_STATUS_INVALID_TIME_STEP" });
            failures += 1;
        }
        if (!false) break;
    }
    s.time_ns = @as(u64, 18446744073709551615) -% @as(u64, @bitCast(@as(u64, @as(c_uint, 10))));
    while (true) {
        if (!(pwr_graph_step(g, &s, @as(u64, @bitCast(@as(i64, @as(c_int, 1000000))))) == PWR_STATUS_INVALID_TIME_STEP)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 316), "pwr_graph_step(g, &s, 1000000) == PWR_STATUS_INVALID_TIME_STEP" });
            failures += 1;
        }
        if (!false) break;
    }
    var canary: pwr_scalar_value = pwr_scalar_value{
        .channel = @as(pwr_channel_id, @bitCast(@as(i64, @as(c_int, 123)))),
        .value = @as(f64, @floatFromInt(@as(c_int, 456))),
        .flags = @as(u32, @bitCast(@as(c_int, 789))),
        .reserved = @as(u32, @bitCast(@as(c_int, 101))),
    };
    _ = &canary;
    var snapshot: pwr_snapshot = pwr_snapshot{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_snapshot))))),
        .simulation_time_ns = @as(u64, 0),
        .state_hash = @as(u64, 0),
        .diagnostic_bits = @as(u64, 0),
        .values = @as([*c]pwr_scalar_value, @ptrFromInt(@as(c_int, 0))),
        .value_capacity = @as(c_uint, 0),
        .value_count = @as(c_uint, 0),
    };
    _ = &snapshot;
    snapshot.values = &canary;
    snapshot.value_capacity = 1;
    while (true) {
        if (!(pwr_graph_snapshot(g, &s, &snapshot) == PWR_STATUS_CAPACITY_EXCEEDED)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 321), "pwr_graph_snapshot(g, &s, &snapshot) == PWR_STATUS_CAPACITY_EXCEEDED" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(((snapshot.value_count == g.*.output_count) and (canary.channel == @as(pwr_channel_id, @bitCast(@as(u64, @as(c_uint, 123)))))) and (canary.value == @as(f64, @floatFromInt(@as(c_int, 456)))))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 322), "snapshot.value_count == g->output_count && canary.channel == 123U && canary.value == 456" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn test_capacity_and_schema(arg_g: [*c]pwr_graph) callconv(.c) void {
    var g = arg_g;
    _ = &g;
    var nodes: [32]pwr_node_desc = undefined;
    _ = &nodes;
    {
        var i: u32 = 0;
        _ = &i;
        while (i < @as(c_uint, 32)) : (i +%= 1) {
            nodes[i] = rotor(i +% @as(c_uint, 1), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 0))));
        }
    }
    const c: pwr_component_desc = motor(@as(u32, @bitCast(@as(c_int, 100))), @as(u32, @bitCast(@as(c_int, 1))), @as(u32, @bitCast(@as(c_int, 0))), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 1))), @as(f64, @floatFromInt(@as(c_int, 1))));
    _ = &c;
    var diagnostic: pwr_model_diagnostic = pwr_model_diagnostic{
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
    _ = &diagnostic;
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(c_uint, 32), &c, @as(u32, @bitCast(@as(c_int, 1))), @as(u64, @bitCast(@as(i64, @as(c_int, 1000000)))), &diagnostic) == PWR_STATUS_CAPACITY_EXCEEDED)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 331), "compile(g, nodes, PWR_MODEL_MAX_NODES, &c, 1, 1000000, &diagnostic) == PWR_STATUS_CAPACITY_EXCEEDED" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(diagnostic.code == @as(u32, @bitCast(PWR_MODEL_DIAG_CAPACITY)))) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 332), "diagnostic.code == PWR_MODEL_DIAG_CAPACITY" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(c_uint, 32) +% @as(c_uint, 1), &c, @as(u32, @bitCast(@as(c_int, 1))), @as(u64, @bitCast(@as(i64, @as(c_int, 1000000)))), null) == PWR_STATUS_CAPACITY_EXCEEDED)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 333), "compile(g, nodes, PWR_MODEL_MAX_NODES + 1U, &c, 1, 1000000, NULL) == PWR_STATUS_CAPACITY_EXCEEDED" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(compile(g, @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))]))), @as(c_uint, 32), null, @as(u32, @bitCast(@as(c_int, 0))), @as(u64, @bitCast(@as(i64, @as(c_int, 1000000)))), null) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 334), "compile(g, nodes, PWR_MODEL_MAX_NODES, NULL, 0, 1000000, NULL) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    var s: pwr_graph_state = undefined;
    _ = &s;
    while (true) {
        if (!(pwr_graph_init(g, &s) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 336), "pwr_graph_init(g, &s) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_graph_step(g, &s, @as(u64, @bitCast(@as(i64, @as(c_int, 1000000))))) == PWR_STATUS_OK)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 337), "pwr_graph_step(g, &s, 1000000) == PWR_STATUS_OK" });
            failures += 1;
        }
        if (!false) break;
    }
    var desc: pwr_model_desc = pwr_model_desc{
        .abi_version = @as(c_uint, 1),
        .struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_desc))))),
        .schema_version = @as(c_uint, 1),
        .flags = @as(u32, @bitCast(@as(c_int, 0))),
        .step_ns = @as(u64, 100000),
        .nodes = @as([*c]const pwr_node_desc, @ptrFromInt(@as(c_int, 0))),
        .node_count = @as(u32, @bitCast(@as(c_int, 0))),
        .component_count = @as(u32, @bitCast(@as(c_int, 0))),
        .components = @as([*c]const pwr_component_desc, @ptrFromInt(@as(c_int, 0))),
    };
    _ = &desc;
    desc.nodes = @as([*c]pwr_node_desc, @ptrCast(@alignCast(&nodes[@as(usize, @intCast(0))])));
    desc.node_count = 1;
    desc.schema_version = @as(c_uint, 4294967295);
    while (true) {
        if (!(pwr_graph_compile(&desc, g, &diagnostic) == PWR_STATUS_UNSUPPORTED)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 340), "pwr_graph_compile(&desc, g, &diagnostic) == PWR_STATUS_UNSUPPORTED" });
            failures += 1;
        }
        if (!false) break;
    }
    desc.schema_version = 1;
    desc.struct_size = 8;
    while (true) {
        if (!(pwr_graph_compile(&desc, g, &diagnostic) == PWR_STATUS_STRUCT_TOO_SMALL)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 342), "pwr_graph_compile(&desc, g, &diagnostic) == PWR_STATUS_STRUCT_TOO_SMALL" });
            failures += 1;
        }
        if (!false) break;
    }
    desc.struct_size = @as(u32, @bitCast(@as(c_uint, @truncate(@sizeOf(pwr_model_desc)))));
    desc.abi_version = 99;
    while (true) {
        if (!(pwr_graph_compile(&desc, g, &diagnostic) == PWR_STATUS_UNSUPPORTED_ABI)) {
            _ = fprintf("{s}:{d}: {s}\n", .{ "legacy/native/tests/test_graph.c", @as(c_int, 344), "pwr_graph_compile(&desc, g, &diagnostic) == PWR_STATUS_UNSUPPORTED_ABI" });
            failures += 1;
        }
        if (!false) break;
    }
}

pub fn run() c_int {
    var g: [*c]pwr_graph = @as([*c]pwr_graph, @ptrCast(@alignCast(malloc(@sizeOf(pwr_graph)))));
    _ = &g;
    if (g == null) {
        return 1;
    }
    test_constant_torque_and_replay(g);
    test_oscillator_convergence_and_conservation(g);
    test_signed_transmission_and_heat(g);
    test_electrical_analytic(g);
    test_thermal_network(g);
    test_coupled_motor_and_regeneration(g);
    test_canonicalization(g);
    test_rejection_and_atomicity(g);
    test_capacity_and_schema(g);
    free(@as(?*anyopaque, @ptrCast(g)));
    if (failures != @as(c_int, 0)) {
        _ = fprintf("{d} graph checks failed\n", .{failures});
        return 1;
    }
    _ = puts("Power! compiled graph physics tests passed");
    return 0;
}

pub fn main() void {
    @import("std").process.exit(@intCast(run()));
}
