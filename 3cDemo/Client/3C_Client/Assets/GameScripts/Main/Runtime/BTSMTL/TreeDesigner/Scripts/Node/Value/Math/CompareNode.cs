using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Compare")]
    [NodePath("Base/Value/Math/Compare")]
    [NodeView("VariablePropertyNodeView")]
    public partial class CompareNode : ValueNode
    {
        public enum CompareType { Equal, NotEqual, Less, LessEqual, GreaterEqual, Greater }

        [SerializeField, ShowInPanel("Type")]
        protected CompareType m_CompareType;
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value1", typeof(int), typeof(float))]
        protected PropertyPort m_InputValue1 = new PropertyPort();
        [SerializeReference, VariablePropertyPort(PortDirection.Input, "Value2", typeof(int), typeof(float))]
        protected PropertyPort m_InputValue2 = new PropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "Result"), ReadOnly]
        protected BoolPropertyPort m_Result = new BoolPropertyPort();
        public CompareType Comparison => m_CompareType;

#if UNITY_EDITOR
        public void ConfigureAuthoring(CompareType compareType)
        {
            m_CompareType = compareType;
            OnNodeChangedCallback();
        }
#endif

        protected override void OutputValue()
        {
            base.OutputValue();
            m_Result.Value = TryReadComparable(m_InputValue1, out float left) &&
                             TryReadComparable(m_InputValue2, out float right) &&
                             CompareValue(left, right);
        }

        static bool TryReadComparable(PropertyPort port, out float value)
        {
            value = 0f;
            object rawValue = port != null && port.SourcePort != null
                ? port.SourcePort.GetValue()
                : port?.GetValue();

            switch (rawValue)
            {
                case int intValue:
                    value = intValue;
                    return true;
                case float floatValue:
                    value = floatValue;
                    return true;
                default:
                    return false;
            }
        }

        bool CompareValue(float a, float b)
        {
            switch (m_CompareType)
            {
                case CompareType.Equal:
                    return a == b;
                case CompareType.NotEqual:
                    return a != b;
                case CompareType.Less:
                    return a < b;
                case CompareType.LessEqual:
                    return a <= b;
                case CompareType.GreaterEqual:
                    return a >= b;
                case CompareType.Greater:
                    return a > b;
                default:
                    return false;
            }
        }
    }
}
