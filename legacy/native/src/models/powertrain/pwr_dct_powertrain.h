#ifndef PWR_MODELS_POWERTRAIN_PWR_DCT_POWERTRAIN_H
#define PWR_MODELS_POWERTRAIN_PWR_DCT_POWERTRAIN_H

/*
 * Internal whole-powertrain integration scene.
 *
 * This scene couples the generic mean-value SI engine, the parameter-driven
 * seven-speed DCT, the reduced-order exhaust network, and a terminal
 * halfshaft inertia.  It is a simulation integration fixture, not a calibrated
 * EA211/DJS or DQ200 data set.  In particular, config_default leaves every DCT
 * gear ratio unset; a caller must supply its own ratios before init succeeds.
 *
 * One call advances exactly 100 us and performs no allocation.  The explicit
 * synchronization order is fixed and observable:
 *
 *   supervisor -> exhaust(t-1 engine source) -> engine(new backpressure,
 *   t-1 clutch load) -> DCT(new engine speed, old halfshaft speed) ->
 *   halfshaft/road load.
 *
 * Because this is an explicit partitioned solve, the state and output expose
 * engine/exhaust mass and enthalpy mismatch plus the two mechanical interface
 * energy mismatches.  They must not be mistaken for component conservation
 * residuals.
 */

#include "pwr_dct.h"
#include "pwr_exhaust.h"
#include "pwr_si_engine.h"

#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#define PWR_DCT_POWERTRAIN_BASE_TICK_NS UINT64_C(100000)

typedef enum pwr_dct_powertrain_result {
    PWR_DCT_POWERTRAIN_OK = 0,
    PWR_DCT_POWERTRAIN_INVALID_ARGUMENT,
    PWR_DCT_POWERTRAIN_INVALID_CONFIG,
    PWR_DCT_POWERTRAIN_INVALID_INPUT,
    PWR_DCT_POWERTRAIN_SUBMODEL_ERROR,
    PWR_DCT_POWERTRAIN_NUMERIC_ERROR
} pwr_dct_powertrain_result;

typedef enum pwr_dct_powertrain_control_mode {
    PWR_DCT_POWERTRAIN_CONTROL_SUPERVISED = 0,
    PWR_DCT_POWERTRAIN_CONTROL_DIRECT = 1
} pwr_dct_powertrain_control_mode;

typedef enum pwr_dct_powertrain_tcu_phase {
    PWR_DCT_POWERTRAIN_TCU_NEUTRAL = 0,
    PWR_DCT_POWERTRAIN_TCU_PRESELECT_1_2,
    PWR_DCT_POWERTRAIN_TCU_LAUNCH_1,
    PWR_DCT_POWERTRAIN_TCU_GEAR_1,
    PWR_DCT_POWERTRAIN_TCU_SHIFT_1_TO_2,
    PWR_DCT_POWERTRAIN_TCU_GEAR_2,
    PWR_DCT_POWERTRAIN_TCU_RELEASE_TO_NEUTRAL,
    PWR_DCT_POWERTRAIN_TCU_DIRECT
} pwr_dct_powertrain_tcu_phase;

typedef enum pwr_dct_powertrain_diagnostic {
    PWR_DCT_POWERTRAIN_DIAG_NONE = 0U,
    PWR_DCT_POWERTRAIN_DIAG_EXHAUST_MASS_MISMATCH = 1U << 0U,
    PWR_DCT_POWERTRAIN_DIAG_EXHAUST_ENTHALPY_MISMATCH = 1U << 1U,
    PWR_DCT_POWERTRAIN_DIAG_ENGINE_DCT_ENERGY_MISMATCH = 1U << 2U,
    PWR_DCT_POWERTRAIN_DIAG_DCT_OUTPUT_ENERGY_MISMATCH = 1U << 3U,
    PWR_DCT_POWERTRAIN_DIAG_HALFSHAFT_SPEED_CLAMP = 1U << 4U,
    PWR_DCT_POWERTRAIN_DIAG_TCU_PRESELECT_ACTIVE = 1U << 5U,
    PWR_DCT_POWERTRAIN_DIAG_TCU_SHIFT_ACTIVE = 1U << 6U,
    PWR_DCT_POWERTRAIN_DIAG_INPUT_REJECTED = 1U << 7U,
    PWR_DCT_POWERTRAIN_DIAG_SUBMODEL_FAILURE = 1U << 8U,
    PWR_DCT_POWERTRAIN_DIAG_NUMERIC_FAILURE = 1U << 9U
} pwr_dct_powertrain_diagnostic;

typedef struct pwr_dct_powertrain_config {
    pwr_si_engine_config engine;
    pwr_dct_config dct;
    pwr_exhaust_parameters exhaust;

    double initial_ambient_pressure_pa;
    double initial_ambient_temperature_k;
    double initial_exhaust_wall_temperature_k;

    /* All road-load torques are expressed at the DCT final output/halfshaft. */
    double halfshaft_inertia_kg_m2;
    double rolling_resistance_torque_nm;
    double viscous_road_load_nm_s_per_rad;
    double quadratic_road_load_nm_s2_per_rad2;
    double road_load_regularization_speed_rad_s;

    /* Generic ECU cold-start/idle supervisor parameters. */
    double starter_torque_nm;
    double starter_release_speed_rad_s;
    double maximum_cranking_time_s;
    double cranking_throttle_command;
    double idle_target_speed_rad_s;
    double idle_throttle_command;
    double idle_speed_throttle_gain_s_per_rad;

    /* Generic launch and 1 -> 2 TCU schedule; not a production calibration. */
    double launch_target_engine_speed_rad_s;
    double launch_clamp_feedforward;
    double launch_clamp_speed_gain_s_per_rad;
    double launch_complete_halfshaft_speed_rad_s;
    double first_to_second_shift_halfshaft_speed_rad_s;
    double minimum_first_gear_time_s;
    double first_to_second_shift_duration_s;
    double shift_engine_throttle_scale;

    /* Thresholds apply to one fixed tick, before accumulation. */
    double maximum_exhaust_mass_mismatch_kg_per_s;
    double maximum_exhaust_enthalpy_mismatch_w;
    double maximum_mechanical_coupling_mismatch_w;
} pwr_dct_powertrain_config;

