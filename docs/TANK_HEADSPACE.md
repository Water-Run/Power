# Geometric tank and finite headspace

**English** · [简体中文](TANK_HEADSPACE.zh-CN.md) · [Français](TANK_HEADSPACE.fr.md) · [Русский](TANK_HEADSPACE.ru.md) · [日本語](TANK_HEADSPACE.ja.md) · [한국어](TANK_HEADSPACE.ko.md) · [Deutsch](TANK_HEADSPACE.de.md) · [Español](TANK_HEADSPACE.es.md) · [Italiano](TANK_HEADSPACE.it.md) · [Português](TANK_HEADSPACE.pt-BR.md)

## Contract

`liquid_fuel_tank.parameters.headspace` declares `capacity` in `m3` or `l` and `gas_node`. The gas node omits `storage`: its volume is `capacity - liquid_mass / density`. It must have one owner and remain positive. The paired pump and relief return use zero prescribed reservoir pressure because the finite gas owns inlet pressure.

The coupled solve exchanges shaft, rail and gas pressure work without an external pressure source. Gas orifices and heat links provide explicit vent/thermal paths. Read tank `pressure`, `fill_fraction` and signed cumulative `hydraulic_work`, plus headspace gas mass, energy and volume. Prescribed dose, delivered liquid, evaporation and burn remain separate.

## Evidence and limits

The examples `vented-tank-liquid-cylinder` and `vented-tank-needle-cylinder` use tank 1513, headspace 1520 and vent input 960. Asset v29 stores geometry and reads v1-v28. Analytic work/derivative checks, independent simultaneous ODE refinement, complete ledgers, portable/MCP replay, rollback and allocation-free stepping pass.

This is a rigid mixed tank with incompressible liquid and ideal gas. Slosh/hydrostatic shape, vapor-phase equilibrium, cavitation, measured pump/valve maps, OEM calibration and actual Unity Editor/Play/Player/IL2CPP remain open. Parameters remain `unverified`.

[NASA](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Cantera](https://www.cantera.org/stable/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.md)
