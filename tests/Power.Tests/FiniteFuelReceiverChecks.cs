// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

internal static class FiniteFuelReceiverChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("finite receiver / variable cavity state / unchanged tank wrapper and displacement", State),
        ("finite receiver / independent ideal-gas adiabatic work and entropy", Dry),
        ("finite receiver / closed wet pressure work and finite-bath second law", Wet)
    ];
    private static FiniteTankFuelEquilibrium Law() => new(FrozenFuelNozzleChecks.Material(), 1e-4, 750);
    private static void State()
    {
        var law = Law(); Require(law.TryPoint(350, .002, .0005, out var point)); Require(law.TryPointAtCapacity(350, .002, .0005, 1e-4, out var same) && same == point);
        Near(point.GasVolumeCubicMeters + point.LiquidMassKilograms / 750, 1e-4, 1e-20);
        Require(law.TryFlashEnergyAtCapacity(.002, .0005, point.ThermalEnergyJoules, 1e-4, out var state)); Near(state.TemperatureKelvin, 350, 1e-9);
        Require(law.TryFlash(state.Liquid, state.VaporMassKilograms, .0005, state.GasEnergyJoules, out var old)); Near(old.TemperatureKelvin, state.TemperatureKelvin, 1e-9);
        Require(!law.TryPointAtCapacity(350, .002, .0005, 1e-7, out _));
    }
    private static void Dry()
    {
        var law = Law(); const double carrier = .0001, energy = carrier * 717.5 * 350;
        foreach (double next in new[] { 8e-5, 1.25e-4 })
        {
            Require(law.TryAdiabaticCapacity(0, carrier, energy, 1e-4, next, out var work));
            double temperature = 350 * Math.Pow(1e-4 / next, .4);
            Near(work.State.TemperatureKelvin, temperature, 1e-9); Near(work.EnergyChangeJoules, carrier * 717.5 * temperature - energy, 1e-11); Near(work.EntropyResidualJoulesPerKelvin, 0, 1e-12);
        }
    }
    private static void Wet()
    {
        var law = Law(); const double fuel = .002, carrier = .0005, wallT = 500, wallC = 2;
        Require(law.TryPoint(350, fuel, carrier, out var initial)); double entropy = law.Entropy(initial, carrier);
        Require(law.TryAdiabaticCapacity(fuel, carrier, initial.ThermalEnergyJoules, 1e-4, 1.2e-4, out var work));
        Near(work.EntropyResidualJoulesPerKelvin, 0, 2e-11); Require(work.EnergyChangeJoules < 0);
        Require(law.TryHeatAtCapacity(fuel, carrier, initial.ThermalEnergyJoules, 1e-4, wallT, wallC, .1, .01, out var heat));
        Require(law.TryPoint(heat.TemperatureKelvin, fuel, carrier, out var point));
        Near(heat.Liquid.ThermalEnergyJoules + heat.GasEnergyJoules + wallC * heat.WallTemperatureKelvin, initial.ThermalEnergyJoules + wallC * wallT, 1e-10);
        Require(law.Entropy(point, carrier) - entropy + wallC * Math.Log(heat.WallTemperatureKelvin / wallT) >= -1e-12);
    }
}
