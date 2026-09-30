// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record GasPistonDefinition
{
    public Quantity Area { get; init; }
    public Quantity ReferenceVolume { get; init; }
    public Quantity ReferencePosition { get; init; }
    public Quantity ReferencePressure { get; init; }
    public int CompressionDirection { get; init; } = 1;
}

public readonly record struct GasPistonWork(double MeanAbsolutePressurePascals, double ForceNewtons,
    double ForceDerivativeNewtonsPerMeter, double GasEnergyChangeJoules, double ReferenceWorkJoules);

/// <summary>Linear gas volume with explicit compression orientation. Gas mass/energy belong to the simulation.</summary>
public sealed class GasPiston
{
    public double AreaSquareMeters { get; }
    public double ReferenceVolumeCubicMeters { get; }
    public double ReferencePositionMeters { get; }
    public double ReferencePressurePascals { get; }
    public int CompressionDirection { get; }
    public GasPiston(double areaSquareMeters, double referenceVolumeCubicMeters, double referencePositionMeters, double referencePressurePascals, int compressionDirection = 1)
    {
        if (!GearReference.Finite(areaSquareMeters, referenceVolumeCubicMeters, referencePositionMeters, referencePressurePascals) ||
            areaSquareMeters <= 0 || referenceVolumeCubicMeters <= 0 || referencePressurePascals < 0 || compressionDirection is not (1 or -1))
            throw new ArgumentException("Use finite geometry, positive area/volume, nonnegative absolute reference pressure and compression direction -1 or 1.");
        AreaSquareMeters = areaSquareMeters; ReferenceVolumeCubicMeters = referenceVolumeCubicMeters;
        ReferencePositionMeters = referencePositionMeters; ReferencePressurePascals = referencePressurePascals;
        CompressionDirection = compressionDirection;
    }
    public double VolumeAt(double positionMeters) => ReferenceVolumeCubicMeters - CompressionDirection * AreaSquareMeters * (positionMeters - ReferencePositionMeters);
    public double Force(double absoluteGasPressurePascals) => -CompressionDirection * AreaSquareMeters * (absoluteGasPressurePascals - ReferencePressurePascals);
    public bool TryAdiabaticWork(double oldPositionMeters, double nextPositionMeters, double energyJoules, double gamma, out GasPistonWork work)
    {
        work = default;
        double volume = VolumeAt(oldPositionMeters), delta = nextPositionMeters - oldPositionMeters;
        double nextVolume = VolumeAt(nextPositionMeters), change = -CompressionDirection * AreaSquareMeters * delta;
        if (!GearReference.Finite(oldPositionMeters, nextPositionMeters, energyJoules, gamma) || energyJoules <= 0 || gamma <= 1 ||
            !Numeric.Finite(volume) || !Numeric.Finite(nextVolume) || volume <= 0 || nextVolume <= 0) return false;
        double z = change / volume, phi, derivative;
        if (!Numeric.Finite(z) || z <= -1) return false;
        if (Math.Abs(gamma * z) < 1e-3)
        {
            // Series of (1-(1+z)^(1-gamma))/((gamma-1)*z), with scaled terms.
            // Avoid large powers of gamma and retain the exact limiting slope at zero travel.
            phi = 1; derivative = -.5 * gamma;
            double term = 1, slopeTerm = derivative;
            for (int n = 1; n <= 7; ++n)
            {
                double factor = -(gamma + n - 1) * z;
                term *= factor / (n + 1); phi += term;
                if (n > 1) { slopeTerm *= factor * n / ((n - 1) * (n + 1)); derivative += slopeTerm; }
            }
        }
        else
        {
            double logarithm = Numeric.Log1p(z);
            phi = -Numeric.Expm1(-(gamma - 1) * logarithm) / ((gamma - 1) * z);
            derivative = (Math.Exp(-gamma * logarithm) - phi) / z;
        }
        double pressure = (gamma - 1) * energyJoules / volume, mean = pressure * phi;
        double force = Force(mean), slope = AreaSquareMeters / volume * AreaSquareMeters * pressure * derivative;
        double energy = -mean * change, reference = ReferencePressurePascals * CompressionDirection * AreaSquareMeters * delta;
        if (!GearReference.Finite(mean, force, slope, energy) || !Numeric.Finite(reference) || !Numeric.Finite(energyJoules + energy) || energyJoules + energy <= 0) return false;
        work = new(mean, force, slope, energy, reference); return true;
    }
}
