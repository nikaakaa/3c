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
using ThirdPersonGameplay.Effects;
using ThirdPersonSimulation;
using TreeDesigner;
using UnityEditor;
using UnityEngine;

using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Editor.Authoring.Presentation;

namespace ThirdPersonCharacter.Pipeline.Editor.Authoring.Skill
{
    internal sealed class BtsmtlSkillGraphAuthoringApplier : IBtsmtlSkillNodeAuthoringResolver
    {
        readonly AgentMutationSession m_Session;
        readonly AgentPackageSkillFlowDocument m_Document;
        readonly Func<string, TimelineAsset> m_ResolveTimeline;
        readonly Dictionary<string, FlowGraph> m_Graphs =
            new Dictionary<string, FlowGraph>(StringComparer.Ordinal);
        readonly Dictionary<string, FlowGraph> m_LocalGraphs =
            new Dictionary<string, FlowGraph>(StringComparer.Ordinal);
        readonly Dictionary<string, FlowNode> m_LocalNodes =
            new Dictionary<string, FlowNode>(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_LocalDeclarations =
            new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_LocalPorts =
            new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, BinderConnection> m_LocalEdges =
            new Dictionary<string, BinderConnection>(StringComparer.Ordinal);
        readonly Dictionary<string, string> m_LocalParameters =
            new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, AgentPackageSkillFlowGraphFile> m_TargetGraphs =
            new Dictionary<string, AgentPackageSkillFlowGraphFile>(StringComparer.Ordinal);
        readonly Dictionary<string, AgentPackageSkillFlowGraphLayoutFile> m_TargetLayouts =
            new Dictionary<string, AgentPackageSkillFlowGraphLayoutFile>(StringComparer.Ordinal);
        readonly BtsmtlSkillGraphClosureIndex m_Current = new BtsmtlSkillGraphClosureIndex();

        public BtsmtlSkillGraphAuthoringApplier(
            AgentMutationSession session,
            AgentPackageSkillFlowDocument document,
            Func<string, TimelineAsset> resolveTimeline)
        {
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_Document = document ?? throw new ArgumentNullException(nameof(document));
            m_ResolveTimeline = resolveTimeline ?? throw new ArgumentNullException(nameof(resolveTimeline));
        }

        public BtsmtlSkillGraphClosureIndex Current => m_Current;

        public void Resolve()
        {
            m_Current.Build(m_Session.Definition);
            m_Graphs.Clear();
            m_LocalGraphs.Clear();
            m_LocalNodes.Clear();
            m_LocalDeclarations.Clear();
            m_LocalPorts.Clear();
            m_LocalEdges.Clear();
            m_LocalParameters.Clear();
            m_TargetGraphs.Clear();
            m_TargetLayouts.Clear();
            foreach (AgentPackageSkillFlowGraphFile graph in m_Document.graphs ?? new List<AgentPackageSkillFlowGraphFile>())
                m_TargetGraphs.Add(graph.id, graph);
            foreach (AgentPackageSkillFlowGraphLayoutFile layout in m_Document.layouts ?? new List<AgentPackageSkillFlowGraphLayoutFile>())
                m_TargetLayouts.Add(layout.graphId, layout);
            ResolveGraphs();
        }

        public void Sync()
        {
            SyncBlackboards();
            SyncMacros();
            SyncNodes();
        }

        public void DeleteRemoved()
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

        void SyncBlackboards()
        {
            foreach (AgentPackageSkillFlowGraphFile target in m_Document.graphs)
            {
                FlowGraph graph = ResolveGraph(target.id);
                if (graph is not IBtsmtlSkillFlowGraph skill)
                    continue;
                BtsmtlSkillFlowEditorMutation.Apply(
                    graph,
                    "应用技能黑板",
                    () => SyncBlackboard(graph, skill, target),
                    false,
                    Array.Empty<UnityEngine.Object>(),
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
                if (!targetIds.Contains(variables[key]?.ID ?? string.Empty))
                    variables.Remove(key);
            var metadata = new List<BtsmtlSkillBlackboardDeclaration>();
            foreach (AgentPackageSkillBlackboardDeclaration declaration in declarations)
            {
                if (!AgentSkillPackageProjection.TryResolveValueType(declaration.valueType, out Type type))
                    throw new InvalidOperationException($"Skill Blackboard '{declaration.id}'类型无法解析。");
                Variable variable = variables.Values.SingleOrDefault(value => value != null && value.ID == declaration.id);
                if (variable != null && variable.varType != type)
                    throw new InvalidOperationException($"Skill Blackboard '{declaration.id}'不能原位改变类型。");
                AgentPackageSkillBlackboardInputBinding inputBinding = declaration.inputBinding;
                if (inputBinding != null && string.IsNullOrWhiteSpace(inputBinding.inputValueId))
                    inputBinding = null;
                AgentPackageSkillBlackboardFactProjection factProjection = declaration.factProjection;
                if (factProjection != null &&
                    (string.IsNullOrWhiteSpace(factProjection.kind) ||
                     string.IsNullOrWhiteSpace(factProjection.windowType) ||
                     string.IsNullOrWhiteSpace(factProjection.windowId)))
                    factProjection = null;
                string variableId = variable?.ID;
                if (variable == null)
                {
                    if (!declaration.id.StartsWith("local:", StringComparison.Ordinal))
                        throw new InvalidOperationException($"Skill Blackboard '{declaration.id}'无法创建新的formal identity。");
                    variableId = Guid.NewGuid().ToString("N");
                }
                var metadataDeclaration = new BtsmtlSkillBlackboardDeclaration(
                    variableId,
                    Enum.Parse<PipelineBlackboardVariableScope>(declaration.scope, false),
                    Enum.Parse<PipelineBlackboardVariableLifetime>(declaration.lifetime, false),
                    declaration.category,
                    inputBinding == null ? null : new PipelineBlackboardInputBinding(inputBinding.inputValueId),
                    factProjection == null ? null : new PipelineBlackboardFactProjection(
                        Enum.Parse<PipelineBlackboardFactProjectionKind>(factProjection.kind, false),
                        factProjection.windowType,
                        factProjection.windowId,
                        factProjection.digest));
                object defaultValue = BtsmtlSkillNodeAuthoringBinding.ParseValue(
                    declaration.defaultValue,
                    type);
                if (variable == null)
                {
                    variable = BtsmtlSkillBlackboardDeclarations.CreateVariable(
                        metadataDeclaration,
                        declaration.key,
                        type,
                        defaultValue);
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
                if (variable.ID != metadataDeclaration.VariableId)
                    throw new InvalidOperationException($"Skill Blackboard '{declaration.id}'稳定identity无法保持。");
                metadataDeclaration.Validate(variable);
                variable.SetValueBoxed(defaultValue);
                metadata.Add(metadataDeclaration);
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
                BtsmtlSkillFlowEditorMutation.Apply(
                    macro,
                    "应用技能Macro接口",
                    () => SyncMacro(macro, target),
                    false,
                    Array.Empty<UnityEngine.Object>(),
                    false);
            }
        }

        void SyncMacro(BtsmtlSkillMacroGraph macro, AgentPackageSkillMacroFile target)
        {
            var existingInputIds = macro.inputDefinitions.Select(value => value.ID).ToHashSet(StringComparer.Ordinal);
            var existingOutputIds = macro.outputDefinitions.Select(value => value.ID).ToHashSet(StringComparer.Ordinal);
            var inputs = new List<DynamicParameterDefinition>();
            foreach (AgentPackageSkillMacroParameter parameter in target.inputs ?? new List<AgentPackageSkillMacroParameter>())
            {
                if (!parameter.id.StartsWith("local:", StringComparison.Ordinal) && !existingInputIds.Contains(parameter.id))
                    throw new InvalidOperationException($"Skill Macro input '{parameter.id}'无法创建新的formal identity。");
                string inputId = parameter.id.StartsWith("local:", StringComparison.Ordinal)
                    ? Guid.NewGuid().ToString("N")
                    : parameter.id;
                inputs.Add(new DynamicParameterDefinition(
                    inputId,
                    parameter.name,
                    ResolveMacroType(parameter.valueType)));
                if (parameter.id.StartsWith("local:", StringComparison.Ordinal))
                    m_LocalParameters[ParameterKey(target.graphId, parameter.id)] = inputId;
            }
            var outputs = new List<DynamicParameterDefinition>();
            foreach (AgentPackageSkillMacroParameter parameter in target.outputs ?? new List<AgentPackageSkillMacroParameter>())
            {
                if (!parameter.id.StartsWith("local:", StringComparison.Ordinal) && !existingOutputIds.Contains(parameter.id))
                    throw new InvalidOperationException($"Skill Macro output '{parameter.id}'无法创建新的formal identity。");
                string outputId = parameter.id.StartsWith("local:", StringComparison.Ordinal)
                    ? Guid.NewGuid().ToString("N")
                    : parameter.id;
                outputs.Add(new DynamicParameterDefinition(
                    outputId,
                    parameter.name,
                    ResolveMacroType(parameter.valueType)));
                if (parameter.id.StartsWith("local:", StringComparison.Ordinal))
                    m_LocalParameters[ParameterKey(target.graphId, parameter.id)] = outputId;
            }
            BtsmtlSkillMacroInterface.Configure(macro, inputs, outputs);
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
                BtsmtlSkillFlowEditorMutation.Apply(
                    graph,
                    "应用技能Graph",
                    () => SyncGraph(graph, target),
                    false,
                    Array.Empty<UnityEngine.Object>(),
                    false);
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
                        AgentSkillPackageProjection.TryGetKind(value, out string kind) && kind == anchor.kind);
                if (anchorNode != null && !anchor.nodeId.StartsWith("local:", StringComparison.Ordinal) && anchorNode.UID != anchor.nodeId)
                    throw new InvalidOperationException($"Skill Graph anchor '{anchor.nodeId}'的identity与正式节点不一致。");
                if (anchorNode != null && anchor.nodeId.StartsWith("local:", StringComparison.Ordinal))
                    m_LocalNodes[NodeKey(target.id, anchor.nodeId)] = anchorNode;
            }
            var targetNodeIds = (target.nodes ?? new List<AgentPackageSkillFlowNode>()).Select(value => value.id).ToHashSet(StringComparer.Ordinal);
            foreach (FlowNode existing in graph.allNodes.OfType<FlowNode>().ToArray())
            {
                if (AgentSkillPackageProjection.TryGetKind(existing, out string kind) && AgentSkillPackageProjection.IsAnchor(kind))
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
                    actual = (FlowNode)graph.AddNode(ResolveNodeType(node), Vector2.zero);
                    m_LocalNodes[NodeKey(target.id, node.id)] = actual;
                }
                else if (NodeKind(actual) != node.capability ||
                         ResolveNodeType(node) != actual.GetType())
                    throw new InvalidOperationException($"Skill Node '{node.id}'不能原位改变Capability或accessMode。");
                ConfigureNode(graph, target, node, actual);
            }
            SyncEdges(target, graph);
        }

        static Type ResolveNodeType(AgentPackageSkillFlowNode node)
        {
            if (BtsmtlSkillGraphAuthoringMetadata.TryResolveType(
                    node.capability,
                    "accessMode",
                    node.properties?.Value<string>("accessMode"),
                    out Type type))
                return type;
            throw new InvalidOperationException($"Skill Node kind无法解析：{node.capability}");
        }

        static string NodeKind(FlowNode node) =>
            AgentSkillPackageProjection.TryGetKind(node, out string kind) ? kind : string.Empty;

        void ConfigureNode(
            FlowGraph graph,
            AgentPackageSkillFlowGraphFile graphFile,
            AgentPackageSkillFlowNode target,
            FlowNode node)
        {
            if (target.name != null && !string.Equals(node.name, target.name, StringComparison.Ordinal))
                node.name = target.name;
            JObject properties = target.properties ?? new JObject();
            BtsmtlSkillNodeAuthoringBinding.Apply(node, target.properties, this);
            if (node is BtsmtlSkillCompositeFlowNode composite)
                ConfigureSteps(graphFile.id, target.id, composite, properties["steps"] as JArray);
            node.position = FindPosition(graphFile.id, target.id);
            node.GatherPorts();
            BtsmtlSkillNodeAuthoringBinding.ApplyValues(node, target.values);
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
                steps.Add(new BtsmtlSkillStepPort(stepId, value.Value<string>("name") ?? string.Empty));
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
                    ApplyTransfer(created, edge, target.id);
                    m_LocalEdges[EdgeKey(target.id, edge.id)] = created;
                    continue;
                }
                if (existing is not BtsmtlSkillFlowConnection &&
                    graph is IBtsmtlSkillFlowGraph { Role: BtsmtlSkillFlowGraphRole.StateMachine } &&
                    source is FlowOutput && destination is FlowInput)
                {
                    string preservedUid = existing.UID;
                    graph.RemoveConnection(existing, false);
                    existing = graph.CreatePortConnection(source, destination)
                        ?? throw new InvalidOperationException($"Skill Edge '{edge.id}'迁移重建失败。");
                    existing.ConfigureAuthoringIdentity(preservedUid);
                }
                if (existing.sourcePort != source)
                    existing.SetSourcePort(source);
                if (existing.targetPort != destination)
                    existing.SetTargetPort(destination);
                ApplyTransfer(existing, edge, target.id);
            }
        }

