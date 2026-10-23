// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

/// <summary>Explicit pure-fuel caloric reference and elastic-vessel bounds; all quantities carry SI units.</summary>
public sealed record EquilibriumFuelDefinition
{
    public Quantity Compliance { get; init; }
    public Quantity LiquidBulkModulus { get; init; }
    public Quantity LiquidDensity { get; init; }
    public Quantity MaximumPressure { get; init; }
    public Quantity GasConstant { get; init; }
    public Quantity VaporSpecificHeat { get; init; }
    public Quantity LiquidSpecificHeat { get; init; }
    public Quantity ReferenceTemperature { get; init; }
    public Quantity LatentInternalEnergy { get; init; }
    public Quantity ReferencePressure { get; init; }
    public Quantity MinimumTemperature { get; init; }
    public Quantity MaximumTemperature { get; init; }
    public Quantity LowerHeatingValue { get; init; }
}

internal sealed record CompiledFuelVolume(int Node, CompliantFuelVolume Law, CompliantFuelPoint Initial, double HeatingValue);
internal sealed record CompiledFuelLink(int Component, int A, int B, double Conductance, bool Heat, double WallCapacity, bool OneWay);

internal sealed class EquilibriumFuelState
{
    internal readonly double[] Mass, MassCorrection, Energy, EnergyCorrection;
    internal readonly double[]? ReferenceVolume;
    internal readonly double[] Flow, VolumeFlow, Transferred, TransferCorrection, Enthalpy, EnthalpyCorrection, Entropy, EntropyCorrection;
    internal EquilibriumFuelState(int volumes, int links, bool moving = false)
    {
        ReferenceVolume = moving ? new double[volumes] : null;
        Mass = new double[volumes]; MassCorrection = new double[volumes]; Energy = new double[volumes]; EnergyCorrection = new double[volumes];
        Flow = new double[links]; VolumeFlow = new double[links]; Transferred = new double[links]; TransferCorrection = new double[links];
        Enthalpy = new double[links]; EnthalpyCorrection = new double[links]; Entropy = new double[links]; EntropyCorrection = new double[links];
    }
    internal void CopyFrom(EquilibriumFuelState other)
    {
        if (ReferenceVolume is not null) Array.Copy(other.ReferenceVolume!, ReferenceVolume, Mass.Length);
        Array.Copy(other.Mass, Mass, Mass.Length); Array.Copy(other.MassCorrection, MassCorrection, Mass.Length);
        Array.Copy(other.Energy, Energy, Energy.Length); Array.Copy(other.EnergyCorrection, EnergyCorrection, Energy.Length);
        Array.Copy(other.Flow, Flow, Flow.Length); Array.Copy(other.VolumeFlow, VolumeFlow, Flow.Length);
        Array.Copy(other.Transferred, Transferred, Flow.Length); Array.Copy(other.TransferCorrection, TransferCorrection, Flow.Length);
        Array.Copy(other.Enthalpy, Enthalpy, Flow.Length); Array.Copy(other.EnthalpyCorrection, EnthalpyCorrection, Flow.Length);
        Array.Copy(other.Entropy, Entropy, Flow.Length); Array.Copy(other.EntropyCorrection, EntropyCorrection, Flow.Length);
    }
    internal void BeginTick() { Array.Clear(Flow, 0, Flow.Length); Array.Clear(VolumeFlow, 0, Flow.Length); }
    internal bool EndTick(double seconds)
    {
        for (int i = 0; i < Flow.Length; ++i) { Flow[i] /= seconds; VolumeFlow[i] /= seconds; }
        return Finite();
    }
    internal bool Finite()
    {
        for (int i = 0; i < Mass.Length; ++i)
            if (Mass[i] <= 0 || !GearReference.Finite(Mass[i], MassCorrection[i], Energy[i], EnergyCorrection[i])) return false;
        if (ReferenceVolume is not null) foreach (double value in ReferenceVolume) if (!Numeric.Finite(value) || value <= 0) return false;
        for (int i = 0; i < Flow.Length; ++i)
            if (!GearReference.Finite(Flow[i], VolumeFlow[i], Transferred[i], TransferCorrection[i]) ||
                !GearReference.Finite(Enthalpy[i], EnthalpyCorrection[i], Entropy[i], EntropyCorrection[i])) return false;
        return true;
    }
    internal ulong Hash(ulong hash)
    {
        if (ReferenceVolume is not null) foreach (double value in ReferenceVolume) hash = Numeric.Hash(hash, value);
        for (int i = 0; i < Mass.Length; ++i)
        { hash = Numeric.Hash(hash, Mass[i]); hash = Numeric.Hash(hash, MassCorrection[i]); hash = Numeric.Hash(hash, Energy[i]); hash = Numeric.Hash(hash, EnergyCorrection[i]); }
        for (int i = 0; i < Flow.Length; ++i)
        {
            hash = Numeric.Hash(hash, Flow[i]); hash = Numeric.Hash(hash, VolumeFlow[i]);
            hash = Numeric.Hash(hash, Transferred[i]); hash = Numeric.Hash(hash, TransferCorrection[i]);
            hash = Numeric.Hash(hash, Enthalpy[i]); hash = Numeric.Hash(hash, EnthalpyCorrection[i]);
            hash = Numeric.Hash(hash, Entropy[i]); hash = Numeric.Hash(hash, EntropyCorrection[i]);
        }
        return hash;
    }
}

