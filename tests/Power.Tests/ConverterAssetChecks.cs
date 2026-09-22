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

internal static class ConverterAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("converter asset / four maps / replay / authentic v8 fingerprint and state", Replay),
        ("converter asset / bounded counts / duplicate and missing maps / downgrade rejection", Corruption)
    ];
    private static PowerAsset Asset()
    {
        var d = ConverterChecks.Model(multiply: true);
        return PowerAsset.Create(d with { Components = [..d.Components, ComponentDefinition.TorqueConverter(12, 2, 1, ConverterChecks.Maps())] },
            "Converter replay", new string('b',64), 500_000_000, 10_000_000, [new(200_000_000,101,10)], [new(10,Field.FluidHeat,1,null,null)]);
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
        byte[] fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","fired-planetary-v8.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(fixture)) == "6872f857bc521ed114d6eea5837cbb380a01f374ddfe7e829b30afa2c44fe0aa");
        var old = AssetCodec.Decode(fixture); Require(old.Model.Fingerprint.ToString("x16") == "6703f00c995e6b62");
        var upgraded = AssetCodec.Decode(AssetCodec.Encode(old)); var first = old.CreatePlayback(); var second = upgraded.CreatePlayback();
        for (ulong at = 0; at < old.DurationNanoseconds; at += old.SampleEveryNanoseconds)
        {
            Require(first.Advance(old.SampleEveryNanoseconds) == SimulationStatus.Ok && second.Advance(old.SampleEveryNanoseconds) == SimulationStatus.Ok);
            Require(first.ReadSnapshot(new Scalar[old.Model.OutputCount]) == second.ReadSnapshot(new Scalar[old.Model.OutputCount]));
        }
        Require(first.ReadSnapshot(new Scalar[old.Model.OutputCount]).StateHash.ToString("x16") == "b328de221532fbae");
    }
    private static void Corruption()
    {
        var asset=Asset(); var bytes=AssetCodec.Encode(asset);
        int counts=78+Encoding.UTF8.GetByteCount(asset.Name), extension=counts+80+44*asset.Nodes.Count+156*asset.Components.Count;
        void Reject(byte[] bad)
        {
            SHA256.HashData(bad.AsSpan(0,bad.Length-32)).CopyTo(bad,bad.Length-32);
            Throws<ArgumentException>(()=>AssetCodec.Decode(bad));
        }
        void Change(int offset,int value)
        { var bad=(byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(offset),value); Reject(bad); }
        Change(counts+56,-1); Change(counts+56,9); Change(counts+56,0);
        Change(counts+60,-1); Change(counts+60,1025); Change(counts+60,8); Change(counts+60,25);
        Change(extension,-1); Change(extension,1); Change(extension,asset.Components.Count);
        Change(extension+4,1); Change(extension+8,33); Change(extension+12,3); Change(extension+16,int.MaxValue);
        int firstPoints=asset.Components[0].Converter!.PumpPositive!.Points.Count*2+asset.Components[0].Converter!.TurbinePositive!.Points.Count*2;
        int second=extension+20+28*firstPoints;
        Change(second,0); // Re-signed duplicate extension must not replace the first converter.
        Change(extension+20+24,(int)Unit.NewtonMeter); // Wrong dimension in a map point.
        var altered=(byte[])bytes.Clone(); BinaryPrimitives.WriteDoubleLittleEndian(altered.AsSpan(extension+20),-.9); Reject(altered);
        var down=bytes.Take(counts+56).Concat(bytes.Skip(counts+80).Take(extension-counts-80)).Concat(bytes.Skip(second+20+28*8)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(down.AsSpan(8),8); Reject(down);
    }
}
