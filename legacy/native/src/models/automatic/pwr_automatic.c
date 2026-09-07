#include "pwr_automatic.h"

#include <math.h>
#include <stddef.h>
#include <string.h>

static double pwr_automatic_clamp(double value, double minimum, double maximum) {
    return fmin(fmax(value, minimum), maximum);
}

static bool pwr_automatic_positive_finite(double value) {
    return isfinite(value) && value > 0.0;
}

static bool pwr_automatic_nonnegative_finite(double value) {
    return isfinite(value) && value >= 0.0;
}

static bool pwr_automatic_gear_valid(pwr_automatic_gear gear) {
    return gear == PWR_AUTOMATIC_GEAR_REVERSE || gear == PWR_AUTOMATIC_GEAR_NEUTRAL ||
           gear == PWR_AUTOMATIC_GEAR_FIRST || gear == PWR_AUTOMATIC_GEAR_SECOND ||
           gear == PWR_AUTOMATIC_GEAR_THIRD || gear == PWR_AUTOMATIC_GEAR_FOURTH;
}

static bool pwr_automatic_config_valid(const pwr_automatic_config *config) {
    if (config == NULL || config->step_ns == 0U || config->step_ns > 10000000U) {
        return false;
    }

    for (size_t index = 0U; index < PWR_AUTOMATIC_FORWARD_GEAR_COUNT; ++index) {
        if (!pwr_automatic_positive_finite(config->forward_ratios[index])) {
            return false;
        }
        if (index > 0U && config->forward_ratios[index - 1U] <= config->forward_ratios[index]) {
            return false;
        }
    }

    if (!isfinite(config->reverse_ratio) || config->reverse_ratio >= 0.0) {
        return false;
    }

    return pwr_automatic_positive_finite(config->pump_inertia_kg_m2) &&
           pwr_automatic_positive_finite(config->turbine_inertia_kg_m2) &&
           pwr_automatic_positive_finite(config->output_inertia_kg_m2) &&
           pwr_automatic_nonnegative_finite(config->pump_viscous_drag_nm_s_rad) &&
           pwr_automatic_nonnegative_finite(config->turbine_viscous_drag_nm_s_rad) &&
           pwr_automatic_nonnegative_finite(config->output_viscous_drag_nm_s_rad) &&
           pwr_automatic_positive_finite(config->converter_k_rad_s_sqrt_nm) &&
           isfinite(config->converter_stall_torque_ratio) &&
           config->converter_stall_torque_ratio >= 1.0 &&
           pwr_automatic_positive_finite(config->converter_coupling_speed_ratio) &&
           config->converter_coupling_speed_ratio <= 1.0 &&
           pwr_automatic_positive_finite(config->converter_slip_smoothing_rad_s) &&
           pwr_automatic_positive_finite(config->nominal_line_pressure_pa) &&
           pwr_automatic_positive_finite(config->maximum_line_pressure_pa) &&
           config->maximum_line_pressure_pa >= config->nominal_line_pressure_pa &&
           pwr_automatic_nonnegative_finite(config->minimum_drive_pressure_pa) &&
           config->minimum_drive_pressure_pa <= config->nominal_line_pressure_pa &&
           pwr_automatic_positive_finite(config->pressure_time_constant_s) &&
           pwr_automatic_positive_finite(config->maximum_gear_clutch_torque_nm) &&
           pwr_automatic_positive_finite(config->gear_clutch_slip_smoothing_rad_s) &&
           pwr_automatic_positive_finite(config->nominal_shift_duration_s) &&
           pwr_automatic_positive_finite(config->maximum_shift_duration_s) &&
           config->maximum_shift_duration_s >= config->nominal_shift_duration_s &&
           pwr_automatic_nonnegative_finite(config->shift_torque_hole_fraction) &&
           config->shift_torque_hole_fraction < 1.0 &&
           pwr_automatic_positive_finite(config->maximum_lockup_torque_nm) &&
           pwr_automatic_positive_finite(config->lockup_apply_time_constant_s) &&
           pwr_automatic_positive_finite(config->lockup_release_time_constant_s) &&
           pwr_automatic_positive_finite(config->lockup_slip_smoothing_rad_s) &&
           pwr_automatic_positive_finite(config->ambient_temperature_k) &&
           pwr_automatic_positive_finite(config->initial_oil_temperature_k) &&
           pwr_automatic_positive_finite(config->initial_lockup_temperature_k) &&
           pwr_automatic_positive_finite(config->oil_thermal_capacity_j_k) &&
           pwr_automatic_nonnegative_finite(config->oil_cooling_conductance_w_k) &&
           pwr_automatic_positive_finite(config->lockup_thermal_capacity_j_k) &&
           pwr_automatic_nonnegative_finite(config->lockup_to_oil_conductance_w_k) &&
           pwr_automatic_nonnegative_finite(config->lockup_heat_fraction) &&
           config->lockup_heat_fraction <= 1.0 &&
           pwr_automatic_positive_finite(config->oil_overtemperature_k) &&
           config->oil_overtemperature_k > config->ambient_temperature_k &&
           pwr_automatic_positive_finite(config->lockup_overtemperature_k) &&
           config->lockup_overtemperature_k > config->ambient_temperature_k &&
           pwr_automatic_nonnegative_finite(config->hydraulic_base_loss_w) &&
           pwr_automatic_nonnegative_finite(config->hydraulic_pressure_speed_loss_m3) &&
           pwr_automatic_nonnegative_finite(config->rotating_loss_nm_s_rad) &&
           pwr_automatic_positive_finite(config->ratio_error_threshold_rad_s) &&
           pwr_automatic_nonnegative_finite(config->ratio_error_delay_s) &&
           pwr_automatic_positive_finite(config->lockup_slip_threshold_rad_s) &&
           pwr_automatic_nonnegative_finite(config->lockup_slip_delay_s) &&
           pwr_automatic_positive_finite(config->maximum_input_torque_nm) &&
           pwr_automatic_positive_finite(config->maximum_external_torque_nm) &&
           pwr_automatic_positive_finite(config->maximum_shaft_speed_rad_s);
}

