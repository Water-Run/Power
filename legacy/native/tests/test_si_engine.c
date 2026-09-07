#include "pwr_si_engine.h"

#include <math.h>
#include <stddef.h>
#include <stdint.h>
#include <stdio.h>
#include <string.h>

#define CHECK(condition)                                                       \
    do {                                                                       \
        if (!(condition)) {                                                    \
            fprintf(stderr, "%s:%d: check failed: %s\n",                    \
                    __FILE__, __LINE__, #condition);                           \
            return 0;                                                          \
        }                                                                      \
    } while (0)

static pwr_si_engine_input nominal_input(void)
{
    return (pwr_si_engine_input){
        .driver_throttle = 0.42,
        .load_torque_nm = 22.0,
        .starter_torque_nm = 0.0,
        .exhaust_backpressure_pa = 103000.0,
        .ambient_pressure_pa = 101325.0,
        .ambient_temperature_k = 298.15,
        .fault_mask = PWR_SI_ENGINE_FAULT_NONE,
        .ignition_on = true,
    };
}

static int run_started_engine(
    pwr_si_engine_state *state,
    pwr_si_engine_output *output,
    double backpressure_pa)
{
    pwr_si_engine_input input = nominal_input();

    input.exhaust_backpressure_pa = backpressure_pa;
    for (uint64_t step = UINT64_C(0); step < UINT64_C(50000); ++step) {
        input.starter_torque_nm = step < UINT64_C(8000) ? 55.0 : 0.0;
        CHECK(pwr_si_engine_step(state, &input, output) == PWR_SI_ENGINE_OK);
    }
    return 1;
}

static int test_invalid_configuration_and_transaction(void)
{
    pwr_si_engine_config config;
    pwr_si_engine_state state;
    pwr_si_engine_state before;
    pwr_si_engine_output output;
    pwr_si_engine_input input = nominal_input();

    pwr_si_engine_config_default(&config);
    config.displacement_m3 = 0.0;
    CHECK(pwr_si_engine_init(&state, &config)
          == PWR_SI_ENGINE_INVALID_CONFIG);
    pwr_si_engine_config_default(&config);
    CHECK(pwr_si_engine_init(&state, &config) == PWR_SI_ENGINE_OK);
    before = state;
    input.driver_throttle = 1.1;
    CHECK(pwr_si_engine_step(&state, &input, &output)
          == PWR_SI_ENGINE_INVALID_INPUT);
    CHECK(memcmp(&before, &state, sizeof(state)) == 0);
    CHECK(pwr_si_engine_init(NULL, &config)
          == PWR_SI_ENGINE_INVALID_ARGUMENT);
    return 1;
}

static int test_start_combustion_and_ledgers(void)
{
    pwr_si_engine_config config;
    pwr_si_engine_state state;
    pwr_si_engine_output output = {0};
    double species_sum = 0.0;

    pwr_si_engine_config_default(&config);
    CHECK(pwr_si_engine_init(&state, &config) == PWR_SI_ENGINE_OK);
    CHECK(run_started_engine(&state, &output, 103000.0));

    CHECK(output.crank_speed_rad_s > 80.0);
    CHECK(output.combustion_active);
    CHECK(output.ecu_active);
    CHECK(output.crank_signal_valid);
    CHECK(output.injected_fuel_flow_kg_per_s > 0.0);
    CHECK(output.exhaust_mass_flow_kg_per_s > 0.0);
    CHECK(output.exhaust_temperature_k >= config.exhaust_temperature_floor_k);
    CHECK(output.coolant_temperature_k > config.coolant_initial_temperature_k);
    CHECK(output.fuel_energy_j > 0.0);
    for (size_t species = 0U; species < PWR_SI_ENGINE_SPECIES_COUNT;
         ++species) {
        CHECK(output.exhaust_species_mass_fraction[species] >= 0.0);
        species_sum += output.exhaust_species_mass_fraction[species];
    }
    CHECK(fabs(species_sum - 1.0) < 1.0e-12);
    CHECK(fabs(output.air_mass_residual_kg) < 1.0e-12);
    CHECK(fabs(output.energy_residual_j)
          / (output.fuel_energy_j + output.stored_energy_change_j + 1.0)
          < 1.0e-10);
    return 1;
}

static int test_determinism(void)
{
    pwr_si_engine_config config;
    pwr_si_engine_state first;
    pwr_si_engine_state second;
    pwr_si_engine_output first_output = {0};
    pwr_si_engine_output second_output = {0};
    pwr_si_engine_input input = nominal_input();

    pwr_si_engine_config_default(&config);
    CHECK(pwr_si_engine_init(&first, &config) == PWR_SI_ENGINE_OK);
    CHECK(pwr_si_engine_init(&second, &config) == PWR_SI_ENGINE_OK);
    for (uint64_t step = UINT64_C(0); step < UINT64_C(25000); ++step) {
        input.starter_torque_nm = step < UINT64_C(8000) ? 55.0 : 0.0;
        CHECK(pwr_si_engine_step(&first, &input, &first_output)
              == PWR_SI_ENGINE_OK);
        CHECK(pwr_si_engine_step(&second, &input, &second_output)
              == PWR_SI_ENGINE_OK);
        CHECK(first_output.state_hash == second_output.state_hash);
    }
    CHECK(memcmp(&first, &second, sizeof(first)) == 0);
    return 1;
}

static int test_backpressure_reduces_speed(void)
{
    pwr_si_engine_config config;
    pwr_si_engine_state open_state;
    pwr_si_engine_state restricted_state;
    pwr_si_engine_output open_output = {0};
    pwr_si_engine_output restricted_output = {0};

    pwr_si_engine_config_default(&config);
    CHECK(pwr_si_engine_init(&open_state, &config) == PWR_SI_ENGINE_OK);
    CHECK(pwr_si_engine_init(&restricted_state, &config) == PWR_SI_ENGINE_OK);
    CHECK(run_started_engine(&open_state, &open_output, 103000.0));
    CHECK(run_started_engine(
        &restricted_state, &restricted_output, 185000.0));
    CHECK(restricted_output.pumping_torque_nm
          > open_output.pumping_torque_nm + 5.0);
    CHECK(restricted_output.crank_speed_rad_s
          + 20.0 < open_output.crank_speed_rad_s);
    return 1;
}

static int test_electronic_faults_cut_fuel(void)
{
    pwr_si_engine_config config;
    pwr_si_engine_state state;
    pwr_si_engine_output output = {0};
    pwr_si_engine_input input = nominal_input();

    pwr_si_engine_config_default(&config);
    CHECK(pwr_si_engine_init(&state, &config) == PWR_SI_ENGINE_OK);
    for (uint64_t step = UINT64_C(0); step < UINT64_C(15000); ++step) {
        input.starter_torque_nm = step < UINT64_C(8000) ? 55.0 : 0.0;
        CHECK(pwr_si_engine_step(&state, &input, &output)
              == PWR_SI_ENGINE_OK);
    }
    CHECK(output.combustion_active);

    input.starter_torque_nm = 0.0;
    input.fault_mask = PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT;
    for (uint64_t step = UINT64_C(0); step < UINT64_C(200); ++step) {
        CHECK(pwr_si_engine_step(&state, &input, &output)
              == PWR_SI_ENGINE_OK);
    }
    CHECK(!output.combustion_active);
    CHECK(!output.crank_signal_valid);
    CHECK((output.diagnostic_flags
           & PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_INVALID)
          != 0U);
    CHECK((output.diagnostic_flags & PWR_SI_ENGINE_DIAG_FUEL_CUT) != 0U);

    input.fault_mask = PWR_SI_ENGINE_FAULT_LOW_VOLTAGE;
    for (uint64_t step = UINT64_C(0); step < UINT64_C(20); ++step) {
        CHECK(pwr_si_engine_step(&state, &input, &output)
              == PWR_SI_ENGINE_OK);
    }
    CHECK(!output.ecu_active);
    CHECK(output.low_voltage_v == 0.0);
    CHECK((output.diagnostic_flags & PWR_SI_ENGINE_DIAG_ECU_BROWNOUT)
          != 0U);
    return 1;
}

int main(void)
{
    CHECK(test_invalid_configuration_and_transaction());
    CHECK(test_start_combustion_and_ledgers());
    CHECK(test_determinism());
    CHECK(test_backpressure_reduces_speed());
    CHECK(test_electronic_faults_cut_fuel());
    return 0;
}
