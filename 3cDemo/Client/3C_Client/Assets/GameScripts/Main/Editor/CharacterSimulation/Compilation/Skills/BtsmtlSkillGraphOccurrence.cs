using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using FlowCanvas;
using FlowCanvas.Macros;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonSimulation;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public enum BtsmtlSkillGraphReferenceKind
    {
        Macro,
        StateMachine,
        StateBody
    }

    public sealed class BtsmtlSkillNativeStateOccurrence
    {
        internal BtsmtlSkillNativeStateOccurrence(
            BtsmtlSkillNativeState state,
            string route,
            BtsmtlSkillGraphOccurrence body)
        {
            State = state;
            Route = route;
            Body = body;
        }

        public BtsmtlSkillNativeState State { get; }
        public string Route { get; }
        public BtsmtlSkillGraphOccurrence Body { get; }
    }

    public sealed class BtsmtlSkillNativeEdgeOccurrence
    {
        internal BtsmtlSkillNativeEdgeOccurrence(
            BtsmtlSkillNativeConnection edge,
            string route,
            BtsmtlSkillNativeState source,
            BtsmtlSkillNativeState target,
            int order,
            int priority,
            ProgramAbortPolicy abortPolicy,
            BtsmtlSkillGraphOccurrence condition)
        {
            Edge = edge;
            Route = route;
            Source = source;
            Target = target;
            Order = order;
            Priority = priority;
            AbortPolicy = abortPolicy;
            Condition = condition;
        }

        public BtsmtlSkillNativeConnection Edge { get; }
        public string Route { get; }
        public BtsmtlSkillNativeState Source { get; }
        public BtsmtlSkillNativeState Target { get; }
        public int Order { get; }
        public int Priority { get; }
        public ProgramAbortPolicy AbortPolicy { get; }
        public BtsmtlSkillGraphOccurrence Condition { get; }
    }

    public sealed class BtsmtlSkillNativeStateMachineOccurrence
    {
        BtsmtlSkillNativeStateMachineOccurrence(
            BtsmtlSkillNativeStateMachine machine,
            string route,
            string contentHash,
            BtsmtlSkillNativeState entry,
            BtsmtlSkillNativeState any,
            BtsmtlSkillNativeState exit,
            IEnumerable<BtsmtlSkillNativeStateOccurrence> states,
            IEnumerable<BtsmtlSkillNativeEdgeOccurrence> edges)
        {
            Machine = machine;
            Route = route;
            ContentHash = contentHash;
            Entry = entry;
            Any = any;
            Exit = exit;
            States = Array.AsReadOnly(states.ToArray());
            Edges = Array.AsReadOnly(edges.ToArray());
        }

        public BtsmtlSkillNativeStateMachine Machine { get; }
        public string GraphId => Machine.AuthoringId;
        public string Route { get; }
        public string ContentHash { get; }
        public BtsmtlSkillNativeState Entry { get; }
        public BtsmtlSkillNativeState Any { get; }
        public BtsmtlSkillNativeState Exit { get; }
        public IReadOnlyList<BtsmtlSkillNativeStateOccurrence> States { get; }
        public IReadOnlyList<BtsmtlSkillNativeEdgeOccurrence> Edges { get; }

        public static BtsmtlSkillNativeStateMachineOccurrence Read(
            BtsmtlSkillNativeStateMachine machine,
            string route,
            TimelineSemanticEmitterRegistry timelineEmitters,
            CharacterSimulationCompileReport report)
        {
            if (machine == null)
                throw new ArgumentNullException(nameof(machine));
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("原生Skill FSM编译需要明确的调用路径。", nameof(route));
            BtsmtlSkillNativeStateMachineContract.Validate(machine, true);
            string contentHash = BtsmtlSkillNativeStateMachineContract.Fingerprint(machine);
            var stateMap = machine.allNodes.OfType<BtsmtlSkillNativeState>()
                .ToDictionary(value => value.UID, StringComparer.Ordinal);
            var states = new List<BtsmtlSkillNativeStateOccurrence>();
            foreach (BtsmtlSkillNativeState state in stateMap.Values.OrderBy(value => value.UID, StringComparer.Ordinal))
            {
                BtsmtlSkillGraphOccurrence body = state.Body == null
                    ? null
                    : BtsmtlSkillGraphOccurrence.Read(
                        state.Body,
                        $"{route}/state:{state.UID}/body:{state.Body.AuthoringId}",
                        timelineEmitters,
                        report);
                states.Add(new BtsmtlSkillNativeStateOccurrence(state,
                    $"{route}/state:{state.UID}", body));
            }
            var edges = new List<BtsmtlSkillNativeEdgeOccurrence>();
            foreach (BtsmtlSkillNativeState state in stateMap.Values.OrderBy(value => value.UID, StringComparer.Ordinal))
                foreach (BtsmtlSkillNativeConnection edge in state.outConnections
                             .OfType<BtsmtlSkillNativeConnection>()
                             .OrderBy(value => value.Order)
                             .ThenBy(value => value.UID, StringComparer.Ordinal))
                {
                    var target = (BtsmtlSkillNativeState)edge.targetNode;
                    BtsmtlSkillGraphOccurrence condition = edge.Condition == null
                        ? null
                        : BtsmtlSkillGraphOccurrence.Read(
                            edge.Condition,
                            $"{route}/edge:{edge.UID}/condition:{edge.Condition.AuthoringId}",
                            timelineEmitters,
                            report);
                    edges.Add(new BtsmtlSkillNativeEdgeOccurrence(
                        edge,
                        $"{route}/edge:{edge.UID}",
                        state,
                        target,
                        edge.Order,
                        edge.Priority,
                        edge.AbortPolicy,
                        condition));
                }
            return new BtsmtlSkillNativeStateMachineOccurrence(
                machine,
                route,
                contentHash,
                stateMap.Values.First(value => value is BtsmtlSkillNativeEntryState),
                stateMap.Values.First(value => value is BtsmtlSkillNativeAnyState),
                stateMap.Values.First(value => value is BtsmtlSkillNativeExitState),
                states,
                edges);
        }
    }

    public sealed class BtsmtlSkillGraphReferenceOccurrence
    {
        internal BtsmtlSkillGraphReferenceOccurrence(FlowNode owner, BtsmtlSkillGraphReferenceKind kind,
            string callSiteIdentity, string ownerContentHash, BtsmtlSkillGraphOccurrence child)
        {
            Owner = owner;
            Kind = kind;
            CallSiteIdentity = callSiteIdentity;
            OwnerContentHash = ownerContentHash;
            Child = child;
        }

        internal BtsmtlSkillGraphReferenceOccurrence(FlowNode owner, BtsmtlSkillGraphReferenceKind kind,
            string callSiteIdentity, string ownerContentHash, BtsmtlSkillNativeStateMachineOccurrence nativeStateMachine)
        {
            Owner = owner;
            Kind = kind;
            CallSiteIdentity = callSiteIdentity;
            OwnerContentHash = ownerContentHash;
            NativeStateMachine = nativeStateMachine;
        }

        public FlowNode Owner { get; }
        public BtsmtlSkillGraphReferenceKind Kind { get; }
        public string CallSiteIdentity { get; }
        public string OwnerContentHash { get; }
        public BtsmtlSkillGraphOccurrence Child { get; }
        public BtsmtlSkillNativeStateMachineOccurrence NativeStateMachine { get; }
    }

    public sealed class BtsmtlSkillEdgeOccurrence
    {
        internal BtsmtlSkillEdgeOccurrence(BinderConnection edge, string route, int order,
            int priority, ProgramAbortPolicy abortPolicy, BtsmtlSkillGraphOccurrence condition)
        {
            Edge = edge;
            Route = route;
            Order = order;
            Priority = priority;
            AbortPolicy = abortPolicy;
            Condition = condition;
        }

        public BinderConnection Edge { get; }
        public string Route { get; }
        public int Order { get; }
        public int Priority { get; }
        public ProgramAbortPolicy AbortPolicy { get; }
        public BtsmtlSkillGraphOccurrence Condition { get; }
    }

    public sealed class BtsmtlSkillTimelineOccurrence
    {
        internal BtsmtlSkillTimelineOccurrence(BtsmtlSkillTimelineFlowNode node, TimelineSemanticContentRecord content,
            Dictionary<string, BtsmtlSkillGraphOccurrence> trees)
        {
            Node = node;
            Content = content;
            Trees = new System.Collections.ObjectModel.ReadOnlyDictionary<string, BtsmtlSkillGraphOccurrence>(trees);
        }

        public BtsmtlSkillTimelineFlowNode Node { get; }
        public TimelineSemanticContentRecord Content { get; }
        public IReadOnlyDictionary<string, BtsmtlSkillGraphOccurrence> Trees { get; }
    }

    public sealed class BtsmtlSkillGraphOccurrence
    {
        BtsmtlSkillGraphOccurrence(FlowGraph graph, string route, string contentHash,
            IEnumerable<FlowNode> nodes, IEnumerable<BtsmtlSkillEdgeOccurrence> edges,
            IEnumerable<BtsmtlSkillGraphReferenceOccurrence> references, IEnumerable<BtsmtlSkillTimelineOccurrence> timelines)
        {
            Graph = graph;
            Route = route;
            ContentHash = contentHash;
            Nodes = Array.AsReadOnly(nodes.ToArray());
            Edges = Array.AsReadOnly(edges.ToArray());
            References = Array.AsReadOnly(references.ToArray());
            Timelines = Array.AsReadOnly(timelines.ToArray());
        }

        public FlowGraph Graph { get; }
        public string GraphId => ((IBtsmtlSkillFlowGraph)Graph).AuthoringId;
        public BtsmtlSkillFlowGraphRole Role => ((IBtsmtlSkillFlowGraph)Graph).Role;
        public string Route { get; }
        public string ContentHash { get; }
        public IReadOnlyList<FlowNode> Nodes { get; }
        public IReadOnlyList<BtsmtlSkillEdgeOccurrence> Edges { get; }
        public IReadOnlyList<BtsmtlSkillGraphReferenceOccurrence> References { get; }
        public IReadOnlyList<BtsmtlSkillTimelineOccurrence> Timelines { get; }

        public static BtsmtlSkillGraphOccurrence Read(FlowGraph graph, string route,
            TimelineSemanticEmitterRegistry timelineEmitters, CharacterSimulationCompileReport report)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("技能编译需要明确的调用路径。", nameof(route));
            BtsmtlSkillGraphClosure.Validate(graph, true);
            return ReadOccurrence(graph, route, new BtsmtlSkillGraphFingerprint().Compute, timelineEmitters, report);
        }

        static BtsmtlSkillGraphOccurrence ReadOccurrence(FlowGraph graph, string route, Func<FlowGraph, string> contentHash,
            TimelineSemanticEmitterRegistry timelineEmitters, CharacterSimulationCompileReport report)
        {
            string hash = contentHash(graph);
            if (string.IsNullOrWhiteSpace(hash))
                throw new InvalidOperationException($"{route}: 缺少正式作者版本。");
            FlowNode[] nodes = graph.allNodes.Cast<FlowNode>().OrderBy(node => node.UID, StringComparer.Ordinal).ToArray();
            var edges = new List<BtsmtlSkillEdgeOccurrence>();
            var references = new List<BtsmtlSkillGraphReferenceOccurrence>();
            var timelines = new List<BtsmtlSkillTimelineOccurrence>();
            foreach (FlowNode node in nodes)
            {
                IEnumerable<BinderConnection> outgoing = node.outConnections.Cast<BinderConnection>();
                outgoing = ((IBtsmtlSkillFlowGraph)graph).Role == BtsmtlSkillFlowGraphRole.StateMachine
                    ? outgoing.OrderBy(edge => edge is BtsmtlSkillFlowConnection transfer
                        ? transfer.Order
                        : int.MaxValue)
                        .ThenBy(edge => edge.UID, StringComparer.Ordinal)
                    : outgoing.OrderBy(edge => edge.UID, StringComparer.Ordinal);
                foreach (BinderConnection edge in outgoing)
                {
                    string edgeRoute = $"{route}/edge:{edge.UID}";
                    int order = 0;
                    int priority = 0;
                    ProgramAbortPolicy abortPolicy = ProgramAbortPolicy.None;
                    BtsmtlSkillGraphOccurrence condition = null;
                    if (((IBtsmtlSkillFlowGraph)graph).Role == BtsmtlSkillFlowGraphRole.StateMachine)
                    {
                        if (edge.sourcePort is not FlowOutput)
                            throw new InvalidOperationException($"{edgeRoute}: 状态机图连线必须从转移端口发出。");
                        if (edge is not BtsmtlSkillFlowConnection transfer)
                            throw new InvalidOperationException($"{edgeRoute}: 状态机转移边必须是正式 BtsmtlSkillFlowConnection。");
                        order = transfer.Order;
                        priority = transfer.Priority;
                        abortPolicy = transfer.AbortPolicy;
                        condition = transfer.Condition != null
                            ? ReadOccurrence(transfer.Condition, $"{edgeRoute}/condition:{transfer.Condition.AuthoringId}", contentHash, timelineEmitters, report)
                            : null;
                    }
                    else if (edge.sourcePort is FlowOutput && node is BtsmtlSkillCompositeFlowNode composite)
                    {
                        order = -1;
                        for (int i = 0; i < composite.Steps.Count; i++)
                            if (string.Equals(composite.Steps[i].Id, edge.sourcePortID, StringComparison.Ordinal))
                            {
                                order = i;
                                priority = composite.Steps[i].Priority;
                                abortPolicy = composite.Steps[i].AbortPolicy;
                                condition = composite.Steps[i].Condition != null
                                    ? ReadOccurrence(composite.Steps[i].Condition, $"{edgeRoute}/condition:{composite.Steps[i].Condition.AuthoringId}", contentHash, timelineEmitters, report)
                                    : null;
                                break;
                            }
                        if (order < 0)
                            throw new InvalidOperationException($"{edgeRoute}: 执行连线没有对应的稳定步骤端口。");
                    }
                    edges.Add(new BtsmtlSkillEdgeOccurrence(edge, edgeRoute, order, priority, abortPolicy, condition));
                }
                switch (node)
                {
                    case MacroNodeWrapper macro:
                        AddReference(node, BtsmtlSkillGraphReferenceKind.Macro, macro.macro);
                        break;
                    case BtsmtlSkillStateMachineFlowNode machine:
                        if (machine.StateMachine != null)
                        {
                            string callSite = $"{route}/node:{node.UID}/call:StateMachine";
                            references.Add(new BtsmtlSkillGraphReferenceOccurrence(
                                node,
                                BtsmtlSkillGraphReferenceKind.StateMachine,
                                callSite,
                                hash,
                                BtsmtlSkillNativeStateMachineOccurrence.Read(
                                    machine.StateMachine,
                                    $"{callSite}/fsm:{machine.StateMachine.AuthoringId}",
                                    timelineEmitters,
                                    report)));
                        }
                        break;
                    case BtsmtlSkillStateFlowNode state:
                        AddReference(node, BtsmtlSkillGraphReferenceKind.StateBody, state.Body);
                        break;
                }
                if (node is BtsmtlSkillTimelineFlowNode timeline)
                {
                    string timelineRoute = $"{route}/node:{node.UID}/timeline:{timeline.Timeline.AuthoringId}";
                    TimelineSemanticContentDiscoveryResult content = TimelineSemanticContentDiscovery.Discover(
                        timeline.Timeline, timelineRoute, timelineEmitters, report);
                    if (!content.IsValid)
                        throw new InvalidOperationException($"{timelineRoute}: Timeline内容发现失败。");
                    var trees = new Dictionary<string, BtsmtlSkillGraphOccurrence>(StringComparer.Ordinal);
                    foreach (TimelineSemanticTrackRecord track in content.Content.Tracks)
                        foreach (TimelineSemanticClipRecord clip in track.Clips)
                            if (clip.Clip is TreeClip tree)
                            {
                                var child = (BtsmtlSkillFlowGraph)tree.AssetTree;
                                trees.Add(tree.AuthoringId, ReadOccurrence(child,
                                    $"{clip.Route}/tree:{child.AuthoringId}", contentHash, timelineEmitters, report));
                            }
                    timelines.Add(new BtsmtlSkillTimelineOccurrence(timeline, content.Content, trees));
                }
            }
            return new BtsmtlSkillGraphOccurrence(graph, route, hash, nodes, edges, references, timelines);

            void AddReference(FlowNode owner, BtsmtlSkillGraphReferenceKind kind, FlowGraph child)
            {
                string callSite = $"{route}/node:{owner.UID}/call:{kind}";
                string childRoute = $"{callSite}/graph:{((IBtsmtlSkillFlowGraph)child).AuthoringId}";
                references.Add(new BtsmtlSkillGraphReferenceOccurrence(owner, kind, callSite, hash,
                    ReadOccurrence(child, childRoute, contentHash, timelineEmitters, report)));
            }
        }

        public IEnumerable<BtsmtlSkillGraphOccurrence> EnumerateOccurrences()
        {
            yield return this;
            foreach (BtsmtlSkillEdgeOccurrence edge in Edges)
                if (edge.Condition != null)
                    foreach (BtsmtlSkillGraphOccurrence condition in edge.Condition.EnumerateOccurrences())
                        yield return condition;
            foreach (BtsmtlSkillGraphReferenceOccurrence reference in References)
            {
                if (reference.Child != null)
                    foreach (BtsmtlSkillGraphOccurrence child in reference.Child.EnumerateOccurrences())
                        yield return child;
                if (reference.NativeStateMachine != null)
                    foreach (BtsmtlSkillNativeStateOccurrence state in reference.NativeStateMachine.States)
                        if (state.Body != null)
                            foreach (BtsmtlSkillGraphOccurrence child in state.Body.EnumerateOccurrences())
                                yield return child;
                if (reference.NativeStateMachine != null)
                    foreach (BtsmtlSkillNativeEdgeOccurrence edge in reference.NativeStateMachine.Edges)
                        if (edge.Condition != null)
                            foreach (BtsmtlSkillGraphOccurrence child in edge.Condition.EnumerateOccurrences())
                                yield return child;
            }
            foreach (BtsmtlSkillTimelineOccurrence timeline in Timelines)
                foreach (BtsmtlSkillGraphOccurrence tree in timeline.Trees.Values)
                    foreach (BtsmtlSkillGraphOccurrence child in tree.EnumerateOccurrences())
                        yield return child;
        }
    }
}
