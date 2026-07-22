using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    internal sealed class AnimationPresentationRuntimeSnapshotPublisher : IDisposable
    {
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterPresentationPoseProgram m_Program;
        readonly Page[] m_Pages;
        int m_ActivePageIndex = -1;
        int m_PendingPageIndex = -1;
        ulong m_PendingCompletionIdentity;
        AnimationPresentationRuntimeSnapshot m_Current;
        bool m_Disposed;

        internal AnimationPresentationRuntimeSnapshotPublisher(
            CharacterPresentationProjection projection,
            in CharacterPoseGraphNativeBinding initialFrame,
            int physicalSourceCapacity)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Program = projection.PoseProgram ?? throw new ArgumentException("Animation Pose Program is missing.", nameof(projection));
            m_Program.RequireValid();
            initialFrame.RequireValid();
            if (physicalSourceCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(physicalSourceCapacity));
            int entryCapacity = 0;
            for (int i = 0; i < projection.BlendSlots.Count; i++)
                entryCapacity = checked(entryCapacity + projection.BlendSlots[i].StackPolicy.MaxActiveSourceEntries);
            int lifecycleCapacity = checked(entryCapacity + initialFrame.Layout.SlotCount + physicalSourceCapacity);
            m_Pages = new[]
            {
                new Page(m_Program, projection.Rig, initialFrame.Layout, entryCapacity, lifecycleCapacity, physicalSourceCapacity),
                new Page(m_Program, projection.Rig, initialFrame.Layout, entryCapacity, lifecycleCapacity, physicalSourceCapacity)
            };
        }

        internal AnimationPresentationRuntimeSnapshot Current
        {
            get
            {
                RequireAlive();
                return m_Current;
            }
        }

        internal bool HasCurrent
        {
            get
            {
                RequireAlive();
                return m_ActivePageIndex >= 0;
            }
        }

        internal void BeginFrame(
            in CharacterPoseGraphNativeBinding frame,
            in AnimationFinalPoseNativeReadBinding finalRead,
            IReadOnlyList<AnimationBlendStackRuntime> stacks,
            AnimationPoseSourcePhysicalRegistry physicalSources)
        {
            RequireAlive();
            frame.RequireValid();
            if (frame.CompletionIdentity != finalRead.CompletionIdentity || stacks == null || physicalSources == null)
                throw new ArgumentException("Animation runtime diagnostics frame inputs are inconsistent.");
            if (m_PendingPageIndex >= 0)
                throw new InvalidOperationException("Animation runtime diagnostics has an unpublished frame.");

            int pageIndex = m_ActivePageIndex == 0 ? 1 : 0;
            Page page = m_Pages[pageIndex];
            page.Lease.Invalidate();
            page.ClearCounts();
            page.CompletionIdentity = frame.CompletionIdentity;

            int entryOffset = 0;
            for (int stackIndex = 0; stackIndex < stacks.Count; stackIndex++)
            {
                AnimationBlendStackRuntime stack = stacks[stackIndex];
                stack.CopyDiagnostics(
                    stackIndex,
                    page.Stacks,
                    page.Entries,
                    entryOffset,
                    page.EntryBoneWeights,
                    page.StoredBoneWeights,
                    page.InertialBoneWeights);
                entryOffset = checked(entryOffset + stack.EntryCount);
            }
            page.StackCount = stacks.Count;
            page.EntryCount = entryOffset;
            CopySlotContributions(page, in frame, physicalSources);
            CopyOperations(page, in frame, physicalSources);
            CopyFinal(page, in finalRead, physicalSources);
            m_PendingPageIndex = pageIndex;
            m_PendingCompletionIdentity = frame.CompletionIdentity;
        }

        internal AnimationPresentationRuntimeSnapshot Publish(
            IReadOnlyList<AnimationPlaybackLifecycleSnapshot> lifecycle,
            AnimationReleasedPoseSourceSnapshot[] releases,
            int releaseCount)
        {
            RequireAlive();
            if (m_PendingPageIndex < 0 || m_PendingCompletionIdentity == 0)
                throw new InvalidOperationException("Animation runtime diagnostics has no completed native frame.");
            Page page = m_Pages[m_PendingPageIndex];
            int lifecycleCount = lifecycle?.Count ?? 0;
            if (lifecycleCount > page.Lifecycle.Length || releases == null || releaseCount < 0 || releaseCount > releases.Length || releaseCount > page.Releases.Length)
                throw new InvalidOperationException("Animation runtime diagnostics fixed capacity was exceeded.");
            for (int i = 0; i < lifecycleCount; i++)
                page.Lifecycle[i] = lifecycle[i];
            Array.Clear(page.Lifecycle, lifecycleCount, page.Lifecycle.Length - lifecycleCount);
            Array.Copy(releases, 0, page.Releases, 0, releaseCount);
            Array.Clear(page.Releases, releaseCount, page.Releases.Length - releaseCount);
            page.LifecycleCount = lifecycleCount;
            page.ReleaseCount = releaseCount;
            page.Lease.BeginWrite(m_PendingCompletionIdentity);
            m_Current = page.CreateSnapshot(m_Projection, m_Program, m_PendingCompletionIdentity);
            m_ActivePageIndex = m_PendingPageIndex;
            m_PendingPageIndex = -1;
            m_PendingCompletionIdentity = 0;
            return m_Current;
        }

        internal void Invalidate()
        {
            if (m_Disposed)
                return;
            for (int i = 0; i < m_Pages.Length; i++)
                m_Pages[i].Lease.Invalidate();
            m_Current = default;
            m_ActivePageIndex = -1;
            m_PendingPageIndex = -1;
            m_PendingCompletionIdentity = 0;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;
            Invalidate();
            m_Disposed = true;
        }

        void CopySlotContributions(
            Page page,
            in CharacterPoseGraphNativeBinding frame,
            AnimationPoseSourcePhysicalRegistry physicalSources)
        {
            int destinationIndex = 0;
            for (int slotIndex = 0; slotIndex < frame.Layout.SlotCount; slotIndex++)
            {
                AnimationPoseSlotNativeRange range = frame.SlotRanges[slotIndex];
                int count = frame.SlotContributionCounts[slotIndex];
                if (count < 0 || count > range.ContributionCapacity)
                    throw new InvalidOperationException($"Pose Slot #{slotIndex} contribution count is invalid.");
                for (int i = 0; i < count; i++)
                {
                    AnimationPrimitivePoseContribution primitive = frame.SlotContributions[range.ContributionOffset + i];
                    page.SlotContributions[destinationIndex] = ConvertContribution(primitive, physicalSources);
                    for (int boneIndex = 0; boneIndex < frame.Layout.BoneCount; boneIndex++)
                    {
                        page.SlotContributionBoneWeights[destinationIndex * frame.Layout.BoneCount + boneIndex] =
                            frame.SlotDenseContributionWeights[
                                range.DenseContributionWeightOffset + i * frame.Layout.BoneCount + boneIndex];
                    }
                    destinationIndex++;
                }
            }
            page.SlotContributionCount = destinationIndex;
        }

        void CopyOperations(
            Page page,
            in CharacterPoseGraphNativeBinding frame,
            AnimationPoseSourcePhysicalRegistry physicalSources)
        {
            int contributionOffset = 0;
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = m_Program.Operations[i];
                CharacterPresentationPoseSourceMapEntry source = m_Program.SourceMap[i];
                int valueIndex = operation.OutputPoseValueIndex;
                int contributionCount = frame.ValueContributionCounts[valueIndex];
                if (contributionCount < 0 || contributionCount > frame.Layout.PoseValueContributionStride)
                    throw new InvalidOperationException($"Animation Pose operation #{i} contribution count is invalid.");
                page.Operations[i] = new AnimationPoseOperationSnapshot(
                    i,
                    source.GraphId,
                    source.NodeId,
                    source.CallSite,
                    operation.Code,
                    frame.ValueAvailability[valueIndex],
                    frame.ValueInvalidReasons[valueIndex],
                    frame.ValueOutputWeights[valueIndex],
                    frame.ValueContinuityIdentities[valueIndex],
                    frame.FrameCacheCompletedAt[operation.Index],
                    contributionOffset,
                    contributionCount);
                int sourceOffset = checked(valueIndex * frame.Layout.PoseValueContributionStride);
                for (int contributionIndex = 0; contributionIndex < contributionCount; contributionIndex++)
                {
                    int destinationIndex = contributionOffset + contributionIndex;
                    page.OperationContributions[destinationIndex] = ConvertContribution(
                        frame.ValueContributions[sourceOffset + contributionIndex],
                        physicalSources);
                    for (int boneIndex = 0; boneIndex < frame.Layout.BoneCount; boneIndex++)
                    {
                        page.OperationContributionBoneWeights[destinationIndex * frame.Layout.BoneCount + boneIndex] =
                            frame.ValueDenseContributionWeights[
                                (sourceOffset + contributionIndex) * frame.Layout.BoneCount + boneIndex];
                    }
                }
                contributionOffset = checked(contributionOffset + contributionCount);
            }
            page.OperationCount = m_Program.Operations.Count;
            page.OperationContributionCount = contributionOffset;
        }

        void CopyFinal(
            Page page,
            in AnimationFinalPoseNativeReadBinding finalRead,
            AnimationPoseSourcePhysicalRegistry physicalSources)
        {
            int contributionCount = finalRead.ContributionCount[0];
            if (contributionCount < 0 || contributionCount > finalRead.Contributions.Length)
                throw new InvalidOperationException("Final Animation Pose contribution count is invalid.");
            for (int i = 0; i < m_Program.Parameters.Count; i++)
            {
                page.Parameters[i] = new AnimationPoseParameterSnapshot(
                    m_Program.Parameters[i].ParameterId,
                    finalRead.PoseParameters[i]);
            }
            for (int i = 0; i < contributionCount; i++)
            {
                page.FinalContributions[i] = ConvertContribution(finalRead.Contributions[i], physicalSources);
                for (int boneIndex = 0; boneIndex < page.BoneIds.Length; boneIndex++)
                {
                    page.FinalContributionBoneWeights[i * page.BoneIds.Length + boneIndex] =
                        finalRead.DenseContributionWeights[i * page.BoneIds.Length + boneIndex];
                }
            }
            page.ParameterCount = m_Program.Parameters.Count;
            page.FinalContributionCount = contributionCount;
            page.FinalAvailability = finalRead.Availability[0];
            page.FinalInvalidReason = finalRead.PoseGraphInvalidReason[0] != AnimationPoseNativeInvalidReason.None
                ? finalRead.PoseGraphInvalidReason[0]
                : finalRead.OutputInvalidReason[0];
            page.InvalidOperationIndex = finalRead.PoseGraphInvalidOperationIndex[0];
            page.PoseGraphCompletedAt = finalRead.PoseGraphCompletedAt[0];
            page.FinalAppliedAt = finalRead.AppliedAt[0];
            page.ContinuityIdentity = finalRead.ContinuityIdentity[0];
            page.LeftFootFeatures = finalRead.LeftFootFeatures[0];
            page.RightFootFeatures = finalRead.RightFootFeatures[0];
            page.HasFootFeatures = finalRead.HasFootFeatures[0] == 1;
        }

        AnimationPoseSourceContribution ConvertContribution(
            AnimationPrimitivePoseContribution primitive,
            AnimationPoseSourcePhysicalRegistry physicalSources)
        {
            AnimationPoseSourceId sourceId = default;
            if (primitive.PhysicalSlotIndex < 0 || primitive.PhysicalSlotIndex >= m_Program.Slots.Count)
                throw new InvalidOperationException("Animation diagnostic contribution Pose Slot index is invalid.");
            PoseSlotId poseSlotId = m_Program.Slots[primitive.PhysicalSlotIndex].PoseSlotId;
            if (primitive.Kind == AnimationPoseContributionKind.Live)
            {
                var physical = new AnimationPhysicalSourceIdentity(
                    new AnimationPhysicalSourceIndex(primitive.PhysicalSourceIndex),
                    primitive.PhysicalSourceGeneration);
                sourceId = physicalSources.RequireSourceId(physical);
                if (physicalSources.RequirePoseSlotId(physical) != poseSlotId)
                    throw new InvalidOperationException("Animation diagnostic contribution Pose Slot identity is inconsistent.");
                if (physicalSources.RequireProgramProducerIndex(physical) != primitive.ProgramProducerIndex)
                    throw new InvalidOperationException("Animation diagnostic contribution producer identity is inconsistent.");
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

        void RequireAlive()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(AnimationPresentationRuntimeSnapshotPublisher));
        }

        sealed class Page
        {
            internal Page(
                CharacterPresentationPoseProgram program,
                CharacterAnimationRigPayload rig,
                AnimationPoseNativeAggregateLayout layout,
                int entryCapacity,
                int lifecycleCapacity,
                int releaseCapacity)
            {
                Lease = new FinalAnimationPoseFramePageLease();
                Stacks = new AnimationBlendStackSnapshot[layout.SlotCount];
                Entries = new AnimationBlendStackEntrySnapshot[entryCapacity];
                Lifecycle = new AnimationPlaybackLifecycleSnapshot[lifecycleCapacity];
                Operations = new AnimationPoseOperationSnapshot[program.Operations.Count];
                Parameters = new AnimationPoseParameterSnapshot[program.Parameters.Count];
                SlotContributions = new AnimationPoseSourceContribution[layout.TotalSlotContributionCapacity];
                OperationContributions = new AnimationPoseSourceContribution[
                    checked(program.Operations.Count * layout.PoseValueContributionStride)];
                FinalContributions = new AnimationPoseSourceContribution[layout.PoseValueContributionStride];
                Releases = new AnimationReleasedPoseSourceSnapshot[releaseCapacity];
                BoneIds = new AnimationBoneId[layout.BoneCount];
                EntryBoneWeights = new float[checked(entryCapacity * layout.BoneCount)];
                StoredBoneWeights = new float[checked(layout.SlotCount * layout.BoneCount)];
                InertialBoneWeights = new float[checked(layout.SlotCount * layout.BoneCount)];
                SlotContributionBoneWeights = new float[checked(layout.TotalSlotContributionCapacity * layout.BoneCount)];
                OperationContributionBoneWeights = new float[
                    checked(program.Operations.Count * layout.PoseValueContributionStride * layout.BoneCount)];
                FinalContributionBoneWeights = new float[checked(layout.PoseValueContributionStride * layout.BoneCount)];
                for (int i = 0; i < BoneIds.Length; i++)
                    BoneIds[i] = rig.Bones[i].BoneId;
            }

            internal readonly FinalAnimationPoseFramePageLease Lease;
            internal readonly AnimationBlendStackSnapshot[] Stacks;
            internal readonly AnimationBlendStackEntrySnapshot[] Entries;
            internal readonly AnimationPlaybackLifecycleSnapshot[] Lifecycle;
            internal readonly AnimationPoseOperationSnapshot[] Operations;
            internal readonly AnimationPoseParameterSnapshot[] Parameters;
            internal readonly AnimationPoseSourceContribution[] SlotContributions;
            internal readonly AnimationPoseSourceContribution[] OperationContributions;
            internal readonly AnimationPoseSourceContribution[] FinalContributions;
            internal readonly AnimationReleasedPoseSourceSnapshot[] Releases;
            internal readonly AnimationBoneId[] BoneIds;
            internal readonly float[] EntryBoneWeights;
            internal readonly float[] StoredBoneWeights;
            internal readonly float[] InertialBoneWeights;
            internal readonly float[] SlotContributionBoneWeights;
            internal readonly float[] OperationContributionBoneWeights;
            internal readonly float[] FinalContributionBoneWeights;
            internal int StackCount;
            internal int EntryCount;
            internal int LifecycleCount;
            internal int OperationCount;
            internal int ParameterCount;
            internal int SlotContributionCount;
            internal int OperationContributionCount;
            internal int FinalContributionCount;
            internal int ReleaseCount;
            internal ulong CompletionIdentity;
            internal PoseSlotFrameAvailability FinalAvailability;
            internal AnimationPoseNativeInvalidReason FinalInvalidReason;
            internal int InvalidOperationIndex;
            internal ulong PoseGraphCompletedAt;
            internal ulong FinalAppliedAt;
            internal ulong ContinuityIdentity;
            internal AnimationFootFeatureSample LeftFootFeatures;
            internal AnimationFootFeatureSample RightFootFeatures;
            internal bool HasFootFeatures;

            internal void ClearCounts()
            {
                StackCount = 0;
                EntryCount = 0;
                LifecycleCount = 0;
                OperationCount = 0;
                ParameterCount = 0;
                SlotContributionCount = 0;
                OperationContributionCount = 0;
                FinalContributionCount = 0;
                ReleaseCount = 0;
            }

            internal AnimationPresentationRuntimeSnapshot CreateSnapshot(
                CharacterPresentationProjection projection,
                CharacterPresentationPoseProgram program,
                ulong leaseIdentity)
            {
                return new AnimationPresentationRuntimeSnapshot(
                    projection.ProjectionRevision,
                    projection.Rig.RigId,
                    projection.Rig.RigRevision,
                    program.PoseGraphId,
                    program.ContentRevision,
                    program.ProgramHash,
                    CompletionIdentity,
                    FinalAvailability,
                    FinalInvalidReason,
                    InvalidOperationIndex,
                    PoseGraphCompletedAt,
                    FinalAppliedAt,
                    ContinuityIdentity,
                    LeftFootFeatures,
                    RightFootFeatures,
                    HasFootFeatures,
                    Lease,
                    leaseIdentity,
                    Stacks,
                    StackCount,
                    Entries,
                    EntryCount,
                    Lifecycle,
                    LifecycleCount,
                    Operations,
                    OperationCount,
                    Parameters,
                    ParameterCount,
                    SlotContributions,
                    SlotContributionCount,
                    OperationContributions,
                    OperationContributionCount,
                    FinalContributions,
                    FinalContributionCount,
                    Releases,
                    ReleaseCount,
                    BoneIds,
                    EntryBoneWeights,
                    StoredBoneWeights,
                    InertialBoneWeights,
                    SlotContributionBoneWeights,
                    OperationContributionBoneWeights,
                    FinalContributionBoneWeights);
            }
        }
    }
}
