using System;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    public sealed class TimelineEditorCursorState : IDisposable
    {
        TimelineData m_Timeline;

        public TimelineData Timeline => m_Timeline;
        public float Time { get; private set; }
        public int Frame => Mathf.RoundToInt(Time * TimelineUtility.FrameRate);
        public bool HasTimeline => m_Timeline != null;
        public event Action Changed;

        public void SetTimeline(TimelineData timeline, bool resetTime = true)
        {
            if (ReferenceEquals(m_Timeline, timeline) && !resetTime)
                return;
            m_Timeline = timeline;
            if (resetTime)
                Time = 0f;
            Time = ClampTime(Time);
            Changed?.Invoke();
        }

        public void Refresh(bool resetTime = false)
        {
            if (resetTime)
                Time = 0f;
            Time = ClampTime(Time);
            Changed?.Invoke();
        }

        public void SetTime(float time)
        {
            float nextTime = ClampTime(time);
            if (Mathf.Approximately(Time, nextTime))
                return;
            Time = nextTime;
            Changed?.Invoke();
        }

        public void Dispose()
        {
            m_Timeline = null;
            Changed = null;
        }

        float ClampTime(float time)
        {
            float duration = m_Timeline != null ? Mathf.Max(0f, m_Timeline.Duration) : 0f;
            return duration > 0f ? Mathf.Clamp(time, 0f, duration) : 0f;
        }
    }
}