        void ApplyTransfer(BinderConnection connection, AgentPackageSkillFlowEdge edge, string graphId)
        {
            if (connection is not BtsmtlSkillFlowConnection transfer)
            {
                if (!string.IsNullOrEmpty(edge.conditionGraphId))
                    throw new InvalidOperationException($"Skill Edge '{edge.id}'声明了转移条件，但连线未携带转移数据。");
                return;
            }
            BtsmtlSkillFlowGraph condition = null;
            if (!string.IsNullOrEmpty(edge.conditionGraphId))
                condition = ResolveGraph(edge.conditionGraphId) as BtsmtlSkillFlowGraph
                    ?? throw new InvalidOperationException($"Skill Edge '{edge.id}'的条件图'{edge.conditionGraphId}'未应用、顺序错误或不是条件规则图。");
            transfer.Configure(condition, edge.priority,
                string.IsNullOrEmpty(edge.abortPolicy)
                    ? ProgramAbortPolicy.None
                    : Enum.Parse<ProgramAbortPolicy>(edge.abortPolicy, false),
                edge.order);
        }

        public void ValidateAppliedIdentityContracts()
        {
            foreach (AgentPackageSkillFlowGraphFile target in m_Document.graphs)
            {
                FlowGraph graph = ResolveGraph(target.id);
                if (graph == null)
                    throw new InvalidOperationException($"Skill Graph '{target.id}'应用后无法回读。");
                foreach (AgentPackageSkillGraphAnchor anchor in target.anchors ?? new List<AgentPackageSkillGraphAnchor>())
                    if (ResolveNode(target.id, anchor.nodeId) == null)
                        throw new InvalidOperationException($"Skill Graph '{target.id}' anchor '{anchor.kind}'的Node identity应用后无法回读。");
                foreach (AgentPackageSkillFlowNode targetNode in target.nodes ?? new List<AgentPackageSkillFlowNode>())
                {
                    FlowNode node = ResolveNode(target.id, targetNode.id);
                    if (node == null || NodeKind(node) != targetNode.capability)
                        throw new InvalidOperationException($"Skill Graph '{target.id}' Node '{targetNode.id}'的identity或Capability应用后无法回读。");
                }
                foreach (AgentPackageSkillFlowEdge targetEdge in target.edges ?? new List<AgentPackageSkillFlowEdge>())
                {
                    BinderConnection edge = ResolveEdge(target.id, graph, targetEdge.id);
                    if (edge == null)
                        throw new InvalidOperationException($"Skill Graph '{target.id}' Edge '{targetEdge.id}'的identity应用后无法回读。");
                    Port source = ResolvePort(graph, target.id, targetEdge.from, false);
                    Port destination = ResolvePort(graph, target.id, targetEdge.to, true);
                    if (source == null || destination == null || edge.sourcePort != source || edge.targetPort != destination)
                        throw new InvalidOperationException($"Skill Graph '{target.id}' Edge '{targetEdge.id}'的Port identity应用后无法回读。");
                }
            }
        }

