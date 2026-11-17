// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.
using Power.Core;
using static Power.Tests.CoreChecks;
namespace Power.Tests;
internal static class EquilibriumNozzleChecks
{
    internal static IEnumerable<(string Name,Action Run)> All=>[
        ("equilibrium nozzle closure / pressure entropy inverse and saturation inversion",Inversion),
        ("equilibrium nozzle / gas and compressible-liquid analytic flux enclosure",Analytic),
        ("equilibrium nozzle / flashing and wet critical flux independent global reference",Flashing),
        ("equilibrium nozzle / frozen versus equilibrium phase entropy energy and sonic scope",Phase),
        ("equilibrium nozzle / bounds closed reverse calls independent use zero allocation",Contracts)];
    private static EquilibriumFuelNozzlePoint Point(EquilibriumFuelNozzle n,double density,double t,double back)
    {var status=n.Evaluate(density,t,back,1,out var p);if(status!=FuelNozzleStatus.Success)throw new InvalidOperationException($"Equilibrium nozzle failed: {status}, rho={density:R}, T={t:R}, back={back:R}.");return p;}
    private static void Inversion()
    {
        var law=CompressibleFuelChecks.Law();
        foreach(double t in new[]{200d,250,350,450,550,600})foreach(double rho in new[]{.1,2,100,751})
        {Require(law.Evaluate(rho,t,out var p));Require(law.TryPressureEntropy(p.PressurePascals,p.SpecificEntropyJoulesPerKilogramKelvin,out var q),$"Inverse failed {rho:R}/{t:R}.");Near(q.DensityKilogramsPerCubicMeter/rho,1,3e-10);Near(q.TemperatureKelvin,t,2e-9);Near(q.SpecificInternalEnergyJoulesPerKilogram,p.SpecificInternalEnergyJoulesPerKilogram,3e-6);}
        foreach(double t in new[]{250d,350,450,550}){Require(law.TrySaturation(t,out var sat));Require(law.TrySaturationAtPressure(sat.PressurePascals,out double recovered,out _));Near(recovered,t,2e-9);}
    }
    private static void Enclosure(double expected,double actual,double uncertainty)
    {Require(uncertainty>=0&&uncertainty<=EquilibriumFuelNozzle.MaximumFluxRelativeUncertainty*(1+1e-9));Require(actual<=expected*(1+2e-8)&&expected<=actual*(1+uncertainty+2e-8),$"Flux {actual:R} +/- {uncertainty:R} does not enclose {expected:R}.");}
    private static void Analytic()
    {
        var law=CompressibleFuelChecks.Law();var n=new EquilibriumFuelNozzle(law,1e-8,.7);
        var gasLaw=new CompressibleFuel(new(new(2000,717.5,400,300000,0),287,1e6,200,600),750,1.5e9,20e6);
        var gas=Point(new(gasLaw,1e-8,.7),2e5/(287*500),500,1e5);double exact=.7e-8*2e5*Math.Sqrt(1.4/(287*500))*Math.Pow(2/2.4,3);Enclosure(exact,gas.MassFlowKilogramsPerSecond,gas.FluxRelativeUncertainty);
        Near(gas.ThroatPressurePascals,2e5*Math.Pow(2/2.4,3.5),1e-6);Require(gas.Choked);
        var liquid=Point(n,750*(1+1e6/1.5e9),300,5e5);double drop=1.5e9/750*Math.Log((1.5e9+1e6)/(1.5e9+5e5));exact=.7e-8*750*(1+5e5/1.5e9)*Math.Sqrt(2*drop);Enclosure(exact,liquid.MassFlowKilogramsPerSecond,liquid.FluxRelativeUncertainty);Require(!liquid.Choked);
        var soft=Point(new(CompressibleFuelChecks.Law(1e5),1e-8,.7),8250,300,5e4);double pressure=1.1e6*Math.Exp(-.5)-1e5;exact=.7e-8*750*(1+pressure/1e5)*Math.Sqrt(1e5/750);Enclosure(exact,soft.MassFlowKilogramsPerSecond,soft.FluxRelativeUncertainty);
    }
    private static (double Density,double Enthalpy,double Quality,double Temperature) Reference(double p,double entropy)
    {
        const double cl=2000,cv=717.5,r=287,k=1.5e9,rho0=750;
        double Saturation(double t)
        {double lo=1,hi=20e6,p0=1e5*Math.Exp((cv+r-cl)/r*Math.Log(t/400)-813000/r*(1/t-1d/400));for(int i=0;i<70;++i){double a=(lo+hi)/2;if(r*t*Math.Log(a/p0)-k/rho0*Math.Log(1+a/k)>0)hi=a;else lo=a;}return(lo+hi)/2;}
        double tl=400*Math.Exp(entropy/cl);if(tl>=200&&tl<=600&&p>=Saturation(tl))return(rho0*(1+p/k),cl*tl-813000+k/rho0*Math.Log(1+p/k),0,tl);
        double tv=400*Math.Exp((entropy+r*Math.Log(p/1e5)-1037)/(cv+r));if(tv>=200&&tv<=600&&p<=Saturation(tv))return(p/(r*tv),(cv+r)*tv,1,tv);
        double min=200,max=600,compression=k/rho0*Math.Log(1+p/k);
        for(int i=0;i<70;++i){double t=(min+max)/2,p0=1e5*Math.Exp((cv+r-cl)/r*Math.Log(t/400)-813000/r*(1/t-1d/400));if(r*t*Math.Log(p/p0)-compression>0)min=t;else max=t;}
        double temperature=(min+max)/2,sl=cl*Math.Log(temperature/400),sv=(cv+r)*Math.Log(temperature/400)-r*Math.Log(p/1e5)+1037,x=(entropy-sl)/(sv-sl);
        double volume=(1-x)/(rho0*(1+p/k))+x*r*temperature/p,h=((1-x)*cl+x*(cv+r))*temperature-(1-x)*813000+(1-x)*compression;
        return(1/volume,h,x,temperature);
    }
    private static void Flashing()
    {
        var law=CompressibleFuelChecks.Law();Require(law.TrySaturation(350,out var sat));var nozzle=new EquilibriumFuelNozzle(law,1e-8,.7);
        foreach(double rho in new[]{750*(1+5e5/1.5e9),1/(.9*sat.LiquidSpecificVolumeCubicMetersPerKilogram+.1*sat.VaporSpecificVolumeCubicMetersPerKilogram)})
        {
            Require(law.Evaluate(rho,350,out var source));double back=20000,h0=source.SpecificInternalEnergyJoulesPerKilogram+source.PressurePascals/rho;var p=Point(nozzle,rho,350,back);
            double Flux(double pressure){var q=Reference(pressure,source.SpecificEntropyJoulesPerKilogramKelvin);return .7e-8*q.Density*Math.Sqrt(2*Math.Max(0,h0-q.Enthalpy));}
            const int count=4000;double best=0,bestP=back,delta=(source.PressurePascals-back)/count;
            for(int i=0;i<=count;++i){double pressure=back+i*delta,value=Flux(pressure);if(value>best){best=value;bestP=pressure;}}
            double lo=Math.Max(back,bestP-delta),hi=Math.Min(source.PressurePascals,bestP+delta);
            for(int i=0;i<80;++i){double a=lo+(hi-lo)/3,b=hi-(hi-lo)/3;if(Flux(a)>Flux(b))hi=b;else lo=a;}best=Math.Max(best,Flux((lo+hi)/2));
            if(source.Phase==FuelVolumePhase.Liquid)best=Math.Max(best,Flux(sat.PressurePascals));Enclosure(best,p.MassFlowKilogramsPerSecond,p.FluxRelativeUncertainty);
            var end=Reference(back,source.SpecificEntropyJoulesPerKilogramKelvin);Near(p.VaporMassFraction,end.Quality,2e-9);Near(p.ExitTemperatureKelvin,end.Temperature,2e-8);Require(p.VaporMassFraction>source.VaporMassFraction&&p.Choked);
            Console.WriteLine($"Equilibrium nozzle reference: rho={rho:R}, rate={p.MassFlowKilogramsPerSecond:R}, throat={p.ThroatPressurePascals:R}, uncertainty={p.FluxRelativeUncertainty:R}, exit_quality={p.VaporMassFraction:R}.");
        }
    }
    private static void Phase()
    {
        var law=CompressibleFuelChecks.Law();Require(law.Evaluate(750*(1+5e5/1.5e9),350,out var source));var eq=Point(new(law,1e-8,.7),source.DensityKilogramsPerCubicMeter,350,20000);
        var frozen=new FrozenCompressibleFuelNozzle(law,1e-8,.7);Require(frozen.Evaluate(source.PressurePascals,350,0,20000,1,out var f)==FuelNozzleStatus.Success);
        Require(eq.VaporMassFraction>0&&f.VaporMassFraction==0&&eq.MassFlowKilogramsPerSecond<f.MassFlowKilogramsPerSecond);
        Near(eq.ExitInternalEnergyJoulesPerKilogram+eq.ExitPressureWorkJoulesPerKilogram+eq.KineticEnergyJoulesPerKilogram,eq.StagnationEnthalpyJoulesPerKilogram,2e-6);
        Require(law.TryPressureEntropy(20000,source.SpecificEntropyJoulesPerKilogramKelvin,out var exit));Near(exit.SpecificEntropyJoulesPerKilogramKelvin,source.SpecificEntropyJoulesPerKilogramKelvin,2e-9);
    }
    private static void Contracts()
    {
        var law=CompressibleFuelChecks.Law();var nozzle=new EquilibriumFuelNozzle(law,1e-8,.7);double rho=2e5/(287*500);
        Require(nozzle.Evaluate(rho,500,3e5,1,out var reverse)==FuelNozzleStatus.Success&&reverse.MassFlowKilogramsPerSecond==0);
        Require(nozzle.Evaluate(rho,500,1,0,out var closed)==FuelNozzleStatus.Success&&closed.MassFlowKilogramsPerSecond==0);
        Require(nozzle.Evaluate(rho,500,1e5,2,out var bad)==FuelNozzleStatus.InvalidInput&&bad==default);
        for(int i=0;i<5;++i)Point(nozzle,rho,500,1e5);long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10;++i)Point(nozzle,rho,500,1e5);Require(GC.GetAllocatedBytesForCurrentThread()==allocated);
    }
}
