// Copyright (C) 2026 Power! contributors
// Licensed under GPL-3.0-or-later with the Unity Linking Exception.
// See COPYING.NOTICE, LICENSE, and UNITY-LINKING-EXCEPTION.md in the repository root.

using System;
using Power.Assets;
using UnityEngine;

namespace Power.Studio
{
    // Unity serializes the validated portable bytes, not framework-specific C# record internals.
    public sealed class PowerModelAsset : ScriptableObject
    {
        [SerializeField, HideInInspector] private byte[] payload = Array.Empty<byte>();
        [NonSerialized] private PowerAsset loaded;

        public PowerAsset Load()
        {
            if (loaded == null) loaded = AssetCodec.Decode(payload);
            return loaded;
        }

        public void Initialize(byte[] bytes)
        {
            var candidate = AssetCodec.Decode(bytes);
            payload = (byte[])bytes.Clone();
            loaded = candidate;
        }
    }
}
