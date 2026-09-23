// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Cross-platform .NET 10 file-based app: dotnet run --file tools/Build.cs -- verify
// The .NET 10 file-based app format is a single source file, so this file also
// carries the native verification toolchain: the Zig toolchain installer, the
// native verifier with its source audit and baseline comparison, the P/Invoke
// model lab for the libpower C ABI, its test suite, and a Python-semantics JSON
// DOM shared by those tools.
#:property AllowUnsafeBlocks=true
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;


static string SourcePath([CallerFilePath] string path = "") => path;
string root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(SourcePath())!, ".."));
string cachedDotnet = Path.Combine(root, ".cache", "dotnet", OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
string dotnet = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? (File.Exists(cachedDotnet) ? cachedDotnet : "dotnet");
string mode = args.Length == 0 ? "verify" : args[0];
Process? activeProcess = null;
Console.CancelKeyPress += (_, _) =>
{
    try { if (activeProcess is { HasExited: false }) activeProcess.Kill(entireProcessTree: true); }
    catch (InvalidOperationException) { }
};

async Task Run(string executable, params string[] arguments)
{
    var start = new ProcessStartInfo(executable) { WorkingDirectory = root, UseShellExecute = false };
    foreach (string argument in arguments) start.ArgumentList.Add(argument);
    start.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";
    start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
    using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start " + executable);
    activeProcess = process;
    await process.WaitForExitAsync();
    activeProcess = null;
    if (process.ExitCode != 0) throw new InvalidOperationException($"{Path.GetFileName(executable)} exited with {process.ExitCode}.");
}

try
{
    if (mode is not ("build" or "verify" or "native-verify" or "unity-validate" or "unity-test" or "install-zig" or "model-lab" or "native-selftest"))
        throw new ArgumentException("Usage: dotnet run --file tools/Build.cs -- [build|verify|native-verify|install-zig|model-lab <model>|native-selftest <library>|unity-validate|unity-test]");
    if (mode == "install-zig")
    {
        ZigToolchain.Install(root);
        return 0;
    }
    if (mode == "model-lab")
        return ModelLab.RunCli(root, args[1..]);
    if (mode == "native-selftest")
    {
        if (args.Length < 2) throw new ArgumentException("Usage: native-selftest <path to libpower>");
        return ModelLabTests.Run(args[1], root);
    }
    if (mode == "native-verify")
        return NativeVerify.Run(root, sourceOnly: args.Contains("--source-only"));
    // Deliberately serialize compilation and tests to keep desktop memory pressure bounded.
    await Run(dotnet, "build", "Power.slnx", "-c", "Release", "--nologo", "--disable-build-servers", "-m:1", "-p:UseSharedCompilation=false");
    string cli = Path.Combine(root, "src", "Power.Cli", "bin", "Release", "net10.0", "Power.Cli.dll");
    foreach (var lab in new[] { (File: "electrothermal", Asset: "Electrothermal", Name: "Electrothermal laboratory"),
        (File: "thermal-network", Asset: "ThermalNetwork", Name: "Thermal exchange laboratory"),
        (File: "sealed-cylinder", Asset: "SealedCylinder", Name: "Sealed cylinder laboratory"),
        (File: "gas-network", Asset: "GasNetwork", Name: "Gas exchange laboratory"),
        (File: "moving-cylinder", Asset: "MovingCylinder", Name: "Moving cylinder laboratory"),
        (File: "crank-timed-cylinder", Asset: "CrankTimedCylinder", Name: "Crank-timed cylinder laboratory"),
        (File: "fired-cylinder", Asset: "FiredCylinder", Name: "Premixed fired cylinder laboratory"),
        (File: "fired-clutch", Asset: "FiredClutch", Name: "Fired clutch laboratory"),
        (File: "fired-pump", Asset: "FiredPump", Name: "Fired pump transmission laboratory"),
        (File: "fired-hydraulic", Asset: "FiredHydraulic", Name: "Fired hydraulic transmission laboratory"),
        (File: "fired-converter", Asset: "FiredConverter", Name: "Fired torque converter laboratory"),
        (File: "fired-planetary", Asset: "FiredPlanetary", Name: "Fired planetary transmission laboratory") })
        await Run(dotnet, cli, "export", $"assets/labs/{lab.File}.power.json", "--name", lab.Name,
            "--output", $"Unity/Assets/Generated/Resources/{lab.Asset}.powerasset");
    if (mode == "verify")
    {
        foreach (string project in new[] { "Power.Tests", "Power.UnityCompatibility", "Power.Mcp.Tests" })
            await Run(dotnet, Path.Combine(root, "tests", project, "bin", "Release", "net10.0", project + ".dll"));
        foreach (string lab in new[] { "electrothermal", "thermal-network", "sealed-cylinder", "gas-network", "moving-cylinder", "crank-timed-cylinder", "fired-cylinder", "fired-clutch", "fired-planetary", "fired-converter", "fired-hydraulic" })
            await Run(dotnet, cli, $"assets/labs/{lab}.power.json", "--output", $"artifacts/reports/{lab}.json");
        Console.WriteLine("Managed verification passed. Unity Editor/Play/IL2CPP require separate Unity validation.");
        return NativeVerify.Run(root);
    }
    if (mode.StartsWith("unity-", StringComparison.Ordinal))
    {
        string editor = Environment.GetEnvironmentVariable("POWER_UNITY_EDITOR") ?? "";
        if (!File.Exists(editor)) throw new ArgumentException("Set POWER_UNITY_EDITOR to the Unity 6000.6.0f1 executable.");
        string project = Path.Combine(root, "Unity");
        string reports = Path.Combine(root, "artifacts", "unity");
        Directory.CreateDirectory(reports);
        await Run(editor, "-batchmode", "-nographics", "-quit", "-projectPath", project,
            "-executeMethod", "Power.Studio.Editor.ProjectSetup.Validate", "-logFile", Path.Combine(reports, "validate.log"));
        if (mode == "unity-test")
            foreach (string platform in new[] { "EditMode", "PlayMode" })
                await Run(editor, "-batchmode", "-projectPath", project, "-runTests", "-testPlatform", platform,
                    "-testResults", Path.Combine(reports, platform + ".xml"), "-logFile", Path.Combine(reports, platform + ".log"));
    }
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

internal sealed class ToolError(string message) : Exception(message);

internal abstract class JValue
{
    // Virtual object access so untyped nodes report a clear error instead of
    // forcing casts at every call site.
    public virtual JValue this[string key]
    {
        get => throw new ToolError("expected an object");
        set => throw new ToolError("expected an object");
    }

    public virtual bool ContainsKey(string key) => false;
}

internal sealed class JNull : JValue
{
    public static readonly JNull Instance = new();
    private JNull() { }
}

internal sealed class JBool(bool value) : JValue
{
    public bool Value { get; } = value;
}

internal sealed class JStr(string text) : JValue
{
    public string Text { get; } = text;
}

internal sealed class JInt(BigInteger value) : JValue
{
    public BigInteger Value { get; } = value;
}

internal sealed class JNum(double value) : JValue
{
    public double Value { get; } = value;
}

internal sealed class JArr : JValue, IEnumerable<JValue>
{
    private readonly List<JValue> items;
    public JArr() => items = [];
    public JArr(IEnumerable<JValue> items) => this.items = [.. items];
    public int Count => items.Count;
    public JValue this[int index] => items[index];
    public void Add(JValue value) => items.Add(value);
    public void Reverse() => items.Reverse();
    public IEnumerable<JValue> Items => items;
    public IEnumerator<JValue> GetEnumerator() => items.GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

internal sealed class JObj : JValue
{
    private readonly List<KeyValuePair<string, JValue>> items = [];
    public int Count => items.Count;
    public IEnumerable<string> Keys => items.Select(pair => pair.Key);
    public IEnumerable<KeyValuePair<string, JValue>> Pairs => items;

    public override bool ContainsKey(string key) => items.Any(pair => pair.Key == key);

    public override JValue this[string key]
    {
        get => items.First(pair => pair.Key == key).Value;
        set
        {
            for (int index = 0; index < items.Count; index++)
                if (items[index].Key == key)
                {
                    items[index] = new KeyValuePair<string, JValue>(key, value);
                    return;
                }
            items.Add(new KeyValuePair<string, JValue>(key, value));
        }
    }

    public bool TryGet(string key, out JValue value)
    {
        foreach (var (existing, candidate) in items.Select(pair => (pair.Key, pair.Value)))
            if (existing == key)
            {
                value = candidate;
                return true;
            }
        value = JNull.Instance;
        return false;
    }

    public bool Remove(string key) => items.RemoveAll(pair => pair.Key == key) > 0;
}

internal static class JValues
{
    // Equality follows Python semantics: unordered object comparison, ordered
    // array comparison, numeric equality across integer and float kinds, and
    // NaN never equal to itself.
    public static bool Equal(JValue? left, JValue? right)
    {
        if (left is JNull && right is JNull) return true;
        if (left is JStr text && right is JStr other) return text.Text == other.Text;
        if (left is JBool flag && right is JBool otherFlag) return flag.Value == otherFlag.Value;
        if (left is JNum number && right is JNum otherNumber) return number.Value == otherNumber.Value;
        if (left is JInt integer && right is JInt otherInteger) return integer.Value == otherInteger.Value;
        if (left is JNum || right is JNum || left is JInt || right is JInt)
            return AsNumber(left) == AsNumber(right);
        if (left is JObj obj && right is JObj otherObj)
        {
            if (obj.Count != otherObj.Count) return false;
            foreach (var (key, value) in obj.Pairs)
            {
                if (!otherObj.TryGet(key, out JValue? candidate) || !Equal(value, candidate)) return false;
            }
            return true;
        }
        if (left is JArr array && right is JArr otherArray)
        {
            if (array.Count != otherArray.Count) return false;
            return array.Items.Zip(otherArray.Items, Equal).All(matched => matched);
        }
        return false;
    }

    public static double AsNumber(JValue? value) => value switch
    {
        JInt integer => (double)integer.Value,
        JNum number => number.Value,
        _ => throw new ToolError("expected a number"),
    };

    public static JValue Clone(JValue value) => value switch
    {
        JNull => JNull.Instance,
        JBool flag => new JBool(flag.Value),
        JStr text => new JStr(text.Text),
        JInt integer => new JInt(integer.Value),
        JNum number => new JNum(number.Value),
        JArr array => new JArr(array.Items.Select(Clone)),
        JObj obj => CloneObject(obj),
        _ => throw new ToolError("unsupported JSON node"),
    };

    private static JObj CloneObject(JObj obj)
    {
        var result = new JObj();
        foreach (var (key, value) in obj.Pairs) result[key] = Clone(value);
        return result;
    }
}

internal static class PowerJson
{
    public static JValue Parse(string source, bool rejectDuplicateKeys, bool rejectNonFiniteConstants) =>
        new JsonParser(source, rejectDuplicateKeys, rejectNonFiniteConstants).ParseDocument();

    public static string Write(JValue value)
    {
        var builder = new StringBuilder();
        WriteNode(value, builder, 0);
        return builder.ToString();
    }

    private static void WriteNode(JValue value, StringBuilder builder, int depth)
    {
        string indent = new(' ', 2 * depth), inner = new(' ', 2 * (depth + 1));
        switch (value)
        {
            case JNull:
                builder.Append("null");
                break;
            case JBool flag:
                builder.Append(flag.Value ? "true" : "false");
                break;
            case JInt integer:
                builder.Append(integer.Value.ToString(CultureInfo.InvariantCulture));
                break;
            case JNum number:
                builder.Append(FormatDouble(number.Value));
                break;
            case JStr text:
                WriteString(text.Text, builder);
                break;
            case JArr array:
                if (array.Count == 0)
                {
                    builder.Append("[]");
                    break;
                }
                builder.Append("[\n");
                foreach (var (item, index) in array.Items.Select((item, index) => (item, index)))
                {
                    builder.Append(inner);
                    WriteNode(item, builder, depth + 1);
                    builder.Append(index + 1 == array.Count ? "\n" : ",\n");
                }
                builder.Append(indent).Append(']');
                break;
            case JObj obj:
                if (obj.Count == 0)
                {
                    builder.Append("{}");
                    break;
                }
                builder.Append("{\n");
                foreach (var (pair, index) in obj.Pairs.Select((pair, index) => (pair, index)))
                {
                    builder.Append(inner);
                    WriteString(pair.Key, builder);
                    builder.Append(": ");
                    WriteNode(pair.Value, builder, depth + 1);
                    builder.Append(index + 1 == obj.Count ? "\n" : ",\n");
                }
                builder.Append(indent).Append('}');
                break;
            default:
                throw new ToolError("unsupported JSON node");
        }
    }

    private static string FormatDouble(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            throw new ToolError("Out of range float values are not JSON compliant");
        string text = value.ToString(CultureInfo.InvariantCulture);
        if (!text.Contains('.') && !text.Contains('E') && !text.Contains('e')) text += ".0";
        return text.Replace("E", "e");
    }

    private static void WriteString(string text, StringBuilder builder)
    {
        builder.Append('"');
        foreach (char character in text)
        {
            switch (character)
            {
                case '"': builder.Append("\\\""); break;
                case '\\': builder.Append("\\\\"); break;
                case '\b': builder.Append("\\b"); break;
                case '\f': builder.Append("\\f"); break;
                case '\n': builder.Append("\\n"); break;
                case '\r': builder.Append("\\r"); break;
                case '\t': builder.Append("\\t"); break;
                case < ' ': builder.Append("\\u").Append(((int)character).ToString("x4")); break;
                default: builder.Append(character); break;
            }
        }
        builder.Append('"');
    }

    private sealed class JsonParser(string source, bool rejectDuplicateKeys, bool rejectNonFiniteConstants)
    {
        private int position;

        public JValue ParseDocument()
        {
            JValue value = ParseValue(0);
            SkipWhitespace();
            if (position != source.Length) throw new ToolError($"Unexpected data at character {position}");
            return value;
        }

        private JValue ParseValue(int depth)
        {
            if (depth > 900) throw new ToolError("maximum JSON nesting depth exceeded");
            SkipWhitespace();
            if (position >= source.Length) throw new ToolError("Unexpected end of input");
            char character = source[position];
            switch (character)
            {
                case '{':
                    return ParseObject(depth);
                case '[':
                    return ParseArray(depth);
                case '"':
                    return new JStr(ParseString());
                case 't':
                    Expect("true");
                    return new JBool(true);
                case 'f':
                    Expect("false");
                    return new JBool(false);
                case 'n':
                    Expect("null");
                    return JNull.Instance;
                case 'N':
                    return ParseNonFinite("NaN", double.NaN);
                case 'I':
                    return ParseNonFinite("Infinity", double.PositiveInfinity);
                default:
                    return ParseNumber();
            }
        }

        private JValue ParseNonFinite(string literal, double value)
        {
            Expect(literal);
            if (rejectNonFiniteConstants) throw new ToolError($"nonfinite JSON constant: {literal}");
            return new JNum(value);
        }

        private JValue ParseObject(int depth)
        {
            position++;
            var result = new JObj();
            SkipWhitespace();
            if (Consume('}')) return result;
            while (true)
            {
                SkipWhitespace();
                if (position >= source.Length || source[position] != '"')
                    throw new ToolError($"Expected an object key at character {position}");
                string key = ParseString();
                SkipWhitespace();
                if (!Consume(':')) throw new ToolError($"Expected ':' at character {position}");
                JValue value = ParseValue(depth + 1);
                if (result.ContainsKey(key))
                {
                    if (rejectDuplicateKeys) throw new ToolError($"duplicate JSON field: {key}");
                    result[key] = value;
                }
                else
                {
                    result[key] = value;
                }
                SkipWhitespace();
                if (Consume(',')) continue;
                if (Consume('}')) return result;
                throw new ToolError($"Expected ',' or '}}' at character {position}");
            }
        }

        private JValue ParseArray(int depth)
        {
            position++;
            var result = new JArr();
            SkipWhitespace();
            if (Consume(']')) return result;
            while (true)
            {
                result.Add(ParseValue(depth + 1));
                SkipWhitespace();
                if (Consume(',')) continue;
                if (Consume(']')) return result;
                throw new ToolError($"Expected ',' or ']' at character {position}");
            }
        }

        private string ParseString()
        {
            position++;
            var builder = new StringBuilder();
            while (position < source.Length)
            {
                char character = source[position++];
                if (character == '"') return builder.ToString();
                if (character == '\\')
                {
                    if (position >= source.Length) break;
                    character = source[position++];
                    switch (character)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u':
                            if (position + 4 > source.Length) throw new ToolError("truncated \\u escape");
                            builder.Append((char)Convert.ToInt32(source.Substring(position, 4), 16));
                            position += 4;
                            break;
                        default:
                            throw new ToolError($"invalid escape: \\{character}");
                    }
                }
                else if (character < ' ')
                {
                    throw new ToolError("control characters must be escaped in JSON strings");
                }
                else
                {
                    builder.Append(character);
                }
            }
            throw new ToolError("unterminated JSON string");
        }

        private JValue ParseNumber()
        {
            int start = position;
            bool fractional = false;
            if (position < source.Length && source[position] == '-') position++;
            ConsumeDigits();
            if (Consume('.')) { fractional = true; ConsumeDigits(); }
            if (position < source.Length && (source[position] == 'e' || source[position] == 'E'))
            {
                fractional = true;
                position++;
                if (position < source.Length && (source[position] == '+' || source[position] == '-')) position++;
                ConsumeDigits();
            }
            string token = source[start..position];
            if (token.Length == 0 || token == "-") throw new ToolError($"Expected a value at character {start}");
            return fractional
                ? new JNum(double.Parse(token, CultureInfo.InvariantCulture))
                : new JInt(BigInteger.Parse(token, CultureInfo.InvariantCulture));
        }

        private void ConsumeDigits()
        {
            while (position < source.Length && source[position] >= '0' && source[position] <= '9') position++;
        }

        private void Expect(string literal)
        {
            if (!source[position..].StartsWith(literal, StringComparison.Ordinal))
                throw new ToolError($"Invalid literal at character {position}");
            position += literal.Length;
        }

        private void SkipWhitespace()
        {
            while (position < source.Length && source[position] is ' ' or '\t' or '\n' or '\r') position++;
        }

        private bool Consume(char character)
        {
            if (position < source.Length && source[position] == character)
            {
                position++;
                return true;
            }
            return false;
        }
    }
}

internal static unsafe class ModelLab
{
    private static readonly (string Name, uint Code)[] UnitTable =
    [
        ("none", 0), ("kg_m2", 1), ("rad", 2), ("rad_s", 3), ("nm", 4), ("nm_rad", 5),
        ("nm_s_rad", 6), ("k", 7), ("j_k", 8), ("w_k", 9), ("ohm", 10), ("h", 11),
        ("nm_a", 12), ("a", 13), ("v", 14), ("j", 15), ("rpm", 16), ("deg", 17),
    ];
    private static readonly Dictionary<string, uint> Units = UnitTable.ToDictionary(unit => unit.Name, unit => unit.Code);

    private static readonly Dictionary<string, uint> Fields = new()
    {
        ["angle"] = 1, ["speed"] = 2, ["temperature"] = 3, ["current"] = 4, ["twist"] = 5,
        ["torque"] = 6, ["source_work"] = 16, ["heat_rejected"] = 17,
        ["stored_energy_change"] = 18, ["energy_residual"] = 19,
    };

    private static readonly Dictionary<string, uint> Kinds = new()
    {
        ["shaft"] = 1, ["dc_motor"] = 2, ["torque_source"] = 3, ["thermal_link"] = 4,
    };

    private static readonly Dictionary<uint, string[]> KindParameters = new()
    {
        [1] = ["stiffness", "damping", "rest_angle", "ratio"],
        [2] = ["resistance", "inductance", "coupling", "initial_current"],
        [3] = [],
        [4] = ["conductance", "ambient_temperature"],
    };

    private static readonly Dictionary<string, Api> LoadedApis = [];

    public static JValue ReadDocument(string path)
    {
        byte[] source = File.ReadAllBytes(path);
        if (source.Length > 1_048_576) throw new ToolError("model document exceeds 1 MiB");
        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(source);
        }
        catch (DecoderFallbackException)
        {
            throw new ToolError("model document is not valid UTF-8");
        }
        return PowerJson.Parse(text, rejectDuplicateKeys: true, rejectNonFiniteConstants: true);
    }

    public static JObj Evaluate(JValue document, string libraryPath)
    {
        string library = Path.GetFullPath(libraryPath);
        if (!File.Exists(library)) throw new ToolError($"library not found: {library}");
        Api api = LoadApi(library);

        void Ok(int status)
        {
            if (status != 0) throw new ToolError(AnsiString(api.status_string(status)));
        }

        using var prepared = PrepareDocument(document);
        ContextDesc contextDesc = Initialized<ContextDesc>();
        ulong contextHandle = 0, modelHandle = 0, model = 0;
        Ok(api.context_create(&contextDesc, &contextHandle));
        ulong context = contextHandle;
        var clock = Stopwatch.StartNew();
        try
        {
            Diagnostic diagnosticStorage = Initialized<Diagnostic>();
            Diagnostic* diagnostic = &diagnosticStorage;
            int status = api.model_compile(context, prepared.Desc, &modelHandle, diagnostic);
            if (status != 0)
                throw new ToolError($"compile: object {diagnostic->object_id}, field {AnsiString(diagnostic->field, 40)}: " +
                    $"{AnsiString(diagnostic->message, 160)} ({AnsiString(api.status_string(status))})");
            model = modelHandle;
            ModelInfo infoStorage = Initialized<ModelInfo>();
            ModelInfo* info = &infoStorage;
            Version versionStorage = Initialized<Version>();
            Version* version = &versionStorage;
            Ok(api.model_get_info(model, info));
            Ok(api.runtime_get_version(version));
            var channels = new Channel[info->channel_count];
            uint reported = 0;
            var channelsHandle = GCHandle.Alloc(channels, GCHandleType.Pinned);
            try
            {
                Ok(api.model_get_channels(model, (Channel*)channelsHandle.AddrOfPinnedObject(), (uint)channels.Length, &reported));
            }
            finally { channelsHandle.Free(); }
            HashSet<ulong> inputs = [.. channels.Where(channel => channel.direction == 1).Select(channel => channel.channel)];
            HashSet<ulong> outputs = [.. channels.Where(channel => channel.direction == 2).Select(channel => channel.channel)];
            if (prepared.Events.Values.Any(eventValues => eventValues.Any(entry => !inputs.Contains(entry.Channel))))
                throw new ToolError("event references an unknown input channel");
            foreach (var check in prepared.Checks)
                if (!outputs.Contains(CheckChannel(check.ObjectId, check.Field)))
                    throw new ToolError("check references an unknown output channel");

            JArr Run(ulong chunkTicks)
            {
                ulong instanceHandle = 0, instance;
                Ok(api.instance_create_from_model(model, &instanceHandle));
                instance = instanceHandle;
                var samples = new JArr();
                try
                {
                    var values = new Scalar[outputs.Count];
                    var valuesHandle = GCHandle.Alloc(values, GCHandleType.Pinned);
                    try
                    {
                        Snapshot snapshotStorage = Initialized<Snapshot>();
                        Snapshot* snapshot = &snapshotStorage;
                        snapshot->values = (Scalar*)valuesHandle.AddrOfPinnedObject();
                        snapshot->value_capacity = (uint)values.Length;
                        ulong now = 0;
                        foreach (ulong boundary in prepared.Boundaries)
                        {
                            while (now < boundary)
                            {
                                ulong delta = Math.Min(boundary - now, chunkTicks * prepared.Desc->step_ns);
                                Ok(api.instance_step(instance, delta));
                                now += delta;
                            }
                            if (prepared.Events.TryGetValue(boundary, out List<EventValue>? eventValues))
                            {
                                var updates = new Scalar[eventValues.Count];
                                for (int index = 0; index < eventValues.Count; index++)
                                    updates[index] = new Scalar { channel = eventValues[index].Channel, value = eventValues[index].Value };
                                var updatesHandle = GCHandle.Alloc(updates, GCHandleType.Pinned);
                                try
                                {
                                    InputFrame frameStorage = Initialized<InputFrame>();
                                    InputFrame* frame = &frameStorage;
                                    frame->values = (Scalar*)updatesHandle.AddrOfPinnedObject();
                                    frame->value_count = (uint)updates.Length;
                                    Ok(api.instance_submit_inputs(instance, frame));
                                }
                                finally { updatesHandle.Free(); }
                            }
                            Ok(api.instance_read_snapshot(instance, snapshot));
                            var sample = new JObj
                            {
                                ["time_ns"] = new JInt(snapshot->simulation_time_ns),
                                ["state_hash"] = new JStr(snapshot->state_hash.ToString("x16")),
                            };
                            var sampleValues = new JObj();
                            foreach (Scalar scalar in values)
                                sampleValues[scalar.channel.ToString(CultureInfo.InvariantCulture)] = new JNum(scalar.value);
                            sample["values"] = sampleValues;
                            samples.Add(sample);
                        }
                    }
                    finally { valuesHandle.Free(); }
                }
                finally
                {
                    Ok(api.instance_destroy(instance));
                }
                return samples;
            }

            JArr samples = Run(1_000_000);
            JArr replay = Run(257);  // Deliberately different batching, same input timeline.
            bool matched = JValues.Equal(samples, replay);
            var results = new JArr();
            foreach (var check in prepared.Checks)
            {
                string checkKey = CheckChannel(check.ObjectId, check.Field).ToString(CultureInfo.InvariantCulture);
                double value = JValues.AsNumber(((JObj)samples[samples.Count - 1]["values"])[checkKey]);
                bool passed = (!check.HasMin || check.Min <= value) && (!check.HasMax || value <= check.Max)
                    && (!check.HasAbsMax || Math.Abs(value) <= check.AbsMax);
                var outcome = (JObj)JValues.Clone(check.Source);
                outcome["value"] = new JNum(value);
                outcome["passed"] = new JBool(passed);
                results.Add(outcome);
            }
            string machine = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x86_64", Architecture.Arm64 => "arm64",
                Architecture.X86 => "x86", Architecture.Arm => "arm",
                _ => RuntimeInformation.ProcessArchitecture.ToString(),
            };
            string system = OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsMacOS() ? "Darwin" : "Linux";
            var channelReport = new JArr();
            for (int index = 0; index < channels.Length; index++)
            {
                channelReport.Add(new JObj
                {
                    ["channel"] = new JStr(channels[index].channel.ToString(CultureInfo.InvariantCulture)),
                    ["object_id"] = new JInt(channels[index].object_id),
                    ["direction"] = new JStr(channels[index].direction == 1 ? "input" : "output"),
                    ["unit"] = new JStr(UnitTable.First(unit => unit.Code == channels[index].unit).Name),
                    ["quantity"] = new JStr(QuantityText(channels, index)),
                });
            }
            return new JObj
            {
                ["schema"] = new JStr("power.experiment_report.v1"),
                ["passed"] = new JBool(matched && results.Items.All(item => ((JBool)((JObj)item)["passed"]).Value)),
                ["runtime"] = new JObj
                {
                    ["version"] = new JStr(AnsiString(version->version_string)),
                    ["abi_version"] = new JInt(1),
                    ["library_sha256"] = new JStr(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(library)))),
                    ["machine"] = new JStr(machine),
                    ["system"] = new JStr(system),
                },
                ["model"] = new JObj
                {
                    ["fingerprint"] = new JStr(info->fingerprint.ToString("x16")),
                    ["step_ns"] = new JInt(info->step_ns),
                    ["node_count"] = new JInt(info->node_count),
                    ["component_count"] = new JInt(info->component_count),
                    ["state_count"] = new JInt(info->state_count),
                    ["fidelity"] = new JStr("linear_lumped"),
                    ["calibration"] = new JStr("unverified"),
                },
                ["replay"] = new JObj
                {
                    ["sample_hashes_match"] = new JBool(matched),
                    ["boundaries_checked"] = new JInt(samples.Count),
                    ["batch_ticks"] = new JArr { new JInt(1_000_000), new JInt(257) },
                },
                ["elapsed_seconds"] = new JNum(clock.Elapsed.TotalSeconds),
                ["channels"] = channelReport,
                ["checks"] = results,
                ["samples"] = samples,
            };
        }
        finally
        {
            if (model != 0) Ok(api.model_destroy(model));
            Ok(api.context_destroy(context));
        }
    }

    public static int RunCli(string root, string[] arguments)
    {
        string? model = null, library = null, output = null;
        for (int index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] == "--library" && index + 1 < arguments.Length) library = arguments[++index];
            else if (arguments[index] == "--output" && index + 1 < arguments.Length) output = arguments[++index];
            else if (model is null && !arguments[index].StartsWith("--")) model = arguments[index];
        }
        if (model is null)
        {
            Fail("usage: model-lab <model.power.json> [--library <path>] [--output <path>]");
            return 1;
        }
        library ??= Path.Combine(root, "legacy", "native", "build", "libpower.so");
        try
        {
            var report = Evaluate(ReadDocument(model), library);
            string rendered = PowerJson.Write(report) + "\n";
            if (output is null) Console.Out.Write(rendered);
            else File.WriteAllText(output, rendered);
            return ((JBool)report["passed"]).Value ? 0 : 2;
        }
        catch (Exception error) when (error is ToolError or IOException or UnauthorizedAccessException)
        {
            Fail(error.Message);
            return 1;
        }
    }

    private static void Fail(string message) =>
        Console.Error.WriteLine(PowerJson.Write(new JObj { ["passed"] = new JBool(false), ["error"] = new JStr(message) }));

    private static Api LoadApi(string library)
    {
        lock (LoadedApis)
        {
            if (LoadedApis.TryGetValue(library, out Api cached)) return cached;
        }
        IntPtr handle = System.Runtime.InteropServices.NativeLibrary.Load(library);
        var entry = System.Runtime.InteropServices.NativeLibrary.GetExport(handle, "pwr_get_api");
        var getApi = (delegate* unmanaged[Cdecl]<uint, uint, Api*, int>)entry;
        Api api;
        if (getApi(1, (uint)sizeof(Api), &api) != 0 || api.struct_size < (uint)sizeof(Api))
            throw new ToolError("libpower does not provide the compiled-model ABI extension");
        lock (LoadedApis)
        {
            LoadedApis[library] = api;
        }
        return api;
    }

    private static T Initialized<T>() where T : unmanaged
    {
        T result = default;
        uint* header = (uint*)&result;
        header[0] = 1;
        header[1] = (uint)sizeof(T);
        return result;
    }

    private static string AnsiString(byte* text)
    {
        if (text == null) return "";
        int length = 0;
        while (text[length] != 0) length++;
        return Encoding.ASCII.GetString(text, length);
    }

    private static string AnsiString(byte* text, int capacity)
    {
        int length = 0;
        while (length < capacity && text[length] != 0) length++;
        return Encoding.ASCII.GetString(text, length);
    }

    // Fixed-buffer reads go through array element indexing so no address of a
    // lambda-containing method's local is ever taken.
    private static string QuantityText(Channel[] channels, int index)
    {
        int length = 0;
        while (length < 32 && channels[index].quantity[length] != 0) length++;
        var bytes = new byte[length];
        for (int position = 0; position < length; position++) bytes[position] = channels[index].quantity[position];
        return Encoding.ASCII.GetString(bytes);
    }

    private static ulong CheckChannel(BigInteger objectId, string field) =>
        0x8000_0000_0000_0000UL | ((ulong)objectId << 8) | Fields[field];

    // ---- document validation ------------------------------------------------

    private sealed record EventValue(ulong Channel, double Value);

    private sealed record CheckSpec(JObj Source, BigInteger ObjectId, string Field,
        bool HasMin, double Min, bool HasMax, double Max, bool HasAbsMax, double AbsMax);

    private sealed class Prepared : IDisposable
    {
        public ModelDesc* Desc;
        public List<ulong> Boundaries = [];
        public Dictionary<ulong, List<EventValue>> Events = [];
        public List<CheckSpec> Checks = [];
        private readonly List<GCHandle> handles = [];
        public void Add(GCHandle handle) => handles.Add(handle);
        public void Dispose()
        {
            foreach (GCHandle handle in handles) handle.Free();
            handles.Clear();
        }
    }

    private static Prepared PrepareDocument(JValue document)
    {
        var prepared = new Prepared();
        try
        {
            BuildDescriptors(document, prepared);
            (prepared.Boundaries, prepared.Events, prepared.Checks) = BuildExperiment(document, prepared.Desc->step_ns);
            return prepared;
        }
        catch
        {
            prepared.Dispose();
            throw;
        }
    }

    private static void BuildDescriptors(JValue document, Prepared prepared)
    {
        CheckKeys(document, ["schema", "step_ns", "nodes", "components", "experiment"], ["description"]);
        if (document["schema"] is not JStr schema || schema.Text != "power.model.v1")
            throw new ToolError("schema: expected power.model.v1");
        foreach (var (name, limit, minimum) in new[] { ("nodes", 32, 1), ("components", 64, 0) })
            if (document[name] is not JArr list || list.Count < minimum || list.Count > limit)
                throw new ToolError($"{name}: expected between {minimum} and {limit} descriptors");
        var nodeSource = (JArr)document["nodes"];
        var nodes = new Node[nodeSource.Count];
        for (int index = 0; index < nodeSource.Count; index++)
        {
            string path = $"nodes[{index}]";
            JValue source = nodeSource[index];
            CheckKeys(source, ["id", "domain", "storage", "initial"], ["position"], path);
            if (source["domain"] is not JStr domain || (domain.Text != "rotational" && domain.Text != "thermal"))
                throw new ToolError($"{path}: unsupported domain");
            if (domain.Text == "rotational" && !source.ContainsKey("position"))
                throw new ToolError($"{path}: rotational position must be explicit");
            nodes[index] = new Node
            {
                id = (uint)Integer(source["id"], uint.MaxValue, $"{path}.id", 1),
                domain = (uint)(domain.Text == "rotational" ? 1 : 2),
                storage = ToQuantity(source["storage"], $"{path}.storage"),
                initial = ToQuantity(source["initial"], $"{path}.initial"),
                position = source.ContainsKey("position") ? ToQuantity(source["position"], $"{path}.position") : default,
            };
        }
        var componentSource = (JArr)document["components"];
        var components = new Component[componentSource.Count];
        for (int index = 0; index < componentSource.Count; index++)
        {
            string path = $"components[{index}]";
            JValue source = componentSource[index];
            CheckKeys(source, ["id", "kind", "node_a"], ["node_b", "heat_node", "input_channel", "initial_input", "parameters"], path);
            if (source["kind"] is not JStr kind || !Kinds.TryGetValue(kind.Text, out uint kindCode))
                throw new ToolError($"{path}: unsupported component kind");
            Component component = new()
            {
                id = (uint)Integer(source["id"], uint.MaxValue, $"{path}.id", 1),
                kind = kindCode,
                node_a = (uint)GetInteger((JObj)source, "node_a", uint.MaxValue, $"{path}.node_a", 1),
                node_b = (uint)GetInteger((JObj)source, "node_b", uint.MaxValue, $"{path}.node_b", 0),
                heat_node = (uint)GetInteger((JObj)source, "heat_node", uint.MaxValue, $"{path}.heat_node", 0),
                input_channel = (ulong)GetInteger((JObj)source, "input_channel", long.MaxValue, $"{path}.input_channel", 0),
                initial_input = source.ContainsKey("initial_input") ? ToQuantity(source["initial_input"], $"{path}.initial_input") : default,
            };
            JValue parameters = source.ContainsKey("parameters") ? source["parameters"] : new JObj();
            string[] expected = KindParameters[kindCode];
            if (kindCode == 4 && component.node_b != 0)
                expected = [.. expected.Where(name => name != "ambient_temperature")];
            CheckKeys(parameters, expected, [], $"{path}.parameters");
            foreach (string field in expected)
            {
                string fieldPath = $"{path}.{field}";
                switch (kindCode, field)
                {
                    case (1, "stiffness"): component.parameters.shaft.stiffness = ToQuantity(parameters[field], fieldPath); break;
                    case (1, "damping"): component.parameters.shaft.damping = ToQuantity(parameters[field], fieldPath); break;
                    case (1, "rest_angle"): component.parameters.shaft.rest_angle = ToQuantity(parameters[field], fieldPath); break;
                    case (1, "ratio"): component.parameters.shaft.ratio = ToNumber(parameters[field], fieldPath); break;
                    case (2, "resistance"): component.parameters.motor.resistance = ToQuantity(parameters[field], fieldPath); break;
                    case (2, "inductance"): component.parameters.motor.inductance = ToQuantity(parameters[field], fieldPath); break;
                    case (2, "coupling"): component.parameters.motor.coupling = ToQuantity(parameters[field], fieldPath); break;
                    case (2, "initial_current"): component.parameters.motor.initial_current = ToQuantity(parameters[field], fieldPath); break;
                    case (4, "conductance"): component.parameters.thermal.conductance = ToQuantity(parameters[field], fieldPath); break;
                    case (4, "ambient_temperature"): component.parameters.thermal.ambient_temperature = ToQuantity(parameters[field], fieldPath); break;
                }
            }
            components[index] = component;
        }
        var nodesHandle = GCHandle.Alloc(nodes, GCHandleType.Pinned);
        var componentsHandle = GCHandle.Alloc(components, GCHandleType.Pinned);
        var descStorage = new ModelDesc
        {
            abi_version = 1,
            struct_size = (uint)sizeof(ModelDesc),
            schema_version = 1,
            step_ns = (ulong)Integer(document["step_ns"], 1_000_000_000, "step_ns", 1),
            nodes = (Node*)nodesHandle.AddrOfPinnedObject(),
            node_count = (uint)nodes.Length,
            components = (Component*)componentsHandle.AddrOfPinnedObject(),
            component_count = (uint)components.Length,
        };
        var descHandle = GCHandle.Alloc(descStorage, GCHandleType.Pinned);
        prepared.Add(nodesHandle);
        prepared.Add(componentsHandle);
        prepared.Add(descHandle);
        prepared.Desc = (ModelDesc*)descHandle.AddrOfPinnedObject();
    }

    private static (List<ulong> Boundaries, Dictionary<ulong, List<EventValue>> Events, List<CheckSpec> Checks) BuildExperiment(
        JValue document, ulong stepNs)
    {
        CheckKeys(document["experiment"], ["duration_ns", "sample_every_ns"], ["events", "checks"], "experiment");
        var experiment = (JObj)document["experiment"];
        ulong duration = (ulong)Integer(experiment["duration_ns"], 3_600_000_000_000, "duration_ns", 1);
        ulong sample = (ulong)Integer(experiment["sample_every_ns"], duration, "sample_every_ns", 1);
        if (duration % stepNs != 0 || sample % stepNs != 0 || duration / stepNs > 10_000_000)
            throw new ToolError("experiment: times must align with step_ns; maximum 10 million ticks");
        var events = new Dictionary<ulong, List<EventValue>>();
        int eventCount = 0;
        JValue? eventList = experiment.ContainsKey("events") ? experiment["events"] : null;
        if (eventList is not null && eventList is not JArr)
            throw new ToolError("experiment: maximum 10000 events and 10000 sample intervals");
        if (eventList is JArr counted) eventCount = counted.Count;
        if (eventCount > 10000 || duration / sample > 10000)
            throw new ToolError("experiment: maximum 10000 events and 10000 sample intervals");
        if (eventList is JArr list)
        {
            long previous = -1;
            foreach (JValue item in list.Items)
            {
                CheckKeys(item, ["time_ns", "values"], [], "event");
                ulong at = (ulong)Integer(item["time_ns"], duration - 1, "event.time_ns", 0);
                if (at % stepNs != 0 || (long)at <= previous)
                    throw new ToolError("events must be strictly ordered at distinct fixed-step boundaries before duration");
                previous = (long)at;
                if (item["values"] is not JArr values || values.Count < 1 || values.Count > 64)
                    throw new ToolError("event.values: expected 1..64 updates");
                var normalized = new List<EventValue>();
                foreach (JValue update in values.Items)
                {
                    CheckKeys(update, ["channel", "value"], [], "event value");
                    normalized.Add(new EventValue((ulong)Integer(update["channel"], long.MaxValue, "channel", 1), ToNumber(update["value"], "value")));
                }
                if (normalized.Select(entry => entry.Channel).Distinct().Count() != normalized.Count)
                    throw new ToolError("event.values: duplicate channel");
                events[at] = normalized;
            }
        }
        var checks = new List<CheckSpec>();
        if (experiment.ContainsKey("checks"))
        {
            if (experiment["checks"] is not JArr checkList || checkList.Count > 256)
                throw new ToolError("checks: expected at most 256 final-state checks");
            foreach (JValue item in checkList.Items)
            {
                CheckKeys(item, ["object_id", "field"], ["min", "max", "abs_max"], "check");
                BigInteger objectId = Integer(item["object_id"], uint.MaxValue, "check.object_id", 0);
                if (item["field"] is not JStr field || !Fields.ContainsKey(field.Text))
                    throw new ToolError("check: unsupported output field");
                if (!(item.ContainsKey("min") || item.ContainsKey("max") || item.ContainsKey("abs_max")))
                    throw new ToolError("check: expected at least one bound");
                foreach (string bound in new[] { "min", "max", "abs_max" })
                    if (item.ContainsKey(bound)) ToNumber(item[bound], $"check.{bound}");
                double absMax = item.ContainsKey("abs_max") ? ToNumber(item["abs_max"], "check.abs_max") : 0;
                double min = item.ContainsKey("min") ? ToNumber(item["min"], "check.min") : double.NegativeInfinity;
                double max = item.ContainsKey("max") ? ToNumber(item["max"], "check.max") : double.PositiveInfinity;
                if (absMax < 0 || min > max) throw new ToolError("check: invalid bounds");
                checks.Add(new CheckSpec((JObj)item, objectId, field.Text,
                    item.ContainsKey("min"), min, item.ContainsKey("max"), max, item.ContainsKey("abs_max"), absMax));
            }
        }
        var boundaries = new SortedSet<ulong> { 0, duration };
        for (ulong at = sample; at < duration; at += sample) boundaries.Add(at);
        foreach (ulong at in events.Keys) boundaries.Add(at);
        return ([.. boundaries], events, checks);
    }

    private static void CheckKeys(JValue value, string[] required, string[] optional, string path = "document")
    {
        if (value is not JObj obj) throw new ToolError($"{path}: expected an object");
        var missing = required.Where(key => !obj.ContainsKey(key)).Order().ToList();
        var extra = obj.Keys.Where(key => !required.Contains(key) && !optional.Contains(key)).Order().ToList();
        if (missing.Count > 0 || extra.Count > 0)
            throw new ToolError($"{path}: missing fields [{string.Join(", ", missing)}], unknown fields [{string.Join(", ", extra)}]");
    }

    private static BigInteger Integer(JValue value, BigInteger maximum, string path, BigInteger minimum = default)
    {
        if (value is not JInt integer || integer.Value < minimum || integer.Value > maximum)
            throw new ToolError($"{path}: expected an integer in [{minimum}, {maximum}]");
        return integer.Value;
    }

    private static BigInteger GetInteger(JObj source, string key, BigInteger maximum, string path, BigInteger minimum) =>
        source.ContainsKey(key) ? Integer(source[key], maximum, path, minimum) : Integer(new JInt(0), maximum, path, minimum);

    private static double ToNumber(JValue value, string path)
    {
        double result = value switch
        {
            JInt integer => (double)integer.Value,
            JNum number => number.Value,
            _ => throw new ToolError($"{path}: expected a finite number"),
        };
        if (!double.IsFinite(result)) throw new ToolError($"{path}: expected a finite number");
        return result;
    }

    private static Quantity ToQuantity(JValue value, string path)
    {
        CheckKeys(value, ["value", "unit"], [], path);
        if (value["unit"] is not JStr unit || !Units.TryGetValue(unit.Text, out uint code))
            throw new ToolError($"{path}: unknown unit");
        return new Quantity { value = ToNumber(value["value"], path), unit = code };
    }

    // ---- native ABI structures mirroring legacy/native/src/abi.zig ----------
    // Reserved/padding members stay zero; they are intentionally never assigned.
