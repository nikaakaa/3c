#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using NodeCanvas.Framework;
using ThirdPersonCharacter.ActionSystem;
using UnityEditor;

namespace ThirdPersonCharacter.Control.Authoring
{
    public static class BtsmtlSkillGraphClosure
    {
        public static IReadOnlyList<FlowGraph> Validate(FlowGraph root, bool requireComplete)
        {
            RequireAbilityRootOwnership(root);
            var result = new List<FlowGraph>();
            var active = new HashSet<FlowGraph>();
            var identities = new Dictionary<string, FlowGraph>(StringComparer.Ordinal);
            Visit(root, "skill", requireComplete, active, identities, result);
            return result.AsReadOnly();
        }

        public static IReadOnlyList<FlowGraph> ValidateTimelineGraph(FlowGraph root, TimelineExecutionDomain domain)
        {
            TimelineExecutionDomainMask mask = TimelineExecutionDomains.ToMask(domain);
            IReadOnlyList<FlowGraph> graphs = Validate(root, true);
            foreach (FlowGraph graph in graphs)
                foreach (FlowNode node in graph.allNodes.Cast<FlowNode>())
                    if ((BtsmtlSkillCapabilityCatalog.TimelineDomains(node.GetType()) & mask) == 0)
                        throw Error($"graph:{((IBtsmtlSkillFlowGraph)graph).AuthoringId}/node:{node.UID}",
                            $"节点 '{node.GetType().Name}' 不支持轨道执行域 {domain}。");
            return graphs;
        }

        public static IReadOnlyList<FlowGraph> Validate(TimelineAsset root, bool requireComplete)
        {
            if (root == null || root.Data == null)
                throw Error("timeline", "Timeline缺少正式作者内容。");
            var result = new List<FlowGraph>();
            VisitTimeline(root, "timeline", requireComplete, new HashSet<FlowGraph>(),
                new Dictionary<string, FlowGraph>(StringComparer.Ordinal), result);
            return result.AsReadOnly();
        }

        static void VisitTimeline(TimelineAsset timeline, string path, bool complete,
            HashSet<FlowGraph> active, Dictionary<string, FlowGraph> identities, List<FlowGraph> result)
        {
            foreach (Track track in timeline.Data.Tracks)
            {
                foreach (Clip clip in track.Clips)
                {
                    if (clip is not TreeClip tree)
                        continue;
                    string clipPath = $"{path}/clip:{clip.AuthoringId}";
                    if (tree.AssetTree is not BtsmtlSkillFlowGraph child)
                        throw Error(clipPath, "技能TreeClip必须引用正式原生节点图。");
                    RequirePrivateOwnership(timeline, child, clipPath);
                    VisitChild(child, BtsmtlSkillFlowGraphRole.TimelineBody, clipPath,
                        complete, active, identities, result);
                }
                foreach (TimelineMarker marker in track.Markers)
                {
                    string markerPath = $"{path}/marker:{marker.AuthoringId}";
                    if (marker.Graph is not BtsmtlSkillFlowGraph trigger)
                        throw Error(markerPath, "Marker必须引用正式TimelineTrigger图。");
                    RequirePrivateOwnership(timeline, trigger, markerPath);
                    VisitChild(trigger, BtsmtlSkillFlowGraphRole.TimelineTrigger, markerPath,
                        complete, active, identities, result);
                }
            }
        }

        static void RequireAbilityRootOwnership(FlowGraph root)
        {
            if (root is not BtsmtlSkillFlowGraph graph || !AssetDatabase.IsSubAsset(graph))
                return;
            string path = AssetDatabase.GetAssetPath(graph);
            UnityEngine.Object mainAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (mainAsset is GameplayAbilityDefinition ability &&
                (ability.AbilityGraph == null ||
                 !string.Equals(AssetDatabase.GetAssetPath(ability.AbilityGraph), path, StringComparison.Ordinal)))
                throw Error("skill", "Gameplay Ability私有图不属于其Definition拥有的AbilityGraph资产闭包。");
        }

