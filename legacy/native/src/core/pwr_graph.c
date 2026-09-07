// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#include "pwr_graph.h"

#include <float.h>
#include <math.h>
#include <stddef.h>
#include <stdio.h>
#include <string.h>

static pwr_status pwr_graph_error(pwr_model_diagnostic *d, uint32_t code,
                                  uint32_t id, const char *field,
                                  const char *message, pwr_status status)
{
    if (d != NULL) {
        d->code = code;
        d->object_id = id;
        (void)snprintf(d->field, sizeof(d->field), "%s", field);
        (void)snprintf(d->message, sizeof(d->message), "%s", message);
    }
    return status;
}

static pwr_status pwr_graph_quantity(pwr_quantity q, uint32_t unit, double *out,
                                     pwr_model_diagnostic *d, uint32_t id,
                                     const char *field)
{
    double scale = 1.0;
    if (q.reserved != 0U) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_SCHEMA, id, field,
                               "Reserved quantity field must be zero.",
                               PWR_STATUS_UNSUPPORTED);
    }
    if (q.unit != unit) {
        if (unit == PWR_UNIT_RAD_S && q.unit == PWR_UNIT_RPM) {
            scale = 0.10471975511965977462;
        }
        else if (unit == PWR_UNIT_RAD && q.unit == PWR_UNIT_DEG) {
            scale = 0.01745329251994329577;
        }
        else {
            return pwr_graph_error(d, PWR_MODEL_DIAG_UNIT, id, field,
                                   "Quantity has an incompatible unit.",
                                   PWR_STATUS_INVALID_MODEL);
        }
    }
    const double value = q.value * scale;
    if (!isfinite(value) || (unit == PWR_UNIT_NONE && value != 0.0)) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_RANGE, id, field,
                               "Quantity must be finite; unused quantities must be zero/NONE.",
                               PWR_STATUS_INVALID_MODEL);
    }
    *out = value == 0.0 ? 0.0 : value;
    return PWR_STATUS_OK;
}

static pwr_status pwr_graph_node_compile(const pwr_node_desc *src,
                                        pwr_graph_node *node,
                                        pwr_model_diagnostic *d)
{
    pwr_status status;
    node->id = src->id;
    node->domain = src->domain;
    if (node->id == 0U) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_ID, 0, "id",
                               "Object zero is reserved for boundaries and the ledger.",
                               PWR_STATUS_INVALID_MODEL);
    }
    if (node->domain != PWR_NODE_ROTATIONAL && node->domain != PWR_NODE_THERMAL) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_SCHEMA, node->id, "domain",
                               "This compiler supports rotational and thermal storage nodes.",
                               PWR_STATUS_UNSUPPORTED);
    }
    const bool rotor = node->domain == PWR_NODE_ROTATIONAL;
    status = pwr_graph_quantity(src->storage, rotor ? PWR_UNIT_KG_M2 : PWR_UNIT_J_K,
                                 &node->storage, d, node->id, "storage");
    if (status != PWR_STATUS_OK) { return status; }
    status = pwr_graph_quantity(src->initial, rotor ? PWR_UNIT_RAD_S : PWR_UNIT_K,
                                 &node->initial, d, node->id, "initial");
    if (status != PWR_STATUS_OK) { return status; }
    status = pwr_graph_quantity(src->position, rotor ? PWR_UNIT_RAD : PWR_UNIT_NONE,
                                 &node->position, d, node->id, "position");
    if (status != PWR_STATUS_OK) { return status; }
    if (!(node->storage > 0.0) || (!rotor && !(node->initial > 0.0))) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_RANGE, node->id,
                               node->storage <= 0.0 ? "storage" : "initial",
                               "Storage and absolute temperature must be positive.",
                               PWR_STATUS_INVALID_MODEL);
    }
    return PWR_STATUS_OK;
}

static uint32_t pwr_graph_find_node(const pwr_graph *g, uint32_t id)
{
    for (uint32_t i = 0; i < g->node_count; ++i) {
        if (g->nodes[i].id == id) { return i; }
    }
    return PWR_GRAPH_NO_INDEX;
}

static bool pwr_graph_has_domain(const pwr_graph *g, uint32_t index, uint32_t domain)
{
    return index != PWR_GRAPH_NO_INDEX && g->nodes[index].domain == domain;
}

