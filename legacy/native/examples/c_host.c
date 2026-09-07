#include <power/power.h>

#include <inttypes.h>
#include <stdio.h>

int main(void)
{
    pwr_api api = {0};
    pwr_version_info version = PWR_VERSION_INFO_INIT;
    pwr_capabilities capabilities = PWR_CAPABILITIES_INIT;
    pwr_context_desc desc = PWR_CONTEXT_DESC_INIT;
    pwr_instance_desc instance_desc = PWR_INSTANCE_DESC_INIT;
    pwr_context context = PWR_CONTEXT_INVALID;
    pwr_instance instance = PWR_INSTANCE_INVALID;
    pwr_scalar_value input_values[] = {
        {PWR_INPUT_TARGET_SPEED_RAD_S, 160.0, UINT32_C(0), UINT32_C(0)},
        {PWR_INPUT_LOAD_TORQUE_NM, 20.0, UINT32_C(0), UINT32_C(0)},
    };
    pwr_input_frame input = PWR_INPUT_FRAME_INIT;
    pwr_scalar_value output_values[32] = {{0}};
    pwr_snapshot snapshot = PWR_SNAPSHOT_INIT;
    pwr_status status;

    status = pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(api), &api);
    if (status != PWR_STATUS_OK) {
        (void)fprintf(stderr, "pwr_get_api failed (%" PRId32 ")\n", status);
        return 1;
    }

    status = api.runtime_get_version(&version);
    if (status != PWR_STATUS_OK) {
        (void)fprintf(stderr, "version query failed: %s\n",
                      api.status_string(status));
        return 1;
    }

    status = api.runtime_get_capabilities(&capabilities);
    if (status != PWR_STATUS_OK) {
        (void)fprintf(stderr, "capability query failed: %s\n",
                      api.status_string(status));
        return 1;
    }

    status = api.context_create(&desc, &context);
    if (status != PWR_STATUS_OK) {
        (void)fprintf(stderr, "context creation failed: %s\n",
                      api.status_string(status));
        return 1;
    }

    (void)printf("%s %s, ABI %" PRIu32 ", context 0x%016" PRIx64 "\n",
                 version.product_name, version.version_string,
                 version.abi_version, context);
    (void)printf("capabilities 0x%016" PRIx64 ", max contexts %" PRIu32
                 "\n",
                 capabilities.feature_bits, capabilities.max_contexts);

    status = api.instance_create(context, &instance_desc, &instance);
    if (status != PWR_STATUS_OK) {
        (void)fprintf(stderr, "instance creation failed: %s\n",
                      api.status_string(status));
        (void)api.context_destroy(context);
        return 1;
    }

    input.values = input_values;
    input.value_count =
        (uint32_t)(sizeof(input_values) / sizeof(input_values[0]));
    status = api.instance_submit_inputs(instance, &input);
    if (status == PWR_STATUS_OK) {
        status = api.instance_step(instance, UINT64_C(2000000000));
    }
    snapshot.values = output_values;
    snapshot.value_capacity =
        (uint32_t)(sizeof(output_values) / sizeof(output_values[0]));
    if (status == PWR_STATUS_OK) {
        status = api.instance_read_snapshot(instance, &snapshot);
    }
    if (status != PWR_STATUS_OK) {
        (void)fprintf(stderr, "simulation failed: %s\n",
                      api.status_string(status));
        (void)api.instance_destroy(instance);
        (void)api.context_destroy(context);
        return 1;
    }

    double load_speed = 0.0;
    double energy_residual = 0.0;
    for (uint32_t index = UINT32_C(0); index < snapshot.value_count; ++index) {
        if (snapshot.values[index].channel == PWR_OUTPUT_LOAD_SPEED_RAD_S) {
            load_speed = snapshot.values[index].value;
        }
        else if (snapshot.values[index].channel == PWR_OUTPUT_ENERGY_RESIDUAL_J) {
            energy_residual = snapshot.values[index].value;
        }
    }
    (void)printf("stepped %.3f s, load speed %.3f rad/s, residual %.6f J, "
                 "hash 0x%016" PRIx64 "\n",
                 (double)snapshot.simulation_time_ns * 1.0e-9, load_speed,
                 energy_residual, snapshot.state_hash);

    status = api.instance_destroy(instance);
    if (status != PWR_STATUS_OK) {
        (void)fprintf(stderr, "instance destruction failed: %s\n",
                      api.status_string(status));
        (void)api.context_destroy(context);
        return 1;
    }

    status = api.context_destroy(context);
    if (status != PWR_STATUS_OK) {
        (void)fprintf(stderr, "context destruction failed: %s\n",
                      api.status_string(status));
        return 1;
    }

    return 0;
}
