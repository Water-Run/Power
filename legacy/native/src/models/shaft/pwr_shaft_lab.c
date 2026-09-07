// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#include "pwr_shaft_lab.h"

#include <float.h>
#include <math.h>
#include <stddef.h>
#include <string.h>

enum {
    PWR_SHAFT_TASK_SENSOR = 10U,
    PWR_SHAFT_TASK_SIGNAL_DELIVERY = 20U,
    PWR_SHAFT_TASK_CONTROLLER = 30U
};

static double pwr_clamp(double value, double minimum, double maximum) {
    return fmin(fmax(value, minimum), maximum);
}

static bool pwr_is_positive_finite(double value) {
    return isfinite(value) && value > 0.0;
}

static bool pwr_shaft_config_valid(const pwr_shaft_config *config) {
    if (config == NULL || config->base_tick_ns == 0U || config->sensor_period_ticks == 0U ||
        config->control_period_ticks == 0U ||
        config->signal_delay_ticks >= PWR_SHAFT_SIGNAL_QUEUE_CAPACITY ||
        config->max_sensor_age_ticks < config->signal_delay_ticks ||
        config->controller_recovery_ticks == 0U) {
        return false;
    }

    return pwr_is_positive_finite(config->high_voltage_v) &&
           pwr_is_positive_finite(config->low_voltage_nominal_v) &&
           isfinite(config->low_voltage_internal_resistance_ohm) &&
           config->low_voltage_internal_resistance_ohm >= 0.0 &&
           pwr_is_positive_finite(config->controller_current_a) &&
           pwr_is_positive_finite(config->brownout_threshold_v) &&
           config->brownout_threshold_v < config->low_voltage_nominal_v &&
           pwr_is_positive_finite(config->max_target_speed_rad_s) &&
           pwr_is_positive_finite(config->motor_resistance_ohm) &&
           pwr_is_positive_finite(config->motor_inductance_h) &&
           pwr_is_positive_finite(config->motor_torque_constant_nm_a) &&
           pwr_is_positive_finite(config->motor_back_emf_v_s_rad) &&
           pwr_is_positive_finite(config->motor_current_limit_a) &&
           pwr_is_positive_finite(config->motor_inertia_kg_m2) &&
           isfinite(config->motor_viscous_friction_nm_s_rad) &&
           config->motor_viscous_friction_nm_s_rad >= 0.0 &&
           pwr_is_positive_finite(config->shaft_stiffness_nm_rad) &&
           isfinite(config->shaft_damping_nm_s_rad) && config->shaft_damping_nm_s_rad >= 0.0 &&
           pwr_is_positive_finite(config->load_inertia_kg_m2) &&
           isfinite(config->load_viscous_friction_nm_s_rad) &&
           config->load_viscous_friction_nm_s_rad >= 0.0 &&
           isfinite(config->controller_kp) && config->controller_kp >= 0.0 &&
           isfinite(config->controller_ki_per_s) && config->controller_ki_per_s >= 0.0 &&
           pwr_is_positive_finite(config->duty_rate_limit_per_s) &&
           pwr_is_positive_finite(config->ambient_temperature_k) &&
           pwr_is_positive_finite(config->thermal_capacity_j_k) &&
           isfinite(config->thermal_conductance_w_k) && config->thermal_conductance_w_k >= 0.0 &&
           isfinite(config->inverter_loss_fraction) && config->inverter_loss_fraction >= 0.0 &&
           config->inverter_loss_fraction < 1.0;
}

static double pwr_shaft_stored_energy(const pwr_shaft_lab *lab) {
    const pwr_shaft_config *config = &lab->config;
    const double magnetic = 0.5 * config->motor_inductance_h * lab->motor_current_a *
                            lab->motor_current_a;
    const double motor_kinetic = 0.5 * config->motor_inertia_kg_m2 * lab->motor_speed_rad_s *
                                 lab->motor_speed_rad_s;
    const double load_kinetic = 0.5 * config->load_inertia_kg_m2 * lab->load_speed_rad_s *
                                lab->load_speed_rad_s;
    const double shaft_potential = 0.5 * config->shaft_stiffness_nm_rad * lab->shaft_twist_rad *
                                   lab->shaft_twist_rad;
    const double thermal = config->thermal_capacity_j_k *
                           (lab->motor_temperature_k - config->ambient_temperature_k);
    return magnetic + motor_kinetic + load_kinetic + shaft_potential + thermal;
}

