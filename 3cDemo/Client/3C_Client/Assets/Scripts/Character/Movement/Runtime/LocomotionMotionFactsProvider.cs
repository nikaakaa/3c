using ThirdPersonAnimation;
using ThirdPersonCharacterConfig;
using ThirdPersonCharacterStateMachine;
using UnityEngine;

namespace ThirdPersonMovement
{
    internal interface ILocomotionMotionFactsProviderHost
    {
        RunLocomotionAnimationConfigSO AnimationConfig { get; }
        string ActiveStatePath { get; }
        float HostYaw { get; }
        AnimationPhasePlaybackProgress CurrentAnimationPlaybackProgress { get; }
        AnimationPhasePlaybackProgress ResolvePlaybackProgress(BasicMovementPhase phase);
    }

    internal sealed class LocomotionMotionFactsProvider
    {
        readonly ILocomotionMotionFactsProviderHost host;
        readonly LocomotionRuntimeStateStore stateStore;

        public LocomotionMotionFactsProvider(
            ILocomotionMotionFactsProviderHost host,
            LocomotionRuntimeStateStore stateStore)
        {
            this.host = host;
            this.stateStore = stateStore;
        }

        public BasicMovementMotionFacts Resolve(
            in CharacterStateMachineFrame stateFrame,
            BasicMovementGait gait,
            int currentStep)
        {
            if (stateFrame.LocomotionPhase == BasicMovementPhase.TurnBack)
                return ResolveTurnBackRootMotionFacts(in stateFrame, gait, currentStep);

            return Resolve(stateFrame.LocomotionPhase, gait);
        }

        public BasicMovementMotionFacts Resolve(BasicMovementPhase phase, BasicMovementGait gait)
        {
            if (phase == BasicMovementPhase.TurnBack)
                return ResolveTurnBackRootMotionFacts(phase, gait);

            RunLocomotionAnimationConfigSO animationConfig = host.AnimationConfig;
            if (animationConfig == null)
            {
                stateStore.ResetMotionPlaybackWindow(phase);
                return BasicMovementMotionFacts.None(phase);
            }

            string aliasKey = animationConfig.ResolveAliasKey(phase, gait);
            LocomotionMotionProfileSO profile = animationConfig.ResolveMotionProfile(phase, gait, aliasKey);
            if (profile == null)
            {
                stateStore.ResetMotionPlaybackWindow(phase);
                return BasicMovementMotionFacts.None(phase);
            }

            AnimationPhasePlaybackProgress progress = host.ResolvePlaybackProgress(phase);
            AnimationMotionPlaybackWindow playbackWindow = BuildMotionPlaybackWindow(phase, gait, in progress);
            AnimationMotionProfileSample sample = ResolveBakedMotionProfileSample(
                animationConfig,
                phase,
                gait,
                aliasKey,
                in playbackWindow);
            if (!sample.HasMotionContribution)
                return BasicMovementMotionFacts.None(phase);

            return new BasicMovementMotionFacts(
                true,
                sample.LocalPlanarDelta,
                sample.YawDelta,
                sample.SourcePhase,
                sample.SourceAliasKey);
        }

        BasicMovementMotionFacts ResolveTurnBackRootMotionFacts(BasicMovementPhase phase, BasicMovementGait gait)
        {
            return ResolveTurnBackRootMotionFacts(
                phase,
                gait,
                TurnBackMotionPolicy.Default,
                Vector3.zero,
                Vector3.zero,
                0);
        }

        BasicMovementMotionFacts ResolveTurnBackRootMotionFacts(
            in CharacterStateMachineFrame stateFrame,
            BasicMovementGait gait,
            int currentStep)
        {
            TurnBackMotionPolicy policy = stateFrame.HasTurnBackMotionPolicy
                ? stateFrame.TurnBackMotionPolicy
                : TurnBackMotionPolicy.Default;
            return ResolveTurnBackRootMotionFacts(
                stateFrame.LocomotionPhase,
                gait,
                policy,
                stateFrame.TurnBackWorldDirection,
                stateFrame.TurnBackEntryBasisForward,
                currentStep,
                stateFrame.TimelineFacts);
        }

