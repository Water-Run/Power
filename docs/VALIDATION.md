# Validation record

**English** · [简体中文](VALIDATION.zh-CN.md) · [Français](VALIDATION.fr.md) · [Русский](VALIDATION.ru.md) · [日本語](VALIDATION.ja.md) · [한국어](VALIDATION.ko.md) · [Deutsch](VALIDATION.de.md) · [Español](VALIDATION.es.md) · [Italiano](VALIDATION.it.md) · [Português](VALIDATION.pt-BR.md)

## 2026-10-02: shared AT hydraulic supply and dynamic piston actuation

The required serial `dotnet run --file tools/Build.cs -- verify` passes locally
on Windows x64/.NET 10.0.12: **354/354** managed checks, **273/273**
Standard-assembly checks hosted on .NET 10, **37/37** actual MCP groups,
**16/16** Zig tests and six native-model C# checks. The Release build reports
zero warnings/errors. All **34** laboratories pass and all **176** original
baseline values match exactly. C/C++/Lua audits remain empty. Actual Unity and
new Linux/macOS acceptance remain unverified.

Evidence files:

- `artifacts/reports/at-actuation-closed-2026-10-02.log`: complete serial run.
- `artifacts/reports/at-actuation-evidence-2026-10-02.json`: log/report/source
  digests, pressure/travel/contact state, pump work and full inventory/energy bounds.
- `artifacts/reports/at-actuation-schema-audit-2026-10-02.json`: all 34 documents
  validate with jsonschema 4.25.1.
- `artifacts/reports/at-finer-10000.json` and `at-finer-5000.json`: finer coupled
  fired references, with explicitly retained authoring documents.

Six physical/transaction groups check immutable ordinary graph lowering,
actual pressure/travel/contact capacity, independent simultaneous RK4
pump-pressure-motion refinement, complete swept inventory, pump work and routed
heat, six common-supply branches, explicit units/stroke/reference/channel errors,
cancellation, late rollback, independent forks and allocation-free stepping.
The single-actuator thermal check independently derives shaft drag heat from
source work, shaft kinetic change and pump work. Front/back areas remain explicit;
the examples use 0.001/0 m2 and therefore retain front swept volume in inventory.

Three integration groups and two new actual MCP scenarios preserve every scalar
and state hash at **201** five-branch torque-train and **87** six-branch fired
boundaries. Helper and JSON fingerprints agree. The previous engagement channels
are absent; only actual fill/drain inputs drive pressure and motion. Zero initial
pad capacity remains zero despite an apply command. Physical locks are confirmed
through all four forward up/down paths. The fired lockup also uses actual piston
motion. Both pressure energy and piston/return/pad/stop energy remain in the
complete transaction and global energy ledger.

The coarser 40/20/10-us final-state comparison was nonmonotone: normalized errors
against 10 us were `1.74505e-5` at 40 us and `2.19913e-5` at 20 us. That failed
criterion is not treated as a passing convergence result. A finer **20/10/5-us**
comparison enters a decreasing-error range: relative to 5 us, the normalized
maximum over eight engine/vehicle/pressure/travel/work/heat outputs decreases
from **2.01463e-5** at 20 us to **6.51409e-6** at 10 us. All reference runs meet
physical KPIs and exact own-runtime replay. No global convergence order or OEM
accuracy is inferred from prescribed hybrid handoffs.

| Final quantity | Five-branch train (2 s) | Fired six-branch train (0.8 s) |
|---|---:|---:|
| Engine/source speed | 102.2242615437 rad/s | 36.2608726849 rad/s |
| Vehicle speed | 8.5186884620 rad/s | 13.5978272568 rad/s |
| Line pressure | 1.1963842937 MPa | 1.0666023745 MPa |
| Pump work | 117.4226709117 J | 37.6491650676 J |
| Reported states | 83 | 106 |
| Maximum sampled energy residual | `1.64680e-7 J` | `1.29307e-8 J` |
| Maximum swept inventory residual | `1.18076e-18 m3` | `2.94767e-19 m3` |
| Maximum gear phase residual | `5.68434e-14 rad` | `1.42109e-14 rad` |
| Fingerprint | `f61d582874bd086b` | `4e83efb34de63922` |
| Final hash | `9307fb49117dad0a` | `70e00a107ef1ca6d` |

Source SHA-256 values are
`119008206e0d1a5372ec399665d30669ac41bbf2f0fbc0c27c56bf3bc90c8f02`
(torque train) and
`9659b282454202857b16ee2dbc0226f30b79549fedab88b4c79f92c6c6e66801`
(fired). The dense fired plant fits the existing 128-state limit at 106 states
without removing engine, converter, pressure or actuator state. Existing v24
records suffice; no new asset format is introduced. The actual 37-scenario MCP
suite remains bounded at 300 s, including these larger coupled plants.

Parameters and maps remain `unverified`. Valve schedules are prescribed;
complete AT feedback, ECU torque coordination, measured valve-body behavior,
seal/cavitation/aeration/temperature models and OEM calibration remain open.
Prepared Studio imports/playback do not establish actual Editor/Play/Player/
IL2CPP acceptance. The full objective remains unfinished. See
[AT_HYDRAULIC_ACTUATION.md](AT_HYDRAULIC_ACTUATION.md).

## 2026-10-02: absolute planet spin, orbital inertia and four physical meshes

The required serial `dotnet run --file tools/Build.cs -- verify` passes locally
on Windows x64/.NET 10.0.12: **345/345** managed checks, **267/267**
Standard-assembly checks hosted on .NET 10, **35/35** actual MCP groups,
**16/16** Zig tests and six native-model C# checks. The Release build reports
zero warnings/errors. All **32** laboratories pass and all **176** historical
baseline values match exactly. C/C++ and Lua audits remain empty. Actual Unity
and new Linux/macOS acceptance remain unverified.

Evidence files:

- `artifacts/reports/resolved-planets-endpoint-2026-10-02.log`: full serial run.
- `artifacts/reports/resolved-planets-evidence-2026-10-02.json`: log/report/source
  digests, geometry, spin/orbit energies, residuals and numerical diagnostics.
- `artifacts/reports/resolved-planets-schema-audit-2026-10-02.json`: 32 valid
  documents and eight malformed carrier-mesh cases rejected by jsonschema 4.25.1.
- `artifacts/reports/resolved-planet-failure-2026-10-02.json` and
  `resolved-planet-refinement-failure-2026-10-02.json`: preserved pre-fix traces.

Eight physical/transaction groups check signed carrier-relative ratios,
independent acceleration-space mass matrices, rigid pitch geometry, per-planet
mass/spin aggregation and orbital inertia, all four forward/reverse reflected
inertias, angular momentum, zero summed mesh reaction power, carrier-capture
impulse/heat, 20-second loaded overdrive, unit/packing/reference errors,
cancellation, late rollback, forks and allocation-free stepping/readback.
Two portable groups retain complete signed ratios, carriers and spin storage,
reject malformed records and digest-resealed v23 downgrades, and replay the
authentic v23 reduced graph. Its SHA-256 is
`f2bd390e8ecf013e5843ed3352ccf2a2828133b541fc61d7927995f8ac1dc2e2`;
fingerprint `63d28eb32bc4cfb2` and every-boundary upgraded replay remain intact.

The initial torque-train run stopped after **1.4033 s**. The large-sun/outer-planet
speed residual was `1.2406076e-11 rad/s`, near the unchanged
`1.2406654e-11 rad/s` bound; phase error was zero and energy residual
`5.26143e-10 J`. Relative Schur refinement alone delayed the stop to **1.4494 s**.
The free midpoint solve now enforces `G v_next=0` using
`v_next=2 v_mid-v_old`, rather than repeatedly reflecting preceding roundoff.
At exact compatible old states this is the ordinary zero midpoint constraint.
All changes use actual force-response multipliers, which accumulate into mean
reactions. Three bounded relative refinements improve small force responses;
scratch is simulation-owned or constructor-local, and compiled factors remain
immutable. Existing graphs keep their preceding projection/replay behavior.
No inertia or velocity/phase tolerance was reduced or increased to pass the case.

Three integration groups and two new actual MCP scenarios match every scalar
and state hash at **201** torque-train and **87** fired/converter boundaries.
The helper and flat JSON fingerprints agree. Absolute inner/outer spin, orbital
carrier storage, all mesh reactions and complete friction/converter thermal
ledgers remain observable. Agent revisions, cancellation, invalid ratio repair
and independent neutral forks remain covered.

| Final quantity | Torque train (2 s) | Fired train (0.8 s) |
|---|---:|---:|
| Engine/source speed | 117.3118882900 rad/s | 40.9899766814 rad/s |
| Vehicle speed | 9.7759906908 rad/s | 15.3712412555 rad/s |
| Inner planet absolute speed | -469.2475531599 rad/s | -204.9498834069 rad/s |
| Outer planet absolute speed | 156.4158510533 rad/s | 122.9699300441 rad/s |
| Planet spin energy | 23.3037873338 J | 12.2863030022 J |
| Planet orbital energy | Near zero with carrier held | 15.4891426738 J |
| Reported states | 27 | 41 |
| Maximum sampled energy residual | `4.01224e-9 J` | `3.00179e-9 J` |
| Maximum gear phase residual | `1.13687e-13 rad` | `2.84217e-14 rad` |
| Maximum gear speed residual | `3.97904e-13 rad/s` | `2.27374e-13 rad/s` |
| Fingerprint | `d3010be4b33fdbaf` | `faf9384667ece88d` |
| Final hash | `8e1da00bda34941b` | `e606196f7b345c99` |

The fired train burns **38.0629762199 mg** and releases **1674.7709536768 J**.
Relative to a 12.5-us final-state reference, the normalized maximum error across
engine/vehicle/planet speeds and work/heat outputs decreases from `1.13450e-6`
at 50 us to `1.04173e-6` at 25 us. This is bounded refinement through prescribed
hybrid handoffs; no global convergence order is claimed.

The declared three synchronous pairs use ring radius **0.1 m**, per inner/outer
planet masses **0.3/1 kg**, and spin inertias **0.000015/0.0005 kg m2**.
Carrier structure **0.03 kg m2** receives explicit orbital inertia
**0.0184375 kg m2**. Source SHA-256 values are
`c45d90282097303da44c9405eb6945637b578db84617f0b749efc58ed879f0a5`
(torque) and
`e71d9ea4755826b1dcd916fd0157827b2e845dd48506e32f6291674d56f315f3`
(fired). Geometry, masses and maps remain `unverified`. Rigid synchronous meshes
don't establish manufacturing load sharing, tooth compliance, lubrication/losses,
complete AT hydraulics/control, OEM identity or calibrated vehicle behavior.
Prepared Studio cases include all three carrier-mesh ports; they don't establish
actual Editor/Play/Player/IL2CPP results. See [RESOLVED_PLANETS.md](RESOLVED_PLANETS.md).

## 2026-10-02: Ravigneaux compound paths and fired converter composition

The required serial `dotnet run --file tools/Build.cs -- verify` passes locally
on Windows x64 with .NET 10.0.12: **332/332** managed checks, **257/257**
Standard-assembly checks hosted on .NET 10, **33/33** actual MCP groups,
**16/16** Zig tests and six native-model C# checks. The Release build reports
zero warnings/errors. All **176** original baseline values match exactly;
C/C++ and Lua source inventories remain empty. All **30** laboratories pass.
Actual Unity and new Linux/macOS acceptance remain unverified.

Evidence files:

- `artifacts/reports/ravigneaux-confirmed-2026-10-02.log`: complete serial run.
- `artifacts/reports/ravigneaux-evidence-2026-10-02.json`: log/source/report
  digests, state bounds, numerical ledgers and declared limitations.
- `artifacts/reports/ravigneaux-schema-audit-2026-10-02.json`: all 30 documents
  pass jsonschema 4.25.1; eight malformed topology cases reject.
- `artifacts/reports/ravigneaux-phase-failure-2026-10-02.json`: isolated
  diagnosis before coordinate compensation.

Six physical/transaction groups check the independently reduced 2x2 free mass
matrix, all four forward and reverse reflected inertias, member reactions and
zero summed gear reaction power, carrier-brake capture impulse/heat, long loaded
overdrive, malformed units/geometry/ports, cancellation, late rollback,
independent forks and allocation-free stepping/readback. Two portable groups
check complete carrier records, typed counts, duplicate references and forged
v22 downgrade rejection. Authentic v22 controlled-DCT fixture SHA-256 is
`db90df5fa9ca90067341abdcaf8c3a4a38316794a4b3c8ecc7c89a852375c24e`;
fingerprint `72122eae163df98e` and exact upgraded replay remain intact.

Long loaded overdrive initially stopped after the last committed **2.9668 s**
boundary. The double-pinion phase residual was `-8.27754e-10 rad`, near the
`8.27906e-10 rad` bound, while speed residual was `-1.77991e-12 rad/s` against
`5.21235e-11 rad/s`. Compound models now use transactional compensated coordinate
accumulation. The same **20-second** analytic load check passes without increasing
phase/speed tolerances or projecting positions. Correction state copies, hashes
and rolls back with every interval; previous compound-free models retain their
existing integration/hash behavior.

Three integration groups and two actual MCP scenarios retain every output and
state hash at **201** torque-train and **87** fired/converter boundaries.
The five friction elements physically enact prescribed forward up/down handoffs;
command acceptance isn't treated as completed lock. Thermal storage matches the
sum of all routed clutch/converter heat. Revision checks, cancellation, bounded
input rejection and independent neutral forks remain covered.

| Final quantity | Torque train (2 s) | Fired converter train (0.8 s) |
|---|---:|---:|
| Input/engine speed | 119.6764605858 rad/s | 42.4589002302 rad/s |
| Vehicle speed | 9.9730383821 rad/s | 15.9220875863 rad/s |
| Reported states | 21 | 35 |
| Friction heat in the five range elements | 546.0473656082 J | 117.6775595348 J |
| Maximum sampled energy residual | `1.87947e-9 J` | `1.96445e-9 J` |
| Maximum gear phase residual | `3.19744e-14 rad` | `1.06581e-14 rad` |
| Maximum thermal ledger difference | `1.52568e-10 J` | `2.41471e-9 J` |
| Fingerprint | `63d28eb32bc4cfb2` | `7c207499d9050fc6` |
| Final hash | `4eb3c05eeef034c6` | `0cbed5442969c06f` |

The fired train burns **37.7116554560 mg**, releases **1659.3128400630 J**,
dissipates **66.4214778376 J** in the converter and **125.9622150447 J** in
lockup. Relative to the 12.5-us final-state reference, the normalized maximum
error across engine/vehicle speed and three work/heat outputs decreases from
`9.92882e-7` at 50 us to `7.19762e-7` at 25 us. This is bounded refinement
through prescribed hybrid events, not a claimed global order of convergence.

Source SHA-256 values are
`8063388dbd341fd16b05ab971f6c2cc23d87ebfee3681da9d1ffcf95fffa52a0`
(torque train) and
`55587bff2a2e5f33c6e876ed0d539615cfee1daf938d72dc4946c39a36a74d44`
(fired train). Research parameters/maps remain `unverified`. Internal planet
spin, detailed gear losses, AT hydraulics/control, ECU coordination, exact OEM
topology and measured samples remain open. Unity import/playback tests are
prepared as individual resource cases, including repaired earlier argument-count
errors; no Editor/Play or IL2CPP result is inferred from the managed run.
See [RAVIGNEAUX_TRANSMISSION.md](RAVIGNEAUX_TRANSMISSION.md).

## 2026-10-01: sampled DCT synchronization, staged handoff and combined fired control

The required serial `dotnet run --file tools/Build.cs -- verify` passes locally
on Windows x64 with SDK 10.0.401/runtime 10.0.12: **321/321** managed checks,
**249/249** Standard-assembly checks hosted on .NET 10, **31/31** actual MCP
groups, **16/16** Zig and **6/6** C# ABI checks. Release build reports zero
warnings/errors. All **176** historical baseline values match exactly and
C/C++/Lua audit is empty. Actual Unity and new Linux/macOS acceptance are unverified.

Evidence files:

- `artifacts/reports/tcu-final-2026-10-01.log` — complete serial run.
- `artifacts/reports/tcu-evidence-2026-10-01.json` — scope, log/source digests,
  actual gear/fault/phase, state bounds, phase and energy residuals.
- `artifacts/reports/tcu-schema-audit-2026-10-01.json` — all **28** laboratories
  pass jsonschema 4.25.1; **10** malformed controller cases reject.
- `artifacts/reports/controlled-dual-clutch.json` and
  `artifacts/reports/controlled-fired-dual-clutch.json` — complete reports.