#pragma warning disable CS0649
    private struct Quantity
    {
        public double value;
        public uint unit;
        public uint reserved;
    }

    private struct Node
    {
        public uint id;
        public uint domain;
        public Quantity storage;
        public Quantity initial;
        public Quantity position;
    }

    private struct Shaft
    {
        public Quantity stiffness;
        public Quantity damping;
        public Quantity rest_angle;
        public double ratio;
    }

    private struct Motor
    {
        public Quantity resistance;
        public Quantity inductance;
        public Quantity coupling;
        public Quantity initial_current;
    }

    private struct Thermal
    {
        public Quantity conductance;
        public Quantity ambient_temperature;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct Parameters
    {
        [FieldOffset(0)] public Shaft shaft;
        [FieldOffset(0)] public Motor motor;
        [FieldOffset(0)] public Thermal thermal;
    }

    private struct Component
    {
        public uint id;
        public uint kind;
        public uint node_a;
        public uint node_b;
        public uint heat_node;
        public uint reserved;
        public ulong input_channel;
        public Quantity initial_input;
        public Parameters parameters;
    }

    private struct ModelDesc
    {
        public uint abi_version;
        public uint struct_size;
        public uint schema_version;
        public uint flags;
        public ulong step_ns;
        public Node* nodes;
        public uint node_count;
        public uint component_count;
        public Component* components;
    }

    private struct Diagnostic
    {
        public uint abi_version;
        public uint struct_size;
        public uint code;
        public uint object_id;
        public fixed byte field[40];
        public fixed byte message[160];
    }

    private struct ModelInfo
    {
        public uint abi_version;
        public uint struct_size;
        public ulong fingerprint;
        public ulong step_ns;
        public uint node_count;
        public uint component_count;
        public uint state_count;
        public uint channel_count;
        public uint fidelity;
        public uint validation;
    }

    private struct Channel
    {
        public ulong channel;
        public uint object_id;
        public uint direction;
        public uint unit;
        public uint reserved;
        public fixed byte quantity[32];
    }

    private struct Scalar
    {
        public ulong channel;
        public double value;
        public uint flags;
        public uint reserved;
    }

    private struct InputFrame
    {
        public uint abi_version;
        public uint struct_size;
        public Scalar* values;
        public uint value_count;
        public uint flags;
    }

    private struct Snapshot
    {
        public uint abi_version;
        public uint struct_size;
        public ulong simulation_time_ns;
        public ulong state_hash;
        public ulong diagnostic_bits;
        public Scalar* values;
        public uint value_capacity;
        public uint value_count;
    }

    private struct ContextDesc
    {
        public uint abi_version;
        public uint struct_size;
        public uint flags;
        public uint reserved;
    }

    private struct Version
    {
        public uint abi_version;
        public uint struct_size;
        public uint major;
        public uint minor;
        public uint patch;
        public uint reserved;
        public byte* product_name;
        public byte* version_string;
    }

    private struct Api
    {
        public uint abi_version;
        public uint struct_size;
        public delegate* unmanaged[Cdecl]<Version*, int> runtime_get_version;
        public void* runtime_get_capabilities;
        public delegate* unmanaged[Cdecl]<ContextDesc*, ulong*, int> context_create;
        public delegate* unmanaged[Cdecl]<ulong, int> context_destroy;
        public delegate* unmanaged[Cdecl]<int, byte*> status_string;
        public void* instance_create;
        public delegate* unmanaged[Cdecl]<ulong, int> instance_destroy;
        public delegate* unmanaged[Cdecl]<ulong, InputFrame*, int> instance_submit_inputs;
        public delegate* unmanaged[Cdecl]<ulong, ulong, int> instance_step;
        public delegate* unmanaged[Cdecl]<ulong, Snapshot*, int> instance_read_snapshot;
        public delegate* unmanaged[Cdecl]<ulong, ModelDesc*, ulong*, Diagnostic*, int> model_compile;
        public delegate* unmanaged[Cdecl]<ulong, int> model_destroy;
        public delegate* unmanaged[Cdecl]<ulong, ModelInfo*, int> model_get_info;
        public delegate* unmanaged[Cdecl]<ulong, Channel*, uint, uint*, int> model_get_channels;
        public delegate* unmanaged[Cdecl]<ulong, ulong*, int> instance_create_from_model;
    }
#pragma warning restore CS0649
}

