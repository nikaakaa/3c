#if UNITY_EDITOR
using System;
using BTSMTL.Timeline;
using ParadoxNotion.Design;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Pipeline.Motion;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    [Name("技能激活入口"), Category("BTSMTL/动作条件")]
    [BtsmtlSkillNodeKind("activation-entry")]
    [BtsmtlSkillAuthoringField("activationEntryId", BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String)]
    public sealed class BtsmtlSkillActivationEntryFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode
    {
        [SerializeField] string m_ActivationEntryId = string.Empty;
        public string ActivationEntryId => m_ActivationEntryId ?? string.Empty;
        public void SetActivationEntryId(string entryId) => m_ActivationEntryId = entryId ?? string.Empty;
        protected override void RegisterPorts() => AddValueOutput<bool>("入口匹配", RejectAuthoringValue<bool>, "m_Output");
    }

    [Name("动作上下文激活中"), Category("BTSMTL/动作条件")]
    [BtsmtlSkillNodeKind("action-context-active")]
    [BtsmtlSkillNodeAuthoringReference(
        "actionContext",
        BtsmtlSkillNodeAuthoringReferenceKind.Asset,
        "skill_action_context_unresolved",
        "Skill Action Context引用无法解析。",
        typeof(ActionContextSlot))]
    [BtsmtlSkillAuthoringField(
        "actionContext",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.IdentityReference)]
    public sealed class BtsmtlSkillActionContextActiveFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IActionContextAuthoring
    {
        [SerializeField] ActionContextSlot m_ActionContext;
        public ActionContextSlot ActionContext => m_ActionContext;
        public void SetActionContext(ActionContextSlot context) => m_ActionContext = context;
        protected override void RegisterPorts() => AddValueOutput<bool>("激活中", RejectAuthoringValue<bool>, "m_Output");
    }

    [Name("动作窗口开放中"), Category("BTSMTL/动作条件")]
    [BtsmtlSkillNodeKind("action-window-active")]
    [BtsmtlSkillAuthoringField(
        "windowType",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String,
        NonEmpty = true)]
    public sealed class BtsmtlSkillActionWindowActiveFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, IActionWindowAuthoring
    {
        [SerializeField] string m_WindowType;
        public string WindowType => m_WindowType ?? string.Empty;
        public void SetWindowType(string windowType) => m_WindowType = CharacterActionAuthoringRules.RequireWindowType(windowType);
        protected override void RegisterPorts() => AddValueOutput<bool>("开放中", RejectAuthoringValue<bool>, "m_Output");
    }

    [Name("动作准入判断"), Category("BTSMTL/动作条件")]
    [BtsmtlSkillNodeKind("can-activate-action")]
    [BtsmtlSkillNodeAuthoringRule(BtsmtlSkillNodeAuthoringRule.TargetSnapshotObject, "targetSnapshot")]
    [BtsmtlSkillNodeAuthoringReference(
        "admissionProfile",
        BtsmtlSkillNodeAuthoringReferenceKind.AdmissionProfile,
        "skill_admission_profile_unresolved",
        "Skill CanActivate节点的GameplayAbilityAdmissionProfile引用无法解析。",
        typeof(GameplayAbilityAdmissionProfile))]
    [BtsmtlSkillAuthoringField(
        "admissionProfile",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField(
        "targetSnapshot",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.Object,
        Optional = true)]
    public sealed class BtsmtlSkillCanActivateActionFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode, ICanActivateActionAuthoring
    {
        [SerializeField] GameplayAbilityAdmissionProfile m_AdmissionProfile;
        [SerializeField] string m_TargetSnapshotDeclarationId;
        [SerializeField] string m_TargetSnapshotOwnerId;
        public GameplayAbilityAdmissionProfile AdmissionProfile => m_AdmissionProfile;
        public string TargetSnapshotDeclarationId => m_TargetSnapshotDeclarationId ?? string.Empty;
        public string TargetSnapshotOwnerId => m_TargetSnapshotOwnerId ?? string.Empty;

        public void Configure(GameplayAbilityAdmissionProfile profile, string snapshotDeclarationId, string snapshotOwnerId)
        {
            CharacterActionAuthoringRules.ValidateTargetSnapshot(snapshotDeclarationId, snapshotOwnerId);
            m_AdmissionProfile = profile;
            m_TargetSnapshotDeclarationId = snapshotDeclarationId ?? string.Empty;
            m_TargetSnapshotOwnerId = snapshotOwnerId ?? string.Empty;
        }

        protected override void RegisterPorts() => AddValueOutput<bool>("允许", RejectAuthoringValue<bool>, "m_Output");
    }

    public interface IBtsmtlSkillCharacterStateNode
    {
        string FieldId { get; }
        string ProviderOwnerId { get; }
        void Configure(string fieldId, string providerOwnerId);
    }

    [Name("移动与朝向夹角"), Category("BTSMTL/技能输入")]
    [BtsmtlSkillNodeKind("character-move-facing-angle")]
    [BtsmtlSkillProvider(BtsmtlSkillProviderKind.CharacterControlModule)]
    [BtsmtlSkillAuthoringField(
        "providerOwnerId",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String)]
    public sealed class BtsmtlSkillMoveFacingAngleFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode
    {
        [SerializeField] string m_ProviderOwnerId;

        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void Configure(string providerOwnerId)
        {
            if (string.IsNullOrWhiteSpace(providerOwnerId))
                throw new ArgumentException("Character State provider reference is incomplete.");
            m_ProviderOwnerId = providerOwnerId.Trim();
        }

        protected override void RegisterPorts()
        {
            AddValueInput<Vector2>("移动输入", "m_MoveInput");
            AddValueOutput<float>("角度", RejectAuthoringValue<float>, "m_Output");
        }
    }

    [BtsmtlSkillProvider(BtsmtlSkillProviderKind.CharacterControlModule)]
    [BtsmtlSkillAuthoringField(
        "fieldId",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String)]
    [BtsmtlSkillAuthoringField(
        "providerOwnerId",
        BTSMTL.Authoring.Graph.GraphAuthoringFieldValueKind.String)]
    public abstract class BtsmtlSkillCharacterStateFlowNode<T> :
        BtsmtlSkillFlowNode,
        IBtsmtlSkillPureValueNode,
        IBtsmtlSkillCharacterStateNode
    {
        [SerializeField] string m_FieldId = string.Empty;
        [SerializeField] string m_ProviderOwnerId = string.Empty;

        public string FieldId => m_FieldId ?? string.Empty;
        public string ProviderOwnerId => m_ProviderOwnerId ?? string.Empty;

        public void Configure(string fieldId, string providerOwnerId)
        {
            if (!CharacterStateProviderFields.IsValid(fieldId))
                throw new ArgumentException("Character State field is not supported.", nameof(fieldId));
            if (!CharacterStateProviderFields.IsOwner(providerOwnerId))
                throw new ArgumentException("Character State provider owner is incomplete.", nameof(providerOwnerId));
            m_FieldId = fieldId.Trim();
            m_ProviderOwnerId = providerOwnerId.Trim();
        }

        protected override void RegisterPorts() => AddValueOutput<T>("值", RejectAuthoringValue<T>, "m_Output");
    }

    [Name("读取Character State向量"), Category("BTSMTL/Character State")]
    [BtsmtlSkillNodeKind("character-state-vector3", TimelineDomains = TimelineExecutionDomainMask.Logic | TimelineExecutionDomainMask.Presentation)]
    [BtsmtlSkillNodeAuthoringRule(BtsmtlSkillNodeAuthoringRule.CharacterStateFieldType, "fieldId", "vector3")]
    public sealed class BtsmtlSkillCharacterStateVector3FlowNode : BtsmtlSkillCharacterStateFlowNode<Vector3>
    {
    }

    [Name("读取Character State数值"), Category("BTSMTL/Character State")]
    [BtsmtlSkillNodeKind("character-state-scalar", TimelineDomains = TimelineExecutionDomainMask.Logic | TimelineExecutionDomainMask.Presentation)]
    [BtsmtlSkillNodeAuthoringRule(BtsmtlSkillNodeAuthoringRule.CharacterStateFieldType, "fieldId", "scalar")]
    public sealed class BtsmtlSkillCharacterStateScalarFlowNode : BtsmtlSkillCharacterStateFlowNode<float>
    {
    }

    [Name("读取Character State朝向"), Category("BTSMTL/Character State")]
    [BtsmtlSkillNodeKind("character-state-yaw", TimelineDomains = TimelineExecutionDomainMask.Logic | TimelineExecutionDomainMask.Presentation)]
    [BtsmtlSkillNodeAuthoringRule(BtsmtlSkillNodeAuthoringRule.CharacterStateFieldType, "fieldId", "yaw")]
    public sealed class BtsmtlSkillCharacterStateYawFlowNode : BtsmtlSkillCharacterStateFlowNode<float>
    {
    }

    [Name("读取Character State布尔"), Category("BTSMTL/Character State")]
    [BtsmtlSkillNodeKind("character-state-bool", TimelineDomains = TimelineExecutionDomainMask.Logic | TimelineExecutionDomainMask.Presentation)]
    [BtsmtlSkillNodeAuthoringRule(BtsmtlSkillNodeAuthoringRule.CharacterStateFieldType, "fieldId", "bool")]
    public sealed class BtsmtlSkillCharacterStateBooleanFlowNode : BtsmtlSkillCharacterStateFlowNode<bool>
    {
    }

}
#endif
