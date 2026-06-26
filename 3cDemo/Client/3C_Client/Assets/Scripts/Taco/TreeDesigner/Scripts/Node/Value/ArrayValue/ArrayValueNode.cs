using UnityEngine;

namespace TreeDesigner
{
    public abstract class ArrayValueNode : ValueNode
    {
        public enum NodeType { Single, List }

        [SerializeField, EnumMenu("NodeType", "OnNodeChangedCallback")]
        protected NodeType m_NodeType;
    }
}