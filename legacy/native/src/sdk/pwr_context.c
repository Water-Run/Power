#include "pwr_internal.h"

#include <stdatomic.h>
#include <stddef.h>

typedef struct pwr_context_slot {
    uint32_t generation;
    uint32_t flags;
    uint32_t active;
    uint32_t reference_count;
} pwr_context_slot;

static atomic_flag pwr_registry_lock = ATOMIC_FLAG_INIT;
static pwr_context_slot pwr_context_slots[PWR_MAX_CONTEXTS];

static void pwr_lock_registry(void)
{
    while (atomic_flag_test_and_set_explicit(&pwr_registry_lock,
                                              memory_order_acquire)) {
    }
}

static void pwr_unlock_registry(void)
{
    atomic_flag_clear_explicit(&pwr_registry_lock, memory_order_release);
}

static pwr_context pwr_make_handle(uint32_t slot_index, uint32_t generation)
{
    return ((uint64_t)generation << 32) | UINT64_C(0x43000000) |
           ((uint64_t)slot_index + UINT64_C(1));
}

static uint32_t pwr_handle_generation(pwr_context context)
{
    return (uint32_t)(context >> 32);
}

static pwr_status pwr_handle_slot(pwr_context context, uint32_t *slot_index)
{
    const uint64_t encoded_slot = context & UINT64_C(0x00ffffff);

    if ((context & UINT64_C(0xff000000)) != UINT64_C(0x43000000) ||
        (encoded_slot == UINT64_C(0)) ||
        (encoded_slot > (uint64_t)PWR_MAX_CONTEXTS)) {
        return PWR_STATUS_INVALID_HANDLE;
    }

    *slot_index = (uint32_t)(encoded_slot - UINT64_C(1));
    return PWR_STATUS_OK;
}

pwr_status PWR_CALL pwr_context_create_impl(const pwr_context_desc *desc,
                                            pwr_context *context)
{
    uint32_t slot_index;

    if (context == NULL) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    *context = PWR_CONTEXT_INVALID;

    if (desc == NULL) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (desc->abi_version != PWR_ABI_VERSION) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if ((size_t)desc->struct_size < sizeof(*desc)) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    if ((desc->flags != UINT32_C(0)) || (desc->reserved != UINT32_C(0))) {
        return PWR_STATUS_UNSUPPORTED;
    }

    pwr_lock_registry();
    for (slot_index = UINT32_C(0); slot_index < PWR_MAX_CONTEXTS;
         ++slot_index) {
        pwr_context_slot *const slot = &pwr_context_slots[slot_index];

        if (slot->active == UINT32_C(0)) {
            if (slot->generation == UINT32_C(0)) {
                slot->generation = UINT32_C(1);
            }
            slot->flags = desc->flags;
            slot->reference_count = UINT32_C(0);
            slot->active = UINT32_C(1);
            *context = pwr_make_handle(slot_index, slot->generation);
            pwr_unlock_registry();
            return PWR_STATUS_OK;
        }
    }
    pwr_unlock_registry();

    return PWR_STATUS_CAPACITY_EXCEEDED;
}

pwr_status PWR_CALL pwr_context_destroy_impl(pwr_context context)
{
    const uint32_t generation = pwr_handle_generation(context);
    uint32_t slot_index = UINT32_C(0);
    pwr_context_slot *slot;

    if ((generation == UINT32_C(0)) ||
        (pwr_handle_slot(context, &slot_index) != PWR_STATUS_OK)) {
        return PWR_STATUS_INVALID_HANDLE;
    }

    pwr_lock_registry();
    slot = &pwr_context_slots[slot_index];
    if ((slot->active == UINT32_C(0)) || (slot->generation != generation)) {
        pwr_unlock_registry();
        return PWR_STATUS_INVALID_HANDLE;
    }
    if (slot->reference_count != UINT32_C(0)) {
        pwr_unlock_registry();
        return PWR_STATUS_IN_USE;
    }

    slot->active = UINT32_C(0);
    slot->flags = UINT32_C(0);
    ++slot->generation;
    if (slot->generation == UINT32_C(0)) {
        slot->generation = UINT32_C(1);
    }
    pwr_unlock_registry();
    return PWR_STATUS_OK;
}

pwr_status pwr_context_retain_impl(pwr_context context)
{
    const uint32_t generation = pwr_handle_generation(context);
    uint32_t slot_index = UINT32_C(0);
    pwr_context_slot *slot;

    if ((generation == UINT32_C(0)) ||
        (pwr_handle_slot(context, &slot_index) != PWR_STATUS_OK)) {
        return PWR_STATUS_INVALID_HANDLE;
    }

    pwr_lock_registry();
    slot = &pwr_context_slots[slot_index];
    if ((slot->active == UINT32_C(0)) || (slot->generation != generation)) {
        pwr_unlock_registry();
        return PWR_STATUS_INVALID_HANDLE;
    }
    if (slot->reference_count == UINT32_MAX) {
        pwr_unlock_registry();
        return PWR_STATUS_CAPACITY_EXCEEDED;
    }
    ++slot->reference_count;
    pwr_unlock_registry();
    return PWR_STATUS_OK;
}

void pwr_context_release_impl(pwr_context context)
{
    const uint32_t generation = pwr_handle_generation(context);
    uint32_t slot_index = UINT32_C(0);

    if ((generation == UINT32_C(0)) ||
        (pwr_handle_slot(context, &slot_index) != PWR_STATUS_OK)) {
        return;
    }

    pwr_lock_registry();
    pwr_context_slot *const slot = &pwr_context_slots[slot_index];
    if ((slot->active != UINT32_C(0)) && (slot->generation == generation) &&
        (slot->reference_count != UINT32_C(0))) {
        --slot->reference_count;
    }
    pwr_unlock_registry();
}
