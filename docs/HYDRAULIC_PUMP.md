# Shaft-driven hydraulic supply

**English** · [简体中文](HYDRAULIC_PUMP.zh-CN.md) · [Français](HYDRAULIC_PUMP.fr.md) · [Русский](HYDRAULIC_PUMP.ru.md) · [日本語](HYDRAULIC_PUMP.ja.md) · [한국어](HYDRAULIC_PUMP.ko.md) · [Deutsch](HYDRAULIC_PUMP.de.md) · [Español](HYDRAULIC_PUMP.es.md) · [Italiano](HYDRAULIC_PUMP.it.md) · [Português](HYDRAULIC_PUMP.pt-BR.md)

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

Measured loss maps, displacement control, battery and voltage-control dynamics, spool and piston
travel/inertia, gas accumulators, cavitation, temperature-dependent properties and
ECU/TCU coordination remain open. This checkpoint does not establish complete DCT/AT,
calibrated vehicle performance or Unity Editor/Player acceptance.

## Explicit leakage, shaft friction and electrical supply

`HydraulicPumpAssembly` supplies a reusable constant-coefficient pump reduction. It
accepts displacement D in m³/rad, leakage conductance G in m³/(s·Pa), and viscous
shaft friction B in N·m·s/rad. D must be positive; G and B must be nonnegative and
finite. No nominal efficiency or oil property is inferred.

For differential pressure `dp = p_out - p_in`:

```text
net inlet-to-outlet flow = D*omega - G*dp
shaft reaction          = -D*dp - B*omega
leakage heat power      = G*dp^2
shaft friction heat     = B*omega^2
absorbed shaft power    = net fluid power + leakage heat + shaft friction heat
```

