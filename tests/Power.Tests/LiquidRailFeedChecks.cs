// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class LiquidRailFeedChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("liquid rail pump / analytic shaft-pressure exchange / work and material boundary", Pump),
        ("liquid rail pump / caloric mixing and inventory / coupled nozzle and complete ledgers", Mixing),
        ("liquid rail pump / signed return flow / source and receiver pressure work", Return),
        ("liquid rail pump / independent simultaneous pressure-mass-temperature ODE / refinement", Reference),
        ("liquid feed transactions / late failure cancellation forks / zero allocations", Transactions),
        ("liquid feed contracts / one rail owner matching pressure storage and thermal reference", Contracts)
    ];
    internal static ModelDefinition Model(ulong tick=20_000,double dose=0,double temperature=340,double speed=100)
    {
        var d=LiquidFuelInjectorChecks.Model(tick,dose);
        return d with{Nodes=[..d.Nodes,NodeDefinition.Rotor(4,.02,speed),NodeDefinition.Hydraulic(5,5e-13,800_000)],
            Components=[..d.Components,ComponentDefinition.DisplacementPump(20,4,5,1e-9,reservoirPressure:100_000),
                ComponentDefinition.RailFeed(21,2,new(){InjectorComponent=12,PumpComponent=20,SupplyTemperature=new(temperature,Unit.Kelvin)})]};
    }
    private static void Ok(SimulationStatus s){if(s!=SimulationStatus.Ok)throw new InvalidOperationException(s.ToString());}
    private static SnapshotInfo Snapshot(Simulation s)=>s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Balance(Simulation s)
    {
        Near(Value(s,0,Field.EnergyResidual),0,2e-7);Near(Value(s,0,Field.MassResidual),0,1e-14);
        Near(Value(s,0,Field.FuelResidual),0,1e-14);Near(Value(s,0,Field.FreshAirResidual),0,1e-14);Near(Value(s,0,Field.HydraulicVolumeResidual),0,1e-16);
    }
    private static void Pump()
    {
        const double d=1e-9,c=5e-13,j=.02,time=.1;double f=d/Math.Sqrt(j*c);
        double expectedW=100*Math.Cos(f*time)-d*700_000/(j*f)*Math.Sin(f*time);
        double expectedP=100_000+700_000*Math.Cos(f*time)+d*100/(c*f)*Math.Sin(f*time);
        var s=CompiledModel.Compile(Model()).CreateSimulation();Ok(s.Step(100_000_000));
        Near(Value(s,4,Field.Speed),expectedW,1e-9);Near(Value(s,5,Field.Pressure),expectedP,1e-5);
        Near(Value(s,12,Field.Pressure),Value(s,5,Field.Pressure),0);Near(Value(s,21,Field.TotalFuelDelivered),750*c*(expectedP-800_000),1e-13);
        Near(Value(s,12,Field.Mass),.0005+Value(s,21,Field.TotalFuelDelivered),1e-13);Require(Value(s,20,Field.HydraulicWork)>0);Balance(s);
    }
    private static void Mixing()
    {
        var s=CompiledModel.Compile(Model(dose:8e-6)).CreateSimulation();Ok(s.Step(100_000_000));double supplied=Value(s,21,Field.TotalFuelDelivered),delivered=Value(s,12,Field.TotalFuelDelivered);
        Near(Value(s,12,Field.Mass)+Value(s,10,Field.Mass),.0005+supplied,1e-13);Near(Value(s,10,Field.Mass),delivered,1e-14);
        Require(supplied>0&&delivered>0&&Value(s,12,Field.Temperature)>300&&Value(s,10,Field.Temperature)>=300);
        Balance(s);
    }
    private static void Return()
    {
        var s=CompiledModel.Compile(Model(speed:-100,temperature:300)).CreateSimulation();Ok(s.Step(20_000_000));
        Require(Value(s,21,Field.TotalFuelDelivered)<0&&Value(s,21,Field.FuelEnergyIn)<0&&Value(s,12,Field.Mass)<.0005);
        Near(Value(s,12,Field.Temperature),300,1e-9);Balance(s);
    }
    private static void Reference()
    {
        double pressureReceiver=Value(CompiledModel.Compile(Model()).CreateSimulation(),2,Field.Pressure);
        var film=FuelFilmChecks.Definition();double cp=film.LiquidSpecificHeat.Value,sat=film.SaturationTemperature.Value;
        var gas=FuelFilmChecks.Model().Nodes[1].Gas!;double vapor=gas.GasConstant.Value/(gas.Gamma-1);double offset=(vapor-cp)*sat-film.LatentInternalEnergy.Value;
        double u0=cp*300+offset,source=cp*340+offset,referenceVolume=.0005/750-5e-13*800_000;
        double[] x=[100,800_000,.0005*u0,0,0];const double h=5e-7,time=.005;
        double[] F(double[] q)
        {
            double mass=750*(referenceVolume+5e-13*q[1]),u=q[2]/mass;
            double injection=.8e-7*Math.Sqrt(2*750*Math.Max(0,q[1]-pressureReceiver)),supply=750*1e-9*q[0];
            return[-1e-9*(q[1]-100_000)/.02,(1e-9*q[0]-injection/750)/5e-13,supply*source-injection*u,injection,injection*u];
        }
        for(int n=0;n<(int)(time/h);++n)
        {
            var a=F(x);var b=F(x.Select((v,i)=>v+.5*h*a[i]).ToArray());var c=F(x.Select((v,i)=>v+.5*h*b[i]).ToArray());var d=F(x.Select((v,i)=>v+h*c[i]).ToArray());
            for(int i=0;i<x.Length;++i)x[i]+=h*(a[i]+2*b[i]+2*c[i]+d[i])/6;
        }
        double previous=double.PositiveInfinity;
        foreach(ulong tick in new ulong[]{100_000,50_000,25_000})
        {
            var s=CompiledModel.Compile(Model(tick,dose:.001)).CreateSimulation();Ok(s.Step(5_000_000));
            double error=Math.Abs(Value(s,5,Field.Pressure)-x[1])/1e6+Math.Abs(Value(s,10,Field.Mass)-x[3])/.001+
                Math.Abs(Value(s,10,Field.InternalEnergy)-x[4])/100;
            Require(error<previous,$"Pump/nozzle refinement {previous:R}/{error:R}");previous=error;Balance(s);
        }
        Require(previous<1e-5);
    }
    private static void Transactions()
    {
        var s=CompiledModel.Compile(Model(dose:8e-6)).CreateSimulation();var before=Snapshot(s);Require(s.Step(10_000_000,new CancellationToken(true))==SimulationStatus.Cancelled&&Snapshot(s)==before);
        var fork=s.Fork();Ok(s.Step(10_000_000));for(int i=0;i<10;++i)Ok(fork.Step(1_000_000));Require(Snapshot(s)==Snapshot(fork));
        before=Snapshot(s);Require(s.Step(10_000_000,[new(before.TimeNanoseconds+5_000_000,100,double.MaxValue)])==SimulationStatus.NumericalFailure&&Snapshot(s)==before);
        var values=new Scalar[s.Model.OutputCount];for(int i=0;i<10;++i){Ok(s.Step(20_000));s.ReadSnapshot(values);}long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<100;++i){Ok(s.Step(20_000));s.ReadSnapshot(values);}Require(bytes==GC.GetAllocatedBytesForCurrentThread());
    }
    private static void Contracts()
    {
        var d=Model();var nodes=d.Nodes.ToArray();nodes[^1]=NodeDefinition.Hydraulic(5,1e-12,800_000);Throws<ModelCompileException>(()=>CompiledModel.Compile(d with{Nodes=nodes}));
        var components=d.Components.ToArray();components[^1]=components[^1] with{LiquidRailFeed=new(){InjectorComponent=12,PumpComponent=20,SupplyTemperature=new(1000,Unit.Kelvin)}};Throws<ModelCompileException>(()=>CompiledModel.Compile(d with{Components=components}));
        Throws<ModelCompileException>(()=>CompiledModel.Compile(d with{Components=[..d.Components,ComponentDefinition.HydraulicResistance(30,5,0,1e-12)]}));
    }
}