static pwr_status pwr_graph_component_compile(const pwr_graph *g,
                                             const pwr_component_desc *src,
                                             pwr_graph_component *c,
                                             pwr_model_diagnostic *d)
{
    pwr_status status;
    c->id = src->id;
    c->kind = src->kind;
    c->a = pwr_graph_find_node(g, src->node_a);
    c->b = pwr_graph_find_node(g, src->node_b);
    c->heat = pwr_graph_find_node(g, src->heat_node);
    c->state_index = PWR_GRAPH_NO_INDEX;
    c->input_channel = src->input_channel;
    if (c->id == 0U || pwr_graph_find_node(g, c->id) != PWR_GRAPH_NO_INDEX) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_ID, c->id, "id",
                               "Component ID must be nonzero and distinct from node IDs.",
                               PWR_STATUS_INVALID_MODEL);
    }
    if (src->reserved != 0U || c->kind < PWR_COMPONENT_SHAFT ||
        c->kind > PWR_COMPONENT_THERMAL_LINK) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_SCHEMA, c->id, "kind",
                               "Unknown component kind or nonzero reserved field.",
                               PWR_STATUS_UNSUPPORTED);
    }
    const bool thermal = c->kind == PWR_COMPONENT_THERMAL_LINK;
    const bool pair = thermal || c->kind == PWR_COMPONENT_SHAFT;
    const uint32_t domain = thermal ? PWR_NODE_THERMAL : PWR_NODE_ROTATIONAL;
    if (!pwr_graph_has_domain(g, c->a, domain)) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_CONNECTION, c->id, "node_a",
                               "Required node is missing or belongs to a different domain.",
                               PWR_STATUS_INVALID_MODEL);
    }
    if ((src->node_b != 0U && (!pair || !pwr_graph_has_domain(g, c->b, domain))) ||
        (src->node_b != 0U && c->b == c->a)) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_CONNECTION, c->id, "node_b",
                               "Connection must reference a distinct node of the required domain.",
                               PWR_STATUS_INVALID_MODEL);
    }
    if (src->heat_node != 0U &&
        ((c->kind != PWR_COMPONENT_SHAFT && c->kind != PWR_COMPONENT_DC_MOTOR) ||
         !pwr_graph_has_domain(g, c->heat, PWR_NODE_THERMAL))) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_CONNECTION, c->id, "heat_node",
                               "Only shaft and motor losses can target a thermal storage node.",
                               PWR_STATUS_INVALID_MODEL);
    }
    const bool input = c->kind == PWR_COMPONENT_DC_MOTOR ||
                       c->kind == PWR_COMPONENT_TORQUE_SOURCE;
    if ((input && (c->input_channel == 0U || (c->input_channel >> 63) != 0U)) ||
        (!input && c->input_channel != 0U)) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_CHANNEL, c->id, "input_channel",
                               "Sources require a nonzero channel with its high bit clear; other components require zero.",
                               PWR_STATUS_INVALID_MODEL);
    }
    status = pwr_graph_quantity(src->initial_input,
                                 input ? (c->kind == PWR_COMPONENT_DC_MOTOR ?
                                          PWR_UNIT_V : PWR_UNIT_NM) : PWR_UNIT_NONE,
                                 &c->initial_input, d, c->id, "initial_input");
    if (status != PWR_STATUS_OK) { return status; }

#define READ_PARAMETER(quantity_, unit_, index_, field_)                        \
    do {                                                                        \
        status = pwr_graph_quantity((quantity_), (unit_), &c->p[(index_)],      \
                                     d, c->id, (field_));                       \
        if (status != PWR_STATUS_OK) { return status; }                         \
    } while (0)

    if (c->kind == PWR_COMPONENT_SHAFT) {
        READ_PARAMETER(src->parameters.shaft.stiffness, PWR_UNIT_NM_RAD, 0, "shaft.stiffness");
        READ_PARAMETER(src->parameters.shaft.damping, PWR_UNIT_NM_S_RAD, 1, "shaft.damping");
        READ_PARAMETER(src->parameters.shaft.rest_angle, PWR_UNIT_RAD, 2, "shaft.rest_angle");
        c->p[3] = src->parameters.shaft.ratio;
        if (c->p[0] < 0.0 || c->p[1] < 0.0 || !isfinite(c->p[3]) || c->p[3] == 0.0 ||
            (src->node_b == 0U && c->p[3] != 1.0)) {
            return pwr_graph_error(d, PWR_MODEL_DIAG_RANGE, c->id, "shaft",
                                   "Stiffness/damping must be nonnegative; ratio must be finite and nonzero (one at ground).",
                                   PWR_STATUS_INVALID_MODEL);
        }
    }
    else if (c->kind == PWR_COMPONENT_DC_MOTOR) {
        READ_PARAMETER(src->parameters.motor.resistance, PWR_UNIT_OHM, 0, "motor.resistance");
        READ_PARAMETER(src->parameters.motor.inductance, PWR_UNIT_H, 1, "motor.inductance");
        READ_PARAMETER(src->parameters.motor.coupling, PWR_UNIT_NM_A, 2, "motor.coupling");
        READ_PARAMETER(src->parameters.motor.initial_current, PWR_UNIT_A, 3, "motor.initial_current");
        if (c->p[0] < 0.0 || !(c->p[1] > 0.0)) {
            return pwr_graph_error(d, PWR_MODEL_DIAG_RANGE, c->id, "motor",
                                   "Resistance must be nonnegative and inductance positive.",
                                   PWR_STATUS_INVALID_MODEL);
        }
    }
    else if (thermal) {
        READ_PARAMETER(src->parameters.thermal.conductance, PWR_UNIT_W_K, 0, "thermal.conductance");
        READ_PARAMETER(src->parameters.thermal.ambient_temperature,
                       src->node_b == 0U ? PWR_UNIT_K : PWR_UNIT_NONE, 1,
                       "thermal.ambient_temperature");
        if (c->p[0] < 0.0 || (src->node_b == 0U && !(c->p[1] > 0.0))) {
            return pwr_graph_error(d, PWR_MODEL_DIAG_RANGE, c->id, "thermal",
                                   "Conductance must be nonnegative and ambient temperature positive.",
                                   PWR_STATUS_INVALID_MODEL);
        }
    }
#undef READ_PARAMETER
    return PWR_STATUS_OK;
}

