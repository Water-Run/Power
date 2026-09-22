# 验证记录

## 2026-09-22 closing checkpoint: shaft-driven pump and pressure relief

Added ideal reversible displacement pumps, explicit finite/reservoir inlets and
finite-conductance one-way pressure relief. Pump speeds and hydraulic pressures join
the cylinder/converter Newton solve; pressure-clutch capacities refresh inside the
constraint iteration. Accepted shaft/fluid transfer, reservoir work, reference volume
and thermal losses share complete transactional state. JSON/schema, asset v11, agent
0.13.0, MCP discovery and the fired-pump laboratory use the same definitions.

Required serial verification completed successfully:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **165/165** managed checks, **132/132** Standard-assembly checks hosted on .NET 10,
  and **15/15** MCP groups against an actual child server process.
- **16/16** Zig tests and **6/6** Python ABI tests; all **176** retained historical
  numerical values match exactly. Source audit retains zero C/C++/Lua files.
- Release compilation: zero warnings and errors. Log:
  `artifacts/reports/pump-integration-verify.log`.

Independent evidence includes ideal power identities in both directions, analytic
shaft/compliance oscillation, closed-inlet inventory, reverse motoring, geared pump
reactions, exact midpoint relief decay, a regulated constant-load equilibrium, and an
analytic slipping-clutch pressure-feedback solution. Smooth oscillator and clutch
feedback refinements approach second order. Capture, branch independence, cancellation,
late numerical failure, retry, negative-pressure rejection and zero-allocation stepping
also pass. The initial allocation test exposed allocation in its own status formatting;
formatting is now limited to failures, and the measured hot loop allocates zero bytes.

Asset tests retain inlet pressure/topology and relief setting, reject malformed and
duplicate extensions, invalid dimensions/counts and forged downgrades. The authentic
v10 fired-hydraulic fixture retains fingerprint `01b69cb3abe52211` and final hash
`46a01d103e6159d3` after upgrade. Prior fixture digests and trajectories also pass.

The fired-pump laboratory has **89** matching report, portable and MCP boundaries over
0.8 s at 50,000-ns ticks. Its 56 counted states include a 4e-12 m³/Pa supply line, an
ideal 1e-6 m³/rad crank-driven pump and a 1e6-Pa relief setting with conductance
1e-9 m³/(s·Pa). Initial hydraulic energy is explicitly 3 J. Pump work is
**53.9425016232 J**, external hydraulic work **0 J**, and relief heat **45.0264051429 J**.
Final line pressure is **1.0697262404 MPa**, crank/turbine speed **69.7555356890 rad/s**,
load speed **6.6433843513 rad/s**, and transmission thermal-node temperature
**301.5306383749 K**. Total-energy residual is `1.0671e-9 J`; reference-volume residual
is `3.0493e-20 m³`. Fingerprint `d0bd8f29a706fd89`, final hash `572150ab5d66a2f6`.
Report: `artifacts/reports/fired-pump.json`.

All twelve laboratory documents pass JSON Schema; ten malformed pump/relief cases are
rejected. Audit: `artifacts/reports/pump-schema-audit.json`. Studio pump-port views and
import/Play lifecycle tests are prepared. `POWER_UNITY_EDITOR` is unset; Editor,
Play Mode and IL2CPP remain unverified. These managed checks are not Unity evidence.

Development is paused here at the owner's request. Pump losses/control, regulator
spool and actuator piston dynamics, complete DCT/AT, ECU/TCU, richer engine behavior,
calibrated vehicle samples and desktop acceptance remain unfinished. See
[the pump contract](HYDRAULIC_PUMP.md) and [development status](DEVELOPMENT_STATUS.md).

## 2026-09-22: hydraulic network and pressure-operated transmission

Added compliant hydraulic nodes, linear and regularized-turbulent restrictions,
explicit reservoir pressures, and pressure-operated clutches. Hydraulic pressure and
volume/work/heat histories participate in internal capture trials and whole-batch
transactions. JSON/schema, asset v10, agent capabilities 0.12.0 and the fired-hydraulic
laboratory share these definitions. See [the equations and limits](HYDRAULIC_NETWORK.md).

Verified locally on Linux with:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **154/154** managed checks and **123/123** Core/Assets Standard-assembly checks on .NET 10.
- **14/14** MCP groups against an actual child server process.
- **16/16** Zig and **6/6** Python ABI checks; all **176** historical values match exactly.
- Release build has zero warnings/errors; source audit passes.
- Log: `artifacts/reports/hydraulic-integration-verify.log`.

New physical evidence includes signed flow/passivity/range checks, analytic RC charging,
closed equalization, exact reservoir-work and thermal identities, independent RK4
nonlinear flow integration, second-order pressure and pressure-driven clutch-impulse
refinement, preload and capture/release. Failure after accepted hydraulic/clutch history,
cancellation, invalid input, forks and batching preserve the complete transaction.
Capture and snapshot reads allocate zero bytes after warm-up. An oversized drain step
rejects negative gauge pressure without changing state.

Portable replay exposed an omitted reservoir-pressure field during implementation.
The v10 restriction record now explicitly carries it, and roundtrip tests compare the
full physical descriptors and every replay boundary. Re-signed malformed records,
duplicate/missing extensions, wrong dimensions, invalid actuator ports and hydraulic-only
node downgrades are rejected. Authentic v9 fixture SHA-256
`96a78326ae2f2e8dd4a434fb81fbe88f4a19156cbf13cf069a7bd4e798e93c9f` retains fingerprint
`839d03901973668d` and final state `834a679376b7a6fd` when upgraded. Older fixtures remain.

The fired-hydraulic laboratory has **89** matching report/portable/MCP boundaries over
0.8 s at 50,000-ns ticks. Fingerprint `01b69cb3abe52211`, final hash `46a01d103e6159d3`.
Final crank/turbine speed is 70.94321138 rad/s; load speed is 6.75649632 rad/s. Reservoirs
supply 8 J, restrictions dissipate 7 J and hydraulic stored energy increases by 1 J.
Converter heat is 48.80297187 J; lockup heat 32.89304173 J; shift clutch/brake heat
119.31915873 J and 60.16098886 J. The shared heat node reaches 301.34088081 K.
Total-energy residual is `1.0896e-9 J`; reference-volume residual is `-1.0804e-18 m³`.
Net external source work is -65.07102675 J, including hydraulic supply, load and cylinder
back-pressure work. It is not a direct measurement of load-only work.

All eleven laboratory documents validate against the schema; ten malformed hydraulic
contracts are structurally rejected. The compiler adds dimension/topology/range checks.
Audit: `artifacts/reports/hydraulic-schema-audit.json`. Studio hydraulic views and
import/Play tests are prepared, but `POWER_UNITY_EDITOR` is unset; actual Editor,
rendering, Play Mode and Player/IL2CPP are unverified. Pumps/regulators, piston and
accumulator dynamics, complete DCT/AT, engine/controls and calibrated samples remain open.

