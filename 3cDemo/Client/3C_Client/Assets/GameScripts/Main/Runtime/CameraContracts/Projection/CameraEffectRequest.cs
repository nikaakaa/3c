using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
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
            ulong sourceActionInstanceId)
        {
            Kind = kind;
            ResourceId = resourceId ?? string.Empty;
            Weight = Mathf.Max(0f, weight);
            Priority = priority;
            SourceId = sourceId ?? string.Empty;
            Generation = generation;
            EventId = eventId ?? string.Empty;
            SourceActionInstanceId = sourceActionInstanceId;
        }

        public CameraEffectKind Kind { get; }
        public string ResourceId { get; }
        public float Weight { get; }
        public int Priority { get; }
        public string SourceId { get; }
        public ulong Generation { get; }
        public string EventId { get; }
        public ulong SourceActionInstanceId { get; }
        public bool Active => Weight > 0f && !string.IsNullOrWhiteSpace(ResourceId);
    }
}
