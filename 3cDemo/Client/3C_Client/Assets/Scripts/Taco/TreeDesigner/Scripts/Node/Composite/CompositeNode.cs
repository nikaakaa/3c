using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    [NodeColor(6, 214, 160)]
    [Input("Input"), Output("Output", PortCapacity.Multi)]
    public abstract partial class CompositeNode : RunnableNode
    {
        [SerializeField]
        string m_InputEdgeGUID;
        public string InputEdgeGUID => m_InputEdgeGUID;

        [SerializeField]
        protected List<string> m_OutputEdgeGUIDs = new List<string>();
        public List<string> OutputGUIDs => m_OutputEdgeGUIDs;

        [NonSerialized]
        protected RunnableNode m_Parent;
        public RunnableNode Parent => m_Parent;

        [NonSerialized]
        protected List<RunnableNode> m_Children = new List<RunnableNode>();
        public List<RunnableNode> Children => m_Children;

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

            m_Children.Clear();
            m_OutputEdgeGUIDs.Clear();
            foreach (var outputEdge in m_Owner.GetOutputEdges(this, "Output").OrderBy(i => i.FlowOrder))
            {
                if (outputEdge.EndNode is RunnableNode child)
                {
                    m_OutputEdgeGUIDs.Add(outputEdge.GUID);
                    m_Children.Add(child);
                }
            }
        }
        public override void Dispose()
        {
            base.Dispose();

            m_Parent = null;
            m_Children.Clear();
        }
        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();

            m_InputEdgeGUID = string.Empty;
            m_Parent = null;
            m_OutputEdgeGUIDs.Clear();
            m_Children.Clear();
        }
        public override void ResetNode()
        {
            base.ResetNode();
            m_Children.ForEach(i => i.ResetNode());
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

            m_OutputEdgeGUIDs.Add(edge.GUID);
            m_Children.Add(edge.EndNode as RunnableNode);

            OrderChildren();
        }
        public override void OnOutputUnlinked(BaseEdge edge)
        {
            base.OnOutputUnlinked(edge);

            m_OutputEdgeGUIDs.Remove(edge.GUID);
            m_Children.Remove(edge.EndNode as RunnableNode);

            OrderChildren();
        }
        public override void OnMoved()
        {
            base.OnMoved();
            if (m_Parent is CompositeNode compositeNode)
                compositeNode.OrderChildren();
        }
        public void OrderChildren()
        {
            m_Children = m_Children.OrderBy(i => i.Position.y).ToList();
            m_OutputEdgeGUIDs = m_OutputEdgeGUIDs.OrderBy(i =>
            {
                if (!m_Owner.GUIDEdgeMap.ContainsKey(i)) return 0f;
                return m_Owner.GUIDEdgeMap[i].EndNode.Position.y;
            }).ToList();

            for (int i = 0; i < m_OutputEdgeGUIDs.Count; i++)
            {
                if (m_Owner.GUIDEdgeMap.TryGetValue(m_OutputEdgeGUIDs[i], out BaseEdge edge))
                    edge.FlowOrder = i;
            }
        }
#endif
    }
}

