using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonSimulation;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal sealed class GameplayAbilitySemanticActionNodeBindingEmitter : IGameplayAbilitySemanticNodeBinding
    {
        readonly GameplayAbilitySemanticCatalogReferenceEmitter m_Catalog;
        readonly GameplayAbilityCatalogIndex m_CatalogIndex;
        readonly GameplayAbilitySemanticBlackboardEmitter m_Blackboard;
        readonly SimulationCompileReport m_Report;

        public GameplayAbilitySemanticActionNodeBindingEmitter(
            GameplayAbilitySemanticCatalogReferenceEmitter catalog,
            GameplayAbilityCatalogIndex catalogIndex,
            GameplayAbilitySemanticBlackboardEmitter blackboard,
            SimulationCompileReport report)
        {
            m_Catalog = catalog;
            m_CatalogIndex = catalogIndex;
            m_Blackboard = blackboard;
            m_Report = report;
        }

        public bool TryBind(
            BaseNode node,
            OperationHandle operation,
            string route,
            CharacterSimulationSourceLocation source)
        {
            if (node is ActivateActionInstanceNode activate)
            {
                string actionId = activate.AdmissionProfile ? activate.AdmissionProfile.ActionId : string.Empty;
                m_Catalog.Bind(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.Action,
                    $"action:{actionId}",
                    m_CatalogIndex.Actions.Contains(actionId));
                if (!string.IsNullOrEmpty(activate.SourceInputRequestId))
                {
                    m_Catalog.Bind(
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
                return true;
            }
            if (node is CanActivateActionInfoNode admission)
            {
                string actionId = admission.AdmissionProfile ? admission.AdmissionProfile.ActionId : string.Empty;
                m_Catalog.Bind(
                    operation,
                    route,
                    source,
                    ProgramCatalogEntryKind.Action,
                    $"action:{actionId}",
                    m_CatalogIndex.Actions.Contains(actionId));
                if (admission.TargetSnapshotVariable.IsValid)
                    m_Blackboard.Bind(operation, route, admission.TargetSnapshotVariable, source);
                return true;
            }
            return false;
        }
    }
}
