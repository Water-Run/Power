// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Ported from legacy/native/tests/test_exhaust.c at c342d4c.
// Original equations and regression assertions are preserved; see migration-manifest.json.

const PWR_EXHAUST_DIAG_CATALYST_LIGHT_OFF = @import("../src/abi.zig").PWR_EXHAUST_DIAG_CATALYST_LIGHT_OFF;
const PWR_EXHAUST_DIAG_CHOKED_FLOW = @import("../src/abi.zig").PWR_EXHAUST_DIAG_CHOKED_FLOW;
const PWR_EXHAUST_DIAG_INPUT_REJECTED = @import("../src/abi.zig").PWR_EXHAUST_DIAG_INPUT_REJECTED;
const PWR_EXHAUST_DIAG_REVERSE_FLOW = @import("../src/abi.zig").PWR_EXHAUST_DIAG_REVERSE_FLOW;
const PWR_EXHAUST_INVALID_PARAMETER = @import("../src/abi.zig").PWR_EXHAUST_INVALID_PARAMETER;
const PWR_EXHAUST_INVALID_TIMESTEP = @import("../src/abi.zig").PWR_EXHAUST_INVALID_TIMESTEP;
const PWR_EXHAUST_OK = @import("../src/abi.zig").PWR_EXHAUST_OK;
const PWR_EXHAUST_PATH_CATALYST_TO_MUFFLER = @import("../src/abi.zig").PWR_EXHAUST_PATH_CATALYST_TO_MUFFLER;
const PWR_EXHAUST_PATH_COUNT = @import("../src/abi.zig").PWR_EXHAUST_PATH_COUNT;
const PWR_EXHAUST_PATH_INLET = @import("../src/abi.zig").PWR_EXHAUST_PATH_INLET;
const PWR_EXHAUST_PATH_MANIFOLD_TO_CATALYST = @import("../src/abi.zig").PWR_EXHAUST_PATH_MANIFOLD_TO_CATALYST;
const PWR_EXHAUST_PATH_TAILPIPE = @import("../src/abi.zig").PWR_EXHAUST_PATH_TAILPIPE;
const PWR_EXHAUST_SPECIES_COUNT = @import("../src/abi.zig").PWR_EXHAUST_SPECIES_COUNT;
const PWR_EXHAUST_SPECIES_INERT = @import("../src/abi.zig").PWR_EXHAUST_SPECIES_INERT;
const PWR_EXHAUST_SPECIES_OXYGEN = @import("../src/abi.zig").PWR_EXHAUST_SPECIES_OXYGEN;
const PWR_EXHAUST_SPECIES_PRODUCTS = @import("../src/abi.zig").PWR_EXHAUST_SPECIES_PRODUCTS;
const PWR_EXHAUST_SPECIES_REDUCTANT = @import("../src/abi.zig").PWR_EXHAUST_SPECIES_REDUCTANT;
const PWR_EXHAUST_VOLUME_CATALYST = @import("../src/abi.zig").PWR_EXHAUST_VOLUME_CATALYST;
const PWR_EXHAUST_VOLUME_COUNT = @import("../src/abi.zig").PWR_EXHAUST_VOLUME_COUNT;
const PWR_EXHAUST_VOLUME_MANIFOLD = @import("../src/abi.zig").PWR_EXHAUST_VOLUME_MANIFOLD;
const PWR_EXHAUST_VOLUME_MUFFLER = @import("../src/abi.zig").PWR_EXHAUST_VOLUME_MUFFLER;
const fabs = @import("../src/support.zig").fabs;
const fprintf = @import("../src/support.zig").fprintf;
const memcmp = @import("../src/support.zig").memcmp;
const puts = @import("../src/support.zig").puts;
const pwr_exhaust_diagnostics = @import("../src/abi.zig").pwr_exhaust_diagnostics;
const pwr_exhaust_initialize_uniform = @import("../src/models/exhaust/pwr_exhaust.zig").pwr_exhaust_initialize_uniform;
const pwr_exhaust_inputs = @import("../src/abi.zig").pwr_exhaust_inputs;
const pwr_exhaust_observation = @import("../src/abi.zig").pwr_exhaust_observation;
const pwr_exhaust_parameters = @import("../src/abi.zig").pwr_exhaust_parameters;
const pwr_exhaust_reservoir = @import("../src/abi.zig").pwr_exhaust_reservoir;
const pwr_exhaust_state = @import("../src/abi.zig").pwr_exhaust_state;
const pwr_exhaust_step = @import("../src/models/exhaust/pwr_exhaust.zig").pwr_exhaust_step;
const pwr_exhaust_validate_parameters = @import("../src/models/exhaust/pwr_exhaust.zig").pwr_exhaust_validate_parameters;

