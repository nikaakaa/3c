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
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new VisualElement();
            root.Add(new Button(() => TimelineEditorWindow.Open((TimelineAsset)target))
            {
                text = "Open Timeline Editor"
            });
            SerializedProperty data = serializedObject.FindProperty("m_Data");
            PropertyField field = new PropertyField(data, "Timeline Data");
            field.BindProperty(data);
            root.Add(field);
            var errors = new List<string>();
            TimelineAsset asset = (TimelineAsset)target;
            if (!asset.ValidateContent(TimelineTreeContractComposition.Create(), errors))
                root.Add(new HelpBox(string.Join("\n", errors), HelpBoxMessageType.Error));
            return root;
        }
    }
}
