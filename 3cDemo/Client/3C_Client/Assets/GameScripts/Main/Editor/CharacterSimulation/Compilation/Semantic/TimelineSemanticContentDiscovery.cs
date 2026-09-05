using System;
using System.Collections.Generic;
using System.Linq;
using BTSMTL.Timeline;
using ThirdPersonSimulation;
using UnityEditor;

namespace ThirdPersonCharacter.Pipeline.Simulation.Editor
{
    public sealed class TimelineSemanticContentRecord
    {
        internal TimelineSemanticContentRecord(
            TimelineAsset asset,
            string assetGuid,
            string route,
            string contentHash,
            IEnumerable<CharacterAuthoringTrackRecord> tracks)
        {
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            Timeline = asset.Data ?? throw new ArgumentException("Timeline asset has no data.", nameof(asset));
            AssetGuid = SimulationIdentity.Require(assetGuid, nameof(assetGuid));
            Route = SimulationIdentity.Require(route, nameof(route));
            ContentHash = SimulationIdentity.Require(contentHash, nameof(contentHash));
            Tracks = Array.AsReadOnly((tracks ?? Array.Empty<CharacterAuthoringTrackRecord>()).ToArray());
        }

        public TimelineAsset Asset { get; }
        public TimelineData Timeline { get; }
        public string AssetGuid { get; }
        public string Route { get; }
        public string ContentHash { get; }
        public IReadOnlyList<CharacterAuthoringTrackRecord> Tracks { get; }
    }

    public static class TimelineSemanticContentDiscovery
    {
        public static TimelineSemanticContentRecord Discover(
            TimelineAsset asset,
            CharacterSimulationCompileReport report)
        {
            if (!asset)
                throw new ArgumentNullException(nameof(asset));
            if (report == null)
                throw new ArgumentNullException(nameof(report));
            string path = AssetDatabase.GetAssetPath(asset);
            string guid = string.IsNullOrEmpty(path)
                ? string.Empty
                : AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(guid))
            {
                report.DiscoveryError(
                    "timeline_root_identity_missing",
                    asset.name,
                    "Timeline root must be a persisted asset with a GUID.");
                return null;
            }
            TimelineData timeline = asset.Data;
            if (timeline == null)
            {
                report.DiscoveryError(
                    "timeline_data_missing",
                    path,
                    "Timeline asset has no TimelineData.");
                return null;
            }
            var identityErrors = new List<string>();
            if (!timeline.ValidateAuthoringIdentities(identityErrors))
            {
                for (int i = 0; i < identityErrors.Count; i++)
                    report.DiscoveryError("timeline_authoring_identity_invalid", path, identityErrors[i]);
                return null;
            }
            timeline.Init();
            CharacterSimulationTimelineEmitterRegistry emitters =
                CharacterSimulationTimelineEmitterRegistry.CreateDefault();
            var tracks = new List<CharacterAuthoringTrackRecord>();
            Track[] orderedTracks = timeline.Tracks
                .Where(value => value != null)
                .OrderBy(value => value.AuthoringId, StringComparer.Ordinal)
                .ToArray();
            for (int trackIndex = 0; trackIndex < orderedTracks.Length; trackIndex++)
            {
                Track track = orderedTracks[trackIndex];
                int authoringIndex = timeline.Tracks.IndexOf(track);
                string trackRoute = $"timeline:{timeline.AuthoringId}/track:{track.AuthoringId}";
                if (!emitters.TryGetTrack(track.GetType(), out _))
                    report.DiscoveryError("timeline_track_emitter_missing", trackRoute, $"Track type '{track.GetType().FullName}' has no semantic emitter.");
                var clips = new List<CharacterAuthoringClipRecord>();
                Clip[] orderedClips = track.Clips
                    .Where(value => value != null)
                    .OrderBy(value => value.AuthoringId, StringComparer.Ordinal)
                    .ToArray();
                for (int clipIndex = 0; clipIndex < orderedClips.Length; clipIndex++)
                {
                    Clip clip = orderedClips[clipIndex];
                    string clipRoute = $"{trackRoute}/clip:{clip.AuthoringId}";
                    if (!emitters.TryGetClip(clip.GetType(), out _))
                        report.DiscoveryError("timeline_clip_emitter_missing", clipRoute, $"Clip type '{clip.GetType().FullName}' has no semantic emitter.");
                    if (clip.EndFrame <= clip.StartFrame)
                        report.DiscoveryError("timeline_clip_range_invalid", clipRoute, "Clip EndFrame must be greater than StartFrame.");
                    if (clip is BTSMTL.Timeline.AnimationClip animation && !animation.Clip)
                        report.DiscoveryError("timeline_animation_clip_missing", clipRoute, "Animation clip resource is missing.");
                    if (clip is MotionCurveClip || clip is MotionWarpClip)
                        report.DiscoveryError("timeline_character_capability_required", clipRoute, "MotionCurve and MotionWarp require the Character Body Motion or Action binding and cannot be a standalone Timeline root.");
                    if (clip is TreeClip treeClip)
                    {
                        string code = treeClip.ResolvedTree == null
                            ? "timeline_tree_clip_missing"
                            : "timeline_tree_clip_content_unavailable";
                        string message = treeClip.ResolvedTree == null
                            ? "TreeClip graph is missing."
                            : "Standalone Timeline content closure does not yet include TreeClip graph operations.";
                        report.DiscoveryError(code, clipRoute, message);
                    }
                    clips.Add(new CharacterAuthoringClipRecord(
                        clip,
                        track.Clips.IndexOf(clip),
                        clipRoute,
                        null));
                }
                tracks.Add(new CharacterAuthoringTrackRecord(track, authoringIndex, trackRoute, clips));
            }
            if (!report.IsValid)
                return null;
            return new TimelineSemanticContentRecord(
                asset,
                guid,
                $"timeline:{timeline.AuthoringId}",
                TimelineAuthoringFingerprint.Compute(timeline),
                tracks);
        }
    }
}
