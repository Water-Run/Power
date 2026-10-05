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

internal static class SolenoidAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("needle asset / v20 magnetic stop opening driver / every boundary / authentic v19", Replay),
        ("needle asset / bounded typed tables / units ownership duplicates missing and downgrade", Corruption)
    ];
    private static PowerAsset Asset(ModelDefinition? model = null) => PowerAsset.Create(model ?? SolenoidChecks.NeedleModel(),
        "Electromagnetic needle", new string('d', 64), 30_000_000, 500_000, [], []);
    private static void Compare(PowerAsset a, PowerAsset b)
    {
        var left = a.CreatePlayback(); var right = b.CreatePlayback(); var lv = new Scalar[a.Model.OutputCount]; var rv = new Scalar[b.Model.OutputCount];
        var times = new SortedSet<ulong> { 0, a.DurationNanoseconds };
        for (ulong at = a.SampleEveryNanoseconds; at < a.DurationNanoseconds; at += a.SampleEveryNanoseconds) times.Add(at);
        foreach (var input in a.Inputs) times.Add(input.TimeNanoseconds);
        foreach (ulong at in times)
        {
            if (at > left.TimeNanoseconds) Require(left.Advance(at - left.TimeNanoseconds) == SimulationStatus.Ok);
            while (right.TimeNanoseconds < at) Require(right.Advance(Math.Min(257 * a.Model.StepNanoseconds, at - right.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(left.ReadSnapshot(lv) == right.ReadSnapshot(rv) && lv.SequenceEqual(rv));
        }
    }
    private static void Replay()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == 25 && decoded.Model.HasNeedleInjectors && decoded.Model.HasSolenoids);
        Require(asset.Components.SequenceEqual(decoded.Components) && bytes.SequenceEqual(AssetCodec.Encode(decoded))); Compare(asset, decoded);
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "liquid-injected-cylinder-v19.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 19 && Convert.ToHexStringLower(SHA256.HashData(fixture)) == "4ff5f6180006d53be5ffb6ef0663cc6de4af45f35f39dc4d52fd7e00e8cdd8c5");
        var old = AssetCodec.Decode(fixture); Require(old.Model.Fingerprint.ToString("x16") == "4f74c6da6d89ab08" && !old.Model.HasNeedleInjectors);
        Compare(old, AssetCodec.Decode(AssetCodec.Encode(old)));
        var playback = old.CreatePlayback(); Require(playback.Advance(old.DurationNanoseconds) == SimulationStatus.Ok);
        AssetChecks.Reference(playback, (17, Field.TotalFuelDelivered, 28e-6, 1e-15), (17, Field.Pressure, 725333.3333333334, 1e-6));
    }
    private static void Corruption()
    {
        void Reject(byte[] bad)
        {
            SHA256.HashData(bad.AsSpan(0, bad.Length - 32)).CopyTo(bad, bad.Length - 32);
            Throws<ArgumentException>(() => AssetCodec.Decode(bad));
        }
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset); int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name);
        int coil = counts + 148 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 24 + 36 + 40 + 64 + 120;
        int stop = coil + 28, needle = stop + 40, driver = needle + 32;
        foreach (int field in new[] { 120, 124, 128, 132 })
        {
            var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(counts + field), int.MaxValue); Reject(bad);
        }
        foreach (var edit in new[] { (coil, 0), (coil + 24, (int)Unit.Henry), (stop + 12, (int)Unit.Kelvin),
            (needle + 4, 3), (driver + 4, 11), (driver + 8, 21) })
        {
            var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(edit.Item1), edit.Item2); Reject(bad);
        }
        foreach (var record in new[] { (coil, 28, 120), (stop, 40, 124), (needle, 32, 128), (driver, 40, 132) })
        {
            var missing = bytes.Take(record.Item1).Concat(bytes.Skip(record.Item1 + record.Item2)).ToArray();
            BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts + record.Item3), 0); Reject(missing);
        }
        var downgrade = bytes.Take(counts + 120).Concat(bytes.Skip(counts + 148).Take(coil - counts - 148)).Concat(bytes.Skip(driver + 40)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8), 19); Reject(downgrade);
        var baseModel = SolenoidChecks.Model(); var two = Asset(baseModel with { Components = [..baseModel.Components,
            ComponentDefinition.SolenoidCoil(12, 1, 2, 101, 4, .002, 8)] });
        var pair = AssetCodec.Encode(two); int pairCounts = 78 + Encoding.UTF8.GetByteCount(two.Name);
        int first = pairCounts + 140 + 44 * two.Nodes.Count + 156 * two.Components.Count;
        var duplicate = pair.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(first + 28), 1); Reject(duplicate);
    }
}