        static void Visit(FlowGraph graph, string path, bool complete, HashSet<FlowGraph> active,
            Dictionary<string, FlowGraph> identities, List<FlowGraph> result)
        {
            if (graph is not IBtsmtlSkillFlowGraph authoring)
                throw Error(path, "引用目标不是技能图。");
            if (!active.Add(graph))
                throw Error(path, "子图或条件调用形成递归。");
            if (string.IsNullOrWhiteSpace(authoring.AuthoringId))
                throw Error(path, "图缺少稳定身份。");
            if (identities.TryGetValue(authoring.AuthoringId, out FlowGraph existing))
            {
                if (existing != graph)
                    throw Error(path, "不同图使用了相同身份。");
                active.Remove(graph);
                return;
            }
            identities.Add(authoring.AuthoringId, graph);
            result.Add(graph);
            BtsmtlSkillBlackboardDeclarations.Validate(graph, authoring.BlackboardDeclarations);
            if (graph is BtsmtlSkillMacroGraph signature)
                BtsmtlSkillMacroInterface.Validate(signature);
            ValidateTopology(graph, authoring.Role, path, complete);
            foreach (FlowNode node in graph.allNodes.Cast<FlowNode>())
            {
                string nodePath = $"{path}/graph:{authoring.AuthoringId}/node:{node.UID}";
                if (node is MacroNodeWrapper call)
                {
                    if (call.macro == null && !complete)
                        continue;
                    if (call.macro is not BtsmtlSkillMacroGraph macro)
                        throw Error(nodePath, "调用目标必须是正式技能 Macro。");
                    try
                    {
                        BtsmtlSkillMacroInterface.ValidateCall(call, macro, nodePath);
                    }
                    catch (InvalidOperationException exception)
                    {
                        throw Error(nodePath, exception.Message);
                    }
                    RequirePrivateOwnership(graph, macro, nodePath);
                    Visit(macro, nodePath, complete, active, identities, result);
                }
                if (node is BtsmtlSkillStateMachineFlowNode machine)
                {
                    RequirePrivateOwnership(graph, machine.StateMachine, nodePath);
                    ValidateNativeStateMachine(machine.StateMachine, nodePath, complete, active, identities, result);
                }
                if (node is BtsmtlSkillStateFlowNode state)
                {
                    RequirePrivateOwnership(graph, state.Body, nodePath);
                    VisitChild(state.Body, BtsmtlSkillFlowGraphRole.StateBody, nodePath, complete, active, identities, result);
                }
                if (node is BtsmtlSkillTimelineFlowNode timeline)
                {
                    if (timeline.TimelineAsset == null)
                    {
                        if (complete)
                            throw Error(nodePath, "缺少技能Timeline资产。");
                    }
                    else
                    {
                        string timelinePath = AssetDatabase.GetAssetPath(timeline.TimelineAsset);
                        string graphPath = AssetDatabase.GetAssetPath(graph);
                        if (timeline.Ownership == BtsmtlSkillTimelineOwnership.Private &&
                            (timelinePath != graphPath ||
                             AssetDatabase.LoadMainAssetAtPath(timelinePath) == timeline.TimelineAsset))
                            throw Error(nodePath, "私有Timeline必须是当前技能根中的子资产。");
                        if (timeline.Ownership == BtsmtlSkillTimelineOwnership.Shared && !AssetDatabase.IsMainAsset(timeline.TimelineAsset))
                            throw Error(nodePath, "共享Timeline必须是明确的独立资产。");
                        VisitTimeline(timeline.TimelineAsset, nodePath, complete, active, identities, result);
                    }
                }
                if (node is BtsmtlSkillCompositeFlowNode composite)
                    foreach (BtsmtlSkillStepPort step in composite.Steps)
                        if (step.Condition != null)
                        {
                            RequirePrivateOwnership(graph, step.Condition, nodePath);
                            VisitChild(step.Condition, BtsmtlSkillFlowGraphRole.ConditionRule, $"{nodePath}/port:{step.Id}", complete, active, identities, result);
                        }
                foreach (Connection outgoing in node.outConnections)
                    if (outgoing is BtsmtlSkillFlowConnection transfer && transfer.Condition != null)
                    {
                        RequirePrivateOwnership(graph, transfer.Condition, nodePath);
                        VisitChild(transfer.Condition, BtsmtlSkillFlowGraphRole.ConditionRule,
                            $"{nodePath}/edge:{transfer.UID}", complete, active, identities, result);
                    }
            }
            active.Remove(graph);
        }