static bool pwr_automatic_input_valid(const pwr_automatic_config *config,
                                      const pwr_automatic_input *input) {
    return input != NULL && pwr_automatic_gear_valid(input->requested_gear) &&
           isfinite(input->engine_torque_nm) &&
           fabs(input->engine_torque_nm) <= config->maximum_input_torque_nm &&
           isfinite(input->output_external_torque_nm) &&
           fabs(input->output_external_torque_nm) <= config->maximum_external_torque_nm &&
           isfinite(input->line_pressure_command_pa) && input->line_pressure_command_pa >= 0.0 &&
           input->line_pressure_command_pa <= config->maximum_line_pressure_pa &&
           isfinite(input->lockup_command) && input->lockup_command >= 0.0 &&
           input->lockup_command <= 1.0;
}

static double pwr_automatic_smoothstep(double progress) {
    const double clamped = pwr_automatic_clamp(progress, 0.0, 1.0);
    return clamped * clamped * (3.0 - 2.0 * clamped);
}

static double pwr_automatic_first_order_alpha(double dt, double time_constant) {
    return 1.0 - exp(-dt / time_constant);
}

void pwr_automatic_config_default(pwr_automatic_config *config) {
    if (config == NULL) {
        return;
    }

    *config = (pwr_automatic_config){
        .step_ns = 100000U,
        .forward_ratios = {2.85, 1.55, 1.00, 0.72},
        .reverse_ratio = -2.40,
        .pump_inertia_kg_m2 = 0.12,
        .turbine_inertia_kg_m2 = 0.08,
        .output_inertia_kg_m2 = 0.80,
        .pump_viscous_drag_nm_s_rad = 0.018,
        .turbine_viscous_drag_nm_s_rad = 0.014,
        .output_viscous_drag_nm_s_rad = 0.18,
        .converter_k_rad_s_sqrt_nm = 20.0,
        .converter_stall_torque_ratio = 2.0,
        .converter_coupling_speed_ratio = 0.90,
        .converter_slip_smoothing_rad_s = 12.0,
        .nominal_line_pressure_pa = 1100000.0,
        .maximum_line_pressure_pa = 1600000.0,
        .minimum_drive_pressure_pa = 550000.0,
        .pressure_time_constant_s = 0.08,
        .maximum_gear_clutch_torque_nm = 280.0,
        .gear_clutch_slip_smoothing_rad_s = 4.0,
        .nominal_shift_duration_s = 0.45,
        .maximum_shift_duration_s = 1.50,
        .shift_torque_hole_fraction = 0.25,
        .maximum_lockup_torque_nm = 300.0,
        .lockup_apply_time_constant_s = 0.25,
        .lockup_release_time_constant_s = 0.08,
        .lockup_slip_smoothing_rad_s = 2.0,
        .ambient_temperature_k = 298.15,
        .initial_oil_temperature_k = 303.15,
        .initial_lockup_temperature_k = 303.15,
        .oil_thermal_capacity_j_k = 30000.0,
        .oil_cooling_conductance_w_k = 60.0,
        .lockup_thermal_capacity_j_k = 1500.0,
        .lockup_to_oil_conductance_w_k = 40.0,
        .lockup_heat_fraction = 0.70,
        .oil_overtemperature_k = 408.15,
        .lockup_overtemperature_k = 473.15,
        .hydraulic_base_loss_w = 80.0,
        .hydraulic_pressure_speed_loss_m3 = 6.0e-8,
        .rotating_loss_nm_s_rad = 0.006,
        .ratio_error_threshold_rad_s = 25.0,
        .ratio_error_delay_s = 0.50,
        .lockup_slip_threshold_rad_s = 15.0,
        .lockup_slip_delay_s = 0.40,
        .maximum_input_torque_nm = 500.0,
        .maximum_external_torque_nm = 2000.0,
        .maximum_shaft_speed_rad_s = 1500.0,
    };
}

