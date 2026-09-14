// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;

namespace Power.Tests;

/// <summary>
/// Evidence for the gas-exchange primitives. Each check compares the implementation against an
/// independently written closed form rather than against a recorded output of the same code.
/// </summary>
internal static class GasChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("ideal gas properties / critical ratio / choking continuity", Properties),
        ("orifice choked and subcritical flow against the closed form", Nozzle),
        ("orifice reverse flow / closed valve / opening fraction / rejected states", Contracts),
        ("adiabatic vessel blowdown against the analytic solution", Blowdown),
        ("reservoir filling enthalpy and the evacuated-vessel limit", Filling),
        ("closed two-volume network mass, energy and pressure equalisation", Network)
    ];

    private const double Cd = 0.85, Area = 3.2e-5;
    private static readonly IdealGas Air = IdealGas.Air;

    private static void Properties()
    {
        Near(Air.IsochoricHeatCapacityJoulePerKilogramKelvin, 287 / 0.4, 1e-12);
        Near(Air.IsobaricHeatCapacityJoulePerKilogramKelvin, 1.4 * 287 / 0.4, 1e-12);
        Near(Air.CriticalPressureRatio, Math.Pow(2.0 / 2.4, 3.5), 1e-15);
        Near(Air.CriticalPressureRatio, 0.5282817877171742, 1e-15);
        Near(Air.Pressure(1.2, 300), 1.2 * 287 * 300, 1e-9);
        Near(Air.SpecificEnthalpy(300), 1.4 * 287 / 0.4 * 300, 1e-9);

        foreach (double gamma in new[] { 1.1, 1.3, 1.4, 5.0 / 3 })
        {
            var gas = new IdealGas(297, gamma);
            // The subcritical branch must reach the choked coefficient exactly at the critical ratio.
            double subcritical = gas.SubcriticalMassFluxCoefficient * Math.Sqrt(Orifice.SubcriticalFlowFunction(gas.CriticalPressureRatio, gamma));
            Near(subcritical, gas.ChokedMassFluxCoefficient, 1e-9 * gas.ChokedMassFluxCoefficient);
            // Monotone decreasing towards zero as the pressure ratio approaches one, with no cancellation.
            double previous = double.PositiveInfinity;
            foreach (double ratio in new[] { 0.6, 0.9, 0.99, 0.999 })
            {
                double value = Orifice.SubcriticalFlowFunction(ratio, gamma);
                Require(value > 0 && value < previous, $"flow function not monotone at {ratio}");
                Near(value, Math.Pow(ratio, 2 / gamma) * (1 - Math.Pow(ratio, (gamma - 1) / gamma)), 1e-9 * value);
                previous = value;
            }
            foreach (double requested in new[] { 1e-6, 1e-12, 1e-15 })
            {
                // The stored ratio, not the requested offset, is what the function can resolve:
                // 1 - 1e-12 only carries about four digits of that offset in binary64.
                double ratio = 1 - requested, epsilon = 1 - ratio;
                double value = Orifice.SubcriticalFlowFunction(ratio, gamma);
                Require(value > 0 && value < previous, $"flow function not monotone at 1-{requested}");
                // Leading order f = (gamma-1)/gamma * epsilon. The naive difference of two nearly equal
                // powers keeps only a few digits here, which is why the implementation uses expm1.
                Near(value, (gamma - 1) / gamma * epsilon, 10 * epsilon * value);
                previous = value;
            }
            Require(Orifice.SubcriticalFlowFunction(1, gamma) == 0);
        }
        Throws<ArgumentOutOfRangeException>(() => Orifice.SubcriticalFlowFunction(0, 1.4));
        Throws<ArgumentOutOfRangeException>(() => Orifice.SubcriticalFlowFunction(1.0000001, 1.4));
        Throws<ArgumentOutOfRangeException>(() => Orifice.SubcriticalFlowFunction(0.5, 1));
        Throws<ArgumentException>(() => new IdealGas(0, 1.4));
        Throws<ArgumentException>(() => new IdealGas(287, 1));
        Throws<ArgumentException>(() => new IdealGas(287, double.NaN));
    }

    private static void Nozzle()
    {
        var orifice = new Orifice(Area, Cd);
        double area = Area * Cd;
        foreach (double upstream in new[] { 1.5e5, 6e5, 2.5e6 })
            foreach (double temperature in new[] { 280.0, 700.0, 1400.0 })
                foreach (double ratio in new[] { 0.02, 0.2, 0.5282817877171742, 0.7, 0.95, 0.999 })
                {
                    double downstream = upstream * ratio;
                    var flow = orifice.Evaluate(Air, upstream, temperature, downstream, 350, 1);
                    // Independent closed form: NASA compressible mass-flow relations, written out in full.
                    const double g = 1.4, r = 287;
                    double expected = ratio <= Math.Pow(2 / (g + 1), g / (g - 1))
                        ? area * upstream / Math.Sqrt(temperature) * Math.Sqrt(g / r) * Math.Pow(2 / (g + 1), (g + 1) / (2 * (g - 1)))
                        : area * upstream / Math.Sqrt(temperature) *
                          Math.Sqrt(2 * g / (r * (g - 1)) * (Math.Pow(ratio, 2 / g) - Math.Pow(ratio, (g + 1) / g)));
                    Near(flow.MassFlowKilogramsPerSecond, expected, 1e-11 * expected);
                    Require(flow.Choked == (ratio <= Air.CriticalPressureRatio));
                    // Enthalpy is carried at the upstream temperature, never the downstream one.
                    Near(flow.EnthalpyFlowWatts, expected * Air.SpecificEnthalpy(temperature), 1e-9 * expected * Air.SpecificEnthalpy(temperature));
                }
        // Below the critical ratio the flow no longer depends on the downstream pressure.
        var a = orifice.Evaluate(Air, 1e6, 500, 1e5, 300, 1);
        var b = orifice.Evaluate(Air, 1e6, 500, 2e5, 300, 1);
        Require(a.Choked && b.Choked && a.MassFlowKilogramsPerSecond == b.MassFlowKilogramsPerSecond);
        Require(orifice.Evaluate(Air, 1e6, 500, 8e5, 300, 1).MassFlowKilogramsPerSecond < a.MassFlowKilogramsPerSecond);
    }

    private static void Contracts()
    {
        var orifice = new Orifice(Area, Cd);
        var forward = orifice.Evaluate(Air, 5e5, 600, 1e5, 300, 1);
        var reverse = orifice.Evaluate(Air, 1e5, 300, 5e5, 600, 1);
        Require(forward.MassFlowKilogramsPerSecond > 0 && reverse.MassFlowKilogramsPerSecond < 0);
        Require(reverse.MassFlowKilogramsPerSecond == -forward.MassFlowKilogramsPerSecond);
        Require(reverse.EnthalpyFlowWatts == -forward.EnthalpyFlowWatts && reverse.Choked == forward.Choked);

        Require(orifice.Evaluate(Air, 5e5, 600, 1e5, 300, 0).MassFlowKilogramsPerSecond == 0);
        Require(orifice.Evaluate(Air, 5e5, 600, 5e5, 300, 1).MassFlowKilogramsPerSecond == 0);
        Near(orifice.Evaluate(Air, 5e5, 600, 1e5, 300, 0.25).MassFlowKilogramsPerSecond,
            0.25 * forward.MassFlowKilogramsPerSecond, 1e-18);
        Near(orifice.EffectiveArea(0.5), 0.5 * Area * Cd, 1e-20);

        Throws<ArgumentOutOfRangeException>(() => orifice.EffectiveArea(1.0000001));
        Throws<ArgumentOutOfRangeException>(() => orifice.EffectiveArea(-1e-9));
        Require(orifice.EffectiveArea(-0.0) == 0, "negative zero is a closed orifice, not an invalid opening");
        Throws<ArgumentOutOfRangeException>(() => orifice.Evaluate(Air, 5e5, 600, 1e5, 300, double.NaN));
        Throws<ArgumentException>(() => orifice.Evaluate(Air, 5e5, 600, 0, 300, 1));
        Throws<ArgumentException>(() => orifice.Evaluate(Air, 5e5, -1, 1e5, 300, 1));
        Throws<ArgumentNullException>(() => orifice.Evaluate(null!, 5e5, 600, 1e5, 300, 1));
        Throws<ArgumentException>(() => new Orifice(0));
        Throws<ArgumentException>(() => new Orifice(Area, 1.5));

        var state = GasVolumeState.FromPressure(Air, 1e-3, 4e5, 450);
        Require(state.IsPhysical);
        Near(state.Pressure(Air), 4e5, 1e-7);
        Near(state.Temperature(Air), 450, 1e-11);
        Near(state.MassKilograms, 4e5 * 1e-3 / (287 * 450), 1e-18);
        Near(state.DensityKilogramsPerCubicMeter, 4e5 / (287 * 450), 1e-15);
        Near(state.SpecificEnthalpy(Air), 1.4 * 287 / 0.4 * 450, 1e-9);
        Require(!new GasVolumeState(0, 1, 1).IsPhysical && !new GasVolumeState(1, 1, 0).IsPhysical);
        Require(!new GasVolumeState(1, double.NaN, 1).IsPhysical);
        Throws<ArgumentException>(() => GasVolumeState.FromPressure(Air, 1e-3, 4e5, 0));
        Throws<ArgumentException>(() => GasVolumeState.FromPressure(Air, 0, 4e5, 450));
    }

    // Explicit fourth-order integration of one control volume; the solver under test is the flow model.
    private static GasVolumeState Integrate(GasVolumeState state, double seconds, int steps,
        Func<GasVolumeState, (double Mass, double Energy)> rates)
    {
        double h = seconds / steps;
        for (int i = 0; i < steps; ++i)
        {
            var k1 = rates(state);
            var k2 = rates(state.Add(0.5 * h * k1.Mass, 0.5 * h * k1.Energy));
            var k3 = rates(state.Add(0.5 * h * k2.Mass, 0.5 * h * k2.Energy));
            var k4 = rates(state.Add(h * k3.Mass, h * k3.Energy));
            state = state.Add(h / 6 * (k1.Mass + 2 * k2.Mass + 2 * k3.Mass + k4.Mass),
                h / 6 * (k1.Energy + 2 * k2.Energy + 2 * k3.Energy + k4.Energy));
            Require(state.IsPhysical, "vessel state left the physical region");
        }
        return state;
    }

    private static void Blowdown()
    {
        var orifice = new Orifice(Area, Cd);
        const double volume = 2e-3, p0 = 2e6, t0 = 900, back = 1e5, duration = 0.4;
        var initial = GasVolumeState.FromPressure(Air, volume, p0, t0);
        (double, double) Rates(GasVolumeState s)
        {
            var flow = orifice.Evaluate(Air, s.Pressure(Air), s.Temperature(Air), back, 300, 1);
            Require(flow.Choked, "the analytic comparison is only valid while the nozzle is choked");
            return (-flow.MassFlowKilogramsPerSecond, -flow.EnthalpyFlowWatts);
        }

        // Uniform adiabatic blowdown keeps the remaining gas isentropic, so density follows
        // rho(t) = (rho0^-k + k*C*t/V)^(-1/k) with k = (gamma-1)/2 and C = A*K*R*sqrt(T0)*rho0^-k.
        const double k = 0.2;
        double density0 = initial.DensityKilogramsPerCubicMeter;
        double c = Area * Cd * Air.ChokedMassFluxCoefficient * 287 * Math.Sqrt(t0) * Math.Pow(density0, -k);
        double expectedDensity = Math.Pow(Math.Pow(density0, -k) + k * c * duration / volume, -1 / k);
        double expectedTemperature = t0 * Math.Pow(expectedDensity / density0, 0.4);

        var coarse = Integrate(initial, duration, 2_000, s => Rates(s));
        var fine = Integrate(initial, duration, 8_000, s => Rates(s));
        Near(fine.DensityKilogramsPerCubicMeter, expectedDensity, 1e-9 * expectedDensity);
        Near(fine.Temperature(Air), expectedTemperature, 1e-9 * expectedTemperature);
        Near(fine.Pressure(Air), expectedDensity * 287 * expectedTemperature, 1e-8 * fine.Pressure(Air));
        Require(fine.Pressure(Air) / back > 1 / Air.CriticalPressureRatio, "the run must stay choked");
        // Refinement must reduce the error; 2000 steps is already close, so require a strict improvement.
        double coarseError = Math.Abs(coarse.DensityKilogramsPerCubicMeter - expectedDensity);
        double fineError = Math.Abs(fine.DensityKilogramsPerCubicMeter - expectedDensity);
        Require(fineError < coarseError, $"refinement did not converge: {coarseError} then {fineError}");
    }

    private static void Filling()
    {
        var orifice = new Orifice(Area, Cd);
        const double volume = 5e-4, supply = 6e5, supplyTemperature = 320, target = 0.98 * supply;
        foreach (double start in new[] { 1e2, 1.0 })
        {
            var initial = GasVolumeState.FromPressure(Air, volume, start, 290);
            var filled = initial;
            // Charge until the vessel is nearly at supply pressure. Stopping short of equality keeps the
            // flow one-directional, which is the condition under which the enthalpy identity below holds.
            int steps = 0;
            while (filled.Pressure(Air) < target)
            {
                Require(++steps <= 400_000, "the vessel did not reach the target pressure");
                filled = Integrate(filled, 1e-6, 1, s =>
                {
                    var flow = orifice.Evaluate(Air, supply, supplyTemperature, s.Pressure(Air), s.Temperature(Air), 1);
                    Require(flow.MassFlowKilogramsPerSecond > 0, "charging must not reverse before the target");
                    return (flow.MassFlowKilogramsPerSecond, flow.EnthalpyFlowWatts);
                });
            }
            Require(filled.Pressure(Air) >= target && filled.Pressure(Air) < supply);
            // Every unit of mass enters carrying the reservoir enthalpy, so dU = cp*T_supply*dm exactly.
            double transported = (filled.MassKilograms - initial.MassKilograms) * Air.SpecificEnthalpy(supplyTemperature);
            Near(filled.InternalEnergyJoules - initial.InternalEnergyJoules, transported, 1e-9 * transported);
            // Charging an evacuated rigid vessel settles at gamma times the supply temperature.
            Near(filled.Temperature(Air), 1.4 * supplyTemperature, start < 10 ? 0.01 : 0.2);
        }
    }

    private static void Network()
    {
        var orifice = new Orifice(Area, Cd);
        var a = GasVolumeState.FromPressure(Air, 1.5e-3, 9e5, 800);
        var b = GasVolumeState.FromPressure(Air, 4e-4, 1.1e5, 290);
        double mass0 = a.MassKilograms + b.MassKilograms, energy0 = a.InternalEnergyJoules + b.InternalEnergyJoules;
        const double h = 2e-5;
        for (int i = 0; i < 100_000; ++i)
        {
            var flow = orifice.Evaluate(Air, a.Pressure(Air), a.Temperature(Air), b.Pressure(Air), b.Temperature(Air), 1);
            double mass = h * flow.MassFlowKilogramsPerSecond, energy = h * flow.EnthalpyFlowWatts;
            a = a.Add(-mass, -energy);
            b = b.Add(mass, energy);
            Require(a.IsPhysical && b.IsPhysical, "a network volume left the physical region");
        }
        // Internal transfers cancel: the closed network conserves mass and energy to rounding.
        Near(a.MassKilograms + b.MassKilograms, mass0, 1e-14 * mass0);
        Near(a.InternalEnergyJoules + b.InternalEnergyJoules, energy0, 1e-12 * energy0);
        // Equilibrium equalises pressure. Temperatures stay apart because the volumes exchange enthalpy.
        Near(a.Pressure(Air), b.Pressure(Air), 1e-6 * a.Pressure(Air));
        Require(a.Temperature(Air) < 800, "the discharging volume must cool as it expands");
        Require(b.Temperature(Air) > 290, "the receiving volume must heat as it is charged");
        // Equilibrium is mechanical, not thermal: the volumes do not reach the fully mixed temperature.
        double mixed = (a.InternalEnergyJoules + b.InternalEnergyJoules) /
            ((a.MassKilograms + b.MassKilograms) * Air.IsochoricHeatCapacityJoulePerKilogramKelvin);
        Require(Math.Abs(a.Temperature(Air) - mixed) > 1 || Math.Abs(b.Temperature(Air) - mixed) > 1,
            "a pressure-driven exchange must not land on the fully mixed temperature");
    }
}
