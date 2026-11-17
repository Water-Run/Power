// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
namespace Power.Core;

public readonly record struct EquilibriumFuelNozzlePoint(double MassFlowKilogramsPerSecond,double ThroatPressurePascals,
    double ExitTemperatureKelvin,double ExitVelocityMetersPerSecond,double VaporMassFraction,double StagnationEnthalpyJoulesPerKilogram,
    double ExitInternalEnergyJoulesPerKilogram,double ExitPressureWorkJoulesPerKilogram,double KineticEnergyJoulesPerKilogram,
    double JetMomentumNewtons,bool Choked,double ThroatVelocityMetersPerSecond,double EquilibriumThroatSoundSpeedMetersPerSecond,double FluxRelativeUncertainty);

/// <summary>Isentropic homogeneous phase equilibrium. Global flux bounds include phase boundaries and multiple extrema.</summary>
public sealed class EquilibriumFuelNozzle
{
    public const double MaximumFluxRelativeUncertainty=1e-4;
    public CompressibleFuel Law {get;}
    public double AreaSquareMeters {get;}
    public double DischargeCoefficient {get;}
    private readonly record struct Interval(double Low,double High,double LowDensity,double HighDensity,double DropUpper,double Bound);
    private readonly record struct Sample(CompressibleFuelPoint State,double DropLow,double DropHigh,double FluxLow);
    public EquilibriumFuelNozzle(CompressibleFuel law,double areaSquareMeters,double dischargeCoefficient)
    {Law=law??throw new ArgumentNullException(nameof(law));if(!Numeric.Finite(areaSquareMeters)||areaSquareMeters<=0||!Numeric.Finite(dischargeCoefficient)||dischargeCoefficient<=0||dischargeCoefficient>1)throw new ArgumentException("Use positive finite area and effective-area coefficient in (0,1].");AreaSquareMeters=areaSquareMeters;DischargeCoefficient=dischargeCoefficient;}
    private bool TrySample(CompressibleFuelPoint source,double p,out Sample sample)
    {
        sample=default;if(!Law.TryPressureEntropy(p,source.SpecificEntropyJoulesPerKilogramKelvin,out var state))return false;
        double dp=source.PressurePascals-p,estimate=Law.IsentropicEnthalpyDrop(source,state),pad=256*GearReference.Epsilon;
        double scale=Math.Abs(source.SpecificInternalEnergyJoulesPerKilogram)+Math.Abs(state.SpecificInternalEnergyJoulesPerKilogram)+Law.Material.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin*source.TemperatureKelvin;
        double error=source.Phase==state.Phase&&source.Phase!=FuelVolumePhase.LiquidVapor?64*GearReference.Epsilon*Math.Abs(estimate):64*GearReference.Epsilon*scale+
            Math.Max(source.TemperatureKelvin,state.TemperatureKelvin)*Math.Abs(state.SpecificEntropyJoulesPerKilogramKelvin-source.SpecificEntropyJoulesPerKilogramKelvin);
        double low=Math.Max(dp/source.DensityKilogramsPerCubicMeter*(1-pad),estimate-error),high=Math.Min(dp/state.DensityKilogramsPerCubicMeter*(1+pad),estimate+error);
        if(!GearReference.Finite(estimate,low,high,0)||high<0||low<0||low>high)return false;
        if(high-low>MaximumFluxRelativeUncertainty*.1*Math.Max(low,1e-300))
        {
            // Monotone v(p)|s gives independent lower/upper Riemann sums when
            // endpoint caloric differences lose precision at very small pressure drop.
            for(int panels=2;panels<=256;panels*=2)
            {
                double h=dp/panels,sumLow=0,sumHigh=0,previous=1/state.DensityKilogramsPerCubicMeter;
                for(int i=1;i<=panels;++i)
                {
                    double v;if(i==panels)v=1/source.DensityKilogramsPerCubicMeter;
                    else{if(!Law.TryPressureEntropy(p+i*h,source.SpecificEntropyJoulesPerKilogramKelvin,out var q))return false;v=1/q.DensityKilogramsPerCubicMeter;}
                    sumLow+=v*h;sumHigh+=previous*h;previous=v;
                }
                low=Math.Max(low,sumLow*(1-pad));high=Math.Min(high,sumHigh*(1+pad));
                if(high-low<=MaximumFluxRelativeUncertainty*.1*Math.Max(low,1e-300))break;
            }
        }
        if(low>high||low<0)return false;
        sample=new(state,low,high,state.DensityKilogramsPerCubicMeter*Math.Sqrt(2*low)*(1-pad));return Numeric.Finite(sample.FluxLow);
    }
    private static bool Push(Span<Interval> heap,ref int count,Interval item)
    {if(count==heap.Length)return false;int i=count++;while(i>0){int parent=(i-1)/2;if(heap[parent].Bound>=item.Bound)break;heap[i]=heap[parent];i=parent;}heap[i]=item;return true;}
    private static Interval Pop(Span<Interval> heap,ref int count)
    {var result=heap[0];var last=heap[--count];int i=0;while(2*i+1<count){int child=2*i+1;if(child+1<count&&heap[child+1].Bound>heap[child].Bound)++child;if(heap[child].Bound<=last.Bound)break;heap[i]=heap[child];i=child;}if(count>0)heap[i]=last;return result;}
    public FuelNozzleStatus Evaluate(double upstreamDensity,double upstreamTemperature,double receiverPressure,double opening,out EquilibriumFuelNozzlePoint point)
    {
        point=default;
        if(!Numeric.Finite(receiverPressure)||receiverPressure<=0||receiverPressure>Law.MaximumPressurePascals||!Numeric.Finite(opening)||opening<0||opening>1||!Law.Evaluate(upstreamDensity,upstreamTemperature,out var source))return FuelNozzleStatus.InvalidInput;
        double h0=source.SpecificInternalEnergyJoulesPerKilogram+source.PressurePascals/source.DensityKilogramsPerCubicMeter;
        if(opening==0||receiverPressure>=source.PressurePascals)
        {point=new(0,source.PressurePascals,source.TemperatureKelvin,0,source.VaporMassFraction,h0,source.SpecificInternalEnergyJoulesPerKilogram,source.PressurePascals/source.DensityKilogramsPerCubicMeter,0,0,false,0,source.SoundSpeedMetersPerSecond,0);return FuelNozzleStatus.Success;}
        if(!TrySample(source,receiverPressure,out var exit))return FuelNozzleStatus.TemperatureOutOfRange;
        var best=exit;double bestP=receiverPressure;
        void Seed(double p)
        {if(p>receiverPressure&&p<source.PressurePascals&&TrySample(source,p,out var q)&&q.FluxLow>best.FluxLow){best=q;bestP=p;}}
        double gamma=1+Law.Material.GasConstantJoulesPerKilogramKelvin/Law.Material.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin;
        if(source.Phase==FuelVolumePhase.Vapor)Seed(source.PressurePascals*Math.Pow(2/(gamma+1),gamma/(gamma-1)));
        if(source.Phase==FuelVolumePhase.Liquid)
        {Seed((Law.LiquidBulkModulusPascals+source.PressurePascals)*Math.Exp(-.5)-Law.LiquidBulkModulusPascals);if(Law.TrySaturation(source.TemperatureKelvin,out var sat))Seed(sat.PressurePascals);}
        Span<Interval> heap=stackalloc Interval[512];int count=0;double padding=1+512*GearReference.Epsilon;
        Push(heap,ref count,new(receiverPressure,source.PressurePascals,exit.State.DensityKilogramsPerCubicMeter,source.DensityKilogramsPerCubicMeter,exit.DropHigh,source.DensityKilogramsPerCubicMeter*Math.Sqrt(2*exit.DropHigh)*padding));
        double upper=double.PositiveInfinity;bool converged=false;
        for(int iteration=0;iteration<8192&&count>0;++iteration)
        {
            upper=heap[0].Bound;
            if(best.FluxLow>0&&upper<=best.FluxLow*(1+.999*MaximumFluxRelativeUncertainty)){converged=true;break;}
            var interval=Pop(heap,ref count);double mid=interval.Low+(interval.High-interval.Low)/2;
            if(mid<=interval.Low||mid>=interval.High||!TrySample(source,mid,out var q))return FuelNozzleStatus.NoConvergence;
            if(q.State.DensityKilogramsPerCubicMeter>interval.HighDensity*padding||q.State.DensityKilogramsPerCubicMeter*padding<interval.LowDensity)return FuelNozzleStatus.NoConvergence;
            if(q.FluxLow>best.FluxLow){best=q;bestP=mid;}
            double left=q.State.DensityKilogramsPerCubicMeter*Math.Sqrt(2*interval.DropUpper)*padding,right=interval.HighDensity*Math.Sqrt(2*q.DropHigh)*padding;
            if(left>best.FluxLow&&!Push(heap,ref count,new(interval.Low,mid,interval.LowDensity,q.State.DensityKilogramsPerCubicMeter,interval.DropUpper,left)))return FuelNozzleStatus.NoConvergence;
            if(right>best.FluxLow&&!Push(heap,ref count,new(mid,interval.High,q.State.DensityKilogramsPerCubicMeter,interval.HighDensity,q.DropHigh,right)))return FuelNozzleStatus.NoConvergence;
        }
        if(count==0){upper=best.FluxLow;converged=true;}
        if(!converged)return FuelNozzleStatus.NoConvergence;
        double kinetic=(exit.DropLow+exit.DropHigh)/2,velocity=Math.Sqrt(2*kinetic),rate=opening*DischargeCoefficient*AreaSquareMeters*best.FluxLow,uncertainty=Math.Max(0,upper/best.FluxLow-1);
        if(!GearReference.Finite(kinetic,velocity,rate,uncertainty))return FuelNozzleStatus.NoConvergence;
        point=new(rate,bestP,exit.State.TemperatureKelvin,velocity,exit.State.VaporMassFraction,h0,exit.State.SpecificInternalEnergyJoulesPerKilogram,receiverPressure/exit.State.DensityKilogramsPerCubicMeter,kinetic,rate*velocity,
            bestP>receiverPressure,Math.Sqrt(2*best.DropLow),best.State.SoundSpeedMetersPerSecond,uncertainty);return FuelNozzleStatus.Success;
    }
}