public sealed partial class CompiledModel
{
    internal CompiledFuelVolume[] FuelVolumes { get; private set; } = [];
    internal CompiledFuelLink[] FuelLinks { get; private set; } = [];
    internal int[] FuelByNode { get; private set; } = [];
    internal int[] FuelLinkByComponent { get; private set; } = [];
    public bool HasEquilibriumFuelNetwork => FuelVolumes.Length != 0;

    private void CompileFuelVolumes(NodeDefinition[] definitions)
    {
        FuelByNode = new int[Nodes.Length]; Array.Fill(FuelByNode, -1);
        var volumes = new List<CompiledFuelVolume>();
        for (int i = 0; i < Nodes.Length; ++i)
        {
            if (Nodes[i].Domain != Domain.Fuel) continue;
            var n = Nodes[i]; var d = definitions[i].Fuel!;
            Require(d.LiquidBulkModulus == default, DiagnosticCode.Schema, n.Id, "fuel.liquid_bulk_modulus", "Bulk modulus applies only to compressible_fuel nodes.");
            double Q(Quantity q, Unit unit, string field) => Convert(q, unit, n.Id, "fuel." + field);
            double cv = Q(d.VaporSpecificHeat, Unit.JoulePerKilogramKelvin, "vapor_specific_heat"),
                cl = Q(d.LiquidSpecificHeat, Unit.JoulePerKilogramKelvin, "liquid_specific_heat"),
                reference = Q(d.ReferenceTemperature, Unit.Kelvin, "reference_temperature"),
                latent = Q(d.LatentInternalEnergy, Unit.JoulePerKilogram, "latent_internal_energy"),
                r = Q(d.GasConstant, Unit.JoulePerKilogramKelvin, "gas_constant"),
                pressure = Q(d.ReferencePressure, Unit.Pascal, "reference_pressure"),
                min = Q(d.MinimumTemperature, Unit.Kelvin, "minimum_temperature"), max = Q(d.MaximumTemperature, Unit.Kelvin, "maximum_temperature"),
                compliance = Q(d.Compliance, Unit.CubicMeterPerPascal, "compliance"), density = Q(d.LiquidDensity, Unit.KilogramPerCubicMeter, "liquid_density"),
                limit = Q(d.MaximumPressure, Unit.Pascal, "maximum_pressure"), heating = Q(d.LowerHeatingValue, Unit.JoulePerKilogram, "lower_heating_value");
            Require(heating > 0, DiagnosticCode.Range, n.Id, "fuel.lower_heating_value", "Declare a positive finite fuel heating value; research data stays unverified.");
            CompliantFuelVolume law;
            try { law = new(new(new(cl, cv, reference, latent, 0), r, pressure, min, max), n.Storage, compliance, density, limit); }
            catch (ArgumentException error) { throw new ModelCompileException(DiagnosticCode.Range, n.Id, "fuel", error.Message); }
            Require(law.Evaluate(n.Initial, n.Position, out var point) == FuelVolumeStatus.Success && Numeric.Finite(n.Position * heating),
                DiagnosticCode.Range, n.Id, "fuel.initial", "Use positive total mass and temperature inside the declared vessel/material bounds.");
            FuelByNode[i] = volumes.Count; volumes.Add(new(i, law, point, heating));
        }
        FuelVolumes = volumes.ToArray();
    }

