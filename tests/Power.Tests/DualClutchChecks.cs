// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class DualClutchChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("DCT assembly / seven forward and reverse / three outputs / stable ordinary graph", Topology),
        ("DCT selected paths / independent reflected-inertia reference / signed reverse work", Ratios),
        ("DCT correlated six/seven locks / bounded linear solve / independent acceleration", CorrelatedLocks),
        ("DCT inactive preselection / independent synchronization impulse and heat", Preselection),
        ("DCT transactions / complete gear clutch heat rollback / batching forks allocations", Transactions),
        ("DCT definitions / units IDs selection channels / immutable parameter ownership", Contracts)
    ];
    internal static double[] RatiosData() => [3.8, 2.3, 1.65, 1.26, 1.02, .86, .72];
    internal static DualClutchParameters Parameters() => new()
    {
        OddShaftInertia = new(.015, Unit.KilogramMeterSquared), EvenShaftInertia = new(.012, Unit.KilogramMeterSquared),
        OutputAInertia = new(.02, Unit.KilogramMeterSquared), OutputBInertia = new(.018, Unit.KilogramMeterSquared),
        ReverseOutputInertia = new(.01, Unit.KilogramMeterSquared), HubInertia = new(.001, Unit.KilogramMeterSquared),
        ReverseIdlerInertia = new(.002, Unit.KilogramMeterSquared), DriveStaticCapacity = new(180, Unit.NewtonMeter),
        DriveSlidingCapacity = new(150, Unit.NewtonMeter), SelectorStaticCapacity = new(120, Unit.NewtonMeter),
        SelectorSlidingCapacity = new(100, Unit.NewtonMeter), FinalDriveA = 4.1, FinalDriveB = 3.7, ReverseFinalDrive = 4.1
    };
    internal static DualClutchTransmissionAssembly Assembly() => new(RatiosData(), 3.3, Parameters());
    internal static DualClutchPorts Ports() => new(1, 4, 3, 100, 101, 102, 103, 104, 105, 400, 401, 402, 403, 404, 405, 500, 501);
    internal static DualClutchGearIds[] Ids() => Enumerable.Range(0, 8).Select(i => new DualClutchGearIds((uint)(110 + i), (uint)(200 + i), (uint)(300 + i), (ulong)(600 + i))).ToArray();
    internal static ModelDefinition Model(int gear = 1, int preselected = 0, ulong tick = 100_000, double torque = 10, bool drive = true)
    {
        var assembly = Assembly(); double vehicle = 60 / assembly.EffectiveReduction(gear);
        bool odd = gear > 0 && gear % 2 == 1;
        int selectedOdd = odd ? gear : preselected, selectedEven = odd ? preselected : gear;
        var graph = assembly.CreateGraph(Ports(), Ids(), vehicle, selectedOdd, selectedEven, drive && odd ? 1 : 0, drive && !odd ? 1 : 0);
        return new()
        {
            StepNanoseconds = tick,
            Nodes = [NodeDefinition.Rotor(1, .2, 60), NodeDefinition.Rotor(4, 10, vehicle), NodeDefinition.Thermal(3, 1000, 300), ..graph.Nodes],
            Components = [..graph.Components, ComponentDefinition.Torque(10, 1, 100, torque), ComponentDefinition.Torque(11, 4, 101, gear == -1 ? 2 : -2)]
        };
    }
    private static void Ok(SimulationStatus status) { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static double PathInertia(bool odd)
    {
        var p = Parameters(); var ratios = RatiosData(); double inertia = odd ? .015 : .012 + .002 + .001 / (3.3 * 3.3);
        for (int i = 0; i < 7; ++i) if ((i % 2 == 0) == odd) inertia += p.HubInertia.Value / (ratios[i] * ratios[i]);
        return inertia;
    }
    private static double VehicleGroup() => 10 + .02 * 4.1 * 4.1 + .018 * 3.7 * 3.7 + .01 * 4.1 * 4.1;
    private static void Topology()
    {
        var graph = Assembly().CreateGraph(Ports(), Ids(), selectedOdd: 1, selectedEven: 2);
        Require(graph.Nodes.Count == 14 && graph.Components.Count == 22);
        Require(graph.Components.Count(c => c.Kind == ComponentKind.IdealGear) == 12 && graph.Components.Count(c => c.Kind == ComponentKind.Clutch) == 10);
        Require(graph.SelectPath(5, true).Length == 4 && graph.SelectPath(-1, false).Length == 4);
        var model = CompiledModel.Compile(Model()); Require(model.StateCount <= CompiledModel.MaxStates && model.HasGears && model.HasClutches);
        var a = model.CreateSimulation(); Ok(a.Step(20_000_000));
        Near(Value(a, 102, Field.Speed), -4.1 * Value(a, 4, Field.Speed), 1e-11);
        Near(Value(a, 103, Field.Speed), -3.7 * Value(a, 4, Field.Speed), 1e-11);
        Near(Value(a, 104, Field.Speed), -4.1 * Value(a, 4, Field.Speed), 1e-11);
    }
    private static void Ratios()
    {
        foreach (int gear in new[] { 1, 2, 3, 4, 5, 6, 7, -1 })
            foreach (bool preselect in new[] { false, true })
            {
                bool odd = gear > 0 && gear % 2 == 1; int other = preselect ? odd ? 2 : 1 : 0;
                var s = CompiledModel.Compile(Model(gear, other)).CreateSimulation(); double ratio = Assembly().EffectiveReduction(gear);
                double vehicle = VehicleGroup(); if (other != 0) { double preRatio = Assembly().EffectiveReduction(other); vehicle += PathInertia(!odd) * preRatio * preRatio; }
                double effective = .2 + PathInertia(odd) + vehicle / (ratio * ratio);
                double acceleration = (10 + (gear == -1 ? 2 : -2) / ratio) / effective;
                Ok(s.Step(20_000_000)); Near(Value(s, 1, Field.Speed), 60 + .02 * acceleration, 1e-9);
                Near(Value(s, 1, Field.Speed), ratio * Value(s, 4, Field.Speed), 1e-9);
                Near(Value(s, 0, Field.EnergyResidual), 0, 1e-8);
            }
    }
    private static void Preselection()
    {
        var definition = Model(1, torque: 0, drive: false); definition.Components[^1] = definition.Components[^1] with { InitialInput = new(0, Unit.NewtonMeter) };
        var s = CompiledModel.Compile(definition).CreateSimulation();
        double odd = Value(s, 100, Field.Speed), vehicle = Value(s, 4, Field.Speed), reduction = 1.65;
        double slip = -odd / reduction + 4.1 * vehicle, mobility = 1 / (reduction * reduction * PathInertia(true)) + 4.1 * 4.1 / VehicleGroup();
        double impulse = -slip / mobility, heat = .5 * slip * slip / mobility;
        Ok(s.SubmitInputs([new(600, 0), new(602, 1)])); Ok(s.Step(200_000_000));
        Near(Value(s, 100, Field.Speed), odd - impulse / reduction / PathInertia(true), 1e-9);
        Near(Value(s, 4, Field.Speed), vehicle + impulse * 4.1 / VehicleGroup(), 1e-9);
        Near(Value(s, 302, Field.FrictionHeat), heat, 1e-8); Near(Value(s, 0, Field.EnergyResidual), 0, 1e-8);
    }
    private static void CorrelatedLocks()
    {
        var s = CompiledModel.Compile(Model(7, 6)).CreateSimulation(); double ratio = Assembly().EffectiveReduction(7), pre = Assembly().EffectiveReduction(6);
        double inertia = .2 + PathInertia(true) + (VehicleGroup() + PathInertia(false) * pre * pre) / (ratio * ratio);
        Ok(s.Step(200_000_000)); Near(Value(s, 1, Field.Speed), 60 + .2 * (10 - 2 / ratio) / inertia, 1e-8);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-8);
    }
    private static void Transactions()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation(); var before = Snapshot(s);
        Require(s.Step(10_000_000, new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        var fork = s.Fork(); Ok(s.Step(10_000_000)); for (int i = 0; i < 10; ++i) Ok(fork.Step(1_000_000)); Require(Snapshot(s) == Snapshot(fork));
        before = Snapshot(s); Require(s.Step(10_000_000, [new(before.TimeNanoseconds + 5_000_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var values = new Scalar[s.Model.OutputCount]; for (int i = 0; i < 10; ++i) { Ok(s.Step(100_000)); s.ReadSnapshot(values); }
        long bytes = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 100; ++i) { Ok(s.Step(100_000)); s.ReadSnapshot(values); }
        Require(bytes == GC.GetAllocatedBytesForCurrentThread(), "DCT stepping or readback allocated.");
    }
    private static void Contracts()
    {
        var ratios = RatiosData(); var assembly = new DualClutchTransmissionAssembly(ratios, 3.3, Parameters()); ratios[0] = 100;
        Near(assembly.ForwardReductions[0], 3.8, 0);
        Throws<ArgumentException>(() => new DualClutchTransmissionAssembly(RatiosData(), 3.3, Parameters() with { HubInertia = new(.001, Unit.Joule) }));
        Throws<ArgumentException>(() => assembly.CreateGraph(Ports() with { EvenShaft = 100 }, Ids()));
        Throws<ArgumentException>(() => assembly.CreateGraph(Ports(), Ids(), selectedOdd: 2));
        var ids = Ids(); ids[1] = ids[1] with { Command = 600 }; Throws<ArgumentException>(() => assembly.CreateGraph(Ports(), ids));
        Throws<ArgumentException>(() => assembly.CreateGraph(Ports(), Ids()).SelectPath(2, true));
    }
}
