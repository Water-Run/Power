// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class GasAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("gas portable asset / mixed topology / composition / restriction round trip", RoundTrip),
        ("gas portable asset / forged extensions / missing records / old-version rejection", Corruption),
        ("authentic cylinder v2 fixture / fingerprint / upgraded replay", Legacy),
        ("gas portable schedule / range validation / cancellation / cursor rollback", Playback)
    ];

    private static PowerAsset Sample() => PowerAsset.Create(new()
    {
        StepNanoseconds = 100_000,
        Nodes = [NodeDefinition.Thermal(3, 500, 300), NodeDefinition.Rotor(4, 0.1, 150, Math.PI),
            NodeDefinition.GasVolume(2, 0.001, 1e5, 300, 307, 1.3),
            NodeDefinition.GasVolume(1, 0.002, 8e5, 600, 307, 1.3) with { Storage = new(2, Unit.Liter), Position = new(8, Unit.Bar) }],
        Components = [ComponentDefinition.GasHeatLink(12, 1, 3, 2), ComponentDefinition.SealedCylinder(13, 4, EngineChecks.Gas),
            ComponentDefinition.GasReservoir(11, 2, 5e-6, 1.2e5, 310, 0.9, 101, 0),
            ComponentDefinition.GasOrifice(10, 1, 2, 20e-6, 0.8, 100, 0) with { Area = new(20, Unit.SquareMillimeter) }]
    }, "Mixed gas laboratory", new string('c', 64), 200_000_000, 20_000_000,
        [new(0, 100, 0.5), new(50_100_000, 100, 1), new(100_300_000, 101, 0.4), new(150_700_000, 100, 0)],
        [new(0, Field.MassResidual, null, null, 1e-14), new(0, Field.EnergyResidual, null, null, 1e-6)]);

    private static SnapshotInfo Snapshot(AssetPlayback playback) => playback.ReadSnapshot(new Scalar[playback.Model.OutputCount]);
    private static void Ok(SimulationStatus status) => Require(status == SimulationStatus.Ok, status.ToString());
    private static void Resign(byte[] bytes) => SHA256.HashData(bytes.AsSpan(0, bytes.Length - 32)).CopyTo(bytes, bytes.Length - 32);

    private static void RoundTrip()
    {
        var asset = Sample(); var bytes = AssetCodec.Encode(asset); var restored = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == 11);
        Require(restored.Nodes.SequenceEqual(asset.Nodes) && restored.Components.SequenceEqual(asset.Components));
        Require(restored.SourceSha256 == asset.SourceSha256 && restored.Inputs.SequenceEqual(asset.Inputs) && restored.Checks.SequenceEqual(asset.Checks));
        Require(restored.Model.Fingerprint == asset.Model.Fingerprint && AssetCodec.Encode(restored).SequenceEqual(bytes));
        var definition = restored.CopyDefinition();
        definition.Nodes[0] = NodeDefinition.GasVolume(1, 1, 1e5, 300);
        Require(restored.Nodes[0].Gas!.GasConstant.Value == 307 && restored.Nodes[0].Storage == new Quantity(2, Unit.Liter));
        var direct = asset.CreatePlayback(); var split = restored.CreatePlayback();
        foreach (ulong at in new ulong[] { 50_100_000, 100_300_000, 150_700_000, 200_000_000 })
        {
            Ok(direct.Advance(at - direct.TimeNanoseconds));
            while (split.TimeNanoseconds < at) Ok(split.Advance(Math.Min(1_700_000, at - split.TimeNanoseconds)));
            Require(Snapshot(direct) == Snapshot(split));
        }
        var values = new Scalar[restored.Model.OutputCount]; split.ReadSnapshot(values);
        foreach (var check in restored.Checks)
            Require(Math.Abs(values.Single(v => v.Channel == Channels.Output(check.ObjectId, check.Field)).Value) <= check.AbsMax);
    }

    private static void Corruption()
    {
        var asset = Sample(); byte[] source = AssetCodec.Encode(asset);
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name);
        int tables = counts + 80;
        int gasExtensions = tables + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 116;
        int orificeExtensions = gasExtensions + 24 * 2;
        void RejectInt(int offset, int value)
        {
            byte[] bytes = (byte[])source.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), value); Resign(bytes);
            Throws<AssetFormatException>(() => AssetCodec.Decode(bytes));
        }
        RejectInt(counts + 20, -1); RejectInt(counts + 24, int.MaxValue);
        RejectInt(gasExtensions, 2); // Thermal node.
        RejectInt(gasExtensions, -1); RejectInt(gasExtensions, asset.Nodes.Count);
        RejectInt(gasExtensions + 24, 0); // Duplicate first gas node.
        RejectInt(orificeExtensions, 2); // Gas heat link.
        RejectInt(orificeExtensions, asset.Components.Count);
        RejectInt(orificeExtensions + 36, 0); // Duplicate first restriction.
        var changedArea = (byte[])source.Clone();
        BinaryPrimitives.WriteInt64LittleEndian(changedArea.AsSpan(orificeExtensions + 4), BitConverter.DoubleToInt64Bits(21));
        Resign(changedArea);
        Throws<AssetFormatException>(() => AssetCodec.Decode(changedArea)); // Valid parameters, stale fingerprint.

        void Remove(int start, int length, int countOffset, int count)
        {
            var bytes = source.Take(start).Concat(source.Skip(start + length)).ToArray();
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(countOffset), count); Resign(bytes);
            Throws<AssetFormatException>(() => AssetCodec.Decode(bytes));
        }
        Remove(gasExtensions, 24, counts + 20, 1);
        Remove(orificeExtensions, 36, counts + 24, 1);
        // A length-correct v2 layout cannot smuggle in gas domains or components.
        var v2 = source.Take(counts + 20).Concat(source.Skip(tables).Take(gasExtensions - tables))
            .Concat(source.Skip(orificeExtensions + 72)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(v2.AsSpan(8), 2); Resign(v2);
        Throws<AssetFormatException>(() => AssetCodec.Decode(v2));
    }

    private static void Legacy()
    {
        byte[] bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "sealed-cylinder-v2.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(bytes)) == "73dd5165bf53761c00d03dd2ee90c681685bed3922eb9b171e69f194a8915644");
        var old = AssetCodec.Decode(bytes); var upgraded = AssetCodec.Decode(AssetCodec.Encode(old));
        Require(old.Model.Fingerprint.ToString("x16") == "c64b61efdb827680");
        Require(old.Model.Fingerprint == upgraded.Model.Fingerprint && old.Components[0].Cylinder == EngineChecks.Gas);
        var a = old.CreatePlayback(); var b = upgraded.CreatePlayback();
        for (int i = 0; i < 20; ++i)
        {
            Ok(a.Advance(10_000_000)); Ok(b.Advance(10_000_000));
            Require(Snapshot(a) == Snapshot(b));
        }
        Require(a.Completed && b.Completed);
    }

    private static void Playback()
    {
        var sample = Sample();
        foreach (double invalid in new[] { -0.001, 1.001, double.NaN, double.PositiveInfinity })
            Throws<ArgumentException>(() => PowerAsset.Create(sample.CopyDefinition(), sample.Name, sample.SourceSha256,
                sample.DurationNanoseconds, sample.SampleEveryNanoseconds, [new(150_000_000, 100, invalid)], []));
        Require(sample.Model.ValidateInput(new(100, 0)) == SimulationStatus.Ok && sample.Model.ValidateInput(new(100, 1)) == SimulationStatus.Ok);
        Require(sample.Model.ValidateInput(new(999, 0)) == SimulationStatus.UnknownChannel);
        var playback = AssetCodec.Decode(AssetCodec.Encode(sample)).CreatePlayback();
        var start = Snapshot(playback);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Require(playback.Advance(200_000_000, cancelled.Token) == SimulationStatus.Cancelled && Snapshot(playback) == start);
        Ok(playback.Advance(200_000_000));
        var reference = sample.CreatePlayback(); Ok(reference.Advance(200_000_000));
        Require(Snapshot(reference) == Snapshot(playback));

        // Failure after a closed first tick must restore the time, inputs and event cursor.
        var failing = PowerAsset.Create(new()
        {
            StepNanoseconds = 500_000_000, Nodes = [NodeDefinition.GasVolume(1, 0.002, 2e6, 900)],
            Components = [ComponentDefinition.GasReservoir(10, 1, 0.02, 1e5, 300, 0.9, 100, 0)]
        }, "Rollback", new string('a', 64), 1_500_000_000, 500_000_000, [new(500_000_000, 100, 1)], []).CreatePlayback();
        var before = Snapshot(failing);
        for (int retry = 0; retry < 2; ++retry)
            Require(failing.Advance(1_000_000_000) == SimulationStatus.NumericalFailure && Snapshot(failing) == before && failing.TimeNanoseconds == 0);
        var branch = failing.ForkSimulation(); Ok(branch.Step(1_000_000_000));
        Require(Snapshot(failing) == before);
    }
}
