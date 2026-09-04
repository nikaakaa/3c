using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
public enum CameraEffectStage : byte
    {
        Sequence = 1,
        Override = 2,
        Zoom = 3,
        Stretch = 4,
        Shake = 5,
        Shot = 6,
        Collision = 7
    }
}
