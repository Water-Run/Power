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

internal static class LiquidFuelTankAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("finite tank asset / empty inventory histories exact replay authentic v26", Replay),
        ("finite tank asset / counts links units exclusivity forged v26 downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(LiquidFuelTankChecks.Model(), "Finite tank", new string('a', 64), 50_000_000, 1_000_000, [], []);
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
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == AssetCodec.FormatVersion && decoded.Model.HasLiquidTanks);
        Require(bytes.SequenceEqual(AssetCodec.Encode(decoded))); Compare(asset, decoded);
        var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "pump-fed-liquid-cylinder-v26.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 26 && Convert.ToHexStringLower(SHA256.HashData(fixture)) == "b57fa9c5ff4814e9fe898882fe7146465b68878f61423805f23e5f717bfb995d");
        var old = AssetCodec.Decode(fixture); Compare(old, AssetCodec.Decode(AssetCodec.Encode(old)));
    }
    private static void Corruption()
    {
        var asset = Asset(); var bytes = AssetCodec.Encode(asset); int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name), tank = bytes.Length - 80, feed = tank - 28;
        void Reject(byte[] bad) { SHA256.HashData(bad.AsSpan(0, bad.Length - 32)).CopyTo(bad, bad.Length - 32); Throws<ArgumentException>(() => AssetCodec.Decode(bad)); }
        foreach (var change in new[] { (counts + 152, -1), (counts + 152, 0), (counts + 152, 65), (tank, -1), (tank + 4, 10), (tank + 16, (int)Unit.Pascal), (feed + 24, 12) })
        { var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(change.Item1), change.Item2); Reject(bad); }
        var down = bytes.Take(counts + 152).Concat(bytes.Skip(counts + 160).Take(feed + 24 - counts - 160)).Concat(bytes.Skip(tank + 32)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(down.AsSpan(8), 26); Reject(down);
    }
}
