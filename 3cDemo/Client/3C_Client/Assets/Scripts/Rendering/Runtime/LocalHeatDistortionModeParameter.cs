using System;
using UnityEngine.Rendering;

namespace ThirdPersonRendering
{
    [Serializable]
    public sealed class LocalHeatDistortionModeParameter : VolumeParameter<LocalHeatDistortionMode>
    {
        public LocalHeatDistortionModeParameter(LocalHeatDistortionMode value, bool overrideState = false)
            : base(value, overrideState)
        {
        }
    }
}
