using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
[Serializable]
    public sealed class CharacterCameraProjectionPayload
    {
        public const string SchemaVersion = "character-camera-projection/v7";

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
        [SerializeField] float m_NearClipPlane;
        [SerializeField] float m_FarClipPlane;
        [SerializeField] float m_CameraLocateRadius;
        [SerializeField] float m_DefaultSmoothTime;
        [SerializeField] float m_RotationTransitionSeconds;
        [SerializeField] CameraInputSettings m_Input;
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
            m_NearClipPlane = profile.NearClipPlane;
            m_FarClipPlane = profile.FarClipPlane;
            m_CameraLocateRadius = profile.CameraLocateRadius;
            m_DefaultSmoothTime = profile.DefaultSmoothTime;
            m_RotationTransitionSeconds = profile.RotationTransitionSeconds;
            m_Input = new CameraInputSettings(profile.Input);
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
        public CameraOrbitPayload DefaultSphere => ResolveDefaultSphere();
        public float NearClipPlane => m_NearClipPlane;
        public float FarClipPlane => m_FarClipPlane;
        public float CameraLocateRadius => m_CameraLocateRadius;
        public float DefaultFieldOfView => ResolveDefaultFieldOfView();
        public float DefaultSmoothTime => m_DefaultSmoothTime;
        public float RotationTransitionSeconds => m_RotationTransitionSeconds;
        public CameraInputSettings Input => m_Input;
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
                Input == null || Collision == null ||
                !float.IsFinite(NearClipPlane) ||
                NearClipPlane < 0f || !float.IsFinite(FarClipPlane) || FarClipPlane <= NearClipPlane ||
                !float.IsFinite(CameraLocateRadius) || CameraLocateRadius <= 0f ||
                !float.IsFinite(DefaultSmoothTime) || DefaultSmoothTime < 0f ||
                !float.IsFinite(RotationTransitionSeconds) || RotationTransitionSeconds < 0f)
                throw new InvalidOperationException("Character Camera Projection payload is incomplete.");
            DefaultSequence.RequireValid("Character Camera Projection DefaultSequence");
            for (int i = 0; i < Sequences.Count; i++)
            {
                CameraSequencePayload sequence = Sequences[i];
                if (sequence == null)
                    throw new InvalidOperationException($"Character Camera Projection Sequences[{i}] is missing.");
                sequence.RequireValid($"Character Camera Projection Sequences[{i}]");
                if (string.Equals(sequence.SequenceId, DefaultSequence.SequenceId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Character Camera Projection contains the default Sequence twice.");
            }
            Input.RequireValid("Character Camera Projection Input");
            Collision.RequireValid("Character Camera Projection Collision");
            DefaultSphere.RequireValid("Character Camera Projection DefaultSphere");
            RequirePayloads();
        }

        void RequirePayloads()
        {
            for (int i = 0; i < OverrideTracks.Count; i++)
            {
                if (OverrideTracks[i] == null)
                    throw new InvalidOperationException($"Character Camera Projection OverrideTracks[{i}] is missing.");
                OverrideTracks[i].RequireValid($"Character Camera Projection OverrideTracks[{i}]");
            }
            for (int i = 0; i < Zooms.Count; i++)
            {
                if (Zooms[i] == null)
                    throw new InvalidOperationException($"Character Camera Projection Zooms[{i}] is missing.");
                Zooms[i].RequireValid($"Character Camera Projection Zooms[{i}]");
            }
            for (int i = 0; i < Stretches.Count; i++)
            {
                if (Stretches[i] == null)
                    throw new InvalidOperationException($"Character Camera Projection Stretches[{i}] is missing.");
                Stretches[i].RequireValid($"Character Camera Projection Stretches[{i}]");
            }
            for (int i = 0; i < Shakes.Count; i++)
            {
                if (Shakes[i] == null)
                    throw new InvalidOperationException($"Character Camera Projection Shakes[{i}] is missing.");
                Shakes[i].RequireValid($"Character Camera Projection Shakes[{i}]");
            }
            for (int i = 0; i < Shots.Count; i++)
            {
                if (Shots[i] == null)
                    throw new InvalidOperationException($"Character Camera Projection Shots[{i}] is missing.");
                Shots[i].RequireValid($"Character Camera Projection Shots[{i}]");
            }
            for (int i = 0; i < Curves.Count; i++)
            {
                if (Curves[i] == null)
                    throw new InvalidOperationException($"Character Camera Projection Curves[{i}] is missing.");
                Curves[i].RequireValid($"Character Camera Projection Curves[{i}]");
            }
            var slots = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < TargetSlots.Count; i++)
            {
                CameraTargetSlotPayload slot = TargetSlots[i];
                if (slot == null || string.IsNullOrWhiteSpace(slot.SlotId) ||
                    string.Equals(slot.SlotId, CameraTargetBindingKeys.Body, StringComparison.Ordinal) ||
                    !slots.Add(slot.SlotId) ||
                    (byte)slot.Space < (byte)CameraSpace.World ||
                    (byte)slot.Space > (byte)CameraSpace.Camera)
                    throw new InvalidOperationException($"Character Camera Projection TargetSlots[{i}] is invalid.");
                slot.RequireValid($"Character Camera Projection TargetSlots[{i}]");
                if (slot.Space != CameraSpace.World)
                    throw new InvalidOperationException(
                        $"Character Camera Projection TargetSlots[{i}] uses unsupported space '{slot.Space}'.");
            }
            RequireEntityStageSlots(slots);
            for (int i = 0; i < Shots.Count; i++)
            {
                CameraShotPayload shot = Shots[i];
                if (!slots.Contains(shot.FollowTargetSlotId) ||
                    !slots.Contains(shot.LookAtTargetSlotId))
                    throw new InvalidOperationException(
                        $"Camera Shot '{shot.ShotId}' references an unregistered target slot.");
            }
        }

        void RequireEntityStageSlots(HashSet<string> slots)
        {
            RequireEntityStageSlots(DefaultSequence, slots);
            for (int i = 0; i < Sequences.Count; i++)
                RequireEntityStageSlots(Sequences[i], slots);
        }

        static void RequireEntityStageSlots(CameraSequencePayload sequence, HashSet<string> slots)
        {
            for (int i = 0; i < sequence.Stages.Count; i++)
            {
                if (sequence.Stages[i] is not CameraEntityFramePayload entity)
                    continue;
                if (!slots.Contains(entity.MainTargetSlotId))
                    throw new InvalidOperationException(
                        $"Camera Sequence '{sequence.SequenceId}' references missing target slot '{entity.MainTargetSlotId}'.");
                for (int subIndex = 0; subIndex < entity.SubTargetSlotIds.Count; subIndex++)
                    if (!slots.Contains(entity.SubTargetSlotIds[subIndex]))
                        throw new InvalidOperationException(
                            $"Camera Sequence '{sequence.SequenceId}' references missing target slot '{entity.SubTargetSlotIds[subIndex]}'.");
            }
        }

        CameraOrbitPayload ResolveDefaultSphere()
        {
            for (int i = 0; i < DefaultSequence.Stages.Count; i++)
            {
                if (DefaultSequence.Stages[i] is not CameraFrameOnePointByTrackPayload track)
                    continue;
                if (track.CameraOrbits.Count != 3)
                    throw new InvalidOperationException("Character Camera Projection default track must contain three orbits.");
                CameraTrackOrbitPayload orbit = track.CameraOrbits[1];
                return new CameraOrbitPayload(orbit.Height, orbit.Radius);
            }
            throw new InvalidOperationException("Character Camera Projection default Sequence has no track stage.");
        }

        float ResolveDefaultFieldOfView()
        {
            for (int i = 0; i < DefaultSequence.Stages.Count; i++)
            {
                switch (DefaultSequence.Stages[i])
                {
                    case CameraFrameOnePointByHeightPayload value:
                        return value.FieldOfView;
                    case CameraFrameOnePointByScreenOffsetPayload value:
                        return value.FieldOfView;
                    case CameraFrameOnePointByTrackPayload value:
                        return value.FieldOfView;
                    case CameraFrameTwoPointsPayload value:
                        return value.FieldOfView;
                    case CameraFrameMultiplePointsPayload value:
                        return value.FieldOfView;
                }
            }
            throw new InvalidOperationException(
                "Character Camera Projection default Sequence has no stage-owned field of view.");
        }
    }
}
