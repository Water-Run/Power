# Pump-fed liquid fuel rail

**English** · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## Contract

`liquid_rail_feed` pairs a liquid injector with an existing displacement pump and explicit material/thermal boundary. The hydraulic outlet node must match the rail compliance and initial absolute pressure. The paired pump and injector own this pressure node; other untracked fluid paths are rejected.

Pressure energy is stored once in the hydraulic node. Pump flow and shaft reaction follow the conserving coupled solve. Incoming fuel contributes caloric and chemical boundary energy; rail caloric storage mixes its temperature. Signed reverse flow returns fuel at the current rail temperature. Nozzle discharge, wall heating, vapor availability and prescribed burn remain distinct.

The examples `pump-fed-liquid-cylinder` and `pump-fed-needle-cylinder` retain physical injection and optional needle motion. Pressure KPIs use a declared pump-only upper bound, with explicit units. Read `total_fuel_delivered`, `reservoir_enthalpy` and `fuel_energy_in` on feed ID 1511; pump ID 1510 exposes actual shaft-to-fluid work.

## Evidence and limits

Asset v28 retains feed links and source temperature and reads v1-v27. Analytic shaft/pressure exchange, independent simultaneous ODE refinement, caloric mixing, mass/fuel/energy/volume ledgers, reverse return and complete rollback have separate checks.

Geometric capacity, vent/headspace/slosh, cavitation, measured pump filling/efficiency/regulation and resolved spray remain open. Parameters are `unverified`; actual Unity Editor/Play/Player/IL2CPP and OEM calibration remain unverified.

## Finite liquid fuel tank

`liquid_fuel_tank` stores finite liquid mass and caloric energy using the paired injector's density, film thermal reference and heating value. A feed selects it with `tank_component` and omits `supply_temperature`. Each tank belongs to one feed with matching fuel properties.

Tank caloric and chemical energy enter the complete stored-energy ledger. Tank-to-rail transfer adds no external mass or chemical supply. The explicit prescribed pump inlet pressure retains its pressure-work boundary. Gas inlet/exhaust can still carry chemical boundary energy.

Geometric capacity, vent/headspace/slosh, cavitation, measured pump filling/efficiency/regulation and resolved spray remain open. Parameters are `unverified`; actual Unity Editor/Play/Player/IL2CPP and OEM calibration remain unverified.

[LIQUID_FUEL_TANK.md](LIQUID_FUEL_TANK.md)

## Tracked fuel relief return

`liquid_rail_return` pairs a feed with an exclusively owned one-way `hydraulic_relief`. The valve connects the rail to the pump's same prescribed inlet pressure. Register every fluid path; incompatible ports, duplicate valve ownership and untracked paths reject.

`fluid_heat_fraction` explicitly selects the fraction in [0,1] of valve loss carried with returned fuel. The remaining heat follows the valve's declared heat path. Simultaneous rail/tank caloric mixing preserves the full mass, chemical, pressure-work and thermal ledgers. External-source returns instead carry mass and energy out through the boundary.

[LIQUID_FUEL_RETURN.md](LIQUID_FUEL_RETURN.md)
