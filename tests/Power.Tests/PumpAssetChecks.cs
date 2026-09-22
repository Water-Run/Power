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

internal static class PumpAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("pump asset / pressure boundary and finite inlet / signed work / authentic v10", Replay),
        ("pump asset / bounded tables / duplicate and malformed extensions / downgrade rejection", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(PumpChecks.ClutchModel(),"Pump replay",new string('e',64),700_000_000,10_000_000,[],[]);
    private static void Replay()
    {
        foreach(var original in new[]{Asset(),PowerAsset.Create(PumpChecks.Oscillator(),"Pressure inlet",new string('f',64),100_000_000,10_000_000,[],[]),
            PowerAsset.Create(new(){StepNanoseconds=1_000_000,Nodes=[NodeDefinition.Rotor(1,.2,10),NodeDefinition.Hydraulic(2,2e-12,1e6),NodeDefinition.Hydraulic(3,3e-12,2e6)],Components=[ComponentDefinition.DisplacementPump(10,1,2,1e-7,3)]},"Finite inlet",new string('f',64),100_000_000,10_000_000,[],[])})
        {
            var bytes=AssetCodec.Encode(original);var decoded=AssetCodec.Decode(bytes);
            Require(decoded.Components.SequenceEqual(original.Components));Require(bytes.SequenceEqual(AssetCodec.Encode(decoded)));
            var a=original.CreatePlayback();var b=decoded.CreatePlayback();
            while(!a.Completed){Require(a.Advance(10_000_000)==SimulationStatus.Ok);for(int i=0;i<10;++i)Require(b.Advance(1_000_000)==SimulationStatus.Ok);Require(a.ReadSnapshot(new Scalar[a.Model.OutputCount])==b.ReadSnapshot(new Scalar[b.Model.OutputCount]));}
        }
        byte[] fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","fired-hydraulic-v10.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(fixture))=="6aa1dff7a252470e5c904bdcebe3a183ca98854b5ac5053ec7b6089104108d86");
        var old=AssetCodec.Decode(fixture);Require(old.Model.Fingerprint.ToString("x16")=="01b69cb3abe52211");
        var first=old.CreatePlayback();var second=AssetCodec.Decode(AssetCodec.Encode(old)).CreatePlayback();
        while(!first.Completed){Require(first.Advance(10_000_000)==SimulationStatus.Ok && second.Advance(10_000_000)==SimulationStatus.Ok);Require(first.ReadSnapshot(new Scalar[first.Model.OutputCount])==second.ReadSnapshot(new Scalar[second.Model.OutputCount]));}
        Require(first.ReadSnapshot(new Scalar[first.Model.OutputCount]).StateHash.ToString("x16")=="46a01d103e6159d3");
    }
    private static void Corruption()
    {
        var asset=Asset();var bytes=AssetCodec.Encode(asset);int counts=78+Encoding.UTF8.GetByteCount(asset.Name);
        int pump=counts+80+44*asset.Nodes.Count+156*asset.Components.Count+40+64,relief=pump+32;
        void Reject(byte[] bad){SHA256.HashData(bad.AsSpan(0,bad.Length-32)).CopyTo(bad,bad.Length-32);Throws<ArgumentException>(()=>AssetCodec.Decode(bad));}
        void Change(int offset,int value){var bad=(byte[])bytes.Clone();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(offset),value);Reject(bad);}
        Change(counts+72,-1);Change(counts+72,65);Change(counts+72,0);Change(counts+76,0);Change(counts+76,65);
        Change(pump,-1);Change(pump,1);Change(pump+4,3);Change(pump+4,1);Change(pump+16,(int)Unit.CubicMeter);Change(pump+28,(int)Unit.NewtonMeter);
        Change(relief,0);Change(relief,4);Change(relief+12,(int)Unit.NewtonMeter);
        var duplicate=bytes.Take(pump+32).Concat(bytes.Skip(pump).Take(32)).Concat(bytes.Skip(pump+32)).ToArray();BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts+72),2);Reject(duplicate);
        var down=bytes.Take(counts+72).Concat(bytes.Skip(counts+80).Take(pump-counts-80)).Concat(bytes.Skip(relief+16)).ToArray();BinaryPrimitives.WriteInt32LittleEndian(down.AsSpan(8),10);Reject(down);
    }
}
