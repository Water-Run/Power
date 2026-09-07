// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#ifndef POWER_MODEL_H
#define POWER_MODEL_H

#include <stdint.h>

#define PWR_ABI_VERSION UINT32_C(1)

/* Data-only model IR. No callbacks, native code, or model-provider dependency.
 * All fields and units are validated at compile time; descriptors can then die.
 * Changing an element layout requires a new schema_version, not just ABI size.
 */
#define PWR_MODEL_SCHEMA_VERSION UINT32_C(1)
#define PWR_MODEL_MAX_NODES UINT32_C(32)
#define PWR_MODEL_MAX_COMPONENTS UINT32_C(64)
#define PWR_MODEL_MAX_STATES UINT32_C(64)

typedef uint64_t pwr_model;
#define PWR_MODEL_INVALID UINT64_C(0)

enum {
    PWR_UNIT_NONE = 0,
    PWR_UNIT_KG_M2 = 1,
    PWR_UNIT_RAD = 2,
    PWR_UNIT_RAD_S = 3,
    PWR_UNIT_NM = 4,
    PWR_UNIT_NM_RAD = 5,
    PWR_UNIT_NM_S_RAD = 6,
    PWR_UNIT_K = 7,
    PWR_UNIT_J_K = 8,
    PWR_UNIT_W_K = 9,
    PWR_UNIT_OHM = 10,
    PWR_UNIT_H = 11,
    PWR_UNIT_NM_A = 12,
    PWR_UNIT_A = 13,
    PWR_UNIT_V = 14,
    PWR_UNIT_J = 15,
    PWR_UNIT_RPM = 16,
    PWR_UNIT_DEG = 17
};

typedef struct pwr_quantity {
    double value;
    uint32_t unit;
    uint32_t reserved;
} pwr_quantity;

#define PWR_QUANTITY(value_, unit_) { (value_), (unit_), UINT32_C(0) }

enum {
    PWR_NODE_ROTATIONAL = 1,
    PWR_NODE_THERMAL = 2
};

typedef struct pwr_node_desc {
    /* IDs are nonzero and unique across both nodes and components. */
    uint32_t id;
    uint32_t domain;
    pwr_quantity storage;  /* rotational: kg m^2; thermal: J/K */
    pwr_quantity initial;  /* rotational: rad/s or rpm; thermal: K */
    pwr_quantity position; /* rotational: rad or deg; thermal: zero/NONE */
} pwr_node_desc;

enum {
    PWR_COMPONENT_SHAFT = 1,
    PWR_COMPONENT_DC_MOTOR = 2,
    PWR_COMPONENT_TORQUE_SOURCE = 3,
    PWR_COMPONENT_THERMAL_LINK = 4
};

typedef union pwr_component_parameters {
    struct {
        pwr_quantity stiffness;
        pwr_quantity damping;
        pwr_quantity rest_angle;
        /* twist = angle_a - ratio * angle_b - rest_angle.
         * Signed finite nonzero ratio; this is an elastic transmission,
         * not a rigid gear constraint. With node_b == 0, ratio must be 1. */
        double ratio;
    } shaft;
    struct {
        pwr_quantity resistance;
        pwr_quantity inductance;
        /* One SI constant for both torque/current and back EMF/speed. */
        pwr_quantity coupling;
        pwr_quantity initial_current;
    } motor;
    struct {
        pwr_quantity conductance;
        /* K when node_b == 0; zero/NONE for an internal connection. */
        pwr_quantity ambient_temperature;
    } thermal;
} pwr_component_parameters;

typedef struct pwr_component_desc {
    uint32_t id;
    uint32_t kind;
    uint32_t node_a;
    uint32_t node_b;    /* zero = mechanical ground or thermal ambient */
    uint32_t heat_node; /* shaft/motor loss sink; zero = external heat */
    uint32_t reserved;
    /* Motor voltage or source torque. Unique, nonzero, high bit clear.
     * Other components must use zero and zero/NONE initial_input. */
    uint64_t input_channel;
    pwr_quantity initial_input;
    pwr_component_parameters parameters;
} pwr_component_desc;

