using System;
using UnityEngine;
using UnityEngine.Playables;
using TreeDesigner;

namespace Taco.Timeline
{
    #region Base
    [Serializable]
    [NodeName("NodeName")]
    [NodeColor(217, 187, 249)]
    [Output("Output", PortCapacity.Single)]
    public class TimelineEnterNode : RunnableNode
    {

        [SerializeField]
        protected string m_OutputEdgeGUID;
        public string OutputGUID => m_OutputEdgeGUID;

        [NonSerialized]
        protected RunnableNode m_Child;
        public RunnableNode Child => m_Child;

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);

            if (!string.IsNullOrEmpty(m_OutputEdgeGUID) && m_Owner.GUIDEdgeMap.ContainsKey(m_OutputEdgeGUID))
                m_Child = m_Owner.GUIDEdgeMap[m_OutputEdgeGUID].EndNode as RunnableNode;
        }
        public override void Dispose()
        {
            base.Dispose();

            m_Child = null;
        }
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_OutputEdgeGUID = string.Empty;
            m_Child = null;
        }
        public override void ResetNode()
        {
            base.ResetNode();
            m_Child?.ResetNode();
        }

        protected override State OnUpdate()
        {
            if (m_Child)
                return m_Child.UpdateNode();
            else
                return State.None;
        }

#if UNITY_EDITOR

        public override NodeCapabilities Capabilities => base.Capabilities & ~NodeCapabilities.Deletable & ~NodeCapabilities.Copiable & ~NodeCapabilities.Groupable & ~NodeCapabilities.Stackable;
        public override void OnOutputLinked(BaseEdge edge)
        {
            base.OnOutputLinked(edge);

            m_OutputEdgeGUID = edge.GUID;
            m_Child = edge.EndNode as RunnableNode;
        }
        public override void OnOutputUnlinked(BaseEdge edge)
        {
            base.OnOutputUnlinked(edge);

            m_OutputEdgeGUID = string.Empty;
            m_Child = null;
        }

        public enum NodeEnterType
        {
            OnEnable,
            OnDisable,
            OnDestroy,
        }

        public NodeEnterType EnterType;

        string NodeName()
        {
            return EnterType.ToString();
        }
#endif
    }
    #endregion

    #region Action
    public abstract class TimelineActionNode : ActionNode
    {
        public TimelineRunningTree TimelineRunningTree { get; private set; }
        public TreeClip Clip => TimelineRunningTree.Clip;
        public Timeline Timeline => Clip?.Timeline;
        public TimelinePlayer TimelinePlayer => Timeline?.TimelinePlayer;

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);
            TimelineRunningTree = Owner as TimelineRunningTree;
        }
    }

    [NodeName("MuteTrack")]
    [NodePath("Timeline/Action/MuteTrack")]
    public class MuteTrackNode : TimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "TrackIndex")]
        IntListPropertyPort m_TrackIndex = new IntListPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "Mute")]
        BoolPropertyPort m_Mute = new BoolPropertyPort();

        protected override void DoAction()
        {
            foreach (int index in m_TrackIndex.Value)
            {
                Timeline.RuntimeMute(index, m_Mute.Value);
            }
        }
    }

    [NodeName("TimeJump")]
    [NodePath("Timeline/Action/TimeJump")]
    public class TimeJumpNode : TimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "TargetTime")]
        FloatPropertyPort m_TargetTime = new FloatPropertyPort();

        protected override void DoAction()
        {
            Timeline.JumpTo(m_TargetTime.Value);
        }
    }

    [NodeName("SetStateTime")]
    [NodePath("Timeline/Action/SetStateTime")]
    public class SetStateTimeNode: TimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "StateName")]
        StringPropertyPort m_StateName = new StringPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "StateTime")]
        FloatPropertyPort m_StateTime = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Input, "StateLayer")]
        IntPropertyPort m_StateLayer = new IntPropertyPort();

        protected override void DoAction()
        {
            TimelinePlayer.SetStateTime(m_StateName.Value, m_StateTime.Value, m_StateLayer.Value);
        }
    }

    [NodeName("SetCtrlPlayState")]
    [NodePath("Timeline/Action/SetCtrlPlayState")]
    public class SetCtrlPlayStateNode : TimelineActionNode
    {
        [SerializeField, PropertyPort(PortDirection.Input, "State")]
        BoolPropertyPort m_PlayState = new BoolPropertyPort();

        protected override void DoAction()
        {
            if (m_PlayState.Value)
            {
                TimelinePlayer.AnimationRootPlayable.GetInput(0).Play();
            }
            else
            {
                TimelinePlayer.AnimationRootPlayable.GetInput(0).Pause();
            }
        }
    }
    #endregion

    #region Value
    public abstract class TimelineValueNode :ValueNode
    {
        public TimelineRunningTree TimelineRunningTree => Owner as TimelineRunningTree;
        public TreeClip Clip => TimelineRunningTree.Clip;
        public Timeline Timeline => Clip.Timeline;
    }

    [NodeName("TimelineTime")]
    [NodePath("Timeline/Value/TimelineTime")]
    public class TimelineTimeNode : TimelineValueNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "TimelineTime"), TreeDesigner.ReadOnly]
        FloatPropertyPort m_TimelineTime = new FloatPropertyPort();
        [SerializeField, PropertyPort(PortDirection.Output, "ClipTime"), TreeDesigner.ReadOnly]
        FloatPropertyPort m_ClipTime = new FloatPropertyPort();

        protected override void OutputValue()
        {
            base.OutputValue();
            m_TimelineTime.Value = Timeline.Time;
            m_ClipTime.Value = Clip.OffsetTime;
        }
    }
    #endregion
}

