// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
namespace Power.Core;

public readonly record struct RelaxingFuelPoint(CompressibleFuelPoint Fluid,double ChemicalAffinityJoulesPerKilogram);
public readonly record struct FuelPhaseRelaxation(RelaxingFuelPoint Fluid,double VaporMassFractionChange,double SpecificEntropyChangeJoulesPerKilogramKelvin,double EquilibriumVaporMassFraction);

/// <summary>Mechanical/thermal equilibrium at prescribed phase quality, with chemical relaxation at fixed density and energy.</summary>
public sealed partial class RelaxingFuel
{
    public CompressibleFuel Equilibrium {get;}
    public ulong RelaxationTimeNanoseconds {get;}
    public RelaxingFuel(CompressibleFuel equilibrium,ulong relaxationTimeNanoseconds)
    {
        Equilibrium=equilibrium??throw new ArgumentNullException(nameof(equilibrium));
        if(relaxationTimeNanoseconds is 0 or >3_600_000_000_000)throw new ArgumentException("Declare a phase relaxation time from 1 ns through one hour.");
        RelaxationTimeNanoseconds=relaxationTimeNanoseconds;
    }
    private double Cold(double p)
    {
        double k=Equilibrium.LiquidBulkModulusPascals,rho=Equilibrium.ReferenceLiquidDensityKilogramsPerCubicMeter,y=p/k,value;
        if(Math.Abs(y)<1e-3){value=0;double power=y*y;for(int n=2;n<=9;++n){value+=(n%2==0?1:-1)*(n-1d)/n*power;power*=y;}}
        else value=Numeric.Log1p(y)-y/(1+y);
        return k/rho*value;
    }
    public bool Evaluate(double density,double temperature,double vaporMassFraction,out RelaxingFuelPoint point)
    {
        point=default;var law=Equilibrium;var material=law.Material;var c=material.Caloric;
        if(!GearReference.Finite(density,temperature,vaporMassFraction,0)||density<=0||temperature<material.MinimumTemperatureKelvin||temperature>material.MaximumTemperatureKelvin||vaporMassFraction<0||vaporMassFraction>1)return false;
        double x=vaporMassFraction,k=law.LiquidBulkModulusPascals,rho0=law.ReferenceLiquidDensityKilogramsPerCubicMeter,r=material.GasConstantJoulesPerKilogramKelvin;
        double v=1/density,p,cl=c.LiquidSpecificHeatJoulesPerKilogramKelvin,cv=c.VaporSpecificHeatJoulesPerKilogramKelvin;
        if(x==0)p=k*(density/rho0-1);
        else if(x==1)p=density*r*temperature;
        else
        {
            double gasPressure=x*r*temperature*density,occupancy=(1-x)*density/rho0,a=gasPressure/k,b=1-occupancy-a;
            if(!Numeric.Finite(a)||!Numeric.Finite(b)||gasPressure>law.MaximumPressurePascals)return false;
            double other=2*Math.Sqrt(a),scale=Math.Max(Math.Abs(b),other),root=scale*Math.Sqrt((b/scale)*(b/scale)+(other/scale)*(other/scale));
            p=b>=0?2*gasPressure/(b+root):.5*k*(root-b);
        }
        if(!Numeric.Finite(p)||p<=0||p>law.MaximumPressurePascals)return false;
        double vl=1/rho0/(1+p/k),vv=r*temperature/p,liquidDerivative=(1-x)*vl/(k+p),gasDerivative=x*r*temperature/(p*p),denominator=liquidDerivative+gasDerivative;
        double dpT=x==0?0:x*r/p/denominator,capacity=(1-x)*cl+x*cv+(1-x)*p*vl/(k+p)*dpT;
        double cp=(1-x)*cl+x*(cv+r),alpha=x*r/cp,soundSquared=v*v/(liquidDerivative+(1-alpha)*gasDerivative);
        double logT=Numeric.Log1p((temperature-c.SaturationTemperatureKelvin)/c.SaturationTemperatureKelvin);
        double sl=cl*logT,sv=(cv+r)*logT-r*Math.Log(p/material.ReferencePressurePascals)+c.LatentInternalEnergyJoulesPerKilogram/c.SaturationTemperatureKelvin+r;
        double ul=cl*temperature+c.LiquidEnergyOffsetJoulesPerKilogram+Cold(p),uv=cv*temperature;
        if(!material.TrySaturationPressure(temperature,out double negligible))return false;
        double compression=k/rho0*Numeric.Log1p(p/k),affinity=compression-r*temperature*(Math.Log(p)-Math.Log(negligible));
        double energy=(1-x)*ul+x*uv,entropy=(1-x)*sl+x*sv,sound=Math.Sqrt(soundSquared);
        if(!GearReference.Finite(energy,entropy,capacity,sound)||!Numeric.Finite(affinity)||capacity<=0||sound<=0)return false;
        point=new(new(density,temperature,p,energy,entropy,capacity,sound,x,x==0?FuelVolumePhase.Liquid:x==1?FuelVolumePhase.Vapor:FuelVolumePhase.LiquidVapor),affinity);return true;
    }
    public bool Flash(double density,double specificInternalEnergy,double vaporMassFraction,out RelaxingFuelPoint point)
    {
        point=default;if(!Numeric.Finite(specificInternalEnergy))return false;
        double lo=Equilibrium.Material.MinimumTemperatureKelvin,hi=Equilibrium.Material.MaximumTemperatureKelvin;
        if(!Numeric.Finite(density)||density<=0||!Numeric.Finite(vaporMassFraction)||vaporMassFraction<0||vaporMassFraction>1)return false;
        if(vaporMassFraction>0)
        {
            double p=Equilibrium.MaximumPressurePascals,vl=1/Equilibrium.ReferenceLiquidDensityKilogramsPerCubicMeter/(1+p/Equilibrium.LiquidBulkModulusPascals);
            double pressureTemperature=p/(vaporMassFraction*Equilibrium.Material.GasConstantJoulesPerKilogramKelvin)*(1/density-(1-vaporMassFraction)*vl);
            if(pressureTemperature<hi)hi=pressureTemperature*(1-32*GearReference.Epsilon);
        }
        if(hi<lo||!Evaluate(density,lo,vaporMassFraction,out var lower)||!Evaluate(density,hi,vaporMassFraction,out var upper))return false;
        double scale=Math.Max(Math.Abs(specificInternalEnergy),Math.Max(Math.Abs(lower.Fluid.SpecificInternalEnergyJoulesPerKilogram),Math.Abs(upper.Fluid.SpecificInternalEnergyJoulesPerKilogram))),rounding=64*GearReference.Epsilon*Math.Max(1,scale);
        if(specificInternalEnergy<lower.Fluid.SpecificInternalEnergyJoulesPerKilogram-rounding||specificInternalEnergy>upper.Fluid.SpecificInternalEnergyJoulesPerKilogram+rounding)return false;
        if(Math.Abs(specificInternalEnergy-lower.Fluid.SpecificInternalEnergyJoulesPerKilogram)<=rounding){point=lower with{Fluid=lower.Fluid with{SpecificInternalEnergyJoulesPerKilogram=specificInternalEnergy}};return true;}
        if(Math.Abs(specificInternalEnergy-upper.Fluid.SpecificInternalEnergyJoulesPerKilogram)<=rounding){point=upper with{Fluid=upper.Fluid with{SpecificInternalEnergyJoulesPerKilogram=specificInternalEnergy}};return true;}
        double t=(lo+hi)/2;
        for(int i=0;i<80;++i)
        {
            if(!Evaluate(density,t,vaporMassFraction,out var p))return false;
            double error=p.Fluid.SpecificInternalEnergyJoulesPerKilogram-specificInternalEnergy,correction=error/p.Fluid.IsochoricHeatCapacityJoulesPerKilogramKelvin;
            if(Math.Abs(correction)<=32*GearReference.Epsilon*Math.Max(1,t)){point=p with{Fluid=p.Fluid with{SpecificInternalEnergyJoulesPerKilogram=specificInternalEnergy}};return true;}
            if(error>0)hi=t;else lo=t;double next=t-correction;t=next>lo&&next<hi?next:(lo+hi)/2;
        }
        return false;
    }
    public bool Relax(double density,double specificInternalEnergy,double vaporMassFraction,double seconds,out FuelPhaseRelaxation relaxation)
    {
        relaxation=default;if(!Numeric.Finite(seconds)||seconds<0||!Flash(density,specificInternalEnergy,vaporMassFraction,out var initial)||!Equilibrium.Flash(density,specificInternalEnergy,out var target))return false;
        double direction=target.VaporMassFraction-vaporMassFraction,change=direction*-Numeric.Expm1(-seconds/(RelaxationTimeNanoseconds*1e-9));
        if(initial.ChemicalAffinityJoulesPerKilogram*direction < -1024*GearReference.Epsilon*(Math.Abs(initial.Fluid.SpecificInternalEnergyJoulesPerKilogram)+initial.Fluid.IsochoricHeatCapacityJoulesPerKilogramKelvin*initial.Fluid.TemperatureKelvin))return false;
        if(!Flash(density,specificInternalEnergy,vaporMassFraction+change,out var result))return false;
        double entropy=result.Fluid.SpecificEntropyJoulesPerKilogramKelvin-initial.Fluid.SpecificEntropyJoulesPerKilogramKelvin;
        if(entropy < -1024*GearReference.Epsilon*(Math.Abs(initial.Fluid.SpecificEntropyJoulesPerKilogramKelvin)+initial.Fluid.IsochoricHeatCapacityJoulesPerKilogramKelvin))return false;
        relaxation=new(result,change,entropy,target.VaporMassFraction);return true;
    }
}
