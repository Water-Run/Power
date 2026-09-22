// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Prescribed Wiebe burn law. No kinetics, flame propagation or autoignition prediction.</summary>
public sealed class WiebeBurnProfile
{
    public double CycleAngleRadians { get; }
    public double StartAngleRadians { get; }
    public double DurationAngleRadians { get; }
    public double ShapeExponent { get; }
    public double BurnCoefficient { get; }
    public double MaxTravelPerTickRadians => Math.Min(.25, DurationAngleRadians / 32);

    public WiebeBurnProfile(double cycle, double start, double duration, double exponent, double coefficient)
    {
        if (cycle != 2 * Math.PI && cycle != 4 * Math.PI)
            throw new ArgumentException("Combustion cycle must be 360 or 720 degrees.");
        if (!Numeric.Finite(start) || !Numeric.Finite(duration) || duration < 1e-6 || duration > cycle)
            throw new ArgumentException("Burn start must be finite and duration must be in [1e-6 rad, cycle angle].");
        if (!Numeric.Finite(exponent) || exponent < 1 || exponent > 16 || !Numeric.Finite(coefficient) || coefficient <= 0 || coefficient > 50)
            throw new ArgumentException("Wiebe shape exponent must be in [1,16] and burn coefficient in (0,50].");
        CycleAngleRadians = cycle; StartAngleRadians = start % cycle;
        if (StartAngleRadians < 0) StartAngleRadians += cycle;
        DurationAngleRadians = duration; ShapeExponent = exponent; BurnCoefficient = coefficient;
    }

    private double Exposure(double local) => BurnCoefficient * Math.Pow(Math.Min(1, Math.Max(0, local / DurationAngleRadians)), ShapeExponent);

    /// <summary>Integrated nonnegative burn hazard between forward angles, including cycle wrap.</summary>
    public double ExposureBetween(double from, double to)
    {
        if (!Numeric.Finite(from) || !Numeric.Finite(to) || to < from || to - from > CycleAngleRadians)
            throw new ArgumentOutOfRangeException(nameof(to), "Use finite increasing angles spanning at most one cycle.");
        double phase = (from % CycleAngleRadians - StartAngleRadians) % CycleAngleRadians;
        if (phase < 0) phase += CycleAngleRadians;
        double end = phase + (to - from);
        // Subtract local potentials, not large unwrapped cycle numbers.
        double result = end <= CycleAngleRadians ? Exposure(end) - Exposure(phase)
            : BurnCoefficient - Exposure(phase) + Exposure(end - CycleAngleRadians);
        return Math.Max(0, result);
    }
}

internal sealed class PremixedGas
{
    internal readonly double Lhv, AirFuelRatio;
    internal readonly MassFractions Initial;
    internal PremixedGas(PremixedGasDefinition definition, uint id)
    {
        Lhv = CompiledModel.Convert(definition.LowerHeatingValue, Unit.JoulePerKilogram, id, "gas.premixed.lower_heating_value");
        AirFuelRatio = definition.StoichiometricAirFuelRatio;
        if (!(Lhv > 0) || !Numeric.Finite(AirFuelRatio) || AirFuelRatio <= 0)
            throw new ModelCompileException(DiagnosticCode.Range, id, "gas.premixed", "Heating value and stoichiometric air/fuel ratio must be positive and finite.");
        Initial = ValidateFractions(definition.InitialFractions, id, "gas.premixed.initial_fractions");
    }
    internal static MassFractions ValidateFractions(MassFractions? value, uint id, string field)
    {
        if (value is null) throw new ModelCompileException(DiagnosticCode.Schema, id, field, "Explicit fuel and fresh-air mass fractions are required.");
        if (!Numeric.Finite(value.Fuel) || !Numeric.Finite(value.FreshAir) || value.Fuel < 0 || value.FreshAir < 0 || value.Fuel + value.FreshAir > 1)
            throw new ModelCompileException(DiagnosticCode.Range, id, field, "Nonnegative fractions must sum to at most one; the remainder is inert products.");
        return value;
    }
    internal bool Compatible(PremixedGas? other) => other is not null && Lhv == other.Lhv && AirFuelRatio == other.AirFuelRatio;
}

internal sealed class Burner(int crank, int gas, WiebeBurnProfile profile)
{
    internal readonly int Crank = crank, Gas = gas;
    internal readonly WiebeBurnProfile Profile = profile;
}

