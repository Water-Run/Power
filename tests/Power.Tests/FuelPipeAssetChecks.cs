// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class FuelPipeAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel pipe asset / own thermal kinetic and directional histories / authentic v34 replay", Replay),
        ("fuel pipe asset / inferred bounded record geometry units phase and downgrade rejection", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(FuelPipeChecks.Model(), "Inertial liquid fuel", new string('b', 64), 20_000_000, 1_000_000, [], []);
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
        var asset = Asset(); var bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(decoded.Model.HasFuelPipes && asset.Components.SequenceEqual(decoded.Components) && bytes.SequenceEqual(AssetCodec.Encode(decoded))); Compare(asset, decoded);
        var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "pump-fed-finite-volume-cylinder-v34.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 34 && Convert.ToHexStringLower(SHA256.HashData(fixture)) == "51c0f51ef6ff0db9d84d952793da0fe2592f855f12302a96774294193d02b587");
        var old = AssetCodec.Decode(fixture); Require(!old.Model.HasFuelPipes && old.Model.HasFuelDischarge && old.Model.Fingerprint.ToString("x16") == "e4225e412bfa9802");
        var upgraded = AssetCodec.Encode(old); var back = upgraded.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(back.AsSpan(8), 34); Sign(back); Require(back.SequenceEqual(fixture));
        Compare(old, AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        var bytes = AssetCodec.Encode(Asset()); int record = bytes.Length - 32 - 88;
        void Reject(byte[] data) { Sign(data); Throws<ArgumentException>(() => AssetCodec.Decode(data)); }
        foreach (int index in new[] { -1, 1, int.MaxValue }) { var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(record), index); Reject(bad); }
        foreach (int offset in new[] { 12, 24, 36, 48, 60, 72, 84 })
        { var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(record + offset), int.MaxValue); Reject(bad); }
        foreach (var (offset, value) in new[] { (4, 0d), (16, -1d), (28, -1d), (40, double.NaN), (64, 900d), (76, 1.1) })
        { var bad = bytes.ToArray(); BinaryPrimitives.WriteDoubleLittleEndian(bad.AsSpan(record + offset), value); Reject(bad); }
        var downgrade = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8), 34); Reject(downgrade);
        Reject(bytes.Take(record).Concat(bytes.Skip(record + 88)).ToArray());
    }
}
