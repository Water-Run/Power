// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Tracked one-way relief return with an explicit fraction of loss heat carried by fuel.</summary>
public sealed record LiquidRailReturnDefinition
{
    public uint FeedComponent { get; init; }
    public uint ValveComponent { get; init; }
    public Quantity FluidHeatFraction { get; init; }
}
internal sealed record CompiledLiquidReturn(int Component, int Feed, int Valve, double HeatFraction);
internal sealed class LiquidReturnState(int count)
{
    internal readonly double[] Mass = new double[count], MassCorrection = new double[count];
    internal readonly double[] Thermal = new double[count], ThermalCorrection = new double[count];
    internal readonly double[] Chemical = new double[count], ChemicalCorrection = new double[count];
    internal readonly double[] Heat = new double[count], HeatCorrection = new double[count];
    internal void CopyFrom(LiquidReturnState other)
    {
        Array.Copy(other.Mass, Mass, Mass.Length); Array.Copy(other.MassCorrection, MassCorrection, Mass.Length);
        Array.Copy(other.Thermal, Thermal, Mass.Length); Array.Copy(other.ThermalCorrection, ThermalCorrection, Mass.Length);
        Array.Copy(other.Chemical, Chemical, Mass.Length); Array.Copy(other.ChemicalCorrection, ChemicalCorrection, Mass.Length);
        Array.Copy(other.Heat, Heat, Mass.Length); Array.Copy(other.HeatCorrection, HeatCorrection, Mass.Length);
    }
    internal ulong Hash(ulong hash)
    {
        for (int i = 0; i < Mass.Length; ++i)
        {
            hash = Numeric.Hash(hash, Mass[i]); hash = Numeric.Hash(hash, MassCorrection[i]);
            hash = Numeric.Hash(hash, Thermal[i]); hash = Numeric.Hash(hash, ThermalCorrection[i]);
            hash = Numeric.Hash(hash, Chemical[i]); hash = Numeric.Hash(hash, ChemicalCorrection[i]);
            hash = Numeric.Hash(hash, Heat[i]); hash = Numeric.Hash(hash, HeatCorrection[i]);
        }
        return hash;
    }
    internal bool Finite()
    {
        for (int i = 0; i < Mass.Length; ++i)
            if (!GearReference.Finite(Mass[i], MassCorrection[i], Thermal[i], ThermalCorrection[i]) ||
                !GearReference.Finite(Chemical[i], ChemicalCorrection[i], Heat[i], HeatCorrection[i]) || Mass[i] < 0 || Heat[i] < 0) return false;
        return true;
    }
}

public sealed partial class CompiledModel
{
    internal CompiledLiquidReturn[] LiquidReturns { get; }
    internal int[] ReturnByComponent { get; }
    internal int[][] ReturnsByFeed { get; }
    internal double[] ReturnHeatFractions { get; }
    public bool HasLiquidReturns => LiquidReturns.Length != 0;
    private void CompileLiquidReturns(ComponentDefinition[] definitions)
    {
        int slot = 0; var valves = new HashSet<int>();
        for (int i = 0; i < Components.Length; ++i)
        {
            if (Components[i].Kind != ComponentKind.LiquidRailReturn) continue;
            var c = Components[i]; var d = definitions[i].LiquidRailReturn!;
            int feed = Array.FindIndex(LiquidFeeds, x => Components[x.Component].Id == d.FeedComponent);
            int valve = Array.FindIndex(Components, x => x.Id == d.ValveComponent && x.Kind == ComponentKind.HydraulicRelief);
            Require(feed >= 0 && valve >= 0, DiagnosticCode.Connection, c.Id, "liquid_rail_return", "Use an existing feed and one-way hydraulic relief valve.");
            var f = LiquidFeeds[feed]; var p = Components[f.Pump]; var v = Components[valve];
            Require(c.A == Components[f.Component].A && v.A == p.B && v.B < 0 && v.P2 == p.P2 && valves.Add(valve),
                DiagnosticCode.Connection, c.Id, "liquid_rail_return.valve_component", "Own a distinct relief at this rail and the pump's prescribed inlet pressure.");
            double fraction = Convert(d.FluidHeatFraction, Unit.Fraction, c.Id, "liquid_rail_return.fluid_heat_fraction");
            Require(fraction >= 0 && fraction <= 1, DiagnosticCode.Range, c.Id,
                "liquid_rail_return.fluid_heat_fraction", "Use a fluid heat fraction in [0,1].");
            LiquidReturns[slot] = new(i, feed, valve, fraction); ReturnByComponent[i] = slot++; ReturnHeatFractions[valve] = fraction;
        }
        for (int i = 0; i < LiquidFeeds.Length; ++i)
            ReturnsByFeed[i] = Enumerable.Range(0, LiquidReturns.Length).Where(k => LiquidReturns[k].Feed == i).ToArray();
    }
}

