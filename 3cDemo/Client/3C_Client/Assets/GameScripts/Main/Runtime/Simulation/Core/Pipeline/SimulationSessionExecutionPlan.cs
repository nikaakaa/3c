using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    static class SimulationActorLookup
    {
        public static bool ContainsSorted(IReadOnlyList<ActorId> actors, ActorId value)
        {
            int lower = 0;
            int upper = actors.Count - 1;
            while (lower <= upper)
            {
                int middle = lower + ((upper - lower) >> 1);
                int comparison = actors[middle].CompareTo(value);
                if (comparison == 0)
                    return true;
                if (comparison < 0)
                    lower = middle + 1;
                else
                    upper = middle - 1;
            }
            return false;
        }
    }

    public enum SimulationSessionExecutionPlanStatus : byte
    {
        Pending = 1,
        Executable = 2,
        NoStep = 3
    }

    public enum SimulationPipelineStepExecutionKind : byte
    {
        Forward = 1,
        Replay = 2,
        Current = 3,
        Authoritative = 4
    }

    [Flags]
    public enum SimulationSessionPlanRequirement : byte
    {
        None = 0,
        WorkingState = 1 << 0,
        Snapshot = 1 << 1,
        OutputDisposition = 1 << 2,
        StateHash = 1 << 3
    }

    public sealed class SimulationRestoreDirective
    {
        public SimulationRestoreDirective(
            string snapshotId,
            SimulationTick tick,
            ProgramCatalogHash programCatalogHash,
            SimulationPipelineHash pipelineHash,
            string backendId,
            string backendSemanticVersion,
            StableHash snapshotHash)
        {
            if (!tick.IsValid || !programCatalogHash.IsValid || !pipelineHash.IsValid || !snapshotHash.IsValid)
                throw new ArgumentException("Restore directive identity is incomplete.");
            SnapshotId = SimulationIdentity.Require(snapshotId, nameof(snapshotId));
            Tick = tick;
            ProgramCatalogHash = programCatalogHash;
            PipelineHash = pipelineHash;
            BackendId = SimulationIdentity.Require(backendId, nameof(backendId));
            BackendSemanticVersion = SimulationIdentity.Require(backendSemanticVersion, nameof(backendSemanticVersion));
            SnapshotHash = snapshotHash;
        }

        public string SnapshotId { get; }
        public SimulationTick Tick { get; }
        public ProgramCatalogHash ProgramCatalogHash { get; }
        public SimulationPipelineHash PipelineHash { get; }
        public string BackendId { get; }
        public string BackendSemanticVersion { get; }
        public StableHash SnapshotHash { get; }
    }

    public readonly struct SimulationPipelineStepProvenance
    {
        public SimulationPipelineStepProvenance(
            SimulationPipelineStepExecutionKind executionKind,
            SimulationTickSourceIdentity source,
            ulong planSequence,
            string baselineIdentity = "")
        {
            if (!Enum.IsDefined(typeof(SimulationPipelineStepExecutionKind), executionKind) ||
                string.IsNullOrEmpty(source.ClockId) || source.SourceTick == 0 || planSequence == 0)
            {
                throw new ArgumentException("Pipeline Step provenance is incomplete.");
            }
            ExecutionKind = executionKind;
            Source = source;
            PlanSequence = planSequence;
            BaselineIdentity = baselineIdentity ?? string.Empty;
        }

        public SimulationPipelineStepExecutionKind ExecutionKind { get; }
        public SimulationTickSourceIdentity Source { get; }
        public ulong PlanSequence { get; }
        public string BaselineIdentity { get; }
    }

    public readonly struct SimulationPipelineStepSourceMapping
    {
        public SimulationPipelineStepSourceMapping(
            string stepClockId,
            string outerClockId,
            SimulationTickSourceKind sourceKind)
        {
            if (!Enum.IsDefined(typeof(SimulationTickSourceKind), sourceKind))
                throw new ArgumentOutOfRangeException(nameof(sourceKind));
            StepClockId = SimulationIdentity.Require(stepClockId, nameof(stepClockId));
            OuterClockId = SimulationIdentity.Require(outerClockId, nameof(outerClockId));
            SourceKind = sourceKind;
        }

        public string StepClockId { get; }
        public string OuterClockId { get; }
        public SimulationTickSourceKind SourceKind { get; }
    }

    public abstract class SimulationPipelineStep
    {
        readonly ReadOnlyCollection<ActorId> m_Actors;

        protected SimulationPipelineStep(
            SimulationTick tick,
            SimulationPipelineStepProvenance provenance,
            IEnumerable<ActorId> actors)
        {
            if (!tick.IsValid || string.IsNullOrEmpty(provenance.Source.ClockId) || provenance.Source.SourceTick == 0 ||
                !Enum.IsDefined(typeof(SimulationPipelineStepExecutionKind), provenance.ExecutionKind))
            {
                throw new ArgumentException("Pipeline Step identity is incomplete.");
            }
            var values = actors == null ? new List<ActorId>() : new List<ActorId>(actors);
            values.Sort();
            if (values.Count == 0)
                throw new ArgumentException("Pipeline Step must contain at least one Actor.", nameof(actors));
            for (int i = 0; i < values.Count; i++)
            {
                if (!values[i].IsValid || i > 0 && values[i - 1].Equals(values[i]))
                    throw new ArgumentException("Pipeline Step contains an invalid or duplicate ActorId.", nameof(actors));
            }
            Tick = tick;
            Provenance = provenance;
            m_Actors = values.AsReadOnly();
        }

        public SimulationTick Tick { get; }
        public SimulationPipelineStepProvenance Provenance { get; }
        public SimulationTickSourceIdentity Source => Provenance.Source;
        public SimulationPipelineStepExecutionKind ExecutionKind => Provenance.ExecutionKind;
        public IReadOnlyList<ActorId> Actors => m_Actors;
    }

    public readonly struct SimulationPipelineActorInput<TInput>
    {
        public SimulationPipelineActorInput(ActorId actorId, ulong sequence, TInput value)
        {
            if (!actorId.IsValid || sequence == 0)
                throw new ArgumentException("Pipeline Actor input identity is incomplete.");
            ActorId = actorId;
            Sequence = sequence;
            Value = value;
        }

        public ActorId ActorId { get; }
        public ulong Sequence { get; }
        public TInput Value { get; }
    }

    public readonly struct SimulationPipelineTypedIngress<TIngress>
    {
        public SimulationPipelineTypedIngress(
            ActorId actorId,
            SimulationTickSourceIdentity source,
            ulong sequence,
            string factIdentity,
            TIngress value)
        {
            if (!actorId.IsValid || string.IsNullOrEmpty(source.ClockId) || source.SourceTick == 0 || sequence == 0)
                throw new ArgumentException("Typed ingress identity is incomplete.");
            ActorId = actorId;
            Source = source;
            Sequence = sequence;
            FactIdentity = SimulationIdentity.Require(factIdentity, nameof(factIdentity));
            Value = value;
        }

        public ActorId ActorId { get; }
        public SimulationTickSourceIdentity Source { get; }
        public ulong Sequence { get; }
        public string FactIdentity { get; }
        public TIngress Value { get; }
    }

    public class TargetSimulationPipelineStep<TInput, TIngress> : SimulationPipelineStep
    {
        readonly ReadOnlyCollection<SimulationPipelineActorInput<TInput>> m_Inputs;
        readonly ReadOnlyCollection<SimulationPipelineTypedIngress<TIngress>> m_Ingress;

        public TargetSimulationPipelineStep(
            SimulationTick tick,
            SimulationPipelineStepProvenance provenance,
            IEnumerable<SimulationPipelineActorInput<TInput>> inputs,
            IEnumerable<SimulationPipelineTypedIngress<TIngress>> ingress)
            : this(tick, provenance, MaterializeInputs(inputs), ingress)
        {
        }

        TargetSimulationPipelineStep(
            SimulationTick tick,
            SimulationPipelineStepProvenance provenance,
            List<SimulationPipelineActorInput<TInput>> inputValues,
            IEnumerable<SimulationPipelineTypedIngress<TIngress>> ingress)
            : base(tick, provenance, CollectActors(inputValues))
        {
            inputValues.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            for (int i = 1; i < inputValues.Count; i++)
            {
                if (inputValues[i - 1].ActorId.Equals(inputValues[i].ActorId))
                    throw new ArgumentException("Pipeline Step contains duplicate Actor input.", nameof(inputValues));
            }
            var ingressValues = ingress == null
                ? new List<SimulationPipelineTypedIngress<TIngress>>()
                : new List<SimulationPipelineTypedIngress<TIngress>>(ingress);
            ingressValues.Sort((left, right) =>
            {
                int actor = left.ActorId.CompareTo(right.ActorId);
                if (actor != 0)
                    return actor;
                int sourceTick = left.Source.SourceTick.CompareTo(right.Source.SourceTick);
                if (sourceTick != 0)
                    return sourceTick;
                int sequence = left.Sequence.CompareTo(right.Sequence);
                return sequence != 0 ? sequence : string.CompareOrdinal(left.FactIdentity, right.FactIdentity);
            });
            for (int i = 0; i < ingressValues.Count; i++)
            {
                if (!SimulationActorLookup.ContainsSorted(Actors, ingressValues[i].ActorId))
                    throw new ArgumentException($"Typed ingress targets Actor '{ingressValues[i].ActorId}' without Step input.", nameof(ingress));
                if (i > 0 && ingressValues[i - 1].ActorId.Equals(ingressValues[i].ActorId) &&
                    ingressValues[i - 1].Source.Equals(ingressValues[i].Source) &&
                    ingressValues[i - 1].Sequence == ingressValues[i].Sequence &&
                    string.Equals(ingressValues[i - 1].FactIdentity, ingressValues[i].FactIdentity, StringComparison.Ordinal))
                {
                    throw new ArgumentException("Pipeline Step contains duplicate typed ingress.", nameof(ingress));
                }
            }
            m_Inputs = inputValues.AsReadOnly();
            m_Ingress = ingressValues.AsReadOnly();
        }

        public IReadOnlyList<SimulationPipelineActorInput<TInput>> Inputs => m_Inputs;
        public IReadOnlyList<SimulationPipelineTypedIngress<TIngress>> Ingress => m_Ingress;

        static List<SimulationPipelineActorInput<TInput>> MaterializeInputs(
            IEnumerable<SimulationPipelineActorInput<TInput>> inputs)
        {
            if (inputs == null)
                throw new ArgumentNullException(nameof(inputs));
            return new List<SimulationPipelineActorInput<TInput>>(inputs);
        }

        static IEnumerable<ActorId> CollectActors(IReadOnlyList<SimulationPipelineActorInput<TInput>> inputs)
        {
            var actors = new List<ActorId>();
            for (int i = 0; i < inputs.Count; i++)
                actors.Add(inputs[i].ActorId);
            return actors;
        }
    }

    public sealed class SimulationSessionExecutionPlan<TStep> where TStep : SimulationPipelineStep
    {
        readonly ReadOnlyCollection<TStep> m_Steps;
        readonly ReadOnlyCollection<SimulationPipelineStepSourceMapping> m_SourceMappings;

        public SimulationSessionExecutionPlan(
            SimulationSessionExecutionPlanStatus status,
            SimulationTickSourceIdentity outerSource,
            ProgramCatalogHash programCatalogHash,
            SimulationPipelineHash pipelineHash,
            SimulationActorRosterDescriptor roster,
            IEnumerable<SimulationPipelineStepSourceMapping> sourceMappings,
            SimulationRestoreDirective restore,
            IEnumerable<TStep> steps,
            SimulationSessionPlanRequirement requirements)
        {
            if (!Enum.IsDefined(typeof(SimulationSessionExecutionPlanStatus), status) ||
                string.IsNullOrEmpty(outerSource.ClockId) || outerSource.SourceTick == 0 ||
                !programCatalogHash.IsValid || !pipelineHash.IsValid || roster == null)
            {
                throw new ArgumentException("Session ExecutionPlan identity is incomplete.");
            }
            var values = steps == null ? new List<TStep>() : new List<TStep>(steps);
            m_SourceMappings = FreezeMappings(sourceMappings, outerSource.ClockId);
            if (status == SimulationSessionExecutionPlanStatus.Pending)
            {
                if (restore != null || values.Count != 0 || requirements != SimulationSessionPlanRequirement.None)
                    throw new ArgumentException("Pending ExecutionPlan cannot contain restore, steps or execution requirements.");
            }
            else if (status == SimulationSessionExecutionPlanStatus.NoStep)
            {
                const SimulationSessionPlanRequirement required =
                    SimulationSessionPlanRequirement.WorkingState |
                    SimulationSessionPlanRequirement.OutputDisposition;
                if (values.Count != 0 || (requirements & required) != required)
                    throw new ArgumentException("NoStep ExecutionPlan requires working state and output disposition without ordered Steps.");
            }
            else
            {
                const SimulationSessionPlanRequirement required =
                    SimulationSessionPlanRequirement.WorkingState |
                    SimulationSessionPlanRequirement.OutputDisposition;
                if (values.Count == 0 || (requirements & required) != required)
                    throw new ArgumentException("Executable ExecutionPlan requires ordered Steps, working state and output disposition.");
                for (int i = 0; i < values.Count; i++)
                {
                    if (values[i] == null || i > 0 && values[i - 1].Tick.CompareTo(values[i].Tick) >= 0)
                        throw new ArgumentException("ExecutionPlan Steps must have strictly increasing SimulationTick values.", nameof(steps));
                    RequireSourceMapping(values[i], m_SourceMappings);
                    RequireRosterBinding(values[i], roster);
                    if (i > 0 && values[i - 1].Provenance.PlanSequence >= values[i].Provenance.PlanSequence)
                        throw new ArgumentException("ExecutionPlan Step provenance sequence must be strictly increasing.", nameof(steps));
                }
            }
            if (values.Count > 0 && restore != null && restore.Tick.CompareTo(values[0].Tick) >= 0)
                throw new ArgumentException("Restore Tick must precede the first ExecutionPlan Step.", nameof(restore));
            Status = status;
            OuterSource = outerSource;
            ProgramCatalogHash = programCatalogHash;
            PipelineHash = pipelineHash;
            RosterHash = roster.RosterHash;
            Restore = restore;
            Requirements = requirements;
            m_Steps = values.AsReadOnly();
        }

        public SimulationSessionExecutionPlanStatus Status { get; }
        public SimulationTickSourceIdentity OuterSource { get; }
        public ProgramCatalogHash ProgramCatalogHash { get; }
        public SimulationPipelineHash PipelineHash { get; }
        public StableHash RosterHash { get; }
        public IReadOnlyList<SimulationPipelineStepSourceMapping> SourceMappings => m_SourceMappings;
        public SimulationRestoreDirective Restore { get; }
        public SimulationSessionPlanRequirement Requirements { get; }
        public IReadOnlyList<TStep> Steps => m_Steps;

        static ReadOnlyCollection<SimulationPipelineStepSourceMapping> FreezeMappings(
            IEnumerable<SimulationPipelineStepSourceMapping> source,
            string outerClockId)
        {
            var values = source == null
                ? new List<SimulationPipelineStepSourceMapping>()
                : new List<SimulationPipelineStepSourceMapping>(source);
            values.Sort((left, right) =>
            {
                int clock = string.CompareOrdinal(left.StepClockId, right.StepClockId);
                return clock != 0 ? clock : left.SourceKind.CompareTo(right.SourceKind);
            });
            for (int i = 0; i < values.Count; i++)
            {
                if (!string.Equals(values[i].OuterClockId, outerClockId, StringComparison.Ordinal) ||
                    i > 0 && string.Equals(values[i - 1].StepClockId, values[i].StepClockId, StringComparison.Ordinal) &&
                    values[i - 1].SourceKind == values[i].SourceKind)
                {
                    throw new ArgumentException("ExecutionPlan contains an invalid or duplicate source clock mapping.", nameof(source));
                }
            }
            return values.AsReadOnly();
        }

        static void RequireSourceMapping(TStep step, IReadOnlyList<SimulationPipelineStepSourceMapping> mappings)
        {
            for (int i = 0; i < mappings.Count; i++)
            {
                if (string.Equals(mappings[i].StepClockId, step.Source.ClockId, StringComparison.Ordinal) &&
                    mappings[i].SourceKind == step.Source.Kind)
                    return;
            }
            throw new ArgumentException($"ExecutionPlan Step source '{step.Source.Kind}:{step.Source.ClockId}' has no explicit outer clock mapping.");
        }

        static void RequireRosterBinding(TStep step, SimulationActorRosterDescriptor roster)
        {
            for (int i = 0; i < step.Actors.Count; i++)
            {
                if (!SimulationActorLookup.ContainsSorted(roster.Actors, step.Actors[i]))
                    throw new ArgumentException($"ExecutionPlan Step targets unknown Actor '{step.Actors[i]}'.");
            }
        }
    }
}
