// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

internal sealed class HydraulicState(int nodes, int restrictions, int pumps = 0)
{
    internal readonly double[] Pressure = new double[nodes];
    internal readonly double[] Flow = new double[restrictions], Power = new double[restrictions];
    internal readonly double[] Heat = new double[restrictions], Correction = new double[restrictions];
    internal readonly double[] PumpFlow = new double[pumps], PumpTorque = new double[pumps], PumpPower = new double[pumps];
    internal readonly double[] PumpWork = new double[pumps], PumpCorrection = new double[pumps];
    internal double VolumeIn, VolumeCorrection, Work, WorkCorrection;
    internal void CopyFrom(HydraulicState other)
    {
        Array.Copy(other.Pressure, Pressure, Pressure.Length); Array.Copy(other.Flow, Flow, Flow.Length);
        Array.Copy(other.Power, Power, Power.Length); Array.Copy(other.Heat, Heat, Heat.Length); Array.Copy(other.Correction, Correction, Correction.Length);
        Array.Copy(other.PumpFlow, PumpFlow, PumpFlow.Length); Array.Copy(other.PumpTorque, PumpTorque, PumpTorque.Length);
        Array.Copy(other.PumpPower, PumpPower, PumpPower.Length); Array.Copy(other.PumpWork, PumpWork, PumpWork.Length); Array.Copy(other.PumpCorrection, PumpCorrection, PumpCorrection.Length);
        VolumeIn = other.VolumeIn; VolumeCorrection = other.VolumeCorrection; Work = other.Work; WorkCorrection = other.WorkCorrection;
    }
    internal void BeginTick() { Array.Clear(PumpFlow, 0, PumpFlow.Length); Array.Clear(PumpTorque, 0, PumpTorque.Length); Array.Clear(PumpPower, 0, PumpPower.Length); Array.Clear(Flow, 0, Flow.Length); Array.Clear(Power, 0, Power.Length); }
    internal bool EndTick(double duration)
    { for (int i = 0; i < Flow.Length; ++i) { Flow[i] /= duration; Power[i] /= duration; }
        for (int i = 0; i < PumpFlow.Length; ++i) { PumpFlow[i] /= duration; PumpTorque[i] /= duration; PumpPower[i] /= duration; } return Finite(); }
    internal bool Finite()
    {
        foreach (double p in Pressure) if (!Numeric.Finite(p) || p < 0) return false;
        for (int i = 0; i < Flow.Length; ++i)
            if (!GearReference.Finite(Flow[i], Power[i], Heat[i], Correction[i]) || Power[i] < 0 || Heat[i] < 0) return false;
        for (int i = 0; i < PumpFlow.Length; ++i)
            if (!GearReference.Finite(PumpFlow[i], PumpTorque[i], PumpPower[i], PumpWork[i]) || !Numeric.Finite(PumpCorrection[i])) return false;
        return GearReference.Finite(VolumeIn, VolumeCorrection, Work, WorkCorrection);
    }
    internal ulong Hash(ulong hash)
    {
        foreach (double p in Pressure) hash = Numeric.Hash(hash, p);
        for (int i = 0; i < Flow.Length; ++i)
        {
            hash = Numeric.Hash(hash, Flow[i]); hash = Numeric.Hash(hash, Power[i]);
            hash = Numeric.Hash(hash, Heat[i]); hash = Numeric.Hash(hash, Correction[i]);
        }
        for (int i = 0; i < PumpFlow.Length; ++i)
        { hash = Numeric.Hash(hash, PumpFlow[i]); hash = Numeric.Hash(hash, PumpTorque[i]); hash = Numeric.Hash(hash, PumpPower[i]); hash = Numeric.Hash(hash, PumpWork[i]); hash = Numeric.Hash(hash, PumpCorrection[i]); }
        hash = Numeric.Hash(hash, VolumeIn); hash = Numeric.Hash(hash, VolumeCorrection);
        hash = Numeric.Hash(hash, Work); return Numeric.Hash(hash, WorkCorrection);
    }
}

