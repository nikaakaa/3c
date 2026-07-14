using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public enum CameraMode
    {
        FreeLook,
        Aim,
        LockOn,
        ActionFocus,
        SkillCloseup
    }

    public enum CameraLookResponseMode
    {
        Full,
        Suppressed,
        Weighted
    }

    public enum CameraInterruptPolicy
    {
        BlendOut,
        Cut,
        HoldUntilSourceEnds
    }

    public enum CameraCueKind
    {
        Shake,
        FovKick,
        Recoil,
        CollisionCorrection,
        Custom
    }

    public readonly struct CameraStateRequest
    {
        public CameraStateRequest(
            CameraMode mode,
            int priority,
            float weight,
            float blendInSeconds,
            float blendOutSeconds,
            string targetKey,
            string sourceId,
            string sourceName,
            ulong sourceActionInstanceId,
            CameraInterruptPolicy interruptPolicy)
        {
            Mode = mode;
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            BlendInSeconds = Mathf.Max(0f, blendInSeconds);
            BlendOutSeconds = Mathf.Max(0f, blendOutSeconds);
            TargetKey = targetKey ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
            InterruptPolicy = interruptPolicy;
        }

        public CameraMode Mode { get; }
        public int Priority { get; }
        public float Weight { get; }
        public float BlendInSeconds { get; }
        public float BlendOutSeconds { get; }
        public string TargetKey { get; }
        public string SourceId { get; }
        public string SourceName { get; }
        public ulong SourceActionInstanceId { get; }
        public CameraInterruptPolicy InterruptPolicy { get; }
        public bool Active => Weight > 0f;

        public static CameraStateRequest FreeLookBase => new CameraStateRequest(
            CameraMode.FreeLook,
            int.MinValue,
            1f,
            0f,
            0f,
            string.Empty,
            "camera.base.freelook",
            "FreeLook",
            0,
            CameraInterruptPolicy.BlendOut);
    }

    public readonly struct CameraResponsePolicy
    {
        public CameraResponsePolicy(
            CameraLookResponseMode lookResponse,
            float manualOrbitWeight,
            float pitchResponseWeight,
            float yawResponseWeight,
            int priority,
            float weight,
            string sourceId,
            ulong sourceActionInstanceId)
        {
            LookResponse = lookResponse;
            ManualOrbitWeight = Mathf.Clamp01(manualOrbitWeight);
            PitchResponseWeight = Mathf.Clamp01(pitchResponseWeight);
            YawResponseWeight = Mathf.Clamp01(yawResponseWeight);
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            SourceId = sourceId ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        public CameraLookResponseMode LookResponse { get; }
        public float ManualOrbitWeight { get; }
        public float PitchResponseWeight { get; }
        public float YawResponseWeight { get; }
        public int Priority { get; }
        public float Weight { get; }
        public string SourceId { get; }
        public ulong SourceActionInstanceId { get; }
        public bool Active => Weight > 0f;

        public Vector2 Apply(Vector2 lookDelta)
        {
            switch (LookResponse)
            {
                case CameraLookResponseMode.Suppressed:
                    return Vector2.zero;
                case CameraLookResponseMode.Weighted:
                    return new Vector2(
                        lookDelta.x * ManualOrbitWeight * YawResponseWeight,
                        lookDelta.y * ManualOrbitWeight * PitchResponseWeight);
                default:
                    return lookDelta;
            }
        }

        public static CameraResponsePolicy Full => new CameraResponsePolicy(
            CameraLookResponseMode.Full,
            1f,
            1f,
            1f,
            int.MinValue,
            1f,
            "camera.response.full",
            0);
    }

    public readonly struct CameraCue
    {
        public CameraCue(
            string cueId,
            CameraCueKind cueKind,
            string cueType,
            float intensity,
            float durationSeconds,
            int priority,
            string sourceId,
            string sourceName,
            ulong sourceActionInstanceId)
        {
            CueId = cueId ?? string.Empty;
            CueKind = cueKind;
            CueType = cueType ?? string.Empty;
            Intensity = Mathf.Max(0f, intensity);
            DurationSeconds = Mathf.Max(0f, durationSeconds);
            Priority = priority;
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        public string CueId { get; }
        public CameraCueKind CueKind { get; }
        public string CueType { get; }
        public float Intensity { get; }
        public float DurationSeconds { get; }
        public int Priority { get; }
        public string SourceId { get; }
        public string SourceName { get; }
        public ulong SourceActionInstanceId { get; }
        public bool Active => Intensity > 0f;
    }

    public readonly struct CameraTargetRequest
    {
        public CameraTargetRequest(
            string targetKey,
            string anchorKey,
            string aimPointKey,
            string preferredBoneKey,
            int priority,
            float weight,
            string sourceId,
            ulong sourceActionInstanceId)
        {
            TargetKey = targetKey ?? string.Empty;
            AnchorKey = anchorKey ?? string.Empty;
            AimPointKey = aimPointKey ?? string.Empty;
            PreferredBoneKey = preferredBoneKey ?? string.Empty;
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            SourceId = sourceId ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        public string TargetKey { get; }
        public string AnchorKey { get; }
        public string AimPointKey { get; }
        public string PreferredBoneKey { get; }
        public int Priority { get; }
        public float Weight { get; }
        public string SourceId { get; }
        public ulong SourceActionInstanceId { get; }
        public bool Active => Weight > 0f;
        public bool HasAnyKey => !string.IsNullOrEmpty(TargetKey) ||
                                 !string.IsNullOrEmpty(AnchorKey) ||
                                 !string.IsNullOrEmpty(AimPointKey) ||
                                 !string.IsNullOrEmpty(PreferredBoneKey);
    }

    public readonly struct CameraBasisSnapshot
    {
        public CameraBasisSnapshot(
            Vector3 planarForward,
            Vector3 planarRight,
            Vector3 lookDirection,
            Vector3 aimPoint,
            float yaw,
            float pitch,
            bool valid)
        {
            PlanarForward = planarForward;
            PlanarRight = planarRight;
            LookDirection = lookDirection;
            AimPoint = aimPoint;
            Yaw = yaw;
            Pitch = pitch;
            Valid = valid;
        }

        public Vector3 PlanarForward { get; }
        public Vector3 PlanarRight { get; }
        public Vector3 LookDirection { get; }
        public Vector3 AimPoint { get; }
        public float Yaw { get; }
        public float Pitch { get; }
        public bool Valid { get; }

        public static CameraBasisSnapshot Invalid => default;
    }

    public readonly struct CameraPosePlan
    {
        public CameraPosePlan(
            CameraMode mode,
            Vector3 followPoint,
            Vector3 aimPoint,
            float fieldOfView,
            CameraResponsePolicy responsePolicy,
            Vector2 lookDelta,
            string sourceId,
            ulong sourceActionInstanceId,
            float blendProgress,
            bool valid)
        {
            Mode = mode;
            FollowPoint = followPoint;
            AimPoint = aimPoint;
            FieldOfView = Mathf.Max(1f, fieldOfView);
            ResponsePolicy = responsePolicy;
            LookDelta = lookDelta;
            SourceId = sourceId ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
            BlendProgress = Mathf.Clamp01(blendProgress);
            Valid = valid;
        }

        public CameraMode Mode { get; }
        public Vector3 FollowPoint { get; }
        public Vector3 AimPoint { get; }
        public float FieldOfView { get; }
        public CameraResponsePolicy ResponsePolicy { get; }
        public Vector2 LookDelta { get; }
        public string SourceId { get; }
        public ulong SourceActionInstanceId { get; }
        public float BlendProgress { get; }
        public bool Valid { get; }
    }

    public readonly struct CameraDebugRequestEntry
    {
        public CameraDebugRequestEntry(CameraMode mode, string sourceId, ulong sourceActionInstanceId, int priority, float weight)
        {
            Mode = mode;
            SourceId = sourceId ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
            Priority = priority;
            Weight = weight;
        }

        public CameraMode Mode { get; }
        public string SourceId { get; }
        public ulong SourceActionInstanceId { get; }
        public int Priority { get; }
        public float Weight { get; }
    }

    public readonly struct CameraDebugCueEntry
    {
        public CameraDebugCueEntry(CameraCueKind cueKind, string cueId, string sourceId, ulong sourceActionInstanceId, float intensity)
        {
            CueKind = cueKind;
            CueId = cueId ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
            Intensity = intensity;
        }

        public CameraCueKind CueKind { get; }
        public string CueId { get; }
        public string SourceId { get; }
        public ulong SourceActionInstanceId { get; }
        public float Intensity { get; }
    }

    public sealed class CameraDebugSnapshot
    {
        readonly List<CameraDebugRequestEntry> m_Requests = new List<CameraDebugRequestEntry>();
        readonly List<CameraDebugCueEntry> m_Cues = new List<CameraDebugCueEntry>();

        public CameraMode Mode { get; private set; }
        public string SourceId { get; private set; } = string.Empty;
        public ulong SourceActionInstanceId { get; private set; }
        public float BlendProgress { get; private set; }
        public CameraResponsePolicy ResponsePolicy { get; private set; } = CameraResponsePolicy.Full;
        public CameraBasisSnapshot Basis { get; private set; }
        public CameraPosePlan PosePlan { get; private set; }
        public string TargetSource { get; private set; } = string.Empty;
        public IReadOnlyList<CameraDebugRequestEntry> Requests => m_Requests;
        public IReadOnlyList<CameraDebugCueEntry> Cues => m_Cues;

        public void Set(
            CameraPosePlan posePlan,
            CameraBasisSnapshot basis,
            string targetSource,
            IReadOnlyList<CameraStateRequest> requests,
            IReadOnlyList<CameraCue> cues)
        {
            Mode = posePlan.Mode;
            SourceId = posePlan.SourceId;
            SourceActionInstanceId = posePlan.SourceActionInstanceId;
            BlendProgress = posePlan.BlendProgress;
            ResponsePolicy = posePlan.ResponsePolicy;
            Basis = basis;
            PosePlan = posePlan;
            TargetSource = targetSource ?? string.Empty;
            m_Requests.Clear();
            m_Cues.Clear();

            if (requests != null)
            {
                for (int i = 0; i < requests.Count; i++)
                {
                    CameraStateRequest request = requests[i];
                    m_Requests.Add(new CameraDebugRequestEntry(
                        request.Mode,
                        request.SourceId,
                        request.SourceActionInstanceId,
                        request.Priority,
                        request.Weight));
                }
            }

            if (cues != null)
            {
                for (int i = 0; i < cues.Count; i++)
                {
                    CameraCue cue = cues[i];
                    m_Cues.Add(new CameraDebugCueEntry(
                        cue.CueKind,
                        cue.CueId,
                        cue.SourceId,
                        cue.SourceActionInstanceId,
                        cue.Intensity));
                }
            }
        }

        public void Clear()
        {
            Mode = CameraMode.FreeLook;
            SourceId = string.Empty;
            SourceActionInstanceId = 0;
            BlendProgress = 0f;
            ResponsePolicy = CameraResponsePolicy.Full;
            Basis = default;
            PosePlan = default;
            TargetSource = string.Empty;
            m_Requests.Clear();
            m_Cues.Clear();
        }
    }
}
