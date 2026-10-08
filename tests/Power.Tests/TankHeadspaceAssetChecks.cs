// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class TankHeadspaceAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("tank headspace asset / complete geometry vent histories exact replay / authentic v28", Replay),
        ("tank headspace asset / forged geometry units links capacity and downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(TankHeadspaceChecks.Model(ventArea: 2e-8), "Tank headspace",
        new string('a', 64), 20_000_000, 1_000_000, [], []);
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
        Require(decoded.Model.HasTankHeadspaces && BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == 29);
        Require(bytes.SequenceEqual(AssetCodec.Encode(decoded))); Compare(asset, decoded);
        var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "recirculating-liquid-cylinder-v28.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 28 &&
            Convert.ToHexStringLower(SHA256.HashData(fixture)) == "3c871df37c63e671efdbaf34730a70fced19840f0fb9f1b1e5655b31d6cd2a57");
        var old = AssetCodec.Decode(fixture); Require(!old.Model.HasTankHeadspaces); Compare(old, AssetCodec.Decode(AssetCodec.Encode(old)));
    }
    private static void Corruption()
    {
        var bytes = AssetCodec.Encode(Asset()); int tank = bytes.Length - 80;
        void Reject(byte[] bad) { SHA256.HashData(bad.AsSpan(0, bad.Length - 32)).CopyTo(bad, bad.Length - 32); Throws<ArgumentException>(() => AssetCodec.Decode(bad)); }
        foreach (var change in new[] { (tank + 40, (int)Unit.Pascal), (tank + 44, 0), (tank + 44, 2) })
        { var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(change.Item1), change.Item2); Reject(bad); }
        var full = bytes.ToArray(); BinaryPrimitives.WriteDoubleLittleEndian(full.AsSpan(tank + 32), 1e-12); Reject(full);
        var downgraded = bytes.Take(tank + 32).Concat(bytes.Skip(tank + 48)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(downgraded.AsSpan(8), 28); Reject(downgraded);
    }
}
