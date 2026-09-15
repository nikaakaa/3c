using System.Collections.Generic;
using ThirdPersonCharacter.ActionSystem;
using ThirdPersonCharacter.Control.Authoring;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterPipelineDefinition))]
    public sealed class CharacterPipelineDefinitionEditor : UnityEditor.Editor
    {
        readonly List<string> m_ConfigurationErrors = new List<string>();
        SerializedProperty m_ControlModuleId;
        SerializedProperty m_ControlParameters;
        SerializedProperty m_AbilityGrants;
        SerializedProperty m_SimulationTickRate;
        SerializedProperty m_InputProfile;
        SerializedProperty m_GameplayEffectProfile;
        SerializedProperty m_BodyMotionProfile;
        SerializedProperty m_AdmissionProfiles;
        SerializedProperty m_BehaviorProfiles;
        SerializedProperty m_AnimationPresentationProfile;
		SerializedProperty m_CameraProfile;
		SerializedProperty m_EquipmentCapabilityEnabled;
		SerializedProperty m_EquipmentProfile;
		SerializedProperty m_EquipmentPresentationProfile;
        bool m_ConfigurationValidated;
        bool m_ConfigurationValid;

        void OnEnable()
        {
            m_ControlModuleId = serializedObject.FindProperty("m_ControlModuleId");
            m_ControlParameters = serializedObject.FindProperty("m_ControlParameters");
            m_AbilityGrants = serializedObject.FindProperty("m_AbilityGrants");
            m_SimulationTickRate = serializedObject.FindProperty("m_SimulationTickRate");
            m_InputProfile = serializedObject.FindProperty("m_InputProfile");
            m_GameplayEffectProfile = serializedObject.FindProperty("m_GameplayEffectProfile");
            m_BodyMotionProfile = serializedObject.FindProperty("m_BodyMotionProfile");
            m_AdmissionProfiles = serializedObject.FindProperty("m_AdmissionProfiles");
            m_BehaviorProfiles = serializedObject.FindProperty("m_BehaviorProfiles");
            m_AnimationPresentationProfile = serializedObject.FindProperty("m_AnimationPresentationProfile");
			m_CameraProfile = serializedObject.FindProperty("m_CameraProfile");
			m_EquipmentCapabilityEnabled = serializedObject.FindProperty("m_EquipmentCapabilityEnabled");
			m_EquipmentProfile = serializedObject.FindProperty("m_EquipmentProfile");
			m_EquipmentPresentationProfile = serializedObject.FindProperty("m_EquipmentPresentationProfile");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPipeline();
            DrawConfigReferences();
            if (serializedObject.ApplyModifiedProperties())
            {
                m_ConfigurationValidated = false;
                m_ConfigurationValid = false;
                m_ConfigurationErrors.Clear();
            }
            DrawNavigation();
        }

        void DrawPipeline()
        {
            EditorGUILayout.LabelField("Pipeline", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SimulationTickRate, new GUIContent("Simulation Tick Rate"));
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_ControlModuleId, new GUIContent("Control Module"));
            EditorGUILayout.PropertyField(m_ControlParameters, new GUIContent("Parameters"), true);
            EditorGUILayout.Space(3f);
            EditorGUILayout.LabelField("Gameplay Abilities", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AbilityGrants, new GUIContent("Grants"), true);
            EditorGUILayout.Space(6f);
        }

        void DrawConfigReferences()
        {
            EditorGUILayout.LabelField("Config References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_InputProfile, new GUIContent("Input"));
            EditorGUILayout.PropertyField(m_GameplayEffectProfile, new GUIContent("Gameplay Effect"));
            EditorGUILayout.PropertyField(m_BodyMotionProfile, new GUIContent("Body Motion"));
            EditorGUILayout.PropertyField(m_AnimationPresentationProfile, new GUIContent("Animation Presentation"));
			EditorGUILayout.PropertyField(m_CameraProfile, new GUIContent("Camera"));
			EditorGUILayout.Space(3f);
			EditorGUILayout.PropertyField(m_EquipmentCapabilityEnabled, new GUIContent("Equipment Capability"));
			using (new EditorGUI.DisabledScope(!m_EquipmentCapabilityEnabled.boolValue))
			{
				EditorGUILayout.PropertyField(m_EquipmentProfile, new GUIContent("Equipment Gameplay"));
				EditorGUILayout.PropertyField(m_EquipmentPresentationProfile, new GUIContent("Equipment Presentation"));
			}
			using (new EditorGUI.DisabledScope(true))
			{
				string equipmentState = !m_EquipmentCapabilityEnabled.boolValue
					? "Disabled"
					: m_EquipmentProfile.objectReferenceValue && m_EquipmentPresentationProfile.objectReferenceValue
						? "Configured"
						: "Incomplete";
				EditorGUILayout.TextField("Equipment State", equipmentState);
			}
            EditorGUILayout.PropertyField(m_AdmissionProfiles, new GUIContent("Admission Profiles"), true);
            EditorGUILayout.PropertyField(m_BehaviorProfiles, new GUIContent("Behaviors"), true);
            EditorGUILayout.Space(6f);
        }

        void DrawNavigation()
        {
            CharacterPipelineDefinition definition = target as CharacterPipelineDefinition;
            if (!definition)
                return;

            EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);
            DrawAbilityNavigation(definition);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!definition.AnimationPresentationProfile))
            {
                if (GUILayout.Button("Open Animation Profile"))
                    OpenAsset(definition.AnimationPresentationProfile);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Validate Configuration"))
                ValidateConfiguration(definition);

            for (int i = 0; i < m_ConfigurationErrors.Count; i++)
                EditorGUILayout.HelpBox(m_ConfigurationErrors[i], MessageType.Error);
            if (m_ConfigurationValidated && m_ConfigurationValid)
                EditorGUILayout.HelpBox("Configuration is valid.", MessageType.Info);
        }

        static void DrawAbilityNavigation(CharacterPipelineDefinition definition)
        {
            EditorGUILayout.LabelField("Gameplay Abilities", EditorStyles.boldLabel);
            IReadOnlyList<AbilityGrant> grants = definition.AbilityGrants;
            for (int i = 0; i < grants.Count; i++)
            {
                AbilityGrant grant = grants[i];
                GameplayAbilityDefinition ability = grant?.Ability;
                BtsmtlSkillFlowGraph graph = ability?.AbilityGraph;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(grant?.AbilityId ?? "<missing>", GUILayout.MinWidth(120f));
                using (new EditorGUI.DisabledScope(!graph))
                {
                    if (GUILayout.Button("Open Ability Graph", GUILayout.Width(140f)))
                        NodeCanvas.Editor.GraphEditor.OpenWindow(graph);
                }
                EditorGUILayout.EndHorizontal();
                if (!ability)
                    EditorGUILayout.HelpBox("AbilityGrant has no GameplayAbilityDefinition.", MessageType.Warning);
                else if (!graph)
                    EditorGUILayout.HelpBox($"Gameplay Ability '{ability.AbilityId}' has no AbilityGraph.", MessageType.Warning);
            }
        }

        void ValidateConfiguration(CharacterPipelineDefinition definition)
        {
            m_ConfigurationErrors.Clear();
            m_ConfigurationValidated = true;
            m_ConfigurationValid = definition.CollectConfigurationErrors(m_ConfigurationErrors);
        }

        static void OpenAsset(Object asset)
        {
            if (!asset)
                return;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            AssetDatabase.OpenAsset(asset);
        }
    }
}
