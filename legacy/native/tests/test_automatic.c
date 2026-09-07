#include "pwr_automatic.h"

#include <assert.h>
#include <math.h>
#include <stddef.h>
#include <stdint.h>
#include <string.h>

static pwr_automatic_input drive_input(pwr_automatic_gear gear, double engine_torque_nm,
                                       double lockup_command,
                                       const pwr_automatic_config *config) {
    return (pwr_automatic_input){
        .engine_torque_nm = engine_torque_nm,
        .output_external_torque_nm = 0.0,
        .line_pressure_command_pa = config->nominal_line_pressure_pa,
        .lockup_command = lockup_command,
        .requested_gear = gear,
    };
}

static pwr_automatic_coupled_input coupled_input(pwr_automatic_gear gear,
                                                 double pump_speed_rad_s,
                                                 const pwr_automatic_config *config) {
    return (pwr_automatic_coupled_input){
        .pump_speed_rad_s = pump_speed_rad_s,
        .output_external_torque_nm = 0.0,
        .line_pressure_command_pa = config->nominal_line_pressure_pa,
        .lockup_command = 0.0,
        .requested_gear = gear,
    };
}

static void run_steps(pwr_automatic *automatic, const pwr_automatic_input *input,
                      uint64_t step_count) {
    for (uint64_t step = 0U; step < step_count; ++step) {
        assert(pwr_automatic_step(automatic, input) == PWR_AUTOMATIC_OK);
    }
}

static void test_invalid_configuration_and_transactional_failure(void) {
    pwr_automatic_config config;
    pwr_automatic_config_default(&config);

    pwr_automatic untouched;
    memset(&untouched, 0xA5, sizeof(untouched));
    pwr_automatic original = untouched;
    config.forward_ratios[1] = config.forward_ratios[0];
    assert(pwr_automatic_init(&untouched, &config) == PWR_AUTOMATIC_INVALID_CONFIG);
    assert(memcmp(&untouched, &original, sizeof(untouched)) == 0);
    assert(pwr_automatic_init(NULL, &config) == PWR_AUTOMATIC_INVALID_ARGUMENT);

    pwr_automatic_config_default(&config);
    pwr_automatic automatic;
    assert(pwr_automatic_init(&automatic, &config) == PWR_AUTOMATIC_OK);
    const pwr_automatic_input valid =
        drive_input(PWR_AUTOMATIC_GEAR_FIRST, 80.0, 0.0, &config);
    run_steps(&automatic, &valid, 100U);
    const uint64_t before_hash = pwr_automatic_state_hash(&automatic);

    pwr_automatic_input invalid = valid;
    invalid.line_pressure_command_pa = config.maximum_line_pressure_pa + 1.0;
    assert(pwr_automatic_step(&automatic, &invalid) == PWR_AUTOMATIC_INVALID_INPUT);
    assert(pwr_automatic_state_hash(&automatic) == before_hash);
    invalid = valid;
    invalid.requested_gear = (pwr_automatic_gear)5;
    assert(pwr_automatic_step(&automatic, &invalid) == PWR_AUTOMATIC_INVALID_INPUT);
    assert(pwr_automatic_state_hash(&automatic) == before_hash);

    double ratio = 123.0;
    assert(pwr_automatic_gear_ratio(&config, (pwr_automatic_gear)5, &ratio) ==
           PWR_AUTOMATIC_INVALID_INPUT);
    assert(ratio == 123.0);
}

