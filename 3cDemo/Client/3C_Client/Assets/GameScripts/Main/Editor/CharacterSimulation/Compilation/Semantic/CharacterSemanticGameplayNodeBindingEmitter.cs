using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonGameplay.Effects;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticGameplayNodeBindingEmitter : ICharacterSemanticNodeBinding
    {
        readonly CharacterSemanticCatalogReferenceEmitter m_Catalog;
        readonly CharacterSimulationCatalogIndex m_CatalogIndex;
        readonly CharacterSimulationCompileReport m_Report;

        public CharacterSemanticGameplayNodeBindingEmitter(
            CharacterSemanticCatalogReferenceEmitter catalog,
            CharacterSimulationCatalogIndex catalogIndex,
            CharacterSimulationCompileReport report)
        {
            m_Catalog = catalog;
            m_CatalogIndex = catalogIndex;
            m_Report = report;
        }

        public bool TryBind(
            BaseNode node,
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source)
        {
            if (node is HasGameplayTagNode hasTag)
            {
                m_Catalog.Bind(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.GameplayTag,
                    $"tag:{hasTag.Tag.Value}",
                    m_CatalogIndex.GameplayTags.Contains(hasTag.Tag.Value));
                return true;
            }
            if (node is MatchGameplayTagQueryNode matchTags)
            {
                m_Catalog.BindTagQuery(operation, route, source, matchTags.Query);
                return true;
            }
            if (node is ReadGameplayAttributeNode readAttribute)
            {
                m_Catalog.Bind(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.Attribute,
                    $"attribute:{readAttribute.Attribute.Value}",
                    m_CatalogIndex.Attributes.Contains(readAttribute.Attribute.Value));
                return true;
            }
            if (node is ApplyGameplayEffectNode applyEffect)
            {
                string effectId = applyEffect.Effect ? applyEffect.Effect.EffectId.Value : string.Empty;
                m_Catalog.Bind(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.GameplayEffect,
                    $"effect:{effectId}",
                    m_CatalogIndex.GameplayEffects.Contains(effectId));
                if (applyEffect.Predicted && !applyEffect.ActionContext)
                {
                    m_Report.Error(
                        "gameplay_effect_prediction_context_missing",
                        source.Identity,
                        "Predicted Gameplay Effect application requires a formal Action Context.");
                }
                return true;
            }
            if (node is RemoveGameplayEffectNode removeEffect)
            {
                if (removeEffect.Selector == ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector.EffectId)
                {
                    string effectId = removeEffect.Effect ? removeEffect.Effect.EffectId.Value : string.Empty;
                    m_Catalog.Bind(
                        operation,
                        route,
                        source,
                        ProgramCatalogEntryKind.GameplayEffect,
                        $"effect:{effectId}",
                        m_CatalogIndex.GameplayEffects.Contains(effectId));
                }
                else if (removeEffect.Selector == ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector.EffectTagQuery)
                    m_Catalog.BindTagQuery(operation, route, source, removeEffect.EffectTagQuery);
                return true;
            }
            return false;
        }
    }
}
