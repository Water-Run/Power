#ifndef PWR_MODELS_DRIVETRAIN_PWR_DCT_H
#define PWR_MODELS_DRIVETRAIN_PWR_DCT_H

/*
 * Internal, parameter-driven dual dry-clutch transmission model.
 *
 * The fixed topology matches a seven-forward-speed dual-clutch layout:
 * K1 drives the odd-gear input shaft (1/3/5/7), while K2 drives the
 * even/reverse input shaft (R/2/4/6).  Ratios and capacities are calibration
 * data.  In particular, pwr_dct_config_default deliberately leaves every
 * gear ratio at zero so no vehicle-specific calibration is implied.
 *
 * The model is an effort-producing coupling element.  A host supplies both
 * boundary speeds and applies input_transmitted_torque_nm as an opposing
 * torque to its engine/crankshaft model and output_torque_nm to its final
 * drive/load model.
 */

#include <stdbool.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

enum {
    PWR_DCT_CLUTCH_COUNT = 2,
    PWR_DCT_GEAR_COUNT = 8
};

typedef enum pwr_dct_clutch {
    PWR_DCT_CLUTCH_INVALID = -1,
    PWR_DCT_CLUTCH_K1 = 0,
    PWR_DCT_CLUTCH_K2 = 1
} pwr_dct_clutch;

typedef enum pwr_dct_gear {
    PWR_DCT_GEAR_NEUTRAL = -1,
    PWR_DCT_GEAR_REVERSE = 0,
    PWR_DCT_GEAR_1 = 1,
    PWR_DCT_GEAR_2 = 2,
    PWR_DCT_GEAR_3 = 3,
    PWR_DCT_GEAR_4 = 4,
    PWR_DCT_GEAR_5 = 5,
    PWR_DCT_GEAR_6 = 6,
    PWR_DCT_GEAR_7 = 7
} pwr_dct_gear;

typedef uint16_t pwr_dct_gear_mask;

#define PWR_DCT_GEAR_MASK_REVERSE UINT16_C(0x01)
#define PWR_DCT_GEAR_MASK_1 UINT16_C(0x02)
#define PWR_DCT_GEAR_MASK_2 UINT16_C(0x04)
#define PWR_DCT_GEAR_MASK_3 UINT16_C(0x08)
#define PWR_DCT_GEAR_MASK_4 UINT16_C(0x10)
#define PWR_DCT_GEAR_MASK_5 UINT16_C(0x20)
#define PWR_DCT_GEAR_MASK_6 UINT16_C(0x40)
#define PWR_DCT_GEAR_MASK_7 UINT16_C(0x80)
#define PWR_DCT_GEAR_MASK_K1 UINT16_C(0xaa)
#define PWR_DCT_GEAR_MASK_K2 UINT16_C(0x55)
#define PWR_DCT_GEAR_MASK_ALL UINT16_C(0xff)

typedef enum pwr_dct_result {
    PWR_DCT_OK = 0,
    PWR_DCT_INVALID_ARGUMENT,
    PWR_DCT_INVALID_CONFIG,
    PWR_DCT_INVALID_GEAR_COMMAND,
    PWR_DCT_INVALID_TIMESTEP,
    PWR_DCT_NUMERIC_ERROR
} pwr_dct_result;

typedef enum pwr_dct_diagnostic {
    PWR_DCT_DIAG_NONE = 0U,
    PWR_DCT_DIAG_INVALID_GEAR_BIT = 1U << 0U,
    PWR_DCT_DIAG_MULTIPLE_GEARS_ON_SHAFT = 1U << 1U,
    PWR_DCT_DIAG_DOG_SHIFT_WHILE_CLAMPED = 1U << 2U,
    PWR_DCT_DIAG_K1_SLIPPING = 1U << 3U,
    PWR_DCT_DIAG_K2_SLIPPING = 1U << 4U,
    PWR_DCT_DIAG_K1_THERMAL_FADE = 1U << 5U,
    PWR_DCT_DIAG_K2_THERMAL_FADE = 1U << 6U,
    PWR_DCT_DIAG_K1_OVERHEAT = 1U << 7U,
    PWR_DCT_DIAG_K2_OVERHEAT = 1U << 8U,
    PWR_DCT_DIAG_CLAMPED_SHAFT_IN_NEUTRAL = 1U << 9U,
    PWR_DCT_DIAG_ENERGY_RESIDUAL = 1U << 10U,
    PWR_DCT_DIAG_NUMERIC_FAILURE = 1U << 11U,
    PWR_DCT_DIAG_INPUT_REJECTED = 1U << 12U
} pwr_dct_diagnostic;

