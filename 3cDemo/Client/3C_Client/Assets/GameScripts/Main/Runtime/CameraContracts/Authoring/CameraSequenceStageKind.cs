using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
public enum CameraSequenceStageKind : byte
    {
        FrameOnePointByHeight = 1,
        FrameOnePointByScreenOffset = 2,
        FrameOnePointByTrack = 3,
        FrameTwoPointsChat = 4,
        FrameMultiplePointsChat = 5,
        FrameOneEntity = 6,
        FrameTwoEntities = 7,
        FrameMultipleEntities = 8,
        FixedInCoreSpace = 9,
        HandleCameraVolume = 10,
        RotationEulerOffset = 11,
        RotationLast = 12
    }
}