static void test_converter_stall_and_coupling_trends(void) {
    pwr_automatic_config config;
    pwr_automatic_config_default(&config);

    pwr_automatic_converter_point stall;
    pwr_automatic_converter_point mid;
    pwr_automatic_converter_point coupled;
    pwr_automatic_converter_point equal_speed;
    assert(pwr_automatic_converter_evaluate(&config, 220.0, 0.0, &stall) ==
           PWR_AUTOMATIC_OK);
    assert(pwr_automatic_converter_evaluate(&config, 220.0, 110.0, &mid) ==
           PWR_AUTOMATIC_OK);
    assert(pwr_automatic_converter_evaluate(&config, 220.0, 198.0, &coupled) ==
           PWR_AUTOMATIC_OK);
    assert(pwr_automatic_converter_evaluate(&config, 220.0, 220.0, &equal_speed) ==
           PWR_AUTOMATIC_OK);

    assert(fabs(stall.speed_ratio) < 1.0e-12);
    assert(fabs(stall.torque_ratio - config.converter_stall_torque_ratio) < 1.0e-12);
    assert(stall.turbine_torque_nm > stall.pump_torque_nm);
    assert(stall.slip_power_w > 0.0);
    assert(mid.torque_ratio < stall.torque_ratio);
    assert(mid.torque_ratio > coupled.torque_ratio);
    assert(coupled.torque_ratio >= 1.0);
    assert(coupled.pump_torque_nm < stall.pump_torque_nm);
    assert(fabs(equal_speed.pump_torque_nm) < 1.0e-12);
    assert(fabs(equal_speed.turbine_torque_nm) < 1.0e-12);
    assert(fabs(equal_speed.slip_power_w) < 1.0e-12);

    pwr_automatic_converter_point sentinel = {
        .speed_ratio = 7.0,
        .torque_ratio = 8.0,
        .pump_torque_nm = 9.0,
        .turbine_torque_nm = 10.0,
        .slip_power_w = 11.0,
    };
    const pwr_automatic_converter_point before = sentinel;
    assert(pwr_automatic_converter_evaluate(&config, NAN, 0.0, &sentinel) ==
           PWR_AUTOMATIC_INVALID_INPUT);
    assert(memcmp(&sentinel, &before, sizeof(sentinel)) == 0);
}

static void test_deterministic_fixed_step(void) {
    pwr_automatic_config config;
    pwr_automatic_config_default(&config);
    pwr_automatic first;
    pwr_automatic second;
    assert(pwr_automatic_init(&first, &config) == PWR_AUTOMATIC_OK);
    assert(pwr_automatic_init(&second, &config) == PWR_AUTOMATIC_OK);

    pwr_automatic_input input =
        drive_input(PWR_AUTOMATIC_GEAR_FIRST, 95.0, 0.0, &config);
    for (uint64_t step = 0U; step < 15000U; ++step) {
        assert(pwr_automatic_step(&first, &input) == PWR_AUTOMATIC_OK);
        assert(pwr_automatic_step(&second, &input) == PWR_AUTOMATIC_OK);
        assert(pwr_automatic_state_hash(&first) == pwr_automatic_state_hash(&second));
    }

    input.requested_gear = PWR_AUTOMATIC_GEAR_SECOND;
    input.lockup_command = 0.6;
    for (uint64_t step = 0U; step < 10000U; ++step) {
        assert(pwr_automatic_step(&first, &input) == PWR_AUTOMATIC_OK);
        assert(pwr_automatic_step(&second, &input) == PWR_AUTOMATIC_OK);
        assert(pwr_automatic_state_hash(&first) == pwr_automatic_state_hash(&second));
    }

    pwr_automatic_snapshot snapshot;
    assert(pwr_automatic_snapshot_read(&first, &snapshot) == PWR_AUTOMATIC_OK);
    assert(fabs(snapshot.time_s - 2.5) < 1.0e-12);
    assert(snapshot.active_gear == PWR_AUTOMATIC_GEAR_SECOND);
    assert(!snapshot.shift_active);
    assert(snapshot.pump_speed_rad_s > 0.0);
    assert(snapshot.output_speed_rad_s > 0.0);
    assert(snapshot.accumulated_loss_j > 0.0);
}

