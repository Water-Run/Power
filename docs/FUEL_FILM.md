# Finite liquid fuel film and evaporation

**English** · [简体中文](FUEL_FILM.zh-CN.md) · [Français](FUEL_FILM.fr.md) · [Русский](FUEL_FILM.ru.md) · [日本語](FUEL_FILM.ja.md) · [한국어](FUEL_FILM.ko.md) · [Deutsch](FUEL_FILM.de.md) · [Español](FUEL_FILM.es.md) · [Italiano](FUEL_FILM.it.md) · [Português](FUEL_FILM.pt-BR.md)

`fuel_film` stores an explicit initial liquid inventory beside a tracked gas
receiver. A finite thermal node supplies sensible and phase-change heat. Evaporated
fuel joins the receiver's mass, internal energy and fuel constituent; the existing
premixed reaction consumes vapor only. The liquid inventory is initial wetting,
not injected fuel, and remains part of the total mass and chemical-energy ledger.

This is a constant-property research model with negligible liquid displacement
volume and a prescribed saturation temperature. It doesn't implement liquid rail,
needle or spray dynamics, pressure-dependent phase equilibrium, condensation,
multicomponent fuel properties or calibrated gasoline behavior.

## Phase energy and finite heat source

Let `c_l` be liquid specific heat, `c_v` the receiver's gas isochoric heat capacity,
`T_s` the declared saturation temperature, and `L_u > 0` the vapor-minus-liquid
specific internal-energy difference at `T_s`. The shared thermal reference is:

```text
e_offset = (c_v - c_l) T_s - L_u
U_liquid = m_liquid (c_l T_liquid + e_offset)
U_vapor_added = delta_m c_v T_s
```

`L_u` is an internal-energy difference in J/kg, rather than an enthalpy of
vaporization. A supplied enthalpy needs an explicit, justified conversion before
it can be used here. Liquid thermal energy can be negative under this reference;
temperature and mass must still be physically admissible. Chemical energy
`m_liquid * LHV` is separate and transfers with the vapor without creating reaction
heat or external source work.

Below saturation, conductance `K` couples liquid capacity `m_liquid c_l` to finite
wall capacity `C_w`. The temperature difference decays analytically at rate
`K (1 / (m_liquid c_l) + 1 / C_w)`. The capacity-weighted mean temperature remains
constant. If the liquid reaches `T_s`, the law resolves that time and uses the
remaining interval for evaporation.

At saturation with `T_wall > T_s`, wall superheat decays at rate `K / C_w`.
Available phase heat over interval `h` is
`C_w (T_wall - T_s) (1 - exp(-K h / C_w))`, capped by `m_liquid L_u`.
The film remains at `T_s` until dry; evaporated mass is phase heat divided by
`L_u`. Dryout leaves exactly zero liquid mass and energy and stops the heat draw.
A cold wall can cool existing liquid; it doesn't condense receiver vapor.

Every transfer satisfies `delta U_liquid + U_vapor_added = Q_from_wall`.
The wall loses that same heat, so phase change doesn't introduce an external
energy boundary. Nonnegative fuel inventories and the full constituent ledger
are checked independently of the total-energy ledger.

## Graph and document contract

| Data | Requirement |
|---|---|
| `node_a` | Finite gas receiver with explicit premixed fuel tracking and LHV |
| `node_b` | Finite thermal wall, with positive capacity and temperature |
| `initial_mass` | Nonnegative kg; the complete initial liquid inventory |
| `initial_temperature` | Positive K, no greater than saturation |
| `liquid_specific_heat` | Positive J/(kg K), JSON unit `j_kg_k` |
| `saturation_temperature` | Positive K, JSON unit `k` |
| `latent_internal_energy` | Positive J/kg, JSON unit `j_kg` |
| `conductance` | Nonnegative W/K, JSON unit `w_k` |

JSON requires all six parameters. The film has no input channel, crank timing or
separate heat sink. Unrelated parameters, wrong port domains, units, nonfinite
values and unsupported state capacity are rejected. Core callers use
`ComponentDefinition.LiquidFilm` and `FuelFilmDefinition`; the independent
`EquilibriumFuelFilm` law exposes admissible state creation and finite-bath advance.

Each film contributes five reported state entries to the compiler's bounded
state budget. Mass, thermal energy, evaporation history, mean flow, wall heat and
their compensated histories are simulation-owned. Cancellation, rejected inputs,
late solver failures, independent forks and speculative clutch intervals preserve
the complete transaction. Successful stepping and snapshot reads allocate no
managed memory after warmup.

## Integration accuracy

An accepted interval uses film / gas / mechanics-and-reaction / gas / film
half-steps. Films sharing a wall run in stable component order before the gas
advance and reverse order afterward. Their finite wall temperature is carried
between film substeps, and the wall heat enters the same thermal solve.

The isolated finite-bath law is analytic across sensible heating, saturation and
dryout. Independent simultaneous ODE integration verifies second-order smooth
refinement for two films sharing a wall and for vapor transported through a choked
gas outlet, without other wall heat sources. Gas heat
links and other thermal sources still read the explicit outer-interval wall
temperature, so that coupling retains first-order accuracy. Reaction windows,
valve events and dryout need their own refinement checks; exact batch replay alone
doesn't prove timestep accuracy or uniform second order for a fired powertrain.

## Observable and portable semantics

| Film field | Meaning |
|---|---|
| `mass` | Remaining liquid fuel, kg |
| `temperature` | Liquid temperature; declared saturation temperature when dry |
| `internal_energy` | Signed liquid thermal energy under the declared phase reference, J |
| `chemical_energy` | Remaining liquid fuel chemical energy, J |
| `evaporated_fuel_mass` | Cumulative vapor delivered, kg |
| `mass_flow` | Mean vapor delivery over the last complete physical tick, kg/s |
| `film_wall_heat` | Cumulative heat drawn from the wall, J; cooling can make it negative |
| `heat_flow` | Instantaneous `K (T_wall - T_liquid)`, W; zero when dry |

Discover output IDs and units through validation or session creation. Global
mass, fuel and chemical-energy observables include the film inventory. Internal
vapor delivery doesn't increment reservoir fuel energy or external enthalpy.

Asset v19 stores all phase properties and retains v1-v18 readers. Every film needs
one typed 64-byte phase record. Bounded lengths/counts, digest, complete coverage,
duplicate/missing records, units, physical compilation and downgrade protection
are checked. The authentic v17 fuel-metering fixture retains its fingerprint and
same-runtime upgraded replay. See [ASSET_FORMAT.md](ASSET_FORMAT.md).

## Laboratory and remaining work

`film-fired-cylinder` heats an initially wetted film, admits air separately, then
consumes available vapor through the prescribed Wiebe burn. The finite hot wall
pays the phase heat; liquid doesn't burn directly. JSON, CLI, portable replay and
the actual MCP server agree at every report boundary. Source, schema and session
contracts remain shared; parameters are `unverified`.

See [VALIDATION.md](VALIDATION.md) for measured numerical evidence. Prepared Unity
film markers and lifecycle checks still require actual Editor/Play verification.
The separate [liquid injector](LIQUID_FUEL_INJECTION.md) now replenishes films from
a finite compliant source. Pump/refill, needle/spray, measured fuel properties,
ignition/ECU, complete intake/exhaust behavior, transmission controls and calibrated
powertrains remain separate requirements.
