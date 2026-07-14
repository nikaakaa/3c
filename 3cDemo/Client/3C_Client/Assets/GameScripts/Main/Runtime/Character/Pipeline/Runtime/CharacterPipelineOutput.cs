using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Network;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonCamera;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    public sealed class CharacterPipelineOutput
    {
        public StrictGameplayOutput StrictGameplay { get; } = new StrictGameplayOutput();
        public PresentationOutput Presentation { get; } = new PresentationOutput();
        public CharacterSyncFacts SyncFacts { get; } = new CharacterSyncFacts();

        public void Clear()
        {
            StrictGameplay.Clear();
            Presentation.Clear();
            SyncFacts.Clear();
        }
    }

    public sealed class StrictGameplayOutput
    {
        public MotionIntent MotionIntent { get; set; }
        public MotionResult MotionResult { get; set; }
        public MotionCorrectionApplicationResult MotionCorrectionApplicationResult { get; set; }
        public MotionResolveDebugFrame MotionDebug { get; } = new MotionResolveDebugFrame();
        public List<MotionContribution> MotionContributions { get; } = new List<MotionContribution>();
        public List<MotionWarpWindow> MotionWarpWindows { get; } = new List<MotionWarpWindow>();

        public void Clear()
        {
            MotionIntent = default;
            MotionResult = default;
            MotionCorrectionApplicationResult = default;
            MotionDebug.Clear();
            MotionContributions.Clear();
            MotionWarpWindows.Clear();
        }
    }

    public sealed class PresentationOutput
    {
        public CharacterPresentationRootPose PresentationRootPose { get; set; }
        public List<AnimationPlaybackLifecycleSnapshot> AnimationPlaybacks { get; } =
            new List<AnimationPlaybackLifecycleSnapshot>();
        public List<CameraStateRequest> CameraStateRequests { get; } = new List<CameraStateRequest>();
        public List<CameraCue> CameraCues { get; } = new List<CameraCue>();
        public List<CameraResponsePolicy> CameraResponsePolicies { get; } = new List<CameraResponsePolicy>();
        public List<CameraTargetRequest> CameraTargetRequests { get; } = new List<CameraTargetRequest>();
        public List<GameplayCueFact> GameplayCues { get; } = new List<GameplayCueFact>();
        public CameraPosePlan CameraPosePlan { get; set; }
        public CameraBasisSnapshot CameraBasisSnapshot { get; set; }
        public CameraDebugSnapshot CameraDebug { get; } = new CameraDebugSnapshot();

        public void Clear()
        {
            PresentationRootPose = default;
            AnimationPlaybacks.Clear();
            CameraStateRequests.Clear();
            CameraCues.Clear();
            CameraResponsePolicies.Clear();
            CameraTargetRequests.Clear();
            GameplayCues.Clear();
            CameraPosePlan = default;
            CameraBasisSnapshot = default;
            CameraDebug.Clear();
        }
    }

    public sealed class CharacterSyncFacts
    {
        public MotionSyncDomainOutput Motion { get; } = new MotionSyncDomainOutput();
        public ActionSyncDomainOutput Action { get; } = new ActionSyncDomainOutput();
        public GameplayResultSyncDomainOutput GameplayResult { get; } = new GameplayResultSyncDomainOutput();
        public GameplayEffectSyncDomainOutput GameplayEffect { get; } = new GameplayEffectSyncDomainOutput();
        public PresentationSyncDomainOutput Presentation { get; } = new PresentationSyncDomainOutput();

        public void CollectInputFrame(CharacterInputFrame frame)
        {
            if (frame == null)
                return;

            Motion.InputFrame = frame.Clone();
            Action.ActionRequests.AddRange(frame.NewRequests);
        }

        public void Clear()
        {
            Motion.Clear();
            Action.Clear();
            GameplayResult.Clear();
            GameplayEffect.Clear();
            Presentation.Clear();
        }
    }

    public sealed class MotionSyncDomainOutput
    {
        public CharacterInputFrame InputFrame { get; set; }
        public ResolvedCharacterMotionFact ResolvedMotion { get; set; }
        public MotionCorrectionApplicationResult CorrectionApplicationResult { get; set; }
        public List<ActionMotionSample> ActionMotionSamples { get; } = new List<ActionMotionSample>();

        public void Clear()
        {
            InputFrame = null;
            ResolvedMotion = default;
            CorrectionApplicationResult = default;
            ActionMotionSamples.Clear();
        }
    }

    public sealed class ActionSyncDomainOutput
    {
        public List<CharacterInputRequest> ActionRequests { get; } = new List<CharacterInputRequest>();
        public List<ActionActivationRequest> ActivationRequests { get; } = new List<ActionActivationRequest>();
        public List<ActionActivationOutput> ActivationOutputs { get; } = new List<ActionActivationOutput>();
        public List<ActionLifecycleTransition> LifecycleTransitions { get; } = new List<ActionLifecycleTransition>();
        public List<ActionWindowSample> WindowSamples { get; } = new List<ActionWindowSample>();

        public void Clear()
        {
            ActionRequests.Clear();
            ActivationRequests.Clear();
            ActivationOutputs.Clear();
            LifecycleTransitions.Clear();
            WindowSamples.Clear();
        }
    }

    public sealed class GameplayResultSyncDomainOutput
    {
        public List<GameplayResultEvent> Events { get; } = new List<GameplayResultEvent>();

        public void Clear()
        {
            Events.Clear();
        }
    }

    public sealed class GameplayEffectSyncDomainOutput
    {
        public List<GameplayEffectLifecycleFact> LifecycleFacts { get; } = new List<GameplayEffectLifecycleFact>();
        public List<GameplayAttributeValueFact> AttributeFacts { get; } = new List<GameplayAttributeValueFact>();

        public void Clear()
        {
            LifecycleFacts.Clear();
            AttributeFacts.Clear();
        }
    }

    public sealed class PresentationSyncDomainOutput
    {
        public List<GameplayCueFact> CueEvents { get; } = new List<GameplayCueFact>();

        public void Clear()
        {
            CueEvents.Clear();
        }
    }
}
