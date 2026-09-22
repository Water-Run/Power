# Crank-angle valve timing

An optional `valve_timing` on a `gas_orifice` multiplies its opening by a periodic
crank-angle envelope. It supports fixed gas vessels and moving cylinders through the
same Core, JSON, CLI, MCP and portable-asset definitions. The envelope follows actual
crank position during acceleration, stopping and reversal. It represents effective
flow area; it does not model cam contact, physical valve lift, spring forces or friction.

## Contract and phase

```json
"valve_timing": {
  "crank_node": 1,
  "cycle_angle": { "value": 720, "unit": "deg" },
  "open_angle": { "value": 520, "unit": "deg" },
  "duration_angle": { "value": 200, "unit": "deg" }
}
```

`crank_node` must identify a rotational node. All three angles require explicit `deg`
or `rad` units. Cycle angle is exactly 360 or 720 degrees; duration lies between 1e-6
radians and the cycle angle. Opening angle is finite and normalized modulo the cycle.
Negative angles and a lobe crossing the cycle boundary are supported. Multiple valves
can reference one crank, including overlapping lobes.

Phase is relative to the referenced crank's angle, including its initial position.
A cylinder's geometry phase is **not** added automatically: the author must select the
appropriate valve opening angle for each cylinder. A 720-degree cycle distinguishes
successive crank revolutions. There is no implicit four-stroke phase inferred from
piston position, speed or elapsed time.

For cycle `C`, opening angle `a`, duration `D`, peak opening `u` and crank angle `theta`:

```text
s = modulo(theta - a, C)       // in [0, C)
opening = u sin²(pi s / D)     // 0 < s < D
opening = 0                   // otherwise, including both boundaries
A_effective = A_orifice * opening
```

This profile and its first derivative are continuous at the lobe boundaries. Reverse
rotation retraces it; a stopped crank holds its current opening and can continue flowing.
Opening does not select flow direction: the existing pressure-driven, bidirectional
orifice law still applies. Its discharge coefficient remains a separate multiplier.

For a timed restriction, `initial_input` and its optional input channel specify **peak
opening**, a fraction in [0, 1]. The channel quantity is `peak_opening`; zero disables
the lobe. The output quantity `effective_opening` uses `Field.Opening` (JSON KPI field
`opening`) and reports the actual fraction. `mass_flow` is evaluated with that fraction.
Untimed restrictions keep their existing input quantity and semantics. Scheduled and
interactive peak changes retain atomic input validation and revision checks.

## Integration and recovery

Timed models use symmetric gas half-step / crank-work full-step / gas half-step
integration, including a fixed vessel driven by an independent crank. The first half
uses the opening crank angle, and the second half the resulting angle. The gas solver
resolves its own mass/energy dynamics within each half-step. It does not continuously
locate valve edges or adapt the outer mechanical tick.

For every active lobe, the outer tick must satisfy:

```text
limit = min(0.25 rad, D / 8)
max(abs(theta_next - theta_old), dt * max(abs(omega_old), abs(omega_next))) <= limit
8 * binary64_epsilon * max(abs(theta_old), abs(theta_next)) <= limit
```

The endpoint-speed bound also covers a reversal whose net angle change is small.
The precision bound prevents an unwrapped angle from losing the resolution needed for
its lobe. An under-resolved tick fails even when both endpoints are closed; it cannot
silently skip an entire narrow opening. A disabled peak does not require lobe resolution.
These are numerical guards, not an error tolerance or a guarantee for arbitrary dynamics.

A failure returns `NumericalFailure` / `numerical_failure` and commits no part of the
caller batch, including earlier ticks and scheduled inputs. Reduce `step_ns` and recreate
the model/session; make sure events still align to the new tick. For very large initial
angles choose an equivalent angle consistent with every connected component's phase.
The existing cylinder and gas limits also apply. No hidden mutable cam state is added;
crank position and peak inputs already participate in snapshots, hashes and forks.

The smooth, wall-free reference cases show second-order convergence. Wall temperatures
remain fixed over the outer tick, so wall-coupled models remain first order. Existing
near-equilibrium flow limiting can reduce local order. Conservation and replay do not
by themselves demonstrate temporal accuracy.

## Evidence and compatibility

The checks include analytic envelope values, explicit cycles, phase wrap, acceleration,
reversal, a stationary crank, disabled peaks, unresolved whole-lobe crossings, cancellation,
full-batch rollback, independent forks and allocation-free stepping/snapshots.

A fixed-vessel blowdown test independently integrates the sin-squared exposure and uses
the closed-form adiabatic choked-discharge solution. Both forward and reverse cases
converge under tick refinement. A separate moving-cylinder test integrates mass, internal
energy, crank motion and an angle-dependent restriction with independently written RK4
ODEs, crossing both lobe boundaries. Reference refinement establishes its own accuracy
before comparing Core results. See [validation](VALIDATION.md) for thresholds.

Only timed models add fingerprint tag 6, the target component/crank IDs and normalized
profile parameters. Untimed models keep their prior fingerprints and stepping. Asset v5
adds bounded timing records and retains v1–v4 readers; authentic earlier fixtures check
fingerprints and upgraded replay. See [asset layout](ASSET_FORMAT.md).

The [crank-timed cylinder laboratory](../assets/labs/crank-timed-cylinder.power.json)
motors a synthetic chamber through repeated 720-degree cycles with intake and exhaust
profiles. Two scheduled torque changes vary crank speed; valve timing itself has no
time schedule. JSON/CLI, MCP and asset playback match at all 63 report boundaries.
The build exports `CrankTimedCylinder.powerasset`; Studio animates schematic markers
from the effective-opening channels. Editor/Play/IL2CPP execution remains pending.

Cantera's [internal-combustion reactor example](https://cantera.org/stable/examples/python/reactors/ic_engine.html)
is a conceptual reference for crank-angle port control. Its fixed-speed assumptions,
valve law and example parameters are not adopted as calibration or verification of
Power!'s solver. This implementation uses the project's conservative crank coupling
and bidirectional nozzle law. Separate [premixed combustion](PREMIXED_COMBUSTION.md)
now adds fuel and chemical-energy accounting. All sample parameters remain `unverified`;
complete engine behavior and measured vehicle calibration remain open.
