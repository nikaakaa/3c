using System;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Simulation.Editor;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Editor
{
    public sealed class CharacterLocomotionPhaseCandidate
    {
        internal CharacterLocomotionPhaseCandidate(
            AnimationClip clip,
            string fullDependencyHash,
            string analysisInputHash,
            string registeredCurveHash,
            string artifactIdentity,
            AnimationCurve curve)
        {
            Clip = clip;
            FullDependencyHash = fullDependencyHash;
            AnalysisInputHash = analysisInputHash;
            RegisteredCurveHash = registeredCurveHash;
            ArtifactIdentity = artifactIdentity;
            Curve = curve;
        }

        public AnimationClip Clip { get; }
        public string FullDependencyHash { get; }
        public string AnalysisInputHash { get; }
        public string RegisteredCurveHash { get; }
        public string ArtifactIdentity { get; }
        public AnimationCurve Curve { get; }
    }

    public static class CharacterLocomotionPhaseAuthoringService
    {
        public static CharacterLocomotionPhaseCandidate BuildCandidate(
            AnimationClip clip,
            AnimationFootAnalysisArtifact artifact,
            float coverageStartSeconds,
            float coverageEndSeconds)
        {
            CharacterAnimationClipContentIdentity identity =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            if (artifact == null ||
                !string.Equals(
                    artifact.Identity.ClipAnalysisInputHash,
                    identity.AnalysisInputHash,
                    StringComparison.Ordinal) ||
                !float.IsFinite(coverageStartSeconds) ||
                !float.IsFinite(coverageEndSeconds) ||
                coverageStartSeconds < 0f ||
                coverageEndSeconds <= coverageStartSeconds ||
                coverageEndSeconds > identity.SourceDurationSeconds)
            {
                throw new InvalidOperationException("Locomotion Phase candidate input is stale or outside Clip coverage.");
            }
            AnimationCurve curve = CharacterLocomotionPhaseCurveBuilder.Build(
                artifact.PhaseValidation,
                identity.SourceDurationSeconds,
                coverageStartSeconds,
                coverageEndSeconds,
                identity.Loop);
            CharacterAnimationClipRegisteredCurveCatalog.Validate(
                clip,
                CharacterAnimationClipRegisteredCurveChannels.LocomotionPhase,
                curve);
            return new CharacterLocomotionPhaseCandidate(
                clip,
                identity.FullDependencyHash,
                identity.AnalysisInputHash,
                identity.RegisteredCurveHash,
                artifact.Identity.IdentityHash.Value,
                curve);
        }

        public static void Apply(
            CharacterLocomotionPhaseCandidate candidate,
            AnimationFootAnalysisArtifact artifact)
        {
            if (candidate == null || !candidate.Clip || artifact == null)
                throw new ArgumentException("Locomotion Phase candidate Apply input is incomplete.");
            CharacterAnimationClipContentIdentity identity =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(candidate.Clip);
            if (!string.Equals(identity.FullDependencyHash, candidate.FullDependencyHash, StringComparison.Ordinal) ||
                !string.Equals(identity.AnalysisInputHash, candidate.AnalysisInputHash, StringComparison.Ordinal) ||
                !string.Equals(identity.RegisteredCurveHash, candidate.RegisteredCurveHash, StringComparison.Ordinal) ||
                !string.Equals(artifact.Identity.IdentityHash.Value, candidate.ArtifactIdentity, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Locomotion Phase candidate became stale before Apply.");
            }
            Undo.RecordObject(candidate.Clip, "Apply Locomotion Phase Curve");
            CharacterAnimationClipRegisteredCurveCatalog.Replace(
                candidate.Clip,
                CharacterAnimationClipRegisteredCurveChannels.LocomotionPhase,
                candidate.Curve);
        }

    }
}
