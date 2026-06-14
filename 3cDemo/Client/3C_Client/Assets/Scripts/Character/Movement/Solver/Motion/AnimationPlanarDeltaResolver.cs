using UnityEngine;

namespace ThirdPersonMovement
{
    public static class AnimationPlanarDeltaResolver
    {
        public static Vector3 ResolveWorldDelta(in MovementCommand command, Quaternion rootRotation)
        {
            if (!command.HasAnimationMotion)
                return Vector3.zero;

            Vector3 localDelta = command.AnimationLocalPlanarDelta;
            localDelta.y = 0f;
            if (localDelta.sqrMagnitude <= 0.000001f)
                return Vector3.zero;

            Vector3 worldDelta = command.AnimationPlanarDeltaSpace switch
            {
                BasicMovementPlanarDeltaSpace.World => localDelta,
                BasicMovementPlanarDeltaSpace.EntryLocal => ResolveEntryLocalWorldDelta(localDelta, command.AnimationPlanarBasisForward),
                _ => rootRotation * localDelta
            };
            worldDelta.y = 0f;
            return worldDelta;
        }

        public static Vector3 ResolveEntryLocalWorldDelta(Vector3 localDelta, Vector3 entryPlanarBasisForward)
        {
            if (!TryNormalizePlanar(entryPlanarBasisForward, out Vector3 forward))
                return Vector3.zero;

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            return right * localDelta.x + forward * localDelta.z;
        }

        public static Vector3 ResolveLastWorldDirection(Vector3 inputWorldDirection, Vector3 animationWorldDelta)
        {
            if (inputWorldDirection.sqrMagnitude > 0.000001f)
                return inputWorldDirection;

            animationWorldDelta.y = 0f;
            return animationWorldDelta.sqrMagnitude > 0.000001f ? animationWorldDelta.normalized : Vector3.zero;
        }

        public static Vector3 ResolvePlanarRightOrZero(Vector3 forward)
        {
            return TryNormalizePlanar(forward, out Vector3 normalizedForward)
                ? Vector3.Cross(Vector3.up, normalizedForward).normalized
                : Vector3.zero;
        }

        static bool TryNormalizePlanar(Vector3 value, out Vector3 normalized)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            if (sqrMagnitude <= 0.000001f)
            {
                normalized = Vector3.zero;
                return false;
            }

            normalized = value / Mathf.Sqrt(sqrMagnitude);
            return true;
        }
    }
}
