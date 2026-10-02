// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class DctControllerChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("DCT control / confirmed seven gears / unloaded preselection and exclusive drive handoff", Shifts),
        ("DCT control / signed reverse / moving direction interlock / explicit neutral recovery", Direction),
        ("DCT control / physical synchronization timeout / unloaded fault and new-request recovery", Timeout),
        ("DCT control / loss of confirmed physical drive lock / observable unloaded fault", LockLoss),
        ("DCT control / integral request and channel ownership / sample-clock input semantics", Inputs),
        ("DCT control / complete sampled histories / cancellation late rollback forks allocations", Transactions),
        ("DCT control / long kinematic phase preservation / compensated positions and energy", LongRun),
        ("DCT control contracts / topology IDs periods units ownership and immutable route arrays", Contracts)
    ];
    internal static DualClutchControllerDefinition Definition() => new()
    {
        VehicleNode = 4, OddClutch = 400, EvenClutch = 401, Selectors = Enumerable.Range(300, 8).Select(i => (uint)i).ToArray(),
        SamplePeriodNanoseconds = 1_000_000, ReleaseNanoseconds = 5_000_000, EngageNanoseconds = 20_000_000,
        SynchronizeTimeoutNanoseconds = 500_000_000, SynchronizeTolerance = new(1e-6, Unit.RadianPerSecond), DirectionChangeSpeedLimit = new(.1, Unit.RadianPerSecond)
    };
    internal static ModelDefinition Model(int requested = 1)
    {
        var graph = DualClutchChecks.Assembly().CreateGraph(DualClutchChecks.Ports(), DualClutchChecks.Ids());
        return new() { StepNanoseconds = 100_000, Nodes = [NodeDefinition.Rotor(1, .2, 60), NodeDefinition.Rotor(4, 10), NodeDefinition.Thermal(3, 1000, 300), ..graph.Nodes],
            Components = [..graph.Components, ComponentDefinition.Torque(10, 1, 100, 20), ComponentDefinition.Torque(11, 4, 101, 0),
                ComponentDefinition.DctControl(800, 1, 700, requested, Definition())] };
    }
    private static void Ok(SimulationStatus status) { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Shifts()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation();
        foreach (int gear in new[] { 1, 2, 3, 4, 5, 6, 7, 6, 4, 2, 1 })
        {
            Ok(s.SubmitInputs([new(700, gear)]));
            for (int i = 0; i < 300; ++i)
            {
                Ok(s.Step(1_000_000));
                int odd = (int)Value(s, 800, Field.SelectedOddGear), even = (int)Value(s, 800, Field.SelectedEvenGear);
                Require(odd == 0 || odd > 0 && odd % 2 == 1); Require(even == 0 || even == -1 || even > 0 && even % 2 == 0);
                Near(Value(s, 0, Field.EnergyResidual), 0, 1e-6);
            }
            Near(Value(s, 800, Field.ActualGear), gear, 0); Near(Value(s, 800, Field.ControlFault), 0, 0);
            Near(Value(s, 1, Field.Speed), DualClutchChecks.Assembly().EffectiveReduction(gear) * Value(s, 4, Field.Speed), 1e-7);
        }
    }
    private static void Direction()
    {
        var reverse = CompiledModel.Compile(Model(-1)).CreateSimulation(); Ok(reverse.Step(300_000_000));
        Near(Value(reverse, 800, Field.ActualGear), -1, 0); Require(Value(reverse, 4, Field.Speed) < 0);
        Ok(reverse.SubmitInputs([new(700, 1)])); Ok(reverse.Step(1_000_000));
        Near(Value(reverse, 800, Field.ControlFault), (int)DctFault.DirectionChangeBlocked, 0); Near(Value(reverse, 800, Field.ActualGear), 0, 0);
        Ok(reverse.SubmitInputs([new(700, 0)])); Ok(reverse.Step(1_000_000)); Near(Value(reverse, 800, Field.ControlFault), 0, 0);
        var forward = CompiledModel.Compile(Model()).CreateSimulation(); Ok(forward.Step(300_000_000));
        Ok(forward.SubmitInputs([new(700, -1)])); Ok(forward.Step(1_000_000)); Near(Value(forward, 800, Field.ControlFault), (int)DctFault.DirectionChangeBlocked, 0);
    }
    private static void Timeout()
    {
        var definition = Model();
        int selector = Array.FindIndex(definition.Components, c => c.Id == 300);
        definition.Components[selector] = definition.Components[selector] with { Friction = new() { StaticCapacity = new(0, Unit.NewtonMeter), SlidingCapacity = new(0, Unit.NewtonMeter) } };
        var s = CompiledModel.Compile(definition).CreateSimulation(); Ok(s.Step(600_000_000));
        Near(Value(s, 800, Field.ControlFault), (int)DctFault.SynchronizationTimeout, 0); Near(Value(s, 800, Field.ActualGear), 0, 0);
        Ok(s.SubmitInputs([new(700, 2)])); Ok(s.Step(300_000_000)); Near(Value(s, 800, Field.ActualGear), 2, 0); Near(Value(s, 800, Field.ControlFault), 0, 0);
    }
    private static void Inputs()
    {
        var model = CompiledModel.Compile(Model(0)); var s = model.CreateSimulation(); var before = Snapshot(s);
        Require(model.ValidateInput(new(700, 1.5)) == SimulationStatus.InvalidInput && s.SubmitInputs([new(700, 1.5)]) == SimulationStatus.InvalidInput && Snapshot(s) == before);
        Require(s.SubmitInputs([new(500, 1)]) == SimulationStatus.ControlledInput && model.TryGetControlCommand(500, out var command) && command.Id == 700);
        Require(s.Step(1_000_000, [new(0, 700, 1.5)]) == SimulationStatus.InvalidInput && Snapshot(s) == before);
        Ok(s.SubmitInputs([new(700, 1)])); Near(Value(s, 800, Field.ActualGear), 0, 0); Ok(s.Step(100_000));
        Require(Value(s, 800, Field.ShiftPhase) != (int)DctShiftPhase.Driving);
    }
    private static void LockLoss()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation(); Ok(s.Step(300_000_000));
        Near(Value(s, 800, Field.ActualGear), 1, 0);
        Ok(s.SubmitInputs([new(100, 1000)])); Ok(s.Step(600_000_000));
        Near(Value(s, 800, Field.ControlFault), (int)DctFault.ConfirmedLockLost, 0); Near(Value(s, 800, Field.ActualGear), 0, 0);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-6);
    }
    private static void Transactions()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation(); Ok(s.Step(10_000_000)); var before = Snapshot(s);
        Require(s.Step(10_000_000, new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        var fork = s.Fork(); Ok(s.Step(10_000_000)); for (int i = 0; i < 10; ++i) Ok(fork.Step(1_000_000)); Require(Snapshot(s) == Snapshot(fork));
        before = Snapshot(s); Require(s.Step(10_000_000, [new(before.TimeNanoseconds + 5_000_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var values = new Scalar[s.Model.OutputCount]; for (int i = 0; i < 10; ++i) { Ok(s.Step(100_000)); s.ReadSnapshot(values); }
        long bytes = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 100; ++i) { Ok(s.Step(100_000)); s.ReadSnapshot(values); }
        Require(bytes == GC.GetAllocatedBytesForCurrentThread(), "Sampled DCT stepping/readback allocated.");
    }
    private static void LongRun()
    {
        var definition = Model() with { StepNanoseconds = 1_000_000 };
        var s = CompiledModel.Compile(definition).CreateSimulation(); Ok(s.Step(20_000_000_000));
        Near(Value(s, 800, Field.ActualGear), 1, 0); Near(Value(s, 800, Field.ControlFault), 0, 0);
        foreach (uint mesh in Enumerable.Range(200, 8).Select(i => (uint)i).Concat(new uint[] { 402, 403, 404, 405 }))
            Near(Value(s, mesh, Field.ConstraintError), 0, 1e-8);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-5);
    }
    private static void Contracts()
    {
        foreach (var bad in new[] { Definition() with { SamplePeriodNanoseconds = 99999 }, Definition() with { OddClutch = 401 },
            Definition() with { Selectors = [300, 300, 302, 303, 304, 305, 306, 307] }, Definition() with { SynchronizeTolerance = new(1, Unit.NewtonMeter) } })
        {
            var model = Model(); model.Components[^1] = model.Components[^1] with { DualClutchController = bad };
            Require(!CompiledModel.TryCompile(model, out _, out _));
        }
        var routes = Enumerable.Range(300, 8).Select(i => (uint)i).ToArray(); var definition = Model();
        definition.Components[^1] = definition.Components[^1] with { DualClutchController = Definition() with { Selectors = routes } };
        var compiled = CompiledModel.Compile(definition); routes[0] = 307;
        var a = compiled.CreateSimulation(); var b = CompiledModel.Compile(Model()).CreateSimulation(); Ok(a.Step(20_000_000)); Ok(b.Step(20_000_000)); Require(Snapshot(a) == Snapshot(b));
    }
}