static void test_lockup_reduces_converter_slip(void) {
    pwr_automatic_config config;
    pwr_automatic_config_default(&config);
    config.output_viscous_drag_nm_s_rad = 0.80;

    pwr_automatic baseline;
    assert(pwr_automatic_init(&baseline, &config) == PWR_AUTOMATIC_OK);
    pwr_automatic_input input =
        drive_input(PWR_AUTOMATIC_GEAR_FOURTH, 90.0, 0.0, &config);
    run_steps(&baseline, &input, 18000U);

    pwr_automatic open_converter = baseline;
    pwr_automatic locked_converter = baseline;
    pwr_automatic_input open_input = input;
    pwr_automatic_input locked_input = input;
    locked_input.lockup_command = 1.0;
    run_steps(&open_converter, &open_input, 18000U);
    run_steps(&locked_converter, &locked_input, 18000U);

    pwr_automatic_snapshot open_snapshot;
    pwr_automatic_snapshot locked_snapshot;
    assert(pwr_automatic_snapshot_read(&open_converter, &open_snapshot) == PWR_AUTOMATIC_OK);
    assert(pwr_automatic_snapshot_read(&locked_converter, &locked_snapshot) ==
           PWR_AUTOMATIC_OK);
    assert(locked_snapshot.lockup_engagement > 0.95);
    assert(fabs(locked_snapshot.lockup_slip_rad_s) + 10.0 <
           fabs(open_snapshot.lockup_slip_rad_s));
    assert(fabs(locked_snapshot.lockup_slip_rad_s) < 5.0);
}

static void test_one_to_two_shift_is_continuous(void) {
    pwr_automatic_config config;
    pwr_automatic_config_default(&config);
    config.output_viscous_drag_nm_s_rad = 0.60;

    pwr_automatic automatic;
    assert(pwr_automatic_init(&automatic, &config) == PWR_AUTOMATIC_OK);
    pwr_automatic_input input =
        drive_input(PWR_AUTOMATIC_GEAR_FIRST, 85.0, 0.0, &config);
    run_steps(&automatic, &input, 16000U);

    pwr_automatic_snapshot before;
    assert(pwr_automatic_snapshot_read(&automatic, &before) == PWR_AUTOMATIC_OK);
    assert(before.active_gear == PWR_AUTOMATIC_GEAR_FIRST);
    assert(!before.shift_active);

    input.requested_gear = PWR_AUTOMATIC_GEAR_SECOND;
    assert(pwr_automatic_step(&automatic, &input) == PWR_AUTOMATIC_OK);
    pwr_automatic_snapshot first_shift_step;
    assert(pwr_automatic_snapshot_read(&automatic, &first_shift_step) == PWR_AUTOMATIC_OK);
    assert(first_shift_step.shift_active);
    assert(first_shift_step.effective_ratio <= before.effective_ratio);
    assert(fabs(first_shift_step.gear_output_torque_nm - before.gear_output_torque_nm) < 1.0);

    double previous_ratio = first_shift_step.effective_ratio;
    double previous_torque = first_shift_step.gear_output_torque_nm;
    double maximum_torque_step = 0.0;
    for (uint64_t step = 0U; step < 8000U; ++step) {
        assert(pwr_automatic_step(&automatic, &input) == PWR_AUTOMATIC_OK);
        pwr_automatic_snapshot current;
        assert(pwr_automatic_snapshot_read(&automatic, &current) == PWR_AUTOMATIC_OK);
        assert(current.effective_ratio <= previous_ratio + 1.0e-12);
        maximum_torque_step =
            fmax(maximum_torque_step, fabs(current.gear_output_torque_nm - previous_torque));
        previous_ratio = current.effective_ratio;
        previous_torque = current.gear_output_torque_nm;
    }

    pwr_automatic_snapshot after;
    assert(pwr_automatic_snapshot_read(&automatic, &after) == PWR_AUTOMATIC_OK);
    assert(after.active_gear == PWR_AUTOMATIC_GEAR_SECOND);
    assert(!after.shift_active);
    assert(fabs(after.effective_ratio - config.forward_ratios[1]) < 1.0e-12);
    assert(maximum_torque_step < 5.0);
}

