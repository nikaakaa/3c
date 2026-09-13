using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public abstract class CameraSequenceStagePayload
    {
        [SerializeField] string m_StageId = string.Empty;
        [SerializeField] CameraSequenceStageKind m_Kind;

        protected CameraSequenceStagePayload(
            string stageId,
            CameraSequenceStageKind kind)
        {
            m_StageId = stageId ?? string.Empty;
            m_Kind = kind;
        }

        public string StageId => m_StageId ?? string.Empty;
        public CameraSequenceStageKind Kind => m_Kind;
    }
}
