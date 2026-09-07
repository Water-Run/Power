#include "pwr_internal.h"

#include <stdatomic.h>
#include <stddef.h>

typedef struct pwr_instance_slot {
    uint32_t generation;
    uint32_t active;
    pwr_context context;
    atomic_bool busy;
    pwr_model model;
    const pwr_instance_backend *backend;
    void *state;
} pwr_instance_slot;

static atomic_flag pwr_instance_registry_lock = ATOMIC_FLAG_INIT;
static pwr_instance_slot pwr_instance_slots[PWR_MAX_INSTANCES];

static void pwr_lock_instance_registry(void)
{
    while (atomic_flag_test_and_set_explicit(&pwr_instance_registry_lock,
                                              memory_order_acquire)) {
    }
}

static void pwr_unlock_instance_registry(void)
{
    atomic_flag_clear_explicit(&pwr_instance_registry_lock,
                               memory_order_release);
}

static pwr_instance pwr_make_instance_handle(uint32_t slot_index,
                                             uint32_t generation)
{
    return ((uint64_t)generation << 32) | UINT64_C(0x49000000) |
           ((uint64_t)slot_index + UINT64_C(1));
}

static pwr_status pwr_decode_instance_handle(pwr_instance instance,
                                             uint32_t *slot_index,
                                             uint32_t *generation)
{
    const uint64_t encoded_slot = instance & UINT64_C(0x00ffffff);
    const uint32_t decoded_generation = (uint32_t)(instance >> 32);

    if ((slot_index == NULL) || (generation == NULL) ||
        (instance & UINT64_C(0xff000000)) != UINT64_C(0x49000000) ||
        (decoded_generation == UINT32_C(0)) ||
        (encoded_slot == UINT64_C(0)) ||
        (encoded_slot > (uint64_t)PWR_MAX_INSTANCES)) {
        return PWR_STATUS_INVALID_HANDLE;
    }
    *slot_index = (uint32_t)(encoded_slot - UINT64_C(1));
    *generation = decoded_generation;
    return PWR_STATUS_OK;
}

static pwr_status pwr_instance_begin(pwr_instance instance,
                                     pwr_instance_slot **slot_out)
{
    uint32_t slot_index = UINT32_C(0);
    uint32_t generation = UINT32_C(0);
    pwr_instance_slot *slot;
    bool expected = false;

    if ((slot_out == NULL) ||
        (pwr_decode_instance_handle(instance, &slot_index, &generation) !=
         PWR_STATUS_OK)) {
        return PWR_STATUS_INVALID_HANDLE;
    }

    pwr_lock_instance_registry();
    slot = &pwr_instance_slots[slot_index];
    if ((slot->active == UINT32_C(0)) || (slot->generation != generation)) {
        pwr_unlock_instance_registry();
        return PWR_STATUS_INVALID_HANDLE;
    }
    if (!atomic_compare_exchange_strong_explicit(&slot->busy, &expected, true,
                                                  memory_order_acquire,
                                                  memory_order_relaxed)) {
        pwr_unlock_instance_registry();
        return PWR_STATUS_BUSY;
    }
    pwr_unlock_instance_registry();
    *slot_out = slot;
    return PWR_STATUS_OK;
}

static void pwr_instance_end(pwr_instance_slot *slot)
{
    atomic_store_explicit(&slot->busy, false, memory_order_release);
}

static pwr_status pwr_validate_instance_desc(const pwr_instance_desc *desc)
{
    if (desc == NULL) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (desc->abi_version != PWR_ABI_VERSION) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if ((size_t)desc->struct_size < sizeof(*desc)) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    if ((desc->flags != UINT32_C(0)) || (desc->seed != UINT64_C(0))) {
        return PWR_STATUS_UNSUPPORTED;
    }
    if (desc->model != (uint32_t)PWR_BUILTIN_MODEL_CONTROLLED_SHAFT) {
        return PWR_STATUS_UNSUPPORTED;
    }
    return PWR_STATUS_OK;
}

pwr_status pwr_instance_register(pwr_context context, pwr_model model,
                                 const pwr_instance_backend *backend,
                                 void *state, pwr_instance *instance)
{
    const pwr_status status = pwr_context_retain_impl(context);
    if (status != PWR_STATUS_OK) { return status; }
    pwr_lock_instance_registry();
    for (uint32_t index = 0; index < PWR_MAX_INSTANCES; ++index) {
        pwr_instance_slot *slot = &pwr_instance_slots[index];
        if (slot->active != 0U) { continue; }
        if (slot->generation == 0U) { slot->generation = 1; }
        slot->context = context;
        slot->model = model;
        slot->backend = backend;
        slot->state = state;
        atomic_store_explicit(&slot->busy, false, memory_order_relaxed);
        slot->active = 1;
        *instance = pwr_make_instance_handle(index, slot->generation);
        pwr_unlock_instance_registry();
        return PWR_STATUS_OK;
    }
    pwr_unlock_instance_registry();
    pwr_context_release_impl(context);
    return PWR_STATUS_CAPACITY_EXCEEDED;
}