static void test_oil_heating_and_pressure_diagnostics(void) {
    pwr_automatic_config config;
    pwr_automatic_config_default(&config);
    config.initial_oil_temperature_k = config.ambient_temperature_k;
    config.initial_lockup_temperature_k = config.ambient_temperature_k;
    config.oil_cooling_conductance_w_k = 0.0;

    pwr_automatic hot;
    assert(pwr_automatic_init(&hot, &config) == PWR_AUTOMATIC_OK);
    const pwr_automatic_input drive =
        drive_input(PWR_AUTOMATIC_GEAR_FIRST, 110.0, 0.0, &config);
    run_steps(&hot, &drive, 30000U);
    pwr_automatic_snapshot hot_snapshot;
    assert(pwr_automatic_snapshot_read(&hot, &hot_snapshot) == PWR_AUTOMATIC_OK);
    assert(hot_snapshot.oil_temperature_k > config.ambient_temperature_k + 0.02);
    assert(hot_snapshot.accumulated_loss_j > 100.0);

    pwr_automatic low_pressure;
    assert(pwr_automatic_init(&low_pressure, &config) == PWR_AUTOMATIC_OK);
    pwr_automatic_input starved = drive;
    starved.line_pressure_command_pa = 0.0;
    run_steps(&low_pressure, &starved, 16000U);
    pwr_automatic_snapshot diagnostic_snapshot;
    assert(pwr_automatic_snapshot_read(&low_pressure, &diagnostic_snapshot) ==
           PWR_AUTOMATIC_OK);
    assert((diagnostic_snapshot.diagnostic_flags & PWR_AUTOMATIC_DIAG_LOW_LINE_PRESSURE) != 0U);
    assert((diagnostic_snapshot.diagnostic_flags & PWR_AUTOMATIC_DIAG_SHIFT_TIMEOUT) != 0U);
}

static void test_coupled_boundary_reaction_trends(void) {
    pwr_automatic_config config;
    pwr_automatic_config_default(&config);
    pwr_automatic stall;
    pwr_automatic near_coupling;
    assert(pwr_automatic_init(&stall, &config) == PWR_AUTOMATIC_OK);
    assert(pwr_automatic_init(&near_coupling, &config) == PWR_AUTOMATIC_OK);
    near_coupling.turbine_speed_rad_s = 219.0;

    pwr_automatic_coupled_input input =
        coupled_input(PWR_AUTOMATIC_GEAR_NEUTRAL, 220.0, &config);
    input.line_pressure_command_pa = 0.0;
    pwr_automatic_coupling_output stall_output;
    pwr_automatic_coupling_output coupled_output;
    assert(pwr_automatic_step_coupled(&stall, &input, &stall_output) == PWR_AUTOMATIC_OK);
    assert(pwr_automatic_step_coupled(&near_coupling, &input, &coupled_output) ==
           PWR_AUTOMATIC_OK);

    assert(stall.pump_speed_rad_s == input.pump_speed_rad_s);
    assert(near_coupling.pump_speed_rad_s == input.pump_speed_rad_s);
    assert(stall_output.pump_load_torque_nm > coupled_output.pump_load_torque_nm + 80.0);
    assert(stall_output.engine_reaction_torque_nm < coupled_output.engine_reaction_torque_nm);
    assert(fabs(stall_output.engine_reaction_torque_nm + stall_output.pump_load_torque_nm) <
           1.0e-12);
    assert(fabs(stall_output.boundary_power_w -
                stall_output.pump_load_torque_nm * input.pump_speed_rad_s) <
           1.0e-9);

    const double expected_projection =
        0.5 * config.pump_inertia_kg_m2 * input.pump_speed_rad_s * input.pump_speed_rad_s;
    assert(fabs(stall_output.projection_adjustment_j - expected_projection) < 1.0e-12);
    assert(stall_output.boundary_energy_j > 0.0);
    assert((stall.diagnostic_flags & PWR_AUTOMATIC_DIAG_COUPLING_SPEED_PROJECTED) != 0U);

    pwr_automatic_coupling_output repeated_output;
    assert(pwr_automatic_step_coupled(&stall, &input, &repeated_output) == PWR_AUTOMATIC_OK);
    assert(stall.pump_speed_rad_s == input.pump_speed_rad_s);
    assert(repeated_output.projection_adjustment_j == 0.0);
    assert(repeated_output.boundary_energy_j > stall_output.boundary_energy_j);
}

