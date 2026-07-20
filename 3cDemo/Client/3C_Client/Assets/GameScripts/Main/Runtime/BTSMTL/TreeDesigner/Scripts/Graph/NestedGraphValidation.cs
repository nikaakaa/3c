using System.Collections.Generic;
using System.Linq;

namespace TreeDesigner
{
    public enum NestedGraphValidationIssueKind
    {
        MissingGraphReference,
        MissingAssetReference,
        MissingSerializedNode,
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
        InvalidConditionRuleGraphReference,
        MissingConditionRuleResultNode,
        DuplicateConditionRuleResultNode,
        InvalidConditionRuleGraphBoundary,
        InvalidBTConditionRuleGraphReference,
        InvalidBTAbortPolicy,
        MissingStateOnEnterNode,
        DuplicateStateOnEnterNode,
        MissingStateOnExitNode,
        DuplicateStateOnExitNode,
        MissingStateRootNode,
        DuplicateStateRootNode,
        InvalidStateLifecycleBoundary,
        InvalidPipelineBlackboardVariable,
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
            ValidateConditionRuleGraph(tree, result);
            ValidateSubTreeLifecycle(tree, result);
            ValidatePipelineBlackboardVariables(tree, result);
            ValidateEdges(tree, result);

            foreach (var node in tree.Nodes)
            {
                if (!node)
                {
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingSerializedNode, tree, null, tree.name, "Graph contains a missing serialized node. Legacy removed nodes such as IfNode must be migrated instead of skipped."));
                    continue;
                }

                ValidateNodeGraphReferenceOwnership(tree, node, result);

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

