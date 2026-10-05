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

internal static class RavigneauxAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("Ravigneaux asset / complete compound ports / all boundaries / authentic v22", Replay),
        ("Ravigneaux asset / typed carriers counts duplicate records and forged downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(RavigneauxChecks.Model(), "Compound planetary", new string('d', 64), 20_000_000, 1_000_000, [], []);
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
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == 26 && decoded.Components.SequenceEqual(asset.Components));
        Require(bytes.SequenceEqual(AssetCodec.Encode(decoded))); Compare(asset, decoded);
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "controlled-dual-clutch-v22.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 22 && Convert.ToHexStringLower(SHA256.HashData(fixture)) == "db90df5fa9ca90067341abdcaf8c3a4a38316794a4b3c8ecc7c89a852375c24e");
        var old = AssetCodec.Decode(fixture); Require(old.Model.Fingerprint.ToString("x16") == "72122eae163df98e"); Compare(old, AssetCodec.Decode(AssetCodec.Encode(old)));
    }
    private static void Corruption()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset); int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name);
        int record = counts + 152 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 28 * 5;
        void Reject(byte[] bad) { SHA256.HashData(bad.AsSpan(0, bad.Length - 32)).CopyTo(bad, bad.Length - 32); Throws<ArgumentException>(() => AssetCodec.Decode(bad)); }
        foreach (var change in new[] { (counts + 52, -1), (counts + 52, 0), (counts + 52, 65), (record + 8, 0), (record + 12, 0), (record + 12, 101) })
        { var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(change.Item1), change.Item2); Reject(bad); }
        var down = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(down.AsSpan(8), 22); Reject(down);
    }
}
