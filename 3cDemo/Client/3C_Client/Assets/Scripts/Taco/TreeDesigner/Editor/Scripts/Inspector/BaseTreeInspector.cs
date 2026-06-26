using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Taco;

namespace TreeDesigner.Editor
{
    [CustomEditor(typeof(BaseTree), true)]
    public class BaseTreeInspector : UnityEditor.Editor
    {
        BaseTree m_Tree;
        VisualElement m_ExposedPropertyContainer;
        Dictionary<FieldInfo, object> m_ValueMap = new Dictionary<FieldInfo, object>();

        public override VisualElement CreateInspectorGUI()
        {
            m_Tree = target as BaseTree;

            VisualElement root = new VisualElement();
            root.name = "root";
            root.AddToClassList("treeInspector");

            var visualTree = Resources.Load<VisualTreeAsset>("VisualTree/BaseTreeInspectorOutside");
            visualTree.CloneTree(root);

            Label openButton = root.Q<Label>("open-tree-button");
            openButton.AddManipulator(new Clickable(() => TreeWindowUtility.OpenTree(m_Tree)));

            VisualElement propertyContainer = root.Q("property-container");
            PopulateProperties(m_Tree, propertyContainer, m_ValueMap);

            m_ExposedPropertyContainer = root.Q("exposed-property-container");
            PopulateExposedProperties();
            m_Tree.OnExposedPropertyChanged += PopulateExposedProperties;
            return root;
        }
        void OnDisable()
        {
            if (m_Tree)
                m_Tree.OnExposedPropertyChanged -= PopulateExposedProperties;
        }

        protected override void OnHeaderGUI() { }

        void PopulateExposedProperties()
        {
            m_ExposedPropertyContainer.Clear();
            m_Tree.ExposedProperties.ForEach(i =>
            {
                if (i.ShowOutside)
                {
                    i.Init(m_Tree);
                    CreateExposedPropertyView(i);
                }
            });
        }
        void CreateExposedPropertyView(BaseExposedProperty exposedProperty)
        {
            VisualElement exposedPropertyView = new VisualElement();
            exposedPropertyView.name = "exposed-property";

            SerializedProperty serializedValueProperty = exposedProperty.GetExposedPropertySerializedProperty("m_Value");
            PropertyField exposedPropertyValue = new PropertyField(serializedValueProperty,exposedProperty.Name);
            exposedPropertyValue.name = "exposed-property-field";
            exposedPropertyValue.Bind(serializedValueProperty.serializedObject);
            exposedPropertyView.Add(exposedPropertyValue);
            if (!exposedProperty.CanEdit)
                exposedPropertyView.SetEnabled(false);
            
            m_ExposedPropertyContainer.Add(exposedPropertyView);
        }

