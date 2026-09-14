using System;

namespace ThirdPersonSimulation
{
    internal static class Float32GameplayAbilityExecutionDataFactory
    {
        public static Float32GameplayAbilityExecutionData Create(
            CharacterSimulationProgram program,
            CharacterSkillId abilityId,
            GameplayAbilityProviderContract providerContract)
        {
            if (program == null)
                throw new ArgumentNullException(nameof(program));
            GameplayAbilityProgramBinding binding = program.AbilityPrograms.Require(abilityId);
            return Float32GameplayAbilityExecutionData.Create(
                abilityId,
                binding,
                providerContract,
                program.Manifest.CompilerVersion,
                program.Manifest.OperationSetVersion,
                program.Manifest.TickRate,
                program.Manifest.SourceRevision,
                program.Manifest.SemanticHash,
                program.Manifest.NumericProfile,
                program.Manifest.Root,
                program.Manifest.ProgramId,
                program.ProgramHash,
                program.LayoutHash,
                program.OperationDefinitions,
                program.Operations,
                program.Constants,
                program.ConstantInputBindings,
                program.ControlFlow,
                program.References,
                program.GraphCallFrames,
                program.StateSlots,
                program.Scopes,
                program.WorldRequests,
                program.OutputChannels,
                program.CatalogEntries,
                program.MotionModifiers,
                program.SourceMap,
                program.Producers);
        }
    }
}
