using System;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ThirdPersonCharacter.Pipeline.Animation
{
    internal sealed class AnimationPoseNativeWorkspace : IDisposable
    {
        sealed class Page
        {
            internal NativeArray<AnimationLocalBonePose>
                SlotDenseLocalPoses;
            internal NativeArray<AnimationBlendBoneVelocity>
                SlotDenseVelocities;
            internal NativeArray<float> SlotPoseParameters;
            internal NativeArray<byte> SlotPoseParameterAvailability;
            internal NativeArray<AnimationPrimitivePoseContribution>
                SlotContributions;
            internal NativeArray<float> SlotDenseContributionWeights;
            internal NativeArray<int> SlotContributionCounts;
            internal NativeArray<float> SlotOutputWeights;
            internal NativeArray<AnimationFootFeatureSample>
                SlotLeftFootFeatures;
            internal NativeArray<AnimationFootFeatureSample>
                SlotRightFootFeatures;
            internal NativeArray<byte> SlotHasFootFeatures;
            internal NativeArray<AnimationPoseAvailability>
                SlotAvailability;
            internal NativeArray<ulong> SlotContinuityIdentities;
            internal NativeArray<PoseDiscontinuityNative> SlotDiscontinuities;
            internal NativeArray<AnimationPoseNativeInvalidReason>
                SlotInvalidReasons;
            internal NativeArray<ulong> SlotCompletedAt;
            internal NativeArray<AnimationLocalBonePose>
                ValueDenseLocalPoses;
            internal NativeArray<AnimationBlendBoneVelocity>
                ValueDenseVelocities;
            internal NativeArray<float> ValuePoseParameters;
            internal NativeArray<byte> ValuePoseParameterAvailability;
            internal NativeArray<AnimationPrimitivePoseContribution>
                ValueContributions;
            internal NativeArray<float> ValueDenseContributionWeights;
            internal NativeArray<int> ValueContributionCounts;
            internal NativeArray<float> ValueOutputWeights;
            internal NativeArray<AnimationFootFeatureSample>
                ValueLeftFootFeatures;
            internal NativeArray<AnimationFootFeatureSample>
                ValueRightFootFeatures;
            internal NativeArray<byte> ValueHasFootFeatures;
            internal NativeArray<AnimationPoseAvailability>
                ValueAvailability;
            internal NativeArray<ulong> ValueContinuityIdentities;
            internal NativeArray<PoseDiscontinuityNative> ValueDiscontinuities;
            internal NativeArray<AnimationPoseNativeInvalidReason>
                ValueInvalidReasons;
            internal NativeArray<ulong> FrameCacheCompletedAt;
            internal NativeArray<ulong> StageCompletedAt;
            internal NativeArray<int> StageInvalidOperationIndex;
            internal NativeArray<AnimationPoseNativeInvalidReason>
                PoseGraphInvalidReason;
            internal NativeArray<int> PoseGraphInvalidOperationIndex;
            internal NativeArray<ulong> PoseGraphCompletedAt;
            internal NativeArray<ulong> FinalAppliedAt;
            internal NativeArray<AnimationFinalPoseWriteOutcome>
                FinalWriteOutcome;
        }

        AnimationPoseNativeAggregateLayout m_Layout;
        NativeArray<AnimationPlayerPoseNativeRange> m_SlotRanges;
        NativeArray<AnimationLocalBonePose> m_SlotDenseLocalPoses;
        NativeArray<AnimationBlendBoneVelocity> m_SlotDenseVelocities;
        NativeArray<float> m_SlotPoseParameters;
        NativeArray<byte> m_SlotPoseParameterAvailability;
        NativeArray<AnimationPrimitivePoseContribution> m_SlotContributions;
        NativeArray<float> m_SlotDenseContributionWeights;
        NativeArray<int> m_SlotContributionCounts;
        NativeArray<float> m_SlotOutputWeights;
        NativeArray<AnimationFootFeatureSample> m_SlotLeftFootFeatures;
        NativeArray<AnimationFootFeatureSample> m_SlotRightFootFeatures;
        NativeArray<byte> m_SlotHasFootFeatures;
        NativeArray<AnimationPoseAvailability> m_SlotAvailability;
        NativeArray<ulong> m_SlotContinuityIdentities;
        NativeArray<PoseDiscontinuityNative> m_SlotDiscontinuities;
        NativeArray<AnimationPoseNativeInvalidReason> m_SlotInvalidReasons;
        NativeArray<ulong> m_SlotCompletedAt;
        NativeArray<AnimationLocalBonePose> m_ValueDenseLocalPoses;
        NativeArray<AnimationBlendBoneVelocity> m_ValueDenseVelocities;
        NativeArray<float> m_ValuePoseParameters;
        NativeArray<byte> m_ValuePoseParameterAvailability;
        NativeArray<AnimationPrimitivePoseContribution> m_ValueContributions;
        NativeArray<float> m_ValueDenseContributionWeights;
        NativeArray<int> m_ValueContributionCounts;
        NativeArray<float> m_ValueOutputWeights;
        NativeArray<AnimationFootFeatureSample> m_ValueLeftFootFeatures;
        NativeArray<AnimationFootFeatureSample> m_ValueRightFootFeatures;
        NativeArray<byte> m_ValueHasFootFeatures;
        NativeArray<AnimationPoseAvailability> m_ValueAvailability;
        NativeArray<ulong> m_ValueContinuityIdentities;
        NativeArray<PoseDiscontinuityNative> m_ValueDiscontinuities;
        NativeArray<AnimationPoseNativeInvalidReason> m_ValueInvalidReasons;
        NativeArray<ulong> m_FrameCacheCompletedAt;
        NativeArray<ulong> m_StageCompletedAt;
        NativeArray<int> m_StageInvalidOperationIndex;
        NativeArray<AnimationPoseNativeInvalidReason> m_PoseGraphInvalidReason;
        NativeArray<int> m_PoseGraphInvalidOperationIndex;
        NativeArray<ulong> m_PoseGraphCompletedAt;
        NativeArray<ulong> m_FinalAppliedAt;
        NativeArray<AnimationFinalPoseWriteOutcome> m_FinalWriteOutcome;
        Page m_CommittedPage;
        Page m_PendingPage;
        PoseNodeId[] m_PoseNodeIds;
        CharacterPoseGraphNativeBinding m_FrameBinding;
        CharacterPoseGraphNativeBinding m_CommittedBinding;
        ulong m_LastCompletionIdentity;
        ulong m_CurrentCompletionIdentity;
        readonly long m_DenseDoublePageResidentPayloadBytes;
        bool m_Disposed;

        internal AnimationPoseNativeWorkspace(
            CharacterPresentationProjection projection)
        {
            try
            {
                if (projection == null)
                    throw new ArgumentNullException(nameof(projection));
                projection.RequirePosePayload();
                CharacterPresentationPosePlan program = projection.PosePlan;
                program.RequireValid();
                int playerCount = program.PlayerCount;
                int boneCount = program.PoseBoneCount;
                int parameterCount = program.Parameters.Count;
                int poseValueCount = program.PoseValueWorkspaceCount;
                if (playerCount <= 0 || boneCount <= 0 || parameterCount <= 0 || poseValueCount <= 0 ||
                    program.ContributionWorkspaceCount % poseValueCount != 0)
                {
                    throw new InvalidOperationException("Animation Pose Native workspace source layout is invalid.");
                }

                int poseValueContributionStride = program.ContributionWorkspaceCount / poseValueCount;
                if (poseValueContributionStride <= 0)
                    throw new InvalidOperationException("Animation Pose Native contribution stride is invalid.");
                CharacterPresentationPoseOperation outputOperation = program.Operations[program.OutputOperationIndex];
                if (outputOperation == null || outputOperation.Index != program.OutputOperationIndex ||
                    outputOperation.Code != CharacterPoseOperationCode.OutputPose ||
                    outputOperation.OutputValueIndex < 0 || outputOperation.OutputValueIndex >= poseValueCount)
                {
                    throw new InvalidOperationException("Animation Pose Native output operation is invalid.");
                }

                m_PoseNodeIds = new PoseNodeId[playerCount];
                int totalPlayerContributionCapacity = 0;
                CharacterPresentationPoseOperation[] playerOperations = program.Operations
                    .Where(operation => operation.Code == CharacterPoseOperationCode.SelectedPosePlayer ||
                                        operation.Code == CharacterPoseOperationCode.BlendStack ||
                                        operation.Code == CharacterPoseOperationCode.BlendSpacePlayer ||
                                        operation.Code == CharacterPoseOperationCode.SequencePlayer ||
                                        operation.Code == CharacterPoseOperationCode.AnimationSlot)
                    .OrderBy(operation => operation.PlayerIndex)
                    .ToArray();
                for (int i = 0; i < playerCount; i++)
                {
                    CharacterPresentationPoseOperation player = playerOperations[i];
                    if (player == null || player.PlayerIndex != i || !player.NodeId.IsValid)
                        throw new InvalidOperationException($"Animation Pose Native Player #{i} is invalid.");
                    int contributionCapacity = player.Code == CharacterPoseOperationCode.BlendStack ||
                                               player.Code == CharacterPoseOperationCode.AnimationSlot
                        ? checked(program.BlendNodes[player.BlendNodeIndex].StackPolicy.MaxActiveSourceEntries + 1)
                        : 1;
                    totalPlayerContributionCapacity = checked(totalPlayerContributionCapacity + contributionCapacity);
                    m_PoseNodeIds[i] = player.NodeId;
                }

                if (poseValueContributionStride < totalPlayerContributionCapacity)
                {
                    throw new InvalidOperationException(
                        "Animation Pose Native value contribution stride cannot contain all Player contributions.");
                }

                m_SlotRanges = Allocate<AnimationPlayerPoseNativeRange>(playerCount);
                int contributionOffset = 0;
                for (int i = 0; i < playerCount; i++)
                {
                    CharacterPresentationPoseOperation player = playerOperations[i];
                    int contributionCapacity = player.Code == CharacterPoseOperationCode.BlendStack ||
                                               player.Code == CharacterPoseOperationCode.AnimationSlot
                        ? checked(program.BlendNodes[player.BlendNodeIndex].StackPolicy.MaxActiveSourceEntries + 1)
                        : 1;
                    m_SlotRanges[i] = new AnimationPlayerPoseNativeRange(
                        i,
                        checked(i * boneCount),
                        checked(i * boneCount),
                        checked(i * parameterCount),
                        contributionOffset,
                        contributionCapacity,
                        checked(contributionOffset * boneCount));
                    contributionOffset = checked(contributionOffset + contributionCapacity);
                }
                if (contributionOffset != totalPlayerContributionCapacity)
                    throw new InvalidOperationException("Animation Pose Native Player contribution capacity is inconsistent.");

                m_Layout = new AnimationPoseNativeAggregateLayout(
                    playerCount,
                    boneCount,
                    parameterCount,
                    totalPlayerContributionCapacity,
                    poseValueCount,
                    poseValueContributionStride,
                    program.Operations.Count,
                    program.FrameCacheCount,
                    program.Stages.Count,
                    outputOperation.OutputValueIndex,
                    m_SlotRanges);

                m_SlotDenseLocalPoses = Allocate<AnimationLocalBonePose>(m_Layout.PlayerPoseCapacity);
                m_SlotDenseVelocities = Allocate<AnimationBlendBoneVelocity>(m_Layout.PlayerVelocityCapacity);
                m_SlotPoseParameters = Allocate<float>(m_Layout.PlayerParameterCapacity);
                m_SlotPoseParameterAvailability = Allocate<byte>(m_Layout.PlayerParameterCapacity);
                m_SlotContributions = Allocate<AnimationPrimitivePoseContribution>(m_Layout.TotalPlayerContributionCapacity);
                m_SlotDenseContributionWeights = Allocate<float>(m_Layout.PlayerDenseContributionWeightCapacity);
                m_SlotContributionCounts = Allocate<int>(m_Layout.PlayerCount);
                m_SlotOutputWeights = Allocate<float>(m_Layout.PlayerCount);
                m_SlotLeftFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.PlayerCount);
                m_SlotRightFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.PlayerCount);
                m_SlotHasFootFeatures = Allocate<byte>(m_Layout.PlayerCount);
                m_SlotAvailability = Allocate<AnimationPoseAvailability>(m_Layout.PlayerCount);
                m_SlotContinuityIdentities = Allocate<ulong>(m_Layout.PlayerCount);
                m_SlotDiscontinuities = Allocate<PoseDiscontinuityNative>(m_Layout.PlayerCount);
                m_SlotInvalidReasons = Allocate<AnimationPoseNativeInvalidReason>(m_Layout.PlayerCount);
                m_SlotCompletedAt = Allocate<ulong>(m_Layout.PlayerCount);
                m_ValueDenseLocalPoses = Allocate<AnimationLocalBonePose>(m_Layout.PoseValuePoseCapacity);
                m_ValueDenseVelocities = Allocate<AnimationBlendBoneVelocity>(m_Layout.PoseValuePoseCapacity);
                m_ValuePoseParameters = Allocate<float>(m_Layout.PoseValueParameterCapacity);
                m_ValuePoseParameterAvailability = Allocate<byte>(m_Layout.PoseValueParameterCapacity);
                m_ValueContributions = Allocate<AnimationPrimitivePoseContribution>(m_Layout.PoseValueContributionCapacity);
                m_ValueDenseContributionWeights = Allocate<float>(m_Layout.PoseValueDenseContributionWeightCapacity);
                m_ValueContributionCounts = Allocate<int>(m_Layout.PoseValueCount);
                m_ValueOutputWeights = Allocate<float>(m_Layout.PoseValueCount);
                m_ValueLeftFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.PoseValueCount);
                m_ValueRightFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.PoseValueCount);
                m_ValueHasFootFeatures = Allocate<byte>(m_Layout.PoseValueCount);
                m_ValueAvailability = Allocate<AnimationPoseAvailability>(m_Layout.PoseValueCount);
                m_ValueContinuityIdentities = Allocate<ulong>(m_Layout.PoseValueCount);
                m_ValueDiscontinuities = Allocate<PoseDiscontinuityNative>(m_Layout.PoseValueCount);
                m_ValueInvalidReasons = Allocate<AnimationPoseNativeInvalidReason>(m_Layout.PoseValueCount);
                m_FrameCacheCompletedAt = Allocate<ulong>(m_Layout.FrameCacheCount);
                m_StageCompletedAt = Allocate<ulong>(m_Layout.StageCount);
                m_StageInvalidOperationIndex = Allocate<int>(m_Layout.StageCount);
                m_PoseGraphInvalidReason = Allocate<AnimationPoseNativeInvalidReason>(1);
                m_PoseGraphInvalidOperationIndex = Allocate<int>(1);
                m_PoseGraphCompletedAt = Allocate<ulong>(1);
                m_FinalAppliedAt = Allocate<ulong>(1);
                m_FinalWriteOutcome = Allocate<AnimationFinalPoseWriteOutcome>(1);
                m_CommittedPage = CaptureActivePage();
                m_PendingPage = AllocatePage();
                m_DenseDoublePageResidentPayloadBytes = checked(
                    CalculatePagePayloadBytes(m_CommittedPage) +
                    CalculatePagePayloadBytes(m_PendingPage));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal CharacterPoseGraphNativeBinding BeginFrame(ulong completionIdentity)
        {
            RequireAlive();
            if (completionIdentity == 0 || completionIdentity <= m_LastCompletionIdentity)
                throw new ArgumentOutOfRangeException(nameof(completionIdentity));
            if (m_CurrentCompletionIdentity != 0)
                throw new InvalidOperationException("Animation Pose Native frame is already open.");

            m_LastCompletionIdentity = completionIdentity;
            m_CurrentCompletionIdentity = completionIdentity;
            BindPage(m_PendingPage);
            for (int i = 0; i < m_Layout.PlayerCount; i++)
            {
                m_SlotContributionCounts[i] = 0;
                m_SlotOutputWeights[i] = 0f;
                m_SlotHasFootFeatures[i] = 0;
                m_SlotAvailability[i] = AnimationPoseAvailability.Invalid;
                m_SlotContinuityIdentities[i] = 0;
                m_SlotDiscontinuities[i] = default;
                m_SlotInvalidReasons[i] = AnimationPoseNativeInvalidReason.SourceIncomplete;
                m_SlotCompletedAt[i] = 0;
            }
            for (int i = 0; i < m_Layout.PoseValueCount; i++)
            {
                m_ValueContributionCounts[i] = 0;
                m_ValueOutputWeights[i] = 0f;
                m_ValueHasFootFeatures[i] = 0;
                m_ValueAvailability[i] = AnimationPoseAvailability.Invalid;
                m_ValueContinuityIdentities[i] = 0;
                m_ValueDiscontinuities[i] = default;
                m_ValueInvalidReasons[i] = AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete;
            }
            for (int i = 0; i < m_FrameCacheCompletedAt.Length; i++)
                m_FrameCacheCompletedAt[i] = 0;
            for (int i = 0; i < m_StageCompletedAt.Length; i++)
            {
                m_StageCompletedAt[i] = 0;
                m_StageInvalidOperationIndex[i] = -1;
            }

            m_PoseGraphInvalidReason[0] = AnimationPoseNativeInvalidReason.PoseGraphInputIncomplete;
            m_PoseGraphInvalidOperationIndex[0] = -1;
            m_PoseGraphCompletedAt[0] = 0;
            m_FinalAppliedAt[0] = 0;
            m_FinalWriteOutcome[0] = AnimationFinalPoseWriteOutcome.None;
            m_FrameBinding = CreateBinding(completionIdentity);
            return m_FrameBinding;
        }

        internal bool HasPendingFrame =>
            m_CurrentCompletionIdentity != 0;

        internal ulong PendingCompletionIdentity =>
            m_CurrentCompletionIdentity;
        internal long DenseDoublePageResidentPayloadBytes =>
            m_DenseDoublePageResidentPayloadBytes;

        internal void RequireStagesCompleted(ulong completionIdentity)
        {
            RequireFrame(completionIdentity);
            for (int stageIndex = 0; stageIndex < m_StageCompletedAt.Length; stageIndex++)
            {
                int invalidOperationIndex =
                    m_StageInvalidOperationIndex[stageIndex];
                if (m_StageCompletedAt[stageIndex] == completionIdentity &&
                    invalidOperationIndex >= -1 &&
                    invalidOperationIndex < m_Layout.OperationCount)
                {
                    continue;
                }
                throw new InvalidOperationException(
                    $"Animation Pose stage #{stageIndex} failed at operation #{m_StageInvalidOperationIndex[stageIndex]}.");
            }
        }

        internal void CommitFrame(ulong completionIdentity)
        {
            RequireFrame(completionIdentity);
            Page previousCommitted = m_CommittedPage;
            m_CommittedPage = m_PendingPage;
            m_PendingPage = previousCommitted;
            m_CommittedBinding = m_FrameBinding;
            m_FrameBinding = default;
            m_CurrentCompletionIdentity = 0;
        }

        internal void DiscardFrame(ulong completionIdentity)
        {
            RequireFrame(completionIdentity);
            BindPage(m_CommittedPage);
            m_FrameBinding = default;
            m_CurrentCompletionIdentity = 0;
        }

        internal bool TryGetCommittedFinalReadBinding(
            out AnimationFinalPoseNativeReadBinding binding)
        {
            RequireAlive();
            if (m_CommittedBinding.CompletionIdentity == 0)
            {
                binding = default;
                return false;
            }
            binding = new AnimationFinalPoseNativeReadBinding(
                in m_CommittedBinding);
            return true;
        }

        internal AnimationPlayerPoseNativeWriteBinding RequirePlayerWriteBinding(
            int physicalSlotIndex,
            ulong completionIdentity)
        {
            RequireFrame(completionIdentity);
            if (physicalSlotIndex < 0 || physicalSlotIndex >= m_Layout.PlayerCount)
                throw new ArgumentOutOfRangeException(nameof(physicalSlotIndex));
            return new AnimationPlayerPoseNativeWriteBinding(in m_FrameBinding, physicalSlotIndex);
        }

        internal CharacterPoseGraphNativeBinding RequirePoseGraphBinding(ulong completionIdentity)
        {
            RequireFrame(completionIdentity);
            return m_FrameBinding;
        }

        internal AnimationPoseValueNativeReadBinding RequirePoseValueReadBinding(
            int valueIndex,
            ulong completionIdentity)
        {
            RequireFrame(completionIdentity);
            return new AnimationPoseValueNativeReadBinding(
                in m_FrameBinding,
                valueIndex);
        }

        internal AnimationFinalPoseNativeReadBinding RequireFinalReadBinding(ulong completionIdentity)
        {
            RequireFrame(completionIdentity);
            return new AnimationFinalPoseNativeReadBinding(in m_FrameBinding);
        }

        internal AnimationFinalPoseWriteOutcome RequireFinalWriteOutcome(
            ulong completionIdentity)
        {
            RequireFrame(completionIdentity);
            AnimationFinalPoseWriteOutcome outcome =
                m_FinalWriteOutcome[0];
            if (outcome == AnimationFinalPoseWriteOutcome.None ||
                outcome == AnimationFinalPoseWriteOutcome.Faulted)
            {
                throw new InvalidOperationException(
                    $"Final animation pose writer failed with outcome '{outcome}'.");
            }
            return outcome;
        }

        internal PoseNodeId RequirePoseNodeId(int physicalSlotIndex)
        {
            RequireAlive();
            if (physicalSlotIndex < 0 || physicalSlotIndex >= m_PoseNodeIds.Length ||
                !m_PoseNodeIds[physicalSlotIndex].IsValid)
            {
                throw new ArgumentOutOfRangeException(nameof(physicalSlotIndex));
            }
            return m_PoseNodeIds[physicalSlotIndex];
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            m_Disposed = true;
            m_FrameBinding = default;
            m_CommittedBinding = default;
            m_CurrentCompletionIdentity = 0;
            if (m_CommittedPage != null)
            {
                DisposePage(m_CommittedPage);
            }
            else
            {
                DisposeActivePage();
            }
            DisposePage(m_PendingPage);
            m_CommittedPage = null;
            m_PendingPage = null;
            DisposeArray(ref m_SlotRanges);
            m_PoseNodeIds = null;
        }

        CharacterPoseGraphNativeBinding CreateBinding(ulong completionIdentity)
        {
            return new CharacterPoseGraphNativeBinding(
                m_Layout,
                completionIdentity,
                m_SlotRanges,
                m_SlotDenseLocalPoses,
                m_SlotDenseVelocities,
                m_SlotPoseParameters,
                m_SlotPoseParameterAvailability,
                m_SlotContributions,
                m_SlotDenseContributionWeights,
                m_SlotContributionCounts,
                m_SlotOutputWeights,
                m_SlotLeftFootFeatures,
                m_SlotRightFootFeatures,
                m_SlotHasFootFeatures,
                m_SlotAvailability,
                m_SlotContinuityIdentities,
                m_SlotDiscontinuities,
                m_SlotInvalidReasons,
                m_SlotCompletedAt,
                m_ValueDenseLocalPoses,
                m_ValueDenseVelocities,
                m_ValuePoseParameters,
                m_ValuePoseParameterAvailability,
                m_ValueContributions,
                m_ValueDenseContributionWeights,
                m_ValueContributionCounts,
                m_ValueOutputWeights,
                m_ValueLeftFootFeatures,
                m_ValueRightFootFeatures,
                m_ValueHasFootFeatures,
                m_ValueAvailability,
                m_ValueContinuityIdentities,
                m_ValueDiscontinuities,
                m_ValueInvalidReasons,
                m_FrameCacheCompletedAt,
                m_StageCompletedAt,
                m_StageInvalidOperationIndex,
                m_PoseGraphInvalidReason,
                m_PoseGraphInvalidOperationIndex,
                m_PoseGraphCompletedAt,
                m_FinalAppliedAt,
                m_FinalWriteOutcome);
        }

        void RequireFrame(ulong completionIdentity)
        {
            RequireAlive();
            if (completionIdentity == 0 || completionIdentity != m_CurrentCompletionIdentity ||
                m_FrameBinding.CompletionIdentity != completionIdentity)
            {
                throw new InvalidOperationException("Animation Pose Native frame completion identity does not match the active frame.");
            }
        }

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationPoseNativeWorkspace));
        }

        static NativeArray<T> Allocate<T>(int length) where T : unmanaged
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            return new NativeArray<T>(length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
        }

        static long CalculatePagePayloadBytes(Page page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            return checked(
                PayloadBytes(page.SlotDenseLocalPoses) +
                PayloadBytes(page.SlotDenseVelocities) +
                PayloadBytes(page.SlotPoseParameters) +
                PayloadBytes(page.SlotPoseParameterAvailability) +
                PayloadBytes(page.SlotContributions) +
                PayloadBytes(page.SlotDenseContributionWeights) +
                PayloadBytes(page.SlotContributionCounts) +
                PayloadBytes(page.SlotOutputWeights) +
                PayloadBytes(page.SlotLeftFootFeatures) +
                PayloadBytes(page.SlotRightFootFeatures) +
                PayloadBytes(page.SlotHasFootFeatures) +
                PayloadBytes(page.SlotAvailability) +
                PayloadBytes(page.SlotContinuityIdentities) +
                PayloadBytes(page.SlotDiscontinuities) +
                PayloadBytes(page.SlotInvalidReasons) +
                PayloadBytes(page.SlotCompletedAt) +
                PayloadBytes(page.ValueDenseLocalPoses) +
                PayloadBytes(page.ValueDenseVelocities) +
                PayloadBytes(page.ValuePoseParameters) +
                PayloadBytes(page.ValuePoseParameterAvailability) +
                PayloadBytes(page.ValueContributions) +
                PayloadBytes(page.ValueDenseContributionWeights) +
                PayloadBytes(page.ValueContributionCounts) +
                PayloadBytes(page.ValueOutputWeights) +
                PayloadBytes(page.ValueLeftFootFeatures) +
                PayloadBytes(page.ValueRightFootFeatures) +
                PayloadBytes(page.ValueHasFootFeatures) +
                PayloadBytes(page.ValueAvailability) +
                PayloadBytes(page.ValueContinuityIdentities) +
                PayloadBytes(page.ValueDiscontinuities) +
                PayloadBytes(page.ValueInvalidReasons) +
                PayloadBytes(page.FrameCacheCompletedAt) +
                PayloadBytes(page.StageCompletedAt) +
                PayloadBytes(page.StageInvalidOperationIndex) +
                PayloadBytes(page.PoseGraphInvalidReason) +
                PayloadBytes(page.PoseGraphInvalidOperationIndex) +
                PayloadBytes(page.PoseGraphCompletedAt) +
                PayloadBytes(page.FinalAppliedAt) +
                PayloadBytes(page.FinalWriteOutcome));
        }

        static long PayloadBytes<T>(NativeArray<T> values)
            where T : unmanaged =>
            checked((long)UnsafeUtility.SizeOf<T>() * values.Length);

        Page CaptureActivePage() => new Page
        {
            SlotDenseLocalPoses = m_SlotDenseLocalPoses,
            SlotDenseVelocities = m_SlotDenseVelocities,
            SlotPoseParameters = m_SlotPoseParameters,
            SlotPoseParameterAvailability = m_SlotPoseParameterAvailability,
            SlotContributions = m_SlotContributions,
            SlotDenseContributionWeights = m_SlotDenseContributionWeights,
            SlotContributionCounts = m_SlotContributionCounts,
            SlotOutputWeights = m_SlotOutputWeights,
            SlotLeftFootFeatures = m_SlotLeftFootFeatures,
            SlotRightFootFeatures = m_SlotRightFootFeatures,
            SlotHasFootFeatures = m_SlotHasFootFeatures,
            SlotAvailability = m_SlotAvailability,
            SlotContinuityIdentities = m_SlotContinuityIdentities,
            SlotDiscontinuities = m_SlotDiscontinuities,
            SlotInvalidReasons = m_SlotInvalidReasons,
            SlotCompletedAt = m_SlotCompletedAt,
            ValueDenseLocalPoses = m_ValueDenseLocalPoses,
            ValueDenseVelocities = m_ValueDenseVelocities,
            ValuePoseParameters = m_ValuePoseParameters,
            ValuePoseParameterAvailability = m_ValuePoseParameterAvailability,
            ValueContributions = m_ValueContributions,
            ValueDenseContributionWeights = m_ValueDenseContributionWeights,
            ValueContributionCounts = m_ValueContributionCounts,
            ValueOutputWeights = m_ValueOutputWeights,
            ValueLeftFootFeatures = m_ValueLeftFootFeatures,
            ValueRightFootFeatures = m_ValueRightFootFeatures,
            ValueHasFootFeatures = m_ValueHasFootFeatures,
            ValueAvailability = m_ValueAvailability,
            ValueContinuityIdentities = m_ValueContinuityIdentities,
            ValueDiscontinuities = m_ValueDiscontinuities,
            ValueInvalidReasons = m_ValueInvalidReasons,
            FrameCacheCompletedAt = m_FrameCacheCompletedAt,
            StageCompletedAt = m_StageCompletedAt,
            StageInvalidOperationIndex = m_StageInvalidOperationIndex,
            PoseGraphInvalidReason = m_PoseGraphInvalidReason,
            PoseGraphInvalidOperationIndex = m_PoseGraphInvalidOperationIndex,
            PoseGraphCompletedAt = m_PoseGraphCompletedAt,
            FinalAppliedAt = m_FinalAppliedAt
            ,
            FinalWriteOutcome = m_FinalWriteOutcome
        };

        Page AllocatePage()
        {
            var page = new Page();
            try
            {
                page.SlotDenseLocalPoses = Allocate<AnimationLocalBonePose>(m_Layout.PlayerPoseCapacity);
                page.SlotDenseVelocities = Allocate<AnimationBlendBoneVelocity>(m_Layout.PlayerVelocityCapacity);
                page.SlotPoseParameters = Allocate<float>(m_Layout.PlayerParameterCapacity);
                page.SlotPoseParameterAvailability = Allocate<byte>(m_Layout.PlayerParameterCapacity);
                page.SlotContributions = Allocate<AnimationPrimitivePoseContribution>(m_Layout.TotalPlayerContributionCapacity);
                page.SlotDenseContributionWeights = Allocate<float>(m_Layout.PlayerDenseContributionWeightCapacity);
                page.SlotContributionCounts = Allocate<int>(m_Layout.PlayerCount);
                page.SlotOutputWeights = Allocate<float>(m_Layout.PlayerCount);
                page.SlotLeftFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.PlayerCount);
                page.SlotRightFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.PlayerCount);
                page.SlotHasFootFeatures = Allocate<byte>(m_Layout.PlayerCount);
                page.SlotAvailability = Allocate<AnimationPoseAvailability>(m_Layout.PlayerCount);
                page.SlotContinuityIdentities = Allocate<ulong>(m_Layout.PlayerCount);
                page.SlotDiscontinuities = Allocate<PoseDiscontinuityNative>(m_Layout.PlayerCount);
                page.SlotInvalidReasons = Allocate<AnimationPoseNativeInvalidReason>(m_Layout.PlayerCount);
                page.SlotCompletedAt = Allocate<ulong>(m_Layout.PlayerCount);
                page.ValueDenseLocalPoses = Allocate<AnimationLocalBonePose>(m_Layout.PoseValuePoseCapacity);
                page.ValueDenseVelocities = Allocate<AnimationBlendBoneVelocity>(m_Layout.PoseValuePoseCapacity);
                page.ValuePoseParameters = Allocate<float>(m_Layout.PoseValueParameterCapacity);
                page.ValuePoseParameterAvailability = Allocate<byte>(m_Layout.PoseValueParameterCapacity);
                page.ValueContributions = Allocate<AnimationPrimitivePoseContribution>(m_Layout.PoseValueContributionCapacity);
                page.ValueDenseContributionWeights = Allocate<float>(m_Layout.PoseValueDenseContributionWeightCapacity);
                page.ValueContributionCounts = Allocate<int>(m_Layout.PoseValueCount);
                page.ValueOutputWeights = Allocate<float>(m_Layout.PoseValueCount);
                page.ValueLeftFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.PoseValueCount);
                page.ValueRightFootFeatures = Allocate<AnimationFootFeatureSample>(m_Layout.PoseValueCount);
                page.ValueHasFootFeatures = Allocate<byte>(m_Layout.PoseValueCount);
                page.ValueAvailability = Allocate<AnimationPoseAvailability>(m_Layout.PoseValueCount);
                page.ValueContinuityIdentities = Allocate<ulong>(m_Layout.PoseValueCount);
                page.ValueDiscontinuities = Allocate<PoseDiscontinuityNative>(m_Layout.PoseValueCount);
                page.ValueInvalidReasons = Allocate<AnimationPoseNativeInvalidReason>(m_Layout.PoseValueCount);
                page.FrameCacheCompletedAt = Allocate<ulong>(m_Layout.FrameCacheCount);
                page.StageCompletedAt = Allocate<ulong>(m_Layout.StageCount);
                page.StageInvalidOperationIndex = Allocate<int>(m_Layout.StageCount);
                page.PoseGraphInvalidReason = Allocate<AnimationPoseNativeInvalidReason>(1);
                page.PoseGraphInvalidOperationIndex = Allocate<int>(1);
                page.PoseGraphCompletedAt = Allocate<ulong>(1);
                page.FinalAppliedAt = Allocate<ulong>(1);
                page.FinalWriteOutcome = Allocate<AnimationFinalPoseWriteOutcome>(1);
                return page;
            }
            catch
            {
                DisposePage(page);
                throw;
            }
        }

        void BindPage(Page page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            m_SlotDenseLocalPoses = page.SlotDenseLocalPoses;
            m_SlotDenseVelocities = page.SlotDenseVelocities;
            m_SlotPoseParameters = page.SlotPoseParameters;
            m_SlotPoseParameterAvailability = page.SlotPoseParameterAvailability;
            m_SlotContributions = page.SlotContributions;
            m_SlotDenseContributionWeights = page.SlotDenseContributionWeights;
            m_SlotContributionCounts = page.SlotContributionCounts;
            m_SlotOutputWeights = page.SlotOutputWeights;
            m_SlotLeftFootFeatures = page.SlotLeftFootFeatures;
            m_SlotRightFootFeatures = page.SlotRightFootFeatures;
            m_SlotHasFootFeatures = page.SlotHasFootFeatures;
            m_SlotAvailability = page.SlotAvailability;
            m_SlotContinuityIdentities = page.SlotContinuityIdentities;
            m_SlotDiscontinuities = page.SlotDiscontinuities;
            m_SlotInvalidReasons = page.SlotInvalidReasons;
            m_SlotCompletedAt = page.SlotCompletedAt;
            m_ValueDenseLocalPoses = page.ValueDenseLocalPoses;
            m_ValueDenseVelocities = page.ValueDenseVelocities;
            m_ValuePoseParameters = page.ValuePoseParameters;
            m_ValuePoseParameterAvailability = page.ValuePoseParameterAvailability;
            m_ValueContributions = page.ValueContributions;
            m_ValueDenseContributionWeights = page.ValueDenseContributionWeights;
            m_ValueContributionCounts = page.ValueContributionCounts;
            m_ValueOutputWeights = page.ValueOutputWeights;
            m_ValueLeftFootFeatures = page.ValueLeftFootFeatures;
            m_ValueRightFootFeatures = page.ValueRightFootFeatures;
            m_ValueHasFootFeatures = page.ValueHasFootFeatures;
            m_ValueAvailability = page.ValueAvailability;
            m_ValueContinuityIdentities = page.ValueContinuityIdentities;
            m_ValueDiscontinuities = page.ValueDiscontinuities;
            m_ValueInvalidReasons = page.ValueInvalidReasons;
            m_FrameCacheCompletedAt = page.FrameCacheCompletedAt;
            m_StageCompletedAt = page.StageCompletedAt;
            m_StageInvalidOperationIndex = page.StageInvalidOperationIndex;
            m_PoseGraphInvalidReason = page.PoseGraphInvalidReason;
            m_PoseGraphInvalidOperationIndex = page.PoseGraphInvalidOperationIndex;
            m_PoseGraphCompletedAt = page.PoseGraphCompletedAt;
            m_FinalAppliedAt = page.FinalAppliedAt;
            m_FinalWriteOutcome = page.FinalWriteOutcome;
        }

        void DisposeActivePage()
        {
            Page page = CaptureActivePage();
            DisposePage(page);
        }

        static void DisposePage(Page page)
        {
            if (page == null)
                return;
            DisposeArray(ref page.FinalAppliedAt);
            DisposeArray(ref page.FinalWriteOutcome);
            DisposeArray(ref page.PoseGraphCompletedAt);
            DisposeArray(ref page.PoseGraphInvalidOperationIndex);
            DisposeArray(ref page.PoseGraphInvalidReason);
            DisposeArray(ref page.StageInvalidOperationIndex);
            DisposeArray(ref page.StageCompletedAt);
            DisposeArray(ref page.FrameCacheCompletedAt);
            DisposeArray(ref page.ValueInvalidReasons);
            DisposeArray(ref page.ValueDiscontinuities);
            DisposeArray(ref page.ValueContinuityIdentities);
            DisposeArray(ref page.ValueAvailability);
            DisposeArray(ref page.ValueHasFootFeatures);
            DisposeArray(ref page.ValueRightFootFeatures);
            DisposeArray(ref page.ValueLeftFootFeatures);
            DisposeArray(ref page.ValueOutputWeights);
            DisposeArray(ref page.ValueContributionCounts);
            DisposeArray(ref page.ValueDenseContributionWeights);
            DisposeArray(ref page.ValueContributions);
            DisposeArray(ref page.ValuePoseParameters);
            DisposeArray(ref page.ValuePoseParameterAvailability);
            DisposeArray(ref page.ValueDenseLocalPoses);
            DisposeArray(ref page.ValueDenseVelocities);
            DisposeArray(ref page.SlotCompletedAt);
            DisposeArray(ref page.SlotInvalidReasons);
            DisposeArray(ref page.SlotDiscontinuities);
            DisposeArray(ref page.SlotContinuityIdentities);
            DisposeArray(ref page.SlotAvailability);
            DisposeArray(ref page.SlotHasFootFeatures);
            DisposeArray(ref page.SlotRightFootFeatures);
            DisposeArray(ref page.SlotLeftFootFeatures);
            DisposeArray(ref page.SlotOutputWeights);
            DisposeArray(ref page.SlotContributionCounts);
            DisposeArray(ref page.SlotDenseContributionWeights);
            DisposeArray(ref page.SlotContributions);
            DisposeArray(ref page.SlotPoseParameters);
            DisposeArray(ref page.SlotPoseParameterAvailability);
            DisposeArray(ref page.SlotDenseVelocities);
            DisposeArray(ref page.SlotDenseLocalPoses);
        }

        static void DisposeArray<T>(ref NativeArray<T> values) where T : unmanaged
        {
            if (values.IsCreated)
                values.Dispose();
            values = default;
        }
    }
}
