// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record LiquidFuelInjectorDefinition
{
    public uint FilmComponent { get; init; }
    public uint CrankNode { get; init; }
    public Quantity CycleAngle { get; init; }
    public Quantity StartAngle { get; init; }
    public Quantity DurationAngle { get; init; }
    public Quantity MaximumDose { get; init; }
    public Quantity InitialMass { get; init; }
    public Quantity SupplyTemperature { get; init; }
    public Quantity LiquidDensity { get; init; }
    public Quantity InitialPressure { get; init; }
    public Quantity PressureCompliance { get; init; }
    public InjectorNeedleDefinition? Needle { get; init; }
}

public readonly record struct LiquidRailSample(double MassKilograms, double PressurePascals);
public readonly record struct LiquidRailTransfer(LiquidRailSample Rail, double DeliveredMassKilograms,
    double SourcePressureWorkJoules, double ReceiverPressureWorkJoules, double NozzleHeatJoules);

/// <summary>Finite incompressible inventory in a compliant rail, discharging through a passive one-way nozzle.</summary>
public sealed class CompliantLiquidRail
{
    public double InitialMassKilograms { get; }
    public double InitialPressurePascals { get; }
    public double DensityKilogramsPerCubicMeter { get; }
    public double ComplianceCubicMetersPerPascal { get; }
    public double NozzleAreaSquareMeters { get; }
    public double DischargeCoefficient { get; }
    public double ReferenceVolumeCubicMeters { get; }
    private readonly double _massPerPressure, _rootDecay;

    public CompliantLiquidRail(double initialMass, double initialPressure, double density,
        double compliance, double area, double dischargeCoefficient)
    {
        if (!GearReference.Finite(initialMass, initialPressure, density, compliance) ||
            initialMass <= 0 || initialPressure <= 0 || density <= 0 || compliance <= 0 ||
            !Numeric.Finite(area) || area <= 0 || !Numeric.Finite(dischargeCoefficient) ||
            dischargeCoefficient <= 0 || dischargeCoefficient > 1)
            throw new ArgumentException("Use positive finite rail mass, absolute pressure, density, compliance and nozzle area; discharge coefficient is in (0,1].");
        InitialMassKilograms = initialMass;
        InitialPressurePascals = initialPressure;
        DensityKilogramsPerCubicMeter = density;
        ComplianceCubicMetersPerPascal = compliance;
        NozzleAreaSquareMeters = area;
        DischargeCoefficient = dischargeCoefficient;
        ReferenceVolumeCubicMeters = initialMass / density - compliance * initialPressure;
        _massPerPressure = density * compliance;
        _rootDecay = dischargeCoefficient * area / (compliance * Math.Sqrt(density) * Math.Sqrt(2));
        if (!GearReference.Finite(ReferenceVolumeCubicMeters, _massPerPressure, _rootDecay, StoredPressureEnergy(initialPressure)) ||
            ReferenceVolumeCubicMeters < 0 || _massPerPressure <= 0 || _rootDecay <= 0 ||
            !Numeric.Finite(InitialMassKilograms / DensityKilogramsPerCubicMeter))
            throw new ArgumentException("Rail volume must cover its initial compliant volume, and pressure/flow scales must be representable.");
    }

    public LiquidRailSample StateAfterDelivery(double deliveredMassKilograms) => new(
        InitialMassKilograms - deliveredMassKilograms,
        InitialPressurePascals - deliveredMassKilograms / _massPerPressure);

    public double StoredPressureEnergy(double pressurePascals) =>
        .5 * ComplianceCubicMetersPerPascal * pressurePascals * pressurePascals;

    public bool TryAdvance(LiquidRailSample state, double receiverPressurePascals, double seconds,
        double maximumDeliveryMassKilograms, out LiquidRailTransfer transfer)
    {
        transfer = default;
        if (!GearReference.Finite(state.MassKilograms, state.PressurePascals, receiverPressurePascals, seconds) ||
            state.MassKilograms < 0 || state.PressurePascals < 0 || receiverPressurePascals < 0 || seconds < 0 ||
            !Numeric.Finite(maximumDeliveryMassKilograms) || maximumDeliveryMassKilograms < 0)
            return false;
        double compatibleMass = DensityKilogramsPerCubicMeter * ReferenceVolumeCubicMeters + _massPerPressure * state.PressurePascals;
        if (!Numeric.Finite(compatibleMass) || Math.Abs(state.MassKilograms - compatibleMass) >
            256 * GearReference.Epsilon * Math.Max(InitialMassKilograms, compatibleMass)) return false;
        double head = state.PressurePascals - receiverPressurePascals;
        if (head <= 0 || seconds == 0 || maximumDeliveryMassKilograms == 0)
        {
            transfer = new(state, 0, 0, 0, 0);
            return true;
        }
        double root = Math.Sqrt(head), reduction = Math.Min(root, _rootDecay * seconds);
        double available = _massPerPressure * reduction * (2 * root - reduction);
        if (!Numeric.Finite(available) || available < 0) return false;
        double mass = Math.Min(Math.Min(available, maximumDeliveryMassKilograms), state.MassKilograms);
        double volume = mass / DensityKilogramsPerCubicMeter;
        double drop = volume / ComplianceCubicMetersPerPascal;
        // Work is the exact integral of rail pressure over discharged volume.
        double sourceWork = volume * (state.PressurePascals - .5 * drop);
        double receiverWork = receiverPressurePascals * volume;
        double nozzleHeat = volume * (head - .5 * drop);
        double pressure = mass == available && reduction == root ? receiverPressurePascals : state.PressurePascals - drop;
        var next = new LiquidRailSample(state.MassKilograms - mass, pressure);
        if (!GearReference.Finite(next.MassKilograms, pressure, sourceWork, receiverWork) ||
            !Numeric.Finite(nozzleHeat) || next.MassKilograms < 0 || pressure < 0 || nozzleHeat < 0) return false;
        transfer = new(next, mass, sourceWork, receiverWork, nozzleHeat);
        return true;
    }
}

