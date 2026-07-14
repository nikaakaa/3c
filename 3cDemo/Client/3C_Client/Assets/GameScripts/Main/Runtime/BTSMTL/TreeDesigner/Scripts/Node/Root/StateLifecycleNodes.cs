using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeColor(118, 167, 255)]
    [Output("Output", PortCapacity.Single)]
    public abstract class StateLifecycleNode : RunnableNode
    {
        [SerializeField, PropertyPort(PortDirection.Output, "DeltaTime")]
        FloatPropertyPort m_DeltaTime = new FloatPropertyPort();

        [SerializeField]
        string m_OutputEdgeGUID;
        public string OutputGUID => m_OutputEdgeGUID;

        [NonSerialized]
        RunnableNode m_Child;
        public RunnableNode Child => m_Child;

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);
            ResolveFlowLink();
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
            m_DeltaTime.Value = Owner != null ? Owner.DeltaTime : 0f;
            return m_Child ? UpdateChild(m_Child, m_OutputEdgeGUID) : State.Success;
        }
        protected override NodeStopStatus OnStopRequested(NodeStopContext context)
        {
            return RequestChildStop(m_Child, context, m_OutputEdgeGUID);
        }
        protected override NodeStopStatus OnStopping(NodeStopContext context)
        {
            return RequestChildStop(m_Child, context, m_OutputEdgeGUID);
        }
        protected override void OnForceStopped(NodeStopContext context)
        {
            ForceStopChild(m_Child, context, m_OutputEdgeGUID);
        }

        void ResolveFlowLink()
        {
            m_OutputEdgeGUID = string.Empty;
            m_Child = null;

            if (m_Owner == null)
                return;

            foreach (var outputEdge in m_Owner.GetOutputEdges(this, "Output"))
            {
                m_OutputEdgeGUID = outputEdge.GUID;
                m_Child = outputEdge.EndNode as RunnableNode;
                break;
            }
        }

#if UNITY_EDITOR
        public override NodeCapabilities Capabilities => base.Capabilities &
                                                         ~NodeCapabilities.Deletable &
                                                         ~NodeCapabilities.Copiable &
                                                         ~NodeCapabilities.Groupable &
                                                         ~NodeCapabilities.Stackable;

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
#endif
    }

    [Serializable]
    [NodeName("OnEnter")]
    [NodePath("Base/State/OnEnter")]
    public sealed class StateOnEnterNode : StateLifecycleNode
    {
    }

    [Serializable]
    [NodeName("OnExit")]
    [NodePath("Base/State/OnExit")]
    public sealed class StateOnExitNode : StateLifecycleNode
    {
    }
}
