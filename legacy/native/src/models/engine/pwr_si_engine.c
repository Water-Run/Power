#include "pwr_si_engine.h"

#include <math.h>
#include <stddef.h>
#include <string.h>

static const double pwr_si_pi = 3.14159265358979323846264338327950288;

static bool pwr_si_positive_finite(double value)
{
    return isfinite(value) && value > 0.0;
}

static bool pwr_si_nonnegative_finite(double value)
{
    return isfinite(value) && value >= 0.0;
}

static double pwr_si_min(double left, double right)
{
    return left < right ? left : right;
}

static double pwr_si_max(double left, double right)
{
    return left > right ? left : right;
}

static double pwr_si_clamp(double value, double minimum, double maximum)
{
    return pwr_si_min(pwr_si_max(value, minimum), maximum);
}

static double pwr_si_initial_air_mass(const pwr_si_engine_config *config)
{
    return config->ambient_pressure_pa * config->intake_manifold_volume_m3
        / (config->air_gas_constant_j_per_kg_k
           * config->ambient_temperature_k);
}

static double pwr_si_stored_energy(const pwr_si_engine_state *state)
{
    const pwr_si_engine_config *const config = &state->config;
    const double kinetic = 0.5 * config->rotational_inertia_kg_m2
        * state->crank_speed_rad_s * state->crank_speed_rad_s;
    const double thermal = config->coolant_heat_capacity_j_per_k
        * (state->coolant_temperature_k
           - config->coolant_initial_temperature_k);
    return kinetic + thermal;
}

static bool pwr_si_input_valid(const pwr_si_engine_input *input)
{
    const uint32_t known_faults = PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT
        | PWR_SI_ENGINE_FAULT_THROTTLE_STUCK
        | PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED
        | PWR_SI_ENGINE_FAULT_LOW_VOLTAGE;

    return input != NULL && isfinite(input->driver_throttle)
        && input->driver_throttle >= 0.0 && input->driver_throttle <= 1.0
        && isfinite(input->load_torque_nm)
        && pwr_si_nonnegative_finite(input->starter_torque_nm)
        && pwr_si_positive_finite(input->exhaust_backpressure_pa)
        && pwr_si_positive_finite(input->ambient_pressure_pa)
        && pwr_si_positive_finite(input->ambient_temperature_k)
        && (input->fault_mask & ~known_faults) == 0U;
}

static double pwr_si_orifice_flow(
    const pwr_si_engine_config *config,
    double throttle_position,
    double upstream_pressure_pa,
    double upstream_temperature_k,
    double downstream_pressure_pa)
{
    const double gamma = 1.4;
    const double pressure_scale = pwr_si_max(
        upstream_pressure_pa, downstream_pressure_pa);
    const double pressure_difference = upstream_pressure_pa
        - downstream_pressure_pa;
    double direction = 1.0;
    double high_pressure = upstream_pressure_pa;
    double low_pressure = downstream_pressure_pa;
    double pressure_ratio;
    double flow_factor;
    double critical_ratio;

    if (fabs(pressure_difference) <= 1.0e-12 * pressure_scale) {
        return 0.0;
    }
    if (pressure_difference < 0.0) {
        direction = -1.0;
        high_pressure = downstream_pressure_pa;
        low_pressure = upstream_pressure_pa;
    }
    pressure_ratio = pwr_si_clamp(low_pressure / high_pressure, 0.0, 1.0);
    critical_ratio = pow(2.0 / (gamma + 1.0), gamma / (gamma - 1.0));
    if (pressure_ratio <= critical_ratio) {
        flow_factor = sqrt(gamma)
            * pow(2.0 / (gamma + 1.0),
                  (gamma + 1.0) / (2.0 * (gamma - 1.0)));
    }
    else {
        const double first = pow(pressure_ratio, 2.0 / gamma);
        const double second = pow(
            pressure_ratio, (gamma + 1.0) / gamma);
        flow_factor = sqrt(pwr_si_max(
            0.0,
            (2.0 * gamma / (gamma - 1.0)) * (first - second)));
    }

    return direction * config->throttle_discharge_coefficient
        * config->throttle_effective_area_m2 * throttle_position
        * high_pressure
        / sqrt(config->air_gas_constant_j_per_kg_k
               * upstream_temperature_k)
        * flow_factor;
}

