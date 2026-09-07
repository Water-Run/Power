using Power.Core;

namespace Power.Tests;

// Run the same physical contracts against both the .NET 10 and Unity-facing assemblies.
internal static class CoreChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("constant torque / batch replay", TorqueAndReplay),
        ("oscillator second-order convergence / long-run conservation", Oscillator),
        ("signed transmission momentum / heat", Transmission),
        ("RL branch analytic response", Electrical),
        ("thermal equilibrium / first-order convergence", Thermal),
        ("coupled motor / regenerative work", Regeneration),
        ("canonical order / SI normalization / fingerprints", Canonicalization),
        ("units / topology / capacities / diagnostics", Diagnostics),
        ("atomic input / failed batch rollback / recovery", Atomicity),
        ("snapshot capacity preserves caller buffer", SnapshotCapacity),
        ("compiled model ownership / independent instances", Ownership),
        ("parallel instances share immutable factors", ParallelInstances),
        ("agent diagnostic / fork / cancellation contracts", AgentContracts),
        ("zero managed allocations in steady-state core", Allocations)
    ];

    internal static void Require(bool condition, string message = "Contract failed")
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    internal static void Near(double actual, double expected, double tolerance) =>
        Require(double.IsFinite(actual) && Math.Abs(actual - expected) <= tolerance,
            $"Expected {expected:R} +/- {tolerance:R}, got {actual:R}");
    internal static T Throws<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T error) { return error; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }
    private static void Ok(SimulationStatus status) => Require(status == SimulationStatus.Ok, status.ToString());
    private static Simulation Make(NodeDefinition[] nodes, ComponentDefinition[] components, ulong step = 1_000_000) =>
        CompiledModel.Compile(new() { Nodes = nodes, Components = components, StepNanoseconds = step }).CreateSimulation();
    private static double Value(Simulation s, uint id, Field field)
    {
        var buffer = new Scalar[s.Model.OutputCount];
        s.ReadSnapshot(buffer);
        return buffer.Single(v => v.Channel == Channels.Output(id, field)).Value;
    }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);

    private static void TorqueAndReplay()
    {
        var a = Make([NodeDefinition.Rotor(1, 2)], [ComponentDefinition.Torque(10, 1, 100, 4)]);
        var b = a.Model.CreateSimulation();
        Ok(a.Step(2_000_000_000));
        for (int i = 0; i < 2000; ++i) Ok(b.Step(1_000_000));
        Require(Snapshot(a) == Snapshot(b));
        Near(Value(a, 1, Field.Speed), 4, 1e-11);
        Near(Value(a, 1, Field.Angle), 4, 1e-11);
        Near(Value(a, 0, Field.SourceWork), 16, 1e-10);
        Near(Value(a, 0, Field.EnergyResidual), 0, 1e-10);
    }

    private static void Oscillator()
    {
        double[] errors = new double[3];
        for (int i = 0; i < errors.Length; ++i)
        {
            var s = Make([NodeDefinition.Rotor(1, 2, angle: 0.5)], [ComponentDefinition.Shaft(10, 1, 0, 8, 0)], 40_000_000UL >> i);
            Ok(s.Step(1_000_000_000));
            errors[i] = Math.Abs(Value(s, 1, Field.Angle) - 0.5 * Math.Cos(2));
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-12);
            Ok(s.Step(1_000_000_000_000));
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-9);
        }
        Near(errors[0] / errors[1], 4, 0.1); Near(errors[1] / errors[2], 4, 0.1);
    }

    private static void Transmission()
    {
        foreach (double ratio in new[] { 2.0, -2.0 })
        {
            var s = Make([NodeDefinition.Rotor(1, 1, 2, 0.2), NodeDefinition.Rotor(2, 3, -1, -0.1), NodeDefinition.Thermal(3, 10, 300)],
                [ComponentDefinition.Shaft(10, 1, 2, 40, 0.8, ratio, 3, 0.07)]);
            Ok(s.Step(10_000_000_000));
            Near(ratio * Value(s, 1, Field.Speed) + 3 * Value(s, 2, Field.Speed), ratio * 2 - 3, 1e-10);
            Require(Value(s, 3, Field.Temperature) > 300);
            Near(Value(s, 0, Field.HeatRejected), 0, 0);
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-7);
        }
    }

    private static void Electrical()
    {
        var s = Make([NodeDefinition.Rotor(1, 1)], [ComponentDefinition.Motor(10, 1, 0, 100, 2, 0.5, 0, 12)]);
        Ok(s.Step(500_000_000));
        Near(Value(s, 10, Field.Current), 6 * (1 - Math.Exp(-2)), 3e-6);
        Near(Value(s, 1, Field.Speed), 0, 0);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-10);
    }

    private static void Thermal()
    {
        var s = Make([NodeDefinition.Thermal(1, 10, 400), NodeDefinition.Thermal(2, 30, 300)],
            [ComponentDefinition.ThermalLink(10, 1, 2, 100)], 1_000_000_000);
        Ok(s.Step(20_000_000_000));
        Near(Value(s, 1, Field.Temperature), 325, 1e-9); Near(Value(s, 2, Field.Temperature), 325, 1e-9);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-8);
        double[] errors = new double[2];
        for (int i = 0; i < 2; ++i)
        {
            s = Make([NodeDefinition.Thermal(1, 10, 400)], [ComponentDefinition.ThermalLink(10, 1, 0, 10, 300)], 10_000_000UL >> i);
            Ok(s.Step(1_000_000_000));
            errors[i] = Math.Abs(Value(s, 1, Field.Temperature) - (300 + 100 * Math.Exp(-1)));
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-8);
        }
        Near(errors[0] / errors[1], 2, 0.02);
    }

    private static void Regeneration()
    {
        var s = CompiledModel.Compile(SampleModels.Electrothermal(1_000_000)).CreateSimulation();
        Ok(s.Step(20_000_000_000));
        Require(Value(s, 1, Field.Speed) > 20 && Value(s, 2, Field.Speed) > 7);
        Require(Value(s, 3, Field.Temperature) > 300 && Value(s, 4, Field.Temperature) > 300);
        double work = Value(s, 0, Field.SourceWork);
        Ok(s.SubmitInputs([new(SampleModels.VoltageChannel, 4)]));
        Ok(s.Step(500_000_000));
        Require(Value(s, 10, Field.Current) < 0 && Value(s, 0, Field.SourceWork) < work);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-6);
    }

    private static void Canonicalization()
    {
        var definition = SampleModels.Electrothermal();
        var a = CompiledModel.Compile(definition);
        var reordered = definition with { Nodes = definition.Nodes.Reverse().ToArray(), Components = definition.Components.Reverse().ToArray() };
        reordered.Nodes[^1] = reordered.Nodes[^1] with { Initial = new(-0.0, Unit.Rpm), Position = new(-0.0, Unit.Degree) };
        var b = CompiledModel.Compile(reordered);
        Require(a.Fingerprint == b.Fingerprint);
        var sa = a.CreateSimulation(); var sb = b.CreateSimulation();
        Ok(sa.Step(1_000_000_000)); Ok(sb.Step(1_000_000_000));
        Require(Snapshot(sa) == Snapshot(sb));
        Require(a.Fingerprint != CompiledModel.Compile(definition with { StepNanoseconds = 50_000 }).Fingerprint);
        var rpm = NodeDefinition.Rotor(1, 1) with { Initial = new(60, Unit.Rpm), Position = new(180, Unit.Degree) };
        var converted = Make([rpm], []);
        Near(Value(converted, 1, Field.Speed), 2 * Math.PI, 1e-14);
        Near(Value(converted, 1, Field.Angle), Math.PI, 1e-14);
    }

    private static void Diagnostics()
    {
        var rotor = NodeDefinition.Rotor(1, 2);
        var error = Throws<ModelCompileException>(() => Make([rotor with { Storage = new(2, Unit.NewtonMeter) }], []));
        Require(error.Code == DiagnosticCode.Unit && error.ObjectId == 1 && error.Field == "storage");
        foreach (double bad in new[] { 0.0, -1.0, double.NaN, double.PositiveInfinity })
            Throws<ModelCompileException>(() => Make([rotor with { Storage = new(bad, Unit.KilogramMeterSquared) }], []));
        Throws<ModelCompileException>(() => Make([rotor, NodeDefinition.Thermal(2, 1, 300)], [ComponentDefinition.Shaft(10, 1, 2, 1, 1)]));
        Throws<ModelCompileException>(() => Make([rotor], [ComponentDefinition.Shaft(10, 1, 999, 1, 1)]));
        Throws<ModelCompileException>(() => Make([rotor], [ComponentDefinition.Shaft(10, 1, 1, 1, 1)]));
        Throws<ModelCompileException>(() => Make([rotor], [ComponentDefinition.Torque(1, 1, 100, 1)]));
        Throws<ModelCompileException>(() => Make([rotor], [ComponentDefinition.Torque(10, 1, 100, 1), ComponentDefinition.Torque(11, 1, 100, 1)]));
        Throws<ModelCompileException>(() => Make([rotor], [ComponentDefinition.Motor(10, 1, 0, 100, 1, 0, 1, 1)]));
        Throws<ModelCompileException>(() => Make([rotor with { Initial = new(1e308, Unit.RadianPerSecond) }], []));
        Throws<ModelCompileException>(() => CompiledModel.Compile(new() { SchemaVersion = 2, Nodes = [rotor] }));
        Throws<ModelCompileException>(() => Make([null!], []));
        var full = Enumerable.Range(1, 32).Select(i => NodeDefinition.Rotor((uint)i, 1)).ToArray();
        Require(Make(full, []).Model.StateCount == 64);
        Throws<ModelCompileException>(() => Make(full, [ComponentDefinition.Motor(100, 1, 0, 100, 1, 1, 1, 1)]));
        Throws<ModelCompileException>(() => Make([.. full, NodeDefinition.Rotor(33, 1)], []));
    }

    private static void Atomicity()
    {
        var s = Make([NodeDefinition.Rotor(1, 2)], [ComponentDefinition.Torque(10, 1, 100, 4), ComponentDefinition.Torque(11, 1, 101, 0)]);
        var initial = Snapshot(s);
        Require(s.SubmitInputs([new(100, 100), new(99, 0)]) == SimulationStatus.UnknownChannel);
        Require(Snapshot(s) == initial);
        Require(s.SubmitInputs([new(100, 100), new(100, 0)]) == SimulationStatus.InvalidInput);
        Require(Snapshot(s) == initial);
        Require(s.SubmitInputs([new(100, 100), new(101, double.NaN)]) == SimulationStatus.InvalidInput);
        Require(Snapshot(s) == initial);
        Ok(s.SubmitInputs([new(100, double.MaxValue)]));
        var submitted = Snapshot(s);
        Require(s.Step(2_000_000) == SimulationStatus.NumericalFailure);
        Require(Snapshot(s) == submitted && submitted.TimeNanoseconds == 0);
        foreach (ulong delta in new[] { 0UL, 3UL, 1_000_001_000_000UL, ulong.MaxValue })
            Require(s.Step(delta) == SimulationStatus.InvalidTimeStep);
        Require(Snapshot(s) == submitted);
        Ok(s.SubmitInputs([new(100, 4)])); Ok(s.Step(1_000_000));
        Near(Value(s, 1, Field.Speed), 0.002, 1e-15);

        // The first tick is finite, but a later tick overflows. The entire batch must roll back.
        s = Make([NodeDefinition.Rotor(1, 1)], [ComponentDefinition.Torque(10, 1, 100, 1e154)], 1_000_000_000);
        initial = Snapshot(s);
        Require(s.Step(3_000_000_000) == SimulationStatus.NumericalFailure);
        Require(Snapshot(s) == initial);
        Ok(s.Step(1_000_000_000));
    }

    private static void SnapshotCapacity()
    {
        var s = CompiledModel.Compile(SampleModels.Electrothermal()).CreateSimulation();
        Scalar[] canary = [new(123, 456)];
        Throws<ArgumentException>(() => s.ReadSnapshot(canary));
        Require(canary[0] == new Scalar(123, 456));
        var full = new Scalar[s.Model.OutputCount + 1]; full[^1] = canary[0];
        Require(s.ReadSnapshot(full).ValueCount == s.Model.OutputCount && full[^1] == canary[0]);
    }

    private static void Ownership()
    {
        var definition = SampleModels.Electrothermal();
        var model = CompiledModel.Compile(definition);
        var a = model.CreateSimulation(); var b = model.CreateSimulation(); var initial = Snapshot(b);
        Array.Fill(definition.Nodes, null!); Array.Fill(definition.Components, null!);
        Ok(a.SubmitInputs([new(100, 48)])); Ok(a.Step(1_000_000_000));
        Require(Snapshot(b) == initial);
        Require(Snapshot(model.CreateSimulation()) == initial);
    }

    private static void ParallelInstances()
    {
        var model = CompiledModel.Compile(SampleModels.Electrothermal());
        var hashes = new ulong[8];
        Parallel.For(0, hashes.Length, i =>
        {
            var s = model.CreateSimulation(); Ok(s.Step(200_000_000)); hashes[i] = Snapshot(s).StateHash;
        });
        Require(hashes.All(h => h == hashes[0]));
    }

    private static void Allocations()
    {
        var s = CompiledModel.Compile(SampleModels.Electrothermal()).CreateSimulation();
        var values = new Scalar[s.Model.OutputCount]; Scalar[] input = [new(100, 24)];
        for (int i = 0; i < 2000; ++i) { s.SubmitInputs(input); s.Step(100_000); s.ReadSnapshot(values); }
        long start = GC.GetAllocatedBytesForCurrentThread();
        bool success = true;
        for (int i = 0; i < 2000; ++i)
        {
            success &= s.SubmitInputs(input) == SimulationStatus.Ok;
            success &= s.Step(100_000) == SimulationStatus.Ok;
            s.ReadSnapshot(values);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        Require(success && allocated == 0, $"Steady-state allocated {allocated} bytes");
    }

    private static void AgentContracts()
    {
        Require(!CompiledModel.TryCompile(new() { Nodes = [NodeDefinition.Rotor(1, -1)] }, out var invalid, out var diagnostic));
        Require(invalid is null && diagnostic is { Code: DiagnosticCode.Range, ObjectId: 1, Field: "storage" });
        var a = CompiledModel.Compile(SampleModels.Electrothermal()).CreateSimulation();
        Ok(a.Step(1_000_000_000));
        var before = Snapshot(a);
        var b = a.Fork();
        Require(Snapshot(b) == before);
        Ok(b.SubmitInputs([new(100, 4)])); Ok(b.Step(100_000_000));
        Require(Snapshot(a) == before && Snapshot(b) != before);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Require(a.Step(1_000_000_000, cancelled.Token) == SimulationStatus.Cancelled);
        Require(Snapshot(a) == before);
        Ok(a.Step(100_000_000));
        Require(Snapshot(a).TimeNanoseconds == 1_100_000_000);
    }
}
