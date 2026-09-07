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
