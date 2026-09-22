# Premixed combustion and fuel-energy accounting

`premixed_combustion` couples a prescribed Wiebe burn profile to a crank and a finite
gas chamber. Fuel, fresh air and inert products are transported through the gas network;
reaction consumes the available limiting reactant and converts stored chemical energy
into thermal gas energy. Pressure work drives the same crank solver used by moving
cylinders. Core, JSON, CLI, MCP and asset v6 share these definitions.

This is a lumped, constant-property premixed model. Every constituent in a connected
network shares one R and gamma. The three mass classes do not represent detailed species,
variable heat capacities, reaction kinetics, flame propagation, autoignition, knock,
emissions, fuel evaporation or injection. The example uses an already mixed gaseous
inlet. A prescribed burn and passing conservation tests do not establish measured engine
performance or complete the full powertrain objective.

## Composition and ports

A gas node optionally adds `premixed` to its existing `gas` object:

```json
"gas": {
  "gas_constant": { "value": 287, "unit": "j_kg_k" },
  "gamma": 1.35,
  "premixed": {
    "lower_heating_value": { "value": 44000000, "unit": "j_kg" },
    "stoichiometric_air_fuel_ratio": 14.7,
    "initial_fractions": { "fuel": 0.04, "fresh_air": 0.96 }
  }
}
```

The heating value and stoichiometric air/fuel mass ratio must be positive and finite.
Fuel and fresh-air fractions must be nonnegative, with sum at most one. The remainder
is inert products. Fresh air represents the oxidizer together with its diluent; consuming
`r` kg of fresh air with 1 kg fuel creates `1+r` kg products. Excess fresh air or fuel
remains available; products cannot react again.

Each reservoir restriction on a premixed node must specify explicit
`reservoir_fractions`, using the same two fields. Fractions are forbidden on other
components or internal restrictions. On inflow, the boundary supplies that composition;
on outflow, it removes the finite volume's actual composition. Connected finite gas
volumes must share tracking, R, gamma, LHV and stoichiometric ratio. Incompatible or
untracked connections are rejected; chemical inventories cannot disappear at a port.

The gas solver transfers each constituent with the same signed mass flux and upstream
fractions as total gas. It evolves nonnegative constituent masses and reconstructs total
mass from their sum. A premixed step is also bounded by total outgoing flow, even when
incoming and outgoing total mass rates nearly cancel. No chemical inventory is created
by pressure equalization or by reservoir backflow.

A premixed gas adds three stored constituent values to the declared state budget. A burn
component adds one irreversible angle frontier; all remain within the existing 64-state
bound. Compensated boundary and reaction ledgers participate in rollback, hashing and forks.

## Burn law and crank history

The component connects `node_a` (crank) to `node_b` (premixed gas). A moving chamber must
use its own geometry crank, and each chamber allows at most one burn component. A fixed
vessel may use an independent crank for analytical experiments.

```json
{
  "id": 15,
  "kind": "premixed_combustion",
  "node_a": 1,
  "node_b": 2,
  "input_channel": 103,
  "initial_input": { "value": 1, "unit": "fraction" },
  "parameters": {
    "cycle_angle": { "value": 720, "unit": "deg" },
    "start_angle": { "value": 340, "unit": "deg" },
    "duration_angle": { "value": 80, "unit": "deg" },
    "shape_exponent": 3,
    "burn_coefficient": 6.9
  }
}
```

The cycle is explicitly 360 or 720 degrees. Start angle is relative to the actual crank,
not implicitly offset by cylinder geometry phase. Duration is in [1e-6 rad, cycle angle];
shape exponent `n` is in [1,16], and coefficient `a` in (0,50]. Start is normalized modulo
the cycle. All angles require units. For forward progress `z` from burn start, clipped
to [0,1], the integrated hazard is `H(z) = a z^n`. Each full cycle contributes `a`.

Over newly traversed forward angles:

```text
hazard = burn_multiplier * delta(H)
limiting_fuel = min(fuel_mass, fresh_air_mass / stoichiometric_air_fuel_ratio)
burned_fuel = limiting_fuel * (1 - exp(-hazard))
consumed_air = stoichiometric_air_fuel_ratio * burned_fuel
created_products = burned_fuel + consumed_air
released_heat = LHV * burned_fuel
```

For a closed charge and multiplier 1, the burned fraction is `1-exp(-a z^n)` of its
initial limiting-fuel amount. It is **not** forced to one at the duration boundary:
`exp(-a)` remains unburned after one full burn window. Small exposures use `expm1` to
avoid cancellation. Fresh charge entering during an active window joins the well-mixed
reactants; there is no hidden, unlimited per-cycle heat source.

The optional input channel is `burn_multiplier`, a fraction in [0,1] that scales hazard.
Zero disables reaction; it does not stop fuel entering through an open inlet. This input
is not an injector command or a predictive ignition controller.

