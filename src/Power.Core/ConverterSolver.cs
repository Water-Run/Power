// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

internal abstract class MechanicalSolver
{
    internal abstract bool SetInterval(double duration, Factorization dynamics, GearSolver? gears);
    internal abstract bool Solve(double[] old, double[] midpoint, double[] energy, CombustionSolver? combustion = null, CancellationToken cancellation = default);
    internal abstract void AddGearReactions(double[] reactions);
}

internal sealed class ConverterState(int count)
{
    internal readonly double[] PumpTorque = new double[count], TurbineTorque = new double[count], Power = new double[count];
    internal readonly double[] Heat = new double[count], Correction = new double[count];
    internal void CopyFrom(ConverterState other)
    {
        Array.Copy(other.PumpTorque, PumpTorque, PumpTorque.Length); Array.Copy(other.TurbineTorque, TurbineTorque, TurbineTorque.Length);
        Array.Copy(other.Power, Power, Power.Length); Array.Copy(other.Heat, Heat, Heat.Length); Array.Copy(other.Correction, Correction, Correction.Length);
    }
    internal void BeginTick()
    { Array.Clear(PumpTorque, 0, PumpTorque.Length); Array.Clear(TurbineTorque, 0, TurbineTorque.Length); Array.Clear(Power, 0, Power.Length); }
    internal bool EndTick(double duration)
    {
        for (int k = 0; k < Heat.Length; ++k)
        { PumpTorque[k] /= duration; TurbineTorque[k] /= duration; Power[k] /= duration; }
        return Finite();
    }
    internal bool Finite()
    {
        for (int k = 0; k < Heat.Length; ++k)
            if (!GearReference.Finite(PumpTorque[k], TurbineTorque[k], -PumpTorque[k] - TurbineTorque[k], Power[k]) ||
                !Numeric.Finite(Heat[k]) || !Numeric.Finite(Correction[k])) return false;
        return true;
    }
    internal ulong Hash(ulong hash)
    {
        for (int k = 0; k < Heat.Length; ++k)
        {
            hash = Numeric.Hash(hash, PumpTorque[k]); hash = Numeric.Hash(hash, TurbineTorque[k]);
            hash = Numeric.Hash(hash, Power[k]); hash = Numeric.Hash(hash, Heat[k]); hash = Numeric.Hash(hash, Correction[k]);
        }
        return hash;
    }
}

/// <summary>Compiled unit-torque responses for joint cylinder, converter and hydraulic pump coupling.</summary>
internal sealed class ConverterCoupling
{
    internal readonly int Cranks, PumpOffset;
    internal readonly int[] Nodes, Variables;
    internal readonly double[][] Response, GearReaction;
    internal ConverterCoupling(CompiledModel model)
    {
        Cranks = model.CylinderCoupling?.Angles.Length ?? 0;
        PumpOffset = Cranks + 2 * model.ConverterComponents.Length;
        int count = PumpOffset + model.PumpComponents.Length;
        Nodes = new int[count]; Variables = new int[count]; Response = new double[count][]; GearReaction = new double[count][];
        for (int k = 0; k < Cranks; ++k) Nodes[k] = model.Components[model.CylinderCoupling!.Components[k][0]].A;
        for (int k = 0; k < model.ConverterComponents.Length; ++k)
        {
            var c = model.Components[model.ConverterComponents[k]];
            Nodes[Cranks + 2 * k] = c.A; Nodes[Cranks + 2 * k + 1] = c.B;
        }
        for (int k = 0; k < model.PumpComponents.Length; ++k) Nodes[PumpOffset + k] = model.Components[model.PumpComponents[k]].A;
        for (int k = 0; k < count; ++k)
        {
            var node = model.Nodes[Nodes[k]];
            Variables[k] = node.Index + (k < Cranks ? 0 : 1);
            var response = Response[k] = new double[model.DynamicCount];
            var reactions = GearReaction[k] = new double[model.GearComponents.Length];
            response[node.Index + 1] = .5 * model.Dt / node.Storage;
            if (!model.Dynamics.Solve(response) || (model.Gears is not null && !model.Gears.FullInterval.Project(response, reactions)))
                throw new ModelCompileException(DiagnosticCode.Solver, 0, "converter.coupling", "Converter/cylinder force response exceeds the supported numerical range.");
        }
    }
}

/// <summary>Joint midpoint-speed, hydraulic-pressure and discrete-cylinder-work Newton solve. All workspace is simulation-owned.</summary>
internal sealed class ConverterSolver : MechanicalSolver
{
    internal const int MaxIterations = 24, MaxLineSearch = 12;
    private readonly CompiledModel _model;
    private readonly ConverterCoupling _coupling;
    private readonly double[] _value, _prediction, _force, _residual, _trial, _trialResidual, _correction;
    private readonly double[,] _derivative, _jacobian;
    private readonly Factorization _factor;
    private readonly double[][] _variableResponse, _variableReactions;
    private double[][] _response, _reactions;
    private CombustionSolver? _combustion;
    private readonly HydraulicSolver? _hydraulics;
    private HydraulicState? _hydraulicState;
    private double[]? _inputs;
    private double _duration;
    internal void PrepareHydraulics(HydraulicState state, double[] inputs, double duration)
    { _hydraulicState = state; _inputs = inputs; _duration = duration; }


