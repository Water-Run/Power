// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class GearModelChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("gear graph / exact signed pair / torque channels / initial phase", Pair),
        ("planetary graph / exact free reference / port reactions / conservation", Planetary),
        ("gear graph / multi-stage constraints / reflected inertia / deterministic ordering", Chain),
        ("gear graph / RL motor and thermal coupling / equivalent inertia", Motor),
        ("gear graph / reacting cylinder / equivalent inertia and chemical ledger", Fired),
        ("planetary graph / clutch shift / internal capture / reduction and direct drive", Shift),
        ("gear graph / rollback / cancellation / branches / zero allocation", Transactions),
        ("gear graph / constrained oscillator / analytic second-order convergence", Convergence),
        ("gear graph / strict ports / incompatible initial speeds / constraint rank", Contracts)
    ];
    private static void Ok(SimulationStatus status) => Require(status == SimulationStatus.Ok, status.ToString());
    internal static double Value(Simulation s, uint id, Field field)
    {
        var values = new Scalar[s.Model.OutputCount]; s.ReadSnapshot(values);
        return values.Single(v => v.Channel == Channels.Output(id, field)).Value;
    }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    internal static ModelDefinition PairModel(double ratio = 3, ulong step = 1_000_000) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, .2, ratio * 20, .3), NodeDefinition.Rotor(2, .8, 20, -.7)],
        Components = [ComponentDefinition.IdealGear(10, 1, 2, ratio), ComponentDefinition.Torque(11, 1, 101, 10), ComponentDefinition.Torque(12, 2, 102, -2)]
    };
    internal static ModelDefinition PlanetaryModel(ulong step = 1_000_000) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, .2, 70, .2), NodeDefinition.Rotor(2, .3, -7, -.1), NodeDefinition.Rotor(3, .8, 15, 1.1)],
        Components = [ComponentDefinition.PlanetaryGear(10, 1, 2, 3, 2.5), ComponentDefinition.Torque(11, 1, 101, 12),
            ComponentDefinition.Torque(12, 2, 102, -4), ComponentDefinition.Torque(13, 3, 103, -3)]
    };
    private static void Pair()
    {
        foreach (double ratio in new[] { -4.0, -.5, .5, 1, 3 })
        {
            var model = CompiledModel.Compile(PairModel(ratio)); var s = model.CreateSimulation();
            Require(model.HasGears && model.Fidelity == "constrained_gear_powertrain" && model.Calibration == "unverified");
            Require(model.StateCount == 5 && model.Channels.Single(c => c.Id == Channels.Output(10, Field.TorqueAtB)).Unit == Unit.NewtonMeter);
            Require(Value(s, 10, Field.Torque) == 0 && Value(s, 10, Field.ConstraintError) == 0);
            Require(new IdealGearPair(.2, .8, ratio).Advance(new(ratio * 20, 20), 10, -2, .75, out var exact) == GearStepStatus.Ok);
            Ok(s.Step(750_000_000));
            Near(Value(s, 1, Field.Speed), exact.State.SpeedARadiansPerSecond, 3e-10);
            Near(Value(s, 2, Field.Speed), exact.State.SpeedBRadiansPerSecond, 3e-10);
            Near(Value(s, 1, Field.Angle), .3 + exact.AngleAdvanceARadians, 3e-10);
            Near(Value(s, 2, Field.Angle), -.7 + exact.AngleAdvanceBRadians, 3e-10);
            Near(Value(s, 10, Field.Torque), exact.TorqueAtANewtonMeters, 2e-10);
            Near(Value(s, 10, Field.TorqueAtB), exact.TorqueAtBNewtonMeters, 2e-10);
            Near(Value(s, 10, Field.SlipSpeed), 0, 1e-10);
            Near(Value(s, 10, Field.ConstraintError), 0, 1e-10);
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-7);
        }
    }
    private static void Planetary()
    {
        var s = CompiledModel.Compile(PlanetaryModel()).CreateSimulation();
        Require(new SimplePlanetaryGear(.2, .3, .8, 2.5).Advance(new(70, -7, 15), 12, -4, -3, .7, out var exact) == GearStepStatus.Ok);
        Ok(s.Step(700_000_000));
        Near(Value(s, 1, Field.Speed), exact.State.SunSpeedRadiansPerSecond, 3e-10);
        Near(Value(s, 2, Field.Speed), exact.State.RingSpeedRadiansPerSecond, 3e-10);
        Near(Value(s, 3, Field.Speed), exact.State.CarrierSpeedRadiansPerSecond, 3e-10);
        Near(Value(s, 10, Field.Torque), exact.SunTorqueNewtonMeters, 3e-10);
        Near(Value(s, 10, Field.TorqueAtB), exact.RingTorqueNewtonMeters, 3e-10);
        Near(Value(s, 10, Field.TorqueAtC), exact.CarrierTorqueNewtonMeters, 3e-10);
        Near(Value(s, 10, Field.ConstraintError), 0, 1e-10);
        Near(Value(s, 0, Field.SourceWork), exact.ExternalWorkJoules, 1e-7);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-7);
    }
    private static void Chain()
    {
        var d = new ModelDefinition
        {
            StepNanoseconds = 1_000_000,
            Nodes = [NodeDefinition.Rotor(1, .2, -60), NodeDefinition.Rotor(2, .3, 30), NodeDefinition.Rotor(3, .8, 10)],
            Components = [ComponentDefinition.IdealGear(10, 1, 2, -2), ComponentDefinition.IdealGear(11, 2, 3, 3),
                ComponentDefinition.Torque(12, 1, 100, -10), ComponentDefinition.Torque(13, 3, 101, -2)]
        };
        var m = CompiledModel.Compile(d); var s = m.CreateSimulation();
        var reordered = CompiledModel.Compile(d with { Nodes = d.Nodes.Reverse().ToArray(), Components = d.Components.Reverse().ToArray() });
        Require(m.Fingerprint == reordered.Fingerprint);
        var other = reordered.CreateSimulation(); Ok(s.Step(500_000_000)); Ok(other.Step(500_000_000));
        Require(Snapshot(s) == Snapshot(other));
        double acceleration = 58 / (.2 * 36 + .3 * 9 + .8);
        Near(Value(s, 3, Field.Speed), 10 + .5 * acceleration, 1e-10);
        Near(Value(s, 1, Field.Speed), -6 * Value(s, 3, Field.Speed), 1e-10);
        Near(Value(s, 10, Field.ConstraintError), 0, 1e-10); Near(Value(s, 11, Field.ConstraintError), 0, 1e-10);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-7);
    }
    private static void Motor()
    {
        var d = new ModelDefinition
        {
            StepNanoseconds = 100_000,
            Nodes = [NodeDefinition.Rotor(1, .2), NodeDefinition.Rotor(2, .8), NodeDefinition.Thermal(3, 100, 300)],
            Components = [ComponentDefinition.IdealGear(10, 1, 2, -2), ComponentDefinition.Motor(11, 1, 3, 101, .7, .04, .2, 24),
                ComponentDefinition.Torque(12, 2, 102, 2), ComponentDefinition.Shaft(13, 1, 0, 0, .1, heat: 3), ComponentDefinition.ThermalLink(14, 3, 0, 5, 300)]
        };
        var equivalent = d with
        {
            Nodes = [NodeDefinition.Rotor(1, .4), d.Nodes[2]],
            Components = [d.Components[1], ComponentDefinition.Torque(12, 1, 102, -1), d.Components[3], d.Components[4]]
        };
        var s = CompiledModel.Compile(d).CreateSimulation(); var reference = CompiledModel.Compile(equivalent).CreateSimulation();
        for (int i = 0; i < 10; ++i)
        {
            Ok(s.Step(50_000_000)); Ok(reference.Step(50_000_000));
            Near(Value(s, 1, Field.Speed), Value(reference, 1, Field.Speed), 2e-9);
            Near(Value(s, 11, Field.Current), Value(reference, 11, Field.Current), 2e-9);
            Near(Value(s, 3, Field.Temperature), Value(reference, 3, Field.Temperature), 2e-9);
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-7);
        }
    }
    private static void Fired()
    {
        var d = new ModelDefinition
        {
            StepNanoseconds = 20_000,
            Nodes = [NodeDefinition.Rotor(1, .1, 50, Math.PI), CombustionChecks.Reactive(NodeDefinition.CylinderGas(2, 1e6, 700), .005, .995),
                NodeDefinition.Rotor(3, .4, -25)],
            Components = [ComponentDefinition.GasCylinder(10, 1, 2, MovingCylinderChecks.Geometry),
                ComponentDefinition.PremixedCombustion(12, 1, 2, CombustionChecks.Burn with
                { StartAngle = new(Math.PI + .05, Unit.Radian), DurationAngle = new(.3, Unit.Radian) }), ComponentDefinition.IdealGear(13, 1, 3, -2)]
        };
        var equivalent = d with { Nodes = [d.Nodes[0] with { Storage = new(.2, Unit.KilogramMeterSquared) }, d.Nodes[1]], Components = d.Components.Take(2).ToArray() };
        var s = CompiledModel.Compile(d).CreateSimulation(); var reference = CompiledModel.Compile(equivalent).CreateSimulation();
        for (int i = 0; i < 20; ++i)
        {
            Ok(s.Step(1_000_000)); Ok(reference.Step(1_000_000));
            Near(Value(s, 1, Field.Speed), Value(reference, 1, Field.Speed), 2e-8);
            Near(Value(s, 2, Field.Pressure), Value(reference, 2, Field.Pressure), .001);
            Near(Value(s, 12, Field.FuelBurned), Value(reference, 12, Field.FuelBurned), 1e-12);
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-7);
        }
        Require(Value(s, 12, Field.HeatReleased) > 0);
        double beforeSpeed = Value(s, 3, Field.Speed); Ok(s.Step(d.StepNanoseconds));
        Near(Value(s, 13, Field.TorqueAtB), .4 * (Value(s, 3, Field.Speed) - beforeSpeed) / (d.StepNanoseconds * 1e-9), 2e-8);
    }
    internal static ModelDefinition ShiftModel(ulong step = 1_000_000) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, .2, 70), NodeDefinition.Rotor(2, .3, 0), NodeDefinition.Rotor(3, .8, 20), NodeDefinition.Thermal(4, 100, 300)],
        Components = [ComponentDefinition.PlanetaryGear(10, 1, 2, 3, 2.5), ComponentDefinition.Torque(11, 1, 101, 12), ComponentDefinition.Torque(12, 3, 102, -3),
            ComponentDefinition.Clutch(13, 2, 0, 200, 80, 105, 1, heat: 4), ComponentDefinition.Clutch(14, 1, 2, 100, 30, 106, 0, heat: 4)]
    };
    private static void Shift()
    {
        var s = CompiledModel.Compile(ShiftModel()).CreateSimulation();
        Ok(s.Step(200_000_000));
        double acceleration = (12 - 3 / 3.5) / (.2 + .8 / (3.5 * 3.5));
        double sun = 70 + .2 * acceleration, carrier = sun / 3.5;
        Near(Value(s, 1, Field.Speed), sun, 2e-9); Near(Value(s, 2, Field.Speed), 0, 1e-9);
        var reference = new SimplePlanetaryGear(.2, .3, .8, 2.5);
        Require(reference.Advance(new(sun, 0, carrier), -18, 30, -3, 1, out var free) == GearStepStatus.Ok);
        double slipRate = free.State.SunSpeedRadiansPerSecond - free.State.RingSpeedRadiansPerSecond - sun;
        double capture = -sun / slipRate; Require(capture > 0 && capture < .5);
        Require(reference.Advance(new(sun, 0, carrier), -18, 30, -3, capture, out var arrival) == GearStepStatus.Ok);
        double expected = arrival.State.SunSpeedRadiansPerSecond + (12 - 3) / 1.3 * (.5 - capture);
        double heat = .5 * 30 * sun * capture;
        Ok(s.SubmitInputs([new(105, 0), new(106, 1)])); Ok(s.Step(500_000_000));
        Near(Value(s, 1, Field.Speed), expected, 2e-8); Near(Value(s, 2, Field.Speed), expected, 2e-8); Near(Value(s, 3, Field.Speed), expected, 2e-8);
        Near(Value(s, 14, Field.FrictionHeat), heat, 2e-7);
        Near(Value(s, 4, Field.Temperature), 300 + heat / 100, 2e-8);
        Require(Value(s, 14, Field.ClutchMode) == (int)ClutchMode.Locked);
        Near(Value(s, 0, Field.EnergyResidual), 0, 2e-7);
        Ok(s.SubmitInputs([new(105, 1), new(106, 0)])); Ok(s.Step(600_000_000));
        Near(Value(s, 2, Field.Speed), 0, 1e-9);
        Near(Value(s, 1, Field.Speed), 3.5 * Value(s, 3, Field.Speed), 1e-9);
        Require(Value(s, 13, Field.FrictionHeat) > 0);
        Near(Value(s, 0, Field.EnergyResidual), 0, 3e-7);
    }
    private static void Transactions()
    {
        var model = CompiledModel.Compile(ShiftModel()); var s = model.CreateSimulation(); var initial = Snapshot(s);
        ScheduledInput[] schedule = [new(200_000_000, 105, 0), new(200_000_000, 106, 1), new(900_000_000, 101, double.MaxValue)];
        Require(s.Step(1_000_000_000, schedule) == SimulationStatus.NumericalFailure && Snapshot(s) == initial);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Require(s.Step(100_000_000, cancel.Token) == SimulationStatus.Cancelled && Snapshot(s) == initial);
        Ok(s.Step(700_000_000, schedule.Take(2).ToArray()));
        var fork = s.Fork(); var parent = Snapshot(s);
        Ok(fork.SubmitInputs([new(101, 5)])); Ok(fork.Step(100_000_000)); Require(Snapshot(s) == parent);
        var replay = model.CreateSimulation(); Ok(replay.Step(200_000_000)); Ok(replay.SubmitInputs([new(105, 0), new(106, 1)]));
        for (int i = 0; i < 50; ++i) Ok(replay.Step(10_000_000)); Require(Snapshot(s) == Snapshot(replay));
        var buffer = new Scalar[model.OutputCount];
        for (int i = 0; i < 100; ++i) { Ok(s.Step(1_000_000)); s.ReadSnapshot(buffer); }
        long before = GC.GetAllocatedBytesForCurrentThread(); int failures = 0;
        for (int i = 0; i < 1000; ++i) { if (s.Step(1_000_000) != SimulationStatus.Ok) ++failures; s.ReadSnapshot(buffer); }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(failures == 0 && allocated == 0, $"gear graph allocated {allocated}; failed {failures}");
        var reduction = model.CreateSimulation(); Ok(reduction.Step(200_000_000));
        for (int i = 0; i < 5; ++i)
        {
            var capture = reduction.Fork(); Ok(capture.SubmitInputs([new(105, 0), new(106, 1)]));
            before = GC.GetAllocatedBytesForCurrentThread();
            var status = capture.Step(500_000_000);
            allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Require(status == SimulationStatus.Ok && allocated == 0, $"gear capture: {status}, allocated {allocated}");
            Require(Value(capture, 14, Field.FrictionHeat) > 1 && Value(capture, 14, Field.ClutchMode) == 1);
        }
    }
    private static void Convergence()
    {
        double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 10_000_000, 5_000_000, 2_500_000 })
        {
            var d = new ModelDefinition
            {
                StepNanoseconds = step,
                Nodes = [NodeDefinition.Rotor(1, 1, 0, .5), NodeDefinition.Rotor(2, 4)],
                Components = [ComponentDefinition.IdealGear(10, 1, 2, 2), ComponentDefinition.Shaft(11, 1, 0, 200, 0)]
            };
            var s = CompiledModel.Compile(d).CreateSimulation(); Ok(s.Step(1_000_000_000));
            double error = Math.Abs(Value(s, 1, Field.Angle) - .5 * Math.Cos(10))
                + Math.Abs(Value(s, 1, Field.Speed) + 5 * Math.Sin(10));
            Require(previous / error > 3.9, $"gear oscillator errors: {previous:R}, {error:R}"); previous = error;
            Near(Value(s, 10, Field.ConstraintError), 0, 1e-12);
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-9);
        }
    }

    private static void Contracts()
    {
        void Reject(ModelDefinition d, DiagnosticCode code, string field)
        {
            Require(!CompiledModel.TryCompile(d, out _, out var error) && error!.Code == code && error.Field == field,
                $"expected {code}/{field}, got {error}");
        }
        var pair = PairModel(); var planetary = PlanetaryModel();
        foreach (double ratio in new[] { 0.0, double.NaN, double.PositiveInfinity })
            Reject(pair with { Components = [pair.Components[0] with { Ratio = ratio }] }, DiagnosticCode.Range, "ratio");
        foreach (double ratio in new[] { -2.0, 0, 1, double.NaN })
            Reject(planetary with { Components = [planetary.Components[0] with { Ratio = ratio }] }, DiagnosticCode.Range, "ratio");
        Reject(pair with { Components = [pair.Components[0] with { RestAngle = new(1, Unit.Radian) }] }, DiagnosticCode.Schema, "parameters");
        Reject(pair with { Components = [pair.Components[0] with { NodeB = 0 }] }, DiagnosticCode.Connection, "node_b");
        Reject(pair with { Components = [pair.Components[0] with { NodeC = 2 }] }, DiagnosticCode.Connection, "node_c");
        Reject(planetary with { Components = [planetary.Components[0] with { NodeC = 0 }] }, DiagnosticCode.Connection, "node_c");
        Reject(planetary with { Components = [planetary.Components[0] with { NodeC = 2 }] }, DiagnosticCode.Connection, "node_c");
        Reject(pair with { Nodes = [pair.Nodes[0] with { Initial = new(61, Unit.RadianPerSecond) }, pair.Nodes[1]] }, DiagnosticCode.Connection, "initial_speed");
        Reject(pair with { Components = [pair.Components[0], pair.Components[0] with { Id = 15 }] }, DiagnosticCode.Solver, "gear.constraints");
        Reject(pair with { Components = [pair.Components[0], ComponentDefinition.Clutch(15, 1, 2, 100, 50, ratio: 3)] }, DiagnosticCode.Solver, "clutch.coupling");
        var m = CompiledModel.Compile(planetary); ulong fingerprint = m.Fingerprint;
        planetary.Nodes[2] = NodeDefinition.Rotor(3, 99, 15); planetary.Components[0] = ComponentDefinition.PlanetaryGear(10, 2, 1, 3, 3);
        Require(m.Fingerprint == fingerprint); Ok(m.CreateSimulation().Step(1_000_000));
    }
}
