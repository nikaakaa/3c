using System;
using System.Collections.Generic;
using System.Linq;
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
        readonly struct LandingEvent
        {
            public LandingEvent(float time, bool right)
            {
                Time = time;
                Right = right;
            }

            public float Time { get; }
            public bool Right { get; }
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
            List<LandingEvent> events = CollectEvents(
                artifact.PhaseValidation,
                identity.SourceDurationSeconds,
                coverageStartSeconds,
                coverageEndSeconds);
            if (events.Count < 2)
                throw new InvalidOperationException("Locomotion Phase candidate requires at least two Landing onsets.");
            AnimationCurve curve = BuildCurve(
                events,
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

        static List<LandingEvent> CollectEvents(
            AnimationFootPhaseValidationDescriptor validation,
            float duration,
            float start,
            float end)
        {
            validation?.RequireValid();
            var result = new List<LandingEvent>();
            AddEvents(result, validation.Left, duration, start, end, false);
            AddEvents(result, validation.Right, duration, start, end, true);
            return result.OrderBy(value => value.Time).ThenBy(value => value.Right ? 0 : 1).ToList();
        }

        static void AddEvents(
            ICollection<LandingEvent> destination,
            AnimationFootPhaseValidationFootDescriptor descriptor,
            float duration,
            float start,
            float end,
            bool right)
        {
            for (int i = 0; i < descriptor.Samples.Count; i++)
            {
                if (!descriptor.Samples[i].LandingOnset)
                    continue;
                float time = descriptor.Samples[i].NormalizedTime * duration;
                if (time >= start && time <= end)
                    destination.Add(new LandingEvent(time, right));
            }
        }

        static AnimationCurve BuildCurve(
            IReadOnlyList<LandingEvent> events,
            float start,
            float end,
            bool loop)
        {
            var keys = new List<Keyframe>();
            float previousPhase = events[0].Right ? 0f : 0.5f;
            var eventPhases = new float[events.Count];
            eventPhases[0] = previousPhase;
            for (int i = 1; i < events.Count; i++)
            {
                float phase = events[i].Right
                    ? Mathf.Floor(previousPhase + 0.0001f) + 1f
                    : Mathf.Floor(previousPhase) + 0.5f;
                if (phase <= previousPhase + 0.0001f)
                    phase += 1f;
                eventPhases[i] = phase;
                previousPhase = phase;
            }
            float firstSlope = (eventPhases[1] - eventPhases[0]) /
                               (events[1].Time - events[0].Time);
            float lastSlope = (eventPhases[events.Count - 1] - eventPhases[events.Count - 2]) /
                              (events[events.Count - 1].Time - events[events.Count - 2].Time);
            float startPhase = eventPhases[0] - (events[0].Time - start) * firstSlope;
            float endPhase = eventPhases[eventPhases.Length - 1] +
                             (end - events[events.Count - 1].Time) * lastSlope;
            if (loop)
            {
                float span = Mathf.Max(1f, Mathf.Round(endPhase - startPhase));
                endPhase = startPhase + span;
            }
            keys.Add(new Keyframe(start, startPhase));
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].Time > start && events[i].Time < end)
                    keys.Add(new Keyframe(events[i].Time, eventPhases[i]));
            }
            keys.Add(new Keyframe(end, endPhase));
            var curve = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapMode.ClampForever
            };
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }
    }
}
