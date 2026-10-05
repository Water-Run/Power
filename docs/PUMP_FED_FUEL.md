# Pump-fed liquid fuel rail

**English** · [简体中文](PUMP_FED_FUEL.zh-CN.md) · [Français](PUMP_FED_FUEL.fr.md) · [Русский](PUMP_FED_FUEL.ru.md) · [日本語](PUMP_FED_FUEL.ja.md) · [한국어](PUMP_FED_FUEL.ko.md) · [Deutsch](PUMP_FED_FUEL.de.md) · [Español](PUMP_FED_FUEL.es.md) · [Italiano](PUMP_FED_FUEL.it.md) · [Português](PUMP_FED_FUEL.pt-BR.md)

## Contract

`liquid_rail_feed` pairs a liquid injector with an existing displacement pump and explicit material/thermal boundary. The hydraulic outlet node must match the rail compliance and initial absolute pressure. The paired pump and injector own this pressure node; other untracked fluid paths are rejected.

Pressure energy is stored once in the hydraulic node. Pump flow and shaft reaction follow the conserving coupled solve. Incoming fuel contributes caloric and chemical boundary energy; rail caloric storage mixes its temperature. Signed reverse flow returns fuel at the current rail temperature. Nozzle discharge, wall heating, vapor availability and prescribed burn remain distinct.

The examples `pump-fed-liquid-cylinder` and `pump-fed-needle-cylinder` retain physical injection and optional needle motion. Pressure KPIs use a declared pump-only upper bound, with explicit units. Read `total_fuel_delivered`, `reservoir_enthalpy` and `fuel_energy_in` on feed ID 1511; pump ID 1510 exposes actual shaft-to-fluid work.

## Evidence and limits

Asset v26 retains feed links and source temperature and reads v1-v25. Analytic shaft/pressure exchange, independent simultaneous ODE refinement, caloric mixing, mass/fuel/energy/volume ledgers, reverse return and complete rollback have separate checks.

The source is an explicit external boundary, not a modeled finite fuel tank. Tank depletion, pump efficiency/regulation, line losses, cavitation, pressure-dependent properties and finite-volume spray remain open. Parameters are `unverified`; this does not establish OEM calibration or actual Unity Editor/Play/Player/IL2CPP acceptance.
