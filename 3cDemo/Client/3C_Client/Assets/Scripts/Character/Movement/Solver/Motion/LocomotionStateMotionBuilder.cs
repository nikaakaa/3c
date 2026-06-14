using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace ThirdPersonMovement
{
    public static class LocomotionStateMotionBuilder
    {
        public static BasicLocomotionFrame BuildFrame(
            BasicLocomotionPipeline pipeline,
            in BasicLocomotionInputSnapshot input,
            in BasicMovementSettings settings,
            in LocomotionDecisionFacts decisionFacts,
            in CharacterStateMachineFrame stateFrame,
            in BasicMovementMotionFacts motionFacts,
            BasicMovementGait frameGait)
        {
            return pipeline.Tick(in input, in settings, in decisionFacts, stateFrame.LocomotionPhase, motionFacts, frameGait);
        }

        public static LocomotionDecisionFacts ApplyTurnBackLockedDirection(
            in LocomotionDecisionFacts facts,
            in CharacterStateMachineFrame stateFrame)
        {
            if (stateFrame.LocomotionPhase != BasicMovementPhase.TurnBack ||
                !stateFrame.HasTurnBackMotionPolicy ||
                !TryNormalizePlanar(stateFrame.TurnBackWorldDirection, out Vector3 lockedDirection))
            {
                return facts;
            }

            LocomotionSpatialFacts spatialFacts = new LocomotionSpatialFacts(
                lockedDirection,
                facts.SpatialFacts.FacingForward,
                facts.SpatialFacts.CameraPlanarForward,
                facts.SpatialFacts.CameraPlanarRight);
            return new LocomotionDecisionFacts(
                facts.MoveIntent,
                facts.GaitCandidate,
                facts.PhaseFacts,
                spatialFacts,
                facts.TurnBackIntent);
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
