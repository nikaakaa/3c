using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("Rule Result")]
    [NodeColor(245, 198, 92)]
    [NodePath("Base/ConditionRule/Result")]
    [NodeAuthoringCapability(NodeAuthoringCapability.SharedPureValue)]
    public sealed class ConditionRuleResultNode : ValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "Result")]
        BoolPropertyPort m_Result = new BoolPropertyPort();

        public bool Evaluate()
        {
            OutputValueImperatively();
            return m_Result.Value;
        }

        public void SetDefaultResult(bool value)
        {
            m_Result.Value = value;
        }

#if UNITY_EDITOR
        public override bool Single => true;
#endif
    }
}
