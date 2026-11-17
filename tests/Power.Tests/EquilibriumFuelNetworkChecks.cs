// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class EquilibriumFuelNetworkChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("equilibrium fuel network / integrated liquid to two-phase flow / enthalpy mass energy entropy", Transfer),
        ("equilibrium fuel network / reverse flow and finite-bath heat / complete ledgers", ReverseHeat),
        ("equilibrium fuel network / connected paths and heat-driven reversal / entropy accounting", ConnectedPaths),
        ("equilibrium fuel network / independent liquid pressure exponential and numerical heating refinement", LiquidReference),
        ("equilibrium fuel network / independent two-phase transport and finite-wall ODE / refinement", PhaseReference),
        ("equilibrium fuel network / scheduled batches cancellation forks rollback and zero allocations", Transactions),
        ("equilibrium fuel network / late mechanical failure rolls back all fuel histories", LateRollback),
        ("equilibrium fuel network / speculative clutch capture and shared heat / exact batch replay", HybridComposition),
        ("equilibrium fuel network / explicit material units ownership limits and immutable compilation", Contracts)
    ];
    internal static EquilibriumFuelDefinition Material => new()
    {
        Compliance = new(5e-13, Unit.CubicMeterPerPascal), LiquidDensity = new(750, Unit.KilogramPerCubicMeter), MaximumPressure = new(20e6, Unit.Pascal),
        GasConstant = new(287, Unit.JoulePerKilogramKelvin), VaporSpecificHeat = new(717.5, Unit.JoulePerKilogramKelvin), LiquidSpecificHeat = new(2000, Unit.JoulePerKilogramKelvin),
        ReferenceTemperature = new(400, Unit.Kelvin), LatentInternalEnergy = new(300000, Unit.JoulePerKilogram), ReferencePressure = new(100000, Unit.Pascal),
        MinimumTemperature = new(200, Unit.Kelvin), MaximumTemperature = new(600, Unit.Kelvin), LowerHeatingValue = new(44e6, Unit.JoulePerKilogram)
    };
    internal static ModelDefinition Model(ulong step = 100_000, bool reverse = false, bool heat = false) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.FuelVolume(1, 8e-7, reverse ? .0003 : .00065, reverse ? 300 : 400, Material),
            NodeDefinition.FuelVolume(2, 8e-7, reverse ? .00065 : .0003, reverse ? 400 : 300, Material), NodeDefinition.Thermal(3, 2, 500)],
        Components = heat ? [ComponentDefinition.FuelRestriction(10, 1, 2, 1e-11, 100), ComponentDefinition.FuelHeatLink(11, 2, 3, .2)] : [ComponentDefinition.FuelRestriction(10, 1, 2, 1e-11, 100)]
    };
    private static SnapshotInfo Snapshot(Simulation simulation) => simulation.ReadSnapshot(new Scalar[simulation.Model.OutputCount]);
    private static void Step(Simulation simulation, ulong time) => Require(simulation.Step(time) == SimulationStatus.Ok, "Equilibrium fuel step failed.");
    private static void Balances(Simulation simulation)
    { Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-7); Near(Value(simulation, 0, Field.MassResidual), 0, 1e-15); Near(Value(simulation, 0, Field.FuelResidual), 0, 1e-15); }
    private static void Transfer()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation();
        Require(s.Model.Fidelity == "homogeneous_equilibrium_fuel_network");
        double initialMass = Value(s, 1, Field.Mass) + Value(s, 2, Field.Mass), initialEnergy = Value(s, 1, Field.InternalEnergy) + Value(s, 2, Field.InternalEnergy);
        double entropy = Value(s, 1, Field.Entropy) + Value(s, 2, Field.Entropy);
        Near(Value(s, 1, Field.VaporMass), 0, 0); Require(Value(s, 2, Field.VaporMass) > 0);
        for (int i = 0; i < 10; ++i)
        {
            Step(s, 20_000_000); Balances(s);
            double next = Value(s, 1, Field.Entropy) + Value(s, 2, Field.Entropy); Require(next >= entropy - 1e-12); entropy = next;
        }
        Near(Value(s, 1, Field.Mass) + Value(s, 2, Field.Mass), initialMass, 1e-16);
        Near(Value(s, 1, Field.InternalEnergy) + Value(s, 2, Field.InternalEnergy), initialEnergy, 1e-11);
        Require(Value(s, 1, Field.VaporMass) > 0 && Value(s, 10, Field.TotalFuelDelivered) > 0);
        Near(Value(s, 1, Field.Mass) + Value(s, 10, Field.TotalFuelDelivered), .00065, 1e-16);
        Require(Value(s, 10, Field.EntropyProduced) > 0);
        Console.WriteLine($"Equilibrium fuel transfer: mass={Value(s, 10, Field.TotalFuelDelivered):R}, source vapor={Value(s, 1, Field.VaporMass):R}, entropy={Value(s, 10, Field.EntropyProduced):R}");
    }
    private static void ReverseHeat()
    {
        var forward = CompiledModel.Compile(Model()).CreateSimulation(); var reverse = CompiledModel.Compile(Model(reverse: true)).CreateSimulation();
        Step(forward, 100_000_000); Step(reverse, 100_000_000);
        Near(Value(forward, 1, Field.Mass), Value(reverse, 2, Field.Mass), 1e-17);
        Near(Value(forward, 10, Field.TotalFuelDelivered), -Value(reverse, 10, Field.TotalFuelDelivered), 1e-17);
        Near(Value(forward, 10, Field.TransportEnergy), -Value(reverse, 10, Field.TransportEnergy), 1e-12);
        var heat = CompiledModel.Compile(Model(heat: true)).CreateSimulation(); Step(heat, 200_000_000); Balances(heat);
        Require(Value(heat, 3, Field.Temperature) < 500 && Value(heat, 11, Field.TransportEnergy) > 0 && Value(heat, 11, Field.EntropyProduced) > 0);
    }
    private static void ConnectedPaths()
    {
        var definition = new ModelDefinition
        {
            StepNanoseconds = 1_000_000,
            Nodes = [NodeDefinition.FuelVolume(1, 8e-7, .0003, 350, Material), NodeDefinition.FuelVolume(2, 8e-7, .0003, 400, Material),
                NodeDefinition.Thermal(3, 2, 600), NodeDefinition.FuelVolume(4, 8e-7, .0001, 300, Material)],
            Components = [ComponentDefinition.FuelRestriction(10, 1, 2, 1e-13), ComponentDefinition.FuelRestriction(11, 2, 4, 3e-14), ComponentDefinition.FuelHeatLink(12, 1, 3, 1)]
        };
        var s = CompiledModel.Compile(definition).CreateSimulation(); double entropy = Value(s, 1, Field.Entropy) + Value(s, 2, Field.Entropy) + Value(s, 4, Field.Entropy);
        bool negative = false, positive = false;
        for (int i = 0; i < 50; ++i)
        {
            Step(s, 10_000_000); double flow = Value(s, 10, Field.MassFlow); negative |= flow < 0; positive |= flow > 0; Balances(s);
        }
        Require(negative && positive, "Heating did not reverse the actual pressure-driven flow.");
        double change = Value(s, 1, Field.Entropy) + Value(s, 2, Field.Entropy) + Value(s, 4, Field.Entropy) - entropy + 2 * Math.Log(Value(s, 3, Field.Temperature) / 600);
        Near(change, Value(s, 10, Field.EntropyProduced) + Value(s, 11, Field.EntropyProduced) + Value(s, 12, Field.EntropyProduced), 2e-11);
        Require(change > 0);
    }

    private static void LiquidReference()
    {
        const double c = 5e-13, k = 1e-11, duration = .02, density = 750, cl = 2000;
        const double p0 = (.00065 / density - 8e-7) / c, p1 = (.00064 / density - 8e-7) / c;
        double exactGap = (p0 - p1) * Math.Exp(-2 * k * duration / c), exactMass = .00064 + (p0 - p1 - exactGap) * density * c / 2;
        double exactTemperature = 300 + c * ((p0 - p1) * (p0 - p1) - exactGap * exactGap) / 4 / exactMass / cl;
        double previous = 0; var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 500_000, 250_000, 125_000, 62_500 })
        {
            var definition = Model(tick) with { Nodes = [NodeDefinition.FuelVolume(1, 8e-7, .00065, 300, Material), NodeDefinition.FuelVolume(2, 8e-7, .00064, 300, Material)] };
            var s = CompiledModel.Compile(definition).CreateSimulation(); Step(s, 20_000_000); Balances(s);
            double discreteGap = (p0 - p1) / Math.Pow(1 + 2 * k * tick * 1e-9 / c, duration / (tick * 1e-9));
            Near(Value(s, 1, Field.Pressure) - Value(s, 2, Field.Pressure), discreteGap, 1e-5);
            Near(Value(s, 1, Field.Pressure) + Value(s, 2, Field.Pressure), p0 + p1, 1e-5);
            Require(Value(s, 1, Field.VaporMass) == 0 && Value(s, 2, Field.VaporMass) == 0);
            double error = Math.Max(Math.Abs(Value(s, 2, Field.Pressure) - (p0 + p1 - exactGap) / 2) / p0,
                Math.Max(Math.Abs(Value(s, 1, Field.Temperature) - 300), Math.Abs(Value(s, 2, Field.Temperature) - exactTemperature)) / 300);
            if (previous > 0) Require(previous / error is > 1.8 and < 2.2);
            previous = error; errors.Add(error);
        }
        Console.WriteLine("Equilibrium fuel liquid-limit normalized errors: " + string.Join(", ", errors.Select(e => e.ToString("R"))));
    }

    // Independent saturation pressure and mass-energy inversion. The production
    // flash, temperature derivatives and pairwise Newton solve are not called.
    private static (double P, double V, double H, double T) Reference(double mass, double energy)
    {
        (double E, double P, double V, double H) Point(double t)
        {
            const double r = 287, cv = 717.5, cl = 2000, density = 750, v0 = 8e-7, c = 5e-13, offset = -813000;
            double p0 = 100000 * Math.Pow(t / 400, (cv + r - cl) / r) * Math.Exp(offset / r * (1 / t - 1 / 400d));
            double lo = p0, hi = p0 * Math.E;
            for (int n = 0; n < 64; ++n)
            { double middle = (lo + hi) / 2; if (Math.Log(middle / p0) > middle / (density * r * t)) hi = middle; else lo = middle; }
            double p = (lo + hi) / 2, v = v0 + c * p, vapor;
            if (mass >= density * v) { p = (mass / density - v0) / c; vapor = 0; }
            else if (mass <= p * v / (r * t)) { p = (-v0 + Math.Sqrt(v0 * v0 + 4 * c * mass * r * t)) / (2 * c); vapor = mass; }
            else vapor = (density * v - mass) / (density * r * t / p - 1);
            v = v0 + c * p;
            double fluid = (mass - vapor) * (cl * t + offset) + vapor * cv * t;
            return (fluid + .5 * c * p * p, p, v, (fluid + p * v) / mass);
        }
        double low = 200, high = 600;
        for (int n = 0; n < 52; ++n) { double t = (low + high) / 2; if (Point(t).E > energy) high = t; else low = t; }
        double temperature = (low + high) / 2; var result = Point(temperature); return (result.P, result.V, result.H, temperature);
    }
    private static void PhaseReference()
    {
        var initial = CompiledModel.Compile(Model(heat: true)).CreateSimulation();
        double[] start = [Value(initial, 1, Field.Mass), Value(initial, 1, Field.InternalEnergy), Value(initial, 2, Field.Mass), Value(initial, 2, Field.InternalEnergy), 500];
        void Rates(double[] x, double[] rate)
        {
            var a = Reference(x[0], x[1]); var b = Reference(x[2], x[3]);
            double flow = 1e-11 * (a.P - b.P) * (a.P >= b.P ? x[0] / a.V : x[2] / b.V);
            double enthalpy = flow * (flow >= 0 ? a.H : b.H), heat = .2 * (x[4] - b.T);
            rate[0] = -flow; rate[1] = -enthalpy; rate[2] = flow; rate[3] = enthalpy + heat; rate[4] = -heat / 2;
        }
        double[] Integrate(int count)
        {
            double[] x = start.ToArray(), stage = new double[5], a = new double[5], b = new double[5], c = new double[5], d = new double[5]; double h = .05 / count;
            for (int i = 0; i < count; ++i)
            {
                Rates(x, a); for (int j = 0; j < 5; ++j) stage[j] = x[j] + h * a[j] / 2; Rates(stage, b);
                for (int j = 0; j < 5; ++j) stage[j] = x[j] + h * b[j] / 2; Rates(stage, c);
                for (int j = 0; j < 5; ++j) stage[j] = x[j] + h * c[j]; Rates(stage, d);
                for (int j = 0; j < 5; ++j) x[j] += h / 6 * (a[j] + 2 * b[j] + 2 * c[j] + d[j]);
            }
            return x;
        }
        var reference = Integrate(1000); var finer = Integrate(2000);
        for (int j = 0; j < 5; ++j) Near(reference[j], finer[j], j is 0 or 2 ? 1e-11 : 2e-6);
        double previous = 0; var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 1_000_000, 500_000, 250_000, 125_000 })
        {
            var s = CompiledModel.Compile(Model(tick, heat: true)).CreateSimulation(); Step(s, 50_000_000); Balances(s);
            double[] result = [Value(s, 1, Field.Mass), Value(s, 1, Field.InternalEnergy), Value(s, 2, Field.Mass), Value(s, 2, Field.InternalEnergy), Value(s, 3, Field.Temperature)];
            double error = 0;
            for (int j = 0; j < 5; ++j) error = Math.Max(error, Math.Abs(result[j] - reference[j]) / (j is 0 or 2 ? .00065 : j == 4 ? 500 : 100));
            if (previous > 0) Require(previous / error is > 1.6 and < 2.4, $"Two-phase network refinement: {previous:R}/{error:R}");
            previous = error; errors.Add(error); Require(Value(s, 1, Field.VaporMass) > 0);
        }
        Console.WriteLine("Equilibrium fuel two-phase wall ODE normalized errors: " + string.Join(", ", errors.Select(e => e.ToString("R"))));
    }
    private static void Transactions()
    {
        var s = CompiledModel.Compile(Model(heat: true)).CreateSimulation(); var replay = s.Fork();
        ScheduledInput[] events = [new(20_000_000, 100, .5), new(50_000_000, 100, 0), new(100_000_000, 100, 1)];
        Require(s.Step(200_000_000, events) == SimulationStatus.Ok);
        for (int i = 0; i < 20; ++i)
        {
            ulong time = (ulong)i * 10_000_000;
            var local = events.Where(e => e.TimeNanoseconds >= time && e.TimeNanoseconds < time + 10_000_000).ToArray();
            Require(replay.Step(10_000_000, local) == SimulationStatus.Ok);
        }
        Require(Snapshot(s) == Snapshot(replay)); var before = Snapshot(s);
        Require(s.Step(100_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.SubmitInputs([new(100, 1.01)]) == SimulationStatus.InvalidInput && Snapshot(s) == before);
        var branch = s.Fork(); Require(branch.SubmitInputs([new(100, 0)]) == SimulationStatus.Ok); Step(branch, 20_000_000); Require(Snapshot(s) == before);
        var values = new Scalar[s.Model.OutputCount];
        for (int i = 0; i < 20; ++i) { Step(s, 100_000); s.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 20; ++i) { Step(s, 100_000); s.ReadSnapshot(values); }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Fuel network stepping allocated.");
    }
    private static void Contracts()
    {
        var definition = Model(); var compiled = CompiledModel.Compile(definition);
        definition.Nodes[0] = definition.Nodes[0] with { Fuel = Material with { LowerHeatingValue = new(43e6, Unit.JoulePerKilogram) } };
        Require(!CompiledModel.TryCompile(definition, out _, out var mismatch) && mismatch!.Field == "fuel.material");
        var invalid = Model() with { Nodes = Model().Nodes.Select(n => n.Id == 1 ? n with { Fuel = Material with { LiquidDensity = new(750, Unit.Kelvin) } } : n).ToArray() };
        Require(!CompiledModel.TryCompile(invalid, out _, out var unit) && unit!.Code == DiagnosticCode.Unit);
        invalid = Model() with { Components = [ComponentDefinition.FuelRestriction(10, 1, 0, 1e-11)] };
        Require(!CompiledModel.TryCompile(invalid, out _, out var port) && port!.Code == DiagnosticCode.Connection);
        invalid = Model() with { Nodes = Model().Nodes.Select(n => n.Id == 1 ? n with { Position = new(0, Unit.Kilogram) } : n).ToArray() };
        Require(!CompiledModel.TryCompile(invalid, out _, out _));
        var a = compiled.CreateSimulation(); var b = CompiledModel.Compile(Model()).CreateSimulation(); Step(a, 1_000_000); Step(b, 1_000_000); Require(Snapshot(a) == Snapshot(b));
        var capacityNodes = Enumerable.Range(1, 32).Select(id => NodeDefinition.FuelVolume((uint)id, 8e-7, .0003, 350, Material)).ToArray();
        var links = Enumerable.Range(0, 13).Select(id => ComponentDefinition.FuelRestriction((uint)(100 + id), 1, (uint)(2 + id), 1e-11)).ToArray();
        Require(CompiledModel.Compile(new() { Nodes = capacityNodes, Components = links.Take(12).ToArray() }).StateCount == 124);
        Require(!CompiledModel.TryCompile(new() { Nodes = capacityNodes, Components = links }, out _, out var limit) && limit!.Code == DiagnosticCode.Capacity);
        // Finite per-tick mass can overflow when normalized to kg/s. This is a
        // numeric-scale fixture, not a plausible measured fuel or flow coefficient.
        var extremeMaterial = Material with
        {
            LiquidDensity = new(1e308, Unit.KilogramPerCubicMeter), GasConstant = new(1e-300, Unit.JoulePerKilogramKelvin),
            VaporSpecificHeat = new(1e-300, Unit.JoulePerKilogramKelvin), LiquidSpecificHeat = new(2e-300, Unit.JoulePerKilogramKelvin),
            LatentInternalEnergy = new(3e-298, Unit.JoulePerKilogram), ReferencePressure = new(.001, Unit.Pascal), LowerHeatingValue = new(1e-298, Unit.JoulePerKilogram)
        };
        var extreme = CompiledModel.Compile(new()
        {
            StepNanoseconds = 1,
            Nodes = [NodeDefinition.FuelVolume(1, 1e-4, 1e308 * (1e-4 + 5e-13 * 1e6), 300, extremeMaterial),
                NodeDefinition.FuelVolume(2, 1e-4, 1e308 * (1e-4 + 5e-13 * 5e5), 300, extremeMaterial)],
            Components = [ComponentDefinition.FuelRestriction(10, 1, 2, 1e-5)]
        }).CreateSimulation();
        var initial = Snapshot(extreme);
        Require(extreme.Step(1) == SimulationStatus.NumericalFailure && Snapshot(extreme) == initial, "Overflowing mean flow was published or partially committed.");
    }
    private static void LateRollback()
    {
        var definition = Model(heat: true);
        definition = definition with { Nodes = [..definition.Nodes, NodeDefinition.Rotor(4, .1)], Components = [..definition.Components, ComponentDefinition.Torque(20, 4, 200, 0)] };
        var s = CompiledModel.Compile(definition).CreateSimulation(); Step(s, 10_000_000); var before = Snapshot(s); var reference = s.Fork();
        Require(s.Step(100_000_000, [new(before.TimeNanoseconds + 20_000_000, 200, double.MaxValue)]) == SimulationStatus.NumericalFailure);
        Require(Snapshot(s) == before, "Late failure retained fuel mass, energy, input or histories.");
        Step(s, 50_000_000); Step(reference, 50_000_000); Require(Snapshot(s) == Snapshot(reference));
    }
    private static void HybridComposition()
    {
        var definition = Model(1_000_000, heat: true);
        definition = definition with { Nodes = [..definition.Nodes, NodeDefinition.Rotor(4, .1, 20), NodeDefinition.Rotor(5, .2)],
            Components = [..definition.Components, ComponentDefinition.Clutch(20, 4, 5, 5, 5, 200, heat: 3)] };
        var s = CompiledModel.Compile(definition).CreateSimulation(); var replay = s.Fork();
        Step(s, 500_000_000); for (int i = 0; i < 100; ++i) Step(replay, 5_000_000);
        Require(Snapshot(s) == Snapshot(replay)); Balances(s);
        Require(Value(s, 20, Field.ClutchMode) == (int)ClutchMode.Locked && Value(s, 20, Field.FrictionHeat) > 0);
        Require(Value(s, 11, Field.TransportEnergy) > 0 && Value(s, 10, Field.TotalFuelDelivered) > 0);
    }
}
