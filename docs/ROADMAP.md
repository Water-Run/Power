# Power! development roadmap

The objective is the complete Power! powertrain platform: modern C# physics, a Unity
3D studio and direct agent operation. Passing a laboratory experiment does not complete
the engine, transmission, controls or vehicle calibration milestones.

**2026-09-30: finite-energy gas accumulator.** Linear gas geometry and actual
mass/energy states now couple to hydraulic separator motion. Analytic work/Jacobian,
independent RK4/refinement, gas transport/heat, recovery and complete transactions
pass locally. Asset v16 and actual MCP retain the same model; the prior published
spool source passes three-platform CI. Current gas physics and actual Unity need
their own acceptance. Development is active after the owner's resume request. See
[the gas/fluid contract](GAS_PISTON.md).

**2026-09-30: mechanical spool regulation.** Actual piston motion meters a passive
turbulent bypass in the shared pressure/motion solve. Steady and independent RK4
checks, contact refinement, flow/motion energy, rollback and allocation bounds pass.
Asset v15 and actual MCP retain geometry. The owner resumed after that checkpoint; complete powertrain and actual Unity
acceptance remain unfinished.
See [the spool contract](HYDRAULIC_SPOOL.md).

**2026-09-30: dynamic piston and contact clutch.** Translational mass, swept fluid
volume, return spring/damping, compliant ends and pad clearance now participate in
the joint solve. Clutch capacity follows contact force, with independent conservation
and contact-refinement checks. Asset v14 and actual MCP replay retain the full
topology. Studio views and tests are prepared; actual Unity, complete DCT/AT control,
seal friction, cavitation, wear and measured calibration remain open. See
[the piston contract](HYDRAULIC_PISTON.md).

