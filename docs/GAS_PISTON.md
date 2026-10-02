# Linear gas piston and hydraulic accumulator

**English** · [简体中文](GAS_PISTON.zh-CN.md) · [Français](GAS_PISTON.fr.md) · [Русский](GAS_PISTON.ru.md) · [日本語](GAS_PISTON.ja.md) · [한국어](GAS_PISTON.ko.md) · [Deutsch](GAS_PISTON.de.md) · [Español](GAS_PISTON.es.md) · [Italiano](GAS_PISTON.it.md) · [Português](GAS_PISTON.pt-BR.md)

`gas_piston` connects a translational mass to one finite gas chamber. The chamber's
mass, internal energy, pressure and temperature remain actual simulation states.
Its volume comes from piston geometry rather than a fixed storage volume. Gas
orifices and wall heat links can use the same chamber.

Combining a gas piston and a hydraulic piston on the same translational node creates
a gas-backed accumulator separator. Both pressure forces act on one mass and one
displacement in the joint solve. Return springs, damping and compliant stroke ends
remain explicit components. This is a lumped piston accumulator, with constant
effective liquid compliance and constant-caloric ideal gas. Bladder geometry, seal
friction, gas dissolution, cavitation, wear and OEM calibration remain outside it.

## Geometry, pressure and work

Area A and reference volume Vr are positive. Reference position xr is explicit,
and compression direction s is +1 or -1:

```text
gas volume       V(x) = Vr - s*A*(x-xr)
absolute pressure p  = (gamma-1)*U/V
gas temperature   T  = U/(m*cv)
force on slider      = -s*A*(p-p_reference)
```

`p_reference` is an explicit absolute pressure, including zero for a declared vacuum.
When composing an accumulator, it supplies the tank reference used by the liquid's
gauge-pressure convention. It is not inferred from the gas precharge. Initial gas
pressure and temperature, gas constant R and gamma come from the gas node; its
initial mass is `p_initial*V_initial/(R*T_initial)`.

During the mechanical interval, closed-gas work follows `U_next=U_old*(V_old/V_next)^(gamma-1)`.
The force uses the mean pressure that gives exactly this discrete energy transfer.
Reference-pressure work is `p_reference*s*A*dx` and enters external source work.
Thus gas internal energy plus the separator's mechanical energy balances the fluid
work, reference work and explicit losses. Gauge-pressure energy available from gas
storage uses `Delta U - reference_work`; absolute gas energy alone overstates that
transfer.

For relative volume change z, the pressure factor is
`phi=(1-(1+z)^(1-gamma))/((gamma-1)*z)`. Its limiting value is one and its limiting
derivative is `-gamma/2`. The implementation uses scaled series for small travel,
and stable logarithm/exponential differences otherwise. The force Jacobian is
analytic. Reversed and opposed gas chambers share the same coordinate and preserve
the signed volume/work convention.

Gas flow and heat use the existing symmetric flow/work/flow split. A closed chamber
preserves the adiabatic invariant; explicit wall-temperature handling retains the
existing first-order wall-coupled accuracy. Gas mass flow is accounted with reservoir
enthalpy, rather than treating added mass as energy-free. No polytropic fit or
isothermal override replaces the energy state.

## Contracts and numerical boundaries

| Parameter | Meaning |
|---|---|
| `node_a` | Translational node with positive mass |
| `node_b` | Gas chamber with one moving-volume owner; omit node `storage` |
| `area` | Positive area in m2 or mm2 |
| `reference_volume` | Positive volume in m3 or liters |
| `reference_position` | Position in m or mm at that volume |
| `reference_pressure` | Nonnegative absolute pressure in Pa or bar |
| `compression_direction` | +1 (default orientation) or -1 |

A gas chamber has one geometry owner; several distinct chambers may act on one
mass. A gas piston has no direct input or thermal-sink override. Use an explicit
force source, connected hydraulic piston, gas orifice or gas heat link. Observe
chamber mass/energy/pressure/temperature and component volume, slider force and
`source_work` from reference pressure.

Gas volume must stay positive, including throughout a shared hydraulic piston's
nominal stroke. An accepted mechanical interval changes at most 25% of the current
gas volume. Large travel, nonpositive volume or unresolved force rejects the whole
batch. Reduce tick size and inspect geometry, mass, pressure and force scales before
retrying. Motion and energy are not clamped. Compliant end penetration still stores
the explicit stop potential and remains subject to positive gas volume.

Physical/control state, gas inventories and all histories share cancellation,
complete rollback, hashes and independent forks. Asset v16 preserves the four
geometry/reference quantities and compression direction. JSON, CLI and MCP expose
the same definitions. Studio separator/chamber views and import/Play tests are
prepared in C# 9 source; actual Editor and Player evidence remain separate.

## Accumulator experiment and evidence

`gas-accumulator-pump` adds a 50-ml initial gas chamber, a 50-g separator, explicit
viscous damping and compliant ends to the electric pump, mechanical spool bypass
and pressure clutch. Gas starts at 200 kPa absolute and 300 K; reference pressure is
100 kPa. The separator starts with 0.1 mm compliant seating penetration, balancing
its precharge against zero liquid gauge pressure. All values are research parameters.
The 3-4-s voltage/demand pulse explicitly opens both fill and drain paths; stored gas
energy and swept liquid volume then decrease before charging resumes.

[MathWorks Gas-Charged Accumulator (IL)](https://www.mathworks.com/help/hydro/ref/gaschargedaccumulatoril.html)
describes the gas/liquid separation and charge/discharge mechanism. Power! composes
its own finite-energy gas and mechanical/hydraulic ports rather than copying a
fixed polytropic exponent, parameter defaults or implementation code.

Checks include analytic adiabatic work and derivatives, thin travel, a separate
mass/energy RK4 transient with smooth second-order refinement, opposed chambers,
common gas/fluid motion, finite-wall RK4 refinement, moving-volume gas inflow,
independent energy/volume accounts and complete transactions. Closed, unmixed
chambers without transport or heat skip redundant zero-rate integration after
state validation. The measured optimization preserves every boundary value/hash;
steady stepping and snapshot reads allocate zero managed bytes. Detailed error
bounds, timings and platform scope are in [VALIDATION.md](VALIDATION.md).