    private void CompileFuelLinks()
    {
        FuelLinkByComponent = new int[Components.Length]; Array.Fill(FuelLinkByComponent, -1);
        var links = new List<CompiledFuelLink>();
        for (int i = 0; i < Components.Length; ++i)
        {
            var c = Components[i]; if (c.Kind is not (ComponentKind.FuelRestriction or ComponentKind.FuelHeatLink or ComponentKind.FuelCheckValve)) continue;
            int a = FuelByNode[c.A]; bool heat = c.Kind == ComponentKind.FuelHeatLink;
            int b = heat ? Nodes[c.B].Index : FuelByNode[c.B];
            if (!heat) Require(FuelCompatible(FuelVolumes[a], FuelVolumes[b]), DiagnosticCode.Connection, c.Id, "fuel.material",
                "Connected fuel volumes require equal density, caloric/phase reference, temperature limits and heating value.");
            FuelLinkByComponent[i] = links.Count; links.Add(new(i, a, b, c.P0, heat, heat ? Nodes[c.B].Storage : 0, c.Kind == ComponentKind.FuelCheckValve));
        }
        FuelLinks = links.ToArray();
    }

    private static bool FuelCompatible(CompiledFuelVolume a, CompiledFuelVolume b)
    {
        var x = a.Law.Material; var y = b.Law.Material; var cx = x.Caloric; var cy = y.Caloric;
        return a.HeatingValue == b.HeatingValue && a.Law.LiquidDensityKilogramsPerCubicMeter == b.Law.LiquidDensityKilogramsPerCubicMeter &&
            x.GasConstantJoulesPerKilogramKelvin == y.GasConstantJoulesPerKilogramKelvin && x.ReferencePressurePascals == y.ReferencePressurePascals &&
            x.MinimumTemperatureKelvin == y.MinimumTemperatureKelvin && x.MaximumTemperatureKelvin == y.MaximumTemperatureKelvin &&
            cx.LiquidSpecificHeatJoulesPerKilogramKelvin == cy.LiquidSpecificHeatJoulesPerKilogramKelvin && cx.VaporSpecificHeatJoulesPerKilogramKelvin == cy.VaporSpecificHeatJoulesPerKilogramKelvin &&
            cx.SaturationTemperatureKelvin == cy.SaturationTemperatureKelvin && cx.LatentInternalEnergyJoulesPerKilogram == cy.LatentInternalEnergyJoulesPerKilogram;
    }
}

/// <summary>
/// Homogeneous dissipative reference-volume flow, with no resolved link inertia or choking.
/// Pairwise backward Euler uses donor enthalpy at the accepted end state; identical mass
/// and energy transfers enter both vessels. Finite-bath heat is another conservative substep.
/// </summary>
internal sealed class EquilibriumFuelSolver(CompiledModel model)
{
    private readonly Factorization _factor = new(2);
    private readonly double[,] _jacobian = new double[2, 2];
    private readonly double[] _correction = new double[2];

    internal bool Advance(EquilibriumFuelState state, double[] temperature, double[] inputs, double seconds)
    {
        for (int i = 0; i < model.FuelLinks.Length; ++i)
        {
            var link = model.FuelLinks[i];
            if (link.Heat)
            {
                if (model.FuelVolumes[link.A].Law.AdvanceHeatAtReferenceVolume(state.Mass[link.A], state.Energy[link.A], Reference(model.FuelVolumes[link.A], state, link.A), temperature[link.B], link.WallCapacity,
                    link.Conductance, seconds, out var heat) != FuelVolumeStatus.Success) return false;
                Numeric.Accumulate(heat.HeatFromWallJoules, ref state.Energy[link.A], ref state.EnergyCorrection[link.A]);
                temperature[link.B] = heat.WallTemperatureKelvin;
                state.Flow[i] += heat.HeatFromWallJoules;
                Numeric.Accumulate(heat.HeatFromWallJoules, ref state.Enthalpy[i], ref state.EnthalpyCorrection[i]);
                Numeric.Accumulate(heat.EntropyChangeJoulesPerKelvin, ref state.Entropy[i], ref state.EntropyCorrection[i]);
            }
            else if (inputs[link.Component] != 0 && !Transfer(state, i, link, seconds * link.Conductance * inputs[link.Component])) return false;
        }
        return state.Finite();
    }

    internal static bool Point(CompiledFuelVolume volume, EquilibriumFuelState state, int slot, out CompliantFuelPoint point) =>
        volume.Law.FlashAtReferenceVolume(state.Mass[slot], state.Energy[slot], Reference(volume, state, slot), out point) == FuelVolumeStatus.Success;

    private static double Reference(CompiledFuelVolume volume, EquilibriumFuelState state, int slot) => state.ReferenceVolume is null ? volume.Law.ReferenceVolumeCubicMeters : state.ReferenceVolume[slot];