internal static class ModelLabTests
{
    private static string rootPath = "";
    private static string libraryPath = "";
    private static string documentPath = "";

    public static int Run(string library, string root)
    {
        libraryPath = Path.GetFullPath(library);
        rootPath = root;
        documentPath = Path.Combine(root, "legacy", "native", "assets", "labs", "electrothermal.power.json");
        (string Name, Action Body)[] tests =
        [
            ("real_model_replay_and_regeneration", RealModelReplayAndRegeneration),
            ("canonical_input_order", CanonicalInputOrder),
            ("native_compile_error_and_cleanup", NativeCompileErrorAndCleanup),
            ("authoring_errors", AuthoringErrors),
            ("failed_kpi_is_a_report_and_nonzero_exit", FailedKpiIsAReportAndNonzeroExit),
            ("json_limits", JsonLimits),
        ];
        int failures = 0;
        foreach (var (name, body) in tests)
        {
            try
            {
                body();
                Console.WriteLine($"test_model_lab {name} ... ok");
            }
            catch (Exception error)
            {
                failures++;
                Console.Error.WriteLine($"test_model_lab {name} ... FAILED: {error.Message}");
            }
        }
        if (failures > 0) Console.Error.WriteLine($"{failures} of {tests.Length} model lab tests failed");
        return failures == 0 ? 0 : 1;
    }

