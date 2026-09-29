using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;
using FlowCanvas;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeAdditivePoseHandler :
        ICharacterPoseNativeNodeHandler
    {
        readonly PoseNodeId m_NodeId;
        readonly (Vector3 Position, Quaternion InverseRotation, Vector3 Scale)[] m_ReferenceBones;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        FlowCanvas.ValueInput<CharacterPoseNativeLocalPoseValue> m_BasePoseInput;
        FlowCanvas.ValueInput<CharacterPoseNativeLocalPoseValue> m_OverlayPoseInput;
        FlowCanvas.ValueInput<CharacterPoseNativeParameterValue> m_WeightInput;
        float m_DefaultWeight;
        AdditiveScalePolicy m_ScalePolicy;
        int m_PageIndex = -1;
        ulong m_NextContinuityIdentity = 1;
        ulong m_ContinuityIdentity;
        ulong m_LastBaseContinuity;
        ulong m_LastOverlayContinuity;
        float m_LastWeight = float.NaN;
        ulong m_CommittedContinuityIdentity;
        ulong m_CommittedBaseContinuity;
        ulong m_CommittedOverlayContinuity;
        float m_CommittedWeight = float.NaN;
        CharacterPoseNativeLocalPoseValue m_Output;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        bool m_FrameOpen;
        int m_CommittedPageIndex = -1;
        bool m_Disposed;

        internal CharacterPoseNativeAdditivePoseHandler(
            PoseNodeId nodeId,
            CharacterAnimationRigPayload rig,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            if (!nodeId.IsValid)
                throw new ArgumentException(
                    "Pose native Additive Pose handler identity is invalid.",
                    nameof(nodeId));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            m_ReferenceBones = new (Vector3, Quaternion, Vector3)[rig.PoseBoneCount];
            for (int i = 0; i < m_ReferenceBones.Length; i++)
            {
                AnimationLocalBonePose reference = rig.GetReferenceLocalPose(i);
                m_ReferenceBones[i] = (reference.Position, Quaternion.Inverse(reference.Rotation), reference.Scale);
            }
            m_NodeId = nodeId;
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.AdditivePose;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind)
                throw new InvalidOperationException(
                    $"Additive Pose handler '{NodeId}' does not match its graph node.");
            if (!string.Equals(
                    node.AdditiveReferencePoseId,
                    AnimationAdditiveReferencePoseIds.RigReference,
                    StringComparison.Ordinal) ||
                node.AdditiveReferenceSpace != AdditiveReferenceSpace.Local)
            {
                throw new InvalidOperationException(
                    $"Additive Pose '{NodeId}' requires the RigReference Local reference pose.");
            }
            if (node.AdditiveScalePolicy != AdditiveScalePolicy.Multiply &&
                node.AdditiveScalePolicy != AdditiveScalePolicy.AddDelta)
            {
                throw new InvalidOperationException(
                    $"Additive Pose '{NodeId}' has an unsupported scale policy.");
            }
            m_ScalePolicy = node.AdditiveScalePolicy;
            m_BasePoseInput = runtime.RequireInputPort<CharacterPoseNativeLocalPoseValue>(
                node,
                "base");
            m_OverlayPoseInput = runtime.RequireInputPort<CharacterPoseNativeLocalPoseValue>(
                node,
                "overlay");
            m_WeightInput = runtime.RequireInputPort<CharacterPoseNativeParameterValue>(
                node,
                "weight");
            m_DefaultWeight = node.Weight;
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime) => RequireAlive();

        public void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            m_ContinuityIdentity = 0;
            m_LastBaseContinuity = 0;
            m_LastOverlayContinuity = 0;
            m_LastWeight = float.NaN;
            m_CommittedContinuityIdentity = 0;
            m_CommittedBaseContinuity = 0;
            m_CommittedOverlayContinuity = 0;
            m_CommittedWeight = float.NaN;
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
                    $"Additive Pose '{NodeId}' frame is already open.");
            m_FrameOpen = true;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            m_ContinuityIdentity = m_CommittedContinuityIdentity;
            m_LastBaseContinuity = m_CommittedBaseContinuity;
            m_LastOverlayContinuity = m_CommittedOverlayContinuity;
            m_LastWeight = m_CommittedWeight;
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
                    $"Additive Pose '{NodeId}' has no output '{portId}'.");
            if (m_Output != null &&
                m_Output.CompletionIdentity == runtime.CurrentLineage.CompletionIdentity)
                return m_Output;
            RequireFrame();
            CharacterPoseNativeLocalPoseValue basePose =
                runtime.ReadInput(
                    m_BasePoseInput,
                    m_NodeId,
                    "base");
            CharacterPoseNativeLocalPoseValue additivePose =
                runtime.ReadInput(
                    m_OverlayPoseInput,
                    m_NodeId,
                    "overlay");
            CharacterPoseNativePoseReadBinding baseBinding =
                RequireAvailable(basePose, "Base");
            CharacterPoseNativePoseReadBinding additiveBinding =
                RequireAvailable(additivePose, "Additive");
            float weight = ResolveWeight(runtime);
            m_ContinuityIdentity = ResolveContinuity(
                baseBinding.ContinuityIdentity[0],
                additiveBinding.ContinuityIdentity[0],
                weight);
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                runtime.CurrentLineage.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.CopyAttributes(
                in baseBinding,
                in m_WriteBinding);
            ApplyAdditive(
                in baseBinding,
                in additiveBinding,
                weight,
                m_ScalePolicy);
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
                m_Output.Native.Space != CharacterPoseSpace.Local ||
                m_Output.Native.Availability[0] != AnimationPoseAvailability.Pose ||
                m_Output.Native.CompletedAt[0] != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Additive Pose '{NodeId}' produced an invalid pending Pose.");
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
            {
                m_CommittedPageIndex = m_PageIndex;
                m_CommittedContinuityIdentity = m_ContinuityIdentity;
                m_CommittedBaseContinuity = m_LastBaseContinuity;
                m_CommittedOverlayContinuity = m_LastOverlayContinuity;
                m_CommittedWeight = m_LastWeight;
            }
            ClearFrame();
        }

        public void DiscardFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            CharacterPoseNativeFailureCode reason) =>
            DiscardPending();

        public void Stop(CharacterPoseNativeGraphRuntime runtime)
        {
            m_CommittedPageIndex = -1;
            m_CommittedContinuityIdentity = 0;
            m_CommittedBaseContinuity = 0;
            m_CommittedOverlayContinuity = 0;
            m_CommittedWeight = float.NaN;
            DiscardPending();
        }

        void DiscardPending()
        {
            m_ContinuityIdentity = m_CommittedContinuityIdentity;
            m_LastBaseContinuity = m_CommittedBaseContinuity;
            m_LastOverlayContinuity = m_CommittedOverlayContinuity;
            m_LastWeight = m_CommittedWeight;
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

        CharacterPoseNativePoseReadBinding RequireAvailable(
            CharacterPoseNativeLocalPoseValue value,
            string branch)
        {
            CharacterPoseNativePoseReadBinding binding = value.Native;
            if (!binding.IsValid ||
                binding.Space != CharacterPoseSpace.Local ||
                binding.Availability[0] != AnimationPoseAvailability.Pose ||
                binding.CompletedAt[0] != binding.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Additive Pose '{NodeId}' {branch} input is unavailable.");
            }
            return binding;
        }

        float ResolveWeight(CharacterPoseNativeGraphRuntime runtime)
        {
            if (!runtime.TryReadInput(
                    m_WeightInput,
                    m_NodeId,
                    "weight",
                    out CharacterPoseNativeParameterValue value))
                return m_DefaultWeight;
            if (value.Value.Kind != EventGraphValueKind.Float32 ||
                !float.IsFinite(value.Value.Float32Value) ||
                value.Value.Float32Value < 0f ||
                value.Value.Float32Value > 1f)
            {
                throw new InvalidOperationException(
                    $"Additive Pose '{m_NodeId}' weight input must be a Float32 in [0, 1].");
            }
            return value.Value.Float32Value;
        }

        ulong ResolveContinuity(
            ulong baseContinuity,
            ulong additiveContinuity,
            float weight)
        {
            if (m_ContinuityIdentity != 0 &&
                m_LastBaseContinuity == baseContinuity &&
                m_LastOverlayContinuity == additiveContinuity &&
                m_LastWeight == weight)
            {
                return m_ContinuityIdentity;
            }
            if (m_NextContinuityIdentity == ulong.MaxValue)
                throw new InvalidOperationException(
                    $"Additive Pose '{NodeId}' continuity identity was exhausted.");
            m_LastBaseContinuity = baseContinuity;
            m_LastOverlayContinuity = additiveContinuity;
            m_LastWeight = weight;
            return m_ContinuityIdentity = m_NextContinuityIdentity++;
        }

        void ApplyAdditive(
            in CharacterPoseNativePoseReadBinding basePose,
            in CharacterPoseNativePoseReadBinding additivePose,
            float weight,
            AdditiveScalePolicy scalePolicy)
        {
            NativeSlice<AnimationLocalBonePose> outputPoses =
                m_WriteBinding.DenseLocalPoses;
            NativeSlice<AnimationBlendBoneVelocity> outputVelocities =
                m_WriteBinding.DenseVelocities;
            NativeSlice<AnimationPoseAvailability> outputAvailability =
                m_WriteBinding.Availability;
            NativeSlice<ulong> outputContinuity =
                m_WriteBinding.ContinuityIdentity;
            NativeSlice<PoseDiscontinuityNative> outputDiscontinuity =
                m_WriteBinding.Discontinuity;
            NativeSlice<AnimationPoseNativeInvalidReason> outputInvalidReason =
                m_WriteBinding.InvalidReason;
            NativeSlice<ulong> outputCompletedAt = m_WriteBinding.CompletedAt;
            if (weight == 0f)
            {
                outputPoses.CopyFrom(basePose.DenseLocalPoses);
                outputVelocities.CopyFrom(basePose.DenseVelocities);
            }
            else
            {
                NativeSlice<AnimationLocalBonePose> basePoses =
                    basePose.DenseLocalPoses;
                NativeSlice<AnimationLocalBonePose> additivePoses =
                    additivePose.DenseLocalPoses;
                NativeSlice<AnimationBlendBoneVelocity> baseVelocities =
                    basePose.DenseVelocities;
                NativeSlice<AnimationBlendBoneVelocity> additiveVelocities =
                    additivePose.DenseVelocities;
                for (int bone = 0; bone < outputPoses.Length; bone++)
                {
                    ref readonly AnimationLocalBonePose baseBone = ref basePoses[bone];
                    ref readonly AnimationLocalBonePose additiveBone = ref additivePoses[bone];
                    ref readonly (Vector3 Position, Quaternion InverseRotation, Vector3 Scale)
                        reference = ref m_ReferenceBones[bone];
                    Vector3 position = baseBone.Position +
                        (additiveBone.Position - reference.Position) * weight;
                    Quaternion referenceToAdditive =
                        reference.InverseRotation * additiveBone.Rotation;
                    Quaternion rotation = baseBone.Rotation *
                        Quaternion.Slerp(Quaternion.identity, referenceToAdditive, weight);
                    Vector3 scale;
                    if (scalePolicy == AdditiveScalePolicy.Multiply)
                    {
                        Vector3 ratio = new Vector3(
                            additiveBone.Scale.x / reference.Scale.x,
                            additiveBone.Scale.y / reference.Scale.y,
                            additiveBone.Scale.z / reference.Scale.z);
                        scale = Vector3.Scale(
                            baseBone.Scale,
                            Vector3.Lerp(Vector3.one, ratio, weight));
                    }
                    else
                    {
                        scale = baseBone.Scale +
                            (additiveBone.Scale - reference.Scale) * weight;
                    }
                    outputPoses[bone] = new AnimationLocalBonePose(position, rotation, scale);
                    ref readonly AnimationBlendBoneVelocity additiveVelocity =
                        ref additiveVelocities[bone];
                    ref readonly AnimationBlendBoneVelocity baseVelocity =
                        ref baseVelocities[bone];
                    outputVelocities[bone] = new AnimationBlendBoneVelocity(
                        baseVelocity.Linear + additiveVelocity.Linear * weight,
                        baseVelocity.Angular + additiveVelocity.Angular * weight,
                        baseVelocity.Scale + additiveVelocity.Scale * weight);
                }
            }
            outputAvailability[0] = AnimationPoseAvailability.Pose;
            outputContinuity[0] = m_ContinuityIdentity;
            outputDiscontinuity[0] = basePose.Discontinuity[0];
            outputInvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            outputCompletedAt[0] = m_WriteBinding.CompletionIdentity;
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
                    $"Additive Pose '{NodeId}' has no open frame.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeAdditivePoseHandler));
        }
    }
}
