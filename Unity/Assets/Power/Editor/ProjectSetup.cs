using System;
using System.IO;
using Power.Core;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Power.Studio.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        private const string Generated = "Assets/Generated";
        private const string PipelinePath = Generated + "/PowerURP.asset";
        public const string ScenePath = "Assets/Scenes/PowerLab.unity";

        static ProjectSetup() { EditorApplication.delayCall += PrepareOnImport; }

        private static void PrepareOnImport()
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling) Prepare();
        }

        [MenuItem("Power/Prepare project")]
        public static void Prepare()
        {
            Directory.CreateDirectory(Generated + "/Resources");
            AssetDatabase.Refresh();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, Generated + "/PowerRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.msaaSampleCount = 4;
                pipeline.supportsHDR = true;
                pipeline.shadowDistance = 40;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Standard);
            if (AssetDatabase.LoadAssetAtPath<Material>(Generated + "/Resources/PowerLit.mat") == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new BuildFailedException("URP Lit shader is unavailable. Restore the pinned Unity packages first.");
                AssetDatabase.CreateAsset(new Material(shader), Generated + "/Resources/PowerLit.mat");
            }
            if (EditorBuildSettings.scenes.Length == 0)
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Power/Open laboratory")]
        public static void OpenLaboratory() => TryOpenLaboratory();

        public static bool TryOpenLaboratory()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return false;
            Prepare();
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
            return true;
        }

        // Batch entry point: -executeMethod Power.Studio.Editor.ProjectSetup.Validate
        public static void Validate()
        {
            Prepare();
            foreach (string resource in new[] { "Electrothermal", "ThermalNetwork" })
            {
                var source = Resources.Load<PowerModelAsset>(resource);
                if (source == null) throw new BuildFailedException("Missing imported model " + resource + ". Run tools/Build.cs build first.");
                var asset = source.Load();
                var playback = asset.CreatePlayback();
                if (playback.Advance(asset.SampleEveryNanoseconds) != SimulationStatus.Ok)
                    throw new BuildFailedException("Portable model playback failed inside Unity: " + resource);
            }
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new BuildFailedException("Laboratory scene is missing.");
            Debug.Log("Power project ready: Unity " + Application.unityVersion + ", portable models imported, compiled and stepped.");
        }

        // Uses Unity's selected -buildTarget and installed platform module.
        public static void BuildPlayer()
        {
            Validate();
            var target = EditorUserBuildSettings.activeBuildTarget;
            string file = target == BuildTarget.StandaloneWindows64 ? "PowerStudio.exe" :
                target == BuildTarget.StandaloneOSX ? "PowerStudio.app" : "PowerStudio.x86_64";
            if (target != BuildTarget.StandaloneWindows64 && target != BuildTarget.StandaloneOSX && target != BuildTarget.StandaloneLinux64)
                throw new BuildFailedException("This laboratory currently targets desktop Windows, macOS and Linux.");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.IL2CPP);
            string destination = Environment.GetEnvironmentVariable("POWER_UNITY_BUILD_DIR");
            if (string.IsNullOrEmpty(destination)) destination = "Builds/" + target;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath }, locationPathName = Path.Combine(destination, file),
                target = target, options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Player build failed: " + report.summary.result);
        }
    }
}
