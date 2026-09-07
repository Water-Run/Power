#include <power/power.h>

#include <stddef.h>
#include <stdio.h>
#include <string.h>

static int failures;

#define CHECK(expression)                                                       \
    do {                                                                        \
        if (!(expression)) {                                                    \
            (void)fprintf(stderr, "%s:%d: check failed: %s\n", __FILE__,      \
                          __LINE__, #expression);                               \
            ++failures;                                                         \
        }                                                                       \
    } while (0)

static void test_api_negotiation(void)
{
    pwr_api api = {0};
    struct extended_api_buffer {
        pwr_api api;
        uint64_t canary;
    } extended = {{0}, UINT64_C(0x5a5aa5a55a5aa5a5)};

    CHECK(pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(api), NULL) ==
          PWR_STATUS_INVALID_ARGUMENT);
    CHECK(pwr_get_api(PWR_ABI_VERSION + UINT32_C(1),
                      (uint32_t)sizeof(api), &api) ==
          PWR_STATUS_UNSUPPORTED_ABI);
    CHECK(pwr_get_api(PWR_ABI_VERSION,
                      (uint32_t)offsetof(pwr_api, status_string),
                      &api) == PWR_STATUS_STRUCT_TOO_SMALL);
    CHECK(pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(api), &api) ==
          PWR_STATUS_OK);
    CHECK(api.abi_version == PWR_ABI_VERSION);
    CHECK(api.struct_size == (uint32_t)sizeof(api));
    CHECK(api.runtime_get_version != NULL);
    CHECK(api.runtime_get_capabilities != NULL);
    CHECK(api.context_create != NULL);
    CHECK(api.context_destroy != NULL);
    CHECK(api.status_string != NULL);
    CHECK(api.instance_create != NULL);
    CHECK(api.instance_destroy != NULL);
    CHECK(api.instance_submit_inputs != NULL);
    CHECK(api.instance_step != NULL);
    CHECK(api.instance_read_snapshot != NULL);

    CHECK(pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(extended),
                      &extended.api) == PWR_STATUS_OK);
    CHECK(extended.canary == UINT64_C(0x5a5aa5a55a5aa5a5));
}

static void test_runtime_queries(const pwr_api *api)
{
    pwr_version_info version = PWR_VERSION_INFO_INIT;
    pwr_capabilities capabilities = PWR_CAPABILITIES_INIT;

    CHECK(api->runtime_get_version(NULL) == PWR_STATUS_INVALID_ARGUMENT);
    version.abi_version = PWR_ABI_VERSION + UINT32_C(1);
    CHECK(api->runtime_get_version(&version) == PWR_STATUS_UNSUPPORTED_ABI);
    version.abi_version = PWR_ABI_VERSION;
    version.struct_size = UINT32_C(1);
    CHECK(api->runtime_get_version(&version) == PWR_STATUS_STRUCT_TOO_SMALL);
    version.struct_size = (uint32_t)sizeof(version);
    CHECK(api->runtime_get_version(&version) == PWR_STATUS_OK);
    CHECK(version.major == PWR_VERSION_MAJOR);
    CHECK(version.minor == PWR_VERSION_MINOR);
    CHECK(version.patch == PWR_VERSION_PATCH);
    CHECK(version.product_name != NULL);
    CHECK(strcmp(version.product_name, "Power!") == 0);
    CHECK(version.version_string != NULL);

    CHECK(api->runtime_get_capabilities(NULL) == PWR_STATUS_INVALID_ARGUMENT);
    capabilities.abi_version = PWR_ABI_VERSION + UINT32_C(1);
    CHECK(api->runtime_get_capabilities(&capabilities) ==
          PWR_STATUS_UNSUPPORTED_ABI);
    capabilities.abi_version = PWR_ABI_VERSION;
    capabilities.struct_size = UINT32_C(1);
    CHECK(api->runtime_get_capabilities(&capabilities) ==
          PWR_STATUS_STRUCT_TOO_SMALL);
    capabilities.struct_size = (uint32_t)sizeof(capabilities);
    CHECK(api->runtime_get_capabilities(&capabilities) == PWR_STATUS_OK);
    CHECK((capabilities.feature_bits & PWR_CAP_CONTEXT_LIFECYCLE) !=
          UINT64_C(0));
    CHECK((capabilities.feature_bits & PWR_CAP_GENERATION_HANDLES) !=
          UINT64_C(0));
    CHECK((capabilities.feature_bits & PWR_CAP_HEADLESS_FIXED_STEP) !=
          UINT64_C(0));
    CHECK((capabilities.feature_bits & PWR_CAP_BATCH_SCALAR_IO) !=
          UINT64_C(0));
    CHECK(capabilities.max_contexts > UINT32_C(0));

    CHECK(strcmp(api->status_string(PWR_STATUS_INVALID_HANDLE),
                 "invalid or stale handle") == 0);
    CHECK(strcmp(api->status_string(INT32_C(123456)), "unknown status") == 0);
}

