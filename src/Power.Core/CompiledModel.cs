// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Collections.ObjectModel;

namespace Power.Core;

internal readonly record struct GraphNode(uint Id, Domain Domain, int Index, double Storage, double Initial, double Position);
internal readonly record struct GraphComponent(uint Id, ComponentKind Kind, int A, int B, int Heat, int Index,
    ulong InputChannel, double InitialInput, double P0, double P1, double P2, double P3);
internal readonly record struct OutputBinding(uint ObjectId, Field Field, int Index, bool IsComponent = false);

/// <summary>Immutable, validated topology and solver factors shared by independent simulations.</summary>
public sealed class CompiledModel
{
    public const int MaxNodes = 32, MaxComponents = 64, MaxStates = 64;
    public ulong StepNanoseconds { get; }
    public ulong Fingerprint { get; }
    public int NodeCount => Nodes.Length;
    public int ComponentCount => Components.Length;
    public int StateCount => DynamicCount + ThermalCount + 2 * GasCount;
    public int OutputCount => Outputs.Length;
    public string Fidelity => GasCount > 0 ? "finite_volume_gas_exchange"
        : Cylinders.Any(c => c is not null) ? "sealed_adiabatic_gas" : "linear_lumped";
    public string Calibration => "unverified";
    public ReadOnlyCollection<ChannelInfo> Channels { get; }
    internal GraphNode[] Nodes { get; }
    internal GraphComponent[] Components { get; }
    internal CylinderPhysics?[] Cylinders { get; }
    internal CylinderCoupling? CylinderCoupling { get; }
    internal GasNetwork? Gas { get; }
    internal OutputBinding[] Outputs { get; }
    internal Dictionary<ulong, int> InputIndices { get; } = [];
    internal double[] InputMinimum { get; }
    internal double[] InputMaximum { get; }
    internal IdealGas?[] NodeGases { get; }
    internal double Dt { get; }
    internal int DynamicCount { get; }
    internal int ThermalCount { get; }
    internal int GasCount { get; }
    internal Factorization Dynamics { get; }
    internal Factorization Thermal { get; }
    internal double[] ConstantForce { get; }
    internal double[] AmbientForce { get; }

    public static CompiledModel Compile(ModelDefinition definition) => new(definition);
    public static bool TryCompile(ModelDefinition definition, out CompiledModel? model, out ModelDiagnostic? diagnostic)
    {
        try { model = Compile(definition); diagnostic = null; return true; }
        catch (ModelCompileException error)
        {
            model = null;
            diagnostic = new(error.Code, error.ObjectId, error.Field, error.Message);
            return false;
        }
        catch (ArgumentNullException error)
        {
            model = null;
            diagnostic = new(DiagnosticCode.Schema, 0, "model", error.Message);
            return false;
        }
    }
    public Simulation CreateSimulation() => new(this);

    private static void Require(bool condition, DiagnosticCode code, uint id, string field, string message)
    {
        if (!condition) throw new ModelCompileException(code, id, field, message);
    }

    private static bool SameGas(IdealGas a, IdealGas b) =>
        a.GasConstantJoulePerKilogramKelvin == b.GasConstantJoulePerKilogramKelvin && a.Gamma == b.Gamma;

    internal static double Convert(Quantity q, Unit expected, uint id, string field)
    {
        double scale = 1, divisor = 1;
        if (q.Unit != expected)
        {
            if (q.Unit == Unit.Rpm && expected == Unit.RadianPerSecond) scale = 0.10471975511965977462;
            else if (q.Unit == Unit.Degree && expected == Unit.Radian) scale = 0.01745329251994329577;
            else if (q.Unit == Unit.Millimeter && expected == Unit.Meter) divisor = 1000;
            else if (q.Unit == Unit.SquareMillimeter && expected == Unit.SquareMeter) divisor = 1_000_000;
            else if (q.Unit == Unit.Liter && expected == Unit.CubicMeter) divisor = 1000;
            else if (q.Unit == Unit.Bar && expected == Unit.Pascal) scale = 100_000;
            else throw new ModelCompileException(DiagnosticCode.Unit, id, field, $"Expected {expected}, received {q.Unit}.");
        }
        double value = q.Value * scale / divisor;
        Require(Numeric.Finite(value) && (expected != Unit.None || value == 0), DiagnosticCode.Range,
            id, field, "Value must be finite; unused quantities must be zero/None.");
        return value == 0 ? 0 : value;
    }

