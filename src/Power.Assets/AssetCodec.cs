// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.IO;
using System.Security.Cryptography;
using System.Text;
using Power.Core;

namespace Power.Assets;

public sealed class AssetFormatException(string message) : ArgumentException(message);

/// <summary>Versioned little-endian model/experiment data, never executable code or serialized solver factors.</summary>
public static class AssetCodec
{
    public const int FormatVersion = 27, MaxBytes = 1_048_576;
    public const string FormatName = "power.asset.v27";
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("POWERAST");
    private static readonly UTF8Encoding Utf8 = new(false, true);

    public static byte[] Encode(PowerAsset asset)
    {
        if (asset is null) throw new ArgumentNullException(nameof(asset));
        // Check the exact maximum allocation before writing caller-provided data.
        int cylinders = asset.Components.Count(c => c.Kind == ComponentKind.SealedCylinder);
        int gases = asset.Nodes.Count(n => n.Domain == Domain.Gas);
        int orifices = asset.Components.Count(c => c.Kind is ComponentKind.GasOrifice or ComponentKind.GasFuelInjector or ComponentKind.LiquidFuelInjector);
        int moving = asset.Components.Count(c => c.Kind == ComponentKind.GasCylinder);
        int valves = asset.Components.Count(c => c.ValveTiming is not null);
        int mixtures = asset.Nodes.Count(n => n.Gas?.Premixed is not null);
        int reservoirMixtures = asset.Components.Count(c => c.ReservoirFractions is not null);
        int burners = asset.Components.Count(c => c.Combustion is not null);
        int clutches = asset.Components.Count(c => c.Friction is not null);
        int gears = asset.Components.Count(c => c.Kind is ComponentKind.IdealGear or ComponentKind.PlanetaryGear or ComponentKind.DoublePinionPlanetaryGear or ComponentKind.CarrierGear);
        int converters = asset.Components.Count(c => c.Converter is not null);
        int points = asset.Components.Where(c => c.Converter is not null).Sum(c => c.Converter!.PumpPositive!.Points.Count +
            c.Converter.PumpNegative!.Points.Count + c.Converter.TurbinePositive!.Points.Count + c.Converter.TurbineNegative!.Points.Count);
        int hydraulicRestrictions = asset.Components.Count(c => c.HydraulicRestriction is not null);
        int hydraulicClutches = asset.Components.Count(c => c.HydraulicClutch is not null);
        int pumps = asset.Components.Count(c => c.HydraulicPump is not null);
        int reliefs = asset.Components.Count(c => c.Kind == ComponentKind.HydraulicRelief);
        int controllers = asset.Components.Count(c => c.PressureController is not null);
        int batteries = asset.Nodes.Count(n => n.Battery is not null), dutyControllers = asset.Components.Count(c => c.PressureDutyController is not null);
        int pistons = asset.Components.Count(c => c.HydraulicPiston is not null), pistonClutches = asset.Components.Count(c => c.PistonClutch is not null);
        int spools = asset.Components.Count(c => c.SpoolValve is not null), gasPistons = asset.Components.Count(c => c.GasPiston is not null);
        int injectors = asset.Components.Count(c => c.FuelInjector is not null),films=asset.Components.Count(c=>c.FuelFilm is not null), liquidInjectors=asset.Components.Count(c=>c.LiquidFuelInjector is not null);
        int solenoids = asset.Components.Count(c => c.Solenoid is not null), stops = asset.Components.Count(c => c.TravelStop is not null);
        int needles = asset.Components.Count(c => c.LiquidFuelInjector?.Needle is not null), needleDrivers = asset.Components.Count(c => c.NeedleDriver is not null);
        int dctControllers = asset.Components.Count(c => c.DualClutchController is not null);
        int atControllers = asset.Components.Count(c => c.HydraulicAtController is not null);
        int atRoutes = asset.Components.Sum(c => c.HydraulicAtController?.Routes.Count ?? 0);
        int liquidFeeds=asset.Components.Count(c=>c.LiquidRailFeed is not null);
        int liquidTanks=asset.Components.Count(c=>c.LiquidFuelTank is not null);
        long size = 8 + 4 + 8 + 8 + 8 + 8 + 2 + Utf8.GetByteCount(asset.Name) + 32 + 39 * 4 +
            44L * asset.Nodes.Count + 156L * asset.Components.Count + 116L * cylinders + 24L * gases + 36L * orifices +
            72L * moving + 44L * valves + 40L * mixtures + 20L * reservoirMixtures + 56L * burners + 28L * clutches + 8L * gears + 20L * converters + 28L * points + 40L * hydraulicRestrictions + 64L * hydraulicClutches + 32L * pumps + 16L * reliefs + 80L * controllers + 68L * batteries + 80L * dutyControllers + 104L * pistons + 40L * pistonClutches + 32L * spools + 56L * gasPistons + 56L * injectors + 64L * films + 120L * liquidInjectors + 28L * solenoids + 40L * stops + 32L * needles + 40L * needleDrivers + 104L * dctControllers + 208L * atControllers + 12L * atRoutes + 28L * liquidFeeds + 32L * liquidTanks + 24L * asset.Inputs.Count + 33L * asset.Checks.Count + 32;
        if (size > MaxBytes) throw new AssetFormatException("Asset exceeds 1 MiB.");
        using var stream = new MemoryStream((int)size);
        using var writer = new BinaryWriter(stream, Utf8, true);
        writer.Write(Magic); writer.Write(FormatVersion); writer.Write(asset.Model.Fingerprint);
        writer.Write(asset.Model.StepNanoseconds); writer.Write(asset.DurationNanoseconds); writer.Write(asset.SampleEveryNanoseconds);
        byte[] name = Utf8.GetBytes(asset.Name); writer.Write((ushort)name.Length); writer.Write(name);
        for (int i = 0; i < 32; ++i) writer.Write(Convert.ToByte(asset.SourceSha256.Substring(i * 2, 2), 16));
        writer.Write(asset.Nodes.Count); writer.Write(asset.Components.Count); writer.Write(asset.Inputs.Count); writer.Write(asset.Checks.Count);
        writer.Write(cylinders); writer.Write(gases); writer.Write(orifices); writer.Write(moving); writer.Write(valves);
        writer.Write(mixtures); writer.Write(reservoirMixtures); writer.Write(burners); writer.Write(clutches); writer.Write(gears); writer.Write(converters); writer.Write(points); writer.Write(hydraulicRestrictions); writer.Write(hydraulicClutches); writer.Write(pumps); writer.Write(reliefs); writer.Write(controllers); writer.Write(batteries); writer.Write(dutyControllers); writer.Write(pistons); writer.Write(pistonClutches); writer.Write(spools); writer.Write(gasPistons); writer.Write(injectors);writer.Write(films); writer.Write(liquidInjectors); writer.Write(solenoids); writer.Write(stops); writer.Write(needles); writer.Write(needleDrivers); writer.Write(dctControllers); writer.Write(atControllers); writer.Write(atRoutes); writer.Write(liquidFeeds);writer.Write(liquidTanks);
        void Quantity(Quantity quantity) { writer.Write(quantity.Value); writer.Write((int)quantity.Unit); }
        foreach (var n in asset.Nodes)
        {
            writer.Write(n.Id); writer.Write((int)n.Domain); Quantity(n.Storage); Quantity(n.Initial); Quantity(n.Position);
        }
        foreach (var c in asset.Components)
        {
            writer.Write(c.Id); writer.Write((int)c.Kind); writer.Write(c.NodeA); writer.Write(c.NodeB); writer.Write(c.HeatNode);
            writer.Write(c.InputChannel); Quantity(c.InitialInput); Quantity(c.Stiffness); Quantity(c.Damping); Quantity(c.RestAngle);
            writer.Write(c.Ratio); Quantity(c.Resistance); Quantity(c.Inductance); Quantity(c.Coupling); Quantity(c.InitialCurrent);
            Quantity(c.Conductance); Quantity(c.AmbientTemperature);
        }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].Cylinder is { } cylinder)
            {
                writer.Write(i);
                Quantity(cylinder.Bore); Quantity(cylinder.Stroke); Quantity(cylinder.RodLength); Quantity(cylinder.Phase);
                writer.Write(cylinder.CompressionRatio); Quantity(cylinder.InitialPressure); Quantity(cylinder.InitialTemperature);
                Quantity(cylinder.GasConstant); writer.Write(cylinder.Gamma); Quantity(cylinder.BackPressure);
            }
        for (int i = 0; i < asset.Nodes.Count; ++i)
            if (asset.Nodes[i].Gas is { } gas)
            {
                writer.Write(i); Quantity(gas.GasConstant); writer.Write(gas.Gamma);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].Kind is ComponentKind.GasOrifice or ComponentKind.GasFuelInjector or ComponentKind.LiquidFuelInjector)
            {
                var c = asset.Components[i];
                writer.Write(i); Quantity(c.Area); writer.Write(c.DischargeCoefficient); Quantity(c.ReservoirPressure);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].MovingCylinder is { } cylinder)
            {
                writer.Write(i); Quantity(cylinder.Bore); Quantity(cylinder.Stroke); Quantity(cylinder.RodLength); Quantity(cylinder.Phase);
                writer.Write(cylinder.CompressionRatio); Quantity(cylinder.BackPressure);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].ValveTiming is { } valve)
            {
                writer.Write(i); writer.Write(valve.CrankNode);
                Quantity(valve.CycleAngle); Quantity(valve.OpenAngle); Quantity(valve.DurationAngle);
            }
        for (int i = 0; i < asset.Nodes.Count; ++i)
            if (asset.Nodes[i].Gas?.Premixed is { } mixture)
            {
                writer.Write(i); Quantity(mixture.LowerHeatingValue); writer.Write(mixture.StoichiometricAirFuelRatio);
                writer.Write(mixture.InitialFractions!.Fuel); writer.Write(mixture.InitialFractions.FreshAir);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].ReservoirFractions is { } fractions)
            { writer.Write(i); writer.Write(fractions.Fuel); writer.Write(fractions.FreshAir); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].Combustion is { } burn)
            {
                writer.Write(i); Quantity(burn.CycleAngle); Quantity(burn.StartAngle); Quantity(burn.DurationAngle);
                writer.Write(burn.ShapeExponent); writer.Write(burn.BurnCoefficient);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].Friction is { } friction)
            { writer.Write(i); Quantity(friction.StaticCapacity); Quantity(friction.SlidingCapacity); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].Kind is ComponentKind.IdealGear or ComponentKind.PlanetaryGear or ComponentKind.DoublePinionPlanetaryGear or ComponentKind.CarrierGear)
            { writer.Write(i); writer.Write(asset.Components[i].NodeC); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].Converter is { } converter)
            {
                writer.Write(i);
                ConverterMap[] maps = [converter.PumpPositive!, converter.PumpNegative!, converter.TurbinePositive!, converter.TurbineNegative!];
                foreach (var map in maps) writer.Write(map.Points.Count);
                foreach (var map in maps) foreach (var point in map.Points)
                { writer.Write(point.SpeedRatio); writer.Write(point.TorqueRatio); Quantity(point.CapacityCoefficient); }
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].HydraulicRestriction is { } restriction)
            { writer.Write(i); Quantity(restriction.Coefficient); Quantity(restriction.TransitionPressure); Quantity(asset.Components[i].ReservoirPressure); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].HydraulicClutch is { } clutch)
            {
                writer.Write(i); writer.Write(clutch.PressureNode); Quantity(clutch.PistonArea); Quantity(clutch.PreloadForce); Quantity(clutch.EffectiveRadius);
                writer.Write(clutch.StaticFriction); writer.Write(clutch.SlidingFriction); writer.Write(clutch.FrictionSurfaces);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].HydraulicPump is { } pump)
            { writer.Write(i); writer.Write(pump.InletNode); Quantity(pump.Displacement); Quantity(asset.Components[i].ReservoirPressure); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].Kind == ComponentKind.HydraulicRelief)
            { writer.Write(i); Quantity(asset.Components[i].HydraulicRestriction!.CrackingPressure); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].PressureController is { } controller)
            {
                writer.Write(i); writer.Write(controller.TargetChannel); writer.Write(controller.SamplePeriodNanoseconds);
                Quantity(controller.ProportionalGain); Quantity(controller.IntegralGain);
                Quantity(controller.MinimumVoltage); Quantity(controller.MaximumVoltage); Quantity(controller.InitialIntegralVoltage);
            }
        for (int i = 0; i < asset.Nodes.Count; ++i)
            if (asset.Nodes[i].Battery is { } battery)
            {
                writer.Write(i); writer.Write(battery.HeatNode); Quantity(battery.EmptyOpenCircuitVoltage); Quantity(battery.FullOpenCircuitVoltage);
                Quantity(battery.SeriesResistance); Quantity(battery.PolarizationResistance); Quantity(battery.PolarizationCapacitance);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].PressureDutyController is { } duty)
            {
                writer.Write(i); writer.Write(duty.TargetChannel); writer.Write(duty.SamplePeriodNanoseconds);
                Quantity(duty.ProportionalGain); Quantity(duty.IntegralGain); Quantity(duty.MinimumDuty); Quantity(duty.MaximumDuty); Quantity(duty.InitialIntegralDuty);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].HydraulicPiston is { } piston)
            {
                writer.Write(i); writer.Write(piston.BackNode); Quantity(piston.FrontArea); Quantity(piston.BackArea); Quantity(piston.BackPressure);
                Quantity(piston.MinimumPosition); Quantity(piston.MaximumPosition); Quantity(piston.StopStiffness); Quantity(piston.ContactPosition); Quantity(piston.ContactStiffness);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].PistonClutch is { } contact)
            {
                writer.Write(i); writer.Write(contact.PistonComponent); Quantity(contact.EffectiveRadius); writer.Write(contact.StaticFriction);
                writer.Write(contact.SlidingFriction); writer.Write(contact.FrictionSurfaces);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].SpoolValve is { } land)
            { writer.Write(i); writer.Write(land.PistonComponent); Quantity(land.ClosedPosition); Quantity(land.FullOpenPosition); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].GasPiston is { } gasPiston)
            { writer.Write(i); writer.Write(gasPiston.CompressionDirection); Quantity(gasPiston.Area); Quantity(gasPiston.ReferenceVolume); Quantity(gasPiston.ReferencePosition); Quantity(gasPiston.ReferencePressure); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].FuelInjector is { } injector)
            { writer.Write(i); writer.Write(injector.CrankNode); Quantity(injector.CycleAngle); Quantity(injector.StartAngle); Quantity(injector.DurationAngle); Quantity(injector.MaximumDose); }
        for(int i=0;i<asset.Components.Count;++i)if(asset.Components[i].FuelFilm is { } film)
        {writer.Write(i);Quantity(film.InitialMass);Quantity(film.InitialTemperature);Quantity(film.LiquidSpecificHeat);Quantity(film.SaturationTemperature);Quantity(film.LatentInternalEnergy);}
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].LiquidFuelInjector is { } injector)
            {
                writer.Write(i); writer.Write(injector.FilmComponent); writer.Write(injector.CrankNode);
                Quantity(injector.CycleAngle); Quantity(injector.StartAngle); Quantity(injector.DurationAngle); Quantity(injector.MaximumDose);
                Quantity(injector.InitialMass); Quantity(injector.SupplyTemperature); Quantity(injector.LiquidDensity);
                Quantity(injector.InitialPressure); Quantity(injector.PressureCompliance);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].Solenoid is { } coil) { writer.Write(i); Quantity(coil.ReferencePosition); Quantity(coil.InductanceGradient); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].TravelStop is { } stop) { writer.Write(i); Quantity(stop.MinimumPosition); Quantity(stop.MaximumPosition); Quantity(stop.Stiffness); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].LiquidFuelInjector?.Needle is { } needle) { writer.Write(i); writer.Write(needle.NeedleNode); Quantity(needle.ClosedPosition); Quantity(needle.FullOpenPosition); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].NeedleDriver is { } driver) { writer.Write(i); writer.Write(driver.InjectorComponent); writer.Write(driver.SolenoidComponent); writer.Write(driver.SamplePeriodNanoseconds); Quantity(driver.DriveVoltage); writer.Write(driver.ClosurePredictionNanoseconds); }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].DualClutchController is { } controller)
            {
                writer.Write(i); writer.Write(controller.VehicleNode); writer.Write(controller.OddClutch); writer.Write(controller.EvenClutch);
                foreach (uint selector in controller.Selectors) writer.Write(selector);
                writer.Write(controller.SamplePeriodNanoseconds); writer.Write(controller.ReleaseNanoseconds); writer.Write(controller.EngageNanoseconds); writer.Write(controller.SynchronizeTimeoutNanoseconds);
                Quantity(controller.SynchronizeTolerance); Quantity(controller.DirectionChangeSpeedLimit);
            }
        for (int i = 0; i < asset.Components.Count; ++i)
            if (asset.Components[i].HydraulicAtController is { } at)
            {
                writer.Write(i); writer.Write(at.VehicleNode); writer.Write(at.RingNode); writer.Write(at.SupplyPressureNode); writer.Write(at.Routes.Count);
                writer.Write(at.SamplePeriodNanoseconds); writer.Write(at.ReleaseTimeoutNanoseconds); writer.Write(at.ApplyTimeoutNanoseconds); writer.Write(at.LowSupplyTimeoutNanoseconds); writer.Write(at.LockupDwellNanoseconds);
                foreach (var q in new[]{at.ApplyPressure,at.PressureTolerance,at.MinimumSupplyPressure,at.ReleaseForce,at.MinimumApplyForce,at.ProportionalGain,at.IntegralGain,at.SynchronizeTolerance,at.DirectionChangeSpeedLimit,at.LockupSpeedLimit,at.UnlockSpeedLimit,at.MinimumLockupInputSpeed}) Quantity(q);
                writer.Write(at.MinimumLockupForwardRange);
                foreach (var route in at.Routes) { writer.Write(route.Clutch); writer.Write(route.FillValve); writer.Write(route.DrainValve); }
            }
        for(int i=0;i<asset.Components.Count;++i)
            if(asset.Components[i].LiquidRailFeed is { } feed)
            {writer.Write(i);writer.Write(feed.InjectorComponent);writer.Write(feed.PumpComponent);Quantity(feed.SupplyTemperature);writer.Write(feed.TankComponent);}
        for(int i=0;i<asset.Components.Count;++i)
            if(asset.Components[i].LiquidFuelTank is { } tank)
            {writer.Write(i);writer.Write(tank.InjectorComponent);Quantity(tank.InitialMass);Quantity(tank.InitialTemperature);}
        foreach (var input in asset.Inputs) { writer.Write(input.TimeNanoseconds); writer.Write(input.Channel); writer.Write(input.Value); }
        foreach (var c in asset.Checks)
        {
            writer.Write(c.ObjectId); writer.Write((int)c.Field);
            writer.Write((byte)((c.Min.HasValue ? 1 : 0) | (c.Max.HasValue ? 2 : 0) | (c.AbsMax.HasValue ? 4 : 0)));
            writer.Write(c.Min ?? 0); writer.Write(c.Max ?? 0); writer.Write(c.AbsMax ?? 0);
        }
        writer.Flush();
        using var sha = SHA256.Create();
        byte[] digest = sha.ComputeHash(stream.GetBuffer(), 0, (int)stream.Length);
        writer.Write(digest); writer.Flush();
        if (stream.Length != size) throw new InvalidOperationException("Asset size calculation diverged from the encoder.");
        return stream.ToArray();
    }

    public static PowerAsset Decode(ReadOnlySpan<byte> payload)
    {
        if (payload.Length < 126 || payload.Length > MaxBytes) throw new AssetFormatException("Invalid asset length (maximum 1 MiB).");
        byte[] bytes = payload.ToArray(); // Own a stable buffer before validating it.
        int contentLength = bytes.Length - 32;
        using var sha = SHA256.Create();
        byte[] digest = sha.ComputeHash(bytes, 0, contentLength);
        if (!digest.AsSpan().SequenceEqual(bytes.AsSpan(contentLength))) throw new AssetFormatException("Asset SHA-256 integrity check failed.");
        try
        {
            using var stream = new MemoryStream(bytes, 0, contentLength, false);
            using var reader = new BinaryReader(stream, Utf8);
            if (!reader.ReadBytes(8).AsSpan().SequenceEqual(Magic)) throw new AssetFormatException("Unknown asset signature.");
            int version = reader.ReadInt32();
            if (version is < 1 or > FormatVersion) throw new AssetFormatException("Unsupported asset format version; supported versions are 1 through 26.");
            ulong fingerprint = reader.ReadUInt64(), step = reader.ReadUInt64(), duration = reader.ReadUInt64(), sample = reader.ReadUInt64();
            int nameLength = reader.ReadUInt16();
            if (nameLength is < 1 or > 512 || stream.Length - stream.Position < nameLength + 32) throw new AssetFormatException("Invalid asset name length.");
            string name = Utf8.GetString(reader.ReadBytes(nameLength));
            string sourceHash = BitConverter.ToString(reader.ReadBytes(32)).Replace("-", "").ToLowerInvariant();
            int Count(int maximum, int minimum = 0)
            {
                int count = reader.ReadInt32();
                return count >= minimum && count <= maximum ? count : throw new AssetFormatException("Asset count exceeds the supported limits.");
            }
            int nodes = Count(CompiledModel.MaxNodes, 1), components = Count(CompiledModel.MaxComponents),
                inputs = Count(PowerAsset.MaxScheduledInputs), checks = Count(PowerAsset.MaxChecks);
            int cylinders = version >= 2 ? Count(components) : 0;
            int gases = version >= 3 ? Count(nodes) : 0, orifices = version >= 3 ? Count(components) : 0;
            int moving = version >= 4 ? Count(components) : 0;
            int valves = version >= 5 ? Count(components) : 0;
            int mixtures = version >= 6 ? Count(nodes) : 0, reservoirMixtures = version >= 6 ? Count(components) : 0, burners = version >= 6 ? Count(components) : 0;
            int clutches = version >= 7 ? Count(components) : 0;
            int gears = version >= 8 ? Count(components) : 0;
            int converters = version >= 9 ? Count(Math.Min(components, CompiledModel.MaxConverters)) : 0;
            int points = version >= 9 ? Count(4 * converters * ConverterMap.MaxPoints, 8 * converters) : 0;
            int hydraulicRestrictions = version >= 10 ? Count(components) : 0, hydraulicClutches = version >= 10 ? Count(components) : 0;
            int pumps = version >= 11 ? Count(components) : 0, reliefs = version >= 11 ? Count(components) : 0;
            int controllers = version >= 12 ? Count(components) : 0;
            int batteries = version >= 13 ? Count(nodes) : 0, dutyControllers = version >= 13 ? Count(components) : 0;
            int pistons = version >= 14 ? Count(components) : 0, pistonClutches = version >= 14 ? Count(components) : 0;
            int spools = version >= 15 ? Count(components) : 0;
            int gasPistons = version >= 16 ? Count(components) : 0;
            int injectors = version >= 17 ? Count(components) : 0;
            int films=version>=18?Count(components):0;
            int liquidInjectors = version >= 19 ? Count(components) : 0;
            int solenoids = version >= 20 ? Count(components) : 0, stops = version >= 20 ? Count(components) : 0;
            int needles = version >= 20 ? Count(components) : 0, needleDrivers = version >= 20 ? Count(components) : 0;
            int dctControllers = version >= 22 ? Count(components) : 0;
            int atControllers = version >= 25 ? Count(components) : 0;
            int atRoutes = version >= 25 ? Count(6 * atControllers, 5 * atControllers) : 0;
            int liquidFeeds=version>=26?Count(components):0;
            int liquidTanks=version>=27?Count(components):0;
            if (stream.Length - stream.Position != 44L * nodes + 156L * components + 116L * cylinders + 24L * gases + 36L * orifices + 72L * moving + 44L * valves + 40L * mixtures + 20L * reservoirMixtures + 56L * burners + 28L * clutches + 8L * gears + 20L * converters + 28L * points + 40L * hydraulicRestrictions + 64L * hydraulicClutches + 32L * pumps + 16L * reliefs + 80L * controllers + 68L * batteries + 80L * dutyControllers + 104L * pistons + 40L * pistonClutches + 32L * spools + 56L * gasPistons + 56L * injectors + 64L * films + 120L * liquidInjectors + 28L * solenoids + 40L * stops + 32L * needles + (version >= 21 ? 40L : 32L) * needleDrivers + 104L * dctControllers + 208L * atControllers + 12L * atRoutes + (version>=27?28L:24L) * liquidFeeds + 32L * liquidTanks + 24L * inputs + 33L * checks)
                throw new AssetFormatException("Asset length does not match its declared counts.");
            Quantity Quantity() => new(reader.ReadDouble(), (Unit)reader.ReadInt32());
            var ns = new NodeDefinition[nodes]; var cs = new ComponentDefinition[components];
            for (int i = 0; i < nodes; ++i) ns[i] = new(reader.ReadUInt32(), (Domain)reader.ReadInt32(), Quantity(), Quantity(), Quantity());
            for (int i = 0; i < components; ++i)
                cs[i] = new()
                {
                    Id = reader.ReadUInt32(), Kind = (ComponentKind)reader.ReadInt32(), NodeA = reader.ReadUInt32(),
                    NodeB = reader.ReadUInt32(), HeatNode = reader.ReadUInt32(), InputChannel = reader.ReadUInt64(),
                    InitialInput = Quantity(), Stiffness = Quantity(), Damping = Quantity(), RestAngle = Quantity(),
                    Ratio = reader.ReadDouble(), Resistance = Quantity(), Inductance = Quantity(), Coupling = Quantity(),
                    InitialCurrent = Quantity(), Conductance = Quantity(), AmbientTemperature = Quantity()
                };
            if(version<27&&cs.Any(c=>c.Kind==ComponentKind.LiquidFuelTank))throw new AssetFormatException("Finite fuel tanks require version 27.");
            if(liquidTanks!=cs.Count(c=>c.Kind==ComponentKind.LiquidFuelTank))throw new AssetFormatException("Every finite fuel tank requires a typed record.");
            if(version<26&&cs.Any(c=>c.Kind==ComponentKind.LiquidRailFeed))throw new AssetFormatException("Pump-fed liquid rails require version 26.");
            if(liquidFeeds!=cs.Count(c=>c.Kind==ComponentKind.LiquidRailFeed))throw new AssetFormatException("Every liquid rail feed requires a typed record.");
            if (version < 25 && cs.Any(c => c.Kind == ComponentKind.HydraulicAtController)) throw new AssetFormatException("Hydraulic AT feedback requires version 25.");
            if (atControllers != cs.Count(c => c.Kind == ComponentKind.HydraulicAtController)) throw new AssetFormatException("Every AT controller requires one typed record.");
            if (version < 24 && cs.Any(c => c.Kind == ComponentKind.CarrierGear)) throw new AssetFormatException("Carrier-relative meshes require version 24.");
            if (version < 23 && cs.Any(c => c.Kind == ComponentKind.DoublePinionPlanetaryGear)) throw new AssetFormatException("Double-pinion topology requires version 23.");
            if (version < 22 && cs.Any(c => c.Kind == ComponentKind.DualClutchController)) throw new AssetFormatException("Sampled DCT control requires version 22.");
            if (dctControllers != cs.Count(c => c.Kind == ComponentKind.DualClutchController)) throw new AssetFormatException("Every DCT controller requires one typed record.");
            if (version < 20 && cs.Any(c => c.Kind is ComponentKind.Solenoid or ComponentKind.TravelStop or ComponentKind.NeedleDriver)) throw new AssetFormatException("Electromagnetic needle actuation requires asset version 20.");
            if (solenoids != cs.Count(c => c.Kind == ComponentKind.Solenoid) || stops != cs.Count(c => c.Kind == ComponentKind.TravelStop) || needleDrivers != cs.Count(c => c.Kind == ComponentKind.NeedleDriver))
                throw new AssetFormatException("Every solenoid, travel stop and needle driver requires one typed record.");
            if (version < 19 && cs.Any(c => c.Kind == ComponentKind.LiquidFuelInjector)) throw new AssetFormatException("Liquid fuel injection requires asset version 19.");
            if (liquidInjectors != cs.Count(c => c.Kind == ComponentKind.LiquidFuelInjector)) throw new AssetFormatException("Every liquid injector requires one finite-source timing record.");
            if(version<18&&cs.Any(c=>c.Kind==ComponentKind.FuelFilm))throw new AssetFormatException("Liquid fuel film requires asset version 18.");
            if(films!=cs.Count(c=>c.Kind==ComponentKind.FuelFilm))throw new AssetFormatException("Every liquid film requires one phase-property record.");
            if (version < 17 && cs.Any(c => c.Kind == ComponentKind.GasFuelInjector)) throw new AssetFormatException("Cycle fuel metering requires asset version 17.");
            if (injectors != cs.Count(c => c.Kind == ComponentKind.GasFuelInjector)) throw new AssetFormatException("Every gas fuel injector requires one timing extension.");
            if (version < 16 && cs.Any(c => c.Kind == ComponentKind.GasPiston)) throw new AssetFormatException("Linear gas pistons require asset version 16.");
            if (gasPistons != cs.Count(c => c.Kind == ComponentKind.GasPiston)) throw new AssetFormatException("Every gas piston requires one matching extension.");
            if (version < 15 && cs.Any(c => c.Kind == ComponentKind.HydraulicSpoolValve))
                throw new AssetFormatException("Mechanically metered spool valves require asset version 15.");
            if (spools != cs.Count(c => c.Kind == ComponentKind.HydraulicSpoolValve))
                throw new AssetFormatException("Every spool valve requires exactly one matching extension.");
            if (version < 14 && (ns.Any(n => n.Domain == Domain.Translational) || cs.Any(c => c.Kind is ComponentKind.LinearSpring or ComponentKind.HydraulicPiston or ComponentKind.PistonClutch or ComponentKind.ForceSource)))
                throw new AssetFormatException("Translational actuators and contact clutches require asset version 14.");
            if (pistons != cs.Count(c => c.Kind == ComponentKind.HydraulicPiston) || pistonClutches != cs.Count(c => c.Kind == ComponentKind.PistonClutch))
                throw new AssetFormatException("Every piston and piston clutch requires exactly one matching extension.");
            if (version < 13 && (ns.Any(n => n.Domain == Domain.Battery) || cs.Any(c => c.Kind is ComponentKind.BatteryMotor or ComponentKind.ResistiveLoad or ComponentKind.PressureDutyController)))
                throw new AssetFormatException("Battery supplies, loads, battery motors and duty controllers require asset version 13.");
            if (batteries != ns.Count(n => n.Domain == Domain.Battery) || dutyControllers != cs.Count(c => c.Kind == ComponentKind.PressureDutyController))
                throw new AssetFormatException("Every battery and duty controller requires exactly one matching extension.");
            if (version < 12 && cs.Any(c => c.Kind == ComponentKind.PressureController))
                throw new AssetFormatException("Pressure controllers require asset version 12.");
            if (version < 11 && cs.Any(c => c.Kind is ComponentKind.HydraulicPump or ComponentKind.HydraulicRelief))
                throw new AssetFormatException("Hydraulic pumps and relief valves require asset version 11.");
            if (version < 10 && (ns.Any(n => n.Domain == Domain.Hydraulic) || cs.Any(c => c.Kind is ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicClutch)))
                throw new AssetFormatException("Hydraulic nodes, restrictions and pressure clutches require asset version 10.");
            if (version < 3 && (ns.Any(n => n.Domain == Domain.Gas) || cs.Any(c => c.Kind is ComponentKind.GasOrifice or ComponentKind.GasHeatLink)))
                throw new AssetFormatException("Finite gas networks require asset version 3.");
            if (cylinders != cs.Count(c => c.Kind == ComponentKind.SealedCylinder) || gases != ns.Count(n => n.Domain == Domain.Gas) ||
                orifices != cs.Count(c => c.Kind is ComponentKind.GasOrifice or ComponentKind.GasFuelInjector or ComponentKind.LiquidFuelInjector) || moving != cs.Count(c => c.Kind == ComponentKind.GasCylinder) || burners != cs.Count(c => c.Kind == ComponentKind.PremixedCombustion) || clutches != cs.Count(c => c.Kind == ComponentKind.Clutch) || gears != cs.Count(c => c.Kind is ComponentKind.IdealGear or ComponentKind.PlanetaryGear or ComponentKind.DoublePinionPlanetaryGear or ComponentKind.CarrierGear) || converters != cs.Count(c => c.Kind == ComponentKind.TorqueConverter) ||
                hydraulicRestrictions != cs.Count(c => c.Kind is ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicRelief or ComponentKind.HydraulicSpoolValve) || hydraulicClutches != cs.Count(c => c.Kind == ComponentKind.HydraulicClutch) || pumps != cs.Count(c => c.Kind == ComponentKind.HydraulicPump) || reliefs != cs.Count(c => c.Kind == ComponentKind.HydraulicRelief) || controllers != cs.Count(c => c.Kind == ComponentKind.PressureController))
                throw new AssetFormatException("Every cylinder, gas node, orifice, burner, clutch, gear, converter and hydraulic component requires exactly one matching extension of a supported version.");
            for (int i = 0; i < cylinders; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.SealedCylinder || cs[index].Cylinder is not null)
                    throw new AssetFormatException("Cylinder extension must reference a distinct sealed-cylinder component.");
                cs[index] = cs[index] with { Cylinder = new()
                {
                    Bore = Quantity(), Stroke = Quantity(), RodLength = Quantity(), Phase = Quantity(), CompressionRatio = reader.ReadDouble(),
                    InitialPressure = Quantity(), InitialTemperature = Quantity(), GasConstant = Quantity(), Gamma = reader.ReadDouble(), BackPressure = Quantity()
                } };
            }
            for (int i = 0; i < gases; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= nodes || ns[index].Domain != Domain.Gas || ns[index].Gas is not null)
                    throw new AssetFormatException("Gas extension must reference a distinct gas node.");
                ns[index] = ns[index] with { Gas = new() { GasConstant = Quantity(), Gamma = reader.ReadDouble() } };
            }
            var seenOrifices = new bool[components];
            for (int i = 0; i < orifices; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind is not (ComponentKind.GasOrifice or ComponentKind.GasFuelInjector or ComponentKind.LiquidFuelInjector) || seenOrifices[index])
                    throw new AssetFormatException("Orifice extension must reference a distinct gas-orifice component.");
                seenOrifices[index] = true;
                cs[index] = cs[index] with { Area = Quantity(), DischargeCoefficient = reader.ReadDouble(), ReservoirPressure = Quantity() };
            }
            for (int i = 0; i < moving; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.GasCylinder || cs[index].MovingCylinder is not null)
                    throw new AssetFormatException("Moving-cylinder extension must reference a distinct gas-cylinder component.");
                cs[index] = cs[index] with { MovingCylinder = new()
                {
                    Bore = Quantity(), Stroke = Quantity(), RodLength = Quantity(), Phase = Quantity(),
                    CompressionRatio = reader.ReadDouble(), BackPressure = Quantity()
                } };
            }
            for (int i = 0; i < valves; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.GasOrifice || cs[index].ValveTiming is not null)
                    throw new AssetFormatException("Valve timing must reference a distinct gas-orifice component.");
                cs[index] = cs[index] with { ValveTiming = new()
                {
                    CrankNode = reader.ReadUInt32(), CycleAngle = Quantity(), OpenAngle = Quantity(), DurationAngle = Quantity()
                } };
            }
            for (int i = 0; i < mixtures; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= nodes || ns[index].Gas is null || ns[index].Gas!.Premixed is not null)
                    throw new AssetFormatException("Premixed composition must reference a distinct gas node.");
                ns[index] = ns[index] with { Gas = ns[index].Gas! with { Premixed = new()
                {
                    LowerHeatingValue = Quantity(), StoichiometricAirFuelRatio = reader.ReadDouble(),
                    InitialFractions = new(reader.ReadDouble(), reader.ReadDouble())
                } } };
            }
            for (int i = 0; i < reservoirMixtures; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.GasOrifice || cs[index].NodeB != 0 || cs[index].ReservoirFractions is not null)
                    throw new AssetFormatException("Reservoir fractions must reference a distinct reservoir orifice.");
                cs[index] = cs[index] with { ReservoirFractions = new(reader.ReadDouble(), reader.ReadDouble()) };
            }
            for (int i = 0; i < burners; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.PremixedCombustion || cs[index].Combustion is not null)
                    throw new AssetFormatException("Burn parameters must reference a distinct premixed combustion component.");
                cs[index] = cs[index] with { Combustion = new()
                {
                    CycleAngle = Quantity(), StartAngle = Quantity(), DurationAngle = Quantity(),
                    ShapeExponent = reader.ReadDouble(), BurnCoefficient = reader.ReadDouble()
                } };
            }
            for (int i = 0; i < clutches; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.Clutch || cs[index].Friction is not null)
                    throw new AssetFormatException("Clutch capacities must reference a distinct clutch component.");
                cs[index] = cs[index] with { Friction = new() { StaticCapacity = Quantity(), SlidingCapacity = Quantity() } };
            }
            var seenGears = new bool[components];
            for (int i = 0; i < gears; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind is not (ComponentKind.IdealGear or ComponentKind.PlanetaryGear or ComponentKind.DoublePinionPlanetaryGear or ComponentKind.CarrierGear) || seenGears[index])
                    throw new AssetFormatException("Gear topology must reference a distinct ideal or planetary gear component.");
                seenGears[index] = true; cs[index] = cs[index] with { NodeC = reader.ReadUInt32() };
            }
            int remainingPoints = points;
            for (int i = 0; i < converters; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.TorqueConverter || cs[index].Converter is not null)
                    throw new AssetFormatException("Converter maps must reference a distinct torque-converter component.");
                int[] counts = [Count(ConverterMap.MaxPoints, 2), Count(ConverterMap.MaxPoints, 2), Count(ConverterMap.MaxPoints, 2), Count(ConverterMap.MaxPoints, 2)];
                int total = counts.Sum();
                if (total > remainingPoints) throw new AssetFormatException("Converter map points exceed the declared total.");
                remainingPoints -= total;
                var maps = new ConverterMap[4];
                for (int k = 0; k < 4; ++k)
                {
                    var entries = new ConverterMapPoint[counts[k]];
                    for (int j = 0; j < entries.Length; ++j) entries[j] = new(reader.ReadDouble(), reader.ReadDouble(), Quantity());
                    maps[k] = new(entries);
                }
                cs[index] = cs[index] with { Converter = new() { PumpPositive = maps[0], PumpNegative = maps[1], TurbinePositive = maps[2], TurbineNegative = maps[3] } };
            }
            if (remainingPoints != 0) throw new AssetFormatException("Converter map counts differ from the declared total.");
            for (int i = 0; i < hydraulicRestrictions; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind is not (ComponentKind.HydraulicResistance or ComponentKind.HydraulicOrifice or ComponentKind.HydraulicRelief or ComponentKind.HydraulicSpoolValve) || cs[index].HydraulicRestriction is not null)
                    throw new AssetFormatException("Hydraulic flow parameters must reference a distinct hydraulic restriction.");
                cs[index] = cs[index] with { HydraulicRestriction = new() { Coefficient = Quantity(), TransitionPressure = Quantity() }, ReservoirPressure = Quantity() };
            }
            for (int i = 0; i < hydraulicClutches; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.HydraulicClutch || cs[index].HydraulicClutch is not null)
                    throw new AssetFormatException("Pressure actuation must reference a distinct hydraulic clutch.");
                cs[index] = cs[index] with { HydraulicClutch = new()
                {
                    PressureNode = reader.ReadUInt32(), PistonArea = Quantity(), PreloadForce = Quantity(), EffectiveRadius = Quantity(),
                    StaticFriction = reader.ReadDouble(), SlidingFriction = reader.ReadDouble(), FrictionSurfaces = reader.ReadUInt32()
                } };
            }
            for (int i = 0; i < pumps; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.HydraulicPump || cs[index].HydraulicPump is not null)
                    throw new AssetFormatException("Pump extension must reference a distinct hydraulic pump.");
                cs[index] = cs[index] with { HydraulicPump = new() { InletNode = reader.ReadUInt32(), Displacement = Quantity() }, ReservoirPressure = Quantity() };
            }
            var seenReliefs = new bool[components];
            for (int i = 0; i < reliefs; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.HydraulicRelief || seenReliefs[index])
                    throw new AssetFormatException("Relief extension must reference a distinct relief valve.");
                seenReliefs[index] = true;
                cs[index] = cs[index] with { HydraulicRestriction = cs[index].HydraulicRestriction! with { CrackingPressure = Quantity() } };
            }
            for (int i = 0; i < controllers; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.PressureController || cs[index].PressureController is not null)
                    throw new AssetFormatException("Controller extension must reference a distinct pressure controller.");
                cs[index] = cs[index] with { PressureController = new()
                {
                    TargetChannel = reader.ReadUInt64(), SamplePeriodNanoseconds = reader.ReadUInt64(),
                    ProportionalGain = Quantity(), IntegralGain = Quantity(), MinimumVoltage = Quantity(),
                    MaximumVoltage = Quantity(), InitialIntegralVoltage = Quantity()
                } };
            }
            for (int i = 0; i < batteries; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= nodes || ns[index].Domain != Domain.Battery || ns[index].Battery is not null)
                    throw new AssetFormatException("Battery extension must reference a distinct battery node.");
                ns[index] = ns[index] with { Battery = new()
                {
                    HeatNode = reader.ReadUInt32(), EmptyOpenCircuitVoltage = Quantity(), FullOpenCircuitVoltage = Quantity(),
                    SeriesResistance = Quantity(), PolarizationResistance = Quantity(), PolarizationCapacitance = Quantity()
                } };
            }
            for (int i = 0; i < dutyControllers; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.PressureDutyController || cs[index].PressureDutyController is not null)
                    throw new AssetFormatException("Duty controller extension must reference a distinct pressure duty controller.");
                cs[index] = cs[index] with { PressureDutyController = new()
                {
                    TargetChannel = reader.ReadUInt64(), SamplePeriodNanoseconds = reader.ReadUInt64(), ProportionalGain = Quantity(),
                    IntegralGain = Quantity(), MinimumDuty = Quantity(), MaximumDuty = Quantity(), InitialIntegralDuty = Quantity()
                } };
            }
            for (int i = 0; i < pistons; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.HydraulicPiston || cs[index].HydraulicPiston is not null)
                    throw new AssetFormatException("Piston extension must reference a distinct hydraulic piston.");
                cs[index] = cs[index] with { HydraulicPiston = new()
                {
                    BackNode = reader.ReadUInt32(), FrontArea = Quantity(), BackArea = Quantity(), BackPressure = Quantity(),
                    MinimumPosition = Quantity(), MaximumPosition = Quantity(), StopStiffness = Quantity(), ContactPosition = Quantity(), ContactStiffness = Quantity()
                } };
            }
            for (int i = 0; i < pistonClutches; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.PistonClutch || cs[index].PistonClutch is not null)
                    throw new AssetFormatException("Contact clutch extension must reference a distinct piston clutch.");
                cs[index] = cs[index] with { PistonClutch = new() { PistonComponent = reader.ReadUInt32(), EffectiveRadius = Quantity(),
                    StaticFriction = reader.ReadDouble(), SlidingFriction = reader.ReadDouble(), FrictionSurfaces = reader.ReadUInt32() } };
            }
            for (int i = 0; i < spools; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.HydraulicSpoolValve || cs[index].SpoolValve is not null)
                    throw new AssetFormatException("Spool geometry must reference a distinct hydraulic spool valve.");
                cs[index] = cs[index] with { SpoolValve = new() { PistonComponent = reader.ReadUInt32(), ClosedPosition = Quantity(), FullOpenPosition = Quantity() } };
            }
            for (int i = 0; i < gasPistons; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.GasPiston || cs[index].GasPiston is not null) throw new AssetFormatException("Linear gas geometry must reference a distinct gas piston.");
                cs[index] = cs[index] with { GasPiston = new() { CompressionDirection = reader.ReadInt32(), Area = Quantity(), ReferenceVolume = Quantity(), ReferencePosition = Quantity(), ReferencePressure = Quantity() } };
            }
            for (int i = 0; i < injectors; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.GasFuelInjector || cs[index].FuelInjector is not null) throw new AssetFormatException("Injector timing must reference a distinct gas fuel injector.");
                cs[index] = cs[index] with { FuelInjector = new() { CrankNode = reader.ReadUInt32(), CycleAngle = Quantity(), StartAngle = Quantity(), DurationAngle = Quantity(), MaximumDose = Quantity() } };
            }
            for(int i=0;i<films;++i)
            {int index=reader.ReadInt32();if(index<0||index>=components||cs[index].Kind!=ComponentKind.FuelFilm||cs[index].FuelFilm is not null)throw new AssetFormatException("Phase properties must reference a distinct fuel film.");
             cs[index]=cs[index] with{FuelFilm=new(){InitialMass=Quantity(),InitialTemperature=Quantity(),LiquidSpecificHeat=Quantity(),SaturationTemperature=Quantity(),LatentInternalEnergy=Quantity()}};}
            for (int i = 0; i < liquidInjectors; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.LiquidFuelInjector || cs[index].LiquidFuelInjector is not null)
                    throw new AssetFormatException("Finite liquid source must reference a distinct liquid injector.");
                cs[index] = cs[index] with { LiquidFuelInjector = new()
                {
                    FilmComponent = reader.ReadUInt32(), CrankNode = reader.ReadUInt32(),
                    CycleAngle = Quantity(), StartAngle = Quantity(), DurationAngle = Quantity(), MaximumDose = Quantity(),
                    InitialMass = Quantity(), SupplyTemperature = Quantity(), LiquidDensity = Quantity(), InitialPressure = Quantity(), PressureCompliance = Quantity()
                } };
            }
            for (int i = 0; i < solenoids; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.Solenoid || cs[index].Solenoid is not null) throw new AssetFormatException("Solenoid record must reference a distinct solenoid.");
                cs[index] = cs[index] with { Solenoid = new() { ReferencePosition = Quantity(), InductanceGradient = Quantity() } };
            }
            for (int i = 0; i < stops; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.TravelStop || cs[index].TravelStop is not null) throw new AssetFormatException("Travel-stop record must reference a distinct stop.");
                cs[index] = cs[index] with { TravelStop = new() { MinimumPosition = Quantity(), MaximumPosition = Quantity(), Stiffness = Quantity() } };
            }
            for (int i = 0; i < needles; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].LiquidFuelInjector is null || cs[index].LiquidFuelInjector!.Needle is not null) throw new AssetFormatException("Needle record must reference a distinct liquid injector.");
                cs[index] = cs[index] with { LiquidFuelInjector = cs[index].LiquidFuelInjector! with { Needle = new() { NeedleNode = reader.ReadUInt32(), ClosedPosition = Quantity(), FullOpenPosition = Quantity() } } };
            }
            for (int i = 0; i < needleDrivers; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.NeedleDriver || cs[index].NeedleDriver is not null) throw new AssetFormatException("Needle-driver record must reference a distinct driver.");
                cs[index] = cs[index] with { NeedleDriver = new() { InjectorComponent = reader.ReadUInt32(), SolenoidComponent = reader.ReadUInt32(), SamplePeriodNanoseconds = reader.ReadUInt64(), DriveVoltage = Quantity(), ClosurePredictionNanoseconds = version >= 21 ? reader.ReadUInt64() : 0 } };
            }
            for (int i = 0; i < dctControllers; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.DualClutchController || cs[index].DualClutchController is not null) throw new AssetFormatException("DCT record must reference a distinct controller.");
                uint vehicle = reader.ReadUInt32(), odd = reader.ReadUInt32(), even = reader.ReadUInt32(); var selectors = new uint[8];
                for (int gear = 0; gear < 8; ++gear) selectors[gear] = reader.ReadUInt32();
                cs[index] = cs[index] with { DualClutchController = new() { VehicleNode = vehicle, OddClutch = odd, EvenClutch = even, Selectors = selectors,
                    SamplePeriodNanoseconds = reader.ReadUInt64(), ReleaseNanoseconds = reader.ReadUInt64(), EngageNanoseconds = reader.ReadUInt64(), SynchronizeTimeoutNanoseconds = reader.ReadUInt64(),
                    SynchronizeTolerance = Quantity(), DirectionChangeSpeedLimit = Quantity() } };
            }
            int remainingAtRoutes = atRoutes;
            for (int i = 0; i < atControllers; ++i)
            {
                int index = reader.ReadInt32();
                if (index < 0 || index >= components || cs[index].Kind != ComponentKind.HydraulicAtController || cs[index].HydraulicAtController is not null) throw new AssetFormatException("AT record must reference a distinct hydraulic AT controller.");
                uint vehicle=reader.ReadUInt32(),ring=reader.ReadUInt32(),supply=reader.ReadUInt32();int count=reader.ReadInt32();
                if (count is <5 or >6 || count>remainingAtRoutes) throw new AssetFormatException("AT route counts must be 5 or 6 and match their declared total.");
                remainingAtRoutes-=count;
                var at = new HydraulicAtControllerDefinition { VehicleNode=vehicle,RingNode=ring,SupplyPressureNode=supply,
                    SamplePeriodNanoseconds=reader.ReadUInt64(),ReleaseTimeoutNanoseconds=reader.ReadUInt64(),ApplyTimeoutNanoseconds=reader.ReadUInt64(),LowSupplyTimeoutNanoseconds=reader.ReadUInt64(),LockupDwellNanoseconds=reader.ReadUInt64(),
                    ApplyPressure=Quantity(),PressureTolerance=Quantity(),MinimumSupplyPressure=Quantity(),ReleaseForce=Quantity(),MinimumApplyForce=Quantity(),ProportionalGain=Quantity(),IntegralGain=Quantity(),
                    SynchronizeTolerance=Quantity(),DirectionChangeSpeedLimit=Quantity(),LockupSpeedLimit=Quantity(),UnlockSpeedLimit=Quantity(),MinimumLockupInputSpeed=Quantity(),MinimumLockupForwardRange=reader.ReadInt32() };
                var routes=new AtActuatorRoute[count];for(int route=0;route<count;++route)routes[route]=new(reader.ReadUInt32(),reader.ReadUInt32(),reader.ReadUInt32());
                cs[index]=cs[index] with { HydraulicAtController=at with { Routes=routes } };
            }
            if (remainingAtRoutes!=0) throw new AssetFormatException("AT route totals differ from the declared count.");
            for(int i=0;i<liquidFeeds;++i)
            {
                int index=reader.ReadInt32();if(index<0||index>=components||cs[index].Kind!=ComponentKind.LiquidRailFeed||cs[index].LiquidRailFeed is not null)throw new AssetFormatException("Liquid feed record must reference a distinct feed component.");
                cs[index]=cs[index] with{LiquidRailFeed=new(){InjectorComponent=reader.ReadUInt32(),PumpComponent=reader.ReadUInt32(),SupplyTemperature=Quantity(),TankComponent=version>=27?reader.ReadUInt32():0}};
            }
            var schedule = new ScheduledInput[inputs];
            for(int i=0;i<liquidTanks;++i)
            {
                int index=reader.ReadInt32();if(index<0||index>=components||cs[index].Kind!=ComponentKind.LiquidFuelTank||cs[index].LiquidFuelTank is not null)throw new AssetFormatException("Tank record must reference a distinct tank component.");
                cs[index]=cs[index] with{LiquidFuelTank=new(){InjectorComponent=reader.ReadUInt32(),InitialMass=Quantity(),InitialTemperature=Quantity()}};
            }
            for (int i = 0; i < inputs; ++i) schedule[i] = new(reader.ReadUInt64(), reader.ReadUInt64(), reader.ReadDouble());
            var conditions = new AssetCheck[checks];
            for (int i = 0; i < checks; ++i)
            {
                uint id = reader.ReadUInt32(); var field = (Field)reader.ReadInt32(); byte flags = reader.ReadByte();
                if (flags is < 1 or > 7) throw new AssetFormatException("Unknown output-check flags.");
                double min = reader.ReadDouble(), max = reader.ReadDouble(), abs = reader.ReadDouble();
                if (((flags & 1) == 0 && min != 0) || ((flags & 2) == 0 && max != 0) || ((flags & 4) == 0 && abs != 0))
                    throw new AssetFormatException("Unused output-check values must be zero.");
                conditions[i] = new(id, field, (flags & 1) != 0 ? min : null, (flags & 2) != 0 ? max : null, (flags & 4) != 0 ? abs : null);
            }
            var asset = PowerAsset.Create(new() { StepNanoseconds = step, Nodes = ns, Components = cs }, name, sourceHash, duration, sample, schedule, conditions);
            if (asset.Model.Fingerprint != fingerprint) throw new AssetFormatException("Model fingerprint differs; rebuild the asset with the current solver.");
            return asset;
        }
        catch (EndOfStreamException) { throw new AssetFormatException("Truncated asset."); }
        catch (DecoderFallbackException) { throw new AssetFormatException("Invalid UTF-8 in asset name."); }
    }
}
