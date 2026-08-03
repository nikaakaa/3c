#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BTSMTL;

namespace TreeDesigner
{
    public struct FlowPortDeclaration
    {
        public string Name { get; }
        public PortDirection Direction { get; }
        public PortCapacity Capacity { get; }

        public FlowPortDeclaration(string name, PortDirection direction, PortCapacity capacity)
        {
            Name = name;
            Direction = direction;
            Capacity = capacity;
        }
    }

    public abstract partial class BaseNode
    {
        [SerializeField]
        protected bool m_Expanded;
        public bool Expanded { get => m_Expanded; set => m_Expanded = value; }

        [SerializeField]
        protected bool m_ShowPanel;
        public bool ShowPanel { get => m_ShowPanel; set => m_ShowPanel = value; }

        [SerializeField]
        protected Vector2 m_Position;
        public Vector2 Position { get => m_Position; set => m_Position = value; }

        [NonSerialized]
        protected Action m_OnNodeChanged;
        public Action OnNodeChanged { get => m_OnNodeChanged; set => m_OnNodeChanged = value; }

        public virtual NodeCapabilities Capabilities => NodeCapabilities.Selectable |
                                                        NodeCapabilities.Movable |
                                                        NodeCapabilities.Deletable |
                                                        NodeCapabilities.Ascendable |
                                                        NodeCapabilities.Copiable |
                                                        NodeCapabilities.Snappable |
                                                        NodeCapabilities.Groupable;
        public virtual bool Single => false;

        public virtual IEnumerable<FlowPortDeclaration> GetFlowPortDeclarations(BaseGraph owner)
        {
            foreach (var inputAttribute in GetType().GetCustomAttributes(typeof(InputAttribute), true).Cast<InputAttribute>())
                yield return new FlowPortDeclaration(inputAttribute.Name, PortDirection.Input, PortCapacity.Single);

            foreach (var outputAttribute in GetType().GetCustomAttributes(typeof(OutputAttribute), true).Cast<OutputAttribute>())
                yield return new FlowPortDeclaration(outputAttribute.Name, PortDirection.Output, outputAttribute.Capacity);
        }

        public virtual IEnumerable<FlowPortDeclaration> GetSupportedFlowPortDeclarations(BaseGraph owner)
        {
            return GetFlowPortDeclarations(owner);
        }

        public virtual bool Refresh()
        {
            bool isDirty = false;

            List<PropertyPort> inputPropertyPorts = new List<PropertyPort>();
            List<PropertyPort> outPropertyPorts = new List<PropertyPort>();

            foreach (var accessor in GetFieldAccessors())
            {
                if (accessor.TryGetPropertyPort(out PropertyPort propertyPort))
                {
                    string portId = propertyPort.PortId;
                    string displayName = propertyPort.DisplayName;
                    string legacyName = propertyPort.Name;
                    ConfigurePropertyPort(accessor, propertyPort, null);
                    if (portId != propertyPort.PortId || displayName != propertyPort.DisplayName || legacyName != propertyPort.Name)
                        isDirty = true;

                    var propertyPortAttributes = accessor.GetAttributes<PropertyPortAttribute>();
                    if (propertyPortAttributes.Count() > 0)
                    {
                        PropertyPortAttribute propertyPortAttribute = propertyPortAttributes.ElementAt(0);
                        switch (propertyPort.Direction)
                        {
                            case PortDirection.Input:
                                inputPropertyPorts.Add(propertyPort);
                                break;
                            case PortDirection.Output:
                                outPropertyPorts.Add(propertyPort);
                                break;
                        }
                        if (propertyPort.Index == -1)
                        {
                            isDirty = true;
                            propertyPort.Index = propertyPortAttribute.Priority;
                        }
                    }

                    var variablePropertyPortAttributes = accessor.GetAttributes<VariablePropertyPortAttribute>();
                    if (variablePropertyPortAttributes.Count() > 0)
                    {
                        VariablePropertyPortAttribute variablePropertyPortAttribute = variablePropertyPortAttributes.ElementAt(0);
                        switch (propertyPort.Direction)
                        {
                            case PortDirection.Input:
                                inputPropertyPorts.Add(propertyPort);
                                break;
                            case PortDirection.Output:
                                outPropertyPorts.Add(propertyPort);
                                break;
                        }
                        if (propertyPort.Index == -1)
                        {
                            isDirty = true;
                            propertyPort.Index = variablePropertyPortAttribute.Priority;
                        }
                    }
                }
                else if (accessor.TryGetPropertyPortList(out List<PropertyPort> propertyPorts))
                {
                    foreach (var listPropertyPort in propertyPorts)
                    {
                        if (listPropertyPort == null)
                            continue;

                        string portId = listPropertyPort.PortId;
                        string displayName = listPropertyPort.DisplayName;
                        string legacyName = listPropertyPort.Name;
                        ConfigurePropertyPort(accessor, listPropertyPort, listPropertyPort.Name);
                        if (portId != listPropertyPort.PortId || displayName != listPropertyPort.DisplayName || legacyName != listPropertyPort.Name)
                            isDirty = true;

                        switch (listPropertyPort.Direction)
                        {
                            case PortDirection.Input:
                                inputPropertyPorts.Add(listPropertyPort);
                                break;
                            case PortDirection.Output:
                                outPropertyPorts.Add(listPropertyPort);
                                break;
                        }
                    }
                }
            }

            inputPropertyPorts = inputPropertyPorts.OrderBy(i => i.Index).ToList();
            outPropertyPorts = outPropertyPorts.OrderBy(i => i.Index).ToList();
            for (int i = 0; i < inputPropertyPorts.Count; i++)
            {
                inputPropertyPorts[i].Index = i;
            }
            for (int i = 0; i < outPropertyPorts.Count; i++)
            {
                outPropertyPorts[i].Index = i;
            }

            return isDirty;
        }
        public virtual void OnInputLinked(BaseEdge edge) { }
        public virtual void OnInputUnlinked(BaseEdge edge) { }
        public virtual void OnOutputLinked(BaseEdge edge) { }
        public virtual void OnOutputUnlinked(BaseEdge edge) { }

