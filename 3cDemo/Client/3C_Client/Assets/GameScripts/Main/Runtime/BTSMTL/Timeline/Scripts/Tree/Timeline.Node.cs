using System;
using UnityEngine;
using TreeDesigner;

namespace BTSMTL.Timeline
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
                return UpdateChild(m_Child, m_OutputEdgeGUID);
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
        public TimelineTreeClipRuntimeContext ClipContext => TimelineRunningTree?.ClipContext;
        public TreeClip Clip => ClipContext?.Clip;
        public TimelineData Timeline => ClipContext?.Timeline;

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);
            TimelineRunningTree = Owner as TimelineRunningTree;
        }
    }

    #endregion

    #region Value
    public abstract class TimelineValueNode :ValueNode
    {
        public TimelineRunningTree TimelineRunningTree => Owner as TimelineRunningTree;
        public TimelineTreeClipRuntimeContext ClipContext => TimelineRunningTree?.ClipContext;
        public TreeClip Clip => ClipContext?.Clip;
        public TimelineData Timeline => ClipContext?.Timeline;
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
            if (ClipContext == null)
                return;

            m_TimelineTime.Value = ClipContext.TimelineTime;
            m_ClipTime.Value = ClipContext.ClipTime;
        }
    }
    #endregion
}

