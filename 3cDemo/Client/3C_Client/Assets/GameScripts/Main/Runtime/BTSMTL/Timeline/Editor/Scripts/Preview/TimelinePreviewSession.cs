using System;
using System.Collections.Generic;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    public sealed class TimelinePreviewSession : IDisposable
    {
        const string SourceId = "TimelinePreview";
        const string SourceName = "Timeline Preview";

        Guid m_SessionId = Guid.NewGuid();
        TimelineData m_SourceTimeline;
        TimelinePreviewTarget m_Target;
        ulong m_EvaluationTick;
        float m_PlaySpeed = 1f;
        string m_MarkerSyncTargetTrackId = string.Empty;
        string m_MarkerSyncSourceTimelineId = string.Empty;
        string m_MarkerSyncSourceTrackId = string.Empty;
        readonly List<TimelineAnimationMarkerSyncPreviewCandidate> m_MarkerSyncSources =
            new List<TimelineAnimationMarkerSyncPreviewCandidate>();

        public TimelineData Timeline => m_SourceTimeline;
        public Guid SessionId => m_SessionId;
        public TimelinePreviewTarget Target => m_Target;
        public float Time { get; private set; }
        public float PlaySpeed
        {
            get => m_PlaySpeed;
            set => m_PlaySpeed = Mathf.Max(0.001f, value);
        }
        public bool IsPlaying { get; set; }
        public bool HasTimeline => m_SourceTimeline != null;
        public bool HasTarget => m_Target && m_Target.CanPreviewTimeline;
        public bool CanPreview => HasTimeline && HasTarget;
        public string Status => !string.IsNullOrEmpty(Error)
            ? Error
            : m_Target
                ? m_Target.PreviewStatus
                : string.Empty;
        public int Frame => Mathf.RoundToInt(Time * TimelineUtility.FrameRate);
        public string Error { get; private set; } = string.Empty;
        public string MarkerSyncTargetTrackId => m_MarkerSyncTargetTrackId;
        public string MarkerSyncSourceTimelineId => m_MarkerSyncSourceTimelineId;
        public string MarkerSyncSourceTrackId => m_MarkerSyncSourceTrackId;
        public IReadOnlyList<TimelineAnimationMarkerSyncPreviewCandidate> MarkerSyncSources => m_MarkerSyncSources;

        public event Action Evaluated;

        public void SetTimeline(TimelineData timeline, bool resetTime = true)
        {
            if (m_SourceTimeline == timeline && !resetTime)
            {
                RefreshTimeline(false);
                return;
            }

            if (m_SourceTimeline != timeline)
            {
                ClearPreviewOutput();
                m_SessionId = Guid.NewGuid();
                m_EvaluationTick = 0;
            }

            m_SourceTimeline = timeline;
            m_MarkerSyncTargetTrackId = string.Empty;
            m_MarkerSyncSourceTimelineId = string.Empty;
            m_MarkerSyncSourceTrackId = string.Empty;
            m_MarkerSyncSources.Clear();
            if (resetTime)
                Time = 0f;

            RefreshTimeline(false);
        }

        public void RefreshTimeline(bool resetTime = false)
        {
            if (resetTime)
                Time = 0f;

            if (m_SourceTimeline == null)
            {
                IsPlaying = false;
                ClearPreviewOutput();
                Evaluated?.Invoke();
                return;
            }

            Time = ClampTime(Time);
            EvaluateAt(Time, Time, 0f, true);
        }

        public void SetTarget(TimelinePreviewTarget target)
        {
            if (m_Target == target)
                return;

            ClearPreviewOutput();
            m_SessionId = Guid.NewGuid();
            m_EvaluationTick = 0;
            m_Target = target;
            RefreshMarkerSyncSources();
            EvaluateAt(Time, Time, 0f, true);
        }

        public void SetMarkerSyncTargetTrack(string trackAuthoringId)
        {
            string canonical = trackAuthoringId ?? string.Empty;
            if (string.Equals(m_MarkerSyncTargetTrackId, canonical, StringComparison.Ordinal))
                return;
            m_MarkerSyncTargetTrackId = canonical;
            m_MarkerSyncSourceTimelineId = string.Empty;
            m_MarkerSyncSourceTrackId = string.Empty;
            RefreshMarkerSyncSources();
            ConfigureMarkerSyncSource();
            EvaluateAt(Time, Time, 0f, true);
        }

        public void SetMarkerSyncSource(string timelineAuthoringId, string trackAuthoringId)
        {
            string timelineId = timelineAuthoringId ?? string.Empty;
            string trackId = trackAuthoringId ?? string.Empty;
            if (string.Equals(m_MarkerSyncSourceTimelineId, timelineId, StringComparison.Ordinal) &&
                string.Equals(m_MarkerSyncSourceTrackId, trackId, StringComparison.Ordinal))
                return;
            m_MarkerSyncSourceTimelineId = timelineId;
            m_MarkerSyncSourceTrackId = trackId;
            ConfigureMarkerSyncSource();
            EvaluateAt(Time, Time, 0f, true);
        }

        public bool TryGetMarkerSyncPreviewState(out TimelineAnimationMarkerSyncPreviewState state)
        {
            if (m_Target && !string.IsNullOrEmpty(m_MarkerSyncTargetTrackId))
                return m_Target.TryGetAnimationMarkerSyncPreviewState(
                    m_SessionId,
                    m_MarkerSyncTargetTrackId,
                    out state);
            state = default;
            return false;
        }

        public void Play()
        {
            if (CanPreview)
                IsPlaying = true;
        }

        public void Pause()
        {
            IsPlaying = false;
        }

        public void Tick(float deltaTime)
        {
            if (!IsPlaying)
                return;

            if (!CanPreview)
            {
                Pause();
                ClearPreviewOutput();
                return;
            }

            float previousTime = Time;
            float presentationDeltaSeconds = Mathf.Max(0f, deltaTime);
            SetTimeInternal(Time + presentationDeltaSeconds * PlaySpeed, presentationDeltaSeconds, false);
            if (Mathf.Approximately(previousTime, Time) || Time >= GetDuration())
                Pause();
        }

        public void SetTime(float time)
        {
            SetTimeInternal(time, 0f, true);
        }

        void SetTimeInternal(float time, float presentationDeltaSeconds, bool resetLifecycle)
        {
            float previousTime = Time;
            Time = ClampTime(time);
            EvaluateAt(previousTime, Time, presentationDeltaSeconds, resetLifecycle);
        }

        public void Dispose()
        {
            Pause();
            ClearPreviewOutput();
            m_SourceTimeline = null;
            m_Target = null;
            m_MarkerSyncSources.Clear();
            Evaluated = null;
        }

        void EvaluateAt(
            float previousTime,
            float currentTime,
            float presentationDeltaSeconds,
            bool resetLifecycle)
        {
            if (m_SourceTimeline == null)
            {
                Error = string.Empty;
                m_Target?.ClearTimelinePreview(m_SessionId);
                Evaluated?.Invoke();
                return;
            }

            if (CanPreview)
            {
                try
                {
                    m_Target.EvaluateTimelinePreview(
                        m_SessionId,
                        m_SourceTimeline,
                        previousTime,
                        currentTime,
                        SourceId,
                        SourceName,
                        NextEvaluationTick(),
                        presentationDeltaSeconds,
                        resetLifecycle);
                    Error = string.Empty;
                }
                catch (InvalidOperationException exception)
                {
                    Error = exception.Message;
                    IsPlaying = false;
                    m_Target.ClearTimelinePreview(m_SessionId);
                }
            }
            else
            {
                Error = string.Empty;
                m_Target?.ClearTimelinePreview(m_SessionId);
            }
            Evaluated?.Invoke();
        }

        ulong NextEvaluationTick()
        {
            m_EvaluationTick++;
            if (m_EvaluationTick == 0)
                m_EvaluationTick++;
            return m_EvaluationTick;
        }

        void ClearPreviewOutput()
        {
            m_Target?.ClearTimelinePreview(m_SessionId);
        }

        void RefreshMarkerSyncSources()
        {
            m_MarkerSyncSources.Clear();
            if (!m_Target || m_SourceTimeline == null || string.IsNullOrEmpty(m_MarkerSyncTargetTrackId))
                return;
            m_Target.CollectAnimationMarkerSyncPreviewSources(
                m_SourceTimeline,
                m_MarkerSyncTargetTrackId,
                m_MarkerSyncSources);
        }

        void ConfigureMarkerSyncSource()
        {
            if (!m_Target || m_SourceTimeline == null)
                return;
            m_Target.ConfigureAnimationMarkerSyncPreviewSource(
                m_SessionId,
                m_SourceTimeline.AuthoringId,
                m_MarkerSyncTargetTrackId,
                m_MarkerSyncSourceTimelineId,
                m_MarkerSyncSourceTrackId);
        }

        float ClampTime(float time)
        {
            float duration = GetDuration();
            return duration > 0f ? Mathf.Clamp(time, 0f, duration) : 0f;
        }

        float GetDuration()
        {
            return m_SourceTimeline != null ? Mathf.Max(0f, m_SourceTimeline.Duration) : 0f;
        }
    }
}
