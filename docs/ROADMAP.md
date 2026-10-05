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
| Agent interface | Schema-defined MCP tools, structured diagnostics, revisions, branches, cancellation and compact reports | Modeling/control workflows for the remaining full-powertrain scope |
| Unity studio | Shared model import, laboratory playback, 3D schematic components and prepared tests | Actual Editor/Play/Player/IL2CPP evidence and desktop packaging |
| Modeling workbench | Portable asset v26, v1-v25 readers and shared JSON/CLI/MCP definitions | Graph editing, saving and selectable channel plots |
| Engine physics | Independent gas mass/energy, slider-crank work, timed valves, prescribed combustion, gaseous and liquid fuel metering, finite compliant rails, film evaporation and physical needle actuation; [Pump-fed liquid fuel rail](PUMP_FED_FUEL.md) | finite fuel tank and measured pump regulation, refined magnetic/electronic/spray behavior, finite-liquid-volume coupling, ignition control, detailed intake/exhaust, mechanical losses, thermochemistry and measured calibration |
| Transmission | Coupled clutches, gears/planetaries, mapped converter/lockup, hydraulics and seven-forward/reverse DCT and four-forward/reverse Ravigneaux paths with resolved planet spin/orbital inertia | Mesh compliance/losses and load sharing, DCT actuation, complete AT pressure/shift control and measured routing, measured maps, valve/seal/cavitation behavior and richer converter dynamics |
| Controls and electrical integration | Sampled pressure PI, needle closure control and sensor-confirmed staged DCT handoff, bounded voltage/duty, actuator ownership, battery equivalent circuit and accessories | Coordinated ECU/TCU cycles, sensors/actuators, torque requests, faults, BMS and measured thermal/electrical behavior |
| Vehicle evidence and release | Research samples with complete boundaries and provenance | Two complete measured powertrains, uncertainty budgets, stability, desktop acceptance and distribution |

Current numerical checkpoints, authentic asset fixtures and platform-specific
verification records live in [VALIDATION.md](VALIDATION.md). See
[DEVELOPMENT_STATUS.md](DEVELOPMENT_STATUS.md) for implementation state and
[ARCHITECTURE.md](ARCHITECTURE.md) for invariants. Published CI evidence applies to
its recorded revision; new local changes need separate platform acceptance.

## Next managed work

Continue the pump-fed rail with a finite fuel tank, measured pump regulation and refined magnetic/electronic actuation. Finite compliant source mass
and pressure energy, actual needle motion, sampled dose feedback, film replenishment
and evaporation are implemented. See [the needle contract](NEEDLE_ACTUATION.md) and [bounded closure prediction](CLOSURE_PREDICTION.md). The
receiver still exports displacement pressure work under the declared negligible
liquid volume boundary; resolved spray/displacement must replace it with verified
geometry and momentum/work coupling. Keep delivery, vapor availability and
prescribed reaction separate, and retain analytic/conservation/convergence evidence. [Pump-fed liquid fuel rail](PUMP_FED_FUEL.md)

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