pub fn test_parameters() callconv(.c) pwr_exhaust_parameters {
    var parameters: pwr_exhaust_parameters = pwr_exhaust_parameters{
        .volume_m3 = [1]f64{
            0,
        } ++ [1]f64{0} ** 2,
        .wall_heat_capacity_j_per_k = @import("std").mem.zeroes([3]f64),
        .gas_wall_conductance_w_per_k = @import("std").mem.zeroes([3]f64),
        .wall_ambient_conductance_w_per_k = @import("std").mem.zeroes([3]f64),
        .orifice_area_m2 = @import("std").mem.zeroes([4]f64),
        .discharge_coefficient = @import("std").mem.zeroes([4]f64),
        .species_gas_constant_j_per_kg_k = @import("std").mem.zeroes([4]f64),
        .species_cv_j_per_kg_k = @import("std").mem.zeroes([4]f64),
        .catalyst_light_off_temperature_k = 0,
        .catalyst_full_activity_temperature_k = 0,
        .catalyst_activity_time_constant_s = 0,
        .catalyst_reaction_time_s = 0,
        .catalyst_oxygen_per_reductant_kg_per_kg = 0,
        .catalyst_reaction_heat_j_per_kg_reductant = 0,
        .catalyst_heat_to_wall_fraction = 0,
        .minimum_temperature_k = 0,
        .minimum_volume_mass_kg = 0,
        .maximum_substep_s = 0,
        .maximum_outflow_fraction_per_substep = 0,
        .maximum_substeps = @import("std").mem.zeroes(u32),
    };
    _ = &parameters;
    parameters.volume_m3[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] = 0.0015;
    parameters.volume_m3[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 0.0025;
    parameters.volume_m3[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MUFFLER))] = 0.004;
    parameters.wall_heat_capacity_j_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] = 300.0;
    parameters.wall_heat_capacity_j_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 180.0;
    parameters.wall_heat_capacity_j_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MUFFLER))] = 500.0;
    parameters.gas_wall_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] = 18.0;
    parameters.gas_wall_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 75.0;
    parameters.gas_wall_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MUFFLER))] = 15.0;
    parameters.wall_ambient_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] = 1.0;
    parameters.wall_ambient_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 1.5;
    parameters.wall_ambient_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MUFFLER))] = 2.0;
    parameters.orifice_area_m2[@as(c_uint, @intCast(PWR_EXHAUST_PATH_INLET))] = 0.00006;
    parameters.orifice_area_m2[@as(c_uint, @intCast(PWR_EXHAUST_PATH_MANIFOLD_TO_CATALYST))] = 0.00005;
    parameters.orifice_area_m2[@as(c_uint, @intCast(PWR_EXHAUST_PATH_CATALYST_TO_MUFFLER))] = 0.00006;
    parameters.orifice_area_m2[@as(c_uint, @intCast(PWR_EXHAUST_PATH_TAILPIPE))] = 0.00008;
    {
        var path: usize = 0;
        _ = &path;
        while (path < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_PATH_COUNT)))) : (path +%= 1) {
            parameters.discharge_coefficient[path] = 0.72;
        }
    }
    parameters.species_gas_constant_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_INERT))] = 287.0;
    parameters.species_gas_constant_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_OXYGEN))] = 260.0;
    parameters.species_gas_constant_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_REDUCTANT))] = 245.0;
    parameters.species_gas_constant_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_PRODUCTS))] = 190.0;
    parameters.species_cv_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_INERT))] = 720.0;
    parameters.species_cv_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_OXYGEN))] = 660.0;
    parameters.species_cv_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_REDUCTANT))] = 1500.0;
    parameters.species_cv_j_per_kg_k[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_PRODUCTS))] = 900.0;
    parameters.catalyst_light_off_temperature_k = 500.0;
    parameters.catalyst_full_activity_temperature_k = 680.0;
    parameters.catalyst_activity_time_constant_s = 0.35;
    parameters.catalyst_reaction_time_s = 0.04;
    parameters.catalyst_oxygen_per_reductant_kg_per_kg = 3.0;
    parameters.catalyst_reaction_heat_j_per_kg_reductant = 20000000.0;
    parameters.catalyst_heat_to_wall_fraction = 0.75;
    parameters.minimum_temperature_k = 150.0;
    parameters.minimum_volume_mass_kg = 0.000000001;
    parameters.maximum_substep_s = 0.0005;
    parameters.maximum_outflow_fraction_per_substep = 0.1;
    parameters.maximum_substeps = 1000;
    return parameters;
}

