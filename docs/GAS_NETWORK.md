# Compiled gas network — Core checkpoint, 2026-09-14

Finite gas networks now run through `CompiledModel` and `Simulation`. The checkpoint
covers fixed-volume chambers, fixed pressure/temperature reservoirs, controlled
orifices and thermal wall links. It does not connect gas exchange to a moving cylinder.

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

## Validation and next checkpoint

Nine compiled-model groups supplement the six gas primitive groups, exercised against
both `net10.0` and `netstandard2.1` assemblies. See [validation](VALIDATION.md) for counts
and full verification scope. The Standard assemblies run on .NET 10 for these checks;
this is not Unity Editor or IL2CPP evidence.

JSON, schema/capability discovery, portable assets, CLI/MCP examples and Unity views
remain the next coherent integration checkpoint. `PowerAsset.Create` explicitly rejects
finite gas nodes because v1/v2 have no representation for composition or restriction
parameters. Existing v1/v2 fixtures continue to load and replay. The future format must
retain those readers and cover gas replay end to end before advertising gas models.

Gas-cylinder pressure work, combustion, valve timing, species mixing, calibrated vehicle
samples and the full engine/transmission/control milestones remain open.
