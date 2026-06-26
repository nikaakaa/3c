using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("For")]
    [NodePath("Base/Decorator/For")]
    [NodeView("VariablePropertyNodeView")]
    public partial class ForNode : DecoratorNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "List", "AcceptableTypes")]
        PropertyPort m_List = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Output, "Element", "AcceptableTypes", 0), ReadOnly]
        PropertyPort m_Element = new PropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Index", 1), ReadOnly]
        IntPropertyPort m_ElementIndex = new IntPropertyPort();

        IList m_ValueList;

        protected override void OnStart()
        {
            base.OnStart();
            m_ElementIndex.Value = 0;
            m_ValueList = (IList)m_List.GetValue();
        }
        protected override State OnUpdate()
        {
            if (m_Parent.State != State.Running)
                return State.None;

            m_Element.SetValue(m_ValueList[m_ElementIndex.Value]);
            m_Child.UpdateNode();
            m_ElementIndex.Value++;
            if (m_ElementIndex.Value < m_ValueList.Count)
                return OnUpdate();
            else
                return State.Success;
        }
    }
}