pub fn air_reservoir(arg_pressure_pa: f64, arg_temperature_k: f64) callconv(.c) pwr_exhaust_reservoir {
    var pressure_pa = arg_pressure_pa;
    _ = &pressure_pa;
    var temperature_k = arg_temperature_k;
    _ = &temperature_k;
    var reservoir: pwr_exhaust_reservoir = pwr_exhaust_reservoir{
        .pressure_pa = @as(f64, @floatFromInt(@as(c_int, 0))),
        .temperature_k = 0,
        .species_mass_fraction = @import("std").mem.zeroes([4]f64),
    };
    _ = &reservoir;
    reservoir.pressure_pa = pressure_pa;
    reservoir.temperature_k = temperature_k;
    reservoir.species_mass_fraction[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_INERT))] = 0.77;
    reservoir.species_mass_fraction[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_OXYGEN))] = 0.23;
    return reservoir;
}

pub fn hot_exhaust_reservoir(arg_pressure_pa: f64, arg_temperature_k: f64) callconv(.c) pwr_exhaust_reservoir {
    var pressure_pa = arg_pressure_pa;
    _ = &pressure_pa;
    var temperature_k = arg_temperature_k;
    _ = &temperature_k;
    var reservoir: pwr_exhaust_reservoir = pwr_exhaust_reservoir{
        .pressure_pa = @as(f64, @floatFromInt(@as(c_int, 0))),
        .temperature_k = 0,
        .species_mass_fraction = @import("std").mem.zeroes([4]f64),
    };
    _ = &reservoir;
    reservoir.pressure_pa = pressure_pa;
    reservoir.temperature_k = temperature_k;
    reservoir.species_mass_fraction[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_INERT))] = 0.76;
    reservoir.species_mass_fraction[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_OXYGEN))] = 0.14;
    reservoir.species_mass_fraction[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_REDUCTANT))] = 0.01;
    reservoir.species_mass_fraction[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_PRODUCTS))] = 0.09;
    return reservoir;
}

pub fn total_mass(arg_state: [*c]const pwr_exhaust_state) callconv(.c) f64 {
    var state = arg_state;
    _ = &state;
    var result: f64 = 0.0;
    _ = &result;
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            result += state.*.volume[volume].total_mass_kg;
        }
    }
    return result;
}

pub fn total_species_mass(arg_state: [*c]const pwr_exhaust_state, arg_species: usize) callconv(.c) f64 {
    var state = arg_state;
    _ = &state;
    var species = arg_species;
    _ = &species;
    var result: f64 = 0.0;
    _ = &result;
    {
        var volume: usize = 0;
        _ = &volume;
        while (volume < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_VOLUME_COUNT)))) : (volume +%= 1) {
            result += state.*.volume[volume].species_mass_kg[species];
        }
    }
    return result;
}

