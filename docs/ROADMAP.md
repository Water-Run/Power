# Power! development roadmap

**English** · [简体中文](ROADMAP.zh-CN.md) · [Français](ROADMAP.fr.md) · [Русский](ROADMAP.ru.md) · [日本語](ROADMAP.ja.md) · [한국어](ROADMAP.ko.md) · [Deutsch](ROADMAP.de.md) · [Español](ROADMAP.es.md) · [Italiano](ROADMAP.it.md) · [Português](ROADMAP.pt-BR.md)

The objective is the complete Power! powertrain platform: modern C# physics,
a Unity 3D studio and direct agent operation. A passing synthetic laboratory
establishes a bounded numerical result; engine, transmission, controls, vehicle
calibration and desktop acceptance each need their own evidence.

## Milestones

| Milestone | Available foundation | Work still required |
|---|---|---|
| Managed core | Dual-target dependency-free physics, topology, units, integer time, replay and atomic transactions | Long-run integrated powertrain validation |
| Agent interface | Schema-defined MCP tools, structured diagnostics, revisions, branches, cancellation and compact reports; `list-labs` / `get_capabilities.examples` | Modeling/control workflows for the remaining full-powertrain scope |
| Unity studio | Shared model import, laboratory playback, 3D schematic components and prepared tests | Actual Editor/Play/Player/IL2CPP evidence and desktop packaging |
| Modeling workbench | Portable asset v34, v1-v33 readers and shared JSON/CLI/MCP definitions | Graph editing, saving and selectable channel plots |
| Engine physics | Independent gas mass/energy, slider-crank work, timed valves, prescribed combustion, gaseous and liquid fuel metering, finite compliant rails, film evaporation and physical needle actuation; [Pump-fed liquid fuel rail](PUMP_FED_FUEL.md); finite tanks and conserving relief returns; [Geometric tank and finite headspace](TANK_HEADSPACE.md); [Fuel phase equilibrium and liquid supply limits](FUEL_PHASE_EQUILIBRIUM.md); [Finite-volume fuel tank equilibrium](TANK_PHASE_EQUILIBRIUM.md); [Frozen-quality fuel discharge and finite receivers](FUEL_DISCHARGE.md) | Resolve local inlet inertia and losses, phase-relaxation and slip sensitivity, then extend ignition, intake/exhaust, mechanical losses and coordinated ECU/TCU control. Validate each new equation against independent references and time refinement; obtain measured calibration and actual Unity acceptance separately. |
| Transmission | Coupled clutches, gears/planetaries, mapped converter/lockup, hydraulics and seven-forward/reverse DCT and four-forward/reverse Ravigneaux paths with resolved planet spin/orbital inertia | Mesh compliance/losses and load sharing, DCT actuation, complete AT pressure/shift control and measured routing, measured maps, valve/seal/cavitation behavior and richer converter dynamics |
| Controls and electrical integration | Sampled pressure PI, needle closure control and sensor-confirmed staged DCT handoff, bounded voltage/duty, actuator ownership, battery equivalent circuit and accessories; [`at_controller`](AT_CONTROL.md) | Coordinated ECU/TCU cycles, sensors/actuators, torque requests, faults, BMS and measured thermal/electrical behavior |
| Vehicle evidence and release | Research samples with complete boundaries and provenance | Two complete measured powertrains, uncertainty budgets, stability, desktop acceptance and distribution |

Current numerical checkpoints, authentic asset fixtures and platform-specific
verification records live in [VALIDATION.md](VALIDATION.md). See
[DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) for implementation state and
[ARCHITECTURE.md](ARCHITECTURE.md) for invariants. Published CI evidence applies to
its recorded revision; new local changes need separate platform acceptance.

## Next managed work

Connect the liquid momentum reference to the finite fuel network with an explicit line energy inventory and compatible port enthalpy. Establish variable-density and phase-change validity before claiming cavitating inlet dynamics. Continue phase relaxation, ignition, intake/exhaust, mechanical losses and coordinated ECU/TCU control. [Liquid inlet momentum reference](LIQUID_COLUMN.md)

Then extend ignition/control, intake/exhaust dynamics and engine mechanical
losses. The current Wiebe burn is prescribed and doesn't establish predictive
combustion, knock, emissions or OEM calibration. The
[gaseous metering contract](FUEL_METERING.md) remains an independent supported path.

Build on the [seven-forward/reverse DCT graph](DUAL_CLUTCH_TRANSMISSION.md) with
detailed synchronizer/dog/clutch actuation. Build on the [Ravigneaux graph](RAVIGNEAUX_TRANSMISSION.md)
with [measured planet properties and mesh behavior](RESOLVED_PLANETS.md), complete
[pump-fed piston actuation](AT_HYDRAULIC_ACTUATION.md) and AT control using the
coupled converter, gears, clutches and hydraulic primitives. Build on [sampled DCT control](DCT_CONTROL.md) toward torque-blended shifts and bounded ECU/TCU
coordination, including torque requests, sensors/actuators and recoverable faults.
Extend constant pump-loss, battery and valve/accumulator models when measured
property/control data is available; supplied research values remain unverified.

The fuel-domain pump now feeds a physical needle and frozen-quality discharge into a finite-liquid-volume fired receiver. Complete inventories include phase change, displacement work, jet kinetic energy and stationary-wall impulse. [Shaft-driven phase-resolving fuel pump](FUEL_CHAMBER_PUMP.md)

## Studio and measured acceptance

Set `POWER_UNITY_EDITOR` to the pinned Editor and run `unity-test`. Obtain actual
import/Play/rendering evidence and then Player/IL2CPP evidence. General channel
selection, graph editing and saving remain separate Studio features. CLI, MCP and
Unity must continue consuming the same model semantics.

EA211 DJS + DQ200 and PSA EC5 + AT8 retain full powertrain boundaries, vehicle
applicability and evidence manifests. Missing OEM measurements aren't replaced by
silent defaults. Functional completion, numerical correctness and measured vehicle
credibility require separate acceptance.

The [Zig archive](NATIVE_ZIG.md) retains original hashes and Git provenance in
`legacy/native/migration-manifest.json`, including original C revision `c342d4c`.
It remains separate from the active C#/Unity application. Native migration doesn't
complete managed feature migration or powertrain acceptance. Introduce additional
parallelism, sparse solving or Burst when measurements justify it and the core
contracts remain stable.

[Frozen-quality fuel discharge and finite receivers](FUEL_DISCHARGE.md)
