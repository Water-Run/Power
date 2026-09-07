using System.Collections.ObjectModel;

namespace Power.Core;

internal readonly record struct GraphNode(uint Id, Domain Domain, int Index, double Storage, double Initial, double Position);
internal readonly record struct GraphComponent(uint Id, ComponentKind Kind, int A, int B, int Heat, int Index,
    ulong InputChannel, double InitialInput, double P0, double P1, double P2, double P3);
internal readonly record struct OutputBinding(uint ObjectId, Field Field, int Index);

/// <summary>Immutable, validated topology and solver factors shared by independent simulations.</summary>
public sealed class CompiledModel
{
    public const int MaxNodes = 32, MaxComponents = 64, MaxStates = 64;
    public ulong StepNanoseconds { get; }
    public ulong Fingerprint { get; }
    public int NodeCount => Nodes.Length;
    public int ComponentCount => Components.Length;
    public int StateCount => DynamicCount + ThermalCount;
    public int OutputCount => Outputs.Length;
    public string Fidelity => "linear_lumped";
    public string Calibration => "unverified";
    public ReadOnlyCollection<ChannelInfo> Channels { get; }
    internal GraphNode[] Nodes { get; }
    internal GraphComponent[] Components { get; }
    internal OutputBinding[] Outputs { get; }
    internal Dictionary<ulong, int> InputIndices { get; } = [];
    internal double Dt { get; }
    internal int DynamicCount { get; }
    internal int ThermalCount { get; }
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

