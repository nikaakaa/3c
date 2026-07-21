using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation
{
    public enum EquipmentChangeFailure : byte
    {
        None = 0,
        CapabilityDisabled = 1,
        UnknownSlot = 2,
        UnknownEquipment = 3,
        SlotMismatch = 4,
        RequiredSlotCannotBeEmpty = 5,
        PendingChangeExists = 6,
        PendingChangeMissing = 7,
        ChangeIdentityMismatch = 8,
        StaleRevision = 9,
        ActionConflict = 10,
        ContributionRejected = 11
    }

    public readonly struct EquipmentChangeRequest
    {
        public EquipmentChangeRequest(
            EquipmentSlotId slotId,
            EquipmentId equipmentId,
            ulong expectedRevision,
            ulong sourceActionInstanceId)
        {
            if (!slotId.IsValid || expectedRevision == 0)
                throw new ArgumentException("Equipment change request is incomplete.");
            SlotId = slotId;
            EquipmentId = equipmentId;
            ExpectedRevision = expectedRevision;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        public EquipmentSlotId SlotId { get; }
        public EquipmentId EquipmentId { get; }
        public ulong ExpectedRevision { get; }
        public ulong SourceActionInstanceId { get; }
        public bool IsValid => SlotId.IsValid && ExpectedRevision != 0;
    }

    public readonly struct EquipmentChangeOutcome
    {
        public EquipmentChangeOutcome(bool succeeded, EquipmentChangeFailure failure, EquipmentChangeId changeId)
        {
            if (succeeded == (failure != EquipmentChangeFailure.None))
                throw new ArgumentException("Equipment change outcome is inconsistent.");
            Succeeded = succeeded;
            Failure = failure;
            ChangeId = changeId;
        }

        public bool Succeeded { get; }
        public EquipmentChangeFailure Failure { get; }
        public EquipmentChangeId ChangeId { get; }
    }

    internal interface IEquipmentMutationScope : IDisposable
    {
        void Complete();
    }

    internal interface IEquipmentRuntimePort
    {
        ActorId ActorId { get; }
        ulong Tick { get; }
        EquipmentProgramLayout Layout { get; }
        EquipmentStateAggregate ReadState();
        void WriteState(EquipmentStateAggregate state);
        EquipmentChangeId AllocateChangeId();
        bool HasActiveActionConflict(EquipmentSlotState slot, ulong sourceActionInstanceId);
        bool IsActionActive(ulong actionInstanceId);
        void ResetLocalState(int stateSlotIndex);
        void SetTags(string sourceId, IReadOnlyList<string> tags);
        void RemoveTags(string sourceId);
        ulong ApplyPassiveEffect(string effectId);
        void RemovePassiveEffect(ulong handle);
        IEquipmentMutationScope BeginMutation();
        void CommitEffectOutputs(OperationHandle source);
        void CancelEffectOutputs();
        void EmitLifecycle(
            OperationHandle source,
            EquipmentSlotState before,
            EquipmentSlotState after,
            PendingEquipmentChangeState state,
            EquipmentChangeId changeId);
    }

    internal sealed class EquipmentRuntimeControl
    {
        readonly IEquipmentRuntimePort m_Port;

        public EquipmentRuntimeControl(IEquipmentRuntimePort port)
        {
            m_Port = port ?? throw new ArgumentNullException(nameof(port));
        }

        public EquipmentProgramLayout Layout => m_Port.Layout;

        public void InitializeContributions(OperationHandle source)
        {
            if (!Layout.CapabilityEnabled)
                return;
            EquipmentStateAggregate state = m_Port.ReadState();
            bool requiresMutation = false;
            for (int i = 0; i < state.Slots.Count; i++)
                requiresMutation |= state.Slots[i].IsEquipped && !state.Slots[i].ContributionsInstalled;
            if (!requiresMutation)
                return;
            using IEquipmentMutationScope mutation = m_Port.BeginMutation();
            try
            {
                for (int i = 0; i < state.Slots.Count; i++)
                {
                    EquipmentSlotState slot = state.Slots[i];
                    if (!slot.IsEquipped || slot.ContributionsInstalled)
                        continue;
                    slot = Install(slot);
                    state = state.WithSlot(slot);
                }
                m_Port.WriteState(state);
                m_Port.CommitEffectOutputs(source);
                mutation.Complete();
            }
            catch
            {
                m_Port.CancelEffectOutputs();
                throw;
            }
        }

        public void CancelOrphanedPending(OperationHandle source)
        {
            EquipmentStateAggregate aggregate = m_Port.ReadState();
            PendingEquipmentChange pending = aggregate.PendingChange;
            if (!pending.IsPending || pending.SourceActionInstanceId == 0 || m_Port.IsActionActive(pending.SourceActionInstanceId))
                return;
            EquipmentSlotState slot = aggregate.RequireSlot(pending.SlotId);
            m_Port.WriteState(aggregate.ResolvePending(PendingEquipmentChangeState.Cancelled, m_Port.Tick));
            m_Port.EmitLifecycle(source, slot, slot, PendingEquipmentChangeState.Cancelled, pending.ChangeId);
        }

        public EquipmentChangeOutcome Validate(EquipmentChangeRequest request)
        {
            if (!Layout.CapabilityEnabled)
                return Fail(EquipmentChangeFailure.CapabilityDisabled);
            EquipmentProgramSlot definition;
            try { definition = Layout.RequireSlot(request.SlotId); }
            catch (InvalidOperationException) { return Fail(EquipmentChangeFailure.UnknownSlot); }
            EquipmentSlotState current = m_Port.ReadState().RequireSlot(request.SlotId);
            if (current.Revision != request.ExpectedRevision)
                return Fail(EquipmentChangeFailure.StaleRevision);
            if (!request.EquipmentId.IsValid)
                return definition.Requirement == EquipmentSlotRequirement.Required
                    ? Fail(EquipmentChangeFailure.RequiredSlotCannotBeEmpty)
                    : Success(default);
            EquipmentProgramItem item;
            try { item = Layout.RequireItem(request.EquipmentId); }
            catch (InvalidOperationException) { return Fail(EquipmentChangeFailure.UnknownEquipment); }
            return item.SlotId == request.SlotId
                ? Success(default)
                : Fail(EquipmentChangeFailure.SlotMismatch);
        }

        public EquipmentChangeOutcome Begin(EquipmentChangeRequest request)
        {
            EquipmentChangeOutcome validation = Validate(request);
            if (!validation.Succeeded)
                return validation;
            EquipmentStateAggregate aggregate = m_Port.ReadState();
            if (aggregate.PendingChange.IsPending)
                return Fail(EquipmentChangeFailure.PendingChangeExists);
            EquipmentSlotState current = aggregate.RequireSlot(request.SlotId);
            EquipmentChangeId changeId = m_Port.AllocateChangeId();
            var pending = new PendingEquipmentChange(
                changeId,
                request.SlotId,
                current.EquipmentId,
                request.EquipmentId,
                request.SourceActionInstanceId,
                m_Port.Tick,
                0,
                PendingEquipmentChangeState.Pending);
            m_Port.WriteState(aggregate.WithPending(pending));
            return Success(changeId);
        }

        public EquipmentChangeOutcome Commit(
            OperationHandle source,
            EquipmentChangeId changeId,
            Action<EquipmentSlotState> abortOutgoingHost)
        {
            EquipmentStateAggregate aggregate = m_Port.ReadState();
            PendingEquipmentChange pending = aggregate.PendingChange;
            if (!pending.IsPending)
                return Fail(EquipmentChangeFailure.PendingChangeMissing);
            if (pending.ChangeId != changeId)
                return Fail(EquipmentChangeFailure.ChangeIdentityMismatch);
            EquipmentSlotState outgoing = aggregate.RequireSlot(pending.SlotId);
            if (outgoing.EquipmentId != pending.FromEquipmentId)
                return Fail(EquipmentChangeFailure.StaleRevision);
            if (m_Port.HasActiveActionConflict(outgoing, pending.SourceActionInstanceId))
                return Fail(EquipmentChangeFailure.ActionConflict);

            using IEquipmentMutationScope mutation = m_Port.BeginMutation();
            try
            {
                abortOutgoingHost?.Invoke(outgoing);
                Remove(outgoing);
                ResetFeatureState(outgoing.FeatureId);
                ulong nextRevision = checked(outgoing.Revision + 1);
                ulong nextGeneration = checked(outgoing.HostGeneration + 1);
                EquipmentSlotState incoming = CreateSlotState(outgoing.SlotId, pending.ToEquipmentId, nextRevision, nextGeneration);
                ResetFeatureState(incoming.FeatureId);
                if (incoming.IsEquipped)
                    incoming = Install(incoming);
                aggregate = aggregate.WithSlot(incoming).ResolvePending(PendingEquipmentChangeState.Committed, m_Port.Tick);
                m_Port.WriteState(aggregate);
                m_Port.CommitEffectOutputs(source);
                m_Port.EmitLifecycle(source, outgoing, incoming, PendingEquipmentChangeState.Committed, changeId);
                mutation.Complete();
                return Success(changeId);
            }
            catch
            {
                m_Port.CancelEffectOutputs();
                throw;
            }
        }

        public EquipmentChangeOutcome Cancel(OperationHandle source, EquipmentChangeId changeId)
        {
            EquipmentStateAggregate aggregate = m_Port.ReadState();
            PendingEquipmentChange pending = aggregate.PendingChange;
            if (!pending.IsPending)
                return Fail(EquipmentChangeFailure.PendingChangeMissing);
            if (pending.ChangeId != changeId)
                return Fail(EquipmentChangeFailure.ChangeIdentityMismatch);
            EquipmentSlotState slot = aggregate.RequireSlot(pending.SlotId);
            m_Port.WriteState(aggregate.ResolvePending(PendingEquipmentChangeState.Cancelled, m_Port.Tick));
            m_Port.EmitLifecycle(source, slot, slot, PendingEquipmentChangeState.Cancelled, changeId);
            return Success(changeId);
        }

        public EquipmentSlotState RequireCurrent(EquipmentSlotId slotId) =>
            m_Port.ReadState().RequireSlot(slotId);

        EquipmentSlotState CreateSlotState(
            EquipmentSlotId slotId,
            EquipmentId equipmentId,
            ulong revision,
            ulong generation)
        {
            if (!equipmentId.IsValid)
                return new EquipmentSlotState(slotId, default, default, default, default, revision, generation, false, string.Empty, Array.Empty<ulong>());
            EquipmentProgramItem item = Layout.RequireItem(equipmentId);
            EquipmentProgramFeature feature = Layout.RequireFeature(item.FeatureId);
            return new EquipmentSlotState(
                slotId,
                item.EquipmentId,
                item.FeatureId,
                feature.Revision,
                item.VisualBindingId,
                revision,
                generation,
                false,
                string.Empty,
                Array.Empty<ulong>());
        }

        EquipmentSlotState Install(EquipmentSlotState slot)
        {
            EquipmentProgramFeature feature = Layout.RequireFeature(slot.FeatureId);
            string source = EquipmentTagSourceIdentity.Create(m_Port.ActorId, slot.SlotId, slot.Revision);
            m_Port.SetTags(source, feature.GrantedTags);
            var handles = new ulong[feature.PassiveEffects.Count];
            for (int i = 0; i < handles.Length; i++)
                handles[i] = m_Port.ApplyPassiveEffect(feature.PassiveEffects[i]);
            return new EquipmentSlotState(
                slot.SlotId,
                slot.EquipmentId,
                slot.FeatureId,
                slot.FeatureRevision,
                slot.VisualBindingId,
                slot.Revision,
                slot.HostGeneration,
                true,
                source,
                handles);
        }

        void Remove(EquipmentSlotState slot)
        {
            if (!slot.IsEquipped || !slot.ContributionsInstalled)
                return;
            m_Port.RemoveTags(slot.TagSource);
            for (int i = 0; i < slot.PassiveEffectHandles.Count; i++)
                m_Port.RemovePassiveEffect(slot.PassiveEffectHandles[i]);
        }

        void ResetFeatureState(EquipmentFeatureId featureId)
        {
            if (!featureId.IsValid)
                return;
            IReadOnlyList<EquipmentProgramLocalState> states = Layout.LocalStates;
            for (int i = 0; i < states.Count; i++)
                if (states[i].FeatureId == featureId)
                    m_Port.ResetLocalState(states[i].StateSlotIndex);
        }

        static EquipmentChangeOutcome Success(EquipmentChangeId changeId) =>
            new EquipmentChangeOutcome(true, EquipmentChangeFailure.None, changeId);

        static EquipmentChangeOutcome Fail(EquipmentChangeFailure failure) =>
            new EquipmentChangeOutcome(false, failure, default);
    }
}
