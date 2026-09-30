// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class GasPistonChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("gas piston / signed volume / adiabatic work identity / analytic derivative / thin travel", Law),
        ("gas piston / independent mass-energy RK4 / smooth second-order refinement", Reference),
        ("opposed gas pistons / shared coordinate / reference work cancellation / energy", Opposed),
        ("gas-backed hydraulic piston / common mass / swept volume / separate energy accounts", Hydraulic),
        ("gas piston wall heat / finite bath / mass and energy / timestep refinement", Wall),
        ("gas piston moving-volume gas port / mass and enthalpy / sealed continuation", Port),
        ("gas piston transactions / forks / cancellation / late volume failure / zero allocation", Transactions),
        ("gas piston contracts / dimensions / ownership / stroke / immutable compiled geometry", Contracts)
    ];
    private static void Ok(SimulationStatus status) { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation simulation) => simulation.ReadSnapshot(new Scalar[simulation.Model.OutputCount]);
    internal static GasPistonDefinition Geometry(double area = .001, double volume = .0001, double reference = 1e5, int direction = 1) => new()
    { Area = new(area, Unit.SquareMeter), ReferenceVolume = new(volume, Unit.CubicMeter), ReferencePosition = new(0, Unit.Meter), ReferencePressure = new(reference, Unit.Pascal), CompressionDirection = direction };
    internal static ModelDefinition Model(ulong step = 100_000) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Translational(1, 1, .01), NodeDefinition.CylinderGas(2, 1e5, 300), NodeDefinition.Thermal(3, 100, 300)],
        Components = [ComponentDefinition.GasActuator(10, 1, 2, Geometry()), ComponentDefinition.LinearSpring(11, 1, 0, 1000, 5, heat: 3), ComponentDefinition.Force(12, 1, 100, 2)]
    };
    private static void Law()
    {
        foreach (int direction in new[] { -1, 1 })
        foreach (double gamma in new[] { 1.01, 1.4, 2.0, 9.0 })
        {
            var law = new GasPiston(.01, .001, 0, 1e5, direction);
            foreach (double old in new[] { -.005, 0, .004 })
            foreach (double next in new[] { -.002, 0, .006 })
            {
                Require(law.TryAdiabaticWork(old, next, 100, gamma, out var work));
                double expected = 100 * (Math.Pow(law.VolumeAt(old) / law.VolumeAt(next), gamma - 1) - 1);
                Near(work.GasEnergyChangeJoules, expected, 1e-11);
                Near(work.ForceNewtons * (next - old), work.ReferenceWorkJoules - work.GasEnergyChangeJoules, 1e-12);
                const double h = 1e-7;
                Require(law.TryAdiabaticWork(old, next + h, 100, gamma, out var upper)); Require(law.TryAdiabaticWork(old, next - h, 100, gamma, out var lower));
                Near(work.ForceDerivativeNewtonsPerMeter, (upper.ForceNewtons - lower.ForceNewtons) / (2 * h), .001);
            }
            Require(law.TryAdiabaticWork(0, 1e-14, 100, gamma, out var thin));
            Near(thin.MeanAbsolutePressurePascals, (gamma - 1) * 100 / .001, 1e-5);
            Require(law.TryAdiabaticWork(0, 0, 100, gamma, out var rest));
            Near(rest.ForceDerivativeNewtonsPerMeter, -.5 * gamma * (gamma - 1) * 100 / .001 * .01 * .01 / .001, 1e-8);
        }
        var invalid = new GasPiston(.001, .0001, 0, 1e5);
        Require(!invalid.TryAdiabaticWork(0, .1, 25, 1.4, out _) && !invalid.TryAdiabaticWork(0, 0, 25, 1, out _));
        Throws<ArgumentException>(() => new GasPiston(-1, 1, 0, 0)); Throws<ArgumentException>(() => new GasPiston(1, 1, 0, 0, 0));
    }
    private static double[] Integrate(bool hydraulic, double time)
    {
        double[] x = [0, hydraulic ? 0 : .01, hydraulic ? 75 : 25, 2e5], a = new double[4], b = new double[4], c = new double[4], d = new double[4], trial = new double[4]; const double h = 1e-6;
        void Derivative(double[] state, double[] target)
        {
            double volume = .0001 - .001 * state[0], pressure = .4 * state[2] / volume;
            target[0] = state[1]; target[1] = (hydraulic ? .001 * state[3] : 0) - .001 * (pressure - 1e5) - 1000 * state[0] - 5 * state[1] + 2;
            target[2] = pressure * .001 * state[1]; target[3] = hydraulic ? -.001 * state[1] / 1e-11 : 0;
        }
        for (int tick = 0; tick < (int)Math.Round(time / h); ++tick)
        {
            Derivative(x, a); for (int i = 0; i < 4; ++i) trial[i] = x[i] + h * a[i] / 2;
            Derivative(trial, b); for (int i = 0; i < 4; ++i) trial[i] = x[i] + h * b[i] / 2;
            Derivative(trial, c); for (int i = 0; i < 4; ++i) trial[i] = x[i] + h * c[i];
            Derivative(trial, d); for (int i = 0; i < 4; ++i) x[i] += h * (a[i] + 2 * b[i] + 2 * c[i] + d[i]) / 6;
        }
        return x;
    }
    private static ModelDefinition HydraulicModel(ulong step) => Model(step) with
    {
        Nodes = [NodeDefinition.Translational(1, 1), NodeDefinition.CylinderGas(2, 3e5, 300), NodeDefinition.Thermal(3, 100, 300), NodeDefinition.Hydraulic(4, 1e-11, 2e5)],
        Components = [..Model(step).Components, ComponentDefinition.Piston(13, 1, 4, PistonChecks.Definition() with
            { MinimumPosition = new(-.02, Unit.Meter), MaximumPosition = new(.02, Unit.Meter), ContactPosition = new(.02, Unit.Meter) })]
    };
    private static void Refinement(bool hydraulic)
    {
        double time = hydraulic ? .02 : .04; var reference = Integrate(hydraulic, time); double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 200_000, 100_000, 50_000 })
        {
            var model = CompiledModel.Compile(hydraulic ? HydraulicModel(step) : Model(step)); var simulation = model.CreateSimulation(); Ok(simulation.Step((ulong)(time * 1e9)));
            double x = Value(simulation, 1, Field.Displacement), speed = Value(simulation, 1, Field.LinearSpeed), volume = Value(simulation, 10, Field.Volume), energy = Value(simulation, 2, Field.InternalEnergy);
            double error = Math.Abs(x - reference[0]) + .01 * Math.Abs(speed - reference[1]) + .00001 * Math.Abs(energy - reference[2]);
            Require(previous / error > 3.7, $"Gas-piston refinement {previous:R}/{error:R}"); previous = error;
            Near(energy * Math.Pow(volume / .0001, .4), hydraulic ? 75 : 25, 1e-9);
            Near(Value(simulation, 2, Field.Mass), (hydraulic ? 3e5 : 1e5) * .0001 / (287 * 300), 1e-18);
            Near(Value(simulation, 10, Field.SourceWork), 100 * x, 1e-12); Near(Value(simulation, 0, Field.SourceWork), 102 * x, 1e-10);
            Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8);
            if (hydraulic)
            {
                Near(Value(simulation, 4, Field.Pressure), 2e5 - .001 * x / 1e-11, 1e-6);
                Near(Value(simulation, 0, Field.HydraulicVolumeResidual), 0, 1e-17);
            }
        }
    }
    private static void Reference() => Refinement(false);
    private static void Hydraulic() => Refinement(true);
    private static void Opposed()
    {
        var definition = new ModelDefinition
        {
            StepNanoseconds = 50_000,
            Nodes = [NodeDefinition.Translational(1, 1, .02), NodeDefinition.CylinderGas(2, 1e5, 300), NodeDefinition.CylinderGas(3, 1e5, 300)],
            Components = [ComponentDefinition.GasActuator(10, 1, 2, Geometry(volume: .0002)), ComponentDefinition.GasActuator(11, 1, 3, Geometry(volume: .0002, direction: -1))]
        };
        var simulation = CompiledModel.Compile(definition).CreateSimulation(); Ok(simulation.Step(20_000_000));
        double x = Value(simulation, 1, Field.Displacement); Require(x > 0);
        Near(Value(simulation, 10, Field.Volume), .0002 - .001 * x, 1e-18); Near(Value(simulation, 11, Field.Volume), .0002 + .001 * x, 1e-18);
        Near(Value(simulation, 10, Field.SourceWork) + Value(simulation, 11, Field.SourceWork), 0, 0);
        Near(Value(simulation, 0, Field.SourceWork), 0, 0); Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-9);
    }
    private static void Wall()
    {
        double previous = double.PositiveInfinity;
        var definition = Model() with
        {
            Nodes = [NodeDefinition.Translational(1, 1), NodeDefinition.CylinderGas(2, 1e5, 300), NodeDefinition.Thermal(3, .1, 400)],
            Components = [ComponentDefinition.GasActuator(10, 1, 2, Geometry()), ComponentDefinition.LinearSpring(11, 1, 0, 1000, 5, heat: 3), ComponentDefinition.GasHeatLink(12, 2, 3, 1)]
        };
        double[] state = [0, 0, 25, 400], a = new double[4], b = new double[4], c = new double[4], d = new double[4], trial = new double[4]; const double h = 1e-6;
        void Derivative(double[] value, double[] rate)
        {
            double pressure = .4 * value[2] / (.0001 - .001 * value[0]), heat = value[3] - value[2] / (25.0 / 300);
            rate[0] = value[1]; rate[1] = -.001 * (pressure - 1e5) - 1000 * value[0] - 5 * value[1];
            rate[2] = pressure * .001 * value[1] + heat; rate[3] = (-heat + 5 * value[1] * value[1]) / .1;
        }
        for (int tick = 0; tick < 20000; ++tick)
        {
            Derivative(state, a); for (int i = 0; i < 4; ++i) trial[i] = state[i] + h * a[i] / 2;
            Derivative(trial, b); for (int i = 0; i < 4; ++i) trial[i] = state[i] + h * b[i] / 2;
            Derivative(trial, c); for (int i = 0; i < 4; ++i) trial[i] = state[i] + h * c[i];
            Derivative(trial, d); for (int i = 0; i < 4; ++i) state[i] += h * (a[i] + 2 * b[i] + 2 * c[i] + d[i]) / 6;
        }
        foreach (ulong step in new ulong[] { 200_000, 100_000, 50_000 })
        {
            var simulation = CompiledModel.Compile(definition with { StepNanoseconds = step }).CreateSimulation(); Ok(simulation.Step(20_000_000));
            Require(Value(simulation, 2, Field.Temperature) > 300 && Value(simulation, 3, Field.Temperature) < 400);
            double error = Math.Abs(Value(simulation, 2, Field.InternalEnergy) - state[2]); Require(previous / error > 1.8, $"Wall refinement {previous:R}/{error:R}"); previous = error;
            Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8);
        }
    }
    private static void Port()
    {
        var definition = Model(20_000); definition = definition with { Components = [..definition.Components,
            ComponentDefinition.GasReservoir(13, 2, 1e-7, 2e5, 300, .8, 101, .4)] };
        var simulation = CompiledModel.Compile(definition).CreateSimulation(); double initial = Value(simulation, 2, Field.Mass);
        Ok(simulation.Step(20_000_000)); double supplied = Value(simulation, 2, Field.Mass);
        Require(supplied > initial && Value(simulation, 0, Field.ReservoirEnthalpy) > 0);
        Near(Value(simulation, 0, Field.MassResidual), 0, 1e-16); Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8);
        Ok(simulation.SubmitInputs([new(101, 0)])); Ok(simulation.Step(20_000_000));
        Near(Value(simulation, 2, Field.Mass), supplied, 0); Near(Value(simulation, 0, Field.MassResidual), 0, 1e-16);
        Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8);
    }
    private static void Transactions()
    {
        var simulation = CompiledModel.Compile(Model()).CreateSimulation(); Ok(simulation.Step(10_000_000)); var before = Snapshot(simulation);
        Require(simulation.Step(20_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(simulation) == before);
        var fork = simulation.Fork(); Ok(simulation.Step(20_000_000)); for (int i = 0; i < 20; ++i) Ok(fork.Step(1_000_000)); Require(Snapshot(simulation) == Snapshot(fork));
        Ok(fork.SubmitInputs([new(100, 0)])); Ok(fork.Step(10_000_000)); Require(Snapshot(fork) != Snapshot(simulation));
        var values = new Scalar[simulation.Model.OutputCount]; for (int i = 0; i < 10; ++i) { Ok(simulation.Step(1_000_000)); simulation.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 100; ++i) { Ok(simulation.Step(1_000_000)); simulation.ReadSnapshot(values); }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Gas-piston stepping allocated.");
        before = Snapshot(simulation); Require(simulation.Step(20_000_000, [new(before.TimeNanoseconds + 10_000_000, 100, 1e8)]) == SimulationStatus.NumericalFailure && Snapshot(simulation) == before);
    }
    private static void Contracts()
    {
        var definition = Model(); var piston = definition.Components[0];
        foreach (var bad in new[] { piston with { NodeA = 2 }, piston with { NodeB = 1 }, piston with { InputChannel = 101 },
            piston with { GasPiston = Geometry() with { Area = new(1, Unit.Meter) } }, piston with { GasPiston = Geometry() with { ReferenceVolume = new(-1, Unit.CubicMeter) } },
            piston with { GasPiston = Geometry(direction: 0) }, piston with { GasPiston = null } })
            Require(!CompiledModel.TryCompile(definition with { Components = [bad, definition.Components[1], definition.Components[2]] }, out _, out _));
        Require(!CompiledModel.TryCompile(definition with { Components = [..definition.Components, piston with { Id = 14 }] }, out _, out _));
        var invalidStroke = HydraulicModel(100_000); invalidStroke.Components[0] = piston with { GasPiston = Geometry(volume: .000001) };
        Require(!CompiledModel.TryCompile(invalidStroke, out _, out _));
        var model = CompiledModel.Compile(definition); definition.Components[0] = piston with { GasPiston = Geometry(volume: .0002) };
        Require(model.Fingerprint != CompiledModel.Compile(definition).Fingerprint);
        var a = model.CreateSimulation(); var b = CompiledModel.Compile(Model()).CreateSimulation(); Ok(a.Step(20_000_000)); Ok(b.Step(20_000_000)); Require(Snapshot(a) == Snapshot(b));
    }
}
