// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#include "pwr_dct.h"

#include <math.h>
#include <stddef.h>
#include <string.h>

static bool pwr_dct_positive_finite(double value) {
    return isfinite(value) && value > 0.0;
}

static double pwr_dct_clamp(double value, double minimum, double maximum) {
    return fmin(fmax(value, minimum), maximum);
}

static double pwr_dct_approach(double current, double target, double rise_rate,
                               double fall_rate, double dt_s) {
    const double maximum_delta = (target >= current ? rise_rate : fall_rate) * dt_s;
    return current + pwr_dct_clamp(target - current, -maximum_delta, maximum_delta);
}

static double pwr_dct_capacity_factor(const pwr_dct_config *config, size_t clutch_index,
                                      double temperature_k) {
    const double fade_start = config->fade_start_temperature_k[clutch_index];
    const double fade_full = config->fade_full_temperature_k[clutch_index];
    const double minimum = config->minimum_fade_capacity_fraction[clutch_index];
    if (temperature_k <= fade_start) {
        return 1.0;
    }
    if (temperature_k >= fade_full) {
        return minimum;
    }
    const double progress = (temperature_k - fade_start) / (fade_full - fade_start);
    return 1.0 - progress * (1.0 - minimum);
}

static bool pwr_dct_clutch_parameters_valid(const pwr_dct_config *config, size_t index) {
    return pwr_dct_positive_finite(config->clutch_nominal_capacity_nm[index]) &&
           pwr_dct_positive_finite(config->clamp_engagement_rate_per_s[index]) &&
           pwr_dct_positive_finite(config->clamp_release_rate_per_s[index]) &&
           pwr_dct_positive_finite(config->initial_clutch_temperature_k[index]) &&
           pwr_dct_positive_finite(config->clutch_heat_capacity_j_per_k[index]) &&
           isfinite(config->clutch_ambient_conductance_w_per_k[index]) &&
           config->clutch_ambient_conductance_w_per_k[index] >= 0.0 &&
           pwr_dct_positive_finite(config->fade_start_temperature_k[index]) &&
           pwr_dct_positive_finite(config->fade_full_temperature_k[index]) &&
           config->fade_full_temperature_k[index] >
               config->fade_start_temperature_k[index] &&
           pwr_dct_positive_finite(config->minimum_fade_capacity_fraction[index]) &&
           config->minimum_fade_capacity_fraction[index] <= 1.0 &&
           pwr_dct_positive_finite(config->overheat_temperature_k[index]) &&
           config->overheat_temperature_k[index] >=
               config->fade_start_temperature_k[index];
}

void pwr_dct_config_default(pwr_dct_config *config) {
    if (config == NULL) {
        return;
    }

    *config = (pwr_dct_config){
        /* Gear ratios are mandatory caller-provided calibration data. */
        .gear_ratio = {0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0},
        .mesh_efficiency = {0.97, 0.97, 0.97, 0.97, 0.97, 0.97, 0.97, 0.97},
        .final_drive_ratio = 1.0,
        .clutch_nominal_capacity_nm = {250.0, 250.0},
        .slip_regularization_speed_rad_s = 0.25,
        .clamp_engagement_rate_per_s = {8.0, 8.0},
        .clamp_release_rate_per_s = {10.0, 10.0},
        .maximum_dog_shift_clamp = 0.02,
        .initial_clutch_temperature_k = {298.15, 298.15},
        .clutch_heat_capacity_j_per_k = {12000.0, 12000.0},
        .clutch_ambient_conductance_w_per_k = {18.0, 18.0},
        .fade_start_temperature_k = {523.15, 523.15},
        .fade_full_temperature_k = {673.15, 673.15},
        .minimum_fade_capacity_fraction = {0.35, 0.35},
        .overheat_temperature_k = {623.15, 623.15},
        .slip_diagnostic_speed_rad_s = 2.0,
        .maximum_timestep_s = 0.02,
    };
}

