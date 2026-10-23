// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class FuelPipeChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel pipe / own liquid caloric chemical and kinetic inventories / finite wall", Inventories),
        ("fuel pipe / independent finite-compliance harmonic momentum / first-order refinement", Harmonic),
        ("fuel pipe / independent nonlinear momentum and three-temperature RK4 / refinement", Reference),
        ("fuel pipe / physical reversal gross directional volumes and pair entropy", Reversal),
        ("fuel pipe / complete forks cancellation late rollback batching and zero allocations", Transactions),
        ("fuel pipe / speculative clutch interval histories / complete batching", Clutch),
        ("fuel pipe / liquid-only material geometry and heat-port contracts", Contracts)
    ];
    internal static EquilibriumFuelDefinition Material => EquilibriumFuelNetworkChecks.Material with { Compliance = new(5e-12, Unit.CubicMeterPerPascal) };
    internal static FuelInertialPipeDefinition Definition(double linear = 1e10, double quadratic = 1e16, double fraction = .5) => new()
    {
        Length = new(.1, Unit.Meter), Area = new(1e-8, Unit.SquareMeter), LinearResistance = new(linear, Unit.PascalSecondPerCubicMeter),
        QuadraticResistance = new(quadratic, Unit.PascalSecondSquaredPerCubicMeterSquared), InitialFlow = new(0, Unit.CubicMeterPerSecond),
        InitialTemperature = new(310, Unit.Kelvin), FluidHeatFraction = new(fraction, Unit.Fraction)
    };
    internal static ModelDefinition Model(ulong tick = 100_000, double linear = 1e10, double quadratic = 1e16, double fraction = .5, bool sameTemperature = false) => new()
    {
        StepNanoseconds = tick,
        Nodes = [NodeDefinition.FuelVolume(1, 1e-5, 750 * (1e-5 + 5e-12 * 6e5), sameTemperature ? 300 : 320, Material),
            NodeDefinition.FuelVolume(2, 1e-5, 750 * (1e-5 + 5e-12 * 4e5), 300, Material), NodeDefinition.Thermal(3, 1, 300)],
        Components = [ComponentDefinition.InertialFuelPipe(10, 1, 2, 3, Definition(linear, quadratic, fraction) with { InitialTemperature = new(sameTemperature ? 300 : 310, Unit.Kelvin) })]
    };
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Step(Simulation s, ulong ns)
    { if (s.Step(ns) != SimulationStatus.Ok) throw new InvalidOperationException("Inertial pipe step failed at " + Snapshot(s).TimeNanoseconds); }
    private static void Balance(Simulation s) { Near(Value(s, 0, Field.EnergyResidual), 0, 3e-7); Near(Value(s, 0, Field.MassResidual), 0, 1e-14); Near(Value(s, 0, Field.FuelResidual), 0, 1e-14); }
    private static void Inventories()
    {
        foreach (double fraction in new[] { 0d, .5, 1d })
        {
            var s = CompiledModel.Compile(Model(fraction: fraction)).CreateSimulation();
            double energyA = Value(s, 1, Field.InternalEnergy), energyB = Value(s, 2, Field.InternalEnergy), energyLine = Value(s, 10, Field.InternalEnergy), sourceMass = Value(s, 1, Field.Mass);
            Near(Value(s, 10, Field.Mass), 7.5e-7, 1e-21); Near(Value(s, 10, Field.ChemicalEnergy), 7.5e-7 * 44e6, 1e-12);
            Step(s, 20_000_000); Balance(s);
            Near(energyA - Value(s, 1, Field.InternalEnergy), Value(s, 10, Field.EnergyAtA), 2e-11);
            Near(Value(s, 2, Field.InternalEnergy) - energyB, Value(s, 10, Field.EnergyAtB), 2e-11);
            Near(Value(s, 10, Field.InternalEnergy) - energyLine, Value(s, 10, Field.EnergyAtA) - Value(s, 10, Field.EnergyAtB) - Value(s, 10, Field.HydraulicWork) + Value(s, 10, Field.FluidHeat), 2e-11);
            Near(Value(s, 10, Field.KineticEnergy), Value(s, 10, Field.HydraulicWork) - Value(s, 10, Field.FrictionHeat), 2e-11);
            Near(Value(s, 10, Field.FluidHeat), fraction * Value(s, 10, Field.FrictionHeat), 2e-12);
            Near(Value(s, 3, Field.Temperature) - 300, Value(s, 10, Field.HeatRejected), 2e-12);
            Near(sourceMass - Value(s, 1, Field.Mass), Value(s, 10, Field.TotalFuelDelivered), 1e-16);
            Require(Value(s, 10, Field.EntropyProduced) >= 0 && Value(s, 10, Field.Temperature) > 310);
        }
    }
    private static void Harmonic()
    {
        const double inertia = 7.5e9, slope = 4e11, duration = .1; double omega = Math.Sqrt(slope / inertia);
        double exact = 2e5 / (inertia * omega) * Math.Sin(omega * duration); var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 2_000_000, 1_000_000, 500_000, 250_000 })
        {
            var s = CompiledModel.Compile(Model(tick, 0, 0, sameTemperature: true)).CreateSimulation(); Step(s, 100_000_000); Balance(s);
            errors.Add(Math.Abs(Value(s, 10, Field.VolumeFlow) - exact) / exact);
            Require(Value(s, 10, Field.FrictionHeat) == 0 && Value(s, 10, Field.EntropyProduced) > 0);
        }
        Console.WriteLine("Fuel pipe finite-compliance harmonic normalized errors: " + string.Join(", ", errors.Select(x => x.ToString("R"))));
        for (int n = 1; n < errors.Count; ++n) Require(errors[n - 1] / errors[n] is > 1.8 and < 2.2);
    }
    private static void Reference()
    {
        double[] Integrate(int count)
        {
            double[] x = [6e5, 4e5, 0, 320, 300, 310, 300, 0], a = new double[8], b = new double[8], c = new double[8], d = new double[8], stage = new double[8]; double h = .02 / count;
            void Rate(double[] y, double[] rate)
            {
                double q = y[2], mass = 750 * Math.Abs(q), friction = 1e10 * q * q + 1e16 * Math.Abs(q * q * q);
                rate[0] = -q / 5e-12; rate[1] = q / 5e-12; rate[2] = (y[0] - y[1] - 1e10 * q - 1e16 * q * Math.Abs(q)) / 7.5e9;
                rate[3] = q >= 0 ? 0 : mass / (750 * (1e-5 + 5e-12 * y[0])) * (y[5] - y[3]);
                rate[4] = q <= 0 ? 0 : mass / (750 * (1e-5 + 5e-12 * y[1])) * (y[5] - y[4]);
                rate[5] = mass / 7.5e-7 * ((q >= 0 ? y[3] : y[4]) - y[5]) + .5 * friction / (7.5e-7 * 2000);
                rate[6] = .5 * friction; rate[7] = friction;
            }
            for (int n = 0; n < count; ++n)
            {
                Rate(x, a); for (int j = 0; j < 8; ++j) stage[j] = x[j] + h * a[j] / 2; Rate(stage, b);
                for (int j = 0; j < 8; ++j) stage[j] = x[j] + h * b[j] / 2; Rate(stage, c);
                for (int j = 0; j < 8; ++j) stage[j] = x[j] + h * c[j]; Rate(stage, d);
                for (int j = 0; j < 8; ++j) x[j] += h / 6 * (a[j] + 2 * b[j] + 2 * c[j] + d[j]);
            }
            return x;
        }
        var reference = Integrate(10000); var finer = Integrate(20000); for (int j = 0; j < 8; ++j) Near(reference[j], finer[j], j < 2 ? 1e-7 : 1e-10);
        var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 200_000, 100_000, 50_000, 25_000 })
        {
            var s = CompiledModel.Compile(Model(tick)).CreateSimulation(); Step(s, 20_000_000); Balance(s);
            double[] values = [Value(s, 1, Field.Pressure), Value(s, 2, Field.Pressure), Value(s, 10, Field.VolumeFlow), Value(s, 1, Field.Temperature), Value(s, 2, Field.Temperature), Value(s, 10, Field.Temperature), Value(s, 3, Field.Temperature), Value(s, 10, Field.FrictionHeat)];
            double error = 0; for (int j = 0; j < 8; ++j) error = Math.Max(error, Math.Abs(values[j] - reference[j]) / (j < 2 ? 6e5 : j == 2 ? 1e-6 : j == 7 ? .01 : 320)); errors.Add(error);
        }
        Console.WriteLine("Fuel pipe nonlinear thermal RK4 normalized errors: " + string.Join(", ", errors.Select(x => x.ToString("R"))));
        for (int n = 1; n < errors.Count; ++n) Require(errors[n - 1] / errors[n] is > 1.7 and < 2.3);
    }
    private static void Reversal()
    {
        var s = CompiledModel.Compile(Model(100_000, 0, 0, sameTemperature: true)).CreateSimulation();
        double entropy = Value(s, 1, Field.Entropy) + Value(s, 2, Field.Entropy);
        Step(s, 600_000_000); Balance(s);
        Require(Value(s, 10, Field.ForwardVolume) > 0 && Value(s, 10, Field.ReverseVolume) > 0 && Value(s, 10, Field.VolumeFlow) < 0);
        double lineT = Value(s, 10, Field.Temperature), totalEntropy = Value(s, 1, Field.Entropy) + Value(s, 2, Field.Entropy) + 7.5e-7 * 2000 * Math.Log(lineT / 300);
        Near(totalEntropy - entropy, Value(s, 10, Field.EntropyProduced), 2e-10);
        Near(Value(s, 10, Field.FrictionHeat), 0, 0);
    }
    private static void Transactions()
    {
        var definition = Model(); definition = definition with { Nodes = [..definition.Nodes, NodeDefinition.Rotor(4, .1)], Components = [..definition.Components, ComponentDefinition.Torque(20, 4, 100, 0)] };
        var s = CompiledModel.Compile(definition).CreateSimulation(); var replay = s.Fork(); Step(s, 10_000_000); for (int i = 0; i < 100; ++i) Step(replay, 100_000); Require(Snapshot(s) == Snapshot(replay));
        var before = Snapshot(s); Require(s.Step(1_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.Step(1_000_000, [new(before.TimeNanoseconds + 500_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var branch = s.Fork(); Step(branch, 1_000_000); Require(Snapshot(s) == before);
        var values = new Scalar[s.Model.OutputCount]; for (int i = 0; i < 20; ++i) { Step(s, 100_000); s.ReadSnapshot(values); }
        long allocation = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 20; ++i) { Step(s, 100_000); s.ReadSnapshot(values); }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocation);
    }
    private static void Clutch()
    {
        var definition = Model(); definition = definition with { Nodes = [..definition.Nodes, NodeDefinition.Rotor(4, .1, .01), NodeDefinition.Rotor(5, .1)],
            Components = [..definition.Components, ComponentDefinition.Clutch(20, 4, 5, 1, 1, 100, 1, heat: 3)] };
        var s = CompiledModel.Compile(definition).CreateSimulation(); var replay = s.Fork(); Step(s, 2_000_000); for (int i = 0; i < 20; ++i) Step(replay, 100_000); Require(Snapshot(s) == Snapshot(replay)); Balance(s);
    }
    private static void Contracts()
    {
        var definition = Model();
        var bad = definition with { Components = [definition.Components[0] with { FuelPipe = Definition() with { Length = new(.1, Unit.Kelvin) } }] };
        Require(!CompiledModel.TryCompile(bad, out _, out var diagnostic) && diagnostic!.Code == DiagnosticCode.Unit);
        bad = definition with { Components = [definition.Components[0] with { HeatNode = 0 }] }; Require(!CompiledModel.TryCompile(bad, out _, out _));
        bad = definition with { Nodes = definition.Nodes.Select(n => n.Id == 1 ? n with { Fuel = Material with { LiquidSpecificHeat = new(2100, Unit.JoulePerKilogramKelvin) } } : n).ToArray() }; Require(!CompiledModel.TryCompile(bad, out _, out _));
        bad = definition with { Nodes = definition.Nodes.Select(n => n.Id == 1 ? n with { Position = new(.0001, Unit.Kilogram) } : n).ToArray() }; Require(!CompiledModel.TryCompile(bad, out _, out _));
        var unresolved = CompiledModel.Compile(Model(200_000_000)).CreateSimulation(); var before = Snapshot(unresolved); Require(unresolved.Step(200_000_000) == SimulationStatus.NumericalFailure && Snapshot(unresolved) == before);
    }
}
