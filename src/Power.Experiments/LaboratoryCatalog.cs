// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Power.Experiments;

public sealed record LaboratoryEntry(
    [property: JsonRequired] string Name,
    [property: JsonRequired] string AssetName,
    [property: JsonRequired] string DisplayName,
    [property: JsonRequired] bool AgentExample);

/// <summary>Shared inventory for CLI discovery, agent examples and serial laboratory verification.</summary>
public static class LaboratoryCatalog
{
    public const string Schema = "power.laboratory_catalog.v1";
    public static IReadOnlyList<LaboratoryEntry> Entries { get; } = Array.AsReadOnly(Load());

    public static string ReadSource(string name)
    {
        if (!Entries.Any(lab => lab.Name == name)) throw new ArgumentException("Unknown laboratory.", nameof(name));
        return Resource($"power.{name}.power.json");
    }

    private sealed record CatalogDocument(
        [property: JsonRequired] string Schema,
        [property: JsonRequired] LaboratoryEntry[] Laboratories);

    private static LaboratoryEntry[] Load()
    {
        var document = JsonSerializer.Deserialize<CatalogDocument>(Resource("power.laboratory-catalog.json"), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        }) ?? throw new InvalidDataException("Missing laboratory catalog.");
        if (document.Schema != Schema || document.Laboratories is not { Length: > 0 })
            throw new InvalidDataException("Invalid laboratory catalog schema or empty inventory.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        var assets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var lab in document.Laboratories)
        {
            if (lab is null || string.IsNullOrEmpty(lab.Name) || !char.IsAsciiLetterLower(lab.Name[0]) ||
                lab.Name.Any(c => !char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '-') ||
                string.IsNullOrEmpty(lab.AssetName) || !char.IsAsciiLetter(lab.AssetName[0]) ||
                lab.AssetName.Any(c => !char.IsAsciiLetterOrDigit(c)) || string.IsNullOrWhiteSpace(lab.DisplayName) ||
                !names.Add(lab.Name) || !assets.Add(lab.AssetName))
                throw new InvalidDataException("Laboratories require unique names, safe asset names and display names.");
        }
        if (!document.Laboratories.Any(lab => lab.Name == "electrothermal" && lab.AgentExample))
            throw new InvalidDataException("The default electrothermal agent example is missing.");
        return document.Laboratories;
    }

    private static string Resource(string name)
    {
        using var stream = typeof(LaboratoryCatalog).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidDataException($"Missing bundled laboratory resource: {name}.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
