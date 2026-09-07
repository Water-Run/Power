#include "pwr_dct.h"

#include <assert.h>
#include <math.h>
#include <stddef.h>
#include <stdint.h>

/* Synthetic test calibration; these values are not a DQ200 data set. */
static pwr_dct_config test_config(void) {
    pwr_dct_config config;
    pwr_dct_config_default(&config);
    config.gear_ratio[(size_t)PWR_DCT_GEAR_REVERSE] = -3.20;
    config.gear_ratio[(size_t)PWR_DCT_GEAR_1] = 3.40;
    config.gear_ratio[(size_t)PWR_DCT_GEAR_2] = 2.30;
    config.gear_ratio[(size_t)PWR_DCT_GEAR_3] = 1.70;
    config.gear_ratio[(size_t)PWR_DCT_GEAR_4] = 1.30;
    config.gear_ratio[(size_t)PWR_DCT_GEAR_5] = 1.00;
    config.gear_ratio[(size_t)PWR_DCT_GEAR_6] = 0.78;
    config.gear_ratio[(size_t)PWR_DCT_GEAR_7] = 0.62;
    config.final_drive_ratio = 1.0;
    config.clamp_engagement_rate_per_s[(size_t)PWR_DCT_CLUTCH_K1] = 100.0;
    config.clamp_engagement_rate_per_s[(size_t)PWR_DCT_CLUTCH_K2] = 100.0;
    config.clamp_release_rate_per_s[(size_t)PWR_DCT_CLUTCH_K1] = 100.0;
    config.clamp_release_rate_per_s[(size_t)PWR_DCT_CLUTCH_K2] = 100.0;
    return config;
}

static pwr_dct_inputs test_inputs(pwr_dct_gear_mask gear_mask) {
    return (pwr_dct_inputs){
        .engine_speed_rad_s = 220.0,
        .output_speed_rad_s = 30.0,
        .engaged_gear_mask = gear_mask,
        .clutch_clamp_command = {0.0, 0.0},
        .ambient_temperature_k = 298.15,
    };
}

static void test_odd_even_topology_and_preselection(void) {
    assert(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_NEUTRAL) == PWR_DCT_CLUTCH_INVALID);
    assert(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_1) == PWR_DCT_CLUTCH_K1);
    assert(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_3) == PWR_DCT_CLUTCH_K1);
    assert(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_5) == PWR_DCT_CLUTCH_K1);
    assert(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_7) == PWR_DCT_CLUTCH_K1);
    assert(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_REVERSE) == PWR_DCT_CLUTCH_K2);
    assert(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_2) == PWR_DCT_CLUTCH_K2);
    assert(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_4) == PWR_DCT_CLUTCH_K2);
    assert(pwr_dct_clutch_for_gear(PWR_DCT_GEAR_6) == PWR_DCT_CLUTCH_K2);

    const pwr_dct_config config = test_config();
    pwr_dct dct;
    assert(pwr_dct_init(&dct, &config) == PWR_DCT_OK);

    pwr_dct_inputs inputs = test_inputs(PWR_DCT_GEAR_MASK_1);
    inputs.clutch_clamp_command[(size_t)PWR_DCT_CLUTCH_K1] = 1.0;
    assert(pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK);

    pwr_dct_snapshot snapshot;
    assert(pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].selected_gear == PWR_DCT_GEAR_1);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].selected_gear ==
           PWR_DCT_GEAR_NEUTRAL);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].transmitted_torque_nm > 0.0);
    assert(snapshot.output_torque_nm > 0.0);

    /* Gear 2 may be dog-engaged on the open K2 shaft while K1 carries torque. */
    inputs.engaged_gear_mask = PWR_DCT_GEAR_MASK_1 | PWR_DCT_GEAR_MASK_2;
    assert(pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK);
    assert(pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].selected_gear == PWR_DCT_GEAR_2);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].preselected);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].transmitted_torque_nm == 0.0);

    /* A negative ratio produces reverse output torque at a negative output speed. */
    pwr_dct reverse_dct;
    assert(pwr_dct_init(&reverse_dct, &config) == PWR_DCT_OK);
    inputs = test_inputs(PWR_DCT_GEAR_MASK_REVERSE);
    inputs.output_speed_rad_s = -20.0;
    inputs.clutch_clamp_command[(size_t)PWR_DCT_CLUTCH_K2] = 1.0;
    assert(pwr_dct_step(&reverse_dct, &inputs, 0.01) == PWR_DCT_OK);
    assert(pwr_dct_snapshot_read(&reverse_dct, &snapshot) == PWR_DCT_OK);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].input_shaft_speed_rad_s > 0.0);
    assert(snapshot.output_torque_nm < 0.0);
    assert(snapshot.output_power_w > 0.0);
}

