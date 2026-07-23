using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    internal sealed class AnimationPresentationRuntimeSnapshotPublisher : IDisposable
    {
        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterPresentationPosePlan m_Program;
        readonly AnimationPoseNativeWorkspace m_Workspace;
        readonly Page[] m_Pages;
        readonly Dictionary<Guid, AnimationPoseWatchIdentity[]> m_PoseWatchInterests =
            new Dictionary<Guid, AnimationPoseWatchIdentity[]>();
        AnimationPoseWatchIdentity[] m_MergedPoseWatchInterests = Array.Empty<AnimationPoseWatchIdentity>();
        int m_ActivePageIndex = -1;
        int m_PendingPageIndex = -1;
        ulong m_PendingCompletionIdentity;
        AnimationPresentationRuntimeSnapshot m_Current;
        bool m_Disposed;

        internal AnimationPresentationRuntimeSnapshotPublisher(
            CharacterPresentationProjection projection,
            in CharacterPoseGraphNativeBinding initialFrame,
            AnimationPoseNativeWorkspace workspace,
            int physicalSourceCapacity)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Program = projection.PosePlan ?? throw new ArgumentException("Animation Pose Program is missing.", nameof(projection));
            m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            m_Program.RequireValid();
            initialFrame.RequireValid();
            if (physicalSourceCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(physicalSourceCapacity));
            int entryCapacity = 0;
            for (int i = 0; i < projection.PosePlan.BlendNodes.Count; i++)
                entryCapacity = checked(entryCapacity + projection.PosePlan.BlendNodes[i].StackPolicy.MaxActiveSourceEntries);
            int lifecycleCapacity = checked(entryCapacity + initialFrame.Layout.PlayerCount + physicalSourceCapacity);
            int blendSpacePlayerCapacity = checked(physicalSourceCapacity * Math.Max(1, projection.BlendSpacePlayers.Count));
            int maximumBlendSpaceSamples = 0;
            for (int i = 0; i < projection.BlendSpaces.Count; i++)
                maximumBlendSpaceSamples = Math.Max(maximumBlendSpaceSamples, projection.BlendSpaces[i].Samples.Count);
            int blendSpaceSampleCapacity = checked(blendSpacePlayerCapacity * Math.Max(1, maximumBlendSpaceSamples));
            m_Pages = new[]
            {
                new Page(m_Program, projection.Rig, initialFrame.Layout, entryCapacity, lifecycleCapacity, physicalSourceCapacity, blendSpacePlayerCapacity, blendSpaceSampleCapacity),
                new Page(m_Program, projection.Rig, initialFrame.Layout, entryCapacity, lifecycleCapacity, physicalSourceCapacity, blendSpacePlayerCapacity, blendSpaceSampleCapacity)
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
            PoseInertializationNativeProgram inertializations,
            AnimationPoseSourcePhysicalRegistry physicalSources)
        {
            RequireAlive();
            frame.RequireValid();
            if (frame.CompletionIdentity != finalRead.CompletionIdentity || stacks == null ||
                inertializations == null || physicalSources == null)
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
                    page.StoredBoneWeights);
                entryOffset = checked(entryOffset + stack.EntryCount);
            }
            page.StackCount = stacks.Count;
            page.EntryCount = entryOffset;
            CopyInertializations(page, inertializations);
            CopySlotContributions(page, in frame, physicalSources);
            CopyOperations(page, in frame, physicalSources);
            CopyPoseWatches(page, in frame, physicalSources);
            CopyFinal(page, in finalRead, physicalSources);
            m_PendingPageIndex = pageIndex;
            m_PendingCompletionIdentity = frame.CompletionIdentity;
        }

        internal AnimationPresentationRuntimeSnapshot Publish(
            IReadOnlyList<AnimationPlaybackLifecycleSnapshot> lifecycle,
            AnimationReleasedPoseSourceSnapshot[] releases,
            int releaseCount,
            BlendSpaceAnimationPoseRequestResolver blendSpaces)
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
            int blendSpacePlayerCount = blendSpaces?.PlayerSnapshotCount ?? 0;
            int blendSpaceSampleCount = blendSpaces?.SampleSnapshotCount ?? 0;
            if (blendSpacePlayerCount > page.BlendSpacePlayers.Length || blendSpaceSampleCount > page.BlendSpaceSamples.Length)
                throw new InvalidOperationException("Animation Blend Space diagnostics fixed capacity was exceeded.");
            for (int i = 0; i < blendSpacePlayerCount; i++)
            {
                AnimationBlendSpacePlayerRuntimeSnapshot player = blendSpaces.GetPlayerSnapshot(i);
                for (int operationIndex = 0; operationIndex < page.OperationCount; operationIndex++)
                {
                    AnimationPoseOperationSnapshot operation = page.Operations[operationIndex];
                    if (operation.Code != CharacterPoseOperationCode.BlendSpacePlayer ||
                        !operation.NodeId.Equals(player.NodeId))
                        continue;
                    player = player.WithPoseResult(operation.Availability, operation.InvalidReason);
                    break;
                }
                page.BlendSpacePlayers[i] = player;
            }
            for (int i = 0; i < blendSpaceSampleCount; i++)
                page.BlendSpaceSamples[i] = blendSpaces.GetSampleSnapshot(i);
            Array.Clear(page.BlendSpacePlayers, blendSpacePlayerCount, page.BlendSpacePlayers.Length - blendSpacePlayerCount);
            Array.Clear(page.BlendSpaceSamples, blendSpaceSampleCount, page.BlendSpaceSamples.Length - blendSpaceSampleCount);
            page.BlendSpacePlayerCount = blendSpacePlayerCount;
            page.BlendSpaceSampleCount = blendSpaceSampleCount;
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

        internal void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests)
        {
            RequireAlive();
            if (ownerId == Guid.Empty)
                throw new ArgumentException("Pose Watch owner identity is missing.", nameof(ownerId));
            int count = interests?.Count ?? 0;
            if (count > AnimationPoseWatchCapacity.PerWindow)
                throw new InvalidOperationException($"Pose Watch window capacity exceeded: {count}/{AnimationPoseWatchCapacity.PerWindow}.");
            var copy = new AnimationPoseWatchIdentity[count];
            var unique = new HashSet<AnimationPoseWatchIdentity>();
            for (int i = 0; i < count; i++)
            {
                AnimationPoseWatchIdentity interest = interests[i];
                if (!interest.IsValid || !unique.Add(interest))
                    throw new ArgumentException("Pose Watch interests contain an invalid or duplicate identity.", nameof(interests));
                copy[i] = interest;
            }
            bool hadPrevious = m_PoseWatchInterests.TryGetValue(ownerId, out AnimationPoseWatchIdentity[] previous);
            m_PoseWatchInterests[ownerId] = copy;
            try
            {
                RebuildMergedPoseWatchInterests();
            }
            catch
            {
                if (hadPrevious)
                    m_PoseWatchInterests[ownerId] = previous;
                else
                    m_PoseWatchInterests.Remove(ownerId);
                RebuildMergedPoseWatchInterests();
                throw;
            }
        }

        internal void RemovePoseWatchInterests(Guid ownerId)
        {
            if (m_Disposed || ownerId == Guid.Empty || !m_PoseWatchInterests.Remove(ownerId))
                return;
            RebuildMergedPoseWatchInterests();
        }

        void RebuildMergedPoseWatchInterests()
        {
            var merged = new HashSet<AnimationPoseWatchIdentity>();
            foreach (AnimationPoseWatchIdentity[] owner in m_PoseWatchInterests.Values)
            {
                for (int i = 0; i < owner.Length; i++)
                    merged.Add(owner[i]);
            }
            if (merged.Count > AnimationPoseWatchCapacity.PerTarget)
                throw new InvalidOperationException($"Pose Watch target capacity exceeded: {merged.Count}/{AnimationPoseWatchCapacity.PerTarget}.");
            m_MergedPoseWatchInterests = merged
                .OrderBy(value => value.GraphId, StringComparer.Ordinal)
                .ThenBy(value => value.NodeId.Value, StringComparer.Ordinal)
                .ThenBy(value => value.CallSite, StringComparer.Ordinal)
                .ToArray();
        }

        void CopyInertializations(Page page, PoseInertializationNativeProgram program)
        {
            if (program.Nodes.Length != m_Program.Inertializations.Count ||
                program.BoneCount != page.BoneIds.Length)
                throw new InvalidOperationException("Pose Inertialization diagnostics layout is inconsistent.");
            for (int nodeIndex = 0; nodeIndex < program.Nodes.Length; nodeIndex++)
            {
                CharacterPresentationInertializationDescriptor descriptor =
                    m_Program.Inertializations[nodeIndex];
                PoseInertializationNativeState state = program.States[nodeIndex];
                PoseInertializationMode mode = default;
                float duration = 0f;
                string ruleIdentity = string.Empty;
                if ((uint)state.ActiveRuleIndex < (uint)program.Rules.Length &&
                    state.RuntimeState != 0 &&
                    state.RuntimeState != PoseInertializationRuntimeState.Reset &&
                    state.RuntimeState != PoseInertializationRuntimeState.Invalid)
                {
                    PoseInertializationNativeRule rule = program.Rules[state.ActiveRuleIndex];
                    mode = rule.Mode;
                    duration = rule.DurationSeconds;
                    ruleIdentity = $"{descriptor.PolicyId}@{descriptor.PolicyRevision}/{rule.SourceProducerIndex}->{rule.TargetProducerIndex}/{rule.Mode}";
                }
                page.Inertializations[nodeIndex] = new PoseInertializationSnapshot(
                    descriptor.NodeId,
                    descriptor.InputPlayerNodeId,
                    descriptor.InputPlayerIndex,
                    state.RuntimeState,
                    state.LastEventIdentity,
                    state.LastReason,
                    state.LastResetReason,
                    state.LastResetSequence,
                    descriptor.PolicyId,
                    descriptor.PolicyRevision,
                    ruleIdentity,
                    state.PreviousEndpoint,
                    state.CurrentEndpoint,
                    state.PreviousContinuityIdentity,
                    state.CurrentContinuityIdentity,
                    mode,
                    state.ElapsedSeconds,
                    duration,
                    state.AccumulatorGeneration,
                    state.HistoryCompletionIdentity,
                    state.OutputCompletionIdentity);
                int offset = nodeIndex * program.BoneCount;
                for (int boneIndex = 0; boneIndex < program.BoneCount; boneIndex++)
                {
                    int index = offset + boneIndex;
                    page.InertialPositionResiduals[index] = program.PositionResiduals[index];
                    page.InertialRotationResiduals[index] = program.RotationResiduals[index];
                    page.InertialScaleResiduals[index] = program.ScaleResiduals[index];
                    page.InertialBoneEnvelopes[index] = program.GetBoneEnvelope(nodeIndex, boneIndex);
                }
            }
            page.InertializationCount = program.Nodes.Length;
        }

        void CopySlotContributions(
            Page page,
            in CharacterPoseGraphNativeBinding frame,
            AnimationPoseSourcePhysicalRegistry physicalSources)
        {
            int destinationIndex = 0;
            for (int slotIndex = 0; slotIndex < frame.Layout.PlayerCount; slotIndex++)
            {
                AnimationPlayerPoseNativeRange range = frame.SlotRanges[slotIndex];
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
            int operationCount = 0;
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = m_Program.Operations[i];
                if (operation.OutputValueIndex < 0)
                    continue;
                CharacterPresentationPoseSourceMapEntry source = m_Program.SourceMap[i];
                int valueIndex = operation.OutputValueIndex;
                int contributionCount = frame.ValueContributionCounts[valueIndex];
                if (contributionCount < 0 || contributionCount > frame.Layout.PoseValueContributionStride)
                    throw new InvalidOperationException($"Animation Pose operation #{i} contribution count is invalid.");
                page.Operations[operationCount++] = new AnimationPoseOperationSnapshot(
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
            page.OperationCount = operationCount;
            page.OperationContributionCount = contributionOffset;
        }

        void CopyPoseWatches(
            Page page,
            in CharacterPoseGraphNativeBinding frame,
            AnimationPoseSourcePhysicalRegistry physicalSources)
        {
            int boneCount = frame.Layout.BoneCount;
            int stride = frame.Layout.PoseValueContributionStride;
            for (int watchIndex = 0; watchIndex < m_MergedPoseWatchInterests.Length; watchIndex++)
            {
                AnimationPoseWatchIdentity identity = m_MergedPoseWatchInterests[watchIndex];
                int poseOffset = watchIndex * boneCount;
                int contributionOffset = watchIndex * stride;
                Array.Clear(page.PoseWatchLocalPoses, poseOffset, boneCount);
                Array.Clear(page.PoseWatchContributions, contributionOffset, stride);
                if (!string.Equals(identity.GraphRevision, m_Program.ContentRevision, StringComparison.Ordinal))
                {
                    page.PoseWatches[watchIndex] = CreateUnavailableWatch(
                        identity,
                        -1,
                        poseOffset,
                        boneCount,
                        contributionOffset,
                        AnimationPoseWatchAvailability.Stale,
                        default,
                        0);
                    continue;
                }
                if (!TryResolvePoseWatchOperation(identity, out CharacterPresentationPoseOperation operation))
                {
                    page.PoseWatches[watchIndex] = CreateUnavailableWatch(
                        identity,
                        -1,
                        poseOffset,
                        boneCount,
                        contributionOffset,
                        AnimationPoseWatchAvailability.Invalid,
                        default,
                        0);
                    continue;
                }
                int valueIndex = operation.OutputValueIndex;
                ulong completion = frame.FrameCacheCompletedAt[operation.Index];
                AnimationPoseAvailability availability = frame.ValueAvailability[valueIndex];
                AnimationPoseNativeInvalidReason invalidReason = frame.ValueInvalidReasons[valueIndex];
                AnimationPoseWatchAvailability watchAvailability = completion != frame.CompletionIdentity
                    ? AnimationPoseWatchAvailability.NotCompleted
                    : availability == AnimationPoseAvailability.Pose
                        ? AnimationPoseWatchAvailability.Pose
                        : availability == AnimationPoseAvailability.NoPose
                            ? AnimationPoseWatchAvailability.NoPose
                            : AnimationPoseWatchAvailability.Invalid;
                int contributionCount = 0;
                if (watchAvailability == AnimationPoseWatchAvailability.Pose)
                {
                    int sourcePoseOffset = valueIndex * boneCount;
                    for (int boneIndex = 0; boneIndex < boneCount; boneIndex++)
                        page.PoseWatchLocalPoses[poseOffset + boneIndex] = frame.ValueDenseLocalPoses[sourcePoseOffset + boneIndex];
                    contributionCount = frame.ValueContributionCounts[valueIndex];
                    if (contributionCount < 0 || contributionCount > stride)
                        throw new InvalidOperationException($"Pose Watch operation #{operation.Index} contribution count is invalid.");
                    int sourceContributionOffset = valueIndex * stride;
                    for (int contributionIndex = 0; contributionIndex < contributionCount; contributionIndex++)
                    {
                        page.PoseWatchContributions[contributionOffset + contributionIndex] = ConvertContribution(
                            frame.ValueContributions[sourceContributionOffset + contributionIndex],
                            physicalSources);
                    }
                }
                page.PoseWatches[watchIndex] = new AnimationPoseWatchSnapshot(
                    identity,
                    operation.Index,
                    poseOffset,
                    boneCount,
                    contributionOffset,
                    contributionCount,
                    watchAvailability,
                    invalidReason,
                    frame.ValueOutputWeights[valueIndex],
                    frame.ValueContinuityIdentities[valueIndex],
                    completion);
            }
            page.PoseWatchCount = m_MergedPoseWatchInterests.Length;
        }

        bool TryResolvePoseWatchOperation(
            AnimationPoseWatchIdentity identity,
            out CharacterPresentationPoseOperation resolved)
        {
            resolved = default;
            bool found = false;
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = m_Program.Operations[i];
                CharacterPresentationPoseSourceMapEntry source = m_Program.SourceMap[i];
                if (operation.OutputValueIndex < 0 ||
                    !string.Equals(source.GraphId, identity.GraphId, StringComparison.Ordinal) ||
                    !source.NodeId.Equals(identity.NodeId) ||
                    !string.Equals(source.CallSite, identity.CallSite, StringComparison.Ordinal))
                {
                    continue;
                }
                if (found)
                    throw new InvalidOperationException($"Pose Watch identity '{identity}' resolves to multiple compiled operations.");
                resolved = operation;
                found = true;
            }
            return found;
        }

        static AnimationPoseWatchSnapshot CreateUnavailableWatch(
            AnimationPoseWatchIdentity identity,
            int operationIndex,
            int poseOffset,
            int boneCount,
            int contributionOffset,
            AnimationPoseWatchAvailability availability,
            AnimationPoseNativeInvalidReason invalidReason,
            ulong completionIdentity) =>
            new AnimationPoseWatchSnapshot(
                identity,
                operationIndex,
                poseOffset,
                boneCount,
                contributionOffset,
                0,
                availability,
                invalidReason,
                0f,
                0,
                completionIdentity);

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
                    finalRead.PoseParameters[i],
                    finalRead.PoseParameterAvailability[i] != 0);
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
            PoseNodeId playerNodeId = m_Workspace.RequirePoseNodeId(primitive.PhysicalPlayerIndex);
            if (primitive.Kind == AnimationPoseContributionKind.Live)
            {
                var physical = new AnimationPhysicalSourceIdentity(
                    new AnimationPhysicalSourceIndex(primitive.PhysicalSourceIndex),
                    primitive.PhysicalSourceGeneration);
                sourceId = physicalSources.RequireSourceId(physical);
                if (physicalSources.RequirePoseNodeId(physical) != playerNodeId)
                    throw new InvalidOperationException("Animation diagnostic contribution Player identity is inconsistent.");
                if (physicalSources.RequireProgramProducerIndex(physical) != primitive.ProgramProducerIndex)
                    throw new InvalidOperationException("Animation diagnostic contribution producer identity is inconsistent.");
            }
            return new AnimationPoseSourceContribution(
                playerNodeId,
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
                CharacterPresentationPosePlan program,
                CharacterAnimationRigPayload rig,
                AnimationPoseNativeAggregateLayout layout,
                int entryCapacity,
                int lifecycleCapacity,
                int releaseCapacity,
                int blendSpacePlayerCapacity,
                int blendSpaceSampleCapacity)
            {
                Lease = new FinalAnimationPoseFramePageLease();
                Stacks = new AnimationBlendStackSnapshot[layout.PlayerCount];
                Inertializations = new PoseInertializationSnapshot[program.Inertializations.Count];
                Entries = new AnimationBlendStackEntrySnapshot[entryCapacity];
                Lifecycle = new AnimationPlaybackLifecycleSnapshot[lifecycleCapacity];
                Operations = new AnimationPoseOperationSnapshot[program.Operations.Count];
                Parameters = new AnimationPoseParameterSnapshot[program.Parameters.Count];
                BlendSpacePlayers = new AnimationBlendSpacePlayerRuntimeSnapshot[blendSpacePlayerCapacity];
                BlendSpaceSamples = new AnimationBlendSpaceSampleRuntimeSnapshot[blendSpaceSampleCapacity];
                SlotContributions = new AnimationPoseSourceContribution[layout.TotalPlayerContributionCapacity];
                OperationContributions = new AnimationPoseSourceContribution[
                    checked(program.Operations.Count * layout.PoseValueContributionStride)];
                FinalContributions = new AnimationPoseSourceContribution[layout.PoseValueContributionStride];
                Releases = new AnimationReleasedPoseSourceSnapshot[releaseCapacity];
                PoseWatches = new AnimationPoseWatchSnapshot[AnimationPoseWatchCapacity.PerTarget];
                PoseWatchLocalPoses = new AnimationLocalBonePose[checked(AnimationPoseWatchCapacity.PerTarget * layout.BoneCount)];
                PoseWatchContributions = new AnimationPoseSourceContribution[
                    checked(AnimationPoseWatchCapacity.PerTarget * layout.PoseValueContributionStride)];
                BoneIds = new AnimationBoneId[layout.BoneCount];
                EntryBoneWeights = new float[checked(entryCapacity * layout.BoneCount)];
                StoredBoneWeights = new float[checked(layout.PlayerCount * layout.BoneCount)];
                InertialPositionResiduals = new Vector3[checked(program.Inertializations.Count * layout.BoneCount)];
                InertialRotationResiduals = new Vector3[checked(program.Inertializations.Count * layout.BoneCount)];
                InertialScaleResiduals = new Vector3[checked(program.Inertializations.Count * layout.BoneCount)];
                InertialBoneEnvelopes = new float[checked(program.Inertializations.Count * layout.BoneCount)];
                SlotContributionBoneWeights = new float[checked(layout.TotalPlayerContributionCapacity * layout.BoneCount)];
                OperationContributionBoneWeights = new float[
                    checked(program.Operations.Count * layout.PoseValueContributionStride * layout.BoneCount)];
                FinalContributionBoneWeights = new float[checked(layout.PoseValueContributionStride * layout.BoneCount)];
                for (int i = 0; i < BoneIds.Length; i++)
                    BoneIds[i] = rig.Bones[i].BoneId;
            }

            internal readonly FinalAnimationPoseFramePageLease Lease;
            internal readonly AnimationBlendStackSnapshot[] Stacks;
            internal readonly PoseInertializationSnapshot[] Inertializations;
            internal readonly AnimationBlendStackEntrySnapshot[] Entries;
            internal readonly AnimationPlaybackLifecycleSnapshot[] Lifecycle;
            internal readonly AnimationPoseOperationSnapshot[] Operations;
            internal readonly AnimationPoseParameterSnapshot[] Parameters;
            internal readonly AnimationBlendSpacePlayerRuntimeSnapshot[] BlendSpacePlayers;
            internal readonly AnimationBlendSpaceSampleRuntimeSnapshot[] BlendSpaceSamples;
            internal readonly AnimationPoseSourceContribution[] SlotContributions;
            internal readonly AnimationPoseSourceContribution[] OperationContributions;
            internal readonly AnimationPoseSourceContribution[] FinalContributions;
            internal readonly AnimationReleasedPoseSourceSnapshot[] Releases;
            internal readonly AnimationPoseWatchSnapshot[] PoseWatches;
            internal readonly AnimationLocalBonePose[] PoseWatchLocalPoses;
            internal readonly AnimationPoseSourceContribution[] PoseWatchContributions;
            internal readonly AnimationBoneId[] BoneIds;
            internal readonly float[] EntryBoneWeights;
            internal readonly float[] StoredBoneWeights;
            internal readonly Vector3[] InertialPositionResiduals;
            internal readonly Vector3[] InertialRotationResiduals;
            internal readonly Vector3[] InertialScaleResiduals;
            internal readonly float[] InertialBoneEnvelopes;
            internal readonly float[] SlotContributionBoneWeights;
            internal readonly float[] OperationContributionBoneWeights;
            internal readonly float[] FinalContributionBoneWeights;
            internal int StackCount;
            internal int InertializationCount;
            internal int EntryCount;
            internal int LifecycleCount;
            internal int OperationCount;
            internal int ParameterCount;
            internal int BlendSpacePlayerCount;
            internal int BlendSpaceSampleCount;
            internal int SlotContributionCount;
            internal int OperationContributionCount;
            internal int FinalContributionCount;
            internal int ReleaseCount;
            internal int PoseWatchCount;
            internal ulong CompletionIdentity;
            internal AnimationPoseAvailability FinalAvailability;
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
                InertializationCount = 0;
                EntryCount = 0;
                LifecycleCount = 0;
                OperationCount = 0;
                ParameterCount = 0;
                BlendSpacePlayerCount = 0;
                BlendSpaceSampleCount = 0;
                SlotContributionCount = 0;
                OperationContributionCount = 0;
                FinalContributionCount = 0;
                ReleaseCount = 0;
                PoseWatchCount = 0;
            }

            internal AnimationPresentationRuntimeSnapshot CreateSnapshot(
                CharacterPresentationProjection projection,
                CharacterPresentationPosePlan program,
                ulong leaseIdentity)
            {
                return new AnimationPresentationRuntimeSnapshot(
                    projection.ProjectionRevision,
                    projection.Rig.RigId,
                    projection.Rig.RigRevision,
                    program.PoseGraphId,
                    program.ContentRevision,
                    program.PlanHash,
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
                    Inertializations,
                    InertializationCount,
                    Entries,
                    EntryCount,
                    Lifecycle,
                    LifecycleCount,
                    Operations,
                    OperationCount,
                    Parameters,
                    ParameterCount,
                    BlendSpacePlayers,
                    BlendSpacePlayerCount,
                    BlendSpaceSamples,
                    BlendSpaceSampleCount,
                    SlotContributions,
                    SlotContributionCount,
                    OperationContributions,
                    OperationContributionCount,
                    FinalContributions,
                    FinalContributionCount,
                    Releases,
                    ReleaseCount,
                    PoseWatches,
                    PoseWatchCount,
                    PoseWatchLocalPoses,
                    PoseWatchContributions,
                    BoneIds,
                    EntryBoneWeights,
                    StoredBoneWeights,
                    InertialPositionResiduals,
                    InertialRotationResiduals,
                    InertialScaleResiduals,
                    InertialBoneEnvelopes,
                    SlotContributionBoneWeights,
                    OperationContributionBoneWeights,
                    FinalContributionBoneWeights);
            }
        }
    }
}
