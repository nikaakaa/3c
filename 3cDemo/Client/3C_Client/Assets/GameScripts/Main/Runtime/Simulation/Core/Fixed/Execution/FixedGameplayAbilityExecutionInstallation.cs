using System;
using ThirdPersonSimulation;

namespace ThirdPersonSimulation.Fixed
{
    internal sealed class FixedAbilityExecutionContext
    {
        public FixedAbilityExecutionContext(
            FixedGameplayAbilityExecutionData data,
            GameplayAbilityExecutionLayout layout,
            FixedGameplayAbilityExecutionServices services)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Layout = layout ?? throw new ArgumentNullException(nameof(layout));
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public FixedGameplayAbilityExecutionData Data { get; }
        public GameplayAbilityExecutionLayout Layout { get; }
        public FixedGameplayAbilityExecutionServices Services { get; }
    }

    public sealed class FixedGameplayAbilityExecutionInstallation
    {
        readonly FixedAbilityExecutionContext m_Execution;
        readonly EquipmentProgramLayout m_EquipmentLayout;

        internal FixedGameplayAbilityExecutionInstallation(
            FixedGameplayAbilityExecutionData data,
            FixedGameplayEffectRuntimeCatalog gameplayEffectCatalog,
            EquipmentProgramLayout equipmentLayout)
        {
            data = data ?? throw new ArgumentNullException(nameof(data));
            bool requiresGameplayEffects = data.Capabilities.HasGameplayCapability("GameplayEffect");
            bool requiresEquipment = data.Capabilities.HasGameplayCapability("Equipment");
            if (requiresGameplayEffects && gameplayEffectCatalog == null)
                throw new ArgumentNullException(nameof(gameplayEffectCatalog));
            if (requiresEquipment && equipmentLayout == null)
                throw new ArgumentNullException(nameof(equipmentLayout));
            GameplayAbilityExecutionLayout layout = FixedGameplayAbilityExecutionLayoutFactory.Create(data);
            FixedGameplayEffectRuntimeCatalog executionGameplayEffectCatalog = requiresGameplayEffects
                ? gameplayEffectCatalog
                : null;
            EquipmentProgramLayout executionEquipmentLayout = requiresEquipment
                ? equipmentLayout
                : null;
            FixedGameplayAbilityExecutionServices services = new FixedGameplayAbilityExecutionServices(
                data,
                layout,
                BuildOperationSourcePaths(layout),
                requiresGameplayEffects,
                executionGameplayEffectCatalog);
            m_EquipmentLayout = executionEquipmentLayout;
            m_Execution = new FixedAbilityExecutionContext(
                data,
                layout,
                services);
        }

        public FixedGameplayAbilityExecutionData Data => m_Execution.Data;
        public GameplayAbilityExecutionLayout Layout => m_Execution.Layout;
        public GameplayAbilityExecutionIdentity Identity => m_Execution.Services.Identity;
        internal EquipmentProgramLayout EquipmentLayout => m_EquipmentLayout;
        internal FixedGameplayAbilityExecutionServices Services => m_Execution.Services;
        internal FixedAbilityExecutionContext Execution => m_Execution;

        static string[] BuildOperationSourcePaths(GameplayAbilityExecutionLayout layout)
        {
            var result = new string[layout.Operations.Count];
            for (int i = 0; i < result.Length; i++)
                result[i] = layout.SourcePath(new OperationHandle(i));
            return result;
        }
    }
}
