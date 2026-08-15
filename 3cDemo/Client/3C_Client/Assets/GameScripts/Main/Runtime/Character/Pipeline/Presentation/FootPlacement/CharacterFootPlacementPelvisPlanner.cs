using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterFootPlacementPelvisLegInput
    {
        internal CharacterFootPlacementPelvisLegInput(
            CharacterFootSide side,
            Vector3 hipPosition,
            Vector3 targetAnklePosition,
            float goalWeight,
            float supportWeight,
            float legLength,
            float authoredSupportLegLength,
            float supportLegCompressionReserve,
            Vector3 supportKneeBendPlane)
        {
            Side = side;
            HipPosition = hipPosition;
            TargetAnklePosition = targetAnklePosition;
            GoalWeight = goalWeight;
            SupportWeight = supportWeight;
            LegLength = legLength;
            AuthoredSupportLegLength = authoredSupportLegLength;
            SupportLegCompressionReserve = supportLegCompressionReserve;
            SupportKneeBendPlane = supportKneeBendPlane;
        }

        internal CharacterFootSide Side { get; }
        internal Vector3 HipPosition { get; }
        internal Vector3 TargetAnklePosition { get; }
        internal float GoalWeight { get; }
        internal float SupportWeight { get; }
        internal float LegLength { get; }
        internal float AuthoredSupportLegLength { get; }
        internal float SupportLegCompressionReserve { get; }
        internal Vector3 SupportKneeBendPlane { get; }
    }

    public readonly struct CharacterFootPlacementPelvisLegRange
    {
        internal CharacterFootPlacementPelvisLegRange(
            CharacterFootSide side,
            float minimumOffset,
            float maximumOffset,
            float supportWeight,
            bool contributes)
        {
            Side = side;
            MinimumOffset = minimumOffset;
            MaximumOffset = maximumOffset;
            SupportWeight = supportWeight;
            Contributes = contributes;
        }

        public CharacterFootSide Side { get; }
        public float MinimumOffset { get; }
        public float MaximumOffset { get; }
        public float SupportWeight { get; }
        public bool Contributes { get; }
        public bool IsValid =>
            Side != 0 &&
            float.IsFinite(MinimumOffset) &&
            float.IsFinite(MaximumOffset) &&
            MinimumOffset <= MaximumOffset;
    }

    public readonly struct CharacterFootPlacementPelvisPlan
    {
        internal CharacterFootPlacementPelvisPlan(
            float lyraTargetOffset,
            float lyraCurrentOffset,
            float resolvedOffset,
            CharacterFootPlacementPelvisLegRange leftRange,
            CharacterFootPlacementPelvisLegRange rightRange,
            bool rejectLeftGoal,
            bool rejectRightGoal)
        {
            LyraTargetOffset = lyraTargetOffset;
            LyraCurrentOffset = lyraCurrentOffset;
            ResolvedOffset = resolvedOffset;
            LeftRange = leftRange;
            RightRange = rightRange;
            RejectLeftGoal = rejectLeftGoal;
            RejectRightGoal = rejectRightGoal;
        }

        public float LyraTargetOffset { get; }
        public float LyraCurrentOffset { get; }
        public float ResolvedOffset { get; }
        public CharacterFootPlacementPelvisLegRange LeftRange { get; }
        public CharacterFootPlacementPelvisLegRange RightRange { get; }
        public bool RejectLeftGoal { get; }
        public bool RejectRightGoal { get; }
        public Vector3 ComponentTranslation => Vector3.up * ResolvedOffset;
    }

    internal sealed class CharacterFootPlacementPelvisPlanner
    {
        const float Epsilon = 0.0001f;

        internal CharacterFootPlacementPelvisPlan Plan(
            float lyraTargetOffset,
            float lyraCurrentOffset,
            in CharacterFootPlacementPelvisLegInput left,
            in CharacterFootPlacementPelvisLegInput right,
            Vector3 componentUp,
            CharacterStanceStabilizationSettings settings)
        {
            if (!float.IsFinite(lyraTargetOffset) ||
                !float.IsFinite(lyraCurrentOffset) ||
                componentUp.sqrMagnitude <= Epsilon)
                throw new ArgumentException("Foot Placement pelvis input is invalid.");
            Vector3 up = componentUp.normalized;
            CharacterFootPlacementPelvisLegRange leftRange = BuildRange(in left, up, settings);
            CharacterFootPlacementPelvisLegRange rightRange = BuildRange(in right, up, settings);
            bool useLeft = left.GoalWeight > Epsilon && left.SupportWeight > Epsilon;
            bool useRight = right.GoalWeight > Epsilon && right.SupportWeight > Epsilon;
            bool leftCurrentReachable = useLeft && Contains(leftRange, lyraCurrentOffset, settings);
            bool rightCurrentReachable = useRight && Contains(rightRange, lyraCurrentOffset, settings);
            bool leftTargetReachable = useLeft && Contains(leftRange, lyraTargetOffset, settings);
            bool rightTargetReachable = useRight && Contains(rightRange, lyraTargetOffset, settings);
            bool rejectLeftGoal = useLeft && !leftCurrentReachable && !leftTargetReachable;
            bool rejectRightGoal = useRight && !rightCurrentReachable && !rightTargetReachable;
            bool contributeLeft = useLeft && !rejectLeftGoal;
            bool contributeRight = useRight && !rejectRightGoal;
            if (contributeLeft && contributeRight && !Intersects(leftRange, rightRange))
            {
                bool keepLeft = SelectLeftSupport(
                    left,
                    right,
                    leftRange,
                    rightRange,
                    lyraCurrentOffset);
                rejectLeftGoal = !keepLeft;
                rejectRightGoal = keepLeft;
                contributeLeft = keepLeft;
                contributeRight = !keepLeft;
            }
            leftRange = SetContribution(leftRange, contributeLeft);
            rightRange = SetContribution(rightRange, contributeRight);
            float minimum = -settings.MaximumPelvisLowering;
            float maximum = settings.MaximumPelvisRaising;
            if (contributeLeft)
            {
                minimum = Mathf.Max(minimum, leftRange.MinimumOffset);
                maximum = Mathf.Min(maximum, leftRange.MaximumOffset);
            }
            if (contributeRight)
            {
                minimum = Mathf.Max(minimum, rightRange.MinimumOffset);
                maximum = Mathf.Min(maximum, rightRange.MaximumOffset);
            }
            float resolvedOffset = contributeLeft || contributeRight
                ? Mathf.Clamp(lyraCurrentOffset, minimum, maximum)
                : lyraCurrentOffset;
            return new CharacterFootPlacementPelvisPlan(
                lyraTargetOffset,
                lyraCurrentOffset,
                resolvedOffset,
                leftRange,
                rightRange,
                rejectLeftGoal,
                rejectRightGoal);
        }

        static CharacterFootPlacementPelvisLegRange BuildRange(
            in CharacterFootPlacementPelvisLegInput input,
            Vector3 up,
            CharacterStanceStabilizationSettings settings)
        {
            if (!IsFinite(input.HipPosition) ||
                !IsFinite(input.TargetAnklePosition) ||
                !float.IsFinite(input.LegLength) || input.LegLength <= Epsilon)
            {
                return new CharacterFootPlacementPelvisLegRange(
                    input.Side,
                    1f,
                    -1f,
                    input.SupportWeight,
                    false);
            }
            Vector3 hipToTarget = input.TargetAnklePosition - input.HipPosition;
            float vertical = Vector3.Dot(hipToTarget, up);
            float horizontalSquared = Vector3.ProjectOnPlane(hipToTarget, up).sqrMagnitude;
            float compressionLimitedLength = input.SupportLegCompressionReserve > Epsilon
                ? input.LegLength - input.SupportLegCompressionReserve
                : input.AuthoredSupportLegLength > Epsilon
                    ? input.AuthoredSupportLegLength
                    : input.LegLength;
            float maximumLength = Mathf.Min(input.LegLength, compressionLimitedLength) + Epsilon;
            float maximumVerticalSquared = maximumLength * maximumLength - horizontalSquared;
            if (maximumVerticalSquared < 0f)
            {
                return new CharacterFootPlacementPelvisLegRange(
                    input.Side,
                    1f,
                    -1f,
                    input.SupportWeight,
                    false);
            }
            float maximumVertical = Mathf.Sqrt(maximumVerticalSquared);
            float minimum = vertical - maximumVertical;
            float maximum = vertical + maximumVertical;
            minimum = Mathf.Max(minimum, -settings.MaximumPelvisLowering);
            maximum = Mathf.Min(maximum, settings.MaximumPelvisRaising);
            if (!float.IsFinite(minimum) || !float.IsFinite(maximum) || minimum > maximum)
            {
                return new CharacterFootPlacementPelvisLegRange(
                    input.Side,
                    1f,
                    -1f,
                    input.SupportWeight,
                    false);
            }
            return new CharacterFootPlacementPelvisLegRange(
                input.Side,
                minimum,
                maximum,
                input.SupportWeight,
                false);
        }

        static bool Contains(
            CharacterFootPlacementPelvisLegRange range,
            float offset,
            CharacterStanceStabilizationSettings settings) =>
            range.IsValid &&
            offset >= Mathf.Max(range.MinimumOffset, -settings.MaximumPelvisLowering) - Epsilon &&
            offset <= Mathf.Min(range.MaximumOffset, settings.MaximumPelvisRaising) + Epsilon;

        static bool Intersects(
            CharacterFootPlacementPelvisLegRange left,
            CharacterFootPlacementPelvisLegRange right) =>
            left.IsValid && right.IsValid &&
            Mathf.Max(left.MinimumOffset, right.MinimumOffset) <=
            Mathf.Min(left.MaximumOffset, right.MaximumOffset) + Epsilon;

        static bool SelectLeftSupport(
            in CharacterFootPlacementPelvisLegInput left,
            in CharacterFootPlacementPelvisLegInput right,
            CharacterFootPlacementPelvisLegRange leftRange,
            CharacterFootPlacementPelvisLegRange rightRange,
            float currentOffset)
        {
            if (Mathf.Abs(left.SupportWeight - right.SupportWeight) > Epsilon)
                return left.SupportWeight > right.SupportWeight;
            float leftDistance = DistanceToRange(leftRange, currentOffset);
            float rightDistance = DistanceToRange(rightRange, currentOffset);
            return leftDistance <= rightDistance;
        }

        static float DistanceToRange(CharacterFootPlacementPelvisLegRange range, float value)
        {
            if (!range.IsValid)
                return float.PositiveInfinity;
            if (value < range.MinimumOffset)
                return range.MinimumOffset - value;
            return value > range.MaximumOffset ? value - range.MaximumOffset : 0f;
        }

        static CharacterFootPlacementPelvisLegRange SetContribution(
            CharacterFootPlacementPelvisLegRange range,
            bool contributes) =>
            new CharacterFootPlacementPelvisLegRange(
                range.Side,
                range.MinimumOffset,
                range.MaximumOffset,
                range.SupportWeight,
                contributes);

        internal void Reset()
        {
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
