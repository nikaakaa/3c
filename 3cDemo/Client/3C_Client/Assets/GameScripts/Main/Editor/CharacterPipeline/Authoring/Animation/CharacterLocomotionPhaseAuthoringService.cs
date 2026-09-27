using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static AnimationClipPhasePlan CompilePhasePlan(
            AnimationClip clip,
            AnimationFootAnalysisArtifact artifact)
        {
            CharacterAnimationClipContentIdentity identity = CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            AnimationCurve curve = CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                clip, CharacterAnimationClipRegisteredCurveChannels.LocomotionPhase);
            ValidateRegisteredCurve(clip, artifact, curve);
            Keyframe[] keys = curve.keys;
            var coverage = new AnimationPhaseCoverage(keys[0].time, keys[keys.Length - 1].time);
            var knots = new AnimationPhaseKnot[keys.Length];
            for (int i = 0; i < keys.Length; i++)
                knots[i] = new AnimationPhaseKnot(keys[i].time, keys[i].value);
            return new AnimationClipPhasePlan(
                identity.AssetGuid + ":" + identity.LocalFileId.ToString(CultureInfo.InvariantCulture),
                identity.FullDependencyHash,
                identity.AnalysisInputHash,
                identity.RegisteredCurveHash,
                artifact.Identity.IdentityHash.Value,
                identity.SourceDurationSeconds,
                coverage,
                identity.Loop,
                knots,
                BuildDoubleSupportCoverage(clip, artifact, coverage));
        }

        public static AnimationPhaseCoverage[] BuildDoubleSupportCoverage(
            AnimationClip clip,
            AnimationFootAnalysisArtifact artifact,
            AnimationPhaseCoverage coverage)
        {
            if (clip.isLooping)
                return Array.Empty<AnimationPhaseCoverage>();
            var left = artifact.PhaseValidation.Left.Samples;
            var right = artifact.PhaseValidation.Right.Samples;
            var intervals = new List<AnimationPhaseCoverage>();
            int start = -1;
            for (int i = 0; i <= left.Count; i++)
            {
                bool supporting = i < left.Count && left[i].IsSupporting && right[i].IsSupporting;
                if (supporting && start < 0)
                    start = i;
                if (supporting || start < 0)
                    continue;
                float from = Math.Max(coverage.StartSeconds, left[start].NormalizedTime * clip.length);
                float to = Math.Min(coverage.EndSeconds, left[i - 1].NormalizedTime * clip.length);
                if (to > from)
                    intervals.Add(new AnimationPhaseCoverage(from, to));
                start = -1;
            }
            return intervals.ToArray();
        }

        public static void ValidateRegisteredCurve(
            AnimationClip clip,
            AnimationFootAnalysisArtifact artifact,
            AnimationCurve curve)
        {
            CharacterAnimationClipRegisteredCurveCatalog.Validate(
                clip, CharacterAnimationClipRegisteredCurveChannels.LocomotionPhase, curve);
            Keyframe[] keys = curve.keys;
            CharacterLocomotionPhaseCandidate candidate = BuildCandidate(
                clip, artifact, keys[0].time, keys[keys.Length - 1].time);
            Keyframe[] expected = candidate.Curve.keys;
            float offset = Mathf.Round(curve.Evaluate(expected[0].time) - expected[0].value);
            for (int i = 0; i < expected.Length; i++)
            {
                float actual = curve.Evaluate(expected[i].time);
                if (Mathf.Abs(actual - expected[i].value - offset) > 0.0001f)
                    throw new InvalidOperationException(
                        $"Clip '{clip.name}' Locomotion Phase at {expected[i].time} seconds disagrees with measured Landing anchors: {actual}, expected {expected[i].value + offset}.");
            }
        }

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
