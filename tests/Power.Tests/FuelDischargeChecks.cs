// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class FuelDischargeChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel discharge / finite liquid displacement energy kinetic heat and total entropy", Transfer),
        ("fuel discharge / moving cylinder phase work and real needle gating", MovingNeedle),
        ("fuel discharge / frozen mixture sonic limiting into finite receiver", Mixture),
        ("finite receiver / existing liquid injector displacement work remains internal", LegacyLiquid),
        ("fuel discharge / independent liquid-nozzle finite-receiver heat and momentum ODE", Reference),
        ("fuel discharge / full batch replay cancellation late rollback forks and zero allocations", Transactions),
        ("fuel discharge / explicit transit material ports and density contracts", Contracts)
    ];
    internal static EquilibriumFuelDefinition Material => EquilibriumFuelNetworkChecks.Material with
    { LiquidSpecificHeat = new(800, Unit.JoulePerKilogramKelvin), MaximumTemperature = new(2500, Unit.Kelvin) };
    internal static FuelFilmDefinition Film => new()
    {
        InitialMass = new(0, Unit.Kilogram), InitialTemperature = new(350, Unit.Kelvin), LiquidSpecificHeat = Material.LiquidSpecificHeat,
        SaturationTemperature = Material.ReferenceTemperature, LatentInternalEnergy = Material.LatentInternalEnergy, FiniteVolumeDensity = Material.LiquidDensity,
        PhaseEquilibrium = new() { ReferencePressure = Material.ReferencePressure, MinimumTemperature = Material.MinimumTemperature, MaximumTemperature = Material.MaximumTemperature }
    };
    internal static GasDefinition Gas => new()
    {
        GasConstant = Material.GasConstant, Gamma = 1.4,
        Premixed = new() { LowerHeatingValue = Material.LowerHeatingValue, StoichiometricAirFuelRatio = 14.7, InitialFractions = new(0, 1) }
    };
    internal static ModelDefinition Model(double wallFraction = 1, bool moving = false, bool needle = false)
    {
        NodeDefinition[] nodes = [NodeDefinition.Rotor(1, .1, 50, Math.PI), moving ? NodeDefinition.CylinderGas(2, 1e5, 350) with { Gas = Gas } : NodeDefinition.GasVolume(2, 1e-5, 1e5, 350) with { Gas = Gas },
            NodeDefinition.Thermal(3, 1, 350), NodeDefinition.FuelVolume(4, 8e-7, 750 * (8e-7 + 5e-13 * 8e5), 300, Material)];
        var discharge = new FuelDischargeDefinition { FilmComponent = 11, PhaseModel = FuelDischargePhaseModel.Frozen, WallKineticFraction = new(wallFraction, Unit.Fraction),
            Needle = needle ? new() { NeedleNode = 5, ClosedPosition = new(0, Unit.Meter), FullOpenPosition = new(.001, Unit.Meter) } : null };
        ComponentDefinition[] components = [ComponentDefinition.FiniteLiquidFilm(11, 2, 3, 0, Film), ComponentDefinition.FuelDischargeNozzle(12, 4, 2, discharge, 2e-8, .7, 100)];
        if (moving) components = [ComponentDefinition.GasCylinder(10, 1, 2, MovingCylinderChecks.Geometry), ..components];
        if (needle) { nodes = [..nodes, NodeDefinition.Translational(5, .001)]; components = [..components, ComponentDefinition.Force(13, 5, 200, 0)]; }
        return new() { StepNanoseconds = 100_000, Nodes = nodes, Components = components };
    }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Step(Simulation s, ulong time) => Require(s.Step(time) == SimulationStatus.Ok, "Fuel discharge step failed.");
    private static void Balance(Simulation s)
    { Near(Value(s, 0, Field.EnergyResidual), 0, 2e-7); Near(Value(s, 0, Field.MassResidual), 0, 1e-14); Near(Value(s, 0, Field.FuelResidual), 0, 1e-14); }
    private static double Entropy(Simulation s, double cavity)
    {
        var caloric = new EquilibriumFuelFilm(800, 717.5, 400, 300000, 0); var material = new FuelPhaseEquilibrium(caloric, 287, 1e5, 200, 2500);
        var law = new FiniteTankFuelEquilibrium(material, cavity, 750);
        double fuel = Value(s, 11, Field.Mass) + Value(s, 2, Field.FuelMass), carrier = Value(s, 2, Field.FreshAirMass) + Value(s, 2, Field.ProductMass);
        Require(law.TryPoint(Value(s, 2, Field.Temperature), fuel, carrier, out var point));
        return Value(s, 4, Field.Entropy) + law.Entropy(point, carrier) + Math.Log(Value(s, 3, Field.Temperature) / 350);
    }
    private static void Transfer()
    {
        foreach (double fraction in new[] { 0d, .5, 1d })
        {
            var s = CompiledModel.Compile(Model(fraction)).CreateSimulation(); double mass = Value(s, 4, Field.Mass), sourceEnergy = Value(s, 4, Field.InternalEnergy), entropy = Entropy(s, 1e-5);
            for (int n = 0; n < 10; ++n) { Step(s, 2_000_000); Balance(s); }
            double delivered = Value(s, 12, Field.TotalFuelDelivered); Require(delivered > 0);
            Near(mass - Value(s, 4, Field.Mass), delivered, 1e-16);
            Near(sourceEnergy - Value(s, 4, Field.InternalEnergy), Value(s, 12, Field.TransportEnergy), 2e-11);
            Near(Value(s, 12, Field.FluidHeat), fraction * Value(s, 12, Field.JetKineticEnergy), 2e-12);
            Near(Value(s, 3, Field.Temperature) - 350, Value(s, 12, Field.FluidHeat), 1e-10);
            Near(Value(s, 2, Field.Volume) + Value(s, 11, Field.Volume), 1e-5, 1e-20);
            Require(Entropy(s, 1e-5) >= entropy - 1e-11 && Value(s, 12, Field.JetImpulse) > 0 && Value(s, 12, Field.Force) > 0);
            Near(Value(s, 0, Field.SourceWork), 0, 0);
            Console.WriteLine($"Finite receiver wall fraction {fraction:R}: delivered {delivered:R} kg; liquid displacement {Value(s, 11, Field.Volume):R} m3.");
        }
    }
    private static void MovingNeedle()
    {
        var s = CompiledModel.Compile(Model(moving: true)).CreateSimulation(); Step(s, 10_000_000); Balance(s);
        Near(Value(s, 2, Field.Volume) + Value(s, 11, Field.Volume), Value(s, 10, Field.Volume), 1e-18);
        var gated = CompiledModel.Compile(Model(needle: true)).CreateSimulation(); Step(gated, 1_000_000); Near(Value(gated, 12, Field.TotalFuelDelivered), 0, 0);
        Require(gated.SubmitInputs([new(200, 1)]) == SimulationStatus.Ok); Step(gated, 1_000_000); Balance(gated);
        Require(Value(gated, 5, Field.Displacement) > 0 && Value(gated, 12, Field.TotalFuelDelivered) > 0);
    }
    private static void Contracts()
    {
        var definition = Model();
        var invalid = definition with { Components = definition.Components.Select(c => c.FuelDischarge is null ? c : c with { FuelDischarge = c.FuelDischarge with { PhaseModel = 0 } }).ToArray() };
        Require(!CompiledModel.TryCompile(invalid, out _, out var mode) && mode!.Field == "fuel_discharge.phase_mode");
        invalid = definition with { Nodes = definition.Nodes.Select(n => n.Id == 4 ? n with { Fuel = Material with { LiquidDensity = new(800, Unit.KilogramPerCubicMeter) } } : n).ToArray() };
        Require(!CompiledModel.TryCompile(invalid, out _, out var material) && material!.Field == "fuel_discharge.material");
        invalid = definition with { Components = definition.Components.Select(c => c.FuelFilm is null ? c : c with { FuelFilm = Film with { FiniteVolumeDensity = new(750, Unit.Kelvin) } }).ToArray() };
        Require(!CompiledModel.TryCompile(invalid, out _, out var unit) && unit!.Code == DiagnosticCode.Unit);
    }
    private static void Mixture()
    {
        var definition = Model(); definition = definition with { Nodes = definition.Nodes.Select(n => n.Id == 4 ? NodeDefinition.FuelVolume(4, 8e-7, 1e-5, 500, Material) : n).ToArray() };
        var s = CompiledModel.Compile(definition).CreateSimulation(); Require(Value(s, 4, Field.VaporMassFraction) is > 0 and < 1);
        Step(s, 2_000_000); Balance(s); Require(Value(s, 12, Field.Choked) == 1 && Value(s, 12, Field.ThroatPressure) > Value(s, 2, Field.Pressure));
        Require(Value(s, 12, Field.TotalFuelDelivered) > 0 && Value(s, 12, Field.JetKineticEnergy) > 0);
    }
    private static void LegacyLiquid()
    {
        var definition = LiquidFuelInjectorChecks.Model();
        definition = definition with { Components = definition.Components.Select(c => c.FuelFilm is null ? c :
            c with { Kind = ComponentKind.FiniteFuelFilm, FuelFilm = Film with { InitialTemperature = new(300, Unit.Kelvin) } }).ToArray() };
        var s = CompiledModel.Compile(definition).CreateSimulation(); double cavity = Value(s, 2, Field.Volume);
        Step(s, 50_000_000); Balance(s);
        Require(Value(s, 12, Field.TotalFuelDelivered) > 0 && Value(s, 12, Field.HydraulicWork) > 0);
        Near(Value(s, 0, Field.SourceWork), 0, 0);
        Near(Value(s, 2, Field.Volume) + Value(s, 10, Field.Volume), cavity, 1e-20);
    }
    private static (double P, double T) ReferenceReceiver(double fuel, double carrier, double energy)
    {
        const double r = 287, cv = 717.5, cl = 800, offset = -333000, capacity = 1e-5;
        (double P, double E) At(double t)
        {
            double p0 = 1e5 * Math.Pow(t / 400, (cv + r - cl) / r) * Math.Exp(offset / r * (1 / t - 1 / 400d));
            double F(double vapor)
            {
                if (vapor == 0) return double.NegativeInfinity;
                double gasVolume = capacity - (fuel - vapor) / 750;
                return Math.Log(vapor * r * t / gasVolume / p0) - (vapor + carrier) / (750 * gasVolume);
            }
            double lo = 0, hi = fuel;
            if (fuel > 0 && F(fuel) > 0)
                for (int i = 0; i < 64; ++i) { double middle = (lo + hi) / 2; if (F(middle) > 0) hi = middle; else lo = middle; }
            else lo = hi;
            double vapor = (lo + hi) / 2, liquid = fuel - vapor;
            double pressure = (carrier + vapor) * r * t / (capacity - liquid / 750);
            double stored = liquid * (cl * t + offset) + (carrier + vapor) * cv * t;
            return (pressure, stored);
        }
        double lower = 200, upper = 2500;
        for (int i = 0; i < 52; ++i) { double t = (lower + upper) / 2; if (At(t).E > energy) upper = t; else lower = t; }
        double temperature = (lower + upper) / 2; return (At(temperature).P, temperature);
    }
    private static void Reference()
    {
        const double carrier = 1e5 * 1e-5 / (287 * 350), duration = .02, wallFraction = .5;
        double[] initial = [8e5, 0, carrier * 717.5 * 350, 350, 0, 0];
        void Rates(double[] x, double[] rate)
        {
            var receiver = ReferenceReceiver(x[1], carrier, x[2]); double head = Math.Max(0, x[0] - receiver.P);
            double velocity = Math.Sqrt(2 * head / 750), flow = .7 * 2e-8 * 750 * velocity, kinetic = head / 750;
            double enthalpy = 800 * 300 - 333000 + x[0] / 750;
            rate[0] = -flow / (750 * 5e-13); rate[1] = flow; rate[2] = flow * (enthalpy - wallFraction * kinetic);
            rate[3] = wallFraction * flow * kinetic; rate[4] = flow * kinetic; rate[5] = flow * velocity;
        }
        double[] Integrate(int count)
        {
            double[] x = initial.ToArray(), stage = new double[6], a = new double[6], b = new double[6], c = new double[6], d = new double[6]; double h = duration / count;
            for (int n = 0; n < count; ++n)
            {
                Rates(x, a); for (int j = 0; j < 6; ++j) stage[j] = x[j] + h * a[j] / 2; Rates(stage, b);
                for (int j = 0; j < 6; ++j) stage[j] = x[j] + h * b[j] / 2; Rates(stage, c);
                for (int j = 0; j < 6; ++j) stage[j] = x[j] + h * c[j]; Rates(stage, d);
                for (int j = 0; j < 6; ++j) x[j] += h / 6 * (a[j] + 2 * b[j] + 2 * c[j] + d[j]);
            }
            return x;
        }
        var reference = Integrate(1000); var finer = Integrate(2000);
        for (int j = 0; j < 6; ++j) Near(reference[j], finer[j], j == 0 ? 1e-4 : j == 1 ? 1e-12 : 1e-8);
        var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 200_000, 100_000, 50_000, 25_000 })
        {
            var definition = Model(wallFraction) with { StepNanoseconds = tick }; var s = CompiledModel.Compile(definition).CreateSimulation(); Step(s, 20_000_000); Balance(s);
            double[] values = [Value(s, 4, Field.Pressure), Value(s, 12, Field.TotalFuelDelivered), Value(s, 11, Field.InternalEnergy) + Value(s, 2, Field.InternalEnergy),
                Value(s, 3, Field.Temperature), Value(s, 12, Field.JetKineticEnergy), Value(s, 12, Field.JetImpulse)];
            double error = 0;
            for (int j = 0; j < 6; ++j) error = Math.Max(error, Math.Abs(values[j] - reference[j]) / (j == 0 ? 8e5 : j == 1 ? 1e-5 : j == 3 ? 350 : j == 2 ? 3 : .01));
            errors.Add(error);
        }
        Console.WriteLine("Finite receiver discharge ODE normalized errors: " + string.Join(", ", errors.Select(e => e.ToString("R"))));
        for (int j = 1; j < errors.Count; ++j) Require(errors[j - 1] / errors[j] is > 1.6 and < 2.4, $"Finite receiver discharge refinement: {errors[j - 1]:R}/{errors[j]:R}");
    }
    private static void Transactions()
    {
        var definition = Model(moving: true); definition = definition with { Components = [..definition.Components, ComponentDefinition.Torque(20, 1, 200, 0)] };
        var s = CompiledModel.Compile(definition).CreateSimulation(); var replay = s.Fork();
        ScheduledInput[] events = [new(2_000_000, 100, .5), new(5_000_000, 100, 0), new(7_000_000, 100, 1)];
        Require(s.Step(10_000_000, events) == SimulationStatus.Ok);
        for (ulong time = 0; time < 10_000_000; time += 1_000_000)
            Require(replay.Step(1_000_000, events.Where(e => e.TimeNanoseconds >= time && e.TimeNanoseconds < time + 1_000_000).ToArray()) == SimulationStatus.Ok);
        Require(Snapshot(s) == Snapshot(replay)); var before = Snapshot(s);
        Require(s.Step(10_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.Step(10_000_000, [new(before.TimeNanoseconds + 2_000_000, 200, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var branch = s.Fork(); Require(branch.SubmitInputs([new(100, 0)]) == SimulationStatus.Ok); Step(branch, 1_000_000); Require(Snapshot(s) == before);
        var values = new Scalar[s.Model.OutputCount]; for (int n = 0; n < 20; ++n) { Step(s, 100_000); s.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread(); for (int n = 0; n < 20; ++n) { Step(s, 100_000); s.ReadSnapshot(values); }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Fuel discharge stepping allocated.");
    }
}
