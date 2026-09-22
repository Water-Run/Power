// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Independent state. Successful stepping and snapshot reads allocate no managed memory.</summary>
public sealed class Simulation
{
    private sealed class State(int dynamics, int thermal, int components, int gases, MixtureState? mixture, int clutches, int gears, int converters, int hydraulicNodes, int hydraulicComponents, int pumps)
    {
        internal readonly double[] X = new double[dynamics], Temperature = new double[thermal], Inputs = new double[components];
        internal readonly double[] Mass = new double[gases], Energy = new double[gases];
        internal readonly MixtureState? Mixture = mixture;
        internal readonly ClutchState? Clutches = clutches == 0 ? null : new(clutches);
        internal readonly HydraulicState? Hydraulic = hydraulicNodes == 0 ? null : new(hydraulicNodes, hydraulicComponents, pumps);
        internal readonly ConverterState? Converters = converters == 0 ? null : new(converters);
        internal readonly double[] GearTorque = new double[gears];
        internal ulong Time;
        internal double InitialEnergy, Work, WorkCorrection, Heat, HeatCorrection;
        internal double Intake, IntakeCorrection, Enthalpy, EnthalpyCorrection;
        internal void CopyFrom(State other)
        {
            Array.Copy(other.X, X, X.Length); Array.Copy(other.Temperature, Temperature, Temperature.Length);
            Array.Copy(other.Inputs, Inputs, Inputs.Length);
            Array.Copy(other.Mass, Mass, Mass.Length); Array.Copy(other.Energy, Energy, Energy.Length);
            Mixture?.CopyFrom(other.Mixture!);
            Clutches?.CopyFrom(other.Clutches!);
            Converters?.CopyFrom(other.Converters!); Hydraulic?.CopyFrom(other.Hydraulic!);
            Array.Copy(other.GearTorque, GearTorque, GearTorque.Length);
            Time = other.Time; InitialEnergy = other.InitialEnergy;
            Work = other.Work; WorkCorrection = other.WorkCorrection;
            Heat = other.Heat; HeatCorrection = other.HeatCorrection;
            Intake = other.Intake; IntakeCorrection = other.IntakeCorrection;
            Enthalpy = other.Enthalpy; EnthalpyCorrection = other.EnthalpyCorrection;
        }
    }

    private readonly CompiledModel _model;
    private State _state, _scratch;
    private readonly double[] _mid, _temperature, _inputCandidate;
    private readonly bool[] _inputSeen;
    private readonly MechanicalSolver? _mechanicalSolver;
    private readonly ConverterSolver? _converters;
    private readonly GasSolver? _gasSolver;
    private readonly HydraulicSolver? _hydraulics;
    private readonly CombustionSolver? _combustion;
    private readonly ClutchSolver? _clutches;
    private readonly GearSolver? _gears;
    private readonly State? _intervalTrial, _intervalAccepted;
    private int _busy;
    public CompiledModel Model => _model;

