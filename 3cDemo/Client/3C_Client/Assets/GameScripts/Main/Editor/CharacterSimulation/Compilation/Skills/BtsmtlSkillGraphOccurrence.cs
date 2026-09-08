using System;
using System.Collections.Generic;
using System.Linq;
using FlowCanvas;
using FlowCanvas.Macros;
using ThirdPersonCharacter.Control.Authoring;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public enum BtsmtlSkillGraphReferenceKind
    {
        Macro,
        StateMachine,
        StateBody
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

        public FlowNode Owner { get; }
        public BtsmtlSkillGraphReferenceKind Kind { get; }
        public string CallSiteIdentity { get; }
        public string OwnerContentHash { get; }
        public BtsmtlSkillGraphOccurrence Child { get; }
    }

    public sealed class BtsmtlSkillEdgeOccurrence
    {
        internal BtsmtlSkillEdgeOccurrence(BinderConnection edge, string route, int order,
            BtsmtlSkillStepPort step, BtsmtlSkillGraphOccurrence condition)
        {
            Edge = edge;
            Route = route;
            Order = order;
            Step = step;
            Condition = condition;
        }

        public BinderConnection Edge { get; }
        public string Route { get; }
        public int Order { get; }
        public BtsmtlSkillStepPort Step { get; }
        public BtsmtlSkillGraphOccurrence Condition { get; }
    }

    public sealed class BtsmtlSkillGraphOccurrence
    {
        BtsmtlSkillGraphOccurrence(FlowGraph graph, string route, string contentHash,
            IEnumerable<FlowNode> nodes, IEnumerable<BtsmtlSkillEdgeOccurrence> edges,
            IEnumerable<BtsmtlSkillGraphReferenceOccurrence> references)
        {
            Graph = graph;
            Route = route;
            ContentHash = contentHash;
            Nodes = Array.AsReadOnly(nodes.ToArray());
            Edges = Array.AsReadOnly(edges.ToArray());
            References = Array.AsReadOnly(references.ToArray());
        }

        public FlowGraph Graph { get; }
        public string GraphId => ((IBtsmtlSkillFlowGraph)Graph).AuthoringId;
        public BtsmtlSkillFlowGraphRole Role => ((IBtsmtlSkillFlowGraph)Graph).Role;
        public string Route { get; }
        public string ContentHash { get; }
        public IReadOnlyList<FlowNode> Nodes { get; }
        public IReadOnlyList<BtsmtlSkillEdgeOccurrence> Edges { get; }
        public IReadOnlyList<BtsmtlSkillGraphReferenceOccurrence> References { get; }

        public static BtsmtlSkillGraphOccurrence Read(FlowGraph graph, string route)
        {
            if (string.IsNullOrWhiteSpace(route))
                throw new ArgumentException("技能编译需要明确的调用路径。", nameof(route));
            BtsmtlSkillGraphClosure.Validate(graph, true);
            return ReadOccurrence(graph, route, new BtsmtlSkillGraphFingerprint().Compute);
        }

        static BtsmtlSkillGraphOccurrence ReadOccurrence(FlowGraph graph, string route, Func<FlowGraph, string> contentHash)
        {
            string hash = contentHash(graph);
            if (string.IsNullOrWhiteSpace(hash))
                throw new InvalidOperationException($"{route}: 缺少正式作者版本。");
            FlowNode[] nodes = graph.allNodes.Cast<FlowNode>().OrderBy(node => node.UID, StringComparer.Ordinal).ToArray();
            var edges = new List<BtsmtlSkillEdgeOccurrence>();
            var references = new List<BtsmtlSkillGraphReferenceOccurrence>();
            foreach (FlowNode node in nodes)
            {
                foreach (BinderConnection edge in node.outConnections.Cast<BinderConnection>().OrderBy(edge => edge.UID, StringComparer.Ordinal))
                {
                    string edgeRoute = $"{route}/edge:{edge.UID}";
                    int order = 0;
                    BtsmtlSkillStepPort step = null;
                    if (edge.sourcePort is FlowOutput && node is BtsmtlSkillCompositeFlowNode composite)
                    {
                        order = -1;
                        for (int i = 0; i < composite.Steps.Count; i++)
                            if (string.Equals(composite.Steps[i].Id, edge.sourcePortID, StringComparison.Ordinal))
                            {
                                order = i;
                                step = composite.Steps[i];
                                break;
                            }
                        if (order < 0)
                            throw new InvalidOperationException($"{edgeRoute}: 执行连线没有对应的稳定步骤端口。");
                    }
                    BtsmtlSkillGraphOccurrence condition = step?.Condition != null
                        ? ReadOccurrence(step.Condition, $"{edgeRoute}/condition:{step.Condition.AuthoringId}", contentHash)
                        : null;
                    edges.Add(new BtsmtlSkillEdgeOccurrence(edge, edgeRoute, order, step, condition));
                }
                switch (node)
                {
                    case MacroNodeWrapper macro:
                        AddReference(node, BtsmtlSkillGraphReferenceKind.Macro, macro.macro);
                        break;
                    case BtsmtlSkillStateMachineFlowNode machine:
                        AddReference(node, BtsmtlSkillGraphReferenceKind.StateMachine, machine.StateMachine);
                        break;
                    case BtsmtlSkillStateFlowNode state:
                        AddReference(node, BtsmtlSkillGraphReferenceKind.StateBody, state.Body);
                        break;
                }
            }
            return new BtsmtlSkillGraphOccurrence(graph, route, hash, nodes, edges, references);

            void AddReference(FlowNode owner, BtsmtlSkillGraphReferenceKind kind, FlowGraph child)
            {
                string callSite = $"{route}/node:{owner.UID}/call:{kind}";
                string childRoute = $"{callSite}/graph:{((IBtsmtlSkillFlowGraph)child).AuthoringId}";
                references.Add(new BtsmtlSkillGraphReferenceOccurrence(owner, kind, callSite, hash,
                    ReadOccurrence(child, childRoute, contentHash)));
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
                foreach (BtsmtlSkillGraphOccurrence child in reference.Child.EnumerateOccurrences())
                    yield return child;
        }
    }
}
