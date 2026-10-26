# Shaft-driven compressible fuel chambers

**English** · [简体中文](COMPRESSIBLE_FUEL_CHAMBER.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_CHAMBER.fr.md) · [Русский](COMPRESSIBLE_FUEL_CHAMBER.ru.md) · [日本語](COMPRESSIBLE_FUEL_CHAMBER.ja.md) · [한국어](COMPRESSIBLE_FUEL_CHAMBER.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_CHAMBER.de.md) · [Español](COMPRESSIBLE_FUEL_CHAMBER.es.md) · [Italiano](COMPRESSIBLE_FUEL_CHAMBER.it.md) · [Português](COMPRESSIBLE_FUEL_CHAMBER.pt-BR.md)

## Physical contract

A `compressible_fuel_chamber` connects a rotational shaft to a finite `compressible_fuel` reservoir. Shaft motion changes its volume, while the same pressure work changes fluid energy and shaft torque. The reservoir can supply or receive an `equilibrium_fuel_duct`, including equilibrium flashing and condensation.

Port `node_a` is rotational and `node_b` owns one chamber geometry. Declare `minimum_reference_volume`, `swept_volume` and `phase`; initial node storage must match volume at the initial shaft angle. The mixed reservoir has zero bulk velocity and cannot be an internal duct cell. The massless sinusoidal mechanism adds no piston or cam inertia; shaft inertia is explicit. External case pressure is zero.

## Work and transport

The closed work step holds mass and entropy fixed. Liquid cold compression energy and caloric energy use the same equilibrium potential as the duct. Liquid-only motion uses the exact pressure integral; ideal-vapor motion uses its analytic adiabat. Phase-changing motion inverts entropy and checks the final energy state. The accepted energy difference supplies a discrete pressure torque in the joint mechanical solve. Positive `hydraulic_work` means energy from shaft to fluid.

```text
V(theta) = Vmin + Vs sin^2((theta-phase)/2)
dE_fluid = -p dV; dM = 0; dS = 0
tau_pressure = -Delta_E_fluid / Delta_theta
tau_pressure(Delta_theta=0) = p dV/dtheta
Delta_E_shaft + Delta_E_fluid = 0
p_liquid = K (M/(rho0 V)-1)
Delta_E_liquid = K [Delta_V - (M/rho0) ln(V_next/V_old)]
T_vapor_next = T_old (V_old/V_next)^(R/cv)
Delta_E_vapor = M cv (T_next-T_old)
```

A moving reservoir uses half duct transport, full shaft work, then half transport. Transport remains first order with numerical diffusion; symmetric splitting does not raise its order. Closed smooth liquid shaft motion has second-order refinement. Angular increments are limited to 0.25 rad per mechanical interval. Duct CFL, material bounds, full rollback, cancellation and independent branches still apply. Stationary port/housing impulse is separate from piston pressure work.

## Use and evidence

Run `compressible-fuel-chamber-duct` or `electric-compressible-fuel-duct`. Read reservoir mass, volume, pressure, temperature, entropy and vapor fraction alongside cell states and chamber torque, power and work. Checks use independent liquid work and ideal-vapor limits, a separately inverted equilibrium isentrope and pressure quadrature, a liquid shaft ODE and ideal-vapor method-of-lines RK4 refinement. Conservation, electrical/gear/hydraulic/clutch composition, exact portable replay, actual MCP and allocation-free transactions have separate checks. Detailed numerical results are in validation evidence.

[VALIDATION.md](VALIDATION.md)

## Validity and remaining work

Parameters remain `unverified`. This is an equilibrium pure pseudo-fluid with a compressible zero-thermal-expansion liquid and ideal vapor, a mixed chamber and rigid duct cells. Enforce declared temperature and pressure bounds. The chamber has no resolved bulk kinetic energy or automatic check valve. Finite-rate nucleation, phase slip, real-fluid critical behavior, measured calibration and actual Unity acceptance remain open.

Add compatible compressible directional-valve and needle/nozzle boundaries with explicit enthalpy, kinetic energy and phase assumptions. Then continue phase relaxation/slip, ignition, intake/exhaust, mechanical losses and coordinated ECU/TCU control. Measured calibration and actual Unity acceptance remain separate.

[Modelica: swept volume and pressure work](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [NIST: consistent thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html)
