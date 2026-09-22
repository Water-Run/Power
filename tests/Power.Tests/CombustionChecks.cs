// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class CombustionChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("combustion / analytic Wiebe exposure / wrap / parameter bounds", Profile),
        ("combustion / closed-vessel analytic fuel and temperature / limiting reactants", Vessel),
        ("premixed transport / constituent conservation / boundary chemical enthalpy", Transport),
        ("combustion / open moving-cylinder independent ODE / second-order convergence", Coupled),
        ("combustion / multiple cylinders on shared and coupled cranks / constituent ledgers", Multiple),
        ("combustion / irreversible reversal / disabled and stopped crank", Irreversibility),
        ("combustion / batch rollback / cancellation / fork / allocation / recovery", Transactions),
        ("combustion / explicit composition / topology / units / capacity", Contracts)
    ];
    internal static PremixedGasDefinition Mixture(double fuel = .05, double air = .95) => new()
    { LowerHeatingValue = new(44e6, Unit.JoulePerKilogram), StoichiometricAirFuelRatio = 14.7, InitialFractions = new(fuel, air) };
    internal static NodeDefinition Reactive(NodeDefinition node, double fuel = .05, double air = .95) =>
        node with { Gas = node.Gas! with { Premixed = Mixture(fuel, air) } };
    internal static WiebeCombustionDefinition Burn => new()
    { CycleAngle = new(360, Unit.Degree), StartAngle = new(.2, Unit.Radian), DurationAngle = new(1, Unit.Radian), ShapeExponent = 3, BurnCoefficient = 6.9 };
    private static ModelDefinition Tank(ulong step = 50_000, double fuel = .05, double air = .95, double angle = 0, double speed = 20) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, 1, speed, angle), Reactive(NodeDefinition.GasVolume(2, .001, 1e5, 300), fuel, air)],
        Components = [ComponentDefinition.PremixedCombustion(10, 1, 2, Burn, 100), ComponentDefinition.Torque(11, 1, 101, 0)]
    };
    private static void Ok(SimulationStatus status) => Require(status == SimulationStatus.Ok, status.ToString());
    private static double Value(Simulation s, uint id, Field field)
    {
        var values = new Scalar[s.Model.OutputCount]; s.ReadSnapshot(values);
        return values.Single(v => v.Channel == Channels.Output(id, field)).Value;
    }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Ledgers(Simulation s, double energy = 2e-8)
    {
        Near(Value(s, 0, Field.EnergyResidual), 0, energy);
        Near(Value(s, 0, Field.MassResidual), 0, 2e-16);
        Near(Value(s, 0, Field.FuelResidual), 0, 2e-17);
        Near(Value(s, 0, Field.FreshAirResidual), 0, 2e-16);
    }

    private static void Profile()
    {
        foreach (double cycle in new[] { 2 * Math.PI, 4 * Math.PI })
        {
            var p = new WiebeBurnProfile(cycle, -.3, 1, 3, 6.9);
            for (int turn = -3; turn <= 3; ++turn)
            {
                double at = turn * cycle - .3;
                Near(p.ExposureBetween(at, at + .5), 6.9 / 8, 1e-13);
                Near(p.ExposureBetween(at - .1, at + 1.1), 6.9, 1e-13);
                Near(p.ExposureBetween(at + 1.1, at + cycle), 0, 1e-13);
                Near(p.ExposureBetween(at + .25, at + .75), 6.9 * (.75 * .75 * .75 - .25 * .25 * .25), 1e-13);
            }
        }
        Throws<ArgumentException>(() => new WiebeBurnProfile(3, 0, 1, 3, 6.9));
        Throws<ArgumentException>(() => new WiebeBurnProfile(2 * Math.PI, 0, 0, 3, 6.9));
        Throws<ArgumentException>(() => new WiebeBurnProfile(2 * Math.PI, 0, 1, .5, 6.9));
        Throws<ArgumentException>(() => new WiebeBurnProfile(2 * Math.PI, 0, 1, 3, double.NaN));
        Throws<ArgumentOutOfRangeException>(() => new WiebeBurnProfile(2 * Math.PI, 0, 1, 3, 6.9).ExposureBetween(1, 0));
    }

    private static void Vessel()
    {
        foreach (var (fuelFraction, airFraction) in new[] { (.05, .95), (.1, .4), (0.0, 1.0), (.1, 0.0) })
        {
            var s = CompiledModel.Compile(Tank(fuel: fuelFraction, air: airFraction)).CreateSimulation();
            double mass = Value(s, 2, Field.Mass), fuel = mass * fuelFraction, air = mass * airFraction;
            double available = Math.Min(fuel, air / 14.7), u0 = Value(s, 2, Field.InternalEnergy);
            foreach (ulong time in new ulong[] { 10_000_000, 20_000_000, 40_000_000, 70_000_000 })
            {
                Ok(s.Step(time - Snapshot(s).TimeNanoseconds));
                double phase = Math.Min(1, Math.Max(0, (20 * time * 1e-9 - .2)));
                double burned = available * (1 - Math.Exp(-6.9 * phase * phase * phase));
                Near(Value(s, 2, Field.FuelMass), fuel - burned, 1e-16);
                Near(Value(s, 2, Field.FreshAirMass), air - burned * 14.7, 1e-15);
                Near(Value(s, 2, Field.Mass), mass, 2e-16);
                Near(Value(s, 2, Field.InternalEnergy), u0 + 44e6 * burned, 2e-8);
                Near(Value(s, 2, Field.Temperature), (u0 + 44e6 * burned) / (mass * 287 / .4), 3e-8);
                Near(Value(s, 10, Field.FuelBurned), burned, 1e-16);
                Near(Value(s, 10, Field.HeatReleased), burned * 44e6, 2e-8); Ledgers(s);
            }
        }
    }

    private static void Transport()
    {
        var definition = new ModelDefinition
        {
            StepNanoseconds = 50_000,
            Nodes = [Reactive(NodeDefinition.GasVolume(1, .002, 4e5, 600), .08, .7), Reactive(NodeDefinition.GasVolume(2, .003, 1e5, 300), 0, .2)],
            Components = [ComponentDefinition.GasOrifice(10, 1, 2, 4e-6, .8)]
        };
        var s = CompiledModel.Compile(definition).CreateSimulation();
        double fuel = Value(s, 1, Field.FuelMass) + Value(s, 2, Field.FuelMass), air = Value(s, 1, Field.FreshAirMass) + Value(s, 2, Field.FreshAirMass);
        Ok(s.Step(200_000_000));
        Near(Value(s, 1, Field.FuelMass) + Value(s, 2, Field.FuelMass), fuel, 1e-17);
        Near(Value(s, 1, Field.FreshAirMass) + Value(s, 2, Field.FreshAirMass), air, 1e-16);
        Require(Value(s, 2, Field.FuelMass) > 0); Ledgers(s);
        var filling = new ModelDefinition
        {
            StepNanoseconds = 50_000, Nodes = [Reactive(NodeDefinition.GasVolume(1, .002, 1e5, 300), 0, 0)],
            Components = [ComponentDefinition.GasReservoir(10, 1, 4e-6, 5e5, 400, .8) with { ReservoirFractions = new(.05, .95) }]
        };
        s = CompiledModel.Compile(filling).CreateSimulation(); double initial = Value(s, 1, Field.Mass);
        Ok(s.Step(100_000_000)); double added = Value(s, 1, Field.Mass) - initial;
        Near(Value(s, 1, Field.FuelMass), added * .05, 1e-17);
        Near(Value(s, 1, Field.FreshAirMass), added * .95, 1e-16);
        Near(Value(s, 0, Field.FuelEnergyIn), added * .05 * 44e6, 2e-8); Ledgers(s);
        // Reverse reservoir transfer carries the volume's actual composition, not the reservoir's.
        var drain = filling with { Nodes = [Reactive(NodeDefinition.GasVolume(1, .002, 8e5, 600), .1, .6)] };
        s = CompiledModel.Compile(drain).CreateSimulation(); initial = Value(s, 1, Field.Mass); Ok(s.Step(100_000_000));
        Near(Value(s, 0, Field.FuelEnergyIn), (Value(s, 1, Field.Mass) - initial) * .1 * 44e6, 2e-8); Ledgers(s);

        // Equal choked inflow/outflow at identical temperature leaves mass, pressure and U
        // constant, while composition approaches the inlet exponentially. Net rates alone
        // cannot choose a safe tracer step when the turnover is this fast.
        var flushing = new ModelDefinition
        {
            StepNanoseconds = 1_000_000, Nodes = [Reactive(NodeDefinition.GasVolume(1, 1e-5, 2e5, 400), 0, 0)],
            Components = [ComponentDefinition.GasReservoir(10, 1, 1e-4, 4e5, 400, .8) with { ReservoirFractions = new(.04, .96) },
                ComponentDefinition.GasReservoir(11, 1, 2e-4, 1e5, 400, .8) with { ReservoirFractions = new(0, 0) }]
        };
        s = CompiledModel.Compile(flushing).CreateSimulation(); initial = Value(s, 1, Field.Mass);
        double flow = .8 * 1e-4 * 4e5 / 20 * Math.Sqrt(1.4 / 287) * Math.Pow(2 / 2.4, 3);
        Require(flow / initial * .001 > 1, "Test must exceed one vessel turnover per outer tick");
        Ok(s.Step(2_000_000)); double remaining = Math.Exp(-flow / initial * .002);
        Near(Value(s, 1, Field.Mass), initial, 1e-16); Near(Value(s, 1, Field.Temperature), 400, 1e-9);
        Near(Value(s, 1, Field.ProductMass) / initial, remaining, 2e-5);
        Near(Value(s, 1, Field.FuelMass) / initial, .04 * (1 - remaining), 2e-5); Ledgers(s);
    }

    private static ModelDefinition Cylinder(ulong step) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, .1, 50, Math.PI), Reactive(NodeDefinition.CylinderGas(2, 1e6, 700), .005, .995)],
        Components = [ComponentDefinition.GasCylinder(10, 1, 2, MovingCylinderChecks.Geometry),
            ComponentDefinition.GasReservoir(11, 2, 4e-6, 1e5, 300, .8) with { ReservoirFractions = new(0, 1) },
            ComponentDefinition.PremixedCombustion(12, 1, 2, Burn with { StartAngle = new(Math.PI + .05, Unit.Radian), DurationAngle = new(.3, Unit.Radian) })]
    };
    private static double[] Reference(double dt)
    {
        const double area = Math.PI * .086 * .086 / 4, radius = .043, rod = .143;
        double volume0 = area * .086 * (1 + 1.0 / 9), mass0 = 1e6 * volume0 / (287 * 700);
        double[] y = [Math.PI, 50, mass0, 1e6 * volume0 / .4, .005 * mass0, .995 * mass0];
        double[] Rate(double[] v)
        {
            double sin = Math.Sin(v[0]), cos = Math.Cos(v[0]), projection = Math.Sqrt(rod * rod - radius * radius * sin * sin);
            double volume = area * .086 / 9 + area * (radius * (1 - cos) + rod - projection);
            double derivative = area * (radius * sin + radius * radius * sin * cos / projection);
            double pressure = .4 * v[3] / volume, temperature = v[3] / (v[2] * 287 / .4);
            double flow = .8 * 4e-6 * pressure / Math.Sqrt(temperature) * Math.Sqrt(1.4 / 287) * Math.Pow(2 / 2.4, 3);
            Require(pressure > 2e5 && v[1] > 0, "Independent reference must remain choked and forward");
            double phase = (v[0] - Math.PI - .05) / .3;
            double hazard = phase is > 0 and < 1 ? 6.9 * 3 * phase * phase / .3 * v[1] : 0;
            double burn = hazard * Math.Min(v[4], v[5] / 14.7);
            return [v[1], (pressure - 1e5) * derivative / .1, -flow,
                -pressure * derivative * v[1] - 1.4 * 287 / .4 * temperature * flow + burn * 44e6,
                -flow * v[4] / v[2] - burn, -flow * v[5] / v[2] - 14.7 * burn];
        }
        for (int i = 0; i < (int)Math.Round(.006 / dt); ++i)
        {
            var a = Rate(y); var b = Rate(y.Select((v, j) => v + dt / 2 * a[j]).ToArray());
            var c = Rate(y.Select((v, j) => v + dt / 2 * b[j]).ToArray()); var d = Rate(y.Select((v, j) => v + dt * c[j]).ToArray());
            for (int j = 0; j < y.Length; ++j) y[j] += dt / 6 * (a[j] + 2 * b[j] + 2 * c[j] + d[j]);
        }
        return y;
    }
    private static void Coupled()
    {
        double[] expected = Reference(1e-6), refined = Reference(.5e-6);
        Require(expected.Select((v, i) => Math.Abs(v - refined[i]) / Math.Abs(v)).Max() < 1e-9, "ODE reference must converge independently");
        double Error(ulong step)
        {
            var s = CompiledModel.Compile(Cylinder(step)).CreateSimulation(); Ok(s.Step(6_000_000)); Ledgers(s, 1e-7);
            double[] actual = [Value(s, 1, Field.Angle), Value(s, 1, Field.Speed), Value(s, 2, Field.Mass), Value(s, 2, Field.InternalEnergy), Value(s, 2, Field.FuelMass), Value(s, 2, Field.FreshAirMass)];
            return actual.Select((v, i) => Math.Abs(v - expected[i]) / Math.Abs(expected[i])).Max();
        }
        double coarse = Error(100_000), fine = Error(50_000), finest = Error(12_500);
        Require(fine < coarse / 2.8 && finest < fine / 8 && finest < 1e-4, $"Reacting open-cylinder errors: {coarse}, {fine}, {finest}");
    }

    private static void Irreversibility()
    {
        var s = CompiledModel.Compile(Tank(speed: 10)).CreateSimulation(); Ok(s.Step(70_000_000));
        // Change velocity by torque, then reverse and retrace the consumed part of the lobe.
        Ok(s.SubmitInputs([new(101, -2000)])); Ok(s.Step(10_000_000)); Ok(s.SubmitInputs([new(101, 0)]));
        double burned = Value(s, 10, Field.FuelBurned); Ok(s.Step(30_000_000));
        Require(Value(s, 10, Field.FuelBurned) == burned);
        Require(Value(s, 10, Field.BurnFrontier) > Value(s, 1, Field.Angle), "Burn frontier must expose why reverse motion cannot burn");
        Ok(s.SubmitInputs([new(101, 2000)])); Ok(s.Step(10_000_000)); Ok(s.SubmitInputs([new(101, 0)])); Ok(s.Step(30_000_000));
        Require(Value(s, 10, Field.FuelBurned) == burned, "Retracing old angles must not reburn fuel");
        Ok(s.Step(10_000_000)); Require(Value(s, 10, Field.FuelBurned) > burned); Ledgers(s);
        s = CompiledModel.Compile(Tank(angle: .7, speed: 0)).CreateSimulation(); Ok(s.Step(50_000_000));
        Require(Value(s, 10, Field.FuelBurned) == 0);
        s = CompiledModel.Compile(Tank()).CreateSimulation(); Ok(s.SubmitInputs([new(100, 0)])); Ok(s.Step(40_000_000));
        Require(Value(s, 10, Field.FuelBurned) == 0); double fuel = Value(s, 2, Field.FuelMass);
        Ok(s.SubmitInputs([new(100, 1)])); Ok(s.Step(20_000_000));
        Near(Value(s, 10, Field.FuelBurned), fuel * (1 - Math.Exp(-6.9 * (1 - .6 * .6 * .6))), 1e-16);
    }

    private static void Multiple()
    {
        foreach (bool shared in new[] { true, false })
        {
            uint crank = shared ? 1U : 5U;
            var secondGas = Reactive(NodeDefinition.CylinderGas(4, 1e5, 500), .005, .995);
            secondGas = secondGas with { Gas = secondGas.Gas! with { Premixed = Mixture(.005, .995) with
                { LowerHeatingValue = new(40e6, Unit.JoulePerKilogram), StoichiometricAirFuelRatio = 17 } } };
            var model = new ModelDefinition
            {
                StepNanoseconds = 50_000,
                Nodes = [NodeDefinition.Rotor(1, .2, 80), Reactive(NodeDefinition.CylinderGas(2, 1e5, 500), .005, .995), secondGas, NodeDefinition.Rotor(5, .2, 80)],
                Components = [ComponentDefinition.GasCylinder(10, 1, 2, MovingCylinderChecks.Geometry),
                    ComponentDefinition.GasCylinder(11, crank, 4, MovingCylinderChecks.Geometry with { Phase = new(Math.PI, Unit.Radian) }),
                    ComponentDefinition.PremixedCombustion(12, 1, 2, Burn),
                    ComponentDefinition.PremixedCombustion(13, crank, 4, Burn with { StartAngle = new(Math.PI + .2, Unit.Radian) }),
                    ComponentDefinition.Shaft(14, 1, 5, 10, .1)]
            };
            var s = CompiledModel.Compile(model).CreateSimulation(); Ok(s.Step(200_000_000)); Ledgers(s, 1e-6);
            Require(Value(s, 12, Field.FuelBurned) > 0 && Value(s, 13, Field.FuelBurned) > 0);
            Near(Value(s, 12, Field.HeatReleased), Value(s, 12, Field.FuelBurned) * 44e6, 1e-12);
            Near(Value(s, 13, Field.HeatReleased), Value(s, 13, Field.FuelBurned) * 40e6, 1e-12);
        }
    }

    private static void Transactions()
    {
        var model = CompiledModel.Compile(Tank()); var s = model.CreateSimulation(); var initial = Snapshot(s);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Require(s.Step(100_000_000, cancel.Token) == SimulationStatus.Cancelled && Snapshot(s) == initial);
        Require(s.Step(100_000_000, [new(20_000_000, 100, .5), new(30_000_000, 101, 1e12)]) == SimulationStatus.NumericalFailure && Snapshot(s) == initial);
        Ok(s.Step(100_000_000)); var reference = model.CreateSimulation(); for (int i = 0; i < 100; ++i) Ok(reference.Step(1_000_000));
        Require(Snapshot(s) == Snapshot(reference));
        var branch = s.Fork(); Ok(branch.SubmitInputs([new(100, 0)])); Ok(branch.Step(250_000_000)); Require(Snapshot(s) == Snapshot(reference));
        var values = new Scalar[model.OutputCount];
        for (int i = 0; i < 1000; ++i) { Ok(s.Step(50_000)); s.ReadSnapshot(values); }
        long before = GC.GetAllocatedBytesForCurrentThread(); bool ok = true;
        for (int i = 0; i < 2000; ++i) { ok &= s.Step(50_000) == SimulationStatus.Ok; s.ReadSnapshot(values); }
        Require(ok && GC.GetAllocatedBytesForCurrentThread() == before);
        var narrow = Tank() with { Components = [ComponentDefinition.PremixedCombustion(10, 1, 2, Burn with { DurationAngle = new(.001, Unit.Radian) })] };
        var unresolved = CompiledModel.Compile(narrow).CreateSimulation(); initial = Snapshot(unresolved);
        Require(unresolved.Step(50_000) == SimulationStatus.NumericalFailure && Snapshot(unresolved) == initial);
        var resolved = CompiledModel.Compile(narrow with { StepNanoseconds = 100 }).CreateSimulation(); Ok(resolved.Step(20_000_000)); Require(Value(resolved, 10, Field.FuelBurned) > 0);
    }

    private static void Contracts()
    {
        var d = Tank();
        void Reject(ModelDefinition definition, DiagnosticCode code) => Require(!CompiledModel.TryCompile(definition, out _, out var error) && error!.Code == code, $"Expected {code}");
        Reject(d with { Nodes = [d.Nodes[0], Reactive(d.Nodes[1], .2, .9)] }, DiagnosticCode.Range);
        Reject(d with { Nodes = [d.Nodes[0], d.Nodes[1] with { Gas = new() { GasConstant = new(287, Unit.JoulePerKilogramKelvin), Gamma = 1.4 } }] }, DiagnosticCode.Connection);
        Reject(d with { Components = [d.Components[0], d.Components[0] with { Id = 12, InputChannel = 102 }] }, DiagnosticCode.Connection);
        Reject(d with { Components = [d.Components[0] with { Combustion = Burn with { ShapeExponent = 0 } }] }, DiagnosticCode.Range);
        Reject(d with { Components = [d.Components[0] with { Combustion = Burn with { StartAngle = new(0, Unit.Kelvin) } }] }, DiagnosticCode.Unit);
        Reject(d with { Components = [ComponentDefinition.GasReservoir(12, 2, 1e-6, 1e5, 300)] }, DiagnosticCode.Schema);
        var incompatible = d with { Nodes = [.. d.Nodes, NodeDefinition.GasVolume(3, .001, 1e5, 300)], Components = [ComponentDefinition.GasOrifice(12, 2, 3, 1e-6)] };
        Reject(incompatible, DiagnosticCode.Connection);
        var changingCrank = Cylinder(50_000) with { Nodes = [.. Cylinder(50_000).Nodes, NodeDefinition.Rotor(3, .1)],
            Components = Cylinder(50_000).Components.Select(c => c.Kind == ComponentKind.PremixedCombustion ? c with { NodeA = 3 } : c).ToArray() };
        Reject(changingCrank, DiagnosticCode.Connection);
        Reject(d with { Nodes = [d.Nodes[0], d.Nodes[1] with { Gas = d.Nodes[1].Gas! with
            { Premixed = Mixture() with { LowerHeatingValue = new(44e6, Unit.Joule) } } }] }, DiagnosticCode.Unit);
        Reject(d with { Nodes = [d.Nodes[0], d.Nodes[1] with { Gas = d.Nodes[1].Gas! with
            { Premixed = Mixture() with { InitialFractions = null } } }] }, DiagnosticCode.Schema);
        Reject(new() { Nodes = Enumerable.Range(1, 13).Select(i => Reactive(NodeDefinition.GasVolume((uint)i, .001, 1e5, 300))).ToArray(), Components = [] }, DiagnosticCode.Capacity);
    }
}