/* Scaled, deterministic partial pivoting. Factors are shared by all instances.
 * The fixed schema is linear, so no factorization occurs in the step loop. */
static bool pwr_graph_factor(pwr_graph_matrix *m)
{
    double scale[PWR_MODEL_MAX_STATES] = {0};
    for (uint32_t row = 0; row < m->size; ++row) {
        for (uint32_t col = 0; col < m->size; ++col) {
            const double v = fabs(m->lu[row][col]);
            if (!isfinite(v)) { return false; }
            scale[row] = fmax(scale[row], v);
        }
        if (scale[row] == 0.0) { return false; }
    }
    for (uint32_t k = 0; k < m->size; ++k) {
        uint32_t pivot = k;
        for (uint32_t row = k + 1U; row < m->size; ++row) {
            if (fabs(m->lu[row][k]) / scale[row] > fabs(m->lu[pivot][k]) / scale[pivot]) {
                pivot = row;
            }
        }
        if (fabs(m->lu[pivot][k]) / scale[pivot] < 64.0 * DBL_EPSILON) { return false; }
        m->pivots[k] = pivot;
        if (pivot != k) {
            const double tmp_scale = scale[k];
            scale[k] = scale[pivot];
            scale[pivot] = tmp_scale;
            for (uint32_t col = 0; col < m->size; ++col) {
                const double tmp = m->lu[k][col];
                m->lu[k][col] = m->lu[pivot][col];
                m->lu[pivot][col] = tmp;
            }
        }
        for (uint32_t row = k + 1U; row < m->size; ++row) {
            m->lu[row][k] /= m->lu[k][k];
            if (!isfinite(m->lu[row][k])) { return false; }
            for (uint32_t col = k + 1U; col < m->size; ++col) {
                m->lu[row][col] -= m->lu[row][k] * m->lu[k][col];
                if (!isfinite(m->lu[row][col])) { return false; }
            }
        }
    }
    return true;
}

static bool pwr_graph_solve(const pwr_graph_matrix *m, double *rhs)
{
    for (uint32_t k = 0; k < m->size; ++k) {
        const double tmp = rhs[k];
        rhs[k] = rhs[m->pivots[k]];
        rhs[m->pivots[k]] = tmp;
    }
    for (uint32_t row = 0; row < m->size; ++row) {
        for (uint32_t col = 0; col < row; ++col) {
            rhs[row] -= m->lu[row][col] * rhs[col];
        }
    }
    for (uint32_t end = m->size; end > 0U; --end) {
        const uint32_t row = end - 1U;
        for (uint32_t col = row + 1U; col < m->size; ++col) {
            rhs[row] -= m->lu[row][col] * rhs[col];
        }
        rhs[row] /= m->lu[row][row];
        if (!isfinite(rhs[row])) { return false; }
    }
    return true;
}

static uint64_t pwr_graph_hash_word(uint64_t hash, uint64_t word)
{
    for (uint32_t i = 0; i < 8U; ++i) {
        hash = (hash ^ (word & UINT64_C(255))) * UINT64_C(1099511628211);
        word >>= 8;
    }
    return hash;
}

static uint64_t pwr_graph_hash_double(uint64_t hash, double value)
{
    uint64_t bits = 0;
    const double normalized = value == 0.0 ? 0.0 : value;
    _Static_assert(sizeof(double) == sizeof(uint64_t), "Graph hashing requires binary64 doubles");
    _Static_assert(FLT_RADIX == 2 && DBL_MANT_DIG == 53 && DBL_MAX_EXP == 1024,
                   "Graph hashing requires binary64 doubles");
    (void)memcpy(&bits, &normalized, sizeof(bits));
    return pwr_graph_hash_word(hash, bits);
}

static uint64_t pwr_graph_fingerprint(const pwr_graph *g)
{
    uint64_t hash = UINT64_C(14695981039346656037);
    hash = pwr_graph_hash_word(hash, PWR_MODEL_SCHEMA_VERSION);
    hash = pwr_graph_hash_word(hash, UINT64_C(1)); /* solver implementation version */
    hash = pwr_graph_hash_word(hash, g->step_ns);
    hash = pwr_graph_hash_word(hash, g->node_count);
    hash = pwr_graph_hash_word(hash, g->component_count);
    for (uint32_t i = 0; i < g->node_count; ++i) {
        const pwr_graph_node *n = &g->nodes[i];
        hash = pwr_graph_hash_word(hash, n->id);
        hash = pwr_graph_hash_word(hash, n->domain);
        hash = pwr_graph_hash_double(hash, n->storage);
        hash = pwr_graph_hash_double(hash, n->initial);
        hash = pwr_graph_hash_double(hash, n->position);
    }
    for (uint32_t i = 0; i < g->component_count; ++i) {
        const pwr_graph_component *c = &g->components[i];
        hash = pwr_graph_hash_word(hash, c->id);
        hash = pwr_graph_hash_word(hash, c->kind);
        hash = pwr_graph_hash_word(hash, c->a);
        hash = pwr_graph_hash_word(hash, c->b);
        hash = pwr_graph_hash_word(hash, c->heat);
        hash = pwr_graph_hash_word(hash, c->input_channel);
        hash = pwr_graph_hash_double(hash, c->initial_input);
        for (uint32_t p = 0; p < 4U; ++p) { hash = pwr_graph_hash_double(hash, c->p[p]); }
    }
    return hash;
}

