using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentMutationLoweringSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentGraphMutationLowering
    {
        internal static AgentMutation LowerEnsureInputNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string inputId = context.RequiredText(operation.inputId, operation.request, "inputId", "ensure_input_node 缺少 inputId。");
            return context.IsValid
                ? new AgentEnsureInputNodeMutation(
                    operation.id,
                    context.Path,
                    graph,
                    existing,
                    operation.nodeType,
                    First(operation.displayName, First(inputId, operation.nodeType)),
                    inputId,
                    operation.position)
                : null;
        }

        internal static AgentMutation LowerEnsureConditionValueNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string nodeType = context.RequiredText(operation.nodeType, string.Empty, "nodeType", "ensure_condition_value_node缺少nodeType。");
            bool configurationValid = TryParseEnum(
                context,
                operation.conditionValueConfiguration,
                "conditionValueConfiguration",
                out AgentConditionValueNodeConfigurationKind configuration);
            AgentAuthoringReference declaration = default;
            StateExitCause exitCause = default;
            AgentAssetReference actionContext = default;
            string windowType = string.Empty;
            AgentAssetReference actionProfile = default;
            AgentAuthoringReference targetSnapshotDeclaration = default;
            if (configurationValid)
            {
                switch (configuration)
                {
                    case AgentConditionValueNodeConfigurationKind.None:
                        break;
                    case AgentConditionValueNodeConfigurationKind.BlackboardDeclaration:
                        declaration = context.RequiredDeclaration(operation.declarationAuthoringId, operation.declarationPlannedIdentity, "blackboardDeclaration");
                        break;
                    case AgentConditionValueNodeConfigurationKind.StateExitCause:
                        configurationValid = TryParseEnum(context, operation.stateExitCause, "stateExitCause", out exitCause);
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionContext:
                        actionContext = ReadActionContext(operation);
                        if (string.IsNullOrEmpty(actionContext.LogicalId) &&
                            string.IsNullOrEmpty(actionContext.AssetPath) &&
                            string.IsNullOrEmpty(actionContext.AssetGuid))
                            context.Error("actionContext", "action_context_required", "Action Context identity缺失。");
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionWindow:
                        windowType = context.RequiredText(operation.windowType, string.Empty, "windowType", "Action Window type缺失。");
                        break;
                    case AgentConditionValueNodeConfigurationKind.ActionAdmission:
                        actionProfile = ReadActionProfile(context, operation);
                        if (!string.IsNullOrEmpty(operation.targetSnapshotBlackboardDeclarationId) ||
                            !string.IsNullOrEmpty(operation.targetSnapshotBlackboardDeclarationPlannedIdentity))
                        {
                            targetSnapshotDeclaration = context.RequiredDeclaration(
                                operation.targetSnapshotBlackboardDeclarationId,
                                operation.targetSnapshotBlackboardDeclarationPlannedIdentity,
                                "targetSnapshotBlackboardDeclaration");
                        }
                        break;
                    default:
                        context.Error("conditionValueConfiguration", "condition_value_configuration_invalid", "未知Condition Value配置类型。");
                        break;
                }
            }
            return context.IsValid && configurationValid
                ? new AgentEnsureConditionValueNodeMutation(
                    operation.id,
                    context.Path,
                    graph,
                    existing,
                    nodeType,
                    First(operation.displayName, nodeType),
                    operation.position,
                    configuration,
                    declaration,
                    exitCause,
                    actionContext,
                    windowType,
                    actionProfile,
                    targetSnapshotDeclaration)
                : null;
        }

        internal static AgentMutation LowerDeleteFlowEdge(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            string edge = context.OptionalAuthoringId(context.RequiredText(operation.targetElementAuthoringId, string.Empty, "targetElementAuthoringId", "delete_flow_edge 缺少 edge identity。"), "targetElementAuthoringId");
            return context.IsValid ? new AgentDeleteFlowEdgeMutation(operation.id, context.Path, graph, edge) : null;
        }

        internal static AgentMutation LowerEnsureGraphNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference existing = context.OptionalElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement", true);
            string nodeType = context.RequiredText(operation.nodeType, string.Empty, "nodeType", "ensure_graph_node缺少Node类型。");
            LoopNode.StopType loopStopType = LoopNode.StopType.None;
            CompareNode.CompareType compareType = CompareNode.CompareType.Equal;
            if (!string.IsNullOrEmpty(operation.loopStopType))
                TryParseEnum(context, operation.loopStopType, "loopStopType", out loopStopType);
            if (!string.IsNullOrEmpty(operation.compareType))
                TryParseEnum(context, operation.compareType, "compareType", out compareType);
            return context.IsValid
                ? new AgentEnsureGraphNodeMutation(operation.id, context.Path, graph, existing, nodeType, operation.displayName, loopStopType, compareType, operation.position)
                : null;
        }

        internal static AgentMutation LowerEnsureGraph(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference parentGraph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "parentGraph");
            AgentElementTargetReference ownerNode = context.RequiredElement(operation.graphOwnerElementAuthoringId, operation.graphOwnerElementPlannedIdentity, "ownerNode");
            bool kindValid = TryParseEnum(context, operation.graphKind, "graphKind", out AgentGraphKind graphKind);
            bool ownershipValid = TryParseEnum(context, operation.graphOwnership, "graphOwnership", out AgentGraphOwnership ownership);
            string referenceKey = context.RequiredText(operation.graphReferenceKey, string.Empty, "graphReferenceKey", "ensure_graph 缺少 graph reference key。");
            string displayName = context.RequiredText(operation.displayName, referenceKey, "displayName", "ensure_graph 缺少 displayName。");
            return context.IsValid && kindValid && ownershipValid
                ? new AgentEnsureGraphMutation(
                    operation.id,
                    context.Path,
                    parentGraph,
                    ownerNode,
                    graphKind,
                    ownership,
                    referenceKey,
                    displayName,
                    operation.graphReferenceSharedAssetPath)
                : null;
        }

        internal static AgentMutation LowerConfigureGraphReference(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference ownerGraph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "ownerGraph");
            AgentElementTargetReference ownerNode = context.RequiredElement(operation.graphOwnerElementAuthoringId, operation.graphOwnerElementPlannedIdentity, "ownerNode");
            AgentGraphTargetReference childGraph = context.OptionalGraph(operation.graphReferenceGraphAuthoringId, operation.graphReferenceGraphPlannedIdentity, "childGraph");
            bool ownershipValid = TryParseEnum(context, operation.graphOwnership, "graphOwnership", out AgentGraphOwnership ownership);
            string referenceKey = context.RequiredText(operation.graphReferenceKey, string.Empty, "graphReferenceKey", "configure_graph_reference 缺少 graph reference key。");
            return context.IsValid && ownershipValid
                ? new AgentConfigureGraphReferenceMutation(
                    operation.id,
                    context.Path,
                    ownerGraph,
                    ownerNode,
                    childGraph,
                    ownership,
                    referenceKey,
                    operation.graphReferenceSharedAssetPath,
                    operation.graphReferenceInputBindings,
                    operation.graphReferenceOutputBindings)
                : null;
        }

        internal static AgentMutation LowerDeleteGraphNode(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference element = context.RequiredElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement");
            return context.IsValid ? new AgentDeleteGraphNodeMutation(operation.id, context.Path, graph, element) : null;
        }

        internal static AgentMutation LowerDeletePropertyEdge(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            string edge = context.OptionalAuthoringId(context.RequiredText(operation.targetElementAuthoringId, string.Empty, "targetElementAuthoringId", "delete_property_edge 缺少 edge identity。"), "targetElementAuthoringId");
            return context.IsValid ? new AgentDeletePropertyEdgeMutation(operation.id, context.Path, graph, edge) : null;
        }

        internal static AgentMutation LowerLinkFlow(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference source = context.RequiredElement(operation.sourceElementAuthoringId, operation.sourcePlannedIdentity, "sourceElement");
            AgentElementTargetReference target = context.RequiredElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement");
            return context.IsValid
                ? new AgentLinkFlowMutation(operation.id, context.Path, graph, source, target, First(operation.startPort, "Output"), First(operation.endPort, "Input"), operation.flowEdgeAuthoringId)
                : null;
        }

        internal static AgentMutation LowerLinkProperty(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentElementTargetReference source = context.RequiredElement(operation.sourceElementAuthoringId, operation.sourcePlannedIdentity, "sourceElement");
            AgentElementTargetReference target = context.RequiredElement(operation.targetElementAuthoringId, operation.targetPlannedIdentity, "targetElement");
            string startPort = context.RequiredText(operation.startPropertyPort, string.Empty, "startPropertyPort", "link_property 缺少 startPropertyPort。");
            string endPort = context.RequiredText(operation.endPropertyPort, string.Empty, "endPropertyPort", "link_property 缺少 endPropertyPort。");
            return context.IsValid
                ? new AgentLinkPropertyMutation(operation.id, context.Path, graph, source, target, startPort, endPort, operation.flowEdgeAuthoringId)
                : null;
        }

        internal static AgentMutation LowerEnsureBTConditionRule(AgentMutationPlanningContext context, AgentMutationDraft operation)
        {
            AgentGraphTargetReference graph = context.RequiredGraph(operation.graphAuthoringId, operation.graphPlannedIdentity, "graph");
            AgentFlowEdgeTargetReference edge = context.RequiredFlowEdge(operation.flowEdgeAuthoringId, operation.flowEdgePlannedIdentity, "flowEdge");
            TryParseEnum(context, operation.abortPolicy, "abortPolicy", out BTAbortPolicy abortPolicy);
            List<AgentConditionGroupMutation> groups = context.RequiredConditionGroups(operation.conditionGroups, operation);
            return context.IsValid
                ? new AgentEnsureBTConditionRuleMutation(operation.id, context.Path, graph, edge, abortPolicy, groups)
                : null;
        }
    }
}