pwr_dct_result pwr_dct_validate_config(const pwr_dct_config *config) {
    if (config == NULL) {
        return PWR_DCT_INVALID_ARGUMENT;
    }

    if (!pwr_dct_positive_finite(config->final_drive_ratio) ||
        !pwr_dct_positive_finite(config->slip_regularization_speed_rad_s) ||
        !isfinite(config->maximum_dog_shift_clamp) ||
        config->maximum_dog_shift_clamp < 0.0 ||
        config->maximum_dog_shift_clamp > 1.0 ||
        !pwr_dct_positive_finite(config->slip_diagnostic_speed_rad_s) ||
        !pwr_dct_positive_finite(config->maximum_timestep_s)) {
        return PWR_DCT_INVALID_CONFIG;
    }

    for (size_t index = 0U; index < (size_t)PWR_DCT_GEAR_COUNT; ++index) {
        const double ratio = config->gear_ratio[index];
        const double efficiency = config->mesh_efficiency[index];
        if (!isfinite(ratio) || !pwr_dct_positive_finite(efficiency) || efficiency > 1.0) {
            return PWR_DCT_INVALID_CONFIG;
        }
        if ((index == (size_t)PWR_DCT_GEAR_REVERSE && ratio >= 0.0) ||
            (index != (size_t)PWR_DCT_GEAR_REVERSE && ratio <= 0.0)) {
            return PWR_DCT_INVALID_CONFIG;
        }
    }

    for (size_t index = 0U; index < (size_t)PWR_DCT_CLUTCH_COUNT; ++index) {
        if (!pwr_dct_clutch_parameters_valid(config, index)) {
            return PWR_DCT_INVALID_CONFIG;
        }
    }
    return PWR_DCT_OK;
}

pwr_dct_clutch pwr_dct_clutch_for_gear(pwr_dct_gear gear) {
    switch (gear) {
        case PWR_DCT_GEAR_1:
        case PWR_DCT_GEAR_3:
        case PWR_DCT_GEAR_5:
        case PWR_DCT_GEAR_7:
            return PWR_DCT_CLUTCH_K1;
        case PWR_DCT_GEAR_REVERSE:
        case PWR_DCT_GEAR_2:
        case PWR_DCT_GEAR_4:
        case PWR_DCT_GEAR_6:
            return PWR_DCT_CLUTCH_K2;
        case PWR_DCT_GEAR_NEUTRAL:
        default:
            return PWR_DCT_CLUTCH_INVALID;
    }
}

pwr_dct_gear_mask pwr_dct_gear_bit(pwr_dct_gear gear) {
    if (gear < PWR_DCT_GEAR_REVERSE || gear > PWR_DCT_GEAR_7) {
        return UINT16_C(0);
    }
    return (pwr_dct_gear_mask)(UINT16_C(1) << (unsigned int)gear);
}

static unsigned int pwr_dct_count_bits(pwr_dct_gear_mask mask) {
    unsigned int count = 0U;
    while (mask != UINT16_C(0)) {
        count += (unsigned int)(mask & UINT16_C(1));
        mask = (pwr_dct_gear_mask)(mask >> 1U);
    }
    return count;
}

static pwr_dct_gear pwr_dct_selected_gear(pwr_dct_gear_mask mask,
                                          pwr_dct_clutch clutch) {
    for (int gear_value = (int)PWR_DCT_GEAR_REVERSE;
         gear_value <= (int)PWR_DCT_GEAR_7; ++gear_value) {
        const pwr_dct_gear gear = (pwr_dct_gear)gear_value;
        if ((mask & pwr_dct_gear_bit(gear)) != UINT16_C(0) &&
            pwr_dct_clutch_for_gear(gear) == clutch) {
            return gear;
        }
    }
    return PWR_DCT_GEAR_NEUTRAL;
}

