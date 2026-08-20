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
            var plan = new AnimationClipPhasePlan(
                $"{identity.AssetGuid}:{identity.LocalFileId}",
                identity.FullDependencyHash,
                identity.AnalysisInputHash,
                identity.RegisteredCurveHash,
                artifact.Identity.IdentityHash.Value,
                identity.SourceDurationSeconds,
                coverage,
                identity.Loop,
                knots);
            ValidateLandingOnsets(plan, artifact.PhaseValidation, clip.name);
            return plan;
        }

        internal static void CompileSources(
            CharacterAnimationPresentationProfile profile,
            IReadOnlyList<CharacterPresentationPoseSourcePlan> poseSources,
            IReadOnlyList<CharacterAnimationBlendSpacePlan> blendSpaces,
            IReadOnlyDictionary<PresentationPoseSourceIndex, int> blendSpacePlanBySource,
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
            foreach (KeyValuePair<PresentationPoseSourceIndex, int> pair in blendSpacePlanBySource)
            {
                if ((uint)pair.Value >= (uint)blendSpaces.Count)
                    throw new InvalidOperationException($"Blend Space source '{pair.Key}' plan index is invalid.");
                CharacterAnimationBlendSpacePlan blendSpace = blendSpaces[pair.Value];
                if (blendSpace.PhasePolicy != CharacterAnimationBlendSpacePhasePolicy.LocomotionPhase)
                    continue;
                var dynamicIndices = new List<int>();
                int referenceIndex = -1;
                AnimationPhaseCoverage coverage = default;
                for (int sampleIndex = 0; sampleIndex < blendSpace.Samples.Count; sampleIndex++)
                {
                    CharacterAnimationBlendSpaceSamplePlan sample = blendSpace.Samples[sampleIndex];
                    if (sample.Role != CharacterAnimationBlendSpaceSampleRole.DynamicCycle)
                        continue;
                    if (profile.FindLocomotionSyncGroup(sample.Clip) == null)
                        throw new InvalidOperationException($"Blend Space Sample '{sample.SampleId}' is not assigned to a Locomotion Sync Group.");
                    string bindingKey = AnimationFootAnalysisProjectionBuildData.BlendSpaceBindingKey(
                        blendSpace.BlendSpaceId,
                        sample.SampleId);
                    AnimationFootAnalysisArtifact artifact = footAnalysis?.RequireArtifact(bindingKey) ??
                        throw new InvalidOperationException($"Blend Space Sample '{sample.SampleId}' requires a Foot Analysis Artifact.");
                    if (!clipIndices.TryGetValue(sample.Clip, out int clipIndex))
                    {
                        clipIndex = clips.Count;
                        clips.Add(CompileClip(sample.Clip, artifact));
                        clipIndices.Add(sample.Clip, clipIndex);
                    }
                    dynamicIndices.Add(clipIndex);
                    if (sampleIndex == blendSpace.PhaseReferenceSampleIndex)
                    {
                        referenceIndex = clipIndex;
                        coverage = clips[clipIndex].CurveCoverage;
                    }
                }
                if (referenceIndex < 0 || !coverage.IsValid)
                    throw new InvalidOperationException($"Blend Space '{blendSpace.BlendSpaceId}' has no valid Phase Reference Sample.");
                sources.Add(new AnimationSourcePhasePlan(
                    pair.Key,
                    AnimationPhaseSourceKind.BlendSpace,
                    referenceIndex,
                    dynamicIndices.ToArray(),
                    coverage));
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

        static void ValidateLandingOnsets(
            AnimationClipPhasePlan plan,
            AnimationFootPhaseValidationDescriptor validation,
            string clipName)
        {
            validation?.RequireValid();
            if (validation == null ||
                Math.Abs(validation.DurationSeconds - plan.SourceDurationSeconds) > 0.0001f)
            {
                throw new InvalidOperationException("Animation Phase validation duration does not match the Clip plan.");
            }
            int firstHalf = Mathf.CeilToInt(plan.PhaseStart * 2f - 0.0001f);
            int lastHalf = Mathf.FloorToInt(plan.PhaseEnd * 2f + 0.0001f);
            for (int half = firstHalf; half <= lastHalf; half++)
            {
                float phase = half * 0.5f;
                bool right = (half & 1) == 0;
                double time = plan.Inverse(phase, 0d);
                if (time < plan.CurveCoverage.StartSeconds - 0.0001f ||
                    time > plan.CurveCoverage.EndSeconds + 0.0001f)
                {
                    continue;
                }
                AnimationFootPhaseValidationFootDescriptor expected = right
                    ? validation.Right
                    : validation.Left;
                AnimationFootPhaseValidationFootDescriptor opposing = right
                    ? validation.Left
                    : validation.Right;
                int index = Mathf.Clamp(
                    Mathf.RoundToInt((float)(time / plan.SourceDurationSeconds) * (expected.Samples.Count - 1)),
                    0,
                    expected.Samples.Count - 1);
                if (!HasOnset(expected, index, 2) || HasOnset(opposing, index, 1))
                {
                    throw new InvalidOperationException(
                        $"AnimationClip '{clipName}' Phase {phase:R} does not match the expected {(right ? "Right" : "Left")} Foot Landing onset.");
                }
            }
        }

        static bool HasOnset(
            AnimationFootPhaseValidationFootDescriptor descriptor,
            int center,
            int radius)
        {
            int first = Mathf.Max(0, center - radius);
            int last = Mathf.Min(descriptor.Samples.Count - 1, center + radius);
            for (int i = first; i <= last; i++)
            {
                if (descriptor.Samples[i].LandingOnset)
                    return true;
            }
            return false;
        }
    }
}
