using System;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    public sealed class AnimationSequencePreviewSession : IDisposable
    {
        Guid m_SessionId = Guid.NewGuid();
        AnimationSequenceAsset m_Sequence;
        TimelinePreviewTarget m_Target;
        ulong m_EvaluationTick;
        float m_PlaySpeed = 1f;

        public AnimationSequenceAsset Sequence => m_Sequence;
        public TimelinePreviewTarget Target => m_Target;
        public float Time { get; private set; }
        public int Frame => m_Sequence
            ? Mathf.RoundToInt(Time * m_Sequence.Clip.frameRate)
            : 0;
        public float PlaySpeed
        {
            get => m_PlaySpeed;
            set => m_PlaySpeed = Mathf.Max(0.001f, value);
        }
        public bool IsPlaying { get; private set; }
        public string Error { get; private set; } = string.Empty;
        public bool CanPreview => ResolveStatus(out _);
        public string Status
        {
            get
            {
                if (!string.IsNullOrEmpty(Error))
                    return Error;
                ResolveStatus(out string status);
                return status;
            }
        }

        public event Action Evaluated;

        public void SetSequence(AnimationSequenceAsset sequence, bool resetTime = true)
        {
            if (m_Sequence != sequence)
            {
                ClearPreviewOutput();
                m_SessionId = Guid.NewGuid();
                m_EvaluationTick = 0;
            }
            m_Sequence = sequence;
            if (resetTime)
                Time = 0f;
            Refresh(false);
        }

        public void SetTarget(TimelinePreviewTarget target)
        {
            if (m_Target == target)
                return;
            ClearPreviewOutput();
            m_SessionId = Guid.NewGuid();
            m_EvaluationTick = 0;
            m_Target = target;
            Refresh(false);
        }

        public void Refresh(bool resetTime)
        {
            if (resetTime)
                Time = 0f;
            Time = ClampTime(Time);
            EvaluateAt(Time, Time, 0f, true);
        }

        public void Play()
        {
            if (CanPreview)
                IsPlaying = true;
        }

        public void Pause() => IsPlaying = false;

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
            float duration = Duration;
            float delta = Mathf.Max(0f, deltaTime) * PlaySpeed * m_Sequence.DefaultPlayRate;
            float next = Time + delta;
            if (m_Sequence.Loop && duration > 0f)
                next %= duration;
            else if (next >= duration)
            {
                next = duration;
                Pause();
            }
            SetTimeInternal(next, delta, false);
        }

        public void SetTime(float time)
        {
            Pause();
            SetTimeInternal(time, 0f, true);
        }

        public void Dispose()
        {
            Pause();
            ClearPreviewOutput();
            m_Sequence = null;
            m_Target = null;
            Evaluated = null;
        }

        void SetTimeInternal(float time, float presentationDeltaSeconds, bool resetLifecycle)
        {
            float previous = Time;
            Time = ClampTime(time);
            EvaluateAt(previous, Time, presentationDeltaSeconds, resetLifecycle);
        }

        void EvaluateAt(
            float previousTime,
            float currentTime,
            float presentationDeltaSeconds,
            bool resetLifecycle)
        {
            if (ResolveStatus(out _))
            {
                try
                {
                    m_Target.EvaluateAnimationSequencePreview(
                        m_SessionId,
                        m_Sequence,
                        previousTime,
                        currentTime,
                        NextEvaluationTick(),
                        presentationDeltaSeconds,
                        resetLifecycle);
                    Error = string.Empty;
                }
                catch (InvalidOperationException exception)
                {
                    Error = exception.Message;
                    Pause();
                    ClearPreviewOutput();
                }
            }
            else
            {
                Error = string.Empty;
                ClearPreviewOutput();
            }
            Evaluated?.Invoke();
        }

        bool ResolveStatus(out string status)
        {
            if (!m_Sequence)
            {
                status = string.Empty;
                return false;
            }
            if (!m_Target)
            {
                status = "Sequence Preview: select a scene Preview Target.";
                return false;
            }
            return m_Target.TryGetAnimationSequencePreviewStatus(m_Sequence, out status);
        }

        void ClearPreviewOutput() =>
            m_Target?.ClearAnimationSequencePreview(m_SessionId);

        ulong NextEvaluationTick()
        {
            m_EvaluationTick++;
            if (m_EvaluationTick == 0)
                m_EvaluationTick++;
            return m_EvaluationTick;
        }

        float ClampTime(float time) =>
            Duration > 0f ? Mathf.Clamp(time, 0f, Duration) : 0f;

        float Duration => m_Sequence && m_Sequence.Clip
            ? Mathf.Max(0f, m_Sequence.Clip.length)
            : 0f;
    }
}
