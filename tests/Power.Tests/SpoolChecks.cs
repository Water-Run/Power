// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class SpoolChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("spool metering / signed travel / passive bidirectional flow / analytic derivatives", Law),
        ("spool pressure feedback / independent three-state RK4 / smooth refinement", Reference),
        ("spool regulator / independent steady pressure root / mechanical equilibrium", Equilibrium),
        ("spool land activation / independent piecewise reference / decreasing error", Activation),
        ("spool finite ports / held actuator / volume and thermal energy", FinitePorts),
        ("spool transactions / late failure / forks / batching / zero allocations", Transactions),
        ("spool contracts / explicit piston / dimensions / stroke / immutable geometry", Contracts)
    ];
    private static void Ok(SimulationStatus status) { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation simulation) => simulation.ReadSnapshot(new Scalar[simulation.Model.OutputCount]);
    internal static ModelDefinition Model(ulong step = 100_000, double closed = 0) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Translational(1, 1, 0, .0001), NodeDefinition.Hydraulic(2, 1e-11, 2e5), NodeDefinition.Thermal(3, 100, 300)],
        Components = [ComponentDefinition.Piston(10, 1, 2, PistonChecks.Definition() with { FrontArea = new(.0001, Unit.SquareMeter) }),
            ComponentDefinition.LinearSpring(11, 1, 0, 2e5, 1000, heat: 3),
            ComponentDefinition.HydraulicResistance(12, 2, 0, 1e-10, 1e6, 100, 1, 3),
            ComponentDefinition.Spool(13, 2, 0, 2e-7, 1000, new() { PistonComponent = 10, ClosedPosition = new(closed, Unit.Meter), FullOpenPosition = new(.001, Unit.Meter) }, heat: 3),
            ComponentDefinition.Force(14, 1, 101, 0)]
    };
    private static void Law()
    {
        foreach (var endpoints in new[] { (.001, .003), (.003, .001) })
        {
            var law = new HydraulicSpoolValve(2e-7, 1000, endpoints.Item1, endpoints.Item2);
            Near(law.Opening(endpoints.Item1), 0, 0); Near(law.Opening(endpoints.Item2), 1, 0); Near(law.Opening(.002), .5, 1e-15);
            foreach (double position in new[] { -.01, .0015, .0025, .01 })
            foreach (double pressure in new[] { 0.0, 100, 1e5, 1e6 })
            {
                Require(law.TryEvaluate(pressure, 2e5, position, out var flow)); Require(flow.HeatFlowWatts >= 0);
                const double dp = .01, dx = 1e-8;
                if (pressure > 0)
                {
                    Require(law.TryEvaluate(pressure + dp, 2e5, position, out var upper)); Require(law.TryEvaluate(pressure - dp, 2e5, position, out var lower));
                    Near(flow.PressureDerivativeCubicMetersPerSecondPascal, (upper.VolumeFlowCubicMetersPerSecond - lower.VolumeFlowCubicMetersPerSecond) / (2 * dp), 1e-15);
                }
                Require(law.TryEvaluate(pressure, 2e5, position + dx, out var right)); Require(law.TryEvaluate(pressure, 2e5, position - dx, out var left));
                Near(flow.PositionDerivativeSquareMetersPerSecond, (right.VolumeFlowCubicMetersPerSecond - left.VolumeFlowCubicMetersPerSecond) / (2 * dx), 1e-9);
                Near(flow.HeatFlowWatts, flow.VolumeFlowCubicMetersPerSecond * (pressure - 2e5), 0);
            }
        }
        var closed = new HydraulicSpoolValve(2e-7, 1000, 0, .001);
        Require(closed.TryEvaluate(1e6, 0, -.001, out var sealedFlow)); Near(sealedFlow.VolumeFlowCubicMetersPerSecond, 0, 0);
        Require(!closed.TryEvaluate(-1, 0, 0, out _) && !closed.TryEvaluate(1e6, 0, double.NaN, out _));
        Throws<ArgumentException>(() => new HydraulicSpoolValve(1, 0, 0, .001));
        Throws<ArgumentException>(() => new HydraulicSpoolValve(1, 100, .001, .001));
    }
    private static double[] Integrate(double closed, double duration)
    {
        double[] x = [.0001, 0, 2e5], a = new double[3], b = new double[3], c = new double[3], d = new double[3], trial = new double[3];
        const double h = .000001;
        void Derivative(double[] state, double[] target)
        {
            double opening = Math.Max(0, Math.Min(1, (state[0] - closed) / (.001 - closed)));
            double flow = 2e-7 * opening * state[2] / Math.Pow(state[2] * state[2] + 1e6, .25);
            target[0] = state[1]; target[1] = .0001 * state[2] - 2e5 * state[0] - 1000 * state[1];
            target[2] = (1e-10 * (1e6 - state[2]) - flow - .0001 * state[1]) / 1e-11;
        }
        for (int tick = 0; tick < (int)Math.Round(duration / h); ++tick)
        {
            Derivative(x, a); for (int i = 0; i < 3; ++i) trial[i] = x[i] + h * a[i] / 2;
            Derivative(trial, b); for (int i = 0; i < 3; ++i) trial[i] = x[i] + h * b[i] / 2;
            Derivative(trial, c); for (int i = 0; i < 3; ++i) trial[i] = x[i] + h * c[i];
            Derivative(trial, d); for (int i = 0; i < 3; ++i) x[i] += h * (a[i] + 2 * b[i] + 2 * c[i] + d[i]) / 6;
        }
        return x;
    }
    private static void Refinement(double closed, bool smooth)
    {
        var reference = Integrate(closed, .08); double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 400_000, 200_000, 100_000 })
        {
            var simulation = CompiledModel.Compile(Model(step, closed)).CreateSimulation(); Ok(simulation.Step(80_000_000));
            double error = Math.Abs(Value(simulation, 2, Field.Pressure) - reference[2]) / 1e6
                + Math.Abs(Value(simulation, 1, Field.Displacement) - reference[0]) / .001 + Math.Abs(Value(simulation, 1, Field.LinearSpeed) - reference[1]);
            Require(smooth ? previous / error > 3.7 : error < previous, $"Spool refinement {previous:R}/{error:R}"); previous = error;
            Near(Value(simulation, 0, Field.HydraulicVolumeResidual), 0, 1e-17); Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8);
            Require(Value(simulation, 13, Field.FluidHeat) > 0);
        }
    }
    private static void Reference() => Refinement(0, true);
    private static void Activation() => Refinement(.0002, false);
    private static void Equilibrium()
    {
        double lower = 0, upper = 1e6;
        for (int i = 0; i < 80; ++i)
        {
            double pressure = .5 * (lower + upper), x = .0001 * pressure / 2e5;
            double discharge = 2e-7 * (x / .001) * pressure / Math.Pow(pressure * pressure + 1e6, .25);
            if (1e-10 * (1e6 - pressure) > discharge) lower = pressure; else upper = pressure;
        }
        var simulation = CompiledModel.Compile(Model()).CreateSimulation(); Ok(simulation.Step(2_000_000_000));
        Near(Value(simulation, 2, Field.Pressure), .5 * (lower + upper), 1e-3);
        Near(Value(simulation, 1, Field.Displacement), .0001 * Value(simulation, 2, Field.Pressure) / 2e5, 1e-12);
        Near(Value(simulation, 13, Field.Opening), Value(simulation, 1, Field.Displacement) / .001, 1e-14);
        Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-7);
    }
    private static void FinitePorts()
    {
        var definition = new ModelDefinition
        {
            StepNanoseconds = 50_000,
            Nodes = [NodeDefinition.Translational(1, 1, 0, .0005), NodeDefinition.Hydraulic(2, 1e-11, 1e6), NodeDefinition.Hydraulic(3, 2e-11), NodeDefinition.Hydraulic(4, 1e-9, 5e5), NodeDefinition.Thermal(5, 100, 300)],
            Components = [ComponentDefinition.Piston(10, 1, 4, PistonChecks.Definition() with { FrontArea = new(.0001, Unit.SquareMeter) }),
                ComponentDefinition.LinearSpring(11, 1, 0, 1e5, 0),
                ComponentDefinition.Spool(12, 2, 3, 2e-7, 1000, new() { PistonComponent = 10, ClosedPosition = new(0, Unit.Meter), FullOpenPosition = new(.001, Unit.Meter) }, heat: 5)]
        };
        var simulation = CompiledModel.Compile(definition).CreateSimulation(); Ok(simulation.Step(1_000_000_000));
        Near(Value(simulation, 1, Field.Displacement), .0005, 1e-15);
        Near(Value(simulation, 2, Field.Pressure), 1e6 / 3, 1e-5); Near(Value(simulation, 3, Field.Pressure), 1e6 / 3, 1e-5);
        Near(Value(simulation, 0, Field.HydraulicVolumeResidual), 0, 1e-17); Near(Value(simulation, 0, Field.HydraulicWork), 0, 0);
        Near(Value(simulation, 12, Field.FluidHeat), 10.0 / 3, 1e-8); Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-7);
    }
    private static void Transactions()
    {
        var simulation = CompiledModel.Compile(Model()).CreateSimulation(); Ok(simulation.Step(10_000_000)); var before = Snapshot(simulation);
        Require(simulation.Step(20_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(simulation) == before);
        Require(simulation.SubmitInputs([new(999, 1)]) == SimulationStatus.UnknownChannel && Snapshot(simulation) == before);
        var fork = simulation.Fork(); Ok(simulation.Step(20_000_000)); for (int i = 0; i < 20; ++i) Ok(fork.Step(1_000_000)); Require(Snapshot(simulation) == Snapshot(fork));
        Ok(fork.SubmitInputs([new(100, .5)])); Ok(fork.Step(10_000_000)); Require(Snapshot(simulation) != Snapshot(fork));
        var values = new Scalar[simulation.Model.OutputCount]; for (int i = 0; i < 10; ++i) { Ok(simulation.Step(1_000_000)); simulation.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 100; ++i) { Ok(simulation.Step(1_000_000)); simulation.ReadSnapshot(values); }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Spool stepping allocated.");
        before = Snapshot(simulation); Require(simulation.Step(20_000_000, [new(before.TimeNanoseconds + 10_000_000, 101, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(simulation) == before);
    }
    private static void Contracts()
    {
        var definition = Model(); var valve = definition.Components[3];
        foreach (var invalid in new[] { valve with { InputChannel = 200 }, valve with { SpoolValve = valve.SpoolValve! with { PistonComponent = 12 } },
            valve with { SpoolValve = valve.SpoolValve! with { FullOpenPosition = new(0, Unit.Meter) } },
            valve with { SpoolValve = valve.SpoolValve! with { FullOpenPosition = new(2, Unit.Meter) } },
            valve with { SpoolValve = valve.SpoolValve! with { ClosedPosition = new(0, Unit.Radian) } },
            valve with { NodeA = 1 }, valve with { SpoolValve = null } })
            Require(!CompiledModel.TryCompile(definition with { Components = [definition.Components[0], definition.Components[1], definition.Components[2], invalid] }, out _, out _));
        var model = CompiledModel.Compile(definition); var changed = definition with { Components = [..definition.Components[..3], valve with { SpoolValve = valve.SpoolValve! with { ClosedPosition = new(-.001, Unit.Meter) } }, definition.Components[4]] };
        Require(model.Fingerprint != CompiledModel.Compile(changed).Fingerprint);
        definition.Components[3] = changed.Components[3]; var original = model.CreateSimulation(); var replay = CompiledModel.Compile(Model()).CreateSimulation();
        Ok(original.Step(20_000_000)); Ok(replay.Step(20_000_000)); Require(Snapshot(original) == Snapshot(replay));
    }
}
