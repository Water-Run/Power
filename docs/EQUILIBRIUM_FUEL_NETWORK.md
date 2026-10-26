# Equilibrium fuel transport network

**English** · [简体中文](EQUILIBRIUM_FUEL_NETWORK.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NETWORK.fr.md) · [Русский](EQUILIBRIUM_FUEL_NETWORK.ru.md) · [日本語](EQUILIBRIUM_FUEL_NETWORK.ja.md) · [한국어](EQUILIBRIUM_FUEL_NETWORK.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NETWORK.de.md) · [Español](EQUILIBRIUM_FUEL_NETWORK.es.md) · [Italiano](EQUILIBRIUM_FUEL_NETWORK.it.md) · [Português](EQUILIBRIUM_FUEL_NETWORK.pt-BR.md)

## Model scope

The `fuel` domain carries pure liquid and vapor together in finite elastic vessels. Each vessel owns independent total mass and caloric-plus-elastic energy; its phase flash determines temperature, pressure and vapor fractions. `fuel_restriction` exchanges homogeneous donor fluid and `fuel_heat_link` exchanges heat with a finite thermal node. Chemical heating-value energy stays in a separate ledger.

A fuel node uses `storage` for reference volume in `m3` or `l`, `initial` for temperature in `k`, and `position` for total mass in `kg`. Its `fuel` object declares the material, compliance and pressure/temperature limits explicitly. Connected vessels share density, caloric and phase reference, temperature limits and heating value. Both restriction ports are finite fuel nodes; a heat link joins fuel to a thermal node. Restriction opening is a fraction in [0,1], with an optional input channel.

## Conservative flow

The restriction is a quasi-static dissipative reference-volume law with explicit conductance K in `m3_s_pa`. It neglects link inertia, kinetic-energy storage and phase slip. Donor density converts reference volume flow to total fuel mass flow; donor enthalpy carries both phases. There is no extra external pressure-work source and no separate heat term added for the pressure drop. Elastic work is already stored in the vessel energy.

```text
Q_ref = K opening (p_s - p_r)
mdot = Q_ref / v_s
q = dt K opening (p_s' - p_r') / v_s'
H = q h_s'
M_s' = M_s - q; E_s' = E_s - H
M_r' = M_r + q; E_r' = E_r + H
h = (E - E_elastic + p V) / M
```

## Time integration and entropy

A bounded backward-Euler solve finds the same accepted mass q and enthalpy H for both vessels. Links advance in stable component-ID order; finite-bath heat uses another conservative substep. The coupled method is first order. Out-of-range mass, temperature or pressure and failed solves reject the complete batch, including inputs, compensated histories and speculative clutch intervals.

Compatible material thermodynamics and positive pressure-driven conductance give nonnegative continuous pair entropy production. Backward Euler adds numerical entropy. The `entropy_produced` channel records the complete accepted pair entropy change, including mixing and time discretization; it is not a measured loss map. Independent entropy checks accompany conservation and refinement.

## Use and evidence

Request `equilibrium-fuel-network` or `electrothermal-fuel-network` through MCP, or run the same JSON through CLI. Read `mass`, `internal_energy`, `elastic_energy`, `vapor_mass`, `vapor_mass_fraction`, `vapor_volume_fraction`, `entropy` and `specific_enthalpy` on each vessel. Here `internal_energy` includes the elastic wall; chemical energy is reported separately. Links expose signed mass/volume flow, cumulative transferred mass, `transport_energy` and pair entropy change.

The laboratories exercise liquid discharge into coexistence, reverse flow, opening changes and finite-wall/electromechanical heat coupling. An independent liquid-pressure exponential, simultaneous two-phase mass/enthalpy/heat ODE, heat-driven reversal and speculative clutch capture check accuracy and composition. Portable assets and actual MCP preserve every sampled value and hash. Asset v37 retains the new material records and reads v1-v36. Actual Unity Editor/Play/Player/IL2CPP remains unverified.

## Remaining work

Fuel-domain transport now composes with shaft-driven `fuel_chamber` components and ideal `fuel_check_valve` links. Physical injector handoff, local inertia/choking and measured real-mixture behavior remain separate work. Parameters remain `unverified`. [Shaft-driven phase-resolving fuel pump](FUEL_CHAMBER_PUMP.md)

[Modelica: two-port transport](https://doc.modelica.org/om/Modelica.Fluid.Interfaces.PartialTwoPortTransport.html) · [CompliantFuelVolume](COMPLIANT_FUEL_VOLUME.md) · [VALIDATION.md](VALIDATION.md)
