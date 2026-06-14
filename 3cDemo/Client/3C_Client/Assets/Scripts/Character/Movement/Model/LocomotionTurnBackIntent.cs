using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct LocomotionTurnBackIntent
    {
        const float DirectionSqrEpsilon = 0.000001f;

        public LocomotionTurnBackIntent(
            bool isValid,
            int originStep,
            int expireStep,
            float angle,
            float threshold,
            Vector3 worldMoveDirection,
            Vector3 facingForward)
        {
            IsValid = isValid;
            OriginStep = Mathf.Max(0, originStep);
            ExpireStep = Mathf.Max(OriginStep, expireStep);
            Angle = Mathf.Max(0f, angle);
            Threshold = Mathf.Max(0f, threshold);
            WorldMoveDirection = NormalizePlanarOrZero(worldMoveDirection);
            FacingForward = NormalizePlanarOrZero(facingForward);
        }

        public bool IsValid { get; }
        public int OriginStep { get; }
        public int ExpireStep { get; }
        public float Angle { get; }
        public float Threshold { get; }
        public Vector3 WorldMoveDirection { get; }
        public Vector3 FacingForward { get; }
        public bool HasWorldMoveDirection => WorldMoveDirection.sqrMagnitude > DirectionSqrEpsilon;
        public bool HasFacingForward => FacingForward.sqrMagnitude > DirectionSqrEpsilon;

        public bool IsValidAt(int currentStep)
        {
            return IsValid && currentStep >= OriginStep && currentStep <= ExpireStep;
        }

        public LocomotionTurnBackIntent ExpireAt(int expireStep)
        {
            return new LocomotionTurnBackIntent(
                IsValid,
                OriginStep,
                expireStep,
                Angle,
                Threshold,
                WorldMoveDirection,
                FacingForward);
        }

        public static LocomotionTurnBackIntent None => default;

        public static LocomotionTurnBackIntent Capture(
            int currentStep,
            int windowSteps,
            float angle,
            float threshold,
            Vector3 worldMoveDirection,
            Vector3 facingForward)
        {
            return new LocomotionTurnBackIntent(
                true,
                currentStep,
                currentStep + Mathf.Max(0, windowSteps),
                angle,
                threshold,
                worldMoveDirection,
                facingForward);
        }

        static Vector3 NormalizePlanarOrZero(Vector3 value)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > DirectionSqrEpsilon ? value / Mathf.Sqrt(sqrMagnitude) : Vector3.zero;
        }
    }
}
