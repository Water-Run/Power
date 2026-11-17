// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class AtControllerChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("AT feedback / actual pressure contact and physical range lock / release before apply", Shifts),
        ("AT feedback / signed reverse moving direction block / neutral recovery", Direction),
        ("AT feedback / actual supply loss and apply timeout / safe vent and recovery", Faults),
        ("AT feedback / blocked physical drain / release-timeout fault", ReleaseFault),
        ("AT feedback / loss of confirmed torque capacity / observable unloaded fault", LockLoss),
        ("AT feedback / integer target valve ownership / exact sample clock", Inputs),
        ("AT feedback / sampled pressure and bounded PI state / complete transactions", Transactions),
        ("AT feedback contracts / typed piston topology timings units and immutable routes", Contracts)
    ];
    internal static HydraulicAtControllerDefinition Definition()=>new()
    {
        VehicleNode=4,RingNode=102,SupplyPressureNode=1100,
        Routes=Enumerable.Range(0,5).Select(i=>new AtActuatorRoute((uint)(300+i),(uint)(1230+i),(uint)(1240+i))).ToArray(),
        SamplePeriodNanoseconds=1_000_000,ReleaseTimeoutNanoseconds=500_000_000,ApplyTimeoutNanoseconds=800_000_000,
        LowSupplyTimeoutNanoseconds=50_000_000,LockupDwellNanoseconds=50_000_000,
        ApplyPressure=new(9e5,Unit.Pascal),PressureTolerance=new(1e5,Unit.Pascal),MinimumSupplyPressure=new(2e5,Unit.Pascal),
        ReleaseForce=new(1,Unit.Newton),MinimumApplyForce=new(200,Unit.Newton),
        ProportionalGain=new(2e-6,Unit.FractionPerPascal),IntegralGain=new(2e-5,Unit.FractionPerPascalSecond),
        SynchronizeTolerance=new(1e-5,Unit.RadianPerSecond),DirectionChangeSpeedLimit=new(.1,Unit.RadianPerSecond),
        LockupSpeedLimit=new(5,Unit.RadianPerSecond),UnlockSpeedLimit=new(15,Unit.RadianPerSecond),
        MinimumLockupInputSpeed=new(30,Unit.RadianPerSecond),MinimumLockupForwardRange=2
    };
    internal static ModelDefinition Model(int requested=1)
    {
        var mechanical=ResolvedPlanetChecks.Graph();
        var branches=mechanical.Components.Where(c=>c.Kind==ComponentKind.Clutch).OrderBy(c=>c.Id).Select((c,i)=>HydraulicActuationChecks.Branch(c,i,0)).ToArray();
        var hydraulic=new HydraulicActuationAssembly(HydraulicActuationChecks.Parameters()).CreateGraph(HydraulicActuationChecks.Ports(),branches);
        return new(){StepNanoseconds=20_000,Nodes=[NodeDefinition.Rotor(1,.2,60),NodeDefinition.Rotor(4,10),NodeDefinition.Thermal(3,1000,300),..mechanical.Nodes,..hydraulic.Nodes],
            Components=[..mechanical.Components.Where(c=>c.Kind!=ComponentKind.Clutch),..hydraulic.Components,
                ComponentDefinition.Torque(10,1,500,20),ComponentDefinition.Torque(11,4,501,0),ComponentDefinition.AtControl(1400,1,900,requested,Definition())]};
    }
    private static void Ok(SimulationStatus s){if(s!=SimulationStatus.Ok)throw new InvalidOperationException(s.ToString());}
    private static SnapshotInfo Snapshot(Simulation s)=>s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Shifts()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();
        foreach(int range in new[]{1,2,3,4,3,2,1})
        {
            Ok(s.SubmitInputs([new(900,range)]));Ok(s.Step(500_000_000));
            Near(Value(s,1400,Field.ControlFault),0,0);Near(Value(s,1400,Field.ActualGear),range,0);
            double ratio=new RavigneauxTransmissionAssembly(RavigneauxChecks.Parameters()).RangeReduction(range)*4;
            Near(Value(s,1,Field.Speed),ratio*Value(s,4,Field.Speed),1e-7);Near(Value(s,0,Field.EnergyResidual),0,1e-6);
        }
        Ok(s.SubmitInputs([new(900,0)]));Ok(s.Step(500_000_000));Near(Value(s,1400,Field.ActualGear),0,0);Near(Value(s,1400,Field.ShiftPhase),(int)AtShiftPhase.Neutral,0);
        foreach(uint id in Enumerable.Range(300,5).Select(i=>(uint)i))Require(Value(s,id,Field.ClampForce)<=1);
    }
    private static void Direction()
    {
        var s=CompiledModel.Compile(Model(-1)).CreateSimulation();Ok(s.Step(800_000_000));Near(Value(s,1400,Field.ActualGear),-1,0);Require(Value(s,4,Field.Speed)<0);
        Ok(s.SubmitInputs([new(900,1)]));Ok(s.Step(1_000_000));Near(Value(s,1400,Field.ControlFault),(int)AtControlFault.DirectionChangeBlocked,0);
        Ok(s.SubmitInputs([new(900,0)]));Ok(s.Step(500_000_000));Near(Value(s,1400,Field.ControlFault),0,0);Near(Value(s,1400,Field.ActualGear),0,0);
    }
    private static void Faults()
    {
        var d=Model();var definitions=d.Components.ToArray();int fill=Array.FindIndex(definitions,c=>c.Id==1231);
        definitions[fill]=definitions[fill] with{HydraulicRestriction=new(){Coefficient=new(0,Unit.CubicMeterPerSecondPascal)}};
        var s=CompiledModel.Compile(d with{Components=definitions}).CreateSimulation();Ok(s.Step(900_000_000));Near(Value(s,1400,Field.ControlFault),(int)AtControlFault.ApplyTimeout,0);
        Ok(s.SubmitInputs([new(900,4)]));Ok(s.Step(800_000_000));Near(Value(s,1400,Field.ControlFault),0,0);Near(Value(s,1400,Field.ActualGear),4,0);
        d=Model();var nodes=d.Nodes.ToArray();int line=Array.FindIndex(nodes,n=>n.Id==1100);nodes[line]=nodes[line] with{Initial=new(0,Unit.Pascal)};
        definitions=d.Components.ToArray();int ctl=Array.FindIndex(definitions,c=>c.Id==1400);definitions[ctl]=definitions[ctl] with{HydraulicAtController=Definition() with{MinimumSupplyPressure=new(2e6,Unit.Pascal)}};
        s=CompiledModel.Compile(d with{Nodes=nodes,Components=definitions}).CreateSimulation();Ok(s.Step(100_000_000));Near(Value(s,1400,Field.ControlFault),(int)AtControlFault.LowSupplyPressure,0);
    }
    private static void ReleaseFault()
    {
        var d=Model();var components=d.Components.ToArray();int drain=Array.FindIndex(components,c=>c.Id==1241);
        components[drain]=components[drain] with{HydraulicRestriction=new(){Coefficient=new(0,Unit.CubicMeterPerSecondPascal)}};
        var s=CompiledModel.Compile(d with{Components=components}).CreateSimulation();Ok(s.Step(500_000_000));Near(Value(s,1400,Field.ActualGear),1,0);
        Ok(s.SubmitInputs([new(900,2)]));Ok(s.Step(600_000_000));Near(Value(s,1400,Field.ControlFault),(int)AtControlFault.ReleaseTimeout,0);Near(Value(s,1400,Field.ActualGear),0,0);
        Require(Value(s,301,Field.ClampForce)>1);
    }
    private static void LockLoss()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();Ok(s.Step(500_000_000));Near(Value(s,1400,Field.ActualGear),1,0);
        Ok(s.SubmitInputs([new(500,400)]));Ok(s.Step(1_000_000_000));Near(Value(s,1400,Field.ControlFault),(int)AtControlFault.ConfirmedLockLost,0);Near(Value(s,1400,Field.ActualGear),0,0);
    }
    private static void Inputs()
    {
        var model=CompiledModel.Compile(Model(0));var s=model.CreateSimulation();var before=Snapshot(s);
        Require(s.SubmitInputs([new(900,1.5)])==SimulationStatus.InvalidInput&&Snapshot(s)==before);
        Require(s.SubmitInputs([new(702,1)])==SimulationStatus.ControlledInput&&model.TryGetControlCommand(702,out var command)&&command.Id==900);
        Require(s.Step(1_000_000,[new(0,900,1.5)])==SimulationStatus.InvalidInput&&Snapshot(s)==before);
        Ok(s.SubmitInputs([new(900,1)]));Ok(s.Step(20_000));Near(Value(s,1400,Field.ActualGear),0,0);
        Require(model.Channels.Count(c=>c.IsInput)==3&&model.StateCount==99);
    }
    private static void Transactions()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();var before=Snapshot(s);Require(s.Step(10_000_000,new CancellationToken(true))==SimulationStatus.Cancelled&&Snapshot(s)==before);
        var fork=s.Fork();Ok(s.Step(10_000_000));for(int i=0;i<10;++i)Ok(fork.Step(1_000_000));Require(Snapshot(s)==Snapshot(fork));
        before=Snapshot(s);Require(s.Step(10_000_000,[new(before.TimeNanoseconds+5_000_000,500,double.MaxValue)])==SimulationStatus.NumericalFailure&&Snapshot(s)==before);
        var values=new Scalar[s.Model.OutputCount];for(int i=0;i<10;++i){Ok(s.Step(20_000));s.ReadSnapshot(values);}long bytes=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<100;++i){Ok(s.Step(20_000));s.ReadSnapshot(values);}Require(bytes==GC.GetAllocatedBytesForCurrentThread());
        foreach(Field field in new[]{Field.ActuatorIntegralA,Field.ActuatorIntegralB,Field.ActuatorIntegralC,Field.ActuatorIntegralD,Field.ActuatorIntegralE})Require(Math.Abs(Value(s,1400,field))<=1);
    }
    private static void Contracts()
    {
        var routes=Definition().Routes.ToArray();var owned=Definition() with{Routes=routes};routes[0]=new(999,1,2);Require(owned.Routes[0].Clutch==300);
        var d=Model();int ctl=Array.FindIndex(d.Components,c=>c.Id==1400);
        foreach(var bad in new[]{Definition() with{SamplePeriodNanoseconds=999_999},Definition() with{ApplyPressure=new(9e5,Unit.Newton)},Definition() with{Routes=Definition().Routes.Reverse().ToArray()}})
        {var components=d.Components.ToArray();components[ctl]=components[ctl] with{HydraulicAtController=bad};Throws<ModelCompileException>(()=>CompiledModel.Compile(d with{Components=components}));}
    }
}