    private static JObj FreshDocument() => (JObj)ModelLab.ReadDocument(documentPath);

    private static void True(bool condition, string message)
    {
        if (!condition) throw new ToolError(message);
    }

    private static JObj Experiment(JValue document) => (JObj)((JObj)document)["experiment"];

    private static JObj FirstNode(JValue document) => (JObj)((JArr)((JObj)document)["nodes"])[0];

    private static JObj FirstComponent(JValue document) => (JObj)((JArr)((JObj)document)["components"])[0];

    private static void RealModelReplayAndRegeneration()
    {
        var report = ModelLab.Evaluate(FreshDocument(), libraryPath);
        True(((JBool)report["passed"]).Value, "experiment did not pass");
        True(((JBool)((JObj)report["replay"])["sample_hashes_match"]).Value, "same-binary replay failed");
        True(JValues.Equal(((JObj)report["model"])["calibration"], new JStr("unverified")), "calibration is not unverified");
        True(JValues.Equal(((JObj)report["model"])["state_count"], new JInt(7)), "unexpected state count");
        string sourceWork = (0x8000_0000_0000_0000UL | 16).ToString(CultureInfo.InvariantCulture);
        string current = (0x8000_0000_0000_0000UL | (10UL << 8) | 4).ToString(CultureInfo.InvariantCulture);
        var samples = (JArr)report["samples"];
        var byTime = samples.Items.ToDictionary(
            sample => ((JInt)((JObj)sample)["time_ns"]).Value,
            sample => (JObj)((JObj)sample)["values"]);
        True(JValues.AsNumber(byTime[6_000_000_000][sourceWork]) < JValues.AsNumber(byTime[5_000_000_000][sourceWork]),
            "source work did not decrease during braking");
        True(JValues.AsNumber(byTime[6_000_000_000][current]) < 0, "current did not reverse during braking");
        True(JValues.Equal(((JObj)samples[samples.Count - 1])["time_ns"], new JInt(10_000_000_000)), "unexpected final sample time");
    }

