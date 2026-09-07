// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#ifndef PWR_MODELS_SHAFT_LAB_H
#define PWR_MODELS_SHAFT_LAB_H

#include "pwr_scheduler.h"

#include <stdbool.h>
#include <stdint.h>

#define PWR_SHAFT_SIGNAL_QUEUE_CAPACITY 64U

typedef enum pwr_shaft_result {
    PWR_SHAFT_OK = 0,
    PWR_SHAFT_INVALID_ARGUMENT,
    PWR_SHAFT_INVALID_CONFIG,
    PWR_SHAFT_SCHEDULER_ERROR,
    PWR_SHAFT_NUMERIC_ERROR
} pwr_shaft_result;

typedef enum pwr_shaft_fault {
    PWR_SHAFT_FAULT_NONE = 0U,
    PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS = 1U << 0U,
    PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT = 1U << 1U,
    PWR_SHAFT_FAULT_SIGNAL_DROP = 1U << 2U,
    PWR_SHAFT_FAULT_ACTUATOR_STUCK = 1U << 3U,
    PWR_SHAFT_FAULT_LV_BROWNOUT = 1U << 4U
} pwr_shaft_fault;

typedef enum pwr_shaft_diagnostic {
    PWR_SHAFT_DIAG_NONE = 0U,
    PWR_SHAFT_DIAG_SENSOR_INVALID = 1U << 0U,
    PWR_SHAFT_DIAG_SENSOR_STALE = 1U << 1U,
    PWR_SHAFT_DIAG_SIGNAL_QUEUE_OVERFLOW = 1U << 2U,
    PWR_SHAFT_DIAG_CONTROLLER_BROWNOUT = 1U << 3U,
    PWR_SHAFT_DIAG_DUTY_SATURATED = 1U << 4U,
    PWR_SHAFT_DIAG_CURRENT_LIMITED = 1U << 5U,
    PWR_SHAFT_DIAG_NUMERIC_FAILURE = 1U << 6U
} pwr_shaft_diagnostic;

typedef struct pwr_shaft_config {
    uint64_t base_tick_ns;
    uint32_t sensor_period_ticks;
    uint32_t control_period_ticks;
    uint32_t signal_delay_ticks;
    uint32_t max_sensor_age_ticks;
    uint32_t controller_recovery_ticks;

    double high_voltage_v;
    double low_voltage_nominal_v;
    double low_voltage_internal_resistance_ohm;
    double controller_current_a;
    double brownout_threshold_v;

    double max_target_speed_rad_s;
    double motor_resistance_ohm;
    double motor_inductance_h;
    double motor_torque_constant_nm_a;
    double motor_back_emf_v_s_rad;
    double motor_current_limit_a;
    double motor_inertia_kg_m2;
    double motor_viscous_friction_nm_s_rad;

    double shaft_stiffness_nm_rad;
    double shaft_damping_nm_s_rad;
    double load_inertia_kg_m2;
    double load_viscous_friction_nm_s_rad;

    double controller_kp;
    double controller_ki_per_s;
    double duty_rate_limit_per_s;

    double ambient_temperature_k;
    double thermal_capacity_j_k;
    double thermal_conductance_w_k;
    double inverter_loss_fraction;
} pwr_shaft_config;

typedef struct pwr_shaft_input {
    double target_speed_rad_s;
    double load_torque_nm;
    uint32_t fault_mask;
    double speed_sensor_bias_rad_s;
    double actuator_stuck_duty;
} pwr_shaft_input;

typedef struct pwr_shaft_signal_slot {
    uint64_t delivery_tick;
    double value;
    bool valid;
    bool occupied;
} pwr_shaft_signal_slot;

typedef struct pwr_shaft_snapshot {
    double time_s;
    double motor_speed_rad_s;
    double load_speed_rad_s;
    double shaft_twist_rad;
    double motor_current_a;
    double commanded_duty;
    double applied_duty;
    double sensed_speed_rad_s;
    double low_voltage_v;
    double motor_temperature_k;
    double electrical_input_j;
    double mechanical_output_j;
    double heat_rejected_j;
    double stored_energy_change_j;
    double numerical_adjustment_j;
    double energy_residual_j;
    double control_energy_input_j;
    uint64_t sensor_age_ticks;
    uint32_t diagnostic_flags;
    bool sensor_valid;
    bool controller_active;
    uint64_t state_hash;
} pwr_shaft_snapshot;

typedef struct pwr_shaft_lab {
    pwr_shaft_config config;
    pwr_scheduler scheduler;
    pwr_shaft_input current_input;
    pwr_shaft_signal_slot signal_queue[PWR_SHAFT_SIGNAL_QUEUE_CAPACITY];

    double motor_speed_rad_s;
    double load_speed_rad_s;
    double shaft_twist_rad;
    double motor_current_a;
    double motor_temperature_k;
    double sensed_speed_rad_s;
    double controller_integral;
    double commanded_duty;
    double applied_duty;
    double low_voltage_v;

    double initial_stored_energy_j;
    double electrical_input_j;
    double mechanical_output_j;
    double heat_rejected_j;
    double control_energy_input_j;
    double limiter_adjustment_j;

    uint64_t last_sensor_delivery_tick;
    uint64_t controller_recovery_progress_ticks;
    uint32_t diagnostic_flags;
    bool sensor_valid;
    bool controller_active;
} pwr_shaft_lab;

void pwr_shaft_config_default(pwr_shaft_config *config);
pwr_shaft_result pwr_shaft_lab_init(pwr_shaft_lab *lab, const pwr_shaft_config *config);
pwr_shaft_result pwr_shaft_lab_step(pwr_shaft_lab *lab, const pwr_shaft_input *input);
pwr_shaft_result pwr_shaft_lab_snapshot(const pwr_shaft_lab *lab, pwr_shaft_snapshot *snapshot);
uint64_t pwr_shaft_lab_state_hash(const pwr_shaft_lab *lab);

#endif
