# Managed dry-clutch physics

`Power.Core` provides an immutable `DryClutch` friction law and a `ClutchPair` reference
integrator for two inertias under constant external torques and engagement. Both compile
for `net10.0` and `netstandard2.1` without third-party dependencies.

These primitives provide an independent reference for the now-integrated
[clutch graph component](CLUTCH_NETWORK.md). The graph couples shafts, motors and
cylinders, supports multiple clutches and thermal routing, and preserves whole-batch
rollback through internal events. JSON, CLI/MCP, asset v8 and Studio consume that graph
definition. The standalone pair documented here remains a constant-load reference;
it does not itself step a compiled network.

## Friction contract

All capacities and reactions are expressed at port A. The signed ratio `r` uses the
same power convention as the existing shaft component:

```text
g       = omega_A - r * omega_B                  [rad/s]
tau_B   = -r * tau_A                            [Nm]
P_heat  = -tau_A * g                            [W]
C_s     = engagement * static_capacity          [Nm]
C_k     = engagement * sliding_capacity         [Nm]
```

The engagement fraction lies in `[0,1]`. Static capacity is nonnegative and no smaller
than sliding capacity. Both may be zero. A zero effective static capacity disengages
the clutch. There is no inferred clamp pressure, friction coefficient, plate geometry,
temperature fade, wear, drag or actuator delay.

For nonzero slip, `tau_A = -sign(g) * C_k`. At exactly zero slip the integrating system
must supply the torque required to keep relative acceleration zero. If its magnitude
is at most `C_s`, the clutch locks at that reaction and produces no friction heat.
Otherwise it starts sliding in the direction of the unbalanced load, using `C_k`.
Equality at the static limit remains locked. The law has no velocity deadband and
does not silently turn a small relative velocity into a sticking constraint.

