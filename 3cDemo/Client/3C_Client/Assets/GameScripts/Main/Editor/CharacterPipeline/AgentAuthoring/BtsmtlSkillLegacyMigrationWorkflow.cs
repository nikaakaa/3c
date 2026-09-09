using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BTSMTL.Timeline;
using FlowCanvas;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public static class BtsmtlSkillProviderOwnerMigration
    {
        [MenuItem("Tools/3C/Character/Normalize Skill Provider Owners")]
        public static void NormalizeCorinSkillProviderOwners()
        {
            Normalize("Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset");
        }

        public static void Normalize(string definitionAssetPath)
        {
            CharacterPipelineDefinition definition =
                AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(definitionAssetPath);
            if (!definition)
                throw new InvalidOperationException($"Character Definition不存在：{definitionAssetPath}");
            if (!definition.InputProfile)
                throw new InvalidOperationException("Character Definition缺少正式InputProfile。");

            string inputGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(definition.InputProfile));
            if (string.IsNullOrWhiteSpace(inputGuid))
                throw new InvalidOperationException("InputProfile没有稳定资产owner。");
            string inputOwner = $"asset:{inputGuid}";
            var graphs = new List<FlowGraph>();
            foreach (BtsmtlSkillFlowGraph root in definition.SkillGraphs ?? Array.Empty<BtsmtlSkillFlowGraph>())
            {
                if (!root)
                    continue;
                foreach (FlowGraph graph in BtsmtlSkillGraphClosure.Validate(root, true))
                    if (!graphs.Contains(graph))
                        graphs.Add(graph);
            }

            int changed = 0;
            string controlOwner = $"control-module:{definition.ControlModuleId}";
            for (int graphIndex = 0; graphIndex < graphs.Count; graphIndex++)
            {
                FlowGraph graph = graphs[graphIndex];
                BtsmtlSkillFlowEditorMutation.Execute(graph, "规范Skill Provider Owner", () =>
                {
                    foreach (FlowNode node in graph.allNodes.OfType<FlowNode>())
                    {
                        if (node is IBtsmtlSkillInputNode input && input.ProviderOwnerId != inputOwner)
                        {
                            input.SetInputId(input.InputId, inputOwner);
                            changed++;
                        }
                        if (node is BtsmtlSkillMoveFacingAngleFlowNode facing && facing.ProviderOwnerId != controlOwner)
                        {
                            facing.Configure(controlOwner);
                            changed++;
                        }
                    }
                });
            }
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            string[] graphPaths = graphs
                .Select(graph => AssetDatabase.GetAssetPath(graph))
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (graphPaths.Length != 0)
                AssetDatabase.ForceReserializeAssets(graphPaths);
            AssetDatabase.SaveAssets();
            Debug.Log($"Skill Provider Owner规范完成：{changed}个节点。", definition);
        }
    }

    public static class BtsmtlSkillLegacyMigrationWorkflow
    {
        const string DefinitionPath = "Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset";
        const string SharedAttack1TimelinePath = "Assets/Configs/Character/Corin/Pipeline/Graphs/SharedTimelines/CorinAttack1Timeline.asset";
        [MenuItem("Tools/3C/Character/Migrate Corin Skills to Skill Graph")]
        public static void MigrateCorinSkills()
        {
            TimelineAsset sharedTimeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(SharedAttack1TimelinePath);
            if (!sharedTimeline)
                throw new InvalidOperationException($"Corin Attack1 Timeline不存在：{SharedAttack1TimelinePath}");
            sharedTimeline.Data.Init();
            CharacterPipelineDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterPipelineDefinition>(DefinitionPath);
            if (!definition)
                throw new InvalidOperationException($"Corin Definition不存在：{DefinitionPath}");
            new Migration(definition).Run();
        }

        sealed class Migration
        {
            readonly CharacterPipelineDefinition m_Definition;
            readonly Dictionary<string, LegacyGraphFile> m_Legacy = new(StringComparer.Ordinal);
            readonly Dictionary<string, AgentSnapshotGraph> m_SnapshotGraphs = new(StringComparer.Ordinal);
            readonly Dictionary<string, AgentSnapshotNode> m_SnapshotNodes = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_GraphIds = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_NodeIds = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_EdgeIds = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_DeclarationIds = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_TimelineIds = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_TrackIds = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_ClipIds = new(StringComparer.Ordinal);
            readonly Dictionary<string, AgentSnapshotTimeline> m_Timelines = new(StringComparer.Ordinal);
            readonly List<AgentSnapshotTimelineTreeClip> m_LegacyTimelineTreeClips = new();
            readonly Dictionary<string, AgentSnapshotBlackboardDeclaration> m_Blackboards = new(StringComparer.Ordinal);
            readonly Dictionary<string, AgentSnapshotTimelineCallSite> m_TimelineCalls = new(StringComparer.Ordinal);
            readonly Dictionary<string, AgentSnapshotLifecycleSummary> m_Lifecycles = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_ConditionOwners = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_TimelineBodyGraphs = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_TimelineBodyParents = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_TimelineBodyNodes = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_TimelineBodyTimelines = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_TimelineBodyTracks = new(StringComparer.Ordinal);
            readonly Dictionary<string, string> m_TimelineBodyClips = new(StringComparer.Ordinal);
            readonly Dictionary<string, RootAsset> m_Roots = new(StringComparer.Ordinal);
            readonly HashSet<string> m_OriginalSharedGraphPaths = new(StringComparer.OrdinalIgnoreCase);
            CharacterSkillAuthoringDefinition[] m_OriginalSkillDefinitions;
            AgentGraphSnapshot m_Snapshot;
            string m_LegacyPath;

            public Migration(CharacterPipelineDefinition definition) => m_Definition = definition;

            public void Run()
            {
                CaptureSharedGraphAssets();
                bool hasPersistedSkillGraph = m_Definition.SkillGraphs != null &&
                                              m_Definition.SkillGraphs.Any(graph =>
                                                  graph &&
                                                  !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(graph)) &&
                                                  File.Exists(AssetDatabase.GetAssetPath(graph)));
                if (hasPersistedSkillGraph)
                    throw new InvalidOperationException("Definition已经存在Skill Graph，迁移器拒绝覆盖现有作者资产。");
                if (m_Definition.SkillGraphs != null && m_Definition.SkillGraphs.Count > 0)
                {
                    m_Definition.SetSkillGraphs(Array.Empty<BtsmtlSkillFlowGraph>());
                    EditorUtility.SetDirty(m_Definition);
                    AssetDatabase.SaveAssets();
                }
                m_LegacyPath = FindLegacyPackage();
                LoadLegacyGraphs();
                m_OriginalSkillDefinitions = m_Definition.SkillDefinitions.ToArray();
                RestoreInterruptedSkillDefinitions();
                m_Snapshot = new AgentGraphSnapshotExporter().ExportFull(m_Definition, true);
                IndexSnapshot();
                PrepareIdentities();
                try
                {
                    CreateRoots();
                    AgentPackageSkillFlowDocument document = BuildDocument();
                    ApplyDocument(document);
                    AssetDatabase.SaveAssets();
                    AgentAuthoringResponse response = new AgentAuthoringDocumentApplicationService().Execute(new AgentAuthoringRequest
                    {
                        action = AgentAuthoringAction.CheckoutDocument,
                        domain = AgentAuthoringSchema.CharacterControllerDomain,
                        rootAssetPath = DefinitionPath
                    });
                    if (!response.success || response.syncState != AgentDocumentSyncState.Clean.ToString())
                        throw new InvalidOperationException($"v7 Document生成失败：{response.errorCode} {response.errorMessage}");
                    Debug.Log("Corin三项技能已迁移到原生Skill Graph，并已生成v7 Document。", m_Definition);
                }
                catch
                {
                    RollbackRoots();
                    RollbackSharedGraphAssets();
                    m_Definition.SetSkillGraphs(Array.Empty<BtsmtlSkillFlowGraph>());
                    m_Definition.SetSkillDefinitions(m_OriginalSkillDefinitions);
                    EditorUtility.SetDirty(m_Definition);
                    AssetDatabase.SaveAssets();
                    throw;
                }
            }

            void CaptureSharedGraphAssets()
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? throw new InvalidOperationException("无法解析项目根目录。");
                string directory = Path.Combine(projectRoot, Path.GetDirectoryName(DefinitionPath).Replace('/', Path.DirectorySeparatorChar));
                foreach (string path in Directory.Exists(directory)
                    ? Directory.GetFiles(directory, "*.SharedGraph.*.asset")
                    : Array.Empty<string>())
                    m_OriginalSharedGraphPaths.Add(path);
            }

            void RollbackSharedGraphAssets()
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? throw new InvalidOperationException("无法解析项目根目录。");
                string directory = Path.Combine(projectRoot, Path.GetDirectoryName(DefinitionPath).Replace('/', Path.DirectorySeparatorChar));
                foreach (string path in Directory.Exists(directory)
                    ? Directory.GetFiles(directory, "*.SharedGraph.*.asset")
                    : Array.Empty<string>())
                {
                    if (m_OriginalSharedGraphPaths.Contains(path))
                        continue;
                    string relative = path.Substring(projectRoot.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                    AssetDatabase.DeleteAsset(relative);
                }
            }

            string FindLegacyPackage()
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? throw new InvalidOperationException("无法解析项目根目录。");
                string root = Path.Combine(projectRoot, "AgentAuthoring", "Documents", AgentAuthoringSchema.CharacterControllerDomain);
                string identity = AssetDatabase.AssetPathToGUID(DefinitionPath);
                var matches = new List<string>();
                foreach (string directory in Directory.Exists(root) ? Directory.GetDirectories(root, "*.btsmtl") : Array.Empty<string>())
                {
                    string path = Path.Combine(directory, "manifest.json");
                    if (!File.Exists(path))
                        continue;
                    JObject manifest = JObject.Parse(File.ReadAllText(path, Encoding.UTF8));
                    if (manifest.Value<string>("schemaVersion") == "btsmtl-agent-authoring-document.v6" && manifest.Value<string>("rootIdentity") == identity)
                        matches.Add(directory);
                }
                if (matches.Count != 1)
                    throw new InvalidOperationException($"技能迁移要求唯一v6 Document，实际找到{matches.Count}个。");
                return matches[0];
            }

            void LoadLegacyGraphs()
            {
                string root = Path.Combine(m_LegacyPath, "editable", "graphs");
                foreach (string path in Directory.GetFiles(root, "graph.json", SearchOption.AllDirectories))
                {
                    LegacyGraphFile graph = JsonConvert.DeserializeObject<LegacyGraphFile>(File.ReadAllText(path, Encoding.UTF8));
                    if (graph == null || string.IsNullOrEmpty(graph.id) || !m_Legacy.TryAdd(graph.id, graph))
                        throw new InvalidOperationException($"旧Graph identity无效或重复：{path}");
                }
                string blackboardPath = Path.Combine(m_LegacyPath, "editable", "blackboard.json");
                if (File.Exists(blackboardPath))
                {
                    JObject blackboard = JObject.Parse(File.ReadAllText(blackboardPath, Encoding.UTF8));
                    foreach (AgentSnapshotBlackboardDeclaration declaration in blackboard["declarations"]?.ToObject<List<AgentSnapshotBlackboardDeclaration>>() ?? new List<AgentSnapshotBlackboardDeclaration>())
                        if (declaration != null && !string.IsNullOrEmpty(declaration.declarationId))
                            m_Blackboards[declaration.declarationId] = declaration;
                }
                string controllerPath = Path.Combine(m_LegacyPath, "editable", "controller.json");
                if (File.Exists(controllerPath))
                {
                    JObject controller = JObject.Parse(File.ReadAllText(controllerPath, Encoding.UTF8));
                    m_LegacyTimelineTreeClips.AddRange(
                        controller["timelineTreeClips"]?.ToObject<List<AgentSnapshotTimelineTreeClip>>() ??
                        new List<AgentSnapshotTimelineTreeClip>());
                }
            }

            void RestoreInterruptedSkillDefinitions()
            {
                string root = Path.Combine(m_LegacyPath, "editable", "skills");
                var entries = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string path in Directory.Exists(root)
                    ? Directory.GetFiles(root, "definition.json", SearchOption.AllDirectories)
                    : Array.Empty<string>())
                {
                    AgentSnapshotSkillDefinition source =
                        JsonConvert.DeserializeObject<AgentSnapshotSkillDefinition>(File.ReadAllText(path, Encoding.UTF8));
                    if (source == null ||
                        string.IsNullOrEmpty(source.skillId) ||
                        string.IsNullOrEmpty(source.entryGraphAuthoringId) ||
                        !entries.TryAdd(source.skillId, source.entryGraphAuthoringId))
                        throw new InvalidOperationException($"旧SkillDefinition identity无效或重复：{path}");
                }

                bool changed = false;
                foreach (CharacterSkillAuthoringDefinition skill in m_Definition.SkillDefinitions)
                {
                    if (skill == null || m_Legacy.ContainsKey(skill.EntryGraphAuthoringId))
                        continue;
                    if (!entries.TryGetValue(skill.SkillId, out string entryGraphAuthoringId) ||
                        !m_Legacy.ContainsKey(entryGraphAuthoringId))
                        throw new InvalidOperationException($"Skill '{skill.SkillId}'无法从v6 Document恢复旧入口Graph。");
                    skill.ConfigureAuthoring(
                        skill.SkillId,
                        entryGraphAuthoringId,
                        skill.ActionProfile,
                        skill.ActionContext,
                        skill.SourceInputRequestId,
                        skill.ConsumeSourceInputRequest,
                        skill.TargetInputValueId,
                        skill.TargetKey);
                    changed = true;
                }
                if (changed)
                    EditorUtility.SetDirty(m_Definition);
            }

            void IndexSnapshot()
            {
                foreach (AgentSnapshotGraph graph in m_Snapshot.graphs ?? new List<AgentSnapshotGraph>())
                {
                    if (graph == null || string.IsNullOrEmpty(graph.graphAuthoringId))
                        continue;
                    m_SnapshotGraphs[graph.graphAuthoringId] = graph;
                    foreach (AgentSnapshotNode node in graph.nodes ?? new List<AgentSnapshotNode>())
                        if (node != null && !string.IsNullOrEmpty(node.elementAuthoringId))
                            m_SnapshotNodes[Key(graph.graphAuthoringId, node.elementAuthoringId)] = node;
                }
                foreach (AgentSnapshotTimeline timeline in m_Snapshot.timelines ?? new List<AgentSnapshotTimeline>())
                {
                    if (timeline == null || string.IsNullOrEmpty(timeline.timelineAuthoringId))
                        continue;
                    m_Timelines[timeline.timelineAuthoringId] = timeline;
                    foreach (AgentSnapshotTimelineCallSite call in timeline.callSites ?? new List<AgentSnapshotTimelineCallSite>())
                        if (call != null && !string.IsNullOrEmpty(call.nodeAuthoringId))
                            m_TimelineCalls[call.nodeAuthoringId] = call;
                }
                foreach (AgentSnapshotStateMachineSummary stateMachine in m_Snapshot.stateMachines ?? new List<AgentSnapshotStateMachineSummary>())
                    foreach (AgentSnapshotStateSummary state in stateMachine?.states ?? new List<AgentSnapshotStateSummary>())
                        foreach (AgentSnapshotLifecycleSummary lifecycle in state?.lifecycleTransitions ?? new List<AgentSnapshotLifecycleSummary>())
                            if (lifecycle != null && !string.IsNullOrEmpty(lifecycle.nodeAuthoringId))
                                m_Lifecycles[lifecycle.nodeAuthoringId] = lifecycle;
            }

            void PrepareIdentities()
            {
                foreach (CharacterSkillAuthoringDefinition skill in m_Definition.SkillDefinitions)
                    if (skill != null && !string.IsNullOrEmpty(skill.EntryGraphAuthoringId))
                        m_GraphIds[skill.EntryGraphAuthoringId] = BtsmtlSkillGraphAssetFactory.StableIdentity("local:" + skill.SkillId);
                foreach (string id in m_Legacy.Keys)
                    if (!m_GraphIds.ContainsKey(id))
                        m_GraphIds[id] = Local("graph", id);
                foreach (KeyValuePair<string, LegacyGraphFile> pair in m_Legacy)
                {
                    foreach (LegacyNode node in pair.Value.nodes ?? new List<LegacyNode>())
                        if (node != null && node.kind != "activate-action-instance")
                            m_NodeIds[Key(pair.Key, node.id)] = Local("node", pair.Key + ":" + node.id);
                    foreach (LegacyFlowEdge edge in pair.Value.flowEdges ?? new List<LegacyFlowEdge>())
                        m_EdgeIds[Key(pair.Key, edge.id)] = Local("edge", pair.Key + ":" + edge.id);
                    foreach (LegacyPropertyEdge edge in pair.Value.propertyEdges ?? new List<LegacyPropertyEdge>())
                        m_EdgeIds[Key(pair.Key, edge.id)] = Local("edge", pair.Key + ":" + edge.id);
                }
                foreach (AgentSnapshotBlackboardDeclaration declaration in m_Snapshot.blackboardDeclarations ?? new List<AgentSnapshotBlackboardDeclaration>())
                    if (declaration != null)
                    {
                        m_Blackboards[declaration.declarationId] = declaration;
                        m_DeclarationIds[Key(declaration.ownerId, declaration.declarationId)] = Local("decl", declaration.ownerId + ":" + declaration.declarationId);
                    }
                foreach (AgentSnapshotBlackboardDeclaration declaration in m_Blackboards.Values)
                    if (declaration != null && !m_DeclarationIds.ContainsKey(Key(declaration.ownerId, declaration.declarationId)))
                        m_DeclarationIds[Key(declaration.ownerId, declaration.declarationId)] = Local("decl", declaration.ownerId + ":" + declaration.declarationId);
                foreach (AgentSnapshotTimeline timeline in m_Timelines.Values)
                {
                    bool shared = IsSharedTimeline(timeline.timelineAuthoringId);
                    m_TimelineIds[timeline.timelineAuthoringId] = shared ? timeline.timelineAuthoringId : Local("timeline", timeline.timelineAuthoringId);
                    foreach (AgentSnapshotTimelineTrack track in timeline.tracks ?? new List<AgentSnapshotTimelineTrack>())
                    {
                        m_TrackIds[Key(timeline.timelineAuthoringId, track.trackAuthoringId)] = shared ? track.trackAuthoringId : Local("track", timeline.timelineAuthoringId + ":" + track.trackAuthoringId);
                        foreach (AgentSnapshotTimelineClip clip in track.clips ?? new List<AgentSnapshotTimelineClip>())
                            m_ClipIds[Key(timeline.timelineAuthoringId, clip.clipAuthoringId)] = shared ? clip.clipAuthoringId : Local("clip", timeline.timelineAuthoringId + ":" + clip.clipAuthoringId);
                    }
                }
                foreach (LegacyGraphFile graph in m_Legacy.Values)
                    foreach (LegacyFlowEdge edge in graph.flowEdges ?? new List<LegacyFlowEdge>())
                        if (!string.IsNullOrEmpty(edge.conditionGraph))
                            m_ConditionOwners[edge.conditionGraph] = Key(graph.id, edge.id);
                foreach (LegacyGraphFile graph in m_Legacy.Values.Where(value => value.kind == "RunnableTree"))
                {
                    AgentSnapshotGraph snapshot = RequireSnapshotGraph(graph.id);
                    Match match = Regex.Match(snapshot.path ?? string.Empty, "timeline:([^/]+)/track:([^/]+)/clip:([^/]+)");
                    string timelineId;
                    string trackId;
                    string clipId;
                    if (match.Success)
                    {
                        timelineId = match.Groups[1].Value;
                        trackId = match.Groups[2].Value;
                        clipId = match.Groups[3].Value;
                    }
                    else
                    {
                        timelineId = m_Timelines.Values.First(value => value.callSites.Any(call => call.nodeAuthoringId == graph.owner.entityId)).timelineAuthoringId;
                        List<AgentSnapshotTimelineTreeClip> candidates = m_Snapshot.timelineTreeClips
                            .Where(value => value != null && value.timelineAuthoringId == timelineId && value.treeName == snapshot.name)
                            .ToList();
                        if (candidates.Count != 1)
                            throw new InvalidOperationException($"TimelineBody Graph无法唯一匹配Timeline Clip：{graph.id} name={snapshot.name} candidates={candidates.Count}");
                        trackId = candidates[0].trackAuthoringId;
                        clipId = candidates[0].clipAuthoringId;
                    }
                    string timelineNode = m_Timelines[timelineId].callSites.First().nodeAuthoringId;
                    string parent = FindContainingGraph(timelineNode);
                    m_TimelineBodyGraphs[TimelineOwnerKey(timelineId, trackId, clipId)] = graph.id;
                    m_TimelineBodyParents[graph.id] = parent;
                    m_TimelineBodyNodes[graph.id] = timelineNode;
                    m_TimelineBodyTimelines[graph.id] = timelineId;
                    m_TimelineBodyTracks[graph.id] = trackId;
                    m_TimelineBodyClips[graph.id] = clipId;
                }
            }

            void CreateRoots()
            {
                foreach (CharacterSkillAuthoringDefinition skill in m_Definition.SkillDefinitions)
                {
                    if (skill == null)
                        continue;
                    string path = AgentSkillFlowAssetPaths.Root(m_Definition, "local:" + skill.SkillId);
                    bool ghostTarget = !File.Exists(path) && !File.Exists(path + ".meta") && !AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    if (ghostTarget && !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
                        AssetDatabase.DeleteAsset(path);
                    if (!ghostTarget)
                        AgentSkillFlowAssetPaths.RequireAvailable(path);
                    BtsmtlSkillFlowGraph graph = ScriptableObject.CreateInstance<BtsmtlSkillFlowGraph>();
                    graph.name = skill.SkillId + "Skill";
                    graph.ConfigureIdentity(m_GraphIds[skill.EntryGraphAuthoringId], BtsmtlSkillFlowGraphRole.Skill);
                    AssetDatabase.CreateAsset(graph, path);
                    m_Roots.Add(skill.SkillId, new RootAsset(graph, path));
                    Undo.RegisterCreatedObjectUndo(graph, "创建技能迁移根资产");
                    BtsmtlSkillFlowEditorMutation.Apply(graph, "初始化技能迁移根", () => BtsmtlSkillGraphAssetFactory.PopulateAnchors(graph), false);
                }
                AssetDatabase.SaveAssets();
            }

            void RollbackRoots()
            {
                foreach (RootAsset root in m_Roots.Values)
                    if (root.Graph)
                        AssetDatabase.DeleteAsset(root.Path);
                m_Roots.Clear();
            }

            AgentPackageSkillFlowDocument BuildDocument()
            {
                var document = new AgentPackageSkillFlowDocument();
                foreach (CharacterSkillAuthoringDefinition skill in m_Definition.SkillDefinitions)
                {
                    if (skill == null)
                        continue;
                    document.skills.Add(new AgentSnapshotSkillDefinition
                    {
                        skillId = skill.SkillId,
                        entryGraphAuthoringId = m_GraphIds[skill.EntryGraphAuthoringId],
                        actionProfileId = skill.ActionProfile?.ActionId ?? string.Empty,
                        actionProfileAssetPath = skill.ActionProfile ? AssetDatabase.GetAssetPath(skill.ActionProfile) : string.Empty,
                        actionProfileAssetGuid = skill.ActionProfile ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(skill.ActionProfile)) : string.Empty,
                        actionContext = skill.ActionContext ? skill.ActionContext.name : string.Empty,
                        actionContextAssetPath = skill.ActionContext ? AssetDatabase.GetAssetPath(skill.ActionContext) : string.Empty,
                        actionContextAssetGuid = skill.ActionContext ? AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(skill.ActionContext)) : string.Empty,
                        sourceInputRequestId = skill.SourceInputRequestId,
                        consumeSourceInputRequest = skill.ConsumeSourceInputRequest,
                        targetInputValueId = skill.TargetInputValueId,
                        targetKey = skill.TargetKey,
                        allowedFollowUpSkillIds = skill.AllowedFollowUpSkillIds.ToList(),
                        subgraphDependencies = new List<AgentSnapshotSkillSubgraphDependency>()
                    });
                }
                HashSet<string> closure = Closure();
                foreach (string oldId in closure.OrderBy(value => value, StringComparer.Ordinal))
                    document.graphs.Add(BuildGraph(oldId));
                foreach (AgentSnapshotTimeline timeline in m_Timelines.Values.Where(value => UsedByClosure(value, closure)).OrderBy(value => value.timelineAuthoringId, StringComparer.Ordinal))
                    document.timelines.Add(BuildTimeline(timeline, closure));
                document.layouts = document.graphs.Select(BuildLayout).ToList();
                return document;
            }

            HashSet<string> Closure()
            {
                var result = new HashSet<string>(StringComparer.Ordinal);
                var queue = new Queue<string>(m_Definition.SkillDefinitions.Where(value => value != null).Select(value => value.EntryGraphAuthoringId));
                while (queue.Count > 0)
                {
                    string id = queue.Dequeue();
                    if (!result.Add(id))
                        continue;
                    LegacyGraphFile graph = m_Legacy[id];
                    foreach (LegacyFlowEdge edge in graph.flowEdges ?? new List<LegacyFlowEdge>())
                        if (!string.IsNullOrEmpty(edge.conditionGraph))
                            queue.Enqueue(edge.conditionGraph);
                    foreach (LegacyNode node in graph.nodes ?? new List<LegacyNode>())
                        foreach (LegacyGraphFile child in m_Legacy.Values.Where(value => value.owner != null && value.owner.entityId == node.id))
                            queue.Enqueue(child.id);
                }
                return result;
            }

            bool UsedByClosure(AgentSnapshotTimeline timeline, ISet<string> closure) =>
                timeline.callSites.Any(value => closure.Contains(FindContainingGraph(value.nodeAuthoringId)));

            AgentPackageSkillFlowGraphFile BuildGraph(string oldId)
            {
                LegacyGraphFile legacy = m_Legacy[oldId];
                BtsmtlSkillFlowGraphRole role = Role(oldId, legacy);
                var graph = new AgentPackageSkillFlowGraphFile
                {
                    id = m_GraphIds[oldId],
                    role = role.ToString(),
                    name = RequireSnapshotGraph(oldId).name ?? oldId,
                    contentRevision = "legacy:" + oldId,
                    ownership = role == BtsmtlSkillFlowGraphRole.Skill
                        ? AgentGraphOwnership.RootAsset.ToString()
                        : role == BtsmtlSkillFlowGraphRole.TimelineBody &&
                          m_TimelineBodyTimelines.TryGetValue(oldId, out string timelineId) &&
                          IsSharedTimeline(timelineId)
                            ? AgentGraphOwnership.SharedAsset.ToString()
                            : AgentGraphOwnership.Inline.ToString(),
                    owner = BuildOwner(oldId, role),
                    asset = role == BtsmtlSkillFlowGraphRole.Skill ? m_Roots[SkillForRoot(oldId)].Reference : null
                };
                foreach (string anchor in RequiredAnchors(role))
                    graph.anchors.Add(new AgentPackageSkillGraphAnchor { kind = anchor, nodeId = Local("anchor", oldId + ":" + anchor) });
                AddBlackboards(graph, oldId);
                if (role == BtsmtlSkillFlowGraphRole.Skill)
                    BuildSkillRoot(graph, legacy);
                else
                    BuildRegularGraph(graph, legacy);
                return graph;
            }

            BtsmtlSkillFlowGraphRole Role(string oldId, LegacyGraphFile graph)
            {
                if (m_Definition.SkillDefinitions.Any(value => value != null && value.EntryGraphAuthoringId == oldId))
                    return BtsmtlSkillFlowGraphRole.Skill;
                return graph.kind switch
                {
                    "ConditionRuleGraph" => BtsmtlSkillFlowGraphRole.ConditionRule,
                    "StateMachineGraph" => BtsmtlSkillFlowGraphRole.StateMachine,
                    "StateBehaviorSubTree" => BtsmtlSkillFlowGraphRole.StateBody,
                    "RunnableTree" => BtsmtlSkillFlowGraphRole.TimelineBody,
                    _ => throw new InvalidOperationException($"不支持旧Graph类型：{graph.kind}")
                };
            }

            IReadOnlyList<string> RequiredAnchors(BtsmtlSkillFlowGraphRole role) => role switch
            {
                BtsmtlSkillFlowGraphRole.Skill => new[] { "@root" },
                BtsmtlSkillFlowGraphRole.StateMachine => new[] { "@enter", "@any", "@exit" },
                BtsmtlSkillFlowGraphRole.ConditionRule => new[] { "@result" },
                BtsmtlSkillFlowGraphRole.StateBody => new[] { "@onEnter", "@root", "@onExit" },
                BtsmtlSkillFlowGraphRole.TimelineBody => new[] { "@timelineEnable", "@root", "@timelineDisable", "@timelineDestroy" },
                _ => Array.Empty<string>()
            };

            void AddBlackboards(AgentPackageSkillFlowGraphFile graph, string oldId)
            {
                foreach (AgentSnapshotBlackboardDeclaration declaration in m_Blackboards.Values)
                {
                    if (declaration == null || !ShouldIncludeDeclaration(declaration, oldId))
                        continue;
                    AgentSnapshotBlackboardInputBinding inputBinding = string.IsNullOrWhiteSpace(declaration.inputBinding?.inputValueId) ? null : declaration.inputBinding;
                    AgentSnapshotBlackboardFactProjection factProjection = declaration.factProjection == null ||
                                                                             string.IsNullOrWhiteSpace(declaration.factProjection.kind) ||
                                                                             string.IsNullOrWhiteSpace(declaration.factProjection.windowType) ||
                                                                             string.IsNullOrWhiteSpace(declaration.factProjection.windowId)
                        ? null
                        : declaration.factProjection;
                    graph.blackboardDeclarations.Add(new AgentPackageSkillBlackboardDeclaration
                    {
                        id = LocalDeclaration(declaration.declarationId, oldId),
                        key = declaration.key,
                        valueType = ValueType(declaration.valueType),
                        scope = declaration.scope,
                        lifetime = declaration.lifetime,
                        category = declaration.categoryPath,
                        defaultValue = DefaultValue(declaration),
                        inputBinding = inputBinding,
                        factProjection = factProjection
                    });
                }
            }

            bool ShouldIncludeDeclaration(AgentSnapshotBlackboardDeclaration declaration, string graphId)
            {
                if (declaration.ownerId == graphId)
                    return true;
                if (!m_Definition.SkillDefinitions.Any(value => value != null && value.EntryGraphAuthoringId == graphId))
                    return false;
                return ReferencedDeclarations(graphId).Contains(declaration.declarationId) && !m_GraphIds.ContainsKey(declaration.ownerId);
            }

            HashSet<string> ReferencedDeclarations(string rootGraphId)
            {
                var result = new HashSet<string>(StringComparer.Ordinal);
                var queue = new Queue<string>();
                queue.Enqueue(rootGraphId);
                var visited = new HashSet<string>(StringComparer.Ordinal);
                while (queue.Count > 0)
                {
                    string graphId = queue.Dequeue();
                    if (!visited.Add(graphId) || !m_Legacy.TryGetValue(graphId, out LegacyGraphFile graph))
                        continue;
                    foreach (LegacyNode node in graph.nodes ?? new List<LegacyNode>())
                    {
                        AgentSnapshotNode snapshot = SnapshotNode(graphId, node.id);
                        if (!string.IsNullOrEmpty(snapshot.blackboardDeclarationId)) result.Add(snapshot.blackboardDeclarationId);
                        if (!string.IsNullOrEmpty(snapshot.targetSnapshotBlackboardDeclarationId)) result.Add(snapshot.targetSnapshotBlackboardDeclarationId);
                        string exposed = (node.properties?["exposedProperty"] as JObject)?.Value<string>("declarationId") ?? snapshot.exposedProperty?.declarationAuthoringId;
                        if (!string.IsNullOrEmpty(exposed)) result.Add(exposed);
                        foreach (LegacyGraphFile child in m_Legacy.Values.Where(value => value.owner?.entityId == node.id)) queue.Enqueue(child.id);
                    }
                    foreach (LegacyFlowEdge edge in graph.flowEdges ?? new List<LegacyFlowEdge>())
                        if (!string.IsNullOrEmpty(edge.conditionGraph)) queue.Enqueue(edge.conditionGraph);
                }
                return result;
            }

            void BuildSkillRoot(AgentPackageSkillFlowGraphFile graph, LegacyGraphFile legacy)
            {
                var branches = legacy.flowEdges.Where(value => value.from?.node == "@root" || value.from?.node == "@onEnter" || value.from?.node == "@onExit").ToList();
                string parallelId = Local("node", legacy.id + ":root-parallel");
                var parallel = new AgentPackageSkillFlowNode
                {
                    id = parallelId,
                    capability = "parallel",
                    name = "Legacy Lifecycle Branches",
                    properties = new JObject { ["mode"] = BtsmtlSkillParallelMode.UpdateAll.ToString(), ["steps"] = new JArray() },
                    values = new JObject()
                };
                int index = 0;
                foreach (LegacyFlowEdge edge in branches)
                {
                    if (!ValidTarget(legacy.id, edge.to?.node))
                        continue;
                    string stepId = Local("step", legacy.id + ":root:" + edge.id);
                    ((JArray)parallel.properties["steps"]).Add(JObject.FromObject(Step(stepId, edge.from.node.Substring(1), edge.conditionGraph, index++, edge.abortPolicy)));
                    graph.edges.Add(FlowEdge(legacy.id, edge.id, parallelId, stepId, MapNode(legacy.id, edge.to.node), edge.to.port));
                }
                graph.nodes.Add(parallel);
                graph.edges.Add(FlowEdge(legacy.id, legacy.id + ":root-entry", "@root", "Output", parallelId, "Input"));
                AddNodes(graph, legacy);
                AddEdges(graph, legacy, true);
            }

            void BuildRegularGraph(AgentPackageSkillFlowGraphFile graph, LegacyGraphFile legacy)
            {
                AddNodes(graph, legacy);
                AddEdges(graph, legacy, false);
                if (graph.role == BtsmtlSkillFlowGraphRole.StateMachine.ToString())
                    foreach (AgentPackageSkillGraphAnchor anchor in graph.anchors.Where(value => value.kind == "@enter" || value.kind == "@any"))
                        anchor.steps = Steps(legacy, anchor.kind);
            }

            void AddNodes(AgentPackageSkillFlowGraphFile graph, LegacyGraphFile legacy)
            {
                foreach (LegacyNode legacyNode in legacy.nodes ?? new List<LegacyNode>())
                {
                    if (legacyNode == null || legacyNode.kind == "activate-action-instance")
                        continue;
                    AgentSnapshotNode snapshot = SnapshotNode(legacy.id, legacyNode.id);
                    string capability = Capability(legacyNode, snapshot);
                    var node = new AgentPackageSkillFlowNode
                    {
                        id = m_NodeIds[Key(legacy.id, legacyNode.id)],
                        capability = capability,
                        name = legacyNode.name,
                        properties = NodeProperties(legacy.id, legacyNode, snapshot),
                        values = new JObject()
                    };
                    if (legacyNode.kind == "exposed-property" && legacyNode.properties?["exposedProperty"] is JObject exposed && exposed.Value<string>("mode") == "Set")
                        node.values["m_Value"] = exposed["value"]?.DeepClone();
                    graph.nodes.Add(node);
                }
            }

            void AddEdges(AgentPackageSkillFlowGraphFile graph, LegacyGraphFile legacy, bool rootHandled)
            {
                foreach (LegacyNode node in legacy.nodes.Where(value => value != null && IsComposite(value.kind)))
                {
                    AgentPackageSkillFlowNode target = graph.nodes.FirstOrDefault(value => value.id == m_NodeIds[Key(legacy.id, node.id)]);
                    if (target != null)
                        target.properties["steps"] = new JArray(Steps(legacy, node.id).Select(value => JObject.FromObject(value)));
                }
                foreach (LegacyFlowEdge edge in legacy.flowEdges ?? new List<LegacyFlowEdge>())
                {
                    string source = edge.from?.node ?? string.Empty;
                    if (rootHandled && (source == "@root" || source == "@onEnter" || source == "@onExit"))
                        continue;
                    if (!ValidTarget(legacy.id, source) || !ValidTarget(legacy.id, edge.to?.node) || IsActivation(legacy.id, source) || IsActivation(legacy.id, edge.to.node))
                        continue;
                    string port = IsCompositeSource(legacy, source) ? StepId(legacy.id, edge.id) : edge.from.port;
                    graph.edges.Add(FlowEdge(legacy.id, edge.id, MapNode(legacy.id, source), port, MapNode(legacy.id, edge.to.node), edge.to.port));
                }
                foreach (LegacyPropertyEdge edge in legacy.propertyEdges ?? new List<LegacyPropertyEdge>())
                {
                    if (!ValidTarget(legacy.id, edge.from?.node) || !ValidTarget(legacy.id, edge.to?.node) || IsActivation(legacy.id, edge.from.node) || IsActivation(legacy.id, edge.to.node))
                        continue;
                    graph.edges.Add(new AgentPackageSkillFlowEdge
                    {
                        id = m_EdgeIds[Key(legacy.id, edge.id)],
                        kind = "value",
                        from = new AgentPackageSkillFlowEdgeEndpoint { node = MapNode(legacy.id, edge.from.node), port = ValuePort(legacy.id, edge.from.node, edge.from.port, false) },
                        to = new AgentPackageSkillFlowEdgeEndpoint { node = MapNode(legacy.id, edge.to.node), port = ValuePort(legacy.id, edge.to.node, edge.to.port, true) }
                    });
                }
            }

            List<AgentPackageSkillFlowStep> Steps(LegacyGraphFile graph, string source)
            {
                var result = new List<AgentPackageSkillFlowStep>();
                foreach (LegacyFlowEdge edge in graph.flowEdges ?? new List<LegacyFlowEdge>())
                    if (edge.from?.node == source && ValidTarget(graph.id, edge.to?.node))
                        result.Add(Step(StepId(graph.id, edge.id), TargetName(graph.id, edge.to.node), edge.conditionGraph, edge.flowOrder, edge.abortPolicy));
                return result;
            }

            AgentPackageSkillFlowStep Step(string id, string name, string condition, int priority, string abort) => new()
            {
                id = id,
                name = name ?? string.Empty,
                conditionGraphId = string.IsNullOrEmpty(condition) ? string.Empty : m_GraphIds[condition],
                priority = priority,
                abortPolicy = abort ?? ProgramAbortPolicy.None.ToString()
            };

            JObject NodeProperties(string graphId, LegacyNode legacy, AgentSnapshotNode snapshot)
            {
                var value = new JObject();
                if (IsComposite(legacy.kind))
                    value["steps"] = new JArray(Steps(m_Legacy[graphId], legacy.id).Select(step => JObject.FromObject(step)));
                if (legacy.kind == "loop") value["stopType"] = snapshot.loopStopType ?? BtsmtlSkillLoopStopType.None.ToString();
                if (legacy.kind == "state-exit-cause") value["cause"] = snapshot.stateExitCause ?? BtsmtlSkillStateExitCause.StateTransition.ToString();
                if (legacy.kind is "character-input-bool" or "character-input-float" or "character-input-vector2" or "character-input-vector2-magnitude" or "character-action-request")
                    value["inputId"] = snapshot.inputId ?? snapshot.requestId ?? string.Empty;
                if (legacy.kind == "action-context-active") value["actionContext"] = ContextReference(snapshot.actionContextId, graphId);
                if (legacy.kind == "action-window-active") value["windowType"] = snapshot.windowType ?? string.Empty;
                if (legacy.kind == "can-activate-action")
                {
                    value["actionProfile"] = ProfileReference(legacy.properties?.Value<string>("actionProfileId") ?? snapshot.actionProfileId);
                    string targetDeclaration = legacy.properties?.Value<string>("targetSnapshotBlackboardDeclarationId") ?? snapshot.targetSnapshotBlackboardDeclarationId;
                    if (!m_Blackboards.ContainsKey(targetDeclaration ?? string.Empty))
                        targetDeclaration = string.Empty;
                    value["targetSnapshot"] = string.IsNullOrEmpty(targetDeclaration)
                        ? new JObject { ["id"] = string.Empty, ["ownerId"] = string.Empty }
                        : new JObject { ["id"] = LocalDeclaration(targetDeclaration, graphId), ["ownerId"] = DeclarationOwner(targetDeclaration, graphId) };
                }
                if (legacy.kind == "submit-action-lifecycle")
                {
                    m_Lifecycles.TryGetValue(legacy.id, out AgentSnapshotLifecycleSummary lifecycle);
                    value["actionContext"] = ContextReference(NodeContextAsset(snapshot), graphId);
                    value["transitionType"] = lifecycle?.transitionType ?? ActionLifecycleTransitionType.Complete.ToString();
                    value["reason"] = lifecycle?.reason ?? legacy.name ?? string.Empty;
                }
                if (legacy.kind == "pipeline-blackboard-bool" || legacy.kind == "pipeline-blackboard-float")
                {
                    value["declarationId"] = LocalDeclaration(snapshot.blackboardDeclarationId, graphId);
                    value["ownerId"] = DeclarationOwner(snapshot.blackboardDeclarationId, graphId);
                    value["valueType"] = legacy.kind == "pipeline-blackboard-bool" ? "bool" : "float";
                }
                if (legacy.kind == "exposed-property")
                {
                    JObject exposed = legacy.properties?["exposedProperty"] as JObject;
                    string declaration = exposed?.Value<string>("declarationId") ?? snapshot.exposedProperty?.declarationAuthoringId;
                    value["declarationId"] = LocalDeclaration(declaration, graphId);
                    value["ownerId"] = DeclarationOwner(declaration, graphId);
                    value["valueType"] = ValueType(exposed?.Value<string>("valueType") ?? snapshot.exposedProperty?.valueType);
                    value["accessMode"] = exposed?.Value<string>("mode") == "Set" ? "set" : "get";
                }
                if (legacy.kind == "state-machine") value["graphId"] = ChildGraph(graphId, legacy.id, "stateMachine");
                if (legacy.kind == "state") value["bodyGraphId"] = ChildGraph(graphId, legacy.id, "body");
                if (legacy.kind == "timeline")
                {
                    string timeline = TimelineForNode(legacy.id);
                    value["timelineId"] = m_TimelineIds[timeline];
                    value["timelineOwnership"] = IsSharedTimeline(timeline) ? BtsmtlSkillTimelineOwnership.Shared.ToString() : BtsmtlSkillTimelineOwnership.Private.ToString();
                    value["actionContext"] = ContextReference(NodeContextAsset(snapshot), graphId);
                    value["playbackMode"] = m_TimelineCalls.TryGetValue(legacy.id, out AgentSnapshotTimelineCallSite call) ? call.playbackMode : TimelinePlaybackMode.Once.ToString();
                }
                if (legacy.kind == "locomotion-input-motion")
                {
                    value["moveSpeed"] = snapshot.moveSpeed;
                    value["displacementMode"] = snapshot.displacementMode;
                    value["turnSpeedDegrees"] = snapshot.turnSpeedDegrees;
                    value["cameraRelative"] = snapshot.cameraRelative;
                    value["executionMode"] = snapshot.executionMode;
                    value["durationSeconds"] = snapshot.durationSeconds;
                }
                return value;
            }

            string Capability(LegacyNode node, AgentSnapshotNode snapshot) => node.kind == "compare" ? CompareKind(snapshot) : node.kind == "exposed-property" ? "exposed-property" : node.kind;

            string CompareKind(AgentSnapshotNode node)
            {
                string type = node.propertyPorts.FirstOrDefault(value => value != null && value.portId == "m_InputValue1")?.valueType;
                string prefix = type == typeof(int).FullName ? "compare.int32." : "compare.number.";
                string comparison = (node.compareType ?? "Equal").ToLowerInvariant().Replace("notequal", "not-equal").Replace("lessequal", "less-equal").Replace("greaterequal", "greater-equal");
                return prefix + comparison;
            }

            AgentPackageSkillTimelineFile BuildTimeline(AgentSnapshotTimeline source, ISet<string> closure)
            {
                AgentSnapshotTimelineCallSite call = source.callSites.First(value => closure.Contains(FindContainingGraph(value.nodeAuthoringId)));
                string ownerGraph = FindContainingGraph(call.nodeAuthoringId);
                bool shared = IsSharedTimeline(source.timelineAuthoringId);
                var result = new AgentPackageSkillTimelineFile
                {
                    id = m_TimelineIds[source.timelineAuthoringId],
                    name = source.name,
                    ownership = shared ? BtsmtlSkillTimelineOwnership.Shared.ToString() : BtsmtlSkillTimelineOwnership.Private.ToString(),
                    asset = shared ? SharedTimelineReference(source.timelineAuthoringId) : null,
                    ownerGraphId = m_GraphIds[ownerGraph],
                    ownerNodeId = m_NodeIds[Key(ownerGraph, call.nodeAuthoringId)]
                };
                foreach (AgentSnapshotTimelineCallSite callSite in source.callSites ?? new List<AgentSnapshotTimelineCallSite>())
                {
                    string graph = FindContainingGraph(callSite.nodeAuthoringId);
                    if (!closure.Contains(graph))
                        continue;
                    result.callSites.Add(new AgentPackageSkillTimelineCallSite
                    {
                        graphId = m_GraphIds[graph],
                        nodeId = m_NodeIds[Key(graph, callSite.nodeAuthoringId)],
                        playbackMode = callSite.playbackMode
                    });
                }
                foreach (AgentSnapshotTimelineSection section in source.sections ?? new List<AgentSnapshotTimelineSection>())
                    result.sections.Add(new AgentPackageSkillTimelineSection
                    {
                        id = shared ? section.sectionAuthoringId : Local("section", source.timelineAuthoringId + ":" + section.sectionAuthoringId),
                        name = section.name,
                        frame = section.frame
                    });
                foreach (AgentSnapshotTimelineTrack track in source.tracks ?? new List<AgentSnapshotTimelineTrack>())
                {
                    var targetTrack = new AgentPackageSkillTimelineTrack
                    {
                        id = m_TrackIds[Key(source.timelineAuthoringId, track.trackAuthoringId)],
                        kind = TrackKind(track.typeName),
                        name = track.name,
                        animationChannelId = track.animationChannelId ?? string.Empty
                    };
                    foreach (AgentSnapshotTimelineClip clip in track.clips ?? new List<AgentSnapshotTimelineClip>())
                        targetTrack.clips.Add(BuildClip(source.timelineAuthoringId, track.trackAuthoringId, clip));
                    result.tracks.Add(targetTrack);
                }
                return result;
            }

            AgentPackageSkillTimelineClip BuildClip(string timelineId, string trackId, AgentSnapshotTimelineClip source)
            {
                string targetId = m_ClipIds[Key(timelineId, source.clipAuthoringId)];
                var result = new AgentPackageSkillTimelineClip
                {
                    id = targetId,
                    kind = ClipKind(source.typeName),
                    startFrame = source.startFrame,
                    endFrame = source.endFrame,
                    otherEaseInFrame = source.otherEaseInFrame,
                    otherEaseOutFrame = source.otherEaseOutFrame,
                    selfEaseInFrame = source.selfEaseInFrame,
                    selfEaseOutFrame = source.selfEaseOutFrame,
                    clipInFrame = source.clipInFrame,
                    animationClip = source.animationClip
                };
                if (source.typeName?.EndsWith("AnimationClip", StringComparison.Ordinal) == true)
                    result.properties["extraPolationMode"] = source.extraPolationMode;
                if (source.typeName?.EndsWith("ActionCueClip", StringComparison.Ordinal) == true)
                {
                    result.properties["cueId"] = source.cueId;
                    result.properties["cueType"] = source.cueType;
                }
                if (source.typeName?.EndsWith("MotionCurveClip", StringComparison.Ordinal) == true)
                {
                    result.properties["curveId"] = source.curveId;
                    result.properties["curveEndFrame"] = source.curveEndFrame;
                    result.properties["space"] = source.motionSpace;
                    result.properties["channel"] = source.motionChannel;
                    result.properties["blendMode"] = source.motionBlendMode;
                    result.properties["priority"] = source.motionPriority;
                    result.properties["consumeLowerChannels"] = source.consumeLowerChannels;
                }
                if (source.typeName?.EndsWith("MotionWarpClip", StringComparison.Ordinal) == true)
                {
                    result.properties["sourceMotionClipId"] = string.IsNullOrEmpty(source.sourceMotionClipAuthoringId) ? string.Empty : m_ClipIds[Key(timelineId, source.sourceMotionClipAuthoringId)];
                    result.properties["translationMode"] = source.translationMode;
                    result.properties["targetOffsetSpace"] = source.targetOffsetSpace;
                    result.properties["rotationMode"] = source.rotationMode;
                    result.properties["rotationMethod"] = source.rotationMethod;
                    result.properties["targetPlanarOffset"] = new JObject { ["x"] = source.targetPlanarOffset?.x ?? 0f, ["y"] = source.targetPlanarOffset?.y ?? 0f };
                    result.properties["targetYawOffsetDegrees"] = source.targetYawOffsetDegrees;
                    result.properties["maxTotalPositionCorrection"] = source.maxTotalPositionCorrection;
                    result.properties["maxTotalYawCorrectionDegrees"] = source.maxTotalYawCorrectionDegrees;
                    result.properties["maximumYawRateDegreesPerSecond"] = source.maximumYawRateDegreesPerSecond;
                    result.properties["limitPolicy"] = source.limitPolicy;
                }
                if (source.typeName?.EndsWith("TreeClip", StringComparison.Ordinal) == true)
                {
                    string body = m_TimelineBodyGraphs[TimelineOwnerKey(timelineId, trackId, source.clipAuthoringId)];
                    result.treeGraphId = m_GraphIds[body];
                    result.treeOwnership = TimelineTreeOwnership.AssetGraph.ToString();
                    result.treePhase = m_Snapshot.timelineTreeClips.FirstOrDefault(value => value != null && value.timelineAuthoringId == timelineId && value.trackAuthoringId == trackId && value.clipAuthoringId == source.clipAuthoringId)?.phase ?? TimelineTreeExecutionPhase.Commit.ToString();
                }
                foreach (AgentSnapshotTimelineCurveChannel curve in source.curveChannels ?? new List<AgentSnapshotTimelineCurveChannel>())
                {
                    TimelineCurveChannelCatalog.TryGet(curve.channelId, out TimelineCurveChannelDescriptor descriptor);
                    bool invalid = descriptor == null ||
                                   curve.timeDomain != descriptor.TimeDomain.ToString() ||
                                   curve.bounded != descriptor.ValueDomain.IsBounded ||
                                   curve.minimum != descriptor.ValueDomain.Minimum ||
                                   curve.maximum != descriptor.ValueDomain.Maximum ||
                                   curve.zero != descriptor.ValueDomain.Zero ||
                                   curve.unit != descriptor.ValueDomain.Unit ||
                                   curve.keys == null || curve.keys.Count == 0;
                    if (invalid)
                        throw new InvalidOperationException($"迁移曲线源无效：timeline={timelineId} clip={source.clipAuthoringId} channel={curve.channelId} actual={curve.timeDomain}/{curve.bounded}/{curve.minimum}/{curve.maximum}/{curve.zero}/{curve.unit}/{curve.preWrapMode}/{curve.postWrapMode}/{curve.keys?.Count ?? 0} expected={descriptor?.TimeDomain.ToString()}/{descriptor?.ValueDomain.IsBounded}/{descriptor?.ValueDomain.Minimum}/{descriptor?.ValueDomain.Maximum}/{descriptor?.ValueDomain.Zero}/{descriptor?.ValueDomain.Unit}");
                    result.curves.Add(new AgentPackageCurve
                    {
                        clipId = targetId,
                        channelId = curve.channelId,
                        timeDomain = curve.timeDomain,
                        bounded = curve.bounded,
                        minimum = curve.minimum,
                        maximum = curve.maximum,
                        zero = curve.zero,
                        unit = curve.unit,
                        preWrapMode = curve.preWrapMode,
                        postWrapMode = curve.postWrapMode,
                        keys = curve.keys
                    });
                }
                return result;
            }

            AgentPackageSkillFlowGraphLayoutFile BuildLayout(AgentPackageSkillFlowGraphFile graph)
            {
                var result = new AgentPackageSkillFlowGraphLayoutFile { graphId = graph.id };
                foreach (AgentPackageSkillFlowNode node in graph.nodes)
                {
                    KeyValuePair<string, string> pair = m_NodeIds.FirstOrDefault(value => value.Value == node.id);
                    Vector2 position = Vector2.zero;
                    if (!string.IsNullOrEmpty(pair.Key))
                    {
                        string[] parts = pair.Key.Split('\0');
                        AgentSnapshotNode snapshot = SnapshotNode(parts[0], parts[1]);
                        if (snapshot.position != null)
                            position = new Vector2(snapshot.position.x, snapshot.position.y);
                    }
                    result.nodes.Add(new AgentPackageSkillFlowNodeLayout { id = node.id, x = position.x, y = position.y });
                }
                return result;
            }

            void ApplyDocument(AgentPackageSkillFlowDocument document)
            {
                var validation = new AgentCompileReport
                {
                    schemaVersion = AgentAuthoringSchema.Version,
                    domain = AgentAuthoringSchema.CharacterControllerDomain,
                    rootIdentity = m_Snapshot.rootIdentity,
                    success = true
                };
                if (!AgentSkillFlowDocumentMapper.Validate(document, validation) || validation.HasErrors())
                    throw new InvalidOperationException(string.Join(Environment.NewLine, validation.messages.Select(value => value.message)));
                var mutation = new AgentSetSkillFlowDocumentMutation("legacy-skill-migration", "editable.skills", document);
                var plan = new AgentMutationPlan(new AgentMutation[] { mutation }, AgentAuthoringSchema.CharacterControllerDomain, m_Snapshot.rootIdentity, m_Snapshot.sourceRevision ?? string.Empty);
                var report = new AgentCompileReport
                {
                    schemaVersion = AgentAuthoringSchema.Version,
                    domain = AgentAuthoringSchema.CharacterControllerDomain,
                    rootIdentity = m_Snapshot.rootIdentity,
                    success = true
                };
                var session = new AgentMutationSession(m_Definition, m_Snapshot, plan, report, true, null, true);
                if (!session.Initialize())
                    throw new InvalidOperationException(string.Join(Environment.NewLine, report.messages.Select(value => value.message)));
                var handler = new AgentSkillFlowDocumentMutationHandler();
                if (!handler.Preflight(session, mutation))
                    throw new InvalidOperationException(string.Join(Environment.NewLine, report.messages.Select(value => value.message)));
                handler.Apply(session, mutation);
                if (report.HasErrors())
                    throw new InvalidOperationException(string.Join(Environment.NewLine, report.messages.Select(value => value.message)));
                foreach (UnityEngine.Object owner in session.TouchedOwners)
                    if (owner)
                        EditorUtility.SetDirty(owner);
            }

            AgentPackageSkillGraphOwner BuildOwner(string oldId, BtsmtlSkillFlowGraphRole role)
            {
                if (role == BtsmtlSkillFlowGraphRole.Skill)
                    return new AgentPackageSkillGraphOwner { kind = "skill-root", skillId = SkillForRoot(oldId) };
                if (role == BtsmtlSkillFlowGraphRole.ConditionRule)
                {
                    string owner = m_ConditionOwners[oldId];
                    int split = owner.IndexOf('\0');
                    string parent = owner.Substring(0, split);
                    string edgeId = owner.Substring(split + 1);
                    LegacyFlowEdge edge = m_Legacy[parent].flowEdges.First(value => value.id == edgeId);
                    return new AgentPackageSkillGraphOwner
                    {
                        kind = "step",
                        graphId = m_GraphIds[parent],
                        nodeId = edge.from.node.StartsWith("@", StringComparison.Ordinal) ? Local("anchor", parent + ":" + edge.from.node) : m_NodeIds[Key(parent, edge.from.node)],
                        referenceKey = StepId(parent, edge.id)
                    };
                }
                if (role == BtsmtlSkillFlowGraphRole.TimelineBody)
                    return new AgentPackageSkillGraphOwner
                    {
                        kind = "timeline-clip",
                        graphId = m_GraphIds[m_TimelineBodyParents[oldId]],
                        nodeId = m_NodeIds[Key(m_TimelineBodyParents[oldId], m_TimelineBodyNodes[oldId])],
                        referenceKey = "timeline",
                        timelineId = m_TimelineIds[m_TimelineBodyTimelines[oldId]],
                        trackId = m_TrackIds[Key(m_TimelineBodyTimelines[oldId], m_TimelineBodyTracks[oldId])],
                        clipId = m_ClipIds[Key(m_TimelineBodyTimelines[oldId], m_TimelineBodyClips[oldId])]
                    };
                string parentGraph = FindContainingGraph(m_Legacy[oldId].owner?.entityId);
                return new AgentPackageSkillGraphOwner
                {
                    kind = "node",
                    graphId = m_GraphIds[parentGraph],
                    nodeId = m_NodeIds[Key(parentGraph, m_Legacy[oldId].owner.entityId)],
                    referenceKey = m_Legacy[oldId].owner.slot == "stateMachine" ? "stateMachine" : "body"
                };
            }

            string SkillForRoot(string oldId) => m_Definition.SkillDefinitions.First(value => value != null && value.EntryGraphAuthoringId == oldId).SkillId;
            bool ValidTarget(string graphId, string id) => !string.IsNullOrEmpty(id) && (id.StartsWith("@", StringComparison.Ordinal) || m_NodeIds.ContainsKey(Key(graphId, id)));
            bool IsActivation(string graphId, string id) => m_Legacy[graphId].nodes.Any(value => value != null && value.id == id && value.kind == "activate-action-instance");
            bool IsComposite(string kind) => kind == "sequence" || kind == "selector" || kind == "parallel" || kind == "state";
            bool IsCompositeSource(LegacyGraphFile graph, string source) => source == "@enter" || source == "@any" || graph.nodes.Any(value => value != null && value.id == source && IsComposite(value.kind));
            string MapNode(string graphId, string node) => node.StartsWith("@", StringComparison.Ordinal) ? node : m_NodeIds[Key(graphId, node)];
            string ValuePort(string graphId, string nodeId, string port, bool input)
            {
                string kind = m_Legacy[graphId].nodes.FirstOrDefault(value => value != null && value.id == nodeId)?.kind;
                if (kind == "and" || kind == "or") return port == "m_Input1" ? "a" : port == "m_Input2" ? "b" : port == "m_Output" ? "Value" : port;
                if (kind == "not") return port == "m_Input" ? "value" : port == "m_Output" ? "Value" : port;
                if (kind == "compare") return port == "m_InputValue1" ? "a" : port == "m_InputValue2" ? "b" : port == "m_Result" ? "Value" : port;
                if (kind == "exposed-property") return port == "m_Value" && input ? "m_Value" : port == "m_Value" ? "m_Output" : port;
                return port;
            }
            AgentPackageSkillFlowEdge FlowEdge(string graph, string oldEdge, string from, string fromPort, string to, string toPort) => new()
            {
                id = m_EdgeIds.TryGetValue(Key(graph, oldEdge), out string value) ? value : Local("edge", graph + ":" + oldEdge),
                kind = "flow",
                from = new AgentPackageSkillFlowEdgeEndpoint { node = from, port = fromPort },
                to = new AgentPackageSkillFlowEdgeEndpoint { node = to, port = toPort }
            };
            AgentSnapshotGraph RequireSnapshotGraph(string id)
            {
                if (m_SnapshotGraphs.TryGetValue(id, out AgentSnapshotGraph graph))
                    return graph;
                if (!m_Legacy.ContainsKey(id))
                    throw new InvalidOperationException($"旧Graph快照缺失：{id}");
                return BuildMissingTimelineTreeSnapshot(id);
            }

            AgentSnapshotGraph BuildMissingTimelineTreeSnapshot(string id)
            {
                LegacyGraphFile legacy = m_Legacy[id];
                string key = legacy.nodes?.FirstOrDefault(value => value != null)?.name ?? id;
                if (key.StartsWith("Set ", StringComparison.Ordinal))
                    key = key.Substring(4);
                string owner = legacy.owner?.entityId ?? string.Empty;
                List<AgentSnapshotTimelineTreeClip> candidates = m_LegacyTimelineTreeClips
                    .Where(value => value != null &&
                                    value.timelineNodePath?.Contains($"/node:{owner}/timeline", StringComparison.Ordinal) == true &&
                                    (string.Equals(value.treeName, key, StringComparison.Ordinal) ||
                                     string.Equals(value.treeName, "Decision " + key, StringComparison.Ordinal) ||
                                     value.treeName?.EndsWith(key, StringComparison.Ordinal) == true))
                    .ToList();
                if (candidates.Count != 1)
                    throw new InvalidOperationException($"旧Timeline Tree Graph无法唯一匹配：{id} owner={owner} key={key} candidates={candidates.Count}");
                AgentSnapshotTimelineTreeClip clip = candidates[0];
                var graph = new AgentSnapshotGraph
                {
                    graphAuthoringId = id,
                    path = $"timeline:{clip.timelineAuthoringId}/track:{clip.trackAuthoringId}/clip:{clip.clipAuthoringId}",
                    name = clip.treeName,
                    kind = legacy.kind,
                    ownership = AgentGraphOwnership.Inline.ToString(),
                    ownerElementAuthoringId = owner
                };
                foreach (LegacyNode node in legacy.nodes ?? new List<LegacyNode>())
                {
                    var snapshot = new AgentSnapshotNode
                    {
                        elementAuthoringId = node.id,
                        typeName = node.kind,
                        displayName = node.name,
                        nodeTypeDisplayName = node.kind,
                        position = new AgentSnapshotVector2()
                    };
                    if (node.properties?["exposedProperty"] is JObject exposed)
                        snapshot.exposedProperty = new AgentSnapshotExposedProperty
                        {
                            mode = exposed.Value<string>("mode"),
                            declarationAuthoringId = exposed.Value<string>("declarationId"),
                            valueType = exposed.Value<string>("valueType"),
                            value = exposed["value"]
                        };
                    graph.nodes.Add(snapshot);
                    m_SnapshotNodes[Key(id, node.id)] = snapshot;
                }
                m_SnapshotGraphs[id] = graph;
                return graph;
            }
            AgentSnapshotNode SnapshotNode(string graph, string node) => m_SnapshotNodes.TryGetValue(Key(graph, node), out AgentSnapshotNode value) ? value : throw new InvalidOperationException($"旧Node快照缺失：{graph}/{node}");
            string TargetName(string graph, string node) => node.StartsWith("@", StringComparison.Ordinal) ? node : m_Legacy[graph].nodes.FirstOrDefault(value => value != null && value.id == node)?.name ?? node;
            string StepId(string graph, string edge) => Local("step", graph + ":" + edge);
            string ChildGraph(string parent, string node, string slot)
            {
                LegacyGraphFile child = m_Legacy.Values.FirstOrDefault(value => value.owner?.entityId == node && value.owner.slot == slot);
                return child == null ? throw new InvalidOperationException($"技能子Graph缺失：{parent}/{node}/{slot}") : m_GraphIds[child.id];
            }
            string TimelineForNode(string node) => m_Timelines.Values.First(value => value.callSites.Any(call => call.nodeAuthoringId == node)).timelineAuthoringId;
            string FindContainingGraph(string node) => m_Legacy.Values.FirstOrDefault(value => value.nodes.Any(item => item != null && item.id == node))?.id ?? string.Empty;
            string NodeContextAsset(AgentSnapshotNode node) => node.assetReferences?.FirstOrDefault(value => value != null && value.assetType == typeof(ActionContextSlot).FullName)?.assetGuid ?? node.actionContextId ?? string.Empty;
            string ValueType(string name) => name switch
            {
                "System.Boolean" => "bool",
                "System.Int32" => "int",
                "System.Single" => "float",
                "System.String" => "string",
                "UnityEngine.Vector2" => "vector2",
                "UnityEngine.Vector3" => "vector3",
                "ThirdPersonCharacter.ActionSystem.ActionTargetSnapshot" => "action-target-snapshot",
                "ThirdPersonSimulation.ActionTargetSnapshot" => "action-target-snapshot",
                _ => throw new InvalidOperationException($"未登记技能值类型：{name}")
            };

            JToken DefaultValue(AgentSnapshotBlackboardDeclaration declaration)
            {
                string valueType = ValueType(declaration.valueType);
                if (valueType == "action-target-snapshot")
                    return new JObject
                    {
                        ["targetId"] = declaration.defaultValue is JObject target ? target.Value<string>("targetId") ?? string.Empty : string.Empty,
                        ["x"] = 0f,
                        ["y"] = 0f,
                        ["z"] = 0f,
                        ["rx"] = 0f,
                        ["ry"] = 0f,
                        ["rz"] = 0f,
                        ["rw"] = 1f
                    };
                return declaration.defaultValue?.DeepClone();
            }
            string DeclarationOwner(string declaration, string fallback)
            {
                AgentSnapshotBlackboardDeclaration value = m_Blackboards.TryGetValue(declaration ?? string.Empty, out AgentSnapshotBlackboardDeclaration declarationValue) ? declarationValue : null;
                string owner = string.IsNullOrEmpty(value?.ownerId) ? fallback : value.ownerId;
                return m_GraphIds.TryGetValue(owner, out string mapped) ? mapped : m_GraphIds[RootEntryGraph(fallback)];
            }
            string LocalDeclaration(string declaration, string fallback)
            {
                AgentSnapshotBlackboardDeclaration value = m_Blackboards.TryGetValue(declaration ?? string.Empty, out AgentSnapshotBlackboardDeclaration declarationValue) ? declarationValue : null;
                string owner = string.IsNullOrEmpty(value?.ownerId) ? fallback : value.ownerId;
                if (m_GraphIds.ContainsKey(owner))
                    return m_DeclarationIds[Key(owner, declaration)];
                return Local("decl", owner + ":" + declaration + ":" + RootSkill(fallback));
            }

            string RootEntryGraph(string graph)
            {
                string skill = RootSkill(graph);
                return m_Definition.SkillDefinitions.First(value => value != null && value.SkillId == skill).EntryGraphAuthoringId;
            }
            JObject ContextReference(string identity, string graph)
            {
                ActionContextSlot context = ResolveContext(identity, graph);
                return context ? new JObject { ["id"] = context.name, ["asset"] = JToken.FromObject(ObjectReference(context)) } : new JObject();
            }
            ActionContextSlot ResolveContext(string identity, string graph)
            {
                if (!string.IsNullOrEmpty(identity) && m_Snapshot.actionContextAssets.FirstOrDefault(value => value != null && value.id == identity) is AgentSnapshotAsset asset)
                    return AssetDatabase.LoadAssetAtPath<ActionContextSlot>(asset.assetPath);
                string skill = m_Definition.SkillDefinitions.First(value => value != null && value.SkillId == RootSkill(graph)).SkillId;
                return m_Definition.SkillDefinitions.First(value => value.SkillId == skill).ActionContext;
            }
            JObject ProfileReference(string id)
            {
                ActionProfile profile = m_Definition.ActionProfiles.FirstOrDefault(value => value && value.ActionId == id);
                return new JObject { ["id"] = id ?? string.Empty, ["asset"] = profile ? JToken.FromObject(ObjectReference(profile)) : null };
            }
            string RootSkill(string graph)
            {
                string current = graph;
                var visited = new HashSet<string>(StringComparer.Ordinal);
                while (visited.Add(current))
                {
                    CharacterSkillAuthoringDefinition skill = m_Definition.SkillDefinitions.FirstOrDefault(value => value != null && value.EntryGraphAuthoringId == current);
                    if (skill != null) return skill.SkillId;
                    current = FindContainingGraph(m_Legacy[current].owner?.entityId);
                    if (string.IsNullOrEmpty(current)) break;
                }
                return m_Definition.SkillDefinitions.First(value => value != null).SkillId;
            }

            AgentPackageObjectReference SharedTimelineReference(string id)
            {
                TimelineAsset asset = AssetDatabase.LoadAssetAtPath<TimelineAsset>(BtsmtlSkillLegacyMigrationWorkflow.SharedAttack1TimelinePath);
                if (!asset || asset.Data == null || asset.Data.AuthoringId != id)
                    throw new InvalidOperationException($"共享Timeline identity不匹配：{id}");
                return ObjectReference(asset);
            }
            bool IsSharedTimeline(string id) => id == "10f4cb90-8b9a-4944-b77c-14efc9a3124d";
            AgentPackageObjectReference ObjectReference(UnityEngine.Object asset)
            {
                if (!asset) return null;
                string path = AssetDatabase.GetAssetPath(asset);
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId))
                    throw new InvalidOperationException($"资产没有持久化identity：{asset.name}");
                return new AgentPackageObjectReference { assetPath = path, assetGuid = guid, localFileId = localId };
            }
            string TrackKind(string type) => type?.EndsWith("AnimationTrack", StringComparison.Ordinal) == true ? TimelineContractKinds.AnimationTrack : type?.EndsWith("MotionCurveTrack", StringComparison.Ordinal) == true ? TimelineContractKinds.MotionCurveTrack : type?.EndsWith("MotionWarpTrack", StringComparison.Ordinal) == true ? TimelineContractKinds.MotionWarpTrack : type?.EndsWith("TreeTrack", StringComparison.Ordinal) == true ? TimelineContractKinds.TreeTrack : type?.EndsWith("ActionCueTrack", StringComparison.Ordinal) == true ? TimelineContractKinds.ActionCueTrack : throw new InvalidOperationException($"未登记Timeline Track：{type}");
            string ClipKind(string type) => type?.EndsWith("AnimationClip", StringComparison.Ordinal) == true ? TimelineContractKinds.AnimationClip : type?.EndsWith("MotionCurveClip", StringComparison.Ordinal) == true ? TimelineContractKinds.MotionCurveClip : type?.EndsWith("MotionWarpClip", StringComparison.Ordinal) == true ? TimelineContractKinds.MotionWarpClip : type?.EndsWith("TreeClip", StringComparison.Ordinal) == true ? TimelineContractKinds.TreeClip : type?.EndsWith("ActionCueClip", StringComparison.Ordinal) == true ? TimelineContractKinds.ActionCueClip : throw new InvalidOperationException($"未登记Timeline Clip：{type}");
            string TimelineOwnerKey(string timeline, string track, string clip) => timeline + "\0" + track + "\0" + clip;
            string Key(string left, string right) => left + "\0" + right;
            string Local(string prefix, string seed)
            {
                using SHA256 sha = SHA256.Create();
                string hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(seed))).Replace("-", string.Empty).ToLowerInvariant();
                return "local:" + prefix + "-" + hash.Substring(0, 24);
            }

            sealed class RootAsset
            {
                public RootAsset(BtsmtlSkillFlowGraph graph, string path) { Graph = graph; Path = path; }
                public BtsmtlSkillFlowGraph Graph { get; }
                public string Path { get; }
                public AgentPackageObjectReference Reference
                {
                    get
                    {
                        string path = AssetDatabase.GetAssetPath(Graph);
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(Graph, out string guid, out long localId);
                        return new AgentPackageObjectReference { assetPath = path, assetGuid = guid, localFileId = localId };
                    }
                }
            }

            [Serializable]
            sealed class LegacyGraphFile
            {
                public string id;
                public string kind;
                public LegacyOwner owner;
                public List<LegacyNode> nodes = new();
                public List<LegacyFlowEdge> flowEdges = new();
                public List<LegacyPropertyEdge> propertyEdges = new();
            }

            [Serializable]
            sealed class LegacyOwner { public string entityId; public string slot; }

            [Serializable]
            sealed class LegacyNode
            {
                public string id;
                public string kind;
                public string name;
                public JObject properties;
            }

            [Serializable]
            sealed class LegacyFlowEdge
            {
                public string id;
                public LegacyEndpoint from;
                public LegacyEndpoint to;
                public string conditionGraph;
                public int flowOrder;
                public string abortPolicy;
            }

            [Serializable]
            sealed class LegacyPropertyEdge
            {
                public string id;
                public LegacyEndpoint from;
                public LegacyEndpoint to;
            }

            [Serializable]
            sealed class LegacyEndpoint { public string node; public string port; }
        }
    }
}