static void pwr_graph_channel(pwr_graph *g, uint64_t channel, uint32_t object,
                               uint32_t direction, uint32_t unit, const char *quantity)
{
    pwr_channel_info *info = &g->channels[g->channel_count++];
    info->channel = channel;
    info->object_id = object;
    info->direction = direction;
    info->unit = unit;
    (void)snprintf(info->quantity, sizeof(info->quantity), "%s", quantity);
    if (direction == PWR_CHANNEL_OUTPUT) { ++g->output_count; }
}

static void pwr_graph_output_channel(pwr_graph *g, uint32_t object, uint32_t field,
                                      uint32_t unit, const char *quantity)
{
    pwr_graph_channel(g, PWR_MODEL_CHANNEL(object, field), object,
                       PWR_CHANNEL_OUTPUT, unit, quantity);
}

static void pwr_graph_assemble(pwr_graph *g)
{
    /* Temporarily assemble x' = A x + f in dynamics.lu. */
    double (*a)[PWR_MODEL_MAX_STATES] = g->dynamics.lu;
    double (*t)[PWR_MODEL_MAX_STATES] = g->thermal.lu;
    for (uint32_t i = 0; i < g->node_count; ++i) {
        const pwr_graph_node *n = &g->nodes[i];
        const uint32_t s = n->state_index;
        if (n->domain == PWR_NODE_ROTATIONAL) {
            a[s][s + 1U] = 1.0;
            pwr_graph_output_channel(g, n->id, PWR_FIELD_ANGLE, PWR_UNIT_RAD, "angle");
            pwr_graph_output_channel(g, n->id, PWR_FIELD_SPEED, PWR_UNIT_RAD_S, "speed");
        }
        else {
            t[s][s] = n->storage;
            pwr_graph_output_channel(g, n->id, PWR_FIELD_TEMPERATURE, PWR_UNIT_K, "temperature");
        }
    }
    for (uint32_t i = 0; i < g->component_count; ++i) {
        const pwr_graph_component *c = &g->components[i];
        const pwr_graph_node *na = &g->nodes[c->a];
        const uint32_t sa = na->state_index;
        const uint32_t sb = c->b == PWR_GRAPH_NO_INDEX ? PWR_GRAPH_NO_INDEX : g->nodes[c->b].state_index;
        if (c->kind == PWR_COMPONENT_SHAFT) {
            const uint32_t indices[2] = {sa, sb};
            const double direction[2] = {1.0, -c->p[3]};
            const uint32_t count = sb == PWR_GRAPH_NO_INDEX ? 1U : 2U;
            for (uint32_t row = 0; row < count; ++row) {
                const double inertia = g->nodes[row == 0U ? c->a : c->b].storage;
                const uint32_t velocity = indices[row] + 1U;
                g->constant_force[velocity] += direction[row] * c->p[0] * c->p[2] / inertia;
                for (uint32_t col = 0; col < count; ++col) {
                    const double factor = direction[row] * direction[col] / inertia;
                    a[velocity][indices[col]] -= factor * c->p[0];
                    a[velocity][indices[col] + 1U] -= factor * c->p[1];
                }
            }
            pwr_graph_output_channel(g, c->id, PWR_FIELD_TWIST, PWR_UNIT_RAD, "twist");
            pwr_graph_output_channel(g, c->id, PWR_FIELD_TORQUE, PWR_UNIT_NM, "reaction_torque_at_a");
        }
        else if (c->kind == PWR_COMPONENT_DC_MOTOR) {
            const uint32_t current = c->state_index;
            a[sa + 1U][current] += c->p[2] / na->storage;
            a[current][sa + 1U] -= c->p[2] / c->p[1];
            a[current][current] -= c->p[0] / c->p[1];
            pwr_graph_channel(g, c->input_channel, c->id, PWR_CHANNEL_INPUT, PWR_UNIT_V, "voltage");
            pwr_graph_output_channel(g, c->id, PWR_FIELD_CURRENT, PWR_UNIT_A, "current");
            pwr_graph_output_channel(g, c->id, PWR_FIELD_TORQUE, PWR_UNIT_NM, "motor_torque");
        }
        else if (c->kind == PWR_COMPONENT_TORQUE_SOURCE) {
            pwr_graph_channel(g, c->input_channel, c->id, PWR_CHANNEL_INPUT, PWR_UNIT_NM, "torque");
        }
        else {
            const double conductance_dt = g->dt * c->p[0];
            t[sa][sa] += conductance_dt;
            if (sb == PWR_GRAPH_NO_INDEX) {
                g->ambient_force[sa] += conductance_dt * c->p[1];
            }
            else {
                t[sa][sb] -= conductance_dt;
                t[sb][sa] -= conductance_dt;
                t[sb][sb] += conductance_dt;
            }
        }
    }
    for (uint32_t row = 0; row < g->dynamic_count; ++row) {
        for (uint32_t col = 0; col < g->dynamic_count; ++col) {
            a[row][col] = (row == col ? 1.0 : 0.0) - 0.5 * g->dt * a[row][col];
        }
    }
    g->dynamics.size = g->dynamic_count;
    g->thermal.size = g->thermal_count;
    pwr_graph_output_channel(g, 0, PWR_FIELD_SOURCE_WORK, PWR_UNIT_J, "source_work");
    pwr_graph_output_channel(g, 0, PWR_FIELD_HEAT_REJECTED, PWR_UNIT_J, "heat_rejected");
    pwr_graph_output_channel(g, 0, PWR_FIELD_STORED_ENERGY_CHANGE, PWR_UNIT_J, "stored_energy_change");
    pwr_graph_output_channel(g, 0, PWR_FIELD_ENERGY_RESIDUAL, PWR_UNIT_J, "energy_residual");
}