void pwr_si_engine_config_default(pwr_si_engine_config *config)
{
    if (config == NULL) {
        return;
    }
    *config = (pwr_si_engine_config){
        .base_tick_ns = UINT64_C(100000),
        .sensor_period_ticks = UINT32_C(10),
        .ecu_period_ticks = UINT32_C(10),
        .maximum_sensor_age_ticks = UINT32_C(25),
        .cylinder_count = UINT32_C(4),
        .displacement_m3 = 1.5e-3,
        .compression_ratio = 10.0,
        .rotational_inertia_kg_m2 = 0.16,
        .intake_manifold_volume_m3 = 2.0e-3,
        .throttle_effective_area_m2 = 7.0e-4,
        .throttle_discharge_coefficient = 0.72,
        .throttle_time_constant_s = 0.055,
        .volumetric_efficiency = 0.88,
        .air_gas_constant_j_per_kg_k = 287.05,
        .ambient_pressure_pa = 101325.0,
        .ambient_temperature_k = 298.15,
        .stoichiometric_air_fuel_ratio = 14.7,
        .oxygen_mass_fraction = 0.23,
        .oxygen_per_fuel_kg_per_kg = 3.38,
        .fuel_lower_heating_value_j_per_kg = 42.5e6,
        .indicated_efficiency = 0.32,
        .exhaust_heat_fraction = 0.56,
        .injector_time_constant_s = 0.008,
        .friction_coulomb_nm = 8.0,
        .friction_viscous_nm_s_per_rad = 0.035,
        .pumping_scale = 1.0,
        .combustion_min_speed_rad_s = 35.0,
        .maximum_speed_rad_s = 680.0,
        .combustion_torque_ripple_fraction = 0.12,
        .coolant_initial_temperature_k = 298.15,
        .coolant_heat_capacity_j_per_k = 90000.0,
        .coolant_ambient_conductance_w_per_k = 45.0,
        .exhaust_temperature_floor_k = 430.0,
        .exhaust_effective_heat_capacity_j_per_kg_k = 1100.0,
        .exhaust_port_resistance_pa_per_kg_s_squared = 8.0e6,
        .low_voltage_nominal_v = 13.6,
        .low_voltage_internal_resistance_ohm = 0.018,
        .ecu_current_a = 3.0,
        .injector_current_a = 4.0,
        .starter_current_per_nm_a = 0.85,
        .ecu_brownout_threshold_v = 8.5,
    };
}

