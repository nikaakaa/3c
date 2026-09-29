using System;
using System.Collections.Generic;
using Animancer;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Sources;
using FlowCanvas;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal interface ICharacterPoseNativeAnimationSlotSource
        : IDisposable
    {
        AnimationSelectionAvailabilityPolicy Availability { get; }
        IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPresentationPoseSourceSlot sourceSlot,
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

    internal sealed class CharacterPoseNativeAnimationSlotHandler :
        ICharacterPoseNativeNodeHandler
    {
        readonly PoseNodeId m_NodeId;
        readonly AnimationSlotId m_SlotId;
        readonly AnimationSelectionAvailabilityPolicy m_Availability;
        readonly ICharacterPoseNativeAnimationSlotSource m_Source;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        FlowCanvas.ValueInput<CharacterPoseNativeLocalPoseValue> m_SourcePoseInput;
        CharacterPresentationPoseSourceSlot m_SourceSlot;
        CharacterPoseNativeLocalPoseValue m_ActionPose;
        CharacterPoseNativeLocalPoseValue m_Output;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        ulong m_NextContinuityIdentity = 1;
        ulong m_ContinuityIdentity;
        ulong m_LastSourceContinuity;
        ulong m_LastActionContinuity;
        float m_LastActionWeight = float.NaN;
        ulong m_CommittedContinuityIdentity;
        ulong m_CommittedSourceContinuity;
        ulong m_CommittedActionContinuity;
        float m_CommittedActionWeight = float.NaN;
        int m_PageIndex = -1;
        int m_CommittedPageIndex = -1;
        bool m_FrameOpen;
        bool m_Disposed;

        internal CharacterPoseNativeAnimationSlotHandler(
            PoseNodeId nodeId,
            AnimationSlotId slotId,
            AnimationSelectionAvailabilityPolicy availability,
            ICharacterPoseNativeAnimationSlotSource source,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            if (!nodeId.IsValid || !slotId.IsValid)
            {
                throw new ArgumentException(
                    "Pose native Animation Slot handler identity is invalid.");
            }
            m_NodeId = nodeId;
            m_SlotId = slotId;
            m_Availability = availability;
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            if (source.Availability != availability)
                throw new ArgumentException(
                    "Pose native Animation Slot source availability is inconsistent.",
                    nameof(source));
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
        }

        public PoseNodeId NodeId => m_NodeId;
        public CharacterPoseNodeKind Kind => CharacterPoseNodeKind.AnimationSlot;

        public void Initialize(CharacterPoseNativeGraphRuntime runtime)
        {
            RequireAlive();
            CharacterPoseCanvasNode node = runtime.Graph.RequireNode(NodeId);
            if (node.Kind != Kind || node.AnimationSlotId != m_SlotId ||
                node.SelectionAvailability != m_Availability)
            {
                throw new InvalidOperationException(
                    $"Animation Slot handler '{NodeId}' does not match its graph node.");
            }
            m_SourcePoseInput = runtime.RequireInputPort<CharacterPoseNativeLocalPoseValue>(
                node,
                "source-pose");
            m_SourceSlot = node.PresentationPoseSourceSlot;
        }

        public void Start(CharacterPoseNativeGraphRuntime runtime) => RequireAlive();

        public void Reset(
            CharacterPoseNativeGraphRuntime runtime,
            ulong resetGeneration)
        {
            RequireAlive();
            m_Source.ResetFrame();
            m_ContinuityIdentity = 0;
            m_LastSourceContinuity = 0;
            m_LastActionContinuity = 0;
            m_LastActionWeight = float.NaN;
            m_CommittedContinuityIdentity = 0;
            m_CommittedSourceContinuity = 0;
            m_CommittedActionContinuity = 0;
            m_CommittedActionWeight = float.NaN;
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
                    $"Animation Slot '{NodeId}' frame is already open.");
            m_FrameOpen = true;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            m_ContinuityIdentity = m_CommittedContinuityIdentity;
            m_LastSourceContinuity = m_CommittedSourceContinuity;
            m_LastActionContinuity = m_CommittedActionContinuity;
            m_LastActionWeight = m_CommittedActionWeight;
            m_ActionPose = null;
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
            return m_Source.PrepareFrame(runtime, m_SourceSlot, in input, in lineage);
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
                    $"Animation Slot '{NodeId}' has no output '{portId}'.");
            if (m_Output != null &&
                m_Output.CompletionIdentity == runtime.CurrentLineage.CompletionIdentity)
                return m_Output;
            RequireFrame();
            if (m_ActionPose == null)
                throw new InvalidOperationException(
                    $"Animation Slot '{NodeId}' has no evaluated Action Pose.");
            CharacterPoseNativeLocalPoseValue sourcePose =
                runtime.ReadInput(
                    m_SourcePoseInput,
                    m_NodeId,
                    "source-pose");
            m_WriteBinding = (m_PageIndex == 0
                    ? m_OutputBuffer
                    : m_SecondaryOutputBuffer).RequireWriteBinding(
                runtime.CurrentLineage.CompletionIdentity);
            CharacterPoseNativePoseReadBinding source = sourcePose.Native;
            CharacterPoseNativePoseReadBinding action = m_ActionPose.Native;
            if (!source.IsValid || !action.IsValid ||
                source.Space != CharacterPoseSpace.Local ||
                action.Space != CharacterPoseSpace.Local ||
                source.CompletionIdentity != runtime.CurrentLineage.CompletionIdentity ||
                action.CompletionIdentity != runtime.CurrentLineage.CompletionIdentity ||
                source.DenseLocalPoses.Length != m_WriteBinding.DenseLocalPoses.Length ||
                action.DenseLocalPoses.Length != m_WriteBinding.DenseLocalPoses.Length ||
                source.PoseParameters.Length != m_WriteBinding.PoseParameters.Length ||
                action.PoseParameters.Length != m_WriteBinding.PoseParameters.Length ||
                !float.IsFinite(source.OutputWeight[0]) ||
                source.OutputWeight[0] < 0f || source.OutputWeight[0] > 1f ||
                !float.IsFinite(action.OutputWeight[0]) ||
                action.OutputWeight[0] < 0f || action.OutputWeight[0] > 1f)
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{NodeId}' received a non-local native Pose.");
            }
            if (source.Availability[0] == AnimationPoseAvailability.Invalid ||
                action.Availability[0] == AnimationPoseAvailability.Invalid)
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{NodeId}' received an invalid Pose.");
            }
            if (source.Availability[0] == AnimationPoseAvailability.NoPose)
            {
                if (action.Availability[0] == AnimationPoseAvailability.Pose)
                    CopyPose(in action, CharacterPoseSpace.Local);
                else if (m_Availability == AnimationSelectionAvailabilityPolicy.RequireSelection)
                    throw new InvalidOperationException(
                        $"Animation Slot '{NodeId}' requires an Action selection.");
                else
                    WriteNoPose(in source);
                return m_Output;
            }
            if (action.Availability[0] == AnimationPoseAvailability.NoPose)
            {
                if (m_Availability == AnimationSelectionAvailabilityPolicy.RequireSelection)
                    throw new InvalidOperationException(
                        $"Animation Slot '{NodeId}' requires an Action selection.");
                CopyPose(in source, CharacterPoseSpace.Local);
                return m_Output;
            }
            BlendPoses(in source, in action);
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
            if (!demand.IsValid || !lineage.Matches(in demand.Lineage) || barrierIdentity == 0)
                throw new InvalidOperationException(
                    $"Animation Slot '{NodeId}' evaluation preparation identity is invalid.");
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
            m_ActionPose = m_Source.Evaluate(
                runtime,
                in lineage,
                barrierIdentity);
            if (m_ActionPose == null ||
                !m_ActionPose.Native.IsValid ||
                m_ActionPose.Native.CompletionIdentity != lineage.CompletionIdentity)
            {
                throw new InvalidOperationException(
                    $"Animation Slot '{NodeId}' Action source did not produce the current Pose.");
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
                    $"Animation Slot '{NodeId}' pending output is invalid.");
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
            m_CommittedContinuityIdentity = m_ContinuityIdentity;
            m_CommittedSourceContinuity = m_LastSourceContinuity;
            m_CommittedActionContinuity = m_LastActionContinuity;
            m_CommittedActionWeight = m_LastActionWeight;
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
            m_ContinuityIdentity = m_CommittedContinuityIdentity;
            m_LastSourceContinuity = m_CommittedSourceContinuity;
            m_LastActionContinuity = m_CommittedActionContinuity;
            m_LastActionWeight = m_CommittedActionWeight;
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

        void BlendPoses(
            in CharacterPoseNativePoseReadBinding source,
            in CharacterPoseNativePoseReadBinding action)
        {
            float actionGlobalWeight = Mathf.Clamp01(action.OutputWeight[0]);
            float sourceGlobalWeight = Mathf.Clamp01(1f - actionGlobalWeight);
            if (m_LastSourceContinuity != source.ContinuityIdentity[0] ||
                m_LastActionContinuity != action.ContinuityIdentity[0] ||
                m_LastActionWeight != actionGlobalWeight)
            {
                if (m_NextContinuityIdentity == ulong.MaxValue)
                    throw new InvalidOperationException(
                        $"Animation Slot '{NodeId}' continuity identity was exhausted.");
                m_ContinuityIdentity = m_NextContinuityIdentity++;
                m_LastSourceContinuity = source.ContinuityIdentity[0];
                m_LastActionContinuity = action.ContinuityIdentity[0];
                m_LastActionWeight = actionGlobalWeight;
            }
            NativeSlice<AnimationLocalBonePose> outputPoses =
                m_WriteBinding.DenseLocalPoses;
            NativeSlice<AnimationBlendBoneVelocity> outputVelocities =
                m_WriteBinding.DenseVelocities;
            NativeSlice<AnimationLocalBonePose> sourcePoses =
                source.DenseLocalPoses;
            NativeSlice<AnimationLocalBonePose> actionPoses =
                action.DenseLocalPoses;
            NativeSlice<AnimationBlendBoneVelocity> sourceVelocities =
                source.DenseVelocities;
            NativeSlice<AnimationBlendBoneVelocity> actionVelocities =
                action.DenseVelocities;
            for (int bone = 0; bone < outputPoses.Length; bone++)
            {
                float actionBoneWeight = BoneOutputWeight(in action, bone);
                float sourceBoneWeight = 1f - actionBoneWeight;
                ref readonly AnimationLocalBonePose sourcePose =
                    ref sourcePoses[bone];
                ref readonly AnimationLocalBonePose actionPose =
                    ref actionPoses[bone];
                float total = sourceBoneWeight + actionBoneWeight;
                if (!sourcePose.IsValid || !actionPose.IsValid || total <= 0f)
                    throw new InvalidOperationException(
                        $"Animation Slot '{NodeId}' Bone #{bone} Pose is invalid.");
                Vector3 position =
                    (sourcePose.Position * sourceBoneWeight +
                     actionPose.Position * actionBoneWeight) / total;
                Vector3 scale =
                    (sourcePose.Scale * sourceBoneWeight +
                     actionPose.Scale * actionBoneWeight) / total;
                Vector4 rotation =
                    new Vector4(
                        sourcePose.Rotation.x * sourceBoneWeight,
                        sourcePose.Rotation.y * sourceBoneWeight,
                        sourcePose.Rotation.z * sourceBoneWeight,
                        sourcePose.Rotation.w * sourceBoneWeight) +
                    AnimationPoseMath.AlignAndScale(
                        actionPose.Rotation,
                        sourcePose.Rotation,
                        actionBoneWeight);
                outputPoses[bone] = AnimationPoseMath.BlendWeighted(
                    position * total,
                    rotation,
                    scale * total,
                    total,
                    sourcePose);
                ref readonly AnimationBlendBoneVelocity sourceVelocity =
                    ref sourceVelocities[bone];
                ref readonly AnimationBlendBoneVelocity actionVelocity =
                    ref actionVelocities[bone];
                outputVelocities[bone] = new AnimationBlendBoneVelocity(
                    Vector3.LerpUnclamped(
                        sourceVelocity.Linear,
                        actionVelocity.Linear,
                        actionBoneWeight),
                    Vector3.LerpUnclamped(
                        sourceVelocity.Angular,
                        actionVelocity.Angular,
                        actionBoneWeight),
                    Vector3.LerpUnclamped(
                        sourceVelocity.Scale,
                        actionVelocity.Scale,
                        actionBoneWeight));
            }
            BlendParameters(in source, in action, sourceGlobalWeight, actionGlobalWeight);
            int count = 0;
            AppendContributions(in source, sourceGlobalWeight, ref count);
            AppendContributions(in action, actionGlobalWeight, ref count);
            CharacterPoseNativePoseBufferCopy.CompleteContributions(in m_WriteBinding, count);
            m_WriteBinding.OutputWeight[0] = Mathf.Clamp01(
                source.OutputWeight[0] + action.OutputWeight[0]);
            BlendFeet(in source, in action, sourceGlobalWeight, actionGlobalWeight);
            m_WriteBinding.Availability[0] = AnimationPoseAvailability.Pose;
            m_WriteBinding.ContinuityIdentity[0] = m_ContinuityIdentity;
            m_WriteBinding.Discontinuity[0] = actionGlobalWeight >= 0.5f
                ? action.Discontinuity[0]
                : source.Discontinuity[0];
            m_WriteBinding.InvalidReason[0] = AnimationPoseNativeInvalidReason.None;
            m_WriteBinding.CompletedAt[0] = m_WriteBinding.CompletionIdentity;
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(in m_WriteBinding);
            m_Output = CharacterPoseNativeLocalPoseValue.Reuse(
                m_Output,
                NodeId,
                in output);
        }

        void CopyPose(
            in CharacterPoseNativePoseReadBinding input,
            CharacterPoseSpace space)
        {
            CharacterPoseNativePoseBufferCopy.CopyMetadata(
                in input,
                in m_WriteBinding);
            NativeSlice<AnimationLocalBonePose> poses =
                m_WriteBinding.DenseLocalPoses;
            poses.CopyFrom(input.DenseLocalPoses);
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(in m_WriteBinding, space);
            m_ContinuityIdentity = input.ContinuityIdentity[0];
            m_LastSourceContinuity = input.ContinuityIdentity[0];
            m_LastActionContinuity = 0;
            m_LastActionWeight = 0f;
            m_Output = CharacterPoseNativeLocalPoseValue.Reuse(
                m_Output,
                NodeId,
                in output);
        }

        void WriteNoPose(in CharacterPoseNativePoseReadBinding input)
        {
            CharacterPoseNativePoseBufferCopy.CopyMetadata(
                in input,
                in m_WriteBinding);
            NativeSlice<AnimationLocalBonePose> poses =
                m_WriteBinding.DenseLocalPoses;
            poses.CopyFrom(input.DenseLocalPoses);
            m_WriteBinding.Availability[0] = AnimationPoseAvailability.NoPose;
            m_WriteBinding.OutputWeight[0] = 0f;
            m_WriteBinding.ContinuityIdentity[0] = input.ContinuityIdentity[0];
            m_WriteBinding.CompletedAt[0] = m_WriteBinding.CompletionIdentity;
            CharacterPoseNativePoseReadBinding output =
                new CharacterPoseNativePoseReadBinding(in m_WriteBinding);
            m_Output = CharacterPoseNativeLocalPoseValue.Reuse(
                m_Output,
                NodeId,
                in output);
        }

        void BlendParameters(
            in CharacterPoseNativePoseReadBinding source,
            in CharacterPoseNativePoseReadBinding action,
            float sourceWeight,
            float actionWeight)
        {
            NativeSlice<float> parameters = m_WriteBinding.PoseParameters;
            NativeSlice<byte> availability = m_WriteBinding.PoseParameterAvailability;
            for (int parameter = 0; parameter < parameters.Length; parameter++)
            {
                byte sourceAvailable = source.PoseParameterAvailability[parameter];
                byte actionAvailable = action.PoseParameterAvailability[parameter];
                if (sourceAvailable != 0 && actionAvailable != 0)
                {
                    parameters[parameter] =
                        (source.PoseParameters[parameter] * sourceWeight +
                         action.PoseParameters[parameter] * actionWeight) /
                        (sourceWeight + actionWeight);
                    availability[parameter] = 1;
                }
                else if (actionAvailable != 0)
                {
                    parameters[parameter] = action.PoseParameters[parameter];
                    availability[parameter] = 1;
                }
                else
                {
                    parameters[parameter] = source.PoseParameters[parameter];
                    availability[parameter] = sourceAvailable;
                }
            }
        }

        void AppendContributions(
            in CharacterPoseNativePoseReadBinding input,
            float factor,
            ref int outputCount)
        {
            int inputCount = input.ContributionCount[0];
            int boneCount = m_WriteBinding.DenseLocalPoses.Length;
            NativeSlice<AnimationPrimitivePoseContribution> output =
                m_WriteBinding.Contributions;
            NativeSlice<float> outputWeights =
                m_WriteBinding.DenseContributionWeights;
            for (int contribution = 0; contribution < inputCount; contribution++)
            {
                if (outputCount >= output.Length)
                    throw new InvalidOperationException(
                        $"Animation Slot '{NodeId}' contribution capacity was exceeded.");
                AnimationPrimitivePoseContribution value =
                    input.Contributions[contribution];
                CharacterPoseNativePoseBufferCopy.ExtendContributionPrefix(in m_WriteBinding, outputCount + 1);
                output[outputCount] = new AnimationPrimitivePoseContribution(
                    value.PhysicalPlayerIndex,
                    value.PhysicalSourceIndex,
                    value.PhysicalSourceGeneration,
                    value.Kind,
                    value.SourceOwnerIndex,
                    value.ContributionContinuityIdentity,
                    value.Weight * factor,
                    value.LeftFootWeight * factor,
                    value.RightFootWeight * factor);
                for (int bone = 0; bone < boneCount; bone++)
                    outputWeights[outputCount * boneCount + bone] =
                        input.DenseContributionWeights[contribution * boneCount + bone] * factor;
                outputCount++;
            }
        }

        void BlendFeet(
            in CharacterPoseNativePoseReadBinding source,
            in CharacterPoseNativePoseReadBinding action,
            float sourceWeight,
            float actionWeight)
        {
            bool hasSource = source.HasFootFeatures[0] != 0;
            bool hasAction = action.HasFootFeatures[0] != 0;
            if (hasSource && hasAction && sourceWeight > 0f && actionWeight > 0f)
            {
                var left = new AnimationFootFeatureBlendAccumulator();
                var right = new AnimationFootFeatureBlendAccumulator();
                left.Add(source.LeftFootFeatures[0], sourceWeight);
                left.Add(action.LeftFootFeatures[0], actionWeight);
                right.Add(source.RightFootFeatures[0], sourceWeight);
                right.Add(action.RightFootFeatures[0], actionWeight);
                m_WriteBinding.LeftFootFeatures[0] = left.Resolve();
                m_WriteBinding.RightFootFeatures[0] = right.Resolve();
                m_WriteBinding.HasFootFeatures[0] = 1;
                return;
            }
            if (hasAction && actionWeight > 0f)
            {
                m_WriteBinding.LeftFootFeatures[0] = action.LeftFootFeatures[0];
                m_WriteBinding.RightFootFeatures[0] = action.RightFootFeatures[0];
                m_WriteBinding.HasFootFeatures[0] = 1;
                return;
            }
            if (hasSource && sourceWeight > 0f)
            {
                m_WriteBinding.LeftFootFeatures[0] = source.LeftFootFeatures[0];
                m_WriteBinding.RightFootFeatures[0] = source.RightFootFeatures[0];
                m_WriteBinding.HasFootFeatures[0] = 1;
                return;
            }
            m_WriteBinding.LeftFootFeatures[0] = default;
            m_WriteBinding.RightFootFeatures[0] = default;
            m_WriteBinding.HasFootFeatures[0] = 0;
        }

        float BoneOutputWeight(
            in CharacterPoseNativePoseReadBinding input,
            int bone)
        {
            float weight = 0f;
            int count = input.ContributionCount[0];
            for (int contribution = 0; contribution < count; contribution++)
                weight += input.DenseContributionWeights[
                    contribution * input.DenseLocalPoses.Length + bone];
            return Mathf.Clamp01(weight);
        }

        void RequireFrame()
        {
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Animation Slot '{NodeId}' frame is not open.");
        }

        void ClearFrame()
        {
            m_FrameOpen = false;
            m_ActionPose = null;
            m_WriteBinding = default;
            m_PageIndex = -1;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeAnimationSlotHandler));
        }
    }

    internal sealed class CharacterPoseNativeAnimationSlotSourceBinding :
        ICharacterPoseNativeAnimationSlotSource, ICharacterPoseSourceRetirementOwner
    {
        readonly AnimationBlendStackRuntime m_Stack;
        readonly CharacterPoseNativeActionSlotSource m_ActionSource;
        readonly CharacterPoseSourceModule m_Source;
        readonly ICharacterPoseNativeBlendStackSourceBinding m_SourceBinding;
        readonly CharacterPoseNativeNodePoseBuffer m_OutputBuffer;
        readonly CharacterPoseNativeNodePoseBuffer m_SecondaryOutputBuffer;
        readonly AnimationBlendStackSourceReleaseToken[] m_StackReleases;
        readonly CharacterPoseSourceRetirementHandle[] m_Retirements;
        int m_RetirementCount;
        ulong m_CompletionIdentity;
        CharacterPoseNativeLocalPoseValue m_Output;
        AnimationScriptPlayable m_Playable;
        AnimationSlotBlendJob m_Job;
        AnimationPlayerPoseNativeWriteBinding m_WriteBinding;
        int m_PageIndex = -1;
        int m_CommittedPageIndex = -1;
        bool m_FrameOpen;
        bool m_EvaluationPrepared;
        bool m_Disposed;

        internal CharacterPoseNativeAnimationSlotSourceBinding(
            AnimationBlendStackRuntime stack,
            CharacterPoseNativeActionSlotSource actionSource,
            CharacterPoseSourceModule source,
            ICharacterPoseNativeBlendStackSourceBinding sourceBinding,
            CharacterPoseNativeNodePoseBuffer outputBuffer)
        {
            m_Stack = stack ?? throw new ArgumentNullException(nameof(stack));
            m_ActionSource = actionSource ??
                throw new ArgumentNullException(nameof(actionSource));
            m_Source = source ?? throw new ArgumentNullException(nameof(source));
            m_SourceBinding = sourceBinding ??
                throw new ArgumentNullException(nameof(sourceBinding));
            m_OutputBuffer = outputBuffer ??
                throw new ArgumentNullException(nameof(outputBuffer));
            m_SecondaryOutputBuffer = m_OutputBuffer.CreateSibling();
            m_StackReleases = new AnimationBlendStackSourceReleaseToken[stack.SourceCapacity + 1];
            m_Retirements = new CharacterPoseSourceRetirementHandle[stack.SourceCapacity + 1];
            m_Source.RegisterRetirementOwner(this);
        }

        public AnimationSelectionAvailabilityPolicy Availability =>
            m_Stack.OutputPolicy;

        public void PrepareRetirements()
        {
            if (!m_EvaluationPrepared)
                return;
            int count = m_Stack.PendingPriorFrameReleaseCount(m_CompletionIdentity);
            for (int i = 0; i < count; i++)
            {
                AnimationBlendStackSourceReleaseToken token =
                    m_Stack.PrepareRelease(i, m_CompletionIdentity);
                m_StackReleases[i] = token;
                var permission = new CharacterPoseSourceRetirementPermission(
                    token.Release.SourceId, token.Release.PoseNodeId, default);
                m_Retirements[i] = m_Source.PrepareRetirement(in permission);
                m_RetirementCount++;
            }
        }

        public void CommitRetirements()
        {
            for (int i = 0; i < m_RetirementCount; i++)
            {
                m_Source.ApplyRetirement(in m_Retirements[i]);
                m_Stack.ApplyPreparedRelease(in m_StackReleases[i]);
            }
            DiscardRetirements();
        }

        public void DiscardRetirements()
        {
            Array.Clear(m_StackReleases, 0, m_RetirementCount);
            Array.Clear(m_Retirements, 0, m_RetirementCount);
            m_RetirementCount = 0;
        }

        public IReadOnlyList<CharacterPoseNativeSourceRequest> PrepareFrame(
            CharacterPoseNativeGraphRuntime runtime,
            CharacterPresentationPoseSourceSlot sourceSlot,
            in CharacterPoseNativeFrameInput input,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            if (m_FrameOpen)
                throw new InvalidOperationException(
                    $"Animation Slot source '{m_Stack.PoseNodeId}' frame is already open.");
            m_Stack.BeginFrame();
            m_FrameOpen = true;
            m_EvaluationPrepared = false;
            m_PageIndex = m_CommittedPageIndex < 0
                ? 0
                : 1 - m_CommittedPageIndex;
            try
            {
                m_Stack.Advance(input.DeltaSeconds);
                m_ActionSource.BeginFrame(
                    in input,
                    in lineage,
                    m_Stack);
                m_Stack.BeginSourceFrame(lineage.CompletionIdentity);
                return m_SourceBinding.PrepareFrame(
                    runtime,
                    m_Stack,
                    sourceSlot,
                    in input,
                    in lineage);
            }
            catch
            {
                m_Stack.DiscardFrame();
                m_ActionSource.DiscardFrame();
                m_SourceBinding.ResetFrame();
                m_FrameOpen = false;
                throw;
            }
        }

        public void PrepareEvaluation(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeSourceDemand demand,
            in CharacterPoseNativeFrameLineage lineage,
            ulong barrierIdentity)
        {
            RequireAlive();
            if (!m_FrameOpen || !demand.IsValid ||
                !lineage.Matches(in demand.Lineage) || barrierIdentity == 0)
            {
                throw new InvalidOperationException(
                    $"Animation Slot source '{m_Stack.PoseNodeId}' evaluation preparation is invalid.");
            }
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
            m_CompletionIdentity = lineage.CompletionIdentity;
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

        public CharacterPoseNativeLocalPoseValue Evaluate(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage,
            ulong barrierIdentity)
        {
            RequireAlive();
            if (!m_FrameOpen || !m_EvaluationPrepared ||
                m_WriteBinding.CompletionIdentity != lineage.CompletionIdentity ||
                barrierIdentity == 0)
            {
                throw new InvalidOperationException(
                    $"Animation Slot source '{m_Stack.PoseNodeId}' evaluation is not prepared.");
            }
            m_Stack.CompleteFrame(lineage.CompletionIdentity);
            if (m_WriteBinding.CompletedAt[0] != lineage.CompletionIdentity)
                throw new InvalidOperationException(
                    $"Animation Slot source '{m_Stack.PoseNodeId}' did not complete the current frame.");
            if (m_WriteBinding.Availability[0] == AnimationPoseAvailability.Invalid)
                throw new InvalidOperationException(
                    $"Animation Slot source '{m_Stack.PoseNodeId}' is invalid: {m_WriteBinding.InvalidReason[0]}.");
            m_Output = CharacterPoseNativeLocalPoseValue.Reuse(
                m_Output,
                m_Stack.PoseNodeId,
                new CharacterPoseNativePoseReadBinding(in m_WriteBinding));
            return m_Output;
        }

        public void CommitFrame(
            CharacterPoseNativeGraphRuntime runtime,
            in CharacterPoseNativeFrameLineage lineage)
        {
            RequireAlive();
            if (!m_FrameOpen)
                throw new InvalidOperationException(
                    $"Animation Slot source '{m_Stack.PoseNodeId}' frame is not open.");
            m_Stack.CommitFrame();
            m_ActionSource.CommitFrame();
            m_CommittedPageIndex = m_PageIndex;
            m_SourceBinding.ResetFrame();
            m_FrameOpen = false;
            m_EvaluationPrepared = false;
            m_WriteBinding = default;
            m_PageIndex = -1;
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
            m_ActionSource.DiscardFrame();
            m_SourceBinding.ResetFrame();
            m_FrameOpen = false;
            m_EvaluationPrepared = false;
            m_WriteBinding = default;
            m_PageIndex = -1;
        }

        public void ResetFrame()
        {
            if (m_Disposed)
                return;
            if (m_FrameOpen)
                m_Stack.DiscardFrame();
            if (m_FrameOpen)
                m_ActionSource.DiscardFrame();
            m_ActionSource.Reset();
            m_SourceBinding.ResetFrame();
            m_FrameOpen = false;
            m_EvaluationPrepared = false;
            m_WriteBinding = default;
            m_PageIndex = -1;
            m_CommittedPageIndex = -1;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            if (m_FrameOpen)
                m_Stack.DiscardFrame();
            if (m_FrameOpen)
                m_ActionSource.DiscardFrame();
            if (m_Playable.IsValid())
                AnimancerUtilities.RemovePlayable(m_Playable);
            m_ActionSource.Dispose();
            m_Source.UnregisterRetirementOwner(this);
            m_Stack.Dispose();
            m_OutputBuffer.Dispose();
            m_SecondaryOutputBuffer.Dispose();
            m_SourceBinding.ResetFrame();
            m_FrameOpen = false;
            m_EvaluationPrepared = false;
            m_PageIndex = -1;
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(
                    nameof(CharacterPoseNativeAnimationSlotSourceBinding));
        }
    }
}