        static void VisitChild(BtsmtlSkillFlowGraph child, BtsmtlSkillFlowGraphRole role, string path,
            bool complete, HashSet<FlowGraph> active, Dictionary<string, FlowGraph> identities, List<FlowGraph> result)
        {
            if (child == null && !complete)
                return;
            if (child == null || child.Role != role)
                throw Error(path, $"缺少 {role} 页面或引用页面类型错误。");
            Visit(child, path, complete, active, identities, result);
        }

        static void ValidateNativeStateMachine(
            BtsmtlSkillNativeStateMachine machine,
            string path,
            bool complete,
            HashSet<FlowGraph> active,
            Dictionary<string, FlowGraph> identities,
            List<FlowGraph> result)
        {
            if (machine == null)
            {
                if (complete)
                    throw Error(path, "缺少原生状态机资产。");
                return;
            }
            BtsmtlSkillNativeStateMachineContract.Validate(machine, complete);
            foreach (BtsmtlSkillNativeState state in machine.allNodes.OfType<BtsmtlSkillNativeState>())
            {
                string statePath = $"{path}/fsm:{machine.AuthoringId}/state:{state.UID}";
                if (state.Body != null)
                {
                    RequirePrivateOwnership(machine, state.Body, statePath);
                    VisitChild(state.Body, BtsmtlSkillFlowGraphRole.StateBody, statePath,
                        complete, active, identities, result);
                }
                foreach (BtsmtlSkillNativeConnection connection in state.outConnections.OfType<BtsmtlSkillNativeConnection>())
                    if (connection.Condition != null)
                    {
                        RequirePrivateOwnership(machine, connection.Condition, $"{statePath}/edge:{connection.UID}");
                        VisitChild(connection.Condition, BtsmtlSkillFlowGraphRole.ConditionRule,
                            $"{statePath}/edge:{connection.UID}", complete, active, identities, result);
                    }
            }
        }

        static void RequirePrivateOwnership(UnityEngine.Object owner, UnityEngine.Object child, string path)
        {
            if (child != null && AssetDatabase.IsSubAsset(child) && AssetDatabase.GetAssetPath(owner) != AssetDatabase.GetAssetPath(child))
                throw Error(path, "不能直接引用其他根的私有节点图，应使用明确的共享资产。");
        }

