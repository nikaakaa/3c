using ThirdPersonSimulation;
using KK.GeneratedDiagnosticSampling;
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

        [DiagnosticField, DiagnosticGroup("camera-effects")]
        public CameraEffectStage Stage { get; }
        [DiagnosticField, DiagnosticGroup("camera-effects")]
        public string ResourceId { get; }
        [DiagnosticField, DiagnosticGroup("camera-effects")]
        public float Weight { get; }
        [DiagnosticField, DiagnosticGroup("camera-effects")]
        [DiagnosticAvailability(DiagnosticAvailabilityReference.Member, nameof(HasFiniteRemainingSeconds))]
        public float RemainingSeconds { get; }
        [DiagnosticField, DiagnosticGroup("camera-effects")]
        public bool HasFiniteRemainingSeconds => float.IsFinite(RemainingSeconds);
        [DiagnosticField, DiagnosticGroup("camera-effects")]
        public int Priority { get; }
        [DiagnosticField, DiagnosticGroup("camera-effects")]
        public bool Active { get; }
        [DiagnosticField, DiagnosticGroup("camera-effects")]
        public string SourceId { get; }
        [DiagnosticField, DiagnosticGroup("camera-effects")]
        public ulong Generation { get; }
        [DiagnosticField, DiagnosticGroup("camera-effects")]
        public ulong SourceActionInstanceId { get; }
        [DiagnosticField, DiagnosticGroup("camera-effects")]
        public int Cycle { get; }
        public EventId EventId { get; }
        [DiagnosticField, DiagnosticGroup("camera-effects")]
        public CameraPresentationStopReason StopReason { get; }
    }
}