pwr_si_engine_result pwr_si_engine_validate_config(
    const pwr_si_engine_config *config)
{
    if (config == NULL) {
        return PWR_SI_ENGINE_INVALID_ARGUMENT;
    }
    if (config->base_tick_ns == UINT64_C(0)
        || config->sensor_period_ticks == UINT32_C(0)
        || config->ecu_period_ticks == UINT32_C(0)
        || config->maximum_sensor_age_ticks < config->sensor_period_ticks
        || config->cylinder_count == UINT32_C(0)
        || config->cylinder_count > UINT32_C(16)) {
        return PWR_SI_ENGINE_INVALID_CONFIG;
    }
    if (!pwr_si_positive_finite(config->displacement_m3)
        || !pwr_si_positive_finite(config->compression_ratio)
        || config->compression_ratio <= 1.0
        || !pwr_si_positive_finite(config->rotational_inertia_kg_m2)
        || !pwr_si_positive_finite(config->intake_manifold_volume_m3)
        || !pwr_si_positive_finite(config->throttle_effective_area_m2)
        || !pwr_si_positive_finite(config->throttle_discharge_coefficient)
        || config->throttle_discharge_coefficient > 1.0
        || !pwr_si_positive_finite(config->throttle_time_constant_s)
        || !pwr_si_positive_finite(config->volumetric_efficiency)
        || config->volumetric_efficiency > 2.0
        || !pwr_si_positive_finite(config->air_gas_constant_j_per_kg_k)
        || !pwr_si_positive_finite(config->ambient_pressure_pa)
        || !pwr_si_positive_finite(config->ambient_temperature_k)
        || !pwr_si_positive_finite(config->stoichiometric_air_fuel_ratio)
        || !pwr_si_positive_finite(config->oxygen_mass_fraction)
        || config->oxygen_mass_fraction >= 1.0
        || !pwr_si_positive_finite(config->oxygen_per_fuel_kg_per_kg)
        || config->oxygen_per_fuel_kg_per_kg
            > config->oxygen_mass_fraction
                * config->stoichiometric_air_fuel_ratio
        || !pwr_si_positive_finite(config->fuel_lower_heating_value_j_per_kg)
        || !pwr_si_positive_finite(config->indicated_efficiency)
        || config->indicated_efficiency >= 1.0
        || !pwr_si_nonnegative_finite(config->exhaust_heat_fraction)
        || config->exhaust_heat_fraction > 1.0
        || !pwr_si_positive_finite(config->injector_time_constant_s)
        || !pwr_si_nonnegative_finite(config->friction_coulomb_nm)
        || !pwr_si_nonnegative_finite(
            config->friction_viscous_nm_s_per_rad)
        || !pwr_si_nonnegative_finite(config->pumping_scale)
        || !pwr_si_positive_finite(config->combustion_min_speed_rad_s)
        || !pwr_si_positive_finite(config->maximum_speed_rad_s)
        || config->maximum_speed_rad_s
            <= config->combustion_min_speed_rad_s
        || !pwr_si_nonnegative_finite(
            config->combustion_torque_ripple_fraction)
        || config->combustion_torque_ripple_fraction >= 1.0
        || !pwr_si_positive_finite(config->coolant_initial_temperature_k)
        || !pwr_si_positive_finite(config->coolant_heat_capacity_j_per_k)
        || !pwr_si_nonnegative_finite(
            config->coolant_ambient_conductance_w_per_k)
        || !pwr_si_positive_finite(config->exhaust_temperature_floor_k)
        || !pwr_si_positive_finite(
            config->exhaust_effective_heat_capacity_j_per_kg_k)
        || !pwr_si_nonnegative_finite(
            config->exhaust_port_resistance_pa_per_kg_s_squared)
        || !pwr_si_positive_finite(config->low_voltage_nominal_v)
        || !pwr_si_nonnegative_finite(
            config->low_voltage_internal_resistance_ohm)
        || !pwr_si_nonnegative_finite(config->ecu_current_a)
        || !pwr_si_nonnegative_finite(config->injector_current_a)
        || !pwr_si_nonnegative_finite(config->starter_current_per_nm_a)
        || !pwr_si_positive_finite(config->ecu_brownout_threshold_v)
        || config->ecu_brownout_threshold_v
            >= config->low_voltage_nominal_v) {
        return PWR_SI_ENGINE_INVALID_CONFIG;
    }
    return PWR_SI_ENGINE_OK;
}

pwr_si_engine_result pwr_si_engine_init(
    pwr_si_engine_state *state,
    const pwr_si_engine_config *config)
{
    pwr_si_engine_state candidate = {0};
    const pwr_si_engine_result validation = pwr_si_engine_validate_config(
        config);

    if (state == NULL) {
        return PWR_SI_ENGINE_INVALID_ARGUMENT;
    }
    if (validation != PWR_SI_ENGINE_OK) {
        return validation;
    }

    candidate.config = *config;
    candidate.intake_air_mass_kg = pwr_si_initial_air_mass(config);
    candidate.initial_intake_air_mass_kg = candidate.intake_air_mass_kg;
    candidate.coolant_temperature_k = config->coolant_initial_temperature_k;
    candidate.low_voltage_v = config->low_voltage_nominal_v;
    candidate.last_lambda = 10.0;
    candidate.last_exhaust_temperature_k =
        config->exhaust_temperature_floor_k;
    candidate.last_exhaust_source_pressure_pa = config->ambient_pressure_pa;
    candidate.last_exhaust_species_mass_fraction[
        PWR_SI_ENGINE_SPECIES_INERT] = 1.0 - config->oxygen_mass_fraction;
    candidate.last_exhaust_species_mass_fraction[
        PWR_SI_ENGINE_SPECIES_OXYGEN] = config->oxygen_mass_fraction;
    candidate.initial_stored_energy_j = pwr_si_stored_energy(&candidate);
    *state = candidate;
    return PWR_SI_ENGINE_OK;
}

static uint64_t pwr_si_hash_u64(uint64_t hash, uint64_t value)
{
    for (unsigned int byte = 0U; byte < 8U; ++byte) {
        hash ^= (value >> (byte * 8U)) & UINT64_C(0xff);
        hash *= UINT64_C(1099511628211);
    }
    return hash;
}

static uint64_t pwr_si_hash_double(uint64_t hash, double value)
{
    uint64_t bits = UINT64_C(0);
    static_assert(sizeof(bits) == sizeof(value), "double must be 64-bit");
    memcpy(&bits, &value, sizeof(bits));
    return pwr_si_hash_u64(hash, bits);
}

