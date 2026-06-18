using UnityEngine;

namespace ThirdPersonMotionWarping
{
    public static class MotionWarpSolver
    {
        public static MotionWarpResult Resolve(in MotionWarpInput input)
        {
            MotionWarpPolicy policy = input.Policy;
            MotionWarpTargetBindingId targetBindingId = input.TargetSnapshot.BindingId.IsValid
                ? input.TargetSnapshot.BindingId
                : MotionWarpTargetBindingId.None;

            if (!policy.HasWarp)
                return MotionWarpResult.None(policy.PolicyId, targetBindingId, input.SourceStep);
            if (!policy.IsValid || !policy.HasRequiredMotionProfile)
                return MotionWarpResult.Invalid(MotionWarpFailureReason.PolicyMissing, policy.PolicyId, targetBindingId, input.SourceStep);
            if (!input.MotionWindowActive)
                return MotionWarpResult.Inactive(policy.PolicyId, targetBindingId, input.SourceStep);
            if (policy.RequireTarget && !input.TargetSnapshot.IsValid)
                return MotionWarpResult.Invalid(MotionWarpFailureReason.TargetMissing, policy.PolicyId, targetBindingId, input.SourceStep);
            if (!input.RootSnapshot.HasPose)
                return MotionWarpResult.Invalid(MotionWarpFailureReason.RootMissing, policy.PolicyId, targetBindingId, input.SourceStep);

            MotionWarpRootSnapshot rootSnapshot = input.RootSnapshot;
            MotionWarpTargetSnapshot targetSnapshot = input.TargetSnapshot;
            Vector3 planarDelta = policy.EnableAttackMagnet
                ? ResolveAttackMagnetDelta(in policy, in rootSnapshot, in targetSnapshot)
                : Vector3.zero;
            float yawDelta = policy.EnableFacingCorrection
                ? ResolveFacingYawDelta(in policy, in rootSnapshot, in targetSnapshot)
                : 0f;

            return new MotionWarpResult(
                true,
                planarDelta.sqrMagnitude > 0.000001f || Mathf.Abs(yawDelta) > 0.0001f,
                planarDelta,
                yawDelta,
                MotionWarpFailureReason.None,
                policy.PolicyId,
                input.TargetSnapshot.BindingId,
                input.SourceStep);
        }

        static Vector3 ResolveAttackMagnetDelta(
            in MotionWarpPolicy policy,
            in MotionWarpRootSnapshot root,
            in MotionWarpTargetSnapshot target)
        {
            Vector3 toTarget = target.Position - root.Position;
            toTarget.y = 0f;
            float distance = toTarget.magnitude;
            if (distance <= 0.000001f)
                return Vector3.zero;

            float availableDistance = Mathf.Max(0f, distance - policy.StoppingDistance);
            float clampedDistance = Mathf.Min(policy.MaxPlanarDelta, availableDistance) * policy.TranslationWeight;
            if (clampedDistance <= 0.000001f)
                return Vector3.zero;

            Vector3 delta = toTarget / distance * clampedDistance;
            if ((policy.AxisMask & MotionWarpAxisMask.X) == 0)
                delta.x = 0f;
            if ((policy.AxisMask & MotionWarpAxisMask.Z) == 0)
                delta.z = 0f;

            return delta;
        }

        static float ResolveFacingYawDelta(
            in MotionWarpPolicy policy,
            in MotionWarpRootSnapshot root,
            in MotionWarpTargetSnapshot target)
        {
            if (policy.RotationPolicy == MotionWarpRotationPolicy.None || policy.MaxYawDeltaDegrees <= 0f)
                return 0f;

            Vector3 desiredForward = ResolveDesiredForward(in policy, in root, in target);
            if (desiredForward.sqrMagnitude <= 0.000001f || root.Forward.sqrMagnitude <= 0.000001f)
                return 0f;

            float rawYaw = Vector3.SignedAngle(root.Forward, desiredForward, Vector3.up);
            float clampedYaw = Mathf.Clamp(rawYaw, -policy.MaxYawDeltaDegrees, policy.MaxYawDeltaDegrees);
            return clampedYaw * policy.RotationWeight;
        }

        static Vector3 ResolveDesiredForward(
            in MotionWarpPolicy policy,
            in MotionWarpRootSnapshot root,
            in MotionWarpTargetSnapshot target)
        {
            Vector3 desired = policy.RotationPolicy == MotionWarpRotationPolicy.MatchTargetForward
                ? target.Forward
                : target.Position - root.Position;
            desired.y = 0f;
            float sqrMagnitude = desired.sqrMagnitude;
            return sqrMagnitude > 0.000001f ? desired / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
