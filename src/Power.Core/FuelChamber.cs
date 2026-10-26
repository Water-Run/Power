// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record FuelChamberDefinition
{
    public Quantity MinimumReferenceVolume { get; init; }
    public Quantity SweptVolume { get; init; }
    public Quantity Phase { get; init; }
}

/// <summary>Massless sinusoidal chamber geometry; shaft angle and pressure work are coupled by the solver.</summary>
public sealed class SinusoidalFuelChamber
{
    public double MinimumReferenceVolumeCubicMeters { get; }
    public double SweptVolumeCubicMeters { get; }
    public double PhaseRadians { get; }
    public SinusoidalFuelChamber(double minimumReferenceVolumeCubicMeters, double sweptVolumeCubicMeters, double phaseRadians)
    {
        if (!Numeric.Finite(minimumReferenceVolumeCubicMeters) || minimumReferenceVolumeCubicMeters <= 0 ||
            !Numeric.Finite(sweptVolumeCubicMeters) || sweptVolumeCubicMeters <= 0 || !Numeric.Finite(phaseRadians) ||
            !Numeric.Finite(minimumReferenceVolumeCubicMeters + sweptVolumeCubicMeters))
            throw new ArgumentException("Declare positive finite minimum/swept volume and finite phase.");
        MinimumReferenceVolumeCubicMeters = minimumReferenceVolumeCubicMeters; SweptVolumeCubicMeters = sweptVolumeCubicMeters;
        PhaseRadians = phaseRadians % (2 * Math.PI);
    }
    private double PhaseAt(double angle) => (angle % (2 * Math.PI) - PhaseRadians) % (2 * Math.PI);
    public double ReferenceVolumeAt(double angleRadians)
    { double sine = Math.Sin(PhaseAt(angleRadians) / 2); return MinimumReferenceVolumeCubicMeters + SweptVolumeCubicMeters * sine * sine; }
    public double VolumeDerivativeAt(double angleRadians) => .5 * SweptVolumeCubicMeters * Math.Sin(PhaseAt(angleRadians));
}

internal sealed record CompiledFuelChamber(int Component, int Fuel, int Crank, SinusoidalFuelChamber Geometry);
internal sealed class FuelChamberState(int count)
{
    internal readonly double[] Torque = new double[count], Power = new double[count], Work = new double[count], Correction = new double[count];
    internal void CopyFrom(FuelChamberState other)
    { Array.Copy(other.Torque, Torque, count); Array.Copy(other.Power, Power, count); Array.Copy(other.Work, Work, count); Array.Copy(other.Correction, Correction, count); }
    internal void BeginTick() { Array.Clear(Torque, 0, count); Array.Clear(Power, 0, count); }
    internal bool EndTick(double seconds)
    { for (int i = 0; i < count; ++i) { Torque[i] /= seconds; Power[i] /= seconds; } return Finite(); }
    internal bool Finite()
    { for (int i = 0; i < count; ++i) if (!GearReference.Finite(Torque[i], Power[i], Work[i], Correction[i])) return false; return true; }
    internal ulong Hash(ulong hash)
    { for (int i = 0; i < count; ++i) { hash = Numeric.Hash(hash, Torque[i]); hash = Numeric.Hash(hash, Power[i]); hash = Numeric.Hash(hash, Work[i]); hash = Numeric.Hash(hash, Correction[i]); } return hash; }
}

public sealed partial class CompiledModel
{
    internal CompiledFuelChamber[] FuelChambers { get; private set; } = [];
    internal int[] FuelChamberByComponent { get; private set; } = [];
    public bool HasFuelChambers => FuelChambers.Length != 0;
    private void CompileFuelChambers(ComponentDefinition[] definitions)
    {
        FuelChamberByComponent = new int[Components.Length]; Array.Fill(FuelChamberByComponent, -1);
        var chambers = new List<CompiledFuelChamber>(); var owners = new HashSet<int>();
        for (int i = 0; i < Components.Length; ++i)
        {
            var c = Components[i]; if (c.Kind != ComponentKind.FuelChamber) continue;
            var d = definitions[i].FuelChamber!;
            double minimum = Convert(d.MinimumReferenceVolume, Unit.CubicMeter, c.Id, "fuel_chamber.minimum_reference_volume"),
                swept = Convert(d.SweptVolume, Unit.CubicMeter, c.Id, "fuel_chamber.swept_volume"), phase = Convert(d.Phase, Unit.Radian, c.Id, "fuel_chamber.phase");
            SinusoidalFuelChamber geometry;
            try { geometry = new(minimum, swept, phase); }
            catch (ArgumentException error) { throw new ModelCompileException(DiagnosticCode.Range, c.Id, "fuel_chamber", error.Message); }
            int fuel = FuelByNode[c.B]; var volume = FuelVolumes[fuel]; double reference = geometry.ReferenceVolumeAt(Nodes[c.A].Position);
            Require(owners.Add(fuel), DiagnosticCode.Connection, c.Id, "fuel_chamber.node_b", "Each moving fuel volume has exactly one shaft/geometry owner.");
            Require(Math.Abs(reference - volume.Law.ReferenceVolumeCubicMeters) <= 128 * GearReference.Epsilon * reference,
                DiagnosticCode.Connection, c.Id, "fuel_chamber.initial_volume", "Fuel node storage must equal the declared chamber reference volume at the initial shaft angle.");
            // Validate geometric volume scales without assuming that the initial mass
            // can be compressed to every angle below the declared pressure limit.
            Require(volume.Law.EvaluateAtReferenceVolume(volume.Initial.TemperatureKelvin,
                volume.Initial.MassKilograms, minimum + swept, out _) == FuelVolumeStatus.Success,
                DiagnosticCode.Range, c.Id, "fuel_chamber.volume_range", "Use representable chamber volumes and a material interval covering the declared geometry.");
            FuelChamberByComponent[i] = chambers.Count; chambers.Add(new(i, fuel, Nodes[c.A].Index, geometry));
        }
        FuelChambers = chambers.ToArray();
    }
    internal bool FuelChamberWork(int slot, EquilibriumFuelState state, double oldAngle, double delta, out CompliantFuelWorkTransfer work, out double torque, out double nextReferenceVolume)
    {
        work = default; torque = nextReferenceVolume = double.NaN;
        if (!Numeric.Finite(oldAngle) || !Numeric.Finite(delta) || Math.Abs(delta) > CylinderSolver.MaxAngleStepRadians ||
            8 * GearReference.Epsilon * Math.Max(Math.Abs(oldAngle), Math.Abs(oldAngle + delta)) > CylinderSolver.MaxAngleStepRadians) return false;
        var chamber = FuelChambers[slot]; var volume = FuelVolumes[chamber.Fuel];
        double oldReference = state.ReferenceVolume![chamber.Fuel];
        nextReferenceVolume = delta == 0 ? oldReference : chamber.Geometry.ReferenceVolumeAt(oldAngle + delta);
        if (volume.Law.AdiabaticReferenceVolume(state.Mass[chamber.Fuel], state.Energy[chamber.Fuel], oldReference, nextReferenceVolume, out work) != FuelVolumeStatus.Success) return false;
        torque = delta == 0 ? work.Fluid.PressurePascals * chamber.Geometry.VolumeDerivativeAt(oldAngle) : -work.EnergyChangeJoules / delta;
        return Numeric.Finite(torque);
    }
}
