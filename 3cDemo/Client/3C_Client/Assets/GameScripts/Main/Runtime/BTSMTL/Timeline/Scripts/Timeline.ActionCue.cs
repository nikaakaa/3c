using System;
using System.Collections.Generic;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public readonly struct TimelineActionCueSample
    {
        public TimelineActionCueSample(string sourceId, string sourceName, string trackName, string cueId, string cueType)
        {
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            TrackName = trackName ?? string.Empty;
            CueId = cueId ?? string.Empty;
            CueType = cueType ?? string.Empty;
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public string TrackName { get; }
        public string CueId { get; }
        public string CueType { get; }
    }

    [TrackGroup("Base"), ScriptGuid("43f20139703b4e96a6c8f201f0a703c7"), Ordered(3), Color(255, 210, 92)]
    public sealed class ActionCueTrack : Track
    {
        public void Sample(float previousTime, float timelineTime, string sourceId, string sourceName, ICollection<TimelineActionCueSample> cues)
        {
            if (m_PersistentMuted || cues == null)
                return;

            foreach (var clip in Clips)
            {
                if (clip is not ActionCueClip actionCueClip)
                    continue;

                if (previousTime < actionCueClip.StartTime && actionCueClip.StartTime <= timelineTime)
                {
                    cues.Add(new TimelineActionCueSample(
                        sourceId,
                        sourceName,
                        Name,
                        actionCueClip.CueId,
                        actionCueClip.CueType));
                }
            }
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(ActionCueClip);
#endif
    }

    [ScriptGuid("43f20139703b4e96a6c8f201f0a703c7"), Color(255, 210, 92)]
    public sealed class ActionCueClip : SignalClip
    {
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string CueId = "Cue";
        [ShowInInspector, OnValueChanged("RebindTimeline")]
        public string CueType = "Gameplay";

#if UNITY_EDITOR
        public ActionCueClip(Track track, int frame) : base(track, frame)
        {
        }
#endif
    }
}
