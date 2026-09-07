#include <power/power.h>

#include <math.h>
#include <stddef.h>
#include <stdio.h>
#include <string.h>

static int failures;
#define CHECK(expr) do { if (!(expr)) {                                         \
    (void)fprintf(stderr, "%s:%d: %s\n", __FILE__, __LINE__, #expr);            \
    ++failures; } } while (0)

static pwr_status compile_rotor(const pwr_api *api, pwr_context context, pwr_model *model)
{
    /* Everything in this descriptor goes out of scope before instance use. */
    const pwr_node_desc node = {1, PWR_NODE_ROTATIONAL,
        PWR_QUANTITY(2, PWR_UNIT_KG_M2), PWR_QUANTITY(0, PWR_UNIT_RAD_S), PWR_QUANTITY(0, PWR_UNIT_RAD)};
    const pwr_component_desc component = {
        .id = 10, .kind = PWR_COMPONENT_TORQUE_SOURCE, .node_a = 1,
        .input_channel = 20, .initial_input = PWR_QUANTITY(4, PWR_UNIT_NM)
    };
    pwr_model_desc desc = PWR_MODEL_DESC_INIT;
    desc.nodes = &node; desc.node_count = 1;
    desc.components = &component; desc.component_count = 1;
    desc.step_ns = UINT64_C(1000000);
    return api->model_compile(context, &desc, model, NULL);
}

static uint64_t snapshot_hash(const pwr_api *api, pwr_instance instance, double *speed)
{
    pwr_scalar_value values[6];
    pwr_snapshot snapshot = PWR_SNAPSHOT_INIT;
    snapshot.values = values; snapshot.value_capacity = 6;
    CHECK(api->instance_read_snapshot(instance, &snapshot) == PWR_STATUS_OK);
    CHECK(snapshot.value_count == 6U);
    CHECK(values[1].channel == PWR_MODEL_CHANNEL(1, PWR_FIELD_SPEED));
    *speed = values[1].value;
    return snapshot.state_hash;
}

static void test_public_workflow(const pwr_api *api)
{
    const pwr_context_desc desc = PWR_CONTEXT_DESC_INIT;
    pwr_context context;
    CHECK(api->context_create(&desc, &context) == PWR_STATUS_OK);
    pwr_model model;
    CHECK(compile_rotor(api, context, &model) == PWR_STATUS_OK);
    CHECK(model != PWR_MODEL_INVALID && model != context);
    CHECK(api->context_destroy(context) == PWR_STATUS_IN_USE);
    CHECK(api->context_destroy(model) == PWR_STATUS_INVALID_HANDLE);
    CHECK(api->model_destroy(context) == PWR_STATUS_INVALID_HANDLE);
    pwr_model_info info = PWR_MODEL_INFO_INIT;
    CHECK(api->model_get_info(model, &info) == PWR_STATUS_OK);
    CHECK(info.node_count == 1U && info.component_count == 1U && info.state_count == 2U);
    CHECK(info.fidelity == PWR_MODEL_FIDELITY_LINEAR_LUMPED);
    CHECK(info.validation == PWR_MODEL_VALIDATION_UNVERIFIED && info.fingerprint != 0U);
    CHECK(info.step_ns == UINT64_C(1000000) && info.channel_count == 7U);
    uint32_t count = 0;
    CHECK(api->model_get_channels(model, NULL, 0, &count) == PWR_STATUS_CAPACITY_EXCEEDED);
    CHECK(count == 7U);
    pwr_channel_info channels[8] = {{0}};
    channels[0].channel = 999;
    channels[7].channel = 888;
    CHECK(api->model_get_channels(model, channels, 1, &count) == PWR_STATUS_CAPACITY_EXCEEDED);
    CHECK(channels[0].channel == 999U);
    CHECK(api->model_get_channels(model, channels, 8, &count) == PWR_STATUS_OK);
    CHECK(channels[7].channel == 888U);
    CHECK(channels[2].channel == 20U && channels[2].direction == PWR_CHANNEL_INPUT);
    CHECK(channels[2].unit == PWR_UNIT_NM && strcmp(channels[2].quantity, "torque") == 0);

    pwr_instance a, b;
    CHECK(api->instance_create_from_model(model, &a) == PWR_STATUS_OK);
    CHECK(api->instance_create_from_model(model, &b) == PWR_STATUS_OK);
    CHECK(a != model && a != context);
    CHECK(api->model_destroy(model) == PWR_STATUS_IN_USE);
    CHECK(api->context_destroy(a) == PWR_STATUS_INVALID_HANDLE);
    CHECK(api->instance_destroy(model) == PWR_STATUS_INVALID_HANDLE);
    CHECK(api->instance_step(context, 1000000) == PWR_STATUS_INVALID_HANDLE);
    CHECK(api->instance_step(a, UINT64_C(1000000000)) == PWR_STATUS_OK);
    for (uint32_t i = 0; i < 10U; ++i) { CHECK(api->instance_step(b, UINT64_C(100000000)) == PWR_STATUS_OK); }
    double speed;
    const uint64_t hash_a = snapshot_hash(api, a, &speed);
    CHECK(fabs(speed - 2.0) < 1.0e-10);
    CHECK(hash_a == snapshot_hash(api, b, &speed));
    const pwr_scalar_value torque = {20, -4, 0, 0};
    pwr_input_frame frame = PWR_INPUT_FRAME_INIT;
    frame.values = &torque; frame.value_count = 1;
    CHECK(api->instance_submit_inputs(a, &frame) == PWR_STATUS_OK);
    CHECK(snapshot_hash(api, a, &speed) != hash_a);
    CHECK(snapshot_hash(api, b, &speed) == hash_a);
    CHECK(api->instance_step(a, UINT64_C(1000000000)) == PWR_STATUS_OK);
    (void)snapshot_hash(api, a, &speed);
    CHECK(fabs(speed) < 1.0e-10);
    CHECK(api->instance_destroy(a) == PWR_STATUS_OK);
    CHECK(api->model_destroy(model) == PWR_STATUS_IN_USE);
    CHECK(api->instance_destroy(b) == PWR_STATUS_OK);
    CHECK(api->model_destroy(model) == PWR_STATUS_OK);
    CHECK(api->model_destroy(model) == PWR_STATUS_INVALID_HANDLE);
    CHECK(api->model_get_info(model, &info) == PWR_STATUS_INVALID_HANDLE);
    pwr_instance stale = UINT64_MAX;
    CHECK(api->instance_create_from_model(model, &stale) == PWR_STATUS_INVALID_HANDLE);
    CHECK(stale == PWR_INSTANCE_INVALID);
    pwr_model next;
    CHECK(compile_rotor(api, context, &next) == PWR_STATUS_OK && next != model);
    CHECK(api->model_get_info(next, &info) == PWR_STATUS_OK);
    CHECK(api->model_destroy(model) == PWR_STATUS_INVALID_HANDLE);
    CHECK(api->model_destroy(next) == PWR_STATUS_OK);
    CHECK(api->context_destroy(context) == PWR_STATUS_OK);
}