        static void ValidateTopology(FlowGraph graph, BtsmtlSkillFlowGraphRole role, string path, bool complete)
        {
            var nodes = new HashSet<string>(StringComparer.Ordinal);
            var edges = new HashSet<string>(StringComparer.Ordinal);
            var anchors = new HashSet<string>(StringComparer.Ordinal);
            var occupied = new HashSet<Port>();
            List<(BinderConnection edge, FlowNode node)> stateTransfers =
                role == BtsmtlSkillFlowGraphRole.StateMachine ? new List<(BinderConnection edge, FlowNode node)>() : null;
            foreach (Node value in graph.allNodes)
            {
                if (value is not FlowNode node)
                    throw Error(path, "页面包含未登记或不属于当前页面的节点。");
                if (!graph.CanAuthorNodeType(node.GetType()))
                    throw Error(path, "页面包含未登记或不属于当前页面的节点。");
                try
                {
                    node.GatherPorts();
                    BtsmtlSkillCapabilityCatalog.ProjectPorts(node);
                }
                catch (InvalidOperationException exception)
                {
                    throw Error(path + "/node:" + value.UID, exception.Message);
                }
            }
            foreach (Node value in graph.allNodes)
            {
                FlowNode node = (FlowNode)value;
                if (!nodes.Add(node.UID))
                    throw Error(path, "节点身份重复。");
                bool anchor = node is IBtsmtlSkillSystemNode || node is MacroInputNode || node is MacroOutputNode;
                if (anchor &&
                    (!BtsmtlSkillGraphAuthoringMetadata.TryGetKind(
                        node.GetType(),
                        out string anchorKind) ||
                     !anchors.Add(anchorKind)))
                    throw Error(path, "系统入口重复。");
                if (node is BtsmtlSkillCompositeFlowNode composite)
                {
                    var ports = new HashSet<string>(StringComparer.Ordinal);
                    foreach (BtsmtlSkillStepPort step in composite.Steps)
                        if (step == null || string.IsNullOrWhiteSpace(step.Id) || !ports.Add(step.Id))
                            throw Error($"{path}/node:{node.UID}", "步骤端口身份缺失或重复。");
                }
                foreach (Connection valueEdge in node.outConnections)
                {
                    if (valueEdge is not BinderConnection edge || !edges.Add(edge.UID) ||
                        edge.sourceNode != node || !graph.allNodes.Contains(edge.targetNode) ||
                        !edge.targetNode.inConnections.Contains(edge))
                        throw Error(path, "连线身份或端点归属无效。");
                    Port source = edge.sourcePort;
                    Port target = edge.targetPort;
                    if (source == null || target == null || source.type != target.type ||
                        source.IsFlowPort() != target.IsFlowPort())
                        throw Error($"{path}/edge:{edge.UID}", "端口缺失或声明类型不一致。");
                    bool stateTransferOutput = role == BtsmtlSkillFlowGraphRole.StateMachine &&
                        node is IBtsmtlSkillStateStructureNode &&
                        source is FlowOutput &&
                        string.Equals(source.ID, "Transfer", StringComparison.Ordinal);
                    if ((!stateTransferOutput && source is FlowOutput && !occupied.Add(source)) ||
                        (target is ValueInput && !occupied.Add(target)))
                        throw Error($"{path}/edge:{edge.UID}", "端口超过原生连接容量。");
                    stateTransfers?.Add((edge, node));
                }
            }
            if (stateTransfers != null)
            {
                var transfers = stateTransfers.Where(value => value.edge is BtsmtlSkillFlowConnection).ToList();
                if (transfers.Count != stateTransfers.Count)
                    throw Error(path, "状态机图存在不携带Transfer数据的转移连线。");
                var orders = new HashSet<string>(StringComparer.Ordinal);
                foreach (var (edge, node) in stateTransfers)
                {
                    var transfer = (BtsmtlSkillFlowConnection)edge;
                    if (edge.sourcePort is not FlowOutput ||
                        !string.Equals(edge.sourcePortID, "Transfer", StringComparison.Ordinal) ||
                        edge.targetPort is not FlowInput ||
                        !string.Equals(edge.targetPortID, "StateIn", StringComparison.Ordinal))
                        throw Error($"{path}/edge:{edge.UID}", "状态机转移必须连接Transfer到StateIn。");
                    if (!orders.Add(node.UID + "\0" + transfer.Order))
                        throw Error($"{path}/node:{node.UID}", "同一状态的转移order必须唯一。");
                    if (transfer.Priority < BtsmtlSkillFlowConnection.MinPriority)
                        throw Error($"{path}/edge:{edge.UID}", "转移优先级不能为负。");
                    if (node is BtsmtlSkillStateAnyFlowNode && transfer.Condition == null)
                        throw Error($"{path}/edge:{edge.UID}", "任意状态的转移必须挂条件图。");
                }
            }
            var visiting = new HashSet<Node>();
            var visited = new HashSet<Node>();
            if (role != BtsmtlSkillFlowGraphRole.StateMachine)
                foreach (Node node in graph.allNodes)
                    VisitEdges(node, path, visiting, visited);
            if (!complete)
                return;
            foreach (string required in BtsmtlSkillGraphAuthoringMetadata.RequiredAnchors(role))
                if (!anchors.Contains(required))
                    throw Error(path, $"页面缺少 {required} 系统入口。");
        }

        static void VisitEdges(Node node, string path, HashSet<Node> active, HashSet<Node> visited)
        {
            if (visited.Contains(node))
                return;
            if (!active.Add(node))
                throw Error($"{path}/node:{node.UID}", "连线形成环；重复执行应使用循环节点。");
            foreach (Connection edge in node.outConnections)
                VisitEdges(edge.targetNode, path, active, visited);
            active.Remove(node);
            visited.Add(node);
        }

        static InvalidOperationException Error(string path, string message) => new($"{path}: {message}");
    }
}
#endif
