#include "pwr_exhaust.h"

#include <math.h>
#include <stddef.h>
#include <string.h>

typedef struct pwr_exhaust_gas_properties {
    double pressure_pa;
    double temperature_k;
    double gas_constant_j_per_kg_k;
    double cv_j_per_kg_k;
    double cp_j_per_kg_k;
    double gamma;
    double mass_fraction[PWR_EXHAUST_SPECIES_COUNT];
} pwr_exhaust_gas_properties;

typedef struct pwr_exhaust_rates {
    double total_mass_kg_per_s[PWR_EXHAUST_VOLUME_COUNT];
    double species_mass_kg_per_s[PWR_EXHAUST_VOLUME_COUNT]
                                      [PWR_EXHAUST_SPECIES_COUNT];
    double internal_energy_w[PWR_EXHAUST_VOLUME_COUNT];
    double wall_temperature_k_per_s[PWR_EXHAUST_VOLUME_COUNT];
    double catalyst_conversion_per_s;

    double path_mass_flow_kg_per_s[PWR_EXHAUST_PATH_COUNT];
    double boundary_mass_net_kg_per_s;
    double boundary_species_net_kg_per_s[PWR_EXHAUST_SPECIES_COUNT];
    double boundary_enthalpy_net_w;
    double ambient_heat_net_w;
    double reaction_heat_w;
    double reaction_species_kg_per_s[PWR_EXHAUST_SPECIES_COUNT];
    double reacted_reductant_kg_per_s;
    double tailpipe_species_out_kg_per_s[PWR_EXHAUST_SPECIES_COUNT];

    uint32_t flags;
    uint32_t choked_path_mask;
    uint32_t flow_limit_events;
    uint32_t reaction_limit_events;
} pwr_exhaust_rates;

typedef struct pwr_exhaust_correction {
    double mass_kg;
    double species_kg[PWR_EXHAUST_SPECIES_COUNT];
    double energy_j;
    uint32_t events;
} pwr_exhaust_correction;

typedef struct pwr_exhaust_ledger {
    double path_mass_kg[PWR_EXHAUST_PATH_COUNT];
    double boundary_mass_net_kg;
    double boundary_species_net_kg[PWR_EXHAUST_SPECIES_COUNT];
    double boundary_enthalpy_net_j;
    double ambient_heat_net_j;
    double reaction_heat_j;
    double reaction_species_kg[PWR_EXHAUST_SPECIES_COUNT];
    double reacted_reductant_kg;
    double tailpipe_species_out_kg[PWR_EXHAUST_SPECIES_COUNT];
    double positivity_mass_adjustment_kg;
    double positivity_species_adjustment_kg[PWR_EXHAUST_SPECIES_COUNT];
    double positivity_energy_adjustment_j;
} pwr_exhaust_ledger;

static int pwr_exhaust_is_positive_finite(double value)
{
    return isfinite(value) && value > 0.0;
}

static int pwr_exhaust_is_nonnegative_finite(double value)
{
    return isfinite(value) && value >= 0.0;
}

static double pwr_exhaust_min(double a, double b)
{
    return a < b ? a : b;
}

static double pwr_exhaust_max(double a, double b)
{
    return a > b ? a : b;
}

static double pwr_exhaust_clamp(double value, double low, double high)
{
    return pwr_exhaust_min(pwr_exhaust_max(value, low), high);
}

static double pwr_exhaust_species_sum(
    const double species_mass_kg[PWR_EXHAUST_SPECIES_COUNT])
{
    double total = 0.0;

    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        total += species_mass_kg[species];
    }
    return total;
}

static double pwr_exhaust_system_mass(const pwr_exhaust_state *state)
{
    double total = 0.0;

    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        total += state->volume[volume].total_mass_kg;
    }
    return total;
}

static double pwr_exhaust_system_species_mass(
    const pwr_exhaust_state *state,
    size_t species)
{
    double total = 0.0;

    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        total += state->volume[volume].species_mass_kg[species];
    }
    return total;
}

static double pwr_exhaust_system_energy(
    const pwr_exhaust_parameters *parameters,
    const pwr_exhaust_state *state)
{
    double total = 0.0;

    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        total += state->volume[volume].internal_energy_j;
        total += parameters->wall_heat_capacity_j_per_k[volume]
            * state->volume[volume].wall_temperature_k;
    }
    return total;
}

static double pwr_exhaust_activity_target(
    const pwr_exhaust_parameters *parameters,
    double catalyst_temperature_k)
{
    const double span = parameters->catalyst_full_activity_temperature_k
        - parameters->catalyst_light_off_temperature_k;
    const double normalized = pwr_exhaust_clamp(
        (catalyst_temperature_k
         - parameters->catalyst_light_off_temperature_k)
            / span,
        0.0,
        1.0);

    /* Smoothstep is a declared surrogate, not a chemistry-data fit. */
    return normalized * normalized * (3.0 - 2.0 * normalized);
}

static void pwr_exhaust_reservoir_properties(
    const pwr_exhaust_parameters *parameters,
    const pwr_exhaust_reservoir *reservoir,
    pwr_exhaust_gas_properties *properties)
{
    double gas_constant = 0.0;
    double cv = 0.0;

    memset(properties, 0, sizeof(*properties));
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        const double fraction = reservoir->species_mass_fraction[species];
        properties->mass_fraction[species] = fraction;
        gas_constant += fraction
            * parameters->species_gas_constant_j_per_kg_k[species];
        cv += fraction * parameters->species_cv_j_per_kg_k[species];
    }

    properties->pressure_pa = reservoir->pressure_pa;
    properties->temperature_k = reservoir->temperature_k;
    properties->gas_constant_j_per_kg_k = gas_constant;
    properties->cv_j_per_kg_k = cv;
    properties->cp_j_per_kg_k = cv + gas_constant;
    properties->gamma = properties->cp_j_per_kg_k / cv;
}

