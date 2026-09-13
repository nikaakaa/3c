using System;
using UnityEngine;

namespace BTSMTL.Timeline
{
    public abstract class TimelinePreviewTarget : MonoBehaviour
    {
        public abstract bool CanPreviewTimeline { get; }
        public abstract string PreviewStatus { get; }
        public abstract void EvaluateTimelinePreview(
            Guid sessionId,
            TimelineData timeline,
            float previousTime,
            float currentTime,
            string sourceId,
            string sourceName,
            ulong evaluationTick,
            float presentationDeltaSeconds,
            bool resetLifecycle);
        public abstract void ClearTimelinePreview(Guid sessionId);
    }
}
