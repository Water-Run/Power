// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Collections.ObjectModel;

namespace Power.Core;

public sealed record RavigneauxParameters
{
    public Quantity LargeSunInertia { get; init; }
    public Quantity SmallSunInertia { get; init; }
    public Quantity RingInertia { get; init; }
    /// <summary>Carrier structure inertia; the resolved path adds declared planet orbital inertia.</summary>
    public Quantity CarrierInertia { get; init; }
    public Quantity StaticCapacity { get; init; }
    public Quantity SlidingCapacity { get; init; }
    public double RingToLargeSun { get; init; }
    public double RingToSmallSun { get; init; }
    public double FinalDrive { get; init; }
}
public readonly record struct RavigneauxPorts(uint Input, uint Vehicle, uint Heat, uint LargeSun, uint SmallSun, uint Ring, uint Carrier,
    uint LargeMesh, uint SmallMesh, uint FinalMesh, uint CarrierInputClutch, uint SmallInputClutch, uint LargeInputClutch,
    uint CarrierBrake, uint LargeBrake, ulong CarrierCommand, ulong SmallCommand, ulong LargeCommand, ulong CarrierBrakeCommand, ulong LargeBrakeCommand);
public sealed record RavigneauxPlanetParameters
{
    public Quantity RingPitchRadius { get; init; }
    public int PlanetCount { get; init; }
    public Quantity InnerPlanetMass { get; init; }
    public Quantity OuterPlanetMass { get; init; }
    public Quantity InnerPlanetSpinInertia { get; init; }
    public Quantity OuterPlanetSpinInertia { get; init; }
}
public readonly record struct RavigneauxPlanetPorts(uint InnerPlanet, uint OuterPlanet, uint OuterRingMesh, uint InnerOuterMesh);
public readonly record struct RavigneauxPlanetGeometry(double LargeSunRadiusMeters, double SmallSunRadiusMeters,
    double InnerPlanetRadiusMeters, double OuterPlanetRadiusMeters, double InnerOrbitRadiusMeters, double OuterOrbitRadiusMeters,
    double OrbitalInertiaKilogramMeterSquared, double InnerSpinInertiaKilogramMeterSquared, double OuterSpinInertiaKilogramMeterSquared);
public sealed class RavigneauxGraph
{
    public ReadOnlyCollection<NodeDefinition> Nodes { get; }
    public ReadOnlyCollection<ComponentDefinition> Components { get; }
    public RavigneauxPorts Ports { get; }
    internal RavigneauxGraph(NodeDefinition[] nodes, ComponentDefinition[] components, RavigneauxPorts ports)
    { Nodes = Array.AsReadOnly(nodes); Components = Array.AsReadOnly(components); Ports = ports; }
    public Scalar[] RangeCommands(int range)
    {
        if (range < -1 || range > 4) throw new ArgumentException("Range is -1 reverse, 0 neutral or 1..4 forward.");
        return [new(Ports.CarrierCommand, range is 3 or 4 ? 1 : 0), new(Ports.SmallCommand, range is 1 or 2 or 3 ? 1 : 0),
            new(Ports.LargeCommand, range == -1 ? 1 : 0), new(Ports.CarrierBrakeCommand, range is 1 or -1 ? 1 : 0), new(Ports.LargeBrakeCommand, range is 2 or 4 ? 1 : 0)];
    }
}