static pwr_exhaust_result pwr_exhaust_volume_properties(
    const pwr_exhaust_parameters *parameters,
    const pwr_exhaust_volume_state *state,
    size_t volume,
    pwr_exhaust_gas_properties *properties)
{
    double heat_capacity = 0.0;
    double mass_weighted_gas_constant = 0.0;
    const double species_total = pwr_exhaust_species_sum(state->species_mass_kg);

    if (!pwr_exhaust_is_positive_finite(species_total)) {
        return PWR_EXHAUST_INVALID_STATE;
    }

    memset(properties, 0, sizeof(*properties));
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        const double mass = state->species_mass_kg[species];
        properties->mass_fraction[species] = mass / species_total;
        heat_capacity += mass * parameters->species_cv_j_per_kg_k[species];
        mass_weighted_gas_constant += mass
            * parameters->species_gas_constant_j_per_kg_k[species];
    }

    if (!pwr_exhaust_is_positive_finite(heat_capacity)) {
        return PWR_EXHAUST_INVALID_STATE;
    }

    properties->temperature_k = state->internal_energy_j / heat_capacity;
    properties->gas_constant_j_per_kg_k =
        mass_weighted_gas_constant / species_total;
    properties->cv_j_per_kg_k = heat_capacity / species_total;
    properties->cp_j_per_kg_k = properties->cv_j_per_kg_k
        + properties->gas_constant_j_per_kg_k;
    properties->gamma = properties->cp_j_per_kg_k
        / properties->cv_j_per_kg_k;
    properties->pressure_pa = properties->temperature_k
        * mass_weighted_gas_constant / parameters->volume_m3[volume];

    if (!pwr_exhaust_is_positive_finite(properties->temperature_k)
        || !pwr_exhaust_is_positive_finite(properties->pressure_pa)
        || !pwr_exhaust_is_positive_finite(properties->gamma)) {
        return PWR_EXHAUST_INVALID_STATE;
    }
    return PWR_EXHAUST_OK;
}

static double pwr_exhaust_orifice_mass_flow(
    double area_m2,
    double discharge_coefficient,
    const pwr_exhaust_gas_properties *left,
    const pwr_exhaust_gas_properties *right,
    int *choked)
{
    const pwr_exhaust_gas_properties *upstream = left;
    const pwr_exhaust_gas_properties *downstream = right;
    double direction = 1.0;
    double pressure_ratio;
    double critical_ratio;
    double flow_factor;

    *choked = 0;
    if (area_m2 == 0.0) {
        return 0.0;
    }
    if (fabs(left->pressure_pa - right->pressure_pa)
        <= 1.0e-12 * pwr_exhaust_max(left->pressure_pa, right->pressure_pa)) {
        return 0.0;
    }
    if (right->pressure_pa > left->pressure_pa) {
        upstream = right;
        downstream = left;
        direction = -1.0;
    }

    pressure_ratio = pwr_exhaust_clamp(
        downstream->pressure_pa / upstream->pressure_pa, 0.0, 1.0);
    critical_ratio = pow(
        2.0 / (upstream->gamma + 1.0),
        upstream->gamma / (upstream->gamma - 1.0));

    if (pressure_ratio <= critical_ratio) {
        *choked = 1;
        flow_factor = sqrt(upstream->gamma)
            * pow(
                2.0 / (upstream->gamma + 1.0),
                (upstream->gamma + 1.0)
                    / (2.0 * (upstream->gamma - 1.0)));
    }
    else {
        const double first = pow(pressure_ratio, 2.0 / upstream->gamma);
        const double second = pow(
            pressure_ratio, (upstream->gamma + 1.0) / upstream->gamma);
        flow_factor = sqrt(pwr_exhaust_max(
            0.0,
            (2.0 * upstream->gamma / (upstream->gamma - 1.0))
                * (first - second)));
    }

    return direction * discharge_coefficient * area_m2
        * upstream->pressure_pa
        / sqrt(upstream->gas_constant_j_per_kg_k * upstream->temperature_k)
        * flow_factor;
}

static int pwr_exhaust_rates_are_finite(const pwr_exhaust_rates *rates)
{
    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        if (!isfinite(rates->total_mass_kg_per_s[volume])
            || !isfinite(rates->internal_energy_w[volume])
            || !isfinite(rates->wall_temperature_k_per_s[volume])) {
            return 0;
        }
        for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT;
             ++species) {
            if (!isfinite(rates->species_mass_kg_per_s[volume][species])) {
                return 0;
            }
        }
    }
    if (!isfinite(rates->catalyst_conversion_per_s)
        || !isfinite(rates->boundary_mass_net_kg_per_s)
        || !isfinite(rates->boundary_enthalpy_net_w)
        || !isfinite(rates->ambient_heat_net_w)
        || !isfinite(rates->reaction_heat_w)
        || !isfinite(rates->reacted_reductant_kg_per_s)) {
        return 0;
    }
    for (size_t path = 0; path < PWR_EXHAUST_PATH_COUNT; ++path) {
        if (!isfinite(rates->path_mass_flow_kg_per_s[path])) {
            return 0;
        }
    }
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        if (!isfinite(rates->boundary_species_net_kg_per_s[species])
            || !isfinite(rates->reaction_species_kg_per_s[species])
            || !isfinite(rates->tailpipe_species_out_kg_per_s[species])) {
            return 0;
        }
    }
    return 1;
}

