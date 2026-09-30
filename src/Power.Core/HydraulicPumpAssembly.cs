// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public readonly record struct PumpAssemblyReaction(
    double NetVolumeFlowCubicMetersPerSecond, double ShaftTorqueNewtonMeters,
    double HydraulicPowerWatts, double ShaftPowerWatts,
    double LeakageHeatWatts, double FrictionHeatWatts);

/// <summary>Reversible displacement pump with explicit constant pressure leakage and viscous shaft drag.</summary>
public sealed class HydraulicPumpAssembly
{
    public double DisplacementCubicMetersPerRadian { get; }
    public double LeakageCubicMetersPerSecondPascal { get; }
    public double FrictionNewtonMeterSecondsPerRadian { get; }

    public HydraulicPumpAssembly(double displacementCubicMetersPerRadian,
        double leakageCubicMetersPerSecondPascal, double frictionNewtonMeterSecondsPerRadian)
    {
        if (!Numeric.Finite(displacementCubicMetersPerRadian) || displacementCubicMetersPerRadian <= 0)
            throw new ArgumentException("Displacement must be finite and positive.", nameof(displacementCubicMetersPerRadian));
        if (!Numeric.Finite(leakageCubicMetersPerSecondPascal) || leakageCubicMetersPerSecondPascal < 0)
            throw new ArgumentException("Leakage conductance must be finite and nonnegative.", nameof(leakageCubicMetersPerSecondPascal));
        if (!Numeric.Finite(frictionNewtonMeterSecondsPerRadian) || frictionNewtonMeterSecondsPerRadian < 0)
            throw new ArgumentException("Viscous friction must be finite and nonnegative.", nameof(frictionNewtonMeterSecondsPerRadian));
        DisplacementCubicMetersPerRadian = displacementCubicMetersPerRadian;
        LeakageCubicMetersPerSecondPascal = leakageCubicMetersPerSecondPascal;
        FrictionNewtonMeterSecondsPerRadian = frictionNewtonMeterSecondsPerRadian;
    }

    /// <summary>Net flow is inlet to outlet; positive shaft power is absorbed by the assembly.</summary>
    public bool TryEvaluate(double speedRadiansPerSecond, double inletPressurePascals,
        double outletPressurePascals, out PumpAssemblyReaction reaction)
    {
        reaction = default;
        if (!Numeric.Finite(speedRadiansPerSecond) || !Numeric.Finite(inletPressurePascals) ||
            !Numeric.Finite(outletPressurePascals) || inletPressurePascals < 0 || outletPressurePascals < 0) return false;
        double difference = outletPressurePascals - inletPressurePascals;
        double idealFlow = DisplacementCubicMetersPerRadian * speedRadiansPerSecond;
        double leakage = LeakageCubicMetersPerSecondPascal * difference;
        double drag = FrictionNewtonMeterSecondsPerRadian * speedRadiansPerSecond;
        double flow = idealFlow - leakage;
        double torque = -DisplacementCubicMetersPerRadian * difference - drag;
        double hydraulicPower = flow * difference, shaftPower = -torque * speedRadiansPerSecond;
        double leakageHeat = leakage * difference, frictionHeat = drag * speedRadiansPerSecond;
        if (!GearReference.Finite(flow, torque, hydraulicPower, shaftPower) ||
            !Numeric.Finite(leakageHeat) || !Numeric.Finite(frictionHeat)) return false;
        reaction = new(flow, torque, hydraulicPower, shaftPower, leakageHeat, frictionHeat);
        return true;
    }

    /// <summary>Lower into ordinary conserving graph components. The compiler checks ports, domains and global IDs.</summary>
    public IReadOnlyList<ComponentDefinition> CreateComponents(uint pumpId, uint leakageId, uint frictionId,
        uint shaft, uint outlet, uint inlet = 0, double reservoirPressurePascals = 0, uint heat = 0)
    {
        if (pumpId == 0 || leakageId == 0 || frictionId == 0 || pumpId == leakageId || pumpId == frictionId || leakageId == frictionId)
            throw new ArgumentException("Pump, leakage and friction IDs must be distinct and nonzero.", nameof(pumpId));
        if (!Numeric.Finite(reservoirPressurePascals) || reservoirPressurePascals < 0 || inlet != 0 && reservoirPressurePascals != 0)
            throw new ArgumentException("A nonnegative reservoir pressure applies only to inlet zero.", nameof(reservoirPressurePascals));
        return Array.AsReadOnly(new[]
        {
            ComponentDefinition.DisplacementPump(pumpId, shaft, outlet, DisplacementCubicMetersPerRadian, inlet, reservoirPressurePascals),
            ComponentDefinition.HydraulicResistance(leakageId, outlet, inlet, LeakageCubicMetersPerSecondPascal, reservoirPressurePascals, heat: heat),
            ComponentDefinition.Shaft(frictionId, shaft, 0, 0, FrictionNewtonMeterSecondsPerRadian, heat: heat)
        });
    }
}
