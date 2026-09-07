#include "pwr_dct_powertrain.h"

#include <math.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

#define CHECK(condition)                                                       \
    do {                                                                       \
        if (!(condition)) {                                                    \
            fprintf(stderr, "%s:%d: check failed: %s\n",                  \
                    __FILE__, __LINE__, #condition);                           \
            return 0;                                                          \
        }                                                                      \
    } while (0)

/* Synthetic integration calibration.  This is explicitly not DQ200 data. */
static pwr_dct_powertrain_config test_config(void)
{
    pwr_dct_powertrain_config config;

    pwr_dct_powertrain_config_default(&config);
    config.dct.gear_ratio[(size_t)PWR_DCT_GEAR_REVERSE] = -3.10;
    config.dct.gear_ratio[(size_t)PWR_DCT_GEAR_1] = 3.20;
    config.dct.gear_ratio[(size_t)PWR_DCT_GEAR_2] = 2.05;
    config.dct.gear_ratio[(size_t)PWR_DCT_GEAR_3] = 1.55;
    config.dct.gear_ratio[(size_t)PWR_DCT_GEAR_4] = 1.20;
    config.dct.gear_ratio[(size_t)PWR_DCT_GEAR_5] = 0.96;
    config.dct.gear_ratio[(size_t)PWR_DCT_GEAR_6] = 0.76;
    config.dct.gear_ratio[(size_t)PWR_DCT_GEAR_7] = 0.61;
    config.dct.final_drive_ratio = 1.0;
    config.dct.clutch_nominal_capacity_nm[(size_t)PWR_DCT_CLUTCH_K1]
        = 180.0;
    config.dct.clutch_nominal_capacity_nm[(size_t)PWR_DCT_CLUTCH_K2]
        = 180.0;
    config.launch_complete_halfshaft_speed_rad_s = 4.0;
    config.first_to_second_shift_halfshaft_speed_rad_s = 13.0;
    config.minimum_first_gear_time_s = 0.12;
    config.first_to_second_shift_duration_s = 0.16;
    return config;
}

static pwr_dct_powertrain_input supervised_input(double tailpipe_scale)
{
    return (pwr_dct_powertrain_input){
        .control_mode = PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED,
        .driver_throttle = 0.52,
        .ambient_pressure_pa = 101325.0,
        .ambient_temperature_k = 298.15,
        .additional_road_load_torque_nm = 2.0,
        .tailpipe_area_scale = tailpipe_scale,
        .engine_fault_mask = PWR_SI_ENGINE_FAULT_NONE,
        .ignition_on = true,
        .selector_drive = true,
    };
}

static int test_default_requires_ratios_and_rejection_is_transactional(void)
{
    pwr_dct_powertrain_config config;
    pwr_dct_powertrain_state state;
    pwr_dct_powertrain_state before;
    pwr_dct_powertrain_output output;
    pwr_dct_powertrain_input input = supervised_input(1.0);

    pwr_dct_powertrain_config_default(&config);
    CHECK(pwr_dct_powertrain_validate_config(&config)
          == PWR_DCT_POWERTRAIN_INVALID_CONFIG);

    config = test_config();
    config.engine.base_tick_ns = UINT64_C(1000000);
    CHECK(pwr_dct_powertrain_validate_config(&config)
          == PWR_DCT_POWERTRAIN_INVALID_CONFIG);
    config = test_config();
    CHECK(pwr_dct_powertrain_init(&state, &config)
          == PWR_DCT_POWERTRAIN_OK);
    before = state;
    input.driver_throttle = 1.01;
    CHECK(pwr_dct_powertrain_step(&state, &input, &output)
          == PWR_DCT_POWERTRAIN_INVALID_INPUT);
    CHECK(memcmp(&state, &before, sizeof(state)) == 0);

    input = supervised_input(1.0);
    input.control_mode = PWR_DCT_POWERTRAIN_CONTROL_DIRECT;
    input.direct_starter_torque_nm = 0.0;
    input.direct_gear_mask
        = PWR_DCT_GEAR_MASK_1 | PWR_DCT_GEAR_MASK_3;
    before = state;
    CHECK(pwr_dct_powertrain_step(&state, &input, &output)
          == PWR_DCT_POWERTRAIN_SUBMODEL_ERROR);
    CHECK(memcmp(&state, &before, sizeof(state)) == 0);
    return 1;
}

static int test_cold_start_launch_and_first_to_second_shift(void)
{
    const pwr_dct_powertrain_config config = test_config();
    pwr_dct_powertrain_state state;
    pwr_dct_powertrain_output output = {0};
    pwr_dct_powertrain_input input = supervised_input(1.0);
    bool saw_starter = false;
    bool saw_combustion = false;
    bool saw_preselection = false;
    bool saw_launch = false;
    bool saw_first = false;
    bool saw_shift = false;
    bool saw_second = false;
    double minimum_shift_drive_torque = INFINITY;

    CHECK(pwr_dct_powertrain_init(&state, &config)
          == PWR_DCT_POWERTRAIN_OK);
    for (uint64_t step = UINT64_C(0); step < UINT64_C(80000); ++step) {
        CHECK(pwr_dct_powertrain_step(&state, &input, &output)
              == PWR_DCT_POWERTRAIN_OK);
        saw_starter = saw_starter || output.commanded_starter_torque_nm > 0.0;
        saw_combustion = saw_combustion || output.engine.combustion_active;
        saw_preselection = saw_preselection
            || output.tcu_phase == PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2;
        saw_launch = saw_launch
            || output.tcu_phase == PWR_DCT_POWERTRAIN_TCU_LAUNCH_1;
        saw_first = saw_first
            || output.tcu_phase == PWR_DCT_POWERTRAIN_TCU_GEAR_1;
        if (output.tcu_phase == PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2) {
            saw_shift = true;
            minimum_shift_drive_torque = fmin(
                minimum_shift_drive_torque, output.halfshaft_drive_torque_nm);
        }
        saw_second = saw_second
            || output.tcu_phase == PWR_DCT_POWERTRAIN_TCU_GEAR_2;
    }

    CHECK(saw_starter);
    CHECK(saw_combustion);
    CHECK(saw_preselection);
    CHECK(saw_launch);
    CHECK(saw_first);
    CHECK(saw_shift);
    CHECK(saw_second);
    CHECK(output.tcu_phase == PWR_DCT_POWERTRAIN_TCU_GEAR_2);
    CHECK(output.engine.crank_speed_rad_s > 50.0);
    CHECK(output.halfshaft_speed_rad_s > 13.0);
    CHECK(output.halfshaft_angle_rad > 0.0);
    CHECK(output.cumulative_halfshaft_drive_work_j > 0.0);
    CHECK(output.dct.clutch[(size_t)PWR_DCT_CLUTCH_K2].applied_clamp
          > 0.90);
    CHECK(output.commanded_gear_mask
          == (PWR_DCT_GEAR_MASK_1 | PWR_DCT_GEAR_MASK_2));
    CHECK(minimum_shift_drive_torque > -5.0);
    CHECK(fabs(output.halfshaft_energy_residual_j) < 1.0e-8);
    CHECK(output.exhaust.pressure_pa[PWR_EXHAUST_VOLUME_MANIFOLD] > 0.0);
    CHECK(isfinite(output.cumulative_engine_dct_energy_mismatch_j));
    CHECK(isfinite(output.cumulative_exhaust_mass_mismatch_kg));
    return 1;
}

static int run_restriction_case(
    double tailpipe_scale,
    pwr_dct_powertrain_output *final_output,
    double *average_brake_power_w)
{
    const pwr_dct_powertrain_config config = test_config();
    pwr_dct_powertrain_state state;
    pwr_dct_powertrain_input input = supervised_input(tailpipe_scale);
    pwr_dct_powertrain_output output = {0};
    double brake_energy_j = 0.0;

    input.additional_road_load_torque_nm = 8.0;
    CHECK(pwr_dct_powertrain_init(&state, &config)
          == PWR_DCT_POWERTRAIN_OK);
    for (uint64_t step = UINT64_C(0); step < UINT64_C(70000); ++step) {
        CHECK(pwr_dct_powertrain_step(&state, &input, &output)
              == PWR_DCT_POWERTRAIN_OK);
        if (step >= UINT64_C(60000)) {
            brake_energy_j += output.engine.brake_torque_nm
                * output.engine.crank_speed_rad_s * 1.0e-4;
        }
    }
    *average_brake_power_w = brake_energy_j;
    *final_output = output;
    return 1;
}

static int test_tailpipe_restriction_feeds_back_to_engine(void)
{
    pwr_dct_powertrain_output open_output;
    pwr_dct_powertrain_output restricted_output;
    double open_average_brake_power_w;
    double restricted_average_brake_power_w;

    CHECK(run_restriction_case(
        1.0, &open_output, &open_average_brake_power_w));
    CHECK(run_restriction_case(
        0.025, &restricted_output, &restricted_average_brake_power_w));
    CHECK(restricted_output.exhaust.pressure_pa[
              PWR_EXHAUST_VOLUME_MANIFOLD]
          > open_output.exhaust.pressure_pa[PWR_EXHAUST_VOLUME_MANIFOLD]
              + 1000.0);
    CHECK(restricted_output.engine.pumping_torque_nm
          > open_output.engine.pumping_torque_nm + 0.5);
    CHECK(restricted_average_brake_power_w
          < open_average_brake_power_w);
    return 1;
}

static int test_deterministic_replay(void)
{
    const pwr_dct_powertrain_config config = test_config();
    pwr_dct_powertrain_state first;
    pwr_dct_powertrain_state second;
    pwr_dct_powertrain_output first_output = {0};
    pwr_dct_powertrain_output second_output = {0};
    pwr_dct_powertrain_input input = supervised_input(1.0);

    CHECK(pwr_dct_powertrain_init(&first, &config)
          == PWR_DCT_POWERTRAIN_OK);
    CHECK(pwr_dct_powertrain_init(&second, &config)
          == PWR_DCT_POWERTRAIN_OK);
    for (uint64_t step = UINT64_C(0); step < UINT64_C(30000); ++step) {
        input.driver_throttle = 0.45
            + 0.08 * sin((double)step * 0.0007);
        CHECK(pwr_dct_powertrain_step(&first, &input, &first_output)
              == PWR_DCT_POWERTRAIN_OK);
        CHECK(pwr_dct_powertrain_step(&second, &input, &second_output)
              == PWR_DCT_POWERTRAIN_OK);
        CHECK(first_output.state_hash == second_output.state_hash);
    }
    CHECK(memcmp(&first, &second, sizeof(first)) == 0);
    return 1;
}

int main(void)
{
    CHECK(test_default_requires_ratios_and_rejection_is_transactional());
    CHECK(test_cold_start_launch_and_first_to_second_shift());
    CHECK(test_tailpipe_restriction_feeds_back_to_engine());
    CHECK(test_deterministic_replay());
    return 0;
}
