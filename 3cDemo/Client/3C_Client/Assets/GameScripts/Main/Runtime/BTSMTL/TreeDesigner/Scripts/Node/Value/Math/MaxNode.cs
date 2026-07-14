using System;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Max")]
    [NodePath("Base/Value/Math/Max")]
    public class MaxNode : MathNode
    {
        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_InputValue1)
            {
                case FloatPropertyPort inputFloat:
                    (m_OutputValue as FloatPropertyPort).Value = Math.Max(inputFloat.Value, (m_InputValue2 as FloatPropertyPort).Value);
                    break;
            }
        }
    }
}