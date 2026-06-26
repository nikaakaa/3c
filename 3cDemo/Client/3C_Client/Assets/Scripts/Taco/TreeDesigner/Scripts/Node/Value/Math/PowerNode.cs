using System;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Power")]
    [NodePath("Base/Value/Math/Power")]
    public class PowerNode : MathNode
    {
        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_InputValue1)
            {
                case FloatPropertyPort inputFloat:
                    (m_OutputValue as FloatPropertyPort).Value = UnityEngine.Mathf.Pow(inputFloat.Value, (m_InputValue2 as FloatPropertyPort).Value);
                    break;
            }
        }
    }
}