static pwr_exhaust_result pwr_exhaust_calculate_rates(
    const pwr_exhaust_parameters *parameters,
    const pwr_exhaust_state *state,
    const pwr_exhaust_inputs *inputs,
    double substep_s,
    pwr_exhaust_rates *rates)
{
    pwr_exhaust_gas_properties gas[PWR_EXHAUST_VOLUME_COUNT];
    pwr_exhaust_gas_properties upstream;
    pwr_exhaust_gas_properties downstream;
    double donor_outflow_kg_per_s[PWR_EXHAUST_VOLUME_COUNT] = {0.0};
    double donor_scale[PWR_EXHAUST_VOLUME_COUNT] = {1.0, 1.0, 1.0};

    memset(rates, 0, sizeof(*rates));
    pwr_exhaust_reservoir_properties(parameters, &inputs->upstream, &upstream);
    pwr_exhaust_reservoir_properties(parameters, &inputs->downstream, &downstream);
    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        if (pwr_exhaust_volume_properties(
                parameters, &state->volume[volume], volume, &gas[volume])
            != PWR_EXHAUST_OK) {
            return PWR_EXHAUST_INVALID_STATE;
        }
    }

    for (size_t path = 0; path < PWR_EXHAUST_PATH_COUNT; ++path) {
        const pwr_exhaust_gas_properties *left =
            path == PWR_EXHAUST_PATH_INLET ? &upstream : &gas[path - 1u];
        const pwr_exhaust_gas_properties *right =
            path == PWR_EXHAUST_PATH_TAILPIPE ? &downstream : &gas[path];
        double area = parameters->orifice_area_m2[path];
        int choked = 0;

        if (path == PWR_EXHAUST_PATH_INLET) {
            area *= inputs->inlet_area_scale;
        }
        else if (path == PWR_EXHAUST_PATH_TAILPIPE) {
            area *= inputs->tailpipe_area_scale;
        }
        rates->path_mass_flow_kg_per_s[path] = pwr_exhaust_orifice_mass_flow(
            area,
            parameters->discharge_coefficient[path],
            left,
            right,
            &choked);
        if (choked) {
            rates->flags |= PWR_EXHAUST_DIAG_CHOKED_FLOW;
            rates->choked_path_mask |= 1u << path;
        }
        if (rates->path_mass_flow_kg_per_s[path] < 0.0) {
            rates->flags |= PWR_EXHAUST_DIAG_REVERSE_FLOW;
        }
    }

    /* Limit all simultaneous exits from a donor with one conservative scale. */
    for (size_t path = 0; path < PWR_EXHAUST_PATH_COUNT; ++path) {
        const double flow = rates->path_mass_flow_kg_per_s[path];
        if (flow > 0.0 && path > PWR_EXHAUST_PATH_INLET) {
            donor_outflow_kg_per_s[path - 1u] += flow;
        }
        else if (flow < 0.0 && path < PWR_EXHAUST_PATH_TAILPIPE) {
            donor_outflow_kg_per_s[path] -= flow;
        }
    }
    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        const double removable_mass = pwr_exhaust_max(
            0.0,
            state->volume[volume].total_mass_kg
                - parameters->minimum_volume_mass_kg);
        const double maximum_outflow =
            parameters->maximum_outflow_fraction_per_substep
            * removable_mass / substep_s;

        if (donor_outflow_kg_per_s[volume] > maximum_outflow
            && donor_outflow_kg_per_s[volume] > 0.0) {
            donor_scale[volume] = maximum_outflow
                / donor_outflow_kg_per_s[volume];
            rates->flags |= PWR_EXHAUST_DIAG_FLOW_LIMITED;
            ++rates->flow_limit_events;
        }
    }
    for (size_t path = 0; path < PWR_EXHAUST_PATH_COUNT; ++path) {
        const double flow = rates->path_mass_flow_kg_per_s[path];
        if (flow > 0.0 && path > PWR_EXHAUST_PATH_INLET) {
            rates->path_mass_flow_kg_per_s[path] *= donor_scale[path - 1u];
        }
        else if (flow < 0.0 && path < PWR_EXHAUST_PATH_TAILPIPE) {
            rates->path_mass_flow_kg_per_s[path] *= donor_scale[path];
        }
    }

    for (size_t path = 0; path < PWR_EXHAUST_PATH_COUNT; ++path) {
        const double flow = rates->path_mass_flow_kg_per_s[path];
        const pwr_exhaust_gas_properties *donor;
        const int left_volume = path == PWR_EXHAUST_PATH_INLET
            ? -1
            : (int)path - 1;
        const int right_volume = path == PWR_EXHAUST_PATH_TAILPIPE
            ? -1
            : (int)path;
        double enthalpy_flow;

        if (flow >= 0.0) {
            donor = left_volume < 0 ? &upstream : &gas[left_volume];
        }
        else {
            donor = right_volume < 0 ? &downstream : &gas[right_volume];
        }
        enthalpy_flow = flow * donor->cp_j_per_kg_k * donor->temperature_k;

        if (left_volume >= 0) {
            rates->total_mass_kg_per_s[left_volume] -= flow;
            rates->internal_energy_w[left_volume] -= enthalpy_flow;
        }
        if (right_volume >= 0) {
            rates->total_mass_kg_per_s[right_volume] += flow;
            rates->internal_energy_w[right_volume] += enthalpy_flow;
        }

        for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT;
             ++species) {
            const double species_flow = flow * donor->mass_fraction[species];
            if (left_volume >= 0) {
                rates->species_mass_kg_per_s[left_volume][species]
                    -= species_flow;
            }
            if (right_volume >= 0) {
                rates->species_mass_kg_per_s[right_volume][species]
                    += species_flow;
            }
            if (path == PWR_EXHAUST_PATH_INLET) {
                rates->boundary_species_net_kg_per_s[species] += species_flow;
            }
            else if (path == PWR_EXHAUST_PATH_TAILPIPE) {
                rates->boundary_species_net_kg_per_s[species] -= species_flow;
                if (flow > 0.0) {
                    rates->tailpipe_species_out_kg_per_s[species]
                        += species_flow;
                }
            }
        }

        if (path == PWR_EXHAUST_PATH_INLET) {
            rates->boundary_mass_net_kg_per_s += flow;
            rates->boundary_enthalpy_net_w += enthalpy_flow;
        }
        else if (path == PWR_EXHAUST_PATH_TAILPIPE) {
            rates->boundary_mass_net_kg_per_s -= flow;
            rates->boundary_enthalpy_net_w -= enthalpy_flow;
        }
    }

    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        const double gas_wall_heat_w =
            parameters->gas_wall_conductance_w_per_k[volume]
            * (state->volume[volume].wall_temperature_k
               - gas[volume].temperature_k);
        const double ambient_heat_w =
            parameters->wall_ambient_conductance_w_per_k[volume]
            * (inputs->ambient_temperature_k
               - state->volume[volume].wall_temperature_k);

        rates->internal_energy_w[volume] += gas_wall_heat_w;
        rates->wall_temperature_k_per_s[volume] +=
            (-gas_wall_heat_w + ambient_heat_w)
            / parameters->wall_heat_capacity_j_per_k[volume];
        rates->ambient_heat_net_w += ambient_heat_w;
    }

    {
        const size_t catalyst = PWR_EXHAUST_VOLUME_CATALYST;
        const double current_conversion = state->catalyst_conversion_fraction;
        const double target_conversion = pwr_exhaust_activity_target(
            parameters, state->volume[catalyst].wall_temperature_k);
        double conversion_rate = (target_conversion - current_conversion)
            / parameters->catalyst_activity_time_constant_s;
        double reductant_rate = current_conversion
            * state->volume[catalyst]
                  .species_mass_kg[PWR_EXHAUST_SPECIES_REDUCTANT]
            / parameters->catalyst_reaction_time_s;
        const double reductant_after_advection = pwr_exhaust_max(
            0.0,
            state->volume[catalyst]
                    .species_mass_kg[PWR_EXHAUST_SPECIES_REDUCTANT]
                + substep_s
                    * rates->species_mass_kg_per_s[catalyst]
                                                      [PWR_EXHAUST_SPECIES_REDUCTANT]);
        const double oxygen_after_advection = pwr_exhaust_max(
            0.0,
            state->volume[catalyst]
                    .species_mass_kg[PWR_EXHAUST_SPECIES_OXYGEN]
                + substep_s
                    * rates->species_mass_kg_per_s[catalyst]
                                                      [PWR_EXHAUST_SPECIES_OXYGEN]);
        double available_rate = reductant_after_advection / substep_s;

        if (parameters->catalyst_oxygen_per_reductant_kg_per_kg > 0.0) {
            available_rate = pwr_exhaust_min(
                available_rate,
                oxygen_after_advection
                    / (substep_s
                       * parameters->catalyst_oxygen_per_reductant_kg_per_kg));
        }
        if (reductant_rate > available_rate) {
            reductant_rate = available_rate;
            rates->flags |= PWR_EXHAUST_DIAG_REACTION_LIMITED;
            ++rates->reaction_limit_events;
        }
        reductant_rate = pwr_exhaust_max(0.0, reductant_rate);

        conversion_rate = pwr_exhaust_clamp(
            conversion_rate,
            -current_conversion / substep_s,
            (1.0 - current_conversion) / substep_s);
        rates->catalyst_conversion_per_s = conversion_rate;

        rates->reaction_species_kg_per_s[PWR_EXHAUST_SPECIES_REDUCTANT]
            = -reductant_rate;
        rates->reaction_species_kg_per_s[PWR_EXHAUST_SPECIES_OXYGEN]
            = -parameters->catalyst_oxygen_per_reductant_kg_per_kg
            * reductant_rate;
        rates->reaction_species_kg_per_s[PWR_EXHAUST_SPECIES_PRODUCTS]
            = (1.0
               + parameters->catalyst_oxygen_per_reductant_kg_per_kg)
            * reductant_rate;

        for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT;
             ++species) {
            rates->species_mass_kg_per_s[catalyst][species]
                += rates->reaction_species_kg_per_s[species];
        }

        rates->reacted_reductant_kg_per_s = reductant_rate;
        rates->reaction_heat_w = reductant_rate
            * parameters->catalyst_reaction_heat_j_per_kg_reductant;
        rates->internal_energy_w[catalyst] += rates->reaction_heat_w
            * (1.0 - parameters->catalyst_heat_to_wall_fraction);
        rates->wall_temperature_k_per_s[catalyst] +=
            rates->reaction_heat_w
            * parameters->catalyst_heat_to_wall_fraction
            / parameters->wall_heat_capacity_j_per_k[catalyst];
    }

    return pwr_exhaust_rates_are_finite(rates) ? PWR_EXHAUST_OK
                                                : PWR_EXHAUST_INVALID_STATE;
}

