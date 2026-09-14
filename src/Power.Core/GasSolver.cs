// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Immutable gas topology, resolved once per compiled model and shared by every simulation.</summary>
internal sealed class GasNetwork
{
    internal readonly int[] NodeIndex;                  // node array position, per gas index
    internal readonly IdealGas[] Gases;                 // composition, per gas index
    internal readonly double[] Volume, InitialMass, InitialEnergy;
    internal readonly int[] OrificeComponent, OrificeA, OrificeB; // OrificeB is -1 for a reservoir
    internal readonly Orifice[] Restriction;
    internal readonly int[] HeatComponent, HeatGas, HeatWall;
    internal readonly int[] Slot;                       // component index -> orifice or heat-link slot, else -1

    internal GasNetwork(CompiledModel model)
    {
        int count = model.GasCount;
        NodeIndex = new int[count]; Gases = new IdealGas[count];
        Volume = new double[count]; InitialMass = new double[count]; InitialEnergy = new double[count];
        for (int i = 0; i < model.Nodes.Length; ++i)
        {
            var n = model.Nodes[i];
            if (n.Domain != Domain.Gas) continue;
            var composition = model.NodeGases[i]!;
            NodeIndex[n.Index] = i; Gases[n.Index] = composition; Volume[n.Index] = n.Storage;
            InitialMass[n.Index] = n.Position * n.Storage / (composition.GasConstantJoulePerKilogramKelvin * n.Initial);
            InitialEnergy[n.Index] = composition.InternalEnergy(InitialMass[n.Index], n.Initial);
        }
        Slot = new int[model.ComponentCount];
        Array.Fill(Slot, -1);
        var orifices = new List<int>();
        var links = new List<int>();
        for (int i = 0; i < model.ComponentCount; ++i)
        {
            if (model.Components[i].Kind == ComponentKind.GasOrifice) orifices.Add(i);
            else if (model.Components[i].Kind == ComponentKind.GasHeatLink) links.Add(i);
        }
        OrificeComponent = orifices.ToArray();
        OrificeA = new int[OrificeComponent.Length]; OrificeB = new int[OrificeComponent.Length];
        Restriction = new Orifice[OrificeComponent.Length];
        for (int k = 0; k < OrificeComponent.Length; ++k)
        {
            var c = model.Components[OrificeComponent[k]];
            Slot[OrificeComponent[k]] = k;
            OrificeA[k] = model.Nodes[c.A].Index;
            OrificeB[k] = c.B < 0 ? -1 : model.Nodes[c.B].Index;
            Restriction[k] = new(c.P0, c.P1);
        }
        HeatComponent = links.ToArray();
        HeatGas = new int[HeatComponent.Length]; HeatWall = new int[HeatComponent.Length];
        for (int k = 0; k < HeatComponent.Length; ++k)
        {
            var c = model.Components[HeatComponent[k]];
            Slot[HeatComponent[k]] = k;
            HeatGas[k] = model.Nodes[c.A].Index; HeatWall[k] = model.Nodes[c.B].Index;
        }
    }
}

/// <summary>Rates produced by one evaluation of the gas network, all in SI per second.</summary>
internal struct GasRates(int gases, int thermal)
{
    internal readonly double[] Mass = new double[gases], Energy = new double[gases], Wall = new double[thermal];
    internal double ReservoirMass = 0, ReservoirEnthalpy = 0;

    internal void Clear()
    {
        Array.Clear(Mass, 0, Mass.Length); Array.Clear(Energy, 0, Energy.Length);
        Array.Clear(Wall, 0, Wall.Length);
        ReservoirMass = 0; ReservoirEnthalpy = 0;
    }
}