    internal Simulation(CompiledModel model)
    {
        _model = model;
        if (model.Gears is not null) _gears = new(model.Gears);
        if (model.HasHydraulics) _hydraulics = new(model);
        if (model.HasConverters || model.HasPumps) { _converters = new(model, _hydraulics); _mechanicalSolver = _converters; }
        else if (model.CylinderCoupling is not null) _mechanicalSolver = new CylinderSolver(model);
        if (model.Gas is not null) _gasSolver = new(model);
        if (model.Burners.Any(b => b is not null)) _combustion = new(model);
        State CreateState() => new(model.DynamicCount, model.ThermalCount, model.ComponentCount, model.GasCount,
            model.HasPremixedGas ? new(model) : null, model.ClutchComponents.Length, model.GearComponents.Length, model.ConverterComponents.Length, model.HydraulicCount, model.HydraulicComponents.Length, model.PumpComponents.Length);
        _state = CreateState(); _scratch = CreateState();
        if (model.HasClutches)
        {
            _clutches = new(model, _mechanicalSolver, _gears); _intervalTrial = CreateState(); _intervalAccepted = CreateState();
        }
        _mid = new double[model.DynamicCount]; _temperature = new double[model.ThermalCount];
        _inputCandidate = new double[model.ComponentCount]; _inputSeen = new bool[model.ComponentCount];
        foreach (var n in model.Nodes)
        {
            if (n.Domain == Domain.Rotational) { _state.X[n.Index] = n.Position; _state.X[n.Index + 1] = n.Initial; }
            else if (n.Domain == Domain.Gas)
            {
                _state.Mass[n.Index] = model.Gas!.InitialMass[n.Index];
                _state.Energy[n.Index] = model.Gas.InitialEnergy[n.Index];
            }
            else if (n.Domain == Domain.Hydraulic) _state.Hydraulic!.Pressure[n.Index] = n.Initial;
            else _state.Temperature[n.Index] = n.Initial;
        }
        for (int i = 0; i < model.ComponentCount; ++i)
        {
            var c = model.Components[i];
            _state.Inputs[i] = c.InitialInput;
            if (c.Kind == ComponentKind.DcMotor) _state.X[c.Index] = c.P3;
        }
        _state.InitialEnergy = Energy(_state);
        _clutches?.BeginTick(_state.Clutches!, _state.X, _state.Inputs, _state.Hydraulic?.Pressure);
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
                if (v.Value < _model.InputMinimum[index] || v.Value > _model.InputMaximum[index]) return SimulationStatus.InvalidInput;
                if (_inputSeen[index]) return SimulationStatus.InvalidInput;
                _inputSeen[index] = true;
                _inputCandidate[index] = v.Value == 0 ? 0 : v.Value;
            }
            if (_gasSolver is not null)
            {
                _scratch.CopyFrom(_state);
                Array.Copy(_inputCandidate, _scratch.Inputs, _inputCandidate.Length);
                if (!ObservablesFinite(_scratch)) return SimulationStatus.InvalidInput;
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
                if (input.Value < _model.InputMinimum[index] || input.Value > _model.InputMaximum[index]) return SimulationStatus.InvalidInput;
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
                if (!Tick(_scratch, cancellationToken)) return cancellationToken.IsCancellationRequested ? SimulationStatus.Cancelled : SimulationStatus.NumericalFailure;
                ApplyScheduledInputs(_scratch, inputs, ref nextInput);
            }
            if (_gasSolver is not null && !ObservablesFinite(_scratch)) return SimulationStatus.NumericalFailure;
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

    private bool Tick(State s, CancellationToken cancellation)
    {
        Array.Clear(s.GearTorque, 0, s.GearTorque.Length);
        s.Converters?.BeginTick(); s.Hydraulic?.BeginTick();
        if (_clutches is null)
        {
            if (!Interval(s, _model.Dt, cancellation, out _)) return false;
        }
        else
        {
            _clutches.BeginTick(s.Clutches!, s.X, s.Inputs, s.Hydraulic?.Pressure);
            double remaining = _model.Dt;
            for (int interval = 0; remaining > 0; ++interval)
            {
                if (interval >= ClutchSolver.MaxIntervals || cancellation.IsCancellationRequested) return false;
                var trial = _intervalTrial!; trial.CopyFrom(s);
                if (!Interval(trial, remaining, cancellation, out bool crossing)) return false;
                if (!crossing) { s.CopyFrom(trial); remaining = 0; break; }
                double lo = 0, hi = remaining;
                var accepted = _intervalAccepted!;
                accepted.CopyFrom(s);
                for (int root = 0; root < ClutchSolver.RootIterations; ++root)
                {
                    if (cancellation.IsCancellationRequested) return false;
                    double h = lo + .5 * (hi - lo);
                    if (h == lo || h == hi) break;
                    trial.CopyFrom(s);
                    if (!Interval(trial, h, cancellation, out crossing)) return false;
                    if (crossing) hi = h;
                    else { lo = h; accepted.CopyFrom(trial); }
                }
                if (!_clutches.PromoteEvents(accepted.Clutches!, accepted.X)) return false;
                s.CopyFrom(accepted);
                if (lo > 0 && remaining - lo == remaining) return false;
                remaining -= lo;
            }
            for (int k = 0; k < s.Clutches!.Torque.Length; ++k)
            { s.Clutches.Torque[k] /= _model.Dt; s.Clutches.Power[k] /= _model.Dt; }
            if (!ObservablesFinite(s)) return false;
        }
        for (int k = 0; k < s.GearTorque.Length; ++k)
        {
            s.GearTorque[k] /= _model.Dt;
            if (!Numeric.Finite(s.GearTorque[k])) return false;
        }
        if (s.Converters is not null && !s.Converters.EndTick(_model.Dt)) return false;
        if (s.Hydraulic is not null && !s.Hydraulic.EndTick(_model.Dt)) return false;
        s.Time += _model.StepNanoseconds;
        return true;
    }

