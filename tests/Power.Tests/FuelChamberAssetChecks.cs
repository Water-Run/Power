// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class FuelChamberAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel chamber asset / moving geometry shaft histories and authentic v32 upgrade", Replay),
        ("fuel chamber asset / inferred bounded geometry records / invalid ownership and downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(FuelChamberChecks.Model(), "Fuel chamber pump", new string('f', 64), 100_000_000, 5_000_000,
        [new(50_000_000, 100, 0), new(75_000_000, 100, 1)], []);
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
        Require(restored.Model.HasFuelChambers && bytes.SequenceEqual(AssetCodec.Encode(restored))); Compare(asset, restored);
        var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "equilibrium-fuel-network-v32.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 32 && Convert.ToHexStringLower(SHA256.HashData(fixture)) == "dc53c55f9ed58503a4e37d3b9a57ace52e5aac8196fdb5707bb555c47f3a593e");
        var old = AssetCodec.Decode(fixture); Require(old.Model.HasEquilibriumFuelNetwork && !old.Model.HasFuelChambers && old.Model.Fingerprint.ToString("x16") == "e27805e1a2c808d1");
        var upgraded = AssetCodec.Encode(old); var back = upgraded.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(back.AsSpan(8), 32); Sign(back); Require(back.SequenceEqual(fixture));
        Compare(old, AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        var asset = Asset(); var bytes = AssetCodec.Encode(asset); int geometry = bytes.Length - 32 - 24 * asset.Inputs.Count - 33 * asset.Checks.Count - 40;
        void Reject(byte[] bad) { Sign(bad); Throws<ArgumentException>(() => AssetCodec.Decode(bad)); }
        foreach (int index in new[] { -1, 0, int.MaxValue }) { var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(geometry), index); Reject(bad); }
        var unit = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(unit.AsSpan(geometry + 12), (int)Unit.Kelvin); Reject(unit);
        var volume = bytes.ToArray(); BinaryPrimitives.WriteDoubleLittleEndian(volume.AsSpan(geometry + 16), -1); Reject(volume);
        var phase = bytes.ToArray(); BinaryPrimitives.WriteDoubleLittleEndian(phase.AsSpan(geometry + 28), double.NaN); Reject(phase);
        var downgrade = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8), 32); Reject(downgrade);
        Reject(bytes.Take(geometry).Concat(bytes.Skip(geometry + 40)).ToArray());
        var strip = bytes.Take(geometry).Concat(bytes.Skip(geometry + 40)).ToArray(); BinaryPrimitives.WriteInt32LittleEndian(strip.AsSpan(8), 32); Reject(strip);
    }
}
