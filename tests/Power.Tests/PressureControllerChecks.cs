// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class PressureControllerChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("pressure PI / exact samples / conditional integration / saturation recovery", Law),
        ("pressure control / integer sampling / held voltage / endpoint events / ownership", Clock),
        ("pressure control / independent sampled RK4 plant / physical tick refinement", Reference),
        ("pressure control / whole-batch rollback / cancellation / clocks and forks", Transactions),
        ("pressure controller contracts / units / periods / ownership / capacity", Contracts),
        ("pressure controller / zero allocations across sample ticks", Allocations)
    ];
    internal static PressureControllerDefinition Control(ulong period = 10_000_000) => new()
    {
        TargetChannel = 100, SamplePeriodNanoseconds = period,
        ProportionalGain = new(2e-5, Unit.VoltPerPascal), IntegralGain = new(1.5e-5, Unit.VoltPerPascalSecond),
        MinimumVoltage = new(0, Unit.Volt), MaximumVoltage = new(12, Unit.Volt), InitialIntegralVoltage = new(0, Unit.Volt)
    };
    internal static ModelDefinition Plant(ulong step = 1_000_000) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, .02), NodeDefinition.Hydraulic(2, 2e-12), NodeDefinition.Thermal(3, 100, 300)],
        Components = [ComponentDefinition.Motor(10, 1, 3, 100, 1, .01, .1, 0),
            ..new HydraulicPumpAssembly(1e-7, 1e-12, .02).CreateComponents(11, 12, 13, 1, 2, heat: 3),
            ComponentDefinition.PressureLoop(20, 2, 105, 3e5, Control())]
    };
    private static void Ok(SimulationStatus status)
    { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation simulation) => simulation.ReadSnapshot(new Scalar[simulation.Model.OutputCount]);
    private static void Law()
    {
        var law = new PressurePiController(2, 3, 0, 5);
        Require(law.TrySample(10, 4, 1, 1, out var result)); Require(result == new PressurePiResult(6, 1, 5));
        Require(law.TrySample(9, 10, 10, 1, out result)); Require(result == new PressurePiResult(-1, 7, 5));
        Require(law.TrySample(9, 10, result.IntegralVoltage, 1, out result)); Require(result == new PressurePiResult(-1, 4, 2));
        Require(law.TrySample(4, 3, -10, 1, out result)); Require(result == new PressurePiResult(1, -7, 0));
        Require(law.TrySample(1, 2, 0, 1, out result)); Require(result == new PressurePiResult(-1, 0, 0));
        Require(law.TrySample(10, 4, 1, 0, out result)); Require(result.IntegralVoltage == 1 && result.CommandVoltage == 5);
        Require(!law.TrySample(-1, 0, 0, .1, out result) && result == default);
        Require(!law.TrySample(1, 0, double.NaN, .1, out result) && result == default);
        Require(!law.TrySample(1, 0, 0, -.1, out result) && result == default);
        Require(!law.TrySample(double.MaxValue, 0, 0, 1, out result) && result == default);
        Throws<ArgumentException>(() => new PressurePiController(-1, 1, 0, 12));
        Throws<ArgumentException>(() => new PressurePiController(1, double.NaN, 0, 12));
        Throws<ArgumentException>(() => new PressurePiController(1, 1, 12, 12));
    }
    private static void Clock()
    {
        var definition = Plant(100_000) with
        {
            Nodes = [NodeDefinition.Rotor(1, .02), NodeDefinition.Hydraulic(2, 2e-12, 20), NodeDefinition.Thermal(3, 100, 300)],
            Components = [Plant().Components[0], ComponentDefinition.PressureLoop(20, 2, 105, 30,
                Control(1_000_000) with { ProportionalGain = new(.1, Unit.VoltPerPascal), IntegralGain = new(.2, Unit.VoltPerPascalSecond) })]
        };
        var model = CompiledModel.Compile(definition); var simulation = model.CreateSimulation();
        Require(model.HasPressureControllers && model.Fidelity == "sampled_pressure_control");
        Require(!model.Channels.Any(c => c.IsInput && c.Id == 100));
        Require(model.ValidateInput(new(100, 2)) == SimulationStatus.ControlledInput);
        var before = Snapshot(simulation); Require(simulation.SubmitInputs([new(100, 2)]) == SimulationStatus.ControlledInput && Snapshot(simulation) == before);
        Ok(simulation.Step(500_000)); Near(Value(simulation, 20, Field.CommandVoltage), 1, 0); Near(Value(simulation, 20, Field.IntegralVoltage), 0, 0);
        Ok(simulation.Step(500_000, [new(1_000_000, 105, 40)]));
        Near(Value(simulation, 20, Field.CommandVoltage), 1, 0); Near(Value(simulation, 20, Field.PressureError), 10, 0);
        Ok(simulation.Step(100_000)); Near(Value(simulation, 20, Field.CommandVoltage), 2.004, 1e-15);
        Near(Value(simulation, 20, Field.SampledPressure), 20, 0); Near(Value(simulation, 20, Field.IntegralVoltage), .004, 1e-15);
        var parent = Snapshot(simulation); var fork = simulation.Fork(); Ok(fork.SubmitInputs([new(105, 10)])); Ok(fork.Step(1_000_000));
        Require(Snapshot(simulation) == parent && Value(fork, 20, Field.CommandVoltage) == 0);
    }
    private static void Reference()
    {
        // Independent RK4 integration with an explicitly specified sample-and-hold controller.
        double[] x = [0, 0, 0], a = new double[3], b = new double[3], c = new double[3], d = new double[3], trial = new double[3];
        double integral = 0, voltage = 0; const double h = .00001;
        void Derivative(double[] state, double[] target)
        {
            target[0] = (.1 * state[2] - 1e-7 * state[1] - .02 * state[0]) / .02;
            target[1] = (1e-7 * state[0] - 1e-12 * state[1]) / 2e-12;
            target[2] = (voltage - state[2] - .1 * state[0]) / .01;
        }
        for (int tick = 0; tick < 50_000; ++tick)
        {
            if (tick % 1000 == 0)
            {
                double error = 3e5 - x[1], change = tick == 0 ? 0 : 1.5e-5 * .01 * error;
                double proposed = integral + change, raw = 2e-5 * error + proposed;
                if (!(raw > 12 && change > 0 || raw < 0 && change < 0)) integral = proposed;
                voltage = Math.Max(0, Math.Min(12, 2e-5 * error + integral));
            }
            Derivative(x, a);
            for (int i = 0; i < 3; ++i) trial[i] = x[i] + h * a[i] / 2;
            Derivative(trial, b);
            for (int i = 0; i < 3; ++i) trial[i] = x[i] + h * b[i] / 2;
            Derivative(trial, c);
            for (int i = 0; i < 3; ++i) trial[i] = x[i] + h * c[i];
            Derivative(trial, d);
            for (int i = 0; i < 3; ++i) x[i] += h * (a[i] + 2 * b[i] + 2 * c[i] + d[i]) / 6;
        }
        double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 1_000_000, 500_000, 250_000 })
        {
            var simulation = CompiledModel.Compile(Plant(step)).CreateSimulation(); Ok(simulation.Step(500_000_000));
            double error = Math.Abs(Value(simulation, 1, Field.Speed) - x[0]) + 1e-5 * Math.Abs(Value(simulation, 2, Field.Pressure) - x[1])
                + Math.Abs(Value(simulation, 10, Field.Current) - x[2]) + Math.Abs(Value(simulation, 20, Field.IntegralVoltage) - integral);
            Require(previous / error > 3.8, $"Sampled plant physical refinement {previous:R}/{error:R}"); previous = error;
            Near(Value(simulation, 20, Field.CommandVoltage), voltage, .001); Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8);
        }
    }
    private static void Transactions()
    {
        var definition = Plant(100_000);
        definition = definition with { Components = [..definition.Components[..^1], definition.Components[^1] with
        { PressureController = Control() with { ProportionalGain = new(1, Unit.VoltPerPascal), IntegralGain = new(100, Unit.VoltPerPascalSecond) } }] };
        var model = CompiledModel.Compile(definition); var simulation = model.CreateSimulation(); Ok(simulation.Step(20_000_000)); var before = Snapshot(simulation);
        Require(simulation.Step(100_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(simulation) == before);
        Require(simulation.Step(100_000_000, [new(50_000_000, 105, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(simulation) == before);
        Require(simulation.Step(1_000_000, [new(20_000_000, 100, 0)]) == SimulationStatus.ControlledInput && Snapshot(simulation) == before);
        var retry = simulation.Fork(); Ok(simulation.Step(100_000_000)); for (int i = 0; i < 100; ++i) Ok(retry.Step(1_000_000)); Require(Snapshot(simulation) == Snapshot(retry));
        Ok(retry.SubmitInputs([new(105, 1e5)])); Ok(retry.Step(20_000_000)); Require(Snapshot(retry) != Snapshot(simulation));
        Require(simulation.SubmitInputs([new(105, -1)]) == SimulationStatus.InvalidInput);
    }
    private static void Contracts()
    {
        var definition = Plant(); var controller = definition.Components[^1];
        foreach (var control in new[] { Control(0), Control(1_000_001), Control(1_000_000_001),
            Control() with { TargetChannel = 105 }, Control() with { TargetChannel = 999 },
            Control() with { ProportionalGain = new(1, Unit.Volt) }, Control() with { IntegralGain = new(-1, Unit.VoltPerPascalSecond) },
            Control() with { MaximumVoltage = new(0, Unit.Volt) } })
            Require(!CompiledModel.TryCompile(definition with { Components = [..definition.Components[..^1], controller with { PressureController = control }] }, out _, out _));
        Require(!CompiledModel.TryCompile(definition with { Components = [..definition.Components, controller with { Id = 21, InputChannel = 106 }] }, out _, out var duplicate) && duplicate!.Code == DiagnosticCode.Channel);
        Require(!CompiledModel.TryCompile(definition with { Components = [..definition.Components[..^1], controller with { NodeA = 1 }] }, out _, out _));
        var compiled = CompiledModel.Compile(definition); definition.Components[^1] = controller with { PressureController = Control() with { SamplePeriodNanoseconds = 20_000_000 } };
        Require(compiled.Fingerprint != CompiledModel.Compile(definition).Fingerprint);
        var simulation = compiled.CreateSimulation(); Ok(simulation.Step(10_000_000)); Require(Value(simulation, 20, Field.CommandVoltage) > 0);
        var crowded = Plant(); var extra = new List<ComponentDefinition>(crowded.Components);
        for (uint i = 0; i < 26; ++i)
        {
            extra.Add(ComponentDefinition.Motor(60 + 2 * i, 1, 3, 300 + i, 1, .01, .1, 0));
            extra.Add(ComponentDefinition.PressureLoop(61 + 2 * i, 2, 400 + i, 3e5, Control() with { TargetChannel = 300 + i }));
        }
        crowded = crowded with { Components = extra.ToArray() };
        Require(!CompiledModel.TryCompile(crowded, out _, out var capacity) && capacity!.Code == DiagnosticCode.Capacity);
    }
    private static void Allocations()
    {
        var simulation = CompiledModel.Compile(Plant(100_000)).CreateSimulation(); var buffer = new Scalar[simulation.Model.OutputCount];
        for (int i = 0; i < 5; ++i) { Ok(simulation.Step(10_000_000)); simulation.ReadSnapshot(buffer); }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; ++i) { Ok(simulation.Step(1_000_000)); simulation.ReadSnapshot(buffer); }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Sampled pressure control allocated managed memory.");
    }
}