The earlier source-work descriptions below now identify net external work explicitly:
that ledger includes cylinder back pressure, so its magnitude must not be labeled as
load-only output work. This is an evidence-description correction, not a physics change.

## 2026-09-22: coupled torque converter and fired lockup/shift laboratory

Added four explicit signed converter maps, passive interpolation validation,
stationary-stator reactions, fluid heat, and a joint converter/cylinder solve integrated
with gear constraints and clutch events. JSON/schema, portable v9, agent capabilities
0.11.0 and the `fired-converter` example share this contract. Studio views and Editor/Play
tests are prepared. See [CONVERTER_NETWORK.md](CONVERTER_NETWORK.md).

The full serial command:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

Local Linux evidence:

- **143/143** managed checks; **114/114** Core/Assets Standard-assembly checks on .NET 10.
- **13/13** MCP groups against an actual child server.
- **16/16** Zig and **6/6** Python ABI tests; all **176** historical numbers match exactly.
- Release build: zero warnings/errors; source audit passes.
- Log: `artifacts/reports/converter-integration-verify.log`.

New checks cover signed maps and reference-member continuity, stator/energy balance,
interior passivity violations, strict units, immutable point ownership, overflow failure,
analytic fluid coupling and stall, second-order refinement, reverse/coast/counterrotation,
shared ports, thermal/external loss routing, gear reflection and parallel lockup. Failure,
cancellation and branch checks preserve complete state; internal capture allocates zero
bytes after warm-up. Portable checks reject re-signed bad counts, wrong indices/units,
duplicate maps and downgrades. The authentic v8 fixture retains SHA-256
`6872f857bc521ed114d6eea5837cbb380a01f374ddfe7e829b30afa2c44fe0aa`, fingerprint
`6703f00c995e6b62` and final state `b328de221532fbae` when read or upgraded. Older fixtures
remain unchanged.

The 0.8-s fired-converter experiment uses 50,000-ns ticks and exactly replays all **87**
boundaries through the report, portable asset and MCP. Fingerprint `839d03901973668d`,
final hash `834a679376b7a6fd`; pump/turbine speed 73.37747546 rad/s and load speed
6.98833100 rad/s. Fluid heat is 24.27663069 J, lockup heat 22.84709072 J, shift-clutch
heat 157.18199410 J and brake heat 83.42288714 J. Thermal node 5 ends at 301.43864301 K,
with total-energy residual `3.2969e-11 J`. Net external source work is -63.19344680 J, including load and cylinder back-pressure
work, while tracked fuel releases 2049.02269691 J. These are synthetic numerical outputs.

A five-step study at 50,000/25,000/12,500/6,250/3,125 ns verifies shrinking combined
normalized distance in final crank speed, fluid heat and lockup heat relative to the
finest run, plus absolute differences below 0.0002 rad/s or J respectively. Individual
heat differences are nonmonotone near clutch events; no uniform coupled convergence
order is claimed. The finest run gives 73.37753852 rad/s, 24.27656820 J and 22.84711838 J.
Separate smooth analytic tests retain a refinement factor greater than 3.9.

Actual Unity Editor, Play Mode, rendering and IL2CPP remain unverified:
`POWER_UNITY_EDITOR` is unset. Complete engine behavior, DCT/AT topology, hydraulics,
controls and calibrated vehicle samples remain open. Quasi-steady maps do not establish
fluid dynamics or measured converter performance.

## 2026-09-22: coupled ideal gears and fired planetary transmission

Ideal gears and three-port planetary constraints now share the electromechanical,
cylinder and clutch solve. Direct constraint projection preserves compatible motion
and initial relative phase; per-port mean reactions are observable and transactional.
JSON/schema, asset v8, CLI/MCP and Studio use the same topology. See
[the gear contract and numerical limits](GEAR_NETWORK.md).

The full serial command passed on Linux x64 with cached .NET SDK 10.0.400/runtime
10.0.11 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **131/131** managed Core/application checks.
- **105/105** Core/Assets Standard-assembly checks hosted on .NET 10.
- **12/12** actual-child-server MCP groups.
- **16/16** Zig groups, **6/6** Python ABI tests and **176** exactly matching historical
  baseline values. Source audit finds no C/C++ or Lua implementation files.
- Release build reports zero warnings/errors. Log: `artifacts/reports/gear-integration-verify.log`.

Nine graph groups compare positive/negative ratios and free planetary motion/reactions
with independent exact references, multi-stage reflected inertia and stable-ID ordering,
RL motor/thermal and reacting-cylinder equivalence, and analytic reduction/direct shift
capture and heat. A constrained oscillator demonstrates second-order convergence and
energy conservation. Complete rollback after an accepted shift prefix, cancellation,
branch independence, exact batched replay and zero allocation are checked; allocation
coverage includes internal capture with variable factors. Rank, initial-speed, port,
ratio and unsupported-parameter diagnostics are explicit.

Two asset groups cover three-port topology, every playback boundary, malformed counts,
missing/duplicate/wrong-kind records, invalid carriers and downgraded gear attempts.
A genuine v7 fired-clutch fixture keeps fingerprint `197be44884deee90` and final hash
`28bf5335d8e35cde` after upgrading. Older fixtures and gear-free models remain unchanged.
Two managed integration groups add strict JSON/agent contracts, revision/cancellation/
branch behavior and the distinction between successful execution and passing KPIs.

The new fired planetary laboratory has **84 matching boundaries** across alternate
batches, portable playback and MCP. Its 0.8-second upshift/downshift experiment records
**-56.83157714 J** of net external source work, generating **254.52399968 J** in the sun/ring clutch and
**156.31560557 J** in the ring brake. The thermal node ends at **302.05419803 K**;
crank/load speeds are **76.81548837 / 7.31576080 rad/s**, with the ring held. Final
energy residual is **2.51020538e-10 J**. Fingerprint is `6703f00c995e6b62`; final hash
is `b328de221532fbae`. Source/report are `assets/labs/fired-planetary.power.json` and
`artifacts/reports/fired-planetary.json`. Parameters remain synthetic and `unverified`.
Disabling the shift schedule removes sun/ring clutch heat and changes load motion.

Studio has schematic three-port planetary and final-drive views with prepared import,
shift replay, reset and cleanup tests. `POWER_UNITY_EDITOR` is unset: no Editor/Play/
IL2CPP evidence is claimed. Complete DCT/AT topology, converter, hydraulics, ECU/TCU,
remaining engine behavior, measured vehicle calibration and release acceptance remain open.

## 2026-09-22: independent ideal gear and planetary references

Added immutable `IdealGearPair` and `SimplePlanetaryGear` primitives for constant external
torques. Results expose member speeds, displacements, reaction torques, work, kinetic
energy change and residual. Initial speeds must satisfy the constraint; no finite-slip
synchronization is inferred. See [equations, signs and limits](IDEAL_GEARS.md).

