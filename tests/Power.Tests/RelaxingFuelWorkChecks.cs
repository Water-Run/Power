// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using Power.Core;
using static Power.Tests.CoreChecks;
namespace Power.Tests;
internal static class RelaxingFuelWorkChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("relaxing work / independent pure liquid and ideal vapor adiabats",Limits),
        ("relaxing work / independent frozen entropy inversion and pressure quadrature",Mixed),
        ("relaxing work / cancellation-safe infinitesimal volume increments",Small),
        ("relaxing work / reversible volume cycle at conserved vapor quality",Cycle),
        ("relaxing work / bounded pressure temperature and actionable failures",Bounds),
        ("relaxing work / allocation-free reversible transfer",Allocations)];
    private static (double Mass,double Volume,double Energy) Initial(double p,double t,double x)
    {
        const double mass=1e-4;var law=RelaxingFuelChecks.Law();double density=RelaxingFuelChecks.Density(p,t,x);Require(law.Evaluate(density,t,x,out var q));return(mass,mass/density,mass*q.Fluid.SpecificInternalEnergyJoulesPerKilogram);
    }
    private static void Limits()
    {
        var law=RelaxingFuelChecks.Law();foreach(double x in new[]{0d,1})
        {
            var a=Initial(x==0?1e6:1e5,450,x);
            foreach(double ratio in x==0?new[]{-.0001,.0001}:new[]{-.1,.1})
            {
                double next=a.Volume*(1+ratio);Require(law.AdiabaticVolume(a.Mass,a.Energy,a.Volume,next,x,out var q)==FuelVolumeStatus.Success);
                double expected=x==0?1.5e9*((next-a.Volume)-a.Mass/750*Math.Log(next/a.Volume)):a.Mass*717.5*450*(Math.Pow(1+ratio,-.4)-1);
                Near(q.EnergyChangeJoules,expected,1e-12);Near(q.Fluid.VaporMassFraction,x,0);Near(q.Fluid.TemperatureKelvin,x==0?450:450*Math.Pow(1+ratio,-.4),1e-10);
            }
        }
    }
    // Direct fixed-quality entropy relation and scalar volume inversion, independent of production work and flash.
    private static double ReferencePressure(double volume,double mass,double p0,double t0,double x)
    {
        double alpha=x*287/((1-x)*2000+x*1004.5),lo=p0/4,hi=p0*4;
        for(int i=0;i<100;++i){double p=(lo+hi)/2,t=t0*Math.Pow(p/p0,alpha),v=mass*((1-x)/(750*(1+p/1.5e9))+x*287*t/p);if(v>volume)lo=p;else hi=p;}
        return(lo+hi)/2;
    }
    private static void Mixed()
    {
        var law=RelaxingFuelChecks.Law();foreach(double x in new[]{.01,.3,.9})foreach(double ratio in new[]{.8,1.2})
        {
            var a=Initial(1e5,450,x);double next=a.Volume*ratio;Require(law.AdiabaticVolume(a.Mass,a.Energy,a.Volume,next,x,out var q)==FuelVolumeStatus.Success);
            double p=ReferencePressure(next,a.Mass,1e5,450,x),alpha=x*287/((1-x)*2000+x*1004.5);Near(q.Fluid.PressurePascals,p,1e-6);Near(q.Fluid.TemperatureKelvin,450*Math.Pow(p/1e5,alpha),1e-9);
            double sum=0,h=(next-a.Volume)/2000;for(int i=0;i<=2000;++i)sum+=(i==0||i==2000?1:i%2==0?2:4)*ReferencePressure(a.Volume+i*h,a.Mass,1e5,450,x);
            Near(q.EnergyChangeJoules,-sum*h/3,2e-11);Near(q.EntropyResidualJoulesPerKelvin,0,1e-12);
        }
    }
    private static void Small()
    {
        var law=RelaxingFuelChecks.Law();var a=Initial(1e5,450,.3);
        foreach(double ratio in new[]{1e-3,1e-4,2e-4,1e-8,1e-10,1e-12,1e-14,-1e-4,-1e-8,-1e-12})
        {
            double next=a.Volume*(1+ratio);var status=law.AdiabaticVolume(a.Mass,a.Energy,a.Volume,next,.3,out var q);Require(status==FuelVolumeStatus.Success,"Tiny reversible work failed: "+status);
            Near(q.MeanPressurePascals,1e5,Math.Max(.003,2*Math.Abs(ratio)*1e5));Require(q.EnergyChangeJoules*(next-a.Volume)<0);
        }
    }
    private static void Cycle()
    {
        var law=RelaxingFuelChecks.Law();var a=Initial(1e5,450,.3);
        Require(law.AdiabaticVolume(a.Mass,a.Energy,a.Volume,a.Volume*.8,.3,out var compression)==FuelVolumeStatus.Success);
        Require(law.AdiabaticVolume(a.Mass,a.Energy+compression.EnergyChangeJoules,a.Volume*.8,a.Volume,.3,out var expansion)==FuelVolumeStatus.Success);
        Near(compression.EnergyChangeJoules+expansion.EnergyChangeJoules,0,2e-12);Near(expansion.Fluid.TemperatureKelvin,450,1e-10);Near(expansion.Fluid.VaporMassFraction,.3,0);
    }
    private static void Bounds()
    {
        var law=RelaxingFuelChecks.Law();var a=Initial(1e5,450,.3);
        Require(law.AdiabaticVolume(0,a.Energy,a.Volume,a.Volume,.3,out var bad)==FuelVolumeStatus.InvalidInput&&bad==default);
        Require(law.AdiabaticVolume(a.Mass,a.Energy,a.Volume,a.Volume,-1,out _)==FuelVolumeStatus.InvalidInput);
        Require(law.AdiabaticVolume(a.Mass,a.Energy,a.Volume,a.Volume*1e30,.3,out _)!=FuelVolumeStatus.Success);
        Require(law.AdiabaticVolume(a.Mass,a.Energy,a.Volume,a.Volume,.3,out var same)==FuelVolumeStatus.Success&&same.EnergyChangeJoules==0);
    }
    private static void Allocations()
    {
        var law=RelaxingFuelChecks.Law();var a=Initial(1e5,450,.3);
        for(int i=0;i<10;++i)Require(law.AdiabaticVolume(a.Mass,a.Energy,a.Volume,a.Volume*.9,.3,out _)==FuelVolumeStatus.Success);
        long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;++i)Require(law.AdiabaticVolume(a.Mass,a.Energy,a.Volume,a.Volume*.9,.3,out _)==FuelVolumeStatus.Success);Require(GC.GetAllocatedBytesForCurrentThread()==before);
    }
}
