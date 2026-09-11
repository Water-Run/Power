// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/src/models/exhaust/pwr_exhaust.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_EXHAUST_DIAG_CATALYST_LIGHT_OFF = @import("../../abi.zig").PWR_EXHAUST_DIAG_CATALYST_LIGHT_OFF;
const PWR_EXHAUST_DIAG_CHOKED_FLOW = @import("../../abi.zig").PWR_EXHAUST_DIAG_CHOKED_FLOW;
const PWR_EXHAUST_DIAG_FLOW_LIMITED = @import("../../abi.zig").PWR_EXHAUST_DIAG_FLOW_LIMITED;
const PWR_EXHAUST_DIAG_INPUT_REJECTED = @import("../../abi.zig").PWR_EXHAUST_DIAG_INPUT_REJECTED;
const PWR_EXHAUST_DIAG_NUMERIC_FAILURE = @import("../../abi.zig").PWR_EXHAUST_DIAG_NUMERIC_FAILURE;
const PWR_EXHAUST_DIAG_POSITIVITY_CORRECTION = @import("../../abi.zig").PWR_EXHAUST_DIAG_POSITIVITY_CORRECTION;
const PWR_EXHAUST_DIAG_REACTION_LIMITED = @import("../../abi.zig").PWR_EXHAUST_DIAG_REACTION_LIMITED;
const PWR_EXHAUST_DIAG_REVERSE_FLOW = @import("../../abi.zig").PWR_EXHAUST_DIAG_REVERSE_FLOW;
const PWR_EXHAUST_INVALID_ARGUMENT = @import("../../abi.zig").PWR_EXHAUST_INVALID_ARGUMENT;
const PWR_EXHAUST_INVALID_BOUNDARY = @import("../../abi.zig").PWR_EXHAUST_INVALID_BOUNDARY;
const PWR_EXHAUST_INVALID_PARAMETER = @import("../../abi.zig").PWR_EXHAUST_INVALID_PARAMETER;
const PWR_EXHAUST_INVALID_STATE = @import("../../abi.zig").PWR_EXHAUST_INVALID_STATE;
const PWR_EXHAUST_INVALID_TIMESTEP = @import("../../abi.zig").PWR_EXHAUST_INVALID_TIMESTEP;
const PWR_EXHAUST_OK = @import("../../abi.zig").PWR_EXHAUST_OK;
const PWR_EXHAUST_PATH_COUNT = @import("../../abi.zig").PWR_EXHAUST_PATH_COUNT;
const PWR_EXHAUST_PATH_INLET = @import("../../abi.zig").PWR_EXHAUST_PATH_INLET;
const PWR_EXHAUST_PATH_TAILPIPE = @import("../../abi.zig").PWR_EXHAUST_PATH_TAILPIPE;
const PWR_EXHAUST_SPECIES_COUNT = @import("../../abi.zig").PWR_EXHAUST_SPECIES_COUNT;
const PWR_EXHAUST_SPECIES_INERT = @import("../../abi.zig").PWR_EXHAUST_SPECIES_INERT;
const PWR_EXHAUST_SPECIES_OXYGEN = @import("../../abi.zig").PWR_EXHAUST_SPECIES_OXYGEN;
const PWR_EXHAUST_SPECIES_PRODUCTS = @import("../../abi.zig").PWR_EXHAUST_SPECIES_PRODUCTS;
const PWR_EXHAUST_SPECIES_REDUCTANT = @import("../../abi.zig").PWR_EXHAUST_SPECIES_REDUCTANT;
const PWR_EXHAUST_SUBSTEP_LIMIT = @import("../../abi.zig").PWR_EXHAUST_SUBSTEP_LIMIT;
const PWR_EXHAUST_VOLUME_CATALYST = @import("../../abi.zig").PWR_EXHAUST_VOLUME_CATALYST;
const PWR_EXHAUST_VOLUME_COUNT = @import("../../abi.zig").PWR_EXHAUST_VOLUME_COUNT;
const ceil = @import("../../support.zig").ceil;
const fabs = @import("../../support.zig").fabs;
const memset = @import("../../support.zig").memset;
const pow = @import("../../support.zig").pow;
const power_isfinite = @import("../../support.zig").power_isfinite;
const pwr_exhaust_correction = @import("../../abi.zig").pwr_exhaust_correction;
const pwr_exhaust_diagnostics = @import("../../abi.zig").pwr_exhaust_diagnostics;
const pwr_exhaust_gas_properties = @import("../../abi.zig").pwr_exhaust_gas_properties;
const pwr_exhaust_inputs = @import("../../abi.zig").pwr_exhaust_inputs;
const pwr_exhaust_ledger = @import("../../abi.zig").pwr_exhaust_ledger;
const pwr_exhaust_observation = @import("../../abi.zig").pwr_exhaust_observation;
const pwr_exhaust_parameters = @import("../../abi.zig").pwr_exhaust_parameters;
const pwr_exhaust_rates = @import("../../abi.zig").pwr_exhaust_rates;
const pwr_exhaust_reservoir = @import("../../abi.zig").pwr_exhaust_reservoir;
const pwr_exhaust_result = @import("../../abi.zig").pwr_exhaust_result;
const pwr_exhaust_state = @import("../../abi.zig").pwr_exhaust_state;
const pwr_exhaust_volume_state = @import("../../abi.zig").pwr_exhaust_volume_state;
const sqrt = @import("../../support.zig").sqrt;

pub fn pwr_exhaust_validate_parameters(arg_parameters: [*c]const pwr_exhaust_parameters) callconv(.c) pwr_exhaust_result {
    var parameters = arg_parameters;
    _ = &parameters;
    if (parameters == null) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_ARGUMENT));
    }
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            if (((!(pwr_exhaust_is_positive_finite(parameters.*.volume_m3[volume]) != 0) or !(pwr_exhaust_is_positive_finite(parameters.*.wall_heat_capacity_j_per_k[volume]) != 0)) or !(pwr_exhaust_is_nonnegative_finite(parameters.*.gas_wall_conductance_w_per_k[volume]) != 0)) or !(pwr_exhaust_is_nonnegative_finite(parameters.*.wall_ambient_conductance_w_per_k[volume]) != 0)) {
                return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_PARAMETER));
            }
        }
    }
    {
        var path: usize = 0;
        _ = &path;
        while (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_COUNT)))) : (path +%= 1) {
            if (!(pwr_exhaust_is_nonnegative_finite(parameters.*.orifice_area_m2[path]) != 0) or !(pwr_exhaust_is_positive_finite(parameters.*.discharge_coefficient[path]) != 0)) {
                return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_PARAMETER));
            }
        }
    }
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            if ((!(pwr_exhaust_is_positive_finite(parameters.*.species_gas_constant_j_per_kg_k[species]) != 0) or !(pwr_exhaust_is_positive_finite(parameters.*.species_cv_j_per_kg_k[species]) != 0)) or (parameters.*.species_gas_constant_j_per_kg_k[species] >= parameters.*.species_cv_j_per_kg_k[species])) {
                return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_PARAMETER));
            }
        }
    }
    if (((((((((((((((!(pwr_exhaust_is_positive_finite(parameters.*.catalyst_light_off_temperature_k) != 0) or !(pwr_exhaust_is_positive_finite(parameters.*.catalyst_full_activity_temperature_k) != 0)) or (parameters.*.catalyst_full_activity_temperature_k <= parameters.*.catalyst_light_off_temperature_k)) or !(pwr_exhaust_is_positive_finite(parameters.*.catalyst_activity_time_constant_s) != 0)) or !(pwr_exhaust_is_positive_finite(parameters.*.catalyst_reaction_time_s) != 0)) or !(pwr_exhaust_is_nonnegative_finite(parameters.*.catalyst_oxygen_per_reductant_kg_per_kg) != 0)) or !(pwr_exhaust_is_nonnegative_finite(parameters.*.catalyst_reaction_heat_j_per_kg_reductant) != 0)) or !(pwr_exhaust_is_nonnegative_finite(parameters.*.catalyst_heat_to_wall_fraction) != 0)) or (parameters.*.catalyst_heat_to_wall_fraction > 1.0)) or !(pwr_exhaust_is_positive_finite(parameters.*.minimum_temperature_k) != 0)) or (parameters.*.minimum_temperature_k >= parameters.*.catalyst_light_off_temperature_k)) or !(pwr_exhaust_is_positive_finite(parameters.*.minimum_volume_mass_kg) != 0)) or !(pwr_exhaust_is_positive_finite(parameters.*.maximum_substep_s) != 0)) or !(pwr_exhaust_is_positive_finite(parameters.*.maximum_outflow_fraction_per_substep) != 0)) or (parameters.*.maximum_outflow_fraction_per_substep > 0.25)) or (parameters.*.maximum_substeps == @as(c_uint, 0))) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_PARAMETER));
    }
    return @as(c_uint, @bitCast(PWR_EXHAUST_OK));
}

