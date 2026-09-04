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
            ulong generation)
        {
            TargetKey = targetKey ?? string.Empty;
            AnchorKey = anchorKey ?? string.Empty;
            AimPointKey = aimPointKey ?? string.Empty;
            PreferredBoneKey = preferredBoneKey ?? string.Empty;
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            SourceId = sourceId ?? string.Empty;
            Generation = generation;
        }

        public string TargetKey { get; }
        public string AnchorKey { get; }
        public string AimPointKey { get; }
        public string PreferredBoneKey { get; }
        public int Priority { get; }
        public float Weight { get; }
        public string SourceId { get; }
        public ulong Generation { get; }
        public bool Active => Weight > 0f && HasAnyKey;
        public bool HasAnyKey => !string.IsNullOrWhiteSpace(TargetKey) ||
                                 !string.IsNullOrWhiteSpace(AnchorKey) ||
                                 !string.IsNullOrWhiteSpace(AimPointKey) ||
                                 !string.IsNullOrWhiteSpace(PreferredBoneKey);
    }
}
