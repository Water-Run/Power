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

internal static class ResolvedPlanetAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("resolved planet asset / signed carriers spin inertia / all boundaries / authentic v23", Replay),
        ("resolved planet asset / malformed typed carrier records / forged downgrade", Corruption)
    ];
    private static PowerAsset Asset()=>PowerAsset.Create(ResolvedPlanetChecks.Model(),"Resolved planets",new string('c',64),20_000_000,1_000_000,[],[]);
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
        var asset=Asset();byte[] bytes=AssetCodec.Encode(asset);var decoded=AssetCodec.Decode(bytes);
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8))==25&&decoded.Components.SequenceEqual(asset.Components)&&decoded.Nodes.SequenceEqual(asset.Nodes));
        Require(bytes.SequenceEqual(AssetCodec.Encode(decoded)));Compare(asset,decoded);
        byte[] fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","ravigneaux-transmission-v23.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8))==23&&Convert.ToHexStringLower(SHA256.HashData(fixture))=="f2bd390e8ecf013e5843ed3352ccf2a2828133b541fc61d7927995f8ac1dc2e2");
        var old=AssetCodec.Decode(fixture);Require(old.Model.Fingerprint.ToString("x16")=="63d28eb32bc4cfb2");Compare(old,AssetCodec.Decode(AssetCodec.Encode(old)));
    }
    private static void Corruption()
    {
        var asset=Asset();byte[] bytes=AssetCodec.Encode(asset);int counts=78+Encoding.UTF8.GetByteCount(asset.Name);
        int record=counts+148+44*asset.Nodes.Count+156*asset.Components.Count+28*5;
        void Reject(byte[] bad){SHA256.HashData(bad.AsSpan(0,bad.Length-32)).CopyTo(bad,bad.Length-32);Throws<ArgumentException>(()=>AssetCodec.Decode(bad));}
        foreach(var change in new[]{(counts+52,-1),(counts+52,0),(counts+52,65),(record,-1),(record+4,0),(record+4,100),(record+8,0)})
        {var bad=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(change.Item1),change.Item2);Reject(bad);}
        var down=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(down.AsSpan(8),23);Reject(down);
    }
}
