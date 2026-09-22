# Quasi-steady torque-converter network

`torque_converter` participates in the same shaft, motor, cylinder, clutch and ideal-gear
solve. Pump and turbine are distinct rotational nodes with explicit inertia. The stator
is stationary ground; its reaction is observable but it performs no work. Fluid loss
feeds an optional thermal node or the external heat-rejection ledger. A separate
parallel `clutch` supplies lockup. All parameters in the laboratory are synthetic and
`unverified`.

## Explicit maps and equations

Four maps are required: `pump_positive`, `pump_negative`, `turbine_positive` and
`turbine_negative`. The member with larger absolute speed is the reference driver;
the pump wins an exact tie. The sign of that member selects its positive/negative map.
This is a mathematical reference-member convention, including during counterrotation.
It does not infer an unavailable reverse or coast characteristic.

For driver speed `wD` and follower speed `wF`, `s = wF/wD` lies in `[-1,1]`. Each map
contains 2–32 explicit points with dimensionless `speed_ratio`, `torque_ratio` R and
`capacity_coefficient` C in `nm_s2_rad2`. C multiplies squared speed; it is not an inverse
K-factor. R and C interpolate linearly and are never extrapolated. Both speeds zero
produce zero reactions and mode zero.

```text
Tdriver   = -C(s) * wD * abs(wD)
Tfollower =  R(s) * C(s) * wD * abs(wD)
Tstator   = -(Tpump + Tturbine)
Qdot      = C(s) * abs(wD)^3 * (1 - s*R(s))
```

Compilation requires strictly increasing knots spanning `[-1,1]`, nonnegative C/R,
and `s*R(s) <= 1` throughout every segment. Checking only knots is insufficient:
R linear in s makes efficiency quadratic; any interior maximum is checked too.
Slopes must be finite. At `s=1`, C must be zero and R one, giving zero fluid torque at
equal same-direction speed. At `s=-1`, pump-positive/turbine-negative and
pump-negative/turbine-positive maps must give matching physical reactions. This prevents
a jump when the reference member changes during counterrotation. The endpoint comparison
allows only `64*epsilon*(abs(a)+abs(b))` rounding tolerance. Map arrays are owned and
immutable; normalized coefficients enter the model fingerprint.

