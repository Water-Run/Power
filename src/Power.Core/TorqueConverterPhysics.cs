// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Collections.ObjectModel;

namespace Power.Core;

public readonly record struct ConverterMapPoint(double SpeedRatio, double TorqueRatio, Quantity CapacityCoefficient);

/// <summary>Owned immutable points. Physical validation occurs with the converter's component ID.</summary>
public sealed class ConverterMap : IEquatable<ConverterMap>
{
    public const int MaxPoints = 32;
    public ReadOnlyCollection<ConverterMapPoint> Points { get; }
    public ConverterMap(IEnumerable<ConverterMapPoint> points)
    {
        if (points is null) throw new ArgumentNullException(nameof(points));
        var owned = points.Take(MaxPoints + 1).ToArray();
        if (owned.Length > MaxPoints) throw new ArgumentException("A converter map supports at most 32 points.", nameof(points));
        Points = Array.AsReadOnly(owned);
    }
    public bool Equals(ConverterMap? other) => other is not null && Points.SequenceEqual(other.Points);
    public override bool Equals(object? other) => other is ConverterMap map && Equals(map);
    public override int GetHashCode()
    {
        int hash = 17;
        foreach (var point in Points) hash = unchecked(hash * 31 + point.GetHashCode());
        return hash;
    }
}

public sealed record TorqueConverterDefinition
{
    public ConverterMap? PumpPositive { get; init; }
    public ConverterMap? PumpNegative { get; init; }
    public ConverterMap? TurbinePositive { get; init; }
    public ConverterMap? TurbineNegative { get; init; }
}

public enum ConverterDrive { Stopped, PumpPositive, PumpNegative, TurbinePositive, TurbineNegative }
public enum ConverterEvaluationStatus { Ok, InvalidSpeed, NumericalFailure }

/// <summary>Torques act on the pump, turbine and stationary stator; positive heat leaves mechanical motion.</summary>
public readonly record struct ConverterReaction(double PumpTorqueNewtonMeters, double TurbineTorqueNewtonMeters,
    double StatorTorqueNewtonMeters, double HeatFlowWatts, double SpeedRatio, ConverterDrive Drive);

/// <summary>
/// Passive quasi-steady converter with explicit signed pump/coast maps. The faster member
/// supplies reference speed; no quadrant is inferred by mirroring an unavailable map.
/// No fluid inertia, fill dynamics, lockup clutch, thermal fade or measured calibration is implied.
/// </summary>
public sealed class TorqueConverter
{
    private readonly record struct Point(double S, double R, double C);
    private readonly Point[][] _maps = new Point[4][];
    public TorqueConverter(TorqueConverterDefinition definition) : this(definition, 0) { }

