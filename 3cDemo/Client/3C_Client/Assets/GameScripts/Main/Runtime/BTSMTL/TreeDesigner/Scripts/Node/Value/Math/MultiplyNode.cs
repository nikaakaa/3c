using System;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Multiply")]
    [NodePath("Base/Value/Math/Multiply")]
    public class MultiplyNode : MathNode
    {
        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_InputValue1)
            {
                case FloatPropertyPort inputFloat:
                    (m_OutputValue as FloatPropertyPort).Value = inputFloat.Value * (m_InputValue2 as FloatPropertyPort).Value;
                    break;
                case IntPropertyPort inputInt:
                    (m_OutputValue as IntPropertyPort).Value = inputInt.Value * (m_InputValue2 as IntPropertyPort).Value;
                    break;
            }
        }
    }
}