using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct BasicMovementMotionFacts
    {
        public BasicMovementMotionFacts(
            bool hasAnimationMotion,
            Vector3 localPlanarDelta,
            float yawDelta,
            BasicMovementPhase sourcePhase,
            string sourceAliasKey)
        {
            HasAnimationMotion = hasAnimationMotion;
            LocalPlanarDelta = new Vector3(localPlanarDelta.x, 0f, localPlanarDelta.z);
            YawDelta = yawDelta;
            SourcePhase = sourcePhase;
            SourceAliasKey = sourceAliasKey ?? string.Empty;
        }

        public bool HasAnimationMotion { get; }
        public Vector3 LocalPlanarDelta { get; }
        public float YawDelta { get; }
        public BasicMovementPhase SourcePhase { get; }
        public string SourceAliasKey { get; }

        public static BasicMovementMotionFacts None(BasicMovementPhase phase)
        {
            return new BasicMovementMotionFacts(false, Vector3.zero, 0f, phase, string.Empty);
        }
    }
}
