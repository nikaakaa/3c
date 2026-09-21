using System;
using System.Collections.Generic;
using BTSMTL.EventGraphs;
using FlowCanvas;
using Unity.Collections;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class CharacterPoseNativeLayeredBoneBlendHandler :
        ICharacterPoseNativeNodeHandler
    {
        readonly PoseNodeId m_NodeId;
        readonly CharacterAnimationRigPayload m_Rig;
        readonly float[] m_BoneMask;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
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

        internal CharacterPoseNativeLayeredBoneBlendHandler(
            PoseNodeId nodeId,
            CharacterAnimationRigPayload rig,
            IReadOnlyList<float> boneMask,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            if (!nodeId.IsValid)
                throw new ArgumentException(
                    "Pose native Layered Bone Blend handler identity is invalid.",
                    nameof(nodeId));
            m_Rig = rig ?? throw new ArgumentNullException(nameof(rig));
            if (boneMask == null || boneMask.Count != m_Rig.PoseBoneCount)
                throw new ArgumentException(
                    "Pose native Layered Bone Blend mask does not match the Rig.",
                    nameof(boneMask));
            m_BoneMask = new float[boneMask.Count];
            for (int i = 0; i < m_BoneMask.Length; i++)
            {
                float value = boneMask[i];
                if (!float.IsFinite(value) || value < 0f || value > 1f)
                    throw new ArgumentException(
                        "Pose native Layered Bone Blend mask contains an invalid weight.",
                        nameof(boneMask));
                m_BoneMask[i] = value;
            }
            m_NodeId = nodeId;
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.LayeredBoneBlend;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind || !node.BoneMaskSlot)
                throw new InvalidOperationException(
                    $"Layered Bone Blend handler '{NodeId}' has no formal Bone Mask resource.");
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
                    $"Layered Bone Blend '{NodeId}' frame is already open.");
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
                    $"Layered Bone Blend '{NodeId}' has no output '{portId}'.");
            if (m_Output != null &&
                m_Output.CompletionIdentity == runtime.CurrentLineage.CompletionIdentity)
                return m_Output;
            RequireFrame();
            CharacterPoseNativeLocalPoseValue basePose =
                runtime.ReadInput<CharacterPoseNativeLocalPoseValue>(node, "base");
            CharacterPoseNativeLocalPoseValue overlayPose =
                runtime.ReadInput<CharacterPoseNativeLocalPoseValue>(node, "overlay");
            CharacterPoseNativePoseReadBinding baseBinding =
                RequireAvailable(basePose, "Base");
            CharacterPoseNativePoseReadBinding overlayBinding =
                RequireAvailable(overlayPose, "Overlay");
            float weight = ResolveWeight(runtime, node);
            m_ContinuityIdentity = ResolveContinuity(
                baseBinding.ContinuityIdentity[0],
                overlayBinding.ContinuityIdentity[0],
                weight);
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                runtime.CurrentLineage.CompletionIdentity);
            CharacterPoseNativePoseBufferCopy.CopyMetadata(
                in baseBinding,
                in m_WriteBinding);
            BlendPose(
                in baseBinding,
                in overlayBinding,
                weight);
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
                    $"Layered Bone Blend '{NodeId}' produced an invalid pending Pose.");
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
                    $"Layered Bone Blend '{NodeId}' {branch} input is unavailable.");
            }
            return binding;
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
                    $"Layered Bone Blend '{node.NodeId}' weight input must be a Float32 in [0, 1].");
            }
            return value.Value.Float32Value;
        }

        ulong ResolveContinuity(
            ulong baseContinuity,
            ulong overlayContinuity,
            float weight)
        {
            if (m_ContinuityIdentity != 0 &&
                m_LastBaseContinuity == baseContinuity &&
                m_LastOverlayContinuity == overlayContinuity &&
                m_LastWeight == weight)
            {
                return m_ContinuityIdentity;
            }
            if (m_NextContinuityIdentity == ulong.MaxValue)
                throw new InvalidOperationException(
                    $"Layered Bone Blend '{NodeId}' continuity identity was exhausted.");
            m_LastBaseContinuity = baseContinuity;
            m_LastOverlayContinuity = overlayContinuity;
            m_LastWeight = weight;
            return m_ContinuityIdentity = m_NextContinuityIdentity++;
        }

        void BlendPose(
            in CharacterPoseNativePoseReadBinding basePose,
            in CharacterPoseNativePoseReadBinding overlayPose,
            float weight)
        {
            NativeSlice<AnimationLocalBonePose> outputPoses =
                m_WriteBinding.DenseLocalPoses;
            NativeSlice<AnimationBlendBoneVelocity> outputVelocities =
                m_WriteBinding.DenseVelocities;
            NativeSlice<float> outputParameters = m_WriteBinding.PoseParameters;
            NativeSlice<byte> outputParameterAvailability =
                m_WriteBinding.PoseParameterAvailability;
            NativeSlice<int> outputContributionCount =
                m_WriteBinding.ContributionCount;
            NativeSlice<float> outputWeight = m_WriteBinding.OutputWeight;
            NativeSlice<AnimationPoseAvailability> outputAvailability =
                m_WriteBinding.Availability;
            NativeSlice<ulong> outputContinuity = m_WriteBinding.ContinuityIdentity;
            NativeSlice<PoseDiscontinuityNative> outputDiscontinuity =
                m_WriteBinding.Discontinuity;
            NativeSlice<AnimationPoseNativeInvalidReason> outputInvalidReason =
                m_WriteBinding.InvalidReason;
            NativeSlice<ulong> outputCompletedAt = m_WriteBinding.CompletedAt;
            float baseGlobalWeight = (1f - weight) * basePose.OutputWeight[0];
            float overlayGlobalWeight = weight * overlayPose.OutputWeight[0];
            for (int bone = 0; bone < outputPoses.Length; bone++)
            {
                float overlayAlpha = weight * m_BoneMask[bone];
                float baseWeight = (1f - overlayAlpha) * basePose.OutputWeight[0];
                float overlayWeight = overlayAlpha * overlayPose.OutputWeight[0];
                float total = baseWeight + overlayWeight;
                if (!float.IsFinite(total) || total <= 0f)
                    throw new InvalidOperationException(
                        $"Layered Bone Blend '{NodeId}' has no visible bone output weight.");
                AnimationLocalBonePose baseBone = basePose.DenseLocalPoses[bone];
                AnimationLocalBonePose overlayBone = overlayPose.DenseLocalPoses[bone];
                Vector3 position =
                    (baseBone.Position * baseWeight +
                     overlayBone.Position * overlayWeight) / total;
                Vector3 scale =
                    (baseBone.Scale * baseWeight +
                     overlayBone.Scale * overlayWeight) / total;
                Vector4 rotation =
                    new Vector4(
                        baseBone.Rotation.x * baseWeight,
                        baseBone.Rotation.y * baseWeight,
                        baseBone.Rotation.z * baseWeight,
                        baseBone.Rotation.w * baseWeight) +
                    AnimationPoseMath.AlignAndScale(
                        overlayBone.Rotation,
                        baseBone.Rotation,
                        overlayWeight);
                outputPoses[bone] = AnimationPoseMath.BlendWeighted(
                    position * total,
                    rotation,
                    scale * total,
                    total,
                    baseBone);
                AnimationBlendBoneVelocity baseVelocity = basePose.DenseVelocities[bone];
                AnimationBlendBoneVelocity overlayVelocity = overlayPose.DenseVelocities[bone];
                outputVelocities[bone] = new AnimationBlendBoneVelocity(
                    (baseVelocity.Linear * baseWeight +
                     overlayVelocity.Linear * overlayWeight) / total,
                    (baseVelocity.Angular * baseWeight +
                     overlayVelocity.Angular * overlayWeight) / total,
                    (baseVelocity.Scale * baseWeight +
                     overlayVelocity.Scale * overlayWeight) / total);
            }
            for (int parameter = 0; parameter < outputParameters.Length; parameter++)
            {
                byte baseAvailable = basePose.PoseParameterAvailability[parameter];
                byte overlayAvailable = overlayPose.PoseParameterAvailability[parameter];
                if (baseAvailable != 0 && overlayAvailable != 0)
                {
                    outputParameters[parameter] =
                        (basePose.PoseParameters[parameter] * baseGlobalWeight +
                         overlayPose.PoseParameters[parameter] * overlayGlobalWeight) /
                        (baseGlobalWeight + overlayGlobalWeight);
                    outputParameterAvailability[parameter] = 1;
                }
                else if (overlayAvailable != 0)
                {
                    outputParameters[parameter] = overlayPose.PoseParameters[parameter];
                    outputParameterAvailability[parameter] = 1;
                }
                else
                {
                    outputParameters[parameter] = basePose.PoseParameters[parameter];
                    outputParameterAvailability[parameter] = baseAvailable;
                }
            }
            int contributionCount = 0;
            float totalGlobalWeight = baseGlobalWeight + overlayGlobalWeight;
            AppendContributions(
                in basePose,
                totalGlobalWeight <= 0f ? 0f : baseGlobalWeight / totalGlobalWeight,
                false,
                ref contributionCount);
            AppendContributions(
                in overlayPose,
                totalGlobalWeight <= 0f ? 0f : overlayGlobalWeight / totalGlobalWeight,
                true,
                ref contributionCount);
            outputContributionCount[0] = contributionCount;
            outputWeight[0] = Mathf.Clamp01(totalGlobalWeight);
            BlendFeet(in basePose, in overlayPose, baseGlobalWeight, overlayGlobalWeight);
            outputAvailability[0] = AnimationPoseAvailability.Pose;
            outputContinuity[0] = m_ContinuityIdentity;
            outputDiscontinuity[0] = weight >= 0.5f
                ? overlayPose.Discontinuity[0]
                : basePose.Discontinuity[0];
            outputInvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            outputCompletedAt[0] = m_WriteBinding.CompletionIdentity;
        }

        void AppendContributions(
            in CharacterPoseNativePoseReadBinding input,
            float globalFactor,
            bool overlay,
            ref int outputCount)
        {
            if (globalFactor <= 0f)
                return;
            int inputCount = input.ContributionCount[0];
            int boneCount = m_WriteBinding.DenseLocalPoses.Length;
            NativeSlice<AnimationPrimitivePoseContribution> outputContributions =
                m_WriteBinding.Contributions;
            NativeSlice<float> outputContributionWeights =
                m_WriteBinding.DenseContributionWeights;
            for (int contribution = 0; contribution < inputCount; contribution++)
            {
                AnimationPrimitivePoseContribution value = input.Contributions[contribution];
                float weight = value.Weight * globalFactor;
                if (weight <= 0f)
                    continue;
                if (outputCount >= outputContributions.Length)
                    throw new InvalidOperationException(
                        $"Layered Bone Blend '{NodeId}' contribution capacity was exceeded.");
                outputContributions[outputCount] =
                    new AnimationPrimitivePoseContribution(
                        value.PhysicalPlayerIndex,
                        value.PhysicalSourceIndex,
                        value.PhysicalSourceGeneration,
                        value.Kind,
                        value.SourceOwnerIndex,
                        value.ContributionContinuityIdentity,
                        weight,
                        value.LeftFootWeight * globalFactor,
                        value.RightFootWeight * globalFactor);
                for (int bone = 0; bone < boneCount; bone++)
                {
                    float boneFactor = overlay
                        ? m_BoneMask[bone]
                        : 1f - m_BoneMask[bone];
                    outputContributionWeights[outputCount * boneCount + bone] =
                        input.DenseContributionWeights[contribution * boneCount + bone] *
                        globalFactor * boneFactor;
                }
                outputCount++;
            }
        }

        void BlendFeet(
            in CharacterPoseNativePoseReadBinding basePose,
            in CharacterPoseNativePoseReadBinding overlayPose,
            float baseWeight,
            float overlayWeight)
        {
            NativeSlice<AnimationFootFeatureSample> outputLeft =
                m_WriteBinding.LeftFootFeatures;
            NativeSlice<AnimationFootFeatureSample> outputRight =
                m_WriteBinding.RightFootFeatures;
            NativeSlice<byte> outputHasFeet = m_WriteBinding.HasFootFeatures;
            bool hasBase = basePose.HasFootFeatures[0] != 0;
            bool hasOverlay = overlayPose.HasFootFeatures[0] != 0;
            if (hasBase && hasOverlay && baseWeight > 0f && overlayWeight > 0f)
            {
                var left = new AnimationFootFeatureBlendAccumulator();
                var right = new AnimationFootFeatureBlendAccumulator();
                left.Add(basePose.LeftFootFeatures[0], baseWeight);
                left.Add(overlayPose.LeftFootFeatures[0], overlayWeight);
                right.Add(basePose.RightFootFeatures[0], baseWeight);
                right.Add(overlayPose.RightFootFeatures[0], overlayWeight);
                outputLeft[0] = left.Resolve();
                outputRight[0] = right.Resolve();
                outputHasFeet[0] = 1;
                return;
            }
            if (hasOverlay && overlayWeight > 0f)
            {
                outputLeft[0] = overlayPose.LeftFootFeatures[0];
                outputRight[0] = overlayPose.RightFootFeatures[0];
                outputHasFeet[0] = 1;
                return;
            }
            if (hasBase && baseWeight > 0f)
            {
                outputLeft[0] = basePose.LeftFootFeatures[0];
                outputRight[0] = basePose.RightFootFeatures[0];
                outputHasFeet[0] = 1;
                return;
            }
            outputLeft[0] = default;
            outputRight[0] = default;
            outputHasFeet[0] = 0;
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
                    $"Layered Bone Blend '{NodeId}' has no open frame.");
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeLayeredBoneBlendHandler));
        }
    }
}
