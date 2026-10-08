// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record LiquidTankHeadspaceDefinition
{
    public Quantity Capacity { get; init; }
    public uint GasNode { get; init; }
}

public readonly record struct HeadspacePressureWork(double MeanPressurePascals,
    double PressureDerivativePascalsPerCubicMeter, double GasEnergyChangeJoules);

/// <summary>Rigid capacity, incompressible liquid and a finite ideal-gas headspace. No implicit vent or pressure source.</summary>
public sealed class LiquidTankHeadspace
{
    public double CapacityCubicMeters { get; }
    public double LiquidDensityKilogramsPerCubicMeter { get; }

    public LiquidTankHeadspace(double capacityCubicMeters, double liquidDensityKilogramsPerCubicMeter)
    {
        if (!Numeric.Finite(capacityCubicMeters) || !Numeric.Finite(liquidDensityKilogramsPerCubicMeter) ||
            capacityCubicMeters <= 0 || liquidDensityKilogramsPerCubicMeter <= 0)
            throw new ArgumentException("Use positive finite tank capacity and liquid density.");
        CapacityCubicMeters = capacityCubicMeters;
        LiquidDensityKilogramsPerCubicMeter = liquidDensityKilogramsPerCubicMeter;
    }

    public double GasVolumeCubicMeters(double liquidMassKilograms) =>
        CapacityCubicMeters - liquidMassKilograms / LiquidDensityKilogramsPerCubicMeter;

    /// <summary>Reversible pressure work over a signed liquid-volume withdrawal; positive withdrawal expands the gas.</summary>
    public static bool TryPressureWork(double gasVolumeCubicMeters, double withdrawalCubicMeters,
        double gasEnergyJoules, double gamma, out HeadspacePressureWork work)
    {
        work = default;
        if (!GearReference.Finite(gasVolumeCubicMeters, withdrawalCubicMeters, gasEnergyJoules, gamma) ||
            gasVolumeCubicMeters <= 0 || gasEnergyJoules <= 0 || gamma <= 1) return false;
        double z = withdrawalCubicMeters / gasVolumeCubicMeters;
        if (!Numeric.Finite(z) || z <= -1) return false;
        double phi, derivative;
        if (Math.Abs(gamma * z) < 1e-3)
        {
            phi = 1; derivative = -.5 * gamma;
            double term = 1, slopeTerm = derivative;
            for (int n = 1; n <= 7; ++n)
            {
                double factor = -(gamma + n - 1) * z;
                term *= factor / (n + 1); phi += term;
                if (n > 1) { slopeTerm *= factor * n / ((n - 1) * (n + 1)); derivative += slopeTerm; }
            }
        }
        else
        {
            double logarithm = Numeric.Log1p(z);
            phi = -Numeric.Expm1(-(gamma - 1) * logarithm) / ((gamma - 1) * z);
            derivative = (Math.Exp(-gamma * logarithm) - phi) / z;
        }
        double pressure = (gamma - 1) * gasEnergyJoules / gasVolumeCubicMeters;
        double mean = pressure * phi, slope = pressure / gasVolumeCubicMeters * derivative;
        double change = -mean * withdrawalCubicMeters;
        if (!GearReference.Finite(mean, slope, change, gasEnergyJoules + change) || mean <= 0 || gasEnergyJoules + change <= 0) return false;
        work = new(mean, slope, change); return true;
    }
}

internal sealed record CompiledTankHeadspace(int Tank, int Gas, LiquidTankHeadspace Geometry, int Feed, int Pump);

public sealed partial class CompiledModel
{
    internal int[] TankHeadspaceByNode { get; }
    internal CompiledTankHeadspace[] TankHeadspaces { get; }
    internal int[] HeadspaceByPump { get; }
    internal int[] HeadspaceByRestriction { get; }
    internal int[] HeadspaceByTank { get; }
    public bool HasTankHeadspaces => TankHeadspaces.Length != 0;