pwr_status pwr_graph_compile(const pwr_model_desc *desc, pwr_graph *g,
                             pwr_model_diagnostic *d)
{
    if (d != NULL) {
        if (d->abi_version != PWR_ABI_VERSION) { return PWR_STATUS_UNSUPPORTED_ABI; }
        if (d->struct_size < sizeof(*d)) { return PWR_STATUS_STRUCT_TOO_SMALL; }
        *d = (pwr_model_diagnostic)PWR_MODEL_DIAGNOSTIC_INIT;
    }
    if (desc == NULL || g == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    if (desc->abi_version != PWR_ABI_VERSION) { return PWR_STATUS_UNSUPPORTED_ABI; }
    if (desc->struct_size < sizeof(*desc)) { return PWR_STATUS_STRUCT_TOO_SMALL; }
    if (desc->schema_version != PWR_MODEL_SCHEMA_VERSION || desc->flags != 0U) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_SCHEMA, 0, "schema_version/flags",
                               "Unsupported model schema or flags.", PWR_STATUS_UNSUPPORTED);
    }
    if (desc->node_count > PWR_MODEL_MAX_NODES || desc->component_count > PWR_MODEL_MAX_COMPONENTS) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_CAPACITY, 0, "counts",
                               "Model exceeds compiler node/component capacity.", PWR_STATUS_CAPACITY_EXCEEDED);
    }
    if (desc->node_count == 0U || desc->nodes == NULL ||
        (desc->component_count != 0U && desc->components == NULL)) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_SCHEMA, 0, "nodes/components",
                               "At least one storage node and valid descriptor arrays are required.",
                               PWR_STATUS_INVALID_ARGUMENT);
    }
    if (desc->step_ns == 0U || desc->step_ns > UINT64_C(1000000000)) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_RANGE, 0, "step_ns",
                               "Fixed step must be between one nanosecond and one second.",
                               PWR_STATUS_INVALID_TIME_STEP);
    }
    (void)memset(g, 0, sizeof(*g));
    g->step_ns = desc->step_ns;
    g->dt = (double)desc->step_ns * 1.0e-9;
    g->node_count = desc->node_count;
    g->component_count = desc->component_count;
    for (uint32_t i = 0; i < g->node_count; ++i) {
        const pwr_status status = pwr_graph_node_compile(&desc->nodes[i], &g->nodes[i], d);
        if (status != PWR_STATUS_OK) { return status; }
        for (uint32_t j = i; j > 0U && g->nodes[j].id < g->nodes[j - 1U].id; --j) {
            const pwr_graph_node tmp = g->nodes[j];
            g->nodes[j] = g->nodes[j - 1U];
            g->nodes[j - 1U] = tmp;
        }
    }
    for (uint32_t i = 0; i < g->node_count; ++i) {
        pwr_graph_node *n = &g->nodes[i];
        if (i > 0U && n->id == g->nodes[i - 1U].id) {
            return pwr_graph_error(d, PWR_MODEL_DIAG_ID, n->id, "id",
                                   "Duplicate node ID.", PWR_STATUS_INVALID_MODEL);
        }
        if (n->domain == PWR_NODE_ROTATIONAL) {
            n->state_index = g->dynamic_count;
            g->dynamic_count += 2U;
        }
        else { n->state_index = g->thermal_count++; }
    }
    for (uint32_t i = 0; i < g->component_count; ++i) {
        const pwr_status status = pwr_graph_component_compile(g, &desc->components[i], &g->components[i], d);
        if (status != PWR_STATUS_OK) { return status; }
        for (uint32_t j = i; j > 0U && g->components[j].id < g->components[j - 1U].id; --j) {
            const pwr_graph_component tmp = g->components[j];
            g->components[j] = g->components[j - 1U];
            g->components[j - 1U] = tmp;
        }
    }
    for (uint32_t i = 0; i < g->component_count; ++i) {
        pwr_graph_component *c = &g->components[i];
        if (i > 0U && c->id == g->components[i - 1U].id) {
            return pwr_graph_error(d, PWR_MODEL_DIAG_ID, c->id, "id",
                                   "Duplicate component ID.", PWR_STATUS_INVALID_MODEL);
        }
        for (uint32_t j = 0; j < i; ++j) {
            if (c->input_channel != 0U && c->input_channel == g->components[j].input_channel) {
                return pwr_graph_error(d, PWR_MODEL_DIAG_CHANNEL, c->id, "input_channel",
                                       "Duplicate input channel.", PWR_STATUS_INVALID_MODEL);
            }
        }
        if (c->kind == PWR_COMPONENT_DC_MOTOR) { c->state_index = g->dynamic_count++; }
    }
    if (g->dynamic_count + g->thermal_count > PWR_MODEL_MAX_STATES) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_CAPACITY, 0, "states",
                               "Model exceeds compiler state capacity.", PWR_STATUS_CAPACITY_EXCEEDED);
    }
    pwr_graph_assemble(g);
    pwr_graph_state initial;
    if (!pwr_graph_factor(&g->dynamics) || !pwr_graph_factor(&g->thermal) ||
        pwr_graph_init(g, &initial) != PWR_STATUS_OK) {
        return pwr_graph_error(d, PWR_MODEL_DIAG_SOLVER, 0, "solver",
                               "Nonfinite initial state or singular/ill-conditioned discrete system.",
                               PWR_STATUS_NUMERIC_ERROR);
    }
    for (uint32_t i = 0; i < g->dynamic_count; ++i) {
        if (!isfinite(g->constant_force[i])) {
            return pwr_graph_error(d, PWR_MODEL_DIAG_SOLVER, 0, "constant_force",
                                   "Parameter scaling overflows the compiled forcing vector.", PWR_STATUS_NUMERIC_ERROR);
        }
    }
    for (uint32_t i = 0; i < g->thermal_count; ++i) {
        if (!isfinite(g->ambient_force[i])) {
            return pwr_graph_error(d, PWR_MODEL_DIAG_SOLVER, 0, "ambient_force",
                                   "Parameter scaling overflows the thermal boundary vector.", PWR_STATUS_NUMERIC_ERROR);
        }
    }
    g->fingerprint = pwr_graph_fingerprint(g);
    return PWR_STATUS_OK;
}

