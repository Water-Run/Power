// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Core;
using Power.Assets;
using static Power.Tests.CoreChecks;
namespace Power.Tests;
internal static class EquilibriumNozzleAssetChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("equilibrium nozzle asset / mode uncertainty and every replay boundary authentic v38",Replay),
        ("equilibrium nozzle asset / phase mode version gating and typed corruption",Corruption)];
    private static PowerAsset Asset()=>PowerAsset.Create(EquilibriumNozzleGraphChecks.Model(),"Equilibrium nozzle",new string('c',64),1000000,100000,[],[]);
    private static void Sign(byte[] b)=>SHA256.HashData(b.AsSpan(0,b.Length-32)).CopyTo(b,b.Length-32);
    private static void Compare(PowerAsset a,PowerAsset b)
    {var l=a.CreatePlayback();var r=b.CreatePlayback();var lv=new Scalar[a.Model.OutputCount];var rv=new Scalar[b.Model.OutputCount];for(ulong at=0;at<=a.DurationNanoseconds;at+=a.SampleEveryNanoseconds){if(at>l.TimeNanoseconds)Require(l.Advance(at-l.TimeNanoseconds)==SimulationStatus.Ok);while(r.TimeNanoseconds<at)Require(r.Advance(Math.Min(7*b.Model.StepNanoseconds,at-r.TimeNanoseconds))==SimulationStatus.Ok);Require(l.ReadSnapshot(lv)==r.ReadSnapshot(rv)&&lv.SequenceEqual(rv));}}
    private static void Replay()
    {
        var asset=Asset();var bytes=AssetCodec.Encode(asset);var decoded=AssetCodec.Decode(bytes);Require(decoded.Model.HasEquilibriumFuelNozzles&&bytes.SequenceEqual(AssetCodec.Encode(decoded)));Compare(asset,decoded);
        var fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","compressible-pump-needle-v38.powerasset"));Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8))==38&&Convert.ToHexStringLower(SHA256.HashData(fixture))=="b8469401c7a4469a603aa81ef34ca70d9fc98bbc7e56aa7877a4fa65a153e195");
        var old=AssetCodec.Decode(fixture);Require(old.Model.HasCompressibleFuelNozzles&&!old.Model.HasEquilibriumFuelNozzles);var upgraded=AssetCodec.Encode(old);var back=upgraded.ToArray();BinaryPrimitives.WriteInt32LittleEndian(back.AsSpan(8),38);Sign(back);Require(back.SequenceEqual(fixture));Compare(old,AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        var bytes=AssetCodec.Encode(Asset());int record=bytes.Length-32-68;
        void Reject(byte[] b){Sign(b);Throws<ArgumentException>(()=>AssetCodec.Decode(b));}
        foreach(int mode in new[]{0,3,int.MaxValue}){var b=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(record+24),mode);Reject(b);}
        foreach(int version in new[]{37,38}){var b=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(8),version);Reject(b);}
    }
}
