// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class LiquidFuelInjectorChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("liquid rail / analytic pressure-head decay / exact pressure work / passive nozzle", Rail),
        ("liquid delivery / finite source and film / sensible chemical and pressure ledgers", Delivery),
        ("liquid metering / latched dose / pressure starvation / reverse-pressure closure", Latching),
        ("liquid metering / reversal and return / no repeated cycle quota", Reversal),
        ("liquid injection and evaporation / independent simultaneous ODE / smooth refinement", Reference),
        ("liquid injection transactions / forks / cancellation / late rollback / zero allocation", Transactions),
        ("liquid histories / speculative clutch capture / complete batching and late rollback", ClutchIntervals),
        ("liquid injection contracts / source volume / units / film ownership / capacity / immutability", Contracts)
    ];
    private static void Ok(SimulationStatus status)
    {
        if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString());
    }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    internal static LiquidFuelInjectorDefinition Definition() => new()
    {
        FilmComponent = 10, CrankNode = 1, CycleAngle = new(4 * Math.PI, Unit.Radian),
        StartAngle = new(.1, Unit.Radian), DurationAngle = new(Math.PI / 2, Unit.Radian), MaximumDose = new(.001, Unit.Kilogram),
        InitialMass = new(.0005, Unit.Kilogram), InitialPressure = new(800_000, Unit.Pascal),
        LiquidDensity = new(750, Unit.KilogramPerCubicMeter), PressureCompliance = new(5e-13, Unit.CubicMeterPerPascal),
        SupplyTemperature = new(300, Unit.Kelvin)
    };
    internal static ModelDefinition Model(ulong tick = 50_000, double dose = 8e-6) => FuelFilmChecks.Model(tick) with
    {
        Nodes = [NodeDefinition.Rotor(1, 1, 2 * Math.PI, .2), FuelFilmChecks.Model().Nodes[1], NodeDefinition.Thermal(3, .2, 500)],
        Components = [ComponentDefinition.LiquidFilm(10, 2, 3, 0, FuelFilmChecks.Definition() with { InitialMass = new(0, Unit.Kilogram) }),
            ComponentDefinition.Torque(11, 1, 100, 0), ComponentDefinition.LiquidFuelMeter(12, 2, 1e-7, .8, 102, dose, Definition())]
    };
    private static void Ledgers(Simulation s)
    {
        Near(Value(s, 0, Field.EnergyResidual), 0, 2e-7);
        Near(Value(s, 0, Field.MassResidual), 0, 2e-16);
        Near(Value(s, 0, Field.FuelResidual), 0, 2e-16);
        Near(Value(s, 0, Field.FreshAirResidual), 0, 2e-16);
    }
    private static void Rail()
    {
        var law = new CompliantLiquidRail(.0005, 800_000, 750, 5e-13, 1e-7, .8);
        var initial = law.StateAfterDelivery(0);
        Require(law.TryAdvance(initial, 100_000, .03, .001, out var transfer));
        double root = Math.Sqrt(700_000) - .8e-7 / (5e-13 * Math.Sqrt(1500)) * .03;
        Near(transfer.Rail.PressurePascals, 100_000 + root * root, 1e-8);
        Near(transfer.DeliveredMassKilograms, 750 * 5e-13 * (800_000 - transfer.Rail.PressurePascals), 1e-16);
        Near(law.StoredPressureEnergy(800_000) - law.StoredPressureEnergy(transfer.Rail.PressurePascals), transfer.SourcePressureWorkJoules, 1e-15);
        Near(transfer.SourcePressureWorkJoules, transfer.ReceiverPressureWorkJoules + transfer.NozzleHeatJoules, 1e-15);
        Require(law.TryAdvance(initial, 100_000, 1, .001, out var starved));
        Near(starved.Rail.PressurePascals, 100_000, 0); Near(starved.DeliveredMassKilograms, 262.5e-6, 1e-16);
        Require(law.TryAdvance(initial, 900_000, 1, .001, out var closed) && closed.Rail == initial && closed.DeliveredMassKilograms == 0);
        Require(!law.TryAdvance(new(.0001, 800_000), 100_000, 1, .001, out _));
        Throws<ArgumentException>(() => new CompliantLiquidRail(1e-6, 800_000, 750, 5e-13, 1e-7, .8));
    }
    private static void Delivery()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation();
        double sourceEnergy = Value(s, 12, Field.InternalEnergy), chemical = Value(s, 0, Field.ChemicalEnergy);
        Ok(s.Step(50_000_000));
        Near(Value(s, 12, Field.TotalFuelDelivered), 8e-6, 1e-16);
        Near(Value(s, 12, Field.Mass), .0005 - 8e-6, 1e-16);
        Near(Value(s, 10, Field.Mass), 8e-6, 1e-16); Near(Value(s, 2, Field.FuelMass), 0, 0);
        Near(Value(s, 10, Field.Temperature), 300, 1e-10); Near(Value(s, 0, Field.ChemicalEnergy), chemical, 1e-8);
        Near(sourceEnergy - Value(s, 12, Field.InternalEnergy), Value(s, 10, Field.InternalEnergy) + Value(s, 12, Field.SourceWork), 1e-10);
        Near(Value(s, 12, Field.SourceWork), Value(s, 12, Field.HydraulicWork) + Value(s, 12, Field.FluidHeat), 1e-14);
        Near(Value(s, 0, Field.SourceWork), -Value(s, 12, Field.HydraulicWork), 1e-14);
        Near(.2 * (Value(s, 3, Field.Temperature) - 500), Value(s, 12, Field.FluidHeat), 1e-10);
        Ledgers(s);
    }
    private static void Latching()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation(); Ok(s.Step(50_000_000));
        Ok(s.SubmitInputs([new(102, 12e-6)])); Ok(s.Step(50_000_000));
        Near(Value(s, 12, Field.RequestedFuelDose), 8e-6, 0); Near(Value(s, 12, Field.TotalFuelDelivered), 8e-6, 1e-16);
        Ok(s.Step(2_000_000_000)); Near(Value(s, 12, Field.TotalFuelDelivered), 20e-6, 1e-16); Ledgers(s);
        var starved = CompiledModel.Compile(Model(dose: .001)).CreateSimulation(); Ok(starved.Step(250_000_000));
        Near(Value(starved, 12, Field.TotalFuelDelivered), 262.5e-6, 1e-15);
        Near(Value(starved, 12, Field.Pressure), 100_000, 1e-8); Ledgers(starved);
        var blockedDefinition = Model();
        blockedDefinition = blockedDefinition with { Components = [..blockedDefinition.Components.Take(2), blockedDefinition.Components[2] with
            { LiquidFuelInjector = Definition() with { InitialPressure = new(50_000, Unit.Pascal) } }] };
        var blocked = CompiledModel.Compile(blockedDefinition).CreateSimulation(); Ok(blocked.Step(50_000_000));
        Near(Value(blocked, 12, Field.TotalFuelDelivered), 0, 0); Ledgers(blocked);
    }
    private static void Reversal()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation(); Ok(s.Step(50_000_000));
        double total = Value(s, 12, Field.TotalFuelDelivered);
        Ok(s.SubmitInputs([new(100, -200)])); Ok(s.Step(100_000_000));
        Require(Value(s, 1, Field.Speed) < 0); Near(Value(s, 12, Field.Opening), 0, 0);
        Ok(s.SubmitInputs([new(100, 200)])); Ok(s.Step(100_000_000));
        Ok(s.SubmitInputs([new(100, 0)])); Ok(s.Step(200_000_000));
        Near(Value(s, 12, Field.TotalFuelDelivered), total, 1e-16); Ledgers(s);
    }
    private static void Reference()
    {
        const double rho = 750, compliance = 5e-13, cv = 287 / .4, sat = 400, latent = 300_000;
        double mass = 1e5 * .01 / (287 * 300);
        double[] initial = [0, .01, 500, mass, mass * cv * 300, 0, 0, 0];
        void Rates(double[] state, double[] rate)
        {
            double railPressure = 800_000 - state[0] / (rho * compliance), receiverPressure = .4 * state[4] / .01;
            Require(railPressure > receiverPressure && state[1] > 0 && state[2] > sat, "Reference left the smooth pressure/phase regime.");
            double flow = .8e-8 * Math.Sqrt(2 * rho * (railPressure - receiverPressure));
            double evaporation = (state[2] - sat) / latent;
            rate[0] = flow; rate[1] = flow - evaporation;
            rate[5] = railPressure * flow / rho; rate[6] = receiverPressure * flow / rho; rate[7] = rate[5] - rate[6];
            rate[2] = (rate[7] - evaporation * latent) / 20;
            rate[3] = evaporation; rate[4] = evaporation * cv * sat;
        }
        double[] Rk4(int steps)
        {
            var state = initial.ToArray(); var stage = new double[8];
            var a = new double[8]; var b = new double[8]; var c = new double[8]; var d = new double[8];
            double dt = .2 / steps;
            for (int step = 0; step < steps; ++step)
            {
                Rates(state, a); for (int i = 0; i < 8; ++i) stage[i] = state[i] + dt / 2 * a[i];
                Rates(stage, b); for (int i = 0; i < 8; ++i) stage[i] = state[i] + dt / 2 * b[i];
                Rates(stage, c); for (int i = 0; i < 8; ++i) stage[i] = state[i] + dt * c[i];
                Rates(stage, d); for (int i = 0; i < 8; ++i) state[i] += dt / 6 * (a[i] + 2 * b[i] + 2 * c[i] + d[i]);
            }
            return state;
        }
        var reference = Rk4(20_000); var refined = Rk4(40_000);
        double[] scales = [1e-4, .01, 100, mass, initial[4], .1, .01, .1];
        for (int i = 0; i < 8; ++i) Near(reference[i], refined[i], 1e-10 * scales[i]);
        var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 20_000_000, 10_000_000, 5_000_000, 2_500_000 })
        {
            var definition = Model(tick, .001) with
            {
                Nodes = [NodeDefinition.Rotor(1, 1, .1, .2), Model().Nodes[1], NodeDefinition.Thermal(3, 20, 500)],
                Components = [ComponentDefinition.LiquidFilm(10, 2, 3, 1, FuelFilmChecks.Definition() with
                    { InitialMass = new(.01, Unit.Kilogram), InitialTemperature = new(sat, Unit.Kelvin) }),
                    ComponentDefinition.LiquidFuelMeter(12, 2, 1e-8, .8, 102, .001, Definition() with { SupplyTemperature = new(sat, Unit.Kelvin) })]
            };
            var s = CompiledModel.Compile(definition).CreateSimulation(); Ok(s.Step(200_000_000)); Ledgers(s);
            double[] actual = [Value(s, 12, Field.TotalFuelDelivered), Value(s, 10, Field.Mass), Value(s, 3, Field.Temperature),
                Value(s, 2, Field.Mass), Value(s, 2, Field.InternalEnergy), Value(s, 12, Field.SourceWork),
                Value(s, 12, Field.HydraulicWork), Value(s, 12, Field.FluidHeat)];
            double error = 0; for (int i = 0; i < 8; ++i) error = Math.Max(error, Math.Abs(actual[i] - reference[i]) / scales[i]);
            if (errors.Count > 0) Require(errors[^1] / error > 3.3 && errors[^1] / error < 4.7, $"Liquid refinement ratio {errors[^1] / error:R}.");
            errors.Add(error);
        }
        Console.WriteLine($"Liquid injection reference: normalized errors {string.Join(", ", errors.Select(e => e.ToString("R")))}");
    }
    private static void Transactions()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation(); Ok(s.Step(500_000)); var before = Snapshot(s);
        Require(s.Step(50_000_000, new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.SubmitInputs([new(102, -.001)]) == SimulationStatus.InvalidInput && Snapshot(s) == before);
        var fork = s.Fork(); Ok(s.Step(5_000_000)); for (int i = 0; i < 10; ++i) Ok(fork.Step(500_000)); Require(Snapshot(s) == Snapshot(fork));
        var rejected = CompiledModel.Compile(Model()).CreateSimulation(); before = Snapshot(rejected);
        Require(rejected.Step(5_000_000, [new(2_500_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(rejected) == before);
        Ok(rejected.Step(5_000_000)); Ledgers(rejected);
        var slow = Model(dose: .001); slow.Components[2] = slow.Components[2] with { Area = new(1e-10, Unit.SquareMeter) };
        var active = CompiledModel.Compile(slow).CreateSimulation(); var values = new Scalar[active.Model.OutputCount];
        for (int i = 0; i < 10; ++i) { Ok(active.Step(1_000_000)); active.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; ++i) { Ok(active.Step(1_000_000)); active.ReadSnapshot(values); }
        Require(allocated == GC.GetAllocatedBytesForCurrentThread(), "Liquid stepping or snapshots allocated.");
        Require(Value(active, 12, Field.TotalFuelDelivered) > 0 && Value(active, 12, Field.TotalFuelDelivered) < .001);
    }
    private static void ClutchIntervals()
    {
        var definition = Model(10_000_000);
        definition.Components[2] = definition.Components[2] with { Area = new(1e-8, Unit.SquareMeter) };
        definition = definition with
        {
            Nodes = [..definition.Nodes, NodeDefinition.Rotor(4, .1, 0)],
            Components = [..definition.Components, ComponentDefinition.Clutch(20, 1, 4, 110, 100, heat: 3)]
        };
        var model = CompiledModel.Compile(definition); var a = model.CreateSimulation(); var b = a.Fork();
        Ok(a.Step(100_000_000)); for (int i = 0; i < 10; ++i) Ok(b.Step(10_000_000));
        Require(Snapshot(a) == Snapshot(b)); Ledgers(a);
        Near(Value(a, 1, Field.Speed), 2 * Math.PI / 1.1, 1e-10);
        Near(Value(a, 4, Field.Speed), Value(a, 1, Field.Speed), 1e-10);
        Near(Value(a, 12, Field.TotalFuelDelivered), 8e-6, 1e-15);
        var rejected = model.CreateSimulation(); var before = Snapshot(rejected);
        Require(rejected.Step(100_000_000, [new(50_000_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(rejected) == before);
    }
    private static void Contracts()
    {
        var model = Model(); var component = model.Components[2];
        foreach (var invalid in new[] { Definition() with { FilmComponent = 11 }, Definition() with { CrankNode = 3 },
            Definition() with { LiquidDensity = new(750, Unit.Kilogram) }, Definition() with { SupplyTemperature = new(401, Unit.Kelvin) },
            Definition() with { PressureCompliance = new(1e-9, Unit.CubicMeterPerPascal) } })
            Require(!CompiledModel.TryCompile(model with { Components = [..model.Components.Take(2), component with { LiquidFuelInjector = invalid }] }, out _, out _));
        var compiled = CompiledModel.Compile(model); model.Components[2] = component with { LiquidFuelInjector = Definition() with { InitialPressure = new(700_000, Unit.Pascal) } };
        Require(compiled.Fingerprint != CompiledModel.Compile(model).Fingerprint);
        var crowded = Model() with { Components = [Model().Components[0], ..Enumerable.Range(0, 14).Select(i =>
            ComponentDefinition.LiquidFuelMeter((uint)(20 + i), 2, 1e-7, .8, (ulong)(200 + i), 8e-6, Definition()))] };
        Require(!CompiledModel.TryCompile(crowded, out _, out var diagnostic) && diagnostic?.Code == DiagnosticCode.Capacity);
    }
}
