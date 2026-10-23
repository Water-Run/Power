// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed record EquilibriumFuelDuctDefinition
{
    public IReadOnlyList<uint> Cells { get; init; } = [];
    public Quantity Area { get; init; }
    public Quantity LinearResistance { get; init; }
    public Quantity QuadraticResistance { get; init; }
    public Quantity FluidHeatFraction { get; init; }
}
internal sealed record CompiledCompressibleFuel(int Node, CompressibleFuel Law, CompressibleFuelPoint Initial, double Velocity, double HeatingValue);
internal sealed record CompiledFuelDuct(int Component, int[] Ports, int Wall, double WallCapacity, double Area, double Linear, double Quadratic, double FluidFraction, double LineVolume);
internal sealed class CompressibleFuelState(int nodes, int ducts)
{
    internal readonly double[] Mass = new double[nodes], MassCorrection = new double[nodes], Momentum = new double[nodes], MomentumCorrection = new double[nodes], Energy = new double[nodes], EnergyCorrection = new double[nodes];
    // Histories: net mass at A/B, energy at A/B, friction/fluid/wall heat, entropy, wall impulse.
    internal readonly double[,] History = new double[ducts,9], Correction = new double[ducts,9];
    internal readonly double[] TickMass = new double[ducts], TickImpulse = new double[ducts];
    internal void CopyFrom(CompressibleFuelState other)
    {
        Array.Copy(other.Mass,Mass,nodes);Array.Copy(other.MassCorrection,MassCorrection,nodes);Array.Copy(other.Momentum,Momentum,nodes);Array.Copy(other.MomentumCorrection,MomentumCorrection,nodes);
        Array.Copy(other.Energy,Energy,nodes);Array.Copy(other.EnergyCorrection,EnergyCorrection,nodes);Array.Copy(other.History,History,History.Length);Array.Copy(other.Correction,Correction,Correction.Length);
        Array.Copy(other.TickMass,TickMass,ducts);Array.Copy(other.TickImpulse,TickImpulse,ducts);
    }
    internal void Add(int d,int field,double value)=>Numeric.Accumulate(value,ref History[d,field],ref Correction[d,field]);
    internal void BeginTick(){Array.Clear(TickMass,0,ducts);Array.Clear(TickImpulse,0,ducts);}
    internal bool EndTick(double seconds){for(int i=0;i<ducts;++i){TickMass[i]/=seconds;TickImpulse[i]/=seconds;}return Finite();}
    internal bool Finite()
    {
        for(int i=0;i<nodes;++i)if(Mass[i]<=0||!GearReference.Finite(Mass[i],MassCorrection[i],Momentum[i],MomentumCorrection[i])||!GearReference.Finite(Energy[i],EnergyCorrection[i],0,0))return false;
        for(int i=0;i<ducts;++i){if(!GearReference.Finite(TickMass[i],TickImpulse[i],0,0))return false;for(int j=0;j<9;++j)if(!Numeric.Finite(History[i,j])||!Numeric.Finite(Correction[i,j]))return false;}
        return true;
    }
    internal ulong Hash(ulong hash)
    {
        for(int i=0;i<nodes;++i){hash=Numeric.Hash(hash,Mass[i]);hash=Numeric.Hash(hash,MassCorrection[i]);hash=Numeric.Hash(hash,Momentum[i]);hash=Numeric.Hash(hash,MomentumCorrection[i]);hash=Numeric.Hash(hash,Energy[i]);hash=Numeric.Hash(hash,EnergyCorrection[i]);}
        for(int i=0;i<ducts;++i){for(int j=0;j<9;++j){hash=Numeric.Hash(hash,History[i,j]);hash=Numeric.Hash(hash,Correction[i,j]);}hash=Numeric.Hash(hash,TickMass[i]);hash=Numeric.Hash(hash,TickImpulse[i]);}return hash;
    }
}

