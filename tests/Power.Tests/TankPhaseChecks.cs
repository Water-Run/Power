// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class TankPhaseChecks
{
    private const double R = 287, Cv = R / .4, Cl = 2000, Tr = 400, Pref = 100000, Latent = 300000, Density = 750;
    private const double Offset = (Cv - Cl) * Tr - Latent;
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("tank phase / finite liquid volume chemical potentials and caloric derivative", Chemical),
        ("tank phase / independent entropy maximum / closed U,V flash and density bounds", Flash),
        ("tank phase / liquid pickup and reverse return / mass volume energy and phase ledgers", Graph),
        ("tank phase / simultaneous relief caloric return / complete conserving inventories", Relief),
        ("tank phase / independent shaft rail source equilibrium ODE / first-order refinement", PumpReference),
        ("tank phase / independent vent wall and phase-flow ODE / first-order refinement", VentReference),
        ("tank phase / complete rollback cancellation branches and zero allocations", Transactions),
        ("tank phase / dry source liquid-only pickup and initial rail pressure guard", Dry),
        ("tank phase / explicit material composition initial equilibrium and immutable ownership", Contracts)
    ];
    private static FuelPhaseEquilibrium Material() => new(new(Cl, Cv, Tr, Latent, 0), R, Pref, 200, 600);
    private static FiniteTankFuelEquilibrium Law(double volume = 2e-5, double density = Density) => new(Material(), volume, density);
    private static double P0(double t) => Pref * Math.Pow(t / Tr, (Cv + R - Cl) / R) * Math.Exp(Offset / R * (1 / t - 1 / Tr));
    private static void Step(Simulation s, ulong time) => Require(s.Step(time) == SimulationStatus.Ok, "Tank phase step failed.");
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private static void Balance(Simulation s)
    {
        Near(Value(s, 0, Field.EnergyResidual), 0, 2e-7); Near(Value(s, 0, Field.MassResidual), 0, 1e-14);
        Near(Value(s, 0, Field.FuelResidual), 0, 1e-14); Near(Value(s, 0, Field.HydraulicVolumeResidual), 0, 1e-16);
        Near(Value(s, 0, Field.HydraulicWork), 0, 0);
    }
    internal static ModelDefinition Model(ulong tick = 50_000, double speed = 100, double vent = 0, double heat = 0, bool relief = false)
    {
        var original = TankHeadspaceChecks.Model(tick, speed, relief, ventArea: vent);
        double initialTemperature = original.Components.Single(c => c.LiquidFuelTank is not null).LiquidFuelTank!.InitialTemperature.Value;
        double fraction = P0(initialTemperature) * Math.Exp(100000 / (Density * R * initialTemperature)) / 100000;
        var nodes = original.Nodes.Select(n => n.Id != 40 ? n : NodeDefinition.CylinderGas(40, 100000, initialTemperature) with
        {
            Gas = new() { GasConstant = new(R, Unit.JoulePerKilogramKelvin), Gamma = 1.4,
                Premixed = new() { LowerHeatingValue = new(44e6, Unit.JoulePerKilogram), StoichiometricAirFuelRatio = 14.7, InitialFractions = new(fraction, 1 - fraction) } }
        }).ToArray();
        var components = original.Components.Select(c => c.FuelFilm is not null ? c with { FuelFilm = c.FuelFilm with { PhaseEquilibrium = FuelPhaseEquilibriumChecks.Phase(Pref, 200, 600) } } :
            c.LiquidFuelTank is not null ? c with { LiquidFuelTank = c.LiquidFuelTank with { Headspace = c.LiquidFuelTank.Headspace! with { PhaseEquilibrium = true } } } :
            c.Id == 41 ? c with { DischargeCoefficient = .7, ReservoirPressure = new(1000, Unit.Pascal), ReservoirFractions = new(0, 1) } : c).ToArray();
        if (heat > 0) { nodes = [..nodes, NodeDefinition.Thermal(60, .2, 360)]; components = [..components, ComponentDefinition.GasHeatLink(61, 40, 60, heat)]; }
        return original with { Nodes = nodes, Components = components };
    }

    private static void Chemical()
    {
        var law = Law();
        foreach (double t in new[] { 220d, 300d, 400d, 550d })
        {
            Require(law.TryPoint(t, .006, .0001, out var point) && point.LiquidMassKilograms > 0);
            Near(point.GasVolumeCubicMeters + point.LiquidMassKilograms / Density, 2e-5, 1e-20);
            double chemicalResidual = R * t * Math.Log(point.VaporPressurePascals / P0(t)) - point.PressurePascals / Density;
            Near(chemicalResidual, 0, 2e-8);
            Require(law.TrySaturationPressure(t, point.PressurePascals, out double saturation)); Near(saturation, point.VaporPressurePascals, 1e-7);
            Require(law.TryPoint(t + .001, .006, .0001, out var high)); Require(law.TryPoint(t - .001, .006, .0001, out var low));
            Near((high.ThermalEnergyJoules - low.ThermalEnergyJoules) / .002, point.HeatCapacityJoulesPerKelvin, 1e-7);
        }
        var limit = Law(.01, 1e12); Require(limit.TryPoint(350, .006, .007, out var finite));
        Require(Material().TryPoint(350, .006, .007, .01, out var negligible));
        Near(finite.VaporMassKilograms, negligible.VaporMassKilograms, 1e-13);
        Require(!law.TryPoint(350, .014, .001, out _) && !law.TrySaturationPressure(350, Density * R * 350, out _));
    }

    private static double Entropy(double liquid, double liquidTemperature, double vapor, double carrier, double gasTemperature, double capacity)
    {
        double volume = capacity - liquid / Density;
        if (volume <= 0 || liquidTemperature <= 0 || gasTemperature <= 0) return double.NegativeInfinity;
        double entropy = liquid * Cl * Math.Log(liquidTemperature / Tr);
        if (vapor > 0) entropy += vapor * ((Cv + R) * Math.Log(gasTemperature / Tr) - R * Math.Log(vapor * R * gasTemperature / volume / Pref) + (Latent + R * Tr) / Tr);
        if (carrier > 0) entropy += carrier * (Cv * Math.Log(gasTemperature / Tr) + R * Math.Log(volume * Pref / (carrier * R * Tr)));
        return entropy;
    }
    private static (double Vapor, double Temperature) EntropyMaximum(double energy, double fuel, double carrier, double volume)
    {
        double T(double vapor) => (energy - (fuel - vapor) * Offset) / ((carrier + vapor) * Cv + (fuel - vapor) * Cl);
        double S(double vapor)
        {
            double t = T(vapor); return t < 200 || t > 600 ? double.NegativeInfinity : Entropy(fuel - vapor, t, vapor, carrier, t, volume);
        }
        double lo = 0, hi = fuel, ratio = (Math.Sqrt(5) - 1) / 2, a = hi - ratio * (hi - lo), b = lo + ratio * (hi - lo);
        for (int i = 0; i < 110; ++i)
            if (S(a) < S(b)) { lo = a; a = b; b = lo + ratio * (hi - lo); }
            else { hi = b; b = a; a = hi - ratio * (hi - lo); }
        double result = (lo + hi) / 2; if (S(fuel) >= S(result)) result = fuel;
        return (result, T(result));
    }
    private static void Flash()
    {
        const double capacity = 2e-5;
        foreach (var (liquid, vapor, carrier, tl, tg) in new[]
        {
            (.006, 1e-7, .0001, 350d, 350d), (.006, .0001, .0001, 350d, 350d), (0d, .0001, .0001, 300d, 300d),
            (.003, .0001, 0d, 300d, 500d), (0d, 1e-9, .0001, 500d, 500d)
        })
        {
            var law = Law(capacity); var initial = Material().CreateLiquidState(liquid, tl); double gasEnergy = (carrier + vapor) * Cv * tg;
            Require(law.TryFlash(initial, vapor, carrier, gasEnergy, out var flash));
            var reference = EntropyMaximum(initial.ThermalEnergyJoules + gasEnergy, liquid + vapor, carrier, capacity);
            Near(flash.VaporMassKilograms, reference.Vapor, 3e-11); Near(flash.TemperatureKelvin, reference.Temperature, 2e-6);
            Near(flash.VaporMassKilograms + flash.Liquid.LiquidMassKilograms, liquid + vapor, 1e-18);
            Near(flash.GasEnergyJoules + flash.Liquid.ThermalEnergyJoules, gasEnergy + initial.ThermalEnergyJoules, 1e-10);
            double actualLiquidTemperature = flash.Liquid.LiquidMassKilograms == 0 ? flash.TemperatureKelvin : Material().Caloric.Temperature(flash.Liquid);
            double actualGasTemperature = flash.GasEnergyJoules / (carrier + flash.VaporMassKilograms) / Cv;
            Require(Entropy(flash.Liquid.LiquidMassKilograms, actualLiquidTemperature, flash.VaporMassKilograms, carrier, actualGasTemperature, capacity) >=
                Entropy(liquid, tl, vapor, carrier, tg, capacity) - 1e-10, "Finite-volume flash decreased entropy.");
        }
        Require(!Law().TryFlash(new(.014, .014 * (Cl * 300 + Offset)), .001, .001, 1000, out _));
    }

    private static void Graph()
    {
        foreach (double speed in new[] { 100d, -100d })
        {
            var s = CompiledModel.Compile(Model(speed: speed)).CreateSimulation();
            double initialLiquid = Value(s, 22, Field.Mass), initialVapor = Value(s, 40, Field.FuelMass), initialVolume = Value(s, 22, Field.Volume) + Value(s, 40, Field.Volume);
            double initialEntropy = SourceAndRailEntropy(s);
            Step(s, 20_000_000); Balance(s);
            double net = Value(s, 22, Field.EvaporatedFuelMass) - Value(s, 22, Field.CondensedFuelMass), supplied = Value(s, 21, Field.TotalFuelDelivered);
            Near(Value(s, 22, Field.Mass) + Value(s, 40, Field.FuelMass) + supplied, initialLiquid + initialVapor, 1e-14);
            Near(initialLiquid - Value(s, 22, Field.Mass), supplied + net, 1e-14);
            Near(Value(s, 40, Field.FuelMass) - initialVapor, net, 1e-14);
            Near(Value(s, 22, Field.Volume) + Value(s, 40, Field.Volume), initialVolume, 1e-21);
            Near(Value(s, 22, Field.Temperature), Value(s, 40, Field.Temperature), 1e-8);
            Near(Value(s, 22, Field.SaturationPressure), Value(s, 22, Field.VaporPartialPressure), 1e-5);
            Require(speed > 0 ? net > 0 : net < 0);
            Require(SourceAndRailEntropy(s) >= initialEntropy - 1e-10, "Adiabatic liquid pickup/return decreased total material entropy.");
        }
    }
    private static double SourceAndRailEntropy(Simulation s)
    {
        double liquid=Value(s,22,Field.Mass),vapor=Value(s,40,Field.FuelMass),carrier=Value(s,40,Field.Mass)-vapor;
        double capacity=Value(s,22,Field.Volume)+Value(s,40,Field.Volume);
        return Entropy(liquid,Value(s,22,Field.Temperature),vapor,carrier,Value(s,40,Field.Temperature),capacity) +
            Value(s,12,Field.Mass)*Cl*Math.Log(Value(s,12,Field.Temperature)/Tr);
    }
    private static void Relief()
    {
        foreach (double fraction in new[] { 0d, .5, 1d })
        {
            var definition = Model(speed: 0, relief: true);
            definition = definition with { Components = definition.Components.Select(c => c.LiquidRailReturn is null ? c : c with { LiquidRailReturn = c.LiquidRailReturn with { FluidHeatFraction = new(fraction, Unit.Fraction) } }).ToArray() };
            var s = CompiledModel.Compile(definition).CreateSimulation(); double initial = Value(s, 22, Field.Mass) + Value(s, 40, Field.FuelMass);
            Step(s, 20_000_000); Balance(s); Require(Value(s, 31, Field.TotalFuelDelivered) > 0);
            Near(Value(s, 22, Field.Mass) + Value(s, 40, Field.FuelMass) + Value(s, 21, Field.TotalFuelDelivered) - Value(s, 31, Field.TotalFuelDelivered), initial, 1e-14);
        }
    }

    // Independent bisection of chemical potentials, followed by simultaneous RK4 of
    // shaft, rail, total source inventories, equilibrium temperature and finite wall.
    private static (double Vapor, double Volume, double Pressure, double Capacity, double FuelEnergyDerivative, double CarrierEnergyDerivative, double VaporT, double VaporFuel, double VaporCarrier) ReferencePoint(double t, double fuel, double carrier, double capacity)
    {
        double F(double vapor)
        {
            double volume = capacity - (fuel - vapor) / Density;
            return vapor == 0 ? double.NegativeInfinity : Math.Log(vapor * R * t / volume / P0(t)) - (carrier + vapor) / (Density * volume);
        }
        double lo = 0, hi = fuel;
        if (F(fuel) > 0)
            for (int i = 0; i < 75; ++i) { double mid = (lo + hi) / 2; if (F(mid) > 0) hi = mid; else lo = mid; }
        else lo = hi;
        double vapor = (lo + hi) / 2, liquid = fuel - vapor, volumeAt = capacity - liquid / Density, pressure = (carrier + vapor) * R * t / volumeAt;
        double latent = (Cv - Cl) * t - Offset, vt = 0, vf = 0, vc = 0;
        if (liquid > 0)
        {
            double b = Density * capacity - fuel, z = b + vapor, denominator = b * b + carrier * vapor;
            vt = latent / (R * t * t) * vapor * z * z / denominator;
            vf = -vapor * (b - carrier) / denominator; vc = vapor * z / denominator;
        }
        return (vapor, volumeAt, pressure, (carrier + vapor) * Cv + liquid * Cl + latent * vt,
            Cl * t + Offset + latent * vf, Cv * t + latent * vc, vt, vf, vc);
    }
    private static void PumpReference() => Reference(0, 0);
    private static void VentReference() => Reference(1e-10, .02);
    private static void Reference(double ventArea, double conductance)
    {
        const double capacity = .0001 / Density + 1e-8, duration = .02;
        var baseline = CompiledModel.Compile(Model(vent: ventArea, heat: conductance)).CreateSimulation();
        double fuel = Value(baseline, 22, Field.Mass) + Value(baseline, 40, Field.FuelMass), carrier = Value(baseline, 40, Field.Mass) - Value(baseline, 40, Field.FuelMass);
        double[] initial = [100, 800000, fuel, carrier, Value(baseline, 22, Field.Temperature), .0005 * (Cl * 300 + Offset), 360, 0, 0];
        void Rates(double[] state, double[] rate)
        {
            var point = ReferencePoint(state[4], state[2], state[3], capacity);
            double railMass = Density * ((.0005 / Density - 5e-13 * 800000) + 5e-13 * state[1]);
            double q = 1e-9 * state[0], pump = Density * q, ul = pump >= 0 ? Cl * state[4] + Offset : state[5] / railMass;
            double heat = conductance * (state[6] - state[4]), flow = 0;
            if (ventArea > 0)
            {
                Require(point.Pressure > 2000);
                flow = .7 * ventArea * point.Pressure / Math.Sqrt(R * state[4]) * Math.Sqrt(1.4) * Math.Pow(2 / 2.4, 3);
            }
            double vaporOut = flow * point.Vapor / (state[3] + point.Vapor), carrierOut = flow - vaporOut;
            rate[0] = -1e-9 * (state[1] - point.Pressure) / .02;
            rate[1] = q / 5e-13; rate[2] = -pump - vaporOut; rate[3] = -carrierOut;
            double energy = heat - pump * ul - point.Pressure * q - flow * (Cv + R) * state[4];
            rate[4] = (energy - point.FuelEnergyDerivative * rate[2] - point.CarrierEnergyDerivative * rate[3]) / point.Capacity;
            rate[5] = pump * ul; rate[6] = -heat / .2;
            double phase = point.VaporT * rate[4] + point.VaporFuel * rate[2] + point.VaporCarrier * rate[3] + vaporOut;
            rate[7] = Math.Max(0, phase); rate[8] = Math.Max(0, -phase);
        }
        double[] Integrate(int count)
        {
            var state = initial.ToArray(); var stage = new double[9]; var a = new double[9]; var b = new double[9]; var c = new double[9]; var d = new double[9]; double h = duration / count;
            for (int n = 0; n < count; ++n)
            {
                Rates(state, a); for (int i = 0; i < 9; ++i) stage[i] = state[i] + h / 2 * a[i]; Rates(stage, b);
                for (int i = 0; i < 9; ++i) stage[i] = state[i] + h / 2 * b[i]; Rates(stage, c);
                for (int i = 0; i < 9; ++i) stage[i] = state[i] + h * c[i]; Rates(stage, d);
                for (int i = 0; i < 9; ++i) state[i] += h / 6 * (a[i] + 2 * b[i] + 2 * c[i] + d[i]);
            }
            return state;
        }
        var reference = Integrate(2000); var finer = Integrate(4000);
        for (int i = 0; i < 9; ++i) Near(reference[i], finer[i], i == 1 ? 1e-6 : 1e-8);
        var errors = new List<double>(); double previous = 0;
        foreach (ulong tick in new ulong[] { 400_000, 200_000, 100_000, 50_000 })
        {
            var s = CompiledModel.Compile(Model(tick, vent: ventArea, heat: conductance)).CreateSimulation(); Step(s, 20_000_000); Balance(s);
            double error = Math.Max(Math.Abs(Value(s, 22, Field.Temperature) - reference[4]) / 100, Math.Abs(Value(s, 40, Field.FuelMass) + Value(s, 22, Field.Mass) - reference[2]) / fuel);
            error = Math.Max(error, Math.Abs(Value(s, 22, Field.EvaporatedFuelMass) - reference[7]) / fuel);
            error = Math.Max(error, Math.Abs(Value(s, 22, Field.CondensedFuelMass) - reference[8]) / fuel);
            if (previous > 0) Require(previous / error > 1.65 && previous / error < 2.4, $"Tank phase refinement: {previous:R} / {error:R}");
            errors.Add(error); previous = error;
        }
        Console.WriteLine($"Finite tank phase ODE errors (vent {ventArea:R}, heat {conductance:R}): " + string.Join(", ", errors.Select(e => e.ToString("R"))));
    }

    private static void Transactions()
    {
        var s = CompiledModel.Compile(Model(vent: 1e-10)).CreateSimulation(); Step(s, 10_000_000); var before = Snapshot(s);
        Require(s.Step(10_000_000, cancellationToken: new CancellationToken(true)) == SimulationStatus.Cancelled && Snapshot(s) == before);
        Require(s.Step(10_000_000, [new(before.TimeNanoseconds + 5_000_000, 100, double.MaxValue)]) == SimulationStatus.NumericalFailure && Snapshot(s) == before);
        var replay = s.Fork(); Step(s, 10_000_000); for (int i=0;i<10;++i) Step(replay, 1_000_000); Require(Snapshot(s)==Snapshot(replay));
        var fork = s.Fork(); before=Snapshot(s); Require(fork.SubmitInputs([new(110,0)])==SimulationStatus.Ok); Step(fork,10_000_000); Require(Snapshot(s)==before);
        var values=new Scalar[s.Model.OutputCount];for(int i=0;i<10;++i){Step(s,50_000);s.ReadSnapshot(values);}long allocated=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<40;++i){Step(s,50_000);s.ReadSnapshot(values);}Require(GC.GetAllocatedBytesForCurrentThread()==allocated,"Tank phase stepping allocated.");
    }
    private static void Contracts()
    {
        var definition=Model();
        var wrong=definition with{Nodes=definition.Nodes.Select(n=>n.Id==40? n with{Gas=n.Gas! with{Gamma=1.35}}:n).ToArray()};
        Require(!CompiledModel.TryCompile(wrong,out _,out var gas)&&gas!.Field=="liquid_fuel_tank.headspace.phase_equilibrium");
        wrong=definition with{Nodes=definition.Nodes.Select(n=>n.Id==40? n with{Gas=n.Gas! with{Premixed=n.Gas!.Premixed! with{InitialFractions=new(0,1)}}}:n).ToArray()};
        Require(!CompiledModel.TryCompile(wrong,out _,out var fraction)&&fraction!.Field=="liquid_fuel_tank.headspace.initial_fractions");
        wrong=definition with{Components=definition.Components.Select(c=>c.FuelFilm is null?c:c with{FuelFilm=c.FuelFilm with{PhaseEquilibrium=null}}).ToArray()};
        Require(!CompiledModel.TryCompile(wrong,out _,out _));
        var compiled=CompiledModel.Compile(definition);int index=Array.FindIndex(definition.Components,c=>c.LiquidFuelTank is not null);
        definition.Components[index]=definition.Components[index] with{LiquidFuelTank=definition.Components[index].LiquidFuelTank! with{Headspace=definition.Components[index].LiquidFuelTank!.Headspace! with{PhaseEquilibrium=false}}};
        Require(compiled.Fingerprint!=CompiledModel.Compile(definition).Fingerprint);
        var a=compiled.CreateSimulation();var b=CompiledModel.Compile(Model()).CreateSimulation();Step(a,1_000_000);Step(b,1_000_000);Require(Snapshot(a)==Snapshot(b));
    }
    private static void Dry()
    {
        var definition=Model();
        definition=definition with { Components=definition.Components.Select(c=>c.LiquidFuelTank is null?c:c with{LiquidFuelTank=c.LiquidFuelTank with{InitialMass=new(0,Unit.Kilogram)}}).ToArray() };
        var dry=CompiledModel.Compile(definition).CreateSimulation();double initialRail=Value(dry,12,Field.Mass);
        Step(dry,10_000_000);Balance(dry);
        Near(Value(dry,21,Field.TotalFuelDelivered),0,1e-15);Near(Value(dry,12,Field.Mass),initialRail,1e-15);
        Require(Value(dry,40,Field.FuelMass)>0&&Value(dry,22,Field.Mass)<1e-15,"Dry pickup consumed source vapor or manufactured a liquid inventory.");
        var bad=definition with
        {
            Nodes=definition.Nodes.Select(n=>n.Id==5?NodeDefinition.Hydraulic(5,5e-13,100000):n).ToArray(),
            Components=definition.Components.Select(c=>c.LiquidFuelInjector is null?c:c with{LiquidFuelInjector=c.LiquidFuelInjector with{InitialPressure=new(100000,Unit.Pascal),SupplyTemperature=new(400,Unit.Kelvin)}}).ToArray()
        };
        Require(!CompiledModel.TryCompile(bad,out _,out var error)&&error!.Field=="liquid_fuel_injector.initial_pressure","A dry source bypassed the paired rail phase boundary.");
    }
}
