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

internal static class ValveTimingChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("valve timing / analytic envelope / periodicity / wrap / explicit cycles", Profile),
        ("valve timing / analytic gated blowdown / reverse flow in angle / convergence", Blowdown),
        ("valve timing / acceleration / reversal / stopped crank / peak control", Kinematics),
        ("valve timing / unresolved lobe rejection / rollback / fork / allocations", Transactions),
        ("valve timing / topology / units / ownership / v5 extensions / v4 fixture", Contracts)
    ];

    internal static ValveTimingDefinition Timing => new()
    { CrankNode = 1, CycleAngle = new(2 * Math.PI, Unit.Radian), OpenAngle = new(5.5, Unit.Radian), DurationAngle = new(1.4, Unit.Radian) };
    private static ModelDefinition Vessel(ulong step = 100_000, double speed = 40, double angle = 5.2) => new()
    {
        StepNanoseconds = step,
        Nodes = [NodeDefinition.Rotor(1, 2, speed, angle), NodeDefinition.GasVolume(2, .002, 2e6, 900)],
        Components = [ComponentDefinition.GasReservoir(10, 2, 1e-6, 1e5, 300, .8, 100, .7) with { ValveTiming = Timing },
            ComponentDefinition.Torque(11, 1, 101, 0)]
    };
    private static void Ok(SimulationStatus status) => Require(status == SimulationStatus.Ok, status.ToString());
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static double Value(Simulation s, uint id, Field field)
    {
        var values = new Scalar[s.Model.OutputCount]; s.ReadSnapshot(values);
        return values.Single(v => v.Channel == Channels.Output(id, field)).Value;
    }

    private static void Profile()
    {
        foreach (double cycle in new[] { 2 * Math.PI, 4 * Math.PI })
        {
            var profile = new CrankValveProfile(cycle, -.25, 1);
            foreach (int turn in Enumerable.Range(-4, 9))
            {
                double start = -.25 + turn * cycle;
                Near(profile.Evaluate(start), 0, 1e-27);
                Near(profile.Evaluate(start + .25), .5, 2e-14);
                Near(profile.Evaluate(start + .5, .6), .6, 1e-15);
                Near(profile.Evaluate(start + .75), .5, 2e-14);
                Near(profile.Evaluate(start + 1), 0, 1e-27);
                Require(profile.Evaluate(start + 1.1) == 0 && profile.Evaluate(start - .1) == 0);
            }
            Near(profile.MaxTravelPerTickRadians, .125, 0);
        }
        Throws<ArgumentException>(() => new CrankValveProfile(Math.PI, 0, 1));
        Throws<ArgumentException>(() => new CrankValveProfile(2 * Math.PI, 0, 0));
        Throws<ArgumentException>(() => new CrankValveProfile(2 * Math.PI, 0, 7));
        Throws<ArgumentException>(() => new CrankValveProfile(2 * Math.PI, double.NaN, 1));
        Throws<ArgumentOutOfRangeException>(() => new CrankValveProfile(2 * Math.PI, 0, 1).Evaluate(0, 1.01));
    }

    // Independent antiderivative of the normalized periodic sin-squared lobe.
    private static double ExposurePrimitive(double angle, double open, double duration, double cycle)
    {
        double turns = Math.Floor((angle - open) / cycle), local = angle - open - turns * cycle;
        double integral = local >= duration ? duration / 2 : local / 2 - duration / (4 * Math.PI) * Math.Sin(2 * Math.PI * local / duration);
        return turns * duration / 2 + integral;
    }

    private static void Blowdown()
    {
        foreach (double speed in new[] { 40.0, -40.0 })
        {
            const double time = .4, initialAngle = 5.2, rho0 = 2e6 / (287.0 * 900), exponent = .2;
            double exposure = .7 * (ExposurePrimitive(initialAngle + speed * time, 5.5, 1.4, 2 * Math.PI)
                - ExposurePrimitive(initialAngle, 5.5, 1.4, 2 * Math.PI)) / speed;
            double coefficient = 1e-6 * .8 * Math.Sqrt(1.4 / 287) * Math.Pow(2 / 2.4, 3) * 287 * 30 * Math.Pow(rho0, -exponent);
            double rho = Math.Pow(Math.Pow(rho0, -exponent) + exponent * coefficient * exposure / .002, -1 / exponent);
            double Error(ulong step)
            {
                var s = CompiledModel.Compile(Vessel(step, speed)).CreateSimulation(); Ok(s.Step(400_000_000));
                Near(Value(s, 2, Field.Temperature), 900 * Math.Pow(rho / rho0, .4), .003);
                Near(Value(s, 0, Field.MassResidual), 0, 2e-16);
                Near(Value(s, 0, Field.EnergyResidual), 0, 1e-8);
                Require(Value(s, 2, Field.Pressure) > 2e5, "Flow must stay choked throughout the reference");
                return Math.Abs(Value(s, 2, Field.Mass) / (.002 * rho) - 1);
            }
            double coarse = Error(1_000_000), fine = Error(500_000), finest = Error(125_000);
            Require(fine < coarse / 2.8 && finest < fine / 8 && finest < 1e-7, $"Gated blowdown error at {speed} rad/s: {coarse}, {fine}, {finest}");
        }
    }

    private static void Kinematics()
    {
        // The rotor is independent of vessel pressure, so constant torque gives an exact angle.
        var s = CompiledModel.Compile(Vessel(speed: -1, angle: 5.8)).CreateSimulation();
        Ok(s.SubmitInputs([new(101, 20)])); // alpha = 10 rad/s^2: direction reverses at 0.1 s.
        for (int i = 1; i <= 30; ++i)
        {
            Ok(s.Step(10_000_000)); double time = i * .01, angle = 5.8 - time + 5 * time * time;
            Near(Value(s, 1, Field.Angle), angle, 1e-10);
            double position = angle - 5.5;
            Near(Value(s, 10, Field.Opening), .7 * Math.Pow(Math.Sin(Math.PI * position / 1.4), 2), 1e-10);
        }
        var stopped = CompiledModel.Compile(Vessel(speed: 0, angle: 6.2)).CreateSimulation();
        Ok(stopped.Step(100_000_000)); Near(Value(stopped, 10, Field.Opening), .7, 1e-15);
        double mass = Value(stopped, 2, Field.Mass); Ok(stopped.SubmitInputs([new(100, 0)])); Ok(stopped.Step(100_000_000));
        Require(Value(stopped, 10, Field.Opening) == 0 && Value(stopped, 2, Field.Mass) == mass);
    }

    private static void Transactions()
    {
        var definition = Vessel(1_000_000, speed: 100, angle: 0);
        definition = definition with { Components = [definition.Components[0] with
            { ValveTiming = Timing with { OpenAngle = new(.02, Unit.Radian), DurationAngle = new(.05, Unit.Radian) } }, definition.Components[1]] };
        var coarse = CompiledModel.Compile(definition).CreateSimulation(); var start = Snapshot(coarse);
        // Both tick endpoints are closed, but the tick crosses the whole lobe. It must fail.
        Require(coarse.Step(1_000_000) == SimulationStatus.NumericalFailure && Snapshot(coarse) == start);
        Ok(coarse.SubmitInputs([new(100, 0)])); var disabled = Snapshot(coarse);
        Require(coarse.Step(3_000_000, [new(1_000_000, 100, .7)]) == SimulationStatus.NumericalFailure && Snapshot(coarse) == disabled);
        Ok(coarse.Step(1_000_000));
        var fine = CompiledModel.Compile(definition with { StepNanoseconds = 10_000 }).CreateSimulation();
        double beforeMass = Value(fine, 2, Field.Mass); Ok(fine.Step(1_000_000));
        Require(Value(fine, 2, Field.Mass) < beforeMass, "Resolved crossing must transfer gas");

        var model = CompiledModel.Compile(Vessel()); var s = model.CreateSimulation(); var reference = model.CreateSimulation();
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        var mark = Snapshot(s); Require(s.Step(100_000_000, cancelled.Token) == SimulationStatus.Cancelled && Snapshot(s) == mark);
        Require(s.Step(100_000_000, [new(20_000_000, 100, .4), new(50_000_000, 101, 1e12)]) == SimulationStatus.NumericalFailure && Snapshot(s) == mark);
        Ok(s.Step(100_000_000)); for (int i = 0; i < 100; ++i) Ok(reference.Step(1_000_000));
        Require(Snapshot(s) == Snapshot(reference));
        var branch = s.Fork(); Ok(branch.SubmitInputs([new(100, 0)])); Ok(branch.Step(50_000_000));
        Require(Snapshot(s) == Snapshot(reference));
        var values = new Scalar[model.OutputCount];
        for (int i = 0; i < 1000; ++i) { Ok(s.Step(100_000)); s.ReadSnapshot(values); }
        long allocation = GC.GetAllocatedBytesForCurrentThread(); bool ok = true;
        for (int i = 0; i < 2000; ++i) { ok &= s.Step(100_000) == SimulationStatus.Ok; s.ReadSnapshot(values); }
        Require(ok && GC.GetAllocatedBytesForCurrentThread() == allocation);
    }

    private static void Contracts()
    {
        var definition = Vessel(); var model = CompiledModel.Compile(definition);
        Require(model.Fidelity == "crank_timed_gas_exchange" && model.Channels.Any(c => c.IsInput && c.Quantity == "peak_opening"));
        void Reject(ComponentDefinition c, DiagnosticCode code)
        { Require(!CompiledModel.TryCompile(definition with { Components = [c] }, out _, out var diagnostic) && diagnostic!.Code == code); }
        Reject(definition.Components[0] with { ValveTiming = Timing with { CrankNode = 2 } }, DiagnosticCode.Connection);
        Reject(definition.Components[0] with { ValveTiming = Timing with { CycleAngle = new(180, Unit.Degree) } }, DiagnosticCode.Range);
        Reject(definition.Components[0] with { ValveTiming = Timing with { DurationAngle = new(1, Unit.Kelvin) } }, DiagnosticCode.Unit);
        Reject(definition.Components[1] with { ValveTiming = Timing }, DiagnosticCode.Schema);
        var degrees = definition with { Components = [definition.Components[0] with { ValveTiming = Timing with { CycleAngle = new(360, Unit.Degree) } }, definition.Components[1]] };
        Require(CompiledModel.Compile(degrees).Fingerprint == model.Fingerprint);
        var asset = PowerAsset.Create(definition, "Valve timing", new string('e', 64), 400_000_000, 10_000_000,
            [new(105_100_000, 100, .5), new(200_100_000, 100, .7)], []);
        byte[] bytes = AssetCodec.Encode(asset); var decoded = AssetCodec.Decode(bytes);
        Require(decoded.Components.SequenceEqual(asset.Components) && decoded.Model.Fingerprint == model.Fingerprint);
        var a = asset.CreatePlayback(); var b = decoded.CreatePlayback(); Ok(a.Advance(400_000_000));
        while (!b.Completed) Ok(b.Advance(Math.Min(1_700_000, decoded.DurationNanoseconds - b.TimeNanoseconds)));
        Require(a.ReadSnapshot(new Scalar[model.OutputCount]) == b.ReadSnapshot(new Scalar[model.OutputCount]));
        int counts = 78 + Encoding.UTF8.GetByteCount(asset.Name), extension = counts + 80 + 44 * asset.Nodes.Count + 156 * asset.Components.Count + 24 + 36;
        void RejectBytes(byte[] data)
        {
            SHA256.HashData(data.AsSpan(0, data.Length - 32)).CopyTo(data, data.Length - 32);
            Throws<AssetFormatException>(() => AssetCodec.Decode(data));
        }
        var wrong = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(wrong.AsSpan(extension), 1); RejectBytes(wrong);
        var negative = (byte[])bytes.Clone(); BinaryPrimitives.WriteInt32LittleEndian(negative.AsSpan(counts + 32), -1); RejectBytes(negative);
        var missing = bytes.Take(extension).Concat(bytes.Skip(extension + 44)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(missing.AsSpan(counts + 32), 0); RejectBytes(missing);
        var duplicate = bytes.Take(extension + 44).Concat(bytes.Skip(extension).Take(44)).Concat(bytes.Skip(extension + 44)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(duplicate.AsSpan(counts + 32), 2); RejectBytes(duplicate);
        var downgraded = bytes.Take(counts + 32).Concat(bytes.Skip(counts + 80).Take(extension - counts - 80)).Concat(bytes.Skip(extension + 44)).ToArray();
        BinaryPrimitives.WriteInt32LittleEndian(downgraded.AsSpan(8), 4); RejectBytes(downgraded);
        byte[] v4Bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "moving-cylinder-v4.powerasset"));
        Require(Convert.ToHexStringLower(SHA256.HashData(v4Bytes)) == "1e73cac5346fdafd3ddae159e658e1e8156aaa69281627842372eeecf08cf325");
        var old = AssetCodec.Decode(v4Bytes); Require(old.Model.Fingerprint.ToString("x16") == "dd62971021fa06e6");
        var upgraded = AssetCodec.Decode(AssetCodec.Encode(old));
        var oldRun = old.CreatePlayback(); var newRun = upgraded.CreatePlayback(); Ok(oldRun.Advance(old.DurationNanoseconds)); Ok(newRun.Advance(old.DurationNanoseconds));
        Require(oldRun.ReadSnapshot(new Scalar[old.Model.OutputCount]) == newRun.ReadSnapshot(new Scalar[old.Model.OutputCount]));
    }
}
