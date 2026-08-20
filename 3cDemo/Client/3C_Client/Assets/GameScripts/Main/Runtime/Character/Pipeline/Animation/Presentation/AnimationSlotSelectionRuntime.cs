using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation.Lifecycle;

namespace ThirdPersonCharacter.Pipeline.Animation.Presentation
{
    public readonly struct AnimationSlotFramePlan
    {
        public AnimationSlotFramePlan(
            AnimationSlotId slotId,
            PoseNodeId slotNodeId,
            PoseNodeId actionPlayerNodeId,
            AnimationPoseSelectionGeneration selectionGeneration,
            bool selectionChanged,
            AnimationPlaybackId targetActionPlaybackId,
            AnimationPlaybackId outgoingPlaybackId)
        {
            SlotId = slotId;
            SlotNodeId = slotNodeId;
            ActionPlayerNodeId = actionPlayerNodeId;
            SelectionGeneration = selectionGeneration;
            SelectionChanged = selectionChanged;
            TargetActionPlaybackId = targetActionPlaybackId;
            OutgoingPlaybackId = outgoingPlaybackId;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Animation Slot frame plan is invalid.");
            }
        }

        public AnimationSlotId SlotId { get; }
        public PoseNodeId SlotNodeId { get; }
        public PoseNodeId ActionPlayerNodeId { get; }
        public AnimationPoseSelectionGeneration SelectionGeneration { get; }
        public bool SelectionChanged { get; }
        public AnimationPlaybackId TargetActionPlaybackId { get; }
        public AnimationPlaybackId OutgoingPlaybackId { get; }
        public bool TargetsSourcePose => !TargetActionPlaybackId.IsValid;
        public bool HasOutgoingSource => OutgoingPlaybackId.IsValid;
        public bool IsValid =>
            SlotId.IsValid &&
            SlotNodeId.IsValid &&
            ActionPlayerNodeId.IsValid &&
            SelectionGeneration.IsValid &&
            (!HasOutgoingSource ||
             TargetActionPlaybackId.IsValid &&
             !OutgoingPlaybackId.Equals(TargetActionPlaybackId));
    }

    public readonly struct AnimationSlotMutationLease
    {
        internal AnimationSlotMutationLease(
            ulong frameIdentity,
            ulong generation)
        {
            FrameIdentity = frameIdentity;
            Generation = generation;
        }

        public ulong FrameIdentity { get; }
        internal ulong Generation { get; }
        public bool IsValid => FrameIdentity != 0 && Generation != 0;
    }

    public readonly struct AnimationSlotActionSourcePlan
    {
        public AnimationSlotActionSourcePlan(
            AnimationSlotId slotId,
            PoseNodeId slotNodeId,
            AnimationPlaybackId playbackId,
            AnimationPoseSelectionGeneration selectionGeneration,
            bool current)
        {
            SlotId = slotId;
            SlotNodeId = slotNodeId;
            PlaybackId = playbackId;
            SelectionGeneration = selectionGeneration;
            Current = current;
            if (!IsValid)
            {
                throw new ArgumentException(
                    "Animation Slot Action source plan is invalid.");
            }
        }

        public AnimationSlotId SlotId { get; }
        public PoseNodeId SlotNodeId { get; }
        public AnimationPlaybackId PlaybackId { get; }
        public AnimationPoseSelectionGeneration SelectionGeneration { get; }
        public bool Current { get; }
        public bool IsValid =>
            SlotId.IsValid &&
            SlotNodeId.IsValid &&
            PlaybackId.IsValid &&
            SelectionGeneration.IsValid;
    }

    public sealed class AnimationSlotRuntime
    {
        sealed class SlotState
        {
            internal readonly CharacterAnimationSlotDescriptor Descriptor;
            internal readonly AnimationPlaybackId[] RetainedActions;
            internal readonly ulong[] RetainedSelectionGenerations;
            internal AnimationPlaybackId CurrentAction;
            internal ulong CurrentActionSelectionGeneration;
            internal AnimationPlaybackId OutgoingAction;
            internal ulong SelectionGeneration;
            internal int RetainedCount;

            internal SlotState(
                CharacterAnimationSlotDescriptor descriptor,
                int retainedCapacity)
            {
                Descriptor = descriptor ??
                    throw new ArgumentNullException(nameof(descriptor));
                RetainedActions =
                    new AnimationPlaybackId[retainedCapacity];
                RetainedSelectionGenerations = new ulong[retainedCapacity];
                SelectionGeneration = 1;
            }

            internal void CopyFrom(SlotState source)
            {
                int previousRetainedCount = RetainedCount;
                CurrentAction = source.CurrentAction;
                CurrentActionSelectionGeneration =
                    source.CurrentActionSelectionGeneration;
                OutgoingAction = source.OutgoingAction;
                SelectionGeneration = source.SelectionGeneration;
                RetainedCount = source.RetainedCount;
                Array.Copy(
                    source.RetainedActions,
                    RetainedActions,
                    source.RetainedCount);
                Array.Copy(
                    source.RetainedSelectionGenerations,
                    RetainedSelectionGenerations,
                    source.RetainedCount);
                if (previousRetainedCount > source.RetainedCount)
                {
                    Array.Clear(
                        RetainedActions,
                        source.RetainedCount,
                        previousRetainedCount - source.RetainedCount);
                    Array.Clear(
                        RetainedSelectionGenerations,
                        source.RetainedCount,
                        previousRetainedCount - source.RetainedCount);
                }
            }

            internal bool ContainsRetained(AnimationPlaybackId playbackId) =>
                FindRetained(playbackId) >= 0;

            internal void AddRetained(
                AnimationPlaybackId playbackId,
                ulong selectionGeneration)
            {
                if (FindRetained(playbackId) >= 0)
                {
                    throw new InvalidOperationException(
                        $"Animation Slot '{Descriptor.SlotId}' already retains Action '{playbackId}'.");
                }
                if (RetainedCount == RetainedActions.Length)
                {
                    throw new InvalidOperationException(
                        $"Animation Slot '{Descriptor.SlotId}' retained source capacity was exceeded.");
                }
                int insertion = RetainedCount;
                while (insertion > 0 &&
                       ComparePlayback(
                           RetainedActions[insertion - 1],
                           playbackId) > 0)
                {
                    RetainedActions[insertion] =
                        RetainedActions[insertion - 1];
                    RetainedSelectionGenerations[insertion] =
                        RetainedSelectionGenerations[insertion - 1];
                    insertion--;
                }
                RetainedActions[insertion] = playbackId;
                RetainedSelectionGenerations[insertion] = selectionGeneration;
                RetainedCount++;
            }

            internal bool RemoveRetained(AnimationPlaybackId playbackId)
            {
                int index = FindRetained(playbackId);
                if (index < 0)
                    return false;
                RetainedCount--;
                for (int i = index; i < RetainedCount; i++)
                {
                    RetainedActions[i] = RetainedActions[i + 1];
                    RetainedSelectionGenerations[i] =
                        RetainedSelectionGenerations[i + 1];
                }
                RetainedActions[RetainedCount] = default;
                RetainedSelectionGenerations[RetainedCount] = 0;
                return true;
            }

            internal void Reset()
            {
                CurrentAction = default;
                CurrentActionSelectionGeneration = 0;
                OutgoingAction = default;
                SelectionGeneration = 1;
                if (RetainedCount > 0)
                {
                    Array.Clear(RetainedActions, 0, RetainedCount);
                    Array.Clear(
                        RetainedSelectionGenerations,
                        0,
                        RetainedCount);
                }
                RetainedCount = 0;
            }

            int FindRetained(AnimationPlaybackId playbackId)
            {
                for (int i = 0; i < RetainedCount; i++)
                {
                    if (RetainedActions[i].Equals(playbackId))
                        return i;
                }
                return -1;
            }
        }

        readonly struct SelectedAction
        {
            internal SelectedAction(
                bool occupied,
                ActionAnimationPlaybackLifecycleFrame lifecycle)
            {
                Occupied = occupied;
                Lifecycle = lifecycle;
            }

            internal bool Occupied { get; }
            internal ActionAnimationPlaybackLifecycleFrame Lifecycle { get; }
        }

        readonly ActionAnimationBindingIndex m_Bindings;
        readonly SlotState[] m_FirstStates;
        readonly SlotState[] m_SecondStates;
        readonly byte[] m_CommittedOwners;
        readonly bool[] m_DirtyStates;
        readonly int[] m_DirtyStateIndices;
        readonly SelectedAction[] m_SelectedActions;
        readonly FixedCapacityFrameBuffer<AnimationSlotFramePlan>
            m_FramePlans;
        readonly FixedCapacityFrameBuffer<AnimationSlotActionSourcePlan>
            m_ActionSourcePlans;
        ulong m_NextLeaseGeneration;
        ulong m_NextUsageCompletionIdentity;
        ulong m_NextPermissionCompletionIdentity;
        ulong m_PendingUsageCompletionIdentity;
        ulong m_PendingPermissionCompletionIdentity;
        int m_DirtyStateCount;
        AnimationSlotMutationLease m_ActiveLease;

        public AnimationSlotRuntime(ActionAnimationBindingIndex bindings)
        {
            m_Bindings = bindings ??
                throw new ArgumentNullException(nameof(bindings));
            CharacterPresentationPosePlan posePlan =
                bindings.Projection.PosePlan;
            int slotCount = posePlan.AnimationSlots.Count;
            int sourceCapacity = 0;
            var descriptors =
                new CharacterAnimationSlotDescriptor[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                CharacterAnimationSlotDescriptor descriptor =
                    posePlan.AnimationSlots[i] ??
                    throw new InvalidOperationException(
                        $"Animation Slot #{i} is missing.");
                descriptor.RequireValid();
                descriptors[i] = descriptor;
                sourceCapacity = checked(
                    sourceCapacity +
                    descriptor.BlendStackWorkspace.Capacity);
            }
            Array.Sort(descriptors, CompareDescriptors);
            for (int i = 1; i < descriptors.Length; i++)
            {
                if (descriptors[i - 1].SlotId.Equals(descriptors[i].SlotId))
                {
                    throw new InvalidOperationException(
                        $"Animation Slot '{descriptors[i].SlotId}' is duplicated.");
                }
            }
            m_FirstStates = CreateStatePage(descriptors);
            m_SecondStates = CreateStatePage(descriptors);
            m_CommittedOwners = new byte[slotCount];
            m_DirtyStates = new bool[slotCount];
            m_DirtyStateIndices = new int[slotCount];
            m_SelectedActions = new SelectedAction[slotCount];
            m_FramePlans =
                new FixedCapacityFrameBuffer<AnimationSlotFramePlan>(
                    Math.Max(1, slotCount));
            m_ActionSourcePlans =
                new FixedCapacityFrameBuffer<AnimationSlotActionSourcePlan>(
                    Math.Max(1, checked(sourceCapacity + slotCount)));
            foreach (KeyValuePair<AnimationProducerId,
                         ResolvedActionAnimationBinding> pair in
                     bindings.Bindings)
            {
                ResolvedActionAnimationBinding binding = pair.Value;
                int stateIndex = FindState(binding.SlotId);
                if (stateIndex < 0 ||
                    GetCommittedState(stateIndex).Descriptor.NodeId !=
                        binding.SlotNodeId ||
                    GetCommittedState(stateIndex).Descriptor.ActionPlayer
                        .PlayerNodeId != binding.ActionPlayerNodeId)
                {
                    throw new InvalidOperationException(
                        $"Action producer '{binding.ProgramProducerId}' has no exact Animation Slot.");
                }
            }
        }

        public int SlotCount => m_CommittedOwners.Length;

        public AnimationSlotMutationLease BeginFrame(ulong frameIdentity)
        {
            if (m_ActiveLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Animation Slot runtime already has an active mutation.");
            }
            if (frameIdentity == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(frameIdentity),
                    "Animation Slot frame identity must be positive.");
            }
            if (m_NextLeaseGeneration == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    "Animation Slot frame lease generation was exhausted.");
            }
            if (m_DirtyStateCount != 0)
            {
                throw new InvalidOperationException(
                    "Animation Slot dirty state journal was not closed.");
            }
            m_NextLeaseGeneration++;
            m_PendingUsageCompletionIdentity =
                m_NextUsageCompletionIdentity;
            m_PendingPermissionCompletionIdentity =
                m_NextPermissionCompletionIdentity;
            m_ActiveLease =
                new AnimationSlotMutationLease(
                    frameIdentity,
                    m_NextLeaseGeneration);
            return m_ActiveLease;
        }

        internal FixedCapacityFrameBuffer<AnimationSlotFramePlan>
            BuildFramePlans(
                AnimationSlotMutationLease lease,
                IReadOnlyList<ActionAnimationPlaybackLifecycleFrame>
                    lifecycleFrames)
        {
            RequireLease(lease);
            if (lifecycleFrames == null)
                throw new ArgumentNullException(nameof(lifecycleFrames));
            m_FramePlans.Clear();
            Array.Clear(m_SelectedActions, 0, m_SelectedActions.Length);
            for (int i = 0; i < lifecycleFrames.Count; i++)
            {
                ActionAnimationPlaybackLifecycleFrame lifecycle =
                    lifecycleFrames[i];
                for (int prior = 0; prior < i; prior++)
                {
                    if (lifecycleFrames[prior].PlaybackId.Equals(
                        lifecycle.PlaybackId))
                    {
                        throw new InvalidOperationException(
                            $"Action playback '{lifecycle.PlaybackId}' is duplicated.");
                    }
                }
                if (!m_Bindings.TryGet(
                        lifecycle.PlaybackId.ProducerId,
                        out ResolvedActionAnimationBinding binding))
                {
                    throw new InvalidOperationException(
                        $"Action playback '{lifecycle.PlaybackId}' has no exact Slot binding.");
                }
                int slotIndex = FindState(binding.SlotId);
                if (slotIndex < 0)
                {
                    throw new InvalidOperationException(
                        $"Action playback '{lifecycle.PlaybackId}' targets an unknown Slot.");
                }
                if (lifecycle.Phase !=
                    ActionAnimationPlaybackLifecyclePhase.Selected)
                {
                    continue;
                }
                SelectedAction selected = m_SelectedActions[slotIndex];
                if (!selected.Occupied ||
                    lifecycle.LatestCommandSequence >
                    selected.Lifecycle.LatestCommandSequence)
                {
                    m_SelectedActions[slotIndex] =
                        new SelectedAction(true, lifecycle);
                }
                else if (lifecycle.LatestCommandSequence ==
                         selected.Lifecycle.LatestCommandSequence)
                {
                    throw new InvalidOperationException(
                        $"Animation Slot '{binding.SlotId}' received ambiguous selected Action order.");
                }
            }
            for (int i = 0; i < SlotCount; i++)
            {
                SlotState state = GetFrameState(i);
                AnimationPlaybackId desired =
                    m_SelectedActions[i].Occupied
                        ? m_SelectedActions[i].Lifecycle.PlaybackId
                        : default;
                bool changed = !state.CurrentAction.Equals(desired);
                if (changed)
                {
                    state = GetMutableState(i);
                    if (state.CurrentAction.IsValid)
                    {
                        state.AddRetained(
                            state.CurrentAction,
                            state.CurrentActionSelectionGeneration);
                        state.OutgoingAction = desired.IsValid
                            ? state.CurrentAction
                            : default;
                    }
                    else
                    {
                        state.OutgoingAction = default;
                    }
                    state.CurrentAction = desired;
                    state.SelectionGeneration++;
                    if (state.SelectionGeneration == 0)
                    {
                        throw new InvalidOperationException(
                            $"Animation Slot '{state.Descriptor.SlotId}' selection generation was exhausted.");
                    }
                    state.CurrentActionSelectionGeneration = desired.IsValid
                        ? state.SelectionGeneration
                        : 0;
                }
                if (state.OutgoingAction.IsValid)
                {
                    int markerIndex = FindLifecycle(
                        lifecycleFrames,
                        state.OutgoingAction);
                    if (markerIndex < 0 ||
                        lifecycleFrames[markerIndex].Phase ==
                            ActionAnimationPlaybackLifecyclePhase.Retired)
                    {
                        throw new InvalidOperationException(
                            $"Animation Slot '{state.Descriptor.SlotId}' retained outgoing source '{state.OutgoingAction}' without a live lifecycle entry.");
                    }
                }
                for (int retainedIndex = 0;
                     retainedIndex < state.RetainedCount;
                     retainedIndex++)
                {
                    AnimationPlaybackId retained =
                        state.RetainedActions[retainedIndex];
                    int lifecycleIndex = FindLifecycle(
                        lifecycleFrames,
                        retained);
                    if (lifecycleIndex < 0 ||
                        lifecycleFrames[lifecycleIndex].Phase ==
                            ActionAnimationPlaybackLifecyclePhase.Retired)
                    {
                        throw new InvalidOperationException(
                            $"Animation Slot '{state.Descriptor.SlotId}' retained Action '{retained}' without a live lifecycle entry.");
                    }
                }
                m_FramePlans.Add(
                    new AnimationSlotFramePlan(
                        state.Descriptor.SlotId,
                        state.Descriptor.NodeId,
                        state.Descriptor.ActionPlayer.PlayerNodeId,
                        new AnimationPoseSelectionGeneration(
                            state.SelectionGeneration),
                        changed,
                        state.CurrentAction,
                        state.OutgoingAction));
            }
            return m_FramePlans;
        }

        internal FixedCapacityFrameBuffer<AnimationSlotActionSourcePlan>
            CollectActionSourcePlans(AnimationSlotMutationLease lease)
        {
            RequireLease(lease);
            m_ActionSourcePlans.Clear();
            for (int i = 0; i < SlotCount; i++)
            {
                SlotState state = GetFrameState(i);
                if (state.CurrentAction.IsValid)
                {
                    m_ActionSourcePlans.Add(
                        new AnimationSlotActionSourcePlan(
                            state.Descriptor.SlotId,
                            state.Descriptor.NodeId,
                            state.CurrentAction,
                            new AnimationPoseSelectionGeneration(
                                state.CurrentActionSelectionGeneration),
                            true));
                }
                for (int retainedIndex = 0;
                     retainedIndex < state.RetainedCount;
                     retainedIndex++)
                {
                    m_ActionSourcePlans.Add(
                        new AnimationSlotActionSourcePlan(
                            state.Descriptor.SlotId,
                            state.Descriptor.NodeId,
                            state.RetainedActions[retainedIndex],
                            new AnimationPoseSelectionGeneration(
                                state.RetainedSelectionGenerations[
                                    retainedIndex]),
                            false));
                }
            }
            return m_ActionSourcePlans;
        }

        public void PublishActionUsages(
            AnimationSlotMutationLease lease,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease workspaceLease)
        {
            RequireLease(lease);
            if (workspace == null || !workspaceLease.IsValid)
            {
                throw new ArgumentException(
                    "Animation Slot Action usage workspace is invalid.");
            }
            for (int i = 0; i < SlotCount; i++)
            {
                SlotState state = GetFrameState(i);
                if (state.CurrentAction.IsValid)
                {
                    workspace.AddActionUsage(
                        workspaceLease,
                        new ActionSlotSourceUsage(
                            state.Descriptor.SlotId,
                            state.CurrentAction,
                            ActionSlotSourceUsageKind.Sample,
                            NextUsageCompletionIdentity()));
                }
                for (int retainedIndex = 0;
                     retainedIndex < state.RetainedCount;
                     retainedIndex++)
                {
                    AnimationPlaybackId playbackId =
                        state.RetainedActions[retainedIndex];
                    workspace.AddActionUsage(
                        workspaceLease,
                        new ActionSlotSourceUsage(
                            state.Descriptor.SlotId,
                            playbackId,
                            playbackId.Equals(state.OutgoingAction)
                                ? ActionSlotSourceUsageKind.OutgoingHandoff
                                : ActionSlotSourceUsageKind.StoredPoseReference,
                            NextUsageCompletionIdentity()));
                }
            }
        }

        public void CompleteSourceRelease(
            AnimationSlotMutationLease lease,
            AnimationSlotId slotId,
            AnimationPlaybackId playbackId)
        {
            RequireLease(lease);
            int stateIndex = FindState(slotId);
            if (!slotId.IsValid ||
                !playbackId.IsValid ||
                stateIndex < 0)
            {
                throw new InvalidOperationException(
                    "Animation Slot source release completion is not exact.");
            }
            SlotState state = GetFrameState(stateIndex);
            if (!state.ContainsRetained(playbackId))
            {
                throw new InvalidOperationException(
                    "Animation Slot source release completion is not exact.");
            }
            state = GetMutableState(stateIndex);
            state.RemoveRetained(playbackId);
            if (state.OutgoingAction.Equals(playbackId))
                state.OutgoingAction = default;
        }

        internal void PublishRetirementPermissions(
            AnimationSlotMutationLease lease,
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame>
                lifecycleFrames,
            PresentationFrameWorkspace workspace,
            PresentationFrameWorkspaceLease workspaceLease)
        {
            RequireLease(lease);
            if (lifecycleFrames == null ||
                workspace == null ||
                !workspaceLease.IsValid)
            {
                throw new ArgumentException(
                    "Animation Slot retirement permission input is invalid.");
            }
            for (int i = 0; i < lifecycleFrames.Count; i++)
            {
                ActionAnimationPlaybackLifecycleFrame lifecycle =
                    lifecycleFrames[i];
                if (lifecycle.Phase !=
                        ActionAnimationPlaybackLifecyclePhase.Retained ||
                    lifecycle.LogicTerminal == ActionLogicTerminalKind.None)
                {
                    continue;
                }
                if (!m_Bindings.TryGet(
                        lifecycle.PlaybackId.ProducerId,
                        out ResolvedActionAnimationBinding binding))
                {
                    throw new InvalidOperationException(
                        $"Action playback '{lifecycle.PlaybackId}' has no exact Animation Slot for retirement.");
                }
                int stateIndex = FindState(binding.SlotId);
                if (stateIndex < 0)
                {
                    throw new InvalidOperationException(
                        $"Action playback '{lifecycle.PlaybackId}' has no exact Animation Slot for retirement.");
                }
                SlotState state = GetFrameState(stateIndex);
                if (state.CurrentAction.Equals(lifecycle.PlaybackId) ||
                    state.ContainsRetained(lifecycle.PlaybackId))
                {
                    continue;
                }
                workspace.AddRetirementPermission(
                    workspaceLease,
                    new ActionRetirementPermission(
                        lifecycle.PlaybackId,
                        binding.SlotId,
                        NextPermissionCompletionIdentity()));
            }
        }

        public void CommitFrame(AnimationSlotMutationLease lease)
        {
            RequireLease(lease);
            for (int i = 0; i < m_DirtyStateCount; i++)
            {
                int stateIndex = m_DirtyStateIndices[i];
                m_CommittedOwners[stateIndex] =
                    (byte)(1 - m_CommittedOwners[stateIndex]);
            }
            m_NextUsageCompletionIdentity =
                m_PendingUsageCompletionIdentity;
            m_NextPermissionCompletionIdentity =
                m_PendingPermissionCompletionIdentity;
            Close();
        }

        public void DiscardFrame(AnimationSlotMutationLease lease)
        {
            RequireLease(lease);
            Close();
        }

        public void Reset()
        {
            if (m_ActiveLease.IsValid)
            {
                throw new InvalidOperationException(
                    "Animation Slot runtime cannot reset during a mutation.");
            }
            for (int i = 0; i < SlotCount; i++)
            {
                m_FirstStates[i].Reset();
                m_SecondStates[i].Reset();
                m_CommittedOwners[i] = 0;
                m_DirtyStates[i] = false;
            }
            m_DirtyStateCount = 0;
            m_FramePlans.Clear();
            m_ActionSourcePlans.Clear();
        }

        ulong NextUsageCompletionIdentity()
        {
            if (m_PendingUsageCompletionIdentity == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    "Animation Slot usage completion identity was exhausted.");
            }
            m_PendingUsageCompletionIdentity++;
            return m_PendingUsageCompletionIdentity;
        }

        ulong NextPermissionCompletionIdentity()
        {
            if (m_PendingPermissionCompletionIdentity == ulong.MaxValue)
            {
                throw new InvalidOperationException(
                    "Animation Slot retirement permission identity was exhausted.");
            }
            m_PendingPermissionCompletionIdentity++;
            return m_PendingPermissionCompletionIdentity;
        }

        int FindState(AnimationSlotId slotId)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (m_FirstStates[i].Descriptor.SlotId.Equals(slotId))
                    return i;
            }
            return -1;
        }

        SlotState GetCommittedState(int stateIndex) =>
            m_CommittedOwners[stateIndex] == 0
                ? m_FirstStates[stateIndex]
                : m_SecondStates[stateIndex];

        SlotState GetFrameState(int stateIndex)
        {
            if (!m_DirtyStates[stateIndex])
                return GetCommittedState(stateIndex);
            return m_CommittedOwners[stateIndex] == 0
                ? m_SecondStates[stateIndex]
                : m_FirstStates[stateIndex];
        }

        SlotState GetMutableState(int stateIndex)
        {
            if (m_DirtyStates[stateIndex])
                return GetFrameState(stateIndex);
            SlotState committed = GetCommittedState(stateIndex);
            SlotState pending = m_CommittedOwners[stateIndex] == 0
                ? m_SecondStates[stateIndex]
                : m_FirstStates[stateIndex];
            if (m_DirtyStateCount >= m_DirtyStateIndices.Length)
            {
                throw new InvalidOperationException(
                    "Animation Slot dirty state capacity was exceeded.");
            }
            pending.CopyFrom(committed);
            m_DirtyStates[stateIndex] = true;
            m_DirtyStateIndices[m_DirtyStateCount] = stateIndex;
            m_DirtyStateCount++;
            return pending;
        }

        void RequireLease(AnimationSlotMutationLease lease)
        {
            if (!lease.IsValid ||
                !m_ActiveLease.IsValid ||
                lease.FrameIdentity != m_ActiveLease.FrameIdentity ||
                lease.Generation != m_ActiveLease.Generation)
            {
                throw new InvalidOperationException(
                    "Animation Slot mutation lease is invalid.");
            }
        }

        void Close()
        {
            for (int i = 0; i < m_DirtyStateCount; i++)
                m_DirtyStates[m_DirtyStateIndices[i]] = false;
            m_DirtyStateCount = 0;
            m_FramePlans.Clear();
            m_ActionSourcePlans.Clear();
            Array.Clear(m_SelectedActions, 0, m_SelectedActions.Length);
            m_ActiveLease = default;
        }

        static SlotState[] CreateStatePage(
            CharacterAnimationSlotDescriptor[] descriptors)
        {
            var states = new SlotState[descriptors.Length];
            for (int i = 0; i < descriptors.Length; i++)
            {
                states[i] = new SlotState(
                    descriptors[i],
                    Math.Max(
                        1,
                        descriptors[i].BlendStackWorkspace.Capacity));
            }
            return states;
        }

        static int FindLifecycle(
            IReadOnlyList<ActionAnimationPlaybackLifecycleFrame> lifecycle,
            AnimationPlaybackId playbackId)
        {
            for (int i = 0; i < lifecycle.Count; i++)
            {
                if (lifecycle[i].PlaybackId.Equals(playbackId))
                    return i;
            }
            return -1;
        }

        static int CompareDescriptors(
            CharacterAnimationSlotDescriptor left,
            CharacterAnimationSlotDescriptor right) =>
            left.SlotId.CompareTo(right.SlotId);

        static int ComparePlayback(
            AnimationPlaybackId left,
            AnimationPlaybackId right)
        {
            int producer = string.Compare(
                left.ProducerId.ProgramProducerIdentity,
                right.ProducerId.ProgramProducerIdentity,
                StringComparison.Ordinal);
            return producer != 0
                ? producer
                : left.Generation.CompareTo(right.Generation);
        }
    }
}
