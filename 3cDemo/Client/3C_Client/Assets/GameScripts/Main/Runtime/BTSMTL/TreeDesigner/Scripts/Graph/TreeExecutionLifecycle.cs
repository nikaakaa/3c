using System;
using System.Collections.Generic;

namespace TreeDesigner
{
    public readonly struct TreeExecutionActivationId : IEquatable<TreeExecutionActivationId>
    {
        public TreeExecutionActivationId(Guid graphRuntimeId, string nodeAuthoringId, ulong generation)
        {
            GraphRuntimeId = graphRuntimeId;
            NodeAuthoringId = nodeAuthoringId ?? string.Empty;
            Generation = generation;
        }

        public Guid GraphRuntimeId { get; }
        public string NodeAuthoringId { get; }
        public ulong Generation { get; }
        public bool IsValid => GraphRuntimeId != Guid.Empty &&
                               !string.IsNullOrEmpty(NodeAuthoringId) &&
                               Generation != 0;

        public bool Equals(TreeExecutionActivationId other)
        {
            return GraphRuntimeId.Equals(other.GraphRuntimeId) &&
                   Generation == other.Generation &&
                   string.Equals(NodeAuthoringId, other.NodeAuthoringId, StringComparison.Ordinal);
        }

        public override bool Equals(object obj) => obj is TreeExecutionActivationId other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = GraphRuntimeId.GetHashCode();
                hash = hash * 31 + NodeAuthoringId.GetHashCode();
                hash = hash * 31 + Generation.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => IsValid ? $"{GraphRuntimeId:N}/{NodeAuthoringId}/{Generation}" : "Invalid";
    }

    public readonly struct TreeExecutionActivationScope : IEquatable<TreeExecutionActivationScope>
    {
        public TreeExecutionActivationScope(
            TreeExecutionActivationId activationId,
            TreeAuthoringRouteId authoringRoute,
            TreeAuthoringElementKey source,
            TreeExecutionActivationId parentActivationId,
            StateMachineExecutionPath stateMachineExecutionPath)
        {
            ActivationId = activationId;
            AuthoringRoute = authoringRoute;
            Source = source;
            ParentActivationId = parentActivationId;
            StateMachineExecutionPath = stateMachineExecutionPath;
        }

        public TreeExecutionActivationId ActivationId { get; }
        public TreeAuthoringRouteId AuthoringRoute { get; }
        public TreeAuthoringElementKey Source { get; }
        public TreeExecutionActivationId ParentActivationId { get; }
        public StateMachineExecutionPath StateMachineExecutionPath { get; }
        public bool HasParent => ParentActivationId.IsValid;
        public bool IsValid => ActivationId.IsValid &&
                               AuthoringRoute != null &&
                               AuthoringRoute.IsValid &&
                               Source.IsValid &&
                               Source.Kind == TreeAuthoringElementKind.Node &&
                               string.Equals(ActivationId.NodeAuthoringId, Source.ElementAuthoringId, StringComparison.Ordinal) &&
                               string.Equals(AuthoringRoute.LeafGraphAuthoringId, Source.GraphAuthoringId, StringComparison.Ordinal);

        public bool Equals(TreeExecutionActivationScope other)
        {
            return ActivationId.Equals(other.ActivationId) &&
                   Equals(AuthoringRoute, other.AuthoringRoute) &&
                   Source.Equals(other.Source) &&
                   ParentActivationId.Equals(other.ParentActivationId);
        }

        public override bool Equals(object obj) => obj is TreeExecutionActivationScope other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ActivationId.GetHashCode();
                hash = hash * 31 + (AuthoringRoute?.GetHashCode() ?? 0);
                hash = hash * 31 + Source.GetHashCode();
                hash = hash * 31 + ParentActivationId.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => IsValid ? $"{Source}@{ActivationId.Generation}" : "Invalid";
    }

    public interface ITreeExecutionContextSource
    {
        TreeExecutionContext TreeExecutionContext { get; }
    }

    public sealed class TreeExecutionContext
    {
        readonly Stack<TreeExecutionActivationScope> m_ActivationStack = new Stack<TreeExecutionActivationScope>();
        readonly Stack<StateMachineExecutionScope> m_StateMachineExecutionScopes = new Stack<StateMachineExecutionScope>();

        public TreeExecutionActivationScope CurrentActivation => m_ActivationStack.Count > 0
            ? m_ActivationStack.Peek()
            : default;

        public StateMachineExecutionPath CurrentStateMachineExecutionPath
        {
            get
            {
                StateMachineExecutionScope[] stack = m_StateMachineExecutionScopes.ToArray();
                var frames = new StateMachineExecutionScope[stack.Length];
                for (int i = 0; i < stack.Length; i++)
                    frames[i] = stack[stack.Length - 1 - i];
                return new StateMachineExecutionPath(frames);
            }
        }

        public TreeExecutionActivationScope BeginActivation(BaseGraph graph, RunnableNode node, ulong generation)
        {
            if (graph == null || node == null)
                throw new ArgumentNullException(graph == null ? nameof(graph) : nameof(node));
            if (graph.AuthoringRoute == null || !graph.AuthoringRoute.IsValid ||
                !string.Equals(graph.AuthoringRoute.LeafGraphAuthoringId, graph.GraphAuthoringId, StringComparison.Ordinal))
                throw new InvalidOperationException($"Graph '{graph.name}/{graph.GraphAuthoringId}' has no valid execution authoring route.");

            TreeExecutionActivationScope parent = CurrentActivation;
            var scope = new TreeExecutionActivationScope(
                new TreeExecutionActivationId(graph.RuntimeId, node.GUID, generation),
                graph.AuthoringRoute,
                TreeAuthoringElementKey.Node(graph.GraphAuthoringId, node.GUID),
                parent.ActivationId,
                CurrentStateMachineExecutionPath);
            if (!scope.IsValid)
                throw new InvalidOperationException($"Node '{graph.name}/{node.GUID}' produced an invalid activation scope.");
            return scope;
        }

        public void PushActivation(TreeExecutionActivationScope scope)
        {
            if (!scope.IsValid)
                throw new InvalidOperationException("Cannot push an invalid Tree execution activation.");
            m_ActivationStack.Push(scope);
        }

        public void PopActivation(TreeExecutionActivationScope scope)
        {
            if (m_ActivationStack.Count == 0 || !m_ActivationStack.Peek().Equals(scope))
                throw new InvalidOperationException($"Tree execution activation stack mismatch while popping '{scope}'.");
            m_ActivationStack.Pop();
        }

        public void PushStateMachineExecutionScope(StateMachineExecutionScope scope)
        {
            if (!scope.IsValid)
                throw new InvalidOperationException("Cannot push an invalid StateMachine execution scope.");
            m_StateMachineExecutionScopes.Push(scope);
        }

        public void PopStateMachineExecutionScope(StateMachineExecutionScope scope)
        {
            if (m_StateMachineExecutionScopes.Count == 0 || !m_StateMachineExecutionScopes.Peek().Equals(scope))
                throw new InvalidOperationException($"StateMachine execution scope stack mismatch while popping '{scope.StateId}/{scope.ActivationGeneration}'.");
            m_StateMachineExecutionScopes.Pop();
        }

        public void Reset()
        {
            if (m_ActivationStack.Count != 0 || m_StateMachineExecutionScopes.Count != 0)
                throw new InvalidOperationException("Tree execution context cannot reset while execution scopes remain active.");
        }
    }
}