pub fn pwr_exhaust_validate_reservoir(arg_parameters: [*c]const pwr_exhaust_parameters, arg_reservoir: [*c]const pwr_exhaust_reservoir) callconv(.c) pwr_exhaust_result {
    var parameters = arg_parameters;
    _ = &parameters;
    var reservoir = arg_reservoir;
    _ = &reservoir;
    var fraction_sum: f64 = 0.0;
    _ = &fraction_sum;
    if (pwr_exhaust_validate_parameters(parameters) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
        return @as(c_uint, @bitCast(if (parameters == null) PWR_EXHAUST_INVALID_ARGUMENT else PWR_EXHAUST_INVALID_PARAMETER));
    }
    if (reservoir == null) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_ARGUMENT));
    }
    if ((!(pwr_exhaust_is_positive_finite(reservoir.*.pressure_pa) != 0) or !(pwr_exhaust_is_positive_finite(reservoir.*.temperature_k) != 0)) or (reservoir.*.temperature_k < parameters.*.minimum_temperature_k)) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_BOUNDARY));
    }
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            if (!(pwr_exhaust_is_nonnegative_finite(reservoir.*.species_mass_fraction[species]) != 0)) {
                return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_BOUNDARY));
            }
            fraction_sum += reservoir.*.species_mass_fraction[species];
        }
    }
    if (fabs(fraction_sum - 1.0) > 0.000000001) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_BOUNDARY));
    }
    return @as(c_uint, @bitCast(PWR_EXHAUST_OK));
}

pub fn pwr_exhaust_initialize_uniform(arg_parameters: [*c]const pwr_exhaust_parameters, arg_initial_gas: [*c]const pwr_exhaust_reservoir, arg_initial_wall_temperature_k: f64, arg_state: [*c]pwr_exhaust_state) callconv(.c) pwr_exhaust_result {
    var parameters = arg_parameters;
    _ = &parameters;
    var initial_gas = arg_initial_gas;
    _ = &initial_gas;
    var initial_wall_temperature_k = arg_initial_wall_temperature_k;
    _ = &initial_wall_temperature_k;
    var state = arg_state;
    _ = &state;
    var gas: pwr_exhaust_gas_properties = undefined;
    _ = &gas;
    if (state == null) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_ARGUMENT));
    }
    if (pwr_exhaust_validate_parameters(parameters) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
        return @as(c_uint, @bitCast(if (parameters == null) PWR_EXHAUST_INVALID_ARGUMENT else PWR_EXHAUST_INVALID_PARAMETER));
    }
    if (((pwr_exhaust_validate_reservoir(parameters, initial_gas) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) or !(pwr_exhaust_is_positive_finite(initial_wall_temperature_k) != 0)) or (initial_wall_temperature_k < parameters.*.minimum_temperature_k)) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_BOUNDARY));
    }
    pwr_exhaust_reservoir_properties(parameters, initial_gas, &gas);
    _ = memset(@as(?*anyopaque, @ptrCast(state)), @as(c_int, 0), @sizeOf(pwr_exhaust_state));
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            const mass: f64 = (initial_gas.*.pressure_pa * parameters.*.volume_m3[volume]) / (gas.gas_constant_j_per_kg_k * initial_gas.*.temperature_k);
            _ = &mass;
            if (mass < parameters.*.minimum_volume_mass_kg) {
                _ = memset(@as(?*anyopaque, @ptrCast(state)), @as(c_int, 0), @sizeOf(pwr_exhaust_state));
                return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
            }
            state.*.volume[volume].total_mass_kg = mass;
            state.*.volume[volume].wall_temperature_k = initial_wall_temperature_k;
            {
                var species: usize = 0;
                _ = &species;
                while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
                    const species_mass: f64 = mass * initial_gas.*.species_mass_fraction[species];
                    _ = &species_mass;
                    state.*.volume[volume].species_mass_kg[species] = species_mass;
                    state.*.volume[volume].internal_energy_j += (species_mass * parameters.*.species_cv_j_per_kg_k[species]) * initial_gas.*.temperature_k;
                }
            }
        }
    }
    state.*.catalyst_conversion_fraction = pwr_exhaust_activity_target(parameters, initial_wall_temperature_k);
    return @as(c_uint, @bitCast(PWR_EXHAUST_OK));
}

pub fn pwr_exhaust_observe(arg_parameters: [*c]const pwr_exhaust_parameters, arg_state: [*c]const pwr_exhaust_state, arg_observation: [*c]pwr_exhaust_observation) callconv(.c) pwr_exhaust_result {
    var parameters = arg_parameters;
    _ = &parameters;
    var state = arg_state;
    _ = &state;
    var observation = arg_observation;
    _ = &observation;
    if (observation == null) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_ARGUMENT));
    }
    if (pwr_exhaust_validate_parameters(parameters) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
        return @as(c_uint, @bitCast(if (parameters == null) PWR_EXHAUST_INVALID_ARGUMENT else PWR_EXHAUST_INVALID_PARAMETER));
    }
    if (pwr_exhaust_validate_state(parameters, state) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
    }
    _ = memset(@as(?*anyopaque, @ptrCast(observation)), @as(c_int, 0), @sizeOf(pwr_exhaust_observation));
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            var properties: pwr_exhaust_gas_properties = undefined;
            _ = &properties;
            if (pwr_exhaust_volume_properties(parameters, &state.*.volume[volume], volume, &properties) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
                return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
            }
            observation.*.pressure_pa[volume] = properties.pressure_pa;
            observation.*.gas_temperature_k[volume] = properties.temperature_k;
            observation.*.wall_temperature_k[volume] = state.*.volume[volume].wall_temperature_k;
            observation.*.total_mass_kg[volume] = state.*.volume[volume].total_mass_kg;
        }
    }
    observation.*.catalyst_conversion_fraction = state.*.catalyst_conversion_fraction;
    return @as(c_uint, @bitCast(PWR_EXHAUST_OK));
}