            foreach (var edge in tree.Edges)
            {
                if (edge != null && edge.ConditionRuleGraph)
                    ValidateNestedGraphReferences(edge.ConditionRuleGraph, stack, visited, result);
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
                else if (tree is ConditionRuleGraph)
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidConditionRuleGraphBoundary, tree, startNode, edge.GUID, "ConditionRuleGraph cannot contain flow edges."));
                else
                {
                    ValidateBehaviorGraphEdge(tree, edge, startNode, endNode, result);
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

            List<StateNode> stateNodes = tree.Nodes.Where(IsPlainStateNode).Cast<StateNode>().ToList();
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
            }
        }

        static bool IsValidStateMachineGraphNode(BaseNode node)
        {
            return node is StateMachineControlNode ||
                   IsPlainStateNode(node);
        }

        static void ValidateConditionRuleGraph(BaseTree tree, NestedGraphValidationResult result)
        {
            if (!(tree is ConditionRuleGraph))
                return;

            List<ConditionRuleResultNode> resultNodes = tree.Nodes.OfType<ConditionRuleResultNode>().ToList();
            if (resultNodes.Count == 0)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingConditionRuleResultNode, tree, null, tree.name, "ConditionRuleGraph requires exactly one Rule Result node."));
            else if (resultNodes.Count > 1)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.DuplicateConditionRuleResultNode, tree, null, tree.name, "ConditionRuleGraph cannot contain multiple Rule Result nodes."));

            foreach (var node in tree.Nodes.Where(i => i))
            {
                if (!IsValidConditionRuleGraphNode(node))
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidConditionRuleGraphBoundary, tree, node, node.GUID, "ConditionRuleGraph can only contain pure value, input, predicate, logic and Rule Result nodes."));
            }
        }

        static bool IsValidConditionRuleGraphNode(BaseNode node)
        {
            if (node is ConditionRuleResultNode)
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
                if (current.FullName == "BTSMTL.Timeline.TimelineValueNode")
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

        static void ValidatePipelineBlackboardVariables(BaseTree tree, NestedGraphValidationResult result)
        {
            if (tree == null || tree.ExposedProperties == null)
                return;

            foreach (BaseExposedProperty variable in tree.ExposedProperties)
            {
                if (!variable)
                    continue;

                if (string.IsNullOrEmpty(variable.BlackboardKey))
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidPipelineBlackboardVariable, tree, null, variable.GUID, "Pipeline blackboard variable requires a key."));

                if (variable.ValueType == null)
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidPipelineBlackboardVariable, tree, null, variable.GUID, $"Pipeline blackboard variable has unsupported type: {variable.Name}"));
            }
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
                startNode is StateMachineEnterNode && IsPlainStateNode(endNode) ||
                startNode is StateMachineAnyStateNode && (IsPlainStateNode(endNode) || endNode is StateMachineExitNode) ||
                IsPlainStateNode(startNode) && (IsPlainStateNode(endNode) || endNode is StateMachineExitNode);

            if (!validTransition)
            {
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidTransition, tree, startNode, edge.GUID, "State machine transition must connect Enter to State, or AnyState/State to State or Exit in the same graph."));
                return;
            }

            if (edge.AbortPolicy != BTAbortPolicy.None)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidBTAbortPolicy, tree, startNode, edge.GUID, "AbortPolicy is only valid on BT composite output edges."));

            ValidateConditionRuleGraphReference(tree, edge, startNode, NestedGraphValidationIssueKind.InvalidConditionRuleGraphReference, true, result);
        }

        static void ValidateNodeGraphReferenceOwnership(BaseTree tree, BaseNode node, NestedGraphValidationResult result)
        {
            if (node is StateMachineNode)
            {
                ScopedGraphReferenceModule module = node.GetModule<ScopedGraphReferenceModule>();
                if (module != null && module.SharedGraphAsset && module.InlineGraph != null)
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateMachineGraphReference, tree, node, node.GUID, "StateMachineNode graph reference cannot hold inline data and shared asset at the same time."));
            }

            if (node is StateNode)
            {
                StateBehaviorGraphReferenceModule module = node.GetModule<StateBehaviorGraphReferenceModule>();
                if (module != null && module.SharedSubTreeAsset && module.InlineSubTree != null)
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateBehaviorGraphReference, tree, node, node.GUID, "StateNode behavior reference cannot hold inline data and shared asset at the same time."));
            }
        }

        static bool IsPlainStateNode(BaseNode node)
        {
            return node != null && node.GetType() == typeof(StateNode);
        }

        static void ValidateBehaviorGraphEdge(BaseTree tree, BaseEdge edge, BaseNode startNode, BaseNode endNode, NestedGraphValidationResult result)
        {
            if (startNode is StateMachineNode && edge.StartPortName == "Output")
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidStateMachineNodeOutput, tree, startNode, edge.GUID, "StateMachineNode does not expose behavior output flow."));

            bool isBTChildEdge =
                startNode is CompositeNode &&
                endNode is RunnableNode &&
                edge.StartPortName == "Output" &&
                edge.EndPortName == "Input";

            if (!isBTChildEdge)
            {
                if (edge.HasConditionRuleGraphConfiguration)
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidBTConditionRuleGraphReference, tree, startNode, edge.GUID, "ConditionRuleGraph is only valid on BT composite output edges or StateMachine transitions."));

                if (edge.AbortPolicy != BTAbortPolicy.None)
                    result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidBTAbortPolicy, tree, startNode, edge.GUID, "AbortPolicy is only valid on BT composite output edges."));

                return;
            }

            ValidateConditionRuleGraphReference(tree, edge, startNode, NestedGraphValidationIssueKind.InvalidBTConditionRuleGraphReference, false, result);

            if (!(startNode is SelectorNode) &&
                (edge.AbortPolicy == BTAbortPolicy.LowerPriority || edge.AbortPolicy == BTAbortPolicy.Both))
            {
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.InvalidBTAbortPolicy, tree, startNode, edge.GUID, "LowerPriority and Both abort policies are only valid on Selector child edges."));
            }
        }

        static void ValidateConditionRuleGraphReference(
            BaseTree tree,
            BaseEdge edge,
            BaseNode startNode,
            NestedGraphValidationIssueKind invalidReferenceKind,
            bool required,
            NestedGraphValidationResult result)
        {
            if (edge.ConditionRuleGraphReferenceStatus == ConditionRuleGraphReferenceStatus.Unspecified)
            {
                if (required)
                {
                    result.Add(new NestedGraphValidationIssue(
                        invalidReferenceKind,
                        tree,
                        startNode,
                        edge.GUID,
                        $"ConditionRuleGraph reference is invalid: owner={tree.name}/{tree.GraphAuthoringId} edge={edge.GUID} ownership={edge.ConditionRuleGraphOwnership} reason={edge.ConditionRuleGraphReferenceError}"));
                }
                return;
            }

            if (!edge.TryResolveConditionRuleGraph(out ConditionRuleGraph ruleGraph, out string error))
            {
                result.Add(new NestedGraphValidationIssue(
                    invalidReferenceKind,
                    tree,
                    startNode,
                    edge.GUID,
                    $"ConditionRuleGraph reference is invalid: owner={tree.name}/{tree.GraphAuthoringId} edge={edge.GUID} ownership={edge.ConditionRuleGraphOwnership} reason={error}"));
                return;
            }

            if (ruleGraph.ResultNode == null)
                result.Add(new NestedGraphValidationIssue(NestedGraphValidationIssueKind.MissingConditionRuleResultNode, ruleGraph, null, ruleGraph.name, "ConditionRuleGraph requires a Rule Result node."));
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
