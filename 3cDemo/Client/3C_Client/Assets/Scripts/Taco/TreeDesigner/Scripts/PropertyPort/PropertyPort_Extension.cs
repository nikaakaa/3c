#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TreeDesigner
{
    [SerializeField]
    public partial class PropertyPort
    {
        public virtual void OnInputLinked(PropertyEdge edge)
        {
            m_InputEdgeGUID = edge.GUID;
            m_SourcePort = edge.StartPort;
        }
        public virtual void OnInputUnlinked(PropertyEdge edge)
        {
            m_InputEdgeGUID = string.Empty;
            m_SourcePort = null;
        }
        public virtual void OnOutputLinked(PropertyEdge edge)
        {
            m_OutputEdgeGUIDs.Add(edge.GUID);
            m_TargetPorts.Add(edge.EndPort);
        }
        public virtual void OnOutputUnlinked(PropertyEdge edge)
        {
            m_OutputEdgeGUIDs.Remove(edge.GUID);
            m_TargetPorts.Remove(edge.EndPort);
        }
    }

    public partial class PropertyPort<T> : PropertyPort
    {
        public override void OnInputUnlinked(PropertyEdge edge)
        {
            base.OnInputUnlinked(edge);
            m_SameTypeSourcePropertyPort = null;
        }
        public override void OnOutputUnlinked(PropertyEdge edge)
        {
            base.OnOutputUnlinked(edge);
            m_TargetPropertyPorts.Remove(edge.EndPort as PropertyPort<T>);
        }
    }
}
#endif
