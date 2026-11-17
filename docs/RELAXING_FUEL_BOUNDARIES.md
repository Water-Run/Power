# Relaxing fuel pumps and frozen metering

**English** · [简体中文](RELAXING_FUEL_BOUNDARIES.zh-CN.md) · [Français](RELAXING_FUEL_BOUNDARIES.fr.md) · [Русский](RELAXING_FUEL_BOUNDARIES.ru.md) · [日本語](RELAXING_FUEL_BOUNDARIES.ja.md) · [한국어](RELAXING_FUEL_BOUNDARIES.ko.md) · [Deutsch](RELAXING_FUEL_BOUNDARIES.de.md) · [Español](RELAXING_FUEL_BOUNDARIES.es.md) · [Italiano](RELAXING_FUEL_BOUNDARIES.it.md) · [Português](RELAXING_FUEL_BOUNDARIES.pt-BR.md)

## Physical model

A `compressible_fuel_chamber` can own the volume of a `relaxing_fuel` reservoir. Reversible shaft work holds vapor mass quality fixed; chemical relaxation is a separate energy-conserving source. A `compressible_fuel_nozzle` connects two relaxing reservoirs with `phase_mode` set to `frozen`, transporting total mass, vapor mass and stagnation enthalpy. An actual solenoid, spring and stop can meter its opening through a physical needle.

[NASA: isentropic ideal-gas relations](https://www.grc.nasa.gov/www/k-12/airplane/isentrop.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Coupling and numerics

The fixed-quality entropy relation determines pressure and temperature after a volume change. Work includes liquid cold compression energy and uses stable incremental functions for small motion. The identical discrete work drives shaft torque. Global intervals apply each node's phase source once around conservative duct and frozen nozzle transport. Duct CFL and a 0.02 fraction of the smaller nozzle-port inventory bound intervals; at most 4096 intervals are allowed. Mechanical work is separated from source evolution. Coupled transport remains first order and needs temporal refinement.

```text
x = vapor_mass / total_mass
cp_x = (1-x) c_liquid + x (c_vapor+R)
cv_x = (1-x) c_liquid + x c_vapor
alpha = x R/cp_x
T/T0 = (p/p0)^alpha                         (fixed quality and entropy)
v(p) = (1-x)/(rho0 (1+p/K)) + x R T/p
work_on_fluid = mass [cv_x T0 expm1(alpha ln(p/p0)) + (1-x) delta_u_cold]
fluid_torque = -work_on_fluid/delta_angle   (nonzero angular increment)
delta_vapor_mass_volume_work = 0
delta_vapor_mass_nozzle = transferred_total_mass * source_vapor_quality
donor_delta_energy = -transferred_total_mass * stagnation_enthalpy
receiver_delta_energy = -donor_delta_energy - wall_fraction * jet_kinetic_energy
dx/dt_phase_source = (x_equilibrium(rho,u)-x)/tau
```

## Use, evidence and limits

Run `relaxing-fuel-chamber` or `relaxing-pump-needle`. Observe chamber volume/work, actual and equilibrium quality, phase entropy, `total_vapor_delivered`, port inventories and the global vapor residual. Checks compare pure-phase adiabats, independent fixed-quality pressure quadrature, infinitesimal work, a reversible cycle and an independent ideal-vapor shaft ODE. Coupled finite-rate refinement, shared-reservoir source time, complete ledgers, cancellation, rollback, branches, allocations, portable replay and actual MCP have separate checks. Numerical values are in validation evidence.

Parameters remain `unverified`. Both nozzle ports must use the same domain and material; relaxing ports reject equilibrium transit. Finite bulk phase relaxation does not resolve chemical conversion or residence inside a zero-volume nozzle. The model has no phase slip, liquid tension, nucleation-barrier law, spray, carrier gas or fired receiver. Moving reservoirs are mixed with zero bulk velocity; internal duct cells remain rigid and have one owner. Use dilute ideal vapor away from critical behavior, declared positive-pressure/temperature bounds, positive latent enthalpy and separated coexistence volumes. Initial vapor quality is explicit research input. Numerical agreement does not establish measured calibration or actual Unity acceptance.

Add resolved finite-rate nozzle residence with material evidence, then resolved spray and compatible fired receivers. Continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control. Measured calibration and actual Unity acceptance remain separate.

[VALIDATION.md](VALIDATION.md)
