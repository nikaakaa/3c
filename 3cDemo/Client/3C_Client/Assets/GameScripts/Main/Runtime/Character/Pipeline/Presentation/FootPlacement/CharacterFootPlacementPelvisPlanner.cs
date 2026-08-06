using System;
using UnityEngine;

namespace ThirdPersonCharacter.Pipeline.Presentation
{
    internal readonly struct CharacterFootPlacementPelvisLegInput
    {
        internal CharacterFootPlacementPelvisLegInput(
            CharacterFootSide side,
            Vector3 hipPosition,
            Vector3 animatedAnklePosition,
            Vector3 targetAnklePosition,
            float positionWeight,
            float plantSupportWeight,
            float contactWeight,
            float legLength,
            FootPlacementSurface support)
        {
            Side = side;
            HipPosition = hipPosition;
            AnimatedAnklePosition = animatedAnklePosition;
            TargetAnklePosition = targetAnklePosition;
            PositionWeight = positionWeight;
            PlantSupportWeight = plantSupportWeight;
            ContactWeight = contactWeight;
            LegLength = legLength;
            Support = support;
        }

        internal CharacterFootSide Side { get; }
        internal Vector3 HipPosition { get; }
        internal Vector3 AnimatedAnklePosition { get; }
        internal Vector3 TargetAnklePosition { get; }
        internal float PositionWeight { get; }
        internal float PlantSupportWeight { get; }
        internal float ContactWeight { get; }
        internal float LegLength { get; }
        internal FootPlacementSurface Support { get; }
        internal float SupportWeight => Mathf.Max(PlantSupportWeight, ContactWeight);
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
        public bool IsValid => Side != 0 && float.IsFinite(MinimumOffset) &&
                               float.IsFinite(MaximumOffset) && MinimumOffset <= MaximumOffset;
    }

    public readonly struct CharacterFootPlacementPelvisPlan
    {
        internal CharacterFootPlacementPelvisPlan(
            float targetOffset,
            float resolvedOffset,
            CharacterFootPlacementPelvisLegRange leftRange,
            CharacterFootPlacementPelvisLegRange rightRange,
            bool rejectLeftGoal,
            bool rejectRightGoal,
            CharacterFootPlacementPelvisHeightMode heightMode,
            CharacterFootPlacementActorMovementCompensationMode movementCompensationMode)
        {
            TargetOffset = targetOffset;
            ResolvedOffset = resolvedOffset;
            LeftRange = leftRange;
            RightRange = rightRange;
            RejectLeftGoal = rejectLeftGoal;
            RejectRightGoal = rejectRightGoal;
            HeightMode = heightMode;
            MovementCompensationMode = movementCompensationMode;
        }

        public float TargetOffset { get; }
        public float ResolvedOffset { get; }
        public CharacterFootPlacementPelvisLegRange LeftRange { get; }
        public CharacterFootPlacementPelvisLegRange RightRange { get; }
        public bool RejectLeftGoal { get; }
        public bool RejectRightGoal { get; }
        public CharacterFootPlacementPelvisHeightMode HeightMode { get; }
        public CharacterFootPlacementActorMovementCompensationMode MovementCompensationMode { get; }
        public Vector3 ComponentTranslation => Vector3.up * ResolvedOffset;
    }

    internal sealed class CharacterFootPlacementPelvisPlanner
    {
        const float Epsilon = 0.0001f;
        float m_ResolvedOffset;
        Vector3 m_LastRootPosition;
        bool m_HasRootPosition;

