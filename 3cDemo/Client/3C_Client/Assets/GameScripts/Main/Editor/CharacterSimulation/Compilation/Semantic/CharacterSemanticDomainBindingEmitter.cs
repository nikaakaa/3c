using System;
using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticDomainBindingEmitter
    {
        readonly CharacterSimulationCatalogIndex m_CatalogIndex;
        readonly CharacterSimulationProgramBuilder m_Builder;
        readonly CharacterSimulationCompileReport m_Report;
        readonly CharacterSemanticBlackboardEmitter m_Blackboard;

        public CharacterSemanticDomainBindingEmitter(
            CharacterSimulationCatalogIndex catalogIndex,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            CharacterSemanticBlackboardEmitter blackboard)
        {
            m_CatalogIndex = catalogIndex ?? throw new ArgumentNullException(nameof(catalogIndex));
            m_Builder = builder ?? throw new ArgumentNullException(nameof(builder));
            m_Report = report ?? throw new ArgumentNullException(nameof(report));
            m_Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
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
            if (node is CharacterInputValueInfoNode input)
            {
                BindCatalog(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.InputValue,
                    $"input:value:{input.InputValueId}",
                    m_CatalogIndex.InputValues.Contains(input.InputValueId));
                return;
            }
            if (node is CharacterActionRequestInfoNode request)
            {
                BindCatalog(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.InputRequest,
                    $"input:request:{request.RequestId}",
                    m_CatalogIndex.InputRequests.Contains(request.RequestId));
                return;
            }
            if (node is ReadEquipmentIdentityNode equipmentIdentity)
            {
                BindEquipmentSlot(operation, route, source, equipmentIdentity.SlotId);
                return;
            }
            if (node is ReadEquipmentParameterNode equipmentParameter)
            {
                BindEquipmentSlot(operation, route, source, equipmentParameter.SlotId);
                string key = $"{equipmentParameter.FeatureId}:{equipmentParameter.ParameterId}";
                BindCatalog(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.EquipmentFeatureParameter,
                    key,
                    m_CatalogIndex.EquipmentParameters.Contains(key));
                return;
            }
            if (node is EquipmentChangeOperationNode equipmentChange)
            {
                BindEquipmentSlot(operation, route, source, equipmentChange.SlotId);
                if (!string.IsNullOrEmpty(equipmentChange.EquipmentId))
                {
                    BindCatalog(
                        operation,
                        route,
                        source,
                        ProgramCatalogEntryKind.EquipmentDefinition,
                        $"equipment:{equipmentChange.EquipmentId}",
                        m_CatalogIndex.EquipmentItems.Contains(equipmentChange.EquipmentId));
                }
                return;
            }
            if (node is EquipmentSlotHostNode equipmentHost)
            {
                BindEquipmentSlot(operation, route, source, equipmentHost.SlotId);
                return;
            }
            if (node is ResolveEquipmentActionRouteNode equipmentRoute)
            {
                BindCatalog(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.EquipmentRoute,
                    $"route:{equipmentRoute.RouteId}",
                    m_CatalogIndex.EquipmentRoutes.Contains(equipmentRoute.RouteId));
                return;
            }
            if (node is ActivateActionInstanceNode activate)
            {
                string actionId = activate.ActionProfile ? activate.ActionProfile.ActionId : string.Empty;
                BindCatalog(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.Action,
                    $"action:{actionId}",
                    m_CatalogIndex.Actions.Contains(actionId));
                if (!string.IsNullOrEmpty(activate.SourceInputRequestId))
                {
                    BindCatalog(
                        operation,
                        route,
                        source,
                        ProgramCatalogEntryKind.InputRequest,
                        $"input:request:{activate.SourceInputRequestId}",
                        m_CatalogIndex.InputRequests.Contains(activate.SourceInputRequestId),
                        "source-request");
                }
                if (!activate.ActionContext)
                    m_Report.Error("action_context_missing", source.Identity, "Action activation requires a formal Action Context asset.");
                if (activate.TargetSnapshotVariable.IsValid)
                    m_Blackboard.Bind(operation, route, activate.TargetSnapshotVariable, source);
                return;
            }
            if (node is CanActivateActionInfoNode admission)
            {
                string actionId = admission.ActionProfile ? admission.ActionProfile.ActionId : string.Empty;
                BindCatalog(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.Action,
                    $"action:{actionId}",
                    m_CatalogIndex.Actions.Contains(actionId));
                if (admission.TargetSnapshotVariable.IsValid)
                    m_Blackboard.Bind(operation, route, admission.TargetSnapshotVariable, source);
                return;
            }
            if (node is HasGameplayTagNode hasTag)
            {
                BindCatalog(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.GameplayTag,
                    $"tag:{hasTag.Tag.Value}",
                    m_CatalogIndex.GameplayTags.Contains(hasTag.Tag.Value));
                return;
            }
            if (node is MatchGameplayTagQueryNode matchTags)
            {
                BindTagQuery(operation, route, source, matchTags.Query);
                return;
            }
            if (node is ReadGameplayAttributeNode readAttribute)
            {
                BindCatalog(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.Attribute,
                    $"attribute:{readAttribute.Attribute.Value}",
                    m_CatalogIndex.Attributes.Contains(readAttribute.Attribute.Value));
                return;
            }
            if (node is ApplyGameplayEffectNode applyEffect)
            {
                string effectId = applyEffect.Effect ? applyEffect.Effect.EffectId.Value : string.Empty;
                BindCatalog(
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
                return;
            }
            if (node is RemoveGameplayEffectNode removeEffect)
            {
                if (removeEffect.Selector == ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector.EffectId)
                {
                    string effectId = removeEffect.Effect ? removeEffect.Effect.EffectId.Value : string.Empty;
                    BindCatalog(
                        operation,
                        route,
                        source,
                        ProgramCatalogEntryKind.GameplayEffect,
                        $"effect:{effectId}",
                        m_CatalogIndex.GameplayEffects.Contains(effectId));
                }
                else if (removeEffect.Selector == ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector.EffectTagQuery)
                    BindTagQuery(operation, route, source, removeEffect.EffectTagQuery);
            }
        }

        void BindTagQuery(
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source,
            ThirdPersonGameplay.Tags.GameplayTagQuery query)
        {
            if (query == null)
            {
                m_Report.Error("gameplay_tag_query_missing", source.Identity, "Gameplay Tag query is missing.");
                return;
            }
            int suffix = 0;
            Bind(query.All);
            Bind(query.Any);
            Bind(query.None);

            void Bind(IReadOnlyList<ThirdPersonGameplay.Tags.GameplayTagId> values)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    string tagId = values[i].Value;
                    BindCatalog(
                        operation,
                        route,
                        source,
                        ProgramCatalogEntryKind.GameplayTag,
                        $"tag:{tagId}",
                        m_CatalogIndex.GameplayTags.Contains(tagId),
                        $"tag-{suffix++:D4}");
                }
            }
        }

        void BindEquipmentSlot(
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source,
            string slotId)
        {
            BindCatalog(
                operation,
                route,
                source,
                ProgramCatalogEntryKind.EquipmentSlot,
                $"slot:{slotId}",
                m_CatalogIndex.EquipmentSlots.Contains(slotId));
        }

        void BindCatalog(
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source,
            ProgramCatalogEntryKind kind,
            string identity,
            bool known,
            string suffix = "catalog")
        {
            if (!known || !m_Builder.TryGetCatalogEntry(kind, identity, out int catalog))
            {
                m_Report.Error("catalog_reference_invalid", source.Identity, $"Node references unknown catalog entry '{identity}'.");
                return;
            }
            m_Builder.DeclareReference(
                $"{route}/node:{source.NodeId}/{suffix}",
                operation,
                ProgramReferenceKind.CatalogEntry,
                catalog,
                identity,
                source);
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
    }
}
