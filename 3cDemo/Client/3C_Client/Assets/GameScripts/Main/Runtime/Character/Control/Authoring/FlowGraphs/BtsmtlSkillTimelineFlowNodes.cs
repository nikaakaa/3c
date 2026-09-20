#if UNITY_EDITOR
using System;
using BTSMTL.Timeline;
using ParadoxNotion.Design;
using ThirdPersonCharacter.Pipeline.Motion;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public enum BtsmtlSkillTimelineOwnership
    {
        Private,
        Shared
    }

    [Name("播放技能Timeline"), Category("BTSMTL/技能流程")]
    [BtsmtlSkillNodeKind("timeline")]
    [BtsmtlSkillNodeAuthoringRule(BtsmtlSkillNodeAuthoringRule.TimelineReference, "timelineId")]
    [BtsmtlSkillAuthoringField(
        "timelineId",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference)]
    [BtsmtlSkillNodeAuthoringReference(
        "timelineId",
        BtsmtlSkillNodeAuthoringReferenceKind.Asset,
        "skill_timeline_unresolved",
        "Skill Timeline引用无法解析。",
        typeof(TimelineAsset))]
    [BtsmtlSkillAuthoringField("timelineOwnership", typeof(BtsmtlSkillTimelineOwnership))]
    [BtsmtlSkillNodeAuthoringReference(
        "actionContext",
        BtsmtlSkillNodeAuthoringReferenceKind.Asset,
        "skill_action_context_unresolved",
        "Skill Action Context引用无法解析。",
        typeof(ActionContextSlot),
        Optional = true)]
    [BtsmtlSkillAuthoringField(
        "actionContext",
        TreeDesigner.Authoring.GraphAuthoringFieldValueKind.IdentityReference,
        Optional = true)]
    [BtsmtlSkillAuthoringField("playbackMode", typeof(TimelinePlaybackMode))]
    public sealed class BtsmtlSkillTimelineFlowNode : BtsmtlSkillFlowNode, IActionContextAuthoring
    {
        [SerializeField] TimelineAsset m_Timeline;
        [SerializeField] BtsmtlSkillTimelineOwnership m_Ownership;
        [SerializeField] ActionContextSlot m_ActionContext;
        [SerializeField] TimelinePlaybackMode m_PlaybackMode = TimelinePlaybackMode.Once;
        public TimelineAsset TimelineAsset => m_Timeline;
        public TimelineData Timeline => m_Timeline != null ? m_Timeline.Data : null;
        public BtsmtlSkillTimelineOwnership Ownership => m_Ownership;
        public ActionContextSlot ActionContext => m_ActionContext;
        public TimelinePlaybackMode PlaybackMode => m_PlaybackMode;

        public void Configure(TimelineAsset timeline, BtsmtlSkillTimelineOwnership ownership,
            ActionContextSlot actionContext, TimelinePlaybackMode playbackMode)
        {
            if (timeline == null || !Enum.IsDefined(typeof(BtsmtlSkillTimelineOwnership), ownership) ||
                !Enum.IsDefined(typeof(TimelinePlaybackMode), playbackMode))
                throw new ArgumentException("技能Timeline引用、所有权及播放模式必须有效。");
            m_Timeline = timeline;
            m_Ownership = ownership;
            m_ActionContext = actionContext;
            m_PlaybackMode = playbackMode;
        }

        protected override void RegisterPorts()
        {
            AddFlowInput("执行", RejectAuthoringExecution, "Input");
            AddValueInput<float>("动作进度倍率", "m_PlaybackRate").SetDefaultAndSerializedValue(1f);
            AddValueInput<bool>("暂停动作进度", "m_Paused").SetDefaultAndSerializedValue(false);
        }

        protected override void OnNodeInspectorGUI()
        {
            BtsmtlSkillNodeInspector.Draw(this);
        }
    }

    [Name("结束片段"), Category("BTSMTL/Timeline")]
    [BtsmtlSkillNodeKind("timelineClipExitRequest")]
    public sealed class BtsmtlSkillTimelineExitRequestFlowNode : BtsmtlSkillFlowNode
    {
        protected override void RegisterPorts() => AddFlowInput("执行", RejectAuthoringExecution, "Input");

        protected override void OnNodeInspectorGUI()
        {
            BtsmtlSkillNodeInspector.Draw(this);
        }
    }

    public enum BtsmtlSkillTimelineHook
    {
        OnEnable,
        OnDisable,
        OnDestroy
    }

    public abstract class BtsmtlSkillTimelineHookFlowNode : BtsmtlSkillFlowNode, IBtsmtlSkillSystemNode
    {
        public abstract BtsmtlSkillTimelineHook Hook { get; }
        protected override void RegisterPorts() => AddFlowOutput("执行", "Output");
    }

    [Name("片段启用"), Category("BTSMTL/Timeline"), DoNotList]
    [BtsmtlSkillNodeKind("@timelineEnable", TimelineDomains = TimelineExecutionDomainMask.Logic | TimelineExecutionDomainMask.Presentation)]
    public sealed class BtsmtlSkillTimelineEnableFlowNode : BtsmtlSkillTimelineHookFlowNode
    {
        public override BtsmtlSkillTimelineHook Hook => BtsmtlSkillTimelineHook.OnEnable;
    }

    [Name("片段停用"), Category("BTSMTL/Timeline"), DoNotList]
    [BtsmtlSkillNodeKind("@timelineDisable")]
    public sealed class BtsmtlSkillTimelineDisableFlowNode : BtsmtlSkillTimelineHookFlowNode
    {
        public override BtsmtlSkillTimelineHook Hook => BtsmtlSkillTimelineHook.OnDisable;
    }

    [Name("片段销毁"), Category("BTSMTL/Timeline"), DoNotList]
    [BtsmtlSkillNodeKind("@timelineDestroy")]
    public sealed class BtsmtlSkillTimelineDestroyFlowNode : BtsmtlSkillTimelineHookFlowNode
    {
        public override BtsmtlSkillTimelineHook Hook => BtsmtlSkillTimelineHook.OnDestroy;
    }
}
#endif