The signs support pumping and hydraulic motoring in either direction, as well as
leakage through a stopped pump. Leakage remains a passive outlet-to-inlet path even
when it exceeds displacement flow. The constant-conductance leakage reduction follows
the analytical loss description in the [MathWorks pump reference](https://www.mathworks.com/help/hydro/ref/fixeddisplacementpumpil.html).
The linear viscous drag is an explicit Power! constitutive choice; it is not that
reference's pressure-dependent friction model or an OEM efficiency map.

`TryEvaluate` returns instantaneous net flow, total shaft reaction, signed shaft/fluid
power and the two nonnegative loss powers. It rejects negative gauge pressures,
nonfinite inputs and overflow without returning a partial reaction.

`CreateComponents` returns an immutable list with explicit, distinct IDs for an ideal
`hydraulic_pump`, a fixed-opening `hydraulic_resistance` from outlet to inlet, and a
zero-stiffness `shaft` from the pump shaft to ground. Specify a thermal sink or let
losses enter external heat rejection. The model compiler checks ports, domains, units
and global IDs. Ordinary components retain the coupled midpoint solve, transactions,
channels, JSON schema and asset v11; there is no hidden assembly state or new format.
Pump channels describe the ideal branch. Subtract leakage flow to obtain assembly
delivery; include shaft drag when interpreting total shaft load. Do not count ideal
pump work as both external source work and internal transfer.

`fired-pump-losses` connects leakage and drag to the existing fired transmission. A
separate pump thermal node receives both losses. The zero-loss limit reproduces all
shared observables of `fired-pump` within physical tolerance. Constant G and B are
research inputs and remain `unverified`.

`electric-pump` connects a 12 V RL DC motor to a separate pump shaft, with explicit
back EMF, inductance, torque and copper heat. A compliant supply line, relief and
scheduled fill/drain valves actuate a clutch between a driven shaft and a load.
Voltage changes and valve events use exact ticks. The pump has no crank connection
and external hydraulic work is zero. Electrical work is included in global source
work; the driven shaft and load torque are separate external power boundaries.
Prescribed voltage and valve commands do not implement a battery, ECU/TCU or a
closed-loop regulator.

Analytic damped shaft/pressure motion and an independently integrated three-state
RL-motor/shaft/pressure ODE check smooth second-order refinement. Closed inlets,
reservoir work, signed operation, passive losses, heat routing, zero allocations,
branches, cancellation and late failure rollback are checked against both Core
assemblies. JSON, portable assets and MCP compare all 89 fired-loss and 106 electric
report boundaries. Prepared Unity import/Play tests require separate Editor execution.

## Sampled pressure regulation

`pressure_controller` reads a hydraulic gauge-pressure node and owns an existing
DC motor voltage channel. It is a discrete PI controller with explicit proportional
gain in `v_pa`, integral gain in `v_pa_s`, voltage bounds and initial integral voltage.
The setpoint input has pressure units. Its integer `sample_period_ns` is 1 ns..1 s
and must be an exact multiple of the model tick. Gains and pressure setpoints are
nonnegative; voltage bounds are finite and strictly increasing. No tuning is inferred.

```text
error = setpoint - sampled_pressure
I_candidate = I + Ki * sample_period_s * error
raw_voltage = Kp * error + I_candidate
if raw_voltage exceeds a limit and the integral increment pushes farther outside:
    retain the preceding integral state
command = clamp(Kp * error + accepted_integral, minimum_voltage, maximum_voltage)
```

Conditional integration is the clamping anti-windup strategy described by the
[MathWorks control reference](https://www.mathworks.com/help/simulink/slref/anti-windup-control-using-a-pid-controller.html).
Power!'s precise discrete transition above is its declared model, not copied
implementation code or evidence of OEM tuning. Saturation alone does not establish
tracking: an unreachable target can execute and replay successfully while failing KPIs.

Samples occur at time zero and absolute multiples of the configured period. The first
sample preserves the explicitly supplied initial integral; later samples use the period.
The command is held between samples. Exact-tick events are applied before a sample at
the same tick. An event at a call's endpoint updates the setpoint before the snapshot;
sampling at that endpoint happens only when the next physical tick begins. Internal
clutch capture/reversal intervals do not trigger extra controller updates.

The compiler checks that the target is a DC motor voltage input and has exactly one
owner, that the sensor is hydraulic, and that the initial motor voltage is in bounds.
The owned voltage channel remains in the component definition but is absent from the
external input list. Direct writes and scheduled voltage overrides are rejected; change
the controller's `pressure_setpoint` input instead. Other motor, pump and valve channels
retain their existing semantics. Multiple independent loops may share a pressure sensor.

Observable channels are `sampled_pressure`, `pressure_error`, `integral_voltage` and
`command_voltage`. Pressure/error histories start at zero; initial command is the motor's
configured voltage, and initial integral is explicit. Histories describe the latest
sample, rather than continuously recomputed pressure error. All four controller states
and the held motor input participate in hashes, forks and complete batch rollback.
Sampling and successful stepping allocate no managed memory after warm-up. Nonfinite
PI arithmetic rejects the full call; inspect gain, setpoint and integral scales.

The controller adds no physical stored energy or power boundary. Its command changes
the existing motor's voltage boundary, whose current, work and copper heat remain in
the coupled solve and conservation ledger. Models without controllers retain their
fingerprints and stepping. Controlled models add fingerprint tag 13. Asset v12 retains
the complete controller definition; an authentic v11 pump fixture retains its original
digest, fingerprint and same-runtime replay after upgrade.

`pressure-regulated-pump` uses a 5 ms controller and 100 µs physical ticks, with
scheduled clutch fill/drain disturbances and 300/350/200 kPa targets. Gains, actuator
limits and all other parameters remain `unverified`. It has 757 matching JSON, asset
and MCP report boundaries. Tests compare a separate sampled controller/RK4 plant,
check physical tick refinement at fixed controller period, exact clock/endpoint rules,
saturation recovery, unit/ownership diagnostics, controller-state rollback, branches,
zero allocations and the complete electrical/hydraulic work ledger.

This supplies one pressure feedback loop. Battery and PWM/current-loop dynamics,
sensor filtering/delay/quantization, valve/spool/piston dynamics, ECU/TCU coordination,
complete DCT/AT, faults and measured calibration remain separate unfinished work.

## Finite battery supply and duty regulation

A `battery` node owns two states: charge fraction z and polarization voltage v_p.
Its storage is explicit charge capacity Q in C or Ah (1 Ah = 3600 C), initial state
is SOC in `fraction`, and position is initial polarization voltage in V. Its battery
record supplies empty/full OCV, series resistance R0, polarization resistance Rp,
capacitance Cp and a thermal sink or external heat rejection. OCV is affine in SOC:

```text
E(z)       = V_empty + (V_full - V_empty)*z
z'         = -I_battery / Q
v_p'       = I_battery / Cp - v_p/(Rp*Cp)
V_bus      = E(z) - v_p - R0*I_battery
U_chemical = Q*(V_empty*z + (V_full - V_empty)*z^2/2)
U_polar    = Cp*v_p^2/2
heat power = R0*I_battery^2 + v_p^2/Rp
```

Positive current discharges; negative current charges. The topology follows the
[battery equivalent-circuit description](https://www.mathworks.com/help/simscape-battery/ref/batteryequivalentcircuit.html).
The affine OCV and constant parameters are explicit Power! reductions, rather than
temperature/ageing tables, measured chemistry, capacity fade or a BMS. SOC stays within
[0,1]. Exceeding charge inventory or obtaining negative bus voltage rejects the whole
batch; there is no silent clamp or invented reserve. Shorten the batch, stop discharge/
charge or supply different declared initial conditions.

`battery_motor` connects a rotational shaft to a battery bus and retains explicit
motor resistance, inductance, torque/back-EMF coefficient and initial current.
Its averaged bidirectional duty input is in [-1,1]: motor voltage is duty times bus
voltage and battery-side current is duty times motor current. That power transfer is
internal and is not added to `source_work`. Both motor inductive energy and battery
polarization/chemical energy participate in total stored energy. Copper, series and
polarization heat route to their explicit sinks. This is an ideal averaged converter,
not PWM switching, converter losses, contactors or a current-control loop.

`resistive_load` supplies an explicit positive resistance, optional opening input in
[0,1] and heat sink. Opening scales conductance; zero opening disconnects exactly.
For total load conductance G and motor-side bus current I_m:

```text
V_bus     = (E(z) - v_p - R0*I_m)/(1 + R0*G)
I_battery = I_m + G*V_bus
load heat = opening*V_bus^2/R_load
```

Shared battery resistance couples all consumers. The coupled midpoint matrix includes
charge, polarization, motor current and mechanical responses. Simulation-owned factors
update when duties, accessory openings or internal interval durations change. Gear,
cylinder, converter and clutch responses use these same factors. Unpowered older
models retain their previous solver path and fingerprints. Affine chemical and RC
energy are quadratic, so midpoint electrical transfers have independent conservation
checks. The same physics supports motor regeneration.

`pressure_duty_controller` uses the existing integer-clock PI/clamping transition,
with gains in `fraction_pa` and `fraction_pa_s`, explicit duty bounds within [-1,1],
and an initial integral duty. It owns a `battery_motor` duty channel. Agents change
`pressure_setpoint`; direct duty overrides return `controlled_input`. Read
`sampled_pressure`, `pressure_error`, `integral_duty` and `command_duty`. Held duty,
charge, polarization and control memory share snapshots, forks, cancellation and
complete rollback. Sampling and successful stepping remain allocation-free.

`battery-regulated-pump` combines finite battery supply, accessory load pulses and a
5 ms duty regulator with the pressure-clutch laboratory. Its 50 C capacity is a small
synthetic test inventory, not a vehicle battery measurement. At 15 s SOC drops from
0.8 to about 0.627, while pressure ends at about 200.828 kPa for a 200 kPa target.
All 761 JSON/asset/MCP boundaries agree. Tests separately check analytic RC relaxation,
resistive-load inventory, independent motor/circuit RK4 integration, physical tick
refinement, signed duty and regeneration, parallel winding equivalence, geared/clutch/
pump coupling, late depletion rollback, branches, cancellation and zero allocations.

Battery BMS/chemistry/ageing and temperature feedback, fault/contactors, PWM/current
control, sensor dynamics, actuator mechanics, full ECU/TCU and calibration remain open.
Battery parameters and all laboratory inputs remain `unverified`.
