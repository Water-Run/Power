// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record FuelFilmDefinition
{
    public Quantity InitialMass { get; init; }
    public Quantity InitialTemperature { get; init; }
    public Quantity LiquidSpecificHeat { get; init; }
    public Quantity SaturationTemperature { get; init; }
    public Quantity LatentInternalEnergy { get; init; }
}

public readonly record struct FuelFilmSample(double LiquidMassKilograms, double ThermalEnergyJoules);
public readonly record struct FuelFilmTransfer(FuelFilmSample Liquid, double LiquidTemperatureKelvin, double WallTemperatureKelvin,
    double EvaporatedMassKilograms, double VaporEnergyJoules, double HeatFromWallJoules);

/// <summary>Negligible-volume liquid film with explicit phase-energy reference and finite-bath equilibrium evaporation.</summary>
public sealed class EquilibriumFuelFilm
{
    public double LiquidSpecificHeatJoulesPerKilogramKelvin { get; }
    public double VaporSpecificHeatJoulesPerKilogramKelvin { get; }
    public double SaturationTemperatureKelvin { get; }
    public double LatentInternalEnergyJoulesPerKilogram { get; }
    public double ConductanceWattsPerKelvin { get; }
    public double LiquidEnergyOffsetJoulesPerKilogram { get; }
    public EquilibriumFuelFilm(double liquidSpecificHeat, double vaporSpecificHeat, double saturationTemperature, double latentInternalEnergy, double conductance)
    {
        if (!GearReference.Finite(liquidSpecificHeat,vaporSpecificHeat,saturationTemperature,latentInternalEnergy) ||
            liquidSpecificHeat<=0 || vaporSpecificHeat<=0 || saturationTemperature<=0 || latentInternalEnergy<=0 || !Numeric.Finite(conductance) || conductance<0)
            throw new ArgumentException("Use positive finite heat capacities, saturation temperature and latent internal energy; conductance is nonnegative.");
        LiquidSpecificHeatJoulesPerKilogramKelvin=liquidSpecificHeat;VaporSpecificHeatJoulesPerKilogramKelvin=vaporSpecificHeat;
        SaturationTemperatureKelvin=saturationTemperature;LatentInternalEnergyJoulesPerKilogram=latentInternalEnergy;ConductanceWattsPerKelvin=conductance;
        LiquidEnergyOffsetJoulesPerKilogram=(vaporSpecificHeat-liquidSpecificHeat)*saturationTemperature-latentInternalEnergy;
        if(!Numeric.Finite(LiquidEnergyOffsetJoulesPerKilogram))throw new ArgumentException("Phase reference exceeds the supported energy scale.");
    }
    public FuelFilmSample CreateState(double massKilograms,double temperatureKelvin)
    {
        if(!Numeric.Finite(massKilograms)||massKilograms<0||!Numeric.Finite(temperatureKelvin)||temperatureKelvin<=0||temperatureKelvin>SaturationTemperatureKelvin)
            throw new ArgumentException("Use nonnegative mass and liquid temperature in (0,saturation_temperature].");
        double energy=massKilograms*(LiquidSpecificHeatJoulesPerKilogramKelvin*temperatureKelvin+LiquidEnergyOffsetJoulesPerKilogram);
        if(!Numeric.Finite(energy))throw new ArgumentException("Initial liquid energy is nonfinite.");
        return new(massKilograms,energy);
    }
    public double Temperature(FuelFilmSample state)=>state.LiquidMassKilograms==0?SaturationTemperatureKelvin:
        (state.ThermalEnergyJoules/state.LiquidMassKilograms-LiquidEnergyOffsetJoulesPerKilogram)/LiquidSpecificHeatJoulesPerKilogramKelvin;
    public bool TryAdvance(FuelFilmSample state,double wallTemperatureKelvin,double wallCapacityJoulesPerKelvin,double seconds,out FuelFilmTransfer transfer)
    {
        transfer=default;double mass=state.LiquidMassKilograms,temperature=Temperature(state),sat=SaturationTemperatureKelvin;
        if(!GearReference.Finite(mass,state.ThermalEnergyJoules,wallTemperatureKelvin,wallCapacityJoulesPerKelvin)||mass<0||wallTemperatureKelvin<=0||wallCapacityJoulesPerKelvin<=0||
            !Numeric.Finite(seconds)||seconds<0||!Numeric.Finite(temperature)||temperature<=0||temperature>sat+64*GearReference.Epsilon*sat||mass==0&&state.ThermalEnergyJoules!=0)return false;
        if(mass==0||seconds==0||ConductanceWattsPerKelvin==0){transfer=new(state,temperature,wallTemperatureKelvin,0,0,0);return true;}
        double wall=wallTemperatureKelvin,heat=0,evaporated=0,remaining=seconds,capacity=mass*LiquidSpecificHeatJoulesPerKilogramKelvin;
        if(!Numeric.Finite(capacity)||capacity<=0)return false;
        if(temperature<sat||wall<sat)
        {
            double total=capacity+wallCapacityJoulesPerKelvin,average=(capacity/total)*temperature+(wallCapacityJoulesPerKelvin/total)*wall;
            double decay=ConductanceWattsPerKelvin*(1/capacity+1/wallCapacityJoulesPerKelvin),until=seconds;
            bool reaches=wall>temperature&&average>sat;
            if(reaches)
            {
                until=Numeric.Log1p((sat-temperature)/(average-sat))/decay;
                reaches=Numeric.Finite(until)&&until<=seconds;
            }
            if(reaches)
            { heat=capacity*(sat-temperature);temperature=sat;remaining-=until; }
            else
            { heat=capacity*(wallCapacityJoulesPerKelvin/total)*(wall-temperature)*-Numeric.Expm1(-decay*seconds);temperature+=heat/capacity;remaining=0; }
            wall-=heat/wallCapacityJoulesPerKelvin;
        }
        if(remaining>0&&wall>sat)
        {
            double available=wallCapacityJoulesPerKelvin*(wall-sat)*-Numeric.Expm1(-ConductanceWattsPerKelvin*remaining/wallCapacityJoulesPerKelvin);
            double dryHeat=mass*LatentInternalEnergyJoulesPerKilogram;
            double phaseHeat=Math.Min(available,dryHeat);evaporated=phaseHeat==dryHeat?mass:phaseHeat/LatentInternalEnergyJoulesPerKilogram;
            heat+=phaseHeat;wall-=phaseHeat/wallCapacityJoulesPerKelvin;temperature=sat;
        }
        double vaporEnergy=evaporated*VaporSpecificHeatJoulesPerKilogramKelvin*sat,newMass=mass-evaporated;
        // Reconstruct from the analytic phase temperature to avoid cancellation as the film dries.
        double newEnergy=newMass==0?0:newMass*(LiquidSpecificHeatJoulesPerKilogramKelvin*temperature+LiquidEnergyOffsetJoulesPerKilogram);
        if(!GearReference.Finite(newMass,newEnergy,vaporEnergy,heat)||!Numeric.Finite(wall)||wall<=0||temperature<=0||newMass<0)return false;
        var next=new FuelFilmSample(newMass,newEnergy);
        if(newMass>0&&Math.Abs(Temperature(next)-temperature)>1e-8+128*GearReference.Epsilon*Math.Abs(temperature))return false;
        transfer=new(next,newMass==0?sat:temperature,wall,evaporated,vaporEnergy,heat);return true;
    }
}

