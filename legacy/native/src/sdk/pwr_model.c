#include "pwr_internal.h"

#include <stdatomic.h>
#include <stddef.h>
#include <stdlib.h>
#include <string.h>

#define PWR_MODEL_HANDLE_TAG UINT32_C(0x4d000000)

typedef struct pwr_model_slot {
    uint32_t generation;
    uint32_t references;
    pwr_context context;
    pwr_graph *graph;
} pwr_model_slot;

typedef struct pwr_graph_runtime {
    const pwr_graph *graph;
    pwr_graph_state state;
} pwr_graph_runtime;

static atomic_flag pwr_model_lock = ATOMIC_FLAG_INIT;
static pwr_model_slot pwr_model_slots[PWR_MAX_MODELS];

static void pwr_lock_models(void)
{
    while (atomic_flag_test_and_set_explicit(&pwr_model_lock, memory_order_acquire)) { }
}

static void pwr_unlock_models(void)
{
    atomic_flag_clear_explicit(&pwr_model_lock, memory_order_release);
}

/* Must be called with the model registry locked. */
static pwr_model_slot *pwr_model_lookup(pwr_model model)
{
    const uint32_t encoded = (uint32_t)model;
    const uint32_t generation = (uint32_t)(model >> 32);
    const uint32_t index = encoded & UINT32_C(0x00ffffff);
    if ((encoded & UINT32_C(0xff000000)) != PWR_MODEL_HANDLE_TAG ||
        generation == 0U || index == 0U || index > PWR_MAX_MODELS) { return NULL; }
    pwr_model_slot *slot = &pwr_model_slots[index - 1U];
    return slot->graph != NULL && slot->generation == generation ? slot : NULL;
}

pwr_status pwr_model_acquire(pwr_model model, const pwr_graph **graph, pwr_context *context)
{
    pwr_lock_models();
    pwr_model_slot *slot = pwr_model_lookup(model);
    if (slot == NULL) { pwr_unlock_models(); return PWR_STATUS_INVALID_HANDLE; }
    if (slot->references == UINT32_MAX) { pwr_unlock_models(); return PWR_STATUS_CAPACITY_EXCEEDED; }
    ++slot->references;
    *graph = slot->graph;
    *context = slot->context;
    pwr_unlock_models();
    return PWR_STATUS_OK;
}

void pwr_model_release(pwr_model model)
{
    pwr_lock_models();
    pwr_model_slot *slot = pwr_model_lookup(model);
    if (slot != NULL && slot->references != 0U) { --slot->references; }
    pwr_unlock_models();
}

