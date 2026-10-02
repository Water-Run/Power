// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using Power.Core;
using static Power.Tests.CoreChecks;
using static Power.Tests.GearModelChecks;

namespace Power.Tests;

internal static class ResolvedPlanetChecks
{
    internal static IEnumerable<(string Name, Action Run)> All =>
    [
        ("carrier-relative mesh / signed ratios / independent free mass matrix / reaction power", Mesh),
        ("resolved planets / SI geometry / orbit and absolute spin / immutable ordinary graph", Geometry),
        ("resolved planets free dynamics / six-rotor mass matrix / angular momentum and work", Free),
        ("resolved planets four forward and reverse / independent reflected inertia", Ranges),
        ("resolved planet carrier capture / independent impulse and heat", Capture),
        ("resolved planets long loaded overdrive / strict phase and conservation", LongRun),
        ("resolved planet transactions / all mesh reactions heat correction / zero allocation", Transactions),
        ("resolved planet contracts / units packing geometry IDs and carrier ratio", Contracts)
    ];
    internal static RavigneauxPlanetParameters Planets() => new() { RingPitchRadius = new(.1, Unit.Meter), PlanetCount = 3,
        InnerPlanetMass = new(.3, Unit.Kilogram), OuterPlanetMass = new(1, Unit.Kilogram),
        InnerPlanetSpinInertia = new(.000015, Unit.KilogramMeterSquared), OuterPlanetSpinInertia = new(.0005, Unit.KilogramMeterSquared) };
    internal static RavigneauxPlanetPorts PlanetPorts() => new(104, 105, 203, 204);
    internal static RavigneauxGraph Graph(double ring = 0, int range = 0) => new RavigneauxTransmissionAssembly(RavigneauxChecks.Parameters()).CreateResolvedGraph(RavigneauxChecks.Ports(), PlanetPorts(), Planets(), ring, range);
    internal static ModelDefinition Model(int range = 1, ulong tick = 100_000)
    {
        var original = RavigneauxChecks.Model(range, tick); var graph = Graph(original.Nodes.Single(n => n.Id == 4).Initial.Value * 4, range);
        return original with { Nodes = [..original.Nodes.Where(n => n.Id < 100), ..graph.Nodes], Components = [..graph.Components, ..original.Components.Where(c => c.Kind == ComponentKind.TorqueSource)] };
    }
    private static void Ok(SimulationStatus status) { if (status != SimulationStatus.Ok) throw new InvalidOperationException(status.ToString()); }
    private static SnapshotInfo Snapshot(Simulation s) => s.ReadSnapshot(new Scalar[s.Model.OutputCount]);
    private const double Orbit = 3 * (.075 * .075 + .3 * (1.0 / 24) * (1.0 / 24));
    private static (double A, double B, double D) Mass() => (.255 + .0015 * 16 + .000045 * 144,
        -.21 - .0015 * 12 - .000045 * 156, .27 + Orbit + .0015 * 9 + .000045 * 169);
    private static void Mesh()
    {
        foreach (double r in new[] { -3.0, -.5, .25, .5, 1, 2, 4 })
        {
            var definition = new ModelDefinition { StepNanoseconds = 100_000,
                Nodes = [NodeDefinition.Rotor(1, .2, 10 + 10 * r), NodeDefinition.Rotor(2, .3, 20), NodeDefinition.Rotor(3, .4, 10)],
                Components = [ComponentDefinition.CarrierGear(10, 1, 2, 3, r), ComponentDefinition.Torque(20, 1, 500, 8), ComponentDefinition.Torque(21, 2, 501, -3), ComponentDefinition.Torque(22, 3, 502, -2)] };
            double a = .3 + .2 * r * r, b = .2 * r * (1-r), d = .4 + .2 * (1-r) * (1-r), det = a*d-b*b;
            double qb = r*8-3, qc = (1-r)*8-2, ab=(d*qb-b*qc)/det, ac=(a*qc-b*qb)/det;
            var s=CompiledModel.Compile(definition).CreateSimulation(); Ok(s.Step(20_000_000));
            Near(Value(s,2,Field.Speed),20+.02*ab,1e-10); Near(Value(s,3,Field.Speed),10+.02*ac,1e-10);
            Near(Value(s,1,Field.Speed),10+10*r+.02*(r*ab+(1-r)*ac),1e-10);
            Near(Value(s,10,Field.TorqueAtB),-r*Value(s,10,Field.Torque),1e-9);
            Near(Value(s,10,Field.TorqueAtC),(r-1)*Value(s,10,Field.Torque),1e-9);
            Near(Value(s,10,Field.Torque)*Value(s,1,Field.Speed)+Value(s,10,Field.TorqueAtB)*Value(s,2,Field.Speed)+Value(s,10,Field.TorqueAtC)*Value(s,3,Field.Speed),0,1e-9);
            Near(Value(s,0,Field.EnergyResidual),0,1e-8);
        }
    }
    private static void Geometry()
    {
        var assembly=new RavigneauxTransmissionAssembly(RavigneauxChecks.Parameters()); var geometry=assembly.ResolvePlanetGeometry(Planets()); var graph=Graph(20);
        Near(geometry.LargeSunRadiusMeters,.05,1e-15); Near(geometry.SmallSunRadiusMeters,1.0/30,1e-15);
        Near(geometry.InnerPlanetRadiusMeters,1.0/120,1e-15); Near(geometry.OuterPlanetRadiusMeters,.025,1e-15);
        Near(geometry.InnerOrbitRadiusMeters,1.0/24,1e-15); Near(geometry.OuterOrbitRadiusMeters,.075,1e-15);
        Near(geometry.OrbitalInertiaKilogramMeterSquared,Orbit,1e-15);
        Require(graph.Nodes.Count==6 && graph.Components.Count==10 && graph.Components.Count(c=>c.Kind==ComponentKind.CarrierGear)==4);
        Near(graph.Nodes.Single(n=>n.Id==103).Storage.Value,.03+Orbit,1e-15);
        Near(graph.Nodes.Single(n=>n.Id==104).Initial.Value,-240,1e-12); Near(graph.Nodes.Single(n=>n.Id==105).Initial.Value,80,1e-12);
    }
    private static ModelDefinition FreeModel(bool capture=false)
    {
        var graph=Graph(20); var nodes=graph.Nodes.Select(n=>n.Id switch { 100=>n with { Initial=new(-10,Unit.RadianPerSecond) }, 101=>n with { Initial=new(40,Unit.RadianPerSecond) },
            103=>n with { Initial=new(10,Unit.RadianPerSecond) }, 104=>n with { Initial=new(-110,Unit.RadianPerSecond) }, 105=>n with { Initial=new(50,Unit.RadianPerSecond) }, _=>n }).ToArray();
        var components=graph.Components.Where(c=>c.Kind==ComponentKind.CarrierGear).ToArray();
        if(capture)return new(){StepNanoseconds=100_000,Nodes=[..nodes,NodeDefinition.Thermal(7,1000,300)],Components=[..components,ComponentDefinition.Clutch(40,103,0,400,300,500,1,heat:7)]};
        double[] torques=[2,-1,3,-2,.1,-.2];
        return new(){StepNanoseconds=100_000,Nodes=nodes,Components=[..components,..Enumerable.Range(0,6).Select(i=>ComponentDefinition.Torque((uint)(20+i),(uint)(100+i),(ulong)(500+i),torques[i]))]};
    }
    private static void Free()
    {
        var mass=Mass(); double qr=-2*2+3*-1+3-12*.1+4*-.2,qc=3*2-2*-1-2+13*.1-3*-.2,det=mass.A*mass.D-mass.B*mass.B;
        double ar=(mass.D*qr-mass.B*qc)/det,ac=(mass.A*qc-mass.B*qr)/det;
        var definition=FreeModel(); var s=CompiledModel.Compile(definition).CreateSimulation(); Ok(s.Step(20_000_000));
        foreach(var term in new[]{(100U,-10.0,-2.0,3.0),(101U,40.0,3.0,-2.0),(102U,20.0,1.0,0.0),(103U,10.0,0.0,1.0),(104U,-110.0,-12.0,13.0),(105U,50.0,4.0,-3.0)})
            Near(Value(s,term.Item1,Field.Speed),term.Item2+.02*(term.Item3*ar+term.Item4*ac),1e-9);
        double initial=definition.Nodes.Sum(n=>n.Storage.Value*n.Initial.Value),final=definition.Nodes.Sum(n=>n.Storage.Value*Value(s,n.Id,Field.Speed));
        Near(final-initial,.02*(2-1+3-2+.1-.2),1e-10);
        foreach(var c in definition.Components.Where(c=>c.Kind==ComponentKind.CarrierGear))
            Near(Value(s,c.Id,Field.Torque)*Value(s,c.NodeA,Field.Speed)+Value(s,c.Id,Field.TorqueAtB)*Value(s,c.NodeB,Field.Speed)+Value(s,c.Id,Field.TorqueAtC)*Value(s,c.NodeC,Field.Speed),0,1e-8);
        Near(Value(s,0,Field.EnergyResidual),0,1e-8);
    }
    private static double Inertia(double r,double c)
    {
        double large=-2*r+3*c,small=3*r-2*c,inner=-12*r+13*c,outer=4*r-3*c;
        return .2+10*r*r/16+.02*large*large+.015*small*small+.04*r*r+(.03+Orbit)*c*c+.000045*inner*inner+.0015*outer*outer;
    }
    private static void Ranges()
    {
        foreach(int range in new[]{1,2,3,4,-1})
        {
            double r=range switch{1=>1.0/3,2=>3.0/5,3=>1,4=>1.5,_=>-.5},c=range switch{2=>.4,3 or 4=>1,_=>0};
            double acceleration=(10+(range==-1?2:-2)*r/4)/Inertia(r,c);
            var s=CompiledModel.Compile(Model(range)).CreateSimulation(); Ok(s.Step(20_000_000));
            double input=30+.02*acceleration; Near(Value(s,1,Field.Speed),input,1e-9);
            Near(Value(s,104,Field.Speed),(-12*r+13*c)*input,1e-9); Near(Value(s,105,Field.Speed),(4*r-3*c)*input,1e-9);
            Near(Value(s,4,Field.Speed),r/4*input,1e-9); Near(Value(s,0,Field.EnergyResidual),0,1e-8);
        }
    }
    private static void Capture()
    {
        var m=Mass();double det=m.A*m.D-m.B*m.B,mobility=m.A/det,impulse=-10/mobility,heat=.5*100/mobility;
        var s=CompiledModel.Compile(FreeModel(true)).CreateSimulation();Ok(s.Step(100_000_000));
        Near(Value(s,103,Field.Speed),0,1e-9);Near(Value(s,102,Field.Speed),20-m.B/det*impulse,1e-9);
        Near(Value(s,40,Field.FrictionHeat),heat,1e-8);Near(Value(s,7,Field.Temperature),300+heat/1000,1e-10);Near(Value(s,0,Field.EnergyResidual),0,1e-8);
    }
    private static void LongRun()
    {
        var s=CompiledModel.Compile(Model(4)).CreateSimulation();double acceleration=(10-2*1.5/4)/Inertia(1.5,1);
        Ok(s.Step(10_000_000_000));Ok(s.Step(10_000_000_000));Near(Value(s,1,Field.Speed),30+20*acceleration,2e-7);
        Near(Value(s,0,Field.EnergyResidual),0,1e-6);foreach(uint id in new uint[]{200,201,202,203,204})Near(Value(s,id,Field.ConstraintError),0,1e-8);
    }
    private static void Transactions()
    {
        var s=CompiledModel.Compile(Model()).CreateSimulation();var before=Snapshot(s);
        Require(s.Step(10_000_000,new CancellationToken(true))==SimulationStatus.Cancelled&&Snapshot(s)==before);
        var fork=s.Fork();Ok(s.Step(10_000_000));for(int i=0;i<10;++i)Ok(fork.Step(1_000_000));Require(Snapshot(s)==Snapshot(fork));
        before=Snapshot(s);Require(s.Step(10_000_000,[new(before.TimeNanoseconds+5_000_000,500,double.MaxValue)])==SimulationStatus.NumericalFailure&&Snapshot(s)==before);
        var values=new Scalar[s.Model.OutputCount];for(int i=0;i<10;++i){Ok(s.Step(100_000));s.ReadSnapshot(values);}
        long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;++i){Ok(s.Step(100_000));s.ReadSnapshot(values);}Require(bytes==GC.GetAllocatedBytesForCurrentThread());
    }
    private static void Contracts()
    {
        var a=new RavigneauxTransmissionAssembly(RavigneauxChecks.Parameters());
        Throws<ArgumentException>(()=>a.ResolvePlanetGeometry(Planets() with{RingPitchRadius=new(.1,Unit.Kilogram)}));
        Throws<ArgumentException>(()=>a.ResolvePlanetGeometry(Planets() with{PlanetCount=32}));
        Throws<ArgumentException>(()=>a.ResolvePlanetGeometry(Planets() with{InnerPlanetSpinInertia=new(0,Unit.KilogramMeterSquared)}));
        Throws<ArgumentException>(()=>a.CreateResolvedGraph(RavigneauxChecks.Ports(),PlanetPorts() with{OuterPlanet=100},Planets()));
        var model=Model();var components=model.Components.ToArray();components[0]=components[0] with{Ratio=0};Throws<ModelCompileException>(()=>CompiledModel.Compile(model with{Components=components}));
        components[0]=model.Components[0] with{NodeC=0};Throws<ModelCompileException>(()=>CompiledModel.Compile(model with{Components=components}));
    }
}
