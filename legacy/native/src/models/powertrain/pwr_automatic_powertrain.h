#ifndef PWR_MODELS_POWERTRAIN_PWR_AUTOMATIC_POWERTRAIN_H
#define PWR_MODELS_POWERTRAIN_PWR_AUTOMATIC_POWERTRAIN_H

/*
 * Internal generic SI-engine + hydraulic four-speed automatic powertrain.
 *
 * This integration scene is parameter driven.  Its defaults and tests are
 * synthetic laboratory calibration and are not PSA EC5 or AT8 production
 * data.  Callers may replace all 1--4/R ratios, final drive, hydraulic,
 * electrical, shift, lockup, differential, and road-load parameters.
 *
 * Each call advances exactly 100 us without allocation.  Synchronization is
 * explicit and deterministic:
 *
 *   electrical/TCU -> exhaust(previous engine source) -> engine(new
 *   backpressure, previous converter reaction) -> automatic(imposed new
 *   crank/pump speed, previous reflected road load) -> final drive/open
 *   differential/left and right halfshaft projection.
 *
 * The exposed mismatch fields audit the one-tick partition lag, imposed pump
 * speed, and differential speed projection.  They are separate from the
 * conservation residuals reported by the component models.
 */

#include "pwr_automatic.h"
#include "pwr_exhaust.h"
#include "pwr_si_engine.h"

#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#define PWR_AUTOMATIC_POWERTRAIN_BASE_TICK_NS UINT64_C(100000)

typedef enum pwr_automatic_powertrain_result {
    PWR_AUTOMATIC_POWERTRAIN_OK = 0,
    PWR_AUTOMATIC_POWERTRAIN_INVALID_ARGUMENT,
    PWR_AUTOMATIC_POWERTRAIN_INVALID_CONFIG,
    PWR_AUTOMATIC_POWERTRAIN_INVALID_INPUT,
    PWR_AUTOMATIC_POWERTRAIN_SUBMODEL_ERROR,
    PWR_AUTOMATIC_POWERTRAIN_NUMERIC_ERROR
} pwr_automatic_powertrain_result;

typedef enum pwr_automatic_powertrain_control_mode {
    PWR_AUTOMATIC_POWERTRAIN_CONTROL_SUPERVISED = 0,
    PWR_AUTOMATIC_POWERTRAIN_CONTROL_DIRECT = 1
} pwr_automatic_powertrain_control_mode;

typedef enum pwr_automatic_powertrain_tcu_phase {
    PWR_AUTOMATIC_POWERTRAIN_TCU_NEUTRAL = 0,
    PWR_AUTOMATIC_POWERTRAIN_TCU_FIRST,
    PWR_AUTOMATIC_POWERTRAIN_TCU_SHIFT_1_TO_2,
    PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND,
    PWR_AUTOMATIC_POWERTRAIN_TCU_SECOND_LOCKED,
    PWR_AUTOMATIC_POWERTRAIN_TCU_BROWNOUT,
    PWR_AUTOMATIC_POWERTRAIN_TCU_DIRECT
} pwr_automatic_powertrain_tcu_phase;

typedef enum pwr_automatic_powertrain_diagnostic {
    PWR_AUTOMATIC_POWERTRAIN_DIAG_NONE = 0U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_ECU_BROWNOUT = 1U << 0U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_TCU_BROWNOUT = 1U << 1U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_SHIFT_ACTIVE = 1U << 2U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_LOCKUP_ACTIVE = 1U << 3U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH = 1U << 4U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH = 1U << 5U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_ENGINE_CONVERTER_ENERGY_MISMATCH = 1U << 6U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_PUMP_SPEED_PROJECTED = 1U << 7U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_DIFFERENTIAL_SPEED_PROJECTED = 1U << 8U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_FINAL_DRIVE_ENERGY_MISMATCH = 1U << 9U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_INPUT_REJECTED = 1U << 10U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_SUBMODEL_FAILURE = 1U << 11U,
    PWR_AUTOMATIC_POWERTRAIN_DIAG_NUMERIC_FAILURE = 1U << 12U
} pwr_automatic_powertrain_diagnostic;

