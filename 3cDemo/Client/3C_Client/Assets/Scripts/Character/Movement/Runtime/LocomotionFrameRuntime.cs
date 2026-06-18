using ThirdPersonAnimation;
using ThirdPersonCharacterStateMachine;

namespace ThirdPersonMovement
{
    internal interface ILocomotionFrameRuntimeOutputHost : ILocomotionMotionFactsProviderHost
    {
        CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot { get; }
        void WriteLocomotionFacts(in CharacterRuntimeLocomotionFacts facts);
        void WriteLocomotionPreemptionFact(in LocomotionPreemptionFact fact);
    }

    internal sealed class LocomotionFrameRuntime
    {
        readonly LocomotionFrameBuilder builder;
        readonly LocomotionRuntimeStateStore stateStore;
        readonly LocomotionPrepareFactsProvider prepareFactsProvider;
        readonly LocomotionSpatialFactsProvider spatialFactsProvider;
        readonly LocomotionMotionFactsProvider motionFactsProvider;
        readonly ILocomotionFrameRuntimeOutputHost host;

        public LocomotionFrameRuntime(
            LocomotionFrameBuilder builder,
            LocomotionRuntimeStateStore stateStore,
            LocomotionPrepareFactsProvider prepareFactsProvider,
            LocomotionSpatialFactsProvider spatialFactsProvider,
            LocomotionMotionFactsProvider motionFactsProvider,
            ILocomotionFrameRuntimeOutputHost host)
        {
            this.builder = builder;
            this.stateStore = stateStore;
            this.prepareFactsProvider = prepareFactsProvider;
            this.spatialFactsProvider = spatialFactsProvider;
            this.motionFactsProvider = motionFactsProvider;
            this.host = host;
        }

        public bool TryPrepareDecisionFrame(
            in BasicLocomotionInputSnapshot input,
            CharacterStateMachineRunner runner,
            int currentStep,
            out LocomotionDecisionFrame decisionFrame)
        {
            if (runner == null)
            {
                decisionFrame = default;
                return false;
            }

            if (!prepareFactsProvider.TryBuildPreparationContext(
                    in input,
                    runner,
                    currentStep,
                    out LocomotionFramePreparationContext context))
            {
                decisionFrame = default;
                return false;
            }

            LocomotionFrameBuilderInput builderInput = context.Input;
            LocomotionFramePrepareFacts prepareFacts = builder.ResolvePrepareFacts(in builderInput);
            BasicMovementSettings baseSettings = context.BaseSettings;
            BasicMovementPhase currentPhase = context.CurrentPhase;
            BasicMovementSettings settings = prepareFactsProvider.ResolveMovementSettings(
                prepareFacts.FrameGait,
                in baseSettings);
            BasicMovementPhaseFacts phaseFacts = prepareFactsProvider.ResolvePhaseFacts(
                currentPhase,
                context.CurrentPhaseTime,
                prepareFacts.FrameGait,
                input.DeltaTime,
                in settings);
            MovementInputIntent prepareIntent = prepareFacts.Intent;
            LocomotionSpatialFacts spatialFacts = spatialFactsProvider.Resolve(in input, in prepareIntent);
            if (!builder.TryPrepareDecisionFrame(
                    in builderInput,
                    in prepareFacts,
                    in settings,
                    in phaseFacts,
                    in spatialFacts,
                    out decisionFrame,
                    out LocomotionFrameBuilderResult result))
            {
                return false;
            }

            stateStore.ApplyFrameState(result.RuntimeState);
            return true;
        }

        public bool TryEvaluatePreparedGameplayDecision(
            in LocomotionDecisionFrame decisionFrame,
            CharacterStateMachineRunner runner,
            in CharacterInputRequestFact inputRequest,
            StateTimelineWindowFacts currentTimelineFacts,
            int currentStep,
            out LocomotionStateDecisionFrame stateDecision)
        {
            if (runner == null)
            {
                stateDecision = default;
                return false;
            }

            BasicLocomotionInputSnapshot frameInput = decisionFrame.Input;
            BasicMovementSettings settings = decisionFrame.Settings;
            CharacterStateMachineSnapshot runnerSnapshot = runner.Snapshot;
            BasicMovementPhase runnerPhase = CharacterStateDomainView.FromSnapshot(in runnerSnapshot).LocomotionPhase;
            LocomotionFrameBuilderInput builderInput = BuildFrameInput(
                in frameInput,
                currentStep,
                runnerPhase,
                runner.StateTime,
                in settings,
                in inputRequest,
                currentTimelineFacts);
            if (!builder.TryEvaluatePreparedGameplayDecision(
                    in decisionFrame,
                    runner,
                    in builderInput,
                    out stateDecision,
                    out LocomotionFrameBuilderResult result))
            {
                return false;
            }

            stateStore.ApplyFrameState(result.RuntimeState);
            if (stateDecision.ConsumedLocomotionPreemption)
            {
                LocomotionPreemptionFact none = LocomotionPreemptionFact.None;
                stateStore.ClearTurnBackPreemptionResidue();
                host.WriteLocomotionPreemptionFact(in none);
            }
            return true;
        }

