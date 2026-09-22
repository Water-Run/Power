# Ideal gear and planetary references

`Power.Core` provides two immutable, allocation-free constant-load references:
`IdealGearPair` and `SimplePlanetaryGear`. They return member speeds, angle advances,
reaction torques, external work, kinetic-energy change and an energy residual. They
provide independent analytic evidence for the coupled transmission solver.
The separate [coupled gear solver](GEAR_NETWORK.md) now exposes permanent gear and
planetary components through JSON, portable assets and CLI/MCP, including clutch-controlled
shift experiments. The reference classes remain pure local analytic solutions.

## Physical scope and signs

An ideal gear has no mesh inertia, compliance, backlash or losses; all supplied inertias
are attached rotor inertias. Both inertias of a pair, or all three members of a planetary,
must be positive and finite. Ground and massless nodes are not inferred from zero inertia.
The abstraction follows the scope of the Modelica Standard Library's
[IdealGear](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealGear.html)
and [IdealPlanetary](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Mechanics.Rotational.Components.IdealPlanetary.html).
Power!'s implementation is independently written; no third-party implementation is
included or called.

For a pair, signed ratio `r` defines:

```text
omega_A = r omega_B
reaction_A = lambda
reaction_B = -r lambda
```

Positive ratios give the same port direction; negative ratios reverse it. Reactions are
torques **on the attached rotors**, not the torques applied by the rotors to the gear.
They perform zero net work for compatible motion. A gear pair's housing may carry a
reaction; the two rotors' ordinary angular momentum alone is not generally conserved.
The generalized momentum `r J_A omega_A + J_B omega_B` changes with generalized external
torque `r T_A + T_B`.

