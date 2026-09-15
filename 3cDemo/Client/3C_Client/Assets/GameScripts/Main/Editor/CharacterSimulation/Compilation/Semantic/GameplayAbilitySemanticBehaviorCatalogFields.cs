using System.Collections.Generic;
using ThirdPersonGameplay.Contracts;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal static class GameplayAbilitySemanticBehaviorCatalogFields
    {
        public static IEnumerable<ProgramCatalogField> Emit(
            IGameplayBehaviorProfile profile,
            GameplayAbilitySemanticBuilder builder,
            SimulationSourceLocation source)
        {
            yield return builder.ConstantField(source, "BehaviorKind", profile.BehaviorKind);
            yield return builder.ConstantField(source, "DisplayName", profile.DisplayName);
            yield return builder.ConstantField(source, "DebugCategory", profile.DebugCategory);
            for (int i = 0; i < profile.Tags.Count; i++)
                yield return builder.IdentityField($"Tag:{i:D4}", $"tag:{profile.Tags[i].Value}");
        }
    }
}