        internal CharacterFootPlacementPelvisPlan Plan(
            in CharacterFootPlacementPelvisLegInput left,
            in CharacterFootPlacementPelvisLegInput right,
            Vector3 componentUp,
            Vector3 componentForward,
            Vector3 bodyVelocity,
            Vector3 rootPosition,
            float deltaSeconds,
            CharacterPredictiveFootPlacementRuntimeSettings settings)
        {
            Vector3 up = RequireDirection(componentUp, nameof(componentUp));
            Vector3 forward = Vector3.ProjectOnPlane(componentForward, up);
            if (forward.sqrMagnitude <= Epsilon)
                throw new InvalidOperationException("Foot Placement pelvis forward direction is degenerate.");
            forward.Normalize();

            CharacterFootPlacementPelvisLegRange leftRange = BuildRange(in left, up, settings);
            CharacterFootPlacementPelvisLegRange rightRange = BuildRange(in right, up, settings);
            bool rejectLeftGoal = left.PositionWeight > Epsilon && !leftRange.IsValid;
            bool rejectRightGoal = right.PositionWeight > Epsilon && !rightRange.IsValid;
            SelectContributors(
                in left,
                in right,
                leftRange,
                rightRange,
                up,
                forward,
                bodyVelocity,
                settings.PelvisHeightMode,
                out bool useLeft,
                out bool useRight);

            leftRange = SetContribution(leftRange, useLeft);
            rightRange = SetContribution(rightRange, useRight);
            ResolveCombinedRange(
                in left,
                in right,
                leftRange,
                rightRange,
                up,
                settings,
                out float minimum,
                out float maximum,
                out CharacterFootSide rejectedSide);
            if (rejectedSide == CharacterFootSide.Left)
                rejectLeftGoal = true;
            else if (rejectedSide == CharacterFootSide.Right)
                rejectRightGoal = true;
            if (rejectLeftGoal)
                leftRange = SetContribution(leftRange, false);
            if (rejectRightGoal)
                rightRange = SetContribution(rightRange, false);

            float target = minimum <= maximum
                ? ResolvePreferredOffset(
                    in left,
                    in right,
                    leftRange,
                    rightRange,
                    up,
                    minimum,
                    maximum)
                : 0f;
            if (Mathf.Abs(target) <= settings.PelvisHeightDeadZone)
                target = 0f;

            if (m_HasRootPosition &&
                settings.ActorMovementCompensationMode ==
                CharacterFootPlacementActorMovementCompensationMode.HoldWorldDuringInterpolation)
            {
                m_ResolvedOffset -= Vector3.Dot(rootPosition - m_LastRootPosition, up);
            }
            m_LastRootPosition = rootPosition;
            m_HasRootPosition = true;

            float response = 1f - Mathf.Exp(-settings.PelvisInterpolationSpeed * deltaSeconds);
            m_ResolvedOffset = Mathf.Lerp(m_ResolvedOffset, target, response);
            m_ResolvedOffset = Mathf.Clamp(
                m_ResolvedOffset,
                -settings.MaximumPelvisLowering,
                settings.MaximumPelvisRaising);
            if (Mathf.Abs(m_ResolvedOffset) <= settings.PelvisHeightDeadZone && target == 0f)
                m_ResolvedOffset = 0f;

            return new CharacterFootPlacementPelvisPlan(
                target,
                m_ResolvedOffset,
                leftRange,
                rightRange,
                rejectLeftGoal,
                rejectRightGoal,
                settings.PelvisHeightMode,
                settings.ActorMovementCompensationMode);
        }

        internal void Reset()
        {
            m_ResolvedOffset = 0f;
            m_LastRootPosition = Vector3.zero;
            m_HasRootPosition = false;
        }

