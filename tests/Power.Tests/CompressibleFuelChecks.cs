// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
namespace Power.Tests;

internal static class CompressibleFuelChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("compressible fuel / independent liquid pressure cold work and ideal vapor", Limits),
        ("compressible fuel / chemical equilibrium Clapeyron and caloric derivatives", Equilibrium),
        ("compressible fuel / Gibbs relation and independent isentropic acoustic derivative", Sound),
        ("compressible fuel / mass-energy inversion bounds and zero allocations", Flash)
    ];
    internal static CompressibleFuel Law(double bulk = 1.5e9) => new(new(new(2000, 717.5, 400, 300000, 0), 287, 100000, 200, 600), 750, bulk, 20e6);
    private static CompressibleFuelPoint Point(CompressibleFuel law, double rho, double t) { Require(law.Evaluate(rho,t,out var p)); return p; }
    private static void Limits()
    {
        var law=Law(); const double p=1e6,t=300,rho0=750,k=1.5e9;
        double rho=rho0*(1+p/k), v=1/rho, v0=1/rho0;
        var liquid=Point(law,rho,t); Require(liquid.Phase==FuelVolumePhase.Liquid);
        Near(liquid.PressurePascals,p,2e-7); Near(liquid.SoundSpeedMetersPerSecond,Math.Sqrt(k/rho0),1e-12);
        double cold=k*(v-v0-v0*Math.Log(v/v0));
        Near(liquid.SpecificInternalEnergyJoulesPerKilogram,2000*t-813000+cold,2e-10);
        var vapor=Point(law,.1,500);Require(vapor.Phase==FuelVolumePhase.Vapor);
        Near(vapor.PressurePascals,.1*287*500,1e-10);Near(vapor.SpecificInternalEnergyJoulesPerKilogram,717.5*500,1e-10);
        Near(vapor.SoundSpeedMetersPerSecond,Math.Sqrt(1.4*287*500),1e-12);
    }
    private static void Equilibrium()
    {
        var law=Law();foreach(double t in new[]{300d,400,500})
        {
            Require(law.TrySaturation(t,out var sat)); double vl=sat.LiquidSpecificVolumeCubicMetersPerKilogram,vv=sat.VaporSpecificVolumeCubicMetersPerKilogram;
            var l=Point(law,1/vl,t);var v=Point(law,1/vv,t);
            Near(l.SpecificInternalEnergyJoulesPerKilogram+sat.PressurePascals*vl-t*l.SpecificEntropyJoulesPerKilogramKelvin,
                 v.SpecificInternalEnergyJoulesPerKilogram+sat.PressurePascals*vv-t*v.SpecificEntropyJoulesPerKilogramKelvin,2e-7);
            Require(law.TrySaturation(t-.001,out var before)); Require(law.TrySaturation(t+.001,out var after));
            Near((after.PressurePascals-before.PressurePascals)/.002,sat.PressureDerivativePascalsPerKelvin,2e-5);
            double rho=1/(.8*vl+.2*vv);var mid=Point(law,rho,t);Require(mid.Phase==FuelVolumePhase.LiquidVapor);
            Near(mid.VaporMassFraction,.2,1e-13);
            Near((Point(law,rho,t+.001).SpecificInternalEnergyJoulesPerKilogram-Point(law,rho,t-.001).SpecificInternalEnergyJoulesPerKilogram)/.002,mid.IsochoricHeatCapacityJoulesPerKilogramKelvin,2e-5);
        }
    }
    private static void Sound()
    {
        var law=Law();Require(law.TrySaturation(400,out var sat));
        foreach(double rho in new[]{751d,1/(.9*sat.LiquidSpecificVolumeCubicMetersPerKilogram+.1*sat.VaporSpecificVolumeCubicMetersPerKilogram),.1})
        {
            var p=Point(law,rho,400);double dv=1/rho*1e-5;
            double IsentropicPressure(double v)
            {
                double lo=390,hi=410;
                for(int i=0;i<60;++i){double t=(lo+hi)/2; if(Point(law,1/v,t).SpecificEntropyJoulesPerKilogramKelvin>p.SpecificEntropyJoulesPerKilogramKelvin)hi=t;else lo=t;}
                return Point(law,1/v,(lo+hi)/2).PressurePascals;
            }
            double derivative=(IsentropicPressure(1/rho+dv)-IsentropicPressure(1/rho-dv))/(2*dv);
            Near(-derivative/(rho*rho),p.SoundSpeedMetersPerSecond*p.SoundSpeedMetersPerSecond,3e-3);
            var left=Point(law,1/(1/rho-dv),400);var right=Point(law,1/(1/rho+dv),400);
            Near((right.SpecificInternalEnergyJoulesPerKilogram-left.SpecificInternalEnergyJoulesPerKilogram)/(2*dv),
                 400*(right.SpecificEntropyJoulesPerKilogramKelvin-left.SpecificEntropyJoulesPerKilogramKelvin)/(2*dv)-p.PressurePascals,.2);
        }
    }
    private static void Flash()
    {
        var law=Law();foreach(double rho in new[]{.1,2,30,100,750,755})foreach(double t in new[]{250d,350,450,550})
        {var p=Point(law,rho,t);Require(law.Flash(rho,p.SpecificInternalEnergyJoulesPerKilogram,out var recovered));Near(recovered.TemperatureKelvin,t,2e-9);Near(recovered.SpecificInternalEnergyJoulesPerKilogram,p.SpecificInternalEnergyJoulesPerKilogram,0);}
        Require(!law.Evaluate(0,300,out var bad)&&bad==default);Require(!law.Flash(750,1e20,out bad)&&bad==default);
        var initial=Point(law,30,400);for(int n=0;n<30;++n)Require(law.Flash(30,initial.SpecificInternalEnergyJoulesPerKilogram,out _));
        long before=GC.GetAllocatedBytesForCurrentThread();for(int n=0;n<100;++n)Require(law.Flash(30,initial.SpecificInternalEnergyJoulesPerKilogram,out _));Require(GC.GetAllocatedBytesForCurrentThread()==before);
    }
}
