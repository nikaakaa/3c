using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ThirdPersonCamera
{
[CreateAssetMenu(fileName = "CameraShot", menuName = "3C/Character/Camera/Shot")]
    public sealed class CameraShotAsset : CameraEffectAsset
    {
        public const string SchemaVersion = "character-camera-shot/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_ShotId = string.Empty;
        [SerializeField] string m_CinePrefabPath = string.Empty;
        [SerializeField] string m_FollowTargetSlotId = string.Empty;
        [SerializeField] string m_LookAtTargetSlotId = string.Empty;
        [SerializeField] float m_NearClipPlane = 0.05f;
        [SerializeField] float m_FarClipPlane = 1000f;
        [SerializeField] float m_Duration = -1f;
        [SerializeField] CameraTimeDomain m_TimeDomain = CameraTimeDomain.PresentationScaled;
        [SerializeField] bool m_IgnoreCameraCollision;
        [SerializeField] bool m_ApplyEntityTimeScale;
        [SerializeField] Vector3 m_FollowOffset;
        [SerializeField] Vector3 m_LookAtOffset;
        [SerializeField] Vector3 m_OffsetRotation;
        [SerializeField] float m_FieldOfView = 60f;
        [SerializeField] CameraShotBlendSettings m_BlendIn = new CameraShotBlendSettings();
        [SerializeField] CameraShotBlendSettings m_BlendOut = new CameraShotBlendSettings();
        [SerializeField] bool m_BlendWithIgnoreLookAtTarget;
        [SerializeField] int m_Priority;
        [SerializeField] string m_Tag = string.Empty;

        public string Schema => m_Schema ?? string.Empty;
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
        public CameraShotBlendSettings BlendIn => m_BlendIn;
        public CameraShotBlendSettings BlendOut => m_BlendOut;
        public bool BlendWithIgnoreLookAtTarget => m_BlendWithIgnoreLookAtTarget;
        public int Priority => m_Priority;

        public override string EffectId => ShotId;
        public override int EffectPriority => Priority;
        public string Tag => m_Tag ?? string.Empty;

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(ShotId) ||
                string.IsNullOrWhiteSpace(CinePrefabPath) || string.IsNullOrWhiteSpace(FollowTargetSlotId) ||
                string.IsNullOrWhiteSpace(LookAtTargetSlotId) || !float.IsFinite(NearClipPlane) || NearClipPlane < 0f ||
                !float.IsFinite(FarClipPlane) || FarClipPlane <= NearClipPlane || !float.IsFinite(Duration) ||
                Duration == 0f || Duration < -1f || !Enum.IsDefined(typeof(CameraTimeDomain), TimeDomain) ||
                !float.IsFinite(FollowOffset.x) || !float.IsFinite(FollowOffset.y) || !float.IsFinite(FollowOffset.z) ||
                !float.IsFinite(LookAtOffset.x) || !float.IsFinite(LookAtOffset.y) || !float.IsFinite(LookAtOffset.z) ||
                !float.IsFinite(OffsetRotation.x) || !float.IsFinite(OffsetRotation.y) || !float.IsFinite(OffsetRotation.z) ||
                !float.IsFinite(FieldOfView) || FieldOfView <= 0f || BlendIn == null || BlendOut == null ||
                string.IsNullOrWhiteSpace(Tag))
                throw new InvalidOperationException($"Camera Shot Asset '{name}' is incomplete.");
            BlendIn.RequireValid($"{name}.BlendIn");
            BlendOut.RequireValid($"{name}.BlendOut");
        }
    }
}
