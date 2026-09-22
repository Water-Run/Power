// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class ConverterChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("converter law / four signed maps / passivity / reaction balance / continuity", Law),
        ("converter maps / interior passivity / units / ownership / diagnostic contracts", Contracts),
        ("converter graph / reverse and coast symmetry / counterrotation / shared ports", Quadrants),
        ("converter graph / analytic fluid coupling / second-order convergence / heat", Coupling),
        ("converter graph / analytic stall / stator reaction / brake and thermal conservation", Stall),
        ("converter graph / ideal gearing / parallel lockup / internal capture", Lockup),
        ("converter graph / rollback / cancellation / replay / forks / allocation", Transactions)
    ];
    private static void Ok(SimulationStatus status) => Require(status == SimulationStatus.Ok, status.ToString());
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    internal static ConverterMap Map(params (double S, double R, double C)[] points) => new(points.Select(p =>
        new ConverterMapPoint(p.S, p.R, new(p.C, Unit.NewtonMeterSecondSquaredPerRadianSquared))));
    internal static TorqueConverterDefinition Maps(bool multiply = false, double k = .002)
    {
        var pump = multiply ? Map((-1, 1, 2*k), (0, 2, k), (.5, 1.5, .5*k), (.85, 1, .15*k), (1, 1, 0)) : Map((-1, 1, 2*k), (1, 1, 0));
        var coast = Map((-1, 1, 2*k), (1, 1, 0));
        return new() { PumpPositive = pump, PumpNegative = pump, TurbinePositive = coast, TurbineNegative = coast };
    }
    internal static ModelDefinition Model(ulong step = 1_000_000, bool multiply = false) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, .5, 100), NodeDefinition.Rotor(2, 1, 20), NodeDefinition.Thermal(3, 100, 300)],
        Components = [ComponentDefinition.TorqueConverter(10, 1, 2, Maps(multiply), 3), ComponentDefinition.Torque(11, 1, 101, 0)]
    };
    private static void Law()
    {
        var law = new TorqueConverter(Maps(true));
        foreach (double pump in new[] { -100.0, -40, 0, 40, 100 })
        foreach (double turbine in new[] { -100.0, -40, 0, 40, 100 })
        {
            Require(law.Evaluate(pump, turbine, out var r) == ConverterEvaluationStatus.Ok);
            Require(r.HeatFlowWatts >= 0 && Math.Abs(r.SpeedRatio) <= 1);
            Near(r.PumpTorqueNewtonMeters + r.TurbineTorqueNewtonMeters + r.StatorTorqueNewtonMeters, 0, 1e-12);
            Near(r.HeatFlowWatts + pump*r.PumpTorqueNewtonMeters + turbine*r.TurbineTorqueNewtonMeters, 0, 1e-10);
            var mode = pump == 0 && turbine == 0 ? ConverterDrive.Stopped : Math.Abs(pump) >= Math.Abs(turbine)
                ? pump > 0 ? ConverterDrive.PumpPositive : ConverterDrive.PumpNegative
                : turbine > 0 ? ConverterDrive.TurbinePositive : ConverterDrive.TurbineNegative;
            Require(r.Drive == mode);
        }
        Require(law.Evaluate(100, 0, out var stall) == ConverterEvaluationStatus.Ok);
        Near(stall.PumpTorqueNewtonMeters, -20, 1e-12); Near(stall.TurbineTorqueNewtonMeters, 40, 1e-12);
        Near(stall.StatorTorqueNewtonMeters, -20, 1e-12); Near(stall.HeatFlowWatts, 2000, 1e-10);
        foreach (double ratio in new[] { -1.0, 0, .5, .85, 1 })
        {
            law.Evaluate(100, 100*ratio - 1e-7, out var a); law.Evaluate(100, 100*ratio + 1e-7, out var b);
            Near(a.PumpTorqueNewtonMeters, b.PumpTorqueNewtonMeters, 1e-6); Near(a.TurbineTorqueNewtonMeters, b.TurbineTorqueNewtonMeters, 1e-6);
        }
        Require(law.Evaluate(double.NaN, 0, out var bad) == ConverterEvaluationStatus.InvalidSpeed && bad == default);
        Require(law.Evaluate(double.MaxValue, 0, out bad) == ConverterEvaluationStatus.NumericalFailure && bad == default);
        // Different explicitly provided reverse coefficients must not be silently mirrored.
        var asym = Maps(true) with { PumpNegative = Map((-1, 1, .004), (0, 1.5, .003), (1, 1, 0)) };
        Require(new TorqueConverter(asym).Evaluate(-100, 0, out var reverse) == ConverterEvaluationStatus.Ok);
        Near(reverse.PumpTorqueNewtonMeters, 30, 1e-12); Near(reverse.TurbineTorqueNewtonMeters, -45, 1e-12);
    }
    private static void Contracts()
    {
        var d = Model();
        void Reject(ComponentDefinition c, DiagnosticCode code, string field)
        {
            Require(!CompiledModel.TryCompile(d with { Components = [c] }, out _, out var error) && error!.Code == code && error.Field == field,
                $"Expected {code}/{field}, got {error}");
        }
        var c = d.Components[0];
        Reject(c with { NodeB = 0 }, DiagnosticCode.Connection, "node_b");
        Reject(c with { NodeB = 1 }, DiagnosticCode.Connection, "node_b");
        Reject(c with { HeatNode = 2 }, DiagnosticCode.Connection, "heat_node");
        Reject(c with { InitialInput = new(1, Unit.None) }, DiagnosticCode.Range, "initial_input");
        Reject(c with { Ratio = 2 }, DiagnosticCode.Schema, "parameters");
        Reject(c with { Converter = c.Converter! with { PumpPositive = null } }, DiagnosticCode.Capacity, "converter.pump_positive");
        foreach (var map in new[] { Map((-1,1,.004), (.5,2,.001), (1,1,0)), Map((-1,1,.004),(0,1,-1),(1,1,0)), Map((-1,1,.004),(1,1,.001)), Map((-1,1,.004),(-1,1,.002),(1,1,0)) })
            Reject(c with { Converter = c.Converter! with { PumpPositive = map } }, DiagnosticCode.Range, "converter.pump_positive");
        Reject(c with { Converter = c.Converter! with { PumpPositive = Map((-1,1,.005),(1,1,0)) } }, DiagnosticCode.Range, "converter.counter_rotation");
        var entries = c.Converter!.PumpPositive!.Points.ToArray(); entries[0] = entries[0] with { CapacityCoefficient = new(.004, Unit.NewtonMeter) };
        Reject(c with { Converter = c.Converter! with { PumpPositive = new(entries) } }, DiagnosticCode.Unit, "converter.pump_positive.capacity_coefficient");
        entries = c.Converter.PumpPositive.Points.ToArray(); var owned = new ConverterMap(entries); entries[0] = default;
        Require(owned.Points[0] == c.Converter.PumpPositive.Points[0]);
        Throws<ArgumentException>(() => new ConverterMap(Enumerable.Repeat(default(ConverterMapPoint), 33)));
        var model = CompiledModel.Compile(d); ulong hash = model.Fingerprint;
        d.Components[0] = c with { Converter = Maps(true) };
        Require(model.Fingerprint == hash && CompiledModel.Compile(d).Fingerprint != hash);
    }
    private static void Quadrants()
    {
        foreach (var speeds in new[] { (100.0,20.0), (20.0,100.0), (100.0,-100.0), (30.0,-100.0), (0.0,0.0) })
        {
            var d=Model(); d=d with { Nodes=[NodeDefinition.Rotor(1,.5,speeds.Item1),NodeDefinition.Rotor(2,1,speeds.Item2),d.Nodes[2]] };
            var s=CompiledModel.Compile(d).CreateSimulation();
            var reverse=CompiledModel.Compile(d with { Nodes=[NodeDefinition.Rotor(1,.5,-speeds.Item1),NodeDefinition.Rotor(2,1,-speeds.Item2),d.Nodes[2]] }).CreateSimulation();
            Ok(s.Step(500_000_000)); Ok(reverse.Step(500_000_000));
            foreach(uint id in new uint[]{1,2}) Near(Value(s,id,Field.Speed),-Value(reverse,id,Field.Speed),1e-10);
            Near(Value(s,10,Field.FluidHeat),Value(reverse,10,Field.FluidHeat),1e-9);
            Near(.5*Value(s,1,Field.Speed)+Value(s,2,Field.Speed),.5*speeds.Item1+speeds.Item2,1e-9);
            Near(Value(s,0,Field.EnergyResidual),0,1e-8);
            // Two converters sharing both ports must sum forces in one simultaneous solve.
            var half=ComponentDefinition.TorqueConverter(10,1,2,Maps(k:.001),3);
            var shared=CompiledModel.Compile(d with { Components=[half,half with { Id=12 }] }).CreateSimulation(); Ok(shared.Step(500_000_000));
            foreach(uint id in new uint[]{1,2}) Near(Value(s,id,Field.Speed),Value(shared,id,Field.Speed),1e-9);
            Near(Value(s,10,Field.FluidHeat),Value(shared,10,Field.FluidHeat)+Value(shared,12,Field.FluidHeat),1e-8);
        }
    }
    private static void Coupling()
    {
        double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 20_000_000, 10_000_000, 5_000_000 })
        {
            var s = CompiledModel.Compile(Model(step)).CreateSimulation(); Ok(s.Step(1_000_000_000));
            // Exact positive-speed solution for C(s)=k(1-s), R=1.
            const double mean = 140.0/3, u0 = 80, beta = 2.0/3, alpha = 1.0/3;
            double a = 3 * .002 * mean, b = 3 * .002 * beta, e = Math.Exp(-a);
            double denom = 1 + b/a*u0*(1-e), slip = u0*e/denom, integral = Math.Log(denom)/b;
            double error = Math.Abs(Value(s,1,Field.Speed) - (mean + beta*slip)) + Math.Abs(Value(s,2,Field.Speed) - (mean-alpha*slip));
            Require(previous/error > 3.9, $"Coupling convergence: {previous:R}, {error:R}"); previous = error;
            Near(Value(s,1,Field.Angle), mean + beta*integral, .002);
            Near(Value(s,2,Field.Angle), mean - alpha*integral, .002);
            Near(.5*Value(s,1,Field.Speed) + Value(s,2,Field.Speed), 70, 1e-10);
            double lost = 2700 - .25*Math.Pow(Value(s,1,Field.Speed),2) - .5*Math.Pow(Value(s,2,Field.Speed),2);
            Near(Value(s,10,Field.FluidHeat), lost, 1e-8); Near(Value(s,3,Field.Temperature), 300+lost/100, 1e-9);
            Near(Value(s,0,Field.EnergyResidual), 0, 1e-8);
        }
        var external = Model() with { Components = [Model().Components[0] with { HeatNode = 0 }] };
        var ext = CompiledModel.Compile(external).CreateSimulation(); Ok(ext.Step(1_000_000_000));
        Near(Value(ext,0,Field.HeatRejected), Value(ext,10,Field.FluidHeat), 1e-10);
        Near(Value(ext,0,Field.EnergyResidual), 0, 1e-8);
    }
    private static void Stall()
    {
        double previous = double.PositiveInfinity;
        foreach (ulong step in new ulong[] { 20_000_000, 10_000_000, 5_000_000 })
        {
            var d = Model(step, true); d = d with { Nodes = [d.Nodes[0], NodeDefinition.Rotor(2,1), d.Nodes[2]],
                Components = [..d.Components, ComponentDefinition.Clutch(12,2,0,1000,500,engagement:1,heat:3)] };
            var s = CompiledModel.Compile(d).CreateSimulation(); Ok(s.Step(1_000_000_000));
            double expected = 100/(1+.002*100/.5), error = Math.Abs(Value(s,1,Field.Speed)-expected);
            Require(previous/error>3.9, $"Stall convergence: {previous:R}, {error:R}"); previous=error;
            Near(Value(s,2,Field.Speed), 0, 1e-9);
            Near(Value(s,10,Field.TorqueAtB), -2*Value(s,10,Field.Torque), 1e-9);
            Near(Value(s,10,Field.TorqueAtC), Value(s,10,Field.Torque), 1e-9);
            Near(Value(s,10,Field.FluidHeat), 2500-.25*Math.Pow(Value(s,1,Field.Speed),2), 1e-7);
            Near(Value(s,0,Field.EnergyResidual), 0, 1e-7);
        }
    }
    internal static ModelDefinition LockupModel()
    {
        var d=Model(multiply:true);
        return d with { Nodes=[..d.Nodes,NodeDefinition.Rotor(4,4,10)], Components=[..d.Components,
            ComponentDefinition.IdealGear(12,2,4,2), ComponentDefinition.Clutch(13,1,2,150,80,channel:102,engagement:0,heat:3)] };
    }
    private static void Lockup()
    {
        var s=CompiledModel.Compile(LockupModel()).CreateSimulation(); Ok(s.Step(100_000_000));
        Require(Value(s,10,Field.FluidHeat)>0); Ok(s.SubmitInputs([new(102,1)])); Ok(s.Step(600_000_000));
        Near(Value(s,1,Field.Speed),Value(s,2,Field.Speed),1e-9); Near(Value(s,2,Field.Speed),2*Value(s,4,Field.Speed),1e-9);
        Require(Value(s,13,Field.ClutchMode)==1 && Value(s,13,Field.FrictionHeat)>1);
        Near(Value(s,10,Field.HeatFlow),0,1e-9); Near(Value(s,10,Field.Torque),0,1e-9);
        Near(Value(s,0,Field.EnergyResidual),0,1e-7);
        Near(Value(s,3,Field.Temperature),300+(Value(s,10,Field.FluidHeat)+Value(s,13,Field.FrictionHeat))/100,1e-8);
    }
    private static void Transactions()
    {
        var model=CompiledModel.Compile(LockupModel()); var s=model.CreateSimulation(); var initial=Snapshot(s);
        ScheduledInput[] schedule=[new(100_000_000,102,1),new(900_000_000,101,double.MaxValue)];
        Require(s.Step(1_000_000_000,schedule)==SimulationStatus.NumericalFailure && Snapshot(s)==initial);
        using var cancel=new CancellationTokenSource(); cancel.Cancel();
        Require(s.Step(100_000_000,cancel.Token)==SimulationStatus.Cancelled && Snapshot(s)==initial);
        Ok(s.Step(700_000_000,schedule.Take(1).ToArray())); var replay=model.CreateSimulation();
        Ok(replay.Step(100_000_000)); Ok(replay.SubmitInputs([new(102,1)])); for(int i=0;i<60;++i) Ok(replay.Step(10_000_000));
        Require(Snapshot(s)==Snapshot(replay)); var parent=Snapshot(s); var child=s.Fork();
        Ok(child.SubmitInputs([new(101,20),new(102,0)])); Ok(child.Step(100_000_000)); Require(Snapshot(s)==parent);
        for(int i=0;i<5;++i)
        {
            var capture=model.CreateSimulation(); Ok(capture.Step(100_000_000)); Ok(capture.SubmitInputs([new(102,1)]));
            long before=GC.GetAllocatedBytesForCurrentThread(); var status=capture.Step(600_000_000); long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Require(status==SimulationStatus.Ok && allocated==0,$"Converter capture: {status}, allocated {allocated}");
        }
    }
}
