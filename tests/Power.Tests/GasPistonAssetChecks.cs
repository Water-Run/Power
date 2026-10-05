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

internal static class GasPistonAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("gas piston asset / complete v18 gas geometry / authentic v15 replay", Replay),
        ("gas piston asset / bounded typed records / units / duplicates / downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(GasPistonChecks.Model(), "Linear gas actuator", new string('f', 64),
        100_000_000, 10_000_000, [new(50_100_000, 100, 0)], []);
    private static void Compare(PowerAsset old, PowerAsset updated)
    {
        var a = old.CreatePlayback(); var b = updated.CreatePlayback(); var left = new Scalar[old.Model.OutputCount]; var right = new Scalar[updated.Model.OutputCount];
        var boundaries = new SortedSet<ulong> { 0, old.DurationNanoseconds };
        for (ulong at = old.SampleEveryNanoseconds; at < old.DurationNanoseconds; at += old.SampleEveryNanoseconds) boundaries.Add(at);
        foreach (var input in old.Inputs) boundaries.Add(input.TimeNanoseconds);
        foreach (ulong at in boundaries)
        {
            if (at > a.TimeNanoseconds) Require(a.Advance(at - a.TimeNanoseconds) == SimulationStatus.Ok);
            while (b.TimeNanoseconds < at) Require(b.Advance(Math.Min(257 * old.Model.StepNanoseconds, at - b.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(a.ReadSnapshot(left) == b.ReadSnapshot(right) && left.SequenceEqual(right));
        }
    }
    private static void Replay()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == 25 && decoded.Model.Fidelity == "linear_gas_actuation");
        Require(asset.Components.SequenceEqual(decoded.Components) && bytes.SequenceEqual(AssetCodec.Encode(decoded))); Compare(asset, decoded);
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "spool-regulated-pump-v15.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(fixture)) == "67b976a27ca0bd7343ca6b024c5bc736e14f48326945da841785ade43b61f14b");
        var original = AssetCodec.Decode(fixture); Require(original.Model.Fingerprint.ToString("x16") == "28aa0965d248e280" && !original.Model.HasGasPistons);
        Compare(original, AssetCodec.Decode(AssetCodec.Encode(original)));
        var final = original.CreatePlayback(); Require(final.Advance(original.DurationNanoseconds) == SimulationStatus.Ok);
        AssetChecks.Reference(final, (2, Field.Pressure, 233956.17402528782, .001), (14, Field.Opening, .08487847737006522, 1e-9), (17, Field.SlipSpeed, 0, 1e-8));
    }
    private static void Corruption()
    {
        var asset = Asset(); byte[] source = AssetCodec.Encode(asset);
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name), record = counts + 148 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 24;
        void Reject(byte[] data) { SHA256.HashData(data.AsSpan(0, data.Length - 32)).CopyTo(data, data.Length - 32); Throws<ArgumentException>(() => AssetCodec.Decode(data)); }
        foreach (int count in new[] { -1, 65, int.MaxValue }) { var bad = source.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(counts + 104), count); Reject(bad); }
        foreach (var replacement in new[] { (0, 1), (4, 0), (16, (int)Unit.Meter), (28, (int)Unit.Kilogram) })
        { var bad = source.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(record + replacement.Item1), replacement.Item2); Reject(bad); }
        var duplicate = source.Take(record + 56).Concat(source.Skip(record).Take(56)).Concat(source.Skip(record + 56)).ToArray(); BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts + 104), 2); Reject(duplicate);
        var missing = source.Take(record).Concat(source.Skip(record + 56)).ToArray(); BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts + 104), 0); Reject(missing);
        var downgraded = source.Take(counts + 104).Concat(source.Skip(counts + 148).Take(record - counts - 148)).Concat(source.Skip(record + 56)).ToArray(); BinaryPrimitives.WriteInt32LittleEndian(downgraded.AsSpan(8), 15); Reject(downgraded);
    }
}