/// <summary>
/// Bounded explicit sub-stepping for the gas states, with a Heun corrector. All workspace belongs to
/// one simulation and every successful step allocates nothing.
/// </summary>
/// <remarks>
/// The engine notes proposed a pairwise implicit transfer solve. That is not adopted here: the
/// subcritical branch of the orifice has an infinite derivative at unit pressure ratio, so a Newton
/// iteration has no usable Jacobian exactly where a nearly equalised pair spends most of its time.
/// This solver instead sizes an explicit sub-step from the fastest relative rate in the network and
/// refuses the tick when the required count exceeds <see cref="MaxSubSteps"/>, which keeps the work
/// per tick bounded and the failure actionable. Gas-to-wall coupling is explicit in the wall
/// temperature, so it is first order in the tick even though the gas sub-steps are second order.
/// </remarks>
internal sealed class GasSolver
{
    internal const int MaxSubSteps = 4096;
    private const double StepFraction = 0.02, HardFraction = 0.25;

    private readonly CompiledModel _model;
    private readonly GasNetwork _network;
    private GasRates _first, _second;
    private readonly double[] _mass, _energy;
    internal readonly double[] WallHeat;
    internal double ReservoirMass, ReservoirEnthalpy;

    internal GasSolver(CompiledModel model)
    {
        _model = model; _network = model.Gas!;
        int gases = model.GasCount, thermal = model.ThermalCount;
        _first = new(gases, thermal); _second = new(gases, thermal);
        _mass = new double[gases]; _energy = new double[gases];
        WallHeat = new double[thermal];
    }

    private double Pressure(int gas, double[] energy) =>
        (_network.Gases[gas].Gamma - 1) * energy[gas] / _network.Volume[gas];
    private double Temperature(int gas, double[] mass, double[] energy) =>
        energy[gas] / (mass[gas] * _network.Gases[gas].IsochoricHeatCapacityJoulePerKilogramKelvin);

    private bool Evaluate(double[] mass, double[] energy, double[] wallTemperature, double[] inputs, ref GasRates rates, double transferSeconds = 0)
    {
        rates.Clear();
        for (int i = 0; i < mass.Length; ++i)
            if (!(mass[i] > 0) || !(energy[i] > 0) || !Numeric.Finite(mass[i]) || !Numeric.Finite(energy[i])) return false;
        for (int k = 0; k < _network.OrificeComponent.Length; ++k)
        {
            int component = _network.OrificeComponent[k], a = _network.OrificeA[k], b = _network.OrificeB[k];
            var c = _model.Components[component];
            var composition = _network.Gases[a];
            double pressureB = b < 0 ? c.P2 : Pressure(b, energy);
            double temperatureB = b < 0 ? c.P3 : Temperature(b, mass, energy);
            if (!_network.Restriction[k].TryEvaluate(composition, Pressure(a, energy), Temperature(a, mass, energy),
                    pressureB, temperatureB, inputs[component], out var flow)) return false;
            // At equal pressure the nozzle derivative is singular. Limit each stage's transfer
            // to the pair's equal-pressure energy so a resting pair cannot oscillate across it.
            // Apply the same factor to mass and upstream enthalpy to preserve both ledgers.
            if (transferSeconds > 0 && flow.EnthalpyFlowWatts != 0)
            {
                double slope = (composition.Gamma - 1) / _network.Volume[a]
                    + (b < 0 ? 0 : (_network.Gases[b].Gamma - 1) / _network.Volume[b]);
                double equalEnergy = Math.Abs(Pressure(a, energy) - pressureB) / slope;
                double scale = Math.Min(1, equalEnergy / (transferSeconds * Math.Abs(flow.EnthalpyFlowWatts)));
                flow = new(flow.MassFlowKilogramsPerSecond * scale, flow.EnthalpyFlowWatts * scale, flow.Choked);
            }
            rates.Mass[a] -= flow.MassFlowKilogramsPerSecond;
            rates.Energy[a] -= flow.EnthalpyFlowWatts;
            if (b < 0)
            {
                rates.ReservoirMass -= flow.MassFlowKilogramsPerSecond;
                rates.ReservoirEnthalpy -= flow.EnthalpyFlowWatts;
            }
            else
            {
                rates.Mass[b] += flow.MassFlowKilogramsPerSecond;
                rates.Energy[b] += flow.EnthalpyFlowWatts;
            }
        }
        for (int k = 0; k < _network.HeatComponent.Length; ++k)
        {
            int gas = _network.HeatGas[k], wall = _network.HeatWall[k];
            double heat = _model.Components[_network.HeatComponent[k]].P0 *
                (Temperature(gas, mass, energy) - wallTemperature[wall]);
            if (!Numeric.Finite(heat)) return false;
            rates.Energy[gas] -= heat;
            rates.Wall[wall] += heat;
        }
        for (int i = 0; i < rates.Mass.Length; ++i)
            if (!Numeric.Finite(rates.Mass[i]) || !Numeric.Finite(rates.Energy[i])) return false;
        return Numeric.Finite(rates.ReservoirMass) && Numeric.Finite(rates.ReservoirEnthalpy);
    }

