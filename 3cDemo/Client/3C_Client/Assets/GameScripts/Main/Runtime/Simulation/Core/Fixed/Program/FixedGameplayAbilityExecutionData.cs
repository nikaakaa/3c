using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedGameplayAbilityExecutionData
    {
        FixedGameplayAbilityExecutionData(
            CharacterSkillId abilityId,
            GameplayAbilityProgramBinding binding,
            CharacterSimulationProgram program,
            GameplayAbilityProviderContract providerContract)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            if (!program.Manifest.Root.IsAbility)
                throw new InvalidOperationException("Fixed Ability execution data requires an Ability root.");
            AbilityId = abilityId;
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            ProviderContract = providerContract ?? throw new ArgumentNullException(nameof(providerContract));
            CompilerVersion = program.Manifest.CompilerVersion;
            OperationSetVersion = program.Manifest.OperationSetVersion;
            TickRate = program.Manifest.TickRate;
            SourceRevision = program.Manifest.SourceRevision;
            SemanticHash = program.Manifest.SemanticHash;
            NumericProfile = program.Manifest.NumericProfile;
            Root = program.Manifest.Root;
            ContentHash = StableHash.Compute(
                "gameplay-ability-execution-data/1",
                abilityId.Value,
                Root.ContentIdentity,
                program.ProgramHash.ToString(),
                program.LayoutHash.ToString());
            OperationDefinitions = Copy(program.OperationDefinitions);
            Operations = Copy(program.Operations);
            Constants = Copy(program.Constants);
            ConstantInputBindings = Copy(program.ConstantInputBindings);
            ControlFlow = Copy(program.ControlFlow);
            References = Copy(program.References);
            GraphCallFrames = Copy(program.GraphCallFrames);
            StateSlots = Copy(program.StateSlots);
            Scopes = Copy(program.Scopes);
            CatalogEntries = Copy(program.CatalogEntries);
            SourceMap = Copy(program.SourceMap);
            Producers = Copy(program.Producers);
        }

        public CharacterSkillId AbilityId { get; }
        public GameplayAbilityProgramBinding Binding { get; }
        public GameplayAbilityProviderContract ProviderContract { get; }
        public string CompilerVersion { get; }
        public OperationSetVersion OperationSetVersion { get; }
        public int TickRate { get; }
        public ProgramRevision SourceRevision { get; }
        public SemanticHash SemanticHash { get; }
        public SimulationNumericProfile NumericProfile { get; }
        public SimulationProgramRootDescriptor Root { get; }
        public StableHash ContentHash { get; }
        public IReadOnlyList<SimulationOperationDefinition> OperationDefinitions { get; }
        public IReadOnlyList<SimulationOperation> Operations { get; }
        public IReadOnlyList<ProgramConstant> Constants { get; }
        public IReadOnlyList<ProgramConstantInputBinding> ConstantInputBindings { get; }
        public IReadOnlyList<ProgramControlFlowEdge> ControlFlow { get; }
        public IReadOnlyList<ProgramReference> References { get; }
        public IReadOnlyList<ProgramGraphCallFrame> GraphCallFrames { get; }
        public IReadOnlyList<ProgramStateSlot> StateSlots { get; }
        public IReadOnlyList<ProgramScopeLayout> Scopes { get; }
        public IReadOnlyList<ProgramCatalogEntry> CatalogEntries { get; }
        public IReadOnlyList<ProgramSourceMapEntry> SourceMap { get; }
        public IReadOnlyList<ProgramProducer> Producers { get; }

        internal static FixedGameplayAbilityExecutionData FromProgram(
            CharacterSimulationProgram program,
            CharacterSkillId abilityId,
            GameplayAbilityProviderContract providerContract)
        {
            if (!abilityId.IsValid)
                throw new ArgumentException("Ability identity is invalid.", nameof(abilityId));
            GameplayAbilityProgramBinding binding = program?.AbilityPrograms.Require(abilityId);
            return new FixedGameplayAbilityExecutionData(abilityId, binding, program, providerContract);
        }

        static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> values) =>
            new ReadOnlyCollection<T>(new List<T>(values ?? Array.Empty<T>()));
    }
}
