using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Power.Core;
using Power.Assets;

namespace Power.Experiments;

public sealed record InputEvent(ulong TimeNanoseconds, Scalar[] Values);
public sealed record OutputCheck(uint ObjectId, Field Field, double? Min, double? Max, double? AbsMax);
public sealed record ModelDocument(ModelDefinition Model, ulong DurationNanoseconds, ulong SampleEveryNanoseconds,
    InputEvent[] Events, OutputCheck[] Checks, string SourceSha256)
{
    public PowerAsset ToAsset(string name)
    {
        ExperimentRunner.Validate(this);
        return PowerAsset.Create(Model, name, SourceSha256, DurationNanoseconds, SampleEveryNanoseconds,
            Events.SelectMany(e => e.Values.Select(v => new ScheduledInput(e.TimeNanoseconds, v.Channel, v.Value))),
            Checks.Select(c => new AssetCheck(c.ObjectId, c.Field, c.Min, c.Max, c.AbsMax)));
    }

    private static readonly Dictionary<string, Unit> Units = new(StringComparer.Ordinal)
    {
        ["none"] = Unit.None, ["kg_m2"] = Unit.KilogramMeterSquared, ["rad"] = Unit.Radian,
        ["rad_s"] = Unit.RadianPerSecond, ["nm"] = Unit.NewtonMeter, ["nm_rad"] = Unit.NewtonMeterPerRadian,
        ["nm_s_rad"] = Unit.NewtonMeterSecondPerRadian, ["k"] = Unit.Kelvin, ["j_k"] = Unit.JoulePerKelvin,
        ["w_k"] = Unit.WattPerKelvin, ["ohm"] = Unit.Ohm, ["h"] = Unit.Henry, ["nm_a"] = Unit.NewtonMeterPerAmpere,
        ["a"] = Unit.Ampere, ["v"] = Unit.Volt, ["j"] = Unit.Joule, ["rpm"] = Unit.Rpm, ["deg"] = Unit.Degree
    };
    private static readonly Dictionary<string, Field> Fields = new(StringComparer.Ordinal)
    {
        ["angle"] = Field.Angle, ["speed"] = Field.Speed, ["temperature"] = Field.Temperature,
        ["current"] = Field.Current, ["twist"] = Field.Twist, ["torque"] = Field.Torque,
        ["source_work"] = Field.SourceWork, ["heat_rejected"] = Field.HeatRejected,
        ["stored_energy_change"] = Field.StoredEnergyChange, ["energy_residual"] = Field.EnergyResidual
    };

    public static ModelDocument Load(string path)
    {
        using var stream = File.OpenRead(path);
        if (stream.Length > 1_048_576) throw new ArgumentException("Model document exceeds 1 MiB.");
        using var reader = new StreamReader(stream);
        return Parse(reader.ReadToEnd());
    }

    private static void Object(JsonElement e, string[] required, params string[] optional)
    {
        if (e.ValueKind != JsonValueKind.Object) throw new ArgumentException("Expected a JSON object.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var p in e.EnumerateObject())
            if (!seen.Add(p.Name) || (!required.Contains(p.Name) && !optional.Contains(p.Name)))
                throw new ArgumentException($"Duplicate or unknown JSON field '{p.Name}'.");
        foreach (string field in required)
            if (!seen.Contains(field)) throw new ArgumentException($"Missing JSON field '{field}'.");
    }
    private static string Text(JsonElement e) => e.ValueKind == JsonValueKind.String ? e.GetString()! :
        throw new ArgumentException("Expected a string.");
    private static ulong Integer(JsonElement e, ulong maximum = ulong.MaxValue, ulong minimum = 0) =>
        e.ValueKind == JsonValueKind.Number && e.TryGetUInt64(out ulong value) && value >= minimum && value <= maximum
            ? value : throw new ArgumentException($"Expected an integer in [{minimum}, {maximum}].");
    private static double Number(JsonElement e) => e.ValueKind == JsonValueKind.Number && e.TryGetDouble(out double value) && double.IsFinite(value)
        ? value : throw new ArgumentException("Expected a finite number.");
    private static JsonElement[] Array(JsonElement e, int maximum, int minimum = 0) =>
        e.ValueKind == JsonValueKind.Array && e.GetArrayLength() >= minimum && e.GetArrayLength() <= maximum
            ? e.EnumerateArray().ToArray() : throw new ArgumentException($"Expected {minimum}..{maximum} array elements.");
    private static Quantity Quantity(JsonElement e)
    {
        Object(e, ["value", "unit"]);
        if (!Units.TryGetValue(Text(e.GetProperty("unit")), out Unit unit)) throw new ArgumentException("Unknown unit.");
        return new(Number(e.GetProperty("value")), unit);
    }
    private static uint Id(JsonElement e, string field, bool optional = false) =>
        e.TryGetProperty(field, out var p) ? (uint)Integer(p, uint.MaxValue, optional ? 0UL : 1UL) : optional ? 0U :
            throw new ArgumentException($"Missing {field}.");

    public static ModelDocument Parse(string source)
    {
        if (Encoding.UTF8.GetByteCount(source) > 1_048_576) throw new ArgumentException("Model document exceeds 1 MiB.");
        using var json = JsonDocument.Parse(source, new JsonDocumentOptions { MaxDepth = 32 });
        JsonElement root = json.RootElement;
        Object(root, ["schema", "step_ns", "nodes", "components", "experiment"], "description");
        if (Text(root.GetProperty("schema")) != "power.model.v1") throw new ArgumentException("Unsupported schema.");
        var nodes = Array(root.GetProperty("nodes"), CompiledModel.MaxNodes, 1).Select(n =>
        {
            Object(n, ["id", "domain", "storage", "initial"], "position");
            var domain = Text(n.GetProperty("domain")) switch
            {
                "rotational" => Domain.Rotational, "thermal" => Domain.Thermal,
                _ => throw new ArgumentException("Unknown node domain.")
            };
            if (domain == Domain.Rotational && !n.TryGetProperty("position", out _))
                throw new ArgumentException("Rotational position must be explicit.");
            return new NodeDefinition(Id(n, "id"), domain, Quantity(n.GetProperty("storage")), Quantity(n.GetProperty("initial")),
                n.TryGetProperty("position", out var p) ? Quantity(p) : default);
        }).ToArray();
        var components = Array(root.GetProperty("components"), CompiledModel.MaxComponents).Select(c =>
        {
            Object(c, ["id", "kind", "node_a"], "node_b", "heat_node", "input_channel", "initial_input", "parameters");
            var kind = Text(c.GetProperty("kind")) switch
            {
                "shaft" => ComponentKind.Shaft, "dc_motor" => ComponentKind.DcMotor,
                "torque_source" => ComponentKind.TorqueSource, "thermal_link" => ComponentKind.ThermalLink,
                _ => throw new ArgumentException("Unknown component kind.")
            };
            var result = new ComponentDefinition
            {
                Id = Id(c, "id"), Kind = kind, NodeA = Id(c, "node_a"), NodeB = Id(c, "node_b", true),
                HeatNode = Id(c, "heat_node", true),
                InputChannel = c.TryGetProperty("input_channel", out var channel) ? Integer(channel, long.MaxValue) : 0,
                InitialInput = c.TryGetProperty("initial_input", out var initial) ? Quantity(initial) : default
            };
            if (kind == ComponentKind.TorqueSource)
            {
                if (c.TryGetProperty("parameters", out var empty)) Object(empty, []);
                return result;
            }
            if (!c.TryGetProperty("parameters", out var parameters)) throw new ArgumentException("Missing component parameters.");
            if (kind == ComponentKind.Shaft)
            {
                Object(parameters, ["stiffness", "damping", "rest_angle", "ratio"]);
                return result with { Stiffness = Quantity(parameters.GetProperty("stiffness")), Damping = Quantity(parameters.GetProperty("damping")),
                    RestAngle = Quantity(parameters.GetProperty("rest_angle")), Ratio = Number(parameters.GetProperty("ratio")) };
            }
            if (kind == ComponentKind.DcMotor)
            {
                Object(parameters, ["resistance", "inductance", "coupling", "initial_current"]);
                return result with { Resistance = Quantity(parameters.GetProperty("resistance")), Inductance = Quantity(parameters.GetProperty("inductance")),
                    Coupling = Quantity(parameters.GetProperty("coupling")), InitialCurrent = Quantity(parameters.GetProperty("initial_current")) };
            }
            Object(parameters, result.NodeB == 0 ? ["conductance", "ambient_temperature"] : ["conductance"]);
            return result with { Conductance = Quantity(parameters.GetProperty("conductance")),
                AmbientTemperature = result.NodeB == 0 ? Quantity(parameters.GetProperty("ambient_temperature")) : default };
        }).ToArray();
        var model = new ModelDefinition { StepNanoseconds = Integer(root.GetProperty("step_ns"), 1_000_000_000, 1), Nodes = nodes, Components = components };
        JsonElement exp = root.GetProperty("experiment");
        Object(exp, ["duration_ns", "sample_every_ns"], "events", "checks");
        ulong duration = Integer(exp.GetProperty("duration_ns"), 3_600_000_000_000, 1);
        ulong sample = Integer(exp.GetProperty("sample_every_ns"), duration, 1);
        InputEvent[] events = exp.TryGetProperty("events", out var es) ? Array(es, 10000).Select(e =>
        {
            Object(e, ["time_ns", "values"]);
            return new InputEvent(Integer(e.GetProperty("time_ns"), duration - 1), Array(e.GetProperty("values"), 64, 1).Select(v =>
            {
                Object(v, ["channel", "value"]);
                return new Scalar(Integer(v.GetProperty("channel"), long.MaxValue, 1), Number(v.GetProperty("value")));
            }).ToArray());
        }).ToArray() : [];
        OutputCheck[] checks = exp.TryGetProperty("checks", out var cs) ? Array(cs, 256).Select(c =>
        {
            Object(c, ["object_id", "field"], "min", "max", "abs_max");
            if (!Fields.TryGetValue(Text(c.GetProperty("field")), out var field)) throw new ArgumentException("Unknown check field.");
            double? Get(string name) => c.TryGetProperty(name, out var bound) ? Number(bound) : null;
            return new OutputCheck((uint)Integer(c.GetProperty("object_id"), uint.MaxValue), field, Get("min"), Get("max"), Get("abs_max"));
        }).ToArray() : [];
        return new(model, duration, sample, events, checks, Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source))));
    }
}