The full serial command passed on Linux x64 with cached SDK 10.0.400/runtime 10.0.11:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **118/118** managed Core/application checks and **94/94** Core/Assets Standard-assembly
  checks hosted on .NET 10; eight new groups run against each Core target.
- **11/11** actual-child-server MCP groups, **16/16** Zig groups and **6/6** Python ABI
  tests. All **176** historical baseline values match exactly; the source audit finds
  no C/C++ or Lua implementation files.
- Release build has zero warnings/errors. Log:
  `artifacts/reports/ideal-gear-reference-verify.log`.

New checks cover positive/negative gear ratios, reflected inertia, per-member impulse
balance, zero reaction power, independent planetary constraint-force dynamics, three
held-member conditions and sun/ring direct drive. Holding and locking loads are explicit.
Constant-load results match partitioned intervals, including speed reversal. Midpoint
sampling of sinusoidal loads converges against independent integrals at about fourfold
error reduction per interval halving for both primitives.

The deterministic sweep includes **2,500 cases per reference**. **10,000 evaluations of
each primitive** allocate no managed memory; independent concurrent callers share only
immutable parameters. Invalid values, incompatible initial speeds, constructor conditioning,
arithmetic overflow and finite force-cancellation errors reject without a partial result.
A high-ratio regression preserves a small, physically required reaction instead of losing
it by subtraction of nearly equal torques.

The graph solver and asset v7 semantics are unchanged; existing laboratory, portable and
MCP replay checks remain passing. These primitives are not yet coupled transmission
components, agent tools, shift simulations or calibrated models. Actual Unity
Editor/Play/IL2CPP verification remains pending, as do the rest of the engine, DCT/AT,
hydraulics, controls and complete calibrated vehicle objectives.

## 2026-09-22: coupled clutches and fired-engine load integration

Clutch static/kinetic reactions now share the electromechanical/cylinder solve, with
bounded internal capture/reversal events and friction-heat routing. Phase, mean outputs
and compensated heat are transactional and hashed. JSON/schema, CLI/MCP, asset v7 and
Studio consume the same component; v1–v6 readers remain supported. See
[the equations and explicit numerical limits](CLUTCH_NETWORK.md).

The full serial command passed on Linux x64 with cached .NET SDK 10.0.400/runtime
10.0.11 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **110/110** managed Core/application checks.
- **86/86** Core/Assets Standard-assembly checks hosted on .NET 10.
- **11/11** actual-child-process MCP integration groups.
- **16/16** Zig groups and **6/6** Python ABI tests; all **176** historical values
  match exactly. The source audit finds no C/C++ or Lua implementation files.
- Release build reports zero warnings and errors. The local log is
  `artifacts/reports/clutch-integration-verify.log`.

New physical evidence compares the graph with the exact constant-load `ClutchPair`
through internal engagement and reversal, positive/negative ratios and both thermal
destinations. Locked motor/current and reacting-cylinder pressure/fuel trajectories
match separate models with analytically combined inertia. A spring/brake oscillator
matches piecewise sinusoidal motion through three reversals and final capture at the
fourth turning point; halving the tick reduces error by more than 3.7x. Three-clutch
loops exercise redundant constraints and simultaneous engagement with conserved motion
and energy. Static release requires saturation; root-resolution residue cannot create
a spurious second reversal.

Complete multi-tick rollback is checked after an accepted heating/capture prefix and a
later numerical overload. Scheduled replay, cancellation, fork independence, immutable
ownership and zero allocation are retained. Allocation checks include repeated internal
reversal events, exercising candidate copies and variable factors. These tests support
the documented solver scope, not arbitrary large-tick hybrid accuracy.

The new `fired-clutch` laboratory has 67 exactly matching report boundaries across
alternate batches, portable playback and MCP. Its 0.6-second report records 96.74607609 J
of net exported external work, including the load and cylinder back pressure, 191.55570747 J of clutch heat, final engine/load speed
68.58488546 rad/s, and a final energy residual of 1.79e-10 J. Its fingerprint is
`197be44884deee90` and final state hash `28bf5335d8e35cde`. The clutch thermal node reaches
300.95777854 K. The source and result are `assets/labs/fired-clutch.power.json` and
`artifacts/reports/fired-clutch.json`; parameters remain synthetic and `unverified`.

Asset v7 round trips capacities and channels, rejects malformed/missing/duplicate
extensions and invalid downgrades, and preserves an authentic v6 fired-cylinder fixture's
digest, fingerprint and upgraded replay. Earlier model hashes remain unchanged.
Strict JSON/agent checks retain actionable errors, input/revision atomicity and the
distinction between successful execution, passing KPIs and measured calibration.

Studio's clutch plates, named phase outputs and import/lifecycle tests are prepared.
`POWER_UNITY_EDITOR` is still unset: Editor, rendering, Play Mode and IL2CPP remain
unverified. DCT/AT topology, planetary sets, torque converter, hydraulics, controls,
complete engine behavior and calibrated vehicle samples remain unfinished.

## 2026-09-22: managed dry-clutch law and constant-load reference

Added `DryClutch`, an immutable static/kinetic torque-capacity law, and `ClutchPair`,
an exact constant-load two-inertia or ground-brake reference. A slip-zero event is
resolved inside the interval, followed by constrained motion or reversal. Results expose
motion, angular advances, reaction mode, impulse, heat, external work and energy change.
See [the equations, API and implementation boundary](CLUTCH_PHYSICS.md).

The serial command passed on Linux x64 with cached .NET SDK 10.0.400/runtime 10.0.11
and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **99/99** managed Core/application checks and **77/77** Standard-assembly checks
  hosted on .NET 10, including the same ten new clutch groups in both targets.
- **10/10** actual-child-process MCP integration groups.
- **16/16** Zig groups and **6/6** Python ABI tests. All **176** historical numerical
  values match exactly. The source audit finds zero C/C++ or Lua implementation files.
- Release build reports zero warnings and errors. Full output is retained locally at
  `artifacts/reports/clutch-kernel-verify.log`.

Physical evidence covers analytical engagement, exact static load sharing, breakaway,
reversal, endpoint events, partial engagement, a ground brake and signed gear ratios.
The tests independently check momentum, integrated external work and absolute kinetic
energies, rather than comparing only the implementation's energy counters. A pair with
inertias 0.2 and 0.8 kg m2, initial speeds 100 and 0 rad/s, and sliding capacity 10 Nm
synchronizes at 20 rad/s after 1.6 s, generating 800 J of heat.

Constant-load solutions agree across interval partitions that cut through hybrid events.
Midpoint-frozen sinusoidal loads converge against independent velocity, angle and heat
integrals by more than 3.8x per halving, with the finest maximum error below 2e-5 in the
tested SI outputs. A deterministic 2,000-case range sweep checks conservation and repeat
evaluation. It found and fixed one-ulp overcounting of the sliding duration during a
reversal. Invalid data and arithmetic/event-resolution failures publish no partial result.
The warmed, isolated measurement path records zero allocation for 10,000 intervals.

