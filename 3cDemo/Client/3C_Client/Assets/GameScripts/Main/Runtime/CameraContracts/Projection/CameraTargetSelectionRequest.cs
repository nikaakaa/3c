using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
public readonly struct CameraTargetSelectionRequest
    {
        public CameraTargetSelectionRequest(
            string targetKey,
            string anchorKey,
            string aimPointKey,
            string preferredBoneKey,
            int priority,
            float weight,
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId = 0,
            int cycle = 0,
            string eventId = "")
        {
            if (cycle < 0)
                throw new ArgumentOutOfRangeException(nameof(cycle));
            TargetKey = targetKey ?? string.Empty;
            AnchorKey = anchorKey ?? string.Empty;
            AimPointKey = aimPointKey ?? string.Empty;
            PreferredBoneKey = preferredBoneKey ?? string.Empty;
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            SourceId = sourceId ?? string.Empty;
            Generation = generation;
            SourceActionInstanceId = sourceActionInstanceId;
            Cycle = cycle;
            EventId = eventId ?? string.Empty;
        }

        public string TargetKey { get; }
        public string AnchorKey { get; }
        public string AimPointKey { get; }
        public string PreferredBoneKey { get; }
        public int Priority { get; }
        public float Weight { get; }
        public string SourceId { get; }
        public ulong Generation { get; }
        public ulong SourceActionInstanceId { get; }
        public int Cycle { get; }
        public string EventId { get; }
        public bool Active => Weight > 0f && HasAnyKey;
        public bool HasAnyKey => !string.IsNullOrWhiteSpace(TargetKey) ||
                                 !string.IsNullOrWhiteSpace(AnchorKey) ||
                                 !string.IsNullOrWhiteSpace(AimPointKey) ||
                                 !string.IsNullOrWhiteSpace(PreferredBoneKey);

        public bool UsesKey(string key) =>
            !string.IsNullOrEmpty(key) &&
            (string.Equals(TargetKey, key, StringComparison.Ordinal) ||
             string.Equals(AnchorKey, key, StringComparison.Ordinal) ||
             string.Equals(AimPointKey, key, StringComparison.Ordinal) ||
             string.Equals(PreferredBoneKey, key, StringComparison.Ordinal));
    }
}
