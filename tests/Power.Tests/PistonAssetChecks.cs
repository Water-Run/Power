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

internal static class PistonAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("piston asset / complete v18 stroke/contact topology / authentic v13 replay", Replay),
        ("piston asset / bounded records / malformed forces / coverage / downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(PistonChecks.ClutchModel(), "Dynamic clutch", new string('d', 64),
        300_000_000, 10_000_000, [new(200_000_000, 100, 0), new(200_000_000, 101, 1)], []);
    private static void Compare(PowerAsset original, PowerAsset decoded)
    {
        var a = original.CreatePlayback(); var b = decoded.CreatePlayback(); var left = new Scalar[a.Model.OutputCount]; var right = new Scalar[b.Model.OutputCount];
        var boundaries = new SortedSet<ulong> { 0, original.DurationNanoseconds };
        for (ulong at = original.SampleEveryNanoseconds; at < original.DurationNanoseconds; at += original.SampleEveryNanoseconds) boundaries.Add(at);
        foreach (var input in original.Inputs) boundaries.Add(input.TimeNanoseconds);
        foreach (ulong at in boundaries)
        {
            if (a.TimeNanoseconds < at) Require(a.Advance(at - a.TimeNanoseconds) == SimulationStatus.Ok);
            while (b.TimeNanoseconds < at) Require(b.Advance(Math.Min(257 * original.Model.StepNanoseconds, at - b.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(a.ReadSnapshot(left) == b.ReadSnapshot(right) && left.SequenceEqual(right));
        }
    }
    private static void Replay()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == 26);
        Require(asset.Nodes.SequenceEqual(decoded.Nodes) && asset.Components.SequenceEqual(decoded.Components));
        Require(asset.Model.Fingerprint == decoded.Model.Fingerprint && bytes.SequenceEqual(AssetCodec.Encode(decoded))); Compare(asset, decoded);
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "battery-regulated-pump-v13.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(fixture)) == "67e7bde4dfcc2b8b41f49cadae8f89ff5fc5a1668059e084b7b5d9da02c7c44a");
        var old = AssetCodec.Decode(fixture); Require(old.Model.Fingerprint.ToString("x16") == "40d4fcbab9cad8f8" && !old.Model.HasPistons);
        Compare(old, AssetCodec.Decode(AssetCodec.Encode(old)));
        var final = old.CreatePlayback(); Require(final.Advance(old.DurationNanoseconds) == SimulationStatus.Ok);
        AssetChecks.Reference(final, (2, Field.Pressure, 200828.2935823, .001), (30, Field.StateOfCharge, .6269678451, 1e-9),
            (20, Field.CommandDuty, .0629221114, 1e-8));
    }
    private static void Corruption()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset);
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name), piston = counts + 152 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 40 * 2, clutch = piston + 104;
        void Reject(byte[] bad)
        { SHA256.HashData(bad.AsSpan(0, bad.Length - 32)).CopyTo(bad, bad.Length - 32); Throws<ArgumentException>(() => AssetCodec.Decode(bad)); }
        void Change(int offset, int value)
        { var bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(offset), value); Reject(bad); }
        Change(counts + 92, -1); Change(counts + 92, 65); Change(counts + 92, 0); Change(counts + 96, -1); Change(counts + 96, 65);
        Change(piston, 1); Change(piston, asset.Components.Count); Change(piston + 16, (int)Unit.Meter); Change(piston + 88, (int)Unit.Pascal);
        Change(clutch, 0); Change(clutch + 4, 11); Change(clutch + 16, (int)Unit.Newton); Change(clutch + 36, 129);
        var duplicate = bytes.Take(piston + 104).Concat(bytes.Skip(piston).Take(104)).Concat(bytes.Skip(piston + 104)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts + 92), 2); Reject(duplicate);
        var missing = bytes.Take(piston).Concat(bytes.Skip(piston + 104)).ToArray(); BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts + 92), 0); Reject(missing);
        var downgrade = bytes.Take(counts + 92).Concat(bytes.Skip(counts + 152).Take(piston - counts - 152)).Concat(bytes.Skip(clutch + 40)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8), 13); Reject(downgrade);
    }
}