    // Partial derivatives at fixed M,E, including elastic storage and coexistence.
    internal static void Derivatives(CompliantFuelVolume law, CompliantFuelPoint p, out double pm, out double pe, out double hm, out double he, out double vm, out double ve, double referenceVolumeCubicMeters)
    {
        double r = law.Material.GasConstantJoulesPerKilogramKelvin, density = law.LiquidDensityKilogramsPerCubicMeter;
        double cv = law.Material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin, cl = law.Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin;
        double ul = cl * p.TemperatureKelvin + law.Material.Caloric.LiquidEnergyOffsetJoulesPerKilogram, em, pt;
        if (p.Phase == FuelVolumePhase.Liquid) { pm = 1 / density / law.ComplianceCubicMetersPerPascal; pe = 0; }
        else
        {
            double direct;
            if (p.Phase == FuelVolumePhase.Vapor)
            {
                double denominator = referenceVolumeCubicMeters + 2 * law.ComplianceCubicMetersPerPascal * p.PressurePascals;
                direct = r * p.TemperatureKelvin / denominator; pt = p.MassKilograms * r / denominator;
                em = cv * p.TemperatureKelvin + law.ComplianceCubicMetersPerPascal * p.PressurePascals * direct;
            }
            else
            {
                law.Saturation(p.TemperatureKelvin, out var sat); direct = 0; pt = sat.PressureDerivativePascalsPerKelvin;
                em = ul - law.Material.LatentInternalEnergy(p.TemperatureKelvin) / density /
                    (sat.VaporSpecificVolumeCubicMetersPerKilogram - 1 / density);
            }
            pe = pt / p.HeatCapacityJoulesPerKelvin; pm = direct - pe * em;
        }
        hm = (p.VolumeCubicMeters * pm - p.SpecificEnthalpyJoulesPerKilogram) / p.MassKilograms;
        he = (1 + p.VolumeCubicMeters * pe) / p.MassKilograms;
        vm = (law.ComplianceCubicMetersPerPascal * pm - p.VolumeCubicMeters / p.MassKilograms) / p.MassKilograms;
        ve = law.ComplianceCubicMetersPerPascal * pe / p.MassKilograms;
    }

