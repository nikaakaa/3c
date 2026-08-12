using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Presentation;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

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
        public static CharacterFootAnalysisArtifactDiagnostic InspectPoseSource(
            CharacterAnimationPresentationProfile profile,
            CharacterPresentationPoseSourceBinding binding)
        {
            string bindingIdentity = binding
                ? CharacterPresentationAssetObjectIdentity.Require(binding)
                : string.Empty;
            string bindingKey = !string.IsNullOrWhiteSpace(bindingIdentity)
                ? AnimationFootAnalysisProjectionBuildData.PoseSourceBindingKey(bindingIdentity)
                : string.Empty;
            CharacterSequencePoseSourceBinding sequence = binding as CharacterSequencePoseSourceBinding;
            CharacterBlendSpacePoseSourceBinding blendSpace = binding as CharacterBlendSpacePoseSourceBinding;
            if (!profile || !binding || string.IsNullOrWhiteSpace(bindingIdentity) ||
                sequence == null && blendSpace == null ||
                sequence != null && !sequence.Clip ||
                blendSpace != null && !blendSpace.BlendSpace)
            {
                return new CharacterFootAnalysisArtifactDiagnostic(
                    AnimationFootAnalysisArtifactStatus.Missing,
                    bindingKey,
                    "Presentation Pose source Foot Analysis requires a Profile, stable source identity, and source asset.");
            }
            if (profile.FootPlacementAnalysisMode == CharacterFootPlacementAnalysisMode.Disabled)
            {
                return new CharacterFootAnalysisArtifactDiagnostic(
                    AnimationFootAnalysisArtifactStatus.Missing,
                    bindingKey,
                    "Presentation Pose source Foot Analysis is disabled by the Profile.");
            }

            List<string> errors = new List<string>();
            if (!TryResolveSource(profile, errors, out CharacterFootPlacementAnalysisSource source))
            {
                return new CharacterFootAnalysisArtifactDiagnostic(
                    AnimationFootAnalysisArtifactStatus.Missing,
                    bindingKey,
                    string.Join("\n", errors));
            }
            if (!string.Equals(
                    binding.FootAnalysisIdentity,
                    source.AnalysisSourceId.Value,
                    StringComparison.Ordinal))
            {
                return new CharacterFootAnalysisArtifactDiagnostic(
                    AnimationFootAnalysisArtifactStatus.Stale,
                    bindingKey,
                    $"Binding analysis identity '{binding.FootAnalysisIdentity}' does not match Profile source '{source.AnalysisSourceId.Value}'.");
            }
            if (blendSpace != null)
            {
                IReadOnlyList<CharacterFootAnalysisArtifactDiagnostic>
                    diagnostics = InspectBlendSpace(
                        profile,
                        blendSpace.BlendSpace);
                for (int i = 0; i < diagnostics.Count; i++)
                {
                    if (diagnostics[i].Status ==
                        AnimationFootAnalysisArtifactStatus.Ready)
                    {
                        continue;
                    }
                    return new CharacterFootAnalysisArtifactDiagnostic(
                        diagnostics[i].Status,
                        bindingKey,
                        diagnostics[i].Message);
                }
                return new CharacterFootAnalysisArtifactDiagnostic(
                    AnimationFootAnalysisArtifactStatus.Ready,
                    bindingKey,
                    $"{binding.name} / {diagnostics.Count} Blend Space samples ready.");
            }

            try
            {
                AnimationFootAnalysisArtifactIdentity expected =
                    AnimationFootAnalysisArtifactBuilder.GetExpectedIdentity(
                        sequence.Clip,
                        source,
                        BuildSchedule(sequence.Markers, sequence.Clip.length, sequence.Clip.frameRate));
                AnimationFootAnalysisArtifactInspection inspection =
                    AnimationFootAnalysisArtifactStore.Inspect(expected);
                return new CharacterFootAnalysisArtifactDiagnostic(
                    inspection.Status,
                    bindingKey,
                    $"{binding.name} / {sequence.Clip.name} / {expected.IdentityHash.Value} / {inspection.Error}");
            }
            catch (Exception exception)
            {
                return new CharacterFootAnalysisArtifactDiagnostic(
                    AnimationFootAnalysisArtifactStatus.Corrupt,
                    bindingKey,
                    $"{binding.name} / {sequence.Clip.name} / {exception.Message}");
            }
        }

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
                        AnimationFootAnalysisArtifactBuilder.GetExpectedIdentity(
                            sample.Clip,
                            source,
                            BuildSchedule(sample.Markers));
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
                UnityEngine.AnimationClip clip,
                AnimationFootContactSchedule contactSchedule)
            {
                TimelineAuthoringId = timelineAuthoringId;
                TrackAuthoringId = trackAuthoringId;
                ClipAuthoringId = clipAuthoringId;
                Clip = clip;
                ContactSchedule = contactSchedule ?? throw new ArgumentNullException(nameof(contactSchedule));
                BlendSpaceId = default;
                SampleId = default;
                PoseSourceBindingIdentity = string.Empty;
            }

            public ClipBinding(
                CharacterAnimationBlendSpaceId blendSpaceId,
                CharacterAnimationBlendSpaceSampleId sampleId,
                UnityEngine.AnimationClip clip,
                AnimationFootContactSchedule contactSchedule)
            {
                if (!blendSpaceId.IsValid || !sampleId.IsValid)
                    throw new ArgumentException("Blend Space Foot Analysis binding identity is invalid.");
                TimelineAuthoringId = string.Empty;
                TrackAuthoringId = string.Empty;
                ClipAuthoringId = string.Empty;
                Clip = clip;
                ContactSchedule = contactSchedule ?? throw new ArgumentNullException(nameof(contactSchedule));
                BlendSpaceId = blendSpaceId;
                SampleId = sampleId;
                PoseSourceBindingIdentity = string.Empty;
            }

            public ClipBinding(
                string poseSourceBindingIdentity,
                UnityEngine.AnimationClip clip,
                AnimationFootContactSchedule contactSchedule)
            {
                if (string.IsNullOrWhiteSpace(poseSourceBindingIdentity))
                    throw new ArgumentException("Presentation Pose source Foot Analysis binding identity is invalid.");
                TimelineAuthoringId = string.Empty;
                TrackAuthoringId = string.Empty;
                ClipAuthoringId = string.Empty;
                Clip = clip;
                ContactSchedule = contactSchedule ?? throw new ArgumentNullException(nameof(contactSchedule));
                BlendSpaceId = default;
                SampleId = default;
                PoseSourceBindingIdentity = poseSourceBindingIdentity.Trim();
            }

            public string TimelineAuthoringId { get; }
            public string TrackAuthoringId { get; }
            public string ClipAuthoringId { get; }
            public UnityEngine.AnimationClip Clip { get; }
            public AnimationFootContactSchedule ContactSchedule { get; }
            public CharacterAnimationBlendSpaceId BlendSpaceId { get; }
            public CharacterAnimationBlendSpaceSampleId SampleId { get; }
            public string PoseSourceBindingIdentity { get; }
            public bool IsBlendSpace => BlendSpaceId.IsValid;
            public bool IsPoseSource => !string.IsNullOrWhiteSpace(PoseSourceBindingIdentity);
            public string BindingKey => IsPoseSource
                ? AnimationFootAnalysisProjectionBuildData.PoseSourceBindingKey(PoseSourceBindingIdentity)
                : IsBlendSpace
                    ? AnimationFootAnalysisProjectionBuildData.BlendSpaceBindingKey(BlendSpaceId, SampleId)
                    : AnimationFootAnalysisProjectionBuildData.BindingKey(
                        TimelineAuthoringId,
                        TrackAuthoringId,
                        ClipAuthoringId);
            public string DisplayIdentity => IsPoseSource
                ? $"Presentation Pose source binding '{PoseSourceBindingIdentity}'"
                : IsBlendSpace
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
                        AnimationFootAnalysisArtifactBuilder.GetExpectedIdentity(
                            binding.Clip,
                            source,
                            binding.ContactSchedule);
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
                            artifact = AnimationFootAnalysisArtifactBuilder.Build(
                                binding.Clip,
                                source,
                                binding.ContactSchedule);
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
            CharacterFootPlacementRigGeometryValidationIdentity geometryValidation =
                source.RigCalibration.GeometryValidation ??
                throw new InvalidOperationException("Foot Placement Calibration geometry validation identity is missing.");
            geometryValidation.RequireMatches(source.RigDefinition, source.RigCalibration);
            var identity = new AnimationFootAnalysisProjectionIdentity(
                CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures,
                source.AnalysisSourceId.Value,
                source.AnalysisVersion,
                CharacterFootPlacementAnalysisSource.AlgorithmVersion,
                source.RigCalibration.CalibrationId,
                source.RigCalibration.SchemaVersion,
                source.RigCalibration.ContentRevision,
                geometryValidation.IdentityHash,
                geometryValidation.GeometryContentHash,
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
                        AnimationFootAnalysisArtifactBuilder.GetExpectedIdentity(
                            binding.Clip,
                            source,
                            binding.ContactSchedule);
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
            CharacterFootPlacementRigGeometryValidationIdentity geometryValidation =
                source.RigCalibration.GeometryValidation;
            if (projection.FootAnalysis == null || !projection.FootAnalysis.IsEnabled ||
                geometryValidation == null ||
                !string.Equals(projection.FootAnalysis.AnalysisSourceId, source.AnalysisSourceId.Value, StringComparison.Ordinal) ||
                projection.FootAnalysis.AnalysisVersion != source.AnalysisVersion ||
                !string.Equals(projection.FootAnalysis.AlgorithmVersion, CharacterFootPlacementAnalysisSource.AlgorithmVersion, StringComparison.Ordinal) ||
                !string.Equals(projection.FootAnalysis.CalibrationId.Value, source.RigCalibration.CalibrationId.Value, StringComparison.Ordinal) ||
                projection.FootAnalysis.CalibrationSchemaVersion != source.RigCalibration.SchemaVersion ||
                !string.Equals(projection.FootAnalysis.CalibrationRevision, source.RigCalibration.ContentRevision, StringComparison.Ordinal) ||
                !string.Equals(projection.FootAnalysis.GeometryValidationIdentity, geometryValidation.IdentityHash, StringComparison.Ordinal) ||
                !string.Equals(projection.FootAnalysis.GeometryValidationContentHash, geometryValidation.GeometryContentHash, StringComparison.Ordinal) ||
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
            var blendSpaceBindings = new HashSet<string>(StringComparer.Ordinal);
            for (int bindingIndex = 0; bindingIndex < profile.PoseSourceBindings.Count; bindingIndex++)
            {
                CharacterPresentationPoseSourceBinding binding = profile.PoseSourceBindings[bindingIndex];
                if (!binding)
                {
                    errors?.Add($"Foot Analysis Presentation Pose source binding #{bindingIndex} is incomplete.");
                    continue;
                }
                if (binding is CharacterSequencePoseSourceBinding sequence)
                {
                    if (!sequence.Clip)
                        errors?.Add($"Foot Analysis Sequence Pose source binding #{bindingIndex} has no AnimationClip.");
                    else
                        result.Add(new ClipBinding(
                            CharacterPresentationAssetObjectIdentity.Require(sequence),
                            sequence.Clip,
                            BuildSchedule(sequence.Markers, sequence.Clip.length, sequence.Clip.frameRate)));
                    continue;
                }
                CharacterAnimationBlendSpaceAsset blendSpace =
                    (binding as CharacterBlendSpacePoseSourceBinding)?.BlendSpace;
                if (!blendSpace)
                {
                    errors?.Add($"Foot Analysis Blend Space Pose source binding #{bindingIndex} has no asset.");
                    continue;
                }
                for (int sampleIndex = 0; sampleIndex < blendSpace.Samples.Count; sampleIndex++)
                {
                    CharacterAnimationBlendSpaceSample sample = blendSpace.Samples[sampleIndex];
                    if (sample == null || !sample.SampleId.IsValid || !sample.Clip)
                    {
                        errors?.Add(
                            $"Foot Analysis Blend Space '{blendSpace.name}' Sample #{sampleIndex} is incomplete.");
                        continue;
                    }
                    var sampleBinding =
                        new ClipBinding(
                            blendSpace.BlendSpaceId,
                            sample.SampleId,
                            sample.Clip,
                            BuildSchedule(sample.Markers));
                    if (blendSpaceBindings.Add(sampleBinding.BindingKey))
                        result.Add(sampleBinding);
                }
            }
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
                        result.Add(new ClipBinding(
                            timeline.AuthoringId,
                            track.AuthoringId,
                            clip.AuthoringId,
                            clip.Clip,
                            BuildSchedule(track.SyncMarkers, clip)));
                    }
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
            for (int sourceIndex = 0; sourceIndex < projection.PoseSources.Count; sourceIndex++)
            {
                CharacterPresentationPoseSourcePlan source = projection.PoseSources[sourceIndex];
                if (source == null || !source.SourceIndex.IsValid ||
                    string.IsNullOrWhiteSpace(source.BindingAssetIdentity) || !source.Clip)
                {
                    errors?.Add($"Projection Presentation Pose source #{sourceIndex} is incomplete.");
                    continue;
                }
                result.Add(new ClipBinding(
                    source.BindingAssetIdentity,
                    source.Clip,
                    BuildSchedule(source.MarkerSync.Markers, source.MarkerSync.DurationSeconds)));
            }
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
                        clip.Clip,
                        BuildSchedule(producer.Animation.MarkerSync.Markers, clip)));
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
                    result.Add(new ClipBinding(
                        plan.BlendSpaceId,
                        sample.SampleId,
                        sample.Clip,
                        BuildSchedule(sample.Markers)));
                }
            }
            SortBindings(result);
            return result;
        }

        static void SortBindings(List<ClipBinding> bindings)
        {
            bindings.Sort((left, right) => string.CompareOrdinal(left.BindingKey, right.BindingKey));
        }

        static AnimationFootContactSchedule BuildSchedule(
            IReadOnlyList<PresentationPoseSourceMarker> markers,
            float durationSeconds,
            float frameRate)
        {
            var entries = new List<(string Id, float Phase)>();
            for (int i = 0; i < markers.Count; i++)
                entries.Add((markers[i].MarkerId, markers[i].Frame / frameRate / durationSeconds));
            return BuildSchedule(entries);
        }

        static AnimationFootContactSchedule BuildSchedule(
            IReadOnlyList<CharacterAnimationBlendSpaceMarker> markers)
        {
            var entries = new List<(string Id, float Phase)>();
            for (int i = 0; i < markers.Count; i++)
                entries.Add((markers[i].MarkerId, markers[i].NormalizedTime));
            return BuildSchedule(entries);
        }

        static AnimationFootContactSchedule BuildSchedule(
            IReadOnlyList<CharacterAnimationBlendSpaceMarkerPlanPayload> markers)
        {
            var entries = new List<(string Id, float Phase)>();
            for (int i = 0; i < markers.Count; i++)
                entries.Add((markers[i].MarkerId, markers[i].NormalizedTime));
            return BuildSchedule(entries);
        }

        static AnimationFootContactSchedule BuildSchedule(
            IReadOnlyList<AnimationSyncMarker> markers,
            BTSMTL.Timeline.AnimationClip clip)
        {
            var entries = new List<(string Id, float Phase)>();
            float frameRate = TimelineUtility.FrameRate;
            for (int i = 0; i < markers.Count; i++)
            {
                float sourceTime = (markers[i].Frame - clip.StartFrame + clip.ClipInFrame) / frameRate;
                if (clip.Clip.isLooping)
                    sourceTime = Mathf.Repeat(sourceTime, clip.Clip.length);
                else if (sourceTime < 0f || sourceTime > clip.Clip.length)
                    continue;
                entries.Add((markers[i].MarkerId, sourceTime / clip.Clip.length));
            }
            return BuildSchedule(entries);
        }

        static AnimationFootContactSchedule BuildSchedule(
            IReadOnlyList<AnimationMarkerSyncMarkerBinding> markers,
            float durationSeconds)
        {
            var entries = new List<(string Id, float Phase)>();
            if (!float.IsFinite(durationSeconds) || durationSeconds <= 0f)
                return AnimationFootContactSchedule.None;
            for (int i = 0; i < markers.Count; i++)
                entries.Add((markers[i].MarkerId, markers[i].TimeSeconds / durationSeconds));
            return BuildSchedule(entries);
        }

        static AnimationFootContactSchedule BuildSchedule(
            IReadOnlyList<AnimationMarkerSyncMarkerBinding> markers,
            CharacterPresentationAnimationClipBinding clip)
        {
            var entries = new List<(string Id, float Phase)>();
            for (int i = 0; i < markers.Count; i++)
            {
                float sourceTime = markers[i].TimeSeconds - clip.StartTime + clip.ClipInTime;
                if (clip.Clip.isLooping)
                    sourceTime = Mathf.Repeat(sourceTime, clip.Clip.length);
                else if (sourceTime < 0f || sourceTime > clip.Clip.length)
                    continue;
                entries.Add((markers[i].MarkerId, sourceTime / clip.Clip.length));
            }
            return BuildSchedule(entries);
        }

        static AnimationFootContactSchedule BuildSchedule(
            IReadOnlyList<(string Id, float Phase)> entries)
        {
            var left = new List<float>();
            var right = new List<float>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].Id, AnimationFootContactSchedule.LeftMarkerId, StringComparison.Ordinal))
                    left.Add(entries[i].Phase);
                else if (string.Equals(entries[i].Id, AnimationFootContactSchedule.RightMarkerId, StringComparison.Ordinal))
                    right.Add(entries[i].Phase);
            }
            return left.Count == 0 && right.Count == 0
                ? AnimationFootContactSchedule.Inferred
                : AnimationFootContactSchedule.Authored(left, right);
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
