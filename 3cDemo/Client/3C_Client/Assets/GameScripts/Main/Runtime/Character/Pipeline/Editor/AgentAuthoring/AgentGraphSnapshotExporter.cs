using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Diagnostics;
using ThirdPersonCharacter.Pipeline.Animation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphSnapshotExporter
    {
        readonly Dictionary<BaseGraph, string> m_GraphIds = new Dictionary<BaseGraph, string>();
        readonly HashSet<BaseGraph> m_ExportedGraphs = new HashSet<BaseGraph>();
        readonly HashSet<string> m_AssetIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_BlackboardDeclarationIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_TimelineTreeClipPaths = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_TimelineIds = new HashSet<string>(StringComparer.Ordinal);
        readonly Dictionary<string, BaseExposedProperty> m_BlackboardDeclarations = new Dictionary<string, BaseExposedProperty>(StringComparer.Ordinal);
        readonly Dictionary<BaseTree, string> m_ProjectedGraphPaths = new Dictionary<BaseTree, string>();

        public AgentGraphSnapshot Export(CharacterPipelineDefinition definition)
        {
            return Export(definition, AgentSnapshotExportMode.Compact);
        }

        public AgentGraphSnapshot ExportFull(CharacterPipelineDefinition definition)
        {
            return Export(definition, AgentSnapshotExportMode.Full);
        }

        public AgentGraphSnapshot Export(CharacterPipelineDefinition definition, AgentSnapshotExportMode mode)
        {
            m_GraphIds.Clear();
            m_ExportedGraphs.Clear();
            m_AssetIds.Clear();
            m_BlackboardDeclarationIds.Clear();
            m_TimelineTreeClipPaths.Clear();
            m_TimelineIds.Clear();
            m_BlackboardDeclarations.Clear();
            m_ProjectedGraphPaths.Clear();

            AgentGraphSnapshot snapshot = new AgentGraphSnapshot();
            snapshot.exportMode = mode.ToString();
            if (!definition)
                return snapshot;

            snapshot.definitionName = definition.name;
            snapshot.definitionAssetPath = AssetDatabase.GetAssetPath(definition);
            snapshot.rootTreeAssetPath = definition.RootTreeAsset ? AssetDatabase.GetAssetPath(definition.RootTreeAsset) : string.Empty;

            ExportInputs(definition.InputProfile, snapshot);
            ExportActionProfiles(definition.ActionProfiles, snapshot);
            ExportPresentation(definition, snapshot);

            BaseTree root = definition.RootTreeAsset ? definition.RootTreeAsset.Tree : null;
            if (root != null)
            {
                snapshot.rootGraphAuthoringId = GraphId("root", root);
                BTSMTL.Diagnostics.RuntimeProgramRevision revision = CharacterRuntimeDebugProgramBuilder.Build(definition).Revision;
                snapshot.programId = revision.ProgramId;
                snapshot.compilationRevision = revision.CompilationRevision;
                snapshot.sourceContentHash = revision.SourceContentHash;
                var projectionErrors = new List<string>();
                CharacterAuthoringTopologyProjection projection = CharacterAuthoringTopologyProjection.Build(root, projectionErrors);
                if (!projection.IsValid)
                    throw new InvalidOperationException(string.Join("\n", projectionErrors));
                IndexProjectedGraphPaths(projection);
                if (mode == AgentSnapshotExportMode.Full)
                {
                    ExportCompactGraph(root, "root", AgentGraphOwnership.RootAsset, string.Empty, string.Empty, snapshot);
                    ExportProjectedGraphs(projection, AgentSnapshotExportMode.Compact, snapshot);
                    m_ExportedGraphs.Clear();
                    ExportGraph(root, "root", AgentGraphOwnership.RootAsset, string.Empty, string.Empty, string.Empty, snapshot);
                    ExportProjectedGraphs(projection, AgentSnapshotExportMode.Full, snapshot);
                }
                else
                {
                    ExportCompactGraph(root, "root", AgentGraphOwnership.RootAsset, string.Empty, string.Empty, snapshot);
                    ExportProjectedGraphs(projection, AgentSnapshotExportMode.Compact, snapshot);
                }
                AttachAuthoringRoutes(projection, snapshot);
            }

            return snapshot;
        }

        void ExportInputs(CharacterInputProfile inputProfile, AgentGraphSnapshot snapshot)
        {
            if (!inputProfile)
                return;

            IReadOnlyList<CharacterInputValueDefinition> values = inputProfile.InputValues;
            for (int i = 0; i < values.Count; i++)
            {
                CharacterInputValueDefinition value = values[i];
                if (value == null)
                    continue;

                snapshot.inputValues.Add(new AgentSnapshotInputValue
                {
                    inputValueId = value.InputValueId,
                    valueType = value.ValueType.ToString()
                });
            }

            IReadOnlyList<CharacterActionRequestDefinition> requests = inputProfile.ActionRequests;
            for (int i = 0; i < requests.Count; i++)
            {
                CharacterActionRequestDefinition request = requests[i];
                if (request == null)
                    continue;

                snapshot.actionRequests.Add(new AgentSnapshotActionRequest
                {
                    requestId = request.RequestId,
                    bufferSeconds = request.BufferSeconds,
                    priority = request.Priority
                });
            }
        }

        void ExportActionProfiles(IReadOnlyList<ActionProfile> profiles, AgentGraphSnapshot snapshot)
        {
            if (profiles == null)
                return;

            for (int i = 0; i < profiles.Count; i++)
            {
                ActionProfile profile = profiles[i];
                if (!profile)
                    continue;

                string path = AssetDatabase.GetAssetPath(profile);
                snapshot.actionProfiles.Add(new AgentSnapshotActionProfile
                {
                    actionId = profile.ActionId,
                    displayName = profile.DisplayName,
                    assetPath = path,
                    assetGuid = AssetDatabase.AssetPathToGUID(path)
                });
            }
        }

        void ExportPresentation(CharacterPipelineDefinition definition, AgentGraphSnapshot snapshot)
        {
            snapshot.presentation.definitionAssetPath = AssetDatabase.GetAssetPath(definition);
            CharacterAnimationPresentationDefinition presentation = definition.AnimationPresentation;
            IReadOnlyList<CharacterAnimationLayerDefinition> layers = presentation?.Layers;
            if (layers == null)
                return;
            string libraryPath = presentation.TransitionLibrary
                ? AssetDatabase.GetAssetPath(presentation.TransitionLibrary)
                : string.Empty;
            snapshot.presentation.transitionLibraryAssetPath = libraryPath;
            snapshot.presentation.transitionLibraryAssetGuid = string.IsNullOrEmpty(libraryPath)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(libraryPath);
            for (int i = 0; i < layers.Count; i++)
            {
                CharacterAnimationLayerDefinition layer = layers[i];
                if (layer == null)
                    continue;
                string maskPath = layer.AvatarMask ? AssetDatabase.GetAssetPath(layer.AvatarMask) : string.Empty;
                snapshot.presentation.layers.Add(new AgentSnapshotAnimationLayer
                {
                    layerId = layer.Id,
                    order = i,
                    animancerLayerIndex = layer.AnimancerLayerIndex,
                    avatarMaskAssetPath = maskPath,
                    avatarMaskAssetGuid = string.IsNullOrEmpty(maskPath) ? string.Empty : AssetDatabase.AssetPathToGUID(maskPath),
                    blendMode = layer.BlendMode.ToString(),
                    outputPolicy = layer.OutputPolicy.ToString()
                });
            }

            var projectionErrors = new List<string>();
            AnimationPresentationProjection projection = AnimationPresentationProjection.Build(definition.RootTree, projectionErrors);
            if (!projection.IsValid)
                throw new InvalidOperationException(string.Join("\n", projectionErrors));
            for (int producerIndex = 0; producerIndex < projection.Producers.Count; producerIndex++)
            {
                AnimationPresentationProducerEntry producer = projection.Producers[producerIndex];
                AnimationProducerPresentationBinding binding = presentation.FindProducerBinding(producer.ProducerId);
                string transitionPath = binding?.Transition
                    ? AssetDatabase.GetAssetPath(binding.Transition)
                    : string.Empty;
                snapshot.presentation.producers.Add(new AgentSnapshotAnimationProducer
                {
                    route = ExportRoute(producer.Route),
                    timelineAuthoringId = producer.ProducerId.TimelineAuthoringId,
                    trackAuthoringId = producer.ProducerId.TrackAuthoringId,
                    timelineName = producer.Timeline.Name,
                    trackName = producer.Track.Name,
                    layerId = producer.LayerId,
                    transitionAssetPath = transitionPath,
                    transitionAssetGuid = string.IsNullOrEmpty(transitionPath)
                        ? string.Empty
                        : AssetDatabase.AssetPathToGUID(transitionPath),
                    easing = binding?.Easing.ToString() ?? string.Empty
                });
            }
        }

        void ExportProjectedGraphs(
            CharacterAuthoringTopologyProjection projection,
            AgentSnapshotExportMode mode,
            AgentGraphSnapshot snapshot)
        {
            if (projection == null)
                return;

            for (int i = 0; i < projection.Graphs.Count; i++)
            {
                CharacterAuthoringGraphEntry entry = projection.Graphs[i];
                BaseTree tree = entry.Graph;
                if (m_ExportedGraphs.Contains(tree))
                    continue;

                TreeAuthoringRouteId route = entry.Route;
                TreeAuthoringRouteSegment segment = route.Count > 0 ? route[route.Count - 1] : default;
                AgentGraphOwnership ownership = route.Count == 0
                    ? AgentGraphOwnership.RootAsset
                    : segment.Ownership == TreeGraphReferenceOwnership.Shared
                        ? AgentGraphOwnership.SharedAsset
                        : AgentGraphOwnership.Inline;
                if (!m_ProjectedGraphPaths.TryGetValue(tree, out string path))
                    throw new InvalidOperationException($"Projected Graph '{tree.name}' has no indexed path.");
                string ownerElementId = route.Count > 0 ? segment.OwnerElement.ElementAuthoringId : string.Empty;
                string referenceKey = route.Count > 0 ? segment.ReferenceKey : string.Empty;
                if (mode == AgentSnapshotExportMode.Full)
                {
                    string sharedPath = ownership == AgentGraphOwnership.SharedAsset
                        ? AssetDatabase.GetAssetPath(tree.SerializedOwner)
                        : string.Empty;
                    ExportGraph(tree, path, ownership, ownerElementId, referenceKey, sharedPath, snapshot);
                }
                else
                {
                    ExportCompactGraph(tree, path, ownership, ownerElementId, referenceKey, snapshot);
                }
            }
        }

        void IndexProjectedGraphPaths(CharacterAuthoringTopologyProjection projection)
        {
            for (int i = 0; i < projection.Graphs.Count; i++)
            {
                CharacterAuthoringGraphEntry entry = projection.Graphs[i];
                if (m_ProjectedGraphPaths.ContainsKey(entry.Graph))
                    continue;
                m_ProjectedGraphPaths.Add(
                    entry.Graph,
                    entry.Route.Count == 0 ? "root" : entry.Route.ToString());
            }
        }

        static void AttachAuthoringRoutes(CharacterAuthoringTopologyProjection projection, AgentGraphSnapshot snapshot)
        {
            if (projection == null)
                return;

            for (int routeIndex = 0; routeIndex < projection.Graphs.Count; routeIndex++)
            {
                CharacterAuthoringGraphEntry entry = projection.Graphs[routeIndex];
                string graphId = entry.Graph?.GraphAuthoringId ?? string.Empty;
                if (string.IsNullOrEmpty(graphId))
                    continue;

                for (int i = 0; i < snapshot.graphSummaries.Count; i++)
                {
                    if (string.Equals(snapshot.graphSummaries[i].graphAuthoringId, graphId, StringComparison.Ordinal))
                        snapshot.graphSummaries[i].routes.Add(ExportRoute(entry.Route));
                }
                for (int i = 0; i < snapshot.stateMachines.Count; i++)
                {
                    if (string.Equals(snapshot.stateMachines[i].graphAuthoringId, graphId, StringComparison.Ordinal))
                        snapshot.stateMachines[i].routes.Add(ExportRoute(entry.Route));
                }
                for (int i = 0; i < snapshot.graphs.Count; i++)
                {
                    if (string.Equals(snapshot.graphs[i].graphAuthoringId, graphId, StringComparison.Ordinal))
                        snapshot.graphs[i].routes.Add(ExportRoute(entry.Route));
                }
            }
        }

        static AgentSnapshotAuthoringRoute ExportRoute(TreeAuthoringRouteId route)
        {
            var result = new AgentSnapshotAuthoringRoute
            {
                rootGraphAuthoringId = route?.RootGraphAuthoringId ?? string.Empty
            };
            if (route == null)
                return result;

            for (int i = 0; i < route.Count; i++)
            {
                TreeAuthoringRouteSegment segment = route[i];
                result.segments.Add(new AgentSnapshotAuthoringRouteSegment
                {
                    kind = segment.Kind.ToString(),
                    ownerElementKind = segment.OwnerElement.Kind.ToString(),
                    ownerGraphAuthoringId = segment.OwnerElement.GraphAuthoringId,
                    ownerElementAuthoringId = segment.OwnerElement.ElementAuthoringId,
                    referenceKey = segment.ReferenceKey,
                    scopeId = segment.ScopeId,
                    childGraphAuthoringId = segment.ChildGraphAuthoringId,
                    ownership = segment.Ownership.ToString(),
                    timelineAuthoringId = segment.TimelineAuthoringId,
                    trackAuthoringId = segment.TrackAuthoringId,
                    clipAuthoringId = segment.ClipAuthoringId
                });
            }
            return result;
        }

        void ExportCompactGraph(
            BaseTree tree,
            string path,
            AgentGraphOwnership ownership,
            string ownerNodeName,
            string referenceKey,
            AgentGraphSnapshot snapshot)
        {
            if (tree == null || m_ExportedGraphs.Contains(tree))
                return;

            tree.CheckInit();
            string graphId = GraphId(path, tree);
            m_ExportedGraphs.Add(tree);
            snapshot.graphSummaries.Add(new AgentSnapshotGraphSummary
            {
                graphAuthoringId = graphId,
                path = path,
                name = tree.name,
                kind = ResolveGraphKind(tree).ToString(),
                ownership = ownership.ToString(),
                ownerNode = ownerNodeName,
                referenceKey = referenceKey
            });
            ExportBlackboardDeclarations(tree, path, snapshot);

            if (tree is StateMachineGraph stateMachineGraph)
                snapshot.stateMachines.Add(ExportStateMachineSummary(stateMachineGraph, graphId, path, ownerNodeName, snapshot));

        }

        AgentSnapshotStateMachineSummary ExportStateMachineSummary(
            StateMachineGraph graph,
            string graphId,
            string graphPath,
            string ownerNodeName,
            AgentGraphSnapshot snapshot)
        {
            AgentSnapshotStateMachineSummary summary = new AgentSnapshotStateMachineSummary
            {
                graphAuthoringId = graphId,
                graphPath = graphPath,
                name = graph.name,
                ownerNode = ownerNodeName
            };

            foreach (StateNode state in graph.StateNodes)
            {
                if (state == null)
                    continue;

                AgentSnapshotStateSummary stateSummary = new AgentSnapshotStateSummary
                {
                    stateAuthoringId = state.GUID,
                    state = state.ResolvedDisplayName,
                    behaviorGraphAuthoringId = state.SubTree?.GraphAuthoringId ?? string.Empty,
                    behaviorGraphPath = FindGraphPath(state.SubTree)
                };

                ExportStateBehaviorFacts(state.SubTree as StateBehaviorSubTree, stateSummary, snapshot);
                summary.states.Add(stateSummary);
            }

            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge == null || !graph.IsTransitionEdge(edge))
                    continue;

                summary.transitions.Add(ExportTransitionSummary(graph, edge));
            }

            return summary;
        }

        void ExportStateBehaviorFacts(StateBehaviorSubTree tree, AgentSnapshotStateSummary stateSummary, AgentGraphSnapshot snapshot)
        {
            if (tree == null)
                return;

            for (int i = 0; i < tree.Nodes.Count; i++)
            {
                BaseNode node = tree.Nodes[i];
                if (node == null)
                    continue;

                if (node is TimelineNode timelineNode)
                {
                    TimelineData timeline = timelineNode.Timeline;
                    RegisterKnownAsset(timelineNode.SharedTimelineAsset, snapshot);
                    RegisterKnownAsset(timelineNode.ActionContext, snapshot);
                    string timelinePath = timelineNode.SharedTimelineAsset
                        ? AssetDatabase.GetAssetPath(timelineNode.SharedTimelineAsset)
                        : string.Empty;
                    string timelineNodePath = $"{stateSummary.behaviorGraphPath}/node:{node.GUID}/timeline";
                    ExportTimelineTreeClips(timeline, timelineNodePath, timelineNode.TimelineOwnership, timelinePath, snapshot);
                    ExportTimeline(timeline, snapshot);
                    stateSummary.timelines.Add(new AgentSnapshotTimelineBindingSummary
                    {
                        nodeAuthoringId = node.GUID,
                        timelineAuthoringId = timeline?.AuthoringId ?? string.Empty,
                        displayName = node.ResolvedDisplayName,
                        timeline = timeline?.Name ?? string.Empty,
                        ownership = timelineNode.TimelineOwnership.ToString(),
                        graphPath = timelineNodePath,
                        timelineAssetPath = timelinePath,
                        timelineAssetGuid = string.IsNullOrEmpty(timelinePath) ? string.Empty : AssetDatabase.AssetPathToGUID(timelinePath),
                        actionContext = AssetName(timelineNode.ActionContext),
                        playbackMode = timelineNode.PlaybackMode.ToString(),
                        trackCount = timeline?.Tracks.Count ?? 0,
                        clipCount = timeline?.Tracks.Sum(track => track.Clips.Count) ?? 0
                    });
                    continue;
                }

                if (node is StateMachineNode stateMachineNode && stateMachineNode.Graph != null)
                {
                    string graphPath = FindGraphPath(stateMachineNode.Graph);
                    stateSummary.nestedStateMachines.Add(new AgentSnapshotNestedStateMachineSummary
                    {
                        nodeAuthoringId = node.GUID,
                        node = node.ResolvedDisplayName,
                        graphAuthoringId = GraphId(graphPath, stateMachineNode.Graph),
                        graphPath = graphPath,
                        ownership = stateMachineNode.GetModule<ScopedGraphReferenceModule>()?.SharedGraphAsset
                            ? AgentGraphOwnership.SharedAsset.ToString()
                            : AgentGraphOwnership.Inline.ToString()
                    });
                    continue;
                }

                if (node is ActivateActionInstanceNode activationNode)
                {
                    RegisterKnownAsset(activationNode.ActionContext, snapshot);
                    stateSummary.actionActivations.Add(new AgentSnapshotActionActivationSummary
                    {
                        displayName = node.ResolvedDisplayName,
                        actionProfile = activationNode.ActionProfile ? activationNode.ActionProfile.ActionId : string.Empty,
                        sourceRequest = activationNode.SourceInputRequestId,
                        actionContext = AssetName(activationNode.ActionContext),
                        targetKey = activationNode.TargetKey,
                        targetSnapshotBlackboardKey = activationNode.TargetSnapshotVariable.DisplayKey
                    });
                    continue;
                }

                if (node is SubmitActionLifecycleTransitionNode lifecycleNode)
                {
                    RegisterKnownAsset(lifecycleNode.ActionContext, snapshot);
                    stateSummary.lifecycleTransitions.Add(new AgentSnapshotLifecycleSummary
                    {
                        displayName = node.ResolvedDisplayName,
                        transitionType = lifecycleNode.TransitionType.ToString(),
                        reason = lifecycleNode.Reason,
                        actionContext = AssetName(lifecycleNode.ActionContext)
                    });
                }
            }
        }

        void ExportBlackboardDeclarations(BaseTree tree, string graphPath, AgentGraphSnapshot snapshot)
        {
            for (int i = 0; i < tree.ExposedProperties.Count; i++)
            {
                BaseExposedProperty declaration = tree.ExposedProperties[i];
                if (declaration == null || !m_BlackboardDeclarationIds.Add(declaration.DeclarationId))
                    continue;

                m_BlackboardDeclarations[declaration.DeclarationId] = declaration;

                snapshot.blackboardDeclarations.Add(new AgentSnapshotBlackboardDeclaration
                {
                    declarationId = declaration.DeclarationId,
                    ownerId = tree.GraphAuthoringId,
                    graphPath = graphPath,
                    key = declaration.BlackboardKey,
                    valueType = declaration.ValueType?.FullName ?? string.Empty,
                    scope = declaration.BlackboardScope.ToString(),
                    lifetime = declaration.BlackboardLifetime.ToString(),
                    authority = declaration.BlackboardAuthority.ToString(),
                    syncPolicy = declaration.BlackboardSyncPolicy.ToString(),
                    factProjection = declaration.BlackboardFactProjection.ToString(),
                    windowType = declaration.ActionWindowType,
                    windowId = declaration.ActionWindowId,
                    digest = declaration.ActionWindowDigest,
                    categoryPath = declaration.BlackboardCategoryPath
                });
            }
        }

        void ExportTimelineTreeClips(
            TimelineData timeline,
            string timelineNodePath,
            TimelineOwnership timelineOwnership,
            string timelineAssetPath,
            AgentGraphSnapshot snapshot)
        {
            if (timeline == null || !m_TimelineTreeClipPaths.Add(timelineNodePath))
                return;

            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (!(timeline.Tracks[trackIndex] is TreeTrack treeTrack))
                    continue;

                for (int clipIndex = 0; clipIndex < treeTrack.Clips.Count; clipIndex++)
                {
                    if (!(treeTrack.Clips[clipIndex] is TreeClip treeClip))
                        continue;

                    TimelineRunningTree tree = treeClip.ResolvedTree;
                    var summary = new AgentSnapshotTimelineTreeClip
                    {
                        timelineAuthoringId = timeline.AuthoringId,
                        trackAuthoringId = treeTrack.AuthoringId,
                        clipAuthoringId = treeClip.AuthoringId,
                        timeline = timeline.Name,
                        timelineNodePath = timelineNodePath,
                        timelineOwnership = timelineOwnership.ToString(),
                        timelineAssetPath = timelineOwnership == TimelineOwnership.Shared ? timelineAssetPath : string.Empty,
                        trackIndex = trackIndex,
                        clipIndex = clipIndex,
                        startFrame = treeClip.StartFrame,
                        endFrame = treeClip.EndFrame,
                        phase = treeClip.ExecutionPhase.ToString(),
                        ownership = treeClip.Ownership.ToString(),
                        treeName = tree ? tree.name : string.Empty
                    };
                    if (tree != null)
                    {
                        summary.blackboardOutputs.AddRange(tree.Nodes
                            .OfType<ExposedPropertyNode>()
                            .Where(i => i.NodeType == ExposedPropertyNodeType.Set && i.BlackboardVariable.IsValid)
                            .Select(i => i.BlackboardVariable.DisplayKey)
                            .Distinct());
                        summary.projectedFacts.AddRange(tree.Nodes
                            .OfType<ExposedPropertyNode>()
                            .Where(i => i.NodeType == ExposedPropertyNodeType.Set &&
                                        i.BlackboardVariable.IsValid &&
                                        m_BlackboardDeclarations.TryGetValue(i.BlackboardVariable.DeclarationId, out BaseExposedProperty declaration) &&
                                        declaration.BlackboardFactProjection == PipelineBlackboardFactProjectionKind.ActionWindow)
                            .Select(i =>
                            {
                                BaseExposedProperty declaration = m_BlackboardDeclarations[i.BlackboardVariable.DeclarationId];
                                return $"{i.BlackboardVariable.DisplayKey} -> ActionWindow({declaration.ActionWindowType}/{declaration.ActionWindowId}/{declaration.ActionWindowDigest})";
                            })
                            .Distinct());
                    }
                    snapshot.timelineTreeClips.Add(summary);
                }
            }
        }

        void ExportTimeline(TimelineData timeline, AgentGraphSnapshot snapshot)
        {
            if (timeline == null || !m_TimelineIds.Add(timeline.AuthoringId))
                return;

            var timelineSnapshot = new AgentSnapshotTimeline
            {
                timelineAuthoringId = timeline.AuthoringId,
                name = timeline.Name
            };
            snapshot.timelines.Add(timelineSnapshot);

            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;

                var trackSnapshot = new AgentSnapshotTimelineTrack
                {
                    trackAuthoringId = track.AuthoringId,
                    typeName = track.GetType().FullName,
                    name = track.Name,
                    index = trackIndex,
                    layerId = track is AnimationTrack animationTrack ? animationTrack.LayerId : string.Empty
                };
                timelineSnapshot.tracks.Add(trackSnapshot);

                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null)
                        continue;

                    string animationClipPath = clip is BTSMTL.Timeline.AnimationClip animationClip && animationClip.Clip
                        ? AssetDatabase.GetAssetPath(animationClip.Clip)
                        : string.Empty;
                    trackSnapshot.clips.Add(new AgentSnapshotTimelineClip
                    {
                        clipAuthoringId = clip.AuthoringId,
                        typeName = clip.GetType().FullName,
                        index = clipIndex,
                        startFrame = clip.StartFrame,
                        endFrame = clip.EndFrame,
                        animationClipAssetPath = animationClipPath,
                        animationClipAssetGuid = string.IsNullOrEmpty(animationClipPath)
                            ? string.Empty
                            : AssetDatabase.AssetPathToGUID(animationClipPath)
                    });
                }
            }
        }

        AgentSnapshotTransitionSummary ExportTransitionSummary(StateMachineGraph graph, BaseEdge edge)
        {
            AgentSnapshotTransitionSummary summary = new AgentSnapshotTransitionSummary
            {
                edgeAuthoringId = edge.GUID,
                fromElementAuthoringId = edge.StartNodeGUID,
                toElementAuthoringId = edge.EndNodeGUID,
                from = NodeLabel(ResolveNode(graph, edge.StartNode, edge.StartNodeGUID)),
                to = NodeLabel(ResolveNode(graph, edge.EndNode, edge.EndNodeGUID)),
                priority = edge.TransitionPriority
            };

            ConditionRuleGraph ruleGraph = edge.ConditionRuleGraph;
            if (ruleGraph == null)
                return summary;

            for (int i = 0; i < ruleGraph.Nodes.Count; i++)
            {
                BaseNode node = ruleGraph.Nodes[i];
                if (node == null || node is ConditionRuleResultNode)
                    continue;

                if (node is CharacterActionRequestInfoNode requestNode)
                {
                    AddUnique(summary.requests, requestNode.RequestId);
                    continue;
                }

                if (node is StateRootCompletedNode)
                {
                    AddUnique(summary.conditions, "StateRootCompleted");
                    continue;
                }

                AddUnique(summary.conditions, node.ResolvedDisplayName);
            }

            return summary;
        }

        void ExportGraph(
            BaseTree tree,
            string path,
            AgentGraphOwnership ownership,
            string ownerNodeId,
            string referenceKey,
            string sharedAssetPath,
            AgentGraphSnapshot snapshot)
        {
            if (tree == null || m_ExportedGraphs.Contains(tree))
                return;

            tree.CheckInit();
            string graphId = GraphId(path, tree);
            m_ExportedGraphs.Add(tree);
            AgentSnapshotGraph graph = new AgentSnapshotGraph
            {
                graphAuthoringId = graphId,
                path = path,
                name = tree.name,
                kind = ResolveGraphKind(tree).ToString(),
                ownership = ownership.ToString(),
                ownerElementAuthoringId = ownerNodeId,
                referenceKey = referenceKey,
                sharedAssetPath = sharedAssetPath
            };
            snapshot.graphs.Add(graph);
            ExportBlackboardDeclarations(tree, path, snapshot);

            Dictionary<string, string> nodeIds = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 0; i < tree.Nodes.Count; i++)
            {
                BaseNode node = tree.Nodes[i];
                if (node == null)
                    continue;

                string nodeId = NodeId(path, node);
                nodeIds[node.GUID] = nodeId;
                graph.nodes.Add(ExportNode(node, nodeId, snapshot));
            }

            for (int i = 0; i < tree.Edges.Count; i++)
            {
                BaseEdge edge = tree.Edges[i];
                if (edge == null)
                    continue;

                graph.flowEdges.Add(new AgentSnapshotFlowEdge
                {
                    elementAuthoringId = EdgeId(path, edge),
                    startElementAuthoringId = ResolveNodeId(nodeIds, edge.StartNodeGUID),
                    endElementAuthoringId = ResolveNodeId(nodeIds, edge.EndNodeGUID),
                    startPort = edge.StartPortName,
                    endPort = edge.EndPortName,
                    flowOrder = edge.FlowOrder,
                    transitionPriority = edge.TransitionPriority,
                    conditionRuleGraphAuthoringId = edge.ConditionRuleGraph ? GraphId(FindGraphPath(edge.ConditionRuleGraph), edge.ConditionRuleGraph) : string.Empty,
                    conditionRuleGraphPath = FindGraphPath(edge.ConditionRuleGraph)
                });

            }

            for (int i = 0; i < tree.PropertyEdges.Count; i++)
            {
                PropertyEdge edge = tree.PropertyEdges[i];
                if (edge == null)
                    continue;

                graph.propertyEdges.Add(new AgentSnapshotPropertyEdge
                {
                    elementAuthoringId = EdgeId(path, edge),
                    startElementAuthoringId = ResolveNodeId(nodeIds, edge.StartNodeGUID),
                    endElementAuthoringId = ResolveNodeId(nodeIds, edge.EndNodeGUID),
                    startPortId = edge.StartPortName,
                    endPortId = edge.EndPortName
                });
            }
        }

        AgentSnapshotNode ExportNode(BaseNode node, string nodeId, AgentGraphSnapshot snapshot)
        {
            AgentSnapshotNode result = new AgentSnapshotNode
            {
                elementAuthoringId = nodeId,
                typeName = node.GetType().FullName,
                displayName = node.ResolvedDisplayName,
                nodeTypeDisplayName = node.NodeTypeDisplayName,
                position = node.Position
            };

            foreach (KeyValuePair<string, PropertyPort> pair in node.PropertyPortMap)
            {
                PropertyPort port = pair.Value;
                if (port == null)
                    continue;

                result.propertyPorts.Add(new AgentSnapshotPropertyPort
                {
                    portId = port.PortId,
                    displayName = port.DisplayName,
                    direction = port.Direction.ToString(),
                    valueType = port.ValueType != null ? port.ValueType.FullName : string.Empty
                });
            }

            foreach (NodeAssetReference reference in node.GetAssetReferences())
            {
                AgentSnapshotAssetReference assetReference = ExportAssetReference(reference);
                result.assetReferences.Add(assetReference);
                RegisterKnownAsset(assetReference, snapshot);
            }

            foreach (NodeGraphReference reference in node.GetGraphReferences())
            {
                BaseTree referenceTree = reference.Tree;
                string childPath = FindGraphPath(referenceTree);
                string childGraphId = referenceTree ? GraphId(childPath, referenceTree) : string.Empty;
                result.graphReferences.Add(new AgentSnapshotGraphReference
                {
                    key = reference.Key,
                    label = reference.Label,
                    graphAuthoringId = childGraphId,
                    graphPath = referenceTree ? childPath : string.Empty,
                    graphKind = referenceTree ? ResolveGraphKind(referenceTree).ToString() : AgentGraphKind.Unknown.ToString(),
                    ownership = reference.SharedAsset ? AgentGraphOwnership.SharedAsset.ToString() : (reference.Inline ? AgentGraphOwnership.Inline.ToString() : AgentGraphOwnership.Unknown.ToString()),
                    scopeId = reference.ScopeId,
                    sharedAssetPath = AssetPath(reference.SharedAsset),
                    required = reference.Required
                });

            }

            return result;
        }

        AgentSnapshotAssetReference ExportAssetReference(NodeAssetReference reference)
        {
            string path = reference.Asset ? AssetDatabase.GetAssetPath(reference.Asset) : string.Empty;
            return new AgentSnapshotAssetReference
            {
                key = reference.Key,
                label = reference.Label,
                assetPath = path,
                assetGuid = string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path),
                assetType = reference.Asset ? reference.Asset.GetType().FullName : string.Empty,
                required = reference.Required
            };
        }

        void RegisterKnownAsset(AgentSnapshotAssetReference reference, AgentGraphSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(reference.assetPath))
                return;

            string id = string.IsNullOrEmpty(reference.assetGuid) ? reference.assetPath : reference.assetGuid;
            if (!m_AssetIds.Add($"{reference.assetType}:{id}"))
                return;

            AgentSnapshotAsset asset = new AgentSnapshotAsset
            {
                id = id,
                name = System.IO.Path.GetFileNameWithoutExtension(reference.assetPath),
                assetPath = reference.assetPath,
                assetGuid = reference.assetGuid,
                assetType = reference.assetType
            };

            if (reference.assetType == typeof(TimelineAsset).FullName)
                snapshot.timelineAssets.Add(asset);
            else if (reference.assetType == typeof(ActionContextSlot).FullName)
                snapshot.actionContextAssets.Add(asset);
        }

        void RegisterKnownAsset(UnityEngine.Object asset, AgentGraphSnapshot snapshot)
        {
            if (!asset)
                return;

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path))
                return;

            RegisterKnownAsset(new AgentSnapshotAssetReference
            {
                key = asset.name,
                label = asset.name,
                assetPath = path,
                assetGuid = AssetDatabase.AssetPathToGUID(path),
                assetType = asset.GetType().FullName,
                required = false
            }, snapshot);
        }

        string FindGraphPath(BaseTree graph)
        {
            if (graph == null)
                return string.Empty;
            if (m_ProjectedGraphPaths.TryGetValue(graph, out string path))
                return path;
            throw new InvalidOperationException($"Graph '{graph.name}' is absent from the authoring topology projection.");
        }

        static string NodeLabel(BaseNode node)
        {
            if (node is StateMachineEnterNode)
                return "Enter";
            if (node is StateMachineAnyStateNode)
                return "AnyState";
            if (node is StateMachineExitNode)
                return "Exit";
            return node != null ? node.ResolvedDisplayName : string.Empty;
        }

        static BaseNode ResolveNode(BaseTree graph, BaseNode cachedNode, string guid)
        {
            if (cachedNode != null)
                return cachedNode;
            if (graph == null || string.IsNullOrEmpty(guid))
                return null;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                BaseNode node = graph.Nodes[i];
                if (node != null && node.GUID == guid)
                    return node;
            }
            return null;
        }

        static string AssetName(UnityEngine.Object asset)
        {
            return asset ? asset.name : string.Empty;
        }

        static void AddUnique(List<string> values, string value)
        {
            if (string.IsNullOrEmpty(value) || values.Contains(value))
                return;

            values.Add(value);
        }

        string GraphId(string path, BaseGraph graph)
        {
            if (!m_GraphIds.TryGetValue(graph, out string id))
            {
                id = graph?.GraphAuthoringId ?? string.Empty;
                m_GraphIds.Add(graph, id);
            }
            return id;
        }

        static AgentGraphKind ResolveGraphKind(BaseGraph graph)
        {
            if (graph is ConditionRuleGraph)
                return AgentGraphKind.ConditionRuleGraph;
            if (graph is StateMachineGraph)
                return AgentGraphKind.StateMachineGraph;
            if (graph is StateBehaviorSubTree)
                return AgentGraphKind.StateBehaviorSubTree;
            if (graph is SubTree)
                return AgentGraphKind.SubTree;
            if (graph is RunnableTree)
                return AgentGraphKind.RunnableTree;
            if (graph is BaseTree)
                return AgentGraphKind.BaseTree;
            return AgentGraphKind.Unknown;
        }

        static string NodeId(string graphPath, BaseNode node)
        {
            return node?.GUID ?? string.Empty;
        }

        static string EdgeId(string graphPath, BaseEdge edge)
        {
            return edge?.GUID ?? string.Empty;
        }

        static string ResolveNodeId(Dictionary<string, string> nodeIds, string guid)
        {
            return !string.IsNullOrEmpty(guid) && nodeIds.TryGetValue(guid, out string id) ? id : string.Empty;
        }

        static string AssetPath(BaseTreeAsset asset)
        {
            return asset ? AssetDatabase.GetAssetPath(asset) : string.Empty;
        }
    }
}