pwr_automatic_result pwr_automatic_gear_ratio(const pwr_automatic_config *config,
                                               pwr_automatic_gear gear, double *ratio) {
    if (config == NULL || ratio == NULL) {
        return PWR_AUTOMATIC_INVALID_ARGUMENT;
    }
    if (!pwr_automatic_config_valid(config)) {
        return PWR_AUTOMATIC_INVALID_CONFIG;
    }

    switch (gear) {
        case PWR_AUTOMATIC_GEAR_REVERSE:
            *ratio = config->reverse_ratio;
            return PWR_AUTOMATIC_OK;
        case PWR_AUTOMATIC_GEAR_NEUTRAL:
            *ratio = 0.0;
            return PWR_AUTOMATIC_OK;
        case PWR_AUTOMATIC_GEAR_FIRST:
        case PWR_AUTOMATIC_GEAR_SECOND:
        case PWR_AUTOMATIC_GEAR_THIRD:
        case PWR_AUTOMATIC_GEAR_FOURTH: {
            const size_t index = (size_t)((int)gear - (int)PWR_AUTOMATIC_GEAR_FIRST);
            *ratio = config->forward_ratios[index];
            return PWR_AUTOMATIC_OK;
        }
        default:
            return PWR_AUTOMATIC_INVALID_INPUT;
    }
}

pwr_automatic_result pwr_automatic_converter_evaluate(
    const pwr_automatic_config *config, double pump_speed_rad_s, double turbine_speed_rad_s,
    pwr_automatic_converter_point *point) {
    if (config == NULL || point == NULL) {
        return PWR_AUTOMATIC_INVALID_ARGUMENT;
    }
    if (!pwr_automatic_config_valid(config)) {
        return PWR_AUTOMATIC_INVALID_CONFIG;
    }
    if (!isfinite(pump_speed_rad_s) || !isfinite(turbine_speed_rad_s) ||
        fabs(pump_speed_rad_s) > config->maximum_shaft_speed_rad_s ||
        fabs(turbine_speed_rad_s) > config->maximum_shaft_speed_rad_s) {
        return PWR_AUTOMATIC_INVALID_INPUT;
    }

    const double pump_magnitude = fabs(pump_speed_rad_s);
    const double slip = pump_speed_rad_s - turbine_speed_rad_s;
    const double capacity_torque =
        (pump_magnitude / config->converter_k_rad_s_sqrt_nm) *
        (pump_magnitude / config->converter_k_rad_s_sqrt_nm);
    const double pump_torque =
        capacity_torque * tanh(slip / config->converter_slip_smoothing_rad_s);

    double speed_ratio = 0.0;
    if (pump_magnitude > 1.0e-12) {
        speed_ratio = turbine_speed_rad_s / pump_speed_rad_s;
    }
    const double multiplication_progress =
        pwr_automatic_clamp(speed_ratio / config->converter_coupling_speed_ratio, 0.0, 1.0);
    const double torque_ratio =
        pump_torque >= 0.0
            ? 1.0 + (config->converter_stall_torque_ratio - 1.0) *
                        (1.0 - multiplication_progress) * (1.0 - multiplication_progress)
            : 1.0;
    const double turbine_torque = pump_torque * torque_ratio;
    const double raw_slip_power = pump_torque * pump_speed_rad_s -
                                  turbine_torque * turbine_speed_rad_s;

    *point = (pwr_automatic_converter_point){
        .speed_ratio = speed_ratio,
        .torque_ratio = torque_ratio,
        .pump_torque_nm = pump_torque,
        .turbine_torque_nm = turbine_torque,
        .slip_power_w = fmax(raw_slip_power, 0.0),
    };
    return PWR_AUTOMATIC_OK;
}

pwr_automatic_result pwr_automatic_init(pwr_automatic *automatic,
                                        const pwr_automatic_config *config) {
    if (automatic == NULL || config == NULL) {
        return PWR_AUTOMATIC_INVALID_ARGUMENT;
    }
    if (!pwr_automatic_config_valid(config)) {
        return PWR_AUTOMATIC_INVALID_CONFIG;
    }

    pwr_automatic candidate;
    memset(&candidate, 0, sizeof(candidate));
    candidate.config = *config;
    candidate.active_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
    candidate.shift_from_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
    candidate.target_gear = PWR_AUTOMATIC_GEAR_NEUTRAL;
    candidate.oil_temperature_k = config->initial_oil_temperature_k;
    candidate.lockup_temperature_k = config->initial_lockup_temperature_k;
    *automatic = candidate;
    return PWR_AUTOMATIC_OK;
}