/// <summary>Implicit midpoint pressure network, using conservative accepted edge transfers and actual pressure-work losses.</summary>
internal sealed class HydraulicSolver
{
    internal const int MaxIterations = 24, MaxLineSearch = 16;
    private readonly CompiledModel _model;
    private readonly double[] _compliance, _mid, _trial, _next, _residual, _trialResidual, _change, _rates, _meteringSlopes;
    private readonly double[,] _jacobian;
    private readonly Factorization _factor;
    internal readonly double[] MidPressure, WallHeat;
    internal double BoundaryWork, RejectedHeat;
    internal int PressureCount => _mid.Length;
    private readonly double[] _headVolume, _headEnergy, _headGamma, _headTransfer, _restrictionSlopes;
    internal int PumpInlet(int pump)
    {
        int headspace = _model.HeadspaceByPump[pump];
        var c = _model.Components[_model.PumpComponents[pump]];
        return headspace >= 0 ? _model.HydraulicCount + headspace : c.C < 0 ? -1 : _model.Nodes[c.C].Index;
    }
    private int RestrictionOutlet(int restriction)
    {
        int headspace = _model.HeadspaceByRestriction[restriction];
        var c = _model.Components[_model.HydraulicComponents[restriction]];
        return headspace >= 0 ? _model.HydraulicCount + headspace : c.B < 0 ? -1 : _model.Nodes[c.B].Index;
    }
    internal void PrepareHeadspaces(LiquidTankState tanks, double[] energy)
    {
        for (int i = 0; i < _model.TankHeadspaces.Length; ++i)
        {
            var h = _model.TankHeadspaces[i];
            _headVolume[i] = h.Geometry.GasVolumeCubicMeters(tanks.Mass[h.Tank]);
            _headEnergy[i] = energy[h.Gas]; _headGamma[i] = _model.Gas!.Gases[h.Gas].Gamma;
        }
    }
    internal void InitialHeadspacePressures(double[] values, int offset)
    {
        for (int i = 0; i < _headVolume.Length; ++i)
            values[offset + _model.HydraulicCount + i] = (_headGamma[i] - 1) * _headEnergy[i] / _headVolume[i];
    }
    internal bool CommitHeadspaces(LiquidTankState tanks, double[] energy)
    {
        for (int i = 0; i < _model.TankHeadspaces.Length; ++i)
        {
            var h = _model.TankHeadspaces[i];
            double volume = h.Geometry.GasVolumeCubicMeters(tanks.Mass[h.Tank]);
            double expected = _headVolume[i] + _headTransfer[i];
            if (volume <= 0 || volume > h.Geometry.CapacityCubicMeters ||
                Math.Abs(volume - expected) > 128 * GearReference.Epsilon * h.Geometry.CapacityCubicMeters ||
                !LiquidTankHeadspace.TryPressureWork(_headVolume[i], _headTransfer[i], _headEnergy[i], _headGamma[i], out var work)) return false;
            energy[h.Gas] += work.GasEnergyChangeJoules;
            Numeric.Accumulate(-work.GasEnergyChangeJoules, ref tanks.HeadspaceWork![h.Tank], ref tanks.WorkCorrection![h.Tank]);
        }
        return true;
    }
    internal double RestrictionVolume(int slot, double duration) => duration * _rates[slot];
    internal double RestrictionLoss(int slot, double duration)
    {
        var c = _model.Components[_model.HydraulicComponents[slot]];
        int outlet = RestrictionOutlet(slot);
        return duration * _rates[slot] * (MidPressure[_model.Nodes[c.A].Index] - (outlet < 0 ? c.P2 : MidPressure[outlet]));
    }
    private readonly double[] _liquidSupplyVolume;
    private double _liquidSupplyDuration;
    internal void PrepareLiquidSupply(LiquidTankState state, double duration)
    {
        _liquidSupplyDuration = duration;
        Array.Fill(_liquidSupplyVolume, double.PositiveInfinity);
        foreach (var feed in _model.LiquidFeeds)
            if (feed.Tank >= 0) _liquidSupplyVolume[_model.Components[feed.Pump].Index] = state.Mass[feed.Tank] / feed.Density;
    }
    internal double PumpVolume(int pump, double speed, double duration)
    {
        double ordinary = duration * _model.Components[_model.PumpComponents[pump]].P0 * speed;
        return Math.Min(ordinary, _liquidSupplyVolume[pump]);
    }
    internal double PumpFlowLaw(int pump, double speed, out double effectiveDisplacement, out double displacementSlope, out double flowSlope)
    {
        double displacement = _model.Components[_model.PumpComponents[pump]].P0;
        double limit = _liquidSupplyVolume[pump];
        if (speed >= 0 && !double.IsPositiveInfinity(limit) && (speed == 0 && limit == 0 || displacement * speed > limit / _liquidSupplyDuration))
        {
            double flow = limit / _liquidSupplyDuration;
            effectiveDisplacement = speed == 0 ? 0 : flow / speed;
            displacementSlope = speed == 0 ? 0 : -effectiveDisplacement / speed;
            flowSlope = 0; return flow;
        }
        effectiveDisplacement = flowSlope = displacement; displacementSlope = 0;
        return displacement * speed;
    }
    internal HydraulicSolver(CompiledModel model)
    {
        _model = model; int count = model.HydraulicCount + model.TankHeadspaces.Length;
        _headVolume = new double[model.TankHeadspaces.Length]; _headEnergy = new double[_headVolume.Length];
        _headGamma = new double[_headVolume.Length]; _headTransfer = new double[_headVolume.Length];
        _restrictionSlopes = new double[model.HydraulicComponents.Length];
        _liquidSupplyVolume = new double[model.PumpComponents.Length]; Array.Fill(_liquidSupplyVolume, double.PositiveInfinity);
        _compliance = new double[count];
        foreach (var node in model.Nodes) if (node.Domain == Domain.Hydraulic) _compliance[node.Index] = node.Storage;
        _mid = new double[count]; _trial = new double[count]; _next = new double[model.HydraulicCount]; _residual = new double[count];
        _trialResidual = new double[count]; _change = new double[count]; MidPressure = new double[count];
        _rates = new double[model.HydraulicComponents.Length]; WallHeat = new double[model.ThermalCount];
        _meteringSlopes = new double[model.HasSpoolValves ? model.HydraulicComponents.Length : 0];
        _jacobian = new double[count, count]; _factor = new(count);
    }
    private double Residual(double[] old, double[] pressures, double[] inputs, double duration, double[] residual, bool derivatives,
        double[]? coordinates = null, int pistonOffset = 0, double[]? dynamics = null)
    {
        if (derivatives) Array.Clear(_jacobian, 0, _jacobian.Length);
        Array.Clear(residual, old.Length, residual.Length - old.Length);
        for (int i = 0; i < old.Length; ++i)
        { residual[i] = pressures[i] - old[i]; if (derivatives) _jacobian[i, i] = 1; }
        for (int k = 0; k < _rates.Length; ++k)
        {
            int index = _model.HydraulicComponents[k]; var c = _model.Components[index];
            int a = _model.Nodes[c.A].Index, b = RestrictionOutlet(k);
            double difference = pressures[a] - (b < 0 ? c.P2 : pressures[b]);
            double rate, slope;
            if (_model.SpoolValves[index] is { } metering)
            {
                double position = dynamics![metering.Position] + .5 * coordinates![pistonOffset + metering.Coordinate];
                if (!metering.Law.Evaluate(difference, position, out rate, out slope, out _meteringSlopes[k])) return double.PositiveInfinity;
            }
            else if (!_model.HydraulicLaws[index]!.Evaluate(difference, c.Kind == ComponentKind.HydraulicRelief ? 1 : inputs[index], out rate, out slope)) return double.PositiveInfinity;
            _rates[k] = rate; _restrictionSlopes[k] = slope;
            double left = .5 * duration / _compliance[a], right = b < 0 || b >= old.Length ? 0 : .5 * duration / _compliance[b];
            residual[a] += left * rate; if (b >= 0 && b < old.Length) residual[b] -= right * rate;
            if (!derivatives) continue;
            _jacobian[a, a] += left * slope;
            if (b >= 0) _jacobian[a, b] -= left * slope;
            if (b >= 0 && b < old.Length) { _jacobian[b, a] -= right * slope; _jacobian[b, b] += right * slope; }
        }
        double norm = 0;
        for (int i = 0; i < old.Length; ++i)
        {
            if (!Numeric.Finite(residual[i]) || !Numeric.Finite(pressures[i])) return double.PositiveInfinity;
            double tolerance = 2e-7 + 64 * GearReference.Epsilon * (Math.Abs(pressures[i]) + Math.Abs(old[i]));
            norm = Math.Max(norm, Math.Abs(residual[i]) / tolerance);
        }
        return norm;
    }
    internal bool Advance(HydraulicState state, double[] inputs, double duration, CancellationToken cancellation)
    {
        BoundaryWork = RejectedHeat = 0; Array.Clear(WallHeat, 0, WallHeat.Length);
        Array.Copy(state.Pressure, _mid, _mid.Length);
        bool converged = false;
        for (int iteration = 0; iteration < MaxIterations; ++iteration)
        {
            if (cancellation.IsCancellationRequested) return false;
            double norm = Residual(state.Pressure, _mid, inputs, duration, _residual, true);
            if (!Numeric.Finite(norm)) return false;
            if (norm <= 1) { converged = true; break; }
            for (int i = 0; i < _mid.Length; ++i) _change[i] = -_residual[i];
            if (!_factor.Refactor(_jacobian) || !_factor.Solve(_change)) return false;
            bool accepted = false; double fraction = 1;
            for (int search = 0; search < MaxLineSearch; ++search, fraction *= .5)
            {
                for (int i = 0; i < _mid.Length; ++i) _trial[i] = _mid[i] + fraction * _change[i];
                if (Residual(state.Pressure, _trial, inputs, duration, _trialResidual, false) < norm)
                { Array.Copy(_trial, _mid, _mid.Length); accepted = true; break; }
            }
            if (!accepted) return false;
        }
        if (!converged) return false;
        return Commit(state, duration);
    }

    // The pump's mechanical unknowns and fluid pressures share the same Newton matrix.
    internal double CoupledResidual(double[] old, double[] values, int offset, int pumpOffset, int pistonOffset, double[] inputs,
        double duration, double[] residual, double[,]? jacobian, double[] dynamics)
    {
        Array.Copy(values, offset, _mid, 0, _mid.Length);
        if (!Numeric.Finite(Residual(old, _mid, inputs, duration, _residual, jacobian is not null, values, pistonOffset, dynamics))) return double.PositiveInfinity;
        for (int i = 0; i < _mid.Length; ++i)
        {
            residual[offset + i] = _residual[i];
            if (jacobian is not null)
                for (int j = 0; j < values.Length; ++j) jacobian[offset + i, j] = j < offset ? 0 : _jacobian[i, j - offset];
        }
        if (jacobian is not null && _model.HasSpoolValves)
            for (int k = 0; k < _rates.Length; ++k)
            {
                int index = _model.HydraulicComponents[k];
                if (_model.SpoolValves[index] is not { } metering) continue;
                var c = _model.Components[index]; int a = _model.Nodes[c.A].Index, b = c.B < 0 ? -1 : _model.Nodes[c.B].Index;
                double slope = .25 * duration * _meteringSlopes[k];
                jacobian[offset + a, pistonOffset + metering.Coordinate] += slope / _compliance[a];
                if (b >= 0) jacobian[offset + b, pistonOffset + metering.Coordinate] -= slope / _compliance[b];
            }
        for (int k = 0; k < _model.PumpComponents.Length; ++k)
        {
            var c = _model.Components[_model.PumpComponents[k]];
            int outlet = _model.Nodes[c.B].Index, inlet = PumpInlet(k);
            double flow = PumpFlowLaw(k, values[pumpOffset + k], out _, out _, out double flowSlope);
            double delivery = .5 * duration / _compliance[outlet], intake = inlet < 0 || inlet >= old.Length ? 0 : .5 * duration / _compliance[inlet];
            residual[offset + outlet] -= delivery * flow;
            if (inlet >= 0 && inlet < old.Length) residual[offset + inlet] += intake * flow;
            if (jacobian is not null)
            {
                jacobian[offset + outlet, pumpOffset + k] -= delivery * flowSlope;
                if (inlet >= 0 && inlet < old.Length) jacobian[offset + inlet, pumpOffset + k] += intake * flowSlope;
            }
        }
        for (int k = 0; k < _model.PistonComponents.Length; ++k)
        {
            var c = _model.Components[_model.PistonComponents[k]]; int front = _model.Nodes[c.B].Index, back = c.C < 0 ? -1 : _model.Nodes[c.C].Index;
            double delta = values[pistonOffset + k], frontScale = .5 * c.P0 / _compliance[front], backScale = back < 0 ? 0 : .5 * c.P1 / _compliance[back];
            residual[offset + front] += frontScale * delta; if (back >= 0) residual[offset + back] -= backScale * delta;
            if (jacobian is not null)
            { jacobian[offset + front, pistonOffset + k] += frontScale; if (back >= 0) jacobian[offset + back, pistonOffset + k] -= backScale; }
        }
        for (int h = 0; h < _model.TankHeadspaces.Length; ++h)
        {
            var headspace = _model.TankHeadspaces[h];
            double withdrawal = duration * PumpFlowLaw(headspace.Pump, values[pumpOffset + headspace.Pump], out _, out _, out double flowSlope);
            foreach (int route in _model.ReturnsByFeed[headspace.Feed])
                withdrawal -= duration * _rates[_model.Components[_model.LiquidReturns[route].Valve].Index];
            if (Math.Abs(withdrawal) > .25 * _headVolume[h] || !LiquidTankHeadspace.TryPressureWork(_headVolume[h], withdrawal, _headEnergy[h], _headGamma[h], out var work)) return double.PositiveInfinity;
            int row = offset + _model.HydraulicCount + h;
            residual[row] = values[row] - work.MeanPressurePascals; _headTransfer[h] = withdrawal;
            if (jacobian is not null)
            {
                jacobian[row, row] = 1;
                jacobian[row, pumpOffset + headspace.Pump] = -work.PressureDerivativePascalsPerCubicMeter * duration * flowSlope;
                foreach (int route in _model.ReturnsByFeed[headspace.Feed])
                {
                    var valve = _model.Components[_model.LiquidReturns[route].Valve];
                    double slope = work.PressureDerivativePascalsPerCubicMeter * duration * _restrictionSlopes[valve.Index];
                    jacobian[row, offset + _model.Nodes[valve.A].Index] += slope;
                    jacobian[row, row] -= slope;
                }
            }
        }
        double norm = 0;
        for (int i = 0; i < _mid.Length; ++i)
        {
            if (!Numeric.Finite(residual[offset + i])) return double.PositiveInfinity;
            double previous = i < old.Length ? old[i] : (_headGamma[i - old.Length] - 1) * _headEnergy[i - old.Length] / _headVolume[i - old.Length];
            norm = Math.Max(norm, Math.Abs(residual[offset + i]) / (2e-7 + 64 * GearReference.Epsilon * (Math.Abs(_mid[i]) + Math.Abs(previous))));
        }
        return norm;
    }

