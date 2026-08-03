using System;
using BTSMTL.Timeline;

namespace ThirdPersonCharacter.Pipeline
{
    internal sealed class PreviewSession : IDisposable
    {
        public PreviewSession(
            ulong generation,
            AnimationPreviewRuntime engine)
        {
            Generation = generation;
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public AnimationPreviewRuntime Engine { get; }
        public ulong Generation { get; set; }
        public TimelineData Timeline { get; private set; }
        public float PreviousTime { get; private set; }
        public float CurrentTime { get; private set; }
        public string SourceId { get; private set; }
        public string SourceName { get; private set; }
        public ulong EvaluationTick { get; private set; }
        public float PresentationDeltaSeconds { get; private set; }
        public bool HasEvaluation => Timeline != null && EvaluationTick != 0;

        public void Capture(
            TimelineData timeline,
            float previousTime,
            float currentTime,
            string sourceId,
            string sourceName,
            ulong evaluationTick,
            float presentationDeltaSeconds)
        {
            Timeline = timeline;
            PreviousTime = previousTime;
            CurrentTime = currentTime;
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            EvaluationTick = evaluationTick;
            PresentationDeltaSeconds = Math.Max(0f, presentationDeltaSeconds);
        }

        public void Dispose()
        {
            Engine.Dispose();
        }
    }
}
