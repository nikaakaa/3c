#if UNITY_EDITOR
using System;
using BTSMTL.Timeline;
using ParadoxNotion.Design;
using UnityEngine;

namespace ThirdPersonCharacter.Control.Authoring
{
    public enum BtsmtlSkillTimelineOwnership
    {
        Private,
        Shared
    }

    [Name("播放技能Timeline"), Category("BTSMTL/技能流程")]
    public sealed class BtsmtlSkillTimelineFlowNode : BtsmtlSkillFlowNode
    {
        [SerializeField] TimelineAsset m_Timeline;
        [SerializeField] BtsmtlSkillTimelineOwnership m_Ownership;
        [SerializeField] ActionContextSlot m_ActionContext;
        [SerializeField] TimelinePlaybackMode m_PlaybackMode = TimelinePlaybackMode.Once;
        public override string CapabilityId => "timeline";
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

        protected override void RegisterPorts() => AddFlowInput("执行", RejectAuthoringExecution, "Input");

        protected override void OnNodeInspectorGUI()
        {
            base.OnNodeInspectorGUI();
            if (m_Timeline && GUILayout.Button("打开Timeline编辑器"))
            {
                var observation = graph.editorObservation as IBtsmtlSkillObservationControls;
                observation?.NotifyTimelineOpening(this);
                try { UnityEditor.AssetDatabase.OpenAsset(m_Timeline); }
                finally { observation?.NotifyTimelineOpening(null); }
            }
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
        public override string CapabilityId => "@timelineEnter";
        public abstract BtsmtlSkillTimelineHook Hook { get; }
        protected override void RegisterPorts() => AddFlowOutput("执行", "Output");
    }

    [Name("片段启用"), Category("BTSMTL/Timeline"), DoNotList]
    public sealed class BtsmtlSkillTimelineEnableFlowNode : BtsmtlSkillTimelineHookFlowNode
    {
        public override BtsmtlSkillTimelineHook Hook => BtsmtlSkillTimelineHook.OnEnable;
    }

    [Name("片段停用"), Category("BTSMTL/Timeline"), DoNotList]
    public sealed class BtsmtlSkillTimelineDisableFlowNode : BtsmtlSkillTimelineHookFlowNode
    {
        public override BtsmtlSkillTimelineHook Hook => BtsmtlSkillTimelineHook.OnDisable;
    }

    [Name("片段销毁"), Category("BTSMTL/Timeline"), DoNotList]
    public sealed class BtsmtlSkillTimelineDestroyFlowNode : BtsmtlSkillTimelineHookFlowNode
    {
        public override BtsmtlSkillTimelineHook Hook => BtsmtlSkillTimelineHook.OnDestroy;
    }
}
#endif