    private bool Interval(State s, double dt, CancellationToken cancellation, out bool crossing)
    {
        crossing = false;
        double work = 0, heat = 0;
        if (_clutches is not null && !_clutches.Prepare(dt)) return false;
        if (_model.HasPumps) _converters!.PrepareHydraulics(s.Hydraulic!, s.Inputs, dt);
        if (!_model.HasPumps && _hydraulics is not null && !_hydraulics.Advance(s.Hydraulic!, s.Inputs, dt, cancellation)) return false;
        bool splitGas = _model.HasMovingGas || _model.HasValveTiming || _combustion is not null;
        double intake = 0, enthalpy = 0;
        // Symmetric flow / adiabatic crank-work / flow split. Wall temperatures remain
        // explicit over the outer tick, so wall-coupled models retain first-order accuracy.
        if (splitGas && !_gasSolver!.Advance(s.Mass, s.Energy, s.Temperature, s.Inputs, s.X, dt / 2, s.Mixture)) return false;
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
        if (!(_clutches?.Dynamics ?? _model.Dynamics).Solve(_mid)) return false;
        if (_gears is not null && !_gears.ProjectFree(_mid)) return false;
        _combustion?.Prepare(s.Mixture!, s.Inputs);
        if (_clutches is not null)
        {
            if (!_clutches.Solve(s.Clutches!, s.X, _mid, s.Inputs, s.Energy, _combustion, cancellation, _hydraulics?.MidPressure)) return false;
            crossing = _clutches.Crosses(s.Clutches!, s.X, _mid);
            if (crossing) return true; // Caller discards the speculative interval and brackets the first event.
        }
        else if (_mechanicalSolver is not null && !_mechanicalSolver.Solve(s.X, _mid, s.Energy, _combustion, cancellation)) return false;
        if (_model.HasPumps && !_hydraulics!.Commit(s.Hydraulic!, dt, _mid)) return false;
        if (_gears is not null)
        {
            _mechanicalSolver?.AddGearReactions(_gears.Torque);
            _clutches?.AddGearReactions(_gears.Torque);
            for (int k = 0; k < s.GearTorque.Length; ++k) s.GearTorque[k] += dt * _gears.Torque[k];
        }
        if (_combustion is not null)
        {
            if (!_combustion.Resolve(s.X, _mid, s.Energy, dt)) return false;
            for (int g = 0; g < s.Energy.Length; ++g) s.Energy[g] += .5 * _combustion.Heat[g];
        }
        for (int i = 0; i < _model.Valves.Length; ++i)
            if (s.Inputs[i] != 0 && _model.Valves[i] is { } valve && !valve.Resolved(s.X, _mid, dt)) return false;
        if (_gasSolver is not null && !splitGas && !_gasSolver.Advance(s.Mass, s.Energy, s.Temperature, s.Inputs, s.X, dt, s.Mixture)) return false;
        foreach (var n in _model.Nodes)
            if (n.Domain == Domain.Thermal)
                _temperature[n.Index] = n.Storage * s.Temperature[n.Index] + _model.AmbientForce[n.Index] * (dt / _model.Dt);
        if (_gasSolver is not null)
        {
            for (int w = 0; w < _temperature.Length; ++w) _temperature[w] += _gasSolver.WallHeat[w];
            intake = _gasSolver.ReservoirMass; enthalpy = _gasSolver.ReservoirEnthalpy;
        }
        if (_hydraulics is not null)
        {
            work += _hydraulics.BoundaryWork; heat += _hydraulics.RejectedHeat;
            for (int i = 0; i < _temperature.Length; ++i) _temperature[i] += _hydraulics.WallHeat[i];
        }
        for (int i = 0; i < _model.ComponentCount; ++i)
        {
            var c = _model.Components[i];
            double loss = 0;
            if (c.Kind == ComponentKind.Shaft)
            {
                double slip = Relative(c, _mid, 1);
                loss = dt * c.P1 * slip * slip;
            }
            else if (c.Kind == ComponentKind.TorqueConverter)
            {
                if (!_converters!.Accumulate(c.Index, s.Converters!, dt, _mid, out loss)) return false;
            }
            else if (c.Kind is ComponentKind.Clutch or ComponentKind.HydraulicClutch)
            {
                if (!_clutches!.Accumulate(c.Index, s.Clutches!, dt, _mid, out loss)) return false;
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
            else if (c.Kind == ComponentKind.GasCylinder)
            {
                int angle = _model.Nodes[c.A].Index, gas = _model.Nodes[c.B].Index;
                double delta = (2 * _mid[angle] - s.X[angle]) - s.X[angle];
                var cylinder = _model.GasCylinders[i]!;
                s.Energy[gas] += cylinder.EnergyChange(s.X[angle], delta, s.Energy[gas], _model.Gas!.Gases[gas].Gamma);
                if (!(s.Energy[gas] > 0) || !Numeric.Finite(s.Energy[gas])) return false;
                work += cylinder.BackPressureWork(s.X[angle], delta);
            }
            if (c.Heat < 0) heat += loss;
            else _temperature[_model.Nodes[c.Heat].Index] += loss;
        }
        for (int i = 0; i < _mid.Length; ++i)
        {
            s.X[i] = 2 * _mid[i] - s.X[i];
            if (!Numeric.Finite(s.X[i])) return false;
        }
        if (_combustion is not null)
        {
            for (int g = 0; g < s.Energy.Length; ++g) s.Energy[g] += .5 * _combustion.Heat[g];
            if (!_combustion.Commit(s.X, s.Mass)) return false;
        }
        if (splitGas)
        {
            if (!_gasSolver!.Advance(s.Mass, s.Energy, s.Temperature, s.Inputs, s.X, dt / 2, s.Mixture)) return false;
            for (int w = 0; w < _temperature.Length; ++w) _temperature[w] += _gasSolver.WallHeat[w];
            intake += _gasSolver.ReservoirMass; enthalpy += _gasSolver.ReservoirEnthalpy;
        }
        if (!(_clutches?.Thermal ?? _model.Thermal).Solve(_temperature)) return false;
        foreach (var c in _model.Components)
            if (c.Kind == ComponentKind.ThermalLink && c.B < 0)
                heat += dt * c.P0 * (_temperature[_model.Nodes[c.A].Index] - c.P1);
        for (int i = 0; i < _temperature.Length; ++i)
        {
            if (!(_temperature[i] > 0)) return false;
            s.Temperature[i] = _temperature[i];
        }
        Numeric.Accumulate(work, ref s.Work, ref s.WorkCorrection);
        Numeric.Accumulate(heat, ref s.Heat, ref s.HeatCorrection);
        if (_gasSolver is not null)
        {
            Numeric.Accumulate(intake, ref s.Intake, ref s.IntakeCorrection);
            Numeric.Accumulate(enthalpy, ref s.Enthalpy, ref s.EnthalpyCorrection);
            if (!Numeric.Finite(s.Intake) || !Numeric.Finite(s.IntakeCorrection) ||
                !Numeric.Finite(s.Enthalpy) || !Numeric.Finite(s.EnthalpyCorrection)) return false;
        }
        if (!Numeric.Finite(s.Work + s.Enthalpy - s.Heat - (Energy(s) - s.InitialEnergy)) ||
            !Numeric.Finite(s.WorkCorrection) || !Numeric.Finite(s.HeatCorrection) || !ObservablesFinite(s)) return false;
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
            else if (n.Domain == Domain.Hydraulic) energy += .5 * n.Storage * s.Hydraulic!.Pressure[n.Index] * s.Hydraulic.Pressure[n.Index];
            else if (n.Domain == Domain.Thermal) energy += n.Storage * (s.Temperature[n.Index] - n.Initial);
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
        for (int i = 0; i < _model.GasCount; ++i) energy += s.Energy[i] - _model.Gas!.InitialEnergy[i];
        if (s.Mixture is { } mixture)
            for (int i = 0; i < _model.GasCount; ++i)
                if (_model.Gas!.Mixtures[i] is { } composition) energy += composition.Lhv * (mixture.Fuel[i] - _model.Gas.InitialFuel[i]);
        return energy;
    }

    private double GasPressure(State s, int gas) =>
        (_model.Gas!.Gases[gas].Gamma - 1) * s.Energy[gas] / _model.Gas.VolumeAt(gas, s.X);
    private double GasTemperature(State s, int gas) =>
        s.Energy[gas] / (s.Mass[gas] * _model.Gas!.Gases[gas].IsochoricHeatCapacityJoulePerKilogramKelvin);

    private double HydraulicVolumeChange(State s)
    {
        double volume = 0;
        foreach (var n in _model.Nodes) if (n.Domain == Domain.Hydraulic) volume += n.Storage * (s.Hydraulic!.Pressure[n.Index] - n.Initial);
        return volume;
    }

    private bool ObservablesFinite(State s)
    {
        if (s.Hydraulic is not null)
        {
            if (!s.Hydraulic.Finite() || !Numeric.Finite(HydraulicVolumeChange(s) - s.Hydraulic.VolumeIn)) return false;
            foreach (var n in _model.Nodes) if (n.Domain == Domain.Hydraulic && !Numeric.Finite(n.Storage * s.Hydraulic.Pressure[n.Index])) return false;
            foreach (var actuator in _model.HydraulicActuators) if (actuator is not null)
            {
                double pressure = s.Hydraulic.Pressure[actuator.Pressure];
                if (!Numeric.Finite(actuator.ClampForce(pressure)) || !Numeric.Finite(actuator.Capacity(pressure, false)) || !Numeric.Finite(actuator.Capacity(pressure, true))) return false;
            }
        }
        if (s.Converters is not null)
        {
            if (!s.Converters.Finite()) return false;
            foreach (int i in _model.ConverterComponents)
            {
                var c = _model.Components[i];
                if (_model.Converters[i]!.Evaluate(s.X[_model.Nodes[c.A].Index + 1], s.X[_model.Nodes[c.B].Index + 1], out _) != ConverterEvaluationStatus.Ok) return false;
            }
        }
        if (_model.Gears is { } gears)
        {
            if (!gears.Resolved(s.X)) return false;
            for (int k = 0; k < s.GearTorque.Length; ++k)
                if (!Numeric.Finite(s.GearTorque[k])) return false;
        }
        if (s.Clutches is { } clutches)
            for (int k = 0; k < clutches.Mode.Length; ++k)
                if (!Numeric.Finite(_clutches!.Slip(k, s.X)) || !Numeric.Finite(clutches.Torque[k]) ||
                    !Numeric.Finite(clutches.Power[k]) || !Numeric.Finite(clutches.Heat[k]) || !Numeric.Finite(clutches.HeatCorrection[k])) return false;
        if (s.Mixture is { } mixture)
        {
            if (!Numeric.Finite(mixture.FuelIn) || !Numeric.Finite(mixture.FuelInCorrection) || !Numeric.Finite(mixture.AirIn) ||
                !Numeric.Finite(mixture.AirInCorrection) || !Numeric.Finite(mixture.ChemicalIn) || !Numeric.Finite(mixture.ChemicalInCorrection)) return false;
            double chemical = 0;
            for (int i = 0; i < _model.GasCount; ++i)
                if (_model.Gas!.Mixtures[i] is { } composition)
                {
                    if (mixture.Fuel[i] < 0 || mixture.Air[i] < 0 || mixture.Products[i] < 0 ||
                        !Numeric.Finite(mixture.Fuel[i] + mixture.Air[i] + mixture.Products[i])) return false;
                    chemical += mixture.Fuel[i] * composition.Lhv;
                }
            if (!Numeric.Finite(chemical)) return false;
            for (int i = 0; i < _model.ComponentCount; ++i)
                if (_model.Burners[i] is { } burner && (!Numeric.Finite(mixture.Frontier[i]) ||
                    !Numeric.Finite(mixture.Burned[i] * _model.Gas!.Mixtures[burner.Gas]!.Lhv) ||
                    !Numeric.Finite(mixture.Burned[i] * _model.Gas!.Mixtures[burner.Gas]!.AirFuelRatio))) return false;
            ChemicalTotals(s, out _, out double fuelResidual, out double airResidual);
            if (!Numeric.Finite(fuelResidual) || !Numeric.Finite(airResidual)) return false;
        }
        foreach (var c in _model.Components)
        {
            if (c.Kind == ComponentKind.Shaft)
            {
                double twist = Relative(c, s.X, 0);
                if (!Numeric.Finite(twist) || !Numeric.Finite(-c.P0 * twist - c.P1 * Relative(c, s.X, 1))) return false;
            }
            else if (c.Kind == ComponentKind.DcMotor && !Numeric.Finite(c.P2 * s.X[c.Index])) return false;
        }
        for (int i = 0; i < _model.GasCount; ++i)
        {
            if (!(s.Mass[i] > 0) || !(s.Energy[i] > 0)) return false;
            double pressure = GasPressure(s, i), temperature = GasTemperature(s, i);
            if (!Numeric.Finite(pressure) || pressure <= 0 || !Numeric.Finite(temperature) || temperature <= 0) return false;
        }
        if (_model.Gas is { } network)
        {
            foreach (int component in network.OrificeComponent)
                if (!TryOrificeMassFlow(s, component, out _)) return false;
            for (int k = 0; k < network.HeatComponent.Length; ++k)
                if (!Numeric.Finite(_model.Components[network.HeatComponent[k]].P0 *
                    (GasTemperature(s, network.HeatGas[k]) - s.Temperature[network.HeatWall[k]]))) return false;
        }
        for (int i = 0; i < _model.ComponentCount; ++i)
        {
            if (_model.Cylinders[i] is { } cylinder && !cylinder.Finite(s.X[_model.Nodes[_model.Components[i].A].Index])) return false;
            if (_model.GasCylinders[i] is { } moving)
            {
                var c = _model.Components[i]; int gas = _model.Nodes[c.B].Index;
                if (!Numeric.Finite(moving.Torque(s.X[_model.Nodes[c.A].Index], s.Energy[gas], _model.Gas!.Gases[gas].Gamma))) return false;
            }
        }
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
        h = Numeric.Hash(h, s.HeatCorrection);
        if (s.Clutches is not null) h = s.Clutches.Hash(h);
        foreach (double torque in s.GearTorque) h = Numeric.Hash(h, torque);
        if (s.Converters is not null) h = s.Converters.Hash(h);
        if (s.Hydraulic is not null) h = s.Hydraulic.Hash(h);
        if (_model.GasCount == 0) return h; // Preserve every pre-gas state hash exactly.
        foreach (double mass in s.Mass) h = Numeric.Hash(h, mass);
        foreach (double energy in s.Energy) h = Numeric.Hash(h, energy);
        h = Numeric.Hash(h, s.Intake); h = Numeric.Hash(h, s.IntakeCorrection);
        h = Numeric.Hash(h, s.Enthalpy);
        h = Numeric.Hash(h, s.EnthalpyCorrection);
        return s.Mixture is null ? h : s.Mixture.Hash(h);
    }

    private bool TryOrificeMassFlow(State state, int component, out double value)
    {
        var network = _model.Gas!;
        int slot = network.Slot[component], a = network.OrificeA[slot], b = network.OrificeB[slot];
        var c = _model.Components[component];
        double pressure = b < 0 ? c.P2 : GasPressure(state, b), temperature = b < 0 ? c.P3 : GasTemperature(state, b);
        bool valid = network.Restriction[slot].TryEvaluate(network.Gases[a], GasPressure(state, a),
            GasTemperature(state, a), pressure, temperature, network.Opening(component, state.X, state.Inputs), out var flow);
        value = flow.MassFlowKilogramsPerSecond;
        return valid;
    }

    private void ChemicalTotals(State state, out double chemical, out double fuelResidual, out double airResidual)
    {
        chemical = 0; fuelResidual = 0; airResidual = 0;
        var mixture = state.Mixture; if (mixture is null) return;
        fuelResidual = mixture.FuelIn; airResidual = mixture.AirIn;
        for (int g = 0; g < _model.GasCount; ++g)
            if (_model.Gas!.Mixtures[g] is { } composition)
            {
                chemical += mixture.Fuel[g] * composition.Lhv;
                fuelResidual -= mixture.Fuel[g] - _model.Gas.InitialFuel[g];
                airResidual -= mixture.Air[g] - _model.Gas.InitialAir[g];
            }
        for (int c = 0; c < _model.ComponentCount; ++c)
            if (_model.Burners[c] is { } burner)
            {
                fuelResidual -= mixture.Burned[c];
                airResidual -= mixture.Burned[c] * _model.Gas!.Mixtures[burner.Gas]!.AirFuelRatio;
            }
    }

    public SnapshotInfo ReadSnapshot(Span<Scalar> destination)
    {
        if (destination.Length < _model.OutputCount) throw new ArgumentException("Snapshot buffer is too small.", nameof(destination));
        if (!Enter()) throw new InvalidOperationException("Simulation is busy on another thread.");
        try
        {
            double stored = Energy(_state) - _state.InitialEnergy, gasMass = 0;
            for (int g = 0; g < _model.GasCount; ++g) gasMass += _state.Mass[g] - _model.Gas!.InitialMass[g];
            ChemicalTotals(_state, out double chemical, out double fuelResidual, out double airResidual);
            var mixture = _state.Mixture;
            for (int i = 0; i < _model.OutputCount; ++i)
            {
                var b = _model.Outputs[i];
                double value;
                var cylinder = b.IsComponent ? _model.Cylinders[b.Index] : null;
                var movingCylinder = b.IsComponent ? _model.GasCylinders[b.Index] : null;
                double crank = cylinder is null && movingCylinder is null ? 0 : _state.X[_model.Nodes[_model.Components[b.Index].A].Index];
                int volume = !b.IsComponent && b.Index >= 0 && _model.Nodes[b.Index].Domain == Domain.Gas
                    ? _model.Nodes[b.Index].Index : -1;
                bool hydraulicNode = !b.IsComponent && b.Index >= 0 && _model.Nodes[b.Index].Domain == Domain.Hydraulic;
                double pressure = hydraulicNode ? _state.Hydraulic!.Pressure[_model.Nodes[b.Index].Index] : 0;
                switch (b.Field)
                {
                    case Field.Angle: value = _state.X[_model.Nodes[b.Index].Index]; break;
                    case Field.Speed: value = _state.X[_model.Nodes[b.Index].Index + 1]; break;
                    case Field.Temperature:
                        value = cylinder is not null ? cylinder.Temperature(crank)
                            : volume >= 0 ? GasTemperature(_state, volume)
                            : _state.Temperature[_model.Nodes[b.Index].Index];
                        break;
                    case Field.Pressure: value = hydraulicNode ? pressure : cylinder is not null ? cylinder.Pressure(crank) : GasPressure(_state, volume); break;
                    case Field.Volume: value = hydraulicNode ? _model.Nodes[b.Index].Storage * pressure : cylinder is not null ? cylinder.GeometryAt(crank).VolumeCubicMeters : movingCylinder!.GeometryAt(crank).VolumeCubicMeters; break;
                    case Field.Mass: value = cylinder is not null ? cylinder.Mass : _state.Mass[volume]; break;
                    case Field.InternalEnergy: value = hydraulicNode ? .5 * _model.Nodes[b.Index].Storage * pressure * pressure : cylinder is not null ? cylinder.Energy(crank) : _state.Energy[volume]; break;
                    case Field.PistonDisplacement: value = cylinder is not null ? cylinder.GeometryAt(crank).DisplacementMeters : movingCylinder!.GeometryAt(crank).DisplacementMeters; break;
                    case Field.FuelMass: value = mixture!.Fuel[volume]; break;
                    case Field.FreshAirMass: value = mixture!.Air[volume]; break;
                    case Field.ProductMass: value = mixture!.Products[volume]; break;
                    case Field.ChemicalEnergy: value = volume < 0 ? chemical : mixture!.Fuel[volume] * _model.Gas!.Mixtures[volume]!.Lhv; break;
                    case Field.FuelBurned: value = mixture!.Burned[b.Index]; break;
                    case Field.BurnFrontier: value = mixture!.Frontier[b.Index]; break;
                    case Field.HeatReleased: value = mixture!.Burned[b.Index] * _model.Gas!.Mixtures[_model.Burners[b.Index]!.Gas]!.Lhv; break;
                    case Field.FuelEnergyIn: value = mixture!.ChemicalIn; break;
                    case Field.FuelResidual: value = fuelResidual; break;
                    case Field.FreshAirResidual: value = airResidual; break;
                    case Field.MassFlow:
                        if (!TryOrificeMassFlow(_state, b.Index, out value))
                            throw new InvalidOperationException("Invalid gas flow in a committed state.");
                        break;
                    case Field.Opening: value = _model.Gas!.Opening(b.Index, _state.X, _state.Inputs); break;
                    case Field.SlipSpeed:
                        var slipComponent = _model.Components[b.Index];
                        value = slipComponent.Kind is ComponentKind.Clutch or ComponentKind.HydraulicClutch ? _clutches!.Slip(slipComponent.Index, _state.X)
                            : _model.Gears!.Dot(slipComponent.Index, _state.X) * _model.Gears.Scale[slipComponent.Index];
                        break;
                    case Field.ConstraintError:
                        int gearIndex = _model.Components[b.Index].Index;
                        value = (_model.Gears!.Dot(gearIndex, _state.X, 0) - _model.Gears.Phase[gearIndex]) * _model.Gears.Scale[gearIndex];
                        break;
                    case Field.HydraulicVolumeIn: value = _state.Hydraulic!.VolumeIn; break;
                    case Field.HydraulicVolumeResidual: value = HydraulicVolumeChange(_state) - _state.Hydraulic!.VolumeIn; break;
                    case Field.HydraulicWork: value = b.IsComponent ? _state.Hydraulic!.PumpWork[_model.Components[b.Index].Index] : _state.Hydraulic!.Work; break;
                    case Field.HydraulicPower: value = _state.Hydraulic!.PumpPower[_model.Components[b.Index].Index]; break;
                    case Field.VolumeFlow: value = _model.Components[b.Index].Kind == ComponentKind.HydraulicPump ? _state.Hydraulic!.PumpFlow[_model.Components[b.Index].Index] : _state.Hydraulic!.Flow[_model.Components[b.Index].Index]; break;
                    case Field.ClampForce:
                    case Field.StaticCapacity:
                    case Field.SlidingCapacity:
                        var actuator = _model.HydraulicActuators[b.Index]!;
                        double controlPressure = _state.Hydraulic!.Pressure[actuator.Pressure];
                        value = b.Field == Field.ClampForce ? actuator.ClampForce(controlPressure) : actuator.Capacity(controlPressure, b.Field == Field.SlidingCapacity);
                        break;
                    case Field.FluidHeat:
                        value = _model.Components[b.Index].Kind == ComponentKind.TorqueConverter ? _state.Converters!.Heat[_model.Components[b.Index].Index]
                            : _state.Hydraulic!.Heat[_model.Components[b.Index].Index]; break;
                    case Field.SpeedRatio:
                    case Field.ConverterDrive:
                        var fluid = _model.Components[b.Index];
                        if (_model.Converters[b.Index]!.Evaluate(_state.X[_model.Nodes[fluid.A].Index + 1], _state.X[_model.Nodes[fluid.B].Index + 1], out var reaction) != ConverterEvaluationStatus.Ok)
                            throw new InvalidOperationException("Invalid converter in a committed state.");
                        value = b.Field == Field.SpeedRatio ? reaction.SpeedRatio : (double)reaction.Drive;
                        break;
                    case Field.TorqueAtB:
                    case Field.TorqueAtC:
                        var gearComponent = _model.Components[b.Index];
                        if (gearComponent.Kind == ComponentKind.TorqueConverter)
                        {
                            value = b.Field == Field.TorqueAtB ? _state.Converters!.TurbineTorque[gearComponent.Index]
                                : -_state.Converters!.PumpTorque[gearComponent.Index] - _state.Converters.TurbineTorque[gearComponent.Index];
                            break;
                        }
                        int port = b.Field == Field.TorqueAtB ? gearComponent.B : gearComponent.C;
                        value = _state.GearTorque[gearComponent.Index] * _model.Gears!.Rows[gearComponent.Index][_model.Nodes[port].Index + 1];
                        break;
                    case Field.ClutchMode: value = (double)_state.Clutches!.Mode[_model.Components[b.Index].Index]; break;
                    case Field.FrictionHeat: value = _state.Clutches!.Heat[_model.Components[b.Index].Index]; break;
                    case Field.HeatFlow:
                        if (_model.Components[b.Index].Kind is ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicRelief)
                        { value = _state.Hydraulic!.Power[_model.Components[b.Index].Index]; break; }
                        if (_model.Components[b.Index].Kind == ComponentKind.TorqueConverter)
                        { value = _state.Converters!.Power[_model.Components[b.Index].Index]; break; }
                        if (_model.Components[b.Index].Kind is ComponentKind.Clutch or ComponentKind.HydraulicClutch)
                        { value = _state.Clutches!.Power[_model.Components[b.Index].Index]; break; }
                        int link = _model.Gas!.Slot[b.Index];
                        value = _model.Components[b.Index].P0 *
                            (GasTemperature(_state, _model.Gas.HeatGas[link]) - _state.Temperature[_model.Gas.HeatWall[link]]);
                        break;
                    case Field.Current: value = _state.X[_model.Components[b.Index].Index]; break;
                    case Field.Twist: value = Relative(_model.Components[b.Index], _state.X, 0); break;
                    case Field.Torque:
                        var c = _model.Components[b.Index];
                        int movingGas = movingCylinder is null ? -1 : _model.Nodes[c.B].Index;
                        value = movingCylinder is not null ? movingCylinder.Torque(crank, _state.Energy[movingGas], _model.Gas!.Gases[movingGas].Gamma)
                            : cylinder is not null ? cylinder.Torque(crank) : c.Kind == ComponentKind.DcMotor ? c.P2 * _state.X[c.Index]
                            : c.Kind == ComponentKind.HydraulicPump ? _state.Hydraulic!.PumpTorque[c.Index]
                            : c.Kind == ComponentKind.TorqueConverter ? _state.Converters!.PumpTorque[c.Index]
                            : c.Kind is ComponentKind.Clutch or ComponentKind.HydraulicClutch ? _state.Clutches!.Torque[c.Index]
                            : c.Kind is ComponentKind.IdealGear or ComponentKind.PlanetaryGear ? _state.GearTorque[c.Index] / _model.Gears!.Scale[c.Index] :
                            -c.P0 * Relative(c, _state.X, 0) - c.P1 * Relative(c, _state.X, 1);
                        break;
                    case Field.SourceWork: value = _state.Work; break;
                    case Field.HeatRejected: value = _state.Heat; break;
                    case Field.StoredEnergyChange: value = stored; break;
                    case Field.EnergyResidual: value = _state.Work + _state.Enthalpy - _state.Heat - stored; break;
                    case Field.ReservoirEnthalpy: value = _state.Enthalpy; break;
                    case Field.MassResidual: value = gasMass - _state.Intake; break;
                    default: throw new InvalidOperationException("Unknown compiled output.");
                }
                destination[i] = new(Channels.Output(b.ObjectId, b.Field), value);
            }
            return new(_state.Time, Hash(_state), _model.OutputCount);
        }
        finally { Exit(); }
    }
}
