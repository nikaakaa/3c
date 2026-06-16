using System;
using System.Collections.Generic;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class StateGraphDefinition
    {
        readonly StateGraphNode[] nodes;
        readonly StateGraphTransition[] transitions;
        readonly Dictionary<StateGraphNodeId, StateGraphNode> nodeMap;

        public StateGraphDefinition(
            StateGraphNodeId initialNodeId,
            StateGraphNode[] nodes,
            StateGraphTransition[] transitions)
        {
            InitialNodeId = initialNodeId;
            this.nodes = nodes ?? Array.Empty<StateGraphNode>();
            this.transitions = transitions ?? Array.Empty<StateGraphTransition>();
            nodeMap = new Dictionary<StateGraphNodeId, StateGraphNode>();

            for (int i = 0; i < this.nodes.Length; i++)
            {
                StateGraphNode node = this.nodes[i];
                if (node != null && node.Id.IsValid && !nodeMap.ContainsKey(node.Id))
                    nodeMap.Add(node.Id, node);
            }
        }

        public StateGraphNodeId InitialNodeId { get; }
        public IReadOnlyList<StateGraphNode> Nodes => nodes;
        public IReadOnlyList<StateGraphTransition> Transitions => transitions;

        public bool TryGetNode(StateGraphNodeId id, out StateGraphNode node)
        {
            return nodeMap.TryGetValue(id, out node);
        }
    }
}