    internal ConverterSolver(CompiledModel model, HydraulicSolver? hydraulics = null)
    {
        _model = model; _coupling = model.ConverterCoupling!; _hydraulics = model.HasPumps ? hydraulics : null;
        int count = _coupling.Nodes.Length + (model.HasPumps ? model.HydraulicCount : 0);
        _value = new double[count]; _prediction = new double[count]; _force = new double[_coupling.Nodes.Length]; _residual = new double[count];
        _trial = new double[count]; _trialResidual = new double[count]; _correction = new double[count];
        _derivative = new double[_force.Length, count]; _jacobian = new double[count, count]; _factor = new(count);
        _response = _coupling.Response; _reactions = _coupling.GearReaction;
        _variableResponse = Enumerable.Range(0, _force.Length).Select(_ => new double[model.DynamicCount]).ToArray();
        _variableReactions = Enumerable.Range(0, _force.Length).Select(_ => new double[model.GearComponents.Length]).ToArray();
    }

    internal override bool SetInterval(double duration, Factorization dynamics, GearSolver? gears)
    {
        if (duration == _model.Dt) { _response = _coupling.Response; _reactions = _coupling.GearReaction; return true; }
        _response = _variableResponse; _reactions = _variableReactions;
        for (int k = 0; k < _response.Length; ++k)
        {
            Array.Clear(_response[k], 0, _response[k].Length);
            var node = _model.Nodes[_coupling.Nodes[k]];
            _response[k][node.Index + 1] = .5 * duration / node.Storage;
            if (!dynamics.Solve(_response[k]) || (gears is not null && !gears.Project(_response[k], _reactions[k]))) return false;
        }
        return true;
    }

    private double CylinderTorque(int k, double angle, double delta, double[] energy)
    {
        double torque = 0;
        foreach (int i in _model.CylinderCoupling!.Components[k])
            if (_model.Cylinders[i] is { } sealedCylinder) torque += sealedCylinder.DiscreteTorque(angle, delta);
            else
            {
                int gas = _model.Nodes[_model.Components[i].B].Index;
                torque += _model.GasCylinders[i]!.DiscreteTorque(angle, delta,
                    energy[gas] + .5 * (_combustion?.PreviewHeat(gas, angle + delta) ?? 0), _model.Gas!.Gases[gas].Gamma);
            }
        return torque;
    }

    private double Tolerance(int k, double value) => k < _coupling.Cranks ? 2e-14
        : 2e-12 + 64 * GearReference.Epsilon * (Math.Abs(value) + Math.Abs(_prediction[k]));

    private double Residual(double[] old, double[] values, double[] residual, double[] energy, bool derivatives)
    {
        int n = values.Length;
        if (derivatives) Array.Clear(_derivative, 0, _derivative.Length);
        for (int k = 0; k < _coupling.Cranks; ++k)
        {
            double delta = values[k], angle = old[_coupling.Variables[k]];
            if (!Numeric.Finite(delta) || Math.Abs(delta) > CylinderSolver.MaxAngleStepRadians) return double.PositiveInfinity;
            _force[k] = CylinderTorque(k, angle, delta, energy);
            if (derivatives) _derivative[k, k] = (CylinderTorque(k, angle, delta + 1e-6, energy) - CylinderTorque(k, angle, delta - 1e-6, energy)) / 2e-6;
        }
        for (int k = 0; k < _model.ConverterComponents.Length; ++k)
        {
            int a = _coupling.Cranks + 2 * k, b = a + 1;
            var law = _model.Converters[_model.ConverterComponents[k]]!;
            if (!law.Reaction(values[a], values[b], out var r, out double aa, out double ab, out double ba, out double bb)) return double.PositiveInfinity;
            _force[a] = r.PumpTorqueNewtonMeters; _force[b] = r.TurbineTorqueNewtonMeters;
            if (derivatives) { _derivative[a, a] = aa; _derivative[a, b] = ab; _derivative[b, a] = ba; _derivative[b, b] = bb; }
        }
        int mechanical = _force.Length;
        for (int k = 0; k < _model.PumpComponents.Length; ++k)
        {
            var c = _model.Components[_model.PumpComponents[k]];
            int row = _coupling.PumpOffset + k, outlet = mechanical + _model.Nodes[c.B].Index;
            int inlet = c.C < 0 ? -1 : mechanical + _model.Nodes[c.C].Index;
            _force[row] = -c.P0 * (values[outlet] - (inlet < 0 ? c.P2 : values[inlet]));
            if (derivatives) { _derivative[row, outlet] = -c.P0; if (inlet >= 0) _derivative[row, inlet] = c.P0; }
        }
        double norm = _hydraulics?.CoupledResidual(_hydraulicState!.Pressure, values, mechanical, _coupling.PumpOffset,
            _inputs!, _duration, residual, derivatives ? _jacobian : null) ?? 0;
        for (int row = 0; row < mechanical; ++row)
        {
            if (!Numeric.Finite(_force[row])) return double.PositiveInfinity;
            double value = values[row] - _prediction[row], scale = row < _coupling.Cranks ? 2 : 1;
            for (int col = 0; col < mechanical; ++col) value -= scale * _response[col][_coupling.Variables[row]] * _force[col];
            if (!Numeric.Finite(value)) return double.PositiveInfinity;
            residual[row] = value; norm = Math.Max(norm, Math.Abs(value) / Tolerance(row, values[row]));
        }
        return norm;
    }