This is a standalone Core physics primitive, **not yet a compiled graph component**.
Shaft/motor/cylinder coupling, multiple-clutch constraints, thermal routing, transactional
hybrid state, JSON/asset/MCP representation and Studio integration remain pending.
Existing graph fingerprints, seven laboratory experiments and asset v6 semantics remain
unchanged. Their previous combustion evidence is retained below.

`POWER_UNITY_EDITOR` remains unset. These Standard-assembly tests do not establish actual
Unity import, Play Mode or IL2CPP behavior. All vehicle parameters remain `unverified`;
the new primitive does not complete a transmission, controls or a calibrated powertrain.

## 2026-09-22: premixed combustion, transported reactants and fired load work

Added optional fuel/fresh-air/product tracking to gas nodes and explicit reservoir
fractions, plus a crank-referenced `premixed_combustion` component. A prescribed Wiebe
hazard consumes limiting reactants, stores irreversible crank history, and converts
chemical energy into thermal energy. Heat preview participates in conservative crank
work. See [the model, equations and limitations](PREMIXED_COMBUSTION.md).

Full serial verification passed on Linux x64 with cached .NET SDK 10.0.400/runtime
10.0.11 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **89/89** managed Core/application checks.
- **67/67** Core/Assets Standard-assembly checks hosted on .NET 10.
- **10/10** actual-child-process MCP integration groups.
- **16/16** Zig groups and **6/6** Python ABI tests, including both native hosts.
- All **176** historical native values match exactly; zero C/C++ or Lua sources found.
  Release build reports zero warnings and errors.

New physical evidence includes:

- Analytic Wiebe exposure through explicit cycles, negative phases and cycle wrap.
  Closed-vessel fuel, fresh-air consumption, heat and temperature match the closed-form
  limiting-reactant solution for lean, rich, fuel-free and air-free charges. Mass and
  total thermal-plus-chemical energy are checked independently of the heat counter.
- Closed-network constituent conservation and reservoir filling/discharge carry the
  upstream composition and chemical enthalpy in either flow direction. A constant-mass,
  constant-temperature vessel with balanced choked inflow/outflow matches exponential
  mixture replacement within 2e-5 in mass fraction, even with more than one vessel
  turnover per outer tick. This exercises the outgoing-flow bound when net mass and
  thermal-energy rates alone provide no useful tracer timestep.
- An independently written reacting-cylinder RK4 reference integrates crank motion,
  mass, thermal energy, fuel and fresh air with choked discharge. Its 1 and 0.5 microsecond
  solutions differ by less than 1e-9 normalized. Core ticks of 100, 50 and 12.5 microseconds
  reduce maximum normalized error by more than 2.8x and then 8x, with the finest below
  1e-4. This is wall-free second-order evidence; wall coupling remains first order.
- Multiple reacting cylinders on shared or shaft-coupled cranks conserve total energy
  and constituents, including isolated mixtures with different heating values and
  stoichiometric ratios. Stopping, reversing and retracing cannot repeat heat release;
  disabling a burn skips forward exposure without later catch-up. The greatest visited
  crank angle is observable as `burn_frontier_angle`.
- Failed scheduled torque changes after partial reaction roll back constituent state,
  angle history and compensated ledgers. Cancellation, caller-batch independence,
  fork isolation, under-resolved-burn rejection/recovery and zero stepping/snapshot
  allocations pass against both assemblies. Strict composition, ownership, units and
  the extended 64-state budget are checked.

The [fired-cylinder laboratory](../assets/labs/fired-cylinder.power.json) passes its KPIs
with fingerprint `a10f880d74494677` and **63** matching report boundaries. JSON/CLI, MCP
and decoded asset v6 agree on every channel at every boundary, including two load events
between report times. The 0.6-second run records **-369.98 J** of net external source work,
consumes **3.265e-5 kg** fuel in reaction and releases **1436.67 J**. Final net boundary
fuel energy is **1785.07 J**, with fuel also remaining in the chamber; these transient
numbers are not a steady efficiency or fuel-economy claim. Disabling combustion removes
heat release and produces substantially lower crank speed under the same load.

Final energy residual is approximately **-2.11e-9 J**, total mass residual **-1.25e-18 kg**,
fuel residual **2.03e-20 kg** and fresh-air residual **1.41e-18 kg**. Sampled pressure peaks
at about **2.08 MPa** and temperature at **1761 K**. These are synthetic model outputs;
the 10 ms report sampling does not establish the continuous pressure/temperature peak.

Asset v6 retains v1–v5 readers. New tests preserve model/mixture/burn definitions and
replay, reject wrong/missing/duplicate extension semantics and malformed counts, and
check an authentic pre-change v5 fixture's digest, fingerprint and upgraded replay.
[Fixture provenance](../tests/Power.Tests/Fixtures/README.md) records its uncommitted
source checkpoint without claiming a published commit. Existing nonreacting fingerprints
remain unchanged. Agent tests cover structured validation, invalid input and cancellation
without revision changes, stale writes, branch independence, filtered fuel/heat outputs,
smaller-tick recovery and the distinction between successful execution and failed KPIs.

The Draft 2020-12 schema and all **seven** laboratories pass Python `jsonschema`. Eight
malformed composition/burn shapes are rejected, including missing fractions, unknown
constituents, wrong units, invalid fractions, missing burn parameters and misplaced/null
reservoir fractions. Compiler checks separately enforce fraction sums and connected-mixture
compatibility. Local evidence is in `artifacts/reports/combustion-verify.log` and
`artifacts/reports/fired-cylinder.json`.

Unity import and Play Mode tests now include a heat-release marker, reset and complete
fired-example replay. **They have not run in the Editor**: `POWER_UNITY_EDITOR` is unset.
No rendering, Mono/IL2CPP or Player claim is inferred from Standard-assembly tests.
Constant R/gamma, prescribed burning and the forward-frontier policy are explicit limits;
fuel metering, ignition control, predictive chemistry, detailed intake/exhaust, mechanical
losses, transmissions, controls and calibrated vehicle samples remain open.

## 2026-09-22: crank-angle valve timing and changing-speed motoring

Added optional `valve_timing` on gas restrictions, with explicit 360/720-degree cycles,
opening/duration angles, peak input and effective-opening output. Profiles follow actual
crank angle through acceleration, stopping, reversal and phase wrap. Under-resolved lobes
reject the complete batch. See [the equations, bounds and scope](VALVE_TIMING.md).

The full serial command passed on Linux x64 with cached .NET SDK 10.0.400/runtime
10.0.11 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **76/76** managed Core/application checks.
- **57/57** checks against Core/Assets .NET Standard 2.1 assemblies hosted on .NET 10.
- **9/9** actual-child-process MCP integration groups.
- **16/16** Zig groups and **6/6** Python ABI tests, with both native hosts executed.
- All **176** historical native values match exactly; source audit finds zero C/C++ or
  Lua files. Release build reports zero warnings and errors.

