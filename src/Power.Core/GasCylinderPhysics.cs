// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Moving geometry and conservative adiabatic pressure work; mass/energy live in Simulation.</summary>
internal sealed class GasCylinderPhysics
{
    internal readonly CrankSlider Geometry;
    internal readonly double Phase, BackPressure;
    internal readonly double[] Parameters;

    internal GasCylinderPhysics(GasCylinderDefinition definition, uint id)
    {
        double Q(Quantity q, Unit unit, string field) => CompiledModel.Convert(q, unit, id, "moving_cylinder." + field);
        double bore = Q(definition.Bore, Unit.Meter, "bore"), stroke = Q(definition.Stroke, Unit.Meter, "stroke"),
            rod = Q(definition.RodLength, Unit.Meter, "rod_length");
        Phase = Q(definition.Phase, Unit.Radian, "phase");
        BackPressure = Q(definition.BackPressure, Unit.Pascal, "back_pressure");
        if (BackPressure < 0) throw new ModelCompileException(DiagnosticCode.Range, id, "moving_cylinder.back_pressure", "Back pressure must be nonnegative.");
        try { Geometry = new(bore, stroke, rod, definition.CompressionRatio); }
        catch (ArgumentException error) { throw new ModelCompileException(DiagnosticCode.Range, id, "moving_cylinder.geometry", error.Message); }
        Parameters = [bore, stroke, rod, Phase, definition.CompressionRatio, BackPressure];
    }

    private double Angle(double crank) => crank % (2 * Math.PI) + Phase % (2 * Math.PI);
    internal CrankSliderSample GeometryAt(double crank) => Geometry.Evaluate(Angle(crank));
    internal double Pressure(double crank, double energy, double gamma) => (gamma - 1) * energy / GeometryAt(crank).VolumeCubicMeters;
    internal double Torque(double crank, double energy, double gamma) =>
        (Pressure(crank, energy, gamma) - BackPressure) * GeometryAt(crank).VolumeDerivativeCubicMetersPerRadian;

    internal double EnergyChange(double crank, double delta, double energy, double gamma)
    {
        double angle = Angle(crank), volume = Geometry.Evaluate(angle).VolumeCubicMeters;
        double z = Geometry.VolumeGradient(angle, delta) * delta / volume;
        return energy * Numeric.Expm1(-(gamma - 1) * Numeric.Log1p(z));
    }

    internal double DiscreteTorque(double crank, double delta, double energy, double gamma)
    {
        double angle = Angle(crank), volume = Geometry.Evaluate(angle).VolumeCubicMeters;
        double gradient = Geometry.VolumeGradient(angle, delta), z = gradient * delta / volume;
        double pressure = (gamma - 1) * energy / volume;
        double meanPressure = z == 0 ? pressure : pressure * -Numeric.Expm1(-(gamma - 1) * Numeric.Log1p(z)) / ((gamma - 1) * z);
        return (meanPressure - BackPressure) * gradient;
    }

    internal double BackPressureWork(double crank, double delta) => -BackPressure * Geometry.VolumeGradient(Angle(crank), delta) * delta;
}
