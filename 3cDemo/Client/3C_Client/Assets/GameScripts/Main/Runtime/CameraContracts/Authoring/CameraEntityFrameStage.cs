using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public class CameraEntityFrameStage : CameraSequenceStage
    {
        [SerializeField] string m_MainTargetSlotId = string.Empty;
        [SerializeField] string[] m_SubTargetSlotIds = Array.Empty<string>();
        [SerializeField] string m_FramePolicyId = string.Empty;
        [SerializeField] string m_RotationPolicyId = string.Empty;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameOneEntity;
        public string MainTargetSlotId => m_MainTargetSlotId ?? string.Empty;
        public IReadOnlyList<string> SubTargetSlotIds => m_SubTargetSlotIds ?? Array.Empty<string>();
        public string FramePolicyId => m_FramePolicyId ?? string.Empty;
        public string RotationPolicyId => m_RotationPolicyId ?? string.Empty;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (string.IsNullOrWhiteSpace(MainTargetSlotId) || string.IsNullOrWhiteSpace(FramePolicyId) ||
                string.IsNullOrWhiteSpace(RotationPolicyId))
                throw new InvalidOperationException($"{source} contains invalid entity framing bindings.");
        }
    }
}
