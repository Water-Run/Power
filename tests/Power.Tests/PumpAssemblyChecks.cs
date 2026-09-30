// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class PumpAssemblyChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("pump assembly / signed flow and power / passive losses / finite ranges", Laws),
        ("pump assembly / analytic damped oscillation / second-order convergence / heat", Oscillation),
        ("pump assembly / finite inlet / reverse operation / reservoir work", Ports),
        ("electric pump / independent RL-mechanical-pressure ODE / equilibrium", Electric),
        ("pump assembly / rollback / cancellation / branches / zero allocations", Transactions),
        ("pump assembly / immutable graph expansion / portable units and topology", Assets)
    ];
    private static void Ok(SimulationStatus status)
    { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation simulation) => simulation.ReadSnapshot(new Scalar[simulation.Model.OutputCount]);
    private static readonly HydraulicPumpAssembly Assembly = new(1e-6, 1e-12, .02);
    internal static ModelDefinition Model(ulong step = 1_000_000, bool finiteInlet = false) => new()
    {
        StepNanoseconds = step,
        Nodes = finiteInlet
            ? [NodeDefinition.Rotor(1, .2, -100), NodeDefinition.Hydraulic(2, 2e-12, 1e6), NodeDefinition.Thermal(3, 100, 300), NodeDefinition.Hydraulic(4, 3e-12, 2e6)]
            : [NodeDefinition.Rotor(1, .2, 100), NodeDefinition.Hydraulic(2, 2e-12, 1e6), NodeDefinition.Thermal(3, 100, 300)],
        Components = Assembly.CreateComponents(10, 11, 12, 1, 2, inlet: finiteInlet ? 4u : 0u, heat: 3).ToArray()
    };
    private static void Laws()
    {
        foreach (double speed in new[] { -100.0, 0, 100 })
        foreach (double inlet in new[] { 0.0, 1e6, 2e6 })
        foreach (double outlet in new[] { 0.0, 1e6, 2e6 })
        {
            Require(Assembly.TryEvaluate(speed, inlet, outlet, out var reaction));
            double pressure = outlet - inlet;
            Near(reaction.NetVolumeFlowCubicMetersPerSecond, 1e-6 * speed - 1e-12 * pressure, 1e-18);
            Near(reaction.ShaftTorqueNewtonMeters, -1e-6 * pressure - .02 * speed, 1e-12);
            Near(reaction.ShaftPowerWatts - reaction.HydraulicPowerWatts,
                reaction.LeakageHeatWatts + reaction.FrictionHeatWatts, 1e-10);
            Require(reaction.LeakageHeatWatts >= 0 && reaction.FrictionHeatWatts >= 0);
        }
        var ideal = new HydraulicPumpAssembly(1e-6, 0, 0);
        Require(ideal.TryEvaluate(10, 0, 1e6, out var r)); Near(r.HydraulicPowerWatts, r.ShaftPowerWatts, 0);
        Require(!Assembly.TryEvaluate(double.NaN, 0, 1, out r) && r == default);
        Require(!Assembly.TryEvaluate(1, -1, 1, out r) && r == default);
        Require(!new HydraulicPumpAssembly(double.MaxValue, 1, 1).TryEvaluate(10, 0, 10, out r) && r == default);
        Throws<ArgumentException>(() => new HydraulicPumpAssembly(0, 0, 0));
        Throws<ArgumentException>(() => new HydraulicPumpAssembly(1, -1, 0));
        Throws<ArgumentException>(() => new HydraulicPumpAssembly(1, 0, double.PositiveInfinity));
    }
    private static void Oscillation()
    {
        const double time = .256, frequency = 1.5684387141358123;
        double expectedSpeed = Math.Exp(-.3 * time) * (100 * Math.Cos(frequency * time) + (20 - 5) / frequency * Math.Sin(frequency * time));
        double expectedPressure = Math.Exp(-.3 * time) * (1e6 * Math.Cos(frequency * time) + (50e6 - .2e6) / frequency * Math.Sin(frequency * time));
        double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 16_000_000, 8_000_000, 4_000_000 })
        {
            var simulation = CompiledModel.Compile(Model(step)).CreateSimulation(); Ok(simulation.Step(256_000_000));
            double speed = Value(simulation, 1, Field.Speed), pressure = Value(simulation, 2, Field.Pressure);
            double error = Math.Abs(speed - expectedSpeed) + 1e-5 * Math.Abs(pressure - expectedPressure);
            Require(previous / error > 3.8, $"Damped pump refinement {previous:R}/{error:R}"); previous = error;
            double losses = .5 * .2 * (10000 - speed * speed) + .5 * 2e-12 * (1e12 - pressure * pressure);
            Near(100 * (Value(simulation, 3, Field.Temperature) - 300), losses, 1e-8);
            Require(losses > Value(simulation, 11, Field.FluidHeat) && Value(simulation, 11, Field.FluidHeat) > 0);
            Near(Value(simulation, 10, Field.HydraulicWork), .5 * 2e-12 * (pressure * pressure - 1e12) + Value(simulation, 11, Field.FluidHeat), 1e-8);
            Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8);
            Near(Value(simulation, 0, Field.HydraulicVolumeResidual), 0, 1e-17);
        }
    }
    private static void Ports()
    {
        var closed = CompiledModel.Compile(Model(finiteInlet: true)).CreateSimulation(); Ok(closed.Step(10_000_000));
        Near(2e-12 * Value(closed, 2, Field.Pressure) + 3e-12 * Value(closed, 4, Field.Pressure), 8e-6, 1e-18);
        Require(Value(closed, 10, Field.VolumeFlow) < 0 && Value(closed, 10, Field.HydraulicWork) > 0);
        Require(Value(closed, 11, Field.FluidHeat) > 0); Near(Value(closed, 0, Field.HydraulicWork), 0, 0);
        Near(Value(closed, 0, Field.EnergyResidual), 0, 1e-8);
        var definition = Model() with { Components = Assembly.CreateComponents(10, 11, 12, 1, 2, reservoirPressurePascals: 5e5, heat: 3).ToArray() };
        var open = CompiledModel.Compile(definition).CreateSimulation(); Ok(open.Step(100_000_000));
        Near(Value(open, 0, Field.HydraulicWork), 5e5 * Value(open, 0, Field.HydraulicVolumeIn), 1e-10);
        Near(Value(open, 0, Field.EnergyResidual), 0, 1e-8);
    }
    private static void Electric()
    {
        var pump = new HydraulicPumpAssembly(1e-6, 1e-10, .01);
        var definition = new ModelDefinition
        {
            StepNanoseconds = 500_000,
            Nodes = [NodeDefinition.Rotor(1, .02), NodeDefinition.Hydraulic(2, 2e-11), NodeDefinition.Thermal(3, 100, 300)],
            Components = [..pump.CreateComponents(10, 11, 12, 1, 2, heat: 3), ComponentDefinition.Motor(13, 1, 3, 100, 1, .1, .2, 12)]
        };
        // Independent RK4 integration of J*w' = k*i - D*p - B*w; C*p' = D*w - G*p; L*i' = V-R*i-k*w.
        double[] Derivative(double[] x) => [(.2 * x[2] - 1e-6 * x[1] - .01 * x[0]) / .02,
            (1e-6 * x[0] - 1e-10 * x[1]) / 2e-11, (12 - x[2] - .2 * x[0]) / .1];
        double[] reference = [0, 0, 0]; const double h = .000025;
        for (int tick = 0; tick < 16000; ++tick)
        {
            var a = Derivative(reference); var b = Derivative(reference.Select((v, i) => v + h * a[i] / 2).ToArray());
            var c = Derivative(reference.Select((v, i) => v + h * b[i] / 2).ToArray());
            var d = Derivative(reference.Select((v, i) => v + h * c[i]).ToArray());
            for (int i = 0; i < 3; ++i) reference[i] += h * (a[i] + 2 * b[i] + 2 * c[i] + d[i]) / 6;
        }
        double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 2_000_000, 1_000_000, 500_000 })
        {
            var simulation = CompiledModel.Compile(definition with { StepNanoseconds = step }).CreateSimulation(); Ok(simulation.Step(400_000_000));
            double error = Math.Abs(Value(simulation, 1, Field.Speed) - reference[0]) + 1e-4 * Math.Abs(Value(simulation, 2, Field.Pressure) - reference[1]) + Math.Abs(Value(simulation, 13, Field.Current) - reference[2]);
            Require(previous / error > 3.8, $"Electric pump refinement {previous:R}/{error:R}"); previous = error;
            Near(Value(simulation, 0, Field.HydraulicWork), 0, 0); Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8);
        }
        var steady = CompiledModel.Compile(definition).CreateSimulation(); Ok(steady.Step(20_000_000_000));
        double speed = .2 * 12 / (.01 + .2 * .2 + 1e-6 * 1e-6 / 1e-10);
        Near(Value(steady, 1, Field.Speed), speed, 1e-5); Near(Value(steady, 2, Field.Pressure), 1e-6 * speed / 1e-10, .1);
        Near(Value(steady, 13, Field.Current), 12 - .2 * speed, 1e-5);
    }
    private static void Transactions()
    {
        var definition = Model() with { Components = [..Model().Components, ComponentDefinition.Torque(13, 1, 100, 0)] };
        var simulation = CompiledModel.Compile(definition).CreateSimulation(); Ok(simulation.Step(10_000_000)); var before = Snapshot(simulation);
        Require(simulation.Step(100_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(simulation) == before);
        Require(simulation.Step(100_000_000, [new(50_000_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(simulation) == before);
        var fork = simulation.Fork(); Ok(simulation.Step(100_000_000)); for (int i = 0; i < 100; ++i) Ok(fork.Step(1_000_000)); Require(Snapshot(simulation) == Snapshot(fork));
        Ok(fork.SubmitInputs([new(100, 2)])); Ok(fork.Step(1_000_000)); Require(Snapshot(simulation) != Snapshot(fork));
        var buffer = new Scalar[simulation.Model.OutputCount]; for (int i = 0; i < 5; ++i) { Ok(simulation.Step(1_000_000)); simulation.ReadSnapshot(buffer); }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; ++i) { Ok(simulation.Step(1_000_000)); simulation.ReadSnapshot(buffer); }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Lossy pump stepping allocated.");
    }
    private static void Assets()
    {
        var components = Assembly.CreateComponents(10, 11, 12, 1, 2, heat: 3);
        Throws<NotSupportedException>(() => ((IList<ComponentDefinition>)components)[0] = ComponentDefinition.Torque(10, 1, 100, 0));
        Throws<ArgumentException>(() => Assembly.CreateComponents(10, 10, 12, 1, 2));
        Throws<ArgumentException>(() => Assembly.CreateComponents(0, 11, 12, 1, 2));
        Throws<ArgumentException>(() => Assembly.CreateComponents(10, 11, 12, 1, 2, 4, 1));
        var asset = PowerAsset.Create(Model(), "Lossy pump", new string('a', 64), 100_000_000, 10_000_000, [], []);
        var decoded = AssetCodec.Decode(AssetCodec.Encode(asset)); Require(asset.Components.SequenceEqual(decoded.Components));
        var a = asset.CreatePlayback(); var b = decoded.CreatePlayback(); var left = new Scalar[a.Model.OutputCount]; var right = new Scalar[b.Model.OutputCount];
        while (!a.Completed)
        {
            Ok(a.Advance(10_000_000)); for (int i = 0; i < 10; ++i) Ok(b.Advance(1_000_000));
            Require(a.ReadSnapshot(left) == b.ReadSnapshot(right) && left.SequenceEqual(right));
        }
    }
}