static bool pwr_shaft_sensor_task(void *user, uint64_t tick) {
    pwr_shaft_lab *lab = user;
    const pwr_shaft_input *input = &lab->current_input;
    if ((input->fault_mask & PWR_SHAFT_FAULT_SIGNAL_DROP) != 0U) {
        return true;
    }

    const uint64_t delivery_tick = tick + lab->config.signal_delay_ticks;
    const size_t slot_index = (size_t)(delivery_tick % PWR_SHAFT_SIGNAL_QUEUE_CAPACITY);
    pwr_shaft_signal_slot *slot = &lab->signal_queue[slot_index];
    if (slot->occupied) {
        lab->diagnostic_flags |= PWR_SHAFT_DIAG_SIGNAL_QUEUE_OVERFLOW;
        return false;
    }

    double sensed = lab->load_speed_rad_s;
    if ((input->fault_mask & PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS) != 0U) {
        sensed += input->speed_sensor_bias_rad_s;
    }
    *slot = (pwr_shaft_signal_slot){
        .delivery_tick = delivery_tick,
        .value = sensed,
        .valid = (input->fault_mask & PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT) == 0U,
        .occupied = true,
    };
    return true;
}

static bool pwr_shaft_signal_delivery_task(void *user, uint64_t tick) {
    pwr_shaft_lab *lab = user;
    const size_t slot_index = (size_t)(tick % PWR_SHAFT_SIGNAL_QUEUE_CAPACITY);
    pwr_shaft_signal_slot *slot = &lab->signal_queue[slot_index];
    if (!slot->occupied || slot->delivery_tick != tick) {
        return true;
    }

    lab->sensed_speed_rad_s = slot->value;
    lab->sensor_valid = slot->valid;
    lab->last_sensor_delivery_tick = tick;
    slot->occupied = false;
    return true;
}

static bool pwr_shaft_controller_task(void *user, uint64_t tick) {
    pwr_shaft_lab *lab = user;
    const pwr_shaft_config *config = &lab->config;
    const pwr_shaft_input *input = &lab->current_input;
    const double control_dt = (double)config->control_period_ticks *
                              (double)config->base_tick_ns * 1.0e-9;

    lab->low_voltage_v = config->low_voltage_nominal_v -
                         config->low_voltage_internal_resistance_ohm *
                             config->controller_current_a;
    if ((input->fault_mask & PWR_SHAFT_FAULT_LV_BROWNOUT) != 0U) {
        lab->low_voltage_v = 0.0;
    }

    if (lab->low_voltage_v < config->brownout_threshold_v) {
        lab->controller_active = false;
        lab->controller_recovery_progress_ticks = 0U;
        lab->controller_integral = 0.0;
        lab->commanded_duty = 0.0;
        lab->diagnostic_flags |= PWR_SHAFT_DIAG_CONTROLLER_BROWNOUT;
        return true;
    }

    if (!lab->controller_active) {
        lab->controller_recovery_progress_ticks += config->control_period_ticks;
        if (lab->controller_recovery_progress_ticks < config->controller_recovery_ticks) {
            lab->commanded_duty = 0.0;
            return true;
        }
        lab->controller_active = true;
        lab->controller_integral = 0.0;
    }

    const uint64_t sensor_age = lab->sensor_valid ? tick - lab->last_sensor_delivery_tick : UINT64_MAX;
    if (!lab->sensor_valid) {
        lab->diagnostic_flags |= PWR_SHAFT_DIAG_SENSOR_INVALID;
    }
    if (sensor_age > config->max_sensor_age_ticks) {
        lab->diagnostic_flags |= PWR_SHAFT_DIAG_SENSOR_STALE;
    }
    if (!lab->sensor_valid || sensor_age > config->max_sensor_age_ticks) {
        lab->controller_integral = 0.0;
        lab->commanded_duty = 0.0;
        return true;
    }

    const double target = pwr_clamp(input->target_speed_rad_s, 0.0,
                                    config->max_target_speed_rad_s);
    const double error = target - lab->sensed_speed_rad_s;
    const double candidate_integral = lab->controller_integral + error * control_dt;
    const double raw_duty = config->controller_kp * error +
                            config->controller_ki_per_s * candidate_integral;
    const double saturated_duty = pwr_clamp(raw_duty, -1.0, 1.0);
    if (saturated_duty != raw_duty) {
        lab->diagnostic_flags |= PWR_SHAFT_DIAG_DUTY_SATURATED;
    }

    const bool drives_further_into_saturation =
        (raw_duty > 1.0 && error > 0.0) || (raw_duty < -1.0 && error < 0.0);
    if (!drives_further_into_saturation) {
        lab->controller_integral = candidate_integral;
    }

    const double maximum_delta = config->duty_rate_limit_per_s * control_dt;
    const double delta = pwr_clamp(saturated_duty - lab->commanded_duty,
                                   -maximum_delta, maximum_delta);
    lab->commanded_duty = pwr_clamp(lab->commanded_duty + delta, -1.0, 1.0);
    return true;
}

