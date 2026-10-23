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

internal static class LiquidRailReturnAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("liquid return asset / complete heat mass histories and authentic v27 replay", Replay),
        ("liquid return asset / counts ownership fraction units and forged v27 downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(LiquidRailReturnChecks.Model(), "Liquid return", new string('a', 64), 20_000_000, 1_000_000, [], []);
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
        var asset = Asset(); var bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == AssetCodec.FormatVersion && decoded.Model.HasLiquidReturns);
        Require(bytes.SequenceEqual(AssetCodec.Encode(decoded))); Compare(asset, decoded);
        var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "finite-tank-liquid-cylinder-v27.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 27 && Convert.ToHexStringLower(SHA256.HashData(fixture)) == "ee344fad148d226c223145d610b18f84912060bc722fee980cae6515ceea3d62");
        var old = AssetCodec.Decode(fixture); Compare(old, AssetCodec.Decode(AssetCodec.Encode(old)));
    }
    private static void Corruption()
    {
        var asset = Asset(); var bytes = AssetCodec.Encode(asset); int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name), record = bytes.Length - 56;
        void Reject(byte[] bad) { SHA256.HashData(bad.AsSpan(0, bad.Length - 32)).CopyTo(bad, bad.Length - 32); Throws<ArgumentException>(() => AssetCodec.Decode(bad)); }
        foreach (var change in new[] { (counts + 160, -1), (counts + 160, 0), (counts + 160, 65), (record, -1), (record + 4, 20), (record + 8, 12), (record + 20, (int)Unit.Pascal) })
        { var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(change.Item1), change.Item2); Reject(bad); }
        var range = bytes.ToArray(); BinaryPrimitives.WriteDoubleLittleEndian(range.AsSpan(record + 12), 2); Reject(range);
        var down = bytes.Take(counts + 160).Concat(bytes.Skip(counts + 160).Take(record - counts - 160)).Concat(bytes.Skip(record + 24)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(down.AsSpan(8), 27); Reject(down);
    }
}
