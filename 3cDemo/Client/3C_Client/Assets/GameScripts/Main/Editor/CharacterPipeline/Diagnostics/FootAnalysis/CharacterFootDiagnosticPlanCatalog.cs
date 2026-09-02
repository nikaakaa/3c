using System;
using System.IO;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    public static class CharacterFootDiagnosticPlanCatalog
    {
        public const string CapabilityId = "character-foot-ik";
        public const string CoreSamplerId = "character-foot-ik/core";
        public const string FullSamplerId = "character-foot-ik/full";
        public const string CurrentCorePlanId = "character-foot-core-current";
        public const string CurrentFullPlanId = "character-foot-full-current";
        public const string CurrentCorePlanAssetPath =
            "Assets/GameScripts/Main/Editor/CharacterPipeline/Diagnostics/FootAnalysis/Plans/character-foot-core.current.json";
        public const string CurrentFullPlanAssetPath =
            "Assets/GameScripts/Main/Editor/CharacterPipeline/Diagnostics/FootAnalysis/Plans/character-foot-full.current.json";

        public static string RequireCurrentPlanAssetPath(string samplerId)
        {
            if (string.Equals(samplerId, CoreSamplerId, StringComparison.Ordinal))
                return CurrentCorePlanAssetPath;
            if (string.Equals(samplerId, FullSamplerId, StringComparison.Ordinal))
                return CurrentFullPlanAssetPath;
            throw new InvalidDataException(
                $"Foot diagnostic sampler has no current Plan: {samplerId}.");
        }

        public static DiagnosticAnalysisPlan OpenCurrent(
            string unityProjectRoot,
            string samplerId)
        {
            if (string.IsNullOrWhiteSpace(unityProjectRoot))
                throw new ArgumentException("Unity project root is required.", nameof(unityProjectRoot));
            string path = Path.GetFullPath(Path.Combine(
                unityProjectRoot,
                RequireCurrentPlanAssetPath(samplerId)));
            DiagnosticAnalysisPlan plan = DiagnosticAnalysisPlanReader.Open(path);
            string expectedId = string.Equals(
                samplerId,
                CoreSamplerId,
                StringComparison.Ordinal)
                ? CurrentCorePlanId
                : CurrentFullPlanId;
            if (!string.Equals(plan.Id, expectedId, StringComparison.Ordinal))
                throw new InvalidDataException("Foot diagnostic plan identity is invalid.");
            return plan;
        }
    }
}