Additional physical evidence:

- Closed-form adiabatic choked blowdown with independently integrated sin-squared valve
  exposure, at +40 and -40 rad/s, tests timing-to-flow coupling across cycle wrap. Refining
  ticks from 1 ms to 0.5 ms reduces relative mass error by more than 2.8x; 0.125 ms reduces
  it by more than a further 8x, to below 1e-7. Energy/mass ledgers are checked separately.
- An independent moving-cylinder RK4 reference includes crank pressure work, choked flow
  and a narrow timed lobe. The trajectory crosses both lobe boundaries. Halving reference
  steps from 1 to 0.5 microseconds changes normalized results by less than 1e-10. Core ticks
  of 200, 100 and 25 microseconds reduce error by more than 2.8x and then 8x, with finest
  error below 1e-6. Wall-free second-order evidence does not change the documented
  first-order wall coupling.
- Exact constant-torque kinematics checks opening during deceleration/reversal; stationary
  and disabled valves retain their documented behavior. A tick spanning an entire narrow
  lobe with closed endpoints must fail and roll back. Reducing the tick resolves its flow.
- Cancellation, failed schedules, caller-batch independence, fork isolation, malformed
  timing parameters and zero-allocation stepping/snapshots pass against both assemblies.

The [crank-timed laboratory](../assets/labs/crank-timed-cylinder.power.json) passes all
KPIs with fingerprint `38f0437eac4def69` and **63** matching report boundaries. JSON/CLI,
MCP and decoded asset v5 agree at every boundary, including two torque events between
report times. Final energy residual is approximately **8.53e-10 J**, and mass residual
is **-2.87e-18 kg**. Sampled crank speed ranges from **53.25 to 63.34 rad/s** while opening
is independently checked against crank angle. These are numerical checks of synthetic
parameters, not calibration.

Asset v5 retains v1–v4 readers. Tests reject malformed counts, duplicate/wrong timing
records and removed timing semantics, and verify an authentic pre-change v4 fixture
with its original digest, fingerprint and upgraded replay. Fixture provenance is recorded
[in the fixture notes](../tests/Power.Tests/Fixtures/README.md). Previous model fingerprints
remain unchanged. MCP tests also preserve state/revision on invalid peak input, and
application checks distinguish successful execution from failed KPIs and demonstrate
recovery from a narrow-lobe runtime failure by recreating with a smaller tick.

The Draft 2020-12 schema and all **six** laboratory documents pass Python `jsonschema`.
Six malformed timing shapes are rejected, including absent fields, extra profile fields,
wrong units, an invalid component placement and null timing. Compiler tests separately
cover cycle/range and topology restrictions.

Evidence is local Linux execution, recorded in `artifacts/reports/valve-timing-verify.log`
and `artifacts/reports/crank-timed-cylinder.json`. New Unity import/Play tests check timed
markers, reset and replay, but **have not run in the Editor**: `POWER_UNITY_EDITOR` is
unset. No Editor, rendering, Mono/IL2CPP or desktop Player result is inferred from managed
checks. Combustion, full engine behavior, transmissions, controls and calibrated vehicle
samples remain open; all research parameters remain `unverified`.

## 2026-09-22: moving-cylinder gas exchange and conservative crank work

Added `gas_cylinder`, a geometry component connecting a rotational crank and a gas
chamber with independent mass/internal energy. Initial volume is derived from crank
position and geometry; ambiguous volume/ownership is rejected. Gas exchange, crank
work and wall transfer run through whole-batch rollback, forks and cancellation.
The same definitions are accepted by JSON/CLI/MCP and portable asset v4, with v1/v2/v3
readers retained. See [the equations and contract](MOVING_CYLINDER.md).

Full serial verification passed on Linux x64 with cached .NET SDK 10.0.400/runtime
10.0.11 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **68/68** managed Core/application checks.
- **51/51** checks against Core/Assets .NET Standard 2.1 assemblies hosted on .NET 10.
- **8/8** actual-child-process MCP integration groups.
- **16/16** Zig groups and **6/6** Python ABI tests, with both native hosts executed.
- All **176** original native baseline values match exactly; source inventory contains
  zero C/C++ and Lua files. Release build reports zero warnings and errors.

New physics checks cover:

- Closed-valve agreement with the sealed-cylinder benchmark during forward/reverse
  rotation, dead centers and tiny ticks; constant mass and conserved energy.
- Open choked-flow motion compared with an independently written RK4 integration of
  mass, energy and crank ODEs. Its geometry and flow equations do not call the Core
  helpers under test. Halving the reference timestep from 1 to 0.5 microseconds changes
  normalized results by less than 1e-10. Reducing the Core tick from 200 to 100 microseconds
  reduces smooth-flow error by more than 3x; 25 microseconds reduces it by more than a
  further 10x and stays below 1e-6 relative.
- Wall-coupled refinement is assessed separately as first order: the same refinements
  reduce error by more than 1.7x and 3x respectively, with finest error below 1e-5 relative.
- Shared/coupled cranks, mixed sealed and open cylinders, gas links, wall heat and full
  energy/mass ledgers. Failed scheduled torque changes restore all earlier ticks and
  inputs; cancellation, fork isolation and zero stepping/snapshot allocations pass.

The new motoring laboratory has fingerprint `dd62971021fa06e6` and **28** replay
boundaries, all identical between JSON experiment reports, decoded assets and actual
MCP export. It demonstrably admits and expels gas while the chamber moves. Final energy
residual is `2.9882230023758893e-10 J`; mass residual is `1.463672932855431e-18 kg`.
These are numerical conservation observations for synthetic parameters, not calibration.
The preceding four laboratory reports retain their fingerprints and pass their replay/KPIs.

Portable coverage includes mixed old/new cylinder records, exact geometry round trip,
wrong-type/duplicate/missing extensions, invalid counts and rejection of moving-cylinder
records under older versions. The saved v3 fixed-volume fixture retains fingerprint
`eeb18a7f1dc76175` and replay after v4 re-encoding. Fixture source/digest provenance is
recorded in [Fixtures](../tests/Power.Tests/Fixtures/README.md).

The Draft 2020-12 schema and all five laboratories pass Python `jsonschema`; six malformed
moving-cylinder documents are rejected. Agent tests cover geometry errors, chamber
ownership, bounded nonlinear failure without revision/state changes and recovery.
Logs are `artifacts/reports/moving-cylinder-verify.log` and
`artifacts/reports/moving-cylinder-schema.log`; the experiment is
`artifacts/reports/moving-cylinder.json`.