static bool pwr_automatic_state_finite(const pwr_automatic *automatic) {
    return isfinite(automatic->pump_speed_rad_s) && isfinite(automatic->turbine_speed_rad_s) &&
           isfinite(automatic->output_speed_rad_s) && isfinite(automatic->shift_from_ratio) &&
           isfinite(automatic->shift_to_ratio) && isfinite(automatic->shift_progress) &&
           isfinite(automatic->shift_elapsed_s) && isfinite(automatic->settled_elapsed_s) &&
           isfinite(automatic->effective_ratio) && isfinite(automatic->gear_torque_factor) &&
           isfinite(automatic->line_pressure_pa) && isfinite(automatic->lockup_engagement) &&
           isfinite(automatic->lockup_command_elapsed_s) &&
           isfinite(automatic->oil_temperature_k) &&
           isfinite(automatic->lockup_temperature_k) &&
           isfinite(automatic->converter.speed_ratio) &&
           isfinite(automatic->converter.torque_ratio) &&
           isfinite(automatic->converter.pump_torque_nm) &&
           isfinite(automatic->converter.turbine_torque_nm) &&
           isfinite(automatic->converter.slip_power_w) &&
           isfinite(automatic->lockup_torque_nm) &&
           isfinite(automatic->gear_input_torque_nm) &&
           isfinite(automatic->gear_output_torque_nm) &&
           isfinite(automatic->gear_clutch_slip_rad_s) &&
           isfinite(automatic->converter_loss_w) && isfinite(automatic->lockup_loss_w) &&
           isfinite(automatic->gear_clutch_loss_w) &&
           isfinite(automatic->hydraulic_loss_w) && isfinite(automatic->rotating_loss_w) &&
           isfinite(automatic->accumulated_loss_j) &&
           isfinite(automatic->engine_reaction_torque_nm) &&
           isfinite(automatic->pump_boundary_power_w) &&
           isfinite(automatic->pump_boundary_energy_j) &&
           isfinite(automatic->coupling_adjustment_j);
}

static void pwr_automatic_begin_shift(pwr_automatic *automatic,
                                      pwr_automatic_gear requested_gear, double requested_ratio) {
    automatic->shift_from_gear = automatic->active_gear;
    automatic->target_gear = requested_gear;
    automatic->shift_from_ratio = automatic->effective_ratio;
    automatic->shift_to_ratio = requested_ratio;
    automatic->shift_progress = 0.0;
    automatic->shift_elapsed_s = 0.0;
    automatic->settled_elapsed_s = 0.0;
    automatic->shift_active = true;
}

static void pwr_automatic_update_shift(pwr_automatic *automatic, double dt) {
    const pwr_automatic_config *config = &automatic->config;
    if (!automatic->shift_active) {
        automatic->effective_ratio = automatic->shift_to_ratio;
        automatic->gear_torque_factor =
            automatic->active_gear == PWR_AUTOMATIC_GEAR_NEUTRAL ? 0.0 : 1.0;
        automatic->settled_elapsed_s += dt;
        return;
    }

    automatic->shift_elapsed_s += dt;
    const double pressure_authority = pwr_automatic_clamp(
        automatic->line_pressure_pa / config->nominal_line_pressure_pa, 0.0, 1.25);
    automatic->shift_progress = pwr_automatic_clamp(
        automatic->shift_progress + dt * pressure_authority / config->nominal_shift_duration_s,
        0.0, 1.0);
    const double blend = pwr_automatic_smoothstep(automatic->shift_progress);
    automatic->effective_ratio =
        automatic->shift_from_ratio + (automatic->shift_to_ratio - automatic->shift_from_ratio) * blend;

    if (fabs(automatic->shift_from_ratio) < 1.0e-12) {
        automatic->gear_torque_factor = blend;
    } else if (fabs(automatic->shift_to_ratio) < 1.0e-12) {
        automatic->gear_torque_factor = 1.0 - blend;
    } else {
        automatic->gear_torque_factor =
            1.0 - config->shift_torque_hole_fraction * 4.0 * blend * (1.0 - blend);
    }

    if (automatic->shift_progress >= 1.0) {
        automatic->active_gear = automatic->target_gear;
        automatic->shift_from_gear = automatic->target_gear;
        automatic->shift_from_ratio = automatic->shift_to_ratio;
        automatic->effective_ratio = automatic->shift_to_ratio;
        automatic->gear_torque_factor =
            automatic->active_gear == PWR_AUTOMATIC_GEAR_NEUTRAL ? 0.0 : 1.0;
        automatic->shift_active = false;
        automatic->settled_elapsed_s = 0.0;
    }
}

static void pwr_automatic_limit_speed(double *speed, double maximum, uint32_t *diagnostics) {
    if (fabs(*speed) > maximum) {
        *speed = pwr_automatic_clamp(*speed, -maximum, maximum);
        *diagnostics |= PWR_AUTOMATIC_DIAG_OVERSPEED | PWR_AUTOMATIC_DIAG_NUMERIC_LIMIT;
    }
}

