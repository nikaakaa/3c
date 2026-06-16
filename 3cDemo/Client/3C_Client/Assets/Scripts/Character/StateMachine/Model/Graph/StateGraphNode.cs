using System;
using System.Collections.Generic;

namespace ThirdPersonCharacterStateMachine
{
    public sealed class StateGraphNode
    {
        readonly StateGraphNodeId[] childIds;

        public StateGraphNode(
            StateGraphNodeId id,
            StateGraphNodeId parentId,
            string pathSegment,
            StateGraphNodeId[] childIds = null)
        {
            Id = id;
            ParentId = parentId;
            PathSegment = pathSegment ?? string.Empty;
            this.childIds = childIds ?? Array.Empty<StateGraphNodeId>();
        }

        public StateGraphNodeId Id { get; }
        public StateGraphNodeId ParentId { get; }
        public string PathSegment { get; }
        public IReadOnlyList<StateGraphNodeId> ChildIds => childIds;
        public bool HasParent => ParentId.IsValid;
        public bool HasChildren => childIds.Length > 0;
    }
}
