# Third-party notices

The repository's GPL license and Unity Linking Exception apply to original
Power! material. They do not relicense third-party software or grant rights
held by Unity, package authors, or the authors of referenced research material.

## Managed dependencies

The current NuGet lock files resolve the following license families, checked
against the installed packages' `.nuspec` metadata on 2026-09-07:

| Packages | Declared license |
|---|---|
| `ModelContextProtocol` and `ModelContextProtocol.Core` 2.2.0 | Apache-2.0 |
| Resolved `Microsoft.Extensions.*` packages | MIT |
| `System.Diagnostics.EventLog` 10.0.11 | MIT |

Exact versions are recorded in `src/Power.Mcp/packages.lock.json` and
`tests/Power.Mcp.Tests/packages.lock.json`. Package files are restored from
NuGet and are not vendored in this repository. When distributing binaries,
retain the applicable package license texts, copyright notices, and any
required NOTICE files. This list does not replace those notices.

References: [MCP C# SDK license](https://github.com/modelcontextprotocol/csharp-sdk/blob/main/LICENSE),
[.NET libraries](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT),
[.NET Extensions](https://github.com/dotnet/extensions/blob/main/LICENSE).

## Zig native runtime

The native toolchain is Zig 0.15.2. The compiler and standard library are
downloaded into an ignored cache; their code is not vendored in Power!.
Zig's standard library also contains mathematical routines ported from Go
and musl. These retain their original permissive licenses independently of
Power!'s GPL and Unity Linking Exception.

The native build installs Power!'s license/exception files together with
the [Zig MIT license](licenses/Zig-MIT.txt),
[Go BSD-3-Clause license](licenses/Go-BSD-3-Clause.txt) and
[musl copyright notices](licenses/musl-COPYRIGHT.txt) under
`share/licenses/power`. Preserve these when distributing the native binaries.

Sources: [Zig 0.15.2 license](https://github.com/ziglang/zig/blob/0.15.2/LICENSE),
[Go license](https://github.com/golang/go/blob/go1.24.0/LICENSE),
[musl 1.2.5 notices](https://git.musl-libc.org/cgit/musl/tree/COPYRIGHT?h=v1.2.5).

## Unity

Unity Editor, Player runtimes, engine assemblies, IL2CPP support code, and
Unity packages remain governed by their applicable Unity or package licenses.
The pinned package list is in `Unity/Packages/manifest.json`; the Editor and
package implementations are not included in this repository.

The [Unity Linking Exception](UNITY-LINKING-EXCEPTION.md) grants permission
from Power!'s copyright holders only. A valid Unity license and compliance
with the applicable package terms are still required. Preserve Unity's and
its dependencies' notices when distributing a Player.

References: [Unity legal terms](https://unity.com/legal),
[Unity Companion License](https://unity.com/legal/licenses/unity-companion-license).

## Research and historical material

`reference/README.md` identifies upstream checkouts used for research. Those
checkouts are excluded from Git and are not Power! dependencies. Original
Power! prototypes in `legacy/native` are covered by `COPYING.NOTICE`; this
does not change the licenses of upstream projects described in the archive.

The manifests in `assets/samples` record external sources and evidence.
Original Power! descriptions and sample definitions follow the repository
license; citations do not grant rights to third-party documents or assets.