static pwr_automatic_result pwr_automatic_step_internal(
    pwr_automatic *automatic, const pwr_automatic_input *input, bool impose_pump_speed,
    double imposed_pump_speed_rad_s, pwr_automatic_coupling_output *coupling_output) {
    if (automatic == NULL || input == NULL) {
        return PWR_AUTOMATIC_INVALID_ARGUMENT;
    }
    if (!pwr_automatic_config_valid(&automatic->config)) {
        return PWR_AUTOMATIC_INVALID_CONFIG;
    }
    if (!pwr_automatic_input_valid(&automatic->config, input)) {
        return PWR_AUTOMATIC_INVALID_INPUT;
    }
    if (impose_pump_speed &&
        (!isfinite(imposed_pump_speed_rad_s) || imposed_pump_speed_rad_s < 0.0 ||
         imposed_pump_speed_rad_s > automatic->config.maximum_shaft_speed_rad_s)) {
        return PWR_AUTOMATIC_INVALID_INPUT;
    }
    if (automatic->tick == UINT64_MAX || !pwr_automatic_state_finite(automatic)) {
        return PWR_AUTOMATIC_NUMERIC_ERROR;
    }

    pwr_automatic next = *automatic;
    const pwr_automatic_config *config = &next.config;
    const double dt = (double)config->step_ns * 1.0e-9;
    double projection_adjustment_j = 0.0;
    next.pump_speed_imposed = impose_pump_speed;
    next.pump_boundary_power_w = 0.0;
    if (impose_pump_speed) {
        projection_adjustment_j =
            0.5 * config->pump_inertia_kg_m2 *
            (imposed_pump_speed_rad_s * imposed_pump_speed_rad_s -
             next.pump_speed_rad_s * next.pump_speed_rad_s);
        if (imposed_pump_speed_rad_s != next.pump_speed_rad_s) {
            next.diagnostic_flags |= PWR_AUTOMATIC_DIAG_COUPLING_SPEED_PROJECTED;
        }
        next.coupling_adjustment_j += projection_adjustment_j;
        next.pump_speed_rad_s = imposed_pump_speed_rad_s;
    }

    double requested_ratio = 0.0;
    const pwr_automatic_result ratio_result =
        pwr_automatic_gear_ratio(config, input->requested_gear, &requested_ratio);
    if (ratio_result != PWR_AUTOMATIC_OK) {
        return ratio_result;
    }

    if (input->requested_gear != next.target_gear) {
        pwr_automatic_begin_shift(&next, input->requested_gear, requested_ratio);
    }

    const double pressure_alpha =
        pwr_automatic_first_order_alpha(dt, config->pressure_time_constant_s);
    next.line_pressure_pa +=
        (input->line_pressure_command_pa - next.line_pressure_pa) * pressure_alpha;
    pwr_automatic_update_shift(&next, dt);

    const double permitted_lockup_command =
        (next.shift_active || next.target_gear == PWR_AUTOMATIC_GEAR_NEUTRAL)
            ? 0.0
            : input->lockup_command;
    const double lockup_time_constant =
        permitted_lockup_command >= next.lockup_engagement
            ? config->lockup_apply_time_constant_s
            : config->lockup_release_time_constant_s;
    const double lockup_alpha = pwr_automatic_first_order_alpha(dt, lockup_time_constant);
    next.lockup_engagement +=
        (permitted_lockup_command - next.lockup_engagement) * lockup_alpha;
    next.lockup_engagement = pwr_automatic_clamp(next.lockup_engagement, 0.0, 1.0);

    if (input->lockup_command >= 0.8 && !next.shift_active) {
        next.lockup_command_elapsed_s += dt;
    } else {
        next.lockup_command_elapsed_s = 0.0;
    }

    pwr_automatic_converter_point converter;
    const pwr_automatic_result converter_result = pwr_automatic_converter_evaluate(
        config, next.pump_speed_rad_s, next.turbine_speed_rad_s, &converter);
    if (converter_result != PWR_AUTOMATIC_OK) {
        return converter_result == PWR_AUTOMATIC_INVALID_INPUT ? PWR_AUTOMATIC_NUMERIC_ERROR
                                                               : converter_result;
    }
    next.converter = converter;

    const double pressure_fraction = pwr_automatic_clamp(
        next.line_pressure_pa / config->nominal_line_pressure_pa, 0.0,
        config->maximum_line_pressure_pa / config->nominal_line_pressure_pa);
    const double lockup_slip = next.pump_speed_rad_s - next.turbine_speed_rad_s;
    const double lockup_capacity = config->maximum_lockup_torque_nm * pressure_fraction *
                                   next.lockup_engagement;
    next.lockup_torque_nm =
        lockup_capacity * tanh(lockup_slip / config->lockup_slip_smoothing_rad_s);

    next.gear_clutch_slip_rad_s =
        next.turbine_speed_rad_s - next.effective_ratio * next.output_speed_rad_s;
    const double gear_capacity = config->maximum_gear_clutch_torque_nm * pressure_fraction *
                                 next.gear_torque_factor;
    next.gear_input_torque_nm =
        gear_capacity * tanh(next.gear_clutch_slip_rad_s /
                             config->gear_clutch_slip_smoothing_rad_s);
    next.gear_output_torque_nm = next.gear_input_torque_nm * next.effective_ratio;

    next.converter_loss_w = converter.slip_power_w;
    next.lockup_loss_w = fmax(next.lockup_torque_nm * lockup_slip, 0.0);
    next.gear_clutch_loss_w =
        fmax(next.gear_input_torque_nm * next.gear_clutch_slip_rad_s, 0.0);

    const double pump_sign = tanh(next.pump_speed_rad_s / 5.0);
    const double hydraulic_drag_torque =
        pump_sign * (config->hydraulic_base_loss_w /
                         (fabs(next.pump_speed_rad_s) + 20.0) +
                     next.line_pressure_pa * config->hydraulic_pressure_speed_loss_m3);
    next.hydraulic_loss_w = fabs(hydraulic_drag_torque * next.pump_speed_rad_s);

    const double pump_drag_coefficient =
        config->pump_viscous_drag_nm_s_rad + config->rotating_loss_nm_s_rad;
    const double turbine_drag_coefficient =
        config->turbine_viscous_drag_nm_s_rad + config->rotating_loss_nm_s_rad;
    const double output_drag_coefficient =
        config->output_viscous_drag_nm_s_rad + config->rotating_loss_nm_s_rad;
    next.rotating_loss_w =
        pump_drag_coefficient * next.pump_speed_rad_s * next.pump_speed_rad_s +
        turbine_drag_coefficient * next.turbine_speed_rad_s * next.turbine_speed_rad_s +
        output_drag_coefficient * next.output_speed_rad_s * next.output_speed_rad_s;

    const double pump_load_torque_nm =
        converter.pump_torque_nm + next.lockup_torque_nm + hydraulic_drag_torque +
        pump_drag_coefficient * next.pump_speed_rad_s;
    next.engine_reaction_torque_nm = -pump_load_torque_nm;
    if (impose_pump_speed) {
        next.pump_boundary_power_w = pump_load_torque_nm * next.pump_speed_rad_s;
        next.pump_boundary_energy_j += next.pump_boundary_power_w * dt;
    }

    const double pump_acceleration =
        (input->engine_torque_nm - pump_load_torque_nm) /
        config->pump_inertia_kg_m2;
    const double turbine_acceleration =
        (converter.turbine_torque_nm + next.lockup_torque_nm - next.gear_input_torque_nm -
         turbine_drag_coefficient * next.turbine_speed_rad_s) /
        config->turbine_inertia_kg_m2;
    const double output_acceleration =
        (next.gear_output_torque_nm + input->output_external_torque_nm -
         output_drag_coefficient * next.output_speed_rad_s) /
        config->output_inertia_kg_m2;

    if (!impose_pump_speed) {
        next.pump_speed_rad_s += pump_acceleration * dt;
    }
    next.turbine_speed_rad_s += turbine_acceleration * dt;
    next.output_speed_rad_s += output_acceleration * dt;
    if (!impose_pump_speed) {
        if (next.pump_speed_rad_s < 0.0) {
            next.pump_speed_rad_s = 0.0;
            next.diagnostic_flags |= PWR_AUTOMATIC_DIAG_NUMERIC_LIMIT;
        }
        pwr_automatic_limit_speed(&next.pump_speed_rad_s, config->maximum_shaft_speed_rad_s,
                                  &next.diagnostic_flags);
    }
    pwr_automatic_limit_speed(&next.turbine_speed_rad_s, config->maximum_shaft_speed_rad_s,
                              &next.diagnostic_flags);
    pwr_automatic_limit_speed(&next.output_speed_rad_s, config->maximum_shaft_speed_rad_s,
                              &next.diagnostic_flags);

    const double lockup_to_oil_w = config->lockup_to_oil_conductance_w_k *
                                   (next.lockup_temperature_k - next.oil_temperature_k);
    const double oil_cooling_w = config->oil_cooling_conductance_w_k *
                                 (next.oil_temperature_k - config->ambient_temperature_k);
    const double lockup_heat_w = config->lockup_heat_fraction * next.lockup_loss_w;
    const double oil_heat_w = next.converter_loss_w + next.gear_clutch_loss_w +
                              next.hydraulic_loss_w + next.rotating_loss_w +
                              (1.0 - config->lockup_heat_fraction) * next.lockup_loss_w +
                              lockup_to_oil_w;
    next.lockup_temperature_k +=
        (lockup_heat_w - lockup_to_oil_w) * dt / config->lockup_thermal_capacity_j_k;
    next.oil_temperature_k +=
        (oil_heat_w - oil_cooling_w) * dt / config->oil_thermal_capacity_j_k;
    next.accumulated_loss_j +=
        (next.converter_loss_w + next.lockup_loss_w + next.gear_clutch_loss_w +
         next.hydraulic_loss_w + next.rotating_loss_w) *
        dt;

    if (input->requested_gear != PWR_AUTOMATIC_GEAR_NEUTRAL &&
        (input->line_pressure_command_pa < config->minimum_drive_pressure_pa ||
         (!next.shift_active && next.settled_elapsed_s >= config->ratio_error_delay_s &&
          next.line_pressure_pa < config->minimum_drive_pressure_pa))) {
        next.diagnostic_flags |= PWR_AUTOMATIC_DIAG_LOW_LINE_PRESSURE;
    }
    if (next.shift_active && next.shift_elapsed_s > config->maximum_shift_duration_s) {
        next.diagnostic_flags |= PWR_AUTOMATIC_DIAG_SHIFT_TIMEOUT;
    }
    if (!next.shift_active && next.active_gear != PWR_AUTOMATIC_GEAR_NEUTRAL &&
        next.settled_elapsed_s >= config->ratio_error_delay_s &&
        fabs(next.gear_clutch_slip_rad_s) > config->ratio_error_threshold_rad_s) {
        next.diagnostic_flags |= PWR_AUTOMATIC_DIAG_EXCESSIVE_RATIO_ERROR;
    }
    if (next.lockup_command_elapsed_s >= config->lockup_slip_delay_s &&
        next.lockup_engagement >= 0.8 && fabs(lockup_slip) > config->lockup_slip_threshold_rad_s) {
        next.diagnostic_flags |= PWR_AUTOMATIC_DIAG_EXCESSIVE_LOCKUP_SLIP;
    }
    if (next.oil_temperature_k >= config->oil_overtemperature_k) {
        next.diagnostic_flags |= PWR_AUTOMATIC_DIAG_OIL_OVERTEMPERATURE;
    }
    if (next.lockup_temperature_k >= config->lockup_overtemperature_k) {
        next.diagnostic_flags |= PWR_AUTOMATIC_DIAG_LOCKUP_OVERTEMPERATURE;
    }

    ++next.tick;
    if (!pwr_automatic_state_finite(&next)) {
        return PWR_AUTOMATIC_NUMERIC_ERROR;
    }
    *automatic = next;
    if (coupling_output != NULL) {
        *coupling_output = (pwr_automatic_coupling_output){
            .engine_reaction_torque_nm = next.engine_reaction_torque_nm,
            .pump_load_torque_nm = -next.engine_reaction_torque_nm,
            .boundary_power_w = next.pump_boundary_power_w,
            .boundary_energy_j = next.pump_boundary_energy_j,
            .projection_adjustment_j = projection_adjustment_j,
            .accumulated_projection_adjustment_j = next.coupling_adjustment_j,
        };
    }
    return PWR_AUTOMATIC_OK;
}

