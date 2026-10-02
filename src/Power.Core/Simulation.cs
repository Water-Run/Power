// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Independent state. Successful stepping and snapshot reads allocate no managed memory.</summary>
public sealed class Simulation
{
    private sealed class State(int dynamics, int thermal, int components, int gases, MixtureState? mixture, int clutches, int gears, int converters, int hydraulicNodes, int hydraulicComponents, int pumps, int controllers, int linearSprings, int injectors, int films)
    {
        internal readonly double[] X = new double[dynamics], Temperature = new double[thermal], Inputs = new double[components];
        internal readonly double[] Mass = new double[gases], Energy = new double[gases];
        internal readonly MixtureState? Mixture = mixture;
        internal readonly FuelFilmState? Films=films==0?null:new(films);
        internal LiquidInjectorState? LiquidInjectors;
        internal SolenoidState? Solenoids;
        internal NeedleDriverState? NeedleDrivers;
        internal DctControllerState? DctControllers;
        internal double[]? PositionCorrection;
        internal readonly FuelInjectorState? Injectors = injectors == 0 ? null : new(injectors);
        internal readonly ClutchState? Clutches = clutches == 0 ? null : new(clutches);
        internal readonly HydraulicState? Hydraulic = hydraulicNodes == 0 ? null : new(hydraulicNodes, hydraulicComponents, pumps);
        internal readonly ConverterState? Converters = converters == 0 ? null : new(converters);
        internal readonly double[] GearTorque = new double[gears];
        internal readonly double[] DampingHeat = new double[linearSprings], DampingCorrection = new double[linearSprings];
        internal readonly PressureControllerState? Controllers = controllers == 0 ? null : new(controllers);
        internal ulong Time;
        internal double InitialEnergy, Work, WorkCorrection, Heat, HeatCorrection;
        internal double Intake, IntakeCorrection, Enthalpy, EnthalpyCorrection;
        internal void CopyFrom(State other)
        {
            Array.Copy(other.X, X, X.Length); Array.Copy(other.Temperature, Temperature, Temperature.Length);
            Array.Copy(other.Inputs, Inputs, Inputs.Length);
            Array.Copy(other.Mass, Mass, Mass.Length); Array.Copy(other.Energy, Energy, Energy.Length);
            Mixture?.CopyFrom(other.Mixture!); Injectors?.CopyFrom(other.Injectors!);Films?.CopyFrom(other.Films!);
            LiquidInjectors?.CopyFrom(other.LiquidInjectors!);
            Solenoids?.CopyFrom(other.Solenoids!); NeedleDrivers?.CopyFrom(other.NeedleDrivers!);
            DctControllers?.CopyFrom(other.DctControllers!);
            if (PositionCorrection is not null) Array.Copy(other.PositionCorrection!, PositionCorrection, PositionCorrection.Length);
            Clutches?.CopyFrom(other.Clutches!);
            Converters?.CopyFrom(other.Converters!); Hydraulic?.CopyFrom(other.Hydraulic!);
            Controllers?.CopyFrom(other.Controllers!);
            Array.Copy(other.GearTorque, GearTorque, GearTorque.Length);
            Array.Copy(other.DampingHeat, DampingHeat, DampingHeat.Length);
            Array.Copy(other.DampingCorrection, DampingCorrection, DampingCorrection.Length);
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
    private readonly FuelFilmSolver? _films;
    private readonly LiquidFuelInjectorSolver? _liquidInjectors;
    private readonly HydraulicSolver? _hydraulics;
    private readonly ElectricalDynamics? _electrical;
    private readonly CombustionSolver? _combustion;
    private readonly ClutchSolver? _clutches;
    private readonly GearSolver? _gears;
    private readonly State? _intervalTrial, _intervalAccepted;
    private readonly State? _closureState;
    private bool _forecasting;
    private int _busy;
    public CompiledModel Model => _model;

    internal Simulation(CompiledModel model)
    {
        _model = model;
        if (model.HasBatteries) _electrical = new(model);
        if (model.Gears is not null) _gears = new(model.Gears);
        if (model.HasHydraulics) _hydraulics = new(model);
        if (model.HasConverters || model.HasCoupledHydraulics || model.HasGasPistons || model.HasSolenoids || model.HasTravelStops) { _converters = new(model, _hydraulics); _mechanicalSolver = _converters; }
        else if (model.CylinderCoupling is not null) _mechanicalSolver = new CylinderSolver(model);
        if (model.Gas is not null) _gasSolver = new(model);
        if(model.HasFuelFilms)_films=new(model);
        if (model.HasLiquidFuelInjectors) _liquidInjectors = new(model, _films!);
        if (model.Burners.Any(b => b is not null)) _combustion = new(model);
        State CreateState() => new(model.DynamicCount, model.ThermalCount, model.ComponentCount, model.GasCount,
            model.HasPremixedGas ? new(model) : null, model.ClutchComponents.Length, model.GearComponents.Length, model.ConverterComponents.Length, model.HydraulicCount, model.HydraulicComponents.Length, model.PumpComponents.Length, model.PressureControllers.Length, model.LinearSpringCount, model.FuelInjectors.Length, model.FuelFilms.Length)
            { Solenoids = model.HasSolenoids ? new(model.Solenoids.Length) : null, NeedleDrivers = model.NeedleDrivers.Length != 0 ? new(model.NeedleDrivers.Length, model.HasClosurePrediction) : null,
                DctControllers = model.DctControllers.Length != 0 ? new(model.DctControllers.Length) : null,
                PositionCorrection = model.DctControllers.Length != 0 || model.Components.Any(c => c.Kind is ComponentKind.DoublePinionPlanetaryGear or ComponentKind.CarrierGear)
                    ? new double[model.DynamicCount] : null };
        _state = CreateState(); _scratch = CreateState();
        if (model.HasLiquidFuelInjectors)
        {
            _state.LiquidInjectors = new(model.LiquidFuelInjectors.Length);
            _scratch.LiquidInjectors = new(model.LiquidFuelInjectors.Length);
        }
        if (model.HasClosurePrediction)
        {
            _closureState = CreateState();
            _closureState.LiquidInjectors = new(model.LiquidFuelInjectors.Length);
        }
        if (model.HasClutches)
        {
            _clutches = new(model, _mechanicalSolver, _gears); _intervalTrial = CreateState(); _intervalAccepted = CreateState();
            if (model.HasLiquidFuelInjectors)
            {
                _intervalTrial.LiquidInjectors = new(model.LiquidFuelInjectors.Length);
                _intervalAccepted.LiquidInjectors = new(model.LiquidFuelInjectors.Length);
            }
        }
        _mid = new double[model.DynamicCount]; _temperature = new double[model.ThermalCount];
        _inputCandidate = new double[model.ComponentCount]; _inputSeen = new bool[model.ComponentCount];
        foreach (var n in model.Nodes)
        {
            if (n.Domain is Domain.Rotational or Domain.Translational) { _state.X[n.Index] = n.Position; _state.X[n.Index + 1] = n.Initial; }
            else if (n.Domain == Domain.Battery) { _state.X[n.Index] = n.Initial; _state.X[n.Index + 1] = n.Position; }
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
            if (c.Kind is ComponentKind.DcMotor or ComponentKind.BatteryMotor) _state.X[c.Index] = c.P3;
        }
        for(int i=0;i<model.FuelFilms.Length;++i){_state.Films!.Mass[i]=model.FuelFilms[i].Initial.LiquidMassKilograms;_state.Films.Energy[i]=model.FuelFilms[i].Initial.ThermalEnergyJoules;}
        for (int i = 0; i < model.Solenoids.Length; ++i) _state.Solenoids!.Flux[i] = model.Solenoids[i].InitialFlux;
        _state.InitialEnergy = Energy(_state);
        for (int k = 0; k < model.PressureControllers.Length; ++k)
        {
            var controller = model.PressureControllers[k];
            _state.Controllers!.Integral[k] = controller.InitialIntegral;
            _state.Controllers.Command[k] = _state.Inputs[controller.Motor];
        }
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
                if (!_model.InputIndices.TryGetValue(v.Channel, out int index))
                    return _model.ControlledInputs.Contains(v.Channel) ? SimulationStatus.ControlledInput : SimulationStatus.UnknownChannel;
                if (v.Value < _model.InputMinimum[index] || v.Value > _model.InputMaximum[index]) return SimulationStatus.InvalidInput;
                if (_model.IntegerInputs[index] && v.Value != Math.Truncate(v.Value)) return SimulationStatus.InvalidInput;
                if (_inputSeen[index]) return SimulationStatus.InvalidInput;
                _inputSeen[index] = true;
                _inputCandidate[index] = v.Value == 0 ? 0 : v.Value;
            }
            if (_gasSolver is not null || _electrical is not null)
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
                if (!_model.InputIndices.TryGetValue(input.Channel, out int index))
                    return _model.ControlledInputs.Contains(input.Channel) ? SimulationStatus.ControlledInput : SimulationStatus.UnknownChannel;
                if (input.Value < _model.InputMinimum[index] || input.Value > _model.InputMaximum[index]) return SimulationStatus.InvalidInput;
                if (_model.IntegerInputs[index] && input.Value != Math.Truncate(input.Value)) return SimulationStatus.InvalidInput;
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
            if ((_gasSolver is not null || _electrical is not null) && !ObservablesFinite(_scratch)) return SimulationStatus.NumericalFailure;
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

    private bool ForecastClosure(State source, CompiledNeedleDriver driver, CancellationToken cancellation, out NeedleClosureEstimate estimate, uint delayTicks = 0)
    {
        estimate = default;
        ulong horizon = (ulong)driver.PredictionTicks * _model.StepNanoseconds;
        if (_closureState is null || driver.PredictionTicks == 0 || ulong.MaxValue - source.Time < horizon) return false;
        _closureState.CopyFrom(source);
        int coil = _model.Solenoids[driver.Solenoid].Component;
        _closureState.Inputs[coil] = delayTicks == 0 ? 0 : driver.Voltage;
        double delivered = source.LiquidInjectors!.Quota.Total[driver.Injector];
        _forecasting = true;
        try
        {
            for (uint i = 0; i < driver.PredictionTicks; ++i)
            {
                if ((i & 31) == 0 && cancellation.IsCancellationRequested) return false;
                if (i == delayTicks) _closureState.Inputs[coil] = 0;
                if (!Tick(_closureState, cancellation)) return false;
            }
            double tail = _closureState.LiquidInjectors!.Quota.Total[driver.Injector] - delivered;
            if (!Numeric.Finite(tail) || tail < 0) return false;
            estimate = new(tail, driver.PredictionTicks, horizon); return true;
        }
        finally { _forecasting = false; }
    }

    /// <summary>Replay a bounded zero-voltage closure without changing committed state or control history.</summary>
    public SimulationStatus PredictNeedleClosure(uint driverId, out NeedleClosureEstimate estimate, CancellationToken cancellation = default)
    {
        estimate = default;
        if (!Enter()) return SimulationStatus.Busy;
        try
        {
            int index = -1;
            for (int i = 0; i < _model.NeedleDrivers.Length; ++i) if (_model.Components[_model.NeedleDrivers[i].Component].Id == driverId) { index = i; break; }
            if (index < 0 || _model.NeedleDrivers[index].PredictionTicks == 0) return SimulationStatus.InvalidInput;
            return ForecastClosure(_state, _model.NeedleDrivers[index], cancellation, out estimate) ? SimulationStatus.Ok
                : cancellation.IsCancellationRequested ? SimulationStatus.Cancelled : SimulationStatus.NumericalFailure;
        }
        finally { Exit(); }
    }

    private bool Tick(State s, CancellationToken cancellation)
    {
        for (int k = 0; !_forecasting && k < _model.DctControllers.Length; ++k)
        {
            var controller = _model.DctControllers[k]; if (s.Time % controller.Period != 0) continue;
            if (!DctControl.Sample(_model, controller, s.DctControllers!, k, s.Time, (int)s.Inputs[controller.Component], s.X, s.Inputs, s.Clutches!)) return false;
        }
        for (int k = 0; !_forecasting && k < _model.NeedleDrivers.Length; ++k)
        {
            if (_model.NeedleDrivers[k].PredictionTicks == 0 || s.NeedleDrivers!.PendingTicks[k] == 0) continue;
            if (--s.NeedleDrivers.PendingTicks[k] == 0)
            { s.NeedleDrivers.Voltage[k] = 0; s.Inputs[_model.Solenoids[_model.NeedleDrivers[k].Solenoid].Component] = 0; }
        }
        for (int k = 0; !_forecasting && k < _model.NeedleDrivers.Length; ++k)
        {
            var driver = _model.NeedleDrivers[k]; if (s.Time % driver.Period != 0) continue;
            var injector = _model.LiquidFuelInjectors[driver.Injector]; var quota = s.LiquidInjectors!.Quota;
            if (!quota.Prepare(driver.Injector, injector.Meter, s.X, s.Inputs[injector.Component], _model.Dt, out double opening, out _)) return false;
            s.NeedleDrivers!.Target[k] = quota.Target[driver.Injector]; s.NeedleDrivers.Delivered[k] = quota.Delivered[driver.Injector];
            double command = opening > 0 ? driver.Voltage : 0;
            if (driver.PredictionTicks != 0)
            {
                if (s.NeedleDrivers.Closed[k] && quota.Cycle[driver.Injector] != s.NeedleDrivers.ClosedCycle[k])
                { s.NeedleDrivers.Closed[k] = false; s.NeedleDrivers.PendingTicks[k] = 0; }
                if (opening > 0 && !s.NeedleDrivers.Closed[k])
                {
                    if (!ForecastClosure(s, driver, cancellation, out var estimate)) return false;
                    s.NeedleDrivers.Tail[k] = estimate.AdditionalFuelKilograms; s.NeedleDrivers.Ticks[k] = estimate.PhysicalTicks;
                    double remaining = quota.Target[driver.Injector] - quota.Delivered[driver.Injector];
                    uint delay = 0; bool close = estimate.AdditionalFuelKilograms >= remaining;
                    uint last = (uint)Math.Min((ulong)driver.PredictionTicks - 1, driver.Period / _model.StepNanoseconds);
                    if (!close && last != 0)
                    {
                        if (!ForecastClosure(s, driver, cancellation, out var delayed, last)) return false;
                        if (delayed.AdditionalFuelKilograms >= remaining)
                        {
                            uint low = 0, high = last; double lowMass = estimate.AdditionalFuelKilograms, highMass = delayed.AdditionalFuelKilograms;
                            while (high - low > 1)
                            {
                                uint mid = low + (high - low) / 2;
                                if (!ForecastClosure(s, driver, cancellation, out var candidate, mid)) return false;
                                if (candidate.AdditionalFuelKilograms < lowMass - 1e-15 || candidate.AdditionalFuelKilograms > highMass + 1e-15) return false;
                                if (candidate.AdditionalFuelKilograms >= remaining) { high = mid; highMass = candidate.AdditionalFuelKilograms; }
                                else { low = mid; lowMass = candidate.AdditionalFuelKilograms; }
                            }
                            delay = remaining - lowMass <= highMass - remaining ? low : high;
                            s.NeedleDrivers.Tail[k] = delay == low ? lowMass : highMass; close = true;
                        }
                    }
                    if (close)
                    { s.NeedleDrivers.Closed[k] = true; s.NeedleDrivers.ClosedCycle[k] = quota.Cycle[driver.Injector]; s.NeedleDrivers.PendingTicks[k] = delay; }
                }
                if (opening == 0) s.NeedleDrivers.PendingTicks[k] = 0;
                if (s.NeedleDrivers.Closed[k]) command = s.NeedleDrivers.PendingTicks[k] == 0 ? 0 : driver.Voltage;
            }
            s.NeedleDrivers.Voltage[k] = command;
            s.Inputs[_model.Solenoids[driver.Solenoid].Component] = s.NeedleDrivers.Voltage[k];
        }
        for (int k = 0; !_forecasting && k < _model.PressureControllers.Length; ++k)
        {
            var controller = _model.PressureControllers[k];
            if (s.Time % controller.Period != 0) continue;
            double measured = s.Hydraulic!.Pressure[controller.Pressure];
            if (!controller.Law.TrySample(s.Inputs[controller.Component], measured, s.Controllers!.Integral[k],
                s.Time == 0 ? 0 : controller.Period * 1e-9, out var result)) return false;
            s.Controllers.Pressure[k] = measured; s.Controllers.Error[k] = result.ErrorPascals;
            s.Controllers.Integral[k] = result.IntegralVoltage; s.Controllers.Command[k] = result.CommandVoltage;
            s.Inputs[controller.Motor] = result.CommandVoltage;
        }
        Array.Clear(s.GearTorque, 0, s.GearTorque.Length);
        s.Films?.BeginTick();s.Injectors?.BeginTick(); s.Converters?.BeginTick(); s.Hydraulic?.BeginTick();
        s.LiquidInjectors?.Quota.BeginTick();
        s.Solenoids?.BeginTick();
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
        if (s.Injectors is not null && !s.Injectors.EndTick(_model.Dt)) return false;
        if(s.Films is not null&&!s.Films.EndTick(_model.Dt))return false;
        if (s.LiquidInjectors is not null && !s.LiquidInjectors.Quota.EndTick(_model.Dt)) return false;
        if (s.Solenoids is not null && !s.Solenoids.EndTick(_model.Dt)) return false;
        s.Time += _model.StepNanoseconds;
        return true;
    }

    private bool Interval(State s, double dt, CancellationToken cancellation, out bool crossing)
    {
        crossing = false;
        double work = 0, heat = 0;
        if (_electrical is not null && !_electrical.Prepare(dt, s.Inputs)) return false;
        var dynamics = _electrical?.Dynamics ?? _model.Dynamics;
        if (_clutches is not null && !_clutches.Prepare(dt, _electrical?.Dynamics, _electrical?.Generation ?? 0)) return false;
        if (_clutches is null && _electrical is not null)
        {
            if (_gears is not null && !_gears.Prepare(dt, dynamics, true)) return false;
            if (_mechanicalSolver is not null && !_mechanicalSolver.SetInterval(dt, dynamics, _gears)) return false;
        }
        if (_model.HasCoupledHydraulics) _converters!.PrepareHydraulics(s.Hydraulic!, s.Inputs, dt);
        if (_model.HasSolenoids) _converters!.PrepareSolenoids(s.Solenoids!, s.Inputs, dt);
        if (!_model.HasCoupledHydraulics && _hydraulics is not null && !_hydraulics.Advance(s.Hydraulic!, s.Inputs, dt, cancellation)) return false;
        bool splitGas = _model.HasFuelFilms || _model.HasFuelInjectors || _model.HasMovingGas || _model.HasValveTiming || _combustion is not null;
        double intake = 0, enthalpy = 0;
        // Symmetric flow / adiabatic crank-work / flow split. Wall temperatures remain
        // explicit over the outer tick, so wall-coupled models retain first-order accuracy.
        if (_films is not null)
        {
            _films.BeginInterval(s.Temperature);
            _liquidInjectors?.BeginInterval();
            if (_liquidInjectors is not null && !_liquidInjectors.Advance(s.LiquidInjectors!, s.Films!, s.X, s.Energy, s.Inputs, dt / 2)) return false;
            if (!_films.Advance(s.Films!, s.Mass, s.Energy, s.Mixture!, dt / 2)) return false;
        }
        if (splitGas && !_gasSolver!.Advance(s.Mass, s.Energy, s.Temperature, s.Inputs, s.X, dt / 2, s.Mixture, s.Injectors)) return false;
        var force = _electrical?.Force ?? _model.ConstantForce;
        for (int i = 0; i < _mid.Length; ++i) _mid[i] = s.X[i] + 0.5 * dt * force[i];
        for (int i = 0; i < _model.ComponentCount; ++i)
        {
            var c = _model.Components[i];
            if (c.Kind is ComponentKind.TorqueSource or ComponentKind.ForceSource)
            {
                var n = _model.Nodes[c.A];
                _mid[n.Index + 1] += 0.5 * dt * s.Inputs[i] / n.Storage;
            }
            else if (c.Kind == ComponentKind.DcMotor) _mid[c.Index] += 0.5 * dt * s.Inputs[i] / c.P1;
        }
        if (!(_clutches?.Dynamics ?? dynamics).Solve(_mid)) return false;
        if (_gears is not null && !_gears.ProjectFree(_mid, s.X)) return false;
        _combustion?.Prepare(s.Mixture!, s.Inputs);
        if (_clutches is not null)
        {
            if (!_clutches.Solve(s.Clutches!, s.X, _mid, s.Inputs, s.Energy, _combustion, cancellation, _hydraulics?.MidPressure)) return false;
            crossing = _clutches.Crosses(s.Clutches!, s.X, _mid);
            if (crossing) return true; // Caller discards the speculative interval and brackets the first event.
        }
        else if (_mechanicalSolver is not null && !_mechanicalSolver.Solve(s.X, _mid, s.Energy, _combustion, cancellation)) return false;
        if (_model.HasCoupledHydraulics && !_hydraulics!.Commit(s.Hydraulic!, dt, _mid)) return false;
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
        foreach (var injector in _model.FuelInjectors)
            if (Math.Abs(2 * (_mid[injector.Crank] - s.X[injector.Crank])) > injector.Profile.MaximumTravelRadians) return false;
        foreach (var injector in _model.LiquidFuelInjectors)
            if (Math.Abs(2 * (_mid[injector.Meter.Crank] - s.X[injector.Meter.Crank])) > injector.Meter.Profile.MaximumTravelRadians) return false;
        for (int i = 0; i < _model.Valves.Length; ++i)
            if (s.Inputs[i] != 0 && _model.Valves[i] is { } valve && !valve.Resolved(s.X, _mid, dt)) return false;
        if (_gasSolver is not null && !splitGas && !_gasSolver.Advance(s.Mass, s.Energy, s.Temperature, s.Inputs, s.X, dt, s.Mixture, s.Injectors)) return false;
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
            if (c.Kind is ComponentKind.Shaft or ComponentKind.LinearSpring)
            {
                double slip = Relative(c, _mid, 1);
                loss = dt * c.P1 * slip * slip;
                if (c.Kind == ComponentKind.LinearSpring) Numeric.Accumulate(loss, ref s.DampingHeat[c.Index], ref s.DampingCorrection[c.Index]);
            }
            else if (c.Kind == ComponentKind.TorqueConverter)
            {
                if (!_converters!.Accumulate(c.Index, s.Converters!, dt, _mid, out loss)) return false;
            }
            else if (c.Kind is ComponentKind.Clutch or ComponentKind.HydraulicClutch or ComponentKind.PistonClutch)
            {
                if (!_clutches!.Accumulate(c.Index, s.Clutches!, dt, _mid, out loss)) return false;
            }
            else if (c.Kind == ComponentKind.Solenoid)
            {
                int slot = _model.SolenoidByComponent[i]; var coil = _model.Solenoids[slot]; double oldPosition = s.X[coil.Position];
                if (!coil.Law.TryInterval(oldPosition, 2 * _mid[coil.Position] - oldPosition, s.Solenoids!.Flux[slot], s.Inputs[i], dt, out var interval)) return false;
                s.Solenoids.Flux[slot] = interval.NextFluxWebers; s.Solenoids.Force[slot] += dt * interval.ForceNewtons;
                Numeric.Accumulate(interval.SupplyWorkJoules, ref s.Solenoids.Work[slot], ref s.Solenoids.WorkCorrection[slot]);
                Numeric.Accumulate(interval.CopperHeatJoules, ref s.Solenoids.Heat[slot], ref s.Solenoids.HeatCorrection[slot]);
                loss = interval.CopperHeatJoules; work += interval.SupplyWorkJoules;
            }
            else if (c.Kind is ComponentKind.DcMotor or ComponentKind.BatteryMotor)
            {
                double current = _mid[c.Index];
                loss = dt * c.P0 * current * current;
                if (c.Kind == ComponentKind.DcMotor) work += dt * s.Inputs[i] * current;
            }
            else if (c.Kind == ComponentKind.ResistiveLoad)
            {
                if (!BatteryCircuit.Evaluate(_model, _model.BatteryByNode[c.A]!, _mid, s.Inputs, out _, out var battery)) return false;
                loss = dt * s.Inputs[i] / c.P0 * battery.TerminalVoltage * battery.TerminalVoltage;
            }
            else if (c.Kind is ComponentKind.TorqueSource or ComponentKind.ForceSource) work += dt * s.Inputs[i] * _mid[_model.Nodes[c.A].Index + 1];
            else if (c.Kind == ComponentKind.SealedCylinder)
            {
                int angle = _model.Nodes[c.A].Index;
                work += _model.Cylinders[i]!.BackPressureWork(s.X[angle], (2 * _mid[angle] - s.X[angle]) - s.X[angle]);
            }
            else if (c.Kind == ComponentKind.GasPiston)
            {
                int position = _model.Nodes[c.A].Index, gas = _model.Nodes[c.B].Index;
                if (!_model.GasPistons[i]!.TryAdiabaticWork(s.X[position], 2 * _mid[position] - s.X[position], s.Energy[gas], _model.Gas!.Gases[gas].Gamma, out var gasWork)) return false;
                s.Energy[gas] += gasWork.GasEnergyChangeJoules; work += gasWork.ReferenceWorkJoules;
                if (!(s.Energy[gas] > 0) || !Numeric.Finite(s.Energy[gas])) return false;
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
        foreach (var battery in _model.Batteries)
        {
            if (!BatteryCircuit.Evaluate(_model, battery, _mid, s.Inputs, out _, out var reaction)) return false;
            double loss = dt * reaction.HeatFlowWatts;
            if (battery.Heat < 0) heat += loss; else _temperature[_model.Nodes[battery.Heat].Index] += loss;
        }
        for (int i = 0; i < _mid.Length; ++i)
        {
            // New long-running controlled gear graphs retain sub-ULP position increments.
            // Their correction is transactional; preceding model paths keep their replay.
            if (s.PositionCorrection is not null && _model.CoordinateStates[i])
                Numeric.Accumulate(dt * _mid[i + 1], ref s.X[i], ref s.PositionCorrection[i]);
            else s.X[i] = 2 * _mid[i] - s.X[i];
            if (!Numeric.Finite(s.X[i])) return false;
        }
        if (_combustion is not null)
        {
            for (int g = 0; g < s.Energy.Length; ++g) s.Energy[g] += .5 * _combustion.Heat[g];
            if (!_combustion.Commit(s.X, s.Mass)) return false;
        }
        if (splitGas)
        {
            if (!_gasSolver!.Advance(s.Mass, s.Energy, s.Temperature, s.Inputs, s.X, dt / 2, s.Mixture, s.Injectors)) return false;
            for (int w = 0; w < _temperature.Length; ++w) _temperature[w] += _gasSolver.WallHeat[w];
            intake += _gasSolver.ReservoirMass; enthalpy += _gasSolver.ReservoirEnthalpy;
        }
        // Reverse the first half's operators, including films that share a finite wall.
        if (_films is not null && !_films.Advance(s.Films!, s.Mass, s.Energy, s.Mixture!, dt / 2, reverse: true)) return false;
        if (_liquidInjectors is not null)
        {
            if (!_liquidInjectors.Advance(s.LiquidInjectors!, s.Films!, s.X, s.Energy, s.Inputs, dt / 2, reverse: true)) return false;
            // The negligible-volume receiver exports the displacement pressure work.
            work -= _liquidInjectors.ReceiverWork;
        }
        if(_films is not null)for(int i=0;i<_temperature.Length;++i)_temperature[i]+=_films.WallHeat[i];
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
            if (n.Domain is Domain.Rotational or Domain.Translational)
            {
                double speed = s.X[n.Index + 1];
                energy += 0.5 * n.Storage * speed * speed;
            }
            else if (n.Domain == Domain.Battery)
                continue;
            else if (n.Domain == Domain.Hydraulic) energy += .5 * n.Storage * s.Hydraulic!.Pressure[n.Index] * s.Hydraulic.Pressure[n.Index];
            else if (n.Domain == Domain.Thermal) energy += n.Storage * (s.Temperature[n.Index] - n.Initial);
        }
        for(int i=0;i<_model.FuelFilms.Length;++i)energy+=s.Films!.Energy[i]+s.Films.Mass[i]*_model.FuelFilms[i].HeatingValue;
        for (int i = 0; i < _model.LiquidFuelInjectors.Length; ++i)
        {
            var injector = _model.LiquidFuelInjectors[i];
            var rail = injector.Rail.StateAfterDelivery(s.LiquidInjectors!.Quota.Total[i]);
            energy += rail.MassKilograms * (injector.SpecificThermalEnergy + injector.HeatingValue)
                + injector.Rail.StoredPressureEnergy(rail.PressurePascals);
        }
        foreach (var battery in _model.Batteries)
        {
            int index = battery.State; var law = battery.Law;
            energy += law.ChemicalEnergy(s.X[index]) + .5 * law.PolarizationCapacitanceFarads * s.X[index + 1] * s.X[index + 1];
        }
        foreach (int index in _model.PistonComponents)
            energy += _model.Pistons[index]!.StoredContactEnergy(s.X[_model.Nodes[_model.Components[index].A].Index]);
        foreach (var stop in _model.TravelStops) energy += stop.Law.Energy(s.X[stop.Position]);
        for (int i = 0; i < _model.Solenoids.Length; ++i)
        { var coil = _model.Solenoids[i]; energy += coil.Law.Energy(s.X[coil.Position], s.Solenoids!.Flux[i]); }
        foreach (var c in _model.Components)
        {
            if (c.Kind is ComponentKind.Shaft or ComponentKind.LinearSpring)
            {
                double twist = Relative(c, s.X, 0);
                energy += 0.5 * c.P0 * twist * twist;
            }
            else if (c.Kind is ComponentKind.DcMotor or ComponentKind.BatteryMotor)
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
        foreach (int index in _model.PistonComponents)
        {
            var c = _model.Components[index]; var slider = _model.Nodes[c.A];
            volume += (c.P0 - c.P1) * (s.X[slider.Index] - slider.Position);
        }
        return volume;
    }

    private bool ObservablesFinite(State s)
    {
        if (s.PositionCorrection is not null) foreach (double value in s.PositionCorrection) if (!Numeric.Finite(value)) return false;
        if (s.Solenoids is not null)
        {
            if (!s.Solenoids.Finite()) return false;
            for (int i = 0; i < _model.Solenoids.Length; ++i)
            {
                var coil = _model.Solenoids[i]; double l = coil.Law.Inductance(s.X[coil.Position]);
                if (!Numeric.Finite(l) || l <= 0 || !Numeric.Finite(coil.Law.Current(s.X[coil.Position], s.Solenoids.Flux[i])) ||
                    !Numeric.Finite(coil.Law.Energy(s.X[coil.Position], s.Solenoids.Flux[i]))) return false;
            }
        }
        foreach (var stop in _model.TravelStops) if (!Numeric.Finite(stop.Law.Energy(s.X[stop.Position]))) return false;
        foreach (int index in _model.PistonComponents)
        {
            var c = _model.Components[index]; var law = _model.Pistons[index]!; double x = s.X[_model.Nodes[c.A].Index];
            if (!Numeric.Finite(law.StoredContactEnergy(x)) || !Numeric.Finite(law.ContactForce(x)) ||
                !Numeric.Finite(law.PressureForce(s.Hydraulic!.Pressure[_model.Nodes[c.B].Index], c.C < 0 ? c.P2 : s.Hydraulic.Pressure[_model.Nodes[c.C].Index]))) return false;
        }
        foreach (var battery in _model.Batteries)
            if (!BatteryCircuit.Evaluate(_model, battery, s.X, s.Inputs, out _, out _)) return false;
        if (s.Controllers is not null && !s.Controllers.Finite()) return false;
        if (s.Injectors is not null && !s.Injectors.Finite()) return false;
        if(s.Films is not null&&!s.Films.Finite())return false;
        if (s.LiquidInjectors is not null)
        {
            if (!s.LiquidInjectors.Finite()) return false;
            for (int i = 0; i < _model.LiquidFuelInjectors.Length; ++i)
            {
                var rail = _model.LiquidFuelInjectors[i].Rail.StateAfterDelivery(s.LiquidInjectors.Quota.Total[i]);
                if (!Numeric.Finite(rail.MassKilograms) || rail.MassKilograms < 0 || !Numeric.Finite(rail.PressurePascals) || rail.PressurePascals < 0) return false;
            }
        }
        for (int i = 0; i < s.DampingHeat.Length; ++i)
            if (!Numeric.Finite(s.DampingHeat[i]) || !Numeric.Finite(s.DampingCorrection[i])) return false;
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
            if (c.Kind is ComponentKind.Shaft or ComponentKind.LinearSpring)
            {
                double twist = Relative(c, s.X, 0);
                if (!Numeric.Finite(twist) || !Numeric.Finite(-c.P0 * twist - c.P1 * Relative(c, s.X, 1))) return false;
            }
            else if (c.Kind is ComponentKind.DcMotor or ComponentKind.BatteryMotor && !Numeric.Finite(c.P2 * s.X[c.Index])) return false;
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
            if (_model.GasPistons[i] is { } gasPiston && !Numeric.Finite(gasPiston.Force(GasPressure(s, _model.Nodes[_model.Components[i].B].Index)))) return false;
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
        if (s.Controllers is not null) h = s.Controllers.Hash(h);
        if (s.Injectors is not null) h = s.Injectors.Hash(h);
        if(s.Films is not null)h=s.Films.Hash(h);
        if (s.LiquidInjectors is not null) h = s.LiquidInjectors.Hash(h);
        if (s.Solenoids is not null) h = s.Solenoids.Hash(h);
        if (s.NeedleDrivers is not null) h = s.NeedleDrivers.Hash(h);
        if (s.DctControllers is not null) h = s.DctControllers.Hash(h);
        if (s.PositionCorrection is not null) foreach (double value in s.PositionCorrection) h = Numeric.Hash(h, value);
        foreach (double loss in s.DampingHeat) h = Numeric.Hash(h, loss);
        foreach (double correction in s.DampingCorrection) h = Numeric.Hash(h, correction);
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
        for(int i=0;i<_model.FuelFilms.Length;++i){chemical+=state.Films!.Mass[i]*_model.FuelFilms[i].HeatingValue;fuelResidual-=state.Films.Mass[i]-_model.FuelFilms[i].InitialMass;}
        for (int i = 0; i < _model.LiquidFuelInjectors.Length; ++i)
        {
            var injector = _model.LiquidFuelInjectors[i];
            double delivered = state.LiquidInjectors!.Quota.Total[i];
            chemical += (injector.Rail.InitialMassKilograms - delivered) * injector.HeatingValue;
            fuelResidual += delivered;
        }
        for (int c = 0; c < _model.ComponentCount; ++c)
            if (_model.Burners[c] is { } burner)
            {
                fuelResidual -= mixture.Burned[c];
                airResidual -= mixture.Burned[c] * _model.Gas!.Mixtures[burner.Gas]!.AirFuelRatio;
            }
    }

    private BatteryReaction ReadBattery(int node, out double current)
    {
        if (!BatteryCircuit.Evaluate(_model, _model.BatteryByNode[node]!, _state.X, _state.Inputs, out current, out var reaction))
            throw new InvalidOperationException("Invalid battery circuit in a committed state.");
        return reaction;
    }

    public SnapshotInfo ReadSnapshot(Span<Scalar> destination)
    {
        if (destination.Length < _model.OutputCount) throw new ArgumentException("Snapshot buffer is too small.", nameof(destination));
        if (!Enter()) throw new InvalidOperationException("Simulation is busy on another thread.");
        try
        {
            double stored = Energy(_state) - _state.InitialEnergy, gasMass = 0;
            for (int g = 0; g < _model.GasCount; ++g) gasMass += _state.Mass[g] - _model.Gas!.InitialMass[g];
            for(int i=0;i<_model.FuelFilms.Length;++i)gasMass+=_state.Films!.Mass[i]-_model.FuelFilms[i].InitialMass;
            if (_state.LiquidInjectors is not null)
                foreach (double delivered in _state.LiquidInjectors.Quota.Total) gasMass -= delivered;
            ChemicalTotals(_state, out double chemical, out double fuelResidual, out double airResidual);
            var mixture = _state.Mixture;
            for (int i = 0; i < _model.OutputCount; ++i)
            {
                var b = _model.Outputs[i];
                double value;
                var cylinder = b.IsComponent ? _model.Cylinders[b.Index] : null;
                var movingCylinder = b.IsComponent ? _model.GasCylinders[b.Index] : null;
                var gasPiston = b.IsComponent ? _model.GasPistons[b.Index] : null;
                double crank = cylinder is null && movingCylinder is null ? 0 : _state.X[_model.Nodes[_model.Components[b.Index].A].Index];
                int volume = !b.IsComponent && b.Index >= 0 && _model.Nodes[b.Index].Domain == Domain.Gas
                    ? _model.Nodes[b.Index].Index : -1;
                bool hydraulicNode = !b.IsComponent && b.Index >= 0 && _model.Nodes[b.Index].Domain == Domain.Hydraulic;
                bool batteryNode = !b.IsComponent && b.Index >= 0 && _model.Nodes[b.Index].Domain == Domain.Battery;
                int liquidSlot = b.IsComponent ? _model.LiquidInjectorByComponent[b.Index] : -1;
                if (liquidSlot >= 0)
                {
                    var injector = _model.LiquidFuelInjectors[liquidSlot]; var state = _state.LiquidInjectors!;
                    var rail = injector.Rail.StateAfterDelivery(state.Quota.Total[liquidSlot]);
                    value = b.Field switch
                    {
                        Field.Mass => rail.MassKilograms, Field.Pressure => rail.PressurePascals,
                        Field.Temperature => injector.SupplyTemperature,
                        Field.Volume => rail.MassKilograms / injector.Rail.DensityKilogramsPerCubicMeter,
                        Field.InternalEnergy => rail.MassKilograms * injector.SpecificThermalEnergy + injector.Rail.StoredPressureEnergy(rail.PressurePascals),
                        Field.ChemicalEnergy => rail.MassKilograms * injector.HeatingValue,
                        Field.Opening => injector.Needle?.Opening(_state.X) ?? state.Quota.Opening(liquidSlot, injector.Meter, _state.X),
                        Field.MassFlow => state.Quota.TickFuel[liquidSlot],
                        Field.RequestedFuelDose => state.Quota.Target[liquidSlot],
                        Field.DeliveredFuelDose => state.Quota.Delivered[liquidSlot],
                        Field.TotalFuelDelivered => state.Quota.Total[liquidSlot],
                        Field.SourceWork => state.SourceWork[liquidSlot],
                        Field.HydraulicWork => state.ReceiverWork[liquidSlot], Field.FluidHeat => state.Heat[liquidSlot],
                        _ => double.NaN
                    };
                    if (!Numeric.Finite(value)) throw new InvalidOperationException("Invalid liquid injector channel in committed state.");
                    destination[i] = new(Channels.Output(b.ObjectId, b.Field), value); continue;
                }
                int filmSlot=b.IsComponent?_model.FilmByComponent[b.Index]:-1;
                int coilSlot = b.IsComponent ? _model.SolenoidByComponent[b.Index] : -1;
                if (coilSlot >= 0)
                {
                    var coil = _model.Solenoids[coilSlot]; var state = _state.Solenoids!;
                    value = b.Field switch { Field.Current => coil.Law.Current(_state.X[coil.Position], state.Flux[coilSlot]),
                        Field.Force => state.Force[coilSlot], Field.InternalEnergy => coil.Law.Energy(_state.X[coil.Position], state.Flux[coilSlot]),
                        Field.CopperHeat => state.Heat[coilSlot], Field.SourceWork => state.Work[coilSlot], _ => double.NaN };
                    if (!Numeric.Finite(value)) throw new InvalidOperationException("Invalid solenoid channel in committed state.");
                    destination[i] = new(Channels.Output(b.ObjectId, b.Field), value); continue;
                }
                int stopSlot = b.IsComponent ? _model.StopByComponent[b.Index] : -1;
                if (stopSlot >= 0)
                {
                    var stop = _model.TravelStops[stopSlot]; double x = _state.X[stop.Position];
                    value = b.Field == Field.InternalEnergy ? stop.Law.Energy(x) : stop.Law.Reaction(x, x);
                    destination[i] = new(Channels.Output(b.ObjectId, b.Field), value); continue;
                }
                int driverSlot = b.IsComponent ? _model.NeedleDriverByComponent[b.Index] : -1;
                int dctSlot = b.IsComponent ? _model.DctControllerByComponent[b.Index] : -1;
                if (dctSlot >= 0)
                {
                    var state = _state.DctControllers!;
                    int active = state.Active[dctSlot];
                    if (active != 0)
                    {
                        var controller = _model.DctControllers[dctSlot]; bool odd = active > 0 && active % 2 == 1;
                        int drive = odd ? controller.Odd : controller.Even, selector = controller.Selectors[active == -1 ? 7 : active - 1];
                        if (_state.Clutches!.Mode[_model.Components[drive].Index] != ClutchMode.Locked || _state.Clutches.Mode[_model.Components[selector].Index] != ClutchMode.Locked)
                            active = 0;
                    }
                    value = b.Field switch { Field.RequestedGear => _state.Inputs[b.Index], Field.ActualGear => active,
                        Field.SelectedOddGear => state.Odd[dctSlot], Field.SelectedEvenGear => state.Even[dctSlot],
                        Field.ShiftPhase => (int)state.Phase[dctSlot], Field.SyncError => state.Error[dctSlot], Field.ControlFault => (int)state.Fault[dctSlot], _ => double.NaN };
                    destination[i] = new(Channels.Output(b.ObjectId, b.Field), value); continue;
                }
                if (driverSlot >= 0)
                {
                    var state = _state.NeedleDrivers!;
                    value = b.Field switch { Field.CommandVoltage => state.Voltage[driverSlot], Field.RequestedFuelDose => state.Target[driverSlot],
                        Field.DeliveredFuelDose => state.Delivered[driverSlot], Field.PredictedFuelMass => state.Tail[driverSlot],
                        Field.PredictionTicks => state.Ticks[driverSlot], Field.DriverState => state.Closed[driverSlot] ? 1 : 0,
                        Field.ClosingDelayTicks => state.PendingTicks[driverSlot], _ => double.NaN };
                    destination[i] = new(Channels.Output(b.ObjectId, b.Field), value); continue;
                }
                if(filmSlot>=0)
                {
                    var film=_model.FuelFilms[filmSlot];var filmState=_state.Films!;double mass=filmState.Mass[filmSlot];
                    value=b.Field switch{Field.Mass=>mass,Field.Temperature=>film.Law.Temperature(new(mass,filmState.Energy[filmSlot])),Field.InternalEnergy=>filmState.Energy[filmSlot],Field.ChemicalEnergy=>mass*film.HeatingValue,Field.EvaporatedFuelMass=>filmState.Vaporized[filmSlot],Field.MassFlow=>filmState.TickVapor[filmSlot],Field.FilmWallHeat=>filmState.WallHeat[filmSlot],Field.HeatFlow=>mass==0?0:film.Law.ConductanceWattsPerKelvin*(_state.Temperature[film.Wall]-film.Law.Temperature(new(mass,filmState.Energy[filmSlot]))),_=>double.NaN};
                    if(!Numeric.Finite(value))throw new InvalidOperationException("Invalid film channel in committed state.");destination[i]=new(Channels.Output(b.ObjectId,b.Field),value);continue;
                }
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
                    case Field.Volume: value = hydraulicNode ? _model.Nodes[b.Index].Storage * pressure : cylinder is not null ? cylinder.GeometryAt(crank).VolumeCubicMeters : gasPiston is not null ? gasPiston.VolumeAt(_state.X[_model.Nodes[_model.Components[b.Index].A].Index]) : movingCylinder!.GeometryAt(crank).VolumeCubicMeters; break;
                    case Field.Mass: value = cylinder is not null ? cylinder.Mass : _state.Mass[volume]; break;
                    case Field.InternalEnergy:
                        if (b.IsComponent && _model.Components[b.Index].Kind == ComponentKind.HydraulicPiston)
                            value = _model.Pistons[b.Index]!.StoredContactEnergy(_state.X[_model.Nodes[_model.Components[b.Index].A].Index]);
                        else if (batteryNode) { var energy = ReadBattery(b.Index, out _); value = energy.ChemicalEnergyJoules + energy.PolarizationEnergyJoules; }
                        else value = hydraulicNode ? .5 * _model.Nodes[b.Index].Storage * pressure * pressure : cylinder is not null ? cylinder.Energy(crank) : _state.Energy[volume];
                        break;
                    case Field.PistonDisplacement: value = cylinder is not null ? cylinder.GeometryAt(crank).DisplacementMeters : movingCylinder!.GeometryAt(crank).DisplacementMeters; break;
                    case Field.FuelMass: value = mixture!.Fuel[volume]; break;
                    case Field.FreshAirMass: value = mixture!.Air[volume]; break;
                    case Field.ProductMass: value = mixture!.Products[volume]; break;
                    case Field.ChemicalEnergy: value = batteryNode ? ReadBattery(b.Index, out _).ChemicalEnergyJoules : volume < 0 ? chemical : mixture!.Fuel[volume] * _model.Gas!.Mixtures[volume]!.Lhv; break;
                    case Field.StateOfCharge: value = _state.X[_model.Nodes[b.Index].Index]; break;
                    case Field.Charge: value = _model.Nodes[b.Index].Storage * _state.X[_model.Nodes[b.Index].Index]; break;
                    case Field.PolarizationVoltage: value = _state.X[_model.Nodes[b.Index].Index + 1]; break;
                    case Field.BatteryCurrent: ReadBattery(b.Index, out value); break;
                    case Field.TerminalVoltage:
                        if (batteryNode) value = ReadBattery(b.Index, out _).TerminalVoltage;
                        else
                        {
                            var source = _model.Components[b.Index];
                            value = ReadBattery(source.Kind == ComponentKind.BatteryMotor ? source.B : source.A, out _).TerminalVoltage;
                            if (source.Kind == ComponentKind.BatteryMotor) value *= _state.Inputs[b.Index];
                        }
                        break;
                    case Field.FuelBurned: value = mixture!.Burned[b.Index]; break;
                    case Field.BurnFrontier: value = mixture!.Frontier[b.Index]; break;
                    case Field.HeatReleased: value = mixture!.Burned[b.Index] * _model.Gas!.Mixtures[_model.Burners[b.Index]!.Gas]!.Lhv; break;
                    case Field.FuelEnergyIn: value = mixture!.ChemicalIn; break;
                    case Field.FuelResidual: value = fuelResidual; break;
                    case Field.FreshAirResidual: value = airResidual; break;
                    case Field.MassFlow:
                        if (_model.InjectorByComponent[b.Index] >= 0) { value = _state.Injectors!.TickFuel[_model.InjectorByComponent[b.Index]]; break; }
                        if (!TryOrificeMassFlow(_state, b.Index, out value))
                            throw new InvalidOperationException("Invalid gas flow in a committed state.");
                        break;
                    case Field.Opening: value = _model.InjectorByComponent[b.Index] >= 0 ? _state.Injectors!.Opening(_model.InjectorByComponent[b.Index], _model.FuelInjectors[_model.InjectorByComponent[b.Index]], _state.X) : _model.SpoolValves[b.Index] is { } metering ? metering.Law.Opening(_state.X[metering.Position]) : _model.Gas!.Opening(b.Index, _state.X, _state.Inputs); break;
                    case Field.RequestedFuelDose: value = _state.Injectors!.Target[_model.InjectorByComponent[b.Index]]; break;
                    case Field.DeliveredFuelDose: value = _state.Injectors!.Delivered[_model.InjectorByComponent[b.Index]]; break;
                    case Field.TotalFuelDelivered: value = _state.Injectors!.Total[_model.InjectorByComponent[b.Index]]; break;
                    case Field.SampledPressure: value = _state.Controllers!.Pressure[_model.Components[b.Index].Index]; break;
                    case Field.PressureError: value = _state.Controllers!.Error[_model.Components[b.Index].Index]; break;
                    case Field.IntegralVoltage: value = _state.Controllers!.Integral[_model.Components[b.Index].Index]; break;
                    case Field.CommandVoltage: value = _state.Controllers!.Command[_model.Components[b.Index].Index]; break;
                    case Field.IntegralDuty: value = _state.Controllers!.Integral[_model.Components[b.Index].Index]; break;
                    case Field.CommandDuty: value = _state.Controllers!.Command[_model.Components[b.Index].Index]; break;
                    case Field.Displacement: value = b.IsComponent ? Relative(_model.Components[b.Index], _state.X, 0) : _state.X[_model.Nodes[b.Index].Index]; break;
                    case Field.LinearSpeed: value = _state.X[_model.Nodes[b.Index].Index + 1]; break;
                    case Field.Force:
                        var forceComponent = _model.Components[b.Index];
                        value = gasPiston is not null ? gasPiston.Force(GasPressure(_state, _model.Nodes[forceComponent.B].Index))
                            : forceComponent.Kind == ComponentKind.LinearSpring ? -forceComponent.P0 * Relative(forceComponent, _state.X, 0) - forceComponent.P1 * Relative(forceComponent, _state.X, 1)
                            : _model.Pistons[b.Index]!.PressureForce(_state.Hydraulic!.Pressure[_model.Nodes[forceComponent.B].Index], forceComponent.C < 0 ? forceComponent.P2 : _state.Hydraulic.Pressure[_model.Nodes[forceComponent.C].Index]);
                        break;
                    case Field.SlipSpeed:
                        var slipComponent = _model.Components[b.Index];
                        value = slipComponent.Kind is ComponentKind.Clutch or ComponentKind.HydraulicClutch or ComponentKind.PistonClutch ? _clutches!.Slip(slipComponent.Index, _state.X)
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
                        if (_model.Components[b.Index].Kind is ComponentKind.PistonClutch or ComponentKind.HydraulicPiston)
                        {
                            var friction = _model.PistonFriction[b.Index]; int index = friction?.Piston ?? b.Index;
                            double normal = _model.Pistons[index]!.ContactForce(_state.X[_model.Nodes[_model.Components[index].A].Index]);
                            value = b.Field == Field.ClampForce ? normal : friction!.Capacity(normal, b.Field == Field.SlidingCapacity); break;
                        }
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
                    case Field.FrictionHeat: value = _model.Components[b.Index].Kind == ComponentKind.LinearSpring ? _state.DampingHeat[_model.Components[b.Index].Index] : _state.Clutches!.Heat[_model.Components[b.Index].Index]; break;
                    case Field.HeatFlow:
                        if (batteryNode) { value = ReadBattery(b.Index, out _).HeatFlowWatts; break; }
                        if (_model.Components[b.Index].Kind == ComponentKind.ResistiveLoad)
                        {
                            var resistor = _model.Components[b.Index]; double voltage = ReadBattery(resistor.A, out _).TerminalVoltage;
                            value = _state.Inputs[b.Index] / resistor.P0 * voltage * voltage; break;
                        }
                        if (_model.Components[b.Index].Kind is ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicRelief or ComponentKind.HydraulicSpoolValve)
                        { value = _state.Hydraulic!.Power[_model.Components[b.Index].Index]; break; }
                        if (_model.Components[b.Index].Kind == ComponentKind.TorqueConverter)
                        { value = _state.Converters!.Power[_model.Components[b.Index].Index]; break; }
                        if (_model.Components[b.Index].Kind is ComponentKind.Clutch or ComponentKind.HydraulicClutch or ComponentKind.PistonClutch)
                        { value = _state.Clutches!.Power[_model.Components[b.Index].Index]; break; }
                        int link = _model.Gas!.Slot[b.Index];
                        value = _model.Components[b.Index].P0 *
                            (GasTemperature(_state, _model.Gas.HeatGas[link]) - _state.Temperature[_model.Gas.HeatWall[link]]);
                        break;
                    case Field.Current:
                        var electrical = _model.Components[b.Index];
                        value = electrical.Kind == ComponentKind.ResistiveLoad ? _state.Inputs[b.Index] / electrical.P0 * ReadBattery(electrical.A, out _).TerminalVoltage : _state.X[electrical.Index];
                        break;
                    case Field.Twist: value = Relative(_model.Components[b.Index], _state.X, 0); break;
                    case Field.Torque:
                        var c = _model.Components[b.Index];
                        int movingGas = movingCylinder is null ? -1 : _model.Nodes[c.B].Index;
                        value = movingCylinder is not null ? movingCylinder.Torque(crank, _state.Energy[movingGas], _model.Gas!.Gases[movingGas].Gamma)
                            : cylinder is not null ? cylinder.Torque(crank) : c.Kind is ComponentKind.DcMotor or ComponentKind.BatteryMotor ? c.P2 * _state.X[c.Index]
                            : c.Kind == ComponentKind.HydraulicPump ? _state.Hydraulic!.PumpTorque[c.Index]
                            : c.Kind == ComponentKind.TorqueConverter ? _state.Converters!.PumpTorque[c.Index]
                            : c.Kind is ComponentKind.Clutch or ComponentKind.HydraulicClutch or ComponentKind.PistonClutch ? _state.Clutches!.Torque[c.Index]
                            : c.Kind is ComponentKind.IdealGear or ComponentKind.PlanetaryGear or ComponentKind.DoublePinionPlanetaryGear or ComponentKind.CarrierGear ? _state.GearTorque[c.Index] / _model.Gears!.Scale[c.Index] :
                            -c.P0 * Relative(c, _state.X, 0) - c.P1 * Relative(c, _state.X, 1);
                        break;
                    case Field.SourceWork: value = gasPiston is null ? _state.Work : gasPiston.ReferencePressurePascals * gasPiston.CompressionDirection * gasPiston.AreaSquareMeters * (_state.X[_model.Nodes[_model.Components[b.Index].A].Index] - _model.Nodes[_model.Components[b.Index].A].Position); break;
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
