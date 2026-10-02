// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Collections.ObjectModel;

namespace Power.Core;

public sealed record DualClutchParameters
{
    public Quantity OddShaftInertia { get; init; }
    public Quantity EvenShaftInertia { get; init; }
    public Quantity OutputAInertia { get; init; }
    public Quantity OutputBInertia { get; init; }
    public Quantity ReverseOutputInertia { get; init; }
    public Quantity HubInertia { get; init; }
    public Quantity ReverseIdlerInertia { get; init; }
    public Quantity DriveStaticCapacity { get; init; }
    public Quantity DriveSlidingCapacity { get; init; }
    public Quantity SelectorStaticCapacity { get; init; }
    public Quantity SelectorSlidingCapacity { get; init; }
    public double FinalDriveA { get; init; }
    public double FinalDriveB { get; init; }
    public double ReverseFinalDrive { get; init; }
}

public readonly record struct DualClutchPorts(uint Engine, uint Vehicle, uint Heat, uint OddShaft, uint EvenShaft,
    uint OutputA, uint OutputB, uint ReverseOutput, uint ReverseIdler, uint OddClutch, uint EvenClutch,
    uint FinalA, uint FinalB, uint FinalReverse, uint ReverseFirstMesh, ulong OddCommand, ulong EvenCommand);
public readonly record struct DualClutchGearIds(uint Hub, uint Mesh, uint Selector, ulong Command);

public sealed class DualClutchGraph
{
    public ReadOnlyCollection<NodeDefinition> Nodes { get; }
    public ReadOnlyCollection<ComponentDefinition> Components { get; }
    public ReadOnlyCollection<DualClutchGearIds> Gears { get; }
    public DualClutchPorts Ports { get; }
    internal DualClutchGraph(NodeDefinition[] nodes, ComponentDefinition[] components, DualClutchGearIds[] gears, DualClutchPorts ports)
    { Nodes = Array.AsReadOnly(nodes); Components = Array.AsReadOnly(components); Gears = Array.AsReadOnly(gears); Ports = ports; }

    /// <summary>Release every selector on one input path before applying the target. Drive-clutch commands are separate.</summary>
    public Scalar[] SelectPath(int gear, bool oddPath)
    {
        if (gear != 0 && (gear < -1 || gear > 7 || (gear > 0 && (gear % 2 == 1) != oddPath) || gear == -1 && oddPath))
            throw new ArgumentException("Select an odd forward gear on the odd path, an even/reverse gear on the even path, or neutral.");
        var result = new List<Scalar>();
        for (int i = 0; i < Gears.Count; ++i)
        {
            int number = i == 7 ? -1 : i + 1; bool odd = number > 0 && number % 2 == 1;
            if (odd == oddPath) result.Add(new(Gears[i].Command, number == gear ? 1 : 0));
        }
        return result.ToArray();
    }
}

