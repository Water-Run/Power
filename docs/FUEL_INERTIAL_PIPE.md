# Finite liquid fuel line momentum

**English** · [简体中文](FUEL_INERTIAL_PIPE.zh-CN.md) · [Français](FUEL_INERTIAL_PIPE.fr.md) · [Русский](FUEL_INERTIAL_PIPE.ru.md) · [日本語](FUEL_INERTIAL_PIPE.ja.md) · [한국어](FUEL_INERTIAL_PIPE.ko.md) · [Deutsch](FUEL_INERTIAL_PIPE.de.md) · [Español](FUEL_INERTIAL_PIPE.es.md) · [Italiano](FUEL_INERTIAL_PIPE.it.md) · [Português](FUEL_INERTIAL_PIPE.pt-BR.md)

## Physical contract

A `fuel_inertial_pipe` connects two finite fuel nodes and a finite thermal wall. Its rigid horizontal line owns liquid mass, caloric energy and kinetic energy, in addition to the inventories of both vessels. The line has constant density and a well-mixed variable temperature. Both ports and the entire line must remain liquid; the existing quasi-static links retain their separate two-phase behavior.

Declare length, area, linear and quadratic resistance, initial flow, initial temperature and `fluid_heat_fraction`. Use `pa_s_m3` and `pa_s2_m6` for resistance. Material properties and density must match at both fuel ports. There is no direct opening input: closing another valve does not erase line momentum. The line's material and chemical reference is inherited from the ports.

## Energy and ports

Donor enthalpy leaves the upstream vessel. The line mixes incoming sensible energy with its own liquid inventory, and its mixed enthalpy enters the downstream vessel at downstream pressure. Pressure work changes kinetic energy and supplies friction heat once. The declared fraction of friction heat warms the line; the remainder warms its finite wall. Direction reversal uses the same balances with exchanged donor/receiver roles.

```text
M_line = rho L A; I = rho L / A
E_line = U_line + I Q^2/2 + M_line LHV
D = R Q^2 + K abs(Q)^3
dM_A/dt = -rho Q; dM_B/dt = rho Q
I dQ/dt = p_A - p_B - R Q - K Q abs(Q)
dU_line/dt = rho abs(Q) (u_donor-u_line) + xi D
dE_kinetic/dt = (p_A-p_B) Q - D
dE_wall/dt = (1-xi) D
h_port = u + p/rho
```

The momentum equation uses midpoint flow and accepted end pressures. Port energy and line mixing use backward Euler, so the complete coupling converges at first order. Even without friction, endpoint pressure discretization converts part of the pressure/kinetic oscillation into numerical caloric heating. That contribution is not reported as physical friction. Pair entropy includes mixing and time discretization. Zero-flow events preserve separate forward and reverse transfer histories.

## Use and evidence

Run `inertial-fuel-line` for reversal and thermal mixing, or `inertial-pump-fed-cylinder` for the inlet line, pump, physical solenoid needle and prescribed firing. Read `volume_flow`, `mass_flow`, `forward_volume`, `reverse_volume`, `kinetic_energy`, `energy_at_a`, `energy_at_b`, thermal inventories and full ledgers. Volume flow is instantaneous; mass flow is the last tick mean. The line adds its own mass and chemical inventory to the model.

An independent finite-compliance harmonic solution and a separate nonlinear momentum/temperature RK4 integration check accuracy and first-order refinement. Checks cover friction routing, pair entropy, physical reversal, complete energy/mass/fuel ledgers, speculative clutch intervals, allocation-free stepping, rollback, cancellation and independent branches. Portable assets and actual MCP check every sampled boundary. This does not establish calibrated hardware or actual Unity acceptance.

## Validity and next work

Parameters remain `unverified`. Use the declared material temperature/pressure ranges and pressure-corrected liquid stability. A failed interval rolls back completely; reduce the tick and inspect source depletion, pressure and phase limits. The model omits variable-density two-phase momentum, cavitation, acoustic waves, gravity, moving pipe geometry and resolved thermal fronts. Prescribed combustion does not establish detailed chemical-equilibrium or reaction-entropy accuracy.

Connect compressible ducts to compatible shaft-driven chambers and needle/nozzle boundaries, with explicit pressure work and phase assumptions. Then address phase relaxation/slip, ignition, intake/exhaust, mechanical losses and coordinated ECU/TCU control. Measured calibration and actual Unity acceptance remain separate. [Compressible equilibrium fuel ducts](EQUILIBRIUM_FUEL_DUCT.md)

[Modelica: momentum and energy balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html) · [Modelica: dynamic pipe boundaries](https://doc.modelica.org/om/Modelica.Fluid.Pipes.DynamicPipe.html) · [VALIDATION.md](VALIDATION.md)
