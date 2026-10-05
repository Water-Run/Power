# Development status

**English** · [简体中文](DEVELOPMENT_STATUS.zh-CN.md) · [Français](DEVELOPMENT_STATUS.fr.md) · [Русский](DEVELOPMENT_STATUS.ru.md) · [日本語](DEVELOPMENT_STATUS.ja.md) · [한국어](DEVELOPMENT_STATUS.ko.md) · [Deutsch](DEVELOPMENT_STATUS.de.md) · [Español](DEVELOPMENT_STATUS.es.md) · [Italiano](DEVELOPMENT_STATUS.it.md) · [Português](DEVELOPMENT_STATUS.pt-BR.md)

Power! has a managed simulation core, shared model documents and portable assets,
a headless CLI, an MCP agent service, and a prepared Unity studio. Synthetic
laboratories exercise engine, transmission, hydraulic and electrical behavior.
Complete powertrains, coordinated ECU/TCU control, measured calibration and an
accepted Unity desktop application remain unfinished.

## Current implementation

| Area | Implemented | Remaining acceptance |
|---|---|---|
| Core | Explicit units and stable IDs; immutable compilation; bounded integer time; observable ledgers; replay, cancellation, independent forks and whole-batch rollback | Long-run and complete powertrain evidence |
| Engine | Sealed/open cylinders, slider-crank pressure work, bidirectional gas flow, crank-timed valves, wall heat and prescribed premixed combustion | Detailed intake/exhaust, ignition, mechanical losses, richer thermochemistry and measured engine behavior |
| Fuel | Tracked fuel/air/products, finite gaseous rails with cycle-dose metering, finite compliant liquid rails feeding films, wall-paid evaporation and vapor-only reaction; [Pump-fed liquid fuel rail](PUMP_FED_FUEL.md) | finite fuel tank and measured pump regulation, nonlinear magnetic/electronic/spray behavior, pressure-dependent phase equilibrium and measured fuel properties |
| Transmission | Static/sliding clutches, contact actuation, signed gears/planetaries, mapped converter/lockup and seven-forward/reverse DCT and four-forward/reverse Ravigneaux paths with resolved planet spin/orbital inertia | Mesh compliance/losses and load sharing, DCT actuation, complete AT pressure/shift control and measured routing, coordinated shifts, measured losses and richer converter behavior |
| Hydraulics | Compliant volumes, restrictions, pumps with explicit leakage/drag, relief, dynamic pistons, metered spools and finite-energy gas accumulators | Measured valve/accumulator/pump maps, seal friction, cavitation and complete transmission hydraulics |
| Electrical | RL motors, reciprocal variable-inductance solenoids, finite-charge battery, resistance/RC polarization, averaged duty conversion and accessories | Measured chemistry/thermal behavior, BMS, current control and complete supply integration |
| Controls | Sampled pressure PI, needle feedback/closure prediction and sensor-confirmed staged DCT control with actuator ownership, integer clocks and transactional memory | ECU/TCU torque coordination, sensors, actuators and fault handling |
| Documents and assets | 38 JSON/CLI laboratories, 37 MCP examples, asset v26 with v1-v25 readers | Workbench editing/saving and calibrated model collections |
| Agents | Twelve schema-defined MCP tools; compact evidence, revision checks and actionable diagnostics | Complete workflows for the remaining physical/control scope |
| Unity | Model import, exact-tick playback, schematic 3D components, controls, reset and prepared lifecycle tests | Actual Editor/Play acceptance, selectable plots, graph editing/saving and Player/IL2CPP |
| Native archive | Zig 0.15.2 research prototypes, preserved ABI and original source provenance | Historical reference; managed migration remains separate from full functionality |

Core and Assets target both `net10.0` and `netstandard2.1`; the Core has no Unity,
transport, model-provider or third-party dependencies. Unity Assets scripts use
C# 9. Unity loads the Standard assemblies built by the external SDK; it doesn't
compile .NET 10/C# 14 source.

## Evidence and limits