static void pwr_exhaust_euler_advance(
    pwr_exhaust_state *state,
    const pwr_exhaust_rates *rates,
    double substep_s)
{
    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        state->volume[volume].total_mass_kg +=
            substep_s * rates->total_mass_kg_per_s[volume];
        state->volume[volume].internal_energy_j +=
            substep_s * rates->internal_energy_w[volume];
        state->volume[volume].wall_temperature_k +=
            substep_s * rates->wall_temperature_k_per_s[volume];
        for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT;
             ++species) {
            state->volume[volume].species_mass_kg[species] += substep_s
                * rates->species_mass_kg_per_s[volume][species];
        }
    }
    state->catalyst_conversion_fraction +=
        substep_s * rates->catalyst_conversion_per_s;
}

static pwr_exhaust_result pwr_exhaust_apply_positivity(
    const pwr_exhaust_parameters *parameters,
    pwr_exhaust_state *state,
    pwr_exhaust_correction *correction)
{
    memset(correction, 0, sizeof(*correction));

    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        pwr_exhaust_volume_state *cell = &state->volume[volume];
        const double old_total_mass = cell->total_mass_kg;
        double new_total_mass;
        double minimum_internal_energy = 0.0;

        if (!isfinite(old_total_mass) || !isfinite(cell->internal_energy_j)
            || !isfinite(cell->wall_temperature_k)) {
            return PWR_EXHAUST_INVALID_STATE;
        }
        for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT;
             ++species) {
            if (!isfinite(cell->species_mass_kg[species])) {
                return PWR_EXHAUST_INVALID_STATE;
            }
            if (cell->species_mass_kg[species] < 0.0) {
                correction->species_kg[species]
                    -= cell->species_mass_kg[species];
                cell->species_mass_kg[species] = 0.0;
                ++correction->events;
            }
        }

        new_total_mass = pwr_exhaust_species_sum(cell->species_mass_kg);
        if (new_total_mass < parameters->minimum_volume_mass_kg) {
            const double added = parameters->minimum_volume_mass_kg
                - new_total_mass;
            cell->species_mass_kg[PWR_EXHAUST_SPECIES_INERT] += added;
            correction->species_kg[PWR_EXHAUST_SPECIES_INERT] += added;
            new_total_mass += added;
            ++correction->events;
        }
        cell->total_mass_kg = new_total_mass;
        correction->mass_kg += new_total_mass - old_total_mass;

        for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT;
             ++species) {
            minimum_internal_energy += cell->species_mass_kg[species]
                * parameters->species_cv_j_per_kg_k[species]
                * parameters->minimum_temperature_k;
        }
        if (cell->internal_energy_j < minimum_internal_energy) {
            correction->energy_j +=
                minimum_internal_energy - cell->internal_energy_j;
            cell->internal_energy_j = minimum_internal_energy;
            ++correction->events;
        }
        if (cell->wall_temperature_k < parameters->minimum_temperature_k) {
            correction->energy_j +=
                parameters->wall_heat_capacity_j_per_k[volume]
                * (parameters->minimum_temperature_k
                   - cell->wall_temperature_k);
            cell->wall_temperature_k = parameters->minimum_temperature_k;
            ++correction->events;
        }
    }

    if (!isfinite(state->catalyst_conversion_fraction)) {
        return PWR_EXHAUST_INVALID_STATE;
    }
    if (state->catalyst_conversion_fraction < 0.0
        || state->catalyst_conversion_fraction > 1.0) {
        state->catalyst_conversion_fraction = pwr_exhaust_clamp(
            state->catalyst_conversion_fraction, 0.0, 1.0);
        ++correction->events;
    }
    return PWR_EXHAUST_OK;
}

