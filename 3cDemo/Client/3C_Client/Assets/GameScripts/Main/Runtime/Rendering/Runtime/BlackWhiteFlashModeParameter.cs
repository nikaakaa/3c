using System;
using UnityEngine.Rendering;

namespace ThirdPersonRendering
{
    [Serializable]
    public sealed class BlackWhiteFlashModeParameter : VolumeParameter<BlackWhiteFlashMode>
    {
        public BlackWhiteFlashModeParameter(BlackWhiteFlashMode value, bool overrideState = false)
            : base(value, overrideState)
        {
        }
    }
}