internal sealed record CompiledLiquidInjector(int Component, int Film, CompiledInjector Meter,
    CompliantLiquidRail Rail, double SupplyTemperature, double SpecificThermalEnergy, double HeatingValue, CompiledNeedle? Needle = null, int PressureNode = -1);

internal sealed class LiquidInjectorState(int count)
{
    internal readonly FuelInjectorState Quota = new(count);
    internal readonly double[] SourceWork = new double[count], SourceCorrection = new double[count];
    internal readonly double[] ReceiverWork = new double[count], ReceiverCorrection = new double[count];
    internal readonly double[] Heat = new double[count], HeatCorrection = new double[count];

    internal void CopyFrom(LiquidInjectorState other)
    {
        Quota.CopyFrom(other.Quota);
        Array.Copy(other.SourceWork, SourceWork, count); Array.Copy(other.SourceCorrection, SourceCorrection, count);
        Array.Copy(other.ReceiverWork, ReceiverWork, count); Array.Copy(other.ReceiverCorrection, ReceiverCorrection, count);
        Array.Copy(other.Heat, Heat, count); Array.Copy(other.HeatCorrection, HeatCorrection, count);
    }
    internal bool Finite()
    {
        if (!Quota.Finite()) return false;
        for (int i = 0; i < count; ++i)
            if (!GearReference.Finite(SourceWork[i], SourceCorrection[i], ReceiverWork[i], ReceiverCorrection[i]) ||
                !Numeric.Finite(Heat[i]) || !Numeric.Finite(HeatCorrection[i]) ||
                SourceWork[i] < 0 || ReceiverWork[i] < 0 || Heat[i] < 0) return false;
        return true;
    }
    internal ulong Hash(ulong hash)
    {
        hash = Quota.Hash(hash);
        for (int i = 0; i < count; ++i)
        {
            hash = Numeric.Hash(hash, SourceWork[i]); hash = Numeric.Hash(hash, SourceCorrection[i]);
            hash = Numeric.Hash(hash, ReceiverWork[i]); hash = Numeric.Hash(hash, ReceiverCorrection[i]);
            hash = Numeric.Hash(hash, Heat[i]); hash = Numeric.Hash(hash, HeatCorrection[i]);
        }
        return hash;
    }
}

internal sealed class LiquidFuelInjectorSolver(CompiledModel model, FuelFilmSolver films)
{
    internal double ReceiverWork;
    internal void BeginInterval() => ReceiverWork = 0;

    internal bool Advance(LiquidInjectorState state, FuelFilmState filmState, double[] dynamics,
        double[] gasEnergy, double[] inputs, double duration, bool reverse = false, HydraulicState? hydraulics = null, LiquidFeedState? feedState = null)
    {
        for (int slot = 0; slot < model.LiquidFuelInjectors.Length; ++slot)
        {
            int i = reverse ? model.LiquidFuelInjectors.Length - 1 - slot : slot;
            var injector = model.LiquidFuelInjectors[i];
            if (!state.Quota.Prepare(i, injector.Meter, dynamics, inputs[injector.Component], duration,
                out double opening, out double rateLimit)) return false;
            double ceiling = rateLimit * duration;
            if (injector.Needle is { } needle) { opening = needle.Opening(dynamics); ceiling = model.RailSample(i,state,hydraulics).MassKilograms; }
            if (opening == 0) continue;
            var film = model.FuelFilms[injector.Film];
            double volume = model.Gas!.VolumeAt(film.Gas, dynamics);
            double pressure = (model.Gas.Gases[film.Gas].Gamma - 1) * gasEnergy[film.Gas] / volume;
            var rail = model.RailSample(i,state,hydraulics);
            if (!injector.Rail.TryAdvance(rail, pressure, duration * opening, ceiling, out var transfer)) return false;
            double mass = transfer.DeliveredMassKilograms;
            if (!state.Quota.Accept(i, mass, enforceQuota: injector.Needle is null)) return false;
            filmState.Mass[injector.Film] += mass;
            double caloric=injector.PressureNode<0?mass*injector.SpecificThermalEnergy:mass==0?0:mass*feedState!.RailEnergy[i]/rail.MassKilograms;
            filmState.Energy[injector.Film] += caloric;
            if(injector.PressureNode>=0)
            {
                Numeric.Accumulate(-caloric,ref feedState!.RailEnergy[i],ref feedState.RailCorrection[i]);
                hydraulics!.Pressure[injector.PressureNode]=transfer.Rail.PressurePascals;
                Numeric.Accumulate(-mass/injector.Rail.DensityKilogramsPerCubicMeter,ref hydraulics.VolumeIn,ref hydraulics.VolumeCorrection);
            }
            films.AddHeat(film.Wall, film.WallCapacity, transfer.NozzleHeatJoules);
            Numeric.Accumulate(transfer.SourcePressureWorkJoules, ref state.SourceWork[i], ref state.SourceCorrection[i]);
            Numeric.Accumulate(transfer.ReceiverPressureWorkJoules, ref state.ReceiverWork[i], ref state.ReceiverCorrection[i]);
            Numeric.Accumulate(transfer.NozzleHeatJoules, ref state.Heat[i], ref state.HeatCorrection[i]);
            ReceiverWork += transfer.ReceiverPressureWorkJoules;
        }
        return state.Finite() && filmState.Finite() && Numeric.Finite(ReceiverWork);
    }
}
