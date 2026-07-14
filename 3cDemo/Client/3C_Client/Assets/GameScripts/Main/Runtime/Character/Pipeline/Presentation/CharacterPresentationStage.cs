using System;
using System.Collections.Generic;
using Animancer;
using BTSMTL.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Presentation.Animancer;
using ThirdPersonGameplay.Tick;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    public sealed class CharacterPresentationStage : IDisposable
    {
        readonly IAnimationPlaybackBatchSource m_AnimationCommands;
        readonly List<AnimationPlaybackCommand> m_CommandBuffer = new List<AnimationPlaybackCommand>();
        readonly HashSet<AnimationPlaybackId> m_DemandedPlaybacks = new HashSet<AnimationPlaybackId>();
        readonly List<AnimationPlaybackId> m_RetiredPlaybacks = new List<AnimationPlaybackId>();
        readonly List<AnimationPlaybackLifecycleSnapshot> m_LifecycleSnapshots =
            new List<AnimationPlaybackLifecycleSnapshot>();
        readonly AnimancerPlaybackAdapter m_AnimationAdapter;
        readonly AnimationPlaybackLifecycle m_AnimationLifecycle;
        readonly CharacterPresentationInterpolator m_Interpolator;
        readonly CharacterAnimationTracePublisher m_TracePublisher;
        readonly CharacterGraphContext m_GraphContext;

        public CharacterPresentationStage(
            AnimancerComponent animancer,
            CharacterAnimationPresentationBindingIndex animationBindings,
            ICharacterLogicPosePort logicPosePort,
            Transform visualRoot,
            IAnimationPlaybackBatchSource animationCommands,
            CharacterGraphContext graphContext,
            CharacterAnimationTracePublisher tracePublisher)
        {
            m_AnimationCommands = animationCommands ?? throw new ArgumentNullException(nameof(animationCommands));
            m_GraphContext = graphContext ?? throw new ArgumentNullException(nameof(graphContext));
            m_AnimationAdapter = new AnimancerPlaybackAdapter(animancer, animationBindings, true);
            m_AnimationLifecycle = new AnimationPlaybackLifecycle(animationBindings, m_AnimationAdapter);
            m_Interpolator = new CharacterPresentationInterpolator(logicPosePort, visualRoot);
            m_TracePublisher = tracePublisher ?? throw new ArgumentNullException(nameof(tracePublisher));
        }

        public IReadOnlyCollection<AnimationPlaybackId> DemandedPlaybacks => m_DemandedPlaybacks;
        public IReadOnlyCollection<AnimationPlaybackId> RetiredPlaybacks => m_RetiredPlaybacks;

        public void Activate()
        {
            m_Interpolator.Reset();
        }

        public void CaptureLogicSample(GameplayLogicTickContext context, CharacterPipelineFrame frame)
        {
            m_Interpolator.CaptureLogicSample(context, frame);
            frame.Output.Presentation.GameplayCues.AddRange(frame.NetworkInput.Presentation.Cues);
            frame.Output.Presentation.GameplayCues.AddRange(frame.Output.SyncFacts.Presentation.CueEvents);
        }

        public void PrepareAnimationSampling()
        {
            m_AnimationCommands.CopyPendingTo(m_CommandBuffer);
            m_AnimationLifecycle.CollectSampleDemand(m_CommandBuffer, m_DemandedPlaybacks);
        }

        public void Update(GameplayPresentationFrameContext context, CharacterPipelineFrame frame)
        {
            if (!m_Interpolator.HasLogicSample)
            {
                if (frame != null)
                    frame.Output.Presentation.PresentationRootPose = default;
                return;
            }

            m_AnimationCommands.CopyPendingTo(m_CommandBuffer);
            m_AnimationLifecycle.Apply(m_CommandBuffer, context.ScaledDeltaSeconds, m_RetiredPlaybacks);
            m_AnimationLifecycle.BuildSnapshot(m_LifecycleSnapshots);
            if (frame != null)
            {
                frame.Output.Presentation.AnimationPlaybacks.Clear();
                frame.Output.Presentation.AnimationPlaybacks.AddRange(m_LifecycleSnapshots);
            }
            m_TracePublisher.PublishPlaybackLifecycle(
                Diagnostics,
                m_CommandBuffer,
                m_LifecycleSnapshots,
                m_RetiredPlaybacks);
            m_AnimationCommands.Acknowledge(m_CommandBuffer);
            m_CommandBuffer.Clear();

            if (!m_Interpolator.TryResolve(
                    context,
                    out CharacterPresentationRootPose rootPose,
                    out CharacterVisualPose visualPose,
                    out float alpha))
                return;
            if (frame != null)
                frame.Output.Presentation.PresentationRootPose = rootPose;
            m_Interpolator.ApplyVisualPose(visualPose);
            m_TracePublisher.PublishPresentationInterpolation(
                Diagnostics,
                rootPose.Valid,
                visualPose.Valid,
                alpha,
                context.ScaledDeltaSeconds,
                m_Interpolator.PreviousLogicTick,
                m_Interpolator.CurrentLogicTick,
                visualPose.Position);
        }

        public void Deactivate()
        {
            m_AnimationCommands.Clear();
            m_AnimationLifecycle.Reset();
            m_CommandBuffer.Clear();
            m_DemandedPlaybacks.Clear();
            m_RetiredPlaybacks.Clear();
            m_LifecycleSnapshots.Clear();
            m_Interpolator.Reset();
        }

        public void Dispose()
        {
            Deactivate();
            m_AnimationAdapter.Dispose();
        }

        RuntimeDiagnosticsContext Diagnostics => m_GraphContext.RuntimeDiagnostics;
    }
}