        static CharacterFootPlacementPelvisLegRange BuildRange(
            in CharacterFootPlacementPelvisLegInput input,
            Vector3 up,
            CharacterPredictiveFootPlacementRuntimeSettings settings)
        {
            if (!input.Support.IsValid || input.PositionWeight <= Epsilon ||
                !float.IsFinite(input.LegLength) || input.LegLength <= Epsilon)
                return default;

            Vector3 horizontalAdjustment = Vector3.ProjectOnPlane(
                input.TargetAnklePosition - input.AnimatedAnklePosition,
                up);
            if (horizontalAdjustment.magnitude > settings.MaximumHorizontalFootAdjustment)
                return default;

            float weight = Mathf.Clamp01(input.PositionWeight);
            Vector3 weightedTarget = Vector3.Lerp(
                input.AnimatedAnklePosition,
                input.TargetAnklePosition,
                weight);
            Vector3 delta = input.HipPosition - weightedTarget;
            float vertical = Vector3.Dot(delta, up);
            Vector3 horizontal = delta - up * vertical;
            float horizontalSquare = horizontal.sqrMagnitude;
            float minimumLength = input.LegLength * settings.MinimumLegExtensionRatio;
            float maximumLength = input.LegLength * settings.MaximumLegExtensionRatio;
            float maximumSquare = maximumLength * maximumLength;
            if (horizontalSquare >= maximumSquare)
                return default;

            float minimumVertical = horizontalSquare < minimumLength * minimumLength
                ? Mathf.Sqrt(minimumLength * minimumLength - horizontalSquare)
                : 0f;
            float maximumVertical = Mathf.Sqrt(maximumSquare - horizontalSquare);
            float minimumOffset = (-vertical + minimumVertical) / weight;
            float maximumOffset = (-vertical + maximumVertical) / weight;
            minimumOffset = Mathf.Max(minimumOffset, -settings.MaximumPelvisLowering);
            maximumOffset = Mathf.Min(maximumOffset, settings.MaximumPelvisRaising);
            if (!float.IsFinite(minimumOffset) || !float.IsFinite(maximumOffset) ||
                minimumOffset > maximumOffset)
                return default;
            return new CharacterFootPlacementPelvisLegRange(
                input.Side,
                minimumOffset,
                maximumOffset,
                input.SupportWeight,
                false);
        }

        static void SelectContributors(
            in CharacterFootPlacementPelvisLegInput left,
            in CharacterFootPlacementPelvisLegInput right,
            CharacterFootPlacementPelvisLegRange leftRange,
            CharacterFootPlacementPelvisLegRange rightRange,
            Vector3 up,
            Vector3 forward,
            Vector3 bodyVelocity,
            CharacterFootPlacementPelvisHeightMode mode,
            out bool useLeft,
            out bool useRight)
        {
            useLeft = leftRange.IsValid;
            useRight = rightRange.IsValid;
            if (mode != CharacterFootPlacementPelvisHeightMode.AllLegs)
            {
                useLeft &= left.SupportWeight > Epsilon;
                useRight &= right.SupportWeight > Epsilon;
            }
            if (mode != CharacterFootPlacementPelvisHeightMode.DirectionalSlopeSupport ||
                !useLeft || !useRight)
                return;

            Vector3 direction = Vector3.ProjectOnPlane(bodyVelocity, up);
            if (direction.sqrMagnitude <= Epsilon)
                direction = forward;
            else
                direction.Normalize();
            float leftForward = Vector3.Dot(left.TargetAnklePosition, direction);
            float rightForward = Vector3.Dot(right.TargetAnklePosition, direction);
            if (Mathf.Abs(leftForward - rightForward) <= 0.01f)
                return;
            bool leftIsFront = leftForward > rightForward;
            float leftHeight = Vector3.Dot(left.TargetAnklePosition, up);
            float rightHeight = Vector3.Dot(right.TargetAnklePosition, up);
            bool frontIsHigher = leftIsFront
                ? leftHeight > rightHeight + 0.01f
                : rightHeight > leftHeight + 0.01f;
            if (!frontIsHigher)
                return;
            useLeft = leftIsFront;
            useRight = !leftIsFront;
        }

        static void ResolveCombinedRange(
            in CharacterFootPlacementPelvisLegInput left,
            in CharacterFootPlacementPelvisLegInput right,
            CharacterFootPlacementPelvisLegRange leftRange,
            CharacterFootPlacementPelvisLegRange rightRange,
            Vector3 up,
            CharacterPredictiveFootPlacementRuntimeSettings settings,
            out float minimum,
            out float maximum,
            out CharacterFootSide rejectedSide)
        {
            bool useLeft = leftRange.Contributes;
            bool useRight = rightRange.Contributes;
            rejectedSide = 0;
            if (!useLeft && !useRight)
            {
                minimum = -settings.MaximumPelvisLowering;
                maximum = settings.MaximumPelvisRaising;
                return;
            }
            if (useLeft && !useRight)
            {
                minimum = leftRange.MinimumOffset;
                maximum = leftRange.MaximumOffset;
                return;
            }
            if (!useLeft)
            {
                minimum = rightRange.MinimumOffset;
                maximum = rightRange.MaximumOffset;
                return;
            }

            minimum = Mathf.Max(leftRange.MinimumOffset, rightRange.MinimumOffset);
            maximum = Mathf.Min(leftRange.MaximumOffset, rightRange.MaximumOffset);
            if (minimum <= maximum)
                return;

            bool keepLeft = SelectPrimary(in left, in right, leftRange, rightRange, up);
            if (keepLeft)
            {
                minimum = leftRange.MinimumOffset;
                maximum = leftRange.MaximumOffset;
                rejectedSide = CharacterFootSide.Right;
            }
            else
            {
                minimum = rightRange.MinimumOffset;
                maximum = rightRange.MaximumOffset;
                rejectedSide = CharacterFootSide.Left;
            }
        }

