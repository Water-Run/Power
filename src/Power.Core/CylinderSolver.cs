// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

internal sealed class CylinderCoupling
{
    internal readonly int[] Angles;
    internal readonly int[][] Components;
    internal readonly double[][] Response, GearReaction;

    internal CylinderCoupling(CompiledModel model)
    {
        int[] nodes = model.Components.Where(c => c.Kind is ComponentKind.SealedCylinder or ComponentKind.GasCylinder).Select(c => c.A).Distinct().OrderBy(i => i).ToArray();
        Angles = nodes.Select(i => model.Nodes[i].Index).ToArray();
        Components = new int[nodes.Length][]; Response = new double[nodes.Length][]; GearReaction = new double[nodes.Length][];
        for (int k = 0; k < nodes.Length; ++k)
        {
            Components[k] = Enumerable.Range(0, model.ComponentCount).Where(i => (model.Cylinders[i] is not null || model.GasCylinders[i] is not null) && model.Components[i].A == nodes[k]).ToArray();
            var response = new double[model.DynamicCount];
            response[Angles[k] + 1] = 0.5 * model.Dt / model.Nodes[nodes[k]].Storage;
            if (!model.Dynamics.Solve(response))
                throw new ModelCompileException(DiagnosticCode.Solver, 0, "cylinder.coupling", "Cylinder response overflows binary64.");
            GearReaction[k] = new double[model.GearComponents.Length];
            if (model.Gears is { } gears && !gears.FullInterval.Project(response, GearReaction[k]))
                throw new ModelCompileException(DiagnosticCode.Solver, 0, "cylinder.gears", "Constrained cylinder response exceeds the supported numerical range.");
            Response[k] = response;
        }
    }
}

/// <summary>Bounded discrete-gradient solve; all mutable workspace belongs to one simulation.</summary>
internal sealed class CylinderSolver : MechanicalSolver
{
    internal const double MaxAngleStepRadians = 0.25;
    private const int MaxIterations = 16, MaxLineSearch = 10;
    private const double Tolerance = 2e-14;
    private readonly CompiledModel _model;
    private CombustionSolver? _combustion;
    private readonly CylinderCoupling _coupling;
    private readonly double[] _delta, _prediction, _torque, _derivative, _residual, _trial, _trialResidual, _correction;
    private readonly double[,] _jacobian;
    private double[][] _response, _gearReaction;
    private readonly double[][]? _variableResponse, _variableGearReaction;

    internal CylinderSolver(CompiledModel model)
    {
        _model = model; _coupling = model.CylinderCoupling!;
        int n = _coupling.Angles.Length;
        _delta = new double[n]; _prediction = new double[n]; _torque = new double[n]; _derivative = new double[n];
        _residual = new double[n]; _trial = new double[n]; _trialResidual = new double[n]; _correction = new double[n];
        _jacobian = new double[n, n];
        _response = _coupling.Response; _gearReaction = _coupling.GearReaction;
        if (model.HasClutches)
        {
            _variableResponse = Enumerable.Range(0, n).Select(_ => new double[model.DynamicCount]).ToArray();
            _variableGearReaction = Enumerable.Range(0, n).Select(_ => new double[model.GearComponents.Length]).ToArray();
        }
    }

    internal override bool SetInterval(double duration, Factorization dynamics, GearSolver? gears)
    {
        if (duration == _model.Dt) { _response = _coupling.Response; _gearReaction = _coupling.GearReaction; return true; }
        _response = _variableResponse!; _gearReaction = _variableGearReaction!;
        for (int k = 0; k < _response.Length; ++k)
        {
            Array.Clear(_response[k], 0, _response[k].Length);
            var crank = _model.Nodes[_model.Components[_coupling.Components[k][0]].A];
            _response[k][crank.Index + 1] = .5 * duration / crank.Storage;
            if (!dynamics.Solve(_response[k]) || (gears is not null && !gears.Project(_response[k], _gearReaction[k]))) return false;
        }
        return true;
    }

    internal override void AddGearReactions(double[] reactions)
    {
        for (int k = 0; k < _gearReaction.Length; ++k)
            for (int j = 0; j < reactions.Length; ++j) reactions[j] += _gearReaction[k][j] * _torque[k];
    }

    private double Torque(int k, double angle, double delta, double[] energy)
    {
        double torque = 0;
        foreach (int i in _coupling.Components[k])
        {
            if (_model.Cylinders[i] is { } sealedCylinder) torque += sealedCylinder.DiscreteTorque(angle, delta);
            else
            {
                int gas = _model.Nodes[_model.Components[i].B].Index;
                torque += _model.GasCylinders[i]!.DiscreteTorque(angle, delta, energy[gas] + .5 * (_combustion?.PreviewHeat(gas, angle + delta) ?? 0), _model.Gas!.Gases[gas].Gamma);
            }
        }
        return torque;
    }