static uint32_t pwr_dct_validate_gear_command(pwr_dct_gear_mask mask) {
    uint32_t flags = PWR_DCT_DIAG_NONE;
    if ((mask & (pwr_dct_gear_mask)(~PWR_DCT_GEAR_MASK_ALL)) != UINT16_C(0)) {
        flags |= PWR_DCT_DIAG_INVALID_GEAR_BIT;
    }
    if (pwr_dct_count_bits((pwr_dct_gear_mask)(mask & PWR_DCT_GEAR_MASK_K1)) > 1U ||
        pwr_dct_count_bits((pwr_dct_gear_mask)(mask & PWR_DCT_GEAR_MASK_K2)) > 1U) {
        flags |= PWR_DCT_DIAG_MULTIPLE_GEARS_ON_SHAFT;
    }
    return flags;
}

pwr_dct_result pwr_dct_init(pwr_dct *dct, const pwr_dct_config *config) {
    if (dct == NULL || config == NULL) {
        return PWR_DCT_INVALID_ARGUMENT;
    }
    const pwr_dct_result validation = pwr_dct_validate_config(config);
    if (validation != PWR_DCT_OK) {
        return validation;
    }

    memset(dct, 0, sizeof(*dct));
    dct->config = *config;
    for (size_t index = 0U; index < (size_t)PWR_DCT_CLUTCH_COUNT; ++index) {
        dct->selected_gear[index] = PWR_DCT_GEAR_NEUTRAL;
        dct->clutch_temperature_k[index] = config->initial_clutch_temperature_k[index];
        dct->last_thermal_capacity_factor[index] = 1.0;
    }
    return PWR_DCT_OK;
}

static void pwr_dct_reject_command(pwr_dct *dct, uint32_t reason_flags) {
    const uint32_t flags = reason_flags | PWR_DCT_DIAG_INPUT_REJECTED;
    dct->last_step_diagnostic_flags = flags;
    dct->diagnostic_flags |= flags;
    dct->rejected_command_count += UINT64_C(1);
}

static bool pwr_dct_inputs_valid(const pwr_dct_inputs *inputs) {
    if (inputs == NULL || !isfinite(inputs->engine_speed_rad_s) ||
        !isfinite(inputs->output_speed_rad_s) ||
        !pwr_dct_positive_finite(inputs->ambient_temperature_k)) {
        return false;
    }
    for (size_t index = 0U; index < (size_t)PWR_DCT_CLUTCH_COUNT; ++index) {
        const double command = inputs->clutch_clamp_command[index];
        if (!isfinite(command) || command < 0.0 || command > 1.0) {
            return false;
        }
    }
    return true;
}

static void pwr_dct_update_thermal(const pwr_dct_config *config, size_t index,
                                   double ambient_temperature_k, double friction_power_w,
                                   double dt_s, double old_temperature_k,
                                   double *new_temperature_k, double *heat_rejected_j) {
    const double capacity = config->clutch_heat_capacity_j_per_k[index];
    const double conductance = config->clutch_ambient_conductance_w_per_k[index];
    if (conductance > 0.0) {
        const double equilibrium_temperature =
            ambient_temperature_k + friction_power_w / conductance;
        const double decay = exp(-conductance * dt_s / capacity);
        *new_temperature_k = equilibrium_temperature +
                             (old_temperature_k - equilibrium_temperature) * decay;
    } else {
        *new_temperature_k = old_temperature_k + friction_power_w * dt_s / capacity;
    }
    const double stored_change_j = capacity * (*new_temperature_k - old_temperature_k);
    *heat_rejected_j = friction_power_w * dt_s - stored_change_j;
}

static double pwr_dct_stored_thermal_change(const pwr_dct *dct) {
    double stored_change_j = 0.0;
    for (size_t index = 0U; index < (size_t)PWR_DCT_CLUTCH_COUNT; ++index) {
        stored_change_j += dct->config.clutch_heat_capacity_j_per_k[index] *
                           (dct->clutch_temperature_k[index] -
                            dct->config.initial_clutch_temperature_k[index]);
    }
    return stored_change_j;
}

