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

internal static class PressureControllerAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("pressure control asset / complete v12 contract / every boundary / authentic v11", Replay),
        ("pressure control asset / bounded records / ownership / wrong units / downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(PressureControllerChecks.Plant(), "Pressure control", new string('a', 64),
        100_000_000, 10_000_000, [new(25_000_000, 105, 4e5), new(75_000_000, 105, 1e5)], []);
    private static void Compare(PowerAsset original, PowerAsset upgraded)
    {
        var first = original.CreatePlayback(); var second = upgraded.CreatePlayback();
        var times = new SortedSet<ulong> { 0, original.DurationNanoseconds };
        for (ulong at = original.SampleEveryNanoseconds; at < original.DurationNanoseconds; at += original.SampleEveryNanoseconds) times.Add(at);
        foreach (var input in original.Inputs) times.Add(input.TimeNanoseconds);
        var left = new Scalar[first.Model.OutputCount]; var right = new Scalar[second.Model.OutputCount];
        foreach (ulong at in times)
        {
            if (at > first.TimeNanoseconds) Require(first.Advance(at - first.TimeNanoseconds) == SimulationStatus.Ok);
            while (second.TimeNanoseconds < at)
                Require(second.Advance(Math.Min(257 * upgraded.Model.StepNanoseconds, at - second.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(first.ReadSnapshot(left) == second.ReadSnapshot(right) && left.SequenceEqual(right));
        }
    }
    private static void Replay()
    {
        var original = Asset(); byte[] bytes = AssetCodec.Encode(original); var decoded = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == 24);
        Require(original.Components.SequenceEqual(decoded.Components) && original.Nodes.SequenceEqual(decoded.Nodes));
        Require(original.Model.Fingerprint == decoded.Model.Fingerprint && bytes.SequenceEqual(AssetCodec.Encode(decoded)));
        Compare(original, decoded);
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "fired-pump-v11.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(fixture)) == "6c5616c7da077377d02cc76a36475fa3aec9163cf7b39c90b4dbeca54a05f0d3");
        var old = AssetCodec.Decode(fixture); Require(old.Model.Fingerprint.ToString("x16") == "d0bd8f29a706fd89" && !old.Model.HasPressureControllers);
        Compare(old, AssetCodec.Decode(AssetCodec.Encode(old)));
        var final = old.CreatePlayback(); Require(final.Advance(old.DurationNanoseconds) == SimulationStatus.Ok);
        AssetChecks.Reference(final, (1, Field.Speed, 69.7555356890, 1e-6), (33, Field.Pressure, 1069726.2404, .001),
            (46, Field.HydraulicWork, 53.9425016232, 1e-7), (0, Field.HydraulicWork, 0, 0));
    }
    private static void Corruption()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset);
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name), extension = counts + 140 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 40 + 32;
        void Reject(byte[] bad)
        { SHA256.HashData(bad.AsSpan(0, bad.Length - 32)).CopyTo(bad, bad.Length - 32); Throws<ArgumentException>(() => AssetCodec.Decode(bad)); }
        void Change(int offset, int value)
        { var bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(offset), value); Reject(bad); }
        Change(counts + 80, -1); Change(counts + 80, 65); Change(counts + 80, 0);
        Change(extension, -1); Change(extension, 0); Change(extension, asset.Components.Count);
        Change(extension + 4, 105); Change(extension + 12, 0); Change(extension + 12, 1);
        Change(extension + 28, (int)Unit.Volt); Change(extension + 40, (int)Unit.VoltPerPascal);
        var duplicate = bytes.Take(extension + 80).Concat(bytes.Skip(extension).Take(80)).Concat(bytes.Skip(extension + 80)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts + 80), 2); Reject(duplicate);
        var missing = bytes.Take(extension).Concat(bytes.Skip(extension + 80)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts + 80), 0); Reject(missing);
        var downgrade = bytes.Take(counts + 80).Concat(bytes.Skip(counts + 140).Take(extension - counts - 140)).Concat(bytes.Skip(extension + 80)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8), 11); Reject(downgrade);
        Throws<ArgumentException>(() => PowerAsset.Create(PressureControllerChecks.Plant(), "Owned voltage", new string('b', 64),
            100_000_000, 10_000_000, [new(25_000_000, 100, 0)], []));
    }
}
