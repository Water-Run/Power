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

internal static class MovingCylinderChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("moving cylinder / closed-valve sealed equivalence / reverse / dead centers", Sealed),
        ("moving cylinder / open-flow independent ODE / second-order convergence", OpenFlow),
        ("moving cylinder / crank-timed boundary crossings / independent ODE convergence", TimedOpenFlow),
        ("moving cylinder / wall exchange ODE / first-order convergence", Wall),
        ("moving cylinders / shared and coupled cranks / conservation", Coupled),
        ("moving cylinder / atomic batch failure / cancellation / fork / allocation", Transactions),
        ("moving cylinder / topology / units / portable v4 / v3 compatibility", Contracts)
    ];
    internal static GasCylinderDefinition Geometry => new()
    {
        Bore = new(86, Unit.Millimeter), Stroke = new(86, Unit.Millimeter), RodLength = new(143, Unit.Millimeter),
        Phase = new(0, Unit.Radian), CompressionRatio = 10, BackPressure = new(1, Unit.Bar)
    };
    internal static ModelDefinition Model(ulong step = 100_000, double speed = 150, double angle = Math.PI,
        double pressure = 1e5, double temperature = 300, double area = 4e-6, double opening = 0, double conductance = 0) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, 0.1, speed, angle), NodeDefinition.CylinderGas(2, pressure, temperature), NodeDefinition.Thermal(3, 100, 300)],
        Components = [ComponentDefinition.GasCylinder(10, 1, 2, Geometry),
            ComponentDefinition.GasReservoir(11, 2, area, 1e5, 300, .8, 100, opening),
            ComponentDefinition.GasHeatLink(12, 2, 3, conductance), ComponentDefinition.Torque(13, 1, 101, 0)]
    };
    private static void Ok(SimulationStatus status) => Require(status == SimulationStatus.Ok, status.ToString());
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static double Value(Simulation s, uint id, Field field)
    {
        var values = new Scalar[s.Model.OutputCount]; s.ReadSnapshot(values);
        return values.Single(v => v.Channel == Channels.Output(id, field)).Value;
    }

    private static void Sealed()
    {
        foreach (var (speed, angle) in new[] { (150.0, Math.PI), (-150.0, Math.PI), (0.0, 0.0), (0.0, Math.PI), (0.0, .8) })
        {
            var moving = CompiledModel.Compile(Model(speed: speed, angle: angle)).CreateSimulation();
            var sealedGas = CompiledModel.Compile(EngineChecks.Model(speed: speed, angle: angle)).CreateSimulation();
            double mass = Value(moving, 2, Field.Mass);
            for (int i = 0; i < 20; ++i)
            {
                Ok(moving.Step(10_000_000)); Ok(sealedGas.Step(10_000_000));
                Near(Value(moving, 1, Field.Speed), Value(sealedGas, 1, Field.Speed), 2e-8);
                Near(Value(moving, 10, Field.Volume), Value(sealedGas, 10, Field.Volume), 2e-12);
                Near(Value(moving, 2, Field.Pressure), Value(sealedGas, 10, Field.Pressure), 1e-6 * Value(sealedGas, 10, Field.Pressure));
                Near(Value(moving, 2, Field.Temperature), Value(sealedGas, 10, Field.Temperature), 1e-6);
                Require(Value(moving, 2, Field.Mass) == mass);
                Near(Value(moving, 0, Field.EnergyResidual), 0, 1e-6);
            }
        }
        var tiny = CompiledModel.Compile(Model(1, angle: 0)).CreateSimulation();
        Ok(tiny.Step(1000)); Near(Value(tiny, 0, Field.EnergyResidual), 0, 1e-8);
    }

    // Independent smooth choked-flow ODE with optional wall exchange, integrated by RK4.
    // Geometry and flow are written directly from the equations, not the implementation helpers.
    private static double[] Reference(double duration, double conductance, double dt = 1e-6, bool timed = false)
    {
        const double radius = .043, rod = .143, area = Math.PI * .086 * .086 / 4, gamma = 1.4, r = 287;
        double v0 = area * .086 * (1 + 1.0 / 9);
        double[] state = [Math.PI, 50, 1e6 * v0 / (r * 700), 1e6 * v0 / (gamma - 1), 300];
        double[] Rate(double[] y)
        {
            double sin = Math.Sin(y[0]), cos = Math.Cos(y[0]), projection = Math.Sqrt(rod * rod - radius * radius * sin * sin);
            double volume = area * .086 / 9 + area * (radius * (1 - cos) + rod - projection);
            double derivative = area * (radius * sin + radius * radius * sin * cos / projection);
            double pressure = (gamma - 1) * y[3] / volume, temperature = y[3] / (y[2] * r / (gamma - 1));
            double flow = .8 * 4e-6 * pressure / Math.Sqrt(temperature) * Math.Sqrt(gamma / r)
                * Math.Pow(2 / (gamma + 1), (gamma + 1) / (2 * (gamma - 1)));
            if (timed)
            {
                double phase = y[0] - (Math.PI + .05);
                phase -= 4 * Math.PI * Math.Floor(phase / (4 * Math.PI));
                flow *= phase < .2 ? Math.Pow(Math.Sin(Math.PI * phase / .2), 2) : 0;
            }
            Require(pressure > 1e5 / Math.Pow(2 / (gamma + 1), gamma / (gamma - 1)), "Reference must remain choked");
            double heat = conductance * (temperature - y[4]);
            return [y[1], (pressure - 1e5) * derivative / .1, -flow,
                -pressure * derivative * y[1] - gamma * r / (gamma - 1) * temperature * flow - heat, heat / 100];
        }
        for (int i = 0; i < (int)Math.Round(duration / dt); ++i)
        {
            var a = Rate(state); var b = Rate(state.Select((v, j) => v + dt / 2 * a[j]).ToArray());
            var c = Rate(state.Select((v, j) => v + dt / 2 * b[j]).ToArray());
            var d = Rate(state.Select((v, j) => v + dt * c[j]).ToArray());
            for (int j = 0; j < state.Length; ++j) state[j] += dt / 6 * (a[j] + 2 * b[j] + 2 * c[j] + d[j]);
        }
        return state;
    }

    private static double Error(ulong step, double conductance, double[] expected, bool timed = false)
    {
        var definition = Model(step, speed: 50, pressure: 1e6, temperature: 700, opening: 1, conductance: conductance);
        if (timed) definition = definition with { Components = definition.Components.Select(c => c.Id == 11
            ? c with { ValveTiming = new() { CrankNode = 1, CycleAngle = new(720, Unit.Degree),
                OpenAngle = new(Math.PI + .05, Unit.Radian), DurationAngle = new(.2, Unit.Radian) } } : c).ToArray() };
        var simulation = CompiledModel.Compile(definition).CreateSimulation();
        Ok(simulation.Step(8_000_000));
        double[] actual = [Value(simulation, 1, Field.Angle), Value(simulation, 1, Field.Speed), Value(simulation, 2, Field.Mass),
            Value(simulation, 2, Field.InternalEnergy), Value(simulation, 3, Field.Temperature)];
        Near(Value(simulation, 0, Field.MassResidual), 0, 1e-16);
        Near(Value(simulation, 0, Field.EnergyResidual), 0, 1e-7);
        return actual.Select((value, i) => Math.Abs(value - expected[i]) / Math.Abs(expected[i])).Max();
    }

    private static void OpenFlow()
    {
        double[] expected = Reference(.008, 0);
        var refinedReference = Reference(.008, 0, .5e-6);
        Require(expected.Select((v, i) => Math.Abs(v - refinedReference[i]) / Math.Abs(v)).Max() < 1e-10,
            "Independent ODE reference must converge well below the measured solver error");
        double coarse = Error(200_000, 0, expected), fine = Error(100_000, 0, expected), finest = Error(25_000, 0, expected);
        Require(fine < coarse / 3 && finest < fine / 10 && finest < 1e-6, $"Open-flow errors: {coarse}, {fine}, {finest}");
    }

    private static void Wall()
    {
        double[] expected = Reference(.008, 20);
        var refinedReference = Reference(.008, 20, .5e-6);
        Require(expected.Select((v, i) => Math.Abs(v - refinedReference[i]) / Math.Abs(v)).Max() < 1e-10);
        double coarse = Error(200_000, 20, expected), fine = Error(100_000, 20, expected), finest = Error(25_000, 20, expected);
        Require(fine < coarse / 1.7 && finest < fine / 3 && finest < 1e-5, $"Wall-coupled errors: {coarse}, {fine}, {finest}");
    }

    private static void TimedOpenFlow()
    {
        double[] expected = Reference(.008, 0, timed: true);
        var refined = Reference(.008, 0, .5e-6, timed: true);
        Require(expected.Select((v, i) => Math.Abs(v - refined[i]) / Math.Abs(v)).Max() < 1e-10);
        Require(expected[0] > Math.PI + .25, "Reference must cross both valve boundaries");
        double coarse = Error(200_000, 0, expected, true), fine = Error(100_000, 0, expected, true), finest = Error(25_000, 0, expected, true);
        Require(fine < coarse / 2.8 && finest < fine / 8 && finest < 1e-6, $"Crank-timed errors: {coarse}, {fine}, {finest}");
    }

    private static void Coupled()
    {
        foreach (bool shared in new[] { true, false })
        {
            var definition = Model(50_000, pressure: 3e5, temperature: 500, opening: .2, conductance: 3);
            definition = definition with
            {
                Nodes = [.. definition.Nodes, NodeDefinition.CylinderGas(4, 2e5, 400), NodeDefinition.Rotor(5, .2, 130)],
                Components = [.. definition.Components, ComponentDefinition.GasCylinder(14, shared ? 1U : 5U, 4, Geometry with { Phase = new(180, Unit.Degree) }),
                    ComponentDefinition.Shaft(15, 1, 5, 10, .01, heat: 3), ComponentDefinition.GasOrifice(16, 2, 4, 2e-6, .8),
                    ComponentDefinition.SealedCylinder(17, 1, EngineChecks.Gas)]
            };
            var s = CompiledModel.Compile(definition).CreateSimulation();
            for (int i = 0; i < 20; ++i) Ok(s.Step(10_000_000));
            Near(Value(s, 0, Field.EnergyResidual), 0, 2e-6);
            Near(Value(s, 0, Field.MassResidual), 0, 1e-15);
            Require(Value(s, 3, Field.Temperature) > 300);
        }
    }

    private static void Transactions()
    {
        var model = CompiledModel.Compile(Model(pressure: 3e5, temperature: 500, opening: .2, conductance: 2));
        var s = model.CreateSimulation(); var start = Snapshot(s);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Require(s.Step(100_000_000, cancelled.Token) == SimulationStatus.Cancelled && Snapshot(s) == start);
        Require(s.Step(100_000_000, [new(20_000_000, 100, .8), new(25_000_000, 101, 1e12)]) == SimulationStatus.NumericalFailure);
        Require(Snapshot(s) == start);
        Ok(s.Step(100_000_000));
        var reference = model.CreateSimulation();
        for (int i = 0; i < 100; ++i) Ok(reference.Step(1_000_000));
        Require(Snapshot(s) == Snapshot(reference));
        var branch = s.Fork(); Ok(branch.SubmitInputs([new(100, 1)])); Ok(branch.Step(1_000_000));
        Require(Snapshot(s) == Snapshot(reference) && Snapshot(branch) != Snapshot(s));
        var values = new Scalar[model.OutputCount];
        for (int i = 0; i < 2000; ++i) { Ok(s.Step(100_000)); s.ReadSnapshot(values); }
        long before = GC.GetAllocatedBytesForCurrentThread(); bool ok = true;
        for (int i = 0; i < 2000; ++i) { ok &= s.Step(100_000) == SimulationStatus.Ok; s.ReadSnapshot(values); }
        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Require(ok && bytes == 0, $"Moving-cylinder step/snapshot allocation: {bytes}, success: {ok}");
    }

    private static void Contracts()
    {
        void Reject(ModelDefinition definition, DiagnosticCode code)
        { Require(!CompiledModel.TryCompile(definition, out _, out var error) && error!.Code == code, $"Expected {code}"); }
        var model = Model(); var compiled = CompiledModel.Compile(model);
        Require(compiled.Fidelity == "moving_cylinder_gas_exchange" && compiled.StateCount == 5);
        Reject(model with { Nodes = [model.Nodes[0], NodeDefinition.GasVolume(2, .001, 1e5, 300), model.Nodes[2]] }, DiagnosticCode.Schema);
        Reject(model with { Components = model.Components.Skip(1).ToArray() }, DiagnosticCode.Unit);
        Reject(model with { Components = [.. model.Components, ComponentDefinition.GasCylinder(20, 1, 2, Geometry)] }, DiagnosticCode.Connection);
        Reject(model with { Components = [model.Components[0] with { MovingCylinder = null }] }, DiagnosticCode.Schema);
        Reject(model with { Components = [model.Components[0] with { NodeA = 3 }] }, DiagnosticCode.Connection);
        Reject(model with { Components = [ComponentDefinition.GasCylinder(10, 1, 2, Geometry with { RodLength = new(20, Unit.Millimeter) })] }, DiagnosticCode.Range);
        var normalized = model with { Components = [.. model.Components.Select(c => c.Kind == ComponentKind.GasCylinder
            ? c with { MovingCylinder = Geometry with { Bore = new(.086, Unit.Meter), BackPressure = new(1e5, Unit.Pascal) } } : c)] };
        Require(CompiledModel.Compile(normalized).Fingerprint == compiled.Fingerprint);
        var asset = PowerAsset.Create(model, "Moving cylinder", new string('d', 64), 100_000_000, 10_000_000, [new(20_100_000, 100, .5)], []);
        byte[] bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(decoded.Nodes.SequenceEqual(asset.Nodes) && decoded.Components.SequenceEqual(asset.Components));
        Require(decoded.Model.Fingerprint == compiled.Fingerprint && AssetCodec.Encode(decoded).SequenceEqual(bytes));
        var a = asset.CreatePlayback(); var b = decoded.CreatePlayback(); Ok(a.Advance(100_000_000)); Ok(b.Advance(100_000_000));
        Require(a.ReadSnapshot(new Scalar[a.Model.OutputCount]) == b.ReadSnapshot(new Scalar[b.Model.OutputCount]));
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name);
        int extension = counts + 80 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 24 + 36;
        var forged = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(forged.AsSpan(extension), 1);
        SHA256.HashData(forged.AsSpan(0, forged.Length - 32)).CopyTo(forged, forged.Length - 32);
        Throws<AssetFormatException>(() => AssetCodec.Decode(forged));

        // A mixed asset has both old cylinder records and multiple moving records.
        var mixedDefinition = model with
        {
            Nodes = [.. model.Nodes, NodeDefinition.CylinderGas(4, 2e5, 400)],
            Components = [.. model.Components, ComponentDefinition.GasCylinder(15, 1, 4, Geometry with { Phase = new(180, Unit.Degree) }),
                ComponentDefinition.SealedCylinder(16, 1, EngineChecks.Gas)]
        };
        var mixed = PowerAsset.Create(mixedDefinition, "Mixed cylinders", new string('e', 64), 100_000_000, 10_000_000, [], []);
        byte[] mixedBytes = AssetCodec.Encode(mixed);
        var mixedDecoded = AssetCodec.Decode(mixedBytes);
        Require(mixedDecoded.Components.SequenceEqual(mixed.Components) && mixedDecoded.Model.Fingerprint == mixed.Model.Fingerprint);
        int mixedCounts = 78 + Encoding.UTF8.GetByteCount(mixed.Name);
        int movingStart = mixedCounts + 80 + 44 * mixed.Nodes.Count + 156 * mixed.Components.Count + 116 + 24 * 2 + 36;
        void RejectBytes(byte[] data)
        {
            SHA256.HashData(data.AsSpan(0, data.Length - 32)).CopyTo(data, data.Length - 32);
            Throws<AssetFormatException>(() => AssetCodec.Decode(data));
        }
        var duplicate = (byte[])mixedBytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(movingStart + 72), 0); RejectBytes(duplicate);
        var negative = (byte[])mixedBytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(negative.AsSpan(mixedCounts + 28), -1); RejectBytes(negative);
        var missing = mixedBytes.Take(movingStart).Concat(mixedBytes.Skip(movingStart + 72)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(mixedCounts + 28), 1); RejectBytes(missing);
        var downVersion = mixedBytes.Take(mixedCounts + 28).Concat(mixedBytes.Skip(mixedCounts + 80).Take(movingStart - mixedCounts - 80))
            .Concat(mixedBytes.Skip(movingStart + 144)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(downVersion.AsSpan(8), 3); RejectBytes(downVersion);
        byte[] v3Bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "gas-network-v3.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(v3Bytes)) == "a1dbbadcb85e7a86f7b2dc9dfd7212b0b46749ad64451a2237a46402a306a396");
        var v3 = AssetCodec.Decode(v3Bytes); Require(v3.Model.Fingerprint.ToString("x16") == "eeb18a7f1dc76175");
        var upgraded = AssetCodec.Decode(AssetCodec.Encode(v3)); var oldRun = v3.CreatePlayback(); var newRun = upgraded.CreatePlayback();
        Ok(oldRun.Advance(v3.DurationNanoseconds)); Ok(newRun.Advance(v3.DurationNanoseconds));
        Require(oldRun.ReadSnapshot(new Scalar[v3.Model.OutputCount]) == newRun.ReadSnapshot(new Scalar[v3.Model.OutputCount]));
    }
}
