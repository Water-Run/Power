// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Reflection;
using System.Runtime.Versioning;
using Power.Core;
using Power.Assets;
using Power.Tests;

string? framework = typeof(Simulation).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
CoreChecks.Require(framework == ".NETStandard,Version=v2.1", $"Wrong assembly under test: {framework}");
CoreChecks.Require(typeof(AssetCodec).Assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName == ".NETStandard,Version=v2.1");
int failed = 0, total = 0;
foreach (var (name, run) in CoreChecks.All.Concat(AssetChecks.All))
{
    ++total;
    try { run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception error) { ++failed; Console.Error.WriteLine($"FAIL {name}: {error}"); }
}
Console.WriteLine($"{total - failed}/{total} checks passed (Unity-facing .NET Standard 2.1 assembly on .NET 10; Unity Editor not exercised).");
return failed == 0 ? 0 : 1;
