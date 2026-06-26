#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Taco;

namespace TreeDesigner
{
    public static partial class PropertyPortUtility
    {
        static Dictionary<Type, PropertyPort> s_PropertyPortTypeMap = new Dictionary<Type, PropertyPort>();
        public static Dictionary<Type, PropertyPort> PropertyPortTypeMap => s_PropertyPortTypeMap;

        static Dictionary<Type, Type> s_TargetTypeMap = new Dictionary<Type, Type>();
        public static Dictionary<Type, Type> TargetTypeMap => s_TargetTypeMap;

        static PropertyPortUtility()
        {
            BuildScriptCache();
        }

        static void BuildScriptCache()
        {
            foreach (var propertyPortType in TypeCache.GetTypesDerivedFrom<PropertyPort>())
            {
                AddPropertyPortInstance(propertyPortType);
            }
        }
        static void AddPropertyPortInstance(Type type)
        {
            if (!type.IsAbstract && !type.IsGenericType)
            {
                PropertyPort propertyPort = Activator.CreateInstance(type) as PropertyPort;
                s_PropertyPortTypeMap[type] = propertyPort;
                s_TargetTypeMap[type] = propertyPort.GetField("m_Value").FieldType;
            }
        }

        public static Type GetElementPropertyPortType(this Type type)
        {
            Type elementType = s_PropertyPortTypeMap[type].GetField("m_Value").FieldType.GetGenericArguments()[0];
            foreach (var propertyPortTypePair in s_PropertyPortTypeMap)
            {
                if (propertyPortTypePair.Value.GetField("m_Value").FieldType == elementType)
                    return propertyPortTypePair.Key;
            }
            return null;
        }
        public static Type GetListPropertyPortType(this Type type)
        {
            Type elementType = s_PropertyPortTypeMap[type].GetField("m_Value").FieldType;
            foreach (var propertyPortTypePair in s_PropertyPortTypeMap)
            {
                Type fieldType = propertyPortTypePair.Value.GetField("m_Value").FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericArguments()[0] == elementType)
                    return propertyPortTypePair.Key;
            }
            return null;
        }
        public static Type TargetType(this Type type)
        {
            if (s_TargetTypeMap.TryGetValue(type, out Type targetType))
                return targetType;
            return null;
        }
        public static Color Color(this PropertyPort propertyPort)
        {
            PropertyColorAttribute propertyColorAttribute = propertyPort.GetAttribute<PropertyColorAttribute>();
            if(propertyColorAttribute != null)
                return propertyColorAttribute.Color / 255f;
            else
                return new Color(210, 210, 210, 255) / 255;
        }
    }
}
#endif