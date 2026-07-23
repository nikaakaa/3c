using System;
using BTSMTL.Timeline;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline
{
    internal sealed class PreviewSession : IDisposable
    {
        public PreviewSession(
            ulong generation,
            PreviewPlaybackEngine engine)
        {
            Generation = generation;
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
        }

        public PreviewPlaybackEngine Engine { get; }
        public ulong Generation { get; set; }
        public TimelineData Timeline { get; private set; }
        public float PreviousTime { get; private set; }
        public float CurrentTime { get; private set; }
        public string SourceId { get; private set; }
        public string SourceName { get; private set; }
        public ulong EvaluationTick { get; private set; }
        public float PresentationDeltaSeconds { get; private set; }
        public string TargetTrackAuthoringId { get; private set; }
        public bool HasBlendSpaceParameter { get; private set; }
        public Vector2 BlendSpaceParameter { get; private set; }
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
            PresentationDeltaSeconds = Mathf.Max(0f, presentationDeltaSeconds);
            TargetTrackAuthoringId = string.Empty;
            HasBlendSpaceParameter = false;
            BlendSpaceParameter = default;
        }

        public void CaptureBlendSpace(
            TimelineData timeline,
            string targetTrackAuthoringId,
            float previousTime,
            float currentTime,
            string sourceId,
            string sourceName,
            ulong evaluationTick,
            float presentationDeltaSeconds,
            Vector2 parameter)
        {
            if (string.IsNullOrWhiteSpace(targetTrackAuthoringId) ||
                !float.IsFinite(parameter.x) || !float.IsFinite(parameter.y))
                throw new ArgumentException("Blend Space preview input is invalid.");
            Capture(
                timeline,
                previousTime,
                currentTime,
                sourceId,
                sourceName,
                evaluationTick,
                presentationDeltaSeconds);
            TargetTrackAuthoringId = targetTrackAuthoringId.Trim();
            HasBlendSpaceParameter = true;
            BlendSpaceParameter = parameter;
        }

        public void Dispose()
        {
            Engine.Dispose();
        }
    }
}
