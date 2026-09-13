using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class CharacterSimulationCatalogIndex
    {
        public HashSet<string> InputValues { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> InputRequests { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> Actions { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> Behaviors { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> GameplayTags { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> Attributes { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> GameplayEffects { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentSlots { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentRoutes { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentFeatures { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentItems { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> EquipmentParameters { get; } = new HashSet<string>(StringComparer.Ordinal);
        public HashSet<string> CharacterStates { get; } = new HashSet<string>(StringComparer.Ordinal);
    }

    public sealed class CharacterSimulationCatalogCompiler
    {
        readonly CharacterSimulationCatalogIndex m_Index = new CharacterSimulationCatalogIndex();
        readonly CharacterSemanticInputCatalogEmitter m_InputCatalog;
        readonly CharacterSemanticTagAttributeCatalogEmitter m_TagAttributes;
        readonly CharacterSemanticActionCatalogEmitter m_ActionCatalog;
        readonly CharacterSemanticGameplayEffectCatalogEmitter m_GameplayEffects;
        readonly CharacterSemanticEquipmentCatalogEmitter m_EquipmentCatalog;
        readonly CharacterSemanticGlobalStateEmitter m_GlobalState;

        public CharacterSimulationCatalogCompiler(
            CharacterAuthoringCompilationModel model,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            m_InputCatalog = new CharacterSemanticInputCatalogEmitter(model, builder, report, m_Index);
            m_TagAttributes = new CharacterSemanticTagAttributeCatalogEmitter(model, builder, report, m_Index);
            m_ActionCatalog = new CharacterSemanticActionCatalogEmitter(model, builder, report, m_Index);
            m_GameplayEffects = new CharacterSemanticGameplayEffectCatalogEmitter(model, builder, report, m_Index);
            m_EquipmentCatalog = new CharacterSemanticEquipmentCatalogEmitter(model, builder, report, m_Index);
            m_GlobalState = new CharacterSemanticGlobalStateEmitter(model, builder);
        }

        public CharacterSimulationCatalogIndex Compile()
        {
            m_InputCatalog.Emit();
            m_TagAttributes.Emit();
            m_ActionCatalog.Emit();
            m_GameplayEffects.Emit();
            m_EquipmentCatalog.Emit();
            m_GlobalState.Emit();
            return m_Index;
        }
    }
}
