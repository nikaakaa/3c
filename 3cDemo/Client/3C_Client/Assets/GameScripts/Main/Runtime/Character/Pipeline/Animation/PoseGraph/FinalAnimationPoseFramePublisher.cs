using System;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using Unity.Collections.LowLevel.Unsafe;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class ComposedAnimationPoseFramePublisher
    {
        readonly string m_PoseGraphId;
        readonly string m_PosePlanHash;
        readonly PoseNodeId[] m_PoseNodeIds;
        readonly float[] m_ParameterDefaults;
        readonly AnimationLocalBonePose[] m_DenseLocalPoses;
        readonly float[] m_PoseParameters;
        readonly byte[] m_PoseParameterAvailability;
        readonly AnimationPoseSourceContribution[] m_Contributions;
        readonly float[] m_DenseContributionWeights;
        readonly CharacterPoseBoneKind[] m_BoneKinds;
        readonly FinalAnimationPoseFramePageLease[] m_PageLeases;
        readonly int m_OperationCount;
        readonly int m_BoneCount;
        readonly int m_ParameterCount;
        readonly int m_ContributionCapacity;
        readonly int m_PhysicalBoneCount;
        readonly int m_VirtualBoneCount;
        readonly long m_DenseDoublePageResidentPayloadBytes;
        int m_CommittedPage = -1;
        ulong m_LastCommittedCompletionIdentity;
        int m_PendingPage = -1;
        ulong m_PendingCompletionIdentity;
        ComposedAnimationPoseFrame m_PendingFrame;
        ComposedAnimationPoseFrame m_CommittedFrame;

        internal ComposedAnimationPoseFramePublisher(
            CharacterPresentationPosePlan program,
            CharacterAnimationRigPayload rig)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (rig == null)
                throw new ArgumentNullException(nameof(rig));
            program.RequireValid();
            rig.RequireValid();
            if (!string.Equals(program.RigId, rig.RigId, StringComparison.Ordinal) ||
                !string.Equals(program.RigRevision, rig.RigRevision, StringComparison.Ordinal) ||
                program.PoseBoneCount != rig.PoseBoneCount)
            {
                throw new InvalidOperationException("Final Animation Pose Frame Rig layout is invalid.");
            }
            if (program.Parameters.Count <= 0 ||
                program.ContributionWorkspaceCount % program.PoseValueWorkspaceCount != 0)
            {
                throw new InvalidOperationException("Final Animation Pose Frame publisher layout is invalid.");
            }

            int contributionCapacity = program.ContributionWorkspaceCount / program.PoseValueWorkspaceCount;
            if (contributionCapacity <= 0)
                throw new InvalidOperationException("Final Animation Pose Frame contribution capacity is invalid.");
            m_PoseGraphId = program.PoseGraphId;
            m_PosePlanHash = program.PlanHash;
            m_OperationCount = program.Operations.Count;
            m_PoseNodeIds = new PoseNodeId[program.PlayerCount];
            for (int i = 0; i < program.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = program.Operations[i];
                if (operation.Code != CharacterPoseOperationCode.SelectedPosePlayer &&
                    operation.Code != CharacterPoseOperationCode.BlendStack &&
                    operation.Code != CharacterPoseOperationCode.BlendSpacePlayer &&
                    operation.Code != CharacterPoseOperationCode.SequencePlayer &&
                    operation.Code != CharacterPoseOperationCode.AnimationSlot)
                    continue;
                if (operation.PlayerIndex < 0 || operation.PlayerIndex >= m_PoseNodeIds.Length ||
                    m_PoseNodeIds[operation.PlayerIndex].IsValid)
                    throw new InvalidOperationException($"Composed Animation Pose Player #{operation.PlayerIndex} is invalid.");
                m_PoseNodeIds[operation.PlayerIndex] = operation.NodeId;
            }

            m_ParameterDefaults = new float[program.Parameters.Count];
            for (int i = 0; i < m_ParameterDefaults.Length; i++)
            {
                CharacterPresentationPoseParameterEntry parameter = program.Parameters[i];
                if (parameter == null || parameter.Index != i || !float.IsFinite(parameter.DefaultValue))
                    throw new InvalidOperationException($"Final Animation Pose Frame Parameter #{i} is invalid.");
                m_ParameterDefaults[i] = parameter.DefaultValue;
            }

            m_BoneCount = program.PoseBoneCount;
            m_PhysicalBoneCount = rig.PhysicalBoneCount;
            m_VirtualBoneCount = rig.VirtualBoneCount;
            m_BoneKinds = new CharacterPoseBoneKind[m_BoneCount];
            for (int i = 0; i < m_BoneKinds.Length; i++)
                m_BoneKinds[i] = rig.GetPoseBoneKind(i);
            m_ParameterCount = program.Parameters.Count;
            m_ContributionCapacity = contributionCapacity;
            m_DenseLocalPoses = new AnimationLocalBonePose[checked(2 * m_BoneCount)];
            m_PoseParameters = new float[checked(2 * m_ParameterCount)];
            m_PoseParameterAvailability = new byte[checked(2 * m_ParameterCount)];
            m_Contributions = new AnimationPoseSourceContribution[checked(2 * m_ContributionCapacity)];
            m_DenseContributionWeights = new float[checked(2 * m_ContributionCapacity * m_BoneCount)];
            m_DenseDoublePageResidentPayloadBytes = checked(
                PayloadBytes(m_DenseLocalPoses) +
                PayloadBytes(m_PoseParameters) +
                PayloadBytes(m_PoseParameterAvailability) +
                PayloadBytes(m_DenseContributionWeights));
            m_PageLeases = new[]
            {
                new FinalAnimationPoseFramePageLease(),
                new FinalAnimationPoseFramePageLease()
            };
        }

        internal long DenseDoublePageResidentPayloadBytes =>
            m_DenseDoublePageResidentPayloadBytes;

        internal void PreparePending(
            in AnimationFinalPoseNativeReadBinding binding,
            PhysicalPoseSourceRegistry sourceRegistry)
        {
            if (sourceRegistry == null)
                throw new ArgumentNullException(nameof(sourceRegistry));
            if (m_PendingPage >= 0)
            {
                throw new InvalidOperationException(
                    "Final Animation Pose Frame already has a pending frame.");
            }
            RequireBinding(in binding);
            ulong completionIdentity = binding.CompletionIdentity;
            if (completionIdentity <= m_LastCommittedCompletionIdentity)
                throw new InvalidOperationException("Final Animation Pose Frame completion identity did not advance.");
            if (binding.PoseGraphCompletedAt[0] != completionIdentity)
                throw new InvalidOperationException("Animation Pose Graph did not complete the requested frame.");

            AnimationPoseAvailability availability = binding.Availability[0];
            if (!IsAvailability(availability) ||
                availability == AnimationPoseAvailability.NoPose || binding.ContinuityIdentity[0] == 0)
            {
                throw new InvalidOperationException("Final Animation Pose Graph output header is invalid.");
            }

            int page = (m_CommittedPage + 1) & 1;
            FinalAnimationPoseFramePageLease pageLease = m_PageLeases[page];
            pageLease.BeginWrite(completionIdentity);
            ComposedAnimationPoseFrame frame = availability == AnimationPoseAvailability.Invalid
                ? PublishInvalid(in binding, page, pageLease)
                : PublishPose(in binding, sourceRegistry, page, pageLease);
            m_PendingPage = page;
            m_PendingCompletionIdentity =
                completionIdentity;
            m_PendingFrame = frame;
        }

        internal int ResolveContributions(
            in AnimationPoseValueNativeReadBinding binding,
            PhysicalPoseSourceRegistry sourceRegistry,
            AnimationPoseSourceContribution[] destination)
        {
            if (sourceRegistry == null)
                throw new ArgumentNullException(nameof(sourceRegistry));
            int count = binding.ContributionCount[0];
            if (binding.CompletionIdentity == 0 ||
                binding.Contributions.Length != m_ContributionCapacity ||
                count <= 0 || count > m_ContributionCapacity ||
                destination == null || destination.Length < count)
            {
                throw new ArgumentException(
                    $"Pose Value contribution resolution input is invalid: completion={binding.CompletionIdentity}, " +
                    $"availability={(binding.Availability.Length == 0 ? default : binding.Availability[0])}, " +
                    $"reason={(binding.InvalidReason.Length == 0 ? default : binding.InvalidReason[0])}, " +
                    $"operation={(binding.PoseGraphInvalidOperationIndex.Length == 0 ? -1 : binding.PoseGraphInvalidOperationIndex[0])}, " +
                    $"contributionLength={binding.Contributions.Length}, count={count}, " +
                    $"capacity={m_ContributionCapacity}, destinationLength={destination?.Length ?? -1}.");
            }
            for (int i = 0; i < count; i++)
            {
                destination[i] = ExpandContribution(
                    binding.Contributions[i],
                    sourceRegistry);
            }
            return count;
        }

        internal ComposedAnimationPoseFrame
            CommitPending()
        {
            if (m_PendingPage < 0 ||
                m_PendingCompletionIdentity == 0)
            {
                throw new InvalidOperationException(
                    "Final Animation Pose Frame has no pending frame.");
            }
            m_CommittedPage = m_PendingPage;
            m_LastCommittedCompletionIdentity =
                m_PendingCompletionIdentity;
            ComposedAnimationPoseFrame result =
                m_PendingFrame;
            m_CommittedFrame = result;
            m_PendingPage = -1;
            m_PendingCompletionIdentity = 0;
            m_PendingFrame = default;
            return result;
        }

        internal void DiscardPending()
        {
            if (m_PendingPage < 0)
                return;
            m_PageLeases[m_PendingPage].Invalidate();
            m_PendingPage = -1;
            m_PendingCompletionIdentity = 0;
            m_PendingFrame = default;
        }

        internal void Invalidate()
        {
            for (int i = 0; i < m_PageLeases.Length; i++)
                m_PageLeases[i].Invalidate();
            m_CommittedPage = -1;
            m_LastCommittedCompletionIdentity = 0;
            m_PendingPage = -1;
            m_PendingCompletionIdentity = 0;
            m_PendingFrame = default;
            m_CommittedFrame = default;
        }

        ComposedAnimationPoseFrame PublishPose(
            in AnimationFinalPoseNativeReadBinding binding,
            PhysicalPoseSourceRegistry sourceRegistry,
            int page,
            FinalAnimationPoseFramePageLease pageLease)
        {
            if (binding.OutputInvalidReason[0] != AnimationPoseNativeInvalidReason.None ||
                binding.PoseGraphInvalidReason[0] != AnimationPoseNativeInvalidReason.None ||
                binding.PoseGraphInvalidOperationIndex[0] != -1 ||
                binding.AppliedAt[0] != binding.CompletionIdentity ||
                binding.WriteOutcome[0] != AnimationFinalPoseWriteOutcome.Committed ||
                !float.IsFinite(binding.OutputWeight[0]) ||
                binding.OutputWeight[0] < 0f || binding.OutputWeight[0] > 1f)
            {
                throw new InvalidOperationException(
                    $"Final Animation Pose Graph completed Pose metadata is invalid: " +
                    $"OutputReason={binding.OutputInvalidReason[0]}, " +
                    $"GraphReason={binding.PoseGraphInvalidReason[0]}, " +
                    $"InvalidOperation={binding.PoseGraphInvalidOperationIndex[0]}, " +
                    $"AppliedAt={binding.AppliedAt[0]}, " +
                    $"Completion={binding.CompletionIdentity}, " +
                    $"OutputWeight={binding.OutputWeight[0]:R}.");
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
                byte parameterAvailable = binding.PoseParameterAvailability[parameter];
                if (!float.IsFinite(value) || parameterAvailable > 1)
                    throw new InvalidOperationException($"Final Animation Pose Graph Parameter #{parameter} is invalid.");
                m_PoseParameters[parameterOffset + parameter] = value;
                m_PoseParameterAvailability[parameterOffset + parameter] = parameterAvailable;
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

            return new ComposedAnimationPoseFrame(
                m_PoseGraphId,
                m_PosePlanHash,
                binding.CompletionIdentity,
                AnimationPoseAvailability.Pose,
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(
                    m_DenseLocalPoses, poseOffset, m_BoneCount, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_PoseParameters, parameterOffset, m_ParameterCount, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<byte>(
                    m_PoseParameterAvailability, parameterOffset, m_ParameterCount, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(
                    m_Contributions, contributionOffset, contributionCount, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_DenseContributionWeights,
                    denseWeightOffset,
                    checked(contributionCount * m_BoneCount),
                    pageLease,
                    binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<CharacterPoseBoneKind>(
                    m_BoneKinds, 0, m_BoneKinds.Length, pageLease, binding.CompletionIdentity),
                m_PhysicalBoneCount,
                m_VirtualBoneCount,
                m_BoneCount,
                left,
                right,
                hasFootFeatures == 1,
                binding.ContinuityIdentity[0],
                pageLease,
                binding.CompletionIdentity);
        }

        ComposedAnimationPoseFrame PublishInvalid(
            in AnimationFinalPoseNativeReadBinding binding,
            int page,
            FinalAnimationPoseFramePageLease pageLease)
        {
            AnimationPoseNativeInvalidReason outputReason = binding.OutputInvalidReason[0];
            AnimationPoseNativeInvalidReason graphReason = binding.PoseGraphInvalidReason[0];
            int invalidOperationIndex = binding.PoseGraphInvalidOperationIndex[0];
            bool finalPhysicalInvalid = outputReason == AnimationPoseNativeInvalidReason.FinalPhysicalWriteInvalid &&
                                      graphReason == AnimationPoseNativeInvalidReason.None &&
                                      invalidOperationIndex == -1 && binding.AppliedAt[0] == 0;
            bool graphInvalid = outputReason != AnimationPoseNativeInvalidReason.None &&
                                graphReason != AnimationPoseNativeInvalidReason.None &&
                                invalidOperationIndex >= 0 && invalidOperationIndex < m_OperationCount &&
                                binding.AppliedAt[0] == 0 && binding.ContributionCount[0] == 0;
            AnimationFinalPoseWriteOutcome outcome = binding.WriteOutcome[0];
            if (!IsInvalidReason(outputReason) ||
                !IsInvalidReason(graphReason) ||
                (outputReason == AnimationPoseNativeInvalidReason.None && graphReason == AnimationPoseNativeInvalidReason.None) ||
                !finalPhysicalInvalid && !graphInvalid ||
                outcome != AnimationFinalPoseWriteOutcome.TypedInvalid)
            {
                throw new InvalidOperationException(
                    $"Final Animation Pose Graph Invalid completion metadata is inconsistent. " +
                    $"Availability={binding.Availability[0]}, OutputReason={outputReason}, " +
                    $"GraphReason={graphReason}, InvalidOperation={invalidOperationIndex}, " +
                    $"ContributionCount={binding.ContributionCount[0]}, AppliedAt={binding.AppliedAt[0]}, " +
                    $"Completion={binding.CompletionIdentity}, GraphCompletedAt={binding.PoseGraphCompletedAt[0]}, " +
                    $"Continuity={binding.ContinuityIdentity[0]}, WriteOutcome={outcome}.");
            }
            int poseOffset = checked(page * m_BoneCount);
            int parameterOffset = checked(page * m_ParameterCount);
            int contributionOffset = checked(page * m_ContributionCapacity);
            int denseWeightOffset = checked(contributionOffset * m_BoneCount);
            Array.Copy(m_ParameterDefaults, 0, m_PoseParameters, parameterOffset, m_ParameterCount);
            Array.Clear(m_PoseParameterAvailability, parameterOffset, m_ParameterCount);
            return new ComposedAnimationPoseFrame(
                m_PoseGraphId,
                m_PosePlanHash,
                binding.CompletionIdentity,
                AnimationPoseAvailability.Invalid,
                new AnimationReadOnlyBuffer<AnimationLocalBonePose>(
                    m_DenseLocalPoses, poseOffset, 0, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_PoseParameters, parameterOffset, m_ParameterCount, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<byte>(
                    m_PoseParameterAvailability, parameterOffset, m_ParameterCount, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<AnimationPoseSourceContribution>(
                    m_Contributions, contributionOffset, 0, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<float>(
                    m_DenseContributionWeights, denseWeightOffset, 0, pageLease, binding.CompletionIdentity),
                new AnimationReadOnlyBuffer<CharacterPoseBoneKind>(
                    m_BoneKinds, 0, m_BoneKinds.Length, pageLease, binding.CompletionIdentity),
                m_PhysicalBoneCount,
                m_VirtualBoneCount,
                m_BoneCount,
                default,
                default,
                false,
                binding.ContinuityIdentity[0],
                pageLease,
                binding.CompletionIdentity);
        }

        AnimationPoseSourceContribution ExpandContribution(
            AnimationPrimitivePoseContribution primitive,
            PhysicalPoseSourceRegistry sourceRegistry)
        {
            if (primitive.PhysicalPlayerIndex < 0 || primitive.PhysicalPlayerIndex >= m_PoseNodeIds.Length ||
                !IsContributionKind(primitive.Kind) ||
                primitive.ContributionContinuityIdentity == 0 ||
                !IsWeight(primitive.Weight) || !IsWeight(primitive.LeftFootWeight) ||
                !IsWeight(primitive.RightFootWeight))
            {
                throw new InvalidOperationException("Final Animation Pose Graph primitive contribution is invalid.");
            }

            PoseNodeId playerNodeId = m_PoseNodeIds[primitive.PhysicalPlayerIndex];
            AnimationPoseSourceId sourceId = default;
            if (primitive.Kind == AnimationPoseContributionKind.Live)
            {
                if (primitive.PhysicalSourceIndex < 0 || primitive.PhysicalSourceGeneration == 0 ||
                    primitive.SourceOwnerIndex < 0)
                    throw new InvalidOperationException("Final Animation Pose Graph Live contribution identity is invalid.");
                var physicalIdentity = new AnimationPhysicalSourceIdentity(
                    new AnimationPhysicalSourceIndex(primitive.PhysicalSourceIndex),
                    primitive.PhysicalSourceGeneration);
                sourceId = sourceRegistry.RequireSourceId(physicalIdentity);
                if (!sourceRegistry.RequirePoseNodeId(physicalIdentity).Equals(playerNodeId) ||
                    sourceRegistry.RequireSourceOwnerIndex(physicalIdentity) != primitive.SourceOwnerIndex)
                {
                    throw new InvalidOperationException("Final Animation Pose Graph Live contribution metadata does not match its physical identity.");
                }
            }
            else if (primitive.PhysicalSourceIndex != -1 || primitive.PhysicalSourceGeneration != 0 ||
                     primitive.SourceOwnerIndex != -1)
            {
                throw new InvalidOperationException("Final Animation Pose Graph captured contribution must not carry a Live source identity.");
            }

            return new AnimationPoseSourceContribution(
                playerNodeId,
                primitive.Kind,
                sourceId,
                primitive.SourceOwnerIndex,
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
                !IsUnit(binding.PoseGraphCompletedAt) || !IsUnit(binding.AppliedAt) ||
                !IsUnit(binding.WriteOutcome))
            {
                throw new ArgumentException("Final Animation Pose Native read binding does not match its publisher.");
            }
        }

        static bool IsWeight(float value) => float.IsFinite(value) && value >= 0f && value <= 1f;
        static bool IsAvailability(AnimationPoseAvailability value) =>
            (int)value >= (int)AnimationPoseAvailability.Pose &&
            (int)value <= (int)AnimationPoseAvailability.Invalid;
        static bool IsInvalidReason(AnimationPoseNativeInvalidReason value) =>
            AnimationPoseNativeInvalidReasonContract.IsDefined(value);
        static bool IsContributionKind(AnimationPoseContributionKind value) =>
            (int)value >= (int)AnimationPoseContributionKind.Live &&
            (int)value <= (int)AnimationPoseContributionKind.Stored;
        static bool IsUnit<T>(Unity.Collections.NativeSlice<T> values) where T : struct =>
            values.Length == 1;
        static long PayloadBytes<T>(T[] values) where T : unmanaged =>
            checked((long)UnsafeUtility.SizeOf<T>() * values.LongLength);
    }
}
