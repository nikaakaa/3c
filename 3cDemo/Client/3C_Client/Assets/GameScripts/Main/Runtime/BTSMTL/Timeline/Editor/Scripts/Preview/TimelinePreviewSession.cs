using System;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    public sealed class TimelinePreviewSession : IDisposable
    {
        const string SourceId = "TimelinePreview";
        const string SourceName = "Timeline Preview";

        Guid m_SessionId = Guid.NewGuid();
        TimelineData m_SourceTimeline;
        TimelineData m_RuntimeTimeline;
        TimelinePreviewTarget m_Target;
        ulong m_EvaluationTick;
        float m_PlaySpeed = 1f;

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
        public int Frame => Mathf.RoundToInt(Time * TimelineUtility.FrameRate);

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
            if (resetTime)
                Time = 0f;

            RefreshTimeline(false);
        }

        public void RefreshTimeline(bool resetTime = false)
        {
            if (resetTime)
                Time = 0f;

            ReleaseRuntimeTimeline();
            if (m_SourceTimeline == null)
            {
                IsPlaying = false;
                ClearPreviewOutput();
                Evaluated?.Invoke();
                return;
            }

            m_RuntimeTimeline = m_SourceTimeline.Clone();
            m_RuntimeTimeline.Init();
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
            EvaluateAt(Time, Time, 0f, true);
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
            ReleaseRuntimeTimeline();
            m_SourceTimeline = null;
            m_Target = null;
            Evaluated = null;
        }

        void EvaluateAt(
            float previousTime,
            float currentTime,
            float presentationDeltaSeconds,
            bool resetLifecycle)
        {
            if (m_RuntimeTimeline == null)
            {
                m_Target?.ClearTimelinePreview(m_SessionId);
                Evaluated?.Invoke();
                return;
            }

            m_RuntimeTimeline.Time = currentTime;
            if (CanPreview)
                m_Target.EvaluateTimelinePreview(
                    m_SessionId,
                    m_RuntimeTimeline,
                    previousTime,
                    currentTime,
                    SourceId,
                    SourceName,
                    NextEvaluationTick(),
                    presentationDeltaSeconds,
                    resetLifecycle);
            else
                m_Target?.ClearTimelinePreview(m_SessionId);
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

        float ClampTime(float time)
        {
            float duration = GetDuration();
            return duration > 0f ? Mathf.Clamp(time, 0f, duration) : 0f;
        }

        float GetDuration()
        {
            if (m_RuntimeTimeline != null)
                return Mathf.Max(0f, m_RuntimeTimeline.Duration);
            return m_SourceTimeline != null ? Mathf.Max(0f, m_SourceTimeline.Duration) : 0f;
        }

        void ReleaseRuntimeTimeline()
        {
            if (m_RuntimeTimeline == null)
                return;
            m_RuntimeTimeline = null;
        }

    }
}