This idealized distinction between kinetic friction and a constrained static reaction
follows the mechanics described by the primary references:
[Modelica Clutch](https://doc.modelica.org/om/Modelica.Mechanics.Rotational.Components.Clutch.html)
and [MathWorks Fundamental Friction Clutch](https://www.mathworks.com/help/sdl/ref/fundamentalfrictionclutch.html).
Power!'s implementation is independently written and uses explicit torque capacities;
it does not reproduce either implementation or claim their broader constitutive models.

`ClutchMode` distinguishes `Disengaged`, `Locked`, `SlippingPositive` and
`SlippingNegative`. A mode at zero velocity can be a departing sliding state when the
external load exceeds static capacity. `HeatFlowWatts` is instantaneous; its value at
such a zero-speed departure is zero even though subsequent heat is positive.

## Exact constant-load pair

For two positive inertias `J_A`, `J_B`, and constant external torques `T_A`, `T_B`:

```text
J_A * domega_A/dt = T_A + tau_A
J_B * domega_B/dt = T_B - r * tau_A
D                 = 1/J_A + r*r/J_B
b                 = T_A/J_A - r*T_B/J_B
required_tau_A    = -b/D
dg/dt             = b + D*tau_A
```

Each sliding phase has constant acceleration. If its relative velocity reaches zero
within the requested interval, the solver advances exactly to `t_zero = -g / (dg/dt)`
and evaluates the static reaction. It then integrates the remainder either locked or
sliding in the opposite direction. Constant forcing allows at most one such arrival,
so the solve requires at most two phases, with no convergence loop or time subdivision.
An event exactly at the interval endpoint returns its right-hand reaction mode.

The locked trajectory obeys `omega_A = r*omega_B` with

```text
domega_B/dt = (r*T_A + T_B) / (r*r*J_A + J_B).
```

At a computed arrival, a momentum-preserving projection removes binary64 event-rounding
residue. It uses bounded inertial weights rather than forming large inertia-weighted
sums. Once locked, the speed constraint is constructed explicitly. This is a rounding
correction at a resolved event, not an inelastic instantaneous engagement of finite slip.
The angular advances integrate each constant-acceleration phase. External work is
`T_A*delta_theta_A + T_B*delta_theta_B`; friction heat is the integral of `-tau_A*g`.
The result includes the signed torque impulse at A and the independently checkable
kinetic-energy change. The energy residual is `external_work - heat - delta_kinetic`.

The pair supports either sign of nonzero finite `r`. Its generalized momentum
`r*J_A*omega_A + J_B*omega_B` changes only through `r*T_A + T_B`. Ordinary angular
momentum conservation applies when `r = 1`; a ratio represents an ideal mechanical
transformer whose support can react torque. For a ground brake, construct
`ClutchPair.Brake(J, friction)`. Port B then has fixed zero speed and external torque,
and `r = 1`. Infinity is not used as an inertia sentinel.

## API and failure behavior

```csharp
var pair = new ClutchPair(0.2, 0.8, new DryClutch(20, 10));
var status = pair.Advance(new ClutchPairState(100, 0),
    externalTorqueANewtonMeters: 0,
    externalTorqueBNewtonMeters: 0,
    engagement: 1,
    durationSeconds: 4,
    out var step);
```

The example reaches 20 rad/s at both ports after 1.6 s and generates 800 J of heat.
Its angular advances over four seconds are 144 rad and 64 rad. All numbers are
synthetic, with no vehicle-calibration claim.

The two classes are immutable. `ClutchPairState` and `ClutchPairStep` are value types.
`Advance` neither mutates a caller's state nor allocates memory. Independent callers
can share the same pair. There is no retained phase history or global simulation clock;
the supplied velocities and new constant loads determine the next interval.

| Status | Meaning and recovery |
|---|---|
| `Ok` | A complete finite local result is available; evaluate conservation and model suitability separately |
| `InvalidDuration` | Supply a finite, strictly positive interval in seconds |
| `InvalidEngagement` | Supply a finite fraction in `[0,1]` |
| `InvalidState` | Supply finite velocities; a ground brake requires speed B equal to zero |
| `InvalidTorque` | Supply finite external torques; a ground brake requires torque B equal to zero |
| `NumericalFailure` | Derived motion, event time or energy exceeds the supported binary64 range; inspect units/scales and shorten or reformulate the interval |

On any rejection the output is `default`; there is no partially published state.
Invalid immutable parameters throw `ArgumentException` or its subclasses at
construction. `DryClutch.Evaluate` likewise rejects invalid inputs or overflowing
instantaneous heat. An event time that underflows to zero fails instead of silently
discarding finite relative kinetic energy. Physical results remain subject to floating
point rounding; finite inputs alone do not guarantee representable derived quantities.

`ZeroSlipTimeSeconds` is the first engaged arrival at zero relative speed, or zero
when the interval starts there. It is null when there is no such arrival, including
disengaged motion. It does not imply sticking: a large external load can cause immediate
reversal. `SlippingDurationSeconds` includes departing sliding phases; disengaged
motion is excluded. `EndReaction` is instantaneous at the final state, while heat,
work, impulse and angle advances are integrated over the complete interval.

The local seconds parameter does not replace `Simulation`'s fixed, bounded nanosecond
clock. The graph integration keeps exact external tick/event boundaries, state hashes,
fork independence, cancellation and complete multi-tick rollback.

## Evidence and limits

[ClutchChecks.cs](../tests/Power.Tests/ClutchChecks.cs) runs the same ten groups against
both Core target assemblies:

- Closed-form two-inertia synchronization time, speed, angular advances, impulse,
  momentum and lost kinetic energy, with full and partial engagement.
- Exact static load sharing, inclusive breakaway threshold, a lower kinetic capacity,
  nonzero slip without a deadband, and a zero-kinetic-capacity static latch.
- Reversal within an interval and at its endpoint, plus ground braking, holding and
  departure under excessive load.
- Positive/negative ratios, generalized momentum and independently calculated energy
  changes. Two thousand deterministic combinations sweep inertia, ratio, velocity,
  external load, capacity and duration.
- Partition invariance through hybrid events under constant forcing. Midpoint sampling
  of changing sinusoidal loads converges against independent analytic velocity, angle
  and heat integrals. This proves that load-sampling example's second-order behavior;
  the graph has its own separate coupled and hybrid convergence checks.
- Invalid inputs, overflow, an unresolvable event, default output on failure, independent
  repeated evaluations and zero allocation over 10,000 successful intervals.

The pair returns heat as generated energy; the graph component routes it to a thermal
node or the external ledger. Neither API implements DCT/AT topology, gear selection,
a torque converter, hydraulic
actuators, ECU/TCU coordination, clutch-material identification or measured calibration.
Those boundaries remain in the [roadmap](ROADMAP.md).