Unity moving-piston/import/Play tests are prepared but not executed: `POWER_UNITY_EDITOR`
is unset. Unity Editor/Play/rendering, Mono/IL2CPP, Player packaging and this increment's
Windows/macOS execution remain unverified. Time-scheduled restriction openings do not
implement crank-angle valve timing. Combustion, full engine-cycle behavior, transmissions,
controls and calibrated vehicle samples remain open; the complete Power! objective is
not complete.


## 2026-09-22: finite gas-network JSON, asset and agent integration

The Core checkpoint at `69bc1c4` was verified before changes: **53/53 managed,
41/41 Standard-assembly and 6/6 MCP groups**. The existing solver equations,
fingerprint construction and physical limits are unchanged in this increment.

Full serial verification then passed on Linux x64 using the cached pinned .NET SDK
10.0.400 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **60/60** .NET 10 Core/application checks.
- **45/45** checks against the .NET Standard 2.1 Core/Assets assemblies, hosted on .NET 10.
- **7/7** MCP integration groups against an actual child server.
- **16/16** native Zig groups and **6/6** Python ABI tests; both native hosts executed.
- All **176** original native baseline values match exactly; zero C/C++ and Lua files.
- Release compilation: **zero warnings and zero errors**.

The four laboratory reports pass KPIs and replay: electrothermal (11 boundaries),
thermal network (11), sealed cylinder (21) and gas network (14). The gas document
compiles to the same fingerprint as an independently assembled Core definition:
`eeb18a7f1dc76175`. JSON reports, decoded v3 playback and actual MCP export agree at
every gas report boundary, including valve events between sampling boundaries. Mass
and energy residual checks use absolute limits of 1e-14 kg and 1e-6 J respectively;
the test also reconstructs reservoir energy exchange from chamber and wall states.

Portable checks include mixed gas/cylinder/thermal topology, non-default gas
composition, non-SI units, ownership, schedule bounds, cancellation and mid-batch
failure with event-cursor rollback. Correctly rehashed but invalid files cover counts,
missing/duplicate/wrong-type extension records, old-version gas rejection and stale
fingerprints. Authentic v1 and cylinder v2 fixtures retain their original fingerprint
and replay behavior after v3 re-encoding; the v2 fixture's source commit and hash are
recorded in [Fixtures](../tests/Power.Tests/Fixtures/README.md).

Agent checks cover discoverability, initial/scheduled opening bounds, invalid-input
atomicity, revision conflicts, cancellation, branch independence and the distinction
between a successful call and a failing KPI. Separately, Python `jsonschema` validated
the published Draft 2020-12 schema, all four laboratory documents and a fixed-opening
variant, and rejected twelve malformed gas document cases.

Logs: `artifacts/reports/gas-integration-baseline.log`,
`artifacts/reports/gas-integration-verify.log` and
`artifacts/reports/gas-integration-schema.log`. Reports and generated Unity assets
remain reproducible build artifacts rather than source fixtures.

`POWER_UNITY_EDITOR` is unset. Gas schematic views, import tests and a Play Mode
lifecycle/replay test are prepared but **not executed in Unity**. Editor/Play/rendering,
Mono/IL2CPP, Player packaging and this increment's Windows/macOS execution remain
unverified. Full engine-cycle physics, transmissions, controls and vehicle calibration
remain open; sample parameters stay `unverified`.


## 2026-09-14: compiled gas-network Core checkpoint

Recovered the WSL work through `4a81716` and its unfinished Core integration. The
checkpoint now compiles gas-only and mixed gas/thermal networks, validates composition
and opening ranges, and includes mass/internal-energy state and reservoir ledgers in
snapshots, hashes, forks and whole-batch rollback. A conservative stage limiter prevents
an isolated equalising pair from oscillating through equilibrium. The unchanged sealed
cylinder and linear models retain their prior fingerprints and replay behavior.

Serial verification using the pinned cached .NET SDK 10.0.400 on Linux x64:

- **53/53** .NET 10 Core/application checks.
- **41/41** checks against the .NET Standard 2.1 Core/Assets assemblies on .NET 10.
- **6/6** MCP integration groups against an actual child server.
- Existing electrothermal, thermal and cylinder experiment replay reports pass.
- Native Zig groups and six Python ABI tests pass; all **176** original baseline values
  match exactly. Source audit: zero C/C++ files and zero Lua files.

Gas-specific evidence includes choked/subcritical nozzle physics, analytic vessel
blowdown and refinement, reservoir filling enthalpy, reverse flow, closed-network mass
and energy conservation, finite-time analytic wall exchange, closed-valve isolation,
input/schedule range rejection, observable overflow, mid-batch failure/recovery, branch
and batching equivalence, immutable compilation and zero stepping/snapshot allocations.
Asset v1/v2 rejection is tested to prevent dropping unsupported gas fields.

See [GAS_NETWORK.md](GAS_NETWORK.md) for the numerical method and remaining scope.
This checkpoint has local Linux evidence; current Windows/macOS CI status must be read
from its commit's workflow. Unity Editor/Play/IL2CPP and calibrated vehicle behavior
remain unverified. The earlier standalone record below describes the preceding commit.

## 2026-09-14: gas-exchange physics (standalone)

Added the first slice of the gas-exchange increment as **physics only**: `IdealGas`,
`GasVolumeState` and `Orifice` in `src/Power.Core/GasExchange.cs`. Finite volumes now
carry mass and internal energy as independent states, and the orifice implements the
standard isentropic nozzle relations in both directions with a discharge coefficient and
a dimensionless opening fraction. `Numeric.Expm1` and `Numeric.Log1p` moved out of
`CylinderPhysics` and are shared; the implementations are unchanged, and every existing
cylinder state hash, model fingerprint and replay boundary still matches.

**No node kind, component kind, channel, schema field or asset version changed.** A model
document still cannot contain a finite gas volume, and the CLI, MCP and Unity surfaces are
untouched. The proposed split method with backward-Euler flow remains unvalidated and
unadopted. See [gas exchange](GAS_EXCHANGE.md) for the equations, the numerical limits and
the full list of contracts that did not land.

Six new analytic checks in `tests/Power.Tests/GasChecks.cs`, each written against an
independent closed form rather than a recorded output: choking continuity and flow-function
monotonicity for gamma in {1.1, 1.3, 1.4, 5/3}; 54 nozzle cases against the NASA compressible
mass-flow relations; orifice contracts including exact reverse-flow antisymmetry, closed-valve
isolation and rejected states; adiabatic vessel blowdown against the analytic isentropic
solution to 1e-9 relative; the reservoir-filling identity dU = cp*T_supply*dm with the
evacuated-vessel limit T -> gamma*T_supply; and closed two-volume conservation to 1e-14
relative in mass and 1e-12 in energy with pressure equalisation.

