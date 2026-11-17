// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Core;
using Power.Assets;
using static Power.Tests.CoreChecks;
namespace Power.Tests;
internal static class RelaxingFuelAssetChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("relaxing asset / quality time phase histories portable replay authentic v39",Replay),
        ("relaxing asset / material extension time quality units and downgrade rejection",Corruption)];
    private static PowerAsset Asset(bool duct=false)=>PowerAsset.Create(RelaxingFuelGraphChecks.Model(duct),"Relaxing fuel",new string('c',64),100000,10000,[],[]);
    private static void Sign(byte[] b)=>SHA256.HashData(b.AsSpan(0,b.Length-32)).CopyTo(b,b.Length-32);
    private static void Compare(PowerAsset a,PowerAsset b)
    {var l=a.CreatePlayback();var r=b.CreatePlayback();var lv=new Scalar[a.Model.OutputCount];var rv=new Scalar[b.Model.OutputCount];for(ulong at=0;at<=a.DurationNanoseconds;at+=a.SampleEveryNanoseconds){if(at>l.TimeNanoseconds)Require(l.Advance(at-l.TimeNanoseconds)==SimulationStatus.Ok);while(r.TimeNanoseconds<at)Require(r.Advance(Math.Min(7*b.Model.StepNanoseconds,at-r.TimeNanoseconds))==SimulationStatus.Ok);Require(l.ReadSnapshot(lv)==r.ReadSnapshot(rv)&&lv.SequenceEqual(rv));}}
    private static void Replay()
    {
        foreach(bool duct in new[]{false,true}){var a=Asset(duct);var bytes=AssetCodec.Encode(a);var b=AssetCodec.Decode(bytes);Require(b.Model.HasRelaxingFuel&&bytes.SequenceEqual(AssetCodec.Encode(b)));Compare(a,b);}
        var fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","equilibrium-pump-needle-v39.powerasset"));Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8))==39&&Convert.ToHexStringLower(SHA256.HashData(fixture))=="95b49037e29e2f6be15291cbb45ea8c053e976aa9e3a0df9a9806a00725bcfac");
        var old=AssetCodec.Decode(fixture);Require(!old.Model.HasRelaxingFuel&&old.Model.HasEquilibriumFuelNozzles);var upgraded=AssetCodec.Encode(old);var back=upgraded.ToArray();BinaryPrimitives.WriteInt32LittleEndian(back.AsSpan(8),39);Sign(back);Require(back.SequenceEqual(fixture));Compare(old,AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        var bytes=AssetCodec.Encode(Asset());int material=bytes.Length-32-180;
        void Reject(byte[] b){Sign(b);Throws<ArgumentException>(()=>AssetCodec.Decode(b));}
        foreach(ulong tau in new ulong[]{0,3_600_000_000_001,ulong.MaxValue}){var b=bytes.ToArray();BinaryPrimitives.WriteUInt64LittleEndian(b.AsSpan(material+160),tau);Reject(b);}
        foreach(double quality in new[]{-1d,1.1,double.NaN}){var b=bytes.ToArray();BinaryPrimitives.WriteDoubleLittleEndian(b.AsSpan(material+168),quality);Reject(b);}
        var unit=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(unit.AsSpan(material+176),(int)Unit.Kelvin);Reject(unit);
        var downgrade=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8),39);Reject(downgrade);Reject(bytes.Take(material+160).Concat(bytes.Skip(material+180)).ToArray());
    }
}