static double pwr_dct_energy_residual(const pwr_dct *dct) {
    return dct->cumulative_input_energy_j - dct->cumulative_output_energy_j -
           dct->cumulative_gear_mesh_loss_j - dct->cumulative_heat_rejected_j -
           pwr_dct_stored_thermal_change(dct);
}

pwr_dct_result pwr_dct_step(pwr_dct *dct, const pwr_dct_inputs *inputs, double dt_s) {
    if (dct == NULL || inputs == NULL) {
        return PWR_DCT_INVALID_ARGUMENT;
    }
    if (!pwr_dct_inputs_valid(inputs)) {
        pwr_dct_reject_command(dct, PWR_DCT_DIAG_NONE);
        return PWR_DCT_INVALID_ARGUMENT;
    }
    if (!pwr_dct_positive_finite(dt_s) || dt_s > dct->config.maximum_timestep_s) {
        return PWR_DCT_INVALID_TIMESTEP;
    }

    uint32_t step_flags = pwr_dct_validate_gear_command(inputs->engaged_gear_mask);
    if (step_flags != PWR_DCT_DIAG_NONE) {
        pwr_dct_reject_command(dct, step_flags);
        return PWR_DCT_INVALID_GEAR_COMMAND;
    }

    pwr_dct_gear desired_gear[PWR_DCT_CLUTCH_COUNT] = {
        pwr_dct_selected_gear(inputs->engaged_gear_mask, PWR_DCT_CLUTCH_K1),
        pwr_dct_selected_gear(inputs->engaged_gear_mask, PWR_DCT_CLUTCH_K2),
    };
    for (size_t index = 0U; index < (size_t)PWR_DCT_CLUTCH_COUNT; ++index) {
        if (desired_gear[index] != dct->selected_gear[index] &&
            dct->applied_clamp[index] > dct->config.maximum_dog_shift_clamp) {
            pwr_dct_reject_command(dct, PWR_DCT_DIAG_DOG_SHIFT_WHILE_CLAMPED);
            return PWR_DCT_INVALID_GEAR_COMMAND;
        }
    }

    double next_clamp[PWR_DCT_CLUTCH_COUNT];
    double next_temperature[PWR_DCT_CLUTCH_COUNT];
    double shaft_speed[PWR_DCT_CLUTCH_COUNT] = {0.0, 0.0};
    double slip_speed[PWR_DCT_CLUTCH_COUNT] = {0.0, 0.0};
    double transmitted_torque[PWR_DCT_CLUTCH_COUNT] = {0.0, 0.0};
    double torque_capacity[PWR_DCT_CLUTCH_COUNT] = {0.0, 0.0};
    double friction_power[PWR_DCT_CLUTCH_COUNT] = {0.0, 0.0};
    double capacity_factor[PWR_DCT_CLUTCH_COUNT];
    double heat_rejected_step_j[PWR_DCT_CLUTCH_COUNT] = {0.0, 0.0};

    double output_torque_nm = 0.0;
    double gear_loss_power_w = 0.0;
    for (size_t index = 0U; index < (size_t)PWR_DCT_CLUTCH_COUNT; ++index) {
        next_clamp[index] = pwr_dct_approach(
            dct->applied_clamp[index], inputs->clutch_clamp_command[index],
            dct->config.clamp_engagement_rate_per_s[index],
            dct->config.clamp_release_rate_per_s[index], dt_s);
        capacity_factor[index] = pwr_dct_capacity_factor(
            &dct->config, index, dct->clutch_temperature_k[index]);

        if (desired_gear[index] == PWR_DCT_GEAR_NEUTRAL) {
            if (next_clamp[index] > dct->config.maximum_dog_shift_clamp) {
                step_flags |= PWR_DCT_DIAG_CLAMPED_SHAFT_IN_NEUTRAL;
            }
        } else {
            const size_t gear_index = (size_t)desired_gear[index];
            const double total_ratio = dct->config.gear_ratio[gear_index] *
                                       dct->config.final_drive_ratio;
            const double efficiency = dct->config.mesh_efficiency[gear_index];
            shaft_speed[index] = inputs->output_speed_rad_s * total_ratio;
            slip_speed[index] = inputs->engine_speed_rad_s - shaft_speed[index];
            torque_capacity[index] = dct->config.clutch_nominal_capacity_nm[index] *
                                     next_clamp[index] * capacity_factor[index];
            transmitted_torque[index] = torque_capacity[index] *
                tanh(slip_speed[index] / dct->config.slip_regularization_speed_rad_s);
            friction_power[index] = transmitted_torque[index] * slip_speed[index];
            if (friction_power[index] < 0.0 && friction_power[index] > -1.0e-12) {
                friction_power[index] = 0.0;
            }

            const double shaft_power_w = transmitted_torque[index] * shaft_speed[index];
            double path_output_torque_nm;
            if (shaft_power_w >= 0.0) {
                path_output_torque_nm = transmitted_torque[index] * total_ratio * efficiency;
            } else {
                path_output_torque_nm = transmitted_torque[index] * total_ratio / efficiency;
            }
            const double path_output_power_w =
                path_output_torque_nm * inputs->output_speed_rad_s;
            double path_gear_loss_w = shaft_power_w - path_output_power_w;
            if (path_gear_loss_w < 0.0 && path_gear_loss_w > -1.0e-9) {
                path_gear_loss_w = 0.0;
            }
            output_torque_nm += path_output_torque_nm;
            gear_loss_power_w += path_gear_loss_w;

            if (fabs(slip_speed[index]) > dct->config.slip_diagnostic_speed_rad_s &&
                next_clamp[index] > dct->config.maximum_dog_shift_clamp) {
                step_flags |= index == (size_t)PWR_DCT_CLUTCH_K1
                                  ? PWR_DCT_DIAG_K1_SLIPPING
                                  : PWR_DCT_DIAG_K2_SLIPPING;
            }
        }

        pwr_dct_update_thermal(
            &dct->config, index, inputs->ambient_temperature_k, friction_power[index], dt_s,
            dct->clutch_temperature_k[index], &next_temperature[index],
            &heat_rejected_step_j[index]);
        const double next_capacity_factor = pwr_dct_capacity_factor(
            &dct->config, index, next_temperature[index]);
        if (next_capacity_factor < 1.0) {
            step_flags |= index == (size_t)PWR_DCT_CLUTCH_K1
                              ? PWR_DCT_DIAG_K1_THERMAL_FADE
                              : PWR_DCT_DIAG_K2_THERMAL_FADE;
        }
        if (next_temperature[index] >= dct->config.overheat_temperature_k[index]) {
            step_flags |= index == (size_t)PWR_DCT_CLUTCH_K1
                              ? PWR_DCT_DIAG_K1_OVERHEAT
                              : PWR_DCT_DIAG_K2_OVERHEAT;
        }
    }

    const double input_transmitted_torque_nm =
        transmitted_torque[(size_t)PWR_DCT_CLUTCH_K1] +
        transmitted_torque[(size_t)PWR_DCT_CLUTCH_K2];
    const double input_power_w = input_transmitted_torque_nm * inputs->engine_speed_rad_s;
    const double output_power_w = output_torque_nm * inputs->output_speed_rad_s;
    double instantaneous_efficiency = 0.0;
    if (input_power_w > 1.0e-12 && output_power_w >= 0.0) {
        instantaneous_efficiency = output_power_w / input_power_w;
    } else if (output_power_w < -1.0e-12 && input_power_w <= 0.0) {
        instantaneous_efficiency = input_power_w / output_power_w;
    }
    instantaneous_efficiency = pwr_dct_clamp(instantaneous_efficiency, 0.0, 1.0);

    const double input_step_j = input_power_w * dt_s;
    const double output_step_j = output_power_w * dt_s;
    const double gear_loss_step_j = gear_loss_power_w * dt_s;
    const double heat_rejected_total_step_j =
        heat_rejected_step_j[(size_t)PWR_DCT_CLUTCH_K1] +
        heat_rejected_step_j[(size_t)PWR_DCT_CLUTCH_K2];

    for (size_t index = 0U; index < (size_t)PWR_DCT_CLUTCH_COUNT; ++index) {
        if (!isfinite(next_clamp[index]) || !pwr_dct_positive_finite(next_temperature[index]) ||
            !isfinite(shaft_speed[index]) || !isfinite(slip_speed[index]) ||
            !isfinite(transmitted_torque[index]) || !isfinite(torque_capacity[index]) ||
            !isfinite(friction_power[index]) || friction_power[index] < 0.0) {
            dct->last_step_diagnostic_flags = PWR_DCT_DIAG_NUMERIC_FAILURE;
            dct->diagnostic_flags |= PWR_DCT_DIAG_NUMERIC_FAILURE;
            return PWR_DCT_NUMERIC_ERROR;
        }
    }
    if (!isfinite(output_torque_nm) || !isfinite(gear_loss_power_w) ||
        gear_loss_power_w < 0.0 || !isfinite(input_power_w) ||
        !isfinite(output_power_w) || !isfinite(instantaneous_efficiency)) {
        dct->last_step_diagnostic_flags = PWR_DCT_DIAG_NUMERIC_FAILURE;
        dct->diagnostic_flags |= PWR_DCT_DIAG_NUMERIC_FAILURE;
        return PWR_DCT_NUMERIC_ERROR;
    }

    dct->engaged_gear_mask = inputs->engaged_gear_mask;
    dct->last_engine_speed_rad_s = inputs->engine_speed_rad_s;
    dct->last_output_speed_rad_s = inputs->output_speed_rad_s;
    dct->last_input_transmitted_torque_nm = input_transmitted_torque_nm;
    dct->last_output_torque_nm = output_torque_nm;
    dct->last_input_power_w = input_power_w;
    dct->last_output_power_w = output_power_w;
    dct->last_gear_mesh_loss_power_w = gear_loss_power_w;
    dct->last_instantaneous_efficiency = instantaneous_efficiency;
    for (size_t index = 0U; index < (size_t)PWR_DCT_CLUTCH_COUNT; ++index) {
        dct->selected_gear[index] = desired_gear[index];
        dct->applied_clamp[index] = next_clamp[index];
        dct->clutch_temperature_k[index] = next_temperature[index];
        dct->cumulative_friction_work_j[index] += friction_power[index] * dt_s;
        dct->last_clamp_command[index] = inputs->clutch_clamp_command[index];
        dct->last_input_shaft_speed_rad_s[index] = shaft_speed[index];
        dct->last_slip_speed_rad_s[index] = slip_speed[index];
        dct->last_transmitted_torque_nm[index] = transmitted_torque[index];
        dct->last_torque_capacity_nm[index] = torque_capacity[index];
        dct->last_friction_power_w[index] = friction_power[index];
        dct->last_thermal_capacity_factor[index] = pwr_dct_capacity_factor(
            &dct->config, index, next_temperature[index]);
    }
    dct->cumulative_input_energy_j += input_step_j;
    dct->cumulative_output_energy_j += output_step_j;
    dct->cumulative_gear_mesh_loss_j += gear_loss_step_j;
    dct->cumulative_heat_rejected_j += heat_rejected_total_step_j;
    dct->time_s += dt_s;

    const double residual_j = pwr_dct_energy_residual(dct);
    const double energy_scale_j = fabs(dct->cumulative_input_energy_j) +
                                  fabs(dct->cumulative_output_energy_j) +
                                  dct->cumulative_gear_mesh_loss_j +
                                  fabs(dct->cumulative_heat_rejected_j) +
                                  fabs(pwr_dct_stored_thermal_change(dct)) + 1.0;
    if (fabs(residual_j) > 1.0e-9 * energy_scale_j) {
        step_flags |= PWR_DCT_DIAG_ENERGY_RESIDUAL;
    }
    dct->last_step_diagnostic_flags = step_flags;
    dct->diagnostic_flags |= step_flags;
    return PWR_DCT_OK;
}

