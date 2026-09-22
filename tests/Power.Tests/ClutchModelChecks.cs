// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class ClutchModelChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("clutch graph / analytic pair / internal events / signed ratios / heat routing", Pair),
        ("clutch graph / locked RL motor / equivalent inertia / power", Motor),
        ("clutch graph / spring reversal events / analytic convergence / final capture", Oscillator),
        ("clutch graph / shared and redundant constraints / simultaneous engagement", Networks),
        ("clutch graph / moving gas and combustion coupling / independent locked model", Fired),
        ("clutch graph / complete rollback / events / cancellation / forks / allocation", Transactions),
        ("clutch graph / units / domains / capacities / stable IDs / diagnostics", Contracts)
    ];

    private static void Ok(SimulationStatus status) => Require(status == SimulationStatus.Ok, status.ToString());
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static double Value(Simulation s, uint id, Field field)
    {
        var values = new Scalar[s.Model.OutputCount]; s.ReadSnapshot(values);
        return values.Single(v => v.Channel == Channels.Output(id, field)).Value;
    }

    internal static ModelDefinition PairModel(ulong step = 700_000_000, double ratio = 1, double ta = 0, double tb = 0,
        double speedA = 100, double speedB = 0, uint heat = 3, bool ground = false) => new()
    {
        StepNanoseconds = step,
        Nodes = ground ? [NodeDefinition.Rotor(1, .2, speedA), NodeDefinition.Thermal(3, 100, 300)]
            : [NodeDefinition.Rotor(1, .2, speedA), NodeDefinition.Rotor(2, .8, speedB), NodeDefinition.Thermal(3, 100, 300)],
        Components = ground
            ? [ComponentDefinition.Clutch(10, 1, 0, 20, 10, 100, heat: heat), ComponentDefinition.Torque(11, 1, 101, ta)]
            : [ComponentDefinition.Clutch(10, 1, 2, 20, 10, 100, ratio: ratio, heat: heat),
                ComponentDefinition.Torque(11, 1, 101, ta), ComponentDefinition.Torque(12, 2, 102, tb)]
    };

    private static void Pair()
    {
        foreach (double ratio in new[] { -2.0, .5, 1, 2 })
            foreach (var (ta, tb) in new[] { (0.0, 0.0), (-80.0, 0.0), (8.0, -2.0) })
                foreach (uint sink in new uint[] { 0, 3 })
                {
                    var definition = PairModel(ratio: ratio, ta: ta, tb: tb, heat: sink);
                    var model = CompiledModel.Compile(definition); var simulation = model.CreateSimulation();
                    var pair = new ClutchPair(.2, .8, new(20, 10), ratio);
                    double heat = 0, angleA = 0, angleB = 0, work = 0;
                    var state = new ClutchPairState(100, 0);
                    Require(model.HasClutches && model.Fidelity == "hybrid_clutch_powertrain");
                    for (int i = 0; i < 5; ++i)
                    {
                        Require(pair.Advance(state, ta, tb, 1, .7, out var expected) == ClutchStepStatus.Ok);
                        state = expected.State; heat += expected.FrictionHeatJoules; work += expected.ExternalWorkJoules;
                        angleA += expected.AngleAdvanceARadians; angleB += expected.AngleAdvanceBRadians;
                        Ok(simulation.Step(definition.StepNanoseconds));
                        Near(Value(simulation, 1, Field.Speed), state.SpeedARadiansPerSecond, 2e-9);
                        Near(Value(simulation, 2, Field.Speed), state.SpeedBRadiansPerSecond, 2e-9);
                        Near(Value(simulation, 1, Field.Angle), angleA, 3e-9);
                        Near(Value(simulation, 2, Field.Angle), angleB, 3e-9);
                        Near(Value(simulation, 10, Field.FrictionHeat), heat, 3e-8);
                        Near(Value(simulation, 10, Field.Torque), expected.ImpulseAtANewtonMeterSeconds / .7, 2e-9);
                        Near(Value(simulation, 10, Field.HeatFlow), expected.FrictionHeatJoules / .7, 3e-8);
                        Near(Value(simulation, 0, Field.SourceWork), work, 1e-7);
                        Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-7);
                        Near(Value(simulation, 3, Field.Temperature), sink == 0 ? 300 : 300 + heat / 100, 1e-9);
                        Near(Value(simulation, 0, Field.HeatRejected), sink == 0 ? heat : 0, 3e-8);
                    }
                }
        var brake = CompiledModel.Compile(PairModel(ground: true)).CreateSimulation();
        Ok(brake.Step(2_800_000_000));
        Near(Value(brake, 1, Field.Speed), 0, 2e-10);
        Near(Value(brake, 10, Field.FrictionHeat), 1000, 1e-8);
        Near(Value(brake, 3, Field.Temperature), 310, 1e-10);
        Require(Value(brake, 10, Field.ClutchMode) == (int)ClutchMode.Locked);
    }

    private static void Motor()
    {
        const double ratio = -2;
        var d = new ModelDefinition
        {
            StepNanoseconds = 100_000,
            Nodes = [NodeDefinition.Rotor(1, .2), NodeDefinition.Rotor(2, .8), NodeDefinition.Thermal(3, 100, 300)],
            Components = [ComponentDefinition.Clutch(10, 1, 2, 100, 80, 100, ratio: ratio, heat: 3),
                ComponentDefinition.Motor(11, 1, 3, 101, .7, .04, .2, 24), ComponentDefinition.Torque(12, 2, 102, 2),
                ComponentDefinition.Shaft(13, 1, 0, 0, .1, heat: 3), ComponentDefinition.ThermalLink(14, 3, 0, 5, 300)]
        };
        var equivalent = d with
        {
            Nodes = [NodeDefinition.Rotor(1, .2 + .8 / (ratio * ratio)), d.Nodes[2]],
            Components = [d.Components[1], ComponentDefinition.Torque(12, 1, 102, 2 / ratio), d.Components[3], d.Components[4]]
        };
        var s = CompiledModel.Compile(d).CreateSimulation(); var reference = CompiledModel.Compile(equivalent).CreateSimulation();
        for (int i = 0; i < 10; ++i)
        {
            Ok(s.Step(50_000_000)); Ok(reference.Step(50_000_000));
            Near(Value(s, 1, Field.Speed), Value(reference, 1, Field.Speed), 2e-9);
            Near(Value(s, 11, Field.Current), Value(reference, 11, Field.Current), 2e-9);
            Near(Value(s, 3, Field.Temperature), Value(reference, 3, Field.Temperature), 2e-9);
            Near(Value(s, 10, Field.SlipSpeed), 0, 1e-10);
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-7);
            Require(Value(s, 10, Field.ClutchMode) == (int)ClutchMode.Locked);
        }
        Near(Value(s, 10, Field.FrictionHeat), 0, 1e-8);
    }

    private static ModelDefinition OscillatorModel(ulong step) => new()
    {
        StepNanoseconds = step, Nodes = [NodeDefinition.Rotor(1, 1, 0, 1), NodeDefinition.Thermal(2, 100, 300)],
        Components = [ComponentDefinition.Shaft(10, 1, 0, 100, 0), ComponentDefinition.Clutch(11, 1, 0, 21, 10, 100, heat: 2)]
    };
    private static void Oscillator()
    {
        double time = 1.1, phaseStart = 3 * Math.PI / 10;
        double angle = -.1 - .3 * Math.Cos(10 * (time - phaseStart)), speed = 3 * Math.Sin(10 * (time - phaseStart));
        double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 1_000_000, 500_000, 250_000 })
        {
            var s = CompiledModel.Compile(OscillatorModel(step)).CreateSimulation();
            Ok(s.Step(1_100_000_000));
            double error = Math.Max(Math.Abs(Value(s, 1, Field.Angle) - angle), Math.Abs(Value(s, 1, Field.Speed) - speed));
            Require(error < previous / 3.7, $"hybrid oscillator errors: {previous:R} then {error:R}"); previous = error;
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-7);
            Ok(s.Step(300_000_000));
            Near(Value(s, 1, Field.Angle), .2, 1e-7);
            Near(Value(s, 1, Field.Speed), 0, 1e-9);
            Near(Value(s, 11, Field.FrictionHeat), 48, 2e-7);
            Near(Value(s, 2, Field.Temperature), 300.48, 1e-8);
            Require(Value(s, 11, Field.ClutchMode) == (int)ClutchMode.Locked);
        }
        Require(previous < 2e-5);
    }

    private static void Networks()
    {
        foreach (bool slipping in new[] { false, true })
        {
            var d = new ModelDefinition
            {
                StepNanoseconds = 10_000_000,
                Nodes = [NodeDefinition.Rotor(1, .2, slipping ? 20 : 0), NodeDefinition.Rotor(2, .3), NodeDefinition.Rotor(3, .5, slipping ? -10 : 0)],
                Components = [ComponentDefinition.Clutch(10, 1, 2, 30, 20), ComponentDefinition.Clutch(11, 2, 3, 30, 20),
                    ComponentDefinition.Clutch(12, 1, 3, 30, 20), ComponentDefinition.Torque(13, 1, 100, slipping ? 0 : 3),
                    ComponentDefinition.Torque(14, 2, 101, slipping ? 0 : 2), ComponentDefinition.Torque(15, 3, 102, slipping ? 0 : -1)]
            };
            var s = CompiledModel.Compile(d).CreateSimulation(); Ok(s.Step(1_000_000_000));
            foreach (uint id in new uint[] { 1, 2, 3 }) Near(Value(s, id, Field.Speed), slipping ? -1 : 4, 2e-9);
            Near(Value(s, 0, Field.HeatRejected), slipping ? 64.5 : 0, 2e-8);
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-7);
            for (uint id = 10; id <= 12; ++id) Require(Value(s, id, Field.ClutchMode) == (int)ClutchMode.Locked);
        }
    }

    private static void Fired()
    {
        var chamber = NodeDefinition.CylinderGas(2, 1e6, 700);
        chamber = chamber with { Gas = chamber.Gas! with { Premixed = new()
        { LowerHeatingValue = new(44e6, Unit.JoulePerKilogram), StoichiometricAirFuelRatio = 14.7, InitialFractions = new(.005, .995) } } };
        var burn = new WiebeCombustionDefinition { CycleAngle = new(720, Unit.Degree), StartAngle = new(Math.PI + .05, Unit.Radian),
            DurationAngle = new(.3, Unit.Radian), ShapeExponent = 3, BurnCoefficient = 6.9 };
        var d = new ModelDefinition
        {
            StepNanoseconds = 50_000,
            Nodes = [NodeDefinition.Rotor(1, .1, 50, Math.PI), chamber, NodeDefinition.Rotor(3, .2, 50), NodeDefinition.Thermal(4, 100, 300)],
            Components = [ComponentDefinition.GasCylinder(10, 1, 2, MovingCylinderChecks.Geometry),
                ComponentDefinition.GasReservoir(11, 2, 4e-6, 1e5, 300, .8) with { ReservoirFractions = new(0, 1) },
                ComponentDefinition.PremixedCombustion(12, 1, 2, burn), ComponentDefinition.Clutch(13, 1, 3, 1000, 800, 100, heat: 4),
                ComponentDefinition.Torque(14, 3, 101, -2)]
        };
        var referenceDefinition = d with { Nodes = [d.Nodes[0] with { Storage = new(.3, Unit.KilogramMeterSquared) }, chamber, d.Nodes[3]],
            Components = [d.Components[0], d.Components[1], d.Components[2], ComponentDefinition.Torque(14, 1, 101, -2)] };
        var s = CompiledModel.Compile(d).CreateSimulation(); var reference = CompiledModel.Compile(referenceDefinition).CreateSimulation();
        for (int i = 0; i < 10; ++i)
        {
            Ok(s.Step(1_000_000)); Ok(reference.Step(1_000_000));
            Near(Value(s, 1, Field.Speed), Value(reference, 1, Field.Speed), 2e-7);
            Near(Value(s, 2, Field.Pressure), Value(reference, 2, Field.Pressure), .02);
            Near(Value(s, 12, Field.FuelBurned), Value(reference, 12, Field.FuelBurned), 2e-13);
            Near(Value(s, 0, Field.EnergyResidual), 0, 2e-6);
            Near(Value(s, 10, Field.Volume), Value(reference, 10, Field.Volume), 2e-12);
            Require(Value(s, 13, Field.ClutchMode) == (int)ClutchMode.Locked);
        }
        Require(Value(s, 12, Field.FuelBurned) > 0);
        // Slip, capture and thermal routing with independent gas/chemical state present.
        var slipping = d with { Nodes = [d.Nodes[0], chamber, NodeDefinition.Rotor(3, .2), d.Nodes[3]],
            Components = [.. d.Components.Take(3), d.Components[3] with { Friction = new() { StaticCapacity = new(30, Unit.NewtonMeter), SlidingCapacity = new(20, Unit.NewtonMeter) } }, d.Components[4]] };
        s = CompiledModel.Compile(slipping).CreateSimulation(); Ok(s.Step(100_000_000));
        Require(Value(s, 13, Field.FrictionHeat) > 0 && Value(s, 4, Field.Temperature) > 300);
        Near(Value(s, 0, Field.EnergyResidual), 0, 2e-6);
        Near(Value(s, 0, Field.FuelResidual), 0, 1e-14);
        Near(Value(s, 0, Field.FreshAirResidual), 0, 1e-14);
    }

    private static void Transactions()
    {
        var model = CompiledModel.Compile(PairModel(step: 1_000_000));
        var s = model.CreateSimulation(); var initial = Snapshot(s);
        Require(s.SubmitInputs([new(100, 1.1)]) == SimulationStatus.InvalidInput && Snapshot(s) == initial);
        ScheduledInput[] schedule = [new(400_000_000, 100, .5), new(2_000_000_000, 100, 0), new(2_050_000_000, 101, -80), new(2_100_000_000, 100, 1)];
        Ok(s.Step(3_000_000_000, schedule)); var reference = model.CreateSimulation();
        foreach (var item in schedule)
        {
            ulong current = Snapshot(reference).TimeNanoseconds;
            if (item.TimeNanoseconds > current) Ok(reference.Step(item.TimeNanoseconds - current));
            Ok(reference.SubmitInputs([new(item.Channel, item.Value)]));
        }
        Ok(reference.Step(3_000_000_000 - Snapshot(reference).TimeNanoseconds));
        Require(Snapshot(s) == Snapshot(reference));
        var fork = s.Fork(); var parent = Snapshot(s); Ok(fork.SubmitInputs([new(100, 0)])); Ok(fork.Step(50_000_000));
        Require(Snapshot(s) == parent && Snapshot(fork) != parent);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Require(s.Step(1_000_000, cancellation.Token) == SimulationStatus.Cancelled && Snapshot(s) == parent);
        // A successful prefix includes clutch heat and a root; the subsequent overload must roll it all back.
        var failure = CompiledModel.Compile(PairModel(step: 100_000_000, ground: true)).CreateSimulation(); var before = Snapshot(failure);
        Require(failure.Step(3_000_000_000, [new(2_500_000_000, 101, double.MaxValue)]) == SimulationStatus.NumericalFailure);
        Require(Snapshot(failure) == before);
        Ok(failure.Step(2_800_000_000)); Near(Value(failure, 10, Field.FrictionHeat), 1000, 1e-8);
        var values = new Scalar[s.Model.OutputCount];
        for (int i = 0; i < 1000; ++i) { Ok(s.Step(1_000_000)); s.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread(); bool success = true;
        for (int i = 0; i < 2000; ++i) { success &= s.Step(1_000_000) == SimulationStatus.Ok; s.ReadSnapshot(values); }
        Require(success && GC.GetAllocatedBytesForCurrentThread() == allocated);
        // Repeated reversals exercise bisection, variable factors and speculative state copies.
        var reversing = CompiledModel.Compile(PairModel(step: 100_000_000, speedA: 0, ground: true)).CreateSimulation();
        Scalar[] forward = [new(101, 50)], reverse = [new(101, -50)];
        for (int i = 0; i < 100; ++i)
        { Ok(reversing.SubmitInputs(i % 2 == 0 ? forward : reverse)); Ok(reversing.Step(200_000_000)); }
        allocated = GC.GetAllocatedBytesForCurrentThread(); success = true;
        for (int i = 0; i < 100; ++i)
        {
            success &= reversing.SubmitInputs(i % 2 == 0 ? forward : reverse) == SimulationStatus.Ok;
            success &= reversing.Step(200_000_000) == SimulationStatus.Ok;
        }
        Require(success && GC.GetAllocatedBytesForCurrentThread() == allocated);
        Require(Value(reversing, 10, Field.FrictionHeat) > 1000);
    }

    private static void Contracts()
    {
        var d = PairModel(); var model = CompiledModel.Compile(d);
        Require(model.StateCount == 6);
        Require(model.Channels.Single(c => c.Id == 100).Unit == Unit.Fraction);
        Require(model.Channels.Single(c => c.Id == Channels.Output(10, Field.ClutchMode)).Unit == Unit.StateCode);
        Require(model.Channels.Single(c => c.Id == Channels.Output(10, Field.FrictionHeat)).Unit == Unit.Joule);
        Require(model.Fingerprint == CompiledModel.Compile(d with { Nodes = d.Nodes.Reverse().ToArray(), Components = d.Components.Reverse().ToArray() }).Fingerprint);
        void Reject(ComponentDefinition c, DiagnosticCode code)
        {
            Require(!CompiledModel.TryCompile(d with { Components = [c, .. d.Components.Skip(1)] }, out _, out var error));
            Require(error!.Code == code, error.Message);
        }
        Reject(d.Components[0] with { Friction = null }, DiagnosticCode.Schema);
        Reject(d.Components[0] with { NodeB = 1 }, DiagnosticCode.Connection);
        Reject(d.Components[0] with { NodeB = 3 }, DiagnosticCode.Connection);
        Reject(d.Components[0] with { HeatNode = 2 }, DiagnosticCode.Connection);
        Reject(d.Components[0] with { Ratio = 0 }, DiagnosticCode.Range);
        Reject(d.Components[0] with { NodeB = 0, Ratio = 2 }, DiagnosticCode.Range);
        Reject(d.Components[0] with { InitialInput = new(2, Unit.Fraction) }, DiagnosticCode.Range);
        Reject(d.Components[0] with { Friction = new() { StaticCapacity = new(20, Unit.Joule), SlidingCapacity = new(10, Unit.NewtonMeter) } }, DiagnosticCode.Unit);
        Reject(d.Components[0] with { Friction = new() { StaticCapacity = new(5, Unit.NewtonMeter), SlidingCapacity = new(10, Unit.NewtonMeter) } }, DiagnosticCode.Range);
        Reject(d.Components[0] with { Kind = ComponentKind.Shaft }, DiagnosticCode.Schema);
        var immutable = model.CreateSimulation(); d.Components[0] = ComponentDefinition.Clutch(10, 1, 2, 0, 0, 100);
        Ok(immutable.Step(2_800_000_000)); Near(Value(immutable, 10, Field.FrictionHeat), 800, 1e-8);
        Require(model.Fingerprint != CompiledModel.Compile(d).Fingerprint);
    }
}