        static bool SelectPrimary(
            in CharacterFootPlacementPelvisLegInput left,
            in CharacterFootPlacementPelvisLegInput right,
            CharacterFootPlacementPelvisLegRange leftRange,
            CharacterFootPlacementPelvisLegRange rightRange,
            Vector3 up)
        {
            if (!Mathf.Approximately(left.SupportWeight, right.SupportWeight))
                return left.SupportWeight > right.SupportWeight;
            float leftDistance = DistanceToZero(leftRange);
            float rightDistance = DistanceToZero(rightRange);
            if (!Mathf.Approximately(leftDistance, rightDistance))
                return leftDistance < rightDistance;
            return Vector3.Dot(left.TargetAnklePosition, up) <=
                   Vector3.Dot(right.TargetAnklePosition, up);
        }

        static float DistanceToZero(CharacterFootPlacementPelvisLegRange range)
        {
            if (range.MinimumOffset > 0f)
                return range.MinimumOffset;
            if (range.MaximumOffset < 0f)
                return -range.MaximumOffset;
            return 0f;
        }

        static CharacterFootPlacementPelvisLegRange SetContribution(
            CharacterFootPlacementPelvisLegRange range,
            bool contributes) =>
            range.IsValid
                ? new CharacterFootPlacementPelvisLegRange(
                    range.Side,
                    range.MinimumOffset,
                    range.MaximumOffset,
                    range.SupportWeight,
                    contributes)
                : default;

        static float ResolvePreferredOffset(
            in CharacterFootPlacementPelvisLegInput left,
            in CharacterFootPlacementPelvisLegInput right,
            CharacterFootPlacementPelvisLegRange leftRange,
            CharacterFootPlacementPelvisLegRange rightRange,
            Vector3 up,
            float minimum,
            float maximum)
        {
            float weightedOffset = 0f;
            float totalWeight = 0f;
            AccumulatePreferredOffset(
                in left,
                leftRange,
                up,
                ref weightedOffset,
                ref totalWeight);
            AccumulatePreferredOffset(
                in right,
                rightRange,
                up,
                ref weightedOffset,
                ref totalWeight);
            float preferred = totalWeight > Epsilon
                ? weightedOffset / totalWeight
                : 0f;
            return Mathf.Clamp(preferred, minimum, maximum);
        }

        static void AccumulatePreferredOffset(
            in CharacterFootPlacementPelvisLegInput input,
            CharacterFootPlacementPelvisLegRange range,
            Vector3 up,
            ref float weightedOffset,
            ref float totalWeight)
        {
            if (!range.Contributes)
                return;
            float contributionWeight = Mathf.Max(
                range.SupportWeight,
                Mathf.Clamp01(input.PositionWeight));
            if (contributionWeight <= Epsilon)
                return;
            float goalOffset = Vector3.Dot(
                input.TargetAnklePosition - input.AnimatedAnklePosition,
                up) * Mathf.Clamp01(input.PositionWeight);
            weightedOffset += goalOffset * contributionWeight;
            totalWeight += contributionWeight;
        }

        static Vector3 RequireDirection(Vector3 value, string field)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) ||
                !float.IsFinite(value.z) || value.sqrMagnitude <= Epsilon)
                throw new ArgumentException("Foot Placement pelvis direction is invalid.", field);
            return value.normalized;
        }
    }
}
