// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Security.Cryptography;
using System.Text;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class AssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("portable asset / SI quantities / provenance / ownership", RoundTrip),
        ("asset corruption / forged counts / version / fingerprint rejection", Corruption),
        ("scheduled inputs / exact boundaries / whole-call rollback", ScheduledInputs),
        ("asset playback / event cursor / batching / cancellation", Playback),
        ("asset playback zero managed allocations", Allocations)
    ];

    private static PowerAsset Sample() => PowerAsset.Create(SampleModels.Electrothermal(), "Electrothermal 实验", new string('a', 64),
        10_000_000_000, 1_000_000_000, [new(5_000_000_000, 100, 4), new(6_000_000_000, 100, 24)],
        [new(1, Field.Speed, 25, 31, null), new(0, Field.EnergyResidual, null, null, 1e-5)]);
    private static SnapshotInfo Snapshot(Simulation simulation) => simulation.ReadSnapshot(new Scalar[simulation.Model.OutputCount]);
    private static SnapshotInfo Snapshot(AssetPlayback playback) => playback.ReadSnapshot(new Scalar[playback.Model.OutputCount]);

    private static void RoundTrip()
    {
        var original = Sample();
        byte[] bytes = AssetCodec.Encode(original);
        var restored = AssetCodec.Decode(bytes);
        Require(restored.Name == original.Name && restored.SourceSha256 == original.SourceSha256);
        Require(restored.Model.Fingerprint == original.Model.Fingerprint && restored.Inputs.SequenceEqual(original.Inputs));
        Require(restored.Checks.SequenceEqual(original.Checks) && AssetCodec.Encode(restored).SequenceEqual(bytes));
        var definition = restored.CopyDefinition();
        definition.Nodes[0] = NodeDefinition.Rotor(1, 50);
        Require(restored.Nodes[0].Storage.Value == 0.2);
        var a = original.CreatePlayback(); var b = restored.CreatePlayback();
        Require(a.Advance(10_000_000_000) == SimulationStatus.Ok && b.Advance(10_000_000_000) == SimulationStatus.Ok);
        Require(Snapshot(a) == Snapshot(b));

        var thermal = PowerAsset.Create(new() { StepNanoseconds = 1_000_000_000, Nodes = [NodeDefinition.Thermal(42, 100, 320)],
            Components = [ComponentDefinition.ThermalLink(100, 42, 0, 10, 300)] }, "Thermal only", new string('b', 64),
            10_000_000_000, 1_000_000_000, [], []);
        var pureThermal = AssetCodec.Decode(AssetCodec.Encode(thermal)).CreatePlayback();
        Require(pureThermal.Advance(10_000_000_000) == SimulationStatus.Ok && pureThermal.Completed);
        Throws<ArgumentException>(() => PowerAsset.Create(original.CopyDefinition(), "", original.SourceSha256, 10, 1, [], []));
    }

    private static void Corruption()
    {
        byte[] source = AssetCodec.Encode(Sample());
        var corrupted = (byte[])source.Clone(); corrupted[200] ^= 1;
        Throws<AssetFormatException>(() => AssetCodec.Decode(corrupted));
        Throws<AssetFormatException>(() => AssetCodec.Decode(source.AsSpan(0, source.Length - 1)));
        Throws<AssetFormatException>(() => AssetCodec.Decode(new byte[AssetCodec.MaxBytes + 1]));
        void Resign(byte[] data) => SHA256.HashData(data.AsSpan(0, data.Length - 32)).CopyTo(data, data.Length - 32);
        var version = (byte[])source.Clone(); version[8] = 99; Resign(version);
        Throws<AssetFormatException>(() => AssetCodec.Decode(version));
        var fingerprint = (byte[])source.Clone(); fingerprint[12] ^= 1; Resign(fingerprint);
        Throws<AssetFormatException>(() => AssetCodec.Decode(fingerprint));
        var counts = (byte[])source.Clone(); counts[78 + Encoding.UTF8.GetByteCount(Sample().Name)] = 255; Resign(counts);
        Throws<AssetFormatException>(() => AssetCodec.Decode(counts));
        var extra = new byte[source.Length + 1]; source.CopyTo(extra, 0); Resign(extra);
        Throws<AssetFormatException>(() => AssetCodec.Decode(extra));
    }

    private static void ScheduledInputs()
    {
        var model = CompiledModel.Compile(new() { StepNanoseconds = 1_000_000, Nodes = [NodeDefinition.Rotor(1, 2)],
            Components = [ComponentDefinition.Torque(10, 1, 100, 0)] });
        var a = model.CreateSimulation(); var b = model.CreateSimulation();
        ScheduledInput[] schedule = [new(0, 100, 4), new(500_000_000, 100, -4), new(1_000_000_000, 100, 0)];
        Require(a.Step(1_000_000_000, schedule) == SimulationStatus.Ok);
        Require(b.SubmitInputs([new(100, 4)]) == SimulationStatus.Ok && b.Step(500_000_000) == SimulationStatus.Ok);
        Require(b.SubmitInputs([new(100, -4)]) == SimulationStatus.Ok && b.Step(500_000_000) == SimulationStatus.Ok);
        Require(b.SubmitInputs([new(100, 0)]) == SimulationStatus.Ok && Snapshot(a) == Snapshot(b));
        var values = new Scalar[model.OutputCount]; a.ReadSnapshot(values);
        Near(values.Single(v => v.Channel == Channels.Output(1, Field.Angle)).Value, 0.5, 1e-10);
        Near(values.Single(v => v.Channel == Channels.Output(1, Field.Speed)).Value, 0, 1e-10);
        var initial = Snapshot(b = model.CreateSimulation());
        Require(b.Step(1_000_000_000, [new(0, 100, 4), new(500_000_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure);
        Require(Snapshot(b) == initial);
        Require(b.Step(1_000_000_000, [new(0, 100, 4), new(500_000_000, 99, 1)]) == SimulationStatus.UnknownChannel);
        Require(Snapshot(b) == initial);
        Require(b.Step(1_000_000_000, [new(0, 100, 4), new(0, 100, 1)]) == SimulationStatus.InvalidInput);
        Require(b.Step(1_000_000_000, [new(3, 100, 4)]) == SimulationStatus.InvalidInput);
        Require(b.Step(1_000_000_000, [new(500_000_000, 100, 4), new(0, 100, 1)]) == SimulationStatus.InvalidInput);
        Require(Snapshot(b) == initial);
        Require(b.Step(1_000_000_000, schedule) == SimulationStatus.Ok && Snapshot(b) == Snapshot(a));
    }

    private static void Playback()
    {
        var asset = Sample(); var full = asset.CreatePlayback(); var split = asset.CreatePlayback();
        Require(full.Advance(5_000_000_000) == SimulationStatus.Ok);
        for (int i = 0; i < 250; ++i) Require(split.Advance(20_000_000) == SimulationStatus.Ok);
        Require(Snapshot(full) == Snapshot(split));
        var atFive = Snapshot(split);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Require(split.Advance(2_000_000_000, cancelled.Token) == SimulationStatus.Cancelled && Snapshot(split) == atFive);
        Require(full.Advance(5_000_000_000) == SimulationStatus.Ok && split.Advance(5_000_000_000) == SimulationStatus.Ok);
        Require(Snapshot(full) == Snapshot(split) && full.Completed);
        Require(full.Advance(100_000) == SimulationStatus.InvalidTimeStep);
        var fork = full.ForkSimulation();
        Require(Snapshot(fork) == Snapshot(full));
        Require(fork.Step(100_000_000) == SimulationStatus.Ok && full.TimeNanoseconds == 10_000_000_000);
    }

    private static void Allocations()
    {
        var playback = PowerAsset.Create(SampleModels.Electrothermal(), "Allocation check", new string('a', 64),
            1_000_000_000, 100_000_000, Enumerable.Range(0, 5000).Select(i => new ScheduledInput((ulong)i * 100_000, 100, i % 2 == 0 ? 24 : 12)), []).CreatePlayback();
        var values = new Scalar[playback.Model.OutputCount];
        for (int i = 0; i < 2000; ++i) { playback.Advance(100_000); playback.ReadSnapshot(values); }
        long before = GC.GetAllocatedBytesForCurrentThread(); bool ok = true;
        for (int i = 0; i < 2000; ++i)
        {
            ok &= playback.Advance(100_000) == SimulationStatus.Ok; playback.ReadSnapshot(values);
        }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(ok && bytes == 0, $"Playback allocated {bytes} bytes");
    }
}
