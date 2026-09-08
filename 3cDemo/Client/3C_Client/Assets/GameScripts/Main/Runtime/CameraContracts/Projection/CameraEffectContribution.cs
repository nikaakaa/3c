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
            bool active)
        {
            Stage = stage;
            ResourceId = resourceId ?? string.Empty;
            Weight = weight;
            RemainingSeconds = remainingSeconds;
            Priority = priority;
            Active = active;
        }

        public CameraEffectStage Stage { get; }
        public string ResourceId { get; }
        public float Weight { get; }
        public float RemainingSeconds { get; }
        public int Priority { get; }
        public bool Active { get; }
    }
}