        BasicMovementMotionFacts ResolveTurnBackRootMotionFacts(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            TurnBackMotionPolicy policy,
            Vector3 lockedWorldDirection,
            Vector3 entryPlanarBasisForward,
            int currentStep,
            StateTimelineWindowFacts timelineFacts = default)
        {
            RunLocomotionAnimationConfigSO animationConfig = host.AnimationConfig;
            string aliasKey = policy.IsEnabled ? policy.AliasKey : animationConfig != null ? animationConfig.ResolveAliasKey(phase, gait) : TurnBackMotionPolicy.DefaultAliasKey;
            AnimationPhasePlaybackProgress progress = host.ResolvePlaybackProgress(phase);
            AnimationMotionPlaybackWindow playbackWindow = stateStore.BuildMotionPlaybackWindow(
                phase,
                gait,
                aliasKey,
                in progress,
                true,
                true);
            AnimationMotionProfileSample bakedSample = TurnBackMotionResolver.RequiresBakedMotion(in policy)
                ? ResolveTurnBackBakedMotionSample(
                    animationConfig,
                    phase,
                    gait,
                    aliasKey,
                    in playbackWindow)
                : AnimationMotionProfileSample.None(phase);
            TurnBackMotionResolution resolution = TurnBackMotionResolver.Resolve(
                phase,
                aliasKey,
                in policy,
                in bakedSample,
                entryPlanarBasisForward,
                in timelineFacts);
            Vector3 appliedPlanarDelta = resolution.AppliedPlanarDelta;
            float appliedYawDelta = resolution.AppliedYawDelta;
            BasicMovementPlanarDeltaSpace deltaSpace = resolution.DeltaSpace;
            Vector3 resolvedEntryBasisForward = resolution.EntryPlanarBasisForward;
            if (resolution.EntryBasisMissing)
            {
                Vector3 rejectedPlanarDelta = resolution.RejectedPlanarDelta;
                LocomotionDiagnostics.LogTurnBackEntryBasisMissing(
                    host.ActiveStatePath,
                    currentStep,
                    phase,
                    gait,
                    aliasKey,
                    in rejectedPlanarDelta);
            }

            LocomotionDiagnostics.LogTurnBackRootMotionConsumed(
                phase,
                gait,
                aliasKey,
                in bakedSample,
                in policy,
                in playbackWindow,
                in appliedPlanarDelta,
                appliedYawDelta,
                deltaSpace,
                resolvedEntryBasisForward,
                in timelineFacts);
            LocomotionDiagnostics.LogTurnBackStatePolicy(
                host.ActiveStatePath,
                currentStep,
                phase,
                gait,
                aliasKey,
                in policy,
                lockedWorldDirection,
                resolvedEntryBasisForward,
                in appliedPlanarDelta,
                appliedYawDelta,
                in timelineFacts,
                host.CurrentAnimationPlaybackProgress,
                host.HostYaw);
            return resolution.MotionFacts;
        }

        AnimationMotionProfileSample ResolveTurnBackBakedMotionSample(
            RunLocomotionAnimationConfigSO animationConfig,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in AnimationMotionPlaybackWindow playbackWindow)
        {
            return ResolveBakedMotionProfileSample(
                animationConfig,
                phase,
                gait,
                aliasKey,
                in playbackWindow);
        }

        AnimationMotionProfileSample ResolveBakedMotionProfileSample(
            RunLocomotionAnimationConfigSO animationConfig,
            BasicMovementPhase phase,
            BasicMovementGait gait,
            string aliasKey,
            in AnimationMotionPlaybackWindow playbackWindow)
        {
            if (animationConfig == null || !playbackWindow.HasValidPlayback)
                return AnimationMotionProfileSample.None(phase);

            LocomotionMotionProfileSO profile = animationConfig.ResolveMotionProfile(phase, gait, aliasKey);
            return AnimationMotionProfileSampler.Sample(profile, in playbackWindow);
        }

        AnimationMotionPlaybackWindow BuildMotionPlaybackWindow(
            BasicMovementPhase phase,
            BasicMovementGait gait,
            in AnimationPhasePlaybackProgress progress)
        {
            return stateStore.BuildMotionPlaybackWindow(phase, gait, progress.AliasKey, in progress, true);
        }
    }
}
