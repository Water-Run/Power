# Resolved Ravigneaux planet motion

**English** · [简体中文](RESOLVED_PLANETS.zh-CN.md) · [Français](RESOLVED_PLANETS.fr.md) · [Русский](RESOLVED_PLANETS.ru.md) · [日本語](RESOLVED_PLANETS.ja.md) · [한국어](RESOLVED_PLANETS.ko.md) · [Deutsch](RESOLVED_PLANETS.de.md) · [Español](RESOLVED_PLANETS.es.md) · [Italiano](RESOLVED_PLANETS.it.md) · [Português](RESOLVED_PLANETS.pt-BR.md)

The resolved assembly includes absolute spin of both internal planet sets and
their orbital mass inertia about the carrier. Four physical mesh constraints
connect six rotors. The five range clutches/brakes and external converter remain
ordinary components. The four-member reduction is still available as a separate
declared simplification; it doesn't supply planet-spin evidence.

The mesh connectivity and pitch relations have a separate [structural reference](https://www.mathworks.com/help/sdl/ref/ravigneauxgear.html).
Power! derives and implements its own conserving rotor graph and independent
mass-matrix checks. No vendor implementation or model package is included.

## Geometry and energy

For ring pitch radius `R`, large/small sun ratios `kL` and `kS`, the rigid pitch
geometry is:

```text
large sun radius = R/kL
small sun radius = R/kS
outer planet radius = (R-large sun radius)/2
inner planet radius = (large sun radius-small sun radius)/2
outer orbit radius = (R+large sun radius)/2
inner orbit radius = (large sun radius+small sun radius)/2
```

`RavigneauxPlanetParameters` requires an SI ring radius, positive per-planet
masses and spin inertias, and 1..32 synchronous equal planet pairs. Even spacing
must fit both sets without pitch-circle overlap. Geometry and aggregated inertia
must remain representable. All these inputs are explicit research properties;
the helper doesn't supply measured values.

For `n` equal pairs, the carrier receives the orbital inertia
`n (mInner orbitInner^2 + mOuter orbitOuter^2)`. The existing `CarrierInertia`
parameter is the carrier structure inertia in this resolved path. Each new rotor
has `n` times its per-planet spin inertia. Their speeds are absolute angular
velocities, so kinetic energy is the ordinary `J omega^2/2`; co-rotation retains
planet spin energy. Using relative spin with this diagonal storage would omit
the carrier coupling.

## Mesh contract

`carrier_gear` imposes `A - ratio B + (ratio-1) C = 0`, where C is the actual
moving carrier. External meshes use a negative pitch-radius ratio; the
ring/outer-planet internal mesh uses a positive ratio. Finite nonzero signed
ratios, including one, are supported. Three distinct rotational ports and
compatible initial speeds are required.

The four meshes are large-sun/outer-planet, small-sun/inner-planet,
ring/outer-planet and inner-planet/outer-planet. All three reaction torques enter
the same midpoint projection and have zero summed port power and zero torque
sum. The carrier reaction isn't silently sent to stationary ground. Bounded relative
residual refinement improves small force responses. The midpoint solve enforces
zero next-endpoint velocity residual, avoiding repeated reflection of preceding
roundoff. Both operations use actual constraint force responses and retain their correction
multipliers in actual reaction histories. Scratch belongs to each simulation;
compiled factors remain immutable. Normalized
rows, compensated coordinates and complete reaction histories preserve phase,
forks, cancellation and batch rollback.

The independent free reference uses ring/carrier coordinates. With
`aOuter = R/outerRadius` and `aInner = R/innerRadius`:

```text
outer planet speed = aOuter ring + (1-aOuter) carrier
inner planet speed = -aInner ring + (1+aInner) carrier
M = sum over rotors of J [ring coefficient, carrier coefficient]^T
                         [ring coefficient, carrier coefficient]
```

This includes both spin energies and the separately added orbital inertia.
Independent generalized loads, reflected inertias for all forward/reverse paths,
angular momentum and carrier-capture impulse/heat check the assembled solve.

## Shared graph and evidence

`CreateResolvedGraph` takes the original ports, four distinct planet node/mesh
IDs, and the declared planet properties. It returns immutable ordinary
definitions: six internal rotors, four carrier meshes, a final drive and five
friction elements. Flat JSON retains the total rotor inertias and signed mesh
ratios; the example description records the generating geometry and per-planet
properties. Source digests preserve that declared authoring evidence.

`resolved-ravigneaux-transmission` exercises all forward up/down handoffs.
`fired-resolved-ravigneaux-converter` adds the engine, signed converter maps and
lockup. Both declare three pairs, R=0.1 m, inner/outer masses 0.3/1 kg and
per-planet spin inertias 0.000015/0.0005 kg m2. Carrier structure is 0.03 kg m2;
the explicit orbit addition is 0.0184375 kg m2. These are research inputs.

Portable asset v24 retains the signed carrier mesh and reads earlier versions.
The primitive adds fingerprint tag 28; previous graphs retain their fingerprints
and replay. Prepared Studio markers identify all three mesh ports. Actual Unity
Editor/Play/Player/IL2CPP verification remains separate. Run the required serial
`dotnet run --file tools/Build.cs -- verify`; numerical outcomes and scope live
in [VALIDATION.md](VALIDATION.md).

## Remaining behavior

Synchronous rigid equal planet sets don't model tooth compliance, manufacturing
load sharing, clearance, mesh losses, lubrication or temperature-dependent
properties. [Pump-fed hydraulic piston actuation](AT_HYDRAULIC_ACTUATION.md) is available.
Complete shift control, ECU coordination and
measured OEM geometry/maps remain unfinished. The generic assembly doesn't prove
PSA AT8/AL4 identity. Sample boundaries and missing measurements remain intact.
