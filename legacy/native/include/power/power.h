#ifndef POWER_POWER_H
#define POWER_POWER_H

#include <stdint.h>
#include <power/model.h>

#if defined(__cplusplus)
extern "C" {
#endif

#if defined(_WIN32)
#define PWR_CALL __cdecl
#if defined(PWR_BUILDING_LIBRARY)
#define PWR_PUBLIC __declspec(dllexport)
#else
#define PWR_PUBLIC __declspec(dllimport)
#endif
#elif defined(__GNUC__) || defined(__clang__)
#define PWR_CALL
#define PWR_PUBLIC __attribute__((visibility("default")))
#else
#define PWR_CALL
#define PWR_PUBLIC
#endif

#define PWR_VERSION_MAJOR UINT32_C(0)
#define PWR_VERSION_MINOR UINT32_C(2)
#define PWR_VERSION_PATCH UINT32_C(0)

typedef int32_t pwr_status;

enum {
    PWR_STATUS_OK = 0,
    PWR_STATUS_INVALID_ARGUMENT = -1,
    PWR_STATUS_UNSUPPORTED_ABI = -2,
    PWR_STATUS_STRUCT_TOO_SMALL = -3,
    PWR_STATUS_INVALID_HANDLE = -4,
    PWR_STATUS_CAPACITY_EXCEEDED = -5,
    PWR_STATUS_UNSUPPORTED = -6,
    PWR_STATUS_INTERNAL_ERROR = -7,
    PWR_STATUS_IN_USE = -8,
    PWR_STATUS_BUSY = -9,
    PWR_STATUS_INVALID_TIME_STEP = -10,
    PWR_STATUS_UNKNOWN_CHANNEL = -11,
    PWR_STATUS_INVALID_MODEL = -12,
    PWR_STATUS_NUMERIC_ERROR = -13,
    PWR_STATUS_OUT_OF_MEMORY = -14
};

typedef uint64_t pwr_context;
typedef uint64_t pwr_instance;

#define PWR_CONTEXT_INVALID UINT64_C(0)
#define PWR_INSTANCE_INVALID UINT64_C(0)

#define PWR_CAP_CONTEXT_LIFECYCLE (UINT64_C(1) << 0)
#define PWR_CAP_GENERATION_HANDLES (UINT64_C(1) << 1)
#define PWR_CAP_THREAD_SAFE_LIFECYCLE (UINT64_C(1) << 2)
#define PWR_CAP_HEADLESS_FIXED_STEP (UINT64_C(1) << 3)
#define PWR_CAP_BATCH_SCALAR_IO (UINT64_C(1) << 4)
#define PWR_CAP_CONTROLLED_SHAFT_PROTOTYPE (UINT64_C(1) << 5)
#define PWR_CAP_COMPILED_MODELS (UINT64_C(1) << 6)
#define PWR_CAP_MODEL_CHANNEL_DISCOVERY (UINT64_C(1) << 7)

typedef struct pwr_context_desc {
    uint32_t abi_version;
    uint32_t struct_size;
    uint32_t flags;
    uint32_t reserved;
} pwr_context_desc;

#define PWR_CONTEXT_DESC_INIT                                                    \
    {                                                                           \
        PWR_ABI_VERSION, (uint32_t)sizeof(pwr_context_desc), UINT32_C(0),        \
            UINT32_C(0)                                                         \
    }

typedef struct pwr_version_info {
    uint32_t abi_version;
    uint32_t struct_size;
    uint32_t major;
    uint32_t minor;
    uint32_t patch;
    uint32_t reserved;
    const char *product_name;
    const char *version_string;
} pwr_version_info;

#define PWR_VERSION_INFO_INIT                                                   \
    {                                                                           \
        PWR_ABI_VERSION, (uint32_t)sizeof(pwr_version_info), UINT32_C(0),        \
            UINT32_C(0), UINT32_C(0), UINT32_C(0), (const char *)0,             \
            (const char *)0                                                     \
    }

typedef struct pwr_capabilities {
    uint32_t abi_version;
    uint32_t struct_size;
    uint64_t feature_bits;
    uint32_t max_contexts;
    uint32_t reserved;
} pwr_capabilities;

#define PWR_CAPABILITIES_INIT                                                   \
    {                                                                           \
        PWR_ABI_VERSION, (uint32_t)sizeof(pwr_capabilities), UINT64_C(0),        \
            UINT32_C(0), UINT32_C(0)                                            \
    }

typedef enum pwr_builtin_model {
    PWR_BUILTIN_MODEL_CONTROLLED_SHAFT = 1
} pwr_builtin_model;

typedef struct pwr_instance_desc {
    uint32_t abi_version;
    uint32_t struct_size;
    uint32_t model;
    uint32_t flags;
    uint64_t seed;
} pwr_instance_desc;

#define PWR_INSTANCE_DESC_INIT                                                  \
    {                                                                           \
        PWR_ABI_VERSION, (uint32_t)sizeof(pwr_instance_desc),                   \
            PWR_BUILTIN_MODEL_CONTROLLED_SHAFT, UINT32_C(0), UINT64_C(0)       \
    }

typedef uint64_t pwr_channel_id;

#define PWR_INPUT_TARGET_SPEED_RAD_S UINT64_C(1)
#define PWR_INPUT_LOAD_TORQUE_NM UINT64_C(2)
#define PWR_INPUT_SPEED_SENSOR_BIAS_RAD_S UINT64_C(3)
#define PWR_INPUT_SPEED_SENSOR_DROPOUT UINT64_C(4)
#define PWR_INPUT_SIGNAL_DROP UINT64_C(5)
#define PWR_INPUT_ACTUATOR_STUCK_ENABLE UINT64_C(6)
#define PWR_INPUT_ACTUATOR_STUCK_DUTY UINT64_C(7)
#define PWR_INPUT_LOW_VOLTAGE_BROWNOUT UINT64_C(8)

#define PWR_OUTPUT_MOTOR_SPEED_RAD_S UINT64_C(1001)
#define PWR_OUTPUT_LOAD_SPEED_RAD_S UINT64_C(1002)
#define PWR_OUTPUT_SHAFT_TWIST_RAD UINT64_C(1003)
#define PWR_OUTPUT_MOTOR_CURRENT_A UINT64_C(1004)
#define PWR_OUTPUT_COMMANDED_DUTY UINT64_C(1005)
#define PWR_OUTPUT_APPLIED_DUTY UINT64_C(1006)
#define PWR_OUTPUT_SENSED_SPEED_RAD_S UINT64_C(1007)
#define PWR_OUTPUT_LOW_VOLTAGE_V UINT64_C(1008)
#define PWR_OUTPUT_MOTOR_TEMPERATURE_K UINT64_C(1009)
#define PWR_OUTPUT_ELECTRICAL_INPUT_J UINT64_C(1010)
#define PWR_OUTPUT_MECHANICAL_OUTPUT_J UINT64_C(1011)
#define PWR_OUTPUT_HEAT_REJECTED_J UINT64_C(1012)
#define PWR_OUTPUT_STORED_ENERGY_CHANGE_J UINT64_C(1013)
#define PWR_OUTPUT_NUMERICAL_ADJUSTMENT_J UINT64_C(1014)
#define PWR_OUTPUT_ENERGY_RESIDUAL_J UINT64_C(1015)
#define PWR_OUTPUT_SENSOR_AGE_TICKS UINT64_C(1016)
#define PWR_OUTPUT_CONTROLLER_ACTIVE UINT64_C(1017)

typedef struct pwr_scalar_value {
    pwr_channel_id channel;
    double value;
    uint32_t flags;
    uint32_t reserved;
} pwr_scalar_value;

typedef struct pwr_input_frame {
    uint32_t abi_version;
    uint32_t struct_size;
    const pwr_scalar_value *values;
    uint32_t value_count;
    uint32_t flags;
} pwr_input_frame;

#define PWR_INPUT_FRAME_INIT                                                    \
    {                                                                           \
        PWR_ABI_VERSION, (uint32_t)sizeof(pwr_input_frame),                    \
            (const pwr_scalar_value *)0, UINT32_C(0), UINT32_C(0)              \
    }

typedef struct pwr_snapshot {
    uint32_t abi_version;
    uint32_t struct_size;
    uint64_t simulation_time_ns;
    uint64_t state_hash;
    uint64_t diagnostic_bits;
    pwr_scalar_value *values;
    uint32_t value_capacity;
    uint32_t value_count;
} pwr_snapshot;

#define PWR_SNAPSHOT_INIT                                                       \
    {                                                                           \
        PWR_ABI_VERSION, (uint32_t)sizeof(pwr_snapshot), UINT64_C(0),           \
            UINT64_C(0), UINT64_C(0), (pwr_scalar_value *)0, UINT32_C(0),       \
            UINT32_C(0)                                                         \
    }

typedef pwr_status(PWR_CALL *pwr_runtime_get_version_fn)(pwr_version_info *info);
typedef pwr_status(PWR_CALL *pwr_runtime_get_capabilities_fn)(
    pwr_capabilities *capabilities);
typedef pwr_status(PWR_CALL *pwr_context_create_fn)(const pwr_context_desc *desc,
                                                     pwr_context *context);
typedef pwr_status(PWR_CALL *pwr_context_destroy_fn)(pwr_context context);
typedef const char *(PWR_CALL *pwr_status_string_fn)(pwr_status status);
typedef pwr_status(PWR_CALL *pwr_instance_create_fn)(
    pwr_context context, const pwr_instance_desc *desc, pwr_instance *instance);
typedef pwr_status(PWR_CALL *pwr_instance_destroy_fn)(pwr_instance instance);
typedef pwr_status(PWR_CALL *pwr_instance_submit_inputs_fn)(
    pwr_instance instance, const pwr_input_frame *frame);
typedef pwr_status(PWR_CALL *pwr_instance_step_fn)(pwr_instance instance,
                                                   uint64_t delta_ns);
typedef pwr_status(PWR_CALL *pwr_instance_read_snapshot_fn)(
    pwr_instance instance, pwr_snapshot *snapshot);

/* Model descriptors are borrowed only during compilation. Models own their
 * normalized configuration and keep their context alive. Model destruction
 * returns IN_USE while instances exist. Fingerprints are reproducibility
 * identifiers, not cryptographic signatures or proof of calibration. */
typedef pwr_status(PWR_CALL *pwr_model_compile_fn)(
    pwr_context context, const pwr_model_desc *desc, pwr_model *model,
    pwr_model_diagnostic *diagnostic);
typedef pwr_status(PWR_CALL *pwr_model_destroy_fn)(pwr_model model);
typedef pwr_status(PWR_CALL *pwr_model_get_info_fn)(pwr_model model,
                                                 pwr_model_info *info);
/* count is always set on a valid model. NULL/zero queries required capacity.
 * Insufficient capacity leaves the channel buffer untouched. */
typedef pwr_status(PWR_CALL *pwr_model_get_channels_fn)(
    pwr_model model, pwr_channel_info *channels, uint32_t capacity, uint32_t *count);
typedef pwr_status(PWR_CALL *pwr_instance_create_from_model_fn)(
    pwr_model model, pwr_instance *instance);

typedef struct pwr_api {
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
    /* Append-only ABI v1 extension, introduced in 0.2.0. */
    pwr_model_compile_fn model_compile;
    pwr_model_destroy_fn model_destroy;
    pwr_model_get_info_fn model_get_info;
    pwr_model_get_channels_fn model_get_channels;
    pwr_instance_create_from_model_fn instance_create_from_model;
} pwr_api;

/*
 * This is the sole exported symbol. api_struct_size is the caller's capacity;
 * successful calls report the runtime's known prefix in api->struct_size.
 */
PWR_PUBLIC pwr_status PWR_CALL pwr_get_api(uint32_t requested_abi_version,
                                           uint32_t api_struct_size,
                                           pwr_api *api);

#if defined(__cplusplus)
}
#endif

#endif
