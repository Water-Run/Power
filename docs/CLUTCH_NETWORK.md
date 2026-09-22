# Coupled clutch simulation

The managed `clutch` component connects two rotational nodes or one rotor to ground.
It participates in the existing electromechanical/cylinder solve and routes generated
friction heat to a thermal node or the external heat ledger. JSON, CLI, MCP, asset v10
and Studio use the same definitions. This implements a transmission coupling element;
complete DCT/AT topology, pump/piston dynamics and ECU/TCU coordination remain separate work.
The [hydraulic network](HYDRAULIC_NETWORK.md) now drives a pressure-operated clutch variant.
The [mapped converter](CONVERTER_NETWORK.md) now shares this solve and uses a separate
parallel clutch for lockup.

The [dry-clutch physics contract](CLUTCH_PHYSICS.md) defines the Coulomb law and an exact,
independent two-inertia reference under constant loads. The graph solver below extends
that law to coupled networks. It does not freeze engine torque or motor current into a
one-way input to the clutch.

## Definition and channels

```json
{
  "id": 16,
  "kind": "clutch",
  "node_a": 1,
  "node_b": 4,
  "heat_node": 5,
  "input_channel": 104,
  "initial_input": { "value": 0, "unit": "fraction" },
  "parameters": {
    "static_capacity": { "value": 100, "unit": "nm" },
    "sliding_capacity": { "value": 30, "unit": "nm" },
    "ratio": 1
  }
}
```

`node_a` is a rotational node. `node_b` is a distinct rotational node, or omitted/zero
for a ground brake. `ratio` is finite and nonzero; ground requires one. Static capacity
is at least sliding capacity, and both are finite, nonnegative torques at port A.
`initial_input` is an explicit engagement fraction in `[0,1]`, scaling both capacities.
An optional input channel changes engagement on exact external tick boundaries. An
omitted/zero `heat_node` sends heat to the external rejection ledger; a supplied sink
must be a thermal node. Temperature does not alter these capacities.

The Core factory is `ComponentDefinition.Clutch(id, a, b, staticCapacity,
slidingCapacity, channel, engagement, ratio, heat)`. Its `Friction` descriptor contains
the two explicit torque quantities. Compiled models copy their parameters, sort by
stable ID and add fingerprint tag 8 only when clutches are present. Each clutch phase
counts against the existing 64-state limit. Earlier models retain their fingerprints
and replay hashes. The combined fidelity name is `hybrid_clutch_powertrain`, with
calibration still `unverified`.

| Field | Unit | Meaning |
|---|---|---|
| `slip_speed` | rad/s | Current `omega_A - ratio*omega_B` |
| `clutch_mode` | StateCode | Last accepted interval's phase: 0 disengaged, 1 locked, 2 positive slip, 3 negative slip |
| `torque` | Nm | Mean reaction at A over the last complete external tick |
| `heat_flow` | W | Mean generated friction power over that tick |
| `friction_heat` | J | Cumulative generated heat, regardless of destination |

Initial torque/power/heat are zero; the initial phase is inferred from engagement and
relative velocity, before solving a load reaction. A phase describes the solved interval,
so an arrival exactly on its endpoint may still show the approaching phase until the
next solve. A boundary input changes the input state immediately and does not rewrite
the preceding interval's output history. This also applies to input events at the end
of a `Step` call. Torque and power averages include every accepted internal interval.

## Coupled integration and events

For `g = omega_A - r*omega_B`, port torques are `tau_A = tau`, `tau_B = -r*tau`.
The mechanical power removed is `-tau*g`; this sign convention works with either sign
of `r`. Disengagement sets zero torque. Sliding uses the kinetic capacity opposing slip.
A locked clutch imposes zero midpoint relative velocity with a reaction bounded by the
static capacity. This gives zero ideal locked work without inserting an artificial
damper or a stiff penalty spring.

Each internal interval uses the existing implicit midpoint electromechanical equations
and conservative cylinder pressure-work solve. Clutch force responses are obtained from
the same coupled linear factors. A projected Gauss–Seidel solve determines bounded
static reactions while cylinder torque is recomputed for the current forces. Saturated
static constraints release when the required motion exceeds the velocity tolerance.
The active set is reconsidered if another constraint changes a departing direction.
Redundant clutch loops are permitted; their individual reactions can be nonunique.
Stable component order selects a deterministic allocation, while the tests check the
resulting motion, capacity limits, total momentum and energy.

If a sliding interval reverses its relative velocity, bounded bisection locates the
observed slip-zero boundary and replays the interval from a complete state copy. The next
interval either sticks or departs with the opposite kinetic reaction. Dynamics, thermal
factors and cylinder force responses are recomputed for each candidate duration; all
mutable factors belong to the individual simulation. The compiled model stays immutable.

