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

internal static class CombustionAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("combustion asset / mixtures and burn replay / atomic playback / authentic v5 fixture", Replay),
        ("combustion asset / v6 bounds / wrong and duplicate extensions / missing semantics", Corruption)
    ];
    private static PowerAsset Asset()
    {
        var model = new ModelDefinition
        {
            StepNanoseconds = 50_000,
            Nodes = [NodeDefinition.Rotor(1, .2, 50), CombustionChecks.Reactive(NodeDefinition.CylinderGas(2, 1e5, 300)),
                CombustionChecks.Reactive(NodeDefinition.GasVolume(3, .002, 2e5, 400), .02, .8)],
            Components = [ComponentDefinition.GasCylinder(10, 1, 2, MovingCylinderChecks.Geometry),
                ComponentDefinition.GasOrifice(11, 2, 3, 4e-6, .8, 100) with { ValveTiming = ValveTimingChecks.Timing },
                ComponentDefinition.GasReservoir(12, 3, 2e-6, 1e5, 300, .8) with { ReservoirFractions = new(.04, .96) },
                ComponentDefinition.PremixedCombustion(13, 1, 2, CombustionChecks.Burn, 101)]
        };
        return PowerAsset.Create(model, "Premixed model", new string('f', 64), 100_000_000, 10_000_000,
            [new(20_050_000, 101, .5), new(40_050_000, 101, 1)], [new(0, Field.EnergyResidual, null, null, 1e-6)]);
    }
    private static void Replay()
    {
        var asset = Asset(); byte[] bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(decoded.Nodes.SequenceEqual(asset.Nodes) && decoded.Components.SequenceEqual(asset.Components));
        Require(decoded.Model.Fingerprint == asset.Model.Fingerprint && AssetCodec.Encode(decoded).SequenceEqual(bytes));
        var a = asset.CreatePlayback(); var b = decoded.CreatePlayback(); var values = new Scalar[a.Model.OutputCount];
        for (int i = 1; i <= 10; ++i)
        {
            Require(a.Advance(10_000_000) == SimulationStatus.Ok);
            ulong at = (ulong)i * 10_000_000;
            while (b.TimeNanoseconds < at) Require(b.Advance(Math.Min(1_350_000, at - b.TimeNanoseconds)) == SimulationStatus.Ok);
            Require(a.ReadSnapshot(values) == b.ReadSnapshot(new Scalar[b.Model.OutputCount]));
        }
        var failed = PowerAsset.Create(asset.CopyDefinition(), "Atomic burn", new string('a', 64), 100_000_000, 10_000_000,
            [new(20_000_000, 101, 0), new(40_000_000, 100, 1)], []);
        var playback = failed.CreatePlayback(); var before = playback.ReadSnapshot(values);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        Require(playback.Advance(100_000_000, cancel.Token) == SimulationStatus.Cancelled && playback.ReadSnapshot(values) == before);
        Require(playback.Advance(100_000_000) == SimulationStatus.Ok);
        var reference = failed.CreatePlayback(); Require(reference.Advance(100_000_000) == SimulationStatus.Ok);
        Require(playback.ReadSnapshot(values) == reference.ReadSnapshot(values));

        byte[] oldBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "crank-timed-cylinder-v5.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(oldBytes)) == "e13c1b90f1192ae21a78f80fc30af85236d165adf702ad65ce242677012c7ac7");
        var old = AssetCodec.Decode(oldBytes); Require(old.Model.Fingerprint.ToString("x16") == "38f0437eac4def69");
        var upgraded = AssetCodec.Decode(AssetCodec.Encode(old)); var oldRun = old.CreatePlayback(); var newRun = upgraded.CreatePlayback();
        Require(oldRun.Advance(old.DurationNanoseconds) == SimulationStatus.Ok && newRun.Advance(old.DurationNanoseconds) == SimulationStatus.Ok);
        Require(oldRun.ReadSnapshot(new Scalar[old.Model.OutputCount]) == newRun.ReadSnapshot(new Scalar[old.Model.OutputCount]));
    }
    private static void Corruption()
    {
        var asset = Asset(); byte[] source = AssetCodec.Encode(asset);
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name);
        int mixtures = counts + 80 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 24 * 2 + 36 * 2 + 72 + 44;
        int reservoirs = mixtures + 40 * 2, burners = reservoirs + 20;
        void Reject(byte[] bytes)
        {
            SHA256.HashData(bytes.AsSpan(0, bytes.Length - 32)).CopyTo(bytes, bytes.Length - 32);
            Throws<ArgumentException>(() => AssetCodec.Decode(bytes));
        }
        void ChangeInt(int offset, int value)
        { var bytes = (byte[])source.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(offset), value); Reject(bytes); }
        ChangeInt(counts + 36, -1); ChangeInt(counts + 40, int.MaxValue); ChangeInt(counts + 44, 0);
        ChangeInt(mixtures, 0); ChangeInt(mixtures + 40, 1); ChangeInt(reservoirs, 1); ChangeInt(burners, 0);
        void Remove(int offset, int size, int countOffset, int remaining)
        {
            var bytes = source.Take(offset).Concat(source.Skip(offset + size)).ToArray();
            BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(countOffset), remaining); Reject(bytes);
        }
        Remove(mixtures, 40, counts + 36, 1); Remove(reservoirs, 20, counts + 40, 0); Remove(burners, 56, counts + 44, 0);
        var duplicate = source.Take(burners + 56).Concat(source.Skip(burners).Take(56)).Concat(source.Skip(burners + 56)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts + 44), 2); Reject(duplicate);
        var oldVersion = source.Take(counts + 36).Concat(source.Skip(counts + 80).Take(mixtures - counts - 80)).Concat(source.Skip(burners + 56)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(oldVersion.AsSpan(8), 5); Reject(oldVersion);
    }
}
