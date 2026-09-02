using System;
using System.IO;
using KK.GeneratedDiagnosticSampling.Host;

namespace ThirdPersonCharacter.Pipeline.Diagnostics.FootAnalysis.Editor
{
    public static class CharacterFootDiagnosticPlanCatalog
    {
        public const string CapabilityId = "character-foot-ik";
        public const string CurrentContactAndLockPlanId =
            "character-foot-contact-and-lock-current";
        public const string CurrentContactAndLockPlanAssetPath =
            "Assets/GameScripts/Main/Editor/CharacterPipeline/Diagnostics/FootAnalysis/Plans/character-foot-contact-and-lock.current.json";

        public static DiagnosticAnalysisPlan OpenCurrentContactAndLock(string unityProjectRoot)
        {
            if (string.IsNullOrWhiteSpace(unityProjectRoot))
                throw new ArgumentException("Unity project root is required.", nameof(unityProjectRoot));
            string path = Path.GetFullPath(Path.Combine(
                unityProjectRoot,
                CurrentContactAndLockPlanAssetPath));
            DiagnosticAnalysisPlan plan = DiagnosticAnalysisPlanReader.Open(path);
            if (!string.Equals(plan.Id, CurrentContactAndLockPlanId, StringComparison.Ordinal))
                throw new InvalidDataException("Foot diagnostic plan identity is invalid.");
            return plan;
        }
    }
}
