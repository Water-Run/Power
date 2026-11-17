// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;
namespace Power.Tests;
internal static class EquilibriumNozzleGraphChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("equilibrium nozzle graph / flashing mass enthalpy jet heat entropy and numerical enclosure",Balances),
        ("equilibrium nozzle graph / independent analytic vapor blowdown first-order refinement",Blowdown),
        ("equilibrium nozzle graph / shaft duct inlet outlet and actual solenoid needle",Pump),
        ("equilibrium nozzle graph / exact replay whole rollback cancellation forks and allocations",Transactions),
        ("equilibrium nozzle graph / explicit phase selection and mode-specific channels",Contracts)];
    internal static ModelDefinition Model(ulong tick=10000)
    {
        var law=CompressibleFuelChecks.Law();Require(law.TrySaturationAtPressure(20000,out double t,out var sat));double density=1/(.8*sat.LiquidSpecificVolumeCubicMetersPerKilogram+.2*sat.VaporSpecificVolumeCubicMetersPerKilogram);var material=EquilibriumDuctChecks.Material();
        return new(){StepNanoseconds=tick,Nodes=[NodeDefinition.CompressibleFuelVolume(1,1e-5,1e-5*750*(1+5e5/1.5e9),350,material),NodeDefinition.CompressibleFuelVolume(2,1e-4,1e-4*density,t,material),NodeDefinition.Thermal(3,1,350)],
            Components=[ComponentDefinition.CompressibleNozzleValve(10,1,2,3,CompressibleNozzleChecks.Nozzle() with{PhaseModel=FuelDischargePhaseModel.Equilibrium},100)]};
    }
    internal static ModelDefinition PumpModel()
    {var d=CompressibleNozzleChecks.PumpModel();return d with{Components=d.Components.Select(c=>c.CompressibleNozzle is {} n?c with{CompressibleNozzle=n with{PhaseModel=FuelDischargePhaseModel.Equilibrium}}:c).ToArray()};}
    private static void Step(Simulation s,ulong ns){var status=s.Step(ns);if(status!=SimulationStatus.Ok)throw new InvalidOperationException("Equilibrium graph failed: "+status);}
    private static SnapshotInfo Snapshot(Simulation s)=>s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Balance(Simulation s){Near(Value(s,0,Field.EnergyResidual),0,2e-7);Near(Value(s,0,Field.MassResidual),0,1e-14);Near(Value(s,0,Field.FuelResidual),0,1e-14);}
    private static void Balances()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();double mass=Value(s,1,Field.Mass),energy=Value(s,1,Field.InternalEnergy);Step(s,1000000);Balance(s);
        Require(s.Model.HasEquilibriumFuelNozzles&&s.Model.Fidelity=="equilibrium_compressible_fuel_nozzle_network");Near(mass-Value(s,1,Field.Mass),Value(s,10,Field.TotalFuelDelivered),1e-17);
        Near(energy-Value(s,1,Field.InternalEnergy),Value(s,10,Field.TransportEnergy),1e-11);Near(Value(s,3,Field.Temperature)-350,Value(s,10,Field.HeatRejected),3e-12);
        Require(Value(s,10,Field.VaporMassFraction)>0&&Value(s,10,Field.EntropyProduced)>=0);Require(Value(s,10,Field.FluxRelativeUncertainty) is >=0 and <=EquilibriumFuelNozzle.MaximumFluxRelativeUncertainty);
    }
    private static void Pump()
    {
        var s=CompiledModel.Compile(PumpModel()).CreateSimulation();Step(s,20000000);Balance(s);Require(Value(s,20,Field.TotalFuelDelivered)>0&&Value(s,21,Field.TotalFuelDelivered)>0);
        Require(Value(s,20,Field.FluxRelativeUncertainty)<=EquilibriumFuelNozzle.MaximumFluxRelativeUncertainty&&Value(s,21,Field.FluxRelativeUncertainty)<=EquilibriumFuelNozzle.MaximumFluxRelativeUncertainty);
        Console.WriteLine($"Equilibrium pump needle delivery: {Value(s,21,Field.TotalFuelDelivered):R} kg.");
    }
    private static void Blowdown()
    {
        const double t0=500,p0=2e5,volume=1e-5,duration=.005,gamma=1.4;double m0=volume*p0/(287*t0),rate=.7e-8/volume*Math.Sqrt(gamma*287*t0)*Math.Pow(2/(gamma+1),(gamma+1)/(2*(gamma-1)));
        double exact=m0*Math.Pow(1+(gamma-1)/2*rate*duration,-2/(gamma-1));var errors=new List<double>();
        foreach(ulong tick in new ulong[]{1000000,500000,250000,125000})
        {
            var d=CompressibleNozzleChecks.Model(tick);d=d with{Components=d.Components.Select(c=>c with{CompressibleNozzle=c.CompressibleNozzle! with{PhaseModel=FuelDischargePhaseModel.Equilibrium}}).ToArray()};
            var s=CompiledModel.Compile(d).CreateSimulation();Step(s,5000000);Balance(s);Require(Value(s,10,Field.Choked)==1);errors.Add(Math.Abs(Value(s,1,Field.Mass)-exact)/m0);
        }
        Console.WriteLine("Equilibrium nozzle analytic blowdown errors: "+string.Join(", ",errors.Select(x=>x.ToString("R"))));for(int i=1;i<errors.Count;++i)Require(errors[i-1]/errors[i] is >1.8 and <2.2);
    }
    private static void Transactions()
    {
        var d=Model();d=d with{Nodes=[..d.Nodes,NodeDefinition.Rotor(4,.1)],Components=[..d.Components,ComponentDefinition.Torque(20,4,200,0)]};
        var s=CompiledModel.Compile(d).CreateSimulation();var replay=s.Fork();Step(s,100000);for(int i=0;i<10;++i)Step(replay,10000);Require(Snapshot(s)==Snapshot(replay));var before=Snapshot(s);
        Require(s.Step(100000,cancellationToken:new CancellationToken(true))==SimulationStatus.Cancelled&&Snapshot(s)==before);
        Require(s.Step(100000,[new(before.TimeNanoseconds+50000,200,double.MaxValue)])==SimulationStatus.NumericalFailure&&Snapshot(s)==before);
        var child=s.Fork();Require(child.SubmitInputs([new(100,0)])==SimulationStatus.Ok);Step(child,100000);Require(Snapshot(s)==before);
        var values=new Scalar[s.Model.OutputCount];for(int i=0;i<5;++i){Step(s,10000);s.ReadSnapshot(values);}long allocation=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10;++i){Step(s,10000);s.ReadSnapshot(values);}Require(GC.GetAllocatedBytesForCurrentThread()==allocation);
    }
    private static void Contracts()
    {
        var frozen=CompiledModel.Compile(CompressibleNozzleChecks.Model());Require(!frozen.HasEquilibriumFuelNozzles&&!frozen.Channels.Any(c=>c.Id==Channels.Output(10,Field.FluxRelativeUncertainty)));
        var d=Model();Require(!CompiledModel.TryCompile(d with{Components=[d.Components[0] with{CompressibleNozzle=d.Components[0].CompressibleNozzle! with{PhaseModel=(FuelDischargePhaseModel)3}}]},out _,out _));
        var compiled=CompiledModel.Compile(d);d.Components[0]=d.Components[0] with{CompressibleNozzle=d.Components[0].CompressibleNozzle! with{PhaseModel=FuelDischargePhaseModel.Frozen}};
        Require(compiled.HasEquilibriumFuelNozzles&&compiled.StateCount==frozen.StateCount+1);
    }
}
