# Compiled gas network

Finite gas networks now run through `CompiledModel` and `Simulation`. The checkpoint
covers fixed-volume chambers, fixed pressure/temperature reservoirs, controlled
orifices and thermal wall links. The [moving-cylinder extension](MOVING_CYLINDER.md) now connects gas exchange to crank-dependent volume and pressure work; the fixed-volume solver described below retains its original behavior.

## C# API and units

```csharp
var model = CompiledModel.Compile(new ModelDefinition
{
    StepNanoseconds = 100_000,
    Nodes = [NodeDefinition.GasVolume(1, 0.002, 2e6, 900)],
    Components = [ComponentDefinition.GasReservoir(10, 1, 2e-5, 1e5, 300,
        dischargeCoefficient: 0.9, channel: 100, opening: 0)]
});
var simulation = model.CreateSimulation();
simulation.SubmitInputs([new Scalar(100, 0.5)]);
var status = simulation.Step(100_000_000);
var values = new Scalar[model.OutputCount];
var snapshot = simulation.ReadSnapshot(values);
```

`GasVolume` takes volume in m³, pressure in Pa, temperature in K, and optionally
R in J/(kg K) and gamma. A gas node stores volume in `Storage`, temperature in `Initial`,
pressure in `Position`, and composition in `Gas`. Litres, bar and square millimetres are
accepted by explicit quantities and normalized before fingerprinting.

`GasOrifice` joins two gas node IDs. `GasReservoir` joins one node to a fixed boundary;
`NodeB == 0` identifies that reservoir. `GasHeatLink` joins a gas node and a thermal node
with conductance in W/K. Connected gas nodes must share exactly the same R and gamma.
Opening is a dimensionless fraction in [0,1], validated for initial, direct and scheduled
inputs. A zero input-channel ID leaves the initial opening fixed. Gas-only networks need
no dummy rotor. Limits remain 32 nodes, 64 components and 64 scalar states; each gas
volume consumes two states.

Each gas node exposes pressure, temperature, mass and internal energy. Restrictions
expose signed A-to-B mass flow; heat links expose signed gas-to-wall heat flow.
Reservoir enthalpy is positive inward. The energy residual is
`source_work + reservoir_enthalpy - heat_rejected - stored_energy_change`.
The mass residual is `sum(mass - initial_mass) - cumulative_reservoir_mass`.
Floating-point residuals are assessed against physical scales, not exact zero.

## Numerical method and boundary

The gas solver uses explicit substeps with a Heun predictor/corrector. The tick's initial
maximum relative mass/energy rate selects a uniform substep count, targeting 2% change
per substep. More than 4096 substeps, nonphysical states, nonfinite values or a corrected
mass/energy change exceeding 25% rejects the whole batch. Reduce `StepNanoseconds` and
recompile, or inspect flow area, volume, conductance and initial conditions.

The nozzle law has a singular derivative at equal pressure. Each evaluation limits the
transferred energy to the connected pair's equal-pressure amount, scaling mass and
upstream enthalpy together. For finite volumes this energy is
`abs(pA-pB) / ((gammaA-1)/VA + (gammaB-1)/VB)`; a fixed reservoir omits the B term.
This prevents isolated-pair pressure-crossing oscillations while preserving the paired
ledgers. The limiter changes near-equilibrium integration; second-order accuracy is
asserted only for the smooth, unbounded choked-flow refinement case in the tests.

Wall temperature stays at its opening value during gas substeps. Accumulated wall heat
then enters the existing thermal solve. This coupling is first order in the outer tick;
large-step stability or conservation alone does not establish accuracy. The wall test
compares finite-time temperatures with the analytic two-capacity solution. This method
is not the earlier proposed pairwise implicit solver and does not validate that proposal.

Mass, energy, reservoir sums and compensated-ledger corrections belong to simulation
state and are included in copying, rollback, forks and hashes. Successful stepping and
caller-buffer snapshots allocate no managed memory. A failed scheduled batch restores
all prior ticks and inputs, including failure after earlier successful ticks. Input
updates and terminal scheduled events also reject nonfinite gas observables.

