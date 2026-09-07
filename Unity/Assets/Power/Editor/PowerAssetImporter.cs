// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System;
using System.IO;
using Power.Assets;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Power.Studio.Editor
{
    [ScriptedImporter(1, "powerasset")]
    public sealed class PowerAssetImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext context)
        {
            if (new FileInfo(context.assetPath).Length > AssetCodec.MaxBytes)
                throw new AssetFormatException("Power asset exceeds 1 MiB.");
            var asset = ScriptableObject.CreateInstance<PowerModelAsset>();
            try
            {
                asset.Initialize(File.ReadAllBytes(context.assetPath));
                asset.name = asset.Load().Name;
                context.AddObjectToAsset("model", asset);
                context.SetMainObject(asset);
            }
            catch (Exception error)
            {
                DestroyImmediate(asset);
                context.LogImportError("Power model import failed: " + error.Message);
            }
        }
    }

    [CustomEditor(typeof(PowerModelAsset))]
    public sealed class PowerModelAssetInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var source = (PowerModelAsset)target;
            try
            {
                var asset = source.Load();
                EditorGUILayout.LabelField(asset.Name, EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Model", asset.Model.Fingerprint.ToString("x16"));
                EditorGUILayout.LabelField("Source SHA-256", asset.SourceSha256);
                EditorGUILayout.LabelField("Topology", asset.Nodes.Count + " nodes / " + asset.Components.Count + " components");
                EditorGUILayout.LabelField("Calibration", asset.Model.Calibration);
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    if (!GUILayout.Button("Open in Studio") || !ProjectSetup.TryOpenLaboratory()) return;
                    var studio = UnityEngine.Object.FindFirstObjectByType<PowerStudio>();
                    if (studio != null)
                    {
                        Undo.RecordObject(studio, "Select Power model");
                        studio.SetModelAsset(source);
                        EditorUtility.SetDirty(studio);
                        EditorSceneManager.MarkSceneDirty(studio.gameObject.scene);
                        Selection.activeGameObject = studio.gameObject;
                    }
                }
            }
            catch (Exception error) { EditorGUILayout.HelpBox(error.Message, MessageType.Error); }
        }
    }
}
