#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner 
{
    public partial class BaseEdge
    {
        public virtual bool Check(BaseGraph tree, ref bool dirty)
        {
            m_Owner = tree;

            if (m_Owner.GUIDNodeMap.TryGetValue(m_StartNodeGUID, out BaseNode startNode))
                m_StartNode = startNode;
            else
            {
                dirty = true;
                return false;
            }

            if (m_Owner.GUIDNodeMap.TryGetValue(m_EndNodeGUID, out BaseNode endNode))
                m_EndNode = endNode;
            else
            {
                dirty = true;
                return false;
            }

            return true;
        }
    }

    public partial class PropertyEdge : BaseEdge
    {
        public override bool Check(BaseGraph tree, ref bool dirty)
        {
            if (!base.Check(tree, ref dirty))
                return false;

            if (m_StartNode.PropertyPortMap.TryGetValue(m_StartPortName, out PropertyPort startPort))
                m_StartPort = startPort;
            else
            {
                dirty = true;
                return false;
            }

            if (m_EndNode.PropertyPortMap.TryGetValue(m_EndPortName, out PropertyPort endPort))
                m_EndPort = endPort;
            else
            {
                dirty = true;
                return false;
            }

            return true;
        }
    }
}
#endif
