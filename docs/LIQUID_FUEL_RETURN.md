# Tracked fuel relief return

**English** · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## Contract

`liquid_rail_return` pairs a feed with an exclusively owned one-way `hydraulic_relief`. The valve connects the rail to the pump's same prescribed inlet pressure. Register every fluid path; incompatible ports, duplicate valve ownership and untracked paths reject.

`fluid_heat_fraction` explicitly selects the fraction in [0,1] of valve loss carried with returned fuel. The remaining heat follows the valve's declared heat path. Simultaneous rail/tank caloric mixing preserves the full mass, chemical, pressure-work and thermal ledgers. External-source returns instead carry mass and energy out through the boundary.

Read `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` and `mass_flow` on return ID 1515. Feed ID 1511 reports gross pump transfer. Gross circulation can exceed initial tank inventory; current inventory equals initial inventory minus pump transfer plus return.

## Evidence and limits

`recirculating-liquid-cylinder` and `recirculating-needle-cylinder` retain finite fuel, actual injection, evaporation and optional needle dynamics. Asset v34 retains return links and heat fraction and reads v1-v33. Each return adds 8 states within the unchanged model bounds.

Independent relief decay/pressure work and simultaneous mechanical, pressure and thermal refinement, heat fractions, multiple routes, external boundaries, every-boundary replay, rollback and allocation checks pass. Liquid boiling and unresolved transport intervals fail the entire batch.

This is a rigid mixed tank with incompressible liquid and ideal gas. Slosh/hydrostatic shape, real-fluid mixture thermodynamics, cavitation, measured pump/valve maps, OEM calibration and actual Unity Editor/Play/Player/IL2CPP remain open. Parameters remain `unverified`.

[VALIDATION.md](VALIDATION.md)

## Geometric tank and finite headspace

`liquid_fuel_tank.parameters.headspace` declares `capacity` in `m3` or `l` and `gas_node`. The gas node omits `storage`: its volume is `capacity - liquid_mass / density`. It must have one owner and remain positive. The paired pump and relief return use zero prescribed reservoir pressure because the finite gas owns inlet pressure.

[Geometric tank and finite headspace](TANK_HEADSPACE.md)

## Fuel phase equilibrium and liquid supply limits

The default liquid-supply model rejects wet states below saturation. Optional finite-volume tank equilibrium instead resolves source vapor transfer with pressure-corrected chemical potentials. The rail and pump pickup still carry separated liquid; bubble entrainment and cavitating pump/rail flow require further physics.

The pressure comparison allows only binary64 roundoff: `64 * epsilon * max(p, p_sat)`, with `epsilon = 2^-52`. It doesn't clamp the reported margin. This is a bulk saturation check; local pressure losses and pump NPSH require a resolved flow model. Independent checks also require nondecreasing total entropy for adiabatic flash and finite-wall exchange.

[Fuel phase equilibrium and liquid supply limits](FUEL_PHASE_EQUILIBRIUM.md)

## Finite-volume fuel tank equilibrium

Set `liquid_fuel_tank.parameters.headspace.phase_equilibrium` to `true` to resolve tank evaporation and condensation. The paired film supplies explicit `phase_equilibrium` material properties; the tracked headspace shares its R, cv, heating value and stoichiometric ratio. Declare a common initial gas/liquid temperature and saturated fuel partial pressure for a wet tank; a dry tank starts at or below saturation. Its total inventory density stays below liquid density.

Tank `evaporated_fuel_mass`, `condensed_fuel_mass` and signed `mass_flow` count net phase transfer per complete physical tick. Read `vapor_partial_pressure`, pressure-corrected `saturation_pressure`, liquid inventory and gas volume together. The pump picks up separated liquid only; two-phase pump/rail transport, entrainment and local NPSH remain open.

[Finite-volume fuel tank equilibrium](TANK_PHASE_EQUILIBRIUM.md)
