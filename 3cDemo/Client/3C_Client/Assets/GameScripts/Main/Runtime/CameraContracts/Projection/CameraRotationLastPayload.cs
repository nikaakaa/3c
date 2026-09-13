using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraRotationLastPayload : CameraSequenceStagePayload
    {
        [SerializeField] Vector3 m_OverrideRotation;
        [SerializeField] Vector3 m_Rotation;
        [SerializeField] bool m_UseRelativeYaw;
        [SerializeField] string m_LastCameraDataId = string.Empty;

        public CameraRotationLastPayload(
            string stageId,
            Vector3 overrideRotation,
            Vector3 rotation,
            bool useRelativeYaw,
            string lastCameraDataId)
            : base(stageId, CameraSequenceStageKind.RotationLast)
        {
            m_OverrideRotation = overrideRotation;
            m_Rotation = rotation;
            m_UseRelativeYaw = useRelativeYaw;
            m_LastCameraDataId = lastCameraDataId ?? string.Empty;
        }

        public Vector3 OverrideRotation => m_OverrideRotation;
        public Vector3 Rotation => m_Rotation;
        public bool UseRelativeYaw => m_UseRelativeYaw;
        public string LastCameraDataId => m_LastCameraDataId ?? string.Empty;
    }
}
