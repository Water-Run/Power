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

internal static class FuelInjectorAssetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel injector asset / v17 nozzle and timing / every boundary / authentic v16", Replay),
        ("fuel injector asset / typed coverage / counts / dimensions / downgrade", Corruption)
    ];
    private static PowerAsset Asset() => PowerAsset.Create(FuelInjectorChecks.Model(), "Cycle fuel meter", new string('a',64),
        300_000_000,10_000_000,[new(150_050_000,100,1e-6)],[]);
    private static void Compare(PowerAsset original,PowerAsset updated)
    {
        var a=original.CreatePlayback();var b=updated.CreatePlayback();var left=new Scalar[a.Model.OutputCount];var right=new Scalar[b.Model.OutputCount];
        var boundaries=new SortedSet<ulong>{0,original.DurationNanoseconds};for(ulong at=original.SampleEveryNanoseconds;at<original.DurationNanoseconds;at+=original.SampleEveryNanoseconds)boundaries.Add(at);foreach(var input in original.Inputs)boundaries.Add(input.TimeNanoseconds);
        foreach(ulong at in boundaries)
        {
            if(at>a.TimeNanoseconds)Require(a.Advance(at-a.TimeNanoseconds)==SimulationStatus.Ok);
            while(b.TimeNanoseconds<at)Require(b.Advance(Math.Min(257*original.Model.StepNanoseconds,at-b.TimeNanoseconds))==SimulationStatus.Ok);
            Require(a.ReadSnapshot(left)==b.ReadSnapshot(right)&&left.SequenceEqual(right));
        }
    }
    private static void Replay()
    {
        var asset=Asset();byte[] data=AssetCodec.Encode(asset);var decoded=AssetCodec.Decode(data);
        Require(BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(8))==AssetCodec.FormatVersion&&decoded.Model.Fidelity=="cycle_fuel_metering");
        Require(asset.Components.SequenceEqual(decoded.Components)&&data.SequenceEqual(AssetCodec.Encode(decoded)));Compare(asset,decoded);
        byte[] fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","gas-accumulator-pump-v16.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(fixture))=="72f0ee5b3180b763de091674565b2b8f2cbd61f80a3e4ab46e9694ad153a79c7");
        var original=AssetCodec.Decode(fixture);Require(original.Model.Fingerprint.ToString("x16")=="739f2baba8c669a0"&&!original.Model.HasFuelInjectors);Compare(original,AssetCodec.Decode(AssetCodec.Encode(original)));
        var final=original.CreatePlayback();Require(final.Advance(original.DurationNanoseconds)==SimulationStatus.Ok);
        AssetChecks.Reference(final,(24,Field.Volume,3.445212037240023e-5,1e-10),(17,Field.SlipSpeed,0,1e-8));
    }
    private static void Corruption()
    {
        var asset=Asset();byte[] source=AssetCodec.Encode(asset);int counts=78+Encoding.UTF8.GetByteCount(asset.Name),record=counts+156+44*asset.Nodes.Count+156*asset.Components.Count+24*2+36+40*2;
        void Reject(byte[] data){SHA256.HashData(data.AsSpan(0,data.Length-32)).CopyTo(data,data.Length-32);Throws<ArgumentException>(()=>AssetCodec.Decode(data));}
        foreach(int count in new[]{-1,65,int.MaxValue}){var bad=source.ToArray();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(counts+108),count);Reject(bad);}
        foreach(var change in new[]{(0,1),(4,2),(52,(int)Unit.Joule)}){var bad=source.ToArray();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(record+change.Item1),change.Item2);Reject(bad);}
        var duplicate=source.Take(record+56).Concat(source.Skip(record).Take(56)).Concat(source.Skip(record+56)).ToArray();BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts+108),2);Reject(duplicate);
        var missing=source.Take(record).Concat(source.Skip(record+56)).ToArray();BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts+108),0);Reject(missing);
        var downgraded=source.Take(counts+108).Concat(source.Skip(counts+156).Take(record-counts-156)).Concat(source.Skip(record+56)).ToArray();BinaryPrimitives.WriteInt32LittleEndian(downgraded.AsSpan(8),16);Reject(downgraded);
    }
}
