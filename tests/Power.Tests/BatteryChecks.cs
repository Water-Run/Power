// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class BatteryChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("battery law / finite charge / affine chemical energy / signed current / passive heat", Law),
        ("battery polarization / analytic relaxation / conserving thermal transfer", Relaxation),
        ("battery load / analytic RC response and coulomb inventory / second-order refinement", Load),
        ("battery motor / independent RK4 circuit / signed duty / regeneration", Motor),
        ("battery power path / clutch and pump coupling / duty-dependent gear response", Coupling),
        ("battery transactions / late depletion / forks / input atomicity / zero allocation", Transactions)
    ];
    internal static BatteryDefinition Definition(double empty = 12, double full = 12, uint heat = 2) => new()
    {
        EmptyOpenCircuitVoltage = new(empty, Unit.Volt), FullOpenCircuitVoltage = new(full, Unit.Volt),
        SeriesResistance = new(.2, Unit.Ohm), PolarizationResistance = new(.3, Unit.Ohm), PolarizationCapacitance = new(2, Unit.Farad), HeatNode = heat
    };
    internal static ModelDefinition LoadModel(ulong step = 1_000_000, double capacity = 100) => new()
    {
        StepNanoseconds = step, Nodes = [NodeDefinition.BatteryNode(1, capacity, .8, Definition()), NodeDefinition.Thermal(2, 100, 300), NodeDefinition.Thermal(3, 100, 300)],
        Components = [ComponentDefinition.ResistiveLoad(10, 1, 2, 3, 100)]
    };
    internal static ModelDefinition MotorModel(ulong step = 1_000_000, double duty = .5, double speed = 0) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, .02, speed), NodeDefinition.BatteryNode(2, 100, .5, Definition(heat: 3)), NodeDefinition.Thermal(3, 100, 300)],
        Components = [ComponentDefinition.BatteryMotor(10, 1, 2, 3, 100, .5, .02, .1, duty)]
    };
    private static void Ok(SimulationStatus status) { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation simulation) => simulation.ReadSnapshot(new Scalar[simulation.Model.OutputCount]);
    private static void Law()
    {
        var law = new TheveninBattery(100, 10, 14, .2, .3, 2);
        foreach (double soc in new[] { 0.0, .5, 1 }) foreach (double current in new[] { -2.0, 0, 2 })
        {
            Require(law.TryEvaluate(soc, .4, current, out var reaction));
            Near(reaction.OpenCircuitVoltage, 10 + 4 * soc, 0); Near(reaction.TerminalVoltage, 10 + 4 * soc - .4 - .2 * current, 1e-14);
            Near(reaction.ChemicalEnergyJoules, 100 * (10 * soc + 2 * soc * soc), 1e-12);
            Near(reaction.PolarizationEnergyJoules, .16, 1e-15); Near(reaction.HeatFlowWatts, .2 * current * current + .16 / .3, 1e-14);
            double next = soc == 1 ? .9 : soc + .1;
            Near(law.ChemicalEnergy(next) - law.ChemicalEnergy(soc), 100 * law.OpenCircuitVoltage((next + soc) / 2) * (next - soc), 1e-12);
        }
        Require(!law.TryEvaluate(1.01, 0, 0, out var failed) && failed == default);
        Require(!law.TryEvaluate(.5, 0, 100, out failed) && failed == default);
        Throws<ArgumentException>(() => new TheveninBattery(0, 10, 14, .2, .3, 2));
        Throws<ArgumentException>(() => new TheveninBattery(100, 14, 10, .2, .3, 2));
        Throws<ArgumentException>(() => new TheveninBattery(100, 10, 14, .2, 0, 2));
    }
    private static void Relaxation()
    {
        var definition = LoadModel() with { Nodes = [NodeDefinition.BatteryNode(1, 100, .8, Definition(), 1), NodeDefinition.Thermal(2, 100, 300)], Components = [] };
        var simulation = CompiledModel.Compile(definition).CreateSimulation(); Ok(simulation.Step(600_000_000));
        double expected = Math.Pow((1 - .001 / 1.2) / (1 + .001 / 1.2), 600);
        Near(Value(simulation, 1, Field.PolarizationVoltage), expected, 1e-13);
        Near(Value(simulation, 1, Field.StateOfCharge), .8, 0);
        Near(100 * (Value(simulation, 2, Field.Temperature) - 300), 1 - expected * expected, 1e-9);
        Near(Value(simulation, 0, Field.SourceWork), 0, 0); Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-9);
    }
    private static void Load()
    {
        const double time = .4, resistance = 2.2;
        double steady = 12 * .3 / 2.5, tau = 2 / (1 / resistance + 1 / .3);
        double polarization = steady * (1 - Math.Exp(-time / tau));
        double charge = (12 * time - steady * (time - tau * (1 - Math.Exp(-time / tau)))) / resistance;
        double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 4_000_000, 2_000_000, 1_000_000 })
        {
            var simulation = CompiledModel.Compile(LoadModel(step)).CreateSimulation(); Ok(simulation.Step(400_000_000));
            double error = Math.Abs(Value(simulation, 1, Field.PolarizationVoltage) - polarization) + Math.Abs(Value(simulation, 1, Field.Charge) - (80 - charge));
            Require(previous / error > 3.8, $"Battery load refinement {previous:R}/{error:R}"); previous = error;
            double stored = Value(simulation, 1, Field.InternalEnergy), heat = 100 * (Value(simulation, 2, Field.Temperature) + Value(simulation, 3, Field.Temperature) - 600);
            Near(stored + heat, 960, 1e-8); Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8); Near(Value(simulation, 0, Field.SourceWork), 0, 0);
            Near(Value(simulation, 1, Field.BatteryCurrent), Value(simulation, 10, Field.Current), 1e-13);
            Near(Value(simulation, 10, Field.Current), Value(simulation, 1, Field.TerminalVoltage) / 2, 1e-13);
        }
    }
    private static void Motor()
    {
        double[] x = [0, 0, .5, 0], a = new double[4], b = new double[4], c = new double[4], d = new double[4], trial = new double[4]; const double h = .00001;
        void Derivative(double[] s, double[] target)
        {
            double batteryCurrent = .5 * s[1], voltage = 12 - s[3] - .2 * batteryCurrent;
            target[0] = .1 * s[1] / .02; target[1] = (.5 * voltage - .5 * s[1] - .1 * s[0]) / .02;
            target[2] = -batteryCurrent / 100; target[3] = batteryCurrent / 2 - s[3] / .6;
        }
        for (int tick = 0; tick < 40_000; ++tick)
        {
            Derivative(x, a); for (int i = 0; i < 4; ++i) trial[i] = x[i] + h * a[i] / 2;
            Derivative(trial, b); for (int i = 0; i < 4; ++i) trial[i] = x[i] + h * b[i] / 2;
            Derivative(trial, c); for (int i = 0; i < 4; ++i) trial[i] = x[i] + h * c[i];
            Derivative(trial, d); for (int i = 0; i < 4; ++i) x[i] += h * (a[i] + 2 * b[i] + 2 * c[i] + d[i]) / 6;
        }
        double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 2_000_000, 1_000_000, 500_000 })
        {
            var simulation = CompiledModel.Compile(MotorModel(step)).CreateSimulation(); Ok(simulation.Step(400_000_000));
            double error = Math.Abs(Value(simulation, 1, Field.Speed) - x[0]) + Math.Abs(Value(simulation, 10, Field.Current) - x[1]) + Math.Abs(Value(simulation, 2, Field.StateOfCharge) - x[2]) + Math.Abs(Value(simulation, 2, Field.PolarizationVoltage) - x[3]);
            Require(previous / error > 3.8, $"Battery motor refinement {previous:R}/{error:R}"); previous = error;
            Near(Value(simulation, 0, Field.SourceWork), 0, 0); Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-8);
        }
        var reverse = CompiledModel.Compile(MotorModel(duty: -.5)).CreateSimulation(); Ok(reverse.Step(400_000_000));
        Near(Value(reverse, 1, Field.Speed), -x[0], .0001); Require(Value(reverse, 2, Field.BatteryCurrent) > 0);
        var regen = CompiledModel.Compile(MotorModel(speed: 200)).CreateSimulation(); Ok(regen.Step(100_000_000));
        Require(Value(regen, 2, Field.StateOfCharge) > .5 && Value(regen, 2, Field.BatteryCurrent) < 0 && Value(regen, 1, Field.Speed) < 200);
        Near(Value(regen, 0, Field.EnergyResidual), 0, 1e-8);
        // Two identical windings in parallel have half the resistance and inductance.
        var parallelDefinition = MotorModel() with { Components = [ComponentDefinition.BatteryMotor(10, 1, 2, 3, 100, .5, .02, 0, .5),
            ComponentDefinition.BatteryMotor(11, 1, 2, 3, 101, .5, .02, 0, .5)] };
        var equivalentDefinition = MotorModel() with { Components = [ComponentDefinition.BatteryMotor(10, 1, 2, 3, 100, .25, .01, 0, .5)] };
        var parallel = CompiledModel.Compile(parallelDefinition).CreateSimulation(); var equivalent = CompiledModel.Compile(equivalentDefinition).CreateSimulation();
        foreach (double duty in new[] { .5, .8, .2 })
        {
            Ok(parallel.SubmitInputs([new(100, duty), new(101, duty)])); Ok(equivalent.SubmitInputs([new(100, duty)]));
            Ok(parallel.Step(100_000_000)); Ok(equivalent.Step(100_000_000));
            Near(Value(parallel, 10, Field.Current) + Value(parallel, 11, Field.Current), Value(equivalent, 10, Field.Current), 1e-10);
            Near(Value(parallel, 2, Field.StateOfCharge), Value(equivalent, 2, Field.StateOfCharge), 1e-13);
            Near(Value(parallel, 2, Field.PolarizationVoltage), Value(equivalent, 2, Field.PolarizationVoltage), 1e-11);
            Near(Value(parallel, 0, Field.EnergyResidual), 0, 1e-8);
        }
    }
    private static void Coupling()
    {
        var definition = PumpChecks.ClutchModel(); definition = definition with
        {
            Nodes = [..definition.Nodes, NodeDefinition.BatteryNode(5, 100, .8, Definition(heat: 4))],
            Components = [..definition.Components, ComponentDefinition.BatteryMotor(14, 1, 5, 4, 101, 1, .01, .1, .8)]
        };
        var simulation = CompiledModel.Compile(definition).CreateSimulation(); Ok(simulation.Step(600_000_000));
        Require(Value(simulation, 12, Field.ClutchMode) == 1); Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-7);
        var geared = MotorModel() with
        {
            Nodes = [..MotorModel().Nodes, NodeDefinition.Rotor(4, .005)],
            Components = [..MotorModel().Components, ComponentDefinition.IdealGear(11, 1, 4, 2)]
        };
        var pair = CompiledModel.Compile(geared).CreateSimulation(); var equivalent = CompiledModel.Compile(MotorModel() with { Nodes = [NodeDefinition.Rotor(1, .02 + .005 / 4), ..MotorModel().Nodes[1..]] }).CreateSimulation();
        foreach (double duty in new[] { .2, .8, -.3 })
        {
            Ok(pair.SubmitInputs([new(100, duty)])); Ok(equivalent.SubmitInputs([new(100, duty)])); Ok(pair.Step(100_000_000)); Ok(equivalent.Step(100_000_000));
            Near(Value(pair, 1, Field.Speed), Value(equivalent, 1, Field.Speed), 1e-9); Near(Value(pair, 10, Field.Current), Value(equivalent, 10, Field.Current), 1e-9);
            Near(Value(pair, 4, Field.Speed), Value(pair, 1, Field.Speed) / 2, 1e-12); Near(Value(pair, 0, Field.EnergyResidual), 0, 1e-8);
        }
    }
    private static void Transactions()
    {
        var simulation = CompiledModel.Compile(LoadModel(capacity: 1)).CreateSimulation(); var before = Snapshot(simulation);
        Require(simulation.Step(300_000_000) == SimulationStatus.NumericalFailure && Snapshot(simulation) == before);
        Require(simulation.Step(10_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(simulation) == before);
        Require(simulation.SubmitInputs([new(100, 1.1)]) == SimulationStatus.InvalidInput && Snapshot(simulation) == before);
        Ok(simulation.Step(10_000_000)); var parent = Snapshot(simulation); var fork = simulation.Fork(); Ok(fork.SubmitInputs([new(100, 0)])); Ok(fork.Step(10_000_000)); Require(Snapshot(simulation) == parent);
        var motor = CompiledModel.Compile(MotorModel(100_000)).CreateSimulation(); var batched = motor.Fork();
        Ok(motor.Step(100_000_000, [new(25_000_000, 100, .8), new(50_000_000, 100, .2)]));
        for (int i = 0; i < 100; ++i)
        { if (i == 25) Ok(batched.SubmitInputs([new(100, .8)])); if (i == 50) Ok(batched.SubmitInputs([new(100, .2)])); Ok(batched.Step(1_000_000)); }
        Require(Snapshot(motor) == Snapshot(batched)); var buffer = new Scalar[motor.Model.OutputCount];
        for (int i = 0; i < 5; ++i) { Ok(motor.Step(1_000_000)); motor.ReadSnapshot(buffer); }
        long allocated = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 100; ++i) { Ok(motor.Step(1_000_000)); motor.ReadSnapshot(buffer); }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Battery stepping allocated.");
    }
}
