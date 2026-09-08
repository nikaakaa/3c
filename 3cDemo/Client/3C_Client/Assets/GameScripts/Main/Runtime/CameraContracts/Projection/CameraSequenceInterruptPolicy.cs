using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
public enum CameraSequenceInterruptPolicy : byte
    {
        BlendOut = 0,
        Cut = 1,
        HoldUntilSourceEnds = 2
    }
}
