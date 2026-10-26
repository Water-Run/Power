// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;
namespace Power.Tests;

internal static class EquilibriumDuctChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("equilibrium duct / phase-changing transport complete mass energy momentum and entropy", Phase),
        ("equilibrium duct / uniform equilibrium and conservative finite-wall drag", UniformDrag),
        ("equilibrium duct / independent small-amplitude vapor acoustics / mesh refinement", Acoustic),
        ("equilibrium duct / branches cancellation late rollback and allocation-free state", Transactions),
        ("equilibrium duct / explicit cell ownership material and velocity contracts", Contracts)
    ];
    internal static EquilibriumFuelDefinition Material(bool vapor=false)=>EquilibriumFuelNetworkChecks.Material with
    {Compliance=default,LiquidBulkModulus=new(1.5e9,Unit.Pascal),ReferencePressure=new(vapor?1e6:1e5,Unit.Pascal)};
    internal static ModelDefinition Model(int count=4,bool vapor=false,bool uniform=false,double speed=0,double resistance=0)
    {
        var material=Material(vapor);var eos=new CompressibleFuel(new(new(2000,717.5,400,300000,0),287,material.ReferencePressure.Value,200,600),750,1.5e9,20e6);
        double rhoA,rhoB,t=vapor?500:350;
        if(vapor){rhoA=(uniform?1e5:2e5)/(287*t);rhoB=1e5/(287*t);}
        else{Require(eos.TrySaturation(t,out var sat));rhoA=uniform?1/(.9*sat.LiquidSpecificVolumeCubicMetersPerKilogram+.1*sat.VaporSpecificVolumeCubicMetersPerKilogram):750*(1+2e5/1.5e9);rhoB=1/(.9*sat.LiquidSpecificVolumeCubicMetersPerKilogram+.1*sat.VaporSpecificVolumeCubicMetersPerKilogram);}
        var nodes=new List<NodeDefinition>{NodeDefinition.CompressibleFuelVolume(1,.001,.001*rhoA,t,material),NodeDefinition.CompressibleFuelVolume(2,.001,.001*rhoB,t,material),NodeDefinition.Thermal(3,1,350)};
        var ids=new List<uint>();double volume=.1e-6/count;
        for(int i=0;i<count;++i){uint id=(uint)(100+i);ids.Add(id);nodes.Add(NodeDefinition.CompressibleFuelVolume(id,volume,volume*(vapor&&i<count/2?rhoA:rhoB),t,material,speed));}
        return new(){StepNanoseconds=1000,Nodes=nodes.ToArray(),Components=[ComponentDefinition.EquilibriumDuct(10,1,2,3,new(){Cells=ids,Area=new(1e-6,Unit.SquareMeter),LinearResistance=new(resistance,Unit.PascalSecondPerCubicMeter),QuadraticResistance=new(0,Unit.PascalSecondSquaredPerCubicMeterSquared),FluidHeatFraction=new(.5,Unit.Fraction)})]};
    }
    private static SnapshotInfo Snapshot(Simulation s)=>s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Step(Simulation s,ulong ns){var status=s.Step(ns);Require(status==SimulationStatus.Ok,"Duct failed: "+status);}
    private static void Balance(Simulation s){Near(Value(s,0,Field.EnergyResidual),0,2e-7);Near(Value(s,0,Field.MassResidual),0,1e-14);Near(Value(s,0,Field.FuelResidual),0,1e-14);}
    private static void Phase()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();double initialQuality=Value(s,100,Field.VaporMassFraction),massA=Value(s,1,Field.Mass),massB=Value(s,2,Field.Mass);
        Step(s,100_000);Balance(s);Require(Value(s,100,Field.VaporMassFraction)!=initialQuality&&Value(s,10,Field.EntropyProduced)>0);
        Near(massA-Value(s,1,Field.Mass),Value(s,10,Field.MassAtA),1e-16);Near(Value(s,2,Field.Mass)-massB,Value(s,10,Field.MassAtB),1e-16);
        double momentum=0;for(uint i=100;i<104;++i)momentum+=Value(s,i,Field.Mass)*Value(s,i,Field.LinearSpeed);
        Near(momentum+Value(s,10,Field.JetImpulse),0,1e-12);
        Console.WriteLine($"Equilibrium duct phase: first-cell quality {initialQuality:R} -> {Value(s,100,Field.VaporMassFraction):R}; entropy {Value(s,10,Field.EntropyProduced):R} J/K.");
        var expansion=Model();double reservoirDensity=expansion.Nodes.Single(n=>n.Id==2).Position.Value/.001,liquidDensity=750*(1+2e5/1.5e9);
        expansion=expansion with{Nodes=expansion.Nodes.Select(n=>n.Id==1?n with{Position=new(reservoirDensity*n.Storage.Value,Unit.Kilogram)}:n.Id>=100?n with{Position=new(liquidDensity*n.Storage.Value,Unit.Kilogram)}:n).ToArray()};
        var boiling=CompiledModel.Compile(expansion).CreateSimulation();Near(Value(boiling,100,Field.VaporMassFraction),0,0);Step(boiling,100000);Balance(boiling);
        Require(Value(boiling,100,Field.VaporMassFraction)>0&&Value(boiling,103,Field.VaporMassFraction)>0);
        Near(Value(boiling,100,Field.Mass),Value(boiling,103,Field.Mass),1e-15);Near(Value(boiling,100,Field.LinearSpeed),-Value(boiling,103,Field.LinearSpeed),1e-9);
        Console.WriteLine($"Equilibrium duct expansion: end-cell vapor fraction {Value(boiling,100,Field.VaporMassFraction):R}.");
    }
    private static void UniformDrag()
    {
        var uniform=CompiledModel.Compile(Model(uniform:true)).CreateSimulation();var initial=new Scalar[uniform.Model.OutputCount];uniform.ReadSnapshot(initial);Step(uniform,10_000);Balance(uniform);
        for(uint i=100;i<104;++i){Near(Value(uniform,i,Field.LinearSpeed),0,1e-12);Near(Value(uniform,i,Field.VaporMassFraction),.1,1e-10);}
        var drag=CompiledModel.Compile(Model(vapor:true,uniform:true,speed:2,resistance:1e12)).CreateSimulation();Step(drag,20_000);Balance(drag);
        Require(Value(drag,10,Field.FrictionHeat)>0);Near(Value(drag,10,Field.FluidHeat),Value(drag,10,Field.FrictionHeat)/2,1e-16);
        Near(Value(drag,3,Field.Temperature)-350,Value(drag,10,Field.HeatRejected),3e-12);
    }
    private static void Acoustic()
    {
        var errors=new List<double>();const double length=.1,amplitude=10,temperature=500,pressure=1e5,gamma=1.4;double density=pressure/(287*temperature),sound=Math.Sqrt(gamma*287*temperature),time=1e-5;
        double Bump(double x)=>Math.Exp(-Math.Pow((x-.05)/.01,2));
        double Average(double left,double right,double shift)
        {double sum=0;const int count=64;double h=(right-left)/count;for(int j=0;j<=count;++j)sum+=(j==0||j==count?1:j%2==0?2:4)*Bump(left+j*h+shift);return sum*h/3/(right-left);}
        foreach(int count in new[]{8,16,24})
        {
            var d=Model(count,true,true);double dx=length/count;
            d=d with{Nodes=d.Nodes.Select(n=>
            {if(n.Id<100)return n;double perturbation=amplitude*Average((n.Id-100)*dx,(n.Id-99)*dx,0),rho=density+perturbation/(sound*sound);
             return NodeDefinition.CompressibleFuelVolume(n.Id,n.Storage.Value,n.Storage.Value*rho,(pressure+perturbation)/(287*rho),Material(true));}).ToArray()};
            var s=CompiledModel.Compile(d).CreateSimulation();Step(s,10000);Balance(s);double error=0;
            for(int i=0;i<count;++i){double exact=pressure+.5*amplitude*(Average(i*dx,(i+1)*dx,-sound*time)+Average(i*dx,(i+1)*dx,sound*time));error+=Math.Abs(Value(s,(uint)(100+i),Field.Pressure)-exact)/amplitude/count;}
            errors.Add(error);
        }
        Console.WriteLine("Equilibrium duct acoustic mesh errors: "+string.Join(", ",errors.Select(x=>x.ToString("R"))));Require(errors[2]<errors[1]&&errors[1]<errors[0]);
    }
    private static void Transactions()
    {
        var d=Model();d=d with{Nodes=[..d.Nodes,NodeDefinition.Rotor(4,.1)],Components=[..d.Components,ComponentDefinition.Torque(20,4,100,0)]};
        var s=CompiledModel.Compile(d).CreateSimulation();var fork=s.Fork();Step(s,20000);for(int i=0;i<20;++i)Step(fork,1000);Require(Snapshot(s)==Snapshot(fork));var before=Snapshot(s);
        Require(s.Step(10000,cancellationToken:new CancellationToken(true))==SimulationStatus.Cancelled&&Snapshot(s)==before);
        Require(s.Step(10000,[new(before.TimeNanoseconds+5000,100,double.MaxValue)])==SimulationStatus.NumericalFailure&&Snapshot(s)==before);
        Step(fork,1000);Require(Snapshot(s)==before);
        var values=new Scalar[s.Model.OutputCount];for(int i=0;i<20;++i){Require(s.Step(1000)==SimulationStatus.Ok);s.ReadSnapshot(values);}
        long allocation=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<20;++i){Require(s.Step(1000)==SimulationStatus.Ok);s.ReadSnapshot(values);}Require(GC.GetAllocatedBytesForCurrentThread()==allocation);
    }
    private static void Contracts()
    {
        var d=Model();var duct=d.Components[0];var bad=d with{Components=[duct with{FuelDuct=duct.FuelDuct! with{Cells=[100,100]}}]};Require(!CompiledModel.TryCompile(bad,out _,out _));
        bad=d with{Components=[duct with{FuelDuct=duct.FuelDuct! with{Cells=[1,100]}}]};Require(!CompiledModel.TryCompile(bad,out _,out _));
        bad=d with{Nodes=d.Nodes.Select(n=>n.Id==1?n with{InitialVelocity=new(1,Unit.MeterPerSecond)}:n).ToArray()};Require(!CompiledModel.TryCompile(bad,out _,out _));
        bad=d with{Nodes=d.Nodes.Select(n=>n.Id==100?n with{Fuel=n.Fuel! with{LiquidBulkModulus=new(1e9,Unit.Pascal)}}:n).ToArray()};Require(!CompiledModel.TryCompile(bad,out _,out _));
    }
}
