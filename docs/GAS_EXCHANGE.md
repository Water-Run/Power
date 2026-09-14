# Gas exchange primitives

This document records the first slice of the gas-exchange increment described in
[the engine resume notes](NEXT_ENGINE_STEP.md): the flow and control-volume physics, validated on
their own, before any of it is wired into the compiled model graph.

Everything here lives in `src/Power.Core/GasExchange.cs` and is covered by
`tests/Power.Tests/GasChecks.cs`. **No new node kind, component kind, channel, schema field or asset
version exists yet.** A model document cannot yet contain a finite gas volume, and the CLI, MCP and
Unity surfaces are unchanged. The sealed adiabatic cylinder remains the only gas component in the
compiled model, and it remains the analytic benchmark the notes ask to retain.

## What is implemented

| Type | Responsibility |
|---|---|
| `IdealGas` | Calorically perfect gas of one fixed composition: `R`, `gamma`, `cv`, `cp`, the critical pressure ratio, and the two precomputed nozzle mass-flux coefficients. |
| `GasVolumeState` | A finite volume tracked by **mass and internal energy as independent states**, with derived density, temperature, pressure and specific enthalpy. |
| `Orifice` | Ideal compressible flow through a restriction with a discharge coefficient and a dimensionless opening fraction in `[0,1]`, signed in both directions, choked and subcritical. |

`GasVolumeState` deliberately replaces the sealed cylinder's angle-and-initial-entropy derivation.
Because mass and internal energy are carried independently, the same state can absorb transported
mass, transported enthalpy and wall heat without assuming an isentropic history.

## Equations

Static pressure uses `p = (gamma - 1) U / V`, which is exact for a calorically perfect gas and avoids
a separate temperature round trip. Temperature is `T = U / (m cv)`.

Mass flow follows the standard isentropic nozzle relations. With `A` the effective area
(geometric area x discharge coefficient x opening), upstream static state `p_u, T_u`, and pressure
ratio `pr = p_d / p_u`:

- choked, `pr <= (2/(gamma+1))^(gamma/(gamma-1))`:
  `mdot = A (p_u / sqrt(T_u)) sqrt(gamma/R) (2/(gamma+1))^((gamma+1)/(2(gamma-1)))`
- subcritical: `mdot = A (p_u / sqrt(T_u)) sqrt(2 gamma / (R (gamma-1)) (pr^(2/gamma) - pr^((gamma+1)/gamma)))`

The stream carries the upstream enthalpy, `hdot = mdot cp T_u`, so the direction of flow decides
which endpoint's temperature is transported. A reservoir is passed as an ordinary `(p, T)` pair, so
no dummy volume is required for a fixed boundary.

Reference for the two branches: [NASA mass-flow choking](https://www.grc.nasa.gov/www/k-12/BGP/mflchk.html).
It is a reference for the relations, not a validation of this implementation.

## Numerical notes

The subcritical flow function is evaluated as `pr^(2/gamma) * (1 - pr^((gamma-1)/gamma))` with the
second factor computed through `expm1`. The textbook difference of two nearly equal powers cancels
catastrophically as `pr` approaches one: at `pr = 1 - 1e-12` it retains roughly four digits, while
the `expm1` form is accurate to the precision of the stored ratio. `Numeric.Expm1` and `Numeric.Log1p`
are now shared with the sealed-cylinder physics rather than duplicated.

Two limits are inherent to the model rather than to the implementation, and a solver that adopts it
must handle both:

- The subcritical branch has an **infinite derivative at unit pressure ratio**. A Newton step must
  not be taken straight through that point; bracket or damp it.
- A ratio close to one cannot be represented usefully in binary64. At `pr = 1 - 1e-15` only about one
  digit of the offset survives, no matter how the function is written.

Upstream stagnation and static conditions are treated as equal. This is the usual quasi-steady
control-volume approximation and is **not** valid for high-Mach chamber flow.

## Evidence

`tests/Power.Tests/GasChecks.cs` adds six checks, each written against an independent closed form
rather than a recorded output of this code:

1. **Properties and choking continuity** — `cv`, `cp` and the critical ratio against their
   definitions for `gamma` in `{1.1, 1.3, 1.4, 5/3}`; the subcritical branch reaching the choked
   coefficient exactly at the critical ratio; monotone decay of the flow function to zero, checked
   against the naive form where that form is trustworthy and against the leading-order expansion
   where it is not.
2. **Nozzle flow** — 54 combinations of upstream pressure, upstream temperature and pressure ratio
   against the NASA relations written out in full, including that choked flow is independent of the
   downstream pressure and that enthalpy is carried at the upstream temperature.
3. **Contracts** — exact antisymmetry under swapping the endpoints, zero flow at a closed orifice and
   at equal pressures, linearity in the opening fraction, and rejection of non-finite or
   non-positive states, openings outside `[0,1]`, and invalid gas or orifice parameters.
4. **Adiabatic vessel blowdown** — RK4 integration of a 2 L vessel from 20 bar and 900 K against the
   analytic isentropic solution `rho(t) = (rho0^-k + k C t / V)^(-1/k)`, `k = (gamma-1)/2`, matched to
   1e-9 relative in density and temperature and 1e-8 in pressure, with a refinement check.
5. **Reservoir filling** — charging a 0.5 L vessel from a 6 bar, 320 K reservoir: the exact identity
   `dU = cp T_supply dm` while flow is one-directional, and the evacuated-vessel limit
   `T -> gamma T_supply`, checked from two different starting pressures.
6. **Closed two-volume network** — 2 s of exchange between a hot 1.5 L volume and a cold 0.4 L volume:
   total mass conserved to 1e-14 relative and total internal energy to 1e-12 relative, pressures
   equalising, and the equilibrium confirmed to be mechanical rather than the fully mixed temperature.

## What is still open

The contracts in [the resume notes](NEXT_ENGINE_STEP.md) that this slice does **not** deliver:
gas endpoints referencing component IDs, gas-to-thermal heat links, a `Domain.Gas` node with mass and
internal-energy states in the compiled model, external mass and energy ledgers, mass-residual and
mass-flow channels, the bounded pairwise implicit transfer solve, coupling gas pressure work through
the existing crank solve, schema and asset extensions, capability discovery, and the Unity views.
The proposed split method with backward-Euler flow is still unvalidated and has not been adopted.

Restricting a connected set of volumes to one gas constant and one gamma is still the intended rule;
`IdealGas` carries a single composition and no mixing is implemented.
