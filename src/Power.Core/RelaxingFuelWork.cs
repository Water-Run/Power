// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
namespace Power.Core;

public sealed partial class RelaxingFuel
{
    private static double WorkExpm1(double value)
    {
        if(Math.Abs(value)>.5)return Math.Exp(value)-1;
        double term=value,sum=value;for(int n=2;n<=20;++n){term*=value/n;sum+=term;}return sum;
    }
    /// <summary>Reversible closed volume work at fixed vapor mass quality. Chemical relaxation is a separate source.</summary>
    public FuelVolumeStatus AdiabaticVolume(double massKilograms,double internalEnergyJoules,
        double oldVolumeCubicMeters,double newVolumeCubicMeters,double vaporMassFraction,out CompressibleFuelWorkTransfer transfer)
    {
        transfer=default;
        if(!GearReference.Finite(massKilograms,internalEnergyJoules,oldVolumeCubicMeters,newVolumeCubicMeters)||massKilograms<=0||oldVolumeCubicMeters<=0||newVolumeCubicMeters<=0||!Numeric.Finite(vaporMassFraction)||vaporMassFraction<0||vaporMassFraction>1)return FuelVolumeStatus.InvalidInput;
        if(!Flash(massKilograms/oldVolumeCubicMeters,internalEnergyJoules/massKilograms,vaporMassFraction,out var start))return FuelVolumeStatus.EnergyOutOfRange;
        var initial=start.Fluid;double delta=newVolumeCubicMeters-oldVolumeCubicMeters;
        if(delta==0){transfer=new(initial,0,initial.PressurePascals,0);return FuelVolumeStatus.Success;}
        var law=Equilibrium;var material=law.Material;var c=material.Caloric;
        double x=vaporMassFraction,k=law.LiquidBulkModulusPascals,rho0=law.ReferenceLiquidDensityKilogramsPerCubicMeter,r=material.GasConstantJoulesPerKilogramKelvin;
        double ratio=delta/oldVolumeCubicMeters,work;
        if(x==0)
        {
            double remainder=ratio-Numeric.Log1p(ratio);
            if(Math.Abs(ratio)<1e-3){remainder=0;double power=ratio*ratio;for(int n=2;n<=10;++n){remainder+=(n%2==0?1:-1)*power/n;power*=ratio;}}
            work=-initial.PressurePascals*delta+massKilograms*k/rho0*remainder;
        }
        else if(x==1)
            work=massKilograms*c.VaporSpecificHeatJoulesPerKilogramKelvin*initial.TemperatureKelvin*WorkExpm1(-r/c.VaporSpecificHeatJoulesPerKilogramKelvin*Numeric.Log1p(ratio));
        else
        {
            double cp=(1-x)*c.LiquidSpecificHeatJoulesPerKilogramKelvin+x*(c.VaporSpecificHeatJoulesPerKilogramKelvin+r),alpha=x*r/cp;
            if(alpha<=0)return FuelVolumeStatus.NoConvergence;
            double p0=initial.PressurePascals,t0=initial.TemperatureKelvin,logP0=Math.Log(p0),target=delta/massKilograms;
            double liquid=(1-x)/rho0/(1+p0/k),vapor=x*r*t0/p0;
            double lo=Math.Max(Math.Log(double.Epsilon)-logP0,Math.Log(material.MinimumTemperatureKelvin/t0)/alpha);
            double hi=Math.Min(Math.Log(law.MaximumPressurePascals)-logP0,Math.Log(material.MaximumTemperatureKelvin/t0)/alpha);
            double VolumeChange(double logRatio)
            {
                double p=p0*Math.Exp(logRatio);
                return -liquid*p0*WorkExpm1(logRatio)/(k+p)+vapor*WorkExpm1((alpha-1)*logRatio);
            }
            if(lo>=hi||VolumeChange(lo)<target||VolumeChange(hi)>target)return FuelVolumeStatus.EntropyOutOfRange;
            double slope0=-liquid*p0/(k+p0)+(alpha-1)*vapor;
            double lr=Math.Max(lo,Math.Min(hi,target/slope0));bool converged=false;
            for(int iteration=0;iteration<96;++iteration)
            {
                double error=VolumeChange(lr)-target;
                if(Math.Abs(error)<=32*GearReference.Epsilon*Math.Abs(target)){converged=true;break;}
                if(error>0)lo=lr;else hi=lr;
                double p=p0*Math.Exp(lr),slope=-(1-x)/rho0/(1+p/k)*p/(k+p)+(alpha-1)*vapor*Math.Exp((alpha-1)*lr);
                double next=lr-error/slope;lr=next>lo&&next<hi?next:(lo+hi)/2;
            }
            if(!converged)return FuelVolumeStatus.NoConvergence;
            double nextP=p0*Math.Exp(lr),dp=p0*WorkExpm1(lr),q=dp/(k+p0),remainder=Numeric.Log1p(q)-q;
            if(Math.Abs(q)<1e-3){remainder=0;double power=q*q;for(int n=2;n<=10;++n){remainder+=(n%2==0?-1:1)*power/n;power*=q;}}
            double coldChange=k/rho0*(remainder+q*nextP/(k+nextP));
            double cv=(1-x)*c.LiquidSpecificHeatJoulesPerKilogramKelvin+x*c.VaporSpecificHeatJoulesPerKilogramKelvin;
            work=massKilograms*(cv*t0*WorkExpm1(alpha*lr)+(1-x)*coldChange);
        }
        double mean=-work/delta;
        if(!Numeric.Finite(work)||!Numeric.Finite(mean)||mean<=0)return FuelVolumeStatus.NoConvergence;
        if(!Flash(massKilograms/newVolumeCubicMeters,(internalEnergyJoules+work)/massKilograms,x,out var end))return FuelVolumeStatus.EnergyOutOfRange;
        double residual=massKilograms*(end.Fluid.SpecificEntropyJoulesPerKilogramKelvin-initial.SpecificEntropyJoulesPerKilogramKelvin);
        double tolerance=512*GearReference.Epsilon*massKilograms*(Math.Abs(initial.SpecificEntropyJoulesPerKilogramKelvin)+initial.IsochoricHeatCapacityJoulesPerKilogramKelvin+end.Fluid.IsochoricHeatCapacityJoulesPerKilogramKelvin);
        if(Math.Abs(residual)>tolerance)return FuelVolumeStatus.NoConvergence;
        transfer=new(end.Fluid,work,mean,residual);return FuelVolumeStatus.Success;
    }
}
