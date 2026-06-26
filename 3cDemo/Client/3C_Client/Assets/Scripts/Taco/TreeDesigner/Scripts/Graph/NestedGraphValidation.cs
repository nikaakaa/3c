using System.Collections.Generic;
using System.Linq;

namespace TreeDesigner
{
    public enum NestedGraphValidationIssueKind
    {
        MissingGraphReference,
        MissingAssetReference,
        GraphCycle,
        CrossScopeEdge,
        MissingPropertyPort,
        MissingStateNode,
        MissingEnterNode,
        DuplicateEnterNode,
        MissingAnyStateNode,
        DuplicateAnyStateNode,
        MissingExitState,
        DuplicateExitState,
        InvalidStateMachineBoundary,
        InvalidTransition,
        InvalidTransitionCondition,
        InvalidStateMachineNodeOutput,
        InvalidStateMachineGraphReference,
        InvalidStateBehaviorGraphReference,
        InvalidStateMachineNodeInGraph,
        InvalidTransitionRuleGraphReference,
        MissingTransitionRuleResultNode,
        DuplicateTransitionRuleResultNode,
        InvalidTransitionRuleGraphBoundary,
        MissingStateOnEnterNode,
        DuplicateStateOnEnterNode,
        MissingStateOnExitNode,
        DuplicateStateOnExitNode,
        MissingStateRootNode,
        DuplicateStateRootNode,
        InvalidStateLifecycleBoundary,
    }

    public sealed class NestedGraphValidationIssue
    {
        public NestedGraphValidationIssueKind Kind { get; }
        public BaseTree Tree { get; }
        public BaseNode Node { get; }
        public string Key { get; }
        public string Message { get; }

        public NestedGraphValidationIssue(NestedGraphValidationIssueKind kind, BaseTree tree, BaseNode node, string key, string message)
        {
            Kind = kind;
            Tree = tree;
            Node = node;
            Key = key;
            Message = message;
        }
    }

    public sealed class NestedGraphValidationResult
    {
        readonly List<NestedGraphValidationIssue> m_Issues = new List<NestedGraphValidationIssue>();

        public IReadOnlyList<NestedGraphValidationIssue> Issues => m_Issues;
        public bool IsValid => m_Issues.Count == 0;

        public void Add(NestedGraphValidationIssue issue)
        {
            m_Issues.Add(issue);
        }
    }

    public partial class BaseTree
    {
        public NestedGraphValidationResult ValidateNestedGraphReferences()
        {
            NestedGraphValidationResult result = new NestedGraphValidationResult();
            ValidateNestedGraphReferences(this, new List<BaseTree>(), new HashSet<BaseTree>(), result);
            return result;
        }

        static void ValidateNestedGraphReferences(BaseTree tree, List<BaseTree> stack, HashSet<BaseTree> visited, NestedGraphValidationResult result)
        {
            if (!tree)
                return;

            if (stack.Contains(tree))
            {
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.GraphCycle, tree, null, tree.name, $"Nested graph cycle: {BuildCyclePath(stack, tree)}"));
                return;
            }

            if (visited.Contains(tree))
                return;

            stack.Add(tree);
            PrepareNodePorts(tree);
            ValidateStateMachineGraph(tree, result);
            ValidateTransitionRuleGraph(tree, result);
            ValidateSubTreeLifecycle(tree, result);
            ValidateEdges(tree, result);

