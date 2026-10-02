// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class ClosurePredictionChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("closure forecast / exact independent zero-voltage replay / no committed mutation", Replay),
        ("closure compensation / actual tail retained / cycle cutoff memory / improved dose", Tracking),
        ("closure prediction horizon / late rebound retained / converged finite-horizon dose", Horizon),
        ("closure forecast / cancelled read / late failed batch / independent forks / zero allocations", Transactions),
        ("closure forecast contracts / aligned horizon / finite tick budget / immutable model", Contracts)
    ];
    internal static ModelDefinition Model(ulong horizon = 20_000_000)
    {
        var model = SolenoidChecks.NeedleModel(); var driver = model.Components[^1];
        model.Components[^1] = driver with { NeedleDriver = driver.NeedleDriver! with { ClosurePredictionNanoseconds = horizon } };
        return model;
    }
    private static void Ok(SimulationStatus status) { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Replay()
    {
        var simulation = CompiledModel.Compile(Model()).CreateSimulation(); Ok(simulation.Step(1_000_000));
        var values = new Scalar[simulation.Model.OutputCount]; var before = simulation.ReadSnapshot(values); var fork = simulation.Fork();
        Ok(simulation.PredictNeedleClosure(24, out var estimate));
        var after = new Scalar[values.Length]; Require(simulation.ReadSnapshot(after) == before && after.SequenceEqual(values));
        Require(estimate.PhysicalTicks == 2000 && estimate.HorizonNanoseconds == 20_000_000 && estimate.AdditionalFuelKilograms > 0);
        var manualDefinition = SolenoidChecks.NeedleModel();
        manualDefinition = manualDefinition with { Components = manualDefinition.Components.Take(manualDefinition.Components.Length - 1).ToArray() };
        manualDefinition.Components[^1] = manualDefinition.Components[^1] with { InitialInput = new(6.8, Unit.Volt) };
        var manual = CompiledModel.Compile(manualDefinition).CreateSimulation(); Ok(manual.Step(1_000_000));
        Near(Value(simulation, 12, Field.TotalFuelDelivered), Value(manual, 12, Field.TotalFuelDelivered), 1e-15);
        double initial = Value(manual, 12, Field.TotalFuelDelivered); Ok(manual.SubmitInputs([new(107, 0)])); Ok(manual.Step(20_000_000));
        Near(estimate.AdditionalFuelKilograms, Value(manual, 12, Field.TotalFuelDelivered) - initial, 1e-15);
        Ok(simulation.Step(1_000_000)); Ok(fork.Step(1_000_000)); Require(Snapshot(simulation) == Snapshot(fork));
        Console.WriteLine($"Closure replay: {estimate.AdditionalFuelKilograms:R} kg predicted over {estimate.PhysicalTicks} ticks.");
    }
    private static void Tracking()
    {
        var predicted = CompiledModel.Compile(Model()).CreateSimulation(); var baseline = CompiledModel.Compile(SolenoidChecks.NeedleModel()).CreateSimulation();
        Ok(predicted.Step(40_000_000)); Ok(baseline.Step(40_000_000));
        double delivered = Value(predicted, 12, Field.TotalFuelDelivered), baselineDelivery = Value(baseline, 12, Field.TotalFuelDelivered);
        Require(Math.Abs(delivered - 8e-6) < Math.Abs(baselineDelivery - 8e-6) / 3, $"Compensated dose {delivered:R}, baseline {baselineDelivery:R}");
        Require(Math.Abs(delivered - 8e-6) < .05e-6);
        Near(Value(predicted, 24, Field.DriverState), 1, 0); Near(Value(predicted, 24, Field.PredictionTicks), 2000, 0);
        Near(Value(predicted, 0, Field.EnergyResidual), 0, 2e-7);
        double total = delivered; Ok(predicted.Step(40_000_000)); Near(Value(predicted, 12, Field.TotalFuelDelivered), total, 1e-15);
        Console.WriteLine($"Closure compensation: requested 8e-6 kg, delivered {delivered:R}; baseline {baselineDelivery:R} kg.");
    }
    private static void Transactions()
    {
        var model = CompiledModel.Compile(Model()); var s = model.CreateSimulation(); Ok(s.Step(500_000)); var before = Snapshot(s);
        Require(s.PredictNeedleClosure(24, out _, new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.PredictNeedleClosure(99, out _) == SimulationStatus.InvalidInput && Snapshot(s) == before);
        var fork = s.Fork(); Ok(s.Step(1_000_000)); for (int i = 0; i < 10; ++i) Ok(fork.Step(100_000)); Require(Snapshot(s) == Snapshot(fork));
        before = Snapshot(s);
        Require(s.Step(1_000_000, [new(before.TimeNanoseconds + 500_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var values = new Scalar[s.Model.OutputCount];
        for (int i = 0; i < 5; ++i) { Ok(s.PredictNeedleClosure(24, out _)); s.ReadSnapshot(values); }
        long allocation = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 5; ++i) { Ok(s.PredictNeedleClosure(24, out _)); s.ReadSnapshot(values); }
        Require(allocation == GC.GetAllocatedBytesForCurrentThread(), "Closure replay allocated managed memory.");
        for (int i = 0; i < 5; ++i) { Ok(s.Step(100_000)); s.ReadSnapshot(values); }
        allocation = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 5; ++i) { Ok(s.Step(100_000)); s.ReadSnapshot(values); }
        Require(allocation == GC.GetAllocatedBytesForCurrentThread(), "Active predictive control allocated managed memory.");
        Require(Value(s, 24, Field.PredictionTicks) == 2000 && Value(s, 24, Field.DriverState) == 0);
        var hybrid = Model(); hybrid = hybrid with { Nodes = [..hybrid.Nodes, NodeDefinition.Rotor(8, .1)],
            Components = [..hybrid.Components, ComponentDefinition.Clutch(30, 1, 8, 220, 200, heat: 3)] };
        var a = CompiledModel.Compile(hybrid).CreateSimulation(); var b = a.Fork();
        Ok(a.Step(5_000_000)); for (int i = 0; i < 50; ++i) Ok(b.Step(100_000)); Require(Snapshot(a) == Snapshot(b));
        Near(Value(a, 0, Field.EnergyResidual), 0, 2e-7);
    }
    private static void Horizon()
    {
        var doses = new List<double>();
        foreach (ulong horizon in new ulong[] { 8_000_000, 12_000_000, 20_000_000, 30_000_000 })
        {
            var s = CompiledModel.Compile(Model(horizon)).CreateSimulation(); Ok(s.Step(40_000_000));
            doses.Add(Value(s, 12, Field.TotalFuelDelivered));
        }
        Require(Math.Abs(doses[2] - doses[3]) < 1e-12 && Math.Abs(doses[2] - 8e-6) < Math.Abs(doses[0] - 8e-6) / 10);
        Console.WriteLine($"Closure horizon study (8/12/20/30 ms): actual doses {string.Join(", ", doses.Select(d => d.ToString("R")))} kg.");
    }
    private static void Contracts()
    {
        foreach (ulong horizon in new ulong[] { 99_999, 40_970_000, ulong.MaxValue })
            Require(!CompiledModel.TryCompile(Model(horizon), out _, out var diagnostic) && diagnostic?.Code == DiagnosticCode.Range);
        var disabled = CompiledModel.Compile(Model(0)); Require(!disabled.HasClosurePrediction);
        var model = Model(); var compiled = CompiledModel.Compile(model); model.Components[^1] = model.Components[^1] with
            { NeedleDriver = model.Components[^1].NeedleDriver! with { ClosurePredictionNanoseconds = 21_000_000 } };
        Require(compiled.Fingerprint != CompiledModel.Compile(model).Fingerprint && compiled.HasClosurePrediction);
    }
}
