// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;
namespace Power.Tests;
internal static class CompressibleNozzleAssetChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("compressible nozzle asset / frozen phase needle complete replay and authentic v37",Replay),
        ("compressible nozzle asset / typed record bounds units unused needle and downgrade rejection",Corruption)];
    private static PowerAsset Asset(bool pump=false)=>PowerAsset.Create(pump?CompressibleNozzleChecks.PumpModel():CompressibleNozzleChecks.Model(),"Compressible nozzle",new string('c',64),1000000,100000,[],[]);
    private static void Sign(byte[] b)=>SHA256.HashData(b.AsSpan(0,b.Length-32)).CopyTo(b,b.Length-32);
    private static void Compare(PowerAsset a,PowerAsset b)
    {var l=a.CreatePlayback();var r=b.CreatePlayback();var lv=new Scalar[a.Model.OutputCount];var rv=new Scalar[b.Model.OutputCount];for(ulong at=0;at<=a.DurationNanoseconds;at+=a.SampleEveryNanoseconds){if(at>l.TimeNanoseconds)Require(l.Advance(at-l.TimeNanoseconds)==SimulationStatus.Ok);while(r.TimeNanoseconds<at)Require(r.Advance(Math.Min(7*b.Model.StepNanoseconds,at-r.TimeNanoseconds))==SimulationStatus.Ok);Require(l.ReadSnapshot(lv)==r.ReadSnapshot(rv)&&lv.SequenceEqual(rv));}}
    private static void Replay()
    {
        foreach(bool pump in new[]{false,true}){var asset=Asset(pump);var bytes=AssetCodec.Encode(asset);var decoded=AssetCodec.Decode(bytes);Require(decoded.Model.HasCompressibleFuelNozzles&&bytes.SequenceEqual(AssetCodec.Encode(decoded)));Compare(asset,decoded);}
        var fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","compressible-fuel-chamber-duct-v37.powerasset"));Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8))==37&&Convert.ToHexStringLower(SHA256.HashData(fixture))=="60dd76119b8079bf5c9ac731026cd4ced1893b36470816955008847e8bbb3db7");
        var old=AssetCodec.Decode(fixture);Require(!old.Model.HasCompressibleFuelNozzles&&old.Model.HasCompressibleFuelChambers);var upgraded=AssetCodec.Encode(old);var back=upgraded.ToArray();BinaryPrimitives.WriteInt32LittleEndian(back.AsSpan(8),37);Sign(back);Require(back.SequenceEqual(fixture));Compare(old,AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        var bytes=AssetCodec.Encode(Asset());int record=bytes.Length-32-68;
        void Reject(byte[] b){Sign(b);Throws<ArgumentException>(()=>AssetCodec.Decode(b));}
        foreach(int offset in new[]{0,12,24,36,40,52,64}){var b=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(record+offset),int.MaxValue);Reject(b);}
        var unused=bytes.ToArray();BinaryPrimitives.WriteDoubleLittleEndian(unused.AsSpan(record+44),1);Reject(unused);
        var downgrade=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8),37);Reject(downgrade);Reject(bytes.Take(record).Concat(bytes.Skip(record+68)).ToArray());
    }
}
