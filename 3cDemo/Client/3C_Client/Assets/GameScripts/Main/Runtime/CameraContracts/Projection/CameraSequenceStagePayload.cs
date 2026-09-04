using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public abstract class CameraSequenceStagePayload
    {
        [SerializeField] string m_StageId = string.Empty;
        [SerializeField] CameraSequenceStageKind m_Kind;
        [SerializeField] bool m_MakeContextDependent;
        [SerializeField] float m_PlayLength;

        protected CameraSequenceStagePayload(
            string stageId,
            CameraSequenceStageKind kind,
            bool makeContextDependent,
            float playLength)
        {
            m_StageId = stageId ?? string.Empty;
            m_Kind = kind;
            m_MakeContextDependent = makeContextDependent;
            m_PlayLength = playLength;
        }

        public string StageId => m_StageId ?? string.Empty;
        public CameraSequenceStageKind Kind => m_Kind;
        public bool MakeContextDependent => m_MakeContextDependent;
        public float PlayLength => m_PlayLength;
    }
}
