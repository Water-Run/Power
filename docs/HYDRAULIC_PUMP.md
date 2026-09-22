# Shaft-driven hydraulic supply

The managed graph supports an ideal reversible displacement pump and a quasi-steady
one-way pressure relief. The [fired-pump laboratory](../assets/labs/fired-pump.power.json)
connects the crank to a compliant supply line, shift valves and pressure-operated
clutches. Its parameters are synthetic and `unverified`.

## Equations and power

Displacement `D > 0` is in m³/rad. Positive shaft speed delivers reference volume from
inlet to outlet:

```
Q = D * omega
shaft reaction = -D * (p_out - p_in)
shaft-to-fluid power = Q * (p_out - p_in) = -shaft reaction * omega
```

Reverse flow and hydraulic motoring are allowed. There is no inferred check valve,
leakage, friction or efficiency map. Inertia belongs to the explicit shaft node. A
finite inlet loses exactly the volume delivered to the outlet. A reservoir inlet
contributes `p_in * Q` to external hydraulic work; shaft-to-fluid work is an internal
transfer and is not added to global source work.

The relief uses an explicit linear excess-pressure characteristic:

```
Q_A_to_B = G * max(p_A - p_B - p_crack, 0)
heat = Q_A_to_B * (p_A - p_B)
```

`G >= 0` has units m³/(s·Pa), and `p_crack >= 0` is a differential pressure. Below the
threshold it seals exactly. Finite flow requires overpressure; pressure is never
clamped to the setting. This is a constitutive approximation, not spool mechanics or
a fitted valve opening-area curve. Its full pressure drop generates heat, including
the cracking-pressure part.

The ideal pump equations follow the zero-loss limit of the
[MathWorks fixed-displacement pump description](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html).
The threshold behavior is consistent with the
[pressure-relief valve description](https://www.mathworks.com/help/hydro/ref/pressurereliefvalveil.html);
Power!'s linear excess-pressure law is an explicit simpler modeling choice. These
references supply equations and scope, not OEM parameter measurements or source code.

## Shared contracts

`hydraulic_pump` requires a rotational `node_a`, hydraulic outlet `node_b`, and
`parameters.inlet_node` (zero selects the reservoir). The inlet must differ from the
outlet. Parameters include positive `displacement` in `m3_rad`, plus explicit
`reservoir_pressure` in `pa` or `bar` only when the inlet is zero. It has no input,
heat sink or planetary `node_c`.

`hydraulic_relief` uses hydraulic `node_a`, optional hydraulic `node_b` (zero/omitted
selects a reservoir), optional thermal `heat_node`, and parameters `coefficient`,
`cracking_pressure`, and reservoir-only `reservoir_pressure`. It has no opening input.
Valve schedules still use separate controlled restrictions.

Pump outputs are last-tick mean `volume_flow`, shaft reaction `torque`, signed
`hydraulic_power`, and cumulative signed `hydraulic_work`. Initial histories are zero;
input changes leave accepted means unchanged. Global `hydraulic_work` remains external
reservoir work. Relief outputs reuse restriction flow, mean heat power and cumulative
fluid heat. All inputs, histories and compensation terms participate in forks, hashes,
cancellation and whole-batch rollback. Asset v11 retains the new definitions and all
v1–v10 readers. Agent 0.13.0 advertises `shaft_driven_hydraulics` and `fired-pump`.

## Numerical evidence and limits

Pump speed, chamber pressures, converter port speeds and cylinder work share one
Newton system, using gear-projected mechanical responses. Pressure-clutch capacities
are refreshed within the bounded constraint iteration. Accepted fluid transfers update
both ports and the reference-volume ledger. The existing independent hydraulic path
is retained for models without pumps, preserving prior replay hashes.

The joint Newton solve permits 24 iterations and 12 line-search halvings. Hydraulic
pressure residual tolerance is `2e-7 Pa + 64*epsilon*(|p_mid|+|p_old|)`; mechanical and
clutch tolerances retain their existing contracts. Nonfinite states, negative accepted
gauge pressures or exhausted solver budgets reject the complete call. Reduce `step_ns`
and inspect pressure, compliance, displacement, inertia and clutch scales before retrying.
There is no cavitation clamp.

Tests cover analytic shaft/compliance oscillation and second-order refinement,
closed-inlet conservation, reverse motoring, geared pump reactions, analytic relief
decay, a regulated steady shaft load, and an independent analytic pressure-dependent
slipping-clutch solution. Capture, branches, cancellation, late failure, retry and
allocation-free stepping are checked. Portable/MCP replay compares all 89 fired-pump
boundaries; malformed contracts and version downgrades are rejected.

In the 0.8-s fired experiment, the shaft delivers 53.94250162 J to the fluid, external
hydraulic work is zero, and the relief dissipates 45.02640514 J. Initial hydraulic
energy is explicitly 3 J. Final line pressure is 1.06972624 MPa, crank/turbine speed
69.75553569 rad/s, and load speed 6.64338435 rad/s. Total-energy residual is about
`1.07e-9 J`; reference-volume residual is `3.05e-20 m³`. Fingerprint
`d0bd8f29a706fd89`, final hash `572150ab5d66a2f6`.

Pump losses, displacement control, electric supply integration, spool and piston
travel/inertia, gas accumulators, cavitation, temperature-dependent properties and
ECU/TCU coordination remain open. This checkpoint does not establish complete DCT/AT,
calibrated vehicle performance or Unity Editor/Player acceptance.