public sealed partial class Simulation
{
    private bool TransferLiquidReturns(State s, int feedSlot, double dt, out double boundaryMass, out double boundaryEnergy)
    {
        boundaryMass = boundaryEnergy = 0;
        var feed = _model.LiquidFeeds[feedSlot]; var routes = _model.ReturnsByFeed[feedSlot]; var state = s.LiquidFeeds!;
        double pumpMass = feed.Tank < 0 ? feed.Density * feed.Displacement * dt * _mid[feed.Shaft + 1]
            : feed.Density * _hydraulics!.PumpVolume(_model.Components[feed.Pump].Index, _mid[feed.Shaft + 1], dt);
        double tank0 = feed.Tank < 0 ? 0 : s.LiquidTanks!.Mass[feed.Tank];
        if (feed.Tank >= 0 && pumpMass >= tank0) pumpMass = tank0;
        double returned = 0, recoveredHeat = 0;
        foreach (int k in routes)
        {
            var route = _model.LiquidReturns[k]; int valve = _model.Components[route.Valve].Index;
            returned += feed.Density * _hydraulics!.RestrictionVolume(valve, dt);
            recoveredHeat += route.HeatFraction * _hydraulics.RestrictionLoss(valve, dt);
        }
        double rail0 = _returnRailMassBefore[feedSlot], rail1 = _model.RailSample(feed.Injector, s.LiquidInjectors!, s.Hydraulic).MassKilograms;
        double tank1 = tank0 - pumpMass + returned, oldRailEnergy = state.RailEnergy[feed.Injector];
        double forward = Math.Max(0, pumpMass), backward = returned + Math.Max(0, -pumpMass), railSum = rail0 + rail1;
        if (!GearReference.Finite(pumpMass, returned, recoveredHeat, railSum) || returned < 0 || feed.Tank >= 0 && tank1 < 0 || railSum <= 0) return false;
        double outCoefficient = backward / railSum, nextRailEnergy, nextTankEnergy = 0, supplySpecific;
        if (outCoefficient > 1) return false;
        if (feed.Tank < 0)
        {
            supplySpecific = feed.SourceThermalEnergy;
            nextRailEnergy = (oldRailEnergy * (1 - outCoefficient) + forward * supplySpecific) / (1 + outCoefficient);
        }
        else
        {
            double oldTankEnergy = s.LiquidTanks!.Energy[feed.Tank], tankSum = tank0 + tank1;
            double inCoefficient = tankSum == 0 ? 0 : forward / tankSum, total = oldRailEnergy + oldTankEnergy + recoveredHeat;
            nextRailEnergy = (oldRailEnergy * (1 - outCoefficient) + inCoefficient * (oldTankEnergy + total)) / (1 + inCoefficient + outCoefficient);
            nextTankEnergy = total - nextRailEnergy;
            if (tank1 == 0) { nextTankEnergy = 0; nextRailEnergy = total; }
            supplySpecific = tankSum == 0 ? 0 : (oldTankEnergy + nextTankEnergy) / tankSum;
        }
        if (rail1 == 0) nextRailEnergy = 0;
        double railSpecific = (oldRailEnergy + nextRailEnergy) / railSum;
        double pumpCaloric = pumpMass * (pumpMass >= 0 ? supplySpecific : railSpecific), pumpChemical = pumpMass * feed.HeatingValue;
        Numeric.Accumulate(pumpMass, ref state.Mass[feedSlot], ref state.MassCorrection[feedSlot]);
        Numeric.Accumulate(pumpCaloric, ref state.Thermal[feedSlot], ref state.ThermalCorrection[feedSlot]);
        Numeric.Accumulate(pumpChemical, ref state.Chemical[feedSlot], ref state.ChemicalCorrection[feedSlot]);
        state.RailEnergy[feed.Injector] = nextRailEnergy; state.RailCorrection[feed.Injector] = 0;
        if (feed.Tank < 0) { boundaryMass = pumpMass; boundaryEnergy = pumpCaloric + pumpChemical; }
        else
        {
            s.LiquidTanks!.Mass[feed.Tank] = tank1; s.LiquidTanks.MassCorrection[feed.Tank] = 0;
            s.LiquidTanks.Energy[feed.Tank] = nextTankEnergy; s.LiquidTanks.EnergyCorrection[feed.Tank] = 0;
        }
        foreach (int k in routes)
        {
            var route = _model.LiquidReturns[k]; int valve = _model.Components[route.Valve].Index;
            double mass = feed.Density * _hydraulics!.RestrictionVolume(valve, dt), heat = route.HeatFraction * _hydraulics.RestrictionLoss(valve, dt);
            double caloric = mass * railSpecific + heat, chemical = mass * feed.HeatingValue; var returns = s.LiquidReturns!;
            Numeric.Accumulate(mass, ref returns.Mass[k], ref returns.MassCorrection[k]);
            Numeric.Accumulate(caloric, ref returns.Thermal[k], ref returns.ThermalCorrection[k]);
            Numeric.Accumulate(chemical, ref returns.Chemical[k], ref returns.ChemicalCorrection[k]);
            Numeric.Accumulate(heat, ref returns.Heat[k], ref returns.HeatCorrection[k]);
            if (feed.Tank < 0) { boundaryMass -= mass; boundaryEnergy -= caloric + chemical; }
        }
        if (feed.Tank < 0)
        {
            Numeric.Accumulate(boundaryMass, ref s.Mixture!.FuelIn, ref s.Mixture.FuelInCorrection);
            Numeric.Accumulate(boundaryMass * feed.HeatingValue, ref s.Mixture.ChemicalIn, ref s.Mixture.ChemicalInCorrection);
        }
        return GearReference.Finite(nextRailEnergy, nextTankEnergy, boundaryMass, boundaryEnergy);
    }
}
