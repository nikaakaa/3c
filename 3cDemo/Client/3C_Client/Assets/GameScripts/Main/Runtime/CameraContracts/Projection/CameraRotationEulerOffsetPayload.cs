using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraRotationEulerOffsetPayload : CameraSequenceStagePayload
    {
        [SerializeField] Vector3 m_Offset;
        [SerializeField] bool m_FlipForward;

        public CameraRotationEulerOffsetPayload(
            string stageId,
            bool makeContextDependent,
            float playLength,
            Vector3 offset,
            bool flipForward)
            : base(stageId, CameraSequenceStageKind.RotationEulerOffset, makeContextDependent, playLength)
        {
            m_Offset = offset;
            m_FlipForward = flipForward;
        }

        public Vector3 Offset => m_Offset;
        public bool FlipForward => m_FlipForward;
    }
}
