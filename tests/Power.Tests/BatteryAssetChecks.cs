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

internal static class BatteryAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("battery asset / complete v13 circuit and duty control / authentic v12 replay", Replay),
        ("battery asset / typed bounded tables / wrong units / missing records / downgrade", Corruption)
    ];
    internal static PressureDutyControllerDefinition Control() => new()
    {
        TargetChannel = 100, SamplePeriodNanoseconds = 10_000_000,
        ProportionalGain = new(1e-6, Unit.FractionPerPascal), IntegralGain = new(1e-7, Unit.FractionPerPascalSecond),
        MinimumDuty = new(0, Unit.Fraction), MaximumDuty = new(1, Unit.Fraction), InitialIntegralDuty = new(.5, Unit.Fraction)
    };
    private static PowerAsset Asset()
    {
        var model = BatteryChecks.MotorModel(); model = model with
        {
            Nodes = [..model.Nodes, NodeDefinition.Hydraulic(4, 1e-12, 1e5)],
            Components = [..model.Components, ComponentDefinition.PressureDutyLoop(20, 4, 105, 1e5, Control())]
        };
        return PowerAsset.Create(model, "Battery control", new string('c', 64), 100_000_000, 10_000_000,
            [new(25_000_000, 105, 2e5), new(75_000_000, 105, 5e4)], []);
    }
    private static void Compare(PowerAsset first, PowerAsset second)
    {
        var a = first.CreatePlayback(); var b = second.CreatePlayback(); var left = new Scalar[a.Model.OutputCount]; var right = new Scalar[b.Model.OutputCount];
        var boundaries = new SortedSet<ulong> { 0, first.DurationNanoseconds };
        for (ulong at = first.SampleEveryNanoseconds; at < first.DurationNanoseconds; at += first.SampleEveryNanoseconds) boundaries.Add(at);
        foreach (var input in first.Inputs) boundaries.Add(input.TimeNanoseconds);
        foreach (ulong at in boundaries)
        {
            if (a.TimeNanoseconds < at) Require(a.Advance(at - a.TimeNanoseconds) == SimulationStatus.Ok);
            while (b.TimeNanoseconds < at) Require(b.Advance(Math.Min(257 * first.Model.StepNanoseconds, at - b.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(a.ReadSnapshot(left) == b.ReadSnapshot(right) && left.SequenceEqual(right));
        }
    }
    private static void Replay()
    {
        var original = Asset(); byte[] bytes = AssetCodec.Encode(original); var decoded = AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8)) == 17);
        Require(original.Components.SequenceEqual(decoded.Components) && original.Nodes.SequenceEqual(decoded.Nodes));
        Require(original.Model.Fingerprint == decoded.Model.Fingerprint && bytes.SequenceEqual(AssetCodec.Encode(decoded))); Compare(original, decoded);
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "pressure-regulated-pump-v12.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(fixture)) == "8e094df27f5e419480232969356731a72dc13c5724b869f0ccb1b2ace98e58e9");
        var old = AssetCodec.Decode(fixture); Require(old.Model.Fingerprint.ToString("x16") == "67e8edb13dc42f42" && !old.Model.HasBatteries);
        Compare(old, AssetCodec.Decode(AssetCodec.Encode(old)));
        var final = old.CreatePlayback(); Require(final.Advance(old.DurationNanoseconds) == SimulationStatus.Ok);
        AssetChecks.Reference(final, (2, Field.Pressure, 200550.7972096, .001), (20, Field.CommandVoltage, .8015751783, 1e-8),
            (20, Field.IntegralVoltage, .8124749429, 1e-8));
        var law = new PressureDutyPiController(1, 1, 0, 1);
        Require(law.TrySample(2, 0, .2, .1, out var sample) && sample.IntegralDuty == .2 && sample.CommandDuty == 1);
        Throws<ArgumentException>(() => new PressureDutyPiController(1, 1, 0, 1.1));
    }
    private static void Corruption()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset);
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name), battery = counts + 112 + 44 * asset.Nodes.Count + 156 * asset.Components.Count, duty = battery + 68;
        void Reject(byte[] bad)
        { SHA256.HashData(bad.AsSpan(0, bad.Length - 32)).CopyTo(bad, bad.Length - 32); Throws<ArgumentException>(() => AssetCodec.Decode(bad)); }
        void Change(int offset, int value)
        { var bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(offset), value); Reject(bad); }
        Change(counts + 84, -1); Change(counts + 84, 33); Change(counts + 84, 0); Change(counts + 88, -1); Change(counts + 88, 65);
        Change(battery, 0); Change(battery, asset.Nodes.Count); Change(battery + 4, 1); Change(battery + 16, (int)Unit.Ohm); Change(battery + 64, (int)Unit.Henry);
        Change(duty, 0); Change(duty + 4, 105); Change(duty + 12, 0); Change(duty + 28, (int)Unit.VoltPerPascal); Change(duty + 52, (int)Unit.Volt);
        var duplicate = bytes.Take(battery + 68).Concat(bytes.Skip(battery).Take(68)).Concat(bytes.Skip(battery + 68)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts + 84), 2); Reject(duplicate);
        var missing = bytes.Take(battery).Concat(bytes.Skip(battery + 68)).ToArray(); BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts + 84), 0); Reject(missing);
        var downgrade = bytes.Take(counts + 84).Concat(bytes.Skip(counts + 112).Take(battery - counts - 112)).Concat(bytes.Skip(duty + 80)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8), 12); Reject(downgrade);
        var isolated = PowerAsset.Create(BatteryChecks.LoadModel() with { Components = [] }, "Isolated battery", new string('a', 64), 100_000_000, 10_000_000, [], []);
        Require(AssetCodec.Decode(AssetCodec.Encode(isolated)).Model.HasBatteries);
    }
}
