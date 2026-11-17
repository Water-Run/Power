// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Rigid, horizontal, always-open liquid line; material properties come from its finite fuel ports.</summary>
public sealed record FuelInertialPipeDefinition
{
    public Quantity Length { get; init; }
    public Quantity Area { get; init; }
    public Quantity LinearResistance { get; init; }
    public Quantity QuadraticResistance { get; init; }
    public Quantity InitialFlow { get; init; }
    public Quantity InitialTemperature { get; init; }
    public Quantity FluidHeatFraction { get; init; }
}

internal sealed record CompiledFuelPipe(int Component, int A, int B, int Wall, double WallCapacity, double Length,
    double Area, double Inertia, double Mass, double Linear, double Quadratic, double FluidFraction, double InitialFlow,
    double InitialTemperature, double InitialEnergy);

internal sealed class FuelPipeState(int count)
{
    internal readonly double[] Flow = new double[count], Energy = new double[count], EnergyCorrection = new double[count],
        Forward = new double[count], ForwardCorrection = new double[count], Reverse = new double[count], ReverseCorrection = new double[count],
        Net = new double[count], NetCorrection = new double[count], Work = new double[count], WorkCorrection = new double[count],
        Friction = new double[count], FrictionCorrection = new double[count], FluidHeat = new double[count], FluidCorrection = new double[count],
        WallHeat = new double[count], WallCorrection = new double[count], EnergyA = new double[count], EnergyACorrection = new double[count],
        EnergyB = new double[count], EnergyBCorrection = new double[count], Entropy = new double[count], EntropyCorrection = new double[count], TickVolume = new double[count];
    internal void CopyFrom(FuelPipeState other)
    {
        Array.Copy(other.Flow, Flow, count); Array.Copy(other.Energy, Energy, count); Array.Copy(other.EnergyCorrection, EnergyCorrection, count);
        Array.Copy(other.Forward, Forward, count); Array.Copy(other.ForwardCorrection, ForwardCorrection, count); Array.Copy(other.Reverse, Reverse, count); Array.Copy(other.ReverseCorrection, ReverseCorrection, count);
        Array.Copy(other.Net, Net, count); Array.Copy(other.NetCorrection, NetCorrection, count); Array.Copy(other.Work, Work, count); Array.Copy(other.WorkCorrection, WorkCorrection, count);
        Array.Copy(other.Friction, Friction, count); Array.Copy(other.FrictionCorrection, FrictionCorrection, count); Array.Copy(other.FluidHeat, FluidHeat, count); Array.Copy(other.FluidCorrection, FluidCorrection, count);
        Array.Copy(other.WallHeat, WallHeat, count); Array.Copy(other.WallCorrection, WallCorrection, count); Array.Copy(other.EnergyA, EnergyA, count); Array.Copy(other.EnergyACorrection, EnergyACorrection, count);
        Array.Copy(other.EnergyB, EnergyB, count); Array.Copy(other.EnergyBCorrection, EnergyBCorrection, count); Array.Copy(other.Entropy, Entropy, count); Array.Copy(other.EntropyCorrection, EntropyCorrection, count); Array.Copy(other.TickVolume, TickVolume, count);
    }
    internal void BeginTick() => Array.Clear(TickVolume, 0, count);
    internal bool EndTick(double seconds) { for (int i = 0; i < count; ++i) TickVolume[i] /= seconds; return Finite(); }
    internal bool Finite()
    {
        for (int i = 0; i < count; ++i)
            if (!GearReference.Finite(Flow[i], Energy[i], EnergyCorrection[i], TickVolume[i]) ||
                !GearReference.Finite(Forward[i], ForwardCorrection[i], Reverse[i], ReverseCorrection[i]) ||
                !GearReference.Finite(Net[i], NetCorrection[i], Work[i], WorkCorrection[i]) ||
                !GearReference.Finite(Friction[i], FrictionCorrection[i], FluidHeat[i], FluidCorrection[i]) ||
                !GearReference.Finite(WallHeat[i], WallCorrection[i], EnergyA[i], EnergyACorrection[i]) ||
                !GearReference.Finite(EnergyB[i], EnergyBCorrection[i], Entropy[i], EntropyCorrection[i]) || Forward[i] < 0 || Reverse[i] < 0 || Friction[i] < 0 || FluidHeat[i] < 0 || WallHeat[i] < 0) return false;
        return true;
    }
    internal ulong Hash(ulong h)
    {
        for (int i = 0; i < count; ++i)
        {
            h = Numeric.Hash(h, Flow[i]); h = Numeric.Hash(h, Energy[i]); h = Numeric.Hash(h, EnergyCorrection[i]); h = Numeric.Hash(h, TickVolume[i]);
            h = Numeric.Hash(h, Forward[i]); h = Numeric.Hash(h, ForwardCorrection[i]); h = Numeric.Hash(h, Reverse[i]); h = Numeric.Hash(h, ReverseCorrection[i]);
            h = Numeric.Hash(h, Net[i]); h = Numeric.Hash(h, NetCorrection[i]); h = Numeric.Hash(h, Work[i]); h = Numeric.Hash(h, WorkCorrection[i]);
            h = Numeric.Hash(h, Friction[i]); h = Numeric.Hash(h, FrictionCorrection[i]); h = Numeric.Hash(h, FluidHeat[i]); h = Numeric.Hash(h, FluidCorrection[i]);
            h = Numeric.Hash(h, WallHeat[i]); h = Numeric.Hash(h, WallCorrection[i]); h = Numeric.Hash(h, EnergyA[i]); h = Numeric.Hash(h, EnergyACorrection[i]);
            h = Numeric.Hash(h, EnergyB[i]); h = Numeric.Hash(h, EnergyBCorrection[i]); h = Numeric.Hash(h, Entropy[i]); h = Numeric.Hash(h, EntropyCorrection[i]);
        }
        return h;
    }
}

