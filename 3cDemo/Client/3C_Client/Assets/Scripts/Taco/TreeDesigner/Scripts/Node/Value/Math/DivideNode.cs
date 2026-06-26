using System;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Divide")]
    [NodePath("Base/Value/Math/Divide")]
    public class DivideNode : MathNode
    {
        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_InputValue1)
            {
                case FloatPropertyPort inputFloat:
                    (m_OutputValue as FloatPropertyPort).Value = inputFloat.Value / (m_InputValue2 as FloatPropertyPort).Value;
                    break;
            }
        }
    }
}