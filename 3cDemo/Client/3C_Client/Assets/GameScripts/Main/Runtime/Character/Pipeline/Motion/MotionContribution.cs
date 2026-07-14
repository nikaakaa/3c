using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Motion
{
    public enum MotionContributionSpace
    {
        World,
        Local
    }

    public enum MotionChannel
    {
        Locomotion,
        Action,
        GameplayResult
    }

    public enum MotionBlendMode
    {
        Additive,
        WeightedBlend,
        Override
    }

    public enum MotionContributionSourceType
    {
        Unknown,
        Input,
        RootMotion,
        GameplayResult,
        MotionCurve = 5
    }

    public readonly struct MotionContribution
    {
        public MotionContribution(
            string sourceId,
            string sourceName,
            Vector3 displacement,
            float yawDegrees,
            MotionContributionSpace space,
            float weight,
            int priority,
            MotionChannel channel,
            MotionBlendMode blendMode,
            MotionContributionSourceType sourceType,
            bool consumeLowerChannels,
            string debugSourceIdentity,
            bool faceMovementDirection = false,
            float maxYawDegrees = 0f)
        {
            SourceId = sourceId ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            Displacement = displacement;
            YawDegrees = yawDegrees;
            Space = space;
            Weight = Mathf.Clamp01(weight);
            Priority = priority;
            Channel = channel;
            BlendMode = blendMode;
            SourceType = sourceType;
            ConsumeLowerChannels = consumeLowerChannels;
            DebugSourceIdentity = debugSourceIdentity ?? string.Empty;
            FaceMovementDirection = faceMovementDirection;
            MaxYawDegrees = Mathf.Max(0f, maxYawDegrees);
        }

        public string SourceId { get; }
        public string SourceName { get; }
        public Vector3 Displacement { get; }
        public float YawDegrees { get; }
        public MotionContributionSpace Space { get; }
        public float Weight { get; }
        public int Priority { get; }
        public MotionChannel Channel { get; }
        public MotionBlendMode BlendMode { get; }
        public MotionContributionSourceType SourceType { get; }
        public bool ConsumeLowerChannels { get; }
        public string DebugSourceIdentity { get; }
        public bool FaceMovementDirection { get; }
        public float MaxYawDegrees { get; }
        public bool HasDelta => Weight > 0f && (Displacement.sqrMagnitude > 0.0000001f || Mathf.Abs(YawDegrees) > 0.0001f);
        public bool ClaimsLowerChannels => Weight > 0f && BlendMode == MotionBlendMode.Override && ConsumeLowerChannels;
        public bool CanResolve => HasDelta || ClaimsLowerChannels;

        public static MotionContribution InputLocomotion(string sourceId, string sourceName, Vector3 worldDisplacement, float maxYawDegrees, float weight)
        {
            return new MotionContribution(
                sourceId,
                sourceName,
                worldDisplacement,
                0f,
                MotionContributionSpace.World,
                weight,
                0,
                MotionChannel.Locomotion,
                MotionBlendMode.Override,
                MotionContributionSourceType.Input,
                false,
                sourceId,
                true,
                maxYawDegrees);
        }

        public static MotionContribution ActionRootMotion(string sourceId, string sourceName, Vector3 localDisplacement, float yawDegrees, float weight, int priority, string debugSourceIdentity)
        {
            return new MotionContribution(
                sourceId,
                sourceName,
                localDisplacement,
                yawDegrees,
                MotionContributionSpace.Local,
                weight,
                priority,
                MotionChannel.Action,
                MotionBlendMode.Override,
                MotionContributionSourceType.RootMotion,
                true,
                debugSourceIdentity);
        }

        public static MotionContribution TimelineMotionCurve(
            string sourceId,
            string sourceName,
            Vector3 displacement,
            float yawDegrees,
            MotionContributionSpace space,
            float weight,
            int priority,
            MotionChannel channel,
            MotionBlendMode blendMode,
            bool consumeLowerChannels,
            string debugSourceIdentity)
        {
            return new MotionContribution(
                sourceId,
                sourceName,
                displacement,
                yawDegrees,
                space,
                weight,
                priority,
                channel,
                blendMode,
                MotionContributionSourceType.MotionCurve,
                consumeLowerChannels,
                debugSourceIdentity);
        }

        public static MotionContribution GameplayResult(string sourceId, string sourceName, Vector3 worldDisplacement, float yawDegrees, float weight, int priority, bool consumeLowerChannels)
        {
            return new MotionContribution(
                sourceId,
                sourceName,
                worldDisplacement,
                yawDegrees,
                MotionContributionSpace.World,
                weight,
                priority,
                MotionChannel.GameplayResult,
                MotionBlendMode.Override,
                MotionContributionSourceType.GameplayResult,
                consumeLowerChannels,
                sourceId);
        }

    }
}