pub fn pwr_exhaust_step(arg_parameters: [*c]const pwr_exhaust_parameters, arg_state: [*c]pwr_exhaust_state, arg_inputs: [*c]const pwr_exhaust_inputs, arg_dt_s: f64, arg_diagnostics: [*c]pwr_exhaust_diagnostics) callconv(.c) pwr_exhaust_result {
    var parameters = arg_parameters;
    _ = &parameters;
    var state = arg_state;
    _ = &state;
    var inputs = arg_inputs;
    _ = &inputs;
    var dt_s = arg_dt_s;
    _ = &dt_s;
    var diagnostics = arg_diagnostics;
    _ = &diagnostics;
    var work: pwr_exhaust_state = undefined;
    _ = &work;
    var ledger: pwr_exhaust_ledger = pwr_exhaust_ledger{
        .path_mass_kg = [1]f64{
            0,
        } ++ [1]f64{0} ** 3,
        .boundary_mass_net_kg = 0,
        .boundary_species_net_kg = @import("std").mem.zeroes([4]f64),
        .boundary_enthalpy_net_j = 0,
        .ambient_heat_net_j = 0,
        .reaction_heat_j = 0,
        .reaction_species_kg = @import("std").mem.zeroes([4]f64),
        .reacted_reductant_kg = 0,
        .tailpipe_species_out_kg = @import("std").mem.zeroes([4]f64),
        .positivity_mass_adjustment_kg = 0,
        .positivity_species_adjustment_kg = @import("std").mem.zeroes([4]f64),
        .positivity_energy_adjustment_j = 0,
    };
    _ = &ledger;
    var initial_mass: f64 = undefined;
    _ = &initial_mass;
    var initial_energy: f64 = undefined;
    _ = &initial_energy;
    var initial_species: [4]f64 = undefined;
    _ = &initial_species;
    var substeps: u32 = undefined;
    _ = &substeps;
    var substep_s: f64 = undefined;
    _ = &substep_s;
    if (diagnostics == null) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_ARGUMENT));
    }
    _ = memset(@as(?*anyopaque, @ptrCast(diagnostics)), @as(c_int, 0), @sizeOf(pwr_exhaust_diagnostics));
    diagnostics.*.result = @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_ARGUMENT));
    if (((parameters == null) or (state == null)) or (inputs == null)) {
        diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_INPUT_REJECTED));
        return diagnostics.*.result;
    }
    diagnostics.*.result = pwr_exhaust_validate_parameters(parameters);
    if (diagnostics.*.result != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
        diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_INPUT_REJECTED));
        return diagnostics.*.result;
    }
    if ((((((pwr_exhaust_validate_reservoir(parameters, &inputs.*.upstream) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) or (pwr_exhaust_validate_reservoir(parameters, &inputs.*.downstream) != @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) or !(pwr_exhaust_is_nonnegative_finite(inputs.*.inlet_area_scale) != 0)) or !(pwr_exhaust_is_nonnegative_finite(inputs.*.tailpipe_area_scale) != 0)) or !(pwr_exhaust_is_positive_finite(inputs.*.ambient_temperature_k) != 0)) or (inputs.*.ambient_temperature_k < parameters.*.minimum_temperature_k)) {
        diagnostics.*.result = @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_BOUNDARY));
        diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_INPUT_REJECTED));
        return diagnostics.*.result;
    }
    if (pwr_exhaust_validate_state(parameters, state) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
        diagnostics.*.result = @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
        diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_INPUT_REJECTED));
        return diagnostics.*.result;
    }
    if (!(pwr_exhaust_is_positive_finite(dt_s) != 0)) {
        diagnostics.*.result = @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_TIMESTEP));
        diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_INPUT_REJECTED));
        return diagnostics.*.result;
    }
    if ((dt_s / parameters.*.maximum_substep_s) > @as(f64, @floatFromInt(parameters.*.maximum_substeps))) {
        diagnostics.*.result = @as(c_uint, @bitCast(PWR_EXHAUST_SUBSTEP_LIMIT));
        diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_INPUT_REJECTED));
        return diagnostics.*.result;
    }
    substeps = @as(u32, @intFromFloat(ceil(dt_s / parameters.*.maximum_substep_s)));
    if (substeps == @as(c_uint, 0)) {
        substeps = 1;
    }
    if (substeps > parameters.*.maximum_substeps) {
        diagnostics.*.result = @as(c_uint, @bitCast(PWR_EXHAUST_SUBSTEP_LIMIT));
        diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_INPUT_REJECTED));
        return diagnostics.*.result;
    }
    substep_s = dt_s / @as(f64, @floatFromInt(substeps));
    work = state.*;
    initial_mass = pwr_exhaust_system_mass(&work);
    initial_energy = pwr_exhaust_system_energy(parameters, &work);
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            initial_species[species] = pwr_exhaust_system_species_mass(&work, species);
        }
    }
    {
        var step: u32 = 0;
        _ = &step;
        while (step < substeps) : (step +%= 1) {
            const start: pwr_exhaust_state = work;
            _ = &start;
            var stage: pwr_exhaust_state = start;
            _ = &stage;
            var first_rates: pwr_exhaust_rates = undefined;
            _ = &first_rates;
            var second_rates: pwr_exhaust_rates = undefined;
            _ = &second_rates;
            var first_correction: pwr_exhaust_correction = undefined;
            _ = &first_correction;
            var second_correction: pwr_exhaust_correction = undefined;
            _ = &second_correction;
            var result: pwr_exhaust_result = undefined;
            _ = &result;
            result = pwr_exhaust_calculate_rates(parameters, &start, inputs, substep_s, &first_rates);
            if (result != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
                diagnostics.*.result = result;
                diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_NUMERIC_FAILURE));
                return result;
            }
            pwr_exhaust_euler_advance(&stage, &first_rates, substep_s);
            result = pwr_exhaust_apply_positivity(parameters, &stage, &first_correction);
            if (result != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
                diagnostics.*.result = result;
                diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_NUMERIC_FAILURE));
                return result;
            }
            result = pwr_exhaust_calculate_rates(parameters, &stage, inputs, substep_s, &second_rates);
            if (result != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
                diagnostics.*.result = result;
                diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_NUMERIC_FAILURE));
                return result;
            }
            pwr_exhaust_euler_advance(&stage, &second_rates, substep_s);
            result = pwr_exhaust_apply_positivity(parameters, &stage, &second_correction);
            if (result != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
                diagnostics.*.result = result;
                diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_NUMERIC_FAILURE));
                return result;
            }
            pwr_exhaust_ssp_average(&start, &stage, &work);
            pwr_exhaust_accumulate_rates(&ledger, &first_rates, 0.5 * substep_s);
            pwr_exhaust_accumulate_rates(&ledger, &second_rates, 0.5 * substep_s);
            pwr_exhaust_accumulate_correction(&ledger, &first_correction, 0.5);
            pwr_exhaust_accumulate_correction(&ledger, &second_correction, 0.5);
            diagnostics.*.flags |= first_rates.flags | second_rates.flags;
            diagnostics.*.choked_path_mask |= first_rates.choked_path_mask | second_rates.choked_path_mask;
            diagnostics.*.flow_limit_events +%= first_rates.flow_limit_events +% second_rates.flow_limit_events;
            diagnostics.*.reaction_limit_events +%= first_rates.reaction_limit_events +% second_rates.reaction_limit_events;
            diagnostics.*.positivity_correction_events +%= first_correction.events +% second_correction.events;
        }
    }
    work.time_s += dt_s;
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            work.cumulative_tailpipe_species_kg[species] += ledger.tailpipe_species_out_kg[species];
        }
    }
    diagnostics.*.substeps = substeps;
    diagnostics.*.boundary_mass_net_kg = ledger.boundary_mass_net_kg;
    diagnostics.*.boundary_enthalpy_net_j = ledger.boundary_enthalpy_net_j;
    diagnostics.*.ambient_heat_net_j = ledger.ambient_heat_net_j;
    diagnostics.*.reaction_heat_j = ledger.reaction_heat_j;
    diagnostics.*.reacted_reductant_kg = ledger.reacted_reductant_kg;
    diagnostics.*.positivity_mass_adjustment_kg = ledger.positivity_mass_adjustment_kg;
    diagnostics.*.positivity_energy_adjustment_j = ledger.positivity_energy_adjustment_j;
    {
        var path: usize = 0;
        _ = &path;
        while (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_COUNT)))) : (path +%= 1) {
            diagnostics.*.average_path_mass_flow_kg_per_s[path] = ledger.path_mass_kg[path] / dt_s;
        }
    }
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            diagnostics.*.boundary_species_net_kg[species] = ledger.boundary_species_net_kg[species];
            diagnostics.*.tailpipe_species_out_kg[species] = ledger.tailpipe_species_out_kg[species];
            diagnostics.*.positivity_species_adjustment_kg[species] = ledger.positivity_species_adjustment_kg[species];
            diagnostics.*.species_mass_residual_kg[species] = (((pwr_exhaust_system_species_mass(&work, species) - initial_species[species]) - ledger.boundary_species_net_kg[species]) - ledger.reaction_species_kg[species]) - ledger.positivity_species_adjustment_kg[species];
        }
    }
    diagnostics.*.mass_residual_kg = ((pwr_exhaust_system_mass(&work) - initial_mass) - ledger.boundary_mass_net_kg) - ledger.positivity_mass_adjustment_kg;
    diagnostics.*.energy_residual_j = ((((pwr_exhaust_system_energy(parameters, &work) - initial_energy) - ledger.boundary_enthalpy_net_j) - ledger.ambient_heat_net_j) - ledger.reaction_heat_j) - ledger.positivity_energy_adjustment_j;
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            diagnostics.*.state_species_closure_residual_kg += work.volume[volume].total_mass_kg - pwr_exhaust_species_sum(@as([*c]f64, @ptrCast(@alignCast(&work.volume[volume].species_mass_kg[@as(usize, @intCast(0))]))));
        }
    }
    if (diagnostics.*.positivity_correction_events > @as(c_uint, 0)) {
        diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_POSITIVITY_CORRECTION));
    }
    if (work.catalyst_conversion_fraction >= 0.5) {
        diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_CATALYST_LIGHT_OFF));
    }
    diagnostics.*.result = pwr_exhaust_observe(parameters, &work, &diagnostics.*.final_observation);
    if (diagnostics.*.result != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
        diagnostics.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_NUMERIC_FAILURE));
        return diagnostics.*.result;
    }
    state.* = work;
    return @as(c_uint, @bitCast(PWR_EXHAUST_OK));
}