/// <summary>All chemical inventories, irreversible angle history and boundary ledgers are transactional.</summary>
internal sealed class MixtureState
{
    internal readonly double[] Fuel, Air, Products, Frontier, Burned, BurnCorrection;
    internal double FuelIn, FuelInCorrection, AirIn, AirInCorrection, ChemicalIn, ChemicalInCorrection;
    internal MixtureState(CompiledModel model)
    {
        int count = model.GasCount;
        Fuel = new double[count]; Air = new double[count]; Products = new double[count];
        Frontier = new double[model.ComponentCount]; Burned = new double[model.ComponentCount]; BurnCorrection = new double[model.ComponentCount];
        for (int i = 0; i < count; ++i)
        {
            Fuel[i] = model.Gas!.InitialFuel[i]; Air[i] = model.Gas.InitialAir[i]; Products[i] = model.Gas.InitialProducts[i];
        }
        for (int i = 0; i < model.ComponentCount; ++i)
            if (model.Burners[i] is not null) Frontier[i] = model.Nodes[model.Components[i].A].Position;
    }
    internal void CopyFrom(MixtureState other)
    {
        Array.Copy(other.Fuel, Fuel, Fuel.Length); Array.Copy(other.Air, Air, Air.Length); Array.Copy(other.Products, Products, Products.Length);
        Array.Copy(other.Frontier, Frontier, Frontier.Length); Array.Copy(other.Burned, Burned, Burned.Length); Array.Copy(other.BurnCorrection, BurnCorrection, BurnCorrection.Length);
        FuelIn = other.FuelIn; FuelInCorrection = other.FuelInCorrection; AirIn = other.AirIn; AirInCorrection = other.AirInCorrection;
        ChemicalIn = other.ChemicalIn; ChemicalInCorrection = other.ChemicalInCorrection;
    }
    internal ulong Hash(ulong h)
    {
        static ulong Values(ulong hash, double[] array)
        { foreach (double value in array) hash = Numeric.Hash(hash, value); return hash; }
        h = Values(h, Fuel); h = Values(h, Air); h = Values(h, Products);
        h = Values(h, Frontier); h = Values(h, Burned); h = Values(h, BurnCorrection);
        h = Numeric.Hash(h, FuelIn); h = Numeric.Hash(h, FuelInCorrection); h = Numeric.Hash(h, AirIn);
        h = Numeric.Hash(h, AirInCorrection); h = Numeric.Hash(h, ChemicalIn);
        return Numeric.Hash(h, ChemicalInCorrection);
    }
}

/// <summary>Heat preview participates in the crank solve; inventories commit only after it converges.</summary>
internal sealed class CombustionSolver(CompiledModel model)
{
    private MixtureState _state = null!;
    private double[] _inputs = null!;
    internal readonly double[] Heat = new double[model.GasCount], Consumed = new double[model.ComponentCount];
    internal void Prepare(MixtureState state, double[] inputs) { _state = state; _inputs = inputs; }

    private double FuelConsumed(int index, double nextAngle)
    {
        var burner = model.Burners[index]!;
        double frontier = _state.Frontier[index];
        if (nextAngle <= frontier || _inputs[index] == 0) return 0;
        double exposure = burner.Profile.ExposureBetween(frontier, nextAngle) * _inputs[index];
        var gas = model.Gas!.Mixtures[burner.Gas]!;
        double available = Math.Min(_state.Fuel[burner.Gas], _state.Air[burner.Gas] / gas.AirFuelRatio);
        return available * -Numeric.Expm1(-exposure);
    }

    internal double PreviewHeat(int gas, double nextAngle)
    {
        int index = model.BurnerByGas[gas];
        return index < 0 ? 0 : FuelConsumed(index, nextAngle) * model.Gas!.Mixtures[gas]!.Lhv;
    }

    internal bool Resolve(double[] old, double[] midpoint, double[] energy, double dt)
    {
        Array.Clear(Heat, 0, Heat.Length); Array.Clear(Consumed, 0, Consumed.Length);
        for (int i = 0; i < model.ComponentCount; ++i)
        {
            var burner = model.Burners[i]; if (burner is null) continue;
            int a = burner.Crank;
            double next = 2 * midpoint[a] - old[a], speed = 2 * midpoint[a + 1] - old[a + 1];
            double travel = Math.Max(Math.Abs(next - old[a]), dt * Math.Max(Math.Abs(old[a + 1]), Math.Abs(speed)));
            double rounding = 8 * 2.2204460492503131e-16 * Math.Max(Math.Abs(old[a]), Math.Abs(next));
            if (!Numeric.Finite(travel) || (_inputs[i] != 0 && (travel > burner.Profile.MaxTravelPerTickRadians || rounding > burner.Profile.MaxTravelPerTickRadians))) return false;
            Consumed[i] = FuelConsumed(i, next);
            Heat[burner.Gas] = Consumed[i] * model.Gas!.Mixtures[burner.Gas]!.Lhv;
            if (!Numeric.Finite(Heat[burner.Gas]) || Heat[burner.Gas] > .25 * energy[burner.Gas]) return false;
        }
        return true;
    }

    internal bool Commit(double[] dynamics, double[] mass)
    {
        for (int i = 0; i < model.ComponentCount; ++i)
        {
            var burner = model.Burners[i]; if (burner is null) continue;
            int g = burner.Gas; double burned = Consumed[i], air = burned * model.Gas!.Mixtures[g]!.AirFuelRatio;
            _state.Frontier[i] = Math.Max(_state.Frontier[i], dynamics[burner.Crank]);
            _state.Fuel[g] -= burned; _state.Air[g] -= air; _state.Products[g] += burned + air;
            mass[g] = _state.Fuel[g] + _state.Air[g] + _state.Products[g];
            Numeric.Accumulate(burned, ref _state.Burned[i], ref _state.BurnCorrection[i]);
            if (_state.Fuel[g] < 0 || _state.Air[g] < 0 || !Numeric.Finite(_state.Burned[i]) || !Numeric.Finite(_state.BurnCorrection[i])) return false;
        }
        return true;
    }
}
