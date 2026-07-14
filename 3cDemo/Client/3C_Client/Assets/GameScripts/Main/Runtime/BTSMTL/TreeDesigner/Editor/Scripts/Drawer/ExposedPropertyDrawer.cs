using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TreeDesigner.Editor
{
    //[CustomPropertyDrawer(typeof(BaseExposedProperty), true)]
    public class ExposedPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            List<string> propertyNames = new List<string>();
            List<GUIContent> guiContents = new List<GUIContent>();


            propertyNames.Add("Empty");
            guiContents.Add(new GUIContent("Empty"));

            EditorGUILayout.Popup(0, guiContents.ToArray());
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 50;
        }
    }
}