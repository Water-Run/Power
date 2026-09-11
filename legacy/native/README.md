# Power! native Zig prototypes

This directory contains the Zig port of the archived native physics runtime.
The active application uses C#/.NET and Unity; the native library remains a
separate research runtime with the [explicit boundary](../../docs/NATIVE_ZIG.md).
All original C source and headers have been replaced by Zig. Their paths and
SHA-256 hashes are retained in [migration-manifest.json](migration-manifest.json)
and the original source is available at commit `c342d4c` in Git history.

## Build and verify

The pinned compiler is [Zig 0.15.2](https://ziglang.org/documentation/0.15.2/).
From the repository root:

```sh
python3 tools/InstallZig.py
dotnet run --file tools/Build.cs -- native-verify
```

Use `python` on Windows. `POWER_ZIG` may select an existing pinned compiler,
and `POWER_PYTHON` may select Python for the .NET build tool. The installer uses
committed official download URLs and SHA-256 checks. Full project verification
with `tools/Build.cs verify` includes these native checks after managed checks;
it keeps all builds and tests serial.

Direct development commands, from this directory:

```sh
zig build -j1 -Doptimize=ReleaseSafe
zig build test -j1 -Doptimize=ReleaseSafe
zig build test -j1
```

On macOS, prefix direct build commands with `DEVELOPER_DIR=/dev/null` to use
Zig's bundled SDK stubs; the root verifier does this automatically. See the
[SDK compatibility notes](../../docs/NATIVE_ZIG.md#build-and-maintenance).

There is no CMake, C translation or C source compilation. Platform OS APIs and
Zig's standard library implement the runtime primitives. Linux needs no libc;
macOS uses the OS-provided `libSystem` through Zig's bundled linker stubs. The shared
library exports `pwr_get_api`; `src/abi.zig` describes its binary layouts along
with the internal prototype types. The `power_host` and `power_model_host` Zig
executables call the shared library. Python `ctypes` remains an independent ABI
consumer through `tools/model_lab.py`.

The root verifier installs the shared library and hosts under `artifacts/native`,
runs the physics/SDK regressions and Python tests, compares all electrothermal
sample values to the original C baseline, and writes machine-readable evidence
under `artifacts/reports`. It rejects C/C++ files and headers in the repository
source inventory, including newly added files. Ignored SDK caches and local
third-party reference checkouts are outside that inventory.

## Physics and evidence

The port retains the compiled electromechanical/thermal graph, fixed scheduler,
controlled shaft, SI engine, exhaust, DCT, four-speed automatic and both
powertrain integration prototypes. The automatic-powertrain module is now
included in regression coverage; it was missing from the old CMake build.
Stable IDs, explicit units, generation handles, batch rollback, diagnostics,
channels, conservation ledgers and unverified parameter status are retained.

The archive's engine, exhaust and DCT-powertrain test entry points could return
success after a failed assertion. Their Zig entry points propagate failure. The
engine suite now separates steady combustion from rev-limiter fuel cut and
compares backpressure from a shared running state, with an analytic pumping
increment check. These corrections preserve the engine equations.

Passing these tests does not establish a complete engine, calibrated vehicle,
Unity Editor/Play behavior or IL2CPP compatibility. Complete EA211 DJS/DQ200 and
PSA EC5/AT8 samples still require the evidence and system boundaries recorded
under the root `assets/samples` directory.

The [historical README](docs/HISTORICAL_README.md) and other documents in `docs`
retain C-era descriptions as design/provenance records. They are not current
build instructions or calibration claims.

## License

Original Power! source, including this port, remains GPL-3.0-or-later with the
Unity Linking Exception. Preserve the root `COPYING.NOTICE`, `LICENSE` and
`UNITY-LINKING-EXCEPTION.md` together with source-file notices. Third-party
materials retain their own licenses.
