using ThirdPersonAnimation;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace ThirdPersonMovement
{
    internal interface ILocomotionPrepareFactsProviderHost
    {
        BasicMovementConfigSO MovementConfig { get; }
        RunLocomotionAnimationConfigSO AnimationConfig { get; }
        CharacterRuntimeBlackboardSnapshot RuntimeBlackboardSnapshot { get; }
        string ActiveStatePath { get; }
        void AdvanceAnimationPlaybackProgress(float deltaTime);
        AnimationPhasePlaybackProgress ResolvePlaybackProgress(BasicMovementPhase phase);
        void SubmitFormalConfigMissing(string eventId, string message);
    }

    internal readonly struct LocomotionFramePreparationContext
    {
        public LocomotionFramePreparationContext(
            LocomotionFrameBuilderInput input,
            BasicMovementSettings baseSettings,
            BasicMovementPhase currentPhase,
            float currentPhaseTime)
        {
            Input = input;
            BaseSettings = baseSettings;
            CurrentPhase = currentPhase;
            CurrentPhaseTime = currentPhaseTime;
        }

        public LocomotionFrameBuilderInput Input { get; }
        public BasicMovementSettings BaseSettings { get; }
        public BasicMovementPhase CurrentPhase { get; }
        public float CurrentPhaseTime { get; }
    }

    internal sealed class LocomotionPrepareFactsProvider
    {
        readonly ILocomotionPrepareFactsProviderHost host;
        readonly LocomotionRuntimeStateStore stateStore;

        public LocomotionPrepareFactsProvider(
            ILocomotionPrepareFactsProviderHost host,
            LocomotionRuntimeStateStore stateStore)
        {
            this.host = host;
            this.stateStore = stateStore;
        }

        public bool TryBuildPreparationContext(
            in BasicLocomotionInputSnapshot input,
            CharacterStateMachineRunner runner,
            int currentStep,
            out LocomotionFramePreparationContext context)
        {
            BasicMovementConfigSO movementConfig = host.MovementConfig;
            if (movementConfig == null)
            {
                host.SubmitFormalConfigMissing("movement-config-missing", "CharacterConfigSO.Movement is missing. Locomotion facts cannot be prepared.");
                context = default;
                return false;
            }

            BasicMovementSettings baseSettings = BasicMovementSettings.FromConfig(movementConfig);
            host.AdvanceAnimationPlaybackProgress(input.DeltaTime);
            CharacterStateMachineSnapshot runnerSnapshot = runner.Snapshot;
            BasicMovementPhase currentPhase = CharacterStateDomainView.FromSnapshot(in runnerSnapshot).LocomotionPhase;
            LocomotionFrameBuilderInput builderInput = new LocomotionFrameBuilderInput(
                input,
                currentStep,
                currentPhase,
                runner.StateTime,
                baseSettings,
                default,
                StateTimelineWindowFacts.None(runnerSnapshot.ActiveState),
                host.RuntimeBlackboardSnapshot,
                stateStore.CaptureFrameState(),
                host.ActiveStatePath);
            context = new LocomotionFramePreparationContext(
                builderInput,
                baseSettings,
                currentPhase,
                runner.StateTime);
            return true;
        }

        public BasicMovementSettings ResolveMovementSettings(
            BasicMovementGait gait,
            in BasicMovementSettings baseSettings)
        {
            RunLocomotionAnimationConfigSO animationConfig = host.AnimationConfig;
            if (animationConfig == null)
                return baseSettings;

            return animationConfig.ApplyPhaseTiming(gait, in baseSettings);
        }

        public BasicMovementPhaseFacts ResolvePhaseFacts(
            BasicMovementPhase phase,
            float currentPhaseTime,
            BasicMovementGait gait,
            float deltaTime,
            in BasicMovementSettings settings)
        {
            float nextPhaseTime = currentPhaseTime + Mathf.Max(0f, deltaTime);
            RunLocomotionAnimationConfigSO animationConfig = host.AnimationConfig;
            if (animationConfig == null)
                return BasicMovementPhaseFacts.FromTiming(phase, nextPhaseTime, in settings);

            LocomotionAnimationPhaseConfig phaseConfig = animationConfig.ResolvePhaseConfig(phase, gait);
            AnimationPhasePlaybackProgress progress = host.ResolvePlaybackProgress(phase);
            AnimationPhaseTimelineFacts facts = AnimationPhaseTimelineSampler.Sample(phase, in phaseConfig, nextPhaseTime, in progress);
            return new BasicMovementPhaseFacts(facts.CanExit);
        }
    }
}
