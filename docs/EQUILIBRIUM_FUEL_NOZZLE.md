# Equilibrium fuel nozzle transit

**English** · [简体中文](EQUILIBRIUM_FUEL_NOZZLE.zh-CN.md) · [Français](EQUILIBRIUM_FUEL_NOZZLE.fr.md) · [Русский](EQUILIBRIUM_FUEL_NOZZLE.ru.md) · [日本語](EQUILIBRIUM_FUEL_NOZZLE.ja.md) · [한국어](EQUILIBRIUM_FUEL_NOZZLE.ko.md) · [Deutsch](EQUILIBRIUM_FUEL_NOZZLE.de.md) · [Español](EQUILIBRIUM_FUEL_NOZZLE.es.md) · [Italiano](EQUILIBRIUM_FUEL_NOZZLE.it.md) · [Português](EQUILIBRIUM_FUEL_NOZZLE.pt-BR.md)

## Physical model

Set `compressible_fuel_nozzle` `phase_mode` to `equilibrium` for instantaneous homogeneous phase equilibrium during transit. Liquid and vapor share pressure, temperature and velocity, and vapor quality changes along the isentrope. The same compressible liquid cold potential and ideal-vapor reference supply both nozzle and finite reservoirs. The `frozen` mode remains a separate supported assumption.

Pressure and specific entropy determine the stable liquid, vapor or coexistence state. The closure uses chemical-potential equality and the existing temperature/pressure bounds. Inclusive temperature endpoints permit only binary64 rounding neighbourhoods. Area, wall heat routing, opening commands and physical needles follow the common nozzle contract. Jet energy describes ideal complete expansion to receiver pressure; stationary jet impulse adds no axial needle force or mechanical work.

## Global flux enclosure

Positive equilibrium sound-speed squared gives increasing density along an isentrope. Enthalpy drop decreases with pressure. These monotone factors bound flux on each pressure interval, including phase boundaries and multiple extrema. Branch-and-bound returns a sampled lower rate and remaining upper bound within 1e-4 relative width. There are at most 512 stack intervals and 8192 splits. Caloric endpoint differences use roundoff allowances and monotone pressure-integral bounds, with up to 256 Riemann panels and 80 closure iterations. Unresolved cases reject the batch. A smooth interior maximum is sonic; a phase-boundary maximum can be nonsmooth.

```text
g_liquid(T,p) = g_vapor(T,p)
s(p,rho,T,x) = s_source
D(p) = h_source - h(p,s_source) = integral(p,p_source) v(q,s_source) dq
G(p) = rho(p,s_source) sqrt(2 D(p))
drho/dp at fixed s = 1/c_equilibrium^2 > 0
G_upper([p_low,p_high]) = rho(p_high) sqrt(2 D_upper(p_low))
G_lower = max(sampled rho(p) sqrt(2 D_lower(p)))
flux_relative_uncertainty = G_upper/G_lower - 1 <= 1e-4
mass_flow_local = opening Cd A G_lower
velocity_throat = c_equilibrium  (smooth interior maximum only)
```

Conservative first-order substeps transfer the same mass and enthalpy between reservoirs. Jet kinetic energy is divided between receiver and wall. Accepted substeps check both material states and total pair entropy. Each interval permits 4096 substeps with at most 64 halvings per candidate; the initial transfer bound is 0.1 times the smaller reservoir mass. Pressure closure allows 128 times binary64 epsilon times the largest bulk modulus or port pressure; reported pressures are not clamped. A failed interval rolls back the whole batch. Reduce the tick and inspect inventory, material bounds, needle travel and valve area.

## Use and evidence

Run `equilibrium-flashing-nozzle` or `equilibrium-pump-needle`. The additional `flux_relative_uncertainty` channel describes the last accepted local constitutive flux enclosure. It does not bound mean flow, delivered mass, first-order time integration or calibration error. Checks use independent saturation/entropy inversion, analytical gas and liquid choking, a separate dense global pressure search, energy and entropy identities, and analytic vapor-blowdown refinement. Complete inventories, shaft/duct/needle composition, rollback, cancellation, independent branches, allocations, portable assets and actual MCP are verified separately. Numerical values and preserved earlier boundaries are in validation evidence.

[VALIDATION.md](VALIDATION.md)

## Validity and next work

Parameters remain `unverified`. This equilibrium limit assumes one pure pseudo-fluid, ideal vapor, zero liquid thermal expansion and common phase velocity. It omits finite nucleation and relaxation times, phase slip, spray, carrier gas and fired receiver coupling. The numerical enclosure uses declared binary64 allowances; it is not a formal directed-rounding interval proof. Selected throat classification near critical transitions must be read with the flux envelope. Measured hardware validity and actual Unity acceptance remain open.

Add resolved finite-rate nozzle residence with material evidence, then resolved spray and compatible fired receivers. Continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control. Measured calibration and actual Unity acceptance remain separate.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
