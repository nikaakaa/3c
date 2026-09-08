using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraRotationLastStage : CameraSequenceStage
    {
        [SerializeField] Vector3 m_OverrideRotation;
        [SerializeField] Vector3 m_Rotation;
        [SerializeField] bool m_UseRelativeYaw;
        [SerializeField] string m_LastCameraDataId = string.Empty;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.RotationLast;
        public Vector3 OverrideRotation => m_OverrideRotation;
        public Vector3 Rotation => m_Rotation;
        public bool UseRelativeYaw => m_UseRelativeYaw;
        public string LastCameraDataId => m_LastCameraDataId ?? string.Empty;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (!float.IsFinite(OverrideRotation.x) || !float.IsFinite(OverrideRotation.y) ||
                !float.IsFinite(OverrideRotation.z) || !float.IsFinite(Rotation.x) ||
                !float.IsFinite(Rotation.y) || !float.IsFinite(Rotation.z) ||
                string.IsNullOrWhiteSpace(LastCameraDataId))
                throw new InvalidOperationException($"{source} contains invalid last-rotation settings.");
        }
    }
}