pub fn test_parameter_and_step_rejection_is_transactional() callconv(.c) c_int {
    var parameters: pwr_exhaust_parameters = test_parameters();
    _ = &parameters;
    const ambient: pwr_exhaust_reservoir = air_reservoir(101325.0, 300.0);
    _ = &ambient;
    var state: pwr_exhaust_state = undefined;
    _ = &state;
    var before: pwr_exhaust_state = undefined;
    _ = &before;
    var inputs: pwr_exhaust_inputs = pwr_exhaust_inputs{
        .upstream = ambient,
        .downstream = ambient,
        .inlet_area_scale = 1.0,
        .tailpipe_area_scale = 1.0,
        .ambient_temperature_k = 300.0,
    };
    _ = &inputs;
    var diagnostics: pwr_exhaust_diagnostics = undefined;
    _ = &diagnostics;
    while (true) {
        if (!(pwr_exhaust_validate_parameters(&parameters) == @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 145), "pwr_exhaust_validate_parameters(&parameters) == PWR_EXHAUST_OK" });
            return 0;
        }
        if (!false) break;
    }
    parameters.volume_m3[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 0.0;
    while (true) {
        if (!(pwr_exhaust_validate_parameters(&parameters) == @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_PARAMETER)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 148), "pwr_exhaust_validate_parameters(&parameters) == PWR_EXHAUST_INVALID_PARAMETER" });
            return 0;
        }
        if (!false) break;
    }
    parameters = test_parameters();
    while (true) {
        if (!(pwr_exhaust_initialize_uniform(&parameters, &ambient, 300.0, &state) == @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 152), "pwr_exhaust_initialize_uniform( &parameters, &ambient, 300.0, &state) == PWR_EXHAUST_OK" });
            return 0;
        }
        if (!false) break;
    }
    before = state;
    while (true) {
        if (!(pwr_exhaust_step(&parameters, &state, &inputs, -0.01, &diagnostics) == @as(c_uint, @bitCast(PWR_EXHAUST_INVALID_TIMESTEP)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 155), "pwr_exhaust_step(&parameters, &state, &inputs, -0.01, &diagnostics) == PWR_EXHAUST_INVALID_TIMESTEP" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!((diagnostics.flags & @as(u32, @bitCast(PWR_EXHAUST_DIAG_INPUT_REJECTED))) != @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 156), "(diagnostics.flags & PWR_EXHAUST_DIAG_INPUT_REJECTED) != 0u" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(memcmp(@as(?*const anyopaque, @ptrCast(&before)), @as(?*const anyopaque, @ptrCast(&state)), @sizeOf(pwr_exhaust_state)) == @as(c_int, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 157), "memcmp(&before, &state, sizeof(state)) == 0" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

pub fn test_closed_network_conserves_total_and_species_mass() callconv(.c) c_int {
    var parameters: pwr_exhaust_parameters = test_parameters();
    _ = &parameters;
    const initial: pwr_exhaust_reservoir = air_reservoir(120000.0, 550.0);
    _ = &initial;
    var state: pwr_exhaust_state = undefined;
    _ = &state;
    var inputs: pwr_exhaust_inputs = pwr_exhaust_inputs{
        .upstream = initial,
        .downstream = initial,
        .inlet_area_scale = 0.0,
        .tailpipe_area_scale = 0.0,
        .ambient_temperature_k = 550.0,
    };
    _ = &inputs;
    var initial_total: f64 = undefined;
    _ = &initial_total;
    var initial_species: [4]f64 = undefined;
    _ = &initial_species;
    parameters.wall_ambient_conductance_w_per_k[@as(c_uint, @intCast(@as(c_int, 0)))] = 0.0;
    parameters.wall_ambient_conductance_w_per_k[@as(c_uint, @intCast(@as(c_int, 1)))] = 0.0;
    parameters.wall_ambient_conductance_w_per_k[@as(c_uint, @intCast(@as(c_int, 2)))] = 0.0;
    while (true) {
        if (!(pwr_exhaust_initialize_uniform(&parameters, &initial, 550.0, &state) == @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 181), "pwr_exhaust_initialize_uniform( &parameters, &initial, 550.0, &state) == PWR_EXHAUST_OK" });
            return 0;
        }
        if (!false) break;
    }
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            state.volume[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))].species_mass_kg[species] *= 1.35;
            state.volume[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))].species_mass_kg[species] *= 0.75;
        }
    }
    state.volume[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))].total_mass_kg *= 1.35;
    state.volume[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))].internal_energy_j *= 1.35;
    state.volume[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))].total_mass_kg *= 0.75;
    state.volume[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))].internal_energy_j *= 0.75;
    initial_total = total_mass(&state);
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            initial_species[species] = total_species_mass(&state, species);
        }
    }
    {
        var iteration: usize = 0;
        _ = &iteration;
        while (iteration < @as(usize, @bitCast(@as(u64, @as(c_uint, 1000))))) : (iteration +%= 1) {
            var diagnostics: pwr_exhaust_diagnostics = undefined;
            _ = &diagnostics;
            while (true) {
                if (!(pwr_exhaust_step(&parameters, &state, &inputs, 0.002, &diagnostics) == @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 204), "pwr_exhaust_step( &parameters, &state, &inputs, 0.002, &diagnostics) == PWR_EXHAUST_OK" });
                    return 0;
                }
                if (!false) break;
            }
            while (true) {
                if (!(fabs(diagnostics.mass_residual_kg) < 0.00000000000002)) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 205), "fabs(diagnostics.mass_residual_kg) < 2.0e-14" });
                    return 0;
                }
                if (!false) break;
            }
            while (true) {
                if (!(fabs(diagnostics.energy_residual_j) < 0.0000002)) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 206), "fabs(diagnostics.energy_residual_j) < 2.0e-7" });
                    return 0;
                }
                if (!false) break;
            }
            while (true) {
                if (!(diagnostics.positivity_correction_events == @as(c_uint, 0))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 207), "diagnostics.positivity_correction_events == 0u" });
                    return 0;
                }
                if (!false) break;
            }
            while (true) {
                if (!(fabs(diagnostics.state_species_closure_residual_kg) < 0.00000000000002)) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 208), "fabs(diagnostics.state_species_closure_residual_kg) < 2.0e-14" });
                    return 0;
                }
                if (!false) break;
            }
        }
    }
    while (true) {
        if (!(fabs(total_mass(&state) - initial_total) < 0.000000000002)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 211), "fabs(total_mass(&state) - initial_total) < 2.0e-12" });
            return 0;
        }
        if (!false) break;
    }
    {
        var species: usize = 0;
        _ = &species;
        while (species < @as(usize, @bitCast(@as(i64, PWR_EXHAUST_SPECIES_COUNT)))) : (species +%= 1) {
            while (true) {
                if (!(fabs(total_species_mass(&state, species) - initial_species[species]) < 0.000000000002)) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 214), "fabs(total_species_mass(&state, species) - initial_species[species]) < 2.0e-12" });
                    return 0;
                }
                if (!false) break;
            }
        }
    }
    return 1;
}

