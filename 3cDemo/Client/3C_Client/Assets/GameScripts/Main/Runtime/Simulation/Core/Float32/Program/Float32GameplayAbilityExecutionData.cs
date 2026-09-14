using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThirdPersonSimulation
{
    public sealed class Float32GameplayAbilityExecutionData
    {
        Float32GameplayAbilityExecutionData(
            CharacterSkillId abilityId,
            GameplayAbilityProgramBinding binding,
            GameplayAbilityProviderContract providerContract,
            string compilerVersion,
            OperationSetVersion operationSetVersion,
            int tickRate,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash,
            SimulationNumericProfile numericProfile,
            SimulationProgramRootDescriptor root,
            ProgramId programId,
            ProgramHash programHash,
            LayoutHash layoutHash,
            IEnumerable<SimulationOperationDefinition> operationDefinitions,
            IEnumerable<SimulationOperation> operations,
            IEnumerable<ProgramConstant> constants,
            IEnumerable<ProgramConstantInputBinding> constantInputBindings,
            IEnumerable<ProgramControlFlowEdge> controlFlow,
            IEnumerable<ProgramReference> references,
            IEnumerable<ProgramGraphCallFrame> graphCallFrames,
            IEnumerable<ProgramStateSlot> stateSlots,
            IEnumerable<ProgramScopeLayout> scopes,
            IEnumerable<ProgramWorldRequestLayout> worldRequests,
            IEnumerable<ProgramOutputChannelLayout> outputChannels,
            IEnumerable<ProgramCatalogEntry> catalogEntries,
            IEnumerable<ProgramMotionModifierDescriptor> motionModifiers,
            IEnumerable<ProgramSourceMapEntry> sourceMap,
            IEnumerable<ProgramProducer> producers)
        {
            if (!root.IsAbility)
                throw new InvalidOperationException("Float32 Ability execution data requires an Ability root.");
            AbilityId = abilityId;
            Binding = binding ?? throw new ArgumentNullException(nameof(binding));
            ProviderContract = providerContract ?? throw new ArgumentNullException(nameof(providerContract));
            CompilerVersion = SimulationIdentity.Require(compilerVersion, nameof(compilerVersion));
            OperationSetVersion = operationSetVersion;
            TickRate = tickRate;
            SourceRevision = sourceRevision;
            SemanticHash = semanticHash;
            NumericProfile = numericProfile;
            Root = root;
            ProgramId = programId;
            ProgramHash = programHash;
            LayoutHash = layoutHash;
            ContentHash = StableHash.Compute(
                "gameplay-ability-execution-data/1",
                abilityId.Value,
                Root.ContentIdentity,
                programHash.ToString(),
                layoutHash.ToString());
            OperationDefinitions = Copy(operationDefinitions);
            Operations = Copy(operations);
            Constants = Copy(constants);
            ConstantInputBindings = Copy(constantInputBindings);
            ControlFlow = Copy(controlFlow);
            References = Copy(references);
            GraphCallFrames = Copy(graphCallFrames);
            StateSlots = Copy(stateSlots);
            Scopes = Copy(scopes);
            WorldRequests = Copy(worldRequests);
            OutputChannels = Copy(outputChannels);
            CatalogEntries = Copy(catalogEntries);
            MotionModifiers = Copy(motionModifiers);
            SourceMap = Copy(sourceMap);
            Producers = Copy(producers);
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
        public ProgramId ProgramId { get; }
        public ProgramHash ProgramHash { get; }
        public LayoutHash LayoutHash { get; }
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
        public IReadOnlyList<ProgramWorldRequestLayout> WorldRequests { get; }
        public IReadOnlyList<ProgramOutputChannelLayout> OutputChannels { get; }
        public IReadOnlyList<ProgramCatalogEntry> CatalogEntries { get; }
        public IReadOnlyList<ProgramMotionModifierDescriptor> MotionModifiers { get; }
        public IReadOnlyList<ProgramSourceMapEntry> SourceMap { get; }
        public IReadOnlyList<ProgramProducer> Producers { get; }

        internal static Float32GameplayAbilityExecutionData Create(
            CharacterSkillId abilityId,
            GameplayAbilityProgramBinding binding,
            GameplayAbilityProviderContract providerContract,
            string compilerVersion,
            OperationSetVersion operationSetVersion,
            int tickRate,
            ProgramRevision sourceRevision,
            SemanticHash semanticHash,
            SimulationNumericProfile numericProfile,
            SimulationProgramRootDescriptor root,
            ProgramId programId,
            ProgramHash programHash,
            LayoutHash layoutHash,
            IEnumerable<SimulationOperationDefinition> operationDefinitions,
            IEnumerable<SimulationOperation> operations,
            IEnumerable<ProgramConstant> constants,
            IEnumerable<ProgramConstantInputBinding> constantInputBindings,
            IEnumerable<ProgramControlFlowEdge> controlFlow,
            IEnumerable<ProgramReference> references,
            IEnumerable<ProgramGraphCallFrame> graphCallFrames,
            IEnumerable<ProgramStateSlot> stateSlots,
            IEnumerable<ProgramScopeLayout> scopes,
            IEnumerable<ProgramWorldRequestLayout> worldRequests,
            IEnumerable<ProgramOutputChannelLayout> outputChannels,
            IEnumerable<ProgramCatalogEntry> catalogEntries,
            IEnumerable<ProgramMotionModifierDescriptor> motionModifiers,
            IEnumerable<ProgramSourceMapEntry> sourceMap,
            IEnumerable<ProgramProducer> producers)
        {
            if (!abilityId.IsValid)
                throw new ArgumentException("Ability identity is invalid.", nameof(abilityId));
            return new Float32GameplayAbilityExecutionData(
                abilityId,
                binding,
                providerContract,
                compilerVersion,
                operationSetVersion,
                tickRate,
                sourceRevision,
                semanticHash,
                numericProfile,
                root,
                programId,
                programHash,
                layoutHash,
                operationDefinitions,
                operations,
                constants,
                constantInputBindings,
                controlFlow,
                references,
                graphCallFrames,
                stateSlots,
                scopes,
                worldRequests,
                outputChannels,
                catalogEntries,
                motionModifiers,
                sourceMap,
                producers);
        }

        static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> values) =>
            new ReadOnlyCollection<T>(new List<T>(values ?? Array.Empty<T>()));
    }
}
