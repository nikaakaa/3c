using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraEntityFramePayload : CameraSequenceStagePayload
    {
        [SerializeField] string m_MainTargetSlotId = string.Empty;
        [SerializeField] string[] m_SubTargetSlotIds = Array.Empty<string>();

        public CameraEntityFramePayload(
            string stageId,
            CameraSequenceStageKind kind,
            string mainTargetSlotId,
            string[] subTargetSlotIds)
            : base(stageId, kind)
        {
            m_MainTargetSlotId = mainTargetSlotId ?? string.Empty;
            m_SubTargetSlotIds = subTargetSlotIds ?? Array.Empty<string>();
        }

        public string MainTargetSlotId => m_MainTargetSlotId ?? string.Empty;
        public IReadOnlyList<string> SubTargetSlotIds => m_SubTargetSlotIds ?? Array.Empty<string>();
    }
}
