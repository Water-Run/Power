// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Finite, mixed liquid inventory at the pump's explicit prescribed inlet pressure.</summary>
public sealed record LiquidFuelTankDefinition
{
    public uint InjectorComponent { get; init; }
    public Quantity InitialMass { get; init; }
    public Quantity InitialTemperature { get; init; }
    public LiquidTankHeadspaceDefinition? Headspace { get; init; }
}

internal sealed record CompiledLiquidTank(int Component, int Injector, double InitialMass,
    double InitialTemperature, double InitialEnergy, double Density, double HeatingValue);

internal sealed class LiquidTankState(int count, bool headspaces = false, CompiledModel? equilibriumModel = null)
{
    internal readonly double[] Mass = new double[count], MassCorrection = new double[count];
    internal readonly double[] Energy = new double[count], EnergyCorrection = new double[count];
    internal readonly double[]? HeadspaceWork = headspaces ? new double[count] : null;
    internal readonly double[]? WorkCorrection = headspaces ? new double[count] : null;
    internal readonly double[]? Vaporized = equilibriumModel is null ? null : new double[count], VaporCorrection = equilibriumModel is null ? null : new double[count];
    internal readonly double[]? Condensed = equilibriumModel is null ? null : new double[count], CondensedCorrection = equilibriumModel is null ? null : new double[count];
    internal readonly double[]? TickPhase = equilibriumModel is null ? null : new double[count];
    internal void BeginTick() { if (TickPhase is not null) Array.Clear(TickPhase, 0, count); }
    internal bool EndTick(double duration)
    {
        if (TickPhase is not null)
            for (int i = 0; i < count; ++i)
            {
                if (TickPhase[i] >= 0) Numeric.Accumulate(TickPhase[i], ref Vaporized![i], ref VaporCorrection![i]);
                else Numeric.Accumulate(-TickPhase[i], ref Condensed![i], ref CondensedCorrection![i]);
                TickPhase[i] /= duration;
            }
        return Finite();
    }
    internal void CopyFrom(LiquidTankState other)
    {
        Array.Copy(other.Mass, Mass, Mass.Length); Array.Copy(other.MassCorrection, MassCorrection, Mass.Length);
        Array.Copy(other.Energy, Energy, Mass.Length); Array.Copy(other.EnergyCorrection, EnergyCorrection, Mass.Length);
        if (HeadspaceWork is not null)
        { Array.Copy(other.HeadspaceWork!, HeadspaceWork, Mass.Length); Array.Copy(other.WorkCorrection!, WorkCorrection!, Mass.Length); }
        if (TickPhase is not null)
        { Array.Copy(other.TickPhase!, TickPhase, count); Array.Copy(other.Vaporized!, Vaporized!, count); Array.Copy(other.VaporCorrection!, VaporCorrection!, count); Array.Copy(other.Condensed!, Condensed!, count); Array.Copy(other.CondensedCorrection!, CondensedCorrection!, count); }
    }
    internal ulong Hash(ulong hash)
    {
        for (int i = 0; i < Mass.Length; ++i)
        {
            hash = Numeric.Hash(hash, Mass[i]); hash = Numeric.Hash(hash, MassCorrection[i]);
            hash = Numeric.Hash(hash, Energy[i]); hash = Numeric.Hash(hash, EnergyCorrection[i]);
            if (HeadspaceWork is not null) { hash = Numeric.Hash(hash, HeadspaceWork[i]); hash = Numeric.Hash(hash, WorkCorrection![i]); }
            if (TickPhase is not null) { hash=Numeric.Hash(hash,TickPhase[i]);hash=Numeric.Hash(hash,Vaporized![i]);hash=Numeric.Hash(hash,VaporCorrection![i]);hash=Numeric.Hash(hash,Condensed![i]);hash=Numeric.Hash(hash,CondensedCorrection![i]); }
        }
        return hash;
    }
    internal bool Finite()
    {
        for (int i = 0; i < Mass.Length; ++i)
            if (!GearReference.Finite(Mass[i], MassCorrection[i], Energy[i], EnergyCorrection[i]) || Mass[i] < 0 || Mass[i] == 0 && Energy[i] != 0 ||
                HeadspaceWork is not null && (!Numeric.Finite(HeadspaceWork[i]) || !Numeric.Finite(WorkCorrection![i]))) return false;
        if (TickPhase is not null)
            for (int i=0;i<count;++i) if (!GearReference.Finite(TickPhase[i],Vaporized![i],VaporCorrection![i],Condensed![i]) || !Numeric.Finite(CondensedCorrection![i]) || Vaporized[i]<0 || Condensed[i]<0) return false;
        return true;
    }
}

public sealed partial class CompiledModel
{
    internal CompiledLiquidTank[] LiquidTanks { get; }
    internal int[] TankByComponent { get; }
    public bool HasLiquidTanks => LiquidTanks.Length != 0;
    private void CompileLiquidTanks(ComponentDefinition[] definitions)
    {
        int slot = 0;
        for (int i = 0; i < Components.Length; ++i)
        {
            if (Components[i].Kind != ComponentKind.LiquidFuelTank) continue;
            var c = Components[i]; var d = definitions[i].LiquidFuelTank!;
            int injector = Array.FindIndex(LiquidFuelInjectors, x => Components[x.Component].Id == d.InjectorComponent);
            Require(injector >= 0, DiagnosticCode.Connection, c.Id, "liquid_fuel_tank.injector_component", "Use the injector whose fuel properties define this tank.");
            var rail = LiquidFuelInjectors[injector]; var film = FuelFilms[rail.Film];
            Require(c.A == Components[rail.Component].A, DiagnosticCode.Connection, c.Id, "liquid_fuel_tank.node_a", "The tank and paired injector must use the same receiver.");
            double mass = Convert(d.InitialMass, Unit.Kilogram, c.Id, "liquid_fuel_tank.initial_mass");
            double temperature = Convert(d.InitialTemperature, Unit.Kelvin, c.Id, "liquid_fuel_tank.initial_temperature");
            Require(mass >= 0 && film.LiquidTemperatureValid(temperature),
                DiagnosticCode.Range, c.Id, "liquid_fuel_tank", "Use nonnegative inventory and liquid temperature inside the film's declared range.");
            double energy = film.LiquidState(mass, temperature).ThermalEnergyJoules;
            LiquidTanks[slot] = new(i, injector, mass, temperature, energy, rail.Rail.DensityKilogramsPerCubicMeter, rail.HeatingValue);
            TankByComponent[i] = slot++;
        }
    }
}
