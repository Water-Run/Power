# Hydraulic flow and pressure-operated clutches

The managed hydraulic domain supplies pressure from a solved flow network to shift
clutches and lockup. It supports compliant chambers, linear restrictions, regularized
turbulent restrictions, explicit pressure reservoirs and pressure-operated friction
clutches. Hydraulic state and ledgers participate in the same internal clutch intervals,
complete batch rollback, forks and observable contract as the fired powertrain.

## Pressure storage and scope

A `hydraulic` node has positive `storage` C in `m3_pa` and nonnegative initial gauge
pressure in Pa or bar. All hydraulic pressures use the same fixed tank reference.
There is no inferred atmospheric pressure, fluid property, leakage or OEM parameter.

```text
stored reference-volume inventory = C * p
stored elastic pressure energy    = C * p^2 / 2
C * dp/dt                         = sum(incoming Q)
```

C is an explicit constant effective compliance. The familiar small-compression chamber
limit is `C = V / bulk_modulus`; a compliant actuator/line can have additional effective
storage. Power! tracks reference-volume inventory, not a full variable-density liquid
mass or temperature-dependent equation of state. A negative final gauge pressure is
outside this model and rejects the complete batch; it is never silently clamped into a
cavitation model. Absolute-pressure cavitation, entrained gas, gas-charged accumulator
laws, free piston travel, inertia and end stops remain separate work.

