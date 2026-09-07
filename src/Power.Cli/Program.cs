// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Text.Json;
using Power.Experiments;
using Power.Assets;
using System.Security.Cryptography;

var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
try
{
    if (args.Length == 0 || args[0] is "--help" or "-h")
    {
        Console.WriteLine("Power! model lab (.NET 10 / C# 14)\nUsage: power <model.power.json> [--output report.json] [--step-ns integer]\n       power export <model.power.json> --output model.powerasset [--name title] [--step-ns integer]");
        return 0;
    }
    bool export = args[0] == "export";
    if (export && args.Length < 2) throw new ArgumentException("Missing model path.");
    string modelPath = args[export ? 1 : 0]; string? output = null; ulong? step = null;
    string name = "Power model";
    for (int i = export ? 2 : 1; i < args.Length; ++i)
    {
        if (++i >= args.Length) throw new ArgumentException("Missing option value.");
        switch (args[i - 1])
        {
            case "--output": output = args[i]; break;
            case "--step-ns": step = ulong.Parse(args[i], System.Globalization.CultureInfo.InvariantCulture); break;
            case "--name" when export: name = args[i]; break;
            default: throw new ArgumentException($"Unknown option: {args[i - 1]}.");
        }
    }
    var document = ModelDocument.Load(modelPath);
    if (step.HasValue) document = document with { Model = document.Model with { StepNanoseconds = step.Value } };
    if (export)
    {
        if (output is null || !output.EndsWith(".powerasset", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Export requires --output with a .powerasset extension.");
        var asset = document.ToAsset(name);
        byte[] bytes = AssetCodec.Encode(asset);
        string absolute = Path.GetFullPath(output);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        string temporary = absolute + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temporary, bytes); File.Move(temporary, absolute, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        Console.WriteLine(JsonSerializer.Serialize(new { exported = true, format = "power.asset.v1", path = absolute,
            name = asset.Name, byte_count = bytes.Length, model_fingerprint = asset.Model.Fingerprint.ToString("x16"),
            source_sha256 = asset.SourceSha256, asset_sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes)) }, options));
        return 0;
    }
    var report = ExperimentRunner.Evaluate(document);
    string json = JsonSerializer.Serialize(report, options) + Environment.NewLine;
    if (output is null) Console.Write(json);
    else
    {
        string absolute = Path.GetFullPath(output);
        Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
        File.WriteAllText(absolute, json);
        Console.WriteLine($"{(report.Passed ? "PASS" : "FAIL")} | {report.Model.Fingerprint} | {report.Replay.BoundariesChecked} replay boundaries | {absolute}");
    }
    return report.Passed ? 0 : 2;
}
catch (Exception error) when (error is ArgumentException or IOException or JsonException or FormatException or OverflowException)
{
    Console.Error.WriteLine(JsonSerializer.Serialize(new { passed = false, error = error.Message }, options));
    return 1;
}
