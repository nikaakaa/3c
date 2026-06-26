using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeView("VariablePropertyNodeView")]
    public abstract partial class MathNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value1", typeof(int), typeof(float))]
        protected PropertyPort m_InputValue1 = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value2", typeof(int), typeof(float))]
        protected PropertyPort m_InputValue2 = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Output, "Result", typeof(int), typeof(float)), ReadOnly]
        protected PropertyPort m_OutputValue = new PropertyPort();
    }
}