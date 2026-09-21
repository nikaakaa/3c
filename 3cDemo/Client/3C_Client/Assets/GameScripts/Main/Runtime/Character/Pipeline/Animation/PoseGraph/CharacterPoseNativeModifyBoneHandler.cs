using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;
using FlowCanvas;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeModifyBoneHandler :
        ICharacterPoseNativeNodeHandler
    {
        readonly PoseNodeId m_NodeId;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        readonly CharacterComponentBonePose[] m_ComponentScratch;
        int m_BoneIndex = -1;
        int m_PageIndex = -1;
        CharacterPoseNativeComponentPoseValue m_Output;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        bool m_FrameOpen;
        int m_CommittedPageIndex = -1;
        bool m_Disposed;

        internal CharacterPoseNativeModifyBoneHandler(
            PoseNodeId nodeId,
            CharacterAnimationRigPayload rig,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            if (!nodeId.IsValid)
                throw new ArgumentException(
                    "Pose native Modify Bone handler identity is invalid.",
                    nameof(nodeId));
            m_NodeId = nodeId;
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
            m_ComponentScratch = new CharacterComponentBonePose[
                m_Rig.PoseBoneCount];
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.ModifyBone;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind || !node.BoneId.IsValid ||
                node.ModifyBoneOperations == ModifyBoneOperationMask.None)
            {
                throw new InvalidOperationException(
                    $"Modify Bone handler '{NodeId}' has invalid node configuration.");
            }
            m_BoneIndex = m_Rig.RequirePoseBoneIndex(node.BoneId);
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime) => RequireAlive();

        public void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            ClearFrame();
            m_CommittedPageIndex = -1;
        }

        public void BeginFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException(
                    $"Modify Bone '{NodeId}' frame is already open.");
            m_FrameOpen = true;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            m_Output = null;
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
            if (portId.Value != "result")
                throw new InvalidOperationException(
                    $"Modify Bone '{NodeId}' has no output '{portId}'.");
            if (m_Output != null)
                return m_Output;
            RequireFrame();
            CharacterPoseNativeComponentPoseValue inputValue =
                runtime.ReadInput<CharacterPoseNativeComponentPoseValue>(
                    node,
                    "pose");
            CharacterPoseNativePoseReadBinding input = inputValue.Native;
            if (!input.IsValid ||
                input.Space != CharacterPoseSpace.Component ||
                input.Availability[0] != AnimationPoseAvailability.Pose ||
                input.CompletedAt[0] != input.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Modify Bone '{NodeId}' received an unavailable Component Pose.");
            }
            float weight = ResolveWeight(runtime, node);
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                runtime.CurrentLineage.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.CopyMetadata(
                in input,
                in m_WriteBinding);
            for (int i = 0; i < m_Rig.PoseBoneCount; i++)
            {
                AnimationLocalBonePose value = input.DenseLocalPoses[i];
                if (!value.IsValid)
                    throw new InvalidOperationException(
                        $"Modify Bone '{NodeId}' received invalid Component bone #{i}.");
                m_ComponentScratch[i] = new CharacterComponentBonePose(
                    value.Position,
                    value.Rotation,
                    value.Scale);
            }
            ApplyModification(node, weight);
            NativeSlice<AnimationLocalBonePose> outputPoses =
                m_WriteBinding.DenseLocalPoses;
            for (int i = 0; i < m_ComponentScratch.Length; i++)
            {
                CharacterComponentBonePose value = m_ComponentScratch[i];
                outputPoses[i] = new AnimationLocalBonePose(
                    value.Position,
                    value.Rotation,
                    value.Scale);
            }
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(
                    in m_WriteBinding,
                    CharacterPoseSpace.Component);
            m_Output = new CharacterPoseNativeComponentPoseValue(NodeId, in output);
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
            if (m_Output == null)
                return;
            if (m_Output.Native.CompletionIdentity != lineage.CompletionIdentity ||
                m_Output.Native.Space != CharacterPoseSpace.Component ||
                m_Output.Native.Availability[0] != AnimationPoseAvailability.Pose ||
                m_Output.Native.CompletedAt[0] != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Modify Bone '{NodeId}' produced an invalid pending Component Pose.");
            }
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativePortValue output)
        {
            RequireAlive();
            RequireFrame();
            if (m_Output != null)
                m_CommittedPageIndex = m_PageIndex;
            ClearFrame();
        }

        public void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason) =>
            ClearFrame();

        public void Stop(CharacterPoseNativeGraphRuntime runtime)
        {
            m_CommittedPageIndex = -1;
            ClearFrame();
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_OutputBuffer.Dispose();
            m_SecondaryOutputBuffer.Dispose();
            ClearFrame();
        }

        static float ResolveWeight(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPoseCanvasNode node)
        {
            ValueInput<CharacterPoseNativeParameterValue> input =
                node.GetInputPort("weight") as
                ValueInput<CharacterPoseNativeParameterValue>;
            if (input == null || !input.isConnected)
                return node.Weight;
            CharacterPoseNativeParameterValue value = runtime.ReadInput<
                CharacterPoseNativeParameterValue>(node, "weight");
            if (value.Value.Kind != EventGraphValueKind.Float32 ||
                !float.IsFinite(value.Value.Float32Value) ||
                value.Value.Float32Value < 0f ||
                value.Value.Float32Value > 1f)
            {
                throw new InvalidOperationException(
                    $"Modify Bone '{node.NodeId}' weight input must be a Float32 in [0, 1].");
            }
            return value.Value.Float32Value;
        }

        void ApplyModification(CharacterPoseCanvasNode node, float weight)
        {
            CharacterComponentBonePose value = m_ComponentScratch[m_BoneIndex];
            int parentIndex = m_Rig.GetPoseParentIndex(m_BoneIndex);
            CharacterComponentBonePose parent = parentIndex >= 0
                ? m_ComponentScratch[parentIndex]
                : default;
            bool local = node.ModifyBoneReferenceSpace ==
                ModifyBoneReferenceSpace.Local;
            ModifyBoneOperationMask operations = node.ModifyBoneOperations;
            Vector3 position = value.Position;
            Quaternion rotation = value.Rotation;
            Vector3 scale = value.Scale;
            if ((operations & ModifyBoneOperationMask.Position) != 0)
            {
                Vector3 delta = node.ModifyPosition * weight;
                position += local && parentIndex >= 0
                    ? parent.Rotation * Vector3.Scale(parent.Scale, delta)
                    : delta;
            }
            if ((operations & ModifyBoneOperationMask.Rotation) != 0)
            {
                Quaternion delta = Quaternion.Slerp(
                    Quaternion.identity,
                    node.ModifyRotation,
                    weight);
                rotation = local
                    ? (rotation * delta).normalized
                    : (delta * rotation).normalized;
            }
            if ((operations & ModifyBoneOperationMask.Scale) != 0)
            {
                scale = Vector3.Scale(
                    scale,
                    Vector3.Lerp(Vector3.one, node.ModifyScale, weight));
            }
            m_ComponentScratch[m_BoneIndex] = new CharacterComponentBonePose(
                position,
                rotation,
                scale);
        }

        void ClearFrame()
        {
            m_FrameOpen = false;
            m_Output = null;
            m_WriteBinding = default;
            m_PageIndex = -1;
        }

        void RequireFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Modify Bone '{NodeId}' has no open frame.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeModifyBoneHandler));
        }
    }
}
