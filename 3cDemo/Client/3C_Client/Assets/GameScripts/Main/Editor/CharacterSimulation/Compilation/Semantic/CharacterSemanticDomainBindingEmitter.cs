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
    internal interface ICharacterSemanticNodeBinding
    {
        bool TryBind(
            BaseNode node,
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source);
    }

    internal sealed class CharacterSemanticDomainBindingEmitter
    {
        readonly CharacterSemanticBlackboardEmitter m_Blackboard;
        readonly IReadOnlyList<ICharacterSemanticNodeBinding> m_Bindings;
        readonly CharacterSemanticCatalogReferenceEmitter m_Catalog;
        readonly CharacterSimulationCatalogIndex m_CatalogIndex;

        public CharacterSemanticDomainBindingEmitter(
            CharacterSimulationCatalogIndex catalogIndex,
            CharacterSimulationProgramBuilder builder,
            CharacterSimulationCompileReport report,
            CharacterSemanticBlackboardEmitter blackboard)
        {
            if (catalogIndex == null)
                throw new ArgumentNullException(nameof(catalogIndex));
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            m_Blackboard = blackboard ?? throw new ArgumentNullException(nameof(blackboard));
            var catalog = new CharacterSemanticCatalogReferenceEmitter(catalogIndex, builder, report);
            m_Catalog = catalog;
            m_CatalogIndex = catalogIndex;
            m_Bindings = new ICharacterSemanticNodeBinding[]
            {
                new CharacterSemanticInputNodeBindingEmitter(catalog, catalogIndex),
                new CharacterSemanticEquipmentNodeBindingEmitter(catalog, catalogIndex),
                new CharacterSemanticActionNodeBindingEmitter(catalog, catalogIndex, blackboard, report),
                new CharacterSemanticGameplayNodeBindingEmitter(catalog, catalogIndex, report)
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
                case IBtsmtlSkillBlackboardReadNode blackboard:
                    m_Blackboard.Bind(operation, route, blackboard.Variable.OwnerId, blackboard.Variable.DeclarationId, blackboard.ValueType, source);
                    break;
                case BtsmtlSkillCanActivateActionFlowNode action:
                    string actionId = action.ActionProfile ? action.ActionProfile.ActionId : string.Empty;
                    m_Catalog.Bind(operation, route, source, ProgramCatalogEntryKind.Action,
                        $"action:{actionId}", m_CatalogIndex.Actions.Contains(actionId));
                    if (!string.IsNullOrEmpty(action.TargetSnapshotDeclarationId))
                        m_Blackboard.Bind(operation, route, action.TargetSnapshotOwnerId, action.TargetSnapshotDeclarationId, typeof(ActionTargetSnapshot), source);
                    break;
            }
        }
    }
}
