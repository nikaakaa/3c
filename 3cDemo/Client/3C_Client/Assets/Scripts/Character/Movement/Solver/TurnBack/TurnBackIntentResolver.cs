using UnityEngine;

namespace ThirdPersonMovement
{
    public readonly struct TurnBackIntentResolution
    {
        public TurnBackIntentResolution(
            LocomotionTurnBackIntent intent,
            LocomotionTurnBackIntent pendingIntent,
            bool hasLog,
            string logReason,
            LocomotionTurnBackIntent logIntent,
            float observedAngle)
        {
            Intent = intent;
            PendingIntent = pendingIntent;
            HasLog = hasLog;
            LogReason = logReason ?? string.Empty;
            LogIntent = logIntent;
            ObservedAngle = observedAngle;
        }

        public LocomotionTurnBackIntent Intent { get; }
        public LocomotionTurnBackIntent PendingIntent { get; }
        public bool HasLog { get; }
        public string LogReason { get; }
        public LocomotionTurnBackIntent LogIntent { get; }
        public float ObservedAngle { get; }
    }

    public static class TurnBackIntentResolver
    {
        const float DirectionSqrEpsilon = 0.000001f;

        public static TurnBackIntentResolution Resolve(
            in MovementInputIntent intent,
            BasicMovementGait frameGait,
            BasicMovementPhase currentPhase,
            in LocomotionSpatialFacts spatialFacts,
            int currentStep,
            in LocomotionTurnBackIntent pendingIntent,
            Vector3 previousWorldDirection,
            float minAngle,
            int windowSteps)
        {
            if (currentPhase == BasicMovementPhase.TurnBack)
                return Clear("already-turnback", currentStep, in pendingIntent);

            if (!intent.HasMoveIntent)
            {
                if (frameGait == BasicMovementGait.Run && pendingIntent.IsValidAt(currentStep))
                    return Log("hold-empty-input-window", pendingIntent, pendingIntent);

                return Clear("no-move-or-expired", currentStep, in pendingIntent);
            }

            if (frameGait != BasicMovementGait.Run || intent.Gait != BasicMovementGait.Run)
                return Clear("not-run-gait", currentStep, in pendingIntent);

            if (!spatialFacts.HasWorldMoveDirection)
                return Clear("missing-spatial-facts", currentStep, in pendingIntent);

            if (pendingIntent.IsValidAt(currentStep) &&
                Vector3.Angle(pendingIntent.WorldMoveDirection, spatialFacts.WorldMoveDirection) <= 20f)
            {
                return Log("hold-existing-reverse-input", pendingIntent, pendingIntent);
            }

            if (!TryResolveReferenceFacing(currentPhase, in spatialFacts, previousWorldDirection, minAngle, out Vector3 referenceFacing))
                return Clear("missing-facing-reference", currentStep, in pendingIntent);

            float angle = Vector3.Angle(referenceFacing, spatialFacts.WorldMoveDirection);
            if (angle >= minAngle)
            {
                LocomotionTurnBackIntent captured = LocomotionTurnBackIntent.Capture(
                    currentStep,
                    windowSteps,
                    angle,
                    minAngle,
                    spatialFacts.WorldMoveDirection,
                    referenceFacing);
                return Log("captured", captured, captured);
            }

            return Clear("angle-below-threshold", currentStep, in pendingIntent, angle);
        }

        static TurnBackIntentResolution Clear(
            string reason,
            int currentStep,
            in LocomotionTurnBackIntent pendingIntent,
            float observedAngle = -1f)
        {
            bool hasLog = pendingIntent.IsValid;
            return new TurnBackIntentResolution(
                LocomotionTurnBackIntent.None,
                LocomotionTurnBackIntent.None,
                hasLog,
                reason,
                pendingIntent,
                observedAngle);
        }

        static TurnBackIntentResolution Log(
            string reason,
            in LocomotionTurnBackIntent intent,
            in LocomotionTurnBackIntent pendingIntent)
        {
            return new TurnBackIntentResolution(
                intent,
                pendingIntent,
                true,
                reason,
                intent,
                -1f);
        }

        static bool TryResolveReferenceFacing(
            BasicMovementPhase currentPhase,
            in LocomotionSpatialFacts spatialFacts,
            Vector3 previousWorldDirection,
            float minAngle,
            out Vector3 referenceFacing)
        {
            if (currentPhase != BasicMovementPhase.MoveLoop)
                return TryNormalizePlanar(previousWorldDirection, out referenceFacing);

            if (TryNormalizePlanar(previousWorldDirection, out Vector3 previousDirection) &&
                spatialFacts.HasWorldMoveDirection &&
                Vector3.Angle(previousDirection, spatialFacts.WorldMoveDirection) >= minAngle)
            {
                referenceFacing = previousDirection;
                return true;
            }

            if (spatialFacts.HasFacingForward)
            {
                referenceFacing = spatialFacts.FacingForward;
                return true;
            }

            referenceFacing = Vector3.zero;
            return false;
        }

        static bool TryNormalizePlanar(Vector3 value, out Vector3 normalized)
        {
            value.y = 0f;
            float sqrMagnitude = value.sqrMagnitude;
            if (sqrMagnitude <= DirectionSqrEpsilon)
            {
                normalized = Vector3.zero;
                return false;
            }

            normalized = value / Mathf.Sqrt(sqrMagnitude);
            return true;
        }
    }
}
