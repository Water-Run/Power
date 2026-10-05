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

internal static class SpoolAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("spool asset / complete v18 metering topology / authentic v14 upgrade", Replay),
        ("spool asset / typed counts and geometry / corruption and downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(SpoolChecks.Model(), "Spool regulator", new string('e', 64),
        100_000_000, 10_000_000, [new(50_000_000, 100, .5)], [new(0, Field.HydraulicVolumeResidual, null, null, 1e-16)]);
    private static void Compare(PowerAsset old, PowerAsset updated)
    {
        var a = old.CreatePlayback(); var b = updated.CreatePlayback(); var left = new Scalar[old.Model.OutputCount]; var right = new Scalar[updated.Model.OutputCount];
        var boundaries = new SortedSet<ulong> { 0, old.DurationNanoseconds };
        for (ulong at = old.SampleEveryNanoseconds; at < old.DurationNanoseconds; at += old.SampleEveryNanoseconds) boundaries.Add(at);
        foreach (var input in old.Inputs) boundaries.Add(input.TimeNanoseconds);
        foreach (ulong at in boundaries)
        {
            if (at > a.TimeNanoseconds) Require(a.Advance(at - a.TimeNanoseconds) == SimulationStatus.Ok);
            while (b.TimeNanoseconds < at) Require(b.Advance(Math.Min(257 * old.Model.StepNanoseconds, at - b.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(a.ReadSnapshot(left) == b.ReadSnapshot(right) && left.SequenceEqual(right));
        }
    }
    private static void Replay()
    {
        var asset = Asset(); byte[] data = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(data);
        Require(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8)) == 25 && decoded.Model.Fidelity == "mechanically_regulated_hydraulics");
        Require(asset.Components.SequenceEqual(decoded.Components) && data.SequenceEqual(AssetCodec.Encode(decoded))); Compare(asset, decoded);
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "piston-actuated-clutch-v14.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(fixture)) == "72e0605d00972a58e2f5358aa8cbd44417e81cf1c452a20ab9661eac0919bae0");
        var original = AssetCodec.Decode(fixture); Require(original.Model.Fingerprint.ToString("x16") == "46f746398142c258" && !original.Model.HasSpoolValves);
        Compare(original, AssetCodec.Decode(AssetCodec.Encode(original)));
        var final = original.CreatePlayback(); Require(final.Advance(original.DurationNanoseconds) == SimulationStatus.Ok);
        AssetChecks.Reference(final, 5e-6, (40, Field.Displacement, .0021772797986273195, 1e-11), (17, Field.ClampForce, 177.27979862731945, 1e-5));
    }
    private static void Corruption()
    {
        var asset = Asset(); byte[] source = AssetCodec.Encode(asset);
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name), land = counts + 148 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 80 + 104;
        void Reject(byte[] data) { SHA256.HashData(data.AsSpan(0, data.Length - 32)).CopyTo(data, data.Length - 32); Throws<ArgumentException>(() => AssetCodec.Decode(data)); }
        foreach (int count in new[] { -1, 65, int.MaxValue }) { var bad = source.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(counts + 100), count); Reject(bad); }
        foreach (var replacement in new[] { (0, 0), (4, 999), (16, (int)Unit.Radian) })
        { var bad = source.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(land + replacement.Item1), replacement.Item2); Reject(bad); }
        var duplicate = source.Take(land + 32).Concat(source.Skip(land).Take(32)).Concat(source.Skip(land + 32)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts + 100), 2); Reject(duplicate);
        var missing = source.Take(land).Concat(source.Skip(land + 32)).ToArray(); BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts + 100), 0); Reject(missing);
        var downgraded = source.Take(counts + 100).Concat(source.Skip(counts + 148).Take(land - counts - 148)).Concat(source.Skip(land + 32)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(downgraded.AsSpan(8), 14); Reject(downgraded);
    }
}
