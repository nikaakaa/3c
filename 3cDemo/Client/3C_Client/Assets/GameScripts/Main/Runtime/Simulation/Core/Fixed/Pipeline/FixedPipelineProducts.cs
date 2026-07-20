using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation.Fixed
{
    public readonly struct FixedStepInput
    {
        public FixedStepInput(CharacterSimulationInput input)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public CharacterSimulationInput Input { get; }
    }

    public sealed class FixedCanonicalInputBatch
    {
        readonly ReadOnlyCollection<SimulationPipelineActorInput<FixedStepInput>> m_Inputs;

        public FixedCanonicalInputBatch(
            SimulationTickSourceIdentity source,
            IEnumerable<SimulationPipelineActorInput<FixedStepInput>> inputs)
        {
            if (string.IsNullOrEmpty(source.ClockId) || source.SourceTick == 0)
                throw new ArgumentException("Canonical input batch source is incomplete.", nameof(source));
            var values = inputs == null
                ? new List<SimulationPipelineActorInput<FixedStepInput>>()
                : new List<SimulationPipelineActorInput<FixedStepInput>>(inputs);
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (values.Count == 0)
                throw new ArgumentException("Canonical input batch cannot be empty.", nameof(inputs));
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0 && values[i - 1].ActorId.Equals(values[i].ActorId) ||
                    values[i].Value.Input == null ||
                    !values[i].Value.Input.TickSource.Equals(source))
                {
                    throw new ArgumentException("Canonical input batch contains duplicate Actors or another source clock.", nameof(inputs));
                }
            }
            Source = source;
            m_Inputs = values.AsReadOnly();
        }

        public SimulationTickSourceIdentity Source { get; }
        public IReadOnlyList<SimulationPipelineActorInput<FixedStepInput>> Inputs => m_Inputs;
    }

    public sealed class FixedTypedIngressBatch
    {
        readonly ReadOnlyCollection<SimulationPipelineTypedIngress<SimulationIngress>> m_Ingress;

        public FixedTypedIngressBatch(IEnumerable<SimulationPipelineTypedIngress<SimulationIngress>> ingress)
        {
            var values = ingress == null
                ? new List<SimulationPipelineTypedIngress<SimulationIngress>>()
                : new List<SimulationPipelineTypedIngress<SimulationIngress>>(ingress);
            values.Sort((left, right) =>
            {
                int actor = left.ActorId.CompareTo(right.ActorId);
                if (actor != 0)
                    return actor;
                int source = left.Source.SourceTick.CompareTo(right.Source.SourceTick);
                if (source != 0)
                    return source;
                int sequence = left.Sequence.CompareTo(right.Sequence);
                return sequence != 0 ? sequence : string.CompareOrdinal(left.FactIdentity, right.FactIdentity);
            });
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0 && SameIdentity(values[i - 1], values[i]))
                    throw new ArgumentException("Typed ingress batch contains a missing or duplicate fact.", nameof(ingress));
            }
            m_Ingress = values.AsReadOnly();
        }

        public IReadOnlyList<SimulationPipelineTypedIngress<SimulationIngress>> Ingress => m_Ingress;

        static bool SameIdentity(
            SimulationPipelineTypedIngress<SimulationIngress> left,
            SimulationPipelineTypedIngress<SimulationIngress> right)
        {
            return left.ActorId.Equals(right.ActorId) && left.Source.Equals(right.Source) &&
                   left.Sequence == right.Sequence && string.Equals(left.FactIdentity, right.FactIdentity, StringComparison.Ordinal);
        }
    }

    public sealed class FixedSimulationStep : TargetSimulationPipelineStep<FixedStepInput, SimulationIngress>
    {
        public FixedSimulationStep(
            SimulationTick tick,
            SimulationPipelineStepProvenance provenance,
            IEnumerable<SimulationPipelineActorInput<FixedStepInput>> inputs,
            IEnumerable<SimulationPipelineTypedIngress<SimulationIngress>> ingress)
            : base(tick, provenance, inputs, ingress)
        {
        }
    }

    public sealed class FixedPendingEvaluationBatch
    {
        readonly PendingCharacterEvaluation[] m_Evaluations;

        internal FixedPendingEvaluationBatch(int actorCount)
        {
            if (actorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(actorCount));
            m_Evaluations = new PendingCharacterEvaluation[actorCount];
        }

        internal FixedPendingEvaluationBatch Reset(
            SimulationTick tick,
            PendingCharacterEvaluation[] evaluations)
        {
            if (!tick.IsValid || evaluations == null || evaluations.Length != m_Evaluations.Length)
                throw new ArgumentException("Pending evaluation batch workspace is invalid.");
            for (int i = 0; i < evaluations.Length; i++)
            {
                if (evaluations[i] == null)
                    throw new ArgumentException("Pending evaluation batch contains a missing evaluation.", nameof(evaluations));
            }
            Array.Copy(evaluations, m_Evaluations, evaluations.Length);
            Array.Sort(m_Evaluations, (left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < m_Evaluations.Length; i++)
            {
                if (m_Evaluations[i].Tick != tick ||
                    i > 0 && m_Evaluations[i - 1].ActorId.Equals(m_Evaluations[i].ActorId))
                {
                    throw new ArgumentException("Pending evaluation batch identity is invalid.", nameof(evaluations));
                }
            }
            Tick = tick;
            return this;
        }

        public SimulationTick Tick { get; private set; }
        public IReadOnlyList<PendingCharacterEvaluation> Evaluations => m_Evaluations;

        internal void AbortUnconsumed()
        {
            for (int i = 0; i < m_Evaluations.Length; i++)
                m_Evaluations[i].AbortUnconsumed();
        }
    }

    public sealed class FixedFinalizedActorResult
    {
        public FixedFinalizedActorResult(SimulationActorTickResult result)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }

        public SimulationActorTickResult Result { get; }
    }

    public sealed class FixedCompletedSimulationStep
    {
        public FixedCompletedSimulationStep(
            FixedSimulationStep step,
            SimulationTickResult result,
            SimulationWorldStateSet state,
            FixedSimulationStepSnapshot stepSnapshot)
        {
            Step = step ?? throw new ArgumentNullException(nameof(step));
            Result = result ?? throw new ArgumentNullException(nameof(result));
            State = state ?? throw new ArgumentNullException(nameof(state));
            if (step.Tick != result.Tick || state.LastCompletedTick != step.Tick.Value)
                throw new ArgumentException("Completed Step result and state Tick do not match.");
            if (stepSnapshot != null && stepSnapshot.Tick != step.Tick)
                throw new ArgumentException("Completed Step snapshot Tick does not match.", nameof(stepSnapshot));
            StepSnapshot = stepSnapshot;
        }

        public FixedSimulationStep Step { get; }
        public SimulationTickResult Result { get; }
        public SimulationWorldStateSet State { get; }
        public FixedSimulationStepSnapshot StepSnapshot { get; }
        public SimulationPipelineStateSnapshot PipelineProjection => StepSnapshot?.PipelineProjection;
    }

    public sealed class SimulationPipelineOutputDispositionSet
    {
        readonly ReadOnlyCollection<SimulationOutputDisposition> m_Dispositions;

        public SimulationPipelineOutputDispositionSet(
            StableHash transactionIdentity,
            IEnumerable<SimulationOutputDisposition> dispositions)
        {
            if (!transactionIdentity.IsValid)
                throw new ArgumentException("Output disposition transaction identity is invalid.", nameof(transactionIdentity));
            var values = dispositions == null
                ? new List<SimulationOutputDisposition>()
                : new List<SimulationOutputDisposition>(dispositions);
            values.Sort((left, right) => left.SourceEventId.CompareTo(right.SourceEventId));
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i - 1].SourceEventId.Equals(values[i].SourceEventId))
                    throw new ArgumentException("Output disposition set contains duplicate EventId ownership.", nameof(dispositions));
            }
            TransactionIdentity = transactionIdentity;
            m_Dispositions = values.AsReadOnly();
        }

        public StableHash TransactionIdentity { get; }
        public IReadOnlyList<SimulationOutputDisposition> Dispositions => m_Dispositions;
    }

    public sealed class FixedSourceEgressRecord
    {
        readonly byte[] m_Payload;

        public FixedSourceEgressRecord(
            ActorId actorId,
            SimulationTick tick,
            string channelId,
            string schemaId,
            int schemaVersion,
            byte[] canonicalPayload)
        {
            if (!actorId.IsValid || !tick.IsValid || schemaVersion <= 0 || canonicalPayload == null)
                throw new ArgumentException("Source egress record identity is incomplete.");
            ActorId = actorId;
            Tick = tick;
            ChannelId = SimulationIdentity.Require(channelId, nameof(channelId));
            SchemaId = SimulationIdentity.Require(schemaId, nameof(schemaId));
            SchemaVersion = schemaVersion;
            m_Payload = (byte[])canonicalPayload.Clone();
            PayloadHash = SimulationCanonicalPayloadHash.Compute(m_Payload);
        }

        public ActorId ActorId { get; }
        public SimulationTick Tick { get; }
        public string ChannelId { get; }
        public string SchemaId { get; }
        public int SchemaVersion { get; }
        public StableHash PayloadHash { get; }
        public byte[] CopyPayload() => (byte[])m_Payload.Clone();
    }

    public sealed class FixedSimulationSessionSnapshot
    {
        public FixedSimulationSessionSnapshot(
            SimulationSessionCompositionIdentity compositionIdentity,
            SimulationWorldSnapshot world,
            SimulationPipelineStateSnapshot pipeline)
        {
            if (!compositionIdentity.IsValid)
                throw new ArgumentException("Session snapshot composition identity is invalid.", nameof(compositionIdentity));
            World = world ?? throw new ArgumentNullException(nameof(world));
            Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            if (pipeline.LastCompletedTick != world.Tick.Value)
                throw new ArgumentException("World and Pipeline snapshot Ticks do not match.");
            CompositionIdentity = compositionIdentity;
            SnapshotHash = StableHash.Compute(
                "fixed-session-snapshot/1",
                compositionIdentity.ToString(),
                world.WorldHash.ToString(),
                pipeline.SnapshotHash.ToString());
        }

        public SimulationSessionCompositionIdentity CompositionIdentity { get; }
        public SimulationWorldSnapshot World { get; }
        public SimulationPipelineStateSnapshot Pipeline { get; }
        public SimulationTick Tick => World.Tick;
        public StableHash SnapshotHash { get; }
    }

    public sealed class FixedSimulationStepSnapshot
    {
        public FixedSimulationStepSnapshot(
            SimulationSessionCompositionIdentity compositionIdentity,
            SimulationWorldSnapshot world,
            SimulationPipelineStateSnapshot pipelineProjection)
        {
            if (!compositionIdentity.IsValid)
                throw new ArgumentException("Step snapshot composition identity is invalid.", nameof(compositionIdentity));
            World = world ?? throw new ArgumentNullException(nameof(world));
            PipelineProjection = pipelineProjection ?? throw new ArgumentNullException(nameof(pipelineProjection));
            if (pipelineProjection.LastCompletedTick != world.Tick.Value)
                throw new ArgumentException("World snapshot and Pipeline projection Ticks do not match.");
            CompositionIdentity = compositionIdentity;
        }

        public SimulationSessionCompositionIdentity CompositionIdentity { get; }
        public SimulationWorldSnapshot World { get; }
        public SimulationPipelineStateSnapshot PipelineProjection { get; }
        public SimulationTick Tick => World.Tick;
    }

    public sealed class FixedSimulationCommitBatch
    {
        readonly ReadOnlyCollection<FixedCompletedSimulationStep> m_Steps;
        readonly ReadOnlyCollection<FixedSourceEgressRecord> m_SourceEgress;

        public FixedSimulationCommitBatch(
            StableHash transactionIdentity,
            IEnumerable<FixedCompletedSimulationStep> steps,
            SimulationPipelineOutputDispositionSet outputDispositions,
            IEnumerable<FixedSourceEgressRecord> sourceEgress)
        {
            if (!transactionIdentity.IsValid)
                throw new ArgumentException("Commit batch transaction identity is invalid.", nameof(transactionIdentity));
            OutputDispositions = outputDispositions ?? throw new ArgumentNullException(nameof(outputDispositions));
            if (!outputDispositions.TransactionIdentity.Equals(transactionIdentity))
                throw new ArgumentException("Commit batch and disposition transaction identities do not match.", nameof(outputDispositions));
            var stepValues = steps == null
                ? new List<FixedCompletedSimulationStep>()
                : new List<FixedCompletedSimulationStep>(steps);
            for (int i = 0; i < stepValues.Count; i++)
            {
                if (stepValues[i] == null || i > 0 && stepValues[i - 1].Step.Tick.CompareTo(stepValues[i].Step.Tick) >= 0)
                    throw new ArgumentException("Commit batch Step order is invalid.", nameof(steps));
            }
            var outputEvents = new List<OutputEventOwner>();
            for (int i = 0; i < stepValues.Count; i++)
            {
                SimulationTickResult result = stepValues[i].Result;
                for (int actorIndex = 0; actorIndex < result.Actors.Count; actorIndex++)
                {
                    SimulationActorTickResult actor = result.Actors[actorIndex];
                    for (int eventIndex = 0; eventIndex < actor.GameplayFacts.Count; eventIndex++)
                    {
                        outputEvents.Add(new OutputEventOwner(
                            actor.GameplayFacts[eventIndex].Header.EventId,
                            actor.ActorId));
                    }
                    for (int eventIndex = 0; eventIndex < actor.PresentationCommands.Count; eventIndex++)
                    {
                        outputEvents.Add(new OutputEventOwner(
                            actor.PresentationCommands[eventIndex].Header.EventId,
                            actor.ActorId));
                    }
                }
            }
            outputEvents.Sort((left, right) => left.EventId.CompareTo(right.EventId));
            if (outputEvents.Count != outputDispositions.Dispositions.Count)
                throw new ArgumentException("Commit batch dispositions do not cover every Step EventId.", nameof(outputDispositions));
            for (int i = 0; i < outputEvents.Count; i++)
            {
                if (!outputEvents[i].EventId.Equals(outputDispositions.Dispositions[i].SourceEventId) ||
                    !outputEvents[i].ActorId.Equals(outputDispositions.Dispositions[i].ActorId) ||
                    i > 0 && outputEvents[i - 1].EventId.Equals(outputEvents[i].EventId))
                {
                    throw new ArgumentException("Commit batch contains duplicate or undisposed EventIds.", nameof(outputDispositions));
                }
            }
            var egressValues = sourceEgress == null
                ? new List<FixedSourceEgressRecord>()
                : new List<FixedSourceEgressRecord>(sourceEgress);
            for (int i = 0; i < egressValues.Count; i++)
            {
                if (egressValues[i] == null)
                    throw new ArgumentException("Commit batch contains a missing Source egress record.", nameof(sourceEgress));
            }
            TransactionIdentity = transactionIdentity;
            m_Steps = stepValues.AsReadOnly();
            m_SourceEgress = egressValues.AsReadOnly();
        }

        public StableHash TransactionIdentity { get; }
        public IReadOnlyList<FixedCompletedSimulationStep> Steps => m_Steps;
        public SimulationPipelineOutputDispositionSet OutputDispositions { get; }
        public IReadOnlyList<FixedSourceEgressRecord> SourceEgress => m_SourceEgress;

        readonly struct OutputEventOwner
        {
            public OutputEventOwner(EventId eventId, ActorId actorId)
            {
                EventId = eventId;
                ActorId = actorId;
            }

            public EventId EventId { get; }
            public ActorId ActorId { get; }
        }
    }

    public interface IFixedSimulationCommitter
    {
        SimulationComponentIdentity Identity { get; }
        void Commit(FixedSimulationCommitBatch batch);
    }

    public interface IFixedSimulationRestoreSource : ISimulationRuntimePort
    {
        FixedSimulationSessionSnapshot GetRequiredSnapshot(SimulationRestoreDirective directive);
    }
}

