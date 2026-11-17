// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class TankPhaseAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("tank phase asset / complete fuel and displacement histories / authentic v30", Replay),
        ("tank phase asset / invalid flags geometry composition and forged downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(TankPhaseChecks.Model(vent:1e-10), "Finite tank phase equilibrium", new string('d',64),20_000_000,1_000_000,[],[]);
    internal static byte[] Version30(PowerAsset asset, byte[] bytes)
    {
        int tanks=asset.Components.Count(c=>c.LiquidFuelTank is not null),returns=asset.Components.Count(c=>c.LiquidRailReturn is not null);
        int end=bytes.Length-32-24*asset.Inputs.Count-33*asset.Checks.Count-24*returns,start=end-52*tanks;
        var output=new List<byte>(bytes.Take(start));
        for(int i=0;i<tanks;++i)output.AddRange(bytes.Skip(start+52*i).Take(48));
        output.AddRange(bytes.Skip(end));var result=output.ToArray();BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(8),30);Sign(result);return result;
    }
    private static void Sign(byte[] bytes)=>SHA256.HashData(bytes.AsSpan(0,bytes.Length-32)).CopyTo(bytes,bytes.Length-32);
    private static void Compare(PowerAsset a,PowerAsset b)
    {
        var left=a.CreatePlayback();var right=b.CreatePlayback();var lv=new Scalar[a.Model.OutputCount];var rv=new Scalar[b.Model.OutputCount];
        for(ulong at=0;at<=a.DurationNanoseconds;at+=a.SampleEveryNanoseconds)
        {
            if(at>left.TimeNanoseconds)Require(left.Advance(at-left.TimeNanoseconds)==SimulationStatus.Ok);
            while(right.TimeNanoseconds<at)Require(right.Advance(Math.Min(257*b.Model.StepNanoseconds,at-right.TimeNanoseconds))==SimulationStatus.Ok);
            Require(left.ReadSnapshot(lv)==right.ReadSnapshot(rv)&&lv.SequenceEqual(rv));
        }
    }
    private static void Replay()
    {
        var asset=Asset();var bytes=AssetCodec.Encode(asset);var decoded=AssetCodec.Decode(bytes);
        Require(decoded.Model.HasEquilibriumTanks&&decoded.Model.EquilibriumTankCount==1&&bytes.SequenceEqual(AssetCodec.Encode(decoded)));Compare(asset,decoded);
        var fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","equilibrium-injected-cylinder-v30.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8))==30&&Convert.ToHexStringLower(SHA256.HashData(fixture))=="c246723026ac3cbdd69b3e0015b29a161d15c85865e94f3054507cd9c7bc2335");
        var old=AssetCodec.Decode(fixture);Require(!old.Model.HasEquilibriumTanks&&old.Model.HasEquilibriumFuelFilms&&old.Model.Fingerprint.ToString("x16")=="74c05089a700aca4");
        var upgraded=AssetCodec.Encode(old);Require(Version30(old,upgraded).SequenceEqual(fixture));Compare(old,AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        var asset=Asset();var bytes=AssetCodec.Encode(asset);int tank=bytes.Length-84;
        void Reject(byte[] bad){Sign(bad);Throws<ArgumentException>(()=>AssetCodec.Decode(bad));}
        foreach(int value in new[]{-1,2,int.MaxValue}){var bad=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(tank+48),value);Reject(bad);}
        var absent=bytes.ToArray();Array.Clear(absent,tank+32,16);Reject(absent);
        var disabled=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(disabled.AsSpan(tank+48),0);Reject(disabled);
        Reject(Version30(asset,bytes));
    }
}
