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

internal static class ClutchAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("clutch asset / capacities and channels / atomic replay / authentic v6 fixture", Replay),
        ("clutch asset / v11 counts / missing and duplicate extensions / downgrade rejection", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(ClutchModelChecks.PairModel(step: 1_000_000),
        "Clutch replay", new string('a', 64), 3_000_000_000, 100_000_000,
        [new(450_000_000, 100, .5), new(600_000_000, 100, 1)],
        [new(10, Field.SlipSpeed, null, null, 1e-8), new(10, Field.FrictionHeat, 799.999, 800.001, null)]);

    private static void Replay()
    {
        var original = Asset(); byte[] bytes = AssetCodec.Encode(original); var decoded = AssetCodec.Decode(bytes);
        Require(decoded.Components.SequenceEqual(original.Components) && decoded.Model.Fingerprint == original.Model.Fingerprint);
        Require(AssetCodec.Encode(decoded).SequenceEqual(bytes));
        var a = original.CreatePlayback(); var b = decoded.CreatePlayback(); var values = new Scalar[a.Model.OutputCount];
        for (int i = 0; i < 30; ++i)
        {
            Require(a.Advance(100_000_000) == SimulationStatus.Ok);
            for (int j = 0; j < 10; ++j) Require(b.Advance(10_000_000) == SimulationStatus.Ok);
            Require(a.ReadSnapshot(values) == b.ReadSnapshot(new Scalar[b.Model.OutputCount]));
        }
        var failing = PowerAsset.Create(original.CopyDefinition(), "Rollback", new string('b', 64), 3_000_000_000, 100_000_000,
            [new(2_500_000_000, 101, double.MaxValue)], []);
        var playback = failing.CreatePlayback(); var before = playback.ReadSnapshot(values);
        Require(playback.Advance(3_000_000_000) == SimulationStatus.NumericalFailure && playback.ReadSnapshot(values) == before);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Require(playback.Advance(100_000_000, cancel.Token) == SimulationStatus.Cancelled && playback.ReadSnapshot(values) == before);
        Require(playback.Advance(2_000_000_000) == SimulationStatus.Ok);

        byte[] oldBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "fired-cylinder-v6.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(oldBytes)) == "b436195dbff16db2e20b1a789fd9fb19ff6e32bdcd6254310eeafb7f278acaf7");
        var old = AssetCodec.Decode(oldBytes); Require(old.Model.Fingerprint.ToString("x16") == "a10f880d74494677");
        var upgraded = AssetCodec.Decode(AssetCodec.Encode(old)); var oldRun = old.CreatePlayback(); var newRun = upgraded.CreatePlayback();
        Require(oldRun.Advance(old.DurationNanoseconds) == SimulationStatus.Ok && newRun.Advance(old.DurationNanoseconds) == SimulationStatus.Ok);
        Require(oldRun.ReadSnapshot(new Scalar[old.Model.OutputCount]) == newRun.ReadSnapshot(new Scalar[old.Model.OutputCount]));
    }

    private static void Corruption()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset);
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name), extension = counts + 80 + 44 * asset.Nodes.Count + 156 * asset.Components.Count;
        void Reject(byte[] bad)
        {
            SHA256.HashData(bad.AsSpan(0, bad.Length - 32)).CopyTo(bad, bad.Length - 32);
            Throws<ArgumentException>(() => AssetCodec.Decode(bad));
        }
        void Change(int offset, int value)
        { var bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(offset), value); Reject(bad); }
        Change(counts + 48, -1); Change(counts + 48, 65); Change(counts + 48, 0);
        Change(extension, -1); Change(extension, asset.Components.Count); Change(extension, 1);
        Change(extension + 12, (int)Unit.Joule);
        var missing = bytes.Take(extension).Concat(bytes.Skip(extension + 28)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts + 48), 0); Reject(missing);
        var duplicate = bytes.Take(extension + 28).Concat(bytes.Skip(extension).Take(28)).Concat(bytes.Skip(extension + 28)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts + 48), 2); Reject(duplicate);
        var oldVersion = bytes.Take(counts + 48).Concat(bytes.Skip(counts + 80).Take(extension - counts - 80)).Concat(bytes.Skip(extension + 28)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(oldVersion.AsSpan(8), 6); Reject(oldVersion);
        var wrongCapacity = (byte[])bytes.Clone(); BitConverter.GetBytes(25.0).CopyTo(wrongCapacity, extension + 16); Reject(wrongCapacity);
    }
}
