using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public readonly struct MotionWarpTarget
    {
        public MotionWarpTarget(Vector3 position)
            : this(position, 0f, false)
        {
        }

        public MotionWarpTarget(Vector3 position, float yawDegrees)
            : this(position, yawDegrees, true)
        {
        }

        MotionWarpTarget(Vector3 position, float yawDegrees, bool hasYaw)
        {
            Position = position;
            YawDegrees = yawDegrees;
            HasYaw = hasYaw;
        }

        public Vector3 Position { get; }
        public float YawDegrees { get; }
        public bool HasYaw { get; }
    }

    public readonly struct MotionWarpWindow
    {
        public MotionWarpWindow(
            string sourceId,
            string sourceName,
            ulong actionInstanceId,
            string targetKey,
            float normalizedTime,
            float weight,
            float positionWeight,
            float yawWeight,
            float maxPositionCorrection,
            float maxYawCorrectionDegrees,
            string debugSourceIdentity)
        {
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            ActionInstanceId = actionInstanceId;
            TargetKey = targetKey ?? string.Empty;
            NormalizedTime = Mathf.Clamp01(normalizedTime);
            Weight = Mathf.Clamp01(weight);
            PositionWeight = Mathf.Clamp01(positionWeight);
            YawWeight = Mathf.Clamp01(yawWeight);
            MaxPositionCorrection = Mathf.Max(0f, maxPositionCorrection);
            MaxYawCorrectionDegrees = Mathf.Max(0f, maxYawCorrectionDegrees);
            DebugSourceIdentity = debugSourceIdentity ?? string.Empty;
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public ulong ActionInstanceId { get; }
        public string TargetKey { get; }
        public float NormalizedTime { get; }
        public float Weight { get; }
        public float PositionWeight { get; }
        public float YawWeight { get; }
        public float MaxPositionCorrection { get; }
        public float MaxYawCorrectionDegrees { get; }
        public string DebugSourceIdentity { get; }
        public bool HasPositionCorrection => Weight > 0f && PositionWeight > 0f && MaxPositionCorrection > 0f;
        public bool HasYawCorrection => Weight > 0f && YawWeight > 0f && MaxYawCorrectionDegrees > 0f;
    }
}
