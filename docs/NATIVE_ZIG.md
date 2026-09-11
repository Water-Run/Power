# Native Zig boundary

The owner resumed the native language migration on 2026-09-10. The native
prototypes under `legacy/native` have been migrated to Zig, including their
tests and hosts. They remain a separate research runtime: `Power.Core` and
`Power.Assets` keep their dependency-free managed contracts and dual targets,
and Unity continues to load the managed assemblies.

## Interoperability

The native shared library retains the versioned `pwr_get_api` entry point,
fixed-width scalar fields, structure sizes, integer nanosecond time, generation
handles, caller-owned snapshot buffers and function table. Zig `extern struct`
declarations and `.c` calling conventions express the existing binary ABI;
they do not require C source or headers in this repository. The Python
`ctypes` experiment runner remains a consumer of this boundary. Native engine,
exhaust and transmission prototypes remain internal Zig APIs, rather than
being silently added to the managed model schema or public native capabilities.

The migration must retain equations, model limits, stable IDs, diagnostics,
batch rollback, energy and mass ledgers, and regression scenarios. Native
prototype fidelity and `unverified` sample calibration are unchanged. Native
verification is separate from actual Unity Editor, Play Mode and IL2CPP
evidence and from completing the full powertrain objective.

## Provenance

The original C implementation is recoverable from Git commit
`c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3`. Its source-file paths and hashes
are recorded in `legacy/native/migration-manifest.json`. The Zig port keeps
the original copyright and GPL-3.0-or-later with Unity Linking Exception
notices. Historical design and research documents retain their original
quotations; their C-era descriptions do not describe the new build.


## Build and maintenance

The compiler is pinned to Zig 0.15.2 in `.zig-version`. `tools/InstallZig.py`
uses the official [Zig download metadata](https://ziglang.org/download/index.json)
with committed per-platform archive hashes. No C translator, headers, CMake or
libc linkage is needed by the build. The port was initially translated once,
then split into maintained Zig modules with shared binary layouts. Memory,
mathematical functions and atomic operations use Zig and platform OS APIs.
Safety checks remain enabled in ReleaseSafe builds.

Zig sources use LF checkouts on every platform. On macOS the verification
script disables Apple SDK discovery only in its Zig build subprocesses by
setting `DEVELOPER_DIR=/dev/null`. This selects Zig's bundled Darwin linker
stubs and avoids Zig 0.15.2's [incompatibility with Xcode 26.4 and newer SDKs](https://github.com/ghostty-org/ghostty/issues/11991),
whose `libSystem` stub uses arm64e targets. The system Xcode selection is unchanged;
these native targets do not need Apple frameworks or SDK headers.

`dotnet run --file tools/Build.cs -- verify` runs managed verification, then
serial native verification. `native-verify` runs just the native portion.
`tools/VerifyNative.py` rejects C/C++ source and headers, checks the compiler
pin and formatting, builds the library and both Zig hosts, runs the Zig and
Python suites, and compares the electrothermal experiment to its original C
baseline fixture. On Linux it also checks that only `pwr_get_api` is publicly
exported and that the library has no unresolved external symbols.

The baseline comparison preserves model fingerprints, units, channel mappings,
11 sample times and physical values. Cross-toolchain values use explicit
absolute/relative tolerances; replay hashes must match within the same binary.
Reports under `artifacts/reports` distinguish execution, KPI/replay results and
unverified calibration. Ignored compiler caches and private third-party
reference checkouts are not repository source.

## Archive test corrections

Three old test entry points returned zero even when their `CHECK` macro failed.
The port propagates these failures. The engine suite previously stopped at a
hidden failure because its final 22 Nm-load sample could be at rev-limiter fuel
cut. That scenario is retained as an explicit limiter test; continuous combustion
uses a synthetic 32 Nm test load. The backpressure comparison now starts from a
shared running state and checks the analytic pumping-torque increment before
comparing speed, avoiding startup/stall confounding. No engine equations or
production calibration values were changed. Automatic-powertrain coverage now
also exercises replay, differential energy, brownout and rollback; the old
CMake build omitted that entire module.