pwr_automatic_result pwr_automatic_step(pwr_automatic *automatic,
                                        const pwr_automatic_input *input) {
    return pwr_automatic_step_internal(automatic, input, false, 0.0, NULL);
}

pwr_automatic_result pwr_automatic_step_coupled(
    pwr_automatic *automatic, const pwr_automatic_coupled_input *input,
    pwr_automatic_coupling_output *coupling_output) {
    if (automatic == NULL || input == NULL || coupling_output == NULL) {
        return PWR_AUTOMATIC_INVALID_ARGUMENT;
    }

    const pwr_automatic_input internal_input = {
        .engine_torque_nm = 0.0,
        .output_external_torque_nm = input->output_external_torque_nm,
        .line_pressure_command_pa = input->line_pressure_command_pa,
        .lockup_command = input->lockup_command,
        .requested_gear = input->requested_gear,
    };
    pwr_automatic_coupling_output candidate_output;
    const pwr_automatic_result result = pwr_automatic_step_internal(
        automatic, &internal_input, true, input->pump_speed_rad_s, &candidate_output);
    if (result == PWR_AUTOMATIC_OK) {
        *coupling_output = candidate_output;
    }
    return result;
}

static uint64_t pwr_automatic_hash_bytes(uint64_t hash, const void *data, size_t size) {
    const unsigned char *bytes = data;
    for (size_t index = 0U; index < size; ++index) {
        hash ^= (uint64_t)bytes[index];
        hash *= UINT64_C(1099511628211);
    }
    return hash;
}

