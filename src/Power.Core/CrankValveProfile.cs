// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>A periodic sin-squared effective-opening envelope, independent of time and rotation direction.</summary>
public sealed class CrankValveProfile
{
    public const double MinimumDurationRadians = 1e-6;
    public double CycleAngleRadians { get; }
    public double OpenAngleRadians { get; }
    public double DurationAngleRadians { get; }
    public double MaxTravelPerTickRadians => Math.Min(0.25, DurationAngleRadians / 8);

    public CrankValveProfile(double cycleAngleRadians, double openAngleRadians, double durationAngleRadians)
    {
        if (cycleAngleRadians != 2 * Math.PI && cycleAngleRadians != 4 * Math.PI)
            throw new ArgumentException("Valve cycle must be 2 pi or 4 pi radians (360 or 720 degrees).");
        if (!Numeric.Finite(openAngleRadians) || !Numeric.Finite(durationAngleRadians) ||
            durationAngleRadians < MinimumDurationRadians || durationAngleRadians > cycleAngleRadians)
            throw new ArgumentException("Opening angle must be finite and duration must be in [1e-6 rad, cycle angle].");
        CycleAngleRadians = cycleAngleRadians;
        OpenAngleRadians = Wrap(openAngleRadians, cycleAngleRadians);
        DurationAngleRadians = durationAngleRadians;
    }

    private static double Wrap(double angle, double cycle)
    {
        double value = angle % cycle;
        return value < 0 ? value + cycle : value == 0 ? 0 : value;
    }

    /// <summary>Effective opening in [0,1]. Both lobe boundaries are closed; reverse motion retraces the same profile.</summary>
    public double Evaluate(double crankAngleRadians, double peakOpening = 1)
    {
        if (!Numeric.Finite(crankAngleRadians)) throw new ArgumentOutOfRangeException(nameof(crankAngleRadians), "Crank angle must be finite.");
        if (!Numeric.Finite(peakOpening) || peakOpening < 0 || peakOpening > 1)
            throw new ArgumentOutOfRangeException(nameof(peakOpening), "Peak opening must be in [0, 1].");
        double position = Wrap(crankAngleRadians % CycleAngleRadians - OpenAngleRadians, CycleAngleRadians);
        if (position == 0 || position >= DurationAngleRadians || peakOpening == 0) return 0;
        double sine = Math.Sin(Math.PI * (position / DurationAngleRadians));
        return peakOpening * sine * sine;
    }
}

internal sealed class TimedValve(int crankIndex, uint crankNode, CrankValveProfile profile)
{
    internal readonly int CrankIndex = crankIndex;
    internal readonly uint CrankNode = crankNode;
    internal readonly CrankValveProfile Profile = profile;

    internal bool Resolved(double[] old, double[] midpoint, double dt)
    {
        double angle = old[CrankIndex], nextAngle = 2 * midpoint[CrankIndex] - angle;
        double speed = old[CrankIndex + 1], nextSpeed = 2 * midpoint[CrankIndex + 1] - speed;
        double travel = Math.Max(Math.Abs(nextAngle - angle), dt * Math.Max(Math.Abs(speed), Math.Abs(nextSpeed)));
        // Large unwrapped angles must still retain enough binary64 precision to resolve a lobe.
        double rounding = 8 * 2.2204460492503131e-16 * Math.Max(Math.Abs(angle), Math.Abs(nextAngle));
        return Numeric.Finite(travel) && travel <= Profile.MaxTravelPerTickRadians && rounding <= Profile.MaxTravelPerTickRadians;
    }
}