pub fn pwr_exhaust_result_string(arg_result: pwr_exhaust_result) callconv(.c) [*c]const u8 {
    var result = arg_result;
    _ = &result;
    while (true) {
        switch (result) {
            @as(c_uint, @bitCast(@as(c_int, 0))) => return "ok",
            @as(c_uint, @bitCast(@as(c_int, 1))) => return "invalid argument",
            @as(c_uint, @bitCast(@as(c_int, 2))) => return "invalid parameter",
            @as(c_uint, @bitCast(@as(c_int, 3))) => return "invalid boundary",
            @as(c_uint, @bitCast(@as(c_int, 4))) => return "invalid state",
            @as(c_uint, @bitCast(@as(c_int, 5))) => return "invalid timestep",
            @as(c_uint, @bitCast(@as(c_int, 6))) => return "substep limit",
            else => return "unknown exhaust result",
        }
        break;
    }
    return null;
}

pub fn pwr_exhaust_is_positive_finite(arg_value: f64) callconv(.c) c_int {
    var value = arg_value;
    _ = &value;
    return @intFromBool((power_isfinite(value) != 0) and (value > 0.0));
}

pub fn pwr_exhaust_is_nonnegative_finite(arg_value: f64) callconv(.c) c_int {
    var value = arg_value;
    _ = &value;
    return @intFromBool((power_isfinite(value) != 0) and (value >= 0.0));
}

pub fn pwr_exhaust_min(arg_a: f64, arg_b: f64) callconv(.c) f64 {
    var a = arg_a;
    _ = &a;
    var b = arg_b;
    _ = &b;
    return if (a < b) a else b;
}

pub fn pwr_exhaust_max(arg_a: f64, arg_b: f64) callconv(.c) f64 {
    var a = arg_a;
    _ = &a;
    var b = arg_b;
    _ = &b;
    return if (a > b) a else b;
}

pub fn pwr_exhaust_clamp(arg_value: f64, arg_low: f64, arg_high: f64) callconv(.c) f64 {
    var value = arg_value;
    _ = &value;
    var low = arg_low;
    _ = &low;
    var high = arg_high;
    _ = &high;
    return pwr_exhaust_min(pwr_exhaust_max(value, low), high);
}

pub fn pwr_exhaust_species_sum(species_mass_kg: [*c]const f64) callconv(.c) f64 {
    _ = &species_mass_kg;
    var total: f64 = 0.0;
    _ = &total;
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            total += species_mass_kg[species];
        }
    }
    return total;
}

pub fn pwr_exhaust_system_mass(arg_state: [*c]const pwr_exhaust_state) callconv(.c) f64 {
    var state = arg_state;
    _ = &state;
    var total: f64 = 0.0;
    _ = &total;
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            total += state.*.volume[volume].total_mass_kg;
        }
    }
    return total;
}

pub fn pwr_exhaust_system_species_mass(arg_state: [*c]const pwr_exhaust_state, arg_species: usize) callconv(.c) f64 {
    var state = arg_state;
    _ = &state;
    var species = arg_species;
    _ = &species;
    var total: f64 = 0.0;
    _ = &total;
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            total += state.*.volume[volume].species_mass_kg[species];
        }
    }
    return total;
}

pub fn pwr_exhaust_system_energy(arg_parameters: [*c]const pwr_exhaust_parameters, arg_state: [*c]const pwr_exhaust_state) callconv(.c) f64 {
    var parameters = arg_parameters;
    _ = &parameters;
    var state = arg_state;
    _ = &state;
    var total: f64 = 0.0;
    _ = &total;
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            total += state.*.volume[volume].internal_energy_j;
            total += parameters.*.wall_heat_capacity_j_per_k[volume] * state.*.volume[volume].wall_temperature_k;
        }
    }
    return total;
}

pub fn pwr_exhaust_activity_target(arg_parameters: [*c]const pwr_exhaust_parameters, arg_catalyst_temperature_k: f64) callconv(.c) f64 {
    var parameters = arg_parameters;
    _ = &parameters;
    var catalyst_temperature_k = arg_catalyst_temperature_k;
    _ = &catalyst_temperature_k;
    const span: f64 = parameters.*.catalyst_full_activity_temperature_k - parameters.*.catalyst_light_off_temperature_k;
    _ = &span;
    const normalized: f64 = pwr_exhaust_clamp((catalyst_temperature_k - parameters.*.catalyst_light_off_temperature_k) / span, 0.0, 1.0);
    _ = &normalized;
    return (normalized * normalized) * (3.0 - (2.0 * normalized));
}

pub fn pwr_exhaust_reservoir_properties(arg_parameters: [*c]const pwr_exhaust_parameters, arg_reservoir: [*c]const pwr_exhaust_reservoir, arg_properties: [*c]pwr_exhaust_gas_properties) callconv(.c) void {
    var parameters = arg_parameters;
    _ = &parameters;
    var reservoir = arg_reservoir;
    _ = &reservoir;
    var properties = arg_properties;
    _ = &properties;
    var gas_constant: f64 = 0.0;
    _ = &gas_constant;
    var cv: f64 = 0.0;
    _ = &cv;
    _ = memset(@as(?*anyopaque, @ptrCast(properties)), @as(c_int, 0), @sizeOf(pwr_exhaust_gas_properties));
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            const fraction: f64 = reservoir.*.species_mass_fraction[species];
            _ = &fraction;
            properties.*.mass_fraction[species] = fraction;
            gas_constant += fraction * parameters.*.species_gas_constant_j_per_kg_k[species];
            cv += fraction * parameters.*.species_cv_j_per_kg_k[species];
        }
    }
    properties.*.pressure_pa = reservoir.*.pressure_pa;
    properties.*.temperature_k = reservoir.*.temperature_k;
    properties.*.gas_constant_j_per_kg_k = gas_constant;
    properties.*.cv_j_per_kg_k = cv;
    properties.*.cp_j_per_kg_k = cv + gas_constant;
    properties.*.gamma = properties.*.cp_j_per_kg_k / cv;
}

