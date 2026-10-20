# Shaft-driven phase-resolving fuel pump

**English** · [简体中文](FUEL_CHAMBER_PUMP.zh-CN.md) · [Français](FUEL_CHAMBER_PUMP.fr.md) · [Русский](FUEL_CHAMBER_PUMP.ru.md) · [日本語](FUEL_CHAMBER_PUMP.ja.md) · [한국어](FUEL_CHAMBER_PUMP.ko.md) · [Deutsch](FUEL_CHAMBER_PUMP.de.md) · [Español](FUEL_CHAMBER_PUMP.es.md) · [Italiano](FUEL_CHAMBER_PUMP.it.md) · [Português](FUEL_CHAMBER_PUMP.pt-BR.md)

## Physical contract

A `fuel_chamber` couples an actual shaft to a finite fuel-domain chamber. Inlet and outlet `fuel_check_valve` components provide pressure-driven filling and delivery. Liquid, vapor and elastic wall energy share the same material closure. An inlet-starved chamber can flash during suction; delivered fuel follows the computed inventory and pressure.

Port `node_a` is rotational and `node_b` is a fuel node with one geometry owner. Declare `minimum_reference_volume`, `swept_volume` in `m3` or `l`, and `phase` in `rad` or `deg`. The fuel node storage matches the reference volume at the initial shaft angle within binary64 rounding. The sinusoidal mechanism has no added piston or cam inertia; shaft inertia is explicit.

```text
V0(theta) = Vmin + Vs sin^2((theta - phase) / 2)
V = V0(theta) + C p
E = U_fluid + C p^2 / 2
dE = T dS + mu dM - p dV0
torque_pressure = -delta_E_adiabatic / delta_theta
```

## Work and ports

Closed mechanical work holds mass and entropy fixed. Stored energy includes the fluid and elastic wall, so shaft work uses the geometric reference-volume change. The same accepted energy change determines pressure torque in the joint mechanical solve, including gears, converters, hydraulics and speculative clutch intervals. Expansion pushes the shaft; positive `hydraulic_work` means work from shaft to chamber. No extra external pressure source is added.

A `fuel_check_valve` uses declared conductance in `m3_s_pa` and optional fractional opening. It passes homogeneous donor fluid only from A to B when A has higher pressure. This is an ideal massless, zero-cracking-pressure check law. The conservative enthalpy flow and finite-wall heat contracts remain those of the fuel network.

## Use and evidence

Request `equilibrium-fuel-pump` or `electric-equilibrium-fuel-pump` through MCP, or run their JSON through CLI. Read chamber-node mass, temperature, pressure, vapor fractions and energy beside valve delivery and shaft speed. Component `volume` is geometric reference volume; node `volume` includes wall expansion. Chamber `torque`, `hydraulic_power` and `hydraulic_work` expose the internal shaft exchange.

Independent liquid pressure/work, ideal-vapor adiabats and two-phase entropy inversion check the constitutive relation. Closed-liquid shaft motion demonstrates second-order refinement; the complete phase-changing filling/delivery experiment demonstrates first-order refinement against an independently inverted RK4 reference. Inlet starvation, full liquid supply, electric drive, gear/hydraulic/clutch composition, exact replay, rollback and allocation checks cover the integration. Asset v34 stores geometry and reads v1-v33. Actual Unity Editor/Play/Player/IL2CPP remains unverified.

## Remaining work

Parameters remain `unverified`. The material temperature, pressure and representable-volume limits apply throughout the cycle. The model assumes incompressible liquid, ideal pseudo-species vapor, instantaneous common-temperature equilibrium and zero external case pressure. Local inlet inertia/NPSH, sonic choking, nucleation delay, valve inertia/hysteresis, real fuel mixtures and measured calibration remain open.

[Modelica: swept volume](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Fluid.Machines.SweptVolume.html) · [Cantera: reactor walls](https://cantera.org/dev/reference/reactors/interactions.html) · [VALIDATION.md](VALIDATION.md)

The fuel-domain pump now feeds a physical needle and frozen-quality discharge into a finite-liquid-volume fired receiver. Complete inventories include phase change, displacement work, jet kinetic energy and stationary-wall impulse. [Frozen-quality fuel discharge and finite receivers](FUEL_DISCHARGE.md)
