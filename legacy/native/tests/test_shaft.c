// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#include "pwr_shaft_lab.h"

#include <assert.h>
#include <math.h>
#include <stddef.h>
#include <stdint.h>

static pwr_shaft_snapshot run_nominal(pwr_shaft_lab *lab, uint64_t steps, double target_speed,
                                      double load_torque) {
    const pwr_shaft_input input = {
        .target_speed_rad_s = target_speed,
        .load_torque_nm = load_torque,
        .fault_mask = PWR_SHAFT_FAULT_NONE,
    };
    for (uint64_t i = 0U; i < steps; ++i) {
        assert(pwr_shaft_lab_step(lab, &input) == PWR_SHAFT_OK);
    }
    pwr_shaft_snapshot snapshot;
    assert(pwr_shaft_lab_snapshot(lab, &snapshot) == PWR_SHAFT_OK);
    return snapshot;
}

static void test_nominal_and_deterministic(void) {
    pwr_shaft_config config;
    pwr_shaft_config_default(&config);

    pwr_shaft_lab first;
    pwr_shaft_lab second;
    assert(pwr_shaft_lab_init(&first, &config) == PWR_SHAFT_OK);
    assert(pwr_shaft_lab_init(&second, &config) == PWR_SHAFT_OK);

    const pwr_shaft_input input = {
        .target_speed_rad_s = 160.0,
        .load_torque_nm = 20.0,
    };
    for (uint64_t i = 0U; i < 30000U; ++i) {
        assert(pwr_shaft_lab_step(&first, &input) == PWR_SHAFT_OK);
        assert(pwr_shaft_lab_step(&second, &input) == PWR_SHAFT_OK);
        assert(pwr_shaft_lab_state_hash(&first) == pwr_shaft_lab_state_hash(&second));
    }

    pwr_shaft_snapshot snapshot;
    assert(pwr_shaft_lab_snapshot(&first, &snapshot) == PWR_SHAFT_OK);
    assert(snapshot.controller_active);
    assert(snapshot.sensor_valid);
    assert(snapshot.load_speed_rad_s > 120.0);
    assert(snapshot.load_speed_rad_s < 200.0);
    assert(snapshot.mechanical_output_j > 0.0);
    assert(snapshot.motor_temperature_k >= config.ambient_temperature_k);

    const double transfer_scale = fabs(snapshot.electrical_input_j) + 1.0;
    assert(fabs(snapshot.energy_residual_j) / transfer_scale < 0.03);
}

static void test_faults_close_the_loop(void) {
    pwr_shaft_config config;
    pwr_shaft_config_default(&config);

    pwr_shaft_lab nominal;
    pwr_shaft_lab biased;
    assert(pwr_shaft_lab_init(&nominal, &config) == PWR_SHAFT_OK);
    assert(pwr_shaft_lab_init(&biased, &config) == PWR_SHAFT_OK);

    const pwr_shaft_input nominal_input = {
        .target_speed_rad_s = 160.0,
        .load_torque_nm = 20.0,
    };
    pwr_shaft_input biased_input = nominal_input;
    biased_input.fault_mask = PWR_SHAFT_FAULT_SPEED_SENSOR_BIAS;
    biased_input.speed_sensor_bias_rad_s = 45.0;

    for (uint64_t i = 0U; i < 25000U; ++i) {
        assert(pwr_shaft_lab_step(&nominal, &nominal_input) == PWR_SHAFT_OK);
        assert(pwr_shaft_lab_step(&biased, &biased_input) == PWR_SHAFT_OK);
    }

    pwr_shaft_snapshot nominal_snapshot;
    pwr_shaft_snapshot biased_snapshot;
    assert(pwr_shaft_lab_snapshot(&nominal, &nominal_snapshot) == PWR_SHAFT_OK);
    assert(pwr_shaft_lab_snapshot(&biased, &biased_snapshot) == PWR_SHAFT_OK);
    assert(biased_snapshot.load_speed_rad_s + 25.0 < nominal_snapshot.load_speed_rad_s);

    pwr_shaft_input dropout_input = nominal_input;
    dropout_input.fault_mask = PWR_SHAFT_FAULT_SPEED_SENSOR_DROPOUT;
    for (uint64_t i = 0U; i < 100U; ++i) {
        assert(pwr_shaft_lab_step(&biased, &dropout_input) == PWR_SHAFT_OK);
    }
    assert(pwr_shaft_lab_snapshot(&biased, &biased_snapshot) == PWR_SHAFT_OK);
    assert(!biased_snapshot.sensor_valid);
    assert(fabs(biased_snapshot.commanded_duty) < 1.0e-12);
    assert((biased_snapshot.diagnostic_flags & PWR_SHAFT_DIAG_SENSOR_INVALID) != 0U);

    pwr_shaft_input brownout_input = nominal_input;
    brownout_input.fault_mask = PWR_SHAFT_FAULT_LV_BROWNOUT;
    for (uint64_t i = 0U; i < config.control_period_ticks + 1U; ++i) {
        assert(pwr_shaft_lab_step(&nominal, &brownout_input) == PWR_SHAFT_OK);
    }
    assert(pwr_shaft_lab_snapshot(&nominal, &nominal_snapshot) == PWR_SHAFT_OK);
    assert(!nominal_snapshot.controller_active);
    assert(fabs(nominal_snapshot.commanded_duty) < 1.0e-12);
    assert((nominal_snapshot.diagnostic_flags & PWR_SHAFT_DIAG_CONTROLLER_BROWNOUT) != 0U);
}

static void test_step_convergence(void) {
    pwr_shaft_config coarse_config;
    pwr_shaft_config_default(&coarse_config);

    pwr_shaft_config fine_config = coarse_config;
    fine_config.base_tick_ns /= 2U;
    fine_config.sensor_period_ticks *= 2U;
    fine_config.control_period_ticks *= 2U;
    fine_config.signal_delay_ticks *= 2U;
    fine_config.max_sensor_age_ticks *= 2U;
    fine_config.controller_recovery_ticks *= 2U;

    pwr_shaft_lab coarse;
    pwr_shaft_lab fine;
    assert(pwr_shaft_lab_init(&coarse, &coarse_config) == PWR_SHAFT_OK);
    assert(pwr_shaft_lab_init(&fine, &fine_config) == PWR_SHAFT_OK);

    const pwr_shaft_snapshot coarse_snapshot = run_nominal(&coarse, 20000U, 140.0, 15.0);
    const pwr_shaft_snapshot fine_snapshot = run_nominal(&fine, 40000U, 140.0, 15.0);
    const double relative_speed_difference =
        fabs(coarse_snapshot.load_speed_rad_s - fine_snapshot.load_speed_rad_s) /
        fmax(fabs(fine_snapshot.load_speed_rad_s), 1.0);
    assert(relative_speed_difference < 0.02);
}

static void test_invalid_configuration(void) {
    pwr_shaft_config config;
    pwr_shaft_config_default(&config);
    config.motor_inertia_kg_m2 = 0.0;
    pwr_shaft_lab lab;
    assert(pwr_shaft_lab_init(&lab, &config) == PWR_SHAFT_INVALID_CONFIG);
    assert(pwr_shaft_lab_init(NULL, &config) == PWR_SHAFT_INVALID_ARGUMENT);
}

int main(void) {
    test_invalid_configuration();
    test_nominal_and_deterministic();
    test_faults_close_the_loop();
    test_step_convergence();
    return 0;
}
