// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

internal sealed record CompiledCompressibleChamber(int Component, int Fuel, int Crank, SinusoidalFuelChamber Geometry);

public sealed partial class CompiledModel
{
    internal CompiledCompressibleChamber[] CompressibleChambers { get; private set; } = [];
    internal int[] CompressibleChamberByComponent { get; private set; } = [];
    public bool HasCompressibleFuelChambers => CompressibleChambers.Length != 0;
    internal int RelaxingChamberCount { get; private set; }
    public bool HasRelaxingFuelBoundaries => RelaxingChamberCount != 0 || RelaxingNozzleCount != 0;
    internal double CompressibleVolume(CompressibleFuelState state, int slot) => state.Volume is null ? Nodes[CompressibleFuels[slot].Node].Storage : state.Volume[slot];

    private void CompileCompressibleChambers(ComponentDefinition[] definitions)
    {
        CompressibleChamberByComponent = new int[Components.Length]; Array.Fill(CompressibleChamberByComponent, -1);
        var chambers = new List<CompiledCompressibleChamber>(); var owners = new HashSet<int>();
        for (int i = 0; i < Components.Length; ++i)
        {
            var c = Components[i]; if (c.Kind != ComponentKind.CompressibleFuelChamber) continue;
            var d = definitions[i].FuelChamber!;
            double minimum = Convert(d.MinimumReferenceVolume, Unit.CubicMeter, c.Id, "fuel_chamber.minimum_reference_volume"),
                swept = Convert(d.SweptVolume, Unit.CubicMeter, c.Id, "fuel_chamber.swept_volume"), phase = Convert(d.Phase, Unit.Radian, c.Id, "fuel_chamber.phase");
            SinusoidalFuelChamber geometry;
            try { geometry = new(minimum, swept, phase); }
            catch (ArgumentException error) { throw new ModelCompileException(DiagnosticCode.Range, c.Id, "fuel_chamber", error.Message); }
            int fuel = Nodes[c.B].Index; double initial = geometry.ReferenceVolumeAt(Nodes[c.A].Position);
            Require(owners.Add(fuel) && CompressibleCellOwner[fuel] < 0, DiagnosticCode.Connection, c.Id, "fuel_chamber.node_b", "Each moving compressible reservoir has one geometry owner and cannot be an internal duct cell.");
            Require(Math.Abs(initial - Nodes[c.B].Storage) <= 128 * GearReference.Epsilon * initial,
                DiagnosticCode.Connection, c.Id, "fuel_chamber.initial_volume", "Node storage must equal chamber volume at the initial shaft angle.");
            Require(CompressibleFuels[fuel].Velocity == 0, DiagnosticCode.Range, c.Id, "fuel_chamber.initial_velocity", "The mixed chamber has zero bulk velocity; only the boundary does mechanical work.");
            CompressibleChamberByComponent[i] = chambers.Count; chambers.Add(new(i, fuel, Nodes[c.A].Index, geometry));
        }
        CompressibleChambers = chambers.ToArray();
        RelaxingChamberCount = chambers.Count(c => CompressibleFuels[c.Fuel].Relaxation is not null);
    }
    internal bool CompressibleChamberWork(int slot, CompressibleFuelState state, double oldAngle, double delta,
        out CompressibleFuelWorkTransfer work, out double torque, out double nextVolume)
    {
        work = default; torque = nextVolume = double.NaN;
        if (!Numeric.Finite(oldAngle) || !Numeric.Finite(delta) || Math.Abs(delta) > CylinderSolver.MaxAngleStepRadians ||
            8 * GearReference.Epsilon * Math.Max(Math.Abs(oldAngle), Math.Abs(oldAngle + delta)) > CylinderSolver.MaxAngleStepRadians) return false;
        var chamber = CompressibleChambers[slot]; double previous = state.Volume![chamber.Fuel];
        nextVolume = delta == 0 ? previous : chamber.Geometry.ReferenceVolumeAt(oldAngle + delta);
        var fuel = CompressibleFuels[chamber.Fuel];
        FuelVolumeStatus status = fuel.Relaxation is {} relaxing
            ? relaxing.AdiabaticVolume(state.Mass[chamber.Fuel], state.Energy[chamber.Fuel], previous, nextVolume, state.VaporMass![chamber.Fuel] / state.Mass[chamber.Fuel], out work)
            : fuel.Law.AdiabaticVolume(state.Mass[chamber.Fuel], state.Energy[chamber.Fuel], previous, nextVolume, out work);
        if (status != FuelVolumeStatus.Success) return false;
        torque = delta == 0 ? work.Fluid.PressurePascals * chamber.Geometry.VolumeDerivativeAt(oldAngle) : -work.EnergyChangeJoules / delta;
        return Numeric.Finite(torque);
    }
}
