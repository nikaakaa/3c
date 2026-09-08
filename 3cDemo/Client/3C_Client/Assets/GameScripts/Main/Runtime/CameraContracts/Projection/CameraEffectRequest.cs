using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
    public enum CameraPresentationStopReason : byte
    {
        NaturalComplete = 1,
        Cancel = 2,
        EventRevoked = 3
    }

public readonly struct CameraEffectRequest
    {
        public CameraEffectRequest(
            CameraEffectKind kind,
            string resourceId,
            float weight,
            int priority,
            string sourceId,
            ulong generation,
            string eventId,
            ulong sourceActionInstanceId,
            int cycle = 0,
            float sampleTime = 0f)
        {
            if (cycle < 0 || !float.IsFinite(sampleTime) || sampleTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(sampleTime));
            Kind = kind;
            ResourceId = resourceId ?? string.Empty;
            Weight = Mathf.Max(0f, weight);
            Priority = priority;
            SourceId = sourceId ?? string.Empty;
            Generation = generation;
            EventId = eventId ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
            Cycle = cycle;
            SampleTime = sampleTime;
        }

        public CameraEffectKind Kind { get; }
        public string ResourceId { get; }
        public float Weight { get; }
        public int Priority { get; }
        public string SourceId { get; }
        public ulong Generation { get; }
        public string EventId { get; }
        public ulong SourceActionInstanceId { get; }
        public int Cycle { get; }
        public float SampleTime { get; }
        public CameraPresentationScopeKey Scope => new CameraPresentationScopeKey(
            SourceId,
            Generation,
            SourceActionInstanceId);
        public bool Active => Weight > 0f && !string.IsNullOrWhiteSpace(ResourceId);
    }
}
