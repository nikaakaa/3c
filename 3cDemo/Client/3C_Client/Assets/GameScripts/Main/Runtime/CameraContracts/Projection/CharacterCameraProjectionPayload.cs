using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CharacterCameraProjectionPayload
    {
        public const string SchemaVersion = "character-camera-projection/v1";

        [SerializeField] string m_Schema = SchemaVersion;
        [SerializeField] string m_ProfileId = string.Empty;
        [SerializeField] string m_ProfileRevision = string.Empty;
        [SerializeField] CameraSequencePayload m_DefaultSequence;
        [SerializeField] CameraSequencePayload[] m_Sequences = Array.Empty<CameraSequencePayload>();
        [SerializeField] CameraOverrideTrackPayload[] m_OverrideTracks = Array.Empty<CameraOverrideTrackPayload>();
        [SerializeField] CameraZoomPayload[] m_Zooms = Array.Empty<CameraZoomPayload>();
        [SerializeField] CameraStretchPayload[] m_Stretches = Array.Empty<CameraStretchPayload>();
        [SerializeField] CameraShakePayload[] m_Shakes = Array.Empty<CameraShakePayload>();
        [SerializeField] CameraShotPayload[] m_Shots = Array.Empty<CameraShotPayload>();
        [SerializeField] CameraCurvePayload[] m_Curves = Array.Empty<CameraCurvePayload>();
        [SerializeField] CameraOrbitPayload m_DefaultSphere;
        [SerializeField] CameraOrbitPayload[] m_DefaultOrbitGroup = Array.Empty<CameraOrbitPayload>();
        [SerializeField] float m_NearClipPlane;
        [SerializeField] float m_FarClipPlane;
        [SerializeField] float m_CameraLocateRadius;
        [SerializeField] float m_DefaultElevationAngle;
        [SerializeField] float m_DefaultFieldOfView;
        [SerializeField] float m_DefaultSmoothTime;
        [SerializeField] float m_RotationTransitionSeconds;
        [SerializeField] float m_ChangeAvatarTransitionSeconds;
        [SerializeField] CameraInputSettings m_Input;
        [SerializeField] CameraLockingSettings m_Locking;
        [SerializeField] CameraCollisionSettings m_Collision;
        [SerializeField] CameraTargetSlotPayload[] m_TargetSlots = Array.Empty<CameraTargetSlotPayload>();

        public CharacterCameraProjectionPayload(
            CharacterCameraProfile profile,
            CameraSequencePayload defaultSequence,
            CameraSequencePayload[] sequences,
            CameraOverrideTrackPayload[] overrideTracks,
            CameraZoomPayload[] zooms,
            CameraStretchPayload[] stretches,
            CameraShakePayload[] shakes,
            CameraShotPayload[] shots,
            CameraCurvePayload[] curves,
            CameraOrbitPayload defaultSphere,
            CameraOrbitPayload[] defaultOrbitGroup,
            CameraTargetSlotPayload[] targetSlots)
        {
            m_ProfileId = profile.ProfileId;
            m_ProfileRevision = profile.Revision;
            m_DefaultSequence = defaultSequence;
            m_Sequences = sequences ?? Array.Empty<CameraSequencePayload>();
            m_OverrideTracks = overrideTracks ?? Array.Empty<CameraOverrideTrackPayload>();
            m_Zooms = zooms ?? Array.Empty<CameraZoomPayload>();
            m_Stretches = stretches ?? Array.Empty<CameraStretchPayload>();
            m_Shakes = shakes ?? Array.Empty<CameraShakePayload>();
            m_Shots = shots ?? Array.Empty<CameraShotPayload>();
            m_Curves = curves ?? Array.Empty<CameraCurvePayload>();
            m_DefaultSphere = defaultSphere;
            m_DefaultOrbitGroup = defaultOrbitGroup ?? Array.Empty<CameraOrbitPayload>();
            m_NearClipPlane = profile.NearClipPlane;
            m_FarClipPlane = profile.FarClipPlane;
            m_CameraLocateRadius = profile.CameraLocateRadius;
            m_DefaultElevationAngle = profile.DefaultElevationAngle;
            m_DefaultFieldOfView = profile.DefaultFieldOfView;
            m_DefaultSmoothTime = profile.DefaultSmoothTime;
            m_RotationTransitionSeconds = profile.RotationTransitionSeconds;
            m_ChangeAvatarTransitionSeconds = profile.ChangeAvatarTransitionSeconds;
            m_Input = new CameraInputSettings(profile.Input);
            m_Locking = new CameraLockingSettings(profile.Locking);
            m_Collision = new CameraCollisionSettings(profile.Collision);
            m_TargetSlots = targetSlots ?? Array.Empty<CameraTargetSlotPayload>();
        }

        public string Schema => m_Schema ?? string.Empty;
        public string ProfileId => m_ProfileId ?? string.Empty;
        public string ProfileRevision => m_ProfileRevision ?? string.Empty;
        public CameraSequencePayload DefaultSequence => m_DefaultSequence;
        public IReadOnlyList<CameraSequencePayload> Sequences => m_Sequences ?? Array.Empty<CameraSequencePayload>();
        public IReadOnlyList<CameraOverrideTrackPayload> OverrideTracks => m_OverrideTracks ?? Array.Empty<CameraOverrideTrackPayload>();
        public IReadOnlyList<CameraZoomPayload> Zooms => m_Zooms ?? Array.Empty<CameraZoomPayload>();
        public IReadOnlyList<CameraStretchPayload> Stretches => m_Stretches ?? Array.Empty<CameraStretchPayload>();
        public IReadOnlyList<CameraShakePayload> Shakes => m_Shakes ?? Array.Empty<CameraShakePayload>();
        public IReadOnlyList<CameraShotPayload> Shots => m_Shots ?? Array.Empty<CameraShotPayload>();
        public IReadOnlyList<CameraCurvePayload> Curves => m_Curves ?? Array.Empty<CameraCurvePayload>();
        public CameraOrbitPayload DefaultSphere => m_DefaultSphere;
        public IReadOnlyList<CameraOrbitPayload> DefaultOrbitGroup => m_DefaultOrbitGroup ?? Array.Empty<CameraOrbitPayload>();
        public float NearClipPlane => m_NearClipPlane;
        public float FarClipPlane => m_FarClipPlane;
        public float CameraLocateRadius => m_CameraLocateRadius;
        public float DefaultElevationAngle => m_DefaultElevationAngle;
        public float DefaultFieldOfView => m_DefaultFieldOfView;
        public float DefaultSmoothTime => m_DefaultSmoothTime;
        public float RotationTransitionSeconds => m_RotationTransitionSeconds;
        public float ChangeAvatarTransitionSeconds => m_ChangeAvatarTransitionSeconds;
        public CameraInputSettings Input => m_Input;
        public CameraLockingSettings Locking => m_Locking;
        public CameraCollisionSettings Collision => m_Collision;
        public IReadOnlyList<CameraTargetSlotPayload> TargetSlots => m_TargetSlots ?? Array.Empty<CameraTargetSlotPayload>();

        public bool TryGetSequence(string sequenceId, out CameraSequencePayload payload)
        {
            if (DefaultSequence != null && string.Equals(DefaultSequence.SequenceId, sequenceId, StringComparison.Ordinal))
            {
                payload = DefaultSequence;
                return true;
            }
            for (int i = 0; i < Sequences.Count; i++)
            {
                CameraSequencePayload candidate = Sequences[i];
                if (candidate != null && string.Equals(candidate.SequenceId, sequenceId, StringComparison.Ordinal))
                {
                    payload = candidate;
                    return true;
                }
            }
            payload = null;
            return false;
        }

        public bool TryGetOverride(string resourceId, out CameraOverrideTrackPayload payload) =>
            TryFind(OverrideTracks, resourceId, value => value.TrackId, out payload);

        public bool TryGetZoom(string resourceId, out CameraZoomPayload payload) =>
            TryFind(Zooms, resourceId, value => value.ZoomId, out payload);

        public bool TryGetStretch(string resourceId, out CameraStretchPayload payload) =>
            TryFind(Stretches, resourceId, value => value.StretchId, out payload);

        public bool TryGetShake(string resourceId, out CameraShakePayload payload) =>
            TryFind(Shakes, resourceId, value => value.ShakeId, out payload);

        public bool TryGetShot(string resourceId, out CameraShotPayload payload) =>
            TryFind(Shots, resourceId, value => value.ShotId, out payload);

        static bool TryFind<T>(
            IReadOnlyList<T> values,
            string resourceId,
            Func<T, string> identity,
            out T result) where T : class
        {
            for (int i = 0; i < values.Count; i++)
            {
                T value = values[i];
                if (value != null && string.Equals(identity(value), resourceId, StringComparison.Ordinal))
                {
                    result = value;
                    return true;
                }
            }
            result = default;
            return false;
        }

        public void RequireValid()
        {
            if (!string.Equals(Schema, SchemaVersion, StringComparison.Ordinal) || string.IsNullOrWhiteSpace(ProfileId) ||
                string.IsNullOrWhiteSpace(ProfileRevision) || DefaultSequence == null || DefaultSphere == null ||
                Input == null || Locking == null || Collision == null || DefaultOrbitGroup.Count != 3 ||
                !float.IsFinite(NearClipPlane) ||
                NearClipPlane < 0f || !float.IsFinite(FarClipPlane) || FarClipPlane <= NearClipPlane ||
                !float.IsFinite(CameraLocateRadius) || CameraLocateRadius <= 0f ||
                !float.IsFinite(DefaultElevationAngle) || !float.IsFinite(DefaultFieldOfView) || DefaultFieldOfView <= 0f ||
                !float.IsFinite(DefaultSmoothTime) || DefaultSmoothTime < 0f ||
                !float.IsFinite(RotationTransitionSeconds) || RotationTransitionSeconds < 0f ||
                !float.IsFinite(ChangeAvatarTransitionSeconds) || ChangeAvatarTransitionSeconds < 0f)
                throw new InvalidOperationException("Character Camera Projection payload is incomplete.");
            DefaultSphere.RequireValid("Character Camera Projection DefaultSphere");
            for (int i = 0; i < DefaultOrbitGroup.Count; i++)
            {
                CameraOrbitPayload orbit = DefaultOrbitGroup[i];
                if (orbit == null)
                    throw new InvalidOperationException($"Character Camera Projection DefaultOrbitGroup[{i}] is missing.");
                orbit.RequireValid($"Character Camera Projection DefaultOrbitGroup[{i}]");
            }
            if (Collision.Enabled)
                throw new InvalidOperationException("Character Camera Projection enables collision, but the formal camera collision consumer is not published.");
        }
    }
}
