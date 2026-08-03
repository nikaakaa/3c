using System.Collections.Generic;
using System.IO;
using System.Linq;
using ThirdPersonCharacter.AI;
using ThirdPersonCharacter.Pipeline;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor.AgentAuthoring
{
    public sealed class AgentCharacterControllerSynthesisWindow : EditorWindow
    {
        enum ControllerDomain
        {
            CharacterController,
            AIController
        }

        ControllerDomain m_Domain;
        UnityEngine.Object m_Root;
        Vector2 m_Scroll;
        AgentAuthoringResponse m_LastResponse;
        string m_ExpectedDocumentHash = string.Empty;
        bool m_ShowPlannedDiff;
        bool m_ShowAppliedDiff;

        public static void Open(CharacterPipelineDefinition definition)
        {
            AgentCharacterControllerSynthesisWindow window = GetWindow<AgentCharacterControllerSynthesisWindow>("Agent Document v3");
            window.m_Domain = ControllerDomain.CharacterController;
            window.m_Root = definition;
            window.Show();
        }

        public static void Open(AIControllerDefinition definition)
        {
            AgentCharacterControllerSynthesisWindow window = GetWindow<AgentCharacterControllerSynthesisWindow>("Agent Document v3");
            window.m_Domain = ControllerDomain.AIController;
            window.m_Root = definition;
            window.Show();
        }

        void OnGUI()
        {
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            m_Domain = (ControllerDomain)EditorGUILayout.EnumPopup("Domain", m_Domain);
            System.Type rootType = m_Domain == ControllerDomain.CharacterController
                ? typeof(CharacterPipelineDefinition)
                : typeof(AIControllerDefinition);
            if (m_Root && !rootType.IsInstanceOfType(m_Root))
                m_Root = null;
            m_Root = EditorGUILayout.ObjectField("Root Definition", m_Root, rootType, false);
            EditorGUILayout.LabelField("Schema", AgentAuthoringSchema.Version);
            EditorGUILayout.LabelField("Root Path", m_Root ? AssetDatabase.GetAssetPath(m_Root) : string.Empty);
            EditorGUILayout.LabelField("Root Identity", m_LastResponse?.rootIdentity ?? string.Empty);
            EditorGUILayout.LabelField("Package Path", m_LastResponse?.packagePath ?? string.Empty);
            EditorGUILayout.LabelField("Sync State", m_LastResponse?.syncState ?? string.Empty);
            EditorGUILayout.LabelField("Editable Hash", m_LastResponse?.editableHash ?? string.Empty);
            EditorGUILayout.LabelField("Context Hash", m_LastResponse?.contextHash ?? string.Empty);
            EditorGUILayout.LabelField("Document Hash", m_LastResponse?.documentHash ?? string.Empty);
            EditorGUILayout.LabelField("Plan Hash", m_LastResponse?.planHash ?? string.Empty);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Checkout"))
                Execute(AgentAuthoringAction.CheckoutDocument);
            if (GUILayout.Button("Open Package") &&
                !string.IsNullOrEmpty(m_LastResponse?.packagePath) &&
                Directory.Exists(m_LastResponse.packagePath))
                EditorUtility.RevealInFinder(m_LastResponse.packagePath);
            if (GUILayout.Button("Rebase"))
                Rebase();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Dry Run"))
                DryRun();
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(m_ExpectedDocumentHash)))
            {
                if (GUILayout.Button("Apply"))
                    Execute(AgentAuthoringAction.ApplyDocument, m_ExpectedDocumentHash);
            }
            if (GUILayout.Button("Validate"))
                Execute(AgentAuthoringAction.Validate);
            EditorGUILayout.EndHorizontal();

            DrawReport();
            EditorGUILayout.EndScrollView();
        }

        void DryRun()
        {
            AgentAuthoringResponse response = Execute(AgentAuthoringAction.DryRunDocument);
            m_ExpectedDocumentHash = response.success ? response.documentHash : string.Empty;
        }

        void Rebase()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebase Agent Document",
                    "接受当前Unity树与只读context作为新基线，并保留Document目标正文？",
                    "确认Rebase",
                    "取消"))
                return;
            Execute(AgentAuthoringAction.RebaseDocument, confirmRebase: true);
        }

        AgentAuthoringResponse Execute(
            AgentAuthoringAction action,
            string expectedDocumentHash = null,
            bool confirmRebase = false)
        {
            m_ExpectedDocumentHash = action == AgentAuthoringAction.ApplyDocument ? m_ExpectedDocumentHash : string.Empty;
            m_LastResponse = new AgentAuthoringDocumentApplicationService().Execute(new AgentAuthoringRequest
            {
                action = action,
                domain = m_Domain.ToString(),
                rootAssetPath = m_Root ? AssetDatabase.GetAssetPath(m_Root) : string.Empty,
                expectedDocumentHash = expectedDocumentHash,
                confirmRebase = confirmRebase
            });
            return m_LastResponse;
        }

        void DrawReport()
        {
            AgentCompileReport report = m_LastResponse?.report;
            if (report == null)
                return;

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(report.success ? "Report: Success" : "Report: Issues", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Applied", report.applied.ToString());
            EditorGUILayout.LabelField("Diff Size", report.metrics.diffSize.ToString());
            DrawPresentationSummary(report);
            m_ShowPlannedDiff = EditorGUILayout.Foldout(m_ShowPlannedDiff, $"Planned Diff ({report.plannedDiff.Count})");
            if (m_ShowPlannedDiff)
                DrawDiff(report.plannedDiff);
            m_ShowAppliedDiff = EditorGUILayout.Foldout(m_ShowAppliedDiff, $"Applied Diff ({report.appliedDiff.Count})");
            if (m_ShowAppliedDiff)
                DrawDiff(report.appliedDiff);
            for (int i = 0; i < report.messages.Count; i++)
            {
                AgentCompileMessage message = report.messages[i];
                MessageType type = message.severity == AgentReportSeverity.Error.ToString()
                    ? MessageType.Error
                    : message.severity == AgentReportSeverity.Warning.ToString()
                        ? MessageType.Warning
                        : MessageType.Info;
                EditorGUILayout.HelpBox($"{message.path}\n{message.code}: {message.message}\n{message.suggestion}", type);
            }
        }

        static void DrawPresentationSummary(AgentCompileReport report)
        {
            AgentCompileDiffEntry[] planned = report.plannedDiff
                .Where(IsPresentationDiff)
                .ToArray();
            AgentCompileDiffEntry[] applied = report.appliedDiff
                .Where(IsPresentationDiff)
                .ToArray();
            int profile = planned.Count(value =>
                value.target?.StartsWith(
                    "editable/presentation/profile.json",
                    System.StringComparison.Ordinal) == true);
            int stateMachines = planned.Count(value =>
                value.target?.StartsWith(
                    "editable/presentation/pose-state-machines/",
                    System.StringComparison.Ordinal) == true);
            int graphs = planned.Length - profile - stateMachines;
            EditorGUILayout.LabelField(
                "Presentation Dirty",
                planned.Length == 0
                    ? "Clean"
                    : $"Profile {profile}, Graph {graphs}, StateMachine {stateMachines}");
            EditorGUILayout.LabelField(
                "Presentation Applied",
                applied.Length.ToString());
            EditorGUILayout.LabelField(
                "Touched Owners",
                report.touchedOwners.Count.ToString());
        }

        static bool IsPresentationDiff(AgentCompileDiffEntry value) =>
            value?.mutationId?.StartsWith(
                "presentation-",
                System.StringComparison.Ordinal) == true;

        static void DrawDiff(IReadOnlyList<AgentCompileDiffEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                AgentCompileDiffEntry entry = entries[i];
                EditorGUILayout.LabelField(
                    $"{entry.mutationId} | {entry.action}",
                    $"{entry.graph} | {entry.target} | {entry.detail}",
                    EditorStyles.wordWrappedMiniLabel);
            }
        }
    }
}
