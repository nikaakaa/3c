using System;
using System.Text;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public static class GameplayAbilityExecutionDataBuildService
    {
        public static Float32GameplayAbilityExecutionCompilationResult CompileFloat32(
            GameplayAbilityDefinition definition)
        {
            GameplayAbilitySemanticFrontendResult frontend = CompileSemantic(definition);
            return GameplayAbilityTargetCompiler.CompileFloat32(frontend.Artifact);
        }

        public static ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionCompilationResult CompileFixed(
            GameplayAbilityDefinition definition)
        {
            GameplayAbilitySemanticFrontendResult frontend = CompileSemantic(definition);
            return GameplayAbilityTargetCompiler.CompileFixed(frontend.Artifact);
        }

        public static Float32GameplayAbilityExecutionData PublishFloat32(
            GameplayAbilityDefinition definition)
        {
            Float32GameplayAbilityExecutionCompilationResult compilation = CompileFloat32(definition);
            return GameplayAbilityExecutionDataArtifactStore.Write(
                compilation.Data.Root.RootIdentity,
                compilation.Data);
        }

        public static ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionData PublishFixed(
            GameplayAbilityDefinition definition)
        {
            ThirdPersonSimulation.Fixed.FixedGameplayAbilityExecutionCompilationResult compilation = CompileFixed(definition);
            return GameplayAbilityExecutionDataArtifactStore.Write(
                compilation.Data.Root.RootIdentity,
                compilation.Data);
        }

        static GameplayAbilitySemanticFrontendResult CompileSemantic(GameplayAbilityDefinition definition)
        {
            GameplayAbilitySemanticFrontendResult result =
                GameplayAbilitySemanticFrontendCompiler.Compile(definition);
            if (result.IsValid)
                return result;
            var message = new StringBuilder();
            for (int i = 0; i < result.Report.Messages.Count; i++)
            {
                if (message.Length > 0)
                    message.AppendLine();
                message.Append(result.Report.Messages[i]);
            }
            throw new InvalidOperationException(
                message.Length == 0 ? "Gameplay Ability Semantic IR compilation failed." : message.ToString());
        }
    }
}
