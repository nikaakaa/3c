using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.AI.Editor;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Graph;
using ThirdPersonCharacter.Pipeline.Input;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using ThirdPersonSimulation;
using TreeDesigner;
using TreeDesigner.Editor;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentGraphValidator
    {
        static readonly BtsmtlGraphAuthoringCapabilities s_Capabilities =
            new BtsmtlGraphAuthoringCapabilities();
        CharacterPipelineDefinition m_Definition;
        AgentCompileReport m_Report;
        readonly Dictionary<string, BaseExposedProperty> m_Declarations = new Dictionary<string, BaseExposedProperty>(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_DeclarationOwnerIds = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly HashSet<string> m_DuplicateDeclarationIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<TimelineData> m_ValidatedTimelines = new HashSet<TimelineData>();
        readonly HashSet<string> m_GraphAuthoringIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_TimelineAuthoringIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_TrackAuthoringIds = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> m_ClipAuthoringIds = new HashSet<string>(StringComparer.Ordinal);
        BaseTree m_RootTree;

        public AgentCompileReport Validate(
            CharacterPipelineDefinition definition,
            bool includeExactCompile = true)
        {
            m_Definition = definition;
            string definitionPath = definition ? AssetDatabase.GetAssetPath(definition) : string.Empty;
            m_Report = new AgentCompileReport
            {
                success = true,
                domain = AgentAuthoringSchema.CharacterControllerDomain,
                rootIdentity = AssetDatabase.AssetPathToGUID(definitionPath)
            };
            m_Declarations.Clear();
            m_DeclarationOwnerIds.Clear();
            m_DuplicateDeclarationIds.Clear();
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
            if (!definition.AnimationPresentationProfile)
                m_Report.Error(
                    "definition.animationPresentationProfile",
                    "missing_animation_presentation_profile",
                    "CharacterAnimationPresentationProfile 缺失。");
            if (!definition.BodyMotionProfile)
            {
                m_Report.Error("definition.bodyMotionProfile", "missing_body_motion_profile", "CharacterBodyMotionProfile 缺失。");
            }
            else
            {
                var bodyMotionErrors = new List<string>();
                definition.BodyMotionProfile.CollectConfigurationErrors(bodyMotionErrors);
                for (int i = 0; i < bodyMotionErrors.Count; i++)
                    m_Report.Error("definition.bodyMotionProfile", "body_motion_profile_invalid", bodyMotionErrors[i]);
            }
            if (!definition.InputProfile)
            {
                m_Report.Error("definition.inputProfile", "missing_input_profile", "CharacterInputProfile 缺失。");
            }
            else
            {
                var inputErrors = new List<string>();
                definition.InputProfile.CollectConfigurationErrors(inputErrors);
                for (int i = 0; i < inputErrors.Count; i++)
                    m_Report.Error("definition.inputProfile", "input_profile_invalid", inputErrors[i]);
            }

            m_RootTree = definition.RootTreeAsset.Tree;
            var topologyErrors = new List<string>();
            CharacterAuthoringTopologyProjection topology =
                CharacterAuthoringTopologyProjection.Build(m_RootTree, topologyErrors);
            for (int i = 0; i < topologyErrors.Count; i++)
                m_Report.Error("root", "authoring_topology_invalid", topologyErrors[i]);

            if (topology.IsValid)
            {
                IndexBlackboardDeclarations(topology);
                ValidateAnimationChannels(topology, definition.AnimationPresentationProfile);
                ValidateLinkedPose(definition.AnimationPresentationProfile);
                var actionTargetIssues = new List<ActionTargetAuthoringIssue>();
                ActionTargetAuthoringValidation.Collect(topology, actionTargetIssues);
                for (int i = 0; i < actionTargetIssues.Count; i++)
                    m_Report.Error(actionTargetIssues[i].Path, actionTargetIssues[i].Code, actionTargetIssues[i].Message);
                for (int i = 0; i < topology.Graphs.Count; i++)
                {
                    CharacterAuthoringGraphEntry entry = topology.Graphs[i];
                    if (!entry.FirstOccurrence)
                        continue;
                    string path = entry.Route.Count == 0 ? "root" : entry.Route.ToString();
                    ValidateGraph(entry.Graph, path, entry.VisibleGraphs);
                }
            }
            if (includeExactCompile)
            {
                CharacterSimulationBuildResult compileResult =
                    CharacterSimulationBuildOrchestrator.DryRun(definition);
                AppendFormalCompileReport(compileResult);
                bool semanticValid = compileResult.Artifact != null &&
                                     compileResult.Report.IsValid;
                m_Report.metrics.semanticValidCount = semanticValid ? 1 : 0;
                m_Report.metrics.semanticInvalidCount = semanticValid ? 0 : 1;
                m_Report.metrics.compileSuccessCount = compileResult.IsValid ? 1 : 0;
                m_Report.metrics.compileFailureCount = compileResult.IsValid ? 0 : 1;
            }
            m_Report.success = !m_Report.HasErrors();
            return m_Report;
        }

        void ValidateAnimationChannels(
            CharacterAuthoringTopologyProjection topology,
            CharacterAnimationPresentationProfile presentation)
        {
            if (!presentation)
                return;
            if (!presentation.PoseGraph || presentation.PoseGraph.Graph == null)
            {
                m_Report.Error(
                    "definition.animationPresentationProfile.poseGraph",
                    "presentation_pose_graph_missing",
                    "Animation Presentation Profile缺少正式Pose Graph。");
                return;
            }
            if (!presentation.RigDefinition)
                m_Report.Error(
                    "definition.animationPresentationProfile.rigDefinition",
                    "presentation_rig_definition_missing",
                    "Animation Presentation Profile缺少正式Rig Definition。");

            var actionTrackChannels = new HashSet<string>(StringComparer.Ordinal);
            for (int timelineIndex = 0; timelineIndex < topology.Timelines.Count; timelineIndex++)
            {
                CharacterAuthoringTimelineEntry entry = topology.Timelines[timelineIndex];
                TimelineData timeline = entry.Timeline;
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    if (timeline.Tracks[trackIndex] is not AnimationTrack track)
                        continue;
                    string path = $"timeline:{timeline.AuthoringId}/track:{track.AuthoringId}";
                    if (!track.AnimationChannelId.IsValid)
                    {
                        m_Report.Error(
                            path,
                            "animation_channel_missing",
                            "AnimationTrack缺少有效AnimationChannelId。");
                    }
                    else
                    {
                        actionTrackChannels.Add(track.AnimationChannelId.Value);
                    }
                    if (entry.Node == null || !entry.Node.ActionContext)
                    {
                        m_Report.Error(
                            path,
                            "animation_track_requires_finite_action",
                            "AnimationTrack只允许属于带Action Context的有限Timeline Action。持续Pose source必须放在Presentation Profile与PoseState provider中。");
                    }
                }
            }
            ValidatePresentationControlBoundary(
                presentation,
                actionTrackChannels);
        }

        void ValidateLinkedPose(
            CharacterAnimationPresentationProfile presentation)
        {
            if (!presentation)
                return;
            var configurationErrors = new List<string>();
            presentation.CollectConfigurationErrors(configurationErrors);
            for (int i = 0; i < configurationErrors.Count; i++)
            {
                m_Report.Error(
                    "definition.animationPresentationProfile.linkedPose",
                    "linked_pose_profile_invalid",
                    configurationErrors[i]);
            }
            var entriesByOwner = new Dictionary<
                CharacterPresentationPoseGraphAsset,
                HashSet<PoseGraphId>>();
            foreach (CharacterLinkedPoseImplementationAsset implementation in
                     presentation.LinkedPoseImplementations)
            {
                if (!implementation)
                    continue;
                string implementationPath =
                    "definition.animationPresentationProfile.linkedPoseImplementation:" +
                    implementation.ImplementationId.Value;
                try
                {
                    implementation.RequireValid();
                    foreach (CharacterLinkedPoseImplementationEntryBinding entry in
                             implementation.Entries)
                    {
                        CharacterTypedPoseGraph graph = entry.RequireValid();
                        CharacterLinkedPosePortProjection.RequireEntryGraphMatch(
                            graph,
                            implementation.Interface,
                            entry.EntryId);
                        if (!entriesByOwner.TryGetValue(
                                entry.GraphOwner,
                                out HashSet<PoseGraphId> entryGraphs))
                        {
                            entryGraphs = new HashSet<PoseGraphId>();
                            entriesByOwner.Add(entry.GraphOwner, entryGraphs);
                        }
                        entryGraphs.Add(entry.GraphId);
                    }
                }
                catch (Exception exception)
                {
                    m_Report.Error(
                        implementationPath,
                        "linked_pose_implementation_invalid",
                        exception.Message);
                }
            }
            foreach (KeyValuePair<CharacterPresentationPoseGraphAsset, HashSet<PoseGraphId>> pair in
                     entriesByOwner)
            {
                IReadOnlyList<string> errors =
                    CharacterPoseGraphCapabilityValidator.Validate(
                        pair.Key,
                        pair.Value);
                for (int i = 0; i < errors.Count; i++)
                {
                    m_Report.Error(
                        "definition.animationPresentationProfile.linkedPoseGraph:" +
                        pair.Key.name,
                        "linked_pose_capability_invalid",
                        errors[i]);
                }
            }
        }

        void ValidatePresentationControlBoundary(
            CharacterAnimationPresentationProfile presentation,
            HashSet<string> actionTrackChannels)
        {
            var actionInputs = new Dictionary<string, int>(StringComparer.Ordinal);
            var animationSlots = new Dictionary<string, int>(StringComparer.Ordinal);
            ValidatePresentationGraph(
                presentation,
                presentation.PoseGraph,
                presentation.PoseGraph.Graph,
                string.Empty,
                new HashSet<PoseGraphId>(),
                actionInputs,
                animationSlots);

            var channels = new HashSet<string>(
                actionTrackChannels,
                StringComparer.Ordinal);
            channels.UnionWith(actionInputs.Keys);
            channels.UnionWith(animationSlots.Keys);
            foreach (string channel in channels.OrderBy(value => value, StringComparer.Ordinal))
            {
                actionInputs.TryGetValue(channel, out int inputCount);
                animationSlots.TryGetValue(channel, out int slotCount);
                bool hasActionTrack = actionTrackChannels.Contains(channel);
                string path = $"definition.animationPresentationProfile.actionChannel:{channel}";
                if (!hasActionTrack)
                {
                    m_Report.Error(
                        path,
                        "action_channel_without_finite_timeline",
                        "Action Playback Input或AnimationSlot引用的AnimationChannel没有任何有限Timeline Action producer。");
                }
                if (inputCount != 1)
                {
                    m_Report.Error(
                        path,
                        "action_playback_input_count_invalid",
                        $"Action AnimationChannel必须有且只有一个ActionPlaybackInput，当前为{inputCount}个。");
                }
                if (slotCount != 1)
                {
                    m_Report.Error(
                        path,
                        "action_channel_consumer_count_invalid",
                        $"AnimationSlot必须是Action AnimationChannel的唯一consumer，当前为{slotCount}个。");
                }
            }
        }

        void ValidatePresentationGraph(
            CharacterAnimationPresentationProfile presentation,
            CharacterPresentationPoseGraphAsset owner,
            CharacterTypedPoseGraph graph,
            string scope,
            HashSet<PoseGraphId> path,
            Dictionary<string, int> actionInputs,
            Dictionary<string, int> animationSlots)
        {
            if (!owner || graph == null || !path.Add(graph.GraphId))
                return;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                CharacterTypedPoseNode node = graph.Nodes[i];
                if (node == null)
                    continue;
                string nodeId = string.IsNullOrEmpty(scope)
                    ? node.NodeId.Value
                    : scope + "/" + node.NodeId.Value;
                string nodePath =
                    $"definition.animationPresentationProfile.poseGraph:{graph.GraphId}/node:{nodeId}";
                bool stateLocal =
                    node.Kind == CharacterPoseNodeKind.SelectedPosePlayer ||
                    node.Kind == CharacterPoseNodeKind.BlendStack ||
                    node.Kind == CharacterPoseNodeKind.BlendSpacePlayer ||
                    node.Kind == CharacterPoseNodeKind.ClipPlayer;
                if (stateLocal)
                    ValidateStateLocalPoseSource(presentation, node, nodePath);
                if (!stateLocal &&
                    node.Kind != CharacterPoseNodeKind.ActionPlaybackInput &&
                    node.Kind != CharacterPoseNodeKind.AnimationSlot &&
                    node.AnimationChannelId.IsValid)
                {
                    m_Report.Error(
                        nodePath,
                        "action_channel_owner_invalid",
                        "只有ActionPlaybackInput与AnimationSlot可以引用Action AnimationChannel。");
                }
                if (node.Kind == CharacterPoseNodeKind.ActionPlaybackInput)
                    CountActionChannel(node, nodePath, actionInputs, "action_playback_input_channel_missing");
                if (node.Kind == CharacterPoseNodeKind.AnimationSlot)
                    CountActionChannel(node, nodePath, animationSlots, "animation_slot_channel_missing");

                if (node.Kind == CharacterPoseNodeKind.PoseSubgraph &&
                    node.Subgraph?.PoseGraphId.IsValid == true)
                {
                    CharacterTypedPoseGraph child =
                        owner.RequireGraph(node.Subgraph.PoseGraphId);
                    ValidatePresentationGraph(
                        presentation,
                        owner,
                        child,
                        nodeId + "/" + child.GraphId,
                        path,
                        actionInputs,
                        animationSlots);
                }

                CharacterPoseStateMachineDefinition machine =
                    node.PoseStateMachine;
                if (node.Kind != CharacterPoseNodeKind.PoseStateMachine ||
                    machine == null)
                {
                    continue;
                }
                for (int stateIndex = 0;
                     stateIndex < machine.States.Count;
                     stateIndex++)
                {
                    CharacterPoseStateDefinition state = machine.States[stateIndex];
                    if (state == null)
                        continue;
                    CharacterTypedPoseGraph child =
                        owner.RequireGraph(state.PoseGraphId);
                    ValidatePresentationGraph(
                        presentation,
                        owner,
                        child,
                        nodeId + "/state/" + state.StateId.Value,
                        path,
                        actionInputs,
                        animationSlots);
                }
            }
            path.Remove(graph.GraphId);
        }

        void ValidateStateLocalPoseSource(
            CharacterAnimationPresentationProfile presentation,
            CharacterTypedPoseNode node,
            string path)
        {
            if (node.AnimationChannelId.IsValid)
            {
                m_Report.Error(
                    path,
                    "state_local_pose_source_has_action_channel",
                    "State-local Pose source不能保存Gameplay AnimationChannel。");
            }
            CharacterPresentationPoseSourceSlot sourceSlot =
                node.PresentationPoseSourceSlot;
            if (!sourceSlot)
            {
                m_Report.Error(
                    path,
                    "presentation_pose_source_missing",
                    "State-local Pose source缺少typed Source Slot对象引用。");
                return;
            }
            Type expectedSlotType = node.Kind switch
            {
                CharacterPoseNodeKind.SelectedPosePlayer =>
                    typeof(CharacterMotionMatchingPoseSourceSlot),
                CharacterPoseNodeKind.BlendStack =>
                    typeof(CharacterMotionMatchingPoseSourceSlot),
                CharacterPoseNodeKind.BlendSpacePlayer =>
                    typeof(CharacterBlendSpacePoseSourceSlot),
                CharacterPoseNodeKind.ClipPlayer =>
                    typeof(CharacterClipPoseSourceSlot),
                _ => typeof(CharacterPresentationPoseSourceSlot)
            };
            if (!expectedSlotType.IsInstanceOfType(sourceSlot))
            {
                m_Report.Error(
                    path,
                    "presentation_pose_source_slot_type_invalid",
                    $"{node.Kind}必须引用{expectedSlotType.Name}。");
                return;
            }

            CharacterPresentationPoseSourceBinding source =
                presentation.FindPoseSourceBinding(sourceSlot);
            if (!source)
            {
                m_Report.Error(
                    path,
                    "presentation_pose_source_unresolved",
                    "Source Slot不能从Presentation Profile解析到唯一typed binding子资产。");
                return;
            }
            if (node.Kind == CharacterPoseNodeKind.ClipPlayer &&
                source?.SourceKind != PresentationPoseSourceKind.Clip)
            {
                m_Report.Error(
                    path,
                    "clip_pose_source_kind_invalid",
                    "ClipPlayer必须引用Clip Presentation Pose source。");
            }
            if (node.Kind == CharacterPoseNodeKind.BlendSpacePlayer &&
                source?.SourceKind != PresentationPoseSourceKind.BlendSpace)
            {
                m_Report.Error(
                    path,
                    "blend_space_pose_source_kind_invalid",
                    "BlendSpacePlayer必须引用BlendSpace Presentation Pose source。");
            }
        }

        void CountActionChannel(
            CharacterTypedPoseNode node,
            string path,
            Dictionary<string, int> counts,
            string missingCode)
        {
            if (!node.AnimationChannelId.IsValid)
            {
                m_Report.Error(
                    path,
                    missingCode,
                    $"{node.Kind}缺少有效Action AnimationChannel。");
                return;
            }
            string channel = node.AnimationChannelId.Value;
            counts.TryGetValue(channel, out int count);
            counts[channel] = count + 1;
        }

        void AppendFormalCompileReport(CharacterSimulationBuildResult result)
        {
            if (result?.Report == null)
            {
                m_Report.Error("compiler", "formal_compile_report_missing", "正式 Character Simulation Compiler 没有返回报告。");
                return;
            }
            for (int i = 0; i < result.Report.Messages.Count; i++)
            {
                CharacterSimulationCompileMessage message = result.Report.Messages[i];
                string path = $"compiler/{message.Stage}/{message.SourceIdentity}";
                switch (message.Severity)
                {
                    case CharacterSimulationCompileSeverity.Information:
                        m_Report.Info(path, message.Code, message.Message);
                        break;
                    case CharacterSimulationCompileSeverity.Warning:
                        m_Report.Warning(path, message.Code, message.Message);
                        break;
                    case CharacterSimulationCompileSeverity.Error:
                        m_Report.Error(path, message.Code, message.Message);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
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

                if (m_DuplicateDeclarationIds.Contains(declaration.DeclarationId))
                    m_Report.Error(declarationPath, "blackboard_declaration_duplicate", $"重复 declaration identity：{declaration.DeclarationId}");

                if (!PipelineBlackboardVariablePolicy.IsValid(declaration.BlackboardScope, declaration.BlackboardLifetime))
                    m_Report.Error(declarationPath, "blackboard_scope_lifetime_invalid", $"非法 scope/lifetime：{declaration.BlackboardScope}/{declaration.BlackboardLifetime}");
                if (!PipelineBlackboardVariablePolicy.TryValidateInputBinding(declaration, out string inputBindingError))
                    m_Report.Error(declarationPath, "blackboard_input_binding_invalid", inputBindingError);
                if (declaration.InputBinding != null &&
                    HasInputValue(declaration.InputValueId))
                    m_Report.Error(declarationPath, "input_value_identity_duplicate", $"Blackboard Input Binding 与普通 InputProfile 重复使用 InputValueId：{declaration.InputValueId}");
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

        void IndexBlackboardDeclarations(CharacterAuthoringTopologyProjection topology)
        {
            for (int graphIndex = 0; graphIndex < topology.Graphs.Count; graphIndex++)
            {
                CharacterAuthoringGraphEntry entry = topology.Graphs[graphIndex];
                if (!entry.FirstOccurrence || !(entry.Graph is BaseTree graph))
                    continue;

                graph.CheckInit();
                for (int declarationIndex = 0; declarationIndex < graph.ExposedProperties.Count; declarationIndex++)
                {
                    BaseExposedProperty declaration = graph.ExposedProperties[declarationIndex];
                    if (declaration == null || string.IsNullOrEmpty(declaration.DeclarationId))
                        continue;

                    if (!m_Declarations.TryAdd(declaration.DeclarationId, declaration))
                    {
                        m_DuplicateDeclarationIds.Add(declaration.DeclarationId);
                        continue;
                    }

                    m_DeclarationOwnerIds.Add(declaration.DeclarationId, graph.GraphAuthoringId);
                }
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
            ValidateNodePortShape(graph, node, path);
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

            if (node is ActionWindowActiveInfoNode windowNode)
            {
                if (string.IsNullOrWhiteSpace(windowNode.WindowType))
                    m_Report.Error(path, "action_window_type_missing", "ActionWindowActiveInfoNode 缺少 WindowType。");
                else if (!HasDecisionWindowProjection(windowNode.WindowType, visibleGraphs))
                    m_Report.Error(path, "action_window_phase_unavailable", $"WindowType '{windowNode.WindowType}' 没有在当前可见 owner 的 Decision TreeClip 中产生同帧 projection candidate。");
            }

            if (node is CanActivateActionInfoNode admissionNode)
            {
                if (!admissionNode.ActionProfile)
                    m_Report.Error(path, "action_profile_missing", "CanActivateActionInfoNode 缺少 ActionProfile。");
                else if (!HasActionProfile(admissionNode.ActionProfile.ActionId))
                    m_Report.Error(path, "action_profile_not_in_definition", $"CanActivateActionInfoNode 引用了 Definition 外的 ActionProfile：{admissionNode.ActionProfile.ActionId}");
                ValidateBlackboardReference(admissionNode.TargetSnapshotVariable, typeof(ActionTargetSnapshot), visibleGraphs, path, false);
            }

            if (node is CharacterInputValueInfoNode inputValueNode &&
                !string.IsNullOrEmpty(inputValueNode.InputValueId) &&
                !HasInputValue(inputValueNode.InputValueId))
                m_Report.Error(path, "input_value_not_in_definition", $"Input value 节点引用了不存在的 input value：{inputValueNode.InputValueId}");

            if (node is CharacterMoveFacingAngleInfoNode facingAngleNode &&
                !HasPropertyInput(graph, facingAngleNode, "m_MoveInput"))
                m_Report.Error(path, "facing_angle_input_missing", "Move Facing Angle 节点缺少 Move Input property edge。");

            if (node is ExposedPropertyNode exposedPropertyNode)
            {
                PortDirection expectedDirection = exposedPropertyNode.NodeType == ExposedPropertyNodeType.Get
                    ? PortDirection.Output
                    : PortDirection.Input;
                if (exposedPropertyNode.Value == null ||
                    exposedPropertyNode.Value.Direction != expectedDirection)
                {
                    m_Report.Error(
                        path,
                        "exposed_property_port_shape_mismatch",
                        $"ExposedProperty Node {node.GUID} 的 mode={exposedPropertyNode.NodeType}，m_Value 实际方向={exposedPropertyNode.Value?.Direction.ToString() ?? "Missing"}，期望方向={expectedDirection}。");
                }
                ValidateBlackboardReference(exposedPropertyNode.BlackboardVariable, exposedPropertyNode.Value?.ValueType, visibleGraphs, path, true);
                if (exposedPropertyNode.NodeType == ExposedPropertyNodeType.Set &&
                    m_Declarations.TryGetValue(exposedPropertyNode.BlackboardVariable.DeclarationId, out BaseExposedProperty writeDeclaration) &&
                    writeDeclaration.FactProjection?.Kind == PipelineBlackboardFactProjectionKind.ActionWindow &&
                    !(graph is TimelineRunningTree) &&
                    !(exposedPropertyNode.FactContext is ActionContextSlot))
                    m_Report.Error(path, "window_projection_fact_context_missing", "非 TimelineData ActionWindow projection 写入必须显式引用 Action Context。");
            }
            if (node is PipelineBlackboardValueInfoNode blackboardValueNode)
                ValidateBlackboardReference(blackboardValueNode.BlackboardVariable, blackboardValueNode.BlackboardValueType, visibleGraphs, path, true);
            if (node is ActivateActionInstanceNode actionActivationNode)
                ValidateBlackboardReference(actionActivationNode.TargetSnapshotVariable, typeof(ActionTargetSnapshot), visibleGraphs, path, false);
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
            var motionWarpIssues = new List<MotionWarpAuthoringIssue>();
            MotionWarpAuthoring.Validate(timeline, motionWarpIssues);
            for (int i = 0; i < motionWarpIssues.Count; i++)
                m_Report.Error(path, motionWarpIssues[i].Code, motionWarpIssues[i].Message);
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
                    if (clip != null)
                    {
                        for (int channelIndex = 0; channelIndex < TimelineCurveChannelCatalog.All.Count; channelIndex++)
                        {
                            TimelineCurveChannelDescriptor descriptor = TimelineCurveChannelCatalog.All[channelIndex];
                            if (!descriptor.Supports(clip))
                                continue;
                            try
                            {
                                descriptor.Validate(clip, descriptor.Read(clip));
                            }
                            catch (InvalidOperationException exception)
                            {
                                m_Report.Error(
                                    $"{path}/track:{trackIndex}/clip:{clipIndex}/curve:{descriptor.ChannelId.Value}",
                                    "timeline_curve_channel_invalid",
                                    exception.Message);
                            }
                        }
                    }
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

        void ValidateNodePortShape(BaseTree graph, BaseNode node, string path)
        {
            if (!s_Capabilities.TryGetSharedCapability(node, out GraphAuthoringCapabilityId capabilityId))
            {
                m_Report.Error(path, "node_capability_missing", $"Node {node.GUID} 没有正式 Capability。");
                return;
            }
            GraphAuthoringCapabilityDescriptor capability =
                s_Capabilities.SharedCatalog.Require(capabilityId);
            IReadOnlyList<GraphAuthoringDynamicPortProjection> projected;
            try
            {
                projected = s_Capabilities.ProjectPortShape(node, graph, capability);
            }
            catch (GraphAuthoringPortShapeException exception)
            {
                m_Report.Error(path, exception.Code, exception.Message);
                return;
            }

            var expected = capability.FixedPorts
                .Select(value => new KeyValuePair<GraphAuthoringPortId, Tuple<GraphAuthoringPortDirection, GraphAuthoringPortCapacity>>(
                    value.PortId,
                    Tuple.Create(value.Direction, value.Capacity)))
                .Concat(projected.Select(value =>
                    new KeyValuePair<GraphAuthoringPortId, Tuple<GraphAuthoringPortDirection, GraphAuthoringPortCapacity>>(
                        value.PortId,
                        Tuple.Create(value.Direction, value.Capacity))))
                .ToDictionary(value => value.Key, value => value.Value);
            var actual = new Dictionary<GraphAuthoringPortId, Tuple<GraphAuthoringPortDirection, GraphAuthoringPortCapacity>>();
            foreach (FlowPortDeclaration port in node.GetFlowPortDeclarations(graph))
            {
                actual[BtsmtlSharedGraphPort.Flow(port.Name)] = Tuple.Create(
                    BtsmtlSharedGraphPort.Direction(port.Direction),
                    BtsmtlSharedGraphPort.Capacity(port.Capacity));
            }
            foreach (PropertyPort port in node.PropertyPortMap.Values.Where(value => value != null))
            {
                actual[BtsmtlSharedGraphPort.Property(port.PortId)] = Tuple.Create(
                    BtsmtlSharedGraphPort.Direction(port.Direction),
                    port.Direction == PortDirection.Input
                        ? GraphAuthoringPortCapacity.Single
                        : GraphAuthoringPortCapacity.Multiple);
            }
            foreach (KeyValuePair<GraphAuthoringPortId, Tuple<GraphAuthoringPortDirection, GraphAuthoringPortCapacity>> pair in expected)
            {
                if (!actual.TryGetValue(pair.Key, out Tuple<GraphAuthoringPortDirection, GraphAuthoringPortCapacity> value))
                {
                    m_Report.Error(path, "node_port_shape_missing", $"Node {node.GUID} 缺少 Capability 端口 {pair.Key}。");
                    continue;
                }
                if (value.Item1 != pair.Value.Item1 || value.Item2 != pair.Value.Item2)
                {
                    m_Report.Error(
                        path,
                        "node_port_shape_mismatch",
                        $"Node {node.GUID} 端口 {pair.Key} 实际为 {value.Item1}/{value.Item2}，期望为 {pair.Value.Item1}/{pair.Value.Item2}。");
                }
            }
            foreach (GraphAuthoringPortId extra in actual.Keys.Except(expected.Keys))
                m_Report.Error(path, "node_port_shape_extra", $"Node {node.GUID} 存在 Capability 未声明端口 {extra}。");
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
                    declaration.FactProjection?.Kind != PipelineBlackboardFactProjectionKind.ActionWindow)
                    continue;

                string outputPath = $"{path}/output:{declaration.DeclarationId}";
                if (profiles.Count == 0)
                {
                    m_Report.Error(outputPath, "window_projection_action_profile_missing", $"{declaration.BlackboardKey} 没有可解析的 ActionProfile context。");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(declaration.FactProjection.ActionWindowType))
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
            int interruptCount = CountLifecycle(lifecycleTransitions, activation.ActionContext, ActionLifecycleTransitionType.Interrupt);
            int abortCount = CountLifecycle(lifecycleTransitions, activation.ActionContext, ActionLifecycleTransitionType.Abort);
            int completeCount = CountLifecycle(lifecycleTransitions, activation.ActionContext, ActionLifecycleTransitionType.Complete);
            int succeedCount = 0;
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (graph.Nodes[i] is SucceedNode && graph.Nodes[i].ResolvedDisplayName == "Succeed")
                    succeedCount++;
            }

            if (cancelCount != 1 || interruptCount != 1 || abortCount != 1 || completeCount != 1 || succeedCount != 1)
            {
                m_Report.Error(
                    path,
                    "action_exit_lifecycle_incomplete",
                    $"动作状态必须为同一 Action Context 配置唯一 Cancel、Interrupt、Abort、Complete 与 Succeed 出口，当前 Cancel={cancelCount}、Interrupt={interruptCount}、Abort={abortCount}、Complete={completeCount}、Succeed={succeedCount}。");
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

        bool HasDecisionWindowProjection(string windowType, IReadOnlyList<BaseGraph> visibleGraphs)
        {
            HashSet<string> visibleOwnerIds = visibleGraphs
                .OfType<BaseTree>()
                .Select(value => value.GraphAuthoringId)
                .ToHashSet(StringComparer.Ordinal);
            var errors = new List<string>();
            CharacterAuthoringTopologyProjection projection = CharacterAuthoringTopologyProjection.Build(m_RootTree, errors);
            for (int timelineIndex = 0; timelineIndex < projection.Timelines.Count; timelineIndex++)
            {
                TimelineData timeline = projection.Timelines[timelineIndex].Timeline;
                foreach (TreeClip clip in timeline.Tracks.OfType<TreeTrack>().SelectMany(value => value.Clips).OfType<TreeClip>())
                {
                    if (clip.ExecutionPhase != TimelineTreeExecutionPhase.Decision || clip.ResolvedTree == null)
                        continue;
                    foreach (ExposedPropertyNode setter in clip.ResolvedTree.Nodes.OfType<ExposedPropertyNode>())
                    {
                        if (setter.NodeType != ExposedPropertyNodeType.Set || !setter.BlackboardVariable.IsValid)
                            continue;
                        if (!m_Declarations.TryGetValue(setter.BlackboardVariable.DeclarationId, out BaseExposedProperty declaration))
                            continue;
                        if (visibleOwnerIds.Contains(declaration.DeclarationOwnerId) &&
                            declaration.FactProjection?.Kind == PipelineBlackboardFactProjectionKind.ActionWindow &&
                            string.Equals(declaration.FactProjection.ActionWindowType, windowType, StringComparison.Ordinal))
                            return true;
                    }
                }
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

    }

    public sealed class AgentAIControllerValidator
    {
        readonly BtsmtlGraphAuthoringCapabilities m_Catalog =
            new BtsmtlGraphAuthoringCapabilities();

        public AgentCompileReport Validate(AIControllerDefinition definition, bool validateCompiler = true)
        {
            var report = new AgentCompileReport
            {
                success = true,
                domain = AgentAuthoringSchema.AIControllerDomain,
                rootIdentity = definition ? definition.ControllerId : string.Empty
            };
            if (!definition)
            {
                report.Error("definition", "ai_definition_missing", "AIControllerDefinition 缺失。");
                return report;
            }

            var errors = new List<string>();
            if (!definition.CollectConfigurationErrors(errors))
            {
                for (int i = 0; i < errors.Count; i++)
                    report.Error("definition", "ai_definition_invalid", errors[i]);
                report.metrics.semanticInvalidCount += errors.Count;
                return report;
            }

            if (definition.RootTreeAsset.Tree is not AIControllerTree root)
            {
                report.Error("root", "ai_root_tree_invalid", "RootTree 不是 AIControllerTree。");
                return report;
            }
            root.RebindReadOnlyViewReferences();
            if (root.AuthoringRole != GraphAuthoringRole.AIController)
                report.Error("root", "ai_graph_role_invalid", $"AI root graph role 无效：{root.AuthoringRole}");

            var declarationIds = new HashSet<string>(StringComparer.Ordinal);
            var declarationKeys = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < root.ExposedProperties.Count; i++)
            {
                BaseExposedProperty declaration = root.ExposedProperties[i];
                string path = $"root.blackboard[{i}]";
                if (declaration == null || !declarationIds.Add(declaration.DeclarationId) || !declarationKeys.Add(declaration.BlackboardKey))
                {
                    report.Error(path, "ai_blackboard_identity_invalid", "AI Blackboard declaration 缺失或 identity/key 重复。");
                    continue;
                }
                if (declaration.BlackboardScope != PipelineBlackboardVariableScope.AIController &&
                    declaration.BlackboardScope != PipelineBlackboardVariableScope.AITick &&
                    declaration.BlackboardScope != PipelineBlackboardVariableScope.Graph)
                {
                    report.Error(path, "ai_blackboard_scope_invalid", $"AI Blackboard scope 不允许：{declaration.BlackboardScope}");
                }
                if (declaration.BlackboardLifetime != PipelineBlackboardVariablePolicy.DefaultLifetime(declaration.BlackboardScope))
                {
                    report.Error(path, "ai_blackboard_lifetime_invalid", "AI Blackboard lifetime 与 scope 不匹配。");
                }
                if (declaration.InputBinding != null || declaration.FactProjection != null)
                {
                    report.Error(path, "ai_blackboard_character_payload_forbidden", "AI Blackboard 不允许 Character Input Binding 或 Fact Projection。");
                }
            }

            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < root.Nodes.Count; i++)
            {
                BaseNode node = root.Nodes[i];
                string path = $"root.nodes[{i}]";
                if (node == null || !nodeIds.Add(node.GUID))
                {
                    report.Error(path, "ai_node_identity_invalid", "AI node 缺失或 identity 重复。");
                    continue;
                }
                if (!m_Catalog.IsNodeTypeAllowed(node.GetType(), AgentAuthoringSchema.AIControllerDomain))
                {
                    report.Error(path, "ai_node_capability_forbidden", $"AI Graph 禁止节点：{node.GetType().FullName}");
                }
            }

            if (!validateCompiler)
            {
                report.Warning(
                    "compiler",
                    "ai_intent_compile_deferred",
                    "受控 Character Program 已过期；AI authoring 已验证，AIIntentProgram 保持 stale，等待 Character Program 重新发布后再编译。");
                report.metrics.semanticValidCount++;
            }
            else try
            {
                AIIntentProgramBuildService.Validate(definition);
                report.metrics.compileSuccessCount++;
                report.metrics.semanticValidCount++;
            }
            catch (Exception exception)
            {
                report.Error("compiler", "ai_intent_compile_validation_failed", exception.Message);
                report.metrics.compileFailureCount++;
                report.metrics.semanticInvalidCount++;
            }
            report.success = !report.HasErrors();
            return report;
        }
    }
}
