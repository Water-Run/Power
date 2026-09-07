#ifndef PWR_MODELS_AUTOMATIC_H
#define PWR_MODELS_AUTOMATIC_H

#include <stdbool.h>
#include <stdint.h>

/*
 * Generic transverse hydraulic automatic-transmission dynamics.
 *
 * The default calibration is intentionally illustrative.  In particular, the
 * ratios and controller constants are not claimed to be PSA AT8 production
 * data.  A sourced vehicle asset is expected to replace every calibration
 * value while retaining this state and port contract.
 */

#define PWR_AUTOMATIC_FORWARD_GEAR_COUNT 4U

typedef enum pwr_automatic_result {
    PWR_AUTOMATIC_OK = 0,
    PWR_AUTOMATIC_INVALID_ARGUMENT,
    PWR_AUTOMATIC_INVALID_CONFIG,
    PWR_AUTOMATIC_INVALID_INPUT,
    PWR_AUTOMATIC_NUMERIC_ERROR
} pwr_automatic_result;

typedef enum pwr_automatic_gear {
    PWR_AUTOMATIC_GEAR_REVERSE = -1,
    PWR_AUTOMATIC_GEAR_NEUTRAL = 0,
    PWR_AUTOMATIC_GEAR_FIRST = 1,
    PWR_AUTOMATIC_GEAR_SECOND = 2,
    PWR_AUTOMATIC_GEAR_THIRD = 3,
    PWR_AUTOMATIC_GEAR_FOURTH = 4
} pwr_automatic_gear;

typedef enum pwr_automatic_diagnostic {
    PWR_AUTOMATIC_DIAG_NONE = 0U,
    PWR_AUTOMATIC_DIAG_LOW_LINE_PRESSURE = 1U << 0U,
    PWR_AUTOMATIC_DIAG_SHIFT_TIMEOUT = 1U << 1U,
    PWR_AUTOMATIC_DIAG_EXCESSIVE_RATIO_ERROR = 1U << 2U,
    PWR_AUTOMATIC_DIAG_EXCESSIVE_LOCKUP_SLIP = 1U << 3U,
    PWR_AUTOMATIC_DIAG_OIL_OVERTEMPERATURE = 1U << 4U,
    PWR_AUTOMATIC_DIAG_LOCKUP_OVERTEMPERATURE = 1U << 5U,
    PWR_AUTOMATIC_DIAG_OVERSPEED = 1U << 6U,
    PWR_AUTOMATIC_DIAG_NUMERIC_LIMIT = 1U << 7U,
    /* Informational: an external solver projected the pump-speed boundary. */
    PWR_AUTOMATIC_DIAG_COUPLING_SPEED_PROJECTED = 1U << 8U
} pwr_automatic_diagnostic;

typedef struct pwr_automatic_config {
    uint64_t step_ns;

    /* Signed input-speed/output-speed ratios. */
    double forward_ratios[PWR_AUTOMATIC_FORWARD_GEAR_COUNT];
    double reverse_ratio;

    double pump_inertia_kg_m2;
    double turbine_inertia_kg_m2;
    double output_inertia_kg_m2;
    double pump_viscous_drag_nm_s_rad;
    double turbine_viscous_drag_nm_s_rad;
    double output_viscous_drag_nm_s_rad;

    /* K has units rad/s/sqrt(N*m): pump torque is approximately (speed/K)^2. */
    double converter_k_rad_s_sqrt_nm;
    double converter_stall_torque_ratio;
    double converter_coupling_speed_ratio;
    double converter_slip_smoothing_rad_s;

    double nominal_line_pressure_pa;
    double maximum_line_pressure_pa;
    double minimum_drive_pressure_pa;
    double pressure_time_constant_s;
    double maximum_gear_clutch_torque_nm;
    double gear_clutch_slip_smoothing_rad_s;
    double nominal_shift_duration_s;
    double maximum_shift_duration_s;
    double shift_torque_hole_fraction;

    double maximum_lockup_torque_nm;
    double lockup_apply_time_constant_s;
    double lockup_release_time_constant_s;
    double lockup_slip_smoothing_rad_s;

    double ambient_temperature_k;
    double initial_oil_temperature_k;
    double initial_lockup_temperature_k;
    double oil_thermal_capacity_j_k;
    double oil_cooling_conductance_w_k;
    double lockup_thermal_capacity_j_k;
    double lockup_to_oil_conductance_w_k;
    double lockup_heat_fraction;
    double oil_overtemperature_k;
    double lockup_overtemperature_k;

    double hydraulic_base_loss_w;
    double hydraulic_pressure_speed_loss_m3;
    double rotating_loss_nm_s_rad;

    double ratio_error_threshold_rad_s;
    double ratio_error_delay_s;
    double lockup_slip_threshold_rad_s;
    double lockup_slip_delay_s;
    double maximum_input_torque_nm;
    double maximum_external_torque_nm;
    double maximum_shaft_speed_rad_s;
} pwr_automatic_config;

typedef struct pwr_automatic_input {
    /* Torques use the positive pump/output rotation convention. */
    double engine_torque_nm;
    double output_external_torque_nm;
    double line_pressure_command_pa;
    double lockup_command;
    pwr_automatic_gear requested_gear;
} pwr_automatic_input;

