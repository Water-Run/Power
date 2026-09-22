// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

internal sealed class ClutchState(int count)
{
    internal readonly ClutchMode[] Mode = new ClutchMode[count];
    internal readonly double[] Torque = new double[count], Power = new double[count];
    internal readonly double[] Heat = new double[count], HeatCorrection = new double[count];

    internal void CopyFrom(ClutchState other)
    {
        Array.Copy(other.Mode, Mode, Mode.Length); Array.Copy(other.Torque, Torque, Torque.Length);
        Array.Copy(other.Power, Power, Power.Length); Array.Copy(other.Heat, Heat, Heat.Length);
        Array.Copy(other.HeatCorrection, HeatCorrection, HeatCorrection.Length);
    }

    internal ulong Hash(ulong hash)
    {
        for (int i = 0; i < Mode.Length; ++i)
        {
            hash = Numeric.Hash(hash, (ulong)Mode[i]); hash = Numeric.Hash(hash, Torque[i]);
            hash = Numeric.Hash(hash, Power[i]); hash = Numeric.Hash(hash, Heat[i]); hash = Numeric.Hash(hash, HeatCorrection[i]);
        }
        return hash;
    }
}

/// <summary>Per-simulation factors and bounded friction/cylinder constraint workspace.</summary>
internal sealed class ClutchSolver
{
    internal const int MaxIntervals = 32, RootIterations = 56, ConstraintIterations = 256;
    private readonly CompiledModel _model;
    private readonly MechanicalSolver? _mechanical;
    private readonly GearSolver? _gears;
    private readonly Factorization _variableDynamics, _variableThermal;
    private readonly double[,] _matrix, _heatMatrix;
    private readonly double[][] _response, _gearReaction;
    private readonly double[] _free, _diagonal, _staticCapacity, _slidingCapacity;
    internal readonly double[] Torque;
    private double _duration;
    internal Factorization Dynamics { get; private set; }
    internal Factorization Thermal { get; private set; }

    internal ClutchSolver(CompiledModel model, MechanicalSolver? mechanical, GearSolver? gears)
    {
        _model = model; _mechanical = mechanical; _gears = gears;
        Dynamics = model.Dynamics; Thermal = model.Thermal;
        _variableDynamics = new(model.DynamicCount); _variableThermal = new(model.ThermalCount);
        _matrix = new double[model.DynamicCount, model.DynamicCount]; _heatMatrix = new double[model.ThermalCount, model.ThermalCount];
        _response = model.ClutchComponents.Select(_ => new double[model.DynamicCount]).ToArray();
        _gearReaction = model.ClutchComponents.Select(_ => new double[model.GearComponents.Length]).ToArray();
        _staticCapacity = new double[model.ClutchComponents.Length]; _slidingCapacity = new double[model.ClutchComponents.Length];
        _free = new double[model.DynamicCount]; Torque = new double[_response.Length]; _diagonal = new double[_response.Length];
        if (!Prepare(model.Dt)) throw new ModelCompileException(DiagnosticCode.Solver, 0, "clutch.coupling", "Clutch response is nonfinite, ill-conditioned or fully constrained by permanent gears; inspect the power path, inertia and ratio scales.");
    }

    internal bool Prepare(double duration)
    {
        if (duration == _duration) return true;
        if (!(duration > 0) || !Numeric.Finite(duration)) return false;
        _duration = 0; // A failed preparation must never make a later retry reuse partial factors.
        if (duration == _model.Dt) { Dynamics = _model.Dynamics; Thermal = _model.Thermal; }
        else
        {
            for (int row = 0; row < _free.Length; ++row)
                for (int col = 0; col < _free.Length; ++col)
                    _matrix[row, col] = (row == col ? 1 : 0) - .5 * duration * _model.DynamicRates![row, col];
            for (int row = 0; row < _model.ThermalCount; ++row)
                for (int col = 0; col < _model.ThermalCount; ++col)
                    _heatMatrix[row, col] = duration * _model.HeatConductance![row, col];
            foreach (var node in _model.Nodes)
                if (node.Domain == Domain.Thermal) _heatMatrix[node.Index, node.Index] += node.Storage;
            if (!_variableDynamics.Refactor(_matrix) || !_variableThermal.Refactor(_heatMatrix)) return false;
            Dynamics = _variableDynamics; Thermal = _variableThermal;
        }
        if (_gears is not null && !_gears.Prepare(duration, Dynamics)) return false;
        if (_mechanical is not null && !_mechanical.SetInterval(duration, Dynamics, _gears)) return false;
        for (int k = 0; k < _response.Length; ++k)
        {
            var c = Component(k); var a = _model.Nodes[c.A];
            var response = _response[k]; Array.Clear(response, 0, response.Length);
            response[a.Index + 1] = .5 * duration / a.Storage;
            if (c.B >= 0) { var b = _model.Nodes[c.B]; response[b.Index + 1] = -.5 * duration * c.P3 / b.Storage; }
            if (!Dynamics.Solve(response)) return false;
            double freeMobility = Slip(k, response);
            if (_gears is not null && !_gears.Project(response, _gearReaction[k])) return false;
            _diagonal[k] = Slip(k, response);
            if (_gears is not null && _diagonal[k] <= 64 * GearReference.Epsilon * freeMobility) return false;
            if (!Numeric.Finite(_diagonal[k]) || _diagonal[k] <= 0) return false;
        }
        _duration = duration;
        return true;
    }

