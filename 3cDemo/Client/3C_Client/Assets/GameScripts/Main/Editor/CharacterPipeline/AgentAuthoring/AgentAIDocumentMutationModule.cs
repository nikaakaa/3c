using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentDocumentMutationSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal static class AgentAIDocumentMutationModule
    {
        internal static void BuildAIMutations(
            AgentGraphSnapshot current,
            AgentDocumentEditable target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            AgentSnapshotAIController oldController = current.aiController ?? new AgentSnapshotAIController();
            AgentDocumentAIEditable controller = target.aiController;
            if (controller == null)
            {
                report.Error("document.editable.aiController", "ai_controller_missing", "AI Document缺少aiController正文。");
                return;
            }
            if (!string.Equals(oldController.controllerId, controller.controllerId, StringComparison.Ordinal))
            {
                Add(mutations, "document.editable.aiController.controllerId", AgentMutationKind.EnsureAIControllerDefinition, operation =>
                    operation.controllerId = controller.controllerId);
            }
            if (!string.Equals(oldController.treeAssetPath, controller.treeAssetPath, StringComparison.Ordinal))
            {
                Add(mutations, "document.editable.aiController.treeAssetPath", AgentMutationKind.EnsureAIControllerTree, operation =>
                    operation.rootTreeAssetPath = controller.treeAssetPath);
            }
            if (!string.Equals(oldController.controlledCharacterAssetPath, controller.controlledCharacterAssetPath, StringComparison.Ordinal) ||
                !string.Equals(oldController.controlledCharacterAssetGuid, controller.controlledCharacterAssetGuid, StringComparison.Ordinal) ||
                !string.Equals(oldController.perceptionAssetPath, controller.perceptionAssetPath, StringComparison.Ordinal) ||
                !string.Equals(oldController.perceptionAssetGuid, controller.perceptionAssetGuid, StringComparison.Ordinal))
            {
                Add(mutations, "document.editable.aiController.assets", AgentMutationKind.BindAIControllerAssets, operation =>
                {
                    operation.controlledCharacterAssetPath = controller.controlledCharacterAssetPath;
                    operation.controlledCharacterAssetGuid = controller.controlledCharacterAssetGuid;
                    operation.perceptionProfileAssetPath = controller.perceptionAssetPath;
                    operation.perceptionProfileAssetGuid = controller.perceptionAssetGuid;
                });
            }
            if (!string.Equals(oldController.candidateOrdering, controller.candidateOrdering, StringComparison.Ordinal) ||
                !SameList(oldController.candidateActorIds, controller.candidateActorIds))
            {
                Add(mutations, "document.editable.aiController.candidates", AgentMutationKind.ConfigureAICandidates, operation =>
                {
                    operation.candidateOrdering = controller.candidateOrdering;
                    operation.candidateActorIds = controller.candidateActorIds;
                });
            }
            bool normalizeBlackboard =
                current.blackboardSchemaRevision != target.blackboardSchemaRevision;
            BuildAIBlackboardMutations(
                oldController.blackboardDeclarations,
                controller.blackboardDeclarations,
                mutations,
                report,
                normalizeBlackboard);
            AgentBlackboardDocumentMutationModule.BuildBlackboardSchemaRevisionMutation(current, target, mutations, normalizeBlackboard);
            HashSet<string> removedNodes = BuildAINodeMutations(
                oldController.nodes,
                controller.nodes,
                current.graphs,
                target.graphs,
                mutations,
                report);
            IReadOnlyList<AgentSnapshotGraph> currentGraphs = (current.graphs ?? new List<AgentSnapshotGraph>())
                .Select(graph =>
                {
                    AgentSnapshotGraph clone = AgentAuthoringDocumentCodec.Clone(graph);
                    clone.flowEdges = (clone.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                        .Where(edge => !removedNodes.Contains(edge.startElementAuthoringId) && !removedNodes.Contains(edge.endElementAuthoringId))
                        .ToList();
                    clone.propertyEdges = (clone.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
                        .Where(edge => !removedNodes.Contains(edge.startElementAuthoringId) && !removedNodes.Contains(edge.endElementAuthoringId))
                        .ToList();
                    return clone;
                })
                .ToList();
            foreach (AgentSnapshotGraph graph in target.graphs ?? new List<AgentSnapshotGraph>())
            {
                if (string.Equals(graph.graphAuthoringId, controller.graphAuthoringId, StringComparison.Ordinal))
                    continue;
                AgentSnapshotGraph oldGraph = currentGraphs.FirstOrDefault(value =>
                    string.Equals(value.graphAuthoringId, graph.graphAuthoringId, StringComparison.Ordinal));
                if (oldGraph == null)
                {
                    report.Error($"document.editable.graphs[{Escape(graph.graphAuthoringId)}]", "graph_create_unsupported", "AI子Graph必须由拥有它的正式Node或Edge创建。");
                    continue;
                }
                    AgentGraphDocumentMutationModule.BuildGenericGraphMutations(oldGraph, graph, mutations, report);
            }
            AgentGraphDocumentMutationModule.BuildGraphEdgeMutations(
                currentGraphs.Where(graph => string.Equals(graph.graphAuthoringId, controller.graphAuthoringId, StringComparison.Ordinal)).ToList(),
                (target.graphs ?? new List<AgentSnapshotGraph>())
                    .Where(graph => string.Equals(graph.graphAuthoringId, controller.graphAuthoringId, StringComparison.Ordinal))
                    .ToList(),
                mutations,
                report);
        }

        static void BuildAIBlackboardMutations(
            IReadOnlyList<AgentSnapshotAIBlackboardDeclaration> current,
            IReadOnlyList<AgentSnapshotAIBlackboardDeclaration> target,
            AgentMutationDraftSet mutations,
            AgentCompileReport report,
            bool normalize)
        {
            var oldValues = Index(current, value => value.declarationAuthoringId, "document.editable.aiController.blackboardDeclarations", report);
            var newValues = Index(target, value => value.declarationAuthoringId, "document.editable.aiController.blackboardDeclarations", report);
            foreach (AgentSnapshotAIBlackboardDeclaration declaration in target ?? Array.Empty<AgentSnapshotAIBlackboardDeclaration>())
            {
                string path = $"document.editable.aiController.blackboardDeclarations[{Escape(declaration.declarationAuthoringId)}]";
                if (!normalize &&
                    !IsLocal(declaration.declarationAuthoringId) &&
                    oldValues.TryGetValue(declaration.declarationAuthoringId, out AgentSnapshotAIBlackboardDeclaration oldValue) &&
                    Same(oldValue, declaration))
                    continue;
                Add(mutations, path, AgentMutationKind.EnsureAIBlackboardDeclaration, operation =>
                {
                    if (IsLocal(declaration.declarationAuthoringId))
                        operation.id = LocalIdentity(declaration.declarationAuthoringId);
                    SetGraph(operation, declaration.ownerGraphAuthoringId);
                    SetOptionalExisting(operation, declaration.declarationAuthoringId, true);
                    operation.blackboardKey = declaration.displayName;
                    operation.blackboardValueType = declaration.valueType;
                    operation.blackboardScope = declaration.scope;
                    SetDefaultValue(operation, declaration.valueType, declaration.defaultValue);
                });
            }
            foreach (AgentSnapshotAIBlackboardDeclaration removed in (current ?? Array.Empty<AgentSnapshotAIBlackboardDeclaration>())
                         .Where(value => !newValues.ContainsKey(value.declarationAuthoringId)))
            {
                Add(mutations, $"document.editable.aiController.blackboardDeclarations[{Escape(removed.declarationAuthoringId)}]", AgentMutationKind.DeleteBlackboardDeclaration, operation =>
                {
                    SetGraph(operation, removed.ownerGraphAuthoringId);
                    operation.declarationAuthoringId = removed.declarationAuthoringId;
                });
            }
        }

        static HashSet<string> BuildAINodeMutations(
            IReadOnlyList<AgentSnapshotAINode> current,
            IReadOnlyList<AgentSnapshotAINode> target,
            IReadOnlyList<AgentSnapshotGraph> currentGraphs,
            IReadOnlyList<AgentSnapshotGraph> targetGraphs,
            AgentMutationDraftSet mutations,
            AgentCompileReport report)
        {
            var oldValues = Index(current, value => value.nodeAuthoringId, "document.editable.aiController.nodes", report);
            var newValues = Index(target, value => value.nodeAuthoringId, "document.editable.aiController.nodes", report);
            var positions = (targetGraphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.elementAuthoringId))
                .GroupBy(node => node.elementAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().position, StringComparer.Ordinal);
            var currentPositions = (currentGraphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.elementAuthoringId))
                .GroupBy(node => node.elementAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().position, StringComparer.Ordinal);
            foreach (AgentSnapshotAINode node in target ?? Array.Empty<AgentSnapshotAINode>())
            {
                string path = $"document.editable.aiController.nodes[{Escape(node.nodeAuthoringId)}]";
                if (!IsLocal(node.nodeAuthoringId) &&
                    oldValues.TryGetValue(node.nodeAuthoringId, out AgentSnapshotAINode oldNode) &&
                    SameAINodeEditable(oldNode, node) &&
                    Same(
                        currentPositions.TryGetValue(node.nodeAuthoringId, out AgentSnapshotVector2 oldPosition) ? oldPosition : null,
                        positions.TryGetValue(node.nodeAuthoringId, out AgentSnapshotVector2 newPosition) ? newPosition : null))
                    continue;
                AgentMutationKind? mutationKind = ResolveAINodeMutationKind(node);
                if (!mutationKind.HasValue)
                {
                    report.Error(path, "ai_node_kind_unsupported", $"AI nodeType未映射到正式Mutation：{node.nodeType}");
                    continue;
                }
                Add(mutations, path, mutationKind.Value, operation =>
                {
                    if (IsLocal(node.nodeAuthoringId))
                        operation.id = LocalIdentity(node.nodeAuthoringId);
                    SetGraph(operation, node.graphAuthoringId);
                    SetOptionalExisting(operation, node.nodeAuthoringId, false);
                    operation.aiNodeKind = ResolveAINodeKind(node);
                    operation.aiMemoryValueKind = node.memoryValueKind;
                    if (IsLocal(node.memoryDeclarationAuthoringId))
                        operation.declarationPlannedIdentity = LocalIdentity(node.memoryDeclarationAuthoringId);
                    else
                        operation.declarationAuthoringId = node.memoryDeclarationAuthoringId;
                    operation.inputId = node.inputId;
                    operation.request = node.requestId;
                    operation.requestBufferSeconds = node.requestBufferSeconds;
                    operation.requestPriority = node.requestPriority;
                    operation.aiRequestRepeatPolicy = node.requestRepeatPolicy;
                    if (positions.TryGetValue(node.nodeAuthoringId, out AgentSnapshotVector2 position))
                        operation.position = new Vector2(position.x, position.y);
                });
            }
            var removed = new HashSet<string>(oldValues.Keys.Except(newValues.Keys, StringComparer.Ordinal), StringComparer.Ordinal);
            foreach (AgentSnapshotAINode node in current ?? Array.Empty<AgentSnapshotAINode>())
            {
                if (!removed.Contains(node.nodeAuthoringId))
                    continue;
                Add(mutations, $"document.editable.aiController.nodes[{Escape(node.nodeAuthoringId)}]", AgentMutationKind.DeleteGraphNode, operation =>
                {
                    SetGraph(operation, node.graphAuthoringId);
                    operation.targetElementAuthoringId = node.nodeAuthoringId;
                });
            }
            return removed;
        }

        static bool SameAINodeEditable(AgentSnapshotAINode left, AgentSnapshotAINode right)
        {
            object Project(AgentSnapshotAINode node) => new
            {
                node.graphAuthoringId,
                node.nodeAuthoringId,
                node.nodeType,
                node.memoryDeclarationAuthoringId,
                node.memoryValueKind,
                node.inputId,
                node.requestId,
                node.requestBufferSeconds,
                node.requestPriority,
                node.requestRepeatPolicy
            };
            return Same(Project(left), Project(right));
        }

        static AgentMutationKind? ResolveAINodeMutationKind(AgentSnapshotAINode node)
        {
            string type = node.nodeType ?? string.Empty;
            if (type.EndsWith("ReadAIMemoryNode", StringComparison.Ordinal) || type.EndsWith("WriteAIMemoryNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAIMemoryNode;
            if (type.EndsWith("WriteContinuousInputNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAIContinuousInput;
            if (type.EndsWith("WriteActionTargetSnapshotNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAIActionTarget;
            if (type.EndsWith("SubmitActionRequestNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAIActionRequest;
            if (type.EndsWith("ReadSelfObservationNode", StringComparison.Ordinal) ||
                type.EndsWith("EnumerateConfiguredCandidatesNode", StringComparison.Ordinal) ||
                type.EndsWith("SelectNearestCandidateNode", StringComparison.Ordinal) ||
                type.EndsWith("ReadTargetDistanceNode", StringComparison.Ordinal) ||
                type.EndsWith("ReadTargetDirectionNode", StringComparison.Ordinal) ||
                type.EndsWith("ReadSelectedTargetSnapshotNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAIObservationNode;
            if (type.EndsWith("SequenceNode", StringComparison.Ordinal) ||
                type.EndsWith("SelectorNode", StringComparison.Ordinal) ||
                type.EndsWith("LoopNode", StringComparison.Ordinal) ||
                type.EndsWith("CompareNode", StringComparison.Ordinal) ||
                type.EndsWith("AIWaitTicksNode", StringComparison.Ordinal))
                return AgentMutationKind.EnsureAISharedNode;
            return null;
        }

        static string ResolveAINodeKind(AgentSnapshotAINode node)
        {
            string type = node.nodeType ?? string.Empty;
            if (type.EndsWith("ReadAIMemoryNode", StringComparison.Ordinal)) return AgentAIMemoryNodeKind.Read.ToString();
            if (type.EndsWith("WriteAIMemoryNode", StringComparison.Ordinal)) return AgentAIMemoryNodeKind.Write.ToString();
            if (type.EndsWith("ReadSelfObservationNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.ReadSelf.ToString();
            if (type.EndsWith("EnumerateConfiguredCandidatesNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.EnumerateConfiguredCandidates.ToString();
            if (type.EndsWith("SelectNearestCandidateNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.SelectNearestCandidate.ToString();
            if (type.EndsWith("ReadTargetDistanceNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.ReadTargetDistance.ToString();
            if (type.EndsWith("ReadTargetDirectionNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.ReadTargetDirection.ToString();
            if (type.EndsWith("ReadSelectedTargetSnapshotNode", StringComparison.Ordinal)) return AgentAIObservationNodeKind.ReadSelectedTargetSnapshot.ToString();
            if (type.EndsWith("AIWaitTicksNode", StringComparison.Ordinal)) return AgentAISharedNodeKind.WaitTicks.ToString();
            if (type.EndsWith("LoopNode", StringComparison.Ordinal)) return AgentAISharedNodeKind.Loop.ToString();
            if (type.EndsWith("SequenceNode", StringComparison.Ordinal)) return AgentAISharedNodeKind.Sequence.ToString();
            if (type.EndsWith("SelectorNode", StringComparison.Ordinal)) return AgentAISharedNodeKind.Selector.ToString();
            if (type.EndsWith("CompareNode", StringComparison.Ordinal)) return AgentAISharedNodeKind.Compare.ToString();
            return string.Empty;
        }
    }
}
