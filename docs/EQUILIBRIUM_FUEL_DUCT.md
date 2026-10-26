# Compressible equilibrium fuel ducts

**English** · [简体中文](EQUILIBRIUM_FUEL_DUCT.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_DUCT.fr.md) · [Русский](EQUILIBRIUM_FUEL_DUCT.ru.md) · [日本語](EQUILIBRIUM_FUEL_DUCT.ja.md) · [한국어](EQUILIBRIUM_FUEL_DUCT.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_DUCT.de.md) · [Español](EQUILIBRIUM_FUEL_DUCT.es.md) · [Italiano](EQUILIBRIUM_FUEL_DUCT.it.md) · [Português](EQUILIBRIUM_FUEL_DUCT.pt-BR.md)

## Model and material

An `equilibrium_fuel_duct` transports mass, momentum and total energy through explicit `compressible_fuel` cells. The liquid is compressible and the vapor is ideal; both phases share pressure, temperature and velocity. Local equilibrium permits condensation and flashing. Two finite stationary reservoirs and a finite thermal wall close the balances.

A `compressible_fuel_chamber` may change either mixed reservoir volume through shaft work. Its bulk velocity stays zero and internal duct cells stay rigid. Stationary port/housing impulse performs no work; piston pressure work is a separate mechanical exchange. [Shaft-driven compressible fuel chambers](COMPRESSIBLE_FUEL_CHAMBER.md)

The liquid has zero thermal expansion and constant heat capacity. `K` is its bulk modulus at zero pressure; the actual modulus is `K+p`. The cold compression energy, chemical potentials, entropy and equilibrium sound speed come from the same potential. `a` is the existing liquid energy offset, `p0(T)` the negligible-liquid-volume saturation curve, `x` vapor mass fraction and `Cv_eq` the derivative of mixture internal energy at fixed specific volume. `Tr`, `Pref` and `Lu_ref` are the material reference temperature, pressure and latent internal energy. All quantities use SI units.

```text
p_l = K (rho_l/rho0 - 1); y = p/K
v_l = 1/[rho0 (1+y)]
u_cold = (K/rho0) [ln(1+y) - y/(1+y)]
u_l = cl T + a + u_cold
h_l = cl T + a + (K/rho0) ln(1+y)
s_l = cl ln(T/Tr)
u_v = cv T; v_v = R T/p; h_v = (cv+R) T
s_v = (cv+R) ln(T/Tr) - R ln(p/Pref) + Lu_ref/Tr + R
R T ln[p_sat/p0(T)] = (K/rho0) ln(1+p_sat/K)
v = (1-x) v_l + x v_v; u = (1-x) u_l + x u_v
dp_sat/dT = (h_v-h_l)/[T(v_v-v_l)]
c_eq^2 = v^2 T (dp_sat/dT)^2 / Cv_eq
c_l^2 = K/rho0; c_v^2 = (1+R/cv) R T
```

## Transport and boundaries

Declare `liquid_bulk_modulus`, omit `compliance`, and explicitly supply `initial_velocity` on each node. Declare 1–24 ordered, distinct internal `cells`; each belongs to one duct. Cell length is volume divided by `area`. Reservoirs have zero velocity and cannot also be internal cells. All ports share the same material. `linear_resistance` and `quadratic_resistance` describe total line resistance and are distributed by cell volume; `fluid_heat_fraction` routes friction heat between fluid and wall. There is no direct opening input.

```text
U = [rho, rho velocity, rho (u + velocity^2/2)]
F = [rho velocity, rho velocity^2+p, (rho (u+velocity^2/2)+p) velocity]
F_Rusanov = (F_left+F_right)/2 - a_max (U_right-U_left)/2
a_max = max_ports(abs(velocity)) + sqrt(max(K/rho0, (1+R/cv) R Tmax))
```

The conservative Rusanov flux advances all faces from the same substep state. Its speed bound uses the largest absolute velocity plus the material acoustic bound. The CFL limit is 0.2 times minimum port volume divided by area and speed; each interval permits at most 4096 substeps. Transport and local exact drag decay are split at first order. Numerical diffusion contributes to total entropy change, separately from reported friction heat. Reservoir momentum is absorbed by stationary boundaries with zero mechanical work; incoming kinetic energy thermalizes in their finite inventory. Invalid states or an entropy decrease beyond roundoff reject the complete batch. Reduce the tick and inspect material bounds and cell resolution after rejection.

## Use and evidence

Run `equilibrium-fuel-condensing-duct` or `equilibrium-fuel-flashing-duct`. Every cell exposes density, velocity, phase fraction, sound speed, temperature and energy. Port mass/energy, wall impulse and full inventories are observable. Independent checks cover liquid compression work, ideal-vapor limits, chemical-potential equality, Clapeyron and Gibbs relations, entropy-inverted sound speed, mass-energy inversion, conservation and finite-wall friction. Cell-average acoustic errors decrease on 8, 16 and 24 cells. Exact replay, rollback, cancellation, branches, allocation-free stepping, portable assets and actual MCP are checked separately; detailed results are in validation evidence.

[VALIDATION.md](VALIDATION.md)

## Validity and next work

Parameters remain `unverified`. Stay inside the declared temperature and pressure range, with positive latent enthalpy, separated coexistence volumes and dilute vapor. This is a rigid constant-area horizontal duct with instantaneous homogeneous equilibrium. It omits finite-rate nucleation, phase slip, surface tension, gravity, pipe elasticity and real-fluid critical behavior. First-order numerical diffusion can smear phase and acoustic fronts; conservation and replay do not establish spatial accuracy or measured hardware validity. Actual Unity acceptance remains pending.

Add compatible compressible directional-valve and needle/nozzle boundaries with explicit enthalpy, kinetic energy and phase assumptions. Then continue phase relaxation/slip, ignition, intake/exhaust, mechanical losses and coordinated ECU/TCU control. Measured calibration and actual Unity acceptance remain separate.

[NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf) · [MFEM: Rusanov flux](https://docs.mfem.org/html/classmfem_1_1RusanovFlux.html) · [Modelica: conservation balances](https://doc.modelica.org/Modelica%203.2.3/Resources/helpWSM/Modelica/Modelica.Fluid.UsersGuide.ComponentDefinition.BalanceEquations.html)
