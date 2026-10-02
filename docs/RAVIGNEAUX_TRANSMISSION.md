# Ravigneaux research transmission

Power! assembles four forward ranges, neutral and reverse from ordinary gear,
rotor and clutch definitions. A large sun, small sun, ring and carrier form two
permanent mesh constraints. Three input clutches and two brakes select a path;
the ring drives a separate final drive and vehicle rotor. A converter and its
parallel lockup remain external components with their own heat histories. The [resolved planet option](RESOLVED_PLANETS.md)
replaces the two condensed member constraints with four actual meshes and adds
absolute spin and orbital inertia.

The structural reference is the [dual-sun Ravigneaux description](https://www.mathworks.com/help/sdl/ref/ravigneauxgear.html).
The [four-speed friction schedule](https://www.mathworks.com/help/sdl/ref/4speedravigneaux.html)
provides a separate reference for the range reductions below. Power!'s equations,
assembly and checks are implemented independently; no vendor code, model files
or packages are included. This generic research arrangement doesn't establish
the PSA AT8/AL4 topology or calibrated properties.

## Physical contract

Let `kL = NR/NL`, `kS = NR/NS`, with `kS > kL > 1`. Angular velocity and angle
increments obey:

```text
large sun + kL ring - (1+kL) carrier = 0
small sun - kS ring + (kS-1) carrier = 0
```

The first is a single-pinion branch. The second is the double-pinion branch,
which preserves the relative rotation direction between sun and ring. Reactions
are proportional to each complete constraint row, so their summed port power
vanishes. Normalized immutable rows enter the existing coupled solve; they don't
impose output speed independently of torque or inertia. Initial speeds must
satisfy both constraints. Initial phase remains observable and conserved.

| Range | Input connections | Grounded member | Input/ring reduction |
|---|---|---|---:|
| 1 | Small sun | Carrier | `kS` |
| 2 | Small sun | Large sun | `(kL+kS)/(1+kL)` |
| 3 | Carrier and small sun | None | `1` |
| 4 | Carrier | Large sun | `kL/(1+kL)` |
| Reverse | Large sun | Carrier | `-kL` |
| Neutral | None | None | Unconstrained input |

These are steady path relationships after the required elements physically
lock. A command alone doesn't establish a selected range. During capture and
handoff, finite capacity permits slip, transfers torque and generates heat.
Ground brakes carry reaction torque at zero ground speed; internal friction
heat comes from the actual slipping member. The research final-drive convention
uses a positive input/output ratio explicitly.

`RavigneauxTransmissionAssembly` takes SI member inertias, static/sliding torque
capacities, tooth ratios and final reduction. `RavigneauxPorts` binds stable IDs
and five distinct engagement channels. `CreateGraph` returns immutable collections
of four internal rotors and eight components. The caller provides input, vehicle
and optional thermal ports. `RangeCommands` returns the declared friction schedule,
without claiming hydraulic actuation or shift control.

## Shared experiments and evidence

- `ravigneaux-transmission` prescribes forward upshifts and downshifts through
  all four paths with explicit friction heat.
- `fired-ravigneaux-converter` connects the premixed engine, four signed converter
  maps, lockup, compound graph and a declared 1 kg m2 vehicle rotor. The different
  10 kg m2 torque-source experiment is an independent load case.

Both use the same JSON, CLI, MCP and portable asset contracts. Six core physics
and transaction groups compare a separately derived 2x2 free mass matrix,
reflected inertias, reverse signs, brake reactions, capture impulse/heat and
full-state rollback. A loaded 20-second overdrive check retains strict phase
limits through compensated coordinate accumulation; correction state copies,
hashes and rolls back with the complete model. Portable tests preserve complete
carriers and reactions, reject malformed
records and forged downgrades, and replay an authentic v22 fixture. Combined
engine/converter refinement and every report boundary have separate checks.
Run `dotnet run --file tools/Build.cs -- verify`; recorded outcomes and digests
belong in [VALIDATION.md](VALIDATION.md).

## Remaining scope

All parameters remain `unverified`. The four-member reduction doesn't resolve
planet spin/orbit inertia; the explicit [resolved path](RESOLVED_PLANETS.md)
supplies those energies. Detailed tooth geometry remains outside both paths. Mesh losses, lubrication,
temperature-dependent properties, measured valve-body routing and
AT control and ECU torque coordination need further conserving components and
measured evidence. The reduced experiments use prescribed engagements; the
[hydraulic option](AT_HYDRAULIC_ACTUATION.md) provides actual piston actuation. The converter
remains quasi-steady with synthetic maps.

Prepared Studio import/playback tests include the double-pinion carrier port.
Actual Editor/Play/rendering and Player/IL2CPP acceptance remain separate gates.
Complete EA211 DJS + DQ200 and PSA EC5 + AT8 sample boundaries and missing OEM
measurements remain intact in `assets/samples`.
