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

internal static class FuelFilmAssetChecks
{
 internal static IEnumerable<(string Name,Action Run)> All=>[("fuel film asset / complete v18 phase references / every boundary / authentic v17",Replay),("fuel film asset / typed counts and phase units / duplicate missing and downgrade",Corruption)];
 private static PowerAsset Asset()=>PowerAsset.Create(FuelFilmChecks.Model(),"Finite liquid film",new string('b',64),2_000_000_000,100_000_000,[],[]);
 private static void Compare(PowerAsset old,PowerAsset updated)
 {
  var a=old.CreatePlayback();var b=updated.CreatePlayback();var left=new Scalar[a.Model.OutputCount];var right=new Scalar[b.Model.OutputCount];var times=new SortedSet<ulong>{0,old.DurationNanoseconds};for(ulong at=old.SampleEveryNanoseconds;at<old.DurationNanoseconds;at+=old.SampleEveryNanoseconds)times.Add(at);foreach(var input in old.Inputs)times.Add(input.TimeNanoseconds);
  foreach(ulong at in times){if(at>a.TimeNanoseconds)Require(a.Advance(at-a.TimeNanoseconds)==SimulationStatus.Ok);while(b.TimeNanoseconds<at)Require(b.Advance(Math.Min(257*old.Model.StepNanoseconds,at-b.TimeNanoseconds))==SimulationStatus.Ok);Require(a.ReadSnapshot(left)==b.ReadSnapshot(right)&&left.SequenceEqual(right));}
 }
 private static void Replay()
 {
  var asset=Asset();byte[] bytes=AssetCodec.Encode(asset);var decoded=AssetCodec.Decode(bytes);Require(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8))==AssetCodec.FormatVersion&&decoded.Model.Fidelity=="finite_liquid_film_evaporation");Require(asset.Components.SequenceEqual(decoded.Components)&&bytes.SequenceEqual(AssetCodec.Encode(decoded)));Compare(asset,decoded);
  byte[] fixture=File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"Fixtures","metered-fired-cylinder-v17.powerasset"));Require(Convert.ToHexStringLower(SHA256.HashData(fixture))=="d51dc0968bc3d154b2c3b227f74b7210215d4ee00c5a47e8dd16dc1d71cd5da1");
  var original=AssetCodec.Decode(fixture);Require(original.Model.Fingerprint.ToString("x16")=="099db1021c8df1fe"&&!original.Model.HasFuelFilms);Compare(original,AssetCodec.Decode(AssetCodec.Encode(original)));var final=original.CreatePlayback();Require(final.Advance(original.DurationNanoseconds)==SimulationStatus.Ok);AssetChecks.Reference(final,(16,Field.TotalFuelDelivered,28e-6,1e-15),(15,Field.HeatReleased,1228.918308706072,1e-6));
 }
 private static void Corruption()
 {
  var asset=Asset();byte[] source=AssetCodec.Encode(asset);int counts=78+Encoding.UTF8.GetByteCount(asset.Name),record=counts+156+44*asset.Nodes.Count+156*asset.Components.Count+24+40;
  void Reject(byte[] data){SHA256.HashData(data.AsSpan(0,data.Length-32)).CopyTo(data,data.Length-32);Throws<ArgumentException>(()=>AssetCodec.Decode(data));}
  foreach(int count in new[]{-1,65,int.MaxValue}){var bad=source.ToArray();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(counts+112),count);Reject(bad);}
  foreach(var change in new[]{(0,1),(12,(int)Unit.Joule),(60,(int)Unit.Kelvin)}){var bad=source.ToArray();BinaryPrimitives.WriteInt32LittleEndian(bad.AsSpan(record+change.Item1),change.Item2);Reject(bad);}
  var duplicate=source.Take(record+64).Concat(source.Skip(record).Take(64)).Concat(source.Skip(record+64)).ToArray();BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts+112),2);Reject(duplicate);
  var missing=source.Take(record).Concat(source.Skip(record+64)).ToArray();BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts+112),0);Reject(missing);
  var old=source.Take(counts+112).Concat(source.Skip(counts+156).Take(record-counts-156)).Concat(source.Skip(record+64)).ToArray();BinaryPrimitives.WriteInt32LittleEndian(old.AsSpan(8),17);Reject(old);
 }
}
