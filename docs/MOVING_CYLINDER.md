# Moving-cylinder gas exchange

A `gas_cylinder` connects one rotational crank to one gas chamber. Unlike the sealed
adiabatic benchmark, this chamber carries independent mass and internal energy, so
restrictions and wall links can change its state while pressure drives the crank.
The component is available through Core, JSON, CLI, MCP and portable assets. It does
not model piston inertia or detailed chemistry. Separate
[premixed combustion](PREMIXED_COMBUSTION.md) and [crank-angle timing](VALVE_TIMING.md)
components now supply fuel-energy conversion and control the connected restrictions.

## Model contract

```csharp
var definition = new ModelDefinition
{
    StepNanoseconds = 50_000,
    Nodes = [NodeDefinition.Rotor(1, 0.2, 60),
        NodeDefinition.CylinderGas(2, 100_000, 300),
        NodeDefinition.Thermal(3, 500, 350)],
    Components = [ComponentDefinition.GasCylinder(10, 1, 2, new()
    {
        Bore = new(86, Unit.Millimeter), Stroke = new(86, Unit.Millimeter),
        RodLength = new(143, Unit.Millimeter), Phase = new(0, Unit.Radian),
        CompressionRatio = 10, BackPressure = new(1, Unit.Bar)
    }), ComponentDefinition.GasReservoir(11, 2, 50e-6, 110_000, 300, 0.8, 100, 1),
        ComponentDefinition.GasHeatLink(12, 2, 3, 2.5)]
};
```

The gas node supplies initial absolute pressure, temperature, R and gamma. Its `Storage`
quantity is zero/None: exactly one cylinder component owns the volume. The compiler
derives initial mass and energy from the geometry at the crank's initial angle, including
phase. It rejects an independently specified volume or two cylinder owners for a chamber.
Unconnected fixed gas nodes still require positive explicit volume.

In JSON, use `domain: "gas"` and omit `storage` for a moving chamber. The `gas_cylinder`
component requires `node_a` (rotational), `node_b` (gas), and parameters `bore`, `stroke`,
`rod_length`, `phase`, `compression_ratio` and `back_pressure`. No composition or initial
gas state is duplicated in this component. The gas node exposes pressure, temperature,
mass and internal energy; the cylinder exposes volume, piston displacement and crank
torque. Ports, restrictions, opening bounds and wall links use the existing
[gas-network contract](GAS_NETWORK.md).

Models containing moving chambers without timed restrictions or premixed tracking report `moving_cylinder_gas_exchange` fidelity and
add solver fingerprint tag 5. Existing linear, sealed-cylinder and fixed-volume-only
models retain their fingerprints and stepping. Composition remains fixed, connected gas
nodes must agree, and all parameters remain `unverified`.

## Equations and conservative coupling

For a calorically perfect gas with fixed composition:

```text
p = (gamma - 1) U / V(theta)
T = U / (m cv)
dm/dt = sum(inflow) - sum(outflow)
dU/dt = -p dV/dt + Q_wall + sum(mdot_in h_in) - sum(mdot_out h_out)
tau_gas = (p - p_back) dV/dtheta
```

The mass/energy balance follows the standard open-system first law; see
[Cantera's control-volume equations](https://www.cantera.org/stable/reference/reactors/controlreactor.html).
That reference supports the equations, not the Power! integration scheme or validation.
The gas uses the existing bidirectional compressible nozzle law rather than Cantera's
linear, one-way valve implementation. Back-pressure work is external source work;
reservoir enthalpy and wall exchange retain their existing ledger signs.

The implementation uses a symmetric operator split for models containing moving chambers:

1. Advance gas exchange and wall heat by half a tick at the opening crank geometry.
2. Solve coupled electromechanics and adiabatic pressure work over the full tick with
   the bounded discrete-gradient crank solver.
3. Advance gas exchange and wall heat by half a tick at the resulting crank geometry.
4. Apply accumulated gas-wall heat and electromechanical losses to the thermal solve.

During step 2, mass is fixed and `U_new = U_old (V_old/V_new)^(gamma-1)`. The mean gas
pressure and torque come from the divided difference of this same energy change.
The crank gains the gas work minus back-pressure work; the chamber loses exactly the
corresponding gas work to floating-point accuracy. `log1p`/`expm1` and the analytic
volume divided difference avoid subtracting nearly equal states around small steps and
dead centers. Multiple cylinders can share a crank or act through coupled shafts.

The split has second-order convergence for the tested smooth choked-flow case without
wall transfer. Wall temperatures remain fixed during both gas half-steps, followed by
the existing thermal solve: wall-coupled accuracy remains first order. The near-equilibrium
flow limiter can also change local order. Conservation does not establish accuracy.

## Bounds, failure and compatibility

Crank travel is limited to 0.25 rad per tick; the nonlinear solve uses at most 16
iterations and 10 line-search trials. Each gas half-step retains its 4096-substep bound,
2% target relative change and 25% corrected-change rejection. Invalid/nonfinite gas
states, outputs or solver exhaustion reject the entire caller batch, including all prior
ticks and scheduled inputs. Reduce `step_ns` and inspect flow area, gas state, conductance,
crank speed and inertia before retrying. Cancellation and forks retain all gas and ledger
state; successful stepping and caller-buffer snapshots allocate no managed memory.

Asset v4 adds a bounded indexed geometry record for each gas cylinder, preserving all
v1/v2/v3 readers. It never serializes solver workspaces. An authentic fixed-volume v3
fixture verifies that introducing moving geometry does not change prior gas fingerprints
or replay. See [asset format](ASSET_FORMAT.md) and [fixture provenance](../tests/Power.Tests/Fixtures/README.md).

## Experiment and evidence

The [moving-cylinder laboratory](../assets/labs/moving-cylinder.power.json) motors one
cylinder with two reservoir restrictions and a finite thermal wall. The eight time-based
opening events exercise flow into and out of the chamber and include ticks between report
and presentation boundaries. This is an uncalibrated motoring experiment; the schedule
is not an ECU, cam profile, four-stroke engine controller or combustion model.

Tests compare a closed chamber with the existing sealed-cylinder implementation through
forward/reverse rotation and dead centers, and compare an open chamber against an
independently written RK4 integration of the governing ODEs. The latter writes geometry,
choked mass flow and pressure work directly from the equations. Step refinement checks
smooth-flow and wall-coupled accuracy separately. Additional checks cover multiple
coupled/shared cranks, mixed sealed/open cylinders, conservation, malformed ownership,
unit normalization, atomic failure/recovery, forks, cancellation, zero allocations,
portable compatibility and all JSON/MCP/asset report boundaries.

Unity includes a moving-piston view, gas connections and import/Play tests. Actual
Editor, rendering, Play Mode and IL2CPP evidence is still pending. See the
[validation record](VALIDATION.md) for executed checks and the [roadmap](ROADMAP.md)
for remaining engine, transmission, control and calibration work.