static void test_failure_contracts(const pwr_api *api)
{
    const pwr_context_desc cd = PWR_CONTEXT_DESC_INIT;
    pwr_context context;
    CHECK(api->context_create(&cd, &context) == PWR_STATUS_OK);
    pwr_model model = UINT64_MAX;
    pwr_model_diagnostic diagnostic = PWR_MODEL_DIAGNOSTIC_INIT;
    CHECK(api->model_compile(context, NULL, &model, &diagnostic) == PWR_STATUS_INVALID_ARGUMENT);
    CHECK(model == PWR_MODEL_INVALID);
    const pwr_node_desc node = {1, PWR_NODE_ROTATIONAL,
        PWR_QUANTITY(-1, PWR_UNIT_KG_M2), PWR_QUANTITY(0, PWR_UNIT_RAD_S), PWR_QUANTITY(0, PWR_UNIT_RAD)};
    pwr_model_desc desc = PWR_MODEL_DESC_INIT;
    desc.nodes = &node; desc.node_count = 1;
    struct { pwr_model_diagnostic d; uint64_t canary; } extended = {PWR_MODEL_DIAGNOSTIC_INIT, 123};
    CHECK(api->model_compile(context, &desc, &model, &extended.d) == PWR_STATUS_INVALID_MODEL);
    CHECK(extended.d.code == PWR_MODEL_DIAG_RANGE && extended.d.object_id == 1U);
    CHECK(strcmp(extended.d.field, "storage") == 0 && extended.canary == 123U);
    extended.d.struct_size = 8;
    CHECK(api->model_compile(context, &desc, &model, &extended.d) == PWR_STATUS_STRUCT_TOO_SMALL);
    CHECK(api->context_destroy(context) == PWR_STATUS_OK); /* failed compiles release ownership */

    CHECK(api->context_create(&cd, &context) == PWR_STATUS_OK);
    CHECK(compile_rotor(api, context, &model) == PWR_STATUS_OK);
    pwr_model_info info = PWR_MODEL_INFO_INIT;
    info.struct_size = 8;
    CHECK(api->model_get_info(model, &info) == PWR_STATUS_STRUCT_TOO_SMALL);
    info.struct_size = (uint32_t)sizeof(info); info.abi_version = 99;
    CHECK(api->model_get_info(model, &info) == PWR_STATUS_UNSUPPORTED_ABI);
    CHECK(api->model_get_info(model, NULL) == PWR_STATUS_INVALID_ARGUMENT);
    uint32_t count;
    CHECK(api->model_get_channels(model, NULL, 1, &count) == PWR_STATUS_INVALID_ARGUMENT);
    CHECK(api->model_get_channels(model, NULL, 0, NULL) == PWR_STATUS_INVALID_ARGUMENT);
    CHECK(api->instance_create_from_model(model, NULL) == PWR_STATUS_INVALID_ARGUMENT);
    CHECK(api->model_destroy(model) == PWR_STATUS_OK);
    CHECK(api->context_destroy(context) == PWR_STATUS_OK);
}

