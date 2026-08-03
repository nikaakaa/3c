using System;

namespace ThirdPersonCharacter.Animation.TransitionRouting
{
    internal sealed class TransitionRoutingWorkspaceState
    {
        struct Cell<T>
        {
            T m_Committed;
            T m_Pending;
            bool m_Dirty;

            internal T Read(bool frameOpen) =>
                frameOpen && m_Dirty ? m_Pending : m_Committed;

            internal void Write(bool frameOpen, T value)
            {
                if (frameOpen)
                {
                    m_Pending = value;
                    m_Dirty = true;
                }
                else
                {
                    m_Committed = value;
                }
            }

            internal void Commit()
            {
                if (m_Dirty)
                    m_Committed = m_Pending;
                m_Pending = default;
                m_Dirty = false;
            }

            internal void Discard()
            {
                m_Pending = default;
                m_Dirty = false;
            }
        }

        Cell<bool> m_IsBound;
        Cell<TransitionRoutingPlanId> m_PlanId;
        Cell<TransitionDefinitionRevision> m_DefinitionRevision;
        Cell<TransitionRouteOwnerId> m_OwnerNodeId;
        Cell<TransitionFrameId> m_LastFrameId;
        Cell<TransitionEndpointId> m_CurrentEndpoint;
        Cell<TransitionEndpointId> m_RequestedEndpoint;
        Cell<TransitionSelectionGeneration> m_SelectionGeneration;
        Cell<TransitionRoutingLifecycle> m_Lifecycle;
        Cell<TransitionRuleId> m_ActiveRuleId;
        Cell<PoseInertializationRequest> m_ActiveRequest;
        Cell<bool> m_HasActiveRequest;
        Cell<bool> m_HasStandardCommand;
        Cell<TransitionEndpointId> m_StandardTarget;
        Cell<TransitionRuleId> m_StandardRuleId;
        Cell<TransitionSelectionGeneration> m_StandardSelectionGeneration;
        Cell<bool> m_HasInertialIntent;
        Cell<bool> m_PendingRebaseRequired;
        Cell<bool> m_CaptureCompleted;
        Cell<bool> m_ReleaseCompleted;
        Cell<ulong> m_RequestGenerationValue;
        Cell<ulong> m_ModuleGenerationValue;
        Cell<ulong> m_RebaseCount;
        Cell<TransitionRoutingResetReason> m_LastResetReason;
        Cell<TransitionRoutingReasonCode> m_LastReasonCode;
        Cell<string> m_LastReason;
        Cell<TransitionRoutingRuntimeSnapshot> m_Snapshot;

        internal TransitionRoutingWorkspaceState()
        {
            Lifecycle = TransitionRoutingLifecycle.Idle;
            LastReason = string.Empty;
        }

