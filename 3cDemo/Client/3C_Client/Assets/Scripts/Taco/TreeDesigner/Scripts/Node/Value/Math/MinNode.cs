using System;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Min")]
    [NodePath("Base/Value/Math/Min")]
    public class MinNode : MathNode
    {
        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_InputValue1)
            {
                case FloatPropertyPort inputFloat:
                    (m_OutputValue as FloatPropertyPort).Value = Math.Min(inputFloat.Value, (m_InputValue2 as FloatPropertyPort).Value);
                    break;
            }
        }
    }
}