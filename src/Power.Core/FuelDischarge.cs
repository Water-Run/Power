// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public enum FuelDischargePhaseModel { Frozen = 1 }
public sealed record FuelDischargeDefinition
{
    public uint FilmComponent { get; init; }
    public FuelDischargePhaseModel PhaseModel { get; init; }
    public Quantity WallKineticFraction { get; init; }
    public InjectorNeedleDefinition? Needle { get; init; }
}
internal sealed record CompiledFuelDischarge(int Component, int Source, int Film, FrozenFuelNozzle Nozzle, double WallFraction, CompiledNeedle? Needle);

internal sealed class FuelDischargeState(int count)
{
    internal readonly double[] Mass = new double[count], MassCorrection = new double[count], Energy = new double[count], EnergyCorrection = new double[count],
        Kinetic = new double[count], KineticCorrection = new double[count], Heat = new double[count], HeatCorrection = new double[count],
        Impulse = new double[count], ImpulseCorrection = new double[count], Flow = new double[count], Force = new double[count],
        Throat = new double[count], ExitTemperature = new double[count], ExitVelocity = new double[count], Choked = new double[count];
    internal void CopyFrom(FuelDischargeState other)
    {
        Array.Copy(other.Mass, Mass, count); Array.Copy(other.MassCorrection, MassCorrection, count);
        Array.Copy(other.Energy, Energy, count); Array.Copy(other.EnergyCorrection, EnergyCorrection, count);
        Array.Copy(other.Kinetic, Kinetic, count); Array.Copy(other.KineticCorrection, KineticCorrection, count);
        Array.Copy(other.Heat, Heat, count); Array.Copy(other.HeatCorrection, HeatCorrection, count);
        Array.Copy(other.Impulse, Impulse, count); Array.Copy(other.ImpulseCorrection, ImpulseCorrection, count);
        Array.Copy(other.Flow, Flow, count); Array.Copy(other.Force, Force, count); Array.Copy(other.Throat, Throat, count);
        Array.Copy(other.ExitTemperature, ExitTemperature, count); Array.Copy(other.ExitVelocity, ExitVelocity, count); Array.Copy(other.Choked, Choked, count);
    }
    internal void BeginTick() { Array.Clear(Flow, 0, count); Array.Clear(Force, 0, count); }
    internal bool EndTick(double seconds)
    { for (int i = 0; i < count; ++i) { Flow[i] /= seconds; Force[i] /= seconds; } return Finite(); }
    internal bool Finite()
    {
        for (int i = 0; i < count; ++i)
            if (!GearReference.Finite(Mass[i], MassCorrection[i], Energy[i], EnergyCorrection[i]) || !GearReference.Finite(Kinetic[i], KineticCorrection[i], Heat[i], HeatCorrection[i]) ||
                !GearReference.Finite(Impulse[i], ImpulseCorrection[i], Flow[i], Force[i]) || !GearReference.Finite(Throat[i], ExitTemperature[i], ExitVelocity[i], Choked[i])) return false;
        return true;
    }
    internal ulong Hash(ulong hash)
    {
        for (int i = 0; i < count; ++i)
        {
            hash = Numeric.Hash(hash, Mass[i]); hash = Numeric.Hash(hash, MassCorrection[i]); hash = Numeric.Hash(hash, Energy[i]); hash = Numeric.Hash(hash, EnergyCorrection[i]);
            hash = Numeric.Hash(hash, Kinetic[i]); hash = Numeric.Hash(hash, KineticCorrection[i]); hash = Numeric.Hash(hash, Heat[i]); hash = Numeric.Hash(hash, HeatCorrection[i]);
            hash = Numeric.Hash(hash, Impulse[i]); hash = Numeric.Hash(hash, ImpulseCorrection[i]); hash = Numeric.Hash(hash, Flow[i]); hash = Numeric.Hash(hash, Force[i]);
            hash = Numeric.Hash(hash, Throat[i]); hash = Numeric.Hash(hash, ExitTemperature[i]); hash = Numeric.Hash(hash, ExitVelocity[i]); hash = Numeric.Hash(hash, Choked[i]);
        }
        return hash;
    }
}

public sealed partial class CompiledModel
{
    internal CompiledFuelDischarge[] FuelDischarges { get; private set; } = [];
    internal int[] FuelDischargeByComponent { get; private set; } = [];
    public bool HasFuelDischarge => FuelDischarges.Length != 0;
    private void CompileFuelDischarges(ComponentDefinition[] definitions)
    {
        FuelDischargeByComponent = new int[Components.Length]; Array.Fill(FuelDischargeByComponent, -1); var result = new List<CompiledFuelDischarge>();
        for (int i = 0; i < Components.Length; ++i)
        {
            var c = Components[i]; if (c.Kind != ComponentKind.FuelDischarge) continue; var d = definitions[i].FuelDischarge!;
            int film = Array.FindIndex(FuelFilms, f => Components[f.Component].Id == d.FilmComponent);
            Require(film >= 0 && FuelFilms[film].FiniteVolume is not null && FuelFilms[film].Gas == Nodes[c.B].Index, DiagnosticCode.Connection, c.Id,
                "fuel_discharge.film_component", "Use an exclusively volume-owning finite-displacement film at the gas receiver.");
            int source = FuelByNode[c.A]; var material = FuelVolumes[source].Law.Material; var receiver = FuelFilms[film]; var reference = receiver.Phase!;
            Require(d.PhaseModel == FuelDischargePhaseModel.Frozen, DiagnosticCode.Schema, c.Id, "fuel_discharge.phase_mode", "Declare the frozen-quality nozzle transit model explicitly.");
            // Gas cv is derived from gamma; fuel nodes declare cv directly. Allow
            // only binary64 conversion rounding between these two representations.
            Require(FuelVolumes[source].Law.LiquidDensityKilogramsPerCubicMeter == receiver.FiniteVolume!.DensityKilogramsPerCubicMeter &&
                FuelVolumes[source].HeatingValue == receiver.HeatingValue && material.GasConstantJoulesPerKilogramKelvin == reference.GasConstantJoulesPerKilogramKelvin &&
                Math.Abs(material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin - reference.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin) <=
                    64 * GearReference.Epsilon * Math.Max(material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin, reference.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin) &&
                material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin == reference.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin &&
                material.Caloric.SaturationTemperatureKelvin == reference.Caloric.SaturationTemperatureKelvin && material.Caloric.LatentInternalEnergyJoulesPerKilogram == reference.Caloric.LatentInternalEnergyJoulesPerKilogram &&
                material.ReferencePressurePascals == reference.ReferencePressurePascals && material.MinimumTemperatureKelvin == reference.MinimumTemperatureKelvin && material.MaximumTemperatureKelvin == reference.MaximumTemperatureKelvin,
                DiagnosticCode.Connection, c.Id, "fuel_discharge.material", "Source and receiver require equal pure-fuel density, caloric/phase reference, bounds, gas R/cv and heating value.");
            double fraction = Convert(d.WallKineticFraction, Unit.Fraction, c.Id, "fuel_discharge.wall_kinetic_fraction");
            Require(fraction is >= 0 and <= 1, DiagnosticCode.Range, c.Id, "fuel_discharge.wall_kinetic_fraction", "Declare the jet kinetic-energy fraction thermalized at the finite stationary film wall in [0,1].");
            CompiledNeedle? needle = null;
            if (d.Needle is { } n)
            {
                int node = Array.FindIndex(Nodes, x => x.Id == n.NeedleNode);
                Require(node >= 0 && Nodes[node].Domain == Domain.Translational, DiagnosticCode.Connection, c.Id, "fuel_discharge.needle_node", "Use a translational needle node.");
                double closed = Convert(n.ClosedPosition, Unit.Meter, c.Id, "fuel_discharge.closed_position"), open = Convert(n.FullOpenPosition, Unit.Meter, c.Id, "fuel_discharge.full_open_position");
                Require(open > closed && Numeric.Finite(open - closed), DiagnosticCode.Range, c.Id, "fuel_discharge.needle", "Full-open position must exceed closed position.");
                needle = new(Nodes[node].Index, closed, open);
            }
            FuelDischargeByComponent[i] = result.Count;
            result.Add(new(i, source, film, new(material, receiver.FiniteVolume.DensityKilogramsPerCubicMeter, c.P0, c.P1), fraction, needle));
        }
        FuelDischarges = result.ToArray();
    }
}

/// <summary>Conservative end-state mass/enthalpy/kinetic solve into a finite-displacement receiver.</summary>
internal sealed class FuelDischargeSolver(CompiledModel model, FuelFilmSolver films)
{
    private readonly double[] _values = new double[3], _residual = new double[3], _trial = new double[3], _trialResidual = new double[3], _difference = new double[3], _change = new double[3];
    private readonly double[,] _jacobian = new double[3, 3];
    private readonly Factorization _factor = new(3);
    internal bool Advance(FuelDischargeState state, EquilibriumFuelState sourceState, FuelFilmState filmState, MixtureState mixture,
        double[] gasMass, double[] gasEnergy, double[] dynamics, double[] inputs, LiquidTankState? tanks, double seconds, bool reverse = false)
    {
        for (int order = 0; order < model.FuelDischarges.Length; ++order)
        {
            int slot = reverse ? model.FuelDischarges.Length - 1 - order : order; var d = model.FuelDischarges[slot]; var film = model.FuelFilms[d.Film];
            double opening = model.Gas!.Opening(d.Component, dynamics, inputs) * (d.Needle?.Opening(dynamics) ?? 1);
            if (opening == 0) continue;
            var source = model.FuelVolumes[d.Source]; double sourceVolume = sourceState.ReferenceVolume is null ? source.Law.ReferenceVolumeCubicMeters : sourceState.ReferenceVolume[d.Source];
            if (source.Law.FlashAtReferenceVolume(sourceState.Mass[d.Source], sourceState.Energy[d.Source], sourceVolume, out var initialSource) != FuelVolumeStatus.Success) return false;
            double cavity = model.Gas.CavityVolumeAt(film.Gas, dynamics, tanks), carrier = mixture.Air[film.Gas] + mixture.Products[film.Gas];
            double fuel = filmState.Mass[d.Film] + mixture.Fuel[film.Gas], receiverEnergy = filmState.Energy[d.Film] + gasEnergy[film.Gas];
            var finite = film.FiniteVolume!;
            if (!finite.TryFlashEnergyAtCapacity(fuel, carrier, receiverEnergy, cavity, out var initialReceiver) ||
                !finite.TryPointAtCapacity(initialReceiver.TemperatureKelvin, fuel, carrier, cavity, out var initialPoint)) return false;
            var status = d.Nozzle.Evaluate(initialSource.PressurePascals, initialSource.TemperatureKelvin, initialSource.VaporMassFraction, initialPoint.PressurePascals, opening, out var initialNozzle);
            if (status != FuelNozzleStatus.Success) return false;
            if (initialNozzle.MassFlowKilogramsPerSecond == 0) continue;
            double m0 = sourceState.Mass[d.Source], e0 = sourceState.Energy[d.Source], wallT = films.CurrentWallTemperature(film.Wall);
            double massScale = seconds * initialNozzle.MassFlowKilogramsPerSecond;
            double energyScale = massScale * (Math.Abs(initialNozzle.StagnationEnthalpyJoulesPerKilogram) + initialSource.HeatCapacityJoulesPerKelvin * initialSource.TemperatureKelvin / m0);
            double kineticScale = massScale * Math.Max(initialNozzle.KineticEnergyJoulesPerKilogram, initialSource.HeatCapacityJoulesPerKelvin * initialSource.TemperatureKelvin / m0 * 1e-6);
            if (!GearReference.Finite(massScale, energyScale, kineticScale, wallT) || massScale <= 0 || energyScale <= 0 || kineticScale <= 0) return false;
            bool Evaluate(double[] values, double[] residual, out double norm, out CompliantFuelPoint left, out FiniteFuelPhaseState right, out FiniteTankPhasePoint point, out FrozenFuelNozzlePoint nozzle)
            {
                left = default; right = default; point = default; nozzle = default; norm = double.PositiveInfinity;
                double mass = values[0] * massScale, enthalpy = values[1] * energyScale, kinetic = values[2] * kineticScale;
                if (mass < 0 || mass > .25 * m0 || kinetic < 0 || source.Law.FlashAtReferenceVolume(m0 - mass, e0 - enthalpy, sourceVolume, out left) != FuelVolumeStatus.Success ||
                    !finite.TryFlashEnergyAtCapacity(fuel + mass, carrier, receiverEnergy + enthalpy - d.WallFraction * kinetic, cavity, out right) ||
                    !finite.TryPointAtCapacity(right.TemperatureKelvin, fuel + mass, carrier, cavity, out point)) return false;
                if (d.Nozzle.Evaluate(left.PressurePascals, left.TemperatureKelvin, left.VaporMassFraction, point.PressurePascals, opening, out nozzle) != FuelNozzleStatus.Success) return false;
                residual[0] = (mass - seconds * nozzle.MassFlowKilogramsPerSecond) / massScale;
                residual[1] = (enthalpy - mass * nozzle.StagnationEnthalpyJoulesPerKilogram) / energyScale;
                residual[2] = (kinetic - mass * nozzle.KineticEnergyJoulesPerKilogram) / kineticScale;
                norm = Math.Max(Math.Abs(residual[0]), Math.Max(Math.Abs(residual[1]), Math.Abs(residual[2])));
                return Numeric.Finite(norm);
            }
            Array.Clear(_values, 0, 3); bool converged = false;
            CompliantFuelPoint endSource = default; FiniteFuelPhaseState endReceiver = default; FiniteTankPhasePoint endPoint = default; FrozenFuelNozzlePoint endNozzle = default;
            for (int iteration = 0; iteration < 40; ++iteration)
            {
                if (!Evaluate(_values, _residual, out double norm, out endSource, out endReceiver, out endPoint, out endNozzle)) return false;
                if (norm <= 1e-10) { converged = true; break; }
                for (int column = 0; column < 3; ++column)
                {
                    Array.Copy(_values, _trial, 3); double delta = 1e-5 * Math.Max(1, Math.Abs(_values[column])); _trial[column] += delta;
                    if (!Evaluate(_trial, _difference, out _, out _, out _, out _, out _))
                    { _trial[column] = _values[column] - delta; delta = -delta; if (!Evaluate(_trial, _difference, out _, out _, out _, out _, out _)) return false; }
                    for (int row = 0; row < 3; ++row) _jacobian[row, column] = (_difference[row] - _residual[row]) / delta;
                }
                for (int row = 0; row < 3; ++row) _change[row] = -_residual[row];
                if (!_factor.Refactor(_jacobian) || !_factor.Solve(_change)) return false;
                double fraction = 1; bool accepted = false;
                for (int search = 0; search < 24; ++search, fraction *= .5)
                {
                    for (int row = 0; row < 3; ++row) _trial[row] = _values[row] + fraction * _change[row];
                    if (Evaluate(_trial, _trialResidual, out double candidate, out _, out _, out _, out _) && candidate < norm)
                    { Array.Copy(_trial, _values, 3); accepted = true; break; }
                }
                if (!accepted) return false;
            }
            if (!converged) return false;
            double q = _values[0] * massScale, h = _values[1] * energyScale, k = _values[2] * kineticScale, heat = d.WallFraction * k;
            double wallNext = wallT + heat / film.WallCapacity;
            double entropy = endSource.EntropyJoulesPerKelvin - initialSource.EntropyJoulesPerKelvin + finite.Entropy(endPoint, carrier) - finite.Entropy(initialPoint, carrier) + film.WallCapacity * Math.Log(wallNext / wallT);
            double entropyScale = Math.Abs(initialSource.EntropyJoulesPerKelvin) + Math.Abs(finite.Entropy(initialPoint, carrier)) + initialSource.HeatCapacityJoulesPerKelvin + initialPoint.HeatCapacityJoulesPerKelvin + film.WallCapacity;
            if (!Numeric.Finite(entropy) || entropy < -512 * GearReference.Epsilon * entropyScale) return false;
            Numeric.Accumulate(-q, ref sourceState.Mass[d.Source], ref sourceState.MassCorrection[d.Source]); Numeric.Accumulate(-h, ref sourceState.Energy[d.Source], ref sourceState.EnergyCorrection[d.Source]);
            double oldLiquid = filmState.Mass[d.Film]; filmState.Mass[d.Film] = endReceiver.Liquid.LiquidMassKilograms; filmState.Energy[d.Film] = endReceiver.Liquid.ThermalEnergyJoules;
            // Remove advected liquid from the net phase history: only local phase transfer counts.
            filmState.TickVapor[d.Film] += oldLiquid + q * (1 - endSource.VaporMassFraction) - endReceiver.Liquid.LiquidMassKilograms;
            mixture.Fuel[film.Gas] = endReceiver.VaporMassKilograms; gasMass[film.Gas] = endReceiver.VaporMassKilograms + carrier; gasEnergy[film.Gas] = endReceiver.GasEnergyJoules;
            films.AddHeat(film.Wall, film.WallCapacity, heat);
            Numeric.Accumulate(q, ref state.Mass[slot], ref state.MassCorrection[slot]); Numeric.Accumulate(h, ref state.Energy[slot], ref state.EnergyCorrection[slot]);
            Numeric.Accumulate(k, ref state.Kinetic[slot], ref state.KineticCorrection[slot]); Numeric.Accumulate(heat, ref state.Heat[slot], ref state.HeatCorrection[slot]);
            double impulse = q * endNozzle.ExitVelocityMetersPerSecond; Numeric.Accumulate(impulse, ref state.Impulse[slot], ref state.ImpulseCorrection[slot]);
            state.Flow[slot] += q; state.Force[slot] += impulse; state.Throat[slot] = endNozzle.ThroatPressurePascals; state.ExitTemperature[slot] = endNozzle.ExitTemperatureKelvin;
            state.ExitVelocity[slot] = endNozzle.ExitVelocityMetersPerSecond; state.Choked[slot] = endNozzle.Choked ? 1 : 0;
        }
        return state.Finite();
    }
}
