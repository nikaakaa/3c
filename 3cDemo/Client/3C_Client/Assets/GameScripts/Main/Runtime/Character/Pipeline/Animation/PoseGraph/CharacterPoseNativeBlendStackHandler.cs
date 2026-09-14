using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface ICharacterPoseNativeBlendStackSourceBinding
    {
        IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            AnimationBlendStackRuntime stack,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage);
        void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            AnimationBlendStackRuntime stack,
            in CharacterPoseNativeSourceDemand demand,
            in CharacterPoseNativeFrameLineage lineage);
        void ResetFrame();
    }

    internal sealed class CharacterPoseNativeBlendStackSourceModuleBinding :
        ICharacterPoseNativeBlendStackSourceBinding
    {
        sealed class PendingSource
        {
            internal AnimationPoseSourceId SourceId;
            internal int SourceOwnerIndex;
            internal AnimationResolvedPoseSourceSample ActionSample;
            internal PresentationPoseSourceSample ProviderSample;
            internal AnimationPoseSourceCaptureBinding Capture;
            internal bool IsProvider;
        }

        readonly CharacterPoseSourceModule m_Source;
        readonly Func<CharacterPoseSourceFrameLease> m_SourceLeaseProvider;
        readonly Func<PoseNodeId, AnimationPoseSourceId,
            AnimationResolvedPoseSourceSample> m_ActionSampleProvider;
        readonly Func<PoseNodeId, AnimationPoseSourceId,
            PresentationPoseSourceSample> m_ProviderSampleProvider;
        readonly List<PendingSource> m_Pending =
            new List<PendingSource>();

        internal CharacterPoseNativeBlendStackSourceModuleBinding(
            CharacterPoseSourceModule source,
            Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
            Func<PoseNodeId, AnimationPoseSourceId,
                AnimationResolvedPoseSourceSample> actionSampleProvider,
            Func<PoseNodeId, AnimationPoseSourceId,
                PresentationPoseSourceSample> providerSampleProvider)
        {
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_SourceLeaseProvider = sourceLeaseProvider ??
                throw new ArgumentNullException(nameof(sourceLeaseProvider));
            m_ActionSampleProvider = actionSampleProvider ??
                throw new ArgumentNullException(nameof(actionSampleProvider));
            m_ProviderSampleProvider = providerSampleProvider ??
                throw new ArgumentNullException(nameof(providerSampleProvider));
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            AnimationBlendStackRuntime stack,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            m_Pending.Clear();
            var requests = new List<CharacterPoseNativeSourceRequest>();
            if (!stack.HasCurrentSelectionSample)
                return requests;
            var sourceIds = new HashSet<AnimationPoseSourceId>();
            for (int i = 0; i < stack.EntryCount; i++)
            {
                AnimationBlendEntryState entry = stack.GetEntryState(i);
                if (entry.IsSourcePose || !sourceIds.Add(entry.SourceId))
                    continue;
                if (entry.SourceId.SourceKind == AnimationPoseSourceKind.Timeline)
                {
                    AnimationResolvedPoseSourceSample sample =
                        m_ActionSampleProvider(stack.PoseNodeId, entry.SourceId);
                    if (sample == null || !sample.IsValid)
                        throw new InvalidOperationException(
                            $"Blend Stack '{stack.PoseNodeId}' has no Action source sample for '{entry.SourceId}'.");
                    m_Pending.Add(new PendingSource
                    {
                        SourceId = entry.SourceId,
                        SourceOwnerIndex = entry.SourceOwnerIndex,
                        ActionSample = sample,
                        Capture = stack.PrepareCapture(
                            sample,
                            input.DeltaSeconds),
                        IsProvider = false
                    });
                }
                else if (entry.SourceId.SourceKind == AnimationPoseSourceKind.MotionMatching)
                {
                    PresentationPoseSourceSample sample =
                        m_ProviderSampleProvider(stack.PoseNodeId, entry.SourceId);
                    if (sample == null || !sample.IsValid ||
                        sample.Availability != PresentationPoseSourceAvailability.Ready)
                    {
                        throw new InvalidOperationException(
                            $"Blend Stack '{stack.PoseNodeId}' has no Provider source sample for '{entry.SourceId}'.");
                    }
                    AnimationResolvedPoseSourceSample resolved =
                        m_Source.ResolveProviderSample(
                            in sample,
                            entry.SourceOwnerIndex);
                    if (resolved.Request.SourceId != entry.SourceId)
                        throw new InvalidOperationException(
                            $"Blend Stack '{stack.PoseNodeId}' Provider source identity is stale.");
                    m_Pending.Add(new PendingSource
                    {
                        SourceId = entry.SourceId,
                        SourceOwnerIndex = entry.SourceOwnerIndex,
                        ProviderSample = sample,
                        Capture = stack.PrepareCapture(
                            resolved,
                            input.DeltaSeconds),
                        IsProvider = true
                    });
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Blend Stack '{stack.PoseNodeId}' source kind '{entry.SourceId.SourceKind}' is unsupported.");
                }
                requests.Add(new CharacterPoseNativeSourceRequest(
                    stack.PoseNodeId,
                    node.PresentationPoseSourceSlot,
                    entry.SourceId,
                    true,
                    runtime.InstanceId));
            }
            return requests;
        }

        public void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            AnimationBlendStackRuntime stack,
            in CharacterPoseNativeSourceDemand demand,
            in CharacterPoseNativeFrameLineage lineage)
        {
            CharacterPoseSourceFrameLease sourceLease = m_SourceLeaseProvider();
            if (!sourceLease.IsValid)
                throw new InvalidOperationException(
                    $"Blend Stack '{stack.PoseNodeId}' has no active Source frame.");
            for (int i = 0; i < m_Pending.Count; i++)
            {
                PendingSource source = m_Pending[i];
                AnimationReadOnlyBuffer<ClipSamplePlan> clips = source.IsProvider
                    ? source.ProviderSample.Clips
                    : source.ActionSample.Request.Clips;
                CharacterPoseSourcePreparationKind kind = source.IsProvider
                    ? CharacterPoseSourcePreparationKind.Provider
                    : CharacterPoseSourcePreparationKind.Action;
                CharacterPoseSourceReadinessTarget target =
                    CharacterPoseSourceReadinessTarget.FromClips(
                        kind,
                        source.SourceId,
                        stack.PoseNodeId,
                        -1,
                        clips);
                if (m_Source.TryDeferSource(in target))
                    throw new InvalidOperationException(
                        $"Blend Stack '{stack.PoseNodeId}' source '{source.SourceId}' is pending.");
                if (source.IsProvider)
                {
                    m_Source.PrepareNativeProviderSource(
                        sourceLease,
                        source.ProviderSample,
                        source.SourceOwnerIndex,
                        in source.Capture,
                        stack.PoseNodeId);
                }
                else
                {
                    m_Source.PrepareNativeActionSource(
                        sourceLease,
                        in source.ActionSample.Request,
                        in source.Capture,
                        stack.PoseNodeId);
                }
            }
        }

        public void ResetFrame() => m_Pending.Clear();
    }

    internal sealed class CharacterPoseNativeBlendStackHandler :
        ICharacterPoseNativeNodeHandler
    {
        readonly AnimationBlendStackRuntime m_Stack;
        readonly CharacterPoseSourceModule m_Source;
        readonly ICharacterPoseNativeBlendStackSourceBinding m_SourceBinding;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        AnimationScriptPlayable m_Playable;
        AnimationSlotBlendJob m_Job;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        CharacterPoseNativeLocalPoseValue m_Output;
        bool m_FrameOpen;
        int m_PageIndex = -1;
        int m_CommittedPageIndex = -1;
        bool m_EvaluationPrepared;
        bool m_Disposed;

        internal CharacterPoseNativeBlendStackHandler(
            AnimationBlendStackRuntime stack,
            CharacterPoseSourceModule source,
            ICharacterPoseNativeBlendStackSourceBinding sourceBinding,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            m_Stack = stack ?? throw new ArgumentNullException(nameof(stack));
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_SourceBinding = sourceBinding ??
                throw new ArgumentNullException(nameof(sourceBinding));
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
        }

        public PoseNodeId NodeId => m_Stack.PoseNodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.BlendStack;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            if (runtime.Graph.RequireNode(NodeId).Kind != Kind)
                throw new InvalidOperationException(
                    $"Blend Stack handler '{NodeId}' does not match its graph node.");
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime) => RequireAlive();

        public void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            if (m_FrameOpen)
                m_Stack.DiscardFrame();
            m_CommittedPageIndex = -1;
            m_SourceBinding.ResetFrame();
            ClearFrame();
        }

        public void BeginFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException(
                    $"Blend Stack '{NodeId}' frame is already open.");
            m_Stack.BeginFrame();
            m_FrameOpen = true;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            m_Output = null;
            m_EvaluationPrepared = false;
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            m_Stack.Advance(input.DeltaSeconds);
            m_Stack.BeginSourceFrame(lineage.CompletionIdentity);
            return m_SourceBinding.PrepareFrame(
                runtime,
                m_Stack,
                node,
                in input,
                in lineage);
        }

        public CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            if (portId.Value != "pose")
                throw new InvalidOperationException(
                    $"Blend Stack '{NodeId}' has no output '{portId}'.");
            return m_Output ?? throw new InvalidOperationException(
                $"Blend Stack '{NodeId}' output is not completed.");
        }

        public void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireFrame();
            if (barrierIdentity == 0 || demand.Lineage != runtime.CurrentLineage)
                throw new InvalidOperationException(
                    $"Blend Stack '{NodeId}' evaluation preparation identity is invalid.");
            m_SourceBinding.PrepareEvaluation(
                runtime,
                m_Stack,
                in demand,
                in lineage);
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                lineage.CompletionIdentity);
            m_Job = m_Stack.PrepareSlotJob(
                lineage.CompletionIdentity,
                in m_WriteBinding,
                m_Source);
            m_Stack.PrepareCompletion(lineage.CompletionIdentity);
            if (!m_Playable.IsValid())
            {
                m_Playable = runtime.InstanceContext.Animancer.Graph.InsertOutputJob(
                    m_Job);
                m_Playable.SetProcessInputs(true);
            }
            else
            {
                m_Playable.SetJobData(m_Job);
            }
            m_EvaluationPrepared = true;
        }

        public void EvaluateFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireFrame();
            if (!m_EvaluationPrepared ||
                m_WriteBinding.CompletionIdentity != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Blend Stack '{NodeId}' Job was not prepared for the current frame.");
            }
            m_Stack.CompleteFrame(lineage.CompletionIdentity);
            if (m_WriteBinding.CompletedAt[0] != lineage.CompletionIdentity)
                throw new InvalidOperationException(
                    $"Blend Stack '{NodeId}' Job did not complete the current frame.");
            m_Output = new CharacterPoseNativeLocalPoseValue(
                NodeId,
                new CharacterPoseNativePoseReadBinding(in m_WriteBinding));
        }

        public void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            if (m_Output == null ||
                m_Output.Native.CompletionIdentity != lineage.CompletionIdentity ||
                m_Output.Native.Space != CharacterPoseSpace.Local ||
                m_Output.Native.CompletedAt[0] != lineage.CompletionIdentity ||
                m_Output.Native.Availability[0] != AnimationPoseAvailability.Pose &&
                m_Output.Native.Availability[0] != AnimationPoseAvailability.NoPose)
            {
                throw new InvalidOperationException(
                    $"Blend Stack '{NodeId}' produced an invalid pending Pose.");
            }
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            RequireAlive();
            RequireFrame();
            m_Stack.CommitFrame();
            if (m_Output != null)
                m_CommittedPageIndex = m_PageIndex;
            m_SourceBinding.ResetFrame();
            ClearFrame();
        }

        public void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason)
        {
            RequireAlive();
            if (!m_FrameOpen)
                return;
            m_Stack.DiscardFrame();
            m_SourceBinding.ResetFrame();
            ClearFrame();
        }

        public void Stop(CharacterPoseNativeGraphRuntime runtime)
        {
            if (m_Disposed)
                return;
            if (m_FrameOpen)
                m_Stack.DiscardFrame();
            m_CommittedPageIndex = -1;
            m_SourceBinding.ResetFrame();
            ClearFrame();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_Playable.IsValid())
                AnimancerUtilities.RemovePlayable(m_Playable);
            m_Stack.Dispose();
            m_OutputBuffer.Dispose();
            m_SecondaryOutputBuffer.Dispose();
            m_SourceBinding.ResetFrame();
            ClearFrame();
        }

        void ClearFrame()
        {
            m_FrameOpen = false;
            m_EvaluationPrepared = false;
            m_Output = null;
            m_WriteBinding = default;
            m_PageIndex = -1;
        }

        void RequireFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Blend Stack '{NodeId}' frame is not open.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeBlendStackHandler));
        }
    }
}
