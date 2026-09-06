using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class TimelineSemanticContentDiscoveryResult
    {
        internal TimelineSemanticContentDiscoveryResult(
            TimelineSemanticContentRecord content,
            bool isValid,
            IReadOnlyList<string> errors)
        {
            Content = content;
            IsValid = content != null && isValid;
            Errors = new System.Collections.ObjectModel.ReadOnlyCollection<string>(
                new List<string>(errors ?? Array.Empty<string>()));
        }

        public TimelineSemanticContentRecord Content { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool IsValid { get; }
    }

    public static class TimelineSemanticContentDiscovery
    {
        public static TimelineSemanticContentDiscoveryResult Discover(
            TimelineData timeline,
            string route,
            TimelineSemanticEmitterRegistry emitters,
            CharacterSimulationCompileReport report)
        {
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            if (emitters == null)
                throw new ArgumentNullException(nameof(emitters));
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            string timelineRoute = route ?? string.Empty;
            var errors = new List<string>();
            bool isValid = true;
            void AddError(string code, string sourcePath, string message)
            {
                isValid = false;
                errors.Add($"{code}:{sourcePath}:{message}");
                report.DiscoveryError(code, sourcePath, message);
            }

            TimelineContentDiscoveryResult closure = TimelineContentDiscovery.Discover(
                timeline,
                TimelineTreeContractComposition.Create());
            if (!closure.IsValid)
            {
                for (int i = 0; i < closure.Errors.Count; i++)
                    AddError("timeline_content_invalid", timelineRoute, closure.Errors[i]);
                return new TimelineSemanticContentDiscoveryResult(null, false, errors);
            }

            var tracks = new List<TimelineSemanticTrackRecord>();
            var trees = new Dictionary<string, TimelineSemanticTreeRecord>(StringComparer.Ordinal);
            Track[] stableTracks = timeline.Tracks
                .Where(value => value != null)
                .OrderBy(value => value.AuthoringId, StringComparer.Ordinal)
                .ToArray();
            if (stableTracks.Length != timeline.Tracks.Count)
                AddError("timeline_track_missing", timelineRoute, "Timeline contains a missing Track.");

            for (int trackIndex = 0; trackIndex < stableTracks.Length; trackIndex++)
            {
                Track track = stableTracks[trackIndex];
                int authoringIndex = timeline.Tracks.IndexOf(track);
                string trackRoute = $"{timelineRoute}/track:{track.AuthoringId}";
                if (!emitters.TryGetTrack(track.GetType(), out _))
                    AddError("timeline_track_emitter_missing", trackRoute, $"Track type '{track.GetType().FullName}' has no Timeline semantic emitter.");
                Clip[] stableClips = track.Clips
                    .Where(value => value != null)
                    .OrderBy(value => value.AuthoringId, StringComparer.Ordinal)
                    .ToArray();
                if (stableClips.Length != track.Clips.Count)
                    AddError("timeline_clip_missing", trackRoute, "Timeline Track contains a missing Clip.");
                var clips = new List<TimelineSemanticClipRecord>();
                for (int clipIndex = 0; clipIndex < stableClips.Length; clipIndex++)
                {
                    Clip clip = stableClips[clipIndex];
                    string clipRoute = $"{trackRoute}/clip:{clip.AuthoringId}";
                    if (!emitters.TryGetClip(clip.GetType(), out _))
                        AddError("timeline_clip_emitter_missing", clipRoute, $"Clip type '{clip.GetType().FullName}' has no Timeline semantic emitter.");
                    if (clip.EndFrame <= clip.StartFrame)
                        AddError("timeline_clip_range_invalid", clipRoute, "Timeline Clip requires EndFrame greater than StartFrame.");
                    if (clip is BTSMTL.Timeline.AnimationClip animation && !animation.Clip)
                        AddError("animation_clip_missing", clipRoute, "AnimationClip resource is missing from the authoring source.");
                    if (clip is TreeClip treeClip && treeClip.ResolvedTree != null)
                    {
                        string treeRoute = $"{clipRoute}/tree:{treeClip.ResolvedTree.GraphAuthoringId}";
                        trees[clip.AuthoringId] = new TimelineSemanticTreeRecord(treeClip, treeClip.ResolvedTree, treeRoute);
                    }
                    clips.Add(new TimelineSemanticClipRecord(clip, track.Clips.IndexOf(clip), clipRoute));
                }
                tracks.Add(new TimelineSemanticTrackRecord(track, authoringIndex, trackRoute, clips));
            }

            return new TimelineSemanticContentDiscoveryResult(
                new TimelineSemanticContentRecord(
                    timeline,
                    timelineRoute,
                    closure.Content.MaxFrame,
                    closure.Content,
                    tracks,
                    trees),
                isValid,
                errors);
        }
    }
}