    internal override bool Solve(double[] old, double[] midpoint, double[] energy, CombustionSolver? combustion = null, CancellationToken cancellation = default)
    {
        _combustion = combustion; int n = _value.Length;
        int mechanical = _force.Length;
        for (int k = 0; k < mechanical; ++k)
        {
            int index = _coupling.Variables[k];
            _value[k] = _prediction[k] = k < _coupling.Cranks ? 2 * (midpoint[index] - old[index]) : midpoint[index];
        }
        if (_hydraulics is not null) Array.Copy(_hydraulicState!.Pressure, 0, _value, mechanical, _model.HydraulicCount);
        for (int iteration = 0; iteration < MaxIterations; ++iteration)
        {
            if (cancellation.IsCancellationRequested) return false;
            double norm = Residual(old, _value, _residual, energy, true);
            if (!Numeric.Finite(norm)) return false;
            if (norm <= 1)
            {
                for (int j = 0; j < midpoint.Length; ++j)
                    for (int k = 0; k < mechanical; ++k) midpoint[j] += _response[k][j] * _force[k];
                for (int k = 0; k < mechanical; ++k)
                {
                    int index = _coupling.Variables[k];
                    double actual = k < _coupling.Cranks ? (2 * midpoint[index] - old[index]) - old[index] : midpoint[index];
                    double rounding = 16 * GearReference.Epsilon * Math.Abs(old[index]);
                    if (!Numeric.Finite(actual) || Math.Abs(actual - _value[k]) > 4 * Tolerance(k, _value[k]) + rounding ||
                        (k < _coupling.Cranks && Math.Abs(actual) > CylinderSolver.MaxAngleStepRadians)) return false;
                }
                _hydraulics?.PreviewPressure();
                return true;
            }
            for (int row = 0; row < n; ++row)
            {
                _correction[row] = -_residual[row];
                if (row >= mechanical) continue;
                double scale = row < _coupling.Cranks ? 2 : 1;
                for (int col = 0; col < n; ++col)
                {
                    double value = row == col ? 1 : 0;
                    for (int k = 0; k < mechanical; ++k) value -= scale * _response[k][_coupling.Variables[row]] * _derivative[k, col];
                    _jacobian[row, col] = value;
                }
            }
            if (!_factor.Refactor(_jacobian) || !_factor.Solve(_correction)) return false;
            bool accepted = false; double fraction = 1;
            for (int search = 0; search < MaxLineSearch; ++search, fraction *= .5)
            {
                for (int k = 0; k < n; ++k) _trial[k] = _value[k] + fraction * _correction[k];
                if (Residual(old, _trial, _trialResidual, energy, false) < norm)
                { Array.Copy(_trial, _value, n); accepted = true; break; }
            }
            if (!accepted) return false;
        }
        return false;
    }

    internal override void AddGearReactions(double[] reactions)
    {
        for (int k = 0; k < _force.Length; ++k)
            for (int j = 0; j < reactions.Length; ++j) reactions[j] += _reactions[k][j] * _force[k];
    }

    internal bool Accumulate(int k, ConverterState state, double duration, double[] mid, out double heat)
    {
        int a = _coupling.Cranks + 2 * k, b = a + 1;
        double tp = _force[a], tt = _force[b], wp = mid[_coupling.Variables[a]], wt = mid[_coupling.Variables[b]];
        heat = -duration * (tp * wp + tt * wt);
        double tolerance = duration * (Math.Abs(tp) + Math.Abs(tt)) * (2e-12 + 64 * GearReference.Epsilon * (Math.Abs(wp) + Math.Abs(wt)));
        if (!Numeric.Finite(heat) || heat < -2 * tolerance) return false;
        if (heat < 0) heat = 0;
        state.PumpTorque[k] += duration * tp; state.TurbineTorque[k] += duration * tt; state.Power[k] += heat;
        Numeric.Accumulate(heat, ref state.Heat[k], ref state.Correction[k]);
        return true;
    }
}
