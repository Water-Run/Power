// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class FuelChamberChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel chamber / sinusoidal volume / closed phase work and coupled shaft energy", Closed),
        ("fuel chamber / finite inlet outlet check valves / starvation vapor and full ledgers", Pump),
        ("fuel chamber / independent simultaneous liquid shaft-pressure ODE / second-order refinement", LiquidReference),
        ("fuel chamber / independent phase-changing filling delivery and shaft ODE / refinement", PumpReference),
        ("fuel chamber / hydraulic gear and speculative clutch composition / complete replay", Composition),
        ("fuel chamber / valve forks cancellation late rollback and zero allocations", Transactions),
        ("fuel chamber / static ideal check valve closes against reverse pressure", CheckValve),
        ("fuel chamber / ownership geometry units and immutable models", Contracts)
    ];
    private static SnapshotInfo Snapshot(Simulation simulation) => simulation.ReadSnapshot(new Scalar[simulation.Model.OutputCount]);
    private static void Step(Simulation simulation, ulong time) => Require(simulation.Step(time) == SimulationStatus.Ok, "Fuel chamber step failed.");
    private static void Balance(Simulation simulation)
    { Near(Value(simulation, 0, Field.EnergyResidual), 0, 3e-6); Near(Value(simulation, 0, Field.MassResidual), 0, 1e-14); Near(Value(simulation, 0, Field.FuelResidual), 0, 1e-14); }
    internal static FuelChamberDefinition Geometry => new() { MinimumReferenceVolume = new(1e-7, Unit.CubicMeter), SweptVolume = new(2e-7, Unit.CubicMeter), Phase = new(0, Unit.Radian) };
    internal static ModelDefinition Model(ulong tick = 50_000, bool flow = true, double inlet = 1e-11)
    {
        var material = EquilibriumFuelNetworkChecks.Material;
        var chamber = material with { Compliance = new(5e-14, Unit.CubicMeterPerPascal) };
        NodeDefinition[] nodes = [NodeDefinition.FuelVolume(1, 1e-4, 750 * (1e-4 + 5e-10 * 1e5), 300, material with { Compliance = new(5e-10, Unit.CubicMeterPerPascal) }),
            NodeDefinition.FuelVolume(2, 1e-7, 750 * (1e-7 + 5e-14 * 1e5), 300, chamber),
            NodeDefinition.FuelVolume(3, 8e-7, 750 * (8e-7 + 5e-12 * 3e5), 300, material with { Compliance = new(5e-12, Unit.CubicMeterPerPascal) }),
            NodeDefinition.Rotor(4, .001, 100)];
        ComponentDefinition[] components = flow ? [ComponentDefinition.FuelCheckValve(10, 1, 2, inlet, 100), ComponentDefinition.FuelCheckValve(11, 2, 3, 1e-9),
            ComponentDefinition.FuelChamberPump(12, 4, 2, Geometry)] : [ComponentDefinition.FuelChamberPump(12, 4, 2, Geometry)];
        return new() { StepNanoseconds = tick, Nodes = nodes, Components = components };
    }
    private static void Closed()
    {
        var geometry = new SinusoidalFuelChamber(1e-7, 2e-7, 0);
        Near(geometry.ReferenceVolumeAt(0), 1e-7, 0); Near(geometry.ReferenceVolumeAt(Math.PI), 3e-7, 1e-22);
        Near(geometry.VolumeDerivativeAt(.4), (geometry.ReferenceVolumeAt(.400001) - geometry.ReferenceVolumeAt(.399999)) / .000002, 1e-16);
        var s = CompiledModel.Compile(Model(flow: false)).CreateSimulation();
        double energy = Value(s, 2, Field.InternalEnergy), entropy = Value(s, 2, Field.Entropy), speed = Value(s, 4, Field.Speed), vapor = 0;
        for (int i = 0; i < 20; ++i)
        {
            Step(s, 10_000_000); Balance(s); vapor = Math.Max(vapor, Value(s, 2, Field.VaporMass));
            Near(Value(s, 2, Field.InternalEnergy) - energy, Value(s, 12, Field.HydraulicWork), 2e-10);
            Near(.5 * .001 * (Value(s, 4, Field.Speed) * Value(s, 4, Field.Speed) - speed * speed) + Value(s, 12, Field.HydraulicWork), 0, 2e-9);
            Near(Value(s, 2, Field.Entropy), entropy, 2e-9);
            Near(Value(s, 12, Field.Volume), geometry.ReferenceVolumeAt(Value(s, 4, Field.Angle)), 1e-19);
        }
        Require(vapor > 0); Console.WriteLine($"Closed fuel chamber maximum vapor: {vapor:R} kg.");
    }
    private static void Pump()
    {
        double Run(double coefficient)
        {
            var s = CompiledModel.Compile(Model(inlet: coefficient)).CreateSimulation(); double source = Value(s, 1, Field.Mass), chamber = Value(s, 2, Field.Mass), receiver = Value(s, 3, Field.Mass);
            double peakVapor = 0;
            for (int i = 0; i < 20; ++i)
            {
                Require(s.Step(10_000_000) == SimulationStatus.Ok, $"Fuel pump inlet {coefficient:R} batch failed.");
                Balance(s); peakVapor = Math.Max(peakVapor, Value(s, 2, Field.VaporMass));
                Near(source - Value(s, 1, Field.Mass), Value(s, 10, Field.TotalFuelDelivered), 1e-14);
                Near(Value(s, 3, Field.Mass) - receiver, Value(s, 11, Field.TotalFuelDelivered), 1e-15);
                Near(Value(s, 2, Field.Mass) - chamber, Value(s, 10, Field.TotalFuelDelivered) - Value(s, 11, Field.TotalFuelDelivered), 1e-15);
                Require(Value(s, 10, Field.MassFlow) >= 0 && Value(s, 11, Field.MassFlow) >= 0);
            }
            Require(Value(s, 11, Field.TotalFuelDelivered) > 0 && Value(s, 12, Field.HydraulicWork) > 0 && Value(s, 4, Field.Speed) < 100);
            Console.WriteLine($"Fuel chamber inlet {coefficient:R}: delivered {Value(s, 11, Field.TotalFuelDelivered):R} kg, peak vapor {peakVapor:R} kg.");
            return Value(s, 11, Field.TotalFuelDelivered);
        }
        double starved = Run(1e-11), supplied = Run(3e-10); Require(supplied > starved);
    }
    private static void LiquidReference()
    {
        const double minimum = 1e-7, swept = 1e-7, compliance = 1e-12, inertia = .0005, duration = .02;
        double referenceVolume = minimum + swept * (1 - Math.Cos(.4)) / 2, mass = 750 * (referenceVolume + compliance * 1e6);
        (double Angle, double Speed) Integrate(int count)
        {
            double angle = .4, speed = 20, h = duration / count;
            double Acceleration(double theta) => ((mass / 750 - minimum - swept * (1 - Math.Cos(theta)) / 2) / compliance) * swept * Math.Sin(theta) / 2 / inertia;
            for (int i = 0; i < count; ++i)
            {
                double a = Acceleration(angle), b = Acceleration(angle + h * speed / 2), c = Acceleration(angle + h * (speed + h * a / 2) / 2), d = Acceleration(angle + h * (speed + h * b / 2));
                angle += h / 6 * (speed + 2 * (speed + h * a / 2) + 2 * (speed + h * b / 2) + speed + h * c);
                speed += h / 6 * (a + 2 * b + 2 * c + d);
            }
            return (angle, speed);
        }
        var exact = Integrate(2000); var finer = Integrate(4000); Near(exact.Angle, finer.Angle, 1e-12); Near(exact.Speed, finer.Speed, 1e-10);
        double previous = 0; var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 500_000, 250_000, 125_000, 62_500 })
        {
            var definition = new ModelDefinition
            {
                StepNanoseconds = tick,
                Nodes = [NodeDefinition.Rotor(4, inertia, 20, .4), NodeDefinition.FuelVolume(2, referenceVolume, mass, 300, EquilibriumFuelNetworkChecks.Material with { Compliance = new(compliance, Unit.CubicMeterPerPascal) })],
                Components = [ComponentDefinition.FuelChamberPump(12, 4, 2, new() { MinimumReferenceVolume = new(minimum, Unit.CubicMeter), SweptVolume = new(swept, Unit.CubicMeter), Phase = new(0, Unit.Radian) })]
            };
            var s = CompiledModel.Compile(definition).CreateSimulation(); Step(s, 20_000_000); Balance(s);
            double error = Math.Max(Math.Abs(Value(s, 4, Field.Angle) - exact.Angle), Math.Abs(Value(s, 4, Field.Speed) - exact.Speed) / 20);
            if (previous > 0) Require(previous / error is > 3.5 and < 4.5, $"Liquid chamber refinement: {previous:R}/{error:R}");
            previous = error; errors.Add(error); Near(Value(s, 2, Field.Temperature), 300, 1e-8); Near(Value(s, 2, Field.VaporMass), 0, 0);
        }
        Console.WriteLine("Fuel chamber closed-liquid normalized errors: " + string.Join(", ", errors.Select(e => e.ToString("R"))));
    }
    private static void Composition()
    {
        var definition = Model(100_000);
        definition = definition with
        {
            Nodes = [..definition.Nodes, NodeDefinition.Hydraulic(5, 1e-11, 1e5), NodeDefinition.Rotor(6, .002, 50), NodeDefinition.Rotor(7, .001)],
            Components = [..definition.Components, ComponentDefinition.DisplacementPump(20, 4, 5, 1e-10),
                new() { Id = 21, Kind = ComponentKind.IdealGear, NodeA = 4, NodeB = 6, Ratio = 2 }, ComponentDefinition.Clutch(22, 4, 7, 2, 1, 200)]
        };
        var s = CompiledModel.Compile(definition).CreateSimulation(); var replay = s.Fork(); Step(s, 200_000_000);
        for (int i = 0; i < 20; ++i) Step(replay, 10_000_000);
        Require(Snapshot(s) == Snapshot(replay)); Balance(s);
        Near(Value(s, 4, Field.Speed), 2 * Value(s, 6, Field.Speed), 1e-9);
        Require(Value(s, 22, Field.ClutchMode) == (int)ClutchMode.Locked && Value(s, 20, Field.HydraulicWork) > 0 && Value(s, 11, Field.TotalFuelDelivered) > 0);
    }
    private static double ReferenceSaturation(double t)
    {
        const double r = 287, cv = 717.5, cl = 2000, offset = -813000;
        double p0 = 1e5 * Math.Pow(t / 400, (cv + r - cl) / r) * Math.Exp(offset / r * (1 / t - 1 / 400d));
        double lo = p0, hi = p0 * Math.E;
        for (int i = 0; i < 64; ++i) { double p = (lo + hi) / 2; if (Math.Log(p / p0) > p / (750 * r * t)) hi = p; else lo = p; }
        return (lo + hi) / 2;
    }
    private static (double Energy, double Pressure, double Volume, double Enthalpy, double Temperature) ReferencePoint(double t, double mass, double v0, double compliance)
    {
        double p = ReferenceSaturation(t), v = v0 + compliance * p, vapor;
        if (mass >= 750 * v) { p = (mass / 750 - v0) / compliance; vapor = 0; }
        else if (mass <= p * v / (287 * t)) { p = 2 * mass * 287 * t / (v0 + Math.Sqrt(v0 * v0 + 4 * compliance * mass * 287 * t)); vapor = mass; }
        else vapor = (750 * v - mass) / (750 * 287 * t / p - 1);
        v = v0 + compliance * p;
        double fluid = (mass - vapor) * (2000 * t - 813000) + vapor * 717.5 * t;
        return (fluid + .5 * compliance * p * p, p, v, (fluid + p * v) / mass, t);
    }
    private static (double Energy, double Pressure, double Volume, double Enthalpy, double Temperature) ReferenceState(double mass, double energy, double v0, double compliance)
    {
        // Try the independently eliminated single-phase relations before the
        // bisection of equilibrium energy. No production closure is called.
        double liquidPressure = (mass / 750 - v0) / compliance;
        double liquidT = (energy - .5 * compliance * liquidPressure * liquidPressure + mass * 813000) / (mass * 2000);
        if (liquidT is >= 200 and <= 600 && liquidPressure > 0 && liquidPressure >= ReferenceSaturation(liquidT)) return ReferencePoint(liquidT, mass, v0, compliance);
        if (energy > 0)
        {
            double a = compliance * (717.5 / 287 + .5), b = 717.5 / 287 * v0;
            double p = 2 * energy / (b + Math.Sqrt(b * b + 4 * a * energy)), t = p * (v0 + compliance * p) / mass / 287;
            if (t is >= 200 and <= 600 && p <= ReferenceSaturation(t)) return ReferencePoint(t, mass, v0, compliance);
        }
        double lo = 200, hi = 600;
        for (int i = 0; i < 52; ++i) { double t = (lo + hi) / 2; if (ReferencePoint(t, mass, v0, compliance).Energy > energy) hi = t; else lo = t; }
        return ReferencePoint((lo + hi) / 2, mass, v0, compliance);
    }
    private static void PumpReference()
    {
        var definition = Model(); const double duration = .075;
        double[] initial = [0, 100, definition.Nodes[0].Position.Value, definition.Nodes[1].Position.Value, definition.Nodes[2].Position.Value,
            ReferencePoint(300, definition.Nodes[0].Position.Value, 1e-4, 5e-10).Energy,
            ReferencePoint(300, definition.Nodes[1].Position.Value, 1e-7, 5e-14).Energy,
            ReferencePoint(300, definition.Nodes[2].Position.Value, 8e-7, 5e-12).Energy, 0];
        void Rates(double[] x, double[] rate)
        {
            double geometricVolume = 1e-7 + 1e-7 * (1 - Math.Cos(x[0])), volumeRate = 1e-7 * Math.Sin(x[0]) * x[1];
            var source = ReferenceState(x[2], x[5], 1e-4, 5e-10); var chamber = ReferenceState(x[3], x[6], geometricVolume, 5e-14); var rail = ReferenceState(x[4], x[7], 8e-7, 5e-12);
            double inlet = 1e-11 * Math.Max(0, source.Pressure - chamber.Pressure) * x[2] / source.Volume;
            double outlet = 1e-9 * Math.Max(0, chamber.Pressure - rail.Pressure) * x[3] / chamber.Volume;
            rate[0] = x[1]; rate[1] = chamber.Pressure * 1e-7 * Math.Sin(x[0]) / .001;
            rate[2] = -inlet; rate[3] = inlet - outlet; rate[4] = outlet;
            rate[5] = -inlet * source.Enthalpy; rate[6] = inlet * source.Enthalpy - outlet * chamber.Enthalpy - chamber.Pressure * volumeRate; rate[7] = outlet * chamber.Enthalpy; rate[8] = outlet;
        }
        double[] Integrate(int steps)
        {
            double[] x = initial.ToArray(), stage = new double[9], a = new double[9], b = new double[9], c = new double[9], d = new double[9]; double h = duration / steps;
            for (int n = 0; n < steps; ++n)
            {
                Rates(x, a); for (int j = 0; j < 9; ++j) stage[j] = x[j] + h * a[j] / 2; Rates(stage, b);
                for (int j = 0; j < 9; ++j) stage[j] = x[j] + h * b[j] / 2; Rates(stage, c);
                for (int j = 0; j < 9; ++j) stage[j] = x[j] + h * c[j]; Rates(stage, d);
                for (int j = 0; j < 9; ++j) x[j] += h / 6 * (a[j] + 2 * b[j] + 2 * c[j] + d[j]);
            }
            return x;
        }
        var reference = Integrate(16000); var finer = Integrate(32000);
        double referenceDifference = 0;
        for (int j = 0; j < 9; ++j)
            referenceDifference = Math.Max(referenceDifference, Math.Abs(reference[j] - finer[j]) / (j is 2 or 3 or 4 or 8 ? initial[3] : j == 1 ? 100 : j == 0 ? 1 : 100));
        Console.WriteLine($"Fuel chamber independent RK4 refinement difference: {referenceDifference:R}.");
        Require(referenceDifference < 1e-8, "The independent reference needs further refinement.");
        var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 50_000, 25_000, 12_500, 6_250 })
        {
            var s = CompiledModel.Compile(Model(tick)).CreateSimulation(); Step(s, 75_000_000); Balance(s);
            double error = Math.Max(Math.Abs(Value(s, 4, Field.Speed) - reference[1]) / 100,
                Math.Max(Math.Abs(Value(s, 2, Field.Mass) - reference[3]), Math.Abs(Value(s, 11, Field.TotalFuelDelivered) - reference[8])) / initial[3]);
            errors.Add(error);
        }
        Console.WriteLine("Fuel chamber phase-changing pump normalized errors: " + string.Join(", ", errors.Select(e => e.ToString("R"))));
        for (int j = 1; j < errors.Count; ++j) Require(errors[j - 1] / errors[j] is > 1.6 and < 2.4, $"Phase-changing pump refinement: {errors[j - 1]:R}/{errors[j]:R}");
    }
    private static void Transactions()
    {
        var definition = Model(); definition = definition with { Components = [..definition.Components, ComponentDefinition.Torque(20, 4, 200, 0)] };
        var s = CompiledModel.Compile(definition).CreateSimulation(); Step(s, 10_000_000); var before = Snapshot(s); var replay = s.Fork();
        Require(s.Step(100_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.Step(100_000_000, [new(before.TimeNanoseconds + 20_000_000, 200, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var branch = s.Fork(); Require(branch.SubmitInputs([new(100, 0)]) == SimulationStatus.Ok); Step(branch, 20_000_000); Require(Snapshot(s) == before);
        Step(s, 50_000_000); for (int i = 0; i < 10; ++i) Step(replay, 5_000_000); Require(Snapshot(s) == Snapshot(replay));
        var values = new Scalar[s.Model.OutputCount]; for (int i = 0; i < 20; ++i) { Step(s, 50_000); s.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20; ++i) { Step(s, 50_000); s.ReadSnapshot(values); }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Moving chamber stepping allocated.");
    }
    private static void Contracts()
    {
        var definition = Model(); var compiled = CompiledModel.Compile(definition);
        var duplicate = definition with { Components = [..definition.Components, ComponentDefinition.FuelChamberPump(13, 4, 2, Geometry)] };
        Require(!CompiledModel.TryCompile(duplicate, out _, out var owner) && owner!.Field == "fuel_chamber.node_b");
        var wrong = definition with { Components = definition.Components.Select(c => c.FuelChamber is null ? c : c with { FuelChamber = Geometry with { SweptVolume = new(1, Unit.Kelvin) } }).ToArray() };
        Require(!CompiledModel.TryCompile(wrong, out _, out var unit) && unit!.Code == DiagnosticCode.Unit);
        wrong = definition with { Nodes = definition.Nodes.Select(n => n.Id == 2 ? n with { Storage = new(2e-7, Unit.CubicMeter) } : n).ToArray() };
        Require(!CompiledModel.TryCompile(wrong, out _, out var initial) && initial!.Field == "fuel_chamber.initial_volume");
        var before = compiled.CreateSimulation(); definition.Components[^1] = ComponentDefinition.FuelChamberPump(12, 4, 2, Geometry with { SweptVolume = new(3e-7, Unit.CubicMeter) });
        var copy = CompiledModel.Compile(Model()).CreateSimulation(); Step(before, 1_000_000); Step(copy, 1_000_000); Require(Snapshot(before) == Snapshot(copy));
    }
    private static void CheckValve()
    {
        var definition = EquilibriumFuelNetworkChecks.Model(reverse: true);
        definition = definition with { Components = [ComponentDefinition.FuelCheckValve(10, 1, 2, 1e-11)] };
        var s = CompiledModel.Compile(definition).CreateSimulation(); double mass = Value(s, 1, Field.Mass), energy = Value(s, 1, Field.InternalEnergy);
        Step(s, 10_000_000); Balance(s); Near(Value(s, 1, Field.Mass), mass, 0); Near(Value(s, 1, Field.InternalEnergy), energy, 0);
        Near(Value(s, 10, Field.TotalFuelDelivered), 0, 0); Near(Value(s, 10, Field.TransportEnergy), 0, 0);
    }
}
