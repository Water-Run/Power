// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using Power.Core;
using static Power.Tests.CoreChecks;
namespace Power.Tests;
internal static class RelaxingFuelChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("relaxing fuel / independent fixed-quality volume energy entropy and frozen acoustics",Closure),
        ("relaxing fuel / monotone fixed-quality mass-energy inversion and caloric derivative",Flash),
        ("relaxing fuel / chemical affinity is fixed-energy entropy gradient",Affinity),
        ("relaxing fuel / exact isolated quality kinetics energy and entropy convergence",Relaxation),
        ("relaxing fuel / equilibrium limit frozen bound transactions and zero allocation",Contracts)];
    internal static RelaxingFuel Law(ulong tau=1000000)=>new(CompressibleFuelChecks.Law(),tau);
    internal static double Density(double p,double t,double x)=>1/((1-x)/(750*(1+p/1.5e9))+x*287*t/p);
    private static RelaxingFuelPoint Point(RelaxingFuel law,double density,double t,double x)
    {Require(law.Evaluate(density,t,x,out var p));return p;}
    private static void Closure()
    {
        var law=Law();foreach(double x in new[]{0d,.01,.3,.9,1})
        {
            const double p=1e5,t=450;double rho=Density(p,t,x);var q=Point(law,rho,t,x);Near(q.Fluid.PressurePascals,p,1e-6);
            double y=p/1.5e9,cold=1.5e9/750*(Math.Log(1+y)-y/(1+y)),expected=(1-x)*(2000*t-813000+cold)+x*717.5*t;
            Near(q.Fluid.SpecificInternalEnergyJoulesPerKilogram,expected,2e-8);
            var nozzle=new FrozenCompressibleFuelNozzle(law.Equilibrium,1e-8,.7);Require(nozzle.Evaluate(p,t,x,p,0,out var n)==FuelNozzleStatus.Success);Near(q.Fluid.SoundSpeedMetersPerSecond,n.FrozenThroatSoundSpeedMetersPerSecond,1e-8);
            double entropy=(1-x)*2000*Math.Log(t/400)+x*(1004.5*Math.Log(t/400)-287*Math.Log(p/1e5)+1037);Near(q.Fluid.SpecificEntropyJoulesPerKilogramKelvin,entropy,1e-9);
        }
    }
    private static void Flash()
    {
        var law=Law();foreach(double x in new[]{0d,.01,.3,.9,1})foreach(double t in new[]{250d,350,450,550})
        {
            double rho=Density(1e5,t,x);var p=Point(law,rho,t,x);Require(law.Flash(rho,p.Fluid.SpecificInternalEnergyJoulesPerKilogram,x,out var q));Near(q.Fluid.TemperatureKelvin,t,2e-9);Near(q.Fluid.VaporMassFraction,x,0);
            double derivative=(Point(law,rho,t+.001,x).Fluid.SpecificInternalEnergyJoulesPerKilogram-Point(law,rho,t-.001,x).Fluid.SpecificInternalEnergyJoulesPerKilogram)/.002;Near(derivative,p.Fluid.IsochoricHeatCapacityJoulesPerKilogramKelvin,2e-4);
        }
    }
    private static void PressureBoundFlash()
    {
        var law=Law();foreach(double x in new[]{.01,.3,1}){double rho=Density(19e6,500,x);var p=Point(law,rho,500,x);Require(law.Flash(rho,p.Fluid.SpecificInternalEnergyJoulesPerKilogram,x,out var recovered));Near(recovered.Fluid.TemperatureKelvin,500,2e-9);}
    }
    private static void Affinity()
    {
        var law=Law();foreach(double x in new[]{.1,.3,.7})
        {
            double rho=Density(1e5,450,x);var p=Point(law,rho,450,x);const double dx=1e-5;double energy=p.Fluid.SpecificInternalEnergyJoulesPerKilogram;
            Require(law.Flash(rho,energy,x-dx,out var left));Require(law.Flash(rho,energy,x+dx,out var right));
            Near((right.Fluid.SpecificEntropyJoulesPerKilogramKelvin-left.Fluid.SpecificEntropyJoulesPerKilogramKelvin)/(2*dx),p.ChemicalAffinityJoulesPerKilogram/p.Fluid.TemperatureKelvin,2e-4);
        }
    }
    private static void Relaxation()
    {
        var law=Law();foreach(var initial in new[]{(Pressure:1e5,Temperature:450d,Quality:.3),(Pressure:3e5,Temperature:350d,Quality:.1)})
        {
            double rho=Density(initial.Pressure,initial.Temperature,initial.Quality);var p=Point(law,rho,initial.Temperature,initial.Quality);double energy=p.Fluid.SpecificInternalEnergyJoulesPerKilogram;
            Require(law.Equilibrium.Flash(rho,energy,out var target));Require(Math.Abs(target.VaporMassFraction-initial.Quality)>1e-3);
            Require(law.Relax(rho,energy,initial.Quality,.002,out var result));double expected=target.VaporMassFraction+(initial.Quality-target.VaporMassFraction)*Math.Exp(-2);
            Near(result.Fluid.Fluid.VaporMassFraction,expected,2e-15);Near(result.Fluid.Fluid.SpecificInternalEnergyJoulesPerKilogram,energy,0);Require(result.SpecificEntropyChangeJoulesPerKilogramKelvin>=0);
            double quality=initial.Quality,entropy=p.Fluid.SpecificEntropyJoulesPerKilogramKelvin;
            for(int i=0;i<20;++i){Require(law.Relax(rho,energy,quality,.0001,out var step));Require(step.Fluid.Fluid.SpecificEntropyJoulesPerKilogramKelvin>=entropy-1e-9);quality=step.Fluid.Fluid.VaporMassFraction;entropy=step.Fluid.Fluid.SpecificEntropyJoulesPerKilogramKelvin;}
            Near(quality,expected,2e-14);Console.WriteLine($"Relaxing fuel isolated: x={initial.Quality:R}, equilibrium={target.VaporMassFraction:R}, after_2tau={quality:R}, entropy_change={result.SpecificEntropyChangeJoulesPerKilogramKelvin:R} J/kg/K.");
        }
    }
    private static void Contracts()
    {
        PressureBoundFlash();
        var law=Law();double rho=Density(1e5,450,.3);var p=Point(law,rho,450,.3);double energy=p.Fluid.SpecificInternalEnergyJoulesPerKilogram;
        Require(law.Relax(rho,energy,.3,1,out var result));Require(law.Equilibrium.Flash(rho,energy,out var target));Near(result.Fluid.Fluid.VaporMassFraction,target.VaporMassFraction,0);Require(result.Fluid.Fluid.SoundSpeedMetersPerSecond>=target.SoundSpeedMetersPerSecond);
        Require(!law.Evaluate(0,450,.3,out var bad)&&bad==default);Require(!law.Relax(rho,energy,.3,-1,out var invalid)&&invalid==default);Throws<ArgumentException>(()=>Law(0));
        for(int i=0;i<10;++i)Require(law.Relax(rho,energy,.3,.001,out _));long allocation=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;++i)Require(law.Relax(rho,energy,.3,.001,out _));Require(GC.GetAllocatedBytesForCurrentThread()==allocation);
    }
}
