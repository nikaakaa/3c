using System;
using UnityEngine;

namespace TreeDesigner 
{
    [Serializable]
    [NodeName("TreeName")]
    [NodePath("Base/Value/TreeName")]
    public class TreeNameNode : ValueNode
    {
        public enum TreeType { Self, Other }

        [SerializeField, EnumMenu("TreeType", "OnNodeChangedCallback")]
        TreeType m_TreeType;
        [SerializeField, PropertyPort(PortDirection.Input, "Tree"), ShowIf("m_TreeType", TreeType.Other), ReadOnly]
        TreePropertyPort m_Tree = new TreePropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Name"), ReadOnly]
        StringPropertyPort m_Name = new StringPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_TreeType)
            {
                case TreeType.Self:
                    m_Name.Value = m_Owner.name;
                    break;
                case TreeType.Other:
                    m_Name.Value = m_Tree.Value.name;
                    break;
                default:
                    break;
            }
        }
    }
}