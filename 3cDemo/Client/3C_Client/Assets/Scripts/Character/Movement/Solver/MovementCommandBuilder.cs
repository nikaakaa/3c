using UnityEngine;

namespace ThirdPersonMovement
{
    public static class MovementCommandBuilder
    {
        public static MovementCommand Build(Vector3 worldDirection, in MovementInputIntent intent, BasicMovementPhase phase, float deltaTime, in BasicMovementSettings settings)
        {
            return Build(worldDirection, in intent, phase, deltaTime, in settings, BasicMovementMotionFacts.None(phase));
        }

        public static MovementCommand Build(
            Vector3 worldDirection,
            in MovementInputIntent intent,
            BasicMovementPhase phase,
            float deltaTime,
            in BasicMovementSettings settings,
            BasicMovementMotionFacts motionFacts)
        {
            return Build(worldDirection, in intent, phase, deltaTime, in settings, motionFacts, intent.Gait);
        }

        public static MovementCommand Build(
            Vector3 worldDirection,
            in MovementInputIntent intent,
            BasicMovementPhase phase,
            float deltaTime,
            in BasicMovementSettings settings,
            BasicMovementMotionFacts motionFacts,
            BasicMovementGait gait)
        {
            float speed = intent.HasMoveIntent ? settings.ResolvePlanarSpeed(gait) * intent.Strength : 0f;
            BasicMovementMotionFacts acceptedMotionFacts = ShouldUseMotionFacts(in intent, phase, in motionFacts)
                ? motionFacts
                : BasicMovementMotionFacts.None(phase);

            return new MovementCommand(worldDirection, speed, settings.RotationSpeed, deltaTime, phase, gait, acceptedMotionFacts);
        }

        static bool ShouldUseMotionFacts(in MovementInputIntent intent, BasicMovementPhase phase, in BasicMovementMotionFacts motionFacts)
        {
            if (!motionFacts.HasAnimationMotion && !motionFacts.SuppressInputRotation && !motionFacts.SuppressInputPlanarMovement)
                return false;

            if (motionFacts.SourcePhase == phase)
                return true;

            return !intent.HasMoveIntent &&
                   motionFacts.SourcePhase == BasicMovementPhase.MoveStop &&
                   phase == BasicMovementPhase.Idle;
        }
    }
}