static void test_overlap_shift_torque_continuity(void) {
    pwr_dct_config config = test_config();
    config.gear_ratio[(size_t)PWR_DCT_GEAR_1] = 2.00;
    config.gear_ratio[(size_t)PWR_DCT_GEAR_2] = 1.90;
    config.clamp_engagement_rate_per_s[(size_t)PWR_DCT_CLUTCH_K1] = 20.0;
    config.clamp_engagement_rate_per_s[(size_t)PWR_DCT_CLUTCH_K2] = 20.0;
    config.clamp_release_rate_per_s[(size_t)PWR_DCT_CLUTCH_K1] = 20.0;
    config.clamp_release_rate_per_s[(size_t)PWR_DCT_CLUTCH_K2] = 20.0;

    pwr_dct dct;
    assert(pwr_dct_init(&dct, &config) == PWR_DCT_OK);
    pwr_dct_inputs inputs =
        test_inputs(PWR_DCT_GEAR_MASK_1 | PWR_DCT_GEAR_MASK_2);
    inputs.engine_speed_rad_s = 250.0;
    inputs.output_speed_rad_s = 30.0;
    inputs.clutch_clamp_command[(size_t)PWR_DCT_CLUTCH_K1] = 1.0;

    for (uint32_t step = 0U; step < 10U; ++step) {
        assert(pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK);
    }
    pwr_dct_snapshot snapshot;
    assert(pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK);
    const double initial_output_torque_nm = snapshot.output_torque_nm;
    assert(initial_output_torque_nm > 0.0);

    double minimum_output_torque_nm = initial_output_torque_nm;
    double previous_output_torque_nm = initial_output_torque_nm;
    double maximum_adjacent_change_nm = 0.0;
    for (uint32_t step = 1U; step <= 20U; ++step) {
        const double progress = (double)step / 20.0;
        inputs.clutch_clamp_command[(size_t)PWR_DCT_CLUTCH_K1] = 1.0 - progress;
        inputs.clutch_clamp_command[(size_t)PWR_DCT_CLUTCH_K2] = progress;
        assert(pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK);
        assert(pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK);
        minimum_output_torque_nm = fmin(minimum_output_torque_nm, snapshot.output_torque_nm);
        maximum_adjacent_change_nm = fmax(
            maximum_adjacent_change_nm,
            fabs(snapshot.output_torque_nm - previous_output_torque_nm));
        previous_output_torque_nm = snapshot.output_torque_nm;
    }

    assert(minimum_output_torque_nm > 0.90 * initial_output_torque_nm);
    assert(maximum_adjacent_change_nm < 0.02 * initial_output_torque_nm);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].applied_clamp < 1.0e-12);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K2].applied_clamp > 0.999);
}

