using System;
using UnityEngine.Rendering;

namespace ThirdPersonRendering
{
    [Serializable]
    public sealed class GlitchModeParameter : VolumeParameter<GlitchMode>
    {
        public GlitchModeParameter(GlitchMode value, bool overrideState = false)
            : base(value, overrideState)
        {
        }
    }
}
