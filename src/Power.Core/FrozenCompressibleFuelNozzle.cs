// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public readonly record struct FrozenCompressibleFuelNozzlePoint(double MassFlowKilogramsPerSecond,
    double ThroatPressurePascals, double ExitTemperatureKelvin, double ExitVelocityMetersPerSecond,
    double VaporMassFraction, double StagnationEnthalpyJoulesPerKilogram, double ExitInternalEnergyJoulesPerKilogram,
    double ExitPressureWorkJoulesPerKilogram, double KineticEnergyJoulesPerKilogram,
    double JetMomentumNewtons, bool Choked, double ThroatVelocityMetersPerSecond, double FrozenThroatSoundSpeedMetersPerSecond);

/// <summary>
/// Common-pressure/temperature, frozen-quality isentropic transit. Liquid cold compression
/// energy shares the reservoir EOS. Phase conversion is suppressed inside the nozzle;
/// bulk reservoirs re-equilibrate separately. This is a limiting model, not an HEM nozzle.
/// </summary>
public sealed class FrozenCompressibleFuelNozzle
{
    public CompressibleFuel Law { get; }
    public double AreaSquareMeters { get; }
    public double DischargeCoefficient { get; }
    public FrozenCompressibleFuelNozzle(CompressibleFuel law, double areaSquareMeters, double dischargeCoefficient)
    {
        Law = law ?? throw new ArgumentNullException(nameof(law));
        if (!Numeric.Finite(areaSquareMeters) || areaSquareMeters <= 0 || !Numeric.Finite(dischargeCoefficient) || dischargeCoefficient <= 0 || dischargeCoefficient > 1)
            throw new ArgumentException("Use positive finite area and effective-area coefficient in (0,1].");
        AreaSquareMeters = areaSquareMeters; DischargeCoefficient = dischargeCoefficient;
    }
    private static double LogSum(double a, double b)
    {
        if (double.IsNegativeInfinity(a)) return b; if (double.IsNegativeInfinity(b)) return a;
        return Math.Max(a,b) + Numeric.Log1p(Math.Exp(-Math.Abs(a-b)));
    }
    public FuelNozzleStatus Evaluate(double upstreamPressure, double upstreamTemperature, double vaporMassFraction,
        double receiverPressure, double opening, out FrozenCompressibleFuelNozzlePoint point)
    {
        point = default;
        if (!GearReference.Finite(upstreamPressure,upstreamTemperature,vaporMassFraction,receiverPressure) || !Numeric.Finite(opening) ||
            upstreamPressure <= 0 || upstreamPressure > Law.MaximumPressurePascals || receiverPressure <= 0 || receiverPressure > Law.MaximumPressurePascals ||
            vaporMassFraction < 0 || vaporMassFraction > 1 || opening < 0 || opening > 1) return FuelNozzleStatus.InvalidInput;
        var m=Law.Material;var c=m.Caloric;
        if (upstreamTemperature < m.MinimumTemperatureKelvin || upstreamTemperature > m.MaximumTemperatureKelvin) return FuelNozzleStatus.TemperatureOutOfRange;
        double x=vaporMassFraction,k=Law.LiquidBulkModulusPascals,rho=Law.ReferenceLiquidDensityKilogramsPerCubicMeter,r=m.GasConstantJoulesPerKilogramKelvin;
        double cp=(1-x)*c.LiquidSpecificHeatJoulesPerKilogramKelvin+x*(c.VaporSpecificHeatJoulesPerKilogramKelvin+r),alpha=x*r/cp;
        double h0=cp*upstreamTemperature+(1-x)*c.LiquidEnergyOffsetJoulesPerKilogram+(1-x)*k/rho*Numeric.Log1p(upstreamPressure/k);
        if (!Numeric.Finite(h0) || !Numeric.Finite(cp) || cp<=0 || alpha<0 || alpha>=1) return FuelNozzleStatus.InvalidInput;
        double LogVolume(double lr)
        {
            double p=upstreamPressure*Math.Exp(lr);
            double liquid=x==1?double.NegativeInfinity:Math.Log(1-x)-Math.Log(rho)-Numeric.Log1p(p/k);
            double vapor=x==0?double.NegativeInfinity:Math.Log(x*r*upstreamTemperature/upstreamPressure)+(alpha-1)*lr;
            return LogSum(liquid,vapor);
        }
        double LogNegativeDerivative(double lr)
        {
            double p=upstreamPressure*Math.Exp(lr);
            double liquid=x==1?double.NegativeInfinity:Math.Log(1-x)-Math.Log(rho)-Numeric.Log1p(p/k)-Math.Log(k+p);
            double vapor=x==0?double.NegativeInfinity:Math.Log(x*r*upstreamTemperature/upstreamPressure)+Math.Log(1-alpha)-Math.Log(upstreamPressure)+(alpha-2)*lr;
            return LogSum(liquid,vapor);
        }
        double Drop(double lr) => -cp*upstreamTemperature*Numeric.Expm1(alpha*lr) -
            (1-x)*k/rho*Numeric.Log1p(upstreamPressure/(k+upstreamPressure)*Numeric.Expm1(lr));
        double Residual(double lr)
        {double d=Drop(lr);return d==0?double.PositiveInfinity:2*LogVolume(lr)-Math.Log(2)-Math.Log(d)-LogNegativeDerivative(lr);}
        double back=receiverPressure>=upstreamPressure||opening==0?0:Math.Log(receiverPressure)-Math.Log(upstreamPressure);
        if(receiverPressure<upstreamPressure && opening>0 && (upstreamPressure-receiverPressure)/upstreamPressure<.5)
            back=Numeric.Log1p(-(upstreamPressure-receiverPressure)/upstreamPressure);
        double exitT=upstreamTemperature*Math.Exp(alpha*back);
        if(exitT<m.MinimumTemperatureKelvin||exitT>m.MaximumTemperatureKelvin)return FuelNozzleStatus.TemperatureOutOfRange;
        double throat=back;bool choked=false;
        if(receiverPressure<upstreamPressure&&opening>0&&Residual(back)<0)
        {
            // For D=h0-h, F=v^2+2D*v' has F'=2D*v''>0 for this frozen EOS.
            // Therefore the positive-pressure flux maximum is unique, including pure liquid.
            double lo=back,hi=0;
            for(int i=0;i<80;++i){double mid=(lo+hi)/2;if(Residual(mid)>0)hi=mid;else lo=mid;}
            throat=(lo+hi)/2;choked=true;
        }
        bool flowing=receiverPressure<upstreamPressure&&opening>0;
        double kinetic=flowing?Drop(back):0,throatK=flowing?Drop(throat):0;
        double speed=Math.Sqrt(2*kinetic),throatSpeed=Math.Sqrt(2*throatK),pressure=flowing?(choked?upstreamPressure*Math.Exp(throat):receiverPressure):upstreamPressure;
        double rate=flowing?opening*DischargeCoefficient*AreaSquareMeters*Math.Exp(.5*Math.Log(2*throatK)-LogVolume(throat)):0;
        double sound=Math.Exp(LogVolume(throat)-.5*LogNegativeDerivative(throat));
        if(!flowing){exitT=upstreamTemperature;back=throat=0;}
        double exitP=flowing?receiverPressure:upstreamPressure,y=exitP/k;
        double cold=k/rho*(Numeric.Log1p(y)-y/(1+y));
        if(Math.Abs(y)<1e-3){cold=0;double power=y*y;for(int n=2;n<=9;++n){cold+=(n%2==0?1:-1)*(n-1d)/n*power;power*=y;}cold*=k/rho;}
        double internalEnergy=((1-x)*c.LiquidSpecificHeatJoulesPerKilogramKelvin+x*c.VaporSpecificHeatJoulesPerKilogramKelvin)*exitT+(1-x)*(c.LiquidEnergyOffsetJoulesPerKilogram+cold);
        double pv=(1-x)*exitP/rho/(1+y)+x*r*exitT,momentum=rate*speed;
        if(!GearReference.Finite(rate,kinetic,speed,pressure)||!GearReference.Finite(internalEnergy,pv,momentum,sound)||rate<0||kinetic<0||sound<=0)return FuelNozzleStatus.NoConvergence;
        point=new(rate,pressure,exitT,speed,x,h0,internalEnergy,pv,kinetic,momentum,choked,throatSpeed,sound);return FuelNozzleStatus.Success;
    }
}
