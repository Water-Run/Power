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

internal static class FuelPhaseEquilibriumAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel phase asset / pressure interval and complete condensation replay / authentic v29", Replay),
        ("fuel phase asset / malformed properties lengths ownership and forged downgrade", Corruption)
    ];
    internal static int FilmOffset(PowerAsset asset, byte[] bytes)
    {
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name), offset = counts + 160;
        foreach (var (index, size) in new (int, int)[] { (0,44),(1,156),(4,116),(5,24),(6,36),(7,72),(8,44),(9,40),(10,20),
            (11,56),(12,28),(13,8),(14,20),(15,28),(16,40),(17,64),(18,32),(19,16),(20,80),(21,68),(22,80),(23,104),(24,40),(25,32),(26,56),(27,56) })
            offset += BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(counts + 4 * index)) * size;
        return offset;
    }
    internal static byte[] Version29(PowerAsset asset, byte[] bytes)
    {
        int count = asset.Components.Count(c => c.FuelFilm is not null), start = FilmOffset(asset, bytes);
        var result = new List<byte>(bytes.Take(start));
        for (int i = 0; i < count; ++i) result.AddRange(bytes.Skip(start + 100 * i).Take(64));
        result.AddRange(bytes.Skip(start + 100 * count));
        var old = result.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(old.AsSpan(8), 29); Sign(old); return old;
    }
    private static void Sign(byte[] bytes) => SHA256.HashData(bytes.AsSpan(0, bytes.Length - 32)).CopyTo(bytes, bytes.Length - 32);
    private static PowerAsset Asset() => PowerAsset.Create(FuelPhaseEquilibriumChecks.Model(wall: 250, vent: 1e-6),
        "Fuel phase equilibrium", new string('c', 64), 400_000_000, 10_000_000, [new(200_000_000, 110, 0)], []);
    private static void Compare(PowerAsset a, PowerAsset b)
    {
        var left = a.CreatePlayback(); var right = b.CreatePlayback(); var lv = new Scalar[a.Model.OutputCount]; var rv = new Scalar[b.Model.OutputCount];
        for (ulong at = 0; at <= a.DurationNanoseconds; at += a.SampleEveryNanoseconds)
        {
            if (at > left.TimeNanoseconds) Require(left.Advance(at - left.TimeNanoseconds) == SimulationStatus.Ok);
            while (right.TimeNanoseconds < at) Require(right.Advance(Math.Min(257 * b.Model.StepNanoseconds, at - right.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(left.ReadSnapshot(lv) == right.ReadSnapshot(rv) && lv.SequenceEqual(rv));
        }
    }
    private static void Replay()
    {
        var asset = Asset(); var bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == AssetCodec.FormatVersion && decoded.Model.HasEquilibriumFuelFilms);
        Require(asset.Components.SequenceEqual(decoded.Components) && bytes.SequenceEqual(AssetCodec.Encode(decoded))); Compare(asset, decoded);
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "vented-tank-liquid-cylinder-v29.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 29 &&
            Convert.ToHexStringLower(SHA256.HashData(fixture)) == "e7f48291b8c9817af7d2eec61b989854d5200130cd4a0987f42e49c21d36d16d");
        var old = AssetCodec.Decode(fixture); Require(!old.Model.HasEquilibriumFuelFilms && old.Model.Fingerprint.ToString("x16") == "3e1af7af1d0de7c7");
        var upgraded = AssetCodec.Encode(old); Require(Version29(old, upgraded).SequenceEqual(fixture)); Compare(old, AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        var asset = Asset(); var bytes = AssetCodec.Encode(asset); int offset = FilmOffset(asset, bytes);
        void Reject(byte[] bad) { Sign(bad); Throws<ArgumentException>(() => AssetCodec.Decode(bad)); }
        foreach (var (field, value) in new[] { (72, (int)Unit.Kelvin), (84, (int)Unit.Pascal), (96, (int)Unit.Kilogram) })
        { var bad = bytes.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(offset + field), value); Reject(bad); }
        foreach (var (field, value) in new[] { (64, -1d), (76, 500d), (88, 1000d) })
        { var bad = bytes.ToArray(); BinaryPrimitives.WriteDoubleLittleEndian(bad.AsSpan(offset + field), value); Reject(bad); }
        var absent = bytes.ToArray(); Array.Clear(absent, offset + 64, 12); Reject(absent);
        Reject(Version29(asset, bytes));
        var truncated = bytes.Take(offset + 88).Concat(bytes.Skip(offset + 100)).ToArray(); Reject(truncated);
    }
}