typedef struct pwr_model_desc {
    uint32_t abi_version;
    uint32_t struct_size;
    uint32_t schema_version;
    uint32_t flags;
    uint64_t step_ns; /* 1 ns .. 1 s; fixed at compilation */
    const pwr_node_desc *nodes;
    uint32_t node_count;
    uint32_t component_count;
    const pwr_component_desc *components;
} pwr_model_desc;

#define PWR_MODEL_DESC_INIT                                                     \
    { PWR_ABI_VERSION, (uint32_t)sizeof(pwr_model_desc),                        \
      PWR_MODEL_SCHEMA_VERSION, 0, UINT64_C(100000),                           \
      (const pwr_node_desc *)0, 0, 0, (const pwr_component_desc *)0 }

enum {
    PWR_MODEL_DIAG_NONE = 0,
    PWR_MODEL_DIAG_SCHEMA = 1,
    PWR_MODEL_DIAG_CAPACITY = 2,
    PWR_MODEL_DIAG_ID = 3,
    PWR_MODEL_DIAG_UNIT = 4,
    PWR_MODEL_DIAG_RANGE = 5,
    PWR_MODEL_DIAG_CONNECTION = 6,
    PWR_MODEL_DIAG_CHANNEL = 7,
    PWR_MODEL_DIAG_SOLVER = 8
};

typedef struct pwr_model_diagnostic {
    uint32_t abi_version;
    uint32_t struct_size;
    uint32_t code;
    uint32_t object_id;
    char field[40];
    char message[160];
} pwr_model_diagnostic;

#define PWR_MODEL_DIAGNOSTIC_INIT                                               \
    { PWR_ABI_VERSION, (uint32_t)sizeof(pwr_model_diagnostic), 0, 0, {0}, {0} }

enum {
    PWR_MODEL_FIDELITY_LINEAR_LUMPED = 1,
    PWR_MODEL_VALIDATION_UNVERIFIED = 0
};

typedef struct pwr_model_info {
    uint32_t abi_version;
    uint32_t struct_size;
    uint64_t fingerprint;
    uint64_t step_ns;
    uint32_t node_count;
    uint32_t component_count;
    uint32_t state_count;
    uint32_t channel_count;
    uint32_t fidelity;
    uint32_t validation;
} pwr_model_info;

#define PWR_MODEL_INFO_INIT                                                     \
    { PWR_ABI_VERSION, (uint32_t)sizeof(pwr_model_info), 0, 0, 0, 0, 0, 0, 0, 0 }

enum {
    PWR_CHANNEL_INPUT = 1,
    PWR_CHANNEL_OUTPUT = 2,
    PWR_FIELD_ANGLE = 1,
    PWR_FIELD_SPEED = 2,
    PWR_FIELD_TEMPERATURE = 3,
    PWR_FIELD_CURRENT = 4,
    PWR_FIELD_TWIST = 5,
    PWR_FIELD_TORQUE = 6,
    /* Object zero is reserved for the whole-model energy ledger. */
    PWR_FIELD_SOURCE_WORK = 16,
    PWR_FIELD_HEAT_REJECTED = 17,
    PWR_FIELD_STORED_ENERGY_CHANGE = 18,
    PWR_FIELD_ENERGY_RESIDUAL = 19
};

#define PWR_MODEL_CHANNEL(object_, field_)                                     \
    (UINT64_C(0x8000000000000000) | ((uint64_t)(object_) << 8) | (uint64_t)(field_))

typedef struct pwr_channel_info {
    uint64_t channel;
    uint32_t object_id;
    uint32_t direction;
    uint32_t unit; /* Input and output values always use canonical SI units. */
    uint32_t reserved;
    char quantity[32];
} pwr_channel_info;

#endif
