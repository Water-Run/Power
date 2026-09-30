// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class PistonChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("piston contact / discrete work / unilateral pad and compliant stroke stops", Contact),
        ("linear damping / analytic decay / independent compact heat histories", Damping),
        ("hydraulic piston / analytic coupled spring and compliance / refinement", Oscillation),
        ("hydraulic piston / two chambers / swept inventory / back reservoir work", Ports),
        ("piston pad / independent piecewise RK4 reference / contact refinement", PadReference),
        ("piston clutch / free fill / contact capacity / capture and release", Clutch),
        ("piston transactions / branches / scheduled rollback / cancellation / allocations", Transactions),
        ("piston contracts / mass and force units / typed chambers and pad ownership", Contracts)
    ];
    private static void Ok(SimulationStatus status) { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation simulation) => simulation.ReadSnapshot(new Scalar[simulation.Model.OutputCount]);
    internal static HydraulicPistonDefinition Definition(uint back = 0, double backArea = 0, double contact = 0) => new()
    {
        FrontArea = new(.001, Unit.SquareMeter), BackArea = new(backArea, Unit.SquareMeter), BackNode = back,
        BackPressure = back == 0 ? new(0, Unit.Pascal) : default,
        MinimumPosition = new(-1, Unit.Meter), MaximumPosition = new(1, Unit.Meter), StopStiffness = new(0, Unit.NewtonPerMeter),
        ContactPosition = new(.002, Unit.Meter), ContactStiffness = new(contact, Unit.NewtonPerMeter)
    };
    internal static ModelDefinition Model(ulong step = 100_000) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Translational(1, 1), NodeDefinition.Hydraulic(2, 1e-9, 1e5), NodeDefinition.Thermal(3, 100, 300)],
        Components = [ComponentDefinition.Piston(10, 1, 2, Definition()), ComponentDefinition.LinearSpring(11, 1, 0, 1000, 0, heat: 3)]
    };
    internal static ModelDefinition ClutchModel(ulong step = 20_000) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, .2, 40), NodeDefinition.Rotor(2, .5), NodeDefinition.Hydraulic(3, 2e-12),
            NodeDefinition.Thermal(4, 100, 300), NodeDefinition.Translational(5, .02)],
        Components = [ComponentDefinition.Piston(10, 5, 3, Definition(contact: 1e6) with
                { MinimumPosition = new(0, Unit.Meter), MaximumPosition = new(.006, Unit.Meter), StopStiffness = new(1e6, Unit.NewtonPerMeter) }),
            ComponentDefinition.LinearSpring(11, 5, 0, 10000, 300, heat: 4),
            ComponentDefinition.ContactClutch(12, 1, 2, new() { PistonComponent = 10, EffectiveRadius = new(.08, Unit.Meter),
                StaticFriction = .4, SlidingFriction = .2, FrictionSurfaces = 4 }, heat: 4),
            ComponentDefinition.HydraulicResistance(13, 3, 0, 2e-10, 1e6, 100, 1, 4),
            ComponentDefinition.HydraulicResistance(14, 3, 0, 2e-10, 0, 101, 0, 4)]
    };
    private static void Contact()
    {
        var law = new HydraulicPiston(.001, .0005, 0, .01, 2000, .003, 1000);
        foreach (double old in new[] { -.002, 0, .001, .003, .005, .012 })
        foreach (double next in new[] { -.001, 0, .002, .003, .006, .014 })
        {
            Near(law.DiscreteElasticReaction(old, next) * (next - old), law.StoredContactEnergy(old) - law.StoredContactEnergy(next), 1e-12);
            Require(law.DiscreteContactForce(old, next) >= 0);
            if (next != 0 && next != .003 && next != .01)
            {
                const double epsilon = 1e-8;
                double numerical = (law.DiscreteElasticReaction(old, next + epsilon) - law.DiscreteElasticReaction(old, next - epsilon)) / (2 * epsilon);
                Near(law.DiscreteElasticDerivative(old, next), numerical, 2e-6);
            }
        }
        Near(law.DiscreteElasticDerivative(.003, .003), -250, 0);
        Near(law.DiscreteElasticDerivative(.001, .002), 0, 0);
        Near(law.DiscreteElasticDerivative(.005, .006), -500, 0);
        Near(law.DiscreteElasticDerivative(.001, .005), -375, 1e-12);
        Near(law.DiscreteElasticDerivative(.005, .001), -125, 1e-12);
        Near(law.ContactForce(.002), 0, 0); Near(law.ContactForce(.005), 2, 1e-14);
        Near(law.PressureForce(2e5, 1e5), 150, 1e-13);
        Near(law.DiscreteContactForce(.005, .005 + 1e-18), 2, 1e-12);
        Throws<ArgumentException>(() => new HydraulicPiston(0, 0, 0, .01, 1000, .003, 1000));
    }
    private static void Damping()
    {
        var definition = new ModelDefinition
        {
            StepNanoseconds = 100_000,
            Nodes = [NodeDefinition.Translational(1, 2, 4), NodeDefinition.Translational(2, 1, 3), NodeDefinition.Thermal(3, 100, 300)],
            Components = [ComponentDefinition.LinearSpring(10, 1, 0, 0, 3, heat: 3), ComponentDefinition.LinearSpring(11, 2, 0, 0, 2)]
        };
        var simulation = CompiledModel.Compile(definition).CreateSimulation(); Ok(simulation.Step(100_000_000));
        double first = 4 * Math.Pow((1 - .000075) / (1 + .000075), 1000), second = 3 * Math.Pow((1 - .0001) / (1 + .0001), 1000);
        Near(Value(simulation, 1, Field.LinearSpeed), first, 1e-11); Near(Value(simulation, 2, Field.LinearSpeed), second, 1e-11);
        Near(Value(simulation, 10, Field.FrictionHeat), 16 - first * first, 1e-9);
        Near(Value(simulation, 11, Field.FrictionHeat), 4.5 - .5 * second * second, 1e-9);
        Near(Value(simulation, 0, Field.HeatRejected), Value(simulation, 11, Field.FrictionHeat), 1e-12);
        Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8);
    }
    private static void Oscillation()
    {
        double frequency = Math.Sqrt(2000), time = .02, expectedX = .05 * (1 - Math.Cos(frequency * time)), expectedV = .05 * frequency * Math.Sin(frequency * time);
        double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 400_000, 200_000, 100_000 })
        {
            var simulation = CompiledModel.Compile(Model(step)).CreateSimulation(); Ok(simulation.Step(20_000_000));
            double x = Value(simulation, 1, Field.Displacement), v = Value(simulation, 1, Field.LinearSpeed), error = Math.Abs(x - expectedX) + .01 * Math.Abs(v - expectedV);
            Require(previous / error > 3.8, $"Piston refinement {previous:R}/{error:R}"); previous = error;
            Near(Value(simulation, 2, Field.Pressure), 1e5 - .001 * x / 1e-9, 1e-7);
            Near(Value(simulation, 0, Field.HydraulicVolumeResidual), 0, 1e-17); Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-9);
        }
    }
    private static void Ports()
    {
        var definition = Model() with { Nodes = [..Model().Nodes, NodeDefinition.Hydraulic(4, 1e-9, 5e4)],
            Components = [ComponentDefinition.Piston(10, 1, 2, Definition(4, .0005)), Model().Components[1]] };
        var simulation = CompiledModel.Compile(definition).CreateSimulation(); Ok(simulation.Step(20_000_000));
        double x = Value(simulation, 1, Field.Displacement), frequency = Math.Sqrt(2250), expected = 75.0 / 2250 * (1 - Math.Cos(frequency * .02));
        Near(x, expected, 1e-7); Near(Value(simulation, 2, Field.Pressure), 1e5 - .001 * x / 1e-9, 1e-7);
        Near(Value(simulation, 4, Field.Pressure), 5e4 + .0005 * x / 1e-9, 1e-7);
        Near(Value(simulation, 0, Field.HydraulicVolumeResidual), 0, 1e-17); Near(Value(simulation, 0, Field.HydraulicWork), 0, 0);
        var reservoir = definition with { Nodes = Model().Nodes, Components = [ComponentDefinition.Piston(10, 1, 2,
            Definition(backArea: .0005) with { BackPressure = new(5e4, Unit.Pascal) }), Model().Components[1]] };
        var open = CompiledModel.Compile(reservoir).CreateSimulation(); Ok(open.Step(20_000_000));
        Near(Value(open, 0, Field.HydraulicWork), -5e4 * .0005 * Value(open, 1, Field.Displacement), 1e-10);
        Near(Value(open, 0, Field.EnergyResidual), 0, 1e-9); Near(Value(open, 0, Field.HydraulicVolumeResidual), 0, 1e-17);
    }
    private static void PadReference()
    {
        var definition = Model() with { Components = [ComponentDefinition.Piston(10, 1, 2, Definition(contact: 5000)), ComponentDefinition.LinearSpring(11, 1, 0, 1000, 4, heat: 3)] };
        double[] x = [0, 0, 1e5], a = new double[3], b = new double[3], c = new double[3], d = new double[3], trial = new double[3]; const double h = .000001;
        void Derivative(double[] state, double[] target)
        { target[0] = state[1]; target[1] = .001 * state[2] - 1000 * state[0] - 4 * state[1] - 5000 * Math.Max(state[0] - .002, 0); target[2] = -.001 * state[1] / 1e-9; }
        for (int tick = 0; tick < 40000; ++tick)
        {
            Derivative(x, a); for (int i = 0; i < 3; ++i) trial[i] = x[i] + h * a[i] / 2;
            Derivative(trial, b); for (int i = 0; i < 3; ++i) trial[i] = x[i] + h * b[i] / 2;
            Derivative(trial, c); for (int i = 0; i < 3; ++i) trial[i] = x[i] + h * c[i];
            Derivative(trial, d); for (int i = 0; i < 3; ++i) x[i] += h * (a[i] + 2 * b[i] + 2 * c[i] + d[i]) / 6;
        }
        double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 400_000, 200_000, 100_000 })
        {
            var simulation = CompiledModel.Compile(definition with { StepNanoseconds = step }).CreateSimulation(); Ok(simulation.Step(40_000_000));
            double error = Math.Abs(Value(simulation, 1, Field.Displacement) - x[0]) + .01 * Math.Abs(Value(simulation, 1, Field.LinearSpeed) - x[1]);
            Require(error < previous && error < 1e-4, $"Pad refinement {previous:R}/{error:R}"); previous = error;
            Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8);
        }
    }
    private static void Clutch()
    {
        var model = CompiledModel.Compile(ClutchModel()); var simulation = model.CreateSimulation();
        Near(Value(simulation, 12, Field.ClampForce), 0, 0); Ok(simulation.Step(20_000));
        Require(Value(simulation, 3, Field.Pressure) > 0 && Value(simulation, 5, Field.Displacement) < .002);
        Near(Value(simulation, 12, Field.StaticCapacity), 0, 0); Near(Value(simulation, 2, Field.Speed), 0, 0);
        Ok(simulation.Step(199_980_000));
        Require(Value(simulation, 12, Field.ClutchMode) == 1);
        Require(Value(simulation, 12, Field.ClampForce) > 0 && Value(simulation, 12, Field.FrictionHeat) > 0);
        Near(Value(simulation, 12, Field.StaticCapacity), .4 * 4 * .08 * Value(simulation, 12, Field.ClampForce), 1e-10);
        Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-7); Near(Value(simulation, 0, Field.HydraulicVolumeResidual), 0, 1e-15);
        Ok(simulation.SubmitInputs([new(100, 0), new(101, 1)])); Ok(simulation.Step(200_000_000));
        Require(Value(simulation, 12, Field.ClutchMode) == 0); Near(Value(simulation, 12, Field.ClampForce), 0, 1e-8);
    }
    private static void Transactions()
    {
        var definition = ClutchModel() with { Components = [..ClutchModel().Components, ComponentDefinition.Force(15, 5, 102, 0)] };
        var model = CompiledModel.Compile(definition); var simulation = model.CreateSimulation(); Ok(simulation.Step(10_000_000)); var before = Snapshot(simulation);
        Require(simulation.Step(20_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(simulation) == before);
        Require(simulation.Step(20_000_000, [new(15_000_000, 100, 2)]) == SimulationStatus.InvalidInput && Snapshot(simulation) == before);
        var fork = simulation.Fork(); Ok(simulation.Step(20_000_000)); for (int i = 0; i < 20; ++i) Ok(fork.Step(1_000_000)); Require(Snapshot(simulation) == Snapshot(fork));
        Ok(fork.SubmitInputs([new(100, 0), new(101, 1)])); Ok(fork.Step(10_000_000)); Require(Snapshot(simulation) != Snapshot(fork));
        var buffer = new Scalar[model.OutputCount]; for (int i = 0; i < 5; ++i) { Ok(simulation.Step(1_000_000)); simulation.ReadSnapshot(buffer); }
        long allocated = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 100; ++i) { Ok(simulation.Step(1_000_000)); simulation.ReadSnapshot(buffer); }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Piston stepping allocated.");
        var late = simulation.Fork(); var initial = Snapshot(late);
        Require(late.Step(20_000_000, [new(initial.TimeNanoseconds + 10_000_000, 102, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(late) == initial);
    }
    private static void Contracts()
    {
        var definition = Model(); var piston = definition.Components[0];
        foreach (var invalid in new[] { piston with { NodeA = 2 }, piston with { NodeB = 1 }, piston with { HydraulicPiston = Definition(2) },
            piston with { HydraulicPiston = Definition() with { FrontArea = new(.001, Unit.Meter) } },
            piston with { HydraulicPiston = Definition() with { ContactPosition = new(2, Unit.Meter) } } })
            Require(!CompiledModel.TryCompile(definition with { Components = [invalid, definition.Components[1]] }, out _, out _));
        Require(!CompiledModel.TryCompile(definition with { Components = [..definition.Components, piston with { Id = 12 }] }, out _, out _));
        var compiled = CompiledModel.Compile(definition); definition.Components[0] = piston with { HydraulicPiston = Definition(contact: 1000) };
        Require(compiled.Fingerprint != CompiledModel.Compile(definition).Fingerprint);
    }
}
