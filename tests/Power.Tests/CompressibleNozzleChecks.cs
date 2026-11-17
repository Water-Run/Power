// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;
namespace Power.Tests;
internal static class CompressibleNozzleChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("compressible nozzle / finite vapor enthalpy kinetic wall heat entropy and complete ledgers",Conservation),
        ("compressible nozzle / independent analytic choked blowdown first-order refinement",Blowdown),
        ("compressible nozzle / one-way closure and finite pressure equalization",Closure),
        ("compressible nozzle / shaft duct supply check valves and physical needle",Pump),
        ("compressible nozzle / scheduled closure rollback cancellation forks zero allocations",Transactions),
        ("compressible nozzle / units material internal-cell ownership and needle contracts",Contracts)];
    internal static CompressibleFuelNozzleDefinition Nozzle(double area=1e-8,double fraction=.4)=>new(){Area=new(area,Unit.SquareMeter),DischargeCoefficient=.7,PhaseModel=FuelDischargePhaseModel.Frozen,WallKineticFraction=new(fraction,Unit.Fraction)};
    internal static ModelDefinition Model(ulong tick=10000)
    {var m=EquilibriumDuctChecks.Material(true);return new(){StepNanoseconds=tick,Nodes=[NodeDefinition.CompressibleFuelVolume(1,1e-5,1e-5*2e5/(287*500),500,m),NodeDefinition.CompressibleFuelVolume(2,1,1e5/(287*500),500,m),NodeDefinition.Thermal(3,1,350)],Components=[ComponentDefinition.CompressibleNozzleValve(10,1,2,3,Nozzle(),100)]};}
    private static SnapshotInfo Snapshot(Simulation s)=>s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Step(Simulation s,ulong ns){var status=s.Step(ns);if(status!=SimulationStatus.Ok)throw new InvalidOperationException("Compressible nozzle failed: "+status);}
    private static void Balance(Simulation s){Near(Value(s,0,Field.EnergyResidual),0,2e-7);Near(Value(s,0,Field.MassResidual),0,1e-14);Near(Value(s,0,Field.FuelResidual),0,1e-14);}
    private static void Conservation()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();double mass=Value(s,1,Field.Mass),energy=Value(s,1,Field.InternalEnergy);Step(s,1000000);Balance(s);
        double transferred=Value(s,10,Field.TotalFuelDelivered);Require(transferred>0);Near(mass-Value(s,1,Field.Mass),transferred,1e-18);Near(energy-Value(s,1,Field.InternalEnergy),Value(s,10,Field.TransportEnergy),2e-15);
        Near(Value(s,10,Field.HeatRejected),.4*Value(s,10,Field.JetKineticEnergy),1e-17);Near(Value(s,10,Field.FluidHeat),.6*Value(s,10,Field.JetKineticEnergy),1e-17);Near(Value(s,3,Field.Temperature)-350,Value(s,10,Field.HeatRejected),3e-12);
        Require(Value(s,10,Field.EntropyProduced)>0&&Value(s,10,Field.JetImpulse)>0&&Value(s,10,Field.Choked)==1);
    }
    private static void Blowdown()
    {
        const double t0=500,p0=2e5,volume=1e-5,duration=.005,gamma=1.4;double m0=volume*p0/(287*t0),rate=.7e-8/volume*Math.Sqrt(gamma*287*t0)*Math.Pow(2/(gamma+1),(gamma+1)/(2*(gamma-1)));
        double exact=m0*Math.Pow(1+(gamma-1)/2*rate*duration,-2/(gamma-1));var errors=new List<double>();
        foreach(ulong tick in new ulong[]{1000000,500000,250000,125000}){var s=CompiledModel.Compile(Model(tick)).CreateSimulation();Step(s,5000000);Balance(s);Require(Value(s,10,Field.Choked)==1);errors.Add(Math.Abs(Value(s,1,Field.Mass)-exact)/m0);}
        Console.WriteLine("Compressible nozzle analytic blowdown errors: "+string.Join(", ",errors.Select(x=>x.ToString("R"))));for(int i=1;i<errors.Count;++i)Require(errors[i-1]/errors[i] is >1.8 and <2.2);
    }
    private static void Closure()
    {
        var d=Model();d=d with{Nodes=d.Nodes.Select(n=>n.Id==2?n with{Storage=new(1e-5,Unit.CubicMeter),Position=new(1e-5*3e5/(287*500),Unit.Kilogram)}:n).ToArray()};var closed=CompiledModel.Compile(d).CreateSimulation();double mass=Value(closed,1,Field.Mass);Step(closed,100000);Near(Value(closed,1,Field.Mass),mass,0);Near(Value(closed,10,Field.TotalFuelDelivered),0,0);
        d=Model(1000000);d=d with{Nodes=d.Nodes.Select(n=>n.Id==2?n with{Storage=new(1e-5,Unit.CubicMeter),Position=new(1e-5*1e5/(287*500),Unit.Kilogram)}:n).ToArray(),Components=[d.Components[0] with{CompressibleNozzle=Nozzle(1e-6)}]};
        var s=CompiledModel.Compile(d).CreateSimulation();Step(s,100000000);Balance(s);Require(Value(s,1,Field.Pressure)>=Value(s,2,Field.Pressure)-1e-4);
    }
    internal static ModelDefinition PumpModel()
    {
        var d=CompressibleChamberChecks.Model(true);var m=EquilibriumDuctChecks.Material();double Density(double p)=>750*(1+p/1.5e9);
        var nodes=d.Nodes.Select(n=>n.Id==2?n with{Storage=new(1e-6,Unit.CubicMeter),Position=new(1e-6*Density(3e5),Unit.Kilogram)}:n.Id>=100?n with{Position=new(n.Storage.Value*Density(3e5),Unit.Kilogram)}:n).ToList();
        nodes.Add(NodeDefinition.CompressibleFuelVolume(5,.001,.001*Density(3e5),350,m));nodes.Add(NodeDefinition.CompressibleFuelVolume(6,1e-4,1e-4*Density(5e5),350,m));nodes.Add(NodeDefinition.Translational(7,.002,0,.001));
        var duct=d.Components[1] with{NodeA=5,NodeB=2};var outlet=Nozzle(1e-7) with{Needle=new(){NeedleNode=7,ClosedPosition=new(0,Unit.Meter),FullOpenPosition=new(.001,Unit.Meter)}};
        nodes=nodes.Select(n=>n.Id==4?n with{Initial=new(500,Unit.RadianPerSecond)}:n).ToList();outlet=outlet with{Area=new(2e-6,Unit.SquareMeter)};
        return d with{StepNanoseconds=10000,Nodes=nodes.ToArray(),Components=[d.Components[0],duct,ComponentDefinition.CompressibleNozzleValve(20,2,1,3,Nozzle(4e-6)),ComponentDefinition.CompressibleNozzleValve(21,1,6,3,outlet,210),ComponentDefinition.LinearSpring(30,7,0,1000,.2,heat:3),ComponentDefinition.SolenoidCoil(31,7,3,220,2,.02,4,voltage:2,current:1),ComponentDefinition.StrokeStop(32,7,0,.001,10000)]};
    }
    private static void Pump()
    {
        var s=CompiledModel.Compile(PumpModel()).CreateSimulation();double rail=Value(s,6,Field.Mass);
        for(int i=0;i<2000;++i){var status=s.Step(10000);Require(status==SimulationStatus.Ok,$"Pump failed at {i*10000} ns; chamber p={Value(s,1,Field.Pressure):R}, T={Value(s,1,Field.Temperature):R}, inlet p={Value(s,2,Field.Pressure):R}, rail p={Value(s,6,Field.Pressure):R}.");}Balance(s);
        Require(Value(s,20,Field.TotalFuelDelivered)>0&&Value(s,21,Field.TotalFuelDelivered)>0);Near(Value(s,6,Field.Mass)-rail,Value(s,21,Field.TotalFuelDelivered),1e-16);
        Require(Value(s,21,Field.Opening)>=0&&Value(s,21,Field.Opening)<=1);Console.WriteLine($"Compressible pump nozzle delivery: {Value(s,21,Field.TotalFuelDelivered):R} kg.");
    }
    private static void Transactions()
    {
        var definition=Model();definition=definition with{Nodes=[..definition.Nodes,NodeDefinition.Rotor(4,.1)],Components=[..definition.Components,ComponentDefinition.Torque(20,4,200,0)]};
        var s=CompiledModel.Compile(definition).CreateSimulation();var replay=s.Fork();Step(s,100000);for(int i=0;i<10;++i)Step(replay,10000);Require(Snapshot(s)==Snapshot(replay));var before=Snapshot(s);
        Require(s.Step(100000,cancellationToken:new CancellationToken(true))==SimulationStatus.Cancelled&&Snapshot(s)==before);
        var failed=s.Step(100000,[new(before.TimeNanoseconds+50000,100,2)]);Require(failed==SimulationStatus.InvalidInput&&Snapshot(s)==before,"Invalid scheduled opening returned "+failed);
        Require(s.Step(100000,[new(before.TimeNanoseconds+50000,200,double.MaxValue)])==SimulationStatus.NumericalFailure&&Snapshot(s)==before);
        var child=s.Fork();var closing=child.SubmitInputs([new(100,0)]);Require(closing==SimulationStatus.Ok,"Closing command returned "+closing);Step(child,100000);Require(Snapshot(s)==before);
        var values=new Scalar[s.Model.OutputCount];for(int i=0;i<20;++i){Step(s,10000);s.ReadSnapshot(values);}long allocation=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<20;++i){Step(s,10000);s.ReadSnapshot(values);}Require(GC.GetAllocatedBytesForCurrentThread()==allocation);
    }
    private static void Contracts()
    {
        var d=Model();Require(!CompiledModel.TryCompile(d with{Components=[d.Components[0] with{NodeB=1}]},out _,out _));
        Require(!CompiledModel.TryCompile(d with{Components=[d.Components[0] with{CompressibleNozzle=Nozzle() with{Area=new(1,Unit.Kelvin)}}]},out _,out var diagnostic)&&diagnostic!.Code==DiagnosticCode.Unit);
        Require(!CompiledModel.TryCompile(d with{Nodes=d.Nodes.Select(n=>n.Id==2?n with{Fuel=n.Fuel! with{LiquidBulkModulus=new(1e9,Unit.Pascal)}}:n).ToArray()},out _,out _));
        Require(!CompiledModel.TryCompile(d with{Components=[d.Components[0] with{CompressibleNozzle=Nozzle() with{Needle=new(){NeedleNode=3,ClosedPosition=new(0,Unit.Meter),FullOpenPosition=new(.001,Unit.Meter)}}}]},out _,out _));
    }
}