public sealed partial class CompiledModel
{
    internal CompiledFuelPipe[] FuelPipes { get; private set; } = [];
    internal int[] FuelPipeByComponent { get; private set; } = [];
    public bool HasFuelPipes => FuelPipes.Length != 0;
    private void CompileFuelPipes(ComponentDefinition[] definitions)
    {
        FuelPipeByComponent = new int[Components.Length]; Array.Fill(FuelPipeByComponent, -1); var result = new List<CompiledFuelPipe>();
        for (int i = 0; i < Components.Length; ++i)
        {
            var c = Components[i]; if (c.Kind != ComponentKind.FuelInertialPipe) continue;
            var d = definitions[i].FuelPipe!; int a = FuelByNode[c.A], b = FuelByNode[c.B];
            Require(FuelCompatible(FuelVolumes[a], FuelVolumes[b]), DiagnosticCode.Connection, c.Id, "fuel_pipe.material", "Inertial line ports require identical pure-fuel material and density.");
            double Q(Quantity q, Unit unit, string field) => Convert(q, unit, c.Id, "fuel_pipe." + field);
            double length = Q(d.Length, Unit.Meter, "length"), area = Q(d.Area, Unit.SquareMeter, "area"),
                linear = Q(d.LinearResistance, Unit.PascalSecondPerCubicMeter, "linear_resistance"), quadratic = Q(d.QuadraticResistance, Unit.PascalSecondSquaredPerCubicMeterSquared, "quadratic_resistance"),
                flow = Q(d.InitialFlow, Unit.CubicMeterPerSecond, "initial_flow"), temperature = Q(d.InitialTemperature, Unit.Kelvin, "initial_temperature"), fraction = Q(d.FluidHeatFraction, Unit.Fraction, "fluid_heat_fraction");
            Require(length > 0 && area > 0 && linear >= 0 && quadratic >= 0 && fraction is >= 0 and <= 1, DiagnosticCode.Range, c.Id, "fuel_pipe", "Declare positive line geometry, nonnegative pressure-loss coefficients and a heat fraction in [0,1].");
            var law = FuelVolumes[a].Law; double density = law.LiquidDensityKilogramsPerCubicMeter, mass = density * length * area, inertia = density * length / area;
            Require(GearReference.Finite(mass, inertia, 1 / inertia, .5 * inertia * flow * flow) && mass > 0 && inertia > 0 && Numeric.Finite(mass * FuelVolumes[a].HeatingValue),
                DiagnosticCode.Range, c.Id, "fuel_pipe.geometry", "Line mass and inertance must be positive and representable.");
            var pa = FuelVolumes[a].Initial; var pb = FuelVolumes[b].Initial;
            Require(pa.Phase == FuelVolumePhase.Liquid && pb.Phase == FuelVolumePhase.Liquid &&
                FuelPipeSolver.LiquidAt(law, temperature, Math.Min(pa.PressurePascals, pb.PressurePascals)), DiagnosticCode.Range, c.Id, "fuel_pipe.initial_temperature",
                "Use liquid-only ports and line temperature inside the material interval and below its pressure-corrected boiling limit. Flashing is unsupported.");
            double energy = mass * (law.Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin * temperature + law.Material.Caloric.LiquidEnergyOffsetJoulesPerKilogram);
            Require(Numeric.Finite(energy), DiagnosticCode.Range, c.Id, "fuel_pipe.initial_temperature", "Line caloric inventory is not representable.");
            FuelPipeByComponent[i] = result.Count;
            result.Add(new(i, a, b, Nodes[c.Heat].Index, Nodes[c.Heat].Storage, length, area, inertia, mass, linear, quadratic, fraction, flow, temperature, energy));
        }
        FuelPipes = result.ToArray();
    }
}