Eight physical/control groups verify all seven confirmed paths, unloaded
preselection, staged exclusive drive handoff, signed reverse, moving-direction
block, synchronization timeout, persistent confirmed-lock loss, neutral/new-target
recovery, integral static/immediate/scheduled input checks, ten-channel ownership,
integer sampling, immutable routes, cancellation/late rollback, forks and zero
allocation. A 20-second controlled run verifies strict gear phase preservation
and energy. Controller outcomes remain observable faults; they aren't silently
treated as a successful shift or numerical failure.

Long loaded synchronization first exposed accumulated coordinate roundoff at
3.7688 s. Gear speed residual was within its bound, while normalized phase error
`-3.2883917811e-10` marginally exceeded the existing `3.2882809435e-10` bound.
New controlled models now accumulate midpoint-velocity coordinates with
transactional compensated correction. Strict tolerances weren't increased and
no state position was projected onto a chosen ratio. Prior models keep their
preceding integration/hash behavior; new compensation copies/hashes/rolls back
with every actual and speculative interval.

The explicit reported-state bound is **128**. Node/component limits remain
**32/64**; serial build/test behavior is unchanged. A 32-rotor/64-RL-state model
compiles and steps at exactly 128 reported states. Higher tracked-gas, film,
injection and controller models reject capacity. The complete fired/DCT/controller
composition now fits at 70 states, rather than omitting engine/control state to
fit the preceding limit. This doesn't establish sparse/Burst or Unity performance.

Two portable groups verify v22 routes, periods, ramps, tolerances, ownership and
fault/replay history, bounded typed counts, wrong references/units, missing records
and forged v21 downgrades. Authentic v21 graph fixture SHA-256 is
`706618f7d32a5ef4b8a18ca801d2c3ba0b293eac03b584d88d584e9290a38437`;
fingerprint `7466a75b99fbfd78` and same-runtime upgraded replay remain intact.
Earlier fixtures and physical fingerprints are retained regression evidence.

Three integration groups and two actual MCP scenarios check sampled state,
integer request, actionable owned-channel guidance, revisions, cancelled/failed
batches and independent gear-command forks. All **421** controlled-train and
**83** controlled-fired report/portable/MCP boundaries match exactly.

| Final quantity | Controlled train (4.2 s) | Controlled fired train (0.8 s) |
|---|---:|---:|
| Requested / confirmed gear | 1 / 1 | 3 / 3 |
| Shift phase / fault | Driving / None | Driving / None |
| Engine speed | 197.0169047878 rad/s | 51.7225290420 rad/s |
| Vehicle rotor speed | 12.6455009492 rad/s | 7.6456066581 rad/s |
| Reported states | 64 | 70 |
| Maximum sampled energy residual | `1.18562e-8 J` | `1.19940e-9 J` |
| Maximum reported gear phase error | `7.16227e-14 rad` | `7.10543e-15 rad` |
| Fingerprint | `72122eae163df98e` | `1d2b1a0c1eabf259` |
| Final hash | `ef2843acbb273e6d` | `b50dea693d6af82a` |

The final loaded downshift and subsequent unloaded preselection are observed
through physical confirmation at 4.2 s. At 4.0 s the prior confirmation was
temporarily disturbed by real selector/drive slip, so actual gear correctly
reported zero rather than assuming completion. The fired combination burns
**37.2949777641 mg** and releases **1640.9790216183 J**.

Source SHA-256 values are
`f15629d3fc8d8915f5884f77603458053c83e7835cf34d434fa607d85442bc87`
(controlled) and
`c3a6d491fd614d526a99831d7dfdc4285bca3c10a57ac811959023cfb9e40d5b`
(controlled fired). All parameters remain `unverified`. This controller deliberately
uses torque-interrupted handoff; complete ECU torque blending, sensors/actuators,
dog/baulk-ring mechanisms, comprehensive faults, AT, measured target powertrains
and actual Unity remain open. See [DCT_CONTROL.md](DCT_CONTROL.md).

## 2026-10-01: seven-forward/reverse dual-clutch power paths

The required serial `dotnet run --file tools/Build.cs -- verify` passes locally
on Windows x64 with SDK 10.0.401/runtime 10.0.12: **308/308** managed checks,
**239/239** Standard-assembly checks hosted on .NET 10, **29/29** actual MCP groups,
**16/16** Zig and **6/6** C# ABI checks. Release build has zero warnings/errors.
All **176** historical baseline values match exactly; C/C++/Lua audit is empty.
Actual Unity and new Linux/macOS acceptance remain unverified.

Evidence files:

- `artifacts/reports/dct-final-2026-10-01.log` — full serial verification.
- `artifacts/reports/dct-evidence-2026-10-01.json` — scope, digests, graph/replay,
  final quantities and measured refinement.
- `artifacts/reports/dct-schema-audit-2026-10-01.json` — all **26** laboratories
  pass the existing jsonschema 4.25.1 structural contract.
- `artifacts/reports/dual-clutch-transmission.json` and
  `artifacts/reports/fired-dual-clutch.json` — complete experiment reports.

Six physical groups verify an ordinary graph with fourteen internal rotors,
twelve permanent gears and ten friction clutches, caller-owned stable bindings,
seven forward and signed reverse paths, three final-drive branches and immutable
parameters. Independent reflected-inertia/constant-torque references cover every
selected path with and without inactive-path preselection. Independent two-coordinate
capture projection checks synchronization impulse, final speeds and heat.
Complete forks, cancellation, late rollback, capacity/unit/ID/selection contracts
and zero managed allocation for successful stepping/readback pass.

The full torque scenario exposed a six-to-seven handoff failure immediately
after old-clutch release. Correlated gear-reflected locks exhausted the scalar
projection budget. A normalized, preallocated linear Schur fallback now solves
independent locks after that budget is exhausted, with the same static limits,
bounded active-set release, residual and passive-heat checks. Singular/nonlinear
cases retain their existing limits. Direct six/seven independent acceleration
and the formerly failing handoff are regression evidence; older physics and
authentic asset trajectories remain verified. No iteration-limit increase or
failed-residual acceptance was used.

Three integration groups verify assembly/JSON fingerprint identity, launch,
preselection, all forward up/down handoffs, thermal routing, structured diagnostics,
revisions/cancellation and independent selector forks. Both laboratories replay
through portable assets and an actual MCP child server. All **281** torque-lab
and **83** fired-lab report/portable/MCP boundaries agree exactly.

The torque scenario reaches each declared effective ratio after handoff:

| Forward gear | Verified engine/vehicle speed ratio |
|---|---:|
| 1 | 15.58 |
| 2 | 9.43 |
| 3 | 6.765 |
| 4 | 5.166 |
| 5 | 3.774 |
| 6 | 3.182 |
| 7 | 2.664 |

These are declared research reductions, not OEM measurements. Reverse sign and
preselected reflected inertia have independent constant-load evidence; the road
scenario doesn't claim moving-vehicle reverse engagement.

| Final quantity | Torque DCT (2.8 s) | Fired DCT (0.8 s) |
|---|---:|---:|
| Engine speed | 161.3549080416 rad/s | 53.8218438613 rad/s |
| Vehicle rotor speed | 10.3565409526 rad/s | 7.9559266609 rad/s |
| Total clutch/synchronization heat | 870.3601867183 J | 224.4917038405 J |
| Thermal node | 300.8703601867 K | 351.7212768995 K |
| Maximum sampled energy residual | `5.22732e-9 J` | `1.23919e-9 J` |
| Reported state count | 55 | 61 |
| Model fingerprint | `7466a75b99fbfd78` | `b7216a2ed8c88dc3` |
| Final hash | `c8932376afe516c6` | `35b434aca827c3a6` |

The fired example burns **39.1255750233 mg** and releases **1721.5253010267 J**.
Its full seven-forward/reverse graph drives 1-to-2-to-3 scheduled handoff within
the current bounded state budget. Versus a 12.5-microsecond reference, maximum
scaled final differences for engine/vehicle speeds, source work and selected
clutch heats are **4.9008455434e-6** at 50 microseconds and **4.3731765238e-6** at
25 microseconds. Error decreases modestly; this alone doesn't establish uniform
hybrid-event order or complete asymptotic convergence. Independent gear/clutch
references and conservation remain separate evidence.

Source SHA-256 values are
`f5db9f0a9f3a0585eff261d71285e4a390c099ec1a3bec27d1b9194beb311294`
(torque) and
`0dfc3ed92930b649c8312043625789a4a4394811fed9e0c88a615a18a6b07473`
(fired). Existing component kinds, units, asset format and prior readers are retained.
All parameters remain `unverified`; this research train arrangement isn't
calibrated DQ200. Friction selectors don't complete dog/baulk-ring actuation,
and prescribed schedules don't implement full TCU/ECU torque coordination.
Complete AT, measured losses/actuation, full target powertrains and actual Unity
remain open. See [DUAL_CLUTCH_TRANSMISSION.md](DUAL_CLUTCH_TRANSMISSION.md).

## 2026-10-01: bounded closure replay and physical-tick cutoff compensation

The serial `dotnet run --file tools/Build.cs -- verify` passes locally on Windows
x64 with SDK 10.0.401/runtime 10.0.12: **299/299** managed checks, **233/233**
Standard-assembly checks hosted on .NET 10, **27/27** actual MCP groups,
**16/16** Zig and **6/6** C# ABI checks. Release build has zero warnings/errors.
All **176** historical numerical values match exactly; C/C++/Lua source audit
remains empty. Actual Unity and new Linux/macOS CI are unverified.

Evidence files:

- `artifacts/reports/closure-final-2026-10-01.log` — full serial verification.
- `artifacts/reports/closure-evidence-2026-10-01.json` — machine-readable scope,
  source/log digests, replay fingerprints, tracking and horizon evidence.
- `artifacts/reports/closure-schema-audit-2026-10-01.json` — all **24** laboratory
  documents pass jsonschema 4.25.1; **6** malformed horizon cases reject.
- `artifacts/reports/closure-compensated-cylinder.json` — fired experiment.

Five Core groups verify exact independent manual zero-voltage replay, unchanged
committed values/hash/time, improved actual delivery, cutoff memory, horizon
refinement, alignment/budget/immutability, read and batch cancellation, late
failure, independent forks, zero managed allocation for forecasts and active
predictive stepping, and complete speculative clutch histories. Every candidate
uses separate preallocated state and normal plant equations; no predicted fuel
is added to a real ledger. Disabled predictions retain previous fingerprints and
hashes. Cancellation or an invalid prediction rejects the complete real batch.

At 1 ms the isolated benchmark forecasts about **2.9894117019 mg** of additional fuel
under immediate voltage removal, and its separate manual closure replay agrees
to the declared `1e-15 kg` comparison tolerance. The held-input forecast is
distinct from actual future events or measured device behavior.

Finite-horizon tracking retains late seat rebound:

| Prediction horizon | Actual delivered fuel for an 8-mg request |
|---|---:|
| 8 ms | 8.2537740284 mg |
| 12 ms | 8.0606880453 mg |
| 20 ms | 8.0101785078 mg |
| 30 ms | 8.0101785078 mg |

The preceding on/off feedback delivers **9.8939438959 mg**. With a 20-ms forecast
and physical-tick cutoff, relative error drops from **23.6743%** to **0.12723%**,
about **186 times** smaller in this synthetic benchmark. The 20/30-ms decision
agrees, while 8 ms truncates a material tail. This is bounded model-based tracking
evidence, not calibrated injector accuracy. Electrical/contact timestep and
controller/horizon refinement remain separate acceptance controls.

Two portable groups verify v21 horizon/cutoff replay, exact re-encoding, malformed
driver horizons and forged v20 downgrade rejection. Authentic v20 fixture SHA-256
`4d87e996d92a50508cfcffac610551b84220f2b3055f5b104c8ec7ec64ca9a2e`
retains fingerprint `3fa813ff44b95a79` and same-runtime upgraded replay. Older
fixtures and ideal/on-off model trajectories remain regression evidence.

Three integration groups plus an actual MCP child server verify model horizons,
prediction observables, owned voltage, actionable errors, revisions, cancellation,
independent forks and complete source/phase/energy ledgers. All **65** fired
report/portable/MCP boundaries match. At the 0.6-s boundary:

| Quantity | Value |
|---|---:|
| Delivered and evaporated liquid | 28.0021908833 mg |
| Rail pressure | 725.327490978 kPa |
| Remaining liquid film | 0 mg |
| Burned vapor | 27.9775490263 mg |
| Reaction heat | 1231.0121571575 J |
| Latest requested / delivered cycle dose | 12 mg / 12.0346025230 mg |
| Last selected closing prediction | 2.7431038678 mg |
| Prediction length | 2000 physical ticks |
| Cutoff latch / pending cutoff ticks | 1 / 0 |
| Maximum sampled absolute energy residual | `1.51078e-8 J` |
| Maximum sampled absolute mass residual | `4.06576e-18 kg` |
| Maximum sampled absolute fuel residual | `8.97855e-20 kg` |

Fingerprint is `ddefea6d870e7e75`; final hash is `b8edcd36b58805b6`; model source
SHA-256 is `106bde4e86ccb10cd214d1372cff300c09f277c88ee8811d2d9a144d72ca8326`.
The live next-cycle command remains 4 mg; prediction and measured delivery are
reported separately from that command.

Forecasts hold other actuator commands, ignore future external input events,
respect a finite integer horizon and require a monotone local cutoff bracket.
Those assumptions and the unverified physical parameters limit this evidence.
Full ECU/TCU, rail refill, magnetic/electronic/fluid refinement, actual Unity and
calibrated powertrain acceptance remain open. See
[CLOSURE_PREDICTION.md](CLOSURE_PREDICTION.md).

## 2026-10-01: electromagnetic needle and sampled delivery feedback

The required serial command `dotnet run --file tools/Build.cs -- verify` passes
locally on Windows x64 with SDK 10.0.401/runtime 10.0.12: **289/289** managed
checks, **226/226** Standard-assembly checks on .NET 10, **26/26** actual MCP
groups, **16/16** Zig and **6/6** C# ABI checks. Release compilation has zero
warnings/errors. All **176** historical numerical values match exactly; source
audit finds zero C/C++/Lua files. No new Linux/macOS CI or actual Unity
Editor/Play/Player/IL2CPP acceptance is claimed.

Evidence files:

- `artifacts/reports/needle-final-2026-10-01.log` — complete serial run.
- `artifacts/reports/needle-evidence-2026-10-01.json` — machine-readable scope,
  source/log digests, laboratory fingerprints, final quantities and refinement.
- `artifacts/reports/needle-schema-audit-2026-10-01.json` — all **23** laboratories
  pass jsonschema 4.25.1; **16** malformed magnetic/stop/needle/driver cases reject.
- `artifacts/reports/needle-actuated-cylinder.json` — complete experiment/replay.

Nine physical groups verify the magnetic discrete energy identity, signed supply
work and nonnegative copper heat, analytic force Jacobian, stationary analytic RL
current, simultaneous magnetic/spring dynamics, conservative travel-stop hinges,
actual needle delivery outside quota/window, owned voltage, delayed unseating and
closure, dose overshoot, complete transactions and immutable/dimensional contracts.
Active stepping and snapshots allocate **zero managed bytes**. Speculative clutch
capture tests preserve flux, source/phase, mean force, compensated heat/work and
sampled/held control history through exact batching and a later failed batch.

An independent five-state ODE integrates needle position/velocity, magnetic flux,
copper heat and electrical work. RK4 references at **20,000/40,000** steps agree
within `1e-10` scaled tolerance. Over 5 ms, smooth physical refinement is:

| Tick | Maximum scaled error | Previous error / current error |
|---|---:|---:|
| 25 microseconds | `8.4976116406e-4` | — |
| 12.5 microseconds | `2.1110507406e-4` | 4.02530 |
| 6.25 microseconds | `5.2689855150e-5` | 4.00656 |
| 3.125 microseconds | `1.3167017353e-5` | 4.00165 |

This is second-order smooth electromagnetic/mechanical refinement. Contact,
window/driver switching and the existing explicit wall coupling keep separate
accuracy limits; exact replay doesn't prove uniform order or calibrated control.

The isolated 8-mg request delivers **9.8939438959 mg** after passive electrical/
mechanical closure and seat rebound, an excess of **1.8939438959 mg**. A zero
opening at 20 ms is followed by about **0.0001200614 mg** of additional rebound
delivery before settling. The model retains that flow instead of clipping mass
at the target or equating a zero-voltage command with a closed valve. These are
research dynamics, not accepted dose tracking or measured injector behavior.