pub fn test_orifice_reports_reverse_and_choked_flow() callconv(.c) c_int {
    const parameters: pwr_exhaust_parameters = test_parameters();
    _ = &parameters;
    const initial: pwr_exhaust_reservoir = air_reservoir(101325.0, 300.0);
    _ = &initial;
    var state: pwr_exhaust_state = undefined;
    _ = &state;
    var inputs: pwr_exhaust_inputs = pwr_exhaust_inputs{
        .upstream = air_reservoir(30000.0, 300.0),
        .downstream = initial,
        .inlet_area_scale = 1.0,
        .tailpipe_area_scale = 0.0,
        .ambient_temperature_k = 300.0,
    };
    _ = &inputs;
    var diagnostics: pwr_exhaust_diagnostics = undefined;
    _ = &diagnostics;
    while (true) {
        if (!(pwr_exhaust_initialize_uniform(&parameters, &initial, 300.0, &state) == @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 235), "pwr_exhaust_initialize_uniform( &parameters, &initial, 300.0, &state) == PWR_EXHAUST_OK" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(pwr_exhaust_step(&parameters, &state, &inputs, 0.0005, &diagnostics) == @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 238), "pwr_exhaust_step( &parameters, &state, &inputs, 0.0005, &diagnostics) == PWR_EXHAUST_OK" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(diagnostics.average_path_mass_flow_kg_per_s[@as(c_uint, @intCast(PWR_EXHAUST_PATH_INLET))] < 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 240), "diagnostics.average_path_mass_flow_kg_per_s[PWR_EXHAUST_PATH_INLET] < 0.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!((diagnostics.flags & @as(u32, @bitCast(PWR_EXHAUST_DIAG_REVERSE_FLOW))) != @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 241), "(diagnostics.flags & PWR_EXHAUST_DIAG_REVERSE_FLOW) != 0u" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!((diagnostics.flags & @as(u32, @bitCast(PWR_EXHAUST_DIAG_CHOKED_FLOW))) != @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 242), "(diagnostics.flags & PWR_EXHAUST_DIAG_CHOKED_FLOW) != 0u" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!((diagnostics.choked_path_mask & (@as(c_uint, 1) << @intCast(PWR_EXHAUST_PATH_INLET))) != @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 245), "(diagnostics.choked_path_mask & (1u << PWR_EXHAUST_PATH_INLET)) != 0u" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(diagnostics.boundary_mass_net_kg < 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 246), "diagnostics.boundary_mass_net_kg < 0.0" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

pub fn run_backpressure_case(arg_tailpipe_area_scale: f64, arg_observation: [*c]pwr_exhaust_observation, arg_final_inlet_flow_kg_per_s: [*c]f64, arg_final_tailpipe_flow_kg_per_s: [*c]f64) callconv(.c) c_int {
    var tailpipe_area_scale = arg_tailpipe_area_scale;
    _ = &tailpipe_area_scale;
    var observation = arg_observation;
    _ = &observation;
    var final_inlet_flow_kg_per_s = arg_final_inlet_flow_kg_per_s;
    _ = &final_inlet_flow_kg_per_s;
    var final_tailpipe_flow_kg_per_s = arg_final_tailpipe_flow_kg_per_s;
    _ = &final_tailpipe_flow_kg_per_s;
    const parameters: pwr_exhaust_parameters = test_parameters();
    _ = &parameters;
    const ambient: pwr_exhaust_reservoir = air_reservoir(101325.0, 300.0);
    _ = &ambient;
    var state: pwr_exhaust_state = undefined;
    _ = &state;
    var inputs: pwr_exhaust_inputs = pwr_exhaust_inputs{
        .upstream = hot_exhaust_reservoir(180000.0, 850.0),
        .downstream = ambient,
        .inlet_area_scale = 1.0,
        .tailpipe_area_scale = tailpipe_area_scale,
        .ambient_temperature_k = 300.0,
    };
    _ = &inputs;
    var diagnostics: pwr_exhaust_diagnostics = pwr_exhaust_diagnostics{
        .result = @as(c_uint, @bitCast(@as(c_int, 0))),
        .flags = @import("std").mem.zeroes(u32),
        .substeps = @import("std").mem.zeroes(u32),
        .flow_limit_events = @import("std").mem.zeroes(u32),
        .reaction_limit_events = @import("std").mem.zeroes(u32),
        .positivity_correction_events = @import("std").mem.zeroes(u32),
        .average_path_mass_flow_kg_per_s = @import("std").mem.zeroes([4]f64),
        .choked_path_mask = @import("std").mem.zeroes(u32),
        .boundary_mass_net_kg = 0,
        .boundary_species_net_kg = @import("std").mem.zeroes([4]f64),
        .boundary_enthalpy_net_j = 0,
        .ambient_heat_net_j = 0,
        .reaction_heat_j = 0,
        .reacted_reductant_kg = 0,
        .tailpipe_species_out_kg = @import("std").mem.zeroes([4]f64),
        .mass_residual_kg = 0,
        .species_mass_residual_kg = @import("std").mem.zeroes([4]f64),
        .energy_residual_j = 0,
        .positivity_mass_adjustment_kg = 0,
        .positivity_species_adjustment_kg = @import("std").mem.zeroes([4]f64),
        .positivity_energy_adjustment_j = 0,
        .state_species_closure_residual_kg = 0,
        .final_observation = @import("std").mem.zeroes(pwr_exhaust_observation),
    };
    _ = &diagnostics;
    while (true) {
        if (!(pwr_exhaust_initialize_uniform(&parameters, &ambient, 300.0, &state) == @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 270), "pwr_exhaust_initialize_uniform( &parameters, &ambient, 300.0, &state) == PWR_EXHAUST_OK" });
            return 0;
        }
        if (!false) break;
    }
    {
        var iteration: usize = 0;
        _ = &iteration;
        while (iteration < @as(usize, @bitCast(@as(u64, @as(c_uint, 1200))))) : (iteration +%= 1) {
            while (true) {
                if (!(pwr_exhaust_step(&parameters, &state, &inputs, 0.005, &diagnostics) == @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 274), "pwr_exhaust_step( &parameters, &state, &inputs, 0.005, &diagnostics) == PWR_EXHAUST_OK" });
                    return 0;
                }
                if (!false) break;
            }
        }
    }
    observation.* = diagnostics.final_observation;
    final_inlet_flow_kg_per_s.* = diagnostics.average_path_mass_flow_kg_per_s[@as(c_uint, @intCast(PWR_EXHAUST_PATH_INLET))];
    final_tailpipe_flow_kg_per_s.* = diagnostics.average_path_mass_flow_kg_per_s[@as(c_uint, @intCast(PWR_EXHAUST_PATH_TAILPIPE))];
    return 1;
}

