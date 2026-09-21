using ThirdPersonSimulation;
using System;
using System.Collections.Generic;

namespace ThirdPersonSimulation.Fixed
{
    public readonly struct FixedStepInput
    {
        public FixedStepInput(SimulationInput input)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public SimulationInput Input { get; }
    }

    public sealed class FixedCanonicalInputBatch
    {
        readonly IReadOnlyList<SimulationPipelineActorInput<FixedStepInput>> m_Inputs;

        internal FixedCanonicalInputBatch(
            SimulationTickSourceIdentity source,
            SimulationPipelineActorInput<FixedStepInput>[] inputs)
        {
            if (string.IsNullOrEmpty(source.ClockId) || source.SourceTick == 0)
                throw new ArgumentException("Canonical input batch source is incomplete.", nameof(source));
            if (inputs == null || inputs.Length == 0)
                throw new ArgumentException("Canonical input batch cannot be empty.", nameof(inputs));
            Array.Sort(inputs, (left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < inputs.Length; i++)
            {
                if (i > 0 && inputs[i - 1].ActorId.Equals(inputs[i].ActorId) ||
                    inputs[i].Value.Input == null ||
                    !inputs[i].Value.Input.TickSource.Equals(source))
                {
                    throw new ArgumentException("Canonical input batch contains duplicate Actors or another source clock.", nameof(inputs));
                }
            }
            Source = source;
            m_Inputs = inputs;
        }

        public SimulationTickSourceIdentity Source { get; }
        public IReadOnlyList<SimulationPipelineActorInput<FixedStepInput>> Inputs => m_Inputs;
    }

    public sealed class FixedTypedIngressBatch
    {
        readonly IReadOnlyList<SimulationPipelineTypedIngress<SimulationIngress>> m_Ingress;

        FixedTypedIngressBatch()
        {
            m_Ingress = Array.Empty<SimulationPipelineTypedIngress<SimulationIngress>>();
        }

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
            m_Ingress = values;
        }

        public static FixedTypedIngressBatch Empty { get; } = new FixedTypedIngressBatch();
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

        public static FixedSimulationStep FromOwnedInputs(
            SimulationTick tick,
            SimulationPipelineStepProvenance provenance,
            SimulationPipelineActorInput<FixedStepInput>[] inputs,
            ActorId[] actors,
            SimulationPipelineTypedIngress<SimulationIngress>[] ingress)
        {
            return new FixedSimulationStep(tick, provenance, inputs, actors, ingress);
        }

        FixedSimulationStep(
            SimulationTick tick,
            SimulationPipelineStepProvenance provenance,
            SimulationPipelineActorInput<FixedStepInput>[] inputs,
            ActorId[] actors,
            SimulationPipelineTypedIngress<SimulationIngress>[] ingress)
            : base(tick, provenance, inputs, actors, ingress)
        {
        }
    }

    public sealed class FixedCharacterEvaluationResultBatch
    {
        readonly FixedCharacterEvaluationResult[] m_Evaluations;

        internal FixedCharacterEvaluationResultBatch(int actorCount)
        {
            if (actorCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(actorCount));
            m_Evaluations = new FixedCharacterEvaluationResult[actorCount];
        }

