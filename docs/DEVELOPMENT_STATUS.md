# Development status — 2026-09-22

Power! has a verified managed simulation foundation and a synthetic fired powertrain
path: open gas cylinders, prescribed combustion, mapped torque converter, lockup clutch,
pressure-operated planetary shift elements and final drive. JSON, CLI, MCP and portable assets share the
same definitions. This is a numerical development checkpoint. Complete engine behavior,
DCT/AT topology, pump losses, regulator and piston dynamics, ECU/TCU coordination, calibrated vehicle samples
and an accepted Unity desktop application remain unfinished.

## Implemented and verified locally

| Area | Available behavior | Evidence and boundary |
|---|---|---|
| Core | Immutable compiled models, explicit units/IDs, bounded integer ticks, replay, cancellation, independent forks and whole-batch rollback | Dependency-free `net10.0` and `netstandard2.1` assemblies |
| Engine physics | Sealed/open cylinders, bidirectional gas flow, timed valves, wall transfer and prescribed premixed combustion | Analytic and independent ODE checks, conservation and refinement; no predictive combustion or calibrated engine |
| Fuel and gas | Fuel/air/product transport, explicit reservoir fractions, chemical energy and irreversible burn history | Nonnegative inventories, mass/constituent/energy ledgers and allocation-free stepping |
| Clutches | Static/sliding reactions, signed ratios, ground brakes, capture/reversal events and thermal routing | Exact reference pair, changing-load refinement, coupled motor/cylinder checks, complete rollback |
| Gears | Signed ideal gears and three-port planetaries, permanent constraints, reactions and phase preservation | Exact references, reflected inertia, analytic shift capture/heat, conservation and convergence |
| Converter | Four explicit signed maps, stationary-stator reaction, fluid heat and separate lockup | Passive interpolation, analytic coupling/stall, reverse/coast/counterrotation, shared ports, joint cylinder/clutch/gear solving |
| Hydraulics | Compliant pressure chambers, controlled restrictions, ideal shaft-driven pumps, finite-conductance relief and pressure-operated clutches | Joint mechanical/fluid power, analytic feedback and second-order smooth refinement, volume/energy ledgers and complete rollback |
| Integration | Twelve JSON/CLI laboratories and eleven discoverable MCP examples | All 89 fired-pump boundaries match portable/MCP replay; parameters remain synthetic |
| Assets | v11 retains pump/inlet topology and relief characteristics | Bounded tables, malformed-record rejection, authentic v1–v10 fixtures and unchanged prior fingerprints |
| Agent interface | Twelve tools with schemas, compact evidence, revisions, branches and actionable errors | Actual child-server integration; tool success, passing KPIs and calibration remain distinct |
| Native archive | Zig 0.15.2 prototypes and preserved binary ABI | Historical reference, separate from the active C#/Unity stack; source audit and baseline checks retained |

The required serial command is `dotnet run --file tools/Build.cs -- verify`. This
workspace uses `.cache/dotnet/dotnet` for the pinned SDK. The pump checkpoint
passes **165/165 managed checks, 132/132 Standard-assembly checks and 15/15 MCP groups**,
plus **16/16 Zig** and **6/6 Python ABI** tests. All **176** historical baseline values
match exactly. The Standard assemblies run under .NET 10, which does not establish
Unity runtime compatibility. See [VALIDATION.md](VALIDATION.md) and
`artifacts/reports/pump-integration-verify.log` for the local evidence.

## Closing checkpoint

Development is paused at the owner's request after the verified pump/relief checkpoint.
The [pump contract](HYDRAULIC_PUMP.md) connects crank motion to hydraulic flow and pressure
reaction in the shared solve. The `fired-pump` laboratory replaces the fixed-pressure
supply with a shaft-driven pump, compliant line and pressure relief. All 89 report,
portable and MCP boundaries agree. Pump work is 53.94250162 J; external hydraulic work
is zero. Energy residual is about `1.07e-9 J`. Fingerprint `d0bd8f29a706fd89`, final hash
`572150ab5d66a2f6`. Asset v11 preserves authentic v1–v10 fixtures.

The full powertrain objective remains unfinished. Resume from the sequence below;
no calibration or actual Unity acceptance is claimed.

## Retained hydraulic checkpoint

The [hydraulic contract](HYDRAULIC_NETWORK.md) now connects solved pressure to clutch
force and capacity. Six explicit valves charge/drain three compliant chambers in the
[fired-hydraulic laboratory](../assets/labs/fired-hydraulic.power.json). Commands change
valve opening; pressure follows from flow and storage. Source work, restriction heat,
reference-volume inventory and all clutch histories share the transaction and ledgers.

