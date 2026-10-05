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

internal static class LiquidRailFeedAssetChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("liquid feed asset / mixed caloric rail pressure pump histories / replay / authentic v25", Replay),
        ("liquid feed asset / bounded links temperatures typed coverage / forged downgrade", Corruption)
    ];
    private static PowerAsset Asset()=>PowerAsset.Create(LiquidRailFeedChecks.Model(dose:8e-6),"Pump-fed rail",new string('a',64),20_000_000,1_000_000,[],[]);
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
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8))==26&&decoded.Model.HasLiquidFeeds);
        Require(bytes.SequenceEqual(AssetCodec.Encode(decoded)));Compare(asset,decoded);
        var fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","closure-compensated-cylinder-v25.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8))==25&&Convert.ToHexStringLower(SHA256.HashData(fixture))=="df400d7e72a2375c6b6185e7206162f2dd8f74ecd52c1862b0bedca6c03ba6dc");
        var old=AssetCodec.Decode(fixture);Compare(old,AssetCodec.Decode(AssetCodec.Encode(old)));
    }
    private static void Corruption()
    {
        var asset=Asset();byte[] bytes=AssetCodec.Encode(asset);int counts=78+Encoding.UTF8.GetByteCount(asset.Name);
        int record=counts+152+44*asset.Nodes.Count+156*asset.Components.Count+24+36+40+64+120+32;
        void Reject(byte[] bad){SHA256.HashData(bad.AsSpan(0,bad.Length-32)).CopyTo(bad,bad.Length-32);Throws<ArgumentException>(()=>AssetCodec.Decode(bad));}
        foreach(var change in new[]{(counts+148,-1),(counts+148,0),(counts+148,65),(record,-1),(record+4,10),(record+8,12),(record+20,(int)Unit.Pascal)})
        {var bad=bytes.ToArray();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(change.Item1),change.Item2);Reject(bad);}
        var down=bytes.Take(counts+148).Concat(bytes.Skip(counts+152).Take(record-counts-152)).Concat(bytes.Skip(record+24)).ToArray();BinaryPrimitives.WriteInt32LittleEndian(down.AsSpan(8),25);Reject(down);
    }
}
