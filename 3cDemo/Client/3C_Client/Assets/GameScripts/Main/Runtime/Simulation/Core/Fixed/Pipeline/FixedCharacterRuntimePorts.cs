using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public static class FixedPipelineRuntimePortIds
    {
        public const string CharacterRuntime = "simulation.target.fixed-character-runtime";
        public const string CharacterRuntimeSchema = "fixed-character-runtime-services";
        public const string WorkingState = "simulation.target.fixed-working-state";
        public const string WorkingStateSchema = "fixed-working-state-read";
        public const string CompletedSteps = "simulation.target.fixed-completed-steps";
        public const string CompletedStepsSchema = "fixed-completed-step-read";
        public const string CommittedObservation = "simulation.target.fixed-committed-actor-observation";
        public const string CommittedObservationSchema = "committed-actor-pose-observation";
        public const string WorldSolver = "simulation.solver.fixed-world";
        public const string WorldSolverSchema = "fixed-world-solver";
        public const string Diagnostics = "simulation.diagnostics.session";
        public const string DiagnosticsSchema = "simulation-diagnostics-sink";
    }

    public sealed class FixedCharacterRuntime
    {
        readonly ReadOnlyCollection<SimulationActorBinding> m_Roster;
        readonly ReadOnlyCollection<FixedGameplayAbilityExecutionData> m_Abilities;
        readonly ReadOnlyCollection<string> m_InputRequestIds;

        public FixedCharacterRuntime(
            IEnumerable<SimulationActorBinding> roster,
            SimulationNumericProfile numericProfile,
            int tickRate,
            OperationSetVersion operationSetVersion,
            CharacterControlModuleCatalog controlModules)
        {
            if (!numericProfile.IsValid || tickRate <= 0 || !operationSetVersion.IsValid)
                throw new ArgumentException("Fixed Character Runtime execution identity is incomplete.");
            ControlModules = controlModules ?? throw new ArgumentNullException(nameof(controlModules));
            var values = roster == null
                ? new List<SimulationActorBinding>()
                : new List<SimulationActorBinding>(roster);
            values.Sort((left, right) => left.ActorId.CompareTo(right.ActorId));
            if (values.Count == 0)
                throw new ArgumentException("Fixed Character Runtime roster cannot be empty.", nameof(roster));
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null || i > 0 && values[i - 1].ActorId == values[i].ActorId)
                    throw new ArgumentException("Fixed Character Runtime roster contains a null or duplicate ActorId.", nameof(roster));
            }
            m_Roster = values.AsReadOnly();
            RosterDescriptor = new SimulationActorRosterDescriptor(ActorIds(values));
            var abilities = new Dictionary<CharacterSkillId, FixedGameplayAbilityExecutionData>();
            for (int actorIndex = 0; actorIndex < values.Count; actorIndex++)
            {
                IReadOnlyList<FixedGameplayAbilityExecutionData> actorAbilities = values[actorIndex].AbilityData.Data;
                for (int abilityIndex = 0; abilityIndex < actorAbilities.Count; abilityIndex++)
                {
                    FixedGameplayAbilityExecutionData ability = actorAbilities[abilityIndex];
                    if (abilities.TryGetValue(ability.AbilityId, out FixedGameplayAbilityExecutionData existing))
                    {
                        if (!existing.ContentHash.Equals(ability.ContentHash) ||
                            !existing.StateSchemaHash.Equals(ability.StateSchemaHash) ||
                            !existing.OperationSetVersion.Equals(ability.OperationSetVersion) ||
                            !existing.NumericProfile.Equals(ability.NumericProfile) ||
                            existing.TickRate != ability.TickRate)
                        {
                            throw new InvalidOperationException($"Ability '{ability.AbilityId}' has different execution identity across Character Runtime bindings.");
                        }
                    }
                    else
                    {
                        abilities.Add(ability.AbilityId, ability);
                    }
                }
            }
            var abilityValues = new List<FixedGameplayAbilityExecutionData>(abilities.Values);
            abilityValues.Sort((left, right) => left.AbilityId.CompareTo(right.AbilityId));
            m_Abilities = abilityValues.AsReadOnly();
            AbilitySetSourceRevision = ComputeAbilitySetSourceRevision(m_Abilities);
            NumericProfile = numericProfile;
            TickRate = tickRate;
            OperationSetVersion = operationSetVersion;
            WorldCapability requiredWorldCapabilities = WorldCapability.None;
            for (int i = 0; i < values.Count; i++)
                requiredWorldCapabilities |= values[i].BodyMotionBinding.RequiredWorldCapability;
            for (int i = 0; i < m_Abilities.Count; i++)
            {
                FixedGameplayAbilityExecutionData ability = m_Abilities[i];
                if (ability.NumericProfile != NumericProfile || ability.TickRate != TickRate ||
                    !ability.OperationSetVersion.Equals(OperationSetVersion))
                {
                    throw new InvalidOperationException("Fixed Character Runtime Ability data uses inconsistent execution identities.");
                }
                requiredWorldCapabilities |= ability.Capabilities.RequiredWorldCapabilities;
            }
            RequiredWorldCapabilities = requiredWorldCapabilities;
            var requestIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < m_Abilities.Count; i++)
            {
                GameplayAbilityExecutionLayout layout = FixedGameplayAbilityExecutionLayoutFactory.Create(m_Abilities[i]);
                for (int requestIndex = 0; requestIndex < layout.InputRequestIds.Count; requestIndex++)
                    requestIds.Add(layout.InputRequestIds[requestIndex]);
            }
            var sortedRequestIds = new List<string>(requestIds);
            sortedRequestIds.Sort(StringComparer.Ordinal);
            m_InputRequestIds = sortedRequestIds.AsReadOnly();
            var parts = new List<string>
            {
                "fixed-character-runtime/1",
                RosterDescriptor.RosterHash.ToString()
            };
            for (int i = 0; i < values.Count; i++)
                parts.Add(values[i].GameplayContentHash.ToString());
            GameplayContentHash = new GameplayContentHash(StableHash.Compute(parts.ToArray()));
        }

        public IReadOnlyList<SimulationActorBinding> Roster => m_Roster;
        public IReadOnlyList<FixedGameplayAbilityExecutionData> Abilities => m_Abilities;
        public SimulationActorRosterDescriptor RosterDescriptor { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public int TickRate { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public CharacterControlModuleCatalog ControlModules { get; }
        public WorldCapability RequiredWorldCapabilities { get; }
        public IReadOnlyList<string> InputRequestIds => m_InputRequestIds;
        public GameplayContentHash GameplayContentHash { get; }
        public string AbilitySetSourceRevision { get; }

        public FixedCharacterRuntimeState CreateInitialState(int actorIndex)
        {
            if (actorIndex < 0 || actorIndex >= Roster.Count)
                throw new ArgumentOutOfRangeException(nameof(actorIndex));
            SimulationActorBinding actor = Roster[actorIndex];
            CharacterControlModuleContract control = ControlModules.RequireContract(actor.ControlRuntimeBinding.ModuleId);
            CharacterControlRuntimeState controlState = CharacterControlRuntimeState.CreateInitial(
                actor.ControlRuntimeBinding,
                control);
            FixedGameplayEffectRuntimeCatalog effectCatalog = !actor.RequiresGameplayEffects || actor.GameplayEffectRuntimeBinding == null
                ? null
                : new FixedGameplayEffectRuntimeCatalog(actor.GameplayEffectRuntimeBinding);
            GameplayEffectStateAggregate effectState = effectCatalog == null
                ? null
                : GameplayEffectStateAggregate.CreateInitial(effectCatalog);
            EquipmentStateAggregate equipmentState = null;
            if (actor.RequiresEquipment)
            {
                EquipmentProgramLayout layout = EquipmentProgramLayoutCompiler.CompileRoleStateLayout(actor.EquipmentRuntimeBinding);
                equipmentState = EquipmentStateAggregate.CreateInitial(layout);
            }
            return FixedCharacterRuntimeState.CreateInitial(
                actor.AbilityInstallations,
                NumericProfile,
                new GameplayContentHash(actor.GameplayContentHash),
                controlState,
                effectState,
                equipmentState);
        }

        static ActorId[] ActorIds(IReadOnlyList<SimulationActorBinding> values)
        {
            var result = new ActorId[values.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = values[i].ActorId;
            return result;
        }

        static string ComputeAbilitySetSourceRevision(
            IReadOnlyList<FixedGameplayAbilityExecutionData> abilities)
        {
            var parts = new List<string> { "fixed-ability-set-revision/1" };
            for (int i = 0; i < abilities.Count; i++)
            {
                FixedGameplayAbilityExecutionData ability = abilities[i];
                parts.Add(ability.AbilityId.Value);
                parts.Add(ability.SourceRevision.ToString());
                parts.Add(ability.ContentHash.ToString());
                parts.Add(ability.StateSchemaHash.ToString());
            }
            return StableHash.Compute(parts.ToArray()).ToString();
        }
    }

    public interface IFixedCharacterRuntimePort : ISimulationRuntimePort
    {
        FixedCharacterRuntime Runtime { get; }
        GameplayContentHash GameplayContentHash { get; }
        IReadOnlyList<SimulationActorBinding> Roster { get; }
        SimulationActorRosterDescriptor RosterDescriptor { get; }
        int GetActorIndex(ActorId actorId);
        CharacterControlRuntimeBinding GetControlRuntimeBinding(int actorIndex);
        CharacterBodyMotionBinding GetBodyMotionBinding(int actorIndex);
        CharacterGameplayEffectRuntimeBinding GetGameplayEffectRuntimeBinding(int actorIndex);
        CharacterEquipmentRuntimeBinding GetEquipmentRuntimeBinding(int actorIndex);
        GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> GetAbilityData(int actorIndex);
        FixedGameplayAbilityExecutionInstallationSet GetAbilityInstallations(int actorIndex);
    }

    public sealed class FixedCharacterRuntimePort : IFixedCharacterRuntimePort
    {
        readonly Dictionary<ActorId, int> m_ActorIndices = new Dictionary<ActorId, int>();

        public FixedCharacterRuntimePort(
            SimulationComponentIdentity backend,
            FixedCharacterRuntime runtime)
        {
            if (!backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Execution Backend identity is invalid.", nameof(backend));
            Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
            for (int i = 0; i < Runtime.Roster.Count; i++)
                m_ActorIndices.Add(Runtime.Roster[i].ActorId, i);
            Descriptor = FixedPipelineRuntimePortDescriptor.Create(
                FixedPipelineRuntimePortIds.CharacterRuntime,
                FixedPipelineRuntimePortIds.CharacterRuntimeSchema,
                backend.ComponentId,
                StableHash.Compute(backend.ToString(), Runtime.GameplayContentHash.ToString()),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public FixedCharacterRuntime Runtime { get; }
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

        public GameplayAbilityExecutionDataSet<FixedGameplayAbilityExecutionData> GetAbilityData(int actorIndex) =>
            Roster[actorIndex].AbilityData;

        public FixedGameplayAbilityExecutionInstallationSet GetAbilityInstallations(int actorIndex) =>
            Roster[actorIndex].AbilityInstallations;
    }

    public interface IFixedWorkingStateReadPort : ISimulationRuntimePort
    {
        SimulationWorldStateSet Current { get; }
        FixedSimulationStep Step { get; }
    }

    public sealed class FixedWorkingStatePort : IFixedWorkingStateReadPort
    {
        public FixedWorkingStatePort(SimulationComponentIdentity backend)
        {
            if (!backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Execution Backend identity is invalid.", nameof(backend));
            Descriptor = FixedPipelineRuntimePortDescriptor.Create(
                FixedPipelineRuntimePortIds.WorkingState,
                FixedPipelineRuntimePortIds.WorkingStateSchema,
                backend.ComponentId,
                StableHash.Compute(backend.ToString(), "working-state/1"),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public SimulationWorldStateSet Current { get; private set; }
        public FixedSimulationStep Step { get; private set; }

        internal void Set(SimulationWorldStateSet current, FixedSimulationStep step)
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

    public interface IFixedCompletedStepReadPort : ISimulationRuntimePort
    {
        IReadOnlyList<FixedCompletedSimulationStep> Steps { get; }
    }

    public sealed class FixedCompletedStepPort : IFixedCompletedStepReadPort
    {
        IReadOnlyList<FixedCompletedSimulationStep> m_Steps = Array.Empty<FixedCompletedSimulationStep>();

        public FixedCompletedStepPort(SimulationComponentIdentity backend)
        {
            if (!backend.IsValid || backend.Role != SimulationComponentRole.ExecutionBackend)
                throw new ArgumentException("Execution Backend identity is invalid.", nameof(backend));
            Descriptor = FixedPipelineRuntimePortDescriptor.Create(
                FixedPipelineRuntimePortIds.CompletedSteps,
                FixedPipelineRuntimePortIds.CompletedStepsSchema,
                backend.ComponentId,
                StableHash.Compute(backend.ToString(), "completed-steps/1"),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public IReadOnlyList<FixedCompletedSimulationStep> Steps => m_Steps;

        internal void Set(IReadOnlyList<FixedCompletedSimulationStep> steps) =>
            m_Steps = steps ?? throw new ArgumentNullException(nameof(steps));

        internal void Clear() => m_Steps = Array.Empty<FixedCompletedSimulationStep>();
    }

    public interface IFixedWorldSolverRuntimePort : ISimulationRuntimePort
    {
        ICharacterWorldSolver Solver { get; }
    }

    public sealed class FixedWorldSolverRuntimePort : IFixedWorldSolverRuntimePort
    {
        public FixedWorldSolverRuntimePort(
            SimulationComponentIdentity worldSolver,
            ICharacterWorldSolver solver)
        {
            if (!worldSolver.IsValid || worldSolver.Role != SimulationComponentRole.WorldSolver)
                throw new ArgumentException("World Solver component identity is invalid.", nameof(worldSolver));
            Solver = solver ?? throw new ArgumentNullException(nameof(solver));
            Descriptor = FixedPipelineRuntimePortDescriptor.Create(
                FixedPipelineRuntimePortIds.WorldSolver,
                FixedPipelineRuntimePortIds.WorldSolverSchema,
                worldSolver.ComponentId,
                StableHash.Compute(worldSolver.ToString(), solver.Descriptor.ImplementationId.Value, solver.Descriptor.Version),
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public ICharacterWorldSolver Solver { get; }
    }

    public interface IFixedDiagnosticsRuntimePort : ISimulationRuntimePort
    {
        ISimulationDiagnosticsSink Sink { get; }
    }

    public sealed class FixedDiagnosticsRuntimePort : IFixedDiagnosticsRuntimePort
    {
        public FixedDiagnosticsRuntimePort(
            SimulationComponentIdentity diagnostics,
            ISimulationDiagnosticsSink sink)
        {
            if (!diagnostics.IsValid || diagnostics.Role != SimulationComponentRole.Diagnostics)
                throw new ArgumentException("Diagnostics component identity is invalid.", nameof(diagnostics));
            Sink = sink ?? throw new ArgumentNullException(nameof(sink));
            Descriptor = FixedPipelineRuntimePortDescriptor.Create(
                FixedPipelineRuntimePortIds.Diagnostics,
                FixedPipelineRuntimePortIds.DiagnosticsSchema,
                diagnostics.ComponentId,
                diagnostics.ConfigurationHash,
                SimulationPortDirection.Input);
        }

        public SimulationPortDescriptor Descriptor { get; }
        public ISimulationDiagnosticsSink Sink { get; }
    }

    static class FixedPipelineRuntimePortDescriptor
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
