// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;
namespace Power.Tests;
internal static class EquilibriumDuctAssetChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("equilibrium duct asset / full cell state and immutable topology / authentic v35",Replay),
        ("equilibrium duct asset / bounded variable cells bulk units and downgrade rejection",Corruption)];
    private static PowerAsset Asset(ModelDefinition? d=null)=>PowerAsset.Create(d??EquilibriumDuctChecks.Model(),"Equilibrium duct",new string('c',64),100000,10000,[],[]);
    private static void Sign(byte[] b)=>SHA256.HashData(b.AsSpan(0,b.Length-32)).CopyTo(b,b.Length-32);
    private static void Compare(PowerAsset a,PowerAsset b)
    {var l=a.CreatePlayback();var r=b.CreatePlayback();var lv=new Scalar[a.Model.OutputCount];var rv=new Scalar[b.Model.OutputCount];for(ulong at=0;at<=a.DurationNanoseconds;at+=a.SampleEveryNanoseconds){if(at>l.TimeNanoseconds)Require(l.Advance(at-l.TimeNanoseconds)==SimulationStatus.Ok);while(r.TimeNanoseconds<at)Require(r.Advance(Math.Min(257*b.Model.StepNanoseconds,at-r.TimeNanoseconds))==SimulationStatus.Ok);Require(l.ReadSnapshot(lv)==r.ReadSnapshot(rv)&&lv.SequenceEqual(rv));}}
    private static void Replay()
    {
        uint[] cells=[100,101,102,103];var d=EquilibriumDuctChecks.Model();d=d with{Components=[d.Components[0] with{FuelDuct=d.Components[0].FuelDuct! with{Cells=cells}}]};
        var asset=Asset(d);var bytes=AssetCodec.Encode(asset);cells[0]=2;Require(bytes.SequenceEqual(AssetCodec.Encode(asset)));var decoded=AssetCodec.Decode(bytes);Require(decoded.Model.HasEquilibriumFuelDucts&&bytes.SequenceEqual(AssetCodec.Encode(decoded)));Compare(asset,decoded);
        var fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","inertial-pump-fed-cylinder-v35.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8))==35&&Convert.ToHexStringLower(SHA256.HashData(fixture))=="a5f8117c0240e888ee9cd9556f13cf668aff5d50ca8e9afad1c6d053310a4ac4");
        var old=AssetCodec.Decode(fixture);Require(old.Model.HasFuelPipes&&!old.Model.HasEquilibriumFuelDucts);var upgraded=AssetCodec.Encode(old);var back=upgraded.ToArray();BinaryPrimitives.WriteInt32LittleEndian(back.AsSpan(8),35);Sign(back);Require(back.SequenceEqual(fixture));Compare(old,AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        var bytes=AssetCodec.Encode(Asset());int duct=bytes.Length-32-72;
        void Reject(byte[] data){Sign(data);Throws<ArgumentException>(()=>AssetCodec.Decode(data));}
        foreach(int count in new[]{0,25,int.MaxValue}){var bad=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(duct+4),count);Reject(bad);}
        foreach(int offset in new[]{0,16,28,40,52}){var bad=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(duct+offset),int.MaxValue);Reject(bad);}
        var duplicate=bytes.ToArray();BinaryPrimitives.WriteUInt32LittleEndian(duplicate.AsSpan(duct+60),100);Reject(duplicate);
        var downgrade=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8),35);Reject(downgrade);
        Reject(bytes.Take(duct).Concat(bytes.Skip(duct+72)).ToArray());
    }
}
