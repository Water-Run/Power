// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

#ifndef PWR_MODELS_EXHAUST_PWR_EXHAUST_H
#define PWR_MODELS_EXHAUST_PWR_EXHAUST_H

/*
 * Internal reduced-order exhaust-network model.
 *
 * This is deliberately not part of Power!'s public ABI.  It is a conservative
 * three-control-volume building block for the first Exhaust Flow Lab.  Values
 * supplied through pwr_exhaust_parameters are calibration inputs, not claimed
 * vehicle or emissions-certification data.
 */

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

enum {
    PWR_EXHAUST_VOLUME_COUNT = 3,
    PWR_EXHAUST_PATH_COUNT = PWR_EXHAUST_VOLUME_COUNT + 1
};

typedef enum pwr_exhaust_species {
    PWR_EXHAUST_SPECIES_INERT = 0,
    PWR_EXHAUST_SPECIES_OXYGEN,
    PWR_EXHAUST_SPECIES_REDUCTANT,
    PWR_EXHAUST_SPECIES_PRODUCTS,
    PWR_EXHAUST_SPECIES_COUNT
} pwr_exhaust_species;

typedef enum pwr_exhaust_volume {
    PWR_EXHAUST_VOLUME_MANIFOLD = 0,
    PWR_EXHAUST_VOLUME_CATALYST,
    PWR_EXHAUST_VOLUME_MUFFLER
} pwr_exhaust_volume;

typedef enum pwr_exhaust_path {
    PWR_EXHAUST_PATH_INLET = 0,
    PWR_EXHAUST_PATH_MANIFOLD_TO_CATALYST,
    PWR_EXHAUST_PATH_CATALYST_TO_MUFFLER,
    PWR_EXHAUST_PATH_TAILPIPE
} pwr_exhaust_path;

typedef enum pwr_exhaust_result {
    PWR_EXHAUST_OK = 0,
    PWR_EXHAUST_INVALID_ARGUMENT,
    PWR_EXHAUST_INVALID_PARAMETER,
    PWR_EXHAUST_INVALID_BOUNDARY,
    PWR_EXHAUST_INVALID_STATE,
    PWR_EXHAUST_INVALID_TIMESTEP,
    PWR_EXHAUST_SUBSTEP_LIMIT
} pwr_exhaust_result;

typedef enum pwr_exhaust_diagnostic_flag {
    PWR_EXHAUST_DIAG_NONE = 0u,
    PWR_EXHAUST_DIAG_CHOKED_FLOW = 1u << 0,
    PWR_EXHAUST_DIAG_REVERSE_FLOW = 1u << 1,
    PWR_EXHAUST_DIAG_FLOW_LIMITED = 1u << 2,
    PWR_EXHAUST_DIAG_REACTION_LIMITED = 1u << 3,
    PWR_EXHAUST_DIAG_POSITIVITY_CORRECTION = 1u << 4,
    PWR_EXHAUST_DIAG_CATALYST_LIGHT_OFF = 1u << 5,
    PWR_EXHAUST_DIAG_INPUT_REJECTED = 1u << 6,
    PWR_EXHAUST_DIAG_NUMERIC_FAILURE = 1u << 7
} pwr_exhaust_diagnostic_flag;

typedef struct pwr_exhaust_parameters {
    double volume_m3[PWR_EXHAUST_VOLUME_COUNT];
    double wall_heat_capacity_j_per_k[PWR_EXHAUST_VOLUME_COUNT];
    double gas_wall_conductance_w_per_k[PWR_EXHAUST_VOLUME_COUNT];
    double wall_ambient_conductance_w_per_k[PWR_EXHAUST_VOLUME_COUNT];

    double orifice_area_m2[PWR_EXHAUST_PATH_COUNT];
    double discharge_coefficient[PWR_EXHAUST_PATH_COUNT];

    double species_gas_constant_j_per_kg_k[PWR_EXHAUST_SPECIES_COUNT];
    double species_cv_j_per_kg_k[PWR_EXHAUST_SPECIES_COUNT];

    /* A transparent, low-order three-way-catalyst surrogate. */
    double catalyst_light_off_temperature_k;
    double catalyst_full_activity_temperature_k;
    double catalyst_activity_time_constant_s;
    double catalyst_reaction_time_s;
    double catalyst_oxygen_per_reductant_kg_per_kg;
    double catalyst_reaction_heat_j_per_kg_reductant;
    double catalyst_heat_to_wall_fraction;

    double minimum_temperature_k;
    double minimum_volume_mass_kg;
    double maximum_substep_s;
    double maximum_outflow_fraction_per_substep;
    uint32_t maximum_substeps;
} pwr_exhaust_parameters;