For the simple planetary, sun, ring and carrier share one positive axis. Tooth ratio
`k = N_ring / N_sun` must exceed one. The kinematic relation and two independent degrees
of freedom agree with the [MathWorks planetary gear equations](https://www.mathworks.com/help/sdl/ref/planetarygear.html).

```text
omega_S + k omega_R = (1+k) omega_C
reaction_S = lambda
reaction_R = k lambda
reaction_C = -(1+k) lambda
```

These reactions sum to zero and perform zero net work. The model accepts a continuous
ratio, without inferring tooth counts, module, tooth strength or manufacturable geometry.
Planet spin/orbital inertia, losses, bearings, lubrication and thermal behavior remain
outside this reference.

## Independent reduced-coordinate solution

Pair motion uses port B as the independent coordinate:

```text
J_equivalent_B = r^2 J_A + J_B
alpha_B = (r T_A + T_B) / J_equivalent_B
alpha_A = r alpha_B
```

The planetary eliminates carrier motion before forming its kinetic-energy mass matrix.
With `a = 1/(1+k)` and `b = k/(1+k)`:

```text
omega_C = a omega_S + b omega_R

M = [ J_S + a^2 J_C,    ab J_C        ]
    [ ab J_C,           J_R + b^2 J_C ]

M [alpha_S, alpha_R]^T = [T_S + a T_C, T_R + b T_C]^T
alpha_C = a alpha_S + b alpha_R
```

The implementation scales this two-by-two matrix and expands its determinant into
positive terms to avoid subtracting nearly equal products. For constant loads, acceleration is
constant, so speed and displacement follow exact linear/quadratic time integration up
to floating-point rounding. Reaction torques are then recovered from member equations.
Tests use an independent acceleration-constraint multiplier for the free planetary;
they do not reuse the reduced matrix as their expected solution.

## State, units and failure contract

Public property names carry SI units: kg m2, rad/s, rad, N m, J and seconds. Ratios are
unitless. `Advance` takes a positive finite local duration and returns `GearStepStatus`.
This local reference duration does not replace `Simulation`'s bounded integer clock.
The classes hold no evolving state. Inputs are value records; output is default on every
rejection, and instances can be shared by independent callers.

Initial speeds must already satisfy the relation. Compatibility uses a relative rounding
test with binary64 epsilon `2.2204460492503131e-16`, with no absolute low-speed deadband.
For a pair the bound is `64 epsilon (|omega_A| + |r omega_B|)`. The planetary additionally
includes the magnitudes of its two weighted speed terms so cancellation is treated
relative to the operations that formed the carrier speed. The terms are scaled before
addition to avoid overflowing the tolerance.

After validation, dependent speed and angle advance are reconstructed from independent
coordinates. This removes accepted rounding residue; it is not a finite-slip engagement
or synchronization calculation. Incompatible speeds return `IncompatibleState`. Use an
explicit clutch/impact model for a real speed mismatch rather than discarding its energy.
Absolute gear phase is unspecified: only angle advances are reported.

Invalid construction parameters throw actionable argument exceptions. Nonfinite or
ill-conditioned parameter combinations are rejected; the scaled planetary determinant
must exceed `64 epsilon`. Interval rejection distinguishes invalid duration, invalid state,
incompatible state, invalid torque and numerical failure. Arithmetic overflow returns
`NumericalFailure`; finite inputs alone do not guarantee representable derived quantities.
A runtime force-balance check also rejects cancellation that leaves finite but inconsistent
member reactions: each force residual is bounded by `512 epsilon` times the sum of
magnitudes of inertial, applied and reaction torques. The accepted endpoint also checks
each member's impulse balance, using `512 epsilon` times the magnitudes of old/new
momentum and applied/reaction impulses. The latter detects excessive cancellation in
dependent-speed reconstruction. These checks bound residuals, not solution error
for arbitrary ill-conditioned parameters. Tests include a finite cancellation failure and
a high-ratio pair whose small reaction must remain observable.
The residual is `external_work - kinetic_energy_change`; no friction heat is fabricated.

## Transmission states and evidence

Tests explicitly supply holding or locking torques to establish these ideal limits:

| Imposed condition | Resulting speed relation |
|---|---|
| Ring held | `omega_C = omega_S / (1+k)` |
| Sun held | `omega_C = k omega_R / (1+k)` |
| Carrier held | `omega_S = -k omega_R` |
| Sun locked to ring | All three member speeds equal |

The supplied brake does zero work when its member is held; a sun/ring lock receives
opposite torques with zero combined work. These checks establish static transmission
states. No shift, clutch engagement, hydraulic circuit or TCU is implemented by this
reference, and arbitrary external torques do not hold a member automatically.

The same eight test groups run against `net10.0` and `netstandard2.1`:

- Signed ratios and reflected inertia; reaction power and per-member impulse balance.
- Free planetary motion against an independent force-multiplier solution.
- Three held-member cases and direct drive, with explicit holding/locking loads.
- Constant-load partition invariance and speed reversal through zero.
- Sinusoidal loading against independent integrals for both references; interval halving
  gives approximately fourfold reduction of speed/angle error.
- Invalid inertia/ratio/state/load values, incompatible speeds, ill conditioning and overflow.
- 2,500 deterministic cases for each reference checking work, momentum and reproducibility.
- 10,000 repeated evaluations of each primitive with zero managed allocation, plus shared
  immutable use by independent concurrent callers.

See [validation](VALIDATION.md) for the full serial verification result. Standard-assembly
tests run on .NET 10 and provide no Unity Editor/Play/IL2CPP evidence.

## Coupled integration

Permanent constraints now participate in the electromechanical/cylinder/clutch solve,
with independent simulation workspaces, complete rollback and stable reaction/error
channels. JSON/schema, asset v8 with prior readers, MCP discovery and replay use the same
topology. The fired planetary experiment performs reduction/direct upshifting and a
downshift. See [the coupled contract](GEAR_NETWORK.md) for equations and evidence.
Complete DCT/AT topology, converter, hydraulics, controls, complete engine behavior and
measured vehicle calibration remain part of the full Power! objective.
