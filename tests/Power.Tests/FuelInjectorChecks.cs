// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class FuelInjectorChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel window / signed cycle ordinal / wrap / forward-only opening / bounds", Profile),
        ("finite fuel rail / exact limited dose / mass chemical and thermal transfer", Dose),
        ("fuel metering / latched command / finite rail starvation / reverse-pressure closure", Latching),
        ("fuel metering / reversal and return / no repeated cycle quota", Reversal),
        ("fuel port / independent two-vessel ODE / timestep refinement", Reference),
        ("metered premixed burn / delivered reactant / analytic heat / complete ledgers", Burn),
        ("injector transactions / cancellation / late rollback / branches / zero allocation", Transactions),
        ("injector contracts / tracked finite ports / dimensions / timing owner / immutable compile", Contracts)
    ];
    private static void Ok(SimulationStatus status) { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    internal static GasFuelInjectorDefinition Timing() => new()
    { CrankNode = 1, CycleAngle = new(4 * Math.PI, Unit.Radian), StartAngle = new(.1, Unit.Radian), DurationAngle = new(Math.PI / 2, Unit.Radian), MaximumDose = new(.001, Unit.Kilogram) };
    private static NodeDefinition Gas(uint id, double volume, double pressure, double fuel) => NodeDefinition.GasVolume(id, volume, pressure, 300) with
    { Gas = new() { GasConstant = new(287, Unit.JoulePerKilogramKelvin), Gamma = 1.4, Premixed = new() { LowerHeatingValue = new(44e6, Unit.JoulePerKilogram), StoichiometricAirFuelRatio = 14.7, InitialFractions = new(fuel, 1-fuel) } } };
    internal static ModelDefinition Model(ulong step = 50_000, double area = 1e-7, double dose = 5e-7, double angle = 0) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, 1, 2 * Math.PI, angle), Gas(2, .002, 5e5, 1), Gas(3, .01, 1e5, 0)],
        Components = [ComponentDefinition.FuelMeter(10, 2, 3, area, .8, 100, dose, Timing()), ComponentDefinition.Torque(11, 1, 101, 0)]
    };
    private static void Ledgers(Simulation s)
    { Near(Value(s, 0, Field.EnergyResidual), 0, 2e-7); Near(Value(s, 0, Field.MassResidual), 0, 1e-16); Near(Value(s, 0, Field.FuelResidual), 0, 1e-16); Near(Value(s, 0, Field.FreshAirResidual), 0, 1e-16); }
    private static void Profile()
    {
        var p = new FuelDoseProfile(4 * Math.PI, -.1, .5, .001);
        foreach (int turn in new[] { -5, 0, 5 })
        {
            double angle = turn * 4 * Math.PI - .1 + .2; Require(p.TryWindow(angle, 1, out var window) && window.Opening == 1);
            Require(p.TryWindow(angle, -1, out var reverse) && reverse.Opening == 0);
            Require(p.TryWindow(angle, 0, out var stopped) && stopped.Opening == 0);
        }
        Require(!p.TryWindow(double.PositiveInfinity, 1, out _) && !p.TryWindow(1e20, 1, out _));
        Throws<ArgumentException>(() => new FuelDoseProfile(3, 0, 1, .001)); Throws<ArgumentException>(() => new FuelDoseProfile(4*Math.PI, 0, 0, .001));
    }
    private static void Dose()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation(); double rail = Value(s, 2, Field.FuelMass), energy = Value(s, 2, Field.InternalEnergy), receiver = Value(s, 3, Field.InternalEnergy);
        Ok(s.Step(400_000_000)); Near(Value(s, 10, Field.TotalFuelDelivered), 5e-7, 1e-20); Near(Value(s, 10, Field.DeliveredFuelDose), 5e-7, 1e-20);
        Near(Value(s, 3, Field.FuelMass), 5e-7, 1e-20); Near(rail - Value(s, 2, Field.FuelMass), 5e-7, 1e-17);
        Near(energy + receiver, Value(s, 2, Field.InternalEnergy) + Value(s, 3, Field.InternalEnergy), 1e-9);
        Near(Value(s, 0, Field.FuelEnergyIn), 0, 0); Near(Value(s, 0, Field.ReservoirEnthalpy), 0, 0); Near(Value(s, 10, Field.Opening), 0, 0); Ledgers(s);
    }
    private static void Latching()
    {
        var s = CompiledModel.Compile(Model(area: 1e-9, dose: 1e-4)).CreateSimulation(); Ok(s.Step(50_000_000));
        Near(Value(s, 10, Field.RequestedFuelDose), 1e-4, 0); Ok(s.SubmitInputs([new(100, 5e-5)])); Ok(s.Step(100_000_000));
        Near(Value(s, 10, Field.RequestedFuelDose), 1e-4, 0); Require(Value(s, 10, Field.DeliveredFuelDose) < 1e-4); Ledgers(s);
        Ok(s.Step(1_900_000_000)); Near(Value(s, 10, Field.RequestedFuelDose), 5e-5, 0);
        var definition = Model(dose: .0005) with { Nodes = [Model().Nodes[0], Gas(2, 1e-6, 5e5, 1), Gas(3, .01, 1e5, 0)] };
        var starved = CompiledModel.Compile(definition).CreateSimulation(); double available = Value(starved, 2, Field.FuelMass); Ok(starved.Step(300_000_000));
        Require(Value(starved, 10, Field.TotalFuelDelivered) > 0 && Value(starved, 10, Field.TotalFuelDelivered) < available && Value(starved, 10, Field.DeliveredFuelDose) < .0005); Ledgers(starved);
        var blocked = CompiledModel.Compile(Model() with { Nodes = [Model().Nodes[0], Gas(2, .002, 5e4, 1), Gas(3, .01, 1e5, 0)] }).CreateSimulation();
        Ok(blocked.Step(300_000_000)); Near(Value(blocked, 10, Field.TotalFuelDelivered), 0, 0); Ledgers(blocked);
    }
    private static void Reversal()
    {
        var s = CompiledModel.Compile(Model(angle: .3)).CreateSimulation(); Ok(s.Step(10_000_000)); Near(Value(s, 10, Field.TotalFuelDelivered), 5e-7, 1e-20);
        Ok(s.SubmitInputs([new(101, -40*Math.PI)])); Ok(s.Step(100_000_000));
        Ok(s.SubmitInputs([new(101, 40*Math.PI), new(100, 1e-6)])); Ok(s.Step(100_000_000));
        Near(Value(s, 10, Field.TotalFuelDelivered), 5e-7, 1e-20); Near(Value(s, 10, Field.RequestedFuelDose), 5e-7, 0); Ledgers(s);
    }
    private static void Reference()
    {
        // Independent always-choked two-vessel ODE; the small port leaves the dose unsaturated.
        double massA = 5e5 * .002 / (287*300), massB = 1e5 * .01 / (287*300); double[] x = [massA, massB, 2500, 2500], a = new double[4], b = new double[4], c = new double[4], d = new double[4], trial = new double[4];
        const double h = 1e-6, time = .02, area = 5e-6;
        void Derivative(double[] state, double[] rate)
        {
            double temperature = state[2] / (state[0] * (287/.4)), pressure = .4 * state[2] / .002;
            double flow = .8 * area * pressure * Math.Sqrt(1.4/(287*temperature)) * Math.Pow(2/2.4, 2.4/.8), enthalpy = flow * (1.4*287/.4) * temperature;
            rate[0] = -flow; rate[1] = flow; rate[2] = -enthalpy; rate[3] = enthalpy;
        }
        for (int tick=0;tick<20000;++tick)
        {
            Derivative(x,a);for(int i=0;i<4;++i)trial[i]=x[i]+h*a[i]/2;Derivative(trial,b);for(int i=0;i<4;++i)trial[i]=x[i]+h*b[i]/2;
            Derivative(trial,c);for(int i=0;i<4;++i)trial[i]=x[i]+h*c[i];Derivative(trial,d);for(int i=0;i<4;++i)x[i]+=h*(a[i]+2*b[i]+2*c[i]+d[i])/6;
        }
        double previous=double.PositiveInfinity;
        foreach(ulong step in new ulong[]{400_000,200_000,100_000})
        {
            var s=CompiledModel.Compile(Model(step,area,.001,.3)).CreateSimulation();Ok(s.Step((ulong)(time*1e9)));
            double error=Math.Abs(Value(s,2,Field.InternalEnergy)-x[2]);Require(previous/error>3.5,$"Injector refinement {previous:R}/{error:R}");previous=error;Ledgers(s);
        }
    }
    private static void Burn()
    {
        var model=Model() with { Components=[..Model().Components, ComponentDefinition.PremixedCombustion(12,1,3,new(){CycleAngle=new(4*Math.PI,Unit.Radian),StartAngle=new(1,Unit.Radian),DurationAngle=new(1,Unit.Radian),ShapeExponent=3,BurnCoefficient=6.9})] };
        var s=CompiledModel.Compile(model).CreateSimulation();Ok(s.Step(400_000_000));double expected=5e-7*(1-Math.Exp(-6.9));
        Near(Value(s,12,Field.FuelBurned),expected,1e-17);Near(Value(s,12,Field.HeatReleased),expected*44e6,1e-9);
        Near(Value(s,3,Field.FuelMass),5e-7-expected,1e-17);Ledgers(s);
    }
    private static void Transactions()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();Ok(s.Step(20_000_000));var before=Snapshot(s);
        Require(s.Step(50_000_000,cancellationToken:new CancellationToken(true))==SimulationStatus.Cancelled&&Snapshot(s)==before);
        Require(s.Step(50_000_000,[new(30_000_000,100,.002)])==SimulationStatus.InvalidInput&&Snapshot(s)==before);
        var fork=s.Fork();Ok(s.Step(50_000_000));for(int i=0;i<50;++i)Ok(fork.Step(1_000_000));Require(Snapshot(s)==Snapshot(fork));
        Ok(fork.SubmitInputs([new(100,0)]));Ok(fork.Step(2_000_000_000));Require(Snapshot(s)!=Snapshot(fork));
        var values=new Scalar[s.Model.OutputCount];for(int i=0;i<10;++i){Ok(s.Step(1_000_000));s.ReadSnapshot(values);}
        long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;++i){Ok(s.Step(1_000_000));s.ReadSnapshot(values);}Require(GC.GetAllocatedBytesForCurrentThread()==allocated,"Injector stepping allocated.");
        before=Snapshot(s);Require(s.Step(20_000_000,[new(before.TimeNanoseconds+10_000_000,101,double.MaxValue)])==SimulationStatus.NumericalFailure&&Snapshot(s)==before);
    }
    private static void Contracts()
    {
        var definition=Model();var meter=definition.Components[0];
        foreach(var bad in new[]{meter with{NodeB=0},meter with{FuelInjector=Timing() with{CrankNode=2}},meter with{InitialInput=new(.002,Unit.Kilogram)},meter with{InitialInput=new(1,Unit.Fraction)},meter with{FuelInjector=Timing() with{MaximumDose=new(1,Unit.Joule)}},meter with{Area=new(0,Unit.SquareMeter)}})
            Require(!CompiledModel.TryCompile(definition with{Components=[bad,definition.Components[1]]},out _,out _));
        Require(!CompiledModel.TryCompile(definition with{Nodes=[definition.Nodes[0],NodeDefinition.GasVolume(2,.002,5e5,300),definition.Nodes[2]]},out _,out _));
        var incompatible=definition.Nodes[2] with{Gas=definition.Nodes[2].Gas! with{Gamma=1.3}};
        Require(!CompiledModel.TryCompile(definition with{Nodes=[definition.Nodes[0],definition.Nodes[1],incompatible]},out _,out _));
        incompatible=definition.Nodes[2] with{Gas=definition.Nodes[2].Gas! with{Premixed=definition.Nodes[2].Gas!.Premixed! with{LowerHeatingValue=new(50e6,Unit.JoulePerKilogram)}}};
        Require(!CompiledModel.TryCompile(definition with{Nodes=[definition.Nodes[0],definition.Nodes[1],incompatible]},out _,out _));
        var crowded=definition with{Components=[..definition.Components,..Enumerable.Range(0,8).Select(i=>meter with{Id=(uint)(20+i),InputChannel=(ulong)(200+i)})]};
        Require(!CompiledModel.TryCompile(crowded,out _,out var capacity)&&capacity!.Code==DiagnosticCode.Capacity);
        var fast=CompiledModel.Compile(definition with{Nodes=[NodeDefinition.Rotor(1,1,1e6),definition.Nodes[1],definition.Nodes[2]]}).CreateSimulation();var unchanged=Snapshot(fast);
        Require(fast.Step(50_000)==SimulationStatus.NumericalFailure&&Snapshot(fast)==unchanged);
        var compiled=CompiledModel.Compile(definition);definition.Components[0]=meter with{FuelInjector=Timing() with{StartAngle=new(.2,Unit.Radian)}};
        Require(compiled.Fingerprint!=CompiledModel.Compile(definition).Fingerprint);var a=compiled.CreateSimulation();var b=CompiledModel.Compile(Model()).CreateSimulation();Ok(a.Step(40_000_000));Ok(b.Step(40_000_000));Require(Snapshot(a)==Snapshot(b));
    }
}
