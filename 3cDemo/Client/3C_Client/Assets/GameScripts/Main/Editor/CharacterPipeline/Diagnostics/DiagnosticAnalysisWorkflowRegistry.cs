using System;
using System.Collections.Generic;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public interface IDiagnosticAnalysisWorkflow
    {
        string CapabilityId { get; }
        bool IsAnalyzing { get; }
        string LastResultDirectory { get; }
        string LastReportPath { get; }
        string LastFailure { get; }
        void AnalyzeLast();
        void AnalyzeExisting(string manifestPath);
        void OpenLastReport();
    }

    public static class DiagnosticAnalysisWorkflowRegistry
    {
        static readonly Dictionary<string, IDiagnosticAnalysisWorkflow>
            s_Workflows =
                new Dictionary<string, IDiagnosticAnalysisWorkflow>(
                    StringComparer.Ordinal);

        public static void Register(IDiagnosticAnalysisWorkflow workflow)
        {
            if (workflow == null ||
                string.IsNullOrWhiteSpace(workflow.CapabilityId))
            {
                throw new ArgumentException(
                    "Diagnostic analysis workflow is invalid.",
                    nameof(workflow));
            }
            if (!s_Workflows.TryAdd(workflow.CapabilityId, workflow))
            {
                throw new InvalidOperationException(
                    $"Diagnostic analysis workflow is already registered: {workflow.CapabilityId}.");
            }
        }

        public static bool TryGet(
            string capabilityId,
            out IDiagnosticAnalysisWorkflow workflow) =>
            s_Workflows.TryGetValue(capabilityId, out workflow);

        public static IDiagnosticAnalysisWorkflow Require(
            string capabilityId) =>
            TryGet(capabilityId, out IDiagnosticAnalysisWorkflow workflow)
                ? workflow
                : throw new InvalidOperationException(
                    $"Diagnostic analysis capability is not compiled: {capabilityId}.");
    }
}
