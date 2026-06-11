using ThirdPersonDiagnostics;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonDiagnostics.Editor
{
    [CustomEditor(typeof(RuntimeDiagnosticLogInspectorController))]
    public sealed class RuntimeDiagnosticLogInspectorControllerEditor : UnityEditor.Editor
    {
        SerializedProperty channelsProperty;
        SerializedProperty containsFilterProperty;
        SerializedProperty prefixFilterProperty;
        SerializedProperty suffixFilterProperty;
        SerializedProperty manualChannelKeyProperty;

        void OnEnable()
        {
            channelsProperty = serializedObject.FindProperty("channels");
            containsFilterProperty = serializedObject.FindProperty("containsFilter");
            prefixFilterProperty = serializedObject.FindProperty("prefixFilter");
            suffixFilterProperty = serializedObject.FindProperty("suffixFilter");
            manualChannelKeyProperty = serializedObject.FindProperty("manualChannelKey");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawCommandButtons();
            EditorGUILayout.Space();
            DrawManualChannel();
            EditorGUILayout.Space();
            DrawNameFilters();
            EditorGUILayout.Space();
            DrawChannels();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawCommandButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("同步"))
            {
                serializedObject.ApplyModifiedProperties();
                RunForTargets(controller => controller.SynchronizeChannels(), "Sync Diagnostic Channels");
                serializedObject.Update();
            }

            if (GUILayout.Button("应用"))
            {
                serializedObject.ApplyModifiedProperties();
                RunForTargets(controller => controller.ApplyChannels(), "Apply Diagnostic Channels");
                serializedObject.Update();
            }

            if (GUILayout.Button("全开"))
            {
                serializedObject.ApplyModifiedProperties();
                RunForTargets(controller => controller.EnableAllChannels(), "Enable Diagnostic Channels");
                serializedObject.Update();
            }

            if (GUILayout.Button("全关"))
            {
                serializedObject.ApplyModifiedProperties();
                RunForTargets(controller => controller.DisableAllChannels(), "Disable Diagnostic Channels");
                serializedObject.Update();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawManualChannel()
        {
            EditorGUILayout.LabelField("手动通道", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(manualChannelKeyProperty, new GUIContent("Key"));
            if (GUILayout.Button("添加", GUILayout.Width(52f)))
            {
                serializedObject.ApplyModifiedProperties();
                RunForTargets(controller => controller.AddManualChannel(), "Add Diagnostic Channel");
                serializedObject.Update();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawNameFilters()
        {
            EditorGUILayout.LabelField("名称筛选", EditorStyles.boldLabel);
            DrawFilterRow(containsFilterProperty, "包含", controller => controller.ApplyContainsFilter(), "Apply Diagnostic Contains Filter");
            DrawFilterRow(prefixFilterProperty, "前缀", controller => controller.ApplyPrefixFilter(), "Apply Diagnostic Prefix Filter");
            DrawFilterRow(suffixFilterProperty, "后缀", controller => controller.ApplySuffixFilter(), "Apply Diagnostic Suffix Filter");
        }

        void DrawFilterRow(SerializedProperty property, string label, ActionForController action, string undoName)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(property, new GUIContent(label));
            if (GUILayout.Button("应用", GUILayout.Width(52f)))
            {
                serializedObject.ApplyModifiedProperties();
                RunForTargets(action, undoName);
                serializedObject.Update();
            }

            EditorGUILayout.EndHorizontal();
        }

        void DrawChannels()
        {
            EditorGUILayout.LabelField("日志通道", EditorStyles.boldLabel);
            for (int i = 0; i < channelsProperty.arraySize; i++)
            {
                SerializedProperty channel = channelsProperty.GetArrayElementAtIndex(i);
                SerializedProperty key = channel.FindPropertyRelative("key");
                SerializedProperty enabled = channel.FindPropertyRelative("enabled");

                EditorGUILayout.BeginHorizontal();
                enabled.boolValue = EditorGUILayout.ToggleLeft(key.stringValue, enabled.boolValue);
                EditorGUILayout.EndHorizontal();
            }
        }

        void RunForTargets(ActionForController action, string undoName)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                RuntimeDiagnosticLogInspectorController controller = (RuntimeDiagnosticLogInspectorController)targets[i];
                Undo.RecordObject(controller, undoName);
                action(controller);
                EditorUtility.SetDirty(controller);
            }
        }

        delegate void ActionForController(RuntimeDiagnosticLogInspectorController controller);
    }
}
