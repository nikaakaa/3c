using System;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Add")]
    [NodePath("Base/Value/Math/Add")]
    public class AddNode : MathNode
    {
        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_InputValue1)
            {
                case IntPropertyPort inputInt:
                    (m_OutputValue as IntPropertyPort).Value = inputInt.Value + (m_InputValue2 as IntPropertyPort).Value;
                    break;
                case FloatPropertyPort inputFloat:
                    (m_OutputValue as FloatPropertyPort).Value = inputFloat.Value + (m_InputValue2 as FloatPropertyPort).Value;
                    break;
            }
        }
    }
}