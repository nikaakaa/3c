using System;
using System.IO;
using KK.GeneratedDiagnosticSampling.Host;
using ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.PresentationReplication.Editor
{
    public static class CharacterPresentationReplicationDiagnosticPlanCatalog
    {
        public const string CorePlanId =
            "character-presentation-replication-core-current";
        public const string FullPlanId =
            "character-presentation-replication-full-current";
        public const string CorePlanAssetPath =
            "Assets/GameScripts/Main/Editor/CharacterPipeline/Diagnostics/PresentationReplication/Plans/character-presentation-replication-core.current.json";
        public const string FullPlanAssetPath =
            "Assets/GameScripts/Main/Editor/CharacterPipeline/Diagnostics/PresentationReplication/Plans/character-presentation-replication-full.current.json";

        public static string RequireCurrentPlanAssetPath(string samplerId)
        {
            if (string.Equals(
                    samplerId,
                    CharacterPresentationReplicationDiagnosticIdentity.CoreSamplerId,
                    StringComparison.Ordinal))
            {
                return CorePlanAssetPath;
            }
            if (string.Equals(
                    samplerId,
                    CharacterPresentationReplicationDiagnosticIdentity.FullSamplerId,
                    StringComparison.Ordinal))
            {
                return FullPlanAssetPath;
            }
            throw new InvalidDataException(
                $"Presentation replication sampler has no current Plan: {samplerId}.");
        }

        public static DiagnosticAnalysisPlan OpenCurrent(
            string unityProjectRoot,
            string samplerId)
        {
            if (string.IsNullOrWhiteSpace(unityProjectRoot))
                throw new ArgumentException(
                    "Unity project root is required.",
                    nameof(unityProjectRoot));
            string path = Path.GetFullPath(Path.Combine(
                unityProjectRoot,
                RequireCurrentPlanAssetPath(samplerId)));
            DiagnosticAnalysisPlan plan = DiagnosticAnalysisPlanReader.Open(path);
            string expectedId = string.Equals(
                samplerId,
                CharacterPresentationReplicationDiagnosticIdentity.CoreSamplerId,
                StringComparison.Ordinal)
                ? CorePlanId
                : FullPlanId;
            if (!string.Equals(plan.Id, expectedId, StringComparison.Ordinal))
                throw new InvalidDataException(
                    "Presentation replication diagnostic plan identity is invalid.");
            return plan;
        }
    }
}