pub fn pwr_exhaust_volume_properties(arg_parameters: [*c]const pwr_exhaust_parameters, arg_state: [*c]const pwr_exhaust_volume_state, arg_volume: usize, arg_properties: [*c]pwr_exhaust_gas_properties) callconv(.c) pwr_exhaust_result {
    var parameters = arg_parameters;
    _ = &parameters;
    var state = arg_state;
    _ = &state;
    var volume = arg_volume;
    _ = &volume;
    var properties = arg_properties;
    _ = &properties;
    var heat_capacity: f64 = 0.0;
    _ = &heat_capacity;
    var mass_weighted_gas_constant: f64 = 0.0;
    _ = &mass_weighted_gas_constant;
    const species_total: f64 = pwr_exhaust_species_sum(@as([*c]const f64, @ptrCast(@alignCast(&state.*.species_mass_kg[@as(usize, @intCast(0))]))));
    _ = &species_total;
    if (!(pwr_exhaust_is_positive_finite(species_total) != 0)) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
    }
    _ = memset(@as(?*anyopaque, @ptrCast(properties)), @as(c_int, 0), @sizeOf(pwr_exhaust_gas_properties));
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            const mass: f64 = state.*.species_mass_kg[species];
            _ = &mass;
            properties.*.mass_fraction[species] = mass / species_total;
            heat_capacity += mass * parameters.*.species_cv_j_per_kg_k[species];
            mass_weighted_gas_constant += mass * parameters.*.species_gas_constant_j_per_kg_k[species];
        }
    }
    if (!(pwr_exhaust_is_positive_finite(heat_capacity) != 0)) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
    }
    properties.*.temperature_k = state.*.internal_energy_j / heat_capacity;
    properties.*.gas_constant_j_per_kg_k = mass_weighted_gas_constant / species_total;
    properties.*.cv_j_per_kg_k = heat_capacity / species_total;
    properties.*.cp_j_per_kg_k = properties.*.cv_j_per_kg_k + properties.*.gas_constant_j_per_kg_k;
    properties.*.gamma = properties.*.cp_j_per_kg_k / properties.*.cv_j_per_kg_k;
    properties.*.pressure_pa = (properties.*.temperature_k * mass_weighted_gas_constant) / parameters.*.volume_m3[volume];
    if ((!(pwr_exhaust_is_positive_finite(properties.*.temperature_k) != 0) or !(pwr_exhaust_is_positive_finite(properties.*.pressure_pa) != 0)) or !(pwr_exhaust_is_positive_finite(properties.*.gamma) != 0)) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
    }
    return @as(c_uint, @bitCast(PWR_EXHAUST_OK));
}

pub fn pwr_exhaust_orifice_mass_flow(arg_area_m2: f64, arg_discharge_coefficient: f64, arg_left: [*c]const pwr_exhaust_gas_properties, arg_right: [*c]const pwr_exhaust_gas_properties, arg_choked: [*c]c_int) callconv(.c) f64 {
    var area_m2 = arg_area_m2;
    _ = &area_m2;
    var discharge_coefficient = arg_discharge_coefficient;
    _ = &discharge_coefficient;
    var left = arg_left;
    _ = &left;
    var right = arg_right;
    _ = &right;
    var choked = arg_choked;
    _ = &choked;
    var upstream: [*c]const pwr_exhaust_gas_properties = left;
    _ = &upstream;
    var downstream: [*c]const pwr_exhaust_gas_properties = right;
    _ = &downstream;
    var direction: f64 = 1.0;
    _ = &direction;
    var pressure_ratio: f64 = undefined;
    _ = &pressure_ratio;
    var critical_ratio: f64 = undefined;
    _ = &critical_ratio;
    var flow_factor: f64 = undefined;
    _ = &flow_factor;
    choked.* = 0;
    if (area_m2 == 0.0) {
        return 0.0;
    }
    if (fabs(left.*.pressure_pa - right.*.pressure_pa) <= (0.000000000001 * pwr_exhaust_max(left.*.pressure_pa, right.*.pressure_pa))) {
        return 0.0;
    }
    if (right.*.pressure_pa > left.*.pressure_pa) {
        upstream = right;
        downstream = left;
        direction = -1.0;
    }
    pressure_ratio = pwr_exhaust_clamp(downstream.*.pressure_pa / upstream.*.pressure_pa, 0.0, 1.0);
    critical_ratio = pow(2.0 / (upstream.*.gamma + 1.0), upstream.*.gamma / (upstream.*.gamma - 1.0));
    if (pressure_ratio <= critical_ratio) {
        choked.* = 1;
        flow_factor = sqrt(upstream.*.gamma) * pow(2.0 / (upstream.*.gamma + 1.0), (upstream.*.gamma + 1.0) / (2.0 * (upstream.*.gamma - 1.0)));
    } else {
        const first: f64 = pow(pressure_ratio, 2.0 / upstream.*.gamma);
        _ = &first;
        const second: f64 = pow(pressure_ratio, (upstream.*.gamma + 1.0) / upstream.*.gamma);
        _ = &second;
        flow_factor = sqrt(pwr_exhaust_max(0.0, ((2.0 * upstream.*.gamma) / (upstream.*.gamma - 1.0)) * (first - second)));
    }
    return ((((direction * discharge_coefficient) * area_m2) * upstream.*.pressure_pa) / sqrt(upstream.*.gas_constant_j_per_kg_k * upstream.*.temperature_k)) * flow_factor;
}

pub fn pwr_exhaust_rates_are_finite(arg_rates: [*c]const pwr_exhaust_rates) callconv(.c) c_int {
    var rates = arg_rates;
    _ = &rates;
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            if ((!(power_isfinite(rates.*.total_mass_kg_per_s[volume]) != 0) or !(power_isfinite(rates.*.internal_energy_w[volume]) != 0)) or !(power_isfinite(rates.*.wall_temperature_k_per_s[volume]) != 0)) {
                return 0;
            }
            {
                var species: usize = 0;
                _ = &species;
                while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
                    if (!(power_isfinite(rates.*.species_mass_kg_per_s[volume][species]) != 0)) {
                        return 0;
                    }
                }
            }
        }
    }
    if (((((!(power_isfinite(rates.*.catalyst_conversion_per_s) != 0) or !(power_isfinite(rates.*.boundary_mass_net_kg_per_s) != 0)) or !(power_isfinite(rates.*.boundary_enthalpy_net_w) != 0)) or !(power_isfinite(rates.*.ambient_heat_net_w) != 0)) or !(power_isfinite(rates.*.reaction_heat_w) != 0)) or !(power_isfinite(rates.*.reacted_reductant_kg_per_s) != 0)) {
        return 0;
    }
    {
        var path: usize = 0;
        _ = &path;
        while (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_COUNT)))) : (path +%= 1) {
            if (!(power_isfinite(rates.*.path_mass_flow_kg_per_s[path]) != 0)) {
                return 0;
            }
        }
    }
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            if ((!(power_isfinite(rates.*.boundary_species_net_kg_per_s[species]) != 0) or !(power_isfinite(rates.*.reaction_species_kg_per_s[species]) != 0)) or !(power_isfinite(rates.*.tailpipe_species_out_kg_per_s[species]) != 0)) {
                return 0;
            }
        }
    }
    return 1;
}