These restrictions define Power!'s current passive signed-map model. They do not claim
to cover arbitrary measured converter curves. General map-based driving/coasting
conventions are documented by [MathWorks Torque Converter](https://www.mathworks.com/help/sdl/ref/torqueconverter.html)
and its [two-mode example](https://de.mathworks.com/help/sdl/ug/torque-converter-two-mode.html).
The four signed maps, interpolation validation and solver below are Power! design;
no source code or measured parameter set was copied from those references.

## Coupled integration and limits

Converter reactions use interval midpoint speeds. When cylinders are present, a joint
Newton system solves their discrete crank-angle work and both converter-port speeds.
Every unit-torque response includes the electromechanical system and permanent gear
projection. Clutch constraint iterations call this same nonlinear solve; capture and
reversal subdivision regenerate interval responses. There is no lagged converter torque
applied after cylinder or clutch integration.

The nonlinear solve has 24 iterations and 12 halving line-search attempts per iteration.
The cylinder angle residual tolerance is `2e-14 rad`. A converter speed residual uses
`2e-12 + 64*epsilon*(abs(value)+abs(free_prediction)) rad/s`. Analytic piecewise map
Jacobians and finite-difference cylinder work derivatives build the joint system.
Existing 0.25-rad cylinder travel, valve/burn resolution, clutch iteration/event and gear
constraint limits still apply. At most eight converters are supported within the existing
32-node, 64-component and 64-state budgets. Each converter adds four logical observable
history states: two mean torques, mean heat power and cumulative heat. Compensated heat
summation also participates in copy/hash/rollback.

Accepted interval heat is `-h*(Tp*wp_mid + Tt*wt_mid)`, so the mechanical work removed
is the heat recorded. Negative heat beyond the solved-speed rounding tolerance rejects
the interval; only rounding-sized negative residue is clamped to zero. Torques and heat
power are duration-weighted across accepted internal intervals, then divided by the full
tick. Speculative event trials never commit their heat or reactions. All converter,
clutch, gear, gas, burn-history, input and global-ledger state rolls back on a failed or
cancelled batch. Forks own their workspaces. Tests exercise allocation-free capture.

Midpoint integration is second order for smooth standalone converter motion. The coupled
fired model retains explicit wall-temperature coupling and interval-average clutch
breakaway, so it does not claim uniform second order through all transitions. Refine ticks
for a numerical failure or accuracy study; inspect map slopes, inertia/speed scales and
clutch constraints before recreating a session. A valid map is not a guarantee that every
chosen timestep is solvable.

## Shared model and observable contract

JSON uses required `node_a` (pump), `node_b` (turbine), four arrays under `parameters`,
and optional `heat_node`. It accepts no converter input channel, carrier port or implicit
map defaults. `ComponentDefinition.TorqueConverter` exposes the same Core model. Map
errors carry the component ID and an actionable field such as `converter.pump_positive`
or `converter.counter_rotation`.

| Field | Meaning | Unit |
|---|---|---|
| `torque` | Last complete tick's mean pump torque | Nm |
| `torque_at_b` | Last complete tick's mean turbine torque | Nm |
| `torque_at_c` | Last complete tick's mean stationary-stator reaction | Nm |
| `heat_flow` | Last complete tick's mean fluid heat power | W |
| `fluid_heat` | Cumulative accepted fluid heat | J |
| `speed_ratio` | Current follower/driver signed speed ratio; zero when stopped | fraction |
| `converter_drive` | 0 stopped, 1 pump positive, 2 pump negative, 3 turbine positive, 4 turbine negative | state code |

Mean torques/power start at zero. Boundary input updates do not rewrite prior-tick mean
outputs. `torque_at_c` names the stator reaction here; this component has no third rotor.
Converter-containing models advertise `quasisteady_converter_powertrain`, and add
fingerprint tag 10. Converter-free model fingerprints and trajectories remain unchanged.
Portable asset v10 retains all four maps; v1–v9 readers and authentic fixtures remain.

## Laboratory and evidence

Request MCP example `fired-converter`, or run:

```sh
dotnet src/Power.Cli/bin/Release/net10.0/Power.Cli.dll assets/labs/fired-converter.power.json --output artifacts/reports/fired-converter.json
```

The 0.8-s laboratory starts with a 600-rpm pump and 300-rpm turbine, feeding the sun of
a planetary and a 3:1 final drive. Ring braking selects reduction; a sun/ring clutch
selects direct drive. Independent scheduled lockup, release and recapture exercise fluid
and friction paths. These are prescribed events, not an automatic transmission controller.

At 50,000-ns ticks, all 87 report boundaries replay exactly through CLI, portable assets
and MCP. The model fingerprint is `839d03901973668d` and final state hash is
`834a679376b7a6fd`. Final pump/turbine speed is approximately 73.37748 rad/s, load speed
6.988331 rad/s, fluid heat 24.27663 J and lockup heat 22.84709 J. The shift clutch and
brake add 157.18199 J and 83.42289 J. The shared heat node reaches 301.438643 K; total
energy residual is about `3.30e-11 J`. These numbers describe a synthetic transient.

Independent tests cover the analytic `C(s)=k(1-s), R=1` two-inertia solution, analytic
stall decay, second-order refinement, thermal and external heat routing, stator balance,
reverse/coast/counterrotating states, shared converter ports, gear reflection, lockup,
full-batch rollback, cancellation, forks and zero allocation. Fired-model refinement
checks a dimensionless combined distance in final speed, fluid heat and lockup heat to
a 3,125-ns run, using five tick sizes. It also bounds absolute differences below
0.0002 rad/s or J respectively. Individual heat errors need not decrease at every
halving near events. This check is separate from standalone analytic order. Signed-map
ownership/validation and re-signed malformed portable records have dedicated checks.

The [hydraulic network](HYDRAULIC_NETWORK.md) now supplies pressure-derived lockup and
shift capacity. Fluid angular momentum, converter fill/pressure dynamics, rotating/freewheeling stator mechanics,
temperature-dependent properties, pump/regulator and piston dynamics, complete DCT/AT topology and
ECU/TCU coordination remain open. Complete engine behavior, OEM measurements and vehicle
calibration also remain open. Studio has schematic fluid ports and prepared tests;
actual Editor/Play/IL2CPP evidence remains separate and unavailable in this environment.
