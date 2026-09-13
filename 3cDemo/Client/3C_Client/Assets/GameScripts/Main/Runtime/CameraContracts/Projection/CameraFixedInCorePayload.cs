using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraFixedInCorePayload : CameraSequenceStagePayload
    {
        [SerializeField] string m_FixedPolicyId = string.Empty;
        [SerializeField] string m_ActiveChannel = string.Empty;

        public CameraFixedInCorePayload(
            string stageId,
            string fixedPolicyId,
            string activeChannel)
            : base(stageId, CameraSequenceStageKind.FixedInCoreSpace)
        {
            m_FixedPolicyId = fixedPolicyId ?? string.Empty;
            m_ActiveChannel = activeChannel ?? string.Empty;
        }

        public string FixedPolicyId => m_FixedPolicyId ?? string.Empty;
        public string ActiveChannel => m_ActiveChannel ?? string.Empty;
    }
}
