using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;
using ThirdPersonCharacter.Animation.TransitionRouting;
using ThirdPersonCharacter.Editor.CharacterSimulation;
using ThirdPersonCharacter.Editor.MotionMatching;
using ThirdPersonCharacter.Equipment;
using ThirdPersonCharacter.Pipeline.Animation;
using ThirdPersonCharacter.Pipeline.Animation.MotionMatching;
using ThirdPersonCharacter.Pipeline.Editor;
using ThirdPersonSimulation;
using UnityEditor;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    internal readonly struct CharacterPresentationTimelineCallSite
    {
        public CharacterPresentationTimelineCallSite(
            string identity,
            TimelinePlaybackMode playbackMode)
        {
            Identity = identity ?? string.Empty;
            PlaybackMode = playbackMode;
        }

        public string Identity { get; }
        public TimelinePlaybackMode PlaybackMode { get; }
    }

    internal sealed class CharacterPresentationProducerCompilationResult
    {
        public CharacterPresentationProducerCompilationResult(
            CharacterPresentationProducerEntry entry,
            IReadOnlyList<string> diagnostics)
        {
            Entry = entry;
            Diagnostics = diagnostics ?? Array.Empty<string>();
        }

        public CharacterPresentationProducerEntry Entry { get; }
        public IReadOnlyList<string> Diagnostics { get; }
    }

    internal static class CharacterPresentationProducerCompiler
    {
        internal static CharacterPresentationProducerCompilationResult Compile(
            CharacterPresentationSemanticReader reader,
            ProgramProducer producer,
            CharacterAnimationPresentationProfile profile,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
            IReadOnlyDictionary<string, TimelineData> timelines,
            IReadOnlyDictionary<string, IReadOnlyList<CharacterPresentationTimelineCallSite>> timelineCallSites)
        {
            var diagnostics = new List<string>();
            CharacterPresentationProducerEntry entry = BuildProducer(
                reader,
                producer,
                profile,
                footAnalysisCompilation,
                timelines,
                timelineCallSites,
                diagnostics);
            return new CharacterPresentationProducerCompilationResult(
                entry,
                diagnostics);
        }

        internal static IReadOnlyDictionary<string,
            IReadOnlyList<CharacterPresentationTimelineCallSite>>
            CollectTimelineCallSites(CharacterAuthoringGraphOccurrence root)
        {
            var result = new Dictionary<string,
                List<CharacterPresentationTimelineCallSite>>(StringComparer.Ordinal);
            CollectTimelineCallSites(root, result);
            return result.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<CharacterPresentationTimelineCallSite>)pair.Value.ToArray(),
                StringComparer.Ordinal);
        }

        static CharacterPresentationProducerEntry BuildProducer(
            CharacterPresentationSemanticReader reader,
            ProgramProducer producer,
            CharacterAnimationPresentationProfile profile,
            CharacterFootPlacementAnalysisCompilation footAnalysisCompilation,
            IReadOnlyDictionary<string, TimelineData> timelines,
            IReadOnlyDictionary<string, IReadOnlyList<CharacterPresentationTimelineCallSite>> timelineCallSites,
            List<string> errors)
        {
            AnimationFootAnalysisProjectionBuildData footAnalysis =
                footAnalysisCompilation?.BuildData;
            CharacterPresentationProducerKind? kind = reader.ResolveKind(producer, errors);
            ProgramSourceMapEntry source = reader.ResolveSource(producer, errors);
            if (!kind.HasValue || source == null)
                return null;

            if (kind.Value != CharacterPresentationProducerKind.Animation)
            {
                CharacterPresentationCameraBinding camera = kind.Value == CharacterPresentationProducerKind.Camera
                    ? BuildCameraBinding(reader, producer, source, timelines, errors)
                    : null;
                CharacterPresentationCueBinding cue = kind.Value == CharacterPresentationProducerKind.Cue
                    ? BuildCueBinding(producer, source, timelines, errors)
                    : null;
                if (kind.Value == CharacterPresentationProducerKind.Camera && camera == null ||
                    kind.Value == CharacterPresentationProducerKind.Cue && cue == null)
                    return null;
                return new CharacterPresentationProducerEntry(
                    producer.Index,
                    producer.Identity,
                    producer.SourceIdentity,
                    producer.ChannelKind,
                    kind.Value,
                    TimelinePlaybackMode.Once,
                    string.Empty,
                    string.Empty,
                    producer.AnimationChannelId,
                    source.GraphId,
                    source.NodeId,
                    source.TimelineId,
                    ParseTrackId(producer.SourceIdentity),
                    source.DisplayPath,
                    null,
                    camera,
                    cue);
            }

            if (!TryParseAnimationSource(producer.SourceIdentity, out AnimationProducerId producerId) ||
                !string.Equals(source.TimelineId, producerId.TimelineAuthoringId, StringComparison.Ordinal))
            {
                errors?.Add($"Animation producer '{producer.Identity}' has an invalid source identity.");
                return null;
            }
            if (!timelines.TryGetValue(producerId.TimelineAuthoringId, out TimelineData timeline))
            {
                errors?.Add($"Animation producer '{producer.Identity}' Timeline source is absent from the compiler inventory.");
                return null;
            }
            if (!TryResolvePlaybackMode(
                    producerId.TimelineAuthoringId,
                    timelineCallSites,
                    errors,
                    out TimelinePlaybackMode playbackMode))
                return null;
            AnimationTrack track = null;
            for (int i = 0; i < timeline.Tracks.Count; i++)
            {
                if (timeline.Tracks[i] is AnimationTrack candidate &&
                    string.Equals(candidate.AuthoringId, producerId.TrackAuthoringId, StringComparison.Ordinal))
                {
                    track = candidate;
                    break;
                }
            }
            if (track == null || track.AnimationChannelId != producer.AnimationChannelId)
            {
                errors?.Add($"Animation producer '{producer.Identity}' Track source or Animation Channel binding is invalid.");
                return null;
            }
            AnimationProducerPresentationBinding authoringBinding = profile.FindProducerBinding(producerId);
            if (authoringBinding == null)
            {
                errors?.Add($"Animation producer '{producerId}' has no Presentation source binding.");
                return null;
            }
            if (footAnalysis == null)
            {
                errors?.Add($"Animation producer '{producerId}' has no compiled Profile Foot Analysis source.");
                return null;
            }
            if (!authoringBinding.Source || !authoringBinding.Source.IsValid)
            {
                errors?.Add(
                    $"Animation producer '{producerId}' is not a finite Timeline Action source. Continuous Pose sources must be bound through a PoseState provider.");
                return null;
            }

            var clips = new List<CharacterPresentationAnimationClipBinding>();
            for (int i = 0; i < track.Clips.Count; i++)
            {
                if (track.Clips[i] is not BTSMTL.Timeline.AnimationClip clip)
                    continue;
                UnityEngine.AnimationClip sourceClip = clip.Clip;
                if (!sourceClip)
                {
                    errors?.Add($"Animation producer '{producerId}' segment '{clip.AuthoringId}' has no AnimationClip.");
                    continue;
                }
                try
                {
                    _ = CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(sourceClip);
                }
                catch (Exception exception)
                {
                    errors?.Add($"Animation producer '{producerId}' Clip '{sourceClip.name}' is invalid: {exception.Message}");
                    continue;
                }
                AnimationFootFeaturePair features = default;
                if (profile.FootPlacementAnalysisMode == CharacterFootPlacementAnalysisMode.GeneratedPerFootFeatures &&
                    (footAnalysis == null || !footAnalysis.TryGet(
                        producerId.TimelineAuthoringId,
                        producerId.TrackAuthoringId,
                        clip.AuthoringId,
                        out features)))
                {
                    errors?.Add($"Animation producer '{producerId}' clip '{clip.AuthoringId}' has no generated Foot Analysis features.");
                    continue;
                }
                CharacterAnimationClipContentIdentity clipIdentity =
                    CharacterAnimationClipRegisteredCurveCatalog.ResolveIdentity(sourceClip);
                CharacterAnimationClipRegisteredCurveCatalog.ValidateFootMotionGroupRequired(sourceClip);
                AnimationCurve footWeight = CharacterAnimationClipRegisteredCurveCatalog.ReadRequired(
                    sourceClip,
                    CharacterAnimationClipRegisteredCurveChannels.FootPlacementWeight);
                AnimationFootAnalysisArtifact artifact =
                    footAnalysisCompilation.RequireArtifact(
                        AnimationFootAnalysisProjectionBuildData.BindingKey(
                            producerId.TimelineAuthoringId,
                            producerId.TrackAuthoringId,
                            clip.AuthoringId));
                clips.Add(new CharacterPresentationAnimationClipBinding(
                    clip.AuthoringId,
                    $"{clipIdentity.AssetGuid}:{clipIdentity.LocalFileId}",
                    clipIdentity.FullDependencyHash,
                    clipIdentity.AnalysisInputHash,
                    clipIdentity.RegisteredCurveHash,
                    sourceClip,
                    clipIdentity.SourceDurationSeconds,
                    clip.StartTime,
                    clip.EndTime,
                    clip.ClipInTime,
                    clip.DurationTime,
                    clip.EaseInTime,
                    clip.EaseOutTime,
                    clip.ExtraPolationMode,
                    clip.WeightCurve,
                    clip.EaseInCurve,
                    clip.EaseOutCurve,
                    CharacterPresentationFootEventCompiler.NormalizeRegisteredCurve(footWeight, clipIdentity.SourceDurationSeconds),
                    features,
                    CharacterPresentationFootEventCompiler.CompileFootStepObservation(
                        sourceClip,
                        clipIdentity.SourceDurationSeconds,
                        artifact.MotionData)));
            }
            if (clips.Count == 0)
            {
                errors?.Add($"Animation producer '{producerId}' has no compiled AnimationClip binding.");
                return null;
            }
            if (!TryCompileLastSampleTime(
                    track,
                    timeline.Duration,
                    producerId,
                    errors,
                    out float lastSampleTimeSeconds))
            {
                return null;
            }
            var animation = new CharacterPresentationAnimationBinding(
                authoringBinding.Source,
                track.Name,
                timeline.Duration,
                lastSampleTimeSeconds,
                clips.ToArray());
            return new CharacterPresentationProducerEntry(
                producer.Index,
                producer.Identity,
                producer.SourceIdentity,
                producer.ChannelKind,
                kind.Value,
                playbackMode,
                producerId.TimelineAuthoringId,
                producerId.TrackAuthoringId,
                producer.AnimationChannelId,
                source.GraphId,
                source.NodeId,
                source.TimelineId,
                producerId.TrackAuthoringId,
                source.DisplayPath,
                animation,
                null,
                null);
        }

        static bool TryCompileLastSampleTime(
            AnimationTrack track,
            float timelineDuration,
            AnimationProducerId producerId,
            List<string> errors,
            out float lastSampleTimeSeconds)
        {
            lastSampleTimeSeconds = 0f;
            if (track == null ||
                !float.IsFinite(timelineDuration) ||
                timelineDuration <= 0f)
            {
                errors?.Add(
                    $"Animation producer '{producerId}' has an invalid Timeline duration.");
                return false;
            }

            BTSMTL.Timeline.AnimationClip[] clips =
                track.Clips
                    .OfType<BTSMTL.Timeline.AnimationClip>()
                    .Where(value => value != null && value.Clip)
                    .OrderBy(value => value.StartTime)
                    .ThenBy(value => value.EndTime)
                    .ToArray();
            if (clips.Length == 0)
            {
                errors?.Add(
                    $"Animation producer '{producerId}' has no sampleable AnimationClip coverage.");
                return false;
            }

            const float tolerance = 0.00001f;
            float coverageEnd = 0f;
            bool held = false;
            for (int i = 0; i < clips.Length; i++)
            {
                BTSMTL.Timeline.AnimationClip clip = clips[i];
                if (clip.StartTime > coverageEnd + tolerance)
                {
                    errors?.Add(
                        $"Animation producer '{producerId}' has an AnimationClip coverage gap at {coverageEnd:R}-{clip.StartTime:R} seconds.");
                    return false;
                }
                coverageEnd = Math.Max(coverageEnd, clip.EndTime);
                if (clip.ExtraPolationMode == ExtraPolationMode.Hold)
                {
                    held = true;
                    coverageEnd = timelineDuration;
                    break;
                }
            }

            float boundedEnd = Math.Min(coverageEnd, timelineDuration);
            lastSampleTimeSeconds = held
                ? boundedEnd
                : Math.Max(0f, boundedEnd - 1f / 60000f);
            if (!float.IsFinite(lastSampleTimeSeconds) ||
                lastSampleTimeSeconds <= 0f)
            {
                errors?.Add(
                    $"Animation producer '{producerId}' has no positive finite sample coverage.");
                return false;
            }
            return true;
        }


        static bool TryResolvePlaybackMode(
            string timelineAuthoringId,
            IReadOnlyDictionary<string, IReadOnlyList<CharacterPresentationTimelineCallSite>> timelineCallSites,
            List<string> errors,
            out TimelinePlaybackMode playbackMode)
        {
            playbackMode = default;
            if (timelineCallSites == null ||
                !timelineCallSites.TryGetValue(timelineAuthoringId, out IReadOnlyList<CharacterPresentationTimelineCallSite> callSites) ||
                callSites == null ||
                callSites.Count == 0)
            {
                errors?.Add($"Animation Timeline '{timelineAuthoringId}' has no playback call site.");
                return false;
            }

            playbackMode = callSites[0].PlaybackMode;
            for (int i = 1; i < callSites.Count; i++)
            {
                if (callSites[i].PlaybackMode == playbackMode)
                    continue;
                errors?.Add(
                    $"Animation Timeline '{timelineAuthoringId}' is called with both {playbackMode} and {callSites[i].PlaybackMode}; " +
                    "one Presentation producer cannot own conflicting playback modes.");
                return false;
            }
            return true;
        }

        static IReadOnlyDictionary<string, IReadOnlyList<CharacterPresentationTimelineCallSite>> CollectTimelineCallSites(
            CharacterAuthoringGraphOccurrence root)
        {
            var result = new Dictionary<string, List<CharacterPresentationTimelineCallSite>>(StringComparer.Ordinal);
            CollectTimelineCallSites(root, result);
            return result.ToDictionary(
                pair => pair.Key,
                pair => (IReadOnlyList<CharacterPresentationTimelineCallSite>)pair.Value.ToArray(),
                StringComparer.Ordinal);
        }

        static void CollectTimelineCallSites(
            CharacterAuthoringGraphOccurrence occurrence,
            Dictionary<string, List<CharacterPresentationTimelineCallSite>> result)
        {
            if (occurrence == null)
                return;
            for (int i = 0; i < occurrence.Timelines.Count; i++)
            {
                CharacterAuthoringTimelineRecord timeline = occurrence.Timelines[i];
                string timelineId = timeline.Timeline.AuthoringId;
                if (!result.TryGetValue(timelineId, out List<CharacterPresentationTimelineCallSite> values))
                {
                    values = new List<CharacterPresentationTimelineCallSite>();
                    result.Add(timelineId, values);
                }
                values.Add(new CharacterPresentationTimelineCallSite(timeline.Route, timeline.Node.PlaybackMode));
            }
            for (int i = 0; i < occurrence.GraphReferences.Count; i++)
                CollectTimelineCallSites(occurrence.GraphReferences[i].Child, result);
        }

        static CharacterPresentationCameraBinding BuildCameraBinding(
            CharacterPresentationSemanticReader reader,
            ProgramProducer producer,
            ProgramSourceMapEntry source,
            IReadOnlyDictionary<string, TimelineData> timelines,
            List<string> errors)
        {
            if (TryFindSourceClip(source, timelines, out Clip clip))
            {
                if (clip is CameraStateClip state)
                {
                    return CharacterPresentationCameraBinding.State(
                        state.Mode,
                        state.Priority,
                        state.BlendInSeconds,
                        state.BlendOutSeconds,
                        state.TargetKey,
                        state.InterruptPolicy);
                }
                if (clip is CameraCueClip cue)
                {
                    return CharacterPresentationCameraBinding.Cue(
                        cue.CueId,
                        cue.CueKind,
                        cue.CueType,
                        cue.DurationSeconds,
                        cue.Priority);
                }
                if (clip is CameraResponseClip response)
                {
                    return CharacterPresentationCameraBinding.Response(
                        response.LookResponse,
                        response.ManualOrbitWeight,
                        response.PitchResponseWeight,
                        response.YawResponseWeight,
                        response.Priority);
                }
                errors?.Add($"Camera producer '{producer.Identity}' source clip type '{clip.GetType().Name}' is unsupported.");
                return null;
            }

            try
            {
                SemanticOperation operation = reader.RequireProducerOperation(producer);
                if (operation.Integer0 != CameraProgramOperationSchema.PayloadVersion)
                    throw new InvalidOperationException($"payload version '{operation.Integer0}' is unsupported");
                return operation.Code switch
                {
                    SimulationOperationCode.CameraStateRequest => CharacterPresentationCameraBinding.State(
                        (TimelineCameraMode)operation.Integer1,
                        reader.RequireInt32(operation, "Priority"),
                        reader.RequireScalar(operation, "BlendInSeconds"),
                        reader.RequireScalar(operation, "BlendOutSeconds"),
                        reader.RequireString(operation, "TargetKey"),
                        (TimelineCameraInterruptPolicy)operation.Flags),
                    SimulationOperationCode.CameraCue => CharacterPresentationCameraBinding.Cue(
                        reader.RequireString(operation, "CueId"),
                        (TimelineCameraCueKind)operation.Integer1,
                        reader.RequireString(operation, "CueType"),
                        reader.RequireScalar(operation, "DurationSeconds"),
                        reader.RequireInt32(operation, "Priority")),
                    SimulationOperationCode.CameraResponse => CharacterPresentationCameraBinding.Response(
                        (TimelineCameraLookResponseMode)operation.Integer1,
                        reader.RequireScalar(operation, "ManualOrbitWeight"),
                        reader.RequireScalar(operation, "PitchResponseWeight"),
                        reader.RequireScalar(operation, "YawResponseWeight"),
                        reader.RequireInt32(operation, "Priority")),
                    SimulationOperationCode.CameraTarget => CharacterPresentationCameraBinding.Target(
                        reader.RequireString(operation, "TargetKey"),
                        reader.RequireString(operation, "AnchorKey"),
                        reader.RequireString(operation, "AimPointKey"),
                        reader.RequireString(operation, "PreferredBoneKey"),
                        reader.RequireInt32(operation, "Priority")),
                    _ => throw new InvalidOperationException($"operation '{operation.Code}' is unsupported")
                };
            }
            catch (Exception exception)
            {
                errors?.Add($"Camera producer '{producer.Identity}' Graph payload is invalid: {exception.Message}.");
                return null;
            }
        }

        static CharacterPresentationCueBinding BuildCueBinding(
            ProgramProducer producer,
            ProgramSourceMapEntry source,
            IReadOnlyDictionary<string, TimelineData> timelines,
            List<string> errors)
        {
            if (TryFindSourceClip(source, timelines, out Clip clip))
            {
                if (clip is ActionCueClip cue)
                    return new CharacterPresentationCueBinding(cue.CueId, cue.CueType);
                errors?.Add($"Cue producer '{producer.Identity}' source clip type '{clip.GetType().Name}' is unsupported.");
                return null;
            }
            const string cueMarker = ":cue:";
            int marker = producer.Identity.LastIndexOf(cueMarker, StringComparison.Ordinal);
            if (marker >= 0)
            {
                string suffix = producer.Identity.Substring(marker + cueMarker.Length);
                int separator = suffix.IndexOf(':');
                string cueId = separator >= 0 ? suffix.Substring(separator + 1) : suffix;
                if (!string.IsNullOrEmpty(cueId))
                    return new CharacterPresentationCueBinding(cueId, "GameplayEffect");
            }
            errors?.Add($"Cue producer '{producer.Identity}' has no resolvable authoring payload.");
            return null;
        }

       static bool TryFindSourceClip(
            ProgramSourceMapEntry source,
            IReadOnlyDictionary<string, TimelineData> timelines,
            out Clip clip)
        {
            clip = null;
            if (source == null || string.IsNullOrEmpty(source.TimelineId) || string.IsNullOrEmpty(source.ClipId) ||
                !timelines.TryGetValue(source.TimelineId, out TimelineData timeline))
                return false;
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    if (string.Equals(track.Clips[clipIndex].AuthoringId, source.ClipId, StringComparison.Ordinal))
                    {
                        clip = track.Clips[clipIndex];
                        return true;
                    }
                }
            }
            return false;
        }

        static bool TryParseAnimationSource(string sourceIdentity, out AnimationProducerId producerId)
        {
            producerId = default;
            const string timelinePrefix = "timeline:";
            const string trackSeparator = "/track:";
            if (string.IsNullOrEmpty(sourceIdentity) || !sourceIdentity.StartsWith(timelinePrefix, StringComparison.Ordinal))
                return false;
            int separator = sourceIdentity.IndexOf(trackSeparator, timelinePrefix.Length, StringComparison.Ordinal);
            if (separator < 0)
                return false;
            producerId = new AnimationProducerId(
                sourceIdentity.Substring(timelinePrefix.Length, separator - timelinePrefix.Length),
                sourceIdentity.Substring(separator + trackSeparator.Length));
            return producerId.IsValid;
        }

        static string ParseTrackId(string sourceIdentity)
        {
            const string trackSeparator = "/track:";
            int separator = string.IsNullOrEmpty(sourceIdentity)
                ? -1
                : sourceIdentity.IndexOf(trackSeparator, StringComparison.Ordinal);
            return separator < 0 ? string.Empty : sourceIdentity.Substring(separator + trackSeparator.Length);
        }

    }
}