        BinderConnection ResolveEdge(string graphId, FlowGraph graph, string identity)
        {
            if (identity.StartsWith("local:", StringComparison.Ordinal) &&
                m_LocalEdges.TryGetValue(EdgeKey(graphId, identity), out BinderConnection local))
                return local;
            return graph.allNodes.OfType<FlowNode>()
                .SelectMany(value => value.outConnections.OfType<BinderConnection>())
                .FirstOrDefault(value => value.UID == identity);
        }

        FlowGraph CreateGraph(AgentPackageSkillFlowGraphFile target)
        {
            if (!target.id.StartsWith("local:", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Skill Graph '{target.id}'只能由local identity创建。");
            FlowGraph graph;
            if (target.role == BtsmtlSkillFlowGraphRole.Subgraph.ToString())
            {
                var macro = ScriptableObject.CreateInstance<BtsmtlSkillMacroGraph>();
                BtsmtlSkillMacroInterface.Initialize(macro);
                macro.ConfigureIdentity(
                    BtsmtlSkillGraphAssetFactory.StableIdentity(target.id));
                graph = macro;
            }
            else
            {
                var skill = ScriptableObject.CreateInstance<BtsmtlSkillFlowGraph>();
                skill.ConfigureIdentity(
                    BtsmtlSkillGraphAssetFactory.StableIdentity(target.id),
                    Enum.Parse<BtsmtlSkillFlowGraphRole>(target.role, false));
                graph = skill;
            }
            graph.name = string.IsNullOrWhiteSpace(target.name) ? "Skill Graph" : target.name;
            if (target.ownership == AgentGraphOwnership.RootAsset.ToString())
            {
                string assetPath = AgentSkillFlowAssetPaths.Root(
                    m_Session.Definition,
                    target.id);
                if (string.IsNullOrEmpty(assetPath))
                    throw new InvalidOperationException($"Skill Graph '{target.id}'缺少根资产路径。");
                AgentSkillFlowAssetPaths.RequireAvailable(assetPath);
                AssetDatabase.CreateAsset(graph, assetPath);
            }
            else if (target.ownership == AgentGraphOwnership.SharedAsset.ToString())
            {
                string assetPath = SharedGraphAssetPath(target.id);
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
            BtsmtlSkillFlowEditorMutation.Apply(
                graph,
                "初始化技能Graph",
                () => BtsmtlSkillGraphAssetFactory.PopulateAnchors(graph),
                false,
                Array.Empty<UnityEngine.Object>(),
                false);
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

        public FlowGraph ResolveGraph(string identity)
        {
            if (string.IsNullOrEmpty(identity))
                return null;
            if (m_Graphs.TryGetValue(identity, out FlowGraph graph))
                return graph;
            if (m_LocalGraphs.TryGetValue(identity, out graph))
                return graph;
            return m_Current.Graphs.TryGetValue(identity, out graph) ? graph : null;
        }

        public TimelineAsset ResolveTimeline(string identity) =>
            m_ResolveTimeline(identity);

        public ActionContextSlot ResolveActionContext(JToken token)
        {
            if (token is not JObject value)
                return null;
            string id = value.Value<string>("id");
            if (string.IsNullOrWhiteSpace(id))
                return null;
            AgentPackageObjectReference reference = value["asset"]?.ToObject<AgentPackageObjectReference>();
            return m_Session.Resolver.TryResolveActionContext(
                       new AgentAssetReference(id, reference?.assetPath, reference?.assetGuid),
                       out ActionContextSlot context)
                ? context
                : throw new InvalidOperationException($"ActionContext无法解析：{id}");
        }

        public ActionProfile ResolveActionProfile(JToken token)
        {
            if (token is not JObject value ||
                !m_Session.Resolver.TryResolveActionProfile(value.Value<string>("id"), out ActionProfile profile))
                throw new InvalidOperationException("ActionProfile无法解析。");
            return profile;
        }

        public string RequiredProviderOwner(JObject properties)
        {
            string value = properties.Value<string>("providerOwnerId");
            return string.IsNullOrWhiteSpace(value)
                ? throw new InvalidOperationException("Skill Provider引用缺少providerOwnerId。")
                : value;
        }

        public GameplayEffectDefinition ResolveGameplayEffect(JToken token)
        {
            if (token is not JObject value)
                throw new InvalidOperationException("Gameplay Effect引用缺失。");
            string id = value.Value<string>("id");
            GameplayEffectDefinition effect = m_Session.Definition.GameplayEffectProfile?.EffectDefinitions
                .FirstOrDefault(candidate => candidate && candidate.EffectId.Value == id);
            return effect ? effect : throw new InvalidOperationException($"Gameplay Effect无法解析：{id}");
        }

        public BtsmtlSkillBlackboardReference ResolveBlackboardReference(JObject properties)
        {
            string declarationId = ResolveDeclarationIdentity(properties.Value<string>("declarationId"));
            string ownerId = properties.Value<string>("ownerId");
            if (ResolveGraph(ownerId) is IBtsmtlSkillFlowGraph owner)
                ownerId = owner.AuthoringId;
            return new BtsmtlSkillBlackboardReference(declarationId, ownerId);
        }

        public string ResolveDeclarationIdentity(string identity) =>
            m_LocalDeclarations.TryGetValue(identity ?? string.Empty, out string resolved)
                ? resolved
                : identity;

        public string ResolveCallSite(string value)
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

        public string StableSkillId(string value, ISet<string> used)
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

        Vector2 FindPosition(string graphId, string nodeId)
        {
            AgentPackageSkillFlowGraphLayoutFile layout = m_TargetLayouts.TryGetValue(graphId, out AgentPackageSkillFlowGraphLayoutFile value)
                ? value
                : null;
            AgentPackageSkillFlowNodeLayout position = layout?.nodes?.FirstOrDefault(item => item != null && item.id == nodeId);
            return position == null ? Vector2.zero : new Vector2(position.x, position.y);
        }

        static string NodeKey(string graphId, string nodeId) => graphId + "\0" + nodeId;
        static string EdgeKey(string graphId, string edgeId) => graphId + "\0" + edgeId;
        static string ParameterKey(string graphId, string parameterId) => graphId + "\0" + parameterId;
        static string StepKey(string graphId, string nodeId, string portId) => graphId + "\0" + nodeId + "\0" + portId;

        static Type ResolveMacroType(string value) =>
            AgentSkillPackageProjection.TryResolveValueType(value, out Type type)
                ? type
                : throw new InvalidOperationException($"Macro parameter value type无法解析：{value}");

        static void RemoveConnections(FlowGraph graph, FlowNode node)
        {
            foreach (BinderConnection connection in node.inConnections.OfType<BinderConnection>()
                         .Concat(node.outConnections.OfType<BinderConnection>())
                         .Distinct()
                         .ToArray())
                graph.RemoveConnection(connection, false);
        }

        static void RemoveConnectionsForRemovedSteps(
            BtsmtlSkillCompositeFlowNode node,
            IReadOnlyList<BtsmtlSkillStepPort> steps)
        {
            var ids = steps.Select(value => value.Id).ToHashSet(StringComparer.Ordinal);
            foreach (BinderConnection connection in node.outConnections.OfType<BinderConnection>().ToArray())
                if (!ids.Contains(connection.sourcePortID))
                    ((FlowGraph)node.graph).RemoveConnection(connection, false);
        }

        Port ResolvePort(
            FlowGraph graph,
            string graphId,
            AgentPackageSkillFlowEdgeEndpoint endpoint,
            bool input)
        {
            FlowNode node = ResolveNode(graphId, endpoint.node);
            if (node == null && AgentSkillPackageProjection.IsAnchor(endpoint.node))
                node = graph.allNodes.OfType<FlowNode>().FirstOrDefault(value =>
                    AgentSkillPackageProjection.TryGetKind(value, out string kind) && kind == endpoint.node);
            if (node == null)
                return null;
            string portId = endpoint.port;
            if (node is BtsmtlSkillCompositeFlowNode &&
                m_LocalPorts.TryGetValue(
                    StepKey(
                        graphId,
                        AgentSkillPackageProjection.IsAnchor(endpoint.node) ? node.UID : endpoint.node,
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

        public T ResolveAsset<T>(JToken token) where T : UnityEngine.Object =>
            ResolveObject<T>(token?.ToObject<AgentPackageObjectReference>());

        T ResolveObject<T>(JToken token) where T : UnityEngine.Object =>
            ResolveObject<T>(token?.ToObject<AgentPackageObjectReference>());

        T ResolveObject<T>(AgentPackageObjectReference reference) where T : UnityEngine.Object
        {
            return m_Session.Resolver.TryResolveSkillObject(reference, out T asset)
                ? asset
                : null;
        }
    }
}
