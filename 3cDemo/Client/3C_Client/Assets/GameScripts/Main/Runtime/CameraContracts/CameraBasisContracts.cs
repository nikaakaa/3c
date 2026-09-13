using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public enum CameraResetReason : byte
    {
        None = 0,
        Initialization = 1,
        BodyCommittedBranchReplacement = 2,
        BodySelectedStreamReset = 3,
        RuntimeReset = 4
    }

    public readonly struct CameraInitialState
    {
        public CameraInitialState(float yaw, float pitch)
        {
            if (!float.IsFinite(yaw) || !float.IsFinite(pitch))
                throw new ArgumentOutOfRangeException(nameof(yaw));
            Yaw = yaw;
            Pitch = pitch;
        }

        public float Yaw { get; }
        public float Pitch { get; }
    }

    public static class CameraTargetBindingKeys
    {
        public const string Body = "camera.body";
    }

    [Serializable]
    public sealed class CameraTargetBinding
    {
        [SerializeField] string key = string.Empty;
        [SerializeField] Transform target;

        public string Key => key ?? string.Empty;
        public Transform Target => target;
    }

    public readonly struct CameraResolvedTargetPlan
    {
        public CameraResolvedTargetPlan(
            bool valid,
            bool hasFollowPoint,
            Vector3 followPoint,
            bool hasAimPoint,
            Vector3 aimPoint,
            string sourceKey,
            string error,
            bool targetInvalid = false,
            string invalidKey = "")
        {
            Valid = valid;
            HasFollowPoint = hasFollowPoint;
            FollowPoint = followPoint;
            HasAimPoint = hasAimPoint;
            AimPoint = aimPoint;
            SourceKey = sourceKey ?? string.Empty;
            Error = error ?? string.Empty;
            TargetInvalid = targetInvalid;
            InvalidKey = invalidKey ?? string.Empty;
        }

        public bool Valid { get; }
        public bool HasFollowPoint { get; }
        public Vector3 FollowPoint { get; }
        public bool HasAimPoint { get; }
        public Vector3 AimPoint { get; }
        public string SourceKey { get; }
        public string Error { get; }
        public bool TargetInvalid { get; }
        public string InvalidKey { get; }

        public static CameraResolvedTargetPlan NoOverride =>
            new CameraResolvedTargetPlan(true, false, default, false, default, string.Empty, string.Empty);

        public static CameraResolvedTargetPlan Invalid(
            string sourceKey,
            string error,
            bool targetInvalid = false,
            string invalidKey = "") =>
            new CameraResolvedTargetPlan(
                false,
                false,
                default,
                false,
                default,
                sourceKey,
                error,
                targetInvalid,
                invalidKey);
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

    public sealed class CameraDebugSnapshot
    {
        public CameraFramePlan Plan { get; private set; }
        public CameraRigResult Result { get; private set; }
        public string TargetSource { get; private set; } = string.Empty;
        public string ProjectionRevision { get; private set; } = string.Empty;
        public Vector2 RawLook { get; private set; }
        public Vector2 ConsumedLook { get; private set; }
        public CameraResponseRequest Response { get; private set; }
        public CameraResetReason ResetReason { get; private set; }
        public CameraPresentationStopReason SequenceStopReason { get; private set; }
        public bool SequenceRetiring { get; private set; }
        public CameraPresentationStopReason TargetStopReason { get; private set; }
        public string TargetRetiredKey { get; private set; } = string.Empty;
        public bool TargetRetired { get; private set; }
        public bool Paused { get; private set; }
        public float DeltaSeconds { get; private set; }
        public IReadOnlyList<CameraEffectContribution> Effects { get; private set; } =
            Array.Empty<CameraEffectContribution>();

        public void Set(
            CameraFramePlan plan,
            CameraRigResult result,
            string targetSource,
            string projectionRevision,
            Vector2 rawLook,
            Vector2 consumedLook,
            in CameraResponseRequest response,
            CameraResetReason resetReason,
            bool sequenceRetiring,
            CameraPresentationStopReason sequenceStopReason,
            bool targetRetired,
            CameraPresentationStopReason targetStopReason,
            string targetRetiredKey,
            bool paused,
            float deltaSeconds,
            IReadOnlyList<CameraEffectContribution> effects)
        {
            Plan = plan;
            Result = result;
            TargetSource = targetSource ?? string.Empty;
            ProjectionRevision = projectionRevision ?? string.Empty;
            RawLook = rawLook;
            ConsumedLook = consumedLook;
            Response = response;
            ResetReason = resetReason;
            SequenceRetiring = sequenceRetiring;
            SequenceStopReason = sequenceStopReason;
            TargetRetired = targetRetired;
            TargetStopReason = targetStopReason;
            TargetRetiredKey = targetRetiredKey ?? string.Empty;
            Paused = paused;
            DeltaSeconds = deltaSeconds;
            if (effects == null || effects.Count == 0)
            {
                Effects = Array.Empty<CameraEffectContribution>();
                return;
            }
            var copy = new CameraEffectContribution[effects.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = effects[i];
            Effects = copy;
        }

        public void Clear()
        {
            Plan = default;
            Result = default;
            TargetSource = string.Empty;
            ProjectionRevision = string.Empty;
            RawLook = Vector2.zero;
            ConsumedLook = Vector2.zero;
            Response = default;
            ResetReason = CameraResetReason.None;
            SequenceRetiring = false;
            SequenceStopReason = CameraPresentationStopReason.NaturalComplete;
            TargetRetired = false;
            TargetStopReason = CameraPresentationStopReason.NaturalComplete;
            TargetRetiredKey = string.Empty;
            Paused = false;
            DeltaSeconds = 0f;
            Effects = Array.Empty<CameraEffectContribution>();
        }
    }
}
