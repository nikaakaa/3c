using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public enum CameraMode
    {
        FreeLook,
        Aim,
        LockOn,
        ActionFocus,
        SkillCloseup
    }

    public enum CameraLookResponseMode
    {
        Full,
        Suppressed,
        Weighted
    }

    public enum CameraInterruptPolicy
    {
        BlendOut,
        Cut,
        HoldUntilSourceEnds
    }

    public enum CameraCueKind
    {
        Shake,
        FovKick,
        Recoil,
        CollisionCorrection,
        Custom
    }
}
