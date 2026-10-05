// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Material/thermal boundary paired with an existing conserving displacement pump.</summary>
public sealed record LiquidRailFeedDefinition
{
    public uint InjectorComponent { get; init; }
    public uint PumpComponent { get; init; }
    public Quantity SupplyTemperature { get; init; }
}
internal sealed record CompiledLiquidFeed(int Component, int Injector, int Pump, int Pressure, int Shaft,
    double Displacement, double Density, double SourceTemperature, double SourceThermalEnergy, double HeatingValue);
internal sealed class LiquidFeedState
{
    internal readonly double[] Mass, MassCorrection, Thermal, ThermalCorrection, Chemical, ChemicalCorrection;
    internal readonly double[] RailEnergy, RailCorrection;
    internal LiquidFeedState(int feeds, int injectors)
    {
        Mass=new double[feeds]; MassCorrection=new double[feeds]; Thermal=new double[feeds]; ThermalCorrection=new double[feeds];
        Chemical=new double[feeds]; ChemicalCorrection=new double[feeds]; RailEnergy=new double[injectors]; RailCorrection=new double[injectors];
    }
    internal void CopyFrom(LiquidFeedState other)
    {
        Array.Copy(other.Mass,Mass,Mass.Length);Array.Copy(other.MassCorrection,MassCorrection,Mass.Length);
        Array.Copy(other.Thermal,Thermal,Thermal.Length);Array.Copy(other.ThermalCorrection,ThermalCorrection,Mass.Length);
        Array.Copy(other.Chemical,Chemical,Mass.Length);Array.Copy(other.ChemicalCorrection,ChemicalCorrection,Mass.Length);
        Array.Copy(other.RailEnergy,RailEnergy,RailEnergy.Length);Array.Copy(other.RailCorrection,RailCorrection,RailEnergy.Length);
    }
    internal ulong Hash(ulong hash)
    {
        for(int i=0;i<Mass.Length;++i)
        {hash=Numeric.Hash(hash,Mass[i]);hash=Numeric.Hash(hash,MassCorrection[i]);hash=Numeric.Hash(hash,Thermal[i]);hash=Numeric.Hash(hash,ThermalCorrection[i]);hash=Numeric.Hash(hash,Chemical[i]);hash=Numeric.Hash(hash,ChemicalCorrection[i]);}
        for(int i=0;i<RailEnergy.Length;++i){hash=Numeric.Hash(hash,RailEnergy[i]);hash=Numeric.Hash(hash,RailCorrection[i]);}
        return hash;
    }
    internal bool Finite()
    {
        for(int i=0;i<Mass.Length;++i)if(!GearReference.Finite(Mass[i],MassCorrection[i],Thermal[i],ThermalCorrection[i])||!Numeric.Finite(Chemical[i])||!Numeric.Finite(ChemicalCorrection[i]))return false;
        for(int i=0;i<RailEnergy.Length;++i)if(!Numeric.Finite(RailEnergy[i])||!Numeric.Finite(RailCorrection[i]))return false;
        return true;
    }
}

public sealed partial class CompiledModel
{
    internal CompiledLiquidFeed[] LiquidFeeds { get; }
    internal int[] FeedByInjector { get; }
    internal int[] FeedByComponent { get; }
    public bool HasLiquidFeeds => LiquidFeeds.Length!=0;
    private void CompileLiquidFeeds(ComponentDefinition[] definitions)
    {
        int slot=0;var pumps=new HashSet<int>();var pressures=new HashSet<int>();
        for(int i=0;i<Components.Length;++i)
        {
            if(Components[i].Kind!=ComponentKind.LiquidRailFeed)continue;
            var c=Components[i];var d=definitions[i].LiquidRailFeed!;
            int injector=Array.FindIndex(LiquidFuelInjectors,x=>Components[x.Component].Id==d.InjectorComponent);
            int pump=Array.FindIndex(Components,x=>x.Id==d.PumpComponent&&x.Kind==ComponentKind.HydraulicPump);
            Require(injector>=0&&pump>=0,DiagnosticCode.Connection,c.Id,"liquid_rail_feed","Use an existing liquid injector and hydraulic displacement pump.");
            var p=Components[pump];var rail=LiquidFuelInjectors[injector];var node=Nodes[p.B];
            Require(c.A==Components[rail.Component].A&&p.C<0&&FeedByInjector[injector]<0&&pumps.Add(pump)&&pressures.Add(p.B),
                DiagnosticCode.Connection,c.Id,"liquid_rail_feed","A feed owns one injector/pressure node and one explicit-reservoir pump, at the injector receiver.");
            Require(node.Storage==rail.Rail.ComplianceCubicMetersPerPascal&&node.Initial==rail.Rail.InitialPressurePascals,
                DiagnosticCode.Connection,c.Id,"liquid_rail_feed.pressure_node","Hydraulic pressure storage and initial pressure must match the rail exactly.");
            Require(!Components.Any(x=>x.Kind is ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicRelief or ComponentKind.HydraulicSpoolValve&& (x.A==p.B||x.B==p.B))&&
                !Components.Any(x=>x.Kind==ComponentKind.HydraulicPump&&x.Id!=p.Id&&(x.B==p.B||x.C==p.B))&&
                !Components.Any(x=>x.Kind==ComponentKind.HydraulicPiston&&(x.B==p.B||x.C==p.B)),
                DiagnosticCode.Connection,c.Id,"liquid_rail_feed.pressure_node","The rail pressure node has only its paired pump and injector; untracked hydraulic fluid paths are unsupported.");
            double temperature=Convert(d.SupplyTemperature,Unit.Kelvin,c.Id,"liquid_rail_feed.supply_temperature");var film=FuelFilms[rail.Film];
            Require(temperature>0&&temperature<=film.Law.SaturationTemperatureKelvin,DiagnosticCode.Range,c.Id,"liquid_rail_feed.supply_temperature","Use liquid supply temperature in (0,saturation_temperature].");
            LiquidFeeds[slot]=new(i,injector,pump,node.Index,Nodes[p.A].Index,p.P0,rail.Rail.DensityKilogramsPerCubicMeter,temperature,film.Law.CreateState(1,temperature).ThermalEnergyJoules,rail.HeatingValue);
            FeedByInjector[injector]=slot;FeedByComponent[i]=slot;LiquidFuelInjectors[injector]=rail with{PressureNode=node.Index};++slot;
        }
    }
    internal LiquidRailSample RailSample(int injector, LiquidInjectorState state, HydraulicState? hydraulics)
    {
        var rail=LiquidFuelInjectors[injector].Rail;int pressure=LiquidFuelInjectors[injector].PressureNode;
        if(pressure<0)return rail.StateAfterDelivery(state.Quota.Total[injector]);
        double p=hydraulics!.Pressure[pressure];return new(rail.DensityKilogramsPerCubicMeter*(rail.ReferenceVolumeCubicMeters+rail.ComplianceCubicMetersPerPascal*p),p);
    }
}
