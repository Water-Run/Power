# Tracked fuel relief return

**English** · [简体中文](LIQUID_FUEL_RETURN.zh-CN.md) · [Français](LIQUID_FUEL_RETURN.fr.md) · [Русский](LIQUID_FUEL_RETURN.ru.md) · [日本語](LIQUID_FUEL_RETURN.ja.md) · [한국어](LIQUID_FUEL_RETURN.ko.md) · [Deutsch](LIQUID_FUEL_RETURN.de.md) · [Español](LIQUID_FUEL_RETURN.es.md) · [Italiano](LIQUID_FUEL_RETURN.it.md) · [Português](LIQUID_FUEL_RETURN.pt-BR.md)

## Contract

`liquid_rail_return` pairs a feed with an exclusively owned one-way `hydraulic_relief`. The valve connects the rail to the pump's same prescribed inlet pressure. Register every fluid path; incompatible ports, duplicate valve ownership and untracked paths reject.

`fluid_heat_fraction` explicitly selects the fraction in [0,1] of valve loss carried with returned fuel. The remaining heat follows the valve's declared heat path. Simultaneous rail/tank caloric mixing preserves the full mass, chemical, pressure-work and thermal ledgers. External-source returns instead carry mass and energy out through the boundary.

Read `total_fuel_delivered`, `reservoir_enthalpy`, `fuel_energy_in`, `fluid_heat` and `mass_flow` on return ID 1515. Feed ID 1511 reports gross pump transfer. Gross circulation can exceed initial tank inventory; current inventory equals initial inventory minus pump transfer plus return.

## Evidence and limits

`recirculating-liquid-cylinder` and `recirculating-needle-cylinder` retain finite fuel, actual injection, evaporation and optional needle dynamics. Asset v28 retains return links and heat fraction and reads v1-v27. Each return adds 8 states within the unchanged model bounds.

Independent relief decay/pressure work and simultaneous mechanical, pressure and thermal refinement, heat fractions, multiple routes, external boundaries, every-boundary replay, rollback and allocation checks pass. Liquid boiling and unresolved transport intervals fail the entire batch. Geometric tank/vent dynamics, measured valves/pumps, cavitation, spray, OEM calibration and actual Unity Editor/Play/Player/IL2CPP remain open.

[VALIDATION.md](VALIDATION.md)