        public bool TryEvaluatePreparedGameplayDecision(
            in LocomotionDecisionFrame decisionFrame,
            CharacterStateMachineRunner runner,
            in CharacterInputRequestFact inputRequest,
            int currentStep,
            out LocomotionStateDecisionFrame stateDecision)
        {
            if (runner == null)
            {
                stateDecision = default;
                return false;
            }

            StateTimelineWindowFacts currentTimelineFacts = ResolveCurrentTimelineFacts(
                runner,
                in decisionFrame,
                in inputRequest,
                currentStep);
            return TryEvaluatePreparedGameplayDecision(
                in decisionFrame,
                runner,
                in inputRequest,
                currentTimelineFacts,
                currentStep,
                out stateDecision);
        }

        public bool TryBuildMotionFromStateDecision(
            in LocomotionStateDecisionFrame stateDecision,
            int currentStep,
            out BasicLocomotionFrame frame,
            out CharacterStateMachineFrame stateFrame)
        {
            if (!stateDecision.HasStateFrame)
            {
                frame = default;
                stateFrame = default;
                return false;
            }

            CharacterStateMachineFrame stateFrameForMotion = stateDecision.StateFrame;
            BasicMovementMotionFacts motionFacts = motionFactsProvider.Resolve(
                in stateFrameForMotion,
                stateDecision.FrameGait,
                currentStep);
            AnimationPhasePlaybackProgress progress = host.CurrentAnimationPlaybackProgress;
            LocomotionFrameRuntimeState runtimeState = stateStore.CaptureFrameState();
            if (!builder.TryBuildMotionFromStateDecision(
                    in stateDecision,
                    currentStep,
                    in motionFacts,
                    in runtimeState,
                    in progress,
                    out frame,
                    out stateFrame,
                    out LocomotionFrameBuilderResult result))
            {
                return false;
            }

            stateStore.ApplyFrameResult(in result);
            CharacterRuntimeLocomotionFacts locomotionFacts = result.LocomotionFacts;
            host.WriteLocomotionFacts(in locomotionFacts);
            return true;
        }

        LocomotionFrameBuilderInput BuildFrameInput(
            in BasicLocomotionInputSnapshot input,
            int currentStep,
            BasicMovementPhase currentPhase,
            float currentPhaseTime,
            in BasicMovementSettings baseSettings,
            in CharacterInputRequestFact inputRequest,
            in StateTimelineWindowFacts currentTimelineFacts)
        {
            return new LocomotionFrameBuilderInput(
                input,
                currentStep,
                currentPhase,
                currentPhaseTime,
                baseSettings,
                inputRequest,
                currentTimelineFacts,
                host.RuntimeBlackboardSnapshot,
                stateStore.CaptureFrameState(),
                host.ActiveStatePath);
        }

        StateTimelineWindowFacts ResolveCurrentTimelineFacts(
            CharacterStateMachineRunner runner,
            in LocomotionDecisionFrame decisionFrame,
            in CharacterInputRequestFact inputRequest,
            int currentStep)
        {
            CharacterStateMachineSnapshot snapshot = runner.Snapshot;
            LocomotionDecisionFacts facts = decisionFrame.Facts;
            CharacterRuntimeBlackboardSnapshot blackboardSnapshot = host.RuntimeBlackboardSnapshot;
            CharacterStateMachineContext context = new CharacterStateMachineContext(
                decisionFrame.Input.DeltaTime,
                currentStep,
                in facts,
                inputRequest,
                blackboardSnapshot);
            return CharacterStateTimelineFactSampler.SampleCurrent(
                runner.Definition,
                snapshot,
                in context,
                runner.StateTime,
                inputRequest.RequestType());
        }
    }
}
