using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace TreeDesigner.Editor
{
    public class PropertyPortView : BasePortView
    {
        protected PropertyPort m_PropertyPort;
        public PropertyPort PropertyPort => m_PropertyPort;

        protected PortHandle m_PortHandle;
        public PortHandle PortHandle => m_PortHandle;

        public int PortIndex => m_PropertyPort.Index;

        protected PropertyPortView(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type) : base(portOrientation, portDirection, portCapacity, type)
        {
        }

        public void SetPropertyPort(PropertyPort propertyPort)
        {
            m_PropertyPort = propertyPort;
            m_Name = propertyPort.PortId;
            m_PortHandle.style.backgroundColor = portColor = propertyPort.Color();
            portType = m_PropertyPort.ValueType == null ? typeof(object) : m_PropertyPort.ValueType;
        }

        public static PropertyPortView Create<TEdge>(PropertyPort propertyPort, Orientation orientation, Capacity capacity) where TEdge : Edge, new()
        {
            PropertyPortView port = new PropertyPortView(orientation, (Direction)propertyPort.Direction, capacity, propertyPort.ValueType);
            port.m_Name = propertyPort.PortId;
            port.m_PropertyPort = propertyPort;
            port.InstallConnector<TEdge>();

            PortHandle portHandle = new PortHandle(port);
            port.m_PortHandle = portHandle;
            port.Insert(1, portHandle);
            portHandle.style.backgroundColor = port.portColor = propertyPort.Color();       
            return port;
        }
    }
}
