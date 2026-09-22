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

internal static class GearAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("gear asset / three-port topology / every replay boundary / authentic v7 fixture", Replay),
        ("gear asset / v11 counts / wrong ports / missing and duplicate topology / downgrade", Corruption)
    ];
    private static PowerAsset Asset()
    {
        var d = GearModelChecks.PlanetaryModel();
        d = d with { Nodes = [.. d.Nodes, NodeDefinition.Rotor(4, .7, 5)], Components = [.. d.Components, ComponentDefinition.IdealGear(15, 3, 4, 3)] };
        return PowerAsset.Create(d, "Gear replay", new string('a', 64), 500_000_000, 10_000_000,
            [new(200_000_000, 101, 5), new(300_000_000, 103, -4)], [new(10, Field.SlipSpeed, null, null, 1e-8), new(15, Field.ConstraintError, null, null, 1e-8)]);
    }
    private static void Replay()
    {
        var original = Asset(); var bytes = AssetCodec.Encode(original); var decoded = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == 11);
        Require(decoded.Components.SequenceEqual(original.Components) && decoded.Model.Fingerprint == original.Model.Fingerprint);
        Require(AssetCodec.Encode(decoded).SequenceEqual(bytes));
        var a = original.CreatePlayback(); var b = decoded.CreatePlayback(); var values = new Scalar[a.Model.OutputCount];
        for (int i = 0; i < 50; ++i)
        {
            Require(a.Advance(10_000_000) == SimulationStatus.Ok);
            for (int j = 0; j < 10; ++j) Require(b.Advance(1_000_000) == SimulationStatus.Ok);
            Require(a.ReadSnapshot(values) == b.ReadSnapshot(new Scalar[b.Model.OutputCount]));
        }
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "fired-clutch-v7.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(fixture)) == "9a670efbbc4046f87be8ee7b20f6fee6823ce5cff982002f619ced5722fc324d");
        var old = AssetCodec.Decode(fixture); Require(old.Model.Fingerprint.ToString("x16") == "197be44884deee90");
        var upgraded = AssetCodec.Decode(AssetCodec.Encode(old)); var first = old.CreatePlayback(); var second = upgraded.CreatePlayback();
        Require(first.Advance(old.DurationNanoseconds) == SimulationStatus.Ok && second.Advance(old.DurationNanoseconds) == SimulationStatus.Ok);
        Require(first.ReadSnapshot(new Scalar[old.Model.OutputCount]) == second.ReadSnapshot(new Scalar[old.Model.OutputCount]));
        Require(first.ReadSnapshot(new Scalar[old.Model.OutputCount]).StateHash.ToString("x16") == "28bf5335d8e35cde");
    }
    private static void Corruption()
    {
        var asset = Asset(); var bytes = AssetCodec.Encode(asset);
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name);
        int extension = counts + 80 + 44 * asset.Nodes.Count + 156 * asset.Components.Count;
        void Reject(byte[] bad)
        {
            SHA256.HashData(bad.AsSpan(0, bad.Length - 32)).CopyTo(bad, bad.Length - 32);
            Throws<ArgumentException>(() => AssetCodec.Decode(bad));
        }
        void Change(int offset, int value)
        { var bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(offset), value); Reject(bad); }
        Change(counts + 52, -1); Change(counts + 52, 65); Change(counts + 52, 0);
        Change(extension, -1); Change(extension, asset.Components.Count); Change(extension, 1);
        Change(extension + 4, 0); Change(extension + 4, 2); Change(extension + 4, 99);
        Change(extension + 8, 0); Change(extension + 12, 1);
        var missing = bytes.Take(extension).Concat(bytes.Skip(extension + 8)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts + 52), 1); Reject(missing);
        var duplicate = bytes.Take(extension + 8).Concat(bytes.Skip(extension).Take(8)).Concat(bytes.Skip(extension + 8)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts + 52), 3); Reject(duplicate);
        var downgraded = bytes.Take(counts + 52).Concat(bytes.Skip(counts + 80).Take(extension - counts - 80)).Concat(bytes.Skip(extension + 16)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(downgraded.AsSpan(8), 7); Reject(downgraded);
    }
}
