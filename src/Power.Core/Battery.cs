// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record BatteryDefinition
{
    public Quantity EmptyOpenCircuitVoltage { get; init; }
    public Quantity FullOpenCircuitVoltage { get; init; }
    public Quantity SeriesResistance { get; init; }
    public Quantity PolarizationResistance { get; init; }
    public Quantity PolarizationCapacitance { get; init; }
    public uint HeatNode { get; init; }
}

public readonly record struct BatteryReaction(double OpenCircuitVoltage, double TerminalVoltage,
    double ChemicalEnergyJoules, double PolarizationEnergyJoules, double HeatFlowWatts);

/// <summary>Finite charge inventory, affine open-circuit voltage and one conserving Thevenin RC branch.</summary>
public sealed class TheveninBattery
{
    public double CapacityCoulombs { get; }
    public double EmptyOpenCircuitVoltage { get; }
    public double FullOpenCircuitVoltage { get; }
    public double SeriesResistanceOhms { get; }
    public double PolarizationResistanceOhms { get; }
    public double PolarizationCapacitanceFarads { get; }
    public TheveninBattery(double capacityCoulombs, double emptyOpenCircuitVoltage, double fullOpenCircuitVoltage,
        double seriesResistanceOhms, double polarizationResistanceOhms, double polarizationCapacitanceFarads)
    {
        if (!Numeric.Finite(capacityCoulombs) || capacityCoulombs <= 0) throw new ArgumentException("Charge capacity must be finite and positive.", nameof(capacityCoulombs));
        if (!Numeric.Finite(emptyOpenCircuitVoltage) || !Numeric.Finite(fullOpenCircuitVoltage) || emptyOpenCircuitVoltage <= 0 || fullOpenCircuitVoltage < emptyOpenCircuitVoltage)
            throw new ArgumentException("Open-circuit voltages must be finite, positive and nondecreasing with charge.", nameof(fullOpenCircuitVoltage));
        if (!Numeric.Finite(seriesResistanceOhms) || seriesResistanceOhms < 0) throw new ArgumentException("Series resistance must be finite and nonnegative.", nameof(seriesResistanceOhms));
        if (!Numeric.Finite(polarizationResistanceOhms) || polarizationResistanceOhms <= 0) throw new ArgumentException("Polarization resistance must be finite and positive.", nameof(polarizationResistanceOhms));
        if (!Numeric.Finite(polarizationCapacitanceFarads) || polarizationCapacitanceFarads <= 0) throw new ArgumentException("Polarization capacitance must be finite and positive.", nameof(polarizationCapacitanceFarads));
        CapacityCoulombs = capacityCoulombs; EmptyOpenCircuitVoltage = emptyOpenCircuitVoltage; FullOpenCircuitVoltage = fullOpenCircuitVoltage;
        SeriesResistanceOhms = seriesResistanceOhms; PolarizationResistanceOhms = polarizationResistanceOhms; PolarizationCapacitanceFarads = polarizationCapacitanceFarads;
    }
    public double OpenCircuitVoltage(double stateOfCharge) => EmptyOpenCircuitVoltage + (FullOpenCircuitVoltage - EmptyOpenCircuitVoltage) * stateOfCharge;
    public double ChemicalEnergy(double stateOfCharge) => CapacityCoulombs * stateOfCharge * (EmptyOpenCircuitVoltage + .5 * (FullOpenCircuitVoltage - EmptyOpenCircuitVoltage) * stateOfCharge);
    public bool TryEvaluate(double stateOfCharge, double polarizationVoltage, double dischargeCurrentAmperes, out BatteryReaction reaction)
    {
        reaction = default;
        if (!Numeric.Finite(stateOfCharge) || stateOfCharge < 0 || stateOfCharge > 1 || !Numeric.Finite(polarizationVoltage) || !Numeric.Finite(dischargeCurrentAmperes)) return false;
        double open = OpenCircuitVoltage(stateOfCharge), terminal = open - polarizationVoltage - SeriesResistanceOhms * dischargeCurrentAmperes;
        double chemical = ChemicalEnergy(stateOfCharge), polarization = .5 * PolarizationCapacitanceFarads * polarizationVoltage * polarizationVoltage;
        double heat = SeriesResistanceOhms * dischargeCurrentAmperes * dischargeCurrentAmperes + polarizationVoltage * (polarizationVoltage / PolarizationResistanceOhms);
        if (!GearReference.Finite(open, terminal, chemical, polarization) || !Numeric.Finite(heat) || terminal < 0) return false;
        reaction = new(open, terminal, chemical, polarization, heat); return true;
    }
}

internal sealed record CompiledBattery(int Node, int State, int Heat, TheveninBattery Law);

