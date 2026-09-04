using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraRotationEulerOffsetStage : CameraSequenceStage
    {
        [SerializeField] Vector3 m_Offset;
        [SerializeField] bool m_FlipForward;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.RotationEulerOffset;
        public Vector3 Offset => m_Offset;
        public bool FlipForward => m_FlipForward;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(Offset.x) || !float.IsFinite(Offset.y) || !float.IsFinite(Offset.z) || FlipForward)
                throw new InvalidOperationException($"{source} contains invalid Euler rotation offsets.");
        }
    }
}
