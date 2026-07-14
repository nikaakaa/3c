using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Graph;
using TreeDesigner;
using ThirdPersonGameplay.Tick;

namespace ThirdPersonCharacter.Pipeline.Logic
{
    public sealed class CharacterBTSMTLPhase : IDisposable
    {
        readonly BehaviorTreeRuntime m_BehaviorTreeRuntime;
        readonly TimelinePlaybackScheduler m_TimelinePlaybackScheduler;
        readonly CharacterGraphContext m_GraphContext;

        public CharacterBTSMTLPhase(
            RunnableTree rootTreeAsset,
            CharacterGraphContext graphContext,
            IAnimationPlaybackCommandSink animationCommands,
            CharacterAnimationPresentationBindingIndex animationBindings)
        {
            m_GraphContext = graphContext;
            m_BehaviorTreeRuntime = new BehaviorTreeRuntime(rootTreeAsset, graphContext);
            m_TimelinePlaybackScheduler = new TimelinePlaybackScheduler(
                graphContext,
                animationCommands,
                animationBindings);
        }

        public void Activate()
        {
            m_BehaviorTreeRuntime.Activate();
        }

        public void Tick(GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            m_TimelinePlaybackScheduler.PrepareDecisionFacts(context);
            m_BehaviorTreeRuntime.Tick(context, frame);
            m_GraphContext.ProjectWindowFacts();
            m_TimelinePlaybackScheduler.Commit(context, frame);
        }

        public void SamplePresentation(
            GameplayPresentationFrameContext context,
            CharacterPipelineFrame frame,
            System.Collections.Generic.IReadOnlyCollection<AnimationPlaybackId> demandedPlaybacks)
        {
            m_TimelinePlaybackScheduler.SamplePresentation(context, frame, demandedPlaybacks);
        }

        public void CompletePresentationFrame(
            System.Collections.Generic.IReadOnlyCollection<AnimationPlaybackId> retiredPlaybacks)
        {
            m_TimelinePlaybackScheduler.CompletePresentationFrame(retiredPlaybacks);
        }

        public void Deactivate()
        {
            m_TimelinePlaybackScheduler.Deactivate();
            m_BehaviorTreeRuntime.Deactivate();
        }

        public void Dispose()
        {
            m_TimelinePlaybackScheduler.Dispose();
            m_BehaviorTreeRuntime.Dispose();
        }
    }
}