pub fn test_tailpipe_blockage_raises_upstream_backpressure() callconv(.c) c_int {
    var open_observation: pwr_exhaust_observation = undefined;
    _ = &open_observation;
    var blocked_observation: pwr_exhaust_observation = undefined;
    _ = &blocked_observation;
    var open_inlet_flow: f64 = undefined;
    _ = &open_inlet_flow;
    var open_tailpipe_flow: f64 = undefined;
    _ = &open_tailpipe_flow;
    var blocked_inlet_flow: f64 = undefined;
    _ = &blocked_inlet_flow;
    var blocked_tailpipe_flow: f64 = undefined;
    _ = &blocked_tailpipe_flow;
    while (true) {
        if (!(run_backpressure_case(1.0, &open_observation, &open_inlet_flow, &open_tailpipe_flow) != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 294), "run_backpressure_case( 1.0, &open_observation, &open_inlet_flow, &open_tailpipe_flow)" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(run_backpressure_case(0.04, &blocked_observation, &blocked_inlet_flow, &blocked_tailpipe_flow) != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 299), "run_backpressure_case( 0.04, &blocked_observation, &blocked_inlet_flow, &blocked_tailpipe_flow)" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(blocked_observation.pressure_pa[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] > (open_observation.pressure_pa[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] + 5000.0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 302), "blocked_observation.pressure_pa[PWR_EXHAUST_VOLUME_MANIFOLD] > open_observation.pressure_pa[PWR_EXHAUST_VOLUME_MANIFOLD] + 5000.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(blocked_observation.pressure_pa[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] > (open_observation.pressure_pa[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] + 5000.0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 304), "blocked_observation.pressure_pa[PWR_EXHAUST_VOLUME_CATALYST] > open_observation.pressure_pa[PWR_EXHAUST_VOLUME_CATALYST] + 5000.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(blocked_inlet_flow < open_inlet_flow)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 305), "blocked_inlet_flow < open_inlet_flow" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(blocked_tailpipe_flow < (0.3 * open_tailpipe_flow))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 306), "blocked_tailpipe_flow < 0.30 * open_tailpipe_flow" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

pub fn test_catalyst_warms_and_reaches_light_off() callconv(.c) c_int {
    var parameters: pwr_exhaust_parameters = test_parameters();
    _ = &parameters;
    const ambient: pwr_exhaust_reservoir = air_reservoir(101325.0, 300.0);
    _ = &ambient;
    var state: pwr_exhaust_state = undefined;
    _ = &state;
    var inputs: pwr_exhaust_inputs = pwr_exhaust_inputs{
        .upstream = hot_exhaust_reservoir(180000.0, 1000.0),
        .downstream = ambient,
        .inlet_area_scale = 1.0,
        .tailpipe_area_scale = 1.0,
        .ambient_temperature_k = 300.0,
    };
    _ = &inputs;
    var diagnostics: pwr_exhaust_diagnostics = pwr_exhaust_diagnostics{
        .result = @as(c_uint, @bitCast(@as(c_int, 0))),
        .flags = @import("std").mem.zeroes(u32),
        .substeps = @import("std").mem.zeroes(u32),
        .flow_limit_events = @import("std").mem.zeroes(u32),
        .reaction_limit_events = @import("std").mem.zeroes(u32),
        .positivity_correction_events = @import("std").mem.zeroes(u32),
        .average_path_mass_flow_kg_per_s = @import("std").mem.zeroes([4]f64),
        .choked_path_mask = @import("std").mem.zeroes(u32),
        .boundary_mass_net_kg = 0,
        .boundary_species_net_kg = @import("std").mem.zeroes([4]f64),
        .boundary_enthalpy_net_j = 0,
        .ambient_heat_net_j = 0,
        .reaction_heat_j = 0,
        .reacted_reductant_kg = 0,
        .tailpipe_species_out_kg = @import("std").mem.zeroes([4]f64),
        .mass_residual_kg = 0,
        .species_mass_residual_kg = @import("std").mem.zeroes([4]f64),
        .energy_residual_j = 0,
        .positivity_mass_adjustment_kg = 0,
        .positivity_species_adjustment_kg = @import("std").mem.zeroes([4]f64),
        .positivity_energy_adjustment_j = 0,
        .state_species_closure_residual_kg = 0,
        .final_observation = @import("std").mem.zeroes(pwr_exhaust_observation),
    };
    _ = &diagnostics;
    var reacted_first_second: f64 = 0.0;
    _ = &reacted_first_second;
    var reacted_total: f64 = 0.0;
    _ = &reacted_total;
    var light_off_time_s: f64 = -1.0;
    _ = &light_off_time_s;
    parameters.wall_heat_capacity_j_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 60.0;
    parameters.gas_wall_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_MANIFOLD))] = 5.0;
    parameters.gas_wall_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 120.0;
    parameters.wall_ambient_conductance_w_per_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] = 0.3;
    while (true) {
        if (!(pwr_exhaust_initialize_uniform(&parameters, &ambient, 300.0, &state) == @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 336), "pwr_exhaust_initialize_uniform( &parameters, &ambient, 300.0, &state) == PWR_EXHAUST_OK" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(state.catalyst_conversion_fraction == 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 337), "state.catalyst_conversion_fraction == 0.0" });
            return 0;
        }
        if (!false) break;
    }
    {
        var iteration: usize = 0;
        _ = &iteration;
        while (iteration < @as(usize, @bitCast(@as(u64, @as(c_uint, 3000))))) : (iteration +%= 1) {
            while (true) {
                if (!(pwr_exhaust_step(&parameters, &state, &inputs, 0.005, &diagnostics) == @as(c_uint, @bitCast(PWR_EXHAUST_OK)))) {
                    _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 342), "pwr_exhaust_step( &parameters, &state, &inputs, 0.005, &diagnostics) == PWR_EXHAUST_OK" });
                    return 0;
                }
                if (!false) break;
            }
            reacted_total += diagnostics.reacted_reductant_kg;
            if (state.time_s <= 1.0) {
                reacted_first_second += diagnostics.reacted_reductant_kg;
            }
            if ((light_off_time_s < 0.0) and (state.catalyst_conversion_fraction >= 0.5)) {
                light_off_time_s = state.time_s;
            }
        }
    }
    while (true) {
        if (!(diagnostics.final_observation.wall_temperature_k[@as(c_uint, @intCast(PWR_EXHAUST_VOLUME_CATALYST))] > parameters.catalyst_light_off_temperature_k)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 355), "diagnostics.final_observation .wall_temperature_k[PWR_EXHAUST_VOLUME_CATALYST] > parameters.catalyst_light_off_temperature_k" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(state.catalyst_conversion_fraction > 0.7)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 356), "state.catalyst_conversion_fraction > 0.70" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(light_off_time_s > 1.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 357), "light_off_time_s > 1.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(light_off_time_s < state.time_s)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 358), "light_off_time_s < state.time_s" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(reacted_total > (5.0 * reacted_first_second))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 359), "reacted_total > 5.0 * reacted_first_second" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(reacted_total > 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 360), "reacted_total > 0.0" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!((diagnostics.flags & @as(u32, @bitCast(PWR_EXHAUST_DIAG_CATALYST_LIGHT_OFF))) != @as(c_uint, 0))) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 361), "(diagnostics.flags & PWR_EXHAUST_DIAG_CATALYST_LIGHT_OFF) != 0u" });
            return 0;
        }
        if (!false) break;
    }
    while (true) {
        if (!(state.cumulative_tailpipe_species_kg[@as(c_uint, @intCast(PWR_EXHAUST_SPECIES_REDUCTANT))] > 0.0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 363), "state.cumulative_tailpipe_species_kg[PWR_EXHAUST_SPECIES_REDUCTANT] > 0.0" });
            return 0;
        }
        if (!false) break;
    }
    return 1;
}

pub fn run() c_int {
    while (true) {
        if (!(test_parameter_and_step_rejection_is_transactional() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 369), "test_parameter_and_step_rejection_is_transactional()" });
            return 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(test_closed_network_conserves_total_and_species_mass() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 370), "test_closed_network_conserves_total_and_species_mass()" });
            return 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(test_orifice_reports_reverse_and_choked_flow() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 371), "test_orifice_reports_reverse_and_choked_flow()" });
            return 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(test_tailpipe_blockage_raises_upstream_backpressure() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 372), "test_tailpipe_blockage_raises_upstream_backpressure()" });
            return 1;
        }
        if (!false) break;
    }
    while (true) {
        if (!(test_catalyst_warms_and_reaches_light_off() != 0)) {
            _ = fprintf("{s}:{d}: check failed: {s}\n", .{ "legacy/native/tests/test_exhaust.c", @as(c_int, 373), "test_catalyst_warms_and_reaches_light_off()" });
            return 1;
        }
        if (!false) break;
    }
    _ = puts("exhaust model tests passed");
    return 0;
}

pub fn main() void {
    @import("std").process.exit(@intCast(run()));
}
