using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Taco.Editor;

namespace Taco.Timeline.Editor
{
    [CustomEditor(typeof(Timeline), true)]
    public class TimelineInspector : UnityEditor.Editor
    {
        Timeline m_Timeline;

        public override VisualElement CreateInspectorGUI()
        {
            m_Timeline = target as Timeline;
            VisualElement root = new VisualElement();

            List<VisualElement> visualElements = new List<VisualElement>();
            Dictionary<string, (VisualElement, List<VisualElement>)> groupMap = new Dictionary<string, (VisualElement, List<VisualElement>)>();

            foreach (var fieldInfo in target.GetAllFields())
            {
                if (fieldInfo.GetCustomAttribute<ShowInInspectorAttribute>() is ShowInInspectorAttribute showInInspectorAttribute)
                {
                    if (!fieldInfo.ShowIf(target))
                        continue;

                    if (fieldInfo.HideIf(target))
                        continue;

                    SerializedProperty sp = serializedObject.FindProperty(fieldInfo.Name);
                    if (sp != null)
                    {
                        PropertyField propertyField = new PropertyField(sp);
                        propertyField.name = showInInspectorAttribute.Index * 10 + visualElements.Count.ToString();
                        propertyField.Bind(serializedObject);

                        fieldInfo.Group(propertyField, showInInspectorAttribute.Index, ref visualElements, ref groupMap);

                        if (fieldInfo.ReadOnly(target))
                            propertyField.SetEnabled(false);

                        if (fieldInfo.GetCustomAttribute<OnValueChangedAttribute>() is OnValueChangedAttribute onValueChanged)
                        {
                            EditorCoroutineHelper.Delay(() =>
                            {
                                propertyField.RegisterValueChangeCallback((e) =>
                                {
                                    foreach (var method in onValueChanged.Methods)
                                    {
                                        target.GetMethod(method)?.Invoke(target, null);
                                    }
                                });
                            }, 0.01f);
                        }
                    }
                }
            }
            foreach (var propertyInfo in target.GetAllProperties())
            {
                if (!propertyInfo.ShowIf(target))
                    continue;

                if (propertyInfo.HideIf(target))
                    continue;

                if (propertyInfo.GetCustomAttribute<ShowTextAttribute>() is ShowTextAttribute showTextAttribute)
                {
                    IMGUIContainer container = new IMGUIContainer(() =>
                    {
                        GUILayout.Label(propertyInfo.GetValue(target).ToString());
                    });
                    container.name = showTextAttribute.Index * 10 + visualElements.Count.ToString();
                    propertyInfo.Group(container, showTextAttribute.Index, ref visualElements, ref groupMap);
                }
            }
            foreach (var methodInfo in target.GetAllMethods())
            {
                if (!methodInfo.ShowIf(target))
                    continue;

                if (methodInfo.HideIf(target))
                    continue;

                if (methodInfo.GetCustomAttribute<ShowTextAttribute>() is ShowTextAttribute showTextAttribute)
                {
                    IMGUIContainer container = new IMGUIContainer(() =>
                    {
                        GUILayout.Label(methodInfo.Invoke(target, null).ToString());
                    });
                    container.name = showTextAttribute.Index * 10 + visualElements.Count.ToString();
                    methodInfo.Group(container, showTextAttribute.Index, ref visualElements, ref groupMap);
                }

                if (methodInfo.GetCustomAttribute<ButtonAttribute>() is ButtonAttribute buttonAttribute)
                {
                    Button button = new Button();
                    button.name = buttonAttribute.Index * 10 + visualElements.Count.ToString();
                    button.text = string.IsNullOrEmpty(buttonAttribute.Label) ? methodInfo.Name : buttonAttribute.Label;
                    button.clicked += () => methodInfo.Invoke(target, null);
                    methodInfo.Group(button, buttonAttribute.Index, ref visualElements, ref groupMap);
                }
            }

            foreach (var visualElement in visualElements.OrderBy(i => float.Parse(i.name)))
            {
                visualElement.AddToClassList("inspectorElement");
                root.Add(visualElement);
            }
            foreach (var groupPair in groupMap)
            {
                foreach (var groupElement in groupPair.Value.Item2.OrderBy(i => float.Parse(i.name)))
                {
                    groupElement.AddToClassList("inspectorElement");
                    groupPair.Value.Item1.Add(groupElement);
                }
            }

            return root;
        }
    }
}