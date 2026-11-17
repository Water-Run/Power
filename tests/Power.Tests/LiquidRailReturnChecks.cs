// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class LiquidRailReturnChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("liquid return / independent relief pressure decay and carried heat", Decay),
        ("liquid recirculation / thermal fractions full inventory and energy ledgers", Mixing),
        ("liquid return / explicit external caloric chemical and pressure boundaries", External),
        ("liquid recirculation / independent simultaneous mass thermal pressure ODE refinement", Reference),
        ("liquid return / multiple routes ownership closures and allocation-free rollback", Transactions)
    ];
    internal static ModelDefinition Model(double fraction = 1, ulong tick = 50_000, bool finite = true, double speed = 100)
    {
        var d = finite ? LiquidFuelTankChecks.Model(mass: .0001, tick: tick, speed: speed) : LiquidRailFeedChecks.Model(tick, speed: speed);
        return d with { Components = [..d.Components, ComponentDefinition.PressureRelief(30, 5, 0, 1e-12, 200_000, reservoirPressure: 100_000),
            ComponentDefinition.RailReturn(31, 2, new() { FeedComponent = 21, ValveComponent = 30, FluidHeatFraction = new(fraction, Unit.Fraction) })] };
    }
    private static void Step(Simulation s, ulong dt) => Require(s.Step(dt) == SimulationStatus.Ok);
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Balance(Simulation s)
    {
        Near(Value(s, 0, Field.EnergyResidual), 0, 3e-7); Near(Value(s, 0, Field.MassResidual), 0, 1e-14);
        Near(Value(s, 0, Field.FuelResidual), 0, 1e-14); Near(Value(s, 0, Field.HydraulicVolumeResidual), 0, 1e-16);
    }
    private static double Caloric(Simulation s) => Value(s, 12, Field.InternalEnergy) - .5 * 5e-13 * Math.Pow(Value(s, 5, Field.Pressure), 2) + Value(s, 22, Field.InternalEnergy);
    private static void Decay()
    {
        var d = Model(speed: 0); d = d with { Components = [..d.Components, ComponentDefinition.Clutch(32, 4, 0, 10, 8)] };
        var s = CompiledModel.Compile(d).CreateSimulation(); double caloric = Caloric(s); Step(s, 100_000_000);
        double p = 300_000 + 500_000 * Math.Exp(-.2), volume = 5e-13 * (800_000 - p);
        double heat = .5 * 5e-13 * (800_000 * 800_000d - p * p) - 100_000 * volume;
        Near(Value(s, 4, Field.Speed), 0, 1e-15); Near(Value(s, 5, Field.Pressure), p, 1e-3);
        Near(Value(s, 31, Field.TotalFuelDelivered), 750 * volume, 1e-12); Near(Value(s, 31, Field.FluidHeat), heat, 1e-9);
        Near(Caloric(s) - caloric, Value(s, 31, Field.FluidHeat), 1e-9); Near(Value(s, 0, Field.HeatRejected), 0, 1e-14); Balance(s);
    }
    private static void Mixing()
    {
        foreach (double fraction in new[] { 0, .5, 1 })
        {
            var s = CompiledModel.Compile(Model(fraction)).CreateSimulation(); double initial = Caloric(s); Step(s, 50_000_000);
            double pump = Value(s, 21, Field.TotalFuelDelivered), returned = Value(s, 31, Field.TotalFuelDelivered);
            Require(returned > 0 && pump > 0); Near(Value(s, 22, Field.Mass) + pump - returned, .0001, 1e-14);
            Near(Value(s, 12, Field.Mass) - pump + returned, .0005, 1e-14);
            Near(Value(s, 31, Field.FluidHeat), fraction * Value(s, 30, Field.FluidHeat), 1e-13);
            Near(Caloric(s) - initial, Value(s, 31, Field.FluidHeat), 2e-9);
            Require(Value(s, 22, Field.Temperature) > 300 && Value(s, 22, Field.Temperature) < 340); Balance(s);
        }
        var d = Model(.5); var components = d.Components.ToArray();
        components[Array.FindIndex(components, c => c.Id == 30)] = ComponentDefinition.PressureRelief(30, 5, 0, 1e-12, 200_000, reservoirPressure: 100_000, heat: 3);
        var wall = CompiledModel.Compile(d with { Components = components }).CreateSimulation(); double initialTemperature = Value(wall, 3, Field.Temperature);
        Step(wall, 50_000_000);
        Near(.2 * (Value(wall, 3, Field.Temperature) - initialTemperature), .5 * Value(wall, 30, Field.FluidHeat), 1e-10);
        Near(Value(wall, 0, Field.HeatRejected), 0, 1e-14); Balance(wall);
    }
    private static void External()
    {
        foreach (double speed in new[] { 100d, -100d })
        {
            var s = CompiledModel.Compile(Model(finite: false, speed: speed)).CreateSimulation(); Step(s, 20_000_000);
            double net = Value(s, 21, Field.TotalFuelDelivered) - Value(s, 31, Field.TotalFuelDelivered);
            Near(Value(s, 0, Field.FuelEnergyIn), net * 44e6, 1e-9);
            Near(Value(s, 12, Field.Mass), .0005 + net, 1e-14); Balance(s);
        }
    }
    private static void Reference()
    {
        var law = FuelFilmChecks.Definition(); var gas = FuelFilmChecks.Model().Nodes[1].Gas!;
        double cp = law.LiquidSpecificHeat.Value, sat = law.SaturationTemperature.Value, cv = gas.GasConstant.Value / (gas.Gamma - 1);
        double offset = (cv - cp) * sat - law.LatentInternalEnergy.Value;
        const double time = .05, h = 5e-6, reference = .0005 / 750 - 5e-13 * 800_000;
        double[] x = [100, 800_000, .0005 * (cp * 300 + offset), .0001 * (cp * 340 + offset), 0, 0, 0];
        double[] F(double[] q)
        {
            double rail = 750 * (reference + 5e-13 * q[1]), tank = .0006 - rail;
            double pump = 750e-9 * q[0], flow = 1e-12 * Math.Max(0, q[1] - 300_000), returned = 750 * flow;
            double transfer = pump * q[3] / tank - returned * q[2] / rail, heat = flow * (q[1] - 100_000);
            return [-1e-9 * (q[1] - 100_000) / .02, (1e-9 * q[0] - flow) / 5e-13, transfer, -transfer + heat, pump, returned, heat];
        }
        for (int n = 0; n < (int)Math.Round(time / h); ++n)
        {
            var a = F(x); var b = F(x.Select((v, i) => v + .5 * h * a[i]).ToArray());
            var c = F(x.Select((v, i) => v + .5 * h * b[i]).ToArray()); var d = F(x.Select((v, i) => v + h * c[i]).ToArray());
            for (int i = 0; i < x.Length; ++i) x[i] += h * (a[i] + 2 * b[i] + 2 * c[i] + d[i]) / 6;
        }
        double previous = double.PositiveInfinity;
        foreach (ulong tick in new ulong[] { 1_000_000, 500_000, 250_000 })
        {
            var s = CompiledModel.Compile(Model(tick: tick)).CreateSimulation(); Step(s, 50_000_000);
            double railEnergy = Value(s, 12, Field.InternalEnergy) - .5 * 5e-13 * Math.Pow(Value(s, 5, Field.Pressure), 2);
            double error = Math.Abs(Value(s, 5, Field.Pressure) - x[1]) / 1e6 + Math.Abs(railEnergy - x[2]) / 100 + Math.Abs(Value(s, 22, Field.InternalEnergy) - x[3]) / 100;
            Require(error < previous); previous = error; Balance(s);
        }
        Require(previous < 1e-6);
    }
    private static void Transactions()
    {
        var d = Model(); var s = CompiledModel.Compile(d).CreateSimulation(); var before = Snapshot(s);
        Require(s.Step(10_000_000, new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.Step(10_000_000, [new(5_000_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var fork = s.Fork(); Step(s, 20_000_000); for (int i = 0; i < 20; ++i) Step(fork, 1_000_000); Require(Snapshot(s) == Snapshot(fork));
        var values = new Scalar[s.Model.OutputCount]; for (int i = 0; i < 10; ++i) { Step(s, 50_000); s.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 100; ++i) { Step(s, 50_000); s.ReadSnapshot(values); }
        Require(allocated == GC.GetAllocatedBytesForCurrentThread());
        Throws<ModelCompileException>(() => CompiledModel.Compile(d with { Components = [..d.Components, ComponentDefinition.RailReturn(33, 2, new() { FeedComponent = 21, ValveComponent = 30, FluidHeatFraction = new(0, Unit.Fraction) })] }));
        var components = d.Components.ToArray(); components[^1] = components[^1] with { LiquidRailReturn = components[^1].LiquidRailReturn! with { FluidHeatFraction = new(2, Unit.Fraction) } };
        Throws<ModelCompileException>(() => CompiledModel.Compile(d with { Components = components }));
        var multiple = d with { Components = [..d.Components, ComponentDefinition.PressureRelief(34, 5, 0, 1e-13, 250_000, reservoirPressure: 100_000),
            ComponentDefinition.RailReturn(35, 2, new() { FeedComponent = 21, ValveComponent = 34, FluidHeatFraction = new(.5, Unit.Fraction) })] };
        var m = CompiledModel.Compile(multiple).CreateSimulation(); Step(m, 10_000_000); Require(Value(m, 35, Field.TotalFuelDelivered) > 0); Balance(m);
        // Unsupported tank boiling fails after physical heat transfer and rolls back the full batch.
        var hot = Model(speed: 0); var hotNodes = hot.Nodes.ToArray(); var hotComponents = hot.Components.ToArray();
        hotNodes[Array.FindIndex(hotNodes, n => n.Id == 5)] = NodeDefinition.Hydraulic(5, 5e-13, 1e9);
        int injector = Array.FindIndex(hotComponents, c => c.Id == 12), tank = Array.FindIndex(hotComponents, c => c.Id == 22);
        hotComponents[injector] = hotComponents[injector] with { LiquidFuelInjector = hotComponents[injector].LiquidFuelInjector! with { InitialMass = new(1, Unit.Kilogram), InitialPressure = new(1e9, Unit.Pascal) } };
        hotComponents[tank] = hotComponents[tank] with { LiquidFuelTank = hotComponents[tank].LiquidFuelTank! with { InitialMass = new(1e-6, Unit.Kilogram), InitialTemperature = new(300, Unit.Kelvin) } };
        hotComponents[Array.FindIndex(hotComponents, c => c.Id == 30)] = ComponentDefinition.PressureRelief(30, 5, 0, 1e-15, 200_000, reservoirPressure: 100_000);
        var boiling = CompiledModel.Compile(hot with { Nodes = hotNodes, Components = [..hotComponents, ComponentDefinition.Clutch(32, 4, 0, 10, 8)] }).CreateSimulation();
        before = Snapshot(boiling); Require(boiling.Step(1_000_000) == SimulationStatus.NumericalFailure && Snapshot(boiling) == before);
        Step(boiling, 50_000); Balance(boiling);
    }
}