static void pwr_exhaust_ssp_average(
    const pwr_exhaust_state *start,
    const pwr_exhaust_state *second_euler,
    pwr_exhaust_state *result)
{
    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        result->volume[volume].total_mass_kg =
            0.5 * (start->volume[volume].total_mass_kg
                   + second_euler->volume[volume].total_mass_kg);
        result->volume[volume].internal_energy_j =
            0.5 * (start->volume[volume].internal_energy_j
                   + second_euler->volume[volume].internal_energy_j);
        result->volume[volume].wall_temperature_k =
            0.5 * (start->volume[volume].wall_temperature_k
                   + second_euler->volume[volume].wall_temperature_k);
        for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT;
             ++species) {
            result->volume[volume].species_mass_kg[species] =
                0.5 * (start->volume[volume].species_mass_kg[species]
                       + second_euler->volume[volume]
                             .species_mass_kg[species]);
        }
    }
    result->catalyst_conversion_fraction =
        0.5 * (start->catalyst_conversion_fraction
               + second_euler->catalyst_conversion_fraction);
}

static void pwr_exhaust_accumulate_rates(
    pwr_exhaust_ledger *ledger,
    const pwr_exhaust_rates *rates,
    double weight_s)
{
    for (size_t path = 0; path < PWR_EXHAUST_PATH_COUNT; ++path) {
        ledger->path_mass_kg[path] +=
            weight_s * rates->path_mass_flow_kg_per_s[path];
    }
    ledger->boundary_mass_net_kg +=
        weight_s * rates->boundary_mass_net_kg_per_s;
    ledger->boundary_enthalpy_net_j +=
        weight_s * rates->boundary_enthalpy_net_w;
    ledger->ambient_heat_net_j += weight_s * rates->ambient_heat_net_w;
    ledger->reaction_heat_j += weight_s * rates->reaction_heat_w;
    ledger->reacted_reductant_kg +=
        weight_s * rates->reacted_reductant_kg_per_s;

    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        ledger->boundary_species_net_kg[species] +=
            weight_s * rates->boundary_species_net_kg_per_s[species];
        ledger->reaction_species_kg[species] +=
            weight_s * rates->reaction_species_kg_per_s[species];
        ledger->tailpipe_species_out_kg[species] +=
            weight_s * rates->tailpipe_species_out_kg_per_s[species];
    }
}

static void pwr_exhaust_accumulate_correction(
    pwr_exhaust_ledger *ledger,
    const pwr_exhaust_correction *correction,
    double weight)
{
    ledger->positivity_mass_adjustment_kg += weight * correction->mass_kg;
    ledger->positivity_energy_adjustment_j += weight * correction->energy_j;
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        ledger->positivity_species_adjustment_kg[species] +=
            weight * correction->species_kg[species];
    }
}

static pwr_exhaust_result pwr_exhaust_validate_state(
    const pwr_exhaust_parameters *parameters,
    const pwr_exhaust_state *state)
{
    if (state == NULL || !isfinite(state->time_s) || state->time_s < 0.0
        || !isfinite(state->catalyst_conversion_fraction)
        || state->catalyst_conversion_fraction < 0.0
        || state->catalyst_conversion_fraction > 1.0) {
        return PWR_EXHAUST_INVALID_STATE;
    }

    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        if (!pwr_exhaust_is_nonnegative_finite(
                state->cumulative_tailpipe_species_kg[species])) {
            return PWR_EXHAUST_INVALID_STATE;
        }
    }
    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        const pwr_exhaust_volume_state *cell = &state->volume[volume];
        double species_total;
        double closure_tolerance;
        pwr_exhaust_gas_properties properties;

        if (!pwr_exhaust_is_positive_finite(cell->total_mass_kg)
            || cell->total_mass_kg < parameters->minimum_volume_mass_kg
            || !pwr_exhaust_is_positive_finite(cell->internal_energy_j)
            || !pwr_exhaust_is_positive_finite(cell->wall_temperature_k)
            || cell->wall_temperature_k < parameters->minimum_temperature_k) {
            return PWR_EXHAUST_INVALID_STATE;
        }
        for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT;
             ++species) {
            if (!pwr_exhaust_is_nonnegative_finite(
                    cell->species_mass_kg[species])) {
                return PWR_EXHAUST_INVALID_STATE;
            }
        }
        species_total = pwr_exhaust_species_sum(cell->species_mass_kg);
        closure_tolerance = 1.0e-12
            + 1.0e-9 * pwr_exhaust_max(species_total, cell->total_mass_kg);
        if (fabs(species_total - cell->total_mass_kg) > closure_tolerance
            || pwr_exhaust_volume_properties(
                   parameters, cell, volume, &properties)
                != PWR_EXHAUST_OK
            || properties.temperature_k < parameters->minimum_temperature_k) {
            return PWR_EXHAUST_INVALID_STATE;
        }
    }
    return PWR_EXHAUST_OK;
}

