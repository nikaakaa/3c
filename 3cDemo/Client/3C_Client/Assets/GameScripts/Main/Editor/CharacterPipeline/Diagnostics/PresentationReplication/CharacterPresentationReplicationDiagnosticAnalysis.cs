using System;
using System.IO;
using System.Linq;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication.Editor
{
    public static class CharacterPresentationReplicationDiagnosticAnalysis
    {
        public static DiagnosticAnalysisArtifacts Analyze(
            string capabilityManifestPath,
            string planPath,
            string analyzerAssemblyPath,
            string outputDirectory)
        {
            DiagnosticAnalysisPlan plan =
                DiagnosticAnalysisPlanReader.Open(planPath);
            if (plan.Datasets.Count != 1)
                throw new InvalidDataException(
                    "Presentation replication Plan must use exactly one dataset.");
            DiagnosticPlanDataset input = plan.Datasets.Single();
            if (!string.Equals(
                    input.CapabilityId,
                    CharacterPresentationReplicationDiagnosticIdentity.CapabilityId,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "Presentation replication Plan capability is invalid.");
            }
            DiagnosticDataset dataset = new DiagnosticArtifactDatasetReader().Open(
                capabilityManifestPath,
                input.SamplerId);
            var datasets = new DiagnosticDatasetSet(new[]
            {
                new DiagnosticNamedDataset(input.Id, dataset)
            });
            DiagnosticCompiledAnalysisPlan compiled =
                new DiagnosticAnalysisPlanCompiler(
                    CharacterPresentationReplicationDiagnosticOperatorCatalog.Create())
                .Compile(plan, datasets);
            DiagnosticAnalysisResult result = new DiagnosticAnalysisRunner().Run(
                compiled,
                DiagnosticAnalyzerBinaryIdentity.Open(analyzerAssemblyPath));
            return new DiagnosticAnalysisArtifactWriter().Publish(
                result,
                outputDirectory);
        }
    }
}
