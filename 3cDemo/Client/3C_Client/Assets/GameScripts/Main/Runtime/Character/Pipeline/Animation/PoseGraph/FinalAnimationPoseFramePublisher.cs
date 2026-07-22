using System;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class FinalAnimationPoseFramePublisher
    {
        readonly string m_PoseGraphId;
        readonly string m_PoseProgramHash;
        readonly PoseSlotId[] m_PoseSlotIds;
        readonly float[] m_ParameterDefaults;
        readonly AnimationLocalBonePose[] m_DenseLocalPoses;
        readonly float[] m_PoseParameters;
        readonly AnimationPoseSourceContribution[] m_Contributions;
        readonly float[] m_DenseContributionWeights;
        readonly FinalAnimationPoseFramePageLease[] m_PageLeases;
        readonly int m_OperationCount;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_ContributionCapacity;
        int m_PublishedPage = -1;
        ulong m_LastPublishedCompletionIdentity;

        internal FinalAnimationPoseFramePublisher(CharacterPresentationPoseProgram program)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            program.RequireValid();
            if (program.Parameters.Count <= 0 ||
                program.ContributionWorkspaceCount % program.PoseValueWorkspaceCount != 0)
            {
                throw new InvalidOperationException("Final Animation Pose Frame publisher layout is invalid.");
            }

            int contributionCapacity = program.ContributionWorkspaceCount / program.PoseValueWorkspaceCount;
            if (contributionCapacity <= 0)
                throw new InvalidOperationException("Final Animation Pose Frame contribution capacity is invalid.");
            m_PoseGraphId = program.PoseGraphId;
            m_PoseProgramHash = program.ProgramHash;
            m_OperationCount = program.Operations.Count;
            m_PoseSlotIds = new PoseSlotId[program.Slots.Count];
            for (int i = 0; i < m_PoseSlotIds.Length; i++)
            {
                CharacterPresentationPoseSlotProgramEntry slot = program.Slots[i];
                if (slot == null || slot.Index != i || !slot.PoseSlotId.IsValid)
                    throw new InvalidOperationException($"Final Animation Pose Frame Slot #{i} is invalid.");
                m_PoseSlotIds[i] = slot.PoseSlotId;
            }

            m_ParameterDefaults = new float[program.Parameters.Count];
            for (int i = 0; i < m_ParameterDefaults.Length; i++)
            {
                CharacterPresentationPoseParameterProgramEntry parameter = program.Parameters[i];
                if (parameter == null || parameter.Index != i || !float.IsFinite(parameter.DefaultValue))
                    throw new InvalidOperationException($"Final Animation Pose Frame Parameter #{i} is invalid.");
                m_ParameterDefaults[i] = parameter.DefaultValue;
            }

            m_BoneCount = program.BoneCount;
            m_ParameterCount = program.Parameters.Count;
            m_ContributionCapacity = contributionCapacity;
            m_DenseLocalPoses = new AnimationLocalBonePose[checked(2 * m_BoneCount)];
            m_PoseParameters = new float[checked(2 * m_ParameterCount)];
            m_Contributions = new AnimationPoseSourceContribution[checked(2 * m_ContributionCapacity)];
            m_DenseContributionWeights = new float[checked(2 * m_ContributionCapacity * m_BoneCount)];
            m_PageLeases = new[]
            {
                new FinalAnimationPoseFramePageLease(),
                new FinalAnimationPoseFramePageLease()
            };
        }

        internal FinalAnimationPoseFrame Publish(
            in AnimationFinalPoseNativeReadBinding binding,
            AnimationPoseSourcePhysicalRegistry sourceRegistry)
        {
            if (sourceRegistry == null)
                throw new ArgumentNullException(nameof(sourceRegistry));
            RequireBinding(in binding);
            ulong completionIdentity = binding.CompletionIdentity;
            if (completionIdentity <= m_LastPublishedCompletionIdentity)
                throw new InvalidOperationException("Final Animation Pose Frame completion identity did not advance.");
            if (binding.PoseGraphCompletedAt[0] != completionIdentity)
                throw new InvalidOperationException("Animation Pose Graph did not complete the requested frame.");

            PoseSlotFrameAvailability availability = binding.Availability[0];
            if (!IsAvailability(availability) ||
                availability == PoseSlotFrameAvailability.NoPose || binding.ContinuityIdentity[0] == 0)
            {
                throw new InvalidOperationException("Final Animation Pose Graph output header is invalid.");
            }

            int page = (m_PublishedPage + 1) & 1;
            FinalAnimationPoseFramePageLease pageLease = m_PageLeases[page];
            pageLease.BeginWrite(completionIdentity);
            FinalAnimationPoseFrame frame = availability == PoseSlotFrameAvailability.Invalid
                ? PublishInvalid(in binding, page, pageLease)
                : PublishPose(in binding, sourceRegistry, page, pageLease);
            m_PublishedPage = page;
            m_LastPublishedCompletionIdentity = completionIdentity;
            return frame;
        }

        internal void Invalidate()
        {
            for (int i = 0; i < m_PageLeases.Length; i++)
                m_PageLeases[i].Invalidate();
            m_PublishedPage = -1;
        }

        FinalAnimationPoseFrame PublishPose(
            in AnimationFinalPoseNativeReadBinding binding,
            AnimationPoseSourcePhysicalRegistry sourceRegistry,
            int page,
            FinalAnimationPoseFramePageLease pageLease)
        {
            if (binding.OutputInvalidReason[0] != AnimationPoseNativeInvalidReason.None ||
                binding.PoseGraphInvalidReason[0] != AnimationPoseNativeInvalidReason.None ||
                binding.PoseGraphInvalidOperationIndex[0] != -1 ||
                binding.AppliedAt[0] != binding.CompletionIdentity ||
                !float.IsFinite(binding.OutputWeight[0]) ||
                binding.OutputWeight[0] < 0f || binding.OutputWeight[0] > 1f)
            {
                throw new InvalidOperationException("Final Animation Pose Graph completed Pose metadata is invalid.");
            }

            int poseOffset = checked(page * m_BoneCount);
            int parameterOffset = checked(page * m_ParameterCount);
            int contributionOffset = checked(page * m_ContributionCapacity);
            int denseWeightOffset = checked(contributionOffset * m_BoneCount);
            for (int bone = 0; bone < m_BoneCount; bone++)
            {
                AnimationLocalBonePose pose = binding.DenseLocalPoses[bone];
                if (!pose.IsValid)
                    throw new InvalidOperationException($"Final Animation Pose Graph Bone #{bone} is invalid.");
                m_DenseLocalPoses[poseOffset + bone] = pose;
            }
            for (int parameter = 0; parameter < m_ParameterCount; parameter++)
            {
                float value = binding.PoseParameters[parameter];
                if (!float.IsFinite(value))
                    throw new InvalidOperationException($"Final Animation Pose Graph Parameter #{parameter} is invalid.");
                m_PoseParameters[parameterOffset + parameter] = value;
            }

            int contributionCount = binding.ContributionCount[0];
            if (contributionCount <= 0 || contributionCount > m_ContributionCapacity)
                throw new InvalidOperationException("Final Animation Pose Graph contribution count is invalid.");
            for (int contribution = 0; contribution < contributionCount; contribution++)
            {
                AnimationPrimitivePoseContribution primitive = binding.Contributions[contribution];
                m_Contributions[contributionOffset + contribution] = ExpandContribution(primitive, sourceRegistry);
                for (int bone = 0; bone < m_BoneCount; bone++)
                {
                    float weight = binding.DenseContributionWeights[
                        contribution * m_BoneCount + bone];
                    if (!float.IsFinite(weight) || weight < 0f || weight > 1f)
                    {
                        throw new InvalidOperationException(
                            $"Final Animation Pose Graph contribution #{contribution} Bone #{bone} weight is invalid.");
                    }
                    m_DenseContributionWeights[denseWeightOffset + contribution * m_BoneCount + bone] = weight;
                }
            }

            byte hasFootFeatures = binding.HasFootFeatures[0];
            if (hasFootFeatures > 1)
                throw new InvalidOperationException("Final Animation Pose Graph Foot Feature state is invalid.");
            AnimationFootFeatureSample left = hasFootFeatures == 1 ? binding.LeftFootFeatures[0] : default;
            AnimationFootFeatureSample right = hasFootFeatures == 1 ? binding.RightFootFeatures[0] : default;
            if (hasFootFeatures == 1 && (!left.IsValid || !right.IsValid))
                throw new InvalidOperationException("Final Animation Pose Graph Foot Features are invalid.");

            return new FinalAnimationPoseFrame(
                m_PoseGraphId,
                m_PoseProgramHash,
                binding.CompletionIdentity,
                PoseSlotFrameAvailability.Pose,
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(
                    m_DenseLocalPoses, poseOffset, m_BoneCount, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_PoseParameters, parameterOffset, m_ParameterCount, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(
                    m_Contributions, contributionOffset, contributionCount, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_DenseContributionWeights,
                    denseWeightOffset,
                    checked(contributionCount * m_BoneCount),
                    pageLease,
                    binding.CompletionIdentity),
                left,
                right,
                hasFootFeatures == 1,
                binding.ContinuityIdentity[0],
                pageLease,
                binding.CompletionIdentity);
        }

        FinalAnimationPoseFrame PublishInvalid(
            in AnimationFinalPoseNativeReadBinding binding,
            int page,
            FinalAnimationPoseFramePageLease pageLease)
        {
            AnimationPoseNativeInvalidReason outputReason = binding.OutputInvalidReason[0];
            AnimationPoseNativeInvalidReason graphReason = binding.PoseGraphInvalidReason[0];
            int invalidOperationIndex = binding.PoseGraphInvalidOperationIndex[0];
            bool finalStreamInvalid = outputReason == AnimationPoseNativeInvalidReason.FinalStreamWriteInvalid &&
                                      graphReason == AnimationPoseNativeInvalidReason.None &&
                                      invalidOperationIndex == -1 && binding.AppliedAt[0] == 0;
            bool graphInvalid = outputReason != AnimationPoseNativeInvalidReason.None &&
                                graphReason != AnimationPoseNativeInvalidReason.None &&
                                invalidOperationIndex >= 0 && invalidOperationIndex < m_OperationCount &&
                                binding.AppliedAt[0] == 0 && binding.ContributionCount[0] == 0;
            if (!IsInvalidReason(outputReason) ||
                !IsInvalidReason(graphReason) ||
                (outputReason == AnimationPoseNativeInvalidReason.None && graphReason == AnimationPoseNativeInvalidReason.None) ||
                !finalStreamInvalid && !graphInvalid)
            {
                throw new InvalidOperationException("Final Animation Pose Graph Invalid completion metadata is inconsistent.");
            }
            int poseOffset = checked(page * m_BoneCount);
            int parameterOffset = checked(page * m_ParameterCount);
            int contributionOffset = checked(page * m_ContributionCapacity);
            int denseWeightOffset = checked(contributionOffset * m_BoneCount);
            Array.Copy(m_ParameterDefaults, 0, m_PoseParameters, parameterOffset, m_ParameterCount);
            return new FinalAnimationPoseFrame(
                m_PoseGraphId,
                m_PoseProgramHash,
                binding.CompletionIdentity,
                PoseSlotFrameAvailability.Invalid,
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(
                    m_DenseLocalPoses, poseOffset, 0, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_PoseParameters, parameterOffset, m_ParameterCount, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(
                    m_Contributions, contributionOffset, 0, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_DenseContributionWeights, denseWeightOffset, 0, pageLease, binding.CompletionIdentity),
                default,
                default,
                false,
                binding.ContinuityIdentity[0],
                pageLease,
                binding.CompletionIdentity);
        }

        AnimationPoseSourceContribution ExpandContribution(
            AnimationPrimitivePoseContribution primitive,
            AnimationPoseSourcePhysicalRegistry sourceRegistry)
        {
            if (primitive.PhysicalSlotIndex < 0 || primitive.PhysicalSlotIndex >= m_PoseSlotIds.Length ||
                !IsContributionKind(primitive.Kind) ||
                primitive.ContributionContinuityIdentity == 0 ||
                !IsWeight(primitive.Weight) || !IsWeight(primitive.LeftFootWeight) ||
                !IsWeight(primitive.RightFootWeight))
            {
                throw new InvalidOperationException("Final Animation Pose Graph primitive contribution is invalid.");
            }

            PoseSlotId poseSlotId = m_PoseSlotIds[primitive.PhysicalSlotIndex];
            AnimationPoseSourceId sourceId = default;
            if (primitive.Kind == AnimationPoseContributionKind.Live)
            {
                if (primitive.PhysicalSourceIndex < 0 || primitive.PhysicalSourceGeneration == 0 ||
                    primitive.ProgramProducerIndex < 0)
                    throw new InvalidOperationException("Final Animation Pose Graph Live contribution identity is invalid.");
                var physicalIdentity = new AnimationPhysicalSourceIdentity(
                    new AnimationPhysicalSourceIndex(primitive.PhysicalSourceIndex),
                    primitive.PhysicalSourceGeneration);
                sourceId = sourceRegistry.RequireSourceId(physicalIdentity);
                if (!sourceRegistry.RequirePoseSlotId(physicalIdentity).Equals(poseSlotId) ||
                    sourceRegistry.RequireProgramProducerIndex(physicalIdentity) != primitive.ProgramProducerIndex)
                {
                    throw new InvalidOperationException("Final Animation Pose Graph Live contribution metadata does not match its physical identity.");
                }
            }
            else if (primitive.PhysicalSourceIndex != -1 || primitive.PhysicalSourceGeneration != 0 ||
                     primitive.ProgramProducerIndex != -1)
            {
                throw new InvalidOperationException("Final Animation Pose Graph captured contribution must not carry a Live source identity.");
            }

            return new AnimationPoseSourceContribution(
                poseSlotId,
                primitive.Kind,
                sourceId,
                primitive.ProgramProducerIndex,
                primitive.ContributionContinuityIdentity,
                primitive.Weight,
                primitive.LeftFootWeight,
                primitive.RightFootWeight);
        }

        void RequireBinding(in AnimationFinalPoseNativeReadBinding binding)
        {
            if (binding.CompletionIdentity == 0 ||
                binding.DenseLocalPoses.Length != m_BoneCount ||
                binding.PoseParameters.Length != m_ParameterCount ||
                binding.Contributions.Length != m_ContributionCapacity ||
                binding.DenseContributionWeights.Length != checked(m_ContributionCapacity * m_BoneCount) ||
                !IsUnit(binding.ContributionCount) || !IsUnit(binding.OutputWeight) ||
                !IsUnit(binding.LeftFootFeatures) || !IsUnit(binding.RightFootFeatures) ||
                !IsUnit(binding.HasFootFeatures) || !IsUnit(binding.Availability) ||
                !IsUnit(binding.ContinuityIdentity) || !IsUnit(binding.OutputInvalidReason) ||
                !IsUnit(binding.PoseGraphInvalidReason) || !IsUnit(binding.PoseGraphInvalidOperationIndex) ||
                !IsUnit(binding.PoseGraphCompletedAt) || !IsUnit(binding.AppliedAt))
            {
                throw new ArgumentException("Final Animation Pose Native read binding does not match its publisher.");
            }
        }

        static bool IsWeight(float value) => float.IsFinite(value) && value >= 0f && value <= 1f;
        static bool IsAvailability(PoseSlotFrameAvailability value) =>
            (int)value >= (int)PoseSlotFrameAvailability.Pose &&
            (int)value <= (int)PoseSlotFrameAvailability.Invalid;
        static bool IsInvalidReason(AnimationPoseNativeInvalidReason value) =>
            (int)value >= (int)AnimationPoseNativeInvalidReason.None &&
            (int)value <= (int)AnimationPoseNativeInvalidReason.FinalStreamWriteInvalid;
        static bool IsContributionKind(AnimationPoseContributionKind value) =>
            (int)value >= (int)AnimationPoseContributionKind.Live &&
            (int)value <= (int)AnimationPoseContributionKind.Inertial;
        static bool IsUnit<T>(Unity.Collections.NativeSlice<T> values) where T : struct =>
            values.Length == 1;
    }
}
