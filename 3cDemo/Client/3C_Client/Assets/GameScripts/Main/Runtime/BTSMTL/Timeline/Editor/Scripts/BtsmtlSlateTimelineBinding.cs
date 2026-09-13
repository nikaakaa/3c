#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    sealed class BtsmtlSlateTimelineBinding
    {
        readonly TimelineEditorOpenRequest m_Request;
        readonly TimelineEditorSessionContext m_Session;
        readonly string m_SourceRevision;

        public BtsmtlSlateTimelineBinding(
            TimelineEditorOpenRequest request,
            TimelineEditorSessionContext session)
        {
            m_Request = request ?? throw new ArgumentNullException(nameof(request));
            m_Session = session ?? throw new ArgumentNullException(nameof(session));
            m_SourceRevision = TimelineAuthoringFingerprint.Compute(request.Timeline);
        }

        public TimelineData Timeline => m_Request.Timeline;
        public TimelineContractCatalog ContractCatalog => m_Request.ContractCatalog;
        public int FrameRate => m_Session.FrameRate;
        public float Duration => Timeline.Duration;
        public IReadOnlyList<Track> Tracks => Timeline.Tracks;
        public IReadOnlyList<TimelineSection> Sections => Timeline.Sections;
        public TimelineEditorSessionContext Session => m_Session;

        public bool IsSourceCurrent()
        {
            return string.Equals(
                m_SourceRevision,
                TimelineAuthoringFingerprint.Compute(Timeline),
                StringComparison.Ordinal);
        }

        public bool TryGetTrack(string authoringId, out Track track)
        {
            track = null;
            if (string.IsNullOrEmpty(authoringId))
                return false;
            for (int index = 0; index < Tracks.Count; index++)
            {
                Track candidate = Tracks[index];
                if (candidate != null && string.Equals(candidate.AuthoringId, authoringId, StringComparison.Ordinal))
                {
                    track = candidate;
                    return true;
                }
            }
            return false;
        }

        public bool TryGetClip(string authoringId, out Clip clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(authoringId))
                return false;
            for (int trackIndex = 0; trackIndex < Tracks.Count; trackIndex++)
            {
                Track track = Tracks[trackIndex];
                if (track == null)
                    continue;
                for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
                {
                    Clip candidate = track.Clips[clipIndex];
                    if (candidate != null && string.Equals(candidate.AuthoringId, authoringId, StringComparison.Ordinal))
                    {
                        clip = candidate;
                        return true;
                    }
                }
            }
            return false;
        }

        public List<TimelineCurveChannelDescriptor> CollectCurveChannels(Track track)
        {
            var result = new List<TimelineCurveChannelDescriptor>();
            if (track != null)
                TimelineCurveChannelCatalog.CollectForTrack(track, result);
            return result;
        }

        public bool TryGetCurve(Clip clip, TimelineCurveChannelDescriptor descriptor, out AnimationCurve curve)
        {
            curve = null;
            if (clip == null || descriptor == null || !descriptor.Supports(clip))
                return false;
            curve = descriptor.Read(clip);
            return curve != null;
        }

        public void Apply(Action mutation, string undoName)
        {
            m_Session.Apply(mutation, undoName);
        }
    }
}
#endif