        public virtual void OnInputPropertyLinked(PropertyEdge propertyEdge) { }
        public virtual void OnInputPropertyUnLinked(PropertyEdge propertyEdge) { }
        public virtual void OnOutputPropertyLinked(PropertyEdge propertyEdge) { }
        public virtual void OnOutputPropertyUnLinked(PropertyEdge propertyEdge) { }
        public virtual void OnMoved() { }

        public virtual PropertyPort SetPropertyPort(string propertyPortName, Type propertyPortType, PortDirection direction)
        {
            var accessor = FindFieldAccessor(propertyPortName);
            if (accessor == null)
                return null;

            PropertyPort propertyPort = accessor.Value.GetValue() as PropertyPort;
            if (propertyPort.GetType() != propertyPortType)
            {
                propertyPort = Activator.CreateInstance(propertyPortType) as PropertyPort;
                accessor.Value.SetValue(propertyPort);
            }
            propertyPort.Direction = direction;
            ConfigurePropertyPort(accessor.Value, propertyPort, null);
            m_PropertyPortMap[propertyPort.PortId] = propertyPort;
            propertyPort.Init(this);
            return propertyPort;
        }
        public virtual PropertyPort AddPropertyPort(string fieldName, string propertyPortName, Type propertyPortType, PortDirection direction)
        {
            PropertyPort propertyPort = Activator.CreateInstance(propertyPortType) as PropertyPort;
            propertyPort.Name = propertyPortName;
            propertyPort.Direction = direction;

            var accessor = FindFieldAccessor(fieldName);
            if (accessor == null)
                return null;

            List<PropertyPort> propertyPorts = accessor.Value.GetValue() as List<PropertyPort>;
            if (propertyPorts == null)
                return null;

            propertyPorts.Add(propertyPort);
            accessor.Value.SetValue(propertyPorts);

            ConfigurePropertyPort(accessor.Value, propertyPort, propertyPortName);
            m_PropertyPortMap.Add(propertyPort.PortId, propertyPort);
            propertyPort.Init(this);
            return propertyPort;
        }
        public virtual void RemovePropertyPort(string fieldName, PropertyPort propertyPort)
        {
            if (propertyPort == null)
                return;

            var accessor = FindFieldAccessor(fieldName);
            if (accessor == null)
                return;

            List<PropertyPort> propertyPorts = accessor.Value.GetValue() as List<PropertyPort>;
            if (propertyPorts != null && propertyPorts.Contains(propertyPort))
            {
                if (m_Owner != null)
                {
                    foreach (var propertyEdge in m_Owner.PropertyEdges
                                 .Where(i => (i.StartNode == this && i.StartPortName == propertyPort.PortId) ||
                                             (i.EndNode == this && i.EndPortName == propertyPort.PortId))
                                 .ToList())
                        m_Owner.UnLinkProperty(propertyEdge);
                }

                propertyPorts.Remove(propertyPort);
                accessor.Value.SetValue(propertyPorts);
                m_PropertyPortMap.Remove(propertyPort.PortId);
            }
        }

        public virtual void OnNodeChangedCallback()
        {
            m_OnNodeChanged?.Invoke();
        }
    }


