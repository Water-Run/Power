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

internal static class DctControllerAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("DCT controller asset / complete v22 routes timings fault state / replay / authentic v21", Replay),
        ("DCT controller asset / bounded typed ownership period units and forged downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(DctControllerChecks.Model(), "Sampled DCT", new string('f', 64), 300_000_000, 10_000_000, [], []);
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
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == AssetCodec.FormatVersion && decoded.Model.Fidelity == "sampled_dual_clutch_control");
        Require(bytes.SequenceEqual(AssetCodec.Encode(decoded)));
        var controller = decoded.Components.Single(c => c.Kind == ComponentKind.DualClutchController).DualClutchController!;
        Require(controller.Selectors.SequenceEqual(Enumerable.Range(300, 8).Select(i => (uint)i)) && controller.SamplePeriodNanoseconds == 1_000_000);
        Compare(asset, decoded);
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "dual-clutch-transmission-v21.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8)) == 21 && Convert.ToHexStringLower(SHA256.HashData(fixture)) == "706618f7d32a5ef4b8a18ca801d2c3ba0b293eac03b584d88d584e9290a38437");
        var old = AssetCodec.Decode(fixture); Require(old.Model.Fingerprint.ToString("x16") == "7466a75b99fbfd78"); Compare(old, AssetCodec.Decode(AssetCodec.Encode(old)));
    }
    private static void Corruption()
    {
        var asset = Asset(); byte[] source = AssetCodec.Encode(asset); int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name);
        int record = counts + 160 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 28 * 10 + 8 * 12;
        void Reject(byte[] data) { SHA256.HashData(data.AsSpan(0, data.Length - 32)).CopyTo(data, data.Length - 32); Throws<ArgumentException>(() => AssetCodec.Decode(data)); }
        foreach (int count in new[] { -1, 65, int.MaxValue }) { var bad = source.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(counts + 136), count); Reject(bad); }
        foreach (var edit in new[] { (record, 0), (record + 8, 401), (record + 20, 300), (record + 88, (int)Unit.NewtonMeter) })
        { var bad = source.ToArray(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(edit.Item1), edit.Item2); Reject(bad); }
        var period = source.ToArray(); BinaryPrimitives.WriteUInt64LittleEndian(period.AsSpan(record + 48), 99999); Reject(period);
        var missing = source.Take(record).Concat(source.Skip(record + 104)).ToArray(); BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts + 136), 0); Reject(missing);
        var down = source.Take(counts + 136).Concat(source.Skip(counts + 160).Take(record - counts - 160)).Concat(source.Skip(record + 104)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(down.AsSpan(8), 21); Reject(down);
    }
}
