// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using Power.Assets;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

/// <summary>
/// Evidence for gas volumes, orifices and gas-to-wall heat links once they are part of the compiled
/// model: the same physics validated in <see cref="GasChecks"/>, now reached through the solver,
/// the channel contracts, the energy and mass ledgers, and the transaction guarantees.
/// </summary>
internal static class GasModelChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("gas topology / units / composition agreement / diagnostics", Contracts),
        ("model blowdown against the analytic solution / step refinement", Blowdown),
        ("closed gas network mass and energy ledgers / equalisation", Network),
        ("gas-to-wall heat exchange / equilibrium / conserved energy", WallHeat),
        ("orifice opening input / range rejection / closed-valve isolation", Opening),
        ("gas batch rollback / cancellation / branching / bounded failure", Transactions),
        ("gas step and snapshot zero managed allocations", Allocations),
        ("reservoir filling / reverse flow / transported enthalpy", Filling),
        ("gas observable overflow rejects input and end-boundary schedules atomically", ObservableBounds)
    ];

    private const double Area = 2e-5, Cd = 0.9;

    // One 2 L vessel at 20 bar / 900 K discharging to a 1 bar, 300 K reservoir.
    private static ModelDefinition Vessel(ulong step = 100_000, double opening = 1, ulong channel = 0) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.GasVolume(1, 2e-3, 2e6, 900)],
        Components = [ComponentDefinition.GasReservoir(10, 1, Area, 1e5, 300, Cd, channel, opening)]
    };

    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Ok(SimulationStatus status) => Require(status == SimulationStatus.Ok, status.ToString());
    private static double Value(Simulation s, uint id, Field field)
    {
        var buffer = new Scalar[s.Model.OutputCount]; s.ReadSnapshot(buffer);
        return buffer.Single(v => v.Channel == Channels.Output(id, field)).Value;
    }

    private static void Contracts()
    {
        var model = CompiledModel.Compile(Vessel());
        Require(model.Fidelity == "finite_volume_gas_exchange" && model.StateCount == 2);
        Require(model.Channels.Select(c => c.Quantity).SequenceEqual(
            ["gas_pressure", "gas_temperature", "gas_mass", "gas_internal_energy", "mass_flow_a_to_b",
             "SourceWork", "HeatRejected", "StoredEnergyChange", "EnergyResidual",
             "reservoir_enthalpy", "mass_residual"]));
        Require(model.Channels.Single(c => c.Quantity == "gas_pressure").Unit == Unit.Pascal);
        Require(model.Channels.Single(c => c.Quantity == "mass_flow_a_to_b").Unit == Unit.KilogramPerSecond);
        Require(!model.Channels.Any(c => c.IsInput));

        var simulation = model.CreateSimulation();
        Near(Value(simulation, 1, Field.Pressure), 2e6, 1e-6);
        Near(Value(simulation, 1, Field.Temperature), 900, 1e-9);
        Near(Value(simulation, 1, Field.Mass), 2e6 * 2e-3 / (287 * 900), 1e-18);
        Require(Value(simulation, 10, Field.MassFlow) > 0);
        Require(Value(simulation, 0, Field.MassResidual) == 0 && Value(simulation, 0, Field.ReservoirEnthalpy) == 0);

        // A litre and a square millimetre are accepted and converted exactly.
        var scaled = CompiledModel.Compile(new()
        {
            Nodes = [new NodeDefinition(1, Domain.Gas, new(2, Unit.Liter), new(900, Unit.Kelvin), new(20, Unit.Bar))
                { Gas = new() { GasConstant = new(287, Unit.JoulePerKilogramKelvin), Gamma = 1.4 } }],
            Components = [new ComponentDefinition { Id = 10, Kind = ComponentKind.GasOrifice, NodeA = 1,
                Area = new(20, Unit.SquareMillimeter), DischargeCoefficient = Cd,
                ReservoirPressure = new(1, Unit.Bar), AmbientTemperature = new(300, Unit.Kelvin),
                InitialInput = new(1, Unit.Fraction) }]
        });
        Require(scaled.Fingerprint == CompiledModel.Compile(Vessel()).Fingerprint);

        ModelDiagnostic Reject(ModelDefinition definition)
        {
            Require(!CompiledModel.TryCompile(definition, out _, out var diagnostic), "expected a rejected model");
            return diagnostic!;
        }
        var gasNode = NodeDefinition.GasVolume(1, 2e-3, 2e6, 900);
        Require(Reject(Vessel() with { Nodes = [gasNode with { Gas = null }] }).Code == DiagnosticCode.Schema);
        Require(Reject(Vessel() with { Nodes = [NodeDefinition.Rotor(1, 1)] }).Code == DiagnosticCode.Connection);
        Require(Reject(Vessel() with { Nodes = [gasNode with { Position = new(0, Unit.Pascal) }] }).Code == DiagnosticCode.Range);
        Require(Reject(Vessel() with { Nodes = [gasNode with { Gas = new() { GasConstant = new(287, Unit.JoulePerKilogramKelvin), Gamma = 1 } }] }).Code == DiagnosticCode.Range);
        Require(Reject(Vessel(opening: 1.5)).Code == DiagnosticCode.Range);
        Require(Reject(Vessel() with { Components = [ComponentDefinition.GasReservoir(10, 1, Area, 0, 300)] }).Code == DiagnosticCode.Range);
        Require(Reject(Vessel() with { Components = [ComponentDefinition.GasReservoir(10, 1, 0, 1e5, 300)] }).Code == DiagnosticCode.Range);
        // Connected volumes must share one composition, and a heat link needs a thermal node.
        Require(Reject(new()
        {
            Nodes = [gasNode, NodeDefinition.GasVolume(2, 1e-3, 1e5, 300, 297, 1.4)],
            Components = [ComponentDefinition.GasOrifice(10, 1, 2, Area)]
        }).Code == DiagnosticCode.Connection);
        Require(Reject(new()
        {
            Nodes = [gasNode, NodeDefinition.GasVolume(2, 1e-3, 1e5, 300)],
            Components = [ComponentDefinition.GasHeatLink(10, 1, 2, 5)]
        }).Code == DiagnosticCode.Connection);
        // A gas volume alone is a legal model: gas networks do not need a mechanical node.
        Require(CompiledModel.TryCompile(new() { Nodes = [gasNode] }, out _, out _));
        // The existing asset format must reject new fields instead of silently dropping them.
        Throws<AssetFormatException>(() => PowerAsset.Create(Vessel(), "Gas boundary", new string('0', 64),
            100_000_000, 10_000_000, [], []));
        var mutable = Vessel();
        var frozen = CompiledModel.Compile(mutable);
        var original = Snapshot(frozen.CreateSimulation());
        mutable.Nodes[0] = NodeDefinition.GasVolume(1, 1, 1e5, 300);
        mutable.Components[0] = ComponentDefinition.GasReservoir(10, 1, Area, 5e5, 400);
        Require(Snapshot(frozen.CreateSimulation()) == original, "compiled gas topology must own its values");
    }

    private static void Blowdown()
    {
        var simulation = CompiledModel.Compile(Vessel()).CreateSimulation();
        Ok(simulation.Step(200_000_000));

        // Independent analytic solution for uniform choked discharge, as in GasChecks.Blowdown.
        const double volume = 2e-3, p0 = 2e6, t0 = 900, k = 0.2, duration = 0.2;
        double density0 = p0 / (287 * t0);
        double c = Area * Cd * IdealGas.Air.ChokedMassFluxCoefficient * 287 * Math.Sqrt(t0) * Math.Pow(density0, -k);
        double density = Math.Pow(Math.Pow(density0, -k) + k * c * duration / volume, -1 / k);
        double temperature = t0 * Math.Pow(density / density0, 0.4);
        Near(Value(simulation, 1, Field.Mass), density * volume, 2e-6 * density * volume);
        Near(Value(simulation, 1, Field.Temperature), temperature, 2e-6 * temperature);
        Near(Value(simulation, 1, Field.Pressure), density * 287 * temperature, 4e-6 * density * 287 * temperature);
        Require(Value(simulation, 1, Field.Pressure) / 1e5 > 1 / IdealGas.Air.CriticalPressureRatio, "must stay choked");

        // Refining the tick must reduce the error against the same closed form.
        double Error(ulong step)
        {
            var refined = CompiledModel.Compile(Vessel(step)).CreateSimulation();
            Ok(refined.Step(200_000_000));
            return Math.Abs(Value(refined, 1, Field.Mass) - density * volume);
        }
        double coarse = Error(1_000_000), fine = Error(250_000);
        Require(fine < coarse / 4, $"refinement did not converge: {coarse} then {fine}");

        // The ledger closes: everything that left the vessel is accounted for at the boundary.
        Near(Value(simulation, 0, Field.MassResidual), 0, 1e-18);
        Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-9);
        Require(Value(simulation, 0, Field.ReservoirEnthalpy) < 0, "discharging must export enthalpy");
    }

    private static void Network()
    {
        var model = CompiledModel.Compile(new()
        {
            StepNanoseconds = 50_000,
            Nodes = [NodeDefinition.GasVolume(1, 1.5e-3, 9e5, 800), NodeDefinition.GasVolume(2, 4e-4, 1.1e5, 290)],
            Components = [ComponentDefinition.GasOrifice(10, 1, 2, Area, Cd)]
        });
        var simulation = model.CreateSimulation();
        double mass0 = Value(simulation, 1, Field.Mass) + Value(simulation, 2, Field.Mass);
        for (int i = 0; i < 40; ++i) Ok(simulation.Step(50_000_000));

        // A closed network exchanges nothing across its boundary.
        Near(Value(simulation, 1, Field.Mass) + Value(simulation, 2, Field.Mass), mass0, 1e-14 * mass0);
        Require(Value(simulation, 0, Field.ReservoirEnthalpy) == 0);
        Near(Value(simulation, 0, Field.MassResidual), 0, 1e-14 * mass0);
        Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-9);
        Near(Value(simulation, 1, Field.Pressure), Value(simulation, 2, Field.Pressure), 1e-6 * Value(simulation, 1, Field.Pressure));
        Require(Math.Abs(Value(simulation, 10, Field.MassFlow)) < 1e-9, "flow must stop at equal pressure");
        Require(Value(simulation, 1, Field.Temperature) < 800 && Value(simulation, 2, Field.Temperature) > 290);
    }

    private static void WallHeat()
    {
        // A hot gas volume against a large cold wall, with no flow path anywhere.
        var model = CompiledModel.Compile(new()
        {
            StepNanoseconds = 200_000,
            Nodes = [NodeDefinition.GasVolume(1, 1e-3, 5e5, 900), NodeDefinition.Thermal(2, 400, 300)],
            Components = [ComponentDefinition.GasHeatLink(10, 1, 2, 3)]
        });
        Require(model.StateCount == 3 && model.Channels.Single(c => c.Quantity == "heat_flow_gas_to_wall").Unit == Unit.Watt);
        var simulation = model.CreateSimulation();
        double energy0 = Value(simulation, 1, Field.InternalEnergy), mass0 = Value(simulation, 1, Field.Mass);
        Require(Value(simulation, 10, Field.HeatFlow) > 0);
        for (int i = 0; i < 20; ++i) Ok(simulation.Step(100_000_000));

        double gas = Value(simulation, 1, Field.Temperature), wall = Value(simulation, 2, Field.Temperature);
        double capacity = mass0 * IdealGas.Air.IsochoricHeatCapacityJoulePerKilogramKelvin;
        double equilibrium = (capacity * 900 + 400 * 300) / (capacity + 400);
        double difference = 600 * Math.Exp(-3 * (1 / capacity + 1.0 / 400) * 2);
        Near(gas, equilibrium + 400 / (capacity + 400) * difference, 0.001);
        Near(wall, equilibrium - capacity / (capacity + 400) * difference, 0.00001);
        Require(gas < 900 && wall > 300, "heat must move from the gas to the wall");
        // Mass is untouched, and the joules the gas lost are exactly the joules the wall gained.
        Require(Value(simulation, 1, Field.Mass) == mass0);
        double lost = energy0 - Value(simulation, 1, Field.InternalEnergy), gained = 400 * (wall - 300);
        Near(lost, gained, 1e-6 * lost);
        Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-6);
        // The gas cooled at constant volume, so the pressure follows the temperature ratio.
        Near(Value(simulation, 1, Field.Pressure), 5e5 * gas / 900, 1e-6 * 5e5);
    }

    private static void Opening()
    {
        var closed = CompiledModel.Compile(Vessel(opening: 0)).CreateSimulation();
        var before = Snapshot(closed);
        Ok(closed.Step(500_000_000));
        Require(Value(closed, 1, Field.Pressure) == 2e6 && Value(closed, 10, Field.MassFlow) == 0);
        Require(Snapshot(closed).StateHash != before.StateHash, "time advances even when nothing flows");
        Require(Value(closed, 0, Field.MassResidual) == 0);

        var valve = CompiledModel.Compile(Vessel(channel: 100, opening: 0)).CreateSimulation();
        Require(valve.Model.Channels.Single(c => c.IsInput) is { Unit: Unit.Fraction, Quantity: "opening" });
        Ok(valve.Step(100_000_000));
        Require(Value(valve, 1, Field.Mass) == Value(closed, 1, Field.Mass));
        double sealedMass = Value(valve, 1, Field.Mass);
        Ok(valve.Step(100_000_000, [new(150_000_000, 100, 0.5)]));
        Require(Value(valve, 1, Field.Mass) < sealedMass, "opening the valve must discharge the vessel");

        // Openings outside [0,1] are rejected identically for direct and scheduled inputs.
        Require(valve.SubmitInputs([new(100, 1.0001)]) == SimulationStatus.InvalidInput);
        Require(valve.SubmitInputs([new(100, -0.0001)]) == SimulationStatus.InvalidInput);
        var guard = Snapshot(valve);
        Require(valve.Step(10_000_000, [new(guard.TimeNanoseconds + 5_000_000, 100, 2)]) == SimulationStatus.InvalidInput);
        Require(Snapshot(valve) == guard, "a rejected schedule must not advance time");
        Ok(valve.SubmitInputs([new(100, 1)]));
    }

    private static void Transactions()
    {
        var simulation = CompiledModel.Compile(Vessel()).CreateSimulation();
        Ok(simulation.Step(50_000_000));
        var mark = Snapshot(simulation);

        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Require(simulation.Step(100_000_000, cancelled.Token) == SimulationStatus.Cancelled);
        Require(Snapshot(simulation) == mark, "a cancelled batch must roll back completely");

        var branch = simulation.Fork();
        Require(Snapshot(branch) == mark);
        Ok(branch.Step(100_000_000));
        Require(Snapshot(simulation) == mark && Snapshot(branch) != mark, "branches must be independent");
        var stepwise = simulation.Fork();
        Ok(simulation.Step(100_000_000));
        for (int i = 0; i < 100; ++i) Ok(stepwise.Step(1_000_000));
        Require(Snapshot(simulation) == Snapshot(stepwise), "batching must not change the trajectory");

        // A tick far too coarse for the flow exceeds the sub-step budget and is refused, not approximated.
        var coarse = CompiledModel.Compile(Vessel(500_000_000) with
        { Components = [ComponentDefinition.GasReservoir(10, 1, 1000 * Area, 1e5, 300, Cd, 100)] }).CreateSimulation();
        var start = Snapshot(coarse);
        Require(coarse.Step(500_000_000) == SimulationStatus.NumericalFailure);
        Require(Snapshot(coarse) == start, "a refused tick must leave the state untouched");
        Ok(coarse.SubmitInputs([new(100, 0)]));
        var closedMark = Snapshot(coarse);
        Require(coarse.Step(1_000_000_000, [new(500_000_000, 100, 1)]) == SimulationStatus.NumericalFailure);
        Require(Snapshot(coarse) == closedMark, "failure after a successful tick must roll back the input schedule too");
        Ok(coarse.Step(500_000_000)); // Failed scratch and rate buffers must not poison recovery.
    }

    private static void Filling()
    {
        var simulation = CompiledModel.Compile(new()
        {
            StepNanoseconds = 50_000,
            Nodes = [NodeDefinition.GasVolume(1, 5e-4, 5e4, 300)],
            Components = [ComponentDefinition.GasReservoir(10, 1, 2e-6, 6e5, 320)]
        }).CreateSimulation();
        double mass0 = Value(simulation, 1, Field.Mass), energy0 = Value(simulation, 1, Field.InternalEnergy);
        Require(Value(simulation, 10, Field.MassFlow) < 0, "reservoir filling is negative A-to-B flow");
        Ok(simulation.Step(50_000_000));
        double added = Value(simulation, 1, Field.Mass) - mass0;
        Require(added > 0 && Value(simulation, 1, Field.Pressure) < 6e5);
        Near(Value(simulation, 1, Field.InternalEnergy) - energy0,
            added * IdealGas.Air.SpecificEnthalpy(320), 1e-10);
        Near(Value(simulation, 0, Field.MassResidual), 0, 1e-13 * Value(simulation, 1, Field.Mass));
        Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-10);
    }

    private static void ObservableBounds()
    {
        var simulation = CompiledModel.Compile(Vessel(opening: 0, channel: 100) with
        { Components = [ComponentDefinition.GasReservoir(10, 1, 1e300, 1e5, 300, 1, 100, 0)] }).CreateSimulation();
        var mark = Snapshot(simulation);
        Require(simulation.SubmitInputs([new(100, 1)]) == SimulationStatus.InvalidInput);
        Require(Snapshot(simulation) == mark);
        // The last event has no following tick; it still cannot publish a non-finite observable.
        Require(simulation.Step(100_000, [new(100_000, 100, 1)]) == SimulationStatus.NumericalFailure);
        Require(Snapshot(simulation) == mark);
        Ok(simulation.Step(100_000));
    }

    private static void Allocations()
    {
        var simulation = CompiledModel.Compile(new()
        {
            StepNanoseconds = 100_000,
            Nodes = [NodeDefinition.GasVolume(1, 2e-3, 4e5, 700), NodeDefinition.GasVolume(2, 2e-3, 1.5e5, 320),
                NodeDefinition.Thermal(3, 500, 350)],
            Components = [ComponentDefinition.GasOrifice(10, 1, 2, Area, Cd),
                ComponentDefinition.GasReservoir(11, 2, Area / 4, 1.2e5, 300, Cd),
                ComponentDefinition.GasHeatLink(12, 1, 3, 2)]
        }).CreateSimulation();
        var values = new Scalar[simulation.Model.OutputCount];
        for (int i = 0; i < 2000; ++i) { Ok(simulation.Step(100_000)); simulation.ReadSnapshot(values); }
        long before = GC.GetAllocatedBytesForCurrentThread(); bool ok = true;
        for (int i = 0; i < 2000; ++i) { ok &= simulation.Step(100_000) == SimulationStatus.Ok; simulation.ReadSnapshot(values); }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(ok && bytes == 0, $"Gas stepping allocated {bytes} bytes; success={ok}.");
    }
}