pwr_exhaust_result pwr_exhaust_validate_parameters(
    const pwr_exhaust_parameters *parameters)
{
    if (parameters == NULL) {
        return PWR_EXHAUST_INVALID_ARGUMENT;
    }
    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        if (!pwr_exhaust_is_positive_finite(parameters->volume_m3[volume])
            || !pwr_exhaust_is_positive_finite(
                parameters->wall_heat_capacity_j_per_k[volume])
            || !pwr_exhaust_is_nonnegative_finite(
                parameters->gas_wall_conductance_w_per_k[volume])
            || !pwr_exhaust_is_nonnegative_finite(
                parameters->wall_ambient_conductance_w_per_k[volume])) {
            return PWR_EXHAUST_INVALID_PARAMETER;
        }
    }
    for (size_t path = 0; path < PWR_EXHAUST_PATH_COUNT; ++path) {
        if (!pwr_exhaust_is_nonnegative_finite(
                parameters->orifice_area_m2[path])
            || !pwr_exhaust_is_positive_finite(
                parameters->discharge_coefficient[path])) {
            return PWR_EXHAUST_INVALID_PARAMETER;
        }
    }
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        if (!pwr_exhaust_is_positive_finite(
                parameters->species_gas_constant_j_per_kg_k[species])
            || !pwr_exhaust_is_positive_finite(
                parameters->species_cv_j_per_kg_k[species])
            || parameters->species_gas_constant_j_per_kg_k[species]
                    >= parameters->species_cv_j_per_kg_k[species]) {
            return PWR_EXHAUST_INVALID_PARAMETER;
        }
    }
    if (!pwr_exhaust_is_positive_finite(
            parameters->catalyst_light_off_temperature_k)
        || !pwr_exhaust_is_positive_finite(
            parameters->catalyst_full_activity_temperature_k)
        || parameters->catalyst_full_activity_temperature_k
            <= parameters->catalyst_light_off_temperature_k
        || !pwr_exhaust_is_positive_finite(
            parameters->catalyst_activity_time_constant_s)
        || !pwr_exhaust_is_positive_finite(
            parameters->catalyst_reaction_time_s)
        || !pwr_exhaust_is_nonnegative_finite(
            parameters->catalyst_oxygen_per_reductant_kg_per_kg)
        || !pwr_exhaust_is_nonnegative_finite(
            parameters->catalyst_reaction_heat_j_per_kg_reductant)
        || !pwr_exhaust_is_nonnegative_finite(
            parameters->catalyst_heat_to_wall_fraction)
        || parameters->catalyst_heat_to_wall_fraction > 1.0
        || !pwr_exhaust_is_positive_finite(parameters->minimum_temperature_k)
        || parameters->minimum_temperature_k
            >= parameters->catalyst_light_off_temperature_k
        || !pwr_exhaust_is_positive_finite(
            parameters->minimum_volume_mass_kg)
        || !pwr_exhaust_is_positive_finite(parameters->maximum_substep_s)
        || !pwr_exhaust_is_positive_finite(
            parameters->maximum_outflow_fraction_per_substep)
        || parameters->maximum_outflow_fraction_per_substep > 0.25
        || parameters->maximum_substeps == 0u) {
        return PWR_EXHAUST_INVALID_PARAMETER;
    }
    return PWR_EXHAUST_OK;
}

pwr_exhaust_result pwr_exhaust_validate_reservoir(
    const pwr_exhaust_parameters *parameters,
    const pwr_exhaust_reservoir *reservoir)
{
    double fraction_sum = 0.0;

    if (pwr_exhaust_validate_parameters(parameters) != PWR_EXHAUST_OK) {
        return parameters == NULL ? PWR_EXHAUST_INVALID_ARGUMENT
                                  : PWR_EXHAUST_INVALID_PARAMETER;
    }
    if (reservoir == NULL) {
        return PWR_EXHAUST_INVALID_ARGUMENT;
    }
    if (!pwr_exhaust_is_positive_finite(reservoir->pressure_pa)
        || !pwr_exhaust_is_positive_finite(reservoir->temperature_k)
        || reservoir->temperature_k < parameters->minimum_temperature_k) {
        return PWR_EXHAUST_INVALID_BOUNDARY;
    }
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        if (!pwr_exhaust_is_nonnegative_finite(
                reservoir->species_mass_fraction[species])) {
            return PWR_EXHAUST_INVALID_BOUNDARY;
        }
        fraction_sum += reservoir->species_mass_fraction[species];
    }
    if (fabs(fraction_sum - 1.0) > 1.0e-9) {
        return PWR_EXHAUST_INVALID_BOUNDARY;
    }
    return PWR_EXHAUST_OK;
}

pwr_exhaust_result pwr_exhaust_initialize_uniform(
    const pwr_exhaust_parameters *parameters,
    const pwr_exhaust_reservoir *initial_gas,
    double initial_wall_temperature_k,
    pwr_exhaust_state *state)
{
    pwr_exhaust_gas_properties gas;

    if (state == NULL) {
        return PWR_EXHAUST_INVALID_ARGUMENT;
    }
    if (pwr_exhaust_validate_parameters(parameters) != PWR_EXHAUST_OK) {
        return parameters == NULL ? PWR_EXHAUST_INVALID_ARGUMENT
                                  : PWR_EXHAUST_INVALID_PARAMETER;
    }
    if (pwr_exhaust_validate_reservoir(parameters, initial_gas)
            != PWR_EXHAUST_OK
        || !pwr_exhaust_is_positive_finite(initial_wall_temperature_k)
        || initial_wall_temperature_k < parameters->minimum_temperature_k) {
        return PWR_EXHAUST_INVALID_BOUNDARY;
    }
    pwr_exhaust_reservoir_properties(parameters, initial_gas, &gas);

    memset(state, 0, sizeof(*state));
    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        const double mass = initial_gas->pressure_pa
            * parameters->volume_m3[volume]
            / (gas.gas_constant_j_per_kg_k * initial_gas->temperature_k);

        if (mass < parameters->minimum_volume_mass_kg) {
            memset(state, 0, sizeof(*state));
            return PWR_EXHAUST_INVALID_STATE;
        }
        state->volume[volume].total_mass_kg = mass;
        state->volume[volume].wall_temperature_k = initial_wall_temperature_k;
        for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT;
             ++species) {
            const double species_mass = mass
                * initial_gas->species_mass_fraction[species];
            state->volume[volume].species_mass_kg[species] = species_mass;
            state->volume[volume].internal_energy_j += species_mass
                * parameters->species_cv_j_per_kg_k[species]
                * initial_gas->temperature_k;
        }
    }
    state->catalyst_conversion_fraction = pwr_exhaust_activity_target(
        parameters, initial_wall_temperature_k);
    return PWR_EXHAUST_OK;
}

