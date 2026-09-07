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
