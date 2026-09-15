using System;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    public sealed class FixedGameplayAbilityExecutionInstallation
    {
        internal FixedGameplayAbilityExecutionInstallation(
            FixedGameplayAbilityExecutionData data,
            FixedGameplayEffectRuntimeCatalog gameplayEffectCatalog,
            EquipmentProgramLayout equipmentLayout)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            RequiresGameplayEffects = Data.Capabilities.HasGameplayCapability("GameplayEffect");
            RequiresEquipment = Data.Capabilities.HasGameplayCapability("Equipment");
            if (RequiresGameplayEffects && gameplayEffectCatalog == null)
                throw new ArgumentNullException(nameof(gameplayEffectCatalog));
            if (RequiresEquipment && equipmentLayout == null)
                throw new ArgumentNullException(nameof(equipmentLayout));
            Layout = FixedGameplayAbilityExecutionLayoutFactory.Create(Data);
            GameplayEffectCatalog = RequiresGameplayEffects ? gameplayEffectCatalog : null;
            EquipmentLayout = RequiresEquipment ? equipmentLayout : null;
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
        internal EquipmentProgramLayout EquipmentLayout { get; }
        internal FixedGameplayAbilityExecutionServices Services { get; }
        internal FixedGameplayAbilityExecutionAccess Access => Services.Access;

        static string[] BuildOperationSourcePaths(GameplayAbilityExecutionLayout layout)
        {
            var result = new string[layout.Operations.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = layout.SourcePath(new OperationHandle(i));
            return result;
        }
    }
}