void pwr_shaft_config_default(pwr_shaft_config *config) {
    if (config == NULL) {
        return;
    }
    *config = (pwr_shaft_config){
        .base_tick_ns = 100000U,
        .sensor_period_ticks = 10U,
        .control_period_ticks = 10U,
        .signal_delay_ticks = 2U,
        .max_sensor_age_ticks = 20U,
        .controller_recovery_ticks = 20U,
        .high_voltage_v = 300.0,
        .low_voltage_nominal_v = 13.5,
        .low_voltage_internal_resistance_ohm = 0.1,
        .controller_current_a = 2.0,
        .brownout_threshold_v = 9.0,
        .max_target_speed_rad_s = 600.0,
        .motor_resistance_ohm = 0.15,
        .motor_inductance_h = 0.002,
        .motor_torque_constant_nm_a = 0.30,
        .motor_back_emf_v_s_rad = 0.30,
        .motor_current_limit_a = 400.0,
        .motor_inertia_kg_m2 = 0.04,
        .motor_viscous_friction_nm_s_rad = 0.01,
        .shaft_stiffness_nm_rad = 1200.0,
        .shaft_damping_nm_s_rad = 8.0,
        .load_inertia_kg_m2 = 0.12,
        .load_viscous_friction_nm_s_rad = 0.02,
        .controller_kp = 0.003,
        .controller_ki_per_s = 0.08,
        .duty_rate_limit_per_s = 5.0,
        .ambient_temperature_k = 298.15,
        .thermal_capacity_j_k = 5000.0,
        .thermal_conductance_w_k = 40.0,
        .inverter_loss_fraction = 0.03,
    };
}

pwr_shaft_result pwr_shaft_lab_init(pwr_shaft_lab *lab, const pwr_shaft_config *config) {
    if (lab == NULL || config == NULL) {
        return PWR_SHAFT_INVALID_ARGUMENT;
    }
    if (!pwr_shaft_config_valid(config)) {
        return PWR_SHAFT_INVALID_CONFIG;
    }

    memset(lab, 0, sizeof(*lab));
    lab->config = *config;
    lab->motor_temperature_k = config->ambient_temperature_k;
    lab->low_voltage_v = config->low_voltage_nominal_v;
    lab->last_sensor_delivery_tick = 0U;

    if (pwr_scheduler_init(&lab->scheduler, config->base_tick_ns) != PWR_SCHEDULER_OK) {
        return PWR_SHAFT_SCHEDULER_ERROR;
    }

    const pwr_task_desc sensor = {
        .stable_id = PWR_SHAFT_TASK_SENSOR,
        .period_ticks = config->sensor_period_ticks,
        .phase_ticks = 0U,
        .priority = 10U,
        .callback = pwr_shaft_sensor_task,
        .user = lab,
    };
    const pwr_task_desc delivery = {
        .stable_id = PWR_SHAFT_TASK_SIGNAL_DELIVERY,
        .period_ticks = 1U,
        .phase_ticks = 0U,
        .priority = 20U,
        .callback = pwr_shaft_signal_delivery_task,
        .user = lab,
    };
    const pwr_task_desc controller = {
        .stable_id = PWR_SHAFT_TASK_CONTROLLER,
        .period_ticks = config->control_period_ticks,
        .phase_ticks = 0U,
        .priority = 30U,
        .callback = pwr_shaft_controller_task,
        .user = lab,
    };

    if (pwr_scheduler_add_task(&lab->scheduler, &sensor) != PWR_SCHEDULER_OK ||
        pwr_scheduler_add_task(&lab->scheduler, &delivery) != PWR_SCHEDULER_OK ||
        pwr_scheduler_add_task(&lab->scheduler, &controller) != PWR_SCHEDULER_OK) {
        return PWR_SHAFT_SCHEDULER_ERROR;
    }

    lab->initial_stored_energy_j = pwr_shaft_stored_energy(lab);
    return PWR_SHAFT_OK;
}

