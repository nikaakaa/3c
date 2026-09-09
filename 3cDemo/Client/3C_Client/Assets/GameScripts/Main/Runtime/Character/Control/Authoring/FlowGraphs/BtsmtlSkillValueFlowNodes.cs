#if UNITY_EDITOR
using System;
using ParadoxNotion.Design;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public interface IBtsmtlSkillInputNode
    {
        string InputId { get; }
        string ProviderOwnerId { get; }
        void SetInputId(string inputId, string providerOwnerId);
    }

    public abstract class BtsmtlSkillInputFlowNode<T> : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IBtsmtlSkillInputNode
    {
        [SerializeField] string m_InputId = string.Empty;
        [SerializeField] string m_ProviderOwnerId = string.Empty;
        public string InputId => m_InputId;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void SetInputId(string inputId, string providerOwnerId = "")
        {
            if (string.IsNullOrWhiteSpace(inputId))
                throw new ArgumentException("A skill input requires its declared identity.", nameof(inputId));
            if (string.IsNullOrWhiteSpace(providerOwnerId))
                throw new ArgumentException("A skill input requires its provider owner identity.", nameof(providerOwnerId));
            m_InputId = inputId;
            m_ProviderOwnerId = providerOwnerId.Trim();
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
#endif
