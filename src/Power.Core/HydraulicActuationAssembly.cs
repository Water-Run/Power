// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Collections.ObjectModel;

namespace Power.Core;

public sealed record HydraulicActuationParameters
{
    public Quantity LineCompliance { get; init; }
    public Quantity InitialLinePressure { get; init; }
    public Quantity PumpDisplacement { get; init; }
    public Quantity PumpLeakage { get; init; }
    public Quantity PumpDrag { get; init; }
    public Quantity ReliefConductance { get; init; }
    public Quantity ReliefPressure { get; init; }
    public Quantity TankPressure { get; init; }
    public Quantity ChamberCompliance { get; init; }
    public Quantity PistonMass { get; init; }
    public Quantity PistonArea { get; init; }
    public Quantity PistonBackArea { get; init; }
    public Quantity ReturnStiffness { get; init; }
    public Quantity ReturnDamping { get; init; }
    public Quantity MinimumPosition { get; init; }
    public Quantity MaximumPosition { get; init; }
    public Quantity StopStiffness { get; init; }
    public Quantity ContactPosition { get; init; }
    public Quantity ContactStiffness { get; init; }
    public Quantity FillConductance { get; init; }
    public Quantity DrainConductance { get; init; }
    public Quantity EffectiveRadius { get; init; }
    public double StaticFriction { get; init; }
    public double SlidingFriction { get; init; }
    public uint FrictionSurfaces { get; init; }
}
public readonly record struct HydraulicActuationPorts(uint PumpShaft, uint Line, uint Heat, uint Pump, uint Leakage, uint Drag, uint Relief);
public readonly record struct HydraulicActuationBranch(ComponentDefinition Clutch, uint Chamber, uint Slider, uint Piston, uint ReturnSpring,
    uint FillValve, uint DrainValve, ulong FillCommand, ulong DrainCommand, double InitialApply = 0);
public sealed class HydraulicActuationGraph
{
    public ReadOnlyCollection<NodeDefinition> Nodes { get; }
    public ReadOnlyCollection<ComponentDefinition> Components { get; }
    public ReadOnlyCollection<HydraulicActuationBranch> Branches { get; }
    internal HydraulicActuationGraph(NodeDefinition[] nodes, ComponentDefinition[] components, HydraulicActuationBranch[] branches)
    { Nodes = Array.AsReadOnly(nodes); Components = Array.AsReadOnly(components); Branches = Array.AsReadOnly(branches); }
    public Scalar[] ValveInputs(ReadOnlySpan<double> apply)
    {
        if (apply.Length != Branches.Count) throw new ArgumentException("Provide one apply fraction per declared branch.");
        var inputs = new Scalar[2 * apply.Length];
        for (int i = 0; i < apply.Length; ++i)
        {
            if (!Numeric.Finite(apply[i]) || apply[i] < 0 || apply[i] > 1) throw new ArgumentException("Apply fractions must be finite in [0,1].");
            inputs[2*i] = new(Branches[i].FillCommand, apply[i]); inputs[2*i+1] = new(Branches[i].DrainCommand, 1-apply[i]);
        }
        return inputs;
    }
}