typedef struct pwr_automatic_powertrain_config {
    pwr_si_engine_config engine;
    pwr_automatic_config automatic;
    pwr_exhaust_parameters exhaust;

    double initial_ambient_pressure_pa;
    double initial_ambient_temperature_k;
    double initial_exhaust_wall_temperature_k;

    /* Common 12 V supply and simplified controller/solenoid current budget. */
    double electrical_nominal_voltage_v;
    double electrical_internal_resistance_ohm;
    double base_accessory_current_a;
    double tcu_current_a;
    double solenoid_current_at_max_pressure_a;
    double starter_current_per_nm_a;
    double ecu_brownout_voltage_v;
    double tcu_brownout_voltage_v;

    /* Generic ECU starting and idle supervisor. */
    double starter_torque_nm;
    double starter_release_speed_rad_s;
    double maximum_cranking_time_s;
    double cranking_throttle_command;
    double idle_target_speed_rad_s;
    double idle_throttle_command;
    double idle_speed_throttle_gain_s_per_rad;

    /* Generic TCU thresholds.  They are not a production shift map. */
    double line_pressure_throttle_gain_pa;
    double first_to_second_halfshaft_speed_rad_s;
    double minimum_first_gear_time_s;
    double lockup_enable_halfshaft_speed_rad_s;
    double shift_engine_throttle_scale;

    /* Final drive and ideal open-differential terminal model. */
    double final_drive_ratio;
    double final_drive_efficiency;
    double left_halfshaft_inertia_kg_m2;
    double right_halfshaft_inertia_kg_m2;
    double rolling_resistance_torque_nm_per_side;
    double viscous_road_load_nm_s_per_rad_per_side;
    double quadratic_road_load_nm_s2_per_rad2_per_side;
    double road_load_regularization_speed_rad_s;

    /* Per-tick mismatch thresholds used only for diagnostics. */
    double maximum_exhaust_mass_mismatch_kg_per_s;
    double maximum_exhaust_enthalpy_mismatch_w;
    double maximum_mechanical_coupling_mismatch_w;
    double maximum_speed_projection_rad_s;
} pwr_automatic_powertrain_config;

typedef struct pwr_automatic_powertrain_input {
    pwr_automatic_powertrain_control_mode control_mode;
    double driver_throttle;
    double ambient_pressure_pa;
    double ambient_temperature_k;
    double electrical_supply_voltage_v;
    double left_additional_road_load_torque_nm;
    double right_additional_road_load_torque_nm;
    double tailpipe_area_scale;
    uint32_t engine_fault_mask;
    bool ignition_on;
    bool selector_drive;

    /* Used only in DIRECT mode. */
    double direct_starter_torque_nm;
    double direct_line_pressure_command_pa;
    double direct_lockup_command;
    pwr_automatic_gear direct_requested_gear;
} pwr_automatic_powertrain_input;

typedef struct pwr_automatic_powertrain_output {
    double time_s;
    uint64_t tick;
    pwr_automatic_powertrain_control_mode control_mode;
    pwr_automatic_powertrain_tcu_phase tcu_phase;
    double low_voltage_v;
    bool ecu_powered;
    bool tcu_powered;
    double commanded_engine_throttle;
    double commanded_starter_torque_nm;
    double commanded_line_pressure_pa;
    double commanded_lockup;
    pwr_automatic_gear commanded_gear;

    pwr_si_engine_output engine;
    pwr_automatic_snapshot automatic;
    pwr_automatic_coupling_output converter_coupling;
    pwr_exhaust_observation exhaust;
    pwr_exhaust_diagnostics exhaust_step;

    double differential_carrier_speed_rad_s;
    double left_halfshaft_speed_rad_s;
    double right_halfshaft_speed_rad_s;
    double left_halfshaft_angle_rad;
    double right_halfshaft_angle_rad;
    double left_drive_torque_nm;
    double right_drive_torque_nm;
    double left_road_load_torque_nm;
    double right_road_load_torque_nm;
    double differential_speed_rad_s;

    double halfshaft_kinetic_energy_j;
    double cumulative_halfshaft_drive_work_j;
    double cumulative_road_load_work_j;
    double cumulative_differential_projection_adjustment_j;
    double halfshaft_energy_residual_j;

    double exhaust_mass_mismatch_kg;
    double cumulative_exhaust_mass_mismatch_kg;
    double exhaust_enthalpy_mismatch_j;
    double cumulative_exhaust_enthalpy_mismatch_j;
    double engine_converter_energy_mismatch_j;
    double cumulative_engine_converter_energy_mismatch_j;
    double pump_speed_projection_rad_s;
    double pump_projection_adjustment_j;
    double unconstrained_carrier_speed_mismatch_rad_s;
    double carrier_constraint_residual_rad_s;
    double differential_projection_adjustment_j;
    double final_drive_energy_mismatch_j;
    double cumulative_final_drive_energy_mismatch_j;

    uint32_t last_step_diagnostic_flags;
    uint32_t diagnostic_flags;
    uint64_t state_hash;
} pwr_automatic_powertrain_output;