    private double Residual(double[] old, double[] delta, double[] residual, double[] energy)
    {
        for (int k = 0; k < delta.Length; ++k)
        {
            if (!Numeric.Finite(delta[k]) || Math.Abs(delta[k]) > MaxAngleStepRadians) return double.PositiveInfinity;
            _torque[k] = Torque(k, old[_coupling.Angles[k]], delta[k], energy);
            if (!Numeric.Finite(_torque[k])) return double.PositiveInfinity;
        }
        double norm = 0;
        for (int row = 0; row < delta.Length; ++row)
        {
            double value = delta[row] - _prediction[row];
            for (int col = 0; col < delta.Length; ++col) value -= 2 * _response[col][_coupling.Angles[row]] * _torque[col];
            residual[row] = value;
            if (!Numeric.Finite(value)) return double.PositiveInfinity;
            norm = Math.Max(norm, Math.Abs(value));
        }
        return norm;
    }

    internal override bool Solve(double[] old, double[] midpoint, double[] energy, CombustionSolver? combustion = null, CancellationToken cancellation = default)
    {
        _combustion = combustion;
        int n = _delta.Length;
        for (int k = 0; k < n; ++k) _delta[k] = _prediction[k] = 2 * (midpoint[_coupling.Angles[k]] - old[_coupling.Angles[k]]);
        for (int iteration = 0; iteration < MaxIterations; ++iteration)
        {
            if (cancellation.IsCancellationRequested) return false;
            double norm = Residual(old, _delta, _residual, energy);
            if (!Numeric.Finite(norm)) return false;
            if (norm <= Tolerance)
            {
                for (int j = 0; j < midpoint.Length; ++j)
                    for (int k = 0; k < n; ++k) midpoint[j] += _response[k][j] * _torque[k];
                for (int k = 0; k < n; ++k)
                {
                    double actual = (2 * midpoint[_coupling.Angles[k]] - old[_coupling.Angles[k]]) - old[_coupling.Angles[k]];
                    double rounding = 8 * 2.2204460492503131e-16 * Math.Abs(old[_coupling.Angles[k]]);
                    if (!Numeric.Finite(actual) || Math.Abs(actual) > MaxAngleStepRadians || Math.Abs(actual - _delta[k]) > 4 * Tolerance + rounding)
                        return false;
                }
                return true;
            }
            for (int k = 0; k < n; ++k)
            {
                const double epsilon = 1e-6;
                double angle = old[_coupling.Angles[k]];
                _derivative[k] = (Torque(k, angle, _delta[k] + epsilon, energy) - Torque(k, angle, _delta[k] - epsilon, energy)) / (2 * epsilon);
            }
            for (int row = 0; row < n; ++row)
            {
                _correction[row] = -_residual[row];
                for (int col = 0; col < n; ++col)
                    _jacobian[row, col] = (row == col ? 1 : 0) - 2 * _response[col][_coupling.Angles[row]] * _derivative[col];
            }
            if (!SolveCorrection()) return false;
            bool accepted = false;
            double scale = 1;
            for (int search = 0; search < MaxLineSearch; ++search, scale *= 0.5)
            {
                for (int k = 0; k < n; ++k) _trial[k] = _delta[k] + scale * _correction[k];
                if (Residual(old, _trial, _trialResidual, energy) < norm)
                {
                    Array.Copy(_trial, _delta, n); accepted = true; break;
                }
            }
            if (!accepted) return false;
        }
        return false;
    }

    private bool SolveCorrection()
    {
        int n = _delta.Length;
        for (int k = 0; k < n; ++k)
        {
            int pivot = k;
            for (int row = k + 1; row < n; ++row)
                if (Math.Abs(_jacobian[row, k]) > Math.Abs(_jacobian[pivot, k])) pivot = row;
            if (!Numeric.Finite(_jacobian[pivot, k]) || Math.Abs(_jacobian[pivot, k]) < 1e-14) return false;
            for (int col = k; col < n; ++col)
                (_jacobian[k, col], _jacobian[pivot, col]) = (_jacobian[pivot, col], _jacobian[k, col]);
            (_correction[k], _correction[pivot]) = (_correction[pivot], _correction[k]);
            for (int row = k + 1; row < n; ++row)
            {
                double factor = _jacobian[row, k] / _jacobian[k, k];
                for (int col = k + 1; col < n; ++col) _jacobian[row, col] -= factor * _jacobian[k, col];
                _correction[row] -= factor * _correction[k];
            }
        }
        for (int row = n - 1; row >= 0; --row)
        {
            for (int col = row + 1; col < n; ++col) _correction[row] -= _jacobian[row, col] * _correction[col];
            _correction[row] /= _jacobian[row, row];
            if (!Numeric.Finite(_correction[row])) return false;
        }
        return true;
    }
}
