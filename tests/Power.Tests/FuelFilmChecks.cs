// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class FuelFilmChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>
    [
        ("fuel film / analytic finite-bath heating / signed phase reference / energy",Heating),
        ("fuel film / saturation plateau / dryout / semigroup / latent budget",Phase),
        ("fuel film / cold wall / insufficient bath / zero heat / invalid properties",Boundaries),
        ("liquid film graph / actual vapor reactant / closed mass and chemical energy",Graph),
        ("film evaporation and delayed reaction / no burning liquid / thermal energy",Burn),
        ("shared film wall / independent coupled ODE / second-order smooth refinement",SharedWall),
        ("film vapor transport / independent choked-flow ODE / second-order split refinement",VaporTransport),
        ("film and gas wall heat / independent coupled ODE / first-order explicit wall refinement",WallExchange),
        ("film transactions / thermal forks / cancelled and late rejected batches / zero allocation",Transactions),
        ("film contracts / tracked receiver / finite thermal wall / units / capacity / immutability",Contracts)
    ];
    private static void Ok(SimulationStatus s){if(s!=SimulationStatus.Ok)throw new InvalidOperationException(s.ToString());}
    private static SnapshotInfo Snapshot(Simulation s)=>s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    internal static FuelFilmDefinition Definition()=>new(){InitialMass=new(20e-6,Unit.Kilogram),InitialTemperature=new(300,Unit.Kelvin),LiquidSpecificHeat=new(2000,Unit.JoulePerKilogramKelvin),SaturationTemperature=new(400,Unit.Kelvin),LatentInternalEnergy=new(300000,Unit.JoulePerKilogram)};
    internal static ModelDefinition Model(ulong step=100_000)=>new()
    {
        StepNanoseconds=step,
        Nodes=[NodeDefinition.Rotor(1,1,Math.PI/2),NodeDefinition.GasVolume(2,.01,1e5,300) with{Gas=new(){GasConstant=new(287,Unit.JoulePerKilogramKelvin),Gamma=1.4,Premixed=new(){LowerHeatingValue=new(44e6,Unit.JoulePerKilogram),StoichiometricAirFuelRatio=14.7,InitialFractions=new(0,1)}}},NodeDefinition.Thermal(3,.2,500)],
        Components=[ComponentDefinition.LiquidFilm(10,2,3,.1,Definition()),ComponentDefinition.Torque(11,1,100,0)]
    };
    private static void Heating()
    {
        var law=new EquilibriumFuelFilm(2000,1000,400,300000,1);var state=law.CreateState(.01,300);Require(state.ThermalEnergyJoules<0);
        Require(law.TryAdvance(state,500,100,1,out var next));double heat=(20.0*100/120)*200*(1-Math.Exp(-.06));
        Near(next.HeatFromWallJoules,heat,1e-10);Near(next.LiquidTemperatureKelvin,300+heat/20,1e-12);Near(next.WallTemperatureKelvin,500-heat/100,1e-12);Near(next.EvaporatedMassKilograms,0,0);
        Near(next.Liquid.ThermalEnergyJoules+next.VaporEnergyJoules-state.ThermalEnergyJoules,next.HeatFromWallJoules,1e-10);
    }
    private static void Phase()
    {
        var law=new EquilibriumFuelFilm(2000,1000,400,300000,1);var initial=law.CreateState(.01,300);double start=Math.Log(2.5)/.06;
        Require(law.TryAdvance(initial,500,100,30,out var phase));double evaporated=8000*(1-Math.Exp(-(30-start)/100))/300000;
        Near(phase.EvaporatedMassKilograms,evaporated,1e-15);Near(phase.LiquidTemperatureKelvin,400,0);
        Require(law.TryAdvance(initial,500,100,80,out var dry));Near(dry.Liquid.LiquidMassKilograms,0,0);Near(dry.Liquid.ThermalEnergyJoules,0,0);Near(dry.VaporEnergyJoules,4000,1e-10);Near(dry.HeatFromWallJoules,5000,1e-10);Near(dry.WallTemperatureKelvin,450,1e-12);
        var state=initial;double wall=500,vapor=0,heat=0;
        for(int i=0;i<80;++i){Require(law.TryAdvance(state,wall,100,1,out var step));state=step.Liquid;wall=step.WallTemperatureKelvin;vapor+=step.EvaporatedMassKilograms;heat+=step.HeatFromWallJoules;}
        Near(state.LiquidMassKilograms,0,0);Near(wall,dry.WallTemperatureKelvin,1e-10);Near(vapor,.01,1e-14);Near(heat,5000,1e-8);
    }
    private static void Boundaries()
    {
        var law=new EquilibriumFuelFilm(2000,1000,400,300000,1);var state=law.CreateState(.01,300);
        Require(law.TryAdvance(state,450,10,1000,out var limited));Near(limited.EvaporatedMassKilograms,0,0);Near(limited.LiquidTemperatureKelvin,350,1e-10);
        Require(law.TryAdvance(state,200,100,1,out var cool));Require(cool.HeatFromWallJoules<0&&cool.LiquidTemperatureKelvin<300);
        var zero=new EquilibriumFuelFilm(2000,1000,400,300000,0);Require(zero.TryAdvance(state,500,100,100,out var sealedFilm));Require(sealedFilm.Liquid==state);
        Require(!law.TryAdvance(new(.01,double.NaN),500,100,1,out _));Throws<ArgumentException>(()=>law.CreateState(.01,401));Throws<ArgumentException>(()=>new EquilibriumFuelFilm(1,1,400,0,1));
    }
    private static void Ledgers(Simulation s)
    {Near(Value(s,0,Field.EnergyResidual),0,1e-8);Near(Value(s,0,Field.MassResidual),0,1e-16);Near(Value(s,0,Field.FuelResidual),0,1e-16);Near(Value(s,0,Field.FreshAirResidual),0,1e-16);}
    private static void Graph()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();double initial=Value(s,0,Field.ChemicalEnergy);Ok(s.Step(100_000_000));
        Near(Value(s,2,Field.FuelMass),0,0);Require(Value(s,10,Field.Temperature)>300);Ledgers(s);
        Ok(s.Step(1_900_000_000));Near(Value(s,10,Field.Mass),0,0);Near(Value(s,10,Field.EvaporatedFuelMass),20e-6,1e-16);Near(Value(s,2,Field.FuelMass),20e-6,1e-16);
        Near(Value(s,0,Field.ChemicalEnergy),initial,1e-8);Near(Value(s,10,Field.InternalEnergy),0,0);Near(Value(s,3,Field.Temperature),450,1e-8);Ledgers(s);
    }
    private static void Burn()
    {
        var definition=Model() with{Components=[..Model().Components,ComponentDefinition.PremixedCombustion(12,1,2,new(){CycleAngle=new(4*Math.PI,Unit.Radian),StartAngle=new(2.2,Unit.Radian),DurationAngle=new(.5,Unit.Radian),ShapeExponent=3,BurnCoefficient=6.9})]};
        var s=CompiledModel.Compile(definition).CreateSimulation();Ok(s.Step(1_000_000_000));Near(Value(s,12,Field.FuelBurned),0,0);Require(Value(s,10,Field.Mass)>0);Ledgers(s);
        Ok(s.Step(1_000_000_000));double burned=20e-6*(1-Math.Exp(-6.9));Near(Value(s,12,Field.FuelBurned),burned,1e-15);Near(Value(s,12,Field.HeatReleased),44e6*burned,1e-7);Ledgers(s);
    }
    private static void SharedWall() => CoupledReference(0, 3.5, 4.5);
    private static void VaporTransport() => CoupledReference(0, 3.4, 4.6, 4e-5);
    private static void WallExchange() => CoupledReference(.3, 1.7, 2.3);

    private static void CoupledReference(double gasConductance, double minimumRatio, double maximumRatio, double outletArea = 0)
    {
        // Two saturated films share a cooling wall and one tracked, fixed-volume receiver.
        // Integrate the simultaneous physical rates without using the film law or graph solver.
        const double cv = 287 / .4, latent = 300_000, wallCapacity = 20;
        double gasMass = 1e5 * .01 / (287 * 300);
        double[] initial = [.01, .012, 500, gasMass, gasMass * cv * 300, 0, gasMass];
        void Rates(double[] state, double[] rates)
        {
            double firstHeat = state[2] - 400, secondHeat = .7 * (state[2] - 420);
            double gasHeat = gasConductance * (state[2] - state[4] / (state[3] * cv));
            rates[0] = -firstHeat / latent;
            rates[1] = -secondHeat / latent;
            rates[2] = -(firstHeat + secondHeat + gasHeat) / wallCapacity;
            rates[3] = -(rates[0] + rates[1]);
            rates[4] = -cv * (400 * rates[0] + 420 * rates[1]) + gasHeat;
            rates[5] = rates[3];
            rates[6] = 0;
            if (outletArea != 0)
            {
                double temperature = state[4] / (state[3] * cv), pressure = .4 * state[4] / .01;
                Require(pressure > 1000 / Math.Pow(2 / 2.4, 1.4 / .4), "Reference left the choked-flow regime.");
                double flow = .9 * outletArea * pressure / Math.Sqrt(287 * temperature)
                    * Math.Sqrt(1.4) * Math.Pow(2 / 2.4, 2.4 / .8);
                rates[3] -= flow;
                rates[4] -= flow * 1.4 * cv * temperature;
                rates[5] -= flow * state[5] / state[3];
                rates[6] -= flow * state[6] / state[3];
            }
        }
        double[] Reference(int steps)
        {
            var state = initial.ToArray();
            var stage = new double[7];
            var a = new double[7]; var b = new double[7];
            var c = new double[7]; var d = new double[7];
            double dt = 2.0 / steps;
            for (int step = 0; step < steps; ++step)
            {
                Rates(state, a);
                for (int i = 0; i < 7; ++i) stage[i] = state[i] + dt / 2 * a[i];
                Rates(stage, b);
                for (int i = 0; i < 7; ++i) stage[i] = state[i] + dt / 2 * b[i];
                Rates(stage, c);
                for (int i = 0; i < 7; ++i) stage[i] = state[i] + dt * c[i];
                Rates(stage, d);
                for (int i = 0; i < 7; ++i) state[i] += dt / 6 * (a[i] + 2 * b[i] + 2 * c[i] + d[i]);
            }
            return state;
        }
        var reference = Reference(20_000);
        var refinedReference = Reference(40_000);
        double[] scale = [.01, .012, 100, gasMass, initial[4], .001, gasMass];
        for (int i = 0; i < 7; ++i) Near(reference[i], refinedReference[i], 1e-10 * scale[i]);

        double previousError = 0;
        var errors = new List<double>();
        foreach (ulong tick in new ulong[] { 40_000_000, 20_000_000, 10_000_000, 5_000_000 })
        {
            var film = Definition() with { InitialMass = new(.01, Unit.Kilogram), InitialTemperature = new(400, Unit.Kelvin) };
            var second = film with { InitialMass = new(.012, Unit.Kilogram), InitialTemperature = new(420, Unit.Kelvin), SaturationTemperature = new(420, Unit.Kelvin) };
            var components = new List<ComponentDefinition>
            {
                ComponentDefinition.LiquidFilm(10, 2, 3, 1, film),
                ComponentDefinition.LiquidFilm(12, 2, 3, .7, second)
            };
            if (gasConductance != 0) components.Add(new()
            {
                Id = 13, Kind = ComponentKind.GasHeatLink, NodeA = 2, NodeB = 3,
                Conductance = new(gasConductance, Unit.WattPerKelvin)
            });
            if (outletArea != 0) components.Add(ComponentDefinition.GasReservoir(14, 2, outletArea, 1000, 300, .9)
                with { ReservoirFractions = new(0, 1) });
            var definition = new ModelDefinition
            {
                StepNanoseconds = tick,
                Nodes = [Model().Nodes[1], NodeDefinition.Thermal(3, wallCapacity, 500)],
                Components = components.ToArray()
            };
            var simulation = CompiledModel.Compile(definition).CreateSimulation();
            Ok(simulation.Step(2_000_000_000));
            double[] actual = [Value(simulation, 10, Field.Mass), Value(simulation, 12, Field.Mass),
                Value(simulation, 3, Field.Temperature), Value(simulation, 2, Field.Mass), Value(simulation, 2, Field.InternalEnergy),
                Value(simulation, 2, Field.FuelMass), Value(simulation, 2, Field.FreshAirMass)];
            double error = 0;
            for (int i = 0; i < 7; ++i) error = Math.Max(error, Math.Abs(actual[i] - reference[i]) / scale[i]);
            Require(actual[0] > 0 && actual[1] > 0 && actual[2] > 420, "Reference left the smooth saturation regime.");
            Ledgers(simulation);
            Near(Value(simulation, 10, Field.FilmWallHeat), latent * (initial[0] - actual[0]), 1e-9);
            Near(Value(simulation, 12, Field.FilmWallHeat), latent * (initial[1] - actual[1]), 1e-9);
            if (previousError != 0)
            {
                double ratio = previousError / error;
                Require(ratio > minimumRatio && ratio < maximumRatio, $"Unexpected coupled film refinement ratio {ratio:R}.");
            }
            previousError = error;
            errors.Add(error);
        }
        Console.WriteLine($"Film coupled reference (gas conductance {gasConductance:R} W/K, outlet {outletArea:R} m2): normalized errors {string.Join(", ", errors.Select(e => e.ToString("R")))}");
    }
    private static void Transactions()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();Ok(s.Step(100_000_000));var before=Snapshot(s);
        Require(s.Step(100_000_000,cancellationToken:new CancellationToken(true))==SimulationStatus.Cancelled&&Snapshot(s)==before);
        var fork=s.Fork();Ok(s.Step(100_000_000));for(int i=0;i<10;++i)Ok(fork.Step(10_000_000));Require(Snapshot(s)==Snapshot(fork));
        before=Snapshot(s);Require(s.Step(100_000_000,[new(before.TimeNanoseconds+50_000_000,100,double.MaxValue)])==SimulationStatus.NumericalFailure&&Snapshot(s)==before);
        var values=new Scalar[s.Model.OutputCount];for(int i=0;i<10;++i){Ok(s.Step(1_000_000));s.ReadSnapshot(values);}long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;++i){Ok(s.Step(1_000_000));s.ReadSnapshot(values);}Require(GC.GetAllocatedBytesForCurrentThread()==allocated,"Film stepping or readback allocated.");
    }
    private static void Contracts()
    {
        var definition=Model();var film=definition.Components[0];
        foreach(var invalid in new[]{film with{NodeA=3},film with{NodeB=2},film with{InputChannel=101},film with{FuelFilm=Definition() with{InitialTemperature=new(401,Unit.Kelvin)}},film with{FuelFilm=Definition() with{LatentInternalEnergy=new(1,Unit.Joule)}}})Require(!CompiledModel.TryCompile(definition with{Components=[invalid,definition.Components[1]]},out _,out _));
        var compiled=CompiledModel.Compile(definition);definition.Components[0]=film with{FuelFilm=Definition() with{InitialMass=new(10e-6,Unit.Kilogram)}};Require(compiled.Fingerprint!=CompiledModel.Compile(definition).Fingerprint);
        var a=compiled.CreateSimulation();var b=CompiledModel.Compile(Model()).CreateSimulation();Ok(a.Step(200_000_000));Ok(b.Step(200_000_000));Require(Snapshot(a)==Snapshot(b));
        var crowded = definition with
        {
            Components = Enumerable.Range(0, 25).Select(i => ComponentDefinition.LiquidFilm((uint)(20 + i), 2, 3, .1, Definition())).ToArray()
        };
        Require(!CompiledModel.TryCompile(crowded, out _, out var diagnostic) && diagnostic?.Code == DiagnosticCode.Capacity);
    }
}
