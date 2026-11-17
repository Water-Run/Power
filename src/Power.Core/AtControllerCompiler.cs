// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

namespace Power.Core;

public sealed partial class CompiledModel
{
    private void CompileAtControllers(ComponentDefinition[] definitions)
    {
        int slot = 0;
        for (int index = 0; index < Components.Length; ++index)
        {
            if (Components[index].Kind != ComponentKind.HydraulicAtController) continue;
            var c = Components[index]; var d = definitions[index].HydraulicAtController!;
            int Node(uint id, Domain domain, string field)
            {
                int result = Array.FindIndex(Nodes,n=>n.Id==id);
                Require(result>=0 && Nodes[result].Domain==domain,DiagnosticCode.Connection,c.Id,field,$"Use an existing {domain} node."); return result;
            }
            int Component(uint id, ComponentKind kind, string field)
            {
                int result = Array.FindIndex(Components,item=>item.Id==id);
                Require(result>=0 && Components[result].Kind==kind,DiagnosticCode.Connection,c.Id,field,$"Use an existing {kind} component."); return result;
            }
            double Quantity(Quantity q, Unit unit, string field, bool positive=true)
            {
                double value=Convert(q,unit,c.Id,field); Require(positive?value>0:value>=0,DiagnosticCode.Range,c.Id,field,"Use a finite positive value, or a declared nonnegative limit.");return value;
            }
            ulong Duration(ulong value,string field,bool zero=false)
            {
                Require((zero||value>0)&&value<=10_000_000_000&&value%StepNanoseconds==0,
                    DiagnosticCode.Range,c.Id,field,"Use a tick-aligned duration no greater than 10 s.");return value;
            }
            int vehicle=Node(d.VehicleNode,Domain.Rotational,"at_controller.vehicle_node"),ring=Node(d.RingNode,Domain.Rotational,"at_controller.ring_node");
            int supply=Node(d.SupplyPressureNode,Domain.Hydraulic,"at_controller.supply_pressure_node");
            Require(vehicle!=c.A && ring!=vehicle && ring!=c.A,DiagnosticCode.Connection,c.Id,"at_controller.ports","Input, ring and vehicle are distinct rotational ports.");
            Require(d.Routes.Count is 5 or 6,DiagnosticCode.Schema,c.Id,"at_controller.routes","Order routes as carrier input, small-sun input, large-sun input, carrier brake, large-sun brake and optional lockup.");
            var routes=new CompiledAtRoute[d.Routes.Count];var ids=new HashSet<int>();
            for(int i=0;i<routes.Length;++i)
            {
                var route=d.Routes[i];int clutch=Component(route.Clutch,ComponentKind.PistonClutch,"at_controller.routes.clutch");
                int piston=PistonFriction[clutch]!.Piston;var hydraulic=Components[piston];
                int fill=Component(route.FillValve,ComponentKind.HydraulicResistance,"at_controller.routes.fill_valve");
                int drain=Component(route.DrainValve,ComponentKind.HydraulicResistance,"at_controller.routes.drain_valve");
                Require(ids.Add(clutch)&&ids.Add(piston)&&ids.Add(fill)&&ids.Add(drain),DiagnosticCode.Connection,c.Id,"at_controller.routes","Routes own distinct clutch, piston and valve components.");
                Require(Components[fill].A==supply&&Components[fill].B==hydraulic.B&&Components[drain].A==hydraulic.B&&Components[drain].B<0&&Components[clutch].P3==1,
                    DiagnosticCode.Connection,c.Id,"at_controller.routes","Fill connects supply to the clutch chamber; drain connects that chamber to the explicit tank; clutch ratio is one.");
                foreach(var pair in new[]{(fill,0.0),(drain,1.0)})
                {
                    var valve=Components[pair.Item1];Require(valve.InputChannel!=0&&valve.InitialInput==pair.Item2&&ControlledInputs.Add(valve.InputChannel),
                        DiagnosticCode.Channel,c.Id,"at_controller.routes","Each valve has one owner and starts with fill closed/drain open.");InputIndices.Remove(valve.InputChannel);
                }
                routes[i]=new(clutch,piston,Nodes[hydraulic.B].Index,Nodes[hydraulic.A].Index,fill,drain);
            }
            var a=Components[routes[0].Clutch];var b=Components[routes[1].Clutch];var large=Components[routes[2].Clutch];
            var carrierBrake=Components[routes[3].Clutch];var largeBrake=Components[routes[4].Clutch];
            Require(a.A==c.A&&b.A==c.A&&large.A==c.A&&a.B==carrierBrake.A&&large.B==largeBrake.A&&carrierBrake.B<0&&largeBrake.B<0&&
                a.B>=0&&b.B>=0&&large.B>=0&&a.B!=b.B&&a.B!=large.B&&b.B!=large.B,
                DiagnosticCode.Connection,c.Id,"at_controller.routes","Range paths must bind the same input to distinct carrier/small/large members and matching carrier/large ground brakes.");
            Require(definitions.Any(g=>g.Kind==ComponentKind.IdealGear&&g.NodeA==d.RingNode&&g.NodeB==d.VehicleNode&&g.Ratio>0),
                DiagnosticCode.Connection,c.Id,"at_controller.ring_node","A positive final-drive reduction connects ring to vehicle.");
            uint carrierId=Nodes[a.B].Id,smallId=Nodes[b.B].Id,largeId=Nodes[large.B].Id;
            var single=definitions.FirstOrDefault(g=>g.Kind==ComponentKind.PlanetaryGear&&g.NodeA==largeId&&g.NodeB==d.RingNode&&g.NodeC==carrierId);
            var dual=definitions.FirstOrDefault(g=>g.Kind==ComponentKind.DoublePinionPlanetaryGear&&g.NodeA==smallId&&g.NodeB==d.RingNode&&g.NodeC==carrierId);
            bool reduced=single is not null&&dual is not null&&dual.Ratio>single.Ratio;
            var lm=definitions.FirstOrDefault(g=>g.Kind==ComponentKind.CarrierGear&&g.NodeA==largeId&&g.NodeC==carrierId&&g.Ratio<0);
            var sm=definitions.FirstOrDefault(g=>g.Kind==ComponentKind.CarrierGear&&g.NodeA==smallId&&g.NodeC==carrierId&&g.Ratio<0);
            var rm=lm is null?null:definitions.FirstOrDefault(g=>g.Kind==ComponentKind.CarrierGear&&g.NodeA==d.RingNode&&g.NodeB==lm.NodeB&&g.NodeC==carrierId&&g.Ratio>0&&g.Ratio<1);
            var pm=lm is null||sm is null?null:definitions.FirstOrDefault(g=>g.Kind==ComponentKind.CarrierGear&&g.NodeA==sm.NodeB&&g.NodeB==lm.NodeB&&g.NodeC==carrierId&&g.Ratio<0);
            double kl=rm is null?0:-lm!.Ratio/rm.Ratio,ks=rm is null||pm is null?0:sm!.Ratio*pm.Ratio/rm.Ratio;
            Require(reduced||lm is not null&&sm is not null&&rm is not null&&pm is not null&&Numeric.Finite(kl)&&Numeric.Finite(ks)&&ks>kl&&kl>1,
                DiagnosticCode.Connection,c.Id,"at_controller.topology","Use a compatible reduced or four-mesh Ravigneaux topology for the declared range schedule.");
            if(routes.Length==6)
            {
                var lockup=Components[routes[5].Clutch];Require(lockup.B==c.A&&Components.Any(item=>item.Kind==ComponentKind.TorqueConverter&&item.A==lockup.A&&item.B==c.A),
                    DiagnosticCode.Connection,c.Id,"at_controller.lockup","Optional lockup is a separate pump-to-input clutch parallel to the converter.");
            }
            ulong period=Duration(d.SamplePeriodNanoseconds,"at_controller.sample_period_ns");
            Require(period<=1_000_000_000,DiagnosticCode.Range,c.Id,"at_controller.sample_period_ns","Sampling must be no slower than 1 s.");
            ulong release=Duration(d.ReleaseTimeoutNanoseconds,"at_controller.release_timeout_ns"),apply=Duration(d.ApplyTimeoutNanoseconds,"at_controller.apply_timeout_ns"),low=Duration(d.LowSupplyTimeoutNanoseconds,"at_controller.low_supply_timeout_ns"),dwell=Duration(d.LockupDwellNanoseconds,"at_controller.lockup_dwell_ns",true);
            Require(release>=period&&apply>=period&&low>=period&&release%period==0&&apply%period==0&&low%period==0&&dwell%period==0,
                DiagnosticCode.Range,c.Id,"at_controller.timings","Timeouts/dwell align to the sample period; each timeout is at least one period.");
            double target=Quantity(d.ApplyPressure,Unit.Pascal,"at_controller.apply_pressure"),tolerance=Quantity(d.PressureTolerance,Unit.Pascal,"at_controller.pressure_tolerance");
            double releaseForce=Quantity(d.ReleaseForce,Unit.Newton,"at_controller.release_force",false),applyForce=Quantity(d.MinimumApplyForce,Unit.Newton,"at_controller.minimum_apply_force");
            double lockLimit=Quantity(d.LockupSpeedLimit,Unit.RadianPerSecond,"at_controller.lockup_speed_limit",false),unlock=Quantity(d.UnlockSpeedLimit,Unit.RadianPerSecond,"at_controller.unlock_speed_limit");
            Require(tolerance<target&&applyForce>releaseForce&&unlock>lockLimit&&d.MinimumLockupForwardRange is >=1 and <=4,
                DiagnosticCode.Range,c.Id,"at_controller.limits","Pressure tolerance is below target, apply force exceeds release force, and unlock slip exceeds lockup slip; minimum lockup range is 1..4.");
            AtControllers[slot]=new(index,Nodes[c.A].Index,Nodes[vehicle].Index,Nodes[supply].Index,routes,period,release,apply,low,dwell,target,tolerance,
                Quantity(d.MinimumSupplyPressure,Unit.Pascal,"at_controller.minimum_supply_pressure"),releaseForce,applyForce,
                Quantity(d.ProportionalGain,Unit.FractionPerPascal,"at_controller.proportional_gain",false),Quantity(d.IntegralGain,Unit.FractionPerPascalSecond,"at_controller.integral_gain",false),
                Quantity(d.SynchronizeTolerance,Unit.RadianPerSecond,"at_controller.synchronize_tolerance"),Quantity(d.DirectionChangeSpeedLimit,Unit.RadianPerSecond,"at_controller.direction_change_speed_limit",false),
                lockLimit,unlock,Quantity(d.MinimumLockupInputSpeed,Unit.RadianPerSecond,"at_controller.minimum_lockup_input_speed",false),d.MinimumLockupForwardRange);
            Require(AtControllers[slot].Kp>0||AtControllers[slot].Ki>0,DiagnosticCode.Range,c.Id,"at_controller.gains","At least one pressure gain is positive.");
            AtControllerByComponent[index]=slot++;
        }
    }
}
