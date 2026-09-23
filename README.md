# Power!

Power! is a powertrain modeling and experimentation project: a cross-platform C# physics core, a Unity 3D studio, and agent-facing MCP interfaces. Models, solvers, experiments, and presentation are separate concerns, so agents can build models, run and branch experiments, and inspect physical evidence through explicit contracts.

The public repository is [Water-Run/Power](https://github.com/Water-Run/Power).

## Technology

| Layer | Version and responsibility |
|---|---|
| Unity 3D | **Unity 6.6 / 6000.6.0f1**, desktop studio |
| Rendering, input, UI | **URP 17.6.0**, **Input System 1.20.0**, UI Toolkit |
| C# tooling | **.NET 10 SDK 10.0.400 / C# 14**, core, CLI, agent services, build tools |
| Unity-facing assemblies | **.NET Standard 2.1**, compiled from the same core and asset source |
| Agent transport | Official **MCP C# SDK 2.2.0**, stdio, committed dependency lock files |
| Native prototypes | **Zig 0.15.2**, separate research library with the preserved binary ABI |

Sources: [Unity release notes](https://unity.com/releases/editor/whats-new/6000.6.0f1), [.NET 10 downloads](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), [MCP SDK](https://www.nuget.org/packages/ModelContextProtocol/2.2.0).

Unity's own compiler supports C# 9 with .NET Standard 2.1 as its API profile. The external .NET SDK compiles modern C# into Unity-compatible assemblies, and scripts inside `Unity/Assets` use C# 9 syntax. A Unity Player doesn't need a separate .NET 10 installation. See [Unity's compiler support](https://docs.unity3d.com/6000.6/Documentation/Manual/csharp-compiler.html) and [API compatibility documentation](https://docs.unity3d.com/6000.6/Documentation/Manual/dotnet-profile-support.html).

## Build and verify

Install the pinned .NET SDK, then install Zig and run from the repository root on Windows, macOS, or Linux:

```sh
dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- install-zig
dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

`verify` builds the solution serially, exports Unity model assets, runs the core and agent checks, exercises an actual MCP server process, and verifies the Zig runtime, shared-library hosts, the C# P/Invoke ABI, and the original numerical baseline. Reports land under `artifacts/reports`. A pinned SDK installed at `.cache/dotnet/dotnet` works too; caches aren't tracked by Git.

> The source audit rejects C/C++ implementation files and headers, plus Lua source, bytecode, and packages. Keep the repository free of them.

Managed checks pass on Windows, macOS, and Linux; the record is [docs/VALIDATION.md](docs/VALIDATION.md). Unity Editor, Play Mode, rendering, and IL2CPP validation remain pending — see [Unity verification](#unity-studio) below. Development is paused at this verified checkpoint at the owner's request.

Run an experiment directly:

```sh
dotnet run --project src/Power.Cli -c Release -- assets/labs/electrothermal.power.json --output artifacts/reports/electrothermal.json
```

CLI exit codes are `0` for a passing experiment, `2` for failed KPIs or replay checks, and `1` for invalid input or execution errors.

Model documents specify units, fixed nanosecond ticks, input events, and KPI bounds. Reports include source hashes, model fingerprints, runtime information, fidelity, channels, replay evidence, and energy residuals.

## Unity studio

1. Run `dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- build`. This creates the Core and Assets assemblies in `Unity/Assets/Plugins` and twelve sample `.powerasset` files in `Unity/Assets/Generated/Resources`.
2. Add the repository's `Unity` directory to Unity Hub and select **6000.6.0f1**.
3. Let package resolution and script import finish — first-time preparation generates URP and material assets.
4. Open `Assets/Scenes/PowerLab.unity`, or choose **Power > Open laboratory**, then enter Play Mode.

The scene builds rotors, thermal nodes, connections, and input controls from the imported model. It supports pause, reset, and saved experiments with events applied at exact simulation ticks. The default electrothermal experiment runs a ten-second braking and recovery sequence; `ThermalNetwork.powerasset` is a thermal-exchange experiment with no external inputs. Use **Open in Studio** in a model asset's Inspector to select it.

`SealedCylinder.powerasset` adds a compression/expansion experiment with a schematic moving piston; its gas-state, crank-torque, and energy channels use the same model semantics as CLI and MCP. See the [cylinder documentation](docs/SEALED_CYLINDER.md).

Export another model after building:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll export assets/labs/electrothermal.power.json --name "My laboratory" --output Unity/Assets/Models/MyLaboratory.powerasset
```

The importer checks integrity, recompiles the model, and verifies its fingerprint — see the [asset format](docs/ASSET_FORMAT.md). Drag to orbit and scroll to zoom. Each `FixedUpdate` advances at most 2,000 complete ticks: 20 ms for the default model, 14 ms for the 7 ms thermal model. Physics doesn't read rendering `deltaTime`, so very fine tick models aren't guaranteed to keep wall-clock real time.

## Unity verification

Unity Editor and Play Mode checks are a separate entry point. Set `POWER_UNITY_EDITOR` to the Editor executable and run:

```sh
dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- unity-test
```

This is the only path that counts as actual Editor/Play Mode evidence. Unity hasn't been installed or exercised in the current development environment, and no validated Player build is available yet.

## Agent interface

After building, launch the server as a client's stdio MCP process:

```sh
dotnet /absolute/path/to/Power/src/Power.Mcp/bin/Release/net10.0/Power.Mcp.dll
```

The service exposes twelve tools with input and output schemas: capabilities, model schema, example, validation, experiments, asset export, session creation, input changes, time advancement, snapshots, branching, and disposal. Protocol output uses stdout; logs use stderr. Agents operate the headless core without driving the Unity UI or calling a model provider inside the physics loop.

The [agent API](docs/AGENT_API.md) documents client configuration and operation sequences. The core provides `TryCompile`, discoverable channels, `Fork`, cancellation, and atomic rollback; the MCP workspace adds revision checks and compact reports.

## Models and laboratories

The executable C# models today cover rotational inertia, elastic shafts with positive or negative ratios, RL DC motors, torque sources, thermal capacities, heat-conduction networks, sealed adiabatic cylinders, and open gas chambers with slider-crank pressure-work coupling, crank-timed 360/720-degree valve profiles, and prescribed premixed combustion with fuel/air/product transport. Validated [gas-exchange physics](docs/GAS_EXCHANGE.md) — ideal gas, finite volume tracked by independent mass and internal energy, and a compressible orifice with choked and subcritical flow — feeds fixed and moving-volume gas networks. Clutches with static/sliding capacities, ideal gear and planetary constraints, mapped torque converters, and a hydraulic network with explicit valves, compliance, and a crank-driven pump supply join the same coupled solve. These models share coupled integration and an energy ledger. All sample parameters are `unverified` — research values, not calibrated measurements.

Every laboratory below runs from the same definitions through JSON, CLI, MCP, and the Unity studio (asset `power.asset.v11`; readers for asset v1–v10 remain supported):

| Example name (`get_example_model`) | Laboratory | What it exercises |
|---|---|---|
| `electrothermal` | `assets/labs/electrothermal.power.json` | default braking/recovery sequence |
| `gas-network` | `assets/labs/gas-network.power.json` | fixed-volume chambers, orifices, wall heat links |
| `moving-cylinder` | `assets/labs/moving-cylinder.power.json` | motoring with crank-dependent volume |
| `crank-timed-cylinder` | `assets/labs/crank-timed-cylinder.power.json` | 720° intake/exhaust profiles at changing speed |
| `fired-cylinder` | `assets/labs/fired-cylinder.power.json` | premixed burn with fuel/air/product transport |
| `fired-clutch` | `assets/labs/fired-clutch.power.json` | dry clutch engagement, release, re-engagement |
| `fired-planetary` | `assets/labs/fired-planetary.power.json` | planetary set and ring brake shifting |
| `fired-converter` | `assets/labs/fired-converter.power.json` | converter maps and scheduled lockup |
| `fired-hydraulic` | `assets/labs/fired-hydraulic.power.json` | valve-fed pressure for shift/lockup clutches |
| `fired-pump` | `assets/labs/fired-pump.power.json` | crank-driven pump, compliant line, relief |

Request `get_example_model` with a `name`, or run one directly:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/<name>.power.json --output artifacts/reports/<name>.json
```

The build exports a matching `.powerasset` for each laboratory. Replay evidence — matched report boundaries, work and heat totals, energy residuals — is recorded in [docs/VALIDATION.md](docs/VALIDATION.md) and the contract documents: [gas network](docs/GAS_NETWORK.md), [moving cylinder](docs/MOVING_CYLINDER.md), [valve timing](docs/VALVE_TIMING.md), [premixed combustion](docs/PREMIXED_COMBUSTION.md), [clutches](docs/CLUTCH_NETWORK.md) with an [independent constant-load reference](docs/CLUTCH_PHYSICS.md), [gears](docs/GEAR_NETWORK.md) with [constant-load references](docs/IDEAL_GEARS.md), [converter](docs/CONVERTER_NETWORK.md), [hydraulics](docs/HYDRAULIC_NETWORK.md), and [pump](docs/HYDRAULIC_PUMP.md).

Still open: complete engine behavior (intake/exhaust modeling, fuel metering, richer thermochemistry, ignition control), complete DCT/AT topology and transmission controls (ECU/TCU), pump and regulator dynamics, hydraulic piston travel, and calibrated powertrains. Earlier native prototypes and tests are ported to Zig in [legacy/native](legacy/native/README.md) as a separate research library; their functionality hasn't all been migrated to C#. The original C sources were replaced by Zig ports, with original hashes and Git provenance in [legacy/native/migration-manifest.json](legacy/native/migration-manifest.json). The [native Zig boundary](docs/NATIVE_ZIG.md) keeps the versioned binary ABI without adding a native dependency to the C#/Unity application. OEM research for EA211 DJS + DQ200 and PSA EC5 + AT8 stays in [assets/samples](assets/samples), with its evidence and calibration boundaries intact.

## Documentation

- [Architecture](docs/ARCHITECTURE.md) — core structure and invariants
- [Roadmap](docs/ROADMAP.md) — remaining full-powertrain work
- [Development status](docs/DEVELOPMENT_STATUS.md) — current component state
- [Validation record](docs/VALIDATION.md) — checkpoints, evidence, and replay results
- [Agent API](docs/AGENT_API.md) — MCP client configuration and operation sequences

## License

Original Power! material is licensed under **GPL-3.0-or-later with the Unity Linking Exception**. Read [COPYING.NOTICE](COPYING.NOTICE), the unmodified [GPLv3 text](LICENSE), and the [exception](UNITY-LINKING-EXCEPTION.md) together.

The exception permits the specified Unity combination while keeping Power! and its modifications under GPL requirements. Unity and other third-party software retain their own licenses; the exception grants no rights held by their authors — see [third-party notices](THIRD_PARTY_NOTICES.md). Preserve the applicable license, copyright, and notice files when distributing source or binaries.