uint64_t pwr_si_engine_state_hash(const pwr_si_engine_state *state)
{
    uint64_t hash = UINT64_C(14695981039346656037);

    if (state == NULL) {
        return UINT64_C(0);
    }
    hash = pwr_si_hash_u64(hash, state->tick);
    hash = pwr_si_hash_double(hash, state->crank_angle_rad);
    hash = pwr_si_hash_double(hash, state->crank_speed_rad_s);
    hash = pwr_si_hash_double(hash, state->sensed_crank_speed_rad_s);
    hash = pwr_si_hash_double(hash, state->throttle_position);
    hash = pwr_si_hash_double(hash, state->latched_throttle_command);
    hash = pwr_si_hash_double(hash, state->intake_air_mass_kg);
    hash = pwr_si_hash_double(hash, state->injector_fuel_flow_kg_per_s);
    hash = pwr_si_hash_double(hash, state->coolant_temperature_k);
    hash = pwr_si_hash_double(hash, state->low_voltage_v);
    hash = pwr_si_hash_double(hash, state->cumulative_intake_air_kg);
    hash = pwr_si_hash_double(hash, state->cumulative_cylinder_air_kg);
    hash = pwr_si_hash_double(hash, state->cumulative_exhaust_mass_kg);
    hash = pwr_si_hash_double(hash, state->cumulative_injected_fuel_kg);
    hash = pwr_si_hash_double(hash, state->cumulative_burned_fuel_kg);
    hash = pwr_si_hash_double(hash, state->cumulative_fuel_energy_j);
    hash = pwr_si_hash_double(hash, state->cumulative_starter_work_j);
    hash = pwr_si_hash_double(hash, state->cumulative_load_work_j);
    hash = pwr_si_hash_double(hash, state->cumulative_exhaust_heat_j);
    hash = pwr_si_hash_double(hash, state->cumulative_ambient_heat_j);
    hash = pwr_si_hash_double(
        hash, state->cumulative_numerical_adjustment_j);
    hash = pwr_si_hash_u64(hash, state->diagnostic_flags);
    hash = pwr_si_hash_u64(hash, state->crank_signal_valid ? 1U : 0U);
    hash = pwr_si_hash_u64(hash, state->ecu_active ? 1U : 0U);
    hash = pwr_si_hash_u64(hash, state->combustion_active ? 1U : 0U);
    return hash;
}

