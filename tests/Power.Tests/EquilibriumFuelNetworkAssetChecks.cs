// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class EquilibriumFuelNetworkAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel network asset / complete phase transport histories / authentic v31 upgrade", Replay),
        ("fuel network asset / inferred bounded material records / corruption and forged downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(EquilibriumFuelNetworkChecks.Model(heat: true), "Fuel network", new string('e', 64), 200_000_000, 10_000_000,
        [new(50_000_000, 100, 0), new(100_000_000, 100, 1)], []);
    private static void Sign(byte[] bytes) => SHA256.HashData(bytes.AsSpan(0, bytes.Length - 32)).CopyTo(bytes, bytes.Length - 32);
    private static void Compare(PowerAsset a, PowerAsset b)
    {
        var left = a.CreatePlayback(); var right = b.CreatePlayback(); var lv = new Scalar[a.Model.OutputCount]; var rv = new Scalar[b.Model.OutputCount];
        for (ulong time = 0; time <= a.DurationNanoseconds; time += a.SampleEveryNanoseconds)
        {
            if (time > left.TimeNanoseconds) Require(left.Advance(time - left.TimeNanoseconds) == SimulationStatus.Ok);
            while (right.TimeNanoseconds < time) Require(right.Advance(Math.Min(257 * b.Model.StepNanoseconds, time - right.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(left.ReadSnapshot(lv) == right.ReadSnapshot(rv) && lv.SequenceEqual(rv));
        }
    }
    private static void Replay()
    {
        var asset = Asset(); var bytes = AssetCodec.Encode(asset); var restored = AssetCodec.Decode(bytes);
        Require(restored.Model.HasEquilibriumFuelNetwork && bytes.SequenceEqual(AssetCodec.Encode(restored))); Compare(asset, restored);
        var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "equilibrium-tank-supply-v31.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 31 &&
            Convert.ToHexStringLower(SHA256.HashData(fixture)) == "8042d65c3171a71cd79eaa0b49ba733a232a805bd4ed429d23a93e5821016349");
        var old = AssetCodec.Decode(fixture); Require(old.Model.HasEquilibriumTanks && !old.Model.HasEquilibriumFuelNetwork && old.Model.Fingerprint.ToString("x16") == "6b0e2b783c7a15f0");
        var upgraded = AssetCodec.Encode(old); var back = upgraded.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(back.AsSpan(8), 31); Sign(back);
        Require(back.SequenceEqual(fixture)); Compare(old, AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        var asset = Asset(); var bytes = AssetCodec.Encode(asset);
        int table = bytes.Length - 32 - 24 * asset.Inputs.Count - 33 * asset.Checks.Count - 2 * 148;
        void Reject(byte[] bad) { Sign(bad); Throws<ArgumentException>(() => AssetCodec.Decode(bad)); }
        foreach (int index in new[] { -1, 2, int.MaxValue })
        { var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(table), index); Reject(bad); }
        var duplicate = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(table + 148), 0); Reject(duplicate);
        var unit = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(unit.AsSpan(table + 12), (int)Unit.Kelvin); Reject(unit);
        var density = bytes.ToArray(); BinaryPrimitives.WriteDoubleLittleEndian(density.AsSpan(table + 16), 0); Reject(density);
        var missing = bytes.Take(table).Concat(bytes.Skip(table + 148)).ToArray(); Reject(missing);
        var downgrade = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8), 31); Reject(downgrade);
        Reject(bytes.Concat(new byte[4]).ToArray());
    }
}
