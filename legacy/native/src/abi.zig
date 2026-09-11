// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Native binary layouts and prototype model types. See docs/NATIVE_ZIG.md.

pub const pwr_model = u64;

pub const PWR_UNIT_NONE: c_int = 0;

pub const PWR_UNIT_KG_M2: c_int = 1;

pub const PWR_UNIT_RAD: c_int = 2;

pub const PWR_UNIT_RAD_S: c_int = 3;

pub const PWR_UNIT_NM: c_int = 4;

pub const PWR_UNIT_NM_RAD: c_int = 5;

pub const PWR_UNIT_NM_S_RAD: c_int = 6;

pub const PWR_UNIT_K: c_int = 7;

pub const PWR_UNIT_J_K: c_int = 8;

pub const PWR_UNIT_W_K: c_int = 9;

pub const PWR_UNIT_OHM: c_int = 10;

pub const PWR_UNIT_H: c_int = 11;

pub const PWR_UNIT_NM_A: c_int = 12;

pub const PWR_UNIT_A: c_int = 13;

pub const PWR_UNIT_V: c_int = 14;

pub const PWR_UNIT_J: c_int = 15;

pub const PWR_UNIT_RPM: c_int = 16;

pub const PWR_UNIT_DEG: c_int = 17;

pub const struct_pwr_quantity = extern struct {
    value: f64 = @import("std").mem.zeroes(f64),
    unit: u32 = @import("std").mem.zeroes(u32),
    reserved: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_quantity = struct_pwr_quantity;

pub const PWR_NODE_ROTATIONAL: c_int = 1;

pub const PWR_NODE_THERMAL: c_int = 2;

pub const struct_pwr_node_desc = extern struct {
    id: u32 = @import("std").mem.zeroes(u32),
    domain: u32 = @import("std").mem.zeroes(u32),
    storage: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
    initial: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
    position: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
};

pub const pwr_node_desc = struct_pwr_node_desc;

pub const PWR_COMPONENT_SHAFT: c_int = 1;

pub const PWR_COMPONENT_DC_MOTOR: c_int = 2;

pub const PWR_COMPONENT_TORQUE_SOURCE: c_int = 3;

pub const PWR_COMPONENT_THERMAL_LINK: c_int = 4;

pub const pwr_shaft_parameters = extern struct {
    stiffness: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
    damping: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
    rest_angle: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
    ratio: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_motor_parameters = extern struct {
    resistance: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
    inductance: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
    coupling: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
    initial_current: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
};

pub const pwr_thermal_parameters = extern struct {
    conductance: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
    ambient_temperature: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
};

pub const union_pwr_component_parameters = extern union {
    shaft: pwr_shaft_parameters,
    motor: pwr_motor_parameters,
    thermal: pwr_thermal_parameters,
};

pub const pwr_component_parameters = union_pwr_component_parameters;

pub const struct_pwr_component_desc = extern struct {
    id: u32 = @import("std").mem.zeroes(u32),
    kind: u32 = @import("std").mem.zeroes(u32),
    node_a: u32 = @import("std").mem.zeroes(u32),
    node_b: u32 = @import("std").mem.zeroes(u32),
    heat_node: u32 = @import("std").mem.zeroes(u32),
    reserved: u32 = @import("std").mem.zeroes(u32),
    input_channel: u64 = @import("std").mem.zeroes(u64),
    initial_input: pwr_quantity = @import("std").mem.zeroes(pwr_quantity),
    parameters: pwr_component_parameters = @import("std").mem.zeroes(pwr_component_parameters),
};

pub const pwr_component_desc = struct_pwr_component_desc;

pub const struct_pwr_model_desc = extern struct {
    abi_version: u32 = @import("std").mem.zeroes(u32),
    struct_size: u32 = @import("std").mem.zeroes(u32),
    schema_version: u32 = @import("std").mem.zeroes(u32),
    flags: u32 = @import("std").mem.zeroes(u32),
    step_ns: u64 = @import("std").mem.zeroes(u64),
    nodes: [*c]const pwr_node_desc = @import("std").mem.zeroes([*c]const pwr_node_desc),
    node_count: u32 = @import("std").mem.zeroes(u32),
    component_count: u32 = @import("std").mem.zeroes(u32),
    components: [*c]const pwr_component_desc = @import("std").mem.zeroes([*c]const pwr_component_desc),
};

pub const pwr_model_desc = struct_pwr_model_desc;

pub const PWR_MODEL_DIAG_NONE: c_int = 0;

pub const PWR_MODEL_DIAG_SCHEMA: c_int = 1;

pub const PWR_MODEL_DIAG_CAPACITY: c_int = 2;

pub const PWR_MODEL_DIAG_ID: c_int = 3;

pub const PWR_MODEL_DIAG_UNIT: c_int = 4;

pub const PWR_MODEL_DIAG_RANGE: c_int = 5;

pub const PWR_MODEL_DIAG_CONNECTION: c_int = 6;

pub const PWR_MODEL_DIAG_CHANNEL: c_int = 7;

pub const PWR_MODEL_DIAG_SOLVER: c_int = 8;

pub const struct_pwr_model_diagnostic = extern struct {
    abi_version: u32 = @import("std").mem.zeroes(u32),
    struct_size: u32 = @import("std").mem.zeroes(u32),
    code: u32 = @import("std").mem.zeroes(u32),
    object_id: u32 = @import("std").mem.zeroes(u32),
    field: [40]u8 = @import("std").mem.zeroes([40]u8),
    message: [160]u8 = @import("std").mem.zeroes([160]u8),
};

pub const pwr_model_diagnostic = struct_pwr_model_diagnostic;

pub const PWR_MODEL_FIDELITY_LINEAR_LUMPED: c_int = 1;

pub const PWR_MODEL_VALIDATION_UNVERIFIED: c_int = 0;

pub const struct_pwr_model_info = extern struct {
    abi_version: u32 = @import("std").mem.zeroes(u32),
    struct_size: u32 = @import("std").mem.zeroes(u32),
    fingerprint: u64 = @import("std").mem.zeroes(u64),
    step_ns: u64 = @import("std").mem.zeroes(u64),
    node_count: u32 = @import("std").mem.zeroes(u32),
    component_count: u32 = @import("std").mem.zeroes(u32),
    state_count: u32 = @import("std").mem.zeroes(u32),
    channel_count: u32 = @import("std").mem.zeroes(u32),
    fidelity: u32 = @import("std").mem.zeroes(u32),
    validation: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_model_info = struct_pwr_model_info;

pub const PWR_CHANNEL_INPUT: c_int = 1;

pub const PWR_CHANNEL_OUTPUT: c_int = 2;

pub const PWR_FIELD_ANGLE: c_int = 1;

pub const PWR_FIELD_SPEED: c_int = 2;

pub const PWR_FIELD_TEMPERATURE: c_int = 3;

pub const PWR_FIELD_CURRENT: c_int = 4;

pub const PWR_FIELD_TWIST: c_int = 5;

pub const PWR_FIELD_TORQUE: c_int = 6;

pub const PWR_FIELD_SOURCE_WORK: c_int = 16;

pub const PWR_FIELD_HEAT_REJECTED: c_int = 17;

pub const PWR_FIELD_STORED_ENERGY_CHANGE: c_int = 18;

pub const PWR_FIELD_ENERGY_RESIDUAL: c_int = 19;

pub const struct_pwr_channel_info = extern struct {
    channel: u64 = @import("std").mem.zeroes(u64),
    object_id: u32 = @import("std").mem.zeroes(u32),
    direction: u32 = @import("std").mem.zeroes(u32),
    unit: u32 = @import("std").mem.zeroes(u32),
    reserved: u32 = @import("std").mem.zeroes(u32),
    quantity: [32]u8 = @import("std").mem.zeroes([32]u8),
};

pub const pwr_channel_info = struct_pwr_channel_info;

pub const pwr_status = i32;

pub const PWR_STATUS_OK: c_int = 0;

pub const PWR_STATUS_INVALID_ARGUMENT: c_int = -1;

pub const PWR_STATUS_UNSUPPORTED_ABI: c_int = -2;

pub const PWR_STATUS_STRUCT_TOO_SMALL: c_int = -3;

pub const PWR_STATUS_INVALID_HANDLE: c_int = -4;

pub const PWR_STATUS_CAPACITY_EXCEEDED: c_int = -5;

pub const PWR_STATUS_UNSUPPORTED: c_int = -6;

pub const PWR_STATUS_INTERNAL_ERROR: c_int = -7;

pub const PWR_STATUS_IN_USE: c_int = -8;

pub const PWR_STATUS_BUSY: c_int = -9;

pub const PWR_STATUS_INVALID_TIME_STEP: c_int = -10;

pub const PWR_STATUS_UNKNOWN_CHANNEL: c_int = -11;

pub const PWR_STATUS_INVALID_MODEL: c_int = -12;

pub const PWR_STATUS_NUMERIC_ERROR: c_int = -13;

pub const PWR_STATUS_OUT_OF_MEMORY: c_int = -14;

pub const pwr_context = u64;

pub const pwr_instance = u64;

pub const struct_pwr_context_desc = extern struct {
    abi_version: u32 = @import("std").mem.zeroes(u32),
    struct_size: u32 = @import("std").mem.zeroes(u32),
    flags: u32 = @import("std").mem.zeroes(u32),
    reserved: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_context_desc = struct_pwr_context_desc;

pub const struct_pwr_version_info = extern struct {
    abi_version: u32 = @import("std").mem.zeroes(u32),
    struct_size: u32 = @import("std").mem.zeroes(u32),
    major: u32 = @import("std").mem.zeroes(u32),
    minor: u32 = @import("std").mem.zeroes(u32),
    patch: u32 = @import("std").mem.zeroes(u32),
    reserved: u32 = @import("std").mem.zeroes(u32),
    product_name: [*c]const u8 = @import("std").mem.zeroes([*c]const u8),
    version_string: [*c]const u8 = @import("std").mem.zeroes([*c]const u8),
};

pub const pwr_version_info = struct_pwr_version_info;

pub const struct_pwr_capabilities = extern struct {
    abi_version: u32 = @import("std").mem.zeroes(u32),
    struct_size: u32 = @import("std").mem.zeroes(u32),
    feature_bits: u64 = @import("std").mem.zeroes(u64),
    max_contexts: u32 = @import("std").mem.zeroes(u32),
    reserved: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_capabilities = struct_pwr_capabilities;

pub const PWR_BUILTIN_MODEL_CONTROLLED_SHAFT: c_int = 1;

pub const enum_pwr_builtin_model = c_uint;

pub const pwr_builtin_model = enum_pwr_builtin_model;

pub const struct_pwr_instance_desc = extern struct {
    abi_version: u32 = @import("std").mem.zeroes(u32),
    struct_size: u32 = @import("std").mem.zeroes(u32),
    model: u32 = @import("std").mem.zeroes(u32),
    flags: u32 = @import("std").mem.zeroes(u32),
    seed: u64 = @import("std").mem.zeroes(u64),
};

pub const pwr_instance_desc = struct_pwr_instance_desc;

pub const pwr_channel_id = u64;

pub const struct_pwr_scalar_value = extern struct {
    channel: pwr_channel_id = @import("std").mem.zeroes(pwr_channel_id),
    value: f64 = @import("std").mem.zeroes(f64),
    flags: u32 = @import("std").mem.zeroes(u32),
    reserved: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_scalar_value = struct_pwr_scalar_value;

pub const struct_pwr_input_frame = extern struct {
    abi_version: u32 = @import("std").mem.zeroes(u32),
    struct_size: u32 = @import("std").mem.zeroes(u32),
    values: [*c]const pwr_scalar_value = @import("std").mem.zeroes([*c]const pwr_scalar_value),
    value_count: u32 = @import("std").mem.zeroes(u32),
    flags: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_input_frame = struct_pwr_input_frame;

pub const struct_pwr_snapshot = extern struct {
    abi_version: u32 = @import("std").mem.zeroes(u32),
    struct_size: u32 = @import("std").mem.zeroes(u32),
    simulation_time_ns: u64 = @import("std").mem.zeroes(u64),
    state_hash: u64 = @import("std").mem.zeroes(u64),
    diagnostic_bits: u64 = @import("std").mem.zeroes(u64),
    values: [*c]pwr_scalar_value = @import("std").mem.zeroes([*c]pwr_scalar_value),
    value_capacity: u32 = @import("std").mem.zeroes(u32),
    value_count: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_snapshot = struct_pwr_snapshot;

pub const pwr_runtime_get_version_fn = ?*const fn ([*c]pwr_version_info) callconv(.c) pwr_status;

pub const pwr_runtime_get_capabilities_fn = ?*const fn ([*c]pwr_capabilities) callconv(.c) pwr_status;

pub const pwr_context_create_fn = ?*const fn ([*c]const pwr_context_desc, [*c]pwr_context) callconv(.c) pwr_status;

pub const pwr_context_destroy_fn = ?*const fn (pwr_context) callconv(.c) pwr_status;

pub const pwr_status_string_fn = ?*const fn (pwr_status) callconv(.c) [*c]const u8;

pub const pwr_instance_create_fn = ?*const fn (pwr_context, [*c]const pwr_instance_desc, [*c]pwr_instance) callconv(.c) pwr_status;

pub const pwr_instance_destroy_fn = ?*const fn (pwr_instance) callconv(.c) pwr_status;

pub const pwr_instance_submit_inputs_fn = ?*const fn (pwr_instance, [*c]const pwr_input_frame) callconv(.c) pwr_status;

pub const pwr_instance_step_fn = ?*const fn (pwr_instance, u64) callconv(.c) pwr_status;

pub const pwr_instance_read_snapshot_fn = ?*const fn (pwr_instance, [*c]pwr_snapshot) callconv(.c) pwr_status;

pub const pwr_model_compile_fn = ?*const fn (pwr_context, [*c]const pwr_model_desc, [*c]pwr_model, [*c]pwr_model_diagnostic) callconv(.c) pwr_status;

pub const pwr_model_destroy_fn = ?*const fn (pwr_model) callconv(.c) pwr_status;

pub const pwr_model_get_info_fn = ?*const fn (pwr_model, [*c]pwr_model_info) callconv(.c) pwr_status;

pub const pwr_model_get_channels_fn = ?*const fn (pwr_model, [*c]pwr_channel_info, u32, [*c]u32) callconv(.c) pwr_status;

pub const pwr_instance_create_from_model_fn = ?*const fn (pwr_model, [*c]pwr_instance) callconv(.c) pwr_status;

pub const struct_pwr_api = extern struct {
    abi_version: u32 = @import("std").mem.zeroes(u32),
    struct_size: u32 = @import("std").mem.zeroes(u32),
    runtime_get_version: pwr_runtime_get_version_fn = @import("std").mem.zeroes(pwr_runtime_get_version_fn),
    runtime_get_capabilities: pwr_runtime_get_capabilities_fn = @import("std").mem.zeroes(pwr_runtime_get_capabilities_fn),
    context_create: pwr_context_create_fn = @import("std").mem.zeroes(pwr_context_create_fn),
    context_destroy: pwr_context_destroy_fn = @import("std").mem.zeroes(pwr_context_destroy_fn),
    status_string: pwr_status_string_fn = @import("std").mem.zeroes(pwr_status_string_fn),
    instance_create: pwr_instance_create_fn = @import("std").mem.zeroes(pwr_instance_create_fn),
    instance_destroy: pwr_instance_destroy_fn = @import("std").mem.zeroes(pwr_instance_destroy_fn),
    instance_submit_inputs: pwr_instance_submit_inputs_fn = @import("std").mem.zeroes(pwr_instance_submit_inputs_fn),
    instance_step: pwr_instance_step_fn = @import("std").mem.zeroes(pwr_instance_step_fn),
    instance_read_snapshot: pwr_instance_read_snapshot_fn = @import("std").mem.zeroes(pwr_instance_read_snapshot_fn),
    model_compile: pwr_model_compile_fn = @import("std").mem.zeroes(pwr_model_compile_fn),
    model_destroy: pwr_model_destroy_fn = @import("std").mem.zeroes(pwr_model_destroy_fn),
    model_get_info: pwr_model_get_info_fn = @import("std").mem.zeroes(pwr_model_get_info_fn),
    model_get_channels: pwr_model_get_channels_fn = @import("std").mem.zeroes(pwr_model_get_channels_fn),
    instance_create_from_model: pwr_instance_create_from_model_fn = @import("std").mem.zeroes(pwr_instance_create_from_model_fn),
};

pub const pwr_api = struct_pwr_api;

pub const PWR_BUILD_VERSION = "0.2.0-zig";

pub const struct_pwr_graph_node = extern struct {
    id: u32 = @import("std").mem.zeroes(u32),
    domain: u32 = @import("std").mem.zeroes(u32),
    state_index: u32 = @import("std").mem.zeroes(u32),
    storage: f64 = @import("std").mem.zeroes(f64),
    initial: f64 = @import("std").mem.zeroes(f64),
    position: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_graph_node = struct_pwr_graph_node;

pub const struct_pwr_graph_component = extern struct {
    id: u32 = @import("std").mem.zeroes(u32),
    kind: u32 = @import("std").mem.zeroes(u32),
    a: u32 = @import("std").mem.zeroes(u32),
    b: u32 = @import("std").mem.zeroes(u32),
    heat: u32 = @import("std").mem.zeroes(u32),
    state_index: u32 = @import("std").mem.zeroes(u32),
    input_channel: u64 = @import("std").mem.zeroes(u64),
    initial_input: f64 = @import("std").mem.zeroes(f64),
    p: [4]f64 = @import("std").mem.zeroes([4]f64),
};

pub const pwr_graph_component = struct_pwr_graph_component;

pub const struct_pwr_graph_matrix = extern struct {
    size: u32 = @import("std").mem.zeroes(u32),
    pivots: [64]u32 = @import("std").mem.zeroes([64]u32),
    lu: [64][64]f64 = @import("std").mem.zeroes([64][64]f64),
};

pub const pwr_graph_matrix = struct_pwr_graph_matrix;

pub const struct_pwr_graph = extern struct {
    step_ns: u64 = @import("std").mem.zeroes(u64),
    fingerprint: u64 = @import("std").mem.zeroes(u64),
    dt: f64 = @import("std").mem.zeroes(f64),
    node_count: u32 = @import("std").mem.zeroes(u32),
    component_count: u32 = @import("std").mem.zeroes(u32),
    dynamic_count: u32 = @import("std").mem.zeroes(u32),
    thermal_count: u32 = @import("std").mem.zeroes(u32),
    channel_count: u32 = @import("std").mem.zeroes(u32),
    output_count: u32 = @import("std").mem.zeroes(u32),
    nodes: [32]pwr_graph_node = @import("std").mem.zeroes([32]pwr_graph_node),
    components: [64]pwr_graph_component = @import("std").mem.zeroes([64]pwr_graph_component),
    dynamics: pwr_graph_matrix = @import("std").mem.zeroes(pwr_graph_matrix),
    thermal: pwr_graph_matrix = @import("std").mem.zeroes(pwr_graph_matrix),
    constant_force: [64]f64 = @import("std").mem.zeroes([64]f64),
    ambient_force: [32]f64 = @import("std").mem.zeroes([32]f64),
    channels: [260]pwr_channel_info = @import("std").mem.zeroes([260]pwr_channel_info),
};

pub const pwr_graph = struct_pwr_graph;

pub const struct_pwr_graph_state = extern struct {
    time_ns: u64 = @import("std").mem.zeroes(u64),
    x: [64]f64 = @import("std").mem.zeroes([64]f64),
    temperature: [32]f64 = @import("std").mem.zeroes([32]f64),
    inputs: [64]f64 = @import("std").mem.zeroes([64]f64),
    initial_energy: f64 = @import("std").mem.zeroes(f64),
    source_work: f64 = @import("std").mem.zeroes(f64),
    source_work_compensation: f64 = @import("std").mem.zeroes(f64),
    heat_rejected: f64 = @import("std").mem.zeroes(f64),
    heat_compensation: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_graph_state = struct_pwr_graph_state;

pub const PWR_GRAPH_MAX_CHANNELS = ((@as(c_uint, 2) * PWR_MODEL_MAX_NODES) + (@as(c_uint, 3) * PWR_MODEL_MAX_COMPONENTS)) + @as(c_uint, 4);

pub const PWR_GRAPH_NO_INDEX = @import("std").math.maxInt(u32);

pub const pwr_task_callback = ?*const fn (?*anyopaque, u64) callconv(.c) bool;

pub const PWR_SCHEDULER_OK: c_int = 0;

pub const PWR_SCHEDULER_INVALID_ARGUMENT: c_int = 1;

pub const PWR_SCHEDULER_CAPACITY_EXCEEDED: c_int = 2;

pub const PWR_SCHEDULER_DUPLICATE_ID: c_int = 3;

pub const PWR_SCHEDULER_CALLBACK_FAILED: c_int = 4;

pub const PWR_SCHEDULER_FAULTED: c_int = 5;

pub const enum_pwr_scheduler_result = c_uint;

pub const pwr_scheduler_result = enum_pwr_scheduler_result;

pub const struct_pwr_task_desc = extern struct {
    stable_id: u32 = @import("std").mem.zeroes(u32),
    period_ticks: u32 = @import("std").mem.zeroes(u32),
    phase_ticks: u32 = @import("std").mem.zeroes(u32),
    priority: u16 = @import("std").mem.zeroes(u16),
    callback: pwr_task_callback = @import("std").mem.zeroes(pwr_task_callback),
    user: ?*anyopaque = @import("std").mem.zeroes(?*anyopaque),
};

pub const pwr_task_desc = struct_pwr_task_desc;

pub const struct_pwr_scheduled_task = extern struct {
    desc: pwr_task_desc = @import("std").mem.zeroes(pwr_task_desc),
    run_count: u64 = @import("std").mem.zeroes(u64),
    last_run_tick: u64 = @import("std").mem.zeroes(u64),
};

pub const pwr_scheduled_task = struct_pwr_scheduled_task;

pub const struct_pwr_scheduler = extern struct {
    base_tick_ns: u64 = @import("std").mem.zeroes(u64),
    current_tick: u64 = @import("std").mem.zeroes(u64),
    task_count: usize = @import("std").mem.zeroes(usize),
    faulted: bool = @import("std").mem.zeroes(bool),
    failed_task_id: u32 = @import("std").mem.zeroes(u32),
    tasks: [64]pwr_scheduled_task = @import("std").mem.zeroes([64]pwr_scheduled_task),
};

pub const pwr_scheduler = struct_pwr_scheduler;

pub const PWR_SCHEDULER_MAX_TASKS = @as(c_uint, 64);

pub const PWR_AUTOMATIC_OK: c_int = 0;

pub const PWR_AUTOMATIC_INVALID_ARGUMENT: c_int = 1;

pub const PWR_AUTOMATIC_INVALID_CONFIG: c_int = 2;

pub const PWR_AUTOMATIC_INVALID_INPUT: c_int = 3;

pub const PWR_AUTOMATIC_NUMERIC_ERROR: c_int = 4;

pub const enum_pwr_automatic_result = c_uint;

pub const pwr_automatic_result = enum_pwr_automatic_result;

pub const PWR_AUTOMATIC_GEAR_REVERSE: c_int = -1;

pub const PWR_AUTOMATIC_GEAR_NEUTRAL: c_int = 0;

pub const PWR_AUTOMATIC_GEAR_FIRST: c_int = 1;

pub const PWR_AUTOMATIC_GEAR_SECOND: c_int = 2;

pub const PWR_AUTOMATIC_GEAR_THIRD: c_int = 3;

pub const PWR_AUTOMATIC_GEAR_FOURTH: c_int = 4;

pub const enum_pwr_automatic_gear = c_int;

pub const pwr_automatic_gear = enum_pwr_automatic_gear;

pub const PWR_AUTOMATIC_DIAG_NONE: c_int = 0;

pub const PWR_AUTOMATIC_DIAG_LOW_LINE_PRESSURE: c_int = 1;

pub const PWR_AUTOMATIC_DIAG_SHIFT_TIMEOUT: c_int = 2;

pub const PWR_AUTOMATIC_DIAG_EXCESSIVE_RATIO_ERROR: c_int = 4;

pub const PWR_AUTOMATIC_DIAG_EXCESSIVE_LOCKUP_SLIP: c_int = 8;

pub const PWR_AUTOMATIC_DIAG_OIL_OVERTEMPERATURE: c_int = 16;

pub const PWR_AUTOMATIC_DIAG_LOCKUP_OVERTEMPERATURE: c_int = 32;

pub const PWR_AUTOMATIC_DIAG_OVERSPEED: c_int = 64;

pub const PWR_AUTOMATIC_DIAG_NUMERIC_LIMIT: c_int = 128;

pub const PWR_AUTOMATIC_DIAG_COUPLING_SPEED_PROJECTED: c_int = 256;

pub const enum_pwr_automatic_diagnostic = c_uint;

pub const pwr_automatic_diagnostic = enum_pwr_automatic_diagnostic;

pub const struct_pwr_automatic_config = extern struct {
    step_ns: u64 = @import("std").mem.zeroes(u64),
    forward_ratios: [4]f64 = @import("std").mem.zeroes([4]f64),
    reverse_ratio: f64 = @import("std").mem.zeroes(f64),
    pump_inertia_kg_m2: f64 = @import("std").mem.zeroes(f64),
    turbine_inertia_kg_m2: f64 = @import("std").mem.zeroes(f64),
    output_inertia_kg_m2: f64 = @import("std").mem.zeroes(f64),
    pump_viscous_drag_nm_s_rad: f64 = @import("std").mem.zeroes(f64),
    turbine_viscous_drag_nm_s_rad: f64 = @import("std").mem.zeroes(f64),
    output_viscous_drag_nm_s_rad: f64 = @import("std").mem.zeroes(f64),
    converter_k_rad_s_sqrt_nm: f64 = @import("std").mem.zeroes(f64),
    converter_stall_torque_ratio: f64 = @import("std").mem.zeroes(f64),
    converter_coupling_speed_ratio: f64 = @import("std").mem.zeroes(f64),
    converter_slip_smoothing_rad_s: f64 = @import("std").mem.zeroes(f64),
    nominal_line_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    maximum_line_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    minimum_drive_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    pressure_time_constant_s: f64 = @import("std").mem.zeroes(f64),
    maximum_gear_clutch_torque_nm: f64 = @import("std").mem.zeroes(f64),
    gear_clutch_slip_smoothing_rad_s: f64 = @import("std").mem.zeroes(f64),
    nominal_shift_duration_s: f64 = @import("std").mem.zeroes(f64),
    maximum_shift_duration_s: f64 = @import("std").mem.zeroes(f64),
    shift_torque_hole_fraction: f64 = @import("std").mem.zeroes(f64),
    maximum_lockup_torque_nm: f64 = @import("std").mem.zeroes(f64),
    lockup_apply_time_constant_s: f64 = @import("std").mem.zeroes(f64),
    lockup_release_time_constant_s: f64 = @import("std").mem.zeroes(f64),
    lockup_slip_smoothing_rad_s: f64 = @import("std").mem.zeroes(f64),
    ambient_temperature_k: f64 = @import("std").mem.zeroes(f64),
    initial_oil_temperature_k: f64 = @import("std").mem.zeroes(f64),
    initial_lockup_temperature_k: f64 = @import("std").mem.zeroes(f64),
    oil_thermal_capacity_j_k: f64 = @import("std").mem.zeroes(f64),
    oil_cooling_conductance_w_k: f64 = @import("std").mem.zeroes(f64),
    lockup_thermal_capacity_j_k: f64 = @import("std").mem.zeroes(f64),
    lockup_to_oil_conductance_w_k: f64 = @import("std").mem.zeroes(f64),
    lockup_heat_fraction: f64 = @import("std").mem.zeroes(f64),
    oil_overtemperature_k: f64 = @import("std").mem.zeroes(f64),
    lockup_overtemperature_k: f64 = @import("std").mem.zeroes(f64),
    hydraulic_base_loss_w: f64 = @import("std").mem.zeroes(f64),
    hydraulic_pressure_speed_loss_m3: f64 = @import("std").mem.zeroes(f64),
    rotating_loss_nm_s_rad: f64 = @import("std").mem.zeroes(f64),
    ratio_error_threshold_rad_s: f64 = @import("std").mem.zeroes(f64),
    ratio_error_delay_s: f64 = @import("std").mem.zeroes(f64),
    lockup_slip_threshold_rad_s: f64 = @import("std").mem.zeroes(f64),
    lockup_slip_delay_s: f64 = @import("std").mem.zeroes(f64),
    maximum_input_torque_nm: f64 = @import("std").mem.zeroes(f64),
    maximum_external_torque_nm: f64 = @import("std").mem.zeroes(f64),
    maximum_shaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_automatic_config = struct_pwr_automatic_config;

pub const struct_pwr_automatic_input = extern struct {
    engine_torque_nm: f64 = @import("std").mem.zeroes(f64),
    output_external_torque_nm: f64 = @import("std").mem.zeroes(f64),
    line_pressure_command_pa: f64 = @import("std").mem.zeroes(f64),
    lockup_command: f64 = @import("std").mem.zeroes(f64),
    requested_gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
};

pub const pwr_automatic_input = struct_pwr_automatic_input;

pub const struct_pwr_automatic_coupled_input = extern struct {
    pump_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    output_external_torque_nm: f64 = @import("std").mem.zeroes(f64),
    line_pressure_command_pa: f64 = @import("std").mem.zeroes(f64),
    lockup_command: f64 = @import("std").mem.zeroes(f64),
    requested_gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
};

pub const pwr_automatic_coupled_input = struct_pwr_automatic_coupled_input;

pub const struct_pwr_automatic_coupling_output = extern struct {
    engine_reaction_torque_nm: f64 = @import("std").mem.zeroes(f64),
    pump_load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    boundary_power_w: f64 = @import("std").mem.zeroes(f64),
    boundary_energy_j: f64 = @import("std").mem.zeroes(f64),
    projection_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    accumulated_projection_adjustment_j: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_automatic_coupling_output = struct_pwr_automatic_coupling_output;

pub const struct_pwr_automatic_converter_point = extern struct {
    speed_ratio: f64 = @import("std").mem.zeroes(f64),
    torque_ratio: f64 = @import("std").mem.zeroes(f64),
    pump_torque_nm: f64 = @import("std").mem.zeroes(f64),
    turbine_torque_nm: f64 = @import("std").mem.zeroes(f64),
    slip_power_w: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_automatic_converter_point = struct_pwr_automatic_converter_point;

pub const struct_pwr_automatic_snapshot = extern struct {
    time_s: f64 = @import("std").mem.zeroes(f64),
    pump_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    turbine_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    output_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    active_gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
    target_gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
    shift_active: bool = @import("std").mem.zeroes(bool),
    shift_progress: f64 = @import("std").mem.zeroes(f64),
    effective_ratio: f64 = @import("std").mem.zeroes(f64),
    gear_torque_factor: f64 = @import("std").mem.zeroes(f64),
    line_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    lockup_engagement: f64 = @import("std").mem.zeroes(f64),
    converter_speed_ratio: f64 = @import("std").mem.zeroes(f64),
    converter_torque_ratio: f64 = @import("std").mem.zeroes(f64),
    converter_pump_torque_nm: f64 = @import("std").mem.zeroes(f64),
    converter_turbine_torque_nm: f64 = @import("std").mem.zeroes(f64),
    lockup_torque_nm: f64 = @import("std").mem.zeroes(f64),
    gear_input_torque_nm: f64 = @import("std").mem.zeroes(f64),
    gear_output_torque_nm: f64 = @import("std").mem.zeroes(f64),
    converter_slip_rad_s: f64 = @import("std").mem.zeroes(f64),
    lockup_slip_rad_s: f64 = @import("std").mem.zeroes(f64),
    gear_clutch_slip_rad_s: f64 = @import("std").mem.zeroes(f64),
    converter_loss_w: f64 = @import("std").mem.zeroes(f64),
    lockup_loss_w: f64 = @import("std").mem.zeroes(f64),
    gear_clutch_loss_w: f64 = @import("std").mem.zeroes(f64),
    hydraulic_loss_w: f64 = @import("std").mem.zeroes(f64),
    rotating_loss_w: f64 = @import("std").mem.zeroes(f64),
    oil_temperature_k: f64 = @import("std").mem.zeroes(f64),
    lockup_temperature_k: f64 = @import("std").mem.zeroes(f64),
    accumulated_loss_j: f64 = @import("std").mem.zeroes(f64),
    pump_speed_imposed: bool = @import("std").mem.zeroes(bool),
    engine_reaction_torque_nm: f64 = @import("std").mem.zeroes(f64),
    pump_boundary_power_w: f64 = @import("std").mem.zeroes(f64),
    pump_boundary_energy_j: f64 = @import("std").mem.zeroes(f64),
    coupling_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    state_hash: u64 = @import("std").mem.zeroes(u64),
};

pub const pwr_automatic_snapshot = struct_pwr_automatic_snapshot;

pub const struct_pwr_automatic = extern struct {
    config: pwr_automatic_config = @import("std").mem.zeroes(pwr_automatic_config),
    tick: u64 = @import("std").mem.zeroes(u64),
    pump_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    turbine_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    output_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    active_gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
    shift_from_gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
    target_gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
    shift_active: bool = @import("std").mem.zeroes(bool),
    shift_from_ratio: f64 = @import("std").mem.zeroes(f64),
    shift_to_ratio: f64 = @import("std").mem.zeroes(f64),
    shift_progress: f64 = @import("std").mem.zeroes(f64),
    shift_elapsed_s: f64 = @import("std").mem.zeroes(f64),
    settled_elapsed_s: f64 = @import("std").mem.zeroes(f64),
    effective_ratio: f64 = @import("std").mem.zeroes(f64),
    gear_torque_factor: f64 = @import("std").mem.zeroes(f64),
    line_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    lockup_engagement: f64 = @import("std").mem.zeroes(f64),
    lockup_command_elapsed_s: f64 = @import("std").mem.zeroes(f64),
    oil_temperature_k: f64 = @import("std").mem.zeroes(f64),
    lockup_temperature_k: f64 = @import("std").mem.zeroes(f64),
    converter: pwr_automatic_converter_point = @import("std").mem.zeroes(pwr_automatic_converter_point),
    lockup_torque_nm: f64 = @import("std").mem.zeroes(f64),
    gear_input_torque_nm: f64 = @import("std").mem.zeroes(f64),
    gear_output_torque_nm: f64 = @import("std").mem.zeroes(f64),
    gear_clutch_slip_rad_s: f64 = @import("std").mem.zeroes(f64),
    converter_loss_w: f64 = @import("std").mem.zeroes(f64),
    lockup_loss_w: f64 = @import("std").mem.zeroes(f64),
    gear_clutch_loss_w: f64 = @import("std").mem.zeroes(f64),
    hydraulic_loss_w: f64 = @import("std").mem.zeroes(f64),
    rotating_loss_w: f64 = @import("std").mem.zeroes(f64),
    accumulated_loss_j: f64 = @import("std").mem.zeroes(f64),
    pump_speed_imposed: bool = @import("std").mem.zeroes(bool),
    engine_reaction_torque_nm: f64 = @import("std").mem.zeroes(f64),
    pump_boundary_power_w: f64 = @import("std").mem.zeroes(f64),
    pump_boundary_energy_j: f64 = @import("std").mem.zeroes(f64),
    coupling_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_automatic = struct_pwr_automatic;

pub const PWR_AUTOMATIC_FORWARD_GEAR_COUNT = @as(c_uint, 4);

pub const PWR_DCT_CLUTCH_COUNT: c_int = 2;

pub const PWR_DCT_GEAR_COUNT: c_int = 8;

pub const PWR_DCT_CLUTCH_INVALID: c_int = -1;

pub const PWR_DCT_CLUTCH_K1: c_int = 0;

pub const PWR_DCT_CLUTCH_K2: c_int = 1;

pub const enum_pwr_dct_clutch = c_int;

pub const pwr_dct_clutch = enum_pwr_dct_clutch;

pub const PWR_DCT_GEAR_NEUTRAL: c_int = -1;

pub const PWR_DCT_GEAR_REVERSE: c_int = 0;

pub const PWR_DCT_GEAR_1: c_int = 1;

pub const PWR_DCT_GEAR_2: c_int = 2;

pub const PWR_DCT_GEAR_3: c_int = 3;

pub const PWR_DCT_GEAR_4: c_int = 4;

pub const PWR_DCT_GEAR_5: c_int = 5;

pub const PWR_DCT_GEAR_6: c_int = 6;

pub const PWR_DCT_GEAR_7: c_int = 7;

pub const enum_pwr_dct_gear = c_int;

pub const pwr_dct_gear = enum_pwr_dct_gear;

pub const pwr_dct_gear_mask = u16;

pub const PWR_DCT_OK: c_int = 0;

pub const PWR_DCT_INVALID_ARGUMENT: c_int = 1;

pub const PWR_DCT_INVALID_CONFIG: c_int = 2;

pub const PWR_DCT_INVALID_GEAR_COMMAND: c_int = 3;

pub const PWR_DCT_INVALID_TIMESTEP: c_int = 4;

pub const PWR_DCT_NUMERIC_ERROR: c_int = 5;

pub const enum_pwr_dct_result = c_uint;

pub const pwr_dct_result = enum_pwr_dct_result;

pub const PWR_DCT_DIAG_NONE: c_int = 0;

pub const PWR_DCT_DIAG_INVALID_GEAR_BIT: c_int = 1;

pub const PWR_DCT_DIAG_MULTIPLE_GEARS_ON_SHAFT: c_int = 2;

pub const PWR_DCT_DIAG_DOG_SHIFT_WHILE_CLAMPED: c_int = 4;

pub const PWR_DCT_DIAG_K1_SLIPPING: c_int = 8;

pub const PWR_DCT_DIAG_K2_SLIPPING: c_int = 16;

pub const PWR_DCT_DIAG_K1_THERMAL_FADE: c_int = 32;

pub const PWR_DCT_DIAG_K2_THERMAL_FADE: c_int = 64;

pub const PWR_DCT_DIAG_K1_OVERHEAT: c_int = 128;

pub const PWR_DCT_DIAG_K2_OVERHEAT: c_int = 256;

pub const PWR_DCT_DIAG_CLAMPED_SHAFT_IN_NEUTRAL: c_int = 512;

pub const PWR_DCT_DIAG_ENERGY_RESIDUAL: c_int = 1024;

pub const PWR_DCT_DIAG_NUMERIC_FAILURE: c_int = 2048;

pub const PWR_DCT_DIAG_INPUT_REJECTED: c_int = 4096;

pub const enum_pwr_dct_diagnostic = c_uint;

pub const pwr_dct_diagnostic = enum_pwr_dct_diagnostic;

pub const struct_pwr_dct_config = extern struct {
    gear_ratio: [8]f64 = @import("std").mem.zeroes([8]f64),
    mesh_efficiency: [8]f64 = @import("std").mem.zeroes([8]f64),
    final_drive_ratio: f64 = @import("std").mem.zeroes(f64),
    clutch_nominal_capacity_nm: [2]f64 = @import("std").mem.zeroes([2]f64),
    slip_regularization_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    clamp_engagement_rate_per_s: [2]f64 = @import("std").mem.zeroes([2]f64),
    clamp_release_rate_per_s: [2]f64 = @import("std").mem.zeroes([2]f64),
    maximum_dog_shift_clamp: f64 = @import("std").mem.zeroes(f64),
    initial_clutch_temperature_k: [2]f64 = @import("std").mem.zeroes([2]f64),
    clutch_heat_capacity_j_per_k: [2]f64 = @import("std").mem.zeroes([2]f64),
    clutch_ambient_conductance_w_per_k: [2]f64 = @import("std").mem.zeroes([2]f64),
    fade_start_temperature_k: [2]f64 = @import("std").mem.zeroes([2]f64),
    fade_full_temperature_k: [2]f64 = @import("std").mem.zeroes([2]f64),
    minimum_fade_capacity_fraction: [2]f64 = @import("std").mem.zeroes([2]f64),
    overheat_temperature_k: [2]f64 = @import("std").mem.zeroes([2]f64),
    slip_diagnostic_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    maximum_timestep_s: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_dct_config = struct_pwr_dct_config;

pub const struct_pwr_dct_inputs = extern struct {
    engine_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    output_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    engaged_gear_mask: pwr_dct_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
    clutch_clamp_command: [2]f64 = @import("std").mem.zeroes([2]f64),
    ambient_temperature_k: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_dct_inputs = struct_pwr_dct_inputs;

pub const struct_pwr_dct_clutch_observation = extern struct {
    selected_gear: pwr_dct_gear = @import("std").mem.zeroes(pwr_dct_gear),
    preselected: bool = @import("std").mem.zeroes(bool),
    clamp_command: f64 = @import("std").mem.zeroes(f64),
    applied_clamp: f64 = @import("std").mem.zeroes(f64),
    input_shaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    slip_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    transmitted_torque_nm: f64 = @import("std").mem.zeroes(f64),
    torque_capacity_nm: f64 = @import("std").mem.zeroes(f64),
    friction_power_w: f64 = @import("std").mem.zeroes(f64),
    cumulative_friction_work_j: f64 = @import("std").mem.zeroes(f64),
    temperature_k: f64 = @import("std").mem.zeroes(f64),
    thermal_capacity_factor: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_dct_clutch_observation = struct_pwr_dct_clutch_observation;

pub const struct_pwr_dct_snapshot = extern struct {
    time_s: f64 = @import("std").mem.zeroes(f64),
    engaged_gear_mask: pwr_dct_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
    clutch: [2]pwr_dct_clutch_observation = @import("std").mem.zeroes([2]pwr_dct_clutch_observation),
    input_transmitted_torque_nm: f64 = @import("std").mem.zeroes(f64),
    output_torque_nm: f64 = @import("std").mem.zeroes(f64),
    input_power_w: f64 = @import("std").mem.zeroes(f64),
    output_power_w: f64 = @import("std").mem.zeroes(f64),
    clutch_friction_power_w: f64 = @import("std").mem.zeroes(f64),
    gear_mesh_loss_power_w: f64 = @import("std").mem.zeroes(f64),
    instantaneous_efficiency: f64 = @import("std").mem.zeroes(f64),
    cumulative_input_energy_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_output_energy_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_gear_mesh_loss_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_heat_rejected_j: f64 = @import("std").mem.zeroes(f64),
    stored_thermal_energy_change_j: f64 = @import("std").mem.zeroes(f64),
    energy_residual_j: f64 = @import("std").mem.zeroes(f64),
    last_step_diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    rejected_command_count: u64 = @import("std").mem.zeroes(u64),
    state_hash: u64 = @import("std").mem.zeroes(u64),
};

pub const pwr_dct_snapshot = struct_pwr_dct_snapshot;

pub const struct_pwr_dct = extern struct {
    config: pwr_dct_config = @import("std").mem.zeroes(pwr_dct_config),
    engaged_gear_mask: pwr_dct_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
    selected_gear: [2]pwr_dct_gear = @import("std").mem.zeroes([2]pwr_dct_gear),
    applied_clamp: [2]f64 = @import("std").mem.zeroes([2]f64),
    clutch_temperature_k: [2]f64 = @import("std").mem.zeroes([2]f64),
    cumulative_friction_work_j: [2]f64 = @import("std").mem.zeroes([2]f64),
    last_clamp_command: [2]f64 = @import("std").mem.zeroes([2]f64),
    last_input_shaft_speed_rad_s: [2]f64 = @import("std").mem.zeroes([2]f64),
    last_slip_speed_rad_s: [2]f64 = @import("std").mem.zeroes([2]f64),
    last_transmitted_torque_nm: [2]f64 = @import("std").mem.zeroes([2]f64),
    last_torque_capacity_nm: [2]f64 = @import("std").mem.zeroes([2]f64),
    last_friction_power_w: [2]f64 = @import("std").mem.zeroes([2]f64),
    last_thermal_capacity_factor: [2]f64 = @import("std").mem.zeroes([2]f64),
    last_engine_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    last_output_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    last_input_transmitted_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_output_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_input_power_w: f64 = @import("std").mem.zeroes(f64),
    last_output_power_w: f64 = @import("std").mem.zeroes(f64),
    last_gear_mesh_loss_power_w: f64 = @import("std").mem.zeroes(f64),
    last_instantaneous_efficiency: f64 = @import("std").mem.zeroes(f64),
    cumulative_input_energy_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_output_energy_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_gear_mesh_loss_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_heat_rejected_j: f64 = @import("std").mem.zeroes(f64),
    time_s: f64 = @import("std").mem.zeroes(f64),
    last_step_diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    rejected_command_count: u64 = @import("std").mem.zeroes(u64),
};

pub const pwr_dct = struct_pwr_dct;

pub const PWR_SI_ENGINE_SPECIES_INERT: c_int = 0;

pub const PWR_SI_ENGINE_SPECIES_OXYGEN: c_int = 1;

pub const PWR_SI_ENGINE_SPECIES_REDUCTANT: c_int = 2;

pub const PWR_SI_ENGINE_SPECIES_PRODUCTS: c_int = 3;

pub const PWR_SI_ENGINE_SPECIES_COUNT: c_int = 4;

pub const PWR_SI_ENGINE_OK: c_int = 0;

pub const PWR_SI_ENGINE_INVALID_ARGUMENT: c_int = 1;

pub const PWR_SI_ENGINE_INVALID_CONFIG: c_int = 2;

pub const PWR_SI_ENGINE_INVALID_INPUT: c_int = 3;

pub const PWR_SI_ENGINE_NUMERIC_ERROR: c_int = 4;

pub const enum_pwr_si_engine_result = c_uint;

pub const pwr_si_engine_result = enum_pwr_si_engine_result;

pub const PWR_SI_ENGINE_FAULT_NONE: c_int = 0;

pub const PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT: c_int = 1;

pub const PWR_SI_ENGINE_FAULT_THROTTLE_STUCK: c_int = 2;

pub const PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED: c_int = 4;

pub const PWR_SI_ENGINE_FAULT_LOW_VOLTAGE: c_int = 8;

pub const enum_pwr_si_engine_fault = c_uint;

pub const pwr_si_engine_fault = enum_pwr_si_engine_fault;

pub const PWR_SI_ENGINE_DIAG_NONE: c_int = 0;

pub const PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_INVALID: c_int = 1;

pub const PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_STALE: c_int = 2;

pub const PWR_SI_ENGINE_DIAG_ECU_BROWNOUT: c_int = 4;

pub const PWR_SI_ENGINE_DIAG_FUEL_CUT: c_int = 8;

pub const PWR_SI_ENGINE_DIAG_THROTTLE_LIMITED: c_int = 16;

pub const PWR_SI_ENGINE_DIAG_SPEED_LIMIT: c_int = 32;

pub const PWR_SI_ENGINE_DIAG_MANIFOLD_MASS_LIMIT: c_int = 64;

pub const PWR_SI_ENGINE_DIAG_NUMERIC_FAILURE: c_int = 128;

pub const enum_pwr_si_engine_diagnostic = c_uint;

pub const pwr_si_engine_diagnostic = enum_pwr_si_engine_diagnostic;

pub const struct_pwr_si_engine_config = extern struct {
    base_tick_ns: u64 = @import("std").mem.zeroes(u64),
    sensor_period_ticks: u32 = @import("std").mem.zeroes(u32),
    ecu_period_ticks: u32 = @import("std").mem.zeroes(u32),
    maximum_sensor_age_ticks: u32 = @import("std").mem.zeroes(u32),
    cylinder_count: u32 = @import("std").mem.zeroes(u32),
    displacement_m3: f64 = @import("std").mem.zeroes(f64),
    compression_ratio: f64 = @import("std").mem.zeroes(f64),
    rotational_inertia_kg_m2: f64 = @import("std").mem.zeroes(f64),
    intake_manifold_volume_m3: f64 = @import("std").mem.zeroes(f64),
    throttle_effective_area_m2: f64 = @import("std").mem.zeroes(f64),
    throttle_discharge_coefficient: f64 = @import("std").mem.zeroes(f64),
    throttle_time_constant_s: f64 = @import("std").mem.zeroes(f64),
    volumetric_efficiency: f64 = @import("std").mem.zeroes(f64),
    air_gas_constant_j_per_kg_k: f64 = @import("std").mem.zeroes(f64),
    ambient_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    ambient_temperature_k: f64 = @import("std").mem.zeroes(f64),
    stoichiometric_air_fuel_ratio: f64 = @import("std").mem.zeroes(f64),
    oxygen_mass_fraction: f64 = @import("std").mem.zeroes(f64),
    oxygen_per_fuel_kg_per_kg: f64 = @import("std").mem.zeroes(f64),
    fuel_lower_heating_value_j_per_kg: f64 = @import("std").mem.zeroes(f64),
    indicated_efficiency: f64 = @import("std").mem.zeroes(f64),
    exhaust_heat_fraction: f64 = @import("std").mem.zeroes(f64),
    injector_time_constant_s: f64 = @import("std").mem.zeroes(f64),
    friction_coulomb_nm: f64 = @import("std").mem.zeroes(f64),
    friction_viscous_nm_s_per_rad: f64 = @import("std").mem.zeroes(f64),
    pumping_scale: f64 = @import("std").mem.zeroes(f64),
    combustion_min_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    maximum_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    combustion_torque_ripple_fraction: f64 = @import("std").mem.zeroes(f64),
    coolant_initial_temperature_k: f64 = @import("std").mem.zeroes(f64),
    coolant_heat_capacity_j_per_k: f64 = @import("std").mem.zeroes(f64),
    coolant_ambient_conductance_w_per_k: f64 = @import("std").mem.zeroes(f64),
    exhaust_temperature_floor_k: f64 = @import("std").mem.zeroes(f64),
    exhaust_effective_heat_capacity_j_per_kg_k: f64 = @import("std").mem.zeroes(f64),
    exhaust_port_resistance_pa_per_kg_s_squared: f64 = @import("std").mem.zeroes(f64),
    low_voltage_nominal_v: f64 = @import("std").mem.zeroes(f64),
    low_voltage_internal_resistance_ohm: f64 = @import("std").mem.zeroes(f64),
    ecu_current_a: f64 = @import("std").mem.zeroes(f64),
    injector_current_a: f64 = @import("std").mem.zeroes(f64),
    starter_current_per_nm_a: f64 = @import("std").mem.zeroes(f64),
    ecu_brownout_threshold_v: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_si_engine_config = struct_pwr_si_engine_config;

pub const struct_pwr_si_engine_input = extern struct {
    driver_throttle: f64 = @import("std").mem.zeroes(f64),
    load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    starter_torque_nm: f64 = @import("std").mem.zeroes(f64),
    exhaust_backpressure_pa: f64 = @import("std").mem.zeroes(f64),
    ambient_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    ambient_temperature_k: f64 = @import("std").mem.zeroes(f64),
    fault_mask: u32 = @import("std").mem.zeroes(u32),
    ignition_on: bool = @import("std").mem.zeroes(bool),
};

pub const pwr_si_engine_input = struct_pwr_si_engine_input;

pub const struct_pwr_si_engine_state = extern struct {
    config: pwr_si_engine_config = @import("std").mem.zeroes(pwr_si_engine_config),
    tick: u64 = @import("std").mem.zeroes(u64),
    last_crank_sample_tick: u64 = @import("std").mem.zeroes(u64),
    crank_angle_rad: f64 = @import("std").mem.zeroes(f64),
    crank_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    sensed_crank_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    throttle_position: f64 = @import("std").mem.zeroes(f64),
    latched_throttle_command: f64 = @import("std").mem.zeroes(f64),
    intake_air_mass_kg: f64 = @import("std").mem.zeroes(f64),
    injector_fuel_flow_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    coolant_temperature_k: f64 = @import("std").mem.zeroes(f64),
    low_voltage_v: f64 = @import("std").mem.zeroes(f64),
    last_combustion_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_brake_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_pumping_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_friction_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_intake_air_flow_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    last_cylinder_air_flow_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    last_burned_fuel_flow_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    last_lambda: f64 = @import("std").mem.zeroes(f64),
    last_exhaust_temperature_k: f64 = @import("std").mem.zeroes(f64),
    last_exhaust_source_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    last_exhaust_species_mass_fraction: [4]f64 = @import("std").mem.zeroes([4]f64),
    initial_intake_air_mass_kg: f64 = @import("std").mem.zeroes(f64),
    cumulative_intake_air_kg: f64 = @import("std").mem.zeroes(f64),
    cumulative_cylinder_air_kg: f64 = @import("std").mem.zeroes(f64),
    cumulative_exhaust_mass_kg: f64 = @import("std").mem.zeroes(f64),
    cumulative_injected_fuel_kg: f64 = @import("std").mem.zeroes(f64),
    cumulative_burned_fuel_kg: f64 = @import("std").mem.zeroes(f64),
    cumulative_fuel_energy_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_unburned_fuel_energy_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_starter_work_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_load_work_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_exhaust_heat_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_ambient_heat_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_numerical_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    initial_stored_energy_j: f64 = @import("std").mem.zeroes(f64),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    crank_signal_valid: bool = @import("std").mem.zeroes(bool),
    ecu_active: bool = @import("std").mem.zeroes(bool),
    combustion_active: bool = @import("std").mem.zeroes(bool),
};

pub const pwr_si_engine_state = struct_pwr_si_engine_state;

pub const struct_pwr_si_engine_output = extern struct {
    time_s: f64 = @import("std").mem.zeroes(f64),
    crank_angle_rad: f64 = @import("std").mem.zeroes(f64),
    crank_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    sensed_crank_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    throttle_position: f64 = @import("std").mem.zeroes(f64),
    manifold_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    low_voltage_v: f64 = @import("std").mem.zeroes(f64),
    combustion_torque_nm: f64 = @import("std").mem.zeroes(f64),
    brake_torque_nm: f64 = @import("std").mem.zeroes(f64),
    pumping_torque_nm: f64 = @import("std").mem.zeroes(f64),
    friction_torque_nm: f64 = @import("std").mem.zeroes(f64),
    intake_air_flow_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    cylinder_air_flow_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    injected_fuel_flow_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    burned_fuel_flow_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    lambda: f64 = @import("std").mem.zeroes(f64),
    exhaust_mass_flow_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    exhaust_temperature_k: f64 = @import("std").mem.zeroes(f64),
    exhaust_source_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    exhaust_species_mass_fraction: [4]f64 = @import("std").mem.zeroes([4]f64),
    coolant_temperature_k: f64 = @import("std").mem.zeroes(f64),
    fuel_energy_j: f64 = @import("std").mem.zeroes(f64),
    mechanical_load_work_j: f64 = @import("std").mem.zeroes(f64),
    exhaust_heat_j: f64 = @import("std").mem.zeroes(f64),
    ambient_heat_j: f64 = @import("std").mem.zeroes(f64),
    stored_energy_change_j: f64 = @import("std").mem.zeroes(f64),
    unburned_fuel_energy_j: f64 = @import("std").mem.zeroes(f64),
    numerical_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    energy_residual_j: f64 = @import("std").mem.zeroes(f64),
    air_mass_residual_kg: f64 = @import("std").mem.zeroes(f64),
    crank_sensor_age_ticks: u64 = @import("std").mem.zeroes(u64),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    crank_signal_valid: bool = @import("std").mem.zeroes(bool),
    ecu_active: bool = @import("std").mem.zeroes(bool),
    combustion_active: bool = @import("std").mem.zeroes(bool),
    state_hash: u64 = @import("std").mem.zeroes(u64),
};

pub const pwr_si_engine_output = struct_pwr_si_engine_output;

pub const pwr_si_pi: f64 = 3.141592653589793;

pub const PWR_EXHAUST_VOLUME_COUNT: c_int = 3;

pub const PWR_EXHAUST_PATH_COUNT: c_int = 4;

pub const PWR_EXHAUST_SPECIES_INERT: c_int = 0;

pub const PWR_EXHAUST_SPECIES_OXYGEN: c_int = 1;

pub const PWR_EXHAUST_SPECIES_REDUCTANT: c_int = 2;

pub const PWR_EXHAUST_SPECIES_PRODUCTS: c_int = 3;

pub const PWR_EXHAUST_SPECIES_COUNT: c_int = 4;

pub const enum_pwr_exhaust_species = c_uint;

pub const pwr_exhaust_species = enum_pwr_exhaust_species;

pub const PWR_EXHAUST_VOLUME_MANIFOLD: c_int = 0;

pub const PWR_EXHAUST_VOLUME_CATALYST: c_int = 1;

pub const PWR_EXHAUST_VOLUME_MUFFLER: c_int = 2;

pub const enum_pwr_exhaust_volume = c_uint;

pub const pwr_exhaust_volume = enum_pwr_exhaust_volume;

pub const PWR_EXHAUST_PATH_INLET: c_int = 0;

pub const PWR_EXHAUST_PATH_MANIFOLD_TO_CATALYST: c_int = 1;

pub const PWR_EXHAUST_PATH_CATALYST_TO_MUFFLER: c_int = 2;

pub const PWR_EXHAUST_PATH_TAILPIPE: c_int = 3;

pub const enum_pwr_exhaust_path = c_uint;

pub const pwr_exhaust_path = enum_pwr_exhaust_path;

pub const PWR_EXHAUST_OK: c_int = 0;

pub const PWR_EXHAUST_INVALID_ARGUMENT: c_int = 1;

pub const PWR_EXHAUST_INVALID_PARAMETER: c_int = 2;

pub const PWR_EXHAUST_INVALID_BOUNDARY: c_int = 3;

pub const PWR_EXHAUST_INVALID_STATE: c_int = 4;

pub const PWR_EXHAUST_INVALID_TIMESTEP: c_int = 5;

pub const PWR_EXHAUST_SUBSTEP_LIMIT: c_int = 6;

pub const enum_pwr_exhaust_result = c_uint;

pub const pwr_exhaust_result = enum_pwr_exhaust_result;

pub const PWR_EXHAUST_DIAG_NONE: c_int = 0;

pub const PWR_EXHAUST_DIAG_CHOKED_FLOW: c_int = 1;

pub const PWR_EXHAUST_DIAG_REVERSE_FLOW: c_int = 2;

pub const PWR_EXHAUST_DIAG_FLOW_LIMITED: c_int = 4;

pub const PWR_EXHAUST_DIAG_REACTION_LIMITED: c_int = 8;

pub const PWR_EXHAUST_DIAG_POSITIVITY_CORRECTION: c_int = 16;

pub const PWR_EXHAUST_DIAG_CATALYST_LIGHT_OFF: c_int = 32;

pub const PWR_EXHAUST_DIAG_INPUT_REJECTED: c_int = 64;

pub const PWR_EXHAUST_DIAG_NUMERIC_FAILURE: c_int = 128;

pub const enum_pwr_exhaust_diagnostic_flag = c_uint;

pub const pwr_exhaust_diagnostic_flag = enum_pwr_exhaust_diagnostic_flag;

pub const struct_pwr_exhaust_parameters = extern struct {
    volume_m3: [3]f64 = @import("std").mem.zeroes([3]f64),
    wall_heat_capacity_j_per_k: [3]f64 = @import("std").mem.zeroes([3]f64),
    gas_wall_conductance_w_per_k: [3]f64 = @import("std").mem.zeroes([3]f64),
    wall_ambient_conductance_w_per_k: [3]f64 = @import("std").mem.zeroes([3]f64),
    orifice_area_m2: [4]f64 = @import("std").mem.zeroes([4]f64),
    discharge_coefficient: [4]f64 = @import("std").mem.zeroes([4]f64),
    species_gas_constant_j_per_kg_k: [4]f64 = @import("std").mem.zeroes([4]f64),
    species_cv_j_per_kg_k: [4]f64 = @import("std").mem.zeroes([4]f64),
    catalyst_light_off_temperature_k: f64 = @import("std").mem.zeroes(f64),
    catalyst_full_activity_temperature_k: f64 = @import("std").mem.zeroes(f64),
    catalyst_activity_time_constant_s: f64 = @import("std").mem.zeroes(f64),
    catalyst_reaction_time_s: f64 = @import("std").mem.zeroes(f64),
    catalyst_oxygen_per_reductant_kg_per_kg: f64 = @import("std").mem.zeroes(f64),
    catalyst_reaction_heat_j_per_kg_reductant: f64 = @import("std").mem.zeroes(f64),
    catalyst_heat_to_wall_fraction: f64 = @import("std").mem.zeroes(f64),
    minimum_temperature_k: f64 = @import("std").mem.zeroes(f64),
    minimum_volume_mass_kg: f64 = @import("std").mem.zeroes(f64),
    maximum_substep_s: f64 = @import("std").mem.zeroes(f64),
    maximum_outflow_fraction_per_substep: f64 = @import("std").mem.zeroes(f64),
    maximum_substeps: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_exhaust_parameters = struct_pwr_exhaust_parameters;

pub const struct_pwr_exhaust_reservoir = extern struct {
    pressure_pa: f64 = @import("std").mem.zeroes(f64),
    temperature_k: f64 = @import("std").mem.zeroes(f64),
    species_mass_fraction: [4]f64 = @import("std").mem.zeroes([4]f64),
};

pub const pwr_exhaust_reservoir = struct_pwr_exhaust_reservoir;

pub const struct_pwr_exhaust_inputs = extern struct {
    upstream: pwr_exhaust_reservoir = @import("std").mem.zeroes(pwr_exhaust_reservoir),
    downstream: pwr_exhaust_reservoir = @import("std").mem.zeroes(pwr_exhaust_reservoir),
    inlet_area_scale: f64 = @import("std").mem.zeroes(f64),
    tailpipe_area_scale: f64 = @import("std").mem.zeroes(f64),
    ambient_temperature_k: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_exhaust_inputs = struct_pwr_exhaust_inputs;

pub const struct_pwr_exhaust_volume_state = extern struct {
    total_mass_kg: f64 = @import("std").mem.zeroes(f64),
    species_mass_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    internal_energy_j: f64 = @import("std").mem.zeroes(f64),
    wall_temperature_k: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_exhaust_volume_state = struct_pwr_exhaust_volume_state;

pub const struct_pwr_exhaust_state = extern struct {
    volume: [3]pwr_exhaust_volume_state = @import("std").mem.zeroes([3]pwr_exhaust_volume_state),
    catalyst_conversion_fraction: f64 = @import("std").mem.zeroes(f64),
    cumulative_tailpipe_species_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    time_s: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_exhaust_state = struct_pwr_exhaust_state;

pub const struct_pwr_exhaust_observation = extern struct {
    pressure_pa: [3]f64 = @import("std").mem.zeroes([3]f64),
    gas_temperature_k: [3]f64 = @import("std").mem.zeroes([3]f64),
    wall_temperature_k: [3]f64 = @import("std").mem.zeroes([3]f64),
    total_mass_kg: [3]f64 = @import("std").mem.zeroes([3]f64),
    catalyst_conversion_fraction: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_exhaust_observation = struct_pwr_exhaust_observation;

pub const struct_pwr_exhaust_diagnostics = extern struct {
    result: pwr_exhaust_result = @import("std").mem.zeroes(pwr_exhaust_result),
    flags: u32 = @import("std").mem.zeroes(u32),
    substeps: u32 = @import("std").mem.zeroes(u32),
    flow_limit_events: u32 = @import("std").mem.zeroes(u32),
    reaction_limit_events: u32 = @import("std").mem.zeroes(u32),
    positivity_correction_events: u32 = @import("std").mem.zeroes(u32),
    average_path_mass_flow_kg_per_s: [4]f64 = @import("std").mem.zeroes([4]f64),
    choked_path_mask: u32 = @import("std").mem.zeroes(u32),
    boundary_mass_net_kg: f64 = @import("std").mem.zeroes(f64),
    boundary_species_net_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    boundary_enthalpy_net_j: f64 = @import("std").mem.zeroes(f64),
    ambient_heat_net_j: f64 = @import("std").mem.zeroes(f64),
    reaction_heat_j: f64 = @import("std").mem.zeroes(f64),
    reacted_reductant_kg: f64 = @import("std").mem.zeroes(f64),
    tailpipe_species_out_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    mass_residual_kg: f64 = @import("std").mem.zeroes(f64),
    species_mass_residual_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    energy_residual_j: f64 = @import("std").mem.zeroes(f64),
    positivity_mass_adjustment_kg: f64 = @import("std").mem.zeroes(f64),
    positivity_species_adjustment_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    positivity_energy_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    state_species_closure_residual_kg: f64 = @import("std").mem.zeroes(f64),
    final_observation: pwr_exhaust_observation = @import("std").mem.zeroes(pwr_exhaust_observation),
};

pub const pwr_exhaust_diagnostics = struct_pwr_exhaust_diagnostics;

pub const struct_pwr_exhaust_gas_properties = extern struct {
    pressure_pa: f64 = @import("std").mem.zeroes(f64),
    temperature_k: f64 = @import("std").mem.zeroes(f64),
    gas_constant_j_per_kg_k: f64 = @import("std").mem.zeroes(f64),
    cv_j_per_kg_k: f64 = @import("std").mem.zeroes(f64),
    cp_j_per_kg_k: f64 = @import("std").mem.zeroes(f64),
    gamma: f64 = @import("std").mem.zeroes(f64),
    mass_fraction: [4]f64 = @import("std").mem.zeroes([4]f64),
};

pub const pwr_exhaust_gas_properties = struct_pwr_exhaust_gas_properties;

pub const struct_pwr_exhaust_ledger = extern struct {
    path_mass_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    boundary_mass_net_kg: f64 = @import("std").mem.zeroes(f64),
    boundary_species_net_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    boundary_enthalpy_net_j: f64 = @import("std").mem.zeroes(f64),
    ambient_heat_net_j: f64 = @import("std").mem.zeroes(f64),
    reaction_heat_j: f64 = @import("std").mem.zeroes(f64),
    reaction_species_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    reacted_reductant_kg: f64 = @import("std").mem.zeroes(f64),
    tailpipe_species_out_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    positivity_mass_adjustment_kg: f64 = @import("std").mem.zeroes(f64),
    positivity_species_adjustment_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    positivity_energy_adjustment_j: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_exhaust_ledger = struct_pwr_exhaust_ledger;

pub const struct_pwr_exhaust_rates = extern struct {
    total_mass_kg_per_s: [3]f64 = @import("std").mem.zeroes([3]f64),
    species_mass_kg_per_s: [3][4]f64 = @import("std").mem.zeroes([3][4]f64),
    internal_energy_w: [3]f64 = @import("std").mem.zeroes([3]f64),
    wall_temperature_k_per_s: [3]f64 = @import("std").mem.zeroes([3]f64),
    catalyst_conversion_per_s: f64 = @import("std").mem.zeroes(f64),
    path_mass_flow_kg_per_s: [4]f64 = @import("std").mem.zeroes([4]f64),
    boundary_mass_net_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    boundary_species_net_kg_per_s: [4]f64 = @import("std").mem.zeroes([4]f64),
    boundary_enthalpy_net_w: f64 = @import("std").mem.zeroes(f64),
    ambient_heat_net_w: f64 = @import("std").mem.zeroes(f64),
    reaction_heat_w: f64 = @import("std").mem.zeroes(f64),
    reaction_species_kg_per_s: [4]f64 = @import("std").mem.zeroes([4]f64),
    reacted_reductant_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    tailpipe_species_out_kg_per_s: [4]f64 = @import("std").mem.zeroes([4]f64),
    flags: u32 = @import("std").mem.zeroes(u32),
    choked_path_mask: u32 = @import("std").mem.zeroes(u32),
    flow_limit_events: u32 = @import("std").mem.zeroes(u32),
    reaction_limit_events: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_exhaust_rates = struct_pwr_exhaust_rates;

pub const struct_pwr_exhaust_correction = extern struct {
    mass_kg: f64 = @import("std").mem.zeroes(f64),
    species_kg: [4]f64 = @import("std").mem.zeroes([4]f64),
    energy_j: f64 = @import("std").mem.zeroes(f64),
    events: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_exhaust_correction = struct_pwr_exhaust_correction;

pub const PWR_AUTOMATIC_POWERTRAIN_OK: c_int = 0;

pub const PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT: c_int = 1;

pub const PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG: c_int = 2;

pub const PWR_AUTOMATIC_POWERTRAIN_INVALID_INPUT: c_int = 3;

pub const PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR: c_int = 4;

pub const PWR_AUTOMATIC_POWERTRAIN_NUMERIC_ERROR: c_int = 5;

pub const enum_pwr_automatic_powertrain_result = c_uint;

pub const pwr_automatic_powertrain_result = enum_pwr_automatic_powertrain_result;

pub const PWR_AUTOMATIC_POWERTRAIN_CONTROL_SUPERVISED: c_int = 0;

pub const PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT: c_int = 1;

pub const enum_pwr_automatic_powertrain_control_mode = c_uint;

pub const pwr_automatic_powertrain_control_mode = enum_pwr_automatic_powertrain_control_mode;

pub const PWR_AUTOMATIC_POWERTRAIN_TCU_NEUTRAL: c_int = 0;

pub const PWR_AUTOMATIC_POWERTRAIN_TCU_FIRST: c_int = 1;

pub const PWR_AUTOMATIC_POWERTRAIN_TCU_SHIFT_1_TO_2: c_int = 2;

pub const PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND: c_int = 3;

pub const PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND_LOCKED: c_int = 4;

pub const PWR_AUTOMATIC_POWERTRAIN_TCU_BROWNOUT: c_int = 5;

pub const PWR_AUTOMATIC_POWERTRAIN_TCU_DIRECT: c_int = 6;

pub const enum_pwr_automatic_powertrain_tcu_phase = c_uint;

pub const pwr_automatic_powertrain_tcu_phase = enum_pwr_automatic_powertrain_tcu_phase;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_NONE: c_int = 0;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_ECU_BROWNOUT: c_int = 1;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_TCU_BROWNOUT: c_int = 2;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_SHIFT_ACTIVE: c_int = 4;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_LOCKUP_ACTIVE: c_int = 8;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH: c_int = 16;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH: c_int = 32;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_ENGINE_CONVERTER_ENERGY_MISMATCH: c_int = 64;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_PUMP_SPEED_PROJECTED: c_int = 128;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_DIFFERENTIAL_SPEED_PROJECTED: c_int = 256;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_FINAL_DRIVE_ENERGY_MISMATCH: c_int = 512;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_INPUT_REJECTED: c_int = 1024;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_SUBMODEL_FAILURE: c_int = 2048;

pub const PWR_AUTOMATIC_POWERTRAIN_DIAG_NUMERIC_FAILURE: c_int = 4096;

pub const enum_pwr_automatic_powertrain_diagnostic = c_uint;

pub const pwr_automatic_powertrain_diagnostic = enum_pwr_automatic_powertrain_diagnostic;

pub const struct_pwr_automatic_powertrain_config = extern struct {
    engine: pwr_si_engine_config = @import("std").mem.zeroes(pwr_si_engine_config),
    automatic: pwr_automatic_config = @import("std").mem.zeroes(pwr_automatic_config),
    exhaust: pwr_exhaust_parameters = @import("std").mem.zeroes(pwr_exhaust_parameters),
    initial_ambient_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    initial_ambient_temperature_k: f64 = @import("std").mem.zeroes(f64),
    initial_exhaust_wall_temperature_k: f64 = @import("std").mem.zeroes(f64),
    electrical_nominal_voltage_v: f64 = @import("std").mem.zeroes(f64),
    electrical_internal_resistance_ohm: f64 = @import("std").mem.zeroes(f64),
    base_accessory_current_a: f64 = @import("std").mem.zeroes(f64),
    tcu_current_a: f64 = @import("std").mem.zeroes(f64),
    solenoid_current_at_max_pressure_a: f64 = @import("std").mem.zeroes(f64),
    starter_current_per_nm_a: f64 = @import("std").mem.zeroes(f64),
    ecu_brownout_voltage_v: f64 = @import("std").mem.zeroes(f64),
    tcu_brownout_voltage_v: f64 = @import("std").mem.zeroes(f64),
    starter_torque_nm: f64 = @import("std").mem.zeroes(f64),
    starter_release_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    maximum_cranking_time_s: f64 = @import("std").mem.zeroes(f64),
    cranking_throttle_command: f64 = @import("std").mem.zeroes(f64),
    idle_target_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    idle_throttle_command: f64 = @import("std").mem.zeroes(f64),
    idle_speed_throttle_gain_s_per_rad: f64 = @import("std").mem.zeroes(f64),
    line_pressure_throttle_gain_pa: f64 = @import("std").mem.zeroes(f64),
    first_to_second_halfshaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    minimum_first_gear_time_s: f64 = @import("std").mem.zeroes(f64),
    lockup_enable_halfshaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    shift_engine_throttle_scale: f64 = @import("std").mem.zeroes(f64),
    final_drive_ratio: f64 = @import("std").mem.zeroes(f64),
    final_drive_efficiency: f64 = @import("std").mem.zeroes(f64),
    left_halfshaft_inertia_kg_m2: f64 = @import("std").mem.zeroes(f64),
    right_halfshaft_inertia_kg_m2: f64 = @import("std").mem.zeroes(f64),
    rolling_resistance_torque_nm_per_side: f64 = @import("std").mem.zeroes(f64),
    viscous_road_load_nm_s_per_rad_per_side: f64 = @import("std").mem.zeroes(f64),
    quadratic_road_load_nm_s2_per_rad2_per_side: f64 = @import("std").mem.zeroes(f64),
    road_load_regularization_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    maximum_exhaust_mass_mismatch_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    maximum_exhaust_enthalpy_mismatch_w: f64 = @import("std").mem.zeroes(f64),
    maximum_mechanical_coupling_mismatch_w: f64 = @import("std").mem.zeroes(f64),
    maximum_speed_projection_rad_s: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_automatic_powertrain_config = struct_pwr_automatic_powertrain_config;

pub const struct_pwr_automatic_powertrain_input = extern struct {
    control_mode: pwr_automatic_powertrain_control_mode = @import("std").mem.zeroes(pwr_automatic_powertrain_control_mode),
    driver_throttle: f64 = @import("std").mem.zeroes(f64),
    ambient_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    ambient_temperature_k: f64 = @import("std").mem.zeroes(f64),
    electrical_supply_voltage_v: f64 = @import("std").mem.zeroes(f64),
    left_additional_road_load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    right_additional_road_load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    tailpipe_area_scale: f64 = @import("std").mem.zeroes(f64),
    engine_fault_mask: u32 = @import("std").mem.zeroes(u32),
    ignition_on: bool = @import("std").mem.zeroes(bool),
    selector_drive: bool = @import("std").mem.zeroes(bool),
    direct_starter_torque_nm: f64 = @import("std").mem.zeroes(f64),
    direct_line_pressure_command_pa: f64 = @import("std").mem.zeroes(f64),
    direct_lockup_command: f64 = @import("std").mem.zeroes(f64),
    direct_requested_gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
};

pub const pwr_automatic_powertrain_input = struct_pwr_automatic_powertrain_input;

pub const struct_pwr_automatic_powertrain_output = extern struct {
    time_s: f64 = @import("std").mem.zeroes(f64),
    tick: u64 = @import("std").mem.zeroes(u64),
    control_mode: pwr_automatic_powertrain_control_mode = @import("std").mem.zeroes(pwr_automatic_powertrain_control_mode),
    tcu_phase: pwr_automatic_powertrain_tcu_phase = @import("std").mem.zeroes(pwr_automatic_powertrain_tcu_phase),
    low_voltage_v: f64 = @import("std").mem.zeroes(f64),
    ecu_powered: bool = @import("std").mem.zeroes(bool),
    tcu_powered: bool = @import("std").mem.zeroes(bool),
    commanded_engine_throttle: f64 = @import("std").mem.zeroes(f64),
    commanded_starter_torque_nm: f64 = @import("std").mem.zeroes(f64),
    commanded_line_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    commanded_lockup: f64 = @import("std").mem.zeroes(f64),
    commanded_gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
    engine: pwr_si_engine_output = @import("std").mem.zeroes(pwr_si_engine_output),
    automatic: pwr_automatic_snapshot = @import("std").mem.zeroes(pwr_automatic_snapshot),
    converter_coupling: pwr_automatic_coupling_output = @import("std").mem.zeroes(pwr_automatic_coupling_output),
    exhaust: pwr_exhaust_observation = @import("std").mem.zeroes(pwr_exhaust_observation),
    exhaust_step: pwr_exhaust_diagnostics = @import("std").mem.zeroes(pwr_exhaust_diagnostics),
    differential_carrier_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    left_halfshaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    right_halfshaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    left_halfshaft_angle_rad: f64 = @import("std").mem.zeroes(f64),
    right_halfshaft_angle_rad: f64 = @import("std").mem.zeroes(f64),
    left_drive_torque_nm: f64 = @import("std").mem.zeroes(f64),
    right_drive_torque_nm: f64 = @import("std").mem.zeroes(f64),
    left_road_load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    right_road_load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    differential_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    halfshaft_kinetic_energy_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_halfshaft_drive_work_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_road_load_work_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_differential_projection_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    halfshaft_energy_residual_j: f64 = @import("std").mem.zeroes(f64),
    exhaust_mass_mismatch_kg: f64 = @import("std").mem.zeroes(f64),
    cumulative_exhaust_mass_mismatch_kg: f64 = @import("std").mem.zeroes(f64),
    exhaust_enthalpy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_exhaust_enthalpy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    engine_converter_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_engine_converter_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    pump_speed_projection_rad_s: f64 = @import("std").mem.zeroes(f64),
    pump_projection_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    unconstrained_carrier_speed_mismatch_rad_s: f64 = @import("std").mem.zeroes(f64),
    carrier_constraint_residual_rad_s: f64 = @import("std").mem.zeroes(f64),
    differential_projection_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    final_drive_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_final_drive_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    last_step_diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    state_hash: u64 = @import("std").mem.zeroes(u64),
};

pub const pwr_automatic_powertrain_output = struct_pwr_automatic_powertrain_output;

pub const struct_pwr_automatic_powertrain_state = extern struct {
    config: pwr_automatic_powertrain_config = @import("std").mem.zeroes(pwr_automatic_powertrain_config),
    engine: pwr_si_engine_state = @import("std").mem.zeroes(pwr_si_engine_state),
    automatic: pwr_automatic = @import("std").mem.zeroes(pwr_automatic),
    exhaust: pwr_exhaust_state = @import("std").mem.zeroes(pwr_exhaust_state),
    engine_output: pwr_si_engine_output = @import("std").mem.zeroes(pwr_si_engine_output),
    automatic_output: pwr_automatic_snapshot = @import("std").mem.zeroes(pwr_automatic_snapshot),
    converter_coupling: pwr_automatic_coupling_output = @import("std").mem.zeroes(pwr_automatic_coupling_output),
    exhaust_output: pwr_exhaust_observation = @import("std").mem.zeroes(pwr_exhaust_observation),
    exhaust_step: pwr_exhaust_diagnostics = @import("std").mem.zeroes(pwr_exhaust_diagnostics),
    tick: u64 = @import("std").mem.zeroes(u64),
    cranking_ticks: u64 = @import("std").mem.zeroes(u64),
    first_gear_ticks: u64 = @import("std").mem.zeroes(u64),
    last_control_mode: pwr_automatic_powertrain_control_mode = @import("std").mem.zeroes(pwr_automatic_powertrain_control_mode),
    tcu_phase: pwr_automatic_powertrain_tcu_phase = @import("std").mem.zeroes(pwr_automatic_powertrain_tcu_phase),
    supervised_gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
    engine_started: bool = @import("std").mem.zeroes(bool),
    ecu_powered: bool = @import("std").mem.zeroes(bool),
    tcu_powered: bool = @import("std").mem.zeroes(bool),
    low_voltage_v: f64 = @import("std").mem.zeroes(f64),
    left_halfshaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    right_halfshaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    left_halfshaft_angle_rad: f64 = @import("std").mem.zeroes(f64),
    right_halfshaft_angle_rad: f64 = @import("std").mem.zeroes(f64),
    initial_halfshaft_kinetic_energy_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_halfshaft_drive_work_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_road_load_work_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_differential_projection_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    last_commanded_engine_throttle: f64 = @import("std").mem.zeroes(f64),
    last_commanded_starter_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_commanded_line_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    last_commanded_lockup: f64 = @import("std").mem.zeroes(f64),
    last_commanded_gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
    last_left_drive_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_right_drive_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_left_road_load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_right_road_load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_exhaust_mass_mismatch_kg: f64 = @import("std").mem.zeroes(f64),
    cumulative_exhaust_mass_mismatch_kg: f64 = @import("std").mem.zeroes(f64),
    last_exhaust_enthalpy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_exhaust_enthalpy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    last_engine_converter_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_engine_converter_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    last_pump_speed_projection_rad_s: f64 = @import("std").mem.zeroes(f64),
    last_unconstrained_carrier_speed_mismatch_rad_s: f64 = @import("std").mem.zeroes(f64),
    last_carrier_constraint_residual_rad_s: f64 = @import("std").mem.zeroes(f64),
    last_differential_projection_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    last_final_drive_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_final_drive_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    last_step_diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_automatic_powertrain_state = struct_pwr_automatic_powertrain_state;

pub const struct_pwr_automatic_powertrain_commands = extern struct {
    engine_throttle: f64 = @import("std").mem.zeroes(f64),
    starter_torque_nm: f64 = @import("std").mem.zeroes(f64),
    line_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    lockup: f64 = @import("std").mem.zeroes(f64),
    gear: pwr_automatic_gear = @import("std").mem.zeroes(pwr_automatic_gear),
};

pub const pwr_automatic_powertrain_commands = struct_pwr_automatic_powertrain_commands;

pub const pwr_automatic_powertrain_tick_s: f64 = 0.0001;

pub const PWR_DCT_POWERTRAIN_OK: c_int = 0;

pub const PWR_DCT_POWERTRAIN_INVALID_ARGUMENT: c_int = 1;

pub const PWR_DCT_POWERTRAIN_INVALID_CONFIG: c_int = 2;

pub const PWR_DCT_POWERTRAIN_INVALID_INPUT: c_int = 3;

pub const PWR_DCT_POWERTRAIN_SUBMODEL_ERROR: c_int = 4;

pub const PWR_DCT_POWERTRAIN_NUMERIC_ERROR: c_int = 5;

pub const enum_pwr_dct_powertrain_result = c_uint;

pub const pwr_dct_powertrain_result = enum_pwr_dct_powertrain_result;

pub const PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED: c_int = 0;

pub const PWR_DCT_POWERTRAIN_CONTROL_DIRECT: c_int = 1;

pub const enum_pwr_dct_powertrain_control_mode = c_uint;

pub const pwr_dct_powertrain_control_mode = enum_pwr_dct_powertrain_control_mode;

pub const PWR_DCT_POWERTRAIN_TCU_NEUTRAL: c_int = 0;

pub const PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2: c_int = 1;

pub const PWR_DCT_POWERTRAIN_TCU_LAUNCH_1: c_int = 2;

pub const PWR_DCT_POWERTRAIN_TCU_GEAR_1: c_int = 3;

pub const PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2: c_int = 4;

pub const PWR_DCT_POWERTRAIN_TCU_GEAR_2: c_int = 5;

pub const PWR_DCT_POWERTRAIN_TCU_RELEASE_TO_NEUTRAL: c_int = 6;

pub const PWR_DCT_POWERTRAIN_TCU_DIRECT: c_int = 7;

pub const enum_pwr_dct_powertrain_tcu_phase = c_uint;

pub const pwr_dct_powertrain_tcu_phase = enum_pwr_dct_powertrain_tcu_phase;

pub const PWR_DCT_POWERTRAIN_DIAG_NONE: c_int = 0;

pub const PWR_DCT_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH: c_int = 1;

pub const PWR_DCT_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH: c_int = 2;

pub const PWR_DCT_POWERTRAIN_DIAG_ENGINE_DCT_ENERGY_MISMATCH: c_int = 4;

pub const PWR_DCT_POWERTRAIN_DIAG_DCT_OUTPUT_ENERGY_MISMATCH: c_int = 8;

pub const PWR_DCT_POWERTRAIN_DIAG_HALFSHAFT_SPEED_CLAMP: c_int = 16;

pub const PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE: c_int = 32;

pub const PWR_DCT_POWERTRAIN_DIAG_TCU_SHIFT_ACTIVE: c_int = 64;

pub const PWR_DCT_POWERTRAIN_DIAG_INPUT_REJECTED: c_int = 128;

pub const PWR_DCT_POWERTRAIN_DIAG_SUBMODEL_FAILURE: c_int = 256;

pub const PWR_DCT_POWERTRAIN_DIAG_NUMERIC_FAILURE: c_int = 512;

pub const enum_pwr_dct_powertrain_diagnostic = c_uint;

pub const pwr_dct_powertrain_diagnostic = enum_pwr_dct_powertrain_diagnostic;

pub const struct_pwr_dct_powertrain_config = extern struct {
    engine: pwr_si_engine_config = @import("std").mem.zeroes(pwr_si_engine_config),
    dct: pwr_dct_config = @import("std").mem.zeroes(pwr_dct_config),
    exhaust: pwr_exhaust_parameters = @import("std").mem.zeroes(pwr_exhaust_parameters),
    initial_ambient_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    initial_ambient_temperature_k: f64 = @import("std").mem.zeroes(f64),
    initial_exhaust_wall_temperature_k: f64 = @import("std").mem.zeroes(f64),
    halfshaft_inertia_kg_m2: f64 = @import("std").mem.zeroes(f64),
    rolling_resistance_torque_nm: f64 = @import("std").mem.zeroes(f64),
    viscous_road_load_nm_s_per_rad: f64 = @import("std").mem.zeroes(f64),
    quadratic_road_load_nm_s2_per_rad2: f64 = @import("std").mem.zeroes(f64),
    road_load_regularization_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    starter_torque_nm: f64 = @import("std").mem.zeroes(f64),
    starter_release_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    maximum_cranking_time_s: f64 = @import("std").mem.zeroes(f64),
    cranking_throttle_command: f64 = @import("std").mem.zeroes(f64),
    idle_target_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    idle_throttle_command: f64 = @import("std").mem.zeroes(f64),
    idle_speed_throttle_gain_s_per_rad: f64 = @import("std").mem.zeroes(f64),
    launch_target_engine_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    launch_clamp_feedforward: f64 = @import("std").mem.zeroes(f64),
    launch_clamp_speed_gain_s_per_rad: f64 = @import("std").mem.zeroes(f64),
    launch_complete_halfshaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    first_to_second_shift_halfshaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    minimum_first_gear_time_s: f64 = @import("std").mem.zeroes(f64),
    first_to_second_shift_duration_s: f64 = @import("std").mem.zeroes(f64),
    shift_engine_throttle_scale: f64 = @import("std").mem.zeroes(f64),
    maximum_exhaust_mass_mismatch_kg_per_s: f64 = @import("std").mem.zeroes(f64),
    maximum_exhaust_enthalpy_mismatch_w: f64 = @import("std").mem.zeroes(f64),
    maximum_mechanical_coupling_mismatch_w: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_dct_powertrain_config = struct_pwr_dct_powertrain_config;

pub const struct_pwr_dct_powertrain_input = extern struct {
    control_mode: pwr_dct_powertrain_control_mode = @import("std").mem.zeroes(pwr_dct_powertrain_control_mode),
    driver_throttle: f64 = @import("std").mem.zeroes(f64),
    ambient_pressure_pa: f64 = @import("std").mem.zeroes(f64),
    ambient_temperature_k: f64 = @import("std").mem.zeroes(f64),
    additional_road_load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    tailpipe_area_scale: f64 = @import("std").mem.zeroes(f64),
    engine_fault_mask: u32 = @import("std").mem.zeroes(u32),
    ignition_on: bool = @import("std").mem.zeroes(bool),
    selector_drive: bool = @import("std").mem.zeroes(bool),
    direct_starter_torque_nm: f64 = @import("std").mem.zeroes(f64),
    direct_gear_mask: pwr_dct_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
    direct_clutch_clamp: [2]f64 = @import("std").mem.zeroes([2]f64),
};

pub const pwr_dct_powertrain_input = struct_pwr_dct_powertrain_input;

pub const struct_pwr_dct_powertrain_output = extern struct {
    time_s: f64 = @import("std").mem.zeroes(f64),
    tick: u64 = @import("std").mem.zeroes(u64),
    control_mode: pwr_dct_powertrain_control_mode = @import("std").mem.zeroes(pwr_dct_powertrain_control_mode),
    tcu_phase: pwr_dct_powertrain_tcu_phase = @import("std").mem.zeroes(pwr_dct_powertrain_tcu_phase),
    shift_progress: f64 = @import("std").mem.zeroes(f64),
    commanded_engine_throttle: f64 = @import("std").mem.zeroes(f64),
    commanded_starter_torque_nm: f64 = @import("std").mem.zeroes(f64),
    commanded_gear_mask: pwr_dct_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
    commanded_clutch_clamp: [2]f64 = @import("std").mem.zeroes([2]f64),
    engine: pwr_si_engine_output = @import("std").mem.zeroes(pwr_si_engine_output),
    dct: pwr_dct_snapshot = @import("std").mem.zeroes(pwr_dct_snapshot),
    exhaust: pwr_exhaust_observation = @import("std").mem.zeroes(pwr_exhaust_observation),
    exhaust_step: pwr_exhaust_diagnostics = @import("std").mem.zeroes(pwr_exhaust_diagnostics),
    halfshaft_angle_rad: f64 = @import("std").mem.zeroes(f64),
    halfshaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    halfshaft_drive_torque_nm: f64 = @import("std").mem.zeroes(f64),
    road_load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    halfshaft_kinetic_energy_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_halfshaft_drive_work_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_road_load_work_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_halfshaft_numerical_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    halfshaft_energy_residual_j: f64 = @import("std").mem.zeroes(f64),
    exhaust_mass_mismatch_kg: f64 = @import("std").mem.zeroes(f64),
    cumulative_exhaust_mass_mismatch_kg: f64 = @import("std").mem.zeroes(f64),
    exhaust_enthalpy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_exhaust_enthalpy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    engine_dct_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_engine_dct_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    dct_output_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_dct_output_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    last_step_diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    state_hash: u64 = @import("std").mem.zeroes(u64),
};

pub const pwr_dct_powertrain_output = struct_pwr_dct_powertrain_output;

pub const struct_pwr_dct_powertrain_state = extern struct {
    config: pwr_dct_powertrain_config = @import("std").mem.zeroes(pwr_dct_powertrain_config),
    engine: pwr_si_engine_state = @import("std").mem.zeroes(pwr_si_engine_state),
    dct: pwr_dct = @import("std").mem.zeroes(pwr_dct),
    exhaust: pwr_exhaust_state = @import("std").mem.zeroes(pwr_exhaust_state),
    engine_output: pwr_si_engine_output = @import("std").mem.zeroes(pwr_si_engine_output),
    dct_output: pwr_dct_snapshot = @import("std").mem.zeroes(pwr_dct_snapshot),
    exhaust_output: pwr_exhaust_observation = @import("std").mem.zeroes(pwr_exhaust_observation),
    exhaust_step: pwr_exhaust_diagnostics = @import("std").mem.zeroes(pwr_exhaust_diagnostics),
    tick: u64 = @import("std").mem.zeroes(u64),
    tcu_phase_tick: u64 = @import("std").mem.zeroes(u64),
    cranking_ticks: u64 = @import("std").mem.zeroes(u64),
    last_control_mode: pwr_dct_powertrain_control_mode = @import("std").mem.zeroes(pwr_dct_powertrain_control_mode),
    tcu_phase: pwr_dct_powertrain_tcu_phase = @import("std").mem.zeroes(pwr_dct_powertrain_tcu_phase),
    engine_started: bool = @import("std").mem.zeroes(bool),
    halfshaft_angle_rad: f64 = @import("std").mem.zeroes(f64),
    halfshaft_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    initial_halfshaft_kinetic_energy_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_halfshaft_drive_work_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_road_load_work_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_halfshaft_numerical_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    last_commanded_engine_throttle: f64 = @import("std").mem.zeroes(f64),
    last_commanded_starter_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_commanded_gear_mask: pwr_dct_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
    last_commanded_clutch_clamp: [2]f64 = @import("std").mem.zeroes([2]f64),
    last_road_load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    last_exhaust_mass_mismatch_kg: f64 = @import("std").mem.zeroes(f64),
    cumulative_exhaust_mass_mismatch_kg: f64 = @import("std").mem.zeroes(f64),
    last_exhaust_enthalpy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_exhaust_enthalpy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    last_engine_dct_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_engine_dct_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    last_dct_output_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    cumulative_dct_output_energy_mismatch_j: f64 = @import("std").mem.zeroes(f64),
    last_step_diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_dct_powertrain_state = struct_pwr_dct_powertrain_state;

pub const struct_pwr_dct_powertrain_commands = extern struct {
    engine_throttle: f64 = @import("std").mem.zeroes(f64),
    starter_torque_nm: f64 = @import("std").mem.zeroes(f64),
    gear_mask: pwr_dct_gear_mask = @import("std").mem.zeroes(pwr_dct_gear_mask),
    clutch_clamp: [2]f64 = @import("std").mem.zeroes([2]f64),
};

pub const pwr_dct_powertrain_commands = struct_pwr_dct_powertrain_commands;

pub const pwr_dct_powertrain_tick_s: f64 = 0.0001;

pub const PWR_SHAFT_OK: c_int = 0;

pub const PWR_SHAFT_INVALID_ARGUMENT: c_int = 1;

pub const PWR_SHAFT_INVALID_CONFIG: c_int = 2;

pub const PWR_SHAFT_SCHEDULER_ERROR: c_int = 3;

pub const PWR_SHAFT_NUMERIC_ERROR: c_int = 4;

pub const enum_pwr_shaft_result = c_uint;

pub const pwr_shaft_result = enum_pwr_shaft_result;

pub const PWR_SHAFT_FAULT_NONE: c_int = 0;

pub const PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS: c_int = 1;

pub const PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT: c_int = 2;

pub const PWR_SHAFT_FAULT_SIGNAL_DROP: c_int = 4;

pub const PWR_SHAFT_FAULT_ACTUATOR_STUCK: c_int = 8;

pub const PWR_SHAFT_FAULT_LV_BROWNOUT: c_int = 16;

pub const enum_pwr_shaft_fault = c_uint;

pub const pwr_shaft_fault = enum_pwr_shaft_fault;

pub const PWR_SHAFT_DIAG_NONE: c_int = 0;

pub const PWR_SHAFT_DIAG_SENSOR_INVALID: c_int = 1;

pub const PWR_SHAFT_DIAG_SENSOR_STALE: c_int = 2;

pub const PWR_SHAFT_DIAG_SIGNAL_QUEUE_OVERFLOW: c_int = 4;

pub const PWR_SHAFT_DIAG_CONTROLLER_BROWNOUT: c_int = 8;

pub const PWR_SHAFT_DIAG_DUTY_SATURATED: c_int = 16;

pub const PWR_SHAFT_DIAG_CURRENT_LIMITED: c_int = 32;

pub const PWR_SHAFT_DIAG_NUMERIC_FAILURE: c_int = 64;

pub const enum_pwr_shaft_diagnostic = c_uint;

pub const pwr_shaft_diagnostic = enum_pwr_shaft_diagnostic;

pub const struct_pwr_shaft_config = extern struct {
    base_tick_ns: u64 = @import("std").mem.zeroes(u64),
    sensor_period_ticks: u32 = @import("std").mem.zeroes(u32),
    control_period_ticks: u32 = @import("std").mem.zeroes(u32),
    signal_delay_ticks: u32 = @import("std").mem.zeroes(u32),
    max_sensor_age_ticks: u32 = @import("std").mem.zeroes(u32),
    controller_recovery_ticks: u32 = @import("std").mem.zeroes(u32),
    high_voltage_v: f64 = @import("std").mem.zeroes(f64),
    low_voltage_nominal_v: f64 = @import("std").mem.zeroes(f64),
    low_voltage_internal_resistance_ohm: f64 = @import("std").mem.zeroes(f64),
    controller_current_a: f64 = @import("std").mem.zeroes(f64),
    brownout_threshold_v: f64 = @import("std").mem.zeroes(f64),
    max_target_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    motor_resistance_ohm: f64 = @import("std").mem.zeroes(f64),
    motor_inductance_h: f64 = @import("std").mem.zeroes(f64),
    motor_torque_constant_nm_a: f64 = @import("std").mem.zeroes(f64),
    motor_back_emf_v_s_rad: f64 = @import("std").mem.zeroes(f64),
    motor_current_limit_a: f64 = @import("std").mem.zeroes(f64),
    motor_inertia_kg_m2: f64 = @import("std").mem.zeroes(f64),
    motor_viscous_friction_nm_s_rad: f64 = @import("std").mem.zeroes(f64),
    shaft_stiffness_nm_rad: f64 = @import("std").mem.zeroes(f64),
    shaft_damping_nm_s_rad: f64 = @import("std").mem.zeroes(f64),
    load_inertia_kg_m2: f64 = @import("std").mem.zeroes(f64),
    load_viscous_friction_nm_s_rad: f64 = @import("std").mem.zeroes(f64),
    controller_kp: f64 = @import("std").mem.zeroes(f64),
    controller_ki_per_s: f64 = @import("std").mem.zeroes(f64),
    duty_rate_limit_per_s: f64 = @import("std").mem.zeroes(f64),
    ambient_temperature_k: f64 = @import("std").mem.zeroes(f64),
    thermal_capacity_j_k: f64 = @import("std").mem.zeroes(f64),
    thermal_conductance_w_k: f64 = @import("std").mem.zeroes(f64),
    inverter_loss_fraction: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_shaft_config = struct_pwr_shaft_config;

pub const struct_pwr_shaft_input = extern struct {
    target_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    load_torque_nm: f64 = @import("std").mem.zeroes(f64),
    fault_mask: u32 = @import("std").mem.zeroes(u32),
    speed_sensor_bias_rad_s: f64 = @import("std").mem.zeroes(f64),
    actuator_stuck_duty: f64 = @import("std").mem.zeroes(f64),
};

pub const pwr_shaft_input = struct_pwr_shaft_input;

pub const struct_pwr_shaft_signal_slot = extern struct {
    delivery_tick: u64 = @import("std").mem.zeroes(u64),
    value: f64 = @import("std").mem.zeroes(f64),
    valid: bool = @import("std").mem.zeroes(bool),
    occupied: bool = @import("std").mem.zeroes(bool),
};

pub const pwr_shaft_signal_slot = struct_pwr_shaft_signal_slot;

pub const struct_pwr_shaft_snapshot = extern struct {
    time_s: f64 = @import("std").mem.zeroes(f64),
    motor_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    load_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    shaft_twist_rad: f64 = @import("std").mem.zeroes(f64),
    motor_current_a: f64 = @import("std").mem.zeroes(f64),
    commanded_duty: f64 = @import("std").mem.zeroes(f64),
    applied_duty: f64 = @import("std").mem.zeroes(f64),
    sensed_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    low_voltage_v: f64 = @import("std").mem.zeroes(f64),
    motor_temperature_k: f64 = @import("std").mem.zeroes(f64),
    electrical_input_j: f64 = @import("std").mem.zeroes(f64),
    mechanical_output_j: f64 = @import("std").mem.zeroes(f64),
    heat_rejected_j: f64 = @import("std").mem.zeroes(f64),
    stored_energy_change_j: f64 = @import("std").mem.zeroes(f64),
    numerical_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    energy_residual_j: f64 = @import("std").mem.zeroes(f64),
    control_energy_input_j: f64 = @import("std").mem.zeroes(f64),
    sensor_age_ticks: u64 = @import("std").mem.zeroes(u64),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    sensor_valid: bool = @import("std").mem.zeroes(bool),
    controller_active: bool = @import("std").mem.zeroes(bool),
    state_hash: u64 = @import("std").mem.zeroes(u64),
};

pub const pwr_shaft_snapshot = struct_pwr_shaft_snapshot;

pub const struct_pwr_shaft_lab = extern struct {
    config: pwr_shaft_config = @import("std").mem.zeroes(pwr_shaft_config),
    scheduler: pwr_scheduler = @import("std").mem.zeroes(pwr_scheduler),
    current_input: pwr_shaft_input = @import("std").mem.zeroes(pwr_shaft_input),
    signal_queue: [64]pwr_shaft_signal_slot = @import("std").mem.zeroes([64]pwr_shaft_signal_slot),
    motor_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    load_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    shaft_twist_rad: f64 = @import("std").mem.zeroes(f64),
    motor_current_a: f64 = @import("std").mem.zeroes(f64),
    motor_temperature_k: f64 = @import("std").mem.zeroes(f64),
    sensed_speed_rad_s: f64 = @import("std").mem.zeroes(f64),
    controller_integral: f64 = @import("std").mem.zeroes(f64),
    commanded_duty: f64 = @import("std").mem.zeroes(f64),
    applied_duty: f64 = @import("std").mem.zeroes(f64),
    low_voltage_v: f64 = @import("std").mem.zeroes(f64),
    initial_stored_energy_j: f64 = @import("std").mem.zeroes(f64),
    electrical_input_j: f64 = @import("std").mem.zeroes(f64),
    mechanical_output_j: f64 = @import("std").mem.zeroes(f64),
    heat_rejected_j: f64 = @import("std").mem.zeroes(f64),
    control_energy_input_j: f64 = @import("std").mem.zeroes(f64),
    limiter_adjustment_j: f64 = @import("std").mem.zeroes(f64),
    last_sensor_delivery_tick: u64 = @import("std").mem.zeroes(u64),
    controller_recovery_progress_ticks: u64 = @import("std").mem.zeroes(u64),
    diagnostic_flags: u32 = @import("std").mem.zeroes(u32),
    sensor_valid: bool = @import("std").mem.zeroes(bool),
    controller_active: bool = @import("std").mem.zeroes(bool),
};

pub const pwr_shaft_lab = struct_pwr_shaft_lab;

pub const PWR_SHAFT_TASK_SENSOR: c_int = 10;

pub const PWR_SHAFT_TASK_SIGNAL_DELIVERY: c_int = 20;

pub const PWR_SHAFT_TASK_CONTROLLER: c_int = 30;

pub const PWR_SHAFT_SIGNAL_QUEUE_CAPACITY = @as(c_uint, 64);

pub const struct_pwr_instance_backend = extern struct {
    submit: ?*const fn (?*anyopaque, [*c]const pwr_input_frame) callconv(.c) pwr_status = @import("std").mem.zeroes(?*const fn (?*anyopaque, [*c]const pwr_input_frame) callconv(.c) pwr_status),
    step: ?*const fn (?*anyopaque, u64) callconv(.c) pwr_status = @import("std").mem.zeroes(?*const fn (?*anyopaque, u64) callconv(.c) pwr_status),
    snapshot: ?*const fn (?*const anyopaque, [*c]pwr_snapshot) callconv(.c) pwr_status = @import("std").mem.zeroes(?*const fn (?*const anyopaque, [*c]pwr_snapshot) callconv(.c) pwr_status),
    destroy: ?*const fn (?*anyopaque) callconv(.c) void = @import("std").mem.zeroes(?*const fn (?*anyopaque) callconv(.c) void),
};

pub const pwr_instance_backend = struct_pwr_instance_backend;

pub const struct_pwr_context_slot = extern struct {
    generation: u32 = @import("std").mem.zeroes(u32),
    flags: u32 = @import("std").mem.zeroes(u32),
    active: u32 = @import("std").mem.zeroes(u32),
    reference_count: u32 = @import("std").mem.zeroes(u32),
};

pub const pwr_context_slot = struct_pwr_context_slot;

pub const struct_pwr_instance_slot = extern struct {
    generation: u32 = @import("std").mem.zeroes(u32),
    active: u32 = @import("std").mem.zeroes(u32),
    context: pwr_context = @import("std").mem.zeroes(pwr_context),
    busy: atomic_bool = @import("std").mem.zeroes(atomic_bool),
    model: pwr_model = @import("std").mem.zeroes(pwr_model),
    backend: [*c]const pwr_instance_backend = @import("std").mem.zeroes([*c]const pwr_instance_backend),
    state: ?*anyopaque = @import("std").mem.zeroes(?*anyopaque),
};

pub const pwr_instance_slot = struct_pwr_instance_slot;

pub const struct_pwr_model_slot = extern struct {
    generation: u32 = @import("std").mem.zeroes(u32),
    references: u32 = @import("std").mem.zeroes(u32),
    context: pwr_context = @import("std").mem.zeroes(pwr_context),
    graph: [*c]pwr_graph = @import("std").mem.zeroes([*c]pwr_graph),
};

pub const pwr_model_slot = struct_pwr_model_slot;

pub const struct_pwr_graph_runtime = extern struct {
    graph: [*c]const pwr_graph = @import("std").mem.zeroes([*c]const pwr_graph),
    state: pwr_graph_state = @import("std").mem.zeroes(pwr_graph_state),
};

pub const pwr_graph_runtime = struct_pwr_graph_runtime;

pub const struct_pwr_shaft_runtime = extern struct {
    shaft: pwr_shaft_lab = @import("std").mem.zeroes(pwr_shaft_lab),
    input: pwr_shaft_input = @import("std").mem.zeroes(pwr_shaft_input),
};

pub const pwr_shaft_runtime = struct_pwr_shaft_runtime;

pub const PWR_SHAFT_SNAPSHOT_VALUE_COUNT: c_int = 17;

pub const atomic_bool = bool;

pub const atomic_flag = bool;

pub const PWR_VERSION_MAJOR = @as(u32, 0);

pub const PWR_VERSION_MINOR = @as(u32, 2);

pub const PWR_VERSION_PATCH = @as(u32, 0);

pub const PWR_CONTEXT_INVALID = @as(u64, 0);

pub const PWR_INSTANCE_INVALID = @as(u64, 0);

pub const PWR_CAP_CONTEXT_LIFECYCLE = (@as(u64, 1) << 0);

pub const PWR_CAP_GENERATION_HANDLES = (@as(u64, 1) << 1);

pub const PWR_CAP_THREAD_SAFE_LIFECYCLE = (@as(u64, 1) << 2);

pub const PWR_CAP_HEADLESS_FIXED_STEP = (@as(u64, 1) << 3);

pub const PWR_CAP_BATCH_SCALAR_IO = (@as(u64, 1) << 4);

pub const PWR_CAP_CONTROLLED_SHAFT_PROTOTYPE = (@as(u64, 1) << 5);

pub const PWR_CAP_COMPILED_MODELS = (@as(u64, 1) << 6);

pub const PWR_CAP_MODEL_CHANNEL_DISCOVERY = (@as(u64, 1) << 7);

pub const PWR_INPUT_TARGET_SPEED_RAD_S = @as(u64, 1);

pub const PWR_INPUT_LOAD_TORQUE_NM = @as(u64, 2);

pub const PWR_INPUT_SPEED_SENSOR_BIAS_RAD_S = @as(u64, 3);

pub const PWR_INPUT_SPEED_SENSOR_DROPOUT = @as(u64, 4);

pub const PWR_INPUT_SIGNAL_DROP = @as(u64, 5);

pub const PWR_INPUT_ACTUATOR_STUCK_ENABLE = @as(u64, 6);

pub const PWR_INPUT_ACTUATOR_STUCK_DUTY = @as(u64, 7);

pub const PWR_INPUT_LOW_VOLTAGE_BROWNOUT = @as(u64, 8);

pub const PWR_OUTPUT_MOTOR_SPEED_RAD_S = @as(u64, 1001);

pub const PWR_OUTPUT_LOAD_SPEED_RAD_S = @as(u64, 1002);

pub const PWR_OUTPUT_SHAFT_TWIST_RAD = @as(u64, 1003);

pub const PWR_OUTPUT_MOTOR_CURRENT_A = @as(u64, 1004);

pub const PWR_OUTPUT_COMMANDED_DUTY = @as(u64, 1005);

pub const PWR_OUTPUT_APPLIED_DUTY = @as(u64, 1006);

pub const PWR_OUTPUT_SENSED_SPEED_RAD_S = @as(u64, 1007);

pub const PWR_OUTPUT_LOW_VOLTAGE_V = @as(u64, 1008);

pub const PWR_OUTPUT_MOTOR_TEMPERATURE_K = @as(u64, 1009);

pub const PWR_OUTPUT_ELECTRICAL_INPUT_J = @as(u64, 1010);

pub const PWR_OUTPUT_MECHANICAL_OUTPUT_J = @as(u64, 1011);

pub const PWR_OUTPUT_HEAT_REJECTED_J = @as(u64, 1012);

pub const PWR_OUTPUT_STORED_ENERGY_CHANGE_J = @as(u64, 1013);

pub const PWR_OUTPUT_NUMERICAL_ADJUSTMENT_J = @as(u64, 1014);

pub const PWR_OUTPUT_ENERGY_RESIDUAL_J = @as(u64, 1015);

pub const PWR_OUTPUT_SENSOR_AGE_TICKS = @as(u64, 1016);

pub const PWR_OUTPUT_CONTROLLER_ACTIVE = @as(u64, 1017);

pub const PWR_ABI_VERSION = @as(u32, 1);

pub const PWR_MODEL_SCHEMA_VERSION = @as(u32, 1);

pub const PWR_MODEL_MAX_NODES = @as(u32, 32);

pub const PWR_MODEL_MAX_COMPONENTS = @as(u32, 64);

pub const PWR_MODEL_MAX_STATES = @as(u32, 64);

pub const PWR_MODEL_INVALID = @as(u64, 0);

pub const PWR_MAX_CONTEXTS = @as(u32, 256);

pub const PWR_MAX_INSTANCES = @as(u32, 256);

pub const PWR_MAX_MODELS = @as(u32, 64);

pub const PWR_DCT_GEAR_MASK_REVERSE = @as(u16, 0x01);

pub const PWR_DCT_GEAR_MASK_1 = @as(u16, 0x02);

pub const PWR_DCT_GEAR_MASK_2 = @as(u16, 0x04);

pub const PWR_DCT_GEAR_MASK_3 = @as(u16, 0x08);

pub const PWR_DCT_GEAR_MASK_4 = @as(u16, 0x10);

pub const PWR_DCT_GEAR_MASK_5 = @as(u16, 0x20);

pub const PWR_DCT_GEAR_MASK_6 = @as(u16, 0x40);

pub const PWR_DCT_GEAR_MASK_7 = @as(u16, 0x80);

pub const PWR_DCT_GEAR_MASK_K1 = @as(u16, 0xaa);

pub const PWR_DCT_GEAR_MASK_K2 = @as(u16, 0x55);

pub const PWR_DCT_GEAR_MASK_ALL = @as(u16, 0xff);

pub const PWR_DCT_POWERTRAIN_BASE_TICK_NS = @as(u64, 100000);

pub const PWR_AUTOMATIC_POWERTRAIN_BASE_TICK_NS = @as(u64, 100000);