/// <summary>Shared shaft-driven supply and explicit fill/drain pistons replacing declared friction commands.</summary>
public sealed class HydraulicActuationAssembly
{
    public HydraulicActuationParameters Parameters { get; }
    public HydraulicActuationAssembly(HydraulicActuationParameters parameters)
    {
        if (parameters is null) throw new ArgumentNullException(nameof(parameters)); Parameters = parameters with { };
        static void Check(Quantity q, Unit unit, bool positive)
        {
            if (q.Unit != unit || !Numeric.Finite(q.Value) || (positive ? q.Value <= 0 : q.Value < 0)) throw new ArgumentException($"Use explicit {(positive ? "positive" : "nonnegative")} finite {unit} parameters.");
        }
        Check(parameters.LineCompliance,Unit.CubicMeterPerPascal,true); Check(parameters.InitialLinePressure,Unit.Pascal,false);
        Check(parameters.PumpDisplacement,Unit.CubicMeterPerRadian,true); Check(parameters.PumpLeakage,Unit.CubicMeterPerSecondPascal,false);
        Check(parameters.PumpDrag,Unit.NewtonMeterSecondPerRadian,false); Check(parameters.ReliefConductance,Unit.CubicMeterPerSecondPascal,true);
        Check(parameters.ReliefPressure,Unit.Pascal,false); Check(parameters.TankPressure,Unit.Pascal,false);
        Check(parameters.ChamberCompliance,Unit.CubicMeterPerPascal,true); Check(parameters.PistonMass,Unit.Kilogram,true);
        Check(parameters.PistonArea,Unit.SquareMeter,true); Check(parameters.PistonBackArea,Unit.SquareMeter,false); Check(parameters.ReturnStiffness,Unit.NewtonPerMeter,true);
        Check(parameters.ReturnDamping,Unit.NewtonSecondPerMeter,false); Check(parameters.StopStiffness,Unit.NewtonPerMeter,false);
        Check(parameters.ContactStiffness,Unit.NewtonPerMeter,true); Check(parameters.FillConductance,Unit.CubicMeterPerSecondPascal,true);
        Check(parameters.DrainConductance,Unit.CubicMeterPerSecondPascal,true); Check(parameters.EffectiveRadius,Unit.Meter,true);
        foreach(var q in new[]{parameters.MinimumPosition,parameters.MaximumPosition,parameters.ContactPosition})
            if(q.Unit!=Unit.Meter||!Numeric.Finite(q.Value))throw new ArgumentException("Use finite stroke/contact positions in meters.");
        if(parameters.MinimumPosition.Value>=parameters.MaximumPosition.Value || parameters.ContactPosition.Value<=parameters.MinimumPosition.Value || parameters.ContactPosition.Value>=parameters.MaximumPosition.Value)
            throw new ArgumentException("Contact must lie strictly inside the increasing nominal stroke.");
        if(!Numeric.Finite(parameters.StaticFriction)||!Numeric.Finite(parameters.SlidingFriction)||parameters.SlidingFriction<0||parameters.StaticFriction<parameters.SlidingFriction||parameters.FrictionSurfaces==0)
            throw new ArgumentException("Use finite static friction at least nonnegative sliding friction and positive surface count.");
    }
    public HydraulicActuationGraph CreateGraph(HydraulicActuationPorts ports, IReadOnlyList<HydraulicActuationBranch> branches)
    {
        if(branches is null)throw new ArgumentNullException(nameof(branches));if(branches.Count<1||branches.Count>6)throw new ArgumentException("Use 1..6 explicitly bound actuator branches.");
        var copied=branches.ToArray();var external=new HashSet<uint>{ports.PumpShaft};if(ports.Heat!=0)external.Add(ports.Heat);
        foreach(var branch in copied)
        {
            if(branch.Clutch is null||branch.Clutch.Kind!=ComponentKind.Clutch||branch.Clutch.NodeA==0||branch.Clutch.NodeA==branch.Clutch.NodeB)throw new ArgumentException("Each branch replaces one ordinary clutch with valid rotor/ground ports.");
            external.Add(branch.Clutch.NodeA);if(branch.Clutch.NodeB!=0)external.Add(branch.Clutch.NodeB);
            if(branch.Clutch.HeatNode!=0&&branch.Clutch.HeatNode!=ports.Heat)throw new ArgumentException("Branch heat must share the explicit supply heat sink.");
        }
        if(ports.PumpShaft==0||ports.Heat!=0&&ports.Heat==ports.PumpShaft)throw new ArgumentException("Supply shaft must be nonzero and distinct from the thermal port.");
        var ids=new HashSet<uint>(external);var channels=new HashSet<ulong>();
        foreach(uint id in new[]{ports.Line,ports.Pump,ports.Leakage,ports.Drag,ports.Relief})
            if(id==0||!ids.Add(id))throw new ArgumentException("Supply bindings must be distinct and nonzero.");
        foreach(var branch in copied)
        {
            foreach(uint id in new[]{branch.Clutch.Id,branch.Chamber,branch.Slider,branch.Piston,branch.ReturnSpring,branch.FillValve,branch.DrainValve})
                if(id==0||!ids.Add(id))throw new ArgumentException("Every branch node/component binding must be globally distinct and nonzero.");
            foreach(ulong channel in new[]{branch.FillCommand,branch.DrainCommand})
                if(channel==0||channel>=0x8000000000000000UL||!channels.Add(channel))throw new ArgumentException("Valve input IDs must be distinct, nonzero and below the output range.");
            if(!Numeric.Finite(branch.InitialApply)||branch.InitialApply<0||branch.InitialApply>1)throw new ArgumentException("Initial apply must be a finite fraction.");
        }
        var p=Parameters;var nodes=new List<NodeDefinition>{NodeDefinition.Hydraulic(ports.Line,p.LineCompliance.Value,p.InitialLinePressure.Value)};
        var components=new HydraulicPumpAssembly(p.PumpDisplacement.Value,p.PumpLeakage.Value,p.PumpDrag.Value)
            .CreateComponents(ports.Pump,ports.Leakage,ports.Drag,ports.PumpShaft,ports.Line,reservoirPressurePascals:p.TankPressure.Value,heat:ports.Heat).ToList();
        components.Add(ComponentDefinition.PressureRelief(ports.Relief,ports.Line,0,p.ReliefConductance.Value,p.ReliefPressure.Value,p.TankPressure.Value,ports.Heat));
        foreach(var branch in copied)
        {
            nodes.Add(NodeDefinition.Hydraulic(branch.Chamber,p.ChamberCompliance.Value,p.TankPressure.Value));
            nodes.Add(NodeDefinition.Translational(branch.Slider,p.PistonMass.Value,displacementMeters:p.MinimumPosition.Value));
            components.Add(ComponentDefinition.Piston(branch.Piston,branch.Slider,branch.Chamber,new(){FrontArea=p.PistonArea,BackArea=p.PistonBackArea,BackPressure=p.TankPressure,
                MinimumPosition=p.MinimumPosition,MaximumPosition=p.MaximumPosition,StopStiffness=p.StopStiffness,ContactPosition=p.ContactPosition,ContactStiffness=p.ContactStiffness}));
            components.Add(ComponentDefinition.LinearSpring(branch.ReturnSpring,branch.Slider,0,p.ReturnStiffness.Value,p.ReturnDamping.Value,p.MinimumPosition.Value,ports.Heat));
            components.Add(ComponentDefinition.ContactClutch(branch.Clutch.Id,branch.Clutch.NodeA,branch.Clutch.NodeB,new(){PistonComponent=branch.Piston,EffectiveRadius=p.EffectiveRadius,
                StaticFriction=p.StaticFriction,SlidingFriction=p.SlidingFriction,FrictionSurfaces=p.FrictionSurfaces},ports.Heat,branch.Clutch.Ratio));
            components.Add(ComponentDefinition.HydraulicResistance(branch.FillValve,ports.Line,branch.Chamber,p.FillConductance.Value,channel:branch.FillCommand,opening:branch.InitialApply,heat:ports.Heat));
            components.Add(ComponentDefinition.HydraulicResistance(branch.DrainValve,branch.Chamber,0,p.DrainConductance.Value,p.TankPressure.Value,branch.DrainCommand,1-branch.InitialApply,ports.Heat));
        }
        return new(nodes.ToArray(),components.ToArray(),copied);
    }
}
