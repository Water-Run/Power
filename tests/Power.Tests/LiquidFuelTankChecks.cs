// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class LiquidFuelTankChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("finite fuel tank / analytic exhaustion shaft work and dry pumping", Exhaustion),
        ("finite fuel tank / internal mass caloric chemical inventory and nozzle delivery", Inventory),
        ("finite fuel tank / signed return caloric mixing and empty restart", Return),
        ("finite fuel tank / independent wet ODE and dry boundary refinement", Reference),
        ("finite fuel tank / complete rollback forks cancellation zero allocations ownership", Transactions)
    ];
    internal static ModelDefinition Model(double mass = 1e-6, double temperature = 340, double speed = 100, double dose = 0, ulong tick = 20_000)
    {
        var d = LiquidRailFeedChecks.Model(tick, dose, speed: speed);
        var components = d.Components.ToArray();
        components[^1] = components[^1] with { LiquidRailFeed = components[^1].LiquidRailFeed! with { SupplyTemperature = default, TankComponent = 22 } };
        return d with { Components = [..components, ComponentDefinition.FuelTank(22, 2, new() { InjectorComponent = 12, InitialMass = new(mass, Unit.Kilogram), InitialTemperature = new(temperature, Unit.Kelvin) })] };
    }
    private static void Step(Simulation s, ulong ns) => Require(s.Step(ns) == SimulationStatus.Ok);
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Balance(Simulation s)
    {
        Near(Value(s, 0, Field.EnergyResidual), 0, 2e-7); Near(Value(s, 0, Field.MassResidual), 0, 1e-14);
        Near(Value(s, 0, Field.FuelResidual), 0, 1e-14); Near(Value(s, 0, Field.HydraulicVolumeResidual), 0, 1e-16);
        Near(Value(s, 0, Field.FuelEnergyIn), 0, 0);
    }
    private static void Exhaustion()
    {
        const double mass = 1e-6, rho = 750, compliance = 5e-13, p0 = 800_000, pin = 100_000, inertia = .02;
        var s = CompiledModel.Compile(Model()).CreateSimulation(); Step(s, 50_000_000);
        double p = p0 + mass / (rho * compliance);
        double work = .5 * compliance * (p * p - p0 * p0) - pin * mass / rho;
        Near(Value(s, 22, Field.Mass), 0, 0); Near(Value(s, 22, Field.InternalEnergy), 0, 0); Near(Value(s, 22, Field.TankState), 1, 0);
        Near(Value(s, 12, Field.Mass), .0005 + mass, 1e-14); Near(Value(s, 5, Field.Pressure), p, 1e-5);
        Near(Value(s, 4, Field.Speed), Math.Sqrt(10000 - 2 * work / inertia), 1e-9);
        Near(Value(s, 20, Field.HydraulicWork), work, 1e-10); Near(Value(s, 20, Field.VolumeFlow), 0, 0); Near(Value(s, 20, Field.Torque), 0, 0);
        Balance(s);
        var empty = CompiledModel.Compile(Model(mass: 0)).CreateSimulation(); Step(empty, 5_000_000);
        Near(Value(empty, 4, Field.Speed), 100, 0); Near(Value(empty, 5, Field.Pressure), p0, 0); Balance(empty);
    }
    private static void Inventory()
    {
        var s = CompiledModel.Compile(Model(mass: 20e-6, dose: 8e-6)).CreateSimulation(); Step(s, 100_000_000);
        double supplied = Value(s, 21, Field.TotalFuelDelivered), discharged = Value(s, 12, Field.TotalFuelDelivered);
        Require(supplied > 0 && discharged > 0); Near(Value(s, 22, Field.Mass) + supplied, 20e-6, 1e-14);
        Near(Value(s, 12, Field.Mass) + discharged, .0005 + supplied, 1e-14);
        Near(Value(s, 22, Field.Temperature), 340, 1e-8); Require(Value(s, 12, Field.Temperature) > 300);
        Balance(s);
    }
    private static void Return()
    {
        foreach (double initial in new[] { 0, 10e-6 })
        {
            var s = CompiledModel.Compile(Model(mass: initial, speed: -100)).CreateSimulation(); Step(s, 20_000_000);
            Require(Value(s, 22, Field.Mass) > initial && Value(s, 21, Field.TotalFuelDelivered) < 0);
            double temperature = Value(s, 22, Field.Temperature);
            Require(temperature >= 300 - 1e-8 && temperature < 340); Near(Value(s, 22, Field.TankState), 0, 0);
            Balance(s);
        }
    }
    private static void Reference()
    {
        // Independent continuous wet solution and exact dry inventory endpoint.
        const double displacement = 1e-7, compliance = 5e-13, inertia = .02, time = .01;
        double frequency = displacement / Math.Sqrt(inertia * compliance);
        double pressure = 100_000 + 700_000 * Math.Cos(frequency * time) + displacement * 100 / (compliance * frequency) * Math.Sin(frequency * time);
        double previous = double.PositiveInfinity;
        foreach (ulong tick in new ulong[] { 1_000_000, 500_000, 250_000 })
        {
            var d = Model(mass: .01, tick: tick); var components = d.Components.ToArray();
            int pump = Array.FindIndex(components, c => c.Id == 20);
            components[pump] = ComponentDefinition.DisplacementPump(20, 4, 5, displacement, reservoirPressure: 100_000);
            var s = CompiledModel.Compile(d with { Components = components }).CreateSimulation(); Step(s, 10_000_000);
            double error = Math.Abs(Value(s, 5, Field.Pressure) - pressure);
            Require(error < previous); previous = error; Balance(s);
            var dry = CompiledModel.Compile(Model(tick: tick)).CreateSimulation(); Step(dry, 50_000_000);
            Near(Value(dry, 22, Field.Mass), 0, 0); Balance(dry);
        }
    }
    private static void Transactions()
    {
        var d = Model(); var s = CompiledModel.Compile(d).CreateSimulation(); var before = Snapshot(s);
        Require(s.Step(50_000_000, new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.Step(50_000_000, [new(30_000_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var fork = s.Fork(); Step(s, 50_000_000); for (int i = 0; i < 50; ++i) Step(fork, 1_000_000); Require(Snapshot(s) == Snapshot(fork));
        var values = new Scalar[s.Model.OutputCount]; for (int i = 0; i < 10; ++i) { Step(s, 20_000); s.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; ++i) { Step(s, 20_000); s.ReadSnapshot(values); }
        Require(allocated == GC.GetAllocatedBytesForCurrentThread());
        var components = d.Components.ToArray(); components[^2] = components[^2] with { LiquidRailFeed = components[^2].LiquidRailFeed! with { SupplyTemperature = new(320, Unit.Kelvin) } };
        Throws<ModelCompileException>(() => CompiledModel.Compile(d with { Components = components }));
        Throws<ModelCompileException>(() => CompiledModel.Compile(d with { Components = d.Components.Take(d.Components.Count() - 2).Append(d.Components[^1]).ToArray() }));
    }
}
