using System;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public readonly struct FixedGameplayAbilityExecutionDataLoadExpectation
    {
        public FixedGameplayAbilityExecutionDataLoadExpectation(
            string definitionGuid,
            string abilityId,
            string compilerVersion,
            string operationSetVersion,
            string sourceRevision,
            string semanticHash,
            string numericProfileId,
            int targetAbiVersion,
            string programId,
            string abilityDataHash,
            string abilityLayoutHash,
            string canonicalBytesHash,
            SimulationProgramRootDescriptor root)
        {
            DefinitionGuid = definitionGuid;
            AbilityId = abilityId;
            CompilerVersion = compilerVersion;
            OperationSetVersion = operationSetVersion;
            SourceRevision = sourceRevision;
            SemanticHash = semanticHash;
            NumericProfileId = numericProfileId;
            TargetAbiVersion = targetAbiVersion;
            ProgramId = programId;
            AbilityDataHash = abilityDataHash;
            AbilityLayoutHash = abilityLayoutHash;
            CanonicalBytesHash = canonicalBytesHash;
            Root = root;
        }

        public string DefinitionGuid { get; }
        public string AbilityId { get; }
        public string CompilerVersion { get; }
        public string OperationSetVersion { get; }
        public string SourceRevision { get; }
        public string SemanticHash { get; }
        public string NumericProfileId { get; }
        public int TargetAbiVersion { get; }
        public string ProgramId { get; }
        public string AbilityDataHash { get; }
        public string AbilityLayoutHash { get; }
        public string CanonicalBytesHash { get; }
        public SimulationProgramRootDescriptor Root { get; }
    }

    public static class FixedGameplayAbilityExecutionDataLoader
    {
        public static FixedGameplayAbilityExecutionData Load(
            byte[] canonicalArtifact,
            FixedGameplayAbilityExecutionDataLoadExpectation expectation,
            GameplayAbilityProviderBinding providerBinding)
        {
            if (canonicalArtifact == null || canonicalArtifact.Length == 0)
                throw new InvalidOperationException("Fixed Gameplay Ability Data asset has no compiled artifact.");
            CharacterTargetProgramArtifactLoader.RequireDefinitionGuid(expectation.DefinitionGuid);
            if (string.IsNullOrEmpty(expectation.AbilityId))
                throw new InvalidOperationException("Fixed Gameplay Ability Data asset has no Ability identity.");
            StableHash bytesHash = CharacterTargetProgramArtifactLoader.ComputeBytesHash(canonicalArtifact);
            if (!bytesHash.IsValid || !string.Equals(bytesHash.ToString(), expectation.CanonicalBytesHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Fixed Gameplay Ability Data asset canonical bytes hash is invalid.");
            if (!string.Equals(expectation.NumericProfileId, FixedSimulationNumericProfile.Value.Id.Value, StringComparison.Ordinal) ||
                expectation.TargetAbiVersion != FixedSimulationNumericProfile.Value.AbiVersion.Value)
                throw new InvalidOperationException("Fixed Gameplay Ability Data asset Numeric Target is not Fixed.");
            if (!expectation.Root.IsAbility ||
                !string.Equals(expectation.Root.RootIdentity, expectation.DefinitionGuid, StringComparison.Ordinal) ||
                !string.Equals(expectation.Root.EntryIdentity, $"ability:{expectation.AbilityId}", StringComparison.Ordinal))
                throw new InvalidOperationException("Fixed Gameplay Ability Data asset root metadata is invalid.");
            CharacterSimulationProgram program = CharacterSimulationProgramCodec.ReadArtifact(
                canonicalArtifact,
                new ProgramLoadExpectation(
                    expectation.CompilerVersion,
                    new OperationSetVersion(expectation.OperationSetVersion),
                    new ProgramRevision(expectation.SourceRevision),
                    new SemanticHash(new StableHash(expectation.SemanticHash)),
                    FixedSimulationNumericProfile.Value,
                    expectation.Root));
            if (!program.Manifest.Root.Equals(expectation.Root) ||
                !string.Equals(program.Manifest.ProgramId.Value, expectation.ProgramId, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.OperationSetVersion.Value, expectation.OperationSetVersion, StringComparison.Ordinal) ||
                !string.Equals(program.Manifest.SemanticHash.ToString(), expectation.SemanticHash, StringComparison.Ordinal) ||
                !string.Equals(program.ProgramHash.ToString(), expectation.AbilityDataHash, StringComparison.Ordinal) ||
                !string.Equals(program.LayoutHash.ToString(), expectation.AbilityLayoutHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Fixed Gameplay Ability Data asset metadata does not match its canonical artifact.");
            CharacterSkillId abilityId = new CharacterSkillId(expectation.AbilityId);
            program.AbilityPrograms.Require(abilityId);
            GameplayAbilityProviderContract providerContract = GameplayAbilityProviderContract
                .Create(program.CatalogEntries, index => program.Constants[index].Int32);
            providerContract.RequireBinding(providerBinding);
            return FixedGameplayAbilityExecutionDataFactory.Create(program, abilityId, providerContract);
        }
    }
}
