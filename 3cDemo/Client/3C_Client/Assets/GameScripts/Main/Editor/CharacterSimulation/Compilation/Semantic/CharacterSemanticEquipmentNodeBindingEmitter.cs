using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class CharacterSemanticEquipmentNodeBindingEmitter : ICharacterSemanticNodeBinding
    {
        readonly CharacterSemanticCatalogReferenceEmitter m_Catalog;
        readonly CharacterSimulationCatalogIndex m_CatalogIndex;

        public CharacterSemanticEquipmentNodeBindingEmitter(
            CharacterSemanticCatalogReferenceEmitter catalog,
            CharacterSimulationCatalogIndex catalogIndex)
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
            if (node is ReadEquipmentIdentityNode equipmentIdentity)
            {
                BindEquipmentSlot(operation, route, source, equipmentIdentity.SlotId);
                return true;
            }
            if (node is ReadEquipmentParameterNode equipmentParameter)
            {
                BindEquipmentSlot(operation, route, source, equipmentParameter.SlotId);
                string key = $"{equipmentParameter.FeatureId}:{equipmentParameter.ParameterId}";
                m_Catalog.Bind(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.EquipmentFeatureParameter,
                    $"equipment:feature:{equipmentParameter.FeatureId}:parameter:{equipmentParameter.ParameterId}",
                    m_CatalogIndex.EquipmentParameters.Contains(key),
                    "equipment-parameter");
                return true;
            }
            if (node is EquipmentChangeOperationNode equipmentChange)
            {
                BindEquipmentSlot(operation, route, source, equipmentChange.SlotId);
                if (!string.IsNullOrEmpty(equipmentChange.EquipmentId))
                {
                    m_Catalog.Bind(
                        operation,
                        route,
                        source,
                        ProgramCatalogEntryKind.EquipmentDefinition,
                        $"equipment:item:{equipmentChange.EquipmentId}",
                        m_CatalogIndex.EquipmentItems.Contains(equipmentChange.EquipmentId),
                        "equipment-item");
                }
                return true;
            }
            return false;
        }

        void BindEquipmentSlot(
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source,
            string slotId)
        {
            m_Catalog.Bind(
                operation,
                route,
                source,
                ProgramCatalogEntryKind.EquipmentSlot,
                $"equipment:slot:{slotId}",
                m_CatalogIndex.EquipmentSlots.Contains(slotId),
                "equipment-slot");
        }
    }
}