    public partial class ForNode : DecoratorNode
    {
        public override void OnInputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_List":
                    if (!IsConnected("m_Element"))
                    {
                        SetPropertyPort("m_Element", propertyEdge.EndPort.GetType().GetElementPropertyPortType(), PortDirection.Output);
                    }
                    break;
            }
        }
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_List":
                    if (!IsConnected("m_Element"))
                    {
                        SetPropertyPort("m_List", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_Element", typeof(PropertyPort), PortDirection.Output);
                    }
                    break;
            }
        }
        public override void OnOutputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyLinked(propertyEdge);
            if (propertyEdge.StartPortName == "m_Element")
            {
                if (!IsConnected("m_List"))
                {
                    SetPropertyPort("m_List", propertyEdge.StartPort.GetType().GetListPropertyPortType(), PortDirection.Input);
                }
            }
        }
        public override void OnOutputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyUnLinked(propertyEdge);
            if (propertyEdge.StartPortName == "m_Element")
            {
                if (!IsConnected("m_List") && !IsConnected("m_Element"))
                {
                    SetPropertyPort("m_List", typeof(PropertyPort), PortDirection.Input);
                    SetPropertyPort("m_Element", typeof(PropertyPort), PortDirection.Output);
                }
            }
        }

        List<Type> AcceptableTypes(string name)
        {
            switch (name)
            {
                case "m_List":
                    return new List<Type> { typeof(List<>) };
                case "m_Element":
                    List<Type> acceptableTypes = new List<Type>();
                    foreach (var item in PropertyPortUtility.PropertyPortTypeMap)
                    {
                        if (!item.Value.ValueType.IsSubClassOfRawGeneric(typeof(List<>)))
                            acceptableTypes.Add(item.Value.ValueType);
                    }
                    return acceptableTypes;
                default:
                    return null;
            }
        }
    }

    public partial class ToListNode : ValueNode
    {
        public override void OnInputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_Element":
                    if (!IsConnected("m_List"))
                    {
                        SetPropertyPort("m_List", propertyEdge.EndPort.GetType().GetListPropertyPortType(), PortDirection.Output);
                    }
                    break;
            }
        }
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_Element":
                    if (!IsConnected("m_List"))
                    {
                        SetPropertyPort("m_Element", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_List", typeof(PropertyPort), PortDirection.Output);
                    }
                    break;
            }
        }
        public override void OnOutputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyLinked(propertyEdge);
            if (propertyEdge.StartPortName == "m_List")
            {
                if (!IsConnected("m_Element"))
                {
                    SetPropertyPort("m_Element", propertyEdge.StartPort.GetType().GetElementPropertyPortType(), PortDirection.Input);
                }
            }
        }
        public override void OnOutputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyUnLinked(propertyEdge);
            if (propertyEdge.StartPortName == "m_List")
            {
                if (!IsConnected("m_List") && !IsConnected("m_Element"))
                {
                    SetPropertyPort("m_Element", typeof(PropertyPort), PortDirection.Input);
                    SetPropertyPort("m_List", typeof(PropertyPort), PortDirection.Output);
                }
            }
        }

        List<Type> AcceptableTypes(string name)
        {
            switch (name)
            {
                case "m_Element":
                    List<Type> acceptableTypes = new List<Type>();
                    foreach (var item in PropertyPortUtility.PropertyPortTypeMap)
                    {
                        if (!item.Value.ValueType.IsSubClassOfRawGeneric(typeof(List<>)))
                            acceptableTypes.Add(item.Value.ValueType);
                    }
                    return acceptableTypes;
                case "m_List":
                    return new List<Type> { typeof(List<>) };
                default:
                    return null;
            }
        }
    }

    public partial class ToStringNode : ValueNode
    {
        public override string ToString()
        {
            return "ToString";
        }
    }

    public abstract partial class MathNode : ValueNode
    {
        public override void OnInputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_InputValue1":
                    if (!IsConnected("m_InputValue2"))
                        SetPropertyPort("m_InputValue2", propertyEdge.EndPort.GetType(), PortDirection.Input);
                    if (!IsConnected("m_OutputValue"))
                        SetPropertyPort("m_OutputValue", propertyEdge.EndPort.GetType(), PortDirection.Output);
                    break;
                case "m_InputValue2":
                    if (!IsConnected("m_InputValue1"))
                        SetPropertyPort("m_InputValue1", propertyEdge.EndPort.GetType(), PortDirection.Input);
                    if (!IsConnected("m_OutputValue"))
                        SetPropertyPort("m_OutputValue", propertyEdge.EndPort.GetType(), PortDirection.Output);
                    break;
            }
        }
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_InputValue1":
                    if (!IsConnected("m_InputValue2") && !IsConnected("m_OutputValue"))
                    {
                        SetPropertyPort("m_InputValue1", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_InputValue2", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_OutputValue", typeof(PropertyPort), PortDirection.Output);
                    }
                    break;
                case "m_InputValue2":
                    if (!IsConnected("m_InputValue1") && !IsConnected("m_OutputValue"))
                    {
                        SetPropertyPort("m_InputValue1", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_InputValue2", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_OutputValue", typeof(PropertyPort), PortDirection.Output);
                    }
                    break;
            }
        }
        public override void OnOutputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyLinked(propertyEdge);
            if (propertyEdge.StartPortName == "m_OutputValue")
            {
                if (!IsConnected("m_InputValue1"))
                    SetPropertyPort("m_InputValue1", propertyEdge.EndPort.GetType(), PortDirection.Input);
                if (!IsConnected("m_InputValue2"))
                    SetPropertyPort("m_InputValue2", propertyEdge.EndPort.GetType(), PortDirection.Input);
            }
        }
        public override void OnOutputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnOutputPropertyUnLinked(propertyEdge);
            if (propertyEdge.StartPortName == "m_OutputValue")
            {
                if (!IsConnected("m_InputValue1") && !IsConnected("m_InputValue2") && !IsConnected("m_OutputValue"))
                {
                    SetPropertyPort("m_InputValue1", typeof(PropertyPort), PortDirection.Input);
                    SetPropertyPort("m_InputValue2", typeof(PropertyPort), PortDirection.Input);
                    SetPropertyPort("m_OutputValue", typeof(PropertyPort), PortDirection.Output);
                }
            }
        }
    }

    public partial class EqualNode : ValueNode
    {
        public override void OnInputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_InputValue1":
                    if (!IsConnected("m_InputValue2"))
                        SetPropertyPort("m_InputValue2", propertyEdge.EndPort.GetType(), PortDirection.Input);
                    break;
                case "m_InputValue2":
                    if (!IsConnected("m_InputValue1"))
                        SetPropertyPort("m_InputValue1", propertyEdge.EndPort.GetType(), PortDirection.Input);
                    break;
            }
        }
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_InputValue1":
                    if (!IsConnected("m_InputValue2") && !IsConnected("m_OutputValue"))
                    {
                        SetPropertyPort("m_InputValue1", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_InputValue2", typeof(PropertyPort), PortDirection.Input);
                    }
                    break;
                case "m_InputValue2":
                    if (!IsConnected("m_InputValue1") && !IsConnected("m_OutputValue"))
                    {
                        SetPropertyPort("m_InputValue1", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_InputValue2", typeof(PropertyPort), PortDirection.Input);
                    }
                    break;
            }
        }

        List<Type> AcceptableTypes(string name)
        {
            List<Type> acceptableTypes = new List<Type>();
            foreach (var item in PropertyPortUtility.PropertyPortTypeMap)
            {
                acceptableTypes.Add(item.Value.ValueType);
            }
            return acceptableTypes;
        }
    }

    public partial class ValidNode : ValueNode
    {
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            SetPropertyPort("m_InputValue", typeof(PropertyPort), PortDirection.Input);
        }

        List<Type> AcceptableTypes(string name)
        {
            List<Type> acceptableTypes = new List<Type>();
            foreach (var propertyPortTypePair in PropertyPortUtility.PropertyPortTypeMap)
            {
                if (propertyPortTypePair.Value.ValueType.IsClass)
                    acceptableTypes.Add(propertyPortTypePair.Value.ValueType);
            }
            return acceptableTypes;
        }
    }

    public partial class CompareNode : ValueNode
    {
        public override void OnInputPropertyLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_InputValue1":
                    if (!IsConnected("m_InputValue2"))
                        SetPropertyPort("m_InputValue2", propertyEdge.EndPort.GetType(), PortDirection.Input);
                    break;
                case "m_InputValue2":
                    if (!IsConnected("m_InputValue1"))
                        SetPropertyPort("m_InputValue1", propertyEdge.EndPort.GetType(), PortDirection.Input);
                    break;
            }
        }
        public override void OnInputPropertyUnLinked(PropertyEdge propertyEdge)
        {
            base.OnInputPropertyUnLinked(propertyEdge);
            switch (propertyEdge.EndPortName)
            {
                case "m_InputValue1":
                    if (!IsConnected("m_InputValue2") && !IsConnected("m_OutputValue"))
                    {
                        SetPropertyPort("m_InputValue1", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_InputValue2", typeof(PropertyPort), PortDirection.Input);
                    }
                    break;
                case "m_InputValue2":
                    if (!IsConnected("m_InputValue1") && !IsConnected("m_OutputValue"))
                    {
                        SetPropertyPort("m_InputValue1", typeof(PropertyPort), PortDirection.Input);
                        SetPropertyPort("m_InputValue2", typeof(PropertyPort), PortDirection.Input);
                    }
                    break;
            }
        }
    }
}
#endif
