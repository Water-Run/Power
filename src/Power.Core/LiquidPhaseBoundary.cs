// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed partial class CompiledModel
{
    internal FiniteTankFuelEquilibrium? TankPhaseForInjector(int injector)
    {
        if (!HasEquilibriumTanks) return null;
        int feed = FeedByInjector[injector], tank = feed < 0 ? -1 : LiquidFeeds[feed].Tank;
        int headspace = tank < 0 ? -1 : HeadspaceByTank[tank];
        return headspace < 0 ? null : TankHeadspaces[headspace].Equilibrium;
    }
    internal bool TryLiquidSaturation(int injector, double temperature, double pressure, out double saturation)
    {
        var finite = TankPhaseForInjector(injector);
        return finite is not null ? finite.TrySaturationPressure(temperature, pressure, out saturation) :
            FuelFilms[LiquidFuelInjectors[injector].Film].Phase!.TrySaturationPressure(temperature, out saturation);
    }
    private void ValidateLiquidPhaseSources()
    {
        foreach (var feed in LiquidFeeds)
        {
            var phase = FuelFilms[LiquidFuelInjectors[feed.Injector].Film].Phase;
            if (phase is null) continue;
            int headspace = feed.Tank < 0 ? -1 : HeadspaceByTank[feed.Tank];
            if (headspace >= 0 && TankHeadspaces[headspace].Equilibrium is { } finite)
            {
                var rail = LiquidFuelInjectors[feed.Injector];
                Require(rail.Rail.InitialMassKilograms == 0 || finite.TrySaturationPressure(rail.SupplyTemperature, rail.Rail.InitialPressurePascals, out double corrected) &&
                    FuelPhaseEquilibrium.PressureAllowsLiquid(rail.Rail.InitialPressurePascals, corrected),
                    DiagnosticCode.Range, Components[rail.Component].Id, "liquid_fuel_injector.initial_pressure", "Use single-phase rail pressure above the finite-density material's pressure-corrected saturation.");
                continue;
            }
            if (feed.Tank >= 0 && LiquidTanks[feed.Tank].InitialMass == 0) continue;
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
        var finite = _model.TankPhaseForInjector(definition.Injector);
        if (finite is not null && state.LiquidTanks!.Mass[definition.Tank] == 0)
        {
            int gas = _model.TankHeadspaces[_model.HeadspaceByTank[definition.Tank]].Gas;
            return state.Energy[gas] / state.Mass[gas] / finite.Material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin;
        }
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
        return _model.TryLiquidSaturation(injector, LiquidRailTemperature(state, injector, rail), rail.PressurePascals, out double pressure) ? pressure : double.NaN;
    }

    private double LiquidSourceSaturation(State state, int feed)
    {
        return _model.TryLiquidSaturation(_model.LiquidFeeds[feed].Injector, LiquidSourceTemperature(state, feed), LiquidSourcePressure(state, feed), out double pressure) ? pressure : double.NaN;
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
            if (!Numeric.Finite(saturation) || wet && _model.TankPhaseForInjector(feed.Injector) is null && !FuelPhaseEquilibrium.PressureAllowsLiquid(LiquidSourcePressure(state, i), saturation)) return false;
        }
        return true;
    }
}
