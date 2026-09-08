using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
public readonly struct CameraResponseRequest
    {
        public CameraResponseRequest(
            CameraResponseMode mode,
            float manualOrbitWeight,
            float pitchWeight,
            float yawWeight,
            int priority,
            float weight,
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId,
            string eventId = "",
            int cycle = 0,
            float sampleTime = 0f)
        {
            if (cycle < 0 || !float.IsFinite(sampleTime) || sampleTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(sampleTime));
            Mode = mode;
            ManualOrbitWeight = Mathf.Clamp01(manualOrbitWeight);
            PitchWeight = Mathf.Clamp01(pitchWeight);
            YawWeight = Mathf.Clamp01(yawWeight);
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            SourceId = sourceId ?? string.Empty;
            Generation = generation;
            SourceActionInstanceId = sourceActionInstanceId;
            EventId = eventId ?? string.Empty;
            Cycle = cycle;
            SampleTime = sampleTime;
        }

        public CameraResponseMode Mode { get; }
        public float ManualOrbitWeight { get; }
        public float PitchWeight { get; }
        public float YawWeight { get; }
        public int Priority { get; }
        public float Weight { get; }
        public string SourceId { get; }
        public ulong Generation { get; }
        public ulong SourceActionInstanceId { get; }
        public string EventId { get; }
        public int Cycle { get; }
        public float SampleTime { get; }
        public bool Active => Weight > 0f;

        public Vector2 Apply(Vector2 lookInput)
        {
            switch (Mode)
            {
                case CameraResponseMode.Suppressed:
                    return Vector2.zero;
                case CameraResponseMode.Weighted:
                    return new Vector2(
                        lookInput.x * ManualOrbitWeight * YawWeight * Weight,
                        lookInput.y * ManualOrbitWeight * PitchWeight * Weight);
                case CameraResponseMode.Full:
                    return lookInput * Weight;
                default:
                    throw new InvalidOperationException($"Camera Response mode '{Mode}' is unsupported.");
            }
        }
    }
}
