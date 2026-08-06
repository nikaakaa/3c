using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.BlendStack;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;
using ThirdPersonCharacter.Pipeline.Animation.Presentation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Animation.Diagnostics
{
    internal sealed class AnimationPresentationRuntimeSnapshotPublisher : IDisposable
    {
        const int InterestOwnerCapacity = 16;
        const AnimationPresentationDiagnosticsInterest ExplicitOwnerMask =
            AnimationPresentationDiagnosticsInterest.LiveState |
            AnimationPresentationDiagnosticsInterest.Capture |
            AnimationPresentationDiagnosticsInterest.OperationDetail |
            AnimationPresentationDiagnosticsInterest.FinalPoseDetail;

        readonly CharacterPresentationProjection m_Projection;
        readonly CharacterPresentationPosePlan m_Program;
        readonly CharacterPoseGraphNativeProgram m_NativeProgram;
        readonly CharacterFinalIkFullBodySolver[] m_FullBodyIkSolvers;
        readonly AnimationPoseNativeWorkspace m_Workspace;
        readonly Page[] m_Pages;
        readonly Guid[] m_InterestOwnerIds = new Guid[InterestOwnerCapacity];
        readonly AnimationPresentationDiagnosticsInterest[] m_OwnerInterests =
            new AnimationPresentationDiagnosticsInterest[InterestOwnerCapacity];
        readonly int[] m_OwnerPoseWatchCounts = new int[InterestOwnerCapacity];
        readonly AnimationPoseWatchIdentity[] m_OwnerPoseWatches =
            new AnimationPoseWatchIdentity[checked(InterestOwnerCapacity * AnimationPoseWatchCapacity.PerWindow)];
        readonly AnimationPoseWatchIdentity[] m_MergedPoseWatchInterests =
            new AnimationPoseWatchIdentity[AnimationPoseWatchCapacity.PerTarget];
        readonly AnimationPoseWatchIdentity[] m_PoseWatchMergeScratch =
            new AnimationPoseWatchIdentity[AnimationPoseWatchCapacity.PerTarget];
        AnimationPresentationDiagnosticsInterest m_Interest;
        int m_MergedPoseWatchInterestCount;
        int m_ActivePageIndex = -1;
        int m_PendingPageIndex = -1;
        ulong m_PendingCompletionIdentity;
        ulong m_NoInterestSkipCount;
        AnimationPresentationRuntimeSnapshot m_Current;
        bool m_Disposed;

        internal AnimationPresentationRuntimeSnapshotPublisher(
            CharacterPresentationProjection projection,
            CharacterPoseGraphNativeProgram nativeProgram,
            CharacterFinalIkFullBodySolver[] fullBodyIkSolvers,
            in CharacterPoseGraphNativeBinding initialFrame,
            AnimationPoseNativeWorkspace workspace,
            int physicalSourceCapacity)
        {
            m_Projection = projection ?? throw new ArgumentNullException(nameof(projection));
            m_Program = projection.PosePlan ?? throw new ArgumentException("Animation Pose Program is missing.", nameof(projection));
            m_NativeProgram = nativeProgram ?? throw new ArgumentNullException(nameof(nativeProgram));
            m_FullBodyIkSolvers = fullBodyIkSolvers ?? throw new ArgumentNullException(nameof(fullBodyIkSolvers));
            m_Workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
            m_Program.RequireValid();
            m_NativeProgram.RequireValid();
            if (m_FullBodyIkSolvers.Length != m_Program.FullBodyIks.Count)
                throw new ArgumentException("FullBodyIK diagnostics solver layout is inconsistent.", nameof(fullBodyIkSolvers));
            initialFrame.RequireValid();
            if (physicalSourceCapacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(physicalSourceCapacity));
            int entryCapacity = 0;
            for (int i = 0; i < projection.PosePlan.BlendNodes.Count; i++)
                entryCapacity = checked(entryCapacity + projection.PosePlan.BlendNodes[i].StackPolicy.MaxActiveSourceEntries);
            int blendSpacePlayerCapacity = projection.BlendSpacePlayers.Count;
            int blendSpaceSampleCapacity = 0;
            for (int i = 0; i < projection.BlendSpacePlayers.Count; i++)
            {
                int planIndex =
                    projection.BlendSpacePlayers[i].BlendSpacePlanIndex;
                blendSpaceSampleCapacity = checked(
                    blendSpaceSampleCapacity +
                    projection.BlendSpaces[planIndex].Samples.Count);
            }
            m_Pages = new[]
            {
                new Page(m_Program, projection.Rig, initialFrame.Layout, entryCapacity, physicalSourceCapacity, blendSpacePlayerCapacity, blendSpaceSampleCapacity, projection.LinkedPose.Groups.Count),
                new Page(m_Program, projection.Rig, initialFrame.Layout, entryCapacity, physicalSourceCapacity, blendSpacePlayerCapacity, blendSpaceSampleCapacity, projection.LinkedPose.Groups.Count)
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

        internal AnimationPresentationDiagnosticsInterest Interest
        {
            get
            {
                RequireAlive();
                return m_Interest;
            }
        }

        internal bool HasInterest =>
            Interest != AnimationPresentationDiagnosticsInterest.None;

        internal ulong NoInterestSkipCount
        {
            get
            {
                RequireAlive();
                return m_NoInterestSkipCount;
            }
        }

        internal bool HasPendingFrame
        {
            get
            {
                RequireAlive();
                return m_PendingPageIndex >= 0;
            }
        }

        internal void RecordNoInterestSkip()
        {
            RequireAlive();
            if (m_NoInterestSkipCount != ulong.MaxValue)
                m_NoInterestSkipCount++;
        }

        internal void BeginFrame(
            in CharacterPoseGraphNativeBinding frame,
            in AnimationFinalPoseNativeReadBinding finalRead,
            IReadOnlyList<AnimationBlendStackRuntime> stacks,
            IReadOnlyList<CharacterAnimationTransitionRouteRuntime> routes,
            IReadOnlyList<CharacterPoseStateMachineRuntime> stateMachines,
            PoseInertializationNativeProgram inertializations,
            PhysicalPoseSourceRegistry physicalSources,
            IReadOnlyList<RootOrientationWarpRuntime> rootOrientationWarps,
            CharacterLinkedPoseRuntimeSession linkedPose,
            in CharacterPredictiveFootPlacementDiagnostics predictiveFootPlacement,
            AnimationPresentationDiagnosticsInterest interest)
        {
            RequireAlive();
            RequireValidFrameInterest(interest);
            frame.RequireValid();
            if (frame.CompletionIdentity != finalRead.CompletionIdentity || stacks == null ||
                routes == null || routes.Count != stacks.Count || stateMachines == null ||
                inertializations == null || physicalSources == null || rootOrientationWarps == null ||
                linkedPose == null)
                throw new ArgumentException("Animation runtime diagnostics frame inputs are inconsistent.");
            if (m_PendingPageIndex >= 0)
                throw new InvalidOperationException("Animation runtime diagnostics has an unpublished frame.");

            int pageIndex = m_ActivePageIndex == 0 ? 1 : 0;
            Page page = m_Pages[pageIndex];
            page.Lease.Invalidate();
            page.ClearCounts();
            page.CompletionIdentity = frame.CompletionIdentity;
            page.Interest = interest;

            if (RequiresBasicState(interest))
            {
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
                CopyAnimationSlots(page, routes);
                CopyPoseStateMachines(page, stateMachines, m_NativeProgram);
                CopyRootOrientationWarps(page, rootOrientationWarps);
                CopyInertializations(page, inertializations);
                CopySlotContributions(page, in frame, physicalSources);
                CopyFootIk(page, in predictiveFootPlacement);
            }
            if (RequiresOperationDetail(interest))
                CopyOperations(page, in frame, physicalSources);
            CopyLinkedPose(page, in frame, linkedPose);
            if ((interest & AnimationPresentationDiagnosticsInterest.PoseWatch) != 0)
                CopyPoseWatches(page, in frame, physicalSources, in predictiveFootPlacement);
            CopyFinalSummary(page, in finalRead);
            if (RequiresFinalPoseDetail(interest))
                CopyFinalDetail(page, in finalRead, physicalSources);
            m_PendingPageIndex = pageIndex;
            m_PendingCompletionIdentity = frame.CompletionIdentity;
        }

        void CopyLinkedPose(
            Page page,
            in CharacterPoseGraphNativeBinding frame,
            CharacterLinkedPoseRuntimeSession linkedPose)
        {
            if (linkedPose.GroupCount != page.LinkedPoseGroups.Length ||
                m_Program.LinkedPoseCalls.Count != page.LinkedPoseEntries.Length)
            {
                throw new InvalidOperationException("Linked Pose diagnostics layout is inconsistent.");
            }
            for (int groupIndex = 0; groupIndex < linkedPose.GroupCount; groupIndex++)
                page.LinkedPoseGroups[groupIndex] = linkedPose.CreateCommittedSnapshot(groupIndex);
            page.LinkedPoseGroupCount = linkedPose.GroupCount;

            for (int callIndex = 0; callIndex < m_Program.LinkedPoseCalls.Count; callIndex++)
            {
                CharacterLinkedPoseCallPlanDescriptor call = m_Program.LinkedPoseCalls[callIndex];
                CharacterLinkedPoseRuntimeGroupSnapshot group = RequireLinkedPoseGroup(page, call.GroupId);
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                    RequireLinkedPoseFragment(call, group.ImplementationId);
                CharacterPresentationPoseOperation callOperation = RequireLinkedPoseCallOperation(call.Index);
                ulong completionIdentity = frame.FrameCacheCompletedAt[callOperation.Index];
                bool completed = completionIdentity == frame.CompletionIdentity;
                for (int operationIndex = fragment.OperationStart;
                     completed && operationIndex < fragment.OperationStart + fragment.OperationCount;
                     operationIndex++)
                {
                    completed = frame.FrameCacheCompletedAt[operationIndex] == frame.CompletionIdentity;
                }

                CharacterFullBodyIkGoalSetAvailability goalAvailability =
                    CharacterFullBodyIkGoalSetAvailability.Invalid;
                int goalCount = 0;
                string goalRigId = string.Empty;
                string goalRigRevision = string.Empty;
                ulong goalCompletionIdentity = 0;
                int goalSetIndex = callOperation.OutputFullBodyIkGoalSetValueIndex;
                if ((uint)goalSetIndex < (uint)m_NativeProgram.FullBodyIkGoalSets.Length)
                {
                    CharacterFullBodyIkGoalSetHeader header = m_NativeProgram.FullBodyIkGoalSets[goalSetIndex];
                    if (header.IsValid)
                    {
                        goalAvailability = header.Availability;
                        goalCount = header.GoalCount;
                        goalRigId = header.RigId.ToString();
                        goalRigRevision = header.RigRevision.ToString();
                        goalCompletionIdentity = header.CompletionIdentity;
                    }
                }
                page.LinkedPoseEntries[callIndex] = new AnimationLinkedPoseEntryRuntimeSnapshot(
                    call.GroupId,
                    call.InterfaceId,
                    call.InterfaceSignature,
                    call.EntryId,
                    call.NodeId,
                    group.ImplementationId,
                    group.Generation,
                    group.StateReset,
                    fragment.Index,
                    fragment.OperationStart,
                    fragment.OperationCount,
                    fragment.StageStart,
                    fragment.StageCount,
                    fragment.SourceIndices.Count,
                    completionIdentity,
                    completed,
                    goalAvailability,
                    goalCount,
                    goalRigId,
                    goalRigRevision,
                    goalCompletionIdentity);
            }
            page.LinkedPoseEntryCount = m_Program.LinkedPoseCalls.Count;
        }

        static CharacterLinkedPoseRuntimeGroupSnapshot RequireLinkedPoseGroup(
            Page page,
            LinkedPoseGroupId groupId)
        {
            for (int i = 0; i < page.LinkedPoseGroupCount; i++)
            {
                CharacterLinkedPoseRuntimeGroupSnapshot group = page.LinkedPoseGroups[i];
                if (group.GroupId == groupId)
                    return group;
            }
            throw new InvalidOperationException($"Linked Pose diagnostics Group '{groupId}' is absent.");
        }

        CharacterLinkedPoseEntryFragmentPlanDescriptor RequireLinkedPoseFragment(
            CharacterLinkedPoseCallPlanDescriptor call,
            LinkedPoseImplementationId implementationId)
        {
            for (int i = 0; i < call.FragmentIndices.Count; i++)
            {
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                    m_Program.LinkedPoseFragments[call.FragmentIndices[i]];
                if (fragment.ImplementationId == implementationId)
                    return fragment;
            }
            throw new InvalidOperationException(
                $"Linked Pose Call '{call.NodeId}' has no fragment for Implementation '{implementationId}'.");
        }

        CharacterPresentationPoseOperation RequireLinkedPoseCallOperation(int callIndex)
        {
            for (int i = 0; i < m_Program.Operations.Count; i++)
            {
                CharacterPresentationPoseOperation operation = m_Program.Operations[i];
                if (operation.Code == CharacterPoseOperationCode.LinkedPoseCall &&
                    operation.LinkedPoseCallIndex == callIndex)
                {
                    return operation;
                }
            }
            throw new InvalidOperationException($"Linked Pose Call #{callIndex} has no root operation.");
        }

        internal void DiscardPendingFrame()
        {
            RequireAlive();
            if (m_PendingPageIndex < 0)
                return;
            Page page = m_Pages[m_PendingPageIndex];
            page.Lease.Invalidate();
            page.ClearCounts();
            m_PendingPageIndex = -1;
            m_PendingCompletionIdentity = 0;
        }

        void CopyAnimationSlots(
            Page page,
            IReadOnlyList<CharacterAnimationTransitionRouteRuntime> routes)
        {
            Array.Clear(page.AnimationSlots, 0, page.AnimationSlots.Length);
            int count = 0;
            for (int routeIndex = 0; routeIndex < routes.Count; routeIndex++)
            {
                CharacterAnimationTransitionRouteRuntime route = routes[routeIndex];
                if (!route.IsAnimationSlot)
                    continue;
                int slotIndex = route.AnimationSlotIndex;
                if ((uint)slotIndex >= (uint)page.AnimationSlots.Length ||
                    page.Stacks[routeIndex].PoseNodeId != route.NodeId)
                {
                    throw new InvalidOperationException("Animation Slot diagnostics layout is inconsistent.");
                }
                page.AnimationSlots[slotIndex] =
                    route.CreateSlotSnapshot(in page.Stacks[routeIndex]);
                count++;
            }
            if (count != page.AnimationSlots.Length)
                throw new InvalidOperationException("Animation Slot diagnostics coverage is incomplete.");
            page.AnimationSlotCount = count;
        }

        static void CopyPoseStateMachines(
            Page page,
            IReadOnlyList<CharacterPoseStateMachineRuntime> stateMachines,
            CharacterPoseGraphNativeProgram nativeProgram)
        {
            if (stateMachines.Count != page.PoseStateMachines.Length)
                throw new InvalidOperationException("Pose StateMachine diagnostics coverage is incomplete.");
            for (int i = 0; i < stateMachines.Count; i++)
            {
                page.PoseStateMachines[i] = stateMachines[i].CreateSnapshot();
                for (int bone = 0; bone < page.PoseBoneCount; bone++)
                {
                    page.PoseStateMachineBoneWeights[
                        i * page.PoseBoneCount + bone] =
                        nativeProgram.GetStateMachineBoneWeight(i, bone);
                }
            }
            page.PoseStateMachineCount = stateMachines.Count;
        }

        static void CopyRootOrientationWarps(
            Page page,
            IReadOnlyList<RootOrientationWarpRuntime> rootOrientationWarps)
        {
            if (rootOrientationWarps.Count != page.RootOrientationWarps.Length)
                throw new InvalidOperationException("Root Orientation Warp diagnostics coverage is incomplete.");
            for (int i = 0; i < rootOrientationWarps.Count; i++)
                page.RootOrientationWarps[i] = rootOrientationWarps[i].CreateDiagnosticsSnapshot();
            page.RootOrientationWarpCount = rootOrientationWarps.Count;
        }

        internal AnimationPresentationRuntimeSnapshot Publish(
            AnimationReleasedPoseSourceSnapshot[] releases,
            int releaseCount,
            IReadOnlyList<AnimationBlendSpacePlayerRuntime> blendSpacePlayers)
        {
            RequireAlive();
            if (m_PendingPageIndex < 0 || m_PendingCompletionIdentity == 0)
                throw new InvalidOperationException("Animation runtime diagnostics has no completed native frame.");
            Page page = m_Pages[m_PendingPageIndex];
            int blendSpacePlayerCount = 0;
            int blendSpaceSampleCount = 0;
            if (RequiresBasicState(page.Interest))
            {
                if (releases == null || releaseCount < 0 || releaseCount > releases.Length || releaseCount > page.Releases.Length)
                    throw new InvalidOperationException("Animation runtime diagnostics fixed capacity was exceeded.");
                Array.Copy(releases, 0, page.Releases, 0, releaseCount);
                Array.Clear(page.Releases, releaseCount, page.Releases.Length - releaseCount);
                page.ReleaseCount = releaseCount;
                int runtimeBlendSpacePlayerCount =
                    blendSpacePlayers?.Count ?? 0;
                if (runtimeBlendSpacePlayerCount >
                    page.BlendSpacePlayers.Length)
                {
                    throw new InvalidOperationException(
                        "Animation Blend Space diagnostics fixed capacity was exceeded.");
                }
                for (int i = 0; i < runtimeBlendSpacePlayerCount; i++)
                {
                    AnimationBlendSpacePlayerRuntime runtime =
                        blendSpacePlayers[i];
                    if (!runtime.IsRelevant || !runtime.HasCompletedFrame)
                        continue;
                    AnimationBlendSpacePlayerRuntimeSnapshot player =
                        runtime.CreateDiagnosticsSnapshot(
                            page.BlendSpaceSamples,
                            ref blendSpaceSampleCount);
                    for (int operationIndex = 0; operationIndex < page.OperationCount; operationIndex++)
                    {
                        AnimationPoseOperationSnapshot operation = page.Operations[operationIndex];
                        if (operation.Code != CharacterPoseOperationCode.BlendSpacePlayer ||
                            !operation.NodeId.Equals(player.NodeId))
                            continue;
                        player = player.WithPoseResult(operation.Availability, operation.InvalidReason);
                        break;
                    }
                    page.BlendSpacePlayers[blendSpacePlayerCount++] = player;
                }
            }
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

        internal AnimationPresentationDiagnosticsInterest ResolveFrameInterest(
            AnimationPresentationDiagnosticsInterest transientInterest)
        {
            RequireAlive();
            if ((transientInterest & ~ExplicitOwnerMask) != 0)
                throw new ArgumentOutOfRangeException(nameof(transientInterest));
            return m_Interest | transientInterest;
        }

        internal void SetDiagnosticsInterest(
            Guid ownerId,
            AnimationPresentationDiagnosticsInterest interest)
        {
            RequireAlive();
            RequireInterestMutationAvailable();
            if (ownerId == Guid.Empty)
                throw new ArgumentException("Animation diagnostics owner identity is missing.", nameof(ownerId));
            if ((interest & ~ExplicitOwnerMask) != 0)
                throw new ArgumentOutOfRangeException(nameof(interest));
            if (interest == AnimationPresentationDiagnosticsInterest.None)
            {
                RemoveDiagnosticsInterest(ownerId);
                return;
            }
            int ownerIndex = FindOwner(ownerId);
            if (ownerIndex < 0)
            {
                ownerIndex = RequireFreeOwner();
                m_InterestOwnerIds[ownerIndex] = ownerId;
            }
            if (m_OwnerInterests[ownerIndex] == interest)
                return;
            m_OwnerInterests[ownerIndex] = interest;
            RebuildInterest(true);
        }

        internal void RemoveDiagnosticsInterest(Guid ownerId)
        {
            if (m_Disposed || ownerId == Guid.Empty)
                return;
            RequireInterestMutationAvailable();
            int ownerIndex = FindOwner(ownerId);
            if (ownerIndex < 0 || m_OwnerInterests[ownerIndex] == AnimationPresentationDiagnosticsInterest.None)
                return;
            m_OwnerInterests[ownerIndex] = AnimationPresentationDiagnosticsInterest.None;
            ReleaseOwnerIfEmpty(ownerIndex);
            RebuildInterest(true);
        }

        internal void SetPoseWatchInterests(Guid ownerId, IReadOnlyList<AnimationPoseWatchIdentity> interests)
        {
            RequireAlive();
            RequireInterestMutationAvailable();
            if (ownerId == Guid.Empty)
                throw new ArgumentException("Pose Watch owner identity is missing.", nameof(ownerId));
            int count = interests?.Count ?? 0;
            if (count > AnimationPoseWatchCapacity.PerWindow)
                throw new InvalidOperationException($"Pose Watch window capacity exceeded: {count}/{AnimationPoseWatchCapacity.PerWindow}.");
            if (count == 0)
            {
                RemovePoseWatchInterests(ownerId);
                return;
            }
            for (int i = 0; i < count; i++)
            {
                AnimationPoseWatchIdentity interest = interests[i];
                if (!interest.IsValid)
                    throw new ArgumentException("Pose Watch interests contain an invalid identity.", nameof(interests));
                for (int duplicateIndex = 0; duplicateIndex < i; duplicateIndex++)
                {
                    if (interest.Equals(interests[duplicateIndex]))
                        throw new ArgumentException("Pose Watch interests contain a duplicate identity.", nameof(interests));
                }
            }
            int ownerIndex = FindOwner(ownerId);
            if (ownerIndex < 0)
                ownerIndex = RequireFreeOwner();
            int mergedCount = BuildMergedPoseWatchScratch(ownerIndex, interests, count);
            int ownerOffset = checked(ownerIndex * AnimationPoseWatchCapacity.PerWindow);
            Array.Clear(m_OwnerPoseWatches, ownerOffset, AnimationPoseWatchCapacity.PerWindow);
            for (int i = 0; i < count; i++)
                m_OwnerPoseWatches[ownerOffset + i] = interests[i];
            m_InterestOwnerIds[ownerIndex] = ownerId;
            m_OwnerPoseWatchCounts[ownerIndex] = count;
            CommitMergedPoseWatchScratch(mergedCount);
            RebuildInterest(true);
        }

        internal void RemovePoseWatchInterests(Guid ownerId)
        {
            if (m_Disposed || ownerId == Guid.Empty)
                return;
            RequireInterestMutationAvailable();
            int ownerIndex = FindOwner(ownerId);
            if (ownerIndex < 0 || m_OwnerPoseWatchCounts[ownerIndex] == 0)
                return;
            int ownerOffset = checked(ownerIndex * AnimationPoseWatchCapacity.PerWindow);
            Array.Clear(m_OwnerPoseWatches, ownerOffset, AnimationPoseWatchCapacity.PerWindow);
            m_OwnerPoseWatchCounts[ownerIndex] = 0;
            ReleaseOwnerIfEmpty(ownerIndex);
            int mergedCount = BuildMergedPoseWatchScratch(-1, null, 0);
            CommitMergedPoseWatchScratch(mergedCount);
            RebuildInterest(true);
        }

        int BuildMergedPoseWatchScratch(
            int replacementOwnerIndex,
            IReadOnlyList<AnimationPoseWatchIdentity> replacement,
            int replacementCount)
        {
            Array.Clear(m_PoseWatchMergeScratch, 0, m_PoseWatchMergeScratch.Length);
            int mergedCount = 0;
            for (int ownerIndex = 0; ownerIndex < InterestOwnerCapacity; ownerIndex++)
            {
                int count = ownerIndex == replacementOwnerIndex
                    ? replacementCount
                    : m_OwnerPoseWatchCounts[ownerIndex];
                int ownerOffset = checked(ownerIndex * AnimationPoseWatchCapacity.PerWindow);
                for (int watchIndex = 0; watchIndex < count; watchIndex++)
                {
                    AnimationPoseWatchIdentity candidate = ownerIndex == replacementOwnerIndex
                        ? replacement[watchIndex]
                        : m_OwnerPoseWatches[ownerOffset + watchIndex];
                    bool duplicate = false;
                    for (int mergedIndex = 0; mergedIndex < mergedCount; mergedIndex++)
                    {
                        if (!m_PoseWatchMergeScratch[mergedIndex].Equals(candidate))
                            continue;
                        duplicate = true;
                        break;
                    }
                    if (duplicate)
                        continue;
                    if (mergedCount >= AnimationPoseWatchCapacity.PerTarget)
                        throw new InvalidOperationException($"Pose Watch target capacity exceeded: more than {AnimationPoseWatchCapacity.PerTarget} unique interests.");
                    int insertionIndex = mergedCount;
                    while (insertionIndex > 0 && ComparePoseWatch(
                               candidate,
                               m_PoseWatchMergeScratch[insertionIndex - 1]) < 0)
                    {
                        m_PoseWatchMergeScratch[insertionIndex] =
                            m_PoseWatchMergeScratch[insertionIndex - 1];
                        insertionIndex--;
                    }
                    m_PoseWatchMergeScratch[insertionIndex] = candidate;
                    mergedCount++;
                }
            }
            return mergedCount;
        }

        void CommitMergedPoseWatchScratch(int count)
        {
            Array.Clear(m_MergedPoseWatchInterests, 0, m_MergedPoseWatchInterests.Length);
            Array.Copy(m_PoseWatchMergeScratch, m_MergedPoseWatchInterests, count);
            m_MergedPoseWatchInterestCount = count;
        }

        static int ComparePoseWatch(
            AnimationPoseWatchIdentity left,
            AnimationPoseWatchIdentity right)
        {
            int comparison = string.Compare(left.GraphId, right.GraphId, StringComparison.Ordinal);
            if (comparison != 0)
                return comparison;
            comparison = string.Compare(left.GraphRevision, right.GraphRevision, StringComparison.Ordinal);
            if (comparison != 0)
                return comparison;
            comparison = string.Compare(left.NodeId.Value, right.NodeId.Value, StringComparison.Ordinal);
            return comparison != 0
                ? comparison
                : string.Compare(left.CallSite, right.CallSite, StringComparison.Ordinal);
        }

        int FindOwner(Guid ownerId)
        {
            for (int i = 0; i < m_InterestOwnerIds.Length; i++)
            {
                if (m_InterestOwnerIds[i] == ownerId)
                    return i;
            }
            return -1;
        }

        int RequireFreeOwner()
        {
            for (int i = 0; i < m_InterestOwnerIds.Length; i++)
            {
                if (m_InterestOwnerIds[i] == Guid.Empty)
                    return i;
            }
            throw new InvalidOperationException($"Animation diagnostics owner capacity exceeded: {InterestOwnerCapacity}.");
        }

        void ReleaseOwnerIfEmpty(int ownerIndex)
        {
            if (m_OwnerInterests[ownerIndex] == AnimationPresentationDiagnosticsInterest.None &&
                m_OwnerPoseWatchCounts[ownerIndex] == 0)
                m_InterestOwnerIds[ownerIndex] = Guid.Empty;
        }

        void RebuildInterest(bool invalidateCurrent)
        {
            AnimationPresentationDiagnosticsInterest interest = AnimationPresentationDiagnosticsInterest.None;
            for (int i = 0; i < InterestOwnerCapacity; i++)
            {
                interest |= m_OwnerInterests[i];
                if (m_OwnerPoseWatchCounts[i] > 0)
                    interest |= AnimationPresentationDiagnosticsInterest.PoseWatch;
            }
            bool changed = interest != m_Interest;
            m_Interest = interest;
            if (invalidateCurrent && (changed || m_ActivePageIndex >= 0))
                Invalidate();
        }

        void RequireInterestMutationAvailable()
        {
            if (m_PendingPageIndex >= 0)
                throw new InvalidOperationException("Animation diagnostics interest cannot change while a committed frame copy is pending publication.");
        }

        void CopyInertializations(Page page, PoseInertializationNativeProgram program)
        {
            if (program.SlotNodeOffset != m_Program.Inertializations.Count ||
                program.Nodes.Length !=
                checked(m_Program.Inertializations.Count + m_Program.AnimationSlots.Count) ||
                program.BoneCount != page.BoneIds.Length)
                throw new InvalidOperationException("Pose Inertialization diagnostics layout is inconsistent.");
            for (int nodeIndex = 0; nodeIndex < m_Program.Inertializations.Count; nodeIndex++)
            {
                CharacterPresentationInertializationDescriptor descriptor =
                    m_Program.Inertializations[nodeIndex];
                PoseInertializationNativeState state = program.States[nodeIndex];
                PoseInertializationMode mode = default;
                float duration = 0f;
                int sourceEndpointIndex = -1;
                int targetEndpointIndex = -1;
                int curveIndex = -1;
                int profileIndex = -1;
                if ((uint)state.ActiveRuleIndex < (uint)program.Rules.Length &&
                    state.RuntimeState != 0 &&
                    state.RuntimeState != PoseInertializationRuntimeState.Reset &&
                    state.RuntimeState != PoseInertializationRuntimeState.Invalid)
                {
                    PoseInertializationNativeRule rule = program.Rules[state.ActiveRuleIndex];
                    mode = rule.Mode;
                    duration = state.ActiveDurationSeconds;
                    sourceEndpointIndex = rule.SourceEndpointIndex;
                    targetEndpointIndex = rule.TargetEndpointIndex;
                    PoseInertializationNativeNode node = program.Nodes[nodeIndex];
                    int descriptorRuleIndex = state.ActiveRuleIndex - node.RuleOffset;
                    if ((uint)descriptorRuleIndex >= (uint)descriptor.Rules.Count)
                    {
                        throw new InvalidOperationException(
                            "Pose Inertialization diagnostic rule layout is inconsistent.");
                    }
                    CharacterPresentationInertializationRuleDescriptor descriptorRule =
                        descriptor.Rules[descriptorRuleIndex];
                    curveIndex = descriptorRule.CurveIndex;
                    profileIndex = descriptorRule.ProfileIndex;
                }
                page.Inertializations[nodeIndex] = new PoseInertializationSnapshot(
                    descriptor.NodeId,
                    descriptor.TemporalOwnerKind,
                    descriptor.InputOwnerNodeId,
                    descriptor.InputOwnerIndex,
                    state.RuntimeState,
                    state.LastEventIdentity,
                    state.LastReason,
                    state.LastResetReason,
                    state.LastResetSequence,
                    descriptor.PolicyId,
                    descriptor.PolicyRevision,
                    sourceEndpointIndex,
                    targetEndpointIndex,
                    curveIndex,
                    profileIndex,
                    state.PreviousEndpoint.IsValid
                        ? state.PreviousEndpoint.ToManaged()
                        : default,
                    state.CurrentEndpoint.IsValid
                        ? state.CurrentEndpoint.ToManaged()
                        : default,
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
            page.InertializationCount = m_Program.Inertializations.Count;
        }

        void CopySlotContributions(
            Page page,
            in CharacterPoseGraphNativeBinding frame,
            PhysicalPoseSourceRegistry physicalSources)
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
            PhysicalPoseSourceRegistry physicalSources)
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
            PhysicalPoseSourceRegistry physicalSources,
            in CharacterPredictiveFootPlacementDiagnostics predictiveFootPlacement)
        {
            int boneCount = frame.Layout.BoneCount;
            int stride = frame.Layout.PoseValueContributionStride;
            for (int watchIndex = 0; watchIndex < m_MergedPoseWatchInterestCount; watchIndex++)
            {
                AnimationPoseWatchIdentity identity = m_MergedPoseWatchInterests[watchIndex];
                int poseOffset = watchIndex * boneCount;
                int contributionOffset = watchIndex * stride;
                int goalOffset = watchIndex * CharacterFullBodyIkGoalSetHeader.MaximumGoalCount;
                int effectorOffset = watchIndex * CharacterFullBodyIkGoalSetHeader.MaximumGoalCount;
                int limbOffset = watchIndex * 4;
                Array.Clear(page.PoseWatchLocalPoses, poseOffset, boneCount);
                Array.Clear(page.PoseWatchComponentPoses, poseOffset, boneCount);
                Array.Clear(page.PoseWatchContributions, contributionOffset, stride);
                page.PoseWatchPredictiveFootPlacements[watchIndex] = default;
                page.PoseWatchFullBodyIkSolvers[watchIndex] = default;
                Array.Clear(
                    page.PoseWatchFullBodyIkGoals,
                    goalOffset,
                    CharacterFullBodyIkGoalSetHeader.MaximumGoalCount);
                Array.Clear(
                    page.PoseWatchFullBodyIkEffectors,
                    effectorOffset,
                    CharacterFullBodyIkGoalSetHeader.MaximumGoalCount);
                Array.Clear(page.PoseWatchFullBodyIkLimbs, limbOffset, 4);
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
                if (!MatchesPoseWatchGraphRevision(identity, operation))
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
                AnimationLinkedPoseEntryRuntimeSnapshot linkedPoseEntry =
                    FindLinkedPoseEntry(page, operation);
                ulong completion = frame.FrameCacheCompletedAt[operation.Index];
                if (operation.OutputFullBodyIkGoalSetValueIndex >= 0)
                {
                    int goalSetIndex = operation.OutputFullBodyIkGoalSetValueIndex;
                    AnimationFullBodyIkGoalSetSnapshot goalSet = default;
                    AnimationPoseWatchAvailability goalAvailability =
                        completion != frame.CompletionIdentity
                            ? AnimationPoseWatchAvailability.NotCompleted
                            : AnimationPoseWatchAvailability.Invalid;
                    if ((uint)goalSetIndex < (uint)m_NativeProgram.FullBodyIkGoalSets.Length)
                    {
                        CharacterFullBodyIkGoalSetHeader header =
                            m_NativeProgram.FullBodyIkGoalSets[goalSetIndex];
                        if (header.IsValid &&
                            header.CompletionIdentity == completion &&
                            header.ProducerOperationIndex == operation.Index &&
                            header.GoalOffset <=
                                m_NativeProgram.FullBodyIkGoals.Length - header.GoalCount)
                        {
                            goalSet = new AnimationFullBodyIkGoalSetSnapshot(
                                in header,
                                goalOffset);
                            if (header.Availability == CharacterFullBodyIkGoalSetAvailability.Ready)
                            {
                                for (int goalIndex = 0; goalIndex < header.GoalCount; goalIndex++)
                                {
                                    page.PoseWatchFullBodyIkGoals[goalOffset + goalIndex] =
                                        m_NativeProgram.FullBodyIkGoals[header.GoalOffset + goalIndex];
                                }
                                goalAvailability = AnimationPoseWatchAvailability.Targets;
                            }
                            else
                            {
                                goalAvailability = AnimationPoseWatchAvailability.WorldContextUnavailable;
                            }
                        }
                    }
                    if (operation.Code == CharacterPoseOperationCode.PredictiveFootPlacement &&
                        predictiveFootPlacement.IsCompleted &&
                        predictiveFootPlacement.CompletionIdentity == completion &&
                        predictiveFootPlacement.FrameSequence == goalSet.FrameSequence)
                    {
                        page.PoseWatchPredictiveFootPlacements[watchIndex] = predictiveFootPlacement;
                    }
                    page.PoseWatches[watchIndex] = new AnimationPoseWatchSnapshot(
                        identity,
                        operation.Index,
                        operation.Code,
                        FindStageIndex(operation.Index),
                        operation.ExecutionDomain,
                        CharacterPoseSpace.None,
                        linkedPoseEntry,
                        goalSet,
                        poseOffset,
                        boneCount,
                        contributionOffset,
                        0,
                        goalAvailability,
                        goalAvailability == AnimationPoseWatchAvailability.Invalid
                            ? frame.PoseGraphInvalidReason[0]
                            : AnimationPoseNativeInvalidReason.None,
                        operation.Weight,
                        0,
                        completion);
                    continue;
                }
                int valueIndex = operation.OutputValueIndex;
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
                    for (int boneIndex = 0; boneIndex < boneCount; boneIndex++)
                    {
                        AnimationLocalBonePose stored = page.PoseWatchLocalPoses[poseOffset + boneIndex];
                        CharacterComponentBonePose component;
                        if (operation.OutputPoseSpace == CharacterPoseSpace.Component)
                        {
                            component = new CharacterComponentBonePose(stored.Position, stored.Rotation, stored.Scale);
                        }
                        else if (!CharacterPoseConstraintMath.TryCreateComponent(
                                     stored,
                                     page.PoseBones[boneIndex].ParentPoseBoneIndex,
                                     page.PoseWatchComponentPoses,
                                     poseOffset,
                                     out component))
                        {
                            throw new InvalidOperationException(
                                $"Pose Watch operation #{operation.Index} component Bone #{boneIndex} is invalid.");
                        }
                        page.PoseWatchComponentPoses[poseOffset + boneIndex] = component;
                    }
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
                if (operation.Code == CharacterPoseOperationCode.FullBodyIK &&
                    (uint)operation.FullBodyIkIndex < (uint)m_FullBodyIkSolvers.Length)
                {
                    CharacterFinalIkFullBodySolver solver = m_FullBodyIkSolvers[operation.FullBodyIkIndex];
                    CharacterFullBodyIkSolverDiagnostics diagnostics = solver.Diagnostics;
                    if (diagnostics.IsCompleted &&
                        diagnostics.InputCompletionIdentity == completion)
                    {
                        page.PoseWatchFullBodyIkSolvers[watchIndex] = diagnostics;
                        for (int effectorIndex = 0; effectorIndex < solver.DiagnosticEffectorCount; effectorIndex++)
                        {
                            page.PoseWatchFullBodyIkEffectors[effectorOffset + effectorIndex] =
                                solver.GetDiagnosticEffector(effectorIndex);
                        }
                        for (int limbIndex = 0; limbIndex < solver.DiagnosticLimbCount; limbIndex++)
                        {
                            page.PoseWatchFullBodyIkLimbs[limbOffset + limbIndex] =
                                solver.GetDiagnosticLimb(limbIndex);
                        }
                    }
                }
                page.PoseWatches[watchIndex] = new AnimationPoseWatchSnapshot(
                    identity,
                    operation.Index,
                    operation.Code,
                    FindStageIndex(operation.Index),
                    operation.ExecutionDomain,
                    operation.OutputPoseSpace,
                    linkedPoseEntry,
                    default,
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
            page.PoseWatchCount = m_MergedPoseWatchInterestCount;
        }

        void CopyFootIk(
            Page page,
            in CharacterPredictiveFootPlacementDiagnostics predictiveFootPlacement)
        {
            if (!predictiveFootPlacement.IsCompleted ||
                predictiveFootPlacement.CompletionIdentity != page.CompletionIdentity)
                return;
            CharacterFullBodyIkSolverDiagnostics solverDiagnostics = default;
            CharacterFullBodyIkEffectorDiagnostics leftFoot = default;
            CharacterFullBodyIkEffectorDiagnostics rightFoot = default;
            for (int solverIndex = 0; solverIndex < m_FullBodyIkSolvers.Length; solverIndex++)
            {
                CharacterFinalIkFullBodySolver solver = m_FullBodyIkSolvers[solverIndex];
                CharacterFullBodyIkSolverDiagnostics candidate = solver.Diagnostics;
                if (!candidate.IsCompleted ||
                    candidate.InputCompletionIdentity != page.CompletionIdentity ||
                    candidate.FrameSequence != predictiveFootPlacement.FrameSequence)
                {
                    continue;
                }
                bool containsFoot = false;
                for (int effectorIndex = 0; effectorIndex < solver.DiagnosticEffectorCount; effectorIndex++)
                {
                    CharacterFullBodyIkEffectorDiagnostics effector = solver.GetDiagnosticEffector(effectorIndex);
                    if (effector.Slot == CharacterFullBodyIkEffectorSlot.LeftFoot)
                    {
                        leftFoot = effector;
                        containsFoot = true;
                    }
                    else if (effector.Slot == CharacterFullBodyIkEffectorSlot.RightFoot)
                    {
                        rightFoot = effector;
                        containsFoot = true;
                    }
                }
                if (!containsFoot)
                    continue;
                solverDiagnostics = candidate;
                break;
            }
            page.FootIk = new AnimationFootIkRuntimeSnapshot(
                predictiveFootPlacement,
                solverDiagnostics,
                leftFoot,
                rightFoot);
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
                if ((operation.OutputValueIndex < 0 &&
                     operation.OutputFullBodyIkGoalSetValueIndex < 0) ||
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

        bool MatchesPoseWatchGraphRevision(
            AnimationPoseWatchIdentity identity,
            CharacterPresentationPoseOperation operation)
        {
            if (operation.LinkedPoseFragmentIndex >= 0)
            {
                CharacterLinkedPoseEntryFragmentPlanDescriptor fragment =
                    m_Program.LinkedPoseFragments[operation.LinkedPoseFragmentIndex];
                return string.Equals(identity.GraphRevision, fragment.GraphRevision, StringComparison.Ordinal);
            }
            return string.Equals(identity.GraphRevision, m_Program.ContentRevision, StringComparison.Ordinal);
        }

        static AnimationLinkedPoseEntryRuntimeSnapshot FindLinkedPoseEntry(
            Page page,
            CharacterPresentationPoseOperation operation)
        {
            if ((uint)operation.LinkedPoseCallIndex < (uint)page.LinkedPoseEntryCount)
                return page.LinkedPoseEntries[operation.LinkedPoseCallIndex];
            for (int i = 0; i < page.LinkedPoseEntryCount; i++)
            {
                AnimationLinkedPoseEntryRuntimeSnapshot entry = page.LinkedPoseEntries[i];
                if (operation.Index >= entry.OperationStart &&
                    operation.Index < entry.OperationStart + entry.OperationCount)
                {
                    return entry;
                }
            }
            return default;
        }

        int FindStageIndex(int operationIndex)
        {
            for (int i = 0; i < m_Program.Stages.Count; i++)
            {
                CharacterPresentationPoseStage stage = m_Program.Stages[i];
                if (operationIndex >= stage.OperationStart &&
                    operationIndex < stage.OperationStart + stage.OperationCount)
                {
                    return stage.Index;
                }
            }
            throw new InvalidOperationException(
                $"Pose Watch operation #{operationIndex} is not owned by a compiled stage.");
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
                default,
                -1,
                default,
                CharacterPoseSpace.None,
                default,
                default,
                poseOffset,
                boneCount,
                contributionOffset,
                0,
                availability,
                invalidReason,
                0f,
                0,
                completionIdentity);

        static void CopyFinalSummary(
            Page page,
            in AnimationFinalPoseNativeReadBinding finalRead)
        {
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

        void CopyFinalDetail(
            Page page,
            in AnimationFinalPoseNativeReadBinding finalRead,
            PhysicalPoseSourceRegistry physicalSources)
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
        }

        AnimationPoseSourceContribution ConvertContribution(
            AnimationPrimitivePoseContribution primitive,
            PhysicalPoseSourceRegistry physicalSources)
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
                if (physicalSources.RequireSourceOwnerIndex(physical) != primitive.SourceOwnerIndex)
                    throw new InvalidOperationException("Animation diagnostic contribution producer identity is inconsistent.");
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

        static bool RequiresBasicState(AnimationPresentationDiagnosticsInterest interest) =>
            (interest & (AnimationPresentationDiagnosticsInterest.LiveState |
                         AnimationPresentationDiagnosticsInterest.Capture)) != 0;

        static bool RequiresOperationDetail(AnimationPresentationDiagnosticsInterest interest) =>
            (interest & (AnimationPresentationDiagnosticsInterest.Capture |
                         AnimationPresentationDiagnosticsInterest.OperationDetail)) != 0;

        static bool RequiresFinalPoseDetail(AnimationPresentationDiagnosticsInterest interest) =>
            (interest & (AnimationPresentationDiagnosticsInterest.Capture |
                         AnimationPresentationDiagnosticsInterest.FinalPoseDetail)) != 0;

        static void RequireValidFrameInterest(AnimationPresentationDiagnosticsInterest interest)
        {
            const AnimationPresentationDiagnosticsInterest all =
                ExplicitOwnerMask |
                AnimationPresentationDiagnosticsInterest.PoseWatch;
            if (interest == AnimationPresentationDiagnosticsInterest.None ||
                (interest & ~all) != 0)
                throw new ArgumentOutOfRangeException(nameof(interest));
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
                int releaseCapacity,
                int blendSpacePlayerCapacity,
                int blendSpaceSampleCapacity,
                int linkedPoseGroupCapacity)
            {
                Lease = new FinalAnimationPoseFramePageLease();
                Stacks = new AnimationBlendStackSnapshot[layout.PlayerCount];
                Inertializations = new PoseInertializationSnapshot[program.Inertializations.Count];
                Entries = new AnimationBlendStackEntrySnapshot[entryCapacity];
                Operations = new AnimationPoseOperationSnapshot[program.Operations.Count];
                Parameters = new AnimationPoseParameterSnapshot[program.Parameters.Count];
                BlendSpacePlayers = new AnimationBlendSpacePlayerRuntimeSnapshot[blendSpacePlayerCapacity];
                BlendSpaceSamples = new AnimationBlendSpaceSampleRuntimeSnapshot[blendSpaceSampleCapacity];
                SlotContributions = new AnimationPoseSourceContribution[layout.TotalPlayerContributionCapacity];
                OperationContributions = new AnimationPoseSourceContribution[
                    checked(program.Operations.Count * layout.PoseValueContributionStride)];
                FinalContributions = new AnimationPoseSourceContribution[layout.PoseValueContributionStride];
                Releases = new AnimationReleasedPoseSourceSnapshot[releaseCapacity];
                AnimationSlots = new AnimationSlotRuntimeSnapshot[program.AnimationSlots.Count];
                PoseStateMachines = new PoseStateMachineRuntimeSnapshot[program.StateMachines.Count];
                PoseStateMachineBoneWeights = new float[
                    checked(program.StateMachines.Count * layout.BoneCount)];
                RootOrientationWarps = new RootOrientationWarpRuntimeSnapshot[program.RootOrientationWarps.Count];
                LinkedPoseGroups = new CharacterLinkedPoseRuntimeGroupSnapshot[linkedPoseGroupCapacity];
                LinkedPoseEntries = new AnimationLinkedPoseEntryRuntimeSnapshot[program.LinkedPoseCalls.Count];
                PoseWatches = new AnimationPoseWatchSnapshot[AnimationPoseWatchCapacity.PerTarget];
                PoseWatchFullBodyIkGoals = new CharacterFullBodyIkGoal[
                    checked(AnimationPoseWatchCapacity.PerTarget * CharacterFullBodyIkGoalSetHeader.MaximumGoalCount)];
                PoseWatchPredictiveFootPlacements =
                    new CharacterPredictiveFootPlacementDiagnostics[AnimationPoseWatchCapacity.PerTarget];
                PoseWatchFullBodyIkSolvers =
                    new CharacterFullBodyIkSolverDiagnostics[AnimationPoseWatchCapacity.PerTarget];
                PoseWatchFullBodyIkEffectors = new CharacterFullBodyIkEffectorDiagnostics[
                    checked(AnimationPoseWatchCapacity.PerTarget * CharacterFullBodyIkGoalSetHeader.MaximumGoalCount)];
                PoseWatchFullBodyIkLimbs = new CharacterFullBodyIkLimbDiagnostics[
                    checked(AnimationPoseWatchCapacity.PerTarget * 4)];
                PoseWatchLocalPoses = new AnimationLocalBonePose[checked(AnimationPoseWatchCapacity.PerTarget * layout.BoneCount)];
                PoseWatchComponentPoses = new CharacterComponentBonePose[checked(AnimationPoseWatchCapacity.PerTarget * layout.BoneCount)];
                PoseWatchContributions = new AnimationPoseSourceContribution[
                    checked(AnimationPoseWatchCapacity.PerTarget * layout.PoseValueContributionStride)];
                BoneIds = new AnimationBoneId[layout.BoneCount];
                PoseBones = new AnimationPoseBoneSnapshot[layout.BoneCount];
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
                PhysicalBoneCount = rig.PhysicalBoneCount;
                VirtualBoneCount = rig.VirtualBoneCount;
                PoseBoneCount = rig.PoseBoneCount;
                for (int i = 0; i < BoneIds.Length; i++)
                {
                    BoneIds[i] = rig.GetPoseBoneId(i);
                    CharacterPoseBoneKind kind = rig.GetPoseBoneKind(i);
                    AnimationBoneId sourceBoneId = default;
                    AnimationBoneId targetBoneId = default;
                    if (kind == CharacterPoseBoneKind.Virtual)
                    {
                        CharacterAnimationVirtualBonePayload virtualBone =
                            rig.VirtualBones[i - rig.PhysicalBoneCount];
                        sourceBoneId = rig.PhysicalBones[virtualBone.SourcePhysicalBoneIndex].BoneId;
                        targetBoneId = rig.PhysicalBones[virtualBone.TargetPhysicalBoneIndex].BoneId;
                    }
                    PoseBones[i] = new AnimationPoseBoneSnapshot(
                        BoneIds[i],
                        kind,
                        rig.GetPoseParentIndex(i),
                        sourceBoneId,
                        targetBoneId);
                }
            }

            internal readonly FinalAnimationPoseFramePageLease Lease;
            internal readonly AnimationBlendStackSnapshot[] Stacks;
            internal readonly PoseInertializationSnapshot[] Inertializations;
            internal readonly AnimationBlendStackEntrySnapshot[] Entries;
            internal readonly AnimationPoseOperationSnapshot[] Operations;
            internal readonly AnimationPoseParameterSnapshot[] Parameters;
            internal readonly AnimationBlendSpacePlayerRuntimeSnapshot[] BlendSpacePlayers;
            internal readonly AnimationBlendSpaceSampleRuntimeSnapshot[] BlendSpaceSamples;
            internal readonly AnimationPoseSourceContribution[] SlotContributions;
            internal readonly AnimationPoseSourceContribution[] OperationContributions;
            internal readonly AnimationPoseSourceContribution[] FinalContributions;
            internal readonly AnimationReleasedPoseSourceSnapshot[] Releases;
            internal readonly AnimationSlotRuntimeSnapshot[] AnimationSlots;
            internal readonly PoseStateMachineRuntimeSnapshot[] PoseStateMachines;
            internal readonly float[] PoseStateMachineBoneWeights;
            internal readonly RootOrientationWarpRuntimeSnapshot[] RootOrientationWarps;
            internal readonly CharacterLinkedPoseRuntimeGroupSnapshot[] LinkedPoseGroups;
            internal readonly AnimationLinkedPoseEntryRuntimeSnapshot[] LinkedPoseEntries;
            internal readonly AnimationPoseWatchSnapshot[] PoseWatches;
            internal readonly CharacterFullBodyIkGoal[] PoseWatchFullBodyIkGoals;
            internal readonly CharacterPredictiveFootPlacementDiagnostics[] PoseWatchPredictiveFootPlacements;
            internal readonly CharacterFullBodyIkSolverDiagnostics[] PoseWatchFullBodyIkSolvers;
            internal readonly CharacterFullBodyIkEffectorDiagnostics[] PoseWatchFullBodyIkEffectors;
            internal readonly CharacterFullBodyIkLimbDiagnostics[] PoseWatchFullBodyIkLimbs;
            internal readonly AnimationLocalBonePose[] PoseWatchLocalPoses;
            internal readonly CharacterComponentBonePose[] PoseWatchComponentPoses;
            internal readonly AnimationPoseSourceContribution[] PoseWatchContributions;
            internal readonly AnimationBoneId[] BoneIds;
            internal readonly AnimationPoseBoneSnapshot[] PoseBones;
            internal readonly float[] EntryBoneWeights;
            internal readonly float[] StoredBoneWeights;
            internal readonly Vector3[] InertialPositionResiduals;
            internal readonly Vector3[] InertialRotationResiduals;
            internal readonly Vector3[] InertialScaleResiduals;
            internal readonly float[] InertialBoneEnvelopes;
            internal readonly float[] SlotContributionBoneWeights;
            internal readonly float[] OperationContributionBoneWeights;
            internal readonly float[] FinalContributionBoneWeights;
            internal readonly int PhysicalBoneCount;
            internal readonly int VirtualBoneCount;
            internal readonly int PoseBoneCount;
            internal AnimationPresentationDiagnosticsInterest Interest;
            internal int StackCount;
            internal int InertializationCount;
            internal int EntryCount;
            internal int OperationCount;
            internal int ParameterCount;
            internal int BlendSpacePlayerCount;
            internal int BlendSpaceSampleCount;
            internal int SlotContributionCount;
            internal int OperationContributionCount;
            internal int FinalContributionCount;
            internal int ReleaseCount;
            internal int AnimationSlotCount;
            internal int PoseStateMachineCount;
            internal int RootOrientationWarpCount;
            internal int LinkedPoseGroupCount;
            internal int LinkedPoseEntryCount;
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
            internal AnimationFootIkRuntimeSnapshot FootIk;

            internal void ClearCounts()
            {
                StackCount = 0;
                InertializationCount = 0;
                EntryCount = 0;
                OperationCount = 0;
                ParameterCount = 0;
                BlendSpacePlayerCount = 0;
                BlendSpaceSampleCount = 0;
                SlotContributionCount = 0;
                OperationContributionCount = 0;
                FinalContributionCount = 0;
                ReleaseCount = 0;
                AnimationSlotCount = 0;
                PoseStateMachineCount = 0;
                RootOrientationWarpCount = 0;
                LinkedPoseGroupCount = 0;
                LinkedPoseEntryCount = 0;
                PoseWatchCount = 0;
                FootIk = default;
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
                    FootIk,
                    PhysicalBoneCount,
                    VirtualBoneCount,
                    PoseBoneCount,
                    Lease,
                    leaseIdentity,
                    Stacks,
                    StackCount,
                    Inertializations,
                    InertializationCount,
                    Entries,
                    EntryCount,
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
                    AnimationSlots,
                    AnimationSlotCount,
                    PoseStateMachines,
                    PoseStateMachineCount,
                    RootOrientationWarps,
                    RootOrientationWarpCount,
                    LinkedPoseGroups,
                    LinkedPoseGroupCount,
                    LinkedPoseEntries,
                    LinkedPoseEntryCount,
                    PoseWatches,
                    PoseWatchCount,
                    PoseWatchFullBodyIkGoals,
                    PoseWatchPredictiveFootPlacements,
                    PoseWatchFullBodyIkSolvers,
                    PoseWatchFullBodyIkEffectors,
                    PoseWatchFullBodyIkLimbs,
                    PoseWatchLocalPoses,
                    PoseWatchComponentPoses,
                    PoseWatchContributions,
                    BoneIds,
                    PoseBones,
                    EntryBoneWeights,
                    StoredBoneWeights,
                    InertialPositionResiduals,
                    InertialRotationResiduals,
                    InertialScaleResiduals,
                    InertialBoneEnvelopes,
                    PoseStateMachineBoneWeights,
                    SlotContributionBoneWeights,
                    OperationContributionBoneWeights,
                    FinalContributionBoneWeights);
            }
        }
    }
}
