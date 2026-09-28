using System;
using System.Collections.Generic;
using FlowCanvas;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface ICharacterPoseNativeStateMachineSource : IDisposable, Diagnostics.ICharacterNativeStateCaptureSource, ICharacterPoseNativePhaseSource
    {
        void PrepareGraphs(CharacterPoseNativeGraphRuntime runtime);
        IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage);
        void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeSourceDemand demand,
            in CharacterPoseNativeFrameLineage lineage,
            ulong barrierIdentity);
        CharacterPoseNativeLocalPoseValue Evaluate(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            ulong barrierIdentity);
        void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage);
        void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason);
        void ResetFrame();
    }

    internal sealed class CharacterPoseNativeStateMachineHandler :
        ICharacterPoseNativeNodeHandler, Diagnostics.ICharacterNativeStateCaptureSource, ICharacterPoseNativePhaseSource
    {
        readonly PoseNodeId m_NodeId;
        readonly ICharacterPoseNativeStateMachineSource m_Source;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        CharacterPoseNativeLocalPoseValue m_StatePose;
        CharacterPoseNativeLocalPoseValue m_Output;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        int m_PageIndex = -1;
        int m_CommittedPageIndex = -1;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterPoseNativeStateMachineHandler(
            PoseNodeId nodeId,
            ICharacterPoseNativeStateMachineSource source,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            if (!nodeId.IsValid)
                throw new ArgumentException(
                    "Pose native StateMachine handler identity is invalid.",
                    nameof(nodeId));
            m_NodeId = nodeId;
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
        }

        public int StateCaptureCount => m_Source.StateCaptureCount;
        public int PhasePlayerCount => m_Source.PhasePlayerCount;
        public Presentation.AnimationClipPlayerRuntime ReadPhasePlayer(int index) => m_Source.ReadPhasePlayer(index);
        public Diagnostics.CharacterNativeStateCaptureRow ReadStateCapture(int index) => m_Source.ReadStateCapture(index);
        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.PoseStateMachine;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            if (runtime.Graph.RequireNode(NodeId).Kind != Kind)
                throw new InvalidOperationException(
                    $"StateMachine handler '{NodeId}' does not match its graph node.");
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            m_Source.PrepareGraphs(runtime);
        }

        public void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            m_Source.ResetFrame();
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
                    $"StateMachine '{NodeId}' frame is already open.");
            m_FrameOpen = true;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            m_StatePose = null;
            m_WriteBinding = default;
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            RequireFrame();
            return m_Source.PrepareFrame(runtime, node, in input, in lineage);
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
                    $"StateMachine '{NodeId}' has no output '{portId}'.");
            if (m_Output != null &&
                m_Output.CompletionIdentity == runtime.CurrentLineage.CompletionIdentity)
                return m_Output;
            RequireFrame();
            if (m_StatePose == null || !m_StatePose.Native.IsValid)
                throw new InvalidOperationException(
                    $"StateMachine '{NodeId}' has no evaluated state Pose.");
            CharacterPoseNativePoseReadBinding input = m_StatePose.Native;
            if (input.Space != CharacterPoseSpace.Local ||
                input.DenseLocalPoses.Length != m_OutputBuffer.BoneCount ||
                input.PoseParameters.Length != m_OutputBuffer.ParameterCount)
            {
                throw new InvalidOperationException(
                    $"StateMachine '{NodeId}' state Pose layout is invalid.");
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
            poses.CopyFrom(input.DenseLocalPoses);
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
            if (!demand.IsValid || demand.Lineage != lineage || barrierIdentity == 0)
                throw new InvalidOperationException(
                    $"StateMachine '{NodeId}' evaluation preparation is invalid.");
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
            m_StatePose = m_Source.Evaluate(
                runtime,
                in lineage,
                barrierIdentity);
            if (m_StatePose == null || !m_StatePose.Native.IsValid ||
                m_StatePose.Native.CompletionIdentity != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"StateMachine '{NodeId}' did not produce the current state Pose.");
            }
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
                m_Output.Native.InvalidReason[0] != AnimationPoseNativeInvalidReason.None)
            {
                throw new InvalidOperationException(
                    $"StateMachine '{NodeId}' pending output is invalid.");
            }
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            RequireAlive();
            RequireFrame();
            m_Source.CommitFrame(runtime, in lineage);
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
                    $"StateMachine '{NodeId}' frame is not open.");
        }

        void ClearFrame()
        {
            m_FrameOpen = false;
            m_StatePose = null;
            m_WriteBinding = default;
            m_PageIndex = -1;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeStateMachineHandler));
        }
    }
}
