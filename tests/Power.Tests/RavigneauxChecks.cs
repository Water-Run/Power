// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class RavigneauxChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("Ravigneaux free dynamics / independent reduced mass matrix / reaction power", Free),
        ("Ravigneaux four forward and reverse / independent reflected inertia and brake reactions", Ranges),
        ("Ravigneaux brake capture / independent impulse and heat / conservation", Capture),
        ("Ravigneaux long loaded overdrive / analytic acceleration / strict phase preservation", LongRun),
        ("Ravigneaux transactions / cancellation rollback forks / zero allocations", Transactions),
        ("Ravigneaux definitions / units geometry IDs commands / immutable graph", Contracts)
    ];
    internal static RavigneauxParameters Parameters() => new()
    {
        LargeSunInertia = new(.02, Unit.KilogramMeterSquared), SmallSunInertia = new(.015, Unit.KilogramMeterSquared),
        RingInertia = new(.04, Unit.KilogramMeterSquared), CarrierInertia = new(.03, Unit.KilogramMeterSquared),
        StaticCapacity = new(400, Unit.NewtonMeter), SlidingCapacity = new(300, Unit.NewtonMeter),
        RingToLargeSun = 2, RingToSmallSun = 3, FinalDrive = 4
    };
    internal static RavigneauxPorts Ports() => new(1, 4, 3, 100, 101, 102, 103, 200, 201, 202, 300, 301, 302, 303, 304, 600, 601, 602, 603, 604);
    internal static ModelDefinition Model(int range = 1, ulong tick = 100_000, double torque = 10)
    {
        var assembly = new RavigneauxTransmissionAssembly(Parameters()); double ring = 30 / assembly.RangeReduction(range);
        var graph = assembly.CreateGraph(Ports(), ring, range);
        return new() { StepNanoseconds = tick,
            Nodes = [NodeDefinition.Rotor(1, .2, 30), NodeDefinition.Rotor(4, 10, ring / 4), NodeDefinition.Thermal(3, 1000, 300), ..graph.Nodes],
            Components = [..graph.Components, ComponentDefinition.Torque(10, 1, 500, torque), ComponentDefinition.Torque(11, 4, 501, range == -1 ? 2 : -2)] };
    }
    private static void Ok(SimulationStatus s) { if (s != SimulationStatus.Ok) throw new InvalidOperationException(s.ToString()); }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static (double A, double B, double D) Mass()
    {
        // Coordinates are ring and carrier; member speeds are [-2r+3c, 3r-2c, r, c].
        return (.02 * 4 + .015 * 9 + .04, -.02 * 6 - .015 * 6, .02 * 9 + .015 * 4 + .03);
    }
    private static void Free()
    {
        var definition = new ModelDefinition { StepNanoseconds = 100_000,
            Nodes = [NodeDefinition.Rotor(1, .02, -10), NodeDefinition.Rotor(2, .015, 40), NodeDefinition.Rotor(3, .04, 20), NodeDefinition.Rotor(4, .03, 10)],
            Components = [ComponentDefinition.PlanetaryGear(10, 1, 3, 4, 2), ComponentDefinition.DoublePinionPlanetary(11, 2, 3, 4, 3),
                ComponentDefinition.Torque(20, 1, 500, 2), ComponentDefinition.Torque(21, 2, 501, -1), ComponentDefinition.Torque(22, 3, 502, 3), ComponentDefinition.Torque(23, 4, 503, -2)] };
        var mass = Mass(); double qr = -2 * 2 + 3 * -1 + 3, qc = 3 * 2 - 2 * -1 - 2;
        double determinant = mass.A * mass.D - mass.B * mass.B;
        double ar = (mass.D * qr - mass.B * qc) / determinant, ac = (mass.A * qc - mass.B * qr) / determinant;
        var s = CompiledModel.Compile(definition).CreateSimulation(); Ok(s.Step(20_000_000));
        Near(Value(s, 1, Field.Speed), -10 + .02 * (-2 * ar + 3 * ac), 1e-10);
        Near(Value(s, 2, Field.Speed), 40 + .02 * (3 * ar - 2 * ac), 1e-10);
        Near(Value(s, 3, Field.Speed), 20 + .02 * ar, 1e-10); Near(Value(s, 4, Field.Speed), 10 + .02 * ac, 1e-10);
        foreach (uint id in new uint[] { 10, 11 })
        {
            uint sun = id == 10 ? 1U : 2U;
            double power = Value(s, id, Field.Torque) * Value(s, sun, Field.Speed) + Value(s, id, Field.TorqueAtB) * Value(s, 3, Field.Speed) + Value(s, id, Field.TorqueAtC) * Value(s, 4, Field.Speed);
            Near(power, 0, 1e-9); Near(Value(s, id, Field.ConstraintError), 0, 1e-10);
        }
        Near(Value(s, 10, Field.Torque) + Value(s, 11, Field.Torque), .02 * (-2 * ar + 3 * ac) - 2 + .015 * (3 * ar - 2 * ac) + 1, 1e-8);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-8);
    }
    private static void Ranges()
    {
        foreach (int range in new[] { 1, 2, 3, 4, -1 })
        {
            // Independent member factors per unit input speed, from the five friction connections.
            double r = range switch { 1 => 1.0 / 3, 2 => 3.0 / 5, 3 => 1, 4 => 1.5, _ => -.5 };
            double c = range switch { 2 => .4, 3 or 4 => 1, _ => 0 }, l = -2 * r + 3 * c, small = 3 * r - 2 * c;
            double inertia = .2 + 10 * r * r / 16 + .02 * l * l + .015 * small * small + .04 * r * r + .03 * c * c;
            double load = range == -1 ? 2 : -2, acceleration = (10 + load * r / 4) / inertia;
            var s = CompiledModel.Compile(Model(range)).CreateSimulation(); Ok(s.Step(20_000_000));
            double input = 30 + .02 * acceleration; Near(Value(s, 1, Field.Speed), input, 1e-9);
            foreach (var item in new[] { (100U, l), (101U, small), (102U, r), (103U, c), (4U, r / 4) }) Near(Value(s, item.Item1, Field.Speed), item.Item2 * input, 1e-9);
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-8);
            foreach (uint gear in new uint[] { 200, 201, 202 }) Near(Value(s, gear, Field.ConstraintError), 0, 1e-10);
            if (range is 1 or -1) Near(Value(s, 303, Field.Torque), -Value(s, 200, Field.TorqueAtC) - Value(s, 201, Field.TorqueAtC), 1e-8);
            if (range is 2 or 4) Near(Value(s, 304, Field.Torque), -Value(s, 200, Field.Torque), 1e-8);
        }
    }
    private static void Capture()
    {
        // Free compound graph with a carrier brake; kinetic projection uses the inverse 2x2 mass matrix.
        var mass = Mass(); double determinant = mass.A * mass.D - mass.B * mass.B, mobility = mass.A / determinant;
        double impulse = -10 / mobility, heat = .5 * 100 / mobility;
        var definition = new ModelDefinition { StepNanoseconds = 100_000,
            Nodes = [NodeDefinition.Rotor(1, .02, -10), NodeDefinition.Rotor(2, .015, 40), NodeDefinition.Rotor(3, .04, 20), NodeDefinition.Rotor(4, .03, 10), NodeDefinition.Thermal(5, 1000, 300)],
            Components = [ComponentDefinition.PlanetaryGear(10, 1, 3, 4, 2), ComponentDefinition.DoublePinionPlanetary(11, 2, 3, 4, 3), ComponentDefinition.Clutch(12, 4, 0, 400, 300, 600, 1, heat: 5)] };
        var s = CompiledModel.Compile(definition).CreateSimulation(); Ok(s.Step(100_000_000));
        Near(Value(s, 4, Field.Speed), 0, 1e-10); Near(Value(s, 3, Field.Speed), 20 - mass.B / determinant * impulse, 1e-9);
        Near(Value(s, 12, Field.FrictionHeat), heat, 1e-8); Near(Value(s, 5, Field.Temperature), 300 + heat / 1000, 1e-10);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-8);
    }
    private static void LongRun()
    {
        var s = CompiledModel.Compile(Model(4)).CreateSimulation();
        // Overdrive: ring=1.5 input, carrier=input, large sun=0, small sun=2.5 input.
        double inertia = .2 + 10 * 1.5 * 1.5 / 16 + .015 * 2.5 * 2.5 + .04 * 1.5 * 1.5 + .03;
        double acceleration = (10 - 2 * 1.5 / 4) / inertia;
        Ok(s.Step(10_000_000_000)); Ok(s.Step(10_000_000_000));
        Near(Value(s, 1, Field.Speed), 30 + 20 * acceleration, 2e-7);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-6);
        foreach (uint gear in new uint[] { 200, 201, 202 }) Near(Value(s, gear, Field.ConstraintError), 0, 1e-8);
    }
    private static void Transactions()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation(); var before = Snapshot(s);
        Require(s.Step(10_000_000, new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        var fork = s.Fork(); Ok(s.Step(10_000_000)); for (int i = 0; i < 10; ++i) Ok(fork.Step(1_000_000)); Require(Snapshot(s) == Snapshot(fork));
        before = Snapshot(s); Require(s.Step(10_000_000, [new(before.TimeNanoseconds + 5_000_000, 500, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var values = new Scalar[s.Model.OutputCount]; for (int i = 0; i < 10; ++i) { Ok(s.Step(100_000)); s.ReadSnapshot(values); }
        long bytes = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 100; ++i) { Ok(s.Step(100_000)); s.ReadSnapshot(values); } Require(bytes == GC.GetAllocatedBytesForCurrentThread());
    }
    private static void Contracts()
    {
        var assembly = new RavigneauxTransmissionAssembly(Parameters()); var graph = assembly.CreateGraph(Ports());
        Require(graph.Nodes.Count == 4 && graph.Components.Count == 8 && graph.RangeCommands(0).All(c => c.Value == 0));
        Throws<ArgumentException>(() => new RavigneauxTransmissionAssembly(Parameters() with { RingToSmallSun = 2 }));
        Throws<ArgumentException>(() => new RavigneauxTransmissionAssembly(Parameters() with { CarrierInertia = new(.03, Unit.Joule) }));
        Throws<ArgumentException>(() => assembly.CreateGraph(Ports() with { SmallSun = 100 }));
        Throws<ArgumentException>(() => assembly.CreateGraph(Ports() with { SmallCommand = 600 }));
        Throws<ArgumentException>(() => graph.RangeCommands(5));
        var model = Model(); var bad = model.Components.ToArray(); bad[1] = bad[1] with { NodeC = 100 }; Throws<ModelCompileException>(() => CompiledModel.Compile(model with { Components = bad }));
        bad[1] = model.Components[1] with { Ratio = 1 }; Throws<ModelCompileException>(() => CompiledModel.Compile(model with { Components = bad }));
    }
}
