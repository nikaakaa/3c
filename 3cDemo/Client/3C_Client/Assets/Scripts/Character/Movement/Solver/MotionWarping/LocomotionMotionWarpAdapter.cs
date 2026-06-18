using ThirdPersonMotionWarping;
using UnityEngine;

namespace ThirdPersonMovement
{
    public static class LocomotionMotionWarpAdapter
    {
        public static BasicMovementMotionFacts ToMotionFacts(
            BasicMovementPhase phase,
            string sourceAliasKey,
            in MotionWarpResult result)
        {
            if (!result.IsValid || !result.HasContribution)
                return BasicMovementMotionFacts.None(phase);

            return new BasicMovementMotionFacts(
                true,
                result.PlanarDelta,
                result.YawDelta,
                phase,
                sourceAliasKey,
                false,
                false,
                BasicMovementPlanarDeltaSpace.World);
        }
    }
}
