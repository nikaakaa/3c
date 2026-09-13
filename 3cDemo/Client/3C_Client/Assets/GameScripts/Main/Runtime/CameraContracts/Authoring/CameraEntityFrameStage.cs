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

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.FrameOneEntity;
        public string MainTargetSlotId => m_MainTargetSlotId ?? string.Empty;
        public IReadOnlyList<string> SubTargetSlotIds => m_SubTargetSlotIds ?? Array.Empty<string>();

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (string.IsNullOrWhiteSpace(MainTargetSlotId))
                throw new InvalidOperationException($"{source} contains invalid entity framing bindings.");
        }
    }
}
