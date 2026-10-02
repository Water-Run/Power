// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class SolenoidChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("solenoid law / exact discrete energy work / analytic derivative / positive inductance", Law),
        ("solenoid stationary RL / analytic current / copper work / second-order refinement", Rl),
        ("solenoid spring motion / independent simultaneous ODE / smooth refinement", Reference),
        ("elastic travel stop / conservative hinge work / derivative / compiled energy", Stops),
        ("actual needle opening / delivery beyond quota and outside window / no fictitious cutoff", PhysicalNeedle),
        ("sampled needle driver / owned voltage / delayed unseating and closure / actual excess dose", Driver),
        ("solenoid and needle transactions / active allocations / cancellation forks and late rollback", Transactions),
        ("needle solenoid / speculative clutch capture / flux heat and sampled histories / atomic failure", ClutchIntervals),
        ("solenoid and driver contracts / dimensions positive inductance ownership period and immutable compile", Contracts)
    ];
    private static void Ok(SimulationStatus status) { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    internal static ModelDefinition Model(ulong tick = 25_000) => new()
    {
        StepNanoseconds = tick,
        Nodes = [NodeDefinition.Translational(1, .002, 0, .00005), NodeDefinition.Thermal(2, .2, 300)],
        Components = [ComponentDefinition.LinearSpring(10, 1, 0, 40000, .5, heat: 2),
            ComponentDefinition.SolenoidCoil(11, 1, 2, 100, 4, .002, 8, voltage: 4, current: .8)]
    };
    internal static ModelDefinition NeedleModel(ulong tick = 10_000)
    {
        var source = LiquidFuelInjectorChecks.Model(tick);
        var injector = source.Components[2] with { LiquidFuelInjector = LiquidFuelInjectorChecks.Definition() with
            { Needle = new() { NeedleNode = 4, ClosedPosition = new(0, Unit.Meter), FullOpenPosition = new(.0002, Unit.Meter) } } };
        return source with
        {
            Nodes = [..source.Nodes, NodeDefinition.Translational(4, .002)],
            Components = [..source.Components.Take(2), injector, ComponentDefinition.LinearSpring(21, 4, 0, 40000, .5, heat: 3, rest: -.00005),
                ComponentDefinition.StrokeStop(22, 4, 0, .0002, 1e6), ComponentDefinition.SolenoidCoil(23, 4, 3, 107, 4, .002, 8),
                ComponentDefinition.NeedleDrive(24, 1, new() { InjectorComponent = 12, SolenoidComponent = 23, SamplePeriodNanoseconds = 100_000, DriveVoltage = new(6.8, Unit.Volt) })]
        };
    }
    private static void Law()
    {
        var law = new LinearGapSolenoid(4, .002, 0, 8);
        foreach (var positions in new[] { (0.0, .0002), (.0002, .00001), (0.0, 0.0) })
            foreach (double voltage in new[] { -4.0, 0.0, 12.0 })
            {
                double flux = law.Inductance(positions.Item1) * .8;
                Require(law.TryInterval(positions.Item1, positions.Item2, flux, voltage, 1e-4, out var response));
                Near(law.Energy(positions.Item2, response.NextFluxWebers) - law.Energy(positions.Item1, flux),
                    response.SupplyWorkJoules - response.CopperHeatJoules - response.ForceNewtons * (positions.Item2 - positions.Item1), 1e-14);
                Require(law.TryInterval(positions.Item1, positions.Item2 + 1e-9, flux, voltage, 1e-4, out var plus));
                Require(law.TryInterval(positions.Item1, positions.Item2 - 1e-9, flux, voltage, 1e-4, out var minus));
                Near(response.ForceDerivativeNewtonsPerMeter, (plus.ForceNewtons - minus.ForceNewtons) / 2e-9,
                    1e-4 + 1e-7 * Math.Abs(response.ForceDerivativeNewtonsPerMeter));
            }
        Require(!law.TryInterval(-.001, 0, 0, 4, 1e-4, out _));
        Throws<ArgumentException>(() => new LinearGapSolenoid(4, 0, 0, 8));
    }
    private static void Rl()
    {
        var law = new LinearGapSolenoid(4, .002, 0, 8); double previous = 0;
        foreach (int steps in new[] { 20, 40, 80, 160 })
        {
            double flux = 0, work = 0, heat = 0, dt = .002 / steps;
            for (int i = 0; i < steps; ++i)
            {
                Require(law.TryInterval(0, 0, flux, 2, dt, out var next)); flux = next.NextFluxWebers;
                work += next.SupplyWorkJoules; heat += next.CopperHeatJoules;
            }
            double error = Math.Abs(law.Current(0, flux) - .5 * (1 - Math.Exp(-4)));
            Near(work - heat, law.Energy(0, flux), 1e-14);
            if (previous != 0) Require(previous / error > 3.8 && previous / error < 4.2);
            previous = error;
        }
    }
    private static void Reference()
    {
        double[] initial = [.00005, 0, (.002 + 8 * .00005) * .8, 0, 0];
        void Rates(double[] state, double[] rate)
        {
            double current = state[2] / (.002 + 8 * state[0]);
            rate[0] = state[1]; rate[1] = (.5 * 8 * current * current - 40000 * state[0] - .5 * state[1]) / .002;
            rate[2] = 4 - 4 * current; rate[3] = 4 * current * current; rate[4] = 4 * current;
        }
        double[] Rk4(int count)
        {
            var state = initial.ToArray(); var stage = new double[5];
            var a = new double[5]; var b = new double[5]; var c = new double[5]; var d = new double[5]; double dt = .005 / count;
            for (int tick = 0; tick < count; ++tick)
            {
                Rates(state, a); for (int i = 0; i < 5; ++i) stage[i] = state[i] + dt / 2 * a[i];
                Rates(stage, b); for (int i = 0; i < 5; ++i) stage[i] = state[i] + dt / 2 * b[i];
                Rates(stage, c); for (int i = 0; i < 5; ++i) stage[i] = state[i] + dt * c[i];
                Rates(stage, d); for (int i = 0; i < 5; ++i) state[i] += dt / 6 * (a[i] + 2 * b[i] + 2 * c[i] + d[i]);
            }
            return state;
        }
        var expected = Rk4(20_000); var refined = Rk4(40_000); double[] scale = [.0001, 1, .005, .1, .1];
        for (int i = 0; i < 5; ++i) Near(expected[i], refined[i], 1e-10 * scale[i]);
        var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 25_000, 12_500, 6_250, 3_125 })
        {
            var s = CompiledModel.Compile(Model(tick)).CreateSimulation(); Ok(s.Step(5_000_000));
            double[] actual = [Value(s, 1, Field.Displacement), Value(s, 1, Field.LinearSpeed),
                Value(s, 11, Field.Current) * (.002 + 8 * Value(s, 1, Field.Displacement)), Value(s, 11, Field.CopperHeat), Value(s, 11, Field.SourceWork)];
            double error = 0; for (int i = 0; i < 5; ++i) error = Math.Max(error, Math.Abs(actual[i] - expected[i]) / scale[i]);
            if (errors.Count > 0) Require(errors[^1] / error > 3.3 && errors[^1] / error < 4.7, $"Solenoid refinement ratio {errors[^1] / error:R}");
            Near(Value(s, 0, Field.EnergyResidual), 0, 1e-8); errors.Add(error);
        }
        Console.WriteLine($"Solenoid coupled reference: normalized errors {string.Join(", ", errors.Select(e => e.ToString("R")))}");
    }
    private static void Stops()
    {
        var law = new ElasticTravelStop(0, .0002, 1e6);
        foreach (var p in new[] { (-2e-6, 5e-6), (.00021, .00019), (-2e-6, .00021), (.00021, .00022) })
        {
            Near(law.Reaction(p.Item1, p.Item2) * (p.Item2 - p.Item1), law.Energy(p.Item1) - law.Energy(p.Item2), 1e-14);
            Near(law.Derivative(p.Item1, p.Item2), (law.Reaction(p.Item1, p.Item2 + 1e-10) - law.Reaction(p.Item1, p.Item2 - 1e-10)) / 2e-10, .1);
        }
        var model = new ModelDefinition { StepNanoseconds = 1_000_000, Nodes = [NodeDefinition.Translational(1, 1, 0, .11)],
            Components = [ComponentDefinition.StrokeStop(10, 1, -.1, .1, 100)] };
        var s = CompiledModel.Compile(model).CreateSimulation(); Ok(s.Step(100_000_000));
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-10);
    }
    private static void PhysicalNeedle()
    {
        var definition = LiquidFuelInjectorChecks.Model(dose: 0);
        definition = definition with { Nodes = [NodeDefinition.Rotor(1, 1, 0, .2), ..definition.Nodes.Skip(1), NodeDefinition.Translational(4, 1, 0, .0001)] };
        definition.Components[2] = definition.Components[2] with { LiquidFuelInjector = LiquidFuelInjectorChecks.Definition() with
            { Needle = new() { NeedleNode = 4, ClosedPosition = new(0, Unit.Meter), FullOpenPosition = new(.0002, Unit.Meter) } } };
        var s = CompiledModel.Compile(definition).CreateSimulation(); Ok(s.Step(50_000_000));
        Require(Value(s, 12, Field.TotalFuelDelivered) > 10e-6); Near(Value(s, 12, Field.Opening), .5, 0);
        Near(Value(s, 12, Field.RequestedFuelDose), 0, 0); Near(Value(s, 0, Field.EnergyResidual), 0, 2e-7);
        var closed = definition with { Nodes = [..definition.Nodes.Take(3), NodeDefinition.Translational(4, 1)] };
        var blocked = CompiledModel.Compile(closed).CreateSimulation(); Ok(blocked.Step(50_000_000)); Near(Value(blocked, 12, Field.TotalFuelDelivered), 0, 0);
    }
    private static void Driver()
    {
        var model = CompiledModel.Compile(NeedleModel()); var s = model.CreateSimulation(); var before = Snapshot(s);
        Require(s.SubmitInputs([new(107, 6.8)]) == SimulationStatus.ControlledInput && Snapshot(s) == before);
        Require(model.TryGetControlCommand(107, out var command) && command.Id == 102 && command.Unit == Unit.Kilogram);
        Ok(s.Step(100_000)); Near(Value(s, 12, Field.TotalFuelDelivered), 0, 0); Require(Value(s, 23, Field.Current) > 0);
        Ok(s.Step(19_900_000)); double delivered = Value(s, 12, Field.TotalFuelDelivered);
        Require(delivered > 8e-6 && delivered < 30e-6, $"Unexpected actuator delivery {delivered:R}");
        Near(Value(s, 24, Field.CommandVoltage), 0, 0); Near(Value(s, 12, Field.Opening), 0, 0);
        Require(Math.Abs(Value(s, 23, Field.Current)) < .01); Near(Value(s, 0, Field.EnergyResidual), 0, 2e-7);
        Ok(s.Step(20_000_000)); double settled = Value(s, 12, Field.TotalFuelDelivered);
        Require(settled >= delivered && settled - delivered < 1e-9, "Unexpected unpowered seat-rebound delivery.");
        Ok(s.Step(40_000_000)); Near(Value(s, 12, Field.TotalFuelDelivered), settled, 1e-15);
        Console.WriteLine($"Needle actuator: requested 8e-6 kg; delivered {settled:R} kg after passive closure and seat rebound.");
    }
    private static void Transactions()
    {
        var model = CompiledModel.Compile(NeedleModel()); var s = model.CreateSimulation(); Ok(s.Step(500_000)); var before = Snapshot(s);
        Require(s.Step(1_000_000, new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        var fork = s.Fork(); Ok(s.Step(1_000_000)); for (int i = 0; i < 10; ++i) Ok(fork.Step(100_000)); Require(Snapshot(s) == Snapshot(fork));
        before = Snapshot(s); Require(s.Step(1_000_000, [new(before.TimeNanoseconds + 500_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var active = CompiledModel.Compile(Model()).CreateSimulation(); var values = new Scalar[active.Model.OutputCount];
        for (int i = 0; i < 10; ++i) { Ok(active.Step(25_000)); active.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; ++i) { Ok(active.Step(25_000)); active.ReadSnapshot(values); }
        Require(allocated == GC.GetAllocatedBytesForCurrentThread(), "Solenoid stepping or snapshots allocated.");
    }
    private static void ClutchIntervals()
    {
        var definition = NeedleModel();
        definition = definition with { Nodes = [..definition.Nodes, NodeDefinition.Rotor(8, .1, 0)],
            Components = [..definition.Components, ComponentDefinition.Clutch(30, 1, 8, 220, 200, heat: 3)] };
        var model = CompiledModel.Compile(definition); var left = model.CreateSimulation(); var right = left.Fork();
        Ok(left.Step(10_000_000)); for (int i = 0; i < 100; ++i) Ok(right.Step(100_000));
        Require(Snapshot(left) == Snapshot(right));
        Near(Value(left, 1, Field.Speed), 2 * Math.PI / 1.1, 1e-10);
        Require(Value(left, 12, Field.TotalFuelDelivered) > 8e-6 && Value(left, 23, Field.CopperHeat) > 0);
        Near(Value(left, 0, Field.EnergyResidual), 0, 2e-7);
        var rejected = model.CreateSimulation(); var before = Snapshot(rejected);
        Require(rejected.Step(10_000_000, [new(5_000_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(rejected) == before);
        Ok(rejected.Step(10_000_000)); Require(Snapshot(rejected) == Snapshot(left));
    }
    private static void Contracts()
    {
        var definition = Model(); var coil = definition.Components[1];
        foreach (var bad in new[] { coil with { NodeA = 2 }, coil with { Solenoid = coil.Solenoid! with { InductanceGradient = new(8, Unit.Henry) } },
            coil with { Solenoid = coil.Solenoid! with { ReferencePosition = new(1, Unit.Meter) } }, coil with { Coupling = new(1, Unit.NewtonMeterPerAmpere) } })
            Require(!CompiledModel.TryCompile(definition with { Components = [definition.Components[0], bad] }, out _, out _));
        var driver = NeedleModel(); var component = driver.Components[^1];
        foreach (var bad in new[] { component.NeedleDriver! with { SamplePeriodNanoseconds = 99_999 }, component.NeedleDriver! with { SolenoidComponent = 21 },
            component.NeedleDriver! with { DriveVoltage = new(0, Unit.Volt) } })
            Require(!CompiledModel.TryCompile(driver with { Components = [..driver.Components.Take(driver.Components.Length - 1), component with { NeedleDriver = bad }] }, out _, out _));
        var compiled = CompiledModel.Compile(definition); definition.Components[1] = coil with { Solenoid = coil.Solenoid! with { InductanceGradient = new(9, Unit.HenryPerMeter) } };
        Require(compiled.Fingerprint != CompiledModel.Compile(definition).Fingerprint);
    }
}
