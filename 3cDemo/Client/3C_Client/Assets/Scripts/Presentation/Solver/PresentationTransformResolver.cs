using UnityEngine;

namespace ThirdPersonPresentation
{
    public static class PresentationTransformResolver
    {
        public static PresentationPose Resolve(
            PresentationPose previousPose,
            PresentationPose currentPose,
            float interpolationAlpha,
            bool hasPreviousPose,
            float snapDistance)
        {
            if (!hasPreviousPose)
                return currentPose;

            float safeSnapDistance = Mathf.Max(0f, snapDistance);
            if (safeSnapDistance > 0f)
            {
                float snapDistanceSquared = safeSnapDistance * safeSnapDistance;
                if ((currentPose.Position - previousPose.Position).sqrMagnitude > snapDistanceSquared)
                    return currentPose;
            }

            float alpha = Mathf.Clamp01(interpolationAlpha);
            return new PresentationPose(
                Vector3.LerpUnclamped(previousPose.Position, currentPose.Position, alpha),
                Quaternion.SlerpUnclamped(previousPose.Rotation, currentPose.Rotation, alpha));
        }
    }
}
