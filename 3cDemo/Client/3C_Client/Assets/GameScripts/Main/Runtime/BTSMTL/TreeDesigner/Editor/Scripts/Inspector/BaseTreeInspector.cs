using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using BTSMTL;

namespace TreeDesigner.Editor
{
    [CustomEditor(typeof(BaseTreeAsset), true)]
    public class BaseTreeInspector : UnityEditor.Editor
    {
        BaseTreeAsset m_TreeAsset;
        BaseTree m_Tree;
        VisualElement m_ExposedPropertyContainer;
        Dictionary<FieldInfo, object> m_ValueMap = new Dictionary<FieldInfo, object>();

        public override VisualElement CreateInspectorGUI()
        {
            m_TreeAsset = target as BaseTreeAsset;
            m_Tree = m_TreeAsset ? m_TreeAsset.Tree : null;

            VisualElement root = new VisualElement();
            root.name = "root";
            root.AddToClassList("treeInspector");

            var visualTree = Resources.Load<VisualTreeAsset>("VisualTree/BaseTreeInspectorOutside");
            visualTree.CloneTree(root);

            Label openButton = root.Q<Label>("open-tree-button");
            openButton.AddManipulator(new Clickable(() => TreeWindowUtility.OpenTree(m_TreeAsset)));

            VisualElement propertyContainer = root.Q("property-container");
            PopulateAuthoringProperties(m_Tree, propertyContainer, m_ValueMap);

            m_ExposedPropertyContainer = root.Q("exposed-property-container");
            PopulateExposedProperties();
            m_Tree.OnExposedPropertyChanged += PopulateExposedProperties;
            return root;
        }
        void OnDisable()
        {
            if (m_Tree != null)
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
                    CreateExposedPropertyField(i);
                }
            });
        }
        void CreateExposedPropertyField(BaseExposedProperty exposedProperty)
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

        public static void PopulateAuthoringProperties(BaseTree tree, VisualElement container, Dictionary<FieldInfo, object> valueMap)
        {
            container.Clear();
            valueMap.Clear();
            if (tree == null)
                return;

            SerializedObject serializedTree = tree.GetSerializedTree();
            foreach (var field in tree.GetAllFields())
            {
                if (field.IsStatic || field.IsNotSerialized || !tree.IsShow(field.Name))
                    continue;

                var showInInspectorAttributes = field.GetCustomAttributes(typeof(ShowInInspectorAttribute), false);
                var readOnlyAttributes = field.GetCustomAttributes(typeof(ReadOnlyAttribute), false);
                if (showInInspectorAttributes.Length == 0 || readOnlyAttributes.Length > 0)
                    continue;

                if (serializedTree.FindProperty(tree.GetSerializedPropertyPath(field.Name)) is SerializedProperty serializedProperty &&
                    showInInspectorAttributes[0] is ShowInInspectorAttribute showInInspectorAttribute)
                {
                    var onValueChangedAttributes = field.GetCustomAttributes(typeof(OnValueChangedAttribute), false);
                    PropertyField propertyField = new PropertyField(serializedProperty, showInInspectorAttribute.Label);
                    if(onValueChangedAttributes.Length > 0 && onValueChangedAttributes[0] is OnValueChangedAttribute onValueChangedAttribute)
                    {
                        valueMap.Add(field, field.GetValue(tree));

                        propertyField.RegisterValueChangeCallback(i =>
                        {
                            if (!object.Equals(field.GetValue(tree), valueMap[field]))
                            {
                                valueMap[field] = field.GetValue(tree);

                                MethodInfo methodInfo = tree.GetMethod(onValueChangedAttribute.CallbackName);
                                methodInfo?.Invoke(tree, null);
                                PopulateAuthoringProperties(tree, container, valueMap);
                            }
                        });
                    }

                    propertyField.Bind(serializedProperty.serializedObject);
                    propertyField.name = showInInspectorAttribute.Priority.ToString();
                    container.Add(propertyField);
                }
            }

            List<VisualElement> children = container.Children().ToList();
            children = children.OrderBy(i => int.Parse(i.name)).ToList();
            container.Clear();
            children.ForEach(i => container.Add(i));
        }
    }
}