    private bool Transfer(EquilibriumFuelState state, int slot, CompiledFuelLink link, double kh)
    {
        if (!Point(model.FuelVolumes[link.A], state, link.A, out var pa) || !Point(model.FuelVolumes[link.B], state, link.B, out var pb)) return false;
        if (pa.PressurePascals == pb.PressurePascals || kh == 0 || link.OneWay && pa.PressurePascals < pb.PressurePascals) return true;
        bool forward = pa.PressurePascals > pb.PressurePascals;
        int source = forward ? link.A : link.B, sink = forward ? link.B : link.A;
        var a = model.FuelVolumes[source]; var b = model.FuelVolumes[sink];
        double m0 = state.Mass[source], m1 = state.Mass[sink], e0 = state.Energy[source], e1 = state.Energy[sink];
        double massScale = Math.Min(m0, m1), energyScale = Math.Max(Math.Abs(e0) + Math.Abs(e1), massScale * a.Law.Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin * pa.TemperatureKelvin);
        double referenceA = Reference(a, state, source), referenceB = Reference(b, state, sink);
        double maximumMassB = b.Law.LiquidDensityKilogramsPerCubicMeter * (referenceB + b.Law.ComplianceCubicMetersPerPascal * b.Law.MaximumPressurePascals);
        double q = 0, energy = 0;
        bool Evaluate(double mass, double transfer, bool derivative, out CompliantFuelPoint left, out CompliantFuelPoint right, out double r0, out double r1, out double norm)
        {
            left = right = default; r0 = r1 = norm = double.PositiveInfinity;
            if (mass < 0 || mass >= m0 || m1 + mass > maximumMassB ||
                a.Law.FlashAtReferenceVolume(m0 - mass, e0 - transfer, referenceA, out left) != FuelVolumeStatus.Success ||
                b.Law.FlashAtReferenceVolume(m1 + mass, e1 + transfer, referenceB, out right) != FuelVolumeStatus.Success) return false;
            double v = left.VolumeCubicMeters / left.MassKilograms, dp = left.PressurePascals - right.PressurePascals;
            r0 = mass - kh * dp / v; r1 = transfer - mass * left.SpecificEnthalpyJoulesPerKilogram;
            if (model.HasFuelChambers || link.OneWay)
            {
                double massTolerance = 128 * GearReference.Epsilon * (mass + kh * (left.PressurePascals + right.PressurePascals) * left.MassKilograms / left.VolumeCubicMeters);
                double energyTolerance = 128 * GearReference.Epsilon * (Math.Abs(transfer) + mass * (Math.Abs(left.SpecificEnthalpyJoulesPerKilogram) + left.HeatCapacityJoulesPerKelvin * left.TemperatureKelvin / left.MassKilograms));
                if (!Numeric.Finite(massTolerance) || !Numeric.Finite(energyTolerance)) return false;
                norm = Math.Max(Math.Abs(r0) / massTolerance, energyTolerance == 0 ? (r1 == 0 ? 0 : double.PositiveInfinity) : Math.Abs(r1) / energyTolerance);
            }
            else norm = Math.Max(Math.Abs(r0) / massScale, Math.Abs(r1) / energyScale);
            if (derivative)
            {
                Derivatives(a.Law, left, out double am, out double ae, out double hm, out double he, out double vm, out double ve, referenceA);
                Derivatives(b.Law, right, out double bm, out double be, out _, out _, out _, out _, referenceB);
                _jacobian[0, 0] = 1 + kh / v * (am + bm - dp * vm / v);
                _jacobian[0, 1] = kh / v * (ae + be - dp * ve / v) * energyScale / massScale;
                _jacobian[1, 0] = (-left.SpecificEnthalpyJoulesPerKilogram + mass * hm) * massScale / energyScale;
                _jacobian[1, 1] = 1 + mass * he;
            }
            return Numeric.Finite(norm);
        }
        CompliantFuelPoint endA = default, endB = default;
        bool converged = false;
        for (int iteration = 0; iteration < 40; ++iteration)
        {
            if (!Evaluate(q, energy, true, out endA, out endB, out double rm, out double re, out double norm)) return false;
            double toleranceMass = 128 * GearReference.Epsilon * (q + kh * (endA.PressurePascals + endB.PressurePascals) * endA.MassKilograms / endA.VolumeCubicMeters);
            double toleranceEnergy = 128 * GearReference.Epsilon * (Math.Abs(energy) + q * (Math.Abs(endA.SpecificEnthalpyJoulesPerKilogram) + endA.HeatCapacityJoulesPerKelvin * endA.TemperatureKelvin / endA.MassKilograms));
            if (Math.Abs(rm) <= toleranceMass && Math.Abs(re) <= toleranceEnergy) { converged = true; break; }
            _correction[0] = -rm / massScale; _correction[1] = -re / energyScale;
            if (!_factor.Refactor(_jacobian) || !_factor.Solve(_correction)) return false;
            bool accepted = false;
            double fraction = 1;
            for (int search = 0; search < 24; ++search, fraction *= .5)
            {
                double nextQ = q + fraction * massScale * _correction[0], nextEnergy = energy + fraction * energyScale * _correction[1];
                if (Evaluate(nextQ, nextEnergy, false, out _, out _, out _, out _, out double nextNorm) && nextNorm < norm)
                { q = nextQ; energy = nextEnergy; accepted = true; break; }
            }
            if (!accepted) return false;
        }
        if (!converged || endA.PressurePascals < endB.PressurePascals - 128 * GearReference.Epsilon * endA.PressurePascals) return false;
        double entropy = endA.EntropyJoulesPerKelvin + endB.EntropyJoulesPerKelvin - pa.EntropyJoulesPerKelvin - pb.EntropyJoulesPerKelvin;
        double entropyScale = (endA.HeatCapacityJoulesPerKelvin + endB.HeatCapacityJoulesPerKelvin) + Math.Abs(pa.EntropyJoulesPerKelvin) + Math.Abs(pb.EntropyJoulesPerKelvin);
        if (entropy < -256 * GearReference.Epsilon * entropyScale) return false;
        Numeric.Accumulate(-q, ref state.Mass[source], ref state.MassCorrection[source]); Numeric.Accumulate(q, ref state.Mass[sink], ref state.MassCorrection[sink]);
        Numeric.Accumulate(-energy, ref state.Energy[source], ref state.EnergyCorrection[source]); Numeric.Accumulate(energy, ref state.Energy[sink], ref state.EnergyCorrection[sink]);
        double sign = forward ? 1 : -1;
        state.Flow[slot] += sign * q; state.VolumeFlow[slot] += sign * q * endA.VolumeCubicMeters / endA.MassKilograms;
        Numeric.Accumulate(sign * q, ref state.Transferred[slot], ref state.TransferCorrection[slot]);
        Numeric.Accumulate(sign * energy, ref state.Enthalpy[slot], ref state.EnthalpyCorrection[slot]);
        Numeric.Accumulate(entropy, ref state.Entropy[slot], ref state.EntropyCorrection[slot]);
        return true;
    }
}