Two portable groups check complete v20 magnetic, stop, needle and driver records,
same-runtime all-boundary replay, exact re-encoding, bounded typed counts, wrong
units/references, missing/duplicate tables and forged v19 downgrade rejection.
Authentic v19 fixture SHA-256 is
`4ff5f6180006d53be5ffb6ef0663cc6de4af45f35f39dc4d52fd7e00e8cdd8c5`;
fingerprint `4f74c6da6d89ab08` and same-runtime upgraded replay remain intact.
The legacy ideal quota path, older fixtures and existing physical fingerprints
remain regression evidence.

Three integration groups plus an actual MCP child server verify strict dimensions/
references, owned-voltage command guidance, revisions, cancellation, independent
forks and magnetic/source/phase/thermal ledgers. All **65** report/portable/MCP
boundaries agree. The 0.6-s fired laboratory ends with:

| Quantity | Value |
|---|---:|
| Delivered liquid | 40.3903592511 mg |
| Rail pressure | 692.292375330 kPa |
| Evaporated fuel | 34.5106221450 mg |
| Remaining liquid film | 5.8797371061 mg |
| Burned vapor | 31.2333851260 mg |
| Reaction heat | 1374.2689455424 J |
| Electrical supply work | 0.168028754073 J |
| Copper heat | 0.167510468341 J |
| Magnetic energy | `6.30199e-16 J` |
| Held coil command | 0 V |
| Actual needle lift | 0.4842244877 micrometers |
| Actual needle velocity | -0.1290065607 m/s |
| Latest latched request / actual delivered dose | 4 mg / 5.8930695115 mg |
| Maximum sampled absolute energy residual | `1.57642e-8 J` |
| Maximum sampled absolute mass residual | `3.30682e-18 kg` |
| Maximum sampled absolute fuel residual | `9.48677e-20 kg` |

The final boundary still has a moving, almost-seated needle and an incompletely
evaporated film. The new experiment therefore checks finite remaining inventory
and the complete fuel ledger instead of inheriting the ideal-injector laboratory's
dry-film condition. Passing numerical KPIs doesn't establish exact commanded dose.
Model fingerprint is `3fa813ff44b95a79`; final hash is `de68045d420b9ffa`; source
SHA-256 is `d8c7472a3335a523821209cb183cf34687e454f326d9ac1946c618b7ca73050c`.

All parameters remain `unverified`. Linear unsaturated inductance, constant R,
pressure-balanced needle, elastic stops and ideal voltage drive are declared
reductions. Nonlinear magnetic/thermal maps, switching/flyback/battery drive,
axial fluid forces, spray/displacement, rail pump/refill, full ECU/TCU and measured
powertrains remain open. Prepared Unity coil/stop/controller views and needle
stroke scaling require actual Editor/Play verification. See
[NEEDLE_ACTUATION.md](NEEDLE_ACTUATION.md).

## 2026-10-01: compliant liquid rail and cycle injection

The required serial command `dotnet run --file tools/Build.cs -- verify` passes
locally on Windows x64 with SDK 10.0.401/runtime 10.0.12: **275/275** managed
checks, **215/215** Standard-assembly checks hosted on .NET 10, **25/25** actual
MCP groups, **16/16** Zig tests and **6/6** C# ABI checks. Release build reports
zero warnings/errors. All **176** historical baseline values match exactly;
source audit finds zero C/C++/Lua files. This doesn't establish new Linux/macOS CI
or actual Unity Editor/Play/Player/IL2CPP acceptance.

Evidence files:

- `artifacts/reports/liquid-injector-final-2026-10-01.log` — complete serial run.
- `artifacts/reports/liquid-injector-evidence-2026-10-01.json` — machine-readable
  scope, log digest, current laboratory/source fingerprints, final quantities and
  independent refinement.
- `artifacts/reports/liquid-injector-schema-audit-2026-10-01.json` — all **22**
  laboratories pass jsonschema 4.25.1; **16** malformed liquid-injector cases reject.
- `artifacts/reports/liquid-injected-cylinder.json` — experiment and complete replay.

Eight physical groups check analytic pressure-head decay, finite compliant source
inventory, exact rail work, passive nozzle heat, caloric/chemical/pressure ledgers,
quota latching, reverse-pressure closure and reversal without quota reissue.
Starvation reaches the prescribed receiver pressure without inventing fuel.
Independent forks, rejected/cancelled inputs, late failure after accepted injection,
and speculative clutch capture preserve every source/film/quota/heat history.
Warm active injection and snapshots allocate **zero managed bytes**. Explicit
properties, source-volume feasibility, units, film/crank ownership, bounded state
capacity and immutable compilation are exercised.

An independent simultaneous eight-state ODE integrates delivered liquid, film mass,
wall temperature, receiver gas mass/internal energy and three pressure/heat
histories. Independent RK4 runs at **20,000/40,000** steps agree within the declared
`1e-10` scaled tolerance. Over 0.2 s in a smooth forward window, physical
20/10/5/2.5-ms ticks give maximum scaled errors:

| Tick | Maximum scaled error | Previous error / current error |
|---|---:|---:|
| 20 ms | `2.9306688569e-7` | — |
| 10 ms | `7.3266958993e-8` | 3.999987 |
| 5 ms | `1.8316757833e-8` | 3.999996 |
| 2.5 ms | `4.5791930144e-9` | 3.999997 |

This establishes second-order smooth injection/evaporation coupling under the
declared reduction. Fixed-tick window events, starvation and other explicit wall
sources retain their separate accuracy limits; no uniform fired-powertrain order
is claimed.

Two portable groups verify v19 source/nozzle/timing records, exact re-encoding,
every-boundary replay, bounded typed counts, wrong units/ownership, duplicate/
missing records and forged v18 downgrades. The authentic v18 film fixture retains
SHA-256 `c0e59c6f6527b2b59ee1094cd6627c0829ecbd4ab0eb2e6ec06394ccfe15da97`,
fingerprint `cb103bce098f4e82` and same-runtime upgraded replay. Earlier fixtures
and film-free physics remain regression evidence. The review also updates current
header offsets in malformed-record tests while retaining old-version table sizes.

Three integration groups plus an actual MCP child server check the shared finite
source, liquid deposition, vapor-only reaction, source/film/wall balances, strict
documents and session revisions/cancellation/forks. All **65** report/portable/MCP
boundaries agree. The 0.6-s laboratory starts with a dry film and **500 mg** of
liquid at **800 kPa**, and ends with:

| Quantity | Value |
|---|---:|
| Delivered and evaporated liquid | 28 mg |
| Remaining source liquid | 472 mg |
| Rail pressure | 725.333333333 kPa |
| Remaining film liquid | 0 mg |
| Released rail pressure work | 0.028472888889 J |
| Exported receiver pressure work | 0.003236038241 J |
| Nozzle heat | 0.025236850648 J |
| Film heat drawn from the wall | 14 J |
| Film wall temperature | 498.602523685 K |
| Burned vapor | 27.9759472237 mg |
| Reaction heat | 1230.9416778442 J |
| Maximum sampled absolute energy residual | `5.52370e-9 J` |
| Maximum sampled absolute mass residual | `1.08420e-18 kg` |
| Maximum sampled absolute fuel residual | `7.45389e-20 kg` |

The final observed cycle retains a **12-mg** request/delivery while the live
command is **4 mg** for a future window. Dose acceptance, delivery and reaction
remain distinct. Model fingerprint is `4f74c6da6d89ab08`; final hash is
`ddf5b1c4b0678451`; source SHA-256 is
`85b9cba983e31258d9341943dab3c844a18c05c5320dda3d9d610a3935a14d09`.

Rail pressure energy is internal stored energy; nozzle heat enters the finite
wall. The negligible-liquid-volume receiver exports displacement pressure work
explicitly instead of crediting hidden gas-volume or crank work. Its zero-pressure
compliance reference and constant properties are declared research reductions.
All parameters remain `unverified`. Pump/refill, backing-pressure/property maps,
needle/electrical/spray dynamics, finite-liquid-volume coupling, ignition/ECU,
complete transmission/control and calibrated vehicle powertrains remain open.
Unity rail/nozzle views and lifecycle tests are prepared but require the pinned
Editor. See [LIQUID_FUEL_INJECTION.md](LIQUID_FUEL_INJECTION.md).

## 2026-10-01: finite liquid fuel film and symmetric transport

The required serial command `dotnet run --file tools/Build.cs -- verify` passes
on Windows x64 with SDK 10.0.401/runtime 10.0.12: **262/262** managed checks,
**205/205** Standard-assembly checks hosted on .NET 10, **24/24** actual MCP groups,
**16/16** Zig tests and **6/6** C# ABI checks. All **176** retained historical
baseline values match exactly. Source audit finds zero C/C++/Lua files. The Release
build reports zero warnings/errors. This is local working-tree evidence, with no
new Linux/macOS CI or actual Unity Editor/Play/Player/IL2CPP acceptance.

Evidence files:

- `artifacts/reports/fuel-film-final-2026-10-01.log` — complete serial run.
- `artifacts/reports/fuel-film-evidence-2026-10-01.json` — machine-readable scope,
  log digest, laboratory fingerprints, phase quantities and refinement errors.
- `artifacts/reports/fuel-film-schema-audit-2026-10-01.json` — all **21** laboratory
  documents pass jsonschema 4.25.1; **12** malformed film cases are rejected.
- `artifacts/reports/film-fired-cylinder.json` — experiment and complete replay.

Ten physical groups verify analytic finite-bath heating, signed phase reference,
saturation/dryout, limited heat availability, cooling, zero conductance, actual
vapor-only reaction and independent constituent/chemical/thermal ledgers. Complete
forks, cancelled and late-rejected batches preserve all histories; warm stepping
and snapshot reads allocate **zero managed bytes**. Wrong ports/units, invalid phase
properties, state-capacity overflow and immutable compilation are exercised.

The review corrected the second half-step from film/gas to gas/film and reversed
the shared-wall film sweep. This makes film/gas/mechanics/gas/film symmetric, while
preserving the existing solver path for film-free models. Independent simultaneous
RK4 references at 20,000 and 40,000 steps agree within the declared `1e-10` scaled
tolerance. A two-second smooth saturated-film study uses 40/20/10/5 ms physical
ticks and the maximum scaled error across liquid/gas mass, wall temperature, gas
internal energy and transported constituents:

| Coupling | Error at 40 ms | Error at 5 ms | Successive halving ratios |
|---|---:|---:|---|
| Two films sharing a finite wall | `3.43324e-8` | `5.36263e-10` | 4.0000, 4.0002, 4.0012 |
| Film vapor leaving through a choked gas port | `3.17195e-5` | `4.89033e-7` | 4.0495, 4.0029, 4.0014 |
| Film plus gas-wall heat exchange | `5.30200e-4` | `6.61364e-5` | 2.0024, 2.0012, 2.0006 |

Shared-film and vapor-transport cases show second-order smooth refinement. Other
wall heat sources retain the explicit outer-interval temperature and first-order
coupling limit. These checks don't establish uniform second order across dryout,
valve/reaction events or a complete fired powertrain.

Two asset groups verify v18 phase quantities, deterministic encoding, every-boundary
replay, typed bounded records, malformed units/counts, duplicate/missing records
and forged v17 downgrade rejection. Authentic v17 metering fixture SHA-256
`d51dc0968bc3d154b2c3b227f74b7210215d4ee00c5a47e8dd16dc1d71cd5da1`
retains fingerprint `099db1021c8df1fe` and same-runtime upgraded replay. Earlier
fixtures and film-free model fingerprints remain regression evidence.

Three integration groups and the actual MCP child server verify the same finite
inventory, phase energy, vapor-only reaction, strict documents and revision/fork/
cancellation behavior. All **63** JSON/report/portable/MCP boundaries agree. The
0.6-second laboratory begins with **40 mg** of explicit wetting and ends with:

| Quantity | Value |
|---|---:|
| Remaining liquid | 0 mg |
| Evaporated fuel | 40 mg |
| Heat drawn from the finite wall | 20 J |
| Film wall temperature | 498 K |
| Burned vapor | 31.1117599551 mg |
| Reaction heat | 1368.9174380261 J |
| Final energy residual | `-1.58434e-9 J` |
| Maximum sampled absolute energy residual | `2.15960e-9 J` |
| Maximum sampled absolute mass residual | `1.49078e-18 kg` |
| Maximum sampled absolute fuel residual | `2.09641e-19 kg` |

Model fingerprint is `cb103bce098f4e82`; final hash is `495582b10f40832c`.
Source SHA-256 is
`8c933a5889d3b8f22f37738e307989d2c0fa5c7dfe9018ffe50a21f19e2aa416`.
All parameters remain `unverified`. Initial wetting isn't liquid injection;
pressure-dependent phase properties, replenishment, ignition/ECU, complete
transmission/controls and measured powertrains remain open. Prepared Unity film
markers and lifecycle tests need the pinned Editor. See [FUEL_FILM.md](FUEL_FILM.md).

## 2026-09-30: finite fuel rail and cycle-dose metering

