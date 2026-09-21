using System;
using System.Collections.Generic;
using FlowCanvas;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface ICharacterPoseNativeHistoryCollectorSource : IDisposable
    {
        CharacterPoseHistoryReadView BeginFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage);
        void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeSourceDemand demand,
            in CharacterPoseNativeFrameLineage lineage,
            ulong barrierIdentity);
        void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativePoseReadBinding output);
        void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason);
        void ResetFrame();
    }

    internal sealed class CharacterPoseNativeHistoryCollectorHandler :
        ICharacterPoseNativeNodeHandler
    {
        readonly PoseNodeId m_NodeId;
        readonly CharacterPoseHistoryId m_HistoryId;
        readonly ICharacterPoseNativeHistoryCollectorSource m_Source;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        CharacterPoseHistoryReadView m_HistoryView;
        CharacterPoseNativeLocalPoseValue m_Output;
        CharacterPoseNativeHistoryValue m_HistoryOutput;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        int m_PageIndex = -1;
        int m_CommittedPageIndex = -1;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterPoseNativeHistoryCollectorHandler(
            PoseNodeId nodeId,
            CharacterPoseHistoryId historyId,
            ICharacterPoseNativeHistoryCollectorSource source,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            if (!nodeId.IsValid || !historyId.IsValid)
                throw new ArgumentException(
                    "Pose native History Collector identity is invalid.");
            m_NodeId = nodeId;
            m_HistoryId = historyId;
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseHistoryCollector;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind ||
                !(node.Payload is CharacterPoseHistoryCollectorPayload payload) ||
                payload.HistoryId != m_HistoryId)
            {
                throw new InvalidOperationException(
                    $"History Collector '{NodeId}' does not match its graph node.");
            }
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime) => RequireAlive();

        public void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            m_Source.ResetFrame();
            m_HistoryView = default;
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
                    $"History Collector '{NodeId}' frame is already open.");
            m_FrameOpen = true;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            m_HistoryView = m_Source.BeginFrame(runtime, in input, in lineage);
            if (!m_HistoryView.IsValid)
                throw new InvalidOperationException(
                    $"History Collector '{NodeId}' returned an invalid history view.");
            m_WriteBinding = default;
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage) => null;

        public CharacterPoseNativePortValue EvaluateOutput(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            PosePortId portId,
            CharacterPoseNativeExecutionStage stage)
        {
            RequireAlive();
            RequireFrame();
            if (portId.Value == "history.pose")
            {
                m_HistoryOutput = CharacterPoseNativeHistoryValue.Reuse(
                    m_HistoryOutput,
                    NodeId,
                    runtime.CurrentLineage.CompletionIdentity,
                    m_HistoryView);
                return m_HistoryOutput;
            }
            if (portId.Value != "pose.local")
                throw new InvalidOperationException(
                    $"History Collector '{NodeId}' has no output '{portId}'.");
            if (m_Output != null &&
                m_Output.CompletionIdentity == runtime.CurrentLineage.CompletionIdentity)
                return m_Output;
            CharacterPoseNativeLocalPoseValue inputValue =
                runtime.ReadInput<CharacterPoseNativeLocalPoseValue>(
                    node,
                    "pose.local.input");
            CharacterPoseNativePoseReadBinding input = inputValue.Native;
            if (!input.IsValid || input.Space != CharacterPoseSpace.Local ||
                input.CompletionIdentity != runtime.CurrentLineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"History Collector '{NodeId}' input Pose is invalid.");
            }
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                runtime.CurrentLineage.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.CopyMetadata(
                in input,
                in m_WriteBinding);
            NativeSlice<AnimationLocalBonePose> poses =
                m_WriteBinding.DenseLocalPoses;
            for (int bone = 0; bone < poses.Length; bone++)
                poses[bone] = input.DenseLocalPoses[bone];
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(in m_WriteBinding);
            m_Output = CharacterPoseNativeLocalPoseValue.Reuse(
                m_Output,
                NodeId,
                in output);
            return m_Output;
        }

        public void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            in CharacterPoseNativeSourceDemand demand,
            ulong barrierIdentity)
        {
            RequireAlive();
            RequireFrame();
            m_Source.PrepareEvaluation(
                runtime,
                in demand,
                in lineage,
                barrierIdentity);
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
        }

        public void ValidatePending(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            if (m_Output == null || !m_Output.Native.IsValid ||
                m_Output.Native.CompletionIdentity != lineage.CompletionIdentity ||
                m_Output.Native.CompletedAt[0] != lineage.CompletionIdentity ||
                m_Output.Native.Availability[0] == AnimationPoseAvailability.Invalid ||
                m_Output.Native.InvalidReason[0] != AnimationPoseNativeInvalidReason.None ||
                !m_HistoryView.IsValid)
            {
                throw new InvalidOperationException(
                    $"History Collector '{NodeId}' pending output is invalid.");
            }
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            RequireAlive();
            RequireFrame();
            if (m_Output == null)
                throw new InvalidOperationException(
                    $"History Collector '{NodeId}' has no pending output.");
            CharacterPoseNativePoseReadBinding outputValue = m_Output.Native;
            m_Source.CommitFrame(runtime, in lineage, in outputValue);
            if (m_Output != null &&
                m_Output.CompletionIdentity == lineage.CompletionIdentity)
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
            m_Source.DiscardFrame(runtime, in lineage, reason);
            ClearFrame();
        }

        public void Stop(CharacterPoseNativeGraphRuntime runtime)
        {
            if (m_Disposed)
                return;
            if (m_FrameOpen)
            {
                CharacterPoseNativeFrameLineage lineage = runtime.CurrentLineage;
                DiscardFrame(
                    runtime,
                    in lineage,
                    CharacterPoseNativeFailureCode.Disposed);
            }
            m_Source.ResetFrame();
            m_CommittedPageIndex = -1;
            ClearFrame();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_Source.Dispose();
            m_OutputBuffer.Dispose();
            m_SecondaryOutputBuffer.Dispose();
            ClearFrame();
        }

        void RequireFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"History Collector '{NodeId}' frame is not open.");
        }

        void ClearFrame()
        {
            m_FrameOpen = false;
            m_HistoryView = default;
            m_WriteBinding = default;
            m_PageIndex = -1;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeHistoryCollectorHandler));
        }
    }
}