pwr_exhaust_result pwr_exhaust_observe(
    const pwr_exhaust_parameters *parameters,
    const pwr_exhaust_state *state,
    pwr_exhaust_observation *observation)
{
    if (observation == NULL) {
        return PWR_EXHAUST_INVALID_ARGUMENT;
    }
    if (pwr_exhaust_validate_parameters(parameters) != PWR_EXHAUST_OK) {
        return parameters == NULL ? PWR_EXHAUST_INVALID_ARGUMENT
                                  : PWR_EXHAUST_INVALID_PARAMETER;
    }
    if (pwr_exhaust_validate_state(parameters, state) != PWR_EXHAUST_OK) {
        return PWR_EXHAUST_INVALID_STATE;
    }

    memset(observation, 0, sizeof(*observation));
    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        pwr_exhaust_gas_properties properties;
        if (pwr_exhaust_volume_properties(
                parameters, &state->volume[volume], volume, &properties)
            != PWR_EXHAUST_OK) {
            return PWR_EXHAUST_INVALID_STATE;
        }
        observation->pressure_pa[volume] = properties.pressure_pa;
        observation->gas_temperature_k[volume] = properties.temperature_k;
        observation->wall_temperature_k[volume] =
            state->volume[volume].wall_temperature_k;
        observation->total_mass_kg[volume] =
            state->volume[volume].total_mass_kg;
    }
    observation->catalyst_conversion_fraction =
        state->catalyst_conversion_fraction;
    return PWR_EXHAUST_OK;
}