The published `dc7ec2d` gas-accumulator checkpoint passes
[Windows/Linux/macOS CI](https://github.com/Water-Run/Power/actions/runs/36710249585).
That evidence covers the preceding source, not the new injector or actual Unity.

The required serial command `dotnet run --file tools/Build.cs -- verify` passes
locally on Windows x64 with SDK 10.0.401/runtime 10.0.12: **247/247** managed checks,
**193/193** Standard-assembly checks hosted on .NET 10, **23/23** actual MCP groups,
**16/16** Zig and **6/6** C# ABI checks. All **176** historical numerical values
match exactly; source audit finds zero C/C++/Lua files. Release build has zero
warnings/errors. Log: `artifacts/reports/fuel-injector-final-2026-09-30.log`.

Eight physical groups verify forward/wrapped timing, finite rail and exact quota,
latched requests, reverse-pressure closure and starvation, reversal without quota
reissue, an independent two-vessel mass/enthalpy ODE with smooth refinement, analytic
metered premixed burn, complete transactions, dimensions/compatibility/capacity and
immutable geometry. Warm stepping and snapshots allocate **zero managed bytes**.
Late torque/volume failure, cancellation, input rejection, exact batching and forks
preserve all delivery/ordinal histories. Timing discontinuities retain fixed-tick
refinement requirements; no continuous switching-time claim is made.

Two portable groups verify v17 nozzle/timing, bounded typed records, wrong units,
missing/duplicate records and forged downgrade rejection. The authentic v16 gas
accumulator fixture retains SHA-256
`72f0ee5b3180b763de091674565b2b8f2cbd61f80a3e4ab46e9694ad153a79c7`, fingerprint
`739f2baba8c669a0` and every-boundary same-runtime upgraded replay. Prior fixtures
and physical fingerprints remain unchanged. Three integration groups verify actual
rail depletion, delivered/reacted/boundary fuel, strict contracts and sessions.

All **20** laboratory documents pass structural schema validation; **12** malformed
injector cases are rejected by jsonschema 4.25.1. Cycle values, finite compatible
ports, dose maxima and crank ownership remain additional compiler checks. Report:
`artifacts/reports/fuel-injector-schema-audit.json`. Existing schema audits also pass
all 20 documents. An invalid reservoir receiver now returns a connection diagnostic
before reservoir-fraction validation. Different R/gamma/LHV/stoichiometry are rejected
so internal transfers cannot invent chemical inventory.

The `metered-fired-cylinder` replaces premixed-fuel intake with pure air plus a
finite gaseous rail. Requested doses are 8/12/4 mg; they latch at the next forward
window. At 0.6 s the latest observed cycle still holds 12 mg, while the live command
is 4 mg for a future window. Execution and dose acceptance remain distinct from
actual delivery. All **65** report/portable/MCP boundaries agree exactly.

| Final quantity | Value |
|---|---:|
| Delivered fuel | 28 mg |
| Burned fuel | 27.9299615615 mg |
| Released reaction heat | 1228.918308706072 J |
| Remaining chamber fuel | 0.0055539661 mg |
| Net boundary fuel | -0.0644844724 mg |

Burned, remaining and boundary-lost fuel account for delivered fuel. Internal rail
transfer adds no external fuel-energy input or unlimited source. Rail thermal
enthalpy and chemical energy use the same limited constituent flux.

| Maximum absolute error over the experiment | Value | Asserted bound |
|---|---:|---:|
| Rail depletion vs delivery | 3.67e-18 kg | 1e-16 kg |
| Delivered vs reacted/remaining/boundary fuel | 6.78e-21 kg | 1e-14 kg |
| Whole-model energy | 1.52e-9 J | 1e-6 J |
| Total mass | 4.07e-18 kg | 1e-14 kg |
| Fuel constituent | 3.67e-18 kg | 1e-14 kg |
| Fresh-air constituent | 1.20e-18 kg | 1e-14 kg |

Fingerprint `099db1021c8df1fe`; final Windows/runtime hash `329e1109392b37f3`.
Reports: `artifacts/reports/metered-fired-cylinder.json` and
`fuel-injector-evidence-summary.json`.

Three serial CLI runs, each with two 0.6-s trajectories (24,000 accepted ticks),
full replay and 65 boundaries, take **0.502 / 0.364 / 0.370 s**, median **0.370 s**
at ordinary desktop load. Traces agree exactly; zero-allocation warm stepping is
checked separately against both assemblies. This is observed local cost, not a
speedup or portable throughput guarantee.

Agent 0.20.0 and actual MCP verify kg-dose discovery, complete export/replay, invalid
quota rejection and independent controller/fuel forks. Unity injector/rail/timing
views and Edit/Play tests are prepared in C# 9 source. Actual Unity Editor/Play/
Mono/IL2CPP/Player and fresh three-platform evidence for this increment remain
separate. This is ideal gaseous metering with common constant gas properties;
liquid spray/evaporation, needle/rail/tank hardware, calibrated gasoline injection,
ignition/ECU and the complete powertrain remain unfinished. Parameters are unverified.

## 2026-09-30: gas piston and finite-energy accumulator

Development resumed at the owner's request. The prior `cebc978` spool checkpoint
passed [Windows, Linux and macOS CI](https://github.com/Water-Run/Power/actions/runs/36695753045);
each matrix job completed its required serial verification. Downloaded log:
`artifacts/reports/spool-three-platform-2026-09-30.log`. That evidence covers the
published spool source, not the new gas-piston increment or actual Unity.

The new required serial command passes on local Windows x64:
`dotnet run --file tools/Build.cs -- verify`. Results: **234/234** managed checks,
**183/183** Standard-assembly checks hosted on .NET 10, **22/22** actual MCP groups,
**16/16** Zig and **6/6** C# ABI checks. All **176** historical values match exactly;
the source audit finds zero C/C++/Lua files. Release build has zero warnings/errors.
Log: `artifacts/reports/gas-accumulator-final-2026-09-30.log`.

Eight physical groups cover analytic adiabatic work and its Jacobian, small travel,
signed/opposed chambers, independent mass/energy RK4 and smooth second-order
refinement, common gas/fluid motion, a separate finite-wall RK4 reference with
first-order wall-coupled refinement, moving-volume gas inflow and reservoir enthalpy,
complete transactions, geometry/units and allocation-free stepping. The inflow
ledger measured about `1.14e-17 kg` cumulative floating-point residual after repeated
mass updates; its `1e-16 kg` bound reflects that accumulation. Closing the port
preserves the accepted mass exactly. No mass or energy correction forces a pass.

Two portable groups retain complete v16 geometry, signed direction, bounded typed
counts, malformed units, missing/duplicate records and downgrade rejection. The
authentic v15 spool fixture matches the published checkpoint package byte-for-byte:
SHA-256 `67b976a27ca0bd7343ca6b024c5bc736e14f48326945da841785ade43b61f14b`,
fingerprint `28aa0965d248e280`. All original/upgraded event boundaries match within
the executing runtime. Earlier fixtures and physical fingerprints remain intact.

All **19** laboratory documents pass schema validation. **12** malformed gas-piston
documents are separately rejected by jsonschema 4.25.1. Positive nominal volume and
unique geometry ownership remain additional compiler checks. Reports are under
`artifacts/reports`, including `gas-piston-schema-audit.json`; the existing piston,
spool, battery/duty and pressure-controller audits also pass all 19 documents.

The six-second `gas-accumulator-pump` model adds a 50-ml gas chamber and a 50-g
separator to the electric pump, mechanical spool bypass and pressure clutch. Gas
starts at 200 kPa absolute/300 K, with a declared 100-kPa reference and 0.1-mm
compliant seating penetration. During the 3-4-s pulse, motor voltage is 6 V and both
fill/drain paths are explicitly open. Gas internal energy drops **0.6196209894 J**;
reference-pressure work is **-0.2094578629 J**. After separator kinetic/stop energy
and damping, net delivery to liquid is **0.4092090634 J**, with **2.094578629 ml**
of returned swept volume. Filling resumes and the clutch is locked with zero slip
at the final boundary. These are synthetic results, not OEM calibration.

| Maximum absolute error over all 306 boundaries | Value | Asserted bound |
|---|---:|---:|
| Separate pump/fluid/motion/gas/reference/heat account | 6.09e-13 J | 1e-8 J |
| Whole-model energy | 1.83e-8 J | 1e-6 J |
| Liquid reference-volume account | 5.19e-19 m3 | 1e-16 m3 |
| Normalized closed-gas adiabatic invariant | 6.83e-13 J | 1e-8 J |

Every **306** JSON/portable/MCP boundary has identical hashes and values. Fingerprint
`739f2baba8c669a0`; final Windows/runtime hash `074917dc8e343131`. Reports:
`artifacts/reports/gas-accumulator-pump.json` and `gas-accumulator-evidence-summary.json`.
The gas/fluid account explicitly subtracts reference work and the initial stop
potential; absolute gas internal energy alone is not labeled delivered fluid energy.

### Measured closed-chamber optimization

Closed, unmixed new gas-piston models with no gas transport or heat links retain
state validation and skip zero-rate integration. Three serial full CLI runs per
stage include two six-second trajectories (600,000 accepted ticks) and all 306
boundaries. Baseline times were **2.727 / 2.603 / 2.496 s**; optimized times were
**2.382 / 2.362 / 2.357 s**. Median cost decreases about **9.3%** on this desktop.
All three before/after traces match every observable and state hash exactly.
Steady Step/ReadSnapshot allocation remains **zero managed bytes**. This is local
measurement, not a portable throughput guarantee. Ports, wall links and constituent
transport retain their ordinary integration path and independent tests.

Agent 0.19.0/MCP verify thermodynamic discovery, complete export/replay, revisions
and independent gas/fluid forks. Studio views and Edit/Play tests are prepared in
C# 9 source. Actual Unity Editor/Play/Mono/IL2CPP/Player and fresh three-platform
verification of this new increment remain separate. The complete engine,
transmission, ECU/TCU, calibrated vehicle samples and accepted desktop application
remain unfinished. All sample boundaries, licensing and provenance are preserved.

## 2026-09-30: mechanical spool regulator checkpoint

The required serial command passes on Windows x64, SDK 10.0.401/runtime 10.0.12:
`dotnet run --file tools/Build.cs -- verify`. The result is **221/221** managed
checks, **173/173** Standard-assembly checks hosted on .NET 10, **21/21** actual
MCP child-server groups, **16/16** Zig and **6/6** C# ABI checks. All **176**
historical numerical values match exactly. Release compilation has zero warnings/
errors; the source audit finds zero C/C++/Lua files. Log:
`artifacts/reports/spool-final-2026-09-30.log`.

The [spool contract](HYDRAULIC_SPOOL.md) adds a pressure-balanced metering land,
explicitly neglecting axial jet force. Actual piston position and both fluid-port
pressures participate in the shared Newton solve with analytic derivatives. Seven
physical groups verify signed travel, passive bidirectional flow and derivatives,
independent steady pressure, a separate three-state RK4 transient with smooth
second-order refinement, decreasing error through opening, finite-port equalization,
transactions and immutable geometry. Warm Step/ReadSnapshot allocation remains zero
against both Core targets. Late failure, cancellation and independent forks retain
all pressure/motion/heat state. No uniform nonsmooth order is claimed.

Two portable groups cover complete v15 geometry and forged malformed counts/types/
units, missing/duplicate records and downgrade rejection. Authentic v14 piston
fixture SHA-256 is
`72e0605d00972a58e2f5358aa8cbd44417e81cf1c452a20ab9661eac0919bae0`; its fingerprint,
physical references and exact same-runtime upgraded replay retain every event
boundary. Prior fixtures are intact. Three integration groups verify strict
documents, session revisions/cancellation/forks and a separate fluid/motion ledger.
Wrong metering-position units retain the unit diagnostic rather than being caught
as constructor range errors.

All **18** laboratory documents pass structural schema validation. **12** malformed
spool cases are independently rejected by jsonschema 4.25.1. Report:
`artifacts/reports/spool-schema-audit.json`. Nonzero signed travel, land positions
within piston stroke and typed ownership remain additional compiler checks.

The `spool-regulated-pump` experiment runs **3 s** with **20,000 ns** ticks and
**156** matching JSON/portable/MCP boundaries. Its moving pressure actuator, return
spring/damping and bypass regulate the line without a sampled valve controller.
The 2 N*m drive/brake are explicit research loads. The inherited two-second
high-pressure capture criterion failed at the lower regulated pressure; the
experiment now runs long enough to observe actual capture while retaining zero-slip
and locked-mode assertions. No pressure, energy or slip state is corrected to pass.

| Final quantity | Value |
|---|---:|
| Line pressure | 233956.17402528782 Pa |
| Spool displacement | 0.00016975695474013045 m |
| Metering opening | 0.08487847737006522 |
| Spool restriction heat | 2.917614235503143 J |
| Return damping heat | 0.0006249365922203377 J |
| Pump hydraulic work | 3.3299555096992406 J |
| Clutch heat | 260.2402810714738 J |
| Final clutch mode/slip | Locked / 0 rad/s |

Across all boundaries, the separate hydraulic/motion/spring/pad/heat balance has
maximum error **9.77e-15 J** (asserted bound 1e-8 J), global energy **1.99e-8 J**
(bound 1e-6 J), and reference-volume inventory **1.35e-20 m3** (bound 1e-16 m3).
Fingerprint `28aa0965d248e280`, final Windows/runtime hash `180744d025212ef7`.
Reports: `artifacts/reports/spool-regulated-pump.json` and
`artifacts/reports/spool-evidence-summary.json`.

Three serial CLI runs, each including two complete trajectories (300,000 accepted
ticks), all replay checks and 156 output boundaries, took **1.183 / 1.230 / 1.153 s**;
median **1.183 s** at ordinary desktop load. This is an observed checkpoint cost,
not a cross-platform throughput guarantee or evidence of speedup. Matrix, land-slope
and rollback buffers are bounded and simulation-owned; steady stepping/readback
remain allocation-free.

Agent 0.18.0 advertises mechanically regulated hydraulics, geometry units and its
jet-force omission. Actual MCP checks cover complete export/replay and rejection of
an attempted opening-output write. Unity valve/actuator views and Edit/Play tests
are prepared in C# 9 source. `POWER_UNITY_EDITOR` is unset; actual Editor/Play/
Mono/IL2CPP/Player and fresh Linux/macOS verification remain pending. The owner ends
today's development at this numerical checkpoint; complete Power! and calibration
are not claimed. Source/package delivery preserves licenses and sample boundaries.

## 2026-09-30: dynamic piston and contact-actuated clutch

The required serial command passed on Windows x64, SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **209/209** managed checks, **164/164** Standard-assembly checks hosted on .NET 10,
  and **20/20** actual-child-server MCP groups.
- **16/16** Zig and **6/6** C# ABI checks; all **176** historical values match exactly.
  Source audit finds zero C/C++/Lua files. Release build: zero warnings/errors.
- Log: `artifacts/reports/piston-final-2026-09-30.log`.
- All **17** laboratories pass JSON Schema validation. Separate audits each reject
  **12** malformed piston, battery/duty and voltage-control cases with jsonschema
  4.25.1. The reports are `piston-schema-audit.json`, `battery-schema-audit.json` and
  `pressure-controller-schema-audit.json` under `artifacts/reports`.

[Hydraulic pistons](HYDRAULIC_PISTON.md) add explicit mass, displacement, chamber
swept volume, spring/damping, pad clearance and compliant stroke ends. Contact
clutches derive capacity from pad force. Eight physical check groups cover hinge
work and analytic derivatives, separate compact damping histories/analytic decay,
coupled spring/fluid oscillation with smooth second-order refinement, finite/back
reservoir work and volume, piecewise RK4 contact refinement, free fill/capture/release,
typed contracts and full transactions. No uniform order is claimed across contact
events. Late numerical failure, cancellation, forks and exact batching preserve the
whole state. Warm core stepping and snapshot reads allocate **zero managed bytes**.

The synthetic `piston-actuated-clutch` laboratory advances **15 s** at **20,000 ns**
ticks with **5 ms** sampled duty control. All **761** report/portable/MCP boundaries
have identical hashes and observable values in this runtime. Pressure during free
fill produces no pad force. The scheduled drain releases the clutch; later fill
captures it. At the final boundary:

| Quantity | Value |
|---|---:|
| Piston displacement | 0.0021772797986273195 m |
| Pad force | 177.27979862731945 N |
| Static/sliding capacities | 22.69181422429689 / 11.345907112148446 N*m |
| Front chamber pressure | 199053.02928772685 Pa |
| Pad/stop stored energy | 0.01571406350067147 J |
| Cumulative return damping heat | 0.0025718766359138913 J |

Fingerprint `46f746398142c258`; final Windows/runtime hash `3a7b8eee248785d3`.
Report: `artifacts/reports/piston-actuated-clutch.json`. The source explicitly uses
300 N*s/m synthetic damping to keep supply sufficient during the contact transient;
the solver retains negative-pressure rejection rather than clamping that state.

### Independent energy accounts and tolerance evidence

Every boundary independently compares pump work with compliant fluid, slider kinetic,
return-spring and pad energy plus restriction/damping heat; battery chemical/RC loss
with motor kinetic/inductive energy, pump work and electrical heat; and external
rotational work with rotor energy and clutch heat.

| Account | Maximum absolute error over the experiment | Asserted bound |
|---|---:|---:|
| Hydraulic, motion and contact | 1.56e-13 J | 1e-9 J |
| Electrical supply and motor | 2.48e-8 J | 1e-7 J |
| Shared thermal node vs direct heat histories | 2.87e-7 J | 5e-7 J |
| Driven rotors and clutch | 1.16e-6 J | 2e-6 J |
| Whole model | 1.42e-6 J | 5e-6 J |

The initial 1e-7-J hydraulic assertion inferred tiny spring heat by subtracting
large clutch heat from rounded shared-node temperature. It failed, and that inferred
heat even decreased near steady motion. A direct compensated damping channel now
retains the physical dissipation independently; the tighter hydraulic balance above
passes. Thermal and rotational accumulation explain the remaining global residual.
The inherited 1e-6-J global threshold was insufficient for this 750,000-tick run;
5e-6 J is an explicit long-run numerical bound, supplemented by the tighter analytic,
fluid-volume and separate energy checks. No energy state is corrected to force a pass.
Supplemental audit: `artifacts/reports/piston-evidence-summary.json`.

### Performance scope

Three serial CLI runs at ordinary desktop load each include **two** complete
trajectories (1.5 million accepted ticks), replay checks and 761 output boundaries.
Elapsed medians were **4.007 s** before the analytic derivative/damping histories,
**4.100 s** with the analytic derivative and full component-indexed histories, and
**4.234 s** with compact histories. The last three runs were 4.234, 4.088 and 4.928 s.
These measurements do not establish a speedup or a portable throughput guarantee.
The analytic Jacobian removes the physical-length perturbation and repeated contact
evaluations; compact histories allocate and copy only actual spring slots. Dense
workspace and LU factors stay bounded, cached and simulation-owned. Successful
steady stepping and snapshot reads retain the zero-allocation assertion.

Asset v14 stores piston/back boundary, stroke/pad parameters and referenced friction
geometry. Counts, coverage, malformed units/types, duplicate/missing records and
forged downgrades are rejected. The authentic v13 battery fixture retains SHA-256
`67e7bde4dfcc2b8b41f49cadae8f89ff5fc5a1668059e084b7b5d9da02c7c44a`, physical
references and exact same-runtime upgraded replay. Older fixtures remain intact.
Agent 0.17.0/MCP exercise discovery, validation, every exported boundary, contact
force, revision conflicts and independent forks.

Unity slider/contact views and Edit/Play tests are prepared in C# 9 source. The
Standard check host is .NET 10; it does not exercise Unity Editor. `POWER_UNITY_EDITOR`
is unset. Editor/Play/Mono/IL2CPP/Player and fresh Linux/macOS evidence remain pending.
Parameters are unverified research inputs; the complete engine, transmission,
ECU/TCU and calibrated vehicle samples remain unfinished.

## 2026-09-30: finite battery supply and duty regulation

The required serial command passed on Windows x64, SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **196/196** managed checks, **154/154** Standard-assembly checks hosted on .NET 10,
  and **19/19** actual-child-server MCP groups.
- **16/16** Zig and **6/6** C# ABI checks; all **176** historical values match exactly.
  Source audit finds zero C/C++/Lua files. Release build: zero warnings/errors.
- Log: `artifacts/reports/battery-final-2026-09-30.log`.
- All **16** laboratories pass JSON Schema; **12** malformed battery/duty cases are
  rejected by jsonschema 4.25.1 in an isolated ignored cache. Supplemental report:
  `artifacts/reports/battery-schema-audit.json`.

Finite-capacity battery nodes add affine OCV/SOC and one polarization RC branch.
Battery motors use a bidirectional averaged duty transformer; switched resistive
accessory loads share bus resistance. Chemical/RC and motor inductive energy, battery/
copper/load heat and mechanical/hydraulic transfers share the conservation ledger.
Battery motor work is internal, not duplicated external source work. Initial numerical
checks exposed missing battery-motor inductive energy; the ledger now includes it.

Evidence covers analytic no-load polarization decay and resistive-load RC response,
charge inventory, independent four-state RK4 motor/battery integration and smooth
second-order refinement. Signed duty, regenerative charging, parallel winding
equivalence, duty-dependent geared responses, joint clutch/pump coupling, full late
depletion rollback, cancellation, input rejection, independent forks, batch replay and
zero allocations pass against both assemblies. Runtime factors and mechanical
responses remain simulation-owned and update for changed duties/load openings.

The battery-regulated pump uses 100 µs physical ticks and a 5 ms duty regulator.
Accessory pulses cause measured-in-simulation bus sag. All **761** report/portable/MCP
boundaries agree. At 15 s SOC is **0.6269678451**, remaining charge **31.3483922575 C**,
terminal voltage **12.6056975839 V**, polarization **0.0207555849 V**, and discharge
current **0.2052072574 A**. Pressure ends at **200828.2935823 Pa** for a 200000 Pa
target. Last sampled error is **-825.4441034 Pa**; integral/held duty are
**0.0642978516 / 0.0629221114**. Final energy residual is about `2.13e-7 J`.
Fingerprint `40d4fcbab9cad8f8`, final Windows/runtime hash `803d9adef384cd35`.
Report: `artifacts/reports/battery-regulated-pump.json`. The 50 C charge capacity is
explicitly a small synthetic verification inventory, not an OEM battery measurement.
Every boundary checks isolated electrical energy and absence of duplicated source work.

Asset v13 retains battery OCV/resistance/capacitance/heat parameters and duty control
period/gains/bounds/initial integral. Re-signed malformed types, counts, units, missing/
duplicate extensions and forged downgrades are rejected. The authentic v12 regulated
pump fixture retains its digest, fingerprint, physical references and same-runtime
upgraded replay. Prior fixtures and uncontrolled model fingerprints remain unchanged.
Agent 0.16.0/MCP verify battery discovery, complete experiment/export replay, duty
ownership, invalid accessory writes, revisions and independent battery/control forks.

Unity battery/electrical/duty views and import/Play lifecycle tests are prepared.
Actual Editor/Play/Mono/IL2CPP/Player evidence and new Linux/macOS checks remain pending.
Constant affine battery parameters, ideal converter and prescribed accessory/valve
schedules do not establish BMS/chemistry/ageing, PWM/current control, contactors/faults,
actuator mechanics, full ECU/TCU, complete DCT/AT, remaining engine behavior or vehicle
calibration. All parameters remain `unverified`; the full Power! objective remains open.

## 2026-09-30: sampled pressure-control checkpoint

The required serial command passed on Windows x64 with SDK 10.0.401/runtime 10.0.12:

```sh
dotnet run --file tools/Build.cs -- verify
```

- **185/185** managed checks, **146/146** Standard-assembly checks hosted on .NET 10,
  and **18/18** actual-child-server MCP integration groups.
- **16/16** Zig and **6/6** C# ABI checks; all **176** historical values match exactly.
  Source audit finds zero C/C++/Lua files. Release build: zero warnings/errors.
- Log: `artifacts/reports/pressure-control-final-2026-09-30.log`.
- All **15** laboratories pass the model JSON Schema; **12** malformed controller
  cases are structurally rejected. The supplemental audit uses jsonschema 4.25.1 in
  an isolated ignored cache. Report: `artifacts/reports/pressure-controller-schema-audit.json`.

`pressure_controller` reads a hydraulic node and owns a DC motor voltage input.
Samples occur at time zero and integer multiples of a tick-aligned period; voltage
is held between samples. The first sample retains the supplied initial integral.
Conditional integration prevents increments farther into voltage saturation. Four
controller states and the held input participate in complete rollback, cancellation,
forks, hashes and zero-allocation stepping. External voltage overrides are rejected
with actionable `controlled_input` errors. See [the contract](HYDRAULIC_PUMP.md#sampled-pressure-regulation).

Independent numerical evidence includes a separately programmed sampled PI/RK4
motor/shaft/pressure plant. Halving physical ticks at a fixed 10 ms control period
shows smooth second-order convergence to that sampled reference; this is not a
claim of second-order convergence toward a continuous-time controller. Exact constant
pressure tests verify sample/hold, event endpoint ordering and fork clock phase.
Saturation and unwinding, malformed units/periods/ownership, state capacity, late
arithmetic failure rollback, cancellation, independent memory and allocation checks
run against both Core target assemblies. An unreachable target executes and replays
successfully but fails tracking KPIs while command stays saturated and integral is held.

Asset v12 carries the complete 80-byte control record with target, integer period,
gains, voltage limits and initial integral. Typed coverage, bounded counts, malformed
units/records, duplicate/missing extensions and forged downgrades are rejected. The
authentic v11 fired-pump fixture was captured before the writer changed; its original
SHA-256 and fingerprint remain fixed. Same-runtime upgraded replay and physical
references pass, alongside all previous fixtures and unchanged uncontrolled models.

The `pressure-regulated-pump` experiment uses 100 µs physical ticks, 5 ms control
samples and 300/350/200 kPa setpoints with scheduled clutch fill/drain disturbances.
All **757** report, portable and MCP boundaries agree. At 15 s, line pressure is
**200550.7972096 Pa** for the 200000 Pa target. Last sampled pressure is
**200544.9882311 Pa**, error **-544.9882311 Pa**, integral **0.8124749429 V**, and held
voltage **0.8015751783 V**. Motor current is **0.5964870315 A** and pump shaft speed
**2.0526093297 rad/s**. Model fingerprint `67e8edb13dc42f42`; final Windows/runtime
state hash `44342c02cd41f3c6`. Report: `artifacts/reports/pressure-regulated-pump.json`.
Tests also isolate electrical work from the driven/load mechanical boundaries at
every report sample and check volume/energy residuals.

Agent 0.15.0 exposes the control contract, dimensional gains, owned-input errors and
example. Real MCP tests exercise complete experiment/export replay, blocked voltage
writes, setpoint updates, revisions, controller forks and parent independence.
Unity now prepares regulator/sensor/command views and import/Play reset/replay tests.
No actual Editor/Play/Mono/IL2CPP or Player evidence was obtained; new Linux/macOS
verification also remains pending. Prescribed valve schedules and the ideal sensor/
voltage source do not implement complete ECU/TCU, battery/PWM, sensor dynamics,
actuator mechanics, full DCT/AT, remaining engine behavior or measured calibration.
All research and sample parameters remain `unverified`. The full Power! goal remains open.

## 2026-09-30: resumed pump-loss and electrical-supply checkpoint

The owner resumed development. Source changes were verified locally on Windows x64
with .NET SDK 10.0.401 and runtime 10.0.12 (the configured `latestPatch` roll-forward).
Base commit is `d266095`; these changes were uncommitted at verification time.

```sh
dotnet run --file tools/Build.cs -- verify
```

- **174/174** managed checks, **138/138** Standard-assembly checks hosted on .NET 10,
  and **17/17** MCP groups against an actual child server.
- **16/16** Zig checks and **6/6** C# ABI checks; all **176** historical values match
  exactly. Source audit retains zero C/C++/Lua files. Release build: zero warnings/errors.
- Log: `artifacts/reports/pump-assembly-final-2026-09-30.log`. The earlier baseline
  repair alone passed 165/165, 132/132 and 15/15 in `resume-baseline-2026-09-30.log`.

The preceding [CI run 35809732259](https://github.com/Water-Run/Power/actions/runs/35809732259)
passed on Linux and failed four asset checks on Windows/macOS. Each failed only at
a hard-coded final state hash from the original Linux fixture run, after original and
upgraded playback agreed. Fixture file digests and model fingerprints remain exact.
Tests now preserve same-runtime hash equality and use physical references from the
documented checkpoints with explicit tolerances reflecting their published precision.
Historical hashes remain recorded in fixture provenance. This Windows run repairs the
observed failure locally; it does not establish a new Linux/macOS CI result.

`HydraulicPumpAssembly` composes the ideal pump, outlet-to-inlet pressure leakage and
grounded viscous shaft friction. Independent evidence covers all signed regimes and
passive power identities, analytic damped shaft/pressure motion, smooth second-order
refinement, finite-inlet inventory, reservoir work, an independent RK4 integration of
the RL-motor/shaft/pressure ODE, analytic electric equilibrium, full late-failure rollback,
cancellation, forks, immutability and zero-allocation stepping. Both target assemblies
run the same checks. The zero-loss fired experiment reproduces the original laboratory's
shared physical observables within declared tolerances.

The `fired-pump-losses` laboratory has **89** matching report/portable/MCP boundaries.
Its model has 60 counted states. At 0.8 s, pump work is **52.6573534421 J**, leakage
heat **8.6081420133 J**, combined pump loss heat **41.0517855370 J**, and the pump
thermal node reaches **300.4105178554 K**. Final crank speed is **68.6812975063 rad/s**
and line pressure **1.0581382360 MPa**. Energy residual is about `-6.13e-10 J`.
Fingerprint `524661ea3d721bbc`. Report: `artifacts/reports/fired-pump-losses.json`.

The `electric-pump` laboratory has **106** matching boundaries over 2 s. Its RL motor,
separate pump shaft, leakage, drag, relief and compliant line drive scheduled pressure
clutch fill/drain/recapture. External hydraulic work is **zero**. Final pump-shaft speed
is **67.0570291504 rad/s**, current **5.3148298325 A**, line pressure **0.5606191525 MPa**,
and clutch slip below `1e-8 rad/s`. Pump work is **3.6117807126 J**; leakage heat is
**0.3597803209 J**. Energy residual is about `5.85e-10 J`. Fingerprint `d8f8fedfdce59003`.
Report: `artifacts/reports/electric-pump.json`. Checks isolate electrical work from the
separate driven/load shaft boundaries and verify that sealed valves prevent pressure
actuation even while the electric pump operates.

Agent 0.14.0 advertises the composition, units, power semantics and both examples.
The unchanged JSON schema and asset v11 carry ordinary components; no new format or
component kind was introduced. Fourteen laboratories export and execute in the serial
build tool, including the previously export-only fired-pump CLI report.

Unity import/Play replay, reset and cleanup tests are prepared for both new assets.
`POWER_UNITY_EDITOR` is unset and the pinned Editor was not found in the standard
installation directory. No actual Editor/Play/Mono/IL2CPP or desktop Player evidence
was obtained. New Linux/macOS verification also remains pending. Constant loss values,
prescribed commands and all sample parameters remain `unverified`; measured maps,
battery/control/regulator/piston dynamics, complete DCT/AT and ECU/TCU, remaining engine
behavior, calibrated vehicle samples and release acceptance remain unfinished.

## 2026-09-22 closing checkpoint: shaft-driven pump and pressure relief

Added ideal reversible displacement pumps, explicit finite/reservoir inlets and
finite-conductance one-way pressure relief. Pump speeds and hydraulic pressures join
the cylinder/converter Newton solve; pressure-clutch capacities refresh inside the
constraint iteration. Accepted shaft/fluid transfer, reservoir work, reference volume
and thermal losses share complete transactional state. JSON/schema, asset v11, agent
0.13.0, MCP discovery and the fired-pump laboratory use the same definitions.

Required serial verification completed successfully:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **165/165** managed checks, **132/132** Standard-assembly checks hosted on .NET 10,
  and **15/15** MCP groups against an actual child server process.
- **16/16** Zig tests and **6/6** Python ABI tests; all **176** retained historical
  numerical values match exactly. Source audit retains zero C/C++/Lua files.
- Release compilation: zero warnings and errors. Log:
  `artifacts/reports/pump-integration-verify.log`.

Independent evidence includes ideal power identities in both directions, analytic
shaft/compliance oscillation, closed-inlet inventory, reverse motoring, geared pump
reactions, exact midpoint relief decay, a regulated constant-load equilibrium, and an
analytic slipping-clutch pressure-feedback solution. Smooth oscillator and clutch
feedback refinements approach second order. Capture, branch independence, cancellation,
late numerical failure, retry, negative-pressure rejection and zero-allocation stepping
also pass. The initial allocation test exposed allocation in its own status formatting;
formatting is now limited to failures, and the measured hot loop allocates zero bytes.

Asset tests retain inlet pressure/topology and relief setting, reject malformed and
duplicate extensions, invalid dimensions/counts and forged downgrades. The authentic
v10 fired-hydraulic fixture retains fingerprint `01b69cb3abe52211` and final hash
`46a01d103e6159d3` after upgrade. Prior fixture digests and trajectories also pass.

The fired-pump laboratory has **89** matching report, portable and MCP boundaries over
0.8 s at 50,000-ns ticks. Its 56 counted states include a 4e-12 m³/Pa supply line, an
ideal 1e-6 m³/rad crank-driven pump and a 1e6-Pa relief setting with conductance
1e-9 m³/(s·Pa). Initial hydraulic energy is explicitly 3 J. Pump work is
**53.9425016232 J**, external hydraulic work **0 J**, and relief heat **45.0264051429 J**.
Final line pressure is **1.0697262404 MPa**, crank/turbine speed **69.7555356890 rad/s**,
load speed **6.6433843513 rad/s**, and transmission thermal-node temperature
**301.5306383749 K**. Total-energy residual is `1.0671e-9 J`; reference-volume residual
is `3.0493e-20 m³`. Fingerprint `d0bd8f29a706fd89`, final hash `572150ab5d66a2f6`.
Report: `artifacts/reports/fired-pump.json`.

All twelve laboratory documents pass JSON Schema; ten malformed pump/relief cases are
rejected. Audit: `artifacts/reports/pump-schema-audit.json`. Studio pump-port views and
import/Play lifecycle tests are prepared. `POWER_UNITY_EDITOR` is unset; Editor,
Play Mode and IL2CPP remain unverified. These managed checks are not Unity evidence.

Development is paused here at the owner's request. Pump losses/control, regulator
spool and actuator piston dynamics, complete DCT/AT, ECU/TCU, richer engine behavior,
calibrated vehicle samples and desktop acceptance remain unfinished. See
[the pump contract](HYDRAULIC_PUMP.md) and [development status](DEVELOPMENT_STATUS.md).

## 2026-09-22: hydraulic network and pressure-operated transmission

Added compliant hydraulic nodes, linear and regularized-turbulent restrictions,
explicit reservoir pressures, and pressure-operated clutches. Hydraulic pressure and
volume/work/heat histories participate in internal capture trials and whole-batch
transactions. JSON/schema, asset v10, agent capabilities 0.12.0 and the fired-hydraulic
laboratory share these definitions. See [the equations and limits](HYDRAULIC_NETWORK.md).

Verified locally on Linux with:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **154/154** managed checks and **123/123** Core/Assets Standard-assembly checks on .NET 10.
- **14/14** MCP groups against an actual child server process.
- **16/16** Zig and **6/6** Python ABI checks; all **176** historical values match exactly.
- Release build has zero warnings/errors; source audit passes.
- Log: `artifacts/reports/hydraulic-integration-verify.log`.

New physical evidence includes signed flow/passivity/range checks, analytic RC charging,
closed equalization, exact reservoir-work and thermal identities, independent RK4
nonlinear flow integration, second-order pressure and pressure-driven clutch-impulse
refinement, preload and capture/release. Failure after accepted hydraulic/clutch history,
cancellation, invalid input, forks and batching preserve the complete transaction.
Capture and snapshot reads allocate zero bytes after warm-up. An oversized drain step
rejects negative gauge pressure without changing state.

Portable replay exposed an omitted reservoir-pressure field during implementation.
The v10 restriction record now explicitly carries it, and roundtrip tests compare the
full physical descriptors and every replay boundary. Re-signed malformed records,
duplicate/missing extensions, wrong dimensions, invalid actuator ports and hydraulic-only
node downgrades are rejected. Authentic v9 fixture SHA-256
`96a78326ae2f2e8dd4a434fb81fbe88f4a19156cbf13cf069a7bd4e798e93c9f` retains fingerprint
`839d03901973668d` and final state `834a679376b7a6fd` when upgraded. Older fixtures remain.

The fired-hydraulic laboratory has **89** matching report/portable/MCP boundaries over
0.8 s at 50,000-ns ticks. Fingerprint `01b69cb3abe52211`, final hash `46a01d103e6159d3`.
Final crank/turbine speed is 70.94321138 rad/s; load speed is 6.75649632 rad/s. Reservoirs
supply 8 J, restrictions dissipate 7 J and hydraulic stored energy increases by 1 J.
Converter heat is 48.80297187 J; lockup heat 32.89304173 J; shift clutch/brake heat
119.31915873 J and 60.16098886 J. The shared heat node reaches 301.34088081 K.
Total-energy residual is `1.0896e-9 J`; reference-volume residual is `-1.0804e-18 m³`.
Net external source work is -65.07102675 J, including hydraulic supply, load and cylinder
back-pressure work. It is not a direct measurement of load-only work.

All eleven laboratory documents validate against the schema; ten malformed hydraulic
contracts are structurally rejected. The compiler adds dimension/topology/range checks.
Audit: `artifacts/reports/hydraulic-schema-audit.json`. Studio hydraulic views and
import/Play tests are prepared, but `POWER_UNITY_EDITOR` is unset; actual Editor,
rendering, Play Mode and Player/IL2CPP are unverified. Pumps/regulators, piston and
accumulator dynamics, complete DCT/AT, engine/controls and calibrated samples remain open.

The earlier source-work descriptions below now identify net external work explicitly:
that ledger includes cylinder back pressure, so its magnitude must not be labeled as
load-only output work. This is an evidence-description correction, not a physics change.

## 2026-09-22: coupled torque converter and fired lockup/shift laboratory

Added four explicit signed converter maps, passive interpolation validation,
stationary-stator reactions, fluid heat, and a joint converter/cylinder solve integrated
with gear constraints and clutch events. JSON/schema, portable v9, agent capabilities
0.11.0 and the `fired-converter` example share this contract. Studio views and Editor/Play
tests are prepared. See [CONVERTER_NETWORK.md](CONVERTER_NETWORK.md).

The full serial command:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

Local Linux evidence:

- **143/143** managed checks; **114/114** Core/Assets Standard-assembly checks on .NET 10.
- **13/13** MCP groups against an actual child server.
- **16/16** Zig and **6/6** Python ABI tests; all **176** historical numbers match exactly.
- Release build: zero warnings/errors; source audit passes.
- Log: `artifacts/reports/converter-integration-verify.log`.

New checks cover signed maps and reference-member continuity, stator/energy balance,
interior passivity violations, strict units, immutable point ownership, overflow failure,
analytic fluid coupling and stall, second-order refinement, reverse/coast/counterrotation,
shared ports, thermal/external loss routing, gear reflection and parallel lockup. Failure,
cancellation and branch checks preserve complete state; internal capture allocates zero
bytes after warm-up. Portable checks reject re-signed bad counts, wrong indices/units,
duplicate maps and downgrades. The authentic v8 fixture retains SHA-256
`6872f857bc521ed114d6eea5837cbb380a01f374ddfe7e829b30afa2c44fe0aa`, fingerprint
`6703f00c995e6b62` and final state `b328de221532fbae` when read or upgraded. Older fixtures
remain unchanged.

The 0.8-s fired-converter experiment uses 50,000-ns ticks and exactly replays all **87**
boundaries through the report, portable asset and MCP. Fingerprint `839d03901973668d`,
final hash `834a679376b7a6fd`; pump/turbine speed 73.37747546 rad/s and load speed
6.98833100 rad/s. Fluid heat is 24.27663069 J, lockup heat 22.84709072 J, shift-clutch
heat 157.18199410 J and brake heat 83.42288714 J. Thermal node 5 ends at 301.43864301 K,
with total-energy residual `3.2969e-11 J`. Net external source work is -63.19344680 J, including load and cylinder back-pressure
work, while tracked fuel releases 2049.02269691 J. These are synthetic numerical outputs.

A five-step study at 50,000/25,000/12,500/6,250/3,125 ns verifies shrinking combined
normalized distance in final crank speed, fluid heat and lockup heat relative to the
finest run, plus absolute differences below 0.0002 rad/s or J respectively. Individual
heat differences are nonmonotone near clutch events; no uniform coupled convergence
order is claimed. The finest run gives 73.37753852 rad/s, 24.27656820 J and 22.84711838 J.
Separate smooth analytic tests retain a refinement factor greater than 3.9.

Actual Unity Editor, Play Mode, rendering and IL2CPP remain unverified:
`POWER_UNITY_EDITOR` is unset. Complete engine behavior, DCT/AT topology, hydraulics,
controls and calibrated vehicle samples remain open. Quasi-steady maps do not establish
fluid dynamics or measured converter performance.

## 2026-09-22: coupled ideal gears and fired planetary transmission

Ideal gears and three-port planetary constraints now share the electromechanical,
cylinder and clutch solve. Direct constraint projection preserves compatible motion
and initial relative phase; per-port mean reactions are observable and transactional.
JSON/schema, asset v8, CLI/MCP and Studio use the same topology. See
[the gear contract and numerical limits](GEAR_NETWORK.md).

The full serial command passed on Linux x64 with cached .NET SDK 10.0.400/runtime
10.0.11 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **131/131** managed Core/application checks.
- **105/105** Core/Assets Standard-assembly checks hosted on .NET 10.
- **12/12** actual-child-server MCP groups.
- **16/16** Zig groups, **6/6** Python ABI tests and **176** exactly matching historical
  baseline values. Source audit finds no C/C++ or Lua implementation files.
- Release build reports zero warnings/errors. Log: `artifacts/reports/gear-integration-verify.log`.

Nine graph groups compare positive/negative ratios and free planetary motion/reactions
with independent exact references, multi-stage reflected inertia and stable-ID ordering,
RL motor/thermal and reacting-cylinder equivalence, and analytic reduction/direct shift
capture and heat. A constrained oscillator demonstrates second-order convergence and
energy conservation. Complete rollback after an accepted shift prefix, cancellation,
branch independence, exact batched replay and zero allocation are checked; allocation
coverage includes internal capture with variable factors. Rank, initial-speed, port,
ratio and unsupported-parameter diagnostics are explicit.

Two asset groups cover three-port topology, every playback boundary, malformed counts,
missing/duplicate/wrong-kind records, invalid carriers and downgraded gear attempts.
A genuine v7 fired-clutch fixture keeps fingerprint `197be44884deee90` and final hash
`28bf5335d8e35cde` after upgrading. Older fixtures and gear-free models remain unchanged.
Two managed integration groups add strict JSON/agent contracts, revision/cancellation/
branch behavior and the distinction between successful execution and passing KPIs.

The new fired planetary laboratory has **84 matching boundaries** across alternate
batches, portable playback and MCP. Its 0.8-second upshift/downshift experiment records
**-56.83157714 J** of net external source work, generating **254.52399968 J** in the sun/ring clutch and
**156.31560557 J** in the ring brake. The thermal node ends at **302.05419803 K**;
crank/load speeds are **76.81548837 / 7.31576080 rad/s**, with the ring held. Final
energy residual is **2.51020538e-10 J**. Fingerprint is `6703f00c995e6b62`; final hash
is `b328de221532fbae`. Source/report are `assets/labs/fired-planetary.power.json` and
`artifacts/reports/fired-planetary.json`. Parameters remain synthetic and `unverified`.
Disabling the shift schedule removes sun/ring clutch heat and changes load motion.

Studio has schematic three-port planetary and final-drive views with prepared import,
shift replay, reset and cleanup tests. `POWER_UNITY_EDITOR` is unset: no Editor/Play/
IL2CPP evidence is claimed. Complete DCT/AT topology, converter, hydraulics, ECU/TCU,
remaining engine behavior, measured vehicle calibration and release acceptance remain open.

## 2026-09-22: independent ideal gear and planetary references

Added immutable `IdealGearPair` and `SimplePlanetaryGear` primitives for constant external
torques. Results expose member speeds, displacements, reaction torques, work, kinetic
energy change and residual. Initial speeds must satisfy the constraint; no finite-slip
synchronization is inferred. See [equations, signs and limits](IDEAL_GEARS.md).

The full serial command passed on Linux x64 with cached SDK 10.0.400/runtime 10.0.11:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **118/118** managed Core/application checks and **94/94** Core/Assets Standard-assembly
  checks hosted on .NET 10; eight new groups run against each Core target.
- **11/11** actual-child-server MCP groups, **16/16** Zig groups and **6/6** Python ABI
  tests. All **176** historical baseline values match exactly; the source audit finds
  no C/C++ or Lua implementation files.
- Release build has zero warnings/errors. Log:
  `artifacts/reports/ideal-gear-reference-verify.log`.

New checks cover positive/negative gear ratios, reflected inertia, per-member impulse
balance, zero reaction power, independent planetary constraint-force dynamics, three
held-member conditions and sun/ring direct drive. Holding and locking loads are explicit.
Constant-load results match partitioned intervals, including speed reversal. Midpoint
sampling of sinusoidal loads converges against independent integrals at about fourfold
error reduction per interval halving for both primitives.

The deterministic sweep includes **2,500 cases per reference**. **10,000 evaluations of
each primitive** allocate no managed memory; independent concurrent callers share only
immutable parameters. Invalid values, incompatible initial speeds, constructor conditioning,
arithmetic overflow and finite force-cancellation errors reject without a partial result.
A high-ratio regression preserves a small, physically required reaction instead of losing
it by subtraction of nearly equal torques.

The graph solver and asset v7 semantics are unchanged; existing laboratory, portable and
MCP replay checks remain passing. These primitives are not yet coupled transmission
components, agent tools, shift simulations or calibrated models. Actual Unity
Editor/Play/IL2CPP verification remains pending, as do the rest of the engine, DCT/AT,
hydraulics, controls and complete calibrated vehicle objectives.

## 2026-09-22: coupled clutches and fired-engine load integration

Clutch static/kinetic reactions now share the electromechanical/cylinder solve, with
bounded internal capture/reversal events and friction-heat routing. Phase, mean outputs
and compensated heat are transactional and hashed. JSON/schema, CLI/MCP, asset v7 and
Studio consume the same component; v1–v6 readers remain supported. See
[the equations and explicit numerical limits](CLUTCH_NETWORK.md).

The full serial command passed on Linux x64 with cached .NET SDK 10.0.400/runtime
10.0.11 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **110/110** managed Core/application checks.
- **86/86** Core/Assets Standard-assembly checks hosted on .NET 10.
- **11/11** actual-child-process MCP integration groups.
- **16/16** Zig groups and **6/6** Python ABI tests; all **176** historical values
  match exactly. The source audit finds no C/C++ or Lua implementation files.
- Release build reports zero warnings and errors. The local log is
  `artifacts/reports/clutch-integration-verify.log`.

New physical evidence compares the graph with the exact constant-load `ClutchPair`
through internal engagement and reversal, positive/negative ratios and both thermal
destinations. Locked motor/current and reacting-cylinder pressure/fuel trajectories
match separate models with analytically combined inertia. A spring/brake oscillator
matches piecewise sinusoidal motion through three reversals and final capture at the
fourth turning point; halving the tick reduces error by more than 3.7x. Three-clutch
loops exercise redundant constraints and simultaneous engagement with conserved motion
and energy. Static release requires saturation; root-resolution residue cannot create
a spurious second reversal.

Complete multi-tick rollback is checked after an accepted heating/capture prefix and a
later numerical overload. Scheduled replay, cancellation, fork independence, immutable
ownership and zero allocation are retained. Allocation checks include repeated internal
reversal events, exercising candidate copies and variable factors. These tests support
the documented solver scope, not arbitrary large-tick hybrid accuracy.

The new `fired-clutch` laboratory has 67 exactly matching report boundaries across
alternate batches, portable playback and MCP. Its 0.6-second report records 96.74607609 J
of net exported external work, including the load and cylinder back pressure, 191.55570747 J of clutch heat, final engine/load speed
68.58488546 rad/s, and a final energy residual of 1.79e-10 J. Its fingerprint is
`197be44884deee90` and final state hash `28bf5335d8e35cde`. The clutch thermal node reaches
300.95777854 K. The source and result are `assets/labs/fired-clutch.power.json` and
`artifacts/reports/fired-clutch.json`; parameters remain synthetic and `unverified`.

Asset v7 round trips capacities and channels, rejects malformed/missing/duplicate
extensions and invalid downgrades, and preserves an authentic v6 fired-cylinder fixture's
digest, fingerprint and upgraded replay. Earlier model hashes remain unchanged.
Strict JSON/agent checks retain actionable errors, input/revision atomicity and the
distinction between successful execution, passing KPIs and measured calibration.

Studio's clutch plates, named phase outputs and import/lifecycle tests are prepared.
`POWER_UNITY_EDITOR` is still unset: Editor, rendering, Play Mode and IL2CPP remain
unverified. DCT/AT topology, planetary sets, torque converter, hydraulics, controls,
complete engine behavior and calibrated vehicle samples remain unfinished.

## 2026-09-22: managed dry-clutch law and constant-load reference

Added `DryClutch`, an immutable static/kinetic torque-capacity law, and `ClutchPair`,
an exact constant-load two-inertia or ground-brake reference. A slip-zero event is
resolved inside the interval, followed by constrained motion or reversal. Results expose
motion, angular advances, reaction mode, impulse, heat, external work and energy change.
See [the equations, API and implementation boundary](CLUTCH_PHYSICS.md).

The serial command passed on Linux x64 with cached .NET SDK 10.0.400/runtime 10.0.11
and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **99/99** managed Core/application checks and **77/77** Standard-assembly checks
  hosted on .NET 10, including the same ten new clutch groups in both targets.
- **10/10** actual-child-process MCP integration groups.
- **16/16** Zig groups and **6/6** Python ABI tests. All **176** historical numerical
  values match exactly. The source audit finds zero C/C++ or Lua implementation files.
- Release build reports zero warnings and errors. Full output is retained locally at
  `artifacts/reports/clutch-kernel-verify.log`.

Physical evidence covers analytical engagement, exact static load sharing, breakaway,
reversal, endpoint events, partial engagement, a ground brake and signed gear ratios.
The tests independently check momentum, integrated external work and absolute kinetic
energies, rather than comparing only the implementation's energy counters. A pair with
inertias 0.2 and 0.8 kg m2, initial speeds 100 and 0 rad/s, and sliding capacity 10 Nm
synchronizes at 20 rad/s after 1.6 s, generating 800 J of heat.

Constant-load solutions agree across interval partitions that cut through hybrid events.
Midpoint-frozen sinusoidal loads converge against independent velocity, angle and heat
integrals by more than 3.8x per halving, with the finest maximum error below 2e-5 in the
tested SI outputs. A deterministic 2,000-case range sweep checks conservation and repeat
evaluation. It found and fixed one-ulp overcounting of the sliding duration during a
reversal. Invalid data and arithmetic/event-resolution failures publish no partial result.
The warmed, isolated measurement path records zero allocation for 10,000 intervals.

This is a standalone Core physics primitive, **not yet a compiled graph component**.
Shaft/motor/cylinder coupling, multiple-clutch constraints, thermal routing, transactional
hybrid state, JSON/asset/MCP representation and Studio integration remain pending.
Existing graph fingerprints, seven laboratory experiments and asset v6 semantics remain
unchanged. Their previous combustion evidence is retained below.

`POWER_UNITY_EDITOR` remains unset. These Standard-assembly tests do not establish actual
Unity import, Play Mode or IL2CPP behavior. All vehicle parameters remain `unverified`;
the new primitive does not complete a transmission, controls or a calibrated powertrain.

## 2026-09-22: premixed combustion, transported reactants and fired load work

Added optional fuel/fresh-air/product tracking to gas nodes and explicit reservoir
fractions, plus a crank-referenced `premixed_combustion` component. A prescribed Wiebe
hazard consumes limiting reactants, stores irreversible crank history, and converts
chemical energy into thermal energy. Heat preview participates in conservative crank
work. See [the model, equations and limitations](PREMIXED_COMBUSTION.md).

Full serial verification passed on Linux x64 with cached .NET SDK 10.0.400/runtime
10.0.11 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **89/89** managed Core/application checks.
- **67/67** Core/Assets Standard-assembly checks hosted on .NET 10.
- **10/10** actual-child-process MCP integration groups.
- **16/16** Zig groups and **6/6** Python ABI tests, including both native hosts.
- All **176** historical native values match exactly; zero C/C++ or Lua sources found.
  Release build reports zero warnings and errors.

New physical evidence includes:

- Analytic Wiebe exposure through explicit cycles, negative phases and cycle wrap.
  Closed-vessel fuel, fresh-air consumption, heat and temperature match the closed-form
  limiting-reactant solution for lean, rich, fuel-free and air-free charges. Mass and
  total thermal-plus-chemical energy are checked independently of the heat counter.
- Closed-network constituent conservation and reservoir filling/discharge carry the
  upstream composition and chemical enthalpy in either flow direction. A constant-mass,
  constant-temperature vessel with balanced choked inflow/outflow matches exponential
  mixture replacement within 2e-5 in mass fraction, even with more than one vessel
  turnover per outer tick. This exercises the outgoing-flow bound when net mass and
  thermal-energy rates alone provide no useful tracer timestep.
- An independently written reacting-cylinder RK4 reference integrates crank motion,
  mass, thermal energy, fuel and fresh air with choked discharge. Its 1 and 0.5 microsecond
  solutions differ by less than 1e-9 normalized. Core ticks of 100, 50 and 12.5 microseconds
  reduce maximum normalized error by more than 2.8x and then 8x, with the finest below
  1e-4. This is wall-free second-order evidence; wall coupling remains first order.
- Multiple reacting cylinders on shared or shaft-coupled cranks conserve total energy
  and constituents, including isolated mixtures with different heating values and
  stoichiometric ratios. Stopping, reversing and retracing cannot repeat heat release;
  disabling a burn skips forward exposure without later catch-up. The greatest visited
  crank angle is observable as `burn_frontier_angle`.
- Failed scheduled torque changes after partial reaction roll back constituent state,
  angle history and compensated ledgers. Cancellation, caller-batch independence,
  fork isolation, under-resolved-burn rejection/recovery and zero stepping/snapshot
  allocations pass against both assemblies. Strict composition, ownership, units and
  the extended 64-state budget are checked.

The [fired-cylinder laboratory](../assets/labs/fired-cylinder.power.json) passes its KPIs
with fingerprint `a10f880d74494677` and **63** matching report boundaries. JSON/CLI, MCP
and decoded asset v6 agree on every channel at every boundary, including two load events
between report times. The 0.6-second run records **-369.98 J** of net external source work,
consumes **3.265e-5 kg** fuel in reaction and releases **1436.67 J**. Final net boundary
fuel energy is **1785.07 J**, with fuel also remaining in the chamber; these transient
numbers are not a steady efficiency or fuel-economy claim. Disabling combustion removes
heat release and produces substantially lower crank speed under the same load.

Final energy residual is approximately **-2.11e-9 J**, total mass residual **-1.25e-18 kg**,
fuel residual **2.03e-20 kg** and fresh-air residual **1.41e-18 kg**. Sampled pressure peaks
at about **2.08 MPa** and temperature at **1761 K**. These are synthetic model outputs;
the 10 ms report sampling does not establish the continuous pressure/temperature peak.

Asset v6 retains v1–v5 readers. New tests preserve model/mixture/burn definitions and
replay, reject wrong/missing/duplicate extension semantics and malformed counts, and
check an authentic pre-change v5 fixture's digest, fingerprint and upgraded replay.
[Fixture provenance](../tests/Power.Tests/Fixtures/README.md) records its uncommitted
source checkpoint without claiming a published commit. Existing nonreacting fingerprints
remain unchanged. Agent tests cover structured validation, invalid input and cancellation
without revision changes, stale writes, branch independence, filtered fuel/heat outputs,
smaller-tick recovery and the distinction between successful execution and failed KPIs.

The Draft 2020-12 schema and all **seven** laboratories pass Python `jsonschema`. Eight
malformed composition/burn shapes are rejected, including missing fractions, unknown
constituents, wrong units, invalid fractions, missing burn parameters and misplaced/null
reservoir fractions. Compiler checks separately enforce fraction sums and connected-mixture
compatibility. Local evidence is in `artifacts/reports/combustion-verify.log` and
`artifacts/reports/fired-cylinder.json`.

Unity import and Play Mode tests now include a heat-release marker, reset and complete
fired-example replay. **They have not run in the Editor**: `POWER_UNITY_EDITOR` is unset.
No rendering, Mono/IL2CPP or Player claim is inferred from Standard-assembly tests.
Constant R/gamma, prescribed burning and the forward-frontier policy are explicit limits;
fuel metering, ignition control, predictive chemistry, detailed intake/exhaust, mechanical
losses, transmissions, controls and calibrated vehicle samples remain open.

## 2026-09-22: crank-angle valve timing and changing-speed motoring

Added optional `valve_timing` on gas restrictions, with explicit 360/720-degree cycles,
opening/duration angles, peak input and effective-opening output. Profiles follow actual
crank angle through acceleration, stopping, reversal and phase wrap. Under-resolved lobes
reject the complete batch. See [the equations, bounds and scope](VALVE_TIMING.md).

The full serial command passed on Linux x64 with cached .NET SDK 10.0.400/runtime
10.0.11 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **76/76** managed Core/application checks.
- **57/57** checks against Core/Assets .NET Standard 2.1 assemblies hosted on .NET 10.
- **9/9** actual-child-process MCP integration groups.
- **16/16** Zig groups and **6/6** Python ABI tests, with both native hosts executed.
- All **176** historical native values match exactly; source audit finds zero C/C++ or
  Lua files. Release build reports zero warnings and errors.

Additional physical evidence:

- Closed-form adiabatic choked blowdown with independently integrated sin-squared valve
  exposure, at +40 and -40 rad/s, tests timing-to-flow coupling across cycle wrap. Refining
  ticks from 1 ms to 0.5 ms reduces relative mass error by more than 2.8x; 0.125 ms reduces
  it by more than a further 8x, to below 1e-7. Energy/mass ledgers are checked separately.
- An independent moving-cylinder RK4 reference includes crank pressure work, choked flow
  and a narrow timed lobe. The trajectory crosses both lobe boundaries. Halving reference
  steps from 1 to 0.5 microseconds changes normalized results by less than 1e-10. Core ticks
  of 200, 100 and 25 microseconds reduce error by more than 2.8x and then 8x, with finest
  error below 1e-6. Wall-free second-order evidence does not change the documented
  first-order wall coupling.
- Exact constant-torque kinematics checks opening during deceleration/reversal; stationary
  and disabled valves retain their documented behavior. A tick spanning an entire narrow
  lobe with closed endpoints must fail and roll back. Reducing the tick resolves its flow.
- Cancellation, failed schedules, caller-batch independence, fork isolation, malformed
  timing parameters and zero-allocation stepping/snapshots pass against both assemblies.

The [crank-timed laboratory](../assets/labs/crank-timed-cylinder.power.json) passes all
KPIs with fingerprint `38f0437eac4def69` and **63** matching report boundaries. JSON/CLI,
MCP and decoded asset v5 agree at every boundary, including two torque events between
report times. Final energy residual is approximately **8.53e-10 J**, and mass residual
is **-2.87e-18 kg**. Sampled crank speed ranges from **53.25 to 63.34 rad/s** while opening
is independently checked against crank angle. These are numerical checks of synthetic
parameters, not calibration.

Asset v5 retains v1–v4 readers. Tests reject malformed counts, duplicate/wrong timing
records and removed timing semantics, and verify an authentic pre-change v4 fixture
with its original digest, fingerprint and upgraded replay. Fixture provenance is recorded
[in the fixture notes](../tests/Power.Tests/Fixtures/README.md). Previous model fingerprints
remain unchanged. MCP tests also preserve state/revision on invalid peak input, and
application checks distinguish successful execution from failed KPIs and demonstrate
recovery from a narrow-lobe runtime failure by recreating with a smaller tick.

The Draft 2020-12 schema and all **six** laboratory documents pass Python `jsonschema`.
Six malformed timing shapes are rejected, including absent fields, extra profile fields,
wrong units, an invalid component placement and null timing. Compiler tests separately
cover cycle/range and topology restrictions.

Evidence is local Linux execution, recorded in `artifacts/reports/valve-timing-verify.log`
and `artifacts/reports/crank-timed-cylinder.json`. New Unity import/Play tests check timed
markers, reset and replay, but **have not run in the Editor**: `POWER_UNITY_EDITOR` is
unset. No Editor, rendering, Mono/IL2CPP or desktop Player result is inferred from managed
checks. Combustion, full engine behavior, transmissions, controls and calibrated vehicle
samples remain open; all research parameters remain `unverified`.

## 2026-09-22: moving-cylinder gas exchange and conservative crank work

Added `gas_cylinder`, a geometry component connecting a rotational crank and a gas
chamber with independent mass/internal energy. Initial volume is derived from crank
position and geometry; ambiguous volume/ownership is rejected. Gas exchange, crank
work and wall transfer run through whole-batch rollback, forks and cancellation.
The same definitions are accepted by JSON/CLI/MCP and portable asset v4, with v1/v2/v3
readers retained. See [the equations and contract](MOVING_CYLINDER.md).

Full serial verification passed on Linux x64 with cached .NET SDK 10.0.400/runtime
10.0.11 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **68/68** managed Core/application checks.
- **51/51** checks against Core/Assets .NET Standard 2.1 assemblies hosted on .NET 10.
- **8/8** actual-child-process MCP integration groups.
- **16/16** Zig groups and **6/6** Python ABI tests, with both native hosts executed.
- All **176** original native baseline values match exactly; source inventory contains
  zero C/C++ and Lua files. Release build reports zero warnings and errors.

New physics checks cover:

- Closed-valve agreement with the sealed-cylinder benchmark during forward/reverse
  rotation, dead centers and tiny ticks; constant mass and conserved energy.
- Open choked-flow motion compared with an independently written RK4 integration of
  mass, energy and crank ODEs. Its geometry and flow equations do not call the Core
  helpers under test. Halving the reference timestep from 1 to 0.5 microseconds changes
  normalized results by less than 1e-10. Reducing the Core tick from 200 to 100 microseconds
  reduces smooth-flow error by more than 3x; 25 microseconds reduces it by more than a
  further 10x and stays below 1e-6 relative.
- Wall-coupled refinement is assessed separately as first order: the same refinements
  reduce error by more than 1.7x and 3x respectively, with finest error below 1e-5 relative.
- Shared/coupled cranks, mixed sealed and open cylinders, gas links, wall heat and full
  energy/mass ledgers. Failed scheduled torque changes restore all earlier ticks and
  inputs; cancellation, fork isolation and zero stepping/snapshot allocations pass.

The new motoring laboratory has fingerprint `dd62971021fa06e6` and **28** replay
boundaries, all identical between JSON experiment reports, decoded assets and actual
MCP export. It demonstrably admits and expels gas while the chamber moves. Final energy
residual is `2.9882230023758893e-10 J`; mass residual is `1.463672932855431e-18 kg`.
These are numerical conservation observations for synthetic parameters, not calibration.
The preceding four laboratory reports retain their fingerprints and pass their replay/KPIs.

Portable coverage includes mixed old/new cylinder records, exact geometry round trip,
wrong-type/duplicate/missing extensions, invalid counts and rejection of moving-cylinder
records under older versions. The saved v3 fixed-volume fixture retains fingerprint
`eeb18a7f1dc76175` and replay after v4 re-encoding. Fixture source/digest provenance is
recorded in [Fixtures](../tests/Power.Tests/Fixtures/README.md).

The Draft 2020-12 schema and all five laboratories pass Python `jsonschema`; six malformed
moving-cylinder documents are rejected. Agent tests cover geometry errors, chamber
ownership, bounded nonlinear failure without revision/state changes and recovery.
Logs are `artifacts/reports/moving-cylinder-verify.log` and
`artifacts/reports/moving-cylinder-schema.log`; the experiment is
`artifacts/reports/moving-cylinder.json`.

Unity moving-piston/import/Play tests are prepared but not executed: `POWER_UNITY_EDITOR`
is unset. Unity Editor/Play/rendering, Mono/IL2CPP, Player packaging and this increment's
Windows/macOS execution remain unverified. Time-scheduled restriction openings do not
implement crank-angle valve timing. Combustion, full engine-cycle behavior, transmissions,
controls and calibrated vehicle samples remain open; the complete Power! objective is
not complete.


## 2026-09-22: finite gas-network JSON, asset and agent integration

The Core checkpoint at `69bc1c4` was verified before changes: **53/53 managed,
41/41 Standard-assembly and 6/6 MCP groups**. The existing solver equations,
fingerprint construction and physical limits are unchanged in this increment.

Full serial verification then passed on Linux x64 using the cached pinned .NET SDK
10.0.400 and Zig 0.15.2:

```sh
.cache/dotnet/dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify
```

- **60/60** .NET 10 Core/application checks.
- **45/45** checks against the .NET Standard 2.1 Core/Assets assemblies, hosted on .NET 10.
- **7/7** MCP integration groups against an actual child server.
- **16/16** native Zig groups and **6/6** Python ABI tests; both native hosts executed.
- All **176** original native baseline values match exactly; zero C/C++ and Lua files.
- Release compilation: **zero warnings and zero errors**.

The four laboratory reports pass KPIs and replay: electrothermal (11 boundaries),
thermal network (11), sealed cylinder (21) and gas network (14). The gas document
compiles to the same fingerprint as an independently assembled Core definition:
`eeb18a7f1dc76175`. JSON reports, decoded v3 playback and actual MCP export agree at
every gas report boundary, including valve events between sampling boundaries. Mass
and energy residual checks use absolute limits of 1e-14 kg and 1e-6 J respectively;
the test also reconstructs reservoir energy exchange from chamber and wall states.

Portable checks include mixed gas/cylinder/thermal topology, non-default gas
composition, non-SI units, ownership, schedule bounds, cancellation and mid-batch
failure with event-cursor rollback. Correctly rehashed but invalid files cover counts,
missing/duplicate/wrong-type extension records, old-version gas rejection and stale
fingerprints. Authentic v1 and cylinder v2 fixtures retain their original fingerprint
and replay behavior after v3 re-encoding; the v2 fixture's source commit and hash are
recorded in [Fixtures](../tests/Power.Tests/Fixtures/README.md).

Agent checks cover discoverability, initial/scheduled opening bounds, invalid-input
atomicity, revision conflicts, cancellation, branch independence and the distinction
between a successful call and a failing KPI. Separately, Python `jsonschema` validated
the published Draft 2020-12 schema, all four laboratory documents and a fixed-opening
variant, and rejected twelve malformed gas document cases.

Logs: `artifacts/reports/gas-integration-baseline.log`,
`artifacts/reports/gas-integration-verify.log` and
`artifacts/reports/gas-integration-schema.log`. Reports and generated Unity assets
remain reproducible build artifacts rather than source fixtures.

`POWER_UNITY_EDITOR` is unset. Gas schematic views, import tests and a Play Mode
lifecycle/replay test are prepared but **not executed in Unity**. Editor/Play/rendering,
Mono/IL2CPP, Player packaging and this increment's Windows/macOS execution remain
unverified. Full engine-cycle physics, transmissions, controls and vehicle calibration
remain open; sample parameters stay `unverified`.

## 2026-09-19: Python toolchain retired, native verification moved to C#

The repository no longer contains Python. `tools/Install Zig.py`, `tools/VerifyNative.py`,
`legacy/native/tools/model_lab.py` and their tests were ported to C# and folded into
the single-file `tools/Build.cs` build tool (.NET 10 file-based apps allow one source file).
The ctypes host became P/Invoke. The external ABI consumer attributes are unchanged. The Zig
installer uses the built-in ZIP extractor on Windows and delegates `tar.xz` platforms to the
system `tar`. CI and the docs were updated in the same change.

Local serial verification on Windows x64 (`install-zig` + `native-verify`):

- Source audit: 0 C/C++ files, 0 Lua files, 35 Zig sources, 38 migration-manifest entries.
- Full `verify` (Windows x64, local, serial): 53/53 managed checks, 41/41 .NET Standard assembly checks,
  6/6 MCP integration groups, 16/16 native Zig tests. `power_host` and `power_model_host` run.
- 6/6 ported C# ABI tests pass, including cleanup of 70 failed compile slots, rejection of 15 document mutations,
  and failing-KPI exit code 2.
- Baseline comparison: **176/176** original C baseline values match exactly (maximum absolute error 0.0).

## 2026-09-14: compiled gas-network Core checkpoint

Recovered the WSL work through `4a81716` and its unfinished Core integration. The
checkpoint now compiles gas-only and mixed gas/thermal networks, validates composition
and opening ranges, and includes mass/internal-energy state and reservoir ledgers in
snapshots, hashes, forks and whole-batch rollback. A conservative stage limiter prevents
an isolated equalising pair from oscillating through equilibrium. The unchanged sealed
cylinder and linear models retain their prior fingerprints and replay behavior.

Serial verification using the pinned cached .NET SDK 10.0.400 on Linux x64:

- **53/53** .NET 10 Core/application checks.
- **41/41** checks against the .NET Standard 2.1 Core/Assets assemblies on .NET 10.
- **6/6** MCP integration groups against an actual child server.
- Existing electrothermal, thermal and cylinder experiment replay reports pass.
- Native Zig groups and six Python ABI tests pass; all **176** original baseline values
  match exactly. Source audit: zero C/C++ files and zero Lua files.

Gas-specific evidence includes choked/subcritical nozzle physics, analytic vessel
blowdown and refinement, reservoir filling enthalpy, reverse flow, closed-network mass
and energy conservation, finite-time analytic wall exchange, closed-valve isolation,
input/schedule range rejection, observable overflow, mid-batch failure/recovery, branch
and batching equivalence, immutable compilation and zero stepping/snapshot allocations.
Asset v1/v2 rejection is tested to prevent dropping unsupported gas fields.

See [GAS_NETWORK.md](GAS_NETWORK.md) for the numerical method and remaining scope.
This checkpoint has local Linux evidence; current Windows/macOS CI status must be read
from its commit's workflow. Unity Editor/Play/IL2CPP and calibrated vehicle behavior
remain unverified. The earlier standalone record below describes the preceding commit.

## 2026-09-14: gas-exchange physics (standalone)

Added the first slice of the gas-exchange increment as **physics only**: `IdealGas`,
`GasVolumeState` and `Orifice` in `src/Power.Core/GasExchange.cs`. Finite volumes now
carry mass and internal energy as independent states, and the orifice implements the
standard isentropic nozzle relations in both directions with a discharge coefficient and
a dimensionless opening fraction. `Numeric.Expm1` and `Numeric.Log1p` moved out of
`CylinderPhysics` and are shared; the implementations are unchanged, and every existing
cylinder state hash, model fingerprint and replay boundary still matches.

**No node kind, component kind, channel, schema field or asset version changed.** A model
document still cannot contain a finite gas volume, and the CLI, MCP and Unity surfaces are
untouched. The proposed split method with backward-Euler flow remains unvalidated and
unadopted. See [gas exchange](GAS_EXCHANGE.md) for the equations, the numerical limits and
the full list of contracts that did not land.

Six new analytic checks in `tests/Power.Tests/GasChecks.cs`, each written against an
independent closed form rather than a recorded output: choking continuity and flow-function
monotonicity for gamma in {1.1, 1.3, 1.4, 5/3}; 54 nozzle cases against the NASA compressible
mass-flow relations; orifice contracts including exact reverse-flow antisymmetry, closed-valve
isolation and rejected states; adiabatic vessel blowdown against the analytic isentropic
solution to 1e-9 relative; the reservoir-filling identity dU = cp*T_supply*dm with the
evacuated-vessel limit T -> gamma*T_supply; and closed two-volume conservation to 1e-14
relative in mass and 1e-12 in energy with pressure equalisation.

Full serial `dotnet run --file tools/Build.cs -p:UseSharedCompilation=false -- verify`
passed on Linux x64 with the cached pinned .NET SDK 10.0.400 and Zig 0.15.2: **44/44 managed
checks** (38 before this change), **26/26 checks against the Unity-facing .NET Standard 2.1
assemblies**, **6/6 MCP process groups**, three experiment reports with 11, 11 and 21 replay
boundaries, **16/16 native Zig groups** and the Python foreign-ABI tests. Both Zig hosts ran
against the actual shared library, the source audit reported `c_source_files: 0` and
`lua_files: 0`, and all **176 baseline values matched the original C binary exactly**
(maximum absolute error 0.0). The log is `artifacts/reports/gas-exchange-verify.log`.

Windows and macOS were not exercised for this change, and Unity Editor, Play Mode, rendering
and IL2CPP validation remain pending as before. Sample parameters remain `unverified`.

## 2026-09-11: Lua packaging cleanup

Removed the remaining LuaInstaller launcher and its obsolete packaging README.
The launcher depended on the never-implemented `power_native` bridge and had
no active build or runtime callers. Original paths and SHA-256 hashes are
preserved in the [migration manifest](../legacy/native/migration-manifest.json)
and match the files at its recorded source revision. Historical design
documents retain their provenance; their Lua proposals are retired.

The source audit now rejects Lua source, bytecode and packages in addition to
C/C++ source and headers, and reports `lua_files: 0`. Temporary untracked
`.lua`, `.luau`, `.luac`, `.rockspec`, `.rock` and uppercase `.LUA` probes each
produced a failing exit status and a structured error identifying the file;
the clean tree passed afterward.

Full serial `dotnet run --file tools/Build.cs -- verify` passed on Linux x64:
**38/38 managed, 26/26 .NET Standard assembly, 6/6 MCP, 16/16 Zig and 6/6 Python
ABI checks**. Both native hosts ran, and all 176 baseline values matched exactly.
The log is `artifacts/reports/lua-removal-verify.log`. Core behavior, sample
evidence and license files are unchanged. Unity Editor was not exercised.

## 2026-09-11: native Zig migration

The owner resumed the native language migration on 2026-09-10. All **26 C
implementation/test/host files and 12 headers** were replaced with Zig. The
[migration manifest](../legacy/native/migration-manifest.json) records original
Git revision, file paths and SHA-256 hashes. No C/C++ source or headers remain
in the repository source inventory; the root verification command enforces
that constraint. License/exception files and sample evidence are preserved.

Full serial `dotnet run --file tools/Build.cs -- verify` passed on Linux x64
using the cached pinned .NET SDK 10.0.400 and Zig 0.15.2: **38/38 managed
checks, 26/26 checks against the Unity-facing .NET Standard 2.1 assemblies,
6/6 MCP process groups, 16/16 native Zig groups and 6/6 Python foreign-ABI
tests**. The native suite also passed all 16 groups in Debug with safety checks
enabled. Both Zig hosts ran against the actual shared library. The library
exports only `pwr_get_api` and has no unresolved ELF symbols. All **176 values
at 11 electrothermal sample times exactly matched** the original C binary on
this host, with the same model fingerprint and channel contracts. The
cross-toolchain fixture comparison still uses explicit tolerances, and
same-binary replay must match exactly. Reports are in
`artifacts/reports/zig-migration-verify.log`, `native-verification.json` and
`native-electrothermal.json`.

ReleaseSafe library/host cross-compilation also passed for **x86_64 Windows**
and **aarch64 macOS**. Cross-compilation is not execution evidence for those
systems. Local logs are `artifacts/reports/zig-cross-windows.log` and
`zig-cross-macos.log`.

GitHub Actions subsequently completed actual verification successfully on
**Linux x64, Windows x64 and macOS arm64**, each passing all **38/38 managed,
26/26 .NET Standard assembly, 6/6 MCP, 16/16 Zig and 6/6 Python ABI checks**.
Both shared-library hosts ran on each platform. All 176 native baseline values
matched exactly on all three runners, and their source inventories contained
zero C/C++ sources or headers. Evidence:
[run 34549950147](https://github.com/Water-Run/Power/actions/runs/34549950147),
code commit [`dd3de10`](https://github.com/Water-Run/Power/commit/dd3de10294949c8b2ffb49afc02a9c70b5808c00).
The Windows checkout pins Zig files to LF; macOS verification uses Zig's bundled
Darwin stubs to avoid the newer Apple SDK incompatibility described in the
[native build notes](NATIVE_ZIG.md#build-and-maintenance). Complete job logs are
retained locally under `artifacts/reports/zig-ci-34549950147-{linux,windows,macos}.log`,
with run metadata in `zig-ci-34549950147.json`. The subsequent documentation and
comment corrections change no executable code.

The native suite additionally covers the previously unbuilt automatic-powertrain
module, including replay, energy accounting, brownout and transaction rollback,
plus concurrent SDK lifetime handling. Three historical test entry points
silently returned success on failed checks; the port fixes propagation and
separates the engine's steady combustion, limiter and backpressure scenarios.
See [the migration notes](NATIVE_ZIG.md) for the preserved equations and
corrected experimental setup. The original CTest result alone was insufficient
because of those hidden failures.

This migration does not complete the managed engine/transmission objectives or
establish vehicle calibration. Unity Editor, Play Mode, rendering, Mono and
IL2CPP were not exercised. All sample calibration remains `unverified`.


## 2026-09-08: sealed-cylinder increment

Serial `tools/Build.cs verify` passed on Linux x64 with SDK 10.0.400 and runtime 10.0.11: **38/38 managed checks, 26/26 checks against the actual .NET Standard 2.1 assemblies, and 6/6 MCP process integration groups**. Release compilation reported zero warnings and errors. The log is `artifacts/reports/cylinder-verify.log`; all three laboratory JSON documents also passed the published JSON Schema using the local `jsonschema` validator.

The new checks cover analytic slider-crank geometry and derivatives, ideal-gas state identities, two-second conservation runs with and without back pressure, second-order convergence under step refinement, reverse rotation, tiny steps and dead centers, shared/coupled cranks with electrical and thermal components, nonlinear failure rollback, cancellation, forks, units, malformed cylinder extensions, and zero managed allocations in steady-state stepping/snapshots. A preserved v1 fixture decodes, retains its original linear fingerprint and replays identically after v2 export.

GitHub Actions repeated the same verification successfully on **Windows, macOS and Linux**, with 38/38, 26/26 and 6/6 checks and zero warnings/errors on every platform. Evidence: [run 34176008291](https://github.com/Water-Run/Power/actions/runs/34176008291), code commit [`6209df2`](https://github.com/Water-Run/Power/commit/6209df22876f9928ddc04a1a32845cd7efe087da). Complete job logs and status metadata are retained locally as `artifacts/reports/github-actions-34176008291.log` and `.json`. The subsequent documentation update changes no executable code.

The synthetic cylinder experiment passed its final KPIs and replayed exactly at 21 boundaries through JSON, asset playback and an actual MCP child server. Linux results: fingerprint `c64b61efdb827680`, final speed `153.00340249454544 rad/s`, pressure `118835.36885412445 Pa`, temperature `315.16234058802814 K`, final energy residual `8.7464e-10 J`, and maximum sampled absolute residual `8.9570e-10 J`. Full report: `artifacts/reports/sealed-cylinder.json`. These values establish numerical evidence for this sealed ideal-gas benchmark, not engine calibration.

Unity importer and Play tests now include the cylinder asset and schematic piston motion, but were **not executed**. Unity Editor, Mono, rendering and IL2CPP evidence remains pending. The active stack remains C#/Unity; the future Zig rewrite direction introduces no native runtime in this increment.

## Pause checkpoint: 2026-09-08

The owner requested wrap-up and a development pause after the cylinder increment. Executable source remains at verified code commit `6209df2`; the later commits update documentation only. The prospective gas-exchange extension was not applied, built or published. Its [resume notes](NEXT_ENGINE_STEP.md) distinguish proposed work from implemented capabilities. No further build was needed for this documentation-only checkpoint. Resume development only after an explicit owner instruction.

## Historical baseline: 2026-09-07


Environment: 2026-09-07, Linux x64, .NET SDK 10.0.400, runtime .NET 10.0.11. The executed result is the `tools/Build.cs verify` output and the generated reports.

Managed baseline at that date: 30/30 core, asset and agent checks, 19/19 standard-library assembly checks, and 5/5 MCP process integration groups passed. The Release build reported 0 warnings and 0 errors. The live MCP process discovered 12 tools and checked input and output schemas. Success and error responses were checked for required output fields and a compatible text result. The raw log is `artifacts/reports/managed-verification.log`.

## Evidence recorded then

- Core and Assets were compiled for both `net10.0` and `netstandard2.1`.
- Analytic checks covered constant torque, the RL response and thermal equilibrium. Halving the step checked second-order mechanical convergence and first-order thermal convergence.
- Positive and negative ratios were checked for generalized momentum, damping heat and conservation. Regenerative braking was checked for negative current and a decrease in source work.
- Input rejection, a later-tick overflow, pre-cancellation and buffer-capacity checks all confirmed that state and caller data are not partially modified.
- Model-description ownership, parallel independent instances, full-state forks and per-tick versus batch stepping were checked for agreement.
- A .NET thread allocation counter measured 0 managed allocations for the core hot path of input, step and snapshot combined. That count excludes compilation, reports and the Unity UI.
- Asset encode and decode kept source, model and events. A damaged digest, forged counts, a bad format version, a bad model fingerprint and extra bytes were all rejected.
- Scheduled inputs covered time zero, the end of a batch and events inside a presentation batch. A later numerical failure rolled the whole batch back. Asset playback measured 0 managed allocations when every tick carried an input change. Cancellation kept the event cursor.
- JSON reports and imported assets compared state hashes and output values at every report boundary, including events that do not fall on a 20 ms presentation boundary.
- The same physics checks loaded the actual .NET Standard 2.1 DLLs copied for Unity and checked their target framework. The host was still .NET 10, so this does not show that Mono or IL2CPP passed.
- Agent checks covered structured field diagnostics, filtered snapshots, the session limit, concurrent revision conflicts, cancellation, parent and child fork isolation, lifecycle and compact reports.
- The official MCP client started a real server child process and completed 12-tool discovery, input and output schemas, error recovery, session operations, a full experiment and asset export. The file digest was checked after Base64 decode, and imported playback was compared with the MCP experiment's final state.

The default electrothermal experiment runs for 10 seconds, drops to 4 V at 5 seconds and returns to 24 V at 6 seconds. Two batch sizes agree bitwise at 11 boundaries. Typical final values are about motor `29.74182442 rad/s`, load `9.91394147 rad/s` and motor temperature `302.4760663 K`. The energy-residual threshold is `1e-5 J`. Replay hashes are compared only for the same binary, runtime and architecture. Cross-runtime numbers use a tolerance.

The heat-exchange experiment uses nodes 42/77, no external input and a 7 ms step, and runs for 7 seconds. Two batch sizes agree at 11 boundaries. The final temperature differs from the backward-Euler discrete solution by less than `1e-9 K`, from the continuous analytic solution by less than `0.004 K`, and the total energy error is less than `1e-7 J`. The two reports are `artifacts/reports/electrothermal.json` and `thermal-network.json`.

## Reproduction

```sh
dotnet run --file tools/Build.cs -- verify
```

These checks are console acceptance programs that execute assertions in Release. They do not depend on empty `Debug.Assert` tests. They do not need Unity, Python or the original C library. The native verification host and the Zig installer were moved into the same .NET build tool on 2026-09-19 (C# P/Invoke). The MCP project uses the official NuGet package, and `packages.lock.json` pins the resolution.

GitHub Actions completed the same managed acceptance on Windows, macOS and Linux: 30/30, 19/19 and 5/5 on each platform. The evidence is code commit [`aea6136`](https://github.com/Water-Run/Power/commit/aea6136bbdcbeaea91d63836d947637e7eac730e) and [run 34087686661](https://github.com/Water-Run/Power/actions/runs/34087686661). `artifacts/reports/github-actions-34087686661.log` and `.json` are stored locally and contain the job output and final status. A further local acceptance was run from a clean source copy with no cache and no generated assemblies. Its log is `github-clean-checkout.log`.

The repository and CI evidence links are public. Development resumed on 2026-09-08; the earlier records below identify their own verified baselines.

The GPL publication update added license notices without changing executable source content; a comparison against the preceding commit confirmed all 90 source/build edits were notice-only. A fresh serial verification passed 30/30 managed checks, 19/19 Unity-facing assembly checks, and 5/5 MCP integration groups, with zero build warnings or errors. Its log is `artifacts/reports/license-verification.log`. This does not add Unity Editor or Player validation evidence.

## Evidence not yet obtained

This environment has no Unity Editor installed. Editor import, Edit Mode and Play Mode tests, scene rendering checks and an IL2CPP build were not run. The project, scenes, tests and automation entry are in place:

```sh
dotnet run --file tools/Build.cs -- unity-test
```

Set `POWER_UNITY_EDITOR` first. Unity logs and XML results go to `artifacts/unity`. Play tests need a machine that can run the graphical editor and a valid Unity license. Tests that are written and not yet run cover URP and assembly import for both model assets, playback agreement, repeated start and stop without leftovers, the 10-second reference experiment, switching to a thermal-only topology while running, dynamic node and input lists, and 7 ms tick scheduling. Controls, theme, window sizes and desktop presentation still need a person to look at them, and Player builds for the three desktop platforms are still open.

The publish entry is `Power.Studio.Editor.ProjectSetup.BuildPlayer`, using the selected desktop target and IL2CPP. It needs the matching Unity platform build module. There is no built or tested Player package yet.

Every current parameter is a synthetic experiment parameter. A complete engine and transmission, vehicle calibration, emissions, acoustics, a real-time budget and long runs still need their own implementation and evidence. These checks do not establish that work.
