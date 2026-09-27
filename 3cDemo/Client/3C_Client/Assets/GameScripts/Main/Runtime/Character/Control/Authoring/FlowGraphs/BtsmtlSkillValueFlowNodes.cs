#if UNITY_EDITOR
using System;
using ThirdPersonCharacter.Pipeline.Motion;
using ParadoxNotion.Design;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public interface IBtsmtlSkillInputNode : ICharacterInputValueAuthoring
    {
        new string InputId { get; }
        string ProviderOwnerId { get; }
        void SetInputId(string inputId, string providerOwnerId);
    }

    [BtsmtlSkillProvider(BtsmtlSkillProviderKind.InputProfile)]
    [BtsmtlSkillAuthoringField("inputId", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String)]
    [BtsmtlSkillAuthoringField("providerOwnerId", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String)]
    public abstract class BtsmtlSkillInputFlowNode<T> : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IBtsmtlSkillInputNode
    {
        [SerializeField] string m_InputId = string.Empty;
        [SerializeField] string m_ProviderOwnerId = string.Empty;
        public string InputId => m_InputId;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void SetInputId(string inputId, string providerOwnerId = "")
        {
            inputId = CharacterInputAuthoringRules.RequireInputId(inputId);
            if (string.IsNullOrWhiteSpace(providerOwnerId))
                throw new ArgumentException("A skill input requires its provider owner identity.", nameof(providerOwnerId));
            m_InputId = inputId;
            m_ProviderOwnerId = providerOwnerId.Trim();
        }

        protected override void RegisterPorts() => AddValueOutput<T>("值", RejectAuthoringValue<T>, "m_Output");
    }

    [Name("读取布尔输入"), Category("BTSMTL/技能输入")]
    [BtsmtlSkillNodeKind("character-input-bool")]
    [BtsmtlSkillNodeAuthoringReference(
        "inputId",
        BtsmtlSkillNodeAuthoringReferenceKind.InputValue,
        "skill_input_value_unresolved",
        "Skill Input节点的inputId无法解析。")]
    public sealed class BtsmtlSkillBooleanInputFlowNode : BtsmtlSkillInputFlowNode<bool>
    {
    }

    [Name("读取数值输入"), Category("BTSMTL/技能输入")]
    [BtsmtlSkillNodeKind("character-input-float")]
    [BtsmtlSkillNodeAuthoringReference(
        "inputId",
        BtsmtlSkillNodeAuthoringReferenceKind.InputValue,
        "skill_input_value_unresolved",
        "Skill Input节点的inputId无法解析。")]
    public sealed class BtsmtlSkillScalarInputFlowNode : BtsmtlSkillInputFlowNode<float>
    {
    }

    [Name("读取方向输入"), Category("BTSMTL/技能输入")]
    [BtsmtlSkillNodeKind("character-input-vector2")]
    [BtsmtlSkillNodeAuthoringReference(
        "inputId",
        BtsmtlSkillNodeAuthoringReferenceKind.InputValue,
        "skill_input_value_unresolved",
        "Skill Input节点的inputId无法解析。")]
    public sealed class BtsmtlSkillVector2InputFlowNode : BtsmtlSkillInputFlowNode<Vector2>
    {
    }

    [Name("读取方向输入长度"), Category("BTSMTL/技能输入")]
    [BtsmtlSkillNodeKind("character-input-vector2-magnitude")]
    [BtsmtlSkillNodeAuthoringReference(
        "inputId",
        BtsmtlSkillNodeAuthoringReferenceKind.InputValue,
        "skill_input_value_unresolved",
        "Skill Input节点的inputId无法解析。")]
    public sealed class BtsmtlSkillInputMagnitudeFlowNode : BtsmtlSkillInputFlowNode<float>
    {
    }

    [Name("读取动作请求"), Category("BTSMTL/技能输入")]
    [BtsmtlSkillNodeKind("character-action-request")]
    [BtsmtlSkillNodeAuthoringReference(
        "inputId",
        BtsmtlSkillNodeAuthoringReferenceKind.ActionRequest,
        "skill_action_request_unresolved",
        "Skill Action Request节点的inputId无法解析。")]
    public sealed class BtsmtlSkillActionRequestFlowNode : BtsmtlSkillInputFlowNode<bool>, ICharacterActionRequestAuthoring
    {
        public string RequestId => InputId;
    }

}
#endif
