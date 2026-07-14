using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeColor(217, 187, 249)]
    [Output("Output", PortCapacity.Single)]
    public abstract partial class TriggerNode : RunnableNode
    {
        [SerializeField]
        protected string m_OutputEdgeGUID;
        public string OutputGUID => m_OutputEdgeGUID;

        [NonSerialized]
        protected RunnableNode m_Child;
        public RunnableNode Child => m_Child;

        Queue<Action> m_Actions = new Queue<Action>();

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
        public override void ResetNode()
        {
            base.ResetNode();
            m_Child?.ResetNode();
        }

        protected override State OnUpdate()
        {
            m_Child?.ResetNode();
            return m_Child ? UpdateChild(m_Child, m_OutputEdgeGUID) : State.Success;
        }
        protected override void OnCompleted(State result)
        {
            if (m_Actions.Count > 0)
                m_Actions.Dequeue()?.Invoke();
        }
        protected override NodeStopStatus OnStopRequested(NodeStopContext context)
        {
            m_Actions.Clear();
            return RequestChildStop(m_Child, context, m_OutputEdgeGUID);
        }
        protected override NodeStopStatus OnStopping(NodeStopContext context)
        {
            return RequestChildStop(m_Child, context, m_OutputEdgeGUID);
        }
        protected override void OnForceStopped(NodeStopContext context)
        {
            m_Actions.Clear();
            ForceStopChild(m_Child, context, m_OutputEdgeGUID);
        }
        protected override void OnReset()
        {
            base.OnReset();
            m_Actions.Clear();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            m_OutputEdgeGUID = string.Empty;
            m_Child = null;
        }

        public abstract void Register();
        public abstract void Unregister();
        public virtual void OnTriggered()
        {
            if (State == State.Running)
                m_Actions.Enqueue(() => UpdateNode());
            else
                UpdateNode();
        }
    }

#if UNITY_EDITOR
    public abstract partial class TriggerNode : RunnableNode
    {
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
    }
#endif
}