The constraint tolerance is `2e-13 + 32*epsilon*(abs(omega_A)+abs(r*omega_B))` rad/s,
with `epsilon = 2.2204460492503131e-16`. Capture accepts roots within sixteen times that
tolerance. It does not project finite slip away or discard finite kinetic energy.
Tiny negative friction work within twice the interval's torque-times-velocity tolerance
is clamped to zero; larger negative work fails. Conservation checks include this
rounding effect. A residual within root tolerance cannot create a second spurious event.

The solve permits at most 32 internal intervals per outer tick, 56 root iterations,
256 constraint iterations per active set, and `2*clutch_count+2` active-set attempts.
Nonfinite factors, unconverged constraints, unresolved events, exhausted budgets or
existing cylinder/gas limits return `NumericalFailure`. Cancellation is checked during
the bounded constraint/root work. Reduce the external tick and inspect inertia/ratio
scales, redundant constraints and capacity schedules; do not interpret a failed call
as a partially completed engagement.

Generated heat is integrated as `-duration*tau*g_mid`, then added to the thermal solve
or external heat ledger. All accepted interval source work, gas transport, chemical
history, wall exchange and thermal rejection enter the existing energy accounting.
Clutch phase, mean outputs, cumulative heat and compensated heat sum are copied and
hashed with the physical state. A failed/cancelled multi-tick call restores the complete
starting state, including scheduled inputs, phase and heat. Forks share only compiled
model data. External time remains a bounded integer nanosecond count; internal event
durations do not introduce fractional externally visible ticks.

The nonlinear breakaway test uses interval-average torque demand. It does not locate
the exact continuous-time instant at which a changing static load first exceeds capacity.
Likewise, event bracketing concerns the discrete midpoint trajectory; a large tick can
miss fast physical oscillations whose endpoints conceal a reversal. Refine time around
transitions and compare outputs. Smooth electromechanical dynamics retain midpoint
accuracy, thermal/wall coupling remains first order, and no universal second-order
claim is made for all switching trajectories.

## Laboratory and evidence

The [fired-clutch laboratory](../assets/labs/fired-clutch.power.json) connects the premixed
cylinder to a separate inertial load and clutch thermal node. Six exact-tick events
apply partial/full engagement, load torque, release and re-engagement. Parameters are
synthetic. Over 0.6 seconds the current Linux report records:

| Quantity | Result |
|---|---|
| Engine/load final speed | 68.58488546 rad/s |
| Net external source work, including load and cylinder back pressure | -96.74607609 J |
| Generated clutch heat | 191.55570747 J |
| Clutch thermal-node final temperature | 300.95777854 K |
| Fuel heat released | 1,630.91064291 J |
| Final slip | 2.84e-14 rad/s, locked phase |
| Final energy residual | 1.79e-10 J |
| Model fingerprint / final state hash | `197be44884deee90` / `28bf5335d8e35cde` |

All 67 report boundaries match alternate batch sizes, portable playback and actual MCP
child-server replay. The report is `artifacts/reports/fired-clutch.json`. Request
`get_example_model` with `name: "fired-clutch"`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-clutch.power.json --output artifacts/reports/fired-clutch.json
```

Core checks compare engagement, braking and reversal against `ClutchPair`, including
signed ratios and both heat destinations. A locked RL motor matches a model with the
analytically combined inertia; a locked reacting gas cylinder likewise matches its
independent equivalent-inertia model, including pressure and fuel consumption.
A spring/brake oscillator matches analytic piecewise sinusoidal motion through three
reversals and a fourth turning point that captures, with refinement reducing error by more than 3.7x per halving.
Three-clutch loops exercise redundant constraints and simultaneous engagement.
Tests also cover complete rollback after a successful heating/capture prefix, cancellation,
exact scheduled replay, immutable ownership, branch independence and allocation-free
operation, including repeated internal reversal events.

Asset v10 round trips explicit capacities and all channels. Malformed counts, missing,
duplicate and wrong-kind records, wrong units, invalid limits and forged downgrades are
rejected. An authentic v6 fired-cylinder fixture retains its digest, fingerprint and
upgraded replay. Strict JSON and agent tests distinguish successful execution from
passing KPIs. See [VALIDATION.md](VALIDATION.md).

The build exports `FiredClutch.powerasset`. Studio prepares two schematic clutch plates,
phase colors and named phase output, alongside engagement controls and heat channels.
Import and Play lifecycle tests are prepared. Actual Unity Editor, rendering, Play Mode
and IL2CPP evidence remains pending; .NET-hosted Standard-assembly checks do not replace it.

## Permanent gear coupling

[Ideal gear and planetary constraints](GEAR_NETWORK.md) now project the free midpoint
and clutch/cylinder force responses into the same permanent constraint space. The
fired planetary laboratory combines a ring brake and a sun/ring clutch with an ideal
planetary and final drive, replaying an upshift and downshift. A clutch whose relative
speed is already permanently constrained is rejected as an undefined independent
reaction. Other clutch state, capacity, thermal and event contracts remain unchanged.
