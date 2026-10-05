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

internal static class LiquidFuelInjectorAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("liquid injector asset / v19 finite supply and timing / complete replay / authentic v18", Replay),
        ("liquid injector asset / bounded typed records / units ownership duplicates missing and downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(LiquidFuelInjectorChecks.Model(), "Finite liquid rail",
        new string('c', 64), 200_000_000, 10_000_000, [new(60_000_000, 102, 12e-6)], []);
    private static void Compare(PowerAsset original, PowerAsset upgraded)
    {
        var a = original.CreatePlayback(); var b = upgraded.CreatePlayback();
        var left = new Scalar[a.Model.OutputCount]; var right = new Scalar[b.Model.OutputCount];
        var times = new SortedSet<ulong> { 0, original.DurationNanoseconds };
        for (ulong at = original.SampleEveryNanoseconds; at < original.DurationNanoseconds; at += original.SampleEveryNanoseconds) times.Add(at);
        foreach (var input in original.Inputs) times.Add(input.TimeNanoseconds);
        foreach (ulong at in times)
        {
            if (at > a.TimeNanoseconds) Require(a.Advance(at - a.TimeNanoseconds) == SimulationStatus.Ok);
            while (b.TimeNanoseconds < at) Require(b.Advance(Math.Min(257 * original.Model.StepNanoseconds, at - b.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(a.ReadSnapshot(left) == b.ReadSnapshot(right) && left.SequenceEqual(right));
        }
    }
    private static void Replay()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == 26 && decoded.Model.HasLiquidFuelInjectors);
        Require(asset.Components.SequenceEqual(decoded.Components) && bytes.SequenceEqual(AssetCodec.Encode(decoded)));
        Compare(asset, decoded);
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "film-fired-cylinder-v18.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 18 &&
            Convert.ToHexStringLower(SHA256.HashData(fixture)) == "c0e59c6f6527b2b59ee1094cd6627c0829ecbd4ab0eb2e6ec06394ccfe15da97");
        var old = AssetCodec.Decode(fixture); Require(old.Model.Fingerprint.ToString("x16") == "cb103bce098f4e82" && !old.Model.HasLiquidFuelInjectors);
        Compare(old, AssetCodec.Decode(AssetCodec.Encode(old)));
        var final = old.CreatePlayback(); Require(final.Advance(old.DurationNanoseconds) == SimulationStatus.Ok);
        AssetChecks.Reference(final, (16, Field.EvaporatedFuelMass, 40e-6, 1e-15), (16, Field.FilmWallHeat, 20, 1e-8));
    }
    private static void Corruption()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset);
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name);
        int record = counts + 152 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 24 + 36 + 40 + 64;
        void Reject(byte[] bad)
        {
            SHA256.HashData(bad.AsSpan(0, bad.Length - 32)).CopyTo(bad, bad.Length - 32);
            Throws<ArgumentException>(() => AssetCodec.Decode(bad));
        }
        foreach (int count in new[] { -1, 65, int.MaxValue })
        {
            var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(counts + 116), count); Reject(bad);
        }
        foreach (var change in new[] { (0, 0), (4, 11), (8, 3), (92, (int)Unit.Kilogram), (116, (int)Unit.Joule) })
        {
            var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(record + change.Item1), change.Item2); Reject(bad);
        }
        var duplicate = bytes.Take(record + 120).Concat(bytes.Skip(record).Take(120)).Concat(bytes.Skip(record + 120)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts + 116), 2); Reject(duplicate);
        var missing = bytes.Take(record).Concat(bytes.Skip(record + 120)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts + 116), 0); Reject(missing);
        var downgrade = bytes.Take(counts + 116).Concat(bytes.Skip(counts + 152).Take(record - counts - 152)).Concat(bytes.Skip(record + 120)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8), 18); Reject(downgrade);
    }
}