static void test_coupled_boundary_deterministic_and_transactional(void) {
    pwr_automatic_config config;
    pwr_automatic_config_default(&config);
    pwr_automatic first;
    pwr_automatic second;
    assert(pwr_automatic_init(&first, &config) == PWR_AUTOMATIC_OK);
    assert(pwr_automatic_init(&second, &config) == PWR_AUTOMATIC_OK);

    for (uint64_t step = 0U; step < 12000U; ++step) {
        const double commanded_speed = 160.0 + (double)(step % 1000U) * 0.02;
        pwr_automatic_coupled_input input =
            coupled_input(PWR_AUTOMATIC_GEAR_FIRST, commanded_speed, &config);
        if (step >= 7000U) {
            input.requested_gear = PWR_AUTOMATIC_GEAR_SECOND;
            input.lockup_command = 0.7;
        }
        pwr_automatic_coupling_output first_output;
        pwr_automatic_coupling_output second_output;
        assert(pwr_automatic_step_coupled(&first, &input, &first_output) ==
               PWR_AUTOMATIC_OK);
        assert(pwr_automatic_step_coupled(&second, &input, &second_output) ==
               PWR_AUTOMATIC_OK);
        assert(first.pump_speed_rad_s == commanded_speed);
        assert(second.pump_speed_rad_s == commanded_speed);
        assert(first_output.engine_reaction_torque_nm ==
               second_output.engine_reaction_torque_nm);
        assert(first_output.boundary_energy_j == second_output.boundary_energy_j);
        assert(pwr_automatic_state_hash(&first) == pwr_automatic_state_hash(&second));
    }

    pwr_automatic_snapshot snapshot;
    assert(pwr_automatic_snapshot_read(&first, &snapshot) == PWR_AUTOMATIC_OK);
    assert(snapshot.pump_speed_imposed);
    assert(snapshot.active_gear == PWR_AUTOMATIC_GEAR_SECOND);
    assert(snapshot.turbine_speed_rad_s > 0.0);
    assert(snapshot.output_speed_rad_s > 0.0);
    assert(snapshot.line_pressure_pa > config.minimum_drive_pressure_pa);
    assert(snapshot.lockup_engagement > 0.0);
    assert(snapshot.pump_boundary_energy_j > 0.0);
    assert(isfinite(snapshot.coupling_adjustment_j));

    const uint64_t before_hash = pwr_automatic_state_hash(&first);
    pwr_automatic_coupled_input invalid =
        coupled_input(PWR_AUTOMATIC_GEAR_SECOND, config.maximum_shaft_speed_rad_s + 1.0,
                      &config);
    pwr_automatic_coupling_output untouched = {
        .engine_reaction_torque_nm = 1.0,
        .pump_load_torque_nm = 2.0,
        .boundary_power_w = 3.0,
        .boundary_energy_j = 4.0,
        .projection_adjustment_j = 5.0,
        .accumulated_projection_adjustment_j = 6.0,
    };
    const pwr_automatic_coupling_output original = untouched;
    assert(pwr_automatic_step_coupled(&first, &invalid, &untouched) ==
           PWR_AUTOMATIC_INVALID_INPUT);
    assert(pwr_automatic_state_hash(&first) == before_hash);
    assert(memcmp(&untouched, &original, sizeof(untouched)) == 0);

    invalid.pump_speed_rad_s = 180.0;
    invalid.lockup_command = NAN;
    assert(pwr_automatic_step_coupled(&first, &invalid, &untouched) ==
           PWR_AUTOMATIC_INVALID_INPUT);
    assert(pwr_automatic_state_hash(&first) == before_hash);
    assert(memcmp(&untouched, &original, sizeof(untouched)) == 0);
}

int main(void) {
    test_invalid_configuration_and_transactional_failure();
    test_converter_stall_and_coupling_trends();
    test_deterministic_fixed_step();
    test_lockup_reduces_converter_slip();
    test_one_to_two_shift_is_continuous();
    test_oil_heating_and_pressure_diagnostics();
    test_coupled_boundary_reaction_trends();
    test_coupled_boundary_deterministic_and_transactional();
    return 0;
}
