using System;
using System.Collections.Generic;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class CharacterStateMetadataSet
    {
        readonly CharacterStateNodeMetadata[] nodes;
        readonly Dictionary<StateGraphNodeId, CharacterStateNodeMetadata> metadataByNode;

        public CharacterStateMetadataSet(CharacterStateNodeMetadata[] nodes)
        {
            this.nodes = nodes ?? Array.Empty<CharacterStateNodeMetadata>();
            metadataByNode = new Dictionary<StateGraphNodeId, CharacterStateNodeMetadata>();

            for (int i = 0; i < this.nodes.Length; i++)
            {
                CharacterStateNodeMetadata node = this.nodes[i];
                if (node.NodeId.IsValid && !metadataByNode.ContainsKey(node.NodeId))
                    metadataByNode.Add(node.NodeId, node);
            }
        }

        public IReadOnlyList<CharacterStateNodeMetadata> Nodes => nodes;

        public bool TryGetNode(StateGraphNodeId nodeId, out CharacterStateNodeMetadata metadata)
        {
            return metadataByNode.TryGetValue(nodeId, out metadata);
        }

        public bool TryDeriveStateDomainView(
            in CharacterStateMachineSnapshot snapshot,
            out CharacterStateDomainView view)
        {
            if (TryGetNode(new StateGraphNodeId(snapshot.ActivePath), out CharacterStateNodeMetadata metadata))
            {
                view = CharacterStateDomainView.FromSnapshotAndMetadata(in snapshot, in metadata);
                return true;
            }

            view = default;
            return false;
        }
    }
}
