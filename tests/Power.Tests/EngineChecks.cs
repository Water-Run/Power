// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Security.Cryptography;
using System.Text;
using Power.Assets;
using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class EngineChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("crank-slider analytic geometry / derivative / periodicity", Geometry),
        ("sealed gas analytic state / pressure work / long-run conservation", Conservation),
        ("cylinder second-order convergence / reverse rotation / small steps", Convergence),
        ("multiple cylinders / coupled cranks / electrical and thermal energy", Coupled),
        ("cylinder batch rollback / cancellation / branching / recovery", Transactions),
        ("cylinder units / validation / portable versions / immutable ownership", Contracts),
        ("nonlinear step and snapshot zero managed allocations", Allocations)
    ];

    internal static SealedCylinderDefinition Gas => new()
    {
        Bore = new(86, Unit.Millimeter), Stroke = new(86, Unit.Millimeter), RodLength = new(143, Unit.Millimeter),
        Phase = new(0, Unit.Radian), CompressionRatio = 10, InitialPressure = new(1, Unit.Bar),
        InitialTemperature = new(300, Unit.Kelvin), GasConstant = new(287, Unit.JoulePerKilogramKelvin),
        Gamma = 1.4, BackPressure = new(1, Unit.Bar)
    };
    internal static ModelDefinition Model(ulong step = 100_000, double speed = 150, double angle = Math.PI) => new()
    {
        StepNanoseconds = step, Nodes = [NodeDefinition.Rotor(1, 0.1, speed, angle)],
        Components = [ComponentDefinition.SealedCylinder(10, 1, Gas), ComponentDefinition.Torque(11, 1, 100, 0)]
    };
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static double Value(Simulation s, uint id, Field field)
    {
        var buffer = new Scalar[s.Model.OutputCount]; s.ReadSnapshot(buffer);
        return buffer.Single(v => v.Channel == Channels.Output(id, field)).Value;
    }
    private static void Ok(SimulationStatus status) => Require(status == SimulationStatus.Ok, status.ToString());

    private static void Geometry()
    {
        var geometry = new CrankSlider(.086, .086, .143, 10);
        double area = Math.PI * .086 * .086 / 4, swept = area * .086;
        Near(geometry.Evaluate(0).VolumeCubicMeters, swept / 9, 1e-19);
        Near(geometry.Evaluate(Math.PI).VolumeCubicMeters, swept * 10 / 9, 1e-18);
        for (int i = -40; i <= 40; ++i)
        {
            double angle = i * .13, r = .043, l = .143;
            var actual = geometry.Evaluate(angle);
            double x = r * (1 - Math.Cos(angle)) + l - Math.Sqrt(l * l - r * r * Math.Sin(angle) * Math.Sin(angle));
            Near(actual.DisplacementMeters, x, 8e-17);
            Near(geometry.Evaluate(angle + 2 * Math.PI).VolumeCubicMeters, actual.VolumeCubicMeters, 1e-18);
            double difference = (geometry.Evaluate(angle + 1e-5).VolumeCubicMeters - geometry.Evaluate(angle - 1e-5).VolumeCubicMeters) / 2e-5;
            Near(actual.VolumeDerivativeCubicMetersPerRadian, difference, 2e-14);
        }
        Throws<ArgumentException>(() => new CrankSlider(.086, .086, .043, 10));
        Throws<ArgumentException>(() => new CrankSlider(.086, .086, .143, 1));
        Throws<ArgumentException>(() => new CrankSlider(double.MaxValue, .086, .143, 10));
    }

    private static void Conservation()
    {
        foreach (double backPressure in new[] { 0.0, 100_000.0 })
        {
            var definition = Model();
            definition.Components[0] = ComponentDefinition.SealedCylinder(10, 1, Gas with { BackPressure = new(backPressure, Unit.Pascal) });
            var s = CompiledModel.Compile(definition).CreateSimulation();
            double v0 = Value(s, 10, Field.Volume), u0 = Value(s, 10, Field.InternalEnergy), mass = 1e5 * v0 / (287 * 300);
            for (int i = 0; i < 200; ++i)
            {
                Ok(s.Step(10_000_000));
                double volume = Value(s, 10, Field.Volume), pressure = Value(s, 10, Field.Pressure), temperature = Value(s, 10, Field.Temperature);
                Near(pressure / 1e5, Math.Pow(v0 / volume, 1.4), 1e-12);
                Near(temperature / 300, Math.Pow(v0 / volume, .4), 1e-13);
                Near(pressure * volume, mass * 287 * temperature, 1e-10);
                Near(Value(s, 10, Field.Mass), mass, 1e-18);
                Near(Value(s, 0, Field.SourceWork), -backPressure * (volume - v0), 3e-8);
                double expectedSpeedSquared = 150 * 150 + 2 * (u0 - Value(s, 10, Field.InternalEnergy) - backPressure * (volume - v0)) / .1;
                Near(Value(s, 1, Field.Speed), Math.Sqrt(expectedSpeedSquared), 5e-8);
                Near(Value(s, 0, Field.EnergyResidual), 0, 8e-7);
            }
        }
    }

    private static void Convergence()
    {
        double Angle(ulong step)
        {
            var s = CompiledModel.Compile(Model(step)).CreateSimulation(); Ok(s.Step(30_000_000));
            return Value(s, 1, Field.Angle);
        }
        double reference = Angle(6_250);
        double e0 = Math.Abs(Angle(200_000) - reference), e1 = Math.Abs(Angle(100_000) - reference), e2 = Math.Abs(Angle(50_000) - reference);
        Near(e0 / e1, 4, .15); Near(e1 / e2, 4, .15);
        var forward = CompiledModel.Compile(Model()).CreateSimulation();
        var reverse = CompiledModel.Compile(Model(speed: -150)).CreateSimulation();
        Ok(forward.Step(100_000_000)); Ok(reverse.Step(100_000_000));
        Near(Value(forward, 10, Field.Pressure), Value(reverse, 10, Field.Pressure), 1e-5);
        Near(Value(forward, 1, Field.Speed), -Value(reverse, 1, Field.Speed), 1e-9);
        foreach (double angle in new[] { 0.0, Math.PI, 1.0 })
            foreach (double speed in new[] { -1e-5, 0.0, 1e-5 })
            {
                var tiny = CompiledModel.Compile(Model(1, speed, angle)).CreateSimulation();
                Ok(tiny.Step(100)); Near(Value(tiny, 0, Field.EnergyResidual), 0, 1e-10);
            }
    }

    private static void Coupled()
    {
        var model = Model() with
        {
            Nodes = [NodeDefinition.Rotor(1, .1, 150, Math.PI), NodeDefinition.Rotor(2, .2, 147, Math.PI), NodeDefinition.Thermal(3, 100, 300)],
            Components = [ComponentDefinition.SealedCylinder(10, 1, Gas),
                ComponentDefinition.SealedCylinder(11, 1, Gas with { Phase = new(120, Unit.Degree) }),
                ComponentDefinition.SealedCylinder(12, 2, Gas with { Phase = new(240, Unit.Degree) }),
                ComponentDefinition.Shaft(13, 1, 2, 30, .1, heat: 3), ComponentDefinition.Motor(14, 2, 3, 100, 1, .1, .1, 24),
                ComponentDefinition.ThermalLink(15, 3, 0, 2, 300)]
        };
        var s = CompiledModel.Compile(model).CreateSimulation(); Ok(s.Step(200_000_000));
        Require(Value(s, 3, Field.Temperature) > 300 && Value(s, 0, Field.HeatRejected) > 0);
        Near(Value(s, 0, Field.EnergyResidual), 0, 1e-6);
    }

    private static void Transactions()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation(); var initial = Snapshot(s);
        Require(s.Step(100_000_000, [new(0, 100, 1), new(25_000_000, 100, 1e12)]) == SimulationStatus.NumericalFailure);
        Require(Snapshot(s) == initial);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Require(s.Step(100_000_000, cancelled.Token) == SimulationStatus.Cancelled && Snapshot(s) == initial);
        var split = s.Fork();
        Ok(s.Step(100_000_000));
        for (int i = 0; i < 100; ++i) Ok(split.Step(1_000_000));
        Require(Snapshot(s) == Snapshot(split));
        var branch = s.Fork(); Ok(branch.SubmitInputs([new(100, 2)])); Ok(branch.Step(1_000_000));
        Require(Snapshot(s) == Snapshot(split) && Snapshot(s) != Snapshot(branch));
        var tooLarge = CompiledModel.Compile(Model(10_000_000)).CreateSimulation();
        Require(tooLarge.Step(10_000_000) == SimulationStatus.NumericalFailure);
    }

    private static void Contracts()
    {
        var definition = Model(); var compiled = CompiledModel.Compile(definition);
        Require(compiled.Fidelity == "sealed_adiabatic_gas" && compiled.Calibration == "unverified");
        definition.Components[0] = ComponentDefinition.SealedCylinder(10, 1, Gas with { Bore = new(.086, Unit.Meter), InitialPressure = new(100_000, Unit.Pascal) });
        Require(compiled.Fingerprint == CompiledModel.Compile(definition).Fingerprint);
        definition.Components[0] = definition.Components[0] with { Cylinder = Gas with { Gamma = 1 } };
        Require(!CompiledModel.TryCompile(definition, out _, out var error) && error!.ObjectId == 10 && error.Code == DiagnosticCode.Range);
        definition.Components[0] = ComponentDefinition.SealedCylinder(10, 1, Gas with { Bore = new(86, Unit.Kelvin) });
        Require(!CompiledModel.TryCompile(definition, out _, out error) && error!.Field == "cylinder.bore" && error.Code == DiagnosticCode.Unit);
        definition.Components[0] = ComponentDefinition.SealedCylinder(10, 1, Gas) with { NodeB = 1 };
        Require(!CompiledModel.TryCompile(definition, out _, out error) && error!.Code == DiagnosticCode.Connection);
        definition.Components[0] = ComponentDefinition.SealedCylinder(10, 1, null!);
        Require(!CompiledModel.TryCompile(definition, out _, out error) && error!.Code == DiagnosticCode.Schema);
        var asset = PowerAsset.Create(Model(), "Cylinder", new string('a', 64), 100_000_000, 10_000_000, [new(25_000_000, 100, 2)], []);
        var encoded = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(encoded);
        Require(decoded.Components[0].Cylinder == Gas && decoded.Model.Fingerprint == compiled.Fingerprint);
        Require(AssetCodec.Encode(decoded).SequenceEqual(encoded));
        var forged = (byte[])encoded.Clone();
        int extensionOffset = 98 + Encoding.UTF8.GetByteCount(asset.Name) + 44 * asset.Nodes.Count + 156 * asset.Components.Count;
        // Redirect gas parameters to the torque source, retaining a valid digest.
        forged[extensionOffset] = 1;
        SHA256.HashData(forged.AsSpan(0, forged.Length - 32)).CopyTo(forged, forged.Length - 32);
        Throws<AssetFormatException>(() => AssetCodec.Decode(forged));
        var a = asset.CreatePlayback(); var b = decoded.CreatePlayback();
        Ok(a.Advance(100_000_000)); Ok(b.Advance(100_000_000));
        Require(a.ReadSnapshot(new Scalar[a.Model.OutputCount]) == b.ReadSnapshot(new Scalar[b.Model.OutputCount]));
        var legacy = AssetCodec.Decode(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "electrothermal-v1.powerasset")));
        Require(legacy.Model.Fingerprint == CompiledModel.Compile(SampleModels.Electrothermal()).Fingerprint);
        var upgraded = AssetCodec.Decode(AssetCodec.Encode(legacy));
        var oldPlayback = legacy.CreatePlayback(); var newPlayback = upgraded.CreatePlayback();
        Ok(oldPlayback.Advance(legacy.DurationNanoseconds)); Ok(newPlayback.Advance(upgraded.DurationNanoseconds));
        Require(oldPlayback.ReadSnapshot(new Scalar[legacy.Model.OutputCount]) == newPlayback.ReadSnapshot(new Scalar[upgraded.Model.OutputCount]));
    }

    private static void Allocations()
    {
        var s = CompiledModel.Compile(Model()).CreateSimulation(); var values = new Scalar[s.Model.OutputCount];
        for (int i = 0; i < 2000; ++i) { Ok(s.Step(100_000)); s.ReadSnapshot(values); }
        long before = GC.GetAllocatedBytesForCurrentThread(); bool ok = true;
        for (int i = 0; i < 2000; ++i) { ok &= s.Step(100_000) == SimulationStatus.Ok; s.ReadSnapshot(values); }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(ok && bytes == 0, $"Nonlinear stepping allocated {bytes} bytes; success={ok}.");
    }
}
