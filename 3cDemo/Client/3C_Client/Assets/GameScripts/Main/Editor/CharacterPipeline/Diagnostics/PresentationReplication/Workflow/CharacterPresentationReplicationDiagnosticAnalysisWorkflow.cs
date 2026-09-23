using System;
using System.IO;
using System.Threading.Tasks;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Editor;
using ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication.Editor
{
    [InitializeOnLoad]
    public static class CharacterPresentationReplicationDiagnosticAnalysisWorkflow
    {
        sealed class Workflow : IDiagnosticAnalysisWorkflow
        {
            public string CapabilityId =>
                CharacterPresentationReplicationDiagnosticIdentity.CapabilityId;
            public bool IsAnalyzing => s_IsAnalyzing;
            public string LastResultDirectory => s_LastResultDirectory;
            public string LastReportPath => s_LastReportPath;
            public string LastQualityScorePath => string.Empty;
            public string LastFailure => s_LastFailure;
            public void AnalyzeLast() => AnalyzeLastCapture();
            public void AnalyzeExisting(string manifestPath) =>
                AnalyzeExistingCapture(
                    manifestPath,
                    CurrentPlanPath(manifestPath));
            public void OpenLastReport() => OpenLastReportFile();
        }

        const string AnalyzeLastMenu =
            "Tools/3C/Diagnostics/Character Presentation Replication Sampling/Analyze Last Capture";
        const string AnalyzeExistingMenu =
            "Tools/3C/Diagnostics/Character Presentation Replication Sampling/Analyze Existing Capture";
        const string OpenReportMenu =
            "Tools/3C/Diagnostics/Character Presentation Replication Sampling/Open Last Report";
        const string LastReportPreference =
            "ThirdPerson.Character.PresentationReplicationDiagnostics.LastReportPath";
        static readonly Workflow s_Workflow = new Workflow();
        static bool s_IsAnalyzing;
        static string s_LastResultDirectory = string.Empty;
        static string s_LastReportPath = string.Empty;
        static string s_LastFailure = string.Empty;
        static Task<DiagnosticAnalysisArtifacts> s_AnalysisTask;
        static string AnalysisRoot => Path.GetFullPath(Path.Combine(
            Application.dataPath, "..", "Diagnostics", "PresentationReplicationAnalysis"));

        static CharacterPresentationReplicationDiagnosticAnalysisWorkflow()
        {
            DiagnosticAnalysisWorkflowRegistry.Register(s_Workflow);
            RestoreLastReport();
            EditorApplication.update += PollAnalysis;
            AssemblyReloadEvents.beforeAssemblyReload += WaitForAnalysis;
            EditorApplication.quitting += WaitForAnalysis;
        }

        public static bool IsAnalyzing => s_IsAnalyzing;
        public static string LastResultDirectory => s_LastResultDirectory;
        public static string LastReportPath => s_LastReportPath;
        public static string LastFailure => s_LastFailure;

        [MenuItem(AnalyzeLastMenu)]
        public static void AnalyzeLastCapture()
        {
            string manifest =
                CharacterNativePresentationSamplingWorkflow
                    .LastManifestPath;
            if (!File.Exists(manifest))
                throw new FileNotFoundException(
                    "Completed presentation replication capture manifest is unavailable.",
                    manifest);
            AnalyzeExistingCapture(
                manifest,
                CurrentPlanPath(manifest));
        }

        [MenuItem(AnalyzeLastMenu, true)]
        static bool CanAnalyzeLast() =>
            !s_IsAnalyzing &&
            File.Exists(
                CharacterNativePresentationSamplingWorkflow
                    .LastManifestPath);

        [MenuItem(AnalyzeExistingMenu)]
        static void AnalyzeExistingFromMenu()
        {
            string manifest = EditorUtility.OpenFilePanel(
                "Select Presentation Replication Capability Manifest",
                Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "..",
                    "Diagnostics")),
                "json");
            if (string.IsNullOrEmpty(manifest))
                return;
            AnalyzeExistingCapture(
                manifest,
                CurrentPlanPath(manifest));
        }

        public static void AnalyzeExistingCapture(
            string manifestPath,
            string planPath)
        {
            if (s_IsAnalyzing)
                throw new InvalidOperationException(
                    "Presentation replication diagnostic analysis is already running.");
            s_IsAnalyzing = true;
            s_LastFailure = string.Empty;
            string fullManifestPath = Path.GetFullPath(manifestPath);
            string outputDirectory = Path.Combine(
                AnalysisRoot, Guid.NewGuid().ToString("N"));
            string fullPlanPath = Path.GetFullPath(planPath);
            string analyzerAssemblyPath = typeof(
                CharacterPresentationReplicationDiagnosticAnalysis).Assembly.Location;
            s_AnalysisTask = Task.Run(() =>
                CharacterPresentationReplicationDiagnosticAnalysis.Analyze(
                    fullManifestPath,
                    fullPlanPath,
                    analyzerAssemblyPath,
                    outputDirectory));
        }

        [MenuItem(OpenReportMenu)]
        public static void OpenLastReportFile()
        {
            if (!File.Exists(s_LastReportPath))
                throw new FileNotFoundException(
                    "Presentation replication diagnostic report is unavailable.",
                    s_LastReportPath);
            EditorUtility.RevealInFinder(s_LastReportPath);
        }

        [MenuItem(OpenReportMenu, true)]
        static bool CanOpenLastReport() => File.Exists(s_LastReportPath);

        static string CurrentPlanPath(string manifestPath)
        {
            byte[] content = File.ReadAllBytes(Path.GetFullPath(manifestPath));
            var document = new DiagnosticEncodedDocument(
                content,
                DiagnosticArtifactIntegrity.ComputeSha256(content));
            DiagnosticCapabilityManifest manifest =
                DiagnosticCapabilityCodec.DecodeCapabilityManifest(document);
            if (manifest.Status != DiagnosticCaptureStatus.Completed ||
                manifest.Samplers.Count != 1)
            {
                throw new InvalidDataException(
                    "Presentation replication analysis requires one completed Sampler manifest.");
            }
            string assetPath = CharacterPresentationReplicationDiagnosticPlanCatalog
                .RequireCurrentPlanAssetPath(manifest.Samplers[0].SamplerId);
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                assetPath));
        }

        static void RestoreLastReport()
        {
            string reportPath = ProjectEditorPreferences.GetString(
                LastReportPreference,
                string.Empty);
            if (!File.Exists(reportPath))
            {
                string root = AnalysisRoot;
                if (Directory.Exists(root))
                {
                    DateTime latestWrite = DateTime.MinValue;
                    foreach (string directory in Directory.GetDirectories(root))
                    {
                        string candidate = Path.Combine(directory, "report.md");
                        if (!File.Exists(candidate))
                            continue;
                        DateTime write = File.GetLastWriteTimeUtc(candidate);
                        if (write <= latestWrite)
                            continue;
                        reportPath = candidate;
                        latestWrite = write;
                    }
                }
            }
            if (!File.Exists(reportPath))
                return;
            s_LastReportPath = Path.GetFullPath(reportPath);
            s_LastResultDirectory = Path.GetDirectoryName(s_LastReportPath);
        }

        static void PollAnalysis()
        {
            if (!s_IsAnalyzing || s_AnalysisTask == null ||
                !s_AnalysisTask.IsCompleted)
            {
                return;
            }
            Task<DiagnosticAnalysisArtifacts> task = s_AnalysisTask;
            s_AnalysisTask = null;
            try
            {
                DiagnosticAnalysisArtifacts artifacts =
                    task.GetAwaiter().GetResult();
                s_LastResultDirectory = artifacts.Directory;
                s_LastReportPath = artifacts.Report.Path;
                ProjectEditorPreferences.SetString(
                    LastReportPreference,
                    Path.GetFullPath(s_LastReportPath));
                Debug.Log(
                    $"Presentation replication diagnostic analysis completed: {s_LastReportPath}");
            }
            catch (Exception exception)
            {
                s_LastFailure = exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                s_IsAnalyzing = false;
            }
        }

        static void WaitForAnalysis()
        {
            if (s_AnalysisTask == null)
                return;
            try
            {
                s_AnalysisTask.GetAwaiter().GetResult();
            }
            catch
            {
            }
            s_AnalysisTask = null;
            s_IsAnalyzing = false;
        }
    }
}

