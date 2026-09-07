/* Public model ABI example. Also valid as a C++17 translation unit. */
#include <power/power.h>

#include <inttypes.h>
#include <stdio.h>

int main(void)
{
    pwr_api api = {};
    pwr_context context = PWR_CONTEXT_INVALID;
    pwr_model model = PWR_MODEL_INVALID;
    pwr_instance instance = PWR_INSTANCE_INVALID;
    const pwr_context_desc context_desc = PWR_CONTEXT_DESC_INIT;
    const pwr_node_desc rotor = {1, PWR_NODE_ROTATIONAL,
        PWR_QUANTITY(2, PWR_UNIT_KG_M2), PWR_QUANTITY(0, PWR_UNIT_RAD_S),
        PWR_QUANTITY(0, PWR_UNIT_RAD)};
    pwr_component_desc torque = {};
    pwr_model_desc model_desc = PWR_MODEL_DESC_INIT;
    pwr_model_diagnostic diagnostic = PWR_MODEL_DIAGNOSTIC_INIT;
    pwr_model_info info = PWR_MODEL_INFO_INIT;
    pwr_snapshot snapshot = PWR_SNAPSHOT_INIT;
    pwr_scalar_value values[6] = {};
    pwr_status status = pwr_get_api(PWR_ABI_VERSION, (uint32_t)sizeof(api), &api);
    pwr_status cleanup_status = PWR_STATUS_OK;
    if (status != PWR_STATUS_OK || api.struct_size < sizeof(api) || api.model_compile == NULL) {
        (void)fprintf(stderr, "The compiled-model ABI extension is required.\n");
        return 1;
    }
    torque.id = 10;
    torque.kind = PWR_COMPONENT_TORQUE_SOURCE;
    torque.node_a = 1;
    torque.input_channel = 20;
    torque.initial_input.value = 4;
    torque.initial_input.unit = PWR_UNIT_NM;
    model_desc.nodes = &rotor;
    model_desc.node_count = 1;
    model_desc.components = &torque;
    model_desc.component_count = 1;
    snapshot.values = values;
    snapshot.value_capacity = 6;

#define RUN(call_) do { status = (call_); if (status != PWR_STATUS_OK) { goto cleanup; } } while (0)
    RUN(api.context_create(&context_desc, &context));
    RUN(api.model_compile(context, &model_desc, &model, &diagnostic));
    RUN(api.model_get_info(model, &info));
    RUN(api.instance_create_from_model(model, &instance));
    RUN(api.instance_step(instance, UINT64_C(1000000000)));
    RUN(api.instance_read_snapshot(instance, &snapshot));
    (void)printf("{\"model_fingerprint\":\"%016" PRIx64 "\",\"time_ns\":%" PRIu64
                 ",\"speed_rad_s\":%.12g,\"energy_residual_j\":%.12g,"
                 "\"state_hash\":\"%016" PRIx64 "\"}\n",
                 info.fingerprint, snapshot.simulation_time_ns, values[1].value,
                 values[5].value, snapshot.state_hash);
#undef RUN

cleanup:
    if (status != PWR_STATUS_OK) {
        (void)fprintf(stderr, "%s; object %" PRIu32 " %s: %s\n",
                      api.status_string(status), diagnostic.object_id,
                      diagnostic.field, diagnostic.message);
    }
    if (instance != PWR_INSTANCE_INVALID) { cleanup_status = api.instance_destroy(instance); }
    if (model != PWR_MODEL_INVALID) {
        const pwr_status result = api.model_destroy(model);
        if (result != PWR_STATUS_OK) { cleanup_status = result; }
    }
    if (context != PWR_CONTEXT_INVALID) {
        const pwr_status result = api.context_destroy(context);
        if (result != PWR_STATUS_OK) { cleanup_status = result; }
    }
    return status == PWR_STATUS_OK && cleanup_status == PWR_STATUS_OK ? 0 : 1;
}