pwr_si_engine_result pwr_si_engine_observe(
    const pwr_si_engine_state *state,
    pwr_si_engine_output *output)
{
    const pwr_si_engine_config *config;
    double manifold_pressure;
    double stored_change;
    double air_residual;
    double energy_residual;
    uint64_t sensor_age;

    if (state == NULL || output == NULL) {
        return PWR_SI_ENGINE_INVALID_ARGUMENT;
    }
    config = &state->config;
    if (pwr_si_engine_validate_config(config) != PWR_SI_ENGINE_OK) {
        return PWR_SI_ENGINE_INVALID_CONFIG;
    }
    manifold_pressure = state->intake_air_mass_kg
        * config->air_gas_constant_j_per_kg_k
        * config->ambient_temperature_k
        / config->intake_manifold_volume_m3;
    stored_change = pwr_si_stored_energy(state)
        - state->initial_stored_energy_j;
    air_residual = state->initial_intake_air_mass_kg
        + state->cumulative_intake_air_kg
        - state->cumulative_cylinder_air_kg - state->intake_air_mass_kg;
    energy_residual = state->cumulative_fuel_energy_j
        + state->cumulative_starter_work_j
        - state->cumulative_load_work_j
        - state->cumulative_exhaust_heat_j
        - state->cumulative_ambient_heat_j
        - state->cumulative_unburned_fuel_energy_j - stored_change
        - state->cumulative_numerical_adjustment_j;
    sensor_age = state->crank_signal_valid
        ? state->tick - state->last_crank_sample_tick
        : UINT64_MAX;

    *output = (pwr_si_engine_output){
        .time_s = (double)state->tick * (double)config->base_tick_ns * 1.0e-9,
        .crank_angle_rad = state->crank_angle_rad,
        .crank_speed_rad_s = state->crank_speed_rad_s,
        .sensed_crank_speed_rad_s = state->sensed_crank_speed_rad_s,
        .throttle_position = state->throttle_position,
        .manifold_pressure_pa = manifold_pressure,
        .low_voltage_v = state->low_voltage_v,
        .combustion_torque_nm = state->last_combustion_torque_nm,
        .brake_torque_nm = state->last_brake_torque_nm,
        .pumping_torque_nm = state->last_pumping_torque_nm,
        .friction_torque_nm = state->last_friction_torque_nm,
        .intake_air_flow_kg_per_s = state->last_intake_air_flow_kg_per_s,
        .cylinder_air_flow_kg_per_s = state->last_cylinder_air_flow_kg_per_s,
        .injected_fuel_flow_kg_per_s = state->injector_fuel_flow_kg_per_s,
        .burned_fuel_flow_kg_per_s = state->last_burned_fuel_flow_kg_per_s,
        .lambda = state->last_lambda,
        .exhaust_mass_flow_kg_per_s = state->last_cylinder_air_flow_kg_per_s
            + state->injector_fuel_flow_kg_per_s,
        .exhaust_temperature_k = state->last_exhaust_temperature_k,
        .exhaust_source_pressure_pa = state->last_exhaust_source_pressure_pa,
        .coolant_temperature_k = state->coolant_temperature_k,
        .fuel_energy_j = state->cumulative_fuel_energy_j,
        .mechanical_load_work_j = state->cumulative_load_work_j,
        .exhaust_heat_j = state->cumulative_exhaust_heat_j,
        .ambient_heat_j = state->cumulative_ambient_heat_j,
        .stored_energy_change_j = stored_change,
        .unburned_fuel_energy_j = state->cumulative_unburned_fuel_energy_j,
        .numerical_adjustment_j = state->cumulative_numerical_adjustment_j,
        .energy_residual_j = energy_residual,
        .air_mass_residual_kg = air_residual,
        .crank_sensor_age_ticks = sensor_age,
        .diagnostic_flags = state->diagnostic_flags,
        .crank_signal_valid = state->crank_signal_valid,
        .ecu_active = state->ecu_active,
        .combustion_active = state->combustion_active,
        .state_hash = pwr_si_engine_state_hash(state),
    };
    for (size_t species = 0U; species < PWR_SI_ENGINE_SPECIES_COUNT;
         ++species) {
        output->exhaust_species_mass_fraction[species]
            = state->last_exhaust_species_mass_fraction[species];
    }
    return PWR_SI_ENGINE_OK;
}

