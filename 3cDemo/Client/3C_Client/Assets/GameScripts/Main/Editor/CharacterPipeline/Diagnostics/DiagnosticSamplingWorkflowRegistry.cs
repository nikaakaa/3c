using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public readonly struct DiagnosticSamplingStartRequest
    {
        public DiagnosticSamplingStartRequest(
            bool controlledCaptureWindow,
            int minimumEventCount)
        {
            if (minimumEventCount < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumEventCount));
            ControlledCaptureWindow = controlledCaptureWindow;
            MinimumEventCount = minimumEventCount;
        }

        public bool ControlledCaptureWindow { get; }
        public int MinimumEventCount { get; }
    }

    public interface IDiagnosticSamplingWorkflow
    {
        string CapabilityId { get; }
        IReadOnlyList<string> SamplerIds { get; }
        string SelectedSamplerId { get; }
        bool IsCapturing { get; }
        bool IsFinalizing { get; }
        bool IsControlledCaptureWindow { get; }
        bool IsCaptureWindowOpen { get; }
        string CurrentSampleIdentity { get; }
        string LastSavedSampleIdentity { get; }
        string LastSavedPath { get; }
        string LastSavedDirectory { get; }
        string LastManifestPath { get; }
        string LastFailure { get; }
        int CapturedFrameCount { get; }
        int LastSavedFrameCount { get; }
        string GetArtifactPath(string artifactId);
        void SelectSampler(string samplerId);
        void Start(in DiagnosticSamplingStartRequest request);
        void OpenControlledCaptureWindow();
        void CloseControlledCaptureWindow();
        void StopAndSave();
    }

    public static class DiagnosticSamplingWorkflowRegistry
    {
        static readonly Dictionary<string, IDiagnosticSamplingWorkflow>
            s_Workflows =
                new Dictionary<string, IDiagnosticSamplingWorkflow>(
                    StringComparer.Ordinal);

        public static void Register(IDiagnosticSamplingWorkflow workflow)
        {
            if (workflow == null ||
                string.IsNullOrWhiteSpace(workflow.CapabilityId))
            {
                throw new ArgumentException(
                    "Diagnostic sampling workflow is invalid.",
                    nameof(workflow));
            }
            if (!s_Workflows.TryAdd(workflow.CapabilityId, workflow))
            {
                throw new InvalidOperationException(
                    $"Diagnostic sampling workflow is already registered: {workflow.CapabilityId}.");
            }
        }

        public static void Unregister(IDiagnosticSamplingWorkflow workflow)
        {
            if (workflow == null)
                return;
            if (s_Workflows.TryGetValue(
                    workflow.CapabilityId,
                    out IDiagnosticSamplingWorkflow current) &&
                ReferenceEquals(current, workflow))
            {
                s_Workflows.Remove(workflow.CapabilityId);
            }
        }

        public static bool TryGet(
            string capabilityId,
            out IDiagnosticSamplingWorkflow workflow) =>
            s_Workflows.TryGetValue(capabilityId, out workflow);

        public static IDiagnosticSamplingWorkflow Require(
            string capabilityId) =>
            TryGet(capabilityId, out IDiagnosticSamplingWorkflow workflow)
                ? workflow
                : throw new InvalidOperationException(
                    $"Diagnostic sampling capability is not compiled: {capabilityId}.");
    }
}
