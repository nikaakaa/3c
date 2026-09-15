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
            RequiresGameplayEffects = Data.Capabilities.HasGameplayCapability("GameplayEffect");
            RequiresEquipment = Data.Capabilities.HasGameplayCapability("Equipment");
            Layout = FixedGameplayAbilityExecutionLayoutFactory.Create(Data);
            GameplayEffectCatalog = CreateGameplayEffectCatalog(RequiresGameplayEffects, gameplayEffectBinding);
            Services = new FixedGameplayAbilityExecutionServices(
                Data,
                Layout,
                BuildOperationSourcePaths(Layout),
                RequiresGameplayEffects,
                GameplayEffectCatalog);
        }

        public FixedGameplayAbilityExecutionData Data { get; }
        public bool RequiresGameplayEffects { get; }
        public bool RequiresEquipment { get; }
        public GameplayAbilityExecutionLayout Layout { get; }
        public GameplayAbilityExecutionIdentity Identity => Services.Identity;
        public OperationExecutionTopology Topology => Layout.Topology;
        internal FixedGameplayEffectRuntimeCatalog GameplayEffectCatalog { get; }
        internal FixedGameplayAbilityExecutionServices Services { get; }
        internal FixedGameplayAbilityExecutionAccess Access => Services.Access;

        static FixedGameplayEffectRuntimeCatalog CreateGameplayEffectCatalog(
            bool enabled,
            CharacterGameplayEffectRuntimeBinding binding)
        {
            if (!enabled)
                return null;
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
