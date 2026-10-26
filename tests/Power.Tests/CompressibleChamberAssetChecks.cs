// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using System.Buffers.Binary;
using System.Security.Cryptography;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;
namespace Power.Tests;
internal static class CompressibleChamberAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("compressible chamber asset / coupled geometry every replay boundary and authentic v36", Replay),
        ("compressible chamber asset / typed geometry units ownership and downgrade rejection", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(CompressibleChamberChecks.Model(true), "Compressible chamber", new string('c',64), 100000, 10000, [], []);
    private static void Sign(byte[] b) => SHA256.HashData(b.AsSpan(0, b.Length-32)).CopyTo(b,b.Length-32);
    private static void Compare(PowerAsset a, PowerAsset b)
    {
        var left=a.CreatePlayback();var right=b.CreatePlayback();var l=new Scalar[a.Model.OutputCount];var r=new Scalar[b.Model.OutputCount];
        for(ulong at=0;at<=a.DurationNanoseconds;at+=a.SampleEveryNanoseconds)
        {if(at>left.TimeNanoseconds)Require(left.Advance(at-left.TimeNanoseconds)==SimulationStatus.Ok);while(right.TimeNanoseconds<at)Require(right.Advance(Math.Min(7*b.Model.StepNanoseconds,at-right.TimeNanoseconds))==SimulationStatus.Ok);Require(left.ReadSnapshot(l)==right.ReadSnapshot(r)&&l.SequenceEqual(r));}
    }
    private static void Replay()
    {
        var asset=Asset();var encoded=AssetCodec.Encode(asset);var decoded=AssetCodec.Decode(encoded);Require(decoded.Model.HasCompressibleFuelChambers&&encoded.SequenceEqual(AssetCodec.Encode(decoded)));Compare(asset,decoded);
        var fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","equilibrium-fuel-condensing-duct-v36.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8))==36&&Convert.ToHexStringLower(SHA256.HashData(fixture))=="093420e1031206913600e1581ed717012194d303f60f8afec7595a324f789cc5");
        var old=AssetCodec.Decode(fixture);Require(old.Model.HasEquilibriumFuelDucts&&!old.Model.HasCompressibleFuelChambers);var upgraded=AssetCodec.Encode(old);var back=upgraded.ToArray();BinaryPrimitives.WriteInt32LittleEndian(back.AsSpan(8),36);Sign(back);Require(back.SequenceEqual(fixture));Compare(old,AssetCodec.Decode(upgraded));
    }
    private static void Corruption()
    {
        var encoded=AssetCodec.Encode(Asset());int geometry=encoded.Length-32-72-40;
        void Reject(byte[] b){Sign(b);Throws<ArgumentException>(()=>AssetCodec.Decode(b));}
        foreach(int offset in new[]{0,12,24,36}){var b=encoded.ToArray();BinaryPrimitives.WriteInt32LittleEndian(b.AsSpan(geometry+offset),int.MaxValue);Reject(b);}
        var downgrade=encoded.ToArray();BinaryPrimitives.WriteInt32LittleEndian(downgrade.AsSpan(8),36);Reject(downgrade);
        Reject(encoded.Take(geometry).Concat(encoded.Skip(geometry+40)).ToArray());
    }
}