        internal FixedCharacterEvaluationResultBatch Reset(
            SimulationTick tick,
            FixedCharacterEvaluationResult[] evaluations)
        {
            if (!tick.IsValid || evaluations == null || evaluations.Length != m_Evaluations.Length)
                throw new ArgumentException("Character evaluation result batch workspace is invalid.");
            for (int i = 0; i < evaluations.Length; i++)
            {
                if (evaluations[i] == null)
                    throw new ArgumentException("Character evaluation result batch contains a missing evaluation.", nameof(evaluations));
            }
            Array.Copy(evaluations, m_Evaluations, evaluations.Length);
            Array.Sort(m_Evaluations, (left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 0; i < m_Evaluations.Length; i++)
            {
                if (m_Evaluations[i].Tick != tick ||
                    i > 0 && m_Evaluations[i - 1].ActorId.Equals(m_Evaluations[i].ActorId))
                {
                    throw new ArgumentException("Character evaluation result batch identity is invalid.", nameof(evaluations));
                }
            }
            Tick = tick;
            return this;
        }

        public SimulationTick Tick { get; private set; }
        public IReadOnlyList<FixedCharacterEvaluationResult> Evaluations => m_Evaluations;

        internal void DiscardUnconsumed()
        {
            for (int i = 0; i < m_Evaluations.Length; i++)
                m_Evaluations[i].DiscardUnconsumed();
        }
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
        readonly IReadOnlyList<SimulationOutputDisposition> m_Dispositions;

        public SimulationPipelineOutputDispositionSet(
            StableHash transactionIdentity,
            IReadOnlyList<SimulationOutputDisposition> dispositions)
        {
            if (!transactionIdentity.IsValid)
                throw new ArgumentException("Output disposition transaction identity is invalid.", nameof(transactionIdentity));
            var values = dispositions == null || dispositions.Count == 0
                ? Array.Empty<SimulationOutputDisposition>()
                : new SimulationOutputDisposition[dispositions.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = dispositions[i];
            Array.Sort(values, (left, right) => left.SourceEventId.CompareTo(right.SourceEventId));
            for (int i = 1; i < values.Length; i++)
            {
                if (values[i - 1].SourceEventId.Equals(values[i].SourceEventId))
                    throw new ArgumentException("Output disposition set contains duplicate EventId ownership.", nameof(dispositions));
            }
            TransactionIdentity = transactionIdentity;
            m_Dispositions = values;
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
                "fixed-session-snapshot/2",
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
        readonly IReadOnlyList<FixedCompletedSimulationStep> m_Steps;
        readonly IReadOnlyList<FixedSourceEgressRecord> m_SourceEgress;

        public FixedSimulationCommitBatch(
            StableHash transactionIdentity,
            IReadOnlyList<FixedCompletedSimulationStep> steps,
            SimulationPipelineOutputDispositionSet outputDispositions,
            IReadOnlyList<FixedSourceEgressRecord> sourceEgress)
        {
            if (!transactionIdentity.IsValid)
                throw new ArgumentException("Commit batch transaction identity is invalid.", nameof(transactionIdentity));
            OutputDispositions = outputDispositions ?? throw new ArgumentNullException(nameof(outputDispositions));
            if (!outputDispositions.TransactionIdentity.Equals(transactionIdentity))
                throw new ArgumentException("Commit batch and disposition transaction identities do not match.", nameof(outputDispositions));
            var stepValues = steps == null || steps.Count == 0
                ? Array.Empty<FixedCompletedSimulationStep>()
                : new FixedCompletedSimulationStep[steps.Count];
            for (int i = 0; i < stepValues.Length; i++)
            {
                stepValues[i] = steps[i];
                if (stepValues[i] == null || i > 0 && stepValues[i - 1].Step.Tick.CompareTo(stepValues[i].Step.Tick) >= 0)
                    throw new ArgumentException("Commit batch Step order is invalid.", nameof(steps));
            }
            var outputEvents = outputDispositions.Dispositions.Count == 0
                ? Array.Empty<OutputEventOwner>()
                : new OutputEventOwner[outputDispositions.Dispositions.Count];
            int outputEventCount = 0;
            for (int i = 0; i < stepValues.Length; i++)
            {
                SimulationTickResult result = stepValues[i].Result;
                for (int actorIndex = 0; actorIndex < result.Actors.Count; actorIndex++)
                {
                    SimulationActorTickResult actor = result.Actors[actorIndex];
                    for (int eventIndex = 0; eventIndex < actor.GameplayFacts.Count; eventIndex++)
                    {
                        if (outputEventCount >= outputEvents.Length)
                            throw new ArgumentException("Commit batch dispositions do not cover every Step EventId.", nameof(outputDispositions));
                        outputEvents[outputEventCount++] = new OutputEventOwner(
                            actor.GameplayFacts[eventIndex].Header.EventId,
                            actor.ActorId);
                    }
                    for (int eventIndex = 0; eventIndex < actor.PresentationCommands.Count; eventIndex++)
                    {
                        if (outputEventCount >= outputEvents.Length)
                            throw new ArgumentException("Commit batch dispositions do not cover every Step EventId.", nameof(outputDispositions));
                        outputEvents[outputEventCount++] = new OutputEventOwner(
                            actor.PresentationCommands[eventIndex].Header.EventId,
                            actor.ActorId);
                    }
                }
            }
            if (outputEventCount != outputEvents.Length)
                throw new ArgumentException("Commit batch dispositions do not cover every Step EventId.", nameof(outputDispositions));
            Array.Sort(outputEvents, (left, right) => left.EventId.CompareTo(right.EventId));
            for (int i = 0; i < outputEvents.Length; i++)
            {
                if (!outputEvents[i].EventId.Equals(outputDispositions.Dispositions[i].SourceEventId) ||
                    !outputEvents[i].ActorId.Equals(outputDispositions.Dispositions[i].ActorId) ||
                    i > 0 && outputEvents[i - 1].EventId.Equals(outputEvents[i].EventId))
                {
                    throw new ArgumentException("Commit batch contains duplicate or undisposed EventIds.", nameof(outputDispositions));
                }
            }
            var egressValues = sourceEgress == null || sourceEgress.Count == 0
                ? Array.Empty<FixedSourceEgressRecord>()
                : new FixedSourceEgressRecord[sourceEgress.Count];
            for (int i = 0; i < egressValues.Length; i++)
            {
                egressValues[i] = sourceEgress[i];
                if (egressValues[i] == null)
                    throw new ArgumentException("Commit batch contains a missing Source egress record.", nameof(sourceEgress));
            }
            TransactionIdentity = transactionIdentity;
            m_Steps = stepValues;
            m_SourceEgress = egressValues;
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