    private CompiledModel(ModelDefinition definition)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        Require(definition.SchemaVersion == 1, DiagnosticCode.Schema, 0, "schema", "Only model schema 1 is supported.");
        Require(definition.Nodes is { Length: > 0 and <= MaxNodes } &&
                definition.Components is { Length: <= MaxComponents }, DiagnosticCode.Capacity,
            0, "counts", "Expected 1..32 nodes and 0..64 components.");
        Require(definition.StepNanoseconds is > 0 and <= 1_000_000_000, DiagnosticCode.Range,
            0, "step_ns", "Fixed step must be 1 ns .. 1 s.");
        StepNanoseconds = definition.StepNanoseconds;
        Dt = StepNanoseconds * 1e-9;
        var ids = new HashSet<uint>();
        var ns = (NodeDefinition[])definition.Nodes!.Clone();
        var cs = (ComponentDefinition[])definition.Components!.Clone();
        Require(ns.All(n => n is not null) && cs.All(c => c is not null), DiagnosticCode.Schema,
            0, "descriptors", "Null descriptors are not allowed.");
        Array.Sort(ns, (a, b) => a.Id.CompareTo(b.Id));
        Array.Sort(cs, (a, b) => a.Id.CompareTo(b.Id));
        Nodes = new GraphNode[ns.Length];
        NodeGases = new IdealGas?[ns.Length];
        int dynamics = 0, thermal = 0, gas = 0;
        for (int i = 0; i < ns.Length; ++i)
        {
            var n = ns[i];
            Require(n.Id != 0 && ids.Add(n.Id), DiagnosticCode.Id, n.Id, "id", "IDs must be nonzero and globally unique.");
            Require(n.Domain is Domain.Rotational or Domain.Thermal or Domain.Gas,
                DiagnosticCode.Schema, n.Id, "domain", "Unknown domain.");
            Require((n.Domain == Domain.Gas) == (n.Gas is not null), DiagnosticCode.Schema, n.Id, "gas",
                "Gas composition is required only for gas volumes.");
            bool rotor = n.Domain == Domain.Rotational, volume = n.Domain == Domain.Gas;
            Unit storageUnit = rotor ? Unit.KilogramMeterSquared : volume ? Unit.CubicMeter : Unit.JoulePerKelvin;
            Unit initialUnit = rotor ? Unit.RadianPerSecond : Unit.Kelvin;
            Unit positionUnit = rotor ? Unit.Radian : volume ? Unit.Pascal : Unit.None;
            double storage = Convert(n.Storage, storageUnit, n.Id, "storage");
            double initial = Convert(n.Initial, initialUnit, n.Id, "initial");
            double position = Convert(n.Position, positionUnit, n.Id, "position");
            Require(storage > 0, DiagnosticCode.Range, n.Id, "storage", "Storage must be positive.");
            Require(rotor || initial > 0, DiagnosticCode.Range, n.Id, "initial", "Absolute temperature must be positive.");
            if (volume)
            {
                Require(position > 0, DiagnosticCode.Range, n.Id, "position", "Absolute pressure must be positive.");
                double constant = Convert(n.Gas!.GasConstant, Unit.JoulePerKilogramKelvin, n.Id, "gas.gas_constant");
                try { NodeGases[i] = new(constant, n.Gas.Gamma); }
                catch (ArgumentException error) { throw new ModelCompileException(DiagnosticCode.Range, n.Id, "gas", error.Message); }
                double mass = position * storage / (constant * initial);
                Require(Numeric.Finite(mass) && mass > 0 && Numeric.Finite(NodeGases[i]!.InternalEnergy(mass, initial)),
                    DiagnosticCode.Range, n.Id, "gas", "Initial gas mass or internal energy exceeds binary64.");
            }
            Nodes[i] = new(n.Id, n.Domain, rotor ? dynamics : volume ? gas : thermal, storage, initial, position);
            if (rotor) dynamics += 2; else if (volume) ++gas; else ++thermal;
        }
        GasCount = gas;
        int FindNode(uint id) => Array.FindIndex(Nodes, n => n.Id == id);
        Components = new GraphComponent[cs.Length];
        Cylinders = new CylinderPhysics?[cs.Length];
        InputMinimum = new double[cs.Length]; InputMaximum = new double[cs.Length];
        Array.Fill(InputMinimum, double.NegativeInfinity); Array.Fill(InputMaximum, double.PositiveInfinity);
        for (int i = 0; i < cs.Length; ++i)
        {
            var c = cs[i];
            Require(c.Id != 0 && ids.Add(c.Id), DiagnosticCode.Id, c.Id, "id", "IDs must be nonzero and globally unique.");
            Require(c.Kind is >= ComponentKind.Shaft and <= ComponentKind.GasHeatLink,
                DiagnosticCode.Schema, c.Id, "kind", "Unknown component kind.");
            Require((c.Kind == ComponentKind.SealedCylinder) == (c.Cylinder is not null),
                DiagnosticCode.Schema, c.Id, "cylinder", "Cylinder parameters are required only for sealed cylinders.");
            bool orifice = c.Kind == ComponentKind.GasOrifice, wall = c.Kind == ComponentKind.GasHeatLink;
            bool pair = c.Kind is ComponentKind.Shaft or ComponentKind.ThermalLink || orifice;
            bool input = c.Kind is ComponentKind.DcMotor or ComponentKind.TorqueSource;
            Domain domain = c.Kind == ComponentKind.ThermalLink ? Domain.Thermal
                : orifice || wall ? Domain.Gas : Domain.Rotational;
            int a = FindNode(c.NodeA), b = FindNode(c.NodeB), heat = FindNode(c.HeatNode);
            Require(a >= 0 && Nodes[a].Domain == domain, DiagnosticCode.Connection, c.Id, "node_a", "Missing node or wrong domain.");
            Require(wall ? b >= 0 && Nodes[b].Domain == Domain.Thermal
                         : c.NodeB == 0 || (pair && b >= 0 && b != a && Nodes[b].Domain == domain),
                DiagnosticCode.Connection, c.Id, "node_b",
                wall ? "A gas heat link requires a thermal node." : "Expected a distinct node of the matching domain.");
            Require(c.HeatNode == 0 || (c.Kind is ComponentKind.Shaft or ComponentKind.DcMotor &&
                    heat >= 0 && Nodes[heat].Domain == Domain.Thermal),
                DiagnosticCode.Connection, c.Id, "heat_node", "Loss sinks must be thermal nodes.");
            bool channelled = input || (orifice && c.InputChannel != 0);
            Require(channelled ? c.InputChannel > 0 && c.InputChannel < 0x8000000000000000UL && !InputIndices.ContainsKey(c.InputChannel)
                               : c.InputChannel == 0, DiagnosticCode.Channel, c.Id, "input_channel", "Invalid or duplicate input channel.");
            double initial = Convert(c.InitialInput, input ? (c.Kind == ComponentKind.DcMotor ? Unit.Volt : Unit.NewtonMeter)
                : orifice ? Unit.Fraction : Unit.None, c.Id, "initial_input");
            Require(c.DischargeCoefficient == 1 || orifice, DiagnosticCode.Schema, c.Id, "discharge_coefficient",
                "A discharge coefficient applies only to a gas orifice.");
            double p0 = 0, p1 = 0, p2 = 0, p3 = 0;
            int state = -1;
            switch (c.Kind)
            {
                case ComponentKind.SealedCylinder:
                    Cylinders[i] = new(c.Cylinder!, c.Id, Nodes[a].Position);
                    break;
                case ComponentKind.Shaft:
                    p0 = Convert(c.Stiffness, Unit.NewtonMeterPerRadian, c.Id, "stiffness");
                    p1 = Convert(c.Damping, Unit.NewtonMeterSecondPerRadian, c.Id, "damping");
                    p2 = Convert(c.RestAngle, Unit.Radian, c.Id, "rest_angle");
                    p3 = c.Ratio;
                    Require(p0 >= 0 && p1 >= 0 && Numeric.Finite(p3) && p3 != 0 && (b >= 0 || p3 == 1),
                        DiagnosticCode.Range, c.Id, "shaft", "Invalid stiffness, damping, or ratio.");
                    break;
                case ComponentKind.DcMotor:
                    p0 = Convert(c.Resistance, Unit.Ohm, c.Id, "resistance");
                    p1 = Convert(c.Inductance, Unit.Henry, c.Id, "inductance");
                    p2 = Convert(c.Coupling, Unit.NewtonMeterPerAmpere, c.Id, "coupling");
                    p3 = Convert(c.InitialCurrent, Unit.Ampere, c.Id, "initial_current");
                    Require(p0 >= 0 && p1 > 0, DiagnosticCode.Range, c.Id, "motor", "R must be nonnegative and L positive.");
                    state = dynamics++;
                    break;
                case ComponentKind.ThermalLink:
                    p0 = Convert(c.Conductance, Unit.WattPerKelvin, c.Id, "conductance");
                    p1 = Convert(c.AmbientTemperature, b < 0 ? Unit.Kelvin : Unit.None, c.Id, "ambient_temperature");
                    Require(p0 >= 0 && (b >= 0 || p1 > 0), DiagnosticCode.Range, c.Id, "thermal", "Invalid conductance or absolute temperature.");
                    break;
                case ComponentKind.GasOrifice:
                    p0 = Convert(c.Area, Unit.SquareMeter, c.Id, "area");
                    p1 = c.DischargeCoefficient;
                    p2 = Convert(c.ReservoirPressure, b < 0 ? Unit.Pascal : Unit.None, c.Id, "reservoir_pressure");
                    p3 = Convert(c.AmbientTemperature, b < 0 ? Unit.Kelvin : Unit.None, c.Id, "reservoir_temperature");
                    Require(initial is >= 0 and <= 1, DiagnosticCode.Range, c.Id, "initial_input", "Opening must be a fraction in [0, 1].");
                    Require(b >= 0 || (p2 > 0 && p3 > 0), DiagnosticCode.Range, c.Id, "reservoir",
                        "A reservoir orifice needs a positive absolute pressure and temperature.");
                    try { _ = new Orifice(p0, p1); }
                    catch (ArgumentException error) { throw new ModelCompileException(DiagnosticCode.Range, c.Id, "orifice", error.Message); }
                    Require(b < 0 || SameGas(NodeGases[a]!, NodeGases[b]!), DiagnosticCode.Connection, c.Id, "node_b",
                        "Connected gas volumes must share one gas constant and gamma until species mixing exists.");
                    break;
                case ComponentKind.GasHeatLink:
                    p0 = Convert(c.Conductance, Unit.WattPerKelvin, c.Id, "conductance");
                    Require(p0 >= 0, DiagnosticCode.Range, c.Id, "conductance", "Conductance must be nonnegative.");
                    break;
            }
            if (orifice) { InputMinimum[i] = 0; InputMaximum[i] = 1; }
            if (channelled) InputIndices.Add(c.InputChannel, i);
            Components[i] = new(c.Id, c.Kind, a, b, heat, state, c.InputChannel, initial, p0, p1, p2, p3);
        }
        Require(dynamics + thermal + 2 * gas <= MaxStates, DiagnosticCode.Capacity, 0, "states", "State capacity exceeded.");
        DynamicCount = dynamics; ThermalCount = thermal;
        double[,] matrix = new double[dynamics, dynamics], heatMatrix = new double[thermal, thermal];
        ConstantForce = new double[dynamics]; AmbientForce = new double[thermal];
        var channels = new List<ChannelInfo>();
        var outputs = new List<OutputBinding>();
        void Output(uint id, Field field, int index, Unit unit, string quantity, bool component = false)
        {
            channels.Add(new(Core.Channels.Output(id, field), id, false, unit, quantity));
            outputs.Add(new(id, field, index, component));
        }
        for (int i = 0; i < Nodes.Length; ++i)
        {
            var n = Nodes[i];
            if (n.Domain == Domain.Rotational)
            {
                matrix[n.Index, n.Index + 1] = 1;
                Output(n.Id, Field.Angle, i, Unit.Radian, "angle");
                Output(n.Id, Field.Speed, i, Unit.RadianPerSecond, "speed");
            }
            else if (n.Domain == Domain.Gas)
            {
                Output(n.Id, Field.Pressure, i, Unit.Pascal, "gas_pressure");
                Output(n.Id, Field.Temperature, i, Unit.Kelvin, "gas_temperature");
                Output(n.Id, Field.Mass, i, Unit.Kilogram, "gas_mass");
                Output(n.Id, Field.InternalEnergy, i, Unit.Joule, "gas_internal_energy");
            }
            else
            {
                heatMatrix[n.Index, n.Index] = n.Storage;
                Output(n.Id, Field.Temperature, i, Unit.Kelvin, "temperature");
            }
        }
        for (int i = 0; i < Components.Length; ++i)
        {
            var c = Components[i]; var na = Nodes[c.A];
            int a = na.Index, b = c.B < 0 ? -1 : Nodes[c.B].Index;
            if (c.Kind == ComponentKind.Shaft)
            {
                int count = b < 0 ? 1 : 2;
                for (int row = 0; row < count; ++row)
                {
                    double directionRow = row == 0 ? 1 : -c.P3;
                    int velocity = (row == 0 ? a : b) + 1;
                    double inertia = Nodes[row == 0 ? c.A : c.B].Storage;
                    ConstantForce[velocity] += directionRow * c.P0 * c.P2 / inertia;
                    for (int col = 0; col < count; ++col)
                    {
                        double factor = directionRow * (col == 0 ? 1 : -c.P3) / inertia;
                        int column = col == 0 ? a : b;
                        matrix[velocity, column] -= factor * c.P0;
                        matrix[velocity, column + 1] -= factor * c.P1;
                    }
                }
                Output(c.Id, Field.Twist, i, Unit.Radian, "twist", true);
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "reaction_torque_at_a", true);
            }
            else if (c.Kind == ComponentKind.DcMotor)
            {
                matrix[a + 1, c.Index] += c.P2 / na.Storage;
                matrix[c.Index, a + 1] -= c.P2 / c.P1;
                matrix[c.Index, c.Index] -= c.P0 / c.P1;
                channels.Add(new(c.InputChannel, c.Id, true, Unit.Volt, "voltage"));
                Output(c.Id, Field.Current, i, Unit.Ampere, "current", true);
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "motor_torque", true);
            }
            else if (c.Kind == ComponentKind.TorqueSource)
                channels.Add(new(c.InputChannel, c.Id, true, Unit.NewtonMeter, "torque"));
            else if (c.Kind == ComponentKind.ThermalLink)
            {
                double gh = Dt * c.P0;
                heatMatrix[a, a] += gh;
                if (b < 0) AmbientForce[a] += gh * c.P1;
                else { heatMatrix[a, b] -= gh; heatMatrix[b, a] -= gh; heatMatrix[b, b] += gh; }
            }
            else if (c.Kind == ComponentKind.GasOrifice)
            {
                if (c.InputChannel != 0) channels.Add(new(c.InputChannel, c.Id, true, Unit.Fraction, "opening"));
                Output(c.Id, Field.MassFlow, i, Unit.KilogramPerSecond, "mass_flow_a_to_b", true);
            }
            else if (c.Kind == ComponentKind.GasHeatLink)
                Output(c.Id, Field.HeatFlow, i, Unit.Watt, "heat_flow_gas_to_wall", true);
            else if (c.Kind == ComponentKind.SealedCylinder)
            {
                Output(c.Id, Field.Pressure, i, Unit.Pascal, "cylinder_pressure", true);
                Output(c.Id, Field.Temperature, i, Unit.Kelvin, "gas_temperature", true);
                Output(c.Id, Field.Volume, i, Unit.CubicMeter, "cylinder_volume", true);
                Output(c.Id, Field.Mass, i, Unit.Kilogram, "trapped_gas_mass", true);
                Output(c.Id, Field.InternalEnergy, i, Unit.Joule, "gas_internal_energy", true);
                Output(c.Id, Field.PistonDisplacement, i, Unit.Meter, "piston_displacement_from_tdc", true);
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "gas_torque_at_crank", true);
            }
        }
        for (int row = 0; row < dynamics; ++row)
            for (int col = 0; col < dynamics; ++col)
                matrix[row, col] = (row == col ? 1 : 0) - 0.5 * Dt * matrix[row, col];
        foreach (Field field in new[] { Field.SourceWork, Field.HeatRejected, Field.StoredEnergyChange, Field.EnergyResidual })
            Output(0, field, -1, Unit.Joule, field.ToString());
        if (gas > 0)
        {
            Output(0, Field.ReservoirEnthalpy, -1, Unit.Joule, "reservoir_enthalpy");
            Output(0, Field.MassResidual, -1, Unit.Kilogram, "mass_residual");
        }
        Outputs = outputs.ToArray();
        Channels = Array.AsReadOnly(channels.ToArray());
        Require(ConstantForce.All(Numeric.Finite) && AmbientForce.All(Numeric.Finite), DiagnosticCode.Solver,
            0, "forcing", "Compiled force overflows binary64.");
        Dynamics = new(matrix); Thermal = new(heatMatrix);
        if (Cylinders.Any(c => c is not null)) CylinderCoupling = new(this);
        if (GasCount > 0) Gas = new(this);
        Fingerprint = ComputeFingerprint();
        _ = CreateSimulation(); // Validate initial energy and derived observables before publishing.
    }

    private ulong ComputeFingerprint()
    {
        ulong h = Numeric.Hash(14695981039346656037UL, 1UL);
        h = Numeric.Hash(h, CylinderCoupling is null ? 2UL : 3UL); // Preserve existing linear asset fingerprints.
        if (GasCount > 0) h = Numeric.Hash(h, 4UL);
        h = Numeric.Hash(h, StepNanoseconds);
        h = Numeric.Hash(h, (ulong)NodeCount); h = Numeric.Hash(h, (ulong)ComponentCount);
        foreach (var n in Nodes)
        {
            h = Numeric.Hash(h, (ulong)n.Id); h = Numeric.Hash(h, (ulong)n.Domain);
            h = Numeric.Hash(h, n.Storage); h = Numeric.Hash(h, n.Initial); h = Numeric.Hash(h, n.Position);
        }
        foreach (var composition in NodeGases)
            if (composition is not null)
            {
                h = Numeric.Hash(h, composition.GasConstantJoulePerKilogramKelvin);
                h = Numeric.Hash(h, composition.Gamma);
            }
        foreach (var c in Components)
        {
            h = Numeric.Hash(h, (ulong)c.Id); h = Numeric.Hash(h, (ulong)c.Kind);
            h = Numeric.Hash(h, unchecked((ulong)c.A)); h = Numeric.Hash(h, unchecked((ulong)c.B));
            h = Numeric.Hash(h, unchecked((ulong)c.Heat)); h = Numeric.Hash(h, c.InputChannel);
            h = Numeric.Hash(h, c.InitialInput); h = Numeric.Hash(h, c.P0); h = Numeric.Hash(h, c.P1);
            h = Numeric.Hash(h, c.P2); h = Numeric.Hash(h, c.P3);
        }
        foreach (var cylinder in Cylinders)
            if (cylinder is not null)
                foreach (double parameter in cylinder.Parameters) h = Numeric.Hash(h, parameter);
        return h;
    }
}