            foreach (var node in tree.Nodes)
            {
                if (!node)
                    continue;

                foreach (var reference in node.GetAssetReferences())
                {
                    if (reference.Required && !reference.Asset)
                        result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingAssetReference, tree, node, reference.Key, $"Missing asset reference: {reference.Label}"));
                }

                foreach (var reference in node.GetGraphReferences())
                {
                    if (reference.Required && !reference.Tree)
                        result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingGraphReference, tree, node, reference.Key, $"Missing graph reference: {reference.Label}"));

                    bool canValidateReference = true;
                    if (reference.Tree && node is StateMachineNode && !StateMachineNode.CanReferenceGraph(tree, reference.Tree))
                    {
                        canValidateReference = false;
                        result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateMachineGraphReference, tree, node, reference.Key, $"Invalid StateMachineNode graph reference: {reference.Tree.name}"));
                    }

                    if (reference.Tree && node is StateNode && !StateNode.CanReferenceGraph(tree, reference.Tree))
                    {
                        canValidateReference = false;
                        result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateBehaviorGraphReference, tree, node, reference.Key, $"StateNode behavior reference must be SubTree: {reference.Tree.name}"));
                    }

                    if (reference.Tree && canValidateReference)
                        ValidateNestedGraphReferences(reference.Tree, stack, visited, result);
                }
            }

            if (tree is StateMachineGraph)
            {
                foreach (var edge in tree.Edges)
                {
                    if (edge != null && edge.TransitionRuleGraph)
                        ValidateNestedGraphReferences(edge.TransitionRuleGraph, stack, visited, result);
                }
            }

            stack.RemoveAt(stack.Count - 1);
            visited.Add(tree);
        }

        static void PrepareNodePorts(BaseTree tree)
        {
            foreach (var node in tree.Nodes)
                node?.BeforeInit();
        }

        static void ValidateEdges(BaseTree tree, NestedGraphValidationResult result)
        {
            Dictionary<string, BaseNode> nodes = tree.Nodes.Where(i => i).ToDictionary(i => i.GUID, i => i);

            foreach (var edge in tree.Edges)
            {
                if (!nodes.TryGetValue(edge.StartNodeGUID, out BaseNode startNode) || !nodes.TryGetValue(edge.EndNodeGUID, out BaseNode endNode))
                    continue;

                if (tree is StateMachineGraph)
                    ValidateStateMachineGraphEdge(tree, edge, startNode, endNode, nodes, result);
                else if (tree is TransitionRuleGraph)
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidTransitionRuleGraphBoundary, tree, startNode, edge.GUID, "TransitionRuleGraph cannot contain flow edges."));
                else
                {
                    ValidateBehaviorGraphEdge(tree, edge, startNode, result);
                    ValidateScopeEdge(tree, startNode, endNode, edge.GUID, result);
                }
            }

            foreach (var propertyEdge in tree.PropertyEdges)
            {
                if (!nodes.TryGetValue(propertyEdge.StartNodeGUID, out BaseNode startNode) || !nodes.TryGetValue(propertyEdge.EndNodeGUID, out BaseNode endNode))
                    continue;

                if (!startNode.PropertyPortMap.ContainsKey(propertyEdge.StartPortName))
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingPropertyPort, tree, startNode, propertyEdge.StartPortName, $"Missing start property port id: {propertyEdge.StartPortName}"));

                if (!endNode.PropertyPortMap.ContainsKey(propertyEdge.EndPortName))
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingPropertyPort, tree, endNode, propertyEdge.EndPortName, $"Missing end property port id: {propertyEdge.EndPortName}"));

                ValidateScopeEdge(tree, startNode, endNode, propertyEdge.GUID, result);
            }
        }

        static void ValidateStateMachineGraph(BaseTree tree, NestedGraphValidationResult result)
        {
            if (!(tree is StateMachineGraph))
                return;

            List<StateNode> stateNodes = tree.Nodes.OfType<StateNode>().ToList();
            if (stateNodes.Count == 0)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingStateNode, tree, null, tree.name, "State machine graph requires at least one StateNode."));

            foreach (var node in tree.Nodes.Where(i => i))
            {
                if (node is StateMachineNode stateMachineNode)
                {
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateMachineNodeInGraph, tree, stateMachineNode, stateMachineNode.GUID, "StateMachineGraph cannot contain StateMachineNode. Use StateNode for states."));
                    continue;
                }

                if (!IsValidStateMachineGraphNode(node))
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateMachineBoundary, tree, node, node.GUID, "StateMachineGraph can only contain Enter, AnyState, Exit and StateNode."));
            }

            List<StateMachineEnterNode> enterNodes = tree.Nodes.OfType<StateMachineEnterNode>().ToList();
            List<StateMachineAnyStateNode> anyStateNodes = tree.Nodes.OfType<StateMachineAnyStateNode>().ToList();
            List<StateMachineExitNode> exitNodes = tree.Nodes.OfType<StateMachineExitNode>().ToList();

            if (enterNodes.Count == 0)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingEnterNode, tree, null, tree.name, "State machine graph requires exactly one Enter node."));
            else if (enterNodes.Count > 1)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.DuplicateEnterNode, tree, null, tree.name, "State machine graph cannot contain multiple Enter nodes."));

            if (anyStateNodes.Count == 0)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingAnyStateNode, tree, null, tree.name, "State machine graph requires exactly one AnyState node."));
            else if (anyStateNodes.Count > 1)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.DuplicateAnyStateNode, tree, null, tree.name, "State machine graph cannot contain multiple AnyState nodes."));

            if (exitNodes.Count == 0)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingExitState, tree, null, tree.name, "State machine graph requires exactly one Exit node."));
            else if (exitNodes.Count > 1)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.DuplicateExitState, tree, null, tree.name, "State machine graph cannot contain multiple Exit nodes."));

            foreach (var enterNode in enterNodes)
            {
                if (tree.GetInputEdges(enterNode, StateMachinePorts.StateIn).Any())
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateMachineBoundary, tree, enterNode, enterNode.GUID, "Enter node cannot have input transitions."));

                if (!tree.GetOutputEdges(enterNode, StateMachinePorts.StateOut).Any())
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateMachineBoundary, tree, enterNode, enterNode.GUID, "Enter node requires at least one output transition."));
            }

            foreach (var anyStateNode in anyStateNodes)
            {
                if (tree.GetInputEdges(anyStateNode, StateMachinePorts.StateIn).Any())
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateMachineBoundary, tree, anyStateNode, anyStateNode.GUID, "AnyState node cannot have input transitions."));
            }

            foreach (var exitNode in exitNodes)
            {
                if (tree.GetOutputEdges(exitNode, StateMachinePorts.StateOut).Any())
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateMachineBoundary, tree, exitNode, exitNode.GUID, "Exit node cannot have output transitions."));

                if (!tree.GetInputEdges(exitNode, StateMachinePorts.StateIn).Any())
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateMachineBoundary, tree, exitNode, exitNode.GUID, "Exit node requires at least one input transition."));
            }
        }

        static bool IsValidStateMachineGraphNode(BaseNode node)
        {
            return node is StateMachineControlNode ||
                   node is StateNode;
        }

        static void ValidateTransitionRuleGraph(BaseTree tree, NestedGraphValidationResult result)
        {
            if (!(tree is TransitionRuleGraph))
                return;

            List<TransitionRuleResultNode> resultNodes = tree.Nodes.OfType<TransitionRuleResultNode>().ToList();
            if (resultNodes.Count == 0)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingTransitionRuleResultNode, tree, null, tree.name, "TransitionRuleGraph requires exactly one Rule Result node."));
            else if (resultNodes.Count > 1)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.DuplicateTransitionRuleResultNode, tree, null, tree.name, "TransitionRuleGraph cannot contain multiple Rule Result nodes."));

            foreach (var node in tree.Nodes.Where(i => i))
            {
                if (!IsValidTransitionRuleGraphNode(node))
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidTransitionRuleGraphBoundary, tree, node, node.GUID, "TransitionRuleGraph can only contain pure value, input, predicate, logic and Rule Result nodes."));
            }
        }

        static bool IsValidTransitionRuleGraphNode(BaseNode node)
        {
            if (node is TransitionRuleResultNode)
                return true;

            if (node is RunnableNode ||
                node is StateMachineControlNode ||
                node is StateMachineNode ||
                node is StateNode ||
                node is StateLifecycleNode ||
                node is RootNode ||
                IsTimelineValueNode(node.GetType()))
                return false;

            return node is ValueNode;
        }

        static bool IsTimelineValueNode(System.Type type)
        {
            for (System.Type current = type; current != null; current = current.BaseType)
            {
                if (current.FullName == "Taco.Timeline.TimelineValueNode")
                    return true;
            }
            return false;
        }

        static void ValidateSubTreeLifecycle(BaseTree tree, NestedGraphValidationResult result)
        {
            if (!(tree is SubTree))
                return;

            if (!(tree is StateBehaviorSubTree))
            {
                if (tree.Nodes.OfType<StateLifecycleNode>().Any())
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateLifecycleBoundary, tree, null, tree.name, "Ordinary SubTree cannot contain OnEnter or OnExit nodes."));
                return;
            }

            List<RootNode> rootNodes = tree.Nodes.OfType<RootNode>().ToList();
            List<StateOnEnterNode> onEnterNodes = tree.Nodes.OfType<StateOnEnterNode>().ToList();
            List<StateOnExitNode> onExitNodes = tree.Nodes.OfType<StateOnExitNode>().ToList();

            if (rootNodes.Count == 0)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingStateRootNode, tree, null, tree.name, "StateBehaviorSubTree requires exactly one Root node."));
            else if (rootNodes.Count > 1)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.DuplicateStateRootNode, tree, null, tree.name, "StateBehaviorSubTree cannot contain multiple Root nodes."));

            if (onEnterNodes.Count == 0)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingStateOnEnterNode, tree, null, tree.name, "StateBehaviorSubTree requires exactly one OnEnter node."));
            else if (onEnterNodes.Count > 1)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.DuplicateStateOnEnterNode, tree, null, tree.name, "StateBehaviorSubTree cannot contain multiple OnEnter nodes."));

            if (onExitNodes.Count == 0)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingStateOnExitNode, tree, null, tree.name, "StateBehaviorSubTree requires exactly one OnExit node."));
            else if (onExitNodes.Count > 1)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.DuplicateStateOnExitNode, tree, null, tree.name, "StateBehaviorSubTree cannot contain multiple OnExit nodes."));
        }

        static void ValidateStateMachineGraphEdge(BaseTree tree, BaseEdge edge, BaseNode startNode, BaseNode endNode, Dictionary<string, BaseNode> nodes, NestedGraphValidationResult result)
        {
            bool transitionLike = edge.StartPortName == StateMachinePorts.StateOut || edge.EndPortName == StateMachinePorts.StateIn;
            if (transitionLike)
            {
                ValidateTransitionEdge(tree, edge, startNode, endNode, nodes, result);
                return;
            }

            result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidTransition, tree, startNode, edge.GUID, "StateMachineGraph flow edge must be StateOut->StateIn transition."));
        }

        static void ValidateTransitionEdge(BaseTree tree, BaseEdge edge, BaseNode startNode, BaseNode endNode, Dictionary<string, BaseNode> nodes, NestedGraphValidationResult result)
        {
            if (edge.StartPortName != StateMachinePorts.StateOut || edge.EndPortName != StateMachinePorts.StateIn)
            {
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidTransition, tree, startNode, edge.GUID, "State machine transition must use StateOut->StateIn ports."));
                return;
            }

            bool validTransition =
                startNode is StateMachineEnterNode && endNode is StateNode ||
                startNode is StateMachineAnyStateNode && (endNode is StateNode || endNode is StateMachineExitNode) ||
                startNode is StateNode && (endNode is StateNode || endNode is StateMachineExitNode);

            if (!validTransition)
            {
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidTransition, tree, startNode, edge.GUID, "State machine transition must connect Enter to State, or AnyState/State to State or Exit in the same graph."));
                return;
            }

            if (startNode is StateMachineAnyStateNode && !edge.HasTransitionRuleGraph)
            {
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidTransitionCondition, tree, startNode, edge.GUID, "AnyState transition requires a TransitionRuleGraph."));
                return;
            }

            if (!edge.HasTransitionRuleGraph)
                return;

            if (!(edge.TransitionRuleGraph is TransitionRuleGraph ruleGraph))
            {
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidTransitionRuleGraphReference, tree, startNode, edge.GUID, "Transition rule graph reference is invalid."));
                return;
            }

            if (ruleGraph.ResultNode == null)
            {
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingTransitionRuleResultNode, ruleGraph, null, ruleGraph.name, "TransitionRuleGraph requires a Rule Result node."));
            }
        }

        static void ValidateBehaviorGraphEdge(BaseTree tree, BaseEdge edge, BaseNode startNode, NestedGraphValidationResult result)
        {
            if (startNode is StateMachineNode && edge.StartPortName == "Output")
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateMachineNodeOutput, tree, startNode, edge.GUID, "StateMachineNode does not expose behavior output flow."));
        }

        static void ValidateScopeEdge(BaseTree tree, BaseNode startNode, BaseNode endNode, string edgeGuid, NestedGraphValidationResult result)
        {
            string startScope = GetNodeScope(startNode);
            string endScope = GetNodeScope(endNode);
            if (!string.IsNullOrEmpty(startScope) && !string.IsNullOrEmpty(endScope) && startScope != endScope)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.CrossScopeEdge, tree, startNode, edgeGuid, $"Cross scope edge: {startScope} -> {endScope}"));
        }

        static string GetNodeScope(BaseNode node)
        {
            foreach (var reference in node.GetGraphReferences())
            {
                if (!string.IsNullOrEmpty(reference.ScopeId))
                    return reference.ScopeId;
            }
            return string.Empty;
        }

        static string BuildCyclePath(List<BaseTree> stack, BaseTree repeatedTree)
        {
            int startIndex = stack.IndexOf(repeatedTree);
            return string.Join(" -> ", stack.Skip(startIndex).Select(i => i.name).Concat(new[] { repeatedTree.name }));
        }
    }
}
