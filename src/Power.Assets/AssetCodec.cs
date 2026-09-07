using System.IO;
using System.Security.Cryptography;
using System.Text;
using Power.Core;

namespace Power.Assets;

public sealed class AssetFormatException(string message) : ArgumentException(message);

/// <summary>Versioned little-endian model/experiment data, never executable code or serialized solver factors.</summary>
public static class AssetCodec
{
    public const int FormatVersion = 1, MaxBytes = 1_048_576;
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("POWERAST");
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public static byte[] Encode(PowerAsset asset)
    {
        if (asset is null) throw new ArgumentNullException(nameof(asset));
        // Check the exact maximum allocation before writing caller-provided data.
        long size = 8 + 4 + 8 + 8 + 8 + 8 + 2 + Utf8.GetByteCount(asset.Name) + 32 + 4 * 4 +
            44L * asset.Nodes.Count + 156L * asset.Components.Count + 24L * asset.Inputs.Count + 33L * asset.Checks.Count + 32;
        if (size > MaxBytes) throw new AssetFormatException("Asset exceeds 1 MiB.");
        using var stream = new MemoryStream((int)size);
        using var writer = new BinaryWriter(stream, Utf8, true);
        writer.Write(Magic); writer.Write(FormatVersion); writer.Write(asset.Model.Fingerprint);
        writer.Write(asset.Model.StepNanoseconds); writer.Write(asset.DurationNanoseconds); writer.Write(asset.SampleEveryNanoseconds);
        byte[] name = Utf8.GetBytes(asset.Name); writer.Write((ushort)name.Length); writer.Write(name);
        for (int i = 0; i < 32; ++i) writer.Write(Convert.ToByte(asset.SourceSha256.Substring(i * 2, 2), 16));
        writer.Write(asset.Nodes.Count); writer.Write(asset.Components.Count); writer.Write(asset.Inputs.Count); writer.Write(asset.Checks.Count);
        void Quantity(Quantity quantity) { writer.Write(quantity.Value); writer.Write((int)quantity.Unit); }
        foreach (var n in asset.Nodes)
        {
            writer.Write(n.Id); writer.Write((int)n.Domain); Quantity(n.Storage); Quantity(n.Initial); Quantity(n.Position);
        }
        foreach (var c in asset.Components)
        {
            writer.Write(c.Id); writer.Write((int)c.Kind); writer.Write(c.NodeA); writer.Write(c.NodeB); writer.Write(c.HeatNode);
            writer.Write(c.InputChannel); Quantity(c.InitialInput); Quantity(c.Stiffness); Quantity(c.Damping); Quantity(c.RestAngle);
            writer.Write(c.Ratio); Quantity(c.Resistance); Quantity(c.Inductance); Quantity(c.Coupling); Quantity(c.InitialCurrent);
            Quantity(c.Conductance); Quantity(c.AmbientTemperature);
        }
        foreach (var input in asset.Inputs) { writer.Write(input.TimeNanoseconds); writer.Write(input.Channel); writer.Write(input.Value); }
        foreach (var c in asset.Checks)
        {
            writer.Write(c.ObjectId); writer.Write((int)c.Field);
            writer.Write((byte)((c.Min.HasValue ? 1 : 0) | (c.Max.HasValue ? 2 : 0) | (c.AbsMax.HasValue ? 4 : 0)));
            writer.Write(c.Min ?? 0); writer.Write(c.Max ?? 0); writer.Write(c.AbsMax ?? 0);
        }
        writer.Flush();
        using var sha = SHA256.Create();
        byte[] digest = sha.ComputeHash(stream.GetBuffer(), 0, (int)stream.Length);
        writer.Write(digest); writer.Flush();
        if (stream.Length != size) throw new InvalidOperationException("Asset size calculation diverged from the encoder.");
        return stream.ToArray();
    }