/// <summary>
/// Pairwise finite-liquid momentum and caloric inventory. End-state port enthalpy and
/// mixing are backward Euler; midpoint momentum is energy conjugate. Pressure-endpoint
/// discretization adds nonnegative numerical caloric heating, distinct from friction.
/// </summary>
internal sealed class FuelPipeSolver(CompiledModel model)
{
    internal static bool LiquidAt(CompliantFuelVolume law, double temperature, double pressure) =>
        law.Saturation(temperature, out var saturation) == FuelVolumeStatus.Success && FuelPhaseEquilibrium.PressureAllowsLiquid(pressure, saturation.PressurePascals);
    internal static double Temperature(CompiledModel model, CompiledFuelPipe pipe, double energy)
    {
        var caloric = model.FuelVolumes[pipe.A].Law.Material.Caloric;
        return (energy / pipe.Mass - caloric.LiquidEnergyOffsetJoulesPerKilogram) / caloric.LiquidSpecificHeatJoulesPerKilogramKelvin;
    }
    internal bool Advance(FuelPipeState pipes, EquilibriumFuelState fuel, double[] wallTemperature, double seconds, bool reverse = false)
    {
        for (int order = 0; order < model.FuelPipes.Length; ++order)
        {
            int i = reverse ? model.FuelPipes.Length - 1 - order : order; var pipe = model.FuelPipes[i];
            if (!EquilibriumFuelSolver.Point(model.FuelVolumes[pipe.A], fuel, pipe.A, out var a) || !EquilibriumFuelSolver.Point(model.FuelVolumes[pipe.B], fuel, pipe.B, out var b) ||
                a.Phase != FuelVolumePhase.Liquid || b.Phase != FuelVolumePhase.Liquid) return false;
            double slope = 1 / model.FuelVolumes[pipe.A].Law.ComplianceCubicMetersPerPascal + 1 / model.FuelVolumes[pipe.B].Law.ComplianceCubicMetersPerPascal;
            if (!Numeric.Finite(slope) || seconds * Math.Sqrt(slope / pipe.Inertia) > .5) return false;
            if (!Mean(pipe, pipes.Flow[i], a.PressurePascals - b.PressurePascals, slope, seconds, out double mean)) return false;
            double end = 2 * mean - pipes.Flow[i], initial = pipes.Flow[i];
            if (initial > 0 && end < 0 || initial < 0 && end > 0)
            {
                double half = initial / 2, lo = 0, hi = seconds;
                for (int iteration = 0; iteration < 64; ++iteration)
                {
                    double h = (lo + hi) / 2;
                    double residual = -pipe.Inertia * initial - h * (a.PressurePascals - b.PressurePascals - h * slope * half - Loss(pipe, half));
                    if ((initial > 0 ? residual : -residual) > 0) hi = h; else lo = h;
                }
                double crossing = (lo + hi) / 2;
                if (crossing <= 0 || crossing >= seconds || !Transfer(pipes, fuel, wallTemperature, i, crossing, true) || !Transfer(pipes, fuel, wallTemperature, i, seconds - crossing, false)) return false;
            }
            else if (!Transfer(pipes, fuel, wallTemperature, i, seconds, false)) return false;
        }
        return pipes.Finite() && fuel.Finite();
    }
    private static double Loss(CompiledFuelPipe p, double flow) => p.Linear * flow + p.Quadratic * flow * Math.Abs(flow);
    private static bool Mean(CompiledFuelPipe p, double initial, double pressure, double slope, double seconds, out double mean)
    {
        double linear = 2 * p.Inertia + seconds * p.Linear + seconds * seconds * slope, quadratic = seconds * p.Quadratic, right = 2 * p.Inertia * initial + seconds * pressure;
        mean = 0;
        if (!GearReference.Finite(linear, quadratic, right, 0) || linear <= 0) return false;
        if (quadratic == 0) mean = right / linear;
        else
        {
            double discriminant = linear * linear + 4 * quadratic * Math.Abs(right);
            if (!Numeric.Finite(discriminant) || discriminant <= 0) return false;
            mean = right / (.5 * linear + .5 * Math.Sqrt(discriminant));
        }
        return Numeric.Finite(mean);
    }
    private bool Transfer(FuelPipeState state, EquilibriumFuelState fuel, double[] wallTemperature, int slot, double seconds, bool zeroEnd)
    {
        var pipe = model.FuelPipes[slot]; var va = model.FuelVolumes[pipe.A]; var vb = model.FuelVolumes[pipe.B];
        if (!EquilibriumFuelSolver.Point(va, fuel, pipe.A, out var a) || !EquilibriumFuelSolver.Point(vb, fuel, pipe.B, out var b) || a.Phase != FuelVolumePhase.Liquid || b.Phase != FuelVolumePhase.Liquid) return false;
        double ca = va.Law.ComplianceCubicMetersPerPascal, cb = vb.Law.ComplianceCubicMetersPerPascal, q0 = state.Flow[slot], mean;
        if (zeroEnd) mean = q0 / 2;
        else if (!Mean(pipe, q0, a.PressurePascals - b.PressurePascals, 1 / ca + 1 / cb, seconds, out mean)) return false;
        double q1 = zeroEnd ? 0 : 2 * mean - q0, volume = seconds * mean, rho = va.Law.LiquidDensityKilogramsPerCubicMeter, mass = rho * volume;
        if (!GearReference.Finite(q1, volume, mass, 0) || Math.Abs(mass) > .25 * Math.Min(a.MassKilograms, b.MassKilograms)) return false;
        double ma = fuel.Mass[pipe.A], mb = fuel.Mass[pipe.B], mac = fuel.MassCorrection[pipe.A], mbc = fuel.MassCorrection[pipe.B];
        Numeric.Accumulate(-mass, ref ma, ref mac); Numeric.Accumulate(mass, ref mb, ref mbc);
        double ra = fuel.ReferenceVolume is null ? va.Law.ReferenceVolumeCubicMeters : fuel.ReferenceVolume[pipe.A];
        double rb = fuel.ReferenceVolume is null ? vb.Law.ReferenceVolumeCubicMeters : fuel.ReferenceVolume[pipe.B];
        double pa = (ma / rho - ra) / ca, pb = (mb / rho - rb) / cb;
        if (!GearReference.Finite(pa, pb, ma, mb) || pa <= 0 || pb <= 0 || pa > va.Law.MaximumPressurePascals || pb > vb.Law.MaximumPressurePascals) return false;
        var caloric = va.Law.Material.Caloric; double cl = caloric.LiquidSpecificHeatJoulesPerKilogramKelvin, offset = caloric.LiquidEnergyOffsetJoulesPerKilogram;
        bool forward = mass >= 0; double amount = Math.Abs(mass), donorMass = forward ? a.MassKilograms : b.MassKilograms, donorEnergy = forward ? fuel.Energy[pipe.A] : fuel.Energy[pipe.B];
        double donorPressure = forward ? pa : pb, donorCompliance = forward ? ca : cb;
        double donorInternal = (donorEnergy - .5 * donorCompliance * donorPressure * donorPressure - amount * donorPressure / rho) / donorMass;
        double donorTemperature = (donorInternal - offset) / cl, oldLineTemperature = Temperature(model, pipe, state.Energy[slot]);
        double friction = seconds * mean * Loss(pipe, mean), fluidHeat = pipe.FluidFraction * friction, wallHeat = friction - fluidHeat;
        double lineTemperature = oldLineTemperature + (amount * cl * (donorTemperature - oldLineTemperature) + fluidHeat) / ((pipe.Mass + amount) * cl);
        double lineInternal = cl * lineTemperature + offset, oldWall = wallTemperature[pipe.Wall], newWall = oldWall + wallHeat / pipe.WallCapacity;
        if (!GearReference.Finite(donorTemperature, lineTemperature, friction, newWall) || friction < 0 || newWall <= 0 ||
            !LiquidAt(va.Law, lineTemperature, Math.Min(pa, pb))) return false;
        double outA = forward ? mass * (donorInternal + pa / rho) : mass * (lineInternal + pa / rho);
        double inB = forward ? mass * (lineInternal + pb / rho) : mass * (donorInternal + pb / rho);
        double lineChange = amount * (donorInternal - lineInternal) + fluidHeat;
        double ea = fuel.Energy[pipe.A], eb = fuel.Energy[pipe.B], eac = fuel.EnergyCorrection[pipe.A], ebc = fuel.EnergyCorrection[pipe.B];
        double el = state.Energy[slot], elc = state.EnergyCorrection[slot];
        Numeric.Accumulate(-outA, ref ea, ref eac); Numeric.Accumulate(inB, ref eb, ref ebc); Numeric.Accumulate(lineChange, ref el, ref elc);
        if (va.Law.FlashAtReferenceVolume(ma, ea, ra, out var endA) != FuelVolumeStatus.Success || vb.Law.FlashAtReferenceVolume(mb, eb, rb, out var endB) != FuelVolumeStatus.Success ||
            endA.Phase != FuelVolumePhase.Liquid || endB.Phase != FuelVolumePhase.Liquid ||
            !LiquidAt(va.Law, Temperature(model, pipe, el), Math.Min(endA.PressurePascals, endB.PressurePascals))) return false;
        double kineticChange = pipe.Inertia * mean * (q1 - q0), work = (pa - pb) * volume;
        double scale = Math.Abs(outA) + Math.Abs(inB) + Math.Abs(lineChange) + friction + Math.Abs(work) + .5 * pipe.Inertia * (q0 * q0 + q1 * q1);
        if (!Numeric.Finite(scale) || Math.Abs(work - friction - kineticChange) > 1024 * GearReference.Epsilon * scale) return false;
        double entropy = endA.EntropyJoulesPerKelvin - a.EntropyJoulesPerKelvin + endB.EntropyJoulesPerKelvin - b.EntropyJoulesPerKelvin +
            pipe.Mass * cl * Numeric.Log1p((Temperature(model, pipe, el) - oldLineTemperature) / oldLineTemperature) + pipe.WallCapacity * Numeric.Log1p((newWall - oldWall) / oldWall);
        double entropyScale = Math.Abs(a.EntropyJoulesPerKelvin) + Math.Abs(b.EntropyJoulesPerKelvin) + a.HeatCapacityJoulesPerKelvin + b.HeatCapacityJoulesPerKelvin + pipe.Mass * cl + pipe.WallCapacity;
        if (!Numeric.Finite(entropy) || entropy < -1024 * GearReference.Epsilon * entropyScale) return false;
        fuel.Mass[pipe.A] = ma; fuel.MassCorrection[pipe.A] = mac; fuel.Mass[pipe.B] = mb; fuel.MassCorrection[pipe.B] = mbc;
        fuel.Energy[pipe.A] = ea; fuel.EnergyCorrection[pipe.A] = eac; fuel.Energy[pipe.B] = eb; fuel.EnergyCorrection[pipe.B] = ebc;
        state.Flow[slot] = q1; state.Energy[slot] = el; state.EnergyCorrection[slot] = elc; wallTemperature[pipe.Wall] = newWall;
        state.TickVolume[slot] += volume;
        Numeric.Accumulate(Math.Max(volume, 0), ref state.Forward[slot], ref state.ForwardCorrection[slot]); Numeric.Accumulate(Math.Max(-volume, 0), ref state.Reverse[slot], ref state.ReverseCorrection[slot]);
        Numeric.Accumulate(volume, ref state.Net[slot], ref state.NetCorrection[slot]); Numeric.Accumulate(work, ref state.Work[slot], ref state.WorkCorrection[slot]);
        Numeric.Accumulate(friction, ref state.Friction[slot], ref state.FrictionCorrection[slot]); Numeric.Accumulate(fluidHeat, ref state.FluidHeat[slot], ref state.FluidCorrection[slot]);
        Numeric.Accumulate(wallHeat, ref state.WallHeat[slot], ref state.WallCorrection[slot]); Numeric.Accumulate(outA, ref state.EnergyA[slot], ref state.EnergyACorrection[slot]);
        Numeric.Accumulate(inB, ref state.EnergyB[slot], ref state.EnergyBCorrection[slot]); Numeric.Accumulate(entropy, ref state.Entropy[slot], ref state.EntropyCorrection[slot]);
        return true;
    }
}