static pwr_si_engine_result pwr_si_engine_advance(
    pwr_si_engine_state *state,
    const pwr_si_engine_input *input)
{
    const pwr_si_engine_config *const config = &state->config;
    const double dt = (double)config->base_tick_ns * 1.0e-9;
    double accessory_current;
    double manifold_pressure;
    double intake_flow;
    double cylinder_flow;
    double minimum_manifold_mass;
    double fuel_command = 0.0;
    double burned_fuel_flow = 0.0;
    double combustion_torque = 0.0;
    double pumping_torque;
    double friction_torque;
    double average_speed;
    double next_speed;
    double ripple;
    double burned_energy;
    double combustion_work;
    double starter_work;
    double pumping_work;
    double friction_work;
    double load_work;
    double net_mechanical_work;
    double kinetic_change;
    double residual_combustion_heat;
    double exhaust_heat;
    double coolant_heat;
    double ambient_heat;
    double exhaust_mass_flow;
    double unburned_fuel_flow;
    double species_mass_flow[PWR_SI_ENGINE_SPECIES_COUNT] = {0.0};

    accessory_current = config->ecu_current_a
        + config->starter_current_per_nm_a * input->starter_torque_nm;
    if (state->injector_fuel_flow_kg_per_s > 0.0) {
        accessory_current += config->injector_current_a;
    }
    state->low_voltage_v = config->low_voltage_nominal_v
        - config->low_voltage_internal_resistance_ohm * accessory_current;
    if ((input->fault_mask & PWR_SI_ENGINE_FAULT_LOW_VOLTAGE) != 0U) {
        state->low_voltage_v = 0.0;
    }

    if ((state->tick % config->sensor_period_ticks) == UINT64_C(0)) {
        if ((input->fault_mask
             & PWR_SI_ENGINE_FAULT_CRANK_SENSOR_DROPOUT)
            != 0U) {
            state->crank_signal_valid = false;
            state->diagnostic_flags
                |= PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_INVALID;
        }
        else {
            state->sensed_crank_speed_rad_s = state->crank_speed_rad_s;
            state->last_crank_sample_tick = state->tick;
            state->crank_signal_valid = true;
        }
    }

    if ((state->tick % config->ecu_period_ticks) == UINT64_C(0)) {
        const uint64_t sensor_age = state->crank_signal_valid
            ? state->tick - state->last_crank_sample_tick
            : UINT64_MAX;
        state->ecu_active = state->low_voltage_v
            >= config->ecu_brownout_threshold_v;
        if (!state->ecu_active) {
            state->diagnostic_flags |= PWR_SI_ENGINE_DIAG_ECU_BROWNOUT;
            state->latched_throttle_command = 0.0;
        }
        else {
            state->latched_throttle_command = input->driver_throttle;
        }
        if (sensor_age > config->maximum_sensor_age_ticks) {
            state->diagnostic_flags |= PWR_SI_ENGINE_DIAG_CRANK_SIGNAL_STALE;
        }
    }

    if ((input->fault_mask & PWR_SI_ENGINE_FAULT_THROTTLE_STUCK) == 0U) {
        const double response = 1.0 - exp(-dt / config->throttle_time_constant_s);
        state->throttle_position += response
            * (state->latched_throttle_command - state->throttle_position);
        state->throttle_position = pwr_si_clamp(
            state->throttle_position, 0.0, 1.0);
    }

    manifold_pressure = state->intake_air_mass_kg
        * config->air_gas_constant_j_per_kg_k * input->ambient_temperature_k
        / config->intake_manifold_volume_m3;
    intake_flow = pwr_si_orifice_flow(
        config,
        state->throttle_position,
        input->ambient_pressure_pa,
        input->ambient_temperature_k,
        manifold_pressure);
    cylinder_flow = manifold_pressure
        / (config->air_gas_constant_j_per_kg_k
           * input->ambient_temperature_k)
        * config->displacement_m3 * config->volumetric_efficiency
        * state->crank_speed_rad_s / (4.0 * pwr_si_pi);
    cylinder_flow = pwr_si_max(0.0, cylinder_flow);
    minimum_manifold_mass = 0.03 * input->ambient_pressure_pa
        * config->intake_manifold_volume_m3
        / (config->air_gas_constant_j_per_kg_k
           * input->ambient_temperature_k);
    if ((intake_flow - cylinder_flow) * dt
        < minimum_manifold_mass - state->intake_air_mass_kg) {
        cylinder_flow = pwr_si_max(
            0.0,
            intake_flow
                + (state->intake_air_mass_kg - minimum_manifold_mass) / dt);
        state->diagnostic_flags |= PWR_SI_ENGINE_DIAG_MANIFOLD_MASS_LIMIT;
    }
    state->intake_air_mass_kg += (intake_flow - cylinder_flow) * dt;
    state->cumulative_intake_air_kg += intake_flow * dt;
    state->cumulative_cylinder_air_kg += cylinder_flow * dt;

    {
        const uint64_t sensor_age = state->crank_signal_valid
            ? state->tick - state->last_crank_sample_tick
            : UINT64_MAX;
        const bool speed_valid = state->crank_signal_valid
            && sensor_age <= config->maximum_sensor_age_ticks;
        const bool below_limit = state->sensed_crank_speed_rad_s
            < config->maximum_speed_rad_s;
        state->combustion_active = input->ignition_on && state->ecu_active
            && speed_valid && below_limit
            && state->crank_speed_rad_s >= config->combustion_min_speed_rad_s
            && (input->fault_mask
                & PWR_SI_ENGINE_FAULT_INJECTOR_STUCK_CLOSED)
                == 0U;
        if (state->sensed_crank_speed_rad_s
            >= config->maximum_speed_rad_s) {
            state->diagnostic_flags |= PWR_SI_ENGINE_DIAG_SPEED_LIMIT;
        }
        if (state->combustion_active) {
            fuel_command = cylinder_flow
                / config->stoichiometric_air_fuel_ratio;
        }
        else {
            state->diagnostic_flags |= PWR_SI_ENGINE_DIAG_FUEL_CUT;
        }
    }

    {
        const double injector_response = 1.0
            - exp(-dt / config->injector_time_constant_s);
        state->injector_fuel_flow_kg_per_s += injector_response
            * (fuel_command - state->injector_fuel_flow_kg_per_s);
        state->injector_fuel_flow_kg_per_s = pwr_si_max(
            0.0, state->injector_fuel_flow_kg_per_s);
    }
    if (state->combustion_active) {
        const double oxygen_available = cylinder_flow
            * config->oxygen_mass_fraction;
        burned_fuel_flow = pwr_si_min(
            state->injector_fuel_flow_kg_per_s,
            oxygen_available / config->oxygen_per_fuel_kg_per_kg);
    }
    if (state->injector_fuel_flow_kg_per_s > 1.0e-12) {
        state->last_lambda = cylinder_flow
            / (state->injector_fuel_flow_kg_per_s
               * config->stoichiometric_air_fuel_ratio);
    }
    else {
        state->last_lambda = 10.0;
    }

    ripple = 1.0 + config->combustion_torque_ripple_fraction
        * cos(0.5 * (double)config->cylinder_count * state->crank_angle_rad);
    if (burned_fuel_flow > 0.0) {
        const double indicated_power = burned_fuel_flow
            * config->fuel_lower_heating_value_j_per_kg
            * config->indicated_efficiency;
        combustion_torque = indicated_power
            / pwr_si_max(
                state->crank_speed_rad_s,
                config->combustion_min_speed_rad_s)
            * ripple;
    }
    pumping_torque = config->pumping_scale
        * pwr_si_max(input->exhaust_backpressure_pa - manifold_pressure, 0.0)
        * config->displacement_m3 / (4.0 * pwr_si_pi);
    friction_torque = config->friction_coulomb_nm
        * tanh(state->crank_speed_rad_s / 5.0)
        + config->friction_viscous_nm_s_per_rad
            * state->crank_speed_rad_s;

    next_speed = state->crank_speed_rad_s
        + (combustion_torque + input->starter_torque_nm - pumping_torque
           - friction_torque - input->load_torque_nm)
            / config->rotational_inertia_kg_m2
            * dt;
    if (next_speed < 0.0) {
        next_speed = 0.0;
        state->diagnostic_flags |= PWR_SI_ENGINE_DIAG_SPEED_LIMIT;
    }
    average_speed = 0.5 * (state->crank_speed_rad_s + next_speed);
    burned_energy = burned_fuel_flow
        * config->fuel_lower_heating_value_j_per_kg * dt;
    combustion_work = combustion_torque * average_speed * dt;
    starter_work = input->starter_torque_nm * average_speed * dt;
    pumping_work = pumping_torque * average_speed * dt;
    friction_work = friction_torque * average_speed * dt;
    load_work = input->load_torque_nm * average_speed * dt;
    net_mechanical_work = combustion_work + starter_work - pumping_work
        - friction_work - load_work;
    kinetic_change = 0.5 * config->rotational_inertia_kg_m2
        * (next_speed * next_speed
           - state->crank_speed_rad_s * state->crank_speed_rad_s);
    state->cumulative_numerical_adjustment_j += net_mechanical_work
        - kinetic_change;
    state->crank_speed_rad_s = next_speed;
    state->crank_angle_rad = fmod(
        state->crank_angle_rad + average_speed * dt, 4.0 * pwr_si_pi);

    residual_combustion_heat = pwr_si_max(
        0.0, burned_energy - combustion_work);
    exhaust_heat = config->exhaust_heat_fraction
        * residual_combustion_heat + pumping_work;
    coolant_heat = (1.0 - config->exhaust_heat_fraction)
        * residual_combustion_heat + friction_work;
    ambient_heat = config->coolant_ambient_conductance_w_per_k
        * (state->coolant_temperature_k - input->ambient_temperature_k) * dt;
    state->coolant_temperature_k += (coolant_heat - ambient_heat)
        / config->coolant_heat_capacity_j_per_k;

    unburned_fuel_flow = state->injector_fuel_flow_kg_per_s
        - burned_fuel_flow;
    exhaust_mass_flow = cylinder_flow
        + state->injector_fuel_flow_kg_per_s;
    species_mass_flow[PWR_SI_ENGINE_SPECIES_INERT] = cylinder_flow
        * (1.0 - config->oxygen_mass_fraction);
    species_mass_flow[PWR_SI_ENGINE_SPECIES_OXYGEN] = pwr_si_max(
        0.0,
        cylinder_flow * config->oxygen_mass_fraction
            - burned_fuel_flow * config->oxygen_per_fuel_kg_per_kg);
    species_mass_flow[PWR_SI_ENGINE_SPECIES_REDUCTANT] = unburned_fuel_flow;
    species_mass_flow[PWR_SI_ENGINE_SPECIES_PRODUCTS] = burned_fuel_flow
        * (1.0 + config->oxygen_per_fuel_kg_per_kg);
    if (exhaust_mass_flow > 1.0e-12) {
        for (size_t species = 0U; species < PWR_SI_ENGINE_SPECIES_COUNT;
             ++species) {
            state->last_exhaust_species_mass_fraction[species]
                = species_mass_flow[species] / exhaust_mass_flow;
        }
    }
    state->last_exhaust_temperature_k = config->exhaust_temperature_floor_k;
    if (exhaust_mass_flow > 1.0e-12) {
        state->last_exhaust_temperature_k = pwr_si_max(
            config->exhaust_temperature_floor_k,
            input->ambient_temperature_k
                + exhaust_heat
                    / (dt * exhaust_mass_flow
                       * config->exhaust_effective_heat_capacity_j_per_kg_k));
    }
    state->last_exhaust_source_pressure_pa = input->exhaust_backpressure_pa
        + config->exhaust_port_resistance_pa_per_kg_s_squared
            * exhaust_mass_flow * exhaust_mass_flow;

    state->cumulative_exhaust_mass_kg += exhaust_mass_flow * dt;
    state->cumulative_injected_fuel_kg +=
        state->injector_fuel_flow_kg_per_s * dt;
    state->cumulative_burned_fuel_kg += burned_fuel_flow * dt;
    state->cumulative_fuel_energy_j +=
        state->injector_fuel_flow_kg_per_s
        * config->fuel_lower_heating_value_j_per_kg * dt;
    state->cumulative_unburned_fuel_energy_j += unburned_fuel_flow
        * config->fuel_lower_heating_value_j_per_kg * dt;
    state->cumulative_starter_work_j += starter_work;
    state->cumulative_load_work_j += load_work;
    state->cumulative_exhaust_heat_j += exhaust_heat;
    state->cumulative_ambient_heat_j += ambient_heat;

    state->last_combustion_torque_nm = combustion_torque;
    state->last_pumping_torque_nm = pumping_torque;
    state->last_friction_torque_nm = friction_torque;
    state->last_brake_torque_nm = combustion_torque - pumping_torque
        - friction_torque;
    state->last_intake_air_flow_kg_per_s = intake_flow;
    state->last_cylinder_air_flow_kg_per_s = cylinder_flow;
    state->last_burned_fuel_flow_kg_per_s = burned_fuel_flow;
    ++state->tick;

    if (!isfinite(state->crank_angle_rad)
        || !pwr_si_nonnegative_finite(state->crank_speed_rad_s)
        || !pwr_si_positive_finite(state->intake_air_mass_kg)
        || !pwr_si_nonnegative_finite(state->injector_fuel_flow_kg_per_s)
        || !pwr_si_positive_finite(state->coolant_temperature_k)
        || !isfinite(state->last_exhaust_temperature_k)
        || !isfinite(state->last_exhaust_source_pressure_pa)) {
        state->diagnostic_flags |= PWR_SI_ENGINE_DIAG_NUMERIC_FAILURE;
        return PWR_SI_ENGINE_NUMERIC_ERROR;
    }
    return PWR_SI_ENGINE_OK;
}

pwr_si_engine_result pwr_si_engine_step(
    pwr_si_engine_state *state,
    const pwr_si_engine_input *input,
    pwr_si_engine_output *output)
{
    pwr_si_engine_state candidate;
    pwr_si_engine_result result;

    if (state == NULL || output == NULL) {
        return PWR_SI_ENGINE_INVALID_ARGUMENT;
    }
    if (pwr_si_engine_validate_config(&state->config) != PWR_SI_ENGINE_OK) {
        return PWR_SI_ENGINE_INVALID_CONFIG;
    }
    if (!pwr_si_input_valid(input)) {
        return PWR_SI_ENGINE_INVALID_INPUT;
    }

    candidate = *state;
    result = pwr_si_engine_advance(&candidate, input);
    if (result != PWR_SI_ENGINE_OK) {
        return result;
    }
    result = pwr_si_engine_observe(&candidate, output);
    if (result != PWR_SI_ENGINE_OK) {
        return result;
    }
    *state = candidate;
    return PWR_SI_ENGINE_OK;
}