static uint64_t pwr_dct_hash_u64(uint64_t hash, uint64_t value) {
    for (unsigned int byte = 0U; byte < 8U; ++byte) {
        hash ^= (value >> (byte * 8U)) & UINT64_C(0xff);
        hash *= UINT64_C(1099511628211);
    }
    return hash;
}

static uint64_t pwr_dct_hash_double(uint64_t hash, double value) {
    uint64_t bits = 0U;
    static_assert(sizeof(bits) == sizeof(value), "double must be 64-bit for deterministic hash");
    memcpy(&bits, &value, sizeof(bits));
    return pwr_dct_hash_u64(hash, bits);
}

uint64_t pwr_dct_state_hash(const pwr_dct *dct) {
    if (dct == NULL) {
        return UINT64_C(0);
    }
    uint64_t hash = UINT64_C(14695981039346656037);
    hash = pwr_dct_hash_u64(hash, dct->engaged_gear_mask);
    hash = pwr_dct_hash_double(hash, dct->time_s);
    for (size_t index = 0U; index < (size_t)PWR_DCT_CLUTCH_COUNT; ++index) {
        hash = pwr_dct_hash_u64(hash, (uint64_t)(int64_t)dct->selected_gear[index]);
        hash = pwr_dct_hash_double(hash, dct->applied_clamp[index]);
        hash = pwr_dct_hash_double(hash, dct->clutch_temperature_k[index]);
        hash = pwr_dct_hash_double(hash, dct->cumulative_friction_work_j[index]);
        hash = pwr_dct_hash_double(hash, dct->last_clamp_command[index]);
        hash = pwr_dct_hash_double(hash, dct->last_input_shaft_speed_rad_s[index]);
        hash = pwr_dct_hash_double(hash, dct->last_slip_speed_rad_s[index]);
        hash = pwr_dct_hash_double(hash, dct->last_transmitted_torque_nm[index]);
        hash = pwr_dct_hash_double(hash, dct->last_torque_capacity_nm[index]);
        hash = pwr_dct_hash_double(hash, dct->last_friction_power_w[index]);
        hash = pwr_dct_hash_double(hash, dct->last_thermal_capacity_factor[index]);
    }
    hash = pwr_dct_hash_double(hash, dct->last_engine_speed_rad_s);
    hash = pwr_dct_hash_double(hash, dct->last_output_speed_rad_s);
    hash = pwr_dct_hash_double(hash, dct->last_input_transmitted_torque_nm);
    hash = pwr_dct_hash_double(hash, dct->last_output_torque_nm);
    hash = pwr_dct_hash_double(hash, dct->last_input_power_w);
    hash = pwr_dct_hash_double(hash, dct->last_output_power_w);
    hash = pwr_dct_hash_double(hash, dct->last_gear_mesh_loss_power_w);
    hash = pwr_dct_hash_double(hash, dct->last_instantaneous_efficiency);
    hash = pwr_dct_hash_double(hash, dct->cumulative_input_energy_j);
    hash = pwr_dct_hash_double(hash, dct->cumulative_output_energy_j);
    hash = pwr_dct_hash_double(hash, dct->cumulative_gear_mesh_loss_j);
    hash = pwr_dct_hash_double(hash, dct->cumulative_heat_rejected_j);
    hash = pwr_dct_hash_u64(hash, dct->last_step_diagnostic_flags);
    hash = pwr_dct_hash_u64(hash, dct->diagnostic_flags);
    hash = pwr_dct_hash_u64(hash, dct->rejected_command_count);
    return hash;
}