    internal void PreviewPressure() => Array.Copy(_mid, MidPressure, _mid.Length);

    internal bool Commit(HydraulicState state, double duration, double[]? shaftMidpoint = null)
    {
        BoundaryWork = RejectedHeat = 0; Array.Clear(WallHeat, 0, WallHeat.Length);
        Array.Copy(state.Pressure, _next, _next.Length);
        // Commit pressures from the same pairwise transfers used by the volume ledger.
        for (int k = 0; k < _rates.Length; ++k)
        {
            var c = _model.Components[_model.HydraulicComponents[k]];
            int a = _model.Nodes[c.A].Index, b = c.B < 0 ? -1 : _model.Nodes[c.B].Index;
            double transfer = duration * _rates[k];
            _next[a] -= transfer / _compliance[a]; if (b >= 0) _next[b] += transfer / _compliance[b];
        }
        for (int k = 0; k < _model.PumpComponents.Length; ++k)
        {
            var c = _model.Components[_model.PumpComponents[k]];
            int outlet = _model.Nodes[c.B].Index, inlet = c.C < 0 ? -1 : _model.Nodes[c.C].Index;
            double transfer = PumpVolume(k, shaftMidpoint![_model.Nodes[c.A].Index + 1], duration);
            _next[outlet] += transfer / _compliance[outlet]; if (inlet >= 0) _next[inlet] -= transfer / _compliance[inlet];
        }
        foreach (int index in _model.PistonComponents)
        {
            var c = _model.Components[index]; int front = _model.Nodes[c.B].Index, back = c.C < 0 ? -1 : _model.Nodes[c.C].Index;
            double travel = duration * shaftMidpoint![_model.Nodes[c.A].Index + 1];
            _next[front] -= c.P0 * travel / _compliance[front]; if (back >= 0) _next[back] += c.P1 * travel / _compliance[back];
        }
        for (int i = 0; i < _next.Length; ++i)
        {
            if (!Numeric.Finite(_next[i]) || _next[i] < 0) return false; // No hidden cavitation clamp.
            MidPressure[i] = .5 * state.Pressure[i] + .5 * _next[i];
            double tolerance = 2e-7 + 64 * GearReference.Epsilon * (Math.Abs(_mid[i]) + Math.Abs(state.Pressure[i]));
            if (Math.Abs(MidPressure[i] - _mid[i]) > 4 * tolerance) return false;
        }
        for (int i = _next.Length; i < _mid.Length; ++i) MidPressure[i] = _mid[i];
        double volumeIn = 0;
        for (int k = 0; k < _rates.Length; ++k)
        {
            var c = _model.Components[_model.HydraulicComponents[k]];
            int a = _model.Nodes[c.A].Index, b = RestrictionOutlet(k);
            double transfer = duration * _rates[k], difference = MidPressure[a] - (b < 0 ? c.P2 : MidPressure[b]);
            double heat = transfer * difference;
            if (!Numeric.Finite(heat) || heat < 0) return false;
            if (b < 0 || b >= _next.Length) volumeIn -= transfer;
            if (b < 0) BoundaryWork -= transfer * c.P2;
            double externalHeat = (1 - _model.ReturnHeatFractions[_model.HydraulicComponents[k]]) * heat;
            if (c.Heat < 0) RejectedHeat += externalHeat; else WallHeat[_model.Nodes[c.Heat].Index] += externalHeat;
            state.Flow[k] += transfer; state.Power[k] += heat;
            Numeric.Accumulate(heat, ref state.Heat[k], ref state.Correction[k]);
        }
        for (int k = 0; k < _model.PumpComponents.Length; ++k)
        {
            var c = _model.Components[_model.PumpComponents[k]];
            int outlet = _model.Nodes[c.B].Index, inlet = PumpInlet(k);
            double speed = shaftMidpoint![_model.Nodes[c.A].Index + 1];
            double flow = PumpFlowLaw(k, speed, out double displacement, out _, out _);
            double pin = inlet < 0 ? c.P2 : MidPressure[inlet], difference = MidPressure[outlet] - pin;
            double work = duration * flow * difference;
            if (inlet < 0 || inlet >= _next.Length) volumeIn += duration * flow;
            if (inlet < 0) BoundaryWork += duration * flow * pin;
            state.PumpFlow[k] += duration * flow; state.PumpTorque[k] -= duration * displacement * difference; state.PumpPower[k] += work;
            Numeric.Accumulate(work, ref state.PumpWork[k], ref state.PumpCorrection[k]);
        }
        foreach (int index in _model.PistonComponents)
        {
            var c = _model.Components[index];
            if (c.C < 0)
            {
                double backTransfer = duration * c.P1 * shaftMidpoint![_model.Nodes[c.A].Index + 1];
                volumeIn -= backTransfer; BoundaryWork -= c.P2 * backTransfer;
            }
        }
        Array.Copy(_next, state.Pressure, _next.Length);
        Numeric.Accumulate(volumeIn, ref state.VolumeIn, ref state.VolumeCorrection);
        Numeric.Accumulate(BoundaryWork, ref state.Work, ref state.WorkCorrection);
        foreach (double heat in WallHeat) if (!Numeric.Finite(heat)) return false;
        return state.Finite() && Numeric.Finite(BoundaryWork) && Numeric.Finite(RejectedHeat);
    }
}