typedef struct pwr_exhaust_reservoir {
    double pressure_pa;
    double temperature_k;
    double species_mass_fraction[PWR_EXHAUST_SPECIES_COUNT];
} pwr_exhaust_reservoir;

typedef struct pwr_exhaust_inputs {
    pwr_exhaust_reservoir upstream;
    pwr_exhaust_reservoir downstream;
    double inlet_area_scale;
    double tailpipe_area_scale;
    double ambient_temperature_k;
} pwr_exhaust_inputs;

typedef struct pwr_exhaust_volume_state {
    /* total_mass_kg is intentionally redundant and audited against species. */
    double total_mass_kg;
    double species_mass_kg[PWR_EXHAUST_SPECIES_COUNT];
    double internal_energy_j;
    double wall_temperature_k;
} pwr_exhaust_volume_state;

typedef struct pwr_exhaust_state {
    pwr_exhaust_volume_state volume[PWR_EXHAUST_VOLUME_COUNT];
    double catalyst_conversion_fraction;
    double cumulative_tailpipe_species_kg[PWR_EXHAUST_SPECIES_COUNT];
    double time_s;
} pwr_exhaust_state;

typedef struct pwr_exhaust_observation {
    double pressure_pa[PWR_EXHAUST_VOLUME_COUNT];
    double gas_temperature_k[PWR_EXHAUST_VOLUME_COUNT];
    double wall_temperature_k[PWR_EXHAUST_VOLUME_COUNT];
    double total_mass_kg[PWR_EXHAUST_VOLUME_COUNT];
    double catalyst_conversion_fraction;
} pwr_exhaust_observation;

typedef struct pwr_exhaust_diagnostics {
    pwr_exhaust_result result;
    uint32_t flags;
    uint32_t substeps;
    uint32_t flow_limit_events;
    uint32_t reaction_limit_events;
    uint32_t positivity_correction_events;

    /* Positive path flow is upstream -> tailpipe.  Values are step averages. */
    double average_path_mass_flow_kg_per_s[PWR_EXHAUST_PATH_COUNT];
    uint32_t choked_path_mask;

    double boundary_mass_net_kg;
    double boundary_species_net_kg[PWR_EXHAUST_SPECIES_COUNT];
    double boundary_enthalpy_net_j;
    double ambient_heat_net_j;
    double reaction_heat_j;
    double reacted_reductant_kg;
    double tailpipe_species_out_kg[PWR_EXHAUST_SPECIES_COUNT];

    /* Residual = state change - boundary/source terms - explicit correction. */
    double mass_residual_kg;
    double species_mass_residual_kg[PWR_EXHAUST_SPECIES_COUNT];
    double energy_residual_j;
    double positivity_mass_adjustment_kg;
    double positivity_species_adjustment_kg[PWR_EXHAUST_SPECIES_COUNT];
    double positivity_energy_adjustment_j;
    double state_species_closure_residual_kg;

    pwr_exhaust_observation final_observation;
} pwr_exhaust_diagnostics;

pwr_exhaust_result pwr_exhaust_validate_parameters(
    const pwr_exhaust_parameters *parameters);

pwr_exhaust_result pwr_exhaust_validate_reservoir(
    const pwr_exhaust_parameters *parameters,
    const pwr_exhaust_reservoir *reservoir);

pwr_exhaust_result pwr_exhaust_initialize_uniform(
    const pwr_exhaust_parameters *parameters,
    const pwr_exhaust_reservoir *initial_gas,
    double initial_wall_temperature_k,
    pwr_exhaust_state *state);

pwr_exhaust_result pwr_exhaust_observe(
    const pwr_exhaust_parameters *parameters,
    const pwr_exhaust_state *state,
    pwr_exhaust_observation *observation);

pwr_exhaust_result pwr_exhaust_step(
    const pwr_exhaust_parameters *parameters,
    pwr_exhaust_state *state,
    const pwr_exhaust_inputs *inputs,
    double dt_s,
    pwr_exhaust_diagnostics *diagnostics);

const char *pwr_exhaust_result_string(pwr_exhaust_result result);

#ifdef __cplusplus
}
#endif

#endif
