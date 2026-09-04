namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterFootDiagnosticSampling
    {
        public const string CapabilityId = "character-foot-ik";
        public const string CoreSamplerId = "character-foot-ik/core";
        public const string FullSamplerId = "character-foot-ik/full";

        public static bool IsAvailable =>
            DiagnosticSamplingWorkflowRegistry.TryGet(
                CapabilityId,
                out _);
        public static bool IsCapturing =>
            TryGet(out IDiagnosticSamplingWorkflow workflow) &&
            workflow.IsCapturing;
        public static bool IsFinalizing =>
            TryGet(out IDiagnosticSamplingWorkflow workflow) &&
            workflow.IsFinalizing;
        public static bool IsControlledCaptureWindow =>
            TryGet(out IDiagnosticSamplingWorkflow workflow) &&
            workflow.IsControlledCaptureWindow;
        public static bool IsCaptureWindowOpen =>
            TryGet(out IDiagnosticSamplingWorkflow workflow) &&
            workflow.IsCaptureWindowOpen;
        public static string CurrentSampleIdentity =>
            TryGet(out IDiagnosticSamplingWorkflow workflow)
                ? workflow.CurrentSampleIdentity
                : string.Empty;
        public static string LastSavedSampleIdentity =>
            TryGet(out IDiagnosticSamplingWorkflow workflow)
                ? workflow.LastSavedSampleIdentity
                : string.Empty;
        public static string LastSavedPath =>
            TryGet(out IDiagnosticSamplingWorkflow workflow)
                ? workflow.LastSavedPath
                : string.Empty;
        public static string LastSavedDirectory =>
            TryGet(out IDiagnosticSamplingWorkflow workflow)
                ? workflow.LastSavedDirectory
                : string.Empty;
        public static string LastManifestPath =>
            TryGet(out IDiagnosticSamplingWorkflow workflow)
                ? workflow.LastManifestPath
                : string.Empty;
        public static string LastFailure =>
            TryGet(out IDiagnosticSamplingWorkflow workflow)
                ? workflow.LastFailure
                : string.Empty;
        public static int CapturedFrameCount =>
            TryGet(out IDiagnosticSamplingWorkflow workflow)
                ? workflow.CapturedFrameCount
                : 0;
        public static int LastSavedFrameCount =>
            TryGet(out IDiagnosticSamplingWorkflow workflow)
                ? workflow.LastSavedFrameCount
                : 0;
        public static string SelectedSamplerId =>
            TryGet(out IDiagnosticSamplingWorkflow workflow)
                ? workflow.SelectedSamplerId
                : string.Empty;
        public static bool IsAnalysisAvailable =>
            DiagnosticAnalysisWorkflowRegistry.TryGet(
                CapabilityId,
                out _);
        public static bool IsAnalyzing =>
            TryGetAnalysis(out IDiagnosticAnalysisWorkflow workflow) &&
            workflow.IsAnalyzing;
        public static string LastAnalysisDirectory =>
            TryGetAnalysis(out IDiagnosticAnalysisWorkflow workflow)
                ? workflow.LastResultDirectory
                : string.Empty;
        public static string LastReportPath =>
            TryGetAnalysis(out IDiagnosticAnalysisWorkflow workflow)
                ? workflow.LastReportPath
                : string.Empty;
        public static string LastQualityScorePath =>
            TryGetAnalysis(out IDiagnosticAnalysisWorkflow workflow)
                ? workflow.LastQualityScorePath
                : string.Empty;
        public static string LastAnalysisFailure =>
            TryGetAnalysis(out IDiagnosticAnalysisWorkflow workflow)
                ? workflow.LastFailure
                : string.Empty;

        public static void StartSampling()
        {
            var request = new DiagnosticSamplingStartRequest(false, 0);
            Require().Start(in request);
        }

        public static void SelectSampler(string samplerId) =>
            Require().SelectSampler(samplerId);

        public static void StartControlledSampling() =>
            StartControlledSampling(0);

        public static void StartControlledSampling(int minimumFrameCount)
        {
            var request = new DiagnosticSamplingStartRequest(
                true,
                minimumFrameCount);
            Require().Start(in request);
        }

        public static void OpenControlledCaptureWindow() =>
            Require().OpenControlledCaptureWindow();

        public static void CloseControlledCaptureWindow() =>
            Require().CloseControlledCaptureWindow();

        public static void StopAndSaveSampling() =>
            Require().StopAndSave();

        public static void AnalyzeLastCapture() =>
            RequireAnalysis().AnalyzeLast();

        public static void AnalyzeExistingCapture(
            string manifestPath) =>
            RequireAnalysis().AnalyzeExisting(manifestPath);

        public static void OpenLastReport() =>
            RequireAnalysis().OpenLastReport();

        public static string GetArtifactPath(string artifactId) =>
            TryGet(out IDiagnosticSamplingWorkflow workflow)
                ? workflow.GetArtifactPath(artifactId)
                : string.Empty;

        static bool TryGet(out IDiagnosticSamplingWorkflow workflow) =>
            DiagnosticSamplingWorkflowRegistry.TryGet(
                CapabilityId,
                out workflow);

        static IDiagnosticSamplingWorkflow Require() =>
            DiagnosticSamplingWorkflowRegistry.Require(CapabilityId);

        static bool TryGetAnalysis(
            out IDiagnosticAnalysisWorkflow workflow) =>
            DiagnosticAnalysisWorkflowRegistry.TryGet(
                CapabilityId,
                out workflow);

        static IDiagnosticAnalysisWorkflow RequireAnalysis() =>
            DiagnosticAnalysisWorkflowRegistry.Require(CapabilityId);
    }
}
