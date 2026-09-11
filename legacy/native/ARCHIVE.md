# Native prototype provenance

The application moved to C# and Unity on 2026-09-07. The owner resumed the
native language migration on 2026-09-10, and the native prototypes, tests and
hosts were ported to Zig. There are no C source files or headers in the current
repository source tree. The original material remains in Git at
`c342d4c05c9d0b14cae0ce1a85e3f2100dfd7bb3`; the [migration manifest](migration-manifest.json)
records every removed C/header file and its replacement, plus retired build files.
The unused LuaInstaller launcher and its README were also retired on 2026-09-11,
with their hashes recorded in the same manifest. No Lua source or runtime is
part of the current project. Historical Lua plans do not apply to current builds.

See the [native README](README.md) for Zig commands, the [boundary](../../docs/NATIVE_ZIG.md)
for interoperability and the [root roadmap](../../docs/ROADMAP.md) for the full
powertrain objective. Historical design documents and sample evidence retain
their original language, provenance and calibration limits.