        public static void PopulateProperties(BaseTree tree, VisualElement container, Dictionary<FieldInfo, object> valueMap)
        {
            container.Clear();
            valueMap.Clear();
            SerializedObject serializedTree = tree.GetSerializedTree();
            foreach (var field in tree.GetAllFields())
            {
                if (!tree.IsShow(field.Name))
                   continue;
                
                if (serializedTree.FindProperty(field.Name) is SerializedProperty serializedProperty)
                {
                    var showInInspectorAttributes = field.GetCustomAttributes(typeof(ShowInInspectorAttribute), false);
                    var onValueChangedAttributes = field.GetCustomAttributes(typeof(OnValueChangedAttribute), false);
                    if (showInInspectorAttributes.Length > 0 && showInInspectorAttributes[0] is ShowInInspectorAttribute showInInspectorAttribute)
                    {
                        PropertyField propertyField = new PropertyField(serializedProperty, showInInspectorAttribute.Label);
                        if(onValueChangedAttributes.Length > 0 && onValueChangedAttributes[0] is OnValueChangedAttribute onValueChangedAttribute)
                        {
                            valueMap.Add(field, field.GetValue(tree));

                            propertyField.RegisterValueChangeCallback(i =>
                            {
                                if (!field.GetValue(tree).Equals(valueMap[field]))
                                {
                                    valueMap[field] = field.GetValue(tree);

                                    MethodInfo methodInfo = tree.GetMethod(onValueChangedAttribute.CallbackName);
                                    methodInfo?.Invoke(tree, null);
                                    PopulateProperties(tree, container, valueMap);
                                }
                            });
                        }

                        propertyField.Bind(serializedProperty.serializedObject);
                        propertyField.name = showInInspectorAttribute.Priority.ToString();
                        container.Add(propertyField);

                        var readOnlyAttributes = field.GetCustomAttributes(typeof(ReadOnlyAttribute), false);
                        if (readOnlyAttributes.Length > 0 && readOnlyAttributes[0] is ReadOnlyAttribute readOnlyAttribute)
                            propertyField.SetEnabled(false);
                    }
                }
                else
                {
                    var showInInspectorAttributes = field.GetCustomAttributes(typeof(ShowInInspectorAttribute), false);
                    if (showInInspectorAttributes.Length > 0 && showInInspectorAttributes[0] is ShowInInspectorAttribute showInInspectorAttribute)
                    {
                        IMGUIContainer guiContainer = new IMGUIContainer(() =>
                        {
                            GUI.enabled = false;
                            switch (field.GetValue(tree))
                            {
                                case bool boolValue:
                                    EditorGUILayout.Toggle(showInInspectorAttribute.Label, boolValue);
                                    break;
                                case Enum enumValue:
                                    EditorGUILayout.EnumPopup(showInInspectorAttribute.Label, enumValue);
                                    break;
                                case int intValue:
                                    EditorGUILayout.IntField(showInInspectorAttribute.Label, intValue);
                                    break;
                                case float floatValue:
                                    EditorGUILayout.FloatField(showInInspectorAttribute.Label, floatValue);
                                    break;
                            }
                            GUI.enabled = true;
                        });
                        guiContainer.name = showInInspectorAttribute.Priority.ToString();
                        container.Add(guiContainer);
                    }
                }
            }
            foreach (var property in tree.GetAllProperties())
            {
                if (!tree.IsShow(property.Name))
                    continue;

                var showInInspectorAttributes = property.GetCustomAttributes(typeof(ShowInInspectorAttribute), false);
                if (showInInspectorAttributes.Length > 0 && showInInspectorAttributes[0] is ShowInInspectorAttribute showInInspectorAttribute)
                {
                    IMGUIContainer guiContainer = new IMGUIContainer(() =>
                    {
                        GUI.enabled = false;
                        switch (property.GetValue(tree))
                        {
                            case bool boolValue:
                                EditorGUILayout.Toggle(showInInspectorAttribute.Label, boolValue);
                                break;
                            case Enum enumValue:
                                EditorGUILayout.EnumPopup(showInInspectorAttribute.Label, enumValue);
                                break;
                            case int intValue:
                                EditorGUILayout.IntField(showInInspectorAttribute.Label, intValue);
                                break;
                            case float floatValue:
                                EditorGUILayout.FloatField(showInInspectorAttribute.Label, floatValue);
                                break;
                        }
                        GUI.enabled = true;
                    });
                    guiContainer.name = showInInspectorAttribute.Priority.ToString();
                    container.Add(guiContainer);
                }
            }
            foreach (var method in tree.GetAllMethods())
            {
                var buttonAttributes = method.GetCustomAttributes(typeof(ButtonAttribute), false);
                if (buttonAttributes.Length > 0 && buttonAttributes[0] is ButtonAttribute buttonAttribute)
                {
                    Button button = new Button();
                    button.name = int.MaxValue.ToString();
                    button.text = method.Name;
                    button.clicked += () => method.Invoke(tree, null);
                    container.Add(button);
                }
            }

            List<VisualElement> children = container.Children().ToList();
            children = children.OrderBy(i => int.Parse(i.name)).ToList();
            container.Clear();
            children.ForEach(i => container.Add(i));
        }
    }
}