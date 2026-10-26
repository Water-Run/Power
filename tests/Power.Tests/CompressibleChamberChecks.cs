// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;
namespace Power.Tests;

internal static class CompressibleChamberChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("compressible chamber work / exact liquid cold integral and ideal-vapor adiabat", AnalyticWork),
        ("compressible chamber work / independent equilibrium entropy inversion and pressure quadrature", PhaseWork),
        ("compressible chamber / closed phase-changing shaft work and entropy", Closed),
        ("compressible chamber / independent liquid shaft ODE second-order refinement", LiquidReference),
        ("compressible chamber duct / independent ideal-vapor method-of-lines RK4 refinement", DuctReference),
        ("compressible chamber duct / phase change gear clutch and hydraulic composition", Composition),
        ("compressible chamber / rollback cancellation branch independence and zero allocations", Transactions),
        ("compressible chamber / units geometry ownership and immutable compilation", Contracts)
    ];
    internal static FuelChamberDefinition Geometry => new() { MinimumReferenceVolume = new(1e-7, Unit.CubicMeter), SweptVolume = new(2e-7, Unit.CubicMeter), Phase = new(0, Unit.Radian) };
    internal static ModelDefinition Model(bool duct = false, bool vapor = false, ulong tick = 1000)
    {
        var material = EquilibriumDuctChecks.Material(vapor); double t = vapor ? 500 : 350, angle = duct ? .5 : 0, speed = duct ? 5000 : 100;
        double volume = 1e-7 + 2e-7 * Math.Pow(Math.Sin(angle / 2), 2), density = vapor ? 1e5 / (287 * t) : 750 * (1 + 2e5 / 1.5e9);
        var nodes = new List<NodeDefinition> { NodeDefinition.Rotor(4, 1e-5, speed, angle), NodeDefinition.CompressibleFuelVolume(1, volume, volume * density, t, material), NodeDefinition.Thermal(3, 1, 350) };
        var components = new List<ComponentDefinition> { ComponentDefinition.CompressibleChamber(12, 4, 1, Geometry) };
        if (duct)
        {
            nodes.Add(NodeDefinition.CompressibleFuelVolume(2, .001, .001 * density, t, material));
            for (uint id = 100; id < 104; ++id) nodes.Add(NodeDefinition.CompressibleFuelVolume(id, .025e-6, .025e-6 * density, t, material));
            components.Add(ComponentDefinition.EquilibriumDuct(10, 1, 2, 3, new() { Cells = new uint[] { 100, 101, 102, 103 }, Area = new(1e-6, Unit.SquareMeter),
                LinearResistance = new(0, Unit.PascalSecondPerCubicMeter), QuadraticResistance = new(0, Unit.PascalSecondSquaredPerCubicMeterSquared), FluidHeatFraction = new(.5, Unit.Fraction) }));
        }
        return new() { StepNanoseconds = tick, Nodes = nodes.ToArray(), Components = components.ToArray() };
    }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Step(Simulation s, ulong duration) => Require(s.Step(duration) == SimulationStatus.Ok, "Compressible chamber interval failed.");
    private static void Balance(Simulation s) { Near(Value(s, 0, Field.EnergyResidual), 0, 2e-7); Near(Value(s, 0, Field.MassResidual), 0, 1e-14); Near(Value(s, 0, Field.FuelResidual), 0, 1e-14); }
    private static void AnalyticWork()
    {
        var law = CompressibleFuelChecks.Law(); const double mass = 1e-4, pressure = 1e6;
        double volume = mass / (750 * (1 + pressure / 1.5e9)); Require(law.Evaluate(mass / volume, 300, out var initial));
        foreach (double ratio in new[] { -.0001, .0001, 1e-12 })
        {
            double next = volume * (1 + ratio); Require(law.AdiabaticVolume(mass, mass * initial.SpecificInternalEnergyJoulesPerKilogram, volume, next, out var w) == FuelVolumeStatus.Success);
            double expected = 1.5e9 * ((next - volume) - mass / 750 * Math.Log(next / volume));
            Near(w.EnergyChangeJoules, expected, 1e-12); Near(w.Fluid.TemperatureKelvin, 300, 2e-10); Near(w.EntropyResidualJoulesPerKelvin, 0, 1e-12);
        }
        volume = mass * 287 * 500 / 10000; Require(law.Evaluate(mass / volume, 500, out initial)); Require(initial.Phase == FuelVolumePhase.Vapor);
        foreach (double ratio in new[] { .8, 1.2, 1 + 1e-12 })
        {
            Require(law.AdiabaticVolume(mass, mass * 717.5 * 500, volume, volume * ratio, out var w) == FuelVolumeStatus.Success);
            Near(w.Fluid.TemperatureKelvin, 500 * Math.Pow(ratio, -.4), 1e-10);
            Near(w.EnergyChangeJoules, mass * 717.5 * 500 * (Math.Pow(ratio, -.4) - 1), 1e-12);
            Near(w.Fluid.PressurePascals * Math.Pow(ratio, 1.4), 10000, 1e-8);
        }
        Require(law.AdiabaticVolume(0, 1, 1, 1, out var bad) == FuelVolumeStatus.InvalidInput && bad == default);
    }
    // Independent scalar chemical-potential equality and entropy inversion. No production EOS is called here.
    private static (double Pressure, double Energy, double Entropy) Reference(double density, double t)
    {
        const double k = 1.5e9, rho0 = 750, r = 287, cl = 2000, cv = 717.5;
        double lo = 1, hi = 20e6, logT = Math.Log(t / 400), p0 = 1e5 * Math.Exp((cv + r - cl) / r * logT - 813000 / r * (1 / t - 1d / 400));
        for (int i = 0; i < 80; ++i) { double p = (lo + hi) / 2; if (r * t * Math.Log(p / p0) - k / rho0 * Math.Log(1 + p / k) > 0) hi = p; else lo = p; }
        double pressure = (lo + hi) / 2, vl = 1 / (rho0 * (1 + pressure / k)), vv = r * t / pressure, v = 1 / density, quality = (v - vl) / (vv - vl);
        if (quality < 0) { quality = 0; pressure = k * (density / rho0 - 1); }
        if (quality > 1) { quality = 1; pressure = density * r * t; }
        double y = pressure / k, cold = k / rho0 * (Math.Log(1 + y) - y / (1 + y));
        return (pressure, (1 - quality) * (cl * t - 813000 + cold) + quality * cv * t,
            (1 - quality) * cl * logT + quality * ((cv + r) * logT - r * Math.Log(pressure / 1e5) + 1037));
    }
    private static (double Pressure, double Energy, double Entropy) Isentrope(double density, double entropy)
    {
        double lo = 200, hi = 600;
        for (int i = 0; i < 60; ++i) { double t = (lo + hi) / 2; if (Reference(density, t).Entropy > entropy) hi = t; else lo = t; }
        return Reference(density, (lo + hi) / 2);
    }
    private static void PhaseWork()
    {
        var law = CompressibleFuelChecks.Law(); const double mass = 1e-4, volume = 5e-7;
        Require(law.Evaluate(mass / volume, 350, out var p)); Require(p.Phase == FuelVolumePhase.LiquidVapor);
        double entropy = Reference(mass / volume, 350).Entropy;
        Require(law.AdiabaticVolume(mass, mass * p.SpecificInternalEnergyJoulesPerKilogram, volume, 2 * volume, out var work) == FuelVolumeStatus.Success);
        var end = Isentrope(mass / (2 * volume), entropy); Near(work.Fluid.PressurePascals, end.Pressure, 1e-6); Near(work.Fluid.SpecificInternalEnergyJoulesPerKilogram, end.Energy, 2e-7);
        double integral = 0; const int count = 256; double h = volume / count;
        for (int i = 0; i <= count; ++i) integral += (i == 0 || i == count ? 1 : i % 2 == 0 ? 2 : 4) * Isentrope(mass / (volume + i * h), entropy).Pressure;
        Near(work.EnergyChangeJoules, -integral * h / 3, 2e-9);
        Require(law.AdiabaticVolume(mass, mass * p.SpecificInternalEnergyJoulesPerKilogram + work.EnergyChangeJoules, 2 * volume, volume, out var reverse) == FuelVolumeStatus.Success);
        Near(work.EnergyChangeJoules + reverse.EnergyChangeJoules, 0, 1e-10);
    }
    private static void Closed()
    {
        var s = CompiledModel.Compile(Model(tick: 50000)).CreateSimulation(); double initial = Value(s, 1, Field.InternalEnergy), entropy = Value(s, 1, Field.Entropy), speed = Value(s, 4, Field.Speed);
        double peak = 0;
        for (int i = 0; i < 10; ++i)
        {
            Step(s, 1000000); Balance(s); peak = Math.Max(peak, Value(s, 1, Field.VaporMassFraction));
            Near(Value(s, 1, Field.InternalEnergy) - initial, Value(s, 12, Field.HydraulicWork), 2e-9);
            Near(.5e-5 * (Math.Pow(Value(s, 4, Field.Speed), 2) - speed * speed) + Value(s, 12, Field.HydraulicWork), 0, 2e-9);
            Near(Value(s, 1, Field.Entropy), entropy, 2e-10); Near(Value(s, 1, Field.Volume), Value(s, 12, Field.Volume), 0);
        }
        Require(peak > 0); Console.WriteLine($"Compressible closed chamber peak vapor fraction: {peak:R}.");
    }
    private static void LiquidReference()
    {
        const double min = 1e-7, sweep = 1e-10, inertia = 1e-8, duration = .002;
        double initialVolume = min + sweep * (1 - Math.Cos(.4)) / 2, mass = initialVolume * 750 * (1 + 2e6 / 1.5e9);
        double Acceleration(double angle) => 1.5e9 * (mass / (750 * (min + sweep * (1 - Math.Cos(angle)) / 2)) - 1) * sweep * Math.Sin(angle) / (2 * inertia);
        (double Angle, double Speed) Integrate(int count)
        {
            double angle = .4, speed = 20, h = duration / count;
            for (int i = 0; i < count; ++i)
            {
                double a = Acceleration(angle), b = Acceleration(angle + h * speed / 2), c = Acceleration(angle + h * (speed + h * a / 2) / 2), d = Acceleration(angle + h * (speed + h * b / 2));
                angle += h / 6 * (speed + 2 * (speed + h * a / 2) + 2 * (speed + h * b / 2) + speed + h * c); speed += h / 6 * (a + 2 * b + 2 * c + d);
            }
            return (angle, speed);
        }
        var exact = Integrate(4000); var finer = Integrate(8000); Near(exact.Angle, finer.Angle, 1e-12); Near(exact.Speed, finer.Speed, 1e-10);
        var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 100000, 50000, 25000, 12500 })
        {
            var d = new ModelDefinition { StepNanoseconds = tick, Nodes = [NodeDefinition.Rotor(4, inertia, 20, .4), NodeDefinition.CompressibleFuelVolume(1, initialVolume, mass, 300, EquilibriumDuctChecks.Material())],
                Components = [ComponentDefinition.CompressibleChamber(12, 4, 1, Geometry with { MinimumReferenceVolume = new(min, Unit.CubicMeter), SweptVolume = new(sweep, Unit.CubicMeter) })] };
            var s = CompiledModel.Compile(d).CreateSimulation(); Step(s, 2000000); Balance(s); Near(Value(s, 1, Field.Temperature), 300, 2e-8);
            errors.Add(Math.Max(Math.Abs(Value(s, 4, Field.Angle) - exact.Angle), Math.Abs(Value(s, 4, Field.Speed) - exact.Speed) / 20));
        }
        Console.WriteLine("Compressible liquid chamber normalized errors: " + string.Join(", ", errors.Select(e => e.ToString("R"))));
        for (int i = 1; i < errors.Count; ++i) Require(errors[i - 1] / errors[i] is > 3.4 and < 4.6);
    }
    private static void DuctReference()
    {
        // Independent RK4 of the semi-discrete Euler balances with a moving mixed chamber.
        // The ideal-gas closure is analytic; the production phase inversion and work split are not used.
        const int cells = 6, dimension = 2 + 3 * cells; const double duration = .0001;
        double Volume(int i, double angle) => i == 0 ? 1e-7 + 1e-7 * (1 - Math.Cos(angle)) : i == cells - 1 ? .001 : .025e-6;
        double[] initial = new double[dimension]; initial[0] = .5; initial[1] = 5000;
        for (int i = 0; i < cells; ++i) { initial[2 + 3 * i] = Volume(i, .5) * 1e5 / (287 * 500); initial[4 + 3 * i] = initial[2 + 3 * i] * 717.5 * 500; }
        void Rates(double[] x, double[] rate)
        {
            Array.Clear(rate); rate[0] = x[1]; double speed = Math.Sqrt(1.5e9 / 750);
            for (int i = 0; i < cells; ++i) speed = Math.Max(speed, Math.Abs(x[3 + 3 * i] / x[2 + 3 * i]) + Math.Sqrt(1.5e9 / 750));
            double Pressure(int i) => .4 * (x[4 + 3 * i] - .5 * x[3 + 3 * i] * x[3 + 3 * i] / x[2 + 3 * i]) / Volume(i, x[0]);
            double chamberPressure = Pressure(0); rate[1] = chamberPressure * 1e-7 * Math.Sin(x[0]) / 1e-5; rate[4] = -chamberPressure * 1e-7 * Math.Sin(x[0]) * x[1];
            for (int face = 0; face < cells - 1; ++face)
            {
                int l = 2 + 3 * face, r = l + 3; double vl = Volume(face, x[0]), vr = Volume(face + 1, x[0]), ul = x[l + 1] / x[l], ur = x[r + 1] / x[r];
                double mass = 1e-6 * (.5 * (x[l + 1] / vl + x[r + 1] / vr) - .5 * speed * (x[r] / vr - x[l] / vl));
                double momentum = 1e-6 * (.5 * (x[l + 1] * ul / vl + Pressure(face) + x[r + 1] * ur / vr + Pressure(face + 1)) - .5 * speed * (x[r + 1] / vr - x[l + 1] / vl));
                double energy = 1e-6 * (.5 * ((x[l + 2] / vl + Pressure(face)) * ul + (x[r + 2] / vr + Pressure(face + 1)) * ur) - .5 * speed * (x[r + 2] / vr - x[l + 2] / vl));
                rate[l] -= mass; rate[r] += mass; rate[l + 2] -= energy; rate[r + 2] += energy;
                if (face > 0) rate[l + 1] -= momentum; if (face + 1 < cells - 1) rate[r + 1] += momentum;
            }
        }
        double[] Integrate(int count)
        {
            var x = initial.ToArray(); double[] a = new double[dimension], b = new double[dimension], c = new double[dimension], d = new double[dimension], y = new double[dimension]; double h = duration / count;
            for (int i = 0; i < count; ++i)
            {
                Rates(x, a); for (int j = 0; j < dimension; ++j) y[j] = x[j] + h * a[j] / 2; Rates(y, b);
                for (int j = 0; j < dimension; ++j) y[j] = x[j] + h * b[j] / 2; Rates(y, c);
                for (int j = 0; j < dimension; ++j) y[j] = x[j] + h * c[j]; Rates(y, d);
                for (int j = 0; j < dimension; ++j) x[j] += h / 6 * (a[j] + 2 * b[j] + 2 * c[j] + d[j]);
            }
            return x;
        }
        var reference = Integrate(4000); var finer = Integrate(8000); Near(reference[2], finer[2], 1e-17); Near(reference[1], finer[1], 1e-8);
        var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 1000, 500, 250, 125 })
        {
            var s = CompiledModel.Compile(Model(true, true, tick)).CreateSimulation(); Step(s, 100000); Balance(s);
            double error = Math.Abs(Value(s, 1, Field.Mass) - reference[2]) / initial[2];
            for (int i = 1; i < cells - 1; ++i) error = Math.Max(error, Math.Abs(Value(s, (uint)(99 + i), Field.Mass) - reference[2 + 3 * i]) / initial[2 + 3 * i]);
            errors.Add(error);
        }
        Console.WriteLine("Compressible moving duct RK4 normalized errors: " + string.Join(", ", errors.Select(e => e.ToString("R"))));
        for (int i = 1; i < errors.Count; ++i) Require(errors[i - 1] / errors[i] is > 1.7 and < 2.3);
    }
    private static void Composition()
    {
        var d = Model(true); d = d with { Nodes = [..d.Nodes, NodeDefinition.Rotor(5, 1e-5, 2500), NodeDefinition.Rotor(6, 1e-5), NodeDefinition.Hydraulic(7, 1e-10, 1e5)],
            Components = [..d.Components, ComponentDefinition.IdealGear(20, 4, 5, 2), ComponentDefinition.Clutch(21, 5, 6, .1, .05, 300, .5, heat: 3), ComponentDefinition.DisplacementPump(22, 5, 7, 1e-9, reservoirPressure: 1e5)] };
        var s = CompiledModel.Compile(d).CreateSimulation(); Step(s, 100000); Balance(s);
        Require(Value(s, 1, Field.VaporMassFraction) > 0); Require(Value(s, 10, Field.MassAtA) != 0);
        Near(Value(s, 4, Field.Speed), 2 * Value(s, 5, Field.Speed), 1e-9);
    }
    private static void Transactions()
    {
        var d = Model(true, true); d = d with { Components = [..d.Components, ComponentDefinition.Torque(20, 4, 200, 0)] };
        var s = CompiledModel.Compile(d).CreateSimulation(); var branch = s.Fork(); Step(s, 10000); for (int i = 0; i < 10; ++i) Step(branch, 1000); Require(Snapshot(s) == Snapshot(branch)); var before = Snapshot(s);
        Require(s.Step(10000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.Step(10000, [new(before.TimeNanoseconds + 5000, 200, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        Require(branch.SubmitInputs([new(200, .1)]) == SimulationStatus.Ok); Step(branch, 1000); Require(Snapshot(s) == before);
        var values = new Scalar[s.Model.OutputCount]; for (int i = 0; i < 20; ++i) { Step(s, 1000); s.ReadSnapshot(values); }
        long allocation = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 20; ++i) { Step(s, 1000); s.ReadSnapshot(values); } Require(GC.GetAllocatedBytesForCurrentThread() == allocation);
    }
    private static void Contracts()
    {
        var d = Model(true); var c = d.Components[0]; var compiled = CompiledModel.Compile(d);
        Require(!CompiledModel.TryCompile(d with { Components = [..d.Components, c with { Id = 13 }] }, out _, out _));
        Require(!CompiledModel.TryCompile(d with { Components = [c with { NodeB = 100 }, d.Components[1]] }, out _, out _));
        Require(!CompiledModel.TryCompile(d with { Components = [c with { FuelChamber = Geometry with { SweptVolume = new(1, Unit.Kelvin) } }, d.Components[1]] }, out _, out var diagnostic) && diagnostic!.Code == DiagnosticCode.Unit);
        Require(!CompiledModel.TryCompile(d with { Nodes = d.Nodes.Select(n => n.Id == 1 ? n with { Storage = new(1e-6, Unit.CubicMeter) } : n).ToArray() }, out _, out _));
        d.Components[0] = c with { FuelChamber = Geometry with { SweptVolume = new(3e-7, Unit.CubicMeter) } };
        var old = compiled.CreateSimulation(); var fresh = CompiledModel.Compile(Model(true)).CreateSimulation(); Step(old, 1000); Step(fresh, 1000); Require(Snapshot(old) == Snapshot(fresh));
    }
}
