using System;
using ThirdPersonSimulation;
using FixedCompilation = ThirdPersonSimulation.Fixed.FixedProgramArtifactCompilationResult;
using FixedCompiler = ThirdPersonSimulation.Fixed.FixedCharacterSimulationTargetCompiler;
using Float32Compilation = ThirdPersonSimulation.Float32ProgramLoweringResult;
using Float32Compiler = ThirdPersonSimulation.Float32CharacterSimulationTargetCompiler;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class GameplayAbilityTargetCompiler
    {
        public static Float32Compilation CompileFloat32(ValidatedSemanticIrArtifact artifact)
        {
            RequireAbilityArtifact(artifact);
            return Float32Compiler.Compile(artifact);
        }

        public static FixedCompilation CompileFixed(ValidatedSemanticIrArtifact artifact)
        {
            RequireAbilityArtifact(artifact);
            return FixedCompiler.CompileArtifact(artifact);
        }

        static void RequireAbilityArtifact(ValidatedSemanticIrArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            if (!artifact.Header.Root.IsAbility ||
                !artifact.SemanticIr.Manifest.Root.Equals(artifact.Header.Root))
                throw new InvalidOperationException("Gameplay Ability Target requires an Ability Semantic IR artifact.");
            GameplayAbilityProviderContract.Create(artifact.SemanticIr.CatalogEntries);
        }
    }
}