static const pwr_scalar_value *find_value(const pwr_snapshot *snapshot,
                                          pwr_channel_id channel)
{
    uint32_t index;

    for (index = UINT32_C(0); index < snapshot->value_count; ++index) {
        if (snapshot->values[index].channel == channel) {
            return &snapshot->values[index];
        }
    }
    return NULL;
}

static void test_instance_lifecycle_and_step(const pwr_api *api)
{
    pwr_context_desc context_desc = PWR_CONTEXT_DESC_INIT;
    pwr_instance_desc instance_desc = PWR_INSTANCE_DESC_INIT;
    pwr_context context = PWR_CONTEXT_INVALID;
    pwr_instance first = PWR_INSTANCE_INVALID;
    pwr_instance second = PWR_INSTANCE_INVALID;
    pwr_scalar_value input_values[] = {
        {PWR_INPUT_TARGET_SPEED_RAD_S, 160.0, UINT32_C(0), UINT32_C(0)},
        {PWR_INPUT_LOAD_TORQUE_NM, 20.0, UINT32_C(0), UINT32_C(0)},
    };
    pwr_input_frame input = PWR_INPUT_FRAME_INIT;
    pwr_scalar_value first_values[32] = {{0}};
    pwr_scalar_value second_values[32] = {{0}};
    pwr_snapshot first_snapshot = PWR_SNAPSHOT_INIT;
    pwr_snapshot second_snapshot = PWR_SNAPSHOT_INIT;
    const pwr_scalar_value *speed;

    CHECK(api->context_create(&context_desc, &context) == PWR_STATUS_OK);
    CHECK(api->instance_create(context, NULL, &first) ==
          PWR_STATUS_INVALID_ARGUMENT);
    instance_desc.model = UINT32_C(999);
    CHECK(api->instance_create(context, &instance_desc, &first) ==
          PWR_STATUS_UNSUPPORTED);
    instance_desc.model = PWR_BUILTIN_MODEL_CONTROLLED_SHAFT;
    CHECK(api->instance_create(context, &instance_desc, &first) ==
          PWR_STATUS_OK);
    CHECK(api->instance_create(context, &instance_desc, &second) ==
          PWR_STATUS_OK);
    CHECK(api->context_destroy(context) == PWR_STATUS_IN_USE);

    input.values = input_values;
    input.value_count = (uint32_t)(sizeof(input_values) / sizeof(input_values[0]));
    CHECK(api->instance_submit_inputs(first, &input) == PWR_STATUS_OK);
    CHECK(api->instance_submit_inputs(second, &input) == PWR_STATUS_OK);
    CHECK(api->instance_step(first, UINT64_C(1)) ==
          PWR_STATUS_INVALID_TIME_STEP);
    CHECK(api->instance_step(first, UINT64_C(3000000000)) == PWR_STATUS_OK);
    CHECK(api->instance_step(second, UINT64_C(3000000000)) == PWR_STATUS_OK);

    CHECK(api->instance_read_snapshot(first, &first_snapshot) ==
          PWR_STATUS_CAPACITY_EXCEEDED);
    CHECK(first_snapshot.value_count > UINT32_C(0));
    first_snapshot.values = first_values;
    first_snapshot.value_capacity =
        (uint32_t)(sizeof(first_values) / sizeof(first_values[0]));
    second_snapshot.values = second_values;
    second_snapshot.value_capacity =
        (uint32_t)(sizeof(second_values) / sizeof(second_values[0]));
    CHECK(api->instance_read_snapshot(first, &first_snapshot) == PWR_STATUS_OK);
    CHECK(api->instance_read_snapshot(second, &second_snapshot) == PWR_STATUS_OK);
    CHECK(first_snapshot.simulation_time_ns == UINT64_C(3000000000));
    CHECK(first_snapshot.state_hash == second_snapshot.state_hash);
    CHECK(first_snapshot.value_count == second_snapshot.value_count);
    speed = find_value(&first_snapshot, PWR_OUTPUT_LOAD_SPEED_RAD_S);
    CHECK(speed != NULL);
    if (speed != NULL) {
        CHECK(speed->value > 120.0);
        CHECK(speed->value < 200.0);
    }

    input_values[0].channel = UINT64_C(0xffffffffffffffff);
    CHECK(api->instance_submit_inputs(first, &input) ==
          PWR_STATUS_UNKNOWN_CHANNEL);

    CHECK(api->instance_destroy(first) == PWR_STATUS_OK);
    CHECK(api->instance_destroy(first) == PWR_STATUS_INVALID_HANDLE);
    CHECK(api->instance_read_snapshot(first, &first_snapshot) ==
          PWR_STATUS_INVALID_HANDLE);
    CHECK(api->instance_destroy(second) == PWR_STATUS_OK);
    CHECK(api->context_destroy(context) == PWR_STATUS_OK);
}

