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
        readonly AnimationLocalBonePose[] m_LocalScratch;
        FlowCanvas.ValueInput<CharacterPoseNativeComponentPoseValue> m_PoseInput;
        FlowCanvas.ValueInput<CharacterPoseNativeParameterValue> m_WeightInput;
        FlowCanvas.ValueInput<CharacterPoseNativeParameterValue> m_PositionInput;
        FlowCanvas.ValueInput<CharacterPoseNativeParameterValue> m_RotationInput;
        FlowCanvas.ValueInput<CharacterPoseNativeParameterValue> m_ScaleInput;
        CharacterModifyBonePosePayload m_Modification;
        int[] m_Descendants;
        int[] m_DescendantParents;
        int[] m_AffectedVirtualBones;
        int m_BoneIndex = -1;
        int m_TargetParent = -1;
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
            m_LocalScratch = new AnimationLocalBonePose[m_Rig.PhysicalBoneCount];
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.ModifyBone;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind || node.Payload is not CharacterModifyBonePosePayload modification)
                throw new InvalidOperationException($"Modify Bone handler '{NodeId}' has invalid node configuration.");
            m_Modification = modification;
            m_BoneIndex = m_Rig.RequirePoseBoneIndex(modification.BoneId);
            if (m_BoneIndex >= m_Rig.PhysicalBoneCount)
                throw new InvalidOperationException($"Modify Bone '{NodeId}' cannot target a virtual bone.");
            m_PoseInput = runtime.RequireInputPort<CharacterPoseNativeComponentPoseValue>(
                node,
                "pose");
            m_WeightInput = runtime.RequireInputPort<CharacterPoseNativeParameterValue>(
                node,
                "weight");
            if (modification.UsesPort("position"))
                m_PositionInput = runtime.RequireInputPort<CharacterPoseNativeParameterValue>(
                    node,
                    "position");
            if (modification.UsesPort("rotation"))
                m_RotationInput = runtime.RequireInputPort<CharacterPoseNativeParameterValue>(
                    node,
                    "rotation");
            if (modification.UsesPort("scale"))
                m_ScaleInput = runtime.RequireInputPort<CharacterPoseNativeParameterValue>(
                    node,
                    "scale");
            m_TargetParent = m_Rig.GetPoseParentIndex(m_BoneIndex);
            var affected = new bool[m_Rig.PhysicalBoneCount];
            var descendants = new List<int>();
            affected[m_BoneIndex] = true;
            for (int i = m_BoneIndex + 1; i < m_Rig.PhysicalBoneCount; i++)
            {
                int parent = m_Rig.GetPoseParentIndex(i);
                if (modification.PropagateToChildren && parent >= 0 && affected[parent])
                {
                    affected[i] = true;
                    descendants.Add(i);
                }
            }
            m_Descendants = descendants.ToArray();
            m_DescendantParents = new int[m_Descendants.Length];
            for (int i = 0; i < m_Descendants.Length; i++)
                m_DescendantParents[i] = m_Rig.GetPoseParentIndex(m_Descendants[i]);
            var virtualBones = new List<int>();
            for (int i = 0; i < m_Rig.VirtualBoneCount; i++)
            {
                CharacterAnimationVirtualBonePayload bone = m_Rig.VirtualBones[i];
                if (affected[bone.SourcePhysicalBoneIndex] || affected[bone.TargetPhysicalBoneIndex])
                    virtualBones.Add(i);
            }
            m_AffectedVirtualBones = virtualBones.ToArray();
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
            if (m_Output != null &&
                m_Output.CompletionIdentity == runtime.CurrentLineage.CompletionIdentity)
                return m_Output;
            RequireFrame();
            CharacterPoseNativeComponentPoseValue inputValue =
                runtime.ReadInput(
                    m_PoseInput,
                    m_NodeId,
                    "pose");
            ref readonly CharacterPoseNativePoseReadBinding input = ref inputValue.Native;
            if (!input.IsValid ||
                input.Space != CharacterPoseSpace.Component ||
                (input.Availability[0] != AnimationPoseAvailability.Pose && input.Availability[0] != AnimationPoseAvailability.NoPose) ||
                input.CompletedAt[0] != input.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Modify Bone '{NodeId}' received an unavailable Component Pose.");
            }
            float weight = ResolveWeight(runtime);
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                runtime.CurrentLineage.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.CopyMetadata(
                in input,
                in m_WriteBinding);
            if (input.Availability[0] == AnimationPoseAvailability.Pose)
            {
                bool modifies = ResolveModification(runtime, weight,
                    out Vector3 position, out Quaternion rotation, out Vector3 scale);
                NativeSlice<AnimationLocalBonePose> outputPoses = m_WriteBinding.DenseLocalPoses;
                NativeSlice<AnimationLocalBonePose> inputPoses =
                    input.DenseLocalPoses;
                for (int i = 0; i < m_Rig.PoseBoneCount; i++)
                {
                    ref readonly AnimationLocalBonePose value = ref inputPoses[i];
                    if (!value.IsValid)
                        throw new InvalidOperationException(
                            $"Modify Bone '{NodeId}' received invalid Component bone #{i}.");
                    if (modifies)
                        m_ComponentScratch[i] = new CharacterComponentBonePose(in value);
                    else
                        outputPoses[i] = value;
                }
                if (modifies)
                {
                    ApplyModification(position, rotation, scale, weight);
                    for (int i = 0; i < m_ComponentScratch.Length; i++)
                    {
                        ref readonly CharacterComponentBonePose value =
                            ref m_ComponentScratch[i];
                        outputPoses[i] = new AnimationLocalBonePose(in value);
                    }
                }
            }
            else
            {
                NativeSlice<AnimationLocalBonePose> emptyOutput = m_WriteBinding.DenseLocalPoses;
                NativeSlice<AnimationLocalBonePose> inputPoses =
                    input.DenseLocalPoses;
                for (int i = 0; i < emptyOutput.Length; i++)
                    emptyOutput[i] = inputPoses[i];
            }
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(
                    in m_WriteBinding,
                    CharacterPoseSpace.Component);
            m_Output = CharacterPoseNativeComponentPoseValue.Reuse(
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
            if (m_Output == null ||
                m_Output.CompletionIdentity != lineage.CompletionIdentity)
                return;
            if (m_Output.Native.CompletionIdentity != lineage.CompletionIdentity ||
                m_Output.Native.Space != CharacterPoseSpace.Component ||
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
            if (m_Output != null &&
                m_Output.CompletionIdentity == lineage.CompletionIdentity)
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

        float ResolveWeight(CharacterPoseNativeGraphRuntime runtime)
        {
            if (!runtime.TryReadInput(
                    m_WeightInput,
                    m_NodeId,
                    "weight",
                    out CharacterPoseNativeParameterValue value))
                return m_Modification.Weight;
            if (value.Value.Kind != EventGraphValueKind.Float32 ||
                !float.IsFinite(value.Value.Float32Value) ||
                value.Value.Float32Value < 0f ||
                value.Value.Float32Value > 1f)
            {
                throw new InvalidOperationException(
                    $"Modify Bone '{m_NodeId}' weight input must be a Float32 in [0, 1].");
            }
            return value.Value.Float32Value;
        }

        bool ResolveModification(CharacterPoseNativeGraphRuntime runtime, float weight,
            out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = m_Modification.Position;
            rotation = m_Modification.Rotation;
            scale = m_Modification.Scale;
            if (m_PositionInput != null)
                position = ReadTransform(
                    runtime,
                    m_PositionInput,
                    "position",
                    EventGraphValueKind.Vector3).Vector3Value;
            if (m_RotationInput != null)
                rotation = ReadTransform(
                    runtime,
                    m_RotationInput,
                    "rotation",
                    EventGraphValueKind.Quaternion).QuaternionValue;
            if (m_ScaleInput != null)
                scale = ReadTransform(
                    runtime,
                    m_ScaleInput,
                    "scale",
                    EventGraphValueKind.Vector3).Vector3Value;
            if (!CharacterPoseConstraintMath.IsFinite(position) ||
                !CharacterPoseConstraintMath.IsFinite(rotation) || Quaternion.Dot(rotation, rotation) <= 0f ||
                !CharacterPoseConstraintMath.IsUsableScale(scale))
                throw new InvalidOperationException($"Modify Bone '{NodeId}' received an invalid transform.");
            if (weight == 0f)
                return false;
            return (m_Modification.PositionMode != ModifyBoneMode.Ignore &&
                    (m_Modification.PositionMode != ModifyBoneMode.Add || !position.Equals(Vector3.zero))) ||
                   (m_Modification.RotationMode != ModifyBoneMode.Ignore &&
                    (m_Modification.RotationMode != ModifyBoneMode.Add || rotation.x != 0f || rotation.y != 0f || rotation.z != 0f)) ||
                   (m_Modification.ScaleMode != ModifyBoneMode.Ignore &&
                    (m_Modification.ScaleMode != ModifyBoneMode.Add || !scale.Equals(Vector3.one)));
        }

        void ApplyModification(Vector3 position, Quaternion rotation, Vector3 scale, float weight)
        {
            CharacterComponentBonePose original = m_ComponentScratch[m_BoneIndex];
            int parent = m_TargetParent;
            bool parentLocal = m_Modification.ReferenceSpace == ModifyBoneReferenceSpace.ParentLocal && parent >= 0;
            if (parentLocal)
            {
                if (!CharacterPoseConstraintMath.TryCreateLocal(original, m_ComponentScratch[parent], out var local))
                    throw new InvalidOperationException($"Modify Bone '{NodeId}' cannot resolve parent space.");
                original = new CharacterComponentBonePose(in local);
            }
            Vector3 targetPosition = m_Modification.PositionMode switch
            {
                ModifyBoneMode.Add => original.Position + position,
                ModifyBoneMode.Replace => position,
                _ => original.Position
            };
            Quaternion targetRotation = m_Modification.RotationMode switch
            {
                ModifyBoneMode.Add => (rotation * original.Rotation).normalized,
                ModifyBoneMode.Replace => rotation.normalized,
                _ => original.Rotation
            };
            Vector3 targetScale = m_Modification.ScaleMode switch
            {
                ModifyBoneMode.Add => Vector3.Scale(original.Scale, scale),
                ModifyBoneMode.Replace => scale,
                _ => original.Scale
            };
            var blended = new AnimationLocalBonePose(
                Vector3.Lerp(original.Position, targetPosition, weight),
                Quaternion.Slerp(original.Rotation, targetRotation, weight),
                Vector3.Lerp(original.Scale, targetScale, weight));
            if (!blended.IsValid || !CharacterPoseConstraintMath.IsUsableScale(blended.Scale))
                throw new InvalidOperationException($"Modify Bone '{NodeId}' produced an invalid target transform.");
            if (blended.Position.Equals(original.Position) &&
                blended.Rotation.Equals(original.Rotation) &&
                blended.Scale.Equals(original.Scale))
                return;
            for (int i = 0; i < m_Descendants.Length; i++)
            {
                int bone = m_Descendants[i];
                if (!CharacterPoseConstraintMath.TryCreateLocal(m_ComponentScratch[bone],
                        m_ComponentScratch[m_DescendantParents[i]], out m_LocalScratch[bone]))
                    throw new InvalidOperationException($"Modify Bone '{NodeId}' cannot preserve local bone #{bone}.");
            }
            if (!CharacterPoseConstraintMath.TryCreateComponent(blended, parentLocal ? parent : -1,
                    m_ComponentScratch, 0, out m_ComponentScratch[m_BoneIndex]))
                throw new InvalidOperationException($"Modify Bone '{NodeId}' produced an invalid target transform.");
            for (int i = 0; i < m_Descendants.Length; i++)
            {
                int bone = m_Descendants[i];
                if (!CharacterPoseConstraintMath.TryCreateComponent(m_LocalScratch[bone], m_DescendantParents[i],
                        m_ComponentScratch, 0, out m_ComponentScratch[bone]))
                    throw new InvalidOperationException($"Modify Bone '{NodeId}' cannot rebuild descendant #{bone}.");
            }
            for (int i = 0; i < m_AffectedVirtualBones.Length; i++)
            {
                int index = m_AffectedVirtualBones[i];
                CharacterAnimationVirtualBonePayload bone = m_Rig.VirtualBones[index];
                m_ComponentScratch[m_Rig.PhysicalBoneCount + index] = CharacterPoseConstraintMath.CreateVirtualComponent(
                    m_ComponentScratch[bone.SourcePhysicalBoneIndex], m_ComponentScratch[bone.TargetPhysicalBoneIndex]);
            }
        }

        EventGraphValue ReadTransform(CharacterPoseNativeGraphRuntime runtime,
            FlowCanvas.ValueInput<CharacterPoseNativeParameterValue> input,
            string port, EventGraphValueKind kind)
        {
            CharacterPoseNativeParameterValue value = runtime.ReadInput(
                input,
                m_NodeId,
                port);
            if (value.CompletionIdentity != runtime.CurrentLineage.CompletionIdentity ||
                value.Value.Kind != kind)
            {
                throw new InvalidOperationException(
                    $"Modify Bone '{runtime.Graph.GraphId}/{m_NodeId}/{port}' requires a same-frame {kind}.");
            }
            return value.Value;
        }

        void ClearFrame()
        {
            m_FrameOpen = false;
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
