// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Independent state. Successful stepping and snapshot reads allocate no managed memory.</summary>
public sealed class Simulation
{
    private sealed class State(int dynamics, int thermal, int components)
    {
        internal readonly double[] X = new double[dynamics], Temperature = new double[thermal], Inputs = new double[components];
        internal ulong Time;
        internal double InitialEnergy, Work, WorkCorrection, Heat, HeatCorrection;
        internal void CopyFrom(State other)
        {
            Array.Copy(other.X, X, X.Length); Array.Copy(other.Temperature, Temperature, Temperature.Length);
            Array.Copy(other.Inputs, Inputs, Inputs.Length);
            Time = other.Time; InitialEnergy = other.InitialEnergy;
            Work = other.Work; WorkCorrection = other.WorkCorrection;
            Heat = other.Heat; HeatCorrection = other.HeatCorrection;
        }
    }

    private readonly CompiledModel _model;
    private State _state, _scratch;
    private readonly double[] _mid, _temperature, _inputCandidate;
    private readonly bool[] _inputSeen;
    private readonly CylinderSolver? _cylinderSolver;
    private int _busy;
    public CompiledModel Model => _model;

    internal Simulation(CompiledModel model)
    {
        _model = model;
        if (model.CylinderCoupling is not null) _cylinderSolver = new(model);
        _state = new(model.DynamicCount, model.ThermalCount, model.ComponentCount);
        _scratch = new(model.DynamicCount, model.ThermalCount, model.ComponentCount);
        _mid = new double[model.DynamicCount]; _temperature = new double[model.ThermalCount];
        _inputCandidate = new double[model.ComponentCount]; _inputSeen = new bool[model.ComponentCount];
        foreach (var n in model.Nodes)
        {
            if (n.Domain == Domain.Rotational) { _state.X[n.Index] = n.Position; _state.X[n.Index + 1] = n.Initial; }
            else _state.Temperature[n.Index] = n.Initial;
        }
        for (int i = 0; i < model.ComponentCount; ++i)
        {
            var c = model.Components[i];
            _state.Inputs[i] = c.InitialInput;
            if (c.Kind == ComponentKind.DcMotor) _state.X[c.Index] = c.P3;
        }
        _state.InitialEnergy = Energy(_state);
        if (!Numeric.Finite(_state.InitialEnergy) || !ObservablesFinite(_state))
            throw new ModelCompileException(DiagnosticCode.Solver, 0, "initial", "Initial energy or output overflows binary64.");
    }

    private bool Enter() => Interlocked.CompareExchange(ref _busy, 1, 0) == 0;
    private void Exit() => Volatile.Write(ref _busy, 0);

    public SimulationStatus SubmitInputs(ReadOnlySpan<Scalar> values)
    {
        if (!Enter()) return SimulationStatus.Busy;
        try
        {
            if (values.Length > _model.ComponentCount) return SimulationStatus.InvalidInput;
            Array.Copy(_state.Inputs, _inputCandidate, _inputCandidate.Length);
            Array.Clear(_inputSeen, 0, _inputSeen.Length);
            foreach (var v in values)
            {
                if (!Numeric.Finite(v.Value)) return SimulationStatus.InvalidInput;
                if (!_model.InputIndices.TryGetValue(v.Channel, out int index)) return SimulationStatus.UnknownChannel;
                if (_inputSeen[index]) return SimulationStatus.InvalidInput;
                _inputSeen[index] = true;
                _inputCandidate[index] = v.Value == 0 ? 0 : v.Value;
            }
            Array.Copy(_inputCandidate, _state.Inputs, _inputCandidate.Length);
            return SimulationStatus.Ok;
        }
        finally { Exit(); }
    }

    /// <summary>A whole multi-tick call commits atomically. At most one million ticks per call.</summary>
    public SimulationStatus Step(ulong deltaNanoseconds, CancellationToken cancellationToken = default) =>
        Step(deltaNanoseconds, ReadOnlySpan<ScheduledInput>.Empty, cancellationToken);

