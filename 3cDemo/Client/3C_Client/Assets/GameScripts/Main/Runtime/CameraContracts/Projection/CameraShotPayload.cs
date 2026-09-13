using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CameraShotPayload
    {
        [SerializeField] string m_ShotId = string.Empty;
        [SerializeField] string m_CinePrefabPath = string.Empty;
        [SerializeField] string m_FollowTargetSlotId = string.Empty;
        [SerializeField] string m_LookAtTargetSlotId = string.Empty;
        [SerializeField] float m_NearClipPlane;
        [SerializeField] float m_FarClipPlane;
        [SerializeField] float m_Duration;
        [SerializeField] CameraTimeDomain m_TimeDomain;
        [SerializeField] bool m_IgnoreCameraCollision;
        [SerializeField] bool m_ApplyEntityTimeScale;
        [SerializeField] Vector3 m_FollowOffset;
        [SerializeField] Vector3 m_LookAtOffset;
        [SerializeField] Vector3 m_OffsetRotation;
        [SerializeField] float m_FieldOfView;
        [SerializeField] CameraShotBlendPayload m_BlendIn;
        [SerializeField] CameraShotBlendPayload m_BlendOut;
        [SerializeField] bool m_BlendWithIgnoreLookAtTarget;
        [SerializeField] int m_Priority;
        [SerializeField] string m_Tag = string.Empty;

        public CameraShotPayload(
            string shotId,
            string cinePrefabPath,
            string followTargetSlotId,
            string lookAtTargetSlotId,
            float nearClipPlane,
            float farClipPlane,
            float duration,
            CameraTimeDomain timeDomain,
            bool ignoreCameraCollision,
            bool applyEntityTimeScale,
            Vector3 followOffset,
            Vector3 lookAtOffset,
            Vector3 offsetRotation,
            float fieldOfView,
            CameraShotBlendPayload blendIn,
            CameraShotBlendPayload blendOut,
            bool blendWithIgnoreLookAtTarget,
            int priority,
            string tag)
        {
            m_ShotId = shotId ?? string.Empty;
            m_CinePrefabPath = cinePrefabPath ?? string.Empty;
            m_FollowTargetSlotId = followTargetSlotId ?? string.Empty;
            m_LookAtTargetSlotId = lookAtTargetSlotId ?? string.Empty;
            m_NearClipPlane = nearClipPlane;
            m_FarClipPlane = farClipPlane;
            m_Duration = duration;
            m_TimeDomain = timeDomain;
            m_IgnoreCameraCollision = ignoreCameraCollision;
            m_ApplyEntityTimeScale = applyEntityTimeScale;
            m_FollowOffset = followOffset;
            m_LookAtOffset = lookAtOffset;
            m_OffsetRotation = offsetRotation;
            m_FieldOfView = fieldOfView;
            m_BlendIn = blendIn;
            m_BlendOut = blendOut;
            m_BlendWithIgnoreLookAtTarget = blendWithIgnoreLookAtTarget;
            m_Priority = priority;
            m_Tag = tag ?? string.Empty;
        }

        public string ShotId => m_ShotId ?? string.Empty;
        public string CinePrefabPath => m_CinePrefabPath ?? string.Empty;
        public string FollowTargetSlotId => m_FollowTargetSlotId ?? string.Empty;
        public string LookAtTargetSlotId => m_LookAtTargetSlotId ?? string.Empty;
        public float NearClipPlane => m_NearClipPlane;
        public float FarClipPlane => m_FarClipPlane;
        public float Duration => m_Duration;
        public CameraTimeDomain TimeDomain => m_TimeDomain;
        public bool IgnoreCameraCollision => m_IgnoreCameraCollision;
        public bool ApplyEntityTimeScale => m_ApplyEntityTimeScale;
        public Vector3 FollowOffset => m_FollowOffset;
        public Vector3 LookAtOffset => m_LookAtOffset;
        public Vector3 OffsetRotation => m_OffsetRotation;
        public float FieldOfView => m_FieldOfView;
        public CameraShotBlendPayload BlendIn => m_BlendIn;
        public CameraShotBlendPayload BlendOut => m_BlendOut;
        public bool BlendWithIgnoreLookAtTarget => m_BlendWithIgnoreLookAtTarget;
        public int Priority => m_Priority;
        public string Tag => m_Tag ?? string.Empty;

        public void RequireValid(string source)
        {
            if (string.IsNullOrWhiteSpace(ShotId) || string.IsNullOrWhiteSpace(CinePrefabPath) ||
                string.IsNullOrWhiteSpace(FollowTargetSlotId) || string.IsNullOrWhiteSpace(LookAtTargetSlotId) ||
                !float.IsFinite(NearClipPlane) || NearClipPlane < 0f || !float.IsFinite(FarClipPlane) ||
                FarClipPlane <= NearClipPlane || !float.IsFinite(Duration) || Duration == 0f || Duration < -1f ||
                !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain) || !Finite(FollowOffset) ||
                !Finite(LookAtOffset) || !Finite(OffsetRotation) || !float.IsFinite(FieldOfView) ||
                FieldOfView <= 0f || BlendIn == null || BlendOut == null || string.IsNullOrWhiteSpace(Tag))
                throw new InvalidOperationException($"{source} contains an invalid Camera Shot payload.");
            BlendIn.RequireValid(source + ".BlendIn");
            BlendOut.RequireValid(source + ".BlendOut");
        }

        static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