public sealed partial class CompiledModel
{
    internal CompiledCompressibleFuel[] CompressibleFuels { get; private set; }=[];
    internal CompiledFuelDuct[] FuelDucts { get; private set; }=[];
    internal int[] CompressibleCellOwner { get; private set; }=[];
    internal int[] FuelDuctByComponent { get; private set; }=[];
    public bool HasEquilibriumFuelDucts=>FuelDucts.Length!=0;
    private void CompileCompressibleFuels(NodeDefinition[] definitions)
    {
        var result=new List<CompiledCompressibleFuel>();
        for(int i=0;i<Nodes.Length;++i)
        {
            var n=Nodes[i];if(n.Domain!=Domain.CompressibleFuel)continue;var d=definitions[i].Fuel!;
            double Q(Quantity q,Unit unit,string field)=>Convert(q,unit,n.Id,"fuel."+field);
            Require(d.Compliance==default,DiagnosticCode.Schema,n.Id,"fuel.compliance","Compressible fluid uses a liquid bulk modulus and rigid volume; omit wall compliance.");
            CompressibleFuel law;
            try
            {
                law=new(new(new(Q(d.LiquidSpecificHeat,Unit.JoulePerKilogramKelvin,"liquid_specific_heat"),Q(d.VaporSpecificHeat,Unit.JoulePerKilogramKelvin,"vapor_specific_heat"),Q(d.ReferenceTemperature,Unit.Kelvin,"reference_temperature"),Q(d.LatentInternalEnergy,Unit.JoulePerKilogram,"latent_internal_energy"),0),
                    Q(d.GasConstant,Unit.JoulePerKilogramKelvin,"gas_constant"),Q(d.ReferencePressure,Unit.Pascal,"reference_pressure"),Q(d.MinimumTemperature,Unit.Kelvin,"minimum_temperature"),Q(d.MaximumTemperature,Unit.Kelvin,"maximum_temperature")),
                    Q(d.LiquidDensity,Unit.KilogramPerCubicMeter,"liquid_density"),Q(d.LiquidBulkModulus,Unit.Pascal,"liquid_bulk_modulus"),Q(d.MaximumPressure,Unit.Pascal,"maximum_pressure"));
            }
            catch(ModelCompileException){throw;}
            catch(ArgumentException ex){throw new ModelCompileException(DiagnosticCode.Range,n.Id,"fuel",ex.Message);}
            double velocity=Q(definitions[i].InitialVelocity,Unit.MeterPerSecond,"initial_velocity"),heating=Q(d.LowerHeatingValue,Unit.JoulePerKilogram,"lower_heating_value");
            Require(heating>0&&law.Evaluate(n.Position/n.Storage,n.Initial,out _),DiagnosticCode.Range,n.Id,"fuel.initial","Use positive finite mass and a valid equilibrium density/temperature inside declared bounds.");
            law.Evaluate(n.Position/n.Storage,n.Initial,out var point);
            Require(GearReference.Finite(n.Position*velocity,n.Position*(point.SpecificInternalEnergyJoulesPerKilogram+.5*velocity*velocity),n.Position*heating,0),DiagnosticCode.Range,n.Id,"initial_velocity","Initial mass, energy and momentum must be representable.");
            result.Add(new(i,law,point,velocity,heating));
        }
        CompressibleFuels=result.ToArray();CompressibleCellOwner=new int[result.Count];Array.Fill(CompressibleCellOwner,-1);
    }
    private void CompileFuelDucts(ComponentDefinition[] definitions)
    {
        FuelDuctByComponent=new int[Components.Length];Array.Fill(FuelDuctByComponent,-1);var result=new List<CompiledFuelDuct>();
        for(int i=0;i<Components.Length;++i)
        {
            var c=Components[i];if(c.Kind!=ComponentKind.EquilibriumFuelDuct)continue;var d=definitions[i].FuelDuct!;
            Require(d.Cells is not null&&d.Cells.Count is >=1 and <=24,DiagnosticCode.Capacity,c.Id,"fuel_duct.cells","Declare 1 through 24 ordered, distinct compressible fuel cells.");
            var ports=new int[d.Cells!.Count+2];ports[0]=Nodes[c.A].Index;ports[ports.Length-1]=Nodes[c.B].Index;double volume=0;
            for(int j=0;j<d.Cells.Count;++j)
            {
                int node=Array.FindIndex(Nodes,n=>n.Id==d.Cells[j]);
                Require(node>=0&&Nodes[node].Domain==Domain.CompressibleFuel&&node!=c.A&&node!=c.B,DiagnosticCode.Connection,c.Id,"fuel_duct.cells","Internal cells must be distinct compressible fuel nodes separate from both reservoirs.");
                int slot=Nodes[node].Index;Require(CompressibleCellOwner[slot]<0,DiagnosticCode.Connection,c.Id,"fuel_duct.cells","Each internal cell has exactly one duct owner.");CompressibleCellOwner[slot]=i;ports[j+1]=slot;volume+=Nodes[node].Storage;
            }
            var reference=CompressibleFuels[ports[0]];
            foreach(int slot in ports)Require(CompressibleCompatible(reference,CompressibleFuels[slot]),DiagnosticCode.Connection,c.Id,"fuel_duct.material","Duct reservoirs and cells require identical caloric, phase, bulk-modulus and heating-value definitions.");
            double area=Convert(d.Area,Unit.SquareMeter,c.Id,"fuel_duct.area"),linear=Convert(d.LinearResistance,Unit.PascalSecondPerCubicMeter,c.Id,"fuel_duct.linear_resistance"),quadratic=Convert(d.QuadraticResistance,Unit.PascalSecondSquaredPerCubicMeterSquared,c.Id,"fuel_duct.quadratic_resistance"),fraction=Convert(d.FluidHeatFraction,Unit.Fraction,c.Id,"fuel_duct.fluid_heat_fraction");
            Require(area>0&&linear>=0&&quadratic>=0&&fraction is >=0 and <=1&&Numeric.Finite(volume/area),DiagnosticCode.Range,c.Id,"fuel_duct","Declare positive area, nonnegative resistance and heat fraction in [0,1].");
            FuelDuctByComponent[i]=result.Count;result.Add(new(i,ports,Nodes[c.Heat].Index,Nodes[c.Heat].Storage,area,linear,quadratic,fraction,volume));
        }
        FuelDucts=result.ToArray();
        foreach(var duct in FuelDucts)
        {
            Require(CompressibleCellOwner[duct.Ports[0]]<0&&CompressibleCellOwner[duct.Ports[duct.Ports.Length-1]]<0,DiagnosticCode.Connection,Components[duct.Component].Id,"fuel_duct.ports","A reservoir cannot also be an internal duct cell.");
        }
        for(int i=0;i<CompressibleFuels.Length;++i)Require(CompressibleCellOwner[i]>=0||CompressibleFuels[i].Velocity==0,DiagnosticCode.Range,Nodes[CompressibleFuels[i].Node].Id,"initial_velocity","Finite reservoirs are stationary; nonzero velocity belongs to an internal duct cell.");
    }
    private static bool CompressibleCompatible(CompiledCompressibleFuel a,CompiledCompressibleFuel b)
    {
        var x=a.Law;var y=b.Law;var m=x.Material;var n=y.Material;
        return a.HeatingValue==b.HeatingValue&&x.LiquidBulkModulusPascals==y.LiquidBulkModulusPascals&&x.ReferenceLiquidDensityKilogramsPerCubicMeter==y.ReferenceLiquidDensityKilogramsPerCubicMeter&&x.MaximumPressurePascals==y.MaximumPressurePascals&&
            m.GasConstantJoulesPerKilogramKelvin==n.GasConstantJoulesPerKilogramKelvin&&m.ReferencePressurePascals==n.ReferencePressurePascals&&m.MinimumTemperatureKelvin==n.MinimumTemperatureKelvin&&m.MaximumTemperatureKelvin==n.MaximumTemperatureKelvin&&
            m.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin==n.Caloric.LiquidSpecificHeatJoulesPerKilogramKelvin&&m.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin==n.Caloric.VaporSpecificHeatJoulesPerKilogramKelvin&&m.Caloric.SaturationTemperatureKelvin==n.Caloric.SaturationTemperatureKelvin&&m.Caloric.LatentInternalEnergyJoulesPerKilogram==n.Caloric.LatentInternalEnergyJoulesPerKilogram;
    }
}

