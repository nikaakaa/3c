using ThirdPersonCharacter.Pipeline.Animation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ThirdPersonCharacter.Editor
{
    [CustomPropertyDrawer(typeof(CharacterAnimationBlendTransitionRule))]
    public sealed class CharacterAnimationBlendTransitionRuleDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            SerializedProperty logic = property.FindPropertyRelative("m_BlendLogic");
            SerializedProperty duration = property.FindPropertyRelative("m_DurationSeconds");
            SerializedProperty mode = property.FindPropertyRelative("m_BlendMode");
            SerializedProperty customCurve = property.FindPropertyRelative("m_CustomBlendCurve");
            SerializedProperty profile = property.FindPropertyRelative("m_BlendProfile");

            var root = new VisualElement();
            var customCurveField = new PropertyField(customCurve, "Custom Curve Asset");
            var modeField = new PropertyField(mode, "Blend Mode");
            modeField.RegisterValueChangeCallback(_ =>
            {
                if ((CharacterAnimationBlendMode)mode.intValue != CharacterAnimationBlendMode.Custom &&
                    customCurve.objectReferenceValue)
                {
                    customCurve.objectReferenceValue = null;
                    property.serializedObject.ApplyModifiedProperties();
                }
                RefreshCurveVisibility(mode, customCurveField);
            });

            root.Add(new PropertyField(logic, "Blend Logic"));
            root.Add(new PropertyField(duration, "Duration Seconds"));
            root.Add(modeField);
            root.Add(customCurveField);
            root.Add(new PropertyField(profile, "Blend Profile"));
            RefreshCurveVisibility(mode, customCurveField);
            return root;
        }

        static void RefreshCurveVisibility(
            SerializedProperty mode,
            VisualElement customCurveField)
        {
            customCurveField.style.display =
                (CharacterAnimationBlendMode)mode.intValue == CharacterAnimationBlendMode.Custom
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
        }
    }

    [CustomEditor(typeof(CharacterAnimationBlendPolicy))]
    public sealed class CharacterAnimationBlendPolicyEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var schema = new PropertyField(
                serializedObject.FindProperty("m_Schema"),
                "Schema");
            schema.SetEnabled(false);
            root.Add(schema);
            root.Add(new PropertyField(
                serializedObject.FindProperty("m_PolicyId"),
                "Policy Id"));
            root.Add(new PropertyField(
                serializedObject.FindProperty("m_Revision"),
                "Revision"));
            root.Add(new PropertyField(
                serializedObject.FindProperty("m_StackPolicy"),
                "Stack Policy"));
            root.Add(new PropertyField(
                serializedObject.FindProperty("m_DefaultTransition"),
                "Default Transition"));
            root.Add(new PropertyField(
                serializedObject.FindProperty("m_Overrides"),
                "Exact Overrides"));
            return root;
        }
    }
}
