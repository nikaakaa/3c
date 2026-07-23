using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring;
using ThirdPersonCharacter.Pipeline.Simulation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    [CustomEditor(typeof(CharacterPipelineDefinition))]
    public sealed class CharacterPipelineDefinitionEditor : UnityEditor.Editor
    {
        enum ArtifactStatus
        {
            Missing,
            Invalid,
            Unchecked,
            NeedsCompile,
            Ready,
            Stale
        }

        readonly List<string> m_ConfigurationErrors = new List<string>();
        SerializedProperty m_RootTreeAsset;
        SerializedProperty m_SimulationTickRate;
        SerializedProperty m_SimulationProgram;
        SerializedProperty m_PresentationProjection;
        SerializedProperty m_InputProfile;
        SerializedProperty m_GameplayEffectProfile;
        SerializedProperty m_BodyMotionProfile;
        SerializedProperty m_ActionProfiles;
        SerializedProperty m_BehaviorProfiles;
        SerializedProperty m_AnimationPresentationProfile;
		SerializedProperty m_EquipmentCapabilityEnabled;
		SerializedProperty m_EquipmentProfile;
		SerializedProperty m_EquipmentPresentationProfile;
        CharacterSimulationCompileReport m_CompileReport;
        string m_DiagnosticsError = string.Empty;
        bool m_ConfigurationValidated;
        bool m_ConfigurationValid;
        bool m_ShowGeneratedArtifacts;
        bool m_ShowDiagnostics;
        CharacterSemanticIrCacheStatus m_IrCacheStatus;
        string m_IrCacheMessage = string.Empty;
        bool m_IrCacheInitialized;
        ArtifactStatus m_ArtifactStatus;
        bool m_ArtifactStatusInitialized;

        void OnEnable()
        {
            m_RootTreeAsset = serializedObject.FindProperty("m_RootTreeAsset");
            m_SimulationTickRate = serializedObject.FindProperty("m_SimulationTickRate");
            m_SimulationProgram = serializedObject.FindProperty("m_SimulationProgram");
            m_PresentationProjection = serializedObject.FindProperty("m_PresentationProjection");
            m_InputProfile = serializedObject.FindProperty("m_InputProfile");
            m_GameplayEffectProfile = serializedObject.FindProperty("m_GameplayEffectProfile");
            m_BodyMotionProfile = serializedObject.FindProperty("m_BodyMotionProfile");
            m_ActionProfiles = serializedObject.FindProperty("m_ActionProfiles");
            m_BehaviorProfiles = serializedObject.FindProperty("m_BehaviorProfiles");
            m_AnimationPresentationProfile = serializedObject.FindProperty("m_AnimationPresentationProfile");
			m_EquipmentCapabilityEnabled = serializedObject.FindProperty("m_EquipmentCapabilityEnabled");
			m_EquipmentProfile = serializedObject.FindProperty("m_EquipmentProfile");
			m_EquipmentPresentationProfile = serializedObject.FindProperty("m_EquipmentPresentationProfile");
            RefreshArtifactHeaderStatus(target as CharacterPipelineDefinition);
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
                m_IrCacheInitialized = false;
                m_ArtifactStatus = ArtifactStatus.NeedsCompile;
                m_ArtifactStatusInitialized = true;
            }
            DrawArtifactStatus();
            DrawNavigation();
        }

        void DrawPipeline()
        {
            EditorGUILayout.LabelField("Pipeline", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_RootTreeAsset, new GUIContent("Root Tree"));
            EditorGUILayout.PropertyField(m_SimulationTickRate, new GUIContent("Simulation Tick Rate"));
            EditorGUILayout.Space(6f);
        }

        void DrawConfigReferences()
        {
            EditorGUILayout.LabelField("Config References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_InputProfile, new GUIContent("Input"));
            EditorGUILayout.PropertyField(m_GameplayEffectProfile, new GUIContent("Gameplay Effect"));
            EditorGUILayout.PropertyField(m_BodyMotionProfile, new GUIContent("Body Motion"));
            EditorGUILayout.PropertyField(m_AnimationPresentationProfile, new GUIContent("Animation Presentation"));
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
            EditorGUILayout.PropertyField(m_ActionProfiles, new GUIContent("Actions"), true);
            EditorGUILayout.PropertyField(m_BehaviorProfiles, new GUIContent("Behaviors"), true);
            EditorGUILayout.Space(6f);
        }

        void DrawArtifactStatus()
        {
            CharacterPipelineDefinition definition = target as CharacterPipelineDefinition;
            if (!definition)
                return;

            EditorGUILayout.LabelField("Artifact Status", EditorStyles.boldLabel);
            if (!m_ArtifactStatusInitialized)
                RefreshArtifactHeaderStatus(definition);
            string status = GetArtifactStatusLabel(m_ArtifactStatus);
            EditorGUILayout.HelpBox(
                GetArtifactStatusMessage(status, m_ArtifactStatus),
                GetArtifactStatusMessageType(m_ArtifactStatus));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Compile"))
            {
                if (CharacterSimulationProgramBuildService.Build(definition, true))
                {
                    m_ArtifactStatus = ArtifactStatus.Ready;
                    m_ArtifactStatusInitialized = true;
                }
                else if (m_ArtifactStatus != ArtifactStatus.NeedsCompile)
                {
                    RefreshArtifactHeaderStatus(definition);
                }
                m_CompileReport = null;
                m_DiagnosticsError = string.Empty;
                m_IrCacheInitialized = false;
            }
            if (GUILayout.Button("Refresh Status"))
                RefreshExactArtifactStatus(definition);
            if (GUILayout.Button("Diagnostics"))
                RunDiagnostics(definition);
            EditorGUILayout.EndHorizontal();

            m_ShowGeneratedArtifacts = EditorGUILayout.Foldout(
                m_ShowGeneratedArtifacts,
                "Generated Artifacts",
                true);
            if (m_ShowGeneratedArtifacts)
            {
                using (new EditorGUI.IndentLevelScope())
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.PropertyField(m_SimulationProgram, new GUIContent("Program"));
                    EditorGUILayout.PropertyField(m_PresentationProjection, new GUIContent("Projection"));
                }
                DrawArtifactMetadata(definition);
                DrawSemanticIrArtifact(definition);
            }

            if (m_ShowDiagnostics)
                DrawDiagnostics();
            EditorGUILayout.Space(6f);
        }

        void RefreshArtifactHeaderStatus(CharacterPipelineDefinition definition)
        {
            m_ArtifactStatusInitialized = true;
            if (!definition || !definition.SimulationProgram || !definition.PresentationProjection)
            {
                m_ArtifactStatus = ArtifactStatus.Missing;
                return;
            }
            m_ArtifactStatus = CharacterSimulationProgramBuildService.HasPublishedArtifactHeader(definition)
                ? ArtifactStatus.Unchecked
                : ArtifactStatus.Invalid;
        }

        void RefreshExactArtifactStatus(CharacterPipelineDefinition definition)
        {
            RefreshArtifactHeaderStatus(definition);
            if (m_ArtifactStatus != ArtifactStatus.Unchecked)
                return;
            m_ArtifactStatus = CharacterSimulationProgramBuildService.EvaluateExactArtifactStaleness(definition)
                ? ArtifactStatus.Stale
                : ArtifactStatus.Ready;
        }

        static string GetArtifactStatusLabel(ArtifactStatus status)
        {
            return status == ArtifactStatus.NeedsCompile ? "Needs Compile" : status.ToString();
        }

        static string GetArtifactStatusMessage(string label, ArtifactStatus status)
        {
            return status == ArtifactStatus.Unchecked
                ? $"Program / Projection: {label}\nPublished headers exist. Use Refresh Status to compare the current authoring source."
                : $"Program / Projection: {label}";
        }

        static MessageType GetArtifactStatusMessageType(ArtifactStatus status)
        {
            return status switch
            {
                ArtifactStatus.Ready => MessageType.Info,
                ArtifactStatus.Unchecked or ArtifactStatus.NeedsCompile or ArtifactStatus.Stale => MessageType.Warning,
                _ => MessageType.Error
            };
        }

        static void DrawArtifactMetadata(CharacterPipelineDefinition definition)
        {
            CharacterSimulationProgramAsset program = definition.SimulationProgram;
            CharacterPresentationProjectionAsset projection = definition.PresentationProjection;
            if (!program || !projection)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Compiler", program.CompilerVersion);
                EditorGUILayout.LabelField("Numeric Profile", $"{program.NumericProfileId} / ABI {program.TargetAbiVersion}");
                DrawIdentity("Source Revision", program.SourceRevision);
                DrawIdentity("Program Hash", program.ProgramHash);
                DrawIdentity("Presentation Contract", projection.ContractHash);
                DrawIdentity("Projection Revision", projection.ProjectionRevision);
            }
        }

        void DrawSemanticIrArtifact(CharacterPipelineDefinition definition)
        {
            if (!m_IrCacheInitialized)
                RefreshIrCacheStatus(definition);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.LabelField("Semantic IR Cache", m_IrCacheStatus.ToString());
                if (!string.IsNullOrEmpty(m_IrCacheMessage))
                    EditorGUILayout.HelpBox(m_IrCacheMessage, m_IrCacheStatus == CharacterSemanticIrCacheStatus.Current ? MessageType.Info : MessageType.Warning);
                if (GUILayout.Button("Open Semantic IR"))
                    CharacterSemanticIrInspectorWindow.Open(definition);
            }
        }

        void RefreshIrCacheStatus(CharacterPipelineDefinition definition)
        {
            m_IrCacheInitialized = true;
            m_IrCacheStatus = CharacterSemanticIrCacheStatus.Missing;
            m_IrCacheMessage = string.Empty;
            if (!definition)
                return;
            string definitionPath = AssetDatabase.GetAssetPath(definition);
            string definitionGuid = string.IsNullOrEmpty(definitionPath) ? string.Empty : AssetDatabase.AssetPathToGUID(definitionPath);
            if (string.IsNullOrEmpty(definitionGuid))
                return;
            CharacterSemanticIrCacheResult cache = CharacterSemanticIrArtifactStore.Inspect(definitionGuid);
            m_IrCacheStatus = cache.Status;
            m_IrCacheMessage = cache.Message;
        }

        void RunDiagnostics(CharacterPipelineDefinition definition)
        {
            m_ShowDiagnostics = true;
            m_DiagnosticsError = string.Empty;
            try
            {
                CharacterSimulationBuildResult result = CharacterSimulationBuildOrchestrator.DryRun(definition);
                m_CompileReport = result.Report;
            }
            catch (System.Exception exception)
            {
                m_CompileReport = null;
                m_DiagnosticsError = exception.Message;
            }
        }

        void DrawDiagnostics()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Compiler Diagnostics", EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(m_DiagnosticsError))
                EditorGUILayout.HelpBox(m_DiagnosticsError, MessageType.Error);
            if (m_CompileReport == null)
                return;

            for (int i = 0; i < m_CompileReport.Messages.Count; i++)
            {
                CharacterSimulationCompileMessage message = m_CompileReport.Messages[i];
                MessageType type = message.Severity == CharacterSimulationCompileSeverity.Error
                    ? MessageType.Error
                    : message.Severity == CharacterSimulationCompileSeverity.Warning
                        ? MessageType.Warning
                        : MessageType.Info;
                EditorGUILayout.HelpBox(
                    $"{message.Stage} / {message.Code}\n{message.SourceIdentity}\n{message.Message}",
                    type);
            }
        }

        void DrawNavigation()
        {
            CharacterPipelineDefinition definition = target as CharacterPipelineDefinition;
            if (!definition)
                return;

            EditorGUILayout.LabelField("Navigation", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!definition.RootTreeAsset))
            {
                if (GUILayout.Button("Open Root Tree"))
                    CharacterPipelineDefinitionTreeWindowUtility.OpenRootTree(definition);
            }
            using (new EditorGUI.DisabledScope(!definition.AnimationPresentationProfile))
            {
                if (GUILayout.Button("Open Animation Profile"))
                    OpenAsset(definition.AnimationPresentationProfile);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Open Agent Controller"))
                AgentCharacterControllerSynthesisWindow.Open(definition);
            if (GUILayout.Button("Validate Configuration"))
                ValidateConfiguration(definition);

            for (int i = 0; i < m_ConfigurationErrors.Count; i++)
                EditorGUILayout.HelpBox(m_ConfigurationErrors[i], MessageType.Error);
            if (m_ConfigurationValidated && m_ConfigurationValid)
                EditorGUILayout.HelpBox("Configuration is valid.", MessageType.Info);
        }

        void ValidateConfiguration(CharacterPipelineDefinition definition)
        {
            m_ConfigurationErrors.Clear();
            m_ConfigurationValidated = true;
            m_ConfigurationValid = definition.CollectConfigurationErrors(m_ConfigurationErrors);
        }

        static void DrawIdentity(string label, string value)
        {
            EditorGUILayout.LabelField(label);
            EditorGUILayout.SelectableLabel(
                value ?? string.Empty,
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
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
