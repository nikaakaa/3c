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

using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill
{
    internal sealed class AgentSkillFlowDocumentExporter
    {
        readonly AgentCompileReport m_Report;
        readonly AgentPackageSkillFlowDocument m_Document = new AgentPackageSkillFlowDocument();
        readonly Dictionary<string, AgentPackageSkillFlowGraphFile> m_Graphs = new Dictionary<string, AgentPackageSkillFlowGraphFile>(StringComparer.Ordinal);
        readonly Dictionary<string, AgentPackageSkillFlowGraphLayoutFile> m_Layouts = new Dictionary<string, AgentPackageSkillFlowGraphLayoutFile>(StringComparer.Ordinal);
        readonly Dictionary<string, AgentPackageSkillMacroFile> m_Macros = new Dictionary<string, AgentPackageSkillMacroFile>(StringComparer.Ordinal);
        readonly Dictionary<string, AgentPackageSkillTimelineFile> m_Timelines = new Dictionary<string, AgentPackageSkillTimelineFile>(StringComparer.Ordinal);
        readonly HashSet<FlowGraph> m_Active = new HashSet<FlowGraph>();

        AgentSkillFlowDocumentExporter(AgentCompileReport report)
        {
            m_Report = report;
        }

        public static AgentPackageSkillFlowDocument Export(CharacterPipelineDefinition definition, AgentCompileReport report)
        {
            var exporter = new AgentSkillFlowDocumentExporter(report);
            exporter.ExportDefinition(definition);
            return exporter.m_Document;
        }

        void ExportDefinition(CharacterPipelineDefinition definition)
        {
            var skillDefinitions = new List<AgentPackageSkillDefinitionFile>();
            foreach (CharacterSkillAuthoringDefinition definitionEntry in definition.SkillDefinitions)
            {
                if (definitionEntry == null)
                    continue;
                skillDefinitions.Add(new AgentPackageSkillDefinitionFile
                {
                    skillId = definitionEntry.SkillId,
                    entryGraphAuthoringId = definitionEntry.EntryGraphAuthoringId,
                    actionProfileId = definitionEntry.ActionProfile ? definitionEntry.ActionProfile.ActionId : string.Empty,
                    actionProfileAssetPath = AssetPath(definitionEntry.ActionProfile),
                    actionProfileAssetGuid = AssetGuid(definitionEntry.ActionProfile),
                    actionContext = definitionEntry.ActionContext ? definitionEntry.ActionContext.name : string.Empty,
                    actionContextAssetPath = AssetPath(definitionEntry.ActionContext),
                    actionContextAssetGuid = AssetGuid(definitionEntry.ActionContext),
                    sourceInputRequestId = definitionEntry.SourceInputRequestId,
                    consumeSourceInputRequest = definitionEntry.ConsumeSourceInputRequest,
                    targetInputValueId = definitionEntry.TargetInputValueId,
                    targetKey = definitionEntry.TargetKey,
                    subgraphDependencies = definitionEntry.SubgraphDependencies.Where(value => value != null).Select(value => new AgentPackageSkillSubgraphDependency
                    {
                        subgraphIdentity = value.SubgraphIdentity,
                        callSiteIdentity = value.CallSiteIdentity
                    }).ToList(),
                    allowedFollowUpSkillIds = definitionEntry.AllowedFollowUpSkillIds.ToList()
                });
            }
            m_Document.skills = skillDefinitions;
            var roots = definition.SkillGraphs ?? Array.Empty<BtsmtlSkillFlowGraph>();
            foreach (BtsmtlSkillFlowGraph root in roots)
            {
                string skillId = skillDefinitions.FirstOrDefault(value =>
                    string.Equals(value.entryGraphAuthoringId, root?.AuthoringId, StringComparison.Ordinal))?.skillId;
                if (root == null || root.Role != BtsmtlSkillFlowGraphRole.Skill || string.IsNullOrEmpty(skillId))
                {
                    m_Report.Error("definition.SkillGraphs", "skill_root_graph_invalid", "Definition.SkillGraphs只能包含被SkillDefinition引用的Skill根图。");
                    continue;
                }
                try
                {
                    BtsmtlSkillGraphClosure.Validate(root, true);
                }
                catch (Exception exception)
                {
                    m_Report.Error("definition.SkillGraphs[" + root.AuthoringId + "]", "skill_graph_closure_invalid", exception.Message);
                    continue;
                }
                VisitGraph(root, Owner("skill-root", skillId: skillId), skillId);
            }
            foreach (AgentPackageSkillDefinitionFile skill in skillDefinitions)
                if (!m_Graphs.ContainsKey(skill.entryGraphAuthoringId))
                    m_Report.Error(
                        "definition.SkillDefinitions[" + skill.skillId + "]",
                        "skill_root_graph_missing",
                        "SkillDefinition入口没有对应的正式Skill Graph。");
            m_Document.graphs = m_Graphs.Values.OrderBy(value => value.id, StringComparer.Ordinal).ToList();
            m_Document.layouts = m_Layouts.Values.OrderBy(value => value.graphId, StringComparer.Ordinal).ToList();
            m_Document.macros = m_Macros.Values.OrderBy(value => value.id, StringComparer.Ordinal).ToList();
            m_Document.timelines = m_Timelines.Values.OrderBy(value => value.id, StringComparer.Ordinal).ToList();
        }

        void VisitGraph(FlowGraph graph, AgentPackageSkillGraphOwner owner, string skillId)
        {
            if (graph is not IBtsmtlSkillFlowGraph authoring)
            {
                m_Report.Error("editable/skills", "skill_graph_type_invalid", "Skill闭包包含非正式技能图。");
                return;
            }
            if (!m_Active.Add(graph))
            {
                m_Report.Error("skill:" + skillId, "skill_graph_recursive", "Skill Graph闭包包含递归引用。");
                return;
            }
            try
            {
                if (m_Graphs.ContainsKey(authoring.AuthoringId))
                    return;
                string graphPath = "skill:" + skillId + "/graph:" + authoring.AuthoringId;
                var file = new AgentPackageSkillFlowGraphFile
                {
                    id = authoring.AuthoringId,
                    role = authoring.Role.ToString(),
                    name = graph.name,
                    contentRevision = new BtsmtlSkillGraphFingerprint().Compute(graph),
                    ownership = owner.kind == "skill-root"
                        ? AgentGraphOwnership.RootAsset.ToString()
                        : owner.kind == "shared" || AssetDatabase.IsMainAsset(graph)
                            ? AgentGraphOwnership.SharedAsset.ToString()
                            : AgentGraphOwnership.Inline.ToString(),
                    owner = owner,
                    asset = ObjectReference(graph)
                };
                m_Graphs.Add(authoring.AuthoringId, file);
                ExportBlackboard(graph, file, graphPath);
                foreach (FlowNode node in graph.allNodes.Cast<FlowNode>().OrderBy(value => value.UID, StringComparer.Ordinal))
                {
                    if (AgentSkillPackageProjection.TryGetKind(node, out string kind) &&
                        AgentSkillPackageProjection.IsAnchor(kind))
                    {
                        file.anchors.Add(new AgentPackageSkillGraphAnchor { kind = kind, nodeId = node.UID });
                        ExportReferences(node, file, skillId);
                        continue;
                    }
                    if (!AgentSkillPackageProjection.TryGetKind(node, out kind))
                    {
                        m_Report.Error(graphPath + ".nodes", "skill_node_capability_missing", $"Node '{node.GetType().Name}'没有正式Skill Capability。");
                        continue;
                    }
                    file.nodes.Add(new AgentPackageSkillFlowNode
                    {
                        id = node.UID,
                        capability = kind,
                        name = node.name,
                        properties = ExportProperties(node),
                        values = ExportValues(node, graphPath)
                    });
                    ExportReferences(node, file, skillId);
                }
                foreach (FlowNode node in graph.allNodes.Cast<FlowNode>())
                    foreach (BinderConnection connection in node.outConnections.OfType<BinderConnection>().OrderBy(value => value.UID, StringComparer.Ordinal))
                    {
                        if (connection.sourcePort == null || connection.targetPort == null ||
                            connection.sourcePort.type != connection.targetPort.type ||
                            connection.sourcePort.IsFlowPort() != connection.targetPort.IsFlowPort())
                        {
                            m_Report.Error(graphPath + ".edges[" + connection.UID + "]", "skill_edge_port_invalid", "Skill Edge端点缺失或类型不一致。");
                            continue;
                        }
                        var transfer = connection as BtsmtlSkillFlowConnection;
                        string conditionGraphId = null;
                        int priority = 0;
                        string abortPolicy = null;
                        int order = 0;
                        if (authoring.Role == BtsmtlSkillFlowGraphRole.StateMachine && transfer == null)
                        {
                            m_Report.Error(graphPath + ".edges[" + connection.UID + "]",
                                "skill_state_transfer_type_invalid",
                                "状态机转移边必须携带Transfer数据。");
                            continue;
                        }
                        if (transfer != null)
                        {
                            conditionGraphId = transfer.Condition != null ? transfer.Condition.AuthoringId : null;
                            priority = transfer.Priority;
                            abortPolicy = transfer.AbortPolicy.ToString();
                            order = transfer.Order;
                        }
                        file.edges.Add(new AgentPackageSkillFlowEdge
                        {
                            id = connection.UID,
                            kind = connection.sourcePort.IsFlowPort() ? "flow" : "value",
                            from = Endpoint(connection.sourceNode as FlowNode, connection.sourcePortID),
                            to = Endpoint(connection.targetNode as FlowNode, connection.targetPortID),
                            conditionGraphId = conditionGraphId,
                            priority = priority,
                            abortPolicy = abortPolicy,
                            order = order
                        });
                    }
                m_Layouts[authoring.AuthoringId] = new AgentPackageSkillFlowGraphLayoutFile
                {
                    graphId = authoring.AuthoringId,
                    nodes = file.nodes.Select(node =>
                    {
                        FlowNode source = graph.allNodes.OfType<FlowNode>().Single(value => value.UID == node.id);
                        return new AgentPackageSkillFlowNodeLayout
                        {
                            id = node.id,
                            x = source.position.x,
                            y = source.position.y
                        };
                    }).ToList()
                };
                if (graph is BtsmtlSkillMacroGraph macro)
                    ExportMacro(macro, owner, graphPath);
            }
            finally
            {
                m_Active.Remove(graph);
            }
        }

        void ExportReferences(FlowNode node, AgentPackageSkillFlowGraphFile ownerGraph, string skillId)
        {
            string ownerId = ownerGraph.id;
            if (node is MacroNodeWrapper macroNode && macroNode.macro is BtsmtlSkillMacroGraph macro)
            {
                string kind = AssetDatabase.IsMainAsset(macro) ? "shared" : "node";
                VisitGraph(macro, Owner(kind, graphId: ownerId, nodeId: node.UID, referenceKey: "macro", asset: ObjectReference(macro)), skillId);
            }
            if (node is BtsmtlSkillStateMachineFlowNode stateMachine && stateMachine.StateMachine)
                VisitGraph(stateMachine.StateMachine, Owner("node", graphId: ownerId, nodeId: node.UID, referenceKey: "stateMachine"), skillId);
            if (node is BtsmtlSkillStateFlowNode state && state.Body)
                VisitGraph(state.Body, Owner("node", graphId: ownerId, nodeId: node.UID, referenceKey: "body"), skillId);
            if (node is BtsmtlSkillCompositeFlowNode composite)
                foreach (BtsmtlSkillStepPort step in composite.Steps)
                    if (step?.Condition)
                        VisitGraph(step.Condition, Owner("step", graphId: ownerId, nodeId: node.UID, referenceKey: step.Id), skillId);
            foreach (BinderConnection connection in node.outConnections.OfType<BinderConnection>())
                if (connection is BtsmtlSkillFlowConnection transfer && transfer.Condition)
                    VisitGraph(transfer.Condition, Owner("edge", graphId: ownerId, nodeId: node.UID,
                        referenceKey: "condition", edgeId: transfer.UID), skillId);
            if (node is BtsmtlSkillTimelineFlowNode timeline && timeline.TimelineAsset)
            {
                ExportTimeline(timeline, ownerId, node.UID, skillId);
                foreach (Track track in timeline.Timeline?.Tracks ?? new List<Track>())
                    foreach (Clip clip in track.Clips)
                        if (clip is TreeClip tree && tree.AssetTree is BtsmtlSkillFlowGraph child)
                            VisitGraph(child, Owner("timeline-clip", graphId: ownerId, nodeId: node.UID,
                                referenceKey: "timeline", timelineId: timeline.Timeline.AuthoringId,
                                trackId: track.AuthoringId, clipId: clip.AuthoringId), skillId);
            }
        }

        JObject ExportProperties(FlowNode node)
        {
            return BtsmtlSkillNodeAuthoringBinding.Export(
                node,
                LogicalReference,
                value => AgentAuthoringDocumentCodec.ToToken(ObjectReference(value)),
                graph => graph is IBtsmtlSkillFlowGraph skill ? skill.AuthoringId : string.Empty,
                timeline => timeline?.Data?.AuthoringId ?? string.Empty);
        }

        static AgentPackageSkillFlowStep ExportStep(BtsmtlSkillStepPort step)
        {
            return new AgentPackageSkillFlowStep
            {
                id = step.Id,
                name = step.Name,
                conditionGraphId = step.Condition ? ((IBtsmtlSkillFlowGraph)step.Condition).AuthoringId : string.Empty,
                priority = step.Priority,
                abortPolicy = step.AbortPolicy.ToString()
            };
        }

        JObject ExportValues(FlowNode node, string path)
        {
            var result = new JObject();
            foreach (ValueInput input in node.GetInputValuePorts())
            {
                if (input.isConnected || input.isDefaultValue)
                    continue;
                JToken value = DocumentValue(input.serializedValue, input.type, path + ".values." + input.ID);
                if (value != null)
                    result[input.ID] = value;
            }
            return result;
        }

        void ExportBlackboard(FlowGraph graph, AgentPackageSkillFlowGraphFile file, string path)
        {
            IBtsmtlSkillFlowGraph authoring = (IBtsmtlSkillFlowGraph)graph;
            foreach (BtsmtlSkillBlackboardDeclaration declaration in authoring.BlackboardDeclarations)
            {
                Variable variable = graph.GetGraphSource().localBlackboard.variables.Values.SingleOrDefault(value =>
                    value != null && string.Equals(value.ID, declaration.VariableId, StringComparison.Ordinal));
                if (variable is not ISerializedVariableValue serialized)
                {
                    m_Report.Error(path + ".blackboardDeclarations", "skill_blackboard_variable_missing", $"Variable '{declaration.VariableId}'缺少正式序列化值。");
                    continue;
                }
                file.blackboardDeclarations.Add(new AgentPackageSkillBlackboardDeclaration
                {
                    id = declaration.VariableId,
                    key = variable.name,
                    valueType = AgentSkillPackageProjection.ValueType(variable.varType),
                    scope = declaration.Scope.ToString(),
                    lifetime = declaration.Lifetime.ToString(),
                    category = declaration.Category,
                    defaultValue = DocumentValue(serialized.serializedValue, variable.varType, path + ".blackboardDeclarations[" + declaration.VariableId + "]"),
                    inputBinding = declaration.InputBinding == null ? null : new AgentPackageSkillBlackboardInputBinding { inputValueId = declaration.InputBinding.InputValueId },
                    factProjection = declaration.FactProjection == null ? null : new AgentPackageSkillBlackboardFactProjection
                    {
                        kind = declaration.FactProjection.Kind.ToString(),
                        windowType = declaration.FactProjection.ActionWindowType,
                        windowId = declaration.FactProjection.ActionWindowId,
                        digest = declaration.FactProjection.ActionWindowDigest
                    }
                });
            }
        }

        void ExportMacro(BtsmtlSkillMacroGraph graph, AgentPackageSkillGraphOwner owner, string path)
        {
            var file = new AgentPackageSkillMacroFile
            {
                id = graph.AuthoringId,
                graphId = graph.AuthoringId,
                ownership = owner.kind == "shared" ? AgentGraphOwnership.SharedAsset.ToString() : AgentGraphOwnership.Inline.ToString(),
                owner = owner,
                asset = ObjectReference(graph)
            };
            foreach (DynamicParameterDefinition parameter in graph.inputDefinitions)
                file.inputs.Add(new AgentPackageSkillMacroParameter
                {
                    id = parameter.ID,
                    name = parameter.name,
                    valueType = AgentSkillPackageProjection.ValueType(parameter.type)
                });
            foreach (DynamicParameterDefinition parameter in graph.outputDefinitions)
                file.outputs.Add(new AgentPackageSkillMacroParameter
                {
                    id = parameter.ID,
                    name = parameter.name,
                    valueType = AgentSkillPackageProjection.ValueType(parameter.type)
                });
            if (!m_Macros.TryAdd(file.id, file))
                m_Report.Error(path, "skill_macro_duplicate", $"Skill Macro identity重复：{file.id}");
        }

        void ExportTimeline(BtsmtlSkillTimelineFlowNode node, string graphId, string nodeId, string skillId)
        {
            TimelineData data = node.Timeline;
            if (data == null)
            {
                m_Report.Error("skill:" + skillId + "/node:" + nodeId, "skill_timeline_missing", "Skill Timeline缺少TimelineData。");
                return;
            }
            if (!m_Timelines.TryGetValue(data.AuthoringId, out AgentPackageSkillTimelineFile file))
            {
                file = new AgentPackageSkillTimelineFile
                {
                    id = data.AuthoringId,
                    name = data.Name,
                    ownership = node.Ownership.ToString(),
                    asset = ObjectReference(node.TimelineAsset),
                    ownerGraphId = graphId,
                    ownerNodeId = nodeId
                };
                m_Timelines.Add(file.id, file);
                foreach (TimelineExternalBindingDeclaration binding in data.ExternalBindings ?? new List<TimelineExternalBindingDeclaration>())
                    file.externalBindings.Add(new AgentPackageSkillTimelineExternalBinding
                    {
                        id = binding.AuthoringId,
                        bindingId = binding.BindingId,
                        displayName = binding.DisplayName,
                        domain = binding.Domain,
                        parameterId = binding.ParameterId,
                        valueKind = binding.ValueKind.ToString(),
                        access = binding.Access.ToString(),
                        lifetime = binding.Lifetime.ToString()
                    });
                foreach (TimelineSection section in data.Sections)
                    file.sections.Add(new AgentPackageSkillTimelineSection
                    {
                        id = section.AuthoringId,
                        name = section.Name,
                        frame = section.Frame,
                        nextSectionId = section.NextSectionId
                    });
                foreach (Track track in data.Tracks)
                {
                    TimelineAuthoringTrackExport authoredTrack =
                        TimelineAuthoringTrackBinding.Export(track);
                    var trackFile = new AgentPackageSkillTimelineTrack
                    {
                        id = track.AuthoringId,
                        kind = track.ContractKind,
                        name = track.Name,
                        animationChannelId = authoredTrack.AnimationChannelId,
                        animationSlotId = authoredTrack.AnimationSlotId
                    };
                    foreach (Clip clip in track.Clips)
                        trackFile.clips.Add(ExportClip(clip, skillId, file.id, trackFile.id));
                    file.tracks.Add(trackFile);
                }
            }
            file.callSites.Add(new AgentPackageSkillTimelineCallSite
            {
                graphId = graphId,
                nodeId = nodeId,
                playbackMode = node.PlaybackMode.ToString()
            });
        }

        AgentPackageSkillTimelineClip ExportClip(Clip clip, string skillId, string timelineId, string trackId)
        {
            var result = new AgentPackageSkillTimelineClip
            {
                id = clip.AuthoringId,
                kind = clip.ContractKind,
                startFrame = clip.StartFrame,
                endFrame = clip.EndFrame,
                otherEaseInFrame = clip.OtherEaseInFrame,
                otherEaseOutFrame = clip.OtherEaseOutFrame,
                selfEaseInFrame = clip.SelfEaseInFrame,
                selfEaseOutFrame = clip.SelfEaseOutFrame,
                clipInFrame = clip.ClipInFrame
            };
            TimelineAuthoringClipExport authored = TimelineAuthoringClipBinding.Export(clip);
            result.properties = authored.Properties;
            if (authored.AnimationAsset)
                result.animationClip = ObjectReference(authored.AnimationAsset);
            if (clip is TreeClip)
            {
                TimelineTreeAuthoringClipExport authoredTree =
                    TimelineTreeAuthoringClipBinding.Export(clip);
                if (authoredTree.AssetTree is BtsmtlSkillFlowGraph graph)
                {
                    result.treeGraphId = graph.AuthoringId;
                    result.treeOwnership = TimelineTreeOwnership.AssetGraph.ToString();
                }
                else
                {
                    m_Report.Error("skill:" + skillId + "/timeline:" + timelineId + "/track:" + trackId + "/clip:" + clip.AuthoringId,
                        "skill_tree_clip_source_invalid", "Skill TreeClip必须引用BtsmtlSkillFlowGraph AssetTree。");
                }
                result.treePhase = authoredTree.Phase;
            }
            foreach (TimelineCurveChannelDescriptor descriptor in TimelineCurveChannelCatalog.All)
            {
                if (!descriptor.Supports(clip))
                    continue;
                AnimationCurve curve = descriptor.Read(clip);
                result.curves.Add(new AgentPackageCurve
                {
                    clipId = clip.AuthoringId,
                    channelId = descriptor.ChannelId.Value,
                    timeDomain = descriptor.TimeDomain.ToString(),
                    bounded = descriptor.ValueDomain.IsBounded,
                    minimum = descriptor.ValueDomain.Minimum,
                    maximum = descriptor.ValueDomain.Maximum,
                    zero = descriptor.ValueDomain.Zero,
                    unit = descriptor.ValueDomain.Unit,
                    preWrapMode = curve.preWrapMode.ToString(),
                    postWrapMode = curve.postWrapMode.ToString(),
                    keys = curve.keys.Select(value => new AgentAnimationCurveKey
                    {
                        time = value.time,
                        value = value.value,
                        inTangent = value.inTangent,
                        outTangent = value.outTangent,
                        inWeight = value.inWeight,
                        outWeight = value.outWeight,
                        weightedMode = value.weightedMode.ToString()
                    }).ToList()
                });
            }
            return result;
        }

        AgentPackageSkillGraphOwner Owner(
            string kind,
            string skillId = null,
            string graphId = null,
            string nodeId = null,
            string edgeId = null,
            string referenceKey = null,
            string timelineId = null,
            string trackId = null,
            string clipId = null,
            AgentPackageObjectReference asset = null)
        {
            return new AgentPackageSkillGraphOwner
            {
                kind = kind,
                skillId = skillId,
                graphId = graphId,
                nodeId = nodeId,
                edgeId = edgeId,
                referenceKey = referenceKey,
                timelineId = timelineId,
                trackId = trackId,
                clipId = clipId,
                asset = asset
            };
        }

        AgentPackageSkillFlowEdgeEndpoint Endpoint(FlowNode node, string port) =>
            new AgentPackageSkillFlowEdgeEndpoint
            {
                node = AgentSkillPackageProjection.TryGetKind(node, out string kind) &&
                       AgentSkillPackageProjection.IsAnchor(kind)
                    ? kind
                    : node?.UID ?? string.Empty,
                port = port
            };

        JObject LogicalReference(UnityEngine.Object value)
        {
            if (!value)
                return new JObject();
            return new JObject
            {
                ["id"] = value switch
                {
                    ActionProfile profile => profile.ActionId,
                    GameplayEffectDefinition effect => effect.EffectId.Value,
                    _ => value.name
                },
                ["asset"] = AgentAuthoringDocumentCodec.ToToken(ObjectReference(value))
            };
        }

        AgentPackageObjectReference ObjectReference(UnityEngine.Object value)
        {
            if (!value)
                return null;
            string path = AssetDatabase.GetAssetPath(value);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value, out string guid, out long localFileId))
            {
                m_Report.Error("editable/skills", "skill_asset_reference_invalid", $"Skill引用'{value.name}'不是持久化资产。");
                return null;
            }
            return new AgentPackageObjectReference
            {
                assetPath = path,
                assetGuid = guid,
                localFileId = localFileId
            };
        }

        JToken DocumentValue(object value, Type type, string path)
        {
            if (value == null)
                return JValue.CreateNull();
            if (type == typeof(Vector2))
            {
                Vector2 vector = (Vector2)value;
                return new JObject { ["x"] = vector.x, ["y"] = vector.y };
            }
            if (type == typeof(Vector3))
            {
                Vector3 vector = (Vector3)value;
                return new JObject { ["x"] = vector.x, ["y"] = vector.y, ["z"] = vector.z };
            }
            if (type == typeof(ActionTargetSnapshot))
            {
                ActionTargetSnapshot target = (ActionTargetSnapshot)value;
                return new JObject
                {
                    ["targetId"] = target.TargetId,
                    ["x"] = target.Position.x,
                    ["y"] = target.Position.y,
                    ["z"] = target.Position.z,
                    ["rx"] = target.Rotation.x,
                    ["ry"] = target.Rotation.y,
                    ["rz"] = target.Rotation.z,
                    ["rw"] = target.Rotation.w
                };
            }
            if (type == typeof(bool) || type == typeof(int) || type == typeof(float) ||
                type == typeof(string) || type == typeof(uint) || type == typeof(ulong))
                return JToken.FromObject(value);
            if (value is UnityEngine.Object asset)
                return AgentAuthoringDocumentCodec.ToToken(ObjectReference(asset));
            m_Report.Error(path, "skill_value_type_unsupported", $"Skill值类型未登记：{type?.Name}");
            return null;
        }

        static string AssetPath(UnityEngine.Object value) => value ? AssetDatabase.GetAssetPath(value) : string.Empty;
        static string AssetGuid(UnityEngine.Object value)
        {
            string path = AssetPath(value);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }
    }
}