    /// <summary>Apply ordered input events on exact absolute tick boundaries, including the end boundary. The entire call is atomic.</summary>
    public SimulationStatus Step(ulong deltaNanoseconds, ReadOnlySpan<ScheduledInput> inputs, CancellationToken cancellationToken = default)
    {
        if (!Enter()) return SimulationStatus.Busy;
        try
        {
            ulong step = _model.StepNanoseconds;
            if (deltaNanoseconds == 0 || deltaNanoseconds % step != 0 || deltaNanoseconds / step > 1_000_000 ||
                ulong.MaxValue - _state.Time < deltaNanoseconds) return SimulationStatus.InvalidTimeStep;
            if (inputs.Length > 65_536) return SimulationStatus.InvalidInput;
            ulong end = _state.Time + deltaNanoseconds, previous = _state.Time;
            Array.Clear(_inputSeen, 0, _inputSeen.Length);
            foreach (var input in inputs)
            {
                if (input.TimeNanoseconds < previous || input.TimeNanoseconds > end ||
                    (input.TimeNanoseconds - _state.Time) % step != 0 || !Numeric.Finite(input.Value))
                    return SimulationStatus.InvalidInput;
                if (!_model.InputIndices.TryGetValue(input.Channel, out int index)) return SimulationStatus.UnknownChannel;
                if (input.TimeNanoseconds != previous) Array.Clear(_inputSeen, 0, _inputSeen.Length);
                if (_inputSeen[index]) return SimulationStatus.InvalidInput;
                _inputSeen[index] = true;
                previous = input.TimeNanoseconds;
            }
            _scratch.CopyFrom(_state);
            int nextInput = 0;
            ApplyScheduledInputs(_scratch, inputs, ref nextInput);
            for (ulong i = 0, count = deltaNanoseconds / step; i < count; ++i)
            {
                if ((i & 255) == 0 && cancellationToken.IsCancellationRequested) return SimulationStatus.Cancelled;
                if (!Tick(_scratch)) return SimulationStatus.NumericalFailure;
                ApplyScheduledInputs(_scratch, inputs, ref nextInput);
            }
            (_state, _scratch) = (_scratch, _state);
            return SimulationStatus.Ok;
        }
        finally { Exit(); }
    }

    private void ApplyScheduledInputs(State state, ReadOnlySpan<ScheduledInput> inputs, ref int next)
    {
        while (next < inputs.Length && inputs[next].TimeNanoseconds == state.Time)
        {
            var input = inputs[next++];
            state.Inputs[_model.InputIndices[input.Channel]] = input.Value == 0 ? 0 : input.Value;
        }
    }

    /// <summary>Branch an experiment from this exact state, sharing only immutable model data.</summary>
    public Simulation Fork()
    {
        if (!Enter()) throw new InvalidOperationException("Simulation is busy on another thread.");
        try
        {
            var fork = new Simulation(_model);
            fork._state.CopyFrom(_state);
            return fork;
        }
        finally { Exit(); }
    }