Models with gas nodes add solver fingerprint tag 4. Existing linear/cylinder model
fingerprints and state hashes retain their previous construction. Sample parameters
remain `unverified`.

## JSON, agent and portable integration — 2026-09-22

The [gas-network laboratory](../assets/labs/gas-network.power.json) is the shared example
for JSON, CLI, MCP and portable replay. It contains two gas chambers, a controlled
internal restriction, a controlled reservoir restriction and a wall heat link. Its events
include ticks between report/presentation boundaries; every report boundary is compared
against decoded asset replay. The parameters remain synthetic and `unverified`.

`power.model.v1` adds these explicit definitions:

| Definition | JSON fields and units |
|---|---|
| Gas node | `domain: "gas"`; `storage`: m3 or l; `initial`: k; `position`: pa or bar; `gas`: gas_constant in j_kg_k and gamma > 1 |
| Gas orifice | `kind: "gas_orifice"`; node_a/node_b; `initial_input`: fraction in [0, 1]; parameters: area in m2 or mm2 and discharge_coefficient |
| Reservoir orifice | Gas orifice with absent/zero node_b; also requires reservoir_pressure in pa or bar and reservoir_temperature in k |
| Gas wall link | `kind: "gas_heat_link"`; node_a is gas, node_b is thermal; parameters: conductance in w_k |

A missing or zero `input_channel` keeps the explicit initial opening fixed. Reservoir
parameters are forbidden for a two-volume restriction. Composition is required only on
gas nodes. New check fields are `mass_flow`, `heat_flow`, `reservoir_enthalpy` and
`mass_residual`; existing gas-state and energy check fields remain available.

`CompiledModel.ValidateInput` checks static channel/value constraints without mutating
state. Experiment validation and portable asset creation use it for all scheduled
openings, including later events. Runtime submission/stepping still performs additional
state-dependent observable checks and retains complete rollback.

`power.asset.v3` and later versions retain gas composition, area, discharge coefficient and reservoir
pressure with bounded indexed extension records. Wall conductance, reservoir temperature,
initial openings and input-channel IDs use the base component fields. The v1/v2 readers
remain supported for their original model sets and reject gas definitions. Authentic
pre-change fixtures verify backward compatibility. See [the asset format](ASSET_FORMAT.md).

MCP capabilities version 0.8.0 advertises the gas domain, components, fidelity, opening
bounds and bounded solver limits. `get_example_model` accepts `gas-network`. The build
exports `GasNetwork.powerasset`; Studio adds schematic vessels, reservoir markers and
restriction/heat paths with existing inputs and output channels. Its new import and
Play Mode tests require an actual Unity Editor run and are not covered by .NET evidence.

## Validation and remaining engine work

The nine compiled-model groups and six gas primitive groups continue running against
both Core targets. Portable tests additionally cover mixed cylinder/gas/thermal models,
non-SI quantities, non-default composition, extension corruption, missing/duplicate
records, v1/v2 compatibility, scheduled bounds, cancellation and event-cursor rollback.
JSON/Core equivalence and actual MCP replay cover the integration boundary.
See [validation](VALIDATION.md) for the serial verification results.

The Standard assemblies run on .NET 10 for these checks; this is not Unity Editor or
IL2CPP evidence. The fixed-volume-only solver equations, integration limits and fingerprint construction
remain unchanged for models without timed restrictions or premixed tracking. Models with moving chambers
or timed restrictions use the separately versioned split coupling documented in
[MOVING_CYLINDER.md](MOVING_CYLINDER.md) and [VALVE_TIMING.md](VALVE_TIMING.md).

Optional [premixed combustion](PREMIXED_COMBUSTION.md) now transports fuel, fresh air
and products at constant gas properties. Detailed species thermochemistry, calibrated
vehicle samples and the complete engine/transmission/control milestones remain open.
