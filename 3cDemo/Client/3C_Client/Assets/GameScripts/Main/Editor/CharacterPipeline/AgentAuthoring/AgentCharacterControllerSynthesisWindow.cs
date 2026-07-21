using System.IO;
using System.Text;
using ThirdPersonCharacter.AI;
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

        enum ControllerDomain
        {
            CharacterController,
            AIController
        }

        ControllerDomain m_Domain;
        UnityEngine.Object m_Root;
        InputKind m_InputKind;
        string m_Json = string.Empty;
        Vector2 m_Scroll;
        AgentCompileReport m_LastReport;

        public static void Open(CharacterPipelineDefinition definition)
        {
            AgentCharacterControllerSynthesisWindow window = GetWindow<AgentCharacterControllerSynthesisWindow>("Agent Controller");
            window.m_Domain = ControllerDomain.CharacterController;
            window.m_Root = definition;
            window.Show();
        }

        public static void Open(AIControllerDefinition definition)
        {
            AgentCharacterControllerSynthesisWindow window = GetWindow<AgentCharacterControllerSynthesisWindow>("Agent Controller");
            window.m_Domain = ControllerDomain.AIController;
            window.m_Root = definition;
            window.Show();
        }

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            m_Domain = (ControllerDomain)EditorGUILayout.EnumPopup("Domain", m_Domain);
            System.Type rootType = m_Domain == ControllerDomain.CharacterController ? typeof(CharacterPipelineDefinition) : typeof(AIControllerDefinition);
            if (m_Root && !rootType.IsInstanceOfType(m_Root))
                m_Root = null;
            m_Root = EditorGUILayout.ObjectField("Root Definition", m_Root, rootType, false);
            EditorGUILayout.LabelField("Schema", AgentAuthoringSchema.Version);
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
            using (new EditorGUI.DisabledScope(m_Domain != ControllerDomain.CharacterController))
            {
                if (GUILayout.Button("Evaluate"))
                    Evaluate();
            }
            EditorGUILayout.EndHorizontal();

            DrawReport();
            EditorGUILayout.EndScrollView();
        }

        void ExportSnapshot()
        {
            if (!m_Root)
                return;
            AgentAuthoringResponse response = Execute(AgentAuthoringAction.ExportSnapshot);
            if (response.success)
                AgentAuthoringJsonUtility.SaveJsonPanel("Export Agent Snapshot", $"{m_Root.name}_AgentSnapshot", response.snapshot);
            m_LastReport = response.report;
        }

        void ExportFullDebugSnapshot()
        {
            if (!m_Root)
                return;
            AgentAuthoringResponse response = Execute(AgentAuthoringAction.ExportSnapshot);
            if (response.success)
                AgentAuthoringJsonUtility.SaveJsonPanel("Export Full Agent Snapshot", $"{m_Root.name}_AgentSnapshot_FullDebug", response.snapshot);
            m_LastReport = response.report;
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
            if (!m_Root)
            {
                m_LastReport.Error("definition", "missing_definition", "Controller root definition 缺失。");
                return;
            }

            if (!TryBuildPatchJson(out string patchJson, m_LastReport))
                return;

            AgentAuthoringResponse response = Execute(apply ? AgentAuthoringAction.ApplyPatch : AgentAuthoringAction.DryRunPatch, patchJson);
            m_LastReport = response.report;
        }

        bool TryBuildPatchJson(out string patchJson, AgentCompileReport report)
        {
            patchJson = m_Json;
            if (m_InputKind == InputKind.Patch)
                return true;

            if (!AgentAuthoringJsonUtility.TryFromJson(m_Json, out AgentControllerIntent intent, report, "intent-json"))
                return false;

            AgentAuthoringResponse response = Execute(AgentAuthoringAction.ExportSnapshot);
            if (!response.success)
            {
                m_LastReport = response.report;
                return false;
            }
            AgentGraphSnapshot snapshot = response.snapshot;
            if (!new AgentMacroLibrary().TryExpand(intent, snapshot, out AgentPatchIR patch, report))
                return false;

            patchJson = AgentAuthoringJsonUtility.ToJson(patch);
            return true;
        }

        void Validate()
        {
            if (!m_Root)
            {
                m_LastReport = new AgentCompileReport { success = false };
                m_LastReport.Error("definition", "missing_definition", "Controller root definition 缺失。");
                return;
            }

            AgentAuthoringResponse response = Execute(AgentAuthoringAction.Validate);
            m_LastReport = response.report;
        }

        void Evaluate()
        {
            m_LastReport = m_Root is not CharacterPipelineDefinition definition
                ? new AgentCompileReport { success = false }
                : new AgentSynthesisEvaluator().EvaluateDefaultSamples(definition);
            if (m_Root is not CharacterPipelineDefinition)
                m_LastReport.Error("definition", "evaluation_domain_unsupported", "Evaluate samples 只适用于 CharacterController domain。");
        }

        AgentAuthoringResponse Execute(AgentAuthoringAction action, string patchJson = null)
        {
            return new AgentPatchAuthoringService().Execute(new AgentAuthoringRequest
            {
                action = action,
                domain = m_Domain.ToString(),
                rootAssetPath = m_Root ? AssetDatabase.GetAssetPath(m_Root) : string.Empty,
                patchJson = patchJson
            });
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
