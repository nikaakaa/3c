using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BTSMTL.Timeline.Editor
{
    internal sealed class TimelineFrameGeometry : IAnimationTimeFieldGeometry
    {
        const float MarkerWidth = 50f;
        const float FieldOffset = 6f;

        float m_Scale = 1f;
        int m_MaxFrame = 60;

        public float Scale
        {
            get => m_Scale;
            set => m_Scale = Mathf.Max(0.01f, value);
        }

        public int MaxFrame => m_MaxFrame;
        public int DurationFrames => m_MaxFrame;
        public float OneFrameWidth => MarkerWidth * m_Scale;
        public float FieldOffsetX => FieldOffset;

        public void ResetExtent(int maxFrame)
        {
            m_MaxFrame = Mathf.Max(60, maxFrame);
        }

        public void ResizeExtent(float contentWidth, float viewportWidth)
        {
            int visibleFrames = Mathf.CeilToInt(Mathf.Max(contentWidth, viewportWidth) / OneFrameWidth);
            EnsureFrameCapacity(visibleFrames);
        }

        public void EnsureFrameCapacity(int inclusiveFrame)
        {
            m_MaxFrame = Mathf.Max(m_MaxFrame, inclusiveFrame + 1);
        }

        public float FrameToPosition(int frame)
        {
            return Mathf.Max(0, frame) * OneFrameWidth + FieldOffset;
        }

        public float FrameToTime(int frame)
        {
            return Mathf.Max(0, frame) / (float)TimelineUtility.FrameRate;
        }

        public int TimeToFrame(float time)
        {
            return Mathf.Max(0, Mathf.RoundToInt(time * TimelineUtility.FrameRate));
        }

        public float FrameToClipNormalizedTime(Clip clip, int frame)
        {
            if (clip == null || clip.EndFrame <= clip.StartFrame)
                throw new System.InvalidOperationException("Timeline clip must have a positive frame duration.");
            return Mathf.InverseLerp(clip.StartFrame, clip.EndFrame, frame);
        }

        public int ClipNormalizedTimeToFrame(Clip clip, float normalizedTime)
        {
            if (clip == null || clip.EndFrame <= clip.StartFrame)
                throw new System.InvalidOperationException("Timeline clip must have a positive frame duration.");
            return Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(clip.StartFrame, clip.EndFrame, Mathf.Clamp01(normalizedTime))),
                clip.StartFrame,
                clip.EndFrame);
        }

        public float PositionToClipNormalizedTime(Clip clip, float position)
        {
            return FrameToClipNormalizedTime(clip, PositionToClosestFrame(position));
        }

        public float ClipNormalizedTimeToPosition(Clip clip, float normalizedTime)
        {
            return FrameToPosition(ClipNormalizedTimeToFrame(clip, normalizedTime));
        }

        public int PositionToClosestFrame(float position)
        {
            return ClampFrame(Mathf.RoundToInt((position - FieldOffset) / OneFrameWidth));
        }

        public float NormalizedTimeToPosition(float normalizedTime) =>
            FrameToPosition(Mathf.RoundToInt(Mathf.Clamp01(normalizedTime) * Mathf.Max(1, m_MaxFrame - 1)));

        public float PositionToNormalizedTime(float position) =>
            PositionToClosestFrame(position) / (float)Mathf.Max(1, m_MaxFrame - 1);

        public int PositionToFloorFrame(float position)
        {
            return ClampFrame(Mathf.FloorToInt((position - FieldOffset) / OneFrameWidth));
        }

        public int PositionToCeilFrame(float position)
        {
            return ClampFrame(Mathf.CeilToInt((position - FieldOffset) / OneFrameWidth));
        }

        public Rect GetClipRect(Clip clip, float height)
        {
            float left = FrameToPosition(clip.StartFrame);
            return new Rect(left, 0f, FrameToPosition(clip.EndFrame) - left, height);
        }

        public bool HitTest(Rect elementRect, Rect selectionRect)
        {
            return elementRect.Overlaps(selectionRect, true);
        }

        public bool IsMoveValid(Clip target)
        {
            if (!target.IsMixable())
            {
                for (int i = 0; i < target.Track.Clips.Count; i++)
                {
                    Clip clip = target.Track.Clips[i];
                    if (clip != target && clip.EndFrame > target.StartFrame && clip.StartFrame < target.EndFrame)
                        return false;
                }
                return true;
            }

            for (int i = 0; i < target.Track.Clips.Count; i++)
            {
                Clip clip = target.Track.Clips[i];
                if (clip == target)
                    continue;
                if (clip.StartFrame < target.StartFrame && clip.EndFrame > target.EndFrame)
                    return false;
                if (clip.StartFrame > target.StartFrame && clip.EndFrame < target.EndFrame)
                    return false;
            }

            List<int> boundaries = new List<int> { target.StartFrame * 2, target.EndFrame * 2 };
            for (int i = 0; i < target.Track.Clips.Count; i++)
            {
                Clip clip = target.Track.Clips[i];
                if (clip == target)
                    continue;
                if (clip.EndFrame < target.StartFrame || clip.StartFrame > target.EndFrame)
                    continue;
                boundaries.Add(Mathf.Max(target.StartFrame, clip.StartFrame) * 2);
                boundaries.Add(Mathf.Min(target.EndFrame, clip.EndFrame) * 2);
            }
            boundaries.Sort();
            List<float> samples = boundaries.Distinct().Select(value => value * 0.5f).ToList();
            for (int i = 1; i < boundaries.Count; i++)
                samples.Add((boundaries[i - 1] + boundaries[i]) * 0.25f);
            for (int i = 0; i < samples.Count; i++)
            {
                float frame = samples[i];
                int overlapCount = 0;
                for (int clipIndex = 0; clipIndex < target.Track.Clips.Count; clipIndex++)
                {
                    Clip clip = target.Track.Clips[clipIndex];
                    if (clip != target && clip.Contains(frame))
                        overlapCount++;
                }
                if (overlapCount > 1)
                    return false;
            }
            return true;
        }

        public Clip GetClosestLeftClip(Clip target)
        {
            Clip result = null;
            int frame = int.MinValue;
            for (int i = 0; i < target.Track.Clips.Count; i++)
            {
                Clip clip = target.Track.Clips[i];
                if (clip != target && clip.StartFrame < target.StartFrame && clip.StartFrame > frame)
                {
                    frame = clip.StartFrame;
                    result = clip;
                }
            }
            return result;
        }

        public Clip GetClosestRightClip(Clip target)
        {
            return GetClosestRightClip(target.Track, target.StartFrame, target);
        }

        public Clip GetClosestRightClip(Track track, int startFrame, Clip excluded = null)
        {
            Clip result = null;
            int frame = int.MaxValue;
            for (int i = 0; i < track.Clips.Count; i++)
            {
                Clip clip = track.Clips[i];
                if (clip != excluded && clip.StartFrame > startFrame && clip.StartFrame < frame)
                {
                    frame = clip.StartFrame;
                    result = clip;
                }
            }
            return result;
        }

        public Clip GetSameStartOverlap(Clip target)
        {
            for (int i = 0; i < target.Track.Clips.Count; i++)
            {
                Clip clip = target.Track.Clips[i];
                if (clip != target && clip.StartFrame == target.StartFrame)
                    return clip;
            }
            return null;
        }

        public int GetRightEdgeFrame(Track track)
        {
            int frame = 0;
            for (int i = 0; i < track.Clips.Count; i++)
                frame = Mathf.Max(frame, track.Clips[i].EndFrame);
            return frame;
        }

        int ClampFrame(int frame)
        {
            return Mathf.Clamp(frame, 0, Mathf.Max(0, m_MaxFrame - 1));
        }
    }
}
