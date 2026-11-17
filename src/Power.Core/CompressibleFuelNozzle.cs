// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record CompressibleFuelNozzleDefinition
{
    public Quantity Area { get; init; }
    public double DischargeCoefficient { get; init; } = 1;
    public FuelDischargePhaseModel PhaseModel { get; init; }
    public Quantity WallKineticFraction { get; init; }
    public InjectorNeedleDefinition? Needle { get; init; }
}
internal readonly record struct NozzleTransfer(double MassFlowKilogramsPerSecond,double ThroatPressurePascals,double ExitTemperatureKelvin,double ExitVelocityMetersPerSecond,double VaporMassFraction,double StagnationEnthalpyJoulesPerKilogram,double KineticEnergyJoulesPerKilogram,bool Choked);
internal sealed record CompiledCompressibleNozzle(int Component,int A,int B,int Wall,double WallCapacity,
    FrozenCompressibleFuelNozzle Law,double WallFraction,CompiledNeedle? Needle)
{
    internal FuelDischargePhaseModel Mode {get;init;}=FuelDischargePhaseModel.Frozen;
    internal EquilibriumFuelNozzle? EquilibriumLaw {get;init;}
    internal FuelNozzleStatus Evaluate(CompressibleFuelPoint source,double back,double opening,out NozzleTransfer transfer,out double uncertainty)
    {
        transfer=default;uncertainty=0;
        if(EquilibriumLaw is {} equilibrium)
        {
            var status=equilibrium.Evaluate(source.DensityKilogramsPerCubicMeter,source.TemperatureKelvin,back,opening,out var point);
            if(status!=FuelNozzleStatus.Success)return status;
            transfer=new(point.MassFlowKilogramsPerSecond,point.ThroatPressurePascals,point.ExitTemperatureKelvin,point.ExitVelocityMetersPerSecond,point.VaporMassFraction,point.StagnationEnthalpyJoulesPerKilogram,point.KineticEnergyJoulesPerKilogram,point.Choked);uncertainty=point.FluxRelativeUncertainty;return status;
        }
        else
        {
            var status=Law.Evaluate(source.PressurePascals,source.TemperatureKelvin,source.VaporMassFraction,back,opening,out var point);
            if(status!=FuelNozzleStatus.Success)return status;
            transfer=new(point.MassFlowKilogramsPerSecond,point.ThroatPressurePascals,point.ExitTemperatureKelvin,point.ExitVelocityMetersPerSecond,point.VaporMassFraction,point.StagnationEnthalpyJoulesPerKilogram,point.KineticEnergyJoulesPerKilogram,point.Choked);return status;
        }
    }
}
internal sealed class CompressibleNozzleState(int count,bool equilibrium=false)
{
    internal readonly double[]? Uncertainty=equilibrium?new double[count]:null;
    // Histories: mass, stagnation enthalpy, jet kinetic energy, wall heat, jet impulse, total pair entropy change.
    internal readonly double[,] History=new double[count,6],Correction=new double[count,6],Last=new double[count,7];
    internal void CopyFrom(CompressibleNozzleState other)
    {if(Uncertainty is not null)Array.Copy(other.Uncertainty!,Uncertainty,count);Array.Copy(other.History,History,History.Length);Array.Copy(other.Correction,Correction,Correction.Length);Array.Copy(other.Last,Last,Last.Length);}
    internal void BeginTick(){for(int i=0;i<count;++i)Last[i,0]=Last[i,1]=0;}
    internal bool EndTick(double seconds){for(int i=0;i<count;++i){Last[i,0]/=seconds;Last[i,1]/=seconds;}return Finite();}
    internal void Add(int i,int j,double value)=>Numeric.Accumulate(value,ref History[i,j],ref Correction[i,j]);
    internal bool Finite(){if(Uncertainty is not null)foreach(double x in Uncertainty)if(!Numeric.Finite(x)||x<0||x>EquilibriumFuelNozzle.MaximumFluxRelativeUncertainty)return false;foreach(double x in History)if(!Numeric.Finite(x))return false;foreach(double x in Correction)if(!Numeric.Finite(x))return false;foreach(double x in Last)if(!Numeric.Finite(x))return false;return true;}
    internal ulong Hash(ulong hash)
    {if(Uncertainty is not null)foreach(double x in Uncertainty)hash=Numeric.Hash(hash,x);for(int i=0;i<count;++i){for(int j=0;j<6;++j){hash=Numeric.Hash(hash,History[i,j]);hash=Numeric.Hash(hash,Correction[i,j]);}for(int j=0;j<7;++j)hash=Numeric.Hash(hash,Last[i,j]);}return hash;}
}
public sealed partial class CompiledModel
{
    internal CompiledCompressibleNozzle[] CompressibleNozzles {get;private set;}=[];
    internal int[] CompressibleNozzleByComponent {get;private set;}=[];
    internal int EquilibriumNozzleCount {get;private set;}
    public bool HasEquilibriumFuelNozzles=>EquilibriumNozzleCount!=0;
    public bool HasCompressibleFuelNozzles=>CompressibleNozzles.Length!=0;
    private void CompileCompressibleNozzles(ComponentDefinition[] definitions)
    {
        CompressibleNozzleByComponent=new int[Components.Length];Array.Fill(CompressibleNozzleByComponent,-1);var result=new List<CompiledCompressibleNozzle>();
        for(int i=0;i<Components.Length;++i)
        {
            var c=Components[i];if(c.Kind!=ComponentKind.CompressibleFuelNozzle)continue;var d=definitions[i].CompressibleNozzle!;
            int a=Nodes[c.A].Index,b=Nodes[c.B].Index;
            Require(CompressibleCellOwner[a]<0&&CompressibleCellOwner[b]<0,DiagnosticCode.Connection,c.Id,"compressible_nozzle.ports","Use mixed finite reservoirs, not internal duct cells.");
            Require(CompressibleCompatible(CompressibleFuels[a],CompressibleFuels[b]),DiagnosticCode.Connection,c.Id,"compressible_nozzle.material","Both reservoirs require identical material, bulk modulus and heating value.");
            double area=Convert(d.Area,Unit.SquareMeter,c.Id,"compressible_nozzle.area"),fraction=Convert(d.WallKineticFraction,Unit.Fraction,c.Id,"compressible_nozzle.wall_kinetic_fraction");
            Require(area>0&&Numeric.Finite(d.DischargeCoefficient)&&d.DischargeCoefficient is >0 and <=1&&fraction is >=0 and <=1&&d.PhaseModel is FuelDischargePhaseModel.Frozen or FuelDischargePhaseModel.Equilibrium,
                DiagnosticCode.Range,c.Id,"compressible_nozzle","Declare positive area, effective-area coefficient in (0,1], wall kinetic fraction in [0,1] and frozen or equilibrium transit explicitly.");
            CompiledNeedle? needle=null;
            if(d.Needle is {} n)
            {
                int node=Array.FindIndex(Nodes,x=>x.Id==n.NeedleNode);Require(node>=0&&Nodes[node].Domain==Domain.Translational,DiagnosticCode.Connection,c.Id,"compressible_nozzle.needle_node","Use a translational needle node.");
                double closed=Convert(n.ClosedPosition,Unit.Meter,c.Id,"compressible_nozzle.closed_position"),open=Convert(n.FullOpenPosition,Unit.Meter,c.Id,"compressible_nozzle.full_open_position");
                Require(open>closed&&Numeric.Finite(open-closed),DiagnosticCode.Range,c.Id,"compressible_nozzle.needle","Full-open position must exceed closed position.");needle=new(Nodes[node].Index,closed,open);
            }
            CompressibleNozzleByComponent[i]=result.Count;result.Add(new(i,a,b,Nodes[c.Heat].Index,Nodes[c.Heat].Storage,new(CompressibleFuels[a].Law,area,d.DischargeCoefficient),fraction,needle){Mode=d.PhaseModel,EquilibriumLaw=d.PhaseModel==FuelDischargePhaseModel.Equilibrium?new(CompressibleFuels[a].Law,area,d.DischargeCoefficient):null});
        }
        CompressibleNozzles=result.ToArray();EquilibriumNozzleCount=result.Count(n=>n.Mode==FuelDischargePhaseModel.Equilibrium);
    }
}

