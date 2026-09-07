// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#include "pwr_internal.h"

#include <stddef.h>
#include <string.h>

#if !defined(PWR_BUILD_VERSION)
#define PWR_BUILD_VERSION "0.0.0-unknown"
#endif

static pwr_status pwr_validate_struct(uint32_t abi_version,
                                      uint32_t struct_size,
                                      size_t required_size)
{
    if (abi_version != PWR_ABI_VERSION) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if ((size_t)struct_size < required_size) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }
    return PWR_STATUS_OK;
}

pwr_status PWR_CALL pwr_runtime_get_version_impl(pwr_version_info *info)
{
    pwr_status status;

    if (info == NULL) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }

    status = pwr_validate_struct(info->abi_version, info->struct_size,
                                 sizeof(*info));
    if (status != PWR_STATUS_OK) {
        return status;
    }

    info->abi_version = PWR_ABI_VERSION;
    info->struct_size = (uint32_t)sizeof(*info);
    info->major = PWR_VERSION_MAJOR;
    info->minor = PWR_VERSION_MINOR;
    info->patch = PWR_VERSION_PATCH;
    info->reserved = UINT32_C(0);
    info->product_name = "Power!";
    info->version_string = PWR_BUILD_VERSION;
    return PWR_STATUS_OK;
}

pwr_status PWR_CALL
pwr_runtime_get_capabilities_impl(pwr_capabilities *capabilities)
{
    pwr_status status;

    if (capabilities == NULL) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }

    status = pwr_validate_struct(capabilities->abi_version,
                                 capabilities->struct_size,
                                 sizeof(*capabilities));
    if (status != PWR_STATUS_OK) {
        return status;
    }

    capabilities->abi_version = PWR_ABI_VERSION;
    capabilities->struct_size = (uint32_t)sizeof(*capabilities);
    capabilities->feature_bits = PWR_CAP_CONTEXT_LIFECYCLE |
                                 PWR_CAP_GENERATION_HANDLES |
                                 PWR_CAP_THREAD_SAFE_LIFECYCLE |
                                 PWR_CAP_HEADLESS_FIXED_STEP |
                                 PWR_CAP_BATCH_SCALAR_IO |
                                 PWR_CAP_CONTROLLED_SHAFT_PROTOTYPE |
                                 PWR_CAP_COMPILED_MODELS |
                                 PWR_CAP_MODEL_CHANNEL_DISCOVERY;
    capabilities->max_contexts = PWR_MAX_CONTEXTS;
    capabilities->reserved = UINT32_C(0);
    return PWR_STATUS_OK;
}

const char *PWR_CALL pwr_status_string_impl(pwr_status status)
{
    switch (status) {
    case PWR_STATUS_OK:
        return "success";
    case PWR_STATUS_INVALID_ARGUMENT:
        return "invalid argument";
    case PWR_STATUS_UNSUPPORTED_ABI:
        return "unsupported ABI version";
    case PWR_STATUS_STRUCT_TOO_SMALL:
        return "structure is too small";
    case PWR_STATUS_INVALID_HANDLE:
        return "invalid or stale handle";
    case PWR_STATUS_CAPACITY_EXCEEDED:
        return "capacity exceeded";
    case PWR_STATUS_UNSUPPORTED:
        return "operation or option is unsupported";
    case PWR_STATUS_INTERNAL_ERROR:
        return "internal error";
    case PWR_STATUS_IN_USE:
        return "object still has live dependants";
    case PWR_STATUS_BUSY:
        return "object is busy on another thread";
    case PWR_STATUS_INVALID_TIME_STEP:
        return "time step is invalid for this model";
    case PWR_STATUS_UNKNOWN_CHANNEL:
        return "input or output channel is unknown";
    case PWR_STATUS_INVALID_MODEL:
        return "model definition is invalid";
    case PWR_STATUS_NUMERIC_ERROR:
        return "numerical solve failed";
    case PWR_STATUS_OUT_OF_MEMORY:
        return "out of memory";
    default:
        return "unknown status";
    }
}

pwr_status PWR_CALL pwr_get_api(uint32_t requested_abi_version,
                                uint32_t api_struct_size,
                                pwr_api *api)
{
    const pwr_api runtime_api = {
        PWR_ABI_VERSION,
        (uint32_t)sizeof(pwr_api),
        pwr_runtime_get_version_impl,
        pwr_runtime_get_capabilities_impl,
        pwr_context_create_impl,
        pwr_context_destroy_impl,
        pwr_status_string_impl,
        pwr_instance_create_impl,
        pwr_instance_destroy_impl,
        pwr_instance_submit_inputs_impl,
        pwr_instance_step_impl,
        pwr_instance_read_snapshot_impl,
        pwr_model_compile_impl,
        pwr_model_destroy_impl,
        pwr_model_get_info_impl,
        pwr_model_get_channels_impl,
        pwr_instance_create_from_model_impl,
    };
    const size_t minimum_v1_size =
        offsetof(pwr_api, status_string) + sizeof(runtime_api.status_string);
    size_t copy_size;

    if (api == NULL) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if (requested_abi_version != PWR_ABI_VERSION) {
        return PWR_STATUS_UNSUPPORTED_ABI;
    }
    if ((size_t)api_struct_size < minimum_v1_size) {
        return PWR_STATUS_STRUCT_TOO_SMALL;
    }

    copy_size = (size_t)api_struct_size;
    if (copy_size > sizeof(runtime_api)) {
        copy_size = sizeof(runtime_api);
    }
    (void)memcpy(api, &runtime_api, copy_size);
    return PWR_STATUS_OK;
}