        internal bool FrameOpen { get; private set; }
        internal bool IsBound { get => m_IsBound.Read(FrameOpen); set => m_IsBound.Write(FrameOpen, value); }
        internal TransitionRoutingPlanId PlanId { get => m_PlanId.Read(FrameOpen); set => m_PlanId.Write(FrameOpen, value); }
        internal TransitionDefinitionRevision DefinitionRevision { get => m_DefinitionRevision.Read(FrameOpen); set => m_DefinitionRevision.Write(FrameOpen, value); }
        internal TransitionRouteOwnerId OwnerNodeId { get => m_OwnerNodeId.Read(FrameOpen); set => m_OwnerNodeId.Write(FrameOpen, value); }
        internal TransitionFrameId LastFrameId { get => m_LastFrameId.Read(FrameOpen); set => m_LastFrameId.Write(FrameOpen, value); }
        internal TransitionEndpointId CurrentEndpoint { get => m_CurrentEndpoint.Read(FrameOpen); set => m_CurrentEndpoint.Write(FrameOpen, value); }
        internal TransitionEndpointId RequestedEndpoint { get => m_RequestedEndpoint.Read(FrameOpen); set => m_RequestedEndpoint.Write(FrameOpen, value); }
        internal TransitionSelectionGeneration SelectionGeneration { get => m_SelectionGeneration.Read(FrameOpen); set => m_SelectionGeneration.Write(FrameOpen, value); }
        internal TransitionRoutingLifecycle Lifecycle { get => m_Lifecycle.Read(FrameOpen); set => m_Lifecycle.Write(FrameOpen, value); }
        internal TransitionRuleId ActiveRuleId { get => m_ActiveRuleId.Read(FrameOpen); set => m_ActiveRuleId.Write(FrameOpen, value); }
        internal PoseInertializationRequest ActiveRequest { get => m_ActiveRequest.Read(FrameOpen); set => m_ActiveRequest.Write(FrameOpen, value); }
        internal bool HasActiveRequest { get => m_HasActiveRequest.Read(FrameOpen); set => m_HasActiveRequest.Write(FrameOpen, value); }
        internal bool HasStandardCommand { get => m_HasStandardCommand.Read(FrameOpen); set => m_HasStandardCommand.Write(FrameOpen, value); }
        internal TransitionEndpointId StandardTarget { get => m_StandardTarget.Read(FrameOpen); set => m_StandardTarget.Write(FrameOpen, value); }
        internal TransitionRuleId StandardRuleId { get => m_StandardRuleId.Read(FrameOpen); set => m_StandardRuleId.Write(FrameOpen, value); }
        internal TransitionSelectionGeneration StandardSelectionGeneration { get => m_StandardSelectionGeneration.Read(FrameOpen); set => m_StandardSelectionGeneration.Write(FrameOpen, value); }
        internal bool HasInertialIntent { get => m_HasInertialIntent.Read(FrameOpen); set => m_HasInertialIntent.Write(FrameOpen, value); }
        internal bool PendingRebaseRequired { get => m_PendingRebaseRequired.Read(FrameOpen); set => m_PendingRebaseRequired.Write(FrameOpen, value); }
        internal bool CaptureCompleted { get => m_CaptureCompleted.Read(FrameOpen); set => m_CaptureCompleted.Write(FrameOpen, value); }
        internal bool ReleaseCompleted { get => m_ReleaseCompleted.Read(FrameOpen); set => m_ReleaseCompleted.Write(FrameOpen, value); }
        internal ulong RequestGenerationValue { get => m_RequestGenerationValue.Read(FrameOpen); set => m_RequestGenerationValue.Write(FrameOpen, value); }
        internal ulong ModuleGenerationValue { get => m_ModuleGenerationValue.Read(FrameOpen); set => m_ModuleGenerationValue.Write(FrameOpen, value); }
        internal ulong RebaseCount { get => m_RebaseCount.Read(FrameOpen); set => m_RebaseCount.Write(FrameOpen, value); }
        internal TransitionRoutingResetReason LastResetReason { get => m_LastResetReason.Read(FrameOpen); set => m_LastResetReason.Write(FrameOpen, value); }
        internal TransitionRoutingReasonCode LastReasonCode { get => m_LastReasonCode.Read(FrameOpen); set => m_LastReasonCode.Write(FrameOpen, value); }
        internal string LastReason { get => m_LastReason.Read(FrameOpen); set => m_LastReason.Write(FrameOpen, value); }
        internal TransitionRoutingRuntimeSnapshot Snapshot { get => m_Snapshot.Read(FrameOpen); set => m_Snapshot.Write(FrameOpen, value); }

        internal void BeginFrame()
        {
            if (FrameOpen)
                throw new InvalidOperationException("Transition Routing state frame is already open.");
            FrameOpen = true;
        }

        internal void CommitFrame()
        {
            RequireOpen();
            m_IsBound.Commit();
            m_PlanId.Commit();
            m_DefinitionRevision.Commit();
            m_OwnerNodeId.Commit();
            m_LastFrameId.Commit();
            m_CurrentEndpoint.Commit();
            m_RequestedEndpoint.Commit();
            m_SelectionGeneration.Commit();
            m_Lifecycle.Commit();
            m_ActiveRuleId.Commit();
            m_ActiveRequest.Commit();
            m_HasActiveRequest.Commit();
            m_HasStandardCommand.Commit();
            m_StandardTarget.Commit();
            m_StandardRuleId.Commit();
            m_StandardSelectionGeneration.Commit();
            m_HasInertialIntent.Commit();
            m_PendingRebaseRequired.Commit();
            m_CaptureCompleted.Commit();
            m_ReleaseCompleted.Commit();
            m_RequestGenerationValue.Commit();
            m_ModuleGenerationValue.Commit();
            m_RebaseCount.Commit();
            m_LastResetReason.Commit();
            m_LastReasonCode.Commit();
            m_LastReason.Commit();
            m_Snapshot.Commit();
            FrameOpen = false;
        }