The compressibility basis is documented in [MathWorks Constant Volume Chamber (IL)](https://www.mathworks.com/help/simscape/ref/constantvolumechamberil.html).
Its general liquid model is broader than Power!'s constant-compliance reduction. No
fluid-property defaults or implementation code were copied.

## Restrictions, source work and heat

Both restriction components connect hydraulic `node_a` to either distinct hydraulic
`node_b` or an explicit reservoir when B is omitted/zero. Reservoir gauge pressure must
then be specified. `initial_input` is an explicit opening fraction in `[0,1]`; an optional
input channel controls it. Zero opening seals the path exactly. Leakage must be another
explicit path or a nonzero opening. The optional thermal `heat_node` receives pressure
loss; absent a sink, loss enters the external heat-rejection ledger.

For `d = pA - pB`, positive Q flows A to B:

```text
hydraulic_resistance: Q = opening * G * d
hydraulic_orifice:    Q = opening * K * d / (d^2 + transition_pressure^2)^(1/4)
restriction loss:     Q * d >= 0
reservoir work:       reservoir_pressure * reference_volume_entering_network
```

G uses `m3_s_pa`, meaning m³/(s·Pa). K uses `m3_s_sqrt_pa`, meaning m³/(s·sqrt(Pa)).
The orifice transition pressure must be positive and has explicit pressure units. It
regularizes the laminar limit, keeps the flow derivative finite at zero differential,
and approaches signed square-root flow at large differential. Coefficients may be zero.
Power! evaluates the denominator with scaled arithmetic to avoid squaring huge pressures.

The smooth restriction form follows the large-port, constant-density, no-pressure-recovery
limit documented by [MathWorks Local Restriction (IL)](https://www.mathworks.com/help/simscape/ref/localrestrictionil.html).
K is supplied directly; Power! does not invent density, viscosity, Reynolds number or
area measurements. Geometry/property-based identification remains future work.

Fixed reservoirs are external power boundaries. Their work is counted in both
`hydraulic_work` and the global `source_work`; it is not a modeled engine/electric pump.
A shaft-driven pump must eventually exchange equal mechanical and hydraulic work,
and electric pump operation must include the electrical circuit and control load.

## Pressure clutch

`hydraulic_clutch` uses the existing bounded Coulomb constraint/event solver, with
rotational A/B ports (or a ground brake), signed ratio and optional heat sink. It requires
an explicit hydraulic `pressure_node`, piston area, preload force, effective radius,
static/sliding friction coefficients and 1–128 friction surfaces. It has no direct
engagement input. Required dimensions are area, force and length; friction and surface
count are dimensionless. Static friction must be at least sliding friction, both
nonnegative.

```text
normal_force      = max(piston_area * gauge_pressure - preload_force, 0)
static_capacity   = static_friction  * surfaces * effective_radius * normal_force
sliding_capacity  = sliding_friction * surfaces * effective_radius * normal_force
```

This is a rigid-contact pressure-actuation reduction. The hydraulic node carries the
explicit effective compliance, and the clutch law derives normal force without an
unmodeled pressure-command lag. It does not implement free fill, moving pressure plates,
release levers, piston inertia, wear, centrifugal oil pressure or temperature fade.
Those effects need additional conserving components and measurements. Pressure-dependent
friction capacity is described in [MathWorks Disc Friction Clutch](https://www.mathworks.com/help/sdl/ref/discfrictionclutch.html);
Power!'s declared reduction and solver limits are independent design decisions.

## Integration and transaction contract

A bounded implicit midpoint solve advances all chamber pressures and restriction flows
together. It permits 24 Newton iterations and 16 halving line-search attempts. Pressure
residual tolerance is `2e-7 + 64*epsilon*(abs(midpoint)+abs(old)) Pa`. The analytical flow
derivative builds the network Jacobian. After convergence, pairwise edge transfers
update the chamber states and reference-volume ledger together. Actual old/new midpoint
pressures determine pressure-work loss, matching the stored quadratic energy change.
A negative accepted restriction loss or final gauge pressure rejects the interval.

Clutch capacities use these same interval midpoint pressures. Internal capture trials
repeat the hydraulic solve on complete speculative state copies; rejected trials leave
no volume, source-work or heat history behind. Pressure crossing the preload threshold
uses the interval capacity approximation, so timestep refinement is required near
engagement and release. There is no claim of exact continuous threshold timing. Existing
clutch event/constraint, gear, gas and combustion limits still apply.

Mean restriction flow and power are weighted over accepted internal intervals and divided
by the full tick. Cumulative heat uses compensated summation. Each hydraulic node adds
one logical state; each restriction adds three history states. The existing 32-node,
64-component and 64-state limits remain. All hydraulic histories are copied and hashed;
failed/cancelled batches commit no changes. Successful stepping, including clutch capture,
allocates no managed memory after warm-up.

On `numerical_failure`, reduce `step_ns` and inspect compliance, restriction coefficients,
pressure scales and clutch geometry. Implicit midpoint is not a guarantee of positive
pressure at arbitrary step sizes. The compiler checks dimensions and topology; it cannot
guarantee that every future command/timestep remains numerically admissible.

## Observable and document contract

| Object | Fields | Meaning |
|---|---|---|
| Hydraulic node | `pressure`, `volume`, `internal_energy` | Gauge pressure, C·p reference-volume inventory, C·p²/2 elastic energy |
| Restriction | `volume_flow`, `heat_flow`, `fluid_heat` | Last-tick mean flow A→B, mean pressure-loss power, cumulative loss |
| Pressure clutch | Existing clutch fields; `clamp_force`, `static_capacity`, `sliding_capacity` | Current pressure-derived force/capacities plus accepted friction history |
| Global | `hydraulic_volume_in`, `hydraulic_volume_residual`, `hydraulic_work` | Signed reservoir inventory transfer, inventory change minus transfer, reservoir pressure work |

Mean flow/power start at zero. Changing valve inputs does not rewrite previous-tick
means or instantaneously alter stored pressure. Source-work, heat and total-energy
residual include the hydraulic network alongside mechanical, electrical and gas energy.
A successfully executed experiment can still fail KPIs; calibration remains `unverified`.

JSON, Core factories, CLI and MCP use the same definitions. Asset v10 retains flow
coefficients, reservoir pressures, pressure-port connections and actuator geometry;
authentic v1–v9 fixtures preserve older fingerprints/replay. Hydraulic models add
fingerprint tag 11 and advertise `compliant_hydraulic_powertrain`. Hydraulic-free
models keep their prior solver behavior and fingerprints.

## Laboratory and numerical evidence

Request MCP example `fired-hydraulic`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-hydraulic.power.json --output artifacts/reports/fired-hydraulic.json
```

Three 2e-12 m³/Pa chambers and six explicit turbulent valve paths operate the sun/ring
clutch, ring brake and converter lockup. Supply pressure is 1 MPa gauge and drain is
zero. Valve schedules include an explicit release/fill gap during each shift; this is
an experiment schedule, not an ECU/TCU. Pump dynamics are not inferred from the fixed
supply reservoir. The pressure rises and decays from flow rather than following the
valve command instantaneously.

The 0.8-s laboratory uses 50,000-ns ticks and replays all 89 boundaries exactly through
portable assets and MCP. Its fingerprint is `01b69cb3abe52211`, final state hash
`46a01d103e6159d3`. Final crank/turbine speed is 70.94321138 rad/s and load speed
6.75649632 rad/s. The reservoir supplies 8 J of hydraulic work; final total-energy
residual is about `1.09e-9 J`, and reference-volume residual about `-1.08e-18 m³`.
All parameters remain synthetic and unverified.

Tests cover analytic RC charging and closed-network equalization, exact work/heat
identities, a separately integrated RK4 turbulent transient, second-order pressure and
clutch-impulse convergence, preload, capture/release, thermal routing, transactional
failure/cancellation, branches and zero-allocation capture. Portable tests reject
malformed and missing physical data, including reservoir pressure, with valid digests.
The fired experiment verifies pressure delays, complete heat and volume ledgers and
boundary-by-boundary replay. Sealing all valve paths preserves initial pressures and
prevents commanded lockup from appearing without flow.

Studio includes schematic hydraulic chambers, reservoir/valve paths and pressure-clutch
connections. Prepared import/Play tests still require the pinned Unity Editor. Neither
these assemblies nor a synthetic laboratory complete DCT/AT topology, pump/regulator
hardware, controls, engine behavior, calibrated vehicle samples or desktop release.

## Subsequent shaft-driven supply

The [pump/relief increment](HYDRAULIC_PUMP.md) adds a crank-driven supply path and joint
pressure/shaft solve. This document's fixed-reservoir laboratory remains an unchanged
regression checkpoint. Pump losses/control, regulator spool and actuator piston dynamics
remain separate unfinished work.