Full serial `dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify`
passed on Linux x64 with the cached pinned .NET SDK 10.0.400 and Zig 0.15.2: **44/44 managed
checks** (38 before this change), **26/26 checks against the Unity-facing .NET Standard 2.1
assemblies**, **6/6 MCP process groups**, three experiment reports with 11, 11 and 21 replay
boundaries, **16/16 native Zig groups** and the Python foreign-ABI tests. Both Zig hosts ran
against the actual shared library, the source audit reported `c_source_files: 0` and
`lua_files: 0`, and all **176 baseline values matched the original C binary exactly**
(maximum absolute error 0.0). The log is `artifacts/reports/gas-exchange-verify.log`.

Windows and macOS were not exercised for this change, and Unity Editor, Play Mode, rendering
and IL2CPP validation remain pending as before. Sample parameters remain `unverified`.

## 2026-09-11: Lua packaging cleanup

Removed the remaining LuaInstaller launcher and its obsolete packaging README.
The launcher depended on the never-implemented `power_native` bridge and had
no active build or runtime callers. Original paths and SHA-256 hashes are
preserved in the [migration manifest](../legacy/native/migration-manifest.json)
and match the files at its recorded source revision. Historical design
documents retain their provenance; their Lua proposals are retired.

The source audit now rejects Lua source, bytecode and packages in addition to
C/C++ source and headers, and reports `lua_files: 0`. Temporary untracked
`.lua`, `.luau`, `.luac`, `.rockspec`, `.rock` and uppercase `.LUA` probes each
produced a failing exit status and a structured error identifying the file;
the clean tree passed afterward.

Full serial `dotnet run --file tools/Build.cs -- verify` passed on Linux x64:
**38/38 managed, 26/26 .NET Standard assembly, 6/6 MCP, 16/16 Zig and 6/6 Python
ABI checks**. Both native hosts ran, and all 176 baseline values matched exactly.
The log is `artifacts/reports/lua-removal-verify.log`. Core behavior, sample
evidence and license files are unchanged. Unity Editor was not exercised.

## 2026-09-11: native Zig migration

The owner resumed the native language migration on 2026-09-10. All **26 C
implementation/test/host files and 12 headers** were replaced with Zig. The
[migration manifest](../legacy/native/migration-manifest.json) records original
Git revision, file paths and SHA-256 hashes. No C/C++ source or headers remain
in the repository source inventory; the root verification command enforces
that constraint. License/exception files and sample evidence are preserved.

Full serial `dotnet run --file tools/Build.cs -- verify` passed on Linux x64
using the cached pinned .NET SDK 10.0.400 and Zig 0.15.2: **38/38 managed
checks, 26/26 checks against the Unity-facing .NET Standard 2.1 assemblies,
6/6 MCP process groups, 16/16 native Zig groups and 6/6 Python foreign-ABI
tests**. The native suite also passed all 16 groups in Debug with safety checks
enabled. Both Zig hosts ran against the actual shared library. The library
exports only `pwr_get_api` and has no unresolved ELF symbols. All **176 values
at 11 electrothermal sample times exactly matched** the original C binary on
this host, with the same model fingerprint and channel contracts. The
cross-toolchain fixture comparison still uses explicit tolerances, and
same-binary replay must match exactly. Reports are in
`artifacts/reports/zig-migration-verify.log`, `native-verification.json` and
`native-electrothermal.json`.

ReleaseSafe library/host cross-compilation also passed for **x86_64 Windows**
and **aarch64 macOS**. Cross-compilation is not execution evidence for those
systems. Local logs are `artifacts/reports/zig-cross-windows.log` and
`zig-cross-macos.log`.

