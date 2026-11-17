// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;
namespace Power.Tests;
internal static class RelaxingFuelGraphChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("relaxing graph / exact isolated relaxation source and complete vapor balance",Closed),
        ("relaxing duct / conservative vapor advection mass momentum energy and entropy",Transport),
        ("relaxing duct / finite relaxation time sensitivity with complete inventories",Sensitivity),
        ("relaxing duct / combined transport-source temporal refinement",Refinement),
        ("relaxing duct / stiff relaxation equilibrium transport limit at resolved CFL",EquilibriumLimit),
        ("relaxing duct / independent frozen acoustic cell-average mesh refinement",Acoustic),
        ("relaxing graph / rollback cancellation branches and zero allocations",Transactions),
        ("relaxing graph / domain material quality and bounded time contracts",Contracts)];
    internal static EquilibriumFuelDefinition Material(ulong tau=1000000)=>EquilibriumDuctChecks.Material() with{PhaseRelaxationTimeNanoseconds=tau};
    internal static ModelDefinition Model(bool duct=true,ulong tick=1000,ulong tau=1000000)
    {
        var material=Material(tau);double rhoA=RelaxingFuelChecks.Density(2e5,450,.3),rhoB=RelaxingFuelChecks.Density(1e5,450,.3);
        if(!duct)return new(){StepNanoseconds=tick,Nodes=[NodeDefinition.RelaxingFuelVolume(1,1e-4,1e-4*rhoB,450,material,.3)],Components=[]};
        var nodes=new List<NodeDefinition>{NodeDefinition.RelaxingFuelVolume(1,1e-4,1e-4*rhoA,450,material,.3),NodeDefinition.RelaxingFuelVolume(2,1e-4,1e-4*rhoB,450,material,.3),NodeDefinition.Thermal(3,1,350)};
        for(uint id=100;id<104;++id)nodes.Add(NodeDefinition.RelaxingFuelVolume(id,.025e-6,.025e-6*rhoB,450,material,.3));
        return new(){StepNanoseconds=tick,Nodes=nodes.ToArray(),Components=[ComponentDefinition.RelaxingDuct(10,1,2,3,new(){Cells=new uint[]{100,101,102,103},Area=new(1e-6,Unit.SquareMeter),LinearResistance=new(0,Unit.PascalSecondPerCubicMeter),QuadraticResistance=new(0,Unit.PascalSecondSquaredPerCubicMeterSquared),FluidHeatFraction=new(.5,Unit.Fraction)})]};
    }
    private static void Step(Simulation s,ulong ns){var status=s.Step(ns);if(status!=SimulationStatus.Ok)throw new InvalidOperationException("Relaxing graph failed: "+status);}
    private static SnapshotInfo Snapshot(Simulation s)=>s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Balance(Simulation s){Near(Value(s,0,Field.EnergyResidual),0,2e-7);Near(Value(s,0,Field.MassResidual),0,1e-14);Near(Value(s,0,Field.FuelResidual),0,1e-14);Near(Value(s,0,Field.VaporMassResidual),0,1e-14);}
    private static void Closed()
    {
        var s=CompiledModel.Compile(Model(false,100000)).CreateSimulation();double initial=Value(s,1,Field.VaporMassFraction),target=Value(s,1,Field.EquilibriumVaporMassFraction),mass=Value(s,1,Field.Mass),energy=Value(s,1,Field.InternalEnergy);
        Step(s,2000000);Balance(s);Near(Value(s,1,Field.VaporMassFraction),target+(initial-target)*Math.Exp(-2),2e-14);Near(Value(s,1,Field.Mass),mass,0);Near(Value(s,1,Field.InternalEnergy),energy,0);
        Near(Value(s,1,Field.EvaporatedFuelMass)-Value(s,1,Field.CondensedFuelMass),mass*(Value(s,1,Field.VaporMassFraction)-initial),2e-17);Require(Value(s,1,Field.EntropyProduced)>0);
    }
    private static void Transport()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();double massA=Value(s,1,Field.Mass),massB=Value(s,2,Field.Mass),vaporA=Value(s,1,Field.VaporMass),vaporB=Value(s,2,Field.VaporMass);
        Step(s,100000);Balance(s);Near(massA-Value(s,1,Field.Mass),Value(s,10,Field.MassAtA),1e-17);Near(Value(s,2,Field.Mass)-massB,Value(s,10,Field.MassAtB),1e-17);
        Near(Value(s,1,Field.VaporMass)-vaporA,-Value(s,10,Field.VaporMassAtA)+Value(s,1,Field.EvaporatedFuelMass)-Value(s,1,Field.CondensedFuelMass),2e-17);
        Near(Value(s,2,Field.VaporMass)-vaporB,Value(s,10,Field.VaporMassAtB)+Value(s,2,Field.EvaporatedFuelMass)-Value(s,2,Field.CondensedFuelMass),2e-17);
        double momentum=0;for(uint id=100;id<104;++id)momentum+=Value(s,id,Field.Mass)*Value(s,id,Field.LinearSpeed);Near(momentum+Value(s,10,Field.JetImpulse),0,1e-12);
        Require(Value(s,10,Field.EntropyProduced)>=0&&Value(s,100,Field.EntropyProduced)>=0);Require(Value(s,10,Field.VaporMassAtA)!=0);
    }
    private static void Sensitivity()
    {
        var fast=CompiledModel.Compile(Model(tau:10000)).CreateSimulation();var slow=CompiledModel.Compile(Model(tau:10000000)).CreateSimulation();Step(fast,100000);Step(slow,100000);Balance(fast);Balance(slow);
        double a=Math.Abs(Value(fast,100,Field.VaporMassFraction)-Value(fast,100,Field.EquilibriumVaporMassFraction)),b=Math.Abs(Value(slow,100,Field.VaporMassFraction)-Value(slow,100,Field.EquilibriumVaporMassFraction));Require(a<b);
        Console.WriteLine($"Relaxing duct equilibrium quality gaps: fast={a:R}, slow={b:R}.");
    }
    private static void Refinement()
    {
        var reference=CompiledModel.Compile(Model(tick:125)).CreateSimulation();var finer=CompiledModel.Compile(Model(tick:50)).CreateSimulation();Step(reference,100000);Step(finer,100000);
        double exact=Value(finer,100,Field.VaporMassFraction);Require(Math.Abs(Value(reference,100,Field.VaporMassFraction)-exact)<2e-4);
        var errors=new List<double>();foreach(ulong tick in new ulong[]{4000,2000,1000,500}){var s=CompiledModel.Compile(Model(tick:tick)).CreateSimulation();Step(s,100000);Balance(s);errors.Add(Math.Abs(Value(s,100,Field.VaporMassFraction)-exact));}
        Console.WriteLine("Relaxing duct temporal quality errors: "+string.Join(", ",errors.Select(x=>x.ToString("R"))));for(int i=1;i<errors.Count;++i)Require(errors[i-1]>errors[i]);
    }
    private static void Transactions()
    {
        var d=Model();d=d with{Nodes=[..d.Nodes,NodeDefinition.Rotor(4,.1)],Components=[..d.Components,ComponentDefinition.Torque(20,4,200,0)]};var s=CompiledModel.Compile(d).CreateSimulation();var replay=s.Fork();Step(s,10000);for(int i=0;i<10;++i)Step(replay,1000);Require(Snapshot(s)==Snapshot(replay));var before=Snapshot(s);
        Require(s.Step(10000,cancellationToken:new CancellationToken(true))==SimulationStatus.Cancelled&&Snapshot(s)==before);Require(s.Step(10000,[new(before.TimeNanoseconds+5000,200,double.MaxValue)])==SimulationStatus.NumericalFailure&&Snapshot(s)==before);
        Step(replay,1000);Require(Snapshot(s)==before);var values=new Scalar[s.Model.OutputCount];for(int i=0;i<10;++i){Step(s,1000);s.ReadSnapshot(values);}long allocation=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<20;++i){Step(s,1000);s.ReadSnapshot(values);}Require(GC.GetAllocatedBytesForCurrentThread()==allocation);
    }
    private static void EquilibriumLimit()
    {
        var d=EquilibriumDuctChecks.Model();d=d with{StepNanoseconds=1000};var law=CompressibleFuelChecks.Law();
        var r=d with{Nodes=d.Nodes.Select(n=>{if(n.Domain!=Domain.CompressibleFuel)return n;Require(law.Evaluate(n.Position.Value/n.Storage.Value,n.Initial.Value,out var p));return n with{Domain=Domain.RelaxingFuel,Fuel=n.Fuel! with{PhaseRelaxationTimeNanoseconds=1},InitialVaporMassFraction=new(p.VaporMassFraction,Unit.Fraction)};}).ToArray(),Components=d.Components.Select(c=>c with{Kind=ComponentKind.RelaxingFuelDuct}).ToArray()};
        var a=CompiledModel.Compile(d).CreateSimulation();var b=CompiledModel.Compile(r).CreateSimulation();Step(a,10000);Step(b,10000);Balance(b);
        for(uint id=100;id<104;++id){Near(Value(a,id,Field.Pressure),Value(b,id,Field.Pressure),1e-6);Near(Value(a,id,Field.Temperature),Value(b,id,Field.Temperature),1e-9);}
    }
    private static void Acoustic()
    {
        var law=RelaxingFuelChecks.Law(3_600_000_000_000);Require(law.Equilibrium.TrySaturation(450,out var sat));double pressure=sat.PressurePascals,density=RelaxingFuelChecks.Density(pressure,450,.3);Require(law.Evaluate(density,450,.3,out var initial));
        double sound=initial.Fluid.SoundSpeedMetersPerSecond,alpha=.3*287/(.7*2000+.3*1004.5);const double length=.1,amplitude=10,time=1e-5;
        double Bump(double x)=>Math.Exp(-Math.Pow((x-.05)/.02,2));
        double Average(double left,double right,double shift){double sum=0,h=(right-left)/64;for(int i=0;i<=64;++i)sum+=(i==0||i==64?1:i%2==0?2:4)*Bump(left+i*h+shift);return sum*h/3/(right-left);}
        var errors=new List<double>();foreach(int count in new[]{4,8,12})
        {
            var material=Material(3_600_000_000_000);var nodes=new List<NodeDefinition>{NodeDefinition.RelaxingFuelVolume(1,.001,.001*density,450,material,.3),NodeDefinition.RelaxingFuelVolume(2,.001,.001*density,450,material,.3),NodeDefinition.Thermal(3,1,350)};var ids=new List<uint>();double dx=length/count,volume=1e-7/count;
            for(int i=0;i<count;++i){uint id=(uint)(100+i);ids.Add(id);double p=pressure+amplitude*Average(i*dx,(i+1)*dx,0),t=450*Math.Pow(p/pressure,alpha),rho=RelaxingFuelChecks.Density(p,t,.3);nodes.Add(NodeDefinition.RelaxingFuelVolume(id,volume,volume*rho,t,material,.3));}
            var d=new ModelDefinition{StepNanoseconds=1000,Nodes=nodes.ToArray(),Components=[ComponentDefinition.RelaxingDuct(10,1,2,3,new(){Cells=ids,Area=new(1e-6,Unit.SquareMeter),LinearResistance=new(0,Unit.PascalSecondPerCubicMeter),QuadraticResistance=new(0,Unit.PascalSecondSquaredPerCubicMeterSquared),FluidHeatFraction=new(.5,Unit.Fraction)})]};
            var s=CompiledModel.Compile(d).CreateSimulation();Step(s,10000);Balance(s);double error=0;for(int i=0;i<count;++i){double exact=pressure+.5*amplitude*(Average(i*dx,(i+1)*dx,-sound*time)+Average(i*dx,(i+1)*dx,sound*time));error+=Math.Abs(Value(s,(uint)(100+i),Field.Pressure)-exact)/amplitude/count;}errors.Add(error);
        }
        Console.WriteLine("Relaxing duct frozen acoustic errors: "+string.Join(", ",errors.Select(x=>x.ToString("R"))));Require(errors[2]<errors[1]&&errors[1]<errors[0]);
    }
    private static void Contracts()
    {
        var d=Model(false);Require(!CompiledModel.TryCompile(d with{Nodes=[d.Nodes[0] with{Fuel=Material(0)}]},out _,out _));Require(!CompiledModel.TryCompile(d with{Nodes=[d.Nodes[0] with{InitialVaporMassFraction=new(1.1,Unit.Fraction)}]},out _,out _));
        Require(!CompiledModel.TryCompile(d with{Nodes=[d.Nodes[0] with{Domain=Domain.CompressibleFuel}]},out _,out _));var pipe=Model();Require(!CompiledModel.TryCompile(pipe with{Components=[pipe.Components[0] with{Kind=ComponentKind.EquilibriumFuelDuct}]},out _,out _));
    }
}