pwr_status PWR_CALL pwr_model_compile_impl(pwr_context context,
    const pwr_model_desc *desc, pwr_model *model, pwr_model_diagnostic *diagnostic)
{
    if (model == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    *model = PWR_MODEL_INVALID;
    pwr_status status = pwr_context_retain_impl(context);
    if (status != PWR_STATUS_OK) { return status; }
    pwr_graph *graph = malloc(sizeof(*graph));
    if (graph == NULL) {
        pwr_context_release_impl(context);
        return PWR_STATUS_OUT_OF_MEMORY;
    }
    status = pwr_graph_compile(desc, graph, diagnostic);
    if (status != PWR_STATUS_OK) {
        free(graph);
        pwr_context_release_impl(context);
        return status;
    }
    pwr_lock_models();
    for (uint32_t i = 0; i < PWR_MAX_MODELS; ++i) {
        pwr_model_slot *slot = &pwr_model_slots[i];
        if (slot->graph != NULL) { continue; }
        if (slot->generation == 0U) { slot->generation = 1; }
        slot->context = context;
        slot->references = 0;
        slot->graph = graph;
        *model = ((uint64_t)slot->generation << 32) | PWR_MODEL_HANDLE_TAG | ((uint64_t)i + 1U);
        pwr_unlock_models();
        return PWR_STATUS_OK;
    }
    pwr_unlock_models();
    free(graph);
    pwr_context_release_impl(context);
    return PWR_STATUS_CAPACITY_EXCEEDED;
}

pwr_status PWR_CALL pwr_model_destroy_impl(pwr_model model)
{
    pwr_lock_models();
    pwr_model_slot *slot = pwr_model_lookup(model);
    if (slot == NULL) { pwr_unlock_models(); return PWR_STATUS_INVALID_HANDLE; }
    if (slot->references != 0U) { pwr_unlock_models(); return PWR_STATUS_IN_USE; }
    pwr_graph *graph = slot->graph;
    const pwr_context context = slot->context;
    slot->graph = NULL;
    slot->context = PWR_CONTEXT_INVALID;
    ++slot->generation;
    if (slot->generation == 0U) { slot->generation = 1; }
    pwr_unlock_models();
    free(graph);
    pwr_context_release_impl(context);
    return PWR_STATUS_OK;
}

pwr_status PWR_CALL pwr_model_get_info_impl(pwr_model model, pwr_model_info *info)
{
    if (info == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    if (info->abi_version != PWR_ABI_VERSION) { return PWR_STATUS_UNSUPPORTED_ABI; }
    if (info->struct_size < sizeof(*info)) { return PWR_STATUS_STRUCT_TOO_SMALL; }
    const pwr_graph *graph = NULL;
    pwr_context context;
    const pwr_status status = pwr_model_acquire(model, &graph, &context);
    if (status != PWR_STATUS_OK) { return status; }
    *info = (pwr_model_info){
        .abi_version = PWR_ABI_VERSION,
        .struct_size = (uint32_t)sizeof(*info),
        .fingerprint = graph->fingerprint,
        .step_ns = graph->step_ns,
        .node_count = graph->node_count,
        .component_count = graph->component_count,
        .state_count = graph->dynamic_count + graph->thermal_count,
        .channel_count = graph->channel_count,
        .fidelity = PWR_MODEL_FIDELITY_LINEAR_LUMPED,
        .validation = PWR_MODEL_VALIDATION_UNVERIFIED
    };
    pwr_model_release(model);
    return PWR_STATUS_OK;
}

pwr_status PWR_CALL pwr_model_get_channels_impl(pwr_model model,
    pwr_channel_info *channels, uint32_t capacity, uint32_t *count)
{
    if (count == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    *count = 0;
    if (channels == NULL && capacity != 0U) { return PWR_STATUS_INVALID_ARGUMENT; }
    const pwr_graph *graph = NULL;
    pwr_context context;
    pwr_status status = pwr_model_acquire(model, &graph, &context);
    if (status != PWR_STATUS_OK) { return status; }
    *count = graph->channel_count;
    if (capacity < graph->channel_count) { status = PWR_STATUS_CAPACITY_EXCEEDED; }
    else { (void)memcpy(channels, graph->channels, graph->channel_count * sizeof(*channels)); }
    pwr_model_release(model);
    return status;
}

static pwr_status pwr_model_submit(void *opaque, const pwr_input_frame *frame)
{
    pwr_graph_runtime *r = opaque;
    return pwr_graph_submit(r->graph, &r->state, frame);
}

static pwr_status pwr_model_step(void *opaque, uint64_t delta_ns)
{
    pwr_graph_runtime *r = opaque;
    return pwr_graph_step(r->graph, &r->state, delta_ns);
}

static pwr_status pwr_model_snapshot(const void *opaque, pwr_snapshot *snapshot)
{
    const pwr_graph_runtime *r = opaque;
    return pwr_graph_snapshot(r->graph, &r->state, snapshot);
}

static const pwr_instance_backend pwr_model_backend = {
    pwr_model_submit, pwr_model_step, pwr_model_snapshot, free
};

pwr_status PWR_CALL pwr_instance_create_from_model_impl(pwr_model model, pwr_instance *instance)
{
    if (instance == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    *instance = PWR_INSTANCE_INVALID;
    const pwr_graph *graph = NULL;
    pwr_context context;
    pwr_status status = pwr_model_acquire(model, &graph, &context);
    if (status != PWR_STATUS_OK) { return status; }
    pwr_graph_runtime *runtime = malloc(sizeof(*runtime));
    if (runtime == NULL) { pwr_model_release(model); return PWR_STATUS_OUT_OF_MEMORY; }
    runtime->graph = graph;
    status = pwr_graph_init(graph, &runtime->state);
    if (status == PWR_STATUS_OK) {
        status = pwr_instance_register(context, model, &pwr_model_backend, runtime, instance);
    }
    if (status != PWR_STATUS_OK) { free(runtime); pwr_model_release(model); }
    return status;
}