typedef struct pwr_dct_config {
    /* Signed input-shaft/output-shaft speed ratios; reverse must be negative. */
    double gear_ratio[PWR_DCT_GEAR_COUNT];
    double mesh_efficiency[PWR_DCT_GEAR_COUNT];
    double final_drive_ratio;

    double clutch_nominal_capacity_nm[PWR_DCT_CLUTCH_COUNT];
    double slip_regularization_speed_rad_s;
    double clamp_engagement_rate_per_s[PWR_DCT_CLUTCH_COUNT];
    double clamp_release_rate_per_s[PWR_DCT_CLUTCH_COUNT];
    double maximum_dog_shift_clamp;

    double initial_clutch_temperature_k[PWR_DCT_CLUTCH_COUNT];
    double clutch_heat_capacity_j_per_k[PWR_DCT_CLUTCH_COUNT];
    double clutch_ambient_conductance_w_per_k[PWR_DCT_CLUTCH_COUNT];
    double fade_start_temperature_k[PWR_DCT_CLUTCH_COUNT];
    double fade_full_temperature_k[PWR_DCT_CLUTCH_COUNT];
    double minimum_fade_capacity_fraction[PWR_DCT_CLUTCH_COUNT];
    double overheat_temperature_k[PWR_DCT_CLUTCH_COUNT];

    double slip_diagnostic_speed_rad_s;
    double maximum_timestep_s;
} pwr_dct_config;

typedef struct pwr_dct_inputs {
    double engine_speed_rad_s;
    double output_speed_rad_s;
    pwr_dct_gear_mask engaged_gear_mask;
    double clutch_clamp_command[PWR_DCT_CLUTCH_COUNT];
    double ambient_temperature_k;
} pwr_dct_inputs;

typedef struct pwr_dct_clutch_observation {
    pwr_dct_gear selected_gear;
    bool preselected;
    double clamp_command;
    double applied_clamp;
    double input_shaft_speed_rad_s;
    double slip_speed_rad_s;
    double transmitted_torque_nm;
    double torque_capacity_nm;
    double friction_power_w;
    double cumulative_friction_work_j;
    double temperature_k;
    double thermal_capacity_factor;
} pwr_dct_clutch_observation;

typedef struct pwr_dct_snapshot {
    double time_s;
    pwr_dct_gear_mask engaged_gear_mask;
    pwr_dct_clutch_observation clutch[PWR_DCT_CLUTCH_COUNT];

    /* Positive input torque leaves the engine and enters the clutches. */
    double input_transmitted_torque_nm;
    double output_torque_nm;
    double input_power_w;
    double output_power_w;
    double clutch_friction_power_w;
    double gear_mesh_loss_power_w;
    double instantaneous_efficiency;

    double cumulative_input_energy_j;
    double cumulative_output_energy_j;
    double cumulative_gear_mesh_loss_j;
    double cumulative_heat_rejected_j;
    double stored_thermal_energy_change_j;
    double energy_residual_j;

    uint32_t last_step_diagnostic_flags;
    uint32_t diagnostic_flags;
    uint64_t rejected_command_count;
    uint64_t state_hash;
} pwr_dct_snapshot;

typedef struct pwr_dct {
    pwr_dct_config config;
    pwr_dct_gear_mask engaged_gear_mask;
    pwr_dct_gear selected_gear[PWR_DCT_CLUTCH_COUNT];
    double applied_clamp[PWR_DCT_CLUTCH_COUNT];
    double clutch_temperature_k[PWR_DCT_CLUTCH_COUNT];
    double cumulative_friction_work_j[PWR_DCT_CLUTCH_COUNT];

    double last_clamp_command[PWR_DCT_CLUTCH_COUNT];
    double last_input_shaft_speed_rad_s[PWR_DCT_CLUTCH_COUNT];
    double last_slip_speed_rad_s[PWR_DCT_CLUTCH_COUNT];
    double last_transmitted_torque_nm[PWR_DCT_CLUTCH_COUNT];
    double last_torque_capacity_nm[PWR_DCT_CLUTCH_COUNT];
    double last_friction_power_w[PWR_DCT_CLUTCH_COUNT];
    double last_thermal_capacity_factor[PWR_DCT_CLUTCH_COUNT];

    double last_engine_speed_rad_s;
    double last_output_speed_rad_s;
    double last_input_transmitted_torque_nm;
    double last_output_torque_nm;
    double last_input_power_w;
    double last_output_power_w;
    double last_gear_mesh_loss_power_w;
    double last_instantaneous_efficiency;

    double cumulative_input_energy_j;
    double cumulative_output_energy_j;
    double cumulative_gear_mesh_loss_j;
    double cumulative_heat_rejected_j;
    double time_s;

    uint32_t last_step_diagnostic_flags;
    uint32_t diagnostic_flags;
    uint64_t rejected_command_count;
} pwr_dct;

void pwr_dct_config_default(pwr_dct_config *config);
pwr_dct_result pwr_dct_validate_config(const pwr_dct_config *config);
pwr_dct_clutch pwr_dct_clutch_for_gear(pwr_dct_gear gear);
pwr_dct_gear_mask pwr_dct_gear_bit(pwr_dct_gear gear);
pwr_dct_result pwr_dct_init(pwr_dct *dct, const pwr_dct_config *config);
pwr_dct_result pwr_dct_step(pwr_dct *dct, const pwr_dct_inputs *inputs, double dt_s);
pwr_dct_result pwr_dct_snapshot_read(const pwr_dct *dct, pwr_dct_snapshot *snapshot);
uint64_t pwr_dct_state_hash(const pwr_dct *dct);
const char *pwr_dct_result_string(pwr_dct_result result);

#ifdef __cplusplus
}
#endif

#endif
