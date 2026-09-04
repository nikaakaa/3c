using System;
using System.IO;
using System.Threading.Tasks;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonCharacter.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootIkSampling.Editor
{
    [InitializeOnLoad]
    public static class CharacterFootDiagnosticAnalysisWorkflow
    {
        sealed class Workflow : IDiagnosticAnalysisWorkflow
        {
            public string CapabilityId =>
                CharacterFootIkDiagnosticIdentity.CapabilityId;
            public bool IsAnalyzing =>
                CharacterFootDiagnosticAnalysisWorkflow.IsAnalyzing;
            public string LastResultDirectory =>
                CharacterFootDiagnosticAnalysisWorkflow.LastResultDirectory;
            public string LastReportPath =>
                CharacterFootDiagnosticAnalysisWorkflow.LastReportPath;
            public string LastQualityScorePath =>
                CharacterFootDiagnosticAnalysisWorkflow.LastQualityScorePath;
            public string LastFailure =>
                CharacterFootDiagnosticAnalysisWorkflow.LastFailure;
            public void AnalyzeLast() =>
                CharacterFootDiagnosticAnalysisWorkflow.AnalyzeLast();
            public void AnalyzeExisting(
                string manifestPath) =>
                CharacterFootDiagnosticAnalysisWorkflow.AnalyzeExisting(
                    manifestPath,
                    CurrentPlanPath(manifestPath));
            public void OpenLastReport() =>
                CharacterFootDiagnosticAnalysisWorkflow.OpenLastReport();
        }

        const string AnalyzeLastMenu =
            "Tools/3C/Diagnostics/Foot IK Generated Sampling/Analyze Last Capture";
        const string AnalyzeExistingMenu =
            "Tools/3C/Diagnostics/Foot IK Generated Sampling/Analyze Existing Capture";
        const string OpenReportMenu =
            "Tools/3C/Diagnostics/Foot IK Generated Sampling/Open Last Report";
        const string LastReportPreference =
            "ThirdPerson.Character.FootDiagnostics.LastReportPath";
        static readonly Workflow s_Workflow = new Workflow();
        static bool s_IsAnalyzing;
        static string s_LastResultDirectory = string.Empty;
        static string s_LastReportPath = string.Empty;
        static string s_LastQualityScorePath = string.Empty;
        static string s_LastFailure = string.Empty;
        static Task<DiagnosticAnalysisArtifacts> s_AnalysisTask;

        static CharacterFootDiagnosticAnalysisWorkflow()
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
        public static string LastQualityScorePath => s_LastQualityScorePath;
        public static string LastFailure => s_LastFailure;

        [MenuItem(AnalyzeLastMenu)]
        public static void AnalyzeLast()
        {
            string manifest =
                CharacterFootIkGeneratedSamplingWorkflow.LastManifestPath;
            if (!File.Exists(manifest))
            {
                throw new FileNotFoundException(
                    "Completed Foot IK capture manifest is unavailable.",
                    manifest);
            }
            AnalyzeExisting(
                manifest,
                CurrentPlanPath(manifest));
        }

        [MenuItem(AnalyzeLastMenu, true)]
        static bool CanAnalyzeLast() =>
            !s_IsAnalyzing &&
            File.Exists(
                CharacterFootIkGeneratedSamplingWorkflow.LastManifestPath);

        [MenuItem(AnalyzeExistingMenu)]
        static void AnalyzeExistingFromMenu()
        {
            string manifest = EditorUtility.OpenFilePanel(
                "Select Foot Capability Manifest",
                Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "..",
                    "Diagnostics")),
                "json");
            if (string.IsNullOrEmpty(manifest))
                return;
            AnalyzeExisting(
                manifest,
                CurrentPlanPath(manifest));
        }

        public static void AnalyzeExisting(
            string manifestPath,
            string planPath)
        {
            if (s_IsAnalyzing)
                throw new InvalidOperationException(
                    "Foot diagnostic analysis is already running.");
            s_IsAnalyzing = true;
            s_LastFailure = string.Empty;
            s_LastQualityScorePath = string.Empty;
            string fullManifestPath = Path.GetFullPath(manifestPath);
            string captureDirectory = Path.GetDirectoryName(fullManifestPath);
            string outputRoot = Path.Combine(
                Path.GetDirectoryName(captureDirectory),
                "FootAnalysis");
            string outputDirectory = Path.Combine(
                outputRoot,
                $"{Path.GetFileName(captureDirectory)}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
            string fullPlanPath = Path.GetFullPath(planPath);
            string analyzerAssemblyPath = typeof(CharacterFootDiagnosticAnalysis)
                .Assembly.Location;
            s_AnalysisTask = Task.Run(() =>
                CharacterFootDiagnosticAnalysis.Analyze(
                    fullManifestPath,
                    fullPlanPath,
                    analyzerAssemblyPath,
                    outputDirectory));
        }

        [MenuItem(OpenReportMenu)]
        public static void OpenLastReport()
        {
            if (!File.Exists(s_LastReportPath))
                throw new FileNotFoundException(
                    "Foot diagnostic report is unavailable.",
                    s_LastReportPath);
            EditorUtility.RevealInFinder(s_LastReportPath);
        }

        [MenuItem(OpenReportMenu, true)]
        static bool CanOpenLastReport() =>
            File.Exists(s_LastReportPath);

        static void RestoreLastReport()
        {
            string reportPath = ProjectEditorPreferences.GetString(
                LastReportPreference,
                string.Empty);
            if (!File.Exists(reportPath))
            {
                string root = Path.GetFullPath(Path.Combine(
                    Application.dataPath,
                    "..",
                    "Diagnostics",
                    "GeneratedFootSampling",
                    "FootAnalysis"));
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
            string qualityScorePath = Path.Combine(
                s_LastResultDirectory,
                CharacterFootDiagnosticAnalysis.QualityScoreFileName);
            s_LastQualityScorePath = File.Exists(qualityScorePath)
                ? qualityScorePath
                : string.Empty;
        }

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
                    "Foot diagnostic analysis requires one completed Sampler manifest.");
            }
            string assetPath = CharacterFootDiagnosticPlanCatalog
                .RequireCurrentPlanAssetPath(manifest.Samplers[0].SamplerId);
            return Path.GetFullPath(Path.Combine(
                Application.dataPath,
                "..",
                assetPath));
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
                s_LastQualityScorePath = Path.Combine(
                    artifacts.Directory,
                    CharacterFootDiagnosticAnalysis.QualityScoreFileName);
                if (!File.Exists(s_LastQualityScorePath))
                    s_LastQualityScorePath = string.Empty;
                ProjectEditorPreferences.SetString(
                    LastReportPreference,
                    Path.GetFullPath(s_LastReportPath));
                Debug.Log(
                    $"Foot diagnostic analysis completed: {s_LastReportPath}");
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