    internal void AddGearReactions(double[] reactions)
    {
        for (int k = 0; k < _gearReaction.Length; ++k)
            for (int j = 0; j < reactions.Length; ++j) reactions[j] += _gearReaction[k][j] * Torque[k];
    }

    private GraphComponent Component(int k) => _model.Components[_model.ClutchComponents[k]];
    internal double Slip(int k, double[] x)
    {
        var c = Component(k);
        return x[_model.Nodes[c.A].Index + 1] - (c.B < 0 ? 0 : c.P3 * x[_model.Nodes[c.B].Index + 1]);
    }
    private double Tolerance(int k, double[] x)
    {
        var c = Component(k);
        double a = Math.Abs(x[_model.Nodes[c.A].Index + 1]), b = c.B < 0 ? 0 : Math.Abs(c.P3 * x[_model.Nodes[c.B].Index + 1]);
        return 2e-13 + 32 * 2.2204460492503131e-16 * (a + b);
    }

    internal void BeginTick(ClutchState state, double[] x, double[] inputs, double[]? pressure = null)
    {
        Array.Clear(state.Torque, 0, state.Torque.Length); Array.Clear(state.Power, 0, state.Power.Length);
        UpdateCapacities(state, x, inputs, pressure);
    }

    private void UpdateCapacities(ClutchState state, double[] x, double[] inputs, double[]? pressure)
    {
        for (int k = 0; k < Torque.Length; ++k)
        {
            int index = _model.ClutchComponents[k]; var c = Component(k);
            var actuator = _model.HydraulicActuators[index];
            _staticCapacity[k] = actuator is null ? inputs[index] * c.P0 : actuator.Capacity(pressure![actuator.Pressure], false);
            _slidingCapacity[k] = actuator is null ? inputs[index] * c.P1 : actuator.Capacity(pressure![actuator.Pressure], true);
            if (_staticCapacity[k] == 0) state.Mode[k] = ClutchMode.Disengaged;
            else if (state.Mode[k] == ClutchMode.Disengaged)
            {
                double slip = Slip(k, x);
                state.Mode[k] = slip == 0 ? ClutchMode.Locked : slip > 0 ? ClutchMode.SlippingPositive : ClutchMode.SlippingNegative;
            }
        }
    }

    private static double Clamp(double value, double limit) => Math.Max(-limit, Math.Min(limit, value));

    private bool Midpoint(double[] old, double[] mid, double[] energy, CombustionSolver? combustion, CancellationToken cancellation)
    {
        Array.Copy(_free, mid, mid.Length);
        for (int k = 0; k < Torque.Length; ++k)
            for (int j = 0; j < mid.Length; ++j) mid[j] += _response[k][j] * Torque[k];
        return _mechanical is null || _mechanical.Solve(old, mid, energy, combustion, cancellation);
    }