static uint64_t pwr_automatic_hash_double(uint64_t hash, double value) {
    uint64_t bits = 0U;
    memcpy(&bits, &value, sizeof(bits));
    return pwr_automatic_hash_bytes(hash, &bits, sizeof(bits));
}

uint64_t pwr_automatic_state_hash(const pwr_automatic *automatic) {
    if (automatic == NULL) {
        return 0U;
    }

    uint64_t hash = UINT64_C(1469598103934665603);
    hash = pwr_automatic_hash_bytes(hash, &automatic->tick, sizeof(automatic->tick));
    hash = pwr_automatic_hash_double(hash, automatic->pump_speed_rad_s);
    hash = pwr_automatic_hash_double(hash, automatic->turbine_speed_rad_s);
    hash = pwr_automatic_hash_double(hash, automatic->output_speed_rad_s);
    hash = pwr_automatic_hash_bytes(hash, &automatic->active_gear,
                                    sizeof(automatic->active_gear));
    hash = pwr_automatic_hash_bytes(hash, &automatic->target_gear,
                                    sizeof(automatic->target_gear));
    hash = pwr_automatic_hash_bytes(hash, &automatic->shift_active,
                                    sizeof(automatic->shift_active));
    hash = pwr_automatic_hash_double(hash, automatic->shift_from_ratio);
    hash = pwr_automatic_hash_double(hash, automatic->shift_to_ratio);
    hash = pwr_automatic_hash_double(hash, automatic->shift_progress);
    hash = pwr_automatic_hash_double(hash, automatic->shift_elapsed_s);
    hash = pwr_automatic_hash_double(hash, automatic->settled_elapsed_s);
    hash = pwr_automatic_hash_double(hash, automatic->effective_ratio);
    hash = pwr_automatic_hash_double(hash, automatic->gear_torque_factor);
    hash = pwr_automatic_hash_double(hash, automatic->line_pressure_pa);
    hash = pwr_automatic_hash_double(hash, automatic->lockup_engagement);
    hash = pwr_automatic_hash_double(hash, automatic->lockup_command_elapsed_s);
    hash = pwr_automatic_hash_double(hash, automatic->oil_temperature_k);
    hash = pwr_automatic_hash_double(hash, automatic->lockup_temperature_k);
    hash = pwr_automatic_hash_double(hash, automatic->converter.pump_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic->converter.turbine_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic->lockup_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic->gear_input_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic->gear_output_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic->gear_clutch_slip_rad_s);
    hash = pwr_automatic_hash_double(hash, automatic->accumulated_loss_j);
    hash = pwr_automatic_hash_bytes(hash, &automatic->pump_speed_imposed,
                                    sizeof(automatic->pump_speed_imposed));
    hash = pwr_automatic_hash_double(hash, automatic->engine_reaction_torque_nm);
    hash = pwr_automatic_hash_double(hash, automatic->pump_boundary_power_w);
    hash = pwr_automatic_hash_double(hash, automatic->pump_boundary_energy_j);
    hash = pwr_automatic_hash_double(hash, automatic->coupling_adjustment_j);
    hash = pwr_automatic_hash_bytes(hash, &automatic->diagnostic_flags,
                                    sizeof(automatic->diagnostic_flags));
    return hash;
}

