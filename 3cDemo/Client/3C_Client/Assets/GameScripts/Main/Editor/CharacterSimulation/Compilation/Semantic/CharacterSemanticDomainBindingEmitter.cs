using System;
using System.Collections.Generic;
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
    }
}