pwr_shaft_result pwr_shaft_lab_step(pwr_shaft_lab *lab, const pwr_shaft_input *input) {
    if (lab == NULL || input == NULL) {
        return PWR_SHAFT_INVALID_ARGUMENT;
    }
    if (!isfinite(input->target_speed_rad_s) || !isfinite(input->load_torque_nm) ||
        input->load_torque_nm < 0.0 || !isfinite(input->speed_sensor_bias_rad_s) ||
        !isfinite(input->actuator_stuck_duty)) {
        return PWR_SHAFT_INVALID_ARGUMENT;
    }

    lab->current_input = *input;
    if (pwr_scheduler_run_tick(&lab->scheduler) != PWR_SCHEDULER_OK) {
        return PWR_SHAFT_SCHEDULER_ERROR;
    }

    const pwr_shaft_config *config = &lab->config;
    const double dt = (double)config->base_tick_ns * 1.0e-9;
    lab->applied_duty = lab->commanded_duty;
    if ((input->fault_mask & PWR_SHAFT_FAULT_ACTUATOR_STUCK) != 0U) {
        lab->applied_duty = pwr_clamp(input->actuator_stuck_duty, -1.0, 1.0);
    }

    const double applied_voltage = lab->applied_duty * config->high_voltage_v;
    const double old_current = lab->motor_current_a;
    const double current_derivative =
        (applied_voltage - config->motor_back_emf_v_s_rad * lab->motor_speed_rad_s -
         config->motor_resistance_ohm * lab->motor_current_a) /
        config->motor_inductance_h;
    double next_current = lab->motor_current_a + current_derivative * dt;
    if (fabs(next_current) > config->motor_current_limit_a) {
        const double unclamped_magnetic = 0.5 * config->motor_inductance_h * next_current * next_current;
        next_current = pwr_clamp(next_current, -config->motor_current_limit_a,
                                 config->motor_current_limit_a);
        const double clamped_magnetic = 0.5 * config->motor_inductance_h * next_current * next_current;
        lab->limiter_adjustment_j += unclamped_magnetic - clamped_magnetic;
        lab->diagnostic_flags |= PWR_SHAFT_DIAG_CURRENT_LIMITED;
    }
    lab->motor_current_a = next_current;

    const double average_current = 0.5 * (old_current + next_current);
    const double motor_torque = config->motor_torque_constant_nm_a * average_current;
    const double shaft_torque = config->shaft_stiffness_nm_rad * lab->shaft_twist_rad +
                                config->shaft_damping_nm_s_rad *
                                    (lab->motor_speed_rad_s - lab->load_speed_rad_s);
    const double motor_alpha =
        (motor_torque - shaft_torque -
         config->motor_viscous_friction_nm_s_rad * lab->motor_speed_rad_s) /
        config->motor_inertia_kg_m2;
    const double resisting_load_torque = input->load_torque_nm * tanh(lab->load_speed_rad_s);
    const double load_alpha =
        (shaft_torque - resisting_load_torque -
         config->load_viscous_friction_nm_s_rad * lab->load_speed_rad_s) /
        config->load_inertia_kg_m2;

    lab->motor_speed_rad_s += motor_alpha * dt;
    lab->load_speed_rad_s += load_alpha * dt;
    lab->shaft_twist_rad += (lab->motor_speed_rad_s - lab->load_speed_rad_s) * dt;

    const double terminal_power_w = applied_voltage * average_current;
    const double inverter_loss_w = config->inverter_loss_fraction * fabs(terminal_power_w);
    const double copper_loss_w = config->motor_resistance_ohm * average_current * average_current;
    const double friction_loss_w =
        config->motor_viscous_friction_nm_s_rad * lab->motor_speed_rad_s *
            lab->motor_speed_rad_s +
        config->load_viscous_friction_nm_s_rad * lab->load_speed_rad_s * lab->load_speed_rad_s +
        config->shaft_damping_nm_s_rad *
            (lab->motor_speed_rad_s - lab->load_speed_rad_s) *
            (lab->motor_speed_rad_s - lab->load_speed_rad_s);
    const double heat_rejection_w = config->thermal_conductance_w_k *
                                    (lab->motor_temperature_k - config->ambient_temperature_k);
    const double thermal_derivative =
        (inverter_loss_w + copper_loss_w + friction_loss_w - heat_rejection_w) /
        config->thermal_capacity_j_k;
    lab->motor_temperature_k += thermal_derivative * dt;

    lab->electrical_input_j += (terminal_power_w + inverter_loss_w) * dt;
    lab->mechanical_output_j += resisting_load_torque * lab->load_speed_rad_s * dt;
    lab->heat_rejected_j += heat_rejection_w * dt;
    lab->control_energy_input_j += lab->low_voltage_v * config->controller_current_a * dt;

    if (!isfinite(lab->motor_speed_rad_s) || !isfinite(lab->load_speed_rad_s) ||
        !isfinite(lab->shaft_twist_rad) || !isfinite(lab->motor_current_a) ||
        !isfinite(lab->motor_temperature_k) || lab->motor_temperature_k <= 0.0) {
        lab->diagnostic_flags |= PWR_SHAFT_DIAG_NUMERIC_FAILURE;
        return PWR_SHAFT_NUMERIC_ERROR;
    }
    return PWR_SHAFT_OK;
}

