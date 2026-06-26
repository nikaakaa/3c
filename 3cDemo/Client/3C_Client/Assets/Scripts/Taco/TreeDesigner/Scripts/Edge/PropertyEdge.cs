using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [Serializable]
    public partial class PropertyEdge : BaseEdge
    {
        [NonSerialized]
        protected PropertyPort m_StartPort;
        public PropertyPort StartPort => m_StartPort;

        [NonSerialized]
        protected PropertyPort m_EndPort;
        public PropertyPort EndPort => m_EndPort;

        public PropertyEdge() { }
        public PropertyEdge(BaseNode startNode, BaseNode endNode, PropertyPort startPropertyPort, PropertyPort endPropertyPort) : base(startNode, endNode, startPropertyPort.PortId, endPropertyPort.PortId) 
        {
            m_StartPort = startPropertyPort;
            m_EndPort = endPropertyPort;
        }
        public override void Init(BaseGraph tree)
        {
            base.Init(tree);
            if (m_StartNode && m_StartNode.PropertyPortMap.TryGetValue(m_StartPortName, out PropertyPort startPort))
                m_StartPort = startPort;
            if (m_EndNode && m_EndNode.PropertyPortMap.TryGetValue(m_EndPortName, out PropertyPort endPort))
                m_EndPort = endPort;
        }
        public override void Dispose()
        {
            base.Dispose();
            m_StartPort = null;
            m_EndPort = null;
        }
    }
}
