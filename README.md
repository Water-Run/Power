# Power!

Power! is a powertrain modeling and experimentation project built around a **cross-platform C# physics core, a Unity 3D studio, and agent-friendly MCP interfaces**. Models, solvers, experiments, and presentation have separate responsibilities. Agents can construct models, inspect diagnostics, branch experiments, and evaluate physical evidence through explicit contracts.

The public repository is [Water-Run/Power](https://github.com/Water-Run/Power). Development resumed on 2026-09-08; the implemented baseline and remaining work are recorded in the [roadmap](docs/ROADMAP.md).

## Technology

Release versions verified and pinned on 2026-09-07:

| Layer | Version and responsibility |
|---|---|
| Unity 3D | **Unity 6.6 / 6000.6.0f1**, desktop studio |
| Rendering, input, UI | **URP 17.6.0**, **Input System 1.20.0**, UI Toolkit |
| C# tooling | **.NET 10 SDK 10.0.400 / C# 14**, core, CLI, agent services, and build tools |
| Unity-facing assemblies | **.NET Standard 2.1**, compiled from the same core and asset source |
| Agent transport | Official **MCP C# SDK 2.2.0**, stdio, committed dependency lock files |

Sources: [Unity release notes](https://unity.com/releases/editor/whats-new/6000.6.0f1), [.NET 10 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), [MCP SDK](https://www.nuget.org/packages/ModelContextProtocol/2.2.0).

Unity's own compiler supports C# 9, with .NET Standard 2.1 as its default API profile. The external .NET SDK compiles modern C# into Unity-compatible assemblies; scripts inside `Unity/Assets` use C# 9 syntax. A Unity Player does not require a separate .NET 10 installation. See [Unity's compiler support](https://docs.unity3d.com/6000.6/Documentation/Manual/csharp-compiler.html) and [API compatibility documentation](https://docs.unity3d.com/6000.6/Documentation/Manual/dotnet-profile-support.html).

## Build and verify

Install the pinned SDK and run from the repository root on Windows, macOS, or Linux:

```sh
dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

This builds the solution serially, exports Unity model assets, runs the core and agent checks, exercises an actual MCP server process, and writes experiment reports under `artifacts/reports`. Build servers and concurrent compilation are disabled inside the build tool to reduce memory pressure. The local `.cache/dotnet/dotnet` executable can also be used when the pinned SDK is installed there; caches are excluded from Git.

The current increment passed **38/38 managed checks, 26/26 Unity-facing assembly checks, and 6/6 MCP integration groups on Windows, macOS, and Linux**. See the [CI run](https://github.com/Water-Run/Power/actions/runs/34176008291) and [validation record](docs/VALIDATION.md). The assembly checks run under .NET 10; actual Unity Editor, Play Mode, rendering, and IL2CPP validation remain pending.

Run an experiment directly:

```sh
dotnet run --project src/Power.Cli -c Release -- assets/labs/electrothermal.power.json --output artifacts/reports/electrothermal.json
```

CLI exit codes are `0` for a passing experiment, `2` for failed KPIs or replay checks, and `1` for invalid input or execution errors. Model documents specify units, fixed nanosecond ticks, input events, and KPI bounds. Reports include source hashes, model fingerprints, runtime information, fidelity, channels, replay evidence, and energy residuals.

## Unity studio

1. Run `dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- build`. This creates the Core and Assets assemblies in `Unity/Assets/Plugins` and three sample `.powerasset` files in `Unity/Assets/Generated/Resources`.
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

The current executable C# models include rotational inertia, elastic shafts with positive or negative ratios, RL DC motors, torque sources, thermal capacities, heat-conduction networks, and sealed adiabatic cylinders with slider-crank geometry. They share coupled integration and an energy ledger. All sample parameters are marked `unverified`.

The complete engine, intake, combustion, exhaust, DCT/AT, hydraulic, ECU/TCU, and calibrated powertrain objectives remain open. Earlier C prototypes and tests are preserved in [legacy/native](legacy/native/ARCHIVE.md); their functionality has not all been migrated to C#. Research for EA211 DJS + DQ200 and PSA EC5 + AT8 remains in [assets/samples](assets/samples), with its evidence and calibration boundaries intact.

Any future rewrite of the archived C portion will use **Zig**, following the owner's direction. The active implementation remains C#/.NET and Unity; the Zig migration boundary will be defined before that work starts.

See the [architecture](docs/ARCHITECTURE.md), [roadmap](docs/ROADMAP.md), and [validation record](docs/VALIDATION.md). Existing documents may retain their original language; new documentation and updates use English.

## License

Original Power! material is licensed under **GPL-3.0-or-later with the Unity Linking Exception**. Read [COPYING.NOTICE](COPYING.NOTICE), the unmodified [GPLv3 text](LICENSE), and the [exception](UNITY-LINKING-EXCEPTION.md) together.

The exception permits the specified Unity combination while keeping Power! and its modifications under GPL requirements. Unity and other third-party software retain their own licenses; the exception grants no rights held by their authors. See [third-party notices](THIRD_PARTY_NOTICES.md). Preserve the applicable license, copyright, and notice files when distributing source or binaries.