static double pwr_graph_relative(const pwr_graph *g, const pwr_graph_component *c,
                                  const double *x, uint32_t offset)
{
    const double a = x[g->nodes[c->a].state_index + offset];
    const double b = c->b == PWR_GRAPH_NO_INDEX ? 0.0 : x[g->nodes[c->b].state_index + offset];
    return a - c->p[3] * b - (offset == 0U ? c->p[2] : 0.0);
}

static double pwr_graph_energy(const pwr_graph *g, const pwr_graph_state *s)
{
    double energy = 0.0;
    for (uint32_t i = 0; i < g->node_count; ++i) {
        const pwr_graph_node *n = &g->nodes[i];
        if (n->domain == PWR_NODE_ROTATIONAL) {
            const double speed = s->x[n->state_index + 1U];
            energy += 0.5 * n->storage * speed * speed;
        }
        else { energy += n->storage * (s->temperature[n->state_index] - n->initial); }
    }
    for (uint32_t i = 0; i < g->component_count; ++i) {
        const pwr_graph_component *c = &g->components[i];
        if (c->kind == PWR_COMPONENT_SHAFT) {
            const double twist = pwr_graph_relative(g, c, s->x, 0);
            energy += 0.5 * c->p[0] * twist * twist;
        }
        else if (c->kind == PWR_COMPONENT_DC_MOTOR) {
            const double current = s->x[c->state_index];
            energy += 0.5 * c->p[1] * current * current;
        }
    }
    return energy;
}

static bool pwr_graph_observables_finite(const pwr_graph *g, const pwr_graph_state *s)
{
    for (uint32_t i = 0; i < g->component_count; ++i) {
        const pwr_graph_component *c = &g->components[i];
        if (c->kind == PWR_COMPONENT_SHAFT) {
            const double twist = pwr_graph_relative(g, c, s->x, 0);
            const double torque = -c->p[0] * twist - c->p[1] * pwr_graph_relative(g, c, s->x, 1);
            if (!isfinite(twist) || !isfinite(torque)) { return false; }
        }
        else if (c->kind == PWR_COMPONENT_DC_MOTOR && !isfinite(c->p[2] * s->x[c->state_index])) {
            return false;
        }
    }
    return true;
}

pwr_status pwr_graph_init(const pwr_graph *g, pwr_graph_state *s)
{
    if (g == NULL || s == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    *s = (pwr_graph_state){0};
    for (uint32_t i = 0; i < g->node_count; ++i) {
        const pwr_graph_node *n = &g->nodes[i];
        if (n->domain == PWR_NODE_ROTATIONAL) {
            s->x[n->state_index] = n->position;
            s->x[n->state_index + 1U] = n->initial;
        }
        else { s->temperature[n->state_index] = n->initial; }
    }
    for (uint32_t i = 0; i < g->component_count; ++i) {
        const pwr_graph_component *c = &g->components[i];
        s->inputs[i] = c->initial_input;
        if (c->kind == PWR_COMPONENT_DC_MOTOR) { s->x[c->state_index] = c->p[3]; }
    }
    s->initial_energy = pwr_graph_energy(g, s);
    return isfinite(s->initial_energy) && pwr_graph_observables_finite(g, s) ?
           PWR_STATUS_OK : PWR_STATUS_NUMERIC_ERROR;
}

pwr_status pwr_graph_submit(const pwr_graph *g, pwr_graph_state *s, const pwr_input_frame *frame)
{
    if (g == NULL || s == NULL || frame == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    if (frame->abi_version != PWR_ABI_VERSION) { return PWR_STATUS_UNSUPPORTED_ABI; }
    if (frame->struct_size < sizeof(*frame)) { return PWR_STATUS_STRUCT_TOO_SMALL; }
    if (frame->flags != 0U || (frame->value_count != 0U && frame->values == NULL)) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    /* At most one update per source per frame, keeping validation bounded. */
    if (frame->value_count > g->component_count) { return PWR_STATUS_CAPACITY_EXCEEDED; }
    double candidate[PWR_MODEL_MAX_COMPONENTS];
    bool seen[PWR_MODEL_MAX_COMPONENTS] = {false};
    (void)memcpy(candidate, s->inputs, sizeof(candidate));
    for (uint32_t i = 0; i < frame->value_count; ++i) {
        const pwr_scalar_value *v = &frame->values[i];
        if (!isfinite(v->value)) { return PWR_STATUS_INVALID_ARGUMENT; }
        if (v->flags != 0U || v->reserved != 0U) { return PWR_STATUS_UNSUPPORTED; }
        uint32_t index = PWR_GRAPH_NO_INDEX;
        for (uint32_t j = 0; j < g->component_count; ++j) {
            if (v->channel != 0U && g->components[j].input_channel == v->channel) { index = j; break; }
        }
        if (index == PWR_GRAPH_NO_INDEX) { return PWR_STATUS_UNKNOWN_CHANNEL; }
        if (seen[index]) { return PWR_STATUS_INVALID_ARGUMENT; }
        seen[index] = true;
        candidate[index] = v->value == 0.0 ? 0.0 : v->value;
    }
    (void)memcpy(s->inputs, candidate, sizeof(candidate));
    return PWR_STATUS_OK;
}

static void pwr_graph_accumulate(double value, double *sum, double *correction)
{
    const double adjusted = value - *correction;
    const double next = *sum + adjusted;
    *correction = (next - *sum) - adjusted;
    *sum = next;
}

static pwr_status pwr_graph_tick(const pwr_graph *g, pwr_graph_state *s)
{
    double mid[PWR_MODEL_MAX_STATES] = {0};
    double temperature[PWR_MODEL_MAX_STATES] = {0};
    double work = 0.0;
    double heat_out = 0.0;
    for (uint32_t i = 0; i < g->dynamic_count; ++i) {
        mid[i] = s->x[i] + 0.5 * g->dt * g->constant_force[i];
    }
    for (uint32_t i = 0; i < g->component_count; ++i) {
        const pwr_graph_component *c = &g->components[i];
        if (c->kind == PWR_COMPONENT_TORQUE_SOURCE) {
            const pwr_graph_node *n = &g->nodes[c->a];
            mid[n->state_index + 1U] += 0.5 * g->dt * s->inputs[i] / n->storage;
        }
        else if (c->kind == PWR_COMPONENT_DC_MOTOR) {
            mid[c->state_index] += 0.5 * g->dt * s->inputs[i] / c->p[1];
        }
    }
    if (!pwr_graph_solve(&g->dynamics, mid)) { return PWR_STATUS_NUMERIC_ERROR; }
    for (uint32_t i = 0; i < g->node_count; ++i) {
        const pwr_graph_node *n = &g->nodes[i];
        if (n->domain == PWR_NODE_THERMAL) {
            temperature[n->state_index] = n->storage * s->temperature[n->state_index] +
                                           g->ambient_force[n->state_index];
        }
    }
    for (uint32_t i = 0; i < g->component_count; ++i) {
        const pwr_graph_component *c = &g->components[i];
        double loss = 0.0;
        if (c->kind == PWR_COMPONENT_SHAFT) {
            const double slip = pwr_graph_relative(g, c, mid, 1);
            loss = g->dt * c->p[1] * slip * slip;
        }
        else if (c->kind == PWR_COMPONENT_DC_MOTOR) {
            const double current = mid[c->state_index];
            loss = g->dt * c->p[0] * current * current;
            work += g->dt * s->inputs[i] * current;
        }
        else if (c->kind == PWR_COMPONENT_TORQUE_SOURCE) {
            work += g->dt * s->inputs[i] * mid[g->nodes[c->a].state_index + 1U];
        }
        if (c->heat == PWR_GRAPH_NO_INDEX) { heat_out += loss; }
        else { temperature[g->nodes[c->heat].state_index] += loss; }
    }
    if (!pwr_graph_solve(&g->thermal, temperature)) { return PWR_STATUS_NUMERIC_ERROR; }
    for (uint32_t i = 0; i < g->component_count; ++i) {
        const pwr_graph_component *c = &g->components[i];
        if (c->kind == PWR_COMPONENT_THERMAL_LINK && c->b == PWR_GRAPH_NO_INDEX) {
            heat_out += g->dt * c->p[0] * (temperature[g->nodes[c->a].state_index] - c->p[1]);
        }
    }
    for (uint32_t i = 0; i < g->dynamic_count; ++i) {
        s->x[i] = 2.0 * mid[i] - s->x[i];
        if (!isfinite(s->x[i])) { return PWR_STATUS_NUMERIC_ERROR; }
    }
    for (uint32_t i = 0; i < g->thermal_count; ++i) {
        if (!(temperature[i] > 0.0)) { return PWR_STATUS_NUMERIC_ERROR; }
        s->temperature[i] = temperature[i];
    }
    pwr_graph_accumulate(work, &s->source_work, &s->source_work_compensation);
    pwr_graph_accumulate(heat_out, &s->heat_rejected, &s->heat_compensation);
    const double residual = s->source_work - s->heat_rejected -
                            (pwr_graph_energy(g, s) - s->initial_energy);
    if (!isfinite(residual) || !isfinite(s->source_work_compensation) ||
        !isfinite(s->heat_compensation) || !pwr_graph_observables_finite(g, s)) {
        return PWR_STATUS_NUMERIC_ERROR;
    }
    s->time_ns += g->step_ns;
    return PWR_STATUS_OK;
}

pwr_status pwr_graph_step(const pwr_graph *g, pwr_graph_state *s, uint64_t delta_ns)
{
    if (g == NULL || s == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    if (delta_ns == 0U || delta_ns % g->step_ns != 0U ||
        delta_ns / g->step_ns > UINT64_C(1000000) || UINT64_MAX - s->time_ns < delta_ns) {
        return PWR_STATUS_INVALID_TIME_STEP;
    }
    pwr_graph_state candidate = *s;
    const uint64_t count = delta_ns / g->step_ns;
    for (uint64_t i = 0; i < count; ++i) {
        const pwr_status status = pwr_graph_tick(g, &candidate);
        if (status != PWR_STATUS_OK) { return status; }
    }
    *s = candidate;
    return PWR_STATUS_OK;
}

uint64_t pwr_graph_state_hash(const pwr_graph *g, const pwr_graph_state *s)
{
    uint64_t hash = pwr_graph_hash_word(g->fingerprint, s->time_ns);
    for (uint32_t i = 0; i < g->dynamic_count; ++i) { hash = pwr_graph_hash_double(hash, s->x[i]); }
    for (uint32_t i = 0; i < g->thermal_count; ++i) { hash = pwr_graph_hash_double(hash, s->temperature[i]); }
    for (uint32_t i = 0; i < g->component_count; ++i) { hash = pwr_graph_hash_double(hash, s->inputs[i]); }
    hash = pwr_graph_hash_double(hash, s->initial_energy);
    hash = pwr_graph_hash_double(hash, s->source_work);
    hash = pwr_graph_hash_double(hash, s->source_work_compensation);
    hash = pwr_graph_hash_double(hash, s->heat_rejected);
    return pwr_graph_hash_double(hash, s->heat_compensation);
}

static void pwr_graph_write(pwr_snapshot *snapshot, uint32_t *index,
                             uint32_t object, uint32_t field, double value)
{
    snapshot->values[(*index)++] = (pwr_scalar_value){PWR_MODEL_CHANNEL(object, field), value, 0, 0};
}

pwr_status pwr_graph_snapshot(const pwr_graph *g, const pwr_graph_state *s, pwr_snapshot *snapshot)
{
    if (g == NULL || s == NULL || snapshot == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    if (snapshot->abi_version != PWR_ABI_VERSION) { return PWR_STATUS_UNSUPPORTED_ABI; }
    if (snapshot->struct_size < sizeof(*snapshot)) { return PWR_STATUS_STRUCT_TOO_SMALL; }
    snapshot->simulation_time_ns = s->time_ns;
    snapshot->state_hash = pwr_graph_state_hash(g, s);
    snapshot->diagnostic_bits = 0;
    snapshot->value_count = g->output_count;
    if (snapshot->values == NULL || snapshot->value_capacity < g->output_count) {
        return PWR_STATUS_CAPACITY_EXCEEDED;
    }
    uint32_t index = 0;
    for (uint32_t i = 0; i < g->node_count; ++i) {
        const pwr_graph_node *n = &g->nodes[i];
        if (n->domain == PWR_NODE_ROTATIONAL) {
            pwr_graph_write(snapshot, &index, n->id, PWR_FIELD_ANGLE, s->x[n->state_index]);
            pwr_graph_write(snapshot, &index, n->id, PWR_FIELD_SPEED, s->x[n->state_index + 1U]);
        }
        else { pwr_graph_write(snapshot, &index, n->id, PWR_FIELD_TEMPERATURE, s->temperature[n->state_index]); }
    }
    for (uint32_t i = 0; i < g->component_count; ++i) {
        const pwr_graph_component *c = &g->components[i];
        if (c->kind == PWR_COMPONENT_SHAFT) {
            const double twist = pwr_graph_relative(g, c, s->x, 0);
            const double torque = -c->p[0] * twist - c->p[1] * pwr_graph_relative(g, c, s->x, 1);
            pwr_graph_write(snapshot, &index, c->id, PWR_FIELD_TWIST, twist);
            pwr_graph_write(snapshot, &index, c->id, PWR_FIELD_TORQUE, torque);
        }
        else if (c->kind == PWR_COMPONENT_DC_MOTOR) {
            const double current = s->x[c->state_index];
            pwr_graph_write(snapshot, &index, c->id, PWR_FIELD_CURRENT, current);
            pwr_graph_write(snapshot, &index, c->id, PWR_FIELD_TORQUE, c->p[2] * current);
        }
    }
    const double stored = pwr_graph_energy(g, s) - s->initial_energy;
    pwr_graph_write(snapshot, &index, 0, PWR_FIELD_SOURCE_WORK, s->source_work);
    pwr_graph_write(snapshot, &index, 0, PWR_FIELD_HEAT_REJECTED, s->heat_rejected);
    pwr_graph_write(snapshot, &index, 0, PWR_FIELD_STORED_ENERGY_CHANGE, stored);
    pwr_graph_write(snapshot, &index, 0, PWR_FIELD_ENERGY_RESIDUAL, s->source_work - s->heat_rejected - stored);
    return PWR_STATUS_OK;
}