pub fn pwr_exhaust_calculate_rates(arg_parameters: [*c]const pwr_exhaust_parameters, arg_state: [*c]const pwr_exhaust_state, arg_inputs: [*c]const pwr_exhaust_inputs, arg_substep_s: f64, arg_rates: [*c]pwr_exhaust_rates) callconv(.c) pwr_exhaust_result {
    var parameters = arg_parameters;
    _ = &parameters;
    var state = arg_state;
    _ = &state;
    var inputs = arg_inputs;
    _ = &inputs;
    var substep_s = arg_substep_s;
    _ = &substep_s;
    var rates = arg_rates;
    _ = &rates;
    var gas: [3]pwr_exhaust_gas_properties = undefined;
    _ = &gas;
    var upstream: pwr_exhaust_gas_properties = undefined;
    _ = &upstream;
    var downstream: pwr_exhaust_gas_properties = undefined;
    _ = &downstream;
    var donor_outflow_kg_per_s: [3]f64 = [1]f64{
        0.0,
    } ++ [1]f64{0} ** 2;
    _ = &donor_outflow_kg_per_s;
    var donor_scale: [3]f64 = [3]f64{
        1.0,
        1.0,
        1.0,
    };
    _ = &donor_scale;
    _ = memset(@as(?*anyopaque, @ptrCast(rates)), @as(c_int, 0), @sizeOf(pwr_exhaust_rates));
    pwr_exhaust_reservoir_properties(parameters, &inputs.*.upstream, &upstream);
    pwr_exhaust_reservoir_properties(parameters, &inputs.*.downstream, &downstream);
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            if (pwr_exhaust_volume_properties(parameters, &state.*.volume[volume], volume, &gas[volume]) != @as(c_uint, @bitCast(PWR_EXHAUST_OK))) {
                return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
            }
        }
    }
    {
        var path: usize = 0;
        _ = &path;
        while (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_COUNT)))) : (path +%= 1) {
            var left: [*c]const pwr_exhaust_gas_properties = if (path == @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_INLET)))) &upstream else &gas[path -% @as(usize, @bitCast(@as(u64, @as(c_uint, 1))))];
            _ = &left;
            var right: [*c]const pwr_exhaust_gas_properties = if (path == @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_TAILPIPE)))) &downstream else &gas[path];
            _ = &right;
            var area: f64 = parameters.*.orifice_area_m2[path];
            _ = &area;
            var choked: c_int = 0;
            _ = &choked;
            if (path == @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_INLET)))) {
                area *= inputs.*.inlet_area_scale;
            } else if (path == @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_TAILPIPE)))) {
                area *= inputs.*.tailpipe_area_scale;
            }
            rates.*.path_mass_flow_kg_per_s[path] = pwr_exhaust_orifice_mass_flow(area, parameters.*.discharge_coefficient[path], left, right, &choked);
            if (choked != 0) {
                rates.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_CHOKED_FLOW));
                rates.*.choked_path_mask |= @as(u32, @bitCast(@as(c_uint, 1) << @intCast(path)));
            }
            if (rates.*.path_mass_flow_kg_per_s[path] < 0.0) {
                rates.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_REVERSE_FLOW));
            }
        }
    }
    {
        var path: usize = 0;
        _ = &path;
        while (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_COUNT)))) : (path +%= 1) {
            const flow: f64 = rates.*.path_mass_flow_kg_per_s[path];
            _ = &flow;
            if ((flow > 0.0) and (path > @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_INLET))))) {
                donor_outflow_kg_per_s[path -% @as(usize, @bitCast(@as(u64, @as(c_uint, 1))))] += flow;
            } else if ((flow < 0.0) and (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_TAILPIPE))))) {
                donor_outflow_kg_per_s[path] -= flow;
            }
        }
    }
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            const removable_mass: f64 = pwr_exhaust_max(0.0, state.*.volume[volume].total_mass_kg - parameters.*.minimum_volume_mass_kg);
            _ = &removable_mass;
            const maximum_outflow: f64 = (parameters.*.maximum_outflow_fraction_per_substep * removable_mass) / substep_s;
            _ = &maximum_outflow;
            if ((donor_outflow_kg_per_s[volume] > maximum_outflow) and (donor_outflow_kg_per_s[volume] > 0.0)) {
                donor_scale[volume] = maximum_outflow / donor_outflow_kg_per_s[volume];
                rates.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_FLOW_LIMITED));
                rates.*.flow_limit_events +%= 1;
            }
        }
    }
    {
        var path: usize = 0;
        _ = &path;
        while (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_COUNT)))) : (path +%= 1) {
            const flow: f64 = rates.*.path_mass_flow_kg_per_s[path];
            _ = &flow;
            if ((flow > 0.0) and (path > @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_INLET))))) {
                rates.*.path_mass_flow_kg_per_s[path] *= donor_scale[path -% @as(usize, @bitCast(@as(u64, @as(c_uint, 1))))];
            } else if ((flow < 0.0) and (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_TAILPIPE))))) {
                rates.*.path_mass_flow_kg_per_s[path] *= donor_scale[path];
            }
        }
    }
    {
        var path: usize = 0;
        _ = &path;
        while (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_COUNT)))) : (path +%= 1) {
            const flow: f64 = rates.*.path_mass_flow_kg_per_s[path];
            _ = &flow;
            var donor: [*c]const pwr_exhaust_gas_properties = undefined;
            _ = &donor;
            const left_volume: c_int = if (path == @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_INLET)))) -@as(c_int, 1) else @as(c_int, @bitCast(@as(c_uint, @truncate(path)))) - @as(c_int, 1);
            _ = &left_volume;
            const right_volume: c_int = if (path == @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_TAILPIPE)))) -@as(c_int, 1) else @as(c_int, @bitCast(@as(c_uint, @truncate(path))));
            _ = &right_volume;
            var enthalpy_flow: f64 = undefined;
            _ = &enthalpy_flow;
            if (flow >= 0.0) {
                donor = if (left_volume < @as(c_int, 0)) &upstream else &gas[@as(c_uint, @intCast(left_volume))];
            } else {
                donor = if (right_volume < @as(c_int, 0)) &downstream else &gas[@as(c_uint, @intCast(right_volume))];
            }
            enthalpy_flow = (flow * donor.*.cp_j_per_kg_k) * donor.*.temperature_k;
            if (left_volume >= @as(c_int, 0)) {
                rates.*.total_mass_kg_per_s[@as(c_uint, @intCast(left_volume))] -= flow;
                rates.*.internal_energy_w[@as(c_uint, @intCast(left_volume))] -= enthalpy_flow;
            }
            if (right_volume >= @as(c_int, 0)) {
                rates.*.total_mass_kg_per_s[@as(c_uint, @intCast(right_volume))] += flow;
                rates.*.internal_energy_w[@as(c_uint, @intCast(right_volume))] += enthalpy_flow;
            }
            {
                var species: usize = 0;
                _ = &species;
                while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
                    const species_flow: f64 = flow * donor.*.mass_fraction[species];
                    _ = &species_flow;
                    if (left_volume >= @as(c_int, 0)) {
                        rates.*.species_mass_kg_per_s[@as(c_uint, @intCast(left_volume))][species] -= species_flow;
                    }
                    if (right_volume >= @as(c_int, 0)) {
                        rates.*.species_mass_kg_per_s[@as(c_uint, @intCast(right_volume))][species] += species_flow;
                    }
                    if (path == @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_INLET)))) {
                        rates.*.boundary_species_net_kg_per_s[species] += species_flow;
                    } else if (path == @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_TAILPIPE)))) {
                        rates.*.boundary_species_net_kg_per_s[species] -= species_flow;
                        if (flow > 0.0) {
                            rates.*.tailpipe_species_out_kg_per_s[species] += species_flow;
                        }
                    }
                }
            }
            if (path == @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_INLET)))) {
                rates.*.boundary_mass_net_kg_per_s += flow;
                rates.*.boundary_enthalpy_net_w += enthalpy_flow;
            } else if (path == @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_TAILPIPE)))) {
                rates.*.boundary_mass_net_kg_per_s -= flow;
                rates.*.boundary_enthalpy_net_w -= enthalpy_flow;
            }
        }
    }
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            const gas_wall_heat_w: f64 = parameters.*.gas_wall_conductance_w_per_k[volume] * (state.*.volume[volume].wall_temperature_k - gas[volume].temperature_k);
            _ = &gas_wall_heat_w;
            const ambient_heat_w: f64 = parameters.*.wall_ambient_conductance_w_per_k[volume] * (inputs.*.ambient_temperature_k - state.*.volume[volume].wall_temperature_k);
            _ = &ambient_heat_w;
            rates.*.internal_energy_w[volume] += gas_wall_heat_w;
            rates.*.wall_temperature_k_per_s[volume] += (-gas_wall_heat_w + ambient_heat_w) / parameters.*.wall_heat_capacity_j_per_k[volume];
            rates.*.ambient_heat_net_w += ambient_heat_w;
        }
    }
    {
        const catalyst: usize = @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_CATALYST)));
        _ = &catalyst;
        const current_conversion: f64 = state.*.catalyst_conversion_fraction;
        _ = &current_conversion;
        const target_conversion: f64 = pwr_exhaust_activity_target(parameters, state.*.volume[catalyst].wall_temperature_k);
        _ = &target_conversion;
        var conversion_rate: f64 = (target_conversion - current_conversion) / parameters.*.catalyst_activity_time_constant_s;
        _ = &conversion_rate;
        var reductant_rate: f64 = (current_conversion * state.*.volume[catalyst].species_mass_kg[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_REDUCTANT))]) / parameters.*.catalyst_reaction_time_s;
        _ = &reductant_rate;
        const reductant_after_advection: f64 = pwr_exhaust_max(0.0, state.*.volume[catalyst].species_mass_kg[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_REDUCTANT))] + (substep_s * rates.*.species_mass_kg_per_s[catalyst][@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_REDUCTANT))]));
        _ = &reductant_after_advection;
        const oxygen_after_advection: f64 = pwr_exhaust_max(0.0, state.*.volume[catalyst].species_mass_kg[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_OXYGEN))] + (substep_s * rates.*.species_mass_kg_per_s[catalyst][@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_OXYGEN))]));
        _ = &oxygen_after_advection;
        var available_rate: f64 = reductant_after_advection / substep_s;
        _ = &available_rate;
        if (parameters.*.catalyst_oxygen_per_reductant_kg_per_kg > 0.0) {
            available_rate = pwr_exhaust_min(available_rate, oxygen_after_advection / (substep_s * parameters.*.catalyst_oxygen_per_reductant_kg_per_kg));
        }
        if (reductant_rate > available_rate) {
            reductant_rate = available_rate;
            rates.*.flags |= @as(u32, @bitCast(PWR_EXHAUST_DIAG_REACTION_LIMITED));
            rates.*.reaction_limit_events +%= 1;
        }
        reductant_rate = pwr_exhaust_max(0.0, reductant_rate);
        conversion_rate = pwr_exhaust_clamp(conversion_rate, -current_conversion / substep_s, (1.0 - current_conversion) / substep_s);
        rates.*.catalyst_conversion_per_s = conversion_rate;
        rates.*.reaction_species_kg_per_s[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_REDUCTANT))] = -reductant_rate;
        rates.*.reaction_species_kg_per_s[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_OXYGEN))] = -parameters.*.catalyst_oxygen_per_reductant_kg_per_kg * reductant_rate;
        rates.*.reaction_species_kg_per_s[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_PRODUCTS))] = (1.0 + parameters.*.catalyst_oxygen_per_reductant_kg_per_kg) * reductant_rate;
        {
            var species: usize = 0;
            _ = &species;
            while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
                rates.*.species_mass_kg_per_s[catalyst][species] += rates.*.reaction_species_kg_per_s[species];
            }
        }
        rates.*.reacted_reductant_kg_per_s = reductant_rate;
        rates.*.reaction_heat_w = reductant_rate * parameters.*.catalyst_reaction_heat_j_per_kg_reductant;
        rates.*.internal_energy_w[catalyst] += rates.*.reaction_heat_w * (1.0 - parameters.*.catalyst_heat_to_wall_fraction);
        rates.*.wall_temperature_k_per_s[catalyst] += (rates.*.reaction_heat_w * parameters.*.catalyst_heat_to_wall_fraction) / parameters.*.wall_heat_capacity_j_per_k[catalyst];
    }
    return @as(c_uint, @bitCast(if (pwr_exhaust_rates_are_finite(rates) != 0) PWR_EXHAUST_OK else PWR_EXHAUST_INVALID_STATE));
}

