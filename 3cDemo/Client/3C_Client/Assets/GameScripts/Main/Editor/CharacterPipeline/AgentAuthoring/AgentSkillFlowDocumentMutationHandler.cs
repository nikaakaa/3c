using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using Newtonsoft.Json.Linq;
using ParadoxNotion;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonCharacter.Pipeline.Motion.RootMotion;
using ThirdPersonGameplay.Effects;
using ThirdPersonGameplay.Tags;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    internal sealed class AgentSkillFlowDocumentMutationHandler : IAgentMutationHandler
    {
        public bool Preflight(AgentMutationSession session, AgentMutation command)
        {
            if (command is not AgentSetSkillFlowDocumentMutation set)
                throw new InvalidOperationException($"Unsupported Skill Flow command: {command.Kind}");
            if (!session.Definition)
            {
                session.Report.Error(command.Path, "skill_flow_definition_missing", "Skill Flow Document mutation只能作用于CharacterController。");
                return false;
            }
            bool valid = AgentSkillFlowDocumentMapper.Validate(set.Document, session.Report);
            valid &= ValidateAssets(session, set.Document, command.Path);
            if (valid)
            {
                try
                {
                    AgentSkillFlowAssetPaths.PlannedRoots(session.Definition, set.Document);
                }
                catch (InvalidOperationException exception)
                {
                    session.Report.Error(command.Path, "skill_root_asset_path_conflict", exception.Message);
                    valid = false;
                }
            }
            if (valid)
                session.AddPlanned(command, null, "Skill Flow Document", $"graphs={set.Document.graphs.Count}; macros={set.Document.macros.Count}; timelines={set.Document.timelines.Count}");
            return valid;
        }

        public void Apply(AgentMutationSession session, AgentMutation command)
        {
            AgentSetSkillFlowDocumentMutation set = command as AgentSetSkillFlowDocumentMutation ??
                throw new InvalidOperationException($"Unsupported Skill Flow command: {command.Kind}");
            new AgentSkillFlowDocumentApplier().Apply(session, set.Document);
            session.AddAppliedAuthoring(command, session.Definition, session.Definition, "Skill Flow Document", "native Skill Graph closure");
        }

        static bool ValidateAssets(AgentMutationSession session, AgentPackageSkillFlowDocument document, string path)
        {
            bool valid = true;
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ?? new List<AgentPackageSkillFlowGraphFile>())
            {
                if (graph?.asset != null && !string.IsNullOrEmpty(graph.asset.localId) && !graph.asset.localId.StartsWith("local:", StringComparison.Ordinal))
                {
                    session.Report.Error(path + ".graphs[" + graph.id + "].asset", "skill_graph_asset_invalid", "Skill Graph计划资产identity无效。");
                    valid = false;
                }
                if (graph?.asset != null && string.IsNullOrEmpty(graph.asset.localId) &&
                    !ResolveAsset<FlowGraph>(graph.asset, out _))
                {
                    session.Report.Error(path + ".graphs[" + graph.id + "].asset", "skill_graph_asset_unresolved", "Skill Graph asset引用无法解析。");
                    valid = false;
                }
                if (graph?.asset != null && string.IsNullOrEmpty(graph.asset.localId) &&
                    ResolveAsset<FlowGraph>(graph.asset, out FlowGraph graphAsset))
                {
                    bool mainOwner = graph.ownership == AgentGraphOwnership.SharedAsset.ToString() ||
                        graph.ownership == AgentGraphOwnership.RootAsset.ToString();
                    bool main = AssetDatabase.IsMainAsset(graphAsset);
                    if (mainOwner != main)
                    {
                        session.Report.Error(path + ".graphs[" + graph.id + "].ownership", "skill_graph_asset_ownership_invalid", "Skill Graph ownership与正式资产类型不一致。");
                        valid = false;
                    }
                    if (!mainOwner &&
                        AssetDatabase.GetAssetPath(graphAsset) != OwnerPath(document, graph.owner?.graphId))
                    {
                        session.Report.Error(path + ".graphs[" + graph.id + "].owner", "skill_graph_owner_file_invalid", "私有Skill Graph必须保存在调用方所属的技能文件中。");
                        valid = false;
                    }
                    valid &= ValidateExistingGraphIdentity(graph, graphAsset, path, session.Report);
                }
                foreach (AgentPackageSkillFlowNode node in graph?.nodes ?? new List<AgentPackageSkillFlowNode>())
                {
                    JObject properties = node?.properties;
                    if (node?.capability == "character-action-request" &&
                        !session.Resolver.TryResolveActionRequest(properties?.Value<string>("inputId"), out _))
                    {
                        session.Report.Error(path + ".graphs[" + graph.id + "].nodes[" + node.id + "].properties.inputId",
                            "skill_action_request_unresolved", "Skill Action Request节点的inputId无法解析。");
                        valid = false;
                    }
                    if ((node?.capability == "character-input-bool" ||
                         node?.capability == "character-input-float" ||
                         node?.capability == "character-input-vector2" ||
                         node?.capability == "character-input-vector2-magnitude") &&
                        !session.Resolver.TryResolveInputValue(properties?.Value<string>("inputId"), out _))
                    {
                        session.Report.Error(path + ".graphs[" + graph.id + "].nodes[" + node.id + "].properties.inputId",
                            "skill_input_value_unresolved", "Skill Input节点的inputId无法解析。");
                        valid = false;
                    }
                    if (properties?["actionMotionCurve"] != null &&
                        (properties["actionMotionCurve"] is not JObject ||
                        !ResolveAsset<RootMotionCurveAsset>(
                            properties["actionMotionCurve"].ToObject<AgentPackageObjectReference>(),
                            out _)))
                    {
                        session.Report.Error(path + ".graphs[" + graph.id + "].nodes[" + node.id + "].properties.actionMotionCurve",
                            "skill_root_motion_curve_unresolved", "Skill Locomotion节点的ActionMotionCurve引用无法解析。");
                        valid = false;
                    }
                    if (properties?["factContext"] != null &&
                        (properties["factContext"] is not JObject ||
                        !ResolveAsset<UnityEngine.Object>(
                            properties["factContext"].ToObject<AgentPackageObjectReference>(),
                            out _)))
                    {
                        session.Report.Error(path + ".graphs[" + graph.id + "].nodes[" + node.id + "].properties.factContext",
                            "skill_fact_context_unresolved", "Skill Blackboard factContext引用无法解析。");
                        valid = false;
                    }
                    if (node?.capability == "can-activate-action" &&
                        (properties?["actionProfile"] is not JObject ||
                         !session.Resolver.TryResolveActionProfile(
                             (properties["actionProfile"] as JObject)?.Value<string>("id"),
                             out _)))
                    {
                        session.Report.Error(path + ".graphs[" + graph.id + "].nodes[" + node.id + "].properties.actionProfile",
                            "skill_action_profile_unresolved", "Skill CanActivate节点的ActionProfile引用无法解析。");
                        valid = false;
                    }
                }
                foreach (AgentPackageSkillBlackboardDeclaration declaration in graph?.blackboardDeclarations ??
                         new List<AgentPackageSkillBlackboardDeclaration>())
                    if (declaration?.inputBinding != null &&
                        !session.Resolver.TryResolvePortableInputValue(
                            declaration.inputBinding.inputValueId,
                            ProgramInputValueKind.ActionTargetSnapshot))
                    {
                        session.Report.Error(path + ".graphs[" + graph.id + "].blackboardDeclarations[" + declaration.id + "].inputBinding",
                            "skill_input_binding_unresolved", "Skill Blackboard inputBinding的inputValueId无法解析。");
                        valid = false;
                    }
            }
            foreach (AgentPackageSkillTimelineFile timeline in document.timelines ?? new List<AgentPackageSkillTimelineFile>())
            {
                if (timeline?.asset != null && string.IsNullOrEmpty(timeline.asset.localId))
                {
                    if (!ResolveAsset<TimelineAsset>(timeline.asset, out TimelineAsset timelineAsset))
                    {
                        session.Report.Error(path + ".timelines[" + timeline.id + "].asset", "skill_timeline_asset_unresolved", "Skill Timeline asset引用无法解析。");
                        valid = false;
                        continue;
                    }
                    bool shared = timeline.ownership == BtsmtlSkillTimelineOwnership.Shared.ToString();
                    string ownerPath = OwnerPath(document, timeline.ownerGraphId);
                    if (shared != AssetDatabase.IsMainAsset(timelineAsset) ||
                        !shared && (!AssetDatabase.IsSubAsset(timelineAsset) ||
                                    AssetDatabase.GetAssetPath(timelineAsset) != ownerPath))
                    {
                        session.Report.Error(path + ".timelines[" + timeline.id + "].ownership", "skill_timeline_asset_ownership_invalid", "Skill Timeline ownership与正式资产类型不一致。");
                        valid = false;
                    }
                }
            }
            foreach (AgentPackageSkillTimelineFile timeline in document.timelines ?? new List<AgentPackageSkillTimelineFile>())
                foreach (AgentPackageSkillTimelineTrack track in timeline?.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                    foreach (AgentPackageSkillTimelineClip clip in track?.clips ?? new List<AgentPackageSkillTimelineClip>())
                        if (clip?.kind == TimelineContractKinds.AnimationClip &&
                            !ResolveAsset<UnityEngine.AnimationClip>(clip.animationClip, out _))
                        {
                            session.Report.Error(path + ".timelines[" + timeline.id + "].tracks[" + track.id + "].clips[" + clip.id + "].animationClip",
                                "skill_animation_clip_unresolved", "Skill Timeline AnimationClip引用无法解析。");
                            valid = false;
                        }
            foreach (AgentSnapshotSkillDefinition skill in document.skills ?? new List<AgentSnapshotSkillDefinition>())
            {
                if (skill == null)
                    continue;
                if (!session.Resolver.TryResolveActionProfile(skill.actionProfileId, out _))
                {
                    session.Report.Error(path + ".skills[" + skill.skillId + "].actionProfileId", "skill_action_profile_unresolved", "Skill ActionProfile无法解析。");
                    valid = false;
                }
                if (!session.Resolver.TryResolveActionContext(new AgentAssetReference(
                        skill.actionContext,
                        skill.actionContextAssetPath,
                        skill.actionContextAssetGuid), out _))
                {
                    session.Report.Error(path + ".skills[" + skill.skillId + "].actionContext", "skill_action_context_unresolved", "Skill ActionContext无法解析。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateExistingGraphIdentity(
            AgentPackageSkillFlowGraphFile target,
            FlowGraph asset,
            string path,
            AgentCompileReport report)
        {
            bool valid = true;
            var nodes = asset.allNodes.OfType<FlowNode>()
                .ToDictionary(value => value.UID, StringComparer.Ordinal);
            var edges = asset.allNodes.OfType<FlowNode>()
                .SelectMany(value => value.outConnections.OfType<BinderConnection>())
                .ToDictionary(value => value.UID, StringComparer.Ordinal);
            foreach (AgentPackageSkillGraphAnchor anchor in target.anchors ?? new List<AgentPackageSkillGraphAnchor>())
            {
                if (!string.IsNullOrEmpty(anchor?.nodeId) &&
                    !anchor.nodeId.StartsWith("local:", StringComparison.Ordinal) &&
                    !nodes.ContainsKey(anchor.nodeId))
                {
                    report.Error(path + ".graphs[" + target.id + "].anchors[" + anchor.kind + "]",
                        "skill_anchor_identity_missing",
                        "Document anchor identity不存在于当前正式Skill Graph。");
                    valid = false;
                }
            }
            foreach (AgentPackageSkillFlowNode node in target.nodes ?? new List<AgentPackageSkillFlowNode>())
            {
                if (!string.IsNullOrEmpty(node?.id) &&
                    !node.id.StartsWith("local:", StringComparison.Ordinal) &&
                    !nodes.ContainsKey(node.id))
                {
                    report.Error(path + ".graphs[" + target.id + "].nodes[" + node.id + "]",
                        "skill_node_identity_missing",
                        "Document Node identity不存在于当前正式Skill Graph。");
                    valid = false;
                }
            }
            foreach (AgentPackageSkillFlowEdge edge in target.edges ?? new List<AgentPackageSkillFlowEdge>())
            {
                if (!string.IsNullOrEmpty(edge?.id) &&
                    !edge.id.StartsWith("local:", StringComparison.Ordinal) &&
                    !edges.ContainsKey(edge.id))
                {
                    report.Error(path + ".graphs[" + target.id + "].edges[" + edge.id + "]",
                        "skill_edge_identity_missing",
                        "Document Edge identity不存在于当前正式Skill Graph。");
                    valid = false;
                }
            }
            return valid;
        }

        static string OwnerPath(AgentPackageSkillFlowDocument document, string graphId)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            while (!string.IsNullOrEmpty(graphId) && visited.Add(graphId))
            {
                AgentPackageSkillFlowGraphFile owner = document.graphs.FirstOrDefault(value => value?.id == graphId);
                if (owner == null)
                    return string.Empty;
                if (ResolveAsset<FlowGraph>(owner.asset, out FlowGraph asset))
                {
                    string path = AssetDatabase.GetAssetPath(asset);
                    return AssetDatabase.LoadMainAssetAtPath(path) is IBtsmtlSkillFlowGraph ? path : string.Empty;
                }
                graphId = owner.owner?.graphId;
            }
            return string.Empty;
        }

        static bool ResolveAsset<T>(AgentPackageObjectReference reference, out T asset)
            where T : UnityEngine.Object
        {
            asset = null;
            if (reference == null || !string.IsNullOrEmpty(reference.localId))
                return false;
            string path = AssetDatabase.GUIDToAssetPath(reference.assetGuid);
            if (string.IsNullOrEmpty(path) || !string.Equals(path, reference.assetPath, StringComparison.Ordinal))
                return false;
            asset = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<T>()
                .FirstOrDefault(value =>
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out _, out long fileId) &&
                    fileId == reference.localFileId);
            return asset;
        }
    }

    internal sealed class AgentSkillFlowDocumentApplier
    {
        readonly Dictionary<string, FlowGraph> m_Graphs = new Dictionary<string, FlowGraph>(StringComparer.Ordinal);
        readonly Dictionary<string, FlowGraph> m_LocalGraphs = new Dictionary<string, FlowGraph>(StringComparer.Ordinal);
        readonly Dictionary<string, TimelineAsset> m_Timelines = new Dictionary<string, TimelineAsset>(StringComparer.Ordinal);
        readonly Dictionary<string, TimelineAsset> m_LocalTimelines = new Dictionary<string, TimelineAsset>(StringComparer.Ordinal);
        readonly Dictionary<string, FlowNode> m_LocalNodes = new Dictionary<string, FlowNode>(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_LocalDeclarations = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_LocalPorts = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_LocalParameters = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, Clip> m_LocalClips = new Dictionary<string, Clip>(StringComparer.Ordinal);
        readonly Dictionary<string, AgentPackageSkillFlowGraphFile> m_TargetGraphs = new Dictionary<string, AgentPackageSkillFlowGraphFile>(StringComparer.Ordinal);
        readonly Dictionary<string, AgentPackageSkillTimelineFile> m_TargetTimelines = new Dictionary<string, AgentPackageSkillTimelineFile>(StringComparer.Ordinal);
        readonly Dictionary<string, AgentPackageSkillFlowGraphLayoutFile> m_TargetLayouts = new Dictionary<string, AgentPackageSkillFlowGraphLayoutFile>(StringComparer.Ordinal);
        readonly AgentSkillFlowDocumentRuntimeIndex m_Current = new AgentSkillFlowDocumentRuntimeIndex();
        readonly IBtsmtlSkillFlowMutationDispatcher m_Dispatcher = new BtsmtlSkillFlowMutationDispatcher();
        AgentMutationSession m_Session;
        AgentPackageSkillFlowDocument m_Document;

        public void Apply(AgentMutationSession session, AgentPackageSkillFlowDocument document)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_Document = document ?? throw new ArgumentNullException(nameof(document));
            m_Current.Build(session.Definition);
            m_TargetGraphs.Clear();
            m_TargetTimelines.Clear();
            m_TargetLayouts.Clear();
            m_LocalDeclarations.Clear();
            m_LocalPorts.Clear();
            m_LocalParameters.Clear();
            m_LocalClips.Clear();
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ?? new List<AgentPackageSkillFlowGraphFile>())
                m_TargetGraphs.Add(graph.id, graph);
            foreach (AgentPackageSkillTimelineFile timeline in document.timelines ?? new List<AgentPackageSkillTimelineFile>())
                m_TargetTimelines.Add(timeline.id, timeline);
            foreach (AgentPackageSkillFlowGraphLayoutFile layout in document.layouts ?? new List<AgentPackageSkillFlowGraphLayoutFile>())
                m_TargetLayouts.Add(layout.graphId, layout);
            ResolveGraphs();
            ResolveTimelines();
            RehomeTimelineBodyGraphs();
            SyncBlackboards();
            SyncMacros();
            SyncNodes();
            SyncTimelines();
            SyncSkillDefinitions();
            DeleteRemovedTimelines();
            DeleteRemovedGraphs();
            foreach (BtsmtlSkillFlowGraph root in m_Session.Definition.SkillGraphs)
                BtsmtlSkillGraphClosure.Validate(root, true);
        }

        void ResolveGraphs()
        {
            var pending = m_Document.graphs
                .OrderBy(value => value.id, StringComparer.Ordinal)
                .ToList();
            while (pending.Count > 0)
            {
                bool progressed = false;
                foreach (AgentPackageSkillFlowGraphFile target in pending.ToArray())
                {
                    if (!string.IsNullOrEmpty(target.owner?.graphId) &&
                        ResolveGraph(target.owner.graphId) == null)
                        continue;
                    ResolveGraphTarget(target);
                    pending.Remove(target);
                    progressed = true;
                }
                if (!progressed)
                    throw new InvalidOperationException("Skill Graph owner闭包无法按依赖顺序解析。");
            }
        }

        void ResolveGraphTarget(AgentPackageSkillFlowGraphFile target)
        {
            if (m_Current.Graphs.TryGetValue(target.id, out FlowGraph existing))
            {
                if (!MatchesGraphType(existing, target.role))
                    throw new InvalidOperationException($"Skill Graph '{target.id}' role与正式对象类型不一致。");
                if (target.asset != null &&
                    ResolveObject<FlowGraph>(target.asset) is FlowGraph declared &&
                    declared != existing)
                    throw new InvalidOperationException($"Skill Graph '{target.id}'的asset identity发生变化。");
                m_Graphs[target.id] = existing;
                m_Session.Touch(existing);
                return;
            }
            if (target.ownership == AgentGraphOwnership.SharedAsset.ToString())
            {
                if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                {
                    FlowGraph shared = ResolveObject<FlowGraph>(target.asset);
                    if (shared == null || !MatchesGraphType(shared, target.role))
                        throw new InvalidOperationException($"Skill Graph '{target.id}' shared asset无法解析。");
                    m_Graphs[target.id] = shared;
                    m_Session.Touch(shared);
                    return;
                }
                FlowGraph createdShared = CreateGraph(target);
                m_Graphs[target.id] = createdShared;
                m_LocalGraphs[target.id] = createdShared;
                return;
            }
            if (!target.id.StartsWith("local:", StringComparison.Ordinal))
            {
                FlowGraph existingAsset = ResolveObject<FlowGraph>(target.asset);
                if (existingAsset == null || !MatchesGraphType(existingAsset, target.role))
                    throw new InvalidOperationException($"Skill Graph '{target.id}'正式资产无法解析。");
                m_Graphs[target.id] = existingAsset;
                m_Session.Touch(existingAsset);
                return;
            }
            FlowGraph created = CreateGraph(target);
            m_Graphs[target.id] = created;
            m_LocalGraphs[target.id] = created;
        }

        void ResolveTimelines()
        {
            foreach (AgentPackageSkillTimelineFile target in m_Document.timelines.OrderBy(value => value.id, StringComparer.Ordinal))
            {
                TimelineAsset existing = ResolveExistingTimeline(target.id);
                if (existing)
                {
                    m_Timelines[target.id] = existing;
                    m_Session.Touch(existing);
                    continue;
                }
                TimelineAsset asset = target.asset != null ? ResolveObject<TimelineAsset>(target.asset) : null;
                if (!asset)
                {
                    asset = ScriptableObject.CreateInstance<TimelineAsset>();
                    asset.name = string.IsNullOrWhiteSpace(target.name) ? "Skill Timeline" : target.name;
                    UnityEngine.Object owner = ResolveGraph(target.ownerGraphId);
                    string ownerPath = AssetDatabase.GetAssetPath(owner);
                    if (string.IsNullOrEmpty(ownerPath) ||
                        AssetDatabase.LoadMainAssetAtPath(ownerPath) is not IBtsmtlSkillFlowGraph)
                        throw new InvalidOperationException($"Skill Timeline '{target.id}' owner资产无法解析。");
                    AssetDatabase.AddObjectToAsset(asset, ownerPath);
                    Undo.RegisterCreatedObjectUndo(asset, "创建技能Timeline");
                    asset.SetData(TimelineData.CreateDefault(asset.name));
                    if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                        asset.Data.ConfigureAuthoringIdentity(target.id);
                }
                m_Timelines[target.id] = asset;
                if (target.id.StartsWith("local:", StringComparison.Ordinal))
                    m_LocalTimelines[target.id] = asset;
                m_Session.Touch(asset);
            }
        }

        void RehomeTimelineBodyGraphs()
        {
            foreach (AgentPackageSkillFlowGraphFile target in m_Document.graphs)
            {
                if (target.role != BtsmtlSkillFlowGraphRole.TimelineBody.ToString() ||
                    target.owner?.kind != "timeline-clip" ||
                    !m_Graphs.TryGetValue(target.id, out FlowGraph graph) ||
                    !m_Timelines.TryGetValue(target.owner.timelineId, out TimelineAsset timeline))
                    continue;
                string timelinePath = AssetDatabase.GetAssetPath(timeline);
                if (string.IsNullOrEmpty(timelinePath))
                    throw new InvalidOperationException($"TimelineBody '{target.id}'的Timeline owner没有正式资产路径。");
                if (AssetDatabase.GetAssetPath(graph) == timelinePath)
                    continue;
                if (AssetDatabase.IsMainAsset(graph))
                    continue;
                if (AssetDatabase.IsSubAsset(graph))
                    AssetDatabase.RemoveObjectFromAsset(graph);
                AssetDatabase.AddObjectToAsset(graph, timelinePath);
            }
        }

        void SyncBlackboards()
        {
            foreach (AgentPackageSkillFlowGraphFile target in m_Document.graphs)
            {
                FlowGraph graph = ResolveGraph(target.id);
                if (graph is not IBtsmtlSkillFlowGraph skill)
                    continue;
                m_Dispatcher.Apply(
                    graph,
                    "应用技能黑板",
                    () => SyncBlackboard(graph, skill, target),
                    false,
                    false);
            }
        }

        void SyncBlackboard(
            FlowGraph graph,
            IBtsmtlSkillFlowGraph skill,
            AgentPackageSkillFlowGraphFile target)
        {
                AgentPackageSkillBlackboardDeclaration[] declarations = target.blackboardDeclarations?.ToArray() ?? Array.Empty<AgentPackageSkillBlackboardDeclaration>();
                var variables = graph.GetGraphSource().localBlackboard.variables;
                var targetIds = declarations.Select(value => value.id).ToHashSet(StringComparer.Ordinal);
                foreach (string key in variables.Keys.ToArray())
                {
                    if (!targetIds.Contains(variables[key]?.ID ?? string.Empty))
                        variables.Remove(key);
                }
                var metadata = new List<BtsmtlSkillBlackboardDeclaration>();
                foreach (AgentPackageSkillBlackboardDeclaration declaration in declarations)
                {
                    if (!AgentSkillFlowAuthoringCapabilities.TryResolveValueType(declaration.valueType, out Type type))
                        throw new InvalidOperationException($"Skill Blackboard '{declaration.id}'类型无法解析。");
                    Variable variable = variables.Values.SingleOrDefault(value => value != null && value.ID == declaration.id);
                    if (variable != null && variable.varType != type)
                        throw new InvalidOperationException($"Skill Blackboard '{declaration.id}'不能原位改变类型。");
                    if (variable == null)
                    {
                        if (!declaration.id.StartsWith("local:", StringComparison.Ordinal))
                            throw new InvalidOperationException($"Skill Blackboard '{declaration.id}'无法创建新的formal identity。");
                        string variableId = declaration.id.StartsWith("local:", StringComparison.Ordinal)
                            ? Guid.NewGuid().ToString("N")
                            : declaration.id;
                        variable = (Variable)Activator.CreateInstance(typeof(Variable<>).MakeGenericType(type), declaration.key, variableId);
                        variables.Add(declaration.key, variable);
                    }
                    else if (!string.Equals(variable.name, declaration.key, StringComparison.Ordinal))
                    {
                        if (variables.ContainsKey(declaration.key) && variables[declaration.key] != variable)
                            throw new InvalidOperationException($"Skill Blackboard key重复：{declaration.key}");
                        variables.Remove(variable.name);
                        variable.name = declaration.key;
                        variables[declaration.key] = variable;
                    }
                    variable.SetValueBoxed(ParseValue(declaration.defaultValue, type));
                    AgentSnapshotBlackboardInputBinding inputBinding = declaration.inputBinding;
                    if (inputBinding != null && string.IsNullOrWhiteSpace(inputBinding.inputValueId))
                        inputBinding = null;
                    AgentSnapshotBlackboardFactProjection factProjection = declaration.factProjection;
                    if (factProjection != null &&
                        (string.IsNullOrWhiteSpace(factProjection.kind) ||
                         string.IsNullOrWhiteSpace(factProjection.windowType) ||
                         string.IsNullOrWhiteSpace(factProjection.windowId)))
                        factProjection = null;
                    metadata.Add(new BtsmtlSkillBlackboardDeclaration(
                        variable.ID,
                        Enum.Parse<PipelineBlackboardVariableScope>(declaration.scope, false),
                        Enum.Parse<PipelineBlackboardVariableLifetime>(declaration.lifetime, false),
                        declaration.category,
                        inputBinding == null ? null : new PipelineBlackboardInputBinding(inputBinding.inputValueId),
                        factProjection == null ? null : new PipelineBlackboardFactProjection(
                            Enum.Parse<PipelineBlackboardFactProjectionKind>(factProjection.kind, false),
                            factProjection.windowType,
                            factProjection.windowId,
                            factProjection.digest)));
                    if (declaration.id.StartsWith("local:", StringComparison.Ordinal))
                        m_LocalDeclarations[declaration.id] = variable.ID;
                }
                skill.SetBlackboardDeclarations(metadata);
        }

        void SyncMacros()
        {
            foreach (AgentPackageSkillMacroFile target in m_Document.macros)
            {
                if (ResolveGraph(target.graphId) is not BtsmtlSkillMacroGraph macro)
                    throw new InvalidOperationException($"Skill Macro '{target.id}'没有对应Macro Graph。");
                m_Dispatcher.Apply(
                    macro,
                    "应用技能Macro接口",
                    () => SyncMacro(macro, target),
                    false,
                    false);
            }
        }

        void SyncMacro(BtsmtlSkillMacroGraph macro, AgentPackageSkillMacroFile target)
        {
                var existingInputIds = macro.inputDefinitions
                    .Select(value => value.ID)
                    .ToHashSet(StringComparer.Ordinal);
                var existingOutputIds = macro.outputDefinitions
                    .Select(value => value.ID)
                    .ToHashSet(StringComparer.Ordinal);
                macro.inputDefinitions.Clear();
                foreach (AgentPackageSkillMacroParameter parameter in target.inputs ?? new List<AgentPackageSkillMacroParameter>())
                {
                    if (!parameter.id.StartsWith("local:", StringComparison.Ordinal) &&
                        !existingInputIds.Contains(parameter.id))
                        throw new InvalidOperationException($"Skill Macro input '{parameter.id}'无法创建新的formal identity。");
                    string inputId = parameter.id.StartsWith("local:", StringComparison.Ordinal)
                        ? Guid.NewGuid().ToString("N")
                        : parameter.id;
                    macro.inputDefinitions.Add(new DynamicParameterDefinition(
                        inputId,
                        parameter.name,
                        ResolveMacroType(parameter.valueType)));
                    if (parameter.id.StartsWith("local:", StringComparison.Ordinal))
                        m_LocalParameters[ParameterKey(target.graphId, parameter.id)] = inputId;
                }
                macro.outputDefinitions.Clear();
                foreach (AgentPackageSkillMacroParameter parameter in target.outputs ?? new List<AgentPackageSkillMacroParameter>())
                {
                    if (!parameter.id.StartsWith("local:", StringComparison.Ordinal) &&
                        !existingOutputIds.Contains(parameter.id))
                        throw new InvalidOperationException($"Skill Macro output '{parameter.id}'无法创建新的formal identity。");
                    string outputId = parameter.id.StartsWith("local:", StringComparison.Ordinal)
                        ? Guid.NewGuid().ToString("N")
                        : parameter.id;
                    macro.outputDefinitions.Add(new DynamicParameterDefinition(
                        outputId,
                        parameter.name,
                        ResolveMacroType(parameter.valueType)));
                    if (parameter.id.StartsWith("local:", StringComparison.Ordinal))
                        m_LocalParameters[ParameterKey(target.graphId, parameter.id)] = outputId;
                }
                BtsmtlSkillMacroInterface.Validate(macro);
                foreach (MacroInputNode input in macro.allNodes.OfType<MacroInputNode>())
                    input.GatherPorts();
                foreach (MacroOutputNode output in macro.allNodes.OfType<MacroOutputNode>())
                    output.GatherPorts();
        }

        void SyncNodes()
        {
            foreach (AgentPackageSkillFlowGraphFile target in m_Document.graphs.OrderBy(value => value.id, StringComparer.Ordinal))
            {
                FlowGraph graph = ResolveGraph(target.id);
                if (graph == null)
                    throw new InvalidOperationException($"Skill Graph '{target.id}'无法解析。");
                m_Dispatcher.Apply(graph, "应用技能Graph", () => SyncGraph(graph, target), false, false);
            }
        }

        void SyncGraph(FlowGraph graph, AgentPackageSkillFlowGraphFile target)
        {
                if (target.name != null && !string.Equals(graph.name, target.name, StringComparison.Ordinal))
                    graph.name = target.name;
                foreach (AgentPackageSkillGraphAnchor anchor in target.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                {
                    FlowNode anchorNode = graph.allNodes.OfType<FlowNode>().FirstOrDefault(value => value.UID == anchor.nodeId);
                    if (anchorNode == null)
                        anchorNode = graph.allNodes.OfType<FlowNode>().FirstOrDefault(value =>
                            AgentSkillFlowAuthoringCapabilities.TryGetKind(value, out string kind) &&
                            kind == anchor.kind);
                    if (anchorNode != null &&
                        !anchor.nodeId.StartsWith("local:", StringComparison.Ordinal) &&
                        anchorNode.UID != anchor.nodeId)
                        throw new InvalidOperationException($"Skill Graph anchor '{anchor.nodeId}'的identity与正式节点不一致。");
                    if (anchorNode != null && anchor.nodeId.StartsWith("local:", StringComparison.Ordinal))
                        m_LocalNodes[NodeKey(target.id, anchor.nodeId)] = anchorNode;
                    if (anchorNode is BtsmtlSkillCompositeFlowNode composite)
                        ConfigureSteps(target.id, anchorNode.UID, composite, AgentAuthoringDocumentCodec.ToToken(anchor.steps) as JArray);
                }
                var targetNodeIds = (target.nodes ?? new List<AgentPackageSkillFlowNode>()).Select(value => value.id).ToHashSet(StringComparer.Ordinal);
                foreach (FlowNode existing in graph.allNodes.OfType<FlowNode>().ToArray())
                {
                    if (AgentSkillFlowAuthoringCapabilities.TryGetKind(existing, out string kind) && AgentSkillFlowAuthoringCapabilities.IsAnchor(kind))
                        continue;
                    if (!targetNodeIds.Contains(existing.UID))
                    {
                        RemoveConnections(graph, existing);
                        graph.RemoveNode(existing, false, true);
                    }
                }
                foreach (AgentPackageSkillFlowNode node in target.nodes ?? new List<AgentPackageSkillFlowNode>())
                {
                    FlowNode actual = graph.allNodes.OfType<FlowNode>().FirstOrDefault(value => value.UID == node.id);
                    if (actual == null)
                    {
                        if (!node.id.StartsWith("local:", StringComparison.Ordinal))
                            throw new InvalidOperationException($"Skill Node '{node.id}'无法创建新的formal identity。");
                        Type type = ResolveNodeType(node);
                        actual = (FlowNode)graph.AddNode(type, Vector2.zero);
                        if (!node.id.StartsWith("local:", StringComparison.Ordinal))
                            actual.ConfigureAuthoringIdentity(node.id);
                        if (node.id.StartsWith("local:", StringComparison.Ordinal))
                            m_LocalNodes[NodeKey(target.id, node.id)] = actual;
                    }
                    else if (NodeKind(actual) != node.capability ||
                             node.capability == "exposed-property" &&
                             (node.properties?.Value<string>("accessMode") == "set") !=
                             (actual is BtsmtlSkillBlackboardSetFlowNode))
                    {
                        throw new InvalidOperationException($"Skill Node '{node.id}'不能原位改变Capability或accessMode。");
                    }
                    ConfigureNode(graph, target, node, actual);
                }
                SyncEdges(target, graph);
        }

        static Type ResolveNodeType(AgentPackageSkillFlowNode node)
        {
            if (node.capability == "exposed-property" &&
                node.properties?.Value<string>("accessMode") == "set")
                return typeof(BtsmtlSkillBlackboardSetFlowNode);
            if (AgentSkillFlowAuthoringCapabilities.TryResolveType(node.capability, out Type type))
                return type;
            throw new InvalidOperationException($"Skill Node kind无法解析：{node.capability}");
        }

        static string NodeKind(FlowNode node)
        {
            return AgentSkillFlowAuthoringCapabilities.TryGetKind(node, out string kind) ? kind : string.Empty;
        }

        void ConfigureNode(FlowGraph graph, AgentPackageSkillFlowGraphFile graphFile, AgentPackageSkillFlowNode target, FlowNode node)
        {
            if (target.name != null && !string.Equals(node.name, target.name, StringComparison.Ordinal))
                node.name = target.name;
            JObject properties = target.properties ?? new JObject();
            if (node is BtsmtlSkillLoopFlowNode loop && properties.Value<string>("stopType") != null)
                loop.SetStopType(Enum.Parse<BtsmtlSkillLoopStopType>(properties.Value<string>("stopType"), false));
            if (node is BtsmtlSkillParallelFlowNode parallel && properties.Value<string>("mode") != null)
                parallel.SetMode(Enum.Parse<BtsmtlSkillParallelMode>(properties.Value<string>("mode"), false));
            if (node is BtsmtlSkillStateExitCauseFlowNode cause && properties.Value<string>("cause") != null)
                cause.SetCause(Enum.Parse<BtsmtlSkillStateExitCause>(properties.Value<string>("cause"), false));
            if (node is IBtsmtlSkillInputNode input && properties.Value<string>("inputId") != null)
                SetInput(input, properties.Value<string>("inputId"), properties.Value<string>("providerOwnerId"));
            if (node is BtsmtlSkillGameplayTagFlowNode tag)
                tag.Configure(
                    new GameplayTagId(properties.Value<string>("tagId")),
                    RequiredProviderOwner(properties));
            if (node is BtsmtlSkillMoveFacingAngleFlowNode moveFacing)
                moveFacing.Configure(RequiredProviderOwner(properties));
            if (node is BtsmtlSkillGameplayTagQueryFlowNode tagQuery)
                tagQuery.Configure(
                    ParseGameplayTagQuery(properties["query"]),
                    RequiredProviderOwner(properties));
            if (node is BtsmtlSkillGameplayAttributeFlowNode attribute)
                attribute.Configure(
                    new ThirdPersonGameplay.Attributes.GameplayAttributeId(properties.Value<string>("attributeId")),
                    RequiredProviderOwner(properties));
            if (node is BtsmtlSkillApplyGameplayEffectFlowNode applyEffect)
                applyEffect.Configure(
                    ResolveGameplayEffect(properties["effect"]),
                    ResolveActionContext(properties["actionContext"]),
                    properties.Value<bool>("predicted"),
                    RequiredProviderOwner(properties));
            if (node is BtsmtlSkillRemoveGameplayEffectFlowNode removeEffect)
            {
                ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector selector =
                    Enum.Parse<ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector>(properties.Value<string>("selector"), false);
                removeEffect.Configure(
                    selector,
                    properties.Value<ulong>("handle"),
                    selector == ThirdPersonGameplay.Effects.GameplayEffectRemoveSelector.EffectId
                        ? ResolveGameplayEffect(properties["effect"])
                        : null,
                    ParseGameplayTagQuery(properties["query"]),
                    RequiredProviderOwner(properties));
            }
            if (node is BtsmtlSkillActionContextActiveFlowNode contextActive)
                contextActive.SetActionContext(ResolveActionContext(properties["actionContext"]));
            if (node is BtsmtlSkillActionWindowActiveFlowNode window)
                window.SetWindowType(properties.Value<string>("windowType"));
            if (node is BtsmtlSkillCanActivateActionFlowNode admission)
            {
                ActionProfile profile = ResolveActionProfile(properties["actionProfile"]);
                JObject targetSnapshot = properties["targetSnapshot"] as JObject;
                string declarationId = targetSnapshot?.Value<string>("id");
                if (m_LocalDeclarations.TryGetValue(declarationId ?? string.Empty, out string resolvedDeclarationId))
                    declarationId = resolvedDeclarationId;
                string ownerId = targetSnapshot?.Value<string>("ownerId");
                if (ResolveGraph(ownerId) is IBtsmtlSkillFlowGraph owner)
                    ownerId = owner.AuthoringId;
                admission.Configure(profile, declarationId, ownerId);
            }
            if (node is BtsmtlSkillSubmitActionLifecycleFlowNode lifecycle)
                lifecycle.Configure(
                    ResolveActionContext(properties["actionContext"]),
                    Enum.Parse<ActionLifecycleTransitionType>(properties.Value<string>("transitionType"), false),
                    properties.Value<string>("reason"));
            if (node is BtsmtlSkillBlackboardReadFlowNode<bool> booleanRead)
                booleanRead.SetVariable(BlackboardReference(properties));
            if (node is BtsmtlSkillBlackboardReadFlowNode<float> scalarRead)
                scalarRead.SetVariable(BlackboardReference(properties));
            if (node is BtsmtlSkillBlackboardAccessFlowNode blackboard)
                blackboard.Configure(
                    BlackboardReference(properties),
                    Enum.Parse<BtsmtlSkillBlackboardValueType>(ValueTypeEnum(properties.Value<string>("valueType")), false),
                    ResolveObject<UnityEngine.Object>(properties["factContext"]));
            if (node is BtsmtlSkillStateMachineFlowNode stateMachine)
                stateMachine.SetStateMachine(ResolveGraph(properties.Value<string>("graphId")) as BtsmtlSkillFlowGraph);
            if (node is BtsmtlSkillStateFlowNode state)
                state.SetBody(ResolveGraph(properties.Value<string>("bodyGraphId")) as BtsmtlSkillFlowGraph);
            if (node is BtsmtlSkillTimelineFlowNode timeline)
                timeline.Configure(
                    ResolveTimeline(properties.Value<string>("timelineId")),
                    Enum.Parse<BtsmtlSkillTimelineOwnership>(properties.Value<string>("timelineOwnership"), false),
                    ResolveActionContext(properties["actionContext"]),
                    Enum.Parse<TimelinePlaybackMode>(properties.Value<string>("playbackMode"), false));
            if (node is BtsmtlSkillLocomotionFlowNode locomotion)
                locomotion.Configure(
                    properties.Value<float>("moveSpeed"),
                    Enum.Parse<LocomotionInputMotionDisplacementMode>(properties.Value<string>("displacementMode"), false),
                    ResolveObject<RootMotionCurveAsset>(properties["actionMotionCurve"]),
                    properties.Value<float>("turnSpeedDegrees"),
                    properties.Value<bool>("cameraRelative"),
                    Enum.Parse<LocomotionInputMotionExecutionMode>(properties.Value<string>("executionMode"), false),
                    properties.Value<float>("durationSeconds"));
            if (node is MacroNodeWrapper macro)
                macro.macro = ResolveGraph(properties.Value<string>("graphId")) as BtsmtlSkillMacroGraph;
            if (node is BtsmtlSkillCompositeFlowNode composite)
                ConfigureSteps(graphFile.id, target.id, composite, properties["steps"] as JArray);
            node.position = FindPosition(graphFile.id, target.id);
            node.GatherPorts();
            ApplyValues(node, target.values);
        }

        void ConfigureSteps(string graphId, string nodeId, BtsmtlSkillCompositeFlowNode node, JArray values)
        {
            if (values == null)
                return;
            var existingStepIds = node.Steps.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
            var steps = new List<BtsmtlSkillStepPort>();
            foreach (JObject value in values.OfType<JObject>())
            {
                string stepId = value.Value<string>("id");
                if (stepId.StartsWith("local:", StringComparison.Ordinal))
                {
                    string resolvedStepId = Guid.NewGuid().ToString("N");
                    m_LocalPorts[StepKey(graphId, nodeId, stepId)] = resolvedStepId;
                    stepId = resolvedStepId;
                }
                else if (!existingStepIds.Contains(stepId))
                    throw new InvalidOperationException($"Skill Step '{stepId}'无法创建新的formal identity。");
                string conditionId = value.Value<string>("conditionGraphId");
                BtsmtlSkillFlowGraph condition = string.IsNullOrEmpty(conditionId)
                    ? null
                    : ResolveGraph(conditionId) as BtsmtlSkillFlowGraph;
                steps.Add(new BtsmtlSkillStepPort(
                    stepId,
                    value.Value<string>("name") ?? string.Empty));
                steps[steps.Count - 1].Configure(
                    value.Value<string>("name") ?? string.Empty,
                    condition,
                    value.Value<int>("priority"),
                    Enum.Parse<ProgramAbortPolicy>(value.Value<string>("abortPolicy"), false));
            }
            RemoveConnectionsForRemovedSteps(node, steps);
            node.SetSteps(steps);
        }

        void SyncEdges(AgentPackageSkillFlowGraphFile target, FlowGraph graph)
        {
            var targetIds = (target.edges ?? new List<AgentPackageSkillFlowEdge>()).Select(value => value.id).ToHashSet(StringComparer.Ordinal);
            foreach (BinderConnection existing in graph.allNodes.OfType<FlowNode>().SelectMany(value => value.outConnections.OfType<BinderConnection>()).ToArray())
                if (!targetIds.Contains(existing.UID))
                    graph.RemoveConnection(existing, false);
            foreach (AgentPackageSkillFlowEdge edge in target.edges ?? new List<AgentPackageSkillFlowEdge>())
            {
                Port source = ResolvePort(graph, target.id, edge.from, false);
                Port destination = ResolvePort(graph, target.id, edge.to, true);
                if (source == null || destination == null || source.type != destination.type || source.IsFlowPort() != destination.IsFlowPort())
                    throw new InvalidOperationException($"Skill Edge '{edge.id}'端口无法解析或类型不一致。");
                BinderConnection existing = graph.allNodes.OfType<FlowNode>()
                    .SelectMany(value => value.outConnections.OfType<BinderConnection>())
                    .FirstOrDefault(value => value.UID == edge.id);
                if (existing == null)
                {
                    if (!edge.id.StartsWith("local:", StringComparison.Ordinal))
                        throw new InvalidOperationException($"Skill Edge '{edge.id}'无法创建新的formal identity。");
                    BinderConnection created = graph.CreatePortConnection(source, destination);
                    if (created == null)
                        throw new InvalidOperationException($"Skill Edge '{edge.id}'创建失败。");
                    if (!edge.id.StartsWith("local:", StringComparison.Ordinal))
                        created.ConfigureAuthoringIdentity(edge.id);
                    continue;
                }
                if (existing.sourcePort != source)
                    existing.SetSourcePort(source);
                if (existing.targetPort != destination)
                    existing.SetTargetPort(destination);
            }
        }

        void SyncTimelines()
        {
            foreach (AgentPackageSkillTimelineFile target in m_Document.timelines)
            {
                TimelineAsset asset = ResolveTimeline(target.id);
                TimelineData timeline = asset.Data;
                timeline.Name = target.name ?? timeline.Name;
                SyncTimelineBindings(timeline, target.externalBindings);
                TimelineContractCatalog catalog = TimelineTreeContractComposition.Create();
                var targetTrackIds = (target.tracks ?? new List<AgentPackageSkillTimelineTrack>()).Select(value => value.id).ToHashSet(StringComparer.Ordinal);
                foreach (Track existing in timeline.Tracks.ToArray())
                    if (!targetTrackIds.Contains(existing.AuthoringId))
                        timeline.RemoveTrack(existing);
                var resolvedTracks = new List<Track>();
                foreach (AgentPackageSkillTimelineTrack targetTrack in target.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                {
                    Track track = timeline.Tracks.FirstOrDefault(value => value.AuthoringId == targetTrack.id);
                    if (track == null)
                    {
                        if (!targetTrack.id.StartsWith("local:", StringComparison.Ordinal))
                            throw new InvalidOperationException($"Timeline Track '{targetTrack.id}'无法创建新的formal identity。");
                        Type type = TrackType(targetTrack.kind);
                        timeline.AddTrack(type, TimelineTreeContractComposition.Create());
                        track = timeline.Tracks.Last();
                        if (!targetTrack.id.StartsWith("local:", StringComparison.Ordinal))
                            track.ConfigureAuthoringIdentity(targetTrack.id);
                    }
                    else if (track.ContractKind != targetTrack.kind)
                        throw new InvalidOperationException($"Timeline Track '{targetTrack.id}'不能原位改变kind。");
                    track.Name = targetTrack.name ?? track.Name;
                    if (track is AnimationTrack animation && !string.IsNullOrEmpty(targetTrack.animationChannelId))
                        animation.SetAnimationChannelId(new AnimationChannelId(targetTrack.animationChannelId));
                    SyncTimelineClips(timeline, catalog, track, targetTrack.clips);
                    resolvedTracks.Add(track);
                }
                timeline.Tracks.Clear();
                timeline.Tracks.AddRange(resolvedTracks);
                SyncTimelineSections(timeline, target.sections);
                SyncTimelineMotionWarpSources(timeline, target.tracks);
                timeline.Init();
                var errors = new List<string>();
                if (!asset.ValidateContent(catalog, errors))
                    throw new InvalidOperationException($"Skill Timeline '{target.id}'内容无效：{string.Join(" ", errors)}");
                m_Session.Touch(asset);
            }
        }

        void SyncTimelineBindings(
            TimelineData timeline,
            IReadOnlyList<AgentPackageSkillTimelineExternalBinding> targets)
        {
            var targetIds = (targets ?? new List<AgentPackageSkillTimelineExternalBinding>())
                .Select(value => value.id)
                .ToHashSet(StringComparer.Ordinal);
            foreach (TimelineExternalBindingDeclaration existing in timeline.ExternalBindings.ToArray())
                if (!targetIds.Contains(existing.AuthoringId))
                    timeline.RemoveExternalBinding(existing);
            foreach (AgentPackageSkillTimelineExternalBinding target in targets ?? new List<AgentPackageSkillTimelineExternalBinding>())
            {
                TimelineExternalBindingDeclaration binding = timeline.ExternalBindings.FirstOrDefault(value => value.AuthoringId == target.id);
                if (binding == null)
                {
                    if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                        throw new InvalidOperationException($"Timeline ExternalBinding '{target.id}'无法保持稳定identity。");
                    binding = timeline.AddExternalBinding(
                        target.bindingId,
                        target.displayName,
                        target.domain,
                        Enum.Parse<TimelineBindingValueKind>(target.valueKind, false),
                        Enum.Parse<TimelineBindingAccess>(target.access, false),
                        Enum.Parse<TimelineBindingLifetime>(target.lifetime, false),
                        target.parameterId);
                    if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                        binding.ConfigureAuthoringIdentity(target.id);
                }
                else
                    binding.Configure(
                        target.bindingId,
                        target.displayName,
                        target.domain,
                        Enum.Parse<TimelineBindingValueKind>(target.valueKind, false),
                        Enum.Parse<TimelineBindingAccess>(target.access, false),
                        Enum.Parse<TimelineBindingLifetime>(target.lifetime, false),
                        target.parameterId);
                if (!binding.Validate(out string error))
                    throw new InvalidOperationException($"Timeline ExternalBinding '{target.id}'无效：{error}");
            }
        }

        void SyncTimelineSections(TimelineData timeline, IReadOnlyList<AgentPackageSkillTimelineSection> targets)
        {
            var targetIds = (targets ?? new List<AgentPackageSkillTimelineSection>())
                .Select(value => value.id)
                .ToHashSet(StringComparer.Ordinal);
            foreach (TimelineSection existing in timeline.Sections.ToArray())
                if (!targetIds.Contains(existing.AuthoringId))
                    timeline.RemoveSection(existing);
            foreach (AgentPackageSkillTimelineSection target in targets ?? new List<AgentPackageSkillTimelineSection>())
            {
                TimelineSection section = timeline.Sections.FirstOrDefault(value => value.AuthoringId == target.id);
                if (section == null)
                {
                    if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                        throw new InvalidOperationException($"Timeline Section '{target.id}'无法保持稳定identity。");
                    section = timeline.AddSection(target.name ?? string.Empty, target.frame);
                    if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                        section.ConfigureAuthoringIdentity(target.id);
                }
                else
                    timeline.ConfigureSection(section, target.name ?? string.Empty, target.frame);
            }
        }

        void SyncTimelineClips(
            TimelineData timeline,
            TimelineContractCatalog catalog,
            Track track,
            IReadOnlyList<AgentPackageSkillTimelineClip> targets)
        {
            var targetIds = (targets ?? new List<AgentPackageSkillTimelineClip>()).Select(value => value.id).ToHashSet(StringComparer.Ordinal);
            foreach (Clip existing in track.Clips.ToArray())
                if (!targetIds.Contains(existing.AuthoringId))
                    track.RemoveClip(existing);
            foreach (AgentPackageSkillTimelineClip target in targets ?? new List<AgentPackageSkillTimelineClip>())
            {
                Clip clip = track.Clips.FirstOrDefault(value => value.AuthoringId == target.id);
                if (clip == null)
                {
                    if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                        throw new InvalidOperationException($"Timeline Clip '{target.id}'无法创建新的formal identity。");
                    clip = CreateClip(timeline, catalog, track, target);
                    if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                        clip.ConfigureAuthoringIdentity(target.id);
                    if (target.id.StartsWith("local:", StringComparison.Ordinal))
                        m_LocalClips[TimelineKey(timeline.AuthoringId, target.id)] = clip;
                }
                else if (clip.ContractKind != target.kind)
                    throw new InvalidOperationException($"Timeline Clip '{target.id}'不能原位改变kind。");
                clip.StartFrame = target.startFrame;
                clip.EndFrame = target.endFrame;
                clip.OtherEaseInFrame = target.otherEaseInFrame;
                clip.OtherEaseOutFrame = target.otherEaseOutFrame;
                clip.SelfEaseInFrame = target.selfEaseInFrame;
                clip.SelfEaseOutFrame = target.selfEaseOutFrame;
                clip.ClipInFrame = target.clipInFrame;
                ConfigureClip(timeline, clip, target);
            }
        }

        void SyncTimelineMotionWarpSources(
            TimelineData timeline,
            IReadOnlyList<AgentPackageSkillTimelineTrack> tracks)
        {
            foreach (AgentPackageSkillTimelineTrack targetTrack in tracks ?? new List<AgentPackageSkillTimelineTrack>())
                foreach (AgentPackageSkillTimelineClip target in targetTrack?.clips ?? new List<AgentPackageSkillTimelineClip>())
                {
                    if (target?.kind != TimelineContractKinds.MotionWarpClip)
                        continue;
                    Clip clip = target.id.StartsWith("local:", StringComparison.Ordinal) &&
                                 m_LocalClips.TryGetValue(TimelineKey(timeline.AuthoringId, target.id), out Clip local)
                        ? local
                        : timeline.Tracks
                            .SelectMany(value => value.Clips)
                            .FirstOrDefault(value => value.AuthoringId == target.id);
                    string sourceId = target.properties?.Value<string>("sourceMotionClipId");
                    if (clip is not MotionWarpClip warp || string.IsNullOrEmpty(sourceId) ||
                        !TryResolveMotionClip(timeline, sourceId, out MotionCurveClip source))
                        throw new InvalidOperationException($"MotionWarp Clip '{target.id}'的source MotionCurve无法解析。");
                    MotionWarpAuthoring.BindSource(timeline, warp, source);
                }
        }

        void SyncSkillDefinitions()
        {
            var values = new List<CharacterSkillAuthoringDefinition>();
            var skillIds = new HashSet<string>(StringComparer.Ordinal);
            var resolvedSkillIds = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (AgentSnapshotSkillDefinition source in m_Document.skills ?? new List<AgentSnapshotSkillDefinition>())
            {
                if (source == null)
                    continue;
                resolvedSkillIds[source.skillId] = StableSkillId(source.skillId, skillIds);
                skillIds.Add(resolvedSkillIds[source.skillId]);
            }
            foreach (AgentSnapshotSkillDefinition source in m_Document.skills ?? new List<AgentSnapshotSkillDefinition>())
            {
                if (source == null)
                    continue;
                string skillId = resolvedSkillIds[source.skillId];
                if (!m_Session.Resolver.TryResolveActionProfile(source.actionProfileId, out ActionProfile profile) ||
                    !m_Session.Resolver.TryResolveActionContext(new AgentAssetReference(source.actionContext, source.actionContextAssetPath, source.actionContextAssetGuid), out ActionContextSlot context))
                    throw new InvalidOperationException($"SkillDefinition '{source.skillId}'资源在apply阶段无法解析。");
                BtsmtlSkillFlowGraph entry = ResolveGraph(source.entryGraphAuthoringId) as BtsmtlSkillFlowGraph;
                if (!entry)
                    throw new InvalidOperationException($"SkillDefinition '{source.skillId}'入口Graph无法解析。");
                var definition = new CharacterSkillAuthoringDefinition();
                definition.ConfigureAuthoring(
                    skillId,
                    entry.AuthoringId,
                    profile,
                    context,
                    source.sourceInputRequestId,
                    source.consumeSourceInputRequest,
                    source.targetInputValueId,
                    source.targetKey);
                definition.ConfigureSkillRelations(
                    (source.subgraphDependencies ?? new List<AgentSnapshotSkillSubgraphDependency>()).Where(value => value != null).Select(value => new CharacterSkillSubgraphDependencyConfiguration(
                        ResolveGraph(value.subgraphIdentity) is IBtsmtlSkillFlowGraph graph ? graph.AuthoringId : value.subgraphIdentity,
                        ResolveCallSite(value.callSiteIdentity))),
                    (source.allowedFollowUpSkillIds ?? new List<string>()).Select(value =>
                        resolvedSkillIds.TryGetValue(value, out string resolved)
                            ? resolved
                            : value));
                values.Add(definition);
            }
            m_Session.Definition.SetSkillDefinitions(values.ToArray());
            BtsmtlSkillFlowGraph[] roots = m_Document.skills
                .Select(value => ResolveGraph(value.entryGraphAuthoringId) as BtsmtlSkillFlowGraph)
                .Where(value => value)
                .Distinct()
                .ToArray();
            m_Session.Definition.SetSkillGraphs(roots);
        }

        void DeleteRemovedGraphs()
        {
            foreach (FlowGraph graph in m_Current.Graphs.Values.Distinct().ToArray())
            {
                if (!graph)
                    continue;
                if (m_TargetGraphs.ContainsKey(((IBtsmtlSkillFlowGraph)graph).AuthoringId))
                    continue;
                if (graph is BtsmtlSkillFlowGraph root && m_Session.Definition.SkillGraphs.Contains(root))
                    continue;
                if (AssetDatabase.IsSubAsset(graph))
                    Undo.DestroyObjectImmediate(graph);
            }
        }

        void DeleteRemovedTimelines()
        {
            foreach (TimelineAsset asset in m_Current.Timelines.Values.Distinct().ToArray())
            {
                if (!asset)
                    continue;
                if (asset.Data != null && m_TargetTimelines.ContainsKey(asset.Data.AuthoringId))
                    continue;
                if (AssetDatabase.IsSubAsset(asset))
                    Undo.DestroyObjectImmediate(asset);
            }
        }

        FlowGraph CreateGraph(AgentPackageSkillFlowGraphFile target)
        {
            FlowGraph graph;
            if (target.role == BtsmtlSkillFlowGraphRole.Subgraph.ToString())
            {
                var macro = ScriptableObject.CreateInstance<BtsmtlSkillMacroGraph>();
                BtsmtlSkillMacroInterface.Initialize(macro);
                macro.ConfigureIdentity(
                    target.id.StartsWith("local:", StringComparison.Ordinal)
                        ? BtsmtlSkillGraphAssetFactory.StableIdentity(target.id)
                        : target.id);
                graph = macro;
            }
            else
            {
                var skill = ScriptableObject.CreateInstance<BtsmtlSkillFlowGraph>();
                string identity = target.id.StartsWith("local:", StringComparison.Ordinal)
                    ? BtsmtlSkillGraphAssetFactory.StableIdentity(target.id)
                    : target.id;
                skill.ConfigureIdentity(
                    identity,
                    Enum.Parse<BtsmtlSkillFlowGraphRole>(target.role, false));
                graph = skill;
            }
            graph.name = string.IsNullOrWhiteSpace(target.name) ? "Skill Graph" : target.name;
            if (target.ownership == AgentGraphOwnership.RootAsset.ToString())
            {
                string assetPath = target.id.StartsWith("local:", StringComparison.Ordinal)
                    ? AgentSkillFlowAssetPaths.Root(m_Session.Definition, target.id)
                    : target.asset?.assetPath;
                if (string.IsNullOrEmpty(assetPath))
                    throw new InvalidOperationException($"Skill Graph '{target.id}'缺少根资产路径。");
                AgentSkillFlowAssetPaths.RequireAvailable(assetPath);
                AssetDatabase.CreateAsset(graph, assetPath);
            }
            else if (target.ownership == AgentGraphOwnership.SharedAsset.ToString())
            {
                string assetPath = target.id.StartsWith("local:", StringComparison.Ordinal)
                    ? SharedGraphAssetPath(target.id)
                    : target.asset?.assetPath;
                if (string.IsNullOrEmpty(assetPath))
                    throw new InvalidOperationException($"共享技能Graph '{target.id}'缺少资产路径。");
                if (File.Exists(assetPath) || File.Exists(assetPath + ".meta") ||
                    AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
                    throw new InvalidOperationException($"共享技能Graph目标资产已存在：{assetPath}");
                AssetDatabase.CreateAsset(graph, assetPath);
            }
            else
            {
                FlowGraph owner = ResolveGraph(target.owner?.graphId);
                string ownerPath = owner ? AssetDatabase.GetAssetPath(owner) : string.Empty;
                if (string.IsNullOrEmpty(ownerPath) ||
                    AssetDatabase.LoadMainAssetAtPath(ownerPath) is not IBtsmtlSkillFlowGraph)
                    throw new InvalidOperationException($"Skill Graph '{target.id}'需要技能图文件作为owner。");
                AssetDatabase.AddObjectToAsset(graph, ownerPath);
            }
            Undo.RegisterCreatedObjectUndo(graph, "创建技能Graph");
            BtsmtlSkillGraphAssetFactory.PopulateAnchors(graph);
            m_Session.Touch(graph);
            return graph;
        }

        string SharedGraphAssetPath(string identity)
        {
            string definitionPath = AssetDatabase.GetAssetPath(m_Session.Definition);
            if (string.IsNullOrEmpty(definitionPath))
                throw new InvalidOperationException("共享技能Graph需要持久化Definition。");
            string directory = Path.GetDirectoryName(definitionPath)?.Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(definitionPath);
            string suffix = BtsmtlSkillGraphAssetFactory.StableIdentity(identity);
            return $"{directory}/{name}.SharedGraph.{suffix}.asset";
        }

        static bool MatchesGraphType(FlowGraph graph, string role)
        {
            return role == BtsmtlSkillFlowGraphRole.Subgraph.ToString()
                ? graph is BtsmtlSkillMacroGraph
                : graph is BtsmtlSkillFlowGraph skill && skill.Role.ToString() == role;
        }

        TimelineAsset ResolveExistingTimeline(string identity)
        {
            foreach (AgentPackageSkillTimelineFile target in m_Document.timelines ?? new List<AgentPackageSkillTimelineFile>())
            {
                if (target.id != identity)
                    continue;
                TimelineAsset asset = target.asset == null ? null : ResolveObject<TimelineAsset>(target.asset);
                if (asset)
                    return asset;
            }
            foreach (TimelineAsset asset in m_Current.Timelines.Values)
                if (asset && asset.Data != null && asset.Data.AuthoringId == identity)
                    return asset;
            return null;
        }

        FlowGraph ResolveGraph(string identity)
        {
            if (string.IsNullOrEmpty(identity))
                return null;
            if (m_Graphs.TryGetValue(identity, out FlowGraph graph))
                return graph;
            if (m_LocalGraphs.TryGetValue(identity, out graph))
                return graph;
            return m_Current.Graphs.TryGetValue(identity, out graph) ? graph : null;
        }

        TimelineAsset ResolveTimeline(string identity)
        {
            if (m_Timelines.TryGetValue(identity, out TimelineAsset asset))
                return asset;
            if (m_LocalTimelines.TryGetValue(identity, out asset))
                return asset;
            throw new InvalidOperationException($"Skill Timeline local identity无法解析：{identity}");
        }

        ActionContextSlot ResolveActionContext(JToken token)
        {
            if (token is not JObject value)
                return null;
            string id = value.Value<string>("id");
            if (string.IsNullOrWhiteSpace(id))
                return null;
            AgentPackageObjectReference reference = value["asset"]?.ToObject<AgentPackageObjectReference>();
            return m_Session.Resolver.TryResolveActionContext(new AgentAssetReference(id, reference?.assetPath, reference?.assetGuid), out ActionContextSlot context)
                ? context
                : throw new InvalidOperationException($"ActionContext无法解析：{id}");
        }

        ActionProfile ResolveActionProfile(JToken token)
        {
            if (token is not JObject value || !m_Session.Resolver.TryResolveActionProfile(value.Value<string>("id"), out ActionProfile profile))
                throw new InvalidOperationException("ActionProfile无法解析。");
            return profile;
        }

        static void SetInput(IBtsmtlSkillInputNode node, string value, string providerOwnerId)
        {
            if (string.IsNullOrWhiteSpace(providerOwnerId))
                throw new InvalidOperationException("Skill input Document缺少providerOwnerId。");
            switch (node)
            {
                case BtsmtlSkillBooleanInputFlowNode boolean:
                    boolean.SetInputId(value, providerOwnerId);
                    break;
                case BtsmtlSkillScalarInputFlowNode scalar:
                    scalar.SetInputId(value, providerOwnerId);
                    break;
                case BtsmtlSkillVector2InputFlowNode vector:
                    vector.SetInputId(value, providerOwnerId);
                    break;
                case BtsmtlSkillInputMagnitudeFlowNode magnitude:
                    magnitude.SetInputId(value, providerOwnerId);
                    break;
                case BtsmtlSkillActionRequestFlowNode request:
                    request.SetInputId(value, providerOwnerId);
                    break;
            }
        }

        static string RequiredProviderOwner(JObject properties)
        {
            string value = properties.Value<string>("providerOwnerId");
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException("Skill Provider引用缺少providerOwnerId。")
                : value;
        }

        GameplayEffectDefinition ResolveGameplayEffect(JToken token)
        {
            if (token is not JObject value)
                throw new InvalidOperationException("Gameplay Effect引用缺失。");
            string id = value.Value<string>("id");
            GameplayEffectDefinition effect = m_Session.Definition.GameplayEffectProfile?.EffectDefinitions
                .FirstOrDefault(candidate => candidate && candidate.EffectId.Value == id);
            return effect ? effect : throw new InvalidOperationException($"Gameplay Effect无法解析：{id}");
        }

        static GameplayTagQuery ParseGameplayTagQuery(JToken token)
        {
            JObject value = token as JObject ?? new JObject();
            return new GameplayTagQuery(
                Tags(value["all"]),
                Tags(value["any"]),
                Tags(value["none"]));

            static IReadOnlyList<GameplayTagId> Tags(JToken source)
            {
                return (source as JArray)?.Values<string>()
                    .Select(item => new GameplayTagId(item))
                    .ToArray() ?? Array.Empty<GameplayTagId>();
            }
        }

        BtsmtlSkillBlackboardReference BlackboardReference(JObject properties)
        {
            string declarationId = properties.Value<string>("declarationId");
            if (m_LocalDeclarations.TryGetValue(declarationId ?? string.Empty, out string resolvedDeclarationId))
                declarationId = resolvedDeclarationId;
            string ownerId = properties.Value<string>("ownerId");
            if (ResolveGraph(ownerId) is IBtsmtlSkillFlowGraph owner)
                ownerId = owner.AuthoringId;
            return new BtsmtlSkillBlackboardReference(declarationId, ownerId);
        }

        static string ValueTypeEnum(string value)
        {
            return value switch
            {
                "bool" => nameof(BtsmtlSkillBlackboardValueType.Boolean),
                "int" => nameof(BtsmtlSkillBlackboardValueType.Integer),
                "float" => nameof(BtsmtlSkillBlackboardValueType.Number),
                "string" => nameof(BtsmtlSkillBlackboardValueType.Identity),
                "vector2" => nameof(BtsmtlSkillBlackboardValueType.Vector2),
                "vector3" => nameof(BtsmtlSkillBlackboardValueType.Vector3),
                _ => value
            };
        }

        static Type ResolveMacroType(string value) =>
            AgentSkillFlowAuthoringCapabilities.TryResolveValueType(value, out Type type)
                ? type
                : throw new InvalidOperationException($"Macro parameter value type无法解析：{value}");

        Vector2 FindPosition(string graphId, string nodeId)
        {
            AgentPackageSkillFlowGraphLayoutFile layout = m_TargetLayouts.TryGetValue(graphId, out AgentPackageSkillFlowGraphLayoutFile value)
                ? value
                : null;
            AgentPackageSkillFlowNodeLayout position = layout?.nodes?.FirstOrDefault(item => item != null && item.id == nodeId);
            return position == null ? Vector2.zero : new Vector2(position.x, position.y);
        }

        static void ApplyValues(FlowNode node, JObject values)
        {
            foreach (ValueInput input in node.GetInputValuePorts())
            {
                if (input.isConnected)
                    continue;
                if (values?.TryGetValue(input.ID, out JToken value) == true)
                    input.serializedValue = ParseValue(value, input.type);
                else
                    input.serializedValue = input.defaultValue;
            }
        }

        static object ParseValue(JToken token, Type type)
        {
            if (token == null || token.Type == JTokenType.Null)
                return type == typeof(string) ? string.Empty : type.IsValueType ? Activator.CreateInstance(type) : null;
            if (type == typeof(Vector2))
                return new Vector2(token.Value<float>("x"), token.Value<float>("y"));
            if (type == typeof(Vector3))
                return new Vector3(token.Value<float>("x"), token.Value<float>("y"), token.Value<float>("z"));
            if (type == typeof(ActionTargetSnapshot))
            {
                string targetId = token.Value<string>("targetId") ?? string.Empty;
                Vector3 position = new Vector3(token.Value<float>("x"), token.Value<float>("y"), token.Value<float>("z"));
                Quaternion rotation = new Quaternion(
                    token.Value<float>("rx"),
                    token.Value<float>("ry"),
                    token.Value<float>("rz"),
                    token["rw"] == null ? 1f : token["rw"].Value<float>());
                return new ActionTargetSnapshot(targetId, position, rotation);
            }
            return token.ToObject(type);
        }

        static string NodeKey(string graphId, string nodeId) => graphId + "\0" + nodeId;
        static string TimelineKey(string timelineId, string itemId) => timelineId + "\0" + itemId;
        static string ParameterKey(string graphId, string parameterId) => graphId + "\0" + parameterId;
        static string StepKey(string graphId, string nodeId, string portId) => graphId + "\0" + nodeId + "\0" + portId;

        string ResolveCallSite(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            int nodeMarker = value.LastIndexOf("/node:", StringComparison.Ordinal);
            if (nodeMarker < 0)
                return value;
            int graphMarker = value.LastIndexOf("/graph:", nodeMarker);
            if (graphMarker < 0 || nodeMarker <= graphMarker)
                return value;
            int nodeStart = nodeMarker + "/node:".Length;
            int nodeEnd = value.IndexOf('/', nodeStart);
            if (nodeEnd < 0)
                nodeEnd = value.Length;
            string graphId = value.Substring(
                graphMarker + "/graph:".Length,
                nodeMarker - graphMarker - "/graph:".Length);
            string nodeId = value.Substring(nodeStart, nodeEnd - nodeStart);
            string graph = graphId;
            if (m_LocalGraphs.TryGetValue(graphId, out FlowGraph graphValue))
                graph = ((IBtsmtlSkillFlowGraph)graphValue).AuthoringId;
            string node = nodeId;
            if (m_LocalNodes.TryGetValue(NodeKey(graphId, nodeId), out FlowNode nodeValue))
                node = nodeValue.UID;
            return value.Substring(0, graphMarker + "/graph:".Length) +
                   graph +
                   value.Substring(nodeMarker, "/node:".Length) +
                   node +
                   value.Substring(nodeEnd);
        }

        string StableSkillId(string value, ISet<string> used)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith("local:", StringComparison.Ordinal))
                return value;
            string candidate = value.Substring("local:".Length);
            if (!used.Contains(candidate))
                return candidate;
            string suffix = AgentAuthoringDocumentCodec.Hash(value).Substring(0, 12);
            string result = candidate + "-" + suffix;
            int index = 1;
            while (used.Contains(result))
                result = candidate + "-" + suffix + "-" + index++.ToString(CultureInfo.InvariantCulture);
            return result;
        }

        static void RemoveConnections(FlowGraph graph, FlowNode node)
        {
            foreach (BinderConnection connection in node.inConnections.OfType<BinderConnection>().Concat(node.outConnections.OfType<BinderConnection>()).Distinct().ToArray())
                graph.RemoveConnection(connection, false);
        }

        static void RemoveConnectionsForRemovedSteps(BtsmtlSkillCompositeFlowNode node, IReadOnlyList<BtsmtlSkillStepPort> steps)
        {
            var ids = steps.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
            foreach (BinderConnection connection in node.outConnections.OfType<BinderConnection>().ToArray())
                if (!ids.Contains(connection.sourcePortID))
                    ((FlowGraph)node.graph).RemoveConnection(connection, false);
        }

        static Type TrackType(string kind)
        {
            return kind switch
            {
                TimelineContractKinds.AnimationTrack => typeof(AnimationTrack),
                TimelineContractKinds.MotionCurveTrack => typeof(MotionCurveTrack),
                TimelineContractKinds.MotionWarpTrack => typeof(MotionWarpTrack),
                TimelineContractKinds.TreeTrack => typeof(TreeTrack),
                TimelineContractKinds.ActionCueTrack => typeof(ActionCueTrack),
                TimelineContractKinds.CameraStateTrack => typeof(CameraStateTrack),
                TimelineContractKinds.CameraCueTrack => typeof(CameraCueTrack),
                TimelineContractKinds.CameraResponseTrack => typeof(CameraResponseTrack),
                TimelineContractKinds.ScenePresentationParameterTrack => typeof(ScenePresentationParameterTrack),
                _ => throw new InvalidOperationException($"Skill Timeline Track kind无法解析：{kind}")
            };
        }

        Clip CreateClip(
            TimelineData timeline,
            TimelineContractCatalog catalog,
            Track track,
            AgentPackageSkillTimelineClip target)
        {
            if (track is AnimationTrack animation)
                return timeline.AddClip(
                    catalog,
                    ResolveObject<UnityEngine.AnimationClip>(target.animationClip),
                    animation,
                    target.startFrame);
            if (track is TreeTrack tree)
                return timeline.AddClip(
                    catalog,
                    ResolveGraph(target.treeGraphId) as ScriptableObject,
                    tree,
                    target.startFrame);
            return timeline.AddClip(catalog, track, target.startFrame);
        }

        void ConfigureClip(TimelineData timeline, Clip clip, AgentPackageSkillTimelineClip target)
        {
            JObject value = target.properties ?? new JObject();
            if (clip is BTSMTL.Timeline.AnimationClip animation)
                animation.ExtraPolationMode = Enum.Parse<ExtraPolationMode>(value.Value<string>("extraPolationMode") ?? animation.ExtraPolationMode.ToString(), false);
            if (clip is MotionCurveClip motion)
            {
                motion.CurveId = value.Value<string>("curveId") ?? motion.CurveId;
                motion.CurveEndFrame = value.Value<int?>("curveEndFrame") ?? motion.CurveEndFrame;
                motion.Space = Enum.Parse<TimelineMotionContributionSpace>(value.Value<string>("space") ?? motion.Space.ToString(), false);
                motion.Channel = Enum.Parse<TimelineMotionChannel>(value.Value<string>("channel") ?? motion.Channel.ToString(), false);
                motion.BlendMode = Enum.Parse<TimelineMotionBlendMode>(value.Value<string>("blendMode") ?? motion.BlendMode.ToString(), false);
                motion.Priority = value.Value<int?>("priority") ?? motion.Priority;
                motion.ConsumeLowerChannels = value.Value<bool?>("consumeLowerChannels") ?? motion.ConsumeLowerChannels;
            }
            if (clip is MotionWarpClip warp)
            {
                warp.ConfigureAuthoring(
                    Enum.Parse<MotionWarpTranslationMode>(value.Value<string>("translationMode") ?? warp.TranslationMode.ToString(), false),
                    Enum.Parse<MotionWarpTargetOffsetSpace>(value.Value<string>("targetOffsetSpace") ?? warp.TargetOffsetSpace.ToString(), false),
                    Enum.Parse<MotionWarpRotationMode>(value.Value<string>("rotationMode") ?? warp.RotationMode.ToString(), false),
                    Enum.Parse<MotionWarpRotationMethod>(value.Value<string>("rotationMethod") ?? warp.RotationMethod.ToString(), false),
                    value["targetPlanarOffset"] == null ? warp.TargetPlanarOffset : new Vector2(value["targetPlanarOffset"].Value<float>("x"), value["targetPlanarOffset"].Value<float>("y")),
                    value.Value<float?>("targetYawOffsetDegrees") ?? warp.TargetYawOffsetDegrees,
                    value.Value<float?>("maxTotalPositionCorrection") ?? warp.MaxTotalPositionCorrection,
                    value.Value<float?>("maxTotalYawCorrectionDegrees") ?? warp.MaxTotalYawCorrectionDegrees,
                    value.Value<float?>("maximumYawRateDegreesPerSecond") ?? warp.MaximumYawRateDegreesPerSecond,
                    Enum.Parse<MotionWarpLimitPolicy>(value.Value<string>("limitPolicy") ?? warp.LimitPolicy.ToString(), false),
                    warp.PositionProgressCurve,
                    warp.YawProgressCurve);
                if (!string.IsNullOrEmpty(value.Value<string>("sourceMotionClipId")) &&
                    TryResolveMotionClip(timeline, value.Value<string>("sourceMotionClipId"), out MotionCurveClip source))
                    MotionWarpAuthoring.BindSource(timeline, warp, source);
            }
            if (clip is ActionCueClip actionCue)
            {
                actionCue.CueId = value.Value<string>("cueId") ?? actionCue.CueId;
                actionCue.CueType = value.Value<string>("cueType") ?? actionCue.CueType;
            }
            if (clip is CameraStateClip cameraState)
            {
                cameraState.Mode = Enum.Parse<TimelineCameraMode>(value.Value<string>("mode") ?? cameraState.Mode.ToString(), false);
                cameraState.Priority = value.Value<int?>("priority") ?? cameraState.Priority;
                cameraState.BlendInSeconds = value.Value<float?>("blendInSeconds") ?? cameraState.BlendInSeconds;
                cameraState.BlendOutSeconds = value.Value<float?>("blendOutSeconds") ?? cameraState.BlendOutSeconds;
                cameraState.TargetKey = value.Value<string>("targetKey") ?? cameraState.TargetKey;
                cameraState.InterruptPolicy = Enum.Parse<TimelineCameraInterruptPolicy>(value.Value<string>("interruptPolicy") ?? cameraState.InterruptPolicy.ToString(), false);
            }
            if (clip is CameraCueClip cameraCue)
            {
                cameraCue.CueId = value.Value<string>("cueId") ?? cameraCue.CueId;
                cameraCue.CueKind = Enum.Parse<TimelineCameraCueKind>(value.Value<string>("cueKind") ?? cameraCue.CueKind.ToString(), false);
                cameraCue.CueType = value.Value<string>("cueType") ?? cameraCue.CueType;
                cameraCue.Intensity = value.Value<float?>("intensity") ?? cameraCue.Intensity;
                cameraCue.DurationSeconds = value.Value<float?>("durationSeconds") ?? cameraCue.DurationSeconds;
                cameraCue.Priority = value.Value<int?>("priority") ?? cameraCue.Priority;
            }
            if (clip is CameraResponseClip cameraResponse)
            {
                cameraResponse.LookResponse = Enum.Parse<TimelineCameraLookResponseMode>(value.Value<string>("lookResponse") ?? cameraResponse.LookResponse.ToString(), false);
                cameraResponse.ManualOrbitWeight = value.Value<float?>("manualOrbitWeight") ?? cameraResponse.ManualOrbitWeight;
                cameraResponse.PitchResponseWeight = value.Value<float?>("pitchResponseWeight") ?? cameraResponse.PitchResponseWeight;
                cameraResponse.YawResponseWeight = value.Value<float?>("yawResponseWeight") ?? cameraResponse.YawResponseWeight;
                cameraResponse.Priority = value.Value<int?>("priority") ?? cameraResponse.Priority;
            }
            if (clip is ScenePresentationParameterCurveClip sceneParameter)
                sceneParameter.ConfigureBindings(
                    value.Value<string>("targetBindingId"),
                    value.Value<string>("parameterBindingId"),
                    CurveFromToken(value["valueCurve"]));
            if (clip is TreeClip tree)
            {
                if (!string.IsNullOrEmpty(target.treeGraphId))
                    tree.SetAssetTree(ResolveGraph(target.treeGraphId) as ScriptableObject);
                tree.SetExecutionPhase(Enum.Parse<TimelineTreeExecutionPhase>(target.treePhase ?? TimelineTreeExecutionPhase.Commit.ToString(), false));
            }
            foreach (AgentPackageCurve curve in target.curves ?? new List<AgentPackageCurve>())
                TimelineCurveChannelCatalog.Require(curve.channelId).Replace(clip, ToCurve(curve));
        }

        static AnimationCurve ToCurve(AgentPackageCurve value)
        {
            var curve = new AnimationCurve((value.keys ?? new List<AgentAnimationCurveKey>()).Select(key => new Keyframe(
                key.time,
                key.value,
                key.inTangent,
                key.outTangent,
                key.inWeight,
                key.outWeight)
            {
                weightedMode = Enum.Parse<WeightedMode>(key.weightedMode, false)
            }).ToArray())
            {
                preWrapMode = Enum.Parse<WrapMode>(value.preWrapMode, false),
                postWrapMode = Enum.Parse<WrapMode>(value.postWrapMode, false)
            };
            return curve;
        }

        static AnimationCurve CurveFromToken(JToken token)
        {
            JObject value = token as JObject ?? throw new InvalidOperationException("Timeline Scene Presentation value curve缺失。");
            return ToCurve(new AgentPackageCurve
            {
                preWrapMode = value.Value<string>("preWrapMode"),
                postWrapMode = value.Value<string>("postWrapMode"),
                keys = value["keys"]?.ToObject<List<AgentAnimationCurveKey>>()
            });
        }

        Port ResolvePort(FlowGraph graph, string graphId, AgentPackageSkillFlowEdgeEndpoint endpoint, bool input)
        {
            FlowNode node = ResolveNode(graphId, endpoint.node);
            if (node == null && AgentSkillFlowAuthoringCapabilities.IsAnchor(endpoint.node))
                node = graph.allNodes.OfType<FlowNode>().FirstOrDefault(value =>
                    AgentSkillFlowAuthoringCapabilities.TryGetKind(value, out string kind) && kind == endpoint.node);
            if (node == null)
                return null;
            string portId = endpoint.port;
            if (node is BtsmtlSkillCompositeFlowNode &&
                m_LocalPorts.TryGetValue(
                    StepKey(
                        graphId,
                        AgentSkillFlowAuthoringCapabilities.IsAnchor(endpoint.node) ? node.UID : endpoint.node,
                        portId),
                    out string resolvedPortId))
                portId = resolvedPortId;
            if ((node is MacroInputNode || node is MacroOutputNode) &&
                m_LocalParameters.TryGetValue(ParameterKey(graphId, portId), out string resolvedParameterId))
                portId = resolvedParameterId;
            if (node is MacroNodeWrapper macro && macro.macro is BtsmtlSkillMacroGraph macroGraph)
            {
                string macroId = m_Graphs.FirstOrDefault(value => value.Value == macroGraph).Key;
                if (m_LocalParameters.TryGetValue(ParameterKey(macroId, portId), out string resolvedMacroPortId))
                    portId = resolvedMacroPortId;
            }
            return input ? node.GetInputPort(portId) : node.GetOutputPort(portId);
        }

        FlowNode ResolveNode(string graphId, string id)
        {
            if (m_LocalNodes.TryGetValue(NodeKey(graphId, id), out FlowNode local))
                return local;
            return ResolveGraph(graphId)?.allNodes.OfType<FlowNode>().FirstOrDefault(value => value.UID == id);
        }

        bool TryResolveMotionClip(TimelineData timeline, string identity, out MotionCurveClip source)
        {
            source = null;
            string localKey = TimelineKey(timeline.AuthoringId, identity);
            if (m_LocalClips.TryGetValue(localKey, out Clip local))
            {
                source = local as MotionCurveClip;
                return source != null;
            }
            if (!MotionWarpAuthoring.TryResolveClip(timeline, identity, out Clip resolved))
                return false;
            source = resolved as MotionCurveClip;
            return source != null;
        }

        T ResolveObject<T>(JToken token) where T : UnityEngine.Object
        {
            return ResolveObject<T>(token?.ToObject<AgentPackageObjectReference>());
        }

        T ResolveObject<T>(AgentPackageObjectReference reference) where T : UnityEngine.Object
        {
            if (reference == null || !string.IsNullOrEmpty(reference.localId))
                return null;
            string path = AssetDatabase.GUIDToAssetPath(reference.assetGuid);
            if (string.IsNullOrEmpty(path) || path != reference.assetPath)
                return null;
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<T>()
                .FirstOrDefault(value =>
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out _, out long localFileId) &&
                    localFileId == reference.localFileId);
        }
    }

    internal sealed class AgentSkillFlowDocumentRuntimeIndex
    {
        public readonly Dictionary<string, FlowGraph> Graphs = new Dictionary<string, FlowGraph>(StringComparer.Ordinal);
        public readonly Dictionary<string, TimelineAsset> Timelines = new Dictionary<string, TimelineAsset>(StringComparer.Ordinal);
        readonly HashSet<FlowGraph> m_Visited = new HashSet<FlowGraph>();

        public void Build(CharacterPipelineDefinition definition)
        {
            Graphs.Clear();
            Timelines.Clear();
            m_Visited.Clear();
            foreach (BtsmtlSkillFlowGraph root in definition.SkillGraphs ?? Array.Empty<BtsmtlSkillFlowGraph>())
                Visit(root);
        }

        void Visit(FlowGraph graph)
        {
            if (graph is not IBtsmtlSkillFlowGraph authoring || !m_Visited.Add(graph))
                return;
            if (Graphs.TryGetValue(authoring.AuthoringId, out FlowGraph existing) && existing != graph)
                throw new InvalidOperationException($"Skill Graph identity重复：{authoring.AuthoringId}");
            Graphs.Add(authoring.AuthoringId, graph);
            foreach (FlowNode node in graph.allNodes.OfType<FlowNode>())
            {
                if (node is MacroNodeWrapper macro && macro.macro is BtsmtlSkillMacroGraph macroGraph)
                    Visit(macroGraph);
                if (node is BtsmtlSkillStateMachineFlowNode machine)
                    Visit(machine.StateMachine);
                if (node is BtsmtlSkillStateFlowNode state)
                    Visit(state.Body);
                if (node is BtsmtlSkillCompositeFlowNode composite)
                    foreach (BtsmtlSkillStepPort step in composite.Steps)
                        Visit(step.Condition);
                if (node is BtsmtlSkillTimelineFlowNode timeline && timeline.TimelineAsset)
                {
                    string timelineId = timeline.Timeline.AuthoringId;
                    if (Timelines.TryGetValue(timelineId, out TimelineAsset existingTimeline) &&
                        existingTimeline != timeline.TimelineAsset)
                        throw new InvalidOperationException($"Skill Timeline identity重复：{timelineId}");
                    Timelines[timelineId] = timeline.TimelineAsset;
                    foreach (Track track in timeline.Timeline.Tracks)
                        foreach (Clip clip in track.Clips)
                            if (clip is TreeClip tree && tree.AssetTree is BtsmtlSkillFlowGraph child)
                                Visit(child);
                }
            }
        }
    }
}
