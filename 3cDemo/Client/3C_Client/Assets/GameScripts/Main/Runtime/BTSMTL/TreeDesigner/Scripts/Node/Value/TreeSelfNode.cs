using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("TreeSelf")]
    [NodePath("Base/Value/TreeSelf")]
    public class TreeSelfNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "Tree"), ReadOnly]
        TreePropertyPort m_Tree = new TreePropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Tree.Value = m_Owner as BaseTree;
        }
    }
}
