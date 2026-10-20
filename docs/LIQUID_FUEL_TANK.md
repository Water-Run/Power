# Finite liquid fuel tank

**English** · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · [Deutsch](LIQUID_FUEL_TANK.de.md) · [Español](LIQUID_FUEL_TANK.es.md) · [Italiano](LIQUID_FUEL_TANK.it.md) · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## Contract

`liquid_fuel_tank` stores finite liquid mass and caloric energy using the paired injector's density, film thermal reference and heating value. A feed selects it with `tank_component` and omits `supply_temperature`. Each tank belongs to one feed with matching fuel properties.

Positive pump flow is bounded by remaining inventory over the accepted interval. The same effective filled displacement sets shaft reaction and pressure transfer, preserving shaft/fluid work. Empty forward rotation delivers no liquid or fluid work; signed return flow mixes the current rail caloric energy into the tank.

Tank caloric and chemical energy enter the complete stored-energy ledger. Tank-to-rail transfer adds no external mass or chemical supply. The explicit prescribed pump inlet pressure retains its pressure-work boundary. Gas inlet/exhaust can still carry chemical boundary energy.

The examples `finite-tank-liquid-cylinder` and `finite-tank-needle-cylinder` use tank ID 1513 and feed ID 1511. Read `mass`, `temperature`, `internal_energy`, `chemical_energy` and `tank_state`; 0 means wet and 1 means empty. Dry temperature reports the declared initial reference.

## Geometric tank and finite headspace

`liquid_fuel_tank.parameters.headspace` declares `capacity` in `m3` or `l` and `gas_node`. The gas node omits `storage`: its volume is `capacity - liquid_mass / density`. It must have one owner and remain positive. The paired pump and relief return use zero prescribed reservoir pressure because the finite gas owns inlet pressure.

[Geometric tank and finite headspace](TANK_HEADSPACE.md)

## Evidence and limits

Asset v34 retains tank data and feed selection and reads v1-v33. Each tank adds 4 reported states within the unchanged bounds. Independent wet exchange, analytic exhausted pressure/shaft energy, reverse mixing, complete ledgers, rollback, forks and allocation-free stepping are checked.

This is a rigid mixed tank with incompressible liquid and ideal gas. Slosh/hydrostatic shape, real-fluid mixture thermodynamics, cavitation, measured pump/valve maps, OEM calibration and actual Unity Editor/Play/Player/IL2CPP remain open. Parameters remain `unverified`.

[VALIDATION.md](VALIDATION.md)

## Tracked fuel relief return

`liquid_rail_return` pairs a feed with an exclusively owned one-way `hydraulic_relief`. The valve connects the rail to the pump's same prescribed inlet pressure. Register every fluid path; incompatible ports, duplicate valve ownership and untracked paths reject.

`fluid_heat_fraction` explicitly selects the fraction in [0,1] of valve loss carried with returned fuel. The remaining heat follows the valve's declared heat path. Simultaneous rail/tank caloric mixing preserves the full mass, chemical, pressure-work and thermal ledgers. External-source returns instead carry mass and energy out through the boundary.

[LIQUID_FUEL_RETURN.md](LIQUID_FUEL_RETURN.md)

## Fuel phase equilibrium and liquid supply limits

The default liquid-supply model rejects wet states below saturation. Optional finite-volume tank equilibrium instead resolves source vapor transfer with pressure-corrected chemical potentials. The rail and pump pickup still carry separated liquid; bubble entrainment and cavitating pump/rail flow require further physics.

The pressure comparison allows only binary64 roundoff: `64 * epsilon * max(p, p_sat)`, with `epsilon = 2^-52`. It doesn't clamp the reported margin. This is a bulk saturation check; local pressure losses and pump NPSH require a resolved flow model. Independent checks also require nondecreasing total entropy for adiabatic flash and finite-wall exchange.

[Fuel phase equilibrium and liquid supply limits](FUEL_PHASE_EQUILIBRIUM.md)

## Finite-volume fuel tank equilibrium

Set `liquid_fuel_tank.parameters.headspace.phase_equilibrium` to `true` to resolve tank evaporation and condensation. The paired film supplies explicit `phase_equilibrium` material properties; the tracked headspace shares its R, cv, heating value and stoichiometric ratio. Declare a common initial gas/liquid temperature and saturated fuel partial pressure for a wet tank; a dry tank starts at or below saturation. Its total inventory density stays below liquid density.

Tank `evaporated_fuel_mass`, `condensed_fuel_mass` and signed `mass_flow` count net phase transfer per complete physical tick. Read `vapor_partial_pressure`, pressure-corrected `saturation_pressure`, liquid inventory and gas volume together. The pump picks up separated liquid only; two-phase pump/rail transport, entrainment and local NPSH remain open.

[Finite-volume fuel tank equilibrium](TANK_PHASE_EQUILIBRIUM.md)
