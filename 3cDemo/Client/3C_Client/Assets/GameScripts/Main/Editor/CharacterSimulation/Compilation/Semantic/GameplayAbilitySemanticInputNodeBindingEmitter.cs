using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class GameplayAbilitySemanticInputNodeBindingEmitter : IGameplayAbilitySemanticNodeBinding
    {
        readonly GameplayAbilitySemanticCatalogReferenceEmitter m_Catalog;
        readonly GameplayAbilityCatalogIndex m_CatalogIndex;

        public GameplayAbilitySemanticInputNodeBindingEmitter(
            GameplayAbilitySemanticCatalogReferenceEmitter catalog,
            GameplayAbilityCatalogIndex catalogIndex)
        {
            m_Catalog = catalog;
            m_CatalogIndex = catalogIndex;
        }

        public bool TryBind(
            BaseNode node,
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source)
        {
            if (node is CharacterInputValueInfoNode input)
            {
                m_Catalog.Bind(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.InputValue,
                    $"input:value:{input.InputValueId}",
                    m_CatalogIndex.InputValues.Contains(input.InputValueId));
                return true;
            }
            if (node is CharacterActionRequestInfoNode request)
            {
                m_Catalog.Bind(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.InputRequest,
                    $"input:request:{request.RequestId}",
                    m_CatalogIndex.InputRequests.Contains(request.RequestId));
                return true;
            }
            return false;
        }
    }
}