pwr_status PWR_CALL pwr_instance_create_impl(
    pwr_context context, const pwr_instance_desc *desc, pwr_instance *instance)
{
    if (instance == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    *instance = PWR_INSTANCE_INVALID;
    pwr_status status = pwr_validate_instance_desc(desc);
    if (status != PWR_STATUS_OK) { return status; }
    void *state = NULL;
    status = pwr_shaft_backend_create(&state);
    if (status != PWR_STATUS_OK) { return status; }
    status = pwr_instance_register(context, PWR_MODEL_INVALID, &pwr_shaft_backend, state, instance);
    if (status != PWR_STATUS_OK) { pwr_shaft_backend.destroy(state); }
    return status;
}

pwr_status PWR_CALL pwr_instance_destroy_impl(pwr_instance instance)
{
    uint32_t slot_index = UINT32_C(0);
    uint32_t generation = UINT32_C(0);
    pwr_context context;

    if (pwr_decode_instance_handle(instance, &slot_index, &generation) !=
        PWR_STATUS_OK) {
        return PWR_STATUS_INVALID_HANDLE;
    }

    pwr_lock_instance_registry();
    pwr_instance_slot *const slot = &pwr_instance_slots[slot_index];
    if ((slot->active == UINT32_C(0)) || (slot->generation != generation)) {
        pwr_unlock_instance_registry();
        return PWR_STATUS_INVALID_HANDLE;
    }
    if (atomic_load_explicit(&slot->busy, memory_order_acquire)) {
        pwr_unlock_instance_registry();
        return PWR_STATUS_BUSY;
    }

    context = slot->context;
    const pwr_model model = slot->model;
    const pwr_instance_backend *backend = slot->backend;
    void *state = slot->state;
    slot->model = PWR_MODEL_INVALID;
    slot->backend = NULL;
    slot->state = NULL;
    slot->active = UINT32_C(0);
    slot->context = PWR_CONTEXT_INVALID;
    ++slot->generation;
    if (slot->generation == UINT32_C(0)) {
        slot->generation = UINT32_C(1);
    }
    pwr_unlock_instance_registry();
    backend->destroy(state);
    if (model != PWR_MODEL_INVALID) { pwr_model_release(model); }
    pwr_context_release_impl(context);
    return PWR_STATUS_OK;
}

pwr_status PWR_CALL pwr_instance_submit_inputs_impl(
    pwr_instance instance, const pwr_input_frame *frame)
{
    if (frame == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    if (frame->abi_version != PWR_ABI_VERSION) { return PWR_STATUS_UNSUPPORTED_ABI; }
    if (frame->struct_size < sizeof(*frame)) { return PWR_STATUS_STRUCT_TOO_SMALL; }
    if (frame->flags != 0U || (frame->value_count != 0U && frame->values == NULL)) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    pwr_instance_slot *slot = NULL;
    pwr_status status = pwr_instance_begin(instance, &slot);
    if (status != PWR_STATUS_OK) { return status; }
    status = slot->backend->submit(slot->state, frame);
    pwr_instance_end(slot);
    return status;
}

pwr_status PWR_CALL pwr_instance_step_impl(pwr_instance instance, uint64_t delta_ns)
{
    pwr_instance_slot *slot = NULL;
    pwr_status status = pwr_instance_begin(instance, &slot);
    if (status != PWR_STATUS_OK) { return status; }
    status = slot->backend->step(slot->state, delta_ns);
    pwr_instance_end(slot);
    return status;
}

pwr_status PWR_CALL pwr_instance_read_snapshot_impl(pwr_instance instance, pwr_snapshot *snapshot)
{
    if (snapshot == NULL) { return PWR_STATUS_INVALID_ARGUMENT; }
    if (snapshot->abi_version != PWR_ABI_VERSION) { return PWR_STATUS_UNSUPPORTED_ABI; }
    if (snapshot->struct_size < sizeof(*snapshot)) { return PWR_STATUS_STRUCT_TOO_SMALL; }
    pwr_instance_slot *slot = NULL;
    pwr_status status = pwr_instance_begin(instance, &slot);
    if (status != PWR_STATUS_OK) { return status; }
    status = slot->backend->snapshot(slot->state, snapshot);
    pwr_instance_end(slot);
    return status;
}