The [AT hydraulic actuation graph](AT_HYDRAULIC_ACTUATION.md) supplies all five
range elements and optional converter lockup from a shared shaft-driven pump.
Explicit fill/drain paths, finite piston motion, return springs and pad-derived
capacity retain hydraulic displacement work and actual capture/release behavior.
Prescribed valves are not sensor-confirmed AT control or measured valve-body
acceptance. Complete powertrain behavior and measured acceptance remain
unfinished.


The required serial `dotnet run --file tools/Build.cs -- verify` passes locally on
Windows x64. It covers both assembly targets hosted on .NET 10, an actual MCP child
process, all laboratory reports, the Zig archive and the C# ABI. Current counts,
log paths, schema checks, numerical results and retained CI provenance live in
[VALIDATION.md](VALIDATION.md). Standard-assembly tests on .NET 10 don't establish
Unity runtime compatibility.

The [liquid film increment](FUEL_FILM.md) now has analytic heating/saturation/dryout
checks, independent simultaneous ODE references, mass/chemical/thermal conservation,
portable replay and complete session transactions. Symmetric film ordering gives
second-order smooth refinement for films sharing a wall. Coupling to other wall
heat sources retains the existing first-order explicit-wall limit. Exact replay
is separate from timestep accuracy, passing KPIs and calibrated physics.

The [Ravigneaux research graph](RAVIGNEAUX_TRANSMISSION.md) adds single/double
pinion constraints, five friction paths, four forward ranges and reverse.
Independent free-mass, reflected-inertia and brake-capture references check
port reactions and heat. Shared torque and fired/converter experiments preserve
full replay and explicit research boundaries. The reduced path omits planet spin; detailed hydraulics,
AT control and OEM topology/calibration remain unfinished. The [resolved option](RESOLVED_PLANETS.md)
adds four actual meshes, two absolute spin rotors and explicit orbital inertia;
independent six-rotor mass, angular-momentum and capture references retain those
energies. Detailed tooth/lubrication/load-sharing behavior still needs evidence.

The [sampled DCT controller](DCT_CONTROL.md) now owns drive/selector commands,
preselects unloaded paths, waits for physical synchronization/lock, and performs
exclusive staged release/engagement. Integral gear requests, neutral abort,
direction/timeout/persistent-lock faults and new-request recovery are observable.
Confirmed gear can be temporarily zero during transient slip even after a prior
confirmation. Controlled long gear runs use transactional compensated coordinates,
with strict phase tolerances unchanged. The explicit state bound is 128; node and
component bounds remain 32/64, allowing the 70-state fired/controller composition.
Full torque-blended shifts, actuators and comprehensive ECU/TCU faults remain open.

The [dual-clutch increment](DUAL_CLUTCH_TRANSMISSION.md) now assembles seven
forward paths, an even-path reverse idler, three output/final-drive branches and
explicit friction selectors. Independent signed/reflected-inertia and preselection
impulse/heat references verify the power paths. Torque and fired experiments replay
through all ordinary graph/asset/agent layers. A bounded normalized linear lock
fallback resolves the previously failing six/seven handoff while keeping existing
trajectories as regressions. Prescribed selection/handoff isn't complete TCU or
detailed dog/baulk-ring/actuator behavior; parameters and OEM samples remain unverified.

The [closure-compensation increment](CLOSURE_PREDICTION.md) replays a bounded
held-input plant future without committing state. It predicts residual needle
flow and schedules voltage removal on the physical tick grid. The isolated dose
tracking improves while real closing/rebound and fuel/energy histories remain
unchanged physical mechanisms. Read-only predictions, integer bounds, horizon
refinement, zero allocations and complete transactions are verified. Prediction
holds other commands and omits future external input events; its model and finite
horizon are explicit limits, rather than calibration or complete ECU acceptance.

The [needle increment](NEEDLE_ACTUATION.md) couples magnetic flux energy and
reciprocal force to actual needle mass, spring/damping and elastic stops. Integer
sampling owns coil voltage from delivered-dose feedback. Fluid remains governed by
physical lift through closing delay and rebound; it isn't clipped to the target.
Independent magnetic/RL/motion references, source/phase/electrical/thermal ledgers,
portable/MCP replay and full controller transactions pass. Excess delivery and
remaining liquid at the experiment boundary remain observable; these results don't
establish calibrated dose tracking or complete injector electronics/magnetics.