    private static void CanonicalInputOrder()
    {
        var document = FreshDocument();
        var first = ModelLab.Evaluate(document, libraryPath);
        ((JArr)document["nodes"]).Reverse();
        ((JArr)document["components"]).Reverse();
        var second = ModelLab.Evaluate(document, libraryPath);
        True(JValues.Equal(first["model"], second["model"]), "model fingerprint changed with descriptor order");
        True(JValues.Equal(first["samples"], second["samples"]), "samples changed with descriptor order");
    }

    private static void NativeCompileErrorAndCleanup()
    {
        var document = FreshDocument();
        var storage = (JObj)FirstNode(document)["storage"];
        storage["unit"] = new JStr("nm");
        for (int attempt = 0; attempt < 70; attempt++)  // Failed compilation must not consume model/context slots.
        {
            bool rejected = false;
            try { ModelLab.Evaluate(document, libraryPath); }
            catch (ToolError error) when (error.Message.Contains("object 1, field storage", StringComparison.Ordinal))
            {
                rejected = true;
            }
            True(rejected, $"attempt {attempt}: compile error was not reported as object 1, field storage");
        }
        storage["unit"] = new JStr("kg_m2");
        True(((JBool)ModelLab.Evaluate(document, libraryPath)["passed"]).Value, "restored document did not pass");
    }