GitHub Actions subsequently completed actual verification successfully on
**Linux x64, Windows x64 and macOS arm64**, each passing all **38/38 managed,
26/26 .NET Standard assembly, 6/6 MCP, 16/16 Zig and 6/6 Python ABI checks**.
Both shared-library hosts ran on each platform. All 176 native baseline values
matched exactly on all three runners, and their source inventories contained
zero C/C++ sources or headers. Evidence:
[run 34549950147](https://github.com/Water-Run/Power/actions/runs/34549950147),
code commit [`dd3de10`](https://github.com/Water-Run/Power/commit/dd3de10294949c8b2ffb49afc02a9c70b5808c00).
The Windows checkout pins Zig files to LF; macOS verification uses Zig's bundled
Darwin stubs to avoid the newer Apple SDK incompatibility described in the
[native build notes](NATIVE_ZIG.md#build-and-maintenance). Complete job logs are
retained locally under `artifacts/reports/zig-ci-34549950147-{linux,windows,macos}.log`,
with run metadata in `zig-ci-34549950147.json`. The subsequent documentation and
comment corrections change no executable code.

The native suite additionally covers the previously unbuilt automatic-powertrain
module, including replay, energy accounting, brownout and transaction rollback,
plus concurrent SDK lifetime handling. Three historical test entry points
silently returned success on failed checks; the port fixes propagation and
separates the engine's steady combustion, limiter and backpressure scenarios.
See [the migration notes](NATIVE_ZIG.md) for the preserved equations and
corrected experimental setup. The original CTest result alone was insufficient
because of those hidden failures.

This migration does not complete the managed engine/transmission objectives or
establish vehicle calibration. Unity Editor, Play Mode, rendering, Mono and
IL2CPP were not exercised. All sample calibration remains `unverified`.


## 2026-09-08: sealed-cylinder increment

Serial `tools/Build.cs verify` passed on Linux x64 with SDK 10.0.400 and runtime 10.0.11: **38/38 managed checks, 26/26 checks against the actual .NET Standard 2.1 assemblies, and 6/6 MCP process integration groups**. Release compilation reported zero warnings and errors. The log is `artifacts/reports/cylinder-verify.log`; all three laboratory JSON documents also passed the published JSON Schema using the local `jsonschema` validator.

The new checks cover analytic slider-crank geometry and derivatives, ideal-gas state identities, two-second conservation runs with and without back pressure, second-order convergence under step refinement, reverse rotation, tiny steps and dead centers, shared/coupled cranks with electrical and thermal components, nonlinear failure rollback, cancellation, forks, units, malformed cylinder extensions, and zero managed allocations in steady-state stepping/snapshots. A preserved v1 fixture decodes, retains its original linear fingerprint and replays identically after v2 export.

GitHub Actions repeated the same verification successfully on **Windows, macOS and Linux**, with 38/38, 26/26 and 6/6 checks and zero warnings/errors on every platform. Evidence: [run 34176008291](https://github.com/Water-Run/Power/actions/runs/34176008291), code commit [`6209df2`](https://github.com/Water-Run/Power/commit/6209df22876f9928ddc04a1a32845cd7efe087da). Complete job logs and status metadata are retained locally as `artifacts/reports/github-actions-34176008291.log` and `.json`. The subsequent documentation update changes no executable code.

The synthetic cylinder experiment passed its final KPIs and replayed exactly at 21 boundaries through JSON, asset playback and an actual MCP child server. Linux results: fingerprint `c64b61efdb827680`, final speed `153.00340249454544 rad/s`, pressure `118835.36885412445 Pa`, temperature `315.16234058802814 K`, final energy residual `8.7464e-10 J`, and maximum sampled absolute residual `8.9570e-10 J`. Full report: `artifacts/reports/sealed-cylinder.json`. These values establish numerical evidence for this sealed ideal-gas benchmark, not engine calibration.

Unity importer and Play tests now include the cylinder asset and schematic piston motion, but were **not executed**. Unity Editor, Mono, rendering and IL2CPP evidence remains pending. The active stack remains C#/Unity; the future Zig rewrite direction introduces no native runtime in this increment.

## Pause checkpoint: 2026-09-08

The owner requested wrap-up and a development pause after the cylinder increment. Executable source remains at verified code commit `6209df2`; the later commits update documentation only. The prospective gas-exchange extension was not applied, built or published. Its [resume notes](NEXT_ENGINE_STEP.md) distinguish proposed work from implemented capabilities. No further build was needed for this documentation-only checkpoint. Resume development only after an explicit owner instruction.

## Historical baseline: 2026-09-07


环境：2026-09-07，Linux x64，.NET SDK 10.0.400，运行时 .NET 10.0.11。实际执行结果以 `tools/Build.cs verify` 输出和生成报告为准。

当前托管基线：30/30 核心、资产与 Agent 检查，19/19 标准库程序集检查，5/5 MCP 进程联调组通过；Release 构建为 0 警告、0 错误。实际 MCP 进程完成 12 个工具发现和输入/输出 Schema 检查，成功与错误响应均检查必填输出字段和文本兼容结果。原始执行日志保存于 `artifacts/reports/managed-verification.log`。

## 已取得的证据

- 核心和资产层已同时编译为 `net10.0` 和 `netstandard2.1`。
- 解析解检查覆盖恒定扭矩、RL 响应、热平衡；步长减半检查机械二阶与热一阶收敛。
- 正负传动比检查广义动量、阻尼发热和守恒；回馈制动检查负电流与源功减少。
- 输入拒绝、后续 tick 溢出、预取消、缓冲区容量检查都验证状态/调用者数据不被部分修改。
- 模型描述所有权、并行独立实例、完整状态分支和逐 tick/批量推进一致性有执行检查。
- 使用 .NET 线程分配计数器测得核心热路径的输入、步进和快照合计 0 托管分配；这不包含编译、报告或 Unity UI。
- 资产编码往返保留来源、模型与事件；损坏摘要、伪造计数、格式版本、模型指纹和额外字节均被拒绝。
- 调度输入覆盖零时刻、批次终点和呈现批次内部的事件，后续数值失败整批回滚。资产回放在每个 tick 均有输入变更时测得 0 托管分配；取消保留事件游标。
- JSON 报告与导入资产在所有报告边界比较状态哈希和输出值，包括不落在 20 ms 呈现边界的事件。
- 同一组物理检查直接加载实际复制给 Unity 的 .NET Standard 2.1 DLL，并核实其目标框架。执行宿主仍为 .NET 10，不能据此声称 Mono/IL2CPP 已通过。
- Agent 检查覆盖结构化字段诊断、过滤快照、会话限制、并发版本冲突、取消、父子分支隔离、生命周期与紧凑报告。
- 官方 MCP 客户端启动实际服务子进程，完成 12 工具发现、输入/输出 Schema、错误恢复、会话操作、完整实验和资产导出。Base64 解码后校验文件摘要，并比较导入回放与 MCP 实验终态。

默认电热实验推进 10 秒，在 5 秒降至 4 V，6 秒恢复 24 V。两个批大小在 11 个边界逐位一致。典型终值约为：电机 `29.74182442 rad/s`、负载 `9.91394147 rad/s`、电机温度 `302.4760663 K`。能量残差门槛为 `1e-5 J`。回放哈希只在相同二进制、运行时与架构范围内比较；跨运行时数值使用容差。

热交换实验使用节点 42/77、无外部输入和 7 ms 步长，推进 7 秒；两个批大小在 11 个边界一致。终温与后向 Euler 离散解相差小于 `1e-9 K`，与连续解析解相差小于 `0.004 K`，总能量误差小于 `1e-7 J`。两份报告分别为 `artifacts/reports/electrothermal.json` 和 `thermal-network.json`。

## 重现

```sh
dotnet run --file tools/Build.cs -- verify
```

这里的检查是会在 Release 执行断言的控制台验收程序，并非依赖 `Debug.Assert` 的空测试。它们不需要 Unity、Python 或原 C 库。MCP 项目使用官方 NuGet 包，`packages.lock.json` 固定解析结果。

GitHub Actions 已在 Windows、macOS、Linux 上完成同一组托管验收：各平台均为 30/30、19/19、5/5。证据对应代码提交 [`aea6136`](https://github.com/Water-Run/Power/commit/aea6136bbdcbeaea91d63836d947637e7eac730e) 和 [运行 34087686661](https://github.com/Water-Run/Power/actions/runs/34087686661)。本地保存了 `artifacts/reports/github-actions-34087686661.log` 与 `.json`，包含实际作业输出和终态；另外从不含缓存及生成程序集的干净源码副本完成了一轮本地验收，日志为 `github-clean-checkout.log`。

The repository and CI evidence links are public. Development resumed on 2026-09-08; the earlier records below identify their own verified baselines.

The GPL publication update added license notices without changing executable source content; a comparison against the preceding commit confirmed all 90 source/build edits were notice-only. A fresh serial verification passed 30/30 managed checks, 19/19 Unity-facing assembly checks, and 5/5 MCP integration groups, with zero build warnings or errors. Its log is `artifacts/reports/license-verification.log`. This does not add Unity Editor or Player validation evidence.

## 尚未取得的证据

当前环境没有安装 Unity Editor。本次没有运行编辑器导入、EditMode/PlayMode 测试、场景画面检查或 IL2CPP 构建。对应项目、场景、测试与自动化入口已提供：

```sh
dotnet run --file tools/Build.cs -- unity-test
```

先设置 `POWER_UNITY_EDITOR`。Unity 日志与 XML 结果输出至 `artifacts/unity`。Play 测试需要能够运行图形编辑器的环境和有效 Unity 许可。已写但未运行的测试涵盖：URP/程序集及两种模型资产导入、回放一致性、连续启停无残留、10 秒参考实验、运行中切换纯热拓扑、动态节点和输入列表、7 ms tick 调度。还需人工检查控件、主题、不同窗口大小、桌面平台显示，并完成三平台 Player 构建。

发布入口为 `Power.Studio.Editor.ProjectSetup.BuildPlayer`，使用所选桌面目标和 IL2CPP。需要相应 Unity 平台构建模块；目前没有已构建或已测试的 Player 包。

所有当前参数均为合成实验参数。完整发动机/变速器功能、实车标定、排放/声学、实时预算与长时运行仍需后续实现及验证，不能由这些检查推导完成。
