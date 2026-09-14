using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    public static class Float32PipelineRuntimePortIds
    {
        public const string CharacterRuntime = "simulation.target.float32-character-runtime";
        public const string CharacterRuntimeSchema = "float32-character-runtime-services";
        public const string WorkingState = "simulation.target.float32-working-state";
        public const string WorkingStateSchema = "float32-working-state-read";
        public const string CompletedSteps = "simulation.target.float32-completed-steps";
        public const string CompletedStepsSchema = "float32-completed-step-read";
        public const string CommittedObservation = "simulation.target.float32-committed-actor-observation";
        public const string CommittedObservationSchema = "committed-actor-pose-observation";
        public const string WorldSolver = "simulation.solver.float32-world";
        public const string WorldSolverSchema = "float32-world-solver";
        public const string Diagnostics = "simulation.diagnostics.session";
        public const string DiagnosticsSchema = "simulation-diagnostics-sink";
    }

    public sealed class Float32CharacterRuntime
    {
        readonly ReadOnlyCollection<SimulationActorBinding> m_Roster;

        public Float32CharacterRuntime(IEnumerable<SimulationActorBinding> roster)
        {
            var values = roster == null
                ? new List<SimulationActorBinding>()
                : new List<SimulationActorBinding>(roster);
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (values.Count == 0)
                throw new ArgumentException("Float32 Character Runtime roster cannot be empty.", nameof(roster));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || i > 0 && values[i - 1].ActorId == values[i].ActorId)
                    throw new ArgumentException("Float32 Character Runtime roster contains a null or duplicate ActorId.", nameof(roster));
            }
            m_Roster = values.AsReadOnly();
            RosterDescriptor = new SimulationActorRosterDescriptor(ActorIds(values));
            var parts = new List<string>
            {
                "float32-character-runtime/1",
                RosterDescriptor.RosterHash.ToString()
            };
            for (int i = 0; i < values.Count; i++)
                parts.Add(values[i].GameplayContentHash.ToString());
            GameplayContentHash = new GameplayContentHash(StableHash.Compute(parts.ToArray()));
        }

        public IReadOnlyList<SimulationActorBinding> Roster => m_Roster;
        public SimulationActorRosterDescriptor RosterDescriptor { get; }
        public GameplayContentHash GameplayContentHash { get; }

        static ActorId[] ActorIds(IReadOnlyList<SimulationActorBinding> values)
        {
            var result = new ActorId[values.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = values[i].ActorId;
            return result;
        }
    }

    public interface IFloat32CharacterRuntimePort : ISimulationRuntimePort
    {
        Float32CharacterRuntime Runtime { get; }
        GameplayContentHash GameplayContentHash { get; }
        IReadOnlyList<SimulationActorBinding> Roster { get; }
        SimulationActorRosterDescriptor RosterDescriptor { get; }
        int GetActorIndex(ActorId actorId);
        CharacterControlRuntimeBinding GetControlRuntimeBinding(int actorIndex);
        CharacterBodyMotionBinding GetBodyMotionBinding(int actorIndex);
        CharacterGameplayEffectRuntimeBinding GetGameplayEffectRuntimeBinding(int actorIndex);
        CharacterEquipmentRuntimeBinding GetEquipmentRuntimeBinding(int actorIndex);
        GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> GetAbilityData(int actorIndex);
        Float32GameplayAbilityExecutionInstallationSet GetAbilityInstallations(int actorIndex);
    }

    public sealed class Float32CharacterRuntimePort : IFloat32CharacterRuntimePort
    {
        readonly Dictionary<ActorId, int> m_ActorIndices = new Dictionary<ActorId, int>();

        public Float32CharacterRuntimePort(
            SimulationComponentIdentity backend,
            Float32CharacterRuntime runtime)
        {
            if (!backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Execution Backend identity is invalid.", nameof(backend));
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            for (int i = 0; i < Runtime.Roster.Count; i++)
                m_ActorIndices.Add(Runtime.Roster[i].ActorId, i);
            Descriptor = Float32PipelineRuntimePortDescriptor.Create(
                Float32PipelineRuntimePortIds.CharacterRuntime,
                Float32PipelineRuntimePortIds.CharacterRuntimeSchema,
                backend.ComponentId,
                StableHash.Compute(backend.ToString(), Runtime.GameplayContentHash.ToString()),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public Float32CharacterRuntime Runtime { get; }
        public GameplayContentHash GameplayContentHash => Runtime.GameplayContentHash;
        public IReadOnlyList<SimulationActorBinding> Roster => Runtime.Roster;
        public SimulationActorRosterDescriptor RosterDescriptor => Runtime.RosterDescriptor;

        public int GetActorIndex(ActorId actorId) =>
            m_ActorIndices.TryGetValue(actorId, out int index)
                ? index
                : throw new InvalidOperationException($"Actor '{actorId}' is not part of the locked Character Runtime roster.");

        public CharacterControlRuntimeBinding GetControlRuntimeBinding(int actorIndex) =>
            Roster[actorIndex].ControlRuntimeBinding;

        public CharacterBodyMotionBinding GetBodyMotionBinding(int actorIndex) =>
            Roster[actorIndex].BodyMotionBinding;

        public CharacterGameplayEffectRuntimeBinding GetGameplayEffectRuntimeBinding(int actorIndex) =>
            Roster[actorIndex].GameplayEffectRuntimeBinding;

        public CharacterEquipmentRuntimeBinding GetEquipmentRuntimeBinding(int actorIndex) =>
            Roster[actorIndex].EquipmentRuntimeBinding;

        public GameplayAbilityExecutionDataSet<Float32GameplayAbilityExecutionData> GetAbilityData(int actorIndex) =>
            Roster[actorIndex].AbilityData;

        public Float32GameplayAbilityExecutionInstallationSet GetAbilityInstallations(int actorIndex) =>
            Roster[actorIndex].AbilityInstallations;
    }

    public interface IFloat32WorkingStateReadPort : ISimulationRuntimePort
    {
        SimulationWorldStateSet Current { get; }
        Float32SimulationStep Step { get; }
    }

    public sealed class Float32WorkingStatePort : IFloat32WorkingStateReadPort
    {
        public Float32WorkingStatePort(SimulationComponentIdentity backend)
        {
            if (!backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Execution Backend identity is invalid.", nameof(backend));
            Descriptor = Float32PipelineRuntimePortDescriptor.Create(
                Float32PipelineRuntimePortIds.WorkingState,
                Float32PipelineRuntimePortIds.WorkingStateSchema,
                backend.ComponentId,
                StableHash.Compute(backend.ToString(), "working-state/1"),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public SimulationWorldStateSet Current { get; private set; }
        public Float32SimulationStep Step { get; private set; }

        internal void Set(SimulationWorldStateSet current, Float32SimulationStep step)
        {
            Current = current ?? throw new ArgumentNullException(nameof(current));
            Step = step ?? throw new ArgumentNullException(nameof(step));
            if (current.Actors.Count != step.Actors.Count)
                throw new InvalidOperationException("Working state and Step rosters do not match.");
            for (int i = 0; i < current.Actors.Count; i++)
            {
                if (!current.Actors[i].ActorId.Equals(step.Actors[i]))
                    throw new InvalidOperationException("Working state and Step Actor order do not match.");
            }
        }

        internal void Clear()
        {
            Current = null;
            Step = null;
        }
    }

    public interface IFloat32CompletedStepReadPort : ISimulationRuntimePort
    {
        IReadOnlyList<Float32CompletedSimulationStep> Steps { get; }
    }

    public sealed class Float32CompletedStepPort : IFloat32CompletedStepReadPort
    {
        IReadOnlyList<Float32CompletedSimulationStep> m_Steps = Array.Empty<Float32CompletedSimulationStep>();

        public Float32CompletedStepPort(SimulationComponentIdentity backend)
        {
            if (!backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Execution Backend identity is invalid.", nameof(backend));
            Descriptor = Float32PipelineRuntimePortDescriptor.Create(
                Float32PipelineRuntimePortIds.CompletedSteps,
                Float32PipelineRuntimePortIds.CompletedStepsSchema,
                backend.ComponentId,
                StableHash.Compute(backend.ToString(), "completed-steps/1"),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public IReadOnlyList<Float32CompletedSimulationStep> Steps => m_Steps;

        internal void Set(IReadOnlyList<Float32CompletedSimulationStep> steps) =>
            m_Steps = steps ?? throw new ArgumentNullException(nameof(steps));

        internal void Clear() => m_Steps = Array.Empty<Float32CompletedSimulationStep>();
    }

    public interface IFloat32WorldSolverRuntimePort : ISimulationRuntimePort
    {
        ICharacterWorldSolver Solver { get; }
    }

    public sealed class Float32WorldSolverRuntimePort : IFloat32WorldSolverRuntimePort
    {
        public Float32WorldSolverRuntimePort(
            SimulationComponentIdentity worldSolver,
            ICharacterWorldSolver solver)
        {
            if (!worldSolver.IsValid || worldSolver.Role != SimulationComponentRole.WorldSolver)
                throw new ArgumentException("World Solver component identity is invalid.", nameof(worldSolver));
            Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            Descriptor = Float32PipelineRuntimePortDescriptor.Create(
                Float32PipelineRuntimePortIds.WorldSolver,
                Float32PipelineRuntimePortIds.WorldSolverSchema,
                worldSolver.ComponentId,
                StableHash.Compute(worldSolver.ToString(), solver.Descriptor.ImplementationId.Value, solver.Descriptor.Version),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public ICharacterWorldSolver Solver { get; }
    }

    public interface IFloat32DiagnosticsRuntimePort : ISimulationRuntimePort
    {
        ISimulationDiagnosticsSink Sink { get; }
    }

    public sealed class Float32DiagnosticsRuntimePort : IFloat32DiagnosticsRuntimePort
    {
        public Float32DiagnosticsRuntimePort(
            SimulationComponentIdentity diagnostics,
            ISimulationDiagnosticsSink sink)
        {
            if (!diagnostics.IsValid || diagnostics.Role != SimulationComponentRole.Diagnostics)
                throw new ArgumentException("Diagnostics component identity is invalid.", nameof(diagnostics));
            Sink = sink ?? throw new ArgumentNullException(nameof(sink));
            Descriptor = Float32PipelineRuntimePortDescriptor.Create(
                Float32PipelineRuntimePortIds.Diagnostics,
                Float32PipelineRuntimePortIds.DiagnosticsSchema,
                diagnostics.ComponentId,
                diagnostics.ConfigurationHash,
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public ISimulationDiagnosticsSink Sink { get; }
    }

    static class Float32PipelineRuntimePortDescriptor
    {
        public static SimulationPortDescriptor Create(
            string portId,
            string schemaId,
            string owner,
            StableHash configurationHash,
            SimulationPortDirection direction) =>
            new SimulationPortDescriptor(portId, schemaId, 1, direction, owner, configurationHash);
    }
}
