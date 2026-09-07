#include "pwr_graph.h"

#include <float.h>
#include <math.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

static int failures;

#define CHECK(expr) do { if (!(expr)) {                                         \
    (void)fprintf(stderr, "%s:%d: %s\n", __FILE__, __LINE__, #expr);            \
    ++failures; } } while (0)

static pwr_node_desc rotor(uint32_t id, double inertia, double angle, double speed)
{
    const pwr_node_desc n = {id, PWR_NODE_ROTATIONAL,
        PWR_QUANTITY(inertia, PWR_UNIT_KG_M2),
        PWR_QUANTITY(speed, PWR_UNIT_RAD_S), PWR_QUANTITY(angle, PWR_UNIT_RAD)};
    return n;
}

static pwr_node_desc thermal(uint32_t id, double capacity, double temperature)
{
    const pwr_node_desc n = {id, PWR_NODE_THERMAL,
        PWR_QUANTITY(capacity, PWR_UNIT_J_K),
        PWR_QUANTITY(temperature, PWR_UNIT_K), PWR_QUANTITY(0, PWR_UNIT_NONE)};
    return n;
}

static pwr_component_desc shaft(uint32_t id, uint32_t a, uint32_t b,
                                 double stiffness, double damping, double ratio)
{
    pwr_component_desc c = {0};
    c.id = id; c.kind = PWR_COMPONENT_SHAFT; c.node_a = a; c.node_b = b;
    c.parameters.shaft.stiffness = (pwr_quantity)PWR_QUANTITY(stiffness, PWR_UNIT_NM_RAD);
    c.parameters.shaft.damping = (pwr_quantity)PWR_QUANTITY(damping, PWR_UNIT_NM_S_RAD);
    c.parameters.shaft.rest_angle = (pwr_quantity)PWR_QUANTITY(0, PWR_UNIT_RAD);
    c.parameters.shaft.ratio = ratio;
    return c;
}

static pwr_component_desc source(uint32_t id, uint32_t a, double torque)
{
    pwr_component_desc c = {0};
    c.id = id; c.kind = PWR_COMPONENT_TORQUE_SOURCE; c.node_a = a;
    c.input_channel = id;
    c.initial_input = (pwr_quantity)PWR_QUANTITY(torque, PWR_UNIT_NM);
    return c;
}

static pwr_component_desc motor(uint32_t id, uint32_t a, uint32_t heat,
                                 double resistance, double inductance, double k, double voltage)
{
    pwr_component_desc c = {0};
    c.id = id; c.kind = PWR_COMPONENT_DC_MOTOR; c.node_a = a; c.heat_node = heat;
    c.input_channel = id;
    c.initial_input = (pwr_quantity)PWR_QUANTITY(voltage, PWR_UNIT_V);
    c.parameters.motor.resistance = (pwr_quantity)PWR_QUANTITY(resistance, PWR_UNIT_OHM);
    c.parameters.motor.inductance = (pwr_quantity)PWR_QUANTITY(inductance, PWR_UNIT_H);
    c.parameters.motor.coupling = (pwr_quantity)PWR_QUANTITY(k, PWR_UNIT_NM_A);
    c.parameters.motor.initial_current = (pwr_quantity)PWR_QUANTITY(0, PWR_UNIT_A);
    return c;
}

static pwr_component_desc link(uint32_t id, uint32_t a, uint32_t b, double g, double ambient)
{
    pwr_component_desc c = {0};
    c.id = id; c.kind = PWR_COMPONENT_THERMAL_LINK; c.node_a = a; c.node_b = b;
    c.parameters.thermal.conductance = (pwr_quantity)PWR_QUANTITY(g, PWR_UNIT_W_K);
    c.parameters.thermal.ambient_temperature =
        (pwr_quantity)PWR_QUANTITY(ambient, b == 0U ? PWR_UNIT_K : PWR_UNIT_NONE);
    return c;
}

