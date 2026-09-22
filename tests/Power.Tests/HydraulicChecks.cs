// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class HydraulicChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("hydraulic law / signed linear and turbulent flow / passivity / finite ranges", Law),
        ("hydraulic network / analytic charging / second-order refinement / boundary work", Charge),
        ("hydraulic network / closed transfer / volume and thermal conservation", Equalization),
        ("hydraulic orifice / independent RK4 transient / nonlinear refinement", Turbulent),
        ("pressure clutch / analytic pressure and angular impulse / capture / release", Clutch),
        ("hydraulic network / whole-batch rollback / cancellation / forks / zero allocation", Transactions),
        ("hydraulic contracts / dimensions / topology / capacities / no cavitation clamp", Contracts)
    ];
    private static void Ok(SimulationStatus status) => Require(status == SimulationStatus.Ok, status.ToString());
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    internal static ModelDefinition ChargeModel(ulong step = 1_000_000) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Hydraulic(1,2e-12), NodeDefinition.Thermal(2,100,300)],
        Components = [ComponentDefinition.HydraulicResistance(10,1,0,5e-11,1e6,100,1,2)]
    };
    internal static HydraulicClutchDefinition Actuator(uint node) => new()
    {
        PressureNode=node, PistonArea=new(.001,Unit.SquareMeter), PreloadForce=new(100,Unit.Newton),
        EffectiveRadius=new(.08,Unit.Meter), StaticFriction=.4, SlidingFriction=.2, FrictionSurfaces=4
    };
    internal static ModelDefinition ClutchModel(ulong step = 1_000_000) => new()
    {
        StepNanoseconds=step,
        Nodes=[NodeDefinition.Rotor(1,.2,100),NodeDefinition.Rotor(2,.5),NodeDefinition.Hydraulic(3,2e-12),NodeDefinition.Thermal(4,100,300)],
        Components=[ComponentDefinition.HydraulicResistance(10,3,0,5e-11,1e6,100,1,4),
            ComponentDefinition.HydraulicResistance(11,3,0,5e-11,0,101,0,4),
            ComponentDefinition.PressureClutch(12,1,2,Actuator(3),heat:4),ComponentDefinition.Torque(13,2,102,0)]
    };
    private static void Law()
    {
        var linear=new HydraulicRestriction(2e-11); var turbulent=new HydraulicRestriction(1e-8,1000);
        foreach(double a in new[]{0.0,100,1e5,1e6}) foreach(double b in new[]{0.0,100,1e5,1e6})
        foreach(double opening in new[]{0.0,.3,1})
        {
            foreach(var law in new[]{linear,turbulent})
            {
                Require(law.TryEvaluate(a,b,opening,out var forward)); Require(law.TryEvaluate(b,a,opening,out var reverse));
                Near(forward.VolumeFlowCubicMetersPerSecond,-reverse.VolumeFlowCubicMetersPerSecond,1e-20);
                Near(forward.HeatFlowWatts,reverse.HeatFlowWatts,1e-12); Require(forward.HeatFlowWatts>=0);
                double q=law==linear ? 2e-11*opening*(a-b) : 1e-8*opening*(a-b)/Math.Pow((a-b)*(a-b)+1e6,.25);
                Near(forward.VolumeFlowCubicMetersPerSecond,q,1e-18);
            }
        }
        Require(!linear.TryEvaluate(-1,0,1,out var failed) && failed==default);
        Require(!linear.TryEvaluate(1,0,1.1,out failed) && failed==default);
        Require(!new HydraulicRestriction(double.MaxValue).TryEvaluate(1e6,0,1,out failed) && failed==default);
        Throws<ArgumentException>(()=>new HydraulicRestriction(-1)); Throws<ArgumentException>(()=>new HydraulicRestriction(1,double.NaN));
    }
    private static void Charge()
    {
        double previous=double.PositiveInfinity;
        foreach(ulong step in new ulong[]{2_000_000,1_000_000,500_000})
        {
            var s=CompiledModel.Compile(ChargeModel(step)).CreateSimulation(); Ok(s.Step(100_000_000));
            double p=Value(s,1,Field.Pressure), expected=1e6*(1-Math.Exp(-2.5)), error=Math.Abs(p-expected);
            Require(previous/error>3.9,$"Pressure refinement: {previous:R} / {error:R}"); previous=error;
            Near(Value(s,0,Field.HydraulicVolumeIn),2e-12*p,1e-18);
            Near(Value(s,0,Field.HydraulicWork),1e6*2e-12*p,1e-11);
            Near(Value(s,1,Field.InternalEnergy),.5*2e-12*p*p,1e-12);
            double heat=1e6*2e-12*p-.5*2e-12*p*p;
            Near(Value(s,10,Field.FluidHeat),heat,1e-10); Near(Value(s,2,Field.Temperature),300+heat/100,1e-10);
            Near(Value(s,0,Field.EnergyResidual),0,1e-8); Near(Value(s,0,Field.HydraulicVolumeResidual),0,1e-18);
            Require(Value(s,10,Field.VolumeFlow)<0);
        }
        var d=ChargeModel(); d=d with{Components=[d.Components[0] with{HeatNode=0}]};
        var external=CompiledModel.Compile(d).CreateSimulation(); Ok(external.Step(100_000_000));
        Near(Value(external,0,Field.HeatRejected),Value(external,10,Field.FluidHeat),1e-12);
        Near(Value(external,0,Field.EnergyResidual),0,1e-10);
    }
    private static void Equalization()
    {
        var d=new ModelDefinition { StepNanoseconds=1_000_000,Nodes=[NodeDefinition.Hydraulic(1,1e-12,1e6),NodeDefinition.Hydraulic(2,2e-12,1e5)],
            Components=[ComponentDefinition.HydraulicResistance(10,1,2,1e-11)] };
        var s=CompiledModel.Compile(d).CreateSimulation(); Ok(s.Step(200_000_000));
        double equilibrium=400000,delta=900000*Math.Pow((1-.0075)/(1+.0075),200);
        Near(Value(s,1,Field.Pressure),equilibrium+2*delta/3,1e-8);
        Near(Value(s,2,Field.Pressure),equilibrium-delta/3,1e-8);
        Near(Value(s,0,Field.HydraulicVolumeResidual),0,1e-19);
        Require(Value(s,0,Field.HydraulicVolumeIn)==0 && Value(s,0,Field.HydraulicWork)==0);
        Near(Value(s,0,Field.EnergyResidual),0,1e-12);
        var isolated=CompiledModel.Compile(d with{Components=[]}).CreateSimulation(); Ok(isolated.Step(100_000_000));
        Near(Value(isolated,1,Field.Pressure),1e6,0); Near(Value(isolated,0,Field.EnergyResidual),0,0);
    }
    private static void Turbulent()
    {
        const double supply=1e6,coefficient=2e-9,transition=10000,compliance=2e-12;
        double Rate(double p) { double dp=supply-p; return coefficient*dp/Math.Pow(dp*dp+transition*transition,.25)/compliance; }
        double expected=0,dt=1e-6;
        for(int i=0;i<100000;++i)
        { double a=Rate(expected),b=Rate(expected+.5*dt*a),c=Rate(expected+.5*dt*b),e=Rate(expected+dt*c); expected+=dt/6*(a+2*b+2*c+e); }
        double previous=double.PositiveInfinity;
        foreach(ulong step in new ulong[]{2_000_000,1_000_000,500_000})
        {
            var d=ChargeModel(step); d=d with {Components=[ComponentDefinition.HydraulicOrifice(10,1,0,coefficient,transition,supply,100,1,2)]};
            var s=CompiledModel.Compile(d).CreateSimulation(); Ok(s.Step(100_000_000));
            double error=Math.Abs(Value(s,1,Field.Pressure)-expected);
            Require(previous/error>3.9,$"Turbulent refinement: {previous:R} / {error:R}"); previous=error;
            Near(Value(s,0,Field.EnergyResidual),0,1e-8); Near(Value(s,0,Field.HydraulicVolumeResidual),0,1e-18);
        }
    }
    private static void Clutch()
    {
        double previous=double.PositiveInfinity;
        foreach(ulong step in new ulong[]{2_000_000,1_000_000,500_000})
        {
            var smooth=ClutchModel(step); smooth=smooth with {Components=smooth.Components.Select(c=>c.Id==12
                ? c with {HydraulicClutch=Actuator(3) with {PreloadForce=new(0,Unit.Newton)}} : c).ToArray()};
            var trial=CompiledModel.Compile(smooth).CreateSimulation(); Ok(trial.Step(100_000_000));
            double integral=.000064*1e6*(.1-.04*(1-Math.Exp(-2.5)));
            double error=Math.Abs(Value(trial,1,Field.Speed)-(100-integral/.2));
            Require(previous/error>3.9,$"Pressure-driven clutch impulse refinement: {previous:R} / {error:R}"); previous=error;
        }
        var d=ClutchModel(100_000); var s=CompiledModel.Compile(d).CreateSimulation();
        Require(Value(s,12,Field.ClampForce)==0 && Value(s,12,Field.StaticCapacity)==0);
        Ok(s.Step(100_000_000)); double threshold=-.04*Math.Log(.9),t=.1;
        double impulse=.000064*(900000*(t-threshold)+40000*(Math.Exp(-t/.04)-.9));
        Near(Value(s,1,Field.Speed),100-impulse/.2,.0001); Near(Value(s,2,Field.Speed),impulse/.5,.0001);
        double p=Value(s,3,Field.Pressure); Near(Value(s,12,Field.ClampForce),.001*p-100,1e-10);
        Near(Value(s,12,Field.StaticCapacity),.128*(.001*p-100),1e-10);
        Ok(s.Step(500_000_000)); Require(Value(s,12,Field.ClutchMode)==1);
        Near(Value(s,1,Field.Speed),200.0/7,1e-9); Near(Value(s,2,Field.Speed),200.0/7,1e-9);
        Near(Value(s,12,Field.FrictionHeat),1000-.5*.7*Math.Pow(200.0/7,2),1e-7);
        Ok(s.SubmitInputs([new(100,0),new(101,1),new(102,-10)])); Ok(s.Step(200_000_000));
        Require(Value(s,12,Field.ClutchMode)==0 && Value(s,12,Field.ClampForce)==0);
        Require(Value(s,1,Field.Speed)>Value(s,2,Field.Speed));
        double heat=Value(s,10,Field.FluidHeat)+Value(s,11,Field.FluidHeat)+Value(s,12,Field.FrictionHeat);
        Near(Value(s,4,Field.Temperature),300+heat/100,1e-8);
        Near(Value(s,0,Field.EnergyResidual),0,1e-7); Near(Value(s,0,Field.HydraulicVolumeResidual),0,1e-18);
    }
    private static void Transactions()
    {
        var model=CompiledModel.Compile(ClutchModel()); var s=model.CreateSimulation(); var initial=Snapshot(s);
        Require(s.SubmitInputs([new(100,0),new(101,1.01)])==SimulationStatus.InvalidInput && Snapshot(s)==initial);
        Require(s.Step(800_000_000,[new(700_000_000,102,double.MaxValue)])==SimulationStatus.NumericalFailure && Snapshot(s)==initial);
        using var cancel=new CancellationTokenSource(); cancel.Cancel();
        Require(s.Step(100_000_000,cancel.Token)==SimulationStatus.Cancelled && Snapshot(s)==initial);
        Ok(s.Step(600_000_000)); var split=model.CreateSimulation(); for(int i=0;i<60;++i) Ok(split.Step(10_000_000)); Require(Snapshot(s)==Snapshot(split));
        var before=Snapshot(s); var child=s.Fork(); Ok(child.SubmitInputs([new(100,0),new(101,1)])); Ok(child.Step(200_000_000)); Require(Snapshot(s)==before);
        for(int i=0;i<4;++i)
        {
            var capture=model.CreateSimulation(); var values=new Scalar[model.OutputCount];
            long start=GC.GetAllocatedBytesForCurrentThread(); var status=capture.Step(600_000_000); capture.ReadSnapshot(values);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-start;
            Require(status==SimulationStatus.Ok && allocated==0,$"Hydraulic capture: {status}, allocated {allocated}");
        }
    }
    private static void Contracts()
    {
        void Reject(ModelDefinition d,DiagnosticCode code,string field)
        { Require(!CompiledModel.TryCompile(d,out _,out var e) && e!.Code==code && e.Field==field,$"Expected {code}/{field}, got {e}"); }
        var d=ChargeModel();
        Reject(d with{Nodes=[d.Nodes[0] with{Initial=new(-1,Unit.Pascal)},d.Nodes[1]]},DiagnosticCode.Range,"initial");
        Reject(d with{Nodes=[d.Nodes[0] with{Storage=new(1,Unit.CubicMeter)},d.Nodes[1]]},DiagnosticCode.Unit,"storage");
        Reject(d with{Components=[d.Components[0] with{NodeB=2}]},DiagnosticCode.Connection,"node_b");
        Reject(d with{Components=[d.Components[0] with{InitialInput=new(1.01,Unit.Fraction)}]},DiagnosticCode.Range,"initial_input");
        var c=ClutchModel(); var actuator=c.Components[2];
        Reject(c with{Components=[actuator with{HydraulicClutch=Actuator(4)}]},DiagnosticCode.Connection,"hydraulic_clutch.pressure_node");
        Reject(c with{Components=[actuator with{HydraulicClutch=Actuator(3) with{SlidingFriction=.5}}]},DiagnosticCode.Range,"hydraulic_clutch");
        Reject(c with{Components=[actuator with{InputChannel=105}]},DiagnosticCode.Channel,"input_channel");
        var drain=d with{StepNanoseconds=1_000_000_000,Nodes=[NodeDefinition.Hydraulic(1,2e-12,1e6)],Components=[ComponentDefinition.HydraulicResistance(10,1,0,5e-11)]};
        var s=CompiledModel.Compile(drain).CreateSimulation(); var initial=Snapshot(s);
        Require(s.Step(1_000_000_000)==SimulationStatus.NumericalFailure && Snapshot(s)==initial);
    }
}
