// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Core;
using Power.Assets;
using static Power.Tests.CoreChecks;
namespace Power.Tests;
internal static class RelaxingBoundaryAssetChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("relaxing boundary asset / moving work vapor metering and authentic v40 replay",Replay),
        ("relaxing boundary asset / old-format topology and equilibrium transit rejection",Corruption)];
    private static PowerAsset Asset(ModelDefinition d)=>PowerAsset.Create(d,"Relaxing boundary",new string('d',64),100000,10000,[],[]);
    private static void Sign(byte[] b)=>SHA256.HashData(b.AsSpan(0,b.Length-32)).CopyTo(b,b.Length-32);
    private static void Compare(PowerAsset a,PowerAsset b)
    {var left=a.CreatePlayback();var right=b.CreatePlayback();var lv=new Scalar[a.Model.OutputCount];var rv=new Scalar[b.Model.OutputCount];for(ulong at=0;at<=a.DurationNanoseconds;at+=a.SampleEveryNanoseconds){if(at>left.TimeNanoseconds)Require(left.Advance(at-left.TimeNanoseconds)==SimulationStatus.Ok);while(right.TimeNanoseconds<at)Require(right.Advance(Math.Min(7*b.Model.StepNanoseconds,at-right.TimeNanoseconds))==SimulationStatus.Ok);Require(left.ReadSnapshot(lv)==right.ReadSnapshot(rv)&&lv.SequenceEqual(rv));}}
    private static void Replay()
    {
        foreach(var model in new[]{RelaxingBoundaryChecks.ChamberModel(),RelaxingBoundaryChecks.NozzleModel(),RelaxingBoundaryChecks.PumpModel()}){var a=Asset(model);var b=AssetCodec.Decode(AssetCodec.Encode(a));Require(b.Model.HasRelaxingFuelBoundaries);Compare(a,b);}
        var bytes=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","relaxing-fuel-duct-v40.powerasset"));Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8))==40&&Convert.ToHexStringLower(SHA256.HashData(bytes))=="17d1ac4e78cb9835faa8f50b0bae30c4c6d84ecc86e7936ad03ab416e3978d39");
        var old=AssetCodec.Decode(bytes);Require(old.Model.HasRelaxingFuel&&!old.Model.HasRelaxingFuelBoundaries);var upgraded=AssetCodec.Encode(old);var back=upgraded.ToArray();BinaryPrimitives.WriteInt32LittleEndian(back.AsSpan(8),40);Sign(back);Require(back.SequenceEqual(bytes));Compare(old,AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        foreach(var model in new[]{RelaxingBoundaryChecks.ChamberModel(),RelaxingBoundaryChecks.NozzleModel()}){var bytes=AssetCodec.Encode(Asset(model));BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8),40);Sign(bytes);Throws<AssetFormatException>(()=>AssetCodec.Decode(bytes));}
        var forged=AssetCodec.Encode(Asset(RelaxingBoundaryChecks.NozzleModel()));int nozzle=forged.Length-32-68;BinaryPrimitives.WriteInt32LittleEndian(forged.AsSpan(nozzle+24),(int)FuelDischargePhaseModel.Equilibrium);Sign(forged);Throws<ArgumentException>(()=>AssetCodec.Decode(forged));
    }
}