The [liquid injection increment](LIQUID_FUEL_INJECTION.md) now starts from a dry
film and draws from a finite compliant source. Analytic rail pressure/work,
independent simultaneous refinement, complete source/film/chemical/thermal ledgers,
actual MCP replay and speculative clutch rollback pass. Rail pressure energy is
stored; nozzle heat and exported receiver pressure work remain distinct. The source is an explicit external boundary, not a modeled finite fuel tank. Tank depletion, pump efficiency/regulation, line losses, cavitation, pressure-dependent properties and finite-volume spray remain open. Parameters are `unverified`; this does not establish OEM calibration or actual Unity Editor/Play/Player/IL2CPP acceptance.

The `film-fired-cylinder` initially contains a declared liquid inventory. It heats
and evaporates that inventory before prescribed reaction; it doesn't implement a
liquid injector. Prepared Studio markers and tests consume the same definitions.
`POWER_UNITY_EDITOR` is unset, so actual Editor/Play, rendering and Player/IL2CPP
remain unverified.

All research parameters remain `unverified`. EA211 DJS + DQ200 and PSA EC5 + AT8
retain their complete powertrain boundaries and evidence manifests in
[assets/samples](../assets/samples). Missing OEM measurements stay missing.
Licenses and historical source provenance are preserved.

## Next development sequence

1. Continue the pump-fed rail with a finite fuel tank, measured pump regulation and refined magnetic/electronic actuation. Replace the declared
   exported displacement-work boundary when finite liquid volume and spray
   momentum are resolved. Keep delivered liquid, evaporated fuel and reaction
   separately observable and retain independent references.
2. Extend the engine with ignition control, intake/exhaust dynamics, mechanical
   losses and richer thermochemistry. Preserve the complete engine objective.
3. Extend the verified DCT research paths with detailed actuation, measured planet properties/losses and complete AT
   hydraulics/control, then build ECU/TCU shift/torque coordination from
   the verified gear, clutch, converter and hydraulic primitives. Add bounded
   controller state, sensor/actuator behavior and fault recovery.
4. Run `unity-test` with the pinned Editor, then obtain Player/IL2CPP evidence.
   Complete channel selection, graph editing and saving as distinct features.
5. Obtain measured maps, OEM data and uncertainty budgets for the two target
   powertrains before declaring calibrated samples or release readiness.

## Hydraulic AT feedback

`at_controller` accepts an integer requested range in [-1,4]; zero is neutral. It owns five fill/drain actuator pairs and optional converter lockup. Route order is carrier input, small-sun input, large-sun input, carrier brake, large-sun brake, then lockup.

The examples `controlled-hydraulic-ravigneaux` and `controlled-fired-hydraulic-ravigneaux` use request channel 900 and controller ID 1400. They retain 99 and 122 reported states within the unchanged 128-state limit. Asset v26 retains routes, gains and clocks and reads v1-v25.

These are research controls and parameters remain `unverified`. Coordinated ECU torque blending, detailed sensors/valves, comprehensive vehicle faults and OEM calibration remain unfinished. Managed and Standard checks do not establish actual Unity Editor/Play/Player/IL2CPP acceptance.

[AT_CONTROL.md](AT_CONTROL.md)

## Pump-fed liquid fuel rail

`liquid_rail_feed` pairs a liquid injector with an existing displacement pump and explicit material/thermal boundary. The hydraulic outlet node must match the rail compliance and initial absolute pressure. The paired pump and injector own this pressure node; other untracked fluid paths are rejected.

Asset v26 retains feed links and source temperature and reads v1-v25. Analytic shaft/pressure exchange, independent simultaneous ODE refinement, caloric mixing, mass/fuel/energy/volume ledgers, reverse return and complete rollback have separate checks.

The source is an explicit external boundary, not a modeled finite fuel tank. Tank depletion, pump efficiency/regulation, line losses, cavitation, pressure-dependent properties and finite-volume spray remain open. Parameters are `unverified`; this does not establish OEM calibration or actual Unity Editor/Play/Player/IL2CPP acceptance.

[PUMP_FED_FUEL.md](PUMP_FED_FUEL.md)
