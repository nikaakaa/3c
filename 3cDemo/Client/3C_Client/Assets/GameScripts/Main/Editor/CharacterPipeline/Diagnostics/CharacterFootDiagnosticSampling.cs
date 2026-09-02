namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterFootDiagnosticSampling
    {
        public const string CapabilityId = "character-foot-ik";

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

        public static void StartSampling() =>
            Require().Start(false);

        public static void StartControlledSampling() =>
            Require().Start(true);

        public static void OpenControlledCaptureWindow() =>
            Require().OpenControlledCaptureWindow();

        public static void CloseControlledCaptureWindow() =>
            Require().CloseControlledCaptureWindow();

        public static void StopAndSaveSampling() =>
            Require().StopAndSave();

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
    }
}
