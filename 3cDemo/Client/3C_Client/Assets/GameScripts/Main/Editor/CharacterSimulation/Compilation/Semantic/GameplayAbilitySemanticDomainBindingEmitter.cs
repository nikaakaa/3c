using System;
using System.Collections.Generic;
using FlowCanvas;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal interface IGameplayAbilitySemanticNodeBinding
    {
        bool TryBind(
            BaseNode node,
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source);
    }

    internal sealed class GameplayAbilitySemanticDomainBindingEmitter
    {
        readonly GameplayAbilitySemanticBlackboardEmitter m_Blackboard;
        readonly IReadOnlyList<IGameplayAbilitySemanticNodeBinding> m_Bindings;
        readonly GameplayAbilitySemanticCatalogReferenceEmitter m_Catalog;
        readonly GameplayAbilityCatalogIndex m_CatalogIndex;
        readonly CharacterSimulationCompileReport m_Report;

        public GameplayAbilitySemanticDomainBindingEmitter(
            GameplayAbilityCatalogIndex catalogIndex,
            GameplayAbilitySemanticBuilder builder,
            CharacterSimulationCompileReport report,
            GameplayAbilitySemanticBlackboardEmitter blackboard)
        {
            if (catalogIndex == null)
                throw new ArgumentNullException(nameof(catalogIndex));
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            m_Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            m_Report = report;
            var catalog = new GameplayAbilitySemanticCatalogReferenceEmitter(catalogIndex, builder, report);
            m_Catalog = catalog;
            m_CatalogIndex = catalogIndex;
            m_Bindings = new IGameplayAbilitySemanticNodeBinding[]
            {
                new GameplayAbilitySemanticInputNodeBindingEmitter(catalog, catalogIndex),
                new GameplayAbilitySemanticEquipmentNodeBindingEmitter(catalog, catalogIndex),
                new GameplayAbilitySemanticActionNodeBindingEmitter(catalog, catalogIndex, blackboard, report),
                new GameplayAbilitySemanticGameplayNodeBindingEmitter(catalog, catalogIndex, report)
            };
        }

        public void Bind(
            BaseNode node,
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source)
        {
            if (TryGetBlackboardReference(node, out PipelineBlackboardVariableReference blackboard))
            {
                m_Blackboard.Bind(operation, route, blackboard, source);
                return;
            }
            for (int i = 0; i < m_Bindings.Count; i++)
            {
                if (m_Bindings[i].TryBind(node, operation, route, source))
                    return;
            }
        }

        static bool TryGetBlackboardReference(BaseNode node, out PipelineBlackboardVariableReference reference)
        {
            if (node is ExposedPropertyNode exposed)
            {
                reference = exposed.BlackboardVariable;
                return true;
            }
            if (node is PipelineBlackboardValueInfoNode value)
            {
                reference = value.BlackboardVariable;
                return true;
            }
            reference = default;
            return false;
        }

        public void Bind(FlowNode node, OperationHandle operation, string route, CharacterSimulationSourceLocation source)
        {
            switch (node)
            {
                case BtsmtlSkillActionRequestFlowNode request:
                    m_Catalog.Bind(operation, route, source, ProgramCatalogEntryKind.InputRequest,
                        $"input:request:{request.InputId}", m_CatalogIndex.InputRequests.Contains(request.InputId));
                    break;
                case IBtsmtlSkillInputNode input:
                    m_Catalog.Bind(operation, route, source, ProgramCatalogEntryKind.InputValue,
                        $"input:value:{input.InputId}", m_CatalogIndex.InputValues.Contains(input.InputId));
                    break;
                case IBtsmtlSkillBlackboardAccessNode blackboard:
                    m_Blackboard.Bind(operation, route, blackboard.Variable.OwnerId, blackboard.Variable.DeclarationId, blackboard.ValueType, source, blackboard.Writes);
                    break;
                case BtsmtlSkillGameplayTagFlowNode tag:
                    m_Catalog.Bind(
                        operation,
                        route,
                        source,
                        ProgramCatalogEntryKind.GameplayTag,
                        $"tag:{tag.Tag.Value}",
                        m_CatalogIndex.GameplayTags.Contains(tag.Tag.Value));
                    break;
                case BtsmtlSkillGameplayTagQueryFlowNode query:
                    m_Catalog.BindTagQuery(operation, route, source, query.Query);
                    break;
                case BtsmtlSkillGameplayAttributeFlowNode attribute:
                    m_Catalog.Bind(
                        operation,
                        route,
                        source,
                        ProgramCatalogEntryKind.Attribute,
                        $"attribute:{attribute.Attribute.Value}",
                        m_CatalogIndex.Attributes.Contains(attribute.Attribute.Value));
                    break;
                case BtsmtlSkillApplyGameplayEffectFlowNode apply:
                    string effectId = apply.Effect ? apply.Effect.EffectId.Value : string.Empty;
                    m_Catalog.Bind(
                        operation,
                        route,
                        source,
                        ProgramCatalogEntryKind.GameplayEffect,
                        $"effect:{effectId}",
                        m_CatalogIndex.GameplayEffects.Contains(effectId));
                    if (apply.Predicted && !apply.ActionContext)
                        m_Report.Error(
                            "gameplay_effect_prediction_context_missing",
                            source.Identity,
                            "Predicted Gameplay Effect application requires a formal Action Context.");
                    break;
                case BtsmtlSkillRemoveGameplayEffectFlowNode remove:
                    if (remove.Selector == ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector.EffectId)
                    {
                        string removedEffectId = remove.Effect ? remove.Effect.EffectId.Value : string.Empty;
                        m_Catalog.Bind(
                            operation,
                            route,
                            source,
                            ProgramCatalogEntryKind.GameplayEffect,
                            $"effect:{removedEffectId}",
                            m_CatalogIndex.GameplayEffects.Contains(removedEffectId));
                    }
                    else if (remove.Selector == ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector.EffectTagQuery)
                        m_Catalog.BindTagQuery(operation, route, source, remove.EffectTagQuery);
                    break;
                case BtsmtlSkillCanActivateActionFlowNode action:
                    string actionId = action.AdmissionProfile ? action.AdmissionProfile.ActionId : string.Empty;
                    m_Catalog.Bind(operation, route, source, ProgramCatalogEntryKind.Action,
                        $"action:{actionId}", m_CatalogIndex.Actions.Contains(actionId));
                    if (!string.IsNullOrEmpty(action.TargetSnapshotDeclarationId))
                        m_Blackboard.Bind(operation, route, action.TargetSnapshotOwnerId, action.TargetSnapshotDeclarationId, typeof(ActionTargetSnapshot), source);
                    break;
            }
        }
    }
}
