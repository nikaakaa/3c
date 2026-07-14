using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline;

namespace ThirdPersonCharacter.Pipeline.Network
{
    public sealed class CharacterNetworkSendStage
    {
        readonly CharacterSyncFacts m_Output = new CharacterSyncFacts();

        public MotionSyncDomainOutput Motion => m_Output.Motion;
        public ActionSyncDomainOutput Action => m_Output.Action;
        public GameplayResultSyncDomainOutput GameplayResult => m_Output.GameplayResult;
        public GameplayEffectSyncDomainOutput GameplayEffect => m_Output.GameplayEffect;
        public PresentationSyncDomainOutput Presentation => m_Output.Presentation;

        public void Clear()
        {
            m_Output.Clear();
        }

        public void Collect(CharacterPipelineFrame frame)
        {
            m_Output.Clear();
            Motion.InputFrame = frame.Output.SyncFacts.Motion.InputFrame;
            Motion.ResolvedMotion = frame.Output.SyncFacts.Motion.ResolvedMotion;
            Motion.CorrectionApplicationResult = frame.Output.SyncFacts.Motion.CorrectionApplicationResult;
            Motion.ActionMotionSamples.AddRange(frame.Output.SyncFacts.Motion.ActionMotionSamples);

            Action.ActionRequests.AddRange(frame.Output.SyncFacts.Action.ActionRequests);
            Action.ActivationRequests.AddRange(frame.Output.SyncFacts.Action.ActivationRequests);
            Action.ActivationOutputs.AddRange(frame.Output.SyncFacts.Action.ActivationOutputs);
            Action.LifecycleTransitions.AddRange(frame.Output.SyncFacts.Action.LifecycleTransitions);
            Action.WindowSamples.AddRange(frame.Output.SyncFacts.Action.WindowSamples);

            GameplayResult.Events.AddRange(frame.Output.SyncFacts.GameplayResult.Events);
            GameplayEffect.LifecycleFacts.AddRange(frame.Output.SyncFacts.GameplayEffect.LifecycleFacts);
            GameplayEffect.AttributeFacts.AddRange(frame.Output.SyncFacts.GameplayEffect.AttributeFacts);
            Presentation.CueEvents.AddRange(frame.Output.SyncFacts.Presentation.CueEvents);
        }
    }
}
