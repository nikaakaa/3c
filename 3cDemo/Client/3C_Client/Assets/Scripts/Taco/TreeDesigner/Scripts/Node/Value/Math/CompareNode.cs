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

        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_InputValue1)
            {
                case IntPropertyPort inputInt:
                    m_Result.Value = CompareValue(inputInt.Value, (m_InputValue2 as IntPropertyPort).Value);
                    break;
                case FloatPropertyPort inputFloat:
                    m_Result.Value = CompareValue(inputFloat.Value, (m_InputValue2 as FloatPropertyPort).Value);
                    break;
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