Each component stores the greatest crank angle reached, initialized to the starting
angle. Reaction occurs only beyond that frontier. Stopping, rotating backward, or
retracing previously visited angles cannot release heat again. Disabled forward travel
still moves the frontier, so re-enabling does not release missed heat. Starting inside
a burn window consumes only its remaining forward exposure. After a large reversal,
burning remains suppressed until the crank exceeds its prior maximum; bidirectional
engine ignition and controller-driven re-arming remain future control work.

## Energy and numerical coupling

Gas internal energy remains thermal: `U = m cv T`. Chemical energy is separately
`E_chemical = m_fuel LHV`. Reservoir total enthalpy includes both `mdot cp T` and the
transported chemical energy. Global stored-energy change includes chemical inventory,
so combustion is an internal conversion, not additional external source work:

```text
energy_residual = mechanical/electrical source work + reservoir total enthalpy
                  - rejected heat - change(total stored energy)
```

`net_fuel_energy_in` separately exposes the chemical part of the boundary ledger. It is
net inflow, including unburned fuel leaving the model; it is not gross fuel delivery or
a steady-state fuel-consumption metric. `fuel_residual` and `fresh_air_residual` compare
initial inventory, net boundary transfer, current inventory and cumulative reaction.
`mass_residual` continues to cover total gas mass. Constituent conversion preserves mass.

For a moving chamber, heat preview depends on the trial new crank angle and participates
in the nonlinear crank solve. With total heat `Q` during the tick and
`r = (V_old/V_new)^(gamma-1)`:

```text
U_after = (U_before + Q/2) r + Q/2
adiabatic_work = (U_before + Q/2) (1-r)
crank_work = adiabatic_work - back_pressure * (V_new - V_old)
```

The discrete pressure torque uses that same work, so gas energy, chemical energy and
crank work agree. Fuel is consumed only after the solve succeeds in the candidate state.
Gas transport still uses symmetric half-steps around crank work/reaction. Wall temperature
remains fixed across the outer tick; wall coupling is first order.

For an enabled burn, angle travel and endpoint-speed travel per tick must stay within
`min(0.25 rad, duration_angle/32)`, with a corresponding binary64 angle-resolution guard.
Released heat must not exceed 25% of pre-burn thermal energy in one tick. These are bounds
on admitted work and resolution, not accuracy guarantees. They apply alongside the gas
substep and cylinder iteration limits. Reduce `step_ns` on `numerical_failure`, align
scheduled events to the new tick, and recreate the model/session. Failed or cancelled
calls commit no state, input, frontier, chemical ledger or playback cursor.

## Outputs, compatibility and evidence

Premixed nodes add `fuel_mass`, `fresh_air_mass`, `product_mass` and `chemical_energy`
KPI fields. A burn component adds cumulative `fuel_burned` (kg) and `heat_released` (J).
The `burn_frontier` KPI field exposes its greatest visited crank angle (channel quantity
`burn_frontier_angle`, rad), so suppressed burning after reversal can be inspected.
Global channels add chemical energy, net fuel-energy input, fuel residual and fresh-air
residual. The channel quantities returned by discovery are authoritative; e.g. node fuel
mass is named `unburned_fuel_mass`. Ordinary gas internal-energy and flow outputs retain
their thermal and signed-flow meanings.

Premixed models add fingerprint tag 7 and normalized reaction/composition parameters.
Earlier nonreacting fingerprints and stepping remain unchanged. Asset v6 adds composition,
reservoir-fraction and burn records; authentic v1–v5 fixtures retain compatibility. The
new fidelities are `premixed_gas_transport` and `premixed_wiebe_combustion`.

Tests cover closed-vessel analytic fuel/air consumption and temperature, limiting
reactants, forward/backward reservoir transfer, closed-network constituent conservation,
independent reacting crank/gas ODE convergence, stopped/reversed/disabled burning,
malformed contracts, batch rollback, cancellation, forks and zero stepping/snapshot
allocations. The [fired-cylinder laboratory](../assets/labs/fired-cylinder.power.json)
drives a load through repeated intake/compression/burn/expansion/exhaust phases and
replays identically at all 63 JSON/CLI/MCP/asset report boundaries. Numerical evidence
and actual execution scope are recorded in [VALIDATION.md](VALIDATION.md).

[Cantera's ideal-gas reactor equations](https://www.cantera.org/stable/reference/reactors/ideal-gas-reactor.html)
provide the control-volume mass/species/energy context. The
[Ansys SI-engine example](https://chemkin.docs.pyansys.com/version/stable/examples/advanced/SI_engine_optimization.html)
uses explicit burn timing and Wiebe parameters. These references motivate the contracts;
their detailed chemistry, two-zone models and example parameters are not copied or
claimed as verification of this constant-property solver. There is no runtime dependency
on either package. All sample parameters remain `unverified`.