    private static double Convert(Quantity q, Unit expected, uint id, string field)
    {
        double scale = 1;
        if (q.Unit != expected)
        {
            if (q.Unit == Unit.Rpm && expected == Unit.RadianPerSecond) scale = 0.10471975511965977462;
            else if (q.Unit == Unit.Degree && expected == Unit.Radian) scale = 0.01745329251994329577;
            else throw new ModelCompileException(DiagnosticCode.Unit, id, field, $"Expected {expected}, received {q.Unit}.");
        }
        double value = q.Value * scale;
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
        int dynamics = 0, thermal = 0;
        for (int i = 0; i < ns.Length; ++i)
        {
            var n = ns[i];
            Require(n.Id != 0 && ids.Add(n.Id), DiagnosticCode.Id, n.Id, "id", "IDs must be nonzero and globally unique.");
            Require(n.Domain is Domain.Rotational or Domain.Thermal, DiagnosticCode.Schema, n.Id, "domain", "Unknown domain.");
            bool rotor = n.Domain == Domain.Rotational;
            double storage = Convert(n.Storage, rotor ? Unit.KilogramMeterSquared : Unit.JoulePerKelvin, n.Id, "storage");
            double initial = Convert(n.Initial, rotor ? Unit.RadianPerSecond : Unit.Kelvin, n.Id, "initial");
            double position = Convert(n.Position, rotor ? Unit.Radian : Unit.None, n.Id, "position");
            Require(storage > 0, DiagnosticCode.Range, n.Id, "storage", "Storage must be positive.");
            Require(rotor || initial > 0, DiagnosticCode.Range, n.Id, "initial", "Absolute temperature must be positive.");
            Nodes[i] = new(n.Id, n.Domain, rotor ? dynamics : thermal, storage, initial, position);
            if (rotor) dynamics += 2; else ++thermal;
        }
        int FindNode(uint id) => Array.FindIndex(Nodes, n => n.Id == id);
        Components = new GraphComponent[cs.Length];
        for (int i = 0; i < cs.Length; ++i)
        {
            var c = cs[i];
            Require(c.Id != 0 && ids.Add(c.Id), DiagnosticCode.Id, c.Id, "id", "IDs must be nonzero and globally unique.");
            Require(c.Kind is >= ComponentKind.Shaft and <= ComponentKind.ThermalLink,
                DiagnosticCode.Schema, c.Id, "kind", "Unknown component kind.");
            bool pair = c.Kind is ComponentKind.Shaft or ComponentKind.ThermalLink;
            bool input = c.Kind is ComponentKind.DcMotor or ComponentKind.TorqueSource;
            Domain domain = c.Kind == ComponentKind.ThermalLink ? Domain.Thermal : Domain.Rotational;
            int a = FindNode(c.NodeA), b = FindNode(c.NodeB), heat = FindNode(c.HeatNode);
            Require(a >= 0 && Nodes[a].Domain == domain, DiagnosticCode.Connection, c.Id, "node_a", "Missing node or wrong domain.");
            Require(c.NodeB == 0 || (pair && b >= 0 && b != a && Nodes[b].Domain == domain),
                DiagnosticCode.Connection, c.Id, "node_b", "Expected a distinct node of the matching domain.");
            Require(c.HeatNode == 0 || (c.Kind is ComponentKind.Shaft or ComponentKind.DcMotor &&
                    heat >= 0 && Nodes[heat].Domain == Domain.Thermal),
                DiagnosticCode.Connection, c.Id, "heat_node", "Loss sinks must be thermal nodes.");
            Require(input ? c.InputChannel > 0 && c.InputChannel < 0x8000000000000000UL && !InputIndices.ContainsKey(c.InputChannel)
                          : c.InputChannel == 0, DiagnosticCode.Channel, c.Id, "input_channel", "Invalid or duplicate input channel.");
            double initial = Convert(c.InitialInput, input ? (c.Kind == ComponentKind.DcMotor ? Unit.Volt : Unit.NewtonMeter) : Unit.None,
                c.Id, "initial_input");
            double p0 = 0, p1 = 0, p2 = 0, p3 = 0;
            int state = -1;
            switch (c.Kind)
            {
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
            }
            if (input) InputIndices.Add(c.InputChannel, i);
            Components[i] = new(c.Id, c.Kind, a, b, heat, state, c.InputChannel, initial, p0, p1, p2, p3);
        }
        Require(dynamics + thermal <= MaxStates, DiagnosticCode.Capacity, 0, "states", "State capacity exceeded.");
        DynamicCount = dynamics; ThermalCount = thermal;
        double[,] matrix = new double[dynamics, dynamics], heatMatrix = new double[thermal, thermal];
        ConstantForce = new double[dynamics]; AmbientForce = new double[thermal];
        var channels = new List<ChannelInfo>();
        var outputs = new List<OutputBinding>();
        void Output(uint id, Field field, int index, Unit unit, string quantity)
        {
            channels.Add(new(Core.Channels.Output(id, field), id, false, unit, quantity));
            outputs.Add(new(id, field, index));
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
                Output(c.Id, Field.Twist, i, Unit.Radian, "twist");
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "reaction_torque_at_a");
            }
            else if (c.Kind == ComponentKind.DcMotor)
            {
                matrix[a + 1, c.Index] += c.P2 / na.Storage;
                matrix[c.Index, a + 1] -= c.P2 / c.P1;
                matrix[c.Index, c.Index] -= c.P0 / c.P1;
                channels.Add(new(c.InputChannel, c.Id, true, Unit.Volt, "voltage"));
                Output(c.Id, Field.Current, i, Unit.Ampere, "current");
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "motor_torque");
            }
            else if (c.Kind == ComponentKind.TorqueSource)
                channels.Add(new(c.InputChannel, c.Id, true, Unit.NewtonMeter, "torque"));
            else
            {
                double gh = Dt * c.P0;
                heatMatrix[a, a] += gh;
                if (b < 0) AmbientForce[a] += gh * c.P1;
                else { heatMatrix[a, b] -= gh; heatMatrix[b, a] -= gh; heatMatrix[b, b] += gh; }
            }
        }
        for (int row = 0; row < dynamics; ++row)
            for (int col = 0; col < dynamics; ++col)
                matrix[row, col] = (row == col ? 1 : 0) - 0.5 * Dt * matrix[row, col];
        foreach (Field field in new[] { Field.SourceWork, Field.HeatRejected, Field.StoredEnergyChange, Field.EnergyResidual })
            Output(0, field, -1, Unit.Joule, field.ToString());
        Outputs = outputs.ToArray();
        Channels = Array.AsReadOnly(channels.ToArray());
        Require(ConstantForce.All(Numeric.Finite) && AmbientForce.All(Numeric.Finite), DiagnosticCode.Solver,
            0, "forcing", "Compiled force overflows binary64.");
        Dynamics = new(matrix); Thermal = new(heatMatrix);
        Fingerprint = ComputeFingerprint();
        _ = CreateSimulation(); // Validate initial energy and derived observables before publishing.
    }

    private ulong ComputeFingerprint()
    {
        ulong h = Numeric.Hash(14695981039346656037UL, 1UL);
        h = Numeric.Hash(h, 2UL); // Managed solver version, distinct from the native prototype.
        h = Numeric.Hash(h, StepNanoseconds);
        h = Numeric.Hash(h, (ulong)NodeCount); h = Numeric.Hash(h, (ulong)ComponentCount);
        foreach (var n in Nodes)
        {
            h = Numeric.Hash(h, (ulong)n.Id); h = Numeric.Hash(h, (ulong)n.Domain);
            h = Numeric.Hash(h, n.Storage); h = Numeric.Hash(h, n.Initial); h = Numeric.Hash(h, n.Position);
        }
        foreach (var c in Components)
        {
            h = Numeric.Hash(h, (ulong)c.Id); h = Numeric.Hash(h, (ulong)c.Kind);
            h = Numeric.Hash(h, unchecked((ulong)c.A)); h = Numeric.Hash(h, unchecked((ulong)c.B));
            h = Numeric.Hash(h, unchecked((ulong)c.Heat)); h = Numeric.Hash(h, c.InputChannel);
            h = Numeric.Hash(h, c.InitialInput); h = Numeric.Hash(h, c.P0); h = Numeric.Hash(h, c.P1);
            h = Numeric.Hash(h, c.P2); h = Numeric.Hash(h, c.P3);
        }
        return h;
    }
}
