// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#ifndef PWR_MODELS_ENGINE_PWR_SI_ENGINE_H
#define PWR_MODELS_ENGINE_PWR_SI_ENGINE_H

/*
 * Internal mean-value spark-ignition engine building block.
 *
 * The defaults are deliberately generic laboratory parameters.  Vehicle
 * samples must replace them with sourced, variant-specific data before they
 * can claim calibrated behaviour.
 */

#include <stdbool.h>
#include <stdint.h>

enum {
    PWR_SI_ENGINE_SPECIES_INERT = 0,
    PWR_SI_ENGINE_SPECIES_OXYGEN,
    PWR_SI_ENGINE_SPECIES_REDUCTANT,
    PWR_SI_ENGINE_SPECIES_PRODUCTS,
    PWR_SI_ENGINE_SPECIES_COUNT
};

typedef enum pwr_si_engine_result {
    PWR_SI_ENGINE_OK = 0,
    PWR_SI_ENGINE_INVALID_ARGUMENT,
    PWR_SI_ENGINE_INVALID_CONFIG,
    PWR_SI_ENGINE_INVALID_INPUT,
    PWR_SI_ENGINE_NUMERIC_ERROR
} pwr_si_engine_result;

typedef enum pwr_si_engine_fault {
    PWR_SI_ENGINE_FAULT_NONE = 0U,
    PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT = 1U << 0U,
    PWR_SI_ENGINE_FAULT_THROTTLE_STUCK = 1U << 1U,
    PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED = 1U << 2U,
    PWR_SI_ENGINE_FAULT_LOW_VOLTAGE = 1U << 3U
} pwr_si_engine_fault;

typedef enum pwr_si_engine_diagnostic {
    PWR_SI_ENGINE_DIAG_NONE = 0U,
    PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_INVALID = 1U << 0U,
    PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_STALE = 1U << 1U,
    PWR_SI_ENGINE_DIAG_ECU_BROWNOUT = 1U << 2U,
    PWR_SI_ENGINE_DIAG_FUEL_CUT = 1U << 3U,
    PWR_SI_ENGINE_DIAG_THROTTLE_LIMITED = 1U << 4U,
    PWR_SI_ENGINE_DIAG_SPEED_LIMIT = 1U << 5U,
    PWR_SI_ENGINE_DIAG_MANIFOLD_MASS_LIMIT = 1U << 6U,
    PWR_SI_ENGINE_DIAG_NUMERIC_FAILURE = 1U << 7U
} pwr_si_engine_diagnostic;

typedef struct pwr_si_engine_config {
    uint64_t base_tick_ns;
    uint32_t sensor_period_ticks;
    uint32_t ecu_period_ticks;
    uint32_t maximum_sensor_age_ticks;
    uint32_t cylinder_count;

    double displacement_m3;
    double compression_ratio;
    double rotational_inertia_kg_m2;
    double intake_manifold_volume_m3;
    double throttle_effective_area_m2;
    double throttle_discharge_coefficient;
    double throttle_time_constant_s;
    double volumetric_efficiency;

    double air_gas_constant_j_per_kg_k;
    double ambient_pressure_pa;
    double ambient_temperature_k;
    double stoichiometric_air_fuel_ratio;
    double oxygen_mass_fraction;
    double oxygen_per_fuel_kg_per_kg;
    double fuel_lower_heating_value_j_per_kg;
    double indicated_efficiency;
    double exhaust_heat_fraction;
    double injector_time_constant_s;

    double friction_coulomb_nm;
    double friction_viscous_nm_s_per_rad;
    double pumping_scale;
    double combustion_min_speed_rad_s;
    double maximum_speed_rad_s;
    double combustion_torque_ripple_fraction;

    double coolant_initial_temperature_k;
    double coolant_heat_capacity_j_per_k;
    double coolant_ambient_conductance_w_per_k;
    double exhaust_temperature_floor_k;
    double exhaust_effective_heat_capacity_j_per_kg_k;
    double exhaust_port_resistance_pa_per_kg_s_squared;

    double low_voltage_nominal_v;
    double low_voltage_internal_resistance_ohm;
    double ecu_current_a;
    double injector_current_a;
    double starter_current_per_nm_a;
    double ecu_brownout_threshold_v;
} pwr_si_engine_config;

