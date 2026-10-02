// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Immutable permanent rotational constraints, with rows normalized by their largest coefficient.</summary>
internal sealed class GearCoupling
{
    internal readonly int[] Components;
    internal readonly double[][] Rows;
    internal readonly double[] Scale, Phase;
    internal readonly GearProjection FullInterval;
    internal readonly CompiledModel Model;

    internal GearCoupling(CompiledModel model)
    {
        Model = model; Components = model.GearComponents;
        Rows = new double[Components.Length][]; Scale = new double[Components.Length]; Phase = new double[Components.Length];
        for (int k = 0; k < Components.Length; ++k)
        {
            var c = model.Components[Components[k]];
            bool doublePinion = c.Kind == ComponentKind.DoublePinionPlanetaryGear;
            bool carrierRelative = c.Kind == ComponentKind.CarrierGear;
            double scale = carrierRelative ? Math.Max(1, Math.Max(Math.Abs(c.P3), Math.Abs(c.P3 - 1))) : c.Kind == ComponentKind.IdealGear ? Math.Max(1, Math.Abs(c.P3)) : doublePinion ? c.P3 : 1 + c.P3;
            var row = Rows[k] = new double[model.DynamicCount]; Scale[k] = scale;
            row[model.Nodes[c.A].Index + 1] = 1 / scale;
            row[model.Nodes[c.B].Index + 1] = (c.Kind == ComponentKind.IdealGear || doublePinion || carrierRelative ? -c.P3 : c.P3) / scale;
            if (c.C >= 0) row[model.Nodes[c.C].Index + 1] = doublePinion || carrierRelative ? (c.P3 - 1) / scale : -1;
            double slip = 0, magnitude = 0;
            foreach (var n in model.Nodes)
                if (n.Domain == Domain.Rotational)
                {
                    double term = row[n.Index + 1] * n.Initial;
                    slip += term; magnitude += Math.Abs(term);
                    Phase[k] += row[n.Index + 1] * n.Position;
                }
            if (!Numeric.Finite(slip) || !Numeric.Finite(magnitude) || !Numeric.Finite(Phase[k]) ||
                Math.Abs(slip) > 64 * GearReference.Epsilon * magnitude)
                throw new ModelCompileException(DiagnosticCode.Connection, c.Id, "initial_speed",
                    "Initial rotor speeds must satisfy the ideal gear relation; use a clutch for a finite speed mismatch.");
        }
        FullInterval = new(this);
        if (!FullInterval.Prepare(model.Dt, model.Dynamics))
            throw new ModelCompileException(DiagnosticCode.Solver, 0, "gear.constraints",
                "Ideal gear constraints are redundant, singular or ill-conditioned. Remove dependent constraints and inspect inertia/ratio scales.");
    }

    internal double Dot(int k, double[] x, int offset = 1)
    {
        double value = 0;
        foreach (var n in Model.Nodes)
            if (n.Domain == Domain.Rotational) value += Rows[k][n.Index + 1] * x[n.Index + offset];
        return value;
    }

    internal bool Resolved(double[] x)
    {
        for (int k = 0; k < Rows.Length; ++k)
        {
            double speeds = 0, angles = Math.Abs(Phase[k]);
            foreach (var n in Model.Nodes)
                if (n.Domain == Domain.Rotational)
                {
                    speeds += Math.Abs(Rows[k][n.Index + 1] * x[n.Index + 1]);
                    angles += Math.Abs(Rows[k][n.Index + 1] * x[n.Index]);
                }
            double slip = Dot(k, x), error = Dot(k, x, 0) - Phase[k];
            if (!Numeric.Finite(slip * Scale[k]) || !Numeric.Finite(error * Scale[k]) ||
                Math.Abs(slip) > 2e-12 + 512 * GearReference.Epsilon * speeds ||
                Math.Abs(error) > 2e-10 + 1024 * GearReference.Epsilon * angles) return false;
        }
        return true;
    }
}

/// <summary>Schur-complement projection. Full-interval instances are immutable after compilation.</summary>
internal sealed class GearProjection
{
    private readonly GearCoupling _coupling;
    private readonly double[][] _response;
    private readonly double[,] _matrix;
    private readonly Factorization _factor;
    private readonly bool _refine;

