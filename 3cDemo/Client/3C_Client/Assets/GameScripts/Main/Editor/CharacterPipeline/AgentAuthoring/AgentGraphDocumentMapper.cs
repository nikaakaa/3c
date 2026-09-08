using TreeDesigner.Authoring;
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline.Motion;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEngine;
using static ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring.AgentPackageMappingSupport;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal sealed class AgentGraphDocumentMapper
    {
readonly BtsmtlGraphAuthoringCapabilities m_Catalog = new BtsmtlGraphAuthoringCapabilities();

        static bool ValidateTimelineTreeClipGraphOwner(
            AgentDocumentEditable editable,
            AgentSnapshotGraph graph,
            IReadOnlyList<AgentSnapshotAuthoringRouteSegment> routes,
            IReadOnlyList<(AgentSnapshotNode Owner, AgentSnapshotGraphReference Reference)> nodeReferences,
            IReadOnlyDictionary<string, AgentSnapshotFlowEdge> flowEdges,
            IReadOnlyDictionary<string, AgentSnapshotNode> nodes)
        {
            if (routes.Count != 1 ||
                nodeReferences.Count != 0 ||
                flowEdges.Values.Any(edge => string.Equals(
                    edge.conditionRuleGraphAuthoringId,
                    graph.graphAuthoringId,
                    StringComparison.Ordinal)))
                return false;

            AgentSnapshotAuthoringRouteSegment route = routes[0];
            if (!string.Equals(route.ownerElementKind, TreeAuthoringElementKind.Node.ToString(), StringComparison.Ordinal) ||
                !string.Equals(route.ownerElementAuthoringId, graph.ownerElementAuthoringId, StringComparison.Ordinal) ||
                !string.Equals(route.referenceKey, graph.referenceKey, StringComparison.Ordinal) ||
                !string.Equals(route.referenceKey, "timeline.treeClip", StringComparison.Ordinal) ||
                !string.Equals(route.scopeId, route.clipAuthoringId, StringComparison.Ordinal) ||
                !string.Equals(RouteOwnership(route.ownership), graph.ownership, StringComparison.Ordinal) ||
                !nodes.TryGetValue(route.ownerElementAuthoringId ?? string.Empty, out AgentSnapshotNode ownerNode) ||
                !string.Equals(ownerNode.typeName, typeof(TimelineNode).FullName, StringComparison.Ordinal))
                return false;

            List<AgentSnapshotGraph> ownerGraphs = (editable.graphs ?? new List<AgentSnapshotGraph>())
                .Where(value =>
                    value != null &&
                    string.Equals(value.graphAuthoringId, route.ownerGraphAuthoringId, StringComparison.Ordinal))
                .ToList();
            if (ownerGraphs.Count != 1 ||
                (ownerGraphs[0].nodes ?? new List<AgentSnapshotNode>()).Count(value =>
                    value != null &&
                    string.Equals(value.elementAuthoringId, route.ownerElementAuthoringId, StringComparison.Ordinal)) != 1)
                return false;

            List<AgentSnapshotTimeline> timelines = (editable.timelines ?? new List<AgentSnapshotTimeline>())
                .Where(value =>
                    value != null &&
                    string.Equals(value.timelineAuthoringId, route.timelineAuthoringId, StringComparison.Ordinal))
                .ToList();
            if (timelines.Count != 1 ||
                (timelines[0].callSites ?? new List<AgentSnapshotTimelineCallSite>()).Count(value =>
                    value != null &&
                    string.Equals(value.nodeAuthoringId, route.ownerElementAuthoringId, StringComparison.Ordinal)) != 1)
                return false;

            List<AgentSnapshotTimelineTrack> tracks = (timelines[0].tracks ?? new List<AgentSnapshotTimelineTrack>())
                .Where(value =>
                    value != null &&
                    string.Equals(value.trackAuthoringId, route.trackAuthoringId, StringComparison.Ordinal))
                .ToList();
            List<AgentSnapshotTimelineClip> clips = (tracks.Count == 1
                    ? tracks[0].clips ?? new List<AgentSnapshotTimelineClip>()
                    : new List<AgentSnapshotTimelineClip>())
                .Where(value =>
                    value != null &&
                    string.Equals(value.clipAuthoringId, route.clipAuthoringId, StringComparison.Ordinal))
                .ToList();
            if (tracks.Count != 1 ||
                clips.Count != 1 ||
                clips[0].typeName?.EndsWith("TreeClip", StringComparison.Ordinal) != true)
                return false;

            List<AgentSnapshotTimelineTreeClip> summaries =
                (editable.timelineTreeClips ?? new List<AgentSnapshotTimelineTreeClip>())
                    .Where(value =>
                        value != null &&
                        string.Equals(value.timelineAuthoringId, route.timelineAuthoringId, StringComparison.Ordinal) &&
                        string.Equals(value.trackAuthoringId, route.trackAuthoringId, StringComparison.Ordinal) &&
                        string.Equals(value.clipAuthoringId, route.clipAuthoringId, StringComparison.Ordinal))
                    .ToList();
            return summaries.Count == 1 &&
                   string.Equals(
                       TimelineTreeOwnershipToGraphOwnership(summaries[0].ownership),
                       graph.ownership,
                       StringComparison.Ordinal) &&
                   summaries[0].startFrame == clips[0].startFrame &&
                   summaries[0].endFrame == clips[0].endFrame;
        }

        internal bool ValidateGraphRelationships(
            AgentDocumentEditable editable,
            AgentCompileReport report)
        {
            IReadOnlyList<AgentSnapshotGraph> graphs = editable?.graphs ?? new List<AgentSnapshotGraph>();
            var graphById = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .Where(graph => graph != null && !string.IsNullOrEmpty(graph.graphAuthoringId))
                .GroupBy(graph => graph.graphAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var nodes = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.elementAuthoringId))
                .GroupBy(node => node.elementAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var flowEdges = (graphs ?? Array.Empty<AgentSnapshotGraph>())
                .SelectMany(graph => graph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                .Where(edge => edge != null && !string.IsNullOrEmpty(edge.elementAuthoringId))
                .GroupBy(edge => edge.elementAuthoringId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            var ownerIds = new HashSet<string>(
                nodes.Keys.Concat(flowEdges.Keys),
                StringComparer.Ordinal);
            foreach (AgentSnapshotStateMachineSummary stateMachine in editable.stateMachines ?? new List<AgentSnapshotStateMachineSummary>())
            {
                foreach (AgentSnapshotStateSummary state in stateMachine?.states ?? new List<AgentSnapshotStateSummary>())
                {
                    if (!string.IsNullOrEmpty(state?.stateAuthoringId))
                        ownerIds.Add(state.stateAuthoringId);
                }
            }
            bool valid = true;
            foreach (AgentSnapshotGraph graph in graphs ?? Array.Empty<AgentSnapshotGraph>())
            {
                string path = $"editable.graphs[{graph.graphAuthoringId}]";
                if (!string.Equals(graph.ownership, AgentGraphOwnership.RootAsset.ToString(), StringComparison.Ordinal) &&
                    !ownerIds.Contains(graph.ownerElementAuthoringId ?? string.Empty))
                {
                    report.Error(path + ".owner", "graph_owner_unknown", $"Graph owner不在文档包entity集合：{graph.ownerElementAuthoringId}");
                    valid = false;
                }
                foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
                {
                    foreach (AgentSnapshotGraphReference reference in node.graphReferences ?? new List<AgentSnapshotGraphReference>())
                    {
                        if (string.IsNullOrEmpty(reference.graphAuthoringId))
                            continue;
                        if (!graphById.TryGetValue(reference.graphAuthoringId, out AgentSnapshotGraph child))
                        {
                            report.Error(path + ".nodes.graphReferences", "graph_reference_unknown", $"Graph reference不在文档包Graph集合：{reference.graphAuthoringId}");
                            valid = false;
                            continue;
                        }
                        if (!string.Equals(child.ownerElementAuthoringId, node.elementAuthoringId, StringComparison.Ordinal) ||
                            !string.Equals(child.ownership, reference.ownership, StringComparison.Ordinal))
                        {
                            report.Error(path + ".nodes.graphReferences", "graph_reference_owner_mismatch", $"Graph reference与目标Graph owner或ownership不一致：{reference.graphAuthoringId}");
                            valid = false;
                        }
                    }
                }
                foreach (AgentSnapshotFlowEdge edge in graph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
                {
                    if (string.IsNullOrEmpty(edge.conditionRuleGraphAuthoringId))
                        continue;
                    if (!graphById.TryGetValue(edge.conditionRuleGraphAuthoringId, out AgentSnapshotGraph child))
                    {
                        report.Error(path + ".flowEdges.conditionGraph", "graph_reference_unknown", $"Condition Graph reference不在文档包Graph集合：{edge.conditionRuleGraphAuthoringId}");
                        valid = false;
                        continue;
                    }
                    if (!string.Equals(child.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal) ||
                        !string.Equals(child.ownerElementAuthoringId, edge.elementAuthoringId, StringComparison.Ordinal) ||
                        !IsChildGraphOwnership(child.ownership))
                    {
                        report.Error(path + ".flowEdges.conditionGraph", "graph_reference_owner_mismatch", $"Condition Graph reference与目标Graph kind、owner或ownership不一致：{edge.conditionRuleGraphAuthoringId}");
                        valid = false;
                    }
                }
            }
            foreach (AgentSnapshotGraph graph in graphs ?? Array.Empty<AgentSnapshotGraph>())
            {
                if (string.Equals(graph.ownership, AgentGraphOwnership.RootAsset.ToString(), StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(graph.ownerElementAuthoringId))
                    {
                        report.Error($"editable.graphs[{graph.graphAuthoringId}].owner", "graph_root_owner_invalid", "Root Graph不能声明owner。");
                        valid = false;
                    }
                    continue;
                }

                if (string.Equals(graph.kind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                {
                    List<AgentSnapshotFlowEdge> references = flowEdges.Values
                        .Where(edge => string.Equals(
                            edge.conditionRuleGraphAuthoringId,
                            graph.graphAuthoringId,
                            StringComparison.Ordinal))
                        .ToList();
                    if (references.Count == 1 &&
                        string.Equals(
                            references[0].elementAuthoringId,
                            graph.ownerElementAuthoringId,
                            StringComparison.Ordinal) &&
                        !nodes.ContainsKey(graph.ownerElementAuthoringId ?? string.Empty) &&
                        IsChildGraphOwnership(graph.ownership))
                    {
                        continue;
                    }
                    report.Error($"editable.graphs[{graph.graphAuthoringId}].owner", "graph_owner_reference_invalid", "ConditionRule Graph必须由owner FlowEdge中的唯一conditionGraph反向指向。");
                    valid = false;
                    continue;
                }

                if (IsSkillGraph(editable, graph))
                {
                    if (ValidateSkillGraphOwner(editable, graph))
                        continue;
                    report.Error(
                        $"editable.graphs[{graph.graphAuthoringId}].owner",
                        "skill_graph_owner_invalid",
                        "Skill入口Graph必须由对应State的behaviorGraph反向指向。");
                    valid = false;
                    continue;
                }

                List<(AgentSnapshotNode Owner, AgentSnapshotGraphReference Reference)> nodeReferences =
                    nodes.Values
                        .SelectMany(owner => (owner.graphReferences ?? new List<AgentSnapshotGraphReference>())
                            .Where(reference => string.Equals(
                                reference.graphAuthoringId,
                                graph.graphAuthoringId,
                                StringComparison.Ordinal))
                            .Select(reference => (owner, reference)))
                        .ToList();
                List<AgentSnapshotAuthoringRouteSegment> timelineTreeClipRoutes =
                    (graph.routes ?? new List<AgentSnapshotAuthoringRoute>())
                        .Where(route => route?.segments != null && route.segments.Count > 0)
                        .Select(route => route.segments[route.segments.Count - 1])
                        .Where(segment =>
                            segment != null &&
                            string.Equals(segment.kind, TreeAuthoringRouteSegmentKind.TimelineTreeClip.ToString(), StringComparison.Ordinal) &&
                            string.Equals(segment.childGraphAuthoringId, graph.graphAuthoringId, StringComparison.Ordinal))
                        .ToList();
                if (timelineTreeClipRoutes.Count > 0)
                {
                    if (ValidateTimelineTreeClipGraphOwner(
                            editable,
                            graph,
                            timelineTreeClipRoutes,
                            nodeReferences,
                            flowEdges,
                            nodes))
                    {
                        continue;
                    }
                    report.Error(
                        $"editable.graphs[{graph.graphAuthoringId}].owner",
                        "graph_owner_reference_invalid",
                        "TimelineTreeClip子Graph必须由唯一route、Timeline、Track、Clip与Graph owner双向一致地持有。");
                    valid = false;
                    continue;
                }
                if (nodeReferences.Count == 1 &&
                    string.Equals(
                        nodeReferences[0].Owner.elementAuthoringId,
                        graph.ownerElementAuthoringId,
                        StringComparison.Ordinal) &&
                    !flowEdges.ContainsKey(graph.ownerElementAuthoringId ?? string.Empty) &&
                    string.Equals(
                        nodeReferences[0].Reference.ownership,
                        graph.ownership,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                report.Error($"editable.graphs[{graph.graphAuthoringId}].owner", "graph_owner_reference_invalid", "非ConditionRule子Graph必须由owner Node中的唯一Graph reference反向指向。");
                valid = false;
            }
            return valid;
        }

        static bool ValidateSkillGraphOwner(AgentDocumentEditable editable, AgentSnapshotGraph graph)
        {
            foreach (AgentSnapshotStateMachineSummary stateMachine in editable.stateMachines ?? new List<AgentSnapshotStateMachineSummary>())
            {
                foreach (AgentSnapshotStateSummary state in stateMachine?.states ?? new List<AgentSnapshotStateSummary>())
                {
                    if (state != null &&
                        string.Equals(state.stateAuthoringId, graph.ownerElementAuthoringId, StringComparison.Ordinal) &&
                        string.Equals(state.behaviorGraphAuthoringId, graph.graphAuthoringId, StringComparison.Ordinal))
                        return true;
                }
            }
            return false;
        }

        static bool IsSkillGraph(AgentDocumentEditable editable, AgentSnapshotGraph graph)
        {
            return (editable?.stateMachines ?? new List<AgentSnapshotStateMachineSummary>())
                .Any(stateMachine => (stateMachine?.states ?? new List<AgentSnapshotStateSummary>())
                    .Any(state => state != null && string.Equals(
                        state.behaviorGraphAuthoringId,
                        graph?.graphAuthoringId,
                        StringComparison.Ordinal)));
        }

        internal bool TryToGraphFiles(
            string domain,
            AgentSnapshotGraph graph,
            AgentCompileReport report,
            out AgentPackageGraphFile graphFile,
            out AgentPackageLayoutFile layoutFile)
        {
            graphFile = new AgentPackageGraphFile
            {
                id = graph.graphAuthoringId,
                kind = graph.kind,
                ownership = graph.ownership,
                owner = string.IsNullOrEmpty(graph.ownerElementAuthoringId) && string.IsNullOrEmpty(graph.referenceKey)
                    ? null
                    : new AgentPackageGraphOwner
                    {
                        entityId = graph.ownerElementAuthoringId,
                        slot = OwnerSlot(graph.kind)
                    },
                sharedAssetPath = graph.sharedAssetPath
            };
            layoutFile = new AgentPackageLayoutFile { graphId = graph.graphAuthoringId };
            var nodeNames = new Dictionary<string, string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
            {
                if (!m_Catalog.TryGetKind(node.typeName, out string kind))
                {
                    report.Error($"editable/graphs/{graph.graphAuthoringId}/nodes/{node.elementAuthoringId}", "authoring_capability_incomplete", $"Node类型没有完整authoring capability：{node.typeName}");
                    valid = false;
                    continue;
                }
                if (m_Catalog.TryGetAnchor(node.typeName, out string anchor))
                {
                    nodeNames[node.elementAuthoringId] = anchor;
                    continue;
                }
                if (!m_Catalog.IsNodeTypeAllowed(node.typeName, domain))
                {
                    report.Error(
                        $"editable/graphs/{graph.graphAuthoringId}/nodes/{node.elementAuthoringId}",
                        "node_domain_forbidden",
                        $"{domain}不允许Node kind：{kind}");
                    valid = false;
                    continue;
                }
                if (!m_Catalog.IsFullyRoundTrippable(kind))
                {
                    report.Error(
                        $"editable/graphs/{graph.graphAuthoringId}/nodes/{node.elementAuthoringId}",
                        "authoring_capability_incomplete",
                        $"Node kind没有完整create/configure/delete闭包：{kind}");
                    valid = false;
                    continue;
                }
                nodeNames[node.elementAuthoringId] = node.elementAuthoringId;
                var properties = new JObject();
                string nodePath = $"editable/graphs/{graph.graphAuthoringId}/nodes/{node.elementAuthoringId}";
                bool editGraphReferences = m_Catalog.CanEditProperty(kind, "graphReferences");
                bool editAssetReferences = m_Catalog.CanEditProperty(kind, "assetReferences");
                if (editGraphReferences &&
                    (node.graphReferences ?? new List<AgentSnapshotGraphReference>())
                    .Any(reference => reference.required && string.IsNullOrEmpty(reference.graphAuthoringId)))
                {
                    report.Error(nodePath, "authoring_capability_incomplete", "必需Graph reference当前没有可往返目标。");
                    valid = false;
                }
                if (editAssetReferences &&
                    (node.assetReferences ?? new List<AgentSnapshotAssetReference>())
                    .Any(reference => reference.required && string.IsNullOrEmpty(reference.assetPath) && string.IsNullOrEmpty(reference.assetGuid)))
                {
                    report.Error(nodePath, "authoring_capability_incomplete", "必需Asset reference当前没有可往返目标。");
                    valid = false;
                }
                if (editGraphReferences)
                {
                    Add(properties, "graphReferences", (node.graphReferences ?? new List<AgentSnapshotGraphReference>())
                        .Where(reference => !string.IsNullOrEmpty(reference.graphAuthoringId))
                        .Select(reference => new AgentPackageGraphReference
                        {
                            key = reference.key,
                            graphId = reference.graphAuthoringId,
                            ownership = reference.ownership,
                            sharedAssetPath = reference.sharedAssetPath,
                            inputBindings = AgentAuthoringDocumentCodec.Clone(reference.inputBindings) ?? new List<AgentSnapshotGraphParameterBinding>(),
                            outputBindings = AgentAuthoringDocumentCodec.Clone(reference.outputBindings) ?? new List<AgentSnapshotGraphParameterBinding>()
                        }).ToList());
                }
                if (editAssetReferences)
                {
                    Add(properties, "assetReferences", (node.assetReferences ?? new List<AgentSnapshotAssetReference>())
                        .Where(reference => !string.IsNullOrEmpty(reference.assetPath) || !string.IsNullOrEmpty(reference.assetGuid))
                        .Select(reference => new AgentPackageAssetReference
                        {
                            key = reference.key,
                            assetPath = reference.assetPath,
                            assetGuid = reference.assetGuid
                        }).ToList());
                }
                if (node.exposedProperty != null)
                {
                    Add(properties, "exposedProperty", new AgentPackageExposedProperty
                    {
                        mode = node.exposedProperty.mode,
                        declarationId = node.exposedProperty.declarationAuthoringId,
                        valueType = node.exposedProperty.valueType,
                        value = node.exposedProperty.value?.DeepClone()
                    });
                }
                if (!string.Equals(node.loopStopType, LoopNode.StopType.None.ToString(), StringComparison.Ordinal))
                    Add(properties, "loopStopType", node.loopStopType);
                if (!string.Equals(node.compareType, CompareNode.CompareType.Equal.ToString(), StringComparison.Ordinal))
                    Add(properties, "compareType", node.compareType);
                if (m_Catalog.CanEditProperty(kind, "moveSpeed"))
                    Add(properties, "moveSpeed", node.moveSpeed);
                if (m_Catalog.CanEditProperty(kind, "displacementMode"))
                    Add(properties, "displacementMode", node.displacementMode);
                if (m_Catalog.CanEditProperty(kind, "turnSpeedDegrees"))
                    Add(properties, "turnSpeedDegrees", node.turnSpeedDegrees);
                if (m_Catalog.CanEditProperty(kind, "cameraRelative"))
                    Add(properties, "cameraRelative", node.cameraRelative);
                if (m_Catalog.CanEditProperty(kind, "executionMode"))
                    Add(properties, "executionMode", node.executionMode);
                if (m_Catalog.CanEditProperty(kind, "durationSeconds"))
                    Add(properties, "durationSeconds", node.durationSeconds);
                if (m_Catalog.CanEditProperty(kind, "inputId"))
                    Add(properties, "inputId", node.inputId);
                if (m_Catalog.CanEditProperty(kind, "requestId"))
                    Add(properties, "requestId", node.requestId);
                if (m_Catalog.CanEditProperty(kind, "blackboardDeclarationId"))
                    Add(properties, "blackboardDeclarationId", node.blackboardDeclarationId);
                if (m_Catalog.CanEditProperty(kind, "stateExitCause"))
                    Add(properties, "stateExitCause", node.stateExitCause);
                if (m_Catalog.CanEditProperty(kind, "actionContextId"))
                    properties["actionContextId"] = node.actionContextId ?? string.Empty;
                if (m_Catalog.CanEditProperty(kind, "windowType"))
                    Add(properties, "windowType", node.windowType);
                if (m_Catalog.CanEditProperty(kind, "actionProfileId"))
                    Add(properties, "actionProfileId", node.actionProfileId);
                if (m_Catalog.CanEditProperty(kind, "targetSnapshotBlackboardDeclarationId"))
                    Add(properties, "targetSnapshotBlackboardDeclarationId", node.targetSnapshotBlackboardDeclarationId);
                graphFile.nodes.Add(new AgentPackageNode
                {
                    id = node.elementAuthoringId,
                    kind = kind,
                    name = string.Equals(node.displayName, node.nodeTypeDisplayName, StringComparison.Ordinal) ? null : node.displayName,
                    properties = properties.HasValues ? properties : null
                });
                if (node.position != null)
                {
                    layoutFile.nodes.Add(new AgentPackageNodeLayout
                    {
                        id = node.elementAuthoringId,
                        x = node.position.x,
                        y = node.position.y
                    });
                }
            }

            foreach (AgentSnapshotFlowEdge edge in graph.flowEdges ?? new List<AgentSnapshotFlowEdge>())
            {
                graphFile.flowEdges.Add(new AgentPackageFlowEdge
                {
                    id = edge.elementAuthoringId,
                    from = new AgentPackageEdgeEndpoint
                    {
                        node = ResolveEndpoint(nodeNames, edge.startElementAuthoringId),
                        port = edge.startPort
                    },
                    to = new AgentPackageEdgeEndpoint
                    {
                        node = ResolveEndpoint(nodeNames, edge.endElementAuthoringId),
                        port = edge.endPort
                    },
                    flowOrder = edge.flowOrder,
                    transitionPriority = edge.transitionPriority,
                    abortPolicy = edge.abortPolicy,
                    conditionGraph = edge.conditionRuleGraphAuthoringId
                });
            }
            foreach (AgentSnapshotPropertyEdge edge in graph.propertyEdges ?? new List<AgentSnapshotPropertyEdge>())
            {
                graphFile.propertyEdges.Add(new AgentPackagePropertyEdge
                {
                    id = edge.elementAuthoringId,
                    from = new AgentPackageEdgeEndpoint
                    {
                        node = ResolveEndpoint(nodeNames, edge.startElementAuthoringId),
                        port = edge.startPortId
                    },
                    to = new AgentPackageEdgeEndpoint
                    {
                        node = ResolveEndpoint(nodeNames, edge.endElementAuthoringId),
                        port = edge.endPortId
                    }
                });
            }
            return valid;
        }

        internal bool TryFromGraphFiles(
            string domain,
            string path,
            AgentPackageGraphFile graphFile,
            AgentPackageLayoutFile layoutFile,
            AgentSnapshotGraph current,
            AgentCompileReport report,
            AgentAuthoringDocumentReadPhase phase,
            out AgentSnapshotGraph graph)
        {
            graph = new AgentSnapshotGraph
            {
                graphAuthoringId = graphFile.id,
                path = current?.path,
                name = current?.name,
                kind = graphFile.kind,
                ownership = graphFile.ownership,
                ownerElementAuthoringId = graphFile.owner?.entityId,
                referenceKey = current?.referenceKey ?? graphFile.owner?.slot,
                sharedAssetPath = graphFile.sharedAssetPath,
                routes = current?.routes ?? new List<AgentSnapshotAuthoringRoute>()
            };
            if (string.IsNullOrWhiteSpace(graph.graphAuthoringId) ||
                string.IsNullOrWhiteSpace(graph.kind) ||
                graphFile.nodes == null ||
                graphFile.flowEdges == null ||
                graphFile.propertyEdges == null)
            {
                report.Error(path, "graph_required_field_missing", "Graph缺少id、kind、nodes或edges。");
                return false;
            }
            if (!Enum.TryParse(
                    graphFile.ownership,
                    false,
                    out AgentGraphOwnership ownership) ||
                ownership == AgentGraphOwnership.Unknown ||
                ownership == AgentGraphOwnership.RootAsset && graphFile.owner != null ||
                ownership != AgentGraphOwnership.RootAsset && graphFile.owner == null ||
                ownership == AgentGraphOwnership.SharedAsset && string.IsNullOrWhiteSpace(graphFile.sharedAssetPath) ||
                ownership != AgentGraphOwnership.SharedAsset && !string.IsNullOrEmpty(graphFile.sharedAssetPath))
            {
                report.Error(path + ".ownership", "graph_ownership_invalid", "Graph ownership、owner与sharedAssetPath组合不符合合同。");
                return false;
            }
            if (!ValidateGraphFile(domain, path, graphFile, layoutFile, current, phase, report))
                return false;

            var positions = (layoutFile.nodes ?? new List<AgentPackageNodeLayout>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.id))
                .ToDictionary(value => value.id, value => value, StringComparer.Ordinal);
            var currentNodes = (current?.nodes ?? new List<AgentSnapshotNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.elementAuthoringId))
                .ToDictionary(node => node.elementAuthoringId, node => node, StringComparer.Ordinal);
            Dictionary<string, AgentPackageNodeLayout> generatedPositions =
                BuildGeneratedPositions(graphFile, positions, currentNodes);
            var anchors = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (AgentSnapshotNode node in current?.nodes ?? new List<AgentSnapshotNode>())
            {
                if (!m_Catalog.TryGetAnchor(node.typeName, out string anchor))
                    continue;
                anchors[anchor] = node.elementAuthoringId;
                graph.nodes.Add(AgentAuthoringDocumentCodec.Clone(node));
            }

            for (int index = 0; index < graphFile.nodes.Count; index++)
            {
                AgentPackageNode packageNode = graphFile.nodes[index];
                string nodePath = $"{path}.nodes[{packageNode?.id}]";
                currentNodes.TryGetValue(packageNode?.id ?? string.Empty, out AgentSnapshotNode currentNode);
                if (packageNode == null ||
                    string.IsNullOrWhiteSpace(packageNode.id) ||
                    string.IsNullOrWhiteSpace(packageNode.kind) ||
                    !m_Catalog.TryGetTypeName(packageNode.kind, out string typeName))
                    return false;
                if (m_Catalog.IsSystemKind(packageNode.kind))
                {
                    report.Error(nodePath, "system_node_not_editable", "系统Node必须使用anchor，不能出现在nodes集合。");
                    return false;
                }
                AgentPackageNodeLayout position = positions.TryGetValue(packageNode.id, out AgentPackageNodeLayout explicitPosition)
                    ? explicitPosition
                    : currentNode?.position != null
                        ? new AgentPackageNodeLayout
                        {
                            id = packageNode.id,
                            x = currentNode.position.x,
                            y = currentNode.position.y
                        }
                        : generatedPositions[packageNode.id];
                JObject properties = packageNode.properties ?? new JObject();
                graph.nodes.Add(new AgentSnapshotNode
                {
                    elementAuthoringId = packageNode.id,
                    typeName = typeName,
                    displayName = string.IsNullOrEmpty(packageNode.name)
                        ? currentNode?.nodeTypeDisplayName ?? packageNode.kind
                        : packageNode.name,
                    nodeTypeDisplayName = currentNode?.nodeTypeDisplayName ?? packageNode.kind,
                    position = new AgentSnapshotVector2 { x = position.x, y = position.y },
                    graphReferences = m_Catalog.CanEditProperty(packageNode.kind, "graphReferences")
                        ? MergeGraphReferences(
                            ReadList<AgentPackageGraphReference>(properties, "graphReferences"),
                            currentNode)
                        : AgentAuthoringDocumentCodec.Clone(currentNode?.graphReferences ?? new List<AgentSnapshotGraphReference>()),
                    assetReferences = m_Catalog.CanEditProperty(packageNode.kind, "assetReferences")
                        ? MergeAssetReferences(
                            ReadList<AgentPackageAssetReference>(properties, "assetReferences"),
                            currentNode)
                        : AgentAuthoringDocumentCodec.Clone(currentNode?.assetReferences ?? new List<AgentSnapshotAssetReference>()),
                    propertyPorts = currentNode?.propertyPorts ?? new List<AgentSnapshotPropertyPort>(),
                    exposedProperty = ToExposedProperty(
                        properties["exposedProperty"]?.ToObject<AgentPackageExposedProperty>(),
                        currentNode),
                    loopStopType = string.Equals(packageNode.kind, "loop", StringComparison.Ordinal)
                        ? properties.Value<string>("loopStopType") ?? LoopNode.StopType.None.ToString()
                        : null,
                    compareType = string.Equals(packageNode.kind, "compare", StringComparison.Ordinal)
                        ? properties.Value<string>("compareType") ?? CompareNode.CompareType.Equal.ToString()
                        : null,
                    moveSpeed = m_Catalog.CanEditProperty(packageNode.kind, "moveSpeed")
                        ? properties.Value<float>("moveSpeed")
                        : 0f,
                    displacementMode = m_Catalog.CanEditProperty(packageNode.kind, "displacementMode")
                        ? properties.Value<string>("displacementMode")
                        : null,
                    turnSpeedDegrees = m_Catalog.CanEditProperty(packageNode.kind, "turnSpeedDegrees")
                        ? properties.Value<float>("turnSpeedDegrees")
                        : 0f,
                    cameraRelative = m_Catalog.CanEditProperty(packageNode.kind, "cameraRelative") &&
                                     properties.Value<bool>("cameraRelative"),
                    executionMode = m_Catalog.CanEditProperty(packageNode.kind, "executionMode")
                        ? properties.Value<string>("executionMode")
                        : null,
                    durationSeconds = m_Catalog.CanEditProperty(packageNode.kind, "durationSeconds")
                        ? properties.Value<float>("durationSeconds")
                        : 0f,
                    inputId = m_Catalog.CanEditProperty(packageNode.kind, "inputId")
                        ? properties.Value<string>("inputId")
                        : null,
                    requestId = m_Catalog.CanEditProperty(packageNode.kind, "requestId")
                        ? properties.Value<string>("requestId")
                        : null,
                    blackboardDeclarationId = m_Catalog.CanEditProperty(packageNode.kind, "blackboardDeclarationId")
                        ? properties.Value<string>("blackboardDeclarationId")
                        : null,
                    stateExitCause = m_Catalog.CanEditProperty(packageNode.kind, "stateExitCause")
                        ? properties.Value<string>("stateExitCause")
                        : null,
                    actionContextId = m_Catalog.CanEditProperty(packageNode.kind, "actionContextId")
                        ? properties.Value<string>("actionContextId")
                        : null,
                    windowType = m_Catalog.CanEditProperty(packageNode.kind, "windowType")
                        ? properties.Value<string>("windowType")
                        : null,
                    actionProfileId = m_Catalog.CanEditProperty(packageNode.kind, "actionProfileId")
                        ? properties.Value<string>("actionProfileId")
                        : null,
                    targetSnapshotBlackboardDeclarationId = m_Catalog.CanEditProperty(packageNode.kind, "targetSnapshotBlackboardDeclarationId")
                        ? properties.Value<string>("targetSnapshotBlackboardDeclarationId")
                        : null
                });
            }

            foreach (AgentPackageFlowEdge packageEdge in graphFile.flowEdges)
            {
                if (!TryEndpoint(packageEdge?.from, anchors, path, report, out string from) ||
                    !TryEndpoint(packageEdge?.to, anchors, path, report, out string to))
                    return false;
                graph.flowEdges.Add(new AgentSnapshotFlowEdge
                {
                    elementAuthoringId = packageEdge.id,
                    startElementAuthoringId = from,
                    endElementAuthoringId = to,
                    startPort = packageEdge.from.port,
                    endPort = packageEdge.to.port,
                    flowOrder = packageEdge.flowOrder,
                    transitionPriority = packageEdge.transitionPriority,
                    abortPolicy = packageEdge.abortPolicy,
                    conditionRuleGraphAuthoringId = packageEdge.conditionGraph
                });
            }
            foreach (AgentPackagePropertyEdge packageEdge in graphFile.propertyEdges)
            {
                if (!TryEndpoint(packageEdge?.from, anchors, path, report, out string from) ||
                    !TryEndpoint(packageEdge?.to, anchors, path, report, out string to))
                    return false;
                graph.propertyEdges.Add(new AgentSnapshotPropertyEdge
                {
                    elementAuthoringId = packageEdge.id,
                    startElementAuthoringId = from,
                    endElementAuthoringId = to,
                    startPortId = packageEdge.from.port,
                    endPortId = packageEdge.to.port
                });
            }
            return true;
        }

        bool ValidateGraphFile(
            string domain,
            string path,
            AgentPackageGraphFile graphFile,
            AgentPackageLayoutFile layoutFile,
            AgentSnapshotGraph current,
            AgentAuthoringDocumentReadPhase phase,
            AgentCompileReport report)
        {
            if (!IsIdentity(graphFile.id) || !m_Catalog.IsGraphKindAllowed(graphFile.kind, domain))
            {
                report.Error(path, "graph_identity_or_kind_invalid", "Graph id非法，或kind不属于当前domain。");
                return false;
            }
            if (current != null && !string.Equals(current.kind, graphFile.kind, StringComparison.Ordinal))
            {
                report.Error(path + ".kind", "graph_kind_changed", "已有Graph不能原地改变kind，必须删除旧identity并创建新local identity。");
                return false;
            }
            if (graphFile.owner != null &&
                (!IsIdentity(graphFile.owner.entityId) || !m_Catalog.IsOwnerSlotAllowed(graphFile.kind, graphFile.owner.slot)))
            {
                report.Error(path + ".owner", "graph_owner_invalid", "Graph owner identity或slot不符合Graph kind合同。");
                return false;
            }

            var nodes = new Dictionary<string, AgentPackageNode>(StringComparer.Ordinal);
            foreach (AgentPackageNode node in graphFile.nodes)
            {
                if (node == null || !IsIdentity(node.id) || nodes.ContainsKey(node.id))
                {
                    report.Error(path + ".nodes", "node_identity_invalid", "Node identity缺失、重复或local语法非法。");
                    return false;
                }
                if (!m_Catalog.IsNodeAllowed(node.kind, graphFile.kind, domain))
                {
                    report.Error(path + $".nodes[{node.id}].kind", "node_kind_not_allowed", $"{domain}/{graphFile.kind}不允许Node kind：{node.kind}");
                    return false;
                }
                AgentSnapshotNode oldNode = current?.nodes?.FirstOrDefault(value =>
                    string.Equals(value.elementAuthoringId, node.id, StringComparison.Ordinal));
                bool allowExistingEmptyActionContext = AllowsExistingEmptyActionContext(
                    phase,
                    node,
                    oldNode);
                if (!m_Catalog.ValidateProperties(
                        node.kind,
                        node.properties,
                        report,
                        path + $".nodes[{node.id}]",
                        allowExistingEmptyActionContext))
                    return false;
                if (!ValidateGraphReferenceBindings(
                        node.properties,
                        path + $".nodes[{node.id}].properties.graphReferences",
                        report))
                    return false;
                if (!m_Catalog.TryProjectDocumentPortShape(
                        node.kind,
                        node.properties,
                        out _,
                        out _,
                        out GraphAuthoringPortShapeException shapeError))
                {
                    report.Error(
                        path + $".nodes[{node.id}].properties",
                        shapeError.Code,
                        shapeError.Message);
                    return false;
                }
                if (string.Equals(node.kind, "exposed-property", StringComparison.Ordinal))
                {
                    JToken exposedToken = node.properties?["exposedProperty"];
                    string declarationId = exposedToken?["declarationId"]?.Value<string>();
                    if (exposedToken is not JObject || !IsIdentity(declarationId))
                    {
                        report.Error(
                            path + $".nodes[{node.id}].properties.exposedProperty",
                            "exposed_property_required",
                            "exposed-property必须声明有效mode、declarationId、valueType与模式匹配的value。");
                        return false;
                    }
                }
                if (oldNode != null &&
                    m_Catalog.TryGetKind(oldNode.typeName, out string oldKind) &&
                    !string.Equals(oldKind, node.kind, StringComparison.Ordinal))
                {
                    report.Error(path + $".nodes[{node.id}].kind", "node_kind_changed", "已有Node不能原地改变kind，必须删除旧identity并创建新local identity。");
                    return false;
                }
                nodes.Add(node.id, node);
            }

            var layoutIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageNodeLayout layout in layoutFile.nodes ?? new List<AgentPackageNodeLayout>())
            {
                if (layout == null ||
                    !layoutIds.Add(layout.id ?? string.Empty) ||
                    !nodes.ContainsKey(layout.id ?? string.Empty) ||
                    layout.id.StartsWith("@", StringComparison.Ordinal))
                {
                    report.Error(path.Replace("graph.json", "layout.json"), "layout_node_invalid", "Layout必须唯一引用editable Node，不能引用anchor或未知Node。");
                    return false;
                }
            }

            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageFlowEdge edge in graphFile.flowEdges)
            {
                if (!ValidateEdgeIdentity(edge?.id, edgeIds, path + ".flowEdges", report) ||
                    !ValidateEndpoint(domain, graphFile.kind, nodes, edge.from, "Output", false, path + $".flowEdges[{edge.id}].from", report) ||
                    !ValidateEndpoint(domain, graphFile.kind, nodes, edge.to, "Input", false, path + $".flowEdges[{edge.id}].to", report))
                    return false;
            }
            foreach (AgentPackagePropertyEdge edge in graphFile.propertyEdges)
            {
                if (!ValidateEdgeIdentity(edge?.id, edgeIds, path + ".propertyEdges", report) ||
                    !ValidateEndpoint(domain, graphFile.kind, nodes, edge.from, "Output", true, path + $".propertyEdges[{edge.id}].from", report) ||
                    !ValidateEndpoint(domain, graphFile.kind, nodes, edge.to, "Input", true, path + $".propertyEdges[{edge.id}].to", report))
                    return false;
            }
            return ValidatePortCapacities(nodes, graphFile, path, report);
        }

        static bool ValidateGraphReferenceBindings(
            JObject properties,
            string path,
            AgentCompileReport report)
        {
            if (properties?["graphReferences"] is not JArray references)
                return true;
            bool valid = true;
            for (int referenceIndex = 0; referenceIndex < references.Count; referenceIndex++)
            {
                if (references[referenceIndex] is not JObject reference)
                {
                    report.Error(path, "graph_reference_invalid", "Graph reference必须是object。");
                    valid = false;
                    continue;
                }
                foreach (JProperty property in reference.Properties())
                {
                    if (property.Name != "key" &&
                        property.Name != "graphId" &&
                        property.Name != "ownership" &&
                        property.Name != "sharedAssetPath" &&
                        property.Name != "inputBindings" &&
                        property.Name != "outputBindings")
                    {
                        report.Error(
                            $"{path}[{referenceIndex}].{property.Name}",
                            "graph_reference_field_unknown",
                            "Graph reference包含未登记字段。");
                        valid = false;
                    }
                }
                if (string.IsNullOrWhiteSpace(reference.Value<string>("key")))
                {
                    report.Error($"{path}[{referenceIndex}].key", "graph_reference_key_missing", "Graph reference必须声明key。");
                    valid = false;
                }
                string graphId = reference.Value<string>("graphId");
                if (!AgentSkillDocumentMapper.IsIdentity(graphId))
                {
                    report.Error($"{path}[{referenceIndex}].graphId", "graph_reference_identity_invalid", "Graph reference必须使用稳定或local identity。");
                    valid = false;
                }
                valid &= ValidateGraphParameterBindings(
                    reference["inputBindings"] as JArray,
                    $"{path}[{referenceIndex}].inputBindings",
                    report);
                valid &= ValidateGraphParameterBindings(
                    reference["outputBindings"] as JArray,
                    $"{path}[{referenceIndex}].outputBindings",
                    report);
            }
            return valid;
        }

        static bool ValidateGraphParameterBindings(
            JArray bindings,
            string path,
            AgentCompileReport report)
        {
            if (bindings == null)
                return true;
            var names = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            for (int index = 0; index < bindings.Count; index++)
            {
                if (bindings[index] is not JObject binding)
                {
                    report.Error($"{path}[{index}]", "graph_parameter_binding_invalid", "Graph parameter binding必须是object。");
                    valid = false;
                    continue;
                }
                foreach (JProperty property in binding.Properties())
                {
                    if (property.Name != "parameterName" &&
                        property.Name != "declarationId" &&
                        property.Name != "portId" &&
                        property.Name != "valueType")
                    {
                        report.Error(
                            $"{path}[{index}].{property.Name}",
                            "graph_parameter_binding_field_unknown",
                            "Graph parameter binding包含未登记字段。");
                        valid = false;
                    }
                }
                string parameterName = binding.Value<string>("parameterName");
                string declarationId = binding.Value<string>("declarationId");
                string portId = binding.Value<string>("portId");
                string valueType = binding.Value<string>("valueType");
                if (string.IsNullOrWhiteSpace(parameterName) ||
                    !names.Add(parameterName) ||
                    !AgentSkillDocumentMapper.IsIdentity(declarationId) ||
                    string.IsNullOrWhiteSpace(portId) ||
                    string.IsNullOrWhiteSpace(valueType))
                {
                    report.Error($"{path}[{index}]", "graph_parameter_binding_invalid", "Graph parameter binding必须具有唯一parameterName、declarationId、portId和valueType。");
                    valid = false;
                }
            }
            return valid;
        }

        bool AllowsExistingEmptyActionContext(
            AgentAuthoringDocumentReadPhase phase,
            AgentPackageNode node,
            AgentSnapshotNode oldNode)
        {
            if (phase != AgentAuthoringDocumentReadPhase.CheckoutRoundTrip ||
                !string.Equals(node.kind, "action-context-active", StringComparison.Ordinal) ||
                oldNode == null ||
                !string.IsNullOrEmpty(oldNode.actionContextId) ||
                !m_Catalog.TryGetKind(oldNode.typeName, out string oldKind) ||
                !string.Equals(oldKind, node.kind, StringComparison.Ordinal))
                return false;
            JToken value = node.properties?["actionContextId"];
            return value == null ||
                   value.Type == JTokenType.String &&
                   string.IsNullOrEmpty(value.Value<string>());
        }

        bool ValidateEndpoint(
            string domain,
            string graphKind,
            IReadOnlyDictionary<string, AgentPackageNode> nodes,
            AgentPackageEdgeEndpoint endpoint,
            string direction,
            bool property,
            string path,
            AgentCompileReport report)
        {
            if (endpoint == null || string.IsNullOrWhiteSpace(endpoint.node) || string.IsNullOrWhiteSpace(endpoint.port))
            {
                report.Error(path, "edge_endpoint_invalid", "Edge endpoint必须声明node与port。");
                return false;
            }
            if (endpoint.node.StartsWith("@", StringComparison.Ordinal))
            {
                if (m_Catalog.IsAnchorPortAllowed(graphKind, endpoint.node, endpoint.port, direction, property, domain))
                    return true;
                report.Error(path, "anchor_or_port_unknown", $"Graph kind不允许anchor或port：{endpoint.node}.{endpoint.port}");
                return false;
            }
            if (!nodes.TryGetValue(endpoint.node, out AgentPackageNode node))
            {
                report.Error(path, "edge_node_unknown", $"Edge引用未知Node：{endpoint.node}");
                return false;
            }
            if (!m_Catalog.TryResolveDocumentPort(
                    node.kind,
                    node.properties,
                    endpoint.port,
                    property,
                    out GraphAuthoringDynamicPortProjection port,
                    out GraphAuthoringPortShapeException error))
            {
                report.Error(path, error.Code, error.Message);
                return false;
            }
            GraphAuthoringPortDirection expected = string.Equals(
                direction,
                GraphAuthoringPortDirection.Input.ToString(),
                StringComparison.Ordinal)
                ? GraphAuthoringPortDirection.Input
                : GraphAuthoringPortDirection.Output;
            if (port.Direction == expected)
                return true;
            string mode = node.properties?["exposedProperty"]?["mode"]?.Value<string>() ?? string.Empty;
            report.Error(
                path,
                "port_shape_direction_mismatch",
                $"Node '{endpoint.node}' kind='{node.kind}' mode='{mode}' 的 port '{endpoint.port}' 实际方向为 {port.Direction}，edge endpoint 要求 {expected}。");
            return false;
        }

        bool ValidatePortCapacities(
            IReadOnlyDictionary<string, AgentPackageNode> nodes,
            AgentPackageGraphFile graph,
            string path,
            AgentCompileReport report)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            void Count(AgentPackageEdgeEndpoint endpoint, bool property)
            {
                if (endpoint?.node?.StartsWith("@", StringComparison.Ordinal) != false)
                    return;
                string key = endpoint.node + "\0" + (property ? "property:" : "flow:") + endpoint.port;
                counts.TryGetValue(key, out int count);
                counts[key] = count + 1;
            }
            foreach (AgentPackageFlowEdge edge in graph.flowEdges)
            {
                Count(edge.from, false);
                Count(edge.to, false);
            }
            foreach (AgentPackagePropertyEdge edge in graph.propertyEdges)
            {
                Count(edge.from, true);
                Count(edge.to, true);
            }

            bool valid = true;
            foreach (KeyValuePair<string, int> pair in counts.Where(value => value.Value > 1))
            {
                string[] identity = pair.Key.Split('\0');
                if (identity.Length != 2 || !nodes.TryGetValue(identity[0], out AgentPackageNode node))
                    continue;
                bool property = identity[1].StartsWith("property:", StringComparison.Ordinal);
                string portId = identity[1].Substring(identity[1].IndexOf(':') + 1);
                if (!m_Catalog.TryResolveDocumentPort(
                        node.kind,
                        node.properties,
                        portId,
                        property,
                        out GraphAuthoringDynamicPortProjection port,
                        out _))
                    continue;
                if (port.Capacity != GraphAuthoringPortCapacity.Single)
                    continue;
                report.Error(
                    $"{path}.nodes[{identity[0]}]",
                    "port_shape_capacity_exceeded",
                    $"Node '{identity[0]}' 的 port '{portId}' 容量为 Single，但目标 Graph 包含 {pair.Value} 条连接。");
                valid = false;
            }
            return valid;
        }

        static bool ValidateEdgeIdentity(string identity, ISet<string> identities, string path, AgentCompileReport report)
        {
            if (IsIdentity(identity) && identities.Add(identity))
                return true;
            report.Error(path, "edge_identity_invalid", "Edge identity缺失、重复或local语法非法。");
            return false;
        }

        static bool TryEndpoint(
            AgentPackageEdgeEndpoint endpoint,
            IReadOnlyDictionary<string, string> anchors,
            string path,
            AgentCompileReport report,
            out string node)
        {
            node = null;
            if (endpoint == null || string.IsNullOrWhiteSpace(endpoint.node) || string.IsNullOrWhiteSpace(endpoint.port))
            {
                report.Error(path, "edge_endpoint_invalid", "Edge endpoint必须声明node与port。");
                return false;
            }
            if (!endpoint.node.StartsWith("@", StringComparison.Ordinal))
            {
                node = endpoint.node;
                return true;
            }
            if (anchors.TryGetValue(endpoint.node, out node))
                return true;
            node = endpoint.node;
            return true;
        }

        static string ResolveEndpoint(IReadOnlyDictionary<string, string> names, string identity)
        {
            return names.TryGetValue(identity ?? string.Empty, out string value) ? value : identity;
        }

        static AgentSnapshotGraphReference ToGraphReference(AgentPackageGraphReference source, AgentSnapshotNode current)
        {
            AgentSnapshotGraphReference existing = current?.graphReferences?.FirstOrDefault(value => string.Equals(value.key, source.key, StringComparison.Ordinal));
            return new AgentSnapshotGraphReference
            {
                key = source.key,
                label = existing?.label,
                graphAuthoringId = source.graphId,
                graphPath = existing?.graphPath,
                graphKind = existing?.graphKind,
                ownership = source.ownership,
                scopeId = existing?.scopeId,
                sharedAssetPath = source.sharedAssetPath,
                required = existing?.required ?? false,
                inputBindings = AgentAuthoringDocumentCodec.Clone(source.inputBindings) ?? new List<AgentSnapshotGraphParameterBinding>(),
                outputBindings = AgentAuthoringDocumentCodec.Clone(source.outputBindings) ?? new List<AgentSnapshotGraphParameterBinding>()
            };
        }

        static List<AgentSnapshotGraphReference> MergeGraphReferences(
            IReadOnlyList<AgentPackageGraphReference> package,
            AgentSnapshotNode current)
        {
            var pending = (package ?? Array.Empty<AgentPackageGraphReference>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.key))
                .ToDictionary(value => value.key, value => value, StringComparer.Ordinal);
            var result = new List<AgentSnapshotGraphReference>();
            foreach (AgentSnapshotGraphReference existing in current?.graphReferences ?? new List<AgentSnapshotGraphReference>())
            {
                if (pending.TryGetValue(existing.key ?? string.Empty, out AgentPackageGraphReference replacement))
                {
                    result.Add(ToGraphReference(replacement, current));
                    pending.Remove(existing.key);
                }
                else if (string.IsNullOrEmpty(existing.graphAuthoringId))
                {
                    result.Add(existing);
                }
            }
            result.AddRange(pending.Values
                .OrderBy(value => value.key, StringComparer.Ordinal)
                .Select(value => ToGraphReference(value, current)));
            return result;
        }

        static AgentSnapshotAssetReference ToAssetReference(AgentPackageAssetReference source, AgentSnapshotNode current)
        {
            AgentSnapshotAssetReference existing = current?.assetReferences?.FirstOrDefault(value => string.Equals(value.key, source.key, StringComparison.Ordinal));
            return new AgentSnapshotAssetReference
            {
                key = source.key,
                label = existing?.label,
                assetPath = source.assetPath,
                assetGuid = source.assetGuid,
                assetType = existing?.assetType,
                required = existing?.required ?? false
            };
        }

        static List<AgentSnapshotAssetReference> MergeAssetReferences(
            IReadOnlyList<AgentPackageAssetReference> package,
            AgentSnapshotNode current)
        {
            var pending = (package ?? Array.Empty<AgentPackageAssetReference>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.key))
                .ToDictionary(value => value.key, value => value, StringComparer.Ordinal);
            var result = new List<AgentSnapshotAssetReference>();
            foreach (AgentSnapshotAssetReference existing in current?.assetReferences ?? new List<AgentSnapshotAssetReference>())
            {
                if (pending.TryGetValue(existing.key ?? string.Empty, out AgentPackageAssetReference replacement))
                {
                    result.Add(ToAssetReference(replacement, current));
                    pending.Remove(existing.key);
                }
                else if (string.IsNullOrEmpty(existing.assetPath) && string.IsNullOrEmpty(existing.assetGuid))
                {
                    result.Add(existing);
                }
            }
            result.AddRange(pending.Values
                .OrderBy(value => value.key, StringComparer.Ordinal)
                .Select(value => ToAssetReference(value, current)));
            return result;
        }

        static AgentSnapshotExposedProperty ToExposedProperty(
            AgentPackageExposedProperty source,
            AgentSnapshotNode current)
        {
            if (source == null)
                return null;
            AgentSnapshotExposedProperty existing = current?.exposedProperty;
            return new AgentSnapshotExposedProperty
            {
                mode = source.mode,
                declarationAuthoringId = source.declarationId,
                declarationOwnerId = existing?.declarationOwnerId,
                key = existing?.key,
                valueType = source.valueType,
                value = source.value?.DeepClone()
            };
        }

        static Dictionary<string, AgentPackageNodeLayout> BuildGeneratedPositions(
            AgentPackageGraphFile graph,
            IReadOnlyDictionary<string, AgentPackageNodeLayout> explicitPositions,
            IReadOnlyDictionary<string, AgentSnapshotNode> currentNodes)
        {
            var nodes = (graph.nodes ?? new List<AgentPackageNode>())
                .Where(node => node != null && !string.IsNullOrEmpty(node.id))
                .OrderBy(node => node.id, StringComparer.Ordinal)
                .ToList();
            var nodeIds = new HashSet<string>(nodes.Select(node => node.id), StringComparer.Ordinal);
            var indegree = nodes.ToDictionary(node => node.id, _ => 0, StringComparer.Ordinal);
            var outgoing = nodes.ToDictionary(node => node.id, _ => new List<string>(), StringComparer.Ordinal);
            foreach (AgentPackageFlowEdge edge in graph.flowEdges ?? new List<AgentPackageFlowEdge>())
            {
                string from = edge?.from?.node;
                string to = edge?.to?.node;
                if (!nodeIds.Contains(to ?? string.Empty) || !nodeIds.Contains(from ?? string.Empty))
                    continue;
                indegree[to]++;
                outgoing[from].Add(to);
            }

            var layers = nodes.ToDictionary(node => node.id, _ => 0, StringComparer.Ordinal);
            var ready = new SortedSet<string>(
                indegree.Where(pair => pair.Value == 0).Select(pair => pair.Key),
                StringComparer.Ordinal);
            while (ready.Count > 0)
            {
                string current = ready.Min;
                ready.Remove(current);
                foreach (string target in outgoing[current].OrderBy(value => value, StringComparer.Ordinal))
                {
                    layers[target] = Math.Max(layers[target], layers[current] + 1);
                    indegree[target]--;
                    if (indegree[target] == 0)
                        ready.Add(target);
                }
            }

            var result = new Dictionary<string, AgentPackageNodeLayout>(StringComparer.Ordinal);
            bool stateMachine = string.Equals(graph.kind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal);
            float horizontalStep = stateMachine ? 340f : 300f;
            float verticalStep = stateMachine ? 200f : 180f;
            foreach (IGrouping<int, AgentPackageNode> layer in nodes
                         .Where(node =>
                             !explicitPositions.ContainsKey(node.id) &&
                             (!currentNodes.TryGetValue(node.id, out AgentSnapshotNode current) || current.position == null))
                         .GroupBy(node => layers[node.id])
                         .OrderBy(group => group.Key))
            {
                int row = 0;
                foreach (AgentPackageNode node in layer.OrderBy(value => value.id, StringComparer.Ordinal))
                {
                    result[node.id] = new AgentPackageNodeLayout
                    {
                        id = node.id,
                        x = layer.Key * horizontalStep,
                        y = row * verticalStep
                    };
                    row++;
                }
            }
            return result;
        }

        static string RouteOwnership(string ownership)
        {
            if (string.Equals(ownership, TreeGraphReferenceOwnership.Inline.ToString(), StringComparison.Ordinal))
                return AgentGraphOwnership.Inline.ToString();
            if (string.Equals(ownership, TreeGraphReferenceOwnership.Shared.ToString(), StringComparison.Ordinal))
                return AgentGraphOwnership.SharedAsset.ToString();
            return AgentGraphOwnership.Unknown.ToString();
        }

        static string TimelineTreeOwnershipToGraphOwnership(string ownership)
        {
            if (string.Equals(ownership, TimelineTreeOwnership.Inline.ToString(), StringComparison.Ordinal))
                return AgentGraphOwnership.Inline.ToString();
            if (string.Equals(ownership, TimelineTreeOwnership.Shared.ToString(), StringComparison.Ordinal))
                return AgentGraphOwnership.SharedAsset.ToString();
            return AgentGraphOwnership.Unknown.ToString();
        }

        static bool IsChildGraphOwnership(string ownership)
        {
            return string.Equals(ownership, AgentGraphOwnership.Inline.ToString(), StringComparison.Ordinal) ||
                   string.Equals(ownership, AgentGraphOwnership.SharedAsset.ToString(), StringComparison.Ordinal);
        }

        static string OwnerSlot(string graphKind)
        {
            if (string.Equals(graphKind, AgentGraphKind.StateMachineGraph.ToString(), StringComparison.Ordinal))
                return "stateMachine";
            if (string.Equals(graphKind, AgentGraphKind.StateBehaviorSubTree.ToString(), StringComparison.Ordinal))
                return "body";
            if (string.Equals(graphKind, AgentGraphKind.ConditionRuleGraph.ToString(), StringComparison.Ordinal))
                return "condition";
            return "root";
        }
    }
}