    private bool Tick(State s)
    {
        double dt = _model.Dt, work = 0, heat = 0;
        for (int i = 0; i < _mid.Length; ++i) _mid[i] = s.X[i] + 0.5 * dt * _model.ConstantForce[i];
        for (int i = 0; i < _model.ComponentCount; ++i)
        {
            var c = _model.Components[i];
            if (c.Kind == ComponentKind.TorqueSource)
            {
                var n = _model.Nodes[c.A];
                _mid[n.Index + 1] += 0.5 * dt * s.Inputs[i] / n.Storage;
            }
            else if (c.Kind == ComponentKind.DcMotor) _mid[c.Index] += 0.5 * dt * s.Inputs[i] / c.P1;
        }
        if (!_model.Dynamics.Solve(_mid)) return false;
        if (_cylinderSolver is not null && !_cylinderSolver.Solve(s.X, _mid)) return false;
        foreach (var n in _model.Nodes)
            if (n.Domain == Domain.Thermal)
                _temperature[n.Index] = n.Storage * s.Temperature[n.Index] + _model.AmbientForce[n.Index];
        for (int i = 0; i < _model.ComponentCount; ++i)
        {
            var c = _model.Components[i];
            double loss = 0;
            if (c.Kind == ComponentKind.Shaft)
            {
                double slip = Relative(c, _mid, 1);
                loss = dt * c.P1 * slip * slip;
            }
            else if (c.Kind == ComponentKind.DcMotor)
            {
                double current = _mid[c.Index];
                loss = dt * c.P0 * current * current;
                work += dt * s.Inputs[i] * current;
            }
            else if (c.Kind == ComponentKind.TorqueSource) work += dt * s.Inputs[i] * _mid[_model.Nodes[c.A].Index + 1];
            else if (c.Kind == ComponentKind.SealedCylinder)
            {
                int angle = _model.Nodes[c.A].Index;
                work += _model.Cylinders[i]!.BackPressureWork(s.X[angle], (2 * _mid[angle] - s.X[angle]) - s.X[angle]);
            }
            if (c.Heat < 0) heat += loss;
            else _temperature[_model.Nodes[c.Heat].Index] += loss;
        }
        if (!_model.Thermal.Solve(_temperature)) return false;
        foreach (var c in _model.Components)
            if (c.Kind == ComponentKind.ThermalLink && c.B < 0)
                heat += dt * c.P0 * (_temperature[_model.Nodes[c.A].Index] - c.P1);
        for (int i = 0; i < _mid.Length; ++i)
        {
            s.X[i] = 2 * _mid[i] - s.X[i];
            if (!Numeric.Finite(s.X[i])) return false;
        }
        for (int i = 0; i < _temperature.Length; ++i)
        {
            if (!(_temperature[i] > 0)) return false;
            s.Temperature[i] = _temperature[i];
        }
        Numeric.Accumulate(work, ref s.Work, ref s.WorkCorrection);
        Numeric.Accumulate(heat, ref s.Heat, ref s.HeatCorrection);
        if (!Numeric.Finite(s.Work - s.Heat - (Energy(s) - s.InitialEnergy)) ||
            !Numeric.Finite(s.WorkCorrection) || !Numeric.Finite(s.HeatCorrection) || !ObservablesFinite(s)) return false;
        s.Time += _model.StepNanoseconds;
        return true;
    }

    private double Relative(GraphComponent c, double[] x, int offset) =>
        x[_model.Nodes[c.A].Index + offset] - c.P3 * (c.B < 0 ? 0 : x[_model.Nodes[c.B].Index + offset]) - (offset == 0 ? c.P2 : 0);

    private double Energy(State s)
    {
        double energy = 0;
        foreach (var n in _model.Nodes)
        {
            if (n.Domain == Domain.Rotational)
            {
                double speed = s.X[n.Index + 1];
                energy += 0.5 * n.Storage * speed * speed;
            }
            else energy += n.Storage * (s.Temperature[n.Index] - n.Initial);
        }
        foreach (var c in _model.Components)
        {
            if (c.Kind == ComponentKind.Shaft)
            {
                double twist = Relative(c, s.X, 0);
                energy += 0.5 * c.P0 * twist * twist;
            }
            else if (c.Kind == ComponentKind.DcMotor)
                energy += 0.5 * c.P1 * s.X[c.Index] * s.X[c.Index];
        }
        for (int i = 0; i < _model.ComponentCount; ++i)
            if (_model.Cylinders[i] is { } cylinder)
                energy += cylinder.EnergyChange(s.X[_model.Nodes[_model.Components[i].A].Index]);
        return energy;
    }

