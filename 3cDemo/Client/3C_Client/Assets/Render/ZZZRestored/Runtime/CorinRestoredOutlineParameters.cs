using System;
using UnityEngine;

namespace ZZZ.Rendering.Restored
{
    [Serializable]
    public struct CorinRestoredOutlineParameters
    {
        public Vector4 CharacterStyle;
        public Vector4 PostTint;
        public Vector4 BloomThreshold;
        public Vector4 AlphaBlend;
        public float GlobalMipBias;
    }
}
