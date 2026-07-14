using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL;
using UnityEngine;

namespace TreeDesigner
{
    [TreeWindow("OpenBaseTreeWindow")]
    [AcceptableNodePaths("Base")]
    public class StateMachineGraph : BaseTree
    {
        public IEnumerable<StateNode> StateNodes => Nodes.OfType<StateNode>();
        public StateMachineEnterNode EnterNode => Nodes.OfType<StateMachineEnterNode>().FirstOrDefault();
        public StateMachineAnyStateNode AnyStateNode => Nodes.OfType<StateMachineAnyStateNode>().FirstOrDefault();
        public StateMachineExitNode ExitNode => Nodes.OfType<StateMachineExitNode>().FirstOrDefault();

        public bool IsTransitionEdge(BaseEdge edge)
        {
            BaseNode startNode = ResolveNode(edge?.StartNode, edge?.StartNodeGUID);
            BaseNode endNode = ResolveNode(edge?.EndNode, edge?.EndNodeGUID);
            return edge != null &&
                   !(edge is PropertyEdge) &&
                   edge.StartPortName == StateMachinePorts.StateOut &&
                   edge.EndPortName == StateMachinePorts.StateIn &&
                   IsValidTransitionStart(startNode) &&
                   IsValidTransitionEnd(endNode);
        }

        bool InitializeNewConditionRuleGraph(BaseEdge edge)
        {
            if (!IsTransitionEdge(edge) || edge.HasConditionRuleGraphConfiguration)
                return false;

            edge.SetConditionRuleGraph(ConditionRuleGraph.CreateDefaultGraph(UniqueConditionRuleGraphName(edge)));
            return true;
        }

        public override bool CanCreateNodeType(Type type)
        {
            if (type == null || type.IsAbstract)
                return false;

            if (type == typeof(StateMachineEnterNode))
                return EnterNode == null;

            if (type == typeof(StateMachineAnyStateNode))
                return AnyStateNode == null;

            if (type == typeof(StateMachineExitNode))
                return ExitNode == null;

            if (typeof(StateMachineControlNode).IsAssignableFrom(type))
                return false;

            if (typeof(StateMachineNode).IsAssignableFrom(type))
                return false;

            if (type == typeof(StateNode))
                return true;

            if (typeof(RunnableNode).IsAssignableFrom(type))
                return false;

            return false;
        }

#if UNITY_EDITOR
        public override BaseEdge Link(BaseNode startNode, BaseNode endNode, string startPortName, string endPortName)
        {
            BaseEdge edge = base.Link(startNode, endNode, startPortName, endPortName);
            if (IsTransitionEdge(edge))
                InitializeNewConditionRuleGraph(edge);
            return edge;
        }

        public void RetargetTransition(BaseEdge edge, BaseNode startNode, BaseNode endNode)
        {
            if (edge == null || edge.Owner != this || startNode == null || endNode == null)
                throw new InvalidOperationException("Transition retarget requires an owned edge and valid endpoints.");
            edge.StartNode?.OnOutputUnlinked(edge);
            edge.EndNode?.OnInputUnlinked(edge);
            edge.Retarget(startNode, endNode);
            edge.Init(this);
            startNode.OnOutputLinked(edge);
            endNode.OnInputLinked(edge);
        }

        public void MoveTransitionTo(BaseEdge edge, StateMachineGraph target)
        {
            if (edge == null || edge.Owner != this || target == null || ReferenceEquals(this, target))
                throw new InvalidOperationException("Transition move requires distinct source and target StateMachineGraphs.");
            RemoveLink(edge);
            target.AddLink(edge);
        }
        public override bool CheckInit()
        {
            bool dirty = base.CheckInit();
            dirty |= EnsureControlNode<StateMachineEnterNode>(new Vector2(-360f, -120f));
            dirty |= EnsureControlNode<StateMachineAnyStateNode>(new Vector2(-360f, 120f));
            dirty |= EnsureControlNode<StateMachineExitNode>(new Vector2(360f, 0f));
            return dirty;
        }

        bool EnsureControlNode<T>(Vector2 position) where T : StateMachineControlNode
        {
            if (Nodes.OfType<T>().Any())
                return false;

            BaseNode node = CreateNode(typeof(T));
            node.Position = position;
            return true;
        }
#endif

        static bool IsValidTransitionStart(BaseNode node)
        {
            return node is StateMachineEnterNode ||
                   node is StateMachineAnyStateNode ||
                   node is StateNode;
        }

        static bool IsValidTransitionEnd(BaseNode node)
        {
            return node is StateNode ||
                   node is StateMachineExitNode;
        }

        BaseNode ResolveNode(BaseNode cachedNode, string guid)
        {
            if (cachedNode != null)
                return cachedNode;
            return string.IsNullOrEmpty(guid) ? null : Nodes.FirstOrDefault(i => i != null && i.GUID == guid);
        }

        string UniqueConditionRuleGraphName(BaseEdge edge)
        {
            string baseName = ConditionRuleGraphBaseName(edge);
            HashSet<string> existingNames = Edges
                .Where(i => i != edge)
                .Select(i => i?.ConditionRuleGraph?.name)
                .Where(i => !string.IsNullOrEmpty(i))
                .ToHashSet();

            if (!existingNames.Contains(baseName))
                return baseName;

            for (int i = 1; ; i++)
            {
                string candidate = $"{baseName} {i}";
                if (!existingNames.Contains(candidate))
                    return candidate;
            }
        }

        static string ConditionRuleGraphBaseName(BaseEdge edge)
        {
            return $"{NodeLabel(edge.StartNode)}_To_{NodeLabel(edge.EndNode)}_Rule";
        }

        static string NodeLabel(BaseNode node)
        {
            if (node == null)
                return "Node";

            NodeNameAttribute nodeNameAttribute = node.GetAttribute<NodeNameAttribute>();
            return SanitizeName(nodeNameAttribute != null ? nodeNameAttribute.Name : node.GetType().Name);
        }

        static string SanitizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Node";

            return value.Replace(" ", string.Empty).Replace("/", "_").Replace("\\", "_");
        }
    }
}