static void test_registry_capacity(const pwr_api *api)
{
    const pwr_context_desc desc = PWR_CONTEXT_DESC_INIT;
    pwr_context context;
    CHECK(api->context_create(&desc, &context) == PWR_STATUS_OK);
    pwr_model models[64];
    for (uint32_t i = 0; i < 64U; ++i) { CHECK(compile_rotor(api, context, &models[i]) == PWR_STATUS_OK); }
    pwr_model overflow = UINT64_MAX;
    CHECK(compile_rotor(api, context, &overflow) == PWR_STATUS_CAPACITY_EXCEEDED);
    CHECK(overflow == PWR_MODEL_INVALID);
    pwr_instance instances[256];
    for (uint32_t i = 0; i < 256U; ++i) {
        CHECK(api->instance_create_from_model(models[0], &instances[i]) == PWR_STATUS_OK);
    }
    pwr_instance extra = UINT64_MAX;
    CHECK(api->instance_create_from_model(models[0], &extra) == PWR_STATUS_CAPACITY_EXCEEDED);
    CHECK(extra == PWR_INSTANCE_INVALID);
    for (uint32_t i = 0; i < 256U; ++i) { CHECK(api->instance_destroy(instances[i]) == PWR_STATUS_OK); }
    for (uint32_t i = 0; i < 64U; ++i) { CHECK(api->model_destroy(models[i]) == PWR_STATUS_OK); }
    CHECK(api->context_destroy(context) == PWR_STATUS_OK);
}

static void test_old_api_prefix(void)
{
    /* An actual old-layout caller buffer, including a trailing canary. */
    typedef struct old_api {
        uint32_t abi_version;
        uint32_t struct_size;
        pwr_runtime_get_version_fn runtime_get_version;
        pwr_runtime_get_capabilities_fn runtime_get_capabilities;
        pwr_context_create_fn context_create;
        pwr_context_destroy_fn context_destroy;
        pwr_status_string_fn status_string;
        pwr_instance_create_fn instance_create;
        pwr_instance_destroy_fn instance_destroy;
        pwr_instance_submit_inputs_fn instance_submit_inputs;
        pwr_instance_step_fn instance_step;
        pwr_instance_read_snapshot_fn instance_read_snapshot;
    } old_api;
    struct { old_api api; uint64_t canary; } caller = {{0}, UINT64_C(0x123456789abcdef0)};
    _Static_assert(sizeof(old_api) == offsetof(pwr_api, model_compile), "ABI prefix moved");
    CHECK(pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(caller.api), (pwr_api *)(void *)&caller.api) == PWR_STATUS_OK);
    CHECK(caller.canary == UINT64_C(0x123456789abcdef0));
    CHECK(caller.api.instance_step != NULL && caller.api.instance_read_snapshot != NULL);
    pwr_context_desc desc = PWR_CONTEXT_DESC_INIT;
    pwr_context context;
    CHECK(caller.api.context_create(&desc, &context) == PWR_STATUS_OK);
    CHECK(caller.api.context_destroy(context) == PWR_STATUS_OK);
}

int main(void)
{
    pwr_api api = {0};
    CHECK(pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(api), &api) == PWR_STATUS_OK);
    pwr_capabilities capabilities = PWR_CAPABILITIES_INIT;
    CHECK(api.runtime_get_capabilities(&capabilities) == PWR_STATUS_OK);
    CHECK((capabilities.feature_bits & PWR_CAP_COMPILED_MODELS) != 0U);
    CHECK((capabilities.feature_bits & PWR_CAP_MODEL_CHANNEL_DISCOVERY) != 0U);
    test_old_api_prefix();
    test_public_workflow(&api);
    test_failure_contracts(&api);
    test_registry_capacity(&api);
    if (failures != 0) { return 1; }
    (void)puts("Power! model SDK tests passed");
    return 0;
}