    internal bool Solve(ClutchState state, double[] old, double[] mid, double[] inputs,
        double[] energy, CombustionSolver? combustion, CancellationToken cancellation, double[]? pressure = null)
    {
        if (pressure is not null) UpdateCapacities(state, old, inputs, pressure);
        for (int k = 0; k < Torque.Length; ++k)
            if (!Numeric.Finite(_staticCapacity[k]) || !Numeric.Finite(_slidingCapacity[k])) return false;
        Array.Copy(mid, _free, mid.Length);
        for (int active = 0; active <= 2 * Torque.Length + 1; ++active)
        {
            for (int k = 0; k < Torque.Length; ++k)
            {
                double kinetic = _slidingCapacity[k];
                Torque[k] = state.Mode[k] == ClutchMode.SlippingPositive ? -kinetic
                    : state.Mode[k] == ClutchMode.SlippingNegative ? kinetic : 0;
            }
            bool converged = false;
            for (int iteration = 0; iteration < ConstraintIterations; ++iteration)
            {
                if ((iteration & 15) == 0 && cancellation.IsCancellationRequested) return false;
                if (!Midpoint(old, mid, energy, combustion, cancellation)) return false;
                bool satisfied = true;
                if (_model.HasPumps)
                {
                    UpdateCapacities(state, old, inputs, pressure);
                    if (!ProjectHydraulicTorques(state, mid, true, ref satisfied)) return false;
                }
                for (int k = 0; k < Torque.Length; ++k)
                {
                    if (state.Mode[k] != ClutchMode.Locked) continue;
                    double limit = _staticCapacity[k];
                    double next = Clamp(Torque[k] - Slip(k, mid) / _diagonal[k], limit);
                    double change = next - Torque[k];
                    if (!Numeric.Finite(change)) return false;
                    if (Math.Abs(change * _diagonal[k]) > Tolerance(k, mid)) satisfied = false;
                    Torque[k] = next;
                    for (int j = 0; j < mid.Length; ++j) mid[j] += change * _response[k][j];
                }
                if (!satisfied) continue;
                // Recompute cylinder torque for the final force, then check the projected residual.
                if (!Midpoint(old, mid, energy, combustion, cancellation)) return false;
                if (_model.HasPumps)
                {
                    UpdateCapacities(state, old, inputs, pressure);
                    if (!ProjectHydraulicTorques(state, mid, false, ref satisfied)) return false;
                }
                for (int k = 0; k < Torque.Length; ++k)
                    if (state.Mode[k] == ClutchMode.Locked)
                    {
                        double limit = _staticCapacity[k];
                        double projected = Math.Abs(Torque[k]) < limit ? Slip(k, mid)
                            : (Clamp(Torque[k] - Slip(k, mid) / _diagonal[k], limit) - Torque[k]) * _diagonal[k];
                        if (!Numeric.Finite(projected) || Math.Abs(projected) > Tolerance(k, mid)) satisfied = false;
                    }
                if (satisfied) { converged = true; break; }
            }
            if (!converged) return false;
            bool changed = false;
            for (int k = 0; k < Torque.Length; ++k)
            {
                double slip = Slip(k, mid), tolerance = Tolerance(k, mid);
                if (!Numeric.Finite(slip) || !Numeric.Finite(Torque[k])) return false;
                double limit = _staticCapacity[k];
                if (state.Mode[k] == ClutchMode.Locked && Math.Abs(Torque[k]) == limit && Math.Abs(slip) > tolerance)
                {
                    state.Mode[k] = slip > 0 ? ClutchMode.SlippingPositive : ClutchMode.SlippingNegative;
                    changed = true;
                }
                else if (Math.Abs(Slip(k, old)) <= 16 * Tolerance(k, old) &&
                    ((state.Mode[k] == ClutchMode.SlippingPositive && slip < -tolerance) ||
                     (state.Mode[k] == ClutchMode.SlippingNegative && slip > tolerance)))
                {
                    // Another constraint can change the direction of a newly released clutch.
                    state.Mode[k] = ClutchMode.Locked; changed = true;
                }
            }
            if (!changed) return true;
        }
        return false;
    }

    private bool ProjectHydraulicTorques(ClutchState state, double[] mid, bool apply, ref bool satisfied)
    {
        for (int k = 0; k < Torque.Length; ++k)
        {
            if (!Numeric.Finite(_staticCapacity[k]) || !Numeric.Finite(_slidingCapacity[k])) return false;
            double next = state.Mode[k] == ClutchMode.Disengaged ? 0
                : state.Mode[k] == ClutchMode.SlippingPositive ? -_slidingCapacity[k]
                : state.Mode[k] == ClutchMode.SlippingNegative ? _slidingCapacity[k] : Clamp(Torque[k], _staticCapacity[k]);
            double change = next - Torque[k];
            if (!Numeric.Finite(change)) return false;
            if (Math.Abs(change * _diagonal[k]) > Tolerance(k, mid)) satisfied = false;
            if (!apply) continue;
            Torque[k] = next;
            for (int j = 0; j < mid.Length; ++j) mid[j] += change * _response[k][j];
        }
        return true;
    }

    internal bool Crosses(ClutchState state, double[] old, double[] mid)
    {
        for (int k = 0; k < Torque.Length; ++k)
        {
            if (state.Mode[k] is not (ClutchMode.SlippingPositive or ClutchMode.SlippingNegative)) continue;
            double start = Slip(k, old), end = 2 * Slip(k, mid) - start;
            // A newly released constraint starts at the root within numerical resolution.
            // Its rounding residue must not create a second, oppositely directed event.
            if (Math.Abs(start) <= 16 * Tolerance(k, old)) continue;
            if ((start > 0 && end < 0) || (start < 0 && end > 0)) return true;
        }
        return false;
    }

    internal bool PromoteEvents(ClutchState state, double[] x)
    {
        bool changed = false;
        for (int k = 0; k < Torque.Length; ++k)
            if (state.Mode[k] is ClutchMode.SlippingPositive or ClutchMode.SlippingNegative &&
                Math.Abs(Slip(k, x)) <= 16 * Tolerance(k, x))
            { state.Mode[k] = ClutchMode.Locked; changed = true; }
        return changed;
    }

    internal bool Accumulate(int k, ClutchState state, double duration, double[] mid, out double heat)
    {
        heat = -duration * Torque[k] * Slip(k, mid);
        double rounding = duration * Math.Abs(Torque[k]) * Tolerance(k, mid);
        if (!Numeric.Finite(heat) || heat < -2 * rounding) return false;
        if (heat < 0) heat = 0;
        state.Torque[k] += duration * Torque[k]; state.Power[k] += heat;
        Numeric.Accumulate(heat, ref state.Heat[k], ref state.HeatCorrection[k]);
        return true;
    }
}
