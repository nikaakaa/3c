using ThirdPersonSimulation;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
public readonly struct CameraEffectContribution
    {
        public CameraEffectContribution(
            CameraEffectStage stage,
            string resourceId,
            float weight,
            float remainingSeconds,
            int priority,
            bool active,
            string sourceId = "",
            ulong generation = 0,
            ulong sourceActionInstanceId = 0,
            int cycle = 0,
            EventId eventId = default,
            CameraPresentationStopReason stopReason = CameraPresentationStopReason.NaturalComplete)
        {
            Stage = stage;
            ResourceId = resourceId ?? string.Empty;
            Weight = weight;
            RemainingSeconds = remainingSeconds;
            Priority = priority;
            Active = active;
            SourceId = sourceId ?? string.Empty;
            Generation = generation;
            SourceActionInstanceId = sourceActionInstanceId;
            Cycle = cycle;
            EventId = eventId;
            StopReason = stopReason;
        }

        public CameraEffectStage Stage { get; }
        public string ResourceId { get; }
        public float Weight { get; }
        public float RemainingSeconds { get; }
        public int Priority { get; }
        public bool Active { get; }
        public string SourceId { get; }
        public ulong Generation { get; }
        public ulong SourceActionInstanceId { get; }
        public int Cycle { get; }
        public EventId EventId { get; }
        public CameraPresentationStopReason StopReason { get; }
    }
}
