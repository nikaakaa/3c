using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using Taco.Editor;

namespace TreeDesigner.Editor
{
    public class NodePortContainerView : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<NodePortContainerView, UxmlTraits> { }

        protected BaseNode m_Node;
        protected BaseNodeView m_NodeView;
        protected DropArea m_DropArea;

        protected Dictionary<string, BasePortView> m_PortViewMap = new Dictionary<string, BasePortView>();
        public Dictionary<string, BasePortView> PortViewMap => m_PortViewMap;

        protected Dictionary<string, PropertyPortView> m_PropertyPortViewMap = new Dictionary<string, PropertyPortView>();
        public Dictionary<string, PropertyPortView> PropertyPortViewMap => m_PropertyPortViewMap;

        public NodePortContainerView()
        {
            VisualTreeAsset template = Resources.Load<VisualTreeAsset>("VisualTree/NodePortContainer");
            template.CloneTree(this);
            AddToClassList("nodePortContainer");

            m_DropArea = new DropArea();
            m_DropArea.Init(this);
            m_DropArea.onDragUpdateEvent += (e) =>
            {
                if(DragAndDrop.GetGenericData("PortHandle") is PortHandle portHandle && m_PropertyPortViewMap.ContainsValue(portHandle.PropertyPortView))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                    float y = e.localMousePosition.y;
                    Dictionary<string, (PropertyPortView, float)> propertyPortViewMap = new Dictionary<string, (PropertyPortView, float)>();
                    propertyPortViewMap.Add(portHandle.PropertyPortView.PropertyPort.PortId, (portHandle.PropertyPortView, y));

                    foreach (var item in m_PropertyPortViewMap)
                    {
                        if (item.Value != portHandle.PropertyPortView)
                            propertyPortViewMap.Add(item.Key, (item.Value, this.WorldToLocal(item.Value.worldBound.position).y + item.Value.worldBound.height / 2));
                    }
                    propertyPortViewMap = propertyPortViewMap.OrderBy(i => i.Value.Item2).ToDictionary(i => i.Key, i => i.Value);
                    Sort(propertyPortViewMap);
                }
            };
            m_DropArea.onDragPerformEvent += (e) =>
            {
                if (DragAndDrop.GetGenericData("PortHandle") is PortHandle portHandle && m_PropertyPortViewMap.ContainsValue(portHandle.PropertyPortView))
                {
                    m_Node.ApplyModify("Move Port", () =>
                    {
                        int index = 0;
                        foreach (var propertyPortViewPair in m_PropertyPortViewMap)
                        {
                            propertyPortViewPair.Value.PropertyPort.Index = index;
                            index++;
                        }
                    });
                }
            };
        }

        public void Init(BaseNode node, BaseNodeView nodeView)
        {
            m_Node = node;
            m_NodeView = nodeView;
        }

        public BasePortView AddPort(string name, Direction direction, Port.Capacity capacity)
        {
            BasePortView portView = BasePortView.Create<BaseEdgeView>(name, Orientation.Horizontal, direction, capacity, typeof(BaseNode));
            portView.portName = "";
            portView.name = "basePort";
            Insert(0, portView);
            m_PortViewMap.Add(name, portView);

            var portConnector = portView.Q("connector");
            portConnector.style.borderTopLeftRadius = portConnector.style.borderTopRightRadius = portConnector.style.borderBottomLeftRadius = portConnector.style.borderBottomRightRadius = 0;
            var portCap = portConnector.Q("cap");
            portCap.style.borderTopLeftRadius = portCap.style.borderTopRightRadius = portCap.style.borderBottomLeftRadius = portCap.style.borderBottomRightRadius = 0;
            return portView;
        }
        public void RemovePort(string name)
        {
            m_NodeView.TreeView.DeleteElements(m_PortViewMap[name].connections);
            Remove(m_PortViewMap[name]);
            m_PortViewMap.Remove(name);
        }
        public PropertyPortView AddPropertyPort(PropertyPort propertyPort, string portName, Port.Capacity capacity)
        {
            if (m_PropertyPortViewMap.TryGetValue(propertyPort.PortId, out PropertyPortView existingView))
            {
                existingView.SetPropertyPort(propertyPort);
                existingView.portName = portName;
                return existingView;
            }

            PropertyPortView propertyPortView = PropertyPortView.Create<PropertyEdgeView>(propertyPort, Orientation.Horizontal, capacity);
            propertyPortView.portName = portName;
            propertyPortView.name = "propertyPort";
            Add(propertyPortView);
            m_PropertyPortViewMap.Add(propertyPort.PortId, propertyPortView);
            return propertyPortView;
        }
        public void RemovePropertyPort(PropertyPort propertyPort)
        {
            if (!m_PropertyPortViewMap.TryGetValue(propertyPort.PortId, out PropertyPortView propertyPortView))
                return;

            m_NodeView.TreeView.DeleteElements(propertyPortView.connections);
            Remove(propertyPortView);
            m_PropertyPortViewMap.Remove(propertyPort.PortId);
        }
        public VariablePropertyPortView AddVariablePropertyPort(PropertyPort propertyPort, string portName, string acceptableTypesMethodName,Type[] acceptableTypes, Port.Capacity capacity)
        {
            if (m_PropertyPortViewMap.TryGetValue(propertyPort.PortId, out PropertyPortView existingView))
            {
                if (existingView is VariablePropertyPortView existingVariableView)
                {
                    existingVariableView.SetPropertyPort(propertyPort);
                    existingVariableView.portName = portName;
                    return existingVariableView;
                }
                RemovePropertyPort(existingView.PropertyPort);
            }

            VariablePropertyPortView variablePropertyPortView = VariablePropertyPortView.Create<PropertyEdgeView>(propertyPort, acceptableTypesMethodName, acceptableTypes, Orientation.Horizontal, capacity);
            variablePropertyPortView.portName = portName;
            variablePropertyPortView.name = "variablePropertyPort";
            Add(variablePropertyPortView);
            m_PropertyPortViewMap.Add(propertyPort.PortId, variablePropertyPortView);
            return variablePropertyPortView;
        }


        public void Sort()
        {
            foreach (var propertyPortViewPair in m_PropertyPortViewMap)
            {
                Remove(propertyPortViewPair.Value);
            }
            m_PropertyPortViewMap = m_PropertyPortViewMap.OrderBy(i => i.Value.PortIndex).ToDictionary(i => i.Key, i => i.Value);
            foreach (var propertyPortViewPair in m_PropertyPortViewMap)
            {
                Add(propertyPortViewPair.Value);
            }
            m_NodeView.NodeInputFieldContainer.Sort();
        }
        public void Sort(Dictionary<string, (PropertyPortView, float)> propertyPortViewMap)
        {
            foreach (var propertyPortViewPair in m_PropertyPortViewMap)
            {
                Remove(propertyPortViewPair.Value);
            }
            m_PropertyPortViewMap = m_PropertyPortViewMap.OrderBy(i => propertyPortViewMap[i.Key].Item2).ToDictionary(i => i.Key, i => i.Value);
            foreach (var propertyPortViewPair in m_PropertyPortViewMap)
            {
                Add(propertyPortViewPair.Value);
            }
            m_NodeView.NodeInputFieldContainer.Sort();
        }
        public void Update()
        {
            if (m_PropertyPortViewMap.Where(i => !i.Value.ClassListContains("hidden")).Count() <= 1)
            {
                foreach (var propertyPortViewPair in m_PropertyPortViewMap)
                {
                    propertyPortViewPair.Value.PortHandle.style.display = DisplayStyle.None;
                }
            }
            else
            {
                foreach (var propertyPortViewPair in m_PropertyPortViewMap)
                {
                    propertyPortViewPair.Value.PortHandle.style.display = DisplayStyle.Flex;
                }
            }
        }
    }
}
