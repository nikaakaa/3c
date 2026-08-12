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
            float legLength)
        {
            Side = side;
            HipPosition = hipPosition;
            TargetAnklePosition = targetAnklePosition;
            GoalWeight = goalWeight;
            SupportWeight = supportWeight;
            LegLength = legLength;
        }

        internal CharacterFootSide Side { get; }
        internal Vector3 HipPosition { get; }
        internal Vector3 TargetAnklePosition { get; }
        internal float GoalWeight { get; }
        internal float SupportWeight { get; }
        internal float LegLength { get; }
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
            bool leftReachable = !useLeft || Contains(leftRange, lyraCurrentOffset, settings);
            bool rightReachable = !useRight || Contains(rightRange, lyraCurrentOffset, settings);
            bool rejectLeftGoal = useLeft && !leftReachable;
            bool rejectRightGoal = useRight && !rightReachable;
            leftRange = SetContribution(leftRange, useLeft && leftReachable);
            rightRange = SetContribution(rightRange, useRight && rightReachable);
            return new CharacterFootPlacementPelvisPlan(
                lyraTargetOffset,
                lyraCurrentOffset,
                lyraCurrentOffset,
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
            float maximumLength = input.LegLength * settings.MaximumLegExtensionRatio;
            float minimumLength = input.LegLength * settings.MinimumLegExtensionRatio;
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
            float minimumVerticalSquared = minimumLength * minimumLength - horizontalSquared;
            if (minimumVerticalSquared > 0f)
                minimum = Mathf.Max(minimum, vertical + Mathf.Sqrt(minimumVerticalSquared));
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
