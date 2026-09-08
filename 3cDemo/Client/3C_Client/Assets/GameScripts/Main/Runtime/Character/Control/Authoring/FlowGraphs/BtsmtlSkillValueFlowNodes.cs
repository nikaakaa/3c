using System;
using ParadoxNotion.Design;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public abstract class BtsmtlSkillInputFlowNode<T> : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode
    {
        [SerializeField] string m_InputId = string.Empty;
        public string InputId => m_InputId;

        public void SetInputId(string inputId)
        {
            if (string.IsNullOrWhiteSpace(inputId))
                throw new ArgumentException("A skill input requires its declared identity.", nameof(inputId));
            m_InputId = inputId;
        }

        protected override void RegisterPorts() => AddValueOutput<T>("值", RejectAuthoringValue<T>, "m_Output");
    }

    [Name("读取布尔输入"), Category("BTSMTL/技能输入")]
    public sealed class BtsmtlSkillBooleanInputFlowNode : BtsmtlSkillInputFlowNode<bool>
    {
        public override string CapabilityId => "character-input-bool";
    }

    [Name("读取数值输入"), Category("BTSMTL/技能输入")]
    public sealed class BtsmtlSkillScalarInputFlowNode : BtsmtlSkillInputFlowNode<float>
    {
        public override string CapabilityId => "character-input-float";
    }

    [Name("读取方向输入"), Category("BTSMTL/技能输入")]
    public sealed class BtsmtlSkillVector2InputFlowNode : BtsmtlSkillInputFlowNode<Vector2>
    {
        public override string CapabilityId => "character-input-vector2";
    }

    [Name("读取方向输入长度"), Category("BTSMTL/技能输入")]
    public sealed class BtsmtlSkillInputMagnitudeFlowNode : BtsmtlSkillInputFlowNode<float>
    {
        public override string CapabilityId => "character-input-vector2-magnitude";
    }

    [Name("读取动作请求"), Category("BTSMTL/技能输入")]
    public sealed class BtsmtlSkillActionRequestFlowNode : BtsmtlSkillInputFlowNode<bool>
    {
        public override string CapabilityId => "character-action-request";
    }

}