pub fn pwr_exhaust_euler_advance(arg_state: [*c]pwr_exhaust_state, arg_rates: [*c]const pwr_exhaust_rates, arg_substep_s: f64) callconv(.c) void {
    var state = arg_state;
    _ = &state;
    var rates = arg_rates;
    _ = &rates;
    var substep_s = arg_substep_s;
    _ = &substep_s;
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            state.*.volume[volume].total_mass_kg += substep_s * rates.*.total_mass_kg_per_s[volume];
            state.*.volume[volume].internal_energy_j += substep_s * rates.*.internal_energy_w[volume];
            state.*.volume[volume].wall_temperature_k += substep_s * rates.*.wall_temperature_k_per_s[volume];
            {
                var species: usize = 0;
                _ = &species;
                while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
                    state.*.volume[volume].species_mass_kg[species] += substep_s * rates.*.species_mass_kg_per_s[volume][species];
                }
            }
        }
    }
    state.*.catalyst_conversion_fraction += substep_s * rates.*.catalyst_conversion_per_s;
}

pub fn pwr_exhaust_apply_positivity(arg_parameters: [*c]const pwr_exhaust_parameters, arg_state: [*c]pwr_exhaust_state, arg_correction: [*c]pwr_exhaust_correction) callconv(.c) pwr_exhaust_result {
    var parameters = arg_parameters;
    _ = &parameters;
    var state = arg_state;
    _ = &state;
    var correction = arg_correction;
    _ = &correction;
    _ = memset(@as(?*anyopaque, @ptrCast(correction)), @as(c_int, 0), @sizeOf(pwr_exhaust_correction));
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            var cell: [*c]pwr_exhaust_volume_state = &state.*.volume[volume];
            _ = &cell;
            const old_total_mass: f64 = cell.*.total_mass_kg;
            _ = &old_total_mass;
            var new_total_mass: f64 = undefined;
            _ = &new_total_mass;
            var minimum_internal_energy: f64 = 0.0;
            _ = &minimum_internal_energy;
            if ((!(power_isfinite(old_total_mass) != 0) or !(power_isfinite(cell.*.internal_energy_j) != 0)) or !(power_isfinite(cell.*.wall_temperature_k) != 0)) {
                return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
            }
            {
                var species: usize = 0;
                _ = &species;
                while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
                    if (!(power_isfinite(cell.*.species_mass_kg[species]) != 0)) {
                        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
                    }
                    if (cell.*.species_mass_kg[species] < 0.0) {
                        correction.*.species_kg[species] -= cell.*.species_mass_kg[species];
                        cell.*.species_mass_kg[species] = 0.0;
                        correction.*.events +%= 1;
                    }
                }
            }
            new_total_mass = pwr_exhaust_species_sum(@as([*c]f64, @ptrCast(@alignCast(&cell.*.species_mass_kg[@as(usize, @intCast(0))]))));
            if (new_total_mass < parameters.*.minimum_volume_mass_kg) {
                const added: f64 = parameters.*.minimum_volume_mass_kg - new_total_mass;
                _ = &added;
                cell.*.species_mass_kg[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_INERT))] += added;
                correction.*.species_kg[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_INERT))] += added;
                new_total_mass += added;
                correction.*.events +%= 1;
            }
            cell.*.total_mass_kg = new_total_mass;
            correction.*.mass_kg += new_total_mass - old_total_mass;
            {
                var species: usize = 0;
                _ = &species;
                while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
                    minimum_internal_energy += (cell.*.species_mass_kg[species] * parameters.*.species_cv_j_per_kg_k[species]) * parameters.*.minimum_temperature_k;
                }
            }
            if (cell.*.internal_energy_j < minimum_internal_energy) {
                correction.*.energy_j += minimum_internal_energy - cell.*.internal_energy_j;
                cell.*.internal_energy_j = minimum_internal_energy;
                correction.*.events +%= 1;
            }
            if (cell.*.wall_temperature_k < parameters.*.minimum_temperature_k) {
                correction.*.energy_j += parameters.*.wall_heat_capacity_j_per_k[volume] * (parameters.*.minimum_temperature_k - cell.*.wall_temperature_k);
                cell.*.wall_temperature_k = parameters.*.minimum_temperature_k;
                correction.*.events +%= 1;
            }
        }
    }
    if (!(power_isfinite(state.*.catalyst_conversion_fraction) != 0)) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
    }
    if ((state.*.catalyst_conversion_fraction < 0.0) or (state.*.catalyst_conversion_fraction > 1.0)) {
        state.*.catalyst_conversion_fraction = pwr_exhaust_clamp(state.*.catalyst_conversion_fraction, 0.0, 1.0);
        correction.*.events +%= 1;
    }
    return @as(c_uint, @bitCast(PWR_EXHAUST_OK));
}

