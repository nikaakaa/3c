using System;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(GameplayAbilityDefinition))]
    public sealed class GameplayAbilityDefinitionEditor : UnityEditor.Editor
    {
        SerializedProperty m_AbilityId;
        SerializedProperty m_DisplayName;
        SerializedProperty m_DebugCategory;
        SerializedProperty m_Tags;
        SerializedProperty m_AdmissionProfile;
        SerializedProperty m_Effects;
        SerializedProperty m_EndRules;
        SerializedProperty m_SubgraphDependencies;
        SerializedProperty m_AllowedFollowUps;
        SerializedProperty m_AbilityGraph;

        void OnEnable()
        {
            m_AbilityId = serializedObject.FindProperty("m_AbilityId");
            m_DisplayName = serializedObject.FindProperty("m_DisplayName");
            m_DebugCategory = serializedObject.FindProperty("m_DebugCategory");
            m_Tags = serializedObject.FindProperty("m_Tags");
            m_AdmissionProfile = serializedObject.FindProperty("m_AdmissionProfile");
            m_Effects = serializedObject.FindProperty("m_Effects");
            m_EndRules = serializedObject.FindProperty("m_EndRules");
            m_SubgraphDependencies = serializedObject.FindProperty("m_SubgraphDependencies");
            m_AllowedFollowUps = serializedObject.FindProperty("m_AllowedFollowUpAbilityIds");
            m_AbilityGraph = serializedObject.FindProperty("m_AbilityGraph");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Gameplay Ability", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AbilityId, new GUIContent("Ability Id"));
            EditorGUILayout.PropertyField(m_DisplayName, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(m_DebugCategory, new GUIContent("Debug Category"));
            EditorGUILayout.PropertyField(m_Tags, new GUIContent("Gameplay Tags"), true);
            EditorGUILayout.PropertyField(m_AdmissionProfile, new GUIContent("Admission Rules"));
            GameplayAbilityDefinition ability = target as GameplayAbilityDefinition;
            if (ability?.AdmissionProfile)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.ObjectField("Shared Rule Source", ability.AdmissionProfile, typeof(GameplayAbilityAdmissionProfile), false);
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.EnumPopup("Target Requirement", ability.TargetRequirement);
                if (GUILayout.Button("Open Shared Admission Rules"))
                    AssetDatabase.OpenAsset(ability.AdmissionProfile);
            }
            EditorGUILayout.PropertyField(m_Effects, new GUIContent("Gameplay Effects"), true);
            EditorGUILayout.PropertyField(m_EndRules, new GUIContent("End Rules"), true);
            EditorGUILayout.PropertyField(m_SubgraphDependencies, new GUIContent("Subgraph Dependencies"), true);
            EditorGUILayout.PropertyField(m_AllowedFollowUps, new GUIContent("Follow-up Abilities"), true);
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Private Ability Graph", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(m_AbilityGraph, new GUIContent("AbilityGraph"));
            BtsmtlSkillFlowGraph graph = ability?.AbilityGraph;
            if (!graph && ability && !string.IsNullOrWhiteSpace(ability.AbilityId) &&
                !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(ability)) &&
                GUILayout.Button("Create Private Ability Graph"))
            {
                graph = BtsmtlSkillGraphAssetFactory.CreatePrivateAbilityGraph(
                    ability,
                    Guid.NewGuid().ToString("N"),
                    ability.DisplayName);
                serializedObject.Update();
            }
            using (new EditorGUI.DisabledScope(!graph))
            {
                if (GUILayout.Button("Open Native FlowCanvas Graph"))
                    NodeCanvas.Editor.GraphEditor.OpenWindow(graph);
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
