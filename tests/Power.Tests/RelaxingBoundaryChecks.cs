// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;
namespace Power.Tests;
internal static class RelaxingBoundaryChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("relaxing chamber / independent phase inventory shaft work energy and entropy",Chamber),
        ("relaxing nozzle / frozen vapor inventory enthalpy wall heat and entropy",Nozzle),
        ("relaxing pump / duct supply shaft check valves and physical needle",Pump),
        ("relaxing chamber / independent ideal-vapor shaft ODE refinement",MechanicalReference),
        ("relaxing boundaries / coupled finite-rate transport temporal refinement",Refinement),
        ("relaxing boundaries / shared reservoirs receive phase source once",Shared),
        ("relaxing boundaries / complete rollback cancellation forks and zero allocations",Transactions),
        ("relaxing boundaries / domain transit geometry ownership and immutable contracts",Contracts)];
    internal static ModelDefinition NozzleModel(ulong tick=1000,ulong tau=1000000,double area=1e-8)
    {
        var m=RelaxingFuelGraphChecks.Material(tau);
        return new(){StepNanoseconds=tick,Nodes=[NodeDefinition.RelaxingFuelVolume(1,1e-5,1e-5*RelaxingFuelChecks.Density(2e5,450,.3),450,m,.3),NodeDefinition.RelaxingFuelVolume(2,1e-4,1e-4*RelaxingFuelChecks.Density(1e5,450,.1),450,m,.1),NodeDefinition.Thermal(3,1,350)],Components=[ComponentDefinition.CompressibleNozzleValve(10,1,2,3,CompressibleNozzleChecks.Nozzle(area),100)]};
    }
    internal static ModelDefinition ChamberModel(ulong tick=10000)
    {
        var m=RelaxingFuelGraphChecks.Material();return new(){StepNanoseconds=tick,Nodes=[NodeDefinition.RelaxingFuelVolume(1,1e-7,1e-7*RelaxingFuelChecks.Density(1e5,450,.3),450,m,.3),NodeDefinition.Rotor(4,1e-5,100)],Components=[ComponentDefinition.CompressibleChamber(12,4,1,CompressibleChamberChecks.Geometry)]};
    }
    internal static ModelDefinition PumpModel()
    {
        var d=CompressibleNozzleChecks.PumpModel();var law=CompressibleFuelChecks.Law();
        return d with{Nodes=d.Nodes.Select(n=>{if(n.Domain!=Domain.CompressibleFuel)return n;Require(law.Evaluate(n.Position.Value/n.Storage.Value,n.Initial.Value,out var p));double rho=RelaxingFuelChecks.Density(p.PressurePascals,n.Initial.Value,.001);return n with{Domain=Domain.RelaxingFuel,Position=new(n.Storage.Value*rho,Unit.Kilogram),InitialVaporMassFraction=new(.001,Unit.Fraction),Fuel=n.Fuel! with{PhaseRelaxationTimeNanoseconds=1000000}};}).ToArray(),Components=d.Components.Select(c=>c.Kind==ComponentKind.EquilibriumFuelDuct?c with{Kind=ComponentKind.RelaxingFuelDuct}:c).ToArray()};
    }
    private static void Step(Simulation s,ulong ns){var status=s.Step(ns);if(status!=SimulationStatus.Ok)throw new InvalidOperationException("Relaxing boundary failed at "+Snapshot(s).TimeNanoseconds+": "+status);}
    private static SnapshotInfo Snapshot(Simulation s)=>s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Balance(Simulation s){Near(Value(s,0,Field.EnergyResidual),0,2e-7);Near(Value(s,0,Field.MassResidual),0,1e-14);Near(Value(s,0,Field.FuelResidual),0,1e-14);Near(Value(s,0,Field.VaporMassResidual),0,1e-14);}
    private static void Chamber()
    {
        var s=CompiledModel.Compile(ChamberModel()).CreateSimulation();double vapor=Value(s,1,Field.VaporMass),energy=Value(s,1,Field.InternalEnergy),entropy=Value(s,1,Field.Entropy);Step(s,20000000);Balance(s);
        Near(Value(s,1,Field.InternalEnergy)-energy,Value(s,12,Field.HydraulicWork),1e-12);Near(Value(s,1,Field.VaporMass)-vapor,Value(s,1,Field.EvaporatedFuelMass)-Value(s,1,Field.CondensedFuelMass),1e-18);
        Near(Value(s,1,Field.Entropy)-entropy,Value(s,1,Field.EntropyProduced),1e-12);Require(Value(s,1,Field.EntropyProduced)>0&&Value(s,12,Field.Volume)!=1e-7);
    }
    private static void Nozzle()
    {
        var s=CompiledModel.Compile(NozzleModel(tau:3600000000000)).CreateSimulation();double va=Value(s,1,Field.VaporMass),vb=Value(s,2,Field.VaporMass);Step(s,1000000);Balance(s);double transfer=Value(s,10,Field.TotalVaporDelivered);Require(transfer>0);
        Near(Value(s,1,Field.VaporMass)-va,-transfer+Value(s,1,Field.EvaporatedFuelMass)-Value(s,1,Field.CondensedFuelMass),1e-18);Near(Value(s,2,Field.VaporMass)-vb,transfer+Value(s,2,Field.EvaporatedFuelMass)-Value(s,2,Field.CondensedFuelMass),1e-18);
        Near(transfer,.3*Value(s,10,Field.TotalFuelDelivered),1e-15);Require(Math.Abs(Value(s,1,Field.VaporMassFraction)-Value(s,1,Field.EquilibriumVaporMassFraction))>1e-3);
        Near(Value(s,10,Field.HeatRejected)+Value(s,10,Field.FluidHeat),Value(s,10,Field.JetKineticEnergy),1e-16);Require(Value(s,10,Field.EntropyProduced)>0);
    }
    private static void Pump()
    {
        var s=CompiledModel.Compile(PumpModel()).CreateSimulation();double vapor=Value(s,6,Field.VaporMass);for(int i=0;i<2000;++i)Step(s,10000);Balance(s);
        Require(Value(s,20,Field.TotalFuelDelivered)>0&&Value(s,21,Field.TotalFuelDelivered)>0&&Value(s,21,Field.TotalVaporDelivered)>0);
        Near(Value(s,6,Field.VaporMass)-vapor,Value(s,21,Field.TotalVaporDelivered)+Value(s,6,Field.EvaporatedFuelMass)-Value(s,6,Field.CondensedFuelMass),1e-16);Require(Value(s,21,Field.Opening) is >=0 and <=1);
        Console.WriteLine($"Relaxing pump needle delivery: mass={Value(s,21,Field.TotalFuelDelivered):R}, vapor={Value(s,21,Field.TotalVaporDelivered):R} kg.");
    }
    private static void MechanicalReference()
    {
        const double initialAngle=.6,initialSpeed=100,duration=.002,minimum=1e-7,swept=2e-7,inertia=1e-5;
        double Volume(double a)=>minimum+swept*Math.Pow(Math.Sin(a/2),2);double v0=Volume(initialAngle);
        double Acceleration(double angle)=>1e5*Math.Pow(v0/Volume(angle),1.4)*swept*.5*Math.Sin(angle)/inertia;
        double angle=initialAngle,speed=initialSpeed,h=duration/20000;
        for(int i=0;i<20000;++i){double a1=speed,b1=Acceleration(angle),a2=speed+h*b1/2,b2=Acceleration(angle+h*a1/2),a3=speed+h*b2/2,b3=Acceleration(angle+h*a2/2),a4=speed+h*b3,b4=Acceleration(angle+h*a3);angle+=h*(a1+2*a2+2*a3+a4)/6;speed+=h*(b1+2*b2+2*b3+b4)/6;}
        var errors=new List<double>();foreach(ulong tick in new ulong[]{100000,50000,25000,12500})
        {
            var m=EquilibriumDuctChecks.Material(true) with{PhaseRelaxationTimeNanoseconds=1000000};var d=new ModelDefinition{StepNanoseconds=tick,Nodes=[NodeDefinition.RelaxingFuelVolume(1,v0,v0*1e5/(287*500),500,m,1),NodeDefinition.Rotor(4,inertia,initialSpeed,initialAngle)],Components=[ComponentDefinition.CompressibleChamber(12,4,1,CompressibleChamberChecks.Geometry)]};var s=CompiledModel.Compile(d).CreateSimulation();Step(s,2000000);Balance(s);errors.Add(Math.Abs(Value(s,4,Field.Speed)-speed));
        }
        Console.WriteLine("Relaxing chamber independent vapor shaft errors: "+string.Join(", ",errors.Select(x=>x.ToString("R"))));for(int i=1;i<errors.Count;++i)Require(errors[i-1]/errors[i] is >3.7 and <4.3);
    }
    private static void Refinement()
    {
        var reference=CompiledModel.Compile(NozzleModel(50,1000000,1e-5)).CreateSimulation();Step(reference,100000);double exact=Value(reference,1,Field.VaporMassFraction);var errors=new List<double>();
        foreach(ulong tick in new ulong[]{4000,2000,1000,500}){var s=CompiledModel.Compile(NozzleModel(tick,1000000,1e-5)).CreateSimulation();Step(s,100000);Balance(s);errors.Add(Math.Abs(Value(s,1,Field.VaporMassFraction)-exact));}
        Console.WriteLine("Relaxing boundary temporal quality errors: "+string.Join(", ",errors.Select(x=>x.ToString("R"))));for(int i=1;i<errors.Count;++i)Require(errors[i-1]>errors[i]);
    }
    private static void Shared()
    {
        var d=NozzleModel(10000);d=d with{Components=[d.Components[0] with{InitialInput=new(0,Unit.Fraction)},d.Components[0] with{Id=11,InputChannel=101,InitialInput=new(0,Unit.Fraction)}]};var s=CompiledModel.Compile(d).CreateSimulation();double initial=Value(s,1,Field.VaporMassFraction),target=Value(s,1,Field.EquilibriumVaporMassFraction);Step(s,2000000);Balance(s);Near(Value(s,1,Field.VaporMassFraction),target+(initial-target)*Math.Exp(-2),3e-14);
    }
    private static void Transactions()
    {
        var d=NozzleModel();d=d with{Nodes=[..d.Nodes,NodeDefinition.Rotor(4,.1)],Components=[..d.Components,ComponentDefinition.Torque(20,4,200,0)]};var s=CompiledModel.Compile(d).CreateSimulation();var fork=s.Fork();Step(s,10000);for(int i=0;i<10;++i)Step(fork,1000);Require(Snapshot(s)==Snapshot(fork));var before=Snapshot(s);
        Require(s.Step(10000,cancellationToken:new CancellationToken(true))==SimulationStatus.Cancelled&&Snapshot(s)==before);Require(s.Step(10000,[new(before.TimeNanoseconds+5000,200,double.MaxValue)])==SimulationStatus.NumericalFailure&&Snapshot(s)==before);Step(fork,1000);Require(Snapshot(s)==before);
        var values=new Scalar[s.Model.OutputCount];for(int i=0;i<10;++i){Step(s,1000);s.ReadSnapshot(values);}long allocation=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<20;++i){Step(s,1000);s.ReadSnapshot(values);}Require(GC.GetAllocatedBytesForCurrentThread()==allocation);
    }
    private static void Contracts()
    {
        var d=NozzleModel();Require(!CompiledModel.TryCompile(d with{Nodes=d.Nodes.Select(n=>n.Id==2?NodeDefinition.CompressibleFuelVolume(2,n.Storage.Value,n.Position.Value,n.Initial.Value,EquilibriumDuctChecks.Material()):n).ToArray()},out _,out _));
        Require(!CompiledModel.TryCompile(d with{Components=[d.Components[0] with{CompressibleNozzle=d.Components[0].CompressibleNozzle! with{PhaseModel=FuelDischargePhaseModel.Equilibrium}}]},out _,out var diagnostic)&&diagnostic?.Field=="compressible_nozzle.phase_mode");
        d=ChamberModel();Require(!CompiledModel.TryCompile(d with{Components=[d.Components[0],d.Components[0] with{Id=13}]},out _,out _));var model=CompiledModel.Compile(d);Require(model.HasRelaxingFuelBoundaries&&model.Fidelity=="homogeneous_relaxing_fuel_boundaries");
        d=NozzleModel(tau:3600000000000);d=d with{Nodes=d.Nodes.Select(n=>n.Id==2?n with{Position=new(n.Storage.Value*RelaxingFuelChecks.Density(2e5,450,.1),Unit.Kilogram)}:n).ToArray()};var closed=CompiledModel.Compile(d).CreateSimulation();Step(closed,1000);Near(Value(closed,10,Field.TotalFuelDelivered),0,0);
    }
}
