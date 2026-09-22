# Power!

Power! is a powertrain modeling and experimentation project built around a **cross-platform C# physics core, a Unity 3D studio, and agent-friendly MCP interfaces**. Models, solvers, experiments, and presentation have separate responsibilities. Agents can construct models, inspect diagnostics, branch experiments, and evaluate physical evidence through explicit contracts.

The public repository is [Water-Run/Power](https://github.com/Water-Run/Power). The owner resumed the native migration on 2026-09-10. The archived native prototypes now use Zig; the managed application baseline and remaining full-powertrain work are recorded in the [roadmap](docs/ROADMAP.md).

## Technology

Managed versions were pinned on 2026-09-07; the separate native Zig toolchain was pinned on 2026-09-11:

| Layer | Version and responsibility |
|---|---|
| Unity 3D | **Unity 6.6 / 6000.6.0f1**, desktop studio |
| Rendering, input, UI | **URP 17.6.0**, **Input System 1.20.0**, UI Toolkit |
| C# tooling | **.NET 10 SDK 10.0.400 / C# 14**, core, CLI, agent services, and build tools |
| Unity-facing assemblies | **.NET Standard 2.1**, compiled from the same core and asset source |
| Agent transport | Official **MCP C# SDK 2.2.0**, stdio, committed dependency lock files |
| Native prototypes | **Zig 0.15.2**, separate research library with the preserved binary ABI |

Sources: [Unity release notes](https://unity.com/releases/editor/whats-new/6000.6.0f1), [.NET 10 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), [MCP SDK](https://www.nuget.org/packages/ModelContextProtocol/2.2.0).

Unity's own compiler supports C# 9, with .NET Standard 2.1 as its default API profile. The external .NET SDK compiles modern C# into Unity-compatible assemblies; scripts inside `Unity/Assets` use C# 9 syntax. A Unity Player does not require a separate .NET 10 installation. See [Unity's compiler support](https://docs.unity3d.com/6000.6/Documentation/Manual/csharp-compiler.html) and [API compatibility documentation](https://docs.unity3d.com/6000.6/Documentation/Manual/dotnet-profile-support.html).

## Build and verify

Install the pinned .NET SDK and Python 3.12 or newer, then install Zig and run from the repository root on Windows, macOS, or Linux (`python` on Windows):

```sh
python3 tools/InstallZig.py
dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

This builds the solution serially, exports Unity model assets, runs the core and agent checks, exercises an actual MCP server process, and verifies the Zig runtime, shared-library hosts, Python ABI and original numerical baseline. It writes experiment and migration reports under `artifacts/reports`. The source audit rejects C/C++ implementation files and headers, plus Lua source, bytecode and packages. Build servers and concurrent compilation are disabled inside the build tool to reduce memory pressure. The local `.cache/dotnet/dotnet` executable can also be used when the pinned SDK is installed there; caches are excluded from Git.

The published three-platform baseline passed **38/38 managed checks, 26/26 Unity-facing assembly checks, and 6/6 MCP integration groups on Windows, macOS, and Linux**; see the [CI run](https://github.com/Water-Run/Power/actions/runs/34176008291). The 2026-09-22 pump checkpoint passes **165/165 managed checks, 132/132 Unity-facing assembly checks and 15/15 MCP groups** locally on Linux. See the [validation record](docs/VALIDATION.md). The assembly checks run under .NET 10; actual Unity Editor, Play Mode, rendering, and IL2CPP validation remain pending.

Run an experiment directly:

```sh
dotnet run --project src/Power.Cli -c Release -- assets/labs/electrothermal.power.json --output artifacts/reports/electrothermal.json
```

CLI exit codes are `0` for a passing experiment, `2` for failed KPIs or replay checks, and `1` for invalid input or execution errors. Model documents specify units, fixed nanosecond ticks, input events, and KPI bounds. Reports include source hashes, model fingerprints, runtime information, fidelity, channels, replay evidence, and energy residuals.

## Unity studio

1. Run `dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- build`. This creates the Core and Assets assemblies in `Unity/Assets/Plugins` and twelve sample `.powerasset` files in `Unity/Assets/Generated/Resources`.
2. Add the repository's `Unity` directory to Unity Hub and select **6000.6.0f1**.
3. Allow package resolution and script import to finish. Initial project preparation generates URP and material assets.
4. Open `Assets/Scenes/PowerLab.unity`, or choose **Power > Open laboratory**, then enter Play Mode.

The scene code builds rotors, thermal nodes, connections, and input controls from the imported model. It supports pause, reset, and saved experiments with events applied at exact simulation ticks. The default electrothermal experiment runs a ten-second braking and recovery sequence; `ThermalNetwork.powerasset` contains a thermal exchange experiment with no external inputs. Use **Open in Studio** in a model asset's Inspector to select it.

`SealedCylinder.powerasset` adds a compression/expansion experiment and a schematic moving piston. Its gas state, crank torque and energy channels use the same model semantics as CLI and MCP. The [cylinder documentation](docs/SEALED_CYLINDER.md) records equations, solver limits and missing engine behavior.

Export another model after building:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll export assets/labs/electrothermal.power.json --name "My laboratory" --output Unity/Assets/Models/MyLaboratory.powerasset
```

The importer checks integrity, recompiles the model, and verifies its fingerprint. See the [asset format](docs/ASSET_FORMAT.md). Drag to orbit and scroll to zoom. Each `FixedUpdate` advances at most 2,000 complete ticks: 20 ms for the default model, or 14 ms for the 7 ms thermal model. Physics never reads rendering `deltaTime`; this presentation schedule does not guarantee wall-clock real-time operation for every tick size.

Set `POWER_UNITY_EDITOR` to the Editor executable and run:

```sh
dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- unity-test
```

This is the separate entry point for actual Editor and Play Mode checks. Unity has not been installed or exercised in the current development environment, and no validated Player build is available yet.

## Agent interface

After building, launch this executable as a client's stdio MCP server:

```sh
dotnet /absolute/path/to/Power/src/Power.Mcp/bin/Release/net10.0/Power.Mcp.dll
```

The service exposes twelve tools with input and output schemas: capabilities, model schema, example, validation, experiments, asset export, session creation, input changes, time advancement, snapshots, branching, and disposal. Protocol output uses stdout and logs use stderr. Agents can operate the headless core without controlling the Unity UI or calling a model provider inside the physics loop.

The [agent API](docs/AGENT_API.md) documents client configuration and operation sequences. The core provides `TryCompile`, discoverable channels, `Fork`, cancellation, and atomic rollback. The MCP workspace adds revision checks and compact reports.

## Scope

The current executable C# models include rotational inertia, elastic shafts with positive or negative ratios, RL DC motors, torque sources, thermal capacities, heat-conduction networks, sealed adiabatic cylinders, and open gas chambers with slider-crank pressure-work coupling, explicit 360/720-degree valve profiles, and prescribed premixed combustion with fuel/air/product transport. They share coupled integration and an energy ledger. All sample parameters are marked `unverified`.

`Power.Core` also provides validated [gas-exchange physics](docs/GAS_EXCHANGE.md) — an ideal gas, a finite volume tracked by independent mass and internal energy, and a compressible orifice with choked and subcritical flow. They participate in fixed and moving-volume Core gas networks with channels, heat links and transactional ledgers. JSON documents, portable assets and CLI/MCP now represent the same networks; see the checkpoint below.

Managed [clutches](docs/CLUTCH_NETWORK.md) now participate in the shaft, motor and
cylinder solve, with static/sliding capacities, internal engagement and reversal events,
thermal routing and transactional phase/heat state. JSON, asset v11 and CLI/MCP consume
the same component. The [exact constant-load pair](docs/CLUTCH_PHYSICS.md) provides an
independent verification reference.

[Ideal gear and planetary constraints](docs/GEAR_NETWORK.md) now share the motor,
cylinder and clutch solve, with reaction channels, initial-speed validation and phase
preservation. JSON, asset v11 and MCP support the same topology. Independent
[constant-load references](docs/IDEAL_GEARS.md) verify motion and reaction forces.

[Mapped torque converters](docs/CONVERTER_NETWORK.md) now couple explicit forward/reverse
pump and coast characteristics to the same solve, with stationary-stator reactions,
fluid heat and an independent lockup clutch. Asset v11 and MCP retain the four signed
maps. The synthetic `fired-converter` laboratory replays all 87 boundaries through
engine firing, lockup and planetary shifts. The [hydraulic network](docs/HYDRAULIC_NETWORK.md) now supplies pressure from explicit
valve flow and compliance to shift and lockup clutches, with volume, work and heat ledgers.
The `fired-hydraulic` example replays 89 boundaries. Pump/regulator dynamics, piston travel,
complete gearbox topology and automatic-transmission control remain open.

A synthetic premixed fired cycle is implemented. Complete engine behavior, intake/exhaust, fuel metering, richer thermochemistry, DCT/AT, hydraulics, ECU/TCU and calibrated powertrains remain open. Earlier native prototypes and tests have been ported to Zig in [legacy/native](legacy/native/README.md); their functionality has not all been migrated to C#. All 38 C source/header files were replaced, with original hashes and Git provenance in the [migration manifest](legacy/native/migration-manifest.json). Research for EA211 DJS + DQ200 and PSA EC5 + AT8 remains in [assets/samples](assets/samples), with its evidence and calibration boundaries intact.

The [native Zig boundary](docs/NATIVE_ZIG.md) retains the versioned binary ABI without introducing a native dependency into the C#/Unity application. Native migration completion is separate from full powertrain functionality, vehicle calibration and Unity Editor validation.

See the [development status](docs/DEVELOPMENT_STATUS.md), [architecture](docs/ARCHITECTURE.md), [roadmap](docs/ROADMAP.md), and [validation record](docs/VALIDATION.md). Existing documents may retain their original language; new documentation and updates use English.

## License

Original Power! material is licensed under **GPL-3.0-or-later with the Unity Linking Exception**. Read [COPYING.NOTICE](COPYING.NOTICE), the unmodified [GPLv3 text](LICENSE), and the [exception](UNITY-LINKING-EXCEPTION.md) together.

The exception permits the specified Unity combination while keeping Power! and its modifications under GPL requirements. Unity and other third-party software retain their own licenses; the exception grants no rights held by their authors. See [third-party notices](THIRD_PARTY_NOTICES.md). Preserve the applicable license, copyright, and notice files when distributing source or binaries.

## Finite gas network integration

Fixed-volume gas chambers, reservoir orifices, controlled restrictions and gas-to-wall
heat links now work through JSON, CLI, MCP and `power.asset.v11`. Asset v1–v10 readers
remain supported. Mass, internal energy and reservoir ledgers participate in stepping,
snapshots, forks and whole-batch rollback. See the [gas contract](docs/GAS_NETWORK.md)
and [gas-network laboratory](assets/labs/gas-network.power.json).

Use `get_example_model` with `name: "gas-network"`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/gas-network.power.json --output artifacts/reports/gas-network.json
```

The build generates `GasNetwork.powerasset`; Studio includes schematic vessels and
connections with the existing input controls and output list. Actual Editor/Play
verification remains pending. Complete engine behavior and
vehicle calibration remain open; synthetic parameters are `unverified`.

## Moving-cylinder motoring experiment

`gas_cylinder` now couples a gas chamber's independent mass and internal energy to
crank-dependent volume and conservative pressure work. Restrictions admit and expel gas;
wall links exchange heat with thermal nodes. JSON, CLI, MCP, asset v11 and Studio consume
the same definitions. The [moving-cylinder contract](docs/MOVING_CYLINDER.md) records
equations, solver bounds, convergence evidence and missing engine behavior.

Use `get_example_model` with `name: "moving-cylinder"`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/moving-cylinder.power.json --output artifacts/reports/moving-cylinder.json
```

The build generates `MovingCylinder.powerasset`. Its time-scheduled openings support a
replayable motoring benchmark; they do not implement crank-angle valve timing or
combustion. Unity import, moving-piston and lifecycle tests are prepared but still need
actual Editor execution.

## Crank-timed motoring experiment

Optional [valve timing](docs/VALVE_TIMING.md) gives gas restrictions explicit 360/720-degree
profiles tied to actual crank angle. Peak-opening inputs and effective-opening outputs
remain discoverable through the same channel contract. Under-resolved lobes reject the
whole batch with timestep recovery guidance.

Request `get_example_model` with `name: "crank-timed-cylinder"`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/crank-timed-cylinder.power.json --output artifacts/reports/crank-timed-cylinder.json
```

The build exports `CrankTimedCylinder.powerasset`. Its 720-degree intake/exhaust cycle
tracks changing speed, with exact replay across 63 report boundaries. Studio includes
schematic opening markers and pending Editor/Play tests. Parameters remain `unverified`;
complete engine behavior, transmissions, controls and calibration remain open.

## Premixed fired-cylinder experiment

The [premixed combustion contract](docs/PREMIXED_COMBUSTION.md) adds transported fuel,
fresh air and inert products, limiting-reactant consumption, a prescribed Wiebe burn,
and chemical-energy accounting. Heat release participates in the crank solve. Asset v11
preserves these definitions and retains v1–v9 readers.

Request `get_example_model` with `name: "fired-cylinder"`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-cylinder.power.json --output artifacts/reports/fired-cylinder.json
```

The build exports `FiredCylinder.powerasset`. Its synthetic cylinder delivers work to a
load while tracking fuel and total energy, with 63 matching replay boundaries. The model
uses constant R/gamma and a prescribed burn; detailed chemistry, fuel metering, ignition
control and calibration remain open. Studio's heat-release marker and lifecycle tests
still require actual Editor execution.

## Fired engine and clutch experiment

The [fired-clutch laboratory](assets/labs/fired-clutch.power.json) drives a separate
inertial load through a controlled dry clutch. Engagement, release and re-engagement
produce tracked friction heat in a thermal node. All 67 report boundaries match portable
and MCP replay. Over 0.6 seconds it records about -96.75 J of net external work and generates
191.56 J of clutch heat, with a final energy residual of about 1.79e-10 J. Parameters
are synthetic and remain `unverified`.

Request `get_example_model` with `name: "fired-clutch"`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-clutch.power.json --output artifacts/reports/fired-clutch.json
```

The build exports `FiredClutch.powerasset`. Studio includes schematic clutch plates and
phase colors, with prepared import/lifecycle tests. Actual Unity verification remains
pending. Complete DCT/AT topology, pump/regulator and piston dynamics,
controls and calibrated vehicles remain open.

## Fired planetary transmission experiment

The [fired-planetary laboratory](assets/labs/fired-planetary.power.json) adds an ideal
planetary set and final drive to the synthetic engine. A ring brake and sun/ring clutch
select reduction and direct drive, with an upshift and downshift over 0.8 s. All **84
boundaries** match portable and MCP replay. The experiment records about **-56.83 J**
of net external work and generates **410.84 J** of combined shift heat, with a final energy
residual of about **2.51e-10 J**. Parameters remain `unverified`.

Request `get_example_model` with `name: "fired-planetary"`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-planetary.power.json --output artifacts/reports/fired-planetary.json
```

The build exports `FiredPlanetary.powerasset`. Studio's schematic gear views and import/
shift/reset tests are prepared; actual Unity verification remains pending. See
[the coupled gear contract](docs/GEAR_NETWORK.md) for equations, outputs and limits.
Complete DCT/AT, hydraulic hardware dynamics, controls, engine behavior and calibration remain open.

## Fired converter and lockup experiment

The [fired-converter laboratory](assets/labs/fired-converter.power.json) adds a mapped
fluid path and separate lockup clutch ahead of the planetary transmission. Four explicit
signed maps produce pump/turbine/stator reactions and tracked fluid heat. The scheduled
lockup, release, recapture and shifts replay all **87 boundaries** exactly. Parameters
remain synthetic and `unverified`.

Request `get_example_model` with `name: "fired-converter"`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-converter.power.json --output artifacts/reports/fired-converter.json
```

The build exports `FiredConverter.powerasset`. The [converter contract](docs/CONVERTER_NETWORK.md)
records equations, analytic/convergence evidence, map restrictions and numerical limits.
Studio fluid-port views and lifecycle tests are prepared; actual Unity verification is
pending. Complete transmission topology, hydraulic hardware dynamics, ECU/TCU coordination,
engine behavior and measured calibration remain open.

## Fired hydraulic transmission experiment

The [fired-hydraulic laboratory](assets/labs/fired-hydraulic.power.json) operates shift
and lockup clutches through three compliant pressure chambers and six explicit valves.
Opening commands produce pressure transients; piston area, preload, friction and radius
set clutch capacity. Reservoir work, reference-volume inventory and restriction heat
join the existing physical ledgers. All **89 boundaries** match portable and MCP replay.

Request `get_example_model` with `name: "fired-hydraulic"`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-hydraulic.power.json --output artifacts/reports/fired-hydraulic.json
```

The build exports `FiredHydraulic.powerasset`. See [the hydraulic contract](docs/HYDRAULIC_NETWORK.md)
for equations, assumptions, transient evidence and numerical limits. Fixed reservoirs
supply external hydraulic work; pump/regulator and piston dynamics, full transmission
controls, actual Unity execution and measured calibration remain unfinished.

## Fired pump supply experiment

The [fired-pump laboratory](assets/labs/fired-pump.power.json) drives the hydraulic
supply from the crank through an ideal displacement pump, compliant line and pressure
relief. Shaft speed and pressure reaction are solved together with the converter,
cylinders and pressure-operated clutches. All 89 replay boundaries agree; pump work
is 53.94 J and external hydraulic work is zero. Parameters remain unverified.

Request `get_example_model` with `name: "fired-pump"`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-pump.power.json --output artifacts/reports/fired-pump.json
```

The build exports `FiredPump.powerasset`. See [the pump contract](docs/HYDRAULIC_PUMP.md)
and [development status](docs/DEVELOPMENT_STATUS.md). Development is paused at this
verified checkpoint at the owner's request. Full powertrain and Unity acceptance remain open.
