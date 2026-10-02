# Hydraulic transmission actuation

**English** · [简体中文](AT_HYDRAULIC_ACTUATION.zh-CN.md) · [Français](AT_HYDRAULIC_ACTUATION.fr.md) · [Русский](AT_HYDRAULIC_ACTUATION.ru.md) · [日本語](AT_HYDRAULIC_ACTUATION.ja.md) · [한국어](AT_HYDRAULIC_ACTUATION.ko.md) · [Deutsch](AT_HYDRAULIC_ACTUATION.de.md) · [Español](AT_HYDRAULIC_ACTUATION.es.md) · [Italiano](AT_HYDRAULIC_ACTUATION.it.md) · [Português](AT_HYDRAULIC_ACTUATION.pt-BR.md)

The Ravigneaux research graph can now use actual hydraulic pistons for all five
range clutches/brakes and, in the fired converter experiment, lockup. A shared
shaft-driven pump supplies compliant line pressure. Explicit fill/drain valves
move oil into each actuator chamber. Pressure moves a finite-mass piston across
its pad gap; elastic contact determines clutch capacity. Return springs and
damping release the pad after venting. A valve command alone doesn't imply lock.

## Assembly contract

`HydraulicActuationAssembly` takes explicit SI supply and actuator properties.
It returns immutable ordinary definitions, using the existing pump, pressure,
piston, spring, valve and contact-clutch physics. The caller supplies the pump
shaft, common heat sink and 1..6 declared target clutches. Each branch requires
distinct chamber/slider/piston/spring/valve IDs and two input channels.

The assembly replaces the target's prescribed friction engagement with a
`piston_clutch`. Its old engagement input is absent. `ValveInputs` maps an apply
fraction to fill and complementary drain commands; both valves can also be
operated explicitly. It is a hydraulic routing helper, not an AT controller.
Only physical pressure, travel and pad contact establish capacity and lock.

Pump displacement, leakage and shaft drag are explicit. Relief routes oil to the
declared tank boundary. No pressure source substitutes for pump work. Compliance
stores `C p^2/2`; moving pistons exchange pressure work with spring, kinetic and
contact energy. Front/back areas and tank pressure are explicit. With unequal
areas, net fluid inventory includes the corresponding swept-volume term.
Restriction, relief, return damping, shaft drag and clutch slip route actual
losses to the common thermal sink. Back-pressure work uses the existing explicit
reservoir boundary.

Stroke stops are elastic energy stores rather than position clamps. Gauge
pressure is constrained by the existing hydraulic model; negative pressure is a
numerical/physical failure, not a silent cavitation clamp. Seal leakage/friction,
fluid aeration, cavitation and measured temperature-dependent properties require
further models and data.

## Shared experiments

`hydraulic-ravigneaux-transmission` applies the five range elements through
actual filling and return motion during forward up/down handoffs.
`fired-hydraulic-ravigneaux` adds the engine, converter and sixth hydraulic lockup
actuator. Both retain absolute planet spin and orbital inertia, with the geometry
and authoring properties recorded in their source descriptions.

The research supply uses displacement 1e-6 m3/rad, leakage 1e-12 m3/(s Pa),
drag 0.02 Nm s/rad, line compliance 2e-11 m3/Pa and initial line pressure 1 MPa.
Relief cracks at 1 MPa with conductance 5e-10 m3/(s Pa); tank gauge pressure is
zero. Each branch has compliance 2e-12 m3/Pa, front/back areas 0.001/0 m2,
piston mass 0.02 kg, a 0..6-mm stroke and pad contact at 2 mm. Return stiffness
is 10000 N/m, damping 300 N s/m, and pad/stop stiffnesses 1e6 N/m.
These properties remain `unverified` research inputs.

Five range branches use valve pairs 700/701 through 708/709. The fired lockup
uses 710/711. Channels expose line/chamber pressures, piston travel/speed,
contact force, capacities, actual clutch modes, pump work, fluid volume and heat.
The complete mechanical, pressure, gas and thermal state participates in the
same hashes, forks, cancellation and batch rollback.

The graph uses portable asset v24 and existing component records. JSON, CLI,
MCP and prepared Studio import/playback cases share these definitions. Independent
pump-pressure-motion ODE refinement, swept-volume/energy ledgers, real six-branch
charging, actual path confirmation and every portable boundary have separate
checks. Run `dotnet run --file tools/Build.cs -- verify`; numerical scope and
recorded results belong in [VALIDATION.md](VALIDATION.md).

## Remaining acceptance

Schedules remain prescribed. Sensor-confirmed AT sequencing, closed-loop pressure
control, ECU torque coordination, valve/solenoid dynamics and comprehensive faults
are unfinished. This generic routing doesn't establish PSA AT8/AL4 valve-body
identity or OEM calibration. Actual Unity Editor/Play/Player/IL2CPP evidence is
also separate from managed/Standard tests. The full powertrain objective and
sample evidence boundaries remain intact.
