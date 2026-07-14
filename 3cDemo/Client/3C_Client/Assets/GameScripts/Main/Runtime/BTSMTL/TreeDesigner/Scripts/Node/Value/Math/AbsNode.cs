using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Abs")]
    [NodePath("Base/Value/Math/Abs")]
    [NodeView("VariablePropertyNodeView")]
    public class AbsNode : ValueNode
    {
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value", typeof(int), typeof(float))]
        protected PropertyPort m_InputValue = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Output, "Value", typeof(int), typeof(float)), ReadOnly]
        protected PropertyPort m_OutputValue = new PropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_InputValue)
            {
                case IntPropertyPort inputInt:
                    (m_OutputValue as IntPropertyPort).Value = Mathf.Abs(inputInt.Value);
                    break;
                case FloatPropertyPort inputFloat:
                    (m_OutputValue as FloatPropertyPort).Value = Mathf.Abs(inputFloat.Value);
                    break;
            }
        }

#if UNITY_EDITOR
        public override void OnInputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyLinked(propertyEdge);
            if (!IsConnected("m_OutputValue"))
                SetPropertyPort("m_OutputValue", propertyEdge.EndPort.GetType(), PortDirection.Output);
        }
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            if (!IsConnected("m_OutputValue"))
            {
                SetPropertyPort("m_InputValue", typeof(PropertyPort), PortDirection.Input);
                SetPropertyPort("m_OutputValue", typeof(PropertyPort), PortDirection.Output);
            }
        }
        public override void OnOutputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyLinked(propertyEdge);
            if (!IsConnected("m_InputValue"))
                SetPropertyPort("m_InputValue", propertyEdge.EndPort.GetType(), PortDirection.Input);
        }
        public override void OnOutputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyUnLinked(propertyEdge);
            if (!IsConnected("m_InputValue") && !IsConnected("m_OutputValue"))
            {
                SetPropertyPort("m_InputValue", typeof(PropertyPort), PortDirection.Input);
                SetPropertyPort("m_OutputValue", typeof(PropertyPort), PortDirection.Output);
            }
        }
#endif
    }
}