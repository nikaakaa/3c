using System;
using System.Collections.Generic;
using FlowCanvas;
using Unity.Collections;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface ICharacterPoseNativeLinkedPoseSource : IDisposable
    {
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

    internal sealed class CharacterPoseNativeLinkedPoseHandler :
        ICharacterPoseNativeNodeHandler
    {
        readonly PoseNodeId m_NodeId;
        readonly ICharacterPoseNativeLinkedPoseSource m_Source;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        CharacterPoseNativeLocalPoseValue m_SourcePose;
        CharacterPoseNativeLocalPoseValue m_Output;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        int m_PageIndex = -1;
        int m_CommittedPageIndex = -1;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterPoseNativeLinkedPoseHandler(
            PoseNodeId nodeId,
            ICharacterPoseNativeLinkedPoseSource source,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            if (!nodeId.IsValid)
                throw new ArgumentException(
                    "Pose native Linked Pose handler identity is invalid.",
                    nameof(nodeId));
            m_NodeId = nodeId;
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.LinkedPoseCall;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind || !(node.Payload is CharacterLinkedPoseCallPayload payload) ||
                !payload.GroupId.IsValid || !payload.InterfaceId.IsValid ||
                !payload.EntryId.IsValid)
            {
                throw new InvalidOperationException(
                    $"Linked Pose handler '{NodeId}' does not match its graph node.");
            }
            int outputCount = 0;
            for (int i = 0; i < node.DynamicPorts.Count; i++)
            {
                CharacterPoseDynamicPort port = node.DynamicPorts[i];
                if (port.Direction == CharacterPosePortDirection.Output &&
                    port.Kind == CharacterPosePortKind.LocalPose)
                    outputCount++;
            }
            if (outputCount != 1)
                throw new InvalidOperationException(
                    $"Linked Pose handler '{NodeId}' requires one Local Pose output port.");
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime) => RequireAlive();

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
                    $"Linked Pose '{NodeId}' frame is already open.");
            m_FrameOpen = true;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            m_SourcePose = null;
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
            CharacterPoseDynamicPort port = FindOutputPort(node, portId);
            if (port == null || port.Kind != CharacterPosePortKind.LocalPose)
                throw new InvalidOperationException(
                    $"Linked Pose '{NodeId}' has no Local Pose output '{portId}'.");
            if (m_Output != null &&
                m_Output.CompletionIdentity == runtime.CurrentLineage.CompletionIdentity)
                return m_Output;
            RequireFrame();
            if (m_SourcePose == null || !m_SourcePose.Native.IsValid)
                throw new InvalidOperationException(
                    $"Linked Pose '{NodeId}' has no evaluated Pose.");
            CharacterPoseNativePoseReadBinding input = m_SourcePose.Native;
            if (input.Space != CharacterPoseSpace.Local ||
                input.DenseLocalPoses.Length != m_OutputBuffer.BoneCount ||
                input.PoseParameters.Length != m_OutputBuffer.ParameterCount)
            {
                throw new InvalidOperationException(
                    $"Linked Pose '{NodeId}' output Pose layout is invalid.");
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
            m_SourcePose = m_Source.Evaluate(
                runtime,
                in lineage,
                barrierIdentity);
            if (m_SourcePose == null ||
                !m_SourcePose.Native.IsValid ||
                m_SourcePose.Native.CompletionIdentity != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Linked Pose '{NodeId}' did not produce the current Pose.");
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
                    $"Linked Pose '{NodeId}' pending output is invalid.");
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

        static CharacterPoseDynamicPort FindOutputPort(
            CharacterPoseCanvasNode node,
            PosePortId portId)
        {
            for (int i = 0; i < node.DynamicPorts.Count; i++)
            {
                CharacterPoseDynamicPort port = node.DynamicPorts[i];
                if (port.Direction == CharacterPosePortDirection.Output &&
                    port.PortId == portId)
                    return port;
            }
            return null;
        }

        void RequireFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Linked Pose '{NodeId}' frame is not open.");
        }

        void ClearFrame()
        {
            m_FrameOpen = false;
            m_SourcePose = null;
            m_WriteBinding = default;
            m_PageIndex = -1;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeLinkedPoseHandler));
        }
    }
}
