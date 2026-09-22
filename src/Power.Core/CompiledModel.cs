// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Collections.ObjectModel;

namespace Power.Core;

internal readonly record struct GraphNode(uint Id, Domain Domain, int Index, double Storage, double Initial, double Position);
internal readonly record struct GraphComponent(uint Id, ComponentKind Kind, int A, int B, int Heat, int Index,
    ulong InputChannel, double InitialInput, double P0, double P1, double P2, double P3, int C = -1);
internal readonly record struct OutputBinding(uint ObjectId, Field Field, int Index, bool IsComponent = false);

/// <summary>Immutable, validated topology and solver factors shared by independent simulations.</summary>
public sealed class CompiledModel
{
    public const int MaxNodes = 32, MaxComponents = 64, MaxStates = 64, MaxConverters = 8;
    public ulong StepNanoseconds { get; }
    public ulong Fingerprint { get; }
    public int NodeCount => Nodes.Length;
    public int ComponentCount => Components.Length;
    public int StateCount => DynamicCount + ThermalCount + 2 * GasCount + 3 * NodeMixtures.Count(m => m is not null) + Burners.Count(b => b is not null) + ClutchComponents.Length + GearComponents.Length + 4 * ConverterComponents.Length + HydraulicCount + 3 * HydraulicComponents.Length + 4 * PumpComponents.Length;
    public int OutputCount => Outputs.Length;
    public string Fidelity => HasPumps ? "shaft_driven_hydraulics" : HasHydraulics ? "compliant_hydraulic_powertrain" : HasConverters ? "quasisteady_converter_powertrain" : HasGears ? "constrained_gear_powertrain" : HasClutches ? "hybrid_clutch_powertrain" : Burners.Any(b => b is not null) ? "premixed_wiebe_combustion"
        : HasPremixedGas ? "premixed_gas_transport"
        : HasValveTiming ? "crank_timed_gas_exchange"
        : GasCylinders.Any(c => c is not null) ? "moving_cylinder_gas_exchange"
        : GasCount > 0 ? "finite_volume_gas_exchange"
        : Cylinders.Any(c => c is not null) ? "sealed_adiabatic_gas" : "linear_lumped";
    public string Calibration => "unverified";
    public ReadOnlyCollection<ChannelInfo> Channels { get; }
    internal GraphNode[] Nodes { get; }
    internal GraphComponent[] Components { get; }
    internal CylinderPhysics?[] Cylinders { get; }
    internal GasCylinderPhysics?[] GasCylinders { get; }
    internal int[] GasCylinderByNode { get; }
    internal bool HasMovingGas { get; }
    internal TimedValve?[] Valves { get; }
    internal bool HasValveTiming { get; }
    internal PremixedGas?[] NodeMixtures { get; }
    internal MassFractions?[] ReservoirFractions { get; }
    internal bool HasPremixedGas { get; }
    internal Burner?[] Burners { get; }
    internal int[] BurnerByGas { get; }
    internal CylinderCoupling? CylinderCoupling { get; }
    internal GasNetwork? Gas { get; }
    internal OutputBinding[] Outputs { get; }
    internal Dictionary<ulong, int> InputIndices { get; } = [];
    internal double[] InputMinimum { get; }
    internal double[] InputMaximum { get; }
    internal IdealGas?[] NodeGases { get; }
    internal double Dt { get; }
    internal int DynamicCount { get; }
    internal int ThermalCount { get; }
    internal int GasCount { get; }
    internal Factorization Dynamics { get; }
    internal Factorization Thermal { get; }
    internal double[] ConstantForce { get; }
    internal double[] AmbientForce { get; }
    public bool HasClutches => ClutchComponents.Length != 0;
    internal int[] ClutchComponents { get; }
    public bool HasGears => GearComponents.Length != 0;
    internal int[] GearComponents { get; }
    internal GearCoupling? Gears { get; }
    public bool HasConverters => ConverterComponents.Length != 0;
    internal int[] ConverterComponents { get; }
    internal TorqueConverter?[] Converters { get; }
    internal ConverterCoupling? ConverterCoupling { get; }
    public bool HasHydraulics => HydraulicCount > 0;
    public bool HasPumps => PumpComponents.Length != 0;
    internal int[] PumpComponents { get; }
    internal int HydraulicCount { get; }
    internal int[] HydraulicComponents { get; }
    internal HydraulicRestriction?[] HydraulicLaws { get; }
    internal HydraulicActuator?[] HydraulicActuators { get; }
    internal double[,]? DynamicRates { get; }
    internal double[,]? HeatConductance { get; }

    public static CompiledModel Compile(ModelDefinition definition) => new(definition);
    public static bool TryCompile(ModelDefinition definition, out CompiledModel? model, out ModelDiagnostic? diagnostic)
    {
        try { model = Compile(definition); diagnostic = null; return true; }
        catch (ModelCompileException error)
        {
            model = null;
            diagnostic = new(error.Code, error.ObjectId, error.Field, error.Message);
            return false;
        }
        catch (ArgumentNullException error)
        {
            model = null;
            diagnostic = new(DiagnosticCode.Schema, 0, "model", error.Message);
            return false;
        }
    }
    public Simulation CreateSimulation() => new(this);

    /// <summary>Validate one input's channel, finiteness and static range without mutating state.
    /// Simulation submission also checks frame duplicates and state-dependent observables.</summary>
    public SimulationStatus ValidateInput(Scalar input)
    {
        if (!Numeric.Finite(input.Value)) return SimulationStatus.InvalidInput;
        if (!InputIndices.TryGetValue(input.Channel, out int index)) return SimulationStatus.UnknownChannel;
        return input.Value < InputMinimum[index] || input.Value > InputMaximum[index]
            ? SimulationStatus.InvalidInput : SimulationStatus.Ok;
    }

    private static void Require(bool condition, DiagnosticCode code, uint id, string field, string message)
    {
        if (!condition) throw new ModelCompileException(code, id, field, message);
    }

    private static bool SameGas(IdealGas a, IdealGas b) =>
        a.GasConstantJoulePerKilogramKelvin == b.GasConstantJoulePerKilogramKelvin && a.Gamma == b.Gamma;

    internal static double Convert(Quantity q, Unit expected, uint id, string field)
    {
        double scale = 1, divisor = 1;
        if (q.Unit != expected)
        {
            if (q.Unit == Unit.Rpm && expected == Unit.RadianPerSecond) scale = 0.10471975511965977462;
            else if (q.Unit == Unit.Degree && expected == Unit.Radian) scale = 0.01745329251994329577;
            else if (q.Unit == Unit.Millimeter && expected == Unit.Meter) divisor = 1000;
            else if (q.Unit == Unit.SquareMillimeter && expected == Unit.SquareMeter) divisor = 1_000_000;
            else if (q.Unit == Unit.Liter && expected == Unit.CubicMeter) divisor = 1000;
            else if (q.Unit == Unit.Bar && expected == Unit.Pascal) scale = 100_000;
            else throw new ModelCompileException(DiagnosticCode.Unit, id, field, $"Expected {expected}, received {q.Unit}.");
        }
        double value = q.Value * scale / divisor;
        Require(Numeric.Finite(value) && (expected != Unit.None || value == 0), DiagnosticCode.Range,
            id, field, "Value must be finite; unused quantities must be zero/None.");
        return value == 0 ? 0 : value;
    }

