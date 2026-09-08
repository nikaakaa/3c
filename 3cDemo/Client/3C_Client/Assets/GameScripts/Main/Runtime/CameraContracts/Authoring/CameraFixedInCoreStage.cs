using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraFixedInCoreStage : CameraSequenceStage
    {
        [SerializeField] string m_FixedPolicyId = string.Empty;
        [SerializeField] string m_ActiveChannel = string.Empty;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FixedInCoreSpace;
        public string FixedPolicyId => m_FixedPolicyId ?? string.Empty;
        public string ActiveChannel => m_ActiveChannel ?? string.Empty;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (string.IsNullOrWhiteSpace(FixedPolicyId) || string.IsNullOrWhiteSpace(ActiveChannel))
                throw new InvalidOperationException($"{source} contains invalid fixed-space framing bindings.");
        }
    }
}
