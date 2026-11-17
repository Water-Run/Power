# Frozen-quality fuel discharge and finite receivers

**English** · [简体中文](FUEL_DISCHARGE.zh-CN.md) · [Français](FUEL_DISCHARGE.fr.md) · [Русский](FUEL_DISCHARGE.ru.md) · [日本語](FUEL_DISCHARGE.ja.md) · [한국어](FUEL_DISCHARGE.ko.md) · [Deutsch](FUEL_DISCHARGE.de.md) · [Español](FUEL_DISCHARGE.es.md) · [Italiano](FUEL_DISCHARGE.it.md) · [Português](FUEL_DISCHARGE.pt-BR.md)

## Physical model

A `fuel_discharge` connects a finite fuel-domain source to the gas node of a `finite_fuel_film`. Declare `phase_mode` as `frozen`: vapor mass quality stays fixed during nozzle transit, while both bulk endpoints re-equilibrate. The homogeneous phases share temperature and velocity; liquid is incompressible and vapor is an ideal pseudo-species. Source and receiver material properties and liquid density must agree.

For upstream stagnation pressure and temperature, maximize the following mass flux between receiver and upstream pressure. The unique interior maximum limits choked flow. The pure-liquid limit is Bernoulli and the pure-vapor limit is the ideal-gas sonic relation. The discharge coefficient scales effective throat area.

```text
x = upstream vapor mass fraction; r = p/p0
cp = (1-x) cl + x (cv+R); k = x R/cp; b = (1-x)/rho_l
T(p) = T0 r^k
h0-h(p) = cp T0 (1-r^k) + b p0 (1-r)
v(p) = b + x R T(p)/p
G(p) = sqrt(2 (h0-h(p))) / v(p)
mass_rate = opening Cd A max(G)
jet_kinetic_energy_per_kg = h0-h(p_receiver)
gas_volume = cavity_volume - liquid_mass/rho_l
```

## Energy, volume and momentum

The source loses transported stagnation enthalpy. `wall_kinetic_fraction` assigns part of the ideal fully expanded jet kinetic energy to the finite stationary film wall; the remaining energy enters receiver fluid. This reference speed assumes ideal expansion to receiver pressure and does not predict a measured nozzle exit speed. Jet impulse goes to the stationary receiver, with the opposite housing reaction; these stationary reactions do no work and add no piston or needle force.

Declare `finite_volume_density` explicitly. Gas `volume` is cavity volume minus liquid mass/density; film `volume` is displaced liquid volume. A moving cylinder uses closed mass/entropy pressure work on the entire receiver. Displacement work stays internal. Fixed cavities and crank cylinders are supported; a linear gas piston cannot own this receiver.

## Use and evidence

Run `finite-volume-fuel-discharge` for mixed-source choking, or `pump-fed-finite-volume-cylinder` for electric pump, physical solenoid needle and prescribed firing. Optional `needle` motion and `valve_timing` multiply opening. Explicit voltage pulses drive the new example; it has no requested cycle mass or sampled dose controller. Read `jet_kinetic_energy`, `jet_impulse`, `force`, `throat_pressure`, `exit_temperature`, `exit_velocity` and `choked` alongside the complete inventories. The last four values retain the last accepted flowing nozzle state when flow stops.

Independent Bernoulli and ideal-gas limits, a separate mixture flux maximization, energy/entropy checks and an independently integrated liquid-nozzle/receiver ODE test the equations. Time refinement is first order. Portable asset replay, actual MCP, rollback, cancellation, independent branches and allocation checks cover integration. Actual Unity Editor/Play/Player/IL2CPP remains unverified.

## Validity and next work

Parameters remain `unverified`. Enforce declared temperature bounds, positive gas volume and the dilute-vapor material domain. The model omits local inlet inertia, finite-rate nucleation, phase slip, resolved spray, real-fluid critical behavior and measured hardware calibration. Prescribed combustion does not establish chemical-equilibrium or reaction-entropy accuracy. Continue inlet dynamics, ignition, intake/exhaust, mechanical losses and coordinated ECU/TCU control.

[NASA: ideal-gas mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html) · [NASA-CR-64098: two-phase mass-limiting experiments](https://ntrs.nasa.gov/citations/19650020869) · [VALIDATION.md](VALIDATION.md)
