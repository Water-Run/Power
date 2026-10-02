// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class HydraulicActuationChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("AT actuation assembly / actual fill drain piston clutch / immutable stable graph", Graph),
        ("AT hydraulic actuator / independent coupled pump-pressure-motion ODE / refinement", Reference),
        ("AT shared pressure supply / complete volume pump-work and routed heat ledgers", Ledgers),
        ("AT six piston branches / common supply / complete pressure travel and contact state", Six),
        ("AT actuator transactions / late rollback cancellation forks / no allocations", Transactions),
        ("AT actuator contracts / explicit units valid stroke branches and channel ownership", Contracts)
    ];
    internal static HydraulicActuationParameters Parameters()=>new()
    {
        LineCompliance=new(2e-11,Unit.CubicMeterPerPascal),InitialLinePressure=new(1e6,Unit.Pascal),
        PumpDisplacement=new(1e-6,Unit.CubicMeterPerRadian),PumpLeakage=new(1e-12,Unit.CubicMeterPerSecondPascal),
        PumpDrag=new(.02,Unit.NewtonMeterSecondPerRadian),ReliefConductance=new(5e-10,Unit.CubicMeterPerSecondPascal),
        ReliefPressure=new(1e6,Unit.Pascal),TankPressure=new(0,Unit.Pascal),ChamberCompliance=new(2e-12,Unit.CubicMeterPerPascal),
        PistonMass=new(.02,Unit.Kilogram),PistonArea=new(.001,Unit.SquareMeter),PistonBackArea=new(0,Unit.SquareMeter),ReturnStiffness=new(10000,Unit.NewtonPerMeter),
        ReturnDamping=new(300,Unit.NewtonSecondPerMeter),MinimumPosition=new(0,Unit.Meter),MaximumPosition=new(.006,Unit.Meter),
        StopStiffness=new(1e6,Unit.NewtonPerMeter),ContactPosition=new(.002,Unit.Meter),ContactStiffness=new(1e6,Unit.NewtonPerMeter),
        FillConductance=new(2e-10,Unit.CubicMeterPerSecondPascal),DrainConductance=new(2e-10,Unit.CubicMeterPerSecondPascal),
        EffectiveRadius=new(.08,Unit.Meter),StaticFriction=.4,SlidingFriction=.2,FrictionSurfaces=4
    };
    internal static HydraulicActuationPorts Ports(uint heat=3)=>new(1,1100,heat,1200,1201,1202,1203);
    internal static HydraulicActuationBranch Branch(ComponentDefinition clutch,int i,double apply=0)=>new(clutch,(uint)(1110+i),(uint)(1120+i),(uint)(1210+i),(uint)(1220+i),
        (uint)(1230+i),(uint)(1240+i),(ulong)(700+2*i),(ulong)(701+2*i),apply);
    internal static ModelDefinition Model(ulong tick=20_000,int branches=1)
    {
        var targets=Enumerable.Range(0,branches).Select(i=>Branch(ComponentDefinition.Clutch((uint)(300+i),2,4,400,300,heat:3),i,1)).ToArray();
        var graph=new HydraulicActuationAssembly(Parameters()).CreateGraph(Ports(),targets);
        return new(){StepNanoseconds=tick,Nodes=[NodeDefinition.Rotor(1,.2,60),NodeDefinition.Rotor(2,.2),NodeDefinition.Rotor(4,10),NodeDefinition.Thermal(3,1000,300),..graph.Nodes],
            Components=[..graph.Components,ComponentDefinition.Torque(10,1,500,10)]};
    }
    private static void Ok(SimulationStatus s){if(s!=SimulationStatus.Ok)throw new InvalidOperationException(s.ToString());}
    private static SnapshotInfo Snapshot(Simulation s)=>s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Graph()
    {
        var input=new[]{Branch(ComponentDefinition.Clutch(300,2,4,400,300,heat:3),0,1)};var graph=new HydraulicActuationAssembly(Parameters()).CreateGraph(Ports(),input);
        input[0]=input[0] with{Chamber=99};Require(graph.Branches[0].Chamber==1110&&graph.Nodes.Count==3&&graph.Components.Count==9);
        Require(graph.Components.Single(c=>c.Id==300).Kind==ComponentKind.PistonClutch&&graph.Components.Single(c=>c.Id==300).InputChannel==0);
        Require(graph.ValveInputs([0]).SequenceEqual(new Scalar[]{new(700,0),new(701,1)}));
        var s=CompiledModel.Compile(Model()).CreateSimulation();Near(Value(s,300,Field.StaticCapacity),0,0);Ok(s.Step(20_000_000));
        Require(Value(s,1110,Field.Pressure)>0&&Value(s,1120,Field.Displacement)>0);
    }
    private static double[] Derivative(double[] x)
    {
        double w=x[0],line=x[1],pressure=x[2],position=x[3],v=x[4];
        double fill=2e-10*(line-pressure),leak=1e-12*line,relief=5e-10*Math.Max(0,line-1e6);
        double reaction=-1e6*Math.Max(0,position-.002)+1e6*Math.Max(0,-position)-1e6*Math.Max(0,position-.006);
        return [(10-1e-6*line-.02*w)/.2,(1e-6*w-leak-relief-fill)/2e-11,(fill-.001*v)/2e-12,v,(.001*pressure-10000*position-300*v+reaction)/.02];
    }
    private static void Reference()
    {
        double[] x=[60,1e6,0,0,0];const double dt=5e-7;
        for(int n=0;n<40_000;++n)
        {
            var a=Derivative(x);double[] y=x.Select((v,i)=>v+.5*dt*a[i]).ToArray();var b=Derivative(y);
            y=x.Select((v,i)=>v+.5*dt*b[i]).ToArray();var c=Derivative(y);y=x.Select((v,i)=>v+dt*c[i]).ToArray();var d=Derivative(y);
            for(int i=0;i<x.Length;++i)x[i]+=dt*(a[i]+2*b[i]+2*c[i]+d[i])/6;
        }
        double previous=double.PositiveInfinity;
        foreach(ulong tick in new ulong[]{80_000,40_000,20_000})
        {
            var s=CompiledModel.Compile(Model(tick)).CreateSimulation();Ok(s.Step(20_000_000));
            double error=Math.Abs(Value(s,1,Field.Speed)-x[0])/100+Math.Abs(Value(s,1100,Field.Pressure)-x[1])/1e6+
                Math.Abs(Value(s,1110,Field.Pressure)-x[2])/1e6+Math.Abs(Value(s,1120,Field.Displacement)-x[3])/.01+Math.Abs(Value(s,1120,Field.LinearSpeed)-x[4]);
            Require(error<previous,$"Actuator refinement {previous:R}/{error:R}");previous=error;Near(Value(s,0,Field.EnergyResidual),0,1e-7);
        }
        Require(previous<1e-5,$"Actuator reference error {previous:R}");
    }
    private static void Ledgers()
    {
        var definition=Model();var s=CompiledModel.Compile(definition).CreateSimulation();Ok(s.Step(100_000_000));
        double pressureChange=2e-11*(Value(s,1100,Field.Pressure)-1e6)+2e-12*Value(s,1110,Field.Pressure),swept=.001*Value(s,1120,Field.Displacement);
        Near(pressureChange+swept,Value(s,0,Field.HydraulicVolumeIn),1e-16);Near(Value(s,0,Field.HydraulicVolumeResidual),0,1e-16);
        double losses=Value(s,1201,Field.FluidHeat)+Value(s,1203,Field.FluidHeat)+Value(s,1230,Field.FluidHeat)+Value(s,1240,Field.FluidHeat)+Value(s,1220,Field.FrictionHeat);
        double drag=10*Value(s,1,Field.Angle)-Value(s,1200,Field.HydraulicWork)-.5*.2*(Math.Pow(Value(s,1,Field.Speed),2)-3600);
        Near(1000*(Value(s,3,Field.Temperature)-300),losses+drag,1e-7);
        Near(Value(s,0,Field.EnergyResidual),0,1e-7);Require(Value(s,1200,Field.HydraulicWork)>0&&Value(s,1210,Field.ClampForce)>0);
    }
    private static void Six()
    {
        var s=CompiledModel.Compile(Model(branches:6)).CreateSimulation();Ok(s.Step(200_000_000));
        foreach(uint chamber in Enumerable.Range(1110,6).Select(i=>(uint)i))Require(Value(s,chamber,Field.Pressure)>0);
        foreach(uint piston in Enumerable.Range(1210,6).Select(i=>(uint)i))Require(Value(s,piston,Field.ClampForce)>0);
        Near(Value(s,0,Field.EnergyResidual),0,1e-6);Near(Value(s,0,Field.HydraulicVolumeResidual),0,1e-15);
    }
    private static void Transactions()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();var before=Snapshot(s);Require(s.Step(10_000_000,new CancellationToken(true))==SimulationStatus.Cancelled&&Snapshot(s)==before);
        var fork=s.Fork();Ok(s.Step(10_000_000));for(int i=0;i<10;++i)Ok(fork.Step(1_000_000));Require(Snapshot(s)==Snapshot(fork));
        before=Snapshot(s);Require(s.Step(10_000_000,[new(before.TimeNanoseconds+5_000_000,500,double.MaxValue)])==SimulationStatus.NumericalFailure&&Snapshot(s)==before);
        var values=new Scalar[s.Model.OutputCount];for(int i=0;i<10;++i){Ok(s.Step(20_000));s.ReadSnapshot(values);}long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<100;++i){Ok(s.Step(20_000));s.ReadSnapshot(values);}Require(bytes==GC.GetAllocatedBytesForCurrentThread());
    }
    private static void Contracts()
    {
        Throws<ArgumentException>(()=>new HydraulicActuationAssembly(Parameters() with{PistonArea=new(.001,Unit.Meter)}));
        Throws<ArgumentException>(()=>new HydraulicActuationAssembly(Parameters() with{ContactPosition=new(0,Unit.Meter)}));
        var a=new HydraulicActuationAssembly(Parameters());var branch=Branch(ComponentDefinition.Clutch(300,2,4,400,300,heat:3),0);
        Throws<ArgumentException>(()=>a.CreateGraph(Ports(),[branch with{Chamber=1}]));Throws<ArgumentException>(()=>a.CreateGraph(Ports(),[branch with{DrainCommand=700}]));
        Throws<ArgumentException>(()=>a.CreateGraph(Ports(),[]));Throws<ArgumentException>(()=>a.CreateGraph(Ports(),[branch]).ValveInputs([2]));
    }
}