pwr_exhaust_result pwr_exhaust_step(
    const pwr_exhaust_parameters *parameters,
    pwr_exhaust_state *state,
    const pwr_exhaust_inputs *inputs,
    double dt_s,
    pwr_exhaust_diagnostics *diagnostics)
{
    pwr_exhaust_state work;
    pwr_exhaust_ledger ledger = {0};
    double initial_mass;
    double initial_energy;
    double initial_species[PWR_EXHAUST_SPECIES_COUNT];
    uint32_t substeps;
    double substep_s;

    if (diagnostics == NULL) {
        return PWR_EXHAUST_INVALID_ARGUMENT;
    }
    memset(diagnostics, 0, sizeof(*diagnostics));
    diagnostics->result = PWR_EXHAUST_INVALID_ARGUMENT;
    if (parameters == NULL || state == NULL || inputs == NULL) {
        diagnostics->flags |= PWR_EXHAUST_DIAG_INPUT_REJECTED;
        return diagnostics->result;
    }
    diagnostics->result = pwr_exhaust_validate_parameters(parameters);
    if (diagnostics->result != PWR_EXHAUST_OK) {
        diagnostics->flags |= PWR_EXHAUST_DIAG_INPUT_REJECTED;
        return diagnostics->result;
    }
    if (pwr_exhaust_validate_reservoir(parameters, &inputs->upstream)
            != PWR_EXHAUST_OK
        || pwr_exhaust_validate_reservoir(parameters, &inputs->downstream)
            != PWR_EXHAUST_OK
        || !pwr_exhaust_is_nonnegative_finite(inputs->inlet_area_scale)
        || !pwr_exhaust_is_nonnegative_finite(inputs->tailpipe_area_scale)
        || !pwr_exhaust_is_positive_finite(inputs->ambient_temperature_k)
        || inputs->ambient_temperature_k < parameters->minimum_temperature_k) {
        diagnostics->result = PWR_EXHAUST_INVALID_BOUNDARY;
        diagnostics->flags |= PWR_EXHAUST_DIAG_INPUT_REJECTED;
        return diagnostics->result;
    }
    if (pwr_exhaust_validate_state(parameters, state) != PWR_EXHAUST_OK) {
        diagnostics->result = PWR_EXHAUST_INVALID_STATE;
        diagnostics->flags |= PWR_EXHAUST_DIAG_INPUT_REJECTED;
        return diagnostics->result;
    }
    if (!pwr_exhaust_is_positive_finite(dt_s)) {
        diagnostics->result = PWR_EXHAUST_INVALID_TIMESTEP;
        diagnostics->flags |= PWR_EXHAUST_DIAG_INPUT_REJECTED;
        return diagnostics->result;
    }
    if (dt_s / parameters->maximum_substep_s
        > (double)parameters->maximum_substeps) {
        diagnostics->result = PWR_EXHAUST_SUBSTEP_LIMIT;
        diagnostics->flags |= PWR_EXHAUST_DIAG_INPUT_REJECTED;
        return diagnostics->result;
    }

    substeps = (uint32_t)ceil(dt_s / parameters->maximum_substep_s);
    if (substeps == 0u) {
        substeps = 1u;
    }
    if (substeps > parameters->maximum_substeps) {
        diagnostics->result = PWR_EXHAUST_SUBSTEP_LIMIT;
        diagnostics->flags |= PWR_EXHAUST_DIAG_INPUT_REJECTED;
        return diagnostics->result;
    }
    substep_s = dt_s / (double)substeps;

    work = *state;
    initial_mass = pwr_exhaust_system_mass(&work);
    initial_energy = pwr_exhaust_system_energy(parameters, &work);
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        initial_species[species] = pwr_exhaust_system_species_mass(&work, species);
    }

    for (uint32_t step = 0; step < substeps; ++step) {
        const pwr_exhaust_state start = work;
        pwr_exhaust_state stage = start;
        pwr_exhaust_rates first_rates;
        pwr_exhaust_rates second_rates;
        pwr_exhaust_correction first_correction;
        pwr_exhaust_correction second_correction;
        pwr_exhaust_result result;

        result = pwr_exhaust_calculate_rates(
            parameters, &start, inputs, substep_s, &first_rates);
        if (result != PWR_EXHAUST_OK) {
            diagnostics->result = result;
            diagnostics->flags |= PWR_EXHAUST_DIAG_NUMERIC_FAILURE;
            return result;
        }
        pwr_exhaust_euler_advance(&stage, &first_rates, substep_s);
        result = pwr_exhaust_apply_positivity(
            parameters, &stage, &first_correction);
        if (result != PWR_EXHAUST_OK) {
            diagnostics->result = result;
            diagnostics->flags |= PWR_EXHAUST_DIAG_NUMERIC_FAILURE;
            return result;
        }

        result = pwr_exhaust_calculate_rates(
            parameters, &stage, inputs, substep_s, &second_rates);
        if (result != PWR_EXHAUST_OK) {
            diagnostics->result = result;
            diagnostics->flags |= PWR_EXHAUST_DIAG_NUMERIC_FAILURE;
            return result;
        }
        pwr_exhaust_euler_advance(&stage, &second_rates, substep_s);
        result = pwr_exhaust_apply_positivity(
            parameters, &stage, &second_correction);
        if (result != PWR_EXHAUST_OK) {
            diagnostics->result = result;
            diagnostics->flags |= PWR_EXHAUST_DIAG_NUMERIC_FAILURE;
            return result;
        }

        pwr_exhaust_ssp_average(&start, &stage, &work);
        pwr_exhaust_accumulate_rates(&ledger, &first_rates, 0.5 * substep_s);
        pwr_exhaust_accumulate_rates(&ledger, &second_rates, 0.5 * substep_s);
        pwr_exhaust_accumulate_correction(
            &ledger, &first_correction, 0.5);
        pwr_exhaust_accumulate_correction(
            &ledger, &second_correction, 0.5);

        diagnostics->flags |= first_rates.flags | second_rates.flags;
        diagnostics->choked_path_mask |=
            first_rates.choked_path_mask | second_rates.choked_path_mask;
        diagnostics->flow_limit_events +=
            first_rates.flow_limit_events + second_rates.flow_limit_events;
        diagnostics->reaction_limit_events += first_rates.reaction_limit_events
            + second_rates.reaction_limit_events;
        diagnostics->positivity_correction_events +=
            first_correction.events + second_correction.events;
    }

    work.time_s += dt_s;
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        work.cumulative_tailpipe_species_kg[species] +=
            ledger.tailpipe_species_out_kg[species];
    }

    diagnostics->substeps = substeps;
    diagnostics->boundary_mass_net_kg = ledger.boundary_mass_net_kg;
    diagnostics->boundary_enthalpy_net_j = ledger.boundary_enthalpy_net_j;
    diagnostics->ambient_heat_net_j = ledger.ambient_heat_net_j;
    diagnostics->reaction_heat_j = ledger.reaction_heat_j;
    diagnostics->reacted_reductant_kg = ledger.reacted_reductant_kg;
    diagnostics->positivity_mass_adjustment_kg =
        ledger.positivity_mass_adjustment_kg;
    diagnostics->positivity_energy_adjustment_j =
        ledger.positivity_energy_adjustment_j;
    for (size_t path = 0; path < PWR_EXHAUST_PATH_COUNT; ++path) {
        diagnostics->average_path_mass_flow_kg_per_s[path] =
            ledger.path_mass_kg[path] / dt_s;
    }
    for (size_t species = 0; species < PWR_EXHAUST_SPECIES_COUNT; ++species) {
        diagnostics->boundary_species_net_kg[species] =
            ledger.boundary_species_net_kg[species];
        diagnostics->tailpipe_species_out_kg[species] =
            ledger.tailpipe_species_out_kg[species];
        diagnostics->positivity_species_adjustment_kg[species] =
            ledger.positivity_species_adjustment_kg[species];
        diagnostics->species_mass_residual_kg[species] =
            pwr_exhaust_system_species_mass(&work, species)
            - initial_species[species]
            - ledger.boundary_species_net_kg[species]
            - ledger.reaction_species_kg[species]
            - ledger.positivity_species_adjustment_kg[species];
    }
    diagnostics->mass_residual_kg = pwr_exhaust_system_mass(&work)
        - initial_mass - ledger.boundary_mass_net_kg
        - ledger.positivity_mass_adjustment_kg;
    diagnostics->energy_residual_j =
        pwr_exhaust_system_energy(parameters, &work) - initial_energy
        - ledger.boundary_enthalpy_net_j - ledger.ambient_heat_net_j
        - ledger.reaction_heat_j - ledger.positivity_energy_adjustment_j;

    for (size_t volume = 0; volume < PWR_EXHAUST_VOLUME_COUNT; ++volume) {
        diagnostics->state_species_closure_residual_kg +=
            work.volume[volume].total_mass_kg
            - pwr_exhaust_species_sum(work.volume[volume].species_mass_kg);
    }
    if (diagnostics->positivity_correction_events > 0u) {
        diagnostics->flags |= PWR_EXHAUST_DIAG_POSITIVITY_CORRECTION;
    }
    if (work.catalyst_conversion_fraction >= 0.5) {
        diagnostics->flags |= PWR_EXHAUST_DIAG_CATALYST_LIGHT_OFF;
    }

    diagnostics->result = pwr_exhaust_observe(
        parameters, &work, &diagnostics->final_observation);
    if (diagnostics->result != PWR_EXHAUST_OK) {
        diagnostics->flags |= PWR_EXHAUST_DIAG_NUMERIC_FAILURE;
        return diagnostics->result;
    }
    *state = work;
    return PWR_EXHAUST_OK;
}

const char *pwr_exhaust_result_string(pwr_exhaust_result result)
{
    switch (result) {
    case PWR_EXHAUST_OK:
        return "ok";
    case PWR_EXHAUST_INVALID_ARGUMENT:
        return "invalid argument";
    case PWR_EXHAUST_INVALID_PARAMETER:
        return "invalid parameter";
    case PWR_EXHAUST_INVALID_BOUNDARY:
        return "invalid boundary";
    case PWR_EXHAUST_INVALID_STATE:
        return "invalid state";
    case PWR_EXHAUST_INVALID_TIMESTEP:
        return "invalid timestep";
    case PWR_EXHAUST_SUBSTEP_LIMIT:
        return "substep limit";
    default:
        return "unknown exhaust result";
    }
}
