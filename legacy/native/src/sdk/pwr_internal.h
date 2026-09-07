#ifndef PWR_INTERNAL_H
#define PWR_INTERNAL_H

#include <power/power.h>
#include "pwr_graph.h"

#define PWR_MAX_CONTEXTS UINT32_C(256)
#define PWR_MAX_INSTANCES UINT32_C(256)
#define PWR_MAX_MODELS UINT32_C(64)

typedef struct pwr_instance_backend {
    pwr_status (*submit)(void *state, const pwr_input_frame *frame);
    pwr_status (*step)(void *state, uint64_t delta_ns);
    pwr_status (*snapshot)(const void *state, pwr_snapshot *snapshot);
    void (*destroy)(void *state);
} pwr_instance_backend;

extern const pwr_instance_backend pwr_shaft_backend;
pwr_status pwr_shaft_backend_create(void **state);
pwr_status pwr_instance_register(pwr_context context, pwr_model model,
                                 const pwr_instance_backend *backend,
                                 void *state, pwr_instance *instance);
pwr_status pwr_model_acquire(pwr_model model, const pwr_graph **graph,
                             pwr_context *context);
void pwr_model_release(pwr_model model);
pwr_status PWR_CALL pwr_model_compile_impl(pwr_context context,
    const pwr_model_desc *desc, pwr_model *model, pwr_model_diagnostic *diagnostic);
pwr_status PWR_CALL pwr_model_destroy_impl(pwr_model model);
pwr_status PWR_CALL pwr_model_get_info_impl(pwr_model model, pwr_model_info *info);
pwr_status PWR_CALL pwr_model_get_channels_impl(pwr_model model,
    pwr_channel_info *channels, uint32_t capacity, uint32_t *count);
pwr_status PWR_CALL pwr_instance_create_from_model_impl(pwr_model model,
                                                       pwr_instance *instance);

pwr_status PWR_CALL pwr_runtime_get_version_impl(pwr_version_info *info);
pwr_status PWR_CALL
pwr_runtime_get_capabilities_impl(pwr_capabilities *capabilities);
pwr_status PWR_CALL pwr_context_create_impl(const pwr_context_desc *desc,
                                            pwr_context *context);
pwr_status PWR_CALL pwr_context_destroy_impl(pwr_context context);
pwr_status pwr_context_retain_impl(pwr_context context);
void pwr_context_release_impl(pwr_context context);
pwr_status PWR_CALL pwr_instance_create_impl(
    pwr_context context, const pwr_instance_desc *desc, pwr_instance *instance);
pwr_status PWR_CALL pwr_instance_destroy_impl(pwr_instance instance);
pwr_status PWR_CALL pwr_instance_submit_inputs_impl(
    pwr_instance instance, const pwr_input_frame *frame);
pwr_status PWR_CALL pwr_instance_step_impl(pwr_instance instance,
                                           uint64_t delta_ns);
pwr_status PWR_CALL pwr_instance_read_snapshot_impl(
    pwr_instance instance, pwr_snapshot *snapshot);
const char *PWR_CALL pwr_status_string_impl(pwr_status status);

#endif