internal sealed record CompiledFuelFilm(int Component,int Gas,int Wall,double WallCapacity,double HeatingValue,double InitialMass,FuelFilmSample Initial,EquilibriumFuelFilm Law);
internal sealed class FuelFilmState(int count)
{
    internal readonly double[] Mass=new double[count],Energy=new double[count],Vaporized=new double[count],VaporCorrection=new double[count],TickVapor=new double[count],WallHeat=new double[count],HeatCorrection=new double[count];
    internal void CopyFrom(FuelFilmState other)
    {Array.Copy(other.Mass,Mass,count);Array.Copy(other.Energy,Energy,count);Array.Copy(other.Vaporized,Vaporized,count);Array.Copy(other.VaporCorrection,VaporCorrection,count);Array.Copy(other.TickVapor,TickVapor,count);Array.Copy(other.WallHeat,WallHeat,count);Array.Copy(other.HeatCorrection,HeatCorrection,count);}
    internal void BeginTick()=>Array.Clear(TickVapor,0,count);
    internal bool EndTick(double duration){for(int i=0;i<count;++i)TickVapor[i]/=duration;return Finite();}
    internal bool Finite(){for(int i=0;i<count;++i)if(!GearReference.Finite(Mass[i],Energy[i],Vaporized[i],TickVapor[i])||!GearReference.Finite(WallHeat[i],VaporCorrection[i],HeatCorrection[i],0)||Mass[i]<0||Vaporized[i]<0||TickVapor[i]<0)return false;return true;}
    internal ulong Hash(ulong hash)
    {foreach(double value in Mass)hash=Numeric.Hash(hash,value);foreach(double value in Energy)hash=Numeric.Hash(hash,value);foreach(double value in Vaporized)hash=Numeric.Hash(hash,value);foreach(double value in VaporCorrection)hash=Numeric.Hash(hash,value);foreach(double value in TickVapor)hash=Numeric.Hash(hash,value);foreach(double value in WallHeat)hash=Numeric.Hash(hash,value);foreach(double value in HeatCorrection)hash=Numeric.Hash(hash,value);return hash;}
}