    private bool ObservablesFinite(State s)
    {
        foreach (var c in _model.Components)
        {
            if (c.Kind == ComponentKind.Shaft)
            {
                double twist = Relative(c, s.X, 0);
                if (!Numeric.Finite(twist) || !Numeric.Finite(-c.P0 * twist - c.P1 * Relative(c, s.X, 1))) return false;
            }
            else if (c.Kind == ComponentKind.DcMotor && !Numeric.Finite(c.P2 * s.X[c.Index])) return false;
        }
        for (int i = 0; i < _model.ComponentCount; ++i)
            if (_model.Cylinders[i] is { } cylinder && !cylinder.Finite(s.X[_model.Nodes[_model.Components[i].A].Index])) return false;
        return true;
    }

    private ulong Hash(State s)
    {
        ulong h = Numeric.Hash(_model.Fingerprint, s.Time);
        foreach (double x in s.X) h = Numeric.Hash(h, x);
        foreach (double t in s.Temperature) h = Numeric.Hash(h, t);
        foreach (double input in s.Inputs) h = Numeric.Hash(h, input);
        h = Numeric.Hash(h, s.InitialEnergy); h = Numeric.Hash(h, s.Work);
        h = Numeric.Hash(h, s.WorkCorrection); h = Numeric.Hash(h, s.Heat);
        return Numeric.Hash(h, s.HeatCorrection);
    }

    public SnapshotInfo ReadSnapshot(Span<Scalar> destination)
    {
        if (destination.Length < _model.OutputCount) throw new ArgumentException("Snapshot buffer is too small.", nameof(destination));
        if (!Enter()) throw new InvalidOperationException("Simulation is busy on another thread.");
        try
        {
            double stored = Energy(_state) - _state.InitialEnergy;
            for (int i = 0; i < _model.OutputCount; ++i)
            {
                var b = _model.Outputs[i];
                double value;
                var cylinder = b.IsComponent ? _model.Cylinders[b.Index] : null;
                double crank = cylinder is null ? 0 : _state.X[_model.Nodes[_model.Components[b.Index].A].Index];
                switch (b.Field)
                {
                    case Field.Angle: value = _state.X[_model.Nodes[b.Index].Index]; break;
                    case Field.Speed: value = _state.X[_model.Nodes[b.Index].Index + 1]; break;
                    case Field.Temperature: value = cylinder is not null ? cylinder.Temperature(crank) : _state.Temperature[_model.Nodes[b.Index].Index]; break;
                    case Field.Pressure: value = cylinder!.Pressure(crank); break;
                    case Field.Volume: value = cylinder!.GeometryAt(crank).VolumeCubicMeters; break;
                    case Field.Mass: value = cylinder!.Mass; break;
                    case Field.InternalEnergy: value = cylinder!.Energy(crank); break;
                    case Field.PistonDisplacement: value = cylinder!.GeometryAt(crank).DisplacementMeters; break;
                    case Field.Current: value = _state.X[_model.Components[b.Index].Index]; break;
                    case Field.Twist: value = Relative(_model.Components[b.Index], _state.X, 0); break;
                    case Field.Torque:
                        var c = _model.Components[b.Index];
                        value = cylinder is not null ? cylinder.Torque(crank) : c.Kind == ComponentKind.DcMotor ? c.P2 * _state.X[c.Index] :
                            -c.P0 * Relative(c, _state.X, 0) - c.P1 * Relative(c, _state.X, 1);
                        break;
                    case Field.SourceWork: value = _state.Work; break;
                    case Field.HeatRejected: value = _state.Heat; break;
                    case Field.StoredEnergyChange: value = stored; break;
                    case Field.EnergyResidual: value = _state.Work - _state.Heat - stored; break;
                    default: throw new InvalidOperationException("Unknown compiled output.");
                }
                destination[i] = new(Channels.Output(b.ObjectId, b.Field), value);
            }
            return new(_state.Time, Hash(_state), _model.OutputCount);
        }
        finally { Exit(); }
    }
}
