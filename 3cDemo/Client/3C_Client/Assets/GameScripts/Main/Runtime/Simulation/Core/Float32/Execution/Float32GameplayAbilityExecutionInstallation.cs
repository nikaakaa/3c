using System;

namespace ThirdPersonSimulation
{
    public sealed class Float32GameplayAbilityExecutionInstallation
    {
        internal Float32GameplayAbilityExecutionInstallation(
            Float32GameplayAbilityExecutionData data,
            Float32GameplayEffectRuntimeCatalog gameplayEffectCatalog,
            EquipmentProgramLayout equipmentLayout)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            RequiresGameplayEffects = Data.Capabilities.HasGameplayCapability("GameplayEffect");
            RequiresEquipment = Data.Capabilities.HasGameplayCapability("Equipment");
            if (RequiresGameplayEffects && gameplayEffectCatalog == null)
                throw new ArgumentNullException(nameof(gameplayEffectCatalog));
            if (RequiresEquipment && equipmentLayout == null)
                throw new ArgumentNullException(nameof(equipmentLayout));
            Layout = Float32GameplayAbilityExecutionLayoutFactory.Create(Data);
            GameplayEffectCatalog = RequiresGameplayEffects ? gameplayEffectCatalog : null;
            EquipmentLayout = RequiresEquipment ? equipmentLayout : null;
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
        internal Float32GameplayEffectRuntimeCatalog GameplayEffectCatalog { get; }
        internal EquipmentProgramLayout EquipmentLayout { get; }
        internal Float32GameplayAbilityExecutionServices Services { get; }
        internal Float32GameplayAbilityExecutionAccess Access => Services.Access;

        static string[] BuildOperationSourcePaths(GameplayAbilityExecutionLayout layout)
        {
            var result = new string[layout.Operations.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = layout.SourcePath(new OperationHandle(i));
            return result;
        }
    }
}