typedef struct pwr_automatic_coupled_input {
    /* Prescribed crank/pump speed for this complete fixed step. */
    double pump_speed_rad_s;
    double output_external_torque_nm;
    double line_pressure_command_pa;
    double lockup_command;
    pwr_automatic_gear requested_gear;
} pwr_automatic_coupled_input;

typedef struct pwr_automatic_coupling_output {
    /* Signed torque exerted by the transmission on the upstream engine shaft. */
    double engine_reaction_torque_nm;
    /* Opposite sign: torque the upstream solver supplies to hold the boundary. */
    double pump_load_torque_nm;
    /* Power entering this transmission through the imposed-speed boundary. */
    double boundary_power_w;
    double boundary_energy_j;
    /* Pump-inertia energy removed/introduced by this step's speed projection. */
    double projection_adjustment_j;
    double accumulated_projection_adjustment_j;
} pwr_automatic_coupling_output;

typedef struct pwr_automatic_converter_point {
    double speed_ratio;
    double torque_ratio;
    double pump_torque_nm;
    double turbine_torque_nm;
    double slip_power_w;
} pwr_automatic_converter_point;

typedef struct pwr_automatic_snapshot {
    double time_s;
    double pump_speed_rad_s;
    double turbine_speed_rad_s;
    double output_speed_rad_s;

    pwr_automatic_gear active_gear;
    pwr_automatic_gear target_gear;
    bool shift_active;
    double shift_progress;
    double effective_ratio;
    double gear_torque_factor;

    double line_pressure_pa;
    double lockup_engagement;
    double converter_speed_ratio;
    double converter_torque_ratio;
    double converter_pump_torque_nm;
    double converter_turbine_torque_nm;
    double lockup_torque_nm;
    double gear_input_torque_nm;
    double gear_output_torque_nm;
    double converter_slip_rad_s;
    double lockup_slip_rad_s;
    double gear_clutch_slip_rad_s;

    double converter_loss_w;
    double lockup_loss_w;
    double gear_clutch_loss_w;
    double hydraulic_loss_w;
    double rotating_loss_w;
    double oil_temperature_k;
    double lockup_temperature_k;
    double accumulated_loss_j;

    bool pump_speed_imposed;
    double engine_reaction_torque_nm;
    double pump_boundary_power_w;
    double pump_boundary_energy_j;
    double coupling_adjustment_j;

    uint32_t diagnostic_flags;
    uint64_t state_hash;
} pwr_automatic_snapshot;

typedef struct pwr_automatic {
    pwr_automatic_config config;
    uint64_t tick;

    double pump_speed_rad_s;
    double turbine_speed_rad_s;
    double output_speed_rad_s;

    pwr_automatic_gear active_gear;
    pwr_automatic_gear shift_from_gear;
    pwr_automatic_gear target_gear;
    bool shift_active;
    double shift_from_ratio;
    double shift_to_ratio;
    double shift_progress;
    double shift_elapsed_s;
    double settled_elapsed_s;
    double effective_ratio;
    double gear_torque_factor;

    double line_pressure_pa;
    double lockup_engagement;
    double lockup_command_elapsed_s;
    double oil_temperature_k;
    double lockup_temperature_k;

    pwr_automatic_converter_point converter;
    double lockup_torque_nm;
    double gear_input_torque_nm;
    double gear_output_torque_nm;
    double gear_clutch_slip_rad_s;
    double converter_loss_w;
    double lockup_loss_w;
    double gear_clutch_loss_w;
    double hydraulic_loss_w;
    double rotating_loss_w;
    double accumulated_loss_j;

    bool pump_speed_imposed;
    double engine_reaction_torque_nm;
    double pump_boundary_power_w;
    double pump_boundary_energy_j;
    double coupling_adjustment_j;

    uint32_t diagnostic_flags;
} pwr_automatic;

void pwr_automatic_config_default(pwr_automatic_config *config);
/* Returns the configured signed ratio without changing either argument. */
pwr_automatic_result pwr_automatic_gear_ratio(const pwr_automatic_config *config,
                                               pwr_automatic_gear gear, double *ratio);
/* Pure converter map evaluation, useful to a future coupled graph solver. */
pwr_automatic_result pwr_automatic_converter_evaluate(
    const pwr_automatic_config *config, double pump_speed_rad_s, double turbine_speed_rad_s,
    pwr_automatic_converter_point *point);
/* Init and step commit no bytes to the destination when validation fails. */
pwr_automatic_result pwr_automatic_init(pwr_automatic *automatic,
                                        const pwr_automatic_config *config);
/* Advances exactly config.step_ns; the complete input frame is transactional. */
pwr_automatic_result pwr_automatic_step(pwr_automatic *automatic,
                                        const pwr_automatic_input *input);
/*
 * Imposes pump speed for the complete step and does not integrate pump inertia.
 * Both state and coupling_output remain unchanged when validation fails.
 */
pwr_automatic_result pwr_automatic_step_coupled(
    pwr_automatic *automatic, const pwr_automatic_coupled_input *input,
    pwr_automatic_coupling_output *coupling_output);
pwr_automatic_result pwr_automatic_snapshot_read(const pwr_automatic *automatic,
                                                 pwr_automatic_snapshot *snapshot);
uint64_t pwr_automatic_state_hash(const pwr_automatic *automatic);

#endif
