// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed partial class CompiledModel
{
    private void ValidateLiquidPhaseSources()
    {
        foreach (var feed in LiquidFeeds)
        {
            var phase = FuelFilms[LiquidFuelInjectors[feed.Injector].Film].Phase;
            if (phase is null || feed.Tank >= 0 && LiquidTanks[feed.Tank].InitialMass == 0) continue;
            int headspace = feed.Tank < 0 ? -1 : HeadspaceByTank[feed.Tank];
            double pressure = headspace < 0 ? Components[feed.Pump].P2 :
                Nodes.First(node => node.Domain == Domain.Gas && node.Index == TankHeadspaces[headspace].Gas).Position;
            Require(phase.TrySaturationPressure(feed.SourceTemperature, out double saturation) && FuelPhaseEquilibrium.PressureAllowsLiquid(pressure, saturation),
                DiagnosticCode.Range, Components[feed.Component].Id, "liquid_rail_feed.inlet_pressure",
                "Absolute liquid inlet pressure must be at least saturation pressure; raise source pressure or lower liquid temperature. Two-phase pump flow is unsupported.");
        }
    }
}

public sealed partial class Simulation
{
    private double LiquidRailTemperature(State state, int injector, LiquidRailSample rail)
    {
        var definition = _model.LiquidFuelInjectors[injector];
        return definition.PressureNode < 0 || rail.MassKilograms == 0 ? definition.SupplyTemperature :
            _model.FuelFilms[definition.Film].Law.Temperature(new(rail.MassKilograms, state.LiquidFeeds!.RailEnergy[injector]));
    }

    private double LiquidSourceTemperature(State state, int feed)
    {
        var definition = _model.LiquidFeeds[feed];
        if (definition.Tank < 0 || state.LiquidTanks!.Mass[definition.Tank] == 0) return definition.SourceTemperature;
        return _model.FuelFilms[_model.LiquidFuelInjectors[definition.Injector].Film].Law.Temperature(
            new(state.LiquidTanks.Mass[definition.Tank], state.LiquidTanks.Energy[definition.Tank]));
    }

    private double LiquidSourcePressure(State state, int feed)
    {
        var definition = _model.LiquidFeeds[feed];
        int headspace = definition.Tank < 0 ? -1 : _model.HeadspaceByTank[definition.Tank];
        return headspace < 0 ? _model.Components[definition.Pump].P2 : GasPressure(state, _model.TankHeadspaces[headspace].Gas);
    }

    private double LiquidRailSaturation(State state, int injector, LiquidRailSample rail)
    {
        var phase = _model.FuelFilms[_model.LiquidFuelInjectors[injector].Film].Phase!;
        return phase.TrySaturationPressure(LiquidRailTemperature(state, injector, rail), out double pressure) ? pressure : double.NaN;
    }

    private double LiquidSourceSaturation(State state, int feed)
    {
        var phase = _model.FuelFilms[_model.LiquidFuelInjectors[_model.LiquidFeeds[feed].Injector].Film].Phase!;
        return phase.TrySaturationPressure(LiquidSourceTemperature(state, feed), out double pressure) ? pressure : double.NaN;
    }

    private bool LiquidPhasesValid(State state)
    {
        for (int i = 0; i < _model.LiquidFuelInjectors.Length; ++i)
        {
            if (_model.FuelFilms[_model.LiquidFuelInjectors[i].Film].Phase is null) continue;
            var rail = _model.RailSample(i, state.LiquidInjectors!, state.Hydraulic);
            double saturation = LiquidRailSaturation(state, i, rail);
            if (!Numeric.Finite(saturation) || rail.MassKilograms > 0 && !FuelPhaseEquilibrium.PressureAllowsLiquid(rail.PressurePascals, saturation)) return false;
        }
        for (int i = 0; i < _model.LiquidFeeds.Length; ++i)
        {
            var feed = _model.LiquidFeeds[i];
            if (_model.FuelFilms[_model.LiquidFuelInjectors[feed.Injector].Film].Phase is null) continue;
            double saturation = LiquidSourceSaturation(state, i);
            bool wet = feed.Tank < 0 || state.LiquidTanks!.Mass[feed.Tank] > 0;
            if (!Numeric.Finite(saturation) || wet && !FuelPhaseEquilibrium.PressureAllowsLiquid(LiquidSourcePressure(state, i), saturation)) return false;
        }
        return true;
    }
}
