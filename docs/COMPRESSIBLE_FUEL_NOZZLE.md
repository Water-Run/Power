# Frozen compressible fuel nozzles

**English** · [简体中文](COMPRESSIBLE_FUEL_NOZZLE.zh-CN.md) · [Français](COMPRESSIBLE_FUEL_NOZZLE.fr.md) · [Русский](COMPRESSIBLE_FUEL_NOZZLE.ru.md) · [日本語](COMPRESSIBLE_FUEL_NOZZLE.ja.md) · [한국어](COMPRESSIBLE_FUEL_NOZZLE.ko.md) · [Deutsch](COMPRESSIBLE_FUEL_NOZZLE.de.md) · [Español](COMPRESSIBLE_FUEL_NOZZLE.es.md) · [Italiano](COMPRESSIBLE_FUEL_NOZZLE.it.md) · [Português](COMPRESSIBLE_FUEL_NOZZLE.pt-BR.md)

## Physical contract

A `compressible_fuel_nozzle` carries one-way flow between finite `compressible_fuel` reservoirs, including shaft-driven chambers. Liquid compression energy and ideal vapor share the reservoir material. Declare `phase_mode` as `frozen`: mass quality stays constant during transit while bulk reservoirs re-equilibrate.

Declare area, effective-area discharge coefficient and `wall_kinetic_fraction`, with a finite thermal wall. `initial_input` is opening in [0,1]; an optional input channel changes it. Optional `needle_node`, `closed_position` and `full_open_position` multiply the command by actual translational needle opening. Both ports have zero bulk velocity, matching material and heating value, and cannot be internal duct cells. Reverse pressure closes this ideal zero-cracking-pressure boundary.

## Energy and choking

The frozen phases share pressure, temperature and velocity; phase mass transfer is suppressed. Their entropy relation and liquid cold energy give the isentropic enthalpy drop. The unique mass-flux maximum uses frozen sound speed, which differs from equilibrium sound speed. The coefficient scales effective area. At choking, reported exit energy describes an ideal jet fully expanded to receiver pressure. Donor stagnation enthalpy supplies receiver energy and explicit finite-wall jet heat once. Jet impulse measures stationary thermalization, not total housing thrust or axial needle force.

```text
cp_f = (1-x) cl + x (cv+R); alpha = x R/cp_f
T(p) = T0 (p/p0)^alpha
v(p) = (1-x)/[rho0 (1+p/K)] + x R T(p)/p
Delta_s = cp_f ln(T/T0) - x R ln(p/p0) = 0
D(p) = h0-h(p)
     = cp_f T0 [1-(p/p0)^alpha] + (1-x) K/rho0 ln[(K+p0)/(K+p)]
G(p) = sqrt(2 D(p))/v(p)
c_frozen^2 = -v(p)^2 / (dv/dp at fixed s,x)
F(p) = v(p)^2 + 2 D(p) v'(p); F'(p) = 2 D(p) v''(p) > 0
G_throat = max(G(p), p_receiver <= p <= p0)
mass_flow = opening Cd A G_throat
Delta_E_A = -Delta_m h0
Delta_E_B = Delta_m h0 - xi Delta_m D(p_receiver)
Delta_E_wall = xi Delta_m D(p_receiver)
```

Conservative first-order substeps transfer the same mass and enthalpy between reservoirs. Jet kinetic energy is divided between receiver and wall. Accepted substeps check both material states and total pair entropy. Each interval permits 4096 substeps with at most 64 halvings per candidate; the initial transfer bound is 0.1 times the smaller reservoir mass. Pressure closure allows 128 times binary64 epsilon times the largest bulk modulus or port pressure; reported pressures are not clamped. A failed interval rolls back the whole batch. Reduce the tick and inspect inventory, material bounds, needle travel and valve area.

## Use and evidence

Run `compressible-fuel-nozzle` for finite vapor discharge or `compressible-pump-needle` for duct supply, shaft pumping, one-way inlet and solenoid needle delivery. Read cumulative mass, enthalpy, jet kinetic energy, wall heat, impulse and pair entropy. Mean flow and force cover the complete tick; throat, exit and quality channels retain the last accepted transit. Checks compare exact liquid work, analytic gas/liquid choking, independent pressure quadrature, frozen entropy and sonic derivatives, a dense flux scan and analytic choked blowdown refinement. Conservation, actual needle motion, replay, branches, rollback, cancellation, allocations and portable/MCP operation are checked separately.

[VALIDATION.md](VALIDATION.md)

## Validity and next work

Parameters remain `unverified`. This is a pure pseudo-fluid with ideal vapor and a compressible zero-thermal-expansion liquid. Stay within declared pressure and temperature bounds, including ideal jet temperature. Frozen transit can retain superheated liquid; the model does not predict a measured nucleation or metastability limit. Finite-rate relaxation, phase slip, resolved spray, noncondensable carrier gas, fired receiver coupling, measured maps and actual Unity acceptance remain open. Conservation and replay do not establish hardware validity.

Add finite-rate phase relaxation with explicit residence and material evidence, then resolved spray and compatible fired receiver coupling. Continue ignition, intake/exhaust, mechanical losses, transmission actuation and coordinated ECU/TCU control. Measured calibration and actual Unity acceptance remain separate.

[NASA: injector flow and finite phase-transfer times](https://ntrs.nasa.gov/api/citations/20190001326/downloads/20190001326.pdf) · [NIST: thermodynamic derivatives](https://pages.nist.gov/teqp-docs/en/latest/derivs/derivs.html) · [Lund: two-phase relaxation hierarchy](https://www.sintef.no/globalassets/project/co2-dynamics/publications/lund_two-phase_relaxation_hierarchy.pdf)