static uint64_t pwr_hash_u64(uint64_t hash, uint64_t value) {
    for (unsigned int byte = 0U; byte < 8U; ++byte) {
        hash ^= (value >> (byte * 8U)) & UINT64_C(0xff);
        hash *= UINT64_C(1099511628211);
    }
    return hash;
}

static uint64_t pwr_hash_double(uint64_t hash, double value) {
    uint64_t bits = 0U;
    static_assert(sizeof(bits) == sizeof(value), "double must be 64-bit for deterministic hash");
    memcpy(&bits, &value, sizeof(bits));
    return pwr_hash_u64(hash, bits);
}

uint64_t pwr_shaft_lab_state_hash(const pwr_shaft_lab *lab) {
    if (lab == NULL) {
        return 0U;
    }
    uint64_t hash = UINT64_C(14695981039346656037);
    hash = pwr_hash_u64(hash, lab->scheduler.current_tick);
    hash = pwr_hash_double(hash, lab->motor_speed_rad_s);
    hash = pwr_hash_double(hash, lab->load_speed_rad_s);
    hash = pwr_hash_double(hash, lab->shaft_twist_rad);
    hash = pwr_hash_double(hash, lab->motor_current_a);
    hash = pwr_hash_double(hash, lab->motor_temperature_k);
    hash = pwr_hash_double(hash, lab->sensed_speed_rad_s);
    hash = pwr_hash_double(hash, lab->controller_integral);
    hash = pwr_hash_double(hash, lab->commanded_duty);
    hash = pwr_hash_double(hash, lab->applied_duty);
    hash = pwr_hash_u64(hash, lab->diagnostic_flags);
    hash = pwr_hash_u64(hash, lab->sensor_valid ? 1U : 0U);
    hash = pwr_hash_u64(hash, lab->controller_active ? 1U : 0U);
    return hash;
}

pwr_shaft_result pwr_shaft_lab_snapshot(const pwr_shaft_lab *lab, pwr_shaft_snapshot *snapshot) {
    if (lab == NULL || snapshot == NULL) {
        return PWR_SHAFT_INVALID_ARGUMENT;
    }

    const double stored_change = pwr_shaft_stored_energy(lab) - lab->initial_stored_energy_j;
    const double residual = lab->electrical_input_j - lab->mechanical_output_j -
                            lab->heat_rejected_j - lab->limiter_adjustment_j - stored_change;
    const uint64_t tick = lab->scheduler.current_tick;
    const uint64_t age = lab->sensor_valid ? tick - lab->last_sensor_delivery_tick : UINT64_MAX;
    *snapshot = (pwr_shaft_snapshot){
        .time_s = pwr_scheduler_time_seconds(&lab->scheduler),
        .motor_speed_rad_s = lab->motor_speed_rad_s,
        .load_speed_rad_s = lab->load_speed_rad_s,
        .shaft_twist_rad = lab->shaft_twist_rad,
        .motor_current_a = lab->motor_current_a,
        .commanded_duty = lab->commanded_duty,
        .applied_duty = lab->applied_duty,
        .sensed_speed_rad_s = lab->sensed_speed_rad_s,
        .low_voltage_v = lab->low_voltage_v,
        .motor_temperature_k = lab->motor_temperature_k,
        .electrical_input_j = lab->electrical_input_j,
        .mechanical_output_j = lab->mechanical_output_j,
        .heat_rejected_j = lab->heat_rejected_j,
        .stored_energy_change_j = stored_change,
        .numerical_adjustment_j = lab->limiter_adjustment_j,
        .energy_residual_j = residual,
        .control_energy_input_j = lab->control_energy_input_j,
        .sensor_age_ticks = age,
        .diagnostic_flags = lab->diagnostic_flags,
        .sensor_valid = lab->sensor_valid,
        .controller_active = lab->controller_active,
        .state_hash = pwr_shaft_lab_state_hash(lab),
    };
    return PWR_SHAFT_OK;
}
