using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAnimation
{
    public readonly struct AnimationMotionProfileSample
    {
        public AnimationMotionProfileSample(
            bool hasMotionContribution,
            Vector3 localPlanarDelta,
            float yawDelta,
            BasicMovementPhase sourcePhase,
            string sourceAliasKey)
        {
            HasMotionContribution = hasMotionContribution;
            LocalPlanarDelta = new Vector3(localPlanarDelta.x, 0f, localPlanarDelta.z);
            YawDelta = yawDelta;
            SourcePhase = sourcePhase;
            SourceAliasKey = sourceAliasKey ?? string.Empty;
        }

        public bool HasMotionContribution { get; }
        public Vector3 LocalPlanarDelta { get; }
        public float YawDelta { get; }
        public BasicMovementPhase SourcePhase { get; }
        public string SourceAliasKey { get; }

        public static AnimationMotionProfileSample None(BasicMovementPhase phase)
        {
            return new AnimationMotionProfileSample(false, Vector3.zero, 0f, phase, string.Empty);
        }
    }
}
