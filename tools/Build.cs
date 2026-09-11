// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

// Cross-platform .NET 10 file-based app: dotnet run --file tools/Build.cs -- verify
using System.Diagnostics;
using System.Runtime.CompilerServices;

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
    if (mode is not ("build" or "verify" or "native-verify" or "unity-validate" or "unity-test"))
        throw new ArgumentException("Usage: dotnet run --file tools/Build.cs -- [build|verify|native-verify|unity-validate|unity-test]");
    string python = Environment.GetEnvironmentVariable("POWER_PYTHON") ?? (OperatingSystem.IsWindows() ? "python" : "python3");
    if (mode == "native-verify")
    {
        await Run(python, "tools/VerifyNative.py");
        return 0;
    }
    // Deliberately serialize compilation and tests to keep desktop memory pressure bounded.
    await Run(dotnet, "build", "Power.slnx", "-c", "Release", "--nologo", "--disable-build-servers", "-m:1", "-p:UseSharedCompilation=false");
    string cli = Path.Combine(root, "src", "Power.Cli", "bin", "Release", "net10.0", "Power.Cli.dll");
    foreach (var lab in new[] { (File: "electrothermal", Asset: "Electrothermal", Name: "Electrothermal laboratory"),
        (File: "thermal-network", Asset: "ThermalNetwork", Name: "Thermal exchange laboratory"),
        (File: "sealed-cylinder", Asset: "SealedCylinder", Name: "Sealed cylinder laboratory") })
        await Run(dotnet, cli, "export", $"assets/labs/{lab.File}.power.json", "--name", lab.Name,
            "--output", $"Unity/Assets/Generated/Resources/{lab.Asset}.powerasset");
    if (mode == "verify")
    {
        foreach (string project in new[] { "Power.Tests", "Power.UnityCompatibility", "Power.Mcp.Tests" })
            await Run(dotnet, Path.Combine(root, "tests", project, "bin", "Release", "net10.0", project + ".dll"));
        foreach (string lab in new[] { "electrothermal", "thermal-network", "sealed-cylinder" })
            await Run(dotnet, cli, $"assets/labs/{lab}.power.json", "--output", $"artifacts/reports/{lab}.json");
        Console.WriteLine("Managed verification passed. Unity Editor/Play/IL2CPP require separate Unity validation.");
        await Run(python, "tools/VerifyNative.py");
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
