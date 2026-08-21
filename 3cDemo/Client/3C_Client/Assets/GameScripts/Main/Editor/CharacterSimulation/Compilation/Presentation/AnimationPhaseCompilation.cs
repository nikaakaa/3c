using System;
using System.Collections.Generic;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonSimulation;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    enum AnimationPhaseQualityFailureKind : byte
    {
        Coverage = 1,
        ContactSide = 2,
        TerminalPose = 3,
        WarpSlope = 4,
        QualityLimit = 5
    }

    sealed class AnimationPhaseQualityException : InvalidOperationException
    {
        public AnimationPhaseQualityException(
            AnimationPhaseQualityFailureKind kind,
            string message)
            : base($"[animation_phase_quality_{ToCode(kind)}] {message}")
        {
            Kind = kind;
        }

        public AnimationPhaseQualityFailureKind Kind { get; }

        static string ToCode(AnimationPhaseQualityFailureKind kind) =>
            kind switch
            {
                AnimationPhaseQualityFailureKind.Coverage => "coverage",
                AnimationPhaseQualityFailureKind.ContactSide => "contact_side",
                AnimationPhaseQualityFailureKind.TerminalPose => "terminal_pose",
                AnimationPhaseQualityFailureKind.WarpSlope => "warp_slope",
                AnimationPhaseQualityFailureKind.QualityLimit => "quality_limit",
                _ => "unknown"
            };
    }

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
            out AnimationSourcePhasePlan[] sourcePlans,
            out AnimationFootPhaseValidationDescriptor[] phaseValidations)
        {
            if (!profile)
                throw new ArgumentNullException(nameof(profile));
            var clips = new List<AnimationClipPhasePlan>();
            var sources = new List<AnimationSourcePhasePlan>();
            var validations = new List<AnimationFootPhaseValidationDescriptor>();
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
                    validations.Add(artifact.PhaseValidation);
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
                        validations.Add(artifact.PhaseValidation);
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
            phaseValidations = validations.ToArray();
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
                if (!HasOnset(expected, index, 2) ||
                    !HasPlantOnset(expected, index, 2) ||
                    HasOnset(opposing, index, 1))
                {
                    throw new AnimationPhaseQualityException(
                        AnimationPhaseQualityFailureKind.ContactSide,
                        $"AnimationClip '{clipName}' Phase {phase:R} at {time:R}s does not match the expected {(right ? "Right" : "Left")} Foot Landing/Plant onset.");
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

        static bool HasPlantOnset(
            AnimationFootPhaseValidationFootDescriptor descriptor,
            int center,
            int radius)
        {
            int first = Mathf.Max(0, center - radius);
            int last = Mathf.Min(descriptor.Samples.Count - 1, center + radius);
            for (int i = first; i <= last; i++)
            {
                if (descriptor.Samples[i].LandingOnset &&
                    descriptor.Samples[i].PlantConfidence >= 0.5f ||
                    i > 0 && descriptor.Samples[i - 1].PlantConfidence < 0.5f &&
                    descriptor.Samples[i].PlantConfidence >= 0.5f)
                {
                    return true;
                }
            }
            return false;
        }
    }

    static class AnimationPhaseRelationQualityCompiler
    {
        const string AlgorithmVersion = "animation-phase-relation-quality/v2";
        const float PlantThreshold = 0.5f;
        const float MaximumPlanarDifference = 0.35f;
        const float MaximumHeightDifference = 0.12f;
        const float MaximumVelocityDifference = 2.5f;
        const float MinimumInverseSlope = 0.01f;
        const float MaximumInverseSlope = 2.1f;

        readonly struct FootSample
        {
            public FootSample(Vector2 planar, float height, Vector3 velocity, float plant)
            {
                Planar = planar;
                Height = height;
                Velocity = velocity;
                Plant = plant;
            }

            public Vector2 Planar { get; }
            public float Height { get; }
            public Vector3 Velocity { get; }
            public float Plant { get; }
        }

        public static string Validate(
            string transitionId,
            AnimationClipPhasePlan sourcePlan,
            AnimationFootPhaseValidationDescriptor sourceValidation,
            AnimationPhaseCoverage sourceCoverage,
            AnimationClipPhasePlan targetPlan,
            AnimationFootPhaseValidationDescriptor targetValidation,
            AnimationPhaseCoverage targetCoverage,
            float blendDurationSeconds)
        {
            sourcePlan?.RequireValid();
            targetPlan?.RequireValid();
            sourceValidation?.RequireValid();
            targetValidation?.RequireValid();
            if (sourcePlan == null || targetPlan == null || sourceValidation == null || targetValidation == null)
                throw new AnimationPhaseQualityException(
                    AnimationPhaseQualityFailureKind.Coverage,
                    $"Transition '{transitionId}' Phase validation input is incomplete.");
            if (!sourceCoverage.IsValid || !targetCoverage.IsValid ||
                sourceCoverage.EndSeconds - sourceCoverage.StartSeconds < blendDurationSeconds ||
                targetCoverage.EndSeconds - targetCoverage.StartSeconds < blendDurationSeconds)
            {
                throw new AnimationPhaseQualityException(
                    AnimationPhaseQualityFailureKind.Coverage,
                    $"Transition '{transitionId}' visible Blend window is outside endpoint Phase coverage.");
            }

            ValidateInverseSlope(transitionId, sourcePlan, sourceCoverage);
            ValidateInverseSlope(transitionId, targetPlan, targetCoverage);
            if (!sourcePlan.Loop)
                ValidateFiniteTerminal(
                    transitionId,
                    sourcePlan,
                    sourceValidation,
                    sourceCoverage.EndSeconds,
                    targetPlan,
                    targetValidation,
                    targetCoverage);
            return StableHash.Compute(
                AlgorithmVersion,
                transitionId,
                sourcePlan.ValidationIdentity,
                targetPlan.ValidationIdentity).Value;
        }

        static void ValidateInverseSlope(
            string transitionId,
            AnimationClipPhasePlan plan,
            AnimationPhaseCoverage coverage)
        {
            for (int i = 1; i < plan.Knots.Count; i++)
            {
                AnimationPhaseKnot left = plan.Knots[i - 1];
                AnimationPhaseKnot right = plan.Knots[i];
                if (right.TimeSeconds < coverage.StartSeconds || left.TimeSeconds > coverage.EndSeconds)
                    continue;
                float slope = (right.TimeSeconds - left.TimeSeconds) /
                              (right.UnwrappedPhase - left.UnwrappedPhase);
                if (!float.IsFinite(slope) || slope < MinimumInverseSlope || slope > MaximumInverseSlope)
                {
                    throw new AnimationPhaseQualityException(
                        AnimationPhaseQualityFailureKind.WarpSlope,
                        $"Transition '{transitionId}' Clip '{plan.ClipIdentity}' inverse slope {slope:R}s/phase is outside [{MinimumInverseSlope:R}, {MaximumInverseSlope:R}].");
                }
            }
        }

        static void ValidateFiniteTerminal(
            string transitionId,
            AnimationClipPhasePlan sourcePlan,
            AnimationFootPhaseValidationDescriptor sourceValidation,
            float sourceTime,
            AnimationClipPhasePlan targetPlan,
            AnimationFootPhaseValidationDescriptor targetValidation,
            AnimationPhaseCoverage targetCoverage)
        {
            double phase = sourcePlan.Forward(sourceTime);
            double targetTime;
            try
            {
                targetTime = targetPlan.Inverse(phase, targetCoverage.StartSeconds);
            }
            catch (Exception exception)
            {
                throw new AnimationPhaseQualityException(
                    AnimationPhaseQualityFailureKind.Coverage,
                    $"Transition '{transitionId}' target cannot cover source terminal Phase {phase:R}: {exception.Message}");
            }
            if (!targetCoverage.Contains(targetTime))
            {
                throw new AnimationPhaseQualityException(
                    AnimationPhaseQualityFailureKind.Coverage,
                    $"Transition '{transitionId}' target time {targetTime:R}s is outside actual coverage.");
            }
            ValidateFootPair(
                transitionId,
                "Left",
                Sample(sourceValidation.Left, sourceValidation.DurationSeconds, sourceTime),
                Sample(targetValidation.Left, targetValidation.DurationSeconds, targetTime));
            ValidateFootPair(
                transitionId,
                "Right",
                Sample(sourceValidation.Right, sourceValidation.DurationSeconds, sourceTime),
                Sample(targetValidation.Right, targetValidation.DurationSeconds, targetTime));
        }

        static FootSample Sample(
            AnimationFootPhaseValidationFootDescriptor descriptor,
            float duration,
            double time)
        {
            float normalized = Mathf.Clamp01((float)(time / duration));
            float scaled = normalized * (descriptor.Samples.Count - 1);
            int first = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, descriptor.Samples.Count - 1);
            int second = Mathf.Min(first + 1, descriptor.Samples.Count - 1);
            float t = scaled - first;
            AnimationFootPhaseValidationSample left = descriptor.Samples[first];
            AnimationFootPhaseValidationSample right = descriptor.Samples[second];
            return new FootSample(
                Vector2.LerpUnclamped(left.RootLocalSolePlanarPosition, right.RootLocalSolePlanarPosition, t),
                Mathf.LerpUnclamped(left.CalibratedSoleHeight, right.CalibratedSoleHeight, t),
                Vector3.LerpUnclamped(left.SoleLocalVelocity, right.SoleLocalVelocity, t),
                Mathf.LerpUnclamped(left.PlantConfidence, right.PlantConfidence, t));
        }

        static void ValidateFootPair(
            string transitionId,
            string side,
            FootSample source,
            FootSample target)
        {
            bool sourcePlanted = source.Plant >= PlantThreshold;
            bool targetPlanted = target.Plant >= PlantThreshold;
            if (sourcePlanted != targetPlanted)
            {
                throw new AnimationPhaseQualityException(
                    AnimationPhaseQualityFailureKind.TerminalPose,
                    $"Transition '{transitionId}' {side} Foot Plant state differs at the finite source terminal.");
            }
            float planar = Vector2.Distance(source.Planar, target.Planar);
            float height = Mathf.Abs(source.Height - target.Height);
            float velocity = Vector3.Distance(source.Velocity, target.Velocity);
            if (planar > MaximumPlanarDifference ||
                height > MaximumHeightDifference ||
                velocity > MaximumVelocityDifference)
            {
                throw new AnimationPhaseQualityException(
                    AnimationPhaseQualityFailureKind.QualityLimit,
                    $"Transition '{transitionId}' {side} Foot exceeds fixed quality limits: planar={planar:R}m, height={height:R}m, velocity={velocity:R}m/s.");
            }
        }
    }
}