typedef struct pwr_dct_powertrain_input {
    pwr_dct_powertrain_control_mode control_mode;
    double driver_throttle;
    double ambient_pressure_pa;
    double ambient_temperature_k;
    double additional_road_load_torque_nm;
    double tailpipe_area_scale;
    uint32_t engine_fault_mask;
    bool ignition_on;
    bool selector_drive;

    /* Used only in DIRECT mode.  Ratios still come from the configuration. */
    double direct_starter_torque_nm;
    pwr_dct_gear_mask direct_gear_mask;
    double direct_clutch_clamp[PWR_DCT_CLUTCH_COUNT];
} pwr_dct_powertrain_input;

typedef struct pwr_dct_powertrain_output {
    double time_s;
    uint64_t tick;
    pwr_dct_powertrain_control_mode control_mode;
    pwr_dct_powertrain_tcu_phase tcu_phase;
    double shift_progress;
    double commanded_engine_throttle;
    double commanded_starter_torque_nm;
    pwr_dct_gear_mask commanded_gear_mask;
    double commanded_clutch_clamp[PWR_DCT_CLUTCH_COUNT];

    pwr_si_engine_output engine;
    pwr_dct_snapshot dct;
    pwr_exhaust_observation exhaust;
    pwr_exhaust_diagnostics exhaust_step;

    double halfshaft_angle_rad;
    double halfshaft_speed_rad_s;
    double halfshaft_drive_torque_nm;
    double road_load_torque_nm;
    double halfshaft_kinetic_energy_j;
    double cumulative_halfshaft_drive_work_j;
    double cumulative_road_load_work_j;
    double cumulative_halfshaft_numerical_adjustment_j;
    double halfshaft_energy_residual_j;

    /* Partition-interface mismatch for the last tick and since init. */
    double exhaust_mass_mismatch_kg;
    double cumulative_exhaust_mass_mismatch_kg;
    double exhaust_enthalpy_mismatch_j;
    double cumulative_exhaust_enthalpy_mismatch_j;
    double engine_dct_energy_mismatch_j;
    double cumulative_engine_dct_energy_mismatch_j;
    double dct_output_energy_mismatch_j;
    double cumulative_dct_output_energy_mismatch_j;

    uint32_t last_step_diagnostic_flags;
    uint32_t diagnostic_flags;
    uint64_t state_hash;
} pwr_dct_powertrain_output;

typedef struct pwr_dct_powertrain_state {
    pwr_dct_powertrain_config config;
    pwr_si_engine_state engine;
    pwr_dct dct;
    pwr_exhaust_state exhaust;
    pwr_si_engine_output engine_output;
    pwr_dct_snapshot dct_output;
    pwr_exhaust_observation exhaust_output;
    pwr_exhaust_diagnostics exhaust_step;

    uint64_t tick;
    uint64_t tcu_phase_tick;
    uint64_t cranking_ticks;
    pwr_dct_powertrain_control_mode last_control_mode;
    pwr_dct_powertrain_tcu_phase tcu_phase;
    bool engine_started;

    double halfshaft_angle_rad;
    double halfshaft_speed_rad_s;
    double initial_halfshaft_kinetic_energy_j;
    double cumulative_halfshaft_drive_work_j;
    double cumulative_road_load_work_j;
    double cumulative_halfshaft_numerical_adjustment_j;

    double last_commanded_engine_throttle;
    double last_commanded_starter_torque_nm;
    pwr_dct_gear_mask last_commanded_gear_mask;
    double last_commanded_clutch_clamp[PWR_DCT_CLUTCH_COUNT];
    double last_road_load_torque_nm;
    double last_exhaust_mass_mismatch_kg;
    double cumulative_exhaust_mass_mismatch_kg;
    double last_exhaust_enthalpy_mismatch_j;
    double cumulative_exhaust_enthalpy_mismatch_j;
    double last_engine_dct_energy_mismatch_j;
    double cumulative_engine_dct_energy_mismatch_j;
    double last_dct_output_energy_mismatch_j;
    double cumulative_dct_output_energy_mismatch_j;

    uint32_t last_step_diagnostic_flags;
    uint32_t diagnostic_flags;
} pwr_dct_powertrain_state;

void pwr_dct_powertrain_config_default(pwr_dct_powertrain_config *config);
pwr_dct_powertrain_result pwr_dct_powertrain_validate_config(
    const pwr_dct_powertrain_config *config);
pwr_dct_powertrain_result pwr_dct_powertrain_init(
    pwr_dct_powertrain_state *state,
    const pwr_dct_powertrain_config *config);
pwr_dct_powertrain_result pwr_dct_powertrain_step(
    pwr_dct_powertrain_state *state,
    const pwr_dct_powertrain_input *input,
    pwr_dct_powertrain_output *output);
pwr_dct_powertrain_result pwr_dct_powertrain_observe(
    const pwr_dct_powertrain_state *state,
    pwr_dct_powertrain_output *output);
uint64_t pwr_dct_powertrain_state_hash(
    const pwr_dct_powertrain_state *state);
const char *pwr_dct_powertrain_result_string(
    pwr_dct_powertrain_result result);

#ifdef __cplusplus
}
#endif

#endif
