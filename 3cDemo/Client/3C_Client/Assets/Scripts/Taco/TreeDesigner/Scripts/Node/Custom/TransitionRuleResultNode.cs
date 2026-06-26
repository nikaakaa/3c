using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Rule Result")]
    [NodeColor(245, 198, 92)]
    [NodePath("Base/TransitionRule/Result")]
    public sealed class TransitionRuleResultNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Can Transition")]
        BoolPropertyPort m_Result = new BoolPropertyPort();

        public bool Evaluate()
        {
            OutputValueImperatively();
            return m_Result.Value;
        }

#if UNITY_EDITOR
        public override bool Single => true;
#endif
    }
}
