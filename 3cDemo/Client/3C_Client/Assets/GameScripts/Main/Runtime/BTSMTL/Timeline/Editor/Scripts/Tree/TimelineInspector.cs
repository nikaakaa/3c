using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BTSMTL.Timeline.Editor
{
    [CustomEditor(typeof(TimelineAsset))]
    public sealed class TimelineInspector : UnityEditor.Editor
    {
        VisualElement m_Root;

        void OnEnable()
        {
            TimelineInspectorSelection.Changed += Rebuild;
        }

        void OnDisable()
        {
            TimelineInspectorSelection.Changed -= Rebuild;
        }

        public override VisualElement CreateInspectorGUI()
        {
            m_Root = new VisualElement();
            Rebuild();
            return m_Root;
        }

        void Rebuild()
        {
            if (m_Root == null)
                return;
            m_Root.Clear();
            m_Root.Add(new Button(() => TimelineEditorWindow.Open((TimelineAsset)target))
            {
                text = "Open Timeline Editor"
            });
            SerializedProperty data = serializedObject.FindProperty("m_Data");
            PropertyField field = new PropertyField(data, "Timeline Data");
            field.BindProperty(data);
            m_Root.Add(field);

            TimelineAsset asset = (TimelineAsset)target;
            if (TimelineInspectorSelection.TryGet(asset, out string selectedPath))
            {
                SerializedProperty selected = serializedObject.FindProperty(selectedPath);
                if (selected != null)
                {
                    m_Root.Add(new Label("Selected Timeline Element"));
                    PropertyField selectedField = new PropertyField(selected, selected.displayName);
                    selectedField.BindProperty(selected);
                    m_Root.Add(selectedField);
                }
            }

            var errors = new List<string>();
            if (!asset.ValidateContent(TimelineTreeContractComposition.Create(), errors))
                m_Root.Add(new HelpBox(string.Join("\n", errors), HelpBoxMessageType.Error));
        }
    }
}
