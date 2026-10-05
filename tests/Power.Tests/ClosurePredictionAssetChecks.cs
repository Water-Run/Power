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

internal static class ClosurePredictionAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("closure asset / v21 horizon and cutoff state / every replay boundary / authentic v20", Replay),
        ("closure asset / aligned bounded horizon / malformed driver and forged v20 downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(ClosurePredictionChecks.Model(), "Closure prediction", new string('e', 64), 30_000_000, 500_000, [], []);
    private static void Compare(PowerAsset a, PowerAsset b)
    {
        var left = a.CreatePlayback(); var right = b.CreatePlayback(); var lv = new Scalar[a.Model.OutputCount]; var rv = new Scalar[b.Model.OutputCount];
        for (ulong at = 0; at <= a.DurationNanoseconds; at += a.SampleEveryNanoseconds)
        {
            if (at > left.TimeNanoseconds) Require(left.Advance(at - left.TimeNanoseconds) == SimulationStatus.Ok);
            while (right.TimeNanoseconds < at) Require(right.Advance(Math.Min(257 * b.Model.StepNanoseconds, at - right.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(left.ReadSnapshot(lv) == right.ReadSnapshot(rv) && lv.SequenceEqual(rv));
        }
    }
    private static void Replay()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == 25 && decoded.Model.HasClosurePrediction);
        Require(asset.Components.SequenceEqual(decoded.Components) && bytes.SequenceEqual(AssetCodec.Encode(decoded))); Compare(asset, decoded);
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "needle-actuated-cylinder-v20.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 20 && Convert.ToHexStringLower(SHA256.HashData(fixture)) == "4d87e996d92a50508cfcffac610551b84220f2b3055f5b104c8ec7ec64ca9a2e");
        var old = AssetCodec.Decode(fixture); Require(old.Model.Fingerprint.ToString("x16") == "3fa813ff44b95a79" && !old.Model.HasClosurePrediction);
        Compare(old, AssetCodec.Decode(AssetCodec.Encode(old)));
    }
    private static void Corruption()
    {
        var asset = Asset(); byte[] source = AssetCodec.Encode(asset);
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name);
        int driver = counts + 148 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 24 + 36 + 40 + 64 + 120 + 28 + 40 + 32;
        void Reject(byte[] data)
        { SHA256.HashData(data.AsSpan(0, data.Length - 32)).CopyTo(data, data.Length - 32); Throws<ArgumentException>(() => AssetCodec.Decode(data)); }
        foreach (ulong horizon in new ulong[] { 1, 99_999, 40_970_000, ulong.MaxValue })
        { var bad = source.ToArray(); BinaryPrimitives.WriteUInt64LittleEndian(bad.AsSpan(driver + 32), horizon); Reject(bad); }
        var downgrade = source.Take(driver + 32).Concat(source.Skip(driver + 40)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8), 20); Reject(downgrade);
    }
}