    internal GearProjection(GearCoupling coupling)
    {
        _coupling = coupling;
        int count = coupling.Rows.Length;
        _response = Enumerable.Range(0, count).Select(_ => new double[coupling.Model.DynamicCount]).ToArray();
        _matrix = new double[count, count]; _factor = new(count);
        _refine = coupling.Model.Components.Any(c => c.Kind == ComponentKind.CarrierGear);
    }

    internal bool Prepare(double duration, Factorization dynamics)
    {
        for (int k = 0; k < _response.Length; ++k)
        {
            var response = _response[k]; Array.Clear(response, 0, response.Length);
            foreach (var n in _coupling.Model.Nodes)
                if (n.Domain == Domain.Rotational) response[n.Index + 1] = .5 * duration * _coupling.Rows[k][n.Index + 1] / n.Storage;
            if (!dynamics.Solve(response)) return false;
            for (int row = 0; row < _response.Length; ++row) _matrix[row, k] = _coupling.Dot(row, response);
        }
        return _factor.Refactor(_matrix);
    }

    // The caller owns the multiplier buffer; immutable factors never own mutable solve state.
    internal bool Project(double[] x, double[] multipliers, double[] correction, double[]? previous = null)
    {
        // Enforce G v_next = 0 with v_next = 2 v_mid - v_old. With exact
        // preceding constraints this is the ordinary zero midpoint target.
        for (int k = 0; k < multipliers.Length; ++k) multipliers[k] = (previous is null ? 0 : .5 * _coupling.Dot(k, previous)) - _coupling.Dot(k, x);
        if (!_factor.Solve(multipliers)) return false;
        for (int k = 0; k < multipliers.Length; ++k)
            for (int j = 0; j < x.Length; ++j) x[j] += _response[k][j] * multipliers[k];
        // Relative residual refinement also applies to small clutch force responses.
        // Factors remain immutable; the caller owns this scratch and all reactions.
        if (_refine)
            for (int iteration = 0; iteration < 3; ++iteration)
            {
                bool resolved = true;
                for (int k = 0; k < correction.Length; ++k)
                {
                    correction[k] = (previous is null ? 0 : .5 * _coupling.Dot(k, previous)) - _coupling.Dot(k, x);
                    double magnitude = 0;
                    foreach (var node in _coupling.Model.Nodes)
                        if (node.Domain == Domain.Rotational) magnitude += Math.Abs(_coupling.Rows[k][node.Index + 1] * x[node.Index + 1]);
                    if (Math.Abs(correction[k]) > 32 * GearReference.Epsilon * magnitude) resolved = false;
                }
                if (resolved) break;
                if (!_factor.Solve(correction)) return false;
                for (int k = 0; k < correction.Length; ++k)
                {
                    multipliers[k] += correction[k];
                    for (int j = 0; j < x.Length; ++j) x[j] += _response[k][j] * correction[k];
                }
            }
        for (int j = 0; j < x.Length; ++j) if (!Numeric.Finite(x[j])) return false;
        return true;
    }
}

/// <summary>All variable-interval projection and reaction workspace belongs to one simulation.</summary>
internal sealed class GearSolver
{
    private readonly GearCoupling _coupling;
    private readonly GearProjection _variable;
    private GearProjection _projection;
    private double _duration;
    private readonly double[] _correction;
    private readonly bool _endpointConstraint;
    internal readonly double[] Torque;

    internal GearSolver(GearCoupling coupling)
    {
        _coupling = coupling; _projection = coupling.FullInterval; _variable = new(coupling);
        _duration = coupling.Model.Dt; Torque = new double[coupling.Rows.Length];
        _correction = new double[coupling.Rows.Length];
        _endpointConstraint = coupling.Model.Components.Any(c => c.Kind == ComponentKind.CarrierGear);
    }

    internal bool Prepare(double duration, Factorization dynamics, bool force = false)
    {
        if (!force && duration == _duration) return true;
        _duration = 0;
        if (!force && duration == _coupling.Model.Dt) _projection = _coupling.FullInterval;
        else
        {
            if (!_variable.Prepare(duration, dynamics)) return false;
            _projection = _variable;
        }
        _duration = duration; return true;
    }

    internal bool Project(double[] x, double[] reactions) => _projection.Project(x, reactions, _correction);
    internal bool ProjectFree(double[] x, double[] previous) => _projection.Project(x, Torque, _correction, _endpointConstraint ? previous : null);
}
