using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonSimulation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public readonly struct CharacterFootAnalysisArtifactDiagnostic
    {
        public CharacterFootAnalysisArtifactDiagnostic(
            AnimationFootAnalysisArtifactStatus status,
            string bindingKey,
            string message)
        {
            Status = status;
            BindingKey = bindingKey ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public AnimationFootAnalysisArtifactStatus Status { get; }
        public string BindingKey { get; }
        public string Message { get; }
    }

    public sealed class CharacterFootPlacementAnalysisCompilation
    {
        readonly string[] m_RevisionTokens;

        public CharacterFootPlacementAnalysisCompilation(
            AnimationFootAnalysisProjectionBuildData buildData,
            IEnumerable<string> revisionTokens)
        {
            BuildData = buildData;
            m_RevisionTokens = revisionTokens?.ToArray() ?? throw new ArgumentNullException(nameof(revisionTokens));
        }

        public AnimationFootAnalysisProjectionBuildData BuildData { get; }
        public IReadOnlyList<string> RevisionTokens => m_RevisionTokens;
    }

    public static class CharacterProjectionFootAnalysisResolver
    {
        public static IReadOnlyList<CharacterFootAnalysisArtifactDiagnostic> InspectBlendSpace(
            CharacterAnimationPresentationProfile profile,
            CharacterAnimationBlendSpaceAsset asset)
        {
            var diagnostics = new List<CharacterFootAnalysisArtifactDiagnostic>();
            var errors = new List<string>();
            if (!profile || !asset)
            {
                diagnostics.Add(new CharacterFootAnalysisArtifactDiagnostic(
                    AnimationFootAnalysisArtifactStatus.Missing,
                    string.Empty,
                    "Blend Space Foot Analysis requires a Profile and asset."));
                return diagnostics;
            }
            if (profile.FootPlacementAnalysisMode == CharacterFootPlacementAnalysisMode.Disabled)
                return diagnostics;
            if (!TryResolveSource(profile, errors, out CharacterFootPlacementAnalysisSource source))
            {
                diagnostics.Add(new CharacterFootAnalysisArtifactDiagnostic(
                    AnimationFootAnalysisArtifactStatus.Missing,
                    string.Empty,
                    string.Join("\n", errors)));
                return diagnostics;
            }
            for (int i = 0; i < asset.Samples.Count; i++)
            {
                CharacterAnimationBlendSpaceSample sample = asset.Samples[i];
                string bindingKey = sample != null && sample.SampleId.IsValid
                    ? AnimationFootAnalysisProjectionBuildData.BlendSpaceBindingKey(asset.BlendSpaceId, sample.SampleId)
                    : string.Empty;
                if (sample == null || !sample.SampleId.IsValid || !sample.Clip)
                {
                    diagnostics.Add(new CharacterFootAnalysisArtifactDiagnostic(
                        AnimationFootAnalysisArtifactStatus.Missing,
                        bindingKey,
                        $"Blend Space Sample #{i} has no valid identity or AnimationClip."));
                    continue;
                }
                try
                {
                    AnimationFootAnalysisArtifactIdentity expected =
                        AnimationFootAnalysisArtifactBuilder.GetExpectedIdentity(sample.Clip, source);
                    AnimationFootAnalysisArtifactInspection inspection =
                        AnimationFootAnalysisArtifactStore.Inspect(expected);
                    diagnostics.Add(new CharacterFootAnalysisArtifactDiagnostic(
                        inspection.Status,
                        bindingKey,
                        $"{sample.SampleId} / {sample.Clip.name} / {expected.IdentityHash.Value} / {inspection.Error}"));
                }
                catch (Exception exception)
                {
                    diagnostics.Add(new CharacterFootAnalysisArtifactDiagnostic(
                        AnimationFootAnalysisArtifactStatus.Corrupt,
                        bindingKey,
                        $"{sample.SampleId} / {sample.Clip.name} / {exception.Message}"));
                }
            }
            return diagnostics;
        }

        readonly struct ClipBinding
        {
            public ClipBinding(
                string timelineAuthoringId,
                string trackAuthoringId,
                string clipAuthoringId,
                UnityEngine.AnimationClip clip)
            {
                TimelineAuthoringId = timelineAuthoringId;
                TrackAuthoringId = trackAuthoringId;
                ClipAuthoringId = clipAuthoringId;
                Clip = clip;
                BlendSpaceId = default;
                SampleId = default;
            }

            public ClipBinding(
                CharacterAnimationBlendSpaceId blendSpaceId,
                CharacterAnimationBlendSpaceSampleId sampleId,
                UnityEngine.AnimationClip clip)
            {
                if (!blendSpaceId.IsValid || !sampleId.IsValid)
                    throw new ArgumentException("Blend Space Foot Analysis binding identity is invalid.");
                TimelineAuthoringId = string.Empty;
                TrackAuthoringId = string.Empty;
                ClipAuthoringId = string.Empty;
                Clip = clip;
                BlendSpaceId = blendSpaceId;
                SampleId = sampleId;
            }

            public string TimelineAuthoringId { get; }
            public string TrackAuthoringId { get; }
            public string ClipAuthoringId { get; }
            public UnityEngine.AnimationClip Clip { get; }
            public CharacterAnimationBlendSpaceId BlendSpaceId { get; }
            public CharacterAnimationBlendSpaceSampleId SampleId { get; }
            public bool IsBlendSpace => BlendSpaceId.IsValid;
            public string BindingKey => IsBlendSpace
                ? AnimationFootAnalysisProjectionBuildData.BlendSpaceBindingKey(BlendSpaceId, SampleId)
                : AnimationFootAnalysisProjectionBuildData.BindingKey(
                    TimelineAuthoringId,
                    TrackAuthoringId,
                    ClipAuthoringId);
            public string DisplayIdentity => IsBlendSpace
                ? $"Blend Space '{BlendSpaceId}' Sample '{SampleId}'"
                : $"Timeline '{TimelineAuthoringId}' Track '{TrackAuthoringId}' Clip '{ClipAuthoringId}'";
        }

        public static CharacterFootPlacementAnalysisCompilation Resolve(
            CharacterAnimationPresentationProfile profile,
            IReadOnlyDictionary<string, TimelineData> timelines,
            bool generateMissingOrStale,
            List<CharacterFootAnalysisArtifactDiagnostic> diagnostics,
            List<string> errors)
        {
            errors ??= new List<string>();
            if (!profile)
            {
                errors.Add("Foot Analysis requires an Animation Presentation Profile.");
                return null;
            }
            if (profile.FootPlacementAnalysisMode == CharacterFootPlacementAnalysisMode.Disabled)
                return new CharacterFootPlacementAnalysisCompilation(null, new[] { "foot-analysis/disabled" });
            if (profile.FootPlacementAnalysisMode != CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures)
            {
                errors?.Add($"Foot Analysis mode '{profile.FootPlacementAnalysisMode}' is unsupported.");
                return null;
            }
            if (!TryResolveSource(profile, errors, out CharacterFootPlacementAnalysisSource source))
                return null;

            List<ClipBinding> bindings = CollectAuthoringBindings(profile, timelines, errors);
            if (bindings.Count == 0)
            {
                errors?.Add("Foot Analysis found no reachable Animation Clip binding.");
                return null;
            }
            var artifacts = new Dictionary<string, AnimationFootAnalysisArtifact>(StringComparer.Ordinal);
            var features = new Dictionary<string, AnimationFootFeaturePair>(StringComparer.Ordinal);
            bool artifactFailure = false;
            for (int i = 0; i < bindings.Count; i++)
            {
                ClipBinding binding = bindings[i];
                try
                {
                    AnimationFootAnalysisArtifactIdentity expected =
                        AnimationFootAnalysisArtifactBuilder.GetExpectedIdentity(binding.Clip, source);
                    if (!artifacts.TryGetValue(expected.IdentityHash.Value, out AnimationFootAnalysisArtifact artifact))
                    {
                        AnimationFootAnalysisArtifactInspection inspection =
                            AnimationFootAnalysisArtifactStore.Inspect(expected);
                        if (inspection.Status == AnimationFootAnalysisArtifactStatus.Corrupt)
                        {
                            diagnostics?.Add(new CharacterFootAnalysisArtifactDiagnostic(
                                inspection.Status,
                                binding.BindingKey,
                                FormatIssue(binding, expected, inspection.Status, inspection.Error)));
                            artifactFailure = true;
                            continue;
                        }
                        if (inspection.Status is AnimationFootAnalysisArtifactStatus.Missing or AnimationFootAnalysisArtifactStatus.Stale)
                        {
                            if (!generateMissingOrStale)
                            {
                                diagnostics?.Add(new CharacterFootAnalysisArtifactDiagnostic(
                                    inspection.Status,
                                    binding.BindingKey,
                                    FormatIssue(binding, expected, inspection.Status, inspection.Error)));
                                artifactFailure = true;
                                continue;
                            }
                            artifact = AnimationFootAnalysisArtifactBuilder.Build(binding.Clip, source);
                        }
                        else
                        {
                            artifact = inspection.Artifact;
                        }
                        if (artifact == null || !artifact.Identity.EqualsExact(expected))
                            throw new InvalidOperationException("Resolved artifact did not preserve the expected identity.");
                        artifacts.Add(expected.IdentityHash.Value, artifact);
                    }
                    if (!features.TryAdd(binding.BindingKey, artifact.Features))
                        errors?.Add($"Foot Analysis stable clip binding '{binding.BindingKey}' is duplicated.");
                }
                catch (Exception exception)
                {
                    errors?.Add(
                        $"Foot Analysis binding {binding.DisplayIdentity} AnimationClip '{binding.Clip?.name}' Source '{source.AnalysisSourceId}' Rig '{source.SamplingRigAssetGuid}' Calibration '{source.RigCalibration.CalibrationId}@{source.RigCalibration.ContentRevision}' failed: {exception.Message}");
                }
            }
            if (artifactFailure || errors.Count > 0 || features.Count != bindings.Count)
                return null;

            string[] artifactTokens = BuildArtifactTokens(artifacts.Values);
            StableHash aggregateHash = StableHash.Compute(artifactTokens);
            var identity = new AnimationFootAnalysisProjectionIdentity(
                CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures,
                source.AnalysisSourceId.Value,
                source.AnalysisVersion,
                CharacterFootPlacementAnalysisSource.AlgorithmVersion,
                source.RigCalibration.CalibrationId,
                source.RigCalibration.ContentRevision,
                aggregateHash.Value);
            var revisionTokens = new List<string> { "foot-analysis-artifacts/v1", aggregateHash.Value };
            revisionTokens.AddRange(artifactTokens);
            return new CharacterFootPlacementAnalysisCompilation(
                new AnimationFootAnalysisProjectionBuildData(identity, features),
                revisionTokens);
        }

        public static bool TryBuildPublishedRevisionTokens(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationProjection projection,
            List<string> errors,
            out string[] revisionTokens)
        {
            errors ??= new List<string>();
            revisionTokens = null;
            if (!profile || projection == null || !projection.IsValid)
            {
                errors?.Add("Foot Analysis stale inspection requires a valid Profile and Projection.");
                return false;
            }
            if (profile.FootPlacementAnalysisMode == CharacterFootPlacementAnalysisMode.Disabled)
            {
                if (projection.FootAnalysis != null && projection.FootAnalysis.IsEnabled)
                {
                    errors?.Add("Projection retains Foot Analysis while the Profile is disabled.");
                    return false;
                }
                revisionTokens = new[] { "foot-analysis/disabled" };
                return true;
            }
            if (!TryResolveSource(profile, errors, out CharacterFootPlacementAnalysisSource source))
                return false;
            List<ClipBinding> bindings = CollectProjectionBindings(projection, errors);
            var artifacts = new Dictionary<string, AnimationFootAnalysisArtifact>(StringComparer.Ordinal);
            for (int i = 0; i < bindings.Count; i++)
            {
                ClipBinding binding = bindings[i];
                try
                {
                    AnimationFootAnalysisArtifactIdentity expected =
                        AnimationFootAnalysisArtifactBuilder.GetExpectedIdentity(binding.Clip, source);
                    if (artifacts.ContainsKey(expected.IdentityHash.Value))
                        continue;
                    AnimationFootAnalysisArtifactInspection inspection =
                        AnimationFootAnalysisArtifactStore.Inspect(expected);
                    if (inspection.Status != AnimationFootAnalysisArtifactStatus.Ready)
                    {
                        errors?.Add(FormatIssue(binding, expected, inspection.Status, inspection.Error));
                        continue;
                    }
                    artifacts.Add(expected.IdentityHash.Value, inspection.Artifact);
                }
                catch (Exception exception)
                {
                    errors?.Add($"Foot Analysis stale inspection failed for '{binding.BindingKey}': {exception.Message}");
                }
            }
            if (errors.Count > 0 || artifacts.Count == 0)
                return false;
            string[] artifactTokens = BuildArtifactTokens(artifacts.Values);
            StableHash aggregateHash = StableHash.Compute(artifactTokens);
            if (projection.FootAnalysis == null || !projection.FootAnalysis.IsEnabled ||
                !string.Equals(projection.FootAnalysis.AnalysisSourceId, source.AnalysisSourceId.Value, StringComparison.Ordinal) ||
                projection.FootAnalysis.AnalysisVersion != source.AnalysisVersion ||
                !string.Equals(projection.FootAnalysis.AlgorithmVersion, CharacterFootPlacementAnalysisSource.AlgorithmVersion, StringComparison.Ordinal) ||
                !string.Equals(projection.FootAnalysis.CalibrationId.Value, source.RigCalibration.CalibrationId.Value, StringComparison.Ordinal) ||
                !string.Equals(projection.FootAnalysis.CalibrationRevision, source.RigCalibration.ContentRevision, StringComparison.Ordinal) ||
                !string.Equals(projection.FootAnalysis.ArtifactContentHash, aggregateHash.Value, StringComparison.Ordinal))
            {
                errors?.Add("Projection Foot Analysis identity does not match the current artifact set.");
                return false;
            }
            var tokens = new List<string> { "foot-analysis-artifacts/v1", aggregateHash.Value };
            tokens.AddRange(artifactTokens);
            revisionTokens = tokens.ToArray();
            return true;
        }

        static bool TryResolveSource(
            CharacterAnimationPresentationProfile profile,
            List<string> errors,
            out CharacterFootPlacementAnalysisSource source)
        {
            source = null;
            string sourceGuid = profile.FootPlacementAnalysisSourceAssetGuid;
            string sourcePath = CharacterFootPlacementAnalysisSource.IsAssetGuid(sourceGuid)
                ? AssetDatabase.GUIDToAssetPath(sourceGuid)
                : string.Empty;
            source = string.IsNullOrEmpty(sourcePath)
                ? null
                : AssetDatabase.LoadAssetAtPath<CharacterFootPlacementAnalysisSource>(sourcePath);
            if (!source)
            {
                errors?.Add($"Foot Analysis Source GUID '{sourceGuid}' does not resolve to a unique Source asset.");
                return false;
            }
            try
            {
                source.RequireValid();
                return true;
            }
            catch (Exception exception)
            {
                errors?.Add($"Foot Analysis Source '{sourcePath}' is invalid: {exception.Message}");
                return false;
            }
        }

        static List<ClipBinding> CollectAuthoringBindings(
            CharacterAnimationPresentationProfile profile,
            IReadOnlyDictionary<string, TimelineData> timelines,
            List<string> errors)
        {
            var result = new List<ClipBinding>();
            foreach (KeyValuePair<string, TimelineData> pair in timelines ??
                         new Dictionary<string, TimelineData>())
            {
                TimelineData timeline = pair.Value;
                if (timeline == null || !string.Equals(pair.Key, timeline.AuthoringId, StringComparison.Ordinal))
                {
                    errors?.Add($"Foot Analysis Timeline inventory entry '{pair.Key}' is invalid.");
                    continue;
                }
                for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
                {
                    if (timeline.Tracks[trackIndex] is not AnimationTrack track)
                        continue;
                    for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                    {
                        if (track.Clips[clipIndex] is not BTSMTL.Timeline.AnimationClip clip)
                            continue;
                        if (!clip.Clip)
                        {
                            errors?.Add($"Foot Analysis Timeline '{timeline.AuthoringId}' Track '{track.AuthoringId}' Clip '{clip.AuthoringId}' has no AnimationClip resource.");
                            continue;
                        }
                        result.Add(new ClipBinding(timeline.AuthoringId, track.AuthoringId, clip.AuthoringId, clip.Clip));
                    }
                }
            }
            var blendSpaceBindings = new HashSet<string>(StringComparer.Ordinal);
            for (int bindingIndex = 0; bindingIndex < profile.ProducerBindings.Count; bindingIndex++)
            {
                AnimationProducerPresentationBinding producerBinding = profile.ProducerBindings[bindingIndex];
                if (producerBinding == null || producerBinding.SourceKind != AnimationPoseSourceKind.BlendSpace ||
                    !producerBinding.BlendSpaceSource)
                    continue;
                CharacterAnimationBlendSpaceAsset blendSpace = producerBinding.BlendSpaceSource;
                for (int sampleIndex = 0; sampleIndex < blendSpace.Samples.Count; sampleIndex++)
                {
                    CharacterAnimationBlendSpaceSample sample = blendSpace.Samples[sampleIndex];
                    if (sample == null || !sample.SampleId.IsValid || !sample.Clip)
                    {
                        errors?.Add($"Foot Analysis Blend Space '{blendSpace.name}' Sample #{sampleIndex} is incomplete.");
                        continue;
                    }
                    var binding = new ClipBinding(blendSpace.BlendSpaceId, sample.SampleId, sample.Clip);
                    if (blendSpaceBindings.Add(binding.BindingKey))
                        result.Add(binding);
                }
            }
            SortBindings(result);
            return result;
        }

        static List<ClipBinding> CollectProjectionBindings(
            CharacterPresentationProjection projection,
            List<string> errors)
        {
            var result = new List<ClipBinding>();
            for (int producerIndex = 0; producerIndex < projection.Producers.Count; producerIndex++)
            {
                CharacterPresentationProducerEntry producer = projection.Producers[producerIndex];
                if (producer == null || producer.Kind != CharacterPresentationProducerKind.Animation || producer.Animation == null)
                    continue;
                for (int clipIndex = 0; clipIndex < producer.Animation.Clips.Count; clipIndex++)
                {
                    CharacterPresentationAnimationClipBinding clip = producer.Animation.Clips[clipIndex];
                    if (clip == null || !clip.Clip || string.IsNullOrWhiteSpace(clip.ClipAuthoringId))
                    {
                        errors?.Add($"Projection producer '{producer.ProducerId}' contains an incomplete Foot Analysis clip binding.");
                        continue;
                    }
                    result.Add(new ClipBinding(
                        producer.ProducerId.TimelineAuthoringId,
                        producer.ProducerId.TrackAuthoringId,
                        clip.ClipAuthoringId,
                        clip.Clip));
                }
            }
            for (int planIndex = 0; planIndex < projection.BlendSpaces.Count; planIndex++)
            {
                CharacterAnimationBlendSpacePlan plan = projection.BlendSpaces[planIndex];
                if (plan == null)
                {
                    errors?.Add($"Projection Blend Space plan #{planIndex} is missing.");
                    continue;
                }
                for (int sampleIndex = 0; sampleIndex < plan.Samples.Count; sampleIndex++)
                {
                    CharacterAnimationBlendSpaceSamplePlan sample = plan.Samples[sampleIndex];
                    if (sample == null || !sample.SampleId.IsValid || !sample.Clip)
                    {
                        errors?.Add($"Projection Blend Space '{plan.BlendSpaceId}' Sample #{sampleIndex} is incomplete.");
                        continue;
                    }
                    result.Add(new ClipBinding(plan.BlendSpaceId, sample.SampleId, sample.Clip));
                }
            }
            SortBindings(result);
            return result;
        }

        static void SortBindings(List<ClipBinding> bindings)
        {
            bindings.Sort((left, right) => string.CompareOrdinal(left.BindingKey, right.BindingKey));
        }

        static string[] BuildArtifactTokens(IEnumerable<AnimationFootAnalysisArtifact> artifacts)
        {
            return artifacts
                .OrderBy(value => value.Identity.IdentityHash.Value, StringComparer.Ordinal)
                .Select(value => string.Concat(value.Identity.IdentityHash.Value, ":", value.ContentHash.Value))
                .ToArray();
        }

        static string FormatIssue(
            ClipBinding binding,
            AnimationFootAnalysisArtifactIdentity identity,
            AnimationFootAnalysisArtifactStatus status,
            string detail)
        {
            return $"Foot Analysis artifact {status} at {binding.DisplayIdentity} AnimationClip '{identity.ClipAssetGuid}' Source '{identity.AnalysisSourceId}' Rig '{identity.SamplingRigAssetGuid}' Calibration '{identity.CalibrationId}@{identity.CalibrationRevision}': {detail}";
        }
    }
}