    private double[] PrepareTankHeadspaces(NodeDefinition[] nodes, ComponentDefinition[] components)
    {
        var volumes = new double[nodes.Length];
        for (int i = 0; i < components.Length; ++i)
        {
            var tank = components[i];
            if (tank.Kind != ComponentKind.LiquidFuelTank || tank.LiquidFuelTank?.Headspace is not { } definition) continue;
            int gas = Array.FindIndex(nodes, node => node.Id == definition.GasNode);
            int injector = Array.FindIndex(components, c => c.Id == tank.LiquidFuelTank.InjectorComponent && c.Kind == ComponentKind.LiquidFuelInjector);
            Require(gas >= 0 && nodes[gas].Domain == Domain.Gas && nodes[gas].Id != tank.NodeA &&
                TankHeadspaceByNode[gas] < 0 && GasCylinderByNode[gas] < 0,
                DiagnosticCode.Connection, tank.Id, "liquid_fuel_tank.headspace.gas_node", "Use a distinct gas node with one tank volume owner.");
            Require(nodes[gas].Storage == default, DiagnosticCode.Schema, nodes[gas].Id, "storage", "A tank headspace omits storage; capacity and liquid mass determine its volume.");
            Require(injector >= 0 && components[injector].LiquidFuelInjector is not null, DiagnosticCode.Connection,
                tank.Id, "liquid_fuel_tank.injector_component", "Use the injector whose liquid density defines this tank.");
            double capacity = Convert(definition.Capacity, Unit.CubicMeter, tank.Id, "liquid_fuel_tank.headspace.capacity");
            double density = Convert(components[injector].LiquidFuelInjector!.LiquidDensity, Unit.KilogramPerCubicMeter, tank.Id, "liquid_fuel_tank.headspace.density");
            double mass = Convert(tank.LiquidFuelTank.InitialMass, Unit.Kilogram, tank.Id, "liquid_fuel_tank.initial_mass");
            Require(capacity > 0 && density > 0 && mass >= 0 && Numeric.Finite(capacity - mass / density) && capacity - mass / density > 0,
                DiagnosticCode.Range, tank.Id, "liquid_fuel_tank.headspace.capacity", "Use positive capacity with a positive initial gas headspace.");
            TankHeadspaceByNode[gas] = i; volumes[gas] = capacity - mass / density;
        }
        return volumes;
    }

    private void CompileTankHeadspaceRoutes(ComponentDefinition[] definitions)
    {
        int slot = 0;
        for (int i = 0; i < LiquidTanks.Length; ++i)
        {
            var tank = LiquidTanks[i];
            if (definitions[tank.Component].LiquidFuelTank!.Headspace is not { } definition) continue;
            int node = Array.FindIndex(Nodes, n => n.Id == definition.GasNode);
            double capacity = Convert(definition.Capacity, Unit.CubicMeter, Components[tank.Component].Id, "liquid_fuel_tank.headspace.capacity");
            Require(!FuelFilms.Any(f => f.Gas == Nodes[node].Index), DiagnosticCode.Connection, Components[tank.Component].Id,
                "liquid_fuel_tank.headspace.gas_node", "The headspace has explicit gas transport and heat links; it cannot also own a liquid film receiver.");
            int feed = Array.FindIndex(LiquidFeeds, f => f.Tank == i);
            int pump = Components[LiquidFeeds[feed].Pump].Index;
            Require(Components[LiquidFeeds[feed].Pump].P2 == 0 && ReturnsByFeed[feed].All(route => Components[LiquidReturns[route].Valve].P2 == 0),
                DiagnosticCode.Schema, Components[tank.Component].Id, "liquid_fuel_tank.headspace", "A headspace owns inlet pressure; set paired pump and return reservoir pressure to zero.");
            TankHeadspaces[slot] = new(i, Nodes[node].Index, new(capacity, tank.Density), feed, pump);
            HeadspaceByTank[i] = slot;
            HeadspaceByPump[Components[LiquidFeeds[feed].Pump].Index] = slot;
            foreach (int route in ReturnsByFeed[feed]) HeadspaceByRestriction[Components[LiquidReturns[route].Valve].Index] = slot;
            ++slot;
        }
    }
}
