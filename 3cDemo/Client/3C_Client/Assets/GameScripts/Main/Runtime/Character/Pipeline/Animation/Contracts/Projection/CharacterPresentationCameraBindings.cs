using System;
using ThirdPersonCamera;
using UnityEngine;

namespace ThirdPersonCamera
{
    public enum CharacterPresentationCameraBindingKind
    {
        Response,
        Target,
        Sequence,
        Override,
        Zoom,
        Stretch,
        Shake,
        Shot
    }

    [Serializable]
    public sealed class CharacterPresentationCameraBinding
    {
        [SerializeField] CharacterPresentationCameraBindingKind m_Kind;
        [SerializeField] int m_Priority;
        [SerializeField] float m_BlendInSeconds;
        [SerializeField] float m_BlendOutSeconds;
        [SerializeField] string m_TargetKey = string.Empty;
        [SerializeField] CameraSequenceInterruptPolicy m_InterruptPolicy;
        [SerializeField] CameraResponseMode m_ResponseMode;
        [SerializeField] float m_ManualOrbitWeight;
        [SerializeField] float m_PitchResponseWeight;
        [SerializeField] float m_YawResponseWeight;
        [SerializeField] string m_AnchorKey = string.Empty;
        [SerializeField] string m_AimPointKey = string.Empty;
        [SerializeField] string m_PreferredBoneKey = string.Empty;
        [SerializeField] string m_SequenceId = string.Empty;
        [SerializeField] string m_ResourceId = string.Empty;
        [SerializeField] CameraEffectKind m_EffectKind;

        public CharacterPresentationCameraBindingKind Kind => m_Kind;
        public int Priority => m_Priority;
        public float BlendInSeconds => m_BlendInSeconds;
        public float BlendOutSeconds => m_BlendOutSeconds;
        public string TargetKey => m_TargetKey ?? string.Empty;
        public CameraSequenceInterruptPolicy InterruptPolicy => m_InterruptPolicy;
        public CameraResponseMode ResponseMode => m_ResponseMode;
        public float ManualOrbitWeight => m_ManualOrbitWeight;
        public float PitchResponseWeight => m_PitchResponseWeight;
        public float YawResponseWeight => m_YawResponseWeight;
        public string AnchorKey => m_AnchorKey ?? string.Empty;
        public string AimPointKey => m_AimPointKey ?? string.Empty;
        public string PreferredBoneKey => m_PreferredBoneKey ?? string.Empty;
        public string SequenceId => m_SequenceId ?? string.Empty;
        public string ResourceId => m_ResourceId ?? string.Empty;
        public CameraEffectKind EffectKind => m_EffectKind;

        public static CharacterPresentationCameraBinding Response(
            CameraResponseMode responseMode,
            float manualOrbitWeight,
            float pitchResponseWeight,
            float yawResponseWeight,
            int priority)
        {
            return new CharacterPresentationCameraBinding
            {
                m_Kind = CharacterPresentationCameraBindingKind.Response,
                m_ResponseMode = responseMode,
                m_ManualOrbitWeight = manualOrbitWeight,
                m_PitchResponseWeight = pitchResponseWeight,
                m_YawResponseWeight = yawResponseWeight,
                m_Priority = priority
            };
        }

        public static CharacterPresentationCameraBinding Target(
            string targetKey,
            string anchorKey,
            string aimPointKey,
            string preferredBoneKey,
            int priority)
        {
            return new CharacterPresentationCameraBinding
            {
                m_Kind = CharacterPresentationCameraBindingKind.Target,
                m_TargetKey = targetKey ?? string.Empty,
                m_AnchorKey = anchorKey ?? string.Empty,
                m_AimPointKey = aimPointKey ?? string.Empty,
                m_PreferredBoneKey = preferredBoneKey ?? string.Empty,
                m_Priority = priority
            };
        }

        public static CharacterPresentationCameraBinding Sequence(
            string sequenceId,
            int priority,
            float blendInSeconds,
            float blendOutSeconds,
            string targetKey,
            CameraSequenceInterruptPolicy interruptPolicy)
        {
            return new CharacterPresentationCameraBinding
            {
                m_Kind = CharacterPresentationCameraBindingKind.Sequence,
                m_SequenceId = sequenceId ?? string.Empty,
                m_Priority = priority,
                m_BlendInSeconds = blendInSeconds,
                m_BlendOutSeconds = blendOutSeconds,
                m_TargetKey = targetKey ?? string.Empty,
                m_InterruptPolicy = interruptPolicy
            };
        }

        public static CharacterPresentationCameraBinding Effect(
            CharacterPresentationCameraBindingKind kind,
            string resourceId,
            int priority)
        {
            if (kind != CharacterPresentationCameraBindingKind.Override &&
                kind != CharacterPresentationCameraBindingKind.Zoom &&
                kind != CharacterPresentationCameraBindingKind.Stretch &&
                kind != CharacterPresentationCameraBindingKind.Shake &&
                kind != CharacterPresentationCameraBindingKind.Shot)
                throw new ArgumentOutOfRangeException(nameof(kind));
            return new CharacterPresentationCameraBinding
            {
                m_Kind = kind,
                m_ResourceId = resourceId ?? string.Empty,
                m_EffectKind = ToEffectKind(kind),
                m_Priority = priority
            };
        }

        static CameraEffectKind ToEffectKind(CharacterPresentationCameraBindingKind kind)
        {
            switch (kind)
            {
                case CharacterPresentationCameraBindingKind.Override: return CameraEffectKind.Override;
                case CharacterPresentationCameraBindingKind.Zoom: return CameraEffectKind.Zoom;
                case CharacterPresentationCameraBindingKind.Stretch: return CameraEffectKind.Stretch;
                case CharacterPresentationCameraBindingKind.Shake: return CameraEffectKind.Shake;
                case CharacterPresentationCameraBindingKind.Shot: return CameraEffectKind.Shot;
                default: throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }
        }
    }
}