/// <summary>Seven forward constant-mesh paths, an even-path reverse idler, and three output/final-drive branches.</summary>
public sealed class DualClutchTransmissionAssembly
{
    private readonly double[] _ratios;
    private readonly double _reverse;
    private readonly DualClutchParameters _parameters;
    public ReadOnlyCollection<double> ForwardReductions { get; }
    public double ReverseReduction => _reverse;
    public DualClutchParameters Parameters => _parameters;
    public DualClutchTransmissionAssembly(ReadOnlySpan<double> forwardReductions, double reverseReduction, DualClutchParameters parameters)
    {
        if (parameters is null || forwardReductions.Length != 7 || !Numeric.Finite(reverseReduction) || reverseReduction <= 0)
            throw new ArgumentException("Supply seven positive forward reductions, positive reverse reduction and explicit parameters.");
        _ratios = forwardReductions.ToArray(); _reverse = reverseReduction; _parameters = parameters with { };
        foreach (double ratio in _ratios) if (!Numeric.Finite(ratio) || ratio <= 0) throw new ArgumentException("Gear reductions must be finite and positive.");
        foreach (var inertia in new[] { parameters.OddShaftInertia, parameters.EvenShaftInertia, parameters.OutputAInertia,
            parameters.OutputBInertia, parameters.ReverseOutputInertia, parameters.HubInertia, parameters.ReverseIdlerInertia }) RequireQuantity(inertia, Unit.KilogramMeterSquared, true);
        foreach (var torque in new[] { parameters.DriveStaticCapacity, parameters.DriveSlidingCapacity, parameters.SelectorStaticCapacity, parameters.SelectorSlidingCapacity })
            RequireQuantity(torque, Unit.NewtonMeter, false);
        if (parameters.DriveStaticCapacity.Value < parameters.DriveSlidingCapacity.Value || parameters.SelectorStaticCapacity.Value < parameters.SelectorSlidingCapacity.Value)
            throw new ArgumentException("Static capacities must be at least sliding capacities.");
        foreach (double ratio in new[] { parameters.FinalDriveA, parameters.FinalDriveB, parameters.ReverseFinalDrive })
            if (!Numeric.Finite(ratio) || ratio <= 0) throw new ArgumentException("Final-drive reductions must be finite and positive.");
        for (int gear = 1; gear <= 7; ++gear) if (!Numeric.Finite(EffectiveReduction(gear))) throw new ArgumentException("Effective reductions must be representable.");
        if (!Numeric.Finite(EffectiveReduction(-1))) throw new ArgumentException("Reverse effective reduction must be representable.");
        for (int i = 1; i < 7; ++i)
            if (EffectiveReduction(i + 1) >= EffectiveReduction(i)) throw new ArgumentException("Effective forward reductions must decrease with gear number.");
        ForwardReductions = Array.AsReadOnly(_ratios);
    }
    private static void RequireQuantity(Quantity quantity, Unit unit, bool positive)
    {
        if (quantity.Unit != unit || !Numeric.Finite(quantity.Value) || (positive ? quantity.Value <= 0 : quantity.Value < 0))
            throw new ArgumentException($"Use explicit {unit} with finite {(positive ? "positive" : "nonnegative")} value.");
    }
    public double EffectiveReduction(int gear)
    {
        if (gear == -1) return -_reverse * _parameters.ReverseFinalDrive;
        if (gear < 1 || gear > 7) throw new ArgumentException("Gear must be 1..7 or -1 for reverse.");
        return _ratios[gear - 1] * (gear <= 4 ? _parameters.FinalDriveA : _parameters.FinalDriveB);
    }

