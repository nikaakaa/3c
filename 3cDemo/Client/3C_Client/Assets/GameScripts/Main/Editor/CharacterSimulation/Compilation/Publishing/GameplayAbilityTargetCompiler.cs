using System;
using ThirdPersonSimulation;
using FixedCompilation = ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionCompilationResult;
using FixedCompiler = ThirdPersonSimulation.Fixed.FixedGameplayAbilityTargetCompiler;
using Float32Compilation = ThirdPersonSimulation.Float32GameplayAbilityExecutionCompilationResult;
using Float32Compiler = ThirdPersonSimulation.Float32GameplayAbilityTargetCompiler;

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
            return FixedCompiler.Compile(artifact);
        }

        static void RequireAbilityArtifact(ValidatedSemanticIrArtifact artifact)
        {
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            if (!artifact.Header.Root.IsAbility ||
                !artifact.SemanticIr.Manifest.Root.Equals(artifact.Header.Root))
                throw new InvalidOperationException("Gameplay Ability Target requires an Ability Semantic IR artifact.");
            GameplayAbilityProviderContract.Create(
                artifact.SemanticIr.CatalogEntries,
                index => artifact.SemanticIr.Literals[index].Int32);
        }
    }
}