    public static PowerAsset Decode(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 126 || payload.Length > MaxBytes) throw new AssetFormatException("Invalid asset length (maximum 1 MiB).");
        byte[] bytes = payload.ToArray(); // Own a stable buffer before validating it.
        int contentLength = bytes.Length - 32;
        using var sha = SHA256.Create();
        byte[] digest = sha.ComputeHash(bytes, 0, contentLength);
        if (!digest.AsSpan().SequenceEqual(bytes.AsSpan(contentLength))) throw new AssetFormatException("Asset SHA-256 integrity check failed.");
        try
        {
            using var stream = new MemoryStream(bytes, 0, contentLength, false);
            using var reader = new BinaryReader(stream, Utf8);
            if (!reader.ReadBytes(8).AsSpan().SequenceEqual(Magic)) throw new AssetFormatException("Unknown asset signature.");
            if (reader.ReadInt32() != FormatVersion) throw new AssetFormatException("Unsupported asset format version.");
            ulong fingerprint = reader.ReadUInt64(), step = reader.ReadUInt64(), duration = reader.ReadUInt64(), sample = reader.ReadUInt64();
            int nameLength = reader.ReadUInt16();
            if (nameLength is < 1 or > 512 || stream.Length - stream.Position < nameLength + 32) throw new AssetFormatException("Invalid asset name length.");
            string name = Utf8.GetString(reader.ReadBytes(nameLength));
            string sourceHash = BitConverter.ToString(reader.ReadBytes(32)).Replace("-", "").ToLowerInvariant();
            int Count(int maximum, int minimum = 0)
            {
                int count = reader.ReadInt32();
                return count >= minimum && count <= maximum ? count : throw new AssetFormatException("Asset count exceeds the supported limits.");
            }
            int nodes = Count(CompiledModel.MaxNodes, 1), components = Count(CompiledModel.MaxComponents),
                inputs = Count(PowerAsset.MaxScheduledInputs), checks = Count(PowerAsset.MaxChecks);
            if (stream.Length - stream.Position != 44L * nodes + 156L * components + 24L * inputs + 33L * checks)
                throw new AssetFormatException("Asset length does not match its declared counts.");
            Quantity Quantity() => new(reader.ReadDouble(), (Unit)reader.ReadInt32());
            var ns = new NodeDefinition[nodes]; var cs = new ComponentDefinition[components];
            for (int i = 0; i < nodes; ++i) ns[i] = new(reader.ReadUInt32(), (Domain)reader.ReadInt32(), Quantity(), Quantity(), Quantity());
            for (int i = 0; i < components; ++i)
                cs[i] = new()
                {
                    Id = reader.ReadUInt32(), Kind = (ComponentKind)reader.ReadInt32(), NodeA = reader.ReadUInt32(),
                    NodeB = reader.ReadUInt32(), HeatNode = reader.ReadUInt32(), InputChannel = reader.ReadUInt64(),
                    InitialInput = Quantity(), Stiffness = Quantity(), Damping = Quantity(), RestAngle = Quantity(),
                    Ratio = reader.ReadDouble(), Resistance = Quantity(), Inductance = Quantity(), Coupling = Quantity(),
                    InitialCurrent = Quantity(), Conductance = Quantity(), AmbientTemperature = Quantity()
                };
            var schedule = new ScheduledInput[inputs];
            for (int i = 0; i < inputs; ++i) schedule[i] = new(reader.ReadUInt64(), reader.ReadUInt64(), reader.ReadDouble());
            var conditions = new AssetCheck[checks];
            for (int i = 0; i < checks; ++i)
            {
                uint id = reader.ReadUInt32(); var field = (Field)reader.ReadInt32(); byte flags = reader.ReadByte();
                if (flags is < 1 or > 7) throw new AssetFormatException("Unknown output-check flags.");
                double min = reader.ReadDouble(), max = reader.ReadDouble(), abs = reader.ReadDouble();
                if (((flags & 1) == 0 && min != 0) || ((flags & 2) == 0 && max != 0) || ((flags & 4) == 0 && abs != 0))
                    throw new AssetFormatException("Unused output-check values must be zero.");
                conditions[i] = new(id, field, (flags & 1) != 0 ? min : null, (flags & 2) != 0 ? max : null, (flags & 4) != 0 ? abs : null);
            }
            var asset = PowerAsset.Create(new() { StepNanoseconds = step, Nodes = ns, Components = cs }, name, sourceHash, duration, sample, schedule, conditions);
            if (asset.Model.Fingerprint != fingerprint) throw new AssetFormatException("Model fingerprint differs; rebuild the asset with the current solver.");
            return asset;
        }
        catch (EndOfStreamException) { throw new AssetFormatException("Truncated asset."); }
        catch (DecoderFallbackException) { throw new AssetFormatException("Invalid UTF-8 in asset name."); }
    }
}
