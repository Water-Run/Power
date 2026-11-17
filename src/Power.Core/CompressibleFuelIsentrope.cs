// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
namespace Power.Core;

public sealed partial class CompressibleFuel
{
    public bool TrySaturationAtPressure(double pressurePascals,out double temperatureKelvin,out CompressibleFuelSaturation saturation)
    {
        temperatureKelvin=0;saturation=default;
        if(!Numeric.Finite(pressurePascals)||pressurePascals<=0||pressurePascals>MaximumPressurePascals)return false;
        double p=pressurePascals,r=Material.GasConstantJoulesPerKilogramKelvin,compression=CompressionEnthalpy(p);
        double Residual(double t)
        {if(!Material.TrySaturationPressure(t,out double negligible))return double.NaN;return r*t*(Math.Log(p)-Math.Log(negligible))-compression;}
        double lo=Material.MinimumTemperatureKelvin,hi=Material.MaximumTemperatureKelvin;
        double fLo=Residual(lo),fHi=Residual(hi),rounding=128*GearReference.Epsilon*Math.Max(r*hi,Math.Abs(compression));
        if(fLo < -rounding||fHi > rounding)return false;
        double t=Math.Max(lo,Math.Min(hi,Material.Caloric.SaturationTemperatureKelvin));bool converged=false;
        if(Math.Abs(fLo)<=rounding){t=lo;converged=true;}else if(Math.Abs(fHi)<=rounding){t=hi;converged=true;}
        for(int i=0;i<80&&!converged;++i)
        {
            double f=Residual(t),derivative=(f+compression-Material.LatentEnthalpy(t))/t;
            double correction=f/derivative;
            if(!Numeric.Finite(f)||!Numeric.Finite(correction))return false;
            if(Math.Abs(correction)<=8*GearReference.Epsilon*Math.Max(1,t)){converged=true;break;}
            if(f>0)lo=t;else hi=t;double next=t-correction;t=next>lo&&next<hi?next:(lo+hi)/2;
        }
        if(!converged)return false;
        double vl=LiquidVolume(p),vv=r*t/p,latent=Material.LatentEnthalpy(t)-compression,slope=latent/(t*(vv-vl));
        if(!GearReference.Finite(vl,vv,latent,slope)||vv<=vl||latent<=0||slope<=0)return false;
        temperatureKelvin=t;saturation=new(p,vl,vv,latent,slope);return true;
    }
    /// <summary>Stable equilibrium at prescribed absolute pressure and specific entropy; no metastable phase is retained.</summary>
    public bool TryPressureEntropy(double pressurePascals,double specificEntropyJoulesPerKilogramKelvin,out CompressibleFuelPoint point)
    {
        point=default;
        if(!Numeric.Finite(pressurePascals)||pressurePascals<=0||pressurePascals>MaximumPressurePascals||!Numeric.Finite(specificEntropyJoulesPerKilogramKelvin))return false;
        double p=pressurePascals,s=specificEntropyJoulesPerKilogramKelvin,r=Material.GasConstantJoulesPerKilogramKelvin;var c=Material.Caloric;
        double cl=c.LiquidSpecificHeatJoulesPerKilogramKelvin,cv=c.VaporSpecificHeatJoulesPerKilogramKelvin,tr=c.SaturationTemperatureKelvin;
        double tl=IsentropeTemperature(tr*Math.Exp(s/cl));
        if(tl>=Material.MinimumTemperatureKelvin&&tl<=Material.MaximumTemperatureKelvin&&TrySaturation(tl,out var liquidSat)&&p>=liquidSat.PressurePascals)
        {double u=cl*tl+c.LiquidEnergyOffsetJoulesPerKilogram+ColdEnergy(p);return FinishIsentrope(new(1/LiquidVolume(p),tl,p,u,cl*Math.Log(tl/tr),cl,Math.Sqrt(LiquidBulkModulusPascals/ReferenceLiquidDensityKilogramsPerCubicMeter),0,FuelVolumePhase.Liquid),s,out point);}
        double tv=IsentropeTemperature(tr*Math.Exp((s+r*Math.Log(p/Material.ReferencePressurePascals)-c.LatentInternalEnergyJoulesPerKilogram/tr-r)/(cv+r)));
        if(tv>=Material.MinimumTemperatureKelvin&&tv<=Material.MaximumTemperatureKelvin&&TrySaturation(tv,out var vaporSat)&&p<=vaporSat.PressurePascals)
        {double entropy=(cv+r)*Math.Log(tv/tr)-r*Math.Log(p/Material.ReferencePressurePascals)+c.LatentInternalEnergyJoulesPerKilogram/tr+r;return FinishIsentrope(new(p/(r*tv),tv,p,cv*tv,entropy,cv,Math.Sqrt((1+r/cv)*r*tv),1,FuelVolumePhase.Vapor),s,out point);}
        if(!TrySaturationAtPressure(p,out double t,out var sat))return false;
        double sl=cl*Math.Log(t/tr),sv=(cv+r)*Math.Log(t/tr)-r*Math.Log(p/Material.ReferencePressurePascals)+c.LatentInternalEnergyJoulesPerKilogram/tr+r;
        double x=(s-sl)/(sv-sl);if(!Numeric.Finite(x)||x<0||x>1)return false;
        double vl=sat.LiquidSpecificVolumeCubicMetersPerKilogram,vv=sat.VaporSpecificVolumeCubicMetersPerKilogram,v=(1-x)*vl+x*vv;
        double ul=cl*t+c.LiquidEnergyOffsetJoulesPerKilogram+ColdEnergy(p),uv=cv*t;
        double vlT=-vl*sat.PressureDerivativePascalsPerKelvin/(LiquidBulkModulusPascals+p),vvT=vv/t-vv/p*sat.PressureDerivativePascalsPerKelvin;
        double xT=-((1-x)*vlT+x*vvT)/(vv-vl),capacity=(1-x)*(cl-p*vlT)+x*cv+xT*(uv-ul);
        double sound=Math.Sqrt(v*v*t*sat.PressureDerivativePascalsPerKelvin*sat.PressureDerivativePascalsPerKelvin/capacity);
        var result=new CompressibleFuelPoint(1/v,t,p,(1-x)*ul+x*uv,(1-x)*sl+x*sv,capacity,sound,x,FuelVolumePhase.LiquidVapor);
        return FinishIsentrope(result,s,out point);
    }
    private static bool FinishIsentrope(CompressibleFuelPoint candidate,double target,out CompressibleFuelPoint point)
    {point=default;if(!FiniteIsentrope(candidate)||Math.Abs(candidate.SpecificEntropyJoulesPerKilogramKelvin-target)>256*GearReference.Epsilon*(Math.Abs(target)+candidate.IsochoricHeatCapacityJoulesPerKilogramKelvin+1))return false;point=candidate;return true;}
    private static bool FiniteIsentrope(CompressibleFuelPoint p)=>GearReference.Finite(p.DensityKilogramsPerCubicMeter,p.TemperatureKelvin,p.PressurePascals,p.SpecificInternalEnergyJoulesPerKilogram)&&
        GearReference.Finite(p.SpecificEntropyJoulesPerKilogramKelvin,p.IsochoricHeatCapacityJoulesPerKilogramKelvin,p.SoundSpeedMetersPerSecond,p.VaporMassFraction)&&p.DensityKilogramsPerCubicMeter>0&&p.IsochoricHeatCapacityJoulesPerKilogramKelvin>0&&p.SoundSpeedMetersPerSecond>0;
    private double IsentropeTemperature(double value)
    {
        // Inverting a logarithm at an inclusive material endpoint can move by a
        // few ulps. Only that roundoff neighbourhood is identified with the endpoint.
        double lo=Material.MinimumTemperatureKelvin,hi=Material.MaximumTemperatureKelvin;
        if(Math.Abs(value-lo)<=16*GearReference.Epsilon*lo)return lo;
        if(Math.Abs(value-hi)<=16*GearReference.Epsilon*hi)return hi;
        return value;
    }
    internal double IsentropicEnthalpyDrop(CompressibleFuelPoint source,CompressibleFuelPoint destination)
    {
        double p=destination.PressurePascals,p0=source.PressurePascals;var c=Material.Caloric;
        double dp=p0-p,compression=LiquidBulkModulusPascals/ReferenceLiquidDensityKilogramsPerCubicMeter*Numeric.Log1p(dp/(LiquidBulkModulusPascals+p));
        if(source.Phase==FuelVolumePhase.Liquid&&destination.Phase==FuelVolumePhase.Liquid)return compression;
        if(source.Phase==FuelVolumePhase.Vapor&&destination.Phase==FuelVolumePhase.Vapor)
            return -(c.VaporSpecificHeatJoulesPerKilogramKelvin+Material.GasConstantJoulesPerKilogramKelvin)*source.TemperatureKelvin*
                Numeric.Expm1(Material.GasConstantJoulesPerKilogramKelvin/(c.VaporSpecificHeatJoulesPerKilogramKelvin+Material.GasConstantJoulesPerKilogramKelvin)*Numeric.Log1p(-dp/p0));
        double x0=source.VaporMassFraction,dx=destination.VaporMassFraction-x0,capacity=(1-x0)*c.LiquidSpecificHeatJoulesPerKilogramKelvin+x0*(c.VaporSpecificHeatJoulesPerKilogramKelvin+Material.GasConstantJoulesPerKilogramKelvin);
        return capacity*(source.TemperatureKelvin-destination.TemperatureKelvin)-dx*(Material.LatentEnthalpy(destination.TemperatureKelvin)-CompressionEnthalpy(p))+(1-x0)*compression;
    }
}