pub fn pwr_exhaust_ssp_average(arg_start: [*c]const pwr_exhaust_state, arg_second_euler: [*c]const pwr_exhaust_state, arg_result: [*c]pwr_exhaust_state) callconv(.c) void {
    var start = arg_start;
    _ = &start;
    var second_euler = arg_second_euler;
    _ = &second_euler;
    var result = arg_result;
    _ = &result;
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            result.*.volume[volume].total_mass_kg = 0.5 * (start.*.volume[volume].total_mass_kg + second_euler.*.volume[volume].total_mass_kg);
            result.*.volume[volume].internal_energy_j = 0.5 * (start.*.volume[volume].internal_energy_j + second_euler.*.volume[volume].internal_energy_j);
            result.*.volume[volume].wall_temperature_k = 0.5 * (start.*.volume[volume].wall_temperature_k + second_euler.*.volume[volume].wall_temperature_k);
            {
                var species: usize = 0;
                _ = &species;
                while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
                    result.*.volume[volume].species_mass_kg[species] = 0.5 * (start.*.volume[volume].species_mass_kg[species] + second_euler.*.volume[volume].species_mass_kg[species]);
                }
            }
        }
    }
    result.*.catalyst_conversion_fraction = 0.5 * (start.*.catalyst_conversion_fraction + second_euler.*.catalyst_conversion_fraction);
}

pub fn pwr_exhaust_accumulate_rates(arg_ledger: [*c]pwr_exhaust_ledger, arg_rates: [*c]const pwr_exhaust_rates, arg_weight_s: f64) callconv(.c) void {
    var ledger = arg_ledger;
    _ = &ledger;
    var rates = arg_rates;
    _ = &rates;
    var weight_s = arg_weight_s;
    _ = &weight_s;
    {
        var path: usize = 0;
        _ = &path;
        while (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_COUNT)))) : (path +%= 1) {
            ledger.*.path_mass_kg[path] += weight_s * rates.*.path_mass_flow_kg_per_s[path];
        }
    }
    ledger.*.boundary_mass_net_kg += weight_s * rates.*.boundary_mass_net_kg_per_s;
    ledger.*.boundary_enthalpy_net_j += weight_s * rates.*.boundary_enthalpy_net_w;
    ledger.*.ambient_heat_net_j += weight_s * rates.*.ambient_heat_net_w;
    ledger.*.reaction_heat_j += weight_s * rates.*.reaction_heat_w;
    ledger.*.reacted_reductant_kg += weight_s * rates.*.reacted_reductant_kg_per_s;
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            ledger.*.boundary_species_net_kg[species] += weight_s * rates.*.boundary_species_net_kg_per_s[species];
            ledger.*.reaction_species_kg[species] += weight_s * rates.*.reaction_species_kg_per_s[species];
            ledger.*.tailpipe_species_out_kg[species] += weight_s * rates.*.tailpipe_species_out_kg_per_s[species];
        }
    }
}

pub fn pwr_exhaust_accumulate_correction(arg_ledger: [*c]pwr_exhaust_ledger, arg_correction: [*c]const pwr_exhaust_correction, arg_weight: f64) callconv(.c) void {
    var ledger = arg_ledger;
    _ = &ledger;
    var correction = arg_correction;
    _ = &correction;
    var weight = arg_weight;
    _ = &weight;
    ledger.*.positivity_mass_adjustment_kg += weight * correction.*.mass_kg;
    ledger.*.positivity_energy_adjustment_j += weight * correction.*.energy_j;
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            ledger.*.positivity_species_adjustment_kg[species] += weight * correction.*.species_kg[species];
        }
    }
}

pub fn pwr_exhaust_validate_state(arg_parameters: [*c]const pwr_exhaust_parameters, arg_state: [*c]const pwr_exhaust_state) callconv(.c) pwr_exhaust_result {
    var parameters = arg_parameters;
    _ = &parameters;
    var state = arg_state;
    _ = &state;
    if ((((((state == null) or !(power_isfinite(state.*.time_s) != 0)) or (state.*.time_s < 0.0)) or !(power_isfinite(state.*.catalyst_conversion_fraction) != 0)) or (state.*.catalyst_conversion_fraction < 0.0)) or (state.*.catalyst_conversion_fraction > 1.0)) {
        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
    }
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            if (!(pwr_exhaust_is_nonnegative_finite(state.*.cumulative_tailpipe_species_kg[species]) != 0)) {
                return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
            }
        }
    }
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            var cell: [*c]const pwr_exhaust_volume_state = &state.*.volume[volume];
            _ = &cell;
            var species_total: f64 = undefined;
            _ = &species_total;
            var closure_tolerance: f64 = undefined;
            _ = &closure_tolerance;
            var properties: pwr_exhaust_gas_properties = undefined;
            _ = &properties;
            if ((((!(pwr_exhaust_is_positive_finite(cell.*.total_mass_kg) != 0) or (cell.*.total_mass_kg < parameters.*.minimum_volume_mass_kg)) or !(pwr_exhaust_is_positive_finite(cell.*.internal_energy_j) != 0)) or !(pwr_exhaust_is_positive_finite(cell.*.wall_temperature_k) != 0)) or (cell.*.wall_temperature_k < parameters.*.minimum_temperature_k)) {
                return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
            }
            {
                var species: usize = 0;
                _ = &species;
                while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
                    if (!(pwr_exhaust_is_nonnegative_finite(cell.*.species_mass_kg[species]) != 0)) {
                        return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
                    }
                }
            }
            species_total = pwr_exhaust_species_sum(@as([*c]const f64, @ptrCast(@alignCast(&cell.*.species_mass_kg[@as(usize, @intCast(0))]))));
            closure_tolerance = 0.000000000001 + (0.000000001 * pwr_exhaust_max(species_total, cell.*.total_mass_kg));
            if (((fabs(species_total - cell.*.total_mass_kg) > closure_tolerance) or (pwr_exhaust_volume_properties(parameters, cell, volume, &properties) != @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) or (properties.temperature_k < parameters.*.minimum_temperature_k)) {
                return @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_STATE));
            }
        }
    }
    return @as(c_uint, @bitCast(PWR_EXHAUST_OK));
}
