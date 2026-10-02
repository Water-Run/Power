# Electromagnetic needle actuation and sampled dose feedback

An actuated liquid injector reads the lift of a translational needle rather than
closing an ideal mass gate at the requested dose. A position-dependent solenoid,
explicit needle mass, return spring/damping and elastic travel stops supply the
motion. A sampled driver owns coil voltage and stops its command when the cycle
window closes or measured delivery reaches the latched request.

Current decay, mechanical closing delay and seat rebound can continue delivery
after that command. Actual fuel remains in the source/film/gas ledger; requested
dose is a control target, not an imposed physical cutoff. This is a research
actuator and simple on/off feedback. Nonlinear magnetic maps, saturation,
hysteresis/eddy losses, temperature-dependent resistance, switching/flyback,
battery supply, axial fluid force and calibrated injection remain open.

## Reciprocal magnetic and mechanical energy

`solenoid` uses a supplied constant winding resistance and linear inductance:

```text
L(x) = L_reference + gradient (x - x_reference) > 0
lambda = L(x) i
W_magnetic = lambda^2 / (2 L(x))
d(lambda)/dt = V - R i
F_magnetic = gradient i^2 / 2
```

Positive lift increases inductance and the magnetic force acts in that direction.
Both current polarities attract the armature. The force follows magnetic
coenergy, as described by [Modelica's reluctance-force guide](https://doc.modelica.org/Modelica%204.0.0/Resources/helpWSM/Modelica/Modelica.Magnetic.FluxTubes.UsersGuide.ReluctanceForceCalculation.html)
and the [Simscape solenoid equations](https://www.mathworks.com/help/simscape-electrical/ref/solenoid.html).
Power! uses its own reduced constitutive law and integration; no Modelica or
Simscape dependency is introduced. Inductance must remain positive at all
accepted and speculative positions. The model doesn't clip negative inductance
or replace missing magnetic measurements with a calibrated map.

The magnetic state is flux linkage. For an interval `h`, endpoint inductances
`L0,L1` and held voltage, a symmetric discrete gradient gives:

```text
A = (1/L0 + 1/L1) / 4
lambda1 = ((1 - h R A) lambda0 + h V) / (1 + h R A)
i_bar = A (lambda0 + lambda1)
F_bar = gradient (lambda0^2 + lambda1^2) / (4 L0 L1)
W_supply = h V i_bar
Q_copper = h R i_bar^2
delta W_magnetic = W_supply - Q_copper - F_bar (x1 - x0)
```

Flux is eliminated analytically for a proposed endpoint position. The remaining
force and its analytic position derivative join the same nonlinear mechanical
solve as cylinders, converters, hydraulic pistons and speculative clutches.
Accepted motion commits flux, electrical work, copper heat and mean force once.
Copper heat enters the declared thermal node or external rejected heat; magnetic
and mechanical stored energy remain separate.

Independent simultaneous ODE integration verifies smooth second-order refinement.
The stationary RL limit also has an analytic current reference. Energy-conjugate
stepping doesn't by itself establish accurate motion at a coarse timestep;
electrical time constants, stroke travel and contact events still need resolution.

## Needle mass, spring and elastic stops

The needle is an ordinary translational node with kg, m and m/s. An ordinary
`linear_spring` supplies preload and damping with explicit heat routing.
`travel_stop` adds reversible unilateral energy at the nominal stroke limits:

```text
E_stop = K/2 [max(0,x_min-x)^2 + max(0,x-x_max)^2]
```

Its discrete reaction is the negative energy gradient between accepted endpoints.
Penetration stores energy rather than clamping position. The analytic derivative
shares the mechanical solve; nominal-stroke travel per interval is limited to
one quarter of the span. A slider has one stop owner, including the stops already
owned by a hydraulic piston. Shared hydraulic/solenoid coordinates remain
possible, with each force contributing to the same coordinate.

Seat rebound is physical within this elastic reduction. A zero opening at one
snapshot doesn't prove zero flow throughout a later interval. Contact damping,
seal friction, impact restitution and measured seat/needle behavior remain open.

## Physical opening and delivery

Optional `parameters.needle` on `liquid_fuel_injector` contains a translational
`needle_node` plus `closed_position` and `full_open_position` in m/mm. Actual opening
is the bounded linear lift ratio:

```text
opening = clamp((x - x_closed)/(x_full_open - x_closed), 0, 1)
```

The passive [liquid rail/nozzle law](LIQUID_FUEL_INJECTION.md) integrates pressure
head with that effective opening. It keeps finite inventory and pressure-energy
limits, but doesn't cap physical delivery at the requested dose or erase flow
when the crank window closes/reverses. An open needle can admit fuel even with a
stopped crank or zero requested dose. The crank window still latches target
history for feedback; delivery outside a new observed window remains part of
the latest observed-cycle and total histories.

Without `needle`, the prior ideal quota-limited injector path is retained, with
unchanged model fingerprints and replay. Needle-equipped models declare their
different fidelity. Needle is pressure-balanced in this increment; no axial
pressure/jet force is inferred. The existing negligible-liquid-volume receiver
exports its displacement pressure work explicitly.

## Integer-clock driver and input ownership

`needle_driver` names an actuated injector, its solenoid and the same timing
crank. It requires an explicit positive `sample_period_ns`, aligned to physical
ticks and no greater than one second, and a positive `drive_voltage` in V. The
owned coil begins at zero voltage. At each due sample the driver records the
latched dose and actual delivered mass, then holds drive voltage while the forward
window has remaining target delivery; otherwise it holds zero voltage.

The driver owns the solenoid voltage channel. Agents write the injector's `kg`
request; direct voltage writes return `controlled_input`, identify the writable
command channel and preserve state/revision. Initial and event writes don't
advance control history. Sampling phase follows integer simulation time. This
driver doesn't implement peak/hold current regulation, predictive closing
compensation, PWM/flyback or complete ECU/TCU behavior.

## Definitions, channels and transactions

| Component | Parameters and ports |
|---|---|
| `solenoid` | Translational A node; V input; nonnegative Ohm resistance, positive H reference inductance and H/m gradient (`h_m`), m/mm reference position, A initial current; optional thermal sink |
| `travel_stop` | Translational A node; increasing m/mm limits and positive N/m stiffness |
| `needle_driver` | Rotational timing A node; stable injector/solenoid IDs, integer sample period and V drive level |

Definitions reject unrelated quantities, wrong units/domains, invalid initial
inductance, duplicate stop/voltage ownership, mismatched needle/coil/crank and
unaligned sample periods. Core clients use `SolenoidCoil`, `StrokeStop`,
`NeedleDrive`, `InjectorNeedleDefinition` and the independent magnetic/contact laws.

Solenoid outputs expose instantaneous `current`, last-tick discrete mean `force`,
magnetic `internal_energy`, cumulative `copper_heat` and electrical `source_work`.
Stop outputs expose elastic energy and instantaneous reaction. Driver outputs
expose held `command_voltage` and last-sample requested/delivered dose. Injector
`opening` is actual position opening, with actual last-tick mean delivery.
All IDs and units are discoverable through validation and session creation.

Four reported state entries per solenoid and three per driver join the bounded
state budget. Flux, mean force, compensated heat/work, sampled control state,
held inputs, needle/stop and all source/phase histories copy/hash/rollback with
the complete simulation. Successful active stepping and snapshots allocate no
managed memory. Cancellation, failed batches and independent forks preserve
electrical, mechanical, thermal and controller histories together.

Asset v20 adds typed magnetic, stop, needle and driver tables while retaining
v1-v19 readers. Bounded lengths/counts, digest, units, distinct ownership and
forged downgrade protection are checked. An authentic v19 liquid-injection
fixture retains its fingerprint and same-runtime upgraded replay. See
[ASSET_FORMAT.md](ASSET_FORMAT.md).

## Experiments and acceptance

`needle-actuated-cylinder` connects the actuator and sampled feedback to the
finite-rail/film fired cylinder. Its 0.6-s boundary can retain liquid film during
the last closing/evaporation transient. Complete source/film/gas/reaction inventory
is verified rather than assuming a dry film or exact target delivery. JSON,
portable assets and an actual MCP child server share the same definitions/replay.

The isolated actuator requests 8 mg and observes excess delivery through current
decay, closing motion and small later seat rebounds. Those quantities are research
results, not calibrated injector timing or an accepted dose-tracking controller.
[VALIDATION.md](VALIDATION.md) records the numerical evidence and limits.
Prepared Unity coil, stop, controller and scaled needle views still require
actual Editor/Play verification. Full powertrain, measured actuation, refined
magnetics/electronics/fluid forces, rail refill and ECU/TCU remain unfinished.