typedef struct pwr_automatic_powertrain_state {
    pwr_automatic_powertrain_config config;
    pwr_si_engine_state engine;
    pwr_automatic automatic;
    pwr_exhaust_state exhaust;
    pwr_si_engine_output engine_output;
    pwr_automatic_snapshot automatic_output;
    pwr_automatic_coupling_output converter_coupling;
    pwr_exhaust_observation exhaust_output;
    pwr_exhaust_diagnostics exhaust_step;

    uint64_t tick;
    uint64_t cranking_ticks;
    uint64_t first_gear_ticks;
    pwr_automatic_powertrain_control_mode last_control_mode;
    pwr_automatic_powertrain_tcu_phase tcu_phase;
    pwr_automatic_gear supervised_gear;
    bool engine_started;
    bool ecu_powered;
    bool tcu_powered;
    double low_voltage_v;

    double left_halfshaft_speed_rad_s;
    double right_halfshaft_speed_rad_s;
    double left_halfshaft_angle_rad;
    double right_halfshaft_angle_rad;
    double initial_halfshaft_kinetic_energy_j;
    double cumulative_halfshaft_drive_work_j;
    double cumulative_road_load_work_j;
    double cumulative_differential_projection_adjustment_j;

    double last_commanded_engine_throttle;
    double last_commanded_starter_torque_nm;
    double last_commanded_line_pressure_pa;
    double last_commanded_lockup;
    pwr_automatic_gear last_commanded_gear;
    double last_left_drive_torque_nm;
    double last_right_drive_torque_nm;
    double last_left_road_load_torque_nm;
    double last_right_road_load_torque_nm;

    double last_exhaust_mass_mismatch_kg;
    double cumulative_exhaust_mass_mismatch_kg;
    double last_exhaust_enthalpy_mismatch_j;
    double cumulative_exhaust_enthalpy_mismatch_j;
    double last_engine_converter_energy_mismatch_j;
    double cumulative_engine_converter_energy_mismatch_j;
    double last_pump_speed_projection_rad_s;
    double last_unconstrained_carrier_speed_mismatch_rad_s;
    double last_carrier_constraint_residual_rad_s;
    double last_differential_projection_adjustment_j;
    double last_final_drive_energy_mismatch_j;
    double cumulative_final_drive_energy_mismatch_j;

    uint32_t last_step_diagnostic_flags;
    uint32_t diagnostic_flags;
} pwr_automatic_powertrain_state;

void pwr_automatic_powertrain_config_default(
    pwr_automatic_powertrain_config *config);
pwr_automatic_powertrain_result pwr_automatic_powertrain_validate_config(
    const pwr_automatic_powertrain_config *config);
pwr_automatic_powertrain_result pwr_automatic_powertrain_init(
    pwr_automatic_powertrain_state *state,
    const pwr_automatic_powertrain_config *config);
pwr_automatic_powertrain_result pwr_automatic_powertrain_step(
    pwr_automatic_powertrain_state *state,
    const pwr_automatic_powertrain_input *input,
    pwr_automatic_powertrain_output *output);
pwr_automatic_powertrain_result pwr_automatic_powertrain_observe(
    const pwr_automatic_powertrain_state *state,
    pwr_automatic_powertrain_output *output);
uint64_t pwr_automatic_powertrain_state_hash(
    const pwr_automatic_powertrain_state *state);
const char *pwr_automatic_powertrain_result_string(
    pwr_automatic_powertrain_result result);

#ifdef __cplusplus
}
#endif

#endif
