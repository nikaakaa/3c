using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace ThirdPersonMovement
{
    public sealed class LocomotionFrameBuilder
    {
        const float DirectionSqrEpsilon = 0.000001f;

        readonly BasicLocomotionPipeline motionPipeline = new BasicLocomotionPipeline();

        public LocomotionFramePrepareFacts ResolvePrepareFacts(in LocomotionFrameBuilderInput input)
        {
            BasicLocomotionInputSnapshot inputSnapshot = input.Input;
            BasicMovementSettings baseSettings = input.BaseSettings;
            LocomotionFrameRuntimeState runtimeState = input.RuntimeState;
            MovementInputIntent intent = LocomotionFactsBuilder.ResolveMovementIntent(
                in inputSnapshot,
                in baseSettings,
                runtimeState.RunLatchActive);
            BasicMovementGait frameGait = LocomotionFactsBuilder.ResolveFrameGait(
                input.CurrentPhase,
                in intent,
                runtimeState.LastMovingGait,
                runtimeState.HasActiveMoveStopGait,
                runtimeState.ActiveMoveStopGait);
            return new LocomotionFramePrepareFacts(intent, frameGait);
        }

        public bool TryPrepareDecisionFrame(
            in LocomotionFrameBuilderInput input,
            in LocomotionFramePrepareFacts prepareFacts,
            in BasicMovementSettings settings,
            in BasicMovementPhaseFacts phaseFacts,
            in LocomotionSpatialFacts spatialFacts,
            out LocomotionDecisionFrame decisionFrame,
            out LocomotionFrameBuilderResult result)
        {
            LocomotionFrameRuntimeState runtimeState = ResolveTurnBackIntent(
                in input,
                in prepareFacts,
                in spatialFacts,
                out LocomotionTurnBackIntent turnBackIntent);
            BasicLocomotionInputSnapshot inputSnapshot = input.Input;
            MovementInputIntent intent = prepareFacts.Intent;
            BasicLocomotionInputSnapshot resolvedInput = LocomotionFactsBuilder.ResolveInput(
                in inputSnapshot,
                in intent,
                input.RuntimeState.RunLatchActive);
            LocomotionDecisionFacts facts = LocomotionFactsBuilder.BuildFacts(
                in intent,
                prepareFacts.FrameGait,
                in phaseFacts,
                in spatialFacts,
                in turnBackIntent);
            LocomotionDiagnostics.LogLocomotionFacts(input.ActiveStatePath, input.CurrentStep, input.CurrentPhase, in facts);
            decisionFrame = new LocomotionDecisionFrame(
                resolvedInput,
                settings,
                prepareFacts.Intent,
                facts,
                prepareFacts.FrameGait);
            result = LocomotionFrameBuilderResult.Prepared(in decisionFrame, in runtimeState);
            return true;
        }

        public bool TryEvaluatePreparedGameplayDecision(
            in LocomotionDecisionFrame decisionFrame,
            CharacterStateMachineRunner runner,
            in LocomotionFrameBuilderInput input,
            out LocomotionStateDecisionFrame stateDecision,
            out LocomotionFrameBuilderResult result)
        {
            if (runner == null)
            {
                stateDecision = default;
                result = default;
                return false;
            }

            BasicLocomotionInputSnapshot frameInput = decisionFrame.Input;
            CharacterStateMachineSnapshot snapshotBeforeTick = runner.Snapshot;
            BasicMovementPhase phaseBeforeTick = FullBodyStateView.FromSnapshot(in snapshotBeforeTick).LocomotionPhase;
            CharacterRuntimeBlackboardSnapshot blackboardBeforeTick = input.BlackboardSnapshot;
            LocomotionDecisionFacts decisionFacts = decisionFrame.Facts;
            CharacterInputRequestFact inputRequest = input.InputRequest;
            LocomotionFrameRuntimeState inputRuntimeState = input.RuntimeState;
            CharacterStateMachineContext context = LocomotionFactsBuilder.BuildContext(
                in frameInput,
                input.CurrentStep,
                in decisionFacts,
                in inputRequest,
                in blackboardBeforeTick,
                input.CurrentTimelineFacts);
            bool runLatchBeforeStateTick = inputRuntimeState.RunLatchActive;
            CharacterStateMachineFrame stateFrame = runner.Tick(in context);
            LocomotionFrameRuntimeState runtimeState = ConsumeTurnBackIntentIfEntered(
                in decisionFacts,
                in stateFrame,
                input.CurrentStep,
                in inputRuntimeState);
            runtimeState = ApplyStateMachineOutputs(in stateFrame, in runtimeState);
            stateDecision = new LocomotionStateDecisionFrame(
                decisionFrame,
                stateFrame,
                phaseBeforeTick,
                decisionFrame.FrameGait,
                decisionFrame.Intent,
                decisionFrame.PhaseFacts,
                decisionFrame.Facts,
                blackboardBeforeTick,
                runLatchBeforeStateTick);
            result = LocomotionFrameBuilderResult.Evaluated(in stateDecision, in runtimeState);
            return true;
        }

        public bool TryBuildMotionFromStateDecision(
            in LocomotionStateDecisionFrame stateDecision,
            int currentStep,
            in BasicMovementMotionFacts motionFacts,
            in LocomotionFrameRuntimeState runtimeState,
            in AnimationPhasePlaybackProgress animationProgress,
            out BasicLocomotionFrame frame,
            out CharacterStateMachineFrame stateFrame,
            out LocomotionFrameBuilderResult result)
        {
            if (!stateDecision.HasStateFrame)
            {
                frame = default;
                stateFrame = default;
                result = default;
                return false;
            }

            LocomotionDecisionFrame decisionFrame = stateDecision.DecisionFrame;
            LocomotionDecisionFacts decisionFacts = stateDecision.DecisionFacts;
            stateFrame = stateDecision.StateFrame;
            BasicLocomotionInputSnapshot inputSnapshot = decisionFrame.Input;
            BasicMovementSettings settings = decisionFrame.Settings;
            MovementInputIntent pendingIntent = stateDecision.PendingIntent;
            BasicMovementPhaseFacts phaseFacts = stateDecision.PhaseFacts;
            LocomotionDecisionFacts motionDecisionFacts = LocomotionStateMotionBuilder.ApplyTurnBackLockedDirection(
                in decisionFacts,
                in stateFrame);
            frame = LocomotionStateMotionBuilder.BuildFrame(
                motionPipeline,
                in inputSnapshot,
                in settings,
                in motionDecisionFacts,
                in stateFrame,
                in motionFacts,
                stateDecision.FrameGait);
            LocomotionFrameRuntimeState phaseRuntimeState = UpdatePhaseGaitMemory(
                in runtimeState,
                stateFrame.LocomotionPhase,
                stateDecision.FrameGait);
            LocomotionDiagnostics.LogStateMachineOutputProbe(
                currentStep,
                stateDecision.PhaseBeforeTick,
                stateDecision.FrameGait,
                in pendingIntent,
                in phaseFacts,
                stateDecision.RunLatchBeforeStateTick,
                phaseRuntimeState.RunLatchActive,
                phaseRuntimeState.LastMovingGait,
                phaseRuntimeState.HasActiveMoveStopGait,
                phaseRuntimeState.ActiveMoveStopGait,
                in stateFrame);
            LocomotionDiagnostics.LogTurnBackFrameSummary(
                currentStep,
                stateDecision.PhaseBeforeTick,
                in decisionFacts,
                in stateFrame,
                in motionFacts,
                in frame,
                in animationProgress);
            MovementInputIntent frameIntent = frame.Intent;
            LocomotionFrameRuntimeState updatedRuntimeState = phaseRuntimeState.WithCurrentIntent(in frameIntent);
            if (frameIntent.HasMoveIntent)
                updatedRuntimeState = updatedRuntimeState.WithLastMovingGait(frameIntent.Gait);
            if (TryNormalizePlanar(frame.WorldDirection, out Vector3 previousDirection))
                updatedRuntimeState = updatedRuntimeState.WithPreviousWorldDirection(previousDirection);
            CharacterRuntimeLocomotionFacts locomotionFacts = new CharacterRuntimeLocomotionFacts(
                stateFrame.LocomotionPhase,
                frame.Command.Gait,
                updatedRuntimeState.LastMovingGait,
                updatedRuntimeState.HasActiveMoveStopGait,
                updatedRuntimeState.ActiveMoveStopGait,
                updatedRuntimeState.RunLatchActive,
                frame.WorldDirection,
                frameIntent.HasMoveIntent,
                frameIntent.Strength,
                currentStep);
            result = LocomotionFrameBuilderResult.Built(
                in stateDecision,
                in frame,
                in motionFacts,
                in locomotionFacts,
                in updatedRuntimeState,
                frame.WorldDirection);
            return true;
        }

        static LocomotionFrameRuntimeState ResolveTurnBackIntent(
            in LocomotionFrameBuilderInput input,
            in LocomotionFramePrepareFacts prepareFacts,
            in LocomotionSpatialFacts spatialFacts,
            out LocomotionTurnBackIntent intent)
        {
            MovementInputIntent intentValue = prepareFacts.Intent;
            LocomotionFrameRuntimeState runtimeState = input.RuntimeState;
            LocomotionTurnBackIntent pendingIntent = runtimeState.PendingTurnBackIntent;
            TurnBackIntentResolution resolution = TurnBackIntentResolver.Resolve(
                in intentValue,
                prepareFacts.FrameGait,
                input.CurrentPhase,
                in spatialFacts,
                input.CurrentStep,
                in pendingIntent,
                runtimeState.PreviousWorldDirection,
                120f,
                2);
            if (resolution.HasLog)
            {
                LocomotionTurnBackIntent logIntent = resolution.LogIntent;
                LocomotionDiagnostics.LogTurnBackIntent(
                    input.ActiveStatePath,
                    resolution.LogReason,
                    input.CurrentStep,
                    in logIntent,
                    resolution.ObservedAngle);
            }

            intent = resolution.Intent;
            LocomotionTurnBackIntent resolvedPendingIntent = resolution.PendingIntent;
            return runtimeState.WithPendingTurnBackIntent(in resolvedPendingIntent);
        }

        static LocomotionFrameRuntimeState ConsumeTurnBackIntentIfEntered(
            in LocomotionDecisionFacts decisionFacts,
            in CharacterStateMachineFrame stateFrame,
            int currentStep,
            in LocomotionFrameRuntimeState runtimeState)
        {
            if (stateFrame.LocomotionPhase != BasicMovementPhase.TurnBack ||
                !decisionFacts.TurnBackIntent.IsValid)
            {
                return runtimeState;
            }

            LocomotionTurnBackIntent consumedIntent = decisionFacts.TurnBackIntent;
            LocomotionDiagnostics.LogTurnBackIntent(
                stateFrame.Snapshot.ActivePath,
                "consumed-enter-turnback",
                currentStep,
                in consumedIntent);
            LocomotionTurnBackIntent clearedIntent = LocomotionTurnBackIntent.None;
            return runtimeState.WithPendingTurnBackIntent(in clearedIntent);
        }

        static LocomotionFrameRuntimeState ApplyStateMachineOutputs(
            in CharacterStateMachineFrame stateFrame,
            in LocomotionFrameRuntimeState runtimeState)
        {
            bool previousRunLatch = runtimeState.RunLatchActive;
            LocomotionFrameRuntimeState updated = runtimeState;
            if (stateFrame.ResetRunLatch)
                updated = updated.WithRunLatch(false, !updated.CurrentIntent.HasMoveIntent);
            if (stateFrame.SetRunLatch)
                updated = updated.WithRunLatch(true, false);
            if (previousRunLatch != updated.RunLatchActive || stateFrame.SetRunLatch || stateFrame.ResetRunLatch)
            {
                LocomotionDiagnostics.LogRunLatchOutputApplied(
                    stateFrame.Snapshot.ActivePath,
                    stateFrame.SetRunLatch,
                    stateFrame.ResetRunLatch,
                    previousRunLatch,
                    updated.RunLatchActive,
                    stateFrame.LocomotionPhase,
                    stateFrame.Snapshot.Variant,
                    stateFrame.ActionCompleted);
            }

            return updated;
        }

        static LocomotionFrameRuntimeState UpdatePhaseGaitMemory(
            in LocomotionFrameRuntimeState runtimeState,
            BasicMovementPhase phase,
            BasicMovementGait frameGait)
        {
            if (phase == BasicMovementPhase.MoveStop)
                return runtimeState.WithMoveStopGait(true, frameGait);
            if (phase != BasicMovementPhase.TurnBack)
                return runtimeState.WithMoveStopGait(false, runtimeState.ActiveMoveStopGait);
            return runtimeState;
        }

        static bool TryNormalizePlanar(Vector3 value, out Vector3 normalized)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            if (sqrMagnitude <= DirectionSqrEpsilon)
            {
                normalized = Vector3.zero;
                return false;
            }

            normalized = value / Mathf.Sqrt(sqrMagnitude);
            return true;
        }
    }
}