static void test_slip_heating_fade_and_energy_balance(void) {
    pwr_dct_config config = test_config();
    config.clutch_heat_capacity_j_per_k[(size_t)PWR_DCT_CLUTCH_K1] = 200.0;
    config.clutch_ambient_conductance_w_per_k[(size_t)PWR_DCT_CLUTCH_K1] = 0.0;
    config.initial_clutch_temperature_k[(size_t)PWR_DCT_CLUTCH_K1] = 295.0;
    config.fade_start_temperature_k[(size_t)PWR_DCT_CLUTCH_K1] = 300.0;
    config.fade_full_temperature_k[(size_t)PWR_DCT_CLUTCH_K1] = 340.0;
    config.overheat_temperature_k[(size_t)PWR_DCT_CLUTCH_K1] = 325.0;
    config.minimum_fade_capacity_fraction[(size_t)PWR_DCT_CLUTCH_K1] = 0.25;

    pwr_dct dct;
    assert(pwr_dct_init(&dct, &config) == PWR_DCT_OK);
    pwr_dct_inputs inputs = test_inputs(PWR_DCT_GEAR_MASK_1);
    inputs.engine_speed_rad_s = 300.0;
    inputs.output_speed_rad_s = 0.0;
    inputs.clutch_clamp_command[(size_t)PWR_DCT_CLUTCH_K1] = 1.0;
    inputs.ambient_temperature_k = 295.0;
    for (uint32_t step = 0U; step < 20U; ++step) {
        assert(pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK);
    }

    pwr_dct_snapshot snapshot;
    assert(pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK);
    const pwr_dct_clutch_observation *k1 =
        &snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1];
    assert(k1->cumulative_friction_work_j > 0.0);
    assert(k1->temperature_k > config.fade_start_temperature_k[(size_t)PWR_DCT_CLUTCH_K1]);
    assert(k1->thermal_capacity_factor < 1.0);
    assert(k1->thermal_capacity_factor >=
           config.minimum_fade_capacity_fraction[(size_t)PWR_DCT_CLUTCH_K1]);
    assert((snapshot.diagnostic_flags & PWR_DCT_DIAG_K1_THERMAL_FADE) != 0U);
    assert((snapshot.diagnostic_flags & PWR_DCT_DIAG_K1_OVERHEAT) != 0U);

    const double energy_scale = fabs(snapshot.cumulative_input_energy_j) + 1.0;
    assert(fabs(snapshot.energy_residual_j) / energy_scale < 1.0e-10);
    assert((snapshot.diagnostic_flags & PWR_DCT_DIAG_ENERGY_RESIDUAL) == 0U);
}

