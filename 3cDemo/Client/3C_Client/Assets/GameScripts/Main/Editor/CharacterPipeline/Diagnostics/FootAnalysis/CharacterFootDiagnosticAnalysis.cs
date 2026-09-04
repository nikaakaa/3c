using System;
using System.IO;
using System.Linq;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    public static class CharacterFootDiagnosticAnalysis
    {
        public const string QualityScoreFileName =
            CharacterFootQualityScorePublisher.FileName;

        public static DiagnosticAnalysisArtifacts Analyze(
            string capabilityManifestPath,
            string planPath,
            string analyzerAssemblyPath,
            string outputDirectory)
        {
            DiagnosticAnalysisPlan plan = DiagnosticAnalysisPlanReader.Open(planPath);
            if (plan.Datasets.Count != 1)
                throw new InvalidDataException("Foot diagnostic plan must use exactly one dataset.");
            DiagnosticPlanDataset input = plan.Datasets.Single();
            if (!string.Equals(
                    input.CapabilityId,
                    CharacterFootDiagnosticPlanCatalog.CapabilityId,
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException("Foot diagnostic plan capability is invalid.");
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
                    CharacterFootDiagnosticOperatorCatalog.Create())
                .Compile(plan, datasets);
            DiagnosticAnalysisResult result = new DiagnosticAnalysisRunner().Run(
                compiled,
                DiagnosticAnalyzerBinaryIdentity.Open(analyzerAssemblyPath));
            DiagnosticAnalysisArtifacts artifacts = new DiagnosticAnalysisArtifactWriter().Publish(
                result,
                outputDirectory);
            CharacterFootQualityScorePublisher.Publish(
                result,
                plan,
                artifacts.Directory);
            return artifacts;
        }
    }
}
