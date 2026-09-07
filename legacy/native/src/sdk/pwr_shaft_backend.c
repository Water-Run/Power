#include "pwr_internal.h"
#include "pwr_shaft_lab.h"

#include <math.h>
#include <stddef.h>
#include <stdlib.h>

enum { PWR_SHAFT_SNAPSHOT_VALUE_COUNT = 17 };

typedef struct pwr_shaft_runtime {
    pwr_shaft_lab shaft;
    pwr_shaft_input input;
} pwr_shaft_runtime;

pwr_status pwr_shaft_backend_create(void **state)
{
    pwr_shaft_runtime *runtime = calloc(1, sizeof(*runtime));
    if (runtime == NULL) { return PWR_STATUS_OUT_OF_MEMORY; }
    pwr_shaft_config config;
    pwr_shaft_config_default(&config);
    if (pwr_shaft_lab_init(&runtime->shaft, &config) != PWR_SHAFT_OK) {
        free(runtime);
        return PWR_STATUS_INTERNAL_ERROR;
    }
    *state = runtime;
    return PWR_STATUS_OK;
}

static bool pwr_scalar_is_boolean(double value)
{
    return value == 0.0 || value == 1.0;
}

static void pwr_set_fault(uint32_t *fault_mask, uint32_t bit, bool enabled)
{
    if (enabled) {
        *fault_mask |= bit;
    }
    else {
        *fault_mask &= ~bit;
    }
}

static pwr_status pwr_apply_input_value(pwr_shaft_input *input,
                                        const pwr_scalar_value *value)
{
    if (!isfinite(value->value)) {
        return PWR_STATUS_INVALID_ARGUMENT;
    }
    if ((value->flags != UINT32_C(0)) || (value->reserved != UINT32_C(0))) {
        return PWR_STATUS_UNSUPPORTED;
    }

    switch (value->channel) {
    case PWR_INPUT_TARGET_SPEED_RAD_S:
        if (value->value < 0.0) {
            return PWR_STATUS_INVALID_ARGUMENT;
        }
        input->target_speed_rad_s = value->value;
        return PWR_STATUS_OK;
    case PWR_INPUT_LOAD_TORQUE_NM:
        if (value->value < 0.0) {
            return PWR_STATUS_INVALID_ARGUMENT;
        }
        input->load_torque_nm = value->value;
        return PWR_STATUS_OK;
    case PWR_INPUT_SPEED_SENSOR_BIAS_RAD_S:
        input->speed_sensor_bias_rad_s = value->value;
        pwr_set_fault(&input->fault_mask, PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS,
                      value->value != 0.0);
        return PWR_STATUS_OK;
    case PWR_INPUT_SPEED_SENSOR_DROPOUT:
        if (!pwr_scalar_is_boolean(value->value)) {
            return PWR_STATUS_INVALID_ARGUMENT;
        }
        pwr_set_fault(&input->fault_mask, PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT,
                      value->value != 0.0);
        return PWR_STATUS_OK;
    case PWR_INPUT_SIGNAL_DROP:
        if (!pwr_scalar_is_boolean(value->value)) {
            return PWR_STATUS_INVALID_ARGUMENT;
        }
        pwr_set_fault(&input->fault_mask, PWR_SHAFT_FAULT_SIGNAL_DROP,
                      value->value != 0.0);
        return PWR_STATUS_OK;
    case PWR_INPUT_ACTUATOR_STUCK_ENABLE:
        if (!pwr_scalar_is_boolean(value->value)) {
            return PWR_STATUS_INVALID_ARGUMENT;
        }
        pwr_set_fault(&input->fault_mask, PWR_SHAFT_FAULT_ACTUATOR_STUCK,
                      value->value != 0.0);
        return PWR_STATUS_OK;
    case PWR_INPUT_ACTUATOR_STUCK_DUTY:
        if ((value->value < -1.0) || (value->value > 1.0)) {
            return PWR_STATUS_INVALID_ARGUMENT;
        }
        input->actuator_stuck_duty = value->value;
        return PWR_STATUS_OK;
    case PWR_INPUT_LOW_VOLTAGE_BROWNOUT:
        if (!pwr_scalar_is_boolean(value->value)) {
            return PWR_STATUS_INVALID_ARGUMENT;
        }
        pwr_set_fault(&input->fault_mask, PWR_SHAFT_FAULT_LV_BROWNOUT,
                      value->value != 0.0);
        return PWR_STATUS_OK;
    default:
        return PWR_STATUS_UNKNOWN_CHANNEL;
    }
}

static pwr_status pwr_shaft_submit(void *opaque, const pwr_input_frame *frame)
{
    pwr_shaft_runtime *runtime = opaque;
    pwr_shaft_input candidate = runtime->input;
    for (uint32_t index = 0; index < frame->value_count; ++index) {
        const pwr_status status = pwr_apply_input_value(&candidate, &frame->values[index]);
        if (status != PWR_STATUS_OK) { return status; }
    }
    runtime->input = candidate;
    return PWR_STATUS_OK;
}

