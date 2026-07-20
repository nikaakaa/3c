using System;
using System.Collections.Generic;
using ThirdPersonSimulation.Fixed;

namespace ThirdPersonSimulation.DeterministicRollback
{
    public interface IRollbackPresentationOutputPort : ISimulationPresentationOutputPort
    {
        void BeginCommit();
        void CompleteCommit(ulong confirmedTick);
        void AbortCommit();
    }

    public interface IRollbackCommitOutputPort :
        IRollbackPresentationOutputPort,
        IFixedPublishedActorResultObserver
    {
    }

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
        readonly IRollbackCommitOutputPort m_Output;
        readonly IFixedSourceEgressOutputPort m_SourceEgress;
        readonly ISimulationDiagnosticsSink m_Diagnostics;
        Dictionary<RollbackOutputSlot, RollbackOutputRecord> m_Records =
            new Dictionary<RollbackOutputSlot, RollbackOutputRecord>();
        ulong m_KeepCount;
        ulong m_ReplaceCount;
        ulong m_CancelCount;
        ulong m_ConfirmedOnlyCommitCount;

        public RollbackOutputCommitter(
            SimulationComponentIdentity identity,
            RollbackRuntimeState state,
            int maximumRecords,
            IRollbackCommitOutputPort output,
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

            Dictionary<EventId, SimulationOutputDisposition> dispositions = IndexDispositions(batch);
            var next = new Dictionary<RollbackOutputSlot, RollbackOutputRecord>(m_Records);
            var operations = new List<RollbackOutputOperation>();
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
                        operations,
                        actor,
                        step.Step.ExecutionKind,
                        dispositions,
                        ref keeps,
                        ref replacements,
                        ref cancellations);
                }
            }

            FlushConfirmed(next, operations, ref confirmations);
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
                for (int i = 0; i < operations.Count; i++)
                {
                    Apply(operations[i]);
                    PublishDiagnostics(operations[i], next.Count);
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

            m_Records = next;
            m_KeepCount = checked(m_KeepCount + keeps);
            m_ReplaceCount = checked(m_ReplaceCount + replacements);
            m_CancelCount = checked(m_CancelCount + cancellations);
            m_ConfirmedOnlyCommitCount = checked(m_ConfirmedOnlyCommitCount + confirmations);
        }

        static Dictionary<EventId, SimulationOutputDisposition> IndexDispositions(FixedSimulationCommitBatch batch)
        {
            var values = new Dictionary<EventId, SimulationOutputDisposition>();
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
            return values;
        }

        static void ResolveActorTick(
            Dictionary<RollbackOutputSlot, RollbackOutputRecord> records,
            ICollection<RollbackOutputOperation> operations,
            SimulationActorTickResult actor,
            SimulationPipelineStepExecutionKind executionKind,
            IReadOnlyDictionary<EventId, SimulationOutputDisposition> dispositions,
            ref ulong keeps,
            ref ulong replacements,
            ref ulong cancellations)
        {
            var existing = new List<RollbackOutputSlot>();
            foreach (RollbackOutputSlot slot in records.Keys)
            {
                if (slot.ActorId == actor.ActorId && slot.Tick == actor.Tick)
                    existing.Add(slot);
            }
            existing.Sort();

            List<RollbackOutputRecord> current = BuildCurrent(actor, executionKind, dispositions);
            var seen = new HashSet<RollbackOutputSlot>();
            for (int i = 0; i < current.Count; i++)
            {
                RollbackOutputRecord value = current[i];
                if (!seen.Add(value.Slot))
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
                    keeps++;
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
                records[value.Slot] = value;
            }

            for (int i = 0; i < existing.Count; i++)
            {
                RollbackOutputSlot slot = existing[i];
                if (seen.Contains(slot))
                    continue;
                RollbackOutputRecord previous = records[slot];
                if (!previous.ConfirmedOnly)
                {
                    operations.Add(RollbackOutputOperation.Retire(previous, executionKind));
                    cancellations++;
                }
                records.Remove(slot);
            }
        }

        static List<RollbackOutputRecord> BuildCurrent(
            SimulationActorTickResult actor,
            SimulationPipelineStepExecutionKind executionKind,
            IReadOnlyDictionary<EventId, SimulationOutputDisposition> dispositions)
        {
            var values = new List<RollbackOutputRecord>(
                actor.GameplayFacts.Count + actor.PresentationCommands.Count);
            for (int i = 0; i < actor.GameplayFacts.Count; i++)
            {
                GameplayFact fact = actor.GameplayFacts[i];
                SimulationOutputDisposition disposition = GetRequiredDisposition(fact.Header, dispositions);
                values.Add(new RollbackOutputRecord(
                    fact,
                    executionKind,
                    disposition.Kind == SimulationOutputDispositionKind.Defer));
            }
            for (int i = 0; i < actor.PresentationCommands.Count; i++)
            {
                PresentationCommand command = actor.PresentationCommands[i];
                SimulationOutputDisposition disposition = GetRequiredDisposition(command.Header, dispositions);
                values.Add(new RollbackOutputRecord(
                    command,
                    executionKind,
                    disposition.Kind == SimulationOutputDispositionKind.Defer));
            }
            values.Sort((left, right) => left.Slot.CompareTo(right.Slot));
            return values;
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
            var release = new List<RollbackOutputSlot>();
            foreach (KeyValuePair<RollbackOutputSlot, RollbackOutputRecord> pair in records)
            {
                if (pair.Key.Tick.Value > m_State.ConfirmedTick)
                    continue;
                if (pair.Value.ConfirmedOnly)
                {
                    operations.Add(RollbackOutputOperation.Publish(pair.Value, pair.Value.ExecutionKind));
                    confirmations++;
                }
                release.Add(pair.Key);
            }
            release.Sort();
            for (int i = 0; i < release.Count; i++)
                records.Remove(release[i]);
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
            m_Diagnostics.PublishModel(new SimulationModelTraceRecord(
                SimulationModelTraceKind.OutputDisposition,
                $"rollback_output_{operation.Kind.ToString().ToLowerInvariant()}",
                $"event={output.EventId};target={operation.TargetEventId};channel={output.Slot.Channel};execution={operation.ExecutionKind};confirmedOnly={output.ConfirmedOnly}",
                output.Slot.ActorId,
                output.Slot.Tick.Value,
                m_State.ConfirmedTick,
                checked((ulong)header.Activation.Operation.Value),
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

    sealed class RollbackOutputRecord
    {
        public RollbackOutputRecord(
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
        }

        public RollbackOutputRecord(
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
        }

        public RollbackOutputSlot Slot { get; }
        public GameplayFact Gameplay { get; }
        public PresentationCommand Presentation { get; }
        public bool IsGameplay { get; }
        public bool ConfirmedOnly { get; }
        public SimulationPipelineStepExecutionKind ExecutionKind { get; }
        public EventId EventId => IsGameplay ? Gameplay.Header.EventId : Presentation.Header.EventId;
        public SimulationEventHeader Header => IsGameplay ? Gameplay.Header : Presentation.Header;

        public EventId CreateRetirementEventId()
        {
            return new EventId(StableHash.Compute(
                "deterministic-rollback-output-retire/1",
                Slot.ActorId.ToString(),
                Slot.Tick.ToString(),
                Slot.Sequence.ToString(),
                Slot.Channel,
                EventId.ToString()));
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
