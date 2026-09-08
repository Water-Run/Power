// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

internal sealed class CylinderCoupling
{
    internal readonly int[] Angles;
    internal readonly int[][] Components;
    internal readonly double[][] Response;

    internal CylinderCoupling(CompiledModel model)
    {
        int[] nodes = model.Components.Where(c => c.Kind == ComponentKind.SealedCylinder).Select(c => c.A).Distinct().OrderBy(i => i).ToArray();
        Angles = nodes.Select(i => model.Nodes[i].Index).ToArray();
        Components = new int[nodes.Length][]; Response = new double[nodes.Length][];
        for (int k = 0; k < nodes.Length; ++k)
        {
            Components[k] = Enumerable.Range(0, model.ComponentCount).Where(i => model.Cylinders[i] is not null && model.Components[i].A == nodes[k]).ToArray();
            var response = new double[model.DynamicCount];
            response[Angles[k] + 1] = 0.5 * model.Dt / model.Nodes[nodes[k]].Storage;
            if (!model.Dynamics.Solve(response))
                throw new ModelCompileException(DiagnosticCode.Solver, 0, "cylinder.coupling", "Cylinder response overflows binary64.");
            Response[k] = response;
        }
    }
}

/// <summary>Bounded discrete-gradient solve; all mutable workspace belongs to one simulation.</summary>
internal sealed class CylinderSolver
{
    internal const double MaxAngleStepRadians = 0.25;
    private const int MaxIterations = 16, MaxLineSearch = 10;
    private const double Tolerance = 2e-14;
    private readonly CompiledModel _model;
    private readonly CylinderCoupling _coupling;
    private readonly double[] _delta, _prediction, _torque, _derivative, _residual, _trial, _trialResidual, _correction;
    private readonly double[,] _jacobian;

    internal CylinderSolver(CompiledModel model)
    {
        _model = model; _coupling = model.CylinderCoupling!;
        int n = _coupling.Angles.Length;
        _delta = new double[n]; _prediction = new double[n]; _torque = new double[n]; _derivative = new double[n];
        _residual = new double[n]; _trial = new double[n]; _trialResidual = new double[n]; _correction = new double[n];
        _jacobian = new double[n, n];
    }

    private double Torque(int k, double angle, double delta)
    {
        double torque = 0;
        foreach (int i in _coupling.Components[k]) torque += _model.Cylinders[i]!.DiscreteTorque(angle, delta);
        return torque;
    }

    private double Residual(double[] old, double[] delta, double[] residual)
    {
        for (int k = 0; k < delta.Length; ++k)
        {
            if (!Numeric.Finite(delta[k]) || Math.Abs(delta[k]) > MaxAngleStepRadians) return double.PositiveInfinity;
            _torque[k] = Torque(k, old[_coupling.Angles[k]], delta[k]);
            if (!Numeric.Finite(_torque[k])) return double.PositiveInfinity;
        }
        double norm = 0;
        for (int row = 0; row < delta.Length; ++row)
        {
            double value = delta[row] - _prediction[row];
            for (int col = 0; col < delta.Length; ++col) value -= 2 * _coupling.Response[col][_coupling.Angles[row]] * _torque[col];
            residual[row] = value;
            if (!Numeric.Finite(value)) return double.PositiveInfinity;
            norm = Math.Max(norm, Math.Abs(value));
        }
        return norm;
    }

    internal bool Solve(double[] old, double[] midpoint)
    {
        int n = _delta.Length;
        for (int k = 0; k < n; ++k) _delta[k] = _prediction[k] = 2 * (midpoint[_coupling.Angles[k]] - old[_coupling.Angles[k]]);
        for (int iteration = 0; iteration < MaxIterations; ++iteration)
        {
            double norm = Residual(old, _delta, _residual);
            if (!Numeric.Finite(norm)) return false;
            if (norm <= Tolerance)
            {
                for (int j = 0; j < midpoint.Length; ++j)
                    for (int k = 0; k < n; ++k) midpoint[j] += _coupling.Response[k][j] * _torque[k];
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
                _derivative[k] = (Torque(k, angle, _delta[k] + epsilon) - Torque(k, angle, _delta[k] - epsilon)) / (2 * epsilon);
            }
            for (int row = 0; row < n; ++row)
            {
                _correction[row] = -_residual[row];
                for (int col = 0; col < n; ++col)
                    _jacobian[row, col] = (row == col ? 1 : 0) - 2 * _coupling.Response[col][_coupling.Angles[row]] * _derivative[col];
            }
            if (!SolveCorrection()) return false;
            bool accepted = false;
            double scale = 1;
            for (int search = 0; search < MaxLineSearch; ++search, scale *= 0.5)
            {
                for (int k = 0; k < n; ++k) _trial[k] = _delta[k] + scale * _correction[k];
                if (Residual(old, _trial, _trialResidual) < norm)
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
