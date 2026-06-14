using ThirdPersonDiagnostics;
using ThirdPersonCharacterStateMachine;
using ThirdPersonInput;
using ThirdPersonMovement;
using UnityEngine;

namespace ThirdPersonAction
{
    public static class FullBodyActionInputRequestBuilder
    {
        public static bool TryBuildDodgeRequest(
            InputRequestBuffer inputBuffer,
            int currentStep,
            in BasicLocomotionInputSnapshot input,
            bool runLatchActive,
            in LocomotionDecisionFacts locomotionFacts,
            in DodgeActionConfig config,
            out DodgeActionRequest request)
        {
            MovementInputIntent intent = locomotionFacts.MoveIntent;
            LocomotionSpatialFacts spatialFacts = locomotionFacts.SpatialFacts;
            Vector3 worldMoveDirection = spatialFacts.WorldMoveDirection;
            Vector3 facingForward = spatialFacts.FacingForward;
            BufferedInputRequest bufferedRequest = default;
            bool hasBufferedDodge = inputBuffer != null &&
                                    inputBuffer.TryPeek(InputRequestKind.Dodge, currentStep, out bufferedRequest);

            if (!DodgeActionPlanner.TryBuildRequest(
                    inputBuffer,
                    currentStep,
                    in intent,
                    worldMoveDirection,
                    facingForward,
                    in config,
                    out request))
            {
                if (hasBufferedDodge)
                    LogDodgeRequestFactProbe(currentStep, in input, in intent, runLatchActive, worldMoveDirection, in bufferedRequest, false, default);

                return false;
            }

            if (hasBufferedDodge)
                LogDodgeRequestFactProbe(currentStep, in input, in intent, runLatchActive, worldMoveDirection, in bufferedRequest, true, in request);

            return true;
        }

        public static CharacterInputRequestFact ToInputRequestFact(in DodgeActionRequest request)
        {
            return new CharacterInputRequestFact(
                true,
                InputRequestKind.Dodge,
                request.OriginStep,
                request.ExpireStep,
                request.Priority,
                request.Variant == DodgeActionVariant.Backstep ? CharacterStateVariant.Backstep : CharacterStateVariant.Directional,
                request.WorldDirection);
        }

        static void LogDodgeRequestFactProbe(
            int currentStep,
            in BasicLocomotionInputSnapshot input,
            in MovementInputIntent intent,
            bool runLatchActive,
            Vector3 worldMoveDirection,
            in BufferedInputRequest bufferedRequest,
            bool resolved,
            in DodgeActionRequest request)
        {
            RuntimeDiagnosticLog.Submit(new RuntimeDiagnosticLogEvent(
                RuntimeDiagnosticLogCategory.Action,
                RuntimeDiagnosticLogLevel.Trace,
                "action-dodge-request-fact-probe",
                ActionStateIds.Dodge.Value,
                string.Empty,
                currentStep,
                Time.frameCount,
                $"origin={bufferedRequest.OriginStep} expire={bufferedRequest.ExpireStep} consumed={bufferedRequest.Consumed} rawMove={input.Move.ToString("F3")} intentMove={intent.NormalizedInput.ToString("F3")} strength={intent.Strength:F3} hasMove={intent.HasMoveIntent} inputRunHeld={input.RunHeld} runLatch={runLatchActive} worldMove={worldMoveDirection.ToString("F3")} resolved={resolved} variant={(resolved ? request.Variant.ToString() : string.Empty)} requestWorld={(resolved ? request.WorldDirection.ToString("F3") : Vector3.zero.ToString("F3"))}"));
        }
    }
}