    private static void AuthoringErrors()
    {
        var document = FreshDocument();
        List<Action<JObj>> mutations =
        [
            d => FirstNode(d)["id"] = new JInt(4294967296),
            d => FirstNode(d)["units"] = new JStr("kg_m2"),
            d => FirstNode(d).Remove("position"),
            d => ((JObj)FirstNode(d)["storage"])["value"] = new JNum(double.NaN),
            d => ((JObj)FirstNode(d)["storage"])["value"] = new JBool(true),
            d => ((JObj)d)["step_ns"] = new JBool(true),
            d => FirstComponent(d)["input_channel"] = new JInt(BigInteger.Parse("18446744073709551724")),
            d => ((JObj)FirstComponent(d)["parameters"])["unknown"] = new JInt(1),
            d => Experiment(d)["sample_every_ns"] = new JInt(1),
            d => ((JArr)Experiment(d)["events"]).Reverse(),
            d => ((JObj)((JArr)Experiment(d)["events"])[0])["time_ns"] = new JInt(1),
            d =>
            {
                var firstUpdate = (JObj)((JArr)((JObj)((JArr)Experiment(d)["events"])[0])["values"])[0];
                firstUpdate["channel"] = new JInt(999);
            },
            d => ((JObj)((JArr)Experiment(d)["checks"])[0])["object_id"] = new JInt(999),
            d =>
            {
                var check = (JObj)((JArr)Experiment(d)["checks"])[0];
                check["min"] = new JInt(100);
                check["max"] = new JInt(1);
            },
            d => ((JObj)((JArr)Experiment(d)["checks"])[0])["abs_max"] = new JInt(-1),
        ];
        foreach (var mutation in mutations)
        {
            var candidate = (JObj)JValues.Clone(document);
            mutation(candidate);
            bool rejected = false;
            try { ModelLab.Evaluate(candidate, libraryPath); }
            catch (ToolError) { rejected = true; }
            True(rejected, "an invalid model document was accepted");
        }
    }