static pwr_status pwr_shaft_step(void *opaque, uint64_t delta_ns)
{
    pwr_shaft_runtime *runtime = opaque;
    const uint64_t tick_ns = runtime->shaft.config.base_tick_ns;
    const uint64_t ticks = delta_ns / tick_ns;
    if (delta_ns == 0U || delta_ns % tick_ns != 0U || ticks > UINT64_C(1000000) ||
        runtime->shaft.scheduler.current_tick > UINT64_MAX / tick_ns ||
        UINT64_MAX - runtime->shaft.scheduler.current_tick * tick_ns < delta_ns) {
        return PWR_STATUS_INVALID_TIME_STEP;
    }
    for (uint64_t i = 0; i < ticks; ++i) {
        if (pwr_shaft_lab_step(&runtime->shaft, &runtime->input) != PWR_SHAFT_OK) {
            return PWR_STATUS_NUMERIC_ERROR;
        }
    }
    return PWR_STATUS_OK;
}

static void pwr_write_snapshot_value(pwr_scalar_value *value,
                                     pwr_channel_id channel,
                                     double scalar)
{
    *value = (pwr_scalar_value){
        .channel = channel,
        .value = scalar,
        .flags = UINT32_C(0),
        .reserved = UINT32_C(0),
    };
}

static pwr_status pwr_shaft_read_snapshot(const void *opaque, pwr_snapshot *snapshot)
{
    const pwr_shaft_runtime *runtime = opaque;
    pwr_shaft_snapshot shaft;
    if (pwr_shaft_lab_snapshot(&runtime->shaft, &shaft) != PWR_SHAFT_OK) {
        return PWR_STATUS_INTERNAL_ERROR;
    }

    snapshot->simulation_time_ns = runtime->shaft.scheduler.current_tick *
                                   runtime->shaft.config.base_tick_ns;
    snapshot->state_hash = shaft.state_hash;
    snapshot->diagnostic_bits = shaft.diagnostic_flags;
    snapshot->value_count = PWR_SHAFT_SNAPSHOT_VALUE_COUNT;
    if ((snapshot->values == NULL) ||
        (snapshot->value_capacity < PWR_SHAFT_SNAPSHOT_VALUE_COUNT)) {
        return PWR_STATUS_CAPACITY_EXCEEDED;
    }

    pwr_write_snapshot_value(&snapshot->values[0], PWR_OUTPUT_MOTOR_SPEED_RAD_S,
                             shaft.motor_speed_rad_s);
    pwr_write_snapshot_value(&snapshot->values[1], PWR_OUTPUT_LOAD_SPEED_RAD_S,
                             shaft.load_speed_rad_s);
    pwr_write_snapshot_value(&snapshot->values[2], PWR_OUTPUT_SHAFT_TWIST_RAD,
                             shaft.shaft_twist_rad);
    pwr_write_snapshot_value(&snapshot->values[3], PWR_OUTPUT_MOTOR_CURRENT_A,
                             shaft.motor_current_a);
    pwr_write_snapshot_value(&snapshot->values[4], PWR_OUTPUT_COMMANDED_DUTY,
                             shaft.commanded_duty);
    pwr_write_snapshot_value(&snapshot->values[5], PWR_OUTPUT_APPLIED_DUTY,
                             shaft.applied_duty);
    pwr_write_snapshot_value(&snapshot->values[6], PWR_OUTPUT_SENSED_SPEED_RAD_S,
                             shaft.sensed_speed_rad_s);
    pwr_write_snapshot_value(&snapshot->values[7], PWR_OUTPUT_LOW_VOLTAGE_V,
                             shaft.low_voltage_v);
    pwr_write_snapshot_value(&snapshot->values[8], PWR_OUTPUT_MOTOR_TEMPERATURE_K,
                             shaft.motor_temperature_k);
    pwr_write_snapshot_value(&snapshot->values[9], PWR_OUTPUT_ELECTRICAL_INPUT_J,
                             shaft.electrical_input_j);
    pwr_write_snapshot_value(&snapshot->values[10], PWR_OUTPUT_MECHANICAL_OUTPUT_J,
                             shaft.mechanical_output_j);
    pwr_write_snapshot_value(&snapshot->values[11], PWR_OUTPUT_HEAT_REJECTED_J,
                             shaft.heat_rejected_j);
    pwr_write_snapshot_value(&snapshot->values[12],
                             PWR_OUTPUT_STORED_ENERGY_CHANGE_J,
                             shaft.stored_energy_change_j);
    pwr_write_snapshot_value(&snapshot->values[13],
                             PWR_OUTPUT_NUMERICAL_ADJUSTMENT_J,
                             shaft.numerical_adjustment_j);
    pwr_write_snapshot_value(&snapshot->values[14], PWR_OUTPUT_ENERGY_RESIDUAL_J,
                             shaft.energy_residual_j);
    pwr_write_snapshot_value(
        &snapshot->values[15], PWR_OUTPUT_SENSOR_AGE_TICKS,
        shaft.sensor_age_ticks == UINT64_MAX ? -1.0 : (double)shaft.sensor_age_ticks);
    pwr_write_snapshot_value(&snapshot->values[16], PWR_OUTPUT_CONTROLLER_ACTIVE,
                             shaft.controller_active ? 1.0 : 0.0);

    return PWR_STATUS_OK;
}

const pwr_instance_backend pwr_shaft_backend = {
    pwr_shaft_submit, pwr_shaft_step, pwr_shaft_read_snapshot, free
};
