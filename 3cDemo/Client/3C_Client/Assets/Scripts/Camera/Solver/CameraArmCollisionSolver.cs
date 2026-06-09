using UnityEngine;

namespace ThirdPersonCamera
{
    public static class CameraArmCollisionSolver
    {
        public static float CalculateTargetDistance(float desiredDistance, bool hasHit, float hitDistance, float minDistance)
        {
            float target = Mathf.Max(0f, desiredDistance);
            if (hasHit)
                target = Mathf.Min(target, Mathf.Max(0f, hitDistance));
            return Mathf.Max(Mathf.Max(0f, minDistance), target);
        }

        public static float SmoothDistance(
            float currentDistance,
            float targetDistance,
            float shrinkSmoothTime,
            float recoverSmoothTime,
            float deltaTime,
            ref float velocity)
        {
            if (deltaTime < 0f)
            {
                velocity = 0f;
                return targetDistance;
            }

            float smoothTime = targetDistance < currentDistance ? shrinkSmoothTime : recoverSmoothTime;
            if (smoothTime <= 0f)
            {
                velocity = 0f;
                return targetDistance;
            }

            return Mathf.SmoothDamp(currentDistance, targetDistance, ref velocity, smoothTime, float.PositiveInfinity, deltaTime);
        }
    }
}
