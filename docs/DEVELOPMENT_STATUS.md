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
| Fuel | Cycle metering, pump-fed compliant rails, finite tanks, conserving relief returns, film evaporation, physical needles and bounded closure prediction; [Geometric tank and finite headspace](TANK_HEADSPACE.md) | slosh/hydrostatic shape and measured pump filling/regulation, nonlinear magnetic/electronic/spray behavior, pressure-dependent phase equilibrium and measured fuel properties |
| Transmission | Static/sliding clutches, contact actuation, signed gears/planetaries, mapped converter/lockup and seven-forward/reverse DCT and four-forward/reverse Ravigneaux paths with resolved planet spin/orbital inertia | Mesh compliance/losses and load sharing, DCT actuation, complete AT pressure/shift control and measured routing, coordinated shifts, measured losses and richer converter behavior |
| Hydraulics | Compliant volumes, restrictions, pumps with explicit leakage/drag, relief, dynamic pistons, metered spools and finite-energy gas accumulators | Measured valve/accumulator/pump maps, seal friction, cavitation and complete transmission hydraulics |
| Electrical | RL motors, reciprocal variable-inductance solenoids, finite-charge battery, resistance/RC polarization, averaged duty conversion and accessories | Measured chemistry/thermal behavior, BMS, current control and complete supply integration |
| Controls | Sampled pressure PI, needle dose/closure control, staged DCT handoff and pressure-feedback AT shifts with physical lock confirmation | ECU/TCU torque coordination, sensors, actuators and fault handling |
| Documents and assets | 44 JSON/CLI laboratories, 43 MCP examples, asset v29 with v1-v28 readers | Workbench editing/saving and calibrated model collections |
| Agents | Twelve schema-defined MCP tools; compact evidence, revision checks and actionable diagnostics | Complete workflows for the remaining physical/control scope |
| Unity | Model import, exact-tick playback, schematic 3D components, controls, reset and prepared lifecycle tests | Actual Editor/Play acceptance, selectable plots, graph editing/saving and Player/IL2CPP |
| Native archive | Zig 0.15.2 research prototypes, preserved ABI and original source provenance | Historical reference; managed migration remains separate from full functionality |

Core and Assets target both `net10.0` and `netstandard2.1`; the Core has no Unity,
transport, model-provider or third-party dependencies. Unity Assets scripts use
C# 9. Unity loads the Standard assemblies built by the external SDK; it doesn't
compile .NET 10/C# 14 source.

## Evidence and limits

The required serial `dotnet run --file tools/Build.cs -- verify` covers both assembly targets, an actual MCP process, all laboratories, the Zig archive and the C# ABI. Counts, results and log paths live in [VALIDATION.md](VALIDATION.md). Standard-assembly checks hosted on .NET 10 don't establish Unity runtime acceptance.

Fuel supply includes [finite tanks](LIQUID_FUEL_TANK.md) and [tracked relief returns](LIQUID_FUEL_RETURN.md), with mass, caloric, chemical and pressure-work ledgers. The [hydraulic AT controller](AT_CONTROL.md) regulates actuator pressure and confirms physical range/lockup. Independent references and complete transactions support these research models; complete ECU/TCU coordination and measured hardware behavior remain open.

`POWER_UNITY_EDITOR` is unset in the current environment. Studio import, playback and prepared tests still need actual Editor/Play, rendering and Player/IL2CPP evidence.

All parameters remain `unverified`. EA211 DJS + DQ200 and PSA EC5 + AT8 retain complete boundaries and evidence manifests in [assets/samples](../assets/samples). Missing OEM measurements stay missing. Licenses and historical source provenance are preserved.

CLI `list-labs`, MCP example discovery and serial verification share [one laboratory catalog](../assets/labs/catalog.json). Verification checks that it covers every laboratory source.

## Next development sequence

1. Extend pressure-dependent phase equilibrium, cavitation, measured pump filling/regulation and refined magnetic/electronic actuation. Replace the declared
   exported displacement-work boundary when finite liquid volume and spray
   momentum are resolved. Keep delivered liquid, evaporated fuel and reaction
   separately observable and retain independent references.
2. Extend the engine with ignition control, intake/exhaust dynamics, mechanical
   losses and richer thermochemistry. Preserve the complete engine objective.
3. Extend DCT actuation and measured planetary/AT hydraulics, then coordinate ECU/TCU torque requests and shifts using the existing sampled DCT and AT controllers. Add measured sensors, actuators and recoverable faults.
4. Run `unity-test` with the pinned Editor, then obtain Player/IL2CPP evidence.
   Complete channel selection, graph editing and saving as distinct features.
5. Obtain measured maps, OEM data and uncertainty budgets for the two target
   powertrains before declaring calibrated samples or release readiness.
