using System;
using System.Collections;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("ToList")]
    [NodePath("Base/Value/Operate/ToList")]
    [NodeView("VariablePropertyNodeView")]
    public partial class ToListNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Element", "AcceptableTypes")]
        PropertyPort m_Element = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Output, "List", "AcceptableTypes"), ReadOnly]
        PropertyPort m_List = new PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            (m_List.GetValue() as IList).Clear();
            (m_List.GetValue() as IList).Add(m_Element.GetValue());
        }
    }
}