static pwr_status compile(pwr_graph *g, const pwr_node_desc *nodes, uint32_t nn,
                           const pwr_component_desc *components, uint32_t nc,
                           uint64_t step_ns, pwr_model_diagnostic *diagnostic)
{
    pwr_model_desc desc = PWR_MODEL_DESC_INIT;
    desc.nodes = nodes; desc.node_count = nn;
    desc.components = components; desc.component_count = nc;
    desc.step_ns = step_ns;
    return pwr_graph_compile(&desc, g, diagnostic);
}

static double value(const pwr_graph *g, const pwr_graph_state *s, uint32_t object, uint32_t field)
{
    pwr_scalar_value values[PWR_GRAPH_MAX_CHANNELS];
    pwr_snapshot snapshot = PWR_SNAPSHOT_INIT;
    snapshot.values = values; snapshot.value_capacity = PWR_GRAPH_MAX_CHANNELS;
    CHECK(pwr_graph_snapshot(g, s, &snapshot) == PWR_STATUS_OK);
    for (uint32_t i = 0; i < snapshot.value_count; ++i) {
        if (values[i].channel == PWR_MODEL_CHANNEL(object, field)) { return values[i].value; }
    }
    CHECK(false);
    return NAN;
}

static void test_constant_torque_and_replay(pwr_graph *g)
{
    const pwr_node_desc n = rotor(1, 2, 0, 0);
    const pwr_component_desc c = source(10, 1, 4);
    CHECK(compile(g, &n, 1, &c, 1, UINT64_C(1000000), NULL) == PWR_STATUS_OK);
    pwr_graph_state a, b;
    CHECK(pwr_graph_init(g, &a) == PWR_STATUS_OK);
    b = a;
    CHECK(pwr_graph_step(g, &a, UINT64_C(2000000000)) == PWR_STATUS_OK);
    for (uint32_t i = 0; i < 2000U; ++i) {
        CHECK(pwr_graph_step(g, &b, UINT64_C(1000000)) == PWR_STATUS_OK);
    }
    CHECK(pwr_graph_state_hash(g, &a) == pwr_graph_state_hash(g, &b));
    CHECK(fabs(value(g, &a, 1, PWR_FIELD_SPEED) - 4.0) < 1.0e-11);
    CHECK(fabs(value(g, &a, 1, PWR_FIELD_ANGLE) - 4.0) < 1.0e-11);
    CHECK(fabs(value(g, &a, 0, PWR_FIELD_SOURCE_WORK) - 16.0) < 1.0e-10);
    CHECK(fabs(value(g, &a, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-10);
}

static void test_oscillator_convergence_and_conservation(pwr_graph *g)
{
    const pwr_node_desc n = rotor(1, 2, 0.5, 0);
    const pwr_component_desc c = shaft(10, 1, 0, 8, 0, 1);
    double errors[3];
    for (uint32_t i = 0; i < 3U; ++i) {
        const uint64_t step_ns = UINT64_C(40000000) >> i;
        CHECK(compile(g, &n, 1, &c, 1, step_ns, NULL) == PWR_STATUS_OK);
        pwr_graph_state s;
        CHECK(pwr_graph_init(g, &s) == PWR_STATUS_OK);
        CHECK(pwr_graph_step(g, &s, UINT64_C(1000000000)) == PWR_STATUS_OK);
        errors[i] = fabs(value(g, &s, 1, PWR_FIELD_ANGLE) - 0.5 * cos(2.0));
        CHECK(fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-12);
    }
    CHECK(errors[0] / errors[1] > 3.9 && errors[0] / errors[1] < 4.1);
    CHECK(errors[1] / errors[2] > 3.9 && errors[1] / errors[2] < 4.1);
    pwr_graph_state s;
    CHECK(pwr_graph_init(g, &s) == PWR_STATUS_OK);
    CHECK(pwr_graph_step(g, &s, UINT64_C(1000000000000)) == PWR_STATUS_OK);
    CHECK(fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-9);
}

static void test_signed_transmission_and_heat(pwr_graph *g)
{
    const pwr_node_desc nodes[] = {rotor(1, 1, 0.2, 2), rotor(2, 3, -0.1, -1), thermal(3, 10, 300)};
    for (uint32_t trial = 0; trial < 2U; ++trial) {
        const double ratio = trial == 0U ? 2.0 : -2.0;
        pwr_component_desc c = shaft(10, 1, 2, 40, 0.8, ratio);
        c.heat_node = 3;
        c.parameters.shaft.rest_angle.value = 0.07;
        CHECK(compile(g, nodes, 3, &c, 1, UINT64_C(1000000), NULL) == PWR_STATUS_OK);
        pwr_graph_state s;
        CHECK(pwr_graph_init(g, &s) == PWR_STATUS_OK);
        CHECK(pwr_graph_step(g, &s, UINT64_C(10000000000)) == PWR_STATUS_OK);
        const double momentum = ratio * value(g, &s, 1, PWR_FIELD_SPEED) + 3 * value(g, &s, 2, PWR_FIELD_SPEED);
        CHECK(fabs(momentum - (ratio * 2.0 - 3.0)) < 1.0e-10);
        CHECK(value(g, &s, 3, PWR_FIELD_TEMPERATURE) > 300.0);
        CHECK(value(g, &s, 0, PWR_FIELD_HEAT_REJECTED) == 0.0);
        CHECK(fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-7);
    }
}

static void test_electrical_analytic(pwr_graph *g)
{
    const pwr_node_desc n = rotor(1, 1, 0, 0);
    const pwr_component_desc c = motor(10, 1, 0, 2, 0.5, 0, 12);
    CHECK(compile(g, &n, 1, &c, 1, UINT64_C(1000000), NULL) == PWR_STATUS_OK);
    pwr_graph_state s;
    CHECK(pwr_graph_init(g, &s) == PWR_STATUS_OK);
    CHECK(pwr_graph_step(g, &s, UINT64_C(500000000)) == PWR_STATUS_OK);
    CHECK(fabs(value(g, &s, 10, PWR_FIELD_CURRENT) - 6.0 * (1.0 - exp(-2.0))) < 3.0e-6);
    CHECK(value(g, &s, 1, PWR_FIELD_SPEED) == 0.0);
    CHECK(fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-10);
}

static void test_thermal_network(pwr_graph *g)
{
    const pwr_node_desc nodes[] = {thermal(1, 10, 400), thermal(2, 30, 300)};
    const pwr_component_desc c = link(10, 1, 2, 100, 0);
    CHECK(compile(g, nodes, 2, &c, 1, UINT64_C(1000000000), NULL) == PWR_STATUS_OK);
    pwr_graph_state s;
    CHECK(pwr_graph_init(g, &s) == PWR_STATUS_OK);
    CHECK(pwr_graph_step(g, &s, UINT64_C(20000000000)) == PWR_STATUS_OK);
    CHECK(fabs(value(g, &s, 1, PWR_FIELD_TEMPERATURE) - 325) < 1.0e-9);
    CHECK(fabs(value(g, &s, 2, PWR_FIELD_TEMPERATURE) - 325) < 1.0e-9);
    CHECK(fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-8);

    const pwr_component_desc ambient = link(10, 1, 0, 10, 300);
    double errors[2];
    for (uint32_t i = 0; i < 2U; ++i) {
        CHECK(compile(g, nodes, 1, &ambient, 1, UINT64_C(10000000) >> i, NULL) == PWR_STATUS_OK);
        CHECK(pwr_graph_init(g, &s) == PWR_STATUS_OK);
        CHECK(pwr_graph_step(g, &s, UINT64_C(1000000000)) == PWR_STATUS_OK);
        errors[i] = fabs(value(g, &s, 1, PWR_FIELD_TEMPERATURE) - (300 + 100 * exp(-1.0)));
        CHECK(fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-8);
    }
    CHECK(errors[0] / errors[1] > 1.98 && errors[0] / errors[1] < 2.02);
}

static void test_coupled_motor_and_regeneration(pwr_graph *g)
{
    const pwr_node_desc nodes[] = {rotor(1, 0.2, 0, 0), rotor(2, 1, 0, 0),
                                  thermal(3, 100, 300), thermal(4, 200, 300)};
    pwr_component_desc cs[] = {motor(10, 1, 3, 0.5, 0.02, 0.8, 24),
        shaft(11, 1, 2, 100, 1, 3), shaft(12, 2, 0, 0, 0.1, 1),
        link(13, 3, 4, 5, 0), link(14, 4, 0, 2, 300)};
    cs[1].heat_node = 4;
    CHECK(compile(g, nodes, 4, cs, 5, UINT64_C(1000000), NULL) == PWR_STATUS_OK);
    pwr_graph_state s;
    CHECK(pwr_graph_init(g, &s) == PWR_STATUS_OK);
    CHECK(pwr_graph_step(g, &s, UINT64_C(20000000000)) == PWR_STATUS_OK);
    CHECK(value(g, &s, 1, PWR_FIELD_SPEED) > 20.0);
    CHECK(value(g, &s, 2, PWR_FIELD_SPEED) > 7.0);
    CHECK(value(g, &s, 3, PWR_FIELD_TEMPERATURE) > 300.0);
    CHECK(value(g, &s, 4, PWR_FIELD_TEMPERATURE) > 300.0);
    CHECK(fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-6);
    const double work_before = s.source_work;
    pwr_scalar_value voltage = {10, 4, 0, 0};
    pwr_input_frame frame = PWR_INPUT_FRAME_INIT;
    frame.values = &voltage; frame.value_count = 1;
    CHECK(pwr_graph_submit(g, &s, &frame) == PWR_STATUS_OK);
    CHECK(pwr_graph_step(g, &s, UINT64_C(500000000)) == PWR_STATUS_OK);
    CHECK(value(g, &s, 10, PWR_FIELD_CURRENT) < 0.0);
    CHECK(s.source_work < work_before);
    CHECK(fabs(value(g, &s, 0, PWR_FIELD_ENERGY_RESIDUAL)) < 1.0e-6);
}

static void test_canonicalization(pwr_graph *g)
{
    pwr_node_desc nodes[] = {rotor(5, 2, 0, 0), rotor(1, 1, 0, 0), thermal(3, 10, 300)};
    pwr_component_desc cs[] = {shaft(20, 5, 1, 40, 1, -2), source(10, 5, 3)};
    cs[0].heat_node = 3;
    CHECK(compile(g, nodes, 3, cs, 2, UINT64_C(1000000), NULL) == PWR_STATUS_OK);
    const uint64_t fingerprint = g->fingerprint;
    pwr_graph_state a, b;
    CHECK(pwr_graph_init(g, &a) == PWR_STATUS_OK);
    CHECK(pwr_graph_step(g, &a, UINT64_C(1000000000)) == PWR_STATUS_OK);
    const uint64_t hash = pwr_graph_state_hash(g, &a);
    const pwr_node_desc tmpn = nodes[0]; nodes[0] = nodes[2]; nodes[2] = tmpn;
    const pwr_component_desc tmpc = cs[0]; cs[0] = cs[1]; cs[1] = tmpc;
    nodes[1].position = (pwr_quantity)PWR_QUANTITY(-0.0, PWR_UNIT_DEG);
    nodes[1].initial.unit = PWR_UNIT_RPM;
    CHECK(compile(g, nodes, 3, cs, 2, UINT64_C(1000000), NULL) == PWR_STATUS_OK);
    CHECK(g->fingerprint == fingerprint);
    CHECK(pwr_graph_init(g, &b) == PWR_STATUS_OK);
    CHECK(pwr_graph_step(g, &b, UINT64_C(1000000000)) == PWR_STATUS_OK);
    CHECK(pwr_graph_state_hash(g, &b) == hash);
    cs[0].initial_input.value = 4;
    CHECK(compile(g, nodes, 3, cs, 2, UINT64_C(1000000), NULL) == PWR_STATUS_OK);
    CHECK(g->fingerprint != fingerprint);

    pwr_node_desc rpm = rotor(1, 1, 0, 60);
    rpm.initial.unit = PWR_UNIT_RPM;
    CHECK(compile(g, &rpm, 1, NULL, 0, UINT64_C(1000000), NULL) == PWR_STATUS_OK);
    CHECK(pwr_graph_init(g, &b) == PWR_STATUS_OK);
    CHECK(fabs(value(g, &b, 1, PWR_FIELD_SPEED) - 2.0 * acos(-1.0)) < 1.0e-14);
}

static void test_rejection_and_atomicity(pwr_graph *g)
{
    pwr_node_desc nodes[] = {rotor(1, 2, 0, 0), thermal(2, 10, 300)};
    pwr_component_desc cs[] = {source(10, 1, 4), shaft(11, 1, 0, 1, 1, 1)};
    pwr_model_diagnostic d = PWR_MODEL_DIAGNOSTIC_INIT;
    nodes[0].storage.unit = PWR_UNIT_NM;
    CHECK(compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL);
    CHECK(d.code == PWR_MODEL_DIAG_UNIT && d.object_id == 1U && strcmp(d.field, "storage") == 0);
    nodes[0].storage.unit = PWR_UNIT_KG_M2;
    nodes[0].storage.value = NAN;
    CHECK(compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL);
    nodes[0].storage.value = 0;
    CHECK(compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL);
    nodes[0].storage.value = 2;
    cs[1].node_b = 2;
    CHECK(compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL);
    CHECK(d.code == PWR_MODEL_DIAG_CONNECTION && d.object_id == 11U);
    cs[1].node_b = 999;
    CHECK(compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL);
    cs[1].node_b = 0;
    cs[1].id = 10;
    CHECK(compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL);
    CHECK(d.code == PWR_MODEL_DIAG_ID);
    cs[1] = source(11, 1, 0); cs[1].input_channel = 10;
    CHECK(compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_INVALID_MODEL);
    CHECK(d.code == PWR_MODEL_DIAG_CHANNEL);
    cs[1].input_channel = 11;
    CHECK(compile(g, nodes, 2, cs, 2, 1000000, &d) == PWR_STATUS_OK);
    CHECK(d.code == PWR_MODEL_DIAG_NONE && d.field[0] == '\0');
    pwr_graph_state s;
    CHECK(pwr_graph_init(g, &s) == PWR_STATUS_OK);
    const uint64_t initial_hash = pwr_graph_state_hash(g, &s);
    pwr_scalar_value values[] = {{10, 100, 0, 0}, {99, 0, 0, 0}};
    pwr_input_frame frame = PWR_INPUT_FRAME_INIT;
    frame.values = values; frame.value_count = 2;
    CHECK(pwr_graph_submit(g, &s, &frame) == PWR_STATUS_UNKNOWN_CHANNEL);
    CHECK(pwr_graph_state_hash(g, &s) == initial_hash);
    values[1].channel = 10;
    CHECK(pwr_graph_submit(g, &s, &frame) == PWR_STATUS_INVALID_ARGUMENT);
    CHECK(pwr_graph_state_hash(g, &s) == initial_hash);
    values[1].channel = 11; values[1].value = INFINITY;
    CHECK(pwr_graph_submit(g, &s, &frame) == PWR_STATUS_INVALID_ARGUMENT);
    CHECK(pwr_graph_state_hash(g, &s) == initial_hash);
    frame.value_count = 1;
    values[0].value = DBL_MAX;
    CHECK(pwr_graph_submit(g, &s, &frame) == PWR_STATUS_OK);
    const uint64_t submitted_hash = pwr_graph_state_hash(g, &s);
    CHECK(submitted_hash != initial_hash);
    CHECK(pwr_graph_step(g, &s, UINT64_C(2000000)) == PWR_STATUS_NUMERIC_ERROR);
    CHECK(pwr_graph_state_hash(g, &s) == submitted_hash);
    CHECK(s.time_ns == 0U);
    CHECK(pwr_graph_step(g, &s, 0) == PWR_STATUS_INVALID_TIME_STEP);
    CHECK(pwr_graph_step(g, &s, 3) == PWR_STATUS_INVALID_TIME_STEP);
    CHECK(pwr_graph_step(g, &s, UINT64_C(1000001000000)) == PWR_STATUS_INVALID_TIME_STEP);
    s.time_ns = UINT64_MAX - 10U;
    CHECK(pwr_graph_step(g, &s, 1000000) == PWR_STATUS_INVALID_TIME_STEP);

    pwr_scalar_value canary = {123, 456, 789, 101};
    pwr_snapshot snapshot = PWR_SNAPSHOT_INIT;
    snapshot.values = &canary; snapshot.value_capacity = 1;
    CHECK(pwr_graph_snapshot(g, &s, &snapshot) == PWR_STATUS_CAPACITY_EXCEEDED);
    CHECK(snapshot.value_count == g->output_count && canary.channel == 123U && canary.value == 456);
}

static void test_capacity_and_schema(pwr_graph *g)
{
    pwr_node_desc nodes[PWR_MODEL_MAX_NODES];
    for (uint32_t i = 0; i < PWR_MODEL_MAX_NODES; ++i) { nodes[i] = rotor(i + 1U, 1, 0, 0); }
    const pwr_component_desc c = motor(100, 1, 0, 1, 1, 1, 1);
    pwr_model_diagnostic diagnostic = PWR_MODEL_DIAGNOSTIC_INIT;
    CHECK(compile(g, nodes, PWR_MODEL_MAX_NODES, &c, 1, 1000000, &diagnostic) == PWR_STATUS_CAPACITY_EXCEEDED);
    CHECK(diagnostic.code == PWR_MODEL_DIAG_CAPACITY);
    CHECK(compile(g, nodes, PWR_MODEL_MAX_NODES + 1U, &c, 1, 1000000, NULL) == PWR_STATUS_CAPACITY_EXCEEDED);
    CHECK(compile(g, nodes, PWR_MODEL_MAX_NODES, NULL, 0, 1000000, NULL) == PWR_STATUS_OK);
    pwr_graph_state s;
    CHECK(pwr_graph_init(g, &s) == PWR_STATUS_OK);
    CHECK(pwr_graph_step(g, &s, 1000000) == PWR_STATUS_OK);
    pwr_model_desc desc = PWR_MODEL_DESC_INIT;
    desc.nodes = nodes; desc.node_count = 1; desc.schema_version = UINT32_MAX;
    CHECK(pwr_graph_compile(&desc, g, &diagnostic) == PWR_STATUS_UNSUPPORTED);
    desc.schema_version = PWR_MODEL_SCHEMA_VERSION; desc.struct_size = 8;
    CHECK(pwr_graph_compile(&desc, g, &diagnostic) == PWR_STATUS_STRUCT_TOO_SMALL);
    desc.struct_size = (uint32_t)sizeof(desc); desc.abi_version = 99;
    CHECK(pwr_graph_compile(&desc, g, &diagnostic) == PWR_STATUS_UNSUPPORTED_ABI);
}

int main(void)
{
    pwr_graph *g = malloc(sizeof(*g));
    if (g == NULL) { return 1; }
    test_constant_torque_and_replay(g);
    test_oscillator_convergence_and_conservation(g);
    test_signed_transmission_and_heat(g);
    test_electrical_analytic(g);
    test_thermal_network(g);
    test_coupled_motor_and_regeneration(g);
    test_canonicalization(g);
    test_rejection_and_atomicity(g);
    test_capacity_and_schema(g);
    free(g);
    if (failures != 0) { (void)fprintf(stderr, "%d graph checks failed\n", failures); return 1; }
    (void)puts("Power! compiled graph physics tests passed");
    return 0;
}