/// <summary>One single-pinion and one double-pinion branch sharing ring/carrier; five explicit friction elements.</summary>
public sealed class RavigneauxTransmissionAssembly
{
    public RavigneauxParameters Parameters { get; }
    public RavigneauxTransmissionAssembly(RavigneauxParameters parameters)
    {
        if (parameters is null) throw new ArgumentNullException(nameof(parameters));
        Parameters = parameters with { };
        foreach (var inertia in new[] { parameters.LargeSunInertia, parameters.SmallSunInertia, parameters.RingInertia, parameters.CarrierInertia })
            if (inertia.Unit != Unit.KilogramMeterSquared || !Numeric.Finite(inertia.Value) || inertia.Value <= 0) throw new ArgumentException("Use explicit positive finite member inertias in kg m2.");
        if (parameters.StaticCapacity.Unit != Unit.NewtonMeter || parameters.SlidingCapacity.Unit != Unit.NewtonMeter ||
            !Numeric.Finite(parameters.StaticCapacity.Value) || !Numeric.Finite(parameters.SlidingCapacity.Value) || parameters.StaticCapacity.Value < parameters.SlidingCapacity.Value || parameters.SlidingCapacity.Value < 0)
            throw new ArgumentException("Use Nm static capacity at least the nonnegative sliding capacity.");
        if (!Numeric.Finite(parameters.RingToLargeSun) || !Numeric.Finite(parameters.RingToSmallSun) ||
            parameters.RingToLargeSun <= 1 || parameters.RingToSmallSun <= parameters.RingToLargeSun || !Numeric.Finite(parameters.FinalDrive) || parameters.FinalDrive <= 0)
            throw new ArgumentException("Require ring/small-sun ratio > ring/large-sun ratio > 1 and positive final-drive reduction.");
        if (!Numeric.Finite(parameters.RingToLargeSun + parameters.RingToSmallSun)) throw new ArgumentException("Compound ratio scale is not representable.");
    }
    public double RangeReduction(int range) => range switch
    {
        -1 => -Parameters.RingToLargeSun, 1 => Parameters.RingToSmallSun,
        2 => (Parameters.RingToLargeSun + Parameters.RingToSmallSun) / (1 + Parameters.RingToLargeSun),
        3 => 1, 4 => Parameters.RingToLargeSun / (1 + Parameters.RingToLargeSun),
        _ => throw new ArgumentException("A driven range is -1 or 1..4.")
    };
    public RavigneauxPlanetGeometry ResolvePlanetGeometry(RavigneauxPlanetParameters planets)
    {
        if (planets is null) throw new ArgumentNullException(nameof(planets));
        if (planets.PlanetCount < 1 || planets.PlanetCount > 32) throw new ArgumentException("Use 1..32 equal synchronous planet pairs.");
        foreach (var item in new[] { (planets.RingPitchRadius, Unit.Meter), (planets.InnerPlanetMass, Unit.Kilogram), (planets.OuterPlanetMass, Unit.Kilogram),
            (planets.InnerPlanetSpinInertia, Unit.KilogramMeterSquared), (planets.OuterPlanetSpinInertia, Unit.KilogramMeterSquared) })
            if (item.Item1.Unit != item.Item2 || !Numeric.Finite(item.Item1.Value) || item.Item1.Value <= 0) throw new ArgumentException("Use explicit positive finite SI pitch radius, per-planet masses and spin inertias.");
        double ring = planets.RingPitchRadius.Value, large = ring / Parameters.RingToLargeSun, small = ring / Parameters.RingToSmallSun;
        double outer = .5 * (ring - large), inner = .5 * (large - small), outerOrbit = .5 * (ring + large), innerOrbit = .5 * (large + small);
        if (inner <= 0 || outer <= 0 || !Numeric.Finite(outerOrbit) || !Numeric.Finite(innerOrbit)) throw new ArgumentException("Pitch geometry isn't representable; inspect ring radius and tooth ratios.");
        if (planets.PlanetCount > 1 && (outer > outerOrbit * Math.Sin(Math.PI / planets.PlanetCount) || inner > innerOrbit * Math.Sin(Math.PI / planets.PlanetCount)))
            throw new ArgumentException("Evenly spaced planet pairs overlap at the declared pitch radii; reduce planet count or inspect tooth ratios.");
        double orbit = planets.PlanetCount * (planets.OuterPlanetMass.Value * outerOrbit * outerOrbit + planets.InnerPlanetMass.Value * innerOrbit * innerOrbit);
        double ji = planets.PlanetCount * planets.InnerPlanetSpinInertia.Value, jo = planets.PlanetCount * planets.OuterPlanetSpinInertia.Value;
        if (!Numeric.Finite(orbit) || orbit <= 0 || !Numeric.Finite(ji) || !Numeric.Finite(jo) || !Numeric.Finite(orbit + Parameters.CarrierInertia.Value))
            throw new ArgumentException("Total orbital/spin inertia isn't representable; inspect masses, count, radius and inertia scales.");
        return new(large, small, inner, outer, innerOrbit, outerOrbit, orbit, ji, jo);
    }
    public RavigneauxGraph CreateResolvedGraph(RavigneauxPorts ports, RavigneauxPlanetPorts planetPorts, RavigneauxPlanetParameters planets,
        double ringSpeedRadiansPerSecond = 0, int range = 0)
    {
        var geometry = ResolvePlanetGeometry(planets); var graph = CreateGraph(ports, ringSpeedRadiansPerSecond, range);
        var ids = new HashSet<uint>(graph.Nodes.Select(n => n.Id).Concat(graph.Components.Select(c => c.Id))) { ports.Input, ports.Vehicle, ports.Heat };
        foreach (uint id in new[] { planetPorts.InnerPlanet, planetPorts.OuterPlanet, planetPorts.OuterRingMesh, planetPorts.InnerOuterMesh })
            if (id == 0 || !ids.Add(id)) throw new ArgumentException("Planet node/mesh bindings must be nonzero and distinct from every existing binding.");
        double carrier = graph.Nodes.Single(n => n.Id == ports.Carrier).Initial.Value;
        double relative = ringSpeedRadiansPerSecond - carrier;
        double outer = carrier + planets.RingPitchRadius.Value / geometry.OuterPlanetRadiusMeters * relative;
        double inner = carrier - planets.RingPitchRadius.Value / geometry.InnerPlanetRadiusMeters * relative;
        if (!Numeric.Finite(inner) || !Numeric.Finite(outer)) throw new ArgumentException("Initial planet speeds are not representable.");
        var nodes = graph.Nodes.Select(n => n.Id == ports.Carrier ? n with { Storage = new(Parameters.CarrierInertia.Value + geometry.OrbitalInertiaKilogramMeterSquared, Unit.KilogramMeterSquared) } : n)
            .Concat(new[] { NodeDefinition.Rotor(planetPorts.InnerPlanet, geometry.InnerSpinInertiaKilogramMeterSquared, inner), NodeDefinition.Rotor(planetPorts.OuterPlanet, geometry.OuterSpinInertiaKilogramMeterSquared, outer) }).ToArray();
        var components = new[]
        {
            ComponentDefinition.CarrierGear(ports.LargeMesh, ports.LargeSun, planetPorts.OuterPlanet, ports.Carrier, -geometry.OuterPlanetRadiusMeters / geometry.LargeSunRadiusMeters),
            ComponentDefinition.CarrierGear(ports.SmallMesh, ports.SmallSun, planetPorts.InnerPlanet, ports.Carrier, -geometry.InnerPlanetRadiusMeters / geometry.SmallSunRadiusMeters),
            ComponentDefinition.CarrierGear(planetPorts.OuterRingMesh, ports.Ring, planetPorts.OuterPlanet, ports.Carrier, geometry.OuterPlanetRadiusMeters / planets.RingPitchRadius.Value),
            ComponentDefinition.CarrierGear(planetPorts.InnerOuterMesh, planetPorts.InnerPlanet, planetPorts.OuterPlanet, ports.Carrier, -geometry.OuterPlanetRadiusMeters / geometry.InnerPlanetRadiusMeters)
        }.Concat(graph.Components.Skip(2)).ToArray();
        return new(nodes, components, ports);
    }
    public RavigneauxGraph CreateGraph(RavigneauxPorts ports, double ringSpeedRadiansPerSecond = 0, int range = 0)
    {
        if (!Numeric.Finite(ringSpeedRadiansPerSecond) || range < -1 || range > 4) throw new ArgumentException("Use finite ring speed and range -1..4.");
        var ids = new HashSet<uint>();
        foreach (uint id in new[] { ports.Input, ports.Vehicle, ports.LargeSun, ports.SmallSun, ports.Ring, ports.Carrier, ports.LargeMesh, ports.SmallMesh,
            ports.FinalMesh, ports.CarrierInputClutch, ports.SmallInputClutch, ports.LargeInputClutch, ports.CarrierBrake, ports.LargeBrake })
            if (id == 0 || !ids.Add(id)) throw new ArgumentException("All node/component bindings must be distinct and nonzero.");
        if (ports.Heat != 0 && !ids.Add(ports.Heat)) throw new ArgumentException("Thermal port conflicts with another binding.");
        var commands = new HashSet<ulong>();
        foreach (ulong channel in new[] { ports.CarrierCommand, ports.SmallCommand, ports.LargeCommand, ports.CarrierBrakeCommand, ports.LargeBrakeCommand })
            if (channel == 0 || channel >= 0x8000000000000000UL || !commands.Add(channel)) throw new ArgumentException("Actuator commands must be distinct nonzero input IDs.");
        double carrier = range switch { -1 or 1 => 0, 2 or 4 => Parameters.RingToLargeSun / (1 + Parameters.RingToLargeSun) * ringSpeedRadiansPerSecond,
            3 => ringSpeedRadiansPerSecond, _ => 0 };
        double large = (1 + Parameters.RingToLargeSun) * carrier - Parameters.RingToLargeSun * ringSpeedRadiansPerSecond;
        double small = Parameters.RingToSmallSun * ringSpeedRadiansPerSecond + (1 - Parameters.RingToSmallSun) * carrier;
        if (!Numeric.Finite(carrier) || !Numeric.Finite(large) || !Numeric.Finite(small)) throw new ArgumentException("Initial member speeds are not representable; inspect ratios and ring speed.");
        var nodes = new[] { NodeDefinition.Rotor(ports.LargeSun, Parameters.LargeSunInertia.Value, large), NodeDefinition.Rotor(ports.SmallSun, Parameters.SmallSunInertia.Value, small),
            NodeDefinition.Rotor(ports.Ring, Parameters.RingInertia.Value, ringSpeedRadiansPerSecond), NodeDefinition.Rotor(ports.Carrier, Parameters.CarrierInertia.Value, carrier) };
        double stat = Parameters.StaticCapacity.Value, slide = Parameters.SlidingCapacity.Value;
        var components = new[]
        {
            ComponentDefinition.PlanetaryGear(ports.LargeMesh, ports.LargeSun, ports.Ring, ports.Carrier, Parameters.RingToLargeSun),
            ComponentDefinition.DoublePinionPlanetary(ports.SmallMesh, ports.SmallSun, ports.Ring, ports.Carrier, Parameters.RingToSmallSun),
            ComponentDefinition.IdealGear(ports.FinalMesh, ports.Ring, ports.Vehicle, Parameters.FinalDrive),
            ComponentDefinition.Clutch(ports.CarrierInputClutch, ports.Input, ports.Carrier, stat, slide, ports.CarrierCommand, range is 3 or 4 ? 1 : 0, heat: ports.Heat),
            ComponentDefinition.Clutch(ports.SmallInputClutch, ports.Input, ports.SmallSun, stat, slide, ports.SmallCommand, range is 1 or 2 or 3 ? 1 : 0, heat: ports.Heat),
            ComponentDefinition.Clutch(ports.LargeInputClutch, ports.Input, ports.LargeSun, stat, slide, ports.LargeCommand, range == -1 ? 1 : 0, heat: ports.Heat),
            ComponentDefinition.Clutch(ports.CarrierBrake, ports.Carrier, 0, stat, slide, ports.CarrierBrakeCommand, range is 1 or -1 ? 1 : 0, heat: ports.Heat),
            ComponentDefinition.Clutch(ports.LargeBrake, ports.LargeSun, 0, stat, slide, ports.LargeBrakeCommand, range is 2 or 4 ? 1 : 0, heat: ports.Heat)
        };
        return new(nodes, components, ports);
    }
}
