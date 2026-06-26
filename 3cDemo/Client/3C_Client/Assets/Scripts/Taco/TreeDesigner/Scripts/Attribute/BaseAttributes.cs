using System;
using UnityEngine;
using UnityEngine.Search;

namespace TreeDesigner
{
    #region Node
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NodeNameAttribute : Attribute
    {
        string m_Name;
        public string Name => m_Name;

        public NodeNameAttribute(string name)
        {
            m_Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NodePathAttribute : Attribute
    {
        string m_Path;
        public string Path => m_Path;

        public NodePathAttribute(string path)
        {
            m_Path = path;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class NodeColorAttribute : Attribute
    {
        Color m_Color;
        public Color Color => m_Color;

        public NodeColorAttribute(float r, float g, float b)
        {
            m_Color = new Color(r, g, b, 255);
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class NodeViewAttribute : Attribute
    {
        string m_NodeViewTypeName;
        public string NodeViewTypeName => m_NodeViewTypeName;

        public NodeViewAttribute(string nodeViewTypeName)
        {
            m_NodeViewTypeName = nodeViewTypeName;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class InputAttribute : Attribute
    {
        string m_Name;
        public string Name => m_Name;

        public InputAttribute(string inputName)
        {
            m_Name = inputName;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class OutputAttribute : Attribute
    {
        string m_Name;
        public string Name => m_Name;

        PortCapacity m_Capacity;
        public PortCapacity Capacity => m_Capacity;

        public OutputAttribute(string inputName, PortCapacity portCapacity)
        {
            m_Name = inputName;
            m_Capacity = portCapacity;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ShowInPanelAttribute : Attribute
    {
        string m_Label;
        public string Label => m_Label;

        public ShowInPanelAttribute(string label = null)
        {
            m_Label = label;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class PropertyPortAttribute : Attribute
    {
        PortDirection m_Direction;
        public PortDirection Direction => m_Direction;

        string m_Name;
        public string Name => m_Name;

        int m_Priority;
        public int Priority => m_Priority;

        public PropertyPortAttribute(PortDirection direction, string name, int priority = 0)
        {
            m_Name = name;
            m_Direction = direction;
            m_Priority = priority;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class VariablePropertyPortAttribute : Attribute
    {
        PortDirection m_Direction;
        public PortDirection Direction => m_Direction;

        string m_Name;
        public string Name => m_Name;

        int m_Priority;
        public int Priority => m_Priority;

        Type[] m_AcceptableTypes;
        public Type[] AcceptableTypes => m_AcceptableTypes;

        string m_AcceptableTypesMethodName;
        public string AcceptableTypesMethodName => m_AcceptableTypesMethodName;

        public VariablePropertyPortAttribute(PortDirection direction, string name, params Type[] acceptableTypes)
        {
            m_Direction = direction;
            m_Name = name;
            m_Priority = -1;
            m_AcceptableTypes = acceptableTypes;
        }
        public VariablePropertyPortAttribute(PortDirection direction, string name, int priority, params Type[] acceptableTypes)
        {
            m_Name = name;
            m_Direction = direction;
            m_Priority = priority;
            m_AcceptableTypes = acceptableTypes;
        }
        public VariablePropertyPortAttribute(PortDirection direction, string name, string acceptableTypesMethodName, int priority = 0)
        {
            m_Direction = direction;
            m_Name = name;
            m_AcceptableTypesMethodName = acceptableTypesMethodName;
            m_Priority = priority;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CompatiblePortsAttribute : Attribute
    {
        Type[] m_CompatibleTypes;
        public Type[] CompatibleTypes => m_CompatibleTypes;

        public CompatiblePortsAttribute(params Type[] compatibleTypes)
        {
            m_CompatibleTypes = compatibleTypes;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class PropertyColorAttribute : Attribute
    {
        Color m_Color;
        public Color Color => m_Color;

        public PropertyColorAttribute(float r, float g, float b)
        {
            m_Color = new Color(r, g, b, 255);
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class RefreshPropertyPortAttribute : Attribute
    {
        public RefreshPropertyPortAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class PropertyPortOnLinkedAttribute : Attribute
    {
        string m_CallbackName;
        public string CallbackName => m_CallbackName;
        public PropertyPortOnLinkedAttribute(string callbackName)
        {
            m_CallbackName = callbackName;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class PropertyPortOnUnlinkedAttribute : Attribute
    {
        string m_CallbackName;
        public string CallbackName => m_CallbackName;
        public PropertyPortOnUnlinkedAttribute(string callbackName)
        {
            m_CallbackName = callbackName;
        }
    }
    #endregion

    #region Tree
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class TreeWindowAttribute : Attribute
    {
        string m_Label;
        public string Label => m_Label;

        public TreeWindowAttribute(string label)
        {
            m_Label = label;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class AcceptableNodePathsAttribute : Attribute
    {
        string[] m_AcceptableNodePaths;
        public string[] AcceptableNodePaths => m_AcceptableNodePaths;

        public AcceptableNodePathsAttribute(params string[] acceptableNodePaths)
        {
            m_AcceptableNodePaths = acceptableNodePaths;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class AcceptableSubTreeTypeAttribute : Attribute
    {
        Type[] m_AcceptableSubTreeTypes;
        public Type[] AcceptableSubTreeTypes => m_AcceptableSubTreeTypes;

        public AcceptableSubTreeTypeAttribute(params Type[] acceptableSubTreeType)
        {
            m_AcceptableSubTreeTypes = acceptableSubTreeType;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ShowInInspectorAttribute : Attribute
    {
        string m_Label;
        public string Label => m_Label;

        int m_Priority;
        public int Priority => m_Priority;

        public ShowInInspectorAttribute(string label = null, int priority = 0)
        {
            m_Label = label;
            m_Priority = priority;
        }
    }
    #endregion

    #region Common
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ReadOnlyAttribute : PropertyAttribute 
    {
        public ReadOnlyAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class ShowIfAttribute : PropertyAttribute
    {
        string m_Name;
        public string Name => m_Name;

        object[] m_Conditions;
        public object[] Conditions => m_Conditions;

        public ShowIfAttribute(string name, params object[] conditions)
        {
            m_Name = name;
            m_Conditions = conditions;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class OnValueChangedAttribute : PropertyAttribute
    {
        string m_CallbackName;
        public string CallbackName => m_CallbackName;

        public OnValueChangedAttribute(string callbackName = "")
        {
            m_CallbackName = callbackName;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class EnumMenuAttribute : PropertyAttribute
    {
        string m_Label;
        public string Label => m_Label;

        string m_CallbackName;
        public string CallbackName => m_CallbackName;

        public EnumMenuAttribute(string label, string callbackName) 
        {
            m_Label = label;
            m_CallbackName = callbackName;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ToggleAttribute : PropertyAttribute
    {
        string m_Label;
        public string Label => m_Label;

        string m_CallbackName;
        public string CallbackName => m_CallbackName;

        public ToggleAttribute(string label, string callbackName)
        {
            m_Label = label;
            m_CallbackName = callbackName;
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ButtonAttribute : Attribute
    {
        public ButtonAttribute() { }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class CustomSearchContext : SearchContextAttribute
    {
        public CustomSearchContext(Type type):base($"t:{type.Name}")
        {

        }
    }
    #endregion
}