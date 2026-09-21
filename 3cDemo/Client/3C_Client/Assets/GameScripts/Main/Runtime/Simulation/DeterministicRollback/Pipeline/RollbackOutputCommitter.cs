using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public readonly struct RollbackOutputLifecycleSnapshot
    {
        public RollbackOutputLifecycleSnapshot(
            int recordCount,
            int pendingConfirmedOnlyCount,
            ulong keepCount,
            ulong replaceCount,
            ulong cancelCount,
            ulong confirmedOnlyCommitCount)
        {
            RecordCount = recordCount;
            PendingConfirmedOnlyCount = pendingConfirmedOnlyCount;
            KeepCount = keepCount;
            ReplaceCount = replaceCount;
            CancelCount = cancelCount;
            ConfirmedOnlyCommitCount = confirmedOnlyCommitCount;
        }

        public int RecordCount { get; }
        public int PendingConfirmedOnlyCount { get; }
        public ulong KeepCount { get; }
        public ulong ReplaceCount { get; }
        public ulong CancelCount { get; }
        public ulong ConfirmedOnlyCommitCount { get; }
    }

    public sealed class RollbackOutputCommitter : IFixedSimulationCommitter
    {
        readonly RollbackRuntimeState m_State;
        readonly int m_MaximumRecords;
        readonly IFixedSimulationResultOutputPort m_Output;
        readonly IFixedSourceEgressOutputPort m_SourceEgress;
        readonly ISimulationDiagnosticsSink m_Diagnostics;
        readonly List<RollbackOutputSlot> m_ExistingSlots;
        readonly List<RollbackOutputRecord> m_CurrentRecords;
        readonly HashSet<RollbackOutputSlot> m_SeenSlots;
        readonly List<RollbackOutputSlot> m_ReleaseSlots;
        readonly Dictionary<EventId, SimulationOutputDisposition> m_DispositionIndex;
        readonly List<RollbackOutputOperation> m_Operations;
        Dictionary<RollbackOutputSlot, RollbackOutputRecord> m_Records;
        Dictionary<RollbackOutputSlot, RollbackOutputRecord> m_RecordWorkspace;
        readonly List<RollbackOutputRecord> m_RecordPool;
        readonly List<RollbackOutputRecord> m_CommitRecords;
        readonly List<RollbackOutputRecord> m_RetiredRecords;
        ulong m_KeepCount;
        ulong m_ReplaceCount;
        ulong m_CancelCount;
        ulong m_ConfirmedOnlyCommitCount;

        public RollbackOutputCommitter(
            SimulationComponentIdentity identity,
            RollbackRuntimeState state,
            int maximumRecords,
            IFixedSimulationResultOutputPort output,
            IFixedSourceEgressOutputPort sourceEgress,
            ISimulationDiagnosticsSink diagnostics)
        {
            if (!identity.IsValid || identity.Role != SimulationComponentRole.Committer)
                throw new ArgumentException("Rollback output Committer identity is invalid.", nameof(identity));
            if (maximumRecords <= 0)
                throw new ArgumentOutOfRangeException(nameof(maximumRecords));
            Identity = identity;
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_MaximumRecords = maximumRecords;
            m_Output = output ?? throw new ArgumentNullException(nameof(output));
            m_SourceEgress = sourceEgress ?? throw new ArgumentNullException(nameof(sourceEgress));
            m_Diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
            m_ExistingSlots = new List<RollbackOutputSlot>(maximumRecords);
            m_CurrentRecords = new List<RollbackOutputRecord>(maximumRecords);
            m_SeenSlots = new HashSet<RollbackOutputSlot>(maximumRecords);
            m_ReleaseSlots = new List<RollbackOutputSlot>(maximumRecords);
            m_DispositionIndex = new Dictionary<EventId, SimulationOutputDisposition>(maximumRecords);
            m_Operations = new List<RollbackOutputOperation>(maximumRecords);
            m_Records = new Dictionary<RollbackOutputSlot, RollbackOutputRecord>(maximumRecords);
            m_RecordWorkspace = new Dictionary<RollbackOutputSlot, RollbackOutputRecord>(maximumRecords);
            m_RecordPool = new List<RollbackOutputRecord>(maximumRecords);
            m_CommitRecords = new List<RollbackOutputRecord>(maximumRecords);
            m_RetiredRecords = new List<RollbackOutputRecord>(maximumRecords);
        }

        public SimulationComponentIdentity Identity { get; }

        public RollbackOutputLifecycleSnapshot CaptureLifecycleSnapshot()
        {
            int pending = 0;
            foreach (RollbackOutputRecord record in m_Records.Values)
            {
                if (record.ConfirmedOnly)
                    pending++;
            }
            return new RollbackOutputLifecycleSnapshot(
                m_Records.Count,
                pending,
                m_KeepCount,
                m_ReplaceCount,
                m_CancelCount,
                m_ConfirmedOnlyCommitCount);
        }

        public void Commit(FixedSimulationCommitBatch batch)
        {
            if (batch == null)
                throw new ArgumentNullException(nameof(batch));
            m_DispositionIndex.Clear();
            m_Operations.Clear();
            m_CommitRecords.Clear();
            m_RetiredRecords.Clear();
            try
            {
                CommitPrepared(batch);
                ReleaseRetiredRecords();
                m_CommitRecords.Clear();
                m_RetiredRecords.Clear();
            }
            catch
            {
                ReleaseCommitRecords();
                m_RetiredRecords.Clear();
                throw;
            }
            finally
            {
                m_DispositionIndex.Clear();
                m_Operations.Clear();
                m_RecordWorkspace.Clear();
            }
        }

        void CommitPrepared(FixedSimulationCommitBatch batch)
        {
            IndexDispositions(batch, m_DispositionIndex);
            foreach (KeyValuePair<RollbackOutputSlot, RollbackOutputRecord> pair in m_Records)
                m_RecordWorkspace.Add(pair.Key, pair.Value);
            Dictionary<RollbackOutputSlot, RollbackOutputRecord> next = m_RecordWorkspace;
            ulong keeps = 0;
            ulong replacements = 0;
            ulong cancellations = 0;
            ulong confirmations = 0;

            for (int stepIndex = 0; stepIndex < batch.Steps.Count; stepIndex++)
            {
                FixedCompletedSimulationStep step = batch.Steps[stepIndex];
                if (step.Step.ExecutionKind == SimulationPipelineStepExecutionKind.Replay &&
                    step.Step.Tick.Value <= m_State.ConfirmedTickAtTransactionStart)
                    throw new InvalidOperationException($"Rollback Step '{step.Step.Tick}' attempts to revise confirmed output history.");
                for (int actorIndex = 0; actorIndex < step.Result.Actors.Count; actorIndex++)
                {
                    SimulationActorTickResult actor = step.Result.Actors[actorIndex];
                    ResolveActorTick(
                        next,
                        m_Operations,
                        actor,
                        step.Step.ExecutionKind,
                        m_DispositionIndex,
                        ref keeps,
                        ref replacements,
                        ref cancellations);
                }
            }

            FlushConfirmed(next, m_Operations, ref confirmations);
            if (next.Count > m_MaximumRecords)
            {
                throw new InvalidOperationException(
                    $"Rollback output registry capacity '{m_MaximumRecords}' is exhausted by '{next.Count}' records.");
            }

            for (int i = 0; i < batch.SourceEgress.Count; i++)
                m_SourceEgress.Commit(batch.SourceEgress[i]);
            try
            {
                m_Output.BeginCommit();
                for (int i = 0; i < m_Operations.Count; i++)
                {
                    Apply(m_Operations[i]);
                    PublishDiagnostics(m_Operations[i], next.Count);
                }
                for (int stepIndex = 0; stepIndex < batch.Steps.Count; stepIndex++)
                {
                    SimulationTickResult result = batch.Steps[stepIndex].Result;
                    for (int actorIndex = 0; actorIndex < result.Actors.Count; actorIndex++)
                        m_Output.ObservePublished(result.Actors[actorIndex]);
                }
                m_Output.CompleteCommit(m_State.ConfirmedTick);
            }
            catch
            {
                m_Output.AbortCommit();
                throw;
            }

            Dictionary<RollbackOutputSlot, RollbackOutputRecord> previous = m_Records;
            m_Records = next;
            m_RecordWorkspace = previous;
            m_KeepCount = checked(m_KeepCount + keeps);
            m_ReplaceCount = checked(m_ReplaceCount + replacements);
            m_CancelCount = checked(m_CancelCount + cancellations);
            m_ConfirmedOnlyCommitCount = checked(m_ConfirmedOnlyCommitCount + confirmations);
        }

        static void IndexDispositions(
            FixedSimulationCommitBatch batch,
            Dictionary<EventId, SimulationOutputDisposition> values)
        {
            for (int i = 0; i < batch.OutputDispositions.Dispositions.Count; i++)
            {
                SimulationOutputDisposition disposition = batch.OutputDispositions.Dispositions[i];
                if (disposition.Kind != SimulationOutputDispositionKind.Publish &&
                    disposition.Kind != SimulationOutputDispositionKind.Defer)
                {
                    throw new InvalidOperationException(
                        $"Rollback Egress produced unsupported initial disposition '{disposition.Kind}'.");
                }
                values.Add(disposition.SourceEventId, disposition);
            }
        }

        void ResolveActorTick(
            Dictionary<RollbackOutputSlot, RollbackOutputRecord> records,
            ICollection<RollbackOutputOperation> operations,
            SimulationActorTickResult actor,
            SimulationPipelineStepExecutionKind executionKind,
            IReadOnlyDictionary<EventId, SimulationOutputDisposition> dispositions,
            ref ulong keeps,
            ref ulong replacements,
            ref ulong cancellations)
        {
            m_ExistingSlots.Clear();
            m_CurrentRecords.Clear();
            m_SeenSlots.Clear();
            try
            {
                foreach (RollbackOutputSlot slot in records.Keys)
                {
                    if (slot.ActorId == actor.ActorId && slot.Tick == actor.Tick)
                        m_ExistingSlots.Add(slot);
                }
                m_ExistingSlots.Sort();

                BuildCurrent(actor, executionKind, dispositions, m_CurrentRecords);
                for (int i = 0; i < m_CurrentRecords.Count; i++)
                {
                    RollbackOutputRecord value = m_CurrentRecords[i];
                    if (!m_SeenSlots.Add(value.Slot))
                        throw new InvalidOperationException($"Rollback output Tick '{actor.Tick}' contains duplicate semantic slot '{value.Slot}'.");
                    if (!records.TryGetValue(value.Slot, out RollbackOutputRecord previous))
                    {
                        if (!value.ConfirmedOnly)
                            operations.Add(RollbackOutputOperation.Publish(value, executionKind));
                        records.Add(value.Slot, value);
                        continue;
                    }

                    if (previous.EventId.Equals(value.EventId))
                    {
                        if (previous.ConfirmedOnly != value.ConfirmedOnly)
                            throw new InvalidOperationException($"Rollback output EventId '{value.EventId}' changed disposition class.");
                        if (!previous.IsGameplay &&
                            !value.IsGameplay &&
                            !SamePresentationCommand(previous.Presentation, value.Presentation))
                        {
                            if (!previous.ConfirmedOnly && !value.ConfirmedOnly)
                            {
                                operations.Add(RollbackOutputOperation.Replace(previous.EventId, value, executionKind));
                                replacements++;
                            }
                            else if (!previous.ConfirmedOnly)
                            {
                                operations.Add(RollbackOutputOperation.Retire(previous, executionKind));
                                cancellations++;
                            }
                            else if (!value.ConfirmedOnly)
                            {
                                operations.Add(RollbackOutputOperation.Publish(value, executionKind));
                            }
                        }
                        else
                            keeps++;
                        RetireRecord(previous);
                        records[value.Slot] = value;
                        continue;
                    }

                    if (!previous.ConfirmedOnly && !value.ConfirmedOnly)
                    {
                        operations.Add(RollbackOutputOperation.Replace(previous.EventId, value, executionKind));
                        replacements++;
                    }
                    else if (!previous.ConfirmedOnly)
                    {
                        operations.Add(RollbackOutputOperation.Retire(previous, executionKind));
                        cancellations++;
                    }
                    else if (!value.ConfirmedOnly)
                    {
                        operations.Add(RollbackOutputOperation.Publish(value, executionKind));
                    }
                    RetireRecord(previous);
                    records[value.Slot] = value;
                }

                for (int i = 0; i < m_ExistingSlots.Count; i++)
                {
                    RollbackOutputSlot slot = m_ExistingSlots[i];
                    if (m_SeenSlots.Contains(slot))
                        continue;
                    RollbackOutputRecord previous = records[slot];
                    if (!previous.ConfirmedOnly)
                    {
                        operations.Add(RollbackOutputOperation.Retire(previous, executionKind));
                        cancellations++;
                    }
                    RetireRecord(previous);
                    records.Remove(slot);
                }
            }
            finally
            {
                m_ExistingSlots.Clear();
                m_CurrentRecords.Clear();
                m_SeenSlots.Clear();
            }
        }

        void BuildCurrent(
            SimulationActorTickResult actor,
            SimulationPipelineStepExecutionKind executionKind,
            IReadOnlyDictionary<EventId, SimulationOutputDisposition> dispositions,
            List<RollbackOutputRecord> values)
        {
            for (int i = 0; i < actor.GameplayFacts.Count; i++)
            {
                GameplayFact fact = actor.GameplayFacts[i];
                SimulationOutputDisposition disposition = GetRequiredDisposition(fact.Header, dispositions);
                values.Add(RentGameplayRecord(
                    fact,
                    executionKind,
                    disposition.Kind == SimulationOutputDispositionKind.Defer));
            }
            for (int i = 0; i < actor.PresentationCommands.Count; i++)
            {
                PresentationCommand command = actor.PresentationCommands[i];
                SimulationOutputDisposition disposition = GetRequiredDisposition(command.Header, dispositions);
                values.Add(RentPresentationRecord(
                    command,
                    executionKind,
                    disposition.Kind == SimulationOutputDispositionKind.Defer));
            }
            values.Sort();
        }

        RollbackOutputRecord RentGameplayRecord(
            GameplayFact gameplay,
            SimulationPipelineStepExecutionKind executionKind,
            bool confirmedOnly)
        {
            RollbackOutputRecord record = RentRecord().ResetGameplay(gameplay, executionKind, confirmedOnly);
            m_CommitRecords.Add(record);
            return record;
        }

        RollbackOutputRecord RentPresentationRecord(
            PresentationCommand presentation,
            SimulationPipelineStepExecutionKind executionKind,
            bool confirmedOnly)
        {
            RollbackOutputRecord record = RentRecord().ResetPresentation(presentation, executionKind, confirmedOnly);
            m_CommitRecords.Add(record);
            return record;
        }

        RollbackOutputRecord RentRecord()
        {
            int last = m_RecordPool.Count - 1;
            if (last < 0)
                return new RollbackOutputRecord();
            RollbackOutputRecord record = m_RecordPool[last];
            m_RecordPool.RemoveAt(last);
            return record;
        }

        void RetireRecord(RollbackOutputRecord record) => m_RetiredRecords.Add(record);

        void ReleaseRetiredRecords()
        {
            for (int i = 0; i < m_RetiredRecords.Count; i++)
                ReleaseRecord(m_RetiredRecords[i]);
        }

        void ReleaseCommitRecords()
        {
            for (int i = 0; i < m_CommitRecords.Count; i++)
                ReleaseRecord(m_CommitRecords[i]);
        }

        void ReleaseRecord(RollbackOutputRecord record)
        {
            record.Reset();
            if (m_RecordPool.Count < m_MaximumRecords)
                m_RecordPool.Add(record);
        }

        static bool SamePresentationCommand(
            PresentationCommand left,
            PresentationCommand right)
        {
            return left.Kind == right.Kind &&
                   left.Header.Tick.Value == right.Header.Tick.Value &&
                   left.Header.Sequence == right.Header.Sequence &&
                   left.Header.Activation.Equals(right.Header.Activation) &&
                   string.Equals(left.Header.Channel, right.Header.Channel, StringComparison.Ordinal) &&
                   string.Equals(left.ProducerId, right.ProducerId, StringComparison.Ordinal) &&
                   left.SampleTime.Equals(right.SampleTime) &&
                   left.Weight.Equals(right.Weight) &&
                   left.ProducerGeneration == right.ProducerGeneration &&
                   left.Cycle == right.Cycle &&
                   left.SourceActionInstanceId == right.SourceActionInstanceId &&
                   left.VisualTimeScale.Equals(right.VisualTimeScale) &&
                   left.TimelineProgress.Equals(right.TimelineProgress) &&
                   left.Header.Activation.Equals(right.Header.Activation);
        }

        static SimulationOutputDisposition GetRequiredDisposition(
            SimulationEventHeader header,
            IReadOnlyDictionary<EventId, SimulationOutputDisposition> dispositions)
        {
            if (!dispositions.TryGetValue(header.EventId, out SimulationOutputDisposition disposition) ||
                disposition.ActorId != header.ActorId)
            {
                throw new InvalidOperationException($"Rollback output EventId '{header.EventId}' has no matching Egress disposition.");
            }
            return disposition;
        }

        void FlushConfirmed(
            Dictionary<RollbackOutputSlot, RollbackOutputRecord> records,
            ICollection<RollbackOutputOperation> operations,
            ref ulong confirmations)
        {
            if (m_State.ConfirmedTick == 0)
                return;
            m_ReleaseSlots.Clear();
            try
            {
                foreach (KeyValuePair<RollbackOutputSlot, RollbackOutputRecord> pair in records)
                {
                    if (pair.Key.Tick.Value > m_State.ConfirmedTick)
                        continue;
                    if (pair.Value.ConfirmedOnly)
                    {
                        operations.Add(RollbackOutputOperation.Publish(pair.Value, pair.Value.ExecutionKind));
                        confirmations++;
                    }
                    RetireRecord(pair.Value);
                    m_ReleaseSlots.Add(pair.Key);
                }
                m_ReleaseSlots.Sort();
                for (int i = 0; i < m_ReleaseSlots.Count; i++)
                    records.Remove(m_ReleaseSlots[i]);
            }
            finally
            {
                m_ReleaseSlots.Clear();
            }
        }

        void Apply(RollbackOutputOperation operation)
        {
            RollbackOutputRecord output = operation.Output;
            switch (operation.Kind)
            {
                case RollbackOutputOperationKind.Publish:
                    if (!output.IsGameplay)
                        m_Output.Publish(output.Presentation);
                    break;
                case RollbackOutputOperationKind.Replace:
                    if (!output.IsGameplay)
                        m_Output.Replace(operation.TargetEventId, output.Presentation);
                    break;
                case RollbackOutputOperationKind.Retire:
                    EventId retirement = output.CreateRetirementEventId();
                    if (!output.IsGameplay)
                        m_Output.Retire(output.Slot.ActorId, retirement, output.EventId);
                    break;
                default:
                    throw new InvalidOperationException($"Rollback output operation '{operation.Kind}' is invalid.");
            }
        }

        void PublishDiagnostics(RollbackOutputOperation operation, int recordCount)
        {
            if (!m_Diagnostics.IsEnabled)
                return;
            RollbackOutputRecord output = operation.Output;
            SimulationEventHeader header = output.Header;
            if (!header.Activation.Source.IsSkillOperation)
                throw new InvalidOperationException("Rollback output diagnostics requires a Skill operation execution source.");
            m_Diagnostics.PublishModel(new SimulationModelTraceRecord(
                SimulationModelTraceKind.OutputDisposition,
                $"rollback_output_{operation.Kind.ToString().ToLowerInvariant()}",
                $"event={output.EventId};target={operation.TargetEventId};channel={output.Slot.Channel};execution={operation.ExecutionKind};confirmedOnly={output.ConfirmedOnly}",
                output.Slot.ActorId,
                output.Slot.Tick.Value,
                m_State.ConfirmedTick,
                checked((ulong)header.Activation.Source.Operation.Value),
                header.Sequence,
                recordCount,
                operation.ExecutionKind == SimulationPipelineStepExecutionKind.Replay ? 1 : 0));
        }
    }

    readonly struct RollbackOutputSlot : IEquatable<RollbackOutputSlot>, IComparable<RollbackOutputSlot>
    {
        public RollbackOutputSlot(SimulationEventHeader header, bool gameplay)
        {
            ActorId = header.ActorId;
            Tick = header.Tick;
            Sequence = header.Sequence;
            Channel = header.Channel;
            Gameplay = gameplay;
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public ulong Sequence { get; }
        public string Channel { get; }
        public bool Gameplay { get; }

        public int CompareTo(RollbackOutputSlot other)
        {
            int tick = Tick.CompareTo(other.Tick);
            if (tick != 0)
                return tick;
            int actor = ActorId.CompareTo(other.ActorId);
            if (actor != 0)
                return actor;
            int sequence = Sequence.CompareTo(other.Sequence);
            if (sequence != 0)
                return sequence;
            int gameplay = Gameplay.CompareTo(other.Gameplay);
            return gameplay != 0 ? gameplay : string.CompareOrdinal(Channel, other.Channel);
        }

        public bool Equals(RollbackOutputSlot other) =>
            ActorId == other.ActorId && Tick == other.Tick && Sequence == other.Sequence &&
            Gameplay == other.Gameplay && string.Equals(Channel, other.Channel, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is RollbackOutputSlot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(ActorId, Tick, Sequence, Channel, Gameplay);
        public override string ToString() => $"{ActorId}/{Tick}/{(Gameplay ? "gameplay" : "presentation")}/{Channel}/{Sequence}";
    }

    sealed class RollbackOutputRecord : IComparable<RollbackOutputRecord>
    {
        internal RollbackOutputRecord() { }

        public RollbackOutputRecord ResetGameplay(
            GameplayFact gameplay,
            SimulationPipelineStepExecutionKind executionKind,
            bool confirmedOnly)
        {
            Gameplay = gameplay;
            Presentation = default;
            IsGameplay = true;
            ExecutionKind = executionKind;
            ConfirmedOnly = confirmedOnly;
            Slot = new RollbackOutputSlot(gameplay.Header, true);
            return this;
        }

        public RollbackOutputRecord ResetPresentation(
            PresentationCommand presentation,
            SimulationPipelineStepExecutionKind executionKind,
            bool confirmedOnly)
        {
            Gameplay = default;
            Presentation = presentation;
            IsGameplay = false;
            ExecutionKind = executionKind;
            ConfirmedOnly = confirmedOnly;
            Slot = new RollbackOutputSlot(presentation.Header, false);
            return this;
        }

        public void Reset()
        {
            Gameplay = default;
            Presentation = default;
            ExecutionKind = default;
            ConfirmedOnly = false;
            Slot = default;
        }

        public RollbackOutputSlot Slot { get; private set; }
        public GameplayFact Gameplay { get; private set; }
        public PresentationCommand Presentation { get; private set; }
        public bool IsGameplay { get; private set; }
        public bool ConfirmedOnly { get; private set; }
        public SimulationPipelineStepExecutionKind ExecutionKind { get; private set; }
        public int CompareTo(RollbackOutputRecord other) => Slot.CompareTo(other.Slot);
        public EventId EventId => IsGameplay ? Gameplay.Header.EventId : Presentation.Header.EventId;
        public SimulationEventHeader Header => IsGameplay ? Gameplay.Header : Presentation.Header;

        public EventId CreateRetirementEventId()
        {
            Span<byte> block = stackalloc byte[64];
            var builder = new EventIdBuilder(block);
            builder.Append("deterministic-rollback-output-retire/1");
            builder.Append(Slot.ActorId.Value);
            builder.Append(Slot.Tick.Value);
            builder.Append(Slot.Sequence);
            builder.Append(Slot.Channel);
            builder.Append(EventId);
            return builder.Build();
        }
    }

    enum RollbackOutputOperationKind : byte
    {
        Publish = 1,
        Replace = 2,
        Retire = 3
    }

    readonly struct RollbackOutputOperation
    {
        RollbackOutputOperation(
            RollbackOutputOperationKind kind,
            RollbackOutputRecord output,
            EventId targetEventId,
            SimulationPipelineStepExecutionKind executionKind)
        {
            Kind = kind;
            Output = output ?? throw new ArgumentNullException(nameof(output));
            TargetEventId = targetEventId;
            ExecutionKind = executionKind;
        }

        public RollbackOutputOperationKind Kind { get; }
        public RollbackOutputRecord Output { get; }
        public EventId TargetEventId { get; }
        public SimulationPipelineStepExecutionKind ExecutionKind { get; }

        public static RollbackOutputOperation Publish(
            RollbackOutputRecord output,
            SimulationPipelineStepExecutionKind executionKind) =>
            new RollbackOutputOperation(RollbackOutputOperationKind.Publish, output, default, executionKind);

        public static RollbackOutputOperation Replace(
            EventId targetEventId,
            RollbackOutputRecord output,
            SimulationPipelineStepExecutionKind executionKind) =>
            new RollbackOutputOperation(RollbackOutputOperationKind.Replace, output, targetEventId, executionKind);

        public static RollbackOutputOperation Retire(
            RollbackOutputRecord output,
            SimulationPipelineStepExecutionKind executionKind) =>
            new RollbackOutputOperation(RollbackOutputOperationKind.Retire, output, output.EventId, executionKind);
    }
}
