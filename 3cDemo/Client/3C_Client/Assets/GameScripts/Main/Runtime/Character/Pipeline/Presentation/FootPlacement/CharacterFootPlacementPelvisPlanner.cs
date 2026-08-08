using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal sealed class CharacterFootPlacementPelvisReachException : InvalidOperationException
    {
        internal CharacterFootPlacementPelvisReachException(
            string reason,
            float lyraTargetOffset,
            float lyraCurrentOffset,
            float globalMinimum,
            float globalMaximum,
            in CharacterFootPlacementPelvisLegInput left,
            CharacterFootPlacementPelvisLegRange leftRange,
            in CharacterFootPlacementPelvisLegInput right,
            CharacterFootPlacementPelvisLegRange rightRange,
            float intersectionMinimum,
            float intersectionMaximum)
            : base(BuildMessage(
                reason,
                lyraTargetOffset,
                lyraCurrentOffset,
                globalMinimum,
                globalMaximum,
                in left,
                leftRange,
                in right,
                rightRange,
                intersectionMinimum,
                intersectionMaximum))
        {
        }

        static string BuildMessage(
            string reason,
            float lyraTargetOffset,
            float lyraCurrentOffset,
            float globalMinimum,
            float globalMaximum,
            in CharacterFootPlacementPelvisLegInput left,
            CharacterFootPlacementPelvisLegRange leftRange,
            in CharacterFootPlacementPelvisLegInput right,
            CharacterFootPlacementPelvisLegRange rightRange,
            float intersectionMinimum,
            float intersectionMaximum) =>
            string.Concat(
                reason,
                " ",
                FormattableString.Invariant(
                    $"LyraTarget={lyraTargetOffset:0.######}, LyraCurrent={lyraCurrentOffset:0.######}, Global=[{globalMinimum:0.######},{globalMaximum:0.######}], Intersection=[{intersectionMinimum:0.######},{intersectionMaximum:0.######}], "),
                FormattableString.Invariant(
                    $"Left(Hip={Format(left.HipPosition)}, Goal={Format(left.TargetAnklePosition)}, Weight={left.GoalWeight:0.######}, Leg={left.LegLength:0.######}, Range=[{leftRange.MinimumOffset:0.######},{leftRange.MaximumOffset:0.######}], Valid={leftRange.IsValid}), "),
                FormattableString.Invariant(
                    $"Right(Hip={Format(right.HipPosition)}, Goal={Format(right.TargetAnklePosition)}, Weight={right.GoalWeight:0.######}, Leg={right.LegLength:0.######}, Range=[{rightRange.MinimumOffset:0.######},{rightRange.MaximumOffset:0.######}], Valid={rightRange.IsValid})."));

        static string Format(Vector3 value) =>
            FormattableString.Invariant($"({value.x:0.######},{value.y:0.######},{value.z:0.######})");
    }

    internal readonly struct CharacterFootPlacementPelvisLegInput
    {
        internal CharacterFootPlacementPelvisLegInput(
            CharacterFootSide side,
            Vector3 hipPosition,
            Vector3 targetAnklePosition,
            float goalWeight,
            float legLength)
        {
            Side = side;
            HipPosition = hipPosition;
            TargetAnklePosition = targetAnklePosition;
            GoalWeight = goalWeight;
            LegLength = legLength;
        }

        internal CharacterFootSide Side { get; }
        internal Vector3 HipPosition { get; }
        internal Vector3 TargetAnklePosition { get; }
        internal float GoalWeight { get; }
        internal float LegLength { get; }
    }

    public readonly struct CharacterFootPlacementPelvisLegRange
    {
        internal CharacterFootPlacementPelvisLegRange(
            CharacterFootSide side,
            float minimumOffset,
            float maximumOffset,
            bool contributes)
        {
            Side = side;
            MinimumOffset = minimumOffset;
            MaximumOffset = maximumOffset;
            Contributes = contributes;
        }

        public CharacterFootSide Side { get; }
        public float MinimumOffset { get; }
        public float MaximumOffset { get; }
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
            CharacterFootPlacementPelvisLegRange rightRange)
        {
            LyraTargetOffset = lyraTargetOffset;
            LyraCurrentOffset = lyraCurrentOffset;
            ResolvedOffset = resolvedOffset;
            LeftRange = leftRange;
            RightRange = rightRange;
        }

        public float LyraTargetOffset { get; }
        public float LyraCurrentOffset { get; }
        public float ResolvedOffset { get; }
        public CharacterFootPlacementPelvisLegRange LeftRange { get; }
        public CharacterFootPlacementPelvisLegRange RightRange { get; }
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
            bool useLeft = left.GoalWeight > Epsilon;
            bool useRight = right.GoalWeight > Epsilon;
            if (useLeft && !leftRange.IsValid || useRight && !rightRange.IsValid)
            {
                throw new CharacterFootPlacementPelvisReachException(
                    "Foot Placement final Foot Goal has no valid pelvis reach interval.",
                    lyraTargetOffset,
                    lyraCurrentOffset,
                    -settings.MaximumPelvisLowering,
                    settings.MaximumPelvisRaising,
                    in left,
                    leftRange,
                    in right,
                    rightRange,
                    1f,
                    -1f);
            }

            float minimum = -settings.MaximumPelvisLowering;
            float maximum = settings.MaximumPelvisRaising;
            Intersect(leftRange, useLeft, ref minimum, ref maximum);
            Intersect(rightRange, useRight, ref minimum, ref maximum);
            if (minimum > maximum)
            {
                throw new CharacterFootPlacementPelvisReachException(
                    "Foot Placement final Foot Goals have no common pelvis reach interval.",
                    lyraTargetOffset,
                    lyraCurrentOffset,
                    -settings.MaximumPelvisLowering,
                    settings.MaximumPelvisRaising,
                    in left,
                    leftRange,
                    in right,
                    rightRange,
                    minimum,
                    maximum);
            }
            leftRange = SetContribution(leftRange, useLeft);
            rightRange = SetContribution(rightRange, useRight);
            return new CharacterFootPlacementPelvisPlan(
                lyraTargetOffset,
                lyraCurrentOffset,
                Mathf.Clamp(lyraCurrentOffset, minimum, maximum),
                leftRange,
                rightRange);
        }

        internal bool HasReachableOffset(
            in CharacterFootPlacementPelvisLegInput input,
            Vector3 componentUp,
            CharacterStanceStabilizationSettings settings)
        {
            if (componentUp.sqrMagnitude <= Epsilon)
                return false;
            CharacterFootPlacementPelvisLegRange range = BuildRange(
                in input,
                componentUp.normalized,
                settings);
            if (!range.IsValid)
                return false;
            float minimum = -settings.MaximumPelvisLowering;
            float maximum = settings.MaximumPelvisRaising;
            Intersect(range, true, ref minimum, ref maximum);
            return minimum <= maximum;
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
                    false);
            }
            float maximumVertical = Mathf.Sqrt(maximumVerticalSquared);
            float minimum = vertical - maximumVertical;
            float maximum = vertical + maximumVertical;
            float minimumVerticalSquared = minimumLength * minimumLength - horizontalSquared;
            if (minimumVerticalSquared > 0f)
                minimum = Mathf.Max(minimum, vertical + Mathf.Sqrt(minimumVerticalSquared));
            return new CharacterFootPlacementPelvisLegRange(
                input.Side,
                minimum,
                maximum,
                false);
        }

        static void Intersect(
            CharacterFootPlacementPelvisLegRange range,
            bool contributes,
            ref float minimum,
            ref float maximum)
        {
            if (!contributes)
                return;
            minimum = Mathf.Max(minimum, range.MinimumOffset);
            maximum = Mathf.Min(maximum, range.MaximumOffset);
        }

        static CharacterFootPlacementPelvisLegRange SetContribution(
            CharacterFootPlacementPelvisLegRange range,
            bool contributes) =>
            new CharacterFootPlacementPelvisLegRange(
                range.Side,
                range.MinimumOffset,
                range.MaximumOffset,
                contributes);

        internal void Reset()
        {
        }

        static bool IsFinite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
