using System;
using System.Collections.Generic;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public readonly struct TimelineActionCueSample
    {
        public TimelineActionCueSample(
            string sourceId,
            string sourceName,
            string trackName,
            string clipAuthoringId,
            string cueId,
            string cueType,
            string stateId,
            int localFrame)
        {
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            TrackName = trackName ?? string.Empty;
            ClipAuthoringId = clipAuthoringId ?? string.Empty;
            CueId = cueId ?? string.Empty;
            CueType = cueType ?? string.Empty;
            StateId = stateId?.Trim() ?? string.Empty;
            LocalFrame = localFrame;
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public string TrackName { get; }
        public string ClipAuthoringId { get; }
        public string CueId { get; }
        public string CueType { get; }
        public string StateId { get; }
        public int LocalFrame { get; }
    }

    [TrackGroup("Base"), ScriptGuid("43f20139703b4e96a6c8f201f0a703c7"), Ordered(3), Color(255, 210, 92)]
    public sealed class ActionCueTrack : Track
    {
        public override string ContractKind => TimelineContractKinds.ActionCueTrack;

        public void Sample(
            float previousTime,
            float timelineTime,
            string sourceId,
            string sourceName,
            ICollection<TimelineActionCueSample> cues,
            bool includeStartBoundary = false,
            Func<Clip, bool> clipFilter = null)
        {
            if (m_PersistentMuted || cues == null)
                return;

            foreach (var clip in Clips)
            {
                if (clip is not ActionCueClip actionCueClip)
                    continue;
                if (clipFilter != null && !clipFilter(actionCueClip))
                {
                    continue;
                }

                if ((includeStartBoundary && Mathf.Abs(actionCueClip.StartTime) <= 0.000001f) ||
                    previousTime < actionCueClip.StartTime && actionCueClip.StartTime <= timelineTime)
                {
                    string stateId = string.Empty;
                    int localFrame = 0;
                    IReadOnlyList<TimelineSection> sections = actionCueClip.Timeline.Sections;
                    for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
                    {
                        TimelineSection section = sections[sectionIndex];
                        if (section.Frame > actionCueClip.StartFrame)
                            break;
                        stateId = section.Name;
                        localFrame = actionCueClip.StartFrame - section.Frame + 1;
                    }

                    cues.Add(new TimelineActionCueSample(
                        sourceId,
                        sourceName,
                        Name,
                        actionCueClip.AuthoringId,
                        actionCueClip.CueId,
                        actionCueClip.CueType,
                        stateId,
                        localFrame));
                }
            }
        }

#if UNITY_EDITOR
        public override Type ClipType => typeof(ActionCueClip);
#endif
    }

    [ScriptGuid("43f20139703b4e96a6c8f201f0a703c7"), Color(255, 210, 92)]
    [TimelineAuthoringProperty("cueId", TimelineAuthoringPropertyKind.Text, Trimmed = true)]
    [TimelineAuthoringProperty("cueType", TimelineAuthoringPropertyKind.Text, Trimmed = true)]
    public sealed class ActionCueClip : SignalClip
    {
        public override ClipCapabilities Capabilities => ClipCapabilities.TickQuantized;
        public override string ContractKind => TimelineContractKinds.ActionCueClip;

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

    public static class ActionCueTimelineContracts
    {
        public static readonly ITimelineContractProvider Provider = new TimelineContractProvider(
            new[]
            {
                new TimelineTrackContract(
                    TimelineContractKinds.ActionCueTrack,
                    TimelineTrackOverlapPolicy.Parallel,
                    TimelineCapability.Cue,
                    TimelineExecutionDomain.Logic,
                    TimelineOutputKind.GameplayFact,
                    TimelineContractKinds.ActionCueClip)
            },
            new[]
            {
                new TimelineClipContract(
                    TimelineContractKinds.ActionCueClip,
                    TimelineContractKinds.ActionCueTrack,
                    TimelineClipExecutionPhase.Commit,
                    TimelineCapability.Cue,
                    false,
                    false,
                    TimelineExecutionDomain.Logic,
                    TimelineOutputKind.GameplayFact)
            });
    }
}

