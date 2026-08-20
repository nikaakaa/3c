using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    static class AnimationPhasePlanCompiler
    {
        const int MaximumKnots = 256;
        const float ReductionTolerance = 0.0001f;

        internal static AnimationClipPhasePlan CompileClip(
            AnimationClip clip,
            AnimationFootAnalysisArtifact artifact)
        {
            if (!clip)
                throw new ArgumentNullException(nameof(clip));
            if (artifact == null)
                throw new ArgumentNullException(nameof(artifact));
            CharacterAnimationClipContentIdentity identity =
                CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(clip);
            if (!string.Equals(
                    identity.AnalysisInputHash,
                    artifact.Identity.ClipAnalysisInputHash,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"AnimationClip '{clip.name}' Analysis Input Hash does not match its Foot Analysis Artifact.");
            }
            AnimationCurve curve = CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                clip,
                CharacterAnimationClipRegisteredCurveChannels.LocomotionPhase);
            Keyframe[] keys = curve.keys;
            var coverage = new AnimationPhaseCoverage(
                keys[0].time,
                keys[keys.Length - 1].time);
            AnimationPhaseKnot[] knots = Reduce(curve, keys);
            return new AnimationClipPhasePlan(
                $"{identity.AssetGuid}:{identity.LocalFileId}",
                identity.FullDependencyHash,
                identity.AnalysisInputHash,
                identity.RegisteredCurveHash,
                artifact.Identity.IdentityHash.Value,
                identity.SourceDurationSeconds,
                coverage,
                identity.Loop,
                knots);
        }

        internal static void CompileDirectSources(
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<CharacterPresentationPoseSourcePlan> poseSources,
            CharacterFootPlacementAnalysisCompilation footAnalysis,
            out AnimationClipPhasePlan[] clipPlans,
            out AnimationSourcePhasePlan[] sourcePlans)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            var clips = new List<AnimationClipPhasePlan>();
            var sources = new List<AnimationSourcePhasePlan>();
            var clipIndices = new Dictionary<AnimationClip, int>();
            for (int i = 0; i < poseSources.Count; i++)
            {
                CharacterPresentationPoseSourcePlan source = poseSources[i];
                if (source == null || profile.FindLocomotionSyncGroup(source.Clip) == null)
                    continue;
                AnimationFootAnalysisArtifact artifact = footAnalysis?.RequireArtifact(
                    AnimationFootAnalysisProjectionBuildData.PoseSourceBindingKey(
                        source.BindingAssetIdentity)) ??
                    throw new InvalidOperationException(
                        $"Locomotion Clip '{source.DisplayName}' requires a Foot Analysis Artifact.");
                if (!clipIndices.TryGetValue(source.Clip, out int clipIndex))
                {
                    clipIndex = clips.Count;
                    clips.Add(CompileClip(source.Clip, artifact));
                    clipIndices.Add(source.Clip, clipIndex);
                }
                AnimationClipPhasePlan clipPlan = clips[clipIndex];
                sources.Add(new AnimationSourcePhasePlan(
                    source.SourceIndex,
                    AnimationPhaseSourceKind.DirectClip,
                    clipIndex,
                    new[] { clipIndex },
                    clipPlan.CurveCoverage));
            }
            clipPlans = clips.ToArray();
            sourcePlans = sources.ToArray();
        }

        static AnimationPhaseKnot[] Reduce(AnimationCurve curve, IReadOnlyList<Keyframe> keys)
        {
            var result = new List<AnimationPhaseKnot>();
            Add(result, curve, keys[0].time);
            for (int i = 0; i < keys.Count - 1; i++)
            {
                ReduceSegment(
                    curve,
                    keys[i].time,
                    keys[i].value,
                    keys[i + 1].time,
                    keys[i + 1].value,
                    result,
                    0);
            }
            if (result.Count > MaximumKnots)
                throw new InvalidOperationException(
                    $"Animation Phase reduction produced {result.Count} knots, exceeding {MaximumKnots}.");
            return result.ToArray();
        }

        static void ReduceSegment(
            AnimationCurve curve,
            float startTime,
            float startValue,
            float endTime,
            float endValue,
            List<AnimationPhaseKnot> result,
            int depth)
        {
            float midpointTime = (startTime + endTime) * 0.5f;
            float midpointValue = curve.Evaluate(midpointTime);
            float linear = (startValue + endValue) * 0.5f;
            if (Mathf.Abs(midpointValue - linear) > ReductionTolerance)
            {
                if (depth >= 20 || result.Count >= MaximumKnots)
                    throw new InvalidOperationException("Animation Phase reduction cannot satisfy its fixed error and capacity.");
                ReduceSegment(
                    curve,
                    startTime,
                    startValue,
                    midpointTime,
                    midpointValue,
                    result,
                    depth + 1);
                ReduceSegment(
                    curve,
                    midpointTime,
                    midpointValue,
                    endTime,
                    endValue,
                    result,
                    depth + 1);
                return;
            }
            Add(result, curve, endTime);
        }

        static void Add(List<AnimationPhaseKnot> result, AnimationCurve curve, float time)
        {
            float value = curve.Evaluate(time);
            if (result.Count > 0 && time <= result[result.Count - 1].TimeSeconds)
                return;
            result.Add(new AnimationPhaseKnot(time, value));
        }
    }
}