**2026-09-30: finite battery supply.** Affine OCV/SOC, series resistance and RC
polarization now feed duty-controlled RL motors and switched accessory loads in the
coupled solve. Duty PI pressure control preserves charge/control transactions and
regeneration. Asset v13 and actual MCP replay retain all parameters; the synthetic
battery-regulated pump has 761 matching boundaries. Battery chemistry/BMS, PWM/current
control, full electrical integration and calibration remain open. See
[the supply contract](HYDRAULIC_PUMP.md#finite-battery-supply-and-duty-regulation).

**2026-09-30: sampled pressure feedback.** A PI loop now owns motor voltage and
reads hydraulic pressure on an integer sampling clock. Voltage limits, clamping
anti-windup, control-state transactions, asset v12 and actual MCP replay are verified
on Windows. The regulated-pump laboratory has 757 matching boundaries; complete
ECU/TCU, actuator mechanics and calibration remain open. See
[the feedback contract](HYDRAULIC_PUMP.md#sampled-pressure-regulation).

**2026-09-30: development resumed with pump losses and electrical supply.** Explicit
pressure leakage, viscous shaft drag and an RL motor supply use the shared conserving
graph and asset v11. The fired-loss and electric-pump laboratories have 89 and 106
matching JSON/portable/MCP boundaries. Windows serial verification passes; new Linux/
macOS and actual Unity evidence remain pending. Historical fixture hashes are retained
as provenance, with same-runtime exact replay and portable physical reference checks.
See [the pump assembly contract](HYDRAULIC_PUMP.md#explicit-leakage-shaft-friction-and-electrical-supply).

**2026-09-22 closing checkpoint: shaft-driven hydraulic supply.** Ideal displacement
pumps and finite-conductance relief now share the crank/converter/pressure-clutch solve.
The fired-pump laboratory replays 89 boundaries with zero external hydraulic supply
work. JSON/MCP and asset v11 retain complete power and volume ledgers. Development is
paused at that checkpoint; the owner resumed it on 2026-09-30. See [the pump contract](HYDRAULIC_PUMP.md).

**2026-09-22: hydraulic network and pressure-operated clutches.** Compliant gauge-pressure
chambers, linear/turbulent valve flow and pressure-derived friction capacity now share
transactional volume/work/heat accounting with the fired powertrain. The new hydraulic
laboratory replays 89 boundaries, and asset v10 retains v1–v9 readers. Fixed reservoirs
are external power boundaries; pumps, regulators, piston dynamics and controls remain
open. See [the hydraulic contract](HYDRAULIC_NETWORK.md).

**Earlier on 2026-09-22: coupled mapped torque converter.** Four explicit signed maps, stationary
stator reaction and fluid heat now share the cylinder, clutch and gear solve. Separate
lockup, release and scheduled planetary shifts replay 87 boundaries in the synthetic
fired-converter laboratory. Asset v9 retains v1–v8 readers; JSON/MCP contracts and
prepared Studio views are integrated. See [the converter contract](CONVERTER_NETWORK.md).

**Earlier on 2026-09-22: coupled ideal transmission.** Permanent gear and planetary constraints
now share the motor, cylinder and clutch solve. Reaction outputs, phase preservation,
initial-speed/rank diagnostics and complete rollback are verified through JSON, asset v8
and MCP. The fired planetary laboratory replays all 84 boundaries through an upshift and
downshift; Studio views/tests are prepared. See [the gear contract](GEAR_NETWORK.md).

**Earlier on 2026-09-22: ideal transmission references.** `IdealGearPair` and
`SimplePlanetaryGear` provide immutable constant-load solutions, signed speed ratios,
member reactions and work/energy evidence. Eight groups run against each Core target,
including independent planetary dynamics, held-member/direct-drive limits, refinement,
range and allocation checks. At that checkpoint these were standalone references; the subsequent increments
added graph constraints, shared document contracts and shift events. See
[the gear contract](IDEAL_GEARS.md).

**Earlier on 2026-09-22: coupled managed clutches.** Static/kinetic clutch reactions now share the
shaft, motor and cylinder solve, with internal engagement/reversal events, thermal
routing and whole-batch rollback. The fired-clutch laboratory drives a separate load;
all 67 boundaries match JSON/CLI/MCP and portable replay. Asset v7 retains v1–v6 readers.
Studio views/tests are prepared; actual Editor evidence is pending. See
[the graph contract](CLUTCH_NETWORK.md) and [the independent reference](CLUTCH_PHYSICS.md).

**Earlier on 2026-09-22: premixed fired cycle.** Fuel, fresh air and products now flow through gas
ports, and a prescribed Wiebe burn converts chemical energy into gas heat and crank work.
The fired cylinder drives an external load, with constituent and total-energy ledgers.
JSON/schema, CLI/MCP and asset v6 share the definitions; v1–v5 readers remain.
The fired example has exact replay across 63 report boundaries. Unity views/tests
are prepared, while actual Editor verification remains pending. See the
[development status](DEVELOPMENT_STATUS.md), [combustion contract](PREMIXED_COMBUSTION.md)
and [validation record](VALIDATION.md).

| Milestone | Acceptance scope | Current status |
|---|---|---|
| 1. Managed foundation | Dual-target Core, topology, units, bounded time, replay, rollback and evidence | Implemented; published baseline passed Windows/macOS/Linux. Current physics has local Windows evidence; the preceding source has Linux CI evidence |
| 2. Agent interface | Schemas, structured diagnostics, MCP, branches, revision conflicts, cancellation and compact reports | Implemented; actual MCP process tests include fired transmission discovery, validation, experiment and export |
| 3. Unity studio | Real import, Play lifecycle, 3D laboratory, input/UI and desktop Player | Project and tests prepared, including hydraulic/lockup/shift views; Editor/Player acceptance pending |
| 4. Modeling workbench | Shared model assets, graph editing, channel configuration, saving and replay | Portable v16 and v1–v15 readers verified; graph editing/saving and selectable plots pending |
| 5. Engine physics | Slider-crank, independent mass/energy, intake/exhaust, combustion, pumping, wall transfer and cylinder cycle | Fixed/moving gas volumes, timed valves and premixed fired cycles implemented. Fuel metering, ignition control, richer thermochemistry, detailed intake/exhaust and calibrated engine behavior pending |
| 6. Transmission | Clutches, DCT/AT, gears/planetaries, torque converter, hydraulics, heat and hybrid events | Coupled clutches, ideal gears/planetaries, mapped converters, thermal routing and hybrid rollback verified, including fired lockup and shifts. Pressure/flow networks now operate the clutches. Shaft pumps, explicit leakage/viscous losses, RL electric supply, finite-charge battery, duty regulation and quasi-steady relief are verified. Complete DCT/AT, measured loss/control maps, measured valve/accumulator and richer converter dynamics remain pending. Dynamic piston/contact actuation is verified as a lumped research model. Historical Zig prototypes remain reference |
| 7. Controls and integration | ECU/TCU cycles, sensors/actuators, torque coordination, power supply, accessories and faults | Sampled voltage/duty pressure PI feedback, finite battery supply/accessories and explicit actuator ownership implemented, including transactional controller state. Complete ECU/TCU coordination, sensors/actuators, supply/accessories and fault handling remain incomplete |
| 8. Evidence and release | Two complete powertrains, measured calibration, provenance, error budgets, long-run stability and desktop packaging | Research boundaries preserved; release criteria unmet |

## Next managed increment

Extend constant pump-loss models and RL electrical supply with measured loss/control
maps, measured battery/BMS/thermal behavior and measured regulation behavior. Retain explicit
reservoirs, power/volume/energy ledgers and actuator limits. Extend verified piston/contact actuation with measured valve and accumulator property/heat behavior required by complete DCT/AT power paths.
Build on the coupled converter/gear/clutch solver and independent physical references, retaining
conservation, bounded events, complete rollback and shared JSON/asset/agent semantics.
A converter, lockup clutch and scheduled planetary shift do not complete gearbox,
controller or powertrain acceptance.

The engine now has a synthetic premixed fired cycle. Continue fuel-metering and ignition
control, intake/exhaust dynamics, mechanical losses, richer thermochemistry and measured
calibration as separate engine work. The present prescribed burn is not a predictive
combustion or emissions model; none of these remaining requirements is waived.

The [moving-cylinder contract](MOVING_CYLINDER.md) records the conservative split
method now implemented. The [engine-step notes](NEXT_ENGINE_STEP.md) retain the broader
scope, while the [gas-network contract](GAS_NETWORK.md) preserves the earlier fixed-volume
method and its unchanged fingerprints.

Unity verification can proceed separately when the pinned Editor is available. Run
`unity-test` with `POWER_UNITY_EDITOR`, then obtain Player/IL2CPP evidence. Studio plots
currently show the first two rotor speeds; all other channels are listed numerically.
Channel selection, graph editing and saving are still distinct deliverables. CLI, MCP
and Unity must continue consuming the same model semantics.

## Retained checkpoints and boundaries

- **2026-09-22, preceding integration:** fixed-volume gas networks gained JSON, MCP and
  asset v3 support before the subsequent moving-cylinder/v4 increment.
- **2026-09-14:** finite gas volumes, restrictions, thermal links and transactional
  conservation ledgers validated through the Core API. The earlier standalone gas
  primitives and this compiled checkpoint run against both Core target assemblies.
- **2026-09-11:** the owner-authorized native migration completed in Zig 0.15.2 after
  resuming on September 10. The original C source remains in Git at `c342d4c`; the
  [migration manifest](../legacy/native/migration-manifest.json) records hashes and
  provenance. The [native boundary](NATIVE_ZIG.md) introduces no native dependency into
  the C#/Unity application. Migration completion is separate from powertrain completion.
- The published managed/cylinder baseline passed [three-platform verification](https://github.com/Water-Run/Power/actions/runs/34176008291).
  New local evidence does not establish current Windows/macOS or Unity results.

Agent work should advance topology, equations, experiment design, parameter identification
and evidence analysis. Introduce worker threads, sparse solvers or Burst only when actual
benchmarks justify them and the contracts remain stable.

EA211 DJS + DQ200 and PSA EC5 + AT8 must retain their full powertrain boundaries,
vehicle applicability and evidence status. Missing OEM measurements stay missing;
research parameters remain `unverified`. Functional completion, numerical correctness
and measured vehicle credibility require separate acceptance.
