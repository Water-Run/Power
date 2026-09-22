// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class PumpChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("hydraulic pump / reversible power law / relief passivity / finite ranges", Laws),
        ("hydraulic pump / analytic shaft-compliance oscillation / midpoint convergence", Oscillation),
        ("hydraulic pump / closed inlet volume / motoring regeneration / gear reactions", Closed),
        ("hydraulic relief / analytic decay / regulated shaft load / heat", Relief),
        ("pump pressure clutch / analytic slipping feedback / second-order refinement", Feedback),
        ("pump pressure clutch / simultaneous pressure feedback / capture / independent branches", Clutch),
        ("hydraulic pump / rollback / cancellation / zero allocation / invalid contracts", Transactions)
    ];
    private static void Ok(SimulationStatus s) { if(s!=SimulationStatus.Ok) throw new InvalidOperationException(s.ToString()); }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    internal static ModelDefinition Oscillator(ulong step=1_000_000) => new()
    {
        StepNanoseconds=step, Nodes=[NodeDefinition.Rotor(1,.2,100),NodeDefinition.Hydraulic(2,2e-12,2e6)],
        Components=[ComponentDefinition.DisplacementPump(10,1,2,1e-6,reservoirPressure:1e6)]
    };
    internal static ModelDefinition ClutchModel(ulong step=100_000) => new()
    {
        StepNanoseconds=step,
        Nodes=[NodeDefinition.Rotor(1,.2,100),NodeDefinition.Rotor(2,.5),NodeDefinition.Hydraulic(3,2e-12,1e6),NodeDefinition.Thermal(4,100,300)],
        Components=[ComponentDefinition.DisplacementPump(10,1,3,1e-7),ComponentDefinition.PressureRelief(11,3,0,1e-10,1e6,heat:4),
            ComponentDefinition.PressureClutch(12,1,2,HydraulicChecks.Actuator(3),heat:4),ComponentDefinition.Torque(13,1,100,0)]
    };
    private static void Laws()
    {
        var pump=new HydraulicPump(2e-6);
        foreach(double w in new[]{-100.0,0,100}) foreach(double inlet in new[]{0.0,1e6,2e6}) foreach(double outlet in new[]{0.0,1e6,2e6})
        {
            Require(pump.TryEvaluate(w,inlet,outlet,out var r));
            Near(r.VolumeFlowCubicMetersPerSecond,2e-6*w,0); Near(r.ShaftTorqueNewtonMeters,-2e-6*(outlet-inlet),0);
            Near(r.HydraulicPowerWatts,-r.ShaftTorqueNewtonMeters*w,0);
            Near(r.HydraulicPowerWatts,(outlet-inlet)*r.VolumeFlowCubicMetersPerSecond,1e-12);
        }
        Require(!pump.TryEvaluate(double.NaN,0,0,out var invalid)&&invalid==default);
        Require(!pump.TryEvaluate(1,-1,0,out invalid)&&invalid==default);
        Require(!new HydraulicPump(double.MaxValue).TryEvaluate(10,0,1,out invalid)&&invalid==default);
        Throws<ArgumentException>(()=>new HydraulicPump(0)); Throws<ArgumentException>(()=>new HydraulicPump(-1));
        var relief=new HydraulicRestriction(1e-10,crackingPressurePascals:1e6);
        foreach(double dp in new[]{-2e6,-1e6,0,1e6,1.1e6,2e6})
        { Require(relief.TryEvaluate(2e6+dp,2e6,1,out var r)); Near(r.VolumeFlowCubicMetersPerSecond,1e-10*Math.Max(dp-1e6,0),1e-18); Near(r.HeatFlowWatts,dp*r.VolumeFlowCubicMetersPerSecond,1e-10); Require(r.HeatFlowWatts>=0); }
        Throws<ArgumentException>(()=>new HydraulicRestriction(1,1,1)); Throws<ArgumentException>(()=>new HydraulicRestriction(1,crackingPressurePascals:-1));
    }
    private static void Oscillation()
    {
        double nu=Math.Sqrt(2.5),time=.2,omega=100*Math.Cos(nu*time)-1/(.2*nu)*Math.Sin(nu*time);
        double pressure=1e6+1e6*Math.Cos(nu*time)+1e-6*100/(2e-12*nu)*Math.Sin(nu*time),previous=double.PositiveInfinity;
        foreach(ulong step in new ulong[]{4_000_000,2_000_000,1_000_000})
        {
            var s=CompiledModel.Compile(Oscillator(step)).CreateSimulation(); Ok(s.Step(200_000_000));
            double w=Value(s,1,Field.Speed),p=Value(s,2,Field.Pressure),error=Math.Abs(w-omega)+Math.Abs(p-pressure)*1e-5;
            Require(previous/error>3.9,$"Pump refinement {previous:R}/{error:R}"); previous=error;
            Near(Value(s,10,Field.HydraulicWork),.1*(10000-w*w),2e-10);
            Near(Value(s,0,Field.HydraulicWork),1e6*2e-12*(p-2e6),1e-10);
            Near(Value(s,0,Field.EnergyResidual),0,5e-10); Near(Value(s,0,Field.HydraulicVolumeResidual),0,1e-18);
            Require(Value(s,10,Field.HydraulicPower)>0 && Value(s,10,Field.Torque)<0);
        }
    }
    private static void Closed()
    {
        var d=Oscillator() with { Nodes=[NodeDefinition.Rotor(1,.2,-10),NodeDefinition.Hydraulic(2,2e-12,10e6),NodeDefinition.Hydraulic(3,3e-12,8e6)],
            Components=[ComponentDefinition.DisplacementPump(10,1,2,1e-6,3)] };
        var s=CompiledModel.Compile(d).CreateSimulation(); Ok(s.Step(100_000_000));
        double effective=1/(1/2e-12+1/3e-12),nu=1e-6/Math.Sqrt(.2*effective);
        double expected=-10*Math.Cos(nu*.1)-1e-6*2e6/(.2*nu)*Math.Sin(nu*.1);
        Near(Value(s,1,Field.Speed),expected,2e-6); Near(Value(s,2,Field.Pressure)*2e-12+Value(s,3,Field.Pressure)*3e-12,44e-6,1e-18);
        Require(Value(s,10,Field.HydraulicWork)<0 && Value(s,10,Field.VolumeFlow)<0);
        Near(Value(s,0,Field.HydraulicWork),0,0); Near(Value(s,0,Field.HydraulicVolumeResidual),0,1e-18); Near(Value(s,0,Field.EnergyResidual),0,1e-10);
        var gear=d with { Nodes=[..d.Nodes,NodeDefinition.Rotor(4,.05,-20)],Components=[..d.Components,ComponentDefinition.IdealGear(11,4,1,2)] };
        var g=CompiledModel.Compile(gear).CreateSimulation(); Ok(g.Step(100_000_000));
        Near(Value(g,4,Field.Speed),2*Value(g,1,Field.Speed),1e-12); Near(Value(g,11,Field.SlipSpeed),0,1e-12);
        Require(Math.Abs(Value(g,11,Field.Torque))>0); Near(Value(g,0,Field.EnergyResidual),0,1e-10);
    }
    private static void Relief()
    {
        var d=new ModelDefinition{StepNanoseconds=1_000_000,Nodes=[NodeDefinition.Hydraulic(1,2e-12,2e6),NodeDefinition.Thermal(2,100,300)],
            Components=[ComponentDefinition.PressureRelief(10,1,0,1e-10,1e6,heat:2)]};
        var s=CompiledModel.Compile(d).CreateSimulation(); Ok(s.Step(100_000_000));
        double p=1e6+1e6*Math.Pow((1-.025)/(1+.025),100);
        Near(Value(s,1,Field.Pressure),p,2e-8); Near(Value(s,10,Field.FluidHeat),.5*2e-12*(4e12-p*p),1e-12);
        Near(Value(s,0,Field.EnergyResidual),0,1e-8);
        var steady=new ModelDefinition{StepNanoseconds=1_000_000,Nodes=[NodeDefinition.Rotor(1,.2,100),NodeDefinition.Hydraulic(2,2e-12,2e6),NodeDefinition.Thermal(3,100,300)],
            Components=[ComponentDefinition.DisplacementPump(10,1,2,1e-6),ComponentDefinition.PressureRelief(11,2,0,1e-10,1e6,heat:3),ComponentDefinition.Torque(12,1,100,2)]};
        var t=CompiledModel.Compile(steady).CreateSimulation(); Ok(t.Step(200_000_000));
        Near(Value(t,1,Field.Speed),100,1e-12); Near(Value(t,2,Field.Pressure),2e6,1e-8);
        Near(Value(t,10,Field.Torque),-2,1e-12); Near(Value(t,10,Field.HydraulicWork),40,1e-10);
        Near(Value(t,11,Field.FluidHeat),40,1e-10); Near(Value(t,3,Field.Temperature),300.4,1e-9); Near(Value(t,0,Field.EnergyResidual),0,1e-8);
    }
    private static void Feedback()
    {
        const double displacement=1e-7, friction=6.4e-5, compliance=2e-12, time=.05;
        double nu=Math.Sqrt(displacement*(displacement+friction)/(.2*compliance));
        double expected=100*Math.Cos(nu*time)-(displacement+friction)*1e6/(.2*nu)*Math.Sin(nu*time);
        double pressure=1e6*Math.Cos(nu*time)+displacement*100/(compliance*nu)*Math.Sin(nu*time),previous=double.PositiveInfinity;
        foreach(ulong step in new ulong[]{2_000_000,1_000_000,500_000})
        {
            var d=ClutchModel(step);d=d with{Components=d.Components.Where(c=>c.Id!=11).Select(c=>c.Id==12 ? c with{HydraulicClutch=c.HydraulicClutch! with{PreloadForce=new(0,Unit.Newton)}} : c).ToArray()};
            var s=CompiledModel.Compile(d).CreateSimulation();Ok(s.Step(50_000_000));
            double w=Value(s,1,Field.Speed),error=Math.Abs(w-expected)+1e-5*Math.Abs(Value(s,3,Field.Pressure)-pressure);
            Require(previous/error>3.9,$"Pressure feedback refinement {previous:R}/{error:R}");previous=error;
            Require(Value(s,12,Field.ClutchMode)==2);
            Near(Value(s,2,Field.Speed),.2/.5*friction/(displacement+friction)*(100-w),2e-10);
            Near(Value(s,0,Field.EnergyResidual),0,2e-8);
        }
    }
    private static void Clutch()
    {
        var model=CompiledModel.Compile(ClutchModel());var s=model.CreateSimulation(); Ok(s.Step(600_000_000));
        Near(Value(s,12,Field.SlipSpeed),0,1e-9); Require(Value(s,12,Field.ClutchMode)==1);
        Require(Value(s,3,Field.Pressure)>1e6 && Value(s,12,Field.FrictionHeat)>0 && Value(s,10,Field.HydraulicWork)>0);
        Near(Value(s,0,Field.EnergyResidual),0,2e-7); Near(Value(s,0,Field.HydraulicWork),0,0);
        var a=s.Fork();var b=s.Fork();Ok(a.Step(100_000_000));for(int i=0;i<100;++i)Ok(b.Step(1_000_000));Require(Snapshot(a)==Snapshot(b));
        var fork=s.Fork();Ok(fork.SubmitInputs([new(100,2)]));Ok(fork.Step(100_000_000));Require(Snapshot(s)!=Snapshot(fork));
    }
    private static void Transactions()
    {
        var model=CompiledModel.Compile(ClutchModel());var s=model.CreateSimulation();Ok(s.Step(10_000_000));var before=Snapshot(s);
        Require(s.Step(100_000_000,cancellationToken:new CancellationToken(true))==SimulationStatus.Cancelled);Require(Snapshot(s)==before);
        Require(s.Step(100_000_000,[new(50_000_000,100,double.MaxValue)])==SimulationStatus.NumericalFailure);Require(Snapshot(s)==before);
        var retry=s.Fork();Ok(s.Step(100_000_000));Ok(retry.Step(100_000_000));Require(Snapshot(s)==Snapshot(retry));
        var buffer=new Scalar[model.OutputCount];for(int i=0;i<5;++i){Ok(s.Step(1_000_000));s.ReadSnapshot(buffer);}
        long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;++i){Ok(s.Step(1_000_000));s.ReadSnapshot(buffer);}Require(GC.GetAllocatedBytesForCurrentThread()==allocated,"Pump stepping allocated");
        var invalid=Oscillator();
        foreach(var c in new[]{invalid.Components[0] with{NodeB=1},invalid.Components[0] with{HydraulicPump=invalid.Components[0].HydraulicPump! with{InletNode=2}},
            invalid.Components[0] with{HydraulicPump=invalid.Components[0].HydraulicPump! with{Displacement=new(1,Unit.CubicMeter)}},invalid.Components[0] with{HeatNode=2}})
            Require(!CompiledModel.TryCompile(invalid with{Components=[c]},out _,out _));
        var negative=invalid with{Nodes=[NodeDefinition.Rotor(1,.2,-100),NodeDefinition.Hydraulic(2,2e-12,0)]};
        var drain=CompiledModel.Compile(negative).CreateSimulation();var initial=Snapshot(drain);Require(drain.Step(1_000_000)==SimulationStatus.NumericalFailure && Snapshot(drain)==initial);
    }
}
