using System.IO;
using NUnit.Framework;
using Power.Assets;
using Power.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Power.Studio.Tests
{
    public sealed class CoreIntegrationTests
    {
        [Test]
        public void ProjectAndManagedAssemblyAreReadyInsideUnity()
        {
            Editor.ProjectSetup.Validate();
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.Not.Null);
            Assert.That(Resources.Load<Material>("PowerLit"), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(Editor.ProjectSetup.ScenePath), Is.Not.Null);
        }

        [Test]
        public void UnityRuntimePreservesBatchReplay()
        {
            var model = CompiledModel.Compile(SampleModels.Electrothermal());
            var a = model.CreateSimulation();
            var b = model.CreateSimulation();
            Assert.That(a.Step(1_000_000_000), Is.EqualTo(SimulationStatus.Ok));
            for (int i = 0; i < 50; ++i)
                Assert.That(b.Step(PowerStudio.PresentationStepNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            var values = new Scalar[model.OutputCount];
            Assert.That(a.ReadSnapshot(values).StateHash, Is.EqualTo(b.ReadSnapshot(values).StateHash));
        }

        [TestCase("Electrothermal")]
        [TestCase("ThermalNetwork")]
        public void ScriptedImportPreservesPortablePayloadAndReplay(string resource)
        {
            var imported = Resources.Load<PowerModelAsset>(resource);
            Assert.That(imported, Is.Not.Null);
            var asset = imported.Load();
            var decoded = AssetCodec.Decode(File.ReadAllBytes(AssetDatabase.GetAssetPath(imported)));
            Assert.That(asset.Model.Fingerprint, Is.EqualTo(decoded.Model.Fingerprint));
            Assert.That(asset.SourceSha256, Is.EqualTo(decoded.SourceSha256));
            var a = asset.CreatePlayback(); var b = decoded.CreatePlayback();
            Assert.That(a.Advance(asset.DurationNanoseconds), Is.EqualTo(SimulationStatus.Ok));
            while (!b.Completed)
                Assert.That(b.Advance(System.Math.Min(257 * asset.Model.StepNanoseconds, asset.DurationNanoseconds - b.TimeNanoseconds)), Is.EqualTo(SimulationStatus.Ok));
            var values = new Scalar[asset.Model.OutputCount];
            Assert.That(a.ReadSnapshot(values).StateHash, Is.EqualTo(b.ReadSnapshot(values).StateHash));
        }
    }
}
