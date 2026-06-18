using System;
using UnityEngine;

namespace ThirdPersonMotionWarping
{
    public readonly struct MotionWarpPolicyId
    {
        readonly string value;

        public MotionWarpPolicyId(string value)
        {
            this.value = (value ?? string.Empty).Trim();
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public static MotionWarpPolicyId None => default;
    }

    public readonly struct MotionWarpTargetBindingId
    {
        readonly string value;

        public MotionWarpTargetBindingId(string value)
        {
            this.value = (value ?? string.Empty).Trim();
        }

        public string Value => value ?? string.Empty;
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);

        public static MotionWarpTargetBindingId None => default;
    }

    [Flags]
    public enum MotionWarpAxisMask
    {
        None = 0,
        X = 1,
        Z = 2,
        Planar = X | Z
    }

    public enum MotionWarpRotationPolicy
    {
        None = 0,
        FaceTargetPosition = 1,
        MatchTargetForward = 2
    }

    public enum MotionWarpFailureReason
    {
        None = 0,
        PolicyMissing = 1,
        TargetMissing = 2,
        RootMissing = 3,
        MotionWindowInactive = 4
    }

    public readonly struct MotionWarpPolicy
    {
        public MotionWarpPolicy(
            MotionWarpPolicyId policyId,
            bool enableAttackMagnet,
            bool enableFacingCorrection,
            bool requireTarget,
            bool requireMotionProfile,
            string motionProfileId,
            MotionWarpAxisMask axisMask,
            MotionWarpRotationPolicy rotationPolicy,
            float maxPlanarDelta,
            float stoppingDistance,
            float maxYawDeltaDegrees,
            float translationWeight,
            float rotationWeight)
        {
            PolicyId = policyId;
            EnableAttackMagnet = enableAttackMagnet;
            EnableFacingCorrection = enableFacingCorrection;
            RequireTarget = requireTarget;
            RequireMotionProfile = requireMotionProfile;
            MotionProfileId = (motionProfileId ?? string.Empty).Trim();
            AxisMask = axisMask == MotionWarpAxisMask.None ? MotionWarpAxisMask.Planar : axisMask;
            RotationPolicy = rotationPolicy;
            MaxPlanarDelta = Mathf.Max(0f, maxPlanarDelta);
            StoppingDistance = Mathf.Max(0f, stoppingDistance);
            MaxYawDeltaDegrees = Mathf.Max(0f, maxYawDeltaDegrees);
            TranslationWeight = Mathf.Clamp01(translationWeight);
            RotationWeight = Mathf.Clamp01(rotationWeight);
        }

        public MotionWarpPolicyId PolicyId { get; }
        public bool EnableAttackMagnet { get; }
        public bool EnableFacingCorrection { get; }
        public bool RequireTarget { get; }
        public bool RequireMotionProfile { get; }
        public string MotionProfileId { get; }
        public MotionWarpAxisMask AxisMask { get; }
        public MotionWarpRotationPolicy RotationPolicy { get; }
        public float MaxPlanarDelta { get; }
        public float StoppingDistance { get; }
        public float MaxYawDeltaDegrees { get; }
        public float TranslationWeight { get; }
        public float RotationWeight { get; }
        public bool HasWarp => EnableAttackMagnet || EnableFacingCorrection;
        public bool IsValid => PolicyId.IsValid && HasWarp;
        public bool HasRequiredMotionProfile => !RequireMotionProfile || !string.IsNullOrWhiteSpace(MotionProfileId);

        public static MotionWarpPolicy None => default;

        public static MotionWarpPolicy AttackMagnetAndFacingCorrection(
            string policyId,
            float maxPlanarDelta,
            float stoppingDistance,
            float maxYawDeltaDegrees)
        {
            return new MotionWarpPolicy(
                new MotionWarpPolicyId(policyId),
                true,
                true,
                true,
                false,
                string.Empty,
                MotionWarpAxisMask.Planar,
                MotionWarpRotationPolicy.FaceTargetPosition,
                maxPlanarDelta,
                stoppingDistance,
                maxYawDeltaDegrees,
                1f,
                1f);
        }
    }

    public readonly struct MotionWarpPayload
    {
        public MotionWarpPayload(MotionWarpPolicy policy, MotionWarpTargetBindingId targetBindingId)
        {
            Policy = policy;
            TargetBindingId = targetBindingId;
        }

        public MotionWarpPolicy Policy { get; }
        public MotionWarpTargetBindingId TargetBindingId { get; }
        public bool HasWarp => Policy.HasWarp;
        public bool IsValid => !HasWarp || Policy.IsValid;
        public bool HasRequiredTargetBinding => !Policy.RequireTarget || TargetBindingId.IsValid;
        public bool HasRequiredMotionProfile => Policy.HasRequiredMotionProfile;

        public static MotionWarpPayload None => default;

        public static MotionWarpPayload AttackMagnetAndFacingCorrection(
            string policyId,
            string targetBindingId,
            float maxPlanarDelta,
            float stoppingDistance,
            float maxYawDeltaDegrees)
        {
            return new MotionWarpPayload(
                MotionWarpPolicy.AttackMagnetAndFacingCorrection(
                    policyId,
                    maxPlanarDelta,
                    stoppingDistance,
                    maxYawDeltaDegrees),
                new MotionWarpTargetBindingId(targetBindingId));
        }
    }