The 0.8-s experiment has 89 matching replay boundaries. Reservoirs supply 8 J of
hydraulic work; restriction loss is 7 J and stored hydraulic energy increases by 1 J.
The final crank/turbine speed is 70.94321138 rad/s and load speed 6.75649632 rad/s.
Total-energy residual is about `1.09e-9 J`; hydraulic reference-volume residual is
`-1.08e-18 m³`. Fingerprint `01b69cb3abe52211`, final hash `46a01d103e6159d3`.

Analytic charging/equalization, independent nonlinear flow integration and pressure-driven
clutch impulse verify conservation and second-order smooth refinement. Tests also cover
preload, capture/release, zero-allocation stepping, branches, cancellation and rollback.
Asset replay caught a missing reservoir-pressure field during development; v10 now
retains that physical boundary explicitly, with a regression check.

This is constant effective compliance and rigid-contact pressure actuation. Internal pump and
regulator dynamics, piston travel/inertia, cavitation, gas accumulator behavior and
ECU/TCU valve control remain open. Fixed pressure reservoirs are explicit external power
boundaries, not engine-driven or electric pumps. Parameters remain unverified.

## Retained converter checkpoint

The [converter contract](CONVERTER_NETWORK.md) defines explicit forward/reverse pump and
coast maps, their units, passivity and continuity checks, observable semantics and solver
limits. Converter torque responds to the jointly solved midpoint speeds, including
cylinder work, gear reactions and clutch capture intervals. Heat enters the same thermal
and total-energy ledger. Runtime histories, inputs and physical states roll back together.

The [fired-converter laboratory](../assets/labs/fired-converter.power.json) schedules
lockup, release, recapture and a reduction/direct upshift and downshift over 0.8 s.
At 50,000-ns ticks it records -63.19 J of net external work, produces 24.28 J of fluid heat,
22.85 J of lockup heat and 240.60 J across the two shift elements. The shared thermal
node reaches 301.43864 K. Final energy residual is approximately `3.30e-11 J`.
Its fingerprint is `839d03901973668d`; final replay hash is `834a679376b7a6fd`.

Standalone fluid-coupling and stall solutions show second-order convergence. A separate
five-step fired-model study checks combined convergence across fluid, combustion and
clutch transitions. Individual small heat differences can be nonmonotone near events;
this coupled model does not claim uniform second order. These are synthetic transient
results, not measured converter efficiency, vehicle performance or calibration.

Earlier laboratories and independent references remain regression evidence. See
[coupled gears](GEAR_NETWORK.md), [clutches](CLUTCH_NETWORK.md),
[premixed combustion](PREMIXED_COMBUSTION.md) and the chronological validation record.
The converter implementation preserves their model fingerprints and replay trajectories.

## Prepared but not verified in Unity

Studio imports the same Core/Assets assemblies and twelve generated model assets. It has
schematic vessels, moving pistons, timed valves, heat-release markers, clutch phases,
ideal/planetary connections, converter pump/turbine/stator views, hydraulic chambers,
valves, shaft-driven pump ports and pressure-clutch connections. Import,
lockup/shift playback, reset and cleanup tests are prepared. Plots still show rotor
speeds; general channel selection, graph editing and saving remain unfinished.

`POWER_UNITY_EDITOR` is unset. Editor import, Play Mode, rendering, Mono/IL2CPP and a
desktop Player remain unverified. Unity Assets scripts remain C# 9. Unity does not
compile .NET 10/C# 14 source; the SDK builds the separate Standard assemblies.

## Next development sequence

1. Extend the ideal pump and quasi-steady relief with measured loss/control behavior,
   electric supply integration and required piston/spool/accumulator dynamics, then
   extend toward complete DCT/AT topologies. Prescribed valve
   schedules still need ECU/TCU coordination.
2. Continue engine fuel metering and ignition control, intake/exhaust dynamics, mechanical
   losses and richer thermochemistry. Prescribed burns do not implement injectors,
   predictive kinetics, knock, emissions or an ECU.
3. Implement ECU/TCU torque coordination, sensors, actuators, power supply, accessories
   and faults within the full powertrain boundaries, with independent evidence.
4. Run `unity-test` with the pinned Editor, then obtain Player/IL2CPP evidence. Complete
   Studio channel selection, graph editing and saving as separate deliverables.
5. Collect missing measurements and uncertainty budgets for EA211 DJS + DQ200 and
   PSA EC5 + AT8 before declaring calibrated samples or release readiness.

All sample parameters remain `unverified`. Full sample boundaries, evidence manifests,
licenses and historical source provenance are preserved. Research inputs never silently
substitute for missing OEM measurements.
