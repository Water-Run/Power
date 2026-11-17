// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class TankHeadspaceChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("tank headspace / independent adiabatic work and pressure derivative", Law),
        ("tank headspace / coupled closed expansion compression and internal pressure work", Closed),
        ("tank headspace / relief compression heat fractions and complete ledgers", Relief),
        ("tank headspace / independent simultaneous shaft rail gas ODE refinement", Reference),
        ("tank headspace / finite vent transport and independent valve branches", Vent),
        ("tank headspace / complete rollback cancellation forks allocations and ownership", Transactions)
    ];

    internal static ModelDefinition Model(ulong tick = 50_000, double speed = 100, bool relief = false, double ventArea = 0, double fraction = 1)
    {
        var source = relief ? LiquidRailReturnChecks.Model(fraction, tick, speed: speed)
            : LiquidFuelTankChecks.Model(mass: .0001, speed: speed, tick: tick);
        var components = source.Components.Select(c => c.Id == 20 ? ComponentDefinition.DisplacementPump(20, 4, 5, 1e-9)
            : c.Id == 22 ? c with { LiquidFuelTank = c.LiquidFuelTank! with { Headspace = new() { Capacity = new(.0001 / 750 + 1e-8, Unit.CubicMeter), GasNode = 40 } } }
            : c.Id == 30 ? ComponentDefinition.PressureRelief(30, 5, 0, 1e-12, 200_000) : c).ToArray();
        return source with { Nodes = [..source.Nodes, NodeDefinition.CylinderGas(40, 100_000, 300)],
            Components = ventArea == 0 ? components : [..components, ComponentDefinition.GasReservoir(41, 40, ventArea, 100_000, 300, channel: 110)] };
    }

    private static void Step(Simulation s, ulong time) => Require(s.Step(time) == SimulationStatus.Ok, "Headspace step failed.");
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Balance(Simulation s)
    {
        Near(Value(s, 0, Field.EnergyResidual), 0, 2e-7); Near(Value(s, 0, Field.MassResidual), 0, 1e-14);
        Near(Value(s, 0, Field.FuelResidual), 0, 1e-14); Near(Value(s, 0, Field.HydraulicVolumeResidual), 0, 1e-16);
        Near(Value(s, 0, Field.HydraulicWork), 0, 0);
    }

    private static void Law()
    {
        foreach (double gamma in new[] { 1.01, 1.4, 5.0 })
            foreach (double fraction in new[] { -.5, -.001, 0, 1e-14, .001, .4 })
            {
                const double volume = 2e-5, energy = 10;
                double change = fraction * volume;
                Require(LiquidTankHeadspace.TryPressureWork(volume, change, energy, gamma, out var work));
                double expected = energy * (Math.Pow(1 + fraction, 1 - gamma) - 1);
                Near(work.GasEnergyChangeJoules, expected, 1e-12);
                Near(work.MeanPressurePascals * change + work.GasEnergyChangeJoules, 0, 1e-13);
                double epsilon = 1e-7 * volume;
                Require(LiquidTankHeadspace.TryPressureWork(volume, change + epsilon, energy, gamma, out var high));
                Require(LiquidTankHeadspace.TryPressureWork(volume, change - epsilon, energy, gamma, out var low));
                Near(work.PressureDerivativePascalsPerCubicMeter / 1e11,
                    (high.MeanPressurePascals - low.MeanPressurePascals) / (2 * epsilon) / 1e11, 1e-6);
            }
        Require(!LiquidTankHeadspace.TryPressureWork(1, -1, 10, 1.4, out _));
        Require(!LiquidTankHeadspace.TryPressureWork(0, 1, 10, 1.4, out _));
    }

    private static void Closed()
    {
        foreach (double speed in new[] { -100d, 100d })
        {
            var s = CompiledModel.Compile(Model(speed: speed)).CreateSimulation();
            double initialEnergy = Value(s, 40, Field.InternalEnergy), initialVolume = Value(s, 40, Field.Volume);
            Step(s, 20_000_000);
            double volume = Value(s, 40, Field.Volume), expected = initialEnergy * Math.Pow(initialVolume / volume, .4);
            Near(Value(s, 40, Field.InternalEnergy), expected, 1e-13);
            Near(Value(s, 40, Field.Pressure), 100_000 * Math.Pow(initialVolume / volume, 1.4), 1e-6);
            Near(Value(s, 22, Field.HydraulicWork), initialEnergy - expected, 1e-13);
            Near(Value(s, 22, Field.Volume) + volume, .0001 / 750 + 1e-8, 1e-21);
            Near(Value(s, 22, Field.Pressure), Value(s, 40, Field.Pressure), 0);
            Require(speed > 0 ? volume > initialVolume : volume < initialVolume); Balance(s);
        }
    }

    private static void Relief()
    {
        foreach (double fraction in new[] { 0d, .5, 1d })
        {
            var d = Model(speed: 0, relief: true, fraction: fraction);
            var s = CompiledModel.Compile(d with { Components = [..d.Components, ComponentDefinition.Clutch(32, 4, 0, 10, 8)] }).CreateSimulation();
            double oldGas = Value(s, 40, Field.InternalEnergy); Step(s, 10_000_000);
            double p = Value(s, 5, Field.Pressure), gasChange = Value(s, 40, Field.InternalEnergy) - oldGas;
            double loss = .5 * 5e-13 * (800_000 * 800_000d - p * p) - gasChange;
            Require(gasChange > 0 && p < 800_000); Near(Value(s, 30, Field.FluidHeat), loss, 1e-10);
            Near(Value(s, 31, Field.FluidHeat), fraction * loss, 1e-10);
            Near(Value(s, 22, Field.HydraulicWork), -gasChange, 1e-13); Balance(s);
        }
    }

    private static void Reference()
    {
        // Independent continuous shaft/rail/headspace equations, integrated with RK4.
        const double time = .02, h = 1e-6, gamma = 1.4, compliance = 5e-13, displacement = 1e-9;
        double[] state = [100, 800_000, 100_000 * 1e-8 / (gamma - 1)];
        double[] Rates(double[] q)
        {
            double volume = 1e-8 + compliance * (q[1] - 800_000);
            double pressure = (gamma - 1) * q[2] / volume;
            double rate = displacement * q[0] - 1e-12 * Math.Max(0, q[1] - pressure - 200_000);
            return [-displacement * (q[1] - pressure) / .02, rate / compliance, -pressure * rate];
        }
        for (int step = 0; step < (int)Math.Round(time / h); ++step)
        {
            var a = Rates(state); var b = Rates(state.Select((v, i) => v + h * .5 * a[i]).ToArray());
            var c = Rates(state.Select((v, i) => v + h * .5 * b[i]).ToArray()); var d = Rates(state.Select((v, i) => v + h * c[i]).ToArray());
            for (int i = 0; i < state.Length; ++i) state[i] += h * (a[i] + 2 * b[i] + 2 * c[i] + d[i]) / 6;
        }
        double previous = double.PositiveInfinity;
        foreach (ulong tick in new ulong[] { 1_000_000, 500_000, 250_000 })
        {
            var s = CompiledModel.Compile(Model(tick, relief: true)).CreateSimulation(); Step(s, 20_000_000);
            double error = Math.Abs(Value(s, 5, Field.Pressure) - state[1]) / 1e6 + Math.Abs(Value(s, 40, Field.InternalEnergy) - state[2]);
            Require(error < previous && (double.IsPositiveInfinity(previous) || previous / error > 3.5), "Expected second-order headspace refinement.");
            previous = error; Balance(s);
        }
        Require(previous < 1e-5);
    }

    private static void Vent()
    {
        var open = CompiledModel.Compile(Model(ventArea: 2e-8)).CreateSimulation(); var closed = open.Fork();
        Require(closed.SubmitInputs([new(110, 0)]) == SimulationStatus.Ok);
        double gas = Value(open, 40, Field.Mass); Step(open, 30_000_000); Step(closed, 30_000_000);
        Require(Value(open, 40, Field.Mass) > gas && Value(open, 40, Field.Pressure) > Value(closed, 40, Field.Pressure));
        Require(Value(open, 0, Field.ReservoirEnthalpy) > 0); Near(Value(closed, 40, Field.Mass), gas, 0);
        Balance(open); Balance(closed);
    }

    private static void Transactions()
    {
        var model = Model(ventArea: 2e-8, relief: true); var s = CompiledModel.Compile(model).CreateSimulation(); var before = Snapshot(s);
        Require(s.Step(10_000_000, new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.Step(10_000_000, [new(5_000_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var fork = s.Fork(); Step(s, 10_000_000); for (int i = 0; i < 10; ++i) Step(fork, 1_000_000); Require(Snapshot(s) == Snapshot(fork));
        var values = new Scalar[s.Model.OutputCount]; for (int i = 0; i < 10; ++i) { Step(s, 50_000); s.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; ++i) { Step(s, 50_000); s.ReadSnapshot(values); }
        Require(allocated == GC.GetAllocatedBytesForCurrentThread());
        var nodes = model.Nodes.ToArray(); nodes[^1] = nodes[^1] with { Storage = new(1e-8, Unit.CubicMeter) };
        Throws<ModelCompileException>(() => CompiledModel.Compile(model with { Nodes = nodes }));
        var components = model.Components.ToArray(); int tank = Array.FindIndex(components, c => c.Id == 22);
        components[tank] = components[tank] with { LiquidFuelTank = components[tank].LiquidFuelTank! with { Headspace = new() { GasNode = 40, Capacity = new(1e-9, Unit.CubicMeter) } } };
        Throws<ModelCompileException>(() => CompiledModel.Compile(model with { Components = components }));
    }
}