    private static void FailedKpiIsAReportAndNonzeroExit()
    {
        var document = FreshDocument();
        ((JObj)((JArr)Experiment(document)["checks"])[0])["min"] = new JInt(30);
        string temporary = Path.Combine(Path.GetTempPath(), "power-model-lab-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            string model = Path.Combine(temporary, "candidate.power.json");
            string output = Path.Combine(temporary, "report.json");
            File.WriteAllText(model, PowerJson.Write(document));
            // Re-enter this same compiled executable rather than `dotnet run --file`:
            // a nested SDK run would try to rebuild the apphost that this process
            // is currently executing and hit its file lock.
            string entry = Environment.ProcessPath
                ?? throw new ToolError("could not locate the running build tool executable");
            var start = new ProcessStartInfo(entry) { WorkingDirectory = rootPath, UseShellExecute = false, RedirectStandardError = true };
            start.ArgumentList.Add("model-lab");
            start.ArgumentList.Add(model);
            start.ArgumentList.Add("--library");
            start.ArgumentList.Add(libraryPath);
            start.ArgumentList.Add("--output");
            start.ArgumentList.Add(output);
            using var process = Process.Start(start) ?? throw new ToolError("could not start dotnet");
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();
            True(process.ExitCode == 2, $"exit code {process.ExitCode}: {stderr}");
            var report = (JObj)PowerJson.Parse(File.ReadAllText(output), rejectDuplicateKeys: false, rejectNonFiniteConstants: false);
            True(!((JBool)report["passed"]).Value, "report passed despite failed KPI");
            True(!((JBool)((JObj)((JArr)report["checks"])[0])["passed"]).Value, "failed check was reported as passed");
            True(((JBool)((JObj)report["replay"])["sample_hashes_match"]).Value, "replay hashes must still match");
        }
        finally
        {
            Directory.Delete(temporary, true);
        }
    }

    private static void JsonLimits()
    {
        string temporary = Path.Combine(Path.GetTempPath(), "power-model-lab-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            string model = Path.Combine(temporary, "invalid.json");
            foreach (string contents in new[] { "{\"schema\": 1, \"schema\": 2}", "{\"value\": NaN}", new string(' ', 1_048_577) })
            {
                File.WriteAllText(model, contents);
                bool rejected = false;
                try { ModelLab.ReadDocument(model); }
                catch (ToolError) { rejected = true; }
                True(rejected, "an invalid JSON document was accepted");
            }
        }
        finally
        {
            Directory.Delete(temporary, true);
        }
    }
}

internal static class Shell
{
    public static void Run(string root, IReadOnlyList<string> command, IReadOnlyDictionary<string, string>? environment = null)
    {
        using var process = Start(root, command, environment);
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new ToolError($"{Path.GetFileName(command[0])} exited with {process.ExitCode}.");
    }

    public static string Capture(string root, IReadOnlyList<string> command, IReadOnlyDictionary<string, string>? environment = null)
    {
        using var process = Start(root, command, environment, redirectOutput: true);
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new ToolError($"{Path.GetFileName(command[0])} exited with {process.ExitCode}.");
        return output;
    }

    public static string? FindOnPath(string executable)
    {
        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (directory.Length == 0) continue;
            try
            {
                string candidate = Path.Combine(directory, executable);
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            }
            catch (ArgumentException) { }
        }
        return null;
    }

    private static Process Start(string root, IReadOnlyList<string> command, IReadOnlyDictionary<string, string>? environment, bool redirectOutput = false)
    {
        string display = string.Join(" ", command.Take(6).Select(Quote));
        if (command.Count > 6) display += $" ... ({command.Count - 6} more arguments)";
        Console.WriteLine("Running: " + display);
        var start = new ProcessStartInfo(command[0]) { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = redirectOutput };
        foreach (string argument in command.Skip(1)) start.ArgumentList.Add(argument);
        start.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        if (environment is not null)
            foreach (var (key, value) in environment) start.Environment[key] = value;
        return Process.Start(start) ?? throw new ToolError("Could not start " + command[0]);
    }

    private static string Quote(string argument) => argument.Contains(' ') ? $"\"{argument}\"" : argument;
}

internal static class NativeVerify
{
    public static int Run(string root, bool sourceOnly = false)
    {
        try
        {
            Verify(root, sourceOnly);
            return 0;
        }
        catch (Exception error) when (error is ToolError or IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine(PowerJson.Write(new JObj
            {
                ["schema"] = new JStr("power.native_verification.v1"),
                ["passed"] = new JBool(false),
                ["error"] = new JStr(error.Message),
            }));
            return 1;
        }
    }

