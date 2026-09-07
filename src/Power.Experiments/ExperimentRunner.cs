// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Power.Core;

namespace Power.Experiments;

public sealed record SampleReport(ulong TimeNs, string StateHash, Dictionary<string, double> Values);
public sealed record CheckResult(uint ObjectId, string Field, double Value, double? Min, double? Max, double? AbsMax, bool Passed);
public sealed record ModelReport(string Fingerprint, ulong StepNs, int NodeCount, int ComponentCount, int StateCount, string Fidelity, string Calibration);
public sealed record ReplayReport(bool SampleHashesMatch, int BoundariesChecked, int[] BatchTicks);
public sealed record ChannelReport(string Channel, uint ObjectId, string Direction, string Unit, string Quantity);
public sealed record ExperimentReport(string Schema, bool Passed, string Runtime, string Machine, string SourceSha256,
    ModelReport Model, ReplayReport Replay, ChannelReport[] Channels, CheckResult[] Checks, SampleReport[] Samples, double ElapsedSeconds);

public static class ExperimentRunner
{
    public static CompiledModel Validate(ModelDocument document)
    {
        Prepare(document, out var model, out _, out _);
        return model;
    }

    private static void Prepare(ModelDocument document, out CompiledModel model,
        out SortedSet<ulong> boundaries, out Dictionary<ulong, Scalar[]> events)
    {
        model = CompiledModel.Compile(document.Model);
        ulong duration = document.DurationNanoseconds, sample = document.SampleEveryNanoseconds, step = model.StepNanoseconds;
        if (duration == 0 || sample == 0 || sample > duration || duration % step != 0 || sample % step != 0 ||
            duration / step > 10_000_000 || duration / sample > 10000 || document.Events.Length > 10000)
            throw new ArgumentException("Experiment times must align with fixed ticks and stay within the tick/sample budgets.");
        boundaries = new SortedSet<ulong> { 0, duration };
        for (ulong at = sample; at < duration; at += sample) boundaries.Add(at);
        var inputs = model.Channels.Where(c => c.IsInput).Select(c => c.Id).ToHashSet();
        var outputs = model.Channels.Where(c => !c.IsInput).Select(c => c.Id).ToHashSet();
        events = new Dictionary<ulong, Scalar[]>();
        ulong previous = 0;
        foreach (var e in document.Events)
        {
            if (e.TimeNanoseconds >= duration || e.TimeNanoseconds % step != 0 ||
                (events.Count > 0 && e.TimeNanoseconds <= previous) || e.Values.Length == 0 || e.Values.Length > inputs.Count ||
                e.Values.Select(v => v.Channel).Distinct().Count() != e.Values.Length ||
                e.Values.Any(v => !inputs.Contains(v.Channel) || !double.IsFinite(v.Value)))
                throw new ArgumentException("Invalid, duplicated, unknown, or unordered input event.");
            events.Add(e.TimeNanoseconds, e.Values); boundaries.Add(e.TimeNanoseconds); previous = e.TimeNanoseconds;
        }
        foreach (var c in document.Checks)
        {
            if (!outputs.Contains(Core.Channels.Output(c.ObjectId, c.Field)) ||
                (c.Min is null && c.Max is null && c.AbsMax is null) || c.Min > c.Max || c.AbsMax < 0 ||
                (c.Min.HasValue && !double.IsFinite(c.Min.Value)) || (c.Max.HasValue && !double.IsFinite(c.Max.Value)) ||
                (c.AbsMax.HasValue && !double.IsFinite(c.AbsMax.Value)))
                throw new ArgumentException("Invalid output check or bounds.");
        }
    }

    public static ExperimentReport Evaluate(ModelDocument document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var timer = Stopwatch.StartNew();
        Prepare(document, out var model, out var boundaries, out var events);
        ulong step = model.StepNanoseconds;
        SampleReport[] Run(ulong chunk)
        {
            var simulation = model.CreateSimulation();
            var values = new Scalar[model.OutputCount];
            var samples = new List<SampleReport>();
            ulong now = 0;
            foreach (ulong at in boundaries)
            {
                while (now < at)
                {
                    ulong delta = Math.Min(at - now, chunk * step);
                    var status = simulation.Step(delta, cancellationToken);
                    if (status == SimulationStatus.Cancelled) cancellationToken.ThrowIfCancellationRequested();
                    CheckStatus(status); now += delta;
                }
                if (events.TryGetValue(at, out var changes)) CheckStatus(simulation.SubmitInputs(changes));
                var snapshot = simulation.ReadSnapshot(values);
                samples.Add(new(snapshot.TimeNanoseconds, snapshot.StateHash.ToString("x16"),
                    values.ToDictionary(v => v.Channel.ToString(System.Globalization.CultureInfo.InvariantCulture), v => v.Value)));
            }
            return samples.ToArray();
        }
        var samples = Run(1_000_000); var replay = Run(257);
        bool matched = samples.Zip(replay).All(pair => pair.First.StateHash == pair.Second.StateHash &&
            pair.First.TimeNs == pair.Second.TimeNs && pair.First.Values.All(v => pair.Second.Values[v.Key] == v.Value));
        var results = document.Checks.Select(c =>
        {
            double value = samples[^1].Values[Core.Channels.Output(c.ObjectId, c.Field).ToString(System.Globalization.CultureInfo.InvariantCulture)];
            return new CheckResult(c.ObjectId, c.Field.ToString(), value, c.Min, c.Max, c.AbsMax,
                value >= (c.Min ?? double.NegativeInfinity) && value <= (c.Max ?? double.PositiveInfinity) && Math.Abs(value) <= (c.AbsMax ?? double.PositiveInfinity));
        }).ToArray();
        return new("power.experiment_report.v2", matched && results.All(c => c.Passed), RuntimeInformation.FrameworkDescription,
            RuntimeInformation.RuntimeIdentifier, document.SourceSha256,
            new(model.Fingerprint.ToString("x16"), step, model.NodeCount, model.ComponentCount, model.StateCount, model.Fidelity, model.Calibration),
            new(matched, samples.Length, [1_000_000, 257]),
            model.Channels.Select(c => new ChannelReport(c.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), c.ObjectId,
                c.IsInput ? "input" : "output", c.Unit.ToString(), c.Quantity)).ToArray(), results, samples, timer.Elapsed.TotalSeconds);
    }

    private static void CheckStatus(SimulationStatus status)
    {
        if (status != SimulationStatus.Ok) throw new ArgumentException($"Simulation failed: {status}.");
    }
}