internal sealed class CompressibleNozzleSolver(CompiledModel model)
{
    internal static double Opening(CompiledCompressibleNozzle d,double[] inputs,double[] dynamics)=>inputs[d.Component]*(d.Needle?.Opening(dynamics)??1);
    internal bool Advance(CompressibleNozzleState state,CompressibleFuelState fluid,double[] wallTemperature,double[] dynamics,double[] inputs,double seconds,bool reverse=false)
    {
        for(int order=0;order<model.CompressibleNozzles.Length;++order)
        {
            int slot=reverse?model.CompressibleNozzles.Length-1-order:order;var d=model.CompressibleNozzles[slot];double remaining=seconds;
            double opening=Opening(d,inputs,dynamics);if(opening==0)continue;
            for(int step=0;remaining>0;++step)
            {
                if(step>=4096||!EquilibriumFuelDuctSolver.Point(model,fluid,d.A,out var a)||!EquilibriumFuelDuctSolver.Point(model,fluid,d.B,out var b))return false;
                double pressureRounding=128*GearReference.Epsilon*Math.Max(d.Law.Law.LiquidBulkModulusPascals,Math.Max(a.PressurePascals,b.PressurePascals));
                if(a.PressurePascals-b.PressurePascals<=pressureRounding)break;
                if(d.Evaluate(a,b.PressurePascals,opening,out var nozzle,out double uncertainty)!=FuelNozzleStatus.Success)return false;
                if(nozzle.MassFlowKilogramsPerSecond==0)break;
                double ma=fluid.Mass[d.A],mb=fluid.Mass[d.B],ea=fluid.Energy[d.A],eb=fluid.Energy[d.B],oldWall=wallTemperature[d.Wall];
                double h=Math.Min(remaining,.1*Math.Min(ma,mb)/nozzle.MassFlowKilogramsPerSecond),mass=0,energy=0,kinetic=0,wallHeat=0,entropy=0;
                bool accepted=false;
                for(int trial=0;trial<64;++trial,h*=.5)
                {
                    mass=h*nozzle.MassFlowKilogramsPerSecond;energy=mass*nozzle.StagnationEnthalpyJoulesPerKilogram;kinetic=mass*nozzle.KineticEnergyJoulesPerKilogram;wallHeat=d.WallFraction*kinetic;
                    if(!GearReference.Finite(mass,energy,kinetic,wallHeat)||mass<=0||mass>=ma)continue;
                    if(!model.CompressibleFuels[d.A].Law.Flash((ma-mass)/model.CompressibleVolume(fluid,d.A),(ea-energy)/(ma-mass),out var na)||
                       !model.CompressibleFuels[d.B].Law.Flash((mb+mass)/model.CompressibleVolume(fluid,d.B),(eb+energy-wallHeat)/(mb+mass),out var nb))continue;
                    if(na.PressurePascals+pressureRounding<nb.PressurePascals)continue;
                    entropy=(ma-mass)*na.SpecificEntropyJoulesPerKilogramKelvin+(mb+mass)*nb.SpecificEntropyJoulesPerKilogramKelvin-
                        ma*a.SpecificEntropyJoulesPerKilogramKelvin-mb*b.SpecificEntropyJoulesPerKilogramKelvin+d.WallCapacity*Numeric.Log1p(wallHeat/d.WallCapacity/oldWall);
                    double scale=ma*(Math.Abs(a.SpecificEntropyJoulesPerKilogramKelvin)+a.IsochoricHeatCapacityJoulesPerKilogramKelvin)+mb*(Math.Abs(b.SpecificEntropyJoulesPerKilogramKelvin)+b.IsochoricHeatCapacityJoulesPerKilogramKelvin)+d.WallCapacity;
                    if(!Numeric.Finite(entropy)||entropy < -2048*GearReference.Epsilon*scale)continue;
                    accepted=true;break;
                }
                if(!accepted||remaining-h==remaining)return false;
                Numeric.Accumulate(-mass,ref fluid.Mass[d.A],ref fluid.MassCorrection[d.A]);Numeric.Accumulate(mass,ref fluid.Mass[d.B],ref fluid.MassCorrection[d.B]);
                Numeric.Accumulate(-energy,ref fluid.Energy[d.A],ref fluid.EnergyCorrection[d.A]);Numeric.Accumulate(energy-wallHeat,ref fluid.Energy[d.B],ref fluid.EnergyCorrection[d.B]);
                wallTemperature[d.Wall]+=wallHeat/d.WallCapacity;
                double impulse=mass*nozzle.ExitVelocityMetersPerSecond;
                state.Add(slot,0,mass);state.Add(slot,1,energy);state.Add(slot,2,kinetic);state.Add(slot,3,wallHeat);state.Add(slot,4,impulse);state.Add(slot,5,entropy);
                state.Last[slot,0]+=mass;state.Last[slot,1]+=impulse;state.Last[slot,2]=nozzle.ThroatPressurePascals;state.Last[slot,3]=nozzle.ExitTemperatureKelvin;
                state.Last[slot,4]=nozzle.ExitVelocityMetersPerSecond;state.Last[slot,5]=nozzle.Choked?1:0;state.Last[slot,6]=nozzle.VaporMassFraction;if(state.Uncertainty is not null)state.Uncertainty[slot]=uncertainty;
                remaining=h==remaining?0:remaining-h;
            }
        }
        return state.Finite();
    }
}
