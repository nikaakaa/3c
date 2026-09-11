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
    internal static class AgentSkillFlowDocumentValidator
    {
        public static bool Validate(
            AgentPackageSkillFlowDocument document,
            AgentCompileReport report,
            string controlModuleId,
            string inputProviderOwnerId = null,
            string gameplayProviderOwnerId = null)
        {
            if (document == null)
            {
                report.Error("editable/skills", "skill_document_missing", "Skill Flow Document正文缺失。");
                return false;
            }
            var graphById = new Dictionary<string, AgentPackageSkillFlowGraphFile>(StringComparer.Ordinal);
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            var edgeIds = new HashSet<string>(StringComparer.Ordinal);
            var layoutById = (document.layouts ?? new List<AgentPackageSkillFlowGraphLayoutFile>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.graphId))
                .GroupBy(value => value.graphId, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            bool valid = true;
            valid &= AgentSkillPackageProjection.ValidateCatalog(report);
            if (document.skills == null || document.graphs == null || document.layouts == null ||
                document.macros == null || document.timelines == null)
            {
                report.Error("editable/skills", "skill_document_collection_missing", "Skill Flow Document的顶层集合不能为null。");
                valid = false;
            }
            if ((document.layouts ?? new List<AgentPackageSkillFlowGraphLayoutFile>())
                .Where(value => value != null)
                .GroupBy(value => value.graphId ?? string.Empty, StringComparer.Ordinal)
                .Any(value => value.Count() != 1))
            {
                report.Error("editable/skills/graphs", "skill_graph_layout_duplicate", "Skill Graph layout identity缺失或重复。");
                valid = false;
            }
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ??
                         new List<AgentPackageSkillFlowGraphFile>())
            {
                string path = AgentSkillFlowDocumentMapper.GraphPath(graph?.id);
                if (graph == null || !IsIdentity(graph.id) || !graphById.TryAdd(graph.id, graph) ||
                    !Enum.TryParse(graph.role, false, out BtsmtlSkillFlowGraphRole role) ||
                    !Enum.IsDefined(typeof(BtsmtlSkillFlowGraphRole), role) ||
                    !Enum.TryParse(graph.ownership, false, out AgentGraphOwnership ownership) ||
                    ownership == AgentGraphOwnership.Unknown ||
                    !layoutById.TryGetValue(graph.id ?? string.Empty, out AgentPackageSkillFlowGraphLayoutFile layout) ||
                    !string.Equals(layout.graphId, graph.id, StringComparison.Ordinal))
                {
                    report.Error(path, "skill_graph_invalid", "Skill Graph identity、role、ownership或layout无效。");
                    valid = false;
                    continue;
                }
                if (graph.anchors == null || graph.nodes == null || graph.edges == null || graph.blackboardDeclarations == null)
                {
                    report.Error(path, "skill_graph_collection_missing", "Skill Graph的anchors、nodes、edges和blackboardDeclarations集合不能为null。");
                    valid = false;
                }
                if (!ValidateOwner(graph, role, ownership, path, document.skills, report))
                    valid = false;
                if (!LocalIdentity(graph.id) && !Asset(graph.asset))
                {
                    report.Error(path + ".asset", "skill_graph_asset_invalid", "已有Skill Graph必须带正式资产引用。");
                    valid = false;
                }
                if (LocalIdentity(graph.id) && graph.asset != null)
                {
                    report.Error(path + ".asset", "skill_graph_local_asset_invalid", "新增Skill Graph不能同时声明资产引用。");
                    valid = false;
                }
                if (LocalIdentity(graph.id) &&
                    (role == BtsmtlSkillFlowGraphRole.Skill && ownership != AgentGraphOwnership.RootAsset ||
                     role != BtsmtlSkillFlowGraphRole.Skill &&
                     ownership != AgentGraphOwnership.Inline &&
                     !(role == BtsmtlSkillFlowGraphRole.TimelineBody && ownership == AgentGraphOwnership.SharedAsset)))
                {
                    report.Error(path + ".ownership", "skill_graph_local_ownership_invalid", "新增Skill Graph必须由RootAsset或Inline owner创建。");
                    valid = false;
                }
                var graphNodes = new HashSet<string>(StringComparer.Ordinal);
                var graphAnchors = new HashSet<string>(StringComparer.Ordinal);
                var anchorNodeIds = new HashSet<string>(StringComparer.Ordinal);
                foreach (AgentPackageSkillGraphAnchor anchor in graph.anchors ??
                             new List<AgentPackageSkillGraphAnchor>())
                {
                    if (anchor == null || !IsAnchor(anchor.kind) || !IsIdentity(anchor.nodeId) ||
                        !graphAnchors.Add(anchor.kind) || !anchorNodeIds.Add(anchor.nodeId) ||
                        !nodeIds.Add(anchor.nodeId))
                    {
                        report.Error(path + ".anchors", "skill_graph_anchor_invalid", "Skill Graph anchor缺失、重复或非法。");
                        valid = false;
                    }
                    if (anchor != null &&
                        BtsmtlSkillGraphAuthoringMetadata.HasOrderedStepPorts(anchor.kind) &&
                        !ValidateSteps(anchor.steps, path + ".anchors[" + anchor.kind + "].steps", report))
                        valid = false;
                    if (anchor != null &&
                        !BtsmtlSkillGraphAuthoringMetadata.IsAnchorAllowed(
                            anchor.kind,
                            role))
                    {
                        report.Error(path + ".anchors[" + anchor.kind + "]", "skill_graph_anchor_role_invalid", "Skill Graph anchor不属于当前role。");
                        valid = false;
                    }
                }
                foreach (string requiredAnchor in
                         BtsmtlSkillGraphAuthoringMetadata.RequiredAnchors(role))
                {
                    if (!graphAnchors.Contains(requiredAnchor))
                    {
                        report.Error(path + ".anchors", "skill_graph_anchor_missing", $"Skill Graph缺少必需anchor：{requiredAnchor}");
                        valid = false;
                    }
                }
                foreach (AgentPackageSkillFlowNode node in graph.nodes ??
                             new List<AgentPackageSkillFlowNode>())
                {
                    string nodePath = path + ".nodes[" + (node?.id ?? string.Empty) + "]";
                    if (node == null || !IsIdentity(node.id) || !graphNodes.Add(node.id) ||
                        !nodeIds.Add(node.id) || !AgentSkillPackageProjection.TryResolveType(node.capability, out _) ||
                        AgentSkillPackageProjection.IsAnchor(node.capability) ||
                        !AgentSkillPackageProjection.IsAllowed(node, role))
                    {
                        report.Error(nodePath, "skill_node_invalid", "Skill Node identity、kind或所属页面无效。");
                        valid = false;
                        continue;
                    }
                    valid &= ValidateNodeProperties(
                        node,
                        graph,
                        document.macros,
                        role,
                        nodePath,
                        report,
                        controlModuleId,
                        inputProviderOwnerId,
                        gameplayProviderOwnerId);
                    valid &= RejectInternalFields(node.properties, nodePath + ".properties", report);
                }
                var layoutNodes = new HashSet<string>(StringComparer.Ordinal);
                if (layout.nodes == null)
                {
                    report.Error(AgentSkillFlowDocumentMapper.GraphLayoutPath(graph.id) + ".nodes", "skill_graph_layout_collection_missing", "Skill Graph layout的nodes集合不能为null。");
                    valid = false;
                }
                foreach (AgentPackageSkillFlowNodeLayout layoutNode in layout.nodes ??
                             new List<AgentPackageSkillFlowNodeLayout>())
                {
                    if (layoutNode == null || !IsIdentity(layoutNode.id) || !graphNodes.Contains(layoutNode.id) ||
                        !layoutNodes.Add(layoutNode.id) || !Finite(layoutNode.x) || !Finite(layoutNode.y))
                    {
                        report.Error(AgentSkillFlowDocumentMapper.GraphLayoutPath(graph.id) + ".nodes", "skill_graph_layout_invalid", "Skill Graph layout必须唯一引用当前Graph的普通节点并使用有限坐标。");
                        valid = false;
                    }
                }
                if (!layoutNodes.SetEquals(graphNodes))
                {
                    report.Error(AgentSkillFlowDocumentMapper.GraphLayoutPath(graph.id) + ".nodes", "skill_graph_layout_incomplete", "Skill Graph layout必须完整覆盖普通节点，不能包含anchor或漏掉节点。");
                    valid = false;
                }
                var connectedOutputs = new HashSet<(string Node, string Port)>();
                var connectedInputs = new HashSet<(string Node, string Port)>();
                foreach (AgentPackageSkillFlowEdge edge in graph.edges ??
                             new List<AgentPackageSkillFlowEdge>())
                {
                    string edgePath = path + ".edges[" + (edge?.id ?? string.Empty) + "]";
                    if (edge == null || !IsIdentity(edge.id) || !edgeIds.Add(edge.id) ||
                        !ValidateEndpoint(edge.from, graphNodes, graphAnchors, edgePath + ".from", report) ||
                        !ValidateEndpoint(edge.to, graphNodes, graphAnchors, edgePath + ".to", report) ||
                        (edge.kind != "flow" && edge.kind != "value"))
                    {
                        report.Error(edgePath, "skill_edge_invalid", "Skill Edge identity、类型或端点无效。");
                        valid = false;
                    }
                    else if (!ValidateEdgePortShapes(
                                 graph,
                                 edge,
                                 document.macros,
                                 connectedOutputs,
                                 connectedInputs,
                                 edgePath,
                                 report))
                        valid = false;
                }
                valid &= ValidateBlackboard(graph, path, report);
            }
            var macroIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillMacroFile macro in document.macros ??
                         new List<AgentPackageSkillMacroFile>())
            {
                if (macro == null || !macroIds.Add(macro.id))
                {
                    report.Error("editable/skills/macros", "skill_macro_identity_duplicate", "Skill Macro identity缺失或重复。");
                    valid = false;
                    continue;
                }
                if (macro.inputs == null || macro.outputs == null)
                {
                    report.Error(AgentSkillFlowDocumentMapper.MacroPath(macro.id), "skill_macro_collection_missing", "Skill Macro的inputs和outputs集合不能为null。");
                    valid = false;
                }
                valid &= ValidateMacro(macro, graphById, report);
            }
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ?? new List<AgentPackageSkillFlowGraphFile>())
            {
                bool hasMacro = macroIds.Contains(graph?.id ?? string.Empty);
                if (graph?.role == BtsmtlSkillFlowGraphRole.Subgraph.ToString() && !hasMacro ||
                    graph?.role != BtsmtlSkillFlowGraphRole.Subgraph.ToString() && hasMacro)
                {
                    report.Error(AgentSkillFlowDocumentMapper.GraphPath(graph?.id), "skill_macro_graph_pair_invalid", "Subgraph Graph与Macro接口分片必须一一对应。");
                    valid = false;
                }
            }
            var timelineIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineFile timeline in document.timelines ??
                         new List<AgentPackageSkillTimelineFile>())
            {
                if (timeline == null || !timelineIds.Add(timeline.id))
                {
                    report.Error("editable/skills/timelines", "skill_timeline_identity_duplicate", "Skill Timeline identity缺失或重复。");
                    valid = false;
                    continue;
                }
                valid &= ValidateTimeline(timeline, graphById, report);
            }
            valid &= ValidateSkillRoots(document.skills, graphById, report);
            valid &= ValidateReferences(document, graphById, report);
            return valid;
        }

        public static bool ValidateDefinition(
            CharacterPipelineDefinition definition,
            AgentCompileReport report)
        {
            if (!definition)
            {
                report.Error("definition", "missing_definition", "CharacterPipelineDefinition缺失。");
                return false;
            }
            try
            {
                AgentPackageSkillFlowDocument document =
                    AgentSkillFlowDocumentExporter.Export(definition, report);
                return !report.HasErrors() && Validate(
                    document,
                    report,
                    definition.ControlModuleId,
                    AssetProviderOwner(definition.InputProfile),
                    AssetProviderOwner(definition.GameplayEffectProfile));
            }
            catch (Exception exception)
            {
                report.Error("definition.SkillGraphs", "skill_flow_source_invalid", exception.Message);
                return false;
            }
        }

        static bool ValidateOwner(
            AgentPackageSkillFlowGraphFile graph,
            BtsmtlSkillFlowGraphRole role,
            AgentGraphOwnership ownership,
            string path,
            IReadOnlyList<AgentPackageSkillDefinitionFile> skills,
            AgentCompileReport report)
        {
            if (graph.owner == null)
            {
                report.Error(path + ".owner", "skill_graph_owner_missing", "Skill Graph必须声明显式owner。");
                return false;
            }
            if (role == BtsmtlSkillFlowGraphRole.Skill)
            {
                if (graph.owner.kind != "skill-root" || !IsIdentity(graph.owner.skillId) ||
                    ownership != AgentGraphOwnership.RootAsset)
                {
                    report.Error(path + ".owner", "skill_root_owner_invalid", "Skill根图必须由SkillDefinition以RootAsset ownership拥有。");
                    return false;
                }
                return (skills ?? new List<AgentPackageSkillDefinitionFile>())
                    .Any(value => value != null && string.Equals(value.skillId, graph.owner.skillId, StringComparison.Ordinal));
            }
            if (graph.owner.kind == "shared")
                return ownership == AgentGraphOwnership.SharedAsset && Asset(graph.asset);
            if (graph.owner.kind != "node" && graph.owner.kind != "step" && graph.owner.kind != "timeline-clip")
            {
                report.Error(path + ".owner", "skill_graph_owner_kind_invalid", "Skill Graph owner kind无效。");
                return false;
            }
            if (!IsIdentity(graph.owner.graphId) || !IsIdentity(graph.owner.nodeId) ||
                !IsIdentity(graph.owner.referenceKey) ||
                graph.owner.kind == "timeline-clip" &&
                (!IsIdentity(graph.owner.timelineId) || !IsIdentity(graph.owner.trackId) ||
                 !IsIdentity(graph.owner.clipId)))
            {
                report.Error(path + ".owner", "skill_graph_owner_reference_invalid", "Skill Graph owner必须声明完整的Graph、Node、reference或Timeline Clip身份。");
                return false;
            }
            return ownership == AgentGraphOwnership.Inline || ownership == AgentGraphOwnership.SharedAsset;
        }

        static bool ValidateNodeProperties(
            AgentPackageSkillFlowNode node,
            AgentPackageSkillFlowGraphFile graph,
            IReadOnlyList<AgentPackageSkillMacroFile> macros,
            BtsmtlSkillFlowGraphRole role,
            string path,
            AgentCompileReport report,
            string controlModuleId,
            string inputProviderOwnerId,
            string gameplayProviderOwnerId)
        {
            bool valid = true;
            if (node.properties == null || node.values == null)
            {
                report.Error(path, "skill_node_payload_missing", "Skill Node的properties和values必须是object。");
                valid = false;
            }
            JObject properties = node.properties ?? new JObject();
            valid &= AgentSkillPackageProjection.ValidateProperties(
                node.capability,
                properties,
                report,
                path + ".properties");
            foreach (BtsmtlSkillNodeAuthoringIssue issue in BtsmtlSkillNodeAuthoringValidation.Validate(
                         node.capability,
                         properties,
                         controlModuleId,
                         inputProviderOwnerId,
                         gameplayProviderOwnerId))
            {
                string issuePath = string.IsNullOrEmpty(issue.FieldId)
                    ? path + ".properties"
                    : path + ".properties." + issue.FieldId;
                report.Error(issuePath, issue.Code, issue.Message);
                valid = false;
            }
            if (node.values != null && node.values.Type != JTokenType.Object)
            {
                report.Error(path + ".values", "skill_node_values_invalid", "Skill Node values必须是object。");
                valid = false;
            }
            if (node.values is JObject values)
            {
                IReadOnlyList<AgentPackagePortDescriptor> ports =
                    AgentSkillPackageProjection.ProjectPorts(node, graph, macros);
                foreach (JProperty property in values.Properties())
                {
                    AgentPackagePortDescriptor port = ports.FirstOrDefault(value =>
                        value.key == property.Name && value.direction == "Input" &&
                        !string.IsNullOrEmpty(value.valueType));
                    if (port == null || !BtsmtlSkillNodeAuthoringValidation.MatchesValue(
                            property.Value,
                            port.valueType))
                    {
                        report.Error(path + ".values." + property.Name, "skill_node_value_invalid", "Skill Node常量值必须匹配声明的Value Input port。");
                        valid = false;
                    }
                }
            }
            return valid;
        }

        static bool ValidateSteps(
            IReadOnlyList<AgentPackageSkillFlowStep> value,
            string path,
            AgentCompileReport report)
        {
            if (value == null)
            {
                report.Error(path, "skill_steps_missing", "组合anchor必须声明稳定steps集合。");
                return false;
            }
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentPackageSkillFlowStep step in value)
            {
                if (step == null || !IsIdentity(step.id) || !ids.Add(step.id) ||
                    step.name == null || !Enum.TryParse(step.abortPolicy, false, out ProgramAbortPolicy _))
                {
                    report.Error(path, "skill_step_invalid", "Skill step必须有唯一id、name和合法abortPolicy。");
                    valid = false;
                }
            }
            return valid && ids.Count == value.Count;
        }

        static bool ValidateBlackboard(
            AgentPackageSkillFlowGraphFile graph,
            string path,
            AgentCompileReport report)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentPackageSkillBlackboardDeclaration declaration in graph.blackboardDeclarations ??
                         new List<AgentPackageSkillBlackboardDeclaration>())
            {
                if (declaration == null || !IsIdentity(declaration.id) || !ids.Add(declaration.id) ||
                    string.IsNullOrWhiteSpace(declaration.key) ||
                    !AgentSkillPackageProjection.TryResolveValueType(declaration.valueType, out Type type) ||
                    !Enum.TryParse(declaration.scope, false, out PipelineBlackboardVariableScope _) ||
                    !Enum.TryParse(declaration.lifetime, false, out PipelineBlackboardVariableLifetime _))
                {
                    report.Error(path + ".blackboardDeclarations", "skill_blackboard_invalid", "Skill Blackboard declaration字段无效。");
                    valid = false;
                    continue;
                }
                try
                {
                    PipelineBlackboardInputBinding inputBinding =
                        declaration.inputBinding == null ||
                        string.IsNullOrWhiteSpace(
                            declaration.inputBinding.inputValueId)
                            ? null
                            : new PipelineBlackboardInputBinding(
                                declaration.inputBinding.inputValueId);
                    PipelineBlackboardFactProjection factProjection =
                        declaration.factProjection == null ||
                        string.IsNullOrWhiteSpace(
                            declaration.factProjection.kind) ||
                        string.IsNullOrWhiteSpace(
                            declaration.factProjection.windowType) ||
                        string.IsNullOrWhiteSpace(
                            declaration.factProjection.windowId)
                            ? null
                            : new PipelineBlackboardFactProjection(
                                Enum.Parse<PipelineBlackboardFactProjectionKind>(
                                    declaration.factProjection.kind,
                                    false),
                                declaration.factProjection.windowType,
                                declaration.factProjection.windowId,
                                declaration.factProjection.digest);
                    BtsmtlSkillBlackboardDeclaration.ValidateDefinition(
                        type,
                        Enum.Parse<PipelineBlackboardVariableScope>(declaration.scope, false),
                        Enum.Parse<PipelineBlackboardVariableLifetime>(declaration.lifetime, false),
                        inputBinding,
                        factProjection);
                }
                catch (Exception exception)
                {
                    report.Error(
                        path + ".blackboardDeclarations[" + declaration.id + "]",
                        "skill_blackboard_definition_invalid",
                        exception.Message);
                    valid = false;
                }
                if (declaration.defaultValue != null &&
                    declaration.defaultValue.Type != JTokenType.Null &&
                    !BtsmtlSkillNodeAuthoringValidation.MatchesValue(
                        declaration.defaultValue,
                        declaration.valueType))
                {
                    report.Error(path + ".blackboardDeclarations[" + declaration.id + "].defaultValue", "skill_blackboard_default_invalid", "Skill Blackboard defaultValue必须匹配声明类型。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateMacro(
            AgentPackageSkillMacroFile macro,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            AgentCompileReport report)
        {
            string path = AgentSkillFlowDocumentMapper.MacroPath(macro?.id);
            if (macro == null || !IsIdentity(macro.id) || !IsIdentity(macro.graphId) ||
                !graphs.TryGetValue(macro.graphId, out AgentPackageSkillFlowGraphFile graph) ||
                graph.role != BtsmtlSkillFlowGraphRole.Subgraph.ToString() ||
                macro.id != graph.id ||
                macro.owner == null ||
                macro.owner.kind != graph.owner?.kind ||
                macro.owner.graphId != graph.owner?.graphId ||
                macro.owner.nodeId != graph.owner?.nodeId ||
                !Enum.TryParse(macro.ownership, false, out AgentGraphOwnership ownership) ||
                ownership == AgentGraphOwnership.Unknown ||
                !string.Equals(macro.ownership, graph.ownership, StringComparison.Ordinal) ||
                !LocalIdentity(macro.id) && !Asset(macro.asset))
            {
                report.Error(path, "skill_macro_invalid", "Skill Macro必须引用Subgraph Graph。");
                return false;
            }
            bool valid = true;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillMacroParameter parameter in macro.inputs ??
                         new List<AgentPackageSkillMacroParameter>())
            {
                if (!ValidParameter(parameter, ids, path + ".inputs", report))
                    valid = false;
            }
            foreach (AgentPackageSkillMacroParameter parameter in macro.outputs ??
                         new List<AgentPackageSkillMacroParameter>())
                if (!ValidParameter(parameter, ids, path + ".outputs", report))
                    valid = false;
            if (valid)
            {
                DynamicParameterDefinition Project(AgentPackageSkillMacroParameter parameter)
                {
                    AgentSkillPackageProjection.TryResolveValueType(parameter.valueType, out Type type);
                    return new DynamicParameterDefinition(parameter.id, parameter.name, type);
                }
                try
                {
                    BtsmtlSkillMacroInterface.Validate(
                        (macro.inputs ?? new List<AgentPackageSkillMacroParameter>()).Select(Project),
                        (macro.outputs ?? new List<AgentPackageSkillMacroParameter>()).Select(Project));
                }
                catch (InvalidOperationException exception)
                {
                    report.Error(path, "skill_macro_interface_invalid", exception.Message);
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidParameter(AgentPackageSkillMacroParameter parameter, ISet<string> ids, string path, AgentCompileReport report)
        {
            if (parameter == null || !IsIdentity(parameter.id) || !ids.Add(parameter.id) ||
                string.IsNullOrWhiteSpace(parameter.name) ||
                !AgentSkillPackageProjection.TryResolveValueType(parameter.valueType, out Type type) ||
                AgentSkillPackageProjection.ValueType(type) != parameter.valueType)
            {
                report.Error(path, "skill_macro_parameter_invalid", "Macro parameter必须有稳定identity、名称和稳定值类型。");
                return false;
            }
            return true;
        }

        static bool ValidateTimeline(
            AgentPackageSkillTimelineFile timeline,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            AgentCompileReport report)
        {
            string path = AgentSkillFlowDocumentMapper.TimelinePath(timeline?.id);
            if (timeline == null || !IsIdentity(timeline.id) ||
                !Enum.TryParse(timeline.ownership, false, out BtsmtlSkillTimelineOwnership _) ||
                timeline.asset == null && !LocalIdentity(timeline.id) ||
                LocalIdentity(timeline.id) && timeline.asset != null ||
                !LocalIdentity(timeline.id) && !Asset(timeline.asset) ||
                LocalIdentity(timeline.id) && timeline.ownership != BtsmtlSkillTimelineOwnership.Private.ToString() ||
                timeline.sections == null || timeline.externalBindings == null || timeline.tracks == null)
            {
                report.Error(path, "skill_timeline_invalid", "Skill Timeline identity、ownership或资产引用无效。");
                return false;
            }
            TimelineContractCatalog catalog = TimelineTreeContractComposition.Create();
            bool valid = IsIdentity(timeline.ownerGraphId) && IsIdentity(timeline.ownerNodeId) &&
                         graphs.ContainsKey(timeline.ownerGraphId);
            if (!valid)
                report.Error(path + ".ownerGraphId", "skill_timeline_owner_invalid", "Skill Timeline必须声明存在的owner Graph与owner Node。");
            if (valid &&
                (!(graphs[timeline.ownerGraphId].nodes ?? new List<AgentPackageSkillFlowNode>()).Any(value => value != null && value.id == timeline.ownerNodeId &&
                    BtsmtlSkillNodeAuthoringValidation.HasRule(
                        value.capability,
                        BtsmtlSkillNodeAuthoringRule.TimelineReference) &&
                    value.properties?.Value<string>("timelineId") == timeline.id)))
            {
                report.Error(path + ".ownerNodeId", "skill_timeline_owner_reference_invalid", "Skill Timeline必须由对应Timeline节点反向拥有。");
                valid = false;
            }
            if (timeline.callSites == null)
            {
                report.Error(path + ".callSites", "skill_timeline_call_sites_missing", "Skill Timeline callSites集合不能为null。");
                valid = false;
            }
            var callSites = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineCallSite callSite in timeline.callSites ?? new List<AgentPackageSkillTimelineCallSite>())
            {
                bool callSiteValid = callSite != null &&
                    IsIdentity(callSite.graphId) &&
                    IsIdentity(callSite.nodeId) &&
                    graphs.TryGetValue(callSite.graphId, out AgentPackageSkillFlowGraphFile callGraph) &&
                    (callGraph.nodes ?? new List<AgentPackageSkillFlowNode>()).Any(value =>
                        value != null && value.id == callSite.nodeId &&
                        BtsmtlSkillNodeAuthoringValidation.HasRule(
                            value.capability,
                            BtsmtlSkillNodeAuthoringRule.TimelineReference) &&
                        value.properties?.Value<string>("timelineId") == timeline.id) &&
                    Enum.TryParse(callSite.playbackMode, false, out TimelinePlaybackMode playbackMode) &&
                    Enum.IsDefined(typeof(TimelinePlaybackMode), playbackMode) &&
                    callSites.Add(callSite.graphId + "\0" + callSite.nodeId);
                if (!callSiteValid)
                {
                    report.Error(path + ".callSites", "skill_timeline_call_site_invalid", "Skill Timeline callSite必须唯一引用对应的Timeline节点。");
                    valid = false;
                }
            }
            if (callSites.Count == 0)
            {
                report.Error(path + ".callSites", "skill_timeline_call_site_missing", "Skill Timeline至少需要一个正式调用位置。");
                valid = false;
            }
            var sectionIds = new HashSet<string>(StringComparer.Ordinal);
            var sectionNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineSection section in timeline.sections ??
                         new List<AgentPackageSkillTimelineSection>())
            {
                if (section == null || !IsIdentity(section.id) || !sectionIds.Add(section.id) ||
                    string.IsNullOrWhiteSpace(section.name) || section.name != section.name.Trim() ||
                    !sectionNames.Add(section.name) || section.frame < 0 ||
                    !string.IsNullOrEmpty(section.nextSectionId) && !IsIdentity(section.nextSectionId))
                {
                    report.Error(path + ".sections", "skill_timeline_section_invalid", "Skill Timeline Section必须有唯一identity、名称和非负frame。");
                    valid = false;
                }
            }
            foreach (AgentPackageSkillTimelineSection section in timeline.sections ??
                         new List<AgentPackageSkillTimelineSection>())
            {
                if (section != null && !string.IsNullOrEmpty(section.nextSectionId) &&
                    sectionIds.Contains(section.id) && !sectionIds.Contains(section.nextSectionId))
                {
                    report.Error(path + ".sections[" + section.id + "].nextSectionId", "skill_timeline_section_next_invalid", "Timeline Section的nextSectionId必须引用同一Timeline中的Section。");
                    valid = false;
                }
            }
            var bindingIds = new HashSet<string>(StringComparer.Ordinal);
            var bindingNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineExternalBinding binding in timeline.externalBindings ??
                         new List<AgentPackageSkillTimelineExternalBinding>())
            {
                if (binding == null || !IsIdentity(binding.id) || !bindingIds.Add(binding.id) ||
                    !IsIdentity(binding.bindingId) || !bindingNames.Add(binding.bindingId) ||
                    string.IsNullOrWhiteSpace(binding.displayName) || binding.displayName != binding.displayName.Trim() ||
                    string.IsNullOrWhiteSpace(binding.domain) || binding.domain != binding.domain.Trim() ||
                    !Enum.TryParse(binding.valueKind, false, out TimelineBindingValueKind valueKind) ||
                    !Enum.IsDefined(typeof(TimelineBindingValueKind), valueKind) ||
                    !Enum.TryParse(binding.access, false, out TimelineBindingAccess access) ||
                    !Enum.IsDefined(typeof(TimelineBindingAccess), access) ||
                    !Enum.TryParse(binding.lifetime, false, out TimelineBindingLifetime lifetime) ||
                    !Enum.IsDefined(typeof(TimelineBindingLifetime), lifetime) ||
                    valueKind == TimelineBindingValueKind.Target && !string.IsNullOrEmpty(binding.parameterId) ||
                    valueKind != TimelineBindingValueKind.Target && !IsIdentity(binding.parameterId) ||
                    access == TimelineBindingAccess.Input && lifetime != TimelineBindingLifetime.Call ||
                    access != TimelineBindingAccess.Input && lifetime != TimelineBindingLifetime.Tick)
                {
                    report.Error(path + ".externalBindings", "skill_timeline_binding_invalid", "Skill Timeline ExternalBinding字段无效。");
                    valid = false;
                }
            }
            var trackIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (AgentPackageSkillTimelineTrack track in timeline.tracks ??
                         new List<AgentPackageSkillTimelineTrack>())
            {
                if (track == null || !IsIdentity(track.id) || !trackIds.Add(track.id) ||
                    track.clips == null ||
                    !catalog.TryGetTrack(track.kind, out _))
                {
                    report.Error(path + ".tracks", "skill_timeline_track_invalid", "Skill Timeline track identity或kind无效。");
                    valid = false;
                    continue;
                }
                foreach (TimelineAuthoringTrackIssue issue in TimelineAuthoringTrackBinding.Validate(
                             track.kind,
                             track.animationChannelId,
                             track.animationSlotId))
                {
                    report.Error(
                        path + ".tracks[" + track.id + "]." + issue.FieldId,
                        issue.ErrorCode,
                        issue.ErrorMessage);
                    valid = false;
                }
                var clipIds = new HashSet<string>(StringComparer.Ordinal);
                var curveIds = new HashSet<string>(StringComparer.Ordinal);
                catalog.TryGetTrack(track.kind, out TimelineTrackContract trackContract);
                foreach (AgentPackageSkillTimelineClip clip in track.clips ??
                         new List<AgentPackageSkillTimelineClip>())
                {
                    catalog.TryGetClip(clip?.kind, out TimelineClipContract clipContract);
                    if (clip == null || !IsIdentity(clip.id) || !clipIds.Add(clip.id) ||
                        clipContract == null ||
                        !trackContract.AllowsClip(clip.kind) ||
                        clip.startFrame < 0 || clip.endFrame < clip.startFrame ||
                        clipContract.RequiresPositiveDuration && clip.endFrame <= clip.startFrame ||
                        clip.clipInFrame < 0)
                    {
                        report.Error(path + ".tracks[" + track.id + "].clips", "skill_timeline_clip_invalid", "Skill Timeline clip字段无效。");
                        valid = false;
                    }
                    valid &= ValidateClipProperties(
                        clip,
                        clipContract,
                        timeline.externalBindings,
                        graphs,
                        path + ".tracks[" + track.id + "].clips[" + (clip?.id ?? string.Empty) + "]",
                        report);
                    if (!string.IsNullOrEmpty(clip?.treeGraphId) &&
                        (!graphs.ContainsKey(clip.treeGraphId) || clip.treeOwnership != TimelineTreeOwnership.AssetGraph.ToString()))
                    {
                        report.Error(path + ".tracks[" + track.id + "].clips[" + clip.id + "]", "skill_tree_clip_reference_invalid", "Skill TreeClip必须引用Skill TimelineBody Graph。");
                        valid = false;
                    }
                    foreach (AgentPackageCurve curve in clip?.curves ?? new List<AgentPackageCurve>())
                        if (curve == null || curve.clipId != clip.id ||
                            !curveIds.Add(clip.id + "\0" + curve.channelId) ||
                            !ValidateCurve(curve, path + ".tracks[" + track.id + "].clips[" + clip.id + "].curves", report))
                            valid = false;
                    valid &= RejectInternalFields(
                        clip?.properties,
                        path + ".tracks[" + track.id + "].clips[" + (clip?.id ?? string.Empty) + "].properties",
                        report);
                }
            }
            return valid;
        }

        static bool ValidateClipProperties(
            AgentPackageSkillTimelineClip clip,
            TimelineClipContract contract,
            IReadOnlyList<AgentPackageSkillTimelineExternalBinding> bindings,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            string path,
            AgentCompileReport report)
        {
            if (clip == null || contract == null)
                return false;
            JObject properties = clip.properties ?? new JObject();
            bool valid = TimelineAuthoringClipBinding.ValidateProperties(
                clip.kind,
                properties,
                clip.startFrame,
                clip.endFrame,
                (property, message) => report.Error(
                    path + ".properties." + property,
                    "skill_timeline_property_invalid",
                    message));
            if (clip.kind == TimelineContractKinds.AnimationClip)
                valid &= Asset(clip.animationClip);
            if (clip.kind == TimelineContractKinds.TreeClip &&
                (!IsIdentity(clip.treeGraphId) ||
                 !graphs.ContainsKey(clip.treeGraphId) ||
                 graphs[clip.treeGraphId].role != BtsmtlSkillFlowGraphRole.TimelineBody.ToString() ||
                 clip.treeOwnership != TimelineTreeOwnership.AssetGraph.ToString() ||
                 !Enum.TryParse(clip.treePhase, false, out TimelineTreeExecutionPhase treePhase) ||
                 !Enum.IsDefined(typeof(TimelineTreeExecutionPhase), treePhase)))
            {
                report.Error(path, "skill_tree_clip_reference_invalid", "TreeClip必须引用TimelineBody Graph并声明合法执行阶段。");
                valid = false;
            }
            if (clip.kind == TimelineContractKinds.ScenePresentationParameterCurveClip)
            {
                string targetBindingId = properties.Value<string>("targetBindingId");
                string parameterBindingId = properties.Value<string>("parameterBindingId");
                AgentPackageSkillTimelineExternalBinding targetBinding = (bindings ?? new List<AgentPackageSkillTimelineExternalBinding>())
                    .FirstOrDefault(value => value != null && value.bindingId == targetBindingId);
                AgentPackageSkillTimelineExternalBinding parameterBinding = (bindings ?? new List<AgentPackageSkillTimelineExternalBinding>())
                    .FirstOrDefault(value => value != null && value.bindingId == parameterBindingId);
                if (!IsIdentity(targetBindingId) ||
                    targetBinding == null ||
                    targetBinding.valueKind != TimelineBindingValueKind.Target.ToString() ||
                    targetBinding.access != TimelineBindingAccess.Input.ToString() ||
                    targetBinding.lifetime != TimelineBindingLifetime.Call.ToString() ||
                    !IsIdentity(parameterBindingId) ||
                    parameterBinding == null ||
                    (parameterBinding.valueKind != TimelineBindingValueKind.Scalar.ToString() &&
                     parameterBinding.valueKind != TimelineBindingValueKind.Boolean.ToString()) ||
                    parameterBinding.access != TimelineBindingAccess.Write.ToString() ||
                    parameterBinding.lifetime != TimelineBindingLifetime.Tick.ToString() ||
                    !IsIdentity(parameterBinding.parameterId) ||
                    !ValidateInlineCurve(properties["valueCurve"], path + ".properties.valueCurve", report))
                {
                    report.Error(path + ".properties", "skill_scene_presentation_clip_invalid", "Scene Presentation Clip必须引用有效ExternalBinding并声明valueCurve。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateInlineCurve(JToken token, string path, AgentCompileReport report)
        {
            if (token is not JObject value ||
                !Enum.TryParse(value.Value<string>("preWrapMode"), false, out WrapMode preWrap) ||
                !Enum.IsDefined(typeof(WrapMode), preWrap) ||
                !Enum.TryParse(value.Value<string>("postWrapMode"), false, out WrapMode postWrap) ||
                !Enum.IsDefined(typeof(WrapMode), postWrap) ||
                value["keys"] is not JArray keys || keys.Count == 0)
            {
                report.Error(path, "skill_timeline_curve_invalid", "Timeline内嵌curve必须声明合法wrap mode与keys。");
                return false;
            }
            float previous = -1f;
            foreach (JObject key in keys.OfType<JObject>())
            {
                if (!Finite(key.Value<float>("time")) || key.Value<float>("time") < 0f ||
                    key.Value<float>("time") > 1f || key.Value<float>("time") <= previous ||
                    !Finite(key.Value<float>("value")) || !Finite(key.Value<float>("inTangent")) ||
                    !Finite(key.Value<float>("outTangent")) || !Finite(key.Value<float>("inWeight")) ||
                    !Finite(key.Value<float>("outWeight")) ||
                    !Enum.TryParse(key.Value<string>("weightedMode"), false, out WeightedMode weightedMode) ||
                    !Enum.IsDefined(typeof(WeightedMode), weightedMode))
                {
                    report.Error(path, "skill_timeline_curve_key_invalid", "Timeline内嵌curve key必须按normalized time递增并使用有限数值。");
                    return false;
                }
                previous = key.Value<float>("time");
            }
            return keys.OfType<JObject>().Count() == keys.Count;
        }

        static bool RejectInternalFields(JToken token, string path, AgentCompileReport report)
        {
            if (token is JObject value)
            {
                bool valid = true;
                foreach (JProperty property in value.Properties())
                {
                    if (property.Name.StartsWith("m_", StringComparison.Ordinal) ||
                        property.Name == "typeName" ||
                        property.Name.IndexOf("runtime", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        property.Name.IndexOf("compiled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        property.Name.IndexOf("generated", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        property.Name.IndexOf("cache", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        report.Error(path + "." + property.Name, "skill_internal_field_forbidden", "Skill Flow Document禁止私有字段、运行状态和编译缓存。");
                        valid = false;
                    }
                    valid &= RejectInternalFields(property.Value, path + "." + property.Name, report);
                }
                return valid;
            }
            if (token is JArray array)
                return array.Select((value, index) => RejectInternalFields(value, path + "[" + index + "]", report)).All(value => value);
            return true;
        }

        static bool ValidateCurve(AgentPackageCurve curve, string path, AgentCompileReport report)
        {
            TimelineCurveChannelDescriptor descriptor = null;
            bool hasDescriptor = curve != null && !string.IsNullOrWhiteSpace(curve.channelId) &&
                TimelineCurveChannelCatalog.TryGet(curve.channelId, out descriptor);
            bool domainValid = hasDescriptor && curve.timeDomain == descriptor.TimeDomain.ToString();
            bool boundedValid = hasDescriptor && curve.bounded == descriptor.ValueDomain.IsBounded;
            bool minimumValid = hasDescriptor && curve.minimum == descriptor.ValueDomain.Minimum;
            bool maximumValid = hasDescriptor && curve.maximum == descriptor.ValueDomain.Maximum;
            bool zeroValid = hasDescriptor && curve.zero == descriptor.ValueDomain.Zero;
            bool unitValid = hasDescriptor && string.Equals(curve.unit ?? string.Empty, descriptor.ValueDomain.Unit, StringComparison.Ordinal);
            bool preValid = curve != null && Enum.TryParse(curve.preWrapMode, false, out WrapMode pre) && Enum.IsDefined(typeof(WrapMode), pre);
            bool postValid = curve != null && Enum.TryParse(curve.postWrapMode, false, out WrapMode post) && Enum.IsDefined(typeof(WrapMode), post);
            bool keysValid = curve?.keys != null && curve.keys.Count > 0;
            bool invalid = curve == null || !hasDescriptor || !domainValid || !boundedValid || !minimumValid ||
                !maximumValid || !zeroValid || !unitValid || !preValid || !postValid || !keysValid;
            if (invalid)
            {
                report.Error(path, "skill_timeline_curve_invalid", "Skill Timeline curve channel或keys无效。");
                return false;
            }
            float previous = -1f;
            for (int index = 0; index < curve.keys.Count; index++)
            {
                AgentAnimationCurveKey key = curve.keys[index];
                if (key == null || key.time < 0f || key.time > 1f || key.time <= previous ||
                    !Finite(key.time) || !Finite(key.value) || !Finite(key.inTangent) ||
                    !Finite(key.outTangent) || !Finite(key.inWeight) || !Finite(key.outWeight) ||
                    !Enum.TryParse(key.weightedMode, false, out WeightedMode weighted) ||
                    !Enum.IsDefined(typeof(WeightedMode), weighted))
                {
                    report.Error(path, "skill_timeline_curve_key_invalid", "Skill Timeline curve key必须按normalized time递增并使用有限数值。");
                    return false;
                }
                previous = key.time;
            }
            return true;
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static bool ValidateSkillRoots(
            IReadOnlyList<AgentPackageSkillDefinitionFile> skills,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            AgentCompileReport report)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var skillById = new Dictionary<string, AgentPackageSkillDefinitionFile>(StringComparer.Ordinal);
            bool valid = true;
            if (skills == null || skills.Count == 0)
            {
                report.Error("editable/skills", "skill_definition_missing", "Skill Flow Document至少需要一个SkillDefinition。");
                return false;
            }
            foreach (AgentPackageSkillDefinitionFile skill in skills ??
                         new List<AgentPackageSkillDefinitionFile>())
            {
                if (skill == null || !IsIdentity(skill.skillId) || !ids.Add(skill.skillId) ||
                    !IsIdentity(skill.entryGraphAuthoringId) ||
                    !graphs.TryGetValue(skill.entryGraphAuthoringId, out AgentPackageSkillFlowGraphFile graph) ||
                    graph.role != BtsmtlSkillFlowGraphRole.Skill.ToString())
                {
                    report.Error("editable/skills", "skill_root_invalid", "SkillDefinition必须引用Document中的Skill根Graph。");
                    valid = false;
                    continue;
                }
                skillById.Add(skill.skillId, skill);
                if (!IsIdentity(skill.actionProfileId) || !IsIdentity(skill.actionContext))
                {
                    report.Error("editable/skills[" + skill.skillId + "]", "skill_reference_invalid", "SkillDefinition必须引用稳定ActionProfile与ActionContext identity。");
                    valid = false;
                }
                var dependencies = new HashSet<string>(StringComparer.Ordinal);
                foreach (AgentPackageSkillSubgraphDependency dependency in skill.subgraphDependencies ??
                             new List<AgentPackageSkillSubgraphDependency>())
                {
                    bool dependencyValid = dependency != null &&
                        IsIdentity(dependency.subgraphIdentity) &&
                        graphs.TryGetValue(dependency.subgraphIdentity, out AgentPackageSkillFlowGraphFile subgraph) &&
                        subgraph.role == BtsmtlSkillFlowGraphRole.Subgraph.ToString() &&
                        IsIdentity(dependency.callSiteIdentity) &&
                        dependencies.Add(dependency.subgraphIdentity + "\0" + dependency.callSiteIdentity) &&
                        TryResolveMacroCallSite(
                            skill.skillId,
                            dependency.callSiteIdentity,
                            dependency.subgraphIdentity,
                            graphs);
                    if (!dependencyValid)
                    {
                        report.Error("editable/skills[" + skill.skillId + "].subgraphDependencies", "skill_dependency_invalid", "Skill子图依赖必须引用唯一的Subgraph与调用位置。");
                        valid = false;
                    }
                }
            }
            foreach (AgentPackageSkillDefinitionFile skill in skills ??
                         new List<AgentPackageSkillDefinitionFile>())
            {
                if (skill == null)
                    continue;
                var followUps = new HashSet<string>(StringComparer.Ordinal);
                foreach (string followUp in skill.allowedFollowUpSkillIds ?? new List<string>())
                {
                    if (!IsIdentity(followUp) || followUp == skill.skillId ||
                        !followUps.Add(followUp) || !skillById.ContainsKey(followUp))
                    {
                        report.Error("editable/skills[" + skill.skillId + "].allowedFollowUpSkillIds", "skill_follow_up_invalid", "Skill后续技能引用必须指向已声明且非自身的唯一Skill。");
                        valid = false;
                    }
                }
            }
            return valid;
        }

        static bool ValidateReferences(
            AgentPackageSkillFlowDocument document,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            AgentCompileReport report)
        {
            var timelines = (document.timelines ?? new List<AgentPackageSkillTimelineFile>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.id))
                .GroupBy(value => value.id, StringComparer.Ordinal)
                .ToDictionary(value => value.Key, value => value.First(), StringComparer.Ordinal);
            bool valid = true;
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ??
                         new List<AgentPackageSkillFlowGraphFile>())
            {
                if (graph == null)
                {
                    valid = false;
                    continue;
                }
                foreach (AgentPackageSkillFlowNode node in graph.nodes ??
                             new List<AgentPackageSkillFlowNode>())
                {
                    if (node == null)
                    {
                        valid = false;
                        continue;
                    }
                    valid &= ValidateBlackboardNodeReference(
                        node,
                        graphs,
                        AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".nodes[" + node.id + "]",
                        report);
                    foreach (BtsmtlSkillGraphReferenceAttribute reference in
                             BtsmtlSkillGraphAuthoringMetadata.GraphReferences(node.capability))
                    {
                        string child = node.properties?.Value<string>(reference.FieldId);
                        if (string.IsNullOrEmpty(child))
                            continue;
                        if (!graphs.ContainsKey(child))
                        {
                            report.Error(
                                AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".nodes[" + node.id + "].properties." + reference.FieldId,
                                "skill_graph_reference_missing",
                                $"Skill Graph reference不存在：{child}");
                            valid = false;
                            continue;
                        }
                        if (graphs[child].role != reference.Role.ToString())
                        {
                            report.Error(
                                AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".nodes[" + node.id + "].properties." + reference.FieldId,
                                "skill_graph_reference_role_invalid",
                                "Skill Graph引用目标role与正式metadata不一致。");
                            valid = false;
                        }
                    }
                    if (BtsmtlSkillNodeAuthoringValidation.HasRule(
                            node.capability,
                            BtsmtlSkillNodeAuthoringRule.TimelineReference))
                    {
                        string timelineId = node.properties?.Value<string>("timelineId");
                        if (string.IsNullOrEmpty(timelineId) || !timelines.ContainsKey(timelineId))
                        {
                            report.Error(AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".nodes[" + node.id + "]", "skill_timeline_reference_missing", "Skill Timeline节点必须引用Document中的Timeline。");
                            valid = false;
                        }
                    }
                    JArray steps = node.properties?["steps"] as JArray;
                    foreach (JObject step in steps?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                    {
                        string condition = step.Value<string>("conditionGraphId");
                        if (!string.IsNullOrEmpty(condition) && !graphs.ContainsKey(condition))
                        {
                            report.Error(AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".nodes[" + node.id + "].properties.steps", "skill_condition_graph_missing", $"Condition Graph不存在：{condition}");
                            valid = false;
                        }
                        if (!string.IsNullOrEmpty(condition) && graphs.TryGetValue(condition, out AgentPackageSkillFlowGraphFile conditionGraph) &&
                            conditionGraph.role != BtsmtlSkillFlowGraphRole.ConditionRule.ToString())
                        {
                            report.Error(AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".nodes[" + node.id + "].properties.steps", "skill_condition_graph_role_invalid", "Step condition必须引用ConditionRule Graph。");
                            valid = false;
                        }
                    }
                }
                foreach (AgentPackageSkillGraphAnchor anchor in graph.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                    foreach (AgentPackageSkillFlowStep step in anchor?.steps ?? new List<AgentPackageSkillFlowStep>())
                        if (!string.IsNullOrEmpty(step?.conditionGraphId) && !graphs.ContainsKey(step.conditionGraphId))
                        {
                            report.Error(AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".anchors[" + anchor.kind + "].steps", "skill_condition_graph_missing", $"Condition Graph不存在：{step.conditionGraphId}");
                            valid = false;
                        }
                        else if (!string.IsNullOrEmpty(step?.conditionGraphId) &&
                                 graphs.TryGetValue(step.conditionGraphId, out AgentPackageSkillFlowGraphFile anchorCondition) &&
                                 anchorCondition.role != BtsmtlSkillFlowGraphRole.ConditionRule.ToString())
                        {
                            report.Error(AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".anchors[" + anchor.kind + "].steps", "skill_condition_graph_role_invalid", "Step condition必须引用ConditionRule Graph。");
                            valid = false;
                        }
            }
            foreach (AgentPackageSkillFlowGraphFile graph in document.graphs ??
                         new List<AgentPackageSkillFlowGraphFile>())
            {
                if (graph == null)
                {
                    valid = false;
                    continue;
                }
                if (graph.owner == null || graph.owner.kind == "skill-root")
                    continue;
                if (!string.IsNullOrEmpty(graph.owner.graphId) && !graphs.ContainsKey(graph.owner.graphId))
                {
                    report.Error(AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".owner", "skill_owner_graph_missing", "Skill Graph owner graph不存在。");
                    valid = false;
                }
                if (!graphs.TryGetValue(graph.owner.graphId ?? string.Empty, out AgentPackageSkillFlowGraphFile ownerGraph))
                    continue;
                AgentPackageSkillFlowNode ownerNode = (ownerGraph.nodes ?? new List<AgentPackageSkillFlowNode>())
                    .FirstOrDefault(value => value != null && value.id == graph.owner.nodeId);
                AgentPackageSkillGraphAnchor ownerAnchor = (ownerGraph.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                    .FirstOrDefault(value => value != null && value.nodeId == graph.owner.nodeId);
                bool referenced = false;
                if (ownerNode != null)
                {
                    referenced = BtsmtlSkillGraphAuthoringMetadata.GraphReferences(ownerNode.capability)
                                     .Any(reference => ownerNode.properties?.Value<string>(reference.FieldId) == graph.id) ||
                                 (ownerNode.properties?["steps"] as JArray)?.OfType<JObject>()
                                     .Any(value => value.Value<string>("id") == graph.owner.referenceKey &&
                                                  value.Value<string>("conditionGraphId") == graph.id) == true;
                }
                if (ownerAnchor != null && graph.owner.kind == "step")
                    referenced = (ownerAnchor.steps ?? new List<AgentPackageSkillFlowStep>())
                        .Any(value => value != null && value.id == graph.owner.referenceKey &&
                                      value.conditionGraphId == graph.id);
                if (graph.owner.kind == "timeline-clip")
                    referenced = timelines.Values.SelectMany(value => value.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                        .SelectMany(value => value?.clips ?? new List<AgentPackageSkillTimelineClip>())
                        .Any(value => value != null && value.id == graph.owner.clipId && value.treeGraphId == graph.id);
                if (!referenced && graph.owner.kind != "shared")
                {
                    report.Error(AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".owner", "skill_owner_reference_missing", "Skill Graph owner没有对应的正式调用引用。");
                    valid = false;
                }
            }
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Stack<string>((document.skills ?? new List<AgentPackageSkillDefinitionFile>())
                .Where(value => value != null && !string.IsNullOrEmpty(value.entryGraphAuthoringId))
                .Select(value => value.entryGraphAuthoringId));
            while (pending.Count > 0)
            {
                string graphId = pending.Pop();
                if (!reachable.Add(graphId) || !graphs.TryGetValue(graphId, out AgentPackageSkillFlowGraphFile graph))
                    continue;
                foreach (AgentPackageSkillFlowNode node in graph.nodes ?? new List<AgentPackageSkillFlowNode>())
                {
                    foreach (BtsmtlSkillGraphReferenceAttribute reference in
                             BtsmtlSkillGraphAuthoringMetadata.GraphReferences(node.capability))
                    {
                        string child = node.properties?.Value<string>(reference.FieldId);
                        if (!string.IsNullOrEmpty(child))
                            pending.Push(child);
                    }
                    foreach (JObject step in (node.properties?["steps"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                        if (!string.IsNullOrEmpty(step.Value<string>("conditionGraphId")))
                            pending.Push(step.Value<string>("conditionGraphId"));
                    if (!BtsmtlSkillNodeAuthoringValidation.HasRule(
                            node.capability,
                            BtsmtlSkillNodeAuthoringRule.TimelineReference))
                        continue;
                    string timelineId = node.properties?.Value<string>("timelineId");
                    if (!timelines.TryGetValue(timelineId ?? string.Empty, out AgentPackageSkillTimelineFile timeline))
                        continue;
                    foreach (AgentPackageSkillTimelineClip clip in (timeline.tracks ?? new List<AgentPackageSkillTimelineTrack>()).SelectMany(value => value?.clips ?? new List<AgentPackageSkillTimelineClip>()))
                        if (!string.IsNullOrEmpty(clip.treeGraphId))
                            pending.Push(clip.treeGraphId);
                }
                foreach (AgentPackageSkillGraphAnchor anchor in graph.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                    foreach (AgentPackageSkillFlowStep step in anchor?.steps ?? new List<AgentPackageSkillFlowStep>())
                        if (!string.IsNullOrEmpty(step?.conditionGraphId))
                            pending.Push(step.conditionGraphId);
            }
            foreach (string graphId in graphs.Keys)
                if (!reachable.Contains(graphId) && graphs[graphId].owner?.kind != "shared")
                {
                    report.Error(AgentSkillFlowDocumentMapper.GraphPath(graphId), "skill_graph_unreachable", "Skill Graph未处于任何Skill入口的正式闭包中。");
                    valid = false;
                }
            valid &= ValidateGraphCycles(document, graphs, timelines, report);
            return valid;
        }

        static bool TryResolveMacroCallSite(
            string skillId,
            string callSite,
            string subgraphId,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs)
        {
            string prefix = "skill:" + skillId + "/";
            if (!callSite.StartsWith(prefix, StringComparison.Ordinal) ||
                !callSite.EndsWith("/call:Macro", StringComparison.Ordinal))
                return false;
            int nodeMarker = callSite.LastIndexOf("/node:", StringComparison.Ordinal);
            if (nodeMarker < 0)
                return false;
            int graphMarker = callSite.LastIndexOf("/graph:", nodeMarker);
            if (graphMarker < prefix.Length || nodeMarker <= graphMarker)
                return false;
            string ownerGraphId = callSite.Substring(
                graphMarker + "/graph:".Length,
                nodeMarker - graphMarker - "/graph:".Length);
            string nodeId = callSite.Substring(
                nodeMarker + "/node:".Length,
                callSite.Length - nodeMarker - "/node:".Length - "/call:Macro".Length);
            return graphs.TryGetValue(ownerGraphId, out AgentPackageSkillFlowGraphFile owner) &&
                   (owner.nodes ?? new List<AgentPackageSkillFlowNode>()).Any(value =>
                       value != null &&
                       value.id == nodeId &&
                       BtsmtlSkillGraphAuthoringMetadata.GraphReferences(value.capability)
                           .Any(reference =>
                               reference.Role == BtsmtlSkillFlowGraphRole.Subgraph &&
                               value.properties?.Value<string>(reference.FieldId) == subgraphId));
        }

        static bool ValidateBlackboardNodeReference(
            AgentPackageSkillFlowNode node,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            string path,
            AgentCompileReport report)
        {
            if (node == null)
                return false;
            bool blackboardNode = BtsmtlSkillCapabilityCatalog.TryResolveType(
                                      node.capability,
                                      out Type nodeType) &&
                                  typeof(IBtsmtlSkillBlackboardAccessNode).IsAssignableFrom(
                                      nodeType);
            bool valid = true;
            if (blackboardNode)
            {
                string declarationId = node.properties?.Value<string>("declarationId");
                string ownerId = node.properties?.Value<string>("ownerId");
                AgentPackageSkillBlackboardDeclaration declaration = null;
                if (graphs.TryGetValue(ownerId ?? string.Empty, out AgentPackageSkillFlowGraphFile owner))
                    declaration = (owner.blackboardDeclarations ?? new List<AgentPackageSkillBlackboardDeclaration>())
                        .FirstOrDefault(value => value != null && value.id == declarationId);
                if (declaration == null)
                {
                    report.Error(path + ".properties", "skill_blackboard_reference_missing", "Skill Blackboard节点必须引用owner Graph中的正式declaration。");
                    valid = false;
                }
                else
                {
                    string expectedType = node.properties?.Value<string>("valueType");
                    if (BtsmtlSkillNodeAuthoringValidation.TryGetRule(
                            node.capability,
                            BtsmtlSkillNodeAuthoringRule.BlackboardValueType,
                            "valueType",
                            out BtsmtlSkillNodeAuthoringRuleAttribute valueTypeRule))
                        expectedType = valueTypeRule.ExpectedValue;
                    if (!string.Equals(expectedType, declaration.valueType, StringComparison.Ordinal))
                    {
                        report.Error(path + ".properties.valueType", "skill_blackboard_reference_type_invalid", "Skill Blackboard节点类型必须与owner declaration类型一致。");
                        valid = false;
                    }
                }
            }
            if (BtsmtlSkillNodeAuthoringValidation.HasRule(
                    node.capability,
                    BtsmtlSkillNodeAuthoringRule.TargetSnapshotObject) &&
                node.properties?["targetSnapshot"] is JObject targetSnapshot)
            {
                string declarationId = targetSnapshot.Value<string>("id");
                string ownerId = targetSnapshot.Value<string>("ownerId");
                if (string.IsNullOrEmpty(declarationId) && string.IsNullOrEmpty(ownerId))
                    return valid;
                AgentPackageSkillBlackboardDeclaration declaration = null;
                if (graphs.TryGetValue(ownerId ?? string.Empty, out AgentPackageSkillFlowGraphFile owner))
                    declaration = (owner.blackboardDeclarations ?? new List<AgentPackageSkillBlackboardDeclaration>())
                        .FirstOrDefault(value => value != null && value.id == declarationId);
                if (declaration == null || declaration.valueType != "action-target-snapshot")
                {
                    report.Error(path + ".properties.targetSnapshot", "skill_target_snapshot_reference_invalid", "Skill CanActivate节点的TargetSnapshot必须引用ActionTargetSnapshot declaration。");
                    valid = false;
                }
            }
            return valid;
        }

        static bool ValidateGraphCycles(
            AgentPackageSkillFlowDocument document,
            IReadOnlyDictionary<string, AgentPackageSkillFlowGraphFile> graphs,
            IReadOnlyDictionary<string, AgentPackageSkillTimelineFile> timelines,
            AgentCompileReport report)
        {
            var states = new Dictionary<string, int>(StringComparer.Ordinal);
            bool valid = true;
            bool Visit(string graphId, string path)
            {
                if (!graphs.TryGetValue(graphId ?? string.Empty, out AgentPackageSkillFlowGraphFile graph))
                    return true;
                if (states.TryGetValue(graphId, out int state))
                {
                    if (state == 1)
                    {
                        report.Error(path, "skill_graph_recursive", $"Skill Graph闭包包含递归引用：{graphId}");
                        return false;
                    }
                    return true;
                }
                states[graphId] = 1;
                bool result = true;
                foreach (AgentPackageSkillFlowNode node in graph.nodes ?? new List<AgentPackageSkillFlowNode>())
                {
                    if (node == null)
                        continue;
                    foreach (BtsmtlSkillGraphReferenceAttribute reference in
                             BtsmtlSkillGraphAuthoringMetadata.GraphReferences(node.capability))
                    {
                        string child = node.properties?.Value<string>(reference.FieldId);
                        if (!string.IsNullOrEmpty(child))
                            result &= Visit(child, AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".nodes[" + node.id + "]");
                    }
                    foreach (JObject step in (node.properties?["steps"] as JArray)?.OfType<JObject>() ?? Enumerable.Empty<JObject>())
                        if (!string.IsNullOrEmpty(step.Value<string>("conditionGraphId")))
                            result &= Visit(
                                step.Value<string>("conditionGraphId"),
                                AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".nodes[" + node.id + "].properties.steps");
                    if (!BtsmtlSkillNodeAuthoringValidation.HasRule(
                            node.capability,
                            BtsmtlSkillNodeAuthoringRule.TimelineReference) ||
                        !timelines.TryGetValue(node.properties?.Value<string>("timelineId") ?? string.Empty, out AgentPackageSkillTimelineFile timeline))
                        continue;
                    foreach (AgentPackageSkillTimelineClip clip in (timeline.tracks ?? new List<AgentPackageSkillTimelineTrack>())
                                 .SelectMany(value => value?.clips ?? new List<AgentPackageSkillTimelineClip>()))
                        if (!string.IsNullOrEmpty(clip.treeGraphId))
                            result &= Visit(clip.treeGraphId, AgentSkillFlowDocumentMapper.TimelinePath(timeline.id) + ".tracks.clips[" + clip.id + "]");
                }
                foreach (AgentPackageSkillGraphAnchor anchor in graph.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                    foreach (AgentPackageSkillFlowStep step in anchor?.steps ?? new List<AgentPackageSkillFlowStep>())
                        if (!string.IsNullOrEmpty(step?.conditionGraphId))
                            result &= Visit(step.conditionGraphId, AgentSkillFlowDocumentMapper.GraphPath(graph.id) + ".anchors[" + anchor.kind + "].steps");
                states[graphId] = 2;
                return result;
            }

            foreach (AgentPackageSkillDefinitionFile skill in document.skills ?? new List<AgentPackageSkillDefinitionFile>())
                if (skill != null)
                    valid &= Visit(skill.entryGraphAuthoringId, "editable/skills[" + skill.skillId + "]");
            return valid;
        }

        static bool ValidateEndpoint(
            AgentPackageSkillFlowEdgeEndpoint endpoint,
            ISet<string> nodes,
            ISet<string> anchors,
            string path,
            AgentCompileReport report)
        {
            if (endpoint == null || string.IsNullOrWhiteSpace(endpoint.node) || string.IsNullOrWhiteSpace(endpoint.port) ||
                !nodes.Contains(endpoint.node) && !anchors.Contains(endpoint.node))
            {
                report.Error(path, "skill_edge_endpoint_invalid", "Skill Edge endpoint必须引用当前Graph node或anchor及逻辑port。");
                return false;
            }
            return true;
        }

        static bool ValidateEdgePortShapes(
            AgentPackageSkillFlowGraphFile graph,
            AgentPackageSkillFlowEdge edge,
            IReadOnlyList<AgentPackageSkillMacroFile> macros,
            ISet<(string Node, string Port)> connectedOutputs,
            ISet<(string Node, string Port)> connectedInputs,
            string path,
            AgentCompileReport report)
        {
            AgentPackagePortDescriptor from = ResolvePort(graph, edge.from, macros, false);
            AgentPackagePortDescriptor to = ResolvePort(graph, edge.to, macros, true);
            if (from == null || to == null ||
                !string.Equals(from.direction, "Output", StringComparison.Ordinal) ||
                !string.Equals(to.direction, "Input", StringComparison.Ordinal) ||
                edge.kind == "flow" && (!string.IsNullOrEmpty(from.valueType) || !string.IsNullOrEmpty(to.valueType)) ||
                edge.kind == "value" && (string.IsNullOrEmpty(from.valueType) || string.IsNullOrEmpty(to.valueType) ||
                                           !string.Equals(from.valueType, to.valueType, StringComparison.Ordinal)))
            {
                report.Error(path, "skill_edge_port_shape_invalid", "Skill Edge必须连接同一类型的合法输出与输入port。");
                return false;
            }
            bool outputAvailable = connectedOutputs.Add((edge.from.node, edge.from.port));
            bool inputAvailable = connectedInputs.Add((edge.to.node, edge.to.port));
            if (from.capacity == "Single" && !outputAvailable || to.capacity == "Single" && !inputAvailable)
            {
                report.Error(path, "skill_edge_port_capacity_exceeded", "单连接端口不能重复接线：Flow输出与值输入必须各自只有一条连接。");
                return false;
            }
            return true;
        }

        static AgentPackagePortDescriptor ResolvePort(
            AgentPackageSkillFlowGraphFile graph,
            AgentPackageSkillFlowEdgeEndpoint endpoint,
            IReadOnlyList<AgentPackageSkillMacroFile> macros,
            bool input)
        {
            AgentPackageSkillFlowNode node = (graph.nodes ?? new List<AgentPackageSkillFlowNode>())
                .FirstOrDefault(value => value != null && value.id == endpoint.node);
            if (node != null)
                return AgentSkillPackageProjection.ProjectPorts(node, graph, macros)
                    .FirstOrDefault(value => value.key == endpoint.port &&
                                             string.Equals(value.direction, input ? "Input" : "Output", StringComparison.Ordinal));
            AgentPackageSkillGraphAnchor anchor = (graph.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                .FirstOrDefault(value => value != null && value.kind == endpoint.node);
            if (anchor == null)
                return null;
            return AgentSkillPackageProjection
                .ProjectAnchorPorts(anchor, graph, macros)
                .FirstOrDefault(value =>
                    value.key == endpoint.port &&
                    string.Equals(
                        value.direction,
                        input ? "Input" : "Output",
                        StringComparison.Ordinal));
        }

        internal static bool IsIdentity(string value) => AgentPackageMappingSupport.IsIdentity(value);
        internal static bool LocalIdentity(string value) => IsIdentity(value) && value.StartsWith("local:", StringComparison.Ordinal);
        static bool IsAnchor(string value) =>
            BtsmtlSkillGraphAuthoringMetadata.IsAnchor(value);

        static bool Asset(AgentPackageObjectReference value)
        {
            return value != null && string.IsNullOrWhiteSpace(value.localId) &&
                   !string.IsNullOrWhiteSpace(value.assetPath) && value.assetPath.StartsWith("Assets/", StringComparison.Ordinal) &&
                   !value.assetPath.Contains("\\") && value.assetGuid?.Length == 32 &&
                   value.assetGuid.All(character => character >= '0' && character <= '9' || character >= 'a' && character <= 'f') &&
                   value.localFileId != 0;
        }

        static string AssetProviderOwner(UnityEngine.Object asset)
        {
            string path = asset ? AssetDatabase.GetAssetPath(asset) : string.Empty;
            return CharacterSkillProviderOwners.Asset(
                string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path));
        }
    }
}