    /// <summary>
    /// Advance the gas states across one whole tick. On success <see cref="WallHeat"/> holds the joules
    /// delivered to each thermal node and the reservoir fields hold the net mass and enthalpy taken in.
    /// </summary>
    internal bool Advance(double[] mass, double[] energy, double[] wallTemperature, double[] inputs, double dt)
    {
        Array.Clear(WallHeat, 0, WallHeat.Length);
        ReservoirMass = 0; ReservoirEnthalpy = 0;
        if (!Evaluate(mass, energy, wallTemperature, inputs, ref _first)) return false;
        double worst = 0;
        for (int i = 0; i < mass.Length; ++i)
            worst = Math.Max(worst, Math.Max(Math.Abs(_first.Mass[i]) / mass[i], Math.Abs(_first.Energy[i]) / energy[i]));
        double required = dt * worst / StepFraction;
        if (!Numeric.Finite(required) || required > MaxSubSteps) return false;
        int steps = required <= 1 ? 1 : (int)Math.Ceiling(required);
        double h = dt / steps;
        for (int step = 0; step < steps; ++step)
        {
            // Re-evaluate with the admitted stage duration to bound near-equilibrium transfers.
            if (!Evaluate(mass, energy, wallTemperature, inputs, ref _first, h)) return false;
            for (int i = 0; i < mass.Length; ++i)
            {
                _mass[i] = mass[i] + h * _first.Mass[i];
                _energy[i] = energy[i] + h * _first.Energy[i];
            }
            if (!Evaluate(_mass, _energy, wallTemperature, inputs, ref _second, h)) return false;
            for (int i = 0; i < mass.Length; ++i)
            {
                double deltaMass = 0.5 * h * (_first.Mass[i] + _second.Mass[i]);
                double deltaEnergy = 0.5 * h * (_first.Energy[i] + _second.Energy[i]);
                if (Math.Abs(deltaMass) > HardFraction * mass[i] || Math.Abs(deltaEnergy) > HardFraction * energy[i]) return false;
                mass[i] += deltaMass; energy[i] += deltaEnergy;
                if (!(mass[i] > 0) || !(energy[i] > 0) || !Numeric.Finite(mass[i]) || !Numeric.Finite(energy[i])) return false;
            }
            for (int w = 0; w < WallHeat.Length; ++w) WallHeat[w] += 0.5 * h * (_first.Wall[w] + _second.Wall[w]);
            ReservoirMass += 0.5 * h * (_first.ReservoirMass + _second.ReservoirMass);
            ReservoirEnthalpy += 0.5 * h * (_first.ReservoirEnthalpy + _second.ReservoirEnthalpy);
        }
        for (int w = 0; w < WallHeat.Length; ++w) if (!Numeric.Finite(WallHeat[w])) return false;
        return Numeric.Finite(ReservoirMass) && Numeric.Finite(ReservoirEnthalpy);
    }
}
