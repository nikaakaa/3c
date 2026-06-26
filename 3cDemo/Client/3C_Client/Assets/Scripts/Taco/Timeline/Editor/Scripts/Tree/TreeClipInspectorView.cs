using Taco.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Taco.Timeline.Editor
{
    public class TreeClipInspectorView : TimelineClipInspectorView
    {
        TreeClip m_TreeClip;
        VisualElement m_PropertyContent;

        public TreeClipInspectorView(Clip clip)
        {
            m_TreeClip = clip as TreeClip;

            m_PropertyContent = new VisualElement();
            Add(m_PropertyContent);

            DropdownMenuHandler dropdownMenuManipulator = new DropdownMenuHandler((menu) =>
            {
                if (m_TreeClip.TreePrefab)
                {
                    foreach (var exposedProperty in m_TreeClip.TreePrefab.ExposedProperties)
                    {
                        if (m_TreeClip.Properties.Find(i => i.ExposedProperty == exposedProperty) != null)
                            continue;

                        menu.AppendAction(exposedProperty.Name, (e) =>
                        {
                            m_TreeClip.Timeline.ApplyModify(() =>
                            {
                                m_TreeClip.AddProperty(exposedProperty);
                            }, "AddProperty");
                            EditorCoroutineHelper.Delay(PopulateView, 0.01f);
                        });
                    }
                }
            });
            Button addPropertyButton = new Button();
            addPropertyButton.text = "AddProperty";
            addPropertyButton.clicked += () =>
            {
                dropdownMenuManipulator.ShowMenu(addPropertyButton);
            };   
            Add(addPropertyButton);

            PopulateView();

            RegisterCallback<DetachFromPanelEvent>(OnDestroy);
        }

        void PopulateView()
        {
            m_PropertyContent.Clear();
            for (int i = m_TreeClip.Properties.Count - 1; i >= 0; i--)
            {
                TreeProperty property = m_TreeClip.Properties[i];

                if (property.ExposedProperty != null)
                {

                    SerializedProperty serializedProperty = m_TreeClip.Timeline.SerializedTimeline.FindProperty("m_Tracks");
                    serializedProperty = serializedProperty.GetArrayElementAtIndex(m_TreeClip.Timeline.Tracks.IndexOf(m_TreeClip.Track));
                    serializedProperty = serializedProperty.FindPropertyRelative("m_Clips");
                    serializedProperty = serializedProperty.GetArrayElementAtIndex(m_TreeClip.Track.Clips.IndexOf(m_TreeClip));
                    serializedProperty = serializedProperty.FindPropertyRelative("m_Properties");
                    serializedProperty = serializedProperty.GetArrayElementAtIndex(m_TreeClip.Properties.IndexOf(property));
                    serializedProperty = serializedProperty.FindPropertyRelative("m_Value");

                    PropertyField propertyField = new PropertyField(serializedProperty);
                    propertyField.Bind(m_TreeClip.Timeline.SerializedTimeline);
                    propertyField.label = property.ExposedProperty.Name;

                    m_PropertyContent.Add(propertyField);
                }
                else
                {
                    m_TreeClip.Properties.Remove(property);
                }
            }
        }

        void OnDestroy(DetachFromPanelEvent e)
        {
            
        }
    }
}