internal sealed class EquilibriumFuelDuctSolver(CompiledModel model)
{
    private readonly CompressibleFuelPoint[] _point=new CompressibleFuelPoint[model.CompressibleFuels.Length];
    private readonly double[] _dm=new double[model.CompressibleFuels.Length],_dp=new double[model.CompressibleFuels.Length],_de=new double[model.CompressibleFuels.Length];
    internal static bool Point(CompiledModel model,CompressibleFuelState s,int i,out CompressibleFuelPoint p)
    {
        var d=model.CompressibleFuels[i];double m=s.Mass[i];
        return d.Law.Flash(m/model.Nodes[d.Node].Storage,s.Energy[i]/m-.5*(s.Momentum[i]/m)*(s.Momentum[i]/m),out p);
    }
    internal bool Advance(CompressibleFuelState s,double[] wallTemperature,double duration)
    {
        for(int index=0;index<model.FuelDucts.Length;++index)
        {
            var duct=model.FuelDucts[index];var ports=duct.Ports;double remaining=duration;
            for(int iteration=0;remaining>0;++iteration)
            {
                if(iteration>=4096)return false;double speed=0,minVolume=double.PositiveInfinity,entropyBefore=0,entropyScale=duct.WallCapacity;
                foreach(int i in ports)
                {
                    if(!Point(model,s,i,out _point[i]))return false;
                    double velocity=s.Momentum[i]/s.Mass[i];speed=Math.Max(speed,Math.Abs(velocity)+model.CompressibleFuels[i].Law.AcousticSpeedBoundMetersPerSecond);
                    minVolume=Math.Min(minVolume,model.Nodes[model.CompressibleFuels[i].Node].Storage);entropyBefore+=s.Mass[i]*_point[i].SpecificEntropyJoulesPerKilogramKelvin;
                    entropyScale+=s.Mass[i]*(_point[i].IsochoricHeatCapacityJoulesPerKilogramKelvin+Math.Abs(_point[i].SpecificEntropyJoulesPerKilogramKelvin));_dm[i]=_dp[i]=_de[i]=0;
                }
                double h=Math.Min(remaining,.2*minVolume/(duct.Area*speed));if(!Numeric.Finite(h)||h<=0)return false;
                double massA=0,massB=0,energyA=0,energyB=0,impulse=0;
                for(int face=0;face<ports.Length-1;++face)
                {
                    int a=ports[face],b=ports[face+1];double va=model.Nodes[model.CompressibleFuels[a].Node].Storage,vb=model.Nodes[model.CompressibleFuels[b].Node].Storage;
                    double ua=s.Momentum[a]/s.Mass[a],ub=s.Momentum[b]/s.Mass[b],rhoA=s.Mass[a]/va,rhoB=s.Mass[b]/vb;
                    double fm=.5*(s.Momentum[a]/va+s.Momentum[b]/vb)-.5*speed*(rhoB-rhoA);
                    double fp=.5*(rhoA*ua*ua+_point[a].PressurePascals+rhoB*ub*ub+_point[b].PressurePascals)-.5*speed*(s.Momentum[b]/vb-s.Momentum[a]/va);
                    double fe=.5*((s.Energy[a]/va+_point[a].PressurePascals)*ua+(s.Energy[b]/vb+_point[b].PressurePascals)*ub)-.5*speed*(s.Energy[b]/vb-s.Energy[a]/va);
                    double mass=h*duct.Area*fm,momentum=h*duct.Area*fp,energy=h*duct.Area*fe;
                    if(!GearReference.Finite(mass,momentum,energy,0))return false;
                    _dm[a]-=mass;_dm[b]+=mass;_de[a]-=energy;_de[b]+=energy;
                    if(face>0)_dp[a]-=momentum;else{massA=mass;energyA=energy;impulse-=momentum;}
                    if(face+1<ports.Length-1)_dp[b]+=momentum;else{massB=mass;energyB=energy;impulse+=momentum;}
                }
                foreach(int i in ports)
                {
                    Numeric.Accumulate(_dm[i],ref s.Mass[i],ref s.MassCorrection[i]);Numeric.Accumulate(_dp[i],ref s.Momentum[i],ref s.MomentumCorrection[i]);Numeric.Accumulate(_de[i],ref s.Energy[i],ref s.EnergyCorrection[i]);
                    if(s.Mass[i]<=0||!Point(model,s,i,out _point[i]))return false;
                }
                double friction=0,wallHeat=0,oldWall=wallTemperature[duct.Wall];
                for(int cell=1;cell<ports.Length-1;++cell)
                {
                    int i=ports[cell];double volume=model.Nodes[model.CompressibleFuels[i].Node].Storage,weight=volume/duct.LineVolume,mass=s.Mass[i],u=s.Momentum[i]/mass;
                    double linear=duct.Area*duct.Area*duct.Linear*weight/mass,quadratic=duct.Area*duct.Area*duct.Area*duct.Quadratic*weight/mass;
                    double exponential=Math.Exp(-linear*h),integral=linear==0?h:-Numeric.Expm1(-linear*h)/linear;
                    double next=u*exponential/(1+quadratic*Math.Abs(u)*integral),loss=.5*mass*(u-next)*(u+next),outward=(1-duct.FluidFraction)*loss;
                    if(!GearReference.Finite(next,loss,outward,0)||loss<0)return false;
                    double change=mass*(next-u);Numeric.Accumulate(change,ref s.Momentum[i],ref s.MomentumCorrection[i]);Numeric.Accumulate(-outward,ref s.Energy[i],ref s.EnergyCorrection[i]);
                    impulse-=change;friction+=loss;wallHeat+=outward;
                }
                wallTemperature[duct.Wall]+=wallHeat/duct.WallCapacity;
                double entropyAfter=0;
                foreach(int i in ports){if(!Point(model,s,i,out _point[i]))return false;entropyAfter+=s.Mass[i]*_point[i].SpecificEntropyJoulesPerKilogramKelvin;}
                double entropy=entropyAfter-entropyBefore+duct.WallCapacity*Numeric.Log1p((wallTemperature[duct.Wall]-oldWall)/oldWall);
                if(!Numeric.Finite(entropy)||entropy < -2048*GearReference.Epsilon*entropyScale)return false;
                s.Add(index,0,massA);s.Add(index,1,massB);s.Add(index,2,energyA);s.Add(index,3,energyB);s.Add(index,4,friction);s.Add(index,5,friction-wallHeat);s.Add(index,6,wallHeat);s.Add(index,7,entropy);s.Add(index,8,impulse);
                s.TickMass[index]+=massA;s.TickImpulse[index]+=impulse;remaining=h==remaining?0:remaining-h;
            }
        }
        return s.Finite();
    }
}
