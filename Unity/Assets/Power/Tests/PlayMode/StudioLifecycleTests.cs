// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System.Collections;
using NUnit.Framework;
using Power.Core;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Power.Studio.Tests
{
    public sealed class StudioLifecycleTests
    {
        private GameObject _host;

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (_host != null) Object.Destroy(_host);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ReenableResetsStateAndReleasesGeneratedObjects()
        {
            _host = new GameObject("Lifecycle test");
            var studio = _host.AddComponent<PowerStudio>();
            studio.SetRunning(false);
            ulong initial = studio.StateHash;
            studio.AdvanceOnePresentationStep();
            Assert.That(studio.SimulationTimeNanoseconds, Is.EqualTo(PowerStudio.PresentationStepNanoseconds));
            studio.enabled = false;
            yield return null;
            Assert.That(_host.transform.childCount, Is.EqualTo(0));
            studio.enabled = true;
            studio.SetRunning(false);
            yield return null;
            Assert.That(studio.enabled, Is.True);
            Assert.That(studio.StateHash, Is.EqualTo(initial));
            Assert.That(studio.SimulationTimeNanoseconds, Is.EqualTo(0UL));
            Assert.That(_host.transform.childCount, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator ReferenceExperimentMatchesHeadlessCore()
        {
            _host = new GameObject("Reference test");
            var studio = _host.AddComponent<PowerStudio>();
            studio.RunReferenceExperiment();
            studio.SetRunning(false);
            for (int i = 0; i < 500; ++i) studio.AdvanceOnePresentationStep();
            var model = CompiledModel.Compile(SampleModels.Electrothermal());
            var simulation = model.CreateSimulation();
            Assert.That(simulation.Step(5_000_000_000), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(simulation.SubmitInputs(new[] { new Scalar(100, 4) }), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(simulation.Step(1_000_000_000), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(simulation.SubmitInputs(new[] { new Scalar(100, 24) }), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(simulation.Step(4_000_000_000), Is.EqualTo(SimulationStatus.Ok));
            var snapshot = simulation.ReadSnapshot(new Scalar[model.OutputCount]);
            Assert.That(studio.SimulationTimeNanoseconds, Is.EqualTo(10_000_000_000UL));
            Assert.That(studio.StateHash, Is.EqualTo(snapshot.StateHash));
            Assert.That(studio.IsRunning, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ImportedCylinderMovesPistonAndReplaysTheExperiment()
        {
            _host = new GameObject("Cylinder model test");
            var studio = _host.AddComponent<PowerStudio>();
            var source = Resources.Load<PowerModelAsset>("SealedCylinder");
            Assert.That(source, Is.Not.Null);
            studio.SetModelAsset(source); studio.SetRunning(false);
            yield return null;
            var piston = _host.transform.Find("Power generated lab/Piston 10");
            Assert.That(piston, Is.Not.Null);
            float initialHeight = piston.localPosition.y;
            studio.AdvanceOnePresentationStep();
            yield return null;
            Assert.That(piston.localPosition.y, Is.GreaterThan(initialHeight + 0.5f));
            studio.RunReferenceExperiment(); studio.SetRunning(false);
            for (int i = 0; i < 10; ++i) studio.AdvanceOnePresentationStep();
            var asset = source.Load(); var playback = asset.CreatePlayback();
            Assert.That(playback.Advance(asset.DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(studio.StateHash, Is.EqualTo(playback.ReadSnapshot(new Scalar[asset.Model.OutputCount]).StateHash));
            studio.enabled = false;
            yield return null;
            Assert.That(_host.transform.childCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator MovingGasCylinderMovesPistonAndReplaysRestrictions()
        {
            _host = new GameObject("Moving gas cylinder test");
            var studio = _host.AddComponent<PowerStudio>();
            var source = Resources.Load<PowerModelAsset>("MovingCylinder");
            Assert.That(source, Is.Not.Null);
            studio.SetModelAsset(source); studio.SetRunning(false);
            yield return null;
            var piston = _host.transform.Find("Power generated lab/Piston 10");
            Assert.That(piston, Is.Not.Null);
            float height = piston.localPosition.y;
            studio.AdvanceOnePresentationStep();
            yield return null;
            Assert.That(piston.localPosition.y, Is.LessThan(height - 0.1f));
            studio.RunReferenceExperiment(); studio.SetRunning(false);
            for (int i = 0; i < 10; ++i) studio.AdvanceOnePresentationStep();
            var asset = source.Load(); var playback = asset.CreatePlayback();
            Assert.That(playback.Advance(asset.DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(studio.StateHash, Is.EqualTo(playback.ReadSnapshot(new Scalar[asset.Model.OutputCount]).StateHash));
            Assert.That(studio.IsRunning, Is.False);
        }

        [UnityTest]
        public IEnumerator FiredCylinderShowsHeatReleaseAndPreservesReferenceReplay()
        {
            _host = new GameObject("Fired cylinder test");
            var studio = _host.AddComponent<PowerStudio>();
            var source = Resources.Load<PowerModelAsset>("FiredCylinder");
            Assert.That(source, Is.Not.Null);
            studio.SetModelAsset(source); studio.SetRunning(false);
            yield return null;
            var marker = _host.transform.Find("Power generated lab/Combustion 15");
            Assert.That(marker, Is.Not.Null);
            var material = marker.GetComponent<Renderer>().sharedMaterial;
            Color idle = material.GetColor("_BaseColor");
            bool showedHeat = false;
            studio.RunReferenceExperiment(); studio.SetRunning(false);
            for (int i = 0; i < 30; ++i)
            {
                studio.AdvanceOnePresentationStep();
                showedHeat |= material.GetColor("_BaseColor").r > idle.r + 0.05f;
            }
            Assert.That(showedHeat, Is.True);
            var asset = source.Load(); var playback = asset.CreatePlayback();
            Assert.That(playback.Advance(asset.DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(studio.StateHash, Is.EqualTo(playback.ReadSnapshot(new Scalar[asset.Model.OutputCount]).StateHash));
            studio.ResetSimulation(); studio.SetRunning(false);
            Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(idle));
            studio.enabled = false;
            yield return null;
            Assert.That(_host.transform.childCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FiredPumpShowsPowerPortsAndReplays()
        {
            _host = new GameObject("Fired pump test");
            var studio = _host.AddComponent<PowerStudio>();
            var source = Resources.Load<PowerModelAsset>("FiredPump");
            Assert.That(source, Is.Not.Null);
            studio.SetModelAsset(source); studio.SetRunning(false);
            yield return null;
            ulong initial = studio.StateHash;
            foreach (string name in new[] { "Hydraulic pump 46", "Pump shaft 46", "Pump inlet 46", "Pump outlet 46", "Hydraulic valve 47" })
                Assert.That(_host.transform.Find("Power generated lab/" + name), Is.Not.Null);
            studio.RunReferenceExperiment(); studio.SetRunning(false);
            for (int i = 0; i < 40; ++i) studio.AdvanceOnePresentationStep();
            var asset = source.Load(); var playback = asset.CreatePlayback();
            Assert.That(playback.Advance(asset.DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(studio.StateHash, Is.EqualTo(playback.ReadSnapshot(new Scalar[asset.Model.OutputCount]).StateHash));
            studio.ResetSimulation(); studio.SetRunning(false);
            Assert.That(studio.StateHash, Is.EqualTo(initial));
            studio.enabled = false;
            yield return null;
            Assert.That(_host.transform.childCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FiredHydraulicShowsPressurePortsAndReplaysLockup()
        {
            _host = new GameObject("Fired hydraulic test");
            var studio = _host.AddComponent<PowerStudio>();
            var source = Resources.Load<PowerModelAsset>("FiredHydraulic");
            Assert.That(source, Is.Not.Null);
            studio.SetModelAsset(source); studio.SetRunning(false);
            yield return null;
            ulong initial = studio.StateHash;
            foreach (string name in new[] { "Hydraulic chamber 32", "Hydraulic valve 44", "Pressure actuator 21", "Converter pump 20", "Planetary gear 18" })
                Assert.That(_host.transform.Find("Power generated lab/" + name), Is.Not.Null);
            var plate = _host.transform.Find("Power generated lab/Clutch plate A 21");
            var material = plate.GetComponent<Renderer>().sharedMaterial;
            bool showedSlip = false, showedLock = false;
            studio.RunReferenceExperiment(); studio.SetRunning(false);
            for (int i = 0; i < 40; ++i)
            {
                studio.AdvanceOnePresentationStep();
                Color color = material.GetColor("_BaseColor");
                showedSlip |= color.r > 0.9f;
                showedLock |= color.g > 0.7f && color.r < 0.2f;
            }
            Assert.That(showedSlip && showedLock, Is.True);
            var asset = source.Load(); var playback = asset.CreatePlayback();
            Assert.That(playback.Advance(asset.DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(studio.StateHash, Is.EqualTo(playback.ReadSnapshot(new Scalar[asset.Model.OutputCount]).StateHash));
            studio.ResetSimulation(); studio.SetRunning(false);
            Assert.That(studio.StateHash, Is.EqualTo(initial));
            studio.enabled = false;
            yield return null;
            Assert.That(_host.transform.childCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FiredConverterShowsFluidPortsAndReplaysLockup()
        {
            _host = new GameObject("Fired converter test");
            var studio = _host.AddComponent<PowerStudio>();
            var source = Resources.Load<PowerModelAsset>("FiredConverter");
            Assert.That(source, Is.Not.Null);
            studio.SetModelAsset(source); studio.SetRunning(false);
            yield return null;
            ulong initial = studio.StateHash;
            foreach (string name in new[] { "Converter pump 20", "Converter turbine 20", "Converter stationary stator 20", "Planetary gear 18", "Ideal gear 19" })
                Assert.That(_host.transform.Find("Power generated lab/" + name), Is.Not.Null);
            var plate = _host.transform.Find("Power generated lab/Clutch plate A 21");
            var material = plate.GetComponent<Renderer>().sharedMaterial;
            bool showedSlip = false, showedLock = false;
            studio.RunReferenceExperiment(); studio.SetRunning(false);
            for (int i = 0; i < 40; ++i)
            {
                studio.AdvanceOnePresentationStep();
                Color color = material.GetColor("_BaseColor");
                showedSlip |= color.r > 0.9f;
                showedLock |= color.g > 0.7f && color.r < 0.2f;
            }
            Assert.That(showedSlip && showedLock, Is.True);
            var asset = source.Load(); var playback = asset.CreatePlayback();
            Assert.That(playback.Advance(asset.DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(studio.StateHash, Is.EqualTo(playback.ReadSnapshot(new Scalar[asset.Model.OutputCount]).StateHash));
            studio.ResetSimulation(); studio.SetRunning(false);
            Assert.That(studio.StateHash, Is.EqualTo(initial));
            studio.enabled = false;
            yield return null;
            Assert.That(_host.transform.childCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FiredPlanetaryShowsThreePortsAndReplaysShift()
        {
            _host = new GameObject("Fired planetary test");
            var studio = _host.AddComponent<PowerStudio>();
            var source = Resources.Load<PowerModelAsset>("FiredPlanetary");
            Assert.That(source, Is.Not.Null);
            studio.SetModelAsset(source); studio.SetRunning(false);
            yield return null;
            ulong initial = studio.StateHash;
            foreach (string name in new[] { "Planetary gear 18", "Planetary sun 18", "Planetary ring 18", "Planetary carrier 18", "Ideal gear 19" })
                Assert.That(_host.transform.Find("Power generated lab/" + name), Is.Not.Null);
            var plate = _host.transform.Find("Power generated lab/Clutch plate A 16");
            var material = plate.GetComponent<Renderer>().sharedMaterial;
            bool showedSlip = false, showedLock = false;
            studio.RunReferenceExperiment(); studio.SetRunning(false);
            for (int i = 0; i < 40; ++i)
            {
                studio.AdvanceOnePresentationStep();
                Color color = material.GetColor("_BaseColor");
                showedSlip |= color.r > 0.9f;
                showedLock |= color.g > 0.7f && color.r < 0.2f;
            }
            Assert.That(showedSlip && showedLock, Is.True);
            var asset = source.Load(); var playback = asset.CreatePlayback();
            Assert.That(playback.Advance(asset.DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(studio.StateHash, Is.EqualTo(playback.ReadSnapshot(new Scalar[asset.Model.OutputCount]).StateHash));
            studio.ResetSimulation(); studio.SetRunning(false);
            Assert.That(studio.StateHash, Is.EqualTo(initial));
            studio.enabled = false;
            yield return null;
            Assert.That(_host.transform.childCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator FiredClutchShowsPhaseAndReplaysAfterReset()
        {
            _host = new GameObject("Fired clutch test");
            var studio = _host.AddComponent<PowerStudio>();
            var source = Resources.Load<PowerModelAsset>("FiredClutch");
            Assert.That(source, Is.Not.Null);
            studio.SetModelAsset(source); studio.SetRunning(false);
            yield return null;
            var plate = _host.transform.Find("Power generated lab/Clutch plate A 16");
            Assert.That(plate, Is.Not.Null);
            var material = plate.GetComponent<Renderer>().sharedMaterial;
            Color idle = material.GetColor("_BaseColor");
            bool showedSlip = false, showedLock = false;
            studio.RunReferenceExperiment(); studio.SetRunning(false);
            for (int i = 0; i < 30; ++i)
            {
                studio.AdvanceOnePresentationStep();
                Color color = material.GetColor("_BaseColor");
                showedSlip |= color.r > 0.9f;
                showedLock |= color.g > 0.7f && color.r < 0.2f;
            }
            Assert.That(showedSlip && showedLock, Is.True);
            var asset = source.Load(); var playback = asset.CreatePlayback();
            Assert.That(playback.Advance(asset.DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(studio.StateHash, Is.EqualTo(playback.ReadSnapshot(new Scalar[asset.Model.OutputCount]).StateHash));
            studio.ResetSimulation(); studio.SetRunning(false);
            Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(idle));
            studio.enabled = false;
            yield return null;
            Assert.That(_host.transform.childCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator CrankTimedValvesFollowOpeningAndReplayAfterReset()
        {
            _host = new GameObject("Crank-timed cylinder test");
            var studio = _host.AddComponent<PowerStudio>();
            var source = Resources.Load<PowerModelAsset>("CrankTimedCylinder");
            Assert.That(source, Is.Not.Null);
            studio.SetModelAsset(source); studio.SetRunning(false);
            yield return null;
            var valve = _host.transform.Find("Power generated lab/Timed valve 11");
            Assert.That(valve, Is.Not.Null);
            float closed = valve.localPosition.y;
            studio.AdvanceOnePresentationStep();
            yield return null;
            Assert.That(valve.localPosition.y, Is.GreaterThan(closed + 0.1f));
            studio.RunReferenceExperiment(); studio.SetRunning(false);
            for (int i = 0; i < 30; ++i) studio.AdvanceOnePresentationStep();
            var asset = source.Load(); var playback = asset.CreatePlayback();
            Assert.That(playback.Advance(asset.DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(studio.StateHash, Is.EqualTo(playback.ReadSnapshot(new Scalar[asset.Model.OutputCount]).StateHash));
            studio.ResetSimulation(); studio.SetRunning(false);
            Assert.That(valve.localPosition.y, Is.EqualTo(closed).Within(1e-6f));
            studio.enabled = false;
            yield return null;
            Assert.That(_host.transform.childCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ImportedGasNetworkShowsVolumesAndReplaysValveEvents()
        {
            _host = new GameObject("Gas model test");
            var studio = _host.AddComponent<PowerStudio>();
            var source = Resources.Load<PowerModelAsset>("GasNetwork");
            Assert.That(source, Is.Not.Null);
            studio.SetModelAsset(source); studio.SetRunning(false);
            yield return null;
            Assert.That(_host.transform.Find("Power generated lab/Gas volume 1"), Is.Not.Null);
            Assert.That(_host.transform.Find("Power generated lab/Gas volume 2"), Is.Not.Null);
            Assert.That(_host.transform.Find("Power generated lab/Reservoir 11"), Is.Not.Null);
            Assert.That(_host.transform.Find("Power generated lab/Gas wall link 12"), Is.Not.Null);
            Assert.That(_host.GetComponentInChildren<UIDocument>().rootVisualElement.Query<TextField>().ToList().Count, Is.EqualTo(2));
            studio.RunReferenceExperiment(); studio.SetRunning(false);
            for (int i = 0; i < 10; ++i) studio.AdvanceOnePresentationStep();
            var asset = source.Load(); var playback = asset.CreatePlayback();
            Assert.That(playback.Advance(asset.DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(studio.StateHash, Is.EqualTo(playback.ReadSnapshot(new Scalar[asset.Model.OutputCount]).StateHash));
            Assert.That(studio.IsRunning, Is.False);
            studio.enabled = false;
            yield return null;
            Assert.That(_host.transform.childCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator ImportedThermalTopologyCanReplaceARunningStudio()
        {
            _host = new GameObject("Imported model test");
            var studio = _host.AddComponent<PowerStudio>();
            var source = Resources.Load<PowerModelAsset>("ThermalNetwork");
            Assert.That(source, Is.Not.Null);
            studio.SetModelAsset(source);
            studio.SetRunning(false);
            yield return null;
            Assert.That(studio.enabled, Is.True);
            Assert.That(studio.ModelName, Is.EqualTo(source.Load().Name));
            Assert.That(_host.transform.childCount, Is.EqualTo(2));
            Assert.That(_host.GetComponentInChildren<UIDocument>().rootVisualElement.Query<TextField>().ToList().Count, Is.EqualTo(0));
            Assert.That(_host.transform.Find("Power generated lab/Thermal node 42"), Is.Not.Null);
            Assert.That(_host.transform.Find("Power generated lab/Thermal node 77"), Is.Not.Null);
            studio.AdvanceOnePresentationStep();
            Assert.That(studio.SimulationTimeNanoseconds, Is.EqualTo(14_000_000UL));
            studio.RunReferenceExperiment();
            studio.SetRunning(false);
            for (int i = 0; i < 500; ++i) studio.AdvanceOnePresentationStep();
            var playback = source.Load().CreatePlayback();
            Assert.That(playback.Advance(source.Load().DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            Assert.That(studio.StateHash, Is.EqualTo(playback.ReadSnapshot(new Scalar[playback.Model.OutputCount]).StateHash));
            Assert.That(studio.SimulationTimeNanoseconds, Is.EqualTo(7_000_000_000UL));
            Assert.That(studio.IsRunning, Is.False);
        }
    }
}
