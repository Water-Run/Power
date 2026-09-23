# Working on Power!

- Use English for new code, comments, documentation, commit messages and user-facing updates. Preserve source quotations and provenance when retaining historical material.
- Original Power! material is GPL-3.0-or-later with the Unity Linking Exception. Preserve COPYING.NOTICE, LICENSE, UNITY-LINKING-EXCEPTION.md and applicable source-file notices; third-party materials retain their own licenses.
- The active stack is C# 14 / .NET 10 plus Unity 6.6. Start with README.md, docs/ARCHITECTURE.md, docs/ROADMAP.md and docs/AGENT_API.md. `legacy/native` is historical reference, not the current implementation target.
- Native prototypes under `legacy/native` use Zig 0.15.2, per the owner's direction. Keep the repository free of C/C++ source and headers, and Lua source, bytecode and packages. Preserve original source provenance through Git and `legacy/native/migration-manifest.json`; follow `docs/NATIVE_ZIG.md` and keep the active C#/Unity contracts.
- Keep the full powertrain objective. A passing electrothermal demo does not mean the engine, transmission, controls or calibrated vehicle samples are complete.
- Core code must target both net10.0 and netstandard2.1 and remain free of Unity, transport, model-provider and third-party dependencies. Unity Assets scripts must compile as C# 9. Do not claim Unity supports .NET 10 or C# 14 source compilation.
- Preserve explicit units, stable IDs, immutable compiled models, bounded integer time, complete batch rollback, observable channels and evidence reports. New physical behavior needs analytic/conservation/convergence checks appropriate to its equations.
- Agent-facing operations need machine-readable contracts and actionable errors. Preserve revision checks, branch independence, cancellation and compact outputs. A successful tool call is distinct from passing KPIs or calibrated physics.
- Build and verify with `dotnet run --file tools/Build.cs -- verify`. This serializes builds and tests; keep that memory-conscious behavior. Do not run simultaneous heavy builds or install large tooling as background work. Do not modify desktop extensions or compositor configuration.
- Unity verification is separate: set POWER_UNITY_EDITOR and run `unity-test`. Never replace actual Editor/Play/IL2CPP evidence with a .NET-only test claim.
- Use the user's existing authorization to progress. Routine reversible code, tests and documentation changes do not need another permission question. Preserve user work and source provenance.
- Do not mark research parameters as calibrated or silently fill missing OEM measurements. Retain the complete boundaries and evidence manifests in assets/samples.

# Documentation style

User-facing docs (`README.md`, `THIRD_PARTY_NOTICES.md`) are written for people,
not for agents. Keep them that way when editing. Write them in English; this
repository has no Chinese README.

- Write for the current version only. No changelogs, release attestations, or
  "since vX.Y" history. Git history is the log. Checkpoint counts and per-lab
  evidence stay in `docs/VALIDATION.md`.
- Say what a user needs and stop. Cut edge-case enumeration, exhaustive
  parameter semantics, and implementation internals. If a detail only matters
  when something breaks, it doesn't belong in the README.
- Plain sentences a person would say. No spec-legalese walls, no nested
  qualifiers, no "every X is Y; Z may differ when..." hedging chains.
- Calm, factual tone. Prefer "doesn't" / "不会" over "never" / "绝不". Soften a
  behavioral absolute with "by design" / "设计上". Describe what the project
  does; don't preach absolutes.
- Use GitHub Markdown deliberately: tables for reference data, `<details>`
  for long lists, blockquote notes for caveats. The area under the title
  carries no badge or nav-link row.
- In `THIRD_PARTY_NOTICES.md`, keep every license, source, and provenance fact.
  Change wording and layout only.
- Machine-only instructions live here, not in the user docs.
