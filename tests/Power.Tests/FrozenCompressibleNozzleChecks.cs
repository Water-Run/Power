// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using Power.Core;
using static Power.Tests.CoreChecks;
namespace Power.Tests;
internal static class FrozenCompressibleNozzleChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("frozen compressible nozzle / exact liquid work and analytic gas choking",Limits),
        ("frozen compressible nozzle / independent pressure integral entropy and frozen sonic derivative",Thermodynamics),
        ("frozen compressible nozzle / global maximum dense pressure scan and incompressible limit",Maximum),
        ("frozen compressible nozzle / bounds closed and reverse states zero allocations",Contracts)];
    private static FrozenCompressibleFuelNozzlePoint Point(FrozenCompressibleFuelNozzle law,double p,double t,double x,double back,double opening=1)
    {Require(law.Evaluate(p,t,x,back,opening,out var result)==FuelNozzleStatus.Success);return result;}
    private static void Limits()
    {
        var law=new FrozenCompressibleFuelNozzle(CompressibleFuelChecks.Law(),1e-8,.7);var liquid=Point(law,1e6,300,0,2e5);
        double drop=1.5e9/750*Math.Log((1.5e9+1e6)/(1.5e9+2e5)),rho=750*(1+2e5/1.5e9);
        Near(liquid.MassFlowKilogramsPerSecond,.7e-8*rho*Math.Sqrt(2*drop),1e-15);Require(!liquid.Choked);Near(liquid.ExitTemperatureKelvin,300,0);
        var gas=Point(law,2e5,500,1,50000);double critical=2e5*Math.Pow(2/2.4,1.4/.4);
        Near(gas.ThroatPressurePascals,critical,1e-7);Require(gas.Choked);Near(gas.ThroatVelocityMetersPerSecond,gas.FrozenThroatSoundSpeedMetersPerSecond,1e-9);
        Near(gas.MassFlowKilogramsPerSecond,.7e-8*2e5*Math.Sqrt(1.4/(287*500))*Math.Pow(2/2.4,2.4/.8),1e-16);
        var weak=new FrozenCompressibleFuelNozzle(CompressibleFuelChecks.Law(1e5),1e-8,.7);var soft=Point(weak,1e6,300,0,5e4);
        Near(soft.ThroatPressurePascals,1.1e6*Math.Exp(-.5)-1e5,1e-7);Require(soft.Choked);Near(soft.ThroatVelocityMetersPerSecond,Math.Sqrt(1e5/750),1e-10);
    }
    private static double Volume(double pressure,double t0,double p0,double x,double bulk)
    {double cp=(1-x)*2000+x*1004.5,temperature=t0*Math.Pow(pressure/p0,x*287/cp);return (1-x)/(750*(1+pressure/bulk))+x*287*temperature/pressure;}
    private static double Integral(double lo,double hi,double t,double p,double x,double bulk)
    {const int count=4096;double h=(hi-lo)/count,sum=0;for(int i=0;i<=count;++i)sum+=(i==0||i==count?1:i%2==0?2:4)*Volume(lo+i*h,t,p,x,bulk);return sum*h/3;}
    private static void Thermodynamics()
    {
        var law=new FrozenCompressibleFuelNozzle(CompressibleFuelChecks.Law(),1e-8,.7);
        foreach(double x in new[]{0d,.01,.2,.9,1})
        {
            var q=Point(law,5e5,500,x,1e5);double expected=Integral(1e5,5e5,500,5e5,x,1.5e9);
            Near(q.KineticEnergyJoulesPerKilogram,expected,Math.Max(1e-7,expected*1e-9));
            Near(q.ExitInternalEnergyJoulesPerKilogram+q.ExitPressureWorkJoulesPerKilogram+q.KineticEnergyJoulesPerKilogram,q.StagnationEnthalpyJoulesPerKilogram,3e-9);
            double cp=(1-x)*2000+x*1004.5;Near(cp*Math.Log(q.ExitTemperatureKelvin/500)-x*287*Math.Log(1e5/5e5),0,1e-10);
            if(q.Choked)
            {
                double p=q.ThroatPressurePascals,h=p*1e-5,v=Volume(p,500,5e5,x,1.5e9),derivative=(Volume(p+h,500,5e5,x,1.5e9)-Volume(p-h,500,5e5,x,1.5e9))/(2*h);
                Near(q.FrozenThroatSoundSpeedMetersPerSecond,Math.Sqrt(-v*v/derivative),1e-6);
            }
        }
    }
    private static void Maximum()
    {
        foreach(double x in new[]{.001,.1,.5,1})
        {
            var law=new FrozenCompressibleFuelNozzle(CompressibleFuelChecks.Law(),1e-8,.7);var q=Point(law,4e5,500,x,8e4);double best=0;
            for(int i=0;i<=2000;++i){double p=8e4+(4e5-8e4)*i/2000,cp=(1-x)*2000+x*1004.5,t=500*Math.Pow(p/4e5,x*287/cp);
                double drop=cp*(500-t)+(1-x)*1.5e9/750*Math.Log((1.5e9+4e5)/(1.5e9+p));best=Math.Max(best,.7e-8*Math.Sqrt(2*Math.Max(0,drop))/Volume(p,500,4e5,x,1.5e9));}
            Near(q.MassFlowKilogramsPerSecond,best,q.MassFlowKilogramsPerSecond*2e-6);
        }
        var rigid=new FrozenCompressibleFuelNozzle(CompressibleFuelChecks.Law(1e15),1e-8,.7);var incompressible=Point(rigid,5e5,350,0,1e5);
        Near(incompressible.MassFlowKilogramsPerSecond,.7e-8*Math.Sqrt(2*750*4e5),1e-12);
    }
    private static void Contracts()
    {
        var law=new FrozenCompressibleFuelNozzle(CompressibleFuelChecks.Law(),1e-8,.7);
        Near(Point(law,1e5,300,1,2e5).MassFlowKilogramsPerSecond,0,0);Near(Point(law,1e5,300,1,1,0).MassFlowKilogramsPerSecond,0,0);
        Require(law.Evaluate(1e5,300,1,1,1,out _)==FuelNozzleStatus.TemperatureOutOfRange);
        Require(law.Evaluate(1e5,300,1.1,1e4,1,out var bad)==FuelNozzleStatus.InvalidInput&&bad==default);
        for(int i=0;i<30;++i)Point(law,4e5,500,.1,1e5);long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;++i)Point(law,4e5,500,.1,1e5);Require(GC.GetAllocatedBytesForCurrentThread()==allocated);
    }
}
