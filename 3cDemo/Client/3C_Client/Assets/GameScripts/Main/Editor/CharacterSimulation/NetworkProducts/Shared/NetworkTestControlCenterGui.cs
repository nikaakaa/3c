using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Editor.CharacterSimulation
{
    internal sealed class NetworkTestControlCenterGui
    {
        readonly NetworkTestControlCenter m_ControlCenter = new NetworkTestControlCenter();
        readonly Dictionary<string, int> m_CandidateSelections = new Dictionary<string, int>(StringComparer.Ordinal);
        readonly Dictionary<string, int> m_SlotSelections = new Dictionary<string, int>(StringComparer.Ordinal);
        string m_CandidateLabel = string.Empty;

        public bool Poll() => m_ControlCenter.Poll();

        public void Draw(bool editorBusy)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("3. 网络测试控制中心 / Network Test Control Center", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Build从干净Git提交创建不可变Candidate；Start只选择Candidate和正式Slot，不重新构建。Rollback两个Slot可并行。",
                    MessageType.Info);
                m_CandidateLabel = EditorGUILayout.TextField("Candidate Label", m_CandidateLabel);
                using (new EditorGUI.DisabledScope(editorBusy || m_ControlCenter.IsWorking))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        foreach (INetworkTestProductBuildAdapter adapter in NetworkTestProductAdapters.All)
                        {
                            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(m_CandidateLabel)))
                            {
                                if (GUILayout.Button($"Build {ShortName(adapter)}"))
                                    Execute(() => m_ControlCenter.Build(adapter, m_CandidateLabel));
                            }
                        }
                    }
                    if (GUILayout.Button("Refresh Candidate + Run Catalog"))
                        m_ControlCenter.Refresh();
                }
                EditorGUILayout.HelpBox(m_ControlCenter.Status,
                    m_ControlCenter.Status.Contains("无效") || m_ControlCenter.Status.Contains("失败")
                        ? MessageType.Warning
                        : MessageType.None);

                foreach (NetworkTestControlCenterProductSnapshot product in m_ControlCenter.Snapshot.Products)
                    DrawProduct(product, editorBusy);
            }
        }

        void DrawProduct(NetworkTestControlCenterProductSnapshot product, bool editorBusy)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(product.Adapter.DisplayName, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(product.Error))
            {
                EditorGUILayout.HelpBox(product.Error, MessageType.Error);
                return;
            }
            if (product.Candidates.Length == 0)
            {
                EditorGUILayout.LabelField("Candidate", "None");
                DrawRuns(product.Runs, editorBusy);
                return;
            }
            int candidateIndex = Mathf.Clamp(
                m_CandidateSelections.TryGetValue(product.Adapter.ProductId, out int selected) ? selected : 0,
                0,
                product.Candidates.Length - 1);
            string[] labels = product.Candidates.Select(value => value.Manifest.candidateId).ToArray();
            candidateIndex = EditorGUILayout.Popup("Candidate", candidateIndex, labels);
            m_CandidateSelections[product.Adapter.ProductId] = candidateIndex;
            NetworkTestCandidateCatalogEntry candidate = product.Candidates[candidateIndex];
            NetworkTestProductBuildManifest manifest = candidate.Manifest;
            EditorGUILayout.LabelField("Source Commit", manifest.sourceCommit);
            EditorGUILayout.LabelField("Program / Pipeline", $"{manifest.programIdentity}\n{manifest.pipelineIdentity}");
            EditorGUILayout.LabelField(
                "Tools",
                string.Join(", ", manifest.toolBundles.Select(value => $"{value.toolId}/{value.toolVersion}")));
            EditorGUILayout.LabelField("Built UTC / Files", $"{manifest.builtAtUtc} / Valid {manifest.files.Length}");

            string[] slots = manifest.sessionPlan.supportedSlotIds;
            int slotIndex = Mathf.Clamp(
                m_SlotSelections.TryGetValue(product.Adapter.ProductId, out int selectedSlot) ? selectedSlot : 0,
                0,
                slots.Length - 1);
            slotIndex = EditorGUILayout.Popup("Session Slot", slotIndex, slots);
            m_SlotSelections[product.Adapter.ProductId] = slotIndex;
            bool activeReference = product.Runs.Any(value =>
                value.IsActive && value.Manifest.candidateId == manifest.candidateId);
            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(editorBusy || m_ControlCenter.IsWorking))
            {
                if (GUILayout.Button("Start Session"))
                    Execute(() => m_ControlCenter.Start(product.Adapter, manifest.candidateId, slots[slotIndex]));
                using (new EditorGUI.DisabledScope(activeReference))
                {
                    if (GUILayout.Button("Remove Candidate") &&
                        EditorUtility.DisplayDialog(
                            "Remove Network Test Candidate",
                            $"删除不可变Candidate及其全部构建文件？\n{candidate.CandidateRoot}",
                            "Remove",
                            "Cancel"))
                        m_ControlCenter.Remove(product.Adapter, manifest.candidateId);
                }
            }
            DrawRuns(product.Runs, editorBusy);
        }

        static void DrawRuns(NetworkTestControlCenterRunSnapshot[] runs, bool editorBusy)
        {
            if (runs.Length == 0)
                return;
            EditorGUILayout.LabelField("Runs", EditorStyles.miniBoldLabel);
            foreach (NetworkTestControlCenterRunSnapshot run in runs)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField(
                        $"{run.Manifest.runId}  {run.Status.state}",
                        $"{run.Manifest.candidateId} / {run.Manifest.slotId}");
                    if (!string.IsNullOrWhiteSpace(run.Status.message))
                        EditorGUILayout.HelpBox(run.Status.message,
                            run.Status.state == "Faulted" ? MessageType.Error : MessageType.Warning);
                    EditorGUILayout.LabelField(
                        "Processes",
                        string.Join(", ", (run.Status.processes ?? Array.Empty<NetworkTestRunProcessDocument>())
                            .Select(value => $"{value.roleId}:{value.processId}")));
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool hasGm = (run.Status.processes ?? Array.Empty<NetworkTestRunProcessDocument>())
                            .Any(value => value != null && value.roleId == "gm");
                        using (new EditorGUI.DisabledScope(!hasGm))
                        {
                            if (GUILayout.Button("Open GM"))
                                Execute(() => NetworkTestControlCenter.OpenGm(run));
                        }
                        if (GUILayout.Button("Open Logs"))
                            Execute(() => NetworkTestControlCenter.OpenLogs(run));
                        using (new EditorGUI.DisabledScope(editorBusy || !run.IsActive))
                        {
                            if (GUILayout.Button("Stop Owned Session"))
                                Execute(() => NetworkTestControlCenter.Stop(run));
                        }
                    }
                }
            }
        }

        static string ShortName(INetworkTestProductBuildAdapter adapter) =>
            adapter == NetworkTestProductAdapters.DeterministicRollback ? "Rollback" :
            adapter == NetworkTestProductAdapters.UnityAuthority ? "Unity Authority" :
            "DotRecast";

        static void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Network Test Control Center", exception.Message, "OK");
            }
        }
    }
}
