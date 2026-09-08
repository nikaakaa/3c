using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraTwoEntitiesFrameStage : CameraEntityFrameStage
    {
        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameTwoEntities;
    }
}
