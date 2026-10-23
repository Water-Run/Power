# Fuel phase equilibrium and liquid supply limits

**English** · [简体中文](FUEL_PHASE_EQUILIBRIUM.zh-CN.md) · [Français](FUEL_PHASE_EQUILIBRIUM.fr.md) · [Русский](FUEL_PHASE_EQUILIBRIUM.ru.md) · [日本語](FUEL_PHASE_EQUILIBRIUM.ja.md) · [한국어](FUEL_PHASE_EQUILIBRIUM.ko.md) · [Deutsch](FUEL_PHASE_EQUILIBRIUM.de.md) · [Español](FUEL_PHASE_EQUILIBRIUM.es.md) · [Italiano](FUEL_PHASE_EQUILIBRIUM.it.md) · [Português](FUEL_PHASE_EQUILIBRIUM.pt-BR.md)

## Contract

Optional `fuel_film.parameters.phase_equilibrium` declares `reference_pressure` in `pa` or `bar`, and `minimum_temperature` / `maximum_temperature` in `k`. Existing `saturation_temperature` is the reference temperature; `latent_internal_energy` is the vapor-minus-liquid internal energy there. One film owns each equilibrium receiver. The authored initial snapshot is retained; accepted intervals equilibrate gas and liquid and exchange heat with the finite wall.

`mass_flow` is the signed net phase transfer over the complete physical tick. `evaporated_fuel_mass` and `condensed_fuel_mass` accumulate its positive and negative mass amounts, so canceling split-operator projections are not counted twice. `vapor_partial_pressure` reports the receiver fuel partial pressure. Thermal, mass and chemical ledgers include both phases.

## Thermodynamic reference

The model uses one ideal pseudo-species, the receiver's common gas constant and constant heat capacities, and negligible liquid displacement. Equality of phase chemical potentials gives the saturation relation below. At fixed volume, total fuel and internal energy determine the common temperature and vapor mass. Evaporation and condensation transfer chemical inventory internally; reaction still consumes vapor only.

```text
a = (cv - cl) T_ref - L_u,ref
u_l(T) = cl T + a
u_v(T) = cv T
L_u(T) = (cv - cl) T - a
ln(p_sat(T) / p_ref) = (cv + R - cl) / R * ln(T / T_ref)
                        + a / R * (1/T - 1/T_ref)
m_v(T) = min(M_f, p_sat(T) V / (R T))
U_eq(T) = (M_c + m_v) cv T + (M_f - m_v) u_l(T)
```

## Supply and numerical limits

A bounded scalar solve uses the monotone equilibrium energy relation. Finite-wall heat uses backward Euler. Coupling to transport and moving-cylinder work has first-order refinement evidence. Liquid coexistence stays within the declared temperature interval. Completely evaporated gas can superheat after dryout within that interval; the saturation curve is not extrapolated.

The default liquid-supply model rejects wet states below saturation. Optional finite-volume tank equilibrium instead resolves source vapor transfer with pressure-corrected chemical potentials. The rail and pump pickup still carry separated liquid; bubble entrainment and cavitating pump/rail flow require further physics.

The pressure comparison allows only binary64 roundoff: `64 * epsilon * max(p, p_sat)`, with `epsilon = 2^-52`. It doesn't clamp the reported margin. This is a bulk saturation check; local pressure losses and pump NPSH require a resolved flow model. Independent checks also require nondecreasing total entropy for adiabatic flash and finite-wall exchange.

## Evidence

`equilibrium-fuel-cell` demonstrates condensation, vent-driven evaporation and renewed condensation. `equilibrium-injected-cylinder` adds reversible receiver phase behavior and observable supply margins to the vented-tank engine. Independent entropy maximization, analytic saturation/heat limits, simultaneous ODE refinement, complete replay, rollback and allocation checks cover the implementation. Asset v35 retains these properties and reads v1-v34.

Parameters remain `unverified`. The model does not supply measured gasoline properties, multicomponent or finite-rate phase kinetics, resolved liquid displacement/spray, or OEM calibration. Actual Unity Editor/Play/Player/IL2CPP acceptance remains pending.

[Cantera: species thermodynamics](https://www.cantera.org/stable/reference/thermo/species-thermo.html) · [Cantera: equilibrium](https://www.cantera.org/stable/python/thermo.html) · [VALIDATION.md](VALIDATION.md)

## Finite-volume fuel tank equilibrium

Set `liquid_fuel_tank.parameters.headspace.phase_equilibrium` to `true` to resolve tank evaporation and condensation. The paired film supplies explicit `phase_equilibrium` material properties; the tracked headspace shares its R, cv, heating value and stoichiometric ratio. Declare a common initial gas/liquid temperature and saturated fuel partial pressure for a wet tank; a dry tank starts at or below saturation. Its total inventory density stays below liquid density.

Tank `evaporated_fuel_mass`, `condensed_fuel_mass` and signed `mass_flow` count net phase transfer per complete physical tick. Read `vapor_partial_pressure`, pressure-corrected `saturation_pressure`, liquid inventory and gas volume together. The pump picks up separated liquid only; two-phase pump/rail transport, entrainment and local NPSH remain open.

[Finite-volume fuel tank equilibrium](TANK_PHASE_EQUILIBRIUM.md)
