using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdPersonCamera
{
public readonly struct CameraSequenceRequest
    {
        public CameraSequenceRequest(
            string sequenceId,
            int priority,
            float weight,
            float blendInSeconds,
            float blendOutSeconds,
            string targetKey,
            string sourceId,
            ulong generation,
            ulong sourceActionInstanceId,
            CameraSequenceInterruptPolicy interruptPolicy,
            bool isDefault = false)
        {
            SequenceId = sequenceId ?? string.Empty;
            Priority = priority;
            Weight = Mathf.Clamp01(weight);
            BlendInSeconds = Mathf.Max(0f, blendInSeconds);
            BlendOutSeconds = Mathf.Max(0f, blendOutSeconds);
            TargetKey = targetKey ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            Generation = generation;
            SourceActionInstanceId = sourceActionInstanceId;
            InterruptPolicy = interruptPolicy;
            IsDefault = isDefault;
        }

        public string SequenceId { get; }
        public int Priority { get; }
        public float Weight { get; }
        public float BlendInSeconds { get; }
        public float BlendOutSeconds { get; }
        public string TargetKey { get; }
        public string SourceId { get; }
        public ulong Generation { get; }
        public ulong SourceActionInstanceId { get; }
        public CameraSequenceInterruptPolicy InterruptPolicy { get; }
        public bool IsDefault { get; }
        public bool Active => Weight > 0f && !string.IsNullOrWhiteSpace(SequenceId);

        public CameraSequenceRequest WithTargetKey(string targetKey) => new CameraSequenceRequest(
            SequenceId,
            Priority,
            Weight,
            BlendInSeconds,
            BlendOutSeconds,
            targetKey,
            SourceId,
            Generation,
            SourceActionInstanceId,
            InterruptPolicy,
            IsDefault);
    }
}
