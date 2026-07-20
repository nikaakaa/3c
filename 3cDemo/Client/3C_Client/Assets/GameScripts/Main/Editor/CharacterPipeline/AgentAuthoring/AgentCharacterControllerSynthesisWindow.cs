using System.IO;
using System.Text;
using ThirdPersonCharacter.Pipeline;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentCharacterControllerSynthesisWindow : EditorWindow
    {
        enum InputKind
        {
            Intent,
            Patch
        }

        CharacterPipelineDefinition m_Definition;
        InputKind m_InputKind;
        string m_Json = string.Empty;
        Vector2 m_Scroll;
        AgentCompileReport m_LastReport;

        public static void Open(CharacterPipelineDefinition definition)
        {
            AgentCharacterControllerSynthesisWindow window = GetWindow<AgentCharacterControllerSynthesisWindow>("Agent Controller");
            window.m_Definition = definition;
            window.Show();
        }

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            m_Definition = EditorGUILayout.ObjectField("Definition", m_Definition, typeof(CharacterPipelineDefinition), false) as CharacterPipelineDefinition;
            m_InputKind = (InputKind)EditorGUILayout.EnumPopup("Input", m_InputKind);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export Snapshot"))
                ExportSnapshot();
            if (GUILayout.Button("Export Full Debug"))
                ExportFullDebugSnapshot();
            if (GUILayout.Button("Load JSON"))
                LoadJson();
            if (GUILayout.Button("Save Report"))
                SaveReport();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Agent JSON", EditorStyles.boldLabel);
            m_Json = EditorGUILayout.TextArea(m_Json, GUILayout.MinHeight(180f));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Dry Run"))
                Run(false);
            if (GUILayout.Button("Apply"))
                Run(true);
            if (GUILayout.Button("Validate"))
                Validate();
            if (GUILayout.Button("Evaluate"))
                Evaluate();
            EditorGUILayout.EndHorizontal();

            DrawReport();
            EditorGUILayout.EndScrollView();
        }

        void ExportSnapshot()
        {
            if (!m_Definition)
                return;

            AgentGraphSnapshot snapshot = new AgentGraphSnapshotExporter().Export(m_Definition);
            AgentAuthoringJsonUtility.SaveJsonPanel("Export Agent Snapshot", $"{m_Definition.name}_AgentSnapshot", snapshot);
        }

        void ExportFullDebugSnapshot()
        {
            if (!m_Definition)
                return;

            AgentGraphSnapshot snapshot = new AgentGraphSnapshotExporter().ExportFull(m_Definition);
            AgentAuthoringJsonUtility.SaveJsonPanel("Export Full Agent Snapshot", $"{m_Definition.name}_AgentSnapshot_FullDebug", snapshot);
        }

        void LoadJson()
        {
            string path = EditorUtility.OpenFilePanel("Load Agent JSON", Application.dataPath, "json");
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return;

            m_Json = File.ReadAllText(path, Encoding.UTF8);
        }

        void SaveReport()
        {
            if (m_LastReport == null)
                return;

            AgentAuthoringJsonUtility.SaveJsonPanel("Save Agent Compile Report", "AgentCompileReport", m_LastReport);
        }

        void Run(bool apply)
        {
            m_LastReport = new AgentCompileReport { success = true };
            if (!m_Definition)
            {
                m_LastReport.Error("definition", "missing_definition", "CharacterPipelineDefinition 缺失。");
                return;
            }

            if (!TryBuildPatchJson(out string patchJson, m_LastReport))
                return;

            AgentAuthoringResponse response = new AgentPatchAuthoringService().Execute(new AgentAuthoringRequest
            {
                action = apply ? AgentAuthoringAction.ApplyPatch : AgentAuthoringAction.DryRunPatch,
                definitionAssetPath = AssetDatabase.GetAssetPath(m_Definition),
                patchJson = patchJson
            });
            m_LastReport = response.report;
        }

        bool TryBuildPatchJson(out string patchJson, AgentCompileReport report)
        {
            patchJson = m_Json;
            if (m_InputKind == InputKind.Patch)
                return true;

            if (!AgentAuthoringJsonUtility.TryFromJson(m_Json, out AgentControllerIntent intent, report, "intent-json"))
                return false;

            AgentGraphSnapshot snapshot = new AgentGraphSnapshotExporter().ExportFull(m_Definition);
            if (!new AgentMacroLibrary().TryExpand(intent, snapshot, out AgentPatchIR patch, report))
                return false;

            patchJson = AgentAuthoringJsonUtility.ToJson(patch);
            return true;
        }

        void Validate()
        {
            if (!m_Definition)
            {
                m_LastReport = new AgentCompileReport { success = false };
                m_LastReport.Error("definition", "missing_definition", "CharacterPipelineDefinition 缺失。");
                return;
            }

            AgentAuthoringResponse response = new AgentPatchAuthoringService().Execute(new AgentAuthoringRequest
            {
                action = AgentAuthoringAction.Validate,
                definitionAssetPath = AssetDatabase.GetAssetPath(m_Definition)
            });
            m_LastReport = response.report;
        }

        void Evaluate()
        {
            m_LastReport = !m_Definition
                ? new AgentCompileReport { success = false }
                : new AgentSynthesisEvaluator().EvaluateDefaultSamples(m_Definition);
            if (!m_Definition)
                m_LastReport.Error("definition", "missing_definition", "CharacterPipelineDefinition 缺失。");
        }

        void DrawReport()
        {
            if (m_LastReport == null)
                return;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(m_LastReport.success ? "Report: Success" : "Report: Issues", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Applied", m_LastReport.applied.ToString());
            EditorGUILayout.LabelField("Diff Size", m_LastReport.metrics.diffSize.ToString());

            for (int i = 0; i < m_LastReport.messages.Count; i++)
            {
                AgentCompileMessage message = m_LastReport.messages[i];
                MessageType type = message.severity == AgentReportSeverity.Error.ToString()
                    ? MessageType.Error
                    : message.severity == AgentReportSeverity.Warning.ToString()
                        ? MessageType.Warning
                        : MessageType.Info;
                EditorGUILayout.HelpBox($"{message.path}\n{message.code}: {message.message}\n{message.suggestion}", type);
            }
        }
    }
}