    internal TorqueConverter(TorqueConverterDefinition definition, uint id)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        ConverterMap?[] maps = [definition.PumpPositive, definition.PumpNegative, definition.TurbinePositive, definition.TurbineNegative];
        string[] names = ["pump_positive", "pump_negative", "turbine_positive", "turbine_negative"];
        for (int k = 0; k < maps.Length; ++k)
        {
            string field = "converter." + names[k]; var source = maps[k]?.Points;
            if (source is null || source.Count is < 2 or > ConverterMap.MaxPoints)
                throw new ModelCompileException(DiagnosticCode.Capacity, id, field, "Supply 2..32 explicit points for each of the four signed converter maps.");
            var points = _maps[k] = new Point[source.Count];
            for (int i = 0; i < points.Length; ++i)
            {
                var p = source[i];
                double c = CompiledModel.Convert(p.CapacityCoefficient, Unit.NewtonMeterSecondSquaredPerRadianSquared, id, field + ".capacity_coefficient");
                if (!Numeric.Finite(p.SpeedRatio) || p.SpeedRatio < -1 || p.SpeedRatio > 1 ||
                    !Numeric.Finite(p.TorqueRatio) || p.TorqueRatio < 0 || c < 0 || p.SpeedRatio * p.TorqueRatio > 1 ||
                    (i > 0 && p.SpeedRatio <= points[i - 1].S))
                    throw new ModelCompileException(DiagnosticCode.Range, id, field, "Use strictly increasing speed ratios in [-1,1], nonnegative coefficients/torque ratios and passive efficiency s*r <= 1.");
                points[i] = new(p.SpeedRatio, p.TorqueRatio, c);
                if (i == 0) continue;
                var previous = points[i - 1];
                double slope = (p.TorqueRatio - previous.R) / (p.SpeedRatio - previous.S);
                double intercept = previous.R - slope * previous.S;
                double capacitySlope = (c - previous.C) / (p.SpeedRatio - previous.S);
                double vertex = slope < 0 ? -.5 * (intercept / slope) : double.NaN;
                if (!Numeric.Finite(slope) || !Numeric.Finite(intercept) || !Numeric.Finite(capacitySlope) ||
                    (vertex > previous.S && vertex < p.SpeedRatio && vertex * (slope * vertex + intercept) > 1))
                    throw new ModelCompileException(DiagnosticCode.Range, id, field, "Map interpolation must remain passive between every knot and have finite slopes.");
            }
            if (points[0].S != -1 || points[^1].S != 1 || points[^1].C != 0 || points[^1].R != 1)
                throw new ModelCompileException(DiagnosticCode.Range, id, field, "Each map must span [-1,1] and end at speed ratio 1 with zero coefficient and torque ratio 1.");
        }
        // At equal and opposite member speeds, either reference-member choice must
        // produce the same reactions. Same-direction equality has zero fluid torque.
        Match(_maps[0][0], _maps[3][0]); Match(_maps[1][0], _maps[2][0]);
        void Match(Point pump, Point turbine)
        {
            if (!Same(pump.C, turbine.R * turbine.C) || !Same(turbine.C, pump.R * pump.C))
                throw new ModelCompileException(DiagnosticCode.Range, id, "converter.counter_rotation",
                    "Pump and opposite-sign turbine maps must give continuous reactions at speed ratio -1.");
        }
    }

    private static bool Same(double a, double b) => Numeric.Finite(b) && Math.Abs(a - b) <=
        64 * GearReference.Epsilon * Math.Abs(a) + 64 * GearReference.Epsilon * Math.Abs(b);

    public ConverterEvaluationStatus Evaluate(double pumpRadiansPerSecond, double turbineRadiansPerSecond, out ConverterReaction reaction)
    {
        reaction = default;
        if (!Numeric.Finite(pumpRadiansPerSecond) || !Numeric.Finite(turbineRadiansPerSecond)) return ConverterEvaluationStatus.InvalidSpeed;
        return Reaction(pumpRadiansPerSecond, turbineRadiansPerSecond, out reaction, out _, out _, out _, out _)
            ? ConverterEvaluationStatus.Ok : ConverterEvaluationStatus.NumericalFailure;
    }

    internal bool Reaction(double pump, double turbine, out ConverterReaction result,
        out double dPumpPump, out double dPumpTurbine, out double dTurbinePump, out double dTurbineTurbine)
    {
        result = default; dPumpPump = dPumpTurbine = dTurbinePump = dTurbineTurbine = 0;
        if (!Numeric.Finite(pump) || !Numeric.Finite(turbine)) return false;
        bool pumpDrives = Math.Abs(pump) >= Math.Abs(turbine);
        double driver = pumpDrives ? pump : turbine, follower = pumpDrives ? turbine : pump;
        if (driver == 0) return true;
        int map = (pumpDrives ? 0 : 2) + (driver < 0 ? 1 : 0);
        double s = follower / driver, speed = Math.Abs(driver);
        var points = _maps[map]; int index = 1;
        while (index < points.Length - 1 && s > points[index].S) ++index;
        var a = points[index - 1]; var b = points[index];
        double interval = b.S - a.S, u = (s - a.S) / interval;
        double c = (1 - u) * a.C + u * b.C, r = (1 - u) * a.R + u * b.R;
        double dc = (b.C - a.C) / interval, dr = (b.R - a.R) / interval;
        double drag = c * speed * driver, td = -drag, tf = r * drag;
        double tp = pumpDrives ? td : tf, tt = pumpDrives ? tf : td;
        double lossFraction = 1 - s * r;
        if (lossFraction < -64 * GearReference.Epsilon) return false;
        double heat = (c * speed * speed) * speed * Math.Max(0, lossFraction), stator = -tp - tt;
        double dd = -speed * (2 * c - s * dc), df = -speed * dc;
        double productDerivative = dr * c + r * dc;
        double fd = speed * (2 * r * c - s * productDerivative), ff = speed * productDerivative;
        if (!GearReference.Finite(tp, tt, stator, heat) || !GearReference.Finite(dd, df, fd, ff)) return false;
        if (pumpDrives) { dPumpPump = dd; dPumpTurbine = df; dTurbinePump = fd; dTurbineTurbine = ff; }
        else { dPumpPump = ff; dPumpTurbine = fd; dTurbinePump = df; dTurbineTurbine = dd; }
        result = new(tp, tt, stator, heat, s, (ConverterDrive)(map + 1));
        return true;
    }

    internal ulong Hash(ulong hash)
    {
        foreach (var map in _maps)
        {
            hash = Numeric.Hash(hash, (ulong)map.Length);
            foreach (var p in map) { hash = Numeric.Hash(hash, p.S); hash = Numeric.Hash(hash, p.R); hash = Numeric.Hash(hash, p.C); }
        }
        return hash;
    }
}
