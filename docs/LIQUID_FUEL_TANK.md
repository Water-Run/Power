# Finite liquid fuel tank

**English** · [简体中文](LIQUID_FUEL_TANK.zh-CN.md) · [Français](LIQUID_FUEL_TANK.fr.md) · [Русский](LIQUID_FUEL_TANK.ru.md) · [日本語](LIQUID_FUEL_TANK.ja.md) · [한국어](LIQUID_FUEL_TANK.ko.md) · [Deutsch](LIQUID_FUEL_TANK.de.md) · [Español](LIQUID_FUEL_TANK.es.md) · [Italiano](LIQUID_FUEL_TANK.it.md) · [Português](LIQUID_FUEL_TANK.pt-BR.md)

## Contract

`liquid_fuel_tank` stores finite liquid mass and caloric energy using the paired injector's density, film thermal reference and heating value. A feed selects it with `tank_component` and omits `supply_temperature`. Each tank belongs to one feed with matching fuel properties.

Positive pump flow is bounded by remaining inventory over the accepted interval. The same effective filled displacement sets shaft reaction and pressure transfer, preserving shaft/fluid work. Empty forward rotation delivers no liquid or fluid work; signed return flow mixes the current rail caloric energy into the tank.

Tank caloric and chemical energy enter the complete stored-energy ledger. Tank-to-rail transfer adds no external mass or chemical supply. The explicit prescribed pump inlet pressure retains its pressure-work boundary. Gas inlet/exhaust can still carry chemical boundary energy.

The examples `finite-tank-liquid-cylinder` and `finite-tank-needle-cylinder` use tank ID 1513 and feed ID 1511. Read `mass`, `temperature`, `internal_energy`, `chemical_energy` and `tank_state`; 0 means wet and 1 means empty. Dry temperature reports the declared initial reference.

## Evidence and limits

Asset v27 retains tank data and feed selection and reads v1-v26. Each tank adds 4 reported states within the unchanged bounds. Independent wet exchange, analytic exhausted pressure/shaft energy, reverse mixing, complete ledgers, rollback, forks and allocation-free stepping are checked.

Geometric capacity, vent/headspace/slosh, cavitation, measured pump filling/efficiency/regulation and resolved spray remain open. Parameters are `unverified`; actual Unity Editor/Play/Player/IL2CPP and OEM calibration remain unverified.

[VALIDATION.md](VALIDATION.md)
