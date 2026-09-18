using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface ICharacterPoseNativeClipSourceBinding
    {
        void Prepare(
            CharacterPoseNativeGraphRuntime runtime,
            AnimationClipPlayerRuntime player,
            in AnimationPoseSourceCaptureBinding capture);
        CharacterPoseSourceBinding RequireBinding(
            CharacterPoseNativeGraphRuntime runtime,
            AnimationClipPlayerRuntime player);
        void ResetFrame();
    }

    internal sealed class CharacterPoseNativeClipSourceModuleBinding :
        ICharacterPoseNativeClipSourceBinding
    {
        readonly CharacterPoseSourceModule m_Source;
        readonly Func<CharacterPoseSourceFrameLease> m_SourceLeaseProvider;
        readonly int m_BindingIndex;
        CharacterPoseSourceBinding m_Binding;
        bool m_Prepared;

        internal CharacterPoseNativeClipSourceModuleBinding(
            CharacterPoseSourceModule source,
            Func<CharacterPoseSourceFrameLease> sourceLeaseProvider,
            int bindingIndex)
        {
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_SourceLeaseProvider = sourceLeaseProvider ??
                throw new ArgumentNullException(nameof(sourceLeaseProvider));
            if (bindingIndex < 0)
                throw new ArgumentException(
                    "Native Clip source module binding is invalid.");
            m_BindingIndex = bindingIndex;
        }

        public void Prepare(
            CharacterPoseNativeGraphRuntime runtime,
            AnimationClipPlayerRuntime player,
            in AnimationPoseSourceCaptureBinding capture)
        {
            if (m_Prepared)
                throw new InvalidOperationException(
                    $"Native Clip source '{player.NodeId}' was prepared twice.");
            CharacterPoseSourceFrameLease sourceLease =
                m_SourceLeaseProvider();
            if (!sourceLease.IsValid)
                throw new InvalidOperationException(
                    $"Native Clip source '{player.NodeId}' has no active source frame.");
            CharacterPoseSourceReadinessTarget target =
                CharacterPoseSourceReadinessTarget.FromResource(
                    CharacterPoseSourcePreparationKind.ClipPlayer,
                    default,
                    player.NodeId,
                    m_BindingIndex,
                    player.Backend,
                    player.ResourceCatalogIndex,
                    player.GroupClipIndex);
            if (m_Source.TryDeferSource(in target))
                throw new InvalidOperationException(
                    $"Native Clip source '{player.NodeId}' is pending.");
            m_Binding = m_Source.PrepareNativeClipPlayer(
                sourceLease,
                player.SourceId,
                player.PlayerIndex,
                player.ClipSamples,
                in capture,
                player.NodeId,
                m_BindingIndex);
            m_Prepared = true;
        }

        public CharacterPoseSourceBinding RequireBinding(
            CharacterPoseNativeGraphRuntime runtime,
            AnimationClipPlayerRuntime player)
        {
            if (!m_Prepared || !m_Binding.IsValid)
                throw new InvalidOperationException(
                    $"Native Clip source '{player.NodeId}' has no prepared binding.");
            return m_Binding;
        }

        public void ResetFrame()
        {
            m_Binding = default;
            m_Prepared = false;
        }
    }

    internal sealed class CharacterPoseNativeClipPlayerHandler :
        ICharacterPoseNativeNodeHandler
    {
        readonly AnimationClipPlayerRuntime m_Player;
        IActionPresentationClockPolicy m_ClockPolicy = FreeRunPresentationClockPolicy.Shared;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        readonly ICharacterPoseNativeClipSourceBinding m_SourceBinding;
        AnimationScriptPlayable m_Playable;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        AnimationSelectedPosePlayerJob m_Job;
        CharacterPoseNativeLocalPoseValue m_Output;
        CharacterPoseNativeDiscontinuityValue m_Discontinuity;
        AnimationPoseSourceCaptureBinding m_Capture;
        bool m_CapturePrepared;
        bool m_EvaluationPrepared;
        bool m_FrameOpen;
        int m_CommittedPageIndex = -1;
        int m_PageIndex = -1;
        bool m_Disposed;

        internal CharacterPoseNativeClipPlayerHandler(
            AnimationClipPlayerRuntime player,
            CharacterPoseNativeNodePoseBuffer outputBuffer,
            ICharacterPoseNativeClipSourceBinding sourceBinding,
            IActionPresentationClockPolicy clockPolicy)
        {
            m_Player = player ?? throw new ArgumentNullException(nameof(player));
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SourceBinding = sourceBinding ??
                throw new ArgumentNullException(nameof(sourceBinding));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
            m_ClockPolicy = clockPolicy ??
                throw new ArgumentNullException(nameof(clockPolicy));
        }

        public PoseNodeId NodeId => m_Player.NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.ClipPlayer;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind ||
                !(node.PresentationPoseSourceSlot is CharacterClipPoseSourceSlot))
            {
                throw new InvalidOperationException(
                    $"Clip Player handler '{NodeId}' does not match its graph node.");
            }
            if (m_Player.PlayRate <= 0f || !float.IsFinite(m_Player.PlayRate))
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' has an invalid play rate.");
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime) =>
            RequireAlive();

        public void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            if (m_FrameOpen)
                m_Player.DiscardFrame();
            m_Player.Reset(PoseDiscontinuityResetReason.BranchReplacement);
            m_CommittedPageIndex = -1;
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
                    $"Clip Player '{NodeId}' frame is already open.");
            try
            {
                m_Player.BeginFrame();
                m_Player.BeginFrame(lineage.CompletionIdentity);
                m_FrameOpen = true;
                m_PageIndex = m_CommittedPageIndex < 0
                    ? 0
                    : 1 - m_CommittedPageIndex;
                ClearFrameResult();
            }
            catch
            {
                m_Player.DiscardFrame();
                throw;
            }
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            m_Player.SetRelevant(true);
            CharacterPresentationFactFrame factFrame = input.FactFrame;
            m_ClockPolicy.DriveClock(
                m_Player,
                node.AnimationChannelId,
                input.PresentationSampleTick,
                in factFrame,
                input.DeltaSeconds);
            m_Capture = m_Player.PrepareCapture(
                input.DeltaSeconds,
                m_Player.PlayRate);
            m_CapturePrepared = true;
            return new[]
            {
                new CharacterPoseNativeSourceRequest(
                    NodeId,
                    node.PresentationPoseSourceSlot,
                    m_Player.SourceId,
                    true,
                    runtime.InstanceId)
            };
        }

        public CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            if (portId.Value == "discontinuity")
                return m_Discontinuity ?? throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' discontinuity is not completed.");
            if (portId.Value != "pose")
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' has no output '{portId}'.");
            return m_Output ?? throw new InvalidOperationException(
                $"Clip Player '{NodeId}' output is not completed.");
        }

        public void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireFrame();
            if (!m_CapturePrepared ||
                demand.Lineage != runtime.CurrentLineage ||
                barrierIdentity == 0)
            {
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' source capture is not ready for evaluation.");
            }
            m_SourceBinding.Prepare(runtime, m_Player, in m_Capture);
            CharacterPoseSourceBinding source =
                m_SourceBinding.RequireBinding(runtime, m_Player);
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                lineage.CompletionIdentity);
            CharacterPoseSourceScalarReadView scalarReadView = source.ScalarReadView;
            m_Job = m_Player.PrepareJob(
                lineage.CompletionIdentity,
                in m_WriteBinding,
                source.PhysicalIdentity,
                source.SourceIndex,
                in scalarReadView);
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
                    $"Clip Player '{NodeId}' Job was not prepared for the current frame.");
            }
            m_Player.CompleteFrame();
            if (m_WriteBinding.CompletedAt[0] != lineage.CompletionIdentity)
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' Job did not complete the current frame.");
            m_Output = new CharacterPoseNativeLocalPoseValue(
                NodeId,
                new CharacterPoseNativePoseReadBinding(in m_WriteBinding));
            m_Discontinuity = new CharacterPoseNativeDiscontinuityValue(
                NodeId,
                new CharacterPoseNativePoseReadBinding(in m_WriteBinding));
        }

        public void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            if (m_Output == null)
                return;
            if (m_Output.Native.CompletionIdentity != lineage.CompletionIdentity ||
                m_Output.Native.Availability[0] != AnimationPoseAvailability.Pose ||
                m_Output.Native.CompletedAt[0] != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' produced an invalid pending Pose.");
            }
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            RequireAlive();
            RequireFrame();
            m_Player.CommitFrame();
            if (m_Output != null)
                m_CommittedPageIndex = m_PageIndex;
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
            m_Player.DiscardFrame();
            ClearFrame();
        }

        public void Stop(CharacterPoseNativeGraphRuntime runtime)
        {
            if (m_Disposed)
                return;
            if (m_FrameOpen)
                m_Player.DiscardFrame();
            m_CommittedPageIndex = -1;
            m_FrameOpen = false;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_Playable.IsValid())
                AnimancerUtilities.RemovePlayable(m_Playable);
            m_Player.Dispose();
            m_OutputBuffer.Dispose();
            m_SecondaryOutputBuffer.Dispose();
            ClearFrame();
        }

        void ClearFrame()
        {
            m_FrameOpen = false;
            ClearFrameResult();
            m_Capture = default;
            m_WriteBinding = default;
            m_PageIndex = -1;
        }

        void ClearFrameResult()
        {
            m_CapturePrepared = false;
            m_EvaluationPrepared = false;
            m_Output = null;
            m_Discontinuity = null;
            m_SourceBinding.ResetFrame();
        }

        void RequireFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Clip Player '{NodeId}' frame is not open.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeClipPlayerHandler));
        }
    }
}


