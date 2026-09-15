using System;

namespace ThirdPersonSimulation
{
    public sealed class Float32GameplayAbilityExecutionInstallation
    {
        public Float32GameplayAbilityExecutionInstallation(
            Float32GameplayAbilityExecutionData data,
            CharacterGameplayEffectRuntimeBinding gameplayEffectBinding)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            RequiresGameplayEffects = Data.Capabilities.HasGameplayCapability("GameplayEffect");
            RequiresEquipment = Data.Capabilities.HasGameplayCapability("Equipment");
            Layout = Float32GameplayAbilityExecutionLayoutFactory.Create(Data);
            GameplayEffectCatalog = CreateGameplayEffectCatalog(RequiresGameplayEffects, gameplayEffectBinding);
            Services = new Float32GameplayAbilityExecutionServices(
                Data,
                Layout,
                BuildOperationSourcePaths(Layout),
                RequiresGameplayEffects,
                GameplayEffectCatalog);
        }

        public Float32GameplayAbilityExecutionData Data { get; }
        public bool RequiresGameplayEffects { get; }
        public bool RequiresEquipment { get; }
        public GameplayAbilityExecutionLayout Layout { get; }
        public GameplayAbilityExecutionIdentity Identity => Services.Identity;
        public OperationExecutionTopology Topology => Layout.Topology;
        internal Float32GameplayEffectRuntimeCatalog GameplayEffectCatalog { get; }
        internal Float32GameplayAbilityExecutionServices Services { get; }
        internal Float32GameplayAbilityExecutionAccess Access => Services.Access;

        static Float32GameplayEffectRuntimeCatalog CreateGameplayEffectCatalog(
            bool enabled,
            CharacterGameplayEffectRuntimeBinding binding)
        {
            if (!enabled)
                return null;
            return new Float32GameplayEffectRuntimeCatalog(binding ??
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