    private static void Verify(string root, bool sourceOnly)
    {
        string native = Path.Combine(root, "legacy", "native");
        JObj inventory = SourceAudit(root, native);
        if (sourceOnly)
        {
            Console.WriteLine(PowerJson.Write(inventory));
            return;
        }
        string versionPin = File.ReadAllText(Path.Combine(root, ".zig-version")).Trim();
        string cached = Path.Combine(root, ".cache", "zig", OperatingSystem.IsWindows() ? "zig.exe" : "zig");
        string? zig = Environment.GetEnvironmentVariable("POWER_ZIG")
            ?? (File.Exists(cached) ? cached : Shell.FindOnPath(OperatingSystem.IsWindows() ? "zig.exe" : "zig"));
        if (zig is null)
            throw new ToolError("Zig is missing. Run dotnet run --file tools/Build.cs -- install-zig or set POWER_ZIG to the pinned compiler executable");
        string installed = Shell.Capture(root, [zig, "version"]).Trim();
        if (installed != versionPin)
            throw new ToolError($"Zig {installed} does not match .zig-version; run the install-zig build command or update POWER_ZIG");
        var zigSources = Directory.EnumerateFiles(native, "*.zig", SearchOption.AllDirectories)
            .Where(path => path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .All(part => part is not ".zig-cache" and not "zig-out"))
            .Order(StringComparer.Ordinal)
            .Select(path => Path.GetRelativePath(root, path))
            .ToList();
        Shell.Run(root, [zig, "fmt", "--check", .. zigSources]);
        string destination = Path.Combine(root, "artifacts", "native");
        List<string> build = [zig, "build", "--build-file", "legacy/native/build.zig", "-j1", "-Doptimize=ReleaseSafe", "--summary", "all"];
        Dictionary<string, string>? buildEnvironment = null;
        if (OperatingSystem.IsMacOS())
        {
            // Zig 0.15.2 cannot read the arm64e-only libSystem stub in newer Apple SDKs.
            // Disable SDK discovery only in these child processes so Zig uses its
            // bundled Darwin stubs, as it does when cross-compiling from Linux.
            buildEnvironment = new Dictionary<string, string> { ["DEVELOPER_DIR"] = "/dev/null" };
            Console.WriteLine("Using Zig's bundled Darwin SDK stubs for native verification");
        }
        Shell.Run(root, [.. build, "--prefix", destination], buildEnvironment);
        Shell.Run(root, [.. build, "test"], buildEnvironment);
        string suffix = OperatingSystem.IsWindows() ? ".exe" : "";
        foreach (string host in new[] { "power_host", "power_model_host" })
            Shell.Run(root, [Path.Combine(destination, "bin", host + suffix)]);
        string library = OperatingSystem.IsWindows() ? Path.Combine(destination, "bin", "power.dll")
            : OperatingSystem.IsMacOS() ? Path.Combine(destination, "lib", "libpower.dylib")
            : Path.Combine(destination, "lib", "libpower.so");
        if (ModelLabTests.Run(library, root) != 0)
            throw new ToolError("the model lab test suite failed");
        JValue exports = JNull.Instance;
        if (OperatingSystem.IsLinux())
        {
            string? nm = Shell.FindOnPath("nm");
            if (nm is null) throw new ToolError("Install binutils to verify the native export boundary with nm");
            string symbols = Shell.Capture(root, [nm, "-D", "--defined-only", library]);
            exports = new JArr(new SortedSet<string>(Regex.Matches(symbols, @"\bpwr_\w+\b").Select(match => match.Value)).Select(name => new JStr(name)));
            if (!JValues.Equal(exports, new JArr { new JStr("pwr_get_api") }))
                throw new ToolError($"Unexpected public native exports: {PowerJson.Write(exports)}");
            string undefined = Shell.Capture(root, [nm, "-D", "--undefined-only", library]).Trim();
            if (undefined.Length > 0)
                throw new ToolError($"Native runtime has external symbol dependencies: {undefined}");
        }
        (JObj report, JObj comparison) = BaselineCheck(root, native, library);
        string reports = Path.Combine(root, "artifacts", "reports");
        Directory.CreateDirectory(reports);
        File.WriteAllText(Path.Combine(reports, "native-electrothermal.json"), PowerJson.Write(report) + "\n");
        var evidence = new JObj
        {
            ["schema"] = new JStr("power.native_verification.v1"),
            ["passed"] = new JBool(true),
            ["zig_version"] = new JStr(installed),
            ["platform"] = new JStr($"{RuntimeInformation.OSDescription} ({RuntimeInformation.OSArchitecture})"),
            ["inventory"] = inventory,
            ["exports"] = exports,
            ["baseline_comparison"] = comparison,
            ["calibration"] = new JStr("unverified"),
            ["library_sha256"] = new JStr(Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(library)))),
        };
        File.WriteAllText(Path.Combine(reports, "native-verification.json"), PowerJson.Write(evidence) + "\n");
        Console.WriteLine(PowerJson.Write(evidence));
    }

    private static JObj SourceAudit(string root, string native)
    {
        // Include new files during development, and exclude deleted tracked files.
        string inventory = Shell.Capture(root, ["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"]);
        List<string> names = [.. inventory.Split('\0')
            .Where(name => name.Length > 0 && File.Exists(Path.Combine(root, name)))
            .Distinct()
            .Order(StringComparer.Ordinal)];
        HashSet<string> forbidden = [".c", ".h", ".cc", ".hh", ".cpp", ".hpp", ".cxx", ".hxx", ".c++", ".h++", ".inl", ".inc"];
        List<string> cFiles = [.. names.Where(name => forbidden.Contains(Path.GetExtension(name).ToLowerInvariant()))];
        if (cFiles.Count > 0) throw new ToolError("C/C++ source or headers remain: " + string.Join(", ", cFiles));
        HashSet<string> luaExtensions = [".lua", ".luau", ".luac", ".rockspec", ".rock"];
        List<string> luaFiles = [.. names.Where(name => luaExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))];
        if (luaFiles.Count > 0) throw new ToolError("Remove remaining Lua source, bytecode or packages: " + string.Join(", ", luaFiles));
        List<string> zigFiles = [.. names.Where(name => name.EndsWith(".zig", StringComparison.Ordinal))];
        Regex reintroducesC = new(@"@cImport\s*\(|@cInclude\s*\(|addCSource|addTranslateC|link_libc\s*=\s*true|linkSystemLibrary");
        foreach (string name in zigFiles)
            if (reintroducesC.IsMatch(File.ReadAllText(Path.Combine(root, name))))
                throw new ToolError($"{name}: native code must not reintroduce C compilation or libc linkage");
        var manifest = (JObj)PowerJson.Parse(File.ReadAllText(Path.Combine(native, "migration-manifest.json")), false, false);
        var sources = (JArr)manifest["sources"];
        foreach (JValue item in sources.Items)
        {
            var entry = (JObj)item;
            string path = ((JStr)entry["path"]).Text, zigPath = ((JStr)entry["zig_path"]).Text;
            if (File.Exists(Path.Combine(root, path)))
                throw new ToolError($"Original source still exists: {path}");
            if (!File.Exists(Path.Combine(root, zigPath)))
                throw new ToolError($"Missing migrated source: {zigPath}");
        }
        foreach (string notice in new[] { "COPYING.NOTICE", "LICENSE", "UNITY-LINKING-EXCEPTION.md" })
            if (!File.Exists(Path.Combine(root, notice)))
                throw new ToolError($"Required license notice missing: {notice}");
        return new JObj
        {
            ["c_source_files"] = new JInt(0),
            ["lua_files"] = new JInt(0),
            ["zig_source_files"] = new JInt(zigFiles.Count),
            ["migrated_files"] = new JInt(sources.Count),
        };
    }

    private static (JObj Report, JObj Comparison) BaselineCheck(string root, string native, string library)
    {
        string document = Path.Combine(native, "assets", "labs", "electrothermal.power.json");
        var baseline = (JObj)PowerJson.Parse(File.ReadAllText(Path.Combine(native, "tests", "fixtures", "c-baseline.json")), false, false);
        if (Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(document))) != ((JStr)baseline["source_sha256"]).Text)
            throw new ToolError("The native baseline model changed; provide new reviewed evidence before updating the fixture");
        var report = ModelLab.Evaluate(ModelLab.ReadDocument(document), library);
        if (!JValues.Equal(report["passed"], new JBool(true)) || !JValues.Equal(((JObj)report["replay"])["sample_hashes_match"], new JBool(true)))
            throw new ToolError("Native KPIs or same-binary replay failed");
        if (!JValues.Equal(report["model"], baseline["model"]) || !JValues.Equal(report["channels"], baseline["channels"]))
            throw new ToolError("Native model fingerprint, fidelity or channel contract changed");
        var reportSamples = (JArr)report["samples"];
        var baselineSamples = (JArr)baseline["samples"];
        if (reportSamples.Count != baselineSamples.Count)
            throw new ToolError("Native baseline sampling changed");
        double relative = JValues.AsNumber(baseline["relative_tolerance"]);
        double absolute = JValues.AsNumber(baseline["absolute_tolerance"]);
        double maximumError = 0.0;
        int valuesChecked = 0;
        for (int index = 0; index < reportSamples.Count; index++)
        {
            var actual = (JObj)reportSamples[index];
            var expected = (JObj)baselineSamples[index];
            var actualValues = (JObj)actual["values"];
            var expectedValues = (JObj)expected["values"];
            if (!JValues.Equal(actual["time_ns"], expected["time_ns"])
                || actualValues.Count != expectedValues.Count || !actualValues.Keys.All(expectedValues.ContainsKey))
                throw new ToolError("Native baseline time/channel mapping changed");
            foreach (var (channel, value) in actualValues.Pairs)
            {
                double reference = JValues.AsNumber(expectedValues[channel]);
                double measured = JValues.AsNumber(value);
                if (!double.IsFinite(measured) || !IsClose(measured, reference, relative, absolute))
                    throw new ToolError($"Native baseline diverged at {((JInt)actual["time_ns"]).Value} ns, channel {channel}: {measured} vs {reference}");
                maximumError = Math.Max(maximumError, Math.Abs(measured - reference));
                valuesChecked++;
            }
        }
        return (report, new JObj
        {
            ["source_revision"] = baseline["source_revision"],
            ["values_checked"] = new JInt(valuesChecked),
            ["maximum_absolute_error"] = new JNum(maximumError),
            ["absolute_tolerance"] = new JNum(absolute),
            ["relative_tolerance"] = new JNum(relative),
        });
    }

    private static bool IsClose(double left, double right, double relative, double absolute) =>
        Math.Abs(left - right) <= Math.Max(relative * Math.Max(Math.Abs(left), Math.Abs(right)), absolute);
}

internal static class ZigToolchain
{
    public static void Install(string root)
    {
        var manifest = (JObj)PowerJson.Parse(File.ReadAllText(Path.Combine(root, "tools", "zig-toolchains.json")), false, false);
        string version = File.ReadAllText(Path.Combine(root, ".zig-version")).Trim();
        if (((JStr)manifest["version"]).Text != version)
            throw new ToolError(".zig-version and tools/zig-toolchains.json disagree");
        string system = OperatingSystem.IsWindows() ? "windows" : OperatingSystem.IsMacOS() ? "macos" : "linux";
        string arch = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x86_64",
            Architecture.Arm64 => "aarch64",
            _ => throw new ToolError($"no pinned Zig toolchain for {RuntimeInformation.OSArchitecture}"),
        };
        string destination = Path.Combine(root, ".cache", "zig");
        string executable = Path.Combine(destination, OperatingSystem.IsWindows() ? "zig.exe" : "zig");
        if (File.Exists(executable))
        {
            string installed = Shell.Capture(root, [executable, "version"]).Trim();
            if (installed != version)
                throw new ToolError($"{executable} is {installed}; move that directory aside before installing {version}");
            Console.WriteLine(executable);
            return;
        }
        if (Directory.Exists(destination))
            throw new ToolError($"{destination} already exists without Zig; move it aside and retry");
        var spec = (JObj)((JObj)manifest["platforms"])[$"{arch}-{system}"];
        string tarball = ((JStr)spec["tarball"]).Text;
        string expected = ((JStr)spec["shasum"]).Text;
        Directory.CreateDirectory(Path.Combine(root, ".cache"));
        string temporary = Path.Combine(root, ".cache", "power-zig-" + Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(temporary);
        try
        {
            string archive = Path.Combine(temporary, Path.GetFileName(new Uri(tarball).AbsolutePath));
            Console.WriteLine($"Downloading Zig {version} for {arch}-{system}");
            using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(15) })
            using (Stream download = client.GetStreamAsync(tarball).ConfigureAwait(false).GetAwaiter().GetResult())
            using (Stream file = File.Create(archive))
            {
                download.CopyTo(file);
            }
            if (Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(archive))) != expected)
                throw new ToolError("Zig archive SHA-256 does not match the committed toolchain manifest");
            string extracted = Path.Combine(temporary, "extracted");
            Directory.CreateDirectory(extracted);
            if (archive.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                ZipFile.ExtractToDirectory(archive, extracted);
            else
                try { Shell.Run(root, ["tar", "-xf", archive, "-C", extracted]); }
                catch (System.ComponentModel.Win32Exception)
                {
                    throw new ToolError("The system tar utility is required to extract the Zig .tar.xz archive on this platform");
                }
            string[] entries = Directory.GetFileSystemEntries(extracted);
            if (entries.Length != 1 || !Directory.Exists(entries[0]))
                throw new ToolError("Unexpected Zig archive layout");
            Directory.Move(entries[0], destination);
        }
        finally
        {
            Directory.Delete(temporary, recursive: true);
        }
        Console.WriteLine(executable);
    }
}