pwr_dct_result pwr_dct_snapshot_read(const pwr_dct *dct, pwr_dct_snapshot *snapshot) {
    if (dct == NULL || snapshot == NULL) {
        return PWR_DCT_INVALID_ARGUMENT;
    }

    *snapshot = (pwr_dct_snapshot){
        .time_s = dct->time_s,
        .engaged_gear_mask = dct->engaged_gear_mask,
        .input_transmitted_torque_nm = dct->last_input_transmitted_torque_nm,
        .output_torque_nm = dct->last_output_torque_nm,
        .input_power_w = dct->last_input_power_w,
        .output_power_w = dct->last_output_power_w,
        .gear_mesh_loss_power_w = dct->last_gear_mesh_loss_power_w,
        .instantaneous_efficiency = dct->last_instantaneous_efficiency,
        .cumulative_input_energy_j = dct->cumulative_input_energy_j,
        .cumulative_output_energy_j = dct->cumulative_output_energy_j,
        .cumulative_gear_mesh_loss_j = dct->cumulative_gear_mesh_loss_j,
        .cumulative_heat_rejected_j = dct->cumulative_heat_rejected_j,
        .stored_thermal_energy_change_j = pwr_dct_stored_thermal_change(dct),
        .energy_residual_j = pwr_dct_energy_residual(dct),
        .last_step_diagnostic_flags = dct->last_step_diagnostic_flags,
        .diagnostic_flags = dct->diagnostic_flags,
        .rejected_command_count = dct->rejected_command_count,
        .state_hash = pwr_dct_state_hash(dct),
    };
    double friction_power_w = 0.0;
    for (size_t index = 0U; index < (size_t)PWR_DCT_CLUTCH_COUNT; ++index) {
        snapshot->clutch[index] = (pwr_dct_clutch_observation){
            .selected_gear = dct->selected_gear[index],
            .preselected = dct->selected_gear[index] != PWR_DCT_GEAR_NEUTRAL &&
                           dct->applied_clamp[index] <=
                               dct->config.maximum_dog_shift_clamp,
            .clamp_command = dct->last_clamp_command[index],
            .applied_clamp = dct->applied_clamp[index],
            .input_shaft_speed_rad_s = dct->last_input_shaft_speed_rad_s[index],
            .slip_speed_rad_s = dct->last_slip_speed_rad_s[index],
            .transmitted_torque_nm = dct->last_transmitted_torque_nm[index],
            .torque_capacity_nm = dct->last_torque_capacity_nm[index],
            .friction_power_w = dct->last_friction_power_w[index],
            .cumulative_friction_work_j = dct->cumulative_friction_work_j[index],
            .temperature_k = dct->clutch_temperature_k[index],
            .thermal_capacity_factor = dct->last_thermal_capacity_factor[index],
        };
        friction_power_w += dct->last_friction_power_w[index];
    }
    snapshot->clutch_friction_power_w = friction_power_w;
    return PWR_DCT_OK;
}

const char *pwr_dct_result_string(pwr_dct_result result) {
    switch (result) {
        case PWR_DCT_OK:
            return "ok";
        case PWR_DCT_INVALID_ARGUMENT:
            return "invalid argument";
        case PWR_DCT_INVALID_CONFIG:
            return "invalid configuration";
        case PWR_DCT_INVALID_GEAR_COMMAND:
            return "invalid gear command";
        case PWR_DCT_INVALID_TIMESTEP:
            return "invalid timestep";
        case PWR_DCT_NUMERIC_ERROR:
            return "numeric error";
        default:
            return "unknown result";
    }
}