/// <summary>Exact linear circuit assembly for held duties/load openings, shared with the electromechanical midpoint system.</summary>
internal static class BatteryCircuit
{
    internal static double Conductance(CompiledModel model, int batteryNode, double[] inputs)
    {
        double conductance = 0;
        foreach (int index in model.ElectricalComponents)
        { var c = model.Components[index]; if (c.Kind == ComponentKind.ResistiveLoad && c.A == batteryNode) conductance += inputs[index] / c.P0; }
        return conductance;
    }
    internal static bool Evaluate(CompiledModel model, CompiledBattery battery, double[] state, double[] inputs, out double current, out BatteryReaction reaction)
    {
        double motorCurrent = 0;
        foreach (int index in model.ElectricalComponents)
        { var c = model.Components[index]; if (c.Kind == ComponentKind.BatteryMotor && c.B == battery.Node) motorCurrent += inputs[index] * state[c.Index]; }
        double conductance = Conductance(model, battery.Node, inputs);
        double potential = battery.Law.OpenCircuitVoltage(state[battery.State]) - state[battery.State + 1];
        double denominator = 1 + battery.Law.SeriesResistanceOhms * conductance;
        current = (motorCurrent + conductance * potential) / denominator;
        if (!Numeric.Finite(denominator) || !Numeric.Finite(conductance) || !Numeric.Finite(current)) { reaction = default; return false; }
        return battery.Law.TryEvaluate(state[battery.State], state[battery.State + 1], current, out reaction);
    }
    internal static void Assemble(CompiledModel model, double[,] rates, double[] force, double[] inputs)
    {
        foreach (var battery in model.Batteries)
        {
            int z = battery.State, p = z + 1; var law = battery.Law;
            double conductance = Conductance(model, battery.Node, inputs), beta = 1 / (1 + law.SeriesResistanceOhms * conductance);
            double slope = law.FullOpenCircuitVoltage - law.EmptyOpenCircuitVoltage, q = law.CapacityCoulombs, cap = law.PolarizationCapacitanceFarads;
            rates[z, z] -= beta * conductance * slope / q; rates[z, p] += beta * conductance / q;
            rates[p, z] += beta * conductance * slope / cap;
            rates[p, p] -= beta * conductance / cap + 1 / law.PolarizationResistanceOhms / cap;
            force[z] -= beta * conductance * law.EmptyOpenCircuitVoltage / q; force[p] += beta * conductance * law.EmptyOpenCircuitVoltage / cap;
            foreach (int index in model.ElectricalComponents)
            {
                var motor = model.Components[index]; if (motor.Kind != ComponentKind.BatteryMotor || motor.B != battery.Node) continue;
                double duty = inputs[index]; int i = motor.Index;
                rates[z, i] -= beta * duty / q; rates[p, i] += beta * duty / cap;
                rates[i, z] += duty * beta * slope / motor.P1; rates[i, p] -= duty * beta / motor.P1;
                force[i] += duty * beta * law.EmptyOpenCircuitVoltage / motor.P1;
                foreach (int otherIndex in model.ElectricalComponents)
                {
                    var other = model.Components[otherIndex];
                    if (other.Kind == ComponentKind.BatteryMotor && other.B == battery.Node)
                        rates[i, other.Index] -= duty * beta * law.SeriesResistanceOhms * inputs[otherIndex] / motor.P1;
                }
            }
        }
    }
}

internal sealed class ElectricalDynamics
{
    private readonly CompiledModel _model;
    private readonly double[,] _rates, _matrix;
    private readonly double[] _inputs;
    private double _duration;
    private bool _ready;
    internal readonly double[] Force;
    internal readonly Factorization Dynamics;
    internal int Generation { get; private set; }
    internal ElectricalDynamics(CompiledModel model)
    {
        _model = model; _rates = new double[model.DynamicCount, model.DynamicCount]; _matrix = new double[model.DynamicCount, model.DynamicCount];
        _inputs = new double[model.ComponentCount]; Force = new double[model.DynamicCount]; Dynamics = new(model.DynamicCount);
    }
    internal bool Prepare(double duration, double[] inputs)
    {
        bool changed = !_ready;
        foreach (int index in _model.ElectricalComponents) if (_inputs[index] != inputs[index]) changed = true;
        if (!changed && duration == _duration) return true;
        _ready = false;
        Array.Copy(_model.ElectricalBaseRates!, _rates, _rates.Length); Array.Copy(_model.ElectricalBaseForce!, Force, Force.Length);
        BatteryCircuit.Assemble(_model, _rates, Force, inputs);
        foreach (double value in Force) if (!Numeric.Finite(value)) return false;
        for (int row = 0; row < Force.Length; ++row)
            for (int col = 0; col < Force.Length; ++col) _matrix[row, col] = (row == col ? 1 : 0) - .5 * duration * _rates[row, col];
        if (!Dynamics.Refactor(_matrix)) return false;
        Array.Copy(inputs, _inputs, inputs.Length); _duration = duration; Generation = unchecked(Generation + 1); _ready = true;
        return true;
    }
}
