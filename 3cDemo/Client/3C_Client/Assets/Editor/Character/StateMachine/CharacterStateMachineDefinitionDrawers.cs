using ThirdPersonCharacterStateMachine;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacterStateMachine.Editor
{
    [CustomPropertyDrawer(typeof(CharacterStateNodeDefinition))]
    public sealed class CharacterStateNodeDefinitionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, BuildLabel(property, label), true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                DrawProperty(ref row, property.FindPropertyRelative("stateId"));
                DrawProperty(ref row, property.FindPropertyRelative("parentStateId"));
                DrawProperty(ref row, property.FindPropertyRelative("pathSegment"));
                DrawProperty(ref row, property.FindPropertyRelative("tags"), true);
                DrawProperty(ref row, property.FindPropertyRelative("modules"), true);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            return EditorGUIUtility.singleLineHeight +
                   Height(property.FindPropertyRelative("stateId")) +
                   Height(property.FindPropertyRelative("parentStateId")) +
                   Height(property.FindPropertyRelative("pathSegment")) +
                   Height(property.FindPropertyRelative("tags"), true) +
                   Height(property.FindPropertyRelative("modules"), true) +
                   EditorGUIUtility.standardVerticalSpacing * 5f;
        }

        static GUIContent BuildLabel(SerializedProperty property, GUIContent fallback)
        {
            SerializedProperty id = property.FindPropertyRelative("stateId");
            return new GUIContent(string.IsNullOrWhiteSpace(id.stringValue) ? fallback.text : id.stringValue);
        }

        static void DrawProperty(ref Rect row, SerializedProperty property, bool includeChildren = false)
        {
            float height = EditorGUI.GetPropertyHeight(property, includeChildren);
            row.height = height;
            EditorGUI.PropertyField(row, property, includeChildren);
            row.y += height + EditorGUIUtility.standardVerticalSpacing;
        }

        static float Height(SerializedProperty property, bool includeChildren = false)
        {
            return EditorGUI.GetPropertyHeight(property, includeChildren);
        }
    }

    [CustomPropertyDrawer(typeof(CharacterStateModuleDefinition))]
    public sealed class CharacterStateModuleDefinitionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            SerializedProperty moduleType = property.FindPropertyRelative("moduleType");
            Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, moduleType.enumDisplayNames[moduleType.enumValueIndex], true);
            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;
                row.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                DrawProperty(ref row, moduleType);
                DrawPayload(ref row, property, (CharacterStateModuleType)moduleType.enumValueIndex);
                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
                return EditorGUIUtility.singleLineHeight;

            SerializedProperty moduleType = property.FindPropertyRelative("moduleType");
            CharacterStateModuleType type = (CharacterStateModuleType)moduleType.enumValueIndex;
            return EditorGUIUtility.singleLineHeight +
                   Height(moduleType) +
                   PayloadHeight(property, type) +
                   EditorGUIUtility.standardVerticalSpacing * (PayloadFieldCount(type) + 1);
        }

        static void DrawPayload(ref Rect row, SerializedProperty property, CharacterStateModuleType type)
        {
            switch (type)
            {
                case CharacterStateModuleType.LocomotionPhase:
                    DrawProperty(ref row, property.FindPropertyRelative("locomotionPhase"));
                    break;
                case CharacterStateModuleType.InputDrivenMotion:
                    break;
                case CharacterStateModuleType.ConfiguredActionMotion:
                    DrawProperty(ref row, property.FindPropertyRelative("actionMovements"), true);
                    break;
                case CharacterStateModuleType.ActionAnimation:
                    DrawProperty(ref row, property.FindPropertyRelative("playbackFactSource"));
                    DrawProperty(ref row, property.FindPropertyRelative("animation"));
                    DrawProperty(ref row, property.FindPropertyRelative("variants"), true);
                    break;
                case CharacterStateModuleType.LocomotionAnimationAlias:
                    DrawProperty(ref row, property.FindPropertyRelative("playbackFactSource"));
                    DrawProperty(ref row, property.FindPropertyRelative("animation"));
                    break;
                case CharacterStateModuleType.TurnBackMotionPolicy:
                    DrawProperty(ref row, property.FindPropertyRelative("turnBackMotionPolicy"), true);
                    break;
                case CharacterStateModuleType.InputConsume:
                    DrawProperty(ref row, property.FindPropertyRelative("requestKind"));
                    break;
                case CharacterStateModuleType.RunLatch:
                    DrawProperty(ref row, property.FindPropertyRelative("resetRunLatchOnEnter"));
                    DrawProperty(ref row, property.FindPropertyRelative("setRunLatchOnComplete"));
                    break;
                case CharacterStateModuleType.TimelineWindow:
                    DrawProperty(ref row, property.FindPropertyRelative("timelineWindows"), true);
                    break;
            }
        }

        static float PayloadHeight(SerializedProperty property, CharacterStateModuleType type)
        {
            switch (type)
            {
                case CharacterStateModuleType.LocomotionPhase:
                    return Height(property.FindPropertyRelative("locomotionPhase"));
                case CharacterStateModuleType.InputDrivenMotion:
                    return 0f;
                case CharacterStateModuleType.ConfiguredActionMotion:
                    return Height(property.FindPropertyRelative("actionMovements"), true);
                case CharacterStateModuleType.ActionAnimation:
                    return Height(property.FindPropertyRelative("playbackFactSource")) +
                           Height(property.FindPropertyRelative("animation")) +
                           Height(property.FindPropertyRelative("variants"), true);
                case CharacterStateModuleType.LocomotionAnimationAlias:
                    return Height(property.FindPropertyRelative("playbackFactSource")) +
                           Height(property.FindPropertyRelative("animation"));
                case CharacterStateModuleType.TurnBackMotionPolicy:
                    return Height(property.FindPropertyRelative("turnBackMotionPolicy"), true);
                case CharacterStateModuleType.InputConsume:
                    return Height(property.FindPropertyRelative("requestKind"));
                case CharacterStateModuleType.RunLatch:
                    return Height(property.FindPropertyRelative("resetRunLatchOnEnter")) +
                           Height(property.FindPropertyRelative("setRunLatchOnComplete"));
                case CharacterStateModuleType.TimelineWindow:
                    return Height(property.FindPropertyRelative("timelineWindows"), true);
                default:
                    return 0f;
            }
        }

        static int PayloadFieldCount(CharacterStateModuleType type)
        {
            switch (type)
            {
                case CharacterStateModuleType.InputDrivenMotion:
                    return 0;
                case CharacterStateModuleType.ActionAnimation:
                    return 3;
                case CharacterStateModuleType.LocomotionAnimationAlias:
                case CharacterStateModuleType.RunLatch:
                    return 2;
                default:
                    return 1;
            }
        }

        static void DrawProperty(ref Rect row, SerializedProperty property, bool includeChildren = false)
        {
            float height = EditorGUI.GetPropertyHeight(property, includeChildren);
            row.height = height;
            EditorGUI.PropertyField(row, property, includeChildren);
            row.y += height + EditorGUIUtility.standardVerticalSpacing;
        }

        static float Height(SerializedProperty property, bool includeChildren = false)
        {
            return EditorGUI.GetPropertyHeight(property, includeChildren);
        }
    }
}
