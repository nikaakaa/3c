using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraSequencePayload
    {
        [SerializeField] string m_SequenceId = string.Empty;
        [SerializeField] CameraTimeDomain m_TimeDomain;
        [SerializeReference] CameraSequenceStagePayload[] m_Stages = Array.Empty<CameraSequenceStagePayload>();

        public CameraSequencePayload(string sequenceId, CameraTimeDomain timeDomain, CameraSequenceStagePayload[] stages)
        {
            m_SequenceId = sequenceId ?? string.Empty;
            m_TimeDomain = timeDomain;
            m_Stages = stages ?? Array.Empty<CameraSequenceStagePayload>();
        }

        public string SequenceId => m_SequenceId ?? string.Empty;
        public CameraTimeDomain TimeDomain => m_TimeDomain;
        public IReadOnlyList<CameraSequenceStagePayload> Stages => m_Stages ?? Array.Empty<CameraSequenceStagePayload>();
    }
}