    /// <summary>Only internal nodes are returned. Caller supplies engine, vehicle and thermal nodes at the declared ports.</summary>
    public DualClutchGraph CreateGraph(DualClutchPorts ports, ReadOnlySpan<DualClutchGearIds> gearIds,
        double vehicleSpeedRadiansPerSecond = 0, int selectedOdd = 0, int selectedEven = 0,
        double oddEngagement = 0, double evenEngagement = 0)
    {
        if (gearIds.Length != 8 || !Numeric.Finite(vehicleSpeedRadiansPerSecond) || !Numeric.Finite(oddEngagement) || !Numeric.Finite(evenEngagement) ||
            oddEngagement < 0 || oddEngagement > 1 || evenEngagement < 0 || evenEngagement > 1 ||
            selectedOdd != 0 && (selectedOdd < 1 || selectedOdd > 7 || selectedOdd % 2 != 1) ||
            selectedEven != 0 && selectedEven != -1 && (selectedEven < 2 || selectedEven > 6 || selectedEven % 2 != 0))
            throw new ArgumentException("Use eight gear ID bindings, finite vehicle speed, valid odd/even selections and engagement fractions.");
        var ids = new HashSet<uint>();
        void Id(uint id) { if (id == 0 || !ids.Add(id)) throw new ArgumentException("All node/component IDs must be distinct and nonzero."); }
        foreach (uint id in new[] { ports.Engine, ports.Vehicle, ports.OddShaft, ports.EvenShaft, ports.OutputA, ports.OutputB,
            ports.ReverseOutput, ports.ReverseIdler, ports.OddClutch, ports.EvenClutch, ports.FinalA, ports.FinalB, ports.FinalReverse, ports.ReverseFirstMesh }) Id(id);
        if (ports.Heat != 0) Id(ports.Heat);
        var commands = new HashSet<ulong>();
        void Channel(ulong channel) { if (channel == 0 || channel >= 0x8000000000000000UL || !commands.Add(channel)) throw new ArgumentException("Actuator commands must be distinct nonzero input IDs."); }
        Channel(ports.OddCommand); Channel(ports.EvenCommand);
        var bindings = gearIds.ToArray();
        foreach (var gear in bindings) { Id(gear.Hub); Id(gear.Mesh); Id(gear.Selector); Channel(gear.Command); }
        double a = -_parameters.FinalDriveA * vehicleSpeedRadiansPerSecond, b = -_parameters.FinalDriveB * vehicleSpeedRadiansPerSecond,
            reverse = -_parameters.ReverseFinalDrive * vehicleSpeedRadiansPerSecond;
        double odd = selectedOdd == 0 ? 0 : EffectiveReduction(selectedOdd) * vehicleSpeedRadiansPerSecond;
        double even = selectedEven == 0 ? 0 : EffectiveReduction(selectedEven) * vehicleSpeedRadiansPerSecond;
        var nodes = new List<NodeDefinition>
        {
            NodeDefinition.Rotor(ports.OddShaft, _parameters.OddShaftInertia.Value, odd),
            NodeDefinition.Rotor(ports.EvenShaft, _parameters.EvenShaftInertia.Value, even),
            NodeDefinition.Rotor(ports.OutputA, _parameters.OutputAInertia.Value, a),
            NodeDefinition.Rotor(ports.OutputB, _parameters.OutputBInertia.Value, b),
            NodeDefinition.Rotor(ports.ReverseOutput, _parameters.ReverseOutputInertia.Value, reverse),
            NodeDefinition.Rotor(ports.ReverseIdler, _parameters.ReverseIdlerInertia.Value, -even)
        };
        var components = new List<ComponentDefinition>
        {
            ComponentDefinition.Clutch(ports.OddClutch, ports.Engine, ports.OddShaft, _parameters.DriveStaticCapacity.Value,
                _parameters.DriveSlidingCapacity.Value, ports.OddCommand, oddEngagement, heat: ports.Heat),
            ComponentDefinition.Clutch(ports.EvenClutch, ports.Engine, ports.EvenShaft, _parameters.DriveStaticCapacity.Value,
                _parameters.DriveSlidingCapacity.Value, ports.EvenCommand, evenEngagement, heat: ports.Heat),
            ComponentDefinition.IdealGear(ports.FinalA, ports.OutputA, ports.Vehicle, -_parameters.FinalDriveA),
            ComponentDefinition.IdealGear(ports.FinalB, ports.OutputB, ports.Vehicle, -_parameters.FinalDriveB),
            ComponentDefinition.IdealGear(ports.FinalReverse, ports.ReverseOutput, ports.Vehicle, -_parameters.ReverseFinalDrive),
            ComponentDefinition.IdealGear(ports.ReverseFirstMesh, ports.EvenShaft, ports.ReverseIdler, -1)
        };
        for (int i = 0; i < 8; ++i)
        {
            int number = i == 7 ? -1 : i + 1; bool oddPath = number > 0 && number % 2 == 1;
            uint input = oddPath ? ports.OddShaft : ports.EvenShaft, output = number == -1 ? ports.ReverseOutput : number <= 4 ? ports.OutputA : ports.OutputB;
            double reduction = number == -1 ? _reverse : _ratios[i];
            double speed = number == -1 ? even / reduction : -(oddPath ? odd : even) / reduction;
            nodes.Add(NodeDefinition.Rotor(bindings[i].Hub, _parameters.HubInertia.Value, speed));
            components.Add(ComponentDefinition.IdealGear(bindings[i].Mesh, number == -1 ? ports.ReverseIdler : input, bindings[i].Hub, -reduction));
            bool selected = oddPath ? selectedOdd == number : selectedEven == number;
            components.Add(ComponentDefinition.Clutch(bindings[i].Selector, bindings[i].Hub, output, _parameters.SelectorStaticCapacity.Value,
                _parameters.SelectorSlidingCapacity.Value, bindings[i].Command, selected ? 1 : 0, heat: ports.Heat));
        }
        return new(nodes.ToArray(), components.ToArray(), bindings, ports);
    }
}
