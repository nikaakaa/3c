using System;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeColor(255, 209, 102)]
    [Input("Input"), Output("Output", PortCapacity.Single)]
    public abstract partial class DecoratorNode : RunnableNode
    {
        [SerializeField]
        string m_InputEdgeGUID;
        public string InputEdgeGUID => m_InputEdgeGUID;
        
        [SerializeField]
        protected string m_OutputEdgeGUID;
        public string OutputGUID => m_OutputEdgeGUID;

        [NonSerialized]
        protected RunnableNode m_Parent;
        public RunnableNode Parent => m_Parent;

        [NonSerialized]
        protected RunnableNode m_Child;
        public RunnableNode Child => m_Child;

        public override void Init(BaseGraph tree)
        {
            base.Init(tree);

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
            foreach (var inputEdge in m_Owner.GetInputEdges(this, "Input"))
            {
                m_InputEdgeGUID = inputEdge.GUID;
                m_Parent = inputEdge.StartNode as RunnableNode;
                break;
            }

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

            m_Parent = null;
            m_Child = null;
        }
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
            m_OutputEdgeGUID = string.Empty;
            m_Child = null;
        }
        public override void ResetNode()
        {
            base.ResetNode();
            m_Child?.ResetNode();
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
        public override void OnInputLinked(BaseEdge edge)
        {
            base.OnInputLinked(edge);
            m_InputEdgeGUID = edge.GUID;
            m_Parent = edge.StartNode as RunnableNode;
        }
        public override void OnInputUnlinked(BaseEdge edge)
        {
            base.OnInputUnlinked(edge);

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
        }
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
        public override void OnMoved()
        {
            base.OnMoved();
            if (m_Parent is CompositeNode compositeNode)
                compositeNode.OrderChildren();
        }
#endif
    }
}

