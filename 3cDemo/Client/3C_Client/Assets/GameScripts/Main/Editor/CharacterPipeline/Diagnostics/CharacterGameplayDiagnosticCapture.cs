using System;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public static class CharacterGameplayDiagnosticCapture
    {
        public const string PresentationCapabilityId = "character-presentation-replication";
        static IDiagnosticSamplingWorkflow s_Foot;
        static IDiagnosticSamplingWorkflow s_Presentation;
        static bool s_OwnsCapture;

        public static bool IsAvailable => CharacterFootDiagnosticSampling.IsAvailable &&
            DiagnosticSamplingWorkflowRegistry.TryGet(PresentationCapabilityId, out _);
        public static bool IsCapturing => s_Foot != null && s_Presentation != null &&
            s_Foot.IsCapturing && s_Presentation.IsCapturing;
        public static bool IsFinalizing => s_Foot?.IsFinalizing == true || s_Presentation?.IsFinalizing == true;
        public static string LastFailure => !string.IsNullOrEmpty(s_Foot?.LastFailure)
            ? s_Foot.LastFailure : s_Presentation?.LastFailure ?? string.Empty;
        public static bool OwnsCapture => s_OwnsCapture;
        public static IDiagnosticSamplingWorkflow Presentation =>
            DiagnosticSamplingWorkflowRegistry.TryGet(PresentationCapabilityId, out var workflow) ? workflow : null;
        public static bool HasActiveCapture => CharacterFootDiagnosticSampling.IsCapturing ||
            CharacterFootDiagnosticSampling.IsFinalizing ||
            DiagnosticSamplingWorkflowRegistry.TryGet(PresentationCapabilityId, out var workflow) &&
            (workflow.IsCapturing || workflow.IsFinalizing);

        public static void Start(int minimumFrameCount) =>
            Start(new DiagnosticSamplingStartRequest(true, minimumFrameCount));

        public static void StartManual() => Start(new DiagnosticSamplingStartRequest(false, 0));

        static void Start(DiagnosticSamplingStartRequest request)
        {
            var foot = DiagnosticSamplingWorkflowRegistry.Require(CharacterFootDiagnosticSampling.CapabilityId);
            var presentation = DiagnosticSamplingWorkflowRegistry.Require(PresentationCapabilityId);
            if (foot.IsCapturing || foot.IsFinalizing || presentation.IsCapturing || presentation.IsFinalizing)
                throw new InvalidOperationException("已有采样正在运行或保存，请完成后再启动带诊断回放。");
            presentation.SelectSampler(foot.SelectedSamplerId == CharacterFootDiagnosticSampling.CoreSamplerId
                ? "character-presentation-replication/core" : "character-presentation-replication/full");
            s_Foot = foot;
            s_Presentation = presentation;
            foot.Start(in request);
            s_OwnsCapture = true;
            try
            {
                presentation.Start(in request);
            }
            catch
            {
                StopAndSave();
                throw;
            }
        }

        public static void Open()
        {
            s_Foot.OpenControlledCaptureWindow();
            s_Presentation.OpenControlledCaptureWindow();
        }

        public static void Close()
        {
            if (!s_OwnsCapture)
                return;
            if (s_Foot?.IsCaptureWindowOpen == true)
                s_Foot.CloseControlledCaptureWindow();
            if (s_Presentation?.IsCaptureWindowOpen == true)
                s_Presentation.CloseControlledCaptureWindow();
        }

        public static void StopAndSave()
        {
            if (!s_OwnsCapture)
                return;
            if (s_Foot?.IsCapturing == true)
                s_Foot.StopAndSave();
            if (s_Presentation?.IsCapturing == true)
                s_Presentation.StopAndSave();
            s_OwnsCapture = false;
        }
    }
}
