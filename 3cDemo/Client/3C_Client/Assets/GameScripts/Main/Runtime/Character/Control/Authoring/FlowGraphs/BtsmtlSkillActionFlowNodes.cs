using System;
using BTSMTL.Timeline;
using ParadoxNotion.Design;
using ThirdPersonCharacter.ActionSystem;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    [Name("动作上下文激活中"), Category("BTSMTL/动作条件")]
    public sealed class BtsmtlSkillActionContextActiveFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode
    {
        [SerializeField] ActionContextSlot m_ActionContext;
        public override string CapabilityId => "action-context-active";
        public ActionContextSlot ActionContext => m_ActionContext;
        public void SetActionContext(ActionContextSlot context) => m_ActionContext = context;
        protected override void RegisterPorts() => AddValueOutput<bool>("激活中", RejectAuthoringValue<bool>, "m_Output");
    }

    [Name("动作窗口开放中"), Category("BTSMTL/动作条件")]
    public sealed class BtsmtlSkillActionWindowActiveFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode
    {
        [SerializeField] string m_WindowType;
        public override string CapabilityId => "action-window-active";
        public string WindowType => m_WindowType ?? string.Empty;
        public void SetWindowType(string windowType) => m_WindowType = windowType?.Trim() ?? string.Empty;
        protected override void RegisterPorts() => AddValueOutput<bool>("开放中", RejectAuthoringValue<bool>, "m_Output");
    }

    [Name("动作准入判断"), Category("BTSMTL/动作条件")]
    public sealed class BtsmtlSkillCanActivateActionFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode
    {
        [SerializeField] ActionProfile m_ActionProfile;
        [SerializeField] string m_TargetSnapshotDeclarationId;
        [SerializeField] string m_TargetSnapshotOwnerId;
        public override string CapabilityId => "can-activate-action";
        public ActionProfile ActionProfile => m_ActionProfile;
        public string TargetSnapshotDeclarationId => m_TargetSnapshotDeclarationId ?? string.Empty;
        public string TargetSnapshotOwnerId => m_TargetSnapshotOwnerId ?? string.Empty;

        public void Configure(ActionProfile profile, string snapshotDeclarationId, string snapshotOwnerId)
        {
            if (string.IsNullOrWhiteSpace(snapshotDeclarationId) != string.IsNullOrWhiteSpace(snapshotOwnerId))
                throw new ArgumentException("目标快照声明与所属作用域必须一起指定。");
            m_ActionProfile = profile;
            m_TargetSnapshotDeclarationId = snapshotDeclarationId ?? string.Empty;
            m_TargetSnapshotOwnerId = snapshotOwnerId ?? string.Empty;
        }

        protected override void RegisterPorts() => AddValueOutput<bool>("允许", RejectAuthoringValue<bool>, "m_Output");
    }

    [Name("提交动作生命周期"), Category("BTSMTL/动作流程")]
    public sealed class BtsmtlSkillSubmitActionLifecycleFlowNode : BtsmtlSkillFlowNode
    {
        [SerializeField] ActionContextSlot m_ActionContext;
        [SerializeField] ActionLifecycleTransitionType m_TransitionType = ActionLifecycleTransitionType.Complete;
        [SerializeField] string m_Reason;
        public override string CapabilityId => "submit-action-lifecycle";
        public ActionContextSlot ActionContext => m_ActionContext;
        public ActionLifecycleTransitionType TransitionType => m_TransitionType;
        public string Reason => m_Reason ?? string.Empty;

        public void Configure(ActionContextSlot context, ActionLifecycleTransitionType transitionType, string reason)
        {
            if (!Enum.IsDefined(typeof(ActionLifecycleTransitionType), transitionType) || transitionType == ActionLifecycleTransitionType.None)
                throw new ArgumentOutOfRangeException(nameof(transitionType));
            m_ActionContext = context;
            m_TransitionType = transitionType;
            m_Reason = reason ?? string.Empty;
        }

        protected override void RegisterPorts()
        {
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
            AddValueOutput<bool>("已提交", RejectAuthoringValue<bool>, "m_Submitted");
        }
    }

    [Name("移动与朝向夹角"), Category("BTSMTL/技能输入")]
    public sealed class BtsmtlSkillMoveFacingAngleFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillPureValueNode
    {
        public override string CapabilityId => "character-move-facing-angle";
        protected override void RegisterPorts()
        {
            AddValueInput<Vector2>("移动输入", "m_MoveInput");
            AddValueOutput<float>("角度", RejectAuthoringValue<float>, "m_Output");
        }
    }
}
