using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraHandleVolumeStage : CameraSequenceStage
    {
        [SerializeField] string m_CollisionDataId = string.Empty;
        [SerializeField] bool m_HandleLineOfSightCollision = true;
        [SerializeField] float m_NearClipPlane = 0.05f;

        public override CameraSequenceStageKind Kind => CameraSequenceStageKind.HandleCameraVolume;
        public string CollisionDataId => m_CollisionDataId ?? string.Empty;
        public bool HandleLineOfSightCollision => m_HandleLineOfSightCollision;
        public float NearClipPlane => m_NearClipPlane;

        public override void RequireValid(string source)
        {
            base.RequireValid(source);
            if (string.IsNullOrWhiteSpace(CollisionDataId) || !float.IsFinite(NearClipPlane) || NearClipPlane < 0f)
                throw new InvalidOperationException($"{source} contains invalid camera volume handling.");
        }
    }
}
