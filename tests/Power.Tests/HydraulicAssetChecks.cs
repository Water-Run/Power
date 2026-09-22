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

internal static class HydraulicAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("hydraulic asset / pressure ports and geometry / all replay boundaries / authentic v9", Replay),
        ("hydraulic asset / counts / malformed extensions / wrong units / downgrade rejection", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(HydraulicChecks.ClutchModel(),"Hydraulic replay",new string('c',64),800_000_000,10_000_000,
        [new(600_000_000,100,0),new(600_000_000,101,1),new(600_000_000,102,-10)],[new(0,Field.HydraulicVolumeResidual,null,null,1e-16)]);
    private static void Replay()
    {
        var original=Asset(); var bytes=AssetCodec.Encode(original); var decoded=AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8))==11);
        Require(decoded.Nodes.SequenceEqual(original.Nodes) && decoded.Components.SequenceEqual(original.Components));
        Require(decoded.Model.Fingerprint==original.Model.Fingerprint && AssetCodec.Encode(decoded).SequenceEqual(bytes));
        var a=original.CreatePlayback(); var b=decoded.CreatePlayback();
        for(int i=0;i<80;++i)
        {
            Require(a.Advance(10_000_000)==SimulationStatus.Ok);
            for(int j=0;j<10;++j) Require(b.Advance(1_000_000)==SimulationStatus.Ok);
            Require(a.ReadSnapshot(new Scalar[a.Model.OutputCount])==b.ReadSnapshot(new Scalar[b.Model.OutputCount]));
        }
        byte[] fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","fired-converter-v9.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(fixture))=="96a78326ae2f2e8dd4a434fb81fbe88f4a19156cbf13cf069a7bd4e798e93c9f");
        var old=AssetCodec.Decode(fixture); Require(old.Model.Fingerprint.ToString("x16")=="839d03901973668d");
        var upgraded=AssetCodec.Decode(AssetCodec.Encode(old)); var first=old.CreatePlayback(); var second=upgraded.CreatePlayback();
        for(ulong at=0;at<old.DurationNanoseconds;at+=old.SampleEveryNanoseconds)
        {
            Require(first.Advance(old.SampleEveryNanoseconds)==SimulationStatus.Ok && second.Advance(old.SampleEveryNanoseconds)==SimulationStatus.Ok);
            Require(first.ReadSnapshot(new Scalar[old.Model.OutputCount])==second.ReadSnapshot(new Scalar[old.Model.OutputCount]));
        }
        Require(first.ReadSnapshot(new Scalar[old.Model.OutputCount]).StateHash.ToString("x16")=="834a679376b7a6fd");
    }
    private static void Corruption()
    {
        var asset=Asset(); var bytes=AssetCodec.Encode(asset);
        int counts=78+Encoding.UTF8.GetByteCount(asset.Name), extension=counts+80+44*asset.Nodes.Count+156*asset.Components.Count;
        void Reject(byte[] bad)
        { SHA256.HashData(bad.AsSpan(0,bad.Length-32)).CopyTo(bad,bad.Length-32); Throws<ArgumentException>(()=>AssetCodec.Decode(bad)); }
        void Change(int offset,int value)
        { var bad=(byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(offset),value); Reject(bad); }
        Change(counts+64,-1); Change(counts+64,65); Change(counts+64,0); Change(counts+68,0); Change(counts+68,65);
        Change(extension,-1); Change(extension,2); Change(extension+40,0); Change(extension+12,(int)Unit.Pascal);
        int actuator=extension+80;
        Change(actuator,0); Change(actuator,asset.Components.Count); Change(actuator+4,4); Change(actuator+4,0);
        Change(actuator+60,0); Change(actuator+60,129); Change(actuator+16,(int)Unit.Meter);
        var missing=bytes.Take(extension).Concat(bytes.Skip(extension+40)).ToArray(); BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts+64),1); Reject(missing);
        var duplicate=bytes.Take(actuator+64).Concat(bytes.Skip(actuator).Take(64)).Concat(bytes.Skip(actuator+64)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts+68),2); Reject(duplicate);
        var down=bytes.Take(counts+64).Concat(bytes.Skip(counts+80).Take(extension-counts-80)).Concat(bytes.Skip(actuator+64)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(down.AsSpan(8),9); Reject(down);
        // A hydraulic node without any extensions must still reject a pre-v10 downgrade.
        var single=PowerAsset.Create(new(){StepNanoseconds=1_000_000,Nodes=[NodeDefinition.Hydraulic(1,1e-12,1e6)],Components=[]},"Isolated",new string('d',64),1_000_000,1_000_000,[],[]);
        byte[] isolated=AssetCodec.Encode(single); int isolatedCounts=78+Encoding.UTF8.GetByteCount(single.Name);
        var old=isolated.Take(isolatedCounts+64).Concat(isolated.Skip(isolatedCounts+80)).ToArray(); BinaryPrimitives.WriteInt32LittleEndian(old.AsSpan(8),9); Reject(old);
    }
}
