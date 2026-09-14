using System;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedGameplayAbilityExecutionInstallation
    {
        public FixedGameplayAbilityExecutionInstallation(
            FixedGameplayAbilityExecutionData data,
            CharacterGameplayEffectRuntimeBinding gameplayEffectBinding)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Layout = FixedGameplayAbilityExecutionLayoutFactory.Create(Data);
            GameplayEffectCatalog = CreateGameplayEffectCatalog(Data, gameplayEffectBinding);
            Services = new FixedGameplayAbilityExecutionServices(
                Data,
                Layout,
                BuildOperationSourcePaths(Layout),
                GameplayEffectCatalog);
        }

        public FixedGameplayAbilityExecutionData Data { get; }
        public GameplayAbilityExecutionLayout Layout { get; }
        public GameplayAbilityExecutionIdentity Identity => Services.Identity;
        public OperationExecutionTopology Topology => Layout.Topology;
        internal FixedGameplayEffectRuntimeCatalog GameplayEffectCatalog { get; }
        internal FixedGameplayAbilityExecutionServices Services { get; }
        internal FixedGameplayAbilityExecutionAccess Access => Services.Access;

        static FixedGameplayEffectRuntimeCatalog CreateGameplayEffectCatalog(
            FixedGameplayAbilityExecutionData data,
            CharacterGameplayEffectRuntimeBinding binding)
        {
            bool enabled = data.Capabilities.HasGameplayCapability("GameplayEffect");
            if (!enabled)
            {
                if (binding != null)
                    throw new InvalidOperationException("Ability execution received a Gameplay Effect binding while the capability is disabled.");
                return null;
            }
            return new FixedGameplayEffectRuntimeCatalog(binding ??
                throw new ArgumentNullException(nameof(binding)));
        }

        static string[] BuildOperationSourcePaths(GameplayAbilityExecutionLayout layout)
        {
            var result = new string[layout.Operations.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = layout.SourcePath(new OperationHandle(i));
            return result;
        }
    }
}