internal sealed class FuelFilmSolver(CompiledModel model)
{
    private readonly double[] _wallTemperature=new double[model.ThermalCount];
    internal readonly double[] WallHeat=new double[model.ThermalCount];
    internal void AddHeat(int wall, double capacity, double heat)
    {
        _wallTemperature[wall] += heat / capacity;
        WallHeat[wall] += heat;
    }
    internal void BeginInterval(double[] temperature){Array.Copy(temperature,_wallTemperature,temperature.Length);Array.Clear(WallHeat,0,WallHeat.Length);}
    internal bool Advance(FuelFilmState state,double[] gasMass,double[] gasEnergy,MixtureState mixture,double duration,bool reverse=false)
    {
        for(int slot=0;slot<model.FuelFilms.Length;++slot)
        {
            int i=reverse?model.FuelFilms.Length-1-slot:slot;
            var film=model.FuelFilms[i];
            if(!film.Law.TryAdvance(new(state.Mass[i],state.Energy[i]),_wallTemperature[film.Wall],film.WallCapacity,duration,out var transfer))return false;
            state.Mass[i]=transfer.Liquid.LiquidMassKilograms;state.Energy[i]=transfer.Liquid.ThermalEnergyJoules;
            _wallTemperature[film.Wall]=transfer.WallTemperatureKelvin;WallHeat[film.Wall]-=transfer.HeatFromWallJoules;
            mixture.Fuel[film.Gas]+=transfer.EvaporatedMassKilograms;
            gasMass[film.Gas]=mixture.Fuel[film.Gas]+mixture.Air[film.Gas]+mixture.Products[film.Gas];gasEnergy[film.Gas]+=transfer.VaporEnergyJoules;
            Numeric.Accumulate(transfer.EvaporatedMassKilograms,ref state.Vaporized[i],ref state.VaporCorrection[i]);state.TickVapor[i]+=transfer.EvaporatedMassKilograms;
            Numeric.Accumulate(transfer.HeatFromWallJoules,ref state.WallHeat[i],ref state.HeatCorrection[i]);
            if(!Numeric.Finite(gasMass[film.Gas])||!Numeric.Finite(gasEnergy[film.Gas]))return false;
        }
        return state.Finite();
    }
}
