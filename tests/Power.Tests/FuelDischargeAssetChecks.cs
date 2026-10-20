// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class FuelDischargeAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel discharge asset / finite density frozen transit needle histories and authentic v33 replay", Replay),
        ("fuel discharge asset / bounded typed records phase mode density needle and downgrade rejection", Corruption)
    ];
    private static PowerAsset Asset(bool needle = false) => PowerAsset.Create(FuelDischargeChecks.Model(needle: needle), "Finite fuel discharge", new string('a', 64),
        10_000_000, 1_000_000, [new(5_000_000, 100, 0), new(7_000_000, 100, 1)], []);
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
        foreach (bool needle in new[] { false, true })
        {
            var original = Asset(needle); var bytes = AssetCodec.Encode(original); var decoded = AssetCodec.Decode(bytes);
            Require(decoded.Model.HasFiniteFuelReceivers && decoded.Model.HasFuelDischarge && bytes.SequenceEqual(AssetCodec.Encode(decoded)));
            Require(original.Components.SequenceEqual(decoded.Components)); Compare(original, decoded);
        }
        var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "equilibrium-fuel-pump-v33.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 33 && Convert.ToHexStringLower(SHA256.HashData(fixture)) == "3e3804c7277baea26c330c47a319669c5f61142486fb73f127679e56192fe9ba");
        var old = AssetCodec.Decode(fixture); Require(old.Model.HasFuelChambers && !old.Model.HasFuelDischarge && old.Model.Fingerprint.ToString("x16") == "30f8057b21ff5860");
        var upgraded = AssetCodec.Encode(old); var back = upgraded.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(back.AsSpan(8), 33); Sign(back); Require(back.SequenceEqual(fixture));
        Compare(old, AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        var asset = Asset(); var bytes = AssetCodec.Encode(asset);
        int discharge = bytes.Length - 32 - 24 * asset.Inputs.Count - 52, film = discharge - 148 - 112;
        void Reject(byte[] bad) { Sign(bad); Throws<ArgumentException>(() => AssetCodec.Decode(bad)); }
        foreach (int offset in new[] { discharge, discharge + 4, discharge + 8, discharge + 20, discharge + 24, film + 108 })
        { var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(offset), int.MaxValue); Reject(bad); }
        foreach (double value in new[] { double.NaN, -1d, 2d })
        { var bad = bytes.ToArray(); BinaryPrimitives.WriteDoubleLittleEndian(bad.AsSpan(discharge + 12), value); Reject(bad); }
        var density = bytes.ToArray(); BinaryPrimitives.WriteDoubleLittleEndian(density.AsSpan(film + 100), 0); Reject(density);
        var orphan = bytes.ToArray(); BinaryPrimitives.WriteDoubleLittleEndian(orphan.AsSpan(discharge + 28), .001); Reject(orphan);
        var downgrade = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8), 33); Reject(downgrade);
        Reject(bytes.Take(discharge).Concat(bytes.Skip(discharge + 52)).ToArray());
    }
}
