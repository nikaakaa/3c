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
        [SerializeField] string m_FramePolicyId = string.Empty;
        [SerializeField] string m_RotationPolicyId = string.Empty;

        public CameraEntityFramePayload(
            string stageId,
            CameraSequenceStageKind kind,
            bool makeContextDependent,
            float playLength,
            string mainTargetSlotId,
            string[] subTargetSlotIds,
            string framePolicyId,
            string rotationPolicyId)
            : base(stageId, kind, makeContextDependent, playLength)
        {
            m_MainTargetSlotId = mainTargetSlotId ?? string.Empty;
            m_SubTargetSlotIds = subTargetSlotIds ?? Array.Empty<string>();
            m_FramePolicyId = framePolicyId ?? string.Empty;
            m_RotationPolicyId = rotationPolicyId ?? string.Empty;
        }

        public string MainTargetSlotId => m_MainTargetSlotId ?? string.Empty;
        public IReadOnlyList<string> SubTargetSlotIds => m_SubTargetSlotIds ?? Array.Empty<string>();
        public string FramePolicyId => m_FramePolicyId ?? string.Empty;
        public string RotationPolicyId => m_RotationPolicyId ?? string.Empty;
    }
}
