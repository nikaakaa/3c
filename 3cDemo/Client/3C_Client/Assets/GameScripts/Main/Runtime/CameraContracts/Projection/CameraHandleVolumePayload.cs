using System;
using UnityEngine;

namespace ThirdPersonCamera
{
    [Serializable]
    public sealed class CameraHandleVolumePayload : CameraSequenceStagePayload
    {
        [SerializeField] string m_CollisionDataId = string.Empty;
        [SerializeField] bool m_HandleLineOfSightCollision;
        [SerializeField] float m_NearClipPlane;

        public CameraHandleVolumePayload(
            string stageId,
            string collisionDataId,
            bool handleLineOfSightCollision,
            float nearClipPlane)
            : base(stageId, CameraSequenceStageKind.HandleCameraVolume)
        {
            m_CollisionDataId = collisionDataId ?? string.Empty;
            m_HandleLineOfSightCollision = handleLineOfSightCollision;
            m_NearClipPlane = nearClipPlane;
        }

        public string CollisionDataId => m_CollisionDataId ?? string.Empty;
        public bool HandleLineOfSightCollision => m_HandleLineOfSightCollision;
        public float NearClipPlane => m_NearClipPlane;
    }
}
