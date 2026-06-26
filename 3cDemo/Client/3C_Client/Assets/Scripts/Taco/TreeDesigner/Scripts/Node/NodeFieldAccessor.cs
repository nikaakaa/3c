using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Taco;

namespace TreeDesigner
{
    public readonly struct NodeFieldAccessor
    {
        readonly BaseNode m_OwnerNode;
        readonly object m_TargetObject;
        readonly FieldInfo m_FieldInfo;
        readonly string m_FieldKey;
        readonly string m_SerializedPropertyPath;
        readonly string m_ModuleId;

        public BaseNode OwnerNode => m_OwnerNode;
        public object TargetObject => m_TargetObject;
        public FieldInfo FieldInfo => m_FieldInfo;
        public string FieldName => m_FieldInfo?.Name;
        public string FieldKey => m_FieldKey;
        public string SerializedPropertyPath => m_SerializedPropertyPath;
        public string ModuleId => m_ModuleId;
        public bool IsModuleField => !string.IsNullOrEmpty(m_ModuleId);

        public NodeFieldAccessor(BaseNode ownerNode, object targetObject, FieldInfo fieldInfo, string fieldKey, string serializedPropertyPath, string moduleId)
        {
            m_OwnerNode = ownerNode;
            m_TargetObject = targetObject;
            m_FieldInfo = fieldInfo;
            m_FieldKey = fieldKey;
            m_SerializedPropertyPath = serializedPropertyPath;
            m_ModuleId = moduleId;
        }

        public object GetValue()
        {
            return m_FieldInfo.GetValue(m_TargetObject);
        }

        public void SetValue(object value)
        {
            m_FieldInfo.SetValue(m_TargetObject, value);
        }

        public T GetAttribute<T>() where T : Attribute
        {
            return GetAttributes<T>().FirstOrDefault();
        }

        public IEnumerable<T> GetAttributes<T>() where T : Attribute
        {
            return m_FieldInfo.GetCustomAttributes<T>(true);
        }

        public bool TryGetPropertyPort(out PropertyPort propertyPort)
        {
            propertyPort = GetValue() as PropertyPort;
            return propertyPort != null;
        }

        public bool TryGetPropertyPortList(out List<PropertyPort> propertyPorts)
        {
            propertyPorts = GetValue() as List<PropertyPort>;
            return propertyPorts != null;
        }

        public bool IsShow()
        {
            var showIfAttributes = GetAttributes<ShowIfAttribute>().ToArray();
            foreach (var showIfAttribute in showIfAttributes)
            {
                if (!MatchShowIf(showIfAttribute))
                    return false;
            }
            return true;
        }

        public bool IsReadOnly()
        {
            return GetAttribute<ReadOnlyAttribute>() != null;
        }

        bool MatchShowIf(ShowIfAttribute showIfAttribute)
        {
            object value = null;
            if (m_TargetObject.GetField(showIfAttribute.Name) is FieldInfo fieldInfo)
                value = fieldInfo.GetValue(m_TargetObject);
            else if (m_TargetObject.GetProperty(showIfAttribute.Name) is PropertyInfo propertyInfo)
                value = propertyInfo.GetValue(m_TargetObject);
            else if (m_TargetObject.GetMethod(showIfAttribute.Name) is MethodInfo methodInfo)
                value = methodInfo.Invoke(m_TargetObject, null);
            else
                return true;

            foreach (var condition in showIfAttribute.Conditions)
            {
                if (Equals(value, condition))
                    return true;
            }
            return false;
        }
    }
}
