using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
public enum CameraEffectKind : byte
    {
        Override = 1,
        Zoom = 2,
        Stretch = 3,
        Shake = 4,
        Shot = 5
    }
}