    private CompiledModel(ModelDefinition definition)
    {
        if (definition is null) throw new ArgumentNullException(nameof(definition));
        Require(definition.SchemaVersion == 1, DiagnosticCode.Schema, 0, "schema", "Only model schema 1 is supported.");
        Require(definition.Nodes is { Length: > 0 and <= MaxNodes } &&
                definition.Components is { Length: <= MaxComponents }, DiagnosticCode.Capacity,
            0, "counts", "Expected 1..32 nodes and 0..64 components.");
        Require(definition.StepNanoseconds is > 0 and <= 1_000_000_000, DiagnosticCode.Range,
            0, "step_ns", "Fixed step must be 1 ns .. 1 s.");
        StepNanoseconds = definition.StepNanoseconds;
        Dt = StepNanoseconds * 1e-9;
        var ids = new HashSet<uint>();
        var ns = (NodeDefinition[])definition.Nodes!.Clone();
        var cs = (ComponentDefinition[])definition.Components!.Clone();
        Require(ns.All(n => n is not null) && cs.All(c => c is not null), DiagnosticCode.Schema,
            0, "descriptors", "Null descriptors are not allowed.");
        Array.Sort(ns, (a, b) => a.Id.CompareTo(b.Id));
        Array.Sort(cs, (a, b) => a.Id.CompareTo(b.Id));
        ClutchComponents = Enumerable.Range(0, cs.Length).Where(i => cs[i].Kind is ComponentKind.Clutch or ComponentKind.HydraulicClutch).ToArray();
        GearComponents = Enumerable.Range(0, cs.Length).Where(i => cs[i].Kind is ComponentKind.IdealGear or ComponentKind.PlanetaryGear).ToArray();
        ConverterComponents = Enumerable.Range(0, cs.Length).Where(i => cs[i].Kind == ComponentKind.TorqueConverter).ToArray();
        Require(ConverterComponents.Length <= MaxConverters, DiagnosticCode.Capacity, 0, "converters", "At most eight torque converters are supported per model.");
        Converters = new TorqueConverter?[cs.Length];
        PumpComponents = Enumerable.Range(0, cs.Length).Where(i => cs[i].Kind == ComponentKind.HydraulicPump).ToArray();
        HydraulicComponents = Enumerable.Range(0, cs.Length).Where(i => cs[i].Kind is ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicRelief).ToArray();
        HydraulicLaws = new HydraulicRestriction?[cs.Length]; HydraulicActuators = new HydraulicActuator?[cs.Length];
        GasCylinders = new GasCylinderPhysics?[cs.Length];
        GasCylinderByNode = new int[ns.Length]; Array.Fill(GasCylinderByNode, -1);
        for (int i = 0; i < cs.Length; ++i)
        {
            var c = cs[i];
            Require((c.Kind == ComponentKind.GasCylinder) == (c.MovingCylinder is not null),
                DiagnosticCode.Schema, c.Id, "moving_cylinder", "Moving-cylinder geometry is required only for gas cylinders.");
            if (c.Kind != ComponentKind.GasCylinder) continue;
            int a = Array.FindIndex(ns, n => n.Id == c.NodeA), b = Array.FindIndex(ns, n => n.Id == c.NodeB);
            Require(a >= 0 && ns[a].Domain == Domain.Rotational, DiagnosticCode.Connection, c.Id, "node_a", "A gas cylinder requires a rotational crank.");
            Require(b >= 0 && ns[b].Domain == Domain.Gas && GasCylinderByNode[b] < 0,
                DiagnosticCode.Connection, c.Id, "node_b", "A gas cylinder requires a gas chamber with exactly one moving-volume owner.");
            Require(ns[b].Storage == default, DiagnosticCode.Schema, ns[b].Id, "storage", "A moving chamber omits storage; its geometry supplies the volume.");
            GasCylinderByNode[b] = i; GasCylinders[i] = new(c.MovingCylinder!, c.Id);
        }
        HasMovingGas = GasCylinders.Any(c => c is not null);
        Nodes = new GraphNode[ns.Length];
        NodeGases = new IdealGas?[ns.Length]; NodeMixtures = new PremixedGas?[ns.Length];
        int dynamics = 0, thermal = 0, gas = 0, hydraulic = 0;
        for (int i = 0; i < ns.Length; ++i)
        {
            var n = ns[i];
            Require(n.Id != 0 && ids.Add(n.Id), DiagnosticCode.Id, n.Id, "id", "IDs must be nonzero and globally unique.");
            Require(n.Domain is Domain.Rotational or Domain.Thermal or Domain.Gas or Domain.Hydraulic,
                DiagnosticCode.Schema, n.Id, "domain", "Unknown domain.");
            Require((n.Domain == Domain.Gas) == (n.Gas is not null), DiagnosticCode.Schema, n.Id, "gas",
                "Gas composition is required only for gas volumes.");
            bool rotor = n.Domain == Domain.Rotational, volume = n.Domain == Domain.Gas, liquid = n.Domain == Domain.Hydraulic;
            Unit storageUnit = rotor ? Unit.KilogramMeterSquared : volume ? Unit.CubicMeter : liquid ? Unit.CubicMeterPerPascal : Unit.JoulePerKelvin;
            Unit initialUnit = rotor ? Unit.RadianPerSecond : liquid ? Unit.Pascal : Unit.Kelvin;
            Unit positionUnit = rotor ? Unit.Radian : volume ? Unit.Pascal : Unit.None;
            double storage;
            int moving = GasCylinderByNode[i];
            if (moving >= 0)
            {
                var crank = ns[Array.FindIndex(ns, node => node.Id == cs[moving].NodeA)];
                storage = GasCylinders[moving]!.GeometryAt(Convert(crank.Position, Unit.Radian, crank.Id, "position")).VolumeCubicMeters;
            }
            else storage = Convert(n.Storage, storageUnit, n.Id, "storage");
            double initial = Convert(n.Initial, initialUnit, n.Id, "initial");
            double position = Convert(n.Position, positionUnit, n.Id, "position");
            Require(storage > 0, DiagnosticCode.Range, n.Id, "storage", "Storage must be positive.");
            Require(rotor || (liquid ? initial >= 0 : initial > 0), DiagnosticCode.Range, n.Id, "initial", "Absolute temperature must be positive; hydraulic gauge pressure must be nonnegative.");
            if (volume)
            {
                Require(position > 0, DiagnosticCode.Range, n.Id, "position", "Absolute pressure must be positive.");
                double constant = Convert(n.Gas!.GasConstant, Unit.JoulePerKilogramKelvin, n.Id, "gas.gas_constant");
                try { NodeGases[i] = new(constant, n.Gas.Gamma); }
                catch (ArgumentException error) { throw new ModelCompileException(DiagnosticCode.Range, n.Id, "gas", error.Message); }
                if (n.Gas.Premixed is { } premixed) NodeMixtures[i] = new(premixed, n.Id);
                double mass = position * storage / (constant * initial);
                Require(Numeric.Finite(mass) && mass > 0 && Numeric.Finite(NodeGases[i]!.InternalEnergy(mass, initial)),
                    DiagnosticCode.Range, n.Id, "gas", "Initial gas mass or internal energy exceeds binary64.");
            }
            Nodes[i] = new(n.Id, n.Domain, rotor ? dynamics : volume ? gas : liquid ? hydraulic : thermal, storage, initial, position);
            if (rotor) dynamics += 2; else if (volume) ++gas; else if (liquid) ++hydraulic; else ++thermal;
        }
        HydraulicCount = hydraulic;
        GasCount = gas; HasPremixedGas = NodeMixtures.Any(m => m is not null);
        BurnerByGas = new int[gas]; Array.Fill(BurnerByGas, -1);
        int FindNode(uint id) => Array.FindIndex(Nodes, n => n.Id == id);
        Components = new GraphComponent[cs.Length];
        Cylinders = new CylinderPhysics?[cs.Length];
        Valves = new TimedValve?[cs.Length]; Burners = new Burner?[cs.Length];
        ReservoirFractions = new MassFractions?[cs.Length];
        InputMinimum = new double[cs.Length]; InputMaximum = new double[cs.Length];
        Array.Fill(InputMinimum, double.NegativeInfinity); Array.Fill(InputMaximum, double.PositiveInfinity);
        for (int i = 0; i < cs.Length; ++i)
        {
            var c = cs[i];
            Require(c.Id != 0 && ids.Add(c.Id), DiagnosticCode.Id, c.Id, "id", "IDs must be nonzero and globally unique.");
            Require(c.Kind is >= ComponentKind.Shaft and <= ComponentKind.HydraulicRelief,
                DiagnosticCode.Schema, c.Id, "kind", "Unknown component kind.");
            Require((c.Kind == ComponentKind.SealedCylinder) == (c.Cylinder is not null),
                DiagnosticCode.Schema, c.Id, "cylinder", "Cylinder parameters are required only for sealed cylinders.");
            bool combustion = c.Kind == ComponentKind.PremixedCombustion;
            bool pump = c.Kind == ComponentKind.HydraulicPump, relief = c.Kind == ComponentKind.HydraulicRelief;
            Require(pump == (c.HydraulicPump is not null), DiagnosticCode.Schema, c.Id, "hydraulic_pump", "Displacement and inlet apply only to hydraulic pumps.");
            bool pressureClutch = c.Kind == ComponentKind.HydraulicClutch;
            bool clutch = c.Kind == ComponentKind.Clutch || pressureClutch;
            bool liquid = c.Kind is ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicRelief;
            Require(liquid == (c.HydraulicRestriction is not null), DiagnosticCode.Schema, c.Id, "hydraulic_restriction", "Flow parameters apply only to hydraulic restrictions.");
            Require(pressureClutch == (c.HydraulicClutch is not null), DiagnosticCode.Schema, c.Id, "hydraulic_clutch", "Actuator geometry applies only to hydraulic clutches.");
            bool gear = c.Kind is ComponentKind.IdealGear or ComponentKind.PlanetaryGear;
            bool converter = c.Kind == ComponentKind.TorqueConverter;
            Require(converter == (c.Converter is not null), DiagnosticCode.Schema, c.Id, "converter", "Converter maps are required only for torque-converter components.");
            int carrier = FindNode(c.NodeC);
            Require(c.Kind == ComponentKind.PlanetaryGear ? carrier >= 0 && Nodes[carrier].Domain == Domain.Rotational &&
                c.NodeC != c.NodeA && c.NodeC != c.NodeB : c.NodeC == 0,
                DiagnosticCode.Connection, c.Id, "node_c", "Only a planetary gear requires a distinct rotational carrier node.");
            Require((c.Kind == ComponentKind.Clutch) == (c.Friction is not null), DiagnosticCode.Schema, c.Id, "friction", "Friction capacities are required only for clutch components.");
            Require(combustion == (c.Combustion is not null), DiagnosticCode.Schema, c.Id, "combustion", "Burn parameters apply only to premixed combustion components.");
            bool orifice = c.Kind == ComponentKind.GasOrifice, wall = c.Kind == ComponentKind.GasHeatLink, moving = c.Kind == ComponentKind.GasCylinder;
            Require(c.ValveTiming is null || orifice, DiagnosticCode.Schema, c.Id, "valve_timing", "Valve timing applies only to gas orifices.");
            if (c.ValveTiming is { } timing)
            {
                int crank = FindNode(timing.CrankNode);
                Require(crank >= 0 && Nodes[crank].Domain == Domain.Rotational, DiagnosticCode.Connection, c.Id, "valve_timing.crank_node", "Valve timing requires a rotational crank node.");
                double cycle = Convert(timing.CycleAngle, Unit.Radian, c.Id, "valve_timing.cycle_angle"),
                    open = Convert(timing.OpenAngle, Unit.Radian, c.Id, "valve_timing.open_angle"),
                    duration = Convert(timing.DurationAngle, Unit.Radian, c.Id, "valve_timing.duration_angle");
                try { Valves[i] = new(Nodes[crank].Index, timing.CrankNode, new(cycle, open, duration)); }
                catch (ArgumentException error) { throw new ModelCompileException(DiagnosticCode.Range, c.Id, "valve_timing", error.Message); }
            }
            if (pump)
            {
                carrier = FindNode(c.HydraulicPump!.InletNode);
                Require(c.HydraulicPump.InletNode == 0 || carrier >= 0 && Nodes[carrier].Domain == Domain.Hydraulic && c.HydraulicPump.InletNode != c.NodeB,
                    DiagnosticCode.Connection, c.Id, "hydraulic_pump.inlet_node", "Use a distinct hydraulic inlet node or zero for a pressure reservoir.");
            }
            bool pair = c.Kind is ComponentKind.Shaft or ComponentKind.ThermalLink || orifice || clutch || gear || converter || liquid;
            bool input = c.Kind is ComponentKind.DcMotor or ComponentKind.TorqueSource;
            Domain domain = c.Kind == ComponentKind.ThermalLink ? Domain.Thermal
                : orifice || wall ? Domain.Gas : liquid ? Domain.Hydraulic : Domain.Rotational;
            int a = FindNode(c.NodeA), b = FindNode(c.NodeB), heat = FindNode(c.HeatNode);
            Require(a >= 0 && Nodes[a].Domain == domain, DiagnosticCode.Connection, c.Id, "node_a", "Missing node or wrong domain.");
            Require(pump ? b >= 0 && Nodes[b].Domain == Domain.Hydraulic : moving || combustion ? b >= 0 && Nodes[b].Domain == Domain.Gas
                         : gear || converter ? b >= 0 && b != a && Nodes[b].Domain == Domain.Rotational
                         : wall ? b >= 0 && Nodes[b].Domain == Domain.Thermal
                         : c.NodeB == 0 || (pair && b >= 0 && b != a && Nodes[b].Domain == domain),
                DiagnosticCode.Connection, c.Id, "node_b",
                wall ? "A gas heat link requires a thermal node." : "Expected a distinct node of the matching domain.");
            Require(c.HeatNode == 0 || (c.Kind is ComponentKind.Shaft or ComponentKind.DcMotor or ComponentKind.Clutch or ComponentKind.TorqueConverter or ComponentKind.HydraulicClutch or ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicRelief &&
                    heat >= 0 && Nodes[heat].Domain == Domain.Thermal),
                DiagnosticCode.Connection, c.Id, "heat_node", "Loss sinks must be thermal nodes.");
            bool channelled = input || ((orifice || combustion || (clutch && !pressureClutch) || (liquid && !relief)) && c.InputChannel != 0);
            Require(channelled ? c.InputChannel > 0 && c.InputChannel < 0x8000000000000000UL && !InputIndices.ContainsKey(c.InputChannel)
                               : c.InputChannel == 0, DiagnosticCode.Channel, c.Id, "input_channel", "Invalid or duplicate input channel.");
            double initial = Convert(c.InitialInput, input ? (c.Kind == ComponentKind.DcMotor ? Unit.Volt : Unit.NewtonMeter)
                : orifice || combustion || (clutch && !pressureClutch) || (liquid && !relief) ? Unit.Fraction : Unit.None, c.Id, "initial_input");
            Require(c.DischargeCoefficient == 1 || orifice, DiagnosticCode.Schema, c.Id, "discharge_coefficient",
                "A discharge coefficient applies only to a gas orifice.");
            bool reservoirMixture = orifice && b < 0 && NodeMixtures[a] is not null;
            Require(reservoirMixture == (c.ReservoirFractions is not null), DiagnosticCode.Schema, c.Id, "reservoir_fractions",
                "Explicit reservoir fractions are required only for a premixed reservoir boundary.");
            if (reservoirMixture) ReservoirFractions[i] = PremixedGas.ValidateFractions(c.ReservoirFractions, c.Id, "reservoir_fractions");
            double p0 = 0, p1 = 0, p2 = 0, p3 = 0;
            int state = -1;
            switch (c.Kind)
            {
                case ComponentKind.HydraulicPump:
                    p0 = Convert(c.HydraulicPump!.Displacement, Unit.CubicMeterPerRadian, c.Id, "hydraulic_pump.displacement");
                    p2 = Convert(c.ReservoirPressure, carrier < 0 ? Unit.Pascal : Unit.None, c.Id, "reservoir_pressure");
                    Require(p0 > 0 && p2 >= 0, DiagnosticCode.Range, c.Id, "hydraulic_pump", "Use positive displacement and nonnegative reservoir gauge pressure.");
                    Require(c.Ratio == 1 && c.Area == default && c.Stiffness == default && c.Damping == default && c.RestAngle == default &&
                        c.Resistance == default && c.Inductance == default && c.Coupling == default && c.InitialCurrent == default && c.Conductance == default && c.AmbientTemperature == default,
                        DiagnosticCode.Schema, c.Id, "parameters", "An ideal pump accepts only displacement, shaft/outlet/inlet ports and reservoir pressure.");
                    state = Array.IndexOf(PumpComponents, i);
                    break;
                case ComponentKind.HydraulicRelief:
                case ComponentKind.HydraulicResistance:
                case ComponentKind.HydraulicOrifice:
                    bool turbulent = c.Kind == ComponentKind.HydraulicOrifice;
                    p0 = Convert(c.HydraulicRestriction!.Coefficient, turbulent ? Unit.CubicMeterPerSecondSqrtPascal : Unit.CubicMeterPerSecondPascal, c.Id, "hydraulic_restriction.coefficient");
                    p1 = Convert(c.HydraulicRestriction.TransitionPressure, turbulent ? Unit.Pascal : Unit.None, c.Id, "hydraulic_restriction.transition_pressure");
                    p2 = Convert(c.ReservoirPressure, b < 0 ? Unit.Pascal : Unit.None, c.Id, "reservoir_pressure");
                    p3 = Convert(c.HydraulicRestriction.CrackingPressure, relief ? Unit.Pascal : Unit.None, c.Id, "hydraulic_restriction.cracking_pressure");
                    Require(p3 >= 0 && p0 >= 0 && (!turbulent || p1 > 0) && p2 >= 0, DiagnosticCode.Range, c.Id, "hydraulic_restriction", "Use nonnegative conductance/flow coefficient and reservoir gauge pressure; turbulent transition pressure must be positive.");
                    Require(initial is >= 0 and <= 1, DiagnosticCode.Range, c.Id, "initial_input", "Valve opening must be in [0,1].");
                    Require(c.Ratio == 1 && c.Area == default && c.Stiffness == default && c.Damping == default && c.RestAngle == default &&
                        c.Resistance == default && c.Inductance == default && c.Coupling == default && c.InitialCurrent == default && c.Conductance == default && c.AmbientTemperature == default,
                        DiagnosticCode.Schema, c.Id, "parameters", "Hydraulic restrictions accept only flow parameters, pressure ports, opening and a thermal sink.");
                    HydraulicLaws[i] = new(p0, p1, relief ? p3 : null); state = Array.IndexOf(HydraulicComponents, i);
                    break;
                case ComponentKind.HydraulicClutch:
                    int pressure = FindNode(c.HydraulicClutch!.PressureNode);
                    Require(pressure >= 0 && Nodes[pressure].Domain == Domain.Hydraulic, DiagnosticCode.Connection, c.Id, "hydraulic_clutch.pressure_node", "A pressure clutch requires a hydraulic compliance node.");
                    HydraulicActuators[i] = new(c.HydraulicClutch, Nodes[pressure].Index, c.Id);
                    p3 = c.Ratio;
                    Require(Numeric.Finite(p3) && p3 != 0 && (b >= 0 || p3 == 1), DiagnosticCode.Range, c.Id, "ratio", "Use a finite nonzero ratio, or one for a ground brake.");
                    Require(c.Area == default && c.Stiffness == default && c.Damping == default && c.RestAngle == default &&
                        c.Resistance == default && c.Inductance == default && c.Coupling == default && c.InitialCurrent == default && c.Conductance == default && c.AmbientTemperature == default && c.ReservoirPressure == default,
                        DiagnosticCode.Schema, c.Id, "parameters", "A pressure clutch accepts only its actuator geometry, rotational ports, ratio and thermal sink.");
                    state = Array.IndexOf(ClutchComponents, i);
                    break;
                case ComponentKind.TorqueConverter:
                    Require(c.Ratio == 1 && c.Stiffness == default && c.Damping == default && c.RestAngle == default &&
                        c.Resistance == default && c.Inductance == default && c.Coupling == default && c.InitialCurrent == default &&
                        c.Conductance == default && c.AmbientTemperature == default && c.Area == default && c.ReservoirPressure == default,
                        DiagnosticCode.Schema, c.Id, "parameters", "A torque converter accepts only its maps, pump/turbine ports and optional thermal sink.");
                    Converters[i] = new(c.Converter!, c.Id); state = Array.IndexOf(ConverterComponents, i);
                    break;
                case ComponentKind.IdealGear:
                case ComponentKind.PlanetaryGear:
                    Require(c.Stiffness == default && c.Damping == default && c.RestAngle == default &&
                        c.Resistance == default && c.Inductance == default && c.Coupling == default && c.InitialCurrent == default &&
                        c.Conductance == default && c.AmbientTemperature == default && c.Area == default && c.ReservoirPressure == default,
                        DiagnosticCode.Schema, c.Id, "parameters", "Ideal gears accept only their ratio and rotational ports; omit unrelated physical parameters.");
                    p3 = c.Ratio;
                    Require(Numeric.Finite(p3) && (c.Kind == ComponentKind.IdealGear ? p3 != 0 : p3 > 1),
                        DiagnosticCode.Range, c.Id, "ratio", "An ideal gear needs a finite nonzero signed ratio; a planetary ring/sun ratio must exceed one.");
                    state = Array.IndexOf(GearComponents, i);
                    break;
                case ComponentKind.Clutch:
                    p0 = Convert(c.Friction!.StaticCapacity, Unit.NewtonMeter, c.Id, "friction.static_capacity");
                    p1 = Convert(c.Friction.SlidingCapacity, Unit.NewtonMeter, c.Id, "friction.sliding_capacity");
                    p3 = c.Ratio;
                    Require(p0 >= p1 && p1 >= 0, DiagnosticCode.Range, c.Id, "friction", "Require static capacity >= sliding capacity >= 0.");
                    Require(Numeric.Finite(p3) && p3 != 0 && (b >= 0 || p3 == 1), DiagnosticCode.Range, c.Id, "ratio", "Use a finite nonzero ratio, or one for a ground brake.");
                    Require(initial is >= 0 and <= 1, DiagnosticCode.Range, c.Id, "initial_input", "Engagement must be a fraction in [0,1].");
                    state = Array.IndexOf(ClutchComponents, i);
                    break;
                case ComponentKind.PremixedCombustion:
                    Require(NodeMixtures[b] is not null, DiagnosticCode.Connection, c.Id, "node_b", "Combustion requires a gas node with explicit premixed composition.");
                    Require(BurnerByGas[Nodes[b].Index] < 0, DiagnosticCode.Connection, c.Id, "node_b", "A gas chamber has at most one combustion component.");
                    int cylinderIndex = GasCylinderByNode[b];
                    Require(cylinderIndex < 0 || cs[cylinderIndex].NodeA == c.NodeA, DiagnosticCode.Connection, c.Id, "node_a", "Combustion must use the moving chamber's own crank.");
                    var burn = c.Combustion!;
                    double cycle = Convert(burn.CycleAngle, Unit.Radian, c.Id, "combustion.cycle_angle"),
                        start = Convert(burn.StartAngle, Unit.Radian, c.Id, "combustion.start_angle"),
                        duration = Convert(burn.DurationAngle, Unit.Radian, c.Id, "combustion.duration_angle");
                    try { Burners[i] = new(Nodes[a].Index, Nodes[b].Index, new(cycle, start, duration, burn.ShapeExponent, burn.BurnCoefficient)); }
                    catch (ArgumentException error) { throw new ModelCompileException(DiagnosticCode.Range, c.Id, "combustion", error.Message); }
                    BurnerByGas[Nodes[b].Index] = i;
                    Require(initial is >= 0 and <= 1, DiagnosticCode.Range, c.Id, "initial_input", "Burn multiplier must be in [0,1].");
                    break;
                case ComponentKind.SealedCylinder:
                    Cylinders[i] = new(c.Cylinder!, c.Id, Nodes[a].Position);
                    break;
                case ComponentKind.Shaft:
                    p0 = Convert(c.Stiffness, Unit.NewtonMeterPerRadian, c.Id, "stiffness");
                    p1 = Convert(c.Damping, Unit.NewtonMeterSecondPerRadian, c.Id, "damping");
                    p2 = Convert(c.RestAngle, Unit.Radian, c.Id, "rest_angle");
                    p3 = c.Ratio;
                    Require(p0 >= 0 && p1 >= 0 && Numeric.Finite(p3) && p3 != 0 && (b >= 0 || p3 == 1),
                        DiagnosticCode.Range, c.Id, "shaft", "Invalid stiffness, damping, or ratio.");
                    break;
                case ComponentKind.DcMotor:
                    p0 = Convert(c.Resistance, Unit.Ohm, c.Id, "resistance");
                    p1 = Convert(c.Inductance, Unit.Henry, c.Id, "inductance");
                    p2 = Convert(c.Coupling, Unit.NewtonMeterPerAmpere, c.Id, "coupling");
                    p3 = Convert(c.InitialCurrent, Unit.Ampere, c.Id, "initial_current");
                    Require(p0 >= 0 && p1 > 0, DiagnosticCode.Range, c.Id, "motor", "R must be nonnegative and L positive.");
                    state = dynamics++;
                    break;
                case ComponentKind.ThermalLink:
                    p0 = Convert(c.Conductance, Unit.WattPerKelvin, c.Id, "conductance");
                    p1 = Convert(c.AmbientTemperature, b < 0 ? Unit.Kelvin : Unit.None, c.Id, "ambient_temperature");
                    Require(p0 >= 0 && (b >= 0 || p1 > 0), DiagnosticCode.Range, c.Id, "thermal", "Invalid conductance or absolute temperature.");
                    break;
                case ComponentKind.GasOrifice:
                    p0 = Convert(c.Area, Unit.SquareMeter, c.Id, "area");
                    p1 = c.DischargeCoefficient;
                    p2 = Convert(c.ReservoirPressure, b < 0 ? Unit.Pascal : Unit.None, c.Id, "reservoir_pressure");
                    p3 = Convert(c.AmbientTemperature, b < 0 ? Unit.Kelvin : Unit.None, c.Id, "reservoir_temperature");
                    Require(initial is >= 0 and <= 1, DiagnosticCode.Range, c.Id, "initial_input", "Opening must be a fraction in [0, 1].");
                    Require(b >= 0 || (p2 > 0 && p3 > 0), DiagnosticCode.Range, c.Id, "reservoir",
                        "A reservoir orifice needs a positive absolute pressure and temperature.");
                    try { _ = new Orifice(p0, p1); }
                    catch (ArgumentException error) { throw new ModelCompileException(DiagnosticCode.Range, c.Id, "orifice", error.Message); }
                    Require(b < 0 || SameGas(NodeGases[a]!, NodeGases[b]!), DiagnosticCode.Connection, c.Id, "node_b",
                        "Connected gas volumes must share one gas constant and gamma.");
                    Require(b < 0 || (NodeMixtures[a] is null ? NodeMixtures[b] is null : NodeMixtures[a]!.Compatible(NodeMixtures[b])),
                        DiagnosticCode.Connection, c.Id, "node_b", "Connected gases must share premixed tracking, heating value and stoichiometric ratio.");
                    break;
                case ComponentKind.GasHeatLink:
                    p0 = Convert(c.Conductance, Unit.WattPerKelvin, c.Id, "conductance");
                    Require(p0 >= 0, DiagnosticCode.Range, c.Id, "conductance", "Conductance must be nonnegative.");
                    break;
            }
            if (orifice || combustion || clutch || liquid) { InputMinimum[i] = 0; InputMaximum[i] = 1; }
            if (channelled) InputIndices.Add(c.InputChannel, i);
            Components[i] = new(c.Id, c.Kind, a, b, heat, state, c.InputChannel, initial, p0, p1, p2, p3, carrier);
        }
        Require(dynamics + thermal + 2 * gas + 3 * NodeMixtures.Count(m => m is not null) + Burners.Count(b => b is not null) + ClutchComponents.Length + GearComponents.Length + 4 * ConverterComponents.Length + hydraulic + 3 * HydraulicComponents.Length + 4 * PumpComponents.Length <= MaxStates, DiagnosticCode.Capacity, 0, "states", "State capacity exceeded.");
        HasValveTiming = Valves.Any(v => v is not null);
        DynamicCount = dynamics; ThermalCount = thermal;
        double[,] matrix = new double[dynamics, dynamics], heatMatrix = new double[thermal, thermal];
        if (HasClutches) HeatConductance = new double[thermal, thermal];
        ConstantForce = new double[dynamics]; AmbientForce = new double[thermal];
        var channels = new List<ChannelInfo>();
        var outputs = new List<OutputBinding>();
        void Output(uint id, Field field, int index, Unit unit, string quantity, bool component = false)
        {
            channels.Add(new(Core.Channels.Output(id, field), id, false, unit, quantity));
            outputs.Add(new(id, field, index, component));
        }
        for (int i = 0; i < Nodes.Length; ++i)
        {
            var n = Nodes[i];
            if (n.Domain == Domain.Rotational)
            {
                matrix[n.Index, n.Index + 1] = 1;
                Output(n.Id, Field.Angle, i, Unit.Radian, "angle");
                Output(n.Id, Field.Speed, i, Unit.RadianPerSecond, "speed");
            }
            else if (n.Domain == Domain.Hydraulic)
            {
                Output(n.Id, Field.Pressure, i, Unit.Pascal, "hydraulic_gauge_pressure");
                Output(n.Id, Field.Volume, i, Unit.CubicMeter, "stored_reference_volume");
                Output(n.Id, Field.InternalEnergy, i, Unit.Joule, "hydraulic_elastic_energy");
            }
            else if (n.Domain == Domain.Gas)
            {
                Output(n.Id, Field.Pressure, i, Unit.Pascal, "gas_pressure");
                Output(n.Id, Field.Temperature, i, Unit.Kelvin, "gas_temperature");
                Output(n.Id, Field.Mass, i, Unit.Kilogram, "gas_mass");
                Output(n.Id, Field.InternalEnergy, i, Unit.Joule, "gas_internal_energy");
                if (NodeMixtures[i] is not null)
                {
                    Output(n.Id, Field.FuelMass, i, Unit.Kilogram, "unburned_fuel_mass");
                    Output(n.Id, Field.FreshAirMass, i, Unit.Kilogram, "fresh_air_mass");
                    Output(n.Id, Field.ProductMass, i, Unit.Kilogram, "product_mass");
                    Output(n.Id, Field.ChemicalEnergy, i, Unit.Joule, "chemical_energy");
                }
            }
            else
            {
                heatMatrix[n.Index, n.Index] = n.Storage;
                Output(n.Id, Field.Temperature, i, Unit.Kelvin, "temperature");
            }
        }
        for (int i = 0; i < Components.Length; ++i)
        {
            var c = Components[i]; var na = Nodes[c.A];
            int a = na.Index, b = c.B < 0 ? -1 : Nodes[c.B].Index;
            if (c.Kind == ComponentKind.Shaft)
            {
                int count = b < 0 ? 1 : 2;
                for (int row = 0; row < count; ++row)
                {
                    double directionRow = row == 0 ? 1 : -c.P3;
                    int velocity = (row == 0 ? a : b) + 1;
                    double inertia = Nodes[row == 0 ? c.A : c.B].Storage;
                    ConstantForce[velocity] += directionRow * c.P0 * c.P2 / inertia;
                    for (int col = 0; col < count; ++col)
                    {
                        double factor = directionRow * (col == 0 ? 1 : -c.P3) / inertia;
                        int column = col == 0 ? a : b;
                        matrix[velocity, column] -= factor * c.P0;
                        matrix[velocity, column + 1] -= factor * c.P1;
                    }
                }
                Output(c.Id, Field.Twist, i, Unit.Radian, "twist", true);
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "reaction_torque_at_a", true);
            }
            else if (c.Kind == ComponentKind.DcMotor)
            {
                matrix[a + 1, c.Index] += c.P2 / na.Storage;
                matrix[c.Index, a + 1] -= c.P2 / c.P1;
                matrix[c.Index, c.Index] -= c.P0 / c.P1;
                channels.Add(new(c.InputChannel, c.Id, true, Unit.Volt, "voltage"));
                Output(c.Id, Field.Current, i, Unit.Ampere, "current", true);
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "motor_torque", true);
            }
            else if (c.Kind == ComponentKind.TorqueSource)
                channels.Add(new(c.InputChannel, c.Id, true, Unit.NewtonMeter, "torque"));
            else if (c.Kind is ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicRelief)
            {
                if (c.InputChannel != 0) channels.Add(new(c.InputChannel, c.Id, true, Unit.Fraction, "opening"));
                Output(c.Id, Field.VolumeFlow, i, Unit.CubicMeterPerSecond, "last_tick_mean_volume_flow_a_to_b", true);
                Output(c.Id, Field.HeatFlow, i, Unit.Watt, "last_tick_mean_hydraulic_loss", true);
                Output(c.Id, Field.FluidHeat, i, Unit.Joule, "cumulative_hydraulic_loss", true);
            }
            else if (c.Kind == ComponentKind.HydraulicPump)
            {
                Output(c.Id, Field.VolumeFlow, i, Unit.CubicMeterPerSecond, "last_tick_mean_volume_flow_inlet_to_outlet", true);
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "last_tick_mean_pump_shaft_reaction", true);
                Output(c.Id, Field.HydraulicPower, i, Unit.Watt, "last_tick_mean_shaft_to_fluid_power", true);
                Output(c.Id, Field.HydraulicWork, i, Unit.Joule, "cumulative_shaft_to_fluid_work", true);
            }
            else if (c.Kind == ComponentKind.TorqueConverter)
            {
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "last_tick_mean_pump_torque", true);
                Output(c.Id, Field.TorqueAtB, i, Unit.NewtonMeter, "last_tick_mean_turbine_torque", true);
                Output(c.Id, Field.TorqueAtC, i, Unit.NewtonMeter, "last_tick_mean_stator_torque", true);
                Output(c.Id, Field.HeatFlow, i, Unit.Watt, "last_tick_mean_fluid_heat_power", true);
                Output(c.Id, Field.FluidHeat, i, Unit.Joule, "cumulative_fluid_heat", true);
                Output(c.Id, Field.SpeedRatio, i, Unit.Fraction, "follower_to_driver_speed_ratio", true);
                Output(c.Id, Field.ConverterDrive, i, Unit.StateCode, "converter_drive", true);
            }
            else if (c.Kind is ComponentKind.IdealGear or ComponentKind.PlanetaryGear)
            {
                Output(c.Id, Field.SlipSpeed, i, Unit.RadianPerSecond, "gear_constraint_speed_error", true);
                Output(c.Id, Field.ConstraintError, i, Unit.Radian, "gear_phase_error_from_initial", true);
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "last_tick_mean_torque_at_a", true);
                Output(c.Id, Field.TorqueAtB, i, Unit.NewtonMeter, "last_tick_mean_torque_at_b", true);
                if (c.C >= 0) Output(c.Id, Field.TorqueAtC, i, Unit.NewtonMeter, "last_tick_mean_torque_at_c", true);
            }
            else if (c.Kind is ComponentKind.Clutch or ComponentKind.HydraulicClutch)
            {
                if (c.InputChannel != 0) channels.Add(new(c.InputChannel, c.Id, true, Unit.Fraction, "engagement"));
                Output(c.Id, Field.SlipSpeed, i, Unit.RadianPerSecond, "relative_slip_speed", true);
                Output(c.Id, Field.ClutchMode, i, Unit.StateCode, "clutch_mode", true);
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "last_tick_mean_torque_at_a", true);
                Output(c.Id, Field.HeatFlow, i, Unit.Watt, "last_tick_mean_friction_power", true);
                Output(c.Id, Field.FrictionHeat, i, Unit.Joule, "cumulative_friction_heat", true);
                if (c.Kind == ComponentKind.HydraulicClutch)
                {
                    Output(c.Id, Field.ClampForce, i, Unit.Newton, "current_clamp_force", true);
                    Output(c.Id, Field.StaticCapacity, i, Unit.NewtonMeter, "current_static_capacity", true);
                    Output(c.Id, Field.SlidingCapacity, i, Unit.NewtonMeter, "current_sliding_capacity", true);
                }
            }
            else if (c.Kind == ComponentKind.ThermalLink)
            {
                double gh = Dt * c.P0;
                heatMatrix[a, a] += gh;
                if (b < 0) AmbientForce[a] += gh * c.P1;
                else { heatMatrix[a, b] -= gh; heatMatrix[b, a] -= gh; heatMatrix[b, b] += gh; }
                if (HeatConductance is not null)
                {
                    HeatConductance[a, a] += c.P0;
                    if (b >= 0) { HeatConductance[a, b] -= c.P0; HeatConductance[b, a] -= c.P0; HeatConductance[b, b] += c.P0; }
                }
            }
            else if (c.Kind == ComponentKind.GasOrifice)
            {
                if (c.InputChannel != 0) channels.Add(new(c.InputChannel, c.Id, true, Unit.Fraction, Valves[i] is null ? "opening" : "peak_opening"));
                Output(c.Id, Field.MassFlow, i, Unit.KilogramPerSecond, "mass_flow_a_to_b", true);
                if (Valves[i] is not null) Output(c.Id, Field.Opening, i, Unit.Fraction, "effective_opening", true);
            }
            else if (c.Kind == ComponentKind.GasHeatLink)
                Output(c.Id, Field.HeatFlow, i, Unit.Watt, "heat_flow_gas_to_wall", true);
            else if (c.Kind == ComponentKind.PremixedCombustion)
            {
                if (c.InputChannel != 0) channels.Add(new(c.InputChannel, c.Id, true, Unit.Fraction, "burn_multiplier"));
                Output(c.Id, Field.FuelBurned, i, Unit.Kilogram, "fuel_burned", true);
                Output(c.Id, Field.HeatReleased, i, Unit.Joule, "heat_released", true);
                Output(c.Id, Field.BurnFrontier, i, Unit.Radian, "burn_frontier_angle", true);
            }
            else if (c.Kind == ComponentKind.GasCylinder)
            {
                Output(c.Id, Field.Volume, i, Unit.CubicMeter, "cylinder_volume", true);
                Output(c.Id, Field.PistonDisplacement, i, Unit.Meter, "piston_displacement_from_tdc", true);
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "gas_torque_at_crank", true);
            }
            else if (c.Kind == ComponentKind.SealedCylinder)
            {
                Output(c.Id, Field.Pressure, i, Unit.Pascal, "cylinder_pressure", true);
                Output(c.Id, Field.Temperature, i, Unit.Kelvin, "gas_temperature", true);
                Output(c.Id, Field.Volume, i, Unit.CubicMeter, "cylinder_volume", true);
                Output(c.Id, Field.Mass, i, Unit.Kilogram, "trapped_gas_mass", true);
                Output(c.Id, Field.InternalEnergy, i, Unit.Joule, "gas_internal_energy", true);
                Output(c.Id, Field.PistonDisplacement, i, Unit.Meter, "piston_displacement_from_tdc", true);
                Output(c.Id, Field.Torque, i, Unit.NewtonMeter, "gas_torque_at_crank", true);
            }
        }
        if (HasClutches) DynamicRates = (double[,])matrix.Clone();
        for (int row = 0; row < dynamics; ++row)
            for (int col = 0; col < dynamics; ++col)
                matrix[row, col] = (row == col ? 1 : 0) - 0.5 * Dt * matrix[row, col];
        foreach (Field field in new[] { Field.SourceWork, Field.HeatRejected, Field.StoredEnergyChange, Field.EnergyResidual })
            Output(0, field, -1, Unit.Joule, field.ToString());
        if (gas > 0)
        {
            Output(0, Field.ReservoirEnthalpy, -1, Unit.Joule, "reservoir_enthalpy");
            Output(0, Field.MassResidual, -1, Unit.Kilogram, "mass_residual");
        }
        if (HasPremixedGas)
        {
            Output(0, Field.ChemicalEnergy, -1, Unit.Joule, "chemical_energy");
            Output(0, Field.FuelEnergyIn, -1, Unit.Joule, "net_fuel_energy_in");
            Output(0, Field.FuelResidual, -1, Unit.Kilogram, "fuel_residual");
            Output(0, Field.FreshAirResidual, -1, Unit.Kilogram, "fresh_air_residual");
        }
        if (HasHydraulics)
        {
            Output(0, Field.HydraulicVolumeIn, -1, Unit.CubicMeter, "net_hydraulic_reference_volume_in");
            Output(0, Field.HydraulicVolumeResidual, -1, Unit.CubicMeter, "hydraulic_reference_volume_residual");
            Output(0, Field.HydraulicWork, -1, Unit.Joule, "hydraulic_boundary_work");
        }
        Outputs = outputs.ToArray();
        Channels = Array.AsReadOnly(channels.ToArray());
        Require(ConstantForce.All(Numeric.Finite) && AmbientForce.All(Numeric.Finite), DiagnosticCode.Solver,
            0, "forcing", "Compiled force overflows binary64.");
        Dynamics = new(matrix); Thermal = new(heatMatrix);
        if (HasGears) Gears = new(this);
        if (Cylinders.Any(c => c is not null) || HasMovingGas) CylinderCoupling = new(this);
        if (HasConverters || HasPumps) ConverterCoupling = new(this);
        if (GasCount > 0) Gas = new(this);
        Fingerprint = ComputeFingerprint();
        _ = CreateSimulation(); // Validate initial energy and derived observables before publishing.
    }

    private ulong ComputeFingerprint()
    {
        ulong h = Numeric.Hash(14695981039346656037UL, 1UL);
        h = Numeric.Hash(h, CylinderCoupling is null ? 2UL : 3UL); // Preserve existing linear asset fingerprints.
        if (GasCount > 0) h = Numeric.Hash(h, 4UL);
        if (HasMovingGas) h = Numeric.Hash(h, 5UL);
        if (HasValveTiming) h = Numeric.Hash(h, 6UL);
        if (HasPremixedGas) h = Numeric.Hash(h, 7UL);
        if (HasClutches) h = Numeric.Hash(h, 8UL);
        if (HasGears) h = Numeric.Hash(h, 9UL);
        if (HasConverters) h = Numeric.Hash(h, 10UL);
        if (HasHydraulics) h = Numeric.Hash(h, 11UL);
        if (HasPumps || Components.Any(c => c.Kind == ComponentKind.HydraulicRelief)) h = Numeric.Hash(h, 12UL);
        h = Numeric.Hash(h, StepNanoseconds);
        h = Numeric.Hash(h, (ulong)NodeCount); h = Numeric.Hash(h, (ulong)ComponentCount);
        foreach (var n in Nodes)
        {
            h = Numeric.Hash(h, (ulong)n.Id); h = Numeric.Hash(h, (ulong)n.Domain);
            h = Numeric.Hash(h, n.Storage); h = Numeric.Hash(h, n.Initial); h = Numeric.Hash(h, n.Position);
        }
        foreach (var composition in NodeGases)
            if (composition is not null)
            {
                h = Numeric.Hash(h, composition.GasConstantJoulePerKilogramKelvin);
                h = Numeric.Hash(h, composition.Gamma);
            }
        foreach (var c in Components)
        {
            h = Numeric.Hash(h, (ulong)c.Id); h = Numeric.Hash(h, (ulong)c.Kind);
            h = Numeric.Hash(h, unchecked((ulong)c.A)); h = Numeric.Hash(h, unchecked((ulong)c.B));
            h = Numeric.Hash(h, unchecked((ulong)c.Heat)); h = Numeric.Hash(h, c.InputChannel);
            h = Numeric.Hash(h, c.InitialInput); h = Numeric.Hash(h, c.P0); h = Numeric.Hash(h, c.P1);
            h = Numeric.Hash(h, c.P2); h = Numeric.Hash(h, c.P3);
            if (c.Kind is ComponentKind.IdealGear or ComponentKind.PlanetaryGear or ComponentKind.HydraulicPump) h = Numeric.Hash(h, unchecked((ulong)c.C));
        }
        foreach (var cylinder in Cylinders)
            if (cylinder is not null)
                foreach (double parameter in cylinder.Parameters) h = Numeric.Hash(h, parameter);
        foreach (var cylinder in GasCylinders)
            if (cylinder is not null)
                foreach (double parameter in cylinder.Parameters) h = Numeric.Hash(h, parameter);
        for (int i = 0; i < Valves.Length; ++i)
            if (Valves[i] is { } valve)
            {
                h = Numeric.Hash(h, (ulong)Components[i].Id); h = Numeric.Hash(h, (ulong)valve.CrankNode);
                h = Numeric.Hash(h, valve.Profile.CycleAngleRadians); h = Numeric.Hash(h, valve.Profile.OpenAngleRadians);
                h = Numeric.Hash(h, valve.Profile.DurationAngleRadians);
            }
        for (int i = 0; i < Nodes.Length; ++i)
            if (NodeMixtures[i] is { } mixture)
            {
                h = Numeric.Hash(h, (ulong)Nodes[i].Id); h = Numeric.Hash(h, mixture.Lhv); h = Numeric.Hash(h, mixture.AirFuelRatio);
                h = Numeric.Hash(h, mixture.Initial.Fuel); h = Numeric.Hash(h, mixture.Initial.FreshAir);
            }
        for (int i = 0; i < Components.Length; ++i)
        {
            if (ReservoirFractions[i] is { } fractions)
            {
                h = Numeric.Hash(h, (ulong)Components[i].Id); h = Numeric.Hash(h, fractions.Fuel); h = Numeric.Hash(h, fractions.FreshAir);
            }
            if (Burners[i] is { } burner)
            {
                h = Numeric.Hash(h, (ulong)Components[i].Id); var p = burner.Profile;
                h = Numeric.Hash(h, p.CycleAngleRadians); h = Numeric.Hash(h, p.StartAngleRadians); h = Numeric.Hash(h, p.DurationAngleRadians);
                h = Numeric.Hash(h, p.ShapeExponent); h = Numeric.Hash(h, p.BurnCoefficient);
            }
        }
        foreach (var converter in Converters) if (converter is not null) h = converter.Hash(h);
        foreach (var actuator in HydraulicActuators) if (actuator is not null)
        { h = Numeric.Hash(h, (ulong)actuator.Pressure); h = actuator.Hash(h); }
        return h;
    }
}