typedef struct pwr_si_engine_input {
    double driver_throttle;
    double load_torque_nm;
    double starter_torque_nm;
    double exhaust_backpressure_pa;
    double ambient_pressure_pa;
    double ambient_temperature_k;
    uint32_t fault_mask;
    bool ignition_on;
} pwr_si_engine_input;

typedef struct pwr_si_engine_state {
    pwr_si_engine_config config;
    uint64_t tick;
    uint64_t last_crank_sample_tick;

    double crank_angle_rad;
    double crank_speed_rad_s;
    double sensed_crank_speed_rad_s;
    double throttle_position;
    double latched_throttle_command;
    double intake_air_mass_kg;
    double injector_fuel_flow_kg_per_s;
    double coolant_temperature_k;
    double low_voltage_v;

    double last_combustion_torque_nm;
    double last_brake_torque_nm;
    double last_pumping_torque_nm;
    double last_friction_torque_nm;
    double last_intake_air_flow_kg_per_s;
    double last_cylinder_air_flow_kg_per_s;
    double last_burned_fuel_flow_kg_per_s;
    double last_lambda;
    double last_exhaust_temperature_k;
    double last_exhaust_source_pressure_pa;
    double last_exhaust_species_mass_fraction[PWR_SI_ENGINE_SPECIES_COUNT];

    double initial_intake_air_mass_kg;
    double cumulative_intake_air_kg;
    double cumulative_cylinder_air_kg;
    double cumulative_exhaust_mass_kg;
    double cumulative_injected_fuel_kg;
    double cumulative_burned_fuel_kg;
    double cumulative_fuel_energy_j;
    double cumulative_unburned_fuel_energy_j;
    double cumulative_starter_work_j;
    double cumulative_load_work_j;
    double cumulative_exhaust_heat_j;
    double cumulative_ambient_heat_j;
    double cumulative_numerical_adjustment_j;
    double initial_stored_energy_j;

    uint32_t diagnostic_flags;
    bool crank_signal_valid;
    bool ecu_active;
    bool combustion_active;
} pwr_si_engine_state;

typedef struct pwr_si_engine_output {
    double time_s;
    double crank_angle_rad;
    double crank_speed_rad_s;
    double sensed_crank_speed_rad_s;
    double throttle_position;
    double manifold_pressure_pa;
    double low_voltage_v;
    double combustion_torque_nm;
    double brake_torque_nm;
    double pumping_torque_nm;
    double friction_torque_nm;
    double intake_air_flow_kg_per_s;
    double cylinder_air_flow_kg_per_s;
    double injected_fuel_flow_kg_per_s;
    double burned_fuel_flow_kg_per_s;
    double lambda;
    double exhaust_mass_flow_kg_per_s;
    double exhaust_temperature_k;
    double exhaust_source_pressure_pa;
    double exhaust_species_mass_fraction[PWR_SI_ENGINE_SPECIES_COUNT];
    double coolant_temperature_k;
    double fuel_energy_j;
    double mechanical_load_work_j;
    double exhaust_heat_j;
    double ambient_heat_j;
    double stored_energy_change_j;
    double unburned_fuel_energy_j;
    double numerical_adjustment_j;
    double energy_residual_j;
    double air_mass_residual_kg;
    uint64_t crank_sensor_age_ticks;
    uint32_t diagnostic_flags;
    bool crank_signal_valid;
    bool ecu_active;
    bool combustion_active;
    uint64_t state_hash;
} pwr_si_engine_output;

void pwr_si_engine_config_default(pwr_si_engine_config *config);
pwr_si_engine_result pwr_si_engine_validate_config(
    const pwr_si_engine_config *config);
pwr_si_engine_result pwr_si_engine_init(
    pwr_si_engine_state *state,
    const pwr_si_engine_config *config);
pwr_si_engine_result pwr_si_engine_step(
    pwr_si_engine_state *state,
    const pwr_si_engine_input *input,
    pwr_si_engine_output *output);
pwr_si_engine_result pwr_si_engine_observe(
    const pwr_si_engine_state *state,
    pwr_si_engine_output *output);
uint64_t pwr_si_engine_state_hash(const pwr_si_engine_state *state);

#endif