pwr_automatic_result pwr_automatic_snapshot_read(const pwr_automatic *automatic,
                                                 pwr_automatic_snapshot *snapshot) {
    if (automatic == NULL || snapshot == NULL) {
        return PWR_AUTOMATIC_INVALID_ARGUMENT;
    }
    if (!pwr_automatic_config_valid(&automatic->config)) {
        return PWR_AUTOMATIC_INVALID_CONFIG;
    }
    if (!pwr_automatic_state_finite(automatic)) {
        return PWR_AUTOMATIC_NUMERIC_ERROR;
    }

    *snapshot = (pwr_automatic_snapshot){
        .time_s = (double)automatic->tick * (double)automatic->config.step_ns * 1.0e-9,
        .pump_speed_rad_s = automatic->pump_speed_rad_s,
        .turbine_speed_rad_s = automatic->turbine_speed_rad_s,
        .output_speed_rad_s = automatic->output_speed_rad_s,
        .active_gear = automatic->active_gear,
        .target_gear = automatic->target_gear,
        .shift_active = automatic->shift_active,
        .shift_progress = automatic->shift_progress,
        .effective_ratio = automatic->effective_ratio,
        .gear_torque_factor = automatic->gear_torque_factor,
        .line_pressure_pa = automatic->line_pressure_pa,
        .lockup_engagement = automatic->lockup_engagement,
        .converter_speed_ratio = automatic->converter.speed_ratio,
        .converter_torque_ratio = automatic->converter.torque_ratio,
        .converter_pump_torque_nm = automatic->converter.pump_torque_nm,
        .converter_turbine_torque_nm = automatic->converter.turbine_torque_nm,
        .lockup_torque_nm = automatic->lockup_torque_nm,
        .gear_input_torque_nm = automatic->gear_input_torque_nm,
        .gear_output_torque_nm = automatic->gear_output_torque_nm,
        .converter_slip_rad_s = automatic->pump_speed_rad_s - automatic->turbine_speed_rad_s,
        .lockup_slip_rad_s = automatic->pump_speed_rad_s - automatic->turbine_speed_rad_s,
        .gear_clutch_slip_rad_s = automatic->gear_clutch_slip_rad_s,
        .converter_loss_w = automatic->converter_loss_w,
        .lockup_loss_w = automatic->lockup_loss_w,
        .gear_clutch_loss_w = automatic->gear_clutch_loss_w,
        .hydraulic_loss_w = automatic->hydraulic_loss_w,
        .rotating_loss_w = automatic->rotating_loss_w,
        .oil_temperature_k = automatic->oil_temperature_k,
        .lockup_temperature_k = automatic->lockup_temperature_k,
        .accumulated_loss_j = automatic->accumulated_loss_j,
        .pump_speed_imposed = automatic->pump_speed_imposed,
        .engine_reaction_torque_nm = automatic->engine_reaction_torque_nm,
        .pump_boundary_power_w = automatic->pump_boundary_power_w,
        .pump_boundary_energy_j = automatic->pump_boundary_energy_j,
        .coupling_adjustment_j = automatic->coupling_adjustment_j,
        .diagnostic_flags = automatic->diagnostic_flags,
        .state_hash = pwr_automatic_state_hash(automatic),
    };
    return PWR_AUTOMATIC_OK;
}
