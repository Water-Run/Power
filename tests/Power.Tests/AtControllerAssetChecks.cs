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

internal static class AtControllerAssetChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("AT controller asset / routes pressure feedback clocks and PI state / complete replay / v24", Replay),
        ("AT controller asset / bounded typed route records units and forged downgrade", Corruption)
    ];
    private static PowerAsset Asset()=>PowerAsset.Create(AtControllerChecks.Model(),"Hydraulic AT feedback",new string('b',64),100_000_000,10_000_000,[],[]);
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
        Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8))==AssetCodec.FormatVersion&&decoded.Model.StateCount==99&&decoded.Model.Fidelity=="sampled_hydraulic_at_control");
        Require(bytes.SequenceEqual(AssetCodec.Encode(decoded)));Compare(asset,decoded);
        byte[] fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","fired-hydraulic-ravigneaux-v24.powerasset"));
        Require(BinaryPrimitives.ReadInt32LittleEndian(fixture.AsSpan(8))==24&&Convert.ToHexStringLower(SHA256.HashData(fixture))=="6d6dc0f3b17ae1dfdcda59fc45ca7db63d260fb954c8c51567f9b00bab783ecc");
        var old=AssetCodec.Decode(fixture);Require(old.Model.Fingerprint.ToString("x16")=="4e83efb34de63922");Compare(old,AssetCodec.Decode(AssetCodec.Encode(old)));
    }
    private static void Corruption()
    {
        var asset=Asset();byte[] source=AssetCodec.Encode(asset);int counts=78+Encoding.UTF8.GetByteCount(asset.Name);
        int record=counts+156+44*asset.Nodes.Count+156*asset.Components.Count+8*5+40*12+32+16+104*5+40*5;
        void Reject(byte[] bad){SHA256.HashData(bad.AsSpan(0,bad.Length-32)).CopyTo(bad,bad.Length-32);Throws<ArgumentException>(()=>AssetCodec.Decode(bad));}
        foreach(var change in new[]{(counts+140,-1),(counts+140,65),(counts+144,0),(counts+144,7),(record,-1),(record+16,4),(record+16,7),(record+208,999),(record+212,1234),(record+216,1230),(record+68,(int)Unit.Newton)})
        {var bad=source.ToArray();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(change.Item1),change.Item2);Reject(bad);}
        var down=source.Take(counts+140).Concat(source.Skip(counts+156).Take(record-counts-156)).Concat(source.Skip(record+208+12*5)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(down.AsSpan(8),24);Reject(down);
    }
}
