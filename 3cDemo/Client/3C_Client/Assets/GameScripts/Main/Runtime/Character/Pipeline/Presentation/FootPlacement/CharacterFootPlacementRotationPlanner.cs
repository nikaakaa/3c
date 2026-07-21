using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal static class CharacterFootPlacementRotationPlanner
    {
        public static float ResolveHeelLift(
            CharacterFootPlacementAnimatedFootPose pose,
            FootPlacementSupportResult support,
            bool constrained,
            FootPlacementConstraintRuntimeSettings settings)
        {
            if (!constrained || !support.HeelSupport.IsValid || !support.ToeSupport.IsValid)
                return 0f;
            Vector3 averageNormal = (support.HeelSupport.Normal + support.ToeSupport.Normal).normalized;
            float boundaryRise = Vector3.Dot(
                averageNormal,
                support.ToeSupport.Point - support.HeelSupport.Point);
            if (boundaryRise <= 0.0001f)
                return 0f;
            float footLength = Vector3.Distance(pose.HeelPosition, pose.ToePosition);
            if (footLength <= 0.0001f)
                return 0f;
            float angleLimit = Mathf.Tan(settings.MaximumHeelLiftDegrees * Mathf.Deg2Rad) * footLength;
            return Mathf.Min(boundaryRise, settings.MaximumHeelLiftDistance, angleLimit);
        }

        public static Quaternion ResolveRotation(
            Transform visualRoot,
            CharacterFootPlacementAnimatedFootPose pose,
            Vector3 supportNormal,
            float heightDelta,
            float heelLiftDistance,
            FootPlacementConstraintRuntimeSettings constraint,
            FootPlacementRotationRuntimeSettings rotation,
            out float ankleTwistDegrees)
        {
            ankleTwistDegrees = 0f;
            Quaternion inverseRoot = Quaternion.Inverse(visualRoot.rotation);
            Vector3 localNormal = inverseRoot * supportNormal.normalized;
            float pitch = Mathf.Clamp(
                Mathf.Atan2(localNormal.z, Mathf.Max(0.0001f, localNormal.y)) * Mathf.Rad2Deg,
                -rotation.MaximumPitchDegrees,
                rotation.MaximumPitchDegrees);
            float roll = Mathf.Clamp(
                -Mathf.Atan2(localNormal.x, Mathf.Max(0.0001f, localNormal.y)) * Mathf.Rad2Deg,
                -rotation.MaximumRollDegrees,
                rotation.MaximumRollDegrees);
            Vector3 clampedNormal = visualRoot.rotation * (Quaternion.Euler(pitch, 0f, roll) * Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(pose.SoleForward, clampedNormal).normalized;
            Vector3 rootForward = Vector3.ProjectOnPlane(visualRoot.forward, clampedNormal).normalized;
            if (forward.sqrMagnitude <= 0.0001f || rootForward.sqrMagnitude <= 0.0001f)
                return pose.AnkleRotation;
            float twist = Vector3.SignedAngle(rootForward, forward, clampedNormal);
            ankleTwistDegrees = Mathf.Clamp(
                twist,
                -constraint.MaximumAnkleTwistDegrees,
                constraint.MaximumAnkleTwistDegrees);
            forward = Quaternion.AngleAxis(ankleTwistDegrees, clampedNormal) * rootForward;
            Quaternion desiredSemanticRotation = Quaternion.LookRotation(forward, clampedNormal);
            Quaternion semanticDelta = desiredSemanticRotation * Quaternion.Inverse(pose.SemanticRotation);
            Quaternion targetRotation = semanticDelta * pose.AnkleRotation;
            float footLength = Vector3.Distance(pose.HeelPosition, pose.ToePosition);
            if (heelLiftDistance > 0f && footLength > 0.0001f)
            {
                float heelLiftDegrees = Mathf.Min(
                    Mathf.Atan2(heelLiftDistance, footLength) * Mathf.Rad2Deg,
                    constraint.MaximumHeelLiftDegrees);
                Vector3 semanticRight = Vector3.Cross(clampedNormal, forward).normalized;
                if (semanticRight.sqrMagnitude > 0.0001f)
                    targetRotation = Quaternion.AngleAxis(heelLiftDegrees, semanticRight) * targetRotation;
            }
            float alignment = heightDelta >= 0f
                ? rotation.AscentSurfaceAlignment
                : rotation.DescentSurfaceAlignment;
            return Quaternion.Slerp(
                pose.AnkleRotation,
                targetRotation,
                Mathf.Clamp01(alignment));
        }
    }
}
