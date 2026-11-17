# Finite-rate fuel phase relaxation

**English** · [简体中文](RELAXING_FUEL.zh-CN.md) · [Français](RELAXING_FUEL.fr.md) · [Русский](RELAXING_FUEL.ru.md) · [日本語](RELAXING_FUEL.ja.md) · [한국어](RELAXING_FUEL.ko.md) · [Deutsch](RELAXING_FUEL.de.md) · [Español](RELAXING_FUEL.es.md) · [Italiano](RELAXING_FUEL.it.md) · [Português](RELAXING_FUEL.pt-BR.md)

## Physical model

A `relaxing_fuel` node carries independent vapor mass quality. Liquid and vapor share pressure, temperature and velocity, while chemical equilibrium takes a declared finite time. A `relaxing_fuel_duct` transports mass, momentum, total energy and vapor mass through explicit cells. Residence follows cell geometry and actual velocity.

The closure retains the compressible liquid cold potential and ideal vapor of the equilibrium fuel model. Initial quality is explicit and is not projected to equilibrium. At fixed mass, volume and internal energy, quality approaches the equilibrium target exponentially with constant `phase_relaxation_time_ns`. Latent heat stays inside the energy balance. Chemical affinity is liquid minus vapor chemical potential; its product with phase transfer gives nonnegative entropy production divided by temperature. This is a research relaxation law, not a measured nucleation correlation.

[Lund: A hierarchy of relaxation models for two-phase flow](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)

## Transport and numerical method

Conservative Rusanov fluxes advect four fields. Each global CFL interval applies half a local phase source, transport, then half a source. Shared reservoirs receive each source once, regardless of duct count. The source trajectory is exact for isolated fixed-volume states; the coupled method retains first-order transport error. CFL uses the frozen-quality acoustic bound. Local phase and duct entropy changes are checked separately. Out-of-range states reject the complete batch.

```text
x = vapor_mass / total_mass; v = 1/rho
v_liquid(p) = 1 / (rho0 (1 + p/K)); v_vapor(T,p) = R T/p
v = (1-x) v_liquid + x v_vapor
u = (1-x) [c_liquid T + a + u_cold(p)] + x c_vapor T
u_cold(p) = K/rho0 [ln(1+p/K) - (p/K)/(1+p/K)]

dx/dt = (x_equilibrium(rho,u) - x)/tau
x(t+h) = x_equilibrium + (x(t)-x_equilibrium) exp(-h/tau)
d(total_mass)/dt = d(total_internal_energy)/dt = d(volume)/dt = 0  (local source)
affinity = mu_liquid - mu_vapor
(ds/dx) at fixed u,v = affinity/T
ds/dt = affinity (x_equilibrium-x)/(T tau) >= 0

cp_frozen = (1-x) c_liquid + x (c_vapor+R)
alpha = x R/cp_frozen
c_frozen^2 = v^2 / [(1-x) v_liquid/(K+p) + (1-alpha) x R T/p^2]
U = (rho, rho velocity, rho total_specific_energy, rho x)
F = (rho velocity, rho velocity^2+p,
     velocity (rho total_specific_energy+p), rho x velocity)
```

## Use, evidence and limits

Run `relaxing-fuel-evaporation` or `relaxing-fuel-duct`. Observe actual and equilibrium quality, chemical affinity, phase entropy, vapor port inventories and the global vapor residual. Evaporated/condensed histories split the net source transfer of each accepted tick by sign; they are not gross microscopic forward/reverse traffic. Checks cover the independent exponential solution, entropy derivatives, frozen acoustics, stiff equilibrium limit, temporal self-refinement and independent linear-acoustic mesh refinement. Transactions, portable replay and actual MCP are checked separately; numbers are recorded in validation evidence.

Parameters remain `unverified`. Both nozzle ports must use the same domain and material; relaxing ports reject equilibrium transit. Finite bulk phase relaxation does not resolve chemical conversion or residence inside a zero-volume nozzle. The model has no phase slip, liquid tension, nucleation-barrier law, spray, carrier gas or fired receiver. Moving reservoirs are mixed with zero bulk velocity; internal duct cells remain rigid and have one owner. Use dilute ideal vapor away from critical behavior, declared positive-pressure/temperature bounds, positive latent enthalpy and separated coexistence volumes. Initial vapor quality is explicit research input. Numerical agreement does not establish measured calibration or actual Unity acceptance.

Add resolved finite-rate nozzle residence with material evidence, then resolved spray and compatible fired receivers. Continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control. Measured calibration and actual Unity acceptance remain separate.

[VALIDATION.md](VALIDATION.md)

A `compressible_fuel_chamber` can own the volume of a `relaxing_fuel` reservoir. Reversible shaft work holds vapor mass quality fixed; chemical relaxation is a separate energy-conserving source. A `compressible_fuel_nozzle` connects two relaxing reservoirs with `phase_mode` set to `frozen`, transporting total mass, vapor mass and stagnation enthalpy. An actual solenoid, spring and stop can meter its opening through a physical needle. [Relaxing fuel pumps and frozen metering](RELAXING_FUEL_BOUNDARIES.md)