        internal void DiscardFrame()
        {
            RequireOpen();
            m_IsBound.Discard();
            m_PlanId.Discard();
            m_DefinitionRevision.Discard();
            m_OwnerNodeId.Discard();
            m_LastFrameId.Discard();
            m_CurrentEndpoint.Discard();
            m_RequestedEndpoint.Discard();
            m_SelectionGeneration.Discard();
            m_Lifecycle.Discard();
            m_ActiveRuleId.Discard();
            m_ActiveRequest.Discard();
            m_HasActiveRequest.Discard();
            m_HasStandardCommand.Discard();
            m_StandardTarget.Discard();
            m_StandardRuleId.Discard();
            m_StandardSelectionGeneration.Discard();
            m_HasInertialIntent.Discard();
            m_PendingRebaseRequired.Discard();
            m_CaptureCompleted.Discard();
            m_ReleaseCompleted.Discard();
            m_RequestGenerationValue.Discard();
            m_ModuleGenerationValue.Discard();
            m_RebaseCount.Discard();
            m_LastResetReason.Discard();
            m_LastReasonCode.Discard();
            m_LastReason.Discard();
            m_Snapshot.Discard();
            FrameOpen = false;
        }

        void RequireOpen()
        {
            if (!FrameOpen)
                throw new InvalidOperationException("Transition Routing state frame is not open.");
        }
    }

    internal sealed class TransitionRoutingEventJournal
    {
        readonly TransitionRoutingEvent[] m_Committed;
        readonly TransitionRoutingEvent[] m_Pending;
        int m_CommittedStart;
        int m_CommittedCount;
        int m_PendingStart;
        int m_PendingCount;

        internal TransitionRoutingEventJournal(int capacity)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            m_Committed = new TransitionRoutingEvent[capacity];
            m_Pending = new TransitionRoutingEvent[capacity];
        }

        internal bool FrameOpen { get; private set; }
        internal int Count => FrameOpen ? m_PendingCount : m_CommittedCount;

        internal void BeginFrame()
        {
            if (FrameOpen)
                throw new InvalidOperationException("Transition Routing event frame is already open.");
            m_PendingStart = 0;
            m_PendingCount = 0;
            FrameOpen = true;
        }

        internal void CommitFrame()
        {
            RequireOpen();
            for (int i = 0; i < m_PendingCount; i++)
            {
                AppendToRing(
                    m_Committed,
                    ref m_CommittedStart,
                    ref m_CommittedCount,
                    m_Pending[(m_PendingStart + i) % m_Pending.Length]);
            }
            Close();
        }

        internal void DiscardFrame()
        {
            RequireOpen();
            Close();
        }

        internal void Append(TransitionRoutingEvent item)
        {
            if (FrameOpen)
            {
                AppendToRing(
                    m_Pending,
                    ref m_PendingStart,
                    ref m_PendingCount,
                    item);
                return;
            }
            AppendToRing(
                m_Committed,
                ref m_CommittedStart,
                ref m_CommittedCount,
                item);
        }

        internal TransitionRoutingEvent[] CopyEvents()
        {
            TransitionRoutingEvent[] source = FrameOpen
                ? m_Pending
                : m_Committed;
            int start = FrameOpen ? m_PendingStart : m_CommittedStart;
            int count = Count;
            var result = new TransitionRoutingEvent[count];
            for (int i = 0; i < count; i++)
                result[i] = source[(start + i) % source.Length];
            return result;
        }

        internal void Clear()
        {
            if (FrameOpen)
            {
                m_PendingStart = 0;
                m_PendingCount = 0;
                return;
            }
            m_CommittedStart = 0;
            m_CommittedCount = 0;
        }

        static void AppendToRing(
            TransitionRoutingEvent[] events,
            ref int start,
            ref int count,
            TransitionRoutingEvent item)
        {
            int writeIndex;
            if (count < events.Length)
            {
                writeIndex = (start + count) % events.Length;
                count++;
            }
            else
            {
                writeIndex = start;
                start = (start + 1) % events.Length;
            }
            events[writeIndex] = item;
        }

        void Close()
        {
            m_PendingStart = 0;
            m_PendingCount = 0;
            FrameOpen = false;
        }

        void RequireOpen()
        {
            if (!FrameOpen)
                throw new InvalidOperationException("Transition Routing event frame is not open.");
        }
    }
}
