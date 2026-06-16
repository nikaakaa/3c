using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace ThirdPersonMovement
{
    public static class LocomotionFactsBuilder
    {
        public static MovementInputIntent ResolveMovementIntent(
            in BasicLocomotionInputSnapshot input,
            in BasicMovementSettings baseSettings,
            bool runLatchActive)
        {
            bool wantsRun = input.RunHeld || runLatchActive;
            return MovementInputIntent.FromRaw(input.Move, baseSettings.InputDeadZone, wantsRun);
        }

        public static BasicMovementGait ResolveFrameGait(
            BasicMovementPhase currentPhase,
            in MovementInputIntent pendingIntent,
            BasicMovementGait lastMovingGait,
            bool hasActiveMoveStopGait,
            BasicMovementGait activeMoveStopGait)
        {
            if (pendingIntent.HasMoveIntent)
                return pendingIntent.Gait;

            if (currentPhase == BasicMovementPhase.MoveStop && hasActiveMoveStopGait)
                return activeMoveStopGait;

            return lastMovingGait;
        }

        public static BasicLocomotionInputSnapshot ResolveInput(
            in BasicLocomotionInputSnapshot input,
            in MovementInputIntent pendingIntent,
            bool runLatchActive)
        {
            bool wantsRun = pendingIntent.HasMoveIntent && pendingIntent.Gait == BasicMovementGait.Run || input.RunHeld || runLatchActive;
            return new BasicLocomotionInputSnapshot(
                input.DeltaTime,
                input.Move,
                input.Look,
                wantsRun);
        }

        public static LocomotionSpatialFacts BuildSpatialFacts(
            in MovementInputIntent intent,
            Vector3 worldMoveDirection,
            Vector3 facingForward,
            Vector3 cameraPlanarForward,
            Vector3 cameraPlanarRight)
        {
            return new LocomotionSpatialFacts(
                worldMoveDirection,
                facingForward,
                cameraPlanarForward,
                cameraPlanarRight);
        }

        public static LocomotionDecisionFacts BuildFacts(
            in MovementInputIntent intent,
            BasicMovementGait frameGait,
            in BasicMovementPhaseFacts phaseFacts,
            in LocomotionSpatialFacts spatialFacts,
            in LocomotionTurnBackIntent turnBackIntent)
        {
            return new LocomotionDecisionFacts(
                intent,
                frameGait,
                phaseFacts,
                spatialFacts,
                turnBackIntent);
        }

        public static CharacterStateMachineContext BuildContext(
            in BasicLocomotionInputSnapshot input,
            int currentStep,
            in LocomotionDecisionFacts facts,
            in CharacterInputRequestFact inputRequest,
            in CharacterRuntimeBlackboardSnapshot blackboardBeforeTick,
            StateTimelineWindowFacts currentTimelineFacts = default)
        {
            return new CharacterStateMachineContext(
                input.DeltaTime,
                currentStep,
                in facts,
                inputRequest,
                blackboardBeforeTick,
                currentTimelineFacts);
        }
    }
}
