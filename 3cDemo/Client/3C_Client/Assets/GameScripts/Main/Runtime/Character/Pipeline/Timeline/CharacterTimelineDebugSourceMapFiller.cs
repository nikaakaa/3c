using System;
using System.Collections.Generic;
using BTSMTL.Diagnostics;
using BTSMTL.Timeline;

namespace ThirdPersonCharacter.Pipeline.Animation.Lifecycle
{
    public static class CharacterTimelineDebugSourceMapFiller
    {
        public static void Fill(DebugSourceMap sourceMap, IReadOnlyList<TimelineAsset> timelines)
        {
            if (sourceMap == null)
                throw new ArgumentNullException(nameof(sourceMap));
            if (timelines == null)
                throw new ArgumentNullException(nameof(timelines));
            for (int index = 0; index < timelines.Count; index++)
            {
                TimelineAsset asset = timelines[index];
                if (!asset || asset.Data == null)
                    throw new InvalidOperationException($"Timeline source #{index} is invalid.");
                Fill(sourceMap, asset.Data);
            }
        }

        public static void Fill(DebugSourceMap sourceMap, TimelineData timeline)
        {
            if (sourceMap == null)
                throw new ArgumentNullException(nameof(sourceMap));
            if (timeline == null)
                throw new ArgumentNullException(nameof(timeline));
            timeline.Init();
            string contentHash = TimelineAuthoringFingerprint.Compute(timeline);
            RuntimeSourceElementHandle timelineHandle = sourceMap.Add(
                RuntimeSourceElementKey.Timeline(timeline.AuthoringId),
                RuntimeSourceElementHandle.Invalid,
                timeline.Name,
                contentHash,
                RuntimeSourceTarget.Source);
            for (int trackIndex = 0; trackIndex < timeline.Tracks.Count; trackIndex++)
            {
                Track track = timeline.Tracks[trackIndex];
                if (track == null)
                    continue;
                RuntimeSourceElementHandle trackHandle = sourceMap.Add(
                    RuntimeSourceElementKey.Track(timeline.AuthoringId, track.AuthoringId),
                    timelineHandle,
                    track.Name,
                    contentHash,
                    RuntimeSourceTarget.Source);
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip clip = track.Clips[clipIndex];
                    if (clip == null)
                        continue;
                    sourceMap.Add(
                        RuntimeSourceElementKey.Clip(
                            timeline.AuthoringId,
                            track.AuthoringId,
                            clip.AuthoringId,
                            clip is TreeClip),
                        trackHandle,
                        clip.Name,
                        contentHash,
                        RuntimeSourceTarget.Source);
                }
            }
        }
    }
}
