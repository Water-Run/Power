// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed partial class CompiledModel
{
    internal int[] FiniteFilmByGas { get; private set; } = [];
    public bool HasFiniteFuelReceivers { get; private set; }

    internal bool FiniteReceiverWork(int component, FuelFilmState films, MixtureState mixture, double[] gasEnergy,
        double angle, double delta, double previewHeat, out FiniteFuelReceiverWork work, out double torque)
    {
        work = default; torque = double.NaN;
        int gas = Nodes[Components[component].B].Index, filmSlot = FiniteFilmByGas[gas];
        var film = FuelFilms[filmSlot]; var geometry = GasCylinders[component]!;
        double before = geometry.GeometryAt(angle).VolumeCubicMeters;
        double gradient = geometry.Geometry.VolumeGradient(angle % (2 * Math.PI) + geometry.Phase % (2 * Math.PI), delta);
        double after = before + gradient * delta;
        double fuel = films.Mass[filmSlot] + mixture.Fuel[gas], carrier = mixture.Air[gas] + mixture.Products[gas];
        if (!film.FiniteVolume!.TryAdiabaticCapacity(fuel, carrier, films.Energy[filmSlot] + gasEnergy[gas] + previewHeat,
            before, after, out work)) return false;
        torque = (work.MeanPressurePascals - geometry.BackPressure) * gradient;
        return Numeric.Finite(torque);
    }
}

public sealed partial class Simulation
{
    private bool EquilibrateFiniteReceivers(State state)
    {
        for (int i = 0; i < _model.FuelFilms.Length; ++i)
        {
            var film = _model.FuelFilms[i]; if (film.FiniteVolume is not { } finite) continue;
            double fuel = state.Films!.Mass[i] + state.Mixture!.Fuel[film.Gas], carrier = state.Mixture.Air[film.Gas] + state.Mixture.Products[film.Gas];
            if (!finite.TryFlashEnergyAtCapacity(fuel, carrier, state.Films.Energy[i] + state.Energy[film.Gas],
                _model.Gas!.CavityVolumeAt(film.Gas, state.X, state.LiquidTanks), out var point)) return false;
            CommitFiniteReceiver(state, i, point);
        }
        return true;
    }
    private void CommitFiniteReceiver(State state, int filmSlot, FiniteFuelPhaseState point)
    {
        var film = _model.FuelFilms[filmSlot]; double oldLiquid = state.Films!.Mass[filmSlot];
        state.Films.Mass[filmSlot] = point.Liquid.LiquidMassKilograms; state.Films.Energy[filmSlot] = point.Liquid.ThermalEnergyJoules;
        state.Films.TickVapor[filmSlot] += oldLiquid - point.Liquid.LiquidMassKilograms;
        state.Mixture!.Fuel[film.Gas] = point.VaporMassKilograms;
        state.Mass[film.Gas] = point.VaporMassKilograms + state.Mixture.Air[film.Gas] + state.Mixture.Products[film.Gas];
        state.Energy[film.Gas] = point.GasEnergyJoules;
    }
}