    public readonly struct MotionWarpTargetSnapshot
    {
        public MotionWarpTargetSnapshot(
            bool isValid,
            Vector3 position,
            Vector3 forward,
            MotionWarpTargetBindingId bindingId,
            string sourceId,
            int sourceStep)
        {
            IsValid = isValid && bindingId.IsValid;
            Position = position;
            Forward = NormalizePlanarOrZero(forward);
            BindingId = bindingId;
            SourceId = (sourceId ?? string.Empty).Trim();
            SourceStep = Mathf.Max(0, sourceStep);
        }

        public bool IsValid { get; }
        public Vector3 Position { get; }
        public Vector3 Forward { get; }
        public MotionWarpTargetBindingId BindingId { get; }
        public string SourceId { get; }
        public int SourceStep { get; }

        public static MotionWarpTargetSnapshot Invalid(MotionWarpTargetBindingId bindingId = default, int sourceStep = 0)
        {
            return new MotionWarpTargetSnapshot(false, Vector3.zero, Vector3.zero, bindingId, string.Empty, sourceStep);
        }

        public static MotionWarpTargetSnapshot Pose(
            string bindingId,
            Vector3 position,
            Vector3 forward,
            string sourceId,
            int sourceStep)
        {
            return new MotionWarpTargetSnapshot(
                true,
                position,
                forward,
                new MotionWarpTargetBindingId(bindingId),
                sourceId,
                sourceStep);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }

    public readonly struct MotionWarpRootSnapshot
    {
        public MotionWarpRootSnapshot(bool hasPose, Vector3 position, Vector3 forward, int sourceStep)
        {
            HasPose = hasPose;
            Position = position;
            Forward = NormalizePlanarOrZero(forward);
            SourceStep = Mathf.Max(0, sourceStep);
        }

        public bool HasPose { get; }
        public Vector3 Position { get; }
        public Vector3 Forward { get; }
        public int SourceStep { get; }

        public static MotionWarpRootSnapshot Invalid(int sourceStep = 0)
        {
            return new MotionWarpRootSnapshot(false, Vector3.zero, Vector3.zero, sourceStep);
        }

        public static MotionWarpRootSnapshot Pose(Vector3 position, Vector3 forward, int sourceStep)
        {
            return new MotionWarpRootSnapshot(true, position, forward, sourceStep);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }

    public readonly struct MotionWarpInput
    {
        public MotionWarpInput(
            MotionWarpPolicy policy,
            MotionWarpRootSnapshot rootSnapshot,
            MotionWarpTargetSnapshot targetSnapshot,
            bool motionWindowActive,
            int sourceStep)
        {
            Policy = policy;
            RootSnapshot = rootSnapshot;
            TargetSnapshot = targetSnapshot;
            MotionWindowActive = motionWindowActive;
            SourceStep = Mathf.Max(0, sourceStep);
        }

        public MotionWarpPolicy Policy { get; }
        public MotionWarpRootSnapshot RootSnapshot { get; }
        public MotionWarpTargetSnapshot TargetSnapshot { get; }
        public bool MotionWindowActive { get; }
        public int SourceStep { get; }

        public static MotionWarpInput None(int sourceStep = 0)
        {
            return new MotionWarpInput(
                MotionWarpPolicy.None,
                MotionWarpRootSnapshot.Invalid(sourceStep),
                MotionWarpTargetSnapshot.Invalid(default, sourceStep),
                false,
                sourceStep);
        }
    }

    public readonly struct MotionWarpResult
    {
        public MotionWarpResult(
            bool isValid,
            bool hasContribution,
            Vector3 planarDelta,
            float yawDelta,
            MotionWarpFailureReason failureReason,
            MotionWarpPolicyId policyId,
            MotionWarpTargetBindingId targetBindingId,
            int sourceStep)
        {
            IsValid = isValid;
            PlanarDelta = new Vector3(planarDelta.x, 0f, planarDelta.z);
            YawDelta = yawDelta;
            HasContribution = isValid && hasContribution &&
                              (PlanarDelta.sqrMagnitude > 0.000001f || Mathf.Abs(yawDelta) > 0.0001f);
            FailureReason = failureReason;
            PolicyId = policyId;
            TargetBindingId = targetBindingId;
            SourceStep = Mathf.Max(0, sourceStep);
        }

        public bool IsValid { get; }
        public bool HasContribution { get; }
        public Vector3 PlanarDelta { get; }
        public float YawDelta { get; }
        public MotionWarpFailureReason FailureReason { get; }
        public MotionWarpPolicyId PolicyId { get; }
        public MotionWarpTargetBindingId TargetBindingId { get; }
        public int SourceStep { get; }

        public static MotionWarpResult None(MotionWarpPolicyId policyId, MotionWarpTargetBindingId targetBindingId, int sourceStep)
        {
            return new MotionWarpResult(
                true,
                false,
                Vector3.zero,
                0f,
                MotionWarpFailureReason.None,
                policyId,
                targetBindingId,
                sourceStep);
        }

        public static MotionWarpResult Inactive(MotionWarpPolicyId policyId, MotionWarpTargetBindingId targetBindingId, int sourceStep)
        {
            return new MotionWarpResult(
                true,
                false,
                Vector3.zero,
                0f,
                MotionWarpFailureReason.MotionWindowInactive,
                policyId,
                targetBindingId,
                sourceStep);
        }

        public static MotionWarpResult Invalid(
            MotionWarpFailureReason reason,
            MotionWarpPolicyId policyId,
            MotionWarpTargetBindingId targetBindingId,
            int sourceStep)
        {
            return new MotionWarpResult(
                false,
                false,
                Vector3.zero,
                0f,
                reason,
                policyId,
                targetBindingId,
                sourceStep);
        }
    }
}
