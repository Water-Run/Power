using System.Collections.ObjectModel;
using Power.Core;

namespace Power.Assets;

public sealed record AssetCheck(uint ObjectId, Field Field, double? Min, double? Max, double? AbsMax);

/// <summary>Immutable model and experiment. The same validated data can be used by CLI, agents and Unity.</summary>
public sealed class PowerAsset
{
    public const int MaxScheduledInputs = 65_536, MaxChecks = 256;
    public string Name { get; }
    public string SourceSha256 { get; }
    public CompiledModel Model { get; }
    public ulong DurationNanoseconds { get; }
    public ulong SampleEveryNanoseconds { get; }
    public ReadOnlyCollection<NodeDefinition> Nodes { get; }
    public ReadOnlyCollection<ComponentDefinition> Components { get; }
    public ReadOnlyCollection<ScheduledInput> Inputs { get; }
    public ReadOnlyCollection<AssetCheck> Checks { get; }
    internal ScheduledInput[] InputArray { get; }

    private PowerAsset(ModelDefinition definition, string name, string sourceSha256, ulong durationNanoseconds,
        ulong sampleEveryNanoseconds, IEnumerable<ScheduledInput> inputs, IEnumerable<AssetCheck> checks)
    {
        if (name is null || name.Length == 0 || name.Length > 128 || name.Any(char.IsControl))
            throw new ArgumentException("Asset name must contain 1..128 printable characters.");
        if (sourceSha256 is null || sourceSha256.Length != 64 || !sourceSha256.All(c =>
            c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
            throw new ArgumentException("A 64-digit source SHA-256 is required.");
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        if (definition.Nodes is not { Length: > 0 and <= CompiledModel.MaxNodes } ||
            definition.Components is not { Length: <= CompiledModel.MaxComponents })
            throw new ArgumentException("Model counts exceed the supported asset limits.");
        var nodes = (NodeDefinition[])definition.Nodes.Clone();
        var components = (ComponentDefinition[])definition.Components.Clone();
        Model = CompiledModel.Compile(definition with { Nodes = nodes, Components = components });
        Array.Sort(nodes, (a, b) => a.Id.CompareTo(b.Id));
        Array.Sort(components, (a, b) => a.Id.CompareTo(b.Id));
        Nodes = Array.AsReadOnly(nodes); Components = Array.AsReadOnly(components);
        Name = name; SourceSha256 = sourceSha256.ToLowerInvariant();
        ulong step = Model.StepNanoseconds;
        if (durationNanoseconds == 0 || durationNanoseconds > 3_600_000_000_000 || sampleEveryNanoseconds == 0 ||
            sampleEveryNanoseconds > durationNanoseconds || durationNanoseconds % step != 0 || sampleEveryNanoseconds % step != 0 ||
            durationNanoseconds / step > 10_000_000 || durationNanoseconds / sampleEveryNanoseconds > 10000)
            throw new ArgumentException("Experiment times must align with fixed ticks and stay within the tick/sample budgets.");
        DurationNanoseconds = durationNanoseconds; SampleEveryNanoseconds = sampleEveryNanoseconds;
        InputArray = (inputs ?? throw new ArgumentNullException(nameof(inputs))).Take(MaxScheduledInputs + 1).ToArray();
        if (InputArray.Length > MaxScheduledInputs) throw new ArgumentException("Too many scheduled inputs.");
        var knownInputs = new HashSet<ulong>(Model.Channels.Where(c => c.IsInput).Select(c => c.Id));
        var knownOutputs = new HashSet<ulong>(Model.Channels.Where(c => !c.IsInput).Select(c => c.Id));
        var seen = new HashSet<ulong>();
        ulong previous = 0; int frames = 0;
        for (int i = 0; i < InputArray.Length; ++i)
        {
            var input = InputArray[i];
            if (input.TimeNanoseconds < previous || input.TimeNanoseconds >= durationNanoseconds ||
                input.TimeNanoseconds % step != 0 || !knownInputs.Contains(input.Channel) || !Finite(input.Value))
                throw new ArgumentException("Invalid time, order, channel or value in the input schedule.");
            if (i == 0 || input.TimeNanoseconds != previous) { ++frames; seen.Clear(); }
            if (!seen.Add(input.Channel)) throw new ArgumentException("Repeated input channel at the same time.");
            previous = input.TimeNanoseconds;
        }
        if (frames > 10000) throw new ArgumentException("Too many input event frames.");
        Inputs = Array.AsReadOnly(InputArray);
        var outputChecks = (checks ?? throw new ArgumentNullException(nameof(checks))).Take(MaxChecks + 1).ToArray();
        if (outputChecks.Length > MaxChecks) throw new ArgumentException("Too many output checks.");
        foreach (var c in outputChecks)
        {
            if (c is null || !Enum.IsDefined(typeof(Field), c.Field) || !knownOutputs.Contains(Channels.Output(c.ObjectId, c.Field)) ||
                (c.Min is null && c.Max is null && c.AbsMax is null) || c.Min > c.Max || c.AbsMax < 0 ||
                (c.Min.HasValue && !Finite(c.Min.Value)) || (c.Max.HasValue && !Finite(c.Max.Value)) ||
                (c.AbsMax.HasValue && !Finite(c.AbsMax.Value)))
                throw new ArgumentException("Invalid output check or bounds.");
        }
        Checks = Array.AsReadOnly(outputChecks);
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    public static PowerAsset Create(ModelDefinition definition, string name, string sourceSha256, ulong durationNanoseconds,
        ulong sampleEveryNanoseconds, IEnumerable<ScheduledInput> inputs, IEnumerable<AssetCheck> checks) =>
        new(definition, name, sourceSha256, durationNanoseconds, sampleEveryNanoseconds, inputs, checks);

    public ModelDefinition CopyDefinition() => new()
    { StepNanoseconds = Model.StepNanoseconds, Nodes = Nodes.ToArray(), Components = Components.ToArray() };

    public AssetPlayback CreatePlayback() => new(this);
}