static void test_invalid_configuration_and_shift_commands(void) {
    pwr_dct_config config;
    pwr_dct_config_default(&config);
    assert(pwr_dct_validate_config(&config) == PWR_DCT_INVALID_CONFIG);

    config = test_config();
    pwr_dct_config invalid = config;
    invalid.gear_ratio[(size_t)PWR_DCT_GEAR_REVERSE] = 3.0;
    assert(pwr_dct_validate_config(&invalid) == PWR_DCT_INVALID_CONFIG);
    invalid = config;
    invalid.mesh_efficiency[(size_t)PWR_DCT_GEAR_4] = 1.01;
    assert(pwr_dct_validate_config(&invalid) == PWR_DCT_INVALID_CONFIG);
    invalid = config;
    invalid.clutch_heat_capacity_j_per_k[(size_t)PWR_DCT_CLUTCH_K2] = 0.0;
    assert(pwr_dct_validate_config(&invalid) == PWR_DCT_INVALID_CONFIG);

    pwr_dct dct;
    assert(pwr_dct_init(&dct, &config) == PWR_DCT_OK);
    pwr_dct_inputs inputs = test_inputs(PWR_DCT_GEAR_MASK_1 | PWR_DCT_GEAR_MASK_3);
    const double initial_k1_temperature_k =
        dct.clutch_temperature_k[(size_t)PWR_DCT_CLUTCH_K1];
    assert(pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_INVALID_GEAR_COMMAND);
    pwr_dct_snapshot snapshot;
    assert(pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK);
    assert(snapshot.time_s == 0.0);
    assert(snapshot.engaged_gear_mask == UINT16_C(0));
    assert(snapshot.cumulative_input_energy_j == 0.0);
    assert(snapshot.cumulative_output_energy_j == 0.0);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].temperature_k ==
           initial_k1_temperature_k);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].applied_clamp == 0.0);
    assert(snapshot.rejected_command_count == UINT64_C(1));
    assert((snapshot.last_step_diagnostic_flags &
            PWR_DCT_DIAG_MULTIPLE_GEARS_ON_SHAFT) != 0U);
    assert((snapshot.last_step_diagnostic_flags & PWR_DCT_DIAG_INPUT_REJECTED) != 0U);

    inputs = test_inputs((pwr_dct_gear_mask)(UINT16_C(1) << 8U));
    assert(pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_INVALID_GEAR_COMMAND);
    assert(pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK);
    assert((snapshot.last_step_diagnostic_flags & PWR_DCT_DIAG_INVALID_GEAR_BIT) != 0U);

    inputs = test_inputs(PWR_DCT_GEAR_MASK_1);
    inputs.clutch_clamp_command[(size_t)PWR_DCT_CLUTCH_K1] = 1.0;
    assert(pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_OK);
    const double time_before_rejected_shift_s = dct.time_s;
    const double energy_before_rejected_shift_j = dct.cumulative_input_energy_j;
    const double clamp_before_rejected_shift =
        dct.applied_clamp[(size_t)PWR_DCT_CLUTCH_K1];
    const double temperature_before_rejected_shift_k =
        dct.clutch_temperature_k[(size_t)PWR_DCT_CLUTCH_K1];
    inputs.engaged_gear_mask = PWR_DCT_GEAR_MASK_3;
    inputs.clutch_clamp_command[(size_t)PWR_DCT_CLUTCH_K1] = 0.0;
    assert(pwr_dct_step(&dct, &inputs, 0.01) == PWR_DCT_INVALID_GEAR_COMMAND);
    assert(pwr_dct_snapshot_read(&dct, &snapshot) == PWR_DCT_OK);
    assert(snapshot.time_s == time_before_rejected_shift_s);
    assert(snapshot.engaged_gear_mask == PWR_DCT_GEAR_MASK_1);
    assert(snapshot.cumulative_input_energy_j == energy_before_rejected_shift_j);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].applied_clamp ==
           clamp_before_rejected_shift);
    assert(snapshot.clutch[(size_t)PWR_DCT_CLUTCH_K1].temperature_k ==
           temperature_before_rejected_shift_k);
    assert((snapshot.last_step_diagnostic_flags &
            PWR_DCT_DIAG_DOG_SHIFT_WHILE_CLAMPED) != 0U);
}

static void test_deterministic_replay(void) {
    const pwr_dct_config config = test_config();
    pwr_dct first;
    pwr_dct second;
    assert(pwr_dct_init(&first, &config) == PWR_DCT_OK);
    assert(pwr_dct_init(&second, &config) == PWR_DCT_OK);

    pwr_dct_inputs inputs =
        test_inputs(PWR_DCT_GEAR_MASK_3 | PWR_DCT_GEAR_MASK_4);
    for (uint32_t step = 0U; step < 1000U; ++step) {
        const double phase = (double)(step % 200U) / 199.0;
        inputs.engine_speed_rad_s = 180.0 + 40.0 * sin((double)step * 0.013);
        inputs.output_speed_rad_s = 40.0 + 3.0 * cos((double)step * 0.007);
        inputs.clutch_clamp_command[(size_t)PWR_DCT_CLUTCH_K1] = 1.0 - phase;
        inputs.clutch_clamp_command[(size_t)PWR_DCT_CLUTCH_K2] = phase;
        assert(pwr_dct_step(&first, &inputs, 0.005) == PWR_DCT_OK);
        assert(pwr_dct_step(&second, &inputs, 0.005) == PWR_DCT_OK);
        assert(pwr_dct_state_hash(&first) == pwr_dct_state_hash(&second));
    }
}

int main(void) {
    test_odd_even_topology_and_preselection();
    test_overlap_shift_torque_continuity();
    test_slip_heating_fade_and_energy_balance();
    test_invalid_configuration_and_shift_commands();
    test_deterministic_replay();
    return 0;
}
