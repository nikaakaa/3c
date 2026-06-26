using System;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Mod")]
    [NodePath("Base/Value/Math/Mod")]
    public class ModNode : MathNode
    {
        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_InputValue1)
            {
                case IntPropertyPort inputInt:
                    (m_OutputValue as IntPropertyPort).Value = inputInt.Value % (m_InputValue2 as IntPropertyPort).Value;
                    break;
                case FloatPropertyPort inputFloat:
                    (m_OutputValue as FloatPropertyPort).Value = inputFloat.Value % (m_InputValue2 as FloatPropertyPort).Value;
                    break;
            }
        }
    }
}