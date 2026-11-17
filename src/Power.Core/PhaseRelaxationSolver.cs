// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
namespace Power.Core;
public sealed partial class Simulation
{
    private bool AdvanceDuctInterval(State s,double duration)
    {
        if(!_model.HasRelaxingFuel)return _fuelDucts is null||_fuelDucts.Advance(s.CompressibleFuel!,s.Temperature,duration);
        var state=s.CompressibleFuel!;double remaining=duration;
        for(int step=0;remaining>0;++step)
        {
            if(step>=4096)return false;double h=remaining;
            foreach(var duct in _model.FuelDucts)
            {
                double speed=0,volume=double.PositiveInfinity;
                foreach(int slot in duct.Ports){speed=Math.Max(speed,Math.Abs(state.Momentum[slot]/state.Mass[slot])+_model.CompressibleFuels[slot].Law.AcousticSpeedBoundMetersPerSecond);volume=Math.Min(volume,_model.CompressibleVolume(state,slot));}
                h=Math.Min(h,.2*volume/(duct.Area*speed));
            }
            if(_model.HasRelaxingFuelBoundaries&&_compressibleNozzles is not null)
            {
                foreach(var nozzle in _model.CompressibleNozzles)
                {
                    double opening=CompressibleNozzleSolver.Opening(nozzle,s.Inputs,s.X);if(opening==0)continue;
                    if(!EquilibriumFuelDuctSolver.Point(_model,state,nozzle.A,out var a)||!EquilibriumFuelDuctSolver.Point(_model,state,nozzle.B,out var b))return false;
                    double pressureRounding=128*GearReference.Epsilon*Math.Max(nozzle.Law.Law.LiquidBulkModulusPascals,Math.Max(a.PressurePascals,b.PressurePascals));
                    if(a.PressurePascals-b.PressurePascals<=pressureRounding)continue;
                    if(nozzle.Evaluate(a,b.PressurePascals,opening,out var flow,out _)!=FuelNozzleStatus.Success)return false;
                    if(flow.MassFlowKilogramsPerSecond>0)h=Math.Min(h,.02*Math.Min(state.Mass[nozzle.A],state.Mass[nozzle.B])/flow.MassFlowKilogramsPerSecond);
                }
            }
            if(!Numeric.Finite(h)||h<=0||remaining-h==remaining||!AdvancePhaseRelaxation(s,h/2))return false;
            if(_model.HasRelaxingFuelBoundaries&&_compressibleNozzles is not null&&!_compressibleNozzles.Advance(s.CompressibleNozzles!,state,s.Temperature,s.X,s.Inputs,h/2))return false;
            if(_fuelDucts is not null&&!_fuelDucts.Advance(state,s.Temperature,h))return false;
            if(_model.HasRelaxingFuelBoundaries&&_compressibleNozzles is not null&&!_compressibleNozzles.Advance(s.CompressibleNozzles!,state,s.Temperature,s.X,s.Inputs,h/2,reverse:true))return false;
            if(!AdvancePhaseRelaxation(s,h/2))return false;
            remaining=h==remaining?0:remaining-h;
        }
        return true;
    }
    private bool AdvancePhaseRelaxation(State s,double seconds)
    {
        var state=s.CompressibleFuel!;
        for(int i=0;i<_model.CompressibleFuels.Length;++i)
        {
            var f=_model.CompressibleFuels[i];if(f.Relaxation is not {} law)continue;
            double mass=state.Mass[i],velocity=state.Momentum[i]/mass,energy=state.Energy[i]/mass-.5*velocity*velocity;
            if(!law.Relax(mass/_model.CompressibleVolume(state,i),energy,state.VaporMass![i]/mass,seconds,out var result))return false;
            double change=mass*result.VaporMassFractionChange;
            Numeric.Accumulate(change,ref state.VaporMass[i],ref state.VaporCorrection![i]);state.PhaseTick![i]+=change;
            Numeric.Accumulate(mass*result.SpecificEntropyChangeJoulesPerKilogramKelvin,ref state.PhaseHistory![i,2],ref state.PhaseCorrection![i,2]);
        }
        return state.Finite();
    }
}
