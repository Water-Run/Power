// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed partial class Simulation
{
    private bool EquilibrateTanks(State state)
    {
        foreach (var headspace in _model.TankHeadspaces)
        {
            if (headspace.Equilibrium is not { } law) continue;
            int tank = headspace.Tank, gas = headspace.Gas; var liquid = state.LiquidTanks!; var mixture = state.Mixture!;
            if (!law.TryFlash(new(liquid.Mass[tank], liquid.Energy[tank]), mixture.Fuel[gas], mixture.Air[gas] + mixture.Products[gas], state.Energy[gas], out var transfer)) return false;
            liquid.Mass[tank] = transfer.Liquid.LiquidMassKilograms; liquid.MassCorrection[tank] = 0;
            liquid.Energy[tank] = transfer.Liquid.ThermalEnergyJoules; liquid.EnergyCorrection[tank] = 0;
            mixture.Fuel[gas] = transfer.VaporMassKilograms;
            state.Mass[gas] = mixture.Fuel[gas] + mixture.Air[gas] + mixture.Products[gas]; state.Energy[gas] = transfer.GasEnergyJoules;
            liquid.TickPhase![tank] += transfer.NetEvaporatedMassKilograms;
        }
        return true;
    }
}
