using System;
using System.IO;
using KK.GeneratedDiagnosticSampling;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor;
using ThirdPersonCharacter.Pipeline.Editor;
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
        static string s_LastFailure = string.Empty;

        static CharacterFootDiagnosticAnalysisWorkflow()
        {
            DiagnosticAnalysisWorkflowRegistry.Register(s_Workflow);
            RestoreLastReport();
        }

        public static bool IsAnalyzing => s_IsAnalyzing;
        public static string LastResultDirectory => s_LastResultDirectory;
        public static string LastReportPath => s_LastReportPath;
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
            try
            {
                string captureDirectory = Path.GetDirectoryName(
                    Path.GetFullPath(manifestPath));
                string outputRoot = Path.Combine(
                    Path.GetDirectoryName(captureDirectory),
                    "FootAnalysis");
                string outputDirectory = Path.Combine(
                    outputRoot,
                    $"{Path.GetFileName(captureDirectory)}-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}");
                DiagnosticAnalysisArtifacts artifacts =
                    CharacterFootDiagnosticAnalysis.Analyze(
                        manifestPath,
                        planPath,
                        typeof(CharacterFootDiagnosticAnalysis)
                            .Assembly.Location,
                        outputDirectory);
                s_LastResultDirectory = artifacts.Directory;
                s_LastReportPath = artifacts.Report.Path;
                EditorPrefs.SetString(
                    LastReportPreference,
                    Path.GetFullPath(s_LastReportPath));
                Debug.Log(
                    $"Foot diagnostic analysis completed: {s_LastReportPath}");
            }
            catch (Exception exception)
            {
                s_LastFailure = exception.Message;
                throw;
            }
            finally
            {
                s_IsAnalyzing = false;
            }
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
            string reportPath = EditorPrefs.GetString(
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
    }
}
