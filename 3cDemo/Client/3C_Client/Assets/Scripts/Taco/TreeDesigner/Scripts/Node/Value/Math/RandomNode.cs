using System;
using Random = UnityEngine.Random;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Random")]
    [NodePath("Base/Value/Math/Random")]
    public class RandomNode : MathNode
    {
        protected override void OutputValue()
        {
            base.OutputValue();
            switch (m_InputValue1)
            {
                case IntPropertyPort inputInt:
                    (m_OutputValue as IntPropertyPort).Value = Random.Range(inputInt.Value, (m_InputValue2 as IntPropertyPort).Value);
                    break;
                case FloatPropertyPort inputFloat:
                    (m_OutputValue as FloatPropertyPort).Value = Random.Range(inputFloat.Value, (m_InputValue2 as FloatPropertyPort).Value);
                    break;
            }
        }
    }
}