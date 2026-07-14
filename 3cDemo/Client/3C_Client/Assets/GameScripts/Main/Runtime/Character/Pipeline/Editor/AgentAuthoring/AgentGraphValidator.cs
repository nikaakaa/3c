using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Network;
using TreeDesigner;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphValidator
    {
        CharacterPipelineDefinition m_Definition;
        AgentCompileReport m_Report;
        readonly Dictionary<string, BaseExposedProperty> m_Declarations = new Dictionary<string, BaseExposedProperty>(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_DeclarationOwnerIds = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly HashSet<TimelineData> m_ValidatedTimelines = new HashSet<TimelineData>();
        readonly HashSet<string> m_GraphAuthoringIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_TimelineAuthoringIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_TrackAuthoringIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_ClipAuthoringIds = new HashSet<string>(StringComparer.Ordinal);
        BaseTree m_RootTree;

        public AgentCompileReport Validate(CharacterPipelineDefinition definition)
        {
            m_Definition = definition;
            m_Report = new AgentCompileReport { success = true };
            m_Declarations.Clear();
            m_DeclarationOwnerIds.Clear();
            m_ValidatedTimelines.Clear();
            m_GraphAuthoringIds.Clear();
            m_TimelineAuthoringIds.Clear();
            m_TrackAuthoringIds.Clear();
            m_ClipAuthoringIds.Clear();

            if (!definition)
            {
                m_Report.Error("definition", "missing_definition", "CharacterPipelineDefinition 缺失。");
                return m_Report;
            }
            if (!definition.RootTreeAsset)
            {
                m_Report.Error("definition.rootTree", "missing_root_tree", "RootTreeAsset 缺失。");
                return m_Report;
            }

            m_RootTree = definition.RootTreeAsset.Tree;
            var topologyErrors = new List<string>();
            CharacterAuthoringTopologyProjection topology =
                CharacterAuthoringTopologyProjection.Build(m_RootTree, topologyErrors);
            for (int i = 0; i < topologyErrors.Count; i++)
                m_Report.Error("root", "authoring_topology_invalid", topologyErrors[i]);

            if (topology.IsValid)
            {
                for (int i = 0; i < topology.Graphs.Count; i++)
                {
                    CharacterAuthoringGraphEntry entry = topology.Graphs[i];
                    if (!entry.FirstOccurrence)
                        continue;
                    string path = entry.Route.Count == 0 ? "root" : entry.Route.ToString();
                    ValidateGraph(entry.Graph, path, entry.VisibleGraphs);
                }
                ValidateCorinAttackHierarchy();
            }
            m_Report.metrics.semanticValidCount = m_Report.HasErrors() ? 0 : 1;
            m_Report.metrics.semanticInvalidCount = m_Report.HasErrors() ? 1 : 0;
            m_Report.success = !m_Report.HasErrors();
            return m_Report;
        }

        void ValidateGraph(BaseTree graph, string path, IReadOnlyList<BaseGraph> visibleGraphs)
        {
            if (graph == null)
                return;

            graph.CheckInit();
            ValidateElementAuthoringIdentities(graph, path);
            ValidateBlackboardDeclarations(graph, path);
            ValidateGraphTypeRules(graph, path);
            ValidateGraphOwnership(graph, path);
            ValidateActionContextChain(graph, path);
            ValidateActionOverrideBody(graph, path);

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                BaseNode node = graph.Nodes[i];
                if (node == null)
                    continue;

                ValidateNode(graph, node, $"{path}/node:{node.GUID}", visibleGraphs);
            }

            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge == null)
                    continue;

                ValidateEdge(graph, edge, $"{path}/edge:{edge.GUID}");
            }
        }

        void ValidateBlackboardDeclarations(BaseTree graph, string path)
        {
            if (!AuthoringIdentity.IsValid(graph.GraphAuthoringId))
                m_Report.Error(path, "graph_authoring_identity_invalid", $"Graph {graph.name} 的 GraphAuthoringId 无效。");
            else if (!m_GraphAuthoringIds.Add(graph.GraphAuthoringId))
                m_Report.Error(path, "graph_authoring_identity_duplicate", $"重复 GraphAuthoringId：{graph.GraphAuthoringId}");

            HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < graph.ExposedProperties.Count; i++)
            {
                BaseExposedProperty declaration = graph.ExposedProperties[i];
                string declarationPath = $"{path}/blackboard:{declaration?.DeclarationId}";
                if (declaration == null || string.IsNullOrEmpty(declaration.DeclarationId) || string.IsNullOrEmpty(declaration.BlackboardKey))
                {
                    m_Report.Error(declarationPath, "blackboard_declaration_invalid", "Pipeline Blackboard declaration 缺少 identity 或 key。");
                    continue;
                }

                if (!keys.Add(declaration.BlackboardKey))
                    m_Report.Error(declarationPath, "blackboard_key_duplicate", $"同一 declaration owner 内存在重复 key：{declaration.BlackboardKey}");

                if (m_Declarations.ContainsKey(declaration.DeclarationId))
                    m_Report.Error(declarationPath, "blackboard_declaration_duplicate", $"重复 declaration identity：{declaration.DeclarationId}");
                else
                {
                    m_Declarations.Add(declaration.DeclarationId, declaration);
                    m_DeclarationOwnerIds.Add(declaration.DeclarationId, graph.GraphAuthoringId);
                }

                if (!PipelineBlackboardVariablePolicy.IsValid(declaration.BlackboardScope, declaration.BlackboardLifetime))
                    m_Report.Error(declarationPath, "blackboard_scope_lifetime_invalid", $"非法 scope/lifetime：{declaration.BlackboardScope}/{declaration.BlackboardLifetime}");
                if (!PipelineBlackboardFactProjectionPolicy.TryValidate(declaration, out string projectionError))
                    m_Report.Error(declarationPath, "blackboard_projection_invalid", projectionError);

                bool ownerValid = declaration.BlackboardScope switch
                {
                    PipelineBlackboardVariableScope.Character => graph == m_RootTree,
                    PipelineBlackboardVariableScope.State => graph is StateBehaviorSubTree,
                    PipelineBlackboardVariableScope.ActionInstance => graph is StateBehaviorSubTree && graph.Nodes.Any(i => i is ActivateActionInstanceNode),
                    _ => true
                };
                if (!ownerValid)
                    m_Report.Error(declarationPath, "blackboard_scope_owner_invalid", $"{declaration.BlackboardScope} declaration {declaration.BlackboardKey} 不属于合法 Graph owner context。");
            }
        }

        void ValidateGraphTypeRules(BaseTree graph, string path)
        {
            if (graph is StateMachineGraph stateMachineGraph)
            {
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    BaseNode node = graph.Nodes[i];
                    if (node is StateMachineEnterNode || node is StateMachineAnyStateNode || node is StateMachineExitNode || node is StateNode)
                        continue;

                    m_Report.Error($"{path}/node:{node?.GUID}", "invalid_state_machine_node", $"StateMachineGraph 中存在非法节点：{node?.GetType().Name}");
                }

                if (stateMachineGraph.EnterNode == null || stateMachineGraph.AnyStateNode == null || stateMachineGraph.ExitNode == null)
                    m_Report.Error(path, "missing_state_machine_control_node", "StateMachineGraph 缺少 Enter/AnyState/Exit 控制节点。");
            }

            if (graph is ConditionRuleGraph conditionRuleGraph)
            {
                int resultCount = 0;
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    BaseNode node = graph.Nodes[i];
                    if (node is ConditionRuleResultNode)
                    {
                        resultCount++;
                        continue;
                    }

                    if (node is RunnableNode || node is StateMachineControlNode || node is StateMachineNode || node is StateNode || node is StateLifecycleNode || node is RootNode)
                        m_Report.Error($"{path}/node:{node?.GUID}", "invalid_condition_rule_node", $"ConditionRuleGraph 中存在行为节点：{node?.GetType().Name}");
                }

                if (resultCount != 1 || conditionRuleGraph.ResultNode == null)
                    m_Report.Error(path, "invalid_condition_rule_result", "ConditionRuleGraph 必须有且只有一个 Rule Result。");
            }
        }

        void ValidateGraphOwnership(BaseTree graph, string path)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                BaseNode node = graph.Nodes[i];
                if (node == null)
                    continue;

                ScopedGraphReferenceModule scoped = node.GetModule<ScopedGraphReferenceModule>();
                if (scoped != null && scoped.InlineGraph != null && scoped.SharedGraphAsset)
                    m_Report.Error($"{path}/node:{node.GUID}", "mixed_graph_ownership", "StateMachineNode 同时持有 inline 和 shared graph。");

                StateBehaviorGraphReferenceModule stateBehavior = node.GetModule<StateBehaviorGraphReferenceModule>();
                if (stateBehavior != null && stateBehavior.InlineSubTree != null && stateBehavior.SharedSubTreeAsset)
                    m_Report.Error($"{path}/node:{node.GUID}", "mixed_graph_ownership", "StateNode 同时持有 inline 和 shared state behavior graph。");
            }

            for (int i = 0; i < graph.Edges.Count; i++)
            {
                BaseEdge edge = graph.Edges[i];
                if (edge == null || edge.ConditionRuleGraphReferenceStatus == ConditionRuleGraphReferenceStatus.Unspecified)
                    continue;

                if (!edge.HasResolvedConditionRuleGraph)
                {
                    m_Report.Error(
                        $"{path}/edge:{edge.GUID}",
                        "condition_rule_ownership_invalid",
                        $"ConditionRuleGraph 引用无效：owner={graph.name}/{graph.GraphAuthoringId} edge={edge.GUID} ownership={edge.ConditionRuleGraphOwnership} reason={edge.ConditionRuleGraphReferenceError}");
                }
            }
        }

        void ValidateNode(BaseTree graph, BaseNode node, string path, IReadOnlyList<BaseGraph> visibleGraphs)
        {
            if (node is StateMachineNode stateMachineNode)
            {
                if (stateMachineNode.Graph == null)
                    m_Report.Error(path, "nested_state_machine_graph_missing", "StateMachineNode 缺少 resolved StateMachineGraph。");
            }

            if (node is TimelineNode timelineNode)
            {
                if (graph is StateMachineGraph || graph is ConditionRuleGraph)
                    m_Report.Error(path, "timeline_wrong_graph", "TimelineNode 不能位于 StateMachineGraph 或 ConditionRuleGraph。", "把 TimelineNode 放入 StateNode 的状态行为图。");

                if (timelineNode.InlineTimeline != null && timelineNode.SharedTimelineAsset)
                    m_Report.Error(path, "timeline_mixed_ownership", "TimelineNode 同时持有 inline TimelineData 与 shared TimelineAsset。");
                if (timelineNode.TimelineOwnership == TimelineOwnership.Missing || timelineNode.Timeline == null)
                    m_Report.Error(path, "timeline_missing_source", "TimelineNode 缺少正式 Inline 或 Shared Timeline source。");
                else
                {
                    TimelineData timeline = timelineNode.Timeline;
                    if (!timeline.SerializedOwner || string.IsNullOrEmpty(timeline.SerializedPropertyPath))
                        m_Report.Error(path, "timeline_serialized_owner_missing", "TimelineData 缺少 serialized owner/path。");
                    if (timelineNode.TimelineOwnership == TimelineOwnership.Inline && !ReferenceEquals(timeline.SerializedOwner, graph.SerializedOwner))
                        m_Report.Error(path, "timeline_inline_owner_mismatch", "Inline TimelineData 未归属当前 RootTree serialized owner。");
                    if (timelineNode.TimelineOwnership == TimelineOwnership.Shared && !ReferenceEquals(timeline.SerializedOwner, timelineNode.SharedTimelineAsset))
                        m_Report.Error(path, "timeline_shared_owner_mismatch", "Shared TimelineData 未归属对应 TimelineAsset。");
                    ValidateTimeline(timelineNode.Timeline, $"{path}/timeline", visibleGraphs);
                }
            }

            if (node is ActivateActionInstanceNode activationNode)
            {
                if (!activationNode.ActionProfile)
                    m_Report.Error(path, "action_profile_missing", "ActivateActionInstanceNode 缺少 ActionProfile。");
                else if (!HasActionProfile(activationNode.ActionProfile.ActionId))
                    m_Report.Error(path, "action_profile_not_in_definition", $"ActionProfile 不属于当前 CharacterPipelineDefinition：{activationNode.ActionProfile.ActionId}");

                if (!string.IsNullOrEmpty(activationNode.SourceInputRequestId) && !HasActionRequest(activationNode.SourceInputRequestId))
                    m_Report.Error(path, "request_not_in_definition", $"Source Input Request 不属于当前 InputProfile：{activationNode.SourceInputRequestId}");
            }

            if (node is CharacterActionRequestInfoNode requestNode &&
                !string.IsNullOrEmpty(requestNode.RequestId) &&
                !HasActionRequest(requestNode.RequestId))
                m_Report.Error(path, "request_not_in_definition", $"Action request 节点引用了不存在的 request：{requestNode.RequestId}");

            if (node is CharacterInputValueInfoNode inputValueNode &&
                !string.IsNullOrEmpty(inputValueNode.InputValueId) &&
                !HasInputValue(inputValueNode.InputValueId))
                m_Report.Error(path, "input_value_not_in_definition", $"Input value 节点引用了不存在的 input value：{inputValueNode.InputValueId}");

            if (node is CharacterMoveFacingAngleInfoNode facingAngleNode &&
                !HasPropertyInput(graph, facingAngleNode, "m_MoveInput"))
                m_Report.Error(path, "facing_angle_input_missing", "Move Facing Angle 节点缺少 Move Input property edge。");

            if (node is ExposedPropertyNode exposedPropertyNode)
            {
                ValidateBlackboardReference(exposedPropertyNode.BlackboardVariable, exposedPropertyNode.Value.ValueType, visibleGraphs, path, true);
                if (exposedPropertyNode.NodeType == ExposedPropertyNodeType.Set &&
                    m_Declarations.TryGetValue(exposedPropertyNode.BlackboardVariable.DeclarationId, out BaseExposedProperty writeDeclaration) &&
                    writeDeclaration.BlackboardFactProjection == PipelineBlackboardFactProjectionKind.ActionWindow &&
                    !(graph is TimelineRunningTree) &&
                    !(exposedPropertyNode.FactContext is ActionContextSlot))
                    m_Report.Error(path, "window_projection_fact_context_missing", "非 TimelineData ActionWindow projection 写入必须显式引用 Action Context。");
            }
            if (node is PipelineBlackboardValueInfoNode blackboardValueNode)
                ValidateBlackboardReference(blackboardValueNode.BlackboardVariable, blackboardValueNode.BlackboardValueType, visibleGraphs, path, true);
            if (node is ActivateActionInstanceNode actionActivationNode)
                ValidateBlackboardReference(actionActivationNode.TargetSnapshotVariable, typeof(ActionTargetSnapshot), visibleGraphs, path, false);
            if (node is SubmitGameplayCueNode cueNode)
                ValidateBlackboardReference(cueNode.BlackboardVariable, typeof(GameplayCueFact), visibleGraphs, path, false);
            if (node is SubmitGameplayResultEventNode gameplayResultNode)
                ValidateBlackboardReference(gameplayResultNode.BlackboardVariable, typeof(GameplayResultEvent), visibleGraphs, path, false);
        }

        void ValidateTimeline(TimelineData timeline, string path, IReadOnlyList<BaseGraph> visibleGraphs)
        {
            if (timeline == null || !m_ValidatedTimelines.Add(timeline))
                return;

            var identityErrors = new List<string>();
            if (!timeline.ValidateAuthoringIdentities(identityErrors))
            {
                for (int i = 0; i < identityErrors.Count; i++)
                    m_Report.Error(path, "timeline_authoring_identity_invalid", identityErrors[i]);
            }
            if (!m_TimelineAuthoringIds.Add(timeline.AuthoringId))
                m_Report.Error(path, "timeline_authoring_identity_duplicate", $"重复 TimelineAuthoringId：{timeline.AuthoringId}");
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                if (!m_TrackAuthoringIds.Add(track.AuthoringId))
                    m_Report.Error($"{path}/track:{trackIndex}", "track_authoring_identity_duplicate", $"重复 TrackAuthoringId：{track.AuthoringId}");
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip != null && !m_ClipAuthoringIds.Add(clip.AuthoringId))
                        m_Report.Error($"{path}/track:{trackIndex}/clip:{clipIndex}", "clip_authoring_identity_duplicate", $"重复 ClipAuthoringId：{clip.AuthoringId}");
                }
            }

            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                if (!(timeline.Tracks[trackIndex] is TreeTrack treeTrack))
                    continue;

                for (int clipIndex = 0; clipIndex < treeTrack.Clips.Count; clipIndex++)
                {
                    if (!(treeTrack.Clips[clipIndex] is TreeClip treeClip))
                        continue;

                    string clipPath = $"{path}/tree:{trackIndex}:{clipIndex}";
                    if (treeClip.InlineTree != null && treeClip.SharedTreeAsset)
                        m_Report.Error(clipPath, "treeclip_mixed_ownership", "TreeClip 同时持有 inline 和 shared Tree 真数据。");
                    if (treeClip.Ownership == TimelineTreeOwnership.Missing || treeClip.ResolvedTree == null)
                    {
                        m_Report.Error(clipPath, "treeclip_missing_tree", "TreeClip 缺少 TimelineRunningTree。");
                        continue;
                    }

                    TimelineRunningTree tree = treeClip.ResolvedTree;
                    if (treeClip.Ownership == TimelineTreeOwnership.Inline &&
                        (!ReferenceEquals(tree.SerializedOwner, timeline.SerializedOwner) || string.IsNullOrEmpty(tree.SerializedPropertyPath)))
                        m_Report.Error(clipPath, "treeclip_inline_owner_mismatch", "Inline TreeClip graph 未绑定 TimelineData 的 serialized owner/path。");
                    if (treeClip.ExecutionPhase == TimelineTreeExecutionPhase.Decision)
                    {
                        var errors = new List<string>();
                        TimelineTreeDecisionValidation.Validate(
                            tree,
                            errors,
                            reference => m_Declarations.TryGetValue(reference.DeclarationId, out BaseExposedProperty declaration) &&
                                         m_DeclarationOwnerIds.TryGetValue(reference.DeclarationId, out string ownerId) &&
                                         ownerId == reference.DeclarationOwnerId
                                ? declaration
                                : null);
                        for (int i = 0; i < errors.Count; i++)
                            m_Report.Error(clipPath, "treeclip_decision_invalid", errors[i]);

                        ValidateProjectedWindowOutputs(tree, visibleGraphs, clipPath);
                    }
                }
            }
        }

        void ValidateElementAuthoringIdentities(BaseGraph graph, string path)
        {
            var identities = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                BaseNode node = graph.Nodes[i];
                string id = node?.GUID;
                if (!AuthoringIdentity.IsValid(id))
                    m_Report.Error($"{path}/node:{i}", "node_authoring_identity_invalid", "Node authoring identity 缺失或格式无效。");
                else if (!identities.Add(id))
                    m_Report.Error($"{path}/node:{id}", "element_authoring_identity_duplicate", $"Graph 内重复 element identity：{id}");
            }
            for (int i = 0; i < graph.Edges.Count; i++)
                ValidateEdgeIdentity(graph.Edges[i], $"{path}/edge:{i}", identities);
            for (int i = 0; i < graph.PropertyEdges.Count; i++)
                ValidateEdgeIdentity(graph.PropertyEdges[i], $"{path}/property-edge:{i}", identities);
        }

        void ValidateEdgeIdentity(BaseEdge edge, string path, HashSet<string> identities)
        {
            string id = edge?.GUID;
            if (!AuthoringIdentity.IsValid(id))
                m_Report.Error(path, "edge_authoring_identity_invalid", "Edge authoring identity 缺失或格式无效。");
            else if (!identities.Add(id))
                m_Report.Error(path, "element_authoring_identity_duplicate", $"Graph 内重复 element identity：{id}");
        }

        void ValidateProjectedWindowOutputs(TimelineRunningTree tree, IReadOnlyList<BaseGraph> visibleGraphs, string path)
        {
            List<ActionProfile> profiles = visibleGraphs
                .SelectMany(i => i.Nodes)
                .OfType<ActivateActionInstanceNode>()
                .Where(i => i.ActionProfile)
                .Select(i => i.ActionProfile)
                .Distinct()
                .ToList();
            foreach (ExposedPropertyNode setter in tree.Nodes.OfType<ExposedPropertyNode>())
            {
                if (setter.NodeType != ExposedPropertyNodeType.Set ||
                    !m_Declarations.TryGetValue(setter.BlackboardVariable.DeclarationId, out BaseExposedProperty declaration) ||
                    declaration.BlackboardFactProjection != PipelineBlackboardFactProjectionKind.ActionWindow)
                    continue;

                string outputPath = $"{path}/output:{declaration.DeclarationId}";
                if (profiles.Count == 0)
                {
                    m_Report.Error(outputPath, "window_projection_action_profile_missing", $"{declaration.BlackboardKey} 没有可解析的 ActionProfile context。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(declaration.ActionWindowType))
                    m_Report.Error(outputPath, "window_projection_type_missing", "ActionWindow projection 缺少 WindowType。");
            }
        }

        void ValidateBlackboardReference(
            PipelineBlackboardVariableReference reference,
            Type expectedType,
            IReadOnlyList<BaseGraph> visibleGraphs,
            string path,
            bool required)
        {
            if (!reference.IsValid)
            {
                if (required)
                    m_Report.Error(path, "blackboard_reference_missing", "Pipeline Blackboard variable reference 缺失。");
                return;
            }

            if (!m_Declarations.TryGetValue(reference.DeclarationId, out BaseExposedProperty declaration))
            {
                m_Report.Error(path, "blackboard_reference_broken", $"Pipeline Blackboard declaration 不存在：{reference.DisplayKey}/{reference.DeclarationId}");
                return;
            }

            if (!m_DeclarationOwnerIds.TryGetValue(reference.DeclarationId, out string declarationOwnerId) ||
                !string.Equals(declarationOwnerId, reference.DeclarationOwnerId, StringComparison.Ordinal))
                m_Report.Error(path, "blackboard_reference_owner_mismatch", $"Pipeline Blackboard reference owner 不匹配：{reference.DisplayKey}");

            if (!visibleGraphs.Any(i => string.Equals(i.GraphAuthoringId, reference.DeclarationOwnerId, StringComparison.Ordinal)))
                m_Report.Error(path, "blackboard_reference_not_visible", $"Pipeline Blackboard declaration 对当前 Graph 不可见：{reference.DisplayKey}");

            if (declaration.ValueType != expectedType || !reference.MatchesValueType(expectedType))
                m_Report.Error(path, "blackboard_reference_type_mismatch", $"Pipeline Blackboard reference {reference.DisplayKey} 需要 {expectedType?.Name ?? "Unknown"}，实际为 {declaration.ValueType?.Name ?? "Unknown"}。");
        }

        void ValidateEdge(BaseTree graph, BaseEdge edge, string path)
        {
            BaseNode startNode = ResolveNode(graph, edge.StartNode, edge.StartNodeGUID);
            BaseNode endNode = ResolveNode(graph, edge.EndNode, edge.EndNodeGUID);
            if (!(graph is StateMachineGraph stateMachineGraph))
            {
                bool isBTConditionEdge =
                    startNode is CompositeNode &&
                    endNode is RunnableNode &&
                    edge.StartPortName == "Output" &&
                    edge.EndPortName == "Input";

                if (!isBTConditionEdge && edge.HasConditionRuleGraphConfiguration)
                    m_Report.Error(path, "invalid_bt_condition_edge", "ConditionRuleGraph 只能配置在 BT composite output edge 或 StateMachine transition 上。");

                if (isBTConditionEdge &&
                    edge.ConditionRuleGraphReferenceStatus != ConditionRuleGraphReferenceStatus.Unspecified &&
                    !edge.HasResolvedConditionRuleGraph)
                {
                    m_Report.Error(path, "bt_condition_rule_invalid", $"BT edge ConditionRuleGraph 无效：owner={graph.name}/{graph.GraphAuthoringId} edge={edge.GUID} ownership={edge.ConditionRuleGraphOwnership} reason={edge.ConditionRuleGraphReferenceError}");
                }

                if (isBTConditionEdge && !(startNode is SelectorNode) &&
                    (edge.AbortPolicy == BTAbortPolicy.LowerPriority || edge.AbortPolicy == BTAbortPolicy.Both))
                    m_Report.Error(path, "invalid_bt_abort_policy", "LowerPriority/Both 只能配置在 Selector child edge 上。");

                if (!isBTConditionEdge && edge.AbortPolicy != BTAbortPolicy.None)
                    m_Report.Error(path, "invalid_bt_abort_policy", "AbortPolicy 只能配置在 BT composite output edge 上。");

                return;
            }

            bool isTransition =
                edge.StartPortName == StateMachinePorts.StateOut &&
                edge.EndPortName == StateMachinePorts.StateIn &&
                (startNode is StateMachineEnterNode || startNode is StateMachineAnyStateNode || startNode is StateNode) &&
                (endNode is StateNode || endNode is StateMachineExitNode);
            if (!isTransition)
                m_Report.Error(path, "invalid_transition_edge", "StateMachineGraph flow edge 必须是 StateOut -> StateIn transition。");

            if (edge.AbortPolicy != BTAbortPolicy.None)
                m_Report.Error(path, "invalid_transition_abort_policy", "StateMachine transition 不使用 BT AbortPolicy。");

            if (!edge.HasResolvedConditionRuleGraph)
            {
                m_Report.Error(path, "transition_condition_rule_invalid", $"Transition ConditionRuleGraph 无效：owner={graph.name}/{graph.GraphAuthoringId} edge={edge.GUID} ownership={edge.ConditionRuleGraphOwnership} reason={edge.ConditionRuleGraphReferenceError}");
                return;
            }

            if (startNode is StateMachineAnyStateNode)
            {
                ConditionRuleGraph ruleGraph = edge.ConditionRuleGraph;
                if (!ruleGraph || ruleGraph.Nodes.Count <= 1)
                    m_Report.Error(path, "anystate_missing_condition", "AnyState transition 必须配置非默认条件。");
            }

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

        static bool HasPropertyInput(BaseTree graph, BaseNode node, string portId)
        {
            if (graph == null || node == null || string.IsNullOrEmpty(portId))
                return false;

            for (int i = 0; i < graph.PropertyEdges.Count; i++)
            {
                PropertyEdge edge = graph.PropertyEdges[i];
                if (edge != null && edge.EndNodeGUID == node.GUID && edge.EndPortName == portId)
                    return true;
            }

            return false;
        }

        void ValidateActionContextChain(BaseTree graph, string path)
        {
            if (!(graph is StateBehaviorSubTree))
                return;

            List<ActivateActionInstanceNode> activations = new List<ActivateActionInstanceNode>();
            List<TimelineNode> timelines = new List<TimelineNode>();
            List<SubmitActionLifecycleTransitionNode> lifecycleTransitions = new List<SubmitActionLifecycleTransitionNode>();

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i] is ActivateActionInstanceNode activation)
                    activations.Add(activation);
                else if (graph.Nodes[i] is TimelineNode timeline)
                    timelines.Add(timeline);
                else if (graph.Nodes[i] is SubmitActionLifecycleTransitionNode lifecycle)
                    lifecycleTransitions.Add(lifecycle);
            }

            for (int i = 0; i < timelines.Count; i++)
            {
                TimelineNode timeline = timelines[i];
                if (!timeline.ActionContext && activations.Count > 0)
                    m_Report.Error($"{path}/node:{timeline.GUID}", "action_context_missing", "动作状态里的 TimelineNode 缺少 Action Context。");

                if (timeline.ActionContext && activations.Count > 0 && !HasMatchingActivationContext(activations, timeline.ActionContext))
                    m_Report.Warning($"{path}/node:{timeline.GUID}", "action_context_unmatched", "TimelineNode 的 Action Context 没有在同一状态行为图中找到对应 activation。");
            }

            for (int i = 0; i < activations.Count; i++)
                ValidateActionTerminalLifecycle(graph, activations[i], lifecycleTransitions, path);
        }

        void ValidateActionTerminalLifecycle(
            BaseTree graph,
            ActivateActionInstanceNode activation,
            List<SubmitActionLifecycleTransitionNode> lifecycleTransitions,
            string path)
        {
            if (!activation.ActionContext)
            {
                m_Report.Error($"{path}/node:{activation.GUID}", "action_context_missing", "动作 activation 缺少 Action Context。");
                return;
            }

            int cancelCount = CountLifecycle(lifecycleTransitions, activation.ActionContext, ActionLifecycleTransitionType.Cancel);
            int abortCount = CountLifecycle(lifecycleTransitions, activation.ActionContext, ActionLifecycleTransitionType.Abort);
            int completeCount = CountLifecycle(lifecycleTransitions, activation.ActionContext, ActionLifecycleTransitionType.Complete);
            int succeedCount = 0;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i] is SucceedNode && graph.Nodes[i].ResolvedDisplayName == "Succeed")
                    succeedCount++;
            }

            if (cancelCount != 1 || abortCount != 1 || completeCount != 1 || succeedCount != 1)
            {
                m_Report.Error(
                    path,
                    "action_exit_lifecycle_incomplete",
                    $"动作状态必须为同一 Action Context 配置唯一 Cancel、Abort、Complete 与 Succeed 出口，当前 Cancel={cancelCount}、Abort={abortCount}、Complete={completeCount}、Succeed={succeedCount}。");
            }
        }

        static int CountLifecycle(
            List<SubmitActionLifecycleTransitionNode> lifecycleTransitions,
            ActionContextSlot actionContext,
            ActionLifecycleTransitionType transitionType)
        {
            int count = 0;
            for (int i = 0; i < lifecycleTransitions.Count; i++)
            {
                SubmitActionLifecycleTransitionNode lifecycle = lifecycleTransitions[i];
                if (lifecycle.ActionContext == actionContext && lifecycle.TransitionType == transitionType)
                    count++;
            }
            return count;
        }

        void ValidateActionOverrideBody(BaseTree graph, string path)
        {
            if (!(graph is StateBehaviorSubTree) ||
                graph.name.IndexOf("ActionOverride", System.StringComparison.OrdinalIgnoreCase) < 0)
                return;

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                BaseNode node = graph.Nodes[i];
                if (node is TimelineNode ||
                    node is ActivateActionInstanceNode ||
                    node is LocomotionInputMotionNode ||
                    node is SubmitGameplayResultMotionNode)
                    m_Report.Error($"{path}/node:{node.GUID}", "action_override_owns_behavior", "ActionOverride 只能表达 locomotion ownership，不得引用 Action、TimelineData 或 motion 行为。");
            }
        }

        bool HasMatchingActivationContext(List<ActivateActionInstanceNode> activations, ActionContextSlot context)
        {
            for (int i = 0; i < activations.Count; i++)
            {
                if (activations[i].ActionContext == context)
                    return true;
            }
            return false;
        }

        bool HasActionProfile(string actionId)
        {
            if (!m_Definition)
                return false;

            IReadOnlyList<ThirdPersonCharacter.ActionSystem.ActionProfile> profiles = m_Definition.ActionProfiles;
            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i] && profiles[i].ActionId == actionId)
                    return true;
            }
            return false;
        }

        bool HasActionRequest(string requestId)
        {
            CharacterInputProfile profile = m_Definition ? m_Definition.InputProfile : null;
            if (!profile)
                return false;

            IReadOnlyList<CharacterActionRequestDefinition> requests = profile.ActionRequests;
            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i] != null && requests[i].RequestId == requestId)
                    return true;
            }
            return false;
        }

        bool HasInputValue(string inputValueId)
        {
            CharacterInputProfile profile = m_Definition ? m_Definition.InputProfile : null;
            if (!profile)
                return false;

            IReadOnlyList<CharacterInputValueDefinition> values = profile.InputValues;
            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] != null && values[i].InputValueId == inputValueId)
                    return true;
            }
            return false;
        }

        void ValidateCorinAttackHierarchy()
        {
            if (m_Definition == null || m_Definition.name.IndexOf("Corin", StringComparison.OrdinalIgnoreCase) < 0)
                return;

            StateMachineNode actionNode = m_RootTree.Nodes
                .OfType<StateMachineNode>()
                .FirstOrDefault(i => string.Equals(i.ResolvedDisplayName, "Action StateMachine", StringComparison.Ordinal));
            StateMachineGraph outer = actionNode?.Graph;
            if (outer == null)
            {
                m_Report.Error("root/action", "corin_action_state_machine_missing", "Corin 缺少外层 Action StateMachine。");
                return;
            }

            string[] expectedOuter = { "None", "Attack", "DodgeBack", "DodgeForward" };
            string[] outerNames = outer.StateNodes.Select(i => i.ResolvedDisplayName).OrderBy(i => i).ToArray();
            if (!outerNames.SequenceEqual(expectedOuter.OrderBy(i => i)))
                m_Report.Error("root/action", "corin_action_categories_invalid", $"外层 Action StateMachine 必须仅包含 None、Attack、DodgeBack、DodgeForward，当前为 {string.Join(", ", outerNames)}。");

            StateNode attack = outer.StateNodes.FirstOrDefault(i => string.Equals(i.ResolvedDisplayName, "Attack", StringComparison.Ordinal));
            StateBehaviorSubTree attackBody = attack?.SubTree as StateBehaviorSubTree;
            if (attackBody == null)
            {
                m_Report.Error("root/action/Attack", "corin_attack_body_missing", "Attack 缺少 inline StateBehaviorSubTree。");
                return;
            }

            if (attackBody.Nodes.Any(i => i is ActivateActionInstanceNode || i is TimelineNode || i is SubmitActionLifecycleTransitionNode))
                m_Report.Error("root/action/Attack", "corin_attack_outer_owns_leaf_lifecycle", "外层 Attack 不得持有 Action activation、Timeline 或 terminal lifecycle。");

            List<StateMachineNode> nestedNodes = attackBody.Nodes.OfType<StateMachineNode>().ToList();
            if (nestedNodes.Count != 1 || nestedNodes[0].GetModule<ScopedGraphReferenceModule>()?.InlineGraph == null)
            {
                m_Report.Error("root/action/Attack", "corin_attack_nested_state_machine_invalid", "Attack Root 必须且只能运行一个 inline Attack Combo StateMachineNode。");
                return;
            }

            StateMachineGraph combo = nestedNodes[0].Graph;
            string[] comboNames = combo.StateNodes.Select(i => i.ResolvedDisplayName).OrderBy(i => i).ToArray();
            string[] expectedCombo = { "Attack1", "Attack2" };
            if (!comboNames.SequenceEqual(expectedCombo.OrderBy(i => i)))
                m_Report.Error("root/action/Attack/combo", "corin_attack_leaf_states_invalid", $"内层 Attack Combo 必须仅包含 Attack1、Attack2，当前为 {string.Join(", ", comboNames)}。");

            ValidateRequiredTransition(outer, "None", "Attack", "root/action");
            ValidateRequiredTransition(outer, "Attack", "None", "root/action");
            ValidateRequiredTransition(combo, "Enter", "Attack1", "root/action/Attack/combo");
            ValidateRequiredTransition(combo, "Attack1", "Attack2", "root/action/Attack/combo");
            ValidateRequiredTransition(combo, "Attack2", "Attack1", "root/action/Attack/combo");
            ValidateRequiredTransition(combo, "Attack1", "Exit", "root/action/Attack/combo");
            ValidateRequiredTransition(combo, "Attack2", "Exit", "root/action/Attack/combo");
            ValidateComboTransitionCondition(combo, "Attack1", "Attack2", "Attack1Cancel");
            ValidateComboTransitionCondition(combo, "Attack2", "Attack1", "Attack2Cancel");

            foreach (StateNode leaf in combo.StateNodes)
            {
                StateBehaviorSubTree body = leaf.SubTree as StateBehaviorSubTree;
                if (body == null)
                    continue;
                int activations = body.Nodes.OfType<ActivateActionInstanceNode>().Count();
                int timelines = body.Nodes.OfType<TimelineNode>().Count(i => i.TimelineOwnership == TimelineOwnership.Inline);
                int lifecycle = body.Nodes.OfType<SubmitActionLifecycleTransitionNode>().Count();
                if (activations != 1 || timelines != 1 || lifecycle != 3)
                    m_Report.Error($"root/action/Attack/combo/{leaf.ResolvedDisplayName}", "corin_attack_leaf_closure_invalid", $"攻击 leaf 必须有 1 activation、1 inline Timeline、3 terminal lifecycle，当前为 {activations}/{timelines}/{lifecycle}。");
            }
        }

        void ValidateComboTransitionCondition(StateMachineGraph graph, string from, string to, string cancelKey)
        {
            StateNode source = graph.StateNodes.FirstOrDefault(i => i.ResolvedDisplayName == from);
            StateNode target = graph.StateNodes.FirstOrDefault(i => i.ResolvedDisplayName == to);
            BaseEdge transition = source != null && target != null
                ? graph.Edges.FirstOrDefault(edge =>
                    edge != null &&
                    edge.StartNodeGUID == source.GUID &&
                    edge.EndNodeGUID == target.GUID &&
                    edge.StartPortName == StateMachinePorts.StateOut &&
                    edge.EndPortName == StateMachinePorts.StateIn)
                : null;
            ConditionRuleGraph rule = transition?.ConditionRuleGraph;
            string path = $"root/action/Attack/combo/{from}->{to}";
            if (rule == null)
            {
                m_Report.Error(path, "corin_combo_condition_missing", $"连段 transition 缺少 ConditionRuleGraph：{cancelKey} AND Attack request。");
                return;
            }

            rule.CheckInit();
            List<PipelineBlackboardBoolInfoNode> cancelNodes = rule.Nodes
                .OfType<PipelineBlackboardBoolInfoNode>()
                .Where(i => string.Equals(i.BlackboardVariable.DisplayKey, cancelKey, StringComparison.Ordinal))
                .ToList();
            List<CharacterActionRequestInfoNode> requestNodes = rule.Nodes
                .OfType<CharacterActionRequestInfoNode>()
                .Where(i => string.Equals(i.RequestId, "Attack", StringComparison.Ordinal))
                .ToList();
            List<AndNode> andNodes = rule.Nodes.OfType<AndNode>().ToList();
            ConditionRuleResultNode result = rule.ResultNode;
            if (cancelNodes.Count != 1 || requestNodes.Count != 1 || andNodes.Count != 1 || result == null)
            {
                m_Report.Error(path, "corin_combo_condition_shape_invalid", $"连段条件必须且只能包含 {cancelKey}、Attack request、And 与 Result。");
                return;
            }

            PipelineBlackboardBoolInfoNode cancel = cancelNodes[0];
            CharacterActionRequestInfoNode request = requestNodes[0];
            AndNode and = andNodes[0];
            bool inputsConnected =
                HasPropertyConnection(rule, cancel, "m_Output", and, "m_Input1") &&
                HasPropertyConnection(rule, request, "m_Output", and, "m_Input2") ||
                HasPropertyConnection(rule, cancel, "m_Output", and, "m_Input2") &&
                HasPropertyConnection(rule, request, "m_Output", and, "m_Input1");
            bool resultConnected = HasPropertyConnection(rule, and, "m_Output", result, "m_Result");
            if (!inputsConnected || !resultConnected)
                m_Report.Error(path, "corin_combo_condition_disconnected", $"连段条件未完整连接：{cancelKey} AND Attack request -> Result。");
        }

        static bool HasPropertyConnection(
            BaseGraph graph,
            BaseNode source,
            string sourcePort,
            BaseNode target,
            string targetPort)
        {
            return graph.PropertyEdges.Any(edge =>
                edge != null &&
                edge.StartNodeGUID == source.GUID &&
                edge.EndNodeGUID == target.GUID &&
                edge.StartPortName == sourcePort &&
                edge.EndPortName == targetPort);
        }

        void ValidateRequiredTransition(StateMachineGraph graph, string from, string to, string path)
        {
            BaseNode source = from == "Enter" ? graph.EnterNode : graph.StateNodes.FirstOrDefault(i => i.ResolvedDisplayName == from);
            BaseNode target = to == "Exit" ? graph.ExitNode : graph.StateNodes.FirstOrDefault(i => i.ResolvedDisplayName == to);
            bool exists = source != null && target != null && graph.Edges.Any(edge =>
                edge != null &&
                edge.StartNodeGUID == source.GUID &&
                edge.EndNodeGUID == target.GUID &&
                edge.StartPortName == StateMachinePorts.StateOut &&
                edge.EndPortName == StateMachinePorts.StateIn);
            if (!exists)
                m_Report.Error(path, "corin_attack_transition_missing", $"缺少 transition：{from} -> {to}");
        }
    }
}
