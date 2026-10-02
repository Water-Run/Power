# Hydraulic piston and contact-actuated clutch

**English** · [简体中文](HYDRAULIC_PISTON.zh-CN.md) · [Français](HYDRAULIC_PISTON.fr.md) · [Русский](HYDRAULIC_PISTON.ru.md) · [日本語](HYDRAULIC_PISTON.ja.md) · [한국어](HYDRAULIC_PISTON.ko.md) · [Deutsch](HYDRAULIC_PISTON.de.md) · [Español](HYDRAULIC_PISTON.es.md) · [Italiano](HYDRAULIC_PISTON.it.md) · [Português](HYDRAULIC_PISTON.pt-BR.md)

`hydraulic_piston` connects one translational mass to a front hydraulic chamber and
either a back chamber or an explicit back-pressure reservoir. `piston_clutch` reads
the piston's pad force. Positive pressure can move a piston through free clearance
without transmitting clutch torque.

## Equations and energy

For displacement x, velocity v, front/back effective areas Af/Ab and gauge pressures
pf/pb, the piston force is `Af*pf - Ab*pb`. Front expansion draws `Af*dx`; back
contraction delivers `Ab*dx`. A finite chamber stores `C*p*p/2` joules and `C*p`
cubic meters of reference inventory. The volume ledger includes the piston's swept
volume `(Af-Ab)*(x-x_initial)`. A back reservoir contributes signed external work
`-pb*Ab*dx` and reference volume `-Ab*dx`.

The translational node stores `m*v*v/2`. A `linear_spring` adds `K*(x-rest)^2/2`
and viscous loss `D*v_relative^2`, with explicit thermal routing. Its
`friction_heat` channel reports cumulative damping heat using compensated summation.
It is independent of rounded thermal-node temperatures.

The unilateral pad potential is `Kpad*max(x-contact,0)^2/2`. Lower and upper stops
add the same quadratic potential outside the nominal stroke. Stops are compliant:
penetration stores energy and produces a restoring force. They do not clamp motion.
For a hinge potential V, the interval reaction uses `-(V(x_next)-V(x_old))/dx`,
evaluated through a cancellation-resistant discrete gradient. Consequently, contact
work is exactly the potential change in the discrete equations. The analytic
Jacobian covers active, inactive, activating and releasing hinges; at a stationary
hinge it uses the mean one-sided derivative.

Static/sliding clutch capacities are `mu*surfaces*radius*Npad`. The solver uses the
pad's discrete force during the interval and instantaneous pad force for snapshots.
Friction heat remains nonnegative; a locked ideal clutch dissipates no slip power.
The same joint solve includes pressure, piston inertia, spring damping, electrical
supply and the existing mechanical/clutch constraints.

## Contracts and numerical boundaries

| Element | Required data |
|---|---|
| `translational` node | Positive mass in kg, initial velocity in m/s, position in m |
| `hydraulic_piston` | A translational A port, hydraulic front B port; front/back areas, back node/pressure, increasing stroke limits, stop stiffness, contact position/stiffness |
| `linear_spring` | Translational A/B ports or grounded B; stiffness N/m, damping N·s/m, rest displacement m, optional thermal sink |
| `piston_clutch` | Rotational A/B ports or grounded B, piston component ID, radius m, static/sliding coefficients, integer friction surfaces |
| `force_source` | Translational A port and external force input in N |

`linear_spring.parameters.rest_angle` retains the shared descriptor key but carries
a displacement quantity in meters. One piston owns a given translational node;
multiple clutch friction elements may explicitly reference its pad. A finite back
chamber must differ from the front chamber. A reservoir has `back_node=0` and an
explicit nonnegative `back_pressure`. Static friction must be at least sliding
friction. Surfaces lie in 1–128 and pad contact lies within the nominal stroke.

The accepted piston travel per interval is limited to one quarter of the nominal
stroke. Reduce the fixed tick if motion violates this limit or the joint solve does
not converge. Negative accepted gauge pressure rejects the complete batch; inspect
supply flow, compliance, effective areas, inertia and damping. This model has no
cavitation clamp. Full rollback, cancellation, forks and state hashes include the
motion, pressure, friction and damping histories.

The equations assume constant effective compliance and areas, a lumped moving mass,
linear return spring/damping, an elastic pad and compliant ends. Seal friction,
cavitation, plate deformation modes, wear, detailed friction maps and OEM calibration
remain outside this implementation.

## Verification and laboratory

Independent checks cover coupled mass/spring/fluid analytic oscillation, finite and
reservoir back chambers, swept-volume and pressure work, hinge work identities,
analytic contact derivatives and a piecewise RK4 contact reference. Smooth linear
motion shows second-order refinement; nonsmooth contact tests check decreasing
error without claiming uniform hybrid order. Clutch checks cover free fill, pad
contact, capture, release and friction heat. Late numerical failure, cancellation,
fork independence, exact batching and allocation-free stepping are verified.

The synthetic [piston-actuated clutch laboratory](../assets/labs/piston-actuated-clutch.power.json)
uses a finite battery, duty-regulated electric pump, fill/drain valves, a 20 g piston,
2 mm pad clearance, a 10 kN/m return spring and 300 N·s/m damping. The damping is an
explicit research parameter chosen to keep the supplied chamber nonnegative during
the transient. It is not an OEM measurement. At 15 s the pad load is about 177.28 N,
with 22.69/11.35 N·m static/sliding capacities. CLI, portable and actual MCP replay
agree at every reported boundary.

See [VALIDATION.md](VALIDATION.md) for numerical bounds, separate energy ledgers,
performance measurements and runtime scope. Asset v14 retains the complete topology.
Studio slider/contact views and import/Play tests are prepared; actual Unity Editor
and Player evidence remains pending.
