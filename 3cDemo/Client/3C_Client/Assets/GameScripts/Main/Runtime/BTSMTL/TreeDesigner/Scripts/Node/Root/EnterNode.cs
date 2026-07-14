using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeName("GetNodeName")]
    [NodeColor(217, 187, 249)]
    [Output("Output", PortCapacity.Single)]
    public class EnterNode : RunnableNode
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

            m_OutputEdgeGUID = string.Empty;
            m_Child = null;
            foreach (var outputEdge in m_Owner.GetOutputEdges(this, "Output"))
            {
                m_OutputEdgeGUID = outputEdge.GUID;
                m_Child = outputEdge.EndNode as RunnableNode;
                break;
            }
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

        public string NodeName;

        protected virtual string GetNodeName()
        {
            return NodeName;
        }
#endif
    }
}

