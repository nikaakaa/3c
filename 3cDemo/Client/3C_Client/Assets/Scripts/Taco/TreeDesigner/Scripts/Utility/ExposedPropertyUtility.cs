#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Taco;

namespace TreeDesigner
{
    public static partial class ExposedPropertyUtility
    {
        static Dictionary<Type, BaseExposedProperty> s_ExposedPropertyTypeMap = new Dictionary<Type, BaseExposedProperty>();
        public static Dictionary<Type, BaseExposedProperty> ExposedPropertyTypeMap => s_ExposedPropertyTypeMap;

        static Dictionary<Type, Type> s_TargetTypeMap = new Dictionary<Type, Type>();
        public static Dictionary<Type, Type> TargetTypeMap => s_TargetTypeMap;

        static ExposedPropertyUtility()
        {
            BuildScriptCache();
        }

        static void BuildScriptCache()
        {
            foreach (var exposedPropertyType in TypeCache.GetTypesDerivedFrom<BaseExposedProperty>())
            {
                AddExposedPropertyInstance(exposedPropertyType);
            }
        }
        static void AddExposedPropertyInstance(Type type)
        {
            if (!type.IsAbstract)
            {
                BaseExposedProperty exposedProperty = Activator.CreateInstance(type) as BaseExposedProperty;
                s_ExposedPropertyTypeMap[type] = exposedProperty;
                s_TargetTypeMap[type] = exposedProperty.GetField("m_Value").FieldType;
            }
        }

        public static Type TargetType(this Type type)
        {
            if (s_TargetTypeMap.TryGetValue(type, out Type targetType))
                return targetType;
            return null;
        }
        public static Color Color(this BaseExposedProperty exposedProperty)
        {
            PropertyColorAttribute propertyColorAttribute = exposedProperty.GetAttribute<PropertyColorAttribute>();
            if (propertyColorAttribute != null)
                return propertyColorAttribute.Color / 255f;
            else
                return new Color(210, 210, 210, 255) / 255;
        }
    }
}
#endif