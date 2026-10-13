// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class FuelPhaseEquilibriumChecks
{
    private const double R = 287, Cv = R / .4, Cl = 2000, Reference = 350, Pressure = 30000, Latent = 300000;
    private const double Offset = (Cv - Cl) * Reference - Latent;
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("fuel phase / chemical-potential saturation reference and Clapeyron slope", Saturation),
        ("fuel phase / independent entropy maximum / evaporation condensation and dry vapor", Flash),
        ("fuel phase / finite wall energy and entropy / dry cooling and curve limits", FiniteBath),
        ("fuel phase graph / reversible mass histories and complete ledgers", Closed),
        ("fuel phase shared wall / equilibrium and legacy films retain independent histories", SharedWall),
        ("fuel phase vent / independent simultaneous thermal and enthalpy ODE / refinement", Transport),
        ("fuel phase moving chamber / independent equilibrium crank-work ODE / refinement", Moving),
        ("fuel phase cavitation / inlet and rail saturation margins / complete rollback", Cavitation),
        ("fuel phase transactions / condensation rollback cancellation forks and allocations", Transactions),
        ("fuel phase contracts / property interval units ownership and immutable compilation", Contracts)
    ];

    internal static FuelPhaseEquilibriumDefinition Phase(double pressure = Pressure, double min = 200, double max = 550) => new()
    {
        ReferencePressure = new(pressure, Unit.Pascal), MinimumTemperature = new(min, Unit.Kelvin), MaximumTemperature = new(max, Unit.Kelvin)
    };
    internal static FuelFilmDefinition Film(double liquid = .003) => new()
    {
        InitialMass = new(liquid, Unit.Kilogram), InitialTemperature = new(Reference, Unit.Kelvin),
        LiquidSpecificHeat = new(Cl, Unit.JoulePerKilogramKelvin), SaturationTemperature = new(Reference, Unit.Kelvin),
        LatentInternalEnergy = new(Latent, Unit.JoulePerKilogram), PhaseEquilibrium = Phase()
    };
    private static GasDefinition Gas(double fraction = .3) => new()
    {
        GasConstant = new(R, Unit.JoulePerKilogramKelvin), Gamma = 1.4,
        Premixed = new() { LowerHeatingValue = new(44e6, Unit.JoulePerKilogram), StoichiometricAirFuelRatio = 14.7, InitialFractions = new(fraction, 1 - fraction) }
    };
    internal static ModelDefinition Model(ulong tick = 1_000_000, double wall = 430, double conductance = 1, double vent = 0) => new()
    {
        StepNanoseconds = tick,
        Nodes = [NodeDefinition.Rotor(1, 1), NodeDefinition.GasVolume(2, .01, 100000, Reference) with { Gas = Gas() }, NodeDefinition.Thermal(3, 20, wall)],
        Components = [ComponentDefinition.LiquidFilm(10, 2, 3, conductance, Film()), ComponentDefinition.Torque(11, 1, 100, 0),
            ComponentDefinition.GasReservoir(12, 2, vent == 0 ? 1e-6 : vent, 1000, 350, .7, 110, vent == 0 ? 0 : 1) with { ReservoirFractions = new(0, 1) }]
    };
    private static FuelPhaseEquilibrium Law(double conductance = 0) => new(new(Cl, Cv, Reference, Latent, conductance), R, Pressure, 200, 550);
    private static void Step(Simulation s, ulong time) => Require(s.Step(time) == SimulationStatus.Ok, "Equilibrium step failed.");
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Ledgers(Simulation s)
    {
        Near(Value(s, 0, Field.EnergyResidual), 0, 2e-7); Near(Value(s, 0, Field.MassResidual), 0, 2e-14);
        Near(Value(s, 0, Field.FuelResidual), 0, 2e-14); Near(Value(s, 0, Field.FreshAirResidual), 0, 2e-14);
    }
    private static double SaturationReference(double t) => Pressure * Math.Pow(t / Reference, (Cv + R - Cl) / R) * Math.Exp(Offset / R * (1 / t - 1 / Reference));

    private static void Saturation()
    {
        var phase = Law(); Require(phase.TrySaturationPressure(Reference, out double reference)); Near(reference, Pressure, 0);
        double prior = 0;
        foreach (double t in new[] { 200d, 300d, 350d, 450d, 550d })
        {
            Require(phase.TrySaturationPressure(t, out double p) && p > prior); prior = p;
            Near(p, SaturationReference(t), 2e-9);
            Near(phase.LatentEnthalpy(t), (Cv + R) * t - (Cl * t + Offset), 1e-9);
            if (t == 200 || t == 550) continue;
            Require(phase.TrySaturationPressure(t + .001, out double high)); Require(phase.TrySaturationPressure(t - .001, out double low));
            Near((Math.Log(high) - Math.Log(low)) / .002, ((Cv + R - Cl) * t - Offset) / (R * t * t), 1e-11);
        }
        var equalCp = new FuelPhaseEquilibrium(new(Cv + R, Cv, Reference, Latent, 0), R, Pressure, 200, 600);
        Require(equalCp.TrySaturationPressure(420, out double constantLatent));
        Near(constantLatent, Pressure * Math.Exp((Latent + R * Reference) / R * (1 / Reference - 1.0 / 420)), 1e-9);
        Require(!phase.TrySaturationPressure(199, out _) && !phase.TrySaturationPressure(551, out _));
        Throws<ArgumentException>(() => new FuelPhaseEquilibrium(new(Cl, Cv, Reference, Latent, 0), R, Pressure, 200, 1000));
    }

    private static double Entropy(double liquid, double liquidTemperature, double vapor, double carrier, double gasTemperature, double volume)
    {
        double entropy = liquid * Cl * Math.Log(liquidTemperature / Reference);
        if (vapor > 0) entropy += vapor * ((Cv + R) * Math.Log(gasTemperature / Reference) - R * Math.Log(vapor * R * gasTemperature / volume / Pressure) + (Latent + R * Reference) / Reference);
        if (carrier > 0) entropy += carrier * (Cv * Math.Log(gasTemperature / Reference) + R * Math.Log(volume * Pressure / (carrier * R * Reference)));
        return entropy;
    }

    // Independent fixed-U,V entropy maximization, with vapor mass as the optimization variable.
    private static (double Vapor, double Temperature) EntropyMaximum(double energy, double fuel, double carrier, double volume)
    {
        double Temperature(double vapor) => (energy - (fuel - vapor) * Offset) / ((carrier + vapor) * Cv + (fuel - vapor) * Cl);
        double Entropy(double vapor)
        {
            double t = Temperature(vapor), liquid = fuel - vapor;
            if (t <= 0 || liquid > 0 && (t < 200 || t > 550)) return double.NegativeInfinity;
            return FuelPhaseEquilibriumChecks.Entropy(liquid, t, vapor, carrier, t, volume);
        }
        double lo = 0, hi = fuel, ratio = (Math.Sqrt(5) - 1) / 2;
        double a = hi - ratio * (hi - lo), b = lo + ratio * (hi - lo);
        for (int i = 0; i < 100; ++i)
        {
            if (Entropy(a) < Entropy(b)) { lo = a; a = b; b = lo + ratio * (hi - lo); }
            else { hi = b; b = a; a = hi - ratio * (hi - lo); }
        }
        double result = (lo + hi) / 2;
        if (Entropy(fuel) >= Entropy(result)) result = fuel;
        return (result, Temperature(result));
    }

    private static void Flash()
    {
        var law = Law();
        foreach (var (liquid, vapor, gasTemperature, liquidTemperature, carrier) in new[]
        {
            (.003, .001, 350d, 350d, .007), (.001, .005, 350d, 350d, .007), (0d, .005, 350d, 350d, .007),
            (0d, .0001, 900d, 350d, .007), (.002, .002, 410d, 300d, .007), (.001, .005, 350d, 350d, 0d)
        })
        {
            const double volume = .01;
            var state = law.CreateLiquidState(liquid, liquidTemperature); double gasEnergy = (vapor + carrier) * Cv * gasTemperature;
            Require(law.TryAdvance(state, vapor, carrier, gasEnergy, volume, 350, 20, 0, out var result));
            var expected = EntropyMaximum(state.ThermalEnergyJoules + gasEnergy, liquid + vapor, carrier, volume);
            Near(result.VaporMassKilograms, expected.Vapor, 3e-10); Near(result.TemperatureKelvin, expected.Temperature, 4e-5);
            Near(result.Liquid.LiquidMassKilograms + result.VaporMassKilograms, liquid + vapor, 2e-18);
            Near(result.Liquid.ThermalEnergyJoules + result.GasEnergyJoules, state.ThermalEnergyJoules + gasEnergy, 1e-10);
            Near(result.HeatFromWallJoules, 0, 0); Near(result.WallTemperatureKelvin, 350, 0);
            Require(Entropy(result.Liquid.LiquidMassKilograms, result.TemperatureKelvin, result.VaporMassKilograms, carrier, result.TemperatureKelvin, volume) >=
                Entropy(liquid, liquidTemperature, vapor, carrier, gasTemperature, volume) - 1e-10, "Adiabatic flash decreased entropy.");
            if (result.Liquid.LiquidMassKilograms > 0)
            { Require(law.TrySaturationPressure(result.TemperatureKelvin, out double p)); Near(result.VaporMassKilograms * R * result.TemperatureKelvin / volume, p, 1e-8); }
        }
    }

    private static void FiniteBath()
    {
        var phase = Law(3); const double mass = .01, capacity = mass * Cv, wallCapacity = 20, wall = 250, gas = 450, dt = .1;
        Require(phase.TryAdvance(new(0, 0), 0, mass, capacity * gas, .01, wall, wallCapacity, dt, out var result));
        double q = dt * 3 * (wall - gas) / (1 + dt * 3 * (1 / capacity + 1 / wallCapacity));
        Near(result.HeatFromWallJoules, q, 1e-12); Near(result.TemperatureKelvin, gas + q / capacity, 1e-12);
        Near(result.GasEnergyJoules + wallCapacity * result.WallTemperatureKelvin, capacity * gas + wallCapacity * wall, 1e-10);
        Require(!phase.TryAdvance(new(.001, -1e9), .001, mass, 100, .01, 250, 20, dt, out _));
        Require(!phase.TryAdvance(new(0, 1), .001, mass, 100, .01, 250, 20, dt, out _));
        Require(!phase.TryAdvance(new(0, 0), .1, mass, 1e9, .00001, 250, 20, dt, out _));
        foreach (double temperature in new[] { 200d, 550d })
        {
            Require(phase.TryPoint(temperature, .02, .01, .01, out var point));
            var liquid = phase.CreateLiquidState(point.LiquidMassKilograms, temperature);
            Require(phase.TryAdvance(liquid, point.VaporMassKilograms, .01, (.01 + point.VaporMassKilograms) * Cv * temperature, .01, temperature, 20, 0, out var edge));
            Near(edge.TemperatureKelvin, temperature, 1e-9);
        }
        Require(phase.TryPoint(Reference, .006, .007, .01, out var equilibrium));
        var wet = phase.CreateLiquidState(equilibrium.LiquidMassKilograms, Reference);
        foreach (double bath in new[] { 250d, 350d, 500d })
            foreach (double interval in new[] { .001, 1d, 100d })
            {
                double initialEntropy = Entropy(wet.LiquidMassKilograms, Reference, equilibrium.VaporMassKilograms, .007, Reference, .01) + 20 * Math.Log(bath / Reference);
                Require(phase.TryAdvance(wet, equilibrium.VaporMassKilograms, .007, (.007 + equilibrium.VaporMassKilograms) * Cv * Reference, .01, bath, 20, interval, out var next));
                double entropy = Entropy(next.Liquid.LiquidMassKilograms, next.TemperatureKelvin, next.VaporMassKilograms, .007, next.TemperatureKelvin, .01) + 20 * Math.Log(next.WallTemperatureKelvin / Reference);
                Require(entropy >= initialEntropy - 1e-10, "Finite-wall exchange decreased total entropy.");
            }
    }

    private static void Closed()
    {
        foreach (double wall in new[] { 250d, 430d })
        {
            var s = CompiledModel.Compile(Model(wall: wall)).CreateSimulation(); double fuel = Value(s, 10, Field.Mass) + Value(s, 2, Field.FuelMass);
            Step(s, 2_000_000_000); Ledgers(s);
            double liquid = Value(s, 10, Field.Mass), vapor = Value(s, 2, Field.FuelMass), t = Value(s, 10, Field.Temperature);
            Near(liquid + vapor, fuel, 1e-14); Near(t, Value(s, 2, Field.Temperature), 1e-8);
            Require(liquid > 0); Near(Value(s, 10, Field.VaporPartialPressure), SaturationReference(t), 1e-7);
            Near(.003 - liquid, Value(s, 10, Field.EvaporatedFuelMass) - Value(s, 10, Field.CondensedFuelMass), 1e-14);
            Require(wall > Reference ? Value(s, 10, Field.MassFlow) > 0 : Value(s, 10, Field.MassFlow) < 0);
            Require(wall > Reference ? Value(s, 10, Field.EvaporatedFuelMass) > 0 : Value(s, 10, Field.CondensedFuelMass) > 0);
        }
    }

    private static double[] Integrate(double[] initial, double duration, int steps, Action<double[], double[]> rates)
    {
        var state = initial.ToArray(); var stage = new double[state.Length]; var a = new double[state.Length];
        var b = new double[state.Length]; var c = new double[state.Length]; var d = new double[state.Length]; double h = duration / steps;
        for (int n = 0; n < steps; ++n)
        {
            rates(state, a); for (int i = 0; i < state.Length; ++i) stage[i] = state[i] + h / 2 * a[i];
            rates(stage, b); for (int i = 0; i < state.Length; ++i) stage[i] = state[i] + h / 2 * b[i];
            rates(stage, c); for (int i = 0; i < state.Length; ++i) stage[i] = state[i] + h * c[i];
            rates(stage, d); for (int i = 0; i < state.Length; ++i) state[i] += h / 6 * (a[i] + 2 * b[i] + 2 * c[i] + d[i]);
        }
        return state;
    }
    private static void SharedWall()
    {
        foreach (bool equilibrium in new[] { false, true })
        {
            var original = Model(); var second = FuelFilmChecks.Definition() with { PhaseEquilibrium = equilibrium ? Phase(100000, 200, 600) : null };
            var model = original with
            {
                Nodes = [..original.Nodes, NodeDefinition.GasVolume(4, .01, 100000, 300) with { Gas = Gas(0) }],
                Components = [..original.Components, ComponentDefinition.LiquidFilm(14, 4, 3, .1, second)]
            };
            var s = CompiledModel.Compile(model).CreateSimulation(); Step(s, 2_000_000_000); Ledgers(s);
            double condensed = equilibrium ? Value(s, 14, Field.CondensedFuelMass) : 0;
            Near(20e-6 - Value(s, 14, Field.Mass), Value(s, 14, Field.EvaporatedFuelMass) - condensed, 1e-14);
            Near(.003 - Value(s, 10, Field.Mass), Value(s, 10, Field.EvaporatedFuelMass) - Value(s, 10, Field.CondensedFuelMass), 1e-14);
            Require(s.Model.EquilibriumFilmCount == (equilibrium ? 2 : 1));
        }
    }
    private static (double Vapor, double Energy, double Capacity, double Latent) ReferencePoint(double t, double fuel, double carrier, double volume)
    {
        double vapor = Math.Min(fuel, SaturationReference(t) * volume / (R * t)), liquid = fuel - vapor, latent = (Cv - Cl) * t - Offset;
        return (vapor, (carrier + vapor) * Cv * t + liquid * (Cl * t + Offset),
            (carrier + vapor) * Cv + liquid * Cl + (liquid > 0 ? vapor * latent * latent / (R * t * t) : 0), latent);
    }
    private static void Transport() { TransportReference(430); TransportReference(250); }
    private static void TransportReference(double wall)
    {
        const double volume = .01, area = 1e-5, duration = .4;
        double mass = 1e5 * volume / (R * Reference), fuel = .003 + .3 * mass, carrier = .7 * mass;
        double[] initial = [carrier, fuel, Reference, wall, 0, 0];
        void Rates(double[] state, double[] rate)
        {
            var point = ReferencePoint(state[2], state[1], state[0], volume); double gas = state[0] + point.Vapor;
            Require(point.Vapor < state[1]); double pressure = gas * R * state[2] / volume;
            Require(pressure > 2000);
            double flow = .7 * area * pressure / Math.Sqrt(R * state[2]) * Math.Sqrt(1.4) * Math.Pow(2 / 2.4, 3);
            double vaporFlow = flow * point.Vapor / gas, carrierFlow = flow - vaporFlow;
            double heat = state[3] - state[2], liquidSpecific = Cl * state[2] + Offset;
            rate[0] = -carrierFlow; rate[1] = -vaporFlow;
            rate[2] = (heat - flow * (Cv + R) * state[2] + carrierFlow * Cv * state[2] + vaporFlow * liquidSpecific) / point.Capacity;
            rate[3] = -heat / 20;
            double netPhase = point.Vapor * point.Latent / (R * state[2] * state[2]) * rate[2] + vaporFlow;
            rate[4] = Math.Max(0, netPhase); rate[5] = Math.Max(0, -netPhase);
        }
        var reference = Integrate(initial, duration, 4000, Rates); var finer = Integrate(initial, duration, 8000, Rates);
        for (int i = 0; i < 6; ++i) Near(reference[i], finer[i], 1e-9);
        double previous = 0; var errors = new List<double>();
        foreach (ulong step in new ulong[] { 20_000_000, 10_000_000, 5_000_000, 2_500_000 })
        {
            var s = CompiledModel.Compile(Model(step, wall: wall, vent: area)).CreateSimulation(); Step(s, 400_000_000); Ledgers(s);
            double error = Math.Max(Math.Abs(Value(s, 2, Field.Temperature) - reference[2]) / 100, Math.Abs(Value(s, 10, Field.Mass) + Value(s, 2, Field.FuelMass) - reference[1]) / fuel);
            error = Math.Max(error, Math.Abs(Value(s, 10, Field.EvaporatedFuelMass) - reference[4]) / fuel);
            error = Math.Max(error, Math.Abs(Value(s, 10, Field.CondensedFuelMass) - reference[5]) / fuel);
            if (previous > 0) Require(previous / error > 1.7 && previous / error < 2.3, $"Phase transport refinement: {previous:R} / {error:R}");
            previous = error; errors.Add(error);
        }
        Console.WriteLine($"Phase vent normalized errors ({wall:R} K wall): " + string.Join(", ", errors.Select(v => v.ToString("R"))));
    }

    private static void Moving()
    {
        const double area = Math.PI * .086 * .086 / 4, radius = .043, rod = .143, duration = .02;
        (double Volume, double Derivative) Geometry(double angle)
        {
            double sin = Math.Sin(angle), cos = Math.Cos(angle), root = Math.Sqrt(rod * rod - radius * radius * sin * sin);
            return (area * .086 / 9 + area * (radius * (1 - cos) + rod - root), area * (radius * sin + radius * radius * sin * cos / root));
        }
        double volume = Geometry(.5).Volume, mass = 1e5 * volume / (R * Reference), fuel = .00002 + .3 * mass, carrier = .7 * mass;
        double[] initial = [.5, 20, Reference];
        void Rates(double[] state, double[] rate)
        {
            var geometry = Geometry(state[0]); var point = ReferencePoint(state[2], fuel, carrier, geometry.Volume);
            double pressure = (carrier + point.Vapor) * R * state[2] / geometry.Volume;
            rate[0] = state[1]; rate[1] = (pressure - 100000) * geometry.Derivative / .1;
            rate[2] = -(pressure + (point.Vapor < fuel ? point.Vapor * point.Latent / geometry.Volume : 0)) * geometry.Derivative * state[1] / point.Capacity;
        }
        var reference = Integrate(initial, duration, 4000, Rates); var finer = Integrate(initial, duration, 8000, Rates);
        for (int i = 0; i < 3; ++i) Near(reference[i], finer[i], 1e-9);
        double previous = 0; var errors = new List<double>();
        foreach (ulong step in new ulong[] { 500_000, 250_000, 125_000, 62_500 })
        {
            var definition = new ModelDefinition
            {
                StepNanoseconds = step,
                Nodes = [NodeDefinition.Rotor(1, .1, 20, .5), NodeDefinition.CylinderGas(2, 100000, Reference) with { Gas = Gas() }, NodeDefinition.Thermal(3, 20, Reference)],
                Components = [ComponentDefinition.GasCylinder(10, 1, 2, MovingCylinderChecks.Geometry), ComponentDefinition.LiquidFilm(11, 2, 3, 0, Film(.00002))]
            };
            var s = CompiledModel.Compile(definition).CreateSimulation(); Step(s, 20_000_000); Ledgers(s);
            double error = Math.Max(Math.Abs(Value(s, 1, Field.Speed) - reference[1]) / 20, Math.Abs(Value(s, 2, Field.Temperature) - reference[2]) / Reference);
            if (previous > 0) Require(previous / error > 1.7 && previous / error < 2.3, $"Phase crank refinement: {previous:R} / {error:R}");
            previous = error; errors.Add(error);
        }
        Console.WriteLine("Phase crank normalized errors: " + string.Join(", ", errors.Select(v => v.ToString("R"))));
    }

    internal static ModelDefinition Supply(ulong tick = 50_000) => TankHeadspaceChecks.Model(tick) with
    {
        Components = TankHeadspaceChecks.Model(tick).Components.Select(c => c.FuelFilm is null ? c :
            c with { FuelFilm = c.FuelFilm with { PhaseEquilibrium = Phase(100000, 200, 600) } }).ToArray()
    };
    private static void Cavitation()
    {
        var definition = Supply(); var s = CompiledModel.Compile(definition).CreateSimulation();
        Step(s, 50_000_000); Ledgers(s);
        Require(Value(s, 22, Field.CavitationMargin) > 0 && Value(s, 12, Field.CavitationMargin) > 0);
        Near(Value(s, 22, Field.CavitationMargin), Value(s, 40, Field.Pressure) - Value(s, 22, Field.SaturationPressure), 1e-8);
        Near(Value(s, 21, Field.CavitationMargin), Value(s, 22, Field.CavitationMargin), 0);
        var before = Snapshot(s); Require(s.Step(250_000_000) == SimulationStatus.NumericalFailure && Snapshot(s) == before, "Cavitating batch was not rolled back.");
        var invalid = definition with { Nodes = definition.Nodes.Select(n => n.Id == 40 ? NodeDefinition.CylinderGas(40, 1000, 300) : n).ToArray() };
        Require(!CompiledModel.TryCompile(invalid, out _, out var inlet) && inlet!.Field == "liquid_rail_feed.inlet_pressure");
        invalid = definition with { Components = definition.Components.Select(c => c.LiquidFuelInjector is null ? c : c with { LiquidFuelInjector = c.LiquidFuelInjector with { InitialPressure = new(1000, Unit.Pascal) } }).ToArray() };
        Require(!CompiledModel.TryCompile(invalid, out _, out var rail) && rail!.Field == "liquid_fuel_injector.initial_pressure");
        var saturated = definition with { Components = definition.Components.Select(c => c.LiquidFuelTank is null ? c : c with { LiquidFuelTank = c.LiquidFuelTank with { InitialTemperature = new(400, Unit.Kelvin) } }).ToArray() };
        var onset = CompiledModel.Compile(saturated).CreateSimulation(); Near(Value(onset, 22, Field.CavitationMargin), 0, 2e-8);
        var atBoundary = Snapshot(onset); Require(onset.Step(saturated.StepNanoseconds) == SimulationStatus.NumericalFailure && Snapshot(onset) == atBoundary);
        var below = saturated with { Nodes = saturated.Nodes.Select(n => n.Id == 40 ? NodeDefinition.CylinderGas(40, 100000 - .0001, 300) : n).ToArray() };
        Require(!CompiledModel.TryCompile(below, out _, out _), "Roundoff allowance admitted a resolved pressure deficit.");
    }

    private static void Transactions()
    {
        var s = CompiledModel.Compile(Model(wall: 250, vent: 1e-6)).CreateSimulation(); Step(s, 20_000_000); var before = Snapshot(s);
        Require(Value(s, 10, Field.CondensedFuelMass) > 0);
        Require(s.Step(100_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.Step(100_000_000, [new(before.TimeNanoseconds + 50_000_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var replay = s.Fork(); Step(s, 100_000_000); for (int i = 0; i < 10; ++i) Step(replay, 10_000_000); Require(Snapshot(s) == Snapshot(replay));
        var branch = s.Fork(); var original = Snapshot(s); Require(branch.SubmitInputs([new(110, 0)]) == SimulationStatus.Ok); Step(branch, 20_000_000); Require(Snapshot(s) == original);
        var values = new Scalar[s.Model.OutputCount]; for (int i = 0; i < 20; ++i) { Step(s, 1_000_000); s.ReadSnapshot(values); }
        long allocated = GC.GetAllocatedBytesForCurrentThread(); for (int i = 0; i < 50; ++i) { Step(s, 1_000_000); s.ReadSnapshot(values); }
        Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "Phase stepping allocated."); Ledgers(s);
    }

    private static void Contracts()
    {
        var definition = Model(); var film = definition.Components[0];
        foreach (var phase in new[] { Phase(-1), Phase(Pressure, 400, 500), Phase(Pressure, 200, 1000), Phase() with { ReferencePressure = new(1, Unit.Kelvin) } })
        {
            var invalid = definition with { Components = [film with { FuelFilm = film.FuelFilm! with { PhaseEquilibrium = phase } }, ..definition.Components.Skip(1)] };
            Require(!CompiledModel.TryCompile(invalid, out _, out _));
        }
        Require(!CompiledModel.TryCompile(definition with { Components = [..definition.Components, ComponentDefinition.LiquidFilm(15, 2, 3, 1, FuelFilmChecks.Definition())] }, out _, out var duplicate) && duplicate!.Code == DiagnosticCode.Connection);
        var compiled = CompiledModel.Compile(definition); definition.Components[0] = film with { FuelFilm = film.FuelFilm! with { PhaseEquilibrium = Phase(20000) } };
        Require(compiled.Fingerprint != CompiledModel.Compile(definition).Fingerprint);
        var a = compiled.CreateSimulation(); var b = CompiledModel.Compile(Model()).CreateSimulation(); Step(a, 20_000_000); Step(b, 20_000_000); Require(Snapshot(a) == Snapshot(b));
    }
}