static void test_context_lifecycle(const pwr_api *api)
{
    pwr_context_desc desc = PWR_CONTEXT_DESC_INIT;
    pwr_context first = PWR_CONTEXT_INVALID;
    pwr_context second = PWR_CONTEXT_INVALID;

    CHECK(api->context_create(NULL, &first) == PWR_STATUS_INVALID_ARGUMENT);
    CHECK(first == PWR_CONTEXT_INVALID);
    CHECK(api->context_create(&desc, NULL) == PWR_STATUS_INVALID_ARGUMENT);

    desc.abi_version = PWR_ABI_VERSION + UINT32_C(1);
    CHECK(api->context_create(&desc, &first) == PWR_STATUS_UNSUPPORTED_ABI);
    CHECK(first == PWR_CONTEXT_INVALID);
    desc.abi_version = PWR_ABI_VERSION;
    desc.struct_size = UINT32_C(1);
    CHECK(api->context_create(&desc, &first) == PWR_STATUS_STRUCT_TOO_SMALL);
    desc.struct_size = (uint32_t)sizeof(desc);
    desc.flags = UINT32_C(1);
    CHECK(api->context_create(&desc, &first) == PWR_STATUS_UNSUPPORTED);
    desc.flags = UINT32_C(0);

    CHECK(api->context_destroy(PWR_CONTEXT_INVALID) ==
          PWR_STATUS_INVALID_HANDLE);
    CHECK(api->context_create(&desc, &first) == PWR_STATUS_OK);
    CHECK(first != PWR_CONTEXT_INVALID);
    CHECK(api->context_destroy(first) == PWR_STATUS_OK);
    CHECK(api->context_destroy(first) == PWR_STATUS_INVALID_HANDLE);

    CHECK(api->context_create(&desc, &second) == PWR_STATUS_OK);
    CHECK(second != PWR_CONTEXT_INVALID);
    CHECK(second != first);
    CHECK(api->context_destroy(first) == PWR_STATUS_INVALID_HANDLE);
    CHECK(api->context_destroy(second) == PWR_STATUS_OK);
}

static void test_context_capacity(const pwr_api *api)
{
    pwr_capabilities capabilities = PWR_CAPABILITIES_INIT;
    pwr_context_desc desc = PWR_CONTEXT_DESC_INIT;
    pwr_context contexts[256] = {PWR_CONTEXT_INVALID};
    pwr_context overflow = PWR_CONTEXT_INVALID;
    uint32_t index;

    CHECK(api->runtime_get_capabilities(&capabilities) == PWR_STATUS_OK);
    CHECK(capabilities.max_contexts == UINT32_C(256));

    for (index = UINT32_C(0); index < capabilities.max_contexts; ++index) {
        CHECK(api->context_create(&desc, &contexts[index]) == PWR_STATUS_OK);
    }
    CHECK(api->context_create(&desc, &overflow) ==
          PWR_STATUS_CAPACITY_EXCEEDED);
    CHECK(overflow == PWR_CONTEXT_INVALID);

    for (index = UINT32_C(0); index < capabilities.max_contexts; ++index) {
        CHECK(api->context_destroy(contexts[index]) == PWR_STATUS_OK);
    }
}

int main(void)
{
    pwr_api api = {0};

    test_api_negotiation();
    CHECK(pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(api), &api) ==
          PWR_STATUS_OK);
    test_runtime_queries(&api);
    test_context_lifecycle(&api);
    test_context_capacity(&api);
    test_instance_lifecycle_and_step(&api);

    if (failures != 0) {
        (void)fprintf(stderr, "%d SDK test(s) failed\n", failures);
        return 1;
    }

    (void)puts("Power! SDK tests passed");
    return 0;
}
