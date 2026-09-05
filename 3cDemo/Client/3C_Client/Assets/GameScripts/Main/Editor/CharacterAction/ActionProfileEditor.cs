using System.Collections.Generic;
using System.Linq;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.ActionSystem.Editor
{
    [CustomEditor(typeof(ActionProfile))]
    public sealed class ActionProfileEditor : UnityEditor.Editor
    {
        readonly List<string> m_Errors = new List<string>();

        SerializedProperty m_ActionId;
        SerializedProperty m_DisplayName;
        SerializedProperty m_DebugCategory;
        SerializedProperty m_Tags;
        SerializedProperty m_BlockTags;
        SerializedProperty m_CancelTags;
        SerializedProperty m_TargetRequirement;

        void OnEnable()
        {
            m_ActionId = serializedObject.FindProperty("m_ActionId");
            m_DisplayName = serializedObject.FindProperty("m_DisplayName");
            m_DebugCategory = serializedObject.FindProperty("m_DebugCategory");
            m_Tags = serializedObject.FindProperty("m_Tags");
            m_BlockTags = serializedObject.FindProperty("m_BlockTags");
            m_CancelTags = serializedObject.FindProperty("m_CancelTags");
            m_TargetRequirement = serializedObject.FindProperty("m_TargetRequirement");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawHeader("Identity");
            EditorGUILayout.PropertyField(m_ActionId);
            EditorGUILayout.PropertyField(m_DisplayName);
            EditorGUILayout.PropertyField(m_DebugCategory);
            EditorGUILayout.PropertyField(m_TargetRequirement);

            DrawHeader("Gameplay Tags");
            EditorGUILayout.PropertyField(m_Tags, true);
            EditorGUILayout.PropertyField(m_BlockTags, true);
            EditorGUILayout.PropertyField(m_CancelTags, true);
            serializedObject.ApplyModifiedProperties();

            DrawHeader("Configuration");
            m_Errors.Clear();
            ActionProfile profile = target as ActionProfile;
            if (profile &&
                !string.IsNullOrWhiteSpace(profile.ActionId) &&
                GUILayout.Button("Open Action Animation Workspace"))
                OpenWorkspace(profile);
            if (profile && profile.CollectConfigurationErrors(m_Errors))
            {
                EditorGUILayout.HelpBox("Configuration is valid.", MessageType.Info);
                return;
            }

            for (int i = 0; i < m_Errors.Count; i++)
                EditorGUILayout.HelpBox(m_Errors[i], MessageType.Error);
        }

        static void DrawHeader(string label)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        static void OpenWorkspace(ActionProfile profile)
        {
            CharacterPipelineDefinition[] definitions =
                AssetDatabase.FindAssets("t:CharacterPipelineDefinition")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Select(
                        AssetDatabase
                            .LoadAssetAtPath<CharacterPipelineDefinition>)
                    .Where(
                        value =>
                            value &&
                            value.ActionProfiles.Any(
                                candidate =>
                                    ReferenceEquals(candidate, profile)))
                    .OrderBy(
                        value => AssetDatabase.GetAssetPath(value),
                        System.StringComparer.Ordinal)
                    .ToArray();
            if (definitions.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Action Animation Workspace",
                    "没有 Character Definition 以正式引用拥有该 ActionProfile。",
                    "OK");
                return;
            }
            if (definitions.Length == 1)
            {
                OpenWorkspace(definitions[0], profile);
                return;
            }
            var menu = new GenericMenu();
            for (int i = 0; i < definitions.Length; i++)
            {
                CharacterPipelineDefinition definition = definitions[i];
                menu.AddItem(
                    new GUIContent(
                        $"{definition.name} ({AssetDatabase.GetAssetPath(definition)})"),
                    false,
                    () => OpenWorkspace(definition, profile));
            }
            menu.ShowAsContext();
        }

        static void OpenWorkspace(
            CharacterPipelineDefinition definition,
            ActionProfile profile)
        {
            CharacterSkillAuthoringDefinition[] skills = definition.SkillDefinitions
                .Where(value =>
                    value != null &&
                    value.ActionProfile &&
                    ReferenceEquals(value.ActionProfile, profile) &&
                    !string.IsNullOrWhiteSpace(value.SkillId))
                .OrderBy(value => value.SkillId, System.StringComparer.Ordinal)
                .ToArray();
            if (skills.Length == 1)
            {
                OpenWorkspace(definition, profile, skills[0].SkillId);
                return;
            }
            if (skills.Length > 1)
            {
                var menu = new GenericMenu();
                for (int i = 0; i < skills.Length; i++)
                {
                    CharacterSkillAuthoringDefinition skill = skills[i];
                    CharacterSkillAuthoringDefinition captured = skill;
                    menu.AddItem(
                        new GUIContent($"{captured.SkillId} ({profile.ActionId})"),
                        false,
                        () => OpenWorkspace(definition, profile, captured.SkillId));
                }
                menu.ShowAsContext();
                return;
            }
            ActionAnimationAuthoringWorkspaceWindow.Open(
                new ActionAnimationWorkspaceOpenRequest(
                    definition,
                    profile.ActionId));
        }

        static void OpenWorkspace(
            CharacterPipelineDefinition definition,
            ActionProfile profile,
            string skillId)
        {
            ActionAnimationAuthoringWorkspaceWindow.Open(
                new ActionAnimationWorkspaceOpenRequest(
                    definition,
                    profile.ActionId,
                    skillId: skillId));
        }
    }
}
