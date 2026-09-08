# Sealed-cylinder foundation

`sealed_cylinder` couples a rigid slider-crank to a rotational node. The cylinder contains a fixed mass of ideal gas with constant specific-heat ratio and no wall heat transfer. This is a compression/expansion benchmark, not a complete firing engine. Intake, exhaust, fuel, combustion, leakage, wall heat transfer, reciprocating inertia and control events remain separate implementation work. All current parameters are synthetic and `unverified`.

The initial pressure and temperature apply at the connected rotor's initial angle plus the cylinder phase. Changing that initial angle changes the trapped mass unless the pressure/temperature are adjusted consistently. Gas state is derived from crank position and the immutable initial entropy; it adds observable channels but no independent state variable. This reduction is valid only for the sealed adiabatic component.

## Geometry and gas state

Lengths compile to meters, pressures to pascals, and phase to radians. Input accepts `m`/`mm`, `pa`/`bar`, and `rad`/`deg`. Temperature is kelvin; the specific gas constant uses `j_kg_k`. Compression ratio and gamma are dimensionless. Bore and stroke must be positive, rod length must exceed half the stroke, compression ratio and gamma must exceed one, and initial gas pressure/temperature and gas constant must be positive. Back pressure may be zero.

With crank radius `r = stroke/2`, rod length `l`, piston area `A = π bore²/4`, and angle `θ` measured from top dead center:

```text
x(θ) = r (1 - cos θ) + l - sqrt(l² - r² sin² θ)
Vc   = A stroke / (compression_ratio - 1)
V(θ) = Vc + A x(θ)
```

The implementation uses an algebraically equivalent form to avoid cancellation near top dead center. This geometry follows the centered [slider-crank volume relation from Colorado State University](https://www.engr.colostate.edu/~allan/thermo/page2/page2.html).

Let `V0`, `P0` and `T0` describe the initial state. The reversible ideal-gas relations are:

```text
m = P0 V0 / (R T0)
P = P0 (V0/V)^gamma
T = T0 (V0/V)^(gamma-1)
U = P V / (gamma-1)
τ = (P - Pback) dV/dθ
```

The pressure/volume and temperature relations follow [NASA's isentropic compression derivation](https://www1.grc.nasa.gov/beginners-guide-to-aeronautics/isentrophic-compression/). At a compression ratio of 10 and gamma of 1.4, compression from bottom to top dead center multiplies pressure by about 25.119 and temperature by about 2.512. These are idealized ratios, not measured engine performance.

## Integration and energy

The existing electromechanical midpoint solve supplies a base solution and a precomputed response to torque at each distinct cylinder crank. A reduced nonlinear solve determines the angular increments of those cranks. Cylinders on the same crank contribute to one torque sum; coupled cranks solve together. No model-provider call, Unity object or third-party dependency participates in a physics tick.

Each cylinder uses a discrete work-consistent torque:

```text
τ_discrete Δθ = -(U_next - U_old) - Pback (V_next - V_old)
```

Stable analytic volume divided differences and small-argument logarithm/exponential evaluations handle small increments and dead-center crossings. The instantaneous `Torque` output remains `(P-Pback) dV/dθ`; it is distinct from the average torque used to integrate a finite tick.

The global stored-energy change includes gas internal-energy change. Back-pressure work is external source work, `-Pback ΔV`, so the ledger remains `source_work - heat_rejected - stored_energy_change`. Shaft and motor dissipation still enters the thermal network or rejected heat. Gas energy changes are evaluated directly to avoid subtracting large absolute energies when gamma approaches one.

Newton iteration is limited to 16 iterations, with at most 10 line-search trials per iteration. The linear predictor and accepted crank travel must stay within 0.25 radians per tick. Nonfinite values, excessive travel or failure to converge return `NumericalFailure`; the entire call, including scheduled inputs and ledger updates, rolls back. Reduce `step_ns` and recreate the model/session to retry with a smaller fixed tick. Acceptance is not a timestep-accuracy guarantee. Very large accumulated angles also lose binary64 angular resolution; long-duration accuracy needs its own evidence.

## Observable and portable experiment

Each cylinder exposes pressure (Pa), gas temperature (K), volume (m³), fixed mass (kg), absolute internal energy (J), piston displacement from top dead center (m), and torque at the crank (N·m). Channel IDs retain the existing object/field encoding. The model reports `sealed_adiabatic_gas` fidelity and `unverified` calibration.

Run `assets/labs/sealed-cylinder.power.json` through the CLI, or request `get_example_model({"name":"sealed-cylinder"})` over MCP. The sample uses a 100 µs tick, 0.2 s duration, two torque changes and 21 report boundaries. Declared KPIs apply to the final sample, as for existing experiments; the core conservation tests inspect repeated boundaries throughout their runs.

The same document exports to `SealedCylinder.powerasset`. Unity has a schematic piston view driven by the displacement channel; one scene unit represents a full stroke. Physical dimensions and outputs remain SI. Actual Editor, Play Mode and IL2CPP evidence is still pending.

`EngineChecks` runs analytic geometry and ideal-gas checks, two-second conservation runs, second-order step refinement, reverse rotation, small-step/dead-center cases, multiple cylinders on shared and coupled cranks, electrical/thermal coupling, atomic failure/recovery, cancellation, independent branches, asset compatibility and zero-allocation stepping. The same checks execute against both target assemblies on the .